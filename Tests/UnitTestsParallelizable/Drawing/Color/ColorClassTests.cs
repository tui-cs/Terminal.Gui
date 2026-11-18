
namespace DrawingTests.ColorTests;

public partial class ColorClassTests
{
    [Theory]
    [CombinatorialData]
    public void Argb_Returns_Expected_Value (
        [CombinatorialValues (0, 255)] byte a,
        [CombinatorialRange (0, 255, 51)] byte r,
        [CombinatorialRange (0, 153, 51)] byte g,
        [CombinatorialRange (0, 128, 32)] byte b
    )
    {
        Color color = new (r, g, b, a);

        // Color.Rgba is expected to be a signed int32 in little endian order (a,b,g,r)
        ReadOnlySpan<byte> littleEndianBytes = [b, g, r, a];
        var expectedArgb = BitConverter.ToUInt32 (littleEndianBytes);
        Assert.Equal (expectedArgb, color.Argb);
    }

    [Fact]
    public void Color_IsClosestToNamedColor_ReturnsExpectedValue ()
    {
        // Arrange
        var color1 = new Color (ColorName16.Red);
        var color2 = new Color (197, 15, 31); // Red in RGB

        Assert.True (color1.IsClosestToNamedColor16 (ColorName16.Red));

        Assert.True (color2.IsClosestToNamedColor16 (ColorName16.Red));
    }

    [Theory]
    [CombinatorialData]
    public void Rgba_Returns_Expected_Value (
        [CombinatorialValues (0, 255)] byte a,
        [CombinatorialRange (0, 255, 51)] byte r,
        [CombinatorialRange (0, 153, 51)] byte g,
        [CombinatorialRange (0, 128, 32)] byte b
    )
    {
        Color color = new (r, g, b, a);

        // Color.Rgba is expected to be a signed int32 in little endian order (a,b,g,r)
        ReadOnlySpan<byte> littleEndianBytes = [b, g, r, a];
        var expectedRgba = BitConverter.ToInt32 (littleEndianBytes);
        Assert.Equal (expectedRgba, color.Rgba);
    }
}

public static partial class ColorTestsTheoryDataGenerators
{
    public static TheoryData<Color, ColorName16> FindClosestColor_ReturnsClosestColor ()
    {
        TheoryData<Color, ColorName16> data = [];
        data.Add (new (0, 0), ColorName16.Black);
        data.Add (new (255, 255, 255), ColorName16.White);
        data.Add (new (5, 100, 255), ColorName16.BrightBlue);
        data.Add (new (0, 255), ColorName16.BrightGreen);
        data.Add (new (255, 70, 8), ColorName16.BrightRed);
        data.Add (new (0, 128, 128), ColorName16.Cyan);
        data.Add (new (128, 64, 32), ColorName16.Yellow);

        return data;
    }
}
