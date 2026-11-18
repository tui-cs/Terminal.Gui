using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrawingTests;

public class FillPairTests
{
    [Fact]
    public void GetAttribute_ReturnsCorrectColors ()
    {
        // Arrange
        var foregroundColor = new Color (100, 150, 200);
        var backgroundColor = new Color (50, 75, 100);
        var foregroundFill = new SolidFill (foregroundColor);
        var backgroundFill = new SolidFill (backgroundColor);

        var fillPair = new FillPair (foregroundFill, backgroundFill);

        // Act
        Attribute resultAttribute = fillPair.GetAttribute (new (0, 0));

        // Assert
        Assert.Equal (foregroundColor, resultAttribute.Foreground);
        Assert.Equal (backgroundColor, resultAttribute.Background);
    }
}
