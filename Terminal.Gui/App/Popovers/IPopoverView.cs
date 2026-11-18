namespace Terminal.Gui.App;

/// <summary>
///     Interface for popover views that combines <see cref="IPopover"/> with View-level operations.
///     Eliminates the need for casting <see cref="IPopover"/> to <see cref="View"/> at caller sites.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="IPopoverView"/> exposes the minimal set of View members needed by the popover infrastructure.
///         Use <see cref="Visible"/> and View's CWP events (<see cref="View.VisibleChanging"/>, <see cref="View.VisibleChanged"/>)
///         instead of a separate IsOpen property.
///     </para>
///     <para>
///         See also: <seealso cref="IPopover"/>
///     </para>
/// </remarks>
public interface IPopoverView : IPopover
{
    /// <summary>
    ///     Gets or sets whether the popover is visible.
    ///     Use this instead of a separate IsOpen property.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When set to <see langword="true"/>, the popover should be shown.
    ///         When set to <see langword="false"/>, the popover should be hidden.
    ///     </para>
    ///     <para>
    ///         Subscribe to <see cref="View.VisibleChanging"/> (cancellable) and <see cref="View.VisibleChanged"/>
    ///         for popover visibility change notifications.
    ///     </para>
    /// </remarks>
    bool Visible { get; set; }

    /// <summary>
    ///     Gets or sets whether the popover is enabled.
    /// </summary>
    bool Enabled { get; set; }

    /// <summary>
    ///     Gets or sets the anchor positioning function. When the popover is shown, this function
    ///     is called to determine the anchor rectangle for positioning.
    /// </summary>
    Func<Rectangle?>? Anchor { get; set; }

    /// <summary>
    ///     Gets or sets a weak reference to the target view that owns this popover.
    ///     Used for command bubbling and automatic hiding when the target loses focus.
    /// </summary>
    WeakReference<View>? Target { get; set; }

    /// <summary>
    ///     Makes the popover visible at the specified position or anchor.
    /// </summary>
    /// <param name="idealScreenPosition">
    ///     The ideal screen position for the popover. If <see langword="null"/>, uses the current mouse position.
    /// </param>
    /// <param name="anchor">
    ///     The anchor rectangle to position relative to. If <see langword="null"/>, uses the <see cref="Anchor"/> property.
    /// </param>
    void MakeVisible (Point? idealScreenPosition = null, Rectangle? anchor = null);

    /// <summary>
    ///     Marks the view as needing to be redrawn.
    /// </summary>
    void SetNeedsDraw ();

    /// <summary>
    ///     Marks the view as needing layout recalculation.
    /// </summary>
    void SetNeedsLayout ();

    /// <summary>
    ///     Advances focus in the specified direction.
    /// </summary>
    /// <param name="direction">The direction to advance focus.</param>
    /// <param name="behavior">The tab behavior to use. If <see langword="null"/>, uses default behavior.</param>
    /// <returns><see langword="true"/> if focus was advanced; otherwise, <see langword="false"/>.</returns>
    bool AdvanceFocus (NavigationDirection direction, TabBehavior? behavior);
}
