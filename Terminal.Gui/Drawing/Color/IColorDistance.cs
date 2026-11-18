namespace Terminal.Gui.Drawing;

/// <summary>
///     Interface for algorithms that compute the relative distance between pairs of colors.
///     This is used for color matching to a limited palette, such as in Sixel rendering.
/// </summary>
public interface IColorDistance
{
    /// <summary>
    ///     Computes a similarity metric between two <see cref="Color"/> instances.
    ///     A larger value indicates more dissimilar colors, while a smaller value indicates more similar colors.
    ///     The metric is internally consistent for the given algorithm.
    /// </summary>
    /// <param name="c1">The first color.</param>
    /// <param name="c2">The second color.</param>
    /// <returns>A numeric value representing the distance between the two colors.</returns>
    double CalculateDistance (Color c1, Color c2);
}
