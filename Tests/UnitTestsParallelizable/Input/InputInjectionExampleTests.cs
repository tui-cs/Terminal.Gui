namespace InputTests;

/// <summary>
///     Example tests demonstrating the simplified input injection API (Phase 6).
///     These tests showcase the benefits of the new architecture:
///     ✅ Single-call injection (no 3-step pattern)
///     ✅ Virtual time (no real delays)
///     ✅ Deterministic results
/// </summary>
[Trait ("Category", "Input")]
[Trait ("Category", "InputInjection")]
public class InputInjectionExampleTests
{
    #region Example 1: Basic Key Injection

    /// <summary>
    ///     Example 1: Simple key injection using the new API.
    ///     Single call handles encoding, queueing, and processing automatically.
    /// </summary>
    [Fact]
    public void Example1_SimpleKeyInjection ()
    {
        // Arrange - Create application with virtual time
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);

        var keyPressed = 0;
        Key? receivedKey = null;

        app.Keyboard.KeyDown += (s, e) =>
                                {
                                    keyPressed++;
                                    receivedKey = e;
                                };

        // Act - Single call does everything: encode → queue → parse → raise event
        app.InjectKey (Key.A);

        // Assert
        Assert.Equal (1, keyPressed);
        Assert.Equal (Key.A, receivedKey);
    }

    #endregion

    #region Example 2: Mouse Click with Virtual Time

    /// <summary>
    ///     Example 2: Mouse click with controlled timing using virtual time.
    ///     Demonstrates deterministic timestamp control without real delays.
    /// </summary>
    [Fact]
    public void Example2_MouseClick_WithVirtualTime ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);

        List<MouseFlags> receivedEvents = [];

        app.Mouse.MouseEvent += (s, e) => { receivedEvents.Add (e.Flags); };

        // Act - Inject press and release with controlled timing
        Mouse press = new () { ScreenPosition = new (5, 5), Flags = MouseFlags.LeftButtonPressed, Timestamp = time.Now };

        Mouse release = new ()
        {
            ScreenPosition = new (5, 5), Flags = MouseFlags.LeftButtonReleased, Timestamp = time.Now.AddMilliseconds (50) // Controlled 50ms delay
        };

        app.InjectMouse (press);

        // Advance virtual time (instant, no real delay)
        time.Advance (TimeSpan.FromMilliseconds (50));

        app.InjectMouse (release);

        // Assert - MouseInterpreter should synthesize Click event
        Assert.Contains (receivedEvents, f => f.HasFlag (MouseFlags.LeftButtonPressed));
        Assert.Contains (receivedEvents, f => f.HasFlag (MouseFlags.LeftButtonReleased));
        Assert.Contains (receivedEvents, f => f.HasFlag (MouseFlags.LeftButtonClicked));
        Assert.Equal (3, receivedEvents.Count);
    }

    #endregion

    #region Example 3: Event Sequence

    /// <summary>
    ///     Example 3: Injecting a sequence of events with delays.
    ///     Virtual time means no real delays - tests execute instantly.
    /// </summary>
    [Fact]
    public void Example3_EventSequence_WithVirtualDelays ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);

        List<Key> receivedKeys = [];

        app.Keyboard.KeyDown += (s, e) => { receivedKeys.Add (e); };

        // Act - Inject sequence with delays (executes instantly)
        InputInjectionEvent [] sequence =
        [
            new KeyInjectionEvent (Key.A) { Delay = TimeSpan.FromMilliseconds (100) },
            new KeyInjectionEvent (Key.B) { Delay = TimeSpan.FromMilliseconds (100) },
            new KeyInjectionEvent (Key.C)
        ];

        app.InjectSequence (sequence);

        // Assert - All keys received in order (no real delay occurred)
        Assert.Equal (3, receivedKeys.Count);
        Assert.Equal (Key.A, receivedKeys [0]);
        Assert.Equal (Key.B, receivedKeys [1]);
        Assert.Equal (Key.C, receivedKeys [2]);
    }

    #endregion

    #region Example 4: Multiple Keys Quickly

    /// <summary>
    ///     Example 4: Typing multiple keys in rapid succession.
    ///     Shows how simple the new API is compared to the old 3-step pattern.
    /// </summary>
    [Fact]
    public void Example4_MultipleKeys_Rapid ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);

        var typedText = "";

        app.Keyboard.KeyDown += (s, e) =>
                                {
                                    if (e.IsValid && e.AsRune.IsAscii)
                                    {
                                        typedText += (char)e.AsRune.Value;
                                    }
                                };

        // Act - Type "hello" one character at a time
        app.InjectKey (Key.H);
        app.InjectKey (Key.E);
        app.InjectKey (Key.L);
        app.InjectKey (Key.L);
        app.InjectKey (Key.O);

        // Assert
        Assert.Equal ("hello", typedText);
    }

    #endregion

    #region Example 5: Testing Ctrl+Key Combinations

    /// <summary>
    ///     Example 5: Testing keyboard combinations (Ctrl, Alt, Shift).
    ///     The new API handles ANSI encoding/decoding automatically.
    /// </summary>
    [Fact]
    public void Example5_KeyCombination_CtrlC ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.Clipboard = new FakeClipboard ();

        var ctrlCPressed = false;
        Key? receivedKey = null;

        app.Keyboard.KeyDown += (s, e) =>
                                {
                                    receivedKey = e;

                                    if (e == Key.C.WithCtrl)
                                    {
                                        ctrlCPressed = true;
                                    }
                                };

        // Act - Inject Ctrl+C
        app.InjectKey (Key.C.WithCtrl);

        // Assert - ANSI encoding of Ctrl+C worked correctly
        Assert.True (ctrlCPressed);
        Assert.Equal (Key.C.WithCtrl, receivedKey);
    }

    #endregion

    #region Example 6: Time-Dependent Behavior

    /// <summary>
    ///     Example 6: Testing time-dependent behavior with virtual time.
    ///     Shows how to test click timing thresholds without real delays.
    /// </summary>
    [Fact]
    public void Example6_DoubleClick_TimingThreshold ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);

        List<MouseFlags> clicks = [];

        app.Mouse.MouseEvent += (s, e) =>
                                {
                                    if (e.Flags.HasFlag (MouseFlags.LeftButtonClicked) || e.Flags.HasFlag (MouseFlags.LeftButtonDoubleClicked))
                                    {
                                        clicks.Add (e.Flags);
                                    }
                                };

        // Act - First click
        app.InjectMouse (new () { ScreenPosition = new (5, 5), Flags = MouseFlags.LeftButtonPressed, Timestamp = time.Now });
        app.InjectMouse (new () { ScreenPosition = new (5, 5), Flags = MouseFlags.LeftButtonReleased, Timestamp = time.Now });

        // Wait a short time (within double-click threshold)
        time.Advance (TimeSpan.FromMilliseconds (200));

        // Second click (should be detected as double-click)
        app.InjectMouse (new () { ScreenPosition = new (5, 5), Flags = MouseFlags.LeftButtonPressed, Timestamp = time.Now });
        app.InjectMouse (new () { ScreenPosition = new (5, 5), Flags = MouseFlags.LeftButtonReleased, Timestamp = time.Now });

        // Assert - Should have first click and then double-click
        Assert.Contains (clicks, f => f.HasFlag (MouseFlags.LeftButtonClicked));
        Assert.Contains (clicks, f => f.HasFlag (MouseFlags.LeftButtonDoubleClicked));
    }

    #endregion

    #region Example 7: Comparing Simplicity

    /// <summary>
    ///     Example 7: Complete example showing the simplified API.
    ///     This would have required 15+ lines with the old 3-step pattern.
    /// </summary>
    [Fact]
    public void Example7_SimplifiedAPI_Complete ()
    {
        // NEW API - Clean, simple, deterministic
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);

        var eventRaised = false;

        app.Keyboard.KeyDown += (s, e) => { eventRaised = true; };

        // Single call - everything handled automatically
        app.InjectKey (Key.Enter);

        Assert.True (eventRaised);

        // OLD API would have been:
        // app.Driver!.InjectKeyDownEvent(Key.Enter);  // Step 1
        // app.SimulateInputThread();                   // Step 2  
        // app.Driver.GetInputProcessor().ProcessQueue();  // Step 3
        //
        // 3 lines reduced to 1, and eliminates room for error!
    }

    #endregion

    #region Example 8: Mouse Click Helper Methods

    /// <summary>
    ///     Example 8: Using helper methods for common mouse click patterns.
    ///     These helpers encapsulate common sequences for cleaner test code.
    /// </summary>
    [Fact]
    public void Example8_MouseClickHelpers ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);

        using Runnable runnable = new ();
        app.Begin (runnable);

        Button button = new () { Text = "Click Me", X = 5, Y = 2 };
        runnable?.Add (button);

        var acceptingCalled = false;
        button.Accepting += (s, e) => acceptingCalled = true;

        // Act - Use helper for simple left-click at button position
        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (new Point (5, 2)));

        // Assert
        Assert.True (acceptingCalled);

        // Reset for next test
        acceptingCalled = false;

        // Right-click helper
        List<MouseFlags> receivedFlags = [];

        app.Mouse.MouseEvent += (s, e) => receivedFlags.Add (e.Flags);

        app.InjectSequence (InputInjectionExtensions.RightButtonClick (new Point (10, 5)));

        Assert.Contains (receivedFlags, f => f.HasFlag (MouseFlags.RightButtonClicked));

        // Double-click helper
        CheckBox checkBox = new () { Text = "_Check", X = 0, Y = 0 };
        runnable?.Add (checkBox);

        CheckState initialState = checkBox.Value;
        app.InjectSequence (InputInjectionExtensions.LeftButtonDoubleClick (new Point (0, 0)));

        // After double-click, state should have toggled twice (back to initial)
        Assert.NotEqual (initialState, checkBox.Value);

        runnable?.Dispose ();
    }

    #endregion

    #region Tests for https: //github.com/tui-cs/Terminal.Gui/issues/4675

    [Fact]
    public void Issue_4675_MakeInjectingDoubleClickEasier_WorksAsExpected ()
    {
        // This test reproduces the exact scenario from the issue
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using Runnable runnable = new ();
        app.Begin (runnable);

        CheckBox checkBox = new () { Text = "_Checkbox" };
        runnable?.Add (checkBox);

        CheckState initialState = checkBox.Value;

        // This is the simplified syntax requested in the issue
        app.InjectSequence (InputInjectionExtensions.LeftButtonDoubleClick (new Point (0, 0)));

        // After double-click, checkbox should have toggled twice (back to initial)
        Assert.NotEqual (initialState, checkBox.Value);

        runnable?.Dispose ();
    }

    [Fact]
    public void Issue_4675_LeftButtonClick_WorksAsExpected ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using Runnable runnable = new ();
        app.Begin (runnable);

        Button button = new () { Text = "Click Me" };
        runnable?.Add (button);

        var acceptingCalled = false;
        button.Accepting += (s, e) => acceptingCalled = true;

        // Test the LeftButtonClick helper
        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (new Point (0, 0)));

        Assert.True (acceptingCalled);
        runnable?.Dispose ();
    }

    [Fact]
    public void Issue_4675_RightButtonClick_WorksAsExpected ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        List<MouseFlags> receivedFlags = [];
        app.Mouse.MouseEvent += (s, e) => receivedFlags.Add (e.Flags);

        // Test the RightButtonClick helper
        app.InjectSequence (InputInjectionExtensions.RightButtonClick (new Point (5, 5)));

        Assert.Contains (receivedFlags, f => f.HasFlag (MouseFlags.RightButtonPressed));
        Assert.Contains (receivedFlags, f => f.HasFlag (MouseFlags.RightButtonReleased));
        Assert.Contains (receivedFlags, f => f.HasFlag (MouseFlags.RightButtonClicked));
    }

    #endregion
}
