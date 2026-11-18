namespace DrawingTests;

public class GradientTests
{
    // Static method to provide all enum values
    public static IEnumerable<object []> GradientDirectionValues ()
    {
        return typeof (GradientDirection).GetEnumValues ()
                                         .Cast<GradientDirection> ()
                                         .Select (direction => new object [] { direction });
    }

    [Theory]
    [MemberData (nameof (GradientDirectionValues))]
    public void GradientIsInclusive_2_by_2 (GradientDirection direction)
    {
        // Define the colors of the gradient stops
        List<Color> stops = new()
        {
            new (255, 0), // Red
            new (0, 0, 255) // Blue
        };

        // Define the number of steps between each color
        List<int> steps = new() { 10 }; // 10 steps between Red -> Blue

        var g = new Gradient (stops, steps);
        Assert.Equal (4, g.BuildCoordinateColorMapping (1, 1, direction).Count);
    }

    [Theory]
    [MemberData (nameof (GradientDirectionValues))]
    public void GradientIsInclusive_1_by_1 (GradientDirection direction)
    {
        // Define the colors of the gradient stops
        List<Color> stops = new()
        {
            new (255, 0), // Red
            new (0, 0, 255) // Blue
        };

        // Define the number of steps between each color
        List<int> steps = new() { 10 }; // 10 steps between Red -> Blue

        var g = new Gradient (stops, steps);

        // Note that maxRow and maxCol are inclusive so this results in 1x1 area i.e. a single cell. 
        KeyValuePair<Point, Color> c = Assert.Single (g.BuildCoordinateColorMapping (0, 0, direction));
        Assert.Equal (c.Key, new (0, 0));
        Assert.Equal (c.Value, new (0, 0, 255));
    }

    [Fact]
    public void SingleColorStop ()
    {
        List<Color> stops = new() { new (255, 0) }; // Red
        List<int> steps = new ();

        var g = new Gradient (stops, steps);
        Assert.All (g.Spectrum, color => Assert.Equal (new (255, 0), color));
    }

    [Fact]
    public void LoopingGradient_CorrectColors ()
    {
        List<Color> stops = new()
        {
            new (255, 0), // Red
            new (0, 0, 255) // Blue
        };

        List<int> steps = new() { 10 };

        var g = new Gradient (stops, steps, true);
        Assert.Equal (new (255, 0), g.Spectrum.First ());
        Assert.Equal (new (255, 0), g.Spectrum.Last ());
    }

    [Fact]
    public void DifferentStepSizes ()
    {
        List<Color> stops = new List<Color>
        {
            new (255, 0), // Red
            new (0, 255), // Green
            new (0, 0, 255) // Blue
        };

        List<int> steps = new() { 5, 15 }; // Different steps

        var g = new Gradient (stops, steps);
        Assert.Equal (22, g.Spectrum.Count);
    }

    [Fact]
    public void FractionOutOfRange_ThrowsException ()
    {
        List<Color> stops = new()
        {
            new (255, 0), // Red
            new (0, 0, 255) // Blue
        };

        List<int> steps = new() { 10 };

        var g = new Gradient (stops, steps);

        Assert.Throws<ArgumentOutOfRangeException> (() => g.GetColorAtFraction (-0.1));
        Assert.Throws<ArgumentOutOfRangeException> (() => g.GetColorAtFraction (1.1));
    }

    [Fact]
    public void NaNFraction_ReturnsLastColor ()
    {
        List<Color> stops = new()
        {
            new (255, 0), // Red
            new (0, 0, 255) // Blue
        };

        List<int> steps = new() { 10 };

        var g = new Gradient (stops, steps);
        Assert.Equal (new (0, 0, 255), g.GetColorAtFraction (double.NaN));
    }

    [Fact]
    public void Constructor_SingleStepProvided_ReplicatesForAllPairs ()
    {
        List<Color> stops = new List<Color>
        {
            new (255, 0), // Red
            new (0, 255), // Green
            new (0, 0, 255) // Blue
        };

        List<int> singleStep = new() { 5 }; // Single step provided
        var gradient = new Gradient (stops, singleStep);

        Assert.NotNull (gradient.Spectrum);
        Assert.Equal (12, gradient.Spectrum.Count); // 5 steps Red -> Green + 5 steps Green -> Blue + 2 end colors
    }

    [Fact]
    public void Constructor_InvalidStepsLength_ThrowsArgumentException ()
    {
        List<Color> stops = new()
        {
            new (255, 0), // Red
            new (0, 0, 255) // Blue
        };

        List<int> invalidSteps = new() { 5, 5 }; // Invalid length (N-1 expected)
        Assert.Throws<ArgumentException> (() => new Gradient (stops, invalidSteps));
    }

    [Fact]
    public void Constructor_ValidStepsLength_DoesNotThrow ()
    {
        List<Color> stops = new List<Color>
        {
            new (255, 0), // Red
            new (0, 255), // Green
            new (0, 0, 255) // Blue
        };

        List<int> validSteps = new() { 5, 5 }; // Valid length (N-1)
        var gradient = new Gradient (stops, validSteps);

        Assert.NotNull (gradient.Spectrum);
        Assert.Equal (12, gradient.Spectrum.Count); // 5 steps Red -> Green + 5 steps Green -> Blue + 2 end colors
    }
}
