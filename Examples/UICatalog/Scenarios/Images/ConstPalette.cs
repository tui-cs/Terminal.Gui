namespace UICatalog.Scenarios;

internal class ConstPalette (Color [] palette) : IStaticPaletteBuilder
{
    private readonly List<Color> _palette = palette.ToList ();

    /// <inheritdoc/>
    public List<Color> BuildPalette (List<Color> colors, int maxColors) => BuildPalette (maxColors);

    /// <inheritdoc/>
    public List<Color> BuildPalette (int maxColors) => [.. _palette.Take (maxColors)];
}
