namespace InputTests.MouseTests;

/// <summary>
///     Tests for mouse input injection examples from mouse.md documentation.
///     These tests validate the documented behavior and serve as executable examples.
///     Note: Most tests do not require adding views to TopRunnable - they test the input injection pipeline.
/// </summary>
[Trait ("Category", "Mouse")]
[Trait ("Category", "Input")]
public class MouseInjectionDocTests
{
    [Fact]
    public void Button_ClickWithMouse_RaisesAccepting ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI); // Use ANSI driver for testing

        IRunnable runnable = new Runnable ();
        Button button = new () { Text = "Click Me" };
        (runnable as View)?.Add (button);
        app.Begin (runnable);

        var acceptingCalled = false;
        button.Accepting += (s, e) => acceptingCalled = true;

        // Use helper method for simplified click injection
        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (new (0, 0)));

        Assert.True (acceptingCalled);
        (runnable as View)?.Dispose ();
    }

    #region Double-Click Detection

    [Fact]
    public void DoubleClick_WithinThreshold_DetectsDoubleClick ()
    {
        VirtualTimeProvider time = new ();
        time.SetTime (new (2025, 1, 1, 12, 0, 0));

        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);

        List<MouseFlags> receivedFlags = [];
        app.Mouse.MouseEvent += (s, e) => receivedFlags.Add (e.Flags);

        // First click at T+0
        app.InjectMouse (new () { Flags = MouseFlags.LeftButtonPressed });

        time.Advance (TimeSpan.FromMilliseconds (50));

        app.InjectMouse (new () { Flags = MouseFlags.LeftButtonReleased });

        // Second click at T+350 (within 500ms double-click threshold)
        time.Advance (TimeSpan.FromMilliseconds (300));

        app.InjectMouse (new () { Flags = MouseFlags.LeftButtonPressed });

        time.Advance (TimeSpan.FromMilliseconds (50));

        app.InjectMouse (new () { Flags = MouseFlags.LeftButtonReleased });

        // Verify double-click detected
        Assert.Contains (receivedFlags, f => f.HasFlag (MouseFlags.LeftButtonDoubleClicked));
    }

    [Fact]
    public void DoubleClick_OutsideThreshold_DoesNotDetectDoubleClick ()
    {
        VirtualTimeProvider time = new ();
        time.SetTime (new (2025, 1, 1, 12, 0, 0));

        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);

        List<MouseFlags> receivedFlags = [];
        app.Mouse.MouseEvent += (s, e) => receivedFlags.Add (e.Flags);

        // First click at T+0
        app.InjectMouse (new () { Flags = MouseFlags.LeftButtonPressed });

        time.Advance (TimeSpan.FromMilliseconds (50));

        app.InjectMouse (new () { Flags = MouseFlags.LeftButtonReleased });

        // Second click at T+600 (outside 500ms threshold)
        time.Advance (TimeSpan.FromMilliseconds (600));

        app.InjectMouse (new () { Flags = MouseFlags.LeftButtonPressed });

        time.Advance (TimeSpan.FromMilliseconds (50));

        app.InjectMouse (new () { Flags = MouseFlags.LeftButtonReleased });

        // Verify double-click NOT detected
        Assert.DoesNotContain (receivedFlags, f => f.HasFlag (MouseFlags.LeftButtonDoubleClicked));

        // Verify we got two single clicks instead
        Assert.Equal (2, receivedFlags.Count (f => f.HasFlag (MouseFlags.LeftButtonClicked)));
    }

    #endregion

    #region Triple-Click Detection

    [Fact]
    public void TripleClick_WithinThreshold_DetectsTripleClick ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);

        List<MouseFlags> receivedFlags = [];
        app.Mouse.MouseEvent += (s, e) => receivedFlags.Add (e.Flags);

        // First click
        app.InjectMouse (new () { Flags = MouseFlags.LeftButtonPressed });

        time.Advance (TimeSpan.FromMilliseconds (50));

        app.InjectMouse (new () { Flags = MouseFlags.LeftButtonReleased });

        // Second click (within 500ms)
        time.Advance (TimeSpan.FromMilliseconds (200));

        app.InjectMouse (new () { Flags = MouseFlags.LeftButtonPressed });

        time.Advance (TimeSpan.FromMilliseconds (50));

        app.InjectMouse (new () { Flags = MouseFlags.LeftButtonReleased });

        // Third click (within 500ms of second)
        time.Advance (TimeSpan.FromMilliseconds (200));

        app.InjectMouse (new () { Flags = MouseFlags.LeftButtonPressed });

        time.Advance (TimeSpan.FromMilliseconds (50));

        app.InjectMouse (new () { Flags = MouseFlags.LeftButtonReleased });

        // Verify triple-click detected
        Assert.Contains (receivedFlags, f => f.HasFlag (MouseFlags.LeftButtonTripleClicked));
    }

    #endregion

    #region Multiple Buttons

    [Fact]
    public void MultipleButtons_RightClick_RaisesEvent ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);

        var rightClickReceived = false;

        app.Mouse.MouseEvent += (s, e) =>
                                {
                                    if (e.Flags.HasFlag (MouseFlags.RightButtonClicked))
                                    {
                                        rightClickReceived = true;
                                    }
                                };

        // Right click
        app.InjectMouse (new () { Flags = MouseFlags.RightButtonPressed });

        app.InjectMouse (new () { Flags = MouseFlags.RightButtonReleased, Position = new (10, 10) });

        Assert.True (rightClickReceived);
    }

    [Fact]
    public void MultipleButtons_MiddleClick_RaisesEvent ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);

        var middleClickReceived = false;

        app.Mouse.MouseEvent += (s, e) =>
                                {
                                    if (e.Flags.HasFlag (MouseFlags.MiddleButtonClicked))
                                    {
                                        middleClickReceived = true;
                                    }
                                };

        // Middle click
        app.InjectMouse (new () { Flags = MouseFlags.MiddleButtonPressed });

        app.InjectMouse (new () { Flags = MouseFlags.MiddleButtonReleased });

        Assert.True (middleClickReceived);
    }

    #endregion

    [Fact]
    public void MouseDrag_MovesView ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);

        View view = new () { Width = 10, Height = 10 };
        Point? lastPosition = null;

        view.MouseEvent += (s, e) =>
                           {
                               if (e.Flags.HasFlag (MouseFlags.LeftButtonPressed) && !e.Flags.HasFlag (MouseFlags.PositionReport))
                               {
                                   lastPosition = e.Position;
                               }

                               if (e.Flags.HasFlag (MouseFlags.PositionReport) && lastPosition.HasValue && e.Position.HasValue)
                               {
                                   // Handle drag
                                   view.X += e.Position.Value.X - lastPosition.Value.X;
                                   view.Y += e.Position.Value.Y - lastPosition.Value.Y;
                                   lastPosition = e.Position;
                               }
                           };

        IRunnable runnable = new Runnable ();
        (runnable as View)?.Add (view);
        app.Begin (runnable);

        // Press at (5, 5)
        app.InjectMouse (new () { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new (0, 0) });

        // Drag to (10, 10)
        app.InjectMouse (new () { Flags = MouseFlags.PositionReport | MouseFlags.LeftButtonPressed, ScreenPosition = new (5, 5) });
        app.LayoutAndDraw ();

        // Release
        app.InjectMouse (new () { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = new (5, 5) });

        Assert.Equal (5, view.Frame.X);
        Assert.Equal (5, view.Frame.Y);

        (runnable as View)?.Dispose ();
    }
}
