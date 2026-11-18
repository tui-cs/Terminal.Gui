using UnitTests;

namespace ViewsTests;

public class ButtonDrawingTests (ITestOutputHelper output) : TestDriverBase
{
    // Copilot
    [Theory]
    [InlineData (false)]
    [InlineData (true)]
    public void FixedWidth_Anchors_Delimiters_To_Edges (bool isDefault)
    {
        const int width = 10;
        string expected = isDefault
                              ? $"{Glyphs.LeftBracket} {Glyphs.LeftDefaultIndicator} OK {Glyphs.RightDefaultIndicator} {Glyphs.RightBracket}"
                              : $"{Glyphs.LeftBracket}   OK   {Glyphs.RightBracket}";

        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (width, 1);

        Runnable runnable = new () { Width = width, Height = 1 };
        app.Begin (runnable);

        Button button = new ()
        {
            Text = "_OK",
            X = 0,
            Y = 0,
            Width = width,
            Height = 1,
            IsDefault = isDefault,
            ShadowStyle = null
        };

        runnable.Add (button);
        app.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsWithFrameAre (expected, output, app.Driver);
    }

    // Copilot
    [Fact]
    public void Focused_FixedWidth_Button_MultiRow_Highlight_Is_Continuous ()
    {
        const int width = 10;
        const int height = 3;

        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (width, height);

        Runnable runnable = new () { Width = width, Height = height };
        app.Begin (runnable);

        Button button = new ()
        {
            Text = "_OK",
            X = 0,
            Y = 0,
            Width = width,
            Height = height,
            ShadowStyle = null
        };

        runnable.Add (button);
        button.SetFocus ();
        app.LayoutAndDraw ();

        // All rows must carry the Focus attribute — verifies the highlight is continuous
        // across rows above and below the text (not just the delimiter row).
        DriverAssert.AssertDriverAttributesAre (
            """
            0000000000
            0000100000
            0000000000
            """,
            output,
            app.Driver,
            button.GetAttributeForRole (VisualRole.Focus),
            button.GetAttributeForRole (VisualRole.HotFocus)
        );
    }

    // Copilot
    [Fact]
    public void Focused_FixedWidth_Button_Highlight_Is_Continuous ()
    {
        const int width = 10;

        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (width, 1);

        Runnable runnable = new () { Width = width, Height = 1 };
        app.Begin (runnable);

        Button button = new ()
        {
            Text = "_OK",
            X = 0,
            Y = 0,
            Width = width,
            Height = 1,
            ShadowStyle = null
        };

        runnable.Add (button);
        button.SetFocus ();
        app.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsWithFrameAre ($"{Glyphs.LeftBracket}   OK   {Glyphs.RightBracket}", output, app.Driver);
        DriverAssert.AssertDriverAttributesAre (
            "0000100000",
            output,
            app.Driver,
            button.GetAttributeForRole (VisualRole.Focus),
            button.GetAttributeForRole (VisualRole.HotFocus)
        );
    }
}
