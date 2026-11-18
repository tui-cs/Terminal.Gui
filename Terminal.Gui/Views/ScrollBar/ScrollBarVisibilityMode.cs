namespace Terminal.Gui.Views;

/// <summary>
///     Controls how a <see cref="ScrollBar"/> manages its own <see cref="View.Visible"/> state.
/// </summary>
public enum ScrollBarVisibilityMode
{
    /// <summary>
    ///     The scrollbar does not manage its own visibility. The developer controls
    ///     <see cref="View.Visible"/> directly to show or hide the scrollbar. This is the default mode.
    /// </summary>
    Manual = 0,

    /// <summary>
    ///     The scrollbar is automatically shown when <see cref="ScrollBar.ScrollableContentSize"/>
    ///     exceeds <see cref="ScrollBar.VisibleContentSize"/>, and hidden otherwise.
    /// </summary>
    Auto,

    /// <summary>
    ///     The scrollbar is always visible regardless of content size.
    /// </summary>
    Always,

    /// <summary>
    ///     The scrollbar is always hidden regardless of content size or <see cref="ViewportSettingsFlags"/>.
    /// </summary>
    None
}
