namespace Terminal.Gui.Drawing;

/// <summary>
///     Describes an area fill (e.g. solid color or gradient).
/// </summary>
public interface IFill
{
    /// <summary>
    ///     Returns the color that should be used at the given point
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    Color GetColor (Point point);
}
