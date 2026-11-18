namespace Terminal.Gui.Drawing;

/// <summary>
///     <see cref="IFill"/> implementation that uses a solid color for all points
/// </summary>
public class SolidFill : IFill
{
    private readonly Color _color;

    /// <summary>
    ///     Creates a new instance of the <see cref="SolidFill"/> class which will return
    ///     the provided <paramref name="color"/> regardless of which point is requested.
    /// </summary>
    /// <param name="color"></param>
    public SolidFill (Color color) { _color = color; }

    /// <summary>
    ///     Returns the color this instance was constructed with regardless of
    ///     which <paramref name="point"/> is being colored.
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public Color GetColor (Point point) { return _color; }
}
