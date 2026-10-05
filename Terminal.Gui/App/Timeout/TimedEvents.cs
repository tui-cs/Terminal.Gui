using System.Diagnostics;

namespace Terminal.Gui.App;

/// <summary>
///     Manages scheduled timeouts (timed callbacks) for the application.
///     <para>
///         Allows scheduling of callbacks to be invoked after a specified delay, with optional repetition.
///         Timeouts are stored in a sorted list by their scheduled execution time (high-resolution ticks).
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
///         <see cref="Add(TimeSpan, Func{bool})"/>, <see cref="Add(Timeout)"/>, <see cref="Remove"/>,
///         <see cref="StopAll"/>, <see cref="GetTimeout"/>, <see cref="CheckTimers"/>, and <see cref="RunTimers"/> are
///         safe to call concurrently from any thread. Timeout callbacks, <see cref="Timeout.Span"/> access,
///         <see cref="Added"/> handlers, and time-provider reads occur outside the timeout queue lock, so user code can
///         schedule or cancel timeouts without deadlocking.
///     </para>
///     <para>
///         <see cref="Timeouts"/> returns a snapshot so the queue can be inspected safely while another thread schedules
///         or cancels timeouts.
///     </para>
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

    // ActiveTimeoutState is a mutable struct, so TryGetValue hands back a copy. Every mutation must be written back
    // with _activeTimeoutStates [timeout] = state (or the entry removed) before _timeoutsLockToken is released.
    private readonly Dictionary<Timeout, ActiveTimeoutState> _activeTimeoutStates = new (ReferenceEqualityComparer.Instance);
    private readonly Dictionary<long, long> _queuedTimeoutOccurrenceIds = [];
    private readonly object _runTimersLockToken = new ();
    private readonly object _timeoutsLockToken = new ();
    private readonly ITimeProvider? _timeProvider;
    private long _nextTimeoutOccurrenceId;
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
    /// <remarks>
    ///     Returns a snapshot. Mutating the returned list does not change the scheduled timeouts. A timeout whose
    ///     callback is currently executing has already been dequeued and is not present.
    /// </remarks>
    public SortedList<long, Timeout> Timeouts
    {
        get
        {
            lock (_timeoutsLockToken)
            {
                return new (_timeouts);
            }
        }
    }

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

            // The same Timeout instance can be queued more than once, so the whole queue is scanned. Do not replace
            // this with an early-exiting lookup such as IndexOfValue.
            for (var i = _timeouts.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals (_timeouts.Values [i], timeout))
                {
                    continue;
                }

                long key = _timeouts.Keys [i];
                bool occurrenceRemoved = _queuedTimeoutOccurrenceIds.Remove (key);
                Debug.Assert (occurrenceRemoved);
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
        ArgumentNullException.ThrowIfNull (timeout);
        ArgumentNullException.ThrowIfNull (timeout.Callback);

        AddTimeout (timeout.Span, timeout);

        return timeout;
    }

    /// <summary>
    ///     Determines whether any timeout is queued and calculates how long the caller may wait before the earliest one
    ///     is due.
    /// </summary>
    /// <param name="waitTimeout">
    ///     The number of milliseconds until the earliest queued timeout is due, <c>0</c> if one is already due, or
    ///     <c>-1</c> if no timeout is queued. <c>-1</c> indicates the caller may wait indefinitely.
    /// </param>
    /// <returns><see langword="true"/> if at least one timeout is queued; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    ///     A timeout whose callback is currently executing has been dequeued and is therefore not counted.
    /// </remarks>
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

        var found = false;

        lock (_timeoutsLockToken)
        {
            foreach (Timeout queuedTimeout in _timeouts.Values)
            {
                if (!ReferenceEquals (queuedTimeout, timeout))
                {
                    continue;
                }

                found = true;

                break;
            }
        }

        return found ? timeout.Span : null;
    }

    private void AddTimeout (TimeSpan time, Timeout timeout)
    {
        long timestampTicks = GetTimestampTicks ();
        long k;

        lock (_timeoutsLockToken)
        {
            k = AddTimeoutCore (timestampTicks, time, timeout);
        }

        Added?.Invoke (this, new (timeout, k));
    }

    private long AddTimeoutCore (long timestampTicks, TimeSpan time, Timeout timeout)
    {
        // Caller must hold _timeoutsLockToken.
        Debug.Assert (Monitor.IsEntered (_timeoutsLockToken));

        long k = timestampTicks + time.Ticks;

        // if user wants to run as soon as possible set timer such that it expires right away (no race conditions)
        if (time == TimeSpan.Zero)
        {
            // Use a more substantial buffer (1ms) to ensure it's truly in the past
            // even under debugger overhead and extreme timing variations
            k -= TimeSpan.TicksPerMillisecond;
        }

        k = NudgeToUniqueKey (k);
        _timeouts.Add (k, timeout);
        _queuedTimeoutOccurrenceIds.Add (k, _nextTimeoutOccurrenceId++);

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
        long occurrenceIdCutoff;

        lock (_timeoutsLockToken)
        {
            if (_timeouts.Count == 0)
            {
                return;
            }

            occurrenceIdCutoff = _nextTimeoutOccurrenceId;
        }

        long passStart = GetTimestampTicks ();

        // Execute only queue occurrences that were due when this pass started. A repeating timeout with a zero or
        // negative span reschedules as already due, but receives a new occurrence ID and is deferred to a later pass.
        // Without occurrence identity, it can reuse its freed queue key and starve an already-due peer indefinitely.
        while (true)
        {
            ActiveTimeoutOccurrence occurrence;

            lock (_timeoutsLockToken)
            {
                int timeoutIndex = GetNextDueTimeoutIndex (passStart, occurrenceIdCutoff);

                if (timeoutIndex == -1)
                {
                    return;
                }

                long scheduledTime = _timeouts.Keys [timeoutIndex];
                Timeout timeoutToExecute = _timeouts.Values [timeoutIndex];
                bool occurrenceRemoved = _queuedTimeoutOccurrenceIds.Remove (scheduledTime);
                Debug.Assert (occurrenceRemoved);
                _timeouts.RemoveAt (timeoutIndex);
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

    private int GetNextDueTimeoutIndex (long passStart, long occurrenceIdCutoff)
    {
        // Caller must hold _timeoutsLockToken.
        Debug.Assert (Monitor.IsEntered (_timeoutsLockToken));
        Debug.Assert (_timeouts.Count == _queuedTimeoutOccurrenceIds.Count);

        for (var i = 0; i < _timeouts.Count; i++)
        {
            long scheduledTime = _timeouts.Keys [i];

            if (scheduledTime > passStart)
            {
                return -1;
            }

            bool found = _queuedTimeoutOccurrenceIds.TryGetValue (scheduledTime, out long occurrenceId);
            Debug.Assert (found);

            if (found && occurrenceId < occurrenceIdCutoff)
            {
                return i;
            }
        }

        return -1;
    }

    private void CompleteTimeout (ActiveTimeoutOccurrence occurrence, bool repeat)
    {
        if (!repeat || !CanReschedule (occurrence))
        {
            CompleteTimeoutCore (occurrence, false, default, 0);

            return;
        }

        TimeSpan repeatInterval = default;
        long timestampTicks = 0;
        var rescheduleInputsRead = false;

        try
        {
            repeatInterval = occurrence.Timeout.Span;
            timestampTicks = GetTimestampTicks ();
            rescheduleInputsRead = true;
        }
        finally
        {
            // Span and the time provider are user-overridable. Read them without the queue lock, then revalidate the
            // occurrence in CompleteTimeoutCore before enqueueing it. The finally also releases active-state bookkeeping
            // if either read throws.
            CompleteTimeoutCore (occurrence, rescheduleInputsRead, repeatInterval, timestampTicks);
        }
    }

    private bool CanReschedule (ActiveTimeoutOccurrence occurrence)
    {
        lock (_timeoutsLockToken)
        {
            bool found = _activeTimeoutStates.TryGetValue (occurrence.Timeout, out ActiveTimeoutState state);
            Debug.Assert (found);

            return found
                   && occurrence.StopAllEpoch == _stopAllEpoch
                   && occurrence.RemovalGeneration == state.RemovalGeneration;
        }
    }

    private void CompleteTimeoutCore (
        ActiveTimeoutOccurrence occurrence,
        bool repeat,
        TimeSpan repeatInterval,
        long timestampTicks)
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

            k = AddTimeoutCore (timestampTicks, repeatInterval, occurrence.Timeout);
        }

        Added?.Invoke (this, new (occurrence.Timeout, k));
    }

    /// <inheritdoc/>
    public void StopAll ()
    {
        lock (_timeoutsLockToken)
        {
            _timeouts.Clear ();
            _queuedTimeoutOccurrenceIds.Clear ();
            _stopAllEpoch++;
        }
    }

    /// <summary>
    ///     Identifies a single in-flight execution of a <see cref="Timeout"/>, capturing the cancellation state that was
    ///     current when the occurrence was dequeued. <see cref="CompleteTimeout"/> reschedules only if both values still
    ///     match, which is how <see cref="Remove"/> and <see cref="StopAll"/> cancel an active occurrence without
    ///     affecting occurrences created after them.
    /// </summary>
    private readonly record struct ActiveTimeoutOccurrence (Timeout Timeout, long StopAllEpoch, long RemovalGeneration);

    /// <summary>
    ///     Per-<see cref="Timeout"/> cancellation bookkeeping. Mutable; see the comment on
    ///     <see cref="_activeTimeoutStates"/> for the write-back requirement.
    /// </summary>
    private struct ActiveTimeoutState
    {
        /// <summary>Number of occurrences of this timeout that are currently executing.</summary>
        public int ActiveCount { get; set; }

        /// <summary>Incremented by <see cref="Remove"/> to invalidate every occurrence dequeued before it.</summary>
        public long RemovalGeneration { get; set; }

        /// <summary>The <see cref="_stopAllEpoch"/> value that <see cref="UncancelledActiveCount"/> is scoped to.</summary>
        public long StopAllEpoch { get; set; }

        /// <summary>
        ///     Number of active occurrences still matching <see cref="StopAllEpoch"/> and <see cref="RemovalGeneration"/>,
        ///     used so <see cref="Remove"/> can report whether it actually cancelled anything.
        /// </summary>
        public int UncancelledActiveCount { get; set; }
    }
}
