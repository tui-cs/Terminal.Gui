namespace ApplicationTests.MouseTests;

/// <summary>
///     Parallelizable tests for IMouse interface.
///     Tests the decoupled mouse handling without Application.Init or global state.
/// </summary>
[Trait ("Category", "Input")]
[Collection ("Application Tests")]
public class MouseInterfaceTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    #region IMouse Basic Properties

    [Fact]
    public void Mouse_LastMousePosition_InitiallyNull ()
    {
        // Arrange
        ApplicationMouse mouse = new ();

        // Act & Assert
        Assert.Null (mouse.LastMousePosition);
    }

    [Theory]
    [InlineData (0, 0)]
    [InlineData (10, 20)]
    [InlineData (-5, -10)]
    [InlineData (100, 200)]
    public void Mouse_LastMousePosition_CanBeSetAndRetrieved (int x, int y)
    {
        // Arrange
        ApplicationMouse mouse = new ();
        Point testPosition = new (x, y);

        // Act
        mouse.LastMousePosition = testPosition;

        // Assert
        Assert.Equal (testPosition, mouse.LastMousePosition);
        Assert.Equal (testPosition, mouse.LastMousePosition);
    }

    [Fact]
    public void Mouse_IsMouseDisabled_DefaultsFalse ()
    {
        // Arrange
        ApplicationMouse mouse = new ();

        // Act & Assert
        Assert.False (mouse.IsMouseDisabled);
    }

    [Theory]
    [InlineData (true)]
    [InlineData (false)]
    public void Mouse_IsMouseDisabled_CanBeSetAndRetrieved (bool disabled)
    {
        // Arrange
        ApplicationMouse mouse = new ();

        // Act
        mouse.IsMouseDisabled = disabled;

        // Assert
        Assert.Equal (disabled, mouse.IsMouseDisabled);
    }

    [Fact]
    public void Mouse_CachedViewsUnderMouse_InitiallyEmpty ()
    {
        // Arrange
        ApplicationMouse mouse = new ();

        // Act & Assert
        Assert.NotNull (mouse.CachedViewsUnderMouse);
        Assert.Empty (mouse.CachedViewsUnderMouse);
    }

    #endregion

    #region IMouse Event Handling

    [Fact]
    public void Mouse_MouseEvent_CanSubscribeAndFire ()
    {
        // Arrange
        ApplicationMouse mouse = new ();
        var eventFired = false;
        Mouse? capturedArgs = null;

        mouse.MouseEvent += (sender, args) =>
                            {
                                eventFired = true;
                                capturedArgs = args;
                            };

        Mouse testEvent = new () { ScreenPosition = new Point (5, 10), Flags = MouseFlags.LeftButtonPressed };

        // Act
        mouse.RaiseMouseEvent (testEvent);

        // Assert
        Assert.True (eventFired);
        Assert.NotNull (capturedArgs);
        Assert.Equal (testEvent.ScreenPosition, capturedArgs.ScreenPosition);
        Assert.Equal (testEvent.Flags, capturedArgs.Flags);
    }

    [Fact]
    public void Mouse_MouseEvent_CanUnsubscribe ()
    {
        // Arrange
        ApplicationMouse mouse = new ();
        var eventCount = 0;

        void Handler (object? sender, Mouse args) => eventCount++;

        mouse.MouseEvent += Handler;

        Mouse testEvent = new () { ScreenPosition = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed };

        // Act - Fire once
        mouse.RaiseMouseEvent (testEvent);
        Assert.Equal (1, eventCount);

        // Unsubscribe
        mouse.MouseEvent -= Handler;

        // Fire again
        mouse.RaiseMouseEvent (testEvent);

        // Assert - Count should not increase
        Assert.Equal (1, eventCount);
    }

    [Fact]
    public void Mouse_RaiseMouseEvent_WithDisabledMouse_DoesNotFireEvent ()
    {
        // Arrange
        ApplicationMouse mouse = new ();
        var eventFired = false;

        mouse.MouseEvent += (sender, args) => { eventFired = true; };
        mouse.IsMouseDisabled = true;

        Mouse testEvent = new () { ScreenPosition = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed };

        // Act
        mouse.RaiseMouseEvent (testEvent);

        // Assert
        Assert.False (eventFired);
    }

    [Theory]
    [InlineData (MouseFlags.LeftButtonPressed)]
    [InlineData (MouseFlags.LeftButtonReleased)]
    [InlineData (MouseFlags.LeftButtonClicked)]
    [InlineData (MouseFlags.MiddleButtonPressed)]
    [InlineData (MouseFlags.WheeledUp)]
    [InlineData (MouseFlags.PositionReport)]
    public void Mouse_RaiseMouseEvent_CorrectlyPassesFlags (MouseFlags flags)
    {
        // Arrange
        ApplicationMouse mouse = new ();
        MouseFlags? capturedFlags = null;

        mouse.MouseEvent += (sender, args) => { capturedFlags = args.Flags; };

        Mouse testEvent = new () { ScreenPosition = new Point (5, 5), Flags = flags };

        // Act
        mouse.RaiseMouseEvent (testEvent);

        // Assert
        Assert.NotNull (capturedFlags);
        Assert.Equal (flags, capturedFlags.Value);
    }

    #endregion

    #region IMouse ResetState

    [Fact]
    public void Mouse_ResetState_ClearsCachedViews ()
    {
        // Arrange
        ApplicationMouse mouse = new ();
        View testView = new () { Width = 10, Height = 10 };

        mouse.CachedViewsUnderMouse.Add (testView);
        Assert.Single (mouse.CachedViewsUnderMouse);

        // Act
        mouse.ResetState ();

        // Assert
        Assert.Empty (mouse.CachedViewsUnderMouse);

        testView.Dispose ();
    }

    [Fact]
    public void Mouse_ResetState_ClearsEventHandlers ()
    {
        // Arrange
        ApplicationMouse mouse = new ();
        var eventCount = 0;

        mouse.MouseEvent += (sender, args) => eventCount++;

        Mouse testEvent = new () { ScreenPosition = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed };

        // Verify event fires before reset
        mouse.RaiseMouseEvent (testEvent);
        Assert.Equal (1, eventCount);

        // Act
        mouse.ResetState ();

        // Raise event again
        mouse.RaiseMouseEvent (testEvent);

        // Assert - Event count should not increase after reset
        Assert.Equal (1, eventCount);
    }

    [Fact]
    public void Mouse_ResetState_DoesNotClearLastMousePosition ()
    {
        // Arrange
        ApplicationMouse mouse = new ();
        Point testPosition = new (42, 84);

        mouse.LastMousePosition = testPosition;

        // Act
        mouse.ResetState ();

        // Assert - LastMousePosition should NOT be cleared (per design)
        Assert.Equal (testPosition, mouse.LastMousePosition);
    }

    #endregion

    #region IMouse Isolation

    [Fact]
    public void Mouse_Instances_AreIndependent ()
    {
        // Arrange
        ApplicationMouse mouse1 = new ();
        ApplicationMouse mouse2 = new ();

        // Act
        mouse1.IsMouseDisabled = true;
        mouse1.LastMousePosition = new Point (10, 10);

        // Assert - mouse2 should be unaffected
        Assert.False (mouse2.IsMouseDisabled);
        Assert.Null (mouse2.LastMousePosition);
    }

    [Fact]
    public void Mouse_Events_AreIndependent ()
    {
        // Arrange
        ApplicationMouse mouse1 = new ();
        var mouse1EventCount = 0;

        ApplicationMouse mouse2 = new ();
        var mouse2EventCount = 0;

        mouse1.MouseEvent += (sender, args) => mouse1EventCount++;
        mouse2.MouseEvent += (sender, args) => mouse2EventCount++;

        Mouse testEvent = new () { ScreenPosition = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed };

        // Act
        mouse1.RaiseMouseEvent (testEvent);

        // Assert
        Assert.Equal (1, mouse1EventCount);
        Assert.Equal (0, mouse2EventCount);
    }

    [Fact]
    public void Mouse_CachedViews_AreIndependent ()
    {
        // Arrange
        ApplicationMouse mouse1 = new ();
        ApplicationMouse mouse2 = new ();

        View view1 = new ();
        View view2 = new ();

        // Act
        mouse1.CachedViewsUnderMouse.Add (view1);
        mouse2.CachedViewsUnderMouse.Add (view2);

        // Assert
        Assert.Single (mouse1.CachedViewsUnderMouse);
        Assert.Single (mouse2.CachedViewsUnderMouse);
        Assert.Contains (view1, mouse1.CachedViewsUnderMouse);
        Assert.Contains (view2, mouse2.CachedViewsUnderMouse);
        Assert.DoesNotContain (view2, mouse1.CachedViewsUnderMouse);
        Assert.DoesNotContain (view1, mouse2.CachedViewsUnderMouse);

        view1.Dispose ();
        view2.Dispose ();
    }

    #endregion

    #region Mouse Grab Tests

    [Fact]
    public void Mouse_GrabMouse_IsGrabbed_ReturnsTrue ()
    {
        // Arrange
        ApplicationMouse mouse = new ();
        View testView = new ();

        // Act
        mouse.GrabMouse (testView);

        // Assert
        Assert.True (mouse.IsGrabbed (testView));
    }

    [Fact]
    public void Mouse_UngrabMouse_IsGrabbed_ReturnsFalse ()
    {
        // Arrange
        ApplicationMouse mouse = new ();
        View testView = new ();
        mouse.GrabMouse (testView);

        // Act
        mouse.UngrabMouse ();

        // Assert
        Assert.False (mouse.IsGrabbed (testView));
    }

    [Fact]
    public void Mouse_GrabbingMouse_CanBeCanceled ()
    {
        // Arrange
        ApplicationMouse mouse = new ();
        View testView = new ();
        var eventFired = false;

        mouse.GrabbingMouse += (sender, args) =>
                               {
                                   eventFired = true;
                                   args.Cancel = true;
                               };

        // Act
        mouse.GrabMouse (testView);

        // Assert
        Assert.True (eventFired);
        Assert.False (mouse.IsGrabbed (testView)); // Should not be set because it was cancelled
    }

    [Fact]
    public void Mouse_GrabbedMouse_EventFired ()
    {
        // Arrange
        ApplicationMouse mouse = new ();
        View testView = new ();
        var eventFired = false;
        View? eventView = null;

        mouse.GrabbedMouse += (sender, args) =>
                              {
                                  eventFired = true;
                                  eventView = args.View;
                              };

        // Act
        mouse.GrabMouse (testView);

        // Assert
        Assert.True (eventFired);
        Assert.Equal (testView, eventView);
    }

    [Fact]
    public void Mouse_UnGrabbedMouse_EventFired ()
    {
        // Arrange
        ApplicationMouse mouse = new ();
        View testView = new ();
        mouse.GrabMouse (testView);

        var eventFired = false;
        View? eventView = null;

        mouse.UnGrabbedMouse += (sender, args) =>
                                {
                                    eventFired = true;
                                    eventView = args.View;
                                };

        // Act
        mouse.UngrabMouse ();

        // Assert
        Assert.True (eventFired);
        Assert.Equal (testView, eventView);
    }

    [Fact]
    public void Mouse_IsGrabbed_Parameterless_ReturnsFalseWhenNoViewGrabbed ()
    {
        // Arrange
        ApplicationMouse mouse = new ();

        // Act & Assert
        Assert.False (mouse.IsGrabbed ());
    }

    [Fact]
    public void Mouse_IsGrabbed_Parameterless_ReturnsTrueWhenViewGrabbed ()
    {
        // Arrange
        ApplicationMouse mouse = new ();
        View testView = new ();

        // Act
        mouse.GrabMouse (testView);

        // Assert
        Assert.True (mouse.IsGrabbed ());
        Assert.True (mouse.IsGrabbed (testView));

        testView.Dispose ();
    }

    [Fact]
    public void Mouse_IsGrabbed_Parameterless_ReturnsFalseAfterUngrab ()
    {
        // Arrange
        ApplicationMouse mouse = new ();
        View testView = new ();
        mouse.GrabMouse (testView);
        Assert.True (mouse.IsGrabbed ());

        // Act
        mouse.UngrabMouse ();

        // Assert
        Assert.False (mouse.IsGrabbed ());
        Assert.False (mouse.IsGrabbed (testView));

        testView.Dispose ();
    }

    [Fact]
    public void Mouse_IsGrabbed_Parameterless_WorksWithMultipleGrabCycles ()
    {
        // Arrange
        ApplicationMouse mouse = new ();
        View view1 = new () { Id = "view1" };
        View view2 = new () { Id = "view2" };

        // Initially no grab
        Assert.False (mouse.IsGrabbed ());

        // Grab view1
        mouse.GrabMouse (view1);
        Assert.True (mouse.IsGrabbed ());
        Assert.True (mouse.IsGrabbed (view1));
        Assert.False (mouse.IsGrabbed (view2));

        // Transfer grab to view2 (GrabMouse with new view)
        mouse.GrabMouse (view2);
        Assert.True (mouse.IsGrabbed ());
        Assert.False (mouse.IsGrabbed (view1));
        Assert.True (mouse.IsGrabbed (view2));

        // Ungrab
        mouse.UngrabMouse ();
        Assert.False (mouse.IsGrabbed ());
        Assert.False (mouse.IsGrabbed (view1));
        Assert.False (mouse.IsGrabbed (view2));

        view1.Dispose ();
        view2.Dispose ();
    }

    [Fact]
    public void Mouse_IsGrabbed_Parameterless_ReturnsFalseAfterResetState ()
    {
        // Arrange
        ApplicationMouse mouse = new ();
        View testView = new ();
        mouse.GrabMouse (testView);
        Assert.True (mouse.IsGrabbed ());

        // Act
        mouse.ResetState ();

        // Assert
        Assert.False (mouse.IsGrabbed ());

        testView.Dispose ();
    }

    #endregion
}
