namespace Terminal.Gui.ViewBase;

/// <summary>
///     Identifies the different arrangement buttons that can be displayed on a border during arrange mode.
/// </summary>
public enum ArrangeButtons
{
    /// <summary>
    ///     Button for moving the view (typically displayed in top-left corner).
    /// </summary>
    Move,

    /// <summary>
    ///     Button for resizing from all directions (typically displayed in bottom-right corner).
    /// </summary>
    AllSize,

    /// <summary>
    ///     Button for resizing from the left edge.
    /// </summary>
    LeftSize,

    /// <summary>
    ///     Button for resizing from the right edge.
    /// </summary>
    RightSize,

    /// <summary>
    ///     Button for resizing from the top edge.
    /// </summary>
    TopSize,

    /// <summary>
    ///     Button for resizing from the bottom edge.
    /// </summary>
    BottomSize
}
