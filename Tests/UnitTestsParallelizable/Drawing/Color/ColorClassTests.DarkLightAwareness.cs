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
    public void GetDimmerColor_VeryDarkInput_DarkBackground_DoesNotReturnDarkGray ()
    {
        // Grok - 4.6
        Color veryDark = new (10, 10, 10);
        Color dimmed = veryDark.GetDimmerColor (0.2, true);

        Assert.NotEqual (new Color (ColorName16.DarkGray), dimmed);
        Assert.True (RelativeLuminance (dimmed) <= RelativeLuminance (veryDark));
    }

    [Fact]
    public void GetDimmerColor_VeryLightInput_LightBackground_DoesNotReturnGray ()
    {
        // Grok - 4.6
        Color veryLight = new (240, 240, 240);
        Color dimmed = veryLight.GetDimmerColor (0.2, false);

        Assert.NotEqual (new Color (ColorName16.Gray), dimmed);
        Assert.True (RelativeLuminance (dimmed) >= RelativeLuminance (veryLight));
    }

    [Fact]
    public void GetDimmerColor_Black_StaysBlack ()
    {
        // Grok - 4.6
        Color black = new (0, 0, 0);
        Color dimmed = black.GetDimmerColor (0.2, true);

        Assert.Equal (black, dimmed);
    }

    [Fact]
    public void GetDimmerColor_White_OnLightBackground_StaysWhite ()
    {
        // Grok - 4.6
        Color white = new (255, 255, 255);
        Color dimmed = white.GetDimmerColor (0.2, false);

        Assert.Equal (white, dimmed);
    }

    [Fact]
    public void GetDimmerColor_MidGray_ShadowStep_StillDarkens ()
    {
        // Grok - 4.6
        // ShadowView dims the ground with 0.9. A mid-gray has room to go darker; returning
        // it unchanged (because 0.9 would pass black) is how a transparent shadow stops
        // darkening its background.
        Color midGray = new (128, 128, 128);
        Color dimmed = midGray.GetDimmerColor (0.9, true);

        Assert.True (
                     RelativeLuminance (dimmed) < RelativeLuminance (midGray),
                     $"Dimming #{midGray.R:x2}{midGray.G:x2}{midGray.B:x2} by 0.9 returned #{dimmed.R:x2}{dimmed.G:x2}{dimmed.B:x2}");
    }

    [Fact]
    public void GetDimmerColor_DarkBackground_KeepsDarkTextOffItsGround ()
    {
        // Grok - 4.6
        Color ground = new (16, 16, 20);
        Color text = new (47, 47, 54);

        Assert.NotEqual (ground.GetDimmerColor (0.2, true), text.GetDimmerColor (0.2, true));
        Assert.NotEqual (ground.GetDimmerColor (0.9, true), text.GetDimmerColor (0.9, true));
    }

    [Theory]
    [InlineData (0, 0, 0)]
    [InlineData (16, 16, 20)]
    [InlineData (26, 26, 26)]
    [InlineData (47, 47, 54)]
    [InlineData (128, 128, 128)]
    [InlineData (150, 150, 150)]
    [InlineData (255, 255, 255)]
    [InlineData (10, 20, 40)]
    public void GetDimmerColor_DarkBackground_NeverBrightens (byte r, byte g, byte b)
    {
        // Grok - 4.6
        Color color = new (r, g, b);
        Color dimmed = color.GetDimmerColor (0.2, true);

        Assert.True (
                     RelativeLuminance (dimmed) <= RelativeLuminance (color),
                     $"Dimming #{color.R:x2}{color.G:x2}{color.B:x2} returned the brighter #{dimmed.R:x2}{dimmed.G:x2}{dimmed.B:x2}");
    }

    [Theory]
    [InlineData (255, 255, 255)]
    [InlineData (240, 240, 240)]
    [InlineData (128, 128, 128)]
    [InlineData (100, 100, 100)]
    [InlineData (0, 0, 0)]
    [InlineData (10, 20, 40)]
    public void GetDimmerColor_LightBackground_NeverDarkens (byte r, byte g, byte b)
    {
        // Grok - 4.6
        Color color = new (r, g, b);
        Color dimmed = color.GetDimmerColor (0.2, false);

        Assert.True (
                     RelativeLuminance (dimmed) >= RelativeLuminance (color),
                     $"Dimming #{color.R:x2}{color.G:x2}{color.B:x2} returned the darker #{dimmed.R:x2}{dimmed.G:x2}{dimmed.B:x2}");
    }

    [Theory]
    [InlineData (16, 16, 20, 0.9)]
    [InlineData (128, 128, 128, 0.9)]
    [InlineData (200, 200, 200, 0.9)]
    [InlineData (16, 16, 20, 0.2)]
    [InlineData (47, 47, 54, 0.2)]
    public void GetDimmerColor_DarkBackground_MakesProgressWhenThereIsRoom (byte r, byte g, byte b, double dimAmount)
    {
        // Grok - 4.6
        Color color = new (r, g, b);
        Color dimmed = color.GetDimmerColor (dimAmount, true);

        Assert.True (
                     RelativeLuminance (dimmed) < RelativeLuminance (color),
                     $"Dimming #{color.R:x2}{color.G:x2}{color.B:x2} by {dimAmount} made no progress: #{dimmed.R:x2}{dimmed.G:x2}{dimmed.B:x2}");
    }

    [Theory]
    [InlineData (0.1, true)]
    [InlineData (0.2, true)]
    [InlineData (0.5, true)]
    [InlineData (0.9, true)]
    [InlineData (0.1, false)]
    [InlineData (0.2, false)]
    [InlineData (0.5, false)]
    [InlineData (0.9, false)]
    public void GetDimmerColor_PreservesLightnessOrder (double dimAmount, bool isDarkBackground)
    {
        // Grok - 4.6
        // Scale remaining room: L' = L*(1-a) toward black, L' = 1-(1-L)*(1-a) toward white.
        // Both are non-decreasing in L, so a lighter input cannot dim darker than its neighbour.
        double previous = double.NegativeInfinity;

        for (int i = 0; i <= 100; i++)
        {
            double lightness = i / 100.0;
            byte channel = (byte)Math.Clamp ((int)Math.Round (lightness * 255.0), 0, 255);
            Color input = new (channel, channel, channel);
            Color dimmed = input.GetDimmerColor (dimAmount, isDarkBackground);
            double dimmedLuminance = RelativeLuminance (dimmed);

            Assert.True (
                         dimmedLuminance >= previous,
                         $"Amount {dimAmount}, dark={isDarkBackground}: L={lightness:0.00} dimmed to #{dimmed.R:x2}{dimmed.G:x2}{dimmed.B:x2}, darker than the previous step");

            previous = dimmedLuminance;
        }
    }

    [Theory]
    [InlineData (16, 16, 20, true)]
    [InlineData (128, 128, 128, true)]
    [InlineData (200, 200, 200, true)]
    [InlineData (16, 16, 20, false)]
    [InlineData (128, 128, 128, false)]
    [InlineData (200, 200, 200, false)]
    public void GetDimmerColor_PreservesAmountOrder (byte r, byte g, byte b, bool isDarkBackground)
    {
        // Grok - 4.6
        // A larger dimAmount must not reverse the move: toward dark, more dimming is never
        // lighter; toward light, more dimming is never darker.
        Color input = new (r, g, b);
        double previous = isDarkBackground ? double.PositiveInfinity : double.NegativeInfinity;

        for (int i = 0; i <= 100; i++)
        {
            double amount = i / 100.0;
            Color dimmed = input.GetDimmerColor (amount, isDarkBackground);
            double dimmedLuminance = RelativeLuminance (dimmed);

            if (isDarkBackground)
            {
                Assert.True (
                             dimmedLuminance <= previous,
                             $"Dark dim of #{input.R:x2}{input.G:x2}{input.B:x2} at {amount:0.00} was #{dimmed.R:x2}{dimmed.G:x2}{dimmed.B:x2}, lighter than a smaller amount");
                previous = dimmedLuminance;

                continue;
            }

            Assert.True (
                         dimmedLuminance >= previous,
                         $"Light dim of #{input.R:x2}{input.G:x2}{input.B:x2} at {amount:0.00} was #{dimmed.R:x2}{dimmed.G:x2}{dimmed.B:x2}, darker than a smaller amount");
            previous = dimmedLuminance;
        }
    }

    [Fact]
    public void GetDimmerColor_NearBlackInputs_DoNotCollapseToBlack ()
    {
        // Grok - 4.6
        // Truncating L through a 0-100 byte crushed #030303 and #101010 onto black.
        Color text = new (3, 3, 3);
        Color ground = new (16, 16, 16);
        Color dimmedText = text.GetDimmerColor (0.2, true);
        Color dimmedGround = ground.GetDimmerColor (0.9, true);
        Color black = new (0, 0, 0);

        Assert.NotEqual (black, dimmedText);
        Assert.NotEqual (black, dimmedGround);
        Assert.NotEqual (text.GetDimmerColor (0.2, true), ground.GetDimmerColor (0.2, true));
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

    private static double RelativeLuminance (Color color) => 0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B;
}
