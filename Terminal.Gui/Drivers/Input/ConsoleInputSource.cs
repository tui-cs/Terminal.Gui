using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Console input source - reads from actual console (production).
///     Abstract base class for platform-specific implementations.
/// </summary>
public abstract class ConsoleInputSource : IInputSource
{
    private Task? _readTask;
    private CancellationTokenSource? _cancellationTokenSource;

    /// <summary>
    ///     Input buffer populated by the background read thread.
    /// </summary>
    protected readonly ConcurrentQueue<InputEventRecord> _inputBuffer = new ();

    /// <summary>
    ///     Initializes a new instance of the <see cref="ConsoleInputSource"/> class.
    /// </summary>
    /// <param name="timeProvider">The time provider for timestamps.</param>
    protected ConsoleInputSource (ITimeProvider timeProvider) { TimeProvider = timeProvider; }

    /// <inheritdoc/>
    public ITimeProvider TimeProvider { get; }

    /// <inheritdoc/>
    public bool IsAvailable => !_inputBuffer.IsEmpty;

    /// <inheritdoc/>
    public IEnumerable<InputEventRecord> ReadAvailable ()
    {
        while (_inputBuffer.TryDequeue (out InputEventRecord? record))
        {
            yield return record;
        }
    }

    /// <inheritdoc/>
    public void Start (CancellationToken cancellationToken)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken);
        _readTask = Task.Run (() => ReadLoop (_cancellationTokenSource.Token), cancellationToken);
    }

    /// <inheritdoc/>
    public void Stop ()
    {
        _cancellationTokenSource?.Cancel ();

        try
        {
            _readTask?.Wait (TimeSpan.FromSeconds (1));
        }
        catch (AggregateException)
        {
            // Expected when cancellation happens
        }
    }

    /// <summary>
    ///     Platform-specific implementation of reading input.
    ///     Runs on background thread, enqueues to InputBuffer.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop reading.</param>
    /// <returns>A task representing the read loop.</returns>
    protected abstract Task ReadLoop (CancellationToken cancellationToken);
}
