#nullable disable

namespace ViewsTests;

/// <summary>
///     Pure unit tests for <see cref="Button"/> that don't require Application static dependencies.
///     These tests can run in parallel without interference.
/// </summary>
public class ButtonTests
{
    [Fact]
    public void Accept_Cancel_Event_OnAccept_Returns_True ()
    {
        var button = new Button ();
        var acceptInvoked = false;

        button.Accepting += ButtonAccept;

        bool? ret = button.InvokeCommand (Command.Accept);
        Assert.True (ret);
        Assert.True (acceptInvoked);

        button.Dispose ();

        return;

        void ButtonAccept (object sender, CommandEventArgs e)
        {
            acceptInvoked = true;
            e.Handled = true;
        }
    }

    [Fact]
    public void Accept_Event_Returns_True ()
    {
        var btn = new Button { Text = "Test" };
        var acceptInvoked = false;

        btn.Accepting += (_, e) =>
                         {
                             acceptInvoked = true;
                             e.Handled = true;
                         };

        Assert.True (btn.InvokeCommand (Command.Accept));
        Assert.True (acceptInvoked);
    }

    [Theory]
    [InlineData (0, 0, 0, 0)]
    [InlineData (1, 0, 1, 0)]
    [InlineData (0, 1, 0, 1)]
    [InlineData (1, 1, 1, 1)]
    [InlineData (10, 1, 10, 1)]
    [InlineData (10, 3, 10, 3)]
    public void AbsoluteSize_DefaultText (int width, int height, int expectedWidth, int expectedHeight)
    {
        var btn1 = new Button ();
        btn1.ShadowStyle = null;
        btn1.Width = width;
        btn1.Height = height;

        Assert.Equal (new Size (expectedWidth, expectedHeight), btn1.Frame.Size);
        Assert.Equal (new Size (expectedWidth, expectedHeight), btn1.Viewport.Size);
        Assert.Equal (new Size (expectedWidth, expectedHeight), btn1.TextFormatter.ConstrainToSize);

        btn1.Dispose ();
    }

    [Theory]
    [InlineData ("01234", 0, 0, 0, 0)]
    [InlineData ("01234", 1, 0, 1, 0)]
    [InlineData ("01234", 0, 1, 0, 1)]
    [InlineData ("01234", 1, 1, 1, 1)]
    [InlineData ("01234", 10, 1, 10, 1)]
    [InlineData ("01234", 10, 3, 10, 3)]
    [InlineData ("0_1234", 0, 0, 0, 0)]
    [InlineData ("0_1234", 1, 0, 1, 0)]
    [InlineData ("0_1234", 0, 1, 0, 1)]
    [InlineData ("0_1234", 1, 1, 1, 1)]
    [InlineData ("0_1234", 10, 1, 10, 1)]
    [InlineData ("0_12你", 10, 3, 10, 3)]
    [InlineData ("0_12你", 0, 0, 0, 0)]
    [InlineData ("0_12你", 1, 0, 1, 0)]
    [InlineData ("0_12你", 0, 1, 0, 1)]
    [InlineData ("0_12你", 1, 1, 1, 1)]
    [InlineData ("0_12你", 10, 1, 10, 1)]
    public void AbsoluteSize_Text (string text, int width, int height, int expectedWidth, int expectedHeight)
    {
        var btn1 = new Button { ShadowStyle = null, Text = text, Width = width, Height = height };

        Assert.Equal (new Size (expectedWidth, expectedHeight), btn1.Frame.Size);
        Assert.Equal (new Size (expectedWidth, expectedHeight), btn1.Viewport.Size);
        Assert.Equal (new Size (expectedWidth, expectedHeight), btn1.GetContentSize ());
        Assert.Equal (new Size (expectedWidth, expectedHeight), btn1.TextFormatter.ConstrainToSize);

        btn1.Dispose ();
    }

    // Claude - Opus 4.5
    // Button does not raise Activating events. See Button.cs documentation.
    [Fact]
    public void Command_HotKey_RaisesAccepting_AndActivating ()
    {
        Button button = new () { Text = "_Test" };
        var activatingFired = false;
        var acceptingFired = false;

        button.Activating += (_, _) => activatingFired = true;

        button.Accepting += (_, e) =>
                            {
                                acceptingFired = true;
                                e.Handled = true;
                            };

        bool? result = button.InvokeCommand (Command.HotKey);

        // HotKey should raise Accepting and Activating
        Assert.True (activatingFired);
        Assert.True (acceptingFired);
        Assert.True (result);

        button.Dispose ();
    }

    // Claude - Opus 4.5
    // Button does not raise Activating events. See Button.cs documentation.
    [Fact]
    public void Enter_Raises_Accepting_Not_Activating ()
    {
        var superView = new View { CanFocus = true };
        Button button = new () { Text = "Test" };
        var activatingFired = false;
        var acceptingFired = false;

        button.Activating += (_, _) => activatingFired = true;

        button.Accepting += (_, e) =>
                            {
                                acceptingFired = true;
                                e.Handled = true;
                            };

        superView.Add (button);
        button.SetFocus ();

        superView.NewKeyDownEvent (Key.Enter);

        Assert.False (activatingFired);
        Assert.True (acceptingFired);

        superView.Dispose ();
    }

    [Fact]
    public void HotKeyChanged_EventFires ()
    {
        var btn = new Button { Text = "_Yar" };

        object sender = null;
        KeyChangedEventArgs args = null;

        btn.HotKeyChanged += (s, e) =>
                             {
                                 sender = s;
                                 args = e;
                             };

        btn.HotKeyChanged += (s, e) =>
                             {
                                 sender = s;
                                 args = e;
                             };

        btn.HotKey = KeyCode.R;
        Assert.Same (btn, sender);
        Assert.Equal (KeyCode.Y, args.OldKey);
        Assert.Equal (KeyCode.R, args.NewKey);
        btn.HotKey = KeyCode.R;
        Assert.Same (btn, sender);
        Assert.Equal (KeyCode.Y, args.OldKey);
        Assert.Equal (KeyCode.R, args.NewKey);
        btn.Dispose ();
    }

    [Fact]
    public void HotKeyChanged_EventFires_WithNone ()
    {
        var btn = new Button ();

        object sender = null;
        KeyChangedEventArgs args = null;

        btn.HotKeyChanged += (s, e) =>
                             {
                                 sender = s;
                                 args = e;
                             };

        btn.HotKey = KeyCode.R;
        Assert.Same (btn, sender);
        Assert.Equal (KeyCode.Null, args.OldKey);
        Assert.Equal (KeyCode.R, args.NewKey);
        btn.Dispose ();
    }

    // Claude - Opus 4.5
    // Button does not raise Activating events. See Button.cs documentation.
    [Fact]
    public void Space_Raises_Accepting_Not_Activating ()
    {
        var superView = new View { CanFocus = true };
        Button button = new () { Text = "Test" };
        var activatingFired = false;
        var acceptingFired = false;

        button.Activating += (_, _) => activatingFired = true;

        button.Accepting += (_, e) =>
                            {
                                acceptingFired = true;
                                e.Handled = true;
                            };

        superView.Add (button);
        button.SetFocus ();

        superView.NewKeyDownEvent (Key.Space);

        Assert.False (activatingFired);
        Assert.True (acceptingFired);

        superView.Dispose ();
    }

    [Theory]
    [InlineData (false, 0)]
    [InlineData (true, 1)]
    public void Enter_Fires_Accept (bool focused, int expected)
    {
        var superView = new View { CanFocus = true };

        Button button = new ();

        button.CanFocus = focused;

        var acceptInvoked = 0;
        button.Accepting += (_, _) => acceptInvoked++;

        superView.Add (button);
        button.SetFocus ();
        Assert.Equal (focused, button.HasFocus);

        superView.NewKeyDownEvent (Key.Enter);

        Assert.Equal (expected, acceptInvoked);

        superView.Dispose ();
    }

    [Fact]
    public void HotKey_Command_Accepts ()
    {
        var btn = new Button { Text = "_Test" };
        var accepted = false;
        btn.Accepting += (_, _) => accepted = true;

        Assert.Equal (KeyCode.T, btn.HotKey);
        btn.InvokeCommand (Command.HotKey);
        Assert.True (accepted);
    }

    [Theory]
    [InlineData (false, 1)]
    [InlineData (true, 1)]
    public void HotKey_Fires_Accept (bool focused, int expected)
    {
        var superView = new View { CanFocus = true };

        Button button = new () { HotKey = Key.A };

        button.CanFocus = focused;

        var acceptInvoked = 0;
        button.Accepting += (_, _) => acceptInvoked++;

        superView.Add (button);
        button.SetFocus ();
        Assert.Equal (focused, button.HasFocus);

        superView.NewKeyDownEvent (Key.A);

        Assert.Equal (expected, acceptInvoked);

        superView.Dispose ();
    }

    [Fact]
    public void HotKeyChange_Works ()
    {
        var clicked = false;
        var btn = new Button { Text = "_Test" };
        btn.Accepting += (_, _) => clicked = true;

        Assert.Equal (KeyCode.T, btn.HotKey);
        btn.NewKeyDownEvent (Key.T);
        Assert.True (clicked);

        clicked = false;
        btn.NewKeyDownEvent (Key.T.WithAlt);
        Assert.True (clicked);

        clicked = false;
        btn.HotKey = KeyCode.E;
        btn.NewKeyDownEvent (Key.E.WithAlt);
        Assert.True (clicked);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Verifies that LeftButtonClicked on a Button invokes Accept (not Activate).
    ///     Button binds LeftButtonClicked to Command.Accept, so only Accepting is raised.
    /// </summary>
    [Fact]
    public void LeftButtonClicked_Accepts ()
    {
        Button button = new () { Text = "_Button" };
        Assert.True (button.CanFocus);

        var activatingCount = 0;
        button.Activating += (_, _) => activatingCount++;

        var acceptingCount = 0;
        button.Accepting += (_, _) => acceptingCount++;

        button.HasFocus = true;
        Assert.True (button.HasFocus);
        Assert.Equal (0, activatingCount);
        Assert.Equal (0, acceptingCount);

        // LeftButtonClicked is bound to Command.Accept, so only Accepting fires (not Activating)
        button.NewMouseEvent (new Mouse { Position = new Point (1), Flags = MouseFlags.LeftButtonClicked });
        Assert.Equal (0, activatingCount);
        Assert.Equal (1, acceptingCount);

        button.NewMouseEvent (new Mouse { Position = new Point (1), Flags = MouseFlags.LeftButtonClicked });
        Assert.Equal (0, activatingCount);
        Assert.Equal (2, acceptingCount);

        // Disable Mouse Highlighting to test that it does not interfere with Accepting event
        button.MouseHighlightStates = MouseState.None;
        button.NewMouseEvent (new Mouse { Position = new Point (1), Flags = MouseFlags.LeftButtonClicked });
        Assert.Equal (0, activatingCount);
        Assert.Equal (3, acceptingCount);

        button.NewMouseEvent (new Mouse { Position = new Point (1), Flags = MouseFlags.LeftButtonClicked });
        Assert.Equal (0, activatingCount);
        Assert.Equal (4, acceptingCount);
    }

    [Fact]
    public void LeftButtonClicked_Accepts_Driver_Injection ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using Runnable runnable = new ();
        app.Begin (runnable);

        Button button = new ()
        {
            Text = "_Button",
            X = 0,
            Y = 0,
            Width = 10,
            Height = 3
        };
        runnable.Add (button);
        runnable.Layout ();

        var activatingCount = 0;
        button.Activating += (_, _) => activatingCount++;

        var acceptingCount = 0;
        button.Accepting += (_, _) => acceptingCount++;

        button.HasFocus = true;
        Assert.True (button.HasFocus);
        Assert.Equal (0, activatingCount);
        Assert.Equal (0, acceptingCount);

        // Use deterministic base time instead of DateTime.Now for predictable test behavior
        DateTime baseTime = new (2025, 1, 1, 12, 0, 0);
        Point clickPos = new (2, 1); // Inside button at screen coordinates (accounting for Runnable border)

        InputInjectionOptions options = new () { Mode = InputInjectionMode.Direct };
        IInputInjector injector = app.GetInputInjector ();

        // First click
        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime }, options);

        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (50) },
                              options);

        Assert.Equal (1, acceptingCount);
        Assert.Equal (0, activatingCount);

        // Second click - more than 500ms later to avoid double-click detection
        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime.AddMilliseconds (600) },
                              options);

        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (650) },
                              options);

        Assert.Equal (2, acceptingCount);
        Assert.Equal (0, activatingCount);
    }

    /// <summary>
    ///     Tests that Button's Accepting event fires correctly when MouseHighlightStates is disabled.
    ///     Button does not raise Activating events. When MouseHighlightStates = None,
    ///     only the Clicked event is processed, which fires Accepting only.
    ///     Uses Direct mode to bypass ANSI encoding which cannot preserve timestamps.
    /// </summary>
    [Fact]
    public void LeftButtonClicked_Accepts_Driver_Injection_With_MouseHighlightStates_None ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using Runnable runnable = new ();
        app.Begin (runnable);

        Button button = new ()
        {
            Text = "_Button",
            X = 0,
            Y = 0,
            Width = 10,
            Height = 3,
            MouseHighlightStates = MouseState.None // Disable auto-grab
        };
        runnable.Add (button);
        runnable.Layout ();

        var activatingCount = 0;
        button.Activating += (_, _) => activatingCount++;

        var acceptingCount = 0;
        button.Accepting += (_, _) => acceptingCount++;

        button.HasFocus = true;
        Assert.True (button.HasFocus);
        Assert.Equal (0, activatingCount);
        Assert.Equal (0, acceptingCount);

        // Use deterministic base time for predictable test behavior
        DateTime baseTime = new (2025, 1, 1, 12, 0, 0);
        Point clickPos = new (2, 1); // Inside button at screen coordinates (accounting for Runnable border)

        InputInjectionOptions options = new () { Mode = InputInjectionMode.Direct };
        IInputInjector injector = app.GetInputInjector ();

        // First click - Button does not raise Activating events
        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime }, options);

        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (50) },
                              options);

        // Activating: 0 (Button never raises Activating)
        // Accepting: 1 from Clicked
        Assert.Equal (0, activatingCount);
        Assert.Equal (1, acceptingCount);

        // Second click - more than 500ms later to avoid double-click detection
        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime.AddMilliseconds (600) },
                              options);

        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (650) },
                              options);

        // Activating: 0 (Button never raises Activating)
        // Accepting: 2
        Assert.Equal (0, activatingCount);
        Assert.Equal (2, acceptingCount);

        // Third click - verify it continues to work
        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime.AddMilliseconds (1200) },
                              options);

        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (1250) },
                              options);

        // Activating: 0 (Button never raises Activating)
        // Accepting: 3
        Assert.Equal (0, activatingCount);
        Assert.Equal (3, acceptingCount);

        // Fourth click - verify consistency
        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime.AddMilliseconds (1800) },
                              options);

        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (1850) },
                              options);

        // Activating: 0 (Button never raises Activating)
        // Accepting: 4
        Assert.Equal (0, activatingCount);
        Assert.Equal (4, acceptingCount);
    }

    /// <summary>
    ///     Tests that Button's Accepting event fires correctly when using Direct mode injection with timestamps.
    ///     Button does not raise Activating events.
    ///     Uses Direct mode to bypass ANSI encoding which cannot preserve timestamps.
    /// </summary>
    [Fact]
    public void LeftButtonClicked_Accepts_Driver_Injection_With_Timestamps ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Button button = new ()
        {
            X = 5,
            Y = 5,
            Width = 10,
            Height = 3,
            Text = "Click Me"
        };

        Runnable top = new () { App = app };
        top.Add (button);
        SessionToken token = app.Begin (top);
        button.HasFocus = true;

        var activatingCount = 0;
        var acceptingCount = 0;

        button.Activating += (_, _) => activatingCount++;
        button.Accepting += (_, _) => acceptingCount++;

        // Act - Inject a complete click sequence (Press -> Release) with timestamps using Direct mode
        DateTime baseTime = new (2025, 1, 1, 12, 0, 0);
        Point clickPos = new (7, 6); // Inside button at screen coordinates

        InputInjectionOptions options = new () { Mode = InputInjectionMode.Direct };
        IInputInjector injector = app.GetInputInjector ();

        // Inject Press and Release to generate a Clicked event
        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime }, options);

        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (50) },
                              options);

        // Assert - Button should receive Clicked event and fire Accepting only (not Activating)
        Assert.Equal (0, activatingCount);
        Assert.Equal (1, acceptingCount);

        // Act - Second click with timestamp spacing >500ms should be a new single click
        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime.AddMilliseconds (600) },
                              options);

        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (650) },
                              options);

        // Assert - Should fire again (two independent single clicks, not a double-click)
        Assert.Equal (0, activatingCount);
        Assert.Equal (2, acceptingCount);

        // Cleanup
        app.End (token!);
        top.Dispose ();
    }

    /// <summary>
    ///     Tests that Button receives double-click events correctly when using Direct mode injection with timestamps.
    ///     Uses Direct mode to bypass ANSI encoding which cannot preserve timestamps.
    /// </summary>
    [Fact]
    public void LeftButtonDoubleClicked_Accepts_Driver_Injection_With_Timestamps ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Button button = new ()
        {
            X = 5,
            Y = 5,
            Width = 10,
            Height = 3,
            Text = "Click Me"
        };

        Runnable top = new () { App = app };
        top.Add (button);
        SessionToken token = app.Begin (top);
        button.HasFocus = true;

        List<MouseFlags> receivedFlags = [];

        button.MouseEvent += (_, e) => { receivedFlags.Add (e.Flags); };

        // Act - Inject two clicks with <500ms spacing to generate a double-click using Direct mode
        DateTime baseTime = new (2025, 1, 1, 12, 0, 0);
        Point clickPos = new (7, 6); // Inside button at screen coordinates

        InputInjectionOptions options = new () { Mode = InputInjectionMode.Direct };
        IInputInjector injector = app.GetInputInjector ();

        // First click
        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime }, options);

        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (50) },
                              options);

        // Second click within 500ms threshold
        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime.AddMilliseconds (300) },
                              options);

        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (350) },
                              options);

        // Assert - Should receive a double-click event (timestamp spacing allows multi-click)
        // We don't assert exact count since View may emit additional events,
        // but we verify the double-click event was generated
        Assert.Contains (MouseFlags.LeftButtonDoubleClicked, receivedFlags);

        // Also verify we got the first single-click
        Assert.Contains (MouseFlags.LeftButtonClicked, receivedFlags);

        // Cleanup
        app.End (token!);
        top.Dispose ();
    }

    /// <summary>
    ///     Tests that MouseHoldRepeat button fires Accept on quick double-click.
    ///     Per spec: When MouseHoldRepeat=true, Press/Release events are used (Click events ignored).
    ///     Each Press/Release cycle fires exactly one Accept.
    /// </summary>
    [Fact]
    public void MouseHoldRepeat_QuickDoubleClick_FiresAcceptTwice ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Button button = new ()
        {
            X = 5,
            Y = 5,
            Width = 10,
            Height = 3,
            Text = "Click Me",
            MouseHoldRepeat = MouseFlags.LeftButtonReleased
        };

        Runnable top = new () { App = app };
        top.Add (button);
        SessionToken token = app.Begin (top);
        button.HasFocus = true;

        var acceptingCount = 0;
        button.Accepting += (_, _) => acceptingCount++;

        // Act - Quick double-click (both clicks within 500ms window)
        DateTime baseTime = new (2025, 1, 1, 12, 0, 0);
        Point clickPos = new (7, 6); // Inside button at screen coordinates

        InputInjectionOptions options = new () { Mode = InputInjectionMode.Direct };
        IInputInjector injector = app.GetInputInjector ();

        // First click
        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime }, options);

        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (50) },
                              options);

        // Second click within 500ms threshold (creates double-click, but we ignore Click events)
        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime.AddMilliseconds (300) },
                              options);

        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (350) },
                              options);

        // Assert - Should fire Accept twice (one per Press/Release cycle)
        // MouseHoldRepeat uses Press/Release events only, ignoring Click/DoubleClick synthesized events
        Assert.Equal (2, acceptingCount);

        // Cleanup
        app.End (token!);
        top.Dispose ();
    }

    /// <summary>
    ///     Tests Button.MouseHoldRepeat = MouseFlags.LeftButtonReleased behavior with quick single click.
    ///     Per spec: Quick click (press + immediate release within 100ms) should fire Accept once.
    ///     Uses Direct mode to bypass ANSI encoding and control timing precisely.
    /// </summary>
    [Fact]
    public void MouseHoldRepeat_QuickSingleClick_FiresAcceptOnce ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using Runnable runnable = new ();
        app.Begin (runnable);

        Button button = new ()
        {
            Text = "_Button",
            X = 0,
            Y = 0,
            Width = 10,
            Height = 3,
            MouseHoldRepeat = MouseFlags.LeftButtonReleased // Enable hold-repeat behavior
        };
        runnable.Add (button);
        runnable.Layout ();

        var acceptingCount = 0;
        button.Accepting += (_, _) => acceptingCount++;

        button.HasFocus = true;

        DateTime baseTime = new (2025, 1, 1, 12, 0, 0);
        Point clickPos = new (2, 1);

        InputInjectionOptions options = new () { Mode = InputInjectionMode.Direct };
        IInputInjector injector = app.GetInputInjector ();

        // Quick single click - press and release within 50ms (too fast for timer to start)
        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime }, options);

        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (50) },
                              options);

        // Should fire Accept once from the Clicked event
        Assert.Equal (1, acceptingCount);
    }

    /// <summary>
    ///     Tests that MouseHoldRepeat button fires Accept on quick triple-click.
    ///     Per spec: When MouseHoldRepeat=true, Press/Release events are used (Click events ignored).
    ///     Each Press/Release cycle fires exactly one Accept.
    /// </summary>
    [Fact]
    public void MouseHoldRepeat_QuickTripleClick_FiresAcceptThreeTimes ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Button button = new ()
        {
            X = 5,
            Y = 5,
            Width = 10,
            Height = 3,
            Text = "Click Me",
            MouseHoldRepeat = MouseFlags.LeftButtonReleased
        };

        Runnable top = new () { App = app };
        top.Add (button);
        SessionToken token = app.Begin (top);
        button.HasFocus = true;

        var acceptingCount = 0;
        button.Accepting += (_, _) => acceptingCount++;

        // Act - Quick triple-click (all clicks within multi-click threshold)
        DateTime baseTime = new (2025, 1, 1, 12, 0, 0);
        Point clickPos = new (7, 6); // Inside button at screen coordinates

        InputInjectionOptions options = new () { Mode = InputInjectionMode.Direct };
        IInputInjector injector = app.GetInputInjector ();

        // First click
        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime }, options);

        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (50) },
                              options);

        // Second click
        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime.AddMilliseconds (200) },
                              options);

        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (250) },
                              options);

        // Third click
        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime.AddMilliseconds (400) },
                              options);

        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (450) },
                              options);

        // Assert - Should fire Accept three times (one per Press/Release cycle)
        // MouseHoldRepeat uses Press/Release events only, ignoring Click/DoubleClick/TripleClick synthesized events
        Assert.Equal (3, acceptingCount);

        // Cleanup
        app.End (token!);
        top.Dispose ();
    }

    /// <summary>
    ///     Tests Button.MouseHoldRepeat = MouseFlags.LeftButtonReleased with spaced clicks (not a multi-click sequence).
    ///     Per spec: Clicks spaced >500ms apart should each fire Accept once independently.
    ///     Uses Direct mode to bypass ANSI encoding and control timing precisely.
    /// </summary>
    [Fact]
    public void MouseHoldRepeat_SpacedClicks_FiresAcceptForEach ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using Runnable runnable = new ();
        app.Begin (runnable);

        Button button = new ()
        {
            Text = "_Button",
            X = 0,
            Y = 0,
            Width = 10,
            Height = 3,
            MouseHoldRepeat = MouseFlags.LeftButtonReleased
        };
        runnable.Add (button);
        runnable.Layout ();

        var acceptingCount = 0;
        button.Accepting += (_, _) => acceptingCount++;

        button.HasFocus = true;

        DateTime baseTime = new (2025, 1, 1, 12, 0, 0);
        Point clickPos = new (2, 1);

        InputInjectionOptions options = new () { Mode = InputInjectionMode.Direct };
        IInputInjector injector = app.GetInputInjector ();

        // First click
        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime }, options);

        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (50) },
                              options);

        Assert.Equal (1, acceptingCount);

        // Second click - more than 500ms later (not a double-click)
        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime.AddMilliseconds (600) },
                              options);

        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (650) },
                              options);

        Assert.Equal (2, acceptingCount);

        // Third click - more than 500ms after second
        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime.AddMilliseconds (1200) },
                              options);

        injector.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (1250) },
                              options);

        Assert.Equal (3, acceptingCount);
    }

    [Fact]
    public void Setting_Empty_Text_Sets_HoKey_To_KeyNull ()
    {
        var btn = new Button { Text = "_Test" };

        Assert.Equal (KeyCode.T, btn.HotKey);

        btn.Text = "";

        Assert.Equal (KeyCode.Null, btn.HotKey);
    }

    [Theory]
    [InlineData (false, 0)]
    [InlineData (true, 1)]
    public void Space_Fires_Accept (bool focused, int expected)
    {
        var superView = new View { CanFocus = true };

        Button button = new ();

        button.CanFocus = focused;

        var acceptInvoked = 0;
        button.Accepting += (_, _) => acceptInvoked++;

        superView.Add (button);
        button.SetFocus ();
        Assert.Equal (focused, button.HasFocus);

        superView.NewKeyDownEvent (Key.Space);

        Assert.Equal (expected, acceptInvoked);

        superView.Dispose ();
    }

    [Fact]
    public void TestAssignTextToButton ()
    {
        var btn = new Button { Text = "_K Ok" };

        Assert.Equal ("_K Ok", btn.Text);

        btn.Text = "_N Btn";

        Assert.Equal ("_N Btn", btn.Text);
    }

    [Fact]
    public void Text_Mirrors_Title ()
    {
        var view = new Button ();
        view.Title = "Hello";
        Assert.Equal ("Hello", view.Title);
        Assert.Equal ("Hello", view.TitleTextFormatter.Text);

        Assert.Equal ("Hello", view.Text);
        Assert.Equal ($"{Glyphs.LeftBracket} Hello {Glyphs.RightBracket}", view.TextFormatter.Text);
        view.Dispose ();
    }

    [Fact]
    public void Title_Mirrors_Text ()
    {
        var view = new Button ();
        view.Text = "Hello";
        Assert.Equal ("Hello", view.Text);
        Assert.Equal ($"{Glyphs.LeftBracket} Hello {Glyphs.RightBracket}", view.TextFormatter.Text);

        Assert.Equal ("Hello", view.Title);
        Assert.Equal ("Hello", view.TitleTextFormatter.Text);
        view.Dispose ();
    }

    [Fact]
    public void HotKey_Command_Does_Raise_Activating ()
    {
        Button button = new () { Text = "_Test" };
        var activatingFired = false;

        button.Activating += (_, _) => activatingFired = true;

        button.InvokeCommand (Command.HotKey);

        Assert.True (activatingFired);

        button.Dispose ();
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Verifies that Button does NOT raise Activating event when Accept command is invoked.
    ///     Button only raises Accepting event.
    /// </summary>
    [Fact]
    public void Accept_Command_Does_Not_Raise_Activating ()
    {
        Button button = new () { Text = "Test" };
        var activatingFired = false;

        button.Activating += (_, _) => activatingFired = true;

        button.InvokeCommand (Command.Accept);

        Assert.False (activatingFired);

        button.Dispose ();
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Verifies that Button does NOT raise Activating event when mouse clicked.
    ///     This is the primary user interaction method, so it's important to verify
    ///     that Activating does not fire during clicks.
    /// </summary>
    [Fact]
    public void Mouse_Click_Does_Not_Raise_Activating ()
    {
        Button button = new () { Text = "Test" };
        var activatingFired = false;

        button.Activating += (_, _) => activatingFired = true;

        // Simulate left button clicked
        button.NewMouseEvent (new Mouse { Position = new Point (1), Flags = MouseFlags.LeftButtonClicked });

        Assert.False (activatingFired);

        button.Dispose ();
    }

    // Copilot
    /// <summary>
    ///     Verifies that a newly-constructed Button has shadow infrastructure allocated by default,
    ///     because <see cref="Button.RaiseInitializingShadowStyle"/> fires with
    ///     <see cref="Button.DefaultShadow"/> and no handler suppresses it.
    /// </summary>
    [Fact]
    public void ShadowStyle_Applied_In_Constructor_By_Default ()
    {
        Button button = new ();

        // Default shadow is applied in the constructor via the InitializingShadowStyle CWP event.
        Assert.Equal (Button.DefaultShadow, button.ShadowStyle);
        Assert.NotNull (button.Margin.View); // MarginView was created.

        button.Dispose ();
    }

    // Copilot
    /// <summary>
    ///     Verifies that setting <see cref="View.ShadowStyle"/> to <see langword="null"/> after construction
    ///     clears the shadow views while keeping the existing <c>Margin.View</c> allocated.
    /// </summary>
    [Fact]
    public void ShadowStyle_Null_After_Construction_Clears_ShadowViews_But_Keeps_MarginView ()
    {
        Button button = new ();
        View marginView = button.Margin.View;

        Assert.NotNull (marginView);
        Assert.NotEmpty (marginView.SubViews);

        button.ShadowStyle = null;

        Assert.Null (button.ShadowStyle);
        Assert.Same (marginView, button.Margin.View);
        Assert.Empty (button.Margin.View.SubViews);

        button.Dispose ();
    }

    /// <summary>
    ///     Verifies that <see cref="ScrollButton"/> does not create shadow infrastructure during construction
    ///     because its <see cref="Button.OnInitializingShadowStyle"/> override sets
    ///     <c>args.NewValue = null</c>, eliminating the create-then-destroy allocation pattern.
    /// </summary>
    [Fact]
    public void ScrollButton_OnInitializingShadowStyle_Suppresses_Shadow ()
    {
        ScrollButton btn = new ();

        // OnInitializingShadowStyle sets args.NewValue = null
        // → base.ShadowStyle = null is a no-op → no shadow infrastructure created.
        Assert.Null (btn.ShadowStyle);
        Assert.Null (btn.Margin.View);

        btn.Dispose ();
    }

    // Copilot
    /// <summary>
    ///     Verifies that explicitly setting <see cref="View.ShadowStyle"/> to a non-default value
    ///     after construction correctly replaces the initial default.
    /// </summary>
    [Fact]
    public void ShadowStyle_ExplicitValue_After_Construction_IsApplied ()
    {
        Button button = new ();
        button.ShadowStyle = ShadowStyles.Transparent;

        Assert.Equal (ShadowStyles.Transparent, button.ShadowStyle);

        button.Dispose ();
    }
}
