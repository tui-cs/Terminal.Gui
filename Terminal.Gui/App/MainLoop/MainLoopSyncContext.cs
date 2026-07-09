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

    /// <inheritdoc/>
    public override void Post (SendOrPostCallback d, object? state)
    {
        ArgumentNullException.ThrowIfNull (d);

        // Queue the task using the modern architecture
        _app.Invoke (() => d (state));
    }

    /// <inheritdoc/>
    public override void Send (SendOrPostCallback d, object? state)
    {
        ArgumentNullException.ThrowIfNull (d);

        if (_app.MainThreadId == Thread.CurrentThread.ManagedThreadId)
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
