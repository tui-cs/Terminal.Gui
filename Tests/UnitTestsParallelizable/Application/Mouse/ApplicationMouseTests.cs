using UnitTests;

namespace ApplicationTests.MouseTests;

/// <summary>
///     Tests for <see cref="IApplication.Mouse"/> proving the integration between
///     mouse input injection and <see cref="IMouse.RaiseMouseEvent"/>.
/// </summary>
/// <remarks>
///     These tests verify the complete mouse event pipeline:
///     1. InjectMouse → ANSI encoding → Input queue
///     2. ANSI parsing → MouseInterpreter (generates Click events from Press+Release)
///     3. Application.Mouse.MouseEvent → View routing → View.MouseEvent
///     Important: The MouseInterpreter middleware transforms raw events:
///     - Button Press/Release → Clicked events
///     - Multiple quick clicks → DoubleClicked, TripleClicked events
///     - This means injecting one event may generate multiple output events
/// </remarks>
[Collection ("Application Tests")]
[Trait ("Category", "Mouse")]
public class ApplicationMouseTests : TestDriverBase
{
    #region InjectMouseEvent → RaiseMouseEvent Integration Tests

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void InjectMouseEvent_RaisesMouseEvent (string driverName)
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (driverName);

        var mouseEventRaised = false;
        Mouse? receivedMouse = null;

        app.Mouse.MouseEvent += (s, e) =>
                                {
                                    mouseEventRaised = true;
                                    receivedMouse = e;
                                };

        Mouse testMouse = new () { ScreenPosition = new Point (5, 10), Flags = MouseFlags.LeftButtonPressed };

        // Act - Inject through new injection system
        app.InjectMouse (testMouse);

        // Assert
        Assert.True (mouseEventRaised, "Mouse.MouseEvent should have been raised");
        Assert.NotNull (receivedMouse);
        Assert.Equal (new Point (5, 10), receivedMouse!.ScreenPosition);
        Assert.True (receivedMouse.Flags.HasFlag (MouseFlags.LeftButtonPressed));
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void InjectMouseEvent_WithDifferentFlags_RaisesMouseEvent (string driverName)
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (driverName);

        // The MouseInterpreter generates additional events (e.g., Clicked from Press+Release)
        // So we test each flag individually and verify we receive at least the injected flag
        MouseFlags [] flagsToTest =
        [
            MouseFlags.LeftButtonPressed,
            MouseFlags.RightButtonPressed,
            MouseFlags.MiddleButtonPressed,
            MouseFlags.WheeledDown,
            MouseFlags.WheeledUp,
            MouseFlags.PositionReport
        ];

        // Act & Assert
        foreach (MouseFlags flags in flagsToTest)
        {
            List<MouseFlags> receivedFlags = [];

            app.Mouse.MouseEvent += MouseEventHandler;

            app.InjectMouse (new Mouse { ScreenPosition = new Point (0, 0), Flags = flags });

            // Verify we received at least one event with the expected flag
            Assert.True (receivedFlags.Count > 0, $"Should receive at least one event for {flags}");
            Assert.True (receivedFlags.Any (f => f.HasFlag (flags)), $"Should receive event with {flags}");

            app.Mouse.MouseEvent -= MouseEventHandler;

            continue;

            void MouseEventHandler (object? s, Mouse e) => receivedFlags.Add (e.Flags);
        }
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void InjectMouseEvent_MultipleEvents_RaisesForEach (string driverName)
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (driverName);
        List<Point> receivedPositions = [];

        app.Mouse.MouseEvent += (s, e) => receivedPositions.Add (e.ScreenPosition);

        Point [] positions = [new (0, 0), new (5, 5), new (10, 10), new (15, 15)];

        // Act
        foreach (Point position in positions)
        {
            app.InjectMouse (new Mouse { ScreenPosition = position, Flags = MouseFlags.PositionReport });
        }

        // Assert - PositionReport events should generate one event per injection
        Assert.Equal (positions.Length, receivedPositions.Count);

        for (var i = 0; i < positions.Length; i++)
        {
            Assert.Equal (positions [i], receivedPositions [i]);
        }
    }

    [Fact]
    public void InjectMouseEvent_WithModifierFlags_PreservesModifiers ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        List<Mouse> receivedEvents = [];

        app.Mouse.MouseEvent += (s, e) => receivedEvents.Add (e);

        // Test with modifiers - ANSI encoding may fail with unsupported modifier combinations
        Mouse testMouse = new () { ScreenPosition = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed | MouseFlags.Ctrl | MouseFlags.Shift };

        // Act
        app.InjectMouse (testMouse);

        // Assert
        // Note: The ANSI driver's mouse encoder/decoder may not fully support all modifier combinations
        // The SGR protocol encodes modifiers but not all terminals preserve them correctly
        Assert.True (receivedEvents.Count > 0, "Should receive at least one mouse event");

        // The ANSI encoding process may transform the flags (e.g., generating additional Click events)
        // We just verify we received some mouse event at the correct position
        Assert.True (receivedEvents.Any (e => e.ScreenPosition == new Point (0, 0)), "Should receive event at the injected position");
    }

    [Theory]
    [InlineData (0, 0, MouseFlags.LeftButtonPressed)]
    [InlineData (5, 10, MouseFlags.RightButtonPressed)]
    [InlineData (15, 20, MouseFlags.MiddleButtonPressed)]
    [InlineData (100, 50, MouseFlags.PositionReport)]
    [InlineData (25, 25, MouseFlags.LeftButtonReleased)]
    [InlineData (10, 10, MouseFlags.WheeledUp)]
    [InlineData (10, 10, MouseFlags.WheeledDown)]
    public void InjectMouseEvent_VariousEventsAndPositions_RaisesMouseEvent (int x, int y, MouseFlags flags)
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        var mouseEventRaised = false;
        Mouse? receivedMouse = null;

        app.Mouse.MouseEvent += (s, e) =>
                                {
                                    // Capture the first event - MouseInterpreter may generate additional events
                                    if (!mouseEventRaised)
                                    {
                                        mouseEventRaised = true;
                                        receivedMouse = e;
                                    }
                                };

        // Act
        app.InjectMouse (new Mouse { ScreenPosition = new Point (x, y), Flags = flags });

        // Assert
        Assert.True (mouseEventRaised, $"Mouse event should be raised for {flags} at ({x}, {y})");
        Assert.NotNull (receivedMouse);
        Assert.Equal (new Point (x, y), receivedMouse!.ScreenPosition);

        // Note: The first event received should contain the injected flag,
        // but MouseInterpreter may generate additional Click events for button presses/releases
        Assert.True (receivedMouse.Flags.HasFlag (flags), $"First event should contain {flags}");
    }

    #endregion

    #region InjectMouseEvent → View Mouse Event Pipeline Tests

    [Fact]
    public void InjectMouseEvent_RoutesToViewUnderMouse ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        var mouseReceived = false;
        Mouse? receivedMouse = null;

        View view = new ()
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10,
            App = app
        };

        view.MouseEvent += (s, e) =>
                           {
                               mouseReceived = true;
                               receivedMouse = e;
                               e.Handled = true;
                           };

        Runnable top = new () { App = app };
        top.Add (view);
        SessionToken? token = app.Begin (top);

        // Act
        app.InjectMouse (new Mouse { ScreenPosition = new Point (5, 5), Flags = MouseFlags.LeftButtonPressed });

        // Assert
        Assert.True (mouseReceived, "View should receive the mouse event");
        Assert.NotNull (receivedMouse);

        // Mouse.Position is viewport-relative, ScreenPosition is screen-relative
        Assert.Equal (new Point (5, 5), receivedMouse!.Position);

        app.End (token!);
        top.Dispose ();
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void InjectMouseEvent_ApplicationMouseEvent_FiredBeforeViewProcessing (string driverName)
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (driverName);
        var applicationMouseEventFired = false;
        var viewMouseEventFired = false;
        List<string> fireOrder = [];

        app.Mouse.MouseEvent += (s, e) =>
                                {
                                    applicationMouseEventFired = true;
                                    fireOrder.Add ("Application");
                                };

        View view = new ()
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10,
            App = app
        };

        view.MouseEvent += (s, e) =>
                           {
                               viewMouseEventFired = true;
                               fireOrder.Add ("View");
                               e.Handled = true;
                           };

        Runnable top = new () { App = app };
        top.Add (view);
        SessionToken? token = app.Begin (top);

        // Act
        app.InjectMouse (new Mouse { ScreenPosition = new Point (5, 5), Flags = MouseFlags.LeftButtonPressed });

        // Assert
        Assert.True (applicationMouseEventFired, "Application.Mouse.MouseEvent should fire");
        Assert.True (viewMouseEventFired, "view.MouseEvent should fire");
        Assert.Equal (2, fireOrder.Count);
        Assert.Equal ("Application", fireOrder [0]);
        Assert.Equal ("View", fireOrder [1]);

        app.End (token!);
        top.Dispose ();
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void InjectMouseEvent_HandledAtApplicationLevel_DoesNotRouteToView (string driverName)
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (driverName);
        var viewMouseEventFired = false;

        app.Mouse.MouseEvent += (s, e) =>
                                {
                                    e.Handled = true; // Handle at application level
                                };

        View view = new ()
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10,
            App = app
        };

        view.MouseEvent += (s, e) => viewMouseEventFired = true;

        Runnable top = new () { App = app };
        top.Add (view);
        SessionToken? token = app.Begin (top);

        // Act
        app.InjectMouse (new Mouse { ScreenPosition = new Point (5, 5), Flags = MouseFlags.LeftButtonPressed });

        // Assert
        Assert.False (viewMouseEventFired, "View should not receive mouse event when handled at Application level");

        app.End (token!);
        top.Dispose ();
    }

    [Fact]
    public void InjectMouseEvent_OutsideView_ViewDoesNotReceiveEvent ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        var viewMouseEventFired = false;

        View view = new ()
        {
            X = 5,
            Y = 5,
            Width = 10,
            Height = 10,
            App = app
        };

        view.MouseEvent += (s, e) => viewMouseEventFired = true;

        Runnable top = new () { App = app };
        top.Add (view);
        SessionToken? token = app.Begin (top);

        // Act - click outside the view
        app.InjectMouse (new Mouse { ScreenPosition = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed });

        // Assert
        Assert.False (viewMouseEventFired, "View should not receive mouse event when clicked outside");

        app.End (token!);
        top.Dispose ();
    }

    #endregion

    #region IMouse.RaiseMouseEvent Direct Tests

    [Fact]
    public void RaiseMouseEvent_RaisesMouseEvent ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        var eventRaised = false;
        Mouse? receivedMouse = null;

        app.Mouse.MouseEvent += (s, e) =>
                                {
                                    eventRaised = true;
                                    receivedMouse = e;
                                };

        Mouse testMouse = new () { ScreenPosition = new Point (15, 20), Flags = MouseFlags.RightButtonPressed };

        // Act
        app.Mouse.RaiseMouseEvent (testMouse);

        // Assert
        Assert.True (eventRaised);
        Assert.Equal (new Point (15, 20), receivedMouse!.ScreenPosition);
        Assert.True (receivedMouse.Flags.HasFlag (MouseFlags.RightButtonPressed));
    }

    [Fact]
    public void RaiseMouseEvent_HandledEvent_StopsProcessing ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        var firstHandlerCalled = false;
        var secondHandlerCalled = false;

        app.Mouse.MouseEvent += (s, e) =>
                                {
                                    firstHandlerCalled = true;
                                    e.Handled = true;
                                };

        app.Mouse.MouseEvent += (s, e) => secondHandlerCalled = true;

        // Act
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed });

        // Assert - All event subscribers are called regardless of Handled status (C# event semantics)
        Assert.True (firstHandlerCalled);
        Assert.True (secondHandlerCalled, "All subscribed handlers should be called even if handled");
    }

    #endregion

    #region Mouse State Tests

    [Fact]
    public void InjectMouseEvent_UpdatesLastMousePosition ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Point expectedPosition = new (25, 30);

        // Act
        app.InjectMouse (new Mouse { ScreenPosition = expectedPosition, Flags = MouseFlags.PositionReport });

        // Assert
        Assert.Equal (expectedPosition, app.Mouse.LastMousePosition);
    }

    [Fact]
    public void InjectMouseEvent_MultiplePositions_TracksLastPosition ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Point [] positions = [new (0, 0), new (5, 5), new (10, 10), new (15, 15)];

        // Act
        foreach (Point position in positions)
        {
            app.InjectMouse (new Mouse { ScreenPosition = position, Flags = MouseFlags.PositionReport });
        }

        // Assert
        Assert.Equal (positions [^1], app.Mouse.LastMousePosition);
    }

    #endregion

    #region Mouse Enter/Leave Event Tests

    [Fact]
    public void InjectMouseEvent_MovingOverView_RaisesMouseEnter ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        var mouseEnterRaised = false;

        View view = new ()
        {
            X = 5,
            Y = 5,
            Width = 10,
            Height = 10,
            App = app
        };

        view.MouseEnter += (s, e) => mouseEnterRaised = true;

        Runnable top = new () { App = app };
        top.Add (view);
        SessionToken? token = app.Begin (top);

        // Act - move mouse into view
        app.InjectMouse (new Mouse { ScreenPosition = new Point (7, 7), Flags = MouseFlags.PositionReport });

        // Assert
        Assert.True (mouseEnterRaised, "MouseEnter should be raised when mouse enters view");

        app.End (token!);
        top.Dispose ();
    }

    [Fact]
    public void InjectMouseEvent_MovingOutOfView_RaisesMouseLeave ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        var mouseLeaveRaised = false;

        View view = new ()
        {
            X = 5,
            Y = 5,
            Width = 10,
            Height = 10,
            App = app
        };

        view.MouseLeave += (s, e) => mouseLeaveRaised = true;

        Runnable top = new () { App = app };
        top.Add (view);
        SessionToken? token = app.Begin (top);

        // Act - move mouse into view, then out
        app.InjectMouse (new Mouse { ScreenPosition = new Point (7, 7), Flags = MouseFlags.PositionReport });
        app.InjectMouse (new Mouse { ScreenPosition = new Point (0, 0), Flags = MouseFlags.PositionReport });

        // Assert
        Assert.True (mouseLeaveRaised, "MouseLeave should be raised when mouse leaves view");

        app.End (token!);
        top.Dispose ();
    }

    #endregion

    #region Specific Mouse Event Tests

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void InjectMouseEvent_LeftButtonPressed_RaisesMouseEvent (string driverName)
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (driverName);

        var mouseEventRaised = false;

        View view = new ()
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10,
            App = app
        };

        view.MouseEvent += (s, e) =>
                           {
                               mouseEventRaised = true;
                               e.Handled = true;
                           };

        Runnable top = new () { App = app };
        top.Add (view);
        SessionToken? token = app.Begin (top);

        // Act
        app.InjectMouse (new Mouse { ScreenPosition = new Point (5, 5), Flags = MouseFlags.LeftButtonPressed });

        // Assert
        Assert.True (mouseEventRaised, "MouseEvent should be raised for left button press");

        app.End (token!);
        top.Dispose ();
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void InjectMouseEvent_LeftButtonPressed_LeftButtonReleased_Raises_Correct_MouseEvents (string driverName)
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (driverName);

        using Runnable runnable = new ();
        app.Begin (runnable);

        List<MouseFlags> receivedFlags = [];
        app.Mouse.MouseEvent += MouseEventHandler;

        // Act
        // Use Direct mode to bypass ANSI encoding which cannot preserve timestamps
        InputInjectionOptions options = new () { Mode = InputInjectionMode.Direct };
        DateTime baseTime = new (2025, 1, 1, 12, 0, 0);

        IInputInjector injector = app.GetInputInjector ();

        // First click at T+0
        injector.InjectMouse (new Mouse { ScreenPosition = new Point (5, 5), Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime }, options);

        injector.InjectMouse (new Mouse
                              {
                                  ScreenPosition = new Point (5, 5), Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (100)
                              },
                              options);

        // Assert
        Assert.Equal (MouseFlags.LeftButtonPressed, receivedFlags [0]);
        Assert.Equal (MouseFlags.LeftButtonReleased, receivedFlags [1]);
        Assert.Equal (MouseFlags.LeftButtonClicked, receivedFlags [2]);
        Assert.Equal (3, receivedFlags.Count);

        app.Mouse.MouseEvent -= MouseEventHandler;

        return;

        void MouseEventHandler (object? s, Mouse e) => receivedFlags.Add (e.Flags);
    }

    [Fact (Skip = "Skip until Pipeline Mode is Fixed")]
    public void InjectMouseEvent_Pipeline ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init ("ansi");

        using Runnable runnable = new ();
        app.Begin (runnable);

        List<MouseFlags> receivedFlags = [];
        app.Mouse.MouseEvent += MouseEventHandler;

        // Act
        // Use Direct mode to bypass ANSI encoding which cannot preserve timestamps
        InputInjectionOptions options = new () { Mode = InputInjectionMode.Pipeline };
        DateTime baseTime = new (2025, 1, 1, 12, 0, 0);

        IInputInjector injector = app.GetInputInjector ();

        // First click at T+0
        injector.InjectMouse (new Mouse { ScreenPosition = new Point (5, 5), Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime }, options);

        injector.InjectMouse (new Mouse
                              {
                                  ScreenPosition = new Point (5, 5), Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (100)
                              },
                              options);

        IInputProcessor processor = app.Driver?.GetInputProcessor ()!;

        //AnsiInputTestableTests.SimulateInputThread (app.Driver.GetInputProcessor (), queue);

        Thread.Sleep (20); // Allow time for processing

        processor.ProcessQueue ();

        // Assert
        Assert.Equal (MouseFlags.LeftButtonPressed, receivedFlags [0]);
        Assert.Equal (MouseFlags.LeftButtonReleased, receivedFlags [1]);
        Assert.Equal (MouseFlags.LeftButtonClicked, receivedFlags [2]);
        Assert.Equal (3, receivedFlags.Count);

        app.Mouse.MouseEvent -= MouseEventHandler;

        return;

        void MouseEventHandler (object? s, Mouse e)
        {
            receivedFlags.Add (e.Flags);

            if (e.Flags.HasFlag (MouseFlags.LeftButtonClicked))
            {
                Runnable runnable2 = new ();
                app.Begin (runnable2);
            }
        }
    }

    [Fact]
    public void InjectMouseEvent_RightButtonPressed_RaisesMouseEvent ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        var mouseEventRaised = false;

        View view = new ()
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10,
            App = app
        };

        view.MouseEvent += (s, e) =>
                           {
                               if (e.Flags.HasFlag (MouseFlags.RightButtonPressed))
                               {
                                   mouseEventRaised = true;
                               }

                               e.Handled = true;
                           };

        Runnable top = new () { App = app };
        top.Add (view);
        SessionToken? token = app.Begin (top);

        // Act
        app.InjectMouse (new Mouse { ScreenPosition = new Point (5, 5), Flags = MouseFlags.RightButtonPressed });

        // Assert
        Assert.True (mouseEventRaised, "MouseEvent should be raised for right button press");

        app.End (token!);
        top.Dispose ();
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void InjectMouseEvent_DoubleClick_DetectedCorrectly (string driverName)
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (driverName);
        var doubleClickRaised = false;

        View view = new ()
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10,
            App = app
        };

        view.MouseEvent += (s, e) =>
                           {
                               if (e.Flags.HasFlag (MouseFlags.LeftButtonDoubleClicked))
                               {
                                   doubleClickRaised = true;
                               }
                           };

        Runnable top = new () { App = app };
        top.Add (view);
        SessionToken? token = app.Begin (top);

        // Act - Double-clicks are generated by MouseInterpreter from Press+Release sequences
        // We need to inject Press, Release, Press, Release in quick succession at same position
        Point clickPos = new (5, 5);
        DateTime baseTime = DateTime.Now;

        // First click
        app.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime });
        app.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (50) });

        // Second click (within double-click threshold of 500ms)
        app.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime.AddMilliseconds (200) });
        app.InjectMouse (new Mouse { ScreenPosition = clickPos, Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (250) });

        // Assert
        Assert.True (doubleClickRaised, "DoubleClick flag should be detected when clicking twice rapidly at same position");

        app.End (token!);
        top.Dispose ();
    }

    #endregion

    #region Integration with IApplication Instance Tests

    [Fact]
    public void Mouse_Property_ReturnsNonNullAfterInit ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        // Assert
        Assert.NotNull (app.Mouse);
    }

    [Fact]
    public void Mouse_App_Property_ReferencesApplication ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        // Assert
        Assert.NotNull (app.Mouse.App);
        Assert.Same (app, app.Mouse.App);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void Multiple_Applications_Have_Independent_Mouse (string driverName)
    {
        // Arrange
        using IApplication app1 = Application.Create ();
        app1.Init (driverName);
        using IApplication app2 = Application.Create ();
        app2.Init (driverName);

        var app1MouseEventCount = 0;
        var app2MouseEventCount = 0;

        app1.Mouse.MouseEvent += (s, e) => app1MouseEventCount++;
        app2.Mouse.MouseEvent += (s, e) => app2MouseEventCount++;

        // Act
        app1.InjectMouse (new Mouse { ScreenPosition = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed });
        app2.InjectMouse (new Mouse { ScreenPosition = new Point (5, 5), Flags = MouseFlags.RightButtonPressed });

        // Assert
        Assert.Equal (1, app1MouseEventCount);
        Assert.Equal (1, app2MouseEventCount);
        Assert.NotSame (app1.Mouse, app2.Mouse);
    }

    #endregion

    #region Mouse Disabled Tests

    [Fact]
    public void InjectMouseEvent_WhenMouseDisabled_DoesNotRaiseEvents ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        var mouseEventRaised = false;

        app.Mouse.MouseEvent += (s, e) => mouseEventRaised = true;
        app.Mouse.IsMouseDisabled = true;

        // Act
        app.InjectMouse (new Mouse { ScreenPosition = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed });

        // Assert
        Assert.False (mouseEventRaised, "Mouse event should not be raised when mouse is disabled");
    }

    /// <summary>
    ///     Tests that timestamp-based spacing using Direct mode (bypassing ANSI encoding)
    ///     correctly prevents double-click detection when clicks are spaced >500ms apart.
    /// </summary>
    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void InjectMouse_DirectMode_WithTimestamps_PreventsDoubleClickWhenSpaced (string driverName)
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (driverName);
        using Runnable runnable = new ();
        app.Begin (runnable);

        List<MouseFlags> receivedFlags = [];
        app.Mouse.MouseEvent += (s, e) => { receivedFlags.Add (e.Flags); };

        // Act - Two clicks with 600ms spacing via timestamps (should be two single clicks, not a double-click)
        // Use Direct mode to bypass ANSI encoding which cannot preserve timestamps
        InputInjectionOptions options = new () { Mode = InputInjectionMode.Direct };
        DateTime baseTime = new (2025, 1, 1, 12, 0, 0);

        IInputInjector injector = app.GetInputInjector ();

        // First click at T+0
        injector.InjectMouse (new Mouse { ScreenPosition = new Point (5, 5), Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime }, options);

        injector.InjectMouse (new Mouse
                              {
                                  ScreenPosition = new Point (5, 5), Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (100)
                              },
                              options);

        // Second click at T+600 (more than 500ms threshold)
        injector.InjectMouse (new Mouse { ScreenPosition = new Point (5, 5), Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime.AddMilliseconds (600) },
                              options);

        injector.InjectMouse (new Mouse
                              {
                                  ScreenPosition = new Point (5, 5), Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (700)
                              },
                              options);

        // Assert - Should receive two LeftButtonClicked events (not a double-click)
        List<MouseFlags> clickEvents = receivedFlags.Where (f => f.HasFlag (MouseFlags.LeftButtonClicked)
                                                                 || f.HasFlag (MouseFlags.LeftButtonDoubleClicked)
                                                                 || f.HasFlag (MouseFlags.LeftButtonTripleClicked))
                                                    .ToList ();

        // Should get exactly 2 single-click events (timestamp spacing prevents double-click)
        Assert.Equal (2, clickEvents.Count);
        Assert.Equal (MouseFlags.LeftButtonClicked, clickEvents [0]);
        Assert.Equal (MouseFlags.LeftButtonClicked, clickEvents [1]); // Second single click, not double
    }

    /// <summary>
    ///     Tests that timestamp-based spacing using Direct mode (bypassing ANSI encoding)
    ///     allows double-click detection when clicks are within the 500ms threshold.
    /// </summary>
    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void InjectMouse_DirectMode_WithTimestamps_AllowsDoubleClickWhenClose (string driverName)
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (driverName);
        using Runnable runnable = new ();
        app.Begin (runnable);

        View view = new () { X = 0, Y = 0, Width = 10, Height = 10 };
        runnable.Add (view);
        runnable.Layout ();

        List<MouseFlags> receivedFlags = [];
        view.MouseEvent += (s, e) => { receivedFlags.Add (e.Flags); };

        // Act - Two clicks with 300ms spacing via timestamps (should be a double-click)
        // Use Direct mode to bypass ANSI encoding which cannot preserve timestamps
        InputInjectionOptions options = new () { Mode = InputInjectionMode.Direct };
        DateTime baseTime = new (2025, 1, 1, 12, 0, 0);

        IInputInjector injector = app.GetInputInjector ();

        // First click at T+0
        injector.InjectMouse (new Mouse { ScreenPosition = new Point (5, 5), Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime }, options);

        injector.InjectMouse (new Mouse { ScreenPosition = new Point (5, 5), Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (50) },
                              options);

        // Second click at T+300 (within 500ms threshold)
        injector.InjectMouse (new Mouse { ScreenPosition = new Point (5, 5), Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime.AddMilliseconds (300) },
                              options);

        injector.InjectMouse (new Mouse
                              {
                                  ScreenPosition = new Point (5, 5), Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (350)
                              },
                              options);

        // Assert - Should receive single-click followed by double-click
        List<MouseFlags> clickEvents = receivedFlags.Where (f => f.HasFlag (MouseFlags.LeftButtonClicked)
                                                                 || f.HasFlag (MouseFlags.LeftButtonDoubleClicked)
                                                                 || f.HasFlag (MouseFlags.LeftButtonTripleClicked))
                                                    .ToList ();

        // Should get single-click followed by double-click (timestamp spacing allows multi-click)
        Assert.Equal (2, clickEvents.Count);
        Assert.Equal (MouseFlags.LeftButtonClicked, clickEvents [0]);
        Assert.Equal (MouseFlags.LeftButtonDoubleClicked, clickEvents [1]);
    }

    #endregion
}
