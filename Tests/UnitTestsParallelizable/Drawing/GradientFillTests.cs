namespace DrawingTests;

public class GradientFillTests
{
    private readonly Gradient _gradient;

    public GradientFillTests ()
    {
        // Define the colors of the gradient stops
        List<Color> stops = new List<Color>
        {
            new (255, 0), // Red
            new (0, 0, 255) // Blue
        };

        // Define the number of steps between each color
        List<int> steps = new() { 10 }; // 10 steps between Red -> Blue

        _gradient = new (stops, steps);
    }

    [Fact]
    public void TestGradientFillCorners_AtOrigin ()
    {
        var area = new Rectangle (0, 0, 10, 10);
        var gradientFill = new GradientFill (area, _gradient, GradientDirection.Diagonal);

        // Test the corners
        var topLeft = new Point (0, 0);
        var topRight = new Point (area.Width - 1, 0);
        var bottomLeft = new Point (0, area.Height - 1);
        var bottomRight = new Point (area.Width - 1, area.Height - 1);

        Color topLeftColor = gradientFill.GetColor (topLeft);
        Color topRightColor = gradientFill.GetColor (topRight);
        Color bottomLeftColor = gradientFill.GetColor (bottomLeft);
        Color bottomRightColor = gradientFill.GetColor (bottomRight);

        // Expected colors
        var expectedTopLeftColor = new Color (255, 0); // Red
        var expectedBottomRightColor = new Color (0, 0, 255); // Blue

        Assert.Equal (expectedTopLeftColor, topLeftColor);
        Assert.Equal (expectedBottomRightColor, bottomRightColor);
    }

    [Fact]
    public void TestGradientFillCorners_NotAtOrigin ()
    {
        var area = new Rectangle (5, 5, 10, 10);
        var gradientFill = new GradientFill (area, _gradient, GradientDirection.Diagonal);

        // Test the corners
        var topLeft = new Point (5, 5);
        var topRight = new Point (area.Right - 1, 5);
        var bottomLeft = new Point (5, area.Bottom - 1);
        var bottomRight = new Point (area.Right - 1, area.Bottom - 1);

        Color topLeftColor = gradientFill.GetColor (topLeft);
        Color topRightColor = gradientFill.GetColor (topRight);
        Color bottomLeftColor = gradientFill.GetColor (bottomLeft);
        Color bottomRightColor = gradientFill.GetColor (bottomRight);

        // Expected colors
        var expectedTopLeftColor = new Color (255, 0); // Red
        var expectedBottomRightColor = new Color (0, 0, 255); // Blue

        Assert.Equal (expectedTopLeftColor, topLeftColor);
        Assert.Equal (expectedBottomRightColor, bottomRightColor);
    }

    [Fact]
    public void TestGradientFillColorTransition ()
    {
        var area = new Rectangle (0, 0, 10, 10);
        var gradientFill = new GradientFill (area, _gradient, GradientDirection.Diagonal);

        for (var row = 0; row < area.Height; row++)
        {
            var previousRed = 255;
            var previousBlue = 0;

            for (var col = 0; col < area.Width; col++)
            {
                var point = new Point (col, row);
                Color color = gradientFill.GetColor (point);

                // Check if the current color is 'more blue' and 'less red' as it goes right and down
                Assert.True (color.R <= previousRed, $"Failed at ({col}, {row}): {color.R} > {previousRed}");
                Assert.True (color.B >= previousBlue, $"Failed at ({col}, {row}): {color.B} < {previousBlue}");

                // Update the previous color values for the next iteration
                previousRed = color.R;
                previousBlue = color.B;
            }
        }

        for (var col = 0; col < area.Width; col++)
        {
            var previousRed = 255;
            var previousBlue = 0;

            for (var row = 0; row < area.Height; row++)
            {
                var point = new Point (col, row);
                Color color = gradientFill.GetColor (point);

                // Check if the current color is 'more blue' and 'less red' as it goes right and down
                Assert.True (color.R <= previousRed, $"Failed at ({col}, {row}): {color.R} > {previousRed}");
                Assert.True (color.B >= previousBlue, $"Failed at ({col}, {row}): {color.B} < {previousBlue}");

                // Update the previous color values for the next iteration
                previousRed = color.R;
                previousBlue = color.B;
            }
        }
    }
}
