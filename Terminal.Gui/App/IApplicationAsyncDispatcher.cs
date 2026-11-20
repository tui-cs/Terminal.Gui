namespace Terminal.Gui.App;

/// <summary>Optional awaitable dispatch capability for <see cref="IApplication"/> implementations.</summary>
/// <remarks>
///     Implement this interface to make the <see cref="ApplicationDispatchExtensions.InvokeAsync(IApplication, Action, CancellationToken)"/>
///     extension methods available on a custom application implementation without adding members to <see cref="IApplication"/>.
/// </remarks>
public interface IApplicationAsyncDispatcher
{
    /// <summary>Runs an action on the application's UI thread and completes when the action returns.</summary>
    /// <param name="action">The action to run with the application instance.</param>
    /// <param name="owner">The session that owns the action, or <see langword="null"/> for application-owned work.</param>
    /// <param name="cancellationToken">Cancels a dispatch that has not started.</param>
    /// <returns>A task that reports completion, cancellation, or the action's exception.</returns>
    Task InvokeAsync (Action<IApplication> action, SessionToken? owner, CancellationToken cancellationToken = default);
}
