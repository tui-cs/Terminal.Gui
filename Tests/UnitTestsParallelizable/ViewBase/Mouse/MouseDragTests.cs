namespace ViewBaseTests.MouseTests;

/// <summary>
///     Parallelizable tests for mouse drag functionality on movable and resizable views.
///     These tests validate mouse drag behavior without Application.Init or global state.
/// </summary>
[Trait ("Category", "Input")]
public class MouseDragTests
{
    #region Movable View Drag Tests

    [Fact]
    public void MovableView_MouseDrag_UpdatesPosition ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        View superView = new () { Width = 50, Height = 50, App = app };

        View movableView = new ()
        {
            X = 10,
            Y = 10,
            Width = 10,
            Height = 10,
            Arrangement = ViewArrangement.Movable,
            BorderStyle = LineStyle.Single,
            App = app
        };

        superView.Add (movableView);

        // Add to a runnable so the views are part of the application
        var runnable = new Runnable { App = app, Frame = new Rectangle (0, 0, 80, 25) };
        runnable.Add (superView);
        app.Begin (runnable);

        // Simulate mouse press on border to start drag
        Mouse pressEvent = new ()
        {
            ScreenPosition = new Point (10, 10), // Screen position
            Flags = MouseFlags.LeftButtonPressed
        };

        // Act - Start drag
        app.Mouse.RaiseMouseEvent (pressEvent);

        // Simulate mouse drag
        Mouse dragEvent = new ()
        {
            ScreenPosition = new Point (15, 15), // New screen position
            Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport
        };

        app.Mouse.RaiseMouseEvent (dragEvent);

        // Assert - View should have moved
        Assert.Equal (15, movableView.Frame.X);
        Assert.Equal (15, movableView.Frame.Y);
        Assert.Equal (10, movableView.Frame.Width);
        Assert.Equal (10, movableView.Frame.Height);

        app.End (app.SessionStack!.First ());
        runnable.Dispose ();
        superView.Dispose ();
    }

    [Fact]
    public void MovableView_MouseDrag_WithSuperview_UsesCorrectCoordinates ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        View superView = new () { X = 5, Y = 5, Width = 50, Height = 50 };

        View movableView = new ()
        {
            X = 10,
            Y = 10,
            Width = 10,
            Height = 10,
            Arrangement = ViewArrangement.Movable,
            BorderStyle = LineStyle.Single
        };

        superView.Add (movableView);

        // Add to a runnable so the views are part of the application
        var runnable = new Runnable { App = app, Frame = new Rectangle (0, 0, 80, 25) };
        runnable.Add (superView);
        app.Begin (runnable);

        // Simulate mouse press on border
        Mouse pressEvent = new ()
        {
            ScreenPosition = new Point (15, 15), // 5+10 offset
            Flags = MouseFlags.LeftButtonPressed
        };

        app.Mouse.RaiseMouseEvent (pressEvent);

        // Simulate mouse drag
        Mouse dragEvent = new ()
        {
            ScreenPosition = new Point (18, 18), // Moved 3,3
            Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport
        };

        app.Mouse.RaiseMouseEvent (dragEvent);

        // Assert - View should have moved relative to superview
        Assert.Equal (13, movableView.Frame.X); // 10 + 3
        Assert.Equal (13, movableView.Frame.Y); // 10 + 3

        app.End (app.SessionStack!.First ());
        runnable.Dispose ();
        superView.Dispose ();
    }

    [Fact]
    public void MovableView_MouseRelease_StopsDrag ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        View superView = new () { Width = 50, Height = 50 };

        View movableView = new ()
        {
            X = 10,
            Y = 10,
            Width = 10,
            Height = 10,
            Arrangement = ViewArrangement.Movable,
            BorderStyle = LineStyle.Single
        };

        superView.Add (movableView);

        // Add to a runnable so the views are part of the application
        var runnable = new Runnable { App = app, Frame = new Rectangle (0, 0, 80, 25) };
        runnable.Add (superView);
        app.Begin (runnable);

        // Start drag
        Mouse pressEvent = new () { ScreenPosition = new Point (10, 10), Flags = MouseFlags.LeftButtonPressed };

        app.Mouse.RaiseMouseEvent (pressEvent);

        // Drag
        Mouse dragEvent = new () { ScreenPosition = new Point (15, 15), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport };

        app.Mouse.RaiseMouseEvent (dragEvent);

        // Release
        Mouse releaseEvent = new () { ScreenPosition = new Point (15, 15), Flags = MouseFlags.LeftButtonReleased };

        app.Mouse.RaiseMouseEvent (releaseEvent);

        // Assert - Position should remain at dragged location
        Assert.Equal (15, movableView.Frame.X);
        Assert.Equal (15, movableView.Frame.Y);

        app.End (app.SessionStack!.First ());
        runnable.Dispose ();
        superView.Dispose ();
    }

    #endregion

    #region Resizable View Drag Tests

    [Fact]
    public void ResizableView_RightResize_Drag_IncreasesWidth ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        View superView = new () { Width = 50, Height = 50 };

        View resizableView = new ()
        {
            X = 10,
            Y = 10,
            Width = 10,
            Height = 10,
            Arrangement = ViewArrangement.RightResizable,
            BorderStyle = LineStyle.Single
        };

        superView.Add (resizableView);

        // Add to a runnable so the views are part of the application
        var runnable = new Runnable { App = app, Frame = new Rectangle (0, 0, 80, 25) };
        runnable.Add (superView);
        app.Begin (runnable);

        // Simulate mouse press on right border
        Mouse pressEvent = new () { ScreenPosition = new Point (19, 15), Flags = MouseFlags.LeftButtonPressed };

        app.Mouse.RaiseMouseEvent (pressEvent);

        // Simulate mouse drag to the right
        Mouse dragEvent = new () { ScreenPosition = new Point (24, 15), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport };

        app.Mouse.RaiseMouseEvent (dragEvent);

        // Assert - Width should have increased
        Assert.Equal (10, resizableView.Frame.X); // X unchanged
        Assert.Equal (10, resizableView.Frame.Y); // Y unchanged
        Assert.Equal (15, resizableView.Frame.Width); // Width increased by 5
        Assert.Equal (10, resizableView.Frame.Height); // Height unchanged

        app.End (app.SessionStack!.First ());
        runnable.Dispose ();
        superView.Dispose ();
    }

    [Fact]
    public void ResizableView_BottomResize_Drag_IncreasesHeight ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        View superView = new () { Width = 50, Height = 50 };

        View resizableView = new ()
        {
            X = 10,
            Y = 10,
            Width = 10,
            Height = 10,
            Arrangement = ViewArrangement.BottomResizable,
            BorderStyle = LineStyle.Single
        };

        superView.Add (resizableView);

        // Add to a runnable so the views are part of the application
        var runnable = new Runnable { App = app, Frame = new Rectangle (0, 0, 80, 25) };
        runnable.Add (superView);
        app.Begin (runnable);

        // Simulate mouse press on bottom border
        Mouse pressEvent = new () { ScreenPosition = new Point (15, 19), Flags = MouseFlags.LeftButtonPressed };

        app.Mouse.RaiseMouseEvent (pressEvent);

        // Simulate mouse drag down
        Mouse dragEvent = new () { ScreenPosition = new Point (15, 24), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport };

        app.Mouse.RaiseMouseEvent (dragEvent);

        // Assert - Height should have increased
        Assert.Equal (10, resizableView.Frame.X); // X unchanged
        Assert.Equal (10, resizableView.Frame.Y); // Y unchanged
        Assert.Equal (10, resizableView.Frame.Width); // Width unchanged
        Assert.Equal (15, resizableView.Frame.Height); // Height increased by 5

        app.End (app.SessionStack!.First ());
        runnable.Dispose ();
        superView.Dispose ();
    }

    [Fact]
    public void ResizableView_LeftResize_Drag_MovesAndResizes ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        View superView = new () { Width = 50, Height = 50 };

        View resizableView = new ()
        {
            X = 10,
            Y = 10,
            Width = 10,
            Height = 10,
            Arrangement = ViewArrangement.LeftResizable,
            BorderStyle = LineStyle.Single
        };

        superView.Add (resizableView);

        // Add to a runnable so the views are part of the application
        var runnable = new Runnable { App = app, Frame = new Rectangle (0, 0, 80, 25) };
        runnable.Add (superView);
        app.Begin (runnable);

        // Simulate mouse press on left border
        Mouse pressEvent = new () { ScreenPosition = new Point (10, 15), Flags = MouseFlags.LeftButtonPressed };

        app.Mouse.RaiseMouseEvent (pressEvent);

        // Simulate mouse drag to the left
        Mouse dragEvent = new () { ScreenPosition = new Point (7, 15), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport };

        app.Mouse.RaiseMouseEvent (dragEvent);

        // Assert - Should move left and resize
        Assert.Equal (7, resizableView.Frame.X); // X moved left by 3
        Assert.Equal (10, resizableView.Frame.Y); // Y unchanged
        Assert.Equal (13, resizableView.Frame.Width); // Width increased by 3
        Assert.Equal (10, resizableView.Frame.Height); // Height unchanged

        app.End (app.SessionStack!.First ());
        runnable.Dispose ();
        superView.Dispose ();
    }

    [Fact]
    public void ResizableView_TopResize_Drag_MovesAndResizes ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        View superView = new () { Width = 50, Height = 50 };

        View resizableView = new ()
        {
            X = 10,
            Y = 10,
            Width = 10,
            Height = 10,
            Arrangement = ViewArrangement.TopResizable,
            BorderStyle = LineStyle.Single
        };

        superView.Add (resizableView);

        // Add to a runnable so the views are part of the application
        var runnable = new Runnable { App = app, Frame = new Rectangle (0, 0, 80, 25) };
        runnable.Add (superView);
        app.Begin (runnable);

        // Simulate mouse press on top border
        Mouse pressEvent = new () { ScreenPosition = new Point (15, 10), Flags = MouseFlags.LeftButtonPressed };

        app.Mouse.RaiseMouseEvent (pressEvent);

        // Simulate mouse drag up
        Mouse dragEvent = new () { ScreenPosition = new Point (15, 8), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport };

        app.Mouse.RaiseMouseEvent (dragEvent);

        // Assert - Should move up and resize
        Assert.Equal (10, resizableView.Frame.X); // X unchanged
        Assert.Equal (8, resizableView.Frame.Y); // Y moved up by 2
        Assert.Equal (10, resizableView.Frame.Width); // Width unchanged
        Assert.Equal (12, resizableView.Frame.Height); // Height increased by 2

        app.End (app.SessionStack!.First ());
        runnable.Dispose ();
        superView.Dispose ();
    }

    #endregion

    #region Corner Resize Tests

    [Fact]
    public void ResizableView_BottomRightCornerResize_Drag_ResizesBothDimensions ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        View superView = new () { Width = 50, Height = 50 };

        View resizableView = new ()
        {
            X = 10,
            Y = 10,
            Width = 10,
            Height = 10,
            Arrangement = ViewArrangement.BottomResizable | ViewArrangement.RightResizable,
            BorderStyle = LineStyle.Single
        };

        superView.Add (resizableView);

        // Add to a runnable so the views are part of the application
        var runnable = new Runnable { App = app, Frame = new Rectangle (0, 0, 80, 25) };
        runnable.Add (superView);
        app.Begin (runnable);

        // Simulate mouse press on bottom-right corner
        Mouse pressEvent = new () { ScreenPosition = new Point (19, 19), Flags = MouseFlags.LeftButtonPressed };

        app.Mouse.RaiseMouseEvent (pressEvent);

        // Simulate mouse drag diagonally
        Mouse dragEvent = new () { ScreenPosition = new Point (24, 24), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport };

        app.Mouse.RaiseMouseEvent (dragEvent);

        // Assert - Both dimensions should increase
        Assert.Equal (10, resizableView.Frame.X); // X unchanged
        Assert.Equal (10, resizableView.Frame.Y); // Y unchanged
        Assert.Equal (15, resizableView.Frame.Width); // Width increased by 5
        Assert.Equal (15, resizableView.Frame.Height); // Height increased by 5

        app.End (app.SessionStack!.First ());
        runnable.Dispose ();
        superView.Dispose ();
    }

    [Fact]
    public void ResizableView_TopLeftCornerResize_Drag_MovesAndResizes ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        View superView = new () { Width = 50, Height = 50 };

        View resizableView = new ()
        {
            X = 10,
            Y = 10,
            Width = 10,
            Height = 10,
            Arrangement = ViewArrangement.TopResizable | ViewArrangement.LeftResizable,
            BorderStyle = LineStyle.Single
        };

        superView.Add (resizableView);

        // Add to a runnable so the views are part of the application
        var runnable = new Runnable { App = app, Frame = new Rectangle (0, 0, 80, 25) };
        runnable.Add (superView);
        app.Begin (runnable);

        // Simulate mouse press on top-left corner
        Mouse pressEvent = new () { ScreenPosition = new Point (10, 10), Flags = MouseFlags.LeftButtonPressed };

        app.Mouse.RaiseMouseEvent (pressEvent);

        // Simulate mouse drag diagonally up and left
        Mouse dragEvent = new () { ScreenPosition = new Point (7, 8), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport };

        app.Mouse.RaiseMouseEvent (dragEvent);

        // Assert - Should move and resize
        Assert.Equal (7, resizableView.Frame.X); // X moved left by 3
        Assert.Equal (8, resizableView.Frame.Y); // Y moved up by 2
        Assert.Equal (13, resizableView.Frame.Width); // Width increased by 3
        Assert.Equal (12, resizableView.Frame.Height); // Height increased by 2

        app.End (app.SessionStack!.First ());
        runnable.Dispose ();
        superView.Dispose ();
    }

    #endregion

    #region Minimum Size Constraints

    [Fact]
    public void ResizableView_Drag_RespectsMinimumWidth ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        View superView = new () { Width = 50, Height = 50 };

        View resizableView = new ()
        {
            X = 10,
            Y = 10,
            Width = 10,
            Height = 10,
            Arrangement = ViewArrangement.LeftResizable,
            BorderStyle = LineStyle.Single
        };

        superView.Add (resizableView);

        // Add to a runnable so the views are part of the application
        var runnable = new Runnable { App = app, Frame = new Rectangle (0, 0, 80, 25) };
        runnable.Add (superView);
        app.Begin (runnable);

        // Try to drag far to the right (making width very small)
        Mouse dragEvent = new ()
        {
            Position = new Point (8, 5), // Drag 8 units right, would make width 2
            ScreenPosition = new Point (18, 15),
            Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport
        };

        // Act
        (resizableView.Border.View as BorderView)?.Arranger?.HandleDragOperation (dragEvent);

        // Assert - Width should be constrained to minimum
        // width = border thickness + margin right
        int expectedMinWidth = resizableView.Border.Thickness.Horizontal + resizableView.Margin.Thickness.Right;
        Assert.True (resizableView.Frame.Width >= expectedMinWidth);

        app.End (app.SessionStack!.First ());
        runnable.Dispose ();
        superView.Dispose ();
    }

    [Fact]
    public void ResizableView_Drag_RespectsMinimumHeight ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        View superView = new () { Width = 50, Height = 50 };

        View resizableView = new ()
        {
            X = 10,
            Y = 10,
            Width = 10,
            Height = 10,
            Arrangement = ViewArrangement.TopResizable,
            BorderStyle = LineStyle.Single
        };

        superView.Add (resizableView);

        // Add to a runnable so the views are part of the application
        var runnable = new Runnable { App = app, Frame = new Rectangle (0, 0, 80, 25) };
        runnable.Add (superView);
        app.Begin (runnable);

        // Try to drag far down (making height very small)
        Mouse dragEvent = new ()
        {
            Position = new Point (5, 8), // Drag 8 units down, would make height 2
            ScreenPosition = new Point (15, 18),
            Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport
        };

        // Act
        (resizableView.Border.View as BorderView)?.Arranger?.HandleDragOperation (dragEvent);

        // Assert - Height should be constrained to minimum
        int expectedMinHeight = resizableView.Border.Thickness.Vertical + resizableView.Margin.Thickness.Bottom;
        Assert.True (resizableView.Frame.Height >= expectedMinHeight);

        app.End (app.SessionStack!.First ());
        runnable.Dispose ();
        superView.Dispose ();
    }

    #endregion
}
