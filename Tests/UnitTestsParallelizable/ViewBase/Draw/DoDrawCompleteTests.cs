// Claude - Opus 4.6

using System.Text;
using UnitTests;

namespace ViewBaseTests.Drawing;

/// <summary>
///     Tests for the DoDrawComplete method in View.Drawing.cs, which handles clip exclusion
///     after a view finishes drawing. Tests cover opaque views, transparent views, adornments,
///     and context accumulation.
/// </summary>
public class DoDrawCompleteTests : TestDriverBase
{
    /// <summary>
    ///     Verifies that an opaque view excludes its entire frame from Driver.Clip after drawing.
    /// </summary>
    [Fact]
    public void OpaqueView_ExcludesEntireFrameFromClip ()
    {
        IDriver driver = CreateTestDriver ();
        Region initialClip = new (driver.Screen);
        driver.Clip = initialClip;

        View view = new ()
        {
            X = 5,
            Y = 5,
            Width = 10,
            Height = 10,
            Driver = driver
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        view.Draw ();

        // After drawing, the view's frame area should be excluded from the clip.
        // Points inside the view's frame should NOT be in the clip.
        Rectangle frameScreen = view.FrameToScreen ();
        Assert.False (driver.Clip!.Contains (frameScreen.X + 1, frameScreen.Y + 1), "Interior point of opaque view should be excluded from clip after Draw()");

        // Points outside the view should still be in the clip.
        Assert.True (driver.Clip.Contains (0, 0), "Point outside view should remain in clip");
    }

    /// <summary>
    ///     Verifies that when a Border exists, DoDrawComplete uses Border.FrameToScreen()
    ///     (which excludes Margin) rather than View.FrameToScreen() (which includes Margin).
    /// </summary>
    [Fact]
    public void OpaqueView_UsesBorderFrameNotViewFrame ()
    {
        IDriver driver = CreateTestDriver ();
        Region initialClip = new (driver.Screen);
        driver.Clip = initialClip;

        View view = new ()
        {
            X = 5,
            Y = 5,
            Width = 12,
            Height = 12,
            BorderStyle = LineStyle.Single,
            Driver = driver
        };
        view.Margin!.Thickness = new Thickness (1);
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        view.Draw ();

        // The Margin area should NOT be excluded from clip (Margin is outside the border).
        // View.Frame in screen coords includes Margin. Border.Frame excludes it.
        Rectangle viewFrameScreen = view.FrameToScreen ();
        Rectangle borderFrameScreen = view.Border!.FrameToScreen ();

        // The top-left corner of the view frame (which is the Margin area) should still be in the clip.
        Assert.True (driver.Clip!.Contains (viewFrameScreen.X, viewFrameScreen.Y), "Margin area (top-left of view frame) should remain in clip");

        // The border area should be excluded from clip.
        Assert.False (driver.Clip.Contains (borderFrameScreen.X + 1, borderFrameScreen.Y + 1), "Border interior should be excluded from clip");
    }

    /// <summary>
    ///     Verifies that an opaque view updates the DrawContext with its borderFrame rectangle,
    ///     allowing its SuperView to know what area was occupied (important for transparency).
    /// </summary>
    [Fact]
    public void OpaqueView_UpdatesDrawContext ()
    {
        IDriver driver = CreateTestDriver ();
        driver.Clip = new Region (driver.Screen);

        DrawContext context = new ();

        View view = new ()
        {
            X = 5,
            Y = 5,
            Width = 10,
            Height = 10,
            Driver = driver
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        // Draw with an explicit context so we can inspect it afterward.
        view.Draw (context);

        // The opaque view should have added its frame rectangle to the context.
        Region drawnRegion = context.GetDrawnRegion ();
        Assert.False (drawnRegion.IsEmpty (), "Opaque view should add its frame to DrawContext");

        Rectangle frameScreen = view.FrameToScreen ();
        Assert.True (drawnRegion.Contains (frameScreen.X + 1, frameScreen.Y + 1), "DrawContext should contain the view's frame area");
    }

    /// <summary>
    ///     Verifies that a transparent view excludes only the actually-drawn cells from Driver.Clip,
    ///     not the entire frame.
    /// </summary>
    [Fact]
    public void TransparentView_ExcludesOnlyDrawnRegion ()
    {
        IDriver driver = CreateTestDriver ();
        Region initialClip = new (driver.Screen);
        driver.Clip = initialClip;

        // A transparent view that draws a small rectangle (not the full viewport).
        TransparentDrawingView view = new ()
        {
            X = 5,
            Y = 5,
            Width = 20,
            Height = 20,
            Driver = driver,
            ViewportSettings = ViewportSettingsFlags.Transparent,

            // Draw only a 5x5 area at viewport position (2, 2)
            DrawRect = new Rectangle (2, 2, 5, 5)
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        view.Draw ();

        // The drawn area (screen coords: 7,7 to 11,11) should be excluded.
        Rectangle drawnScreen = view.ViewportToScreen (view.DrawRect);
        Assert.False (driver.Clip!.Contains (drawnScreen.X + 1, drawnScreen.Y + 1), "Drawn area should be excluded from clip for transparent view");

        // The undrawn area within the viewport should NOT be excluded (still in clip).
        // Check a point inside the viewport but outside the drawn rect.
        Point undrawPoint = new (view.ViewportToScreen (view.Viewport).X, view.ViewportToScreen (view.Viewport).Y);
        Assert.True (driver.Clip.Contains (undrawPoint.X, undrawPoint.Y), "Undrawn area of transparent view should remain in clip");
    }

    /// <summary>
    ///     Verifies that the drawn region is clamped to the Viewport bounds before being excluded
    ///     from the clip. Content drawn outside the Viewport shouldn't be excluded.
    /// </summary>
    [Fact]
    public void TransparentView_ClampsDrawnRegionToViewport ()
    {
        IDriver driver = CreateTestDriver ();
        driver.Clip = new Region (driver.Screen);

        DrawContext context = new ();

        // A transparent view that reports a drawn region extending beyond its viewport.
        TransparentDrawingView view = new ()
        {
            X = 5,
            Y = 5,
            Width = 10,
            Height = 10,
            Driver = driver,
            ViewportSettings = ViewportSettingsFlags.Transparent,

            // Draw a rect that extends beyond the viewport
            DrawRect = new Rectangle (-5, -5, 30, 30)
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        view.Draw (context);

        // Points outside the viewport should still be in the clip (the drawn region was clamped).
        Assert.True (driver.Clip!.Contains (0, 0), "Points outside viewport should remain in clip even if drawn region extends beyond");
    }

    /// <summary>
    ///     Verifies that Border and Padding thickness areas are excluded from the clip even for
    ///     transparent views (they are always considered opaque).
    /// </summary>
    [Fact]
    public void TransparentView_ExcludesBorderAndPadding ()
    {
        IDriver driver = CreateTestDriver ();
        driver.Clip = new Region (driver.Screen);

        View view = new ()
        {
            X = 5,
            Y = 5,
            Width = 20,
            Height = 20,
            BorderStyle = LineStyle.Single,
            Driver = driver,
            ViewportSettings = ViewportSettingsFlags.Transparent
        };
        view.Padding!.Thickness = new Thickness (1);
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        view.Draw ();

        // The Border thickness area should be excluded (always opaque).
        Rectangle borderFrame = view.Border!.FrameToScreen ();

        // Top border line
        Assert.False (driver.Clip!.Contains (borderFrame.X + 1, borderFrame.Y), "Border top line should be excluded from clip for transparent view");

        // The Padding area should also be excluded.
        Rectangle paddingFrame = view.Padding!.FrameToScreen ();

        // Top padding line (just inside the border)
        Assert.False (driver.Clip.Contains (paddingFrame.X + 1, paddingFrame.Y), "Padding area should be excluded from clip for transparent view");
    }

    /// <summary>
    ///     Verifies that the opaque viewport exclusion in the per-layer DoDrawComplete path uses
    ///     an empty viewport location. A scrolled viewport must still exclude the on-screen viewport
    ///     area rather than an offset content rectangle.
    /// </summary>
    [Fact]
    public void OpaqueView_WithScrolledViewport_ExcludesOnScreenViewportArea ()
    {
        IDriver driver = CreateTestDriver ();
        driver.Clip = new Region (driver.Screen);

        View view = new ()
        {
            X = 5,
            Y = 5,
            Width = 12,
            Height = 12,
            BorderStyle = LineStyle.Single,
            Driver = driver
        };
        view.Border.ViewportSettings |= ViewportSettingsFlags.Transparent;
        view.SetContentSize (new Size (100, 100));
        view.ClearingViewport += (_, e) => e.Cancel = true;
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        view.Viewport = view.Viewport with { Location = new Point (10, 10) };

        Assert.NotEqual (Point.Empty, view.Viewport.Location);

        view.Draw ();

        Rectangle viewportScreen = view.ViewportToScreen (new Rectangle (Point.Empty, view.Viewport.Size));

        Assert.False (driver.Clip!.Contains (viewportScreen.X + 1, viewportScreen.Y + 1),
                      "Scrolled viewport interior should be excluded using an empty viewport location");
    }

    /// <summary>
    ///     Verifies that Adornment views (Margin, Border, Padding) do NOT modify Driver.Clip
    ///     in their own DoDrawComplete — their parent handles clip exclusion for them.
    /// </summary>
    [Fact]
    public void Adornment_SkipsClipExclusion ()
    {
        IDriver driver = CreateTestDriver ();
        Region initialClip = new (driver.Screen);
        driver.Clip = initialClip;

        View view = new ()
        {
            X = 5,
            Y = 5,
            Width = 20,
            Height = 20,
            BorderStyle = LineStyle.Single,
            Driver = driver
        };
        view.Margin!.Thickness = new Thickness (1);
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        // Capture clip before drawing just the Border adornment directly.
        Region clipBeforeBorderDraw = driver.Clip.Clone ();
        view.Border!.View!.Draw ();
        Region clipAfterBorderDraw = driver.Clip.Clone ();

        // Border's DoDrawComplete should NOT have modified the clip (Adornment guard).
        // Compare the clip rectangles — they should be identical.
        Assert.Equal (clipBeforeBorderDraw.GetBounds (), clipAfterBorderDraw.GetBounds ());

        // Now draw the full parent view — this SHOULD modify the clip.
        driver.Clip = new Region (driver.Screen);
        view.Draw ();

        // After parent draws, the Border area should be excluded (parent handled it).
        Rectangle borderFrame = view.Border.FrameToScreen ();
        Assert.False (driver.Clip!.Contains (borderFrame.X + 1, borderFrame.Y + 1), "Border area should be excluded from clip after parent Draw()");
    }

    /// <summary>
    ///     Verifies that when a transparent parent has an opaque child, the child's frame is
    ///     added to the parent's DrawContext via AddDrawnRectangle. This allows the parent's
    ///     DoDrawComplete to know which areas its children occupied.
    /// </summary>
    [Fact]
    public void TransparentParent_OpaqueChild_ContextFlows ()
    {
        IDriver driver = CreateTestDriver ();
        driver.Clip = new Region (driver.Screen);

        View parent = new ()
        {
            X = 0,
            Y = 0,
            Width = 40,
            Height = 40,
            Driver = driver,
            ViewportSettings = ViewportSettingsFlags.Transparent
        };

        View child = new () { X = 5, Y = 5, Width = 10, Height = 10 };
        parent.Add (child);
        parent.BeginInit ();
        parent.EndInit ();
        parent.LayoutSubViews ();

        DrawContext context = new ();
        parent.Draw (context);

        // The child's frame area should be in the context's drawn region,
        // because the opaque child called context.AddDrawnRectangle in its DoDrawComplete.
        Region drawnRegion = context.GetDrawnRegion ();
        Rectangle childFrameScreen = child.FrameToScreen ();

        Assert.True (drawnRegion.Contains (childFrameScreen.X + 1, childFrameScreen.Y + 1),
                     "Opaque child's frame should be in transparent parent's DrawContext");

        // An area of the parent NOT covered by the child should NOT be in the drawn region
        // (since the parent is transparent and didn't draw there itself).
        Assert.False (drawnRegion.Contains (0, 0), "Uncovered area of transparent parent should not be in DrawContext");
    }

    #region Phase 2: CachedDrawnRegion tests

    /// <summary>
    ///     Verifies that CachedDrawnRegion is populated after Draw() for a view with TransparentMouse set.
    ///     Requires Phase 2a (add _cachedDrawnRegion field) and 2b (cache in DoDrawComplete).
    /// </summary>
    [Fact]
    public void CachedDrawnRegion_PopulatedAfterDraw_WhenTransparentMouse ()
    {
        IDriver driver = CreateTestDriver ();
        driver.Clip = new Region (driver.Screen);

        View view = new ()
        {
            X = 5,
            Y = 5,
            Width = 10,
            Height = 10,
            Driver = driver,
            ViewportSettings = ViewportSettingsFlags.TransparentMouse
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        // Before draw, CachedDrawnRegion should be null.
        Assert.Null (view.CachedDrawnRegion);

        view.Draw ();

        // After draw, CachedDrawnRegion should be populated (opaque view draws its entire frame).
        Assert.NotNull (view.CachedDrawnRegion);
        Assert.False (view.CachedDrawnRegion!.IsEmpty ());
    }

    /// <summary>
    ///     Verifies that CachedDrawnRegion is NOT populated for views without TransparentMouse.
    ///     No point caching for views that won't be filtered during hit-testing.
    ///     Requires Phase 2a/2b.
    /// </summary>
    [Fact]
    public void CachedDrawnRegion_Null_WhenNotTransparentMouse ()
    {
        IDriver driver = CreateTestDriver ();
        driver.Clip = new Region (driver.Screen);

        View view = new ()
        {
            X = 5,
            Y = 5,
            Width = 10,
            Height = 10,
            Driver = driver
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        view.Draw ();

        // No TransparentMouse flag = no caching.
        Assert.Null (view.CachedDrawnRegion);
    }

    /// <summary>
    ///     Verifies that CachedDrawnRegion is cleared when SetNeedsDraw is called.
    ///     Requires Phase 2c (invalidate cache in SetNeedsDraw).
    /// </summary>
    [Fact]
    public void CachedDrawnRegion_ClearedBySetNeedsDraw ()
    {
        IDriver driver = CreateTestDriver ();
        driver.Clip = new Region (driver.Screen);

        View view = new ()
        {
            X = 5,
            Y = 5,
            Width = 10,
            Height = 10,
            Driver = driver,
            ViewportSettings = ViewportSettingsFlags.TransparentMouse
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        view.Draw ();
        Assert.NotNull (view.CachedDrawnRegion);

        // SetNeedsDraw should invalidate the cache.
        view.SetNeedsDraw ();
        Assert.Null (view.CachedDrawnRegion);
    }

    /// <summary>
    ///     Verifies that for a transparent view with TransparentMouse, CachedDrawnRegion contains
    ///     only the actually-drawn cells, not the entire frame.
    ///     Requires Phase 2a/2b.
    /// </summary>
    [Fact]
    public void CachedDrawnRegion_TransparentView_ContainsOnlyDrawnCells ()
    {
        IDriver driver = CreateTestDriver ();
        driver.Clip = new Region (driver.Screen);

        TransparentDrawingView view = new ()
        {
            X = 5,
            Y = 5,
            Width = 20,
            Height = 20,
            Driver = driver,
            ViewportSettings = ViewportSettingsFlags.Transparent | ViewportSettingsFlags.TransparentMouse,
            DrawRect = new Rectangle (2, 2, 5, 5)
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        view.Draw ();

        Assert.NotNull (view.CachedDrawnRegion);

        // The drawn rect (viewport 2,2 5x5 → screen 7,7 5x5) should be in the cached region.
        Rectangle drawnScreen = view.ViewportToScreen (view.DrawRect);
        Assert.True (view.CachedDrawnRegion!.Contains (drawnScreen.X + 1, drawnScreen.Y + 1),
                     "Drawn area should be in CachedDrawnRegion");

        // An undrawn area within the viewport should NOT be in the cached region.
        Assert.False (view.CachedDrawnRegion.Contains (view.ViewportToScreen (view.Viewport).X, view.ViewportToScreen (view.Viewport).Y),
                      "Undrawn area should not be in CachedDrawnRegion");
    }

    /// <summary>
    ///     Verifies that a Border adornment with TransparentMouse gets its CachedDrawnRegion populated
    ///     after Draw(). The cached region should contain the border line cells.
    ///     Requires Phase 2a/2b.
    /// </summary>
    [Fact]
    public void CachedDrawnRegion_BorderAdornment_PopulatedAfterDraw ()
    {
        IDriver driver = CreateTestDriver ();
        driver.Clip = new Region (driver.Screen);

        View view = new ()
        {
            X = 5,
            Y = 5,
            Width = 10,
            Height = 10,
            BorderStyle = LineStyle.Single,
            Driver = driver
        };
        view.Border!.ViewportSettings |= ViewportSettingsFlags.TransparentMouse;
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        Assert.Null (view.Border.CachedDrawnRegion);

        view.Draw ();

        // After draw, Border's CachedDrawnRegion should be populated with the border line cells.
        Assert.NotNull (view.Border.CachedDrawnRegion);
        Assert.False (view.Border.CachedDrawnRegion!.IsEmpty ());

        // A point on the top border line (screen coords) should be in the cached region.
        Rectangle borderFrame = view.Border.FrameToScreen ();
        Assert.True (view.Border.CachedDrawnRegion.Contains (borderFrame.X, borderFrame.Y),
                     "Top-left border line cell should be in Border's CachedDrawnRegion");
    }

    /// <summary>
    ///     Verifies that a Border's CachedDrawnRegion includes both border lines (from LineCanvas)
    ///     AND title text. Both are "drawn content" and should receive mouse events.
    ///     Requires Phase 2a/2b.
    /// </summary>
    [Fact]
    public void CachedDrawnRegion_Border_IncludesTitleAndLines ()
    {
        IDriver driver = CreateTestDriver ();
        driver.Clip = new Region (driver.Screen);

        View view = new ()
        {
            Title = "Test",
            X = 0,
            Y = 0,
            Width = 12,
            Height = 5,
            BorderStyle = LineStyle.Single,
            Driver = driver
        };
        view.Border!.ViewportSettings |= ViewportSettingsFlags.TransparentMouse;
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        view.Draw ();

        Assert.NotNull (view.Border.CachedDrawnRegion);

        // The top-left corner (a border line character) should be in the cached region.
        Rectangle borderFrame = view.Border.FrameToScreen ();
        Assert.True (view.Border.CachedDrawnRegion!.Contains (borderFrame.X, borderFrame.Y),
                     "Border line cell should be in CachedDrawnRegion");

        // The title "Test" starts after "┌┤" — at column 2 on the top border row.
        // The title text cells should also be in the cached region.
        Assert.True (view.Border.CachedDrawnRegion.Contains (borderFrame.X + 2, borderFrame.Y),
                     "Title text cell should be in CachedDrawnRegion");

        // Interior cell (inside the border, not on a line) should NOT be in the cached region.
        Assert.False (view.Border.CachedDrawnRegion.Contains (borderFrame.X + 2, borderFrame.Y + 2),
                      "Interior cell should not be in Border's CachedDrawnRegion");
    }

    /// <summary>
    ///     Verifies that ShadowView reports its drawn region to the DrawContext.
    ///     This is needed so that Margin's CachedDrawnRegion includes shadow cells.
    ///     Requires Phase 2d (ShadowView reports drawn region).
    /// </summary>
    [Fact]
    public void ShadowView_ReportsDrawnRegionToContext ()
    {
        IDriver driver = CreateTestDriver ();
        driver.Clip = new Region (driver.Screen);

        View view = new ()
        {
            X = 1,
            Y = 1,
            Width = 5,
            Height = 3,
            ShadowStyle = ShadowStyles.Opaque,
            Driver = driver
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        DrawContext context = new ();
        view.Draw (context);

        // The Margin should have a non-empty drawn region because ShadowView drew shadow cells.
        // The shadow occupies the right column and bottom row of the Margin.
        Region drawnRegion = context.GetDrawnRegion ();

        // The shadow is at the right (col 6) and bottom (row 4) relative to screen.
        // At minimum, some shadow cells should have been reported.
        Assert.False (drawnRegion.IsEmpty (), "DrawContext should contain shadow drawn region");
    }

    /// <summary>
    ///     Verifies that CachedDrawnRegion is repopulated after a redraw following invalidation.
    ///     Requires Phase 2a/2b/2c.
    /// </summary>
    [Fact]
    public void CachedDrawnRegion_RepopulatedAfterRedraw ()
    {
        IDriver driver = CreateTestDriver ();
        driver.Clip = new Region (driver.Screen);

        View view = new ()
        {
            X = 5,
            Y = 5,
            Width = 10,
            Height = 10,
            Driver = driver,
            ViewportSettings = ViewportSettingsFlags.TransparentMouse
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        // First draw — cache populated.
        view.Draw ();
        Assert.NotNull (view.CachedDrawnRegion);

        // Invalidate — cache cleared.
        view.SetNeedsDraw ();
        Assert.Null (view.CachedDrawnRegion);

        // Second draw — cache repopulated.
        driver.Clip = new Region (driver.Screen);
        view.Draw ();
        Assert.NotNull (view.CachedDrawnRegion);
        Assert.False (view.CachedDrawnRegion!.IsEmpty ());
    }

    #endregion

    /// <summary>
    ///     Helper view that draws a specific rectangle and reports it to DrawContext.
    ///     Used for testing transparent view clip exclusion.
    /// </summary>
    private class TransparentDrawingView : View
    {
        public Rectangle DrawRect { get; init; }

        protected override bool OnDrawingContent (DrawContext? context)
        {
            if (DrawRect != Rectangle.Empty)
            {
                FillRect (DrawRect, (Rune)'*');
                context?.AddDrawnRectangle (ViewportToScreen (DrawRect));
            }

            return true;
        }
    }
}
