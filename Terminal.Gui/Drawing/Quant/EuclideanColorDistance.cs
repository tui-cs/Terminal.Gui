namespace Terminal.Gui.Drawing;

/// <summary>
///     <para>
///         Calculates the distance between two colors using Euclidean distance in 3D RGB space.
///         This measures the straight-line distance between the two points representing the colors.
///     </para>
///     <para>
///         Euclidean distance in RGB space is calculated as:
///     </para>
///     <code>
///      √((R2 - R1)² + (G2 - G1)² + (B2 - B1)²)
///  </code>
///     <remarks>Values vary from 0 to ~441.67 linearly</remarks>
///     <remarks>
///         This distance metric is commonly used for comparing colors in RGB space, though
///         it doesn't account for perceptual differences in color.
///     </remarks>
/// </summary>
public class EuclideanColorDistance : IColorDistance
{
    /// <inheritdoc/>
    public double CalculateDistance (Color c1, Color c2)
    {
        int rDiff = c1.R - c2.R;
        int gDiff = c1.G - c2.G;
        int bDiff = c1.B - c2.B;

        return Math.Sqrt (rDiff * rDiff + gDiff * gDiff + bDiff * bDiff);
    }
}
