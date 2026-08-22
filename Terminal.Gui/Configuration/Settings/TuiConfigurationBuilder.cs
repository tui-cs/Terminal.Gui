using System.Diagnostics.CodeAnalysis;
using System.Reflection;
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
    private string? _runtimeConfig;
    private IConfiguration? _configuration;

    /// <summary>Initializes a new instance of <see cref="TuiConfigurationBuilder"/>.</summary>
    /// <param name="appName">The application name for app-specific config file discovery. If null, uses entry assembly name.</param>
    public TuiConfigurationBuilder (string? appName = null)
    {
        _appName = appName ?? System.Reflection.Assembly.GetEntryAssembly ()?.GetName ().Name;
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
    /// <returns>The built configuration root.</returns>
    public IConfiguration Build ()
    {
        IConfigurationBuilder builder = new ConfigurationBuilder ()
                                        .AddTuiLibraryDefaults ()
                                        .AddTuiAppDefaults (_appName)
                                        .AddTuiUserFiles (_appName)
                                        .AddTuiEnvironmentVariable ()
                                        .AddTuiRuntimeConfig (_runtimeConfig);

        _configuration = builder.Build ();

        return _configuration;
    }

    /// <summary>
    ///     Gets the MEC-backed theme manager instance for this builder.
    /// </summary>
    public IThemeManager ThemeManager => _themeManager ??= new MecThemeManager (this);
    private IThemeManager? _themeManager;

    /// <summary>
    ///     Gets the MEC-backed scheme manager instance for this builder.
    /// </summary>
    public ISchemeManager SchemeManager => _schemeManager ??= new MecSchemeManager ();
    private ISchemeManager? _schemeManager;

    /// <summary>
    ///     Applies the current configuration to all static settings facades.
    ///     Call this after building or rebuilding to push MEC values to the static <c>Defaults</c>/<c>Current</c> properties.
    /// </summary>
    /// <param name="rebindSelectedTheme">
    ///     When <see langword="true"/> (the default), the selected theme is read from configuration.
    ///     When <see langword="false"/>, the already-selected <see cref="ThemeSettings.Defaults"/>.Theme is kept so
    ///     <see cref="MecThemeManager.SwitchTheme"/> can re-apply overlays without resetting to the config default.
    /// </param>
    public void ApplyToStaticFacades (bool rebindSelectedTheme = true)
    {
        IConfiguration config = Configuration;

        if (rebindSelectedTheme)
        {
            BindThemeScalar (config);
        }

        // SettingsScope POCOs
        BindSection<ApplicationSettings> (config, "Application", s => ApplicationSettings.Defaults = s);
        BindSection<DriverSettings> (config, "Driver", s => DriverSettings.Defaults = s);
        BindSection<FileDialogSettings> (config, "FileDialog", s => FileDialogSettings.Defaults = s);
        BindSection<FileDialogStyleSettings> (config, "FileDialogStyle", s => FileDialogStyleSettings.Defaults = s);
        BindSection<KeySettings> (config, "Key", s => KeySettings.Defaults = s);
        BindSection<TraceSettings> (config, "Trace", s => TraceSettings.Defaults = s);

        // ThemeScope POCOs: two-pass overlay (root section + Themes:<active>:<section>) writes Current.
        // TODO(A2): when ThemeSettings converts to record + Current, this becomes an immutable snapshot.
        string activeTheme = ThemeSettings.Defaults.Theme;
        BindThemeScope<ButtonSettings> (config, "Button", activeTheme, s => ButtonSettings.Current = s);
        BindThemeScope<CheckBoxSettings> (config, "CheckBox", activeTheme, s => CheckBoxSettings.Current = s);
        BindThemeScope<CharMapSettings> (config, "CharMap", activeTheme, s => CharMapSettings.Current = s);
        BindThemeScope<DialogSettings> (config, "Dialog", activeTheme, s => DialogSettings.Current = s);
        BindThemeScope<FrameViewSettings> (config, "FrameView", activeTheme, s => FrameViewSettings.Current = s);
        BindThemeScope<HexViewSettings> (config, "HexView", activeTheme, s => HexViewSettings.Current = s);
        BindThemeScope<LinearRangeSettings> (config, "LinearRange", activeTheme, s => LinearRangeSettings.Current = s);
        BindThemeScope<MenuBarSettings> (config, "MenuBar", activeTheme, s => MenuBarSettings.Current = s);
        BindThemeScope<MenuSettings> (config, "Menu", activeTheme, s => MenuSettings.Current = s);
        BindThemeScope<MessageBoxSettings> (config, "MessageBox", activeTheme, s => MessageBoxSettings.Current = s);
        BindThemeScope<NerdFontsSettings> (config, "NerdFonts", activeTheme, s => NerdFontsSettings.Current = s);
        BindThemeScope<PopoverMenuSettings> (config, "PopoverMenu", activeTheme, s => PopoverMenuSettings.Current = s);
        BindThemeScope<SelectorBaseSettings> (config, "SelectorBase", activeTheme, s => SelectorBaseSettings.Current = s);
        BindThemeScope<StatusBarSettings> (config, "StatusBar", activeTheme, s => StatusBarSettings.Current = s);
        BindThemeScope<TextFieldSettings> (config, "TextField", activeTheme, s => TextFieldSettings.Current = s);
        BindThemeScope<TextViewSettings> (config, "TextView", activeTheme, s => TextViewSettings.Current = s);
        BindThemeScope<WindowSettings> (config, "Window", activeTheme, s => WindowSettings.Current = s);
        BindThemeScope<GlyphSettings> (config, "Glyphs", activeTheme, s => GlyphSettings.Current = s);

        global::Terminal.Gui.Configuration.SchemeManager.ApplyFromConfiguration (config, activeTheme);
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
    private static void BindSection<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] T> (IConfiguration config, string sectionName, Action<T> apply) where T : new ()
    {
        T settings = new ();
        IConfigurationSection section = config.GetSection (sectionName);

        if (section.Exists ())
        {
            // Nested object format: { "Driver": { "Force16Colors": true } }
            section.Bind (settings);
            BindDirectProperties (section, settings);
        }

        // Flat dotted keys (e.g. RuntimeConfig `"Driver.Force16Colors": true`) overlay nested
        // library sections. MEC stores a literal dot; GetSection ("Driver") does not see them,
        // so they must always be applied after the nested Bind, not only when the section is missing.
        BindFlatDottedKeys (config, sectionName, settings);

        apply (settings);
    }

    /// <summary>
    ///     Two-pass overlay bind for ThemeScope POCOs. Binds the root section, then overlays
    ///     <c>Themes:<paramref name="activeTheme"/>:<paramref name="sectionName"/></c>. Properties not present in the
    ///     overlay JSON retain the root value (property-level merge — matches legacy CM <c>Scope.Apply</c> semantics).
    /// </summary>
    [UnconditionalSuppressMessage ("Trimming", "IL2026", Justification = "Settings POCOs are simple public types; Bind only walks declared properties.")]
    [UnconditionalSuppressMessage ("AOT", "IL3050", Justification = "Settings POCOs are simple types; no generic instantiation needed at runtime.")]
    private static void BindThemeScope<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] T> (IConfiguration config, string sectionName, string activeTheme, Action<T> apply) where T : new ()
    {
        T settings = new ();
        IConfigurationSection section = config.GetSection (sectionName);

        if (section.Exists ())
        {
            section.Bind (settings);
            BindDirectProperties (section, settings);
        }

        BindFlatDottedKeys (config, sectionName, settings);

        IConfigurationSection? themeObject = FindThemeObject (config, activeTheme);

        if (themeObject is not null)
        {
            IConfigurationSection overlay = themeObject.GetSection (sectionName);

            if (overlay.Exists ())
            {
                overlay.Bind (settings);
                BindDirectProperties (overlay, settings);
            }

            BindFlatDottedKeys (themeObject, sectionName, settings);
        }

        apply (settings);
    }

    /// <summary>
    ///     Resolves a theme object from either the nested dictionary shape
    ///     (<c>Themes:{name}</c>) or the legacy array-of-single-key-objects shape
    ///     (<c>Themes:{index}:{name}</c>).
    /// </summary>
    private static IConfigurationSection? FindThemeObject (IConfiguration config, string activeTheme)
    {
        if (string.IsNullOrEmpty (activeTheme))
        {
            return null;
        }

        IConfigurationSection themes = config.GetSection ("Themes");
        IConfigurationSection named = themes.GetSection (activeTheme);

        if (named.Exists ())
        {
            return named;
        }

        foreach (IConfigurationSection child in themes.GetChildren ())
        {
            IConfigurationSection candidate = child.GetSection (activeTheme);

            if (candidate.Exists ())
            {
                return candidate;
            }
        }

        return null;
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
    ///     Binds flat dotted keys (e.g. <c>Driver.Force16Colors</c>) from the configuration root to the
    ///     corresponding properties on the settings POCO. <typeparamref name="T"/>'s public properties are
    ///     preserved for trimming via the <see cref="DynamicallyAccessedMembersAttribute"/> on the type parameter.
    /// </summary>
    private static void BindFlatDottedKeys<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] T> (IConfiguration config, string sectionName, T settings)
    {
        string prefix = sectionName + ".";

        foreach (PropertyInfo prop in typeof (T).GetProperties (BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanWrite)
            {
                continue;
            }

            string? value = config [prefix + prop.Name];

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
    ///     and are skipped by <see cref="BindFlatDottedKeys{T}"/>.
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
            return value.Length > 0 ? new Rune (value [0]) : new Rune ('+');
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
