namespace Terminal.Gui.Drawing;

/// <summary>
///     Translates colors in an image into a Palette of up to <see cref="MaxColors"/> colors (typically 256).
/// </summary>
public class ColorQuantizer
{
    /// <summary>
    ///     Gets the current colors in the palette based on the last call to
    ///     <see cref="BuildPalette"/>.
    /// </summary>
    public IReadOnlyCollection<Color> Palette { get; private set; } = new List<Color> ();

    private Color [] _palette = [];
    private Dictionary<Color, int> _paletteIndex = [];

    /// <summary>
    ///     Gets or sets the maximum number of colors to put into the <see cref="Palette"/>.
    ///     Defaults to 256 (the maximum for sixel images).
    /// </summary>
    public int MaxColors { get; set; } = 256;

    /// <summary>
    ///     Gets or sets the algorithm used to map novel colors into existing
    ///     palette colors (closest match). Defaults to <see cref="EuclideanColorDistance"/>
    /// </summary>
    public IColorDistance DistanceAlgorithm { get; set; } = new EuclideanColorDistance ();

    /// <summary>
    ///     Gets or sets the algorithm used to build the <see cref="Palette"/>.
    /// </summary>
    public IPaletteBuilder PaletteBuildingAlgorithm { get; set; } = new PopularityPaletteWithThreshold (new EuclideanColorDistance (), 8);

    private readonly Dictionary<Color, int> _nearestColorCache = [];

    /// <summary>
    ///     Builds a <see cref="Palette"/> of colors that most represent the colors used in <paramref name="pixels"/> image.
    ///     This is based on the currently configured <see cref="PaletteBuildingAlgorithm"/>.
    /// </summary>
    /// <param name="pixels"></param>
    public void BuildPalette (Color [,] pixels)
    {
        if (PaletteBuildingAlgorithm is IStaticPaletteBuilder staticPaletteBuilder)
        {
            SetPalette (staticPaletteBuilder.BuildPalette (MaxColors));

            return;
        }

        List<Color> allColors = [];
        int width = pixels.GetLength (0);
        int height = pixels.GetLength (1);

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                allColors.Add (pixels [x, y]);
            }
        }

        _nearestColorCache.Clear ();
        SetPalette (PaletteBuildingAlgorithm.BuildPalette (allColors, MaxColors));
    }

    private void SetPalette (List<Color> palette)
    {
        _nearestColorCache.Clear ();
        Palette = palette;
        _palette = [.. palette];
        _paletteIndex = new (_palette.Length);

        for (int i = 0; i < _palette.Length; i++)
        {
            _paletteIndex.TryAdd (_palette [i], i);
        }
    }

    /// <summary>
    ///     Returns the closest color in <see cref="Palette"/> that matches <paramref name="toTranslate"/>
    ///     based on the color comparison algorithm defined by <see cref="DistanceAlgorithm"/>
    /// </summary>
    /// <param name="toTranslate"></param>
    /// <returns></returns>
    public int GetNearestColor (Color toTranslate)
    {
        if (_paletteIndex.TryGetValue (toTranslate, out int exactIndex))
        {
            return exactIndex;
        }

        if (_nearestColorCache.TryGetValue (toTranslate, out int cachedAnswer))
        {
            return cachedAnswer;
        }

        double minDistance = double.MaxValue;
        int nearestIndex = 0;

        for (int index = 0; index < _palette.Length; index++)
        {
            Color color = _palette [index];
            double distance = DistanceAlgorithm is EuclideanColorDistance
                                  ? CalculateEuclideanDistanceSquared (color, toTranslate)
                                  : DistanceAlgorithm.CalculateDistance (color, toTranslate);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearestIndex = index;
            }
        }

        _nearestColorCache.TryAdd (toTranslate, nearestIndex);

        return nearestIndex;
    }

    private static double CalculateEuclideanDistanceSquared (Color c1, Color c2)
    {
        int rDiff = c1.R - c2.R;
        int gDiff = c1.G - c2.G;
        int bDiff = c1.B - c2.B;

        return rDiff * rDiff + gDiff * gDiff + bDiff * bDiff;
    }
}
