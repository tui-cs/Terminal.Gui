#nullable enable

namespace DrawingTests;

public class SixelSupportResultTests
{
    [Fact]
    public void Defaults_AreCorrect ()
    {
        // Arrange & Act
        var result = new SixelSupportResult ();

        // Assert
        Assert.False (result.IsSupported);
        Assert.Equal (10, result.Resolution.Width);
        Assert.Equal (20, result.Resolution.Height);
        Assert.Equal (256, result.MaxPaletteColors);
        Assert.False (result.SupportsTransparency);
    }

    [Fact]
    public void Properties_CanBeModified ()
    {
        // Arrange
        var result = new SixelSupportResult ();

        // Act
        result.IsSupported = true;
        result.Resolution = new Size (24, 48);
        result.MaxPaletteColors = 16;
        result.SupportsTransparency = true;

        // Assert
        Assert.True (result.IsSupported);
        Assert.Equal (24, result.Resolution.Width);
        Assert.Equal (48, result.Resolution.Height);
        Assert.Equal (16, result.MaxPaletteColors);
        Assert.True (result.SupportsTransparency);
    }

    [Fact]
    public void Resolution_IsValueType_CopyDoesNotAffectOriginal ()
    {
        // Arrange
        var result = new SixelSupportResult ();
        Size original = result.Resolution;

        // Act
        // Mutate a local copy and ensure original remains unchanged
        Size copy = original;
        copy.Width = 123;
        copy.Height = 456;

        // Assert
        Assert.Equal (10, result.Resolution.Width);
        Assert.Equal (20, result.Resolution.Height);
        Assert.Equal (10, original.Width);
        Assert.Equal (20, original.Height);
        Assert.Equal (123, copy.Width);
        Assert.Equal (456, copy.Height);
    }
}
