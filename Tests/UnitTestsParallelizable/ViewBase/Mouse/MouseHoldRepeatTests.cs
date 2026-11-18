using Timeout = Terminal.Gui.App.Timeout;

namespace ViewBaseTests.MouseTests;

/// <summary>
///     Tests for View.MouseHoldRepeat functionality.
///     These tests verify that when MouseHoldRepeat is true, views correctly handle
///     press/release cycles with timer-based repetition.
/// </summary>

// CoPilot - GitHub Copilot Workspace
[Trait ("Category", "Input")]
public class MouseHoldRepeatTests (ITestOutputHelper output)
{
    [Fact]
    public void MouseHoldRepeat_True_Press_Release_Starts_And_Stops_Timer ()
    {
        // Arrange
        View view = new () { Width = 10, Height = 10, MouseHoldRepeat = MouseFlags.LeftButtonReleased };

        TimedEvents timedEvents = new ();
        ApplicationMouse mouseGrabber = new ();
        view.MouseHoldRepeater = new MouseHoldRepeaterImpl (view, timedEvents, mouseGrabber);

        Mouse mouse = new () { Position = new Point (5, 5), Flags = MouseFlags.LeftButtonPressed };

        // Act - Press button
        view.NewMouseEvent (mouse);

        // Assert - Timer should be started
        Assert.NotEmpty (timedEvents.Timeouts);

        // Act - Release button
        mouse.Flags = MouseFlags.LeftButtonReleased;
        mouse.Handled = false;
        view.NewMouseEvent (mouse);

        // Assert - Timer should be stopped
        Assert.Empty (timedEvents.Timeouts);

        view.Dispose ();
    }

    [Fact]
    public void MouseHoldRepeat_True_Press_Release_Raises_Activating_Once ()
    {
        // Arrange
        View view = new () { Width = 10, Height = 10, MouseHoldRepeat = MouseFlags.LeftButtonReleased };

        TimedEvents timedEvents = new ();
        ApplicationMouse mouseGrabber = new ();
        view.MouseHoldRepeater = new MouseHoldRepeaterImpl (view, timedEvents, mouseGrabber);

        var activatingCount = 0;

        view.Activating += (_, e) =>
                           {
                               activatingCount++;
                               e.Handled = true;
                           };

        Mouse mouse = new () { Position = new Point (5, 5), Flags = MouseFlags.LeftButtonPressed };

        // Act - Press button
        view.NewMouseEvent (mouse);
        Assert.Equal (0, activatingCount); // Should not fire on press

        // Act - Release button
        mouse.Flags = MouseFlags.LeftButtonReleased;
        mouse.Handled = false;
        view.NewMouseEvent (mouse);

        // Assert - Activating should be raised exactly once on release
        Assert.Equal (1, activatingCount);

        view.Dispose ();
    }

    [Fact]
    public void MouseHoldRepeat_False_Press_Release_Raises_Activating_Once ()
    {
        // Arrange
        View view = new ()
        {
            Width = 10, Height = 10, MouseHoldRepeat = null // false is the default; here for clarity
        };

        TimedEvents timedEvents = new ();
        ApplicationMouse mouseGrabber = new ();
        view.MouseHoldRepeater = new MouseHoldRepeaterImpl (view, timedEvents, mouseGrabber);

        var activatingCount = 0;

        view.Activating += (_, e) =>
                           {
                               activatingCount++;
                               e.Handled = true;
                           };

        Mouse mouse = new () { Position = new Point (5, 5), Flags = MouseFlags.LeftButtonPressed };

        // Act - Press button
        view.NewMouseEvent (mouse);
        Assert.Equal (0, activatingCount); // Default changed: should NOT fire on press (issue #4674)

        // Act - Release button
        mouse.Flags = MouseFlags.LeftButtonReleased;
        mouse.Handled = false;
        view.NewMouseEvent (mouse);

        // Assert - Activating should be raised exactly once (on release)
        Assert.Equal (1, activatingCount);

        view.Dispose ();
    }

    [Fact]
    public void MouseHoldRepeat_True_Then_False_Press_Release_Raises_Activating_Once ()
    {
        // Arrange
        View view = new () { Width = 10, Height = 10 };

        TimedEvents timedEvents = new ();
        ApplicationMouse mouseGrabber = new ();
        view.MouseHoldRepeater = new MouseHoldRepeaterImpl (view, timedEvents, mouseGrabber);

        var activatingCount = 0;

        view.Activating += (_, e) =>
                           {
                               activatingCount++;
                               e.Handled = true;
                           };

        Mouse mouse = new () { Position = new Point (5, 5), Flags = MouseFlags.LeftButtonPressed };

        // Act - Enable MouseHoldRepeat then disable it
        view.MouseHoldRepeat = MouseFlags.LeftButtonReleased;
        view.MouseHoldRepeat = null;

        // Now press
        view.NewMouseEvent (mouse);
        Assert.Equal (0, activatingCount); // Default changed: should NOT fire on press (issue #4674)

        // Act - Release button
        mouse.Flags = MouseFlags.LeftButtonReleased;
        mouse.Handled = false;
        view.NewMouseEvent (mouse);

        // Assert - Activating should be raised exactly once on release
        Assert.Equal (1, activatingCount);

        view.Dispose ();
    }

    [Fact]
    public void MouseHoldRepeat_True_Two_Press_Release_Cycles_Raises_Activating_Twice ()
    {
        // Arrange
        View view = new () { Width = 10, Height = 10, MouseHoldRepeat = MouseFlags.LeftButtonReleased };

        TimedEvents timedEvents = new ();
        ApplicationMouse mouseGrabber = new ();
        view.MouseHoldRepeater = new MouseHoldRepeaterImpl (view, timedEvents, mouseGrabber);

        var activatingCount = 0;

        view.Activating += (_, e) =>
                           {
                               activatingCount++;
                               e.Handled = true;
                           };

        Mouse mouse = new () { Position = new Point (5, 5) };

        // Act - First press/release cycle
        mouse.Flags = MouseFlags.LeftButtonPressed;
        view.NewMouseEvent (mouse);

        mouse.Flags = MouseFlags.LeftButtonReleased;
        mouse.Handled = false;
        view.NewMouseEvent (mouse);

        Assert.Equal (1, activatingCount); // First release

        // Act - Second press/release cycle
        mouse.Flags = MouseFlags.LeftButtonPressed;
        mouse.Handled = false;
        view.NewMouseEvent (mouse);

        mouse.Flags = MouseFlags.LeftButtonReleased;
        mouse.Handled = false;
        view.NewMouseEvent (mouse);

        // Assert - Activating should be raised twice (once per release)
        Assert.Equal (2, activatingCount);

        view.Dispose ();
    }

    [Fact]
    public void MouseHoldRepeat_True_Press_Wait_Release_Raises_Activating_Multiple_Times ()
    {
        // Arrange
        View view = new () { Width = 10, Height = 10, MouseHoldRepeat = MouseFlags.LeftButtonReleased };

        TimedEvents timedEvents = new ();
        ApplicationMouse mouseGrabber = new ();
        view.MouseHoldRepeater = new MouseHoldRepeaterImpl (view, timedEvents, mouseGrabber);

        var activatingCount = 0;

        view.Activating += (_, e) =>
                           {
                               activatingCount++;
                               e.Handled = true;
                           };

        Mouse mouse = new () { Position = new Point (5, 5), Flags = MouseFlags.LeftButtonPressed };

        // Act - Press button
        view.NewMouseEvent (mouse);
        Assert.Equal (0, activatingCount); // Should not fire on press

        // Simulate timer ticks (waiting while button is held)
        for (var i = 0; i < 3; i++)
        {
            KeyValuePair<long, Timeout> timeout = Assert.Single (timedEvents.Timeouts);
            timeout.Value.Callback?.Invoke ();
        }

        // Timer ticks should NOT directly invoke Activating - they invoke MouseIsHeldDownTick
        // which then invokes commands, but we're testing the view's behavior

        // Act - Release button
        mouse.Flags = MouseFlags.LeftButtonReleased;
        mouse.Handled = false;
        view.NewMouseEvent (mouse);

        // Assert - Should have > 3 activations (3 from timer ticks + 1 from release)
        // Note: The timer invokes commands through MouseIsHeldDownTick which calls
        // RaiseCommandsBoundToButtonFlags internally
        Assert.True (activatingCount >= 3, $"Expected >= 3 activations, got {activatingCount}");
        output.WriteLine ($"Expected >= 3 activations, got {activatingCount}");

        view.Dispose ();
    }

    [Fact]
    public void MouseHoldRepeat_Changing_In_SubViews_Works_Correctly ()
    {
        // Arrange
        View view = new () { Width = 10, Height = 10, MouseHoldRepeat = MouseFlags.LeftButtonPressed };

        Exception? exception = Record.Exception (() => new View { MouseHoldRepeat = view.MouseHoldRepeat }); // Inherit from parent
        Assert.Null (exception);
    }

    [Theory]
    [InlineData (MouseFlags.None)]
    [InlineData (MouseFlags.PositionReport)]
    [InlineData (MouseFlags.Button4Pressed)]
    public void MouseHoldRepeat_Throws_On_Invalid_Flags (MouseFlags mouseFlags)
    {
        // Arrange
        View view = new ();

        // Act & Assert - Setting invalid flags should throw
        Assert.Throws<ArgumentException> (() => view.MouseHoldRepeat = mouseFlags);
    }

    #region Input Injection Tests (Application Level)

    [Theory]
    [CombinatorialData]
    public void MouseHoldRepeat_True_AppInjection_Press_Release_Raises_Activating_Once (MouseState mouseState)
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);

        IRunnable runnable = new Runnable ();

        View view = new () { Width = 10, Height = 10, MouseHighlightStates = mouseState, MouseHoldRepeat = MouseFlags.LeftButtonReleased };
        (runnable as View)?.Add (view);
        app.Begin (runnable);

        var activatingCount = 0;

        view.Activating += (_, e) =>
                           {
                               activatingCount++;
                               e.Handled = true;
                           };

        // Act - Press at (0, 0)
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new Point (0, 0) });

        Assert.Equal (0, activatingCount); // Should not fire on press

        // Act - Release at (0, 0)
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = new Point (0, 0) });

        // Assert - Activating should be raised exactly once on release
        // Note: Clicked event is synthesized but ignored when MouseHoldRepeat is true
        Assert.Equal (1, activatingCount);

        (runnable as View)?.Dispose ();
    }

    [Fact]
    public void MouseHoldRepeat_True_AppInjection_Two_Press_Release_Cycles_Raises_Activating_Twice ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);

        IRunnable runnable = new Runnable ();

        View view = new () { Width = 10, Height = 10, MouseHoldRepeat = MouseFlags.LeftButtonReleased };
        (runnable as View)?.Add (view);
        app.Begin (runnable);

        var activatingCount = 0;

        view.Activating += (_, e) =>
                           {
                               activatingCount++;
                               e.Handled = true;
                           };

        // Act - First press/release cycle
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new Point (0, 0) });

        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = new Point (0, 0) });

        Assert.Equal (1, activatingCount); // First release

        // Act - Second press/release cycle (will synthesize DoubleClicked)
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new Point (0, 0) });

        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = new Point (0, 0) });

        // Assert - Activating should be raised twice (once per release)
        // DoubleClicked is synthesized but ignored when MouseHoldRepeat is true
        Assert.Equal (2, activatingCount);

        (runnable as View)?.Dispose ();
    }

    [Theory]
    [CombinatorialData]
    public void MouseHoldRepeat_True_AppInjection_Press_Wait_Release_Raises_Activating_Multiple_Times (MouseState mouseState)
    {
        // Arrange
        const int TIME_OUT_INTERVAL = 100;
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);

        IRunnable runnable = new Runnable ();

        View view = new () { Width = 10, Height = 10, MouseHighlightStates = mouseState, MouseHoldRepeat = MouseFlags.LeftButtonReleased };
        (runnable as View)?.Add (view);
        app.Begin (runnable);

        // Override the MouseHoldRepeater with a test instance
        view.MouseHoldRepeater = new MouseHoldRepeaterImpl (view, app.TimedEvents, app.Mouse);

        // Configure a simple repeating timeout for predictable testing (100ms interval)
        view.MouseHoldRepeater.Timeout = new Timeout
        {
            Span = TimeSpan.FromMilliseconds (TIME_OUT_INTERVAL), Callback = null! // Will be set by MouseHoldRepeaterImpl
        };

        var activatingCount = 0;

        view.Activating += (_, e) =>
                           {
                               activatingCount++;
                               e.Handled = true;
                           };

        // Act - Press button
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new Point (0, 0) });

        Assert.Equal (0, activatingCount); // Should not fire on press

        // With a simple 100ms repeating timeout, we can precisely control timing
        // Advance time and trigger 5 timer ticks (5 ensure no triple-click logic
        // is involved.
        for (var i = 1; i <= 5; i++)
        {
            time.Advance (TimeSpan.FromMilliseconds (TIME_OUT_INTERVAL));
            app.TimedEvents?.RunTimers ();
            output.WriteLine ($"After tick {i}: activatingCount={activatingCount}");
        }

        // Act - Release button
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = new Point (0, 0) });

        // Assert - Should have >= 5 activations (5 from timer ticks + 1 from release)
        Assert.True (activatingCount >= 5, $"Expected >= 5 activations, got {activatingCount}");
        output.WriteLine ($"Expected >= 5 activations, got {activatingCount}");

        (runnable as View)?.Dispose ();
    }

    [Fact]
    public void MouseHoldRepeat_False_AppInjection_Press_Release_Raises_Activating_Once ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);

        IRunnable runnable = new Runnable ();

        View view = new ()
        {
            Width = 10, Height = 10, MouseHoldRepeat = null // Default behavior
        };
        (runnable as View)?.Add (view);
        app.Begin (runnable);

        var activatingCount = 0;

        view.Activating += (_, e) =>
                           {
                               activatingCount++;
                               e.Handled = true;
                           };

        // Act - Press at (0, 0)
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new Point (0, 0) });

        Assert.Equal (0, activatingCount); // Default changed: should NOT fire on press (issue #4674)

        // Act - Release at (0, 0) - synthesizes Clicked event
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = new Point (0, 0) });

        // Assert - Activating should fire once on release
        Assert.Equal (1, activatingCount);

        (runnable as View)?.Dispose ();
    }

    #endregion

    #region Visibility/Enabled Change During Hold

    // Claude - Opus 4.6

    [Fact]
    public void MouseHoldRepeat_ViewBecomesInvisible_StopsRepeat ()
    {
        // Arrange
        View view = new () { Width = 10, Height = 10, MouseHoldRepeat = MouseFlags.LeftButtonReleased };

        TimedEvents timedEvents = new ();
        ApplicationMouse mouseGrabber = new ();
        view.MouseHoldRepeater = new MouseHoldRepeaterImpl (view, timedEvents, mouseGrabber);

        var activatingCount = 0;

        view.Activating += (_, e) =>
                           {
                               activatingCount++;
                               e.Handled = true;
                           };

        Mouse mouse = new () { Position = new Point (5, 5), Flags = MouseFlags.LeftButtonPressed, View = view };

        // Act - Press button to start hold repeat
        view.NewMouseEvent (mouse);
        Assert.NotEmpty (timedEvents.Timeouts);

        // Simulate one tick to confirm repeat is working
        KeyValuePair<long, Timeout> timeout = Assert.Single (timedEvents.Timeouts);
        timeout.Value.Callback?.Invoke ();
        Assert.True (activatingCount >= 1, "Should have at least one activation from tick");

        int countBeforeHide = activatingCount;

        // Act - Make view invisible while mouse is held down
        view.Visible = false;

        // Simulate another tick — should stop the repeat, not fire again
        if (timedEvents.Timeouts.Count > 0)
        {
            KeyValuePair<long, Timeout> timeout2 = timedEvents.Timeouts.First ();
            timeout2.Value.Callback?.Invoke ();
        }

        // Assert - No additional activations after becoming invisible
        Assert.Equal (countBeforeHide, activatingCount);

        // Assert - Timer should be stopped
        Assert.Empty (timedEvents.Timeouts);

        view.Dispose ();
    }

    [Fact]
    public void MouseHoldRepeat_ViewBecomesDisabled_StopsRepeat ()
    {
        // Arrange
        View view = new () { Width = 10, Height = 10, MouseHoldRepeat = MouseFlags.LeftButtonReleased };

        TimedEvents timedEvents = new ();
        ApplicationMouse mouseGrabber = new ();
        view.MouseHoldRepeater = new MouseHoldRepeaterImpl (view, timedEvents, mouseGrabber);

        var activatingCount = 0;

        view.Activating += (_, e) =>
                           {
                               activatingCount++;
                               e.Handled = true;
                           };

        Mouse mouse = new () { Position = new Point (5, 5), Flags = MouseFlags.LeftButtonPressed, View = view };

        // Act - Press button to start hold repeat
        view.NewMouseEvent (mouse);
        Assert.NotEmpty (timedEvents.Timeouts);

        // Simulate one tick to confirm repeat is working
        KeyValuePair<long, Timeout> timeout = Assert.Single (timedEvents.Timeouts);
        timeout.Value.Callback?.Invoke ();
        Assert.True (activatingCount >= 1, "Should have at least one activation from tick");

        int countBeforeDisable = activatingCount;

        // Act - Disable view while mouse is held down
        view.Enabled = false;

        // Simulate another tick — should stop the repeat, not fire again
        if (timedEvents.Timeouts.Count > 0)
        {
            KeyValuePair<long, Timeout> timeout2 = timedEvents.Timeouts.First ();
            timeout2.Value.Callback?.Invoke ();
        }

        // Assert - No additional activations after becoming disabled
        Assert.Equal (countBeforeDisable, activatingCount);

        // Assert - Timer should be stopped
        Assert.Empty (timedEvents.Timeouts);

        view.Dispose ();
    }

    [Fact]
    public void MouseHoldRepeat_ViewBecomesInvisible_NoReleaseNeeded ()
    {
        // Arrange - This verifies that the repeat stops without needing
        // a mouse release event, which won't arrive because NewMouseEvent
        // aborts for invisible views.
        View view = new () { Width = 10, Height = 10, MouseHoldRepeat = MouseFlags.LeftButtonReleased };

        TimedEvents timedEvents = new ();
        ApplicationMouse mouseGrabber = new ();
        view.MouseHoldRepeater = new MouseHoldRepeaterImpl (view, timedEvents, mouseGrabber);

        Mouse mouse = new () { Position = new Point (5, 5), Flags = MouseFlags.LeftButtonPressed, View = view };

        // Act - Press button to start hold repeat
        view.NewMouseEvent (mouse);
        Assert.NotEmpty (timedEvents.Timeouts);

        // Act - Make view invisible (simulates what Tabs does when hiding scroll buttons)
        view.Visible = false;

        // Act - Try to send release — NewMouseEvent will abort for invisible views,
        // so this should NOT stop the timer through normal release handling
        mouse.Flags = MouseFlags.LeftButtonReleased;
        mouse.Handled = false;
        view.NewMouseEvent (mouse);

        // The timer tick should detect the view is invisible and stop
        if (timedEvents.Timeouts.Count > 0)
        {
            KeyValuePair<long, Timeout> timeout = timedEvents.Timeouts.First ();
            timeout.Value.Callback?.Invoke ();
        }

        // Assert - Timer should be stopped after tick detects invisible view
        Assert.Empty (timedEvents.Timeouts);

        view.Dispose ();
    }

    #endregion
}
