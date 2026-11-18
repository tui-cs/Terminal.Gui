#nullable disable
using System.Text;
using UnitTests;

namespace ViewBaseTests.Draw;

// Claude - Opus 4.7
/// <summary>
///     Issue #5358: the framework's DoClearViewport narrows the clear to NeedsDrawRect when an
///     explicit partial region has been set AND the view is not itself scrolled. The public
///     ClearViewport API always does a full clear (preserves the contract used by
///     Code.OnClearingViewport, MarkdownCodeBlock, direct test callers). View.SetFrame
///     invalidates the SuperView for the union of the old and new Frame when Frame changes,
///     so stale uncovered cells get cleared on the next pass.
/// </summary>
public class RegionAwareClearViewportTests : TestDriverBase
{
    /// <summary>
    ///     Public ClearViewport always does a full clear regardless of NeedsDrawRect state —
    ///     this is the backward-compatible contract used by Code.OnClearingViewport,
    ///     MarkdownCodeBlock.OnClearingViewport, and direct test callers like
    ///     ClearViewport_FillsViewportArea.
    /// </summary>
    [Fact]
    public void PublicClearViewport_AlwaysClearsFullViewport ()
    {
        IDriver driver = CreateTestDriver (40, 20);
        driver.Clip = new Region (driver.Screen);

        View view = new () { Driver = driver, X = 5, Y = 5, Width = 10, Height = 5 };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        driver.FillRect (driver.Screen, new Rune ('X'));

        // Even with a partial NeedsDrawRect, public ClearViewport must clear the full viewport.
        view.ClearNeedsDraw ();
        view.SetNeedsDraw (new Rectangle (1, 1, 3, 2));
        Assert.Equal (new Rectangle (1, 1, 3, 2), view.NeedsDrawRect);

        view.ClearViewport ();

        Rectangle screen = view.ViewportToScreen (view.Viewport with { Location = new Point (0, 0) });

        for (int y = screen.Y; y < screen.Y + screen.Height; y++)
        {
            for (int x = screen.X; x < screen.X + screen.Width; x++)
            {
                Assert.Equal (" ", driver.Contents [y, x].Grapheme);
            }
        }

        view.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     The framework's draw pipeline (Draw → DoClearViewport) narrows the clear to the
    ///     dirty region when the view has a partial NeedsDrawRect and is not itself scrolled.
    ///     Verifies by invoking Draw() and observing that cells outside the dirty region keep
    ///     their pre-fill 'X' value.
    /// </summary>
    [Fact]
    public void FrameworkDraw_PartialNeedsDrawRect_ClearsOnlyDirtyRegion ()
    {
        IDriver driver = CreateTestDriver (40, 20);
        driver.Clip = new Region (driver.Screen);

        View view = new () { Driver = driver, X = 5, Y = 5, Width = 10, Height = 5 };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();
        view.Draw ();

        Assert.True (view.NeedsDrawRect.IsEmpty);

        // Re-set clip after Draw (DoDrawComplete excludes the view's frame from the clip,
        // which would prevent the next FillRect from reaching cells inside the view's area).
        driver.Clip = new Region (driver.Screen);
        driver.FillRect (driver.Screen, new Rune ('X'));
        Assert.Equal ("X", driver.Contents [5, 5].Grapheme);

        // Invalidate just a 3×2 region — strictly smaller than the 10×5 viewport.
        view.SetNeedsDraw (new Rectangle (1, 1, 3, 2));
        Assert.Equal (new Rectangle (1, 1, 3, 2), view.NeedsDrawRect);

        // Drive the framework's DoClearViewport via the normal draw path.
        view.Draw ();

        // The dirty region (viewport-local (1,1,3,2) → screen (6,6,3,2)) is cleared.
        for (var y = 6; y < 8; y++)
        {
            for (var x = 6; x < 9; x++)
            {
                Assert.Equal (" ", driver.Contents [y, x].Grapheme);
            }
        }

        // Cells outside the dirty region inside the viewport remain 'X'.
        Assert.Equal ("X", driver.Contents [5, 5].Grapheme);
        Assert.Equal ("X", driver.Contents [9, 14].Grapheme);

        view.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     SetNeedsDraw() (no-arg) sets NeedsDrawRect = Viewport. The framework must still
    ///     clear the full viewport in this case — narrowing only fires when the rect is
    ///     strictly smaller than the viewport.
    /// </summary>
    [Fact]
    public void FrameworkDraw_FullViewportNeedsDrawRect_ClearsFullViewport ()
    {
        IDriver driver = CreateTestDriver (40, 20);
        driver.Clip = new Region (driver.Screen);

        View view = new () { Driver = driver, X = 5, Y = 5, Width = 10, Height = 5 };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();
        view.Draw ();

        Assert.True (view.NeedsDrawRect.IsEmpty);

        driver.Clip = new Region (driver.Screen);
        driver.FillRect (driver.Screen, new Rune ('X'));

        // SetNeedsDraw() no-arg sets NeedsDrawRect to Viewport (full).
        view.SetNeedsDraw ();
        Assert.Equal (view.Viewport, view.NeedsDrawRect);

        view.Draw ();

        Rectangle screen = view.ViewportToScreen (view.Viewport with { Location = new Point (0, 0) });

        for (int y = screen.Y; y < screen.Y + screen.Height; y++)
        {
            for (int x = screen.X; x < screen.X + screen.Width; x++)
            {
                Assert.Equal (" ", driver.Contents [y, x].Grapheme);
            }
        }

        view.Dispose ();
        driver.Dispose ();
    }

    // Note: a prior FrameworkDraw_ScrolledView_FallsBackToFullClear test was removed in #5359.
    // It documented the temporary "Viewport.Location == Point.Empty" guard added in #5431 as a
    // workaround for the cascade coord-system inconsistency. Issue #5359 normalized the
    // convention and removed that guard. Narrowing on scrolled views is now correct and is
    // covered by NeedsDrawCoordTests.FrameworkNarrowing_NowWorks_OnScrolledView.

    /// <summary>
    ///     When a subview's Frame shrinks, View.Layout invalidates the SuperView for the union
    ///     of the old and new frames. This is the foundation for clearing stale uncovered cells
    ///     after geometry changes (without forcing the whole SuperView viewport to redraw).
    /// </summary>
    [Fact]
    public void FrameShrink_InvalidatesSuperViewWithUnionOfOldAndNewFrames ()
    {
        IDriver driver = CreateTestDriver (40, 20);
        driver.Clip = new Region (driver.Screen);

        View superView = new () { Driver = driver, Width = 20, Height = 10 };
        View view = new () { X = 0, Y = 0, Width = 7, Height = 4 };
        superView.Add (view);

        superView.Layout ();
        superView.Draw ();

        Assert.True (superView.NeedsDrawRect.IsEmpty);
        Assert.True (view.NeedsDrawRect.IsEmpty);

        // Shrink the subview's frame.
        view.Frame = new Rectangle (0, 0, 3, 2);

        // SuperView's NeedsDrawRect must include the OLD frame area so stale cells get cleared.
        Assert.False (superView.NeedsDrawRect.IsEmpty);
        Assert.True (superView.NeedsDrawRect.Contains (new Rectangle (0, 0, 7, 4)));

        superView.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     Pure scroll (Viewport.X/Y change without Frame change) must NOT invalidate the
    ///     SuperView. Otherwise we re-introduce the per-scroll draw fan-out that issue #5358
    ///     is fixing for overlapping tab content.
    /// </summary>
    [Fact]
    public void Scroll_DoesNotInvalidateSuperView ()
    {
        IDriver driver = CreateTestDriver (40, 20);
        driver.Clip = new Region (driver.Screen);

        View superView = new () { Driver = driver, Width = 20, Height = 10 };
        View view = new () { X = 0, Y = 0, Width = 10, Height = 5 };
        view.SetContentSize (new Size (100, 100));
        superView.Add (view);

        superView.Layout ();
        superView.Draw ();

        Assert.True (superView.NeedsDrawRect.IsEmpty);

        // Pure scroll — Viewport location changes, Frame does not.
        view.Viewport = view.Viewport with { Y = 5 };

        // SuperView must NOT have been invalidated by the scroll alone.
        Assert.True (superView.NeedsDrawRect.IsEmpty);

        superView.Dispose ();
        driver.Dispose ();
    }
}
