using System.Diagnostics;
using System.Runtime.ExceptionServices;
using Terminal.Gui.Configuration;
using Wcwidth;
using Trace = Terminal.Gui.Tracing.Trace;

namespace Terminal.Gui.App;

internal partial class ApplicationImpl
{
    /// <inheritdoc/>
    public int? MainThreadId { get; set; }

    /// <inheritdoc/>
    public bool Initialized { get; set; }

    internal SynchronizationContext? SynchronizationContext { get; private set; }

    /// <inheritdoc/>
    public event EventHandler<EventArgs<bool>>? InitializedChanged;

    /// <inheritdoc/>
    public IApplication Init (string? driverName = null)
    {
        if (Initialized)
        {
            Logging.Error ("Init called multiple times without shutdown, aborting.");

            throw new InvalidOperationException ("Init called multiple times without Shutdown");
        }

        MainThreadId = Thread.CurrentThread.ManagedThreadId;

        Trace.Lifecycle (MainThreadId?.ToString (), "Init", $"driverName: {driverName}");

        // In Application.Init(), before starting the input thread:
        // Pre-warm the Wcwidth static cache to prevent ZeroTable._lookup race condition
        // See: https://github.com/spectreconsole/wcwidth/issues/11
        _ = UnicodeCalculator.GetWidth (new Rune ('A'));

        // Thread-safe fence check: Ensure we're not mixing application models
        // Use lock to make check-and-set atomic
        lock (_modelUsageLock)
        {
            // If this is a legacy static instance and instance-based model was used, throw
            if (this == _instance && ModelUsage == ApplicationModelUsage.InstanceBased)
            {
                throw new InvalidOperationException (ERROR_LEGACY_AFTER_MODERN);
            }

            // If this is an instance-based instance and legacy static model was used, throw
            if (this != _instance && ModelUsage == ApplicationModelUsage.LegacyStatic)
            {
                throw new InvalidOperationException (ERROR_MODERN_AFTER_LEGACY);
            }

            // If no model has been set yet, set it now based on which instance this is
            if (ModelUsage == ApplicationModelUsage.None)
            {
                ModelUsage = this == _instance ? ApplicationModelUsage.LegacyStatic : ApplicationModelUsage.InstanceBased;
            }
        }

        if (!string.IsNullOrWhiteSpace (driverName))
        {
            _driverName = driverName;
        }

        if (string.IsNullOrWhiteSpace (_driverName))
        {
            _driverName = ForceDriver;
        }

        // Reset keyboard to ensure fresh state with default bindings
        _keyboard = new ApplicationKeyboard { App = this };

        CreateDriver (_driverName);

        lock (_dispatchLock)
        {
            _inlineOutputGeneration++;
            _inlineOutputUnits = 0;
            _inlineOutputOutstanding = 0;
            _inlineOutputFailure = null;
            _inlineScreenSized = false;
            _inlineResize = null;
            _inlineScreenHasValidOwnership = false;
            _inlineOriginEstablished = false;
            _screen = null;
            Volatile.Write (ref _dispatchStopping, false);

            // Created here; installed as the thread's ambient context only while Run is executing
            // (see Run). Begin and End leave the caller's context alone: a Begin-only session has no loop
            // to drain continuations, so installing it there strands them (#5680). Installing at Init time
            // would also strand an await between Init and Run (#5636).
            SynchronizationContext = new MainLoopSyncContext (this);
            Initialized = true;
        }

        RaiseInitializedChanged (this, new EventArgs<bool> (true));
        SubscribeDriverEvents ();

        _result = null;

        // Raise static event for modern instance-based model
        Application.RaiseInstanceInitialized (this);

        return this;
    }

    #region IDisposable Implementation

    private bool _disposed;

    /// <summary>
    ///     Disposes the application instance and releases all resources.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method implements the <see cref="IDisposable"/> pattern and performs the same cleanup
    ///         as <see cref="IDisposable.Dispose"/>, but without returning a result.
    ///     </para>
    ///     <para>
    ///         After calling <see cref="Dispose()"/>, use <see cref="GetResult"/> or <see cref="IApplication.GetResult{T}"/>
    ///         to retrieve the result from the last run session.
    ///     </para>
    /// </remarks>
    public void Dispose ()
    {
        Dispose (true);
        GC.SuppressFinalize (this);
    }

    /// <summary>
    ///     Disposes the application instance and releases all resources.
    /// </summary>
    /// <param name="disposing">
    ///     <see langword="true"/> if called from <see cref="Dispose()"/>;
    ///     <see langword="false"/> if called from finalizer.
    /// </param>
    protected virtual void Dispose (bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            if (!disposing)
            {
                return;
            }

            DisposeCore ();
        }
        finally
        {
            // The legacy singleton supports Init/Dispose/Init. Modern instances stay disposed even when a
            // notification failed: DisposeCore has already completed teardown before rethrowing that failure.
            _disposed = this != _instance;
        }
    }

    /// <summary>
    ///     Core disposal logic - same as Shutdown() but without returning result.
    /// </summary>
    private void DisposeCore ()
    {
        ExceptionDispatchInfo? failure = null;
        bool wasInitialized = Initialized;

        // Publish shutdown before waiting for the render gate so nested modal loops can unwind.
        Cleanup (StopDispatching);
        Cleanup (() => Coordinator?.Stop ());
        Cleanup (() => Application.RaiseInstanceDisposed (this));

#if DEBUG
        if (wasInitialized)
        {
            Cleanup (() => AssertNoEventSubscribers (nameof (Iteration), Iteration));
            Cleanup (() => AssertNoEventSubscribers (nameof (SessionBegun), SessionBegun));
            Cleanup (() => AssertNoEventSubscribers (nameof (SessionEnded), SessionEnded));
            Cleanup (() => AssertNoEventSubscribers (nameof (ScreenChanged), ScreenChanged));
        }
#endif

        Cleanup (() => ResetState ());
        Cleanup (TuiJsonErrors.Print);

        if (wasInitialized)
        {
            bool init = Initialized;
            Cleanup (() => RaiseInitializedChanged (this, new EventArgs<bool> (in init)));
        }

        InitializedChanged = null;
        failure?.Throw ();

        return;

        void Cleanup (Action action)
        {
            try
            {
                action ();
            }
            catch (Exception error)
            {
                CaptureCleanupFailure (ref failure, error);
            }
        }
    }

    #endregion IDisposable Implementation

    /// <summary>Shutdown an application initialized with <see cref="Init"/>.</summary>
    [Obsolete ("Use Dispose() or a using statement instead. This method will be removed in a future version.")]
    public object? Shutdown ()
    {
        // Shutdown is now just a wrapper around Dispose that returns the result
        object? result = GetResult ();
        Dispose ();

        return result;
    }

    private object? _result;

    /// <inheritdoc/>
    public object? GetResult () => _result;

    /// <inheritdoc/>
    public void ResetState (bool ignoreDisposed = false)
    {
        Trace.Lifecycle (MainThreadId?.ToString (), "Shutdown");
        ExceptionDispatchInfo? failure = null;
        Cleanup (StopDispatching);
        Cleanup (TimedEvents.StopAll);

        // Raise every session's notifications with affinity checks still active. A failed notification must not
        // leave another session running or prevent restoration of terminal modes by the driver.
        foreach (SessionToken token in SessionStack!.Reverse ())
        {
            if (token.Runnable is null)
            {
                continue;
            }

            Cleanup (() => End (token));

            // Disposal cannot honor a stop veto or leave a session active after a pre-notification throws.
            if (token.Runnable is not { } runnable)
            {
                continue;
            }

            // An End already in progress (including Dispose called by its pre-notification) owns the final
            // notifications. Force only an unclaimed session after a veto or failed pre-notification.
            if (!token.TryClaimEnd ())
            {
                // Reset still releases its cached top reference; the in-flight End owns runnable state and events.
                Interlocked.CompareExchange (ref _topRunnable, null, runnable);

                continue;
            }

            bool wasModal = runnable.IsModal;

            lock (_sessionStackLock)
            {
                token.IsDispatchClosed = true;
                Cleanup (() => runnable.SetIsRunning (false));
                Cleanup (() => runnable.SetIsModal (false));
                if (ReferenceEquals (TopRunnable, runnable))
                {
                    TopRunnable = null;
                }
            }

            if (wasModal)
            {
                Cleanup (() => runnable.RaiseIsModalChangedEvent (false));
            }

            Cleanup (() => runnable.RaiseIsRunningChangedEvent (false));
            Cleanup (() => token.Result = runnable.Result);
            token.Runnable = null;
            Cleanup (() => SessionEnded?.Invoke (this, new SessionTokenEventArgs (token)));
        }

        // No loop remains to marshal resource cleanup after worker disposal. Publish the end of association
        // under the enqueue lock, then drain; user session notifications above receive no exemption.
        lock (_dispatchLock)
        {
            Initialized = false;
            MainThreadId = null;
        }
        Cleanup (DrainLifecycleUiWork);

        Cleanup (() => Popovers?.Dispose ());
        Popovers = null;
        Cleanup (() => ToolTips?.Dispose ());
        ToolTips = null;
        SessionStack?.Clear ();

#if DEBUG_IDISPOSABLE
        if (View.EnableDebugIDisposableAsserts && !ignoreDisposed && TopRunnableView is { } topRunnableView)
        {
            Cleanup (() => Debug.Assert (topRunnableView.WasDisposed, topRunnableView.ToDebugString ()));
        }
#endif

        TopRunnable = null;

        // Capture-and-null before cleanup so a competing Dispose does not dispose the same driver twice.
        IDriver? driver = Driver;
        Driver = null;

        if (driver is { })
        {
            driver.SizeChanged -= Driver_SizeChanged;
            driver.KeyDown -= Driver_KeyDown;
            driver.KeyUp -= Driver_KeyUp;
            driver.MouseEvent -= Driver_MouseEvent;
            Cleanup (driver.Dispose);
        }

        Iteration = null;
        SessionBegun = null;
        SessionEnded = null;
        StopAfterFirstIteration = false;
        ClearScreenNextIteration = false;

        if (_keyboard is IDisposable keyboardDisposable)
        {
            Cleanup (keyboardDisposable.Dispose);
        }
        _keyboard = null;

        if (_mouse is IDisposable mouseDisposable)
        {
            Cleanup (mouseDisposable.Dispose);
        }
        _mouse = null;

        ScreenChanged = null;
        ResetHasEndedSession ();
        Cleanup (UnsubscribeApplicationEvents);
        failure?.Throw ();

        return;

        void Cleanup (Action action)
        {
            try
            {
                action ();
            }
            catch (Exception error)
            {
                CaptureCleanupFailure (ref failure, error);
            }
        }
    }

    private static void CaptureCleanupFailure (ref ExceptionDispatchInfo? failure, Exception error)
    {
        if (failure is null)
        {
            failure = ExceptionDispatchInfo.Capture (error);

            return;
        }

        Logging.Error ($"Additional failure during application cleanup: {error}");
    }

    /// <summary>
    ///     Raises the <see cref="InitializedChanged"/> event.
    /// </summary>
    internal void RaiseInitializedChanged (object sender, EventArgs<bool> e) => InitializedChanged?.Invoke (sender, e);

#if DEBUG
    /// <summary>
    ///     DEBUG ONLY: Asserts that an event has no remaining subscribers.
    /// </summary>
    /// <param name="eventName">The name of the event for diagnostic purposes.</param>
    /// <param name="eventDelegate">The event delegate to check.</param>
    private static void AssertNoEventSubscribers (string eventName, Delegate? eventDelegate)
    {
        if (eventDelegate is null)
        {
            return;
        }

        Delegate [] subscribers = eventDelegate.GetInvocationList ();

        if (subscribers.Length <= 0)
        {
            return;
        }
        string subscriberInfo = string.Join (", ", subscribers.Select (d => $"{d.Method.DeclaringType?.Name}.{d.Method.Name}"));

        Debug.Fail ($"Application.{eventName} has {subscribers.Length} remaining subscriber(s) after Shutdown: {subscriberInfo}");
    }
#endif

    private void OnForceDriverChanged (object? sender, ValueChangedEventArgs<string> e) => ForceDriver = e.NewValue;

    private void OnAppModelChanged (object? sender, ValueChangedEventArgs<AppModel> e) => AppModel = e.NewValue;

    private void OnForceInlinePositionChanged (object? sender, ValueChangedEventArgs<Point?> e) => ForceInlinePosition = e.NewValue;

    /// <summary>
    ///     Unsubscribes from Application static property change events.
    /// </summary>
    private void UnsubscribeApplicationEvents ()
    {
        Application.ForceDriverChanged -= OnForceDriverChanged;
        Application.AppModelChanged -= OnAppModelChanged;
        Application.ForceInlinePositionChanged -= OnForceInlinePositionChanged;
    }
}
