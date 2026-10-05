using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Terminal.Gui.App;

/// <summary>
///     Interface for instances that provide backing functionality to static
///     gateway class <see cref="Application"/>.
/// </summary>
/// <remarks>
///     <para>
///         Implements <see cref="IDisposable"/> to support automatic resource cleanup via using statements.
///         Call <see cref="IDisposable.Dispose"/> or use a using statement to properly clean up resources.
///     </para>
/// </remarks>
public interface IApplication : IDisposable
{
    #region Lifecycle - App Initialization and Shutdown

    /// <summary>
    ///     Gets or sets the managed thread ID of the application's main UI thread, which is set during
    ///     <see cref="Init"/> and used to determine if code is executing on the main thread.
    /// </summary>
    /// <value>
    ///     The managed thread ID of the main UI thread, or <see langword="null"/> if the application is not initialized.
    /// </value>
    public int? MainThreadId { get; internal set; }

    /// <summary>Initializes a new instance of <see cref="Terminal.Gui"/> Application.</summary>
    /// <param name="driverName">
    ///     The short name (<see cref="DriverRegistry.Names"/>) of the
    ///     <see cref="IDriver"/> to use. If not specified the default driver for the platform will be used.
    /// </param>
    /// <returns>This instance for fluent API chaining.</returns>
    /// <remarks>
    ///     <para>Call this method once per instance (or after <see cref="IDisposable.Dispose"/> has been called).</para>
    ///     <para>
    ///         This function loads the right <see cref="IDriver"/> for the platform, creates a main loop coordinator,
    ///         initializes keyboard and mouse handlers, and subscribes to driver events.
    ///     </para>
    ///     <para>
    ///         <see cref="IDisposable.Dispose"/> must be called when the application is closing (typically after
    ///         <see cref="Run{TRunnable}"/> has returned) to ensure all resources are cleaned up (disposed) and
    ///         terminal settings are restored.
    ///     </para>
    ///     <para>
    ///         Supports fluent API with automatic resource management:
    ///     </para>
    ///     <para>
    ///         Recommended pattern (using statement):
    ///         <code>
    ///         using (var app = Application.Create().Init())
    ///         {
    ///             app.Run&lt;MyDialog&gt;();
    ///             var result = app.GetResult&lt;MyResultType&gt;();
    ///         } // app.Dispose() called automatically
    ///         </code>
    ///     </para>
    ///     <para>
    ///         Alternative pattern (manual disposal):
    ///         <code>
    ///         var app = Application.Create().Init();
    ///         app.Run&lt;MyDialog&gt;();
    ///         var result = app.GetResult&lt;MyResultType&gt;();
    ///         app.Dispose(); // Must call explicitly
    ///         </code>
    ///     </para>
    ///     <para>
    ///         Note: Runnables created by <see cref="Run{TRunnable}"/> are automatically disposed when
    ///         that method returns. Runnables passed to <see cref="Run(IRunnable, Func{Exception, bool})"/>
    ///         must be disposed by the caller.
    ///     </para>
    /// </remarks>
    public IApplication Init (string? driverName = null);

    /// <summary>
    ///     This event is raised after the <see cref="Init"/> and <see cref="IDisposable.Dispose"/> methods have been called.
    /// </summary>
    /// <remarks>
    ///     Intended to support unit tests that need to know when the application has been initialized.
    /// </remarks>
    public event EventHandler<EventArgs<bool>>? InitializedChanged;

    /// <summary>Gets or sets whether the application has been initialized.</summary>
    bool Initialized { get; set; }

    /// <summary>
    ///     INTERNAL: Resets the state of this instance. Called by Dispose.
    /// </summary>
    /// <param name="ignoreDisposed">If true, ignores disposed state checks during reset.</param>
    /// <remarks>
    ///     <para>
    ///         Encapsulates all setting of initial state for Application; having this in a function like this ensures we
    ///         don't make mistakes in guaranteeing that the state of this singleton is deterministic when <see cref="Init"/>
    ///         starts running and after <see cref="IDisposable.Dispose"/> returns.
    ///     </para>
    ///     <para>
    ///         IMPORTANT: Ensure all property/fields are reset here. See Init_ResetState_Resets_Properties unit test.
    ///     </para>
    /// </remarks>
    internal void ResetState (bool ignoreDisposed = false);

    #endregion App Initialization and Shutdown

    #region Session Management - Begin->Run->Iteration->Stop->End

    /// <summary>
    ///     Gets the stack of all active runnable session tokens.
    ///     Sessions execute serially - the top of stack is the currently modal session.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Session tokens are pushed onto the stack when <see cref="Run(IRunnable, Func{Exception, bool})"/> is called and
    ///         popped when
    ///         <see cref="RequestStop(IRunnable)"/> completes. The stack grows during nested modal calls and
    ///         shrinks as they complete.
    ///     </para>
    ///     <para>
    ///         Only the top session (<see cref="TopRunnableView"/>) has exclusive keyboard/mouse input (
    ///         <see cref="IRunnable.IsModal"/> = true).
    ///         All other sessions on the stack continue to be laid out, drawn, and receive iteration events (
    ///         <see cref="IRunnable.IsRunning"/> = true),
    ///         but they don't receive user input.
    ///     </para>
    ///     <example>
    ///         Stack during nested modals:
    ///         <code>
    /// RunnableSessionStack (top to bottom):
    /// - MessageBox (TopRunnable, IsModal=true, IsRunning=true, has input)
    /// - FileDialog (IsModal=false, IsRunning=true, continues to update/draw)
    /// - MainWindow (IsModal=false, IsRunning=true, continues to update/draw)
    /// </code>
    ///     </example>
    /// </remarks>
    ConcurrentStack<SessionToken>? SessionStack { get; }

    /// <summary>
    ///     Raised by <see cref="Begin(IRunnable)"/> after the new <see cref="SessionToken"/> has been pushed onto
    ///     <see cref="SessionStack"/> and <see cref="TopRunnable"/> has been set, but before
    ///     <see cref="IRunnable.IsRunningChanged"/> and <see cref="IRunnable.IsModalChanged"/> fire. This event
    ///     reports a token-lifecycle moment, not a state transition on the <see cref="IRunnable"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         To observe the runnable transitioning to the running state, subscribe to
    ///         <see cref="IRunnable.IsRunningChanged"/>. <see cref="SessionBegun"/> is not a substitute: at the moment
    ///         it fires, the cached <see cref="IRunnable.IsRunning"/> value is still <see langword="false"/> because
    ///         <see cref="IRunnable.SetIsRunning"/> has not yet been called. Subscribers that need post-state-change
    ///         semantics must use <see cref="IRunnable.IsRunningChanged"/>.
    ///     </para>
    ///     <para>
    ///         <see cref="SessionBegun"/> and <see cref="SessionEnded"/> are intentionally NOT a symmetric
    ///         before/after pair. <see cref="SessionBegun"/> fires early — after the token has been pushed onto
    ///         <see cref="SessionStack"/> but before the runnable's running/modal state has been published.
    ///         <see cref="SessionEnded"/> fires late — after all state has unwound. This asymmetry is deliberate:
    ///         the events mark token-lifecycle boundaries, not runnable-state transitions.
    ///     </para>
    ///     <para>
    ///         If <see cref="StopAfterFirstIteration"/> is <see langword="true"/>, callers to
    ///         <see cref="Begin(IRunnable)"/> must also subscribe to <see cref="SessionEnded"/> and call
    ///         <see cref="End(SessionToken)"/> themselves to complete the session.
    ///     </para>
    /// </remarks>
    public event EventHandler<SessionTokenEventArgs>? SessionBegun;

    #region TopRunnable Properties

    /// <summary>Gets the Runnable that is on the top of the <see cref="SessionStack"/>.</summary>
    /// <remarks>
    ///     <para>
    ///         The top runnable in the session stack captures all mouse and keyboard input.
    ///         This is set by <see cref="Begin(IRunnable)"/> and cleared by <see cref="End(SessionToken)"/>.
    ///     </para>
    /// </remarks>
    IRunnable? TopRunnable { get; }

    /// <summary>Gets the View that is on the top of the <see cref="SessionStack"/>.</summary>
    /// <remarks>
    ///     <para>
    ///         This is a convenience property that casts <see cref="TopRunnable"/> to a <see cref="View"/>.
    ///     </para>
    /// </remarks>
    View? TopRunnableView { get; }

    #endregion TopRunnable Properties

    /// <summary>
    ///     Building block API: Creates a <see cref="SessionToken"/> and prepares the provided <see cref="IRunnable"/>
    ///     for
    ///     execution. Not usually called directly by applications. Use <see cref="Run(IRunnable, Func{Exception, bool})"/>
    ///     instead.
    /// </summary>
    /// <param name="runnable">The <see cref="IRunnable"/> to prepare execution for.</param>
    /// <returns>
    ///     The <see cref="SessionToken"/> that needs to be passed to the <see cref="End(SessionToken)"/>
    ///     method upon
    ///     completion.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method prepares the provided <see cref="IRunnable"/> for running. It adds this to the
    ///         <see cref="SessionStack"/>, lays out the SubViews, focuses the first element, and draws the
    ///         runnable on the screen. This is usually followed by starting the main loop, and then the
    ///         <see cref="End(SessionToken)"/> method upon termination which will undo these changes.
    ///     </para>
    ///     <para>
    ///         Raises the <see cref="IRunnable.IsRunningChanging"/>, <see cref="IRunnable.IsRunningChanged"/>,
    ///         and <see cref="IRunnable.IsModalChanged"/> events.
    ///     </para>
    /// </remarks>
    /// <returns>The session token. <see langword="null"/> if the operation was cancelled.</returns>
    SessionToken? Begin (IRunnable runnable);

    /// <summary>
    ///     Runs a new Session with the provided runnable view.
    /// </summary>
    /// <param name="runnable">The runnable to execute.</param>
    /// <param name="errorHandler">Optional handler for unhandled exceptions (resumes when returns true, rethrows when null).</param>
    /// <remarks>
    ///     <para>
    ///         This method is used to start processing events for the main application, but it is also used to run other
    ///         modal views such as dialogs.
    ///     </para>
    ///     <para>
    ///         To make <see cref="Run(IRunnable, Func{Exception, bool})"/> stop execution, call
    ///         <see cref="RequestStop()"/> or <see cref="RequestStop(IRunnable)"/>.
    ///     </para>
    ///     <para>
    ///         Calling <see cref="Run(IRunnable, Func{Exception, bool})"/> is equivalent to calling
    ///         <see cref="Begin(IRunnable)"/>, followed by starting the main loop, and then calling
    ///         <see cref="End(SessionToken)"/>.
    ///     </para>
    ///     <para>
    ///         In RELEASE builds: When <paramref name="errorHandler"/> is <see langword="null"/> any exceptions will be
    ///         rethrown. Otherwise, <paramref name="errorHandler"/> will be called. If <paramref name="errorHandler"/>
    ///         returns <see langword="true"/> the main loop will resume; otherwise this method will exit.
    ///     </para>
    /// </remarks>
    object? Run (IRunnable runnable, Func<Exception, bool>? errorHandler = null);

    /// <summary>
    ///     Asynchronously runs a new Session with the provided runnable view, observing a <see cref="CancellationToken"/>.
    /// </summary>
    /// <param name="runnable">The runnable to execute.</param>
    /// <param name="cancellationToken">
    ///     A cancellation token that, when cancelled, will call <see cref="RequestStop(IRunnable)"/> to cleanly stop the run
    ///     loop.
    /// </param>
    /// <param name="errorHandler">Optional handler for unhandled exceptions (resumes when returns true, rethrows when null).</param>
    /// <returns>
    ///     A <see cref="Task{Object}"/> that completes when the session ends. The result is the value returned by
    ///     <see cref="Run(IRunnable, Func{Exception, bool})"/>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method wraps <see cref="Run(IRunnable, Func{Exception, bool})"/> and registers the
    ///         <paramref name="cancellationToken"/> so that cancellation triggers <see cref="RequestStop(IRunnable)"/>.
    ///     </para>
    ///     <para>
    ///         If the token is already cancelled when this method is called, it returns immediately without starting the
    ///         session.
    ///     </para>
    ///     <para>
    ///         Cancellation is idempotent: if both the token and an internal <see cref="RequestStop()"/> fire, only a single
    ///         shutdown occurs.
    ///     </para>
    /// </remarks>
    Task<object?> RunAsync (IRunnable runnable, CancellationToken cancellationToken, Func<Exception, bool>? errorHandler = null);

    /// <summary>
    ///     Asynchronously runs a new Session creating a <see cref="IRunnable"/>-derived object of type
    ///     <typeparamref name="TRunnable"/>, observing a <see cref="CancellationToken"/>.
    /// </summary>
    /// <typeparam name="TRunnable">The type of runnable to create and run.</typeparam>
    /// <param name="cancellationToken">
    ///     A cancellation token that, when cancelled, will call <see cref="RequestStop(IRunnable)"/> to cleanly stop the run
    ///     loop.
    /// </param>
    /// <param name="errorHandler">Optional handler for unhandled exceptions (resumes when returns true, rethrows when null).</param>
    /// <param name="driverName">
    ///     The driver name. If not specified the default driver for the platform will be used.
    /// </param>
    /// <returns>
    ///     A <see cref="Task"/> representing the asynchronous operation. The <see cref="IApplication"/> instance is returned
    ///     for fluent chaining.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method wraps <see cref="Run{TRunnable}(Func{Exception, bool}, string)"/> and registers the
    ///         <paramref name="cancellationToken"/> so that cancellation triggers <see cref="RequestStop()"/>.
    ///     </para>
    ///     <para>
    ///         If the token is already cancelled when this method is called, it returns immediately without starting the
    ///         session.
    ///     </para>
    /// </remarks>
    Task<IApplication> RunAsync<TRunnable> (CancellationToken cancellationToken, Func<Exception, bool>? errorHandler = null, string? driverName = null) where TRunnable : IRunnable, new();

    /// <summary>
    ///     Runs a new Session creating a <see cref="IRunnable"/>-derived object of type <typeparamref name="TRunnable"/>
    ///     and calling <see cref="Run(IRunnable, Func{Exception, bool})"/>. When the session is stopped,
    ///     <see cref="End(SessionToken)"/> will be called.
    /// </summary>
    /// <typeparam name="TRunnable"></typeparam>
    /// <param name="errorHandler">Handler for any unhandled exceptions (resumes when returns true, rethrows when null).</param>
    /// <param name="driverName">
    ///     The driver name. If not specified the default driver for the platform will be used. Must be
    ///     <see langword="null"/> if <see cref="Init"/> has already been called.
    /// </param>
    /// <returns>
    ///     The created <see name="IApplication"/> object. The caller is responsible for calling
    ///     <see cref="IDisposable.Dispose"/> on this
    ///     object.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method is used to start processing events for the main application, but it is also used to run other
    ///         modal <see cref="View"/>s such as <see cref="Dialog"/> boxes.
    ///     </para>
    ///     <para>
    ///         To make <see cref="Run(IRunnable, Func{Exception, bool})"/> stop execution, call
    ///         <see cref="RequestStop()"/> or <see cref="RequestStop(IRunnable)"/>.
    ///     </para>
    ///     <para>
    ///         In RELEASE builds: When <paramref name="errorHandler"/> is <see langword="null"/> any exceptions will be
    ///         rethrown. Otherwise, <paramref name="errorHandler"/> will be called. If <paramref name="errorHandler"/>
    ///         returns <see langword="true"/> the main loop will resume; otherwise this method will exit.
    ///     </para>
    ///     <para>
    ///         <see cref="IDisposable.Dispose"/> must be called when the application is closing (typically after Run has
    ///         returned) to
    ///         ensure resources are cleaned up and terminal settings restored.
    ///     </para>
    ///     <para>
    ///         In RELEASE builds: When <paramref name="errorHandler"/> is <see langword="null"/> any exceptions will be
    ///         rethrown. Otherwise, <paramref name="errorHandler"/> will be called. If <paramref name="errorHandler"/>
    ///         returns <see langword="true"/> the main loop will resume; otherwise this method will exit.
    ///     </para>
    ///     <para>
    ///         The caller is responsible for disposing the object returned by this method.
    ///     </para>
    /// </remarks>
    public IApplication Run<TRunnable> (Func<Exception, bool>? errorHandler = null, string? driverName = null) where TRunnable : IRunnable, new();

    #region Iteration & Invoke

    /// <summary>
    ///     Raises the <see cref="Iteration"/> event.
    /// </summary>
    /// <remarks>
    ///     This is called once per main loop iteration, before processing input, timeouts, or rendering.
    /// </remarks>
    public void RaiseIteration ();

    /// <summary>This event is raised on each iteration of the main loop.</summary>
    /// <remarks>
    ///     <para>
    ///         This event is raised before input processing, timeout callbacks, and rendering occur each iteration.
    ///     </para>
    ///     <para>The event args contain the current application instance.</para>
    /// </remarks>
    /// <seealso cref="AddTimeout"/>
    /// <seealso cref="TimedEvents"/>
    /// .
    public event EventHandler<EventArgs<IApplication?>>? Iteration;

    /// <summary>Runs <paramref name="action"/> on the main UI loop thread.</summary>
    /// <param name="action">The action to be invoked on the main processing thread.</param>
    /// <remarks>
    ///     <para>
    ///         If called from the main thread, the action is executed immediately. Otherwise, it is queued via
    ///         <see cref="AddTimeout"/> with <see cref="TimeSpan.Zero"/> and will be executed on the next main loop
    ///         iteration.
    ///     </para>
    /// </remarks>
    /// <exception cref="NotInitializedException">
    ///     Thrown when <see cref="Init"/> has not been called or after <see cref="IDisposable.Dispose"/> has been called.
    /// </exception>
    void Invoke (Action<IApplication>? action);

    /// <summary>Runs <paramref name="action"/> on the main UI loop thread.</summary>
    /// <param name="action">The action to be invoked on the main processing thread.</param>
    /// <remarks>
    ///     <para>
    ///         If called from the main thread, the action is executed immediately. Otherwise, it is queued via
    ///         <see cref="AddTimeout"/> with <see cref="TimeSpan.Zero"/> and will be executed on the next main loop
    ///         iteration.
    ///     </para>
    /// </remarks>
    /// <exception cref="NotInitializedException">
    ///     Thrown when <see cref="Init"/> has not been called or after <see cref="IDisposable.Dispose"/> has been called.
    /// </exception>
    void Invoke (Action action);

    #endregion Iteration & Invoke

    /// <summary>
    ///     Set to <see langword="true"/> to cause the session to stop running after first iteration.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Used primarily for unit testing. When <see langword="true"/>, <see cref="End(SessionToken)"/> will be
    ///         called
    ///         automatically after the first main loop iteration.
    ///     </para>
    /// </remarks>
    bool StopAfterFirstIteration { get; set; }

    /// <summary>Requests that the currently running Session stop. The Session will stop after the current iteration completes.</summary>
    /// <remarks>
    ///     <para>This will cause <see cref="Run(IRunnable, Func{Exception, bool})"/> to return.</para>
    ///     <para>
    ///         This is equivalent to calling <see cref="RequestStop(IRunnable)"/> with <see cref="TopRunnableView"/> as the
    ///         parameter.
    ///     </para>
    /// </remarks>
    void RequestStop ();

    /// <summary>
    ///     Requests that the specified runnable session stop.
    /// </summary>
    /// <param name="runnable">
    ///     The runnable to stop. If <see langword="null"/>, stops the current <see cref="TopRunnableView"/>
    ///     .
    /// </param>
    /// <remarks>
    ///     <para>
    ///         This will cause <see cref="Run(IRunnable, Func{Exception, bool})"/> to return.
    ///     </para>
    ///     <para>
    ///         Raises <see cref="IRunnable.IsRunningChanging"/>, <see cref="IRunnable.IsRunningChanged"/>,
    ///         and <see cref="IRunnable.IsModalChanged"/> events.
    ///     </para>
    /// </remarks>
    void RequestStop (IRunnable? runnable);

    /// <summary>
    ///     Building block API: Ends the session associated with the token and completes the execution of an
    ///     <see cref="IRunnable"/>.
    ///     Not usually called directly by applications. <see cref="Run(IRunnable, Func{Exception, bool})"/>
    ///     will automatically call this method when the session is stopped.
    /// </summary>
    /// <param name="sessionToken">
    ///     The <see cref="SessionToken"/> returned by the <see cref="Begin(IRunnable)"/>
    ///     method.
    /// </param>
    /// <remarks>
    ///     <para>
    ///         This method removes the <see cref="IRunnable"/> from the <see cref="SessionStack"/>,
    ///         raises the lifecycle events, and clears <see cref="SessionToken.Runnable"/> on
    ///         <paramref name="sessionToken"/>.
    ///     </para>
    ///     <para>
    ///         Raises <see cref="IRunnable.IsRunningChanging"/>, <see cref="IRunnable.IsRunningChanged"/>,
    ///         and <see cref="IRunnable.IsModalChanged"/> events.
    ///     </para>
    /// </remarks>
    void End (SessionToken sessionToken);

    /// <summary>
    ///     Raised by <see cref="End(SessionToken)"/> after the runnable's state has fully unwound — including
    ///     <see cref="IRunnable.IsRunningChanged"/> firing with <c>false</c> — and after <see cref="End(SessionToken)"/>
    ///     has finished its state-unwinding work. The <see cref="SessionToken.Runnable"/> reference is
    ///     <see langword="null"/> at this point. This event reports a token-lifecycle moment, not a state transition
    ///     on the <see cref="IRunnable"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         At the moment <see cref="SessionEnded"/> fires, all state has already unwound:
    ///         <see cref="IRunnable.IsRunning"/> is <see langword="false"/> and
    ///         <see cref="IRunnable.IsRunningChanged"/> has already fired with <c>false</c>. To observe the runnable
    ///         transitioning out of the running state, subscribe to <see cref="IRunnable.IsRunningChanged"/> instead.
    ///     </para>
    ///     <para>
    ///         The token's <see cref="SessionToken.Runnable"/> property is <see langword="null"/> at this point — it
    ///         is cleared immediately before the event is raised. Subscribers that need the session result must read
    ///         <see cref="SessionToken.Result"/> from the token; they must not attempt to access
    ///         <see cref="SessionToken.Runnable"/>.
    ///     </para>
    ///     <para>
    ///         <see cref="SessionEnded"/> and <see cref="SessionBegun"/> are intentionally NOT a symmetric
    ///         before/after pair. <see cref="SessionBegun"/> fires early — after the token has been pushed onto
    ///         <see cref="SessionStack"/> but before the runnable's running/modal state has been published.
    ///         <see cref="SessionEnded"/> fires late — after all state has unwound. This asymmetry is deliberate:
    ///         the events mark token-lifecycle boundaries, not runnable-state transitions.
    ///     </para>
    ///     <para>
    ///         If <see cref="StopAfterFirstIteration"/> is <see langword="true"/>, callers to
    ///         <see cref="Begin(IRunnable)"/> must also subscribe to <see cref="SessionEnded"/> and call
    ///         <see cref="End(SessionToken)"/> themselves to complete the session.
    ///     </para>
    /// </remarks>
    public event EventHandler<SessionTokenEventArgs>? SessionEnded;

    #endregion Session Management - Begin->Run->Iteration->Stop->End

    #region Result Management

    /// <summary>
    ///     Gets the result from the last <see cref="Run(IRunnable, Func{Exception, bool})"/> or
    ///     <see cref="Run{TRunnable}(Func{Exception, bool}, string)"/> call.
    /// </summary>
    /// <returns>
    ///     The result from the last run session, or <see langword="null"/> if no session has been run or the result was null.
    /// </returns>
    object? GetResult ();

    /// <summary>
    ///     Gets the result from the last <see cref="Run(IRunnable, Func{Exception, bool})"/> or
    ///     <see cref="Run{TRunnable}(Func{Exception, bool}, string)"/> call, cast to type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected result type.</typeparam>
    /// <returns>
    ///     The result cast to <typeparamref name="T"/>, or <see langword="null"/> if the result is null or cannot be cast.
    /// </returns>
    /// <example>
    ///     <code>
    ///     using (var app = Application.Create().Init())
    ///     {
    ///         app.Run&lt;ColorPickerDialog&gt;();
    ///         var selectedColor = app.GetResult&lt;Color&gt;();
    ///         if (selectedColor.HasValue)
    ///         {
    ///             // Use the color
    ///         }
    ///     }
    ///     </code>
    /// </example>
    T? GetResult<T> () where T : class => GetResult () as T;

    #endregion Result Management

    #region Screen and Driver

    /// <summary>Gets or sets the console driver being used.</summary>
    /// <remarks>
    ///     <para>
    ///         Set by <see cref="Init"/> based on the driver parameter or platform default.
    ///     </para>
    /// </remarks>
    IDriver? Driver { get; set; }

    /// <summary>
    ///     Gets or sets the ANSI startup readiness gate.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When set, startup rendering can be deferred until required ANSI startup capability
    ///         probes complete (or time out). When <see langword="null"/>, startup rendering proceeds
    ///         immediately.
    ///     </para>
    ///     <para>
    ///         Tests and inline app mode can assign a gate instance to opt into this behavior.
    ///     </para>
    /// </remarks>
    AnsiStartupGate? AnsiStartupGate { get; set; }

    /// <summary>
    ///     Gets the clipboard for this application instance.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Provides access to the OS clipboard through the driver. Returns <see langword="null"/> if
    ///         <see cref="Driver"/> is not initialized.
    ///     </para>
    /// </remarks>
    IClipboard? Clipboard { get; internal set; }

    /// <summary>
    ///     Forces the use of the specified driver (<see cref="DriverRegistry.Names"/>). If not
    ///     specified, the driver is selected based on the platform.
    /// </summary>
    string ForceDriver { get; set; }

    /// <summary>
    ///     Gets or sets how the application interacts with the terminal buffer.
    ///     <see cref="AppModel.FullScreen"/> uses the alternate screen buffer (default).
    ///     <see cref="AppModel.Inline"/> renders inline in the primary scrollback buffer.
    /// </summary>
    AppModel AppModel { get; set; }

    /// <summary>
    ///     Gets or sets an override for the initial cursor position used in <see cref="AppModel.Inline"/> mode.
    ///     When set (non-null) before <see cref="Run{T}"/>, this value is used instead of
    ///     querying the terminal via ANSI CPR. Useful for testing inline mode at specific cursor positions.
    ///     The <c>Y</c> component specifies the terminal row; <c>X</c> is reserved for future use.
    /// </summary>
    Point? ForceInlinePosition { get; set; }

    /// <summary>
    ///     Gets or sets the size of the screen. By default, this is the size of the screen as reported by the
    ///     <see cref="IDriver"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If the <see cref="IDriver"/> has not been initialized, this will return a default size of 2048x2048; useful
    ///         for unit tests.
    ///     </para>
    ///     <para>
    ///         In <see cref="AppModel.FullScreen"/> mode, setting this property delegates to
    ///         <see cref="IDriver.SetScreenSize(int,int)"/> when the Driver has been initialized. If the Driver
    ///         is <see langword="null"/> (before <see cref="IApplication.Init(string?)"/> or after disposal),
    ///         the setter raises <see cref="ScreenChanged"/> with the current getter value but does not call the Driver.
    ///         To simulate a terminal resize in tests, prefer calling <see cref="IDriver.SetScreenSize(int,int)"/> directly.
    ///     </para>
    /// </remarks>
    Rectangle Screen { get; set; }

    /// <summary>Raised when the terminal's size changed. The new size of the terminal is provided.</summary>
    /// <remarks>
    ///     <para>
    ///         This event is raised when the driver detects a screen size change. The event provides the new screen
    ///         rectangle.
    ///     </para>
    /// </remarks>
    public event EventHandler<EventArgs<Rectangle>>? ScreenChanged;

    /// <summary>
    ///     Gets or sets whether the screen will be cleared, and all Views redrawn, during the next Application iteration.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is typically set to <see langword="true"/> when a View's <see cref="View.Frame"/> changes and that view
    ///         has no SuperView (e.g. when <see cref="TopRunnableView"/> is moved or resized).
    ///     </para>
    ///     <para>
    ///         Automatically reset to <see langword="false"/> after <see cref="LayoutAndDraw"/> processes it.
    ///     </para>
    /// </remarks>
    bool ClearScreenNextIteration { get; set; }

    #endregion Screen and Driver

    #region Input (Mouse/Keyboard)

    /// <summary>
    ///     Gets the input injector for programmatic input injection in tests.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The input injector provides a simplified API for injecting keyboard and mouse events
    ///         in tests. It handles encoding, queueing, and processing automatically.
    ///     </para>
    ///     <para>
    ///         Use <see cref="Application.Create"/> to create an application with
    ///         <see cref="VirtualTimeProvider"/> for deterministic, fast tests.
    ///     </para>
    ///     <para>
    ///         Example usage:
    ///         <code>
    ///             VirtualTimeProvider time = new ();
    ///             using IApplication app = Application.Create (time);
    ///             app.Init ();
    ///             app.InjectKey (Key.Enter);  // Extension method uses GetInputInjector()
    ///         </code>
    ///     </para>
    /// </remarks>
    /// <returns>The <see cref="IInputInjector"/> for input injection.</returns>
    IInputInjector GetInputInjector ();

    /// <summary>
    ///     Handles keyboard input and key bindings at the Application level.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Provides access to keyboard state, key bindings, and keyboard event handling. Set during <see cref="Init"/>.
    ///     </para>
    /// </remarks>
    IKeyboard Keyboard { get; set; }

    /// <summary>
    ///     Handles mouse event state and processing.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Provides access to mouse state, mouse grabbing, and mouse event handling. Set during <see cref="Init"/>.
    ///     </para>
    /// </remarks>
    IMouse Mouse { get; set; }

    /// <summary>
    ///     Raised when the terminal delivers a bracketed paste. Fires before the paste is dispatched
    ///     to the focused view; set <see cref="System.ComponentModel.HandledEventArgs.Handled"/> on
    ///     the event arguments to <see langword="true"/> to prevent the focused view from receiving
    ///     the paste.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Bracketed paste mode is enabled automatically by the driver. On terminals that do not
    ///         support bracketed paste (or have it disabled by user configuration) the pasted text is
    ///         delivered as ordinary key events and this event does not fire.
    ///     </para>
    /// </remarks>
    event EventHandler<PasteEventArgs>? Paste;

    /// <summary>
    ///     Raises the <see cref="Paste"/> event with <paramref name="text"/>, then dispatches the
    ///     paste to the focused view by invoking <see cref="Command.Paste"/> with a dedicated
    ///     command-context paste payload if not already handled. The default
    ///     <see cref="Command.Paste"/> handler on <see cref="View"/> sanitizes the payload, raises
    ///     <see cref="View.Pasting"/>, and delegates insertion to the focused view's
    ///     <see cref="View.OnPaste"/> override. If the focused view consumes the paste and reports
    ///     a final-text segment for the pasted range, the handler raises <see cref="View.Pasted"/>.
    /// </summary>
    /// <param name="text">The pasted content with bracketing markers stripped.</param>
    /// <returns><see langword="true"/> if the paste was handled.</returns>
    bool RaisePasteEvent (string text);

    #endregion Input (Mouse/Keyboard)

    #region Layout and Drawing

    /// <summary>
    ///     Causes any Runnables that need layout to be laid out, then draws any Runnables that need display. Only Views
    ///     that need to be laid out (see <see cref="View.NeedsLayout"/>) will be laid out. Only Views that need to be drawn
    ///     (see <see cref="View.NeedsDraw"/>) will be drawn.
    /// </summary>
    /// <param name="forceRedraw">
    ///     If <see langword="true"/> the entire View hierarchy will be redrawn. The default is <see langword="false"/> and
    ///     should only be overridden for testing.
    /// </param>
    /// <remarks>
    ///     <para>
    ///         This method is called automatically each main loop iteration when any views need layout or drawing.
    ///     </para>
    ///     <para>
    ///         If <see cref="ClearScreenNextIteration"/> is <see langword="true"/>, the screen will be cleared before
    ///         drawing and the flag will be reset to <see langword="false"/>.
    ///     </para>
    /// </remarks>
    public void LayoutAndDraw (bool forceRedraw = false);

    /// <summary>Raised when the <see cref="LayoutAndDraw"/> has completed and one or more Views has been laid out or drawn.</summary>
    /// <remarks>
    ///     This is not raised every iteration; or call to LayoutAndDraw. It is only raised if View.Draw or View.Layout was called
    ///     on at least one View in the app.
    /// </remarks>
    public event EventHandler<EventArgs>? LayoutAndDrawComplete;

    #endregion Layout and Drawing

    #region Navigation and Popover

    /// <summary>Gets or sets the navigation manager.</summary>
    /// <remarks>
    ///     <para>
    ///         Manages focus navigation and tracking of the most focused view. Initialized during <see cref="Init"/>.
    ///     </para>
    /// </remarks>
    ApplicationNavigation? Navigation { get; set; }

    /// <summary>Gets or sets the popover manager.</summary>
    /// <remarks>
    ///     <para>
    ///         Manages application-level popover views. Initialized during <see cref="Init"/>.
    ///     </para>
    /// </remarks>
    ApplicationPopover? Popovers { get; set; }

    /// <summary>
    ///     Gets or sets the tool tip manager used to display contextual help for UI elements within the application.
    /// </summary>
    /// <remarks>
    ///     Assigning a value to this property enables tool tip functionality, allowing users to receive
    ///     additional information when interacting with supported controls. If set to null, tool tips are disabled for the
    ///     application.
    /// </remarks>
    ApplicationToolTip? ToolTips { get; set; }

    #endregion Navigation and Popover

    #region Timeouts

    /// <summary>Adds a timeout to the application.</summary>
    /// <param name="time">The time span to wait before invoking the callback.</param>
    /// <param name="callback">
    ///     The callback to invoke. If it returns <see langword="true"/>, the timeout will be reset and repeat. If it
    ///     returns <see langword="false"/>, the timeout will stop and be removed.
    /// </param>
    /// <returns>
    ///     Call <see cref="RemoveTimeout(object)"/> with the returned value to stop the timeout.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         When the time specified passes, the callback will be invoked on the main UI thread.
    ///     </para>
    ///     <para>
    ///         <see cref="IDisposable.Dispose"/> calls StopAll on <see cref="TimedEvents"/> to remove all timeouts.
    ///     </para>
    /// </remarks>
    object? AddTimeout (TimeSpan time, Func<bool> callback);

    /// <summary>Cancels all timeout occurrences associated with a previously returned token.</summary>
    /// <param name="token">The token returned by <see cref="AddTimeout"/>.</param>
    /// <remarks>
    ///     A matching callback that is already executing is not interrupted, but it will not be rescheduled if it returns
    ///     <see langword="true"/>.
    /// </remarks>
    /// <returns>
    ///     <see langword="true"/> if at least one queued occurrence was removed or a cancellation request was recorded for
    ///     at least one active occurrence; otherwise, <see langword="false"/>.
    /// </returns>
    bool RemoveTimeout (object token);

    /// <summary>
    ///     Handles recurring events. These are invoked on the main UI thread - allowing for
    ///     safe updates to <see cref="View"/> instances.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Provides low-level access to the timeout management system. Most applications should use
    ///         <see cref="AddTimeout"/> and <see cref="RemoveTimeout"/> instead.
    ///     </para>
    /// </remarks>
    ITimedEvents? TimedEvents { get; }

    #endregion Timeouts

    /// <summary>
    ///     Gets a string representation of the Application as rendered by <see cref="Driver"/>.
    /// </summary>
    /// <returns>A string representation of the Application </returns>
    public string ToString ();

}
