using System.Collections.Concurrent;

namespace Terminal.Gui.App;

internal partial class ApplicationImpl
{
    private bool _threadAccessChecksEnabled;

    /// <inheritdoc/>
    public bool ThreadAccessChecksEnabled
    {
        get => Volatile.Read (ref _threadAccessChecksEnabled);
        set => Volatile.Write (ref _threadAccessChecksEnabled, value);
    }

    private readonly ConcurrentQueue<Action> _lifecycleUiWork = new ();

    // End may be called from a worker. Keep its synchronized session bookkeeping and notifications synchronous,
    // but let the UI thread perform focus/popover/invalidation work. These actions are not session-owned dispatch:
    // ending the last session must not discard its cleanup. User lifecycle notifications never run in an exemption.
    internal static void RunLifecycleUiWork (IApplication? app, Action action)
    {
        if (app is ApplicationImpl { ThreadAccessChecksEnabled: true, Initialized: true } implementation && !app.CheckAccess ())
        {
            lock (implementation._dispatchLock)
            {
                // Reset publishes the unavailable thread under this same lock, so cleanup cannot be enqueued
                // after its final drain has completed.
                if (implementation.Initialized && !app.CheckAccess ())
                {
                    implementation._lifecycleUiWork.Enqueue (action);

                    return;
                }
            }
        }

        action ();
    }

    private void DrainLifecycleUiWork ()
    {
        if (Initialized && !this.CheckAccess ())
        {
            return;
        }

        int count = _lifecycleUiWork.Count;

        for (var index = 0; index < count && _lifecycleUiWork.TryDequeue (out Action? action); index++)
        {
            try
            {
                action ();
            }
            catch (Exception error)
            {
                // The worker's End has already returned. Report asynchronous cleanup errors without preventing
                // an unrelated Begin/End, or Run's finally, from completing its own session transition.
                Logging.Error ($"Deferred lifecycle UI cleanup failed: {error}");
            }
        }
    }
}
