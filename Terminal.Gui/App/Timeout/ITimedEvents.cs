namespace Terminal.Gui.App;

/// <summary>
///     Manages timers.
/// </summary>
public interface ITimedEvents
{
    /// <summary>
    ///     Adds a timeout to the application.
    /// </summary>
    /// <remarks>
    ///     When the specified time passes, the callback will be invoked. If the callback returns <see langword="true"/>, the
    ///     timeout will be
    ///     reset, repeating the invocation. If it returns <see langword="false"/>, the timeout will stop and be removed. The
    ///     returned value is a
    ///     token that can be used to stop the timeout by calling <see cref="Remove"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="callback"/> is <see langword="null"/>.</exception>
    object Add (TimeSpan time, Func<bool> callback);

    /// <inheritdoc cref="Add(System.TimeSpan,System.Func{bool})"/>
    /// <remarks>
    ///     Adding the same <see cref="Timeout"/> instance more than once creates multiple occurrences that share one
    ///     cancellation token. Calling <see cref="Remove"/> with that token cancels all matching occurrences that precede
    ///     the removal operation's synchronization point.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="timeout"/> or its <see cref="Timeout.Callback"/> is <see langword="null"/>.
    /// </exception>
    object Add (Timeout timeout);

    /// <summary>
    ///     Invoked when a new timeout is added. To be used in the case when
    ///     <see cref="IApplication.StopAfterFirstIteration"/> is <see langword="true"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The event is raised after the timeout is added and after the timeout queue lock is released. A concurrent
    ///         <see cref="RunTimers"/> call can execute a due timeout before its <see cref="Added"/> handler runs.
    ///     </para>
    ///     <para>
    ///         A handler exception propagates to whatever caused the timeout to be scheduled. For a repeating timeout that
    ///         is being rescheduled, that is the <see cref="RunTimers"/> call, and it ends that timer pass. The timeout
    ///         remains scheduled, so a handler that throws on every reschedule ends every subsequent pass as well.
    ///     </para>
    /// </remarks>
    event EventHandler<TimeoutEventArgs>? Added;

    /// <summary>
    ///     Cancels all timeout occurrences associated with a previously returned token.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The token parameter is the value returned by <see cref="Add(TimeSpan, Func{bool})"/> or
    ///         <see cref="Add(Timeout)"/>. All matching queued occurrences are removed.
    ///     </para>
    ///     <para>
    ///         A matching callback that is already executing is not interrupted, but it will not be rescheduled if it
    ///         returns <see langword="true"/>. Occurrences registered after the cancellation takes effect are not affected,
    ///         including occurrences created by adding the same <see cref="Timeout"/> instance again. A concurrent
    ///         <see cref="Add(TimeSpan, Func{bool})"/> or <see cref="Add(Timeout)"/> call can be ordered before or after the
    ///         cancellation.
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     <see langword="true"/> if at least one queued occurrence was removed or a cancellation request was recorded for
    ///     at least one active occurrence; otherwise, <see langword="false"/>.
    /// </returns>
    bool Remove (object token);

    /// <summary>
    ///     Runs timeouts that are due.
    /// </summary>
    /// <remarks>
    ///     Timeout callbacks are serialized. A nested call on the active runner thread is supported. A call from a
    ///     competing thread returns without running callbacks; remaining due timeouts are processed by a later successful
    ///     call. Each pass executes only queue occurrences that were due when it started. An occurrence added or rescheduled
    ///     after the pass starts is deferred to a later pass, preventing an immediately repeating callback from starving
    ///     already-due peers. A callback exception propagates directly from the call that executes it and ends that timer
    ///     pass.
    /// </remarks>
    void RunTimers ();

    /// <summary>
    ///     Returns a snapshot containing the next planned execution timestamp, in 100-nanosecond units, for each timeout
    ///     that is not actively executing. Mutating the returned list does not change the scheduled timeouts.
    /// </summary>
    SortedList<long, Timeout> Timeouts { get; }

    /// <summary>
    ///     Gets the configured interval for a queued timeout occurrence associated with the specified token.
    /// </summary>
    /// <param name="token">The token of the event.</param>
    /// <returns>
    ///     The <see cref="TimeSpan"/> for a queued occurrence, or <see langword="null"/> if no queued occurrence is found.
    ///     A callback can be actively executing when this method returns <see langword="null"/>; use <see cref="Remove"/>
    ///     to prevent an active repeating callback from rescheduling.
    /// </returns>
    TimeSpan? GetTimeout (object token);

    /// <summary>Stops and removes all timed events.</summary>
    /// <remarks>
    ///     Removes all queued timeout occurrences and prevents callbacks that are active when this method is called from
    ///     rescheduling. It does not interrupt callbacks already executing. Timeouts added after the cancellation takes
    ///     effect are unaffected, including a reused <see cref="Timeout"/> instance. A concurrent
    ///     <see cref="Add(TimeSpan, Func{bool})"/> or <see cref="Add(Timeout)"/> call can be ordered before or after the
    ///     cancellation.
    /// </remarks>
    void StopAll ();
}
