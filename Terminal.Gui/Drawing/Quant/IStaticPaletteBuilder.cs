namespace Terminal.Gui.Drawing;

/// <summary>
///     Builds a palette without needing to inspect every source pixel.
/// </summary>
public interface IStaticPaletteBuilder : IPaletteBuilder
{
    /// <summary>
    ///     Builds the static palette, limited to <paramref name="maxColors"/> colors.
    /// </summary>
    /// <param name="maxColors">The maximum number of colors that should be represented.</param>
    /// <returns>The static palette colors.</returns>
    List<Color> BuildPalette (int maxColors);
}
