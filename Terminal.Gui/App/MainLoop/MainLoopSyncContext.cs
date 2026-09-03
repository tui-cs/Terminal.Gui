namespace Terminal.Gui.App;

/// <summary>
///     Provides the sync context set while executing code in Terminal.Gui, to let
///     users use async/await on their code
/// </summary>
internal sealed class MainLoopSyncContext : SynchronizationContext
{
    private readonly IApplication _app;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MainLoopSyncContext"/> class.
    /// </summary>
    /// <param name="app">The application instance that owns the main loop.</param>
    public MainLoopSyncContext (IApplication app) => _app = app;

    /// <inheritdoc/>
    public override SynchronizationContext CreateCopy () => new MainLoopSyncContext (_app);

    private bool CanPump => _app is ApplicationImpl { CanPumpPostedWork: true };

    /// <inheritdoc/>
    public override void Post (SendOrPostCallback d, object? state)
    {
        ArgumentNullException.ThrowIfNull (d);

        // With no main loop pumping (after Shutdown/Dispose, or after a session ended with none
        // running), run the callback on the thread pool instead of stranding it — and any awaiter —
        // forever (#5636). Posts made between Init and the first Run stay queued for that Run.
        if (!CanPump)
        {
            ThreadPool.QueueUserWorkItem (
                                          static s =>
                                          {
                                              (SendOrPostCallback callback, object? callbackState) = ((SendOrPostCallback, object?))s!;
                                              callback (callbackState);
                                          },
                                          (d, state));

            return;
        }

        // Queue the task using the modern architecture
        _app.Invoke (() => d (state));
    }

    /// <inheritdoc/>
    /// <remarks>
    ///     A call from outside the main-loop thread blocks until the main loop executes the callback. As with other
    ///     synchronous UI dispatch APIs, this can deadlock if the main-loop thread is waiting for the calling thread.
    /// </remarks>
    public override void Send (SendOrPostCallback d, object? state)
    {
        ArgumentNullException.ThrowIfNull (d);

        // With no main loop pumping, execute inline rather than waiting on a queue nothing drains.
        if (!CanPump || _app.MainThreadId == Thread.CurrentThread.ManagedThreadId)
        {
            d (state);

            return;
        }

        object gate = new ();
        bool wasExecuted = false;
        Exception? error = null;

        _app.Invoke (() =>
        {
            try
            {
                d (state);
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                lock (gate)
                {
                    wasExecuted = true;
                    Monitor.Pulse (gate);
                }
            }
        });

        lock (gate)
        {
            while (!wasExecuted)
            {
                Monitor.Wait (gate);
            }
        }

        if (error is { })
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture (error).Throw ();
        }
    }
}
