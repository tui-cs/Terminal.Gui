namespace DrawingTests;

public class SolidFillTests
{
    [Fact]
    public void GetColor_ReturnsCorrectColor ()
    {
        // Arrange
        var expectedColor = new Color (100, 150, 200);
        var solidFill = new SolidFill (expectedColor);

        // Act
        Color resultColor = solidFill.GetColor (new (0, 0));

        // Assert
        Assert.Equal (expectedColor, resultColor);
    }

    [Theory]
    [InlineData (0, 0)]
    [InlineData (1, 1)]
    [InlineData (-1, -1)]
    [InlineData (100, 100)]
    [InlineData (-100, -100)]
    public void GetColor_ReturnsSameColorForDifferentPoints (int x, int y)
    {
        // Arrange
        var expectedColor = new Color (50, 100, 150);
        var solidFill = new SolidFill (expectedColor);

        // Act
        Color resultColor = solidFill.GetColor (new (x, y));

        // Assert
        Assert.Equal (expectedColor, resultColor);
    }
}
