// Codex - GPT-5
using Terminal.Gui.Configuration;

namespace ConfigurationTests;

[Collection ("StaticSettingsTests")]
public class MenuBorderStyleTests
{
    [Fact]
    public void MenuBar_EndInit_PreservesThemeDefaultBorderStyle ()
    {
        using SettingsFacadeSnapshot snapshot = new ();
        string previousTheme = ThemeManager.GetCurrentThemeName ();
        TuiConfigurationBuilder tuiBuilder = new ();

        try
        {
            tuiBuilder.RuntimeConfig = """
                                       {
                                         "Theme": "Test",
                                         "Themes": {
                                           "Test": {
                                             "MenuBar": {
                                               "DefaultBorderStyle": "Rounded"
                                             }
                                           }
                                         }
                                       }
                                       """;
            tuiBuilder.ApplyToStaticFacades ();

            using MenuBar menuBar = new ();
            menuBar.EndInit ();

            Assert.Equal (LineStyle.Rounded, menuBar.BorderStyle);
        }
        finally
        {
            tuiBuilder.ThemeManager.SwitchTheme (previousTheme);
        }
    }
}
