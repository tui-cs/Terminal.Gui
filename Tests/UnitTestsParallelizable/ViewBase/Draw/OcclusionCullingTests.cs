using UnitTests;

namespace ViewBaseTests.Draw;

// Claude - Opus 4.8 (1M context)
/// <summary>
///     Issue #5360: <see cref="View.DrawSubViews"/> culls fully-occluded Overlapped opaque siblings.
///     When a lower-Z Overlapped opaque sibling is entirely covered by the higher-Z opaque peers
///     already drawn, every cell it would draw (including its own LineCanvas) is clipped away, so
///     skipping its <see cref="View.Draw(DrawContext?)"/> is output-neutral.
/// </summary>
/// <remarks>
///     Culling is intentionally conservative — it never applies to transparent views, shadowed
///     views, partially-covered views, non-Overlapped views, or views that contribute to the
///     SuperView's LineCanvas compositing (<see cref="View.SuperViewRendersLineCanvas"/>, e.g. Tabs
///     pages). Each test below pins one of those guards.
/// </remarks>
public class OcclusionCullingTests : TestDriverBase
{
    /// <summary>
    ///     Builds a SuperView (no adornments) sized to fill the driver, plus a stack of full-fill
    ///     Overlapped subviews added back-to-front (the last view added is highest-Z / drawn first).
    /// </summary>
    private static View BuildOverlappedStack (IDriver driver, params View [] backToFront)
    {
        View super = new () { Driver = driver, Width = 20, Height = 10 };

        foreach (View view in backToFront)
        {
            view.Width = Dim.Fill ();
            view.Height = Dim.Fill ();
            view.Arrangement = ViewArrangement.Overlapped;
            super.Add (view);
        }

        super.Layout ();

        return super;
    }

    /// <summary>
    ///     GIVEN two full-fill Overlapped opaque siblings
    ///     WHEN the SuperView draws
    ///     THEN the higher-Z (top) sibling draws and the fully-covered lower-Z (bottom) sibling is
    ///     culled (its Draw is never invoked).
    /// </summary>
    [Fact]
    public void FullyOccluded_OpaqueOverlappedSibling_IsCulled ()
    {
        IDriver driver = CreateTestDriver (40, 20);

        View bottom = new ();
        View top = new ();
        View super = BuildOverlappedStack (driver, bottom, top);

        var bottomDraws = 0;
        var topDraws = 0;
        bottom.DrawComplete += (_, _) => bottomDraws++;
        top.DrawComplete += (_, _) => topDraws++;

        super.SetNeedsDraw ();
        super.Draw ();

        Assert.True (topDraws >= 1, $"Top (occluder) should draw (got {topDraws}).");
        Assert.Equal (0, bottomDraws);

        super.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     GIVEN a fully-occluded sibling that was culled
    ///     WHEN the SuperView draws
    ///     THEN the culled sibling's NeedsDraw is cleared (mirroring a drawn-but-clipped pass) so it
    ///     does not keep the SuperView perpetually dirty.
    /// </summary>
    [Fact]
    public void CulledSibling_HasNeedsDrawCleared ()
    {
        IDriver driver = CreateTestDriver (40, 20);

        View bottom = new ();
        View top = new ();
        View super = BuildOverlappedStack (driver, bottom, top);

        super.SetNeedsDraw ();
        Assert.True (bottom.NeedsDraw);

        super.Draw ();

        Assert.False (bottom.NeedsDraw);
        Assert.False (super.SubViewNeedsDraw);

        super.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     GIVEN a lower-Z sibling that is only partially covered by the higher-Z sibling
    ///     WHEN the SuperView draws
    ///     THEN the partially-covered sibling still draws.
    /// </summary>
    [Fact]
    public void PartiallyCovered_OverlappedSibling_StillDraws ()
    {
        IDriver driver = CreateTestDriver (40, 20);

        // Bottom fills the whole SuperView; top covers only the left half, so the bottom's right
        // half remains visible and must not be culled.
        View bottom = new () { Width = Dim.Fill (), Height = Dim.Fill (), Arrangement = ViewArrangement.Overlapped };
        View top = new () { Width = 10, Height = 10, Arrangement = ViewArrangement.Overlapped };

        View super = new () { Driver = driver, Width = 20, Height = 10 };
        super.Add (bottom);
        super.Add (top);
        super.Layout ();

        var bottomDraws = 0;
        bottom.DrawComplete += (_, _) => bottomDraws++;

        super.SetNeedsDraw ();
        super.Draw ();

        Assert.True (bottomDraws >= 1, $"Partially-covered sibling must still draw (got {bottomDraws}).");

        super.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     GIVEN a higher-Z sibling that is transparent (does not opaquely cover the area)
    ///     WHEN the SuperView draws
    ///     THEN the lower-Z sibling beneath it is NOT culled.
    /// </summary>
    [Fact]
    public void TransparentOccluder_DoesNotCullSiblingBeneath ()
    {
        IDriver driver = CreateTestDriver (40, 20);

        View bottom = new ();
        View top = new () { ViewportSettings = ViewportSettingsFlags.Transparent };
        View super = BuildOverlappedStack (driver, bottom, top);

        var bottomDraws = 0;
        bottom.DrawComplete += (_, _) => bottomDraws++;

        super.SetNeedsDraw ();
        super.Draw ();

        Assert.True (bottomDraws >= 1, $"Sibling beneath a transparent peer must draw (got {bottomDraws}).");

        super.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     GIVEN a lower-Z sibling that is itself transparent
    ///     WHEN it is fully covered by a higher-Z opaque sibling
    ///     THEN it is NOT culled (culling is restricted to opaque candidates to avoid disturbing
    ///     transparency / TransparentMouse hit-test caching).
    /// </summary>
    [Fact]
    public void TransparentCandidate_IsNotCulled ()
    {
        IDriver driver = CreateTestDriver (40, 20);

        View bottom = new () { ViewportSettings = ViewportSettingsFlags.Transparent };
        View top = new ();
        View super = BuildOverlappedStack (driver, bottom, top);

        var bottomDraws = 0;
        bottom.DrawComplete += (_, _) => bottomDraws++;

        super.SetNeedsDraw ();
        super.Draw ();

        Assert.True (bottomDraws >= 1, $"Transparent candidate must not be culled (got {bottomDraws}).");

        super.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     GIVEN a lower-Z sibling with a Margin shadow
    ///     WHEN it is fully covered by a higher-Z opaque sibling
    ///     THEN it is NOT culled — a shadowed Margin is transparent and is drawn in a separate
    ///     second pass, so it must continue to participate in drawing.
    /// </summary>
    [Fact]
    public void ShadowedCandidate_IsNotCulled ()
    {
        IDriver driver = CreateTestDriver (40, 20);

        View bottom = new () { ShadowStyle = ShadowStyles.Opaque };
        View top = new ();
        View super = BuildOverlappedStack (driver, bottom, top);

        var bottomDraws = 0;
        bottom.DrawComplete += (_, _) => bottomDraws++;

        super.SetNeedsDraw ();
        super.Draw ();

        Assert.True (bottomDraws >= 1, $"Shadowed candidate must not be culled (got {bottomDraws}).");

        super.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     GIVEN a lower-Z sibling that renders its border via the SuperView's LineCanvas
    ///     (<see cref="View.SuperViewRendersLineCanvas"/> — the Tabs page case)
    ///     WHEN it is fully covered by a higher-Z opaque sibling
    ///     THEN it is NOT culled, because its LineCanvas still contributes to the SuperView's
    ///     painters'-algorithm composition (dropping it could erase tab-header line art).
    /// </summary>
    [Fact]
    public void SuperViewRendersLineCanvasCandidate_IsNotCulled ()
    {
        IDriver driver = CreateTestDriver (40, 20);

        View bottom = new () { SuperViewRendersLineCanvas = true };
        View top = new ();
        View super = BuildOverlappedStack (driver, bottom, top);

        var bottomDraws = 0;
        bottom.DrawComplete += (_, _) => bottomDraws++;

        super.SetNeedsDraw ();
        super.Draw ();

        Assert.True (bottomDraws >= 1, $"SuperViewRendersLineCanvas candidate must not be culled (got {bottomDraws}).");

        super.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     GIVEN a lower-Z sibling that is NOT Overlapped but happens to sit fully inside a higher-Z
    ///     Overlapped opaque sibling
    ///     WHEN the SuperView draws
    ///     THEN it is NOT culled — culling is scoped to Overlapped candidates.
    /// </summary>
    [Fact]
    public void NonOverlappedCandidate_IsNotCulled ()
    {
        IDriver driver = CreateTestDriver (40, 20);

        View bottom = new () { Width = Dim.Fill (), Height = Dim.Fill () };
        View top = new () { Width = Dim.Fill (), Height = Dim.Fill (), Arrangement = ViewArrangement.Overlapped };

        View super = new () { Driver = driver, Width = 20, Height = 10 };
        super.Add (bottom);
        super.Add (top);
        super.Layout ();

        var bottomDraws = 0;
        bottom.DrawComplete += (_, _) => bottomDraws++;

        super.SetNeedsDraw ();
        super.Draw ();

        Assert.True (bottomDraws >= 1, $"Non-Overlapped candidate must not be culled (got {bottomDraws}).");

        super.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     GIVEN no Overlapped siblings at all (the common, non-overlapping case)
    ///     WHEN the SuperView draws
    ///     THEN occlusion culling is not engaged and every sibling draws.
    /// </summary>
    [Fact]
    public void NoOverlappedSiblings_NothingIsCulled ()
    {
        IDriver driver = CreateTestDriver (40, 20);

        // Two tiled (non-overlapping) siblings.
        View left = new () { X = 0, Y = 0, Width = 10, Height = 10 };
        View right = new () { X = 10, Y = 0, Width = 10, Height = 10 };

        View super = new () { Driver = driver, Width = 20, Height = 10 };
        super.Add (left);
        super.Add (right);
        super.Layout ();

        var leftDraws = 0;
        var rightDraws = 0;
        left.DrawComplete += (_, _) => leftDraws++;
        right.DrawComplete += (_, _) => rightDraws++;

        super.SetNeedsDraw ();
        super.Draw ();

        Assert.True (leftDraws >= 1, $"Left sibling should draw (got {leftDraws}).");
        Assert.True (rightDraws >= 1, $"Right sibling should draw (got {rightDraws}).");

        super.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     GIVEN two stacked Overlapped opaque siblings with distinct text
    ///     WHEN the SuperView draws
    ///     THEN the rendered contents show the top (occluder) text and never the culled bottom text
    ///     (no visual regression from culling).
    /// </summary>
    [Fact]
    public void Culling_IsOutputNeutral ()
    {
        IDriver driver = CreateTestDriver (40, 20);

        View bottom = new () { Text = "BOTTOM" };
        View top = new () { Text = "TOPVIEW" };
        View super = BuildOverlappedStack (driver, bottom, top);

        super.SetNeedsDraw ();
        super.Draw ();

        var contents = driver.ToString ();

        Assert.Contains ("TOPVIEW", contents);
        Assert.DoesNotContain ("BOTTOM", contents);

        super.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     GIVEN a <see cref="Tabs"/> control with several overlapping pages
    ///     WHEN it draws
    ///     THEN no page is culled (each page is <see cref="View.SuperViewRendersLineCanvas"/>), so tab
    ///     header line art is preserved.
    /// </summary>
    [Fact]
    public void Tabs_InactivePages_AreNotCulled ()
    {
        IDriver driver = CreateTestDriver (60, 20);

        Tabs tabs = new () { Driver = driver, Width = 40, Height = 12 };

        View page1 = new () { Title = "One" };
        View page2 = new () { Title = "Two" };
        View page3 = new () { Title = "Three" };
        tabs.Add (page1, page2, page3);

        tabs.Layout ();

        var page1Draws = 0;
        var page2Draws = 0;
        var page3Draws = 0;
        page1.DrawComplete += (_, _) => page1Draws++;
        page2.DrawComplete += (_, _) => page2Draws++;
        page3.DrawComplete += (_, _) => page3Draws++;

        tabs.SetNeedsDraw ();
        tabs.Draw ();

        Assert.True (page1Draws >= 1, $"Tab page 1 must not be culled (got {page1Draws}).");
        Assert.True (page2Draws >= 1, $"Tab page 2 must not be culled (got {page2Draws}).");
        Assert.True (page3Draws >= 1, $"Tab page 3 must not be culled (got {page3Draws}).");

        tabs.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     GIVEN a fully-covered opaque sibling with <see cref="ViewportSettingsFlags.TransparentMouse"/>
    ///     WHEN the SuperView draws
    ///     THEN it is NOT culled, so <see cref="View.Draw(DrawContext?)"/> runs and <see cref="View.DoDrawComplete"/>
    ///     repopulates its <see cref="View.CachedDrawnRegion"/> (which <see cref="View.SetNeedsDraw()"/> invalidated).
    ///     Culling it would drop the view from mouse hit-testing (null cache → blanket removal).
    /// </summary>
    [Fact]
    public void TransparentMouseCandidate_IsNotCulled_AndKeepsHitRegion ()
    {
        IDriver driver = CreateTestDriver (40, 20);

        View bottom = new () { ViewportSettings = ViewportSettingsFlags.TransparentMouse };
        View top = new ();
        View super = BuildOverlappedStack (driver, bottom, top);

        var bottomDraws = 0;
        bottom.DrawComplete += (_, _) => bottomDraws++;

        super.SetNeedsDraw ();
        super.Draw ();

        Assert.True (bottomDraws >= 1, $"TransparentMouse candidate must not be culled (got {bottomDraws}).");
        Assert.NotNull (bottom.CachedDrawnRegion);

        super.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     GIVEN a fully-covered opaque sibling with a non-empty Margin (the Margin is
    ///     <see cref="ViewportSettingsFlags.TransparentMouse"/> by default)
    ///     WHEN the SuperView draws
    ///     THEN it is NOT culled, so the Margin's hit-testing cache is not left stale.
    /// </summary>
    [Fact]
    public void TransparentMouseMarginCandidate_IsNotCulled ()
    {
        IDriver driver = CreateTestDriver (40, 20);

        View bottom = new ();
        bottom.Margin.Thickness = new Thickness (1);
        View top = new ();
        View super = BuildOverlappedStack (driver, bottom, top);

        var bottomDraws = 0;
        bottom.DrawComplete += (_, _) => bottomDraws++;

        super.SetNeedsDraw ();
        super.Draw ();

        Assert.True (bottomDraws >= 1, $"Candidate with a TransparentMouse Margin must not be culled (got {bottomDraws}).");

        super.Dispose ();
        driver.Dispose ();
    }
}
