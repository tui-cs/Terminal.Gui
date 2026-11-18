namespace Terminal.Gui.Drawing;

/// <summary>Direction flags for box-drawing character analysis during overlapped compositing.</summary>
[Flags]
public enum LineDirections
{
    /// <summary>
    ///     No lines in any direction.
    /// </summary>
    None = 0,

    /// <summary>
    ///     Line(s) extending up from the cell.
    /// </summary>
    Up = 1,

    /// <summary>
    ///     Line(s) extending down from the cell.
    /// </summary>
    Down = 2,

    /// <summary>
    ///     Line(s) extending left from the cell.
    /// </summary>
    Left = 4,

    /// <summary>
    ///     Line(s) extending right from the cell.
    /// </summary>
    Right = 8
}
