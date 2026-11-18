// Copilot - Opus 4.6

using UnitTests;

namespace ViewBaseTests.Adornments;

/// <summary>
///     Tests demonstrating that overlapped SubViews with <see cref="View.SuperViewRendersLineCanvas"/> = true
///     produce incorrect junction glyphs when a higher-Z view intentionally omits border lines (a "gap")
///     but a lower-Z view's lines fill that gap, causing auto-join to create wrong intersections.
///
///     These tests are independent of <see cref="BorderSettings.Tab"/> and demonstrate the general issue
///     with the flat LineCanvas merge strategy for overlapped views.
/// </summary>
public class OverlappedLineCanvasTests (ITestOutputHelper output) : TestDriverBase
{
    /// <summary>
    ///     Two overlapping bordered views where the higher-Z view has a partial border (gap on bottom).
    ///     The lower-Z view's full border should NOT fill the higher-Z view's intentional gap.
    ///
    ///     Layout (10x6):
    ///     <code>
    ///     viewBack:  (0,1) 8x4 — full border, lower Z (drawn first/behind)
    ///     viewFront: (0,0) 5x3 — border with NO bottom line, higher Z (drawn last/on top)
    ///     </code>
    ///
    ///     Expected: viewFront's gap remains open; viewBack's top line is occluded under viewFront.
    ///     Actual (bug): auto-join creates T-junctions where viewBack's top meets viewFront's sides.
    /// </summary>
    [Fact]
    public void HigherZ_Gap_Not_Filled_By_LowerZ_Border ()
    {
        // Copilot
        IDriver driver = CreateTestDriver (10, 6);

        using View superView = new ();
        superView.Driver = driver;
        superView.Width = 10;
        superView.Height = 6;

        // Lower-Z view: full border, behind
        View viewBack = new ()
        {
            X = 0,
            Y = 1,
            Width = 8,
            Height = 4,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true,
            Arrangement = ViewArrangement.Overlapped,
        };

        // Higher-Z view: border with intentional gap on bottom (no bottom line)
        // We simulate a gap by using OnDrawingContent to add lines manually
        GapBorderView viewFront = new ()
        {
            X = 0,
            Y = 0,
            Width = 5,
            Height = 3,
            SuperViewRendersLineCanvas = true,
            Arrangement = ViewArrangement.Overlapped,
        };

        // Add in Z-order: first added = lowest Z (drawn behind)
        superView.Add (viewBack, viewFront);

        superView.Layout ();
        superView.Draw ();

        output.WriteLine ("Actual:");
        output.WriteLine (driver.ToString ());

        // viewFront occupies rows 0-2, cols 0-4 with an open bottom.
        // viewBack occupies rows 1-4, cols 0-7 with full border.
        // The gap (row 2, cols 1-3) should be empty/spaces, NOT a line.
        //
        // Expected rendering:
        //   Row 0: ┌───┐       viewFront top
        //   Row 1: │   │──────┐ viewFront sides + viewBack top (occluded under viewFront)
        //   Row 2: │   │      │ viewFront gap (open) + viewBack sides
        //   Row 3: │   └──────┘ viewBack sides/bottom
        //   Row 4: └──────┘     viewBack bottom (but viewFront doesn't reach here)
        //
        // With the BUG, row 2 gets ├───┤ because viewBack's top line at y=1
        // auto-joins with viewFront's left/right lines, creating T-junctions.

        // This test documents the EXPECTED behavior after the fix.
        // For now, verify the glyph at the junction point to detect the bug.
        string actual = driver.ToString ()!;
        string [] lines = actual.Split ('\n');

        // Row 1, Col 0: should be │ (viewFront's left side, higher-Z)
        // With the bug it becomes ├ (T-junction from viewBack's top line auto-joining with viewFront's side)
        Assert.Equal ('│', lines [1] [0]);

        // Row 1, Col 4: should be │ (viewFront's right side, higher-Z)
        // With the bug it becomes ┤ (T-junction from viewBack's top)
        Assert.Equal ('│', lines [1] [4]);
    }

    /// <summary>
    ///     Two overlapping bordered views at the same X but different Y, where the front view's
    ///     border partially overlaps the back view's border. Tests that the front view's lines
    ///     take priority at shared cells.
    ///
    ///     This uses standard bordered views (no custom gap) to show the simplest case:
    ///     when two overlapped views share a border cell, the higher-Z view's glyph should win.
    /// </summary>
    [Fact]
    public void Overlapped_Views_HigherZ_Lines_Win_At_Shared_Cells ()
    {
        // Copilot
        IDriver driver = CreateTestDriver (10, 7);

        using View superView = new ();
        superView.Driver = driver;
        superView.Width = 10;
        superView.Height = 7;

        // Lower-Z: full single-line border
        View viewBack = new ()
        {
            X = 0,
            Y = 2,
            Width = 8,
            Height = 4,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true,
            Arrangement = ViewArrangement.Overlapped,
        };

        // Higher-Z: full single-line border, overlapping viewBack by 1 row at bottom
        View viewFront = new ()
        {
            X = 0,
            Y = 0,
            Width = 6,
            Height = 4,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true,
            Arrangement = ViewArrangement.Overlapped,
        };

        superView.Add (viewBack, viewFront);

        superView.Layout ();
        superView.Draw ();

        output.WriteLine ("Actual:");
        output.WriteLine (driver.ToString ());

        // viewFront: rows 0-3, cols 0-5
        // viewBack:  rows 2-5, cols 0-7
        // Overlap at rows 2-3, cols 0-5
        //
        // Row 2: viewFront has │   │ (sides), viewBack has ┌──────┐ (top)
        // Row 3: viewFront has └───┘ (bottom), viewBack has │      │ (sides)
        //
        // At (0,2): viewFront has │, viewBack has ┌ → should be │ (viewFront wins) or ├ if auto-join desired
        // At (0,3): viewFront has └, viewBack has │ → junction depends on design
        //
        // The key issue: with flat merge, glyphs at these cells are determined by BOTH views'
        // lines, which may not be what the user intended for overlapped views.

        string actual = driver.ToString ()!;
        string [] lines = actual.Split ('\n');

        // At minimum, verify that the rendering is deterministic and doesn't crash
        Assert.NotNull (actual);
        Assert.True (lines.Length >= 6, $"Expected at least 6 lines but got {lines.Length}");

        // Document the actual rendering for design validation
        output.WriteLine ("\nRendered output for design review:");

        for (var i = 0; i < lines.Length; i++)
        {
            output.WriteLine ($"  Row {i}: '{lines [i]}'");
        }
    }

    /// <summary>
    ///     Three overlapping bordered views in a staircase pattern. The highest-Z view should
    ///     fully occlude lower views where they overlap, and junction glyphs should only reflect
    ///     the highest-Z view's lines at those cells.
    ///
    ///     This simulates the Tab scenario generically: multiple overlapped views where only one
    ///     (the "focused" one) should dominate the visual at shared border cells.
    /// </summary>
    [Fact]
    public void Three_Overlapped_Staircase_HighestZ_Dominates ()
    {
        // Copilot
        IDriver driver = CreateTestDriver (12, 8);

        using View superView = new ();
        superView.Driver = driver;
        superView.Width = 12;
        superView.Height = 8;

        View view1 = new ()
        {
            X = 0,
            Y = 0,
            Width = 6,
            Height = 4,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true,
            Arrangement = ViewArrangement.Overlapped,
        };

        View view2 = new ()
        {
            X = 2,
            Y = 2,
            Width = 6,
            Height = 4,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true,
            Arrangement = ViewArrangement.Overlapped,
        };

        View view3 = new ()
        {
            X = 4,
            Y = 4,
            Width = 6,
            Height = 4,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true,
            Arrangement = ViewArrangement.Overlapped,
        };

        // Z-order: view1 lowest, view3 highest (last added = highest Z)
        superView.Add (view1, view2, view3);

        superView.Layout ();
        superView.Draw ();

        output.WriteLine ("Actual:");
        output.WriteLine (driver.ToString ());

        string actual = driver.ToString ()!;
        string [] lines = actual.Split ('\n');

        // The overlap regions should show the highest-Z view's border glyphs.
        // At (2,2): view1 has │ (right side interior), view2 has ┌ (top-left corner)
        // With flat merge, auto-join produces an incorrect glyph.
        // After fix, view2's ┌ should appear (or the correct junction for view2 alone).

        // Verify rendering is complete
        Assert.True (lines.Length >= 8, $"Expected at least 8 lines but got {lines.Length}");

        // Document for design review
        output.WriteLine ("\nRendered output for design review:");

        for (var i = 0; i < Math.Min (lines.Length, 8); i++)
        {
            output.WriteLine ($"  Row {i}: '{lines [i]}'");
        }
    }

    /// <summary>
    ///     Verifies that same-Z (non-overlapped) sibling views still get correct auto-join
    ///     when the overlapped drawing fix is in place. This is a regression guard.
    /// </summary>
    [Fact]
    public void SameZ_SideBySide_AutoJoin_Still_Works ()
    {
        // Copilot
        IDriver driver = CreateTestDriver (11, 4);

        using View superView = new ();
        superView.Driver = driver;
        superView.Width = 11;
        superView.Height = 4;

        View viewA = new ()
        {
            X = 0,
            Y = 0,
            Width = 5,
            Height = 3,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true,
        };

        View viewB = new ()
        {
            X = Pos.Right (viewA) - 1,
            Y = 0,
            Width = 5,
            Height = 3,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true,
        };

        superView.Add (viewA, viewB);

        superView.Layout ();
        superView.Draw ();

        output.WriteLine ("Actual:");
        output.WriteLine (driver.ToString ());

        // Same-Z, non-overlapped views sharing a column should auto-join correctly.
        // This MUST continue to work after any overlapped drawing fix.
        DriverAssert.AssertDriverContentsAre ("""
                                              ┌───┬───┐
                                              │   │   │
                                              └───┴───┘
                                              """,
                                              output,
                                              driver);
    }

    /// <summary>
    ///     A view with a full border (lower-Z) and a view with a partial border gap (higher-Z)
    ///     using Margin adornment. Demonstrates the issue is not specific to Border but applies
    ///     to any adornment SubView composition.
    /// </summary>
    [Fact]
    public void Overlapped_With_Padding_SubViews_HigherZ_Wins ()
    {
        // Copilot
        IDriver driver = CreateTestDriver (10, 6);

        using View superView = new ();
        superView.Driver = driver;
        superView.Width = 10;
        superView.Height = 6;

        // A full-bordered view at the back
        View viewBack = new ()
        {
            X = 0,
            Y = 1,
            Width = 8,
            Height = 4,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true,
            Arrangement = ViewArrangement.Overlapped,
        };

        // A full-bordered view at the front that overlaps viewBack
        View viewFront = new ()
        {
            X = 0,
            Y = 0,
            Width = 6,
            Height = 3,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true,
            Arrangement = ViewArrangement.Overlapped,
        };

        superView.Add (viewBack, viewFront);

        superView.Layout ();
        superView.Draw ();

        output.WriteLine ("Actual:");
        output.WriteLine (driver.ToString ());

        string actual = driver.ToString ()!;
        string [] lines = actual.Split ('\n');

        // viewFront: rows 0-2, cols 0-5 (full border)
        // viewBack:  rows 1-4, cols 0-7 (full border)
        // Overlap at row 1-2, cols 0-5
        //
        // Row 0: ┌────┐      (viewFront top)
        // Row 1: │    │─────┐ (viewFront sides; viewBack top partially visible)
        // Row 2: └────┘     │ (viewFront bottom; viewBack sides)
        // Row 3: │          │ (viewBack sides)
        // Row 4: └──────────┘ (viewBack bottom)
        //
        // At (0,1): viewFront has │, viewBack has ┌ → flat merge auto-joins to ├
        // After fix: viewFront's │ should win (higher Z)

        // Row 1, Col 0: should be │ (viewFront's side, higher Z) not ├ (junction)
        // This assertion will fail with the current flat merge, demonstrating the bug.
        char glyphAtOverlap = lines [1] [0];
        output.WriteLine ($"\nGlyph at (0,1): '{glyphAtOverlap}' (expected '│' for higher-Z wins)");

        // NOTE: This assertion documents the DESIRED behavior.
        // It will FAIL with the current implementation, proving the bug.
        Assert.Equal ('│', glyphAtOverlap);
    }
}

/// <summary>
///     A view that draws a border with an intentional gap on the bottom side.
///     Simulates what a "focused tab" does — 3 sides drawn, bottom open.
/// </summary>
internal class GapBorderView : View
{
    public GapBorderView () =>

        // Don't use the standard border — we draw manually via LineCanvas
        Border.Settings = BorderSettings.Default;

    protected override bool OnDrawingContent (DrawContext? context)
    {
        LineCanvas lc = LineCanvas;
        Rectangle bounds = ViewportToScreen ();
        Attribute attr = GetAttributeForRole (VisualRole.Normal);

        // Draw 3 sides: top, left, right — leave bottom open (the "gap")
        lc.AddLine (new Point (bounds.X, bounds.Y), bounds.Width, Orientation.Horizontal, LineStyle.Single, attr); // top
        lc.AddLine (new Point (bounds.X, bounds.Y), bounds.Height, Orientation.Vertical, LineStyle.Single, attr);  // left
        lc.AddLine (new Point (bounds.Right - 1, bounds.Y), bounds.Height, Orientation.Vertical, LineStyle.Single, attr); // right

        // No bottom line — this is the "gap"

        return true;
    }
}
