using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using Terminal.Gui.Tracing;

namespace Terminal.Gui.App;

internal partial class ApplicationImpl
{
    // Lock object to protect session stack operations and cached state updates
    private readonly Lock _sessionStackLock = new ();

    #region Session State - Stack and TopRunnable

    /// <inheritdoc/>
    public ConcurrentStack<SessionToken>? SessionStack { get; } = new ();

    private IRunnable? _topRunnable;

    /// <inheritdoc/>
    public IRunnable? TopRunnable
    {
        get => Volatile.Read (ref _topRunnable);
        private set => Volatile.Write (ref _topRunnable, value);
    }

    /// <inheritdoc/>
    public View? TopRunnableView => TopRunnable as View;

    /// <inheritdoc/>
    public event EventHandler<SessionTokenEventArgs>? SessionBegun;

    /// <inheritdoc/>
    public event EventHandler<SessionTokenEventArgs>? SessionEnded;

    #endregion Session State - Stack and TopRunnable

    #region Main Loop Iteration

    private int _runLoopCount;

    /// <inheritdoc/>
    public bool StopAfterFirstIteration { get; set; }

    /// <inheritdoc/>
    public event EventHandler<EventArgs<IApplication?>>? Iteration;

    /// <inheritdoc/>
    public void RaiseIteration () => Iteration?.Invoke (this, new EventArgs<IApplication?> (this));

    internal bool IsRunLoopRunning => Volatile.Read (ref _runLoopCount) > 0;

    #endregion Main Loop Iteration

    #region Timeouts and Invoke

    /// <inheritdoc/>
    public ITimedEvents TimedEvents { get; }

    /// <inheritdoc/>
    public object AddTimeout (TimeSpan time, Func<bool> callback) => TimedEvents.Add (time, callback);

    /// <inheritdoc/>
    public bool RemoveTimeout (object token) => TimedEvents.Remove (token);

    /// <inheritdoc/>
    public void Invoke (Action<IApplication>? action)
    {
        if (!Initialized)
        {
            throw new NotInitializedException (nameof (Invoke));
        }

        // If we are already on the main UI thread
        if (TopRunnableView is IRunnable { IsRunning: true } && MainThreadId == Thread.CurrentThread.ManagedThreadId)
        {
            action?.Invoke (this);

            return;
        }

        TimedEvents.Add (TimeSpan.Zero,
                         () =>
                         {
                             action?.Invoke (this);

                             return false;
                         });
    }

    /// <inheritdoc/>
    public void Invoke (Action action)
    {
        ArgumentNullException.ThrowIfNull (action);

        if (!Initialized)
        {
            throw new NotInitializedException (nameof (Invoke));
        }

        // If we are already on the main UI thread
        if (TopRunnableView is IRunnable { IsRunning: true } && MainThreadId == Thread.CurrentThread.ManagedThreadId)
        {
            action.Invoke ();

            return;
        }

        TimedEvents.Add (TimeSpan.Zero,
                         () =>
                         {
                             action.Invoke ();

                             return false;
                         });
    }

    #endregion Timeouts and Invoke

    #region Session Lifecycle - Begin

    // The ambient context from before the first running session installed this app's context.
    private SynchronizationContext? _callerSynchronizationContext;

    /// <inheritdoc/>
    public SessionToken? Begin (IRunnable runnable)
    {
        ArgumentNullException.ThrowIfNull (runnable);

        if (runnable.IsRunning)
        {
            throw new ArgumentException (@"The runnable is already running.", nameof (runnable));
        }

        // Create session token
        SessionToken token = new (runnable);

        Trace.Lifecycle (MainThreadId.ToString (), "Begin", "(token.Runnable as Runnable)?.ToIdentifyingString ()");

        // Get old IsRunning value BEFORE any stack changes (safe - cached value)
        bool oldIsRunning = runnable.IsRunning;

        // Raise IsRunningChanging OUTSIDE lock (false -> true) - can be canceled
        if (runnable.RaiseIsRunningChanging (oldIsRunning, true))
        {
            // Starting was canceled
            return null;
        }

        // Set the application reference in the runnable
        runnable.SetApp (this);

        // Make this app's MainLoopSyncContext ambient while sessions run. The first running session saves the
        // caller's context so the last session to end can restore it (#5636).
        if (!HasRunningSession && System.Threading.SynchronizationContext.Current != SynchronizationContext)
        {
            _callerSynchronizationContext = System.Threading.SynchronizationContext.Current;
        }

        SynchronizationContext.SetSynchronizationContext (SynchronizationContext);

        // Ensure the mouse is ungrabbed
        Mouse.UngrabMouse ();

        Navigation?.SetFocused (null);

        IRunnable? previousTop = null;

        // CRITICAL SECTION - Atomic stack + cached state update
        lock (_sessionStackLock)
        {
            // Get the previous top BEFORE pushing new token
            if (SessionStack?.TryPeek (out SessionToken? previousToken) == true && previousToken.Runnable is { })
            {
                previousTop = previousToken.Runnable;
            }

            if (previousTop == runnable)
            {
                throw new ArgumentOutOfRangeException (nameof (runnable), runnable, @"Attempt to Run the runnable that's already the top runnable.");
            }

            // Push token onto SessionStack
            SessionStack?.Push (token);

            TopRunnable = runnable;

            // Update cached state atomically - IsRunning and IsModal are now consistent
            SessionBegun?.Invoke (this, new SessionTokenEventArgs (token));
            runnable.SetIsRunning (true);
            runnable.SetIsModal (true);

            // Previous top is no longer modal
            previousTop?.SetIsModal (false);
        }

        // END CRITICAL SECTION - IsRunning/IsModal now thread-safe

        // Fire events AFTER lock released (avoid deadlocks in event handlers)
        previousTop?.RaiseIsModalChangedEvent (false);

        runnable.RaiseIsRunningChangedEvent (true);
        runnable.RaiseIsModalChangedEvent (true);

        IDriver? driver = Driver;

        if (driver?.AnsiStartupGate is { IsReady: false } startupGate)
        {
            string pending = string.Join (", ", startupGate.PendingQueries);
            Trace.Lifecycle (MainThreadId.ToString (), "Begin", $"Deferring initial LayoutAndDraw until ANSI startup queries complete. Pending: {pending}");
        }
        else
        {
            LayoutAndDraw ();
        }

        return token;
    }

    #endregion Session Lifecycle - Begin

    #region Session Lifecycle - Run

    /// <inheritdoc/>
    public IApplication Run<TRunnable> (Func<Exception, bool>? errorHandler = null, string? driverName = null) where TRunnable : IRunnable, new()
    {
        if (!Initialized)
        {
            // Init() has NOT been called. Auto-initialize as per interface contract.
            Init (driverName);
        }

        if (Driver is null)
        {
            throw new InvalidOperationException (@"Driver is null after Init.");
        }

        TRunnable runnable = new ();
        Run (runnable, errorHandler);

        // We created the runnable, so dispose it if it's disposable
        if (runnable is IDisposable disposable)
        {
            disposable.Dispose ();
        }

        return this;
    }

    /// <inheritdoc/>
    public object? Run (IRunnable runnable, Func<Exception, bool>? errorHandler = null)
    {
        ArgumentNullException.ThrowIfNull (runnable);

        if (!Initialized)
        {
            throw new NotInitializedException (@"Init must be called before Run.");
        }

        // Begin installs this app's MainLoopSyncContext as the thread's ambient context for the
        // duration of the session; restore the caller's context on exit so an await after Run
        // does not capture a context that is no longer pumping (#5636).
        SynchronizationContext? previousContext = System.Threading.SynchronizationContext.Current;

        try
        {
            // Begin the session (adds to stack, raises IsRunningChanging/IsRunningChanged)

            SessionToken? token =

                // Find it on the stack
                runnable.IsRunning ? SessionStack?.FirstOrDefault (st => st.Runnable == runnable) : Begin (runnable);

            if (token is null)
            {
                Logging.Warning (@"Run - Begin session failed or was cancelled.");

                return null;
            }

            // Loop to handle the case where End is cancelled by an IsRunningChanging handler.
            // When End is cancelled, IsRunning remains true; we reset StopRequested and re-run the loop.
            while (true)
            {
                try
                {
                    // All runnables block until RequestStop() is called
                    RunLoop (runnable, errorHandler);
                }
                finally
                {
                    // End the session (raises IsRunningChanging/IsRunningChanged, pops from stack)
                    End (token);
                }

                // Shutdown must also unwind a notification's nested Run instead of retrying a vetoed End.
                if (!runnable.IsRunning || Volatile.Read (ref _dispatchStopping))
                {
                    break;
                }

                // End was cancelled by an IsRunningChanging handler (e.g., "Are you sure?" veto).
                // Reset StopRequested so RunLoop can re-enter its while condition correctly.
                runnable.StopRequested = false;
            }

            return token.Result;
        }
        finally
        {
            // Once the last session has ended, End has restored the caller's context; do not reinstall this app's.
            if (HasRunningSession || previousContext != SynchronizationContext)
            {
                System.Threading.SynchronizationContext.SetSynchronizationContext (previousContext);
            }
        }
    }

    /// <inheritdoc/>
    public Task<object?> RunAsync (IRunnable runnable, CancellationToken cancellationToken, Func<Exception, bool>? errorHandler = null)
    {
        ArgumentNullException.ThrowIfNull (runnable);

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult<object?> (null);
        }

        TaskCompletionSource<object?> tcs = new (TaskCreationOptions.RunContinuationsAsynchronously);

        // Register the cancellation token to request stop on the main loop via Invoke.
        CancellationTokenRegistration registration = cancellationToken.Register (() => Invoke (() => RequestStop (runnable)));

        try
        {
            object? result = Run (runnable, errorHandler);
            tcs.TrySetResult (result);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            tcs.TrySetCanceled (cancellationToken);
        }
        catch (Exception ex)
        {
            tcs.TrySetException (ex);
        }
        finally
        {
            registration.Dispose ();
        }

        return tcs.Task;
    }

    /// <inheritdoc/>
    public Task<IApplication> RunAsync<TRunnable> (CancellationToken cancellationToken, Func<Exception, bool>? errorHandler = null, string? driverName = null) where TRunnable : IRunnable, new()
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult<IApplication> (this);
        }

        if (!Initialized)
        {
            // Init() has NOT been called. Auto-initialize as per interface contract.
            Init (driverName);
        }

        if (Driver is null)
        {
            throw new InvalidOperationException (@"Driver is null after Init.");
        }

        TRunnable runnable = new ();

        TaskCompletionSource<IApplication> tcs = new (TaskCreationOptions.RunContinuationsAsynchronously);

        // Register the cancellation token to request stop on the main loop via Invoke.
        CancellationTokenRegistration registration = cancellationToken.Register (() => Invoke (() => RequestStop (runnable)));

        try
        {
            Run (runnable, errorHandler);
            tcs.TrySetResult (this);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            tcs.TrySetCanceled (cancellationToken);
        }
        catch (Exception ex)
        {
            tcs.TrySetException (ex);
        }
        finally
        {
            registration.Dispose ();

            // We created the runnable, so dispose it if it's disposable
            if (runnable is IDisposable disposable)
            {
                disposable.Dispose ();
            }
        }

        return tcs.Task;
    }

    private void RunLoop (IRunnable runnable, Func<Exception, bool>? errorHandler)
    {
        runnable.StopRequested = false;

        // Main loop - blocks until RequestStop() is called
        // Note: IsRunning is now a cached property, safe to check each iteration
        var firstIteration = true;

        Trace.Lifecycle (MainThreadId.ToString (), "Run", $"{(runnable as Runnable)?.ToIdentifyingString ()}");

        Interlocked.Increment (ref _runLoopCount);

        try
        {
            // Disposal publishes shutdown before waiting for the coordinator's render gate. A synchronous
            // modal entered from a rendering notification must unwind so that gate can be released.
            while (!Volatile.Read(ref _dispatchStopping) && runnable is { StopRequested: false, IsRunning: true })
            {
                if (Coordinator is null)
                {
                    throw new Exception ($"{nameof (IMainLoopCoordinator)} inexplicably became null during Run");
                }

                try
                {
                    // Process one iteration of the event loop
                    Coordinator.RunIteration ();
                }
                catch (Exception ex)
                {
                    if (errorHandler is null || !errorHandler (ex))
                    {
                        throw;
                    }
                }

                if (StopAfterFirstIteration && firstIteration)
                {
                    Trace.Lifecycle (MainThreadId.ToString (),
                                     "Run",
                                     $"{(runnable as Runnable)?.ToIdentifyingString ()} Stopping after first iteration as requested");
                    RequestStop (runnable);
                }

                firstIteration = false;
            }
        }
        finally
        {
            Interlocked.Decrement (ref _runLoopCount);
        }

        Trace.Lifecycle (MainThreadId.ToString (), "Run", $"{(runnable as Runnable)?.ToIdentifyingString ()} Stopped");
    }

    #endregion Session Lifecycle - Run

    #region Session Lifecycle - End

    /// <inheritdoc/>
    public void End (SessionToken token)
    {
        ArgumentNullException.ThrowIfNull (token);

        // Claim the session before the cancellable IsRunningChanging, so a concurrent or reentrant End returns instead
        // of raising it again or tearing down ahead of a veto. Once teardown starts, the claim is never released.
        if (token.Runnable is not { } runnable || !token.TryClaimEnd ())
        {
            return; // Already ended or ending
        }

        Trace.Lifecycle (MainThreadId.ToString (), "End", $"{(runnable as Runnable)?.ToIdentifyingString ()}");

        var stopping = false;

        try
        {
            if (Popovers?.GetActivePopover () is { Visible: true } visiblePopover)
            {
                ApplicationPopover.HideWithQuitCommand (visiblePopover);
            }

            // Get old IsRunning value (safe - cached value)
            bool oldIsRunning = runnable.IsRunning;

            // Raise IsRunningChanging OUTSIDE lock (true -> false) - can be canceled
            // This is where Result should be extracted!
            stopping = !runnable.RaiseIsRunningChanging (oldIsRunning, false);
        }
        finally
        {
            // Stopping was canceled or a handler threw; release the claim so a later End can stop this session.
            if (!stopping)
            {
                token.ReleaseEndClaim ();
            }
        }

        if (!stopping)
        {
            return;
        }

        bool wasModal;
        IRunnable? previousRunnable = null;

        // CRITICAL SECTION - Atomic stack + cached state update
        lock (_sessionStackLock)
        {
            // Close owned dispatch first, so no owned work starts once teardown is visible.
            token.IsDispatchClosed = true;

            // Read under the lock: a concurrent Begin moves IsModal to the session it pushes.
            wasModal = runnable.IsModal;

            // Pop only this token; popping first and comparing after would discard another session.
            bool wasTop = wasModal && SessionStack?.TryPeek (out SessionToken? top) == true && top == token;

            if (wasTop)
            {
                SessionStack!.TryPop (out _);

                // Discard sessions that ended while beneath this one so the nearest running session becomes top.
                while (SessionStack?.TryPeek (out SessionToken? endedToken) == true && endedToken.Runnable is not { IsRunning: true })
                {
                    SessionStack.TryPop (out _);
                }

                // Restore previous top runnable
                if (SessionStack?.TryPeek (out SessionToken? previousToken) == true && previousToken.Runnable is { })
                {
                    previousRunnable = previousToken.Runnable;

                    // Previous runnable becomes modal again
                    previousRunnable.SetIsModal (true);
                }
            }

            // Update cached state atomically - IsRunning and IsModal are now consistent
            runnable.SetIsRunning (false);
            runnable.SetIsModal (false);

            if (wasTop)
            {
                TopRunnable = previousRunnable;
            }
            else
            {
                // A session ended beneath the top is no longer drawn; clear its cells on the next draw.
                ClearScreenNextIteration = true;
            }
        }

        // END CRITICAL SECTION - IsRunning/IsModal now thread-safe

        EndSessionDispatches (token);

        // The held claim now makes End a no-op, so finish teardown and raise every notification even if a handler
        // throws. The first exception is rethrown after SessionEnded.
        ExceptionDispatchInfo? failure = null;

        // Fire events AFTER lock released
        if (wasModal)
        {
            Notify (() => runnable.RaiseIsModalChangedEvent (false));
        }

        if (previousRunnable is { } restoredRunnable)
        {
            Notify (() => restoredRunnable.RaiseIsModalChangedEvent (true));
        }

        Notify (Mouse.UngrabMouse);
        Notify (() => runnable.RaiseIsRunningChangedEvent (false));

        token.Result = runnable.Result;

        _result = token.Result;

        Trace.Lifecycle (MainThreadId.ToString (), "End", $"{(runnable as Runnable)?.ToIdentifyingString ()} - Result: {_result ?? Glyphs.Null}");

        // Keep Runnable available to the state-change handlers, then clear it before SessionEnded.
        token.Runnable = null;

        // Keep this app's context ambient while any session still runs, including one that began first or one a
        // handler above began. After the last running session, restore the caller's context so a later await cannot
        // capture a context that is no longer pumping (#5636).
        if (!HasRunningSession && System.Threading.SynchronizationContext.Current == SynchronizationContext)
        {
            System.Threading.SynchronizationContext.SetSynchronizationContext (_callerSynchronizationContext);
        }

        Notify (() => SessionEnded?.Invoke (this, new SessionTokenEventArgs (token)));

        failure?.Throw ();

        return;

        void Notify (Action raise)
        {
            try
            {
                raise ();
            }
            catch (Exception exception)
            {
                failure ??= ExceptionDispatchInfo.Capture (exception);
            }
        }
    }

    private bool _hasEndedSession;

    /// <summary>
    ///     INTERNAL: Whether any session has ended. Once true (and no session is running), posts to
    ///     <see cref="MainLoopSyncContext"/> fall back to the thread pool instead of queueing onto a
    ///     loop that may never pump again.
    /// </summary>
    internal bool HasEndedSession
    {
        get => Volatile.Read (ref _hasEndedSession);
        private set => Volatile.Write (ref _hasEndedSession, value);
    }

    /// <summary>Whether the session stack contains a running session, including one beneath the top runnable.</summary>
    internal bool HasRunningSession => SessionStack?.Any (session => session.Runnable is { IsRunning: true }) == true;

    /// <summary>
    ///     INTERNAL: Whether work posted to <see cref="MainLoopSyncContext"/> can rely on the main
    ///     loop to pump it: the app is initialized, and either a session is running or none has run
    ///     to completion yet (posts made between Init and the first Run are pumped by that Run).
    /// </summary>
    internal bool CanPumpPostedWork => Initialized
                                       && !Volatile.Read (ref _dispatchStopping)
                                       && (!HasEndedSession || HasRunningSession);

    /// <summary>
    ///     INTERNAL: Whether queued UI work may start on the calling thread now: it is the UI thread or is pumping a
    ///     Begin-only session, dispatch is not stopping, and a session is running. Check it under <c>_dispatchLock</c>
    ///     when claiming queued work.
    /// </summary>
    internal bool CanStartUiWork => (MainThreadId == Thread.CurrentThread.ManagedThreadId || _isPumpingPostedWork.Value)
                                    && !Volatile.Read (ref _dispatchStopping)
                                    && HasRunningSession;

    internal void ResetHasEndedSession () => HasEndedSession = false;

    #endregion Session Lifecycle - End

    #region Session Lifecycle - RequestStop

    /// <inheritdoc/>
    public void RequestStop () => RequestStop (null);

    /// <inheritdoc/>
    public void RequestStop (IRunnable? runnable)
    {
        // Get the runnable to stop
        if (runnable is null)
        {
            // Try to get from TopRunnable
            if (TopRunnableView is IRunnable r)
            {
                runnable = r;
            }
            else
            {
                return;
            }
        }

        Trace.Lifecycle (MainThreadId.ToString (), "Run", $"{(runnable as Runnable)?.ToIdentifyingString ()}");

        runnable.StopRequested = true;

        // Note: The End() method will be called from the finally block in Run()
        // and that's where IsRunningChanging/IsRunningChanged will be raised
    }

    #endregion Session Lifecycle - RequestStop
}
