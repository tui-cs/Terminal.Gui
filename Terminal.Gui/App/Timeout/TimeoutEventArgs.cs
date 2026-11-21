namespace Terminal.Gui.App;

/// <summary><see cref="EventArgs"/> for timeout events (e.g. <see cref="TimedEvents.Added"/>)</summary>
public class TimeoutEventArgs : EventArgs
{
    /// <summary>Creates a new instance of the <see cref="TimeoutEventArgs"/> class.</summary>
    /// <param name="timeout"></param>
    /// <param name="ticks"></param>
    public TimeoutEventArgs (Timeout timeout, long ticks)
    {
        Timeout = timeout;
        Ticks = ticks;
    }

    /// <summary>
    ///     Gets the actual queue key, as a timestamp in 100-nanosecond units, after collision nudging has been applied.
    /// </summary>
    public long Ticks { get; }

    /// <summary>Gets the timeout callback handler</summary>
    public Timeout Timeout { get; }
}
