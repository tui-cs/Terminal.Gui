// Claude - Fable 5
using Terminal.Gui.Configuration;

namespace ConfigurationTests;

/// <summary>
///     <see cref="PopoverMenu.DefaultKey"/> has a public setter, like <see cref="MenuBar.DefaultKey"/>.
///     An app-set value must survive theme switches and configuration re-application
///     (<see cref="PopoverMenuSettings.Current"/> is wholesale-replaced on each apply).
/// </summary>
[Collection ("StaticSettingsTests")]
public class PopoverMenuDefaultKeyTests
{
    [Fact]
    public void DefaultKey_SetByApp_SurvivesThemeOverlayReapply ()
    {
        using SettingsFacadeSnapshot snapshot = new ();

        try
        {
            PopoverMenu.DefaultKey = Key.F9;

            TuiConfigurationBuilder tuiBuilder = new ();
            tuiBuilder.RuntimeConfig = """{ "Theme": "Dark", "Themes": { "Dark": {} } }""";
            tuiBuilder.ApplyToStaticFacades ();

            Assert.Equal (Key.F9, PopoverMenu.DefaultKey);
        }
        finally
        {
            PopoverMenu.ResetDefaultKeyOverride ();
        }
    }

    [Fact]
    public void DefaultKey_SetByApp_SurvivesThemeSwitch ()
    {
        using SettingsFacadeSnapshot snapshot = new ();

        try
        {
            PopoverMenu.DefaultKey = Key.F9;

            TuiConfigurationBuilder tuiBuilder = new ();
            tuiBuilder.RuntimeConfig = """{ "Themes": { "Dark": {} } }""";
            tuiBuilder.ApplyToStaticFacades ();

            MecThemeManager manager = new (tuiBuilder);
            Assert.True (manager.SwitchTheme ("Dark"));

            Assert.Equal (Key.F9, PopoverMenu.DefaultKey);
        }
        finally
        {
            PopoverMenu.ResetDefaultKeyOverride ();
        }
    }

    [Fact]
    public void DefaultKey_NotSetByApp_ComesFromConfiguration ()
    {
        using SettingsFacadeSnapshot snapshot = new ();

        PopoverMenu.ResetDefaultKeyOverride ();
        TuiConfigurationBuilder tuiBuilder = new ();
        tuiBuilder.RuntimeConfig = """{ "PopoverMenu": { "DefaultKey": "F11" } }""";
        tuiBuilder.ApplyToStaticFacades ();

        Assert.Equal (Key.F11, PopoverMenu.DefaultKey);
    }
}
