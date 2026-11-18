namespace Terminal.Gui.ViewBase;

/// <summary>
///     Defines the contract for a replaceable tab header view used by <see cref="BorderView"/>.
///     Implementations must also derive from <see cref="View"/> so they can be added as SubViews.
/// </summary>
public interface ITitleView : IOrientation
{
    /// <summary>
    ///     Gets or sets the tab depth — the number of rows (or columns) the tab header occupies
    ///     on its <see cref="TabSide"/>.
    /// </summary>
    int TabDepth { get; set; }

    /// <summary>
    ///     Gets the measured tab length (in cells) after <see cref="UpdateLayout"/> has auto-sized the title view.
    ///     Returns 0 if <see cref="UpdateLayout"/> has not yet been called.
    /// </summary>
    int MeasuredTabLength { get; set; }

    /// <summary>Gets or sets which side of the content border the tab header sits on.</summary>
    Side TabSide { get; set; }

    /// <summary>
    ///     Updates this title view's frame, border thickness, text, orientation, padding,
    ///     and visibility based on the tab layout context provided by the owning <see cref="BorderView"/>.
    /// </summary>
    void UpdateLayout (in TabLayoutContext context);
}
