namespace Terminal.Gui.App;

/// <summary>Provides awaitable UI dispatch for Terminal.Gui application instances.</summary>
public static class ApplicationDispatchExtensions
{
    /// <summary>Runs <paramref name="action"/> on the UI thread and completes after it returns.</summary>
    /// <param name="app">The application that owns the UI thread.</param>
    /// <param name="action">The action to run.</param>
    /// <param name="cancellationToken">Cancels the dispatch if execution has not started.</param>
    /// <returns>A task that reports completion, cancellation, or the action's exception.</returns>
    /// <remarks>
    ///     The action runs immediately when called on the UI thread during a running session. Otherwise it is queued
    ///     for the next main-loop iteration. Exceptions from the action fault the task and do not enter the main-loop
    ///     error handler, except that an <see cref="OperationCanceledException"/> carrying the canceled caller token
    ///     cancels the task. Pending dispatches are canceled when the application is disposed or its final session ends.
    ///     Queued dispatches are independent of user timers; removing or stopping timers does not discard them.
    ///     Dispatches queued after initialization but before the first session wait for that session to start.
    ///     After the final session ends or disposal begins, a canceled await may resume on a thread-pool thread.
    ///     To observe action failures, await or otherwise inspect the returned task.
    /// </remarks>
    /// <exception cref="NotInitializedException">The application is not initialized or is shutting down.</exception>
    /// <exception cref="NotSupportedException">The application does not support awaitable dispatch.</exception>
    public static Task InvokeAsync (this IApplication app, Action action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull (action);

        return GetDispatcher (app).InvokeAsync (_ => action (), null, cancellationToken);
    }

    /// <summary>Runs <paramref name="action"/> on the UI thread and completes after it returns.</summary>
    /// <param name="app">The application that owns the UI thread.</param>
    /// <param name="action">The action to run with the application instance.</param>
    /// <param name="cancellationToken">Cancels the dispatch if execution has not started.</param>
    /// <returns>A task that reports completion, cancellation, or the action's exception.</returns>
    /// <inheritdoc cref="InvokeAsync(IApplication, Action, CancellationToken)" path="/remarks|/exception"/>
    public static Task InvokeAsync (this IApplication app, Action<IApplication> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull (action);

        return GetDispatcher (app).InvokeAsync (action, null, cancellationToken);
    }

    /// <summary>Runs <paramref name="action"/> while <paramref name="owner"/> is active.</summary>
    /// <param name="app">The application that owns the UI thread.</param>
    /// <param name="owner">The session that owns the action's referenced views.</param>
    /// <param name="action">The action to run.</param>
    /// <param name="cancellationToken">Cancels the dispatch if execution has not started.</param>
    /// <returns>A task that reports completion, cancellation, or the action's exception.</returns>
    /// <remarks>
    ///     The owner may be an outer session while an inner modal is running. Ending the owner cancels its pending
    ///     dispatches; ending another session does not. An owner that has already ended yields a canceled task. An owner
    ///     that has not started running yet, such as one reported by <see cref="IApplication.SessionBegun"/>, runs the
    ///     action on a later main-loop iteration after it starts.
    ///     After the final session ends or disposal begins, a canceled await may resume on a thread-pool thread.
    /// </remarks>
    /// <inheritdoc cref="InvokeAsync(IApplication, Action, CancellationToken)" path="/exception"/>
    public static Task InvokeAsync (this IApplication app, SessionToken owner, Action action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull (owner);
        ArgumentNullException.ThrowIfNull (action);

        return GetDispatcher (app).InvokeAsync (_ => action (), owner, cancellationToken);
    }

    /// <summary>Runs <paramref name="action"/> while <paramref name="owner"/> is active.</summary>
    /// <param name="app">The application that owns the UI thread.</param>
    /// <param name="owner">The session that owns the action's referenced views.</param>
    /// <param name="action">The action to run with the application instance.</param>
    /// <param name="cancellationToken">Cancels the dispatch if execution has not started.</param>
    /// <returns>A task that reports completion, cancellation, or the action's exception.</returns>
    /// <inheritdoc cref="InvokeAsync(IApplication, SessionToken, Action, CancellationToken)" path="/remarks|/exception"/>
    public static Task InvokeAsync (this IApplication app, SessionToken owner, Action<IApplication> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull (owner);
        ArgumentNullException.ThrowIfNull (action);

        return GetDispatcher (app).InvokeAsync (action, owner, cancellationToken);
    }

    private static IApplicationAsyncDispatcher GetDispatcher (IApplication app)
    {
        ArgumentNullException.ThrowIfNull (app);

        return app as IApplicationAsyncDispatcher ?? throw new NotSupportedException ("This IApplication implementation does not support awaitable UI dispatch.");
    }
}
