using System.ComponentModel;
using System.Diagnostics;
using Trace = Terminal.Gui.Tracing.Trace;

namespace Terminal.Gui.App;

/// <summary>
///     INTERNAL: Implements <see cref="IMouse"/> to manage mouse event handling and state.
///     <para>
///         This class holds all mouse-related state that was previously in the static <see cref="App"/> class,
///         enabling better testability and parallel test execution.
///     </para>
/// </summary>
internal class ApplicationMouse : IMouse, IDisposable
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ApplicationMouse"/> class and subscribes to Application configuration
    ///     property events.
    /// </summary>
    public ApplicationMouse ()
    {
        // Seed from configuration before App is set so this has no terminal side effects.
        _isMouseDisabled = ApplicationSettings.Defaults.IsMouseDisabled;

        // Subscribe to Application static property change events
        Application.IsMouseDisabledChanged += OnIsMouseDisabledChanged;
    }

    /// <summary>
    ///     The popover that was just dismissed by a mouse-press-outside event.
    ///     Used to prevent re-entrant show of the same popover during the
    ///     press → release → click cycle that triggered the dismiss.
    /// </summary>
    private IPopoverView? _dismissedByMousePress;

    /// <summary>
    ///     The active popover at the time of the most recent mouse press. Used to detect when a popover
    ///     closes programmatically during the press → release → click cycle (e.g., when a ListView item
    ///     selection hides the popover on Released). If this was set during Pressed but the popover is no
    ///     longer active at Clicked time, the Clicked event is suppressed to prevent it from leaking to
    ///     views below the now-hidden popover.
    /// </summary>
    private IPopoverView? _activePopoverAtPress;

    /// <summary>
    ///     Tracks whether <see cref="RaiseMouseEvent"/> is currently executing within the
    ///     dismiss recursion, to avoid clearing <see cref="_dismissedByMousePress"/> prematurely.
    /// </summary>
    private bool _isDismissRecursing;

    /// <inheritdoc/>
    public void Dispose ()
    {
        ResetState ();

        // Unsubscribe from Application static property change events
        Application.IsMouseDisabledChanged -= OnIsMouseDisabledChanged;
    }

    /// <inheritdoc/>
    public IApplication? App { get; set; }

    /// <inheritdoc/>
    public Point? LastMousePosition { get; set; }

    private long _mouseDisableGeneration;

    /// <summary>
    ///     Identifies the current mouse-disable cycle so holds cannot resume after reporting is re-enabled.
    /// </summary>
    internal long MouseDisableGeneration => Interlocked.Read (ref _mouseDisableGeneration);

    private volatile bool _isMouseDisabled;

    /// <inheritdoc/>
    public bool IsMouseDisabled
    {
        get => _isMouseDisabled;
        set
        {
            // Use the render gate for the entire transition, not just the physical mode write. A separate mouse
            // lock would invert the lock order when rendering invokes a mouse callback that changes this property.
            if (App is ApplicationImpl { Initialized: true, Coordinator: IInlineRenderCoordinator coordinator })
            {
                coordinator.WithRenderGate (() => SetMouseDisabled (value));
                return;
            }

            SetMouseDisabled (value);
        }
    }

    private void SetMouseDisabled (bool value)
    {
        if (_isMouseDisabled == value)
        {
            return;
        }

        if (App is ApplicationImpl { Initialized: true } app)
        {
            (app.Coordinator as IMouseReportingCoordinator)?.SetMouseDisabled (value);
        }

        _isMouseDisabled = value;

        if (!value)
        {
            return;
        }

        // Invalidate every existing hold, including those no longer grabbed or in the hover cache.
        Interlocked.Increment (ref _mouseDisableGeneration);

        // View state belongs to the UI thread; queue the release when called from another thread.
        if (App is ApplicationImpl { Initialized: true, MainThreadId: { } mainThreadId } uiApp
            && mainThreadId != Environment.CurrentManagedThreadId)
        {
            uiApp.Invoke (ReleaseMouseState);
            return;
        }

        ReleaseMouseState ();
    }

    /// <inheritdoc/>
    public List<View?> CachedViewsUnderMouse { get; } = [];

    /// <inheritdoc/>
    public event EventHandler<Mouse>? MouseEvent;

    /// <inheritdoc/>
    public void RaiseMouseEvent (Mouse mouseEvent)
    {
        // In inline mode, the driver reports terminal-absolute coordinates but views
        // expect coordinates relative to App.Screen origin. Subtract the inline offset.
        // IMPORTANT: Save and restore the original ScreenPosition because the MouseInterpreter
        // generator yields the same Mouse object for original and synthesized events. If we
        // mutate ScreenPosition permanently, synthesized events (click, double-click) inherit
        // the already-adjusted position and get adjusted again — a double-subtraction bug.
        Point originalScreenPosition = mouseEvent.ScreenPosition;

        try
        {
            if (App?.AppModel == AppModel.Inline)
            {
                Rectangle appScreen = App.Screen;

                mouseEvent.ScreenPosition = new Point (mouseEvent.ScreenPosition.X - appScreen.X, mouseEvent.ScreenPosition.Y - appScreen.Y);
            }

            if (App?.Initialized is true)
            {
                // LastMousePosition is only set if the application is initialized.
                LastMousePosition = mouseEvent.ScreenPosition;
            }

            if (IsMouseDisabled)
            {
                return;
            }

            // The position of the mouse is the same as the screen position at the application level.
            mouseEvent.Position = mouseEvent.ScreenPosition;

            List<View?>? currentViewsUnderMouse =
                App?.TopRunnableView?.GetViewsUnderLocation (mouseEvent.ScreenPosition, ViewportSettingsFlags.TransparentMouse);

            View? deepestViewUnderMouse = currentViewsUnderMouse?.LastOrDefault ();

            if (deepestViewUnderMouse is { })
            {
#if DEBUG_IDISPOSABLE
                if (View.EnableDebugIDisposableAsserts && deepestViewUnderMouse.WasDisposed)
                {
                    throw new ObjectDisposedException (deepestViewUnderMouse.ToDebugString ());
                }
#endif
                mouseEvent.View = deepestViewUnderMouse;
            }

            Trace.Mouse ("app", mouseEvent.Flags, mouseEvent.ScreenPosition, "Entry", "Invoking MouseEvent");
            MouseEvent?.Invoke (this, mouseEvent);

            if (mouseEvent.Handled || IsMouseDisabled)
            {
                return;
            }

            // Record the active popover when a press starts so we can detect if it closes mid-cycle.
            if (mouseEvent.IsPressed)
            {
                _activePopoverAtPress = App?.Popovers?.GetActivePopover ();
            }

            // Suppress Clicked events that were synthesized after a popover closed during the same
            // press → release → click cycle. Without this, selecting an item in a popover (which
            // hides the popover on Released) causes the Clicked event to leak to views below.
            if (mouseEvent.IsSingleDoubleOrTripleClicked && _activePopoverAtPress is { } && App?.Popovers?.GetActivePopover () != _activePopoverAtPress)
            {
                Trace.Mouse ("app", mouseEvent.Flags, mouseEvent.ScreenPosition, "Popovers", "Suppressing Clicked - popover closed mid-cycle");
                _activePopoverAtPress = null;

                return;
            }

            // Clear the dismissed-popover guard on a genuinely new press that isn't part of dismiss recursion.
            if (mouseEvent.IsPressed && !_isDismissRecursing && _dismissedByMousePress is { })
            {
                _dismissedByMousePress = null;
            }

            // Dismiss the Popover if the user presses mouse outside of it
            if (mouseEvent.IsPressed
                && App?.Popovers?.GetActivePopover () is { Visible: true } visiblePopover and View popoverView
                && !View.IsInHierarchy (popoverView, deepestViewUnderMouse, true)
                && !IsGrabbedByViewInHierarchy (popoverView))
            {
                Trace.Mouse ("app", mouseEvent.Flags, mouseEvent.ScreenPosition, "Popovers", "Hide Visible Popover");

                ApplicationPopover.HideWithQuitCommand (visiblePopover);

                // Record the dismissed popover so ApplicationPopover.Show can suppress re-show
                // during the remainder of this press → release → click cycle.
                _dismissedByMousePress = visiblePopover;
                _isDismissRecursing = true;

                // Restore original terminal-absolute coordinates before recursing so the
                // recursive call performs its own inline offset subtraction correctly.
                mouseEvent.ScreenPosition = originalScreenPosition;

                // Recurse once so the event can be handled below the popover
                RaiseMouseEvent (mouseEvent);

                _isDismissRecursing = false;

                return;
            }

            if (HandleMouseGrab (deepestViewUnderMouse, mouseEvent))
            {
                return;
            }

            // May be null before the prior condition or the condition may set it as null.
            // So, the checking must be outside the prior condition.
            if (deepestViewUnderMouse is null)
            {
                return;
            }

            // if the mouse is outside the Application.TopRunnable or Popover hierarchy, we don't want to
            // send the mouse event to the deepest view under the mouse.
            if (!View.IsInHierarchy (App?.TopRunnableView, deepestViewUnderMouse, true)
                && !View.IsInHierarchy (App?.Popovers?.GetActivePopover () as View, deepestViewUnderMouse, true))
            {
                return;
            }

            // Create a view-relative mouse event to send to the view that is under the mouse.
            Mouse viewMouseEvent;

            if (deepestViewUnderMouse is AdornmentView adornment)
            {
                Point frameLoc = adornment.ScreenToFrame (mouseEvent.ScreenPosition);

                viewMouseEvent = new Mouse
                {
                    Timestamp = mouseEvent.Timestamp,
                    Position = frameLoc,
                    Flags = mouseEvent.Flags,
                    ScreenPosition = mouseEvent.ScreenPosition,
                    View = deepestViewUnderMouse
                };
            }
            else if (deepestViewUnderMouse.ViewportToScreen (Rectangle.Empty with { Size = deepestViewUnderMouse.Viewport.Size })
                                          .Contains (mouseEvent.ScreenPosition))
            {
                Point viewportLocation = deepestViewUnderMouse.ScreenToViewport (mouseEvent.ScreenPosition);

                viewMouseEvent = new Mouse
                {
                    Timestamp = mouseEvent.Timestamp,
                    Position = viewportLocation,
                    Flags = mouseEvent.Flags,
                    ScreenPosition = mouseEvent.ScreenPosition,
                    View = deepestViewUnderMouse
                };
            }
            else
            {
                // The mouse was outside any View's Viewport.
                // Debug.Fail ("This should never happen. If it does please file an Issue!!");

                return;
            }

            if (currentViewsUnderMouse is { })
            {
                RaiseMouseEnterLeaveEvents (viewMouseEvent.ScreenPosition, currentViewsUnderMouse);
            }

            // A handler may disable the mouse mid-dispatch; stop so no view grabs or highlights afterward.
            if (IsMouseDisabled)
            {
                return;
            }

            Trace.Mouse ("app", viewMouseEvent.Flags, viewMouseEvent.ScreenPosition, "Dispatch");

            while (deepestViewUnderMouse.NewMouseEvent (viewMouseEvent) is not true && _mouseGrabViewRef is null && !IsMouseDisabled)
            {
                if (deepestViewUnderMouse is AdornmentView adornmentView)
                {
                    deepestViewUnderMouse = adornmentView.Adornment?.Parent?.SuperView;
                }
                else
                {
                    deepestViewUnderMouse = deepestViewUnderMouse.SuperView;
                }

                if (deepestViewUnderMouse is null)
                {
                    break;
                }

                Point boundsPoint = deepestViewUnderMouse.ScreenToViewport (mouseEvent.ScreenPosition);

                viewMouseEvent = new Mouse
                {
                    Timestamp = mouseEvent.Timestamp,
                    Position = boundsPoint,
                    Flags = mouseEvent.Flags,
                    ScreenPosition = mouseEvent.ScreenPosition,
                    View = deepestViewUnderMouse
                };

                Trace.Mouse ("app", viewMouseEvent.Flags, viewMouseEvent.ScreenPosition, "Dispatch");
            }

            Trace.Mouse ("app", mouseEvent.Flags, mouseEvent.ScreenPosition, "Exit");

            // Clear the dismissed-popover guard after the click cycle completes.
            // The Click event is the last event in a press → release → click cycle.
            if (_dismissedByMousePress is { } && mouseEvent.IsSingleDoubleOrTripleClicked && !_isDismissRecursing)
            {
                _dismissedByMousePress = null;
            }
        }
        finally
        {
            // Restore original terminal-absolute position so the MouseInterpreter generator
            // creates synthesized events with the correct (unadjusted) coordinates.
            mouseEvent.ScreenPosition = originalScreenPosition;
        }
    }

    /// <inheritdoc/>
    public void RaiseMouseEnterLeaveEvents (Point screenPosition, List<View?> currentViewsUnderMouse)
    {
        Trace.Mouse ("app", MouseFlags.None, screenPosition, "EnterLeave");

        // Tell any views that are no longer under the mouse that the mouse has left
        List<View?> viewsToLeave = CachedViewsUnderMouse.Where (v => v is { } && !currentViewsUnderMouse.Contains (v)).ToList ();

        foreach (View? view in viewsToLeave)
        {
            // Remove before callbacks so reentrant disable cleanup cannot notify the same departure twice.
            if (view is null || !CachedViewsUnderMouse.Remove (view))
            {
                continue;
            }

            view.NewMouseLeaveEvent ();
            if (!IsMouseDisabled)
            {
                continue;
            }
            view.MouseHoldRepeater?.Stop ();
            view.MouseState = MouseState.None;
        }

        // Tell any views that are now under the mouse that the mouse has entered and add them to the list
        foreach (View? view in currentViewsUnderMouse)
        {
            // An enter handler may disable the mouse, which clears the hover cache; don't repopulate it.
            if (IsMouseDisabled)
            {
                break;
            }

            if (view is null)
            {
                continue;
            }

            if (CachedViewsUnderMouse.Contains (view))
            {
                continue;
            }

            // If the mouse is grabbed by another view, don't send MouseEnter events to other views.
            // This prevents views from highlighting when the user drags the mouse over them while holding
            // a button down on a different view.
            if (_mouseGrabViewRef is { } && !IsGrabbed (view))
            {
                continue;
            }

            CachedViewsUnderMouse.Add (view);
            bool raise;

            if (view is AdornmentView { Adornment.Parent: { } } adornmentView)
            {
                Point superViewLoc = adornmentView.Adornment.Parent.SuperView?.ScreenToViewport (screenPosition) ?? screenPosition;
                raise = adornmentView.Contains (superViewLoc);
            }
            else
            {
                Point superViewLoc = view.SuperView?.ScreenToViewport (screenPosition) ?? screenPosition;
                raise = view.Contains (superViewLoc);
            }

            if (!raise)
            {
                continue;
            }

            var eventArgs = new CancelEventArgs ();
            bool? cancelled = view.NewMouseEnterEvent (eventArgs);

            if (IsMouseDisabled)
            {
                // Disable already cleared the cache and raised MouseLeave, but the enter callback resumed afterward.
                view.MouseState = MouseState.None;
                break;
            }

            if (cancelled is true || eventArgs.Cancel)
            {
                break;
            }
        }
    }

    /// <inheritdoc/>
    public void ResetState ()
    {
        // Do not clear LastMousePosition; Popovers require it to stay set with last mouse pos.
        CachedViewsUnderMouse.Clear ();
        MouseEvent = null;
        _mouseGrabViewRef = null;
        _dismissedByMousePress = null;
        _isDismissRecursing = false;
    }

    /// <summary>
    ///     Gets the popover that was just dismissed by a mouse-press-outside event, if any.
    ///     Checked by <see cref="ApplicationPopover.Show"/> to suppress re-show during the
    ///     same press → release → click cycle.
    /// </summary>
    internal IPopoverView? DismissedByMousePress => _dismissedByMousePress;

    /// <summary>
    ///     Returns <see langword="true"/> when the mouse is currently grabbed by a view
    ///     that belongs to <paramref name="hierarchyRoot"/>'s view hierarchy.
    /// </summary>
    private bool IsGrabbedByViewInHierarchy (View hierarchyRoot) =>
        _mouseGrabViewRef?.TryGetTarget (out View? grabbed) is true && View.IsInHierarchy (hierarchyRoot, grabbed, true);

    /// <summary>
    ///     Force-releases the mouse grab, stops hold repetition, and clears hover and popover press state so no view is
    ///     left waiting for a mouse event that will not arrive while the mouse is disabled.
    /// </summary>
    private void ReleaseMouseState ()
    {
        // The mouse may have been re-enabled before a queued release ran.
        if (!_isMouseDisabled)
        {
            return;
        }

        View? grabbed = null;
        _mouseGrabViewRef?.TryGetTarget (out grabbed);
        _mouseGrabViewRef = null;
        grabbed?.MouseHoldRepeater?.Stop ();

        if (grabbed is { })
        {
            grabbed.MouseState = MouseState.None;
            RaiseUnGrabbedMouseEvent (grabbed);
        }

        View? [] hovered = [.. CachedViewsUnderMouse];
        CachedViewsUnderMouse.Clear ();
        _activePopoverAtPress = null;
        _dismissedByMousePress = null;

        foreach (View? view in hovered)
        {
            if (view is null)
            {
                continue;
            }

            // A canceled grab can leave an active repeater on a hovered view without a grabbed view.
            view.MouseHoldRepeater?.Stop ();
            view.MouseState = MouseState.None;
            view.NewMouseLeaveEvent ();
        }
    }

    // Event handler for Application static property changes
    private void OnIsMouseDisabledChanged (object? sender, ValueChangedEventArgs<bool> e)
    {
        // The legacy static setting fans out to every live instance; one failing terminal must not block the rest.
        try
        {
            IsMouseDisabled = e.NewValue;
        }
        catch (Exception error)
        {
            Logging.Error ($"Failed to apply Application.IsMouseDisabled = {e.NewValue}: {error}");
        }
    }

    #region IMouseGrabHandler Implementation

    private WeakReference<View>? _mouseGrabViewRef;

    // A delayed End must release only the grab that existed when it ended, including when the same view re-grabs.
    internal Action CaptureUngrabMouse ()
    {
        WeakReference<View>? grab = _mouseGrabViewRef;

        return () =>
               {
                   if (!ReferenceEquals (_mouseGrabViewRef, grab))
                   {
                       return;
                   }

                   UngrabMouse ();
               };
    }

    /// <inheritdoc/>
    public bool IsGrabbed (View? view)
    {
        if (view is null || _mouseGrabViewRef is null)
        {
            return false;
        }

        return _mouseGrabViewRef.TryGetTarget (out View? grabbed) && ReferenceEquals (grabbed, view);
    }

    /// <inheritdoc/>
    public bool IsGrabbed () => _mouseGrabViewRef is { } && _mouseGrabViewRef.TryGetTarget (out _);

    /// <inheritdoc/>
    public event EventHandler<GrabMouseEventArgs>? GrabbingMouse;

    /// <inheritdoc/>
    public event EventHandler<GrabMouseEventArgs>? UnGrabbingMouse;

    /// <inheritdoc/>
    public event EventHandler<ViewEventArgs>? GrabbedMouse;

    /// <inheritdoc/>
    public event EventHandler<ViewEventArgs>? UnGrabbedMouse;

    /// <inheritdoc/>
    public void GrabMouse (View? view)
    {
        Trace.Mouse ("app", MouseFlags.None, LastMousePosition ?? Point.Empty, "Grab");

        // No release event can arrive while the mouse is disabled, so a new grab would never end.
        if (view is { } && IsMouseDisabled)
        {
            return;
        }

        if (RaiseGrabbingMouseEvent (view))
        {
            return;
        }

        if (view is null)
        {
            UngrabMouse ();

            return;
        }

        if (IsMouseDisabled)
        {
            return;
        }

        RaiseGrabbedMouseEvent (view);

        if (IsMouseDisabled)
        {
            return;
        }

        // _mouseGrabViewRef is only set if the application is initialized.
        _mouseGrabViewRef = new WeakReference<View> (view);
    }

    /// <inheritdoc/>
    public void UngrabMouse ()
    {
        WeakReference<View>? grab = _mouseGrabViewRef;
        if (grab is null || !grab.TryGetTarget (out View? grabbedView))
        {
            return;
        }

        Trace.Mouse ("app", MouseFlags.None, LastMousePosition ?? Point.Empty, "Grab");

        if (RaiseUnGrabbingMouseEvent (grabbedView))
        {
            return;
        }

        // A callback can release this grab or replace it, even with a new grab on the same view.
        if (!ReferenceEquals (_mouseGrabViewRef, grab))
        {
            return;
        }

        _mouseGrabViewRef = null;
        RaiseUnGrabbedMouseEvent (grabbedView);

        // After ungrabbing, immediately update enter/leave state for views under the current mouse position
        // This ensures that if the mouse was released over a different view, that view receives MouseEnter
        if (App?.Initialized is not true || LastMousePosition is not { } position)
        {
            return;
        }
        List<View?>? currentViewsUnderMouse = App.TopRunnableView?.GetViewsUnderLocation (position, ViewportSettingsFlags.TransparentMouse);

        if (currentViewsUnderMouse is { })
        {
            RaiseMouseEnterLeaveEvents (position, currentViewsUnderMouse);
        }
    }

    /// <exception cref="Exception">A delegate callback throws an exception.</exception>
    private bool RaiseGrabbingMouseEvent (View? view)
    {
        if (view is null)
        {
            return false;
        }

        GrabMouseEventArgs evArgs = new (view);
        GrabbingMouse?.Invoke (view, evArgs);

        return evArgs.Cancel;
    }

    /// <exception cref="Exception">A delegate callback throws an exception.</exception>
    private bool RaiseUnGrabbingMouseEvent (View? view)
    {
        if (view is null)
        {
            return false;
        }

        GrabMouseEventArgs evArgs = new (view);
        UnGrabbingMouse?.Invoke (view, evArgs);

        return evArgs.Cancel;
    }

    /// <exception cref="Exception">A delegate callback throws an exception.</exception>
    private void RaiseGrabbedMouseEvent (View? view)
    {
        if (view is null)
        {
            return;
        }

        GrabbedMouse?.Invoke (view, new ViewEventArgs (view));
    }

    /// <exception cref="Exception">A delegate callback throws an exception.</exception>
    private void RaiseUnGrabbedMouseEvent (View? view)
    {
        if (view is null)
        {
            return;
        }

        UnGrabbedMouse?.Invoke (view, new ViewEventArgs (view));
    }

    /// <summary>
    ///     Handles mouse grab logic for a mouse event.
    /// </summary>
    /// <param name="deepestViewUnderMouse">The deepest view under the mouse.</param>
    /// <param name="mouse">The mouse event to handle.</param>
    /// <returns><see langword="true"/> if the event was handled by the grab handler; otherwise <see langword="false"/>.</returns>
    public bool HandleMouseGrab (View? deepestViewUnderMouse, Mouse mouse)
    {
        if (_mouseGrabViewRef?.TryGetTarget (out View? grabbed) is not true)
        {
            return false;
        }

#if DEBUG_IDISPOSABLE

        // TODO: Now that we use WeakRef for IsMouseGrabbed, it should be theoretically
        // TODO: impossible for this to happen.
        // TODO: Leave this in for a while to see if it is encountered just to makes sure.
        if (View.EnableDebugIDisposableAsserts && grabbed.WasDisposed)
        {
            throw new ObjectDisposedException (grabbed.ToDebugString ());
        }
#endif

        // If the mouse is grabbed, send the event to the view that grabbed it.
        // The coordinates are relative to the Bounds of the view that grabbed the mouse.
        Point frameLoc = grabbed.ScreenToViewport (mouse.ScreenPosition);

        Mouse viewRelativeMouseEvent = new ()
        {
            Timestamp = mouse.Timestamp,
            Position = frameLoc,
            Flags = mouse.Flags,
            ScreenPosition = mouse.ScreenPosition,
            View = grabbed // Always set to the grab view. See Issue #4370
        };

        Trace.Mouse ("app", mouse.Flags, mouse.ScreenPosition, "Grab");
        grabbed.NewMouseEvent (viewRelativeMouseEvent);

        // When the mouse is grabbed, always return true to prevent the event from propagating
        // to other views, regardless of whether the grabbed view handled it or not.
        // This ensures that during a drag operation starting on one view, other views don't
        // receive Enter/Leave events or process the mouse event.
        return true;
    }

    #endregion IMouseGrabHandler Implementation
}
