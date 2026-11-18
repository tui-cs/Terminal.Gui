namespace Terminal.Gui.Time;

/// <summary>
///     Virtual time provider for testing - all time is controlled.
/// </summary>
public class VirtualTimeProvider : ITimeProvider
{
    private readonly List<VirtualTimer> _timers = [];
    private readonly List<VirtualDelay> _delays = [];

    /// <inheritdoc/>
    public DateTime Now { get; private set; } = new (2025, 1, 1, 0, 0, 0);

    /// <summary>
    ///     Advances virtual time by the specified duration.
    ///     This triggers any timers/delays that should fire.
    /// </summary>
    /// <param name="duration">The time span to advance.</param>
    public void Advance (TimeSpan duration)
    {
        Now += duration;

        // Fire any timers that should trigger
        foreach (VirtualTimer timer in _timers.Where (t => t.IsRunning && t.NextTrigger <= Now).ToList ())
        {
            timer.Trigger (Now);
        }

        // Complete any delays that should finish
        foreach (VirtualDelay delay in _delays.Where (d => !d.IsCompleted && d.CompletionTime <= Now).ToList ())
        {
            delay.Complete ();
        }
    }

    /// <summary>
    ///     Sets virtual time to a specific value.
    /// </summary>
    /// <param name="time">The new current time.</param>
    public void SetTime (DateTime time) { Now = time; }

    /// <inheritdoc/>
    public Task Delay (TimeSpan duration, CancellationToken cancellationToken = default)
    {
        VirtualDelay delay = new (Now + duration, cancellationToken);
        _delays.Add (delay);

        return delay.Task;
    }

    /// <inheritdoc/>
    public ITimer CreateTimer (TimeSpan interval, Action callback)
    {
        VirtualTimer timer = new (Now, interval, callback);
        _timers.Add (timer);

        return timer;
    }

    /// <summary>
    ///     Removes completed delays from the internal list (for cleanup).
    /// </summary>
    internal void CleanupCompletedDelays () { _delays.RemoveAll (d => d.IsCompleted); }

    /// <summary>
    ///     Removes disposed timers from the internal list (for cleanup).
    /// </summary>
    internal void CleanupDisposedTimers () { _timers.RemoveAll (t => t.IsDisposed); }
}

/// <summary>
///     Virtual timer for testing - fires based on virtual time advancement.
/// </summary>
internal class VirtualTimer : ITimer
{
    private readonly Action _callback;
    private readonly TimeSpan _interval;

    /// <summary>
    ///     Initializes a new instance of the <see cref="VirtualTimer"/> class.
    /// </summary>
    /// <param name="currentTime">The current virtual time.</param>
    /// <param name="interval">The interval between timer callbacks.</param>
    /// <param name="callback">The action to invoke when the timer fires.</param>
    public VirtualTimer (DateTime currentTime, TimeSpan interval, Action callback)
    {
        _interval = interval;
        _callback = callback;
        NextTrigger = currentTime + interval;
    }

    /// <inheritdoc/>
    public bool IsRunning { get; private set; }

    /// <summary>
    ///     Gets a value indicating whether this timer has been disposed.
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    ///     Gets the next time this timer should trigger.
    /// </summary>
    public DateTime NextTrigger { get; private set; }

    /// <inheritdoc/>
    public void Start () { IsRunning = true; }

    /// <inheritdoc/>
    public void Stop () { IsRunning = false; }

    /// <summary>
    ///     Triggers the timer callback and schedules the next trigger.
    /// </summary>
    /// <param name="currentTime">The current virtual time.</param>
    public void Trigger (DateTime currentTime)
    {
        if (!IsRunning || IsDisposed)
        {
            return;
        }

        _callback ();
        NextTrigger = currentTime + _interval;
    }

    /// <inheritdoc/>
    public void Dispose ()
    {
        IsRunning = false;
        IsDisposed = true;
    }
}

/// <summary>
///     Virtual delay for testing - completes based on virtual time advancement.
/// </summary>
internal class VirtualDelay
{
    private readonly TaskCompletionSource<bool> _completionSource = new ();
    private readonly CancellationTokenRegistration _cancellationRegistration;

    /// <summary>
    ///     Initializes a new instance of the <see cref="VirtualDelay"/> class.
    /// </summary>
    /// <param name="completionTime">The time at which this delay should complete.</param>
    /// <param name="cancellationToken">Cancellation token for the delay.</param>
    public VirtualDelay (DateTime completionTime, CancellationToken cancellationToken)
    {
        CompletionTime = completionTime;

        if (cancellationToken.CanBeCanceled)
        {
            _cancellationRegistration = cancellationToken.Register (() => { _completionSource.TrySetCanceled (cancellationToken); });
        }
    }

    /// <summary>
    ///     Gets the time at which this delay should complete.
    /// </summary>
    public DateTime CompletionTime { get; }

    /// <summary>
    ///     Gets the task representing this delay.
    /// </summary>
    public Task Task => _completionSource.Task;

    /// <summary>
    ///     Gets a value indicating whether this delay has completed.
    /// </summary>
    public bool IsCompleted => _completionSource.Task.IsCompleted;

    /// <summary>
    ///     Completes the delay.
    /// </summary>
    public void Complete ()
    {
        _completionSource.TrySetResult (true);
        _cancellationRegistration.Dispose ();
    }
}
