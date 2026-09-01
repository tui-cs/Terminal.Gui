using System.Diagnostics;

namespace Terminal.Gui.App;

/// <summary>
///     Manages scheduled timeouts (timed callbacks) for the application.
///     <para>
///         Allows scheduling of callbacks to be invoked after a specified delay, with optional repetition.
///         Timeouts are stored in a sorted list by their scheduled execution time (high-resolution ticks).
///         Thread-safe for concurrent access.
///     </para>
///     <para>
///         Typical usage:
///         <list type="number">
///             <item>
///                 <description>Call <see cref="Add(TimeSpan, Func{bool})"/> to schedule a callback.</description>
///             </item>
///             <item>
///                 <description>
///                     Call <see cref="RunTimers"/> periodically (e.g., from the main loop) to execute due
///                     callbacks.
///                 </description>
///             </item>
///             <item>
///                 <description>Call <see cref="Remove"/> to cancel a scheduled timeout.</description>
///             </item>
///         </list>
///     </para>
/// </summary>
/// <remarks>
///     <para>
///         By default, uses <see cref="Stopwatch.GetTimestamp"/> for high-resolution timing to provide microsecond-level
///         precision and eliminate race conditions from timer resolution issues.
///     </para>
///     <para>
///         For testing scenarios, an <see cref="ITimeProvider"/> can be injected via the constructor to enable
///         virtual time control, allowing tests to run instantly without real delays.
///     </para>
/// </remarks>
public class TimedEvents : ITimedEvents
{
    internal SortedList<long, Timeout> _timeouts = new ();
    private readonly Dictionary<Timeout, ActiveTimeoutState> _activeTimeoutStates = new (ReferenceEqualityComparer.Instance);
    private readonly object _runTimersLockToken = new ();
    private readonly object _timeoutsLockToken = new ();
    private readonly ITimeProvider? _timeProvider;
    private long _stopAllEpoch;

    /// <summary>
    ///     Initializes a new instance of <see cref="TimedEvents"/> with the default system time provider.
    /// </summary>
    public TimedEvents () : this (null) { }

    /// <summary>
    ///     Initializes a new instance of <see cref="TimedEvents"/> with the specified time provider.
    /// </summary>
    /// <param name="timeProvider">
    ///     The time provider to use for timing. If <see langword="null"/>, uses <see cref="Stopwatch.GetTimestamp"/>
    ///     for high-resolution system time.
    /// </param>
    /// <remarks>
    ///     For production use, pass <see langword="null"/> or omit to use the default high-resolution timing.
    ///     For testing, pass a <see cref="VirtualTimeProvider"/> to enable deterministic time control.
    /// </remarks>
    public TimedEvents (ITimeProvider? timeProvider)
    {
        _timeProvider = timeProvider;
    }

    /// <summary>
    ///     Gets the list of all timeouts sorted by the <see cref="TimeSpan"/> time ticks. A shorter limit time can be
    ///     added at the end, but it will be called before an earlier addition that has a longer limit time.
    /// </summary>
    public SortedList<long, Timeout> Timeouts => _timeouts;

    /// <inheritdoc/>
    public event EventHandler<TimeoutEventArgs>? Added;

    /// <summary>
    ///     Gets the current timestamp in TimeSpan ticks (100-nanosecond units).
    ///     Uses either the configured <see cref="ITimeProvider"/> or <see cref="Stopwatch.GetTimestamp"/>
    ///     for high-resolution timing.
    /// </summary>
    /// <returns>Current timestamp in TimeSpan ticks (100-nanosecond units).</returns>
    private long GetTimestampTicks ()
    {
        if (_timeProvider != null)
        {
            // Use ITimeProvider for testable, controllable time
            return _timeProvider.Now.Ticks;
        }

        // Default: Use Stopwatch for high-resolution system time
        // Convert Stopwatch ticks to TimeSpan ticks (100-nanosecond units)
        // Stopwatch.Frequency gives ticks per second, so we need to scale appropriately
        // To avoid overflow, we perform the operation in double precision first and then cast to long.
        var ticks = (long)((double)Stopwatch.GetTimestamp () * TimeSpan.TicksPerSecond / Stopwatch.Frequency);

        // Ensure ticks is positive and not overflowed (very unlikely now)
        Debug.Assert (ticks > 0);

        return ticks;
    }

    /// <inheritdoc/>
    public void RunTimers ()
    {
        // A monitor is reentrant, so nested runs on this thread remain supported. A competing caller returns while the
        // active runner drains the due timeouts.
        if (!Monitor.TryEnter (_runTimersLockToken))
        {
            return;
        }

        try
        {
            RunTimersImpl ();
        }
        finally
        {
            Monitor.Exit (_runTimersLockToken);
        }
    }

    /// <inheritdoc/>
    public bool Remove (object token)
    {
        Timeout? timeout = token as Timeout;

        if (timeout is null)
        {
            return false;
        }

        lock (_timeoutsLockToken)
        {
            var found = false;

            for (var i = _timeouts.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals (_timeouts.Values [i], timeout))
                {
                    continue;
                }

                _timeouts.RemoveAt (i);
                found = true;
            }

            if (_activeTimeoutStates.TryGetValue (timeout, out ActiveTimeoutState state)
                && state.StopAllEpoch == _stopAllEpoch
                && state.UncancelledActiveCount > 0)
            {
                state.RemovalGeneration++;
                state.UncancelledActiveCount = 0;
                _activeTimeoutStates [timeout] = state;
                found = true;
            }

            return found;
        }
    }

    /// <inheritdoc/>
    public object Add (TimeSpan time, Func<bool> callback)
    {
        ArgumentNullException.ThrowIfNull (callback);

        var timeout = new Timeout { Span = time, Callback = callback };
        AddTimeout (time, timeout);

        return timeout;
    }

    /// <inheritdoc/>
    public object Add (Timeout timeout)
    {
        AddTimeout (timeout.Span, timeout);

        return timeout;
    }

    /// <inheritdoc/>
    public bool CheckTimers (out int waitTimeout)
    {
        long now = GetTimestampTicks ();

        waitTimeout = 0;

        lock (_timeoutsLockToken)
        {
            if (_timeouts.Count > 0)
            {
                waitTimeout = (int)((_timeouts.Keys [0] - now) / TimeSpan.TicksPerMillisecond);

                if (waitTimeout < 0)
                {
                    // This avoids 'poll' waiting infinitely if 'waitTimeout < 0' until some action is detected
                    // This can occur after IMainLoopDriver.Wakeup is executed where the pollTimeout is less than 0
                    // and no event occurred in elapsed time when the 'poll' is start running again.
                    waitTimeout = 0;
                }

                return true;
            }

            // ManualResetEventSlim.Wait, which is called by IMainLoopDriver.EventsPending, will wait indefinitely if
            // the timeout is -1.
            waitTimeout = -1;
        }

        return false;
    }

    /// <inheritdoc/>
    public TimeSpan? GetTimeout (object token)
    {
        if (token is not Timeout timeout)
        {
            return null;
        }

        lock (_timeoutsLockToken)
        {
            foreach (Timeout queuedTimeout in _timeouts.Values)
            {
                if (ReferenceEquals (queuedTimeout, timeout))
                {
                    return timeout.Span;
                }
            }

            return null;
        }
    }

    private void AddTimeout (TimeSpan time, Timeout timeout)
    {
        long k;

        lock (_timeoutsLockToken)
        {
            k = AddTimeoutCore (time, timeout);
        }

        Added?.Invoke (this, new (timeout, k));
    }

    private long AddTimeoutCore (TimeSpan time, Timeout timeout)
    {
        // Caller must hold _timeoutsLockToken.
        Debug.Assert (Monitor.IsEntered (_timeoutsLockToken));

        long k = GetTimestampTicks () + time.Ticks;

        // if user wants to run as soon as possible set timer such that it expires right away (no race conditions)
        if (time == TimeSpan.Zero)
        {
            // Use a more substantial buffer (1ms) to ensure it's truly in the past
            // even under debugger overhead and extreme timing variations
            k -= TimeSpan.TicksPerMillisecond;
        }

        _timeouts.Add (NudgeToUniqueKey (k), timeout);

        return k;
    }

    /// <summary>
    ///     Finds the closest number to <paramref name="k"/> that is not present in <see cref="_timeouts"/>
    ///     (incrementally).
    /// </summary>
    /// <param name="k"></param>
    /// <returns></returns>
    private long NudgeToUniqueKey (long k)
    {
        // Caller must hold _timeoutsLockToken.
        Debug.Assert (Monitor.IsEntered (_timeoutsLockToken));

        while (_timeouts.ContainsKey (k))
        {
            k++;
        }

        return k;
    }

    private void RunTimersImpl ()
    {
        // Process due timeouts one at a time, without blocking the entire queue
        while (true)
        {
            ActiveTimeoutOccurrence occurrence;

            // Find the next due timeout
            lock (_timeoutsLockToken)
            {
                if (_timeouts.Count == 0)
                {
                    return;
                }

                // Re-evaluate current time for each iteration
                long now = GetTimestampTicks ();

                // Check if the earliest timeout is due
                long scheduledTime = _timeouts.Keys [0];

                if (scheduledTime > now)
                {
                    return;
                }

                // This timeout is due - remove it from the queue
                Timeout timeoutToExecute = _timeouts.Values [0];
                _timeouts.RemoveAt (0);
                _activeTimeoutStates.TryGetValue (timeoutToExecute, out ActiveTimeoutState state);

                if (state.StopAllEpoch != _stopAllEpoch)
                {
                    state.StopAllEpoch = _stopAllEpoch;
                    state.UncancelledActiveCount = 0;
                }

                occurrence = new (timeoutToExecute, _stopAllEpoch, state.RemovalGeneration);
                state.ActiveCount++;
                state.UncancelledActiveCount++;
                _activeTimeoutStates [timeoutToExecute] = state;
            }

            // Execute the callback outside the lock
            // This allows nested RunTimers() calls to access the timeout queue
            bool repeat = false;

            try
            {
                repeat = occurrence.Timeout.Callback! ();
            }
            finally
            {
                CompleteTimeout (occurrence, repeat);
            }
        }
    }

    private void CompleteTimeout (ActiveTimeoutOccurrence occurrence, bool repeat)
    {
        long k;

        lock (_timeoutsLockToken)
        {
            bool found = _activeTimeoutStates.TryGetValue (occurrence.Timeout, out ActiveTimeoutState state);
            Debug.Assert (found);

            if (!found)
            {
                return;
            }

            state.ActiveCount--;
            bool canReschedule = occurrence.StopAllEpoch == _stopAllEpoch
                                 && occurrence.RemovalGeneration == state.RemovalGeneration;

            if (canReschedule)
            {
                state.UncancelledActiveCount--;
                Debug.Assert (state.UncancelledActiveCount >= 0);
            }

            if (state.ActiveCount == 0)
            {
                _activeTimeoutStates.Remove (occurrence.Timeout);
            }
            else
            {
                _activeTimeoutStates [occurrence.Timeout] = state;
            }

            if (!repeat || !canReschedule)
            {
                return;
            }

            k = AddTimeoutCore (occurrence.Timeout.Span, occurrence.Timeout);
        }

        Added?.Invoke (this, new (occurrence.Timeout, k));
    }

    /// <inheritdoc/>
    public void StopAll ()
    {
        lock (_timeoutsLockToken)
        {
            _timeouts.Clear ();
            _stopAllEpoch++;
        }
    }

    private readonly record struct ActiveTimeoutOccurrence (Timeout Timeout, long StopAllEpoch, long RemovalGeneration);

    private struct ActiveTimeoutState
    {
        public int ActiveCount { get; set; }
        public long RemovalGeneration { get; set; }
        public long StopAllEpoch { get; set; }
        public int UncancelledActiveCount { get; set; }
    }
}
