namespace DrawingTests;

public class ColorQuantizerTests
{
    // Copilot - GPT-5.5
    [Fact]
    public void GetNearestColor_ReturnsExactPaletteColorIndex ()
    {
        ColorQuantizer quantizer = new ();
        Color [,] pixels = new Color [2, 1];
        pixels [0, 0] = new (255, 0);
        pixels [1, 0] = new (0, 255);

        quantizer.BuildPalette (pixels);

        Assert.Equal (0, quantizer.GetNearestColor (pixels [0, 0]));
        Assert.Equal (1, quantizer.GetNearestColor (pixels [1, 0]));
    }

    // Copilot - GPT-5.5
    [Fact]
    public void GetNearestColor_CachesNearestColorForNonPaletteColor ()
    {
        CountingColorDistance distance = new ();
        ColorQuantizer quantizer = new ()
        {
            DistanceAlgorithm = distance
        };
        Color [,] pixels = new Color [2, 1];
        pixels [0, 0] = new (255, 0);
        pixels [1, 0] = new (0, 255);

        quantizer.BuildPalette (pixels);

        Color nearRed = new (254, 0);
        int first = quantizer.GetNearestColor (nearRed);
        int callsAfterFirst = distance.CallCount;
        int second = quantizer.GetNearestColor (nearRed);

        Assert.Equal (first, second);
        Assert.Equal (callsAfterFirst, distance.CallCount);
    }

    private sealed class CountingColorDistance : IColorDistance
    {
        public int CallCount { get; private set; }

        public double CalculateDistance (Color c1, Color c2)
        {
            CallCount++;
            int rDiff = c1.R - c2.R;
            int gDiff = c1.G - c2.G;
            int bDiff = c1.B - c2.B;

            return Math.Sqrt (rDiff * rDiff + gDiff * gDiff + bDiff * bDiff);
        }
    }
}
