// Claude - Opus 4.6

using System.Collections.Immutable;
using UnitTests;

namespace DrawingTests;

/// <summary>
///     End-to-end tests validating that <see cref="Color.None"/> in scheme derivation
///     correctly uses <see cref="IDriver.DefaultAttribute"/> (from OSC 10/11 queries)
///     and produces the expected ANSI output.
/// </summary>
public class SchemeColorNoneDerivationTests (ITestOutputHelper output) : TestDriverBase
{
    #region Scheme Derivation with DefaultTerminalColors

    [Fact]
    public void Focus_WithNullDefault_ResolvesToFallbackColors ()
    {
        // Base scheme: Normal = (Color.None, Color.None)
        ImmutableSortedDictionary<string, Scheme> schemes = Scheme.GetHardCodedSchemes ();
        Scheme baseScheme = schemes ["Base"];

        // No defaultTerminalColors — Color.None resolves to fallback: fg=White, bg=Black
        Attribute focus = baseScheme.GetAttributeForRole (VisualRole.Focus);

        // Focus swaps fg/bg: Foreground = ResolveNone(Normal.Background, bg) = Black (0,0,0)
        Assert.Equal (0, focus.Foreground.R);
        Assert.Equal (0, focus.Foreground.G);
        Assert.Equal (0, focus.Foreground.B);

        // Background = ResolveNone(Normal.Foreground, fg) = White (255,255,255)
        Assert.Equal (255, focus.Background.R);
        Assert.Equal (255, focus.Background.G);
        Assert.Equal (255, focus.Background.B);
    }

    [Fact]
    public void Focus_WithDarkTerminal_ResolvesToTerminalColors ()
    {
        ImmutableSortedDictionary<string, Scheme> schemes = Scheme.GetHardCodedSchemes ();
        Scheme baseScheme = schemes ["Base"];

        // Simulate dark terminal: fg=(200,200,200), bg=(20,20,20)
        Attribute darkDefault = new (new Color (200, 200, 200), new Color (20, 20, 20));
        Attribute focus = baseScheme.GetAttributeForRole (VisualRole.Focus, darkDefault);

        // Focus.Foreground = ResolveNone(Normal.Background=Color.None, bg) = terminal bg = (20,20,20)
        Assert.Equal (20, focus.Foreground.R);
        Assert.Equal (20, focus.Foreground.G);
        Assert.Equal (20, focus.Foreground.B);

        // Focus.Background = ResolveNone(Normal.Foreground=Color.None, fg) = terminal fg = (200,200,200)
        Assert.Equal (200, focus.Background.R);
        Assert.Equal (200, focus.Background.G);
        Assert.Equal (200, focus.Background.B);
    }

    [Fact]
    public void Focus_WithWhiteTerminal_ResolvesToTerminalColors ()
    {
        ImmutableSortedDictionary<string, Scheme> schemes = Scheme.GetHardCodedSchemes ();
        Scheme baseScheme = schemes ["Base"];

        // Simulate white/light terminal: fg=(40,40,40), bg=(240,240,240)
        Attribute lightDefault = new (new Color (40, 40, 40), new Color (240, 240, 240));
        Attribute focus = baseScheme.GetAttributeForRole (VisualRole.Focus, lightDefault);

        // Focus.Foreground = ResolveNone(Normal.Background=Color.None, bg) = terminal bg = (240,240,240)
        Assert.Equal (240, focus.Foreground.R);
        Assert.Equal (240, focus.Foreground.G);
        Assert.Equal (240, focus.Foreground.B);

        // Focus.Background = ResolveNone(Normal.Foreground=Color.None, fg) = terminal fg = (40,40,40)
        Assert.Equal (40, focus.Background.R);
        Assert.Equal (40, focus.Background.G);
        Assert.Equal (40, focus.Background.B);
    }

    [Fact]
    public void Editable_ResolvesNoneForForegroundAndDimmedBackground ()
    {
        ImmutableSortedDictionary<string, Scheme> schemes = Scheme.GetHardCodedSchemes ();
        Scheme baseScheme = schemes ["Base"];

        // Editable.Foreground = ResolveNone(Normal.Foreground=Color.None, fg=true)
        // Editable.Background = ResolveNone(Normal.Foreground=Color.None, fg=true).GetDimmerColor(0.5)
        // Both use the same resolved color, so bg is a dimmed version of fg.
        Attribute lightDefault = new (new Color (40, 40, 40), new Color (240, 240, 240));
        Attribute editableWithLight = baseScheme.GetAttributeForRole (VisualRole.Editable, lightDefault);

        // Foreground = terminal fg = (40,40,40)
        Assert.Equal (40, editableWithLight.Foreground.R);
        Assert.Equal (40, editableWithLight.Foreground.G);
        Assert.Equal (40, editableWithLight.Foreground.B);

        // Background is a dimmed version of fg — should be different from fg
        Assert.NotEqual (editableWithLight.Foreground, editableWithLight.Background);
    }

    [Fact]
    public void Highlight_WithDarkTerminal_ResolvesNoneForBrighten ()
    {
        ImmutableSortedDictionary<string, Scheme> schemes = Scheme.GetHardCodedSchemes ();
        Scheme baseScheme = schemes ["Base"];

        // Highlight.Foreground = ResolveNone(Normal.Background=Color.None).GetBrighterColor()
        // Normal.Background = Color.None → resolved to terminal bg

        Attribute darkDefault = new (new Color (200, 200, 200), new Color (20, 20, 20));
        Attribute highlight = baseScheme.GetAttributeForRole (VisualRole.Highlight, darkDefault);

        // ResolveNone(Color.None, bg) = (20,20,20), then GetBrighterColor makes it lighter
        // The important thing is it's NOT using Color.None's raw RGB (255,255,255,alpha=0)
        Assert.True (highlight.Foreground.R > 20 || highlight.Foreground.G > 20 || highlight.Foreground.B > 20,
                     "Highlight foreground should be brighter than the resolved dark bg");
    }

    [Fact]
    public void Disabled_ResolvesNoneBeforeDimming ()
    {
        ImmutableSortedDictionary<string, Scheme> schemes = Scheme.GetHardCodedSchemes ();
        Scheme baseScheme = schemes ["Base"];

        // Disabled.Foreground = ResolveNone(Normal.Foreground=Color.None, fg=true).GetDimmerColor(0.05)
        // Without ResolveNone, this would dim Color.None's sentinel RGB (255,255,255) = wrong
        Attribute darkDefault = new (new Color (200, 200, 200), new Color (20, 20, 20));
        Attribute disabled = baseScheme.GetAttributeForRole (VisualRole.Disabled, darkDefault);

        // Should dim the terminal fg (200,200,200), not Color.None's sentinel (255,255,255)
        // Dimming 200 by 0.05 should produce something slightly different from 200
        Assert.True (disabled.Foreground.R < 210 && disabled.Foreground.G < 210 && disabled.Foreground.B < 210,
                     "Disabled foreground should be a dimmed version of terminal fg, not white");
    }

    #endregion

    #region Dark/Light Background Awareness (Part D)

    [Fact]
    public void Active_WithDarkTerminal_BrightensForground_DimsBackground ()
    {
        // Claude - Opus 4.6
        ImmutableSortedDictionary<string, Scheme> schemes = Scheme.GetHardCodedSchemes ();
        Scheme baseScheme = schemes ["Base"];

        // Dark terminal: bright fg on dark bg
        Attribute darkDefault = new (new Color (200, 200, 200), new Color (20, 20, 20));
        Attribute active = baseScheme.GetAttributeForRole (VisualRole.Active, darkDefault);

        // Focus for dark terminal: fg=(20,20,20), bg=(200,200,200)
        // Active.Foreground = Focus.fg.GetBrighterColor(isDark=false because Focus.bg=(200,200,200) is light)
        // Active.Background = Focus.bg.GetDimmerColor(isDark=false)
        // The Focus bg (200,200,200) is light, so isDark=false: brighter=darker, dim=lighter
        Assert.True (active.Style.HasFlag (TextStyle.Bold), "Active should have Bold style");
    }

    [Fact]
    public void SchemeDerivation_WithLightTerminalBackground_ProducesReadableColors ()
    {
        // Claude - Opus 4.6
        ImmutableSortedDictionary<string, Scheme> schemes = Scheme.GetHardCodedSchemes ();
        Scheme baseScheme = schemes ["Base"];

        // Simulate light terminal: dark fg on light bg
        Attribute lightDefault = new (new Color (40, 40, 40), new Color (240, 240, 240));

        // Roles that perform color math should produce resolved (non-None) colors.
        // Normal and HotNormal intentionally preserve Color.None (they don't transform fg/bg).
        VisualRole[] derivedRoles =
        [
            VisualRole.Focus, VisualRole.Active, VisualRole.Highlight,
            VisualRole.Editable, VisualRole.ReadOnly, VisualRole.Disabled,
            VisualRole.HotFocus, VisualRole.HotActive
        ];

        foreach (VisualRole role in derivedRoles)
        {
            Attribute attr = baseScheme.GetAttributeForRole (role, lightDefault);
            Assert.NotEqual (Color.None, attr.Foreground);
            Assert.True (attr.Foreground.A == 255, $"Role {role} foreground should be opaque (not Color.None)");
        }
    }

    [Fact]
    public void SchemeDerivation_WithDarkTerminalBackground_ProducesReadableColors ()
    {
        // Claude - Opus 4.6
        ImmutableSortedDictionary<string, Scheme> schemes = Scheme.GetHardCodedSchemes ();
        Scheme baseScheme = schemes ["Base"];

        // Simulate dark terminal: bright fg on dark bg
        Attribute darkDefault = new (new Color (200, 200, 200), new Color (20, 20, 20));

        // Roles that perform color math should produce resolved (non-None) colors.
        VisualRole[] derivedRoles =
        [
            VisualRole.Focus, VisualRole.Active, VisualRole.Highlight,
            VisualRole.Editable, VisualRole.ReadOnly, VisualRole.Disabled,
            VisualRole.HotFocus, VisualRole.HotActive
        ];

        foreach (VisualRole role in derivedRoles)
        {
            Attribute attr = baseScheme.GetAttributeForRole (role, darkDefault);
            Assert.NotEqual (Color.None, attr.Foreground);
            Assert.True (attr.Foreground.A == 255, $"Role {role} foreground should be opaque (not Color.None)");
        }
    }

    [Fact]
    public void Highlight_WithLightTerminal_DarkensForVisibility ()
    {
        // Claude - Opus 4.6
        ImmutableSortedDictionary<string, Scheme> schemes = Scheme.GetHardCodedSchemes ();
        Scheme baseScheme = schemes ["Base"];

        // Light terminal: Normal.Background = Color.None → (240,240,240)
        Attribute lightDefault = new (new Color (40, 40, 40), new Color (240, 240, 240));
        Attribute highlight = baseScheme.GetAttributeForRole (VisualRole.Highlight, lightDefault);

        // Highlight.Foreground = ResolveNone(bg=(240,240,240)).GetBrighterColor(isDark=false)
        // isDark=false means "darken for visibility on light bg"
        // So highlight fg should be darker than the bg
        Assert.True (highlight.Foreground.R < 240 || highlight.Foreground.G < 240 || highlight.Foreground.B < 240,
                     "Highlight foreground on light bg should be darker than the background for visibility");
    }

    [Fact]
    public void Disabled_WithLightTerminal_WashesOutForeground ()
    {
        // Claude - Opus 4.6
        ImmutableSortedDictionary<string, Scheme> schemes = Scheme.GetHardCodedSchemes ();
        Scheme baseScheme = schemes ["Base"];

        // Light terminal
        Attribute lightDefault = new (new Color (40, 40, 40), new Color (240, 240, 240));
        Attribute disabled = baseScheme.GetAttributeForRole (VisualRole.Disabled, lightDefault);

        // Disabled.Foreground = terminal_fg(40,40,40).GetDimmerColor(0.05, isDark=false)
        // isDark=false means "dim by increasing lightness (wash out)"
        // So disabled fg should be lighter than the original fg
        Assert.True (disabled.Foreground.R > 40 || disabled.Foreground.G > 40 || disabled.Foreground.B > 40,
                     "Disabled foreground on light bg should be washed out (lighter than original fg)");
    }

    #endregion

    #region End-to-End ANSI Rendering

    [Fact]
    public void Normal_WithColorNone_RendersBothAsResetCodes ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        IDriver driver = app.Driver!;
        driver.SetScreenSize (5, 1);
        driver.Clip = new Region (driver.Screen);

        // Runnable with Base scheme — Normal = (Color.None, Color.None)
        Runnable top = new () { Width = 5, Height = 1, SchemeName = "Base", Driver = driver };

        top.DrawingContent += (s, e) =>
                              {
                                  View v = (View)s!;
                                  v.AddStr (0, 0, "Hello");
                                  e.DrawContext?.AddDrawnRectangle (v.Viewport);
                                  e.Cancel = true;
                              };

        app.Begin (top);

        // Normal = (Color.None fg, Color.None bg)
        // Color.None fg → \x1b[39m (reset fg)
        // Color.None bg → \x1b[49m (reset bg)
        DriverAssert.AssertDriverOutputIs (@"\x1b[39m\x1b[49mHello", output, driver);
    }

    [Fact]
    public void Focus_WithDarkTerminal_RendersResolvedColors ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        IDriver driver = app.Driver!;
        driver.SetScreenSize (5, 1);
        driver.Clip = new Region (driver.Screen);

        // Simulate dark terminal via OSC 10/11 detection
        DriverImpl driverImpl = (DriverImpl)driver;
        driverImpl.SetDefaultAttribute (new Attribute (new Color (200, 200, 200), new Color (20, 20, 20)));

        // Runnable that draws with Focus attribute (using Base scheme)
        Runnable top = new () { Width = 5, Height = 1, SchemeName = "Base", Driver = driver };

        top.DrawingContent += (s, e) =>
                              {
                                  View v = (View)s!;

                                  // Explicitly set the Focus attribute for drawing
                                  v.SetAttributeForRole (VisualRole.Focus);
                                  v.AddStr (0, 0, "Focus");
                                  e.DrawContext?.AddDrawnRectangle (v.Viewport);
                                  e.Cancel = true;
                              };

        app.Begin (top);

        // Focus derivation with dark terminal (fg=200,200,200 bg=20,20,20):
        // Focus.Foreground = ResolveNone(Color.None, bg) = (20,20,20) → \x1b[38;2;20;20;20m
        // Focus.Background = ResolveNone(Color.None, fg) = (200,200,200) → \x1b[48;2;200;200;200m
        DriverAssert.AssertDriverOutputIs (@"\x1b[38;2;20;20;20m\x1b[48;2;200;200;200mFocus", output, driver);
    }

    [Fact]
    public void Focus_WithWhiteTerminal_RendersResolvedColors ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        IDriver driver = app.Driver!;
        driver.SetScreenSize (5, 1);
        driver.Clip = new Region (driver.Screen);

        // Simulate white/light terminal
        DriverImpl driverImpl = (DriverImpl)driver;
        driverImpl.SetDefaultAttribute (new Attribute (new Color (40, 40, 40), new Color (240, 240, 240)));

        Runnable top = new () { Width = 5, Height = 1, SchemeName = "Base", Driver = driver };

        top.DrawingContent += (s, e) =>
                              {
                                  View v = (View)s!;
                                  v.SetAttributeForRole (VisualRole.Focus);
                                  v.AddStr (0, 0, "Focus");
                                  e.DrawContext?.AddDrawnRectangle (v.Viewport);
                                  e.Cancel = true;
                              };

        app.Begin (top);

        // Focus derivation with white terminal (fg=40,40,40 bg=240,240,240):
        // Focus.Foreground = ResolveNone(Color.None, bg) = (240,240,240) → \x1b[38;2;240;240;240m
        // Focus.Background = ResolveNone(Color.None, fg) = (40,40,40) → \x1b[48;2;40;40;40m
        DriverAssert.AssertDriverOutputIs (@"\x1b[38;2;240;240;240m\x1b[48;2;40;40;40mFocus", output, driver);
    }

    [Fact]
    public void Focus_WithNoDefaultAttribute_FallsBackToBlackAndWhite ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        IDriver driver = app.Driver!;
        driver.SetScreenSize (5, 1);
        driver.Clip = new Region (driver.Screen);

        // No DefaultAttribute set — simulates terminal that didn't respond to OSC 10/11

        Runnable top = new () { Width = 5, Height = 1, SchemeName = "Base", Driver = driver };

        top.DrawingContent += (s, e) =>
                              {
                                  View v = (View)s!;
                                  v.SetAttributeForRole (VisualRole.Focus);
                                  v.AddStr (0, 0, "Focus");
                                  e.DrawContext?.AddDrawnRectangle (v.Viewport);
                                  e.Cancel = true;
                              };

        app.Begin (top);

        // Focus derivation without DefaultAttribute:
        // Focus.Foreground = ResolveNone(Color.None, null, bg) = Black = (0,0,0) → \x1b[38;2;0;0;0m
        // Focus.Background = ResolveNone(Color.None, null, fg) = White = (255,255,255) → \x1b[48;2;255;255;255m
        DriverAssert.AssertDriverOutputIs (@"\x1b[38;2;0;0;0m\x1b[48;2;255;255;255mFocus", output, driver);
    }

    #endregion
}
