// Copilot

using Terminal.Gui.App;
using Terminal.Gui.Configuration;

namespace ConfigurationTests;

/// <summary>Tests for the MEC-backed theme and scheme manager interfaces.</summary>
[Collection ("StaticSettingsTests")]
public class MecThemeTests
{
    [Fact]
    public void ThemeSettings_Defaults_HasDefaultThemeName ()
    {
        Assert.Equal ("Default", ThemeSettings.Defaults.Theme);
    }

    [Fact]
    public void MecThemeManager_CurrentThemeName_ReturnsDefault ()
    {
        TuiConfigurationBuilder builder = new ();
        MecThemeManager manager = new (builder);
        Assert.Equal ("Default", manager.CurrentThemeName);
    }

    [Fact]
    public void MecThemeManager_ThemeNames_ContainsDefault ()
    {
        TuiConfigurationBuilder builder = new ();
        MecThemeManager manager = new (builder);
        Assert.Contains ("Default", manager.ThemeNames);
    }

    [Fact]
    public void MecThemeManager_SwitchTheme_NullOrEmpty_ReturnsFalse ()
    {
        TuiConfigurationBuilder builder = new ();
        MecThemeManager manager = new (builder);

        Assert.False (manager.SwitchTheme (""));
        Assert.False (manager.SwitchTheme (null!));
    }

    [Fact]
    public void MecThemeManager_SwitchTheme_NonExistent_ReturnsFalse ()
    {
        TuiConfigurationBuilder builder = new ();
        MecThemeManager manager = new (builder);

        // A non-existent theme name should return false without corrupting state
        bool result = manager.SwitchTheme ("NonExistent");
        Assert.False (result);
    }

    [Fact]
    public void MecSchemeManager_SchemeNames_DoesNotThrow ()
    {
        MecSchemeManager manager = new ();
        IReadOnlyList<string> names = manager.SchemeNames;

        // Should return a list without throwing (may be empty if CM state is odd in parallel tests)
        Assert.NotNull (names);
    }

    [Fact]
    public void MecSchemeManager_GetScheme_ReturnsNull_ForInvalid ()
    {
        MecSchemeManager manager = new ();
        Scheme? scheme = manager.GetScheme ("NonExistentScheme12345");
        Assert.Null (scheme);
    }

    [Fact]
    public void IThemeManager_Interface_IsImplementedByMecThemeManager ()
    {
        TuiConfigurationBuilder builder = new ();
        IThemeManager manager = builder.ThemeManager;
        Assert.IsType<MecThemeManager> (manager);
    }

    [Fact]
    public void ISchemeManager_Interface_IsImplementedByMecSchemeManager ()
    {
        TuiConfigurationBuilder builder = new ();
        ISchemeManager manager = builder.SchemeManager;
        Assert.IsType<MecSchemeManager> (manager);
    }

    // Copilot - Claude Opus 4.7
    [Fact]
    public void IThemeManager_ThemeChanged_EventCanBeSubscribedAndUnsubscribed ()
    {
        TuiConfigurationBuilder builder = new ();
        IThemeManager manager = builder.ThemeManager;
        bool fired = false;
        EventHandler<EventArgs<string>> handler = (_, _) => fired = true;

        manager.ThemeChanged += handler;
        manager.ThemeChanged -= handler;

        Assert.False (fired);
    }

    // Copilot - Claude Opus 4.7
    [Fact]
    public void ThemeChanges_ThemeChanged_EventCanBeSubscribedAndUnsubscribed ()
    {
        bool fired = false;
        EventHandler<EventArgs<string>> handler = (_, _) => fired = true;

        ThemeChanges.ThemeChanged += handler;
        ThemeChanges.ThemeChanged -= handler;

        Assert.False (fired);
    }

    // Copilot - Claude Opus 4.7
    [Fact]
    public void ThemeChanges_ThemeChanged_StaticClassIsPublic ()
    {
        // Phase B contract: views can reach a static facade without DI.
        Type t = typeof (ThemeChanges);
        Assert.True (t.IsAbstract && t.IsSealed, "ThemeChanges must be a static class");
        Assert.NotNull (t.GetEvent ("ThemeChanged"));
    }
}
