using Timeout = Terminal.Gui.App.Timeout;

namespace Terminal.Gui.ViewBase;

/// <summary>
///     INTERNAL: Manages the logic for handling a "mouse held down" state on a View. It is used to
///     repeatedly trigger an action (via events) while the mouse button is held down, such as for auto-repeat in
///     scrollbars or buttons.
/// </summary>
/// <remarks>
///     <para>
///         This class implements an accelerating timeout pattern by default: the first tick occurs after 500ms,
///         subsequent ticks accelerate with a 0.7 decay factor down to a minimum of 50ms.
///     </para>
///     <para>
///         When started, it automatically grabs the mouse to ensure all mouse events are directed to the host view.
///         The <see cref="MouseIsHeldDownTick"/> event is raised periodically until <see cref="Stop"/> is called
///         or the event is cancelled.
///     </para>
///     <para>
///         This is typically used by views that set <see cref="View.MouseHoldRepeat"/> to <see langword="true"/>,
///         enabling behaviors like auto-scrolling or button repeat.
///     </para>
///     <para>
///         For testing or custom timing behavior, set the <see cref="Timeout"/> property before calling
///         <see cref="Start"/>.
///     </para>
/// </remarks>
internal sealed class MouseHoldRepeaterImpl : IMouseHoldRepeater
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MouseHoldRepeaterImpl"/> class.
    /// </summary>
    /// <param name="host">The view that will receive the mouse held down events.</param>
    /// <param name="timedEvents">The timed events service for scheduling periodic ticks. Can be null for testing.</param>
    /// <param name="mouseGrabber">The mouse grab handler for managing mouse capture. Can be null for testing.</param>
    public MouseHoldRepeaterImpl (View host, ITimedEvents? timedEvents, IMouseGrabHandler? mouseGrabber)
    {
        _mouseGrabView = host;
        _timedEvents = timedEvents;
        _mouseGrabber = mouseGrabber;
    }

    private readonly View _mouseGrabView;
    private readonly ITimedEvents? _timedEvents;
    private readonly IMouseGrabHandler? _mouseGrabber;

    private Timeout? _timeout;
    private Timeout? _userTimeout;
    private bool _isDown;
    private object? _timeoutToken;
    private long _mouseDisableGeneration;

    private bool HoldWasInvalidated =>
        _mouseGrabber is ApplicationMouse mouse && _mouseDisableGeneration != mouse.MouseDisableGeneration;

    /// <inheritdoc/>
    public Timeout? Timeout
    {
        get => _userTimeout;
        set
        {
            if (_isDown)
            {
                throw new InvalidOperationException ("Cannot change timeout while mouse is held down. Call Stop() first.");
            }
            _userTimeout = value;
        }
    }

    /// <summary>
    ///     The most recent mouse event arguments associated with the mouse held down action.
    /// </summary>
    private Mouse? _mouseEvent;

    public void Start (Mouse mouse)
    {
        // A fresh press can arrive before the invalidated hold's next scheduled tick.
        if (HoldWasInvalidated)
        {
            Stop ();
        }

        if (_isDown)
        {
            return;
        }

        _mouseDisableGeneration = (_mouseGrabber as ApplicationMouse)?.MouseDisableGeneration ?? 0;
        _mouseEvent = new Mouse
        {
            Timestamp = mouse.Timestamp,
            Flags = mouse.Flags,
            Position = mouse.Position,
            ScreenPosition = mouse.ScreenPosition,
            View = mouse.View
        };

        //Logging.Trace ($"host: {_mouseGrabView.Id} {_mouseEvent.View?.Id}: {_mouseEvent.Flags}");

        _isDown = true;
        _mouseGrabber?.GrabMouse (_mouseGrabView);

        // A grab callback can disable mouse handling and stop this repeater before startup resumes.
        if (!_isDown)
        {
            return;
        }

        // A grab callback can disable and re-enable reporting before startup resumes.
        if (HoldWasInvalidated)
        {
            Stop ();
            return;
        }

        // Use custom timeout if provided, otherwise default to SmoothAcceleratingTimeout
        if (_userTimeout != null)
        {
            _timeout = _userTimeout;
            _timeout.Callback = TickWhileMouseIsHeldDown;
        }
        else
        {
            _timeout = new SmoothAcceleratingTimeout (TimeSpan.FromMilliseconds (500), TimeSpan.FromMilliseconds (10), 0.8, TickWhileMouseIsHeldDown);
        }

        // Then periodic ticks
        _timeoutToken = _timedEvents?.Add (_timeout);
    }

    public void Stop ()
    {
        if (_mouseEvent is null)
        {
            //Logging.Trace ($"host: {_mouseGrabView.Id}");

            return;
        }

        //Logging.Trace ($"host: {_mouseGrabView.Id} {_mouseEvent.View?.Id}: {_mouseEvent.Flags}");

        _mouseEvent = null;

        // Reset timeout if it's SmoothAcceleratingTimeout
        if (_timeout is SmoothAcceleratingTimeout smoothTimeout)
        {
            smoothTimeout.Reset ();
        }

        if (_mouseGrabber is { } && _mouseGrabber.IsGrabbed (_mouseGrabView))
        {
            _mouseGrabber.UngrabMouse ();
        }

        if (_timeoutToken != null)
        {
            _timedEvents?.Remove (_timeoutToken);
        }

        _mouseGrabView.MouseState = MouseState.None;
        _isDown = false;
    }

    public void Dispose ()
    {
        if (_mouseGrabber is { } && _mouseGrabber.IsGrabbed (_mouseGrabView))
        {
            //Logging.Trace ($"host: {_mouseGrabView.Id} Disposing and ungrabbing mouse");
            Stop ();
        }
    }

    public event EventHandler<CancelEventArgs<Mouse>>? MouseIsHeldDownTick;

    private void RaiseMouseIsHeldDownTick ()
    {
        Mouse currentMouseEventArgs = _mouseEvent ?? new Mouse ();
        Mouse newMouseEventArgs = _mouseEvent ?? new Mouse ();

        if (_mouseEvent?.View?.Visible is false || _mouseEvent?.View?.Enabled is false)
        {
            Stop ();

            return;
        }

        CancelEventArgs<Mouse> args = new (ref currentMouseEventArgs, ref newMouseEventArgs);

        MouseIsHeldDownTick?.Invoke (this, args);

        // User event cancelled the mouse held down status so
        // stop the currently running operation.
        if (args.Cancel)
        {
            Stop ();
        }
    }

    private bool TickWhileMouseIsHeldDown ()
    {
        // Checking only IsMouseDisabled would miss an off/on cycle between ticks.
        if (HoldWasInvalidated)
        {
            Stop ();
            return false;
        }

        if (_isDown)
        {
            // Only advance stage if using SmoothAcceleratingTimeout
            if (_timeout is SmoothAcceleratingTimeout smoothTimeout)
            {
                smoothTimeout.AdvanceStage ();
            }
            RaiseMouseIsHeldDownTick ();
        }
        else
        {
            // Only reset if using SmoothAcceleratingTimeout
            if (_timeout is SmoothAcceleratingTimeout smoothTimeout)
            {
                smoothTimeout.Reset ();
            }
            Stop ();
        }

        return _isDown;
    }
}
