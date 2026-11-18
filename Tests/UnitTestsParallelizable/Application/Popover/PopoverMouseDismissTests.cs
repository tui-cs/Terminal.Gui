namespace ApplicationTests.Popover;

/// <summary>
///     Tests for the popover dismiss-on-click-outside logic in <see cref="ApplicationMouse.RaiseMouseEvent"/>.
///     <para>
///         The selected code under test:
///         <code>
///             // Dismiss the Popover if the user presses mouse outside of it
///             if (mouseEvent.IsPressed
///                 &amp;&amp; App?.Popovers?.GetActivePopover () is { Visible: true } visiblePopover
///                 &amp;&amp; visiblePopover is View popoverView
///                 &amp;&amp; !View.IsInHierarchy (popoverView, deepestViewUnderMouse, true))
///         </code>
///     </para>
/// </summary>
[Collection ("Application Tests")]
[Trait ("Category", "Mouse")]
public class PopoverMouseDismissTests
{
    /// <summary>
    ///     Clicking outside a visible popover should dismiss it.
    /// </summary>
    [Fact]
    public void MousePress_OutsidePopover_DismissesPopover ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (80, 25);

        Runnable top = new () { App = app };
        SessionToken? token = app.Begin (top);

        // Create a popover and register/show it
        var popover = new ApplicationPopoverTests.PopoverTestClass { App = app };
        app.Popovers!.Register (popover);
        app.Popovers.Show (popover);

        Assert.True (popover.Visible, "Popover should be visible after Show");
        Assert.NotNull (app.Popovers.GetActivePopover ());

        // Act - Send a mouse press outside the popover's SubViews.
        // The popover fills the screen but has no SubViews, so any click goes to the transparent area,
        // which means deepestViewUnderMouse will be the Runnable (not in popover hierarchy).
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (5, 5), Flags = MouseFlags.LeftButtonPressed });

        // Assert
        Assert.False (popover.Visible, "Popover should be dismissed after mouse press outside it");

        app.End (token!);
        top.Dispose ();
    }

    /// <summary>
    ///     Clicking inside a popover's SubView should NOT dismiss it.
    ///     <see cref="View.GetViewsUnderLocation"/> checks the active popover hierarchy first,
    ///     so a non-transparent SubView inside the popover is found in the hierarchy and
    ///     <see cref="View.IsInHierarchy"/> returns true, preventing dismissal.
    /// </summary>
    [Fact]
    public void MousePress_InsidePopoverSubView_DoesNotDismissPopover ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (80, 25);

        Runnable top = new () { App = app };
        SessionToken? token = app.Begin (top);

        // Create a popover that fills the screen (like PopoverImpl does) with a child view
        var popover = new ApplicationPopoverTests.PopoverTestClass { App = app, Width = Dim.Fill (), Height = Dim.Fill () };

        View child = new ()
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10,
            CanFocus = true
        };
        popover.Add (child);

        app.Popovers!.Register (popover);
        app.Popovers.Show (popover);

        // Layout is required so the popover and child have actual screen coordinates
        popover.Layout (new Size (80, 25));

        Assert.True (popover.Visible, "Popover should be visible after Show");

        // Act - Send a mouse press inside the popover's child view
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (1, 1), Flags = MouseFlags.LeftButtonPressed });

        // Assert - popover should remain visible because click was inside its hierarchy
        Assert.True (popover.Visible, "Popover should remain visible when clicking inside its SubView");

        app.End (token!);
        top.Dispose ();
    }

    /// <summary>
    ///     Non-press mouse events (e.g., mouse move/position report) should NOT dismiss the popover.
    /// </summary>
    [Fact]
    public void MouseMove_OutsidePopover_DoesNotDismissPopover ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (80, 25);

        Runnable top = new () { App = app };
        SessionToken? token = app.Begin (top);

        var popover = new ApplicationPopoverTests.PopoverTestClass { App = app };
        app.Popovers!.Register (popover);
        app.Popovers.Show (popover);

        Assert.True (popover.Visible);

        // Act - Send a non-press mouse event (position report) outside the popover
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (5, 5), Flags = MouseFlags.PositionReport });

        // Assert - popover should remain visible because it was not a press event
        Assert.True (popover.Visible, "Popover should remain visible on non-press mouse events");

        app.End (token!);
        top.Dispose ();
    }

    /// <summary>
    ///     When no popover is active, mouse press should not cause any errors.
    /// </summary>
    [Fact]
    public void MousePress_NoActivePopover_DoesNotThrow ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (80, 25);

        Runnable top = new () { App = app };
        SessionToken? token = app.Begin (top);

        Assert.Null (app.Popovers!.GetActivePopover ());

        // Act & Assert - Should not throw
        Exception? ex = Record.Exception (() =>
                                          {
                                              app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (5, 5), Flags = MouseFlags.LeftButtonPressed });
                                          });

        Assert.Null (ex);

        app.End (token!);
        top.Dispose ();
    }

    /// <summary>
    ///     When the popover is not visible, a mouse press should not attempt to dismiss it.
    /// </summary>
    [Fact]
    public void MousePress_PopoverNotVisible_DoesNotAttemptDismiss ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (80, 25);

        Runnable top = new () { App = app };
        SessionToken? token = app.Begin (top);

        var popover = new ApplicationPopoverTests.PopoverTestClass { App = app };
        app.Popovers!.Register (popover);
        app.Popovers.Show (popover);

        // Hide it first
        app.Popovers.Hide (popover);
        Assert.False (popover.Visible);

        // Act - Send mouse press
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (5, 5), Flags = MouseFlags.LeftButtonPressed });

        // Assert - popover should stay hidden (no errors, no state change)
        Assert.False (popover.Visible);

        app.End (token!);
        top.Dispose ();
    }

    /// <summary>
    ///     Right mouse button press outside a visible popover should also dismiss it.
    /// </summary>
    [Fact]
    public void RightMousePress_OutsidePopover_DismissesPopover ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (80, 25);

        Runnable top = new () { App = app };
        SessionToken? token = app.Begin (top);

        var popover = new ApplicationPopoverTests.PopoverTestClass { App = app };
        app.Popovers!.Register (popover);
        app.Popovers.Show (popover);

        Assert.True (popover.Visible);

        // Act - Send a right button press outside the popover
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (5, 5), Flags = MouseFlags.RightButtonPressed });

        // Assert
        Assert.False (popover.Visible, "Popover should be dismissed on right mouse press outside it");

        app.End (token!);
        top.Dispose ();
    }

    /// <summary>
    ///     Middle mouse button press outside a visible popover should also dismiss it.
    /// </summary>
    [Fact]
    public void MiddleMousePress_OutsidePopover_DismissesPopover ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (80, 25);

        Runnable top = new () { App = app };
        SessionToken? token = app.Begin (top);

        var popover = new ApplicationPopoverTests.PopoverTestClass { App = app };
        app.Popovers!.Register (popover);
        app.Popovers.Show (popover);

        Assert.True (popover.Visible);

        // Act
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (5, 5), Flags = MouseFlags.MiddleButtonPressed });

        // Assert
        Assert.False (popover.Visible, "Popover should be dismissed on middle mouse press outside it");

        app.End (token!);
        top.Dispose ();
    }

    /// <summary>
    ///     After dismissing a popover, the mouse event should be re-raised (recursed) so
    ///     views below the popover can handle it. This tests the recursion behavior.
    /// </summary>
    [Fact]
    public void MousePress_OutsidePopover_EventRecursesToViewsBelow ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (80, 25);

        Runnable top = new () { App = app };

        View viewBelowPopover = new ()
        {
            X = 0,
            Y = 0,
            Width = 20,
            Height = 10,
            App = app
        };

        var viewReceivedMouseEvent = false;

        viewBelowPopover.MouseEvent += (_, e) =>
                                       {
                                           viewReceivedMouseEvent = true;
                                           e.Handled = true;
                                       };

        top.Add (viewBelowPopover);
        SessionToken? token = app.Begin (top);

        var popover = new ApplicationPopoverTests.PopoverTestClass { App = app };
        app.Popovers!.Register (popover);
        app.Popovers.Show (popover);

        Assert.True (popover.Visible);

        // Act - Press outside popover (but inside viewBelowPopover)
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (5, 5), Flags = MouseFlags.LeftButtonPressed });

        // Assert - Popover dismissed AND the view below got the event via recursion
        Assert.False (popover.Visible, "Popover should be dismissed");
        Assert.True (viewReceivedMouseEvent, "View below popover should receive the mouse event via recursion");

        app.End (token!);
        top.Dispose ();
    }

    /// <summary>
    ///     Wheel events should NOT dismiss the popover (they are not IsPressed).
    /// </summary>
    [Fact]
    public void MouseWheel_OutsidePopover_DoesNotDismissPopover ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (80, 25);

        Runnable top = new () { App = app };
        SessionToken? token = app.Begin (top);

        var popover = new ApplicationPopoverTests.PopoverTestClass { App = app };
        app.Popovers!.Register (popover);
        app.Popovers.Show (popover);

        Assert.True (popover.Visible);

        // Act - Send wheel event outside the popover
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (5, 5), Flags = MouseFlags.WheeledDown });

        // Assert - popover should remain visible because wheel is not a press
        Assert.True (popover.Visible, "Popover should remain visible on wheel events");

        app.End (token!);
        top.Dispose ();
    }

    /// <summary>
    ///     Mouse release outside a popover should NOT dismiss it (only press events dismiss).
    /// </summary>
    [Fact]
    public void MouseRelease_OutsidePopover_DoesNotDismissPopover ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (80, 25);

        Runnable top = new () { App = app };
        SessionToken? token = app.Begin (top);

        var popover = new ApplicationPopoverTests.PopoverTestClass { App = app };
        app.Popovers!.Register (popover);
        app.Popovers.Show (popover);

        Assert.True (popover.Visible);

        // Act - Send a release event (not a press)
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (5, 5), Flags = MouseFlags.LeftButtonReleased });

        // Assert
        Assert.True (popover.Visible, "Popover should remain visible on mouse release events");

        app.End (token!);
        top.Dispose ();
    }

    /// <summary>
    ///     When MouseEvent handler marks the event as Handled before the popover dismiss logic,
    ///     the popover should NOT be dismissed.
    /// </summary>
    [Fact]
    public void MousePress_HandledAtApplicationLevel_DoesNotDismissPopover ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (80, 25);

        Runnable top = new () { App = app };
        SessionToken? token = app.Begin (top);

        var popover = new ApplicationPopoverTests.PopoverTestClass { App = app };
        app.Popovers!.Register (popover);
        app.Popovers.Show (popover);

        Assert.True (popover.Visible);

        // Handle the event at the application level before popover dismiss logic runs
        app.Mouse.MouseEvent += (_, e) => e.Handled = true;

        // Act
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (5, 5), Flags = MouseFlags.LeftButtonPressed });

        // Assert - popover should remain visible because the event was handled before dismiss logic
        Assert.True (popover.Visible, "Popover should remain visible when event is handled at application level");

        app.End (token!);
        top.Dispose ();
    }

    /// <summary>
    ///     A view that shows its associated popover when activated.
    ///     Uses <see cref="MouseFlags.LeftButtonPressed"/> → <see cref="Command.Activate"/> so the same
    ///     press event that triggers the popover dismiss logic also activates this view during recursion.
    /// </summary>
    private class ActivatorView : View
    {
        public int ActivateCount { get; private set; }
        public ApplicationPopoverTests.PopoverTestClass? Popover { get; set; }

        public ActivatorView ()
        {
            CanFocus = true;

            // Map left button press to Activate so the same press that dismisses the popover also activates this view.
            // The default binding is LeftButtonReleased, which would not fire for a LeftButtonPressed event.
            MouseBindings.Add (MouseFlags.LeftButtonPressed, Command.Activate);
        }

        protected override void OnActivated (ICommandContext? ctx)
        {
            ActivateCount++;

            if (Popover is { } p && App?.Popovers is { } popovers)
            {
                if (!popovers.IsRegistered (p))
                {
                    popovers.Register (p);
                }

                popovers.Show (p);
                p.Layout (new Size (80, 25));
            }

            base.OnActivated (ctx);
        }
    }

    /// <summary>
    ///     After the fix, clicking on the activator while the popover is visible dismisses the popover
    ///     and the <see cref="ApplicationPopover.Show"/> guard prevents re-show during the same click cycle.
    ///     <list type="number">
    ///         <item>An <see cref="ActivatorView"/> on the Runnable shows a popover on Activate.</item>
    ///         <item>The popover content is at X=20, so it does not cover the activator at X=0.</item>
    ///         <item>First press on the activator shows the popover.</item>
    ///         <item>
    ///             Second press on the activator (while popover is visible):
    ///             <list type="bullet">
    ///                 <item>Dismiss logic fires — popover is hidden via <see cref="ApplicationPopover.HideWithQuitCommand"/>.</item>
    ///                 <item><see cref="ApplicationMouse.RaiseMouseEvent"/> recurses.</item>
    ///                 <item>The recursed event reaches the activator, invoking <see cref="Command.Activate"/>.</item>
    ///                 <item>
    ///                     <see cref="ActivatorView.OnActivated"/> calls <see cref="ApplicationPopover.Show"/> but the
    ///                     <see cref="ApplicationMouse.DismissedByMousePress"/> guard suppresses the re-show.
    ///                 </item>
    ///             </list>
    ///         </item>
    ///     </list>
    /// </summary>
    [Fact]
    public void MousePress_OnActivatorSubview_DismissesPopover_DoesNotReshow ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (80, 25);

        Runnable top = new () { App = app };

        // Activator view at top-left corner
        var activator = new ActivatorView { X = 0, Y = 0, Width = 10, Height = 3 };

        // Popover fills the screen (transparent overlay), but its only non-transparent
        // child is at X=20 — well away from the activator at X=0.
        var popover = new ApplicationPopoverTests.PopoverTestClass { App = app, Width = Dim.Fill (), Height = Dim.Fill () };

        View popoverContent = new ()
        {
            X = 20,
            Y = 0,
            Width = 15,
            Height = 5,
            CanFocus = true
        };
        popover.Add (popoverContent);

        activator.Popover = popover;
        top.Add (activator);
        SessionToken? token = app.Begin (top);

        // --- First press: activator shows the popover ---
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (1, 1), Flags = MouseFlags.LeftButtonPressed });

        Assert.True (popover.Visible, "Popover should be visible after first activation");
        Assert.Equal (1, activator.ActivateCount);

        // --- Second press on the activator while popover is visible ---
        // Because the activator is NOT in the popover hierarchy, the dismiss logic fires.
        // Then RaiseMouseEvent recurses, the event reaches the activator, and Activate tries to re-show.
        // The DismissedByMousePress guard in ApplicationPopover.Show suppresses the re-show.
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (1, 1), Flags = MouseFlags.LeftButtonPressed });

        // Assert — the popover was dismissed and the guard prevented re-show
        Assert.Equal (2, activator.ActivateCount);
        Assert.False (popover.Visible, "Popover should stay hidden — DismissedByMousePress guard prevents re-show");

        app.End (token!);
        top.Dispose ();
    }

    /// <summary>
    ///     After dismiss-by-click, a subsequent fresh mouse press should allow the popover to be shown again.
    ///     The <see cref="ApplicationMouse.DismissedByMousePress"/> guard is cleared on the next new press event.
    /// </summary>
    [Fact]
    public void MousePress_AfterDismissCycle_AllowsReshowOnNextPress ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (80, 25);

        Runnable top = new () { App = app };

        var activator = new ActivatorView { X = 0, Y = 0, Width = 10, Height = 3 };

        var popover = new ApplicationPopoverTests.PopoverTestClass { App = app, Width = Dim.Fill (), Height = Dim.Fill () };

        View popoverContent = new ()
        {
            X = 20,
            Y = 0,
            Width = 15,
            Height = 5,
            CanFocus = true
        };
        popover.Add (popoverContent);

        activator.Popover = popover;
        top.Add (activator);
        SessionToken? token = app.Begin (top);

        // First press: show popover
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (1, 1), Flags = MouseFlags.LeftButtonPressed });
        Assert.True (popover.Visible);

        // Second press: dismiss + suppressed re-show
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (1, 1), Flags = MouseFlags.LeftButtonPressed });
        Assert.False (popover.Visible, "Popover should be dismissed");

        // Third press: guard was cleared by the new press → popover can be shown again
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (1, 1), Flags = MouseFlags.LeftButtonPressed });
        Assert.True (popover.Visible, "Popover should be re-shown on a fresh press after the dismiss cycle");
        Assert.Equal (3, activator.ActivateCount);

        app.End (token!);
        top.Dispose ();
    }

    /// <summary>
    ///     When a SubView inside the popover grabs the mouse on press, dragging outside
    ///     the popover (while the button is still held) should NOT dismiss the popover.
    ///     This simulates a press-and-hold drag scenario (e.g., dragging a scrollbar
    ///     slider or selecting text) where the mouse leaves the popover bounds.
    /// </summary>

    // Claude - Opus 4.6
    [Fact]
    public void MousePressInsidePopover_ThenDragOutside_DoesNotDismissPopover ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (80, 25);

        Runnable top = new () { App = app };
        SessionToken? token = app.Begin (top);

        // Create a popover with a mouse-grabbing SubView
        var popover = new ApplicationPopoverTests.PopoverTestClass { App = app, Width = Dim.Fill (), Height = Dim.Fill (), GrabsMouseOnPress = true };

        app.Popovers!.Register (popover);
        app.Popovers.Show (popover);
        popover.Layout (new Size (80, 25));

        Assert.True (popover.Visible, "Popover should be visible after Show");
        Assert.NotNull (popover.MouseGrabbingSubView);

        // Act Step 1 - Press inside the mouse-grabbing SubView (which is at 0,0 size 10x10)
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (5, 5), Flags = MouseFlags.LeftButtonPressed });

        // The SubView should have grabbed the mouse
        Assert.True (app.Mouse.IsGrabbed (popover.MouseGrabbingSubView!), "Mouse should be grabbed by the SubView after press");
        Assert.True (popover.Visible, "Popover should still be visible after press inside SubView");

        // Act Step 2 - Drag outside the popover's SubView while button is still held
        // Point (50, 20) is outside the mouse-grabbing SubView (10x10) but the mouse is grabbed
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (50, 20), Flags = MouseFlags.LeftButtonPressed });

        // Assert - Popover should NOT be dismissed because mouse was grabbed by a view in the popover hierarchy
        Assert.True (popover.Visible, "Popover should remain visible when mouse is grabbed and dragged outside");

        // Act Step 3 - Release the mouse outside
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (50, 20), Flags = MouseFlags.LeftButtonReleased });

        // Popover should still be visible after release (release doesn't dismiss)
        Assert.True (popover.Visible, "Popover should remain visible after mouse release");

        app.End (token!);
        top.Dispose ();
    }

    /// <summary>
    ///     The dismiss guard only suppresses the SAME popover. Clicking outside one popover
    ///     and having the view beneath open a DIFFERENT popover should work.
    /// </summary>
    [Fact]
    public void MousePress_OnActivator_ShowsDifferentPopover_AfterDismiss ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (80, 25);

        Runnable top = new () { App = app };

        // First popover (will be dismissed)
        var popover1 = new ApplicationPopoverTests.PopoverTestClass { App = app, Width = Dim.Fill (), Height = Dim.Fill () };

        View content1 = new ()
        {
            X = 30,
            Y = 0,
            Width = 10,
            Height = 5,
            CanFocus = true
        };
        popover1.Add (content1);

        // Second popover (will be opened by activator)
        var popover2 = new ApplicationPopoverTests.PopoverTestClass { App = app, Width = Dim.Fill (), Height = Dim.Fill () };

        View content2 = new ()
        {
            X = 30,
            Y = 0,
            Width = 10,
            Height = 5,
            CanFocus = true
        };
        popover2.Add (content2);

        // Activator always opens popover2
        var activator = new ActivatorView
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 3,
            Popover = popover2
        };

        top.Add (activator);
        SessionToken? token = app.Begin (top);

        // Show popover1 first
        app.Popovers!.Register (popover1);
        app.Popovers.Show (popover1);
        popover1.Layout (new Size (80, 25));

        Assert.True (popover1.Visible);

        // Act - Click on the activator, which is outside popover1. This should:
        // 1. Dismiss popover1
        // 2. Allow popover2 to be shown (different popover, not blocked by guard)
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (1, 1), Flags = MouseFlags.LeftButtonPressed });

        // Assert
        Assert.False (popover1.Visible, "popover1 should be dismissed");
        Assert.True (popover2.Visible, "popover2 should be shown — guard only blocks the dismissed popover");

        app.End (token!);
        top.Dispose ();
    }
}
