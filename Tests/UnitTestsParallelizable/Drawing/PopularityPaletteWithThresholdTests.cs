namespace DrawingTests;

public class PopularityPaletteWithThresholdTests
{
    private readonly IColorDistance _colorDistance;

    public PopularityPaletteWithThresholdTests () { _colorDistance = new EuclideanColorDistance (); }

    [Fact]
    public void BuildPalette_EmptyColorList_ReturnsEmptyPalette ()
    {
        // Arrange
        var paletteBuilder = new PopularityPaletteWithThreshold (_colorDistance, 50);
        List<Color> colors = new ();

        // Act
        List<Color> result = paletteBuilder.BuildPalette (colors, 256);

        // Assert
        Assert.Empty (result);
    }

    [Fact]
    public void BuildPalette_MaxColorsZero_ReturnsEmptyPalette ()
    {
        // Arrange
        var paletteBuilder = new PopularityPaletteWithThreshold (_colorDistance, 50);
        List<Color> colors = new () { new (255, 0), new (0, 255) };

        // Act
        List<Color> result = paletteBuilder.BuildPalette (colors, 0);

        // Assert
        Assert.Empty (result);
    }

    [Fact]
    public void BuildPalette_SingleColorList_ReturnsSingleColor ()
    {
        // Arrange
        var paletteBuilder = new PopularityPaletteWithThreshold (_colorDistance, 50);
        List<Color> colors = new () { new (255, 0), new (255, 0) };

        // Act
        List<Color> result = paletteBuilder.BuildPalette (colors, 256);

        // Assert
        Assert.Single (result);
        Assert.Equal (new (255, 0), result [0]);
    }

    [Fact]
    public void BuildPalette_ThresholdMergesSimilarColors_WhenColorCountExceedsMax ()
    {
        // Arrange
        var paletteBuilder = new PopularityPaletteWithThreshold (_colorDistance, 50); // Set merge threshold to 50

        List<Color> colors = new()
        {
            new (255, 0), // Red
            new (250, 0), // Very close to Red
            new (0, 255), // Green
            new (0, 250) // Very close to Green
        };

        // Act
        List<Color> result = paletteBuilder.BuildPalette (colors, 2); // Limit palette to 2 colors

        // Assert
        Assert.Equal (2, result.Count); // Red and Green should be merged with their close colors
        Assert.Contains (new (255, 0), result); // Red (or close to Red) should be present
        Assert.Contains (new (0, 255), result); // Green (or close to Green) should be present
    }

    [Fact]
    public void BuildPalette_NoMergingIfColorCountIsWithinMax ()
    {
        // Arrange
        var paletteBuilder = new PopularityPaletteWithThreshold (_colorDistance, 50);

        List<Color> colors = new ()
        {
            new (255, 0), // Red
            new (0, 255) // Green
        };

        // Act
        List<Color> result = paletteBuilder.BuildPalette (colors, 256); // Set maxColors higher than the number of unique colors

        // Assert
        Assert.Equal (2, result.Count); // No merging should occur since we are under the limit
        Assert.Contains (new (255, 0), result);
        Assert.Contains (new (0, 255), result);
    }

    [Fact]
    public void BuildPalette_MergesUntilMaxColorsReached ()
    {
        // Arrange
        var paletteBuilder = new PopularityPaletteWithThreshold (_colorDistance, 50);

        List<Color> colors = new()
        {
            new (255, 0), // Red
            new (254, 0), // Close to Red
            new (0, 255), // Green
            new (0, 254) // Close to Green
        };

        // Act
        List<Color> result = paletteBuilder.BuildPalette (colors, 2); // Set maxColors to 2

        // Assert
        Assert.Equal (2, result.Count); // Only two colors should be in the final palette
        Assert.Contains (new (255, 0), result);
        Assert.Contains (new (0, 255), result);
    }
}
