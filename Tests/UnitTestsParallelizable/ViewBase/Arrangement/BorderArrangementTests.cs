using System.Text;
using UnitTests;

namespace ViewBaseTests.Arrangement;

public class BorderArrangementTests (ITestOutputHelper output)
{
    #region Redraw Tests - Fixes #4623/#4629

    /// <summary>
    ///     Tests that when a view is moved via keyboard arrangement, the superview's background is properly redrawn.
    ///     This tests the fix for issue #4623 where moving a view could leave visual artifacts.
    /// </summary>
    [Fact]
    public void Arrangement_KeyboardMove_SuperViewRedrawn ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        app.Driver?.SetScreenSize (10, 5);

        using Runnable superview = new ();
        superview.Width = Dim.Fill ();
        superview.Height = Dim.Fill ();

        View view = new ()
        {
            X = 5,
            Y = 0,
            Width = 4,
            Height = 4,
            BorderStyle = LineStyle.Single,
            Arrangement = ViewArrangement.Movable | ViewArrangement.Resizable,
            CanFocus = true,
            Text = "V"
        };
        superview.Add (view);

        app.Begin (superview);

        // Initial state - view at X=5
        Assert.Equal (5, view.Frame.X);

        // Enter arrange mode
        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.F5.WithCtrl));
        app.LayoutAndDraw ();

        // Clear the NeedsDraw state to test propagation
        superview.ClearNeedsDraw ();
        view.ClearNeedsDraw ();

        Assert.False (superview.NeedsDraw);
        Assert.False (view.NeedsDraw);

        // Move left - this should mark the view AND superview as needing redraw
        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.CursorLeft));
        Assert.Equal (4, view.Frame.X);

        // The key assertion: Both the view and its superview should need redrawing
        // This is what the fix ensures - that the area vacated by the view is redrawn
        Assert.True (view.NeedsDraw);
        Assert.True (superview.NeedsDraw);
    }

    /// <summary>
    ///     Tests that when multiple runnables are on the session stack, moving a view in the top runnable
    ///     causes the underlying runnables to be redrawn properly.
    ///     This tests the fix for issue #4629.
    /// </summary>
    [Fact]
    public void Arrangement_WithMultipleRunnables_AllRedrawnCorrectly ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        app.Driver?.SetScreenSize (15, 8);

        // Bottom runnable with background pattern
        using Runnable bottomRunnable = new ();
        bottomRunnable.Width = Dim.Fill ();
        bottomRunnable.Height = Dim.Fill ();
        bottomRunnable.Text = "BOTTOM_RUNNABLE";
        app.Begin (bottomRunnable);

        // Top runnable (like a dialog) with a movable view
        using Runnable topRunnable = new ();
        topRunnable.X = 2;
        topRunnable.Y = 1;
        topRunnable.Width = 10;
        topRunnable.Height = 5;
        topRunnable.BorderStyle = LineStyle.Double;
        topRunnable.Text = "TOP";

        View movableView = new ()
        {
            X = 2,
            Y = 1,
            Width = 4,
            Height = 2,
            BorderStyle = LineStyle.Single,
            Arrangement = ViewArrangement.Movable,
            CanFocus = true,
            Text = "M"
        };
        topRunnable.Add (movableView);

        app.Begin (topRunnable);
        app.LayoutAndDraw ();

        // Initial state
        var initial = app.Driver?.ToString ();
        Assert.NotNull (initial);
        Assert.Contains ("TOP", initial);
        Assert.Contains ("BOTTOM", initial);

        // Enter arrange mode on movable view
        movableView.SetFocus ();
        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.F5.WithCtrl));
        app.LayoutAndDraw ();

        // Move the view left
        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.CursorLeft));
        app.LayoutAndDraw ();

        // Both runnables should be properly drawn
        var afterMove = app.Driver?.ToString ();
        Assert.NotNull (afterMove);

        // The bottom runnable text should still be visible in the corners
        Assert.Contains ("BO", afterMove);

        // Exit arrange mode
        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.Esc));
        app.LayoutAndDraw ();
    }

    #endregion

    [Fact]
    public void Arrangement_Handles_Wide_Glyphs_Correctly ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        app.Driver?.SetScreenSize (6, 5);

        // Using a replacement char to make sure wide glyphs are handled correctly
        // in the shadow area, to not confusing with a space char.
        app.Driver?.GetOutputBuffer ().SetWideGlyphReplacement (Rune.ReplacementChar);

        using Runnable superview = new ();
        superview.Width = Dim.Fill ();
        superview.Height = Dim.Fill ();

        superview.Text = """
                         🍎🍎🍎
                         🍎🍎🍎
                         🍎🍎🍎
                         🍎🍎🍎
                         🍎🍎🍎
                         """;

        View view = new ()
        {
            X = 2,
            Width = 4,
            Height = 4,
            BorderStyle = LineStyle.Single,
            Arrangement = ViewArrangement.Movable | ViewArrangement.Resizable,
            CanFocus = true
        };
        superview.Add (view);

        app.Begin (superview);

        Assert.Equal ("Absolute(2)", view.X.ToString ());

        DriverAssert.AssertDriverContentsAre ("""
                                              🍎┌──┐
                                              🍎│  │
                                              🍎│  │
                                              🍎└──┘
                                              🍎🍎🍎
                                              """,
                                              output,
                                              app.Driver);

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.F5.WithCtrl));
        app.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              🍎◊↕─┐
                                              🍎↔  ↔
                                              🍎│  │
                                              🍎└↕─↘
                                              🍎🍎🍎
                                              """,
                                              output,
                                              app.Driver);

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.CursorLeft));
        Assert.Equal ("Absolute(1)", view.X.ToString ());
        app.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              �◊↕─┐
                                              �↔  ↔
                                              �│  │
                                              �└↕─↘
                                              🍎🍎🍎
                                              """,
                                              output,
                                              app.Driver);

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.CursorLeft));
        Assert.Equal ("Absolute(0)", view.X.ToString ());
        app.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              ◊↕─┐🍎
                                              ↔  ↔🍎
                                              │  │🍎
                                              └↕─↘🍎
                                              🍎🍎🍎
                                              """,
                                              output,
                                              app.Driver);
    }

    [Fact]
    public void Arrangement_With_SubView_In_Border_Handles_Wide_Glyphs_Correctly ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        app.Driver?.SetScreenSize (8, 7);

        // Using a replacement char to make sure wide glyphs are handled correctly
        // in the shadow area, to not confusing with a space char.
        app.Driver?.GetOutputBuffer ().SetWideGlyphReplacement (Rune.ReplacementChar);

        // Don't remove this array even if it seems unused, it is used to map the attributes indexes in the DriverAssert
        // Otherwise the test won't detect issues with attributes not visibly by the naked eye
        Attribute [] attributes =
        [
            new (ColorName16.Blue, ColorName16.BrightBlue, TextStyle.None),
            new (ColorName16.BrightBlue, ColorName16.Blue, TextStyle.None),
            new (ColorName16.Green, ColorName16.BrightGreen, TextStyle.None),
            new (ColorName16.Magenta, ColorName16.BrightMagenta, TextStyle.None),
            new (ColorName16.BrightMagenta, ColorName16.Magenta, TextStyle.None)
        ];

        using Runnable superview = new ();
        superview.Width = Dim.Fill ();
        superview.Height = Dim.Fill ();
        superview.SetScheme (new Scheme { Normal = attributes [0], Focus = attributes [1] });

        superview.Text = """
                         🍎🍎🍎🍎
                         🍎🍎🍎🍎
                         🍎🍎🍎🍎
                         🍎🍎🍎🍎
                         🍎🍎🍎🍎
                         🍎🍎🍎🍎
                         🍎🍎🍎🍎
                         """;

        View view = new () { X = 6, Width = 2, Height = 1, Text = "🦮" };
        view.SetScheme (new Scheme { Normal = attributes [2] });

        View view2 = new ()
        {
            X = 2,
            Width = 6,
            Height = 6,
            Arrangement = ViewArrangement.Movable | ViewArrangement.Resizable,
            CanFocus = true
        };
        view2.Border.Thickness = new Thickness (1);
        view2.Border.GetOrCreateView ().Add (new View { Height = Dim.Auto (), Width = Dim.Auto (), Text = "Hi" });
        view2.SetScheme (new Scheme { Normal = attributes [3], HotNormal = attributes [4] });

        superview.Add (view, view2);

        app.Begin (superview);

        Assert.Equal ("Absolute(2)", view2.X.ToString ());

        DriverAssert.AssertDriverContentsAre ("""
                                              🍎Hi
                                              🍎
                                              🍎
                                              🍎
                                              🍎
                                              🍎
                                              🍎🍎🍎🍎
                                              """,
                                              output,
                                              app.Driver);

        DriverAssert.AssertDriverAttributesAre ("""
                                                11333333
                                                11333333
                                                11333333
                                                11333333
                                                11333333
                                                11333333
                                                11111111
                                                """,
                                                output,
                                                app.Driver,
                                                attributes);

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.F5.WithCtrl));
        app.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              🍎◊i↕
                                              🍎
                                              🍎↔    ↔
                                              🍎
                                              🍎
                                              🍎  ↕  ↘
                                              🍎🍎🍎🍎
                                              """,
                                              output,
                                              app.Driver);

        DriverAssert.AssertDriverAttributesAre ("""
                                                11433333
                                                11333333
                                                11333333
                                                11333333
                                                11333333
                                                11333333
                                                11111111
                                                """,
                                                output,
                                                app.Driver,
                                                attributes);

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.CursorLeft));
        Assert.Equal ("Absolute(1)", view2.X.ToString ());
        app.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              �◊i↕
                                              �
                                              �↔    ↔
                                              �
                                              �
                                              �  ↕  ↘
                                              🍎🍎🍎🍎
                                              """,
                                              output,
                                              app.Driver);

        DriverAssert.AssertDriverAttributesAre ("""
                                                14333332
                                                13333330
                                                13333330
                                                13333330
                                                13333330
                                                13333330
                                                11111111
                                                """,
                                                output,
                                                app.Driver,
                                                attributes);

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.CursorLeft));
        Assert.Equal ("Absolute(0)", view2.X.ToString ());
        app.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              ◊i↕   🦮
                                                    🍎
                                              ↔    ↔🍎
                                                    🍎
                                                    🍎
                                                ↕  ↘🍎
                                              🍎🍎🍎🍎
                                              """,
                                              output,
                                              app.Driver);

        DriverAssert.AssertDriverAttributesAre ("""
                                                43333322
                                                33333311
                                                33333311
                                                33333311
                                                33333311
                                                33333311
                                                11111111
                                                """,
                                                output,
                                                app.Driver,
                                                attributes);
    }
}
