namespace Terminal.Gui.App;

internal partial class ApplicationImpl
{
    // Teardown publishes first (owner closed, last session stopped, or dispatch stopping), then releases queued work
    // under _dispatchLock. Queued work is claimed under the same lock and only while CanStartUiWork holds and its owner,
    // if any, is running. A claim between the publish and the release either read the state from before the publish,
    // so it is ordered before teardown, or sees the stop and leaves the item for the release. So each item either
    // starts on the UI thread or is released by teardown, never both and never neither. Work that cannot start yet
    // stays queued. End publishes under _sessionStackLock alone, so IRunnable state changes never block dispatch.
    private readonly Lock _dispatchLock = new ();
    private readonly LinkedList<UiDispatchOperation> _queuedDispatches = new ();
    private readonly LinkedList<PostedCallback> _postedCallbacks = new ();
    private bool _dispatchStopping;

    Task IApplicationAsyncDispatcher.InvokeAsync (Action<IApplication> action, SessionToken? owner, CancellationToken cancellationToken) =>
        InvokeAsyncCore (action, owner, cancellationToken);

    internal Task InvokeAsyncCore (Action<IApplication> action, SessionToken? owner, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull (action);

        if (!Initialized)
        {
            throw new NotInitializedException (nameof (ApplicationDispatchExtensions.InvokeAsync));
        }

        UiDispatchOperation operation;

        lock (_dispatchLock)
        {
            if (!Initialized || _dispatchStopping)
            {
                throw new NotInitializedException (nameof (ApplicationDispatchExtensions.InvokeAsync));
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled (cancellationToken);
            }

            if (owner is { })
            {
                if (owner.IsDispatchClosed || owner.Runnable is null || SessionStack?.Contains (owner) != true)
                {
                    return Task.FromCanceled (new CancellationToken (true));
                }
            }
            else if (HasEndedSession && !HasRunningSession)
            {
                return Task.FromCanceled (new CancellationToken (true));
            }

            operation = new (this, action, owner, cancellationToken);
            operation.QueueNode = _queuedDispatches.AddLast (operation);
        }

        operation.RegisterCancellation ();

        if (!operation.IsPending)
        {
            return operation.Task;
        }

        if (CanStartUiWork)
        {
            operation.Execute ();
        }

        return operation.Task;
    }

    internal int QueuedDispatchCount
    {
        get
        {
            lock (_dispatchLock)
            {
                return _queuedDispatches.Count;
            }
        }
    }

    internal int QueuedPostCount
    {
        get
        {
            lock (_dispatchLock)
            {
                return _postedCallbacks.Count;
            }
        }
    }

    internal void DrainDispatches ()
    {
        DrainLifecycleUiWork ();

        if (!CanStartUiWork)
        {
            return;
        }

        int postedCount;
        UiDispatchOperation [] dispatches;

        lock (_dispatchLock)
        {
            // Leave queued operations linked until each starts so End can cancel them during a reentrant callback.
            postedCount = _postedCallbacks.Count;
            dispatches = _queuedDispatches.ToArray ();
        }

        // Run only the posts present at the start of this pass, so a callback that posts again cannot starve the loop.
        for (var i = 0; i < postedCount && TryTakePostedCallback (out PostedCallback posted); i++)
        {
            posted.Callback (posted.State);
        }

        foreach (UiDispatchOperation operation in dispatches)
        {
            operation.Execute ();
        }
    }

    internal void CompleteDispatch (UiDispatchOperation operation)
    {
        lock (_dispatchLock)
        {
            if (operation.QueueNode is not { } node)
            {
                return;
            }

            _queuedDispatches.Remove (node);
            operation.QueueNode = null;
        }
    }

    internal bool TryStartDispatch (UiDispatchOperation operation)
    {
        lock (_dispatchLock)
        {
            // An owner still inside Begin (for example, in SessionBegun) is not active yet; its work waits for a later drain.
            if (!CanStartUiWork || operation.Owner is { IsDispatchActive: false } || !operation.TryStart ())
            {
                return false;
            }

            if (operation.QueueNode is { } node)
            {
                _queuedDispatches.Remove (node);
                operation.QueueNode = null;
            }

            return true;
        }
    }

    private void EndSessionDispatches (SessionToken owner)
    {
        UiDispatchOperation [] pending;
        InlineOutputOperation [] inlinePending;
        PostedCallback [] posted = [];

        lock (_dispatchLock)
        {
            // End closed the owner before it stopped running; Runnable stays available to the lifecycle handlers.
            // Non-top ended tokens remain in SessionStack until the sessions above them end.
            HasEndedSession = true;

            if (HasRunningSession)
            {
                pending = _queuedDispatches.Where (operation => ReferenceEquals (operation.Owner, owner)).ToArray ();
                inlinePending = _inlineOutputQueue.Where (operation => ReferenceEquals (operation.Owner, owner)).ToArray ();
            }
            else
            {
                pending = [.. _queuedDispatches];
                inlinePending = [.. _inlineOutputQueue];
                posted = TakePostedCallbacks ();
            }
        }

        // A new session may enqueue after the lock is released; only cancel the captured requests.
        foreach (InlineOutputOperation operation in inlinePending)
        {
            operation.Cancel ();
        }
        CancelAndRelease (pending, posted);
    }

    private void StopDispatching ()
    {
        UiDispatchOperation [] pending;
        PostedCallback [] posted;

        lock (_dispatchLock)
        {
            Volatile.Write (ref _dispatchStopping, true);
            pending = [.. _queuedDispatches];
            posted = TakePostedCallbacks ();
        }

        EndInlineOutput ();
        CancelAndRelease (pending, posted);
    }

    private static void CancelAndRelease (UiDispatchOperation [] pending, PostedCallback [] posted)
    {
        foreach (UiDispatchOperation operation in pending)
        {
            operation.Cancel ();
        }

        foreach (PostedCallback callback in posted)
        {
            RunOnThreadPool (callback.Callback, callback.State);
        }
    }

    /// <summary>
    ///     INTERNAL: Runs <see cref="MainLoopSyncContext"/> work on the UI thread. Work posted while the loop can pump
    ///     waits for <see cref="DrainDispatches"/>; if the loop stops first, it moves to the thread pool so awaiters are
    ///     not stranded (#5636).
    /// </summary>
    internal void PostToMainLoop (SendOrPostCallback callback, object? state)
    {
        bool runNow;

        lock (_dispatchLock)
        {
            runNow = CanStartUiWork && !_settlingInlineOutput;

            if (!runNow && CanPumpPostedWork)
            {
                _postedCallbacks.AddLast (new PostedCallback (callback, state));

                return;
            }
        }

        // Like Invoke: run inline on the running UI thread.
        if (runNow)
        {
            callback (state);

            return;
        }

        RunOnThreadPool (callback, state);
    }

    private bool TryTakePostedCallback (out PostedCallback posted)
    {
        lock (_dispatchLock)
        {
            // Once teardown is visible, leave the post for teardown to release to the thread pool.
            if (!CanStartUiWork || _postedCallbacks.First is not { } first)
            {
                posted = default;

                return false;
            }

            _postedCallbacks.RemoveFirst ();
            posted = first.Value;

            return true;
        }
    }

    // Caller must hold _dispatchLock.
    private PostedCallback [] TakePostedCallbacks ()
    {
        PostedCallback [] posted = [.. _postedCallbacks];
        _postedCallbacks.Clear ();

        return posted;
    }

    internal static void RunOnThreadPool (SendOrPostCallback callback, object? state) =>
        ThreadPool.QueueUserWorkItem (static posted => posted.Callback (posted.State), new PostedCallback (callback, state), false);

    private readonly record struct PostedCallback (SendOrPostCallback Callback, object? State);
}
