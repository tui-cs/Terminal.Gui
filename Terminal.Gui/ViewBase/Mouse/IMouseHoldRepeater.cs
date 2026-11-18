using Timeout = Terminal.Gui.App.Timeout;

namespace Terminal.Gui.ViewBase;

/// <summary>
///     <para>
///         Handler for raising periodic events while the mouse is held down.
///         Typically, mouse button only needs to be pressed down in a view
///         to begin this event after which it can be moved elsewhere.
///     </para>
///     <para>
///         Common use cases for this includes holding a button down to increase
///         a counter (e.g. in <see cref="NumericUpDown"/>).
///     </para>
/// </summary>
public interface IMouseHoldRepeater : IDisposable
{
    /// <summary>
    ///     Gets or sets the timeout behavior for mouse hold repetition.
    ///     If not set, defaults to <see cref="SmoothAcceleratingTimeout"/> with 500ms initial delay,
    ///     50ms minimum delay, and 0.7 decay factor.
    /// </summary>
    /// <remarks>
    ///     Setting a custom timeout allows tests to use simpler, more predictable timing behavior.
    ///     The timeout's callback will be set automatically by the repeater.
    /// </remarks>
    Timeout? Timeout { get; set; }

    /// <summary>
    ///     Periodically raised when the mouse is pressed down inside the view <see cref="View"/>.
    /// </summary>
    public event EventHandler<CancelEventArgs<Mouse>> MouseIsHeldDownTick;

    /// <summary>
    ///     Call to indicate that the mouse has been pressed down and any relevant actions should
    ///     be undertaken (start timers, <see cref="IMouseGrabHandler.GrabMouse"/> etc).
    /// </summary>
    /// <param name="mouseEventArgs"></param>
    void Start (Mouse mouseEventArgs);

    /// <summary>
    ///     Call to indicate that the mouse has been released and any relevant actions should
    ///     be undertaken (stop timers, <see cref="IMouseGrabHandler.UngrabMouse"/> etc).
    /// </summary>
    void Stop ();
}
