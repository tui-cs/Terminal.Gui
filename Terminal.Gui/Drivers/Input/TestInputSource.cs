namespace Terminal.Gui.Drivers;

/// <summary>
///     Test input source - provides pre-programmed input for testing.
/// </summary>
public class TestInputSource : IInputSource
{
    private readonly Queue<InputEventRecord> _inputQueue = new ();

    /// <summary>
    ///     Initializes a new instance of the <see cref="TestInputSource"/> class.
    /// </summary>
    /// <param name="timeProvider">The time provider for timestamps.</param>
    public TestInputSource (ITimeProvider timeProvider) { TimeProvider = timeProvider; }

    /// <inheritdoc/>
    public ITimeProvider TimeProvider { get; }

    /// <inheritdoc/>
    public bool IsAvailable => _inputQueue.Count > 0;

    /// <inheritdoc/>
    public IEnumerable<InputEventRecord> ReadAvailable ()
    {
        while (_inputQueue.Count > 0)
        {
            yield return _inputQueue.Dequeue ();
        }
    }

    /// <summary>
    ///     Adds input to the queue. Called by InputInjector.
    /// </summary>
    /// <param name="record">The input record to enqueue.</param>
    public void Enqueue (InputEventRecord record)
    {
        // Set timestamp if not already set
        if (record.Timestamp == default (DateTime))
        {
            record = record with { Timestamp = TimeProvider.Now };
        }

        _inputQueue.Enqueue (record);
    }

    /// <inheritdoc/>
    public void Start (CancellationToken cancellationToken)
    {
        // No-op for test implementation
    }

    /// <inheritdoc/>
    public void Stop ()
    {
        // No-op for test implementation
    }
}
