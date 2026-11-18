namespace Terminal.Gui.ViewBase;

/// <summary>
///     Describes how <see cref="View.TabStop"/> behaves. A TabStop is a stop-point for keyboard navigation between Views.
/// </summary>
public enum TabBehavior
{
    /// <summary>
    ///     The View will not be a stop-point for keyboard-based navigation.
    ///     <para>
    ///         This flag has no impact on whether the view can be focused via means other than the keyboard. Use
    ///         <see cref="View.CanFocus"/>
    ///         to control whether a View can focus or not.
    ///     </para>
    /// </summary>
    NoStop = 0,

    /// <summary>
    ///     The View will be a stop-point for keyboard-based navigation across Views (e.g. if the user presses `Tab`).
    /// </summary>
    TabStop = 1,

    /// <summary>
    ///     The View will be a stop-point for keyboard-based navigation across groups. (e.g. if the user presses
    ///     the NextTabGroup key (`Ctrl+PageDown`)).
    /// </summary>
    TabGroup = 2
}
