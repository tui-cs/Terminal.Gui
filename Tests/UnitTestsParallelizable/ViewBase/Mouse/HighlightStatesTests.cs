using UnitTests;

namespace ViewBaseTests.MouseTests;

public class HighlightStatesTests (ITestOutputHelper output)
{
    [Theory]
    [CombinatorialData]
    public void View_MouseEvent_With_Press_Release_Gets_3 (MouseState mouseState)
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using Runnable runnable = new ();

        View view = new ()
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10,
            MouseHighlightStates = mouseState
        };

        List<MouseFlags> receivedFlags = [];
        view.MouseEvent += MouseEventHandler;

        runnable.Add (view);
        app.Begin (runnable);

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
        Assert.Equal (3, receivedFlags.Count);
        Assert.Equal (MouseFlags.LeftButtonPressed, receivedFlags [0]);
        Assert.Equal (MouseFlags.LeftButtonReleased, receivedFlags [1]);
        Assert.Equal (MouseFlags.LeftButtonClicked, receivedFlags [2]);

        view.MouseEvent -= MouseEventHandler;

        return;

        void MouseEventHandler (object? s, Mouse e) => receivedFlags.Add (e.Flags);
    }

    [Theory]
    [CombinatorialData]
    public void View_MouseEvent_With_Press_Release_Raises_Activate (MouseState mouseState)
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using Runnable runnable = new ();

        View view = new ()
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10,
            MouseHighlightStates = mouseState
        };

        var activateCount = 0;
        view.Activating += (_, _) => activateCount++;

        runnable.Add (view);
        app.Begin (runnable);

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
        Assert.Equal (1, activateCount);
    }

    [Fact]
    public void SubView_With_Single_Runnable_WorkAsExpected ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        app.Driver?.SetScreenSize (6, 1);

        Attribute focus = new (ColorName16.White, ColorName16.Black, TextStyle.None);
        Attribute highlight = new (ColorName16.Blue, ColorName16.Black, TextStyle.Italic);

        Runnable superview = new () { Width = Dim.Fill (), Height = Dim.Fill () };
        superview.SetScheme (new Scheme { Focus = focus, Highlight = highlight });
        View view = new () { Width = Dim.Fill (), Height = Dim.Fill (), Text = "| Hi |", MouseHighlightStates = MouseState.In };
        superview.Add (view);

        app.Begin (superview);

        for (var i = 0; i < app.Driver?.Cols; i++)
        {
            Assert.Equal (focus, app.Driver.Contents? [0, i].Attribute);
        }

        DriverAssert.AssertDriverContentsAre ("| Hi |", output, app.Driver);

        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (2, 0), Flags = MouseFlags.PositionReport });
        app.LayoutAndDraw ();

        for (var i = 0; i < app.Driver?.Cols; i++)
        {
            Assert.Equal (highlight, app.Driver.Contents? [0, i].Attribute);
        }

        DriverAssert.AssertDriverContentsAre ("| Hi |", output, app.Driver);

        app.Dispose ();
    }

    [Fact]
    public void SubView_With_Multiple_Runnable_WorkAsExpected ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        app.Driver?.SetScreenSize (9, 5);

        Attribute focus = new (ColorName16.White, ColorName16.Black, TextStyle.None);
        Attribute highlight = new (ColorName16.Blue, ColorName16.Black, TextStyle.Italic);

        Runnable superview = new () { Width = Dim.Fill (), Height = Dim.Fill () };
        superview.SetScheme (new Scheme { Focus = focus, Highlight = highlight });
        View view = new () { Width = Dim.Fill (), Height = Dim.Fill (), Text = "| Hi |", MouseHighlightStates = MouseState.In };
        superview.Add (view);

        app.Begin (superview);

        Attribute normal = new (ColorName16.Green, ColorName16.Magenta, TextStyle.None);
        Attribute highlight2 = new (ColorName16.Red, ColorName16.Yellow, TextStyle.Italic);

        Runnable modalSuperview = new () { Y = 1, Width = 9, Height = 4, BorderStyle = LineStyle.Single };
        modalSuperview.SetScheme (new Scheme { Normal = normal, Highlight = highlight2 });
        View view2 = new () { Width = Dim.Fill (), Height = Dim.Fill (), Text = "| Hey |", MouseHighlightStates = MouseState.In };
        modalSuperview.Add (view2);

        app.Begin (modalSuperview);

        for (var i = 0; i < app.Driver?.Cols; i++)
        {
            Assert.Equal (focus, app.Driver.Contents? [0, i].Attribute);
        }

        for (var i = 0; i < app.Driver?.Cols; i++)
        {
            Assert.Equal (normal, app.Driver.Contents? [2, i].Attribute);
        }

        DriverAssert.AssertDriverContentsAre ("""
                                              | Hi |
                                              ┌───────┐
                                              │| Hey |│
                                              │       │
                                              └───────┘
                                              """,
                                              output,
                                              app.Driver);

        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (2, 2), Flags = MouseFlags.PositionReport });
        app.LayoutAndDraw ();

        for (var i = 0; i < app.Driver?.Cols; i++)
        {
            Assert.Equal (focus, app.Driver.Contents? [0, i].Attribute);
        }

        for (var i = 1; i < app.Driver?.Cols - 1; i++)
        {
            Assert.Equal (highlight2, app.Driver?.Contents? [2, i].Attribute);
        }

        DriverAssert.AssertDriverContentsAre ("""
                                              | Hi |
                                              ┌───────┐
                                              │| Hey |│
                                              │       │
                                              └───────┘
                                              """,
                                              output,
                                              app.Driver);

        app.Dispose ();
    }

    [Fact]
    public void PressAndHold_Then_MoveOver_AnotherView_Should_Not_Highlight_SecondView ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        app.Driver?.SetScreenSize (20, 1);

        Runnable runnable = new () { Width = Dim.Fill (), Height = Dim.Fill () };

        List<MouseState> view2States = [];

        // First view with MouseHighlightStates In and Pressed
        View view1 = new ()
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 1,
            Text = "View1",
            MouseHighlightStates = MouseState.In | MouseState.Pressed
        };

        // Second view with MouseHighlightStates In
        View view2 = new ()
        {
            X = 10,
            Y = 0,
            Width = 10,
            Height = 1,
            Text = "View2",
            MouseHighlightStates = MouseState.In
        };
        view2.MouseStateChanged += (_, e) => view2States.Add (e.Value);

        runnable.Add (view1, view2);
        app.Begin (runnable);

        // Initially both views should have MouseState.None
        Assert.Equal (MouseState.None, view1.MouseState);
        Assert.Equal (MouseState.None, view2.MouseState);

        // Press mouse button on view1
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (5, 0), Flags = MouseFlags.LeftButtonPressed });

        // view1 should have Pressed state (may also have In)
        Assert.True (view1.MouseState.HasFlag (MouseState.Pressed));
        Assert.Equal (MouseState.None, view2.MouseState);

        // Clear state tracking to focus on what happens next
        view2States.Clear ();

        // Move mouse over view2 while still holding the button down
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (15, 0), Flags = MouseFlags.PositionReport });

        // view2 should NOT be highlighted
        Assert.Equal (MouseState.None, view2.MouseState);
        Assert.Empty (view2States);

        // Move mouse within view2 to a different position (still holding button)
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (20, 1), Flags = MouseFlags.PositionReport });

        // CRITICAL: view2 should STILL not be highlighted - this will fail with current implementation
        Assert.Equal (MouseState.None, view2.MouseState);
        Assert.Empty (view2States);

        // Move mouse out of view2 (into empty space)
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (5, 2), Flags = MouseFlags.PositionReport });

        // view2 should still be None
        Assert.Equal (MouseState.None, view2.MouseState);
        Assert.Empty (view2States);

        // Move back into view2 at different position (still holding button)
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (25, 2), Flags = MouseFlags.PositionReport });

        // CRITICAL: view2 should STILL not be highlighted - this will fail with current implementation
        Assert.Equal (MouseState.None, view2.MouseState);

        // view2 should have had NO state changes throughout the entire drag operation
        Assert.Empty (view2States);

        app.Dispose ();
    }

    [Fact]
    public void PressAndHold_Drag_Into_And_Within_AnotherView_Should_Not_Highlight_SecondView ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        app.Driver?.SetScreenSize (30, 3);

        Runnable runnable = new () { Width = Dim.Fill (), Height = Dim.Fill () };

        List<MouseState> view2States = [];

        // First view with MouseHighlightStates In and Pressed
        View view1 = new ()
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 1,
            Text = "View1",
            MouseHighlightStates = MouseState.In | MouseState.Pressed
        };

        // Second view with MouseHighlightStates In
        View view2 = new ()
        {
            X = 10,
            Y = 0,
            Width = 20,
            Height = 3,
            Text = "View2",
            MouseHighlightStates = MouseState.In
        };
        view2.MouseStateChanged += (_, e) => view2States.Add (e.Value);

        runnable.Add (view1, view2);
        app.Begin (runnable);

        // Initially both views should have MouseState.None
        Assert.Equal (MouseState.None, view1.MouseState);
        Assert.Equal (MouseState.None, view2.MouseState);

        // Press mouse button on view1
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (5, 0), Flags = MouseFlags.LeftButtonPressed });

        // view1 should have Pressed state (may also have In)
        Assert.True (view1.MouseState.HasFlag (MouseState.Pressed));
        Assert.Equal (MouseState.None, view2.MouseState);

        // Clear state tracking to focus on what happens next
        view2States.Clear ();

        // Move mouse over view2 while still holding the button down
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (15, 0), Flags = MouseFlags.PositionReport });

        // view2 should NOT be highlighted
        Assert.Equal (MouseState.None, view2.MouseState);
        Assert.Empty (view2States);

        // Move mouse within view2 to a different position (still holding button)
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (20, 1), Flags = MouseFlags.PositionReport });

        // CRITICAL: view2 should STILL not be highlighted - this will fail with current implementation
        Assert.Equal (MouseState.None, view2.MouseState);
        Assert.Empty (view2States);

        // Move mouse out of view2 (into empty space)
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (5, 2), Flags = MouseFlags.PositionReport });

        // view2 should still be None
        Assert.Equal (MouseState.None, view2.MouseState);
        Assert.Empty (view2States);

        // Move back into view2 at different position (still holding button)
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (25, 2), Flags = MouseFlags.PositionReport });

        // CRITICAL: view2 should STILL not be highlighted - this will fail with current implementation
        Assert.Equal (MouseState.None, view2.MouseState);

        // view2 should have had NO state changes throughout the entire drag operation
        Assert.Empty (view2States);

        app.Dispose ();
    }

    [Fact]
    public void PressOnView1_DragToView2_Release_Should_Not_Affect_View2_Until_After_Release ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        app.Driver?.SetScreenSize (20, 1);

        Runnable runnable = new () { Width = Dim.Fill (), Height = Dim.Fill () };

        List<MouseState> view2States = [];

        // First view with MouseHighlightStates In and Pressed
        View view1 = new ()
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 1,
            Text = "View1",
            MouseHighlightStates = MouseState.In | MouseState.Pressed
        };

        // Second view with MouseHighlightStates In
        View view2 = new ()
        {
            X = 10,
            Y = 0,
            Width = 10,
            Height = 1,
            Text = "View2",
            MouseHighlightStates = MouseState.In
        };
        view2.MouseStateChanged += (_, e) => view2States.Add (e.Value);

        runnable.Add (view1, view2);
        app.Begin (runnable);

        // Press on view1
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (5, 0), Flags = MouseFlags.LeftButtonPressed });

        // Drag to view2
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (15, 0), Flags = MouseFlags.PositionReport });

        // view2 should not be highlighted during drag
        Assert.Equal (MouseState.None, view2.MouseState);
        Assert.Empty (view2States);

        // Release button while over view2
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (15, 0), Flags = MouseFlags.LeftButtonReleased });

        // Send Clicked event (this is what triggers ungrab)
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (15, 0), Flags = MouseFlags.LeftButtonClicked });

        // After clicked (which ungrabs), view2 should get MouseState.In (mouse is now over it and no button is pressed)
        Assert.Equal (MouseState.In, view2.MouseState);
        Assert.Single (view2States);
        Assert.Equal (MouseState.In, view2States [0]);

        app.Dispose ();
    }

    [Fact]
    public void PressOnView1_DragToView2_MoveWithinView2_Should_Not_Change_View2_State ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        app.Driver?.SetScreenSize (20, 2);

        Runnable runnable = new () { Width = Dim.Fill (), Height = Dim.Fill () };

        List<string> view2Events = [];
        var view2EnterCalled = false;

        // First view with MouseHighlightStates In and Pressed
        View view1 = new ()
        {
            Id = "view1",
            X = 0,
            Y = 0,
            Width = 10,
            Height = 1,
            Text = "View1",
            MouseHighlightStates = MouseState.In | MouseState.Pressed
        };

        // Second view with MouseHighlightStates In
        View view2 = new ()
        {
            Id = "view2",
            X = 10,
            Y = 0,
            Width = 10,
            Height = 2,
            Text = "View2",
            MouseHighlightStates = MouseState.In
        };
        view2.MouseStateChanged += (_, e) => { view2Events.Add ($"StateChanged: {e.Value}"); };

        view2.MouseEnter += (_, _) =>
                            {
                                view2EnterCalled = true;
                                view2Events.Add ("MouseEnter");
                            };
        view2.MouseLeave += (_, _) => view2Events.Add ("MouseLeave");

        runnable.Add (view1, view2);
        app.Begin (runnable);

        // Press on view1
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (5, 0), Flags = MouseFlags.LeftButtonPressed });

        // Verify view1 grabbed the mouse
        Assert.True (app.Mouse.IsGrabbed (view1));

        // Clear tracking
        view2Events.Clear ();
        view2EnterCalled = false;

        // Drag to view2 position (15, 0) - button still held
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (15, 0), Flags = MouseFlags.PositionReport });

        // CRITICAL: view2 should NOT receive MouseEnter event
        Assert.False (view2EnterCalled, "view2 received MouseEnter event during drag from view1");
        Assert.Empty (view2Events);
        Assert.Equal (MouseState.None, view2.MouseState);

        // Move WITHIN view2 to position (15, 1) - still holding button
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (15, 1), Flags = MouseFlags.PositionReport });

        // CRITICAL: view2 should STILL not receive any events
        Assert.False (view2EnterCalled, "view2 received MouseEnter event while dragging within it");
        Assert.Empty (view2Events);
        Assert.Equal (MouseState.None, view2.MouseState);

        // Move OUT of view2 back to view1 area - still holding button
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (5, 0), Flags = MouseFlags.PositionReport });

        // view2 should still have no events (it was never "in" so no "leave")
        Assert.False (view2EnterCalled);
        Assert.Empty (view2Events);
        Assert.Equal (MouseState.None, view2.MouseState);

        // Move BACK into view2 - still holding button
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (15, 0), Flags = MouseFlags.PositionReport });

        // CRITICAL: view2 should STILL not receive any events
        Assert.False (view2EnterCalled, "view2 received MouseEnter event when re-entering during drag");
        Assert.Empty (view2Events);
        Assert.Equal (MouseState.None, view2.MouseState);

        app.Dispose ();
    }
}
