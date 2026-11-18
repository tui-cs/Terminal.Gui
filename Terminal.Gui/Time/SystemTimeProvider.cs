namespace Terminal.Gui.Time;

/// <summary>
///     Real time provider using DateTime.Now and Task.Delay for production use.
/// </summary>
public class SystemTimeProvider : ITimeProvider
{
    /// <inheritdoc/>
    public DateTime Now => DateTime.Now;

    /// <inheritdoc/>
    public Task Delay (TimeSpan duration, CancellationToken cancellationToken = default) { return Task.Delay (duration, cancellationToken); }

    /// <inheritdoc/>
    public ITimer CreateTimer (TimeSpan interval, Action callback) { return new SystemTimer (interval, callback); }
}

/// <summary>
///     System timer implementation using System.Threading.Timer.
/// </summary>
internal class SystemTimer : ITimer
{
    private readonly Action _callback;
    private readonly TimeSpan _interval;
    private Timer? _timer;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SystemTimer"/> class.
    /// </summary>
    /// <param name="interval">The interval between timer callbacks.</param>
    /// <param name="callback">The action to invoke when the timer fires.</param>
    public SystemTimer (TimeSpan interval, Action callback)
    {
        _interval = interval;
        _callback = callback;
    }

    /// <inheritdoc/>
    public bool IsRunning { get; private set; }

    /// <inheritdoc/>
    public void Start ()
    {
        if (IsRunning)
        {
            return;
        }

        _timer = new (_ => _callback (), null, _interval, _interval);
        IsRunning = true;
    }

    /// <inheritdoc/>
    public void Stop ()
    {
        if (!IsRunning)
        {
            return;
        }

        _timer?.Dispose ();
        _timer = null;
        IsRunning = false;
    }

    /// <inheritdoc/>
    public void Dispose () { Stop (); }
}
