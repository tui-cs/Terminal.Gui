namespace Terminal.Gui.Drawing;

/// <summary>
///     Describes a pair of <see cref="IFill"/> which cooperate in creating
///     <see cref="Attribute"/>. One gives foreground color while other gives background.
/// </summary>
public class FillPair
{
    /// <summary>
    ///     Creates a new instance using the provided fills for foreground and background
    ///     color when assembling <see cref="Attribute"/>.
    /// </summary>
    /// <param name="fore"></param>
    /// <param name="back"></param>
    public FillPair (IFill fore, IFill back)
    {
        Foreground = fore;
        Background = back;
    }

    /// <summary>
    ///     The fill which provides point based foreground color.
    /// </summary>
    public IFill Foreground { get; init; }

    /// <summary>
    ///     The fill which provides point based background color.
    /// </summary>
    public IFill Background { get; init; }

    /// <summary>
    ///     Returns the color pair (foreground+background) to use when rendering
    ///     a rune at the given <paramref name="point"/>.
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public Attribute GetAttribute (Point point)
    {
        return new (Foreground.GetColor (point), Background.GetColor (point));
    }
}
