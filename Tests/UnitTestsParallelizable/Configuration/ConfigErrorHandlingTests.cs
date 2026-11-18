// Claude - Fable 5
using Microsoft.Extensions.Configuration;
using Terminal.Gui.Configuration;

namespace ConfigurationTests;

/// <summary>
///     Malformed or legacy-shaped configuration sources must never crash the app (the build runs from a
///     module initializer). Errors are collected in <see cref="TuiJsonErrors"/> and printed at shutdown;
///     valid sources still apply.
/// </summary>
[Collection ("StaticSettingsTests")]
public class ConfigErrorHandlingTests
{
    [Fact]
    public void Build_MalformedRuntimeConfig_DoesNotThrow_AndCollectsError ()
    {
        TuiJsonErrors.Print ();
        TuiConfigurationBuilder tuiBuilder = new ();
        tuiBuilder.RuntimeConfig = "{ this is not json";

        IConfiguration config = tuiBuilder.Configuration;

        Assert.NotNull (config);
        Assert.Contains (TuiJsonErrors.GetErrors (), e => e.Contains ("RuntimeConfig"));
    }

    [Fact]
    public void ApplyToStaticFacades_MalformedRuntimeConfig_DoesNotThrow_OtherSourcesStillApply ()
    {
        using SettingsFacadeSnapshot snapshot = new ();
        TuiJsonErrors.Print ();
        TuiConfigurationBuilder tuiBuilder = new ();
        tuiBuilder.RuntimeConfig = "{ this is not json";

        tuiBuilder.ApplyToStaticFacades ();

        // The library's embedded config.json still applied.
        Assert.True (tuiBuilder.Configuration.GetSection ("Themes").Exists ());
    }

    [Fact]
    public void AddTuiRuntimeConfig_LegacyShape_IsSkipped ()
    {
        IConfiguration config = new ConfigurationBuilder ()
                                .AddTuiRuntimeConfig ("""{ "Button.DefaultShadow": "None" }""")
                                .Build ();

        Assert.Null (config ["Button.DefaultShadow"]);
    }

    [Fact]
    public void AddTuiInlineJson_Malformed_IsSkipped_AndCollectsError ()
    {
        TuiJsonErrors.Print ();

        IConfigurationBuilder builder = new ConfigurationBuilder ();
        TuiConfigurationExtensions.AddTuiInlineJson (builder, "{ this is not json", "TUI_CONFIG");
        IConfiguration config = builder.Build ();

        Assert.Empty (config.GetChildren ());
        Assert.Contains (TuiJsonErrors.GetErrors (), e => e.Contains ("TUI_CONFIG"));
    }

    [Fact]
    public void AddTuiInlineJson_LegacyShape_IsSkipped_AndWarns ()
    {
        IConfigurationBuilder builder = new ConfigurationBuilder ();
        TuiConfigurationExtensions.AddTuiInlineJson (builder, """{ "Themes": [ { "Dark": {} } ] }""", "TUI_CONFIG");
        IConfiguration config = builder.Build ();

        Assert.Empty (config.GetChildren ());
    }

    // Claude - Fable 5
    // A non-object root parses as valid JSON but would throw inside builder.Build(), past every
    // per-source handler — discarding ALL sources. It must be skipped at add time instead.
    [Fact]
    public void AddTuiInlineJson_NonObjectRoot_IsSkipped_AndCollectsError ()
    {
        TuiJsonErrors.Print ();

        IConfigurationBuilder builder = new ConfigurationBuilder ();
        TuiConfigurationExtensions.AddTuiInlineJson (builder, "[]", "TUI_CONFIG");
        IConfiguration config = builder.Build ();

        Assert.Empty (config.GetChildren ());
        Assert.Contains (TuiJsonErrors.GetErrors (), e => e.Contains ("TUI_CONFIG"));
    }

    // Claude - Fable 5
    // Duplicate top-level keys would throw inside MEC's parser at builder.Build(), past every
    // per-source handler; the source must be skipped at add time instead.
    [Fact]
    public void AddTuiInlineJson_DuplicateKeys_IsSkipped_AndCollectsError ()
    {
        TuiJsonErrors.Print ();

        IConfigurationBuilder builder = new ConfigurationBuilder ();
        TuiConfigurationExtensions.AddTuiInlineJson (builder, """{ "Theme": "A", "Theme": "B" }""", "TUI_CONFIG");
        IConfiguration config = builder.Build ();

        Assert.Empty (config.GetChildren ());
        Assert.Contains (TuiJsonErrors.GetErrors (), e => e.Contains ("TUI_CONFIG"));
    }

    // Claude - Fable 5
    [Fact]
    public void ApplyToStaticFacades_NonObjectRuntimeConfig_OtherSourcesStillApply ()
    {
        using SettingsFacadeSnapshot snapshot = new ();
        TuiJsonErrors.Print ();
        TuiConfigurationBuilder tuiBuilder = new (null, null, includeUserSources: false);
        tuiBuilder.RuntimeConfig = "[]";

        tuiBuilder.ApplyToStaticFacades ();

        // The library's embedded config.json still applied.
        Assert.True (tuiBuilder.Configuration.GetSection ("Themes").Exists ());
        Assert.Contains (TuiJsonErrors.GetErrors (), e => e.Contains ("RuntimeConfig"));
    }

    // Claude - Fable 5
    // Each build clears previously collected errors, so a watcher re-applying a persistently
    // malformed source reports it once per effective configuration, not once per rebuild.
    [Fact]
    public void Reload_PersistentlyMalformedSource_DoesNotAccumulateDuplicateErrors ()
    {
        using SettingsFacadeSnapshot snapshot = new ();
        TuiJsonErrors.Print ();
        TuiConfigurationBuilder tuiBuilder = new (null, null, includeUserSources: false);
        tuiBuilder.RuntimeConfig = "{ this is not json";

        _ = tuiBuilder.Configuration;
        tuiBuilder.Reload ();
        _ = tuiBuilder.Configuration;
        tuiBuilder.Reload ();
        _ = tuiBuilder.Configuration;

        Assert.Equal (1, TuiJsonErrors.GetErrors ().Count (e => e.Contains ("RuntimeConfig")));
    }

    // A theme whose name is numeric (e.g. "2077") is a legitimate nested MEC theme. Legacy array
    // shapes can no longer leak numeric section keys in, so numeric names must not be filtered out.
    [Fact]
    public void ThemeNames_NumericThemeName_IsListedAndSwitchable ()
    {
        using SettingsFacadeSnapshot snapshot = new ();
        TuiConfigurationBuilder tuiBuilder = new ();

        tuiBuilder.RuntimeConfig = """
                                   {
                                     "Themes": { "2077": { "Button": { "DefaultShadow": "None" } } }
                                   }
                                   """;
        tuiBuilder.ApplyToStaticFacades ();

        MecThemeManager manager = new (tuiBuilder);

        Assert.Contains ("2077", manager.ThemeNames);
        Assert.True (manager.SwitchTheme ("2077"));
        Assert.Equal (ShadowStyles.None, ButtonSettings.Current.DefaultShadow);
    }
}
