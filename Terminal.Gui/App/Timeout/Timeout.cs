namespace Terminal.Gui.App;

/// <summary>
///     Represents a scheduled timeout for use with timer management APIs.
///     <para>
///         Encapsulates a callback function to be invoked after a specified time interval. The callback can optionally
///         indicate whether the timeout should repeat.
///     </para>
///     <para>
///         Used by <see cref="ITimedEvents"/> and related timer systems to manage timed operations in the application.
///     </para>
/// </summary>
public class Timeout
{
    /// <summary>
    ///     Gets or sets the function to invoke when the timeout expires.
    /// </summary>
    /// <value>
    ///     A <see cref="Func{Boolean}"/> delegate. If the callback returns <see langword="true"/>, the timeout will be
    ///     rescheduled and invoked again after the same interval.
    ///     If the callback returns <see langword="false"/>, the timeout will be removed and not invoked again.
    /// </value>
    public Func<bool>? Callback { get; set; }

    /// <summary>
    ///     Gets or sets the time interval to wait before invoking the <see cref="Callback"/>.
    /// </summary>
    /// <value>
    ///     A <see cref="TimeSpan"/> representing the delay before the callback is invoked. If the timeout is rescheduled
    ///     (i.e., <see cref="Callback"/> returns <see langword="true"/>), this interval is used again.
    /// </value>
    public virtual TimeSpan Span { get; set; }
}
