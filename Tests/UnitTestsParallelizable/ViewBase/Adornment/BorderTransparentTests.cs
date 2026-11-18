// Claude - Opus 4.6

using System.Text;
using UnitTests;

// ReSharper disable StringLiteralTypo

namespace ViewBaseTests.Adornments;

/// <summary>
///     Tests for Border transparency support (Issue #4834).
///     Border should support ViewportSettingsFlags.Transparent and ViewportSettingsFlags.TransparentMouse.
/// </summary>
public class BorderTransparentTests (ITestOutputHelper output)
{
    /// <summary>
    ///     Verifies that a Border with Transparent set only draws border lines,
    ///     allowing underlying content to show through in the border gap cells.
    ///     With Thickness(1), border lines fill the entire thickness so there are no gap cells.
    ///     The View's content area is independently opaque (cleared to spaces).
    /// </summary>
    [Fact]
    public void Border_Transparent_Thickness1_View_Opaque_Same_As_Opaque_Border ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (7, 5);

        using Runnable window = new ();
        window.Width = Dim.Fill ();
        window.Height = Dim.Fill ();

        // Fill window background with 'X' to detect transparency
        window.ClearingViewport += (_, args) =>
                                   {
                                       window.FillRect (args.NewViewport, new Rune ('X'));
                                       args.Cancel = true;
                                   };

        View borderedView = new ()
        {
            X = 1,
            Y = 1,
            Width = 5,
            Height = 3,
            BorderStyle = LineStyle.Single
        };
        borderedView.Border!.ViewportSettings |= ViewportSettingsFlags.Transparent;

        window.Add (borderedView);
        app.Begin (window);

        // With Thickness(1), border lines fill the entire thickness — no gap cells.
        // The View's content area is opaque (View is not Transparent), so interior is spaces.
        // Result is identical to an opaque border.
        DriverAssert.AssertDriverContentsAre ("""

                                              XXXXXXX
                                              X┌───┐X
                                              X│   │X
                                              X└───┘X
                                              XXXXXXX
                                              """,
                                              output,
                                              app.Driver);
    }

    /// <summary>
    ///     Verifies that when BOTH the Border AND the View are transparent (Thickness 1),
    ///     the border lines are drawn but the View's content area shows through to
    ///     the underlying window content.
    /// </summary>
    [Fact]
    public void Border_Transparent_And_View_Transparent_Shows_Underlying_Content ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (7, 5);

        using Runnable window = new ();
        window.Width = Dim.Fill ();
        window.Height = Dim.Fill ();

        // Fill window background with 'X' to detect transparency
        window.ClearingViewport += (_, args) =>
                                   {
                                       window.FillRect (args.NewViewport, new Rune ('X'));
                                       args.Cancel = true;
                                   };

        View borderedView = new ()
        {
            X = 1,
            Y = 1,
            Width = 5,
            Height = 3,
            BorderStyle = LineStyle.Single,
            ViewportSettings = ViewportSettingsFlags.Transparent
        };
        borderedView.Border!.ViewportSettings |= ViewportSettingsFlags.Transparent;

        window.Add (borderedView);
        app.Begin (window);

        // Both View and Border are transparent.
        // Border lines are drawn (opaque). Content area shows through (transparent).
        DriverAssert.AssertDriverContentsAre ("""

                                              XXXXXXX
                                              X┌───┐X
                                              X│XXX│X
                                              X└───┘X
                                              XXXXXXX
                                              """,
                                              output,
                                              app.Driver);
    }

    [Fact]
    public void Border_Transparent_Shows_Underlying_Content_Where_Border_DrawContent_Are_Not ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (11, 9);

        using Runnable window = new ();
        window.Width = Dim.Fill ();
        window.Height = Dim.Fill ();

        // Fill window background with 'X' to detect transparency
        window.ClearingViewport += (_, args) =>
                                   {
                                       window.FillRect (args.NewViewport, new Rune ('X'));
                                       args.Cancel = true;
                                   };

        View borderedView = new ()
        {
            Title = "B",
            X = 2,
            Y = 2,
            Width = 7,
            Height = 5,
            BorderStyle = LineStyle.Single
        };
        borderedView.Border!.ViewportSettings |= ViewportSettingsFlags.Transparent;
        borderedView.Border.Thickness = new Thickness (2);

        window.Add (borderedView);
        app.Begin (window);

        // Gap cells in the thick border show X's (border is transparent, gap cells not drawn).
        // Content area (inside border lines) shows spaces (View is opaque, not transparent).
        // Border lines and title are drawn normally.
        DriverAssert.AssertDriverContentsAre ("""
                                              XXXXXXXXXXX
                                              XXXXXXXXXXX
                                              XXXX┌─┐XXXX
                                              XXX┌┘B└┐XXX
                                              XXX│   │XXX
                                              XXX└───┘XXX
                                              XXXXXXXXXXX
                                              XXXXXXXXXXX
                                              XXXXXXXXXXX
                                              """,
                                              output,
                                              app.Driver);
    }

    [Fact]
    public void Border_Transparent_Shows_Underlying_SubViews_Where_Border_DrawContent_Are_Not ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (11, 9);

        using Runnable window = new ();
        window.Width = Dim.Fill ();
        window.Height = Dim.Fill ();

        // Fill window background with 'X' to detect transparency
        window.ClearingViewport += (_, args) =>
                                   {
                                       window.FillRect (args.NewViewport, new Rune ('X'));
                                       args.Cancel = true;
                                   };

        View subView = new ()
        {
            Text = "sub",
            X = 1,
            Y = 2,
            Width = Dim.Auto (),
            Height = Dim.Auto ()
        };
        window.Add (subView);

        View borderedView = new ()
        {
            Title = "B",
            X = 2,
            Y = 2,
            Width = 7,
            Height = 5,
            BorderStyle = LineStyle.Single
        };
        borderedView.Border!.ViewportSettings |= ViewportSettingsFlags.Transparent;
        borderedView.Border.Thickness = new Thickness (2);

        window.Add (borderedView);
        app.Begin (window);

        // The subview "sub" shows through the transparent gap cells (it was drawn by the window before the borderedView).
        // The content area (inside border lines) shows spaces (View is opaque).
        // Border lines and title are drawn normally.
        DriverAssert.AssertDriverContentsAre ("""
                                              XXXXXXXXXXX
                                              XXXXXXXXXXX
                                              Xsub┌─┐XXXX
                                              XXX┌┘B└┐XXX
                                              XXX│   │XXX
                                              XXX└───┘XXX
                                              XXXXXXXXXXX
                                              XXXXXXXXXXX
                                              XXXXXXXXXXX
                                              """,
                                              output,
                                              app.Driver);
    }

    [Fact]
    public void Border_Transparent_Occludes_Underlying_SubViews_Where_Border_DrawContent_Is ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (11, 3);

        using Runnable window = new ();
        window.Width = Dim.Fill ();
        window.Height = Dim.Fill ();

        // Fill window background with 'X' to detect transparency
        window.ClearingViewport += (_, args) =>
                                   {
                                       window.FillRect (args.NewViewport, new Rune ('X'));
                                       args.Cancel = true;
                                   };

        View subView = new ()
        {
            Text = "subview",
            X = 0,
            Y = 1,
            Width = Dim.Auto (),
            Height = Dim.Auto ()
        };
        window.Add (subView);

        View borderedView = new ()
        {
            Title = "AB",
            X = 0,
            Y = 0,
            Width = 9,
            Height = 3,
            BorderStyle = LineStyle.Single
        };
        borderedView.Border!.ViewportSettings |= ViewportSettingsFlags.Transparent;
        borderedView.Border.Thickness = new Thickness (2, 3, 1, 0);

        window.Add (borderedView);
        app.Begin (window);

        DriverAssert.AssertDriverContentsAre ("""
                                              XX┌──┐XXXXX
                                              s┌┤AB├──┐XX
                                              X│└──┘XX│XX
                                              """,
                                              output,
                                              app.Driver);
    }

    /// <summary>
    ///     Verifies that mouse events in the transparent interior of a Border with TransparentMouse
    ///     pass through to views underneath.
    ///     Currently, passes — but only because the blanket TransparentMouse flag removes the Border
    ///     entirely from hit-testing. After per-cell TransparentMouse is implemented, this should
    ///     still pass because the interior cells were not drawn by the Border.
    /// </summary>
    [Fact]
    public void Border_TransparentMouse_Interior_Clicks_Pass_Through ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (10, 6);

        using Runnable window = new ();
        window.Width = Dim.Fill ();
        window.Height = Dim.Fill ();

        View borderedView = new ()
        {
            X = 2,
            Y = 1,
            Width = 5,
            Height = 3,
            BorderStyle = LineStyle.Single,
            Id = "Bordered"
        };
        borderedView.Border!.ViewportSettings |= ViewportSettingsFlags.TransparentMouse;

        window.Add (borderedView);
        app.Begin (window);

        // Screen position (4, 2) is in the interior of the bordered view —
        // inside the border outline but NOT on a border line.
        // With TransparentMouse, this should pass through to views underneath.
        List<View?> viewsUnderInterior = window.GetViewsUnderLocation (new Point (4, 2), ViewportSettingsFlags.TransparentMouse);

        // The bordered view's Border should NOT be in the hit list for interior points
        bool borderInList = viewsUnderInterior.Any (v => v is BorderView);

        Assert.False (borderInList, "Border with TransparentMouse should not capture mouse events in its transparent interior");
    }

    /// <summary>
    ///     Verifies that mouse events ON the border lines of a Border with TransparentMouse
    ///     are still captured by the Border (only the transparent interior passes through).
    ///     Requires Phase 2e (drawn-region-aware hit-testing).
    /// </summary>
    [Fact]
    public void Border_TransparentMouse_BorderLine_Clicks_Are_Captured ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (10, 6);

        using Runnable window = new ();
        window.Width = Dim.Fill ();
        window.Height = Dim.Fill ();

        View borderedView = new ()
        {
            X = 2,
            Y = 1,
            Width = 5,
            Height = 3,
            BorderStyle = LineStyle.Single,
            Id = "Bordered"
        };
        borderedView.Border!.ViewportSettings |= ViewportSettingsFlags.TransparentMouse;

        window.Add (borderedView);
        app.Begin (window);

        // Click the top-left corner of the border (screen position 2, 1)
        // This IS on a border line, so it should be captured by the Border.
        List<View?> viewsOnBorderLine = window.GetViewsUnderLocation (new Point (2, 1), ViewportSettingsFlags.TransparentMouse);

        bool borderInList = viewsOnBorderLine.Any (v => v is BorderView);

        Assert.True (borderInList, "Border with TransparentMouse should still capture mouse events on its drawn border lines");
    }

    /// <summary>
    ///     Verifies that mouse events on the title text of a Border with TransparentMouse
    ///     are captured by the Border. The title is drawn content and should receive clicks.
    ///     Requires Phase 2e.
    /// </summary>
    [Fact]
    public void Border_TransparentMouse_Title_Clicks_Are_Captured ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (12, 5);

        using Runnable window = new ();
        window.Width = Dim.Fill ();
        window.Height = Dim.Fill ();

        View borderedView = new ()
        {
            Title = "Test",
            X = 1,
            Y = 1,
            Width = 10,
            Height = 3,
            BorderStyle = LineStyle.Single,
            Id = "Bordered"
        };
        borderedView.Border!.ViewportSettings |= ViewportSettingsFlags.TransparentMouse;

        window.Add (borderedView);
        app.Begin (window);

        // The title "Test" is drawn at the top border line. Click on the 'T' of "Test".
        // Screen position: border starts at (1,1), title starts at col 3 (after "┌┤").
        List<View?> viewsOnTitle = window.GetViewsUnderLocation (new Point (3, 1), ViewportSettingsFlags.TransparentMouse);

        bool borderInList = viewsOnTitle.Any (v => v is BorderView);

        Assert.True (borderInList, "Border with TransparentMouse should capture mouse events on its title text");
    }

    /// <summary>
    ///     Verifies that a Margin with TransparentMouse and a shadow passes mouse events through
    ///     empty margin cells but captures them on shadow cells.
    ///     Requires Phase 2d (ShadowView reports drawn region) and 2e (drawn-region-aware hit-testing).
    /// </summary>
    [Fact (Skip = "Phase 2d/2e — ShadowView drawn region and hit-testing not yet implemented")]
    public void Margin_TransparentMouse_Shadow_Clicks_Are_Captured ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (10, 6);

        using Runnable window = new ();
        window.Width = Dim.Fill ();
        window.Height = Dim.Fill ();

        View shadowView = new ()
        {
            X = 1,
            Y = 1,
            Width = 5,
            Height = 3,
            ShadowStyle = ShadowStyles.Opaque,
            Id = "Shadowed"
        };

        window.Add (shadowView);
        app.Begin (window);

        // The shadow is drawn on the right (col 6) and bottom (row 4) of the view.
        // The Margin has TransparentMouse set by default.
        // Click on a shadow cell — should be captured (shadow drew there).
        // The shadow is drawn by two ShadowViews:
        // - Vertical: at the right edge of the Margin (x=5 in screen coords for view at x=1, w=5)
        // - Horizontal: at the bottom edge of the Margin (y=3 in screen coords for view at y=1, h=3)
        // Click on the vertical shadow cell at screen position (5, 2) — should be captured.
        List<View?> viewsOnShadow = window.GetViewsUnderLocation (new Point (5, 2), ViewportSettingsFlags.TransparentMouse);
        bool marginInList = viewsOnShadow.Any (v => v is MarginView);

        Assert.True (marginInList, "Margin with shadow should capture mouse events on shadow cells");

        // Click on an empty margin cell (if any) — should pass through.
        // With default Margin thickness of 0 + shadow thickness of 1, the empty margin area
        // is minimal. Skip this sub-assertion if there's no empty margin area to test.
    }

    /// <summary>
    ///     Verifies that drawn-region-aware hit-testing works for a regular View with TransparentMouse.
    ///     A view that draws only part of its area should pass through clicks on undrawn cells
    ///     but capture clicks on drawn cells.
    ///     Requires Phase 2a/2b/2e.
    /// </summary>
    [Fact]
    public void View_TransparentMouse_DrawnCells_Captured_UndrawnCells_PassThrough ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (20, 20);

        using Runnable window = new ();
        window.Width = Dim.Fill ();
        window.Height = Dim.Fill ();

        // A view that draws only a small rectangle in its viewport.
        View partialView = new ()
        {
            X = 5,
            Y = 5,
            Width = 10,
            Height = 10,
            Id = "Partial",
            ViewportSettings = ViewportSettingsFlags.Transparent | ViewportSettingsFlags.TransparentMouse
        };

        // Draw a 3x3 block at viewport (1, 1)
        partialView.DrawingContent += (_, args) =>
                                      {
                                          partialView.FillRect (new Rectangle (1, 1, 3, 3), new Rune ('#'));
                                          args.DrawContext?.AddDrawnRectangle (partialView.ViewportToScreen (new Rectangle (1, 1, 3, 3)));
                                      };

        window.Add (partialView);
        app.Begin (window);

        // Screen (7, 7) is inside the drawn 3x3 block (viewport 1,1 + view origin 5,5 + viewport offset).
        // This should be captured by the view.
        List<View?> viewsOnDrawnCell = window.GetViewsUnderLocation (new Point (7, 7), ViewportSettingsFlags.TransparentMouse);
        bool partialInList = viewsOnDrawnCell.Any (v => v?.Id == "Partial");
        Assert.True (partialInList, "TransparentMouse view should capture clicks on drawn cells");

        // Screen (5, 5) is the top-left of the view's viewport — undrawn area.
        // This should pass through.
        List<View?> viewsOnUndrawnCell = window.GetViewsUnderLocation (new Point (5, 5), ViewportSettingsFlags.TransparentMouse);
        bool partialInListUndrawn = viewsOnUndrawnCell.Any (v => v?.Id == "Partial");
        Assert.False (partialInListUndrawn, "TransparentMouse view should pass through clicks on undrawn cells");
    }

    /// <summary>
    ///     A transparent overlay with TransparentMouse must not capture mouse events on cells
    ///     drawn by a peer SubView. The overlay's CachedDrawnRegion must reflect only what the
    ///     overlay itself drew, not what leaked into the shared DrawContext from peers.
    /// </summary>
    [Fact]
    public void TransparentMouse_Overlay_Does_Not_Capture_Peer_SubView_Drawn_Cells ()
    {
        // Claude - Opus 4.6
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (20, 10);

        using Runnable top = new ();
        top.Width = Dim.Fill ();
        top.Height = Dim.Fill ();

        // Opaque button-like view at top-left
        View button = new ()
        {
            X = 0, Y = 0, Width = 8, Height = 3, Id = "Button"
        };

        // Transparent overlay covering the whole top area.
        // Draws a 2x2 indicator at viewport (15, 1) — far from the button.
        View overlay = new ()
        {
            X = 0, Y = 0, Width = 20, Height = 10, Id = "Overlay",
            ViewportSettings = ViewportSettingsFlags.Transparent | ViewportSettingsFlags.TransparentMouse
        };

        overlay.DrawingContent += (_, args) =>
                                  {
                                      overlay.FillRect (new Rectangle (15, 1, 2, 2), new Rune ('*'));
                                      args.DrawContext?.AddDrawnRectangle (overlay.ViewportToScreen (new Rectangle (15, 1, 2, 2)));
                                  };

        // Add order matters for Z-order: button first (behind), overlay second (in front).
        top.Add (button, overlay);
        app.Begin (top);

        // 1. Click on the overlay's indicator (screen 15,1) — overlay should capture.
        List<View?> viewsOnIndicator = top.GetViewsUnderLocation (new Point (15, 1), ViewportSettingsFlags.TransparentMouse);
        Assert.Contains (viewsOnIndicator, v => v?.Id == "Overlay");

        // 2. Click on the button area (screen 2,1) — overlay should NOT capture.
        //    The button drew there, but that's the button's content, not the overlay's.
        List<View?> viewsOnButton = top.GetViewsUnderLocation (new Point (2, 1), ViewportSettingsFlags.TransparentMouse);
        Assert.DoesNotContain (viewsOnButton, v => v?.Id == "Overlay");

        // 3. Click on empty area (screen 10,5) — overlay should NOT capture.
        List<View?> viewsOnEmpty = top.GetViewsUnderLocation (new Point (10, 5), ViewportSettingsFlags.TransparentMouse);
        Assert.DoesNotContain (viewsOnEmpty, v => v?.Id == "Overlay");

        app.End (app.SessionStack?.FirstOrDefault ()!);
    }

    /// <summary>
    ///     Verifies that when CachedDrawnRegion is null (before first draw), TransparentMouse
    ///     falls back to blanket removal (no regression from current behavior).
    ///     Requires Phase 2e.
    /// </summary>
    [Fact]
    public void View_TransparentMouse_NullCache_FallsBackToBlanketRemoval ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (20, 20);

        using Runnable window = new ();
        window.Width = Dim.Fill ();
        window.Height = Dim.Fill ();

        View transparentMouseView = new ()
        {
            X = 5,
            Y = 5,
            Width = 10,
            Height = 10,
            Id = "TMView",
            ViewportSettings = ViewportSettingsFlags.TransparentMouse
        };

        window.Add (transparentMouseView);

        // Do NOT call app.Begin — no draw has occurred, so CachedDrawnRegion is null.
        // Manually init and layout so GetViewsUnderLocation can work.
        window.BeginInit ();
        window.EndInit ();
        window.LayoutSubViews ();

        // With null cache, TransparentMouse should fall back to blanket removal.
        List<View?> views = window.GetViewsUnderLocation (new Point (7, 7), ViewportSettingsFlags.TransparentMouse);
        bool tmViewInList = views.Any (v => v?.Id == "TMView");

        Assert.False (tmViewInList, "View with TransparentMouse and null CachedDrawnRegion should be removed (blanket fallback)");
    }

    /// <summary>
    ///     Verifies that a Border with TransparentMouse on a thick border correctly distinguishes
    ///     between drawn border line cells and empty cells in the border thickness area.
    ///     Requires Phase 2a/2b/2e.
    /// </summary>
    [Fact]
    public void Border_TransparentMouse_ThickBorder_EmptyCells_PassThrough ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (12, 10);

        using Runnable window = new ();
        window.Width = Dim.Fill ();
        window.Height = Dim.Fill ();

        View borderedView = new ()
        {
            X = 1,
            Y = 1,
            Width = 9,
            Height = 7,
            BorderStyle = LineStyle.Single,
            Id = "ThickBordered"
        };
        borderedView.Border!.Thickness = new Thickness (2);
        borderedView.Border.ViewportSettings |= ViewportSettingsFlags.TransparentMouse;

        window.Add (borderedView);
        app.Begin (window);

        // With thickness=2, GetBorderRectangle() places the line at offset 1 from the Border viewport edge.
        // View at (1,1,9,7) → Border frame at (1,1,9,7) → border line rectangle at (2,2,7,5).
        // Click on the top-left corner of the border line (screen 2,2) — should be captured.
        List<View?> viewsOnBorderLine = window.GetViewsUnderLocation (new Point (2, 2), ViewportSettingsFlags.TransparentMouse);
        bool borderCaptured = viewsOnBorderLine.Any (v => v is BorderView);
        Assert.True (borderCaptured, "Click on border line should be captured");

        // Click on an empty cell in the outer thickness gap (screen 1,1) — inside the border frame
        // but NOT on a drawn line. This should pass through.
        List<View?> viewsOnGap = window.GetViewsUnderLocation (new Point (1, 1), ViewportSettingsFlags.TransparentMouse);
        bool borderCapturedOnGap = viewsOnGap.Any (v => v is BorderView);
        Assert.False (borderCapturedOnGap, "Click on empty gap in border thickness should pass through");
    }

    /// <summary>
    ///     Verifies that a Border SubView positioned with Pos.AnchorEnd renders at the bottom
    ///     of the border, and repositions correctly when the parent view is resized.
    /// </summary>
    [Fact]
    public void Border_SubView_AnchorEnd_Renders_At_Bottom_Before_And_After_Resize ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (7, 8);

        using Runnable window = new ();
        window.Width = Dim.Fill ();
        window.Height = Dim.Fill ();

        View borderedView = new ()
        {
            X = 0,
            Y = 0,
            Width = 7,
            Height = 6,
            BorderStyle = LineStyle.Single
        };
        borderedView.Border!.Thickness = new Thickness (1, 1, 1, 2);

        View borderSubView = new ()
        {
            X = 0,
            Y = Pos.AnchorEnd (),
            Width = Dim.Fill (),
            Height = 1,
            Text = "ZZZ"
        };
        borderedView.Border.GetOrCreateView ().Add (borderSubView);

        window.Add (borderedView);
        app.Begin (window);

        // Height=6, border top=1, bottom=2: 3 content rows.
        // Bottom thickness 2 = border line + subview row below it.
        // "ZZZ" should render at the last row of the border (below └─────┘).
        DriverAssert.AssertDriverContentsAre ("""
                                              ┌─────┐
                                              │     │
                                              │     │
                                              │     │
                                              └─────┘
                                              ZZZ
                                              """,
                                              output,
                                              app.Driver);

        // Resize: shrink height from 6 to 4. Now 1 content row.
        borderedView.Height = 4;
        app.LayoutAndDraw ();

        // "ZZZ" should now be at the new bottom.
        DriverAssert.AssertDriverContentsAre ("""
                                              ┌─────┐
                                              │     │
                                              └─────┘
                                              ZZZ
                                              """,
                                              output,
                                              app.Driver);
    }

    /// <summary>
    ///     Verifies that when an adornment (Padding) is transparent and has Text,
    ///     the Text is opaque (drawn cells occlude underlying peer SubViews),
    ///     while undrawn cells in the Padding are transparent and show peer SubViews through.
    /// </summary>
    [Fact]
    public void Padding_Transparent_Text_Is_Opaque_Over_Peer_SubViews ()
    {
        // Copilot
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (12, 6);

        using Runnable window = new ();
        window.Width = Dim.Fill ();
        window.Height = Dim.Fill ();

        // Fill window background with 'X' to detect transparency
        window.ClearingViewport += (_, args) =>
                                   {
                                       window.FillRect (args.NewViewport, new Rune ('X'));
                                       args.Cancel = true;
                                   };

        // A peer subview that will overlap with borderedView's thick padding area
        // "PEER" at screen (1,1) will be entirely in the padding band
        View peerView = new ()
        {
            Text = "PEER",
            X = 1,
            Y = 1,
            Width = Dim.Auto (),
            Height = Dim.Auto ()
        };
        window.Add (peerView);

        // A view with transparent thick top-padding that has text
        View borderedView = new ()
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 5,
            BorderStyle = LineStyle.Single
        };

        // Padding only on top (2 cells thick) to keep geometry simple
        borderedView.Padding!.ViewportSettings |= ViewportSettingsFlags.Transparent;
        borderedView.Padding.Thickness = new Thickness (0, 2, 0, 0);
        borderedView.Padding.GetOrCreateView ();
        borderedView.Padding.View!.Text = "Pad";

        window.Add (borderedView);
        app.Begin (window);

        // Layout (12x6):
        //
        //        0123456789AB
        // Row 0: ┌────────┐XX  border top
        // Row 1: │Pad     │XX  padding row 1: "Pad" text is OPAQUE; cols 4+ are transparent → peer "R" + window "X"s show
        // Row 2: │        │XX  padding row 2: all transparent → X's from window show through
        // Row 3: │        │XX  content area (opaque)
        // Row 4: └────────┘XX  border bottom
        // Row 5: XXXXXXXXXXXX  window background
        //
        // Peer "PEER" at (1,1): P(1,1) E(2,1) E(3,1) R(4,1)
        // Padding text "Pad" at padding viewport (0,0): P→screen(1,1) a→screen(2,1) d→screen(3,1)
        //
        // At screen (1,1): Padding drew "P" → opaque → "P"
        // At screen (2,1): Padding drew "a" → opaque → "a"
        // At screen (3,1): Padding drew "d" → opaque → "d"
        // At screen (4,1): Padding undrawn → transparent → peer "R" shows through → "R"
        // At screen (5-8,1): Padding undrawn → transparent → window "X" shows through
        //
        // Row 2: Padding undrawn → transparent → window "X" should show through
        //
        // This test currently FAILS because transparent padding text does not occlude properly.

        DriverAssert.AssertDriverContentsAre ("""
                                              ┌────────┐XX
                                              │PadRXXXX│XX
                                              │XXXXXXXX│XX
                                              │        │XX
                                              └────────┘XX
                                              XXXXXXXXXXXX
                                              """,
                                              output,
                                              app.Driver);
    }

    /// <summary>
    ///     Verifies that a Margin with Transparent draws text opaquely over peer content,
    ///     while undrawn cells show the underlying content.
    ///     Currently skipped because transparent non-shadow Margin rendering uses a separate
    ///     pass (MarginView.DrawMargins) that only supports shadow compositing.
    /// </summary>
    [Fact (Skip = "Transparent non-shadow Margin text rendering not yet supported in first-pass drawing")]
    public void Margin_Transparent_Text_Is_Opaque_Over_Peer_SubViews ()
    {
        // Copilot
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (12, 5);

        using Runnable window = new ();
        window.Width = Dim.Fill ();
        window.Height = Dim.Fill ();

        // Fill window background with 'X'
        window.ClearingViewport += (_, args) =>
                                    {
                                        window.FillRect (args.NewViewport, new Rune ('X'));
                                        args.Cancel = true;
                                    };

        // A peer subview behind the margin area
        View peerView = new ()
        {
            Text = "PEER",
            X = 0,
            Y = 0,
            Width = Dim.Auto (),
            Height = Dim.Auto ()
        };
        window.Add (peerView);

        // A view with a transparent thick top-margin that has text
        View borderedView = new ()
        {
            X = 0,
            Y = 0,
            Width = 8,
            Height = 4,
            BorderStyle = LineStyle.Single
        };

        // Margin on top (1 cell thick)
        borderedView.Margin!.ViewportSettings |= ViewportSettingsFlags.Transparent;
        borderedView.Margin.Thickness = new Thickness (0, 1, 0, 0);
        borderedView.Margin.GetOrCreateView ();
        borderedView.Margin.View!.Text = "Mg";

        window.Add (borderedView);
        app.Begin (window);

        // Layout (12x5):
        //        0123456789AB
        // Row 0: MgRRXXXXXXXX  margin row: "Mg" opaque, cols 2-3→peer "ER", cols 4+→window "X"
        // Row 1: ┌──────┐XXXXX  border top
        // Row 2: │      │XXXXX  content area (opaque)
        // Row 3: └──────┘XXXXX  border bottom
        // Row 4: XXXXXXXXXXXX   window background
        //
        // Peer "PEER" at (0,0): P(0,0) E(1,0) E(2,0) R(3,0)
        // Margin text "Mg" at margin viewport (0,0): M→screen(0,0) g→screen(1,0)
        //
        // At screen (0,0): Margin drew "M" → opaque
        // At screen (1,0): Margin drew "g" → opaque
        // At screen (2,0): Margin undrawn → transparent → peer "E" shows through
        // At screen (3,0): Margin undrawn → transparent → peer "R" shows through
        // At screen (4-7,0): Margin undrawn → transparent → window "X" shows through

        DriverAssert.AssertDriverContentsAre ("""
                                              MgERXXXXXXXX
                                              ┌──────┐XXXXX
                                              │      │XXXXX
                                              └──────┘XXXXX
                                              XXXXXXXXXXXX
                                              """,
                                              output,
                                              app.Driver);
    }

    /// <summary>
    ///     Verifies that Padding with TransparentMouse has a CachedDrawnRegion that only
    ///     contains the drawn text cells, not the full padding area.
    /// </summary>
    [Fact]
    public void Padding_TransparentMouse_CachedDrawnRegion_Contains_Only_Drawn_Cells ()
    {
        // Copilot
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (12, 6);

        using Runnable window = new ();
        window.Width = Dim.Fill ();
        window.Height = Dim.Fill ();

        View borderedView = new ()
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 5,
            BorderStyle = LineStyle.Single,
            Id = "Bordered"
        };

        // TransparentMouse padding with text
        borderedView.Padding!.ViewportSettings |= ViewportSettingsFlags.TransparentMouse;
        borderedView.Padding.Thickness = new Thickness (0, 2, 0, 0);
        borderedView.Padding.GetOrCreateView ();
        borderedView.Padding.View!.Text = "Pad";

        window.Add (borderedView);
        app.Begin (window);

        // The Padding's CachedDrawnRegion should contain only the "Pad" text cells,
        // not the full padding area.
        Region? cachedRegion = borderedView.Padding.CachedDrawnRegion;

        Assert.NotNull (cachedRegion);

        // "Pad" is drawn at padding viewport (0,0), which maps to screen (1,1)
        // P at (1,1), a at (2,1), d at (3,1)
        Assert.True (cachedRegion.Contains (1, 1), "CachedDrawnRegion should contain 'P' at (1,1)");
        Assert.True (cachedRegion.Contains (2, 1), "CachedDrawnRegion should contain 'a' at (2,1)");
        Assert.True (cachedRegion.Contains (3, 1), "CachedDrawnRegion should contain 'd' at (3,1)");

        // Undrawn padding cells should NOT be in CachedDrawnRegion
        Assert.False (cachedRegion.Contains (5, 1), "CachedDrawnRegion should not contain undrawn cell (5,1)");
        Assert.False (cachedRegion.Contains (5, 2), "CachedDrawnRegion should not contain undrawn cell (5,2)");
    }

    /// <summary>
    ///     Verifies that Margin with TransparentMouse passes clicks through undrawn cells
    ///     but captures clicks on drawn text.
    ///     Currently skipped because transparent non-shadow Margin rendering uses a separate
    ///     pass that doesn't track drawn regions for mouse hit-testing.
    /// </summary>
    [Fact (Skip = "Transparent non-shadow Margin mouse hit-testing not yet supported")]
    public void Margin_TransparentMouse_DrawnText_Captured_UndrawnCells_PassThrough ()
    {
        // Copilot
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (12, 5);

        using Runnable window = new ();
        window.Width = Dim.Fill ();
        window.Height = Dim.Fill ();

        View borderedView = new ()
        {
            X = 0,
            Y = 0,
            Width = 8,
            Height = 4,
            BorderStyle = LineStyle.Single,
            Id = "Bordered"
        };

        // Transparent thick margin with text
        borderedView.Margin!.ViewportSettings |= ViewportSettingsFlags.TransparentMouse;
        borderedView.Margin.Thickness = new Thickness (0, 1, 0, 0);
        borderedView.Margin.GetOrCreateView ();
        borderedView.Margin.View!.Text = "Mg";

        window.Add (borderedView);
        app.Begin (window);

        // Layout:
        //        01234567
        // Row 0: MgXXXXXX  margin row: "Mg" text drawn, cols 2+ undrawn
        // Row 1: ┌──────┐  border top
        // Row 2: │      │  content area
        // Row 3: └──────┘  border bottom
        //
        // Click on "M" at screen (0,0) — should be captured by MarginView
        List<View?> viewsOnText = window.GetViewsUnderLocation (new Point (0, 0), ViewportSettingsFlags.TransparentMouse);

        Assert.True (viewsOnText.Count > 0, "Clicking on margin text should find views");

        // Click on undrawn margin cell at screen (4,0) — should pass through
        List<View?> viewsOnEmpty = window.GetViewsUnderLocation (new Point (4, 0), ViewportSettingsFlags.TransparentMouse);
        bool marginNotInEmpty = !viewsOnEmpty.Any (v => v?.Id == borderedView.Margin.View!.Id);

        Assert.True (marginNotInEmpty, "Clicking on undrawn margin cell with TransparentMouse should pass through");
    }

    /// <summary>
    ///     Verifies that when Border and Padding are both transparent,
    ///     each layer's drawn text is opaque while undrawn cells show through to the underlying content.
    /// </summary>
    [Fact]
    public void Border_And_Padding_Transparent_Text_Is_Opaque ()
    {
        // Copilot
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (14, 7);

        using Runnable window = new ();
        window.Width = Dim.Fill ();
        window.Height = Dim.Fill ();

        // Fill window background with 'X'
        window.ClearingViewport += (_, args) =>
                                    {
                                        window.FillRect (args.NewViewport, new Rune ('X'));
                                        args.Cancel = true;
                                    };

        View borderedView = new ()
        {
            X = 0,
            Y = 0,
            Width = 12,
            Height = 6,
            BorderStyle = LineStyle.Single,
            Text = "Hi"
        };

        // Transparent border
        borderedView.Border!.ViewportSettings |= ViewportSettingsFlags.Transparent;

        // Transparent view content (so underlying X's show through content area)
        borderedView.ViewportSettings |= ViewportSettingsFlags.Transparent;

        // Transparent padding (top 1)
        borderedView.Padding!.ViewportSettings |= ViewportSettingsFlags.Transparent;
        borderedView.Padding.Thickness = new Thickness (0, 1, 0, 0);
        borderedView.Padding.GetOrCreateView ();
        borderedView.Padding.View!.Text = "Pd";

        window.Add (borderedView);
        app.Begin (window);

        // Layout (14x7):
        //        01234567890123
        // Row 0: ┌──────────┐XX  border top line: opaque (drawn line chars)
        // Row 1: │PdXXXXXXXX│XX  padding: "Pd" opaque, rest transparent → "X"; border sides opaque
        // Row 2: │HiXXXXXXXX│XX  content: transparent → "Hi" opaque, rest "X"; border sides opaque
        // Row 3: │XXXXXXXXXX│XX  content row: transparent → all "X"
        // Row 4: │XXXXXXXXXX│XX
        // Row 5: └──────────┘XX  border bottom line: opaque
        // Row 6: XXXXXXXXXXXXXX  window background

        DriverAssert.AssertDriverContentsAre ("""
                                              ┌──────────┐XX
                                              │PdXXXXXXXX│XX
                                              │HiXXXXXXXX│XX
                                              │XXXXXXXXXX│XX
                                              │XXXXXXXXXX│XX
                                              └──────────┘XX
                                              XXXXXXXXXXXXXX
                                              """,
                                              output,
                                              app.Driver);
    }
}
