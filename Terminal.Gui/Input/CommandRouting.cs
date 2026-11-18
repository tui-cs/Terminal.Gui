namespace Terminal.Gui.Input;

/// <summary>
///     Describes how a <see cref="Command"/> is being routed through the view hierarchy.
///     Replaces the ad-hoc boolean flags <c>IsBubblingUp</c> and <c>IsBubblingDown</c>
///     with a single discriminated enum.
/// </summary>
public enum CommandRouting
{
    /// <summary>
    ///     Direct invocation (programmatic or from this view's own bindings).
    /// </summary>
    Direct,

    /// <summary>
    ///     The command is propagating upward through the SuperView chain.
    /// </summary>
    BubblingUp,

    /// <summary>
    ///     A SuperView is dispatching downward to a specific SubView.
    /// </summary>
    DispatchingDown,

    /// <summary>
    ///     The command is crossing a non-containment boundary via <see cref="CommandBridge"/>.
    /// </summary>
    Bridged,
}
