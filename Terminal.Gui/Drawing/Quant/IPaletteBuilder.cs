namespace Terminal.Gui.Drawing;

/// <summary>
///     Builds a palette of a given size for a given set of input colors.
/// </summary>
public interface IPaletteBuilder
{
    /// <summary>
    ///     Reduce the number of <paramref name="colors"/> to <paramref name="maxColors"/> (or less)
    ///     using an appropriate selection algorithm.
    /// </summary>
    /// <param name="colors">
    ///     Color of every pixel in the image. Contains duplication in order
    ///     to support algorithms that weigh how common a color is.
    /// </param>
    /// <param name="maxColors">The maximum number of colours that should be represented.</param>
    /// <returns></returns>
    List<Color> BuildPalette (List<Color> colors, int maxColors);
}
