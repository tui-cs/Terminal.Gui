using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Terminal.Gui.Tracing;

namespace Terminal.Gui.App;

internal partial class ApplicationImpl
{
    // Lock object to protect session stack operations and cached state updates
    private readonly Lock _sessionStackLock = new ();

    #region Session State - Stack and TopRunnable

    /// <inheritdoc/>
    public ConcurrentStack<SessionToken>? SessionStack { get; } = new ();

    /// <inheritdoc/>
    public IRunnable? TopRunnable { get; private set; }

    /// <inheritdoc/>
    public View? TopRunnableView => TopRunnable as View;

    /// <inheritdoc/>
    public event EventHandler<SessionTokenEventArgs>? SessionBegun;

    /// <inheritdoc/>
    public event EventHandler<SessionTokenEventArgs>? SessionEnded;

    #endregion Session State - Stack and TopRunnable

    #region Main Loop Iteration

    /// <inheritdoc/>
    public bool StopAfterFirstIteration { get; set; }

    /// <inheritdoc/>
    public event EventHandler<EventArgs<IApplication?>>? Iteration;

    /// <inheritdoc/>
    public void RaiseIteration () => Iteration?.Invoke (this, new EventArgs<IApplication?> (this));

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

        // Set the synchronization context to MainLoopSyncContext for this application
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

            // If End succeeded IsRunning is now false — we are done
            if (!runnable.IsRunning)
            {
                break;
            }

            // End was cancelled by an IsRunningChanging handler (e.g., "Are you sure?" veto).
            // Reset StopRequested so RunLoop can re-enter its while condition correctly.
            runnable.StopRequested = false;
        }

        return token.Result;
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

        while (runnable is { StopRequested: false, IsRunning: true })
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

        Trace.Lifecycle (MainThreadId.ToString (), "Run", $"{(runnable as Runnable)?.ToIdentifyingString ()} Stopped");
    }

    #endregion Session Lifecycle - Run

    #region Session Lifecycle - End

    /// <inheritdoc/>
    public void End (SessionToken token)
    {
        ArgumentNullException.ThrowIfNull (token);

        if (token.Runnable is null)
        {
            return; // Already ended
        }

        Trace.Lifecycle (MainThreadId.ToString (), "End", $"{(token.Runnable as Runnable)?.ToIdentifyingString ()}");

        if (Popovers?.GetActivePopover () is { Visible: true } visiblePopover)
        {
            ApplicationPopover.HideWithQuitCommand (visiblePopover);
        }

        IRunnable runnable = token.Runnable;

        // Get old IsRunning value (safe - cached value)
        bool oldIsRunning = runnable.IsRunning;

        // Raise IsRunningChanging OUTSIDE lock (true -> false) - can be canceled
        // This is where Result should be extracted!
        if (runnable.RaiseIsRunningChanging (oldIsRunning, false))
        {
            // Stopping was canceled - do not proceed with End
            return;
        }

        bool wasModal = runnable.IsModal;
        IRunnable? previousRunnable = null;

        // CRITICAL SECTION - Atomic stack + cached state update
        lock (_sessionStackLock)
        {
            // Pop token from SessionStack
            if (wasModal && SessionStack?.TryPop (out SessionToken? popped) == true && popped == token)
            {
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
        }

        // END CRITICAL SECTION - IsRunning/IsModal now thread-safe

        // Fire events AFTER lock released
        if (wasModal)
        {
            runnable.RaiseIsModalChangedEvent (false);
        }

        TopRunnable = null;

        if (previousRunnable != null)
        {
            TopRunnable = previousRunnable;
            previousRunnable.RaiseIsModalChangedEvent (true);
        }

        Mouse.UngrabMouse ();

        runnable.RaiseIsRunningChangedEvent (false);

        token.Result = runnable.Result;

        _result = token.Result;

        Trace.Lifecycle (MainThreadId.ToString (), "End", $"{(token.Runnable as Runnable)?.ToIdentifyingString ()} - Result: {_result ?? Glyphs.Null}");

        // Clear the Runnable from the token
        token.Runnable = null;
        SessionEnded?.Invoke (this, new SessionTokenEventArgs (token));
    }

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
