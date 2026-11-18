namespace Terminal.Gui.Drawing;

/// <summary>
///     Simple fast palette building algorithm which uses the frequency that a color is seen
///     to determine whether it will appear in the final palette. Includes a threshold whereby
///     colors will be considered 'the same'. This reduces the chance of underrepresented
///     colors being missed completely.
/// </summary>
public class PopularityPaletteWithThreshold : IPaletteBuilder
{
    private readonly IColorDistance _colorDistance;
    private readonly double _mergeThreshold;

    /// <summary>
    ///     Creates a new instance with the given color grouping parameters.
    /// </summary>
    /// <param name="colorDistance">Determines which different colors can be considered the same.</param>
    /// <param name="mergeThreshold">Threshold for merging two colors together.</param>
    public PopularityPaletteWithThreshold (IColorDistance colorDistance, double mergeThreshold)
    {
        _colorDistance = colorDistance;
        _mergeThreshold = mergeThreshold; // Set the threshold for merging similar colors
    }

    /// <inheritdoc/>
    public List<Color> BuildPalette (List<Color>? colors, int maxColors)
    {
        if (colors is null || colors.Count == 0 || maxColors <= 0)
        {
            return [];
        }

        // Step 1: Build the histogram of colors (count occurrences)
        Dictionary<Color, int> colorHistogram = new ();

        foreach (Color color in colors)
        {
            if (!colorHistogram.TryAdd (color, 1))
            {
                colorHistogram [color]++;
            }
        }

        // If we already have fewer or equal colors than the limit, no need to merge
        if (colorHistogram.Count <= maxColors)
        {
            return colorHistogram.Keys.ToList ();
        }

        // Step 2: Merge similar colors using the color distance threshold
        Dictionary<Color, int> mergedHistogram = MergeSimilarColors (colorHistogram, maxColors);

        // Step 3: Sort the histogram by frequency (most frequent colors first)
        List<Color> sortedColors = mergedHistogram.OrderByDescending (c => c.Value)
                                                  .Take (maxColors) // Keep only the top `maxColors` colors
                                                  .Select (c => c.Key)
                                                  .ToList ();

        return sortedColors;
    }

    /// <summary>
    ///     Merge colors in the histogram if they are within the threshold distance
    /// </summary>
    /// <param name="colorHistogram"></param>
    /// <param name="maxColors"></param>
    /// <returns></returns>
    private Dictionary<Color, int> MergeSimilarColors (Dictionary<Color, int> colorHistogram, int maxColors)
    {
        Dictionary<Color, int> mergedHistogram = new ();

        foreach (KeyValuePair<Color, int> entry in colorHistogram)
        {
            Color currentColor = entry.Key;
            var merged = false;

            // Try to merge the current color with an existing entry in the merged histogram
            foreach (Color mergedEntry in mergedHistogram.Keys.ToList ())
            {
                double distance = _colorDistance.CalculateDistance (currentColor, mergedEntry);

                // If the colors are similar enough (within the threshold), merge them
                if (distance <= _mergeThreshold)
                {
                    mergedHistogram [mergedEntry] += entry.Value; // Add the color frequency to the existing one
                    merged = true;

                    break;
                }
            }

            // If no similar color is found, add the current color as a new entry
            if (!merged)
            {
                mergedHistogram [currentColor] = entry.Value;
            }

            // Early exit if we've reduced the colors to the maxColors limit
            if (mergedHistogram.Count >= maxColors)
            {
                return mergedHistogram;
            }
        }

        return mergedHistogram;
    }
}
