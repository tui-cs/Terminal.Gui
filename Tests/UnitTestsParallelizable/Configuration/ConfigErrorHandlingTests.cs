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
