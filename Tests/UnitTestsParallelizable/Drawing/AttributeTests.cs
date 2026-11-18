// Alias Console to MockConsole so we don't accidentally use Console

using UnitTests;

namespace DrawingTests;

public class AttributeTests : TestDriverBase
{
    [Fact]
    public void Constructor_ParsesNamedColorsAndStyle ()
    {
        var attr = new Attribute ("Red", "Black", "Bold,Underline");
        Assert.Equal (Color.Parse ("Red"), attr.Foreground);
        Assert.Equal (Color.Parse ("Black"), attr.Background);
        Assert.True (attr.Style.HasFlag (TextStyle.Bold));
        Assert.True (attr.Style.HasFlag (TextStyle.Underline));
    }

    [Fact]
    public void Constructor_ParsesHexColors ()
    {
        var attr = new Attribute ("#FF0000", "#000000", "Italic");
        Assert.Equal (Color.Parse ("#FF0000"), attr.Foreground);
        Assert.Equal (Color.Parse ("#000000"), attr.Background);
        Assert.Equal (TextStyle.Italic, attr.Style);
    }

    [Fact]
    public void Constructor_ParsesRgbColors ()
    {
        var attr = new Attribute ("rgb(0,255,0)", "rgb(0,0,255)", "Faint");
        Assert.Equal (Color.Parse ("rgb(0,255,0)"), attr.Foreground);
        Assert.Equal (Color.Parse ("rgb(0,0,255)"), attr.Background);
        Assert.Equal (TextStyle.Faint, attr.Style);
    }

    [Fact]
    public void Constructor_DefaultsToNoneStyle_WhenStyleIsNullOrEmpty ()
    {
        var attr1 = new Attribute ("White", "Black");
        var attr2 = new Attribute ("White", "Black");
        var attr3 = new Attribute ("White", "Black", "");
        Assert.Equal (TextStyle.None, attr1.Style);
        Assert.Equal (TextStyle.None, attr2.Style);
        Assert.Equal (TextStyle.None, attr3.Style);
    }

    [Fact]
    public void Constructor_DefaultsToNoneStyle_WhenStyleIsInvalid ()
    {
        var attr = new Attribute ("White", "Black", "NotAStyle");
        Assert.Equal (TextStyle.None, attr.Style);
    }

    [Fact]
    public void ColorAndColorNamesConstructor ()
    {
        // Arrange & Act
        var foregroundColor = new Color (0, 0, 255);
        var backgroundColorName = ColorName16.Black;
        var attribute = new Attribute (foregroundColor, backgroundColorName);

        // Assert
        Assert.Equal (foregroundColor, attribute.Foreground);
        Assert.Equal (new (backgroundColorName), attribute.Background);
    }

    [Fact]
    public void ColorConstructor ()
    {
        // Arrange & Act
        var foregroundColor = new Color (0, 0, 255);
        var backgroundColor = new Color (255, 255, 255);
        var attribute = new Attribute (foregroundColor, backgroundColor);

        // Assert
        Assert.Equal (foregroundColor, attribute.Foreground);
        Assert.Equal (backgroundColor, attribute.Background);
    }

    [Fact]
    public void ColorNamesAndColorConstructor ()
    {
        // Arrange & Act
        var foregroundColorName = ColorName16.BrightYellow;
        var backgroundColor = new Color (128, 128, 128);
        var attribute = new Attribute (foregroundColorName, backgroundColor);

        // Assert
        Assert.Equal (new (foregroundColorName), attribute.Foreground);
        Assert.Equal (backgroundColor, attribute.Background);
    }

    [Fact]
    public void ColorNamesConstructor ()
    {
        // Arrange & Act
        var attribute = new Attribute (ColorName16.Blue);

        // Assert
        Assert.Equal (new (Color.Blue), attribute.Foreground);
        Assert.Equal (new (Color.Blue), attribute.Background);
    }

    [Fact]
    public void Constructors_Construct ()
    {
        IDriver driver = CreateTestDriver ();

        // Test parameterless constructor
        var attr = new Attribute ();

        Assert.Equal (new (Color.White), attr.Foreground);
        Assert.Equal (new (Color.Black), attr.Background);

        // Test foreground, background
        var fg = new Color ();
        fg = new (Color.Red);

        var bg = new Color ();
        bg = new (Color.Blue);

        attr = new (fg, bg);

        //Assert.True (attr.Initialized);
        //Assert.True (attr.HasValidColors);
        Assert.Equal (fg, attr.Foreground);
        Assert.Equal (bg, attr.Background);

        attr = new (fg);

        //Assert.True (attr.Initialized);
        //Assert.True (attr.HasValidColors);
        Assert.Equal (fg, attr.Foreground);
        Assert.Equal (fg, attr.Background);

        attr = new (bg);

        //Assert.True (attr.Initialized);
        //Assert.True (attr.HasValidColors);
        Assert.Equal (bg, attr.Foreground);
        Assert.Equal (bg, attr.Background);

        driver.Dispose ();
    }

    [Fact]
    public void DefaultConstructor ()
    {
        // Arrange & Act
        var attribute = new Attribute ();

        // Assert
        Assert.Equal (new (Color.White), attribute.Foreground);
        Assert.Equal (new (Color.Black), attribute.Background);
    }

    [Fact]
    public void Equality_IncludesStyle ()
    {
        var attr1 = new Attribute (Color.Red, Color.Black, TextStyle.Bold);
        var attr2 = new Attribute (Color.Red, Color.Black, TextStyle.Bold);
        var attr3 = new Attribute (Color.Red, Color.Black, TextStyle.Underline);

        Assert.Equal (attr1, attr2);
        Assert.NotEqual (attr1, attr3);
    }

    [Fact]
    public void EqualityOperator_ShouldReturnFalseForDifferentAttributes ()
    {
        // Arrange
        var attribute1 = new Attribute (Color.Red, Color.Black);
        var attribute2 = new Attribute (Color.Blue, Color.Black);

        // Act & Assert
        Assert.False (attribute1 == attribute2);
    }

    [Fact]
    public void EqualityOperator_ShouldReturnTrueForEqualAttributes ()
    {
        // Arrange
        var attribute1 = new Attribute (Color.Red, Color.Black);
        var attribute2 = new Attribute (Color.Red, Color.Black);

        // Act & Assert
        Assert.True (attribute1 == attribute2);
    }

    [Fact]
    public void Equals_Initialized ()
    {
        var attr1 = new Attribute (Color.Red, Color.Green);
        var attr2 = new Attribute (Color.Red, Color.Green);

        Assert.True (attr1.Equals (attr2));
        Assert.True (attr2.Equals (attr1));
    }

    [Fact]
    public void Equals_NotInitialized ()
    {
        var attr1 = new Attribute (Color.Red, Color.Green);
        var attr2 = new Attribute (Color.Red, Color.Green);

        Assert.True (attr1.Equals (attr2));
        Assert.True (attr2.Equals (attr1));
    }

    [Fact]
    public void GetHashCode_ConsistentWithEquals ()
    {
        var attr1 = new Attribute (Color.Red, Color.Black, TextStyle.Bold);
        var attr2 = new Attribute (Color.Red, Color.Black, TextStyle.Bold);

        Assert.Equal (attr1, attr2);
        Assert.Equal (attr1.GetHashCode (), attr2.GetHashCode ());
    }

    [Fact]
    public void InequalityOperator_ShouldReturnFalseForEqualAttributes ()
    {
        // Arrange
        var attribute1 = new Attribute (Color.Red, Color.Black);
        var attribute2 = new Attribute (Color.Red, Color.Black);

        // Act & Assert
        Assert.False (attribute1 != attribute2);
    }

    [Fact]
    public void InequalityOperator_ShouldReturnTrueForDifferentAttributes ()
    {
        // Arrange
        var attribute1 = new Attribute (Color.Red, Color.Black);
        var attribute2 = new Attribute (Color.Blue, Color.Black);

        // Act & Assert
        Assert.True (attribute1 != attribute2);
    }

    [Fact]
    public void Is_Value_Type ()
    {
        // prove that Color is a value type
        Assert.True (typeof (Attribute).IsValueType);
    }

    [Fact]
    public void List_RoundTrip_EqualityHolds ()
    {
        List<Attribute> list1 = [new (Color.Red, Color.Black, TextStyle.Bold)];
        List<Attribute> list2 = new (list1);

        Assert.Equal (list1, list2);
    }

    [Fact]
    public void Make_Creates ()
    {
        IDriver driver = CreateTestDriver ();

        var fg = new Color ();
        fg = new (Color.Red);

        var bg = new Color ();
        bg = new (Color.Blue);

        var attr = new Attribute (fg, bg);

        //Assert.True (attr.Initialized);
        Assert.Equal (fg, attr.Foreground);
        Assert.Equal (bg, attr.Background);

        driver.Dispose ();
    }

    [Fact]
    public void Make_Creates_NoDriver ()
    {
        var fg = new Color ();
        fg = new (Color.Red);

        var bg = new Color ();
        bg = new (Color.Blue);

        var attr = new Attribute (fg, bg);

        //Assert.False (attr.Initialized);
        Assert.Equal (fg, attr.Foreground);
        Assert.Equal (bg, attr.Background);
    }

    [Fact]
    public void Make_SetsNotInitialized_NoDriver ()
    {
        var fg = new Color ();
        fg = new (Color.Red);

        var bg = new Color ();
        bg = new (Color.Blue);

        var a = new Attribute (fg, bg);

        //Assert.False (a.Initialized);
    }

    [Fact]
    public void MakeColorAndColor_ForegroundAndBackgroundShouldMatchInput ()
    {
        // Arrange
        var foregroundColor = new Color (0, 0, 255);
        var backgroundColor = new Color (255, 255, 255);

        // Act
        var attribute = new Attribute (foregroundColor, backgroundColor);

        // Assert
        Assert.Equal (foregroundColor, attribute.Foreground);
        Assert.Equal (backgroundColor, attribute.Background);
    }

    [Fact]
    public void MakeColorAndColorNames_ForegroundAndBackgroundShouldMatchInput ()
    {
        // Arrange
        var foregroundColor = new Color (255, 0);
        var backgroundColorName = ColorName16.White;

        // Act
        var attribute = new Attribute (foregroundColor, backgroundColorName);

        // Assert
        Assert.Equal (foregroundColor, attribute.Foreground);
        Assert.Equal (new (backgroundColorName), attribute.Background);
    }

    [Fact]
    public void MakeColorNamesAndColor_ForegroundAndBackgroundShouldMatchInput ()
    {
        // Arrange
        var foregroundColorName = ColorName16.Green;
        var backgroundColor = new Color (128, 128, 128);

        // Act
        var attribute = new Attribute (foregroundColorName, backgroundColor);

        // Assert
        Assert.Equal (new (foregroundColorName), attribute.Foreground);
        Assert.Equal (backgroundColor, attribute.Background);
    }

    [Fact]
    public void MakeColorNamesAndColorNames_ForegroundAndBackgroundShouldMatchInput ()
    {
        // Arrange
        var foregroundColorName = ColorName16.BrightYellow;
        var backgroundColorName = ColorName16.Black;

        // Act
        var attribute = new Attribute (foregroundColorName, backgroundColorName);

        // Assert
        Assert.Equal (new (foregroundColorName), attribute.Foreground);
        Assert.Equal (new (backgroundColorName), attribute.Background);
    }

    [Fact]
    public void NotEquals_Initialized ()
    {
        var attr1 = new Attribute (Color.Red, Color.Green);
        var attr2 = new Attribute (Color.Green, Color.Red);

        Assert.False (attr1.Equals (attr2));
        Assert.False (attr2.Equals (attr1));
    }

    [Fact]
    public void NotEquals_NotInitialized ()
    {
        var attr1 = new Attribute (Color.Red, Color.Green);
        var attr2 = new Attribute (Color.Green, Color.Red);

        Assert.False (attr1.Equals (attr2));
        Assert.False (attr2.Equals (attr1));
    }

    [Fact]
    public void ToString_Formats_Correctly ()
    {
        // Arrange
        var foregroundColor = new Color (0, 0, 255);
        var backgroundColor = new Color (255, 255, 255);
        var expectedString = $"[{foregroundColor},{backgroundColor},None]";

        // Act
        var attribute = new Attribute (foregroundColor, backgroundColor);
        var attributeString = attribute.ToString ();

        // Assert
        Assert.Equal (expectedString, attributeString);
    }

    [Theory]
    [InlineData (TextStyle.Bold, "Bold")]
    [InlineData (TextStyle.Bold | TextStyle.Underline, "Bold, Underline")]
    [InlineData (TextStyle.None, "None")]
    public void ToString_IncludesStyle (TextStyle style, string expectedStyleString)
    {
        var attr = new Attribute (Color.Red, Color.Black, style);
        var result = attr.ToString ();

        Assert.Contains (expectedStyleString, result);
    }

    [Fact]
    public void ToString_ShouldFailComparison_IfDifferentInstancesSameContent ()
    {
        var original = new Attribute (Color.White, Color.White);
        var clone = new Attribute (Color.White, Color.White);

        // These print the same
        Assert.Equal (original.ToString (), clone.ToString ());

        // But this will fail if anything differs under the hood
        Assert.Equal (original, clone); // Should pass — record struct
    }
}
