// Claude - Fable 5
using Terminal.Gui.Configuration;

namespace ConfigurationTests;

/// <summary>
///     <see cref="MenuBar.DefaultKey"/> has a public setter (as does <see cref="PopoverMenu.DefaultKey"/>).
///     An app-set value must survive theme switches and configuration re-application, matching the legacy
///     SettingsScope behavior where theme switches never reset it.
/// </summary>
[Collection ("StaticSettingsTests")]
public class MenuBarDefaultKeyTests
{
    [Fact]
    public void DefaultKey_SetByApp_SurvivesThemeOverlayReapply ()
    {
        using SettingsFacadeSnapshot snapshot = new ();

        try
        {
            MenuBar.DefaultKey = Key.F12;

            TuiConfigurationBuilder tuiBuilder = new ();
            tuiBuilder.RuntimeConfig = """{ "Theme": "Dark", "Themes": { "Dark": {} } }""";
            tuiBuilder.ApplyToStaticFacades ();

            Assert.Equal (Key.F12, MenuBar.DefaultKey);
        }
        finally
        {
            MenuBar.ResetDefaultKeyOverride ();
        }
    }

    [Fact]
    public void DefaultKey_SetByApp_SurvivesThemeSwitch ()
    {
        using SettingsFacadeSnapshot snapshot = new ();

        try
        {
            MenuBar.DefaultKey = Key.F12;

            TuiConfigurationBuilder tuiBuilder = new ();
            tuiBuilder.RuntimeConfig = """{ "Themes": { "Dark": {} } }""";
            tuiBuilder.ApplyToStaticFacades ();

            MecThemeManager manager = new (tuiBuilder);
            Assert.True (manager.SwitchTheme ("Dark"));

            Assert.Equal (Key.F12, MenuBar.DefaultKey);
        }
        finally
        {
            MenuBar.ResetDefaultKeyOverride ();
        }
    }

    [Fact]
    public void DefaultKey_NotSetByApp_ComesFromConfiguration ()
    {
        using SettingsFacadeSnapshot snapshot = new ();

        MenuBar.ResetDefaultKeyOverride ();
        TuiConfigurationBuilder tuiBuilder = new ();
        tuiBuilder.RuntimeConfig = """{ "MenuBar": { "DefaultKey": "F11" } }""";
        tuiBuilder.ApplyToStaticFacades ();

        Assert.Equal (Key.F11, MenuBar.DefaultKey);
    }
}
