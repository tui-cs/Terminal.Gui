// Copilot

namespace ViewBaseTests.Viewport;

/// <summary>
///     Regression tests for issue #4890: Vertical scrollbar moves to the left when scrolling down.
///     The vertical scrollbar's X position should remain anchored at the right edge of the
///     PaddingView regardless of scroll position.
/// </summary>
public class ViewScrollbarPositionTests (ITestOutputHelper output)
{
    /// <summary>
    ///     Verifies that the vertical scrollbar stays at the right edge of the
    ///     PaddingView after scrolling the Viewport down.
    /// </summary>
    [Fact]
    public void VerticalScrollBar_X_Stays_At_Right_Edge_After_Scrolling ()
    {
        View view = new ()
        {
            Frame = new Rectangle (0, 0, 20, 10)
        };

        view.ViewportSettings |= ViewportSettingsFlags.HasVerticalScrollBar;
        view.SetContentSize (new Size (20, 50)); // Content taller than viewport

        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        Assert.True (view.VerticalScrollBar.Visible, "ScrollBar should be visible");

        int initialX = view.VerticalScrollBar.Frame.X;
        output.WriteLine ($"Initial ScrollBar Frame: {view.VerticalScrollBar.Frame}");
        output.WriteLine ($"Initial PaddingView Frame: {view.Padding.View!.Frame}");
        output.WriteLine ($"Initial Padding.Thickness: {view.Padding.Thickness}");

        // Scroll down
        view.VerticalScrollBar.Value = 10;
        view.LayoutSubViews ();

        int afterScrollX = view.VerticalScrollBar.Frame.X;
        output.WriteLine ($"After scroll ScrollBar Frame: {view.VerticalScrollBar.Frame}");
        output.WriteLine ($"After scroll Padding.Thickness: {view.Padding.Thickness}");

        Assert.Equal (initialX, afterScrollX);
    }

    /// <summary>
    ///     Verifies that Padding.Thickness.Right does not keep incrementing
    ///     when the scrollbar is scrolled (it should only be incremented once
    ///     when the scrollbar first becomes visible).
    /// </summary>
    [Fact]
    public void VerticalScrollBar_Scrolling_Does_Not_Increment_Padding_Thickness ()
    {
        View view = new ()
        {
            Frame = new Rectangle (0, 0, 20, 10)
        };

        view.ViewportSettings |= ViewportSettingsFlags.HasVerticalScrollBar;
        view.SetContentSize (new Size (20, 50));

        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        Assert.True (view.VerticalScrollBar.Visible);
        int expectedRight = view.Padding.Thickness.Right;
        output.WriteLine ($"Initial Padding.Thickness.Right: {expectedRight}");
        Assert.Equal (1, expectedRight);

        // Scroll down multiple times
        for (var i = 1; i <= 10; i++)
        {
            view.VerticalScrollBar.Value = i;
            view.LayoutSubViews ();

            int currentRight = view.Padding.Thickness.Right;
            output.WriteLine ($"After scroll to {i}: Padding.Thickness.Right = {currentRight}");
            Assert.Equal (expectedRight, currentRight);
        }
    }

    /// <summary>
    ///     Reproduces issue #4890 exactly: a TextView with ScrollBars=true and
    ///     VerticalScrollBar.VisibilityMode=Auto should keep its scrollbar
    ///     at the right edge after scrolling down.
    /// </summary>
    [Fact]
    public void Issue4890_TextView_VerticalScrollBar_Does_Not_Move_Left_On_Scroll ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (40, 15);

        using Runnable top = new ();
        SessionToken? token = app.Begin (top);

        // Reproduce the exact scenario from issue #4890
        TextView textView = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Text = string.Join ("\n", Enumerable.Range (1, 200).Select (x => $"Line {x}")),
            ScrollBars = true,
        };

        textView.VerticalScrollBar.VisibilityMode = ScrollBarVisibilityMode.Auto;

        top.Add (textView);
        app.LayoutAndDraw ();

        Assert.True (textView.VerticalScrollBar.Visible, "ScrollBar should be visible");

        int initialScrollBarX = textView.VerticalScrollBar.Frame.X;
        output.WriteLine ($"Initial ScrollBar X: {initialScrollBarX}");
        output.WriteLine ($"Initial Padding.Thickness: {textView.Padding.Thickness}");
        output.WriteLine ($"PaddingView Frame: {textView.Padding.View!.Frame}");

        // Scroll down several times (simulating the user scenario)
        for (var i = 1; i <= 5; i++)
        {
            textView.VerticalScrollBar.Value = i * 5;
            app.LayoutAndDraw ();

            int currentX = textView.VerticalScrollBar.Frame.X;
            output.WriteLine ($"After scroll to {i * 5}: ScrollBar X = {currentX}, Padding.Right = {textView.Padding.Thickness.Right}");
            Assert.Equal (initialScrollBarX, currentX);
        }

        app.End (token!);
    }

    /// <summary>
    ///     Verifies that the vertical scrollbar Frame.X matches the expected
    ///     position at the right edge of the PaddingView for multiple layout passes.
    /// </summary>
    [Fact]
    public void VerticalScrollBar_X_Correct_After_Multiple_LayoutAndDraw ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (30, 10);

        using Runnable top = new ();
        SessionToken? token = app.Begin (top);

        View view = new ()
        {
            Width = 20,
            Height = 8
        };

        view.ViewportSettings |= ViewportSettingsFlags.HasVerticalScrollBar;
        view.SetContentSize (new Size (20, 40));

        top.Add (view);
        app.LayoutAndDraw ();

        Assert.True (view.VerticalScrollBar.Visible, "ScrollBar should be visible");

        // The scrollbar should be at the right edge of the PaddingView
        // PaddingView.Frame.Width - 1 (for the scrollbar width of 1) should be the X
        int paddingViewWidth = view.Padding.View!.Frame.Width;
        int expectedX = paddingViewWidth - 1;
        output.WriteLine ($"PaddingView width: {paddingViewWidth}, expected ScrollBar X: {expectedX}");
        Assert.Equal (expectedX, view.VerticalScrollBar.Frame.X);

        // Now scroll and verify the X stays the same after each layout pass
        view.VerticalScrollBar.Value = 5;
        app.LayoutAndDraw ();
        output.WriteLine ($"After scroll(5): ScrollBar X = {view.VerticalScrollBar.Frame.X}");
        Assert.Equal (expectedX, view.VerticalScrollBar.Frame.X);

        view.VerticalScrollBar.Value = 15;
        app.LayoutAndDraw ();
        output.WriteLine ($"After scroll(15): ScrollBar X = {view.VerticalScrollBar.Frame.X}");
        Assert.Equal (expectedX, view.VerticalScrollBar.Frame.X);

        view.VerticalScrollBar.Value = 30;
        app.LayoutAndDraw ();
        output.WriteLine ($"After scroll(30): ScrollBar X = {view.VerticalScrollBar.Frame.X}");
        Assert.Equal (expectedX, view.VerticalScrollBar.Frame.X);

        app.End (token!);
    }

    /// <summary>
    ///     Verifies that setting Viewport.Y (scroll position) on a view with a
    ///     vertical scrollbar doesn't cause the scrollbar's X to drift.
    /// </summary>
    [Fact]
    public void VerticalScrollBar_X_Stable_When_Viewport_Y_Changes ()
    {
        View view = new ()
        {
            Frame = new Rectangle (0, 0, 20, 10)
        };

        view.ViewportSettings |= ViewportSettingsFlags.HasVerticalScrollBar;
        view.SetContentSize (new Size (20, 100));

        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        Assert.True (view.VerticalScrollBar.Visible);

        int initialX = view.VerticalScrollBar.Frame.X;
        output.WriteLine ($"Initial ScrollBar X: {initialX}");

        // Directly change Viewport.Y (which triggers the ViewportChanged event
        // that updates scrollBar.Value)
        view.Viewport = view.Viewport with { Y = 10 };
        view.LayoutSubViews ();

        int afterViewportChangeX = view.VerticalScrollBar.Frame.X;
        output.WriteLine ($"After Viewport.Y=10: ScrollBar X = {afterViewportChangeX}");
        Assert.Equal (initialX, afterViewportChangeX);

        view.Viewport = view.Viewport with { Y = 50 };
        view.LayoutSubViews ();

        int afterSecondChangeX = view.VerticalScrollBar.Frame.X;
        output.WriteLine ($"After Viewport.Y=50: ScrollBar X = {afterSecondChangeX}");
        Assert.Equal (initialX, afterSecondChangeX);
    }

    /// <summary>
    ///     Verifies that the vertical scrollbar renders at the rightmost column
    ///     of the view's content area after scrolling (visual verification).
    /// </summary>
    [Fact]
    public void Issue4890_ScrollBar_Renders_At_Right_Edge_After_Scroll ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (25, 12);

        using Runnable top = new ();
        SessionToken? token = app.Begin (top);

        // Create a view with border to match common usage
        Window window = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        TextView textView = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Text = string.Join ("\n", Enumerable.Range (1, 100).Select (x => $"Line {x}")),
            ScrollBars = true,
        };

        textView.VerticalScrollBar.VisibilityMode = ScrollBarVisibilityMode.Auto;

        window.Add (textView);
        top.Add (window);
        app.LayoutAndDraw ();

        Assert.True (textView.VerticalScrollBar.Visible, "ScrollBar should be visible");

        // Record the initial X of the scrollbar
        int initialX = textView.VerticalScrollBar.Frame.X;
        output.WriteLine ($"Initial ScrollBar Frame: {textView.VerticalScrollBar.Frame}");

        // Scroll down significantly
        textView.VerticalScrollBar.Value = 20;
        app.LayoutAndDraw ();

        int afterScrollX = textView.VerticalScrollBar.Frame.X;
        output.WriteLine ($"After scroll(20) ScrollBar Frame: {textView.VerticalScrollBar.Frame}");

        // The scrollbar X must not have shifted left
        Assert.Equal (initialX, afterScrollX);

        // Scroll down more
        textView.VerticalScrollBar.Value = 50;
        app.LayoutAndDraw ();

        int afterMoreScrollX = textView.VerticalScrollBar.Frame.X;
        output.WriteLine ($"After scroll(50) ScrollBar Frame: {textView.VerticalScrollBar.Frame}");

        Assert.Equal (initialX, afterMoreScrollX);

        app.End (token!);
    }
}
