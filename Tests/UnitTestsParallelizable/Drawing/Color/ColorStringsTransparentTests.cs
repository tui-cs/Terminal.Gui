namespace DrawingTests.ColorTests;

public class ColorStringsTransparentTests
{
    [Fact]
    public void GetColorName_ReturnsTransparent_ForTransparentColor ()
    {
        string? name = ColorStrings.GetColorName (Color.None);

        Assert.Equal ("None", name);
    }

    [Fact]
    public void GetColorName_Transparent_TakesPriority_OverAlphaIgnoringMatch ()
    {
        // Color.None has RGB (255, 255, 255) which would match "White" if alpha is ignored.
        // But the Transparent check should come first.
        string? name = ColorStrings.GetColorName (Color.None);

        Assert.Equal ("None", name);
    }

    [Theory]
    [InlineData ("None")]
    [InlineData ("none")]
    [InlineData ("NONE")]
    public void TryParseStandardColorName_ParsesTransparent_CaseInsensitively (string colorName)
    {
        bool result = ColorStrings.TryParseStandardColorName (colorName, out Color color);

        Assert.True (result);
        Assert.Equal (Color.None, color);
    }

    [Theory]
    [InlineData ("None")]
    [InlineData ("none")]
    [InlineData ("NONE")]
    public void TryParseNamedColor_ParsesTransparent_CaseInsensitively (string colorName)
    {
        bool result = ColorStrings.TryParseNamedColor (colorName, out Color color);

        Assert.True (result);
        Assert.Equal (Color.None, color);
    }

    [Fact]
    public void TryParseStandardColorName_Transparent_HasAlphaZero ()
    {
        ColorStrings.TryParseStandardColorName ("None", out Color color);

        Assert.Equal (0, color.A);
    }

    [Fact]
    public void TryParseNamedColor_Transparent_HasAlphaZero ()
    {
        ColorStrings.TryParseNamedColor ("None", out Color color);

        Assert.Equal (0, color.A);
    }

    [Fact]
    public void GetColorName_And_TryParseNamedColor_RoundTrip_Transparent ()
    {
        // Get the name
        string? name = ColorStrings.GetColorName (Color.None);
        Assert.NotNull (name);
        Assert.Equal ("None", name);

        // Parse it back
        bool result = ColorStrings.TryParseNamedColor (name, out Color parsedColor);
        Assert.True (result);
        Assert.Equal (Color.None, parsedColor);
    }

    [Fact]
    public void GetStandardColorNames_DoesNotContain_Transparent ()
    {
        // Transparent is not a W3C standard color — it should NOT appear in the enum-based list
        IEnumerable<string> names = ColorStrings.GetStandardColorNames ();
        string [] namesArray = names.ToArray ();

        Assert.DoesNotContain ("None", namesArray);
    }

    [Fact]
    public void TryParseStandardColorName_Transparent_WorksWithReadOnlySpan ()
    {
        ReadOnlySpan<char> span = "None".AsSpan ();
        bool result = ColorStrings.TryParseStandardColorName (span, out Color color);

        Assert.True (result);
        Assert.Equal (Color.None, color);
    }

    [Fact]
    public void TryParseNamedColor_Transparent_WorksWithReadOnlySpan ()
    {
        ReadOnlySpan<char> span = "none".AsSpan ();
        bool result = ColorStrings.TryParseNamedColor (span, out Color color);

        Assert.True (result);
        Assert.Equal (Color.None, color);
    }
}
