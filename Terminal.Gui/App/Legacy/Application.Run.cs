namespace Terminal.Gui.App;

public static partial class Application // Run (Begin -> Run -> Layout/Draw -> End -> Stop)
{
    /// <inheritdoc cref="IApplication.Begin(IRunnable)"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static SessionToken Begin (IRunnable runnable) => ApplicationImpl.Instance.Begin (runnable)!;

    /// <inheritdoc cref="IApplication.Run{TRunnable}(Func{Exception, bool}, string)"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static IApplication Run<TRunnable> (Func<Exception, bool>? errorHandler = null, string? driverName = null) where TRunnable : IRunnable, new() =>
        ApplicationImpl.Instance.Run<TRunnable> (errorHandler, driverName);

    /// <inheritdoc cref="IApplication.Run(IRunnable, Func{Exception, bool})"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static void Run (IRunnable runnable, Func<Exception, bool>? errorHandler = null) => ApplicationImpl.Instance.Run (runnable, errorHandler);

    /// <inheritdoc cref="IApplication.AddTimeout"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static object? AddTimeout (TimeSpan time, Func<bool> callback) => ApplicationImpl.Instance.AddTimeout (time, callback);

    /// <inheritdoc cref="IApplication.RemoveTimeout"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static bool RemoveTimeout (object token) => ApplicationImpl.Instance.RemoveTimeout (token);

    /// <inheritdoc cref="IApplication.TimedEvents"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static ITimedEvents? TimedEvents => ApplicationImpl.Instance.TimedEvents;

    /// <inheritdoc cref="IApplication.Invoke(Action{IApplication})"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static void Invoke (Action<IApplication> action) => ApplicationImpl.Instance.Invoke (action);

    /// <inheritdoc cref="IApplication.Invoke(Action)"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static void Invoke (Action action) => ApplicationImpl.Instance.Invoke (action);

    /// <inheritdoc cref="IApplication.LayoutAndDraw"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static void LayoutAndDraw (bool forceRedraw = false) => ApplicationImpl.Instance.LayoutAndDraw (forceRedraw);

    /// <inheritdoc cref="IApplication.StopAfterFirstIteration"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static bool StopAfterFirstIteration
    {
        get => ApplicationImpl.Instance.StopAfterFirstIteration;
        set => ApplicationImpl.Instance.StopAfterFirstIteration = value;
    }

    /// <inheritdoc cref="IApplication.RequestStop(IRunnable)"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static void RequestStop (IRunnable? runnable = null) => ApplicationImpl.Instance.RequestStop (runnable);

    /// <inheritdoc cref="IApplication.End(SessionToken)"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static void End (SessionToken sessionToken) => ApplicationImpl.Instance.End (sessionToken);

    /// <inheritdoc cref="IApplication.Iteration"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static event EventHandler<EventArgs<IApplication?>>? Iteration
    {
        add => ApplicationImpl.Instance.Iteration += value;
        remove => ApplicationImpl.Instance.Iteration -= value;
    }

    /// <inheritdoc cref="IApplication.SessionBegun"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static event EventHandler<SessionTokenEventArgs>? SessionBegun
    {
        add => ApplicationImpl.Instance.SessionBegun += value;
        remove => ApplicationImpl.Instance.SessionBegun -= value;
    }

    /// <inheritdoc cref="IApplication.SessionEnded"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static event EventHandler<SessionTokenEventArgs>? SessionEnded
    {
        add => ApplicationImpl.Instance.SessionEnded += value;
        remove => ApplicationImpl.Instance.SessionEnded -= value;
    }
}
