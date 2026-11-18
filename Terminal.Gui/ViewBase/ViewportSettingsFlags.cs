namespace Terminal.Gui.ViewBase;

/// <summary>
///     Settings for how the <see cref="View.Viewport"/> behaves.
/// </summary>
/// <remarks>
///     See the Layout Deep Dive for more information:
///     <see href="https://tui-cs.github.io/Terminal.Gui/docs/layout.html"/>
/// </remarks>
[Flags]
public enum ViewportSettingsFlags
{
    /// <summary>
    ///     No settings.
    /// </summary>
    None = 0b_0000_0000_0000,

    /// <summary>
    ///     If set, <see cref="View.Viewport"/><c>.X</c> can be set to negative values enabling scrolling beyond the left of
    ///     the
    ///     content area.
    ///     <para>
    ///         When not set, <see cref="View.Viewport"/><c>.X</c> is constrained to positive values.
    ///     </para>
    /// </summary>
    AllowNegativeX = 0b_0000_0000_0001,

    /// <summary>
    ///     If set, <see cref="View.Viewport"/><c>.Y</c> can be set to negative values enabling scrolling beyond the top of the
    ///     content area.
    ///     <para>
    ///         When not set, <see cref="View.Viewport"/><c>.Y</c> is constrained to positive values.
    ///     </para>
    /// </summary>
    AllowNegativeY = 0b_0000_0000_0010,

    /// <summary>
    ///     Combines <see cref="AllowNegativeX"/> and <see cref="AllowNegativeY"/>.
    /// </summary>
    AllowNegativeLocation = AllowNegativeX | AllowNegativeY,

    /// <summary>
    ///     If set, <see cref="View.Viewport"/><c>.X</c> can exceed <c>ContentSize.Width - 1</c>,
    ///     enabling scrolling beyond the right of the content.
    ///     <para>
    ///         When not set, <c>Viewport.X</c> is clamped to keep at least the last column visible.
    ///     </para>
    /// </summary>
    AllowXGreaterThanContentWidth = 0b_0000_0000_0100,

    /// <summary>
    ///     If set, <see cref="View.Viewport"/><c>.Y</c> can exceed <c>ContentSize.Height - 1</c>,
    ///     enabling scrolling beyond the bottom of the content.
    ///     <para>
    ///         When not set, <c>Viewport.Y</c> is clamped to keep at least the last row visible.
    ///     </para>
    /// </summary>
    AllowYGreaterThanContentHeight = 0b_0000_0000_1000,

    /// <summary>
    ///     Combines <see cref="AllowXGreaterThanContentWidth"/> and <see cref="AllowYGreaterThanContentHeight"/>.
    /// </summary>
    AllowLocationGreaterThanContentSize = AllowXGreaterThanContentWidth | AllowYGreaterThanContentHeight,

    /// <summary>
    ///     If set and <see cref="View.Viewport"/><c>.Width</c> is greater than <see cref="View.GetContentSize ()"/>
    ///     <c>.Width</c> <see cref="View.Viewport"/><c>.X</c> can be negative.
    ///     <para>
    ///         When not set, <see cref="View.Viewport"/><c>.X</c> will be constrained to non-negative values when
    ///         <see cref="View.Viewport"/><c>.Width</c> is greater than <see cref="View.GetContentSize ()"/>
    ///         <c>.Width</c>, preventing
    ///         scrolling beyond the left of the Viewport.
    ///     </para>
    ///     <para>
    ///         This can be useful in infinite scrolling scenarios.
    ///     </para>
    /// </summary>
    AllowNegativeXWhenWidthGreaterThanContentWidth = 0b_0000_0001_0000,

    /// <summary>
    ///     If set and <see cref="View.Viewport"/><c>.Height</c> is greater than <see cref="View.GetContentSize ()"/>
    ///     <c>.Height</c> <see cref="View.Viewport"/><c>.Y</c> can be negative.
    ///     <para>
    ///         When not set, <see cref="View.Viewport"/><c>.Y</c> will be constrained to non-negative values when
    ///         <see cref="View.Viewport"/><c>.Height</c> is greater than <see cref="View.GetContentSize ()"/>
    ///         <c>.Height</c>, preventing
    ///         scrolling above the top of the Viewport.
    ///     </para>
    ///     <para>
    ///         This can be useful in infinite scrolling scenarios.
    ///     </para>
    /// </summary>
    AllowNegativeYWhenHeightGreaterThanContentHeight = 0b_0000_0010_0000,

    /// <summary>
    ///     The combination of <see cref="AllowNegativeXWhenWidthGreaterThanContentWidth"/> and
    ///     <see cref="AllowNegativeYWhenHeightGreaterThanContentHeight"/>.
    /// </summary>
    AllowNegativeLocationWhenSizeGreaterThanContentSize = AllowNegativeXWhenWidthGreaterThanContentWidth | AllowNegativeYWhenHeightGreaterThanContentHeight,

    /// <summary>
    ///     If set, <c>Viewport.X + Viewport.Width</c> can exceed <c>ContentSize.Width</c>,
    ///     allowing blank space on the right when scrolling.
    ///     <para>
    ///         When not set (default), <c>Viewport.X</c> is clamped so the content always fills the viewport horizontally.
    ///     </para>
    /// </summary>
    AllowXPlusWidthGreaterThanContentWidth = 0b_0000_0100_0000,

    /// <summary>
    ///     If set, <c>Viewport.Y + Viewport.Height</c> can exceed <c>ContentSize.Height</c>,
    ///     allowing blank space at the bottom when scrolling.
    ///     <para>
    ///         When not set (default), <c>Viewport.Y</c> is clamped so the content always fills the viewport vertically.
    ///     </para>
    /// </summary>
    AllowYPlusHeightGreaterThanContentHeight = 0b_0000_1000_0000,

    /// <summary>
    ///     Combines <see cref="AllowXPlusWidthGreaterThanContentWidth"/> and
    ///     <see cref="AllowYPlusHeightGreaterThanContentHeight"/>.
    ///     Allows blank space to appear when scrolling in either direction.
    /// </summary>
    AllowLocationPlusSizeGreaterThanContentSize = AllowXPlusWidthGreaterThanContentWidth | AllowYPlusHeightGreaterThanContentHeight,

    /// <summary>
    ///     By default, clipping is applied to the <see cref="View.Viewport"/>. Setting this flag will cause clipping to be
    ///     applied to the visible content area.
    /// </summary>
    ClipContentOnly = 0b_0001_0000_0000,

    /// <summary>
    ///     If set <see cref="View.ClearViewport"/> will clear only the portion of the content
    ///     area that is visible within the <see cref="View.Viewport"/>. This is useful for views that have a
    ///     content area larger than the Viewport and want the area outside the content to be visually distinct.
    ///     <see cref="ClipContentOnly"/> must be set for this setting to work (clipping beyond the visible area must be
    ///     disabled).
    /// </summary>
    ClearContentOnly = 0b_0010_0000_0000,

    /// <summary>
    ///     If set the View will be transparent: The <see cref="View.Viewport"/> will not be cleared when the View is drawn and
    ///     the clip region
    ///     will be set to clip the View's <see cref="View.Text"/> and <see cref="View.SubViews"/>.
    ///     <para>
    ///         Only the topmost View in a SubView Hierarchy can be transparent. Any subviews of the topmost transparent view
    ///         will have indeterminate draw behavior.
    ///     </para>
    ///     <para>
    ///         Combine this with <see cref="TransparentMouse"/> to get a view that is both visually transparent and
    ///         transparent to the mouse.
    ///     </para>
    /// </summary>
    Transparent = 0b_0100_0000_0000,

    /// <summary>
    ///     If set the View will be transparent to mouse events: Specifically, any mouse event that occurs over the View that
    ///     is NOT occupied by a SubView
    ///     will not be captured by the View.
    ///     <para>
    ///         Combine this with <see cref="Transparent"/> to get a view that is both visually transparent and transparent to
    ///         the mouse.
    ///     </para>
    /// </summary>
    TransparentMouse = 0b_1000_0000_0000,

    /// <summary>
    ///     If set, the built-in <see cref="View.VerticalScrollBar"/> is enabled with
    ///     <see cref="ScrollBarVisibilityMode.Auto"/> behavior. Clearing this flag disables
    ///     the scrollbar and sets its <see cref="ScrollBar.VisibilityMode"/> to
    ///     <see cref="ScrollBarVisibilityMode.Manual"/> with <see cref="View.Visible"/> = false.
    /// </summary>
    HasVerticalScrollBar = 0b_0001_0000_0000_0000,

    /// <summary>
    ///     If set, the built-in <see cref="View.HorizontalScrollBar"/> is enabled with
    ///     <see cref="ScrollBarVisibilityMode.Auto"/> behavior. Clearing this flag disables
    ///     the scrollbar and sets its <see cref="ScrollBar.VisibilityMode"/> to
    ///     <see cref="ScrollBarVisibilityMode.Manual"/> with <see cref="View.Visible"/> = false.
    /// </summary>
    HasHorizontalScrollBar = 0b_0010_0000_0000_0000,

    /// <summary>
    ///     Combines <see cref="HasVerticalScrollBar"/> and <see cref="HasHorizontalScrollBar"/>.
    /// </summary>
    HasScrollBars = HasVerticalScrollBar | HasHorizontalScrollBar
}
