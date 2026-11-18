using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Base class for reading console input in perpetual loop.
///     The <see cref="Peek"/> and <see cref="Read"/> methods are executed
///     on the input thread created by <see cref="MainLoopCoordinator{TInputRecord}.StartInputTaskAsync"/>.
/// </summary>
/// <typeparam name="TInputRecord"></typeparam>
public abstract class InputImpl<TInputRecord> : IInput<TInputRecord>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="InputImpl{TInputRecord}"/> class and detects if we are attached to a
    ///     real terminal device. If not, the input implementation will run in a degraded mode where all operations are no-op.
    /// </summary>
    protected InputImpl () => IsAttachedToTerminal = Driver.IsAttachedToTerminal (out _, out _);

    /// <summary>
    ///     Gets whether this input instance is attached to a real terminal device.
    /// </summary>
    protected bool IsAttachedToTerminal { get; }

    private ConcurrentQueue<TInputRecord>? _inputQueue;

    /// <summary>
    ///     Determines how to get the current system type, adjust
    ///     in unit tests to simulate specific timings.
    /// </summary>
    public Func<DateTime> Now { get; set; } = () => DateTime.Now;

    /// <inheritdoc/>
    public CancellationTokenSource? ExternalCancellationTokenSource { get; set; }

    /// <inheritdoc/>
    public void Initialize (ConcurrentQueue<TInputRecord> inputQueue) => _inputQueue = inputQueue;

    /// <inheritdoc/>
    public void Run (CancellationToken runCancellationToken)
    {
        // Create a linked token source if we have an external one
        CancellationTokenSource? linkedCts = null;
        CancellationToken effectiveToken = runCancellationToken;

        if (ExternalCancellationTokenSource != null)
        {
            linkedCts = CancellationTokenSource.CreateLinkedTokenSource (runCancellationToken, ExternalCancellationTokenSource.Token);
            effectiveToken = linkedCts.Token;
        }

        try
        {
            if (_inputQueue == null)
            {
                throw new Exception ("Cannot run input before Initialization");
            }

            do
            {
                while (Peek ())
                {
                    //Logging.Trace($"Read...");
                    IEnumerable<TInputRecord> records = Read ();

                    foreach (TInputRecord r in records)
                    {
                        _inputQueue.Enqueue (r);
                    }
                }

                effectiveToken.ThrowIfCancellationRequested ();

                // Throttle the input loop to avoid CPU spinning when no input is available
                // This is especially important when multiple ApplicationImpl instances are created
                // in parallel tests without calling Shutdown() - prevents thread pool exhaustion
                Task.Delay (20, effectiveToken).Wait (effectiveToken);
            }
            while (!effectiveToken.IsCancellationRequested);
        }
        catch (OperationCanceledException)
        { }
        finally
        {
            //Logging.Trace ("Stopping input processing");
            linkedCts?.Dispose ();
        }
    }

    /// <summary>
    ///     When implemented in a derived class, returns true if there is data available
    ///     to read from console.
    /// </summary>
    /// <returns></returns>
    public abstract bool Peek ();

    /// <summary>
    ///     Returns the available data without blocking, called when <see cref="Peek"/>
    ///     returns <see langword="true"/>.
    /// </summary>
    /// <returns></returns>
    public abstract IEnumerable<TInputRecord> Read ();

    /// <inheritdoc/>
    public virtual void Dispose () { }
}
