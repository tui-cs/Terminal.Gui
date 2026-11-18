using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;

namespace Terminal.Gui.Configuration;

/// <summary>
///     Builds and manages a Terminal.Gui MEC-based configuration, loading from all standard sources
///     in the correct precedence order.
/// </summary>
/// <remarks>
///     <para>
///         This is the MEC-based configuration entry point.
///         It provides the same multi-source precedence (library defaults → app defaults → user files
///         → environment variables → runtime config) using standard Microsoft.Extensions.Configuration.
///     </para>
///     <para><b>App Developer Usage:</b></para>
///     <code>
///     // Define your app settings POCO:
///     public class MyAppSettings
///     {
///         public string Title { get; set; } = "My App";
///         public bool DarkMode { get; set; }
///         public static MyAppSettings Defaults { get; set; } = new ();
///     }
///
///     // In your app startup:
///     var builder = new TuiConfigurationBuilder ("MyApp");
///     builder.BindAppSettings&lt;MyAppSettings&gt; ("MyApp", s =&gt; MyAppSettings.Defaults = s);
///     builder.ApplyToStaticFacades ();
///
///     // Access settings:
///     string title = MyAppSettings.Defaults.Title;
///     </code>
///     <para>
///         To add custom configuration sources, use the MEC extension methods directly:
///     </para>
///     <code>
///     IConfigurationBuilder configBuilder = new ConfigurationBuilder ()
///         .AddTuiLibraryDefaults ()
///         .AddTuiUserFiles ("MyApp")
///         .AddJsonFile ("custom-settings.json", optional: true);
///     IConfiguration config = configBuilder.Build ();
///     </code>
/// </remarks>
public class TuiConfigurationBuilder
{
    /// <summary>
    ///     Process-wide builder used by <see cref="ThemeManager"/> and module initialization.
    /// </summary>
    public static TuiConfigurationBuilder Shared { get; } = new ();

    private readonly string? _appName;
    private readonly string? _currentDirectory;
    private readonly bool _includeUserSources;
    private string? _runtimeConfig;
    private IConfiguration? _configuration;
    private JsonObject? _mergedSourceJson;

    // Root (theme-independent) settings bound from _rootSettingsConfig, keyed by settings type
    private readonly Dictionary<Type, object> _rootSettingsCache = [];
    private IConfiguration? _rootSettingsConfig;

    // Fully merged (root + theme overlay) settings, keyed by theme path, section name and settings type
    private readonly Dictionary<(string ThemePath, string Section, Type SettingsType), object> _overlaySettingsCache = [];

    /// <summary>Initializes a new instance of <see cref="TuiConfigurationBuilder"/>.</summary>
    /// <param name="appName">The application name for app-specific config file discovery. If null, uses entry assembly name.</param>
    /// <param name="currentDirectory">
    ///     The directory <c>./.tui/</c> config files are resolved against. If null, uses
    ///     <see cref="Environment.CurrentDirectory"/>.
    /// </param>
    public TuiConfigurationBuilder (string? appName = null, string? currentDirectory = null)
        : this (appName, currentDirectory, includeUserSources: true)
    { }

    /// <summary>
    ///     INTERNAL: Also controls whether user files (<c>~/.tui</c>, <c>./.tui</c>) and the
    ///     <c>TUI_CONFIG</c> environment variable are loaded. Tests pass
    ///     <paramref name="includeUserSources"/> <see langword="false"/> so machine-local
    ///     configuration cannot change test outcomes.
    /// </summary>
    internal TuiConfigurationBuilder (string? appName, string? currentDirectory, bool includeUserSources)
    {
        _appName = appName ?? System.Reflection.Assembly.GetEntryAssembly ()?.GetName ().Name;
        _currentDirectory = currentDirectory;
        _includeUserSources = includeUserSources;
    }

    /// <summary>
    ///     Gets or sets the runtime configuration JSON string (highest priority).
    ///     Setting this invalidates the cached configuration, causing a rebuild on next access.
    /// </summary>
    public string? RuntimeConfig
    {
        get => _runtimeConfig;
        set
        {
            _runtimeConfig = value;
            _configuration = null; // force rebuild
        }
    }

    /// <summary>
    ///     Gets the built <see cref="IConfiguration"/> instance. Lazily built on first access.
    ///     Rebuilt when <see cref="RuntimeConfig"/> changes.
    /// </summary>
    public IConfiguration Configuration => _configuration ??= Build ();

    /// <summary>
    ///     Builds the configuration from all sources in precedence order.
    /// </summary>
    /// <remarks>
    ///     A malformed source must never crash the app — the build runs from a module initializer.
    ///     Per-source failures are collected in <see cref="TuiJsonErrors"/> and skipped; if the build
    ///     still fails, the library's embedded defaults are used and the error is printed at shutdown.
    /// </remarks>
    /// <returns>The built configuration root.</returns>
    public IConfiguration Build ()
    {
        // Errors describe the current effective configuration; without this, a watcher re-applying a
        // persistently malformed source would re-add the identical error on every rebuild.
        TuiJsonErrors.Clear ();

        List<JsonObject> jsonSources = [];
        IConfigurationBuilder builder = new ConfigurationBuilder ();
        TuiConfigurationExtensions.AddTuiLibraryDefaults (builder, jsonSources);
        TuiConfigurationExtensions.AddTuiAppDefaults (builder, _appName, jsonSources);

        if (_includeUserSources)
        {
            TuiConfigurationExtensions.AddTuiUserFiles (builder, _appName, _currentDirectory, jsonSources);
            TuiConfigurationExtensions.AddTuiEnvironmentVariable (builder, jsonSources);
        }

        TuiConfigurationExtensions.AddTuiRuntimeConfig (builder, _runtimeConfig, jsonSources);

        try
        {
            _configuration = builder.Build ();
        }
        catch (Exception ex)
        {
            TuiJsonErrors.Add ($"Configuration build failed: {ex.Message}. Falling back to library defaults.");
            _configuration = new ConfigurationBuilder ().AddTuiLibraryDefaults ().Build ();
        }

        JsonObject merged = [];

        foreach (JsonObject source in jsonSources)
        {
            JsonMerge.DeepMerge (merged, source);
        }

        _mergedSourceJson = merged;

        return _configuration;
    }

    /// <summary>
    ///     Gets a raw-JSON deep merge of all sources (arrays replace wholesale, unlike MEC's per-index
    ///     merge). Used for sections bound via STJ — key bindings and schemes — where a higher-priority
    ///     source's array must atomically replace a lower-priority source's.
    /// </summary>
    internal JsonObject MergedSourceJson
    {
        get
        {
            _ = Configuration;

            return _mergedSourceJson!;
        }
    }

    /// <summary>
    ///     Invalidates the cached configuration so the next access to <see cref="Configuration"/> rebuilds
    ///     from all sources. Use after a configuration file changes on disk.
    /// </summary>
    public void Reload ()
    {
        _configuration = null;
        _rootSettingsCache.Clear ();
        _overlaySettingsCache.Clear ();
    }

    /// <summary>
    ///     Gets the MEC-backed theme manager instance for this builder.
    /// </summary>
    public IThemeManager ThemeManager => _themeManager ??= new MecThemeManager (this);
    private IThemeManager? _themeManager;

    /// <summary>
    ///     Gets the MEC-backed scheme manager instance for this builder.
    /// </summary>
    public ISchemeManager SchemeManager => _schemeManager ??= new SchemeManager ();
    private ISchemeManager? _schemeManager;

    /// <summary>
    ///     Applies configuration sources to SettingsScope facades, then publishes the active theme's overlays.
    ///     Always raises <see cref="ThemeManager.ThemeChanged"/> afterwards — the facades were re-published
    ///     wholesale, so subscribers must re-render even when the theme name is unchanged (e.g. a hot reload
    ///     that edits the current theme's colors).
    /// </summary>
    public void ApplyToStaticFacades ()
    {
        IConfiguration config = Configuration;
        BindThemeScalar (config);
        ApplicationSettings.Defaults = BindSection<ApplicationSettings> (config, "Application");
        DriverSettings.Defaults = BindSection<DriverSettings> (config, "Driver");
        FileDialogSettings.Defaults = BindSection<FileDialogSettings> (config, "FileDialog");
        FileDialogStyleSettings.Defaults = BindSection<FileDialogStyleSettings> (config, "FileDialogStyle");
        KeySettings.Defaults = BindSection<KeySettings> (config, "Key");
        TraceSettings.Defaults = BindSection<TraceSettings> (config, "Trace");
        KeyBindingConfiguration.Apply (MergedSourceJson);
        ApplyActiveThemeOverlays ();
        global::Terminal.Gui.Configuration.ThemeManager.RaiseThemeChanged (ThemeSettings.Defaults.Theme);
    }

    /// <summary>
    ///     Publishes ThemeScope <c>Current</c> values and schemes for <see cref="ThemeSettings.Defaults"/>.Theme.
    ///     Does not re-read the Theme scalar from configuration.
    /// </summary>
    public void ApplyActiveThemeOverlays ()
    {
        IConfiguration config = Configuration;
        string activeTheme = ThemeSettings.Defaults.Theme ?? global::Terminal.Gui.Configuration.ThemeManager.DEFAULT_THEME_NAME;

        // Resolve the theme section once to avoid repeated lookups
        IConfigurationSection? themeObject = ThemeCatalog.Find (config, activeTheme);

        if (!ReferenceEquals (_rootSettingsConfig, config))
        {
            _rootSettingsCache.Clear ();
            _overlaySettingsCache.Clear ();
            _rootSettingsConfig = config;
        }

        ButtonSettings.Current = BindThemeScope<ButtonSettings> (config, "Button", themeObject);
        CheckBoxSettings.Current = BindThemeScope<CheckBoxSettings> (config, "CheckBox", themeObject);
        CharMapSettings.Current = BindThemeScope<CharMapSettings> (config, "CharMap", themeObject);
        DialogSettings.Current = BindThemeScope<DialogSettings> (config, "Dialog", themeObject);
        FrameViewSettings.Current = BindThemeScope<FrameViewSettings> (config, "FrameView", themeObject);
        HexViewSettings.Current = BindThemeScope<HexViewSettings> (config, "HexView", themeObject);
        LinearRangeSettings.Current = BindThemeScope<LinearRangeSettings> (config, "LinearRange", themeObject);
        MenuBarSettings.Current = BindThemeScope<MenuBarSettings> (config, "MenuBar", themeObject);
        MenuSettings.Current = BindThemeScope<MenuSettings> (config, "Menu", themeObject);
        MessageBoxSettings.Current = BindThemeScope<MessageBoxSettings> (config, "MessageBox", themeObject);
        NerdFontsSettings.Current = BindThemeScope<NerdFontsSettings> (config, "NerdFonts", themeObject);
        PopoverMenuSettings.Current = BindThemeScope<PopoverMenuSettings> (config, "PopoverMenu", themeObject);
        SelectorBaseSettings.Current = BindThemeScope<SelectorBaseSettings> (config, "SelectorBase", themeObject);
        StatusBarSettings.Current = BindThemeScope<StatusBarSettings> (config, "StatusBar", themeObject);
        TextFieldSettings.Current = BindThemeScope<TextFieldSettings> (config, "TextField", themeObject);
        TextViewSettings.Current = BindThemeScope<TextViewSettings> (config, "TextView", themeObject);
        WindowSettings.Current = BindThemeScope<WindowSettings> (config, "Window", themeObject);
        GlyphSettings.Current = BindThemeScope<GlyphSettings> (config, "Glyphs", themeObject);

        string? canonicalTheme = ThemeCatalog.CanonicalName (config, activeTheme);
        global::Terminal.Gui.Configuration.SchemeManager.ApplyFromConfiguration (MergedSourceJson, canonicalTheme);
    }

    /// <summary>
    ///     Two-pass overlay bind for ThemeScope POCOs. The root section is bound once per configuration and
    ///     cached; if the active theme has an overlay for <paramref name="sectionName"/>, it is applied on top
    ///     of a copy of the root values.
    /// </summary>
    [UnconditionalSuppressMessage ("Trimming", "IL2026", Justification = "Theme settings are simple public types; Bind only walks declared properties.")]
    [UnconditionalSuppressMessage ("AOT", "IL3050", Justification = "Theme settings are simple types; all closed generic calls use library-defined types.")]
    private T BindThemeScope<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] T> (IConfiguration config, string sectionName, IConfigurationSection? themeObject) where T : new ()
    {
        T rootSettings;

        if (_rootSettingsCache.TryGetValue (typeof (T), out object? cached))
        {
            rootSettings = (T)cached;
        }
        else
        {
            rootSettings = BindSection<T> (config, sectionName);
            _rootSettingsCache [typeof (T)] = rootSettings!;
        }

        IConfigurationSection? overlay = themeObject?.GetSection (sectionName);

        if (overlay is null || !overlay.Exists ())
        {
            return rootSettings;
        }

        (string, string, Type) key = (themeObject!.Path, sectionName, typeof (T));

        if (_overlaySettingsCache.TryGetValue (key, out object? merged))
        {
            return (T)merged;
        }

        T result = new ();
        CopySettingsProperties (rootSettings, result);
        overlay.Bind (result);
        BindDirectProperties (overlay, result);
        _overlaySettingsCache [key] = result!;

        return result;
    }

    /// <summary>Copy all public properties from source to target.</summary>
    private static void CopySettingsProperties<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] T> (T source, T target)
    {
        foreach (var prop in typeof (T).GetProperties (System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
        {
            if (prop is not { CanRead: true, CanWrite: true })
            {
                continue;
            }

            try
            {
                prop.SetValue (target, prop.GetValue (source));
            }
            catch
            {
                // Ignore copy errors
            }
        }
    }

    /// <summary>
    ///     Sets the active theme, publishes overlays, and raises <see cref="ThemeManager.ThemeChanged"/>.
    ///     Unknown names are ignored.
    /// </summary>
    /// <returns><see langword="true"/> if the theme exists (already active or newly applied).</returns>
    internal bool TrySwitchTheme (string themeName)
    {
        string? canonical = ThemeCatalog.CanonicalName (Configuration, themeName);

        if (canonical is null)
        {
            return false;
        }

        if (string.Equals (ThemeSettings.Defaults.Theme, canonical, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        ThemeSettings.Defaults = new () { Theme = canonical };
        ApplyActiveThemeOverlays ();
        global::Terminal.Gui.Configuration.ThemeManager.RaiseThemeChanged (canonical);

        return true;
    }

    /// <summary>
    ///     Binds a custom application settings section from the configuration to a POCO instance.
    ///     This is the MEC replacement for application-specific settings sections.
    /// </summary>
    /// <typeparam name="T">The settings POCO type.</typeparam>
    /// <param name="sectionName">The JSON section name to bind from.</param>
    /// <param name="apply">Action to apply the bound settings (typically update a static Defaults property).</param>
    /// <returns>This builder for chaining.</returns>
    [UnconditionalSuppressMessage ("Trimming", "IL2026", Justification = "Settings POCOs are simple public types; Bind only walks declared properties.")]
    [UnconditionalSuppressMessage ("AOT", "IL3050", Justification = "Settings POCOs are simple types; no generic instantiation needed at runtime.")]
    public TuiConfigurationBuilder BindAppSettings<T> (string sectionName, Action<T> apply) where T : new ()
    {
        T settings = new ();
        Configuration.GetSection (sectionName).Bind (settings);
        apply (settings);

        return this;
    }

    [UnconditionalSuppressMessage ("Trimming", "IL2026", Justification = "Settings POCOs are simple public types; Bind only walks declared properties.")]
    [UnconditionalSuppressMessage ("AOT", "IL3050", Justification = "Settings POCOs are simple types; no generic instantiation needed at runtime.")]
    private static T BindSection<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] T> (IConfiguration config, string sectionName) where T : new ()
    {
        T settings = new ();
        IConfigurationSection section = config.GetSection (sectionName);

        if (!section.Exists ())
        {
            return settings;
        }

        section.Bind (settings);
        BindDirectProperties (section, settings);

        return settings;
    }

    /// <summary>
    ///     Binds the scalar <c>Theme</c> key from configuration to <see cref="ThemeSettings.Defaults"/>.
    ///     Accepts either a root scalar (<c>"Theme": "Dark"</c>) or a nested section
    ///     (<c>"Theme": { "Theme": "Dark" }</c>).
    /// </summary>
    private static void BindThemeScalar (IConfiguration config)
    {
        string? themeValue = config ["Theme"];

        if (string.IsNullOrEmpty (themeValue))
        {
            themeValue = config.GetSection ("Theme") ["Theme"];
        }

        if (string.IsNullOrEmpty (themeValue))
        {
            return;
        }

        ThemeSettings.Defaults = new () { Theme = themeValue };
    }

    /// <summary>
    ///     Binds scalar properties whose names match keys on <paramref name="section"/> (no dotted prefix).
    ///     Used after <c>Bind</c> so types MEC does not convert (notably <see cref="Rune"/>) still apply.
    /// </summary>
    [UnconditionalSuppressMessage ("Trimming", "IL2026", Justification = "Settings POCOs are simple public types; Bind only walks declared properties.")]
    [UnconditionalSuppressMessage ("AOT", "IL3050", Justification = "Settings POCOs are simple types; no generic instantiation needed at runtime.")]
    private static void BindDirectProperties<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] T> (IConfiguration section, T settings)
    {
        foreach (PropertyInfo prop in typeof (T).GetProperties (BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanWrite)
            {
                continue;
            }

            string? value = section [prop.Name];

            if (value is null)
            {
                continue;
            }

            try
            {
                object? converted = ConvertValue (value, prop.PropertyType);

                if (converted is not null)
                {
                    prop.SetValue (settings, converted);
                }
            }
            catch (Exception)
            {
                // Skip properties whose value cannot be converted to the target type.
            }
        }
    }

    /// <summary>
    ///     Converts a configuration string value to the target property type. Only the scalar types used by the
    ///     settings POCOs are supported, so this path is trim/AOT-safe — it deliberately avoids
    ///     <c>TypeDescriptor.GetConverter</c> (which is <see cref="RequiresUnreferencedCodeAttribute"/> /
    ///     <see cref="RequiresDynamicCodeAttribute"/> and breaks NativeAOT/trimmed consumers). New non-scalar
    ///     settings property types must be added here explicitly. Unsupported types return <see langword="null"/>
    ///     and are skipped by <see cref="BindDirectProperties{T}"/>.
    /// </summary>
    private static object? ConvertValue (string value, Type targetType)
    {
        if (targetType == typeof (string))
        {
            return value;
        }

        if (targetType == typeof (bool))
        {
            return bool.Parse (value);
        }

        if (targetType == typeof (int))
        {
            return int.Parse (value);
        }

        if (targetType == typeof (Rune))
        {
            // Glyph forms first ("6" is the glyph '6', "U+2611" is ☑). Multi-digit strings are legacy
            // JSON-number codepoints flattened to strings by MEC (e.g. "9733" is ★); the glyph parser
            // rejects them, so they fall through to the codepoint parse.
            if (RuneJsonConverter.TryParse (value, out Rune rune))
            {
                return rune;
            }

            if (uint.TryParse (value, out uint codePoint) && Rune.IsValid (codePoint))
            {
                return new Rune (codePoint);
            }

            return null;
        }

        if (targetType == typeof (Key))
        {
            return Key.TryParse (value, out Key key) ? key : null;
        }

        if (targetType.IsEnum)
        {
            return Enum.Parse (targetType, value);
        }

        return null;
    }
}
