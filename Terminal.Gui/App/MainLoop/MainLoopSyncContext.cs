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

        // ApplicationImpl runs the callback on the main loop, or on the thread pool if the loop is not pumping
        // or stops before running it, so an awaiter is never stranded (#5636). Posts made between Init and the
        // first Run stay queued for that Run.
        if (_app is ApplicationImpl app)
        {
            app.PostToMainLoop (d, state);

            return;
        }

        ApplicationImpl.RunOnThreadPool (d, state);
    }

    /// <inheritdoc/>
    /// <remarks>
    ///     A call from outside the main-loop thread blocks until the main loop executes the callback, or until the
    ///     thread pool executes it if the loop stops first. As with other synchronous UI dispatch APIs, this can
    ///     deadlock if the main-loop thread is waiting for the calling thread.
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

        Post (_ =>
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
        }, null);

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
