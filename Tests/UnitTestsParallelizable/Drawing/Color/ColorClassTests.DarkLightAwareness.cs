// Claude - Opus 4.6

namespace DrawingTests.ColorTests;

public partial class ColorClassTests
{
    #region IsDarkColor Tests

    [Fact]
    public void IsDarkColor_Black_ReturnsTrue ()
    {
        Color black = new (0, 0);
        Assert.True (black.IsDarkColor ());
    }

    [Fact]
    public void IsDarkColor_White_ReturnsFalse ()
    {
        Color white = new (255, 255, 255);
        Assert.False (white.IsDarkColor ());
    }

    [Fact]
    public void IsDarkColor_DarkBlue_ReturnsTrue ()
    {
        Color darkBlue = new (0, 0, 128);
        Assert.True (darkBlue.IsDarkColor ());
    }

    [Fact]
    public void IsDarkColor_LightYellow_ReturnsFalse ()
    {
        Color lightYellow = new (255, 255, 200);
        Assert.False (lightYellow.IsDarkColor ());
    }

    [Fact]
    public void IsDarkColor_DarkGray_ReturnsTrue ()
    {
        // RGB(100,100,100) has HSL L≈39, well below 50
        Color darkGray = new (100, 100, 100);
        Assert.True (darkGray.IsDarkColor ());
    }

    [Fact]
    public void IsDarkColor_LightGray_ReturnsFalse ()
    {
        Color lightGray = new (200, 200, 200);
        Assert.False (lightGray.IsDarkColor ());
    }

    #endregion

    #region GetBrighterColor with isDarkBackground Tests

    [Fact]
    public void GetBrighterColor_WithDarkBackground_IncreasesLightness ()
    {
        Color color = new (100, 100, 100);
        Color brighter = color.GetBrighterColor (0.2, true);

        // On dark background, should increase lightness (brighter RGB values)
        Assert.True (brighter.R > color.R || brighter.G > color.G || brighter.B > color.B, "Brightening on dark background should increase lightness");
    }

    [Fact]
    public void GetBrighterColor_WithLightBackground_DecreasesLightness ()
    {
        Color color = new (100, 100, 100);
        Color brighter = color.GetBrighterColor (0.2, false);

        // On light background, should decrease lightness (darker = more visible)
        Assert.True (brighter.R < color.R || brighter.G < color.G || brighter.B < color.B, "Brightening on light background should decrease lightness");
    }

    [Fact]
    public void GetBrighterColor_WithNullBackground_UsesAutoDetect ()
    {
        // Dark color auto-detects to brighten
        Color darkColor = new (50, 50, 50);
        Color brighterAuto = darkColor.GetBrighterColor ();
        Color brighterExplicit = darkColor.GetBrighterColor (0.2, true);

        // Auto-detect on dark color should behave same as explicit isDarkBackground=true
        Assert.Equal (brighterExplicit, brighterAuto);
    }

    [Fact]
    public void GetBrighterColor_LightColor_WithNullBackground_Darkens ()
    {
        // Light color auto-detects to darken
        Color lightColor = new (200, 200, 200);
        Color result = lightColor.GetBrighterColor ();

        Assert.True (result.R < lightColor.R || result.G < lightColor.G || result.B < lightColor.B, "Auto-detect on light color should decrease lightness");
    }

    #endregion

    #region GetDimmerColor with isDarkBackground Tests

    [Fact]
    public void GetDimmerColor_WithDarkBackground_DecreasesLightness ()
    {
        Color color = new (150, 150, 150);
        Color dimmed = color.GetDimmerColor (0.2, true);

        // On dark background, dim should reduce lightness
        Assert.True (dimmed.R < color.R || dimmed.G < color.G || dimmed.B < color.B, "Dimming on dark background should decrease lightness");
    }

    [Fact]
    public void GetDimmerColor_WithLightBackground_IncreasesLightness ()
    {
        Color color = new (100, 100, 100);
        Color dimmed = color.GetDimmerColor (0.2, false);

        // On light background, dim should increase lightness (wash out toward white)
        Assert.True (dimmed.R > color.R || dimmed.G > color.G || dimmed.B > color.B, "Dimming on light background should increase lightness");
    }

    [Fact]
    public void GetDimmerColor_VeryDarkInput_DarkBackground_ReturnsTheColorUnchanged ()
    {
        Color veryDark = new (10, 10, 10);
        Color dimmed = veryDark.GetDimmerColor (0.2, true);

        // A color with no room left to dim keeps what it has: a named gray would be brighter than the
        // color it replaced, and black would take the little contrast the color still carries
        Assert.Equal (veryDark, dimmed);
    }

    [Fact]
    public void GetDimmerColor_VeryLightInput_LightBackground_ReturnsTheColorUnchanged ()
    {
        Color veryLight = new (240, 240, 240);
        Color dimmed = veryLight.GetDimmerColor (0.2, false);

        Assert.Equal (veryLight, dimmed);
    }

    // Text drawn in a color that was dimmed against a ground dimmed the same way has to stay legible:
    // neither may be flattened onto the end of the range, where both become the same color.
    [Fact]
    public void GetDimmerColor_DarkBackground_KeepsDarkTextOffItsGround ()
    {
        Color ground = new (16, 16, 20);
        Color text = new (47, 47, 54);

        Assert.NotEqual (ground.GetDimmerColor (0.2, true), text.GetDimmerColor (0.2, true));
    }

    // A dimmer that brightens is what this guards: on a dark background no input may come back
    // lighter than it went in, and a near-black one used to come back as DarkGray (#767676).
    [Theory]
    [InlineData (0, 0, 0)]
    [InlineData (16, 16, 20)]
    [InlineData (26, 26, 26)]
    [InlineData (47, 47, 54)]
    [InlineData (150, 150, 150)]
    [InlineData (255, 255, 255)]
    public void GetDimmerColor_DarkBackground_NeverBrightens (byte r, byte g, byte b)
    {
        Color color = new (r, g, b);
        Color dimmed = color.GetDimmerColor (0.2, true);

        Assert.True (
                     dimmed.R <= color.R && dimmed.G <= color.G && dimmed.B <= color.B,
                     $"Dimming #{color.R:x2}{color.G:x2}{color.B:x2} returned the brighter #{dimmed.R:x2}{dimmed.G:x2}{dimmed.B:x2}");
    }

    // The same contract in the other direction: dimming toward a light background washes a color out,
    // and a near-white one must not come back darker.
    [Theory]
    [InlineData (255, 255, 255)]
    [InlineData (240, 240, 240)]
    [InlineData (100, 100, 100)]
    [InlineData (0, 0, 0)]
    public void GetDimmerColor_LightBackground_NeverDarkens (byte r, byte g, byte b)
    {
        Color color = new (r, g, b);
        Color dimmed = color.GetDimmerColor (0.2, false);

        Assert.True (
                     dimmed.R >= color.R && dimmed.G >= color.G && dimmed.B >= color.B,
                     $"Dimming #{color.R:x2}{color.G:x2}{color.B:x2} returned the darker #{dimmed.R:x2}{dimmed.G:x2}{dimmed.B:x2}");
    }

    [Fact]
    public void GetDimmerColor_WithNullBackground_ReducesLightness ()
    {
        // null isDarkBackground defaults to reducing lightness (backward compat)
        Color color = new (150, 150, 150);
        Color dimmedNull = color.GetDimmerColor ();
        Color dimmedTrue = color.GetDimmerColor (0.2, true);

        Assert.Equal (dimmedTrue, dimmedNull);
    }

    [Fact]
    public void GetDimmerColor_NoArgs_BackwardCompatible ()
    {
        // No-argument call should behave identically to isDarkBackground=null (which defaults to true)
        Color color = new (150, 150, 150);
        Color dimmedNoArgs = color.GetDimmerColor ();
        Color dimmedNull = color.GetDimmerColor ();

        Assert.Equal (dimmedNull, dimmedNoArgs);
    }

    #endregion
}
