using UnitTests;

namespace ViewsTests;

/// <summary>
///     Visual + input-driven tests for <see cref="LinearRange{T}"/> (range-only).
/// </summary>
public class LinearRangeVisualTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    // Copilot
    [Fact]
    public void Renders_Closed_Range ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver?.SetScreenSize (12, 3);

        IRunnable runnable = new Runnable ();
        LinearRange<int> r = new ([1, 2, 3, 4]);
        r.Value = new LinearRangeSpan<int> (LinearRangeSpanKind.Closed, 2, 3, 1, 2);
        (runnable as View)?.Add (r);
        app.Begin (runnable);

        app.LayoutAndDraw ();

        // Indexes 1..2 selected: '●' (option), '─' (space), '█' (set), '░' (range).
        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       """
                                                       ●─█░█─●
                                                       1 2 3 4
                                                       """,
                                                       _output,
                                                       app.Driver);
    }

    // Copilot
    [Fact]
    public void Mouse_Drag_Adjusts_End_Of_Range ()
    {
        // This is the regression test for the user-reported bug:
        //   "Mouse dragging of the end of a range is not adjusting the values."
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver?.SetScreenSize (15, 3);

        IRunnable runnable = new Runnable ();
        LinearRange<int> r = new ([1, 2, 3, 4, 5]) { AllowEmpty = true };
        (runnable as View)?.Add (r);
        app.Begin (runnable);
        app.LayoutAndDraw ();

        Assert.True (r.TryGetPositionByOption (1, out (int x, int y) p1));
        Assert.True (r.TryGetPositionByOption (4, out (int x, int y) p4));
        Point sStart = r.ViewportToScreen (new Point (p1.x, p1.y));
        Point sEnd = r.ViewportToScreen (new Point (p4.x, p4.y));

        // Press at index 1 (the start of the range).
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport, ScreenPosition = sStart });

        // Drag to index 4 — the end of the range.
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport, ScreenPosition = sEnd });

        // Release.
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = sEnd });

        // Range should span [2..5] = data values at indexes 1..4.
        Assert.Equal (LinearRangeSpanKind.Closed, r.Value.Kind);
        Assert.Equal (2, r.Value.Start);
        Assert.Equal (5, r.Value.End);
    }

    // Copilot
    [Fact]
    public void Keyboard_Extends_Range ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver?.SetScreenSize (15, 3);

        IRunnable runnable = new Runnable ();
        LinearRange<int> r = new ([1, 2, 3, 4, 5]) { AllowEmpty = true };
        (runnable as View)?.Add (r);
        app.Begin (runnable);
        r.SetFocus ();

        // Activate at index 0 (start of range).
        app.InjectKey (new Key (KeyCode.Space));
        Assert.Equal (1, r.Value.Start);

        // Ctrl+Right extends the range.
        app.InjectKey (new Key (KeyCode.CursorRight | KeyCode.CtrlMask));
        app.InjectKey (new Key (KeyCode.CursorRight | KeyCode.CtrlMask));

        Assert.Equal (LinearRangeSpanKind.Closed, r.Value.Kind);
        Assert.Equal (1, r.Value.Start);
        Assert.Equal (3, r.Value.End);
    }

    // Copilot
    [Fact]
    public void Mouse_Press_On_Left_End_Of_Closed_Range_Preserves_Right_End ()
    {
        // Bug: pressing on the left end of an existing Closed range causes the right end
        // to reset/collapse depending on the previous _lastFocusedOption.
        //
        // Setup: Closed range covering all five options [0..4] (data 1..5).
        // Click at the right end first to set _lastFocusedOption to the right.
        // Then press at the left end. The range should still terminate at option 4.
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver?.SetScreenSize (40, 3);

        IRunnable runnable = new Runnable ();
        LinearRange<int> r = new ([1, 2, 3, 4, 5]) { AllowEmpty = true, MinimumInnerSpacing = 3 };
        r.Value = new LinearRangeSpan<int> (LinearRangeSpanKind.Closed, 1, 5, 0, 4);
        (runnable as View)?.Add (r);
        app.Begin (runnable);
        app.LayoutAndDraw ();

        // First, force focus on the right end so _lastFocusedOption=4 when we press the left.
        r.OnOptionFocused (4, new LinearRangeEventArgs<int> (new (), 4));

        Assert.True (r.TryGetPositionByOption (0, out (int x, int y) pLeft));
        Point sLeft = r.ViewportToScreen (new Point (pLeft.x, pLeft.y));

        // Press at left end (option 0).
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport, ScreenPosition = sLeft });
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = sLeft });

        // The range should still cover [option 0 .. option 4] = data 1..5. The right end must
        // not have collapsed.
        Assert.Equal (LinearRangeSpanKind.Closed, r.Value.Kind);
        Assert.Equal (1, r.Value.Start);
        Assert.Equal (5, r.Value.End);
    }

    // Copilot
    [Fact]
    public void Mouse_Press_Inside_Closed_Range_Does_Not_Collapse_Range ()
    {
        // Bug: pressing inside an existing Closed range causes one end to reset (collapse to a point).
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver?.SetScreenSize (40, 3);

        IRunnable runnable = new Runnable ();
        LinearRange<int> r = new ([1, 2, 3, 4, 5]) { AllowEmpty = true, MinimumInnerSpacing = 3 };
        r.Value = new LinearRangeSpan<int> (LinearRangeSpanKind.Closed, 2, 5, 1, 4);
        (runnable as View)?.Add (r);
        app.Begin (runnable);
        app.LayoutAndDraw ();

        // Force the right end (option 4) to be the most recently focused.
        r.OnOptionFocused (4, new LinearRangeEventArgs<int> (new (), 4));

        // Press at option 2 (inside the [1..4] range).
        Assert.True (r.TryGetPositionByOption (2, out (int x, int y) pMid));
        Point sMid = r.ViewportToScreen (new Point (pMid.x, pMid.y));

        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport, ScreenPosition = sMid });
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = sMid });

        // Range should remain closed and bracket option 2 — both ends should still exist.
        Assert.Equal (LinearRangeSpanKind.Closed, r.Value.Kind);
        Assert.NotEqual (r.Value.StartIndex, r.Value.EndIndex);
    }

    // Copilot
    [Fact]
    public void Mouse_Drag_Adjusts_End_Of_LeftBounded_Range ()
    {
        // Bug: for LeftBounded (RangeKind=LeftBounded), dragging the single end should follow the mouse.
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver?.SetScreenSize (40, 3);

        IRunnable runnable = new Runnable ();
        LinearRange<int> r = new ([1, 2, 3, 4, 5]) { AllowEmpty = true, MinimumInnerSpacing = 3, RangeKind = LinearRangeSpanKind.LeftBounded };
        r.Value = new LinearRangeSpan<int> (LinearRangeSpanKind.LeftBounded, default, 3, -1, 2);
        (runnable as View)?.Add (r);
        app.Begin (runnable);
        app.LayoutAndDraw ();

        Assert.True (r.TryGetPositionByOption (0, out (int x, int y) p0));
        Assert.True (r.TryGetPositionByOption (4, out (int x, int y) p4));
        Point s0 = r.ViewportToScreen (new Point (p0.x, p0.y));
        Point s4 = r.ViewportToScreen (new Point (p4.x, p4.y));

        // Press at option 0 and drag to option 4.
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport, ScreenPosition = s0 });
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport, ScreenPosition = s4 });
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = s4 });

        // The single bounded end should now be option 4 (data 5).
        Assert.Equal (LinearRangeSpanKind.LeftBounded, r.Value.Kind);
        Assert.Equal (5, r.Value.End);
        Assert.Equal (4, r.Value.EndIndex);
    }

    // Copilot
    [Fact]
    public void Mouse_Drag_Adjusts_Start_Of_RightBounded_Range ()
    {
        // Bug: for RightBounded (RangeKind=RightBounded), dragging the single start should follow the mouse,
        // including through positions that snap via threshold.
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver?.SetScreenSize (40, 3);

        IRunnable runnable = new Runnable ();
        LinearRange<int> r = new ([1, 2, 3, 4, 5]) { AllowEmpty = true, MinimumInnerSpacing = 3, RangeKind = LinearRangeSpanKind.RightBounded };
        r.Value = new LinearRangeSpan<int> (LinearRangeSpanKind.RightBounded, 2, default, 1, -1);
        (runnable as View)?.Add (r);
        app.Begin (runnable);
        app.LayoutAndDraw ();

        Assert.True (r.TryGetPositionByOption (1, out (int x, int y) p1));
        Assert.True (r.TryGetPositionByOption (3, out (int x, int y) p3));
        Point s1 = r.ViewportToScreen (new Point (p1.x, p1.y));
        Point s3 = r.ViewportToScreen (new Point (p3.x, p3.y));

        // Press at option 1, drag through intermediate positions to option 3.
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport, ScreenPosition = s1 });

        // Step through every viewport-x between s1.X and s3.X to simulate a continuous mouse drag.
        for (int x = s1.X + 1; x <= s3.X; x++)
        {
            app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport, ScreenPosition = new Point (x, s1.Y) });
        }

        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = s3 });

        // Start should now be at option 3 (data 4); right end stays unbounded.
        Assert.Equal (LinearRangeSpanKind.RightBounded, r.Value.Kind);
        Assert.Equal (4, r.Value.Start);
        Assert.Equal (3, r.Value.StartIndex);
    }
}
