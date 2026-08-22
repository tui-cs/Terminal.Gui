// Claude - Opus 4.8
// Grok - grok-4.6
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Terminal.Gui.Configuration;
using Terminal.Gui.Input;
using Terminal.Gui.Tools.MigrateConfig;

namespace ConfigurationTests;

/// <summary>
///     Nested MEC binding is the supported contract. Flat dotted keys are a pre-MEC shape:
///     they do not bind until converted with <see cref="ConfigJsonMigrator"/>.
/// </summary>
[Collection ("StaticSettingsTests")]
public class DottedKeyTests
{
    [Fact]
    public void JsonProvider_DotInKey_DoesNotCreateSection ()
    {
        string json = """{ "Driver.Force16Colors": true }""";

        IConfiguration config = new ConfigurationBuilder ()
                                .AddTuiRuntimeConfig (json)
                                .Build ();

        IConfigurationSection section = config.GetSection ("Driver");

        Assert.False (section.Exists (), "MEC JSON does not auto-split dotted keys into sections");
    }

    [Fact]
    public void ApplyToStaticFacades_DoesNotBindFlatDottedKeys ()
    {
        using SettingsFacadeSnapshot snapshot = new ();

        TuiConfigurationBuilder builder = new ();
        builder.RuntimeConfig = """
                                {
                                  "Driver.Force16Colors": true,
                                  "Application.IsMouseDisabled": true,
                                  "Key.Separator": "-",
                                  "PopoverMenu.DefaultKey": "Ctrl+P"
                                }
                                """;
        builder.ApplyToStaticFacades ();

        Assert.False (DriverSettings.Defaults.Force16Colors);
        Assert.False (ApplicationSettings.Defaults.IsMouseDisabled);
        Assert.NotEqual (new Rune ('-'), KeySettings.Defaults.Separator);
        Assert.NotEqual (Key.P.WithCtrl, PopoverMenu.DefaultKey);
    }

    [Fact]
    public void ApplyToStaticFacades_BindsScalarThemeKey ()
    {
        using SettingsFacadeSnapshot snapshot = new ();

        TuiConfigurationBuilder builder = new ();
        builder.RuntimeConfig = """{ "Theme": "Dark" }""";
        builder.ApplyToStaticFacades ();

        Assert.Equal ("Dark", ThemeSettings.Defaults.Theme);
    }

    [Fact]
    public void ApplyToStaticFacades_BindsNestedSectionFormat ()
    {
        using SettingsFacadeSnapshot snapshot = new ();

        TuiConfigurationBuilder builder = new ();
        builder.RuntimeConfig = """{ "Driver": { "Force16Colors": true } }""";
        builder.ApplyToStaticFacades ();

        Assert.True (DriverSettings.Defaults.Force16Colors);
    }

    [Fact]
    public void ApplyToStaticFacades_BindsNestedKeyTypedProperty ()
    {
        using SettingsFacadeSnapshot snapshot = new ();

        TuiConfigurationBuilder builder = new ();
        builder.RuntimeConfig = """{ "PopoverMenu": { "DefaultKey": "Ctrl+P" } }""";
        builder.ApplyToStaticFacades ();

        Assert.Equal (Key.P.WithCtrl, PopoverMenu.DefaultKey);
    }

    [Fact]
    public void ApplyToStaticFacades_BindsRuneViaJsonConverterFormats ()
    {
        using SettingsFacadeSnapshot snapshot = new ();

        TuiConfigurationBuilder builder = new ();
        builder.RuntimeConfig = """{ "Key": { "Separator": "U+002D" } }""";
        builder.ApplyToStaticFacades ();

        Assert.Equal (new Rune ('-'), KeySettings.Defaults.Separator);
    }

    [Fact]
    public void MigratedDottedJson_BindsAfterMigrateObject ()
    {
        using SettingsFacadeSnapshot snapshot = new ();

        string dotted = """
                        {
                          "Driver.Force16Colors": true,
                          "Key.Separator": "-"
                        }
                        """;
        JsonObject migrated = ConfigJsonMigrator.MigrateObject (JsonNode.Parse (dotted)!.AsObject ());

        TuiConfigurationBuilder builder = new ();
        builder.RuntimeConfig = migrated.ToJsonString ();
        builder.ApplyToStaticFacades ();

        Assert.True (DriverSettings.Defaults.Force16Colors);
        Assert.Equal (new Rune ('-'), KeySettings.Defaults.Separator);
    }
}
