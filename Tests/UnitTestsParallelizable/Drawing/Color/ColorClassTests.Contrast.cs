namespace DrawingTests.ColorTests;

public partial class ColorClassTests
{
    [Fact]
    public void GetContrastRatio_WhiteOnBlack_Is21 ()
    {
        // Grok - 4.6
        Assert.Equal (21.0, new Color (255, 255, 255).GetContrastRatio (new Color (0, 0, 0)), 2);
    }

    [Fact]
    public void EnsureContrastAgainst_LeavesSufficientPairUnchanged ()
    {
        // Grok - 4.6
        Color foreground = new (232, 232, 232);
        Color background = new (16, 16, 20);

        Assert.Equal (foreground, foreground.EnsureContrastAgainst (background));
    }

    [Fact]
    public void EnsureContrastAgainst_LightThemeCollapse_MovesForegroundAwayFromBackground ()
    {
        // Grok - 4.6
        Color lightGround = new (240, 240, 240);
        Color darkText = new (26, 26, 26);
        Color background = lightGround.GetDimmerColor (0.9);
        Color collapsed = darkText.GetDimmerColor ();
        Color rescued = collapsed.EnsureContrastAgainst (background);

        Assert.True (collapsed.GetContrastRatio (background) < Color.MinimumReadableContrastRatio);
        Assert.True (rescued.GetContrastRatio (background) >= Color.MinimumReadableContrastRatio);
        Assert.NotEqual (new Color (ColorName16.DarkGray), rescued);
        Assert.True (rescued.GetRelativeLuminance () > background.GetRelativeLuminance ());
    }

    [Fact]
    public void EnsureContrastAgainst_NearBlackPair_ReachesReadableRatio ()
    {
        // Grok - 4.6
        Color nearBlackGround = new (16, 16, 20);
        Color nearBlackText = new (47, 47, 54);
        Color background = nearBlackGround.GetDimmerColor (0.9);
        Color collapsed = nearBlackText.GetDimmerColor ();
        Color rescued = collapsed.EnsureContrastAgainst (background);

        Assert.True (rescued.GetContrastRatio (background) >= Color.MinimumReadableContrastRatio);
        Assert.NotEqual (rescued, background);
    }
}
