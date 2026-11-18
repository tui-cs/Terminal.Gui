// Copilot

namespace DrawingTests.RegionTests;

/// <summary>
///     Tests that verify correct handling of zero-width and zero-height rectangles
///     in <see cref="Region"/> operations. These degenerate rectangles represent no area
///     and must not affect region computations.
/// </summary>
public class ZeroAreaRectangleTests
{
    [Fact]
    public void MergeRectangles_ZeroWidthRect_IsIgnored ()
    {
        /*
            INPUT: Three valid rectangles plus one zero-width rectangle.
            The zero-width rect {0,0,0,2} has Left == Right == 0, so it covers no area.

              x=0 1 2 3
            y=0  A B
            y=1  A B C C

            Rectangles:
              Zero: (0,0,0,2)  // zero width — should be ignored
              A:    (0,0,1,2)
              B:    (1,0,1,2)
              C:    (2,1,2,1)

            EXPECTED: The zero-width rect should not affect the merge.
            Without it, the result should be:
              (0,0,1,2)  (1,0,1,2)  (2,1,2,1)
        */

        List<Rectangle> rectangles =
        [
            new (0, 0, 0, 2), // zero width — degenerate
            new (0, 0, 1, 2), // A
            new (1, 0, 1, 2), // B
            new (2, 1, 2, 1) // C
        ];

        List<Rectangle> merged = Region.MergeRectangles (rectangles, false);

        // The zero-width rect should have no effect. C should remain at Y=1, Height=1.
        Assert.Equal (3, merged.Count);
        Assert.Contains (new Rectangle (0, 0, 1, 2), merged); // A
        Assert.Contains (new Rectangle (1, 0, 1, 2), merged); // B
        Assert.Contains (new Rectangle (2, 1, 2, 1), merged); // C
    }

    [Fact]
    public void MergeRectangles_ZeroHeightRect_IsIgnored ()
    {
        /*
            INPUT: Two non-adjacent rectangles bridged by a zero-height rectangle.
            The zero-height rect {1,0,2,0} has Top == Bottom == 0, so it covers no area.

              x=0 1 2
            y=0  A   B
            y=1  A   B

            Rectangles:
              A:    (0,0,1,2)
              Zero: (1,0,2,0)  // zero height — should be ignored
              B:    (2,0,1,2)

            EXPECTED: The zero-height rect should not appear in the output.
            Without it, the result should be just A and B.
        */

        List<Rectangle> rectangles =
        [
            new (0, 0, 1, 2), // A
            new (1, 0, 2, 0), // zero height — degenerate
            new (2, 0, 1, 2) // B
        ];

        List<Rectangle> merged = Region.MergeRectangles (rectangles, false);

        // Zero-height rect should produce no output rect.
        Assert.Equal (2, merged.Count);
        Assert.Contains (new Rectangle (0, 0, 1, 2), merged); // A
        Assert.Contains (new Rectangle (2, 0, 1, 2), merged); // B
    }

    [Fact]
    public void Intersect_ProducingZeroWidthRect_ExcludesItFromResult ()
    {
        /*
            Region has a rect at (-1,0,1,2) — one column to the left of the origin.
            Intersecting with (0,0,4,2) clips it to {0,0,0,2} — zero width.
            This zero-width result should NOT be kept in the region.
        */

        Region region = new (new Rectangle (-1, 0, 1, 2));
        region.Combine (new Rectangle (0, 0, 4, 2), RegionOp.Intersect);

        Rectangle [] rects = region.GetRectangles ();
        Assert.Empty (rects);
    }

    [Fact]
    public void Intersect_ProducingZeroHeightRect_ExcludesItFromResult ()
    {
        /*
            Region has a rect at (0,-1,2,1) — one row above the origin.
            Intersecting with (0,0,4,4) clips it to {0,0,2,0} — zero height.
            This zero-height result should NOT be kept in the region.
        */

        Region region = new (new Rectangle (0, -1, 2, 1));
        region.Combine (new Rectangle (0, 0, 4, 4), RegionOp.Intersect);

        Rectangle [] rects = region.GetRectangles ();
        Assert.Empty (rects);
    }

    [Fact]
    public void Intersect_MixedResults_OnlyKeepsPositiveAreaRects ()
    {
        /*
            Region has two rects: one that will clip to zero-width,
            and one that will produce a valid intersection.

              Rect A: (-1,0,1,2) — entirely left of the clip area
              Rect B: (1,0,2,2)  — fully inside the clip area

            Clip region: (0,0,4,2)

            After intersect:
              A clips to (0,0,0,2) — zero width, should be excluded
              B clips to (1,0,2,2) — valid, should be kept
        */

        Region region = new (new Rectangle (-1, 0, 1, 2));
        region.Combine (new Rectangle (1, 0, 2, 2), RegionOp.Union);

        region.Combine (new Rectangle (0, 0, 4, 2), RegionOp.Intersect);

        Rectangle [] rects = region.GetRectangles ();
        Assert.Single (rects);
        Assert.Equal (new Rectangle (1, 0, 2, 2), rects [0]);
    }

    [Fact]
    public void Union_WithZeroWidthRectFromIntersect_DoesNotCorruptResult ()
    {
        /*
            This reproduces the exact bug observed during debugging of
            View.Drawing.AddDrawnRegionForAdornment.

            A TabView subview with TabOffset = -1 causes its Border LineCanvas
            to render at X = -1. The raw line canvas region (lastLineCanvasRegion)
            contains a rect at {-1,0,1,2}.

            When intersected with the adornment frame {0,0,4,2}, this clips to
            {0,0,0,2} — a zero-width rect that remains in _rectangles because
            Rectangle.IsEmpty returns false for it.

            This zero-width rect then poisons the Union sweep-line merge,
            causing rect {2,1,2,1} to inflate to {2,0,2,2}.

            Step 1 — Build the raw line canvas region:
              (-1,0,1,2)  (0,0,1,1)  (1,0,1,2)  (2,1,2,1)

            Step 2 — Intersect with adornment frame (0,0,4,2):
              {-1,0,1,2} clips to {0,0,0,2} (zero-width, kept by buggy IsEmpty check)
              {0,0,1,1}  {1,0,1,2}  {2,1,2,1} unchanged

            Step 3 — Build exclusion from LastDrawnRegion:
              (0,0,1,2)  (1,0,1,2)

            Step 4 — Union lineRegion into exclusion:
              exclusion[2] must be (2,1,2,1), NOT (2,0,2,2).
        */

        // Step 1: Build lineRegion matching the raw line canvas output
        Region lineRegion = new (new Rectangle (-1, 0, 1, 2));
        lineRegion.Combine (new Rectangle (0, 0, 1, 1), RegionOp.Union);
        lineRegion.Combine (new Rectangle (1, 0, 1, 2), RegionOp.Union);
        lineRegion.Combine (new Rectangle (2, 1, 2, 1), RegionOp.Union);

        // Step 2: Intersect with adornment frame — this creates the zero-width rect
        lineRegion.Combine (new Rectangle (0, 0, 4, 2), RegionOp.Intersect);

        // Step 3: Build exclusion region
        Region exclusion = new ();
        exclusion.Combine (new Rectangle (0, 0, 1, 2), RegionOp.Union);
        exclusion.Combine (new Rectangle (1, 0, 1, 2), RegionOp.Union);

        // Step 4: Union lineRegion into exclusion — this is where the bug manifests
        exclusion.Combine (lineRegion, RegionOp.Union);

        // The region should cover:
        //   x=0..1, y=0..1 (columns 0-1, full height)
        //   x=2..3, y=1    (columns 2-3, only row 1)
        // It must NOT cover x=2..3, y=0.
        Assert.False (exclusion.Contains (2, 0), "Cell (2,0) should NOT be in the exclusion region.");
        Assert.False (exclusion.Contains (3, 0), "Cell (3,0) should NOT be in the exclusion region.");
        Assert.True (exclusion.Contains (2, 1), "Cell (2,1) should be in the exclusion region.");
        Assert.True (exclusion.Contains (3, 1), "Cell (3,1) should be in the exclusion region.");
        Assert.True (exclusion.Contains (0, 0), "Cell (0,0) should be in the exclusion region.");
        Assert.True (exclusion.Contains (1, 0), "Cell (1,0) should be in the exclusion region.");
    }
}
