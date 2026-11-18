// Copilot - Claude Opus 4.7
// Grok - grok-4.6

using Terminal.Gui.Configuration;
using Terminal.Gui.Drawing;

namespace ConfigurationTests;

/// <summary>
///     End-to-end tests for the A2.1 two-pass MEC theme overlay applied through
///     <see cref="TuiConfigurationBuilder.ApplyToStaticFacades"/>.
/// </summary>
/// <remarks>
///     <para>
///         The contract under test: <c>BindThemeScope&lt;T&gt;</c> binds the root section first, then overlays
///         <c>Themes:{active}:{section}</c>. Properties present only in the root must survive; properties present
///         in the overlay must win. This mirrors legacy CM <c>Scope.Apply</c> property-level merge semantics.
///     </para>
/// </remarks>
[Collection ("StaticSettingsTests")]
public class ThemeOverlayMergeTests
{
    /// <summary>
    ///     When the theme overlay only mentions one property of a ThemeScope POCO, the other properties keep their
    ///     root-section values (not the compile-time defaults, not <see langword="null"/>).
    /// </summary>
    [Fact]
    public void ApplyToStaticFacades_ThemeOverlay_PreservesRootDefaultsForUnmentionedProperties ()
    {
        using SettingsFacadeSnapshot snapshot = new ();
        TuiConfigurationBuilder tuiBuilder = new ();

        tuiBuilder.RuntimeConfig = """
                                   {
                                     "Theme": "Custom",
                                     "Dialog": {
                                       "DefaultShadow": "Opaque",
                                       "DefaultBorderStyle": "Double",
                                       "DefaultButtonAlignment": "Start"
                                     },
                                     "Themes": {
                                       "Custom": {
                                         "Dialog": {
                                           "DefaultBorderStyle": "Single"
                                         }
                                       }
                                     }
                                   }
                                   """;

        tuiBuilder.ApplyToStaticFacades ();

        Assert.Equal (LineStyle.Single, DialogSettings.Current.DefaultBorderStyle);
        Assert.Equal (ShadowStyles.Opaque, DialogSettings.Current.DefaultShadow);
        Assert.Equal (Alignment.Start, DialogSettings.Current.DefaultButtonAlignment);
    }

    /// <summary>
    ///     When no theme overlay exists for a POCO, the root section's values are applied verbatim.
    /// </summary>
    [Fact]
    public void ApplyToStaticFacades_NoOverlay_UsesRootValuesAsIs ()
    {
        using SettingsFacadeSnapshot snapshot = new ();
        TuiConfigurationBuilder tuiBuilder = new ();

        tuiBuilder.RuntimeConfig = """
                                   {
                                     "Theme": "Custom",
                                     "Button": {
                                       "DefaultShadow": "None"
                                     },
                                     "Themes": { "Custom": { } }
                                   }
                                   """;

        tuiBuilder.ApplyToStaticFacades ();

        Assert.Equal (ShadowStyles.None, ButtonSettings.Current.DefaultShadow);
    }

    /// <summary>
    ///     The atomic-swap pattern produces a new <see cref="ButtonSettings"/> reference on each apply, never
    ///     mutates the existing instance in place. A reader that captured the prior reference still sees the prior
    ///     values.
    /// </summary>
    [Fact]
    public void ApplyToStaticFacades_AtomicSwap_DoesNotMutatePriorReference ()
    {
        using SettingsFacadeSnapshot snapshot = new ();
        TuiConfigurationBuilder tuiBuilder = new ();
        tuiBuilder.RuntimeConfig = """{ "Button": { "DefaultShadow": "Transparent" } }""";
        tuiBuilder.ApplyToStaticFacades ();

        ButtonSettings captured = ButtonSettings.Current;
        Assert.Equal (ShadowStyles.Transparent, captured.DefaultShadow);

        tuiBuilder.RuntimeConfig = """{ "Button": { "DefaultShadow": "None" } }""";
        tuiBuilder.ApplyToStaticFacades ();

        Assert.Equal (ShadowStyles.Transparent, captured.DefaultShadow);
        Assert.Equal (ShadowStyles.None, ButtonSettings.Current.DefaultShadow);
        Assert.NotSame (captured, ButtonSettings.Current);
    }

    // Grok - grok-4.6
    [Fact]
    public void SwitchTheme_AppliesOverlay_DoesNotResetActiveThemeToConfigDefault ()
    {
        using SettingsFacadeSnapshot snapshot = new ();
        TuiConfigurationBuilder tuiBuilder = new ();

        tuiBuilder.RuntimeConfig = """
                                   {
                                     "Theme": "Default",
                                     "Button": { "DefaultShadow": "Opaque" },
                                     "Glyphs": { "CheckStateChecked": "☑" },
                                     "Themes": {
                                       "EightBit": {
                                         "Button": { "DefaultShadow": "None" },
                                         "Glyphs": { "CheckStateChecked": "X" }
                                       }
                                     }
                                   }
                                   """;

        tuiBuilder.ApplyToStaticFacades ();
        Assert.Equal (ShadowStyles.Opaque, ButtonSettings.Current.DefaultShadow);

        MecThemeManager manager = new (tuiBuilder);
        Assert.True (manager.SwitchTheme ("EightBit"));
        Assert.Equal ("EightBit", manager.CurrentThemeName);
        Assert.Equal (ShadowStyles.None, ButtonSettings.Current.DefaultShadow);
        Assert.Equal ((System.Text.Rune)'X', Glyphs.CheckStateChecked);
    }

    // Grok - grok-4.6
    [Fact]
    public void SwitchTheme_ThemeChangedHandler_SeesNewOverlay ()
    {
        using SettingsFacadeSnapshot snapshot = new ();
        TuiConfigurationBuilder tuiBuilder = new ();

        tuiBuilder.RuntimeConfig = """
                                   {
                                     "Theme": "Default",
                                     "Button": { "DefaultShadow": "Opaque" },
                                     "Themes": {
                                       "EightBit": {
                                         "Button": { "DefaultShadow": "None" }
                                       }
                                     }
                                   }
                                   """;

        tuiBuilder.ApplyToStaticFacades ();

        ShadowStyles seenInHandler = ShadowStyles.Opaque;
        MecThemeManager manager = new (tuiBuilder);
        manager.ThemeChanged += (_, _) => seenInHandler = ButtonSettings.Current.DefaultShadow;

        Assert.True (manager.SwitchTheme ("EightBit"));
        Assert.Equal (ShadowStyles.None, seenInHandler);
    }

    // Grok - grok-4.6
    [Fact]
    public void SwitchTheme_IgnoresThemeNameCase ()
    {
        using SettingsFacadeSnapshot snapshot = new ();
        TuiConfigurationBuilder tuiBuilder = new ();

        tuiBuilder.RuntimeConfig = """
                                   {
                                     "Theme": "Default",
                                     "Themes": { "Dark": { "Button": { "DefaultShadow": "None" } } }
                                   }
                                   """;

        tuiBuilder.ApplyToStaticFacades ();
        MecThemeManager manager = new (tuiBuilder);

        Assert.True (manager.SwitchTheme ("dark"));
        Assert.Equal (ShadowStyles.None, ButtonSettings.Current.DefaultShadow);
    }

    // Claude - Fable 5
    [Fact]
    public void ApplyToStaticFacades_ConfigChangesActiveTheme_RaisesThemeChanged ()
    {
        using SettingsFacadeSnapshot snapshot = new ();
        TuiConfigurationBuilder tuiBuilder = new ();

        tuiBuilder.RuntimeConfig = """
                                   {
                                     "Theme": "A",
                                     "Themes": { "A": {}, "B": {} }
                                   }
                                   """;

        tuiBuilder.ApplyToStaticFacades ();

        MecThemeManager manager = new (tuiBuilder);
        Assert.True (manager.SwitchTheme ("B"));
        Assert.Equal ("B", manager.CurrentThemeName);

        string? raisedTheme = null;
        manager.ThemeChanged += Handler;

        try
        {
            // Re-applying configuration snaps the theme back to "A"; subscribers must be told.
            tuiBuilder.ApplyToStaticFacades ();
        }
        finally
        {
            manager.ThemeChanged -= Handler;
        }

        Assert.Equal ("A", manager.CurrentThemeName);
        Assert.Equal ("A", raisedTheme);

        void Handler (object? sender, EventArgs<string> e) { raisedTheme = e.Value; }
    }

    // Claude - Fable 5
    // Re-applying configuration re-publishes every facade wholesale, so subscribers must be notified
    // even when the theme NAME is unchanged — a hot reload that edits the current theme's colors
    // would otherwise update state without anything repainting.
    [Fact]
    public void ApplyToStaticFacades_ActiveThemeUnchanged_StillRaisesThemeChanged ()
    {
        using SettingsFacadeSnapshot snapshot = new ();
        TuiConfigurationBuilder tuiBuilder = new ();

        tuiBuilder.RuntimeConfig = """
                                   {
                                     "Theme": "A",
                                     "Themes": { "A": {} }
                                   }
                                   """;

        tuiBuilder.ApplyToStaticFacades ();

        string? raisedTheme = null;
        MecThemeManager manager = new (tuiBuilder);
        manager.ThemeChanged += Handler;

        try
        {
            tuiBuilder.ApplyToStaticFacades ();
        }
        finally
        {
            manager.ThemeChanged -= Handler;
        }

        Assert.Equal ("A", raisedTheme);

        void Handler (object? sender, EventArgs<string> e) { raisedTheme = e.Value; }
    }

    // Grok - grok-4.6
    [Fact]
    public void ApplyToStaticFacades_LegacyArrayThemeShape_IsNotApplied ()
    {
        using SettingsFacadeSnapshot snapshot = new ();
        TuiConfigurationBuilder tuiBuilder = new ();

        tuiBuilder.RuntimeConfig = """
                                   {
                                     "Theme": "EightBit",
                                     "Glyphs": { "CheckStateChecked": "☑" },
                                     "Themes": [
                                       {
                                         "EightBit": {
                                           "Glyphs.CheckStateChecked": "X",
                                           "Button.DefaultShadow": "None"
                                         }
                                       }
                                     ]
                                   }
                                   """;

        tuiBuilder.ApplyToStaticFacades ();

        Assert.Equal ((System.Text.Rune)'☑', Glyphs.CheckStateChecked);
        Assert.NotEqual (ShadowStyles.None, ButtonSettings.Current.DefaultShadow);
    }
}
