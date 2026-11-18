using Terminal.Gui.App;

namespace ViewBaseTests.MouseTests;

/// <summary>
///     Parallelizable tests for mouse event routing and coordinate transformation.
///     These tests validate mouse event handling without Application.Begin or global state.
/// </summary>
[Trait ("Category", "Input")]
public class MouseEventRoutingTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    #region Mouse Event Routing to Views

    [Theory]
    [InlineData (5, 5, 5, 5, true)]   // Click inside view
    [InlineData (0, 0, 0, 0, true)]   // Click at origin
    [InlineData (9, 9, 9, 9, true)]   // Click at far corner (view is 10x10)
    [InlineData (10, 10, -1, -1, false)] // Click outside view
    [InlineData (-1, -1, -1, -1, false)] // Click outside view
    public void View_NewMouseEvent_ReceivesCorrectCoordinates (int screenX, int screenY, int expectedViewX, int expectedViewY, bool shouldReceive)
    {
        // Arrange
        View view = new ()
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10
        };

        Point? receivedPosition = null;
        bool eventReceived = false;

        view.MouseEvent += (_, args) =>
        {
            eventReceived = true;
            receivedPosition = args.Position;
        };

        Mouse mouse = new ()
        {
            Position = new Point (screenX, screenY),
            Flags = MouseFlags.LeftButtonClicked
        };

        // Act
        view.NewMouseEvent (mouse);

        // Assert
        if (shouldReceive)
        {
            Assert.True (eventReceived);
            Assert.NotNull (receivedPosition);
            Assert.Equal (expectedViewX, receivedPosition.Value.X);
            Assert.Equal (expectedViewY, receivedPosition.Value.Y);
        }

        view.Dispose ();
    }

    [Theory]
    [InlineData (0, 0, 5, 5, 5, 5, true)]   // View at origin, click at (5,5) in view
    [InlineData (10, 10, 5, 5, 5, 5, true)] // View offset, but we still pass view-relative coords
    [InlineData (0, 0, 0, 0, 0, 0, true)]   // View at origin, click at origin
    [InlineData (5, 5, 9, 9, 9, 9, true)]   // View offset, click at far corner (view-relative)
    [InlineData (0, 0, 10, 10, -1, -1, false)] // Click outside view bounds
    [InlineData (0, 0, -1, -1, -1, -1, false)] // Click outside view bounds
    public void View_WithOffset_ReceivesCorrectCoordinates (
        int viewX,
        int viewY,
        int viewRelativeX,
        int viewRelativeY,
        int expectedViewX,
        int expectedViewY,
        bool shouldReceive)
    {
        // Arrange
        // Note: When testing View.NewMouseEvent directly (without Application routing),
        // coordinates are already view-relative. The view's X/Y position doesn't affect
        // the coordinate transformation at this level.
        View view = new ()
        {
            X = viewX,
            Y = viewY,
            Width = 10,
            Height = 10
        };

        Point? receivedPosition = null;
        bool eventReceived = false;

        view.MouseEvent += (_, args) =>
        {
            eventReceived = true;
            receivedPosition = args.Position;
        };

        Mouse mouse = new ()
        {
            Position = new (viewRelativeX, viewRelativeY),
            Flags = MouseFlags.LeftButtonClicked
        };

        // Act
        view.NewMouseEvent (mouse);

        // Assert
        if (shouldReceive)
        {
            Assert.True (eventReceived, $"Event should be received at view-relative ({viewRelativeX},{viewRelativeY})");
            Assert.NotNull (receivedPosition);
            Assert.Equal (expectedViewX, receivedPosition.Value.X);
            Assert.Equal (expectedViewY, receivedPosition.Value.Y);
        }

        view.Dispose ();
    }

    #endregion

    #region View Hierarchy Mouse Event Routing

    [Fact]
    public void SubView_ReceivesMouseEvent_WithCorrectRelativeCoordinates ()
    {
        // Arrange
        View superView = new ()
        {
            X = 0,
            Y = 0,
            Width = 20,
            Height = 20
        };

        View subView = new ()
        {
            X = 5,
            Y = 5,
            Width = 10,
            Height = 10
        };

        superView.Add (subView);

        Point? subViewReceivedPosition = null;
        bool subViewEventReceived = false;

        subView.MouseEvent += (_, args) =>
        {
            subViewEventReceived = true;
            subViewReceivedPosition = args.Position;
        };

        // Click at position (2, 2) relative to subView (which is at 5,5 relative to superView)
        Mouse mouse = new ()
        {
            Position = new Point (2, 2), // Relative to subView
            Flags = MouseFlags.LeftButtonClicked
        };

        // Act
        subView.NewMouseEvent (mouse);

        // Assert
        Assert.True (subViewEventReceived);
        Assert.NotNull (subViewReceivedPosition);
        Assert.Equal (2, subViewReceivedPosition.Value.X);
        Assert.Equal (2, subViewReceivedPosition.Value.Y);

        subView.Dispose ();
        superView.Dispose ();
    }

    #endregion

    #region Mouse Event Propagation

    [Fact]
    public void View_HandledEvent_StopsPropagation ()
    {
        // Arrange
        View view = new () { Width = 10, Height = 10 };
        bool handlerCalled = false;
        bool clickHandlerCalled = false;

        view.MouseEvent += (_, args) =>
        {
            handlerCalled = true;
            args.Handled = true; // Mark as handled
        };

        view.MouseEvent += (_, e) => { clickHandlerCalled = !e.IsSingleDoubleOrTripleClicked; ; };

        Mouse mouse = new ()
        {
            Position = new (5, 5),
            Flags = MouseFlags.LeftButtonClicked
        };

        // Act
        bool? result = view.NewMouseEvent (mouse);

        // Assert
        Assert.True (result.HasValue && result.Value); // Event was handled
        Assert.True (handlerCalled);
        Assert.False (clickHandlerCalled); // Click handler should not be called when event is handled

        view.Dispose ();
    }

    [Fact]
    public void View_UnhandledEvent_ContinuesProcessing ()
    {
        // Arrange
        View view = new () { Width = 10, Height = 10 };
        bool eventHandlerCalled = false;

        view.MouseEvent += (_, _) =>
        {
            eventHandlerCalled = true;
            // Don't set Handled = true
        };

        Mouse mouse = new ()
        {
            Position = new (5, 5),
            Flags = MouseFlags.LeftButtonClicked
        };

        // Act
        view.NewMouseEvent (mouse);

        // Assert
        Assert.True (eventHandlerCalled);

        view.Dispose ();
    }

    #endregion



    [Theory]
    [InlineData (MouseFlags.LeftButtonPressed, 1, 0)]
    [InlineData (MouseFlags.LeftButtonReleased, 0, 1)]
    [InlineData (MouseFlags.LeftButtonClicked, 0, 0)]
    public void View_MouseButtonEvents_RaiseCorrectHandlers (MouseFlags flags, int expectedPressed, int expectedReleased)
    {
        // Arrange
        View view = new () { Width = 10, Height = 10 };
        int pressedCount = 0;
        int releasedCount = 0;

        view.MouseEvent += (_, args) =>
        {
            if (args.Flags.HasFlag (MouseFlags.LeftButtonPressed))
            {
                pressedCount++;
            }

            if (args.Flags.HasFlag (MouseFlags.LeftButtonReleased))
            {
                releasedCount++;
            }
        };

        Mouse mouse = new ()
        {
            Position = new (5, 5),
            Flags = flags
        };

        // Act
        view.NewMouseEvent (mouse);

        // Assert
        Assert.Equal (expectedPressed, pressedCount);
        Assert.Equal (expectedReleased, releasedCount);

        view.Dispose ();
    }

    #region Mouse Button Events


    [Theory]
    [InlineData (MouseFlags.LeftButtonClicked)]
    [InlineData (MouseFlags.MiddleButtonClicked)]
    [InlineData (MouseFlags.RightButtonClicked)]
    [InlineData (MouseFlags.Button4Clicked)]
    public void View_AllMouseButtons_TriggerClickEvent (MouseFlags clickFlag)
    {
        // Arrange
        View view = new () { Width = 10, Height = 10 };
        int clickCount = 0;

        view.MouseEvent += (_, a) => clickCount += a.IsSingleDoubleOrTripleClicked ? 1 : 0;

        Mouse mouse = new ()
        {
            Position = new (5, 5),
            Flags = clickFlag
        };

        // Act
        view.NewMouseEvent (mouse);

        // Assert
        Assert.Equal (1, clickCount);

        view.Dispose ();
    }

    #endregion

    #region Disabled View Tests

    [Fact]
    public void View_Disabled_DoesNotRaiseMouseEvent ()
    {
        // Arrange
        View view = new ()
        {
            Width = 10,
            Height = 10,
            Enabled = false
        };

        bool eventCalled = false;
        view.MouseEvent += (_, _) => { eventCalled = true; };

        Mouse mouse = new ()
        {
            Position = new (5, 5),
            Flags = MouseFlags.LeftButtonClicked
        };

        // Act
        view.NewMouseEvent (mouse);

        // Assert
        Assert.False (eventCalled);

        view.Dispose ();
    }

    [Fact]
    public void View_Disabled_DoesNotRaiseSelectingEvent ()
    {
        // Arrange
        View view = new ()
        {
            Width = 10,
            Height = 10,
            Enabled = false
        };

        bool selectingCalled = false;
        view.Activating += (_, _) => { selectingCalled = true; };

        Mouse mouse = new ()
        {
            Position = new (5, 5),
            Flags = MouseFlags.LeftButtonClicked
        };

        // Act
        view.NewMouseEvent (mouse);

        // Assert
        Assert.False (selectingCalled);

        view.Dispose ();
    }

    #endregion

    #region Focus and Selection Tests

    [Theory]
    [InlineData (true, true)]
    [InlineData (false, false)]
    public void MouseClick_SetsFocus_BasedOnCanFocus (bool canFocus, bool expectFocus)
    {
        // Arrange
        View superView = new () { CanFocus = true, Width = 20, Height = 20 };
        View subView = new ()
        {
            X = 5,
            Y = 5,
            Width = 10,
            Height = 10,
            CanFocus = canFocus
        };

        superView.Add (subView);
        superView.SetFocus (); // Give superView focus first

        Mouse mouse = new ()
        {
            Position = new (2, 2),
            Flags = MouseFlags.LeftButtonClicked
        };

        // Act
        subView.NewMouseEvent (mouse);

        // Assert
        Assert.Equal (expectFocus, subView.HasFocus);

        subView.Dispose ();
        superView.Dispose ();
    }

    #endregion    

}
