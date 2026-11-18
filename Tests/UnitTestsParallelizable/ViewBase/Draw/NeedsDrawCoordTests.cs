#nullable disable
using System.Text;
using UnitTests;

namespace ViewBaseTests.Draw;

// Claude - Opus 4.7
/// <summary>
///     Issue #5359: NeedsDrawRect and the viewPortRelativeRegion parameter of SetNeedsDraw are in
///     viewport-LOCAL coordinates — (0, 0) is the top-left visible cell of the View's Viewport,
///     independent of Viewport.Location (scroll offset, possibly negative).
///
///     These tests pin down the convention across:
///       * SetNeedsDraw() no-arg on a scrolled or negative-viewport-location view
///       * Subview cascade when parent is scrolled
///       * Subview cascade when subview has adornments
///       * Subview cascade when subview is itself scrolled
///       * Subview cascade when dirty region only overlaps subview's adornments
///       * Framework ClearViewport narrowing now working on scrolled views
///       * Zero-size viewport edge case
/// </summary>
public class NeedsDrawCoordTests : TestDriverBase
{
    /// <summary>
    ///     SetNeedsDraw() no-arg on a scrolled view stores a viewport-LOCAL rect — (0, 0, W, H),
    ///     NOT the current Viewport (which carries the scroll offset).
    /// </summary>
    [Fact]
    public void SetNeedsDraw_NoArg_OnScrolledView_StoresViewportLocalRect ()
    {
        IDriver driver = CreateTestDriver (40, 20);

        View view = new () { Driver = driver, X = 0, Y = 0, Width = 10, Height = 8 };
        view.SetContentSize (new Size (100, 100));
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();
        view.Draw ();

        // Scroll so Viewport.Location is non-zero.
        view.Viewport = view.Viewport with { Location = new Point (5, 3) };
        Assert.Equal (new Point (5, 3), view.Viewport.Location);

        view.ClearNeedsDraw ();
        view.SetNeedsDraw ();

        // NeedsDrawRect must be viewport-LOCAL: (0, 0, W, H).
        Assert.Equal (new Rectangle (0, 0, 10, 8), view.NeedsDrawRect);

        view.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     SetNeedsDraw() no-arg on a view with a negative Viewport.Location (allowed via
    ///     AllowNegativeX/Y) must still produce a viewport-local rect — negative locations must
    ///     not leak into NeedsDrawRect.
    /// </summary>
    [Fact]
    public void SetNeedsDraw_NoArg_OnNegativeViewportLocation_StoresViewportLocalRect ()
    {
        IDriver driver = CreateTestDriver (40, 20);

        View view = new ()
        {
            Driver = driver,
            X = 0,
            Y = 0,
            Width = 10,
            Height = 5,
            ViewportSettings = ViewportSettingsFlags.AllowNegativeLocation
                               | ViewportSettingsFlags.AllowLocationGreaterThanContentSize
        };
        view.SetContentSize (new Size (100, 100));
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();
        view.Draw ();

        view.Viewport = view.Viewport with { Location = new Point (-3, -2) };
        Assert.Equal (new Point (-3, -2), view.Viewport.Location);

        view.ClearNeedsDraw ();
        view.SetNeedsDraw ();

        Assert.Equal (new Rectangle (0, 0, 10, 5), view.NeedsDrawRect);

        view.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     When the parent is scrolled and a content-area region is invalidated, the cascade
    ///     translates the parent's viewport-local region into the subview's viewport-local
    ///     coordinates correctly.
    ///
    ///     Setup: parent (20×10), scrolled to (5, 0). Subview at Frame (3, 1, 10, 6) in
    ///     parent's CONTENT coords — that means in the parent's viewport-local frame the
    ///     subview's visible portion starts at viewport-local X = (3 - 5) = -2. Invalidate a
    ///     parent viewport-local rect of (1, 2, 5, 2). That maps to parent CONTENT
    ///     (6, 2, 5, 2), which overlaps the subview at content (6, 2, 5, 2) ∩ Frame
    ///     (3, 1, 10, 6) = (6, 2, 5, 2). In subview-frame-local: (3, 1, 5, 2). The subview
    ///     has no adornments and isn't scrolled, so subview-viewport-local equals
    ///     subview-frame-local.
    /// </summary>
    [Fact]
    public void SetNeedsDraw_ScrolledParent_CascadesViewportLocalRectToSubview ()
    {
        IDriver driver = CreateTestDriver (40, 20);

        View parent = new () { Driver = driver, X = 0, Y = 0, Width = 20, Height = 10 };
        parent.SetContentSize (new Size (40, 20));

        View subview = new () { X = 3, Y = 1, Width = 10, Height = 6 };
        parent.Add (subview);

        parent.BeginInit ();
        parent.EndInit ();
        parent.LayoutSubViews ();
        parent.Draw ();

        // Scroll parent.
        parent.Viewport = parent.Viewport with { Location = new Point (5, 0) };
        parent.Draw ();

        parent.ClearNeedsDraw ();

        // Invalidate a parent viewport-local 5×2 rect at viewport (1, 2).
        parent.SetNeedsDraw (new Rectangle (1, 2, 5, 2));

        Assert.Equal (new Rectangle (3, 1, 5, 2), subview.NeedsDrawRect);

        parent.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     When a subview has padding (adornment), the cascade lands the dirty region in the
    ///     subview's viewport-local coords (inside the padding), NOT subview-frame-local.
    ///
    ///     Setup: parent (20×10), subview at Frame (2, 2, 12, 6) with Padding.Thickness = 1.
    ///     Subview Viewport size = (10, 4). Invalidate parent viewport-local (3, 3, 10, 4).
    ///     That intersects subview.Frame at content (3, 3, 10, 4) ∩ (2, 2, 12, 6)
    ///     = (3, 3, 10, 4). Subview-frame-local: (1, 1, 10, 4). Subtract padding offset
    ///     (1, 1): (0, 0, 10, 4). Clip to subview viewport (0, 0, 10, 4): (0, 0, 10, 4).
    /// </summary>
    [Fact]
    public void SetNeedsDraw_SubViewWithPadding_CascadesViewportLocalRectIntoSubviewViewport ()
    {
        IDriver driver = CreateTestDriver (40, 20);

        View parent = new () { Driver = driver, X = 0, Y = 0, Width = 20, Height = 10 };

        View subview = new () { X = 2, Y = 2, Width = 12, Height = 6 };
        subview.Padding.Thickness = new Thickness (1);
        parent.Add (subview);

        parent.BeginInit ();
        parent.EndInit ();
        parent.LayoutSubViews ();
        parent.Draw ();

        Assert.Equal (new Size (10, 4), subview.Viewport.Size);

        parent.ClearNeedsDraw ();

        parent.SetNeedsDraw (new Rectangle (3, 3, 10, 4));

        // Subview's NeedsDrawRect is in subview-VIEWPORT-local — adornment offset is removed.
        Assert.Equal (new Rectangle (0, 0, 10, 4), subview.NeedsDrawRect);

        parent.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     When the subview itself is scrolled, the cascade lands the dirty rect at the
    ///     correct VIEWPORT-LOCAL position — viewport-local coords are scroll-INDEPENDENT
    ///     ((0, 0) is always the top-left visible cell regardless of which content cell the
    ///     scroll maps there). We propagate the dirty ON-SCREEN cells, not the content cells
    ///     they happen to be showing.
    ///
    ///     Setup: parent (20×10), subview Frame (0, 0, 10, 5) with content (100, 100) and
    ///     subview is scrolled to (4, 1). Parent invalidation at viewport-local (2, 2, 4, 2).
    ///     Subview-frame-local: (2, 2, 4, 2). No adornment offset, no scroll subtraction:
    ///     subview-viewport-local: (2, 2, 4, 2). Clip to (0, 0, 10, 5): (2, 2, 4, 2).
    /// </summary>
    [Fact]
    public void SetNeedsDraw_ScrolledSubview_CascadesIntoSubviewViewportAtCorrectOnScreenPosition ()
    {
        IDriver driver = CreateTestDriver (40, 20);

        View parent = new () { Driver = driver, X = 0, Y = 0, Width = 20, Height = 10 };

        View subview = new () { X = 0, Y = 0, Width = 10, Height = 5 };
        subview.SetContentSize (new Size (100, 100));
        parent.Add (subview);

        parent.BeginInit ();
        parent.EndInit ();
        parent.LayoutSubViews ();
        parent.Draw ();

        subview.Viewport = subview.Viewport with { Location = new Point (4, 1) };
        parent.Draw ();
        parent.ClearNeedsDraw ();

        parent.SetNeedsDraw (new Rectangle (2, 2, 4, 2));

        Assert.Equal (new Rectangle (2, 2, 4, 2), subview.NeedsDrawRect);

        parent.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     When the dirty region only overlaps the subview's adornment area (not its viewport),
    ///     the cascade falls back to a no-arg SetNeedsDraw on the subview — the subview's
    ///     NeedsDrawRect becomes the full viewport AND its adornments are flagged for redraw.
    ///
    ///     Setup: subview Frame (5, 5, 10, 6) with Padding.Thickness = 1; visible viewport
    ///     is (0, 0, 8, 4) in subview-local. Invalidate parent (5, 5, 1, 1) — only the
    ///     top-left padding cell of the subview. Subview-frame-local: (0, 0, 1, 1). Minus
    ///     padding offset (1, 1): (-1, -1, 1, 1). Clip with viewport bounds (0, 0, 8, 4):
    ///     empty. Cascade falls back to full subview invalidation.
    /// </summary>
    [Fact]
    public void SetNeedsDraw_DirtyRegionInSubviewAdornmentOnly_FallsBackToFullSubviewRedraw ()
    {
        IDriver driver = CreateTestDriver (40, 20);

        View parent = new () { Driver = driver, X = 0, Y = 0, Width = 20, Height = 10 };

        View subview = new () { X = 5, Y = 5, Width = 10, Height = 6 };
        subview.Padding.Thickness = new Thickness (1);
        subview.Padding.GetOrCreateView ();
        parent.Add (subview);

        parent.BeginInit ();
        parent.EndInit ();
        parent.LayoutSubViews ();
        parent.Draw ();

        Assert.Equal (new Size (8, 4), subview.Viewport.Size);

        parent.ClearNeedsDraw ();

        parent.SetNeedsDraw (new Rectangle (5, 5, 1, 1));

        // Fallback: subview's NeedsDrawRect ends up as the full viewport, and adornments
        // are flagged for redraw.
        Assert.Equal (new Rectangle (0, 0, 8, 4), subview.NeedsDrawRect);
        Assert.True (subview.Padding.View?.NeedsDraw);

        parent.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     Issue #5359 follow-up to PR #5431: with the coord-system normalized, the framework's
    ///     region-aware ClearViewport now narrows correctly on scrolled views. The temporary
    ///     "Viewport.Location == Point.Empty" guard from PR #5431 is removed.
    /// </summary>
    [Fact]
    public void FrameworkNarrowing_NowWorks_OnScrolledView ()
    {
        IDriver driver = CreateTestDriver (40, 20);
        driver.Clip = new Region (driver.Screen);

        View view = new () { Driver = driver, X = 5, Y = 5, Width = 10, Height = 5 };
        view.SetContentSize (new Size (100, 100));
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();
        view.Draw ();

        // Scroll so Viewport.Location != (0, 0).
        view.Viewport = view.Viewport with { Y = 5 };
        Assert.Equal (new Point (0, 5), view.Viewport.Location);

        view.Draw ();
        driver.Clip = new Region (driver.Screen);
        driver.FillRect (driver.Screen, new Rune ('X'));

        // Partial dirty rect in viewport-local coords.
        view.ClearNeedsDraw ();
        view.SetNeedsDraw (new Rectangle (1, 1, 3, 2));

        view.Draw ();

        // Only the 3×2 dirty region in viewport-local should be cleared. viewport-local (1, 1)
        // → screen (5+1, 5+1) = (6, 6) (the view's screen position is (5, 5), no adornments).
        for (var y = 6; y < 8; y++)
        {
            for (var x = 6; x < 9; x++)
            {
                Assert.Equal (" ", driver.Contents [y, x].Grapheme);
            }
        }

        // Cells outside the dirty region (but inside the viewport) keep their 'X'.
        Assert.Equal ("X", driver.Contents [5, 5].Grapheme);
        Assert.Equal ("X", driver.Contents [9, 14].Grapheme);

        view.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     SetNeedsDraw() no-arg on a view with a zero-size viewport is a no-op (early-return
    ///     guards: NeedsDrawRect already set + viewport empty).
    /// </summary>
    [Fact]
    public void SetNeedsDraw_ZeroSizeViewport_IsNoOp ()
    {
        View view = new () { Width = 0, Height = 0 };
        view.BeginInit ();
        view.EndInit ();

        view.SetNeedsDraw ();

        Assert.False (view.NeedsDraw);
        Assert.Equal (Rectangle.Empty, view.NeedsDrawRect);

        view.Dispose ();
    }

    /// <summary>
    ///     When a subview's Frame changes and the SuperView is itself scrolled, SetFrame's
    ///     SuperView invalidation must translate the union(old, new) rect from SuperView
    ///     CONTENT coords to SuperView VIEWPORT-LOCAL coords. Without the translation, the
    ///     invalidation lands at the wrong on-screen position.
    ///
    ///     Setup: superView (20×10) scrolled by (5, 3). subview at Frame (10, 8, 7, 4) in
    ///     superView's content coords. Shrink subview Frame to (10, 8, 3, 2). SetFrame
    ///     invalidates with union(old, new) = (10, 8, 7, 4) (content coords). Translation
    ///     to superView viewport-local: subtract (5, 3) → (5, 5, 7, 4). superView's
    ///     NeedsDrawRect must contain this rect (it may be larger due to union with previously
    ///     accumulated dirty regions, but must contain (5, 5, 7, 4)).
    /// </summary>
    [Fact]
    public void SetFrame_ScrolledSuperView_TranslatesInvalidationToViewportLocal ()
    {
        IDriver driver = CreateTestDriver (40, 20);

        View superView = new () { Driver = driver, X = 0, Y = 0, Width = 20, Height = 10 };
        superView.SetContentSize (new Size (100, 100));

        View subview = new () { X = 10, Y = 8, Width = 7, Height = 4 };
        superView.Add (subview);

        superView.BeginInit ();
        superView.EndInit ();
        superView.LayoutSubViews ();
        superView.Draw ();

        superView.Viewport = superView.Viewport with { Location = new Point (5, 3) };
        Assert.Equal (new Point (5, 3), superView.Viewport.Location);

        superView.Draw ();
        superView.ClearNeedsDraw ();
        Assert.Equal (Rectangle.Empty, superView.NeedsDrawRect);

        // Shrink subview's Frame. SetFrame invalidates SuperView with union(old, new) =
        // (10, 8, 7, 4) in superView CONTENT coords; the translation subtracts the scroll
        // (5, 3) to produce viewport-local (5, 5, 7, 4).
        subview.Frame = new Rectangle (10, 8, 3, 2);

        Assert.True (superView.NeedsDrawRect.Contains (new Rectangle (5, 5, 7, 4)),
                     $"Expected superView.NeedsDrawRect to contain viewport-local (5, 5, 7, 4); got {superView.NeedsDrawRect}.");

        // Sanity: the rect must NOT have left the scroll offset baked in. If translation
        // were skipped, NeedsDrawRect would contain content-coord (10, 8, ...) which has
        // X >= 10.
        Assert.True (superView.NeedsDrawRect.X < 10,
                     $"NeedsDrawRect.X = {superView.NeedsDrawRect.X}; if scroll wasn't subtracted it would be >= 10 (content-coord X of the subview).");

        superView.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     Repeated small invalidations on a scrolled view still produce a precise accumulating
    ///     union (AC1 from #5359 — already enabled by #5431's union fix, but worth pinning to
    ///     verify the new coord convention doesn't regress accumulation).
    /// </summary>
    [Fact]
    public void SetNeedsDraw_RepeatedSmallInvalidationsOnScrolledView_AccumulateAsViewportLocalUnion ()
    {
        IDriver driver = CreateTestDriver (40, 20);

        View view = new () { Driver = driver, X = 0, Y = 0, Width = 20, Height = 10 };
        view.SetContentSize (new Size (100, 100));
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();
        view.Draw ();

        view.Viewport = view.Viewport with { Location = new Point (4, 2) };
        view.ClearNeedsDraw ();

        view.SetNeedsDraw (new Rectangle (1, 1, 3, 2));
        view.SetNeedsDraw (new Rectangle (10, 5, 4, 3));

        // Union((1,1,3,2),(10,5,4,3)) = (1,1,13,7).
        Assert.Equal (new Rectangle (1, 1, 13, 7), view.NeedsDrawRect);

        view.Dispose ();
        driver.Dispose ();
    }
}
