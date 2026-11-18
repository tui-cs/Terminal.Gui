namespace DrawingTests.ColorTests;
public class ColorStringsTests
{
    [Fact]
    public void GetColorName_ReturnsNameForStandardColor ()
    {
        Color red = new (255, 0);
        string? name = ColorStrings.GetColorName (red);

        Assert.Equal ("Red", name);
    }

    [Fact]
    public void GetColorName_ReturnsNullForNonStandardColor ()
    {
        Color custom = new (1, 2, 3);
        string? name = ColorStrings.GetColorName (custom);

        Assert.Null (name);
    }

    [Fact]
    public void GetColorName_IgnoresAlphaChannel ()
    {
        Color opaqueRed = new (255, 0, 0, 255);
        Color transparentRed = new (255, 0, 0, 128);
        Color fullyTransparentRed = new (255, 0, 0, 0);

        string? name1 = ColorStrings.GetColorName (opaqueRed);
        string? name2 = ColorStrings.GetColorName (transparentRed);
        string? name3 = ColorStrings.GetColorName (fullyTransparentRed);

        Assert.Equal ("Red", name1);
        Assert.Equal ("Red", name2);
        Assert.Equal ("Red", name3);
    }

    [Theory]
    [InlineData (240, 248, 255, "AliceBlue")]
    [InlineData (0, 255, 255, "Aqua")]
    [InlineData (0, 0, 0, "Black")]
    [InlineData (0, 0, 255, "Blue")]
    [InlineData (0, 128, 0, "Green")]
    [InlineData (255, 0, 0, "Red")]
    [InlineData (255, 255, 255, "White")]
    [InlineData (255, 255, 0, "Yellow")]
    public void GetColorName_ReturnsCorrectNameForKnownColors (int r, int g, int b, string expectedName)
    {
        Color color = new (r, g, b);
        string? name = ColorStrings.GetColorName (color);

        Assert.Equal (expectedName, name);
    }

    [Fact]
    public void GetStandardColorNames_ReturnsNonEmptyCollection ()
    {
        IEnumerable<string> names = ColorStrings.GetStandardColorNames ();

        Assert.NotNull (names);
        Assert.NotEmpty (names);
    }

    [Fact]
    public void GetStandardColorNames_ReturnsAlphabeticallySortedNames ()
    {
        IEnumerable<string> names = ColorStrings.GetStandardColorNames ();
        string [] namesArray = names.ToArray ();
        string [] sortedNames = namesArray.OrderBy (n => n).ToArray ();

        Assert.Equal (sortedNames, namesArray);
    }

    [Fact]
    public void GetStandardColorNames_ContainsKnownColors ()
    {
        IEnumerable<string> names = ColorStrings.GetStandardColorNames ();
        string [] namesArray = names.ToArray ();

        Assert.Contains ("Red", namesArray);
        Assert.Contains ("Green", namesArray);
        Assert.Contains ("Blue", namesArray);
        Assert.Contains ("White", namesArray);
        Assert.Contains ("Black", namesArray);
        Assert.Contains ("AliceBlue", namesArray);
        Assert.Contains ("Tomato", namesArray);
    }

    [Fact]
    public void GetStandardColorNames_ContainsAllStandardColorEnumValues ()
    {
        IEnumerable<string> names = ColorStrings.GetStandardColorNames ();
        string [] namesArray = names.ToArray ();
        string [] enumNames = Enum.GetNames<StandardColor> ();

        // All enum names should be in the returned collection
        foreach (string enumName in enumNames)
        {
            Assert.Contains (enumName, namesArray);
        }

        // The counts should match
        Assert.Equal (enumNames.Length, namesArray.Length);
    }

    [Theory]
    [InlineData ("Red")]
    [InlineData ("red")]
    [InlineData ("RED")]
    [InlineData ("Green")]
    [InlineData ("green")]
    [InlineData ("Blue")]
    [InlineData ("AliceBlue")]
    [InlineData ("aliceblue")]
    [InlineData ("ALICEBLUE")]
    public void TryParseStandardColorName_ParsesValidColorNamesCaseInsensitively (string colorName)
    {
        bool result = ColorStrings.TryParseStandardColorName (colorName, out Color color);

        Assert.True (result);
        Assert.NotEqual (default (Color), color);
    }

    [Theory]
    [InlineData ("Red", 255, 0, 0)]
    [InlineData ("Green", 0, 128, 0)]
    [InlineData ("Blue", 0, 0, 255)]
    [InlineData ("White", 255, 255, 255)]
    [InlineData ("Black", 0, 0, 0)]
    [InlineData ("AliceBlue", 240, 248, 255)]
    [InlineData ("Tomato", 255, 99, 71)]
    public void TryParseStandardColorName_ParsesCorrectRgbValues (string colorName, int expectedR, int expectedG, int expectedB)
    {
        bool result = ColorStrings.TryParseStandardColorName (colorName, out Color color);

        Assert.True (result);
        Assert.Equal (expectedR, color.R);
        Assert.Equal (expectedG, color.G);
        Assert.Equal (expectedB, color.B);
    }

    [Theory]
    [InlineData ("#FF0000", 255, 0, 0)]
    [InlineData ("#00FF00", 0, 255, 0)]
    [InlineData ("#0000FF", 0, 0, 255)]
    [InlineData ("#FFFFFF", 255, 255, 255)]
    [InlineData ("#000000", 0, 0, 0)]
    [InlineData ("#F0F8FF", 240, 248, 255)]
    public void TryParseStandardColorName_ParsesHexColorFormat (string hexColor, int expectedR, int expectedG, int expectedB)
    {
        bool result = ColorStrings.TryParseStandardColorName (hexColor, out Color color);

        Assert.True (result);
        Assert.Equal (expectedR, color.R);
        Assert.Equal (expectedG, color.G);
        Assert.Equal (expectedB, color.B);
    }

    [Theory]
    [InlineData ("#ff0000")]
    [InlineData ("#FF0000")]
    [InlineData ("#Ff0000")]
    public void TryParseStandardColorName_ParsesHexColorCaseInsensitively (string hexColor)
    {
        bool result = ColorStrings.TryParseStandardColorName (hexColor, out Color color);

        Assert.True (result);
        Assert.Equal (255, color.R);
        Assert.Equal (0, color.G);
        Assert.Equal (0, color.B);
    }

    [Theory]
    [InlineData ("")]
    [InlineData ("NotAColor")]
    [InlineData ("Invalid")]
    [InlineData ("123")]
    [InlineData ("#FF")]
    [InlineData ("#FFFF")]
    [InlineData ("#FFFFFFFF")]
    [InlineData ("FF0000")]
    public void TryParseStandardColorName_ReturnsFalseForInvalidInput (string invalidInput)
    {
        bool result = ColorStrings.TryParseStandardColorName (invalidInput, out Color color);

        Assert.False (result);
        Assert.Equal (default (Color), color);
    }

    [Fact]
    public void TryParseStandardColorName_SetsAlphaToFullyOpaque ()
    {
        bool result = ColorStrings.TryParseStandardColorName ("Red", out Color color);

        Assert.True (result);
        Assert.Equal (255, color.A);
    }

    [Fact]
    public void TryParseStandardColorName_WorksWithReadOnlySpan ()
    {
        ReadOnlySpan<char> span = "Red".AsSpan ();
        bool result = ColorStrings.TryParseStandardColorName (span, out Color color);

        Assert.True (result);
        Assert.Equal (255, color.R);
        Assert.Equal (0, color.G);
        Assert.Equal (0, color.B);
    }

    [Theory]
    [InlineData ("Red")]
    [InlineData ("Green")]
    [InlineData ("Blue")]
    [InlineData ("AliceBlue")]
    [InlineData ("#FF0000")]
    public void TryParseNamedColor_ParsesValidColorNames (string colorName)
    {
        bool result = ColorStrings.TryParseNamedColor (colorName, out Color color);

        Assert.True (result);
        Assert.NotEqual (default (Color), color);
    }

    [Theory]
    [InlineData ("Red", 255, 0, 0)]
    [InlineData ("Green", 0, 128, 0)]
    [InlineData ("Blue", 0, 0, 255)]
    [InlineData ("#FF0000", 255, 0, 0)]
    [InlineData ("#00FF00", 0, 255, 0)]
    public void TryParseNamedColor_ParsesCorrectRgbValues (string colorName, int expectedR, int expectedG, int expectedB)
    {
        bool result = ColorStrings.TryParseNamedColor (colorName, out Color color);

        Assert.True (result);
        Assert.Equal (expectedR, color.R);
        Assert.Equal (expectedG, color.G);
        Assert.Equal (expectedB, color.B);
    }

    [Theory]
    [InlineData ("")]
    [InlineData ("NotAColor")]
    [InlineData ("Invalid")]
    [InlineData ("#ZZ0000")]
    public void TryParseNamedColor_ReturnsFalseForInvalidInput (string invalidInput)
    {
        bool result = ColorStrings.TryParseNamedColor (invalidInput, out Color color);

        Assert.False (result);
        Assert.Equal (default (Color), color);
    }

    [Fact]
    public void TryParseNamedColor_WorksWithReadOnlySpan ()
    {
        ReadOnlySpan<char> span = "Blue".AsSpan ();
        bool result = ColorStrings.TryParseNamedColor (span, out Color color);

        Assert.True (result);
        Assert.Equal (0, color.R);
        Assert.Equal (0, color.G);
        Assert.Equal (255, color.B);
    }

    [Theory]
    [InlineData (nameof (StandardColor.Aqua), nameof (StandardColor.Cyan))]
    [InlineData (nameof (StandardColor.Fuchsia), nameof (StandardColor.Magenta))]
    public void TryParseNamedColor_HandlesColorAliases (string name1, string name2)
    {
        bool result1 = ColorStrings.TryParseNamedColor (name1, out Color color1);
        bool result2 = ColorStrings.TryParseNamedColor (name2, out Color color2);

        Assert.True (result1);
        Assert.True (result2);
        Assert.Equal (color1.R, color2.R);
        Assert.Equal (color1.G, color2.G);
        Assert.Equal (color1.B, color2.B);
    }

    [Fact]
    public void GetColorName_And_TryParseNamedColor_RoundTrip ()
    {
        // Get a standard color name
        Color originalColor = new (255, 0);
        string? colorName = ColorStrings.GetColorName (originalColor);

        Assert.NotNull (colorName);

        // Parse it back
        bool result = ColorStrings.TryParseNamedColor (colorName, out Color parsedColor);

        Assert.True (result);
        Assert.Equal (originalColor.R, parsedColor.R);
        Assert.Equal (originalColor.G, parsedColor.G);
        Assert.Equal (originalColor.B, parsedColor.B);
    }

    [Fact]
    public void GetStandardColorNames_And_TryParseStandardColorName_RoundTrip ()
    {
        // Get all standard color names
        IEnumerable<string> names = ColorStrings.GetStandardColorNames ();

        // Each name should parse successfully
        foreach (string name in names)
        {
            bool result = ColorStrings.TryParseStandardColorName (name, out Color color);
            Assert.True (result, $"Failed to parse standard color name: {name}");

            // And should get the same name back (for non-aliases)
            string? retrievedName = ColorStrings.GetColorName (color);
            Assert.NotNull (retrievedName);

            // The retrieved name should be one of the valid names for this color
            // (could be different if there are aliases)
            Assert.True (
                         ColorStrings.TryParseStandardColorName (retrievedName, out Color retrievedColor),
                         $"Retrieved name '{retrievedName}' should be parseable"
                        );
            Assert.Equal (color.R, retrievedColor.R);
            Assert.Equal (color.G, retrievedColor.G);
            Assert.Equal (color.B, retrievedColor.B);
        }
    }
}
