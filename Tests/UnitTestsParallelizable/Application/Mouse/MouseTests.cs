namespace ApplicationTests.MouseTests;

/// <summary>
///     Tests for the <see cref="IMouse"/> interface and <see cref="ApplicationMouse"/> implementation.
///     These tests demonstrate the decoupled mouse handling that enables parallel test execution.
/// </summary>
[Collection("Application Tests")]
public class MouseTests
{
    [Fact]
    public void Mouse_Instance_CreatedSuccessfully ()
    {
        // Arrange & Act
        ApplicationMouse mouse = new ();

        // Assert
        Assert.NotNull (mouse);
        Assert.False (mouse.IsMouseDisabled);
        Assert.Null (mouse.LastMousePosition);
    }

    [Fact]
    public void Mouse_LastMousePosition_CanBeSetAndRetrieved ()
    {
        // Arrange
        ApplicationMouse mouse = new ();
        Point expectedPosition = new (10, 20);

        // Act
        mouse.LastMousePosition = expectedPosition;
        Point? actualPosition = mouse.LastMousePosition;

        // Assert
        Assert.Equal (expectedPosition, actualPosition);
    }

    [Fact]
    public void Mouse_IsMouseDisabled_CanBeSetAndRetrieved ()
    {
        // Arrange
        ApplicationMouse mouse = new ();

        // Act
        mouse.IsMouseDisabled = true;

        // Assert
        Assert.True (mouse.IsMouseDisabled);
    }

    [Fact]
    public void Mouse_CachedViewsUnderMouse_InitializedEmpty ()
    {
        // Arrange
        ApplicationMouse mouse = new ();

        // Assert
        Assert.NotNull (mouse.CachedViewsUnderMouse);
        Assert.Empty (mouse.CachedViewsUnderMouse);
    }

    [Fact]
    public void Mouse_ResetState_ClearsEventAndCachedViews ()
    {
        // Arrange
        ApplicationMouse mouseImpl = new ();
        var eventFired = false;
        mouseImpl.MouseEvent += (sender, args) => eventFired = true;
        mouseImpl.CachedViewsUnderMouse.Add (new View ());

        // Act
        mouseImpl.ResetState ();

        // Assert - CachedViewsUnderMouse should be cleared
        Assert.Empty (mouseImpl.CachedViewsUnderMouse);

        // Event handlers should be cleared
        Mouse mouse = new () { ScreenPosition = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed };
        mouseImpl.RaiseMouseEvent (mouse);
        Assert.False (eventFired, "Event should not fire after ResetState");
    }

    [Fact]
    public void Mouse_RaiseMouseEvent_DoesNotUpdateLastPositionWhenNotInitialized ()
    {
        // Arrange
        ApplicationMouse mouseImpl = new ();
        Mouse mouse = new () { ScreenPosition = new Point (5, 10), Flags = MouseFlags.LeftButtonPressed };

        // Act - Application is not initialized, so LastMousePosition should not be set
        mouseImpl.RaiseMouseEvent (mouse);

        // Assert
        // Since Application.Initialized is false, LastMousePosition should remain null
        // This behavior matches the original implementation
        Assert.Null (mouseImpl.LastMousePosition);
    }

    [Fact]
    public void Mouse_MouseEvent_CanBeSubscribedAndUnsubscribed ()
    {
        // Arrange
        ApplicationMouse mouseImpl = new ();
        var eventCount = 0;
        EventHandler<Mouse> handler = (sender, args) => eventCount++;

        // Act - Subscribe
        mouseImpl.MouseEvent += handler;
        Mouse mouse = new () { ScreenPosition = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed };
        mouseImpl.RaiseMouseEvent (mouse);

        // Assert - Event fired once
        Assert.Equal (1, eventCount);

        // Act - Unsubscribe
        mouseImpl.MouseEvent -= handler;
        mouseImpl.RaiseMouseEvent (mouse);

        // Assert - Event count unchanged
        Assert.Equal (1, eventCount);
    }
    
    /// <summary>
    ///     Tests that the mouse coordinates passed to the focused view are correct when the mouse is clicked. With
    ///     Frames; Frame != Viewport
    /// </summary>
    [Theory]

    // click on border
    [InlineData (0, 0, 0, 0, 0, 0)]
    [InlineData (0, 1, 0, 0, 0, 0)]
    [InlineData (0, 0, 1, 0, 0, 0)]
    [InlineData (0, 9, 0, 0, 0, 0)]
    [InlineData (0, 0, 9, 0, 0, 0)]

    // outside border
    [InlineData (0, 10, 0, 0, 0, 0)]
    [InlineData (0, 0, 10, 0, 0, 0)]

    // view is offset from origin ; click is on border
    [InlineData (1, 1, 1, 0, 0, 0)]
    [InlineData (1, 2, 1, 0, 0, 0)]
    [InlineData (1, 1, 2, 0, 0, 0)]
    [InlineData (1, 10, 1, 0, 0, 0)]
    [InlineData (1, 1, 10, 0, 0, 0)]

    // outside border
    [InlineData (1, -1, 0, 0, 0, 0)]
    [InlineData (1, 0, -1, 0, 0, 0)]
    [InlineData (1, 10, 10, 0, 0, 0)]
    [InlineData (1, 11, 11, 0, 0, 0)]

    // view is at origin, click is inside border
    [InlineData (0, 1, 1, 0, 0, 1)]
    [InlineData (0, 2, 1, 1, 0, 1)]
    [InlineData (0, 1, 2, 0, 1, 1)]
    [InlineData (0, 8, 1, 7, 0, 1)]
    [InlineData (0, 1, 8, 0, 7, 1)]
    [InlineData (0, 8, 8, 7, 7, 1)]

    // view is offset from origin ; click inside border
    // our view is 10x10, but has a border, so it's bounds is 8x8
    [InlineData (1, 2, 2, 0, 0, 1)]
    [InlineData (1, 3, 2, 1, 0, 1)]
    [InlineData (1, 2, 3, 0, 1, 1)]
    [InlineData (1, 9, 2, 7, 0, 1)]
    [InlineData (1, 2, 9, 0, 7, 1)]
    [InlineData (1, 9, 9, 7, 7, 1)]
    [InlineData (1, 10, 10, 7, 7, 0)]

    //01234567890123456789
    // |12345678|
    // |xxxxxxxx
    public void MouseCoordinatesTest_Border (
        int offset,
        int clickX,
        int clickY,
        int expectedX,
        int expectedY,
        int expectedClickedCount
    )
    {
        Size size = new (10, 10);
        Point pos = new (offset, offset);

        int clickedCount = 0;

        using IApplication? application = Application.Create ();

        application.Begin (new Window ()
        {
            Id = "top",
        });
        application.TopRunnableView!.X = 0;
        application.TopRunnableView.Y = 0;
        application.TopRunnableView.Width = size.Width * 2;
        application.TopRunnableView.Height = size.Height * 2;
        application.TopRunnableView.BorderStyle = LineStyle.None;

        var view = new View { Id = "view", X = pos.X, Y = pos.Y, Width = size.Width, Height = size.Height };

        // Give the view a border. With PR #2920, mouse clicks are only passed if they are inside the view's Viewport.
        view.BorderStyle = LineStyle.Single;
        view.CanFocus = true;

        application.TopRunnableView.Add (view);

        var mouse = new Mouse { Position = new (clickX, clickY), ScreenPosition = new (clickX, clickY), Flags = MouseFlags.LeftButtonClicked };

        view.MouseEvent += (_s, e) =>
        {
            Assert.Equal (expectedX, e.Position!.Value.X);
            Assert.Equal (expectedY, e.Position!.Value.Y);
            clickedCount += e.IsSingleDoubleOrTripleClicked ? 1 : 0;
        };

        application.Mouse.RaiseMouseEvent (mouse);
        Assert.Equal (expectedClickedCount, clickedCount);
    }
}
