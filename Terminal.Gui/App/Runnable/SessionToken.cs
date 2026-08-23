namespace Terminal.Gui.App;

/// <summary>
///     Represents a running session created by <see cref="IApplication.Begin(IRunnable)"/>.
///     Wraps an <see cref="IRunnable"/> instance and is stored in <see cref="IApplication.SessionStack"/>.
/// </summary>
public class SessionToken
{
    internal SessionToken (IRunnable runnable) => Runnable = runnable;

    /// <summary>
    ///     Gets or sets the runnable associated with this session.
    ///     Set to <see langword="null"/> by <see cref="IApplication.End(SessionToken)"/> when the session completes.
    /// </summary>
    public IRunnable? Runnable { get; internal set; }

    /// <summary>
    ///     The result of the session. Typically set by the runnable in <see langword="IRunnable.IsRunningChanged"/>
    /// </summary>
    public object? Result { get; set; }

    /// <summary>
    ///     The ambient <see cref="SynchronizationContext"/> the caller had when
    ///     <see cref="IApplication.Begin(IRunnable)"/> installed the app's context;
    ///     restored by <see cref="IApplication.End(SessionToken)"/>.
    /// </summary>
    internal SynchronizationContext? PreviousSynchronizationContext { get; set; }
}
