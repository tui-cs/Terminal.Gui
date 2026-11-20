namespace Terminal.Gui.App;

/// <summary>
///     Represents a running session created by <see cref="IApplication.Begin(IRunnable)"/>.
///     Wraps an <see cref="IRunnable"/> instance and is stored in <see cref="IApplication.SessionStack"/>.
/// </summary>
public class SessionToken
{
    internal SessionToken (IRunnable runnable) => Runnable = runnable;

    /// <summary>
    ///     Gets or sets the runnable associated with this session.
    ///     Set to <see langword="null"/> by <see cref="IApplication.End(SessionToken)"/> when the session completes.
    /// </summary>
    public IRunnable? Runnable { get; internal set; }

    private int _endClaimed;

    /// <summary>
    ///     Claims this session for <see cref="IApplication.End(SessionToken)"/>. Fails while another call holds the
    ///     claim, and after teardown has started.
    /// </summary>
    internal bool TryClaimEnd () => Interlocked.CompareExchange (ref _endClaimed, 1, 0) == 0;

    /// <summary>Releases the claim taken by <see cref="TryClaimEnd"/> when stopping is canceled or fails.</summary>
    internal void ReleaseEndClaim () => Volatile.Write (ref _endClaimed, 0);

    private bool _isDispatchClosed;

    /// <summary>Whether this session has stopped accepting and running owned dispatches.</summary>
    internal bool IsDispatchClosed
    {
        get => Volatile.Read (ref _isDispatchClosed);
        set => Volatile.Write (ref _isDispatchClosed, value);
    }

    /// <summary>Whether owned dispatches may start: the session is running and has not begun to end.</summary>
    internal bool IsDispatchActive => !IsDispatchClosed && Runnable is { IsRunning: true };

    /// <summary>
    ///     The result of the session. Typically set by the runnable in <see langword="IRunnable.IsRunningChanged"/>
    /// </summary>
    public object? Result { get; set; }
}
