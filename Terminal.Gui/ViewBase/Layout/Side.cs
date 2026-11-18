namespace Terminal.Gui.ViewBase;

/// <summary>
///     Indicates the side of a <see cref="View"/>. Used by <see cref="Pos"/> and <see cref="Dim"/> to specify which side
///     of the view to use for layout calculations.
/// </summary>
public enum Side
{
    /// <summary>
    ///     The left (X) side of the view.
    /// </summary>
    Left = 0,

    /// <summary>
    ///     The top (Y) side of the view.
    /// </summary>
    Top = 1,

    /// <summary>
    ///     The right (X + Width) side of the view.
    /// </summary>
    Right = 2,

    /// <summary>
    ///     The bottom (Y + Height) side of the view.
    /// </summary>
    Bottom = 3
}
