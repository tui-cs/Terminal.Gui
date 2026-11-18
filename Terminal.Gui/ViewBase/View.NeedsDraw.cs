namespace Terminal.Gui.ViewBase;

public partial class View
{
    // NOTE: NeedsDrawRect is in viewport-LOCAL coordinates: (0, 0) is the top-left visible
    //       cell of the View's Viewport (inside Padding, after any scroll). The rect is
    //       independent of Viewport.Location, so scrolling and negative viewport locations do
    //       not bleed into the dirty rect. See SetNeedsDraw(Rectangle) for the cascade
    //       convention used to propagate dirty rects into SubViews.
    // NOTE: Consider changing NeedsDrawRect from Rectangle to Region for more precise invalidation
    //       NeedsDraw is already efficiently cached via NeedsDrawRect. It checks:
    //       1. NeedsDrawRect (cached by SetNeedsDraw/ClearNeedsDraw)
    //       2. Adornment NeedsDraw flags (each cached separately)
    /// <summary>
    ///     INTERNAL: Gets the viewport-local region that needs to be redrawn. <c>(0, 0)</c> is the
    ///     top-left of <see cref="Viewport"/>; the rect is independent of <see cref="Viewport"/>'s
    ///     <see cref="Rectangle.Location"/> (scroll offset).
    /// </summary>
    internal Rectangle NeedsDrawRect { get; private set; } = Rectangle.Empty;

    /// <summary>Gets whether the view needs to be redrawn.</summary>
    /// <remarks>
    ///     <para>
    ///         Will be <see langword="true"/> if the <see cref="NeedsLayout"/> property is <see langword="true"/> or if
    ///         any part of the view's <see cref="Viewport"/> needs to be redrawn.
    ///     </para>
    /// </remarks>
    public bool NeedsDraw =>
        Visible && (NeedsDrawRect != Rectangle.Empty || Margin.View?.NeedsDraw == true || Border.View?.NeedsDraw == true || Padding.View?.NeedsDraw == true);

    /// <summary>
    ///     Sets <see cref="NeedsDraw"/> to <see langword="true"/> indicating the <see cref="Viewport"/> of this View needs to
    ///     be redrawn.
    /// </summary>
    /// <remarks>
    ///     If the view is not visible (<see cref="Visible"/> is <see langword="false"/>), this method
    ///     does nothing.
    /// </remarks>
    public void SetNeedsDraw ()
    {
        Rectangle viewport = Viewport;

        if (!Visible || (NeedsDrawRect != Rectangle.Empty && viewport.IsEmpty))
        {
            // This handles the case where the view has not been initialized yet
            return;
        }

        // Pass a viewport-LOCAL rect: (0, 0, W, H) covers the whole visible viewport regardless
        // of scroll. Passing Viewport here would leak Viewport.Location (the scroll offset, which
        // can also be negative under AllowNegativeX/Y) into NeedsDrawRect, breaking the
        // viewport-local convention NeedsDrawRect is supposed to honor.
        SetNeedsDraw (new (Point.Empty, viewport.Size));
    }

    /// <summary>Expands the area of this view needing to be redrawn to include <paramref name="viewPortRelativeRegion"/>.</summary>
    /// <remarks>
    ///     <para>
    ///         <paramref name="viewPortRelativeRegion"/> is in viewport-LOCAL coordinates:
    ///         <c>(0, 0)</c> is the top-left visible cell of the View's <see cref="Viewport"/>.
    ///         The rect does NOT include <see cref="Viewport"/>'s <see cref="Rectangle.Location"/>
    ///         (scroll offset).
    ///     </para>
    ///     <para>
    ///         The cascade to intersecting SubViews translates the region into each SubView's
    ///         own viewport-local coordinates, accounting for the parent's scroll and the SubView's
    ///         adornments. Viewport-local coordinates are scroll-independent, so the SubView's own
    ///         <see cref="Viewport"/> <see cref="Rectangle.Location"/> does not affect the propagated rect.
    ///     </para>
    ///     <para>
    ///         If the view has not been initialized (<see cref="IsInitialized"/> is <see langword="false"/>), the area to be
    ///         redrawn will be the <paramref name="viewPortRelativeRegion"/>.
    ///     </para>
    /// </remarks>
    /// <param name="viewPortRelativeRegion">The viewport-local region that needs to be redrawn.</param>
    public void SetNeedsDraw (Rectangle viewPortRelativeRegion)
    {
        // Invalidate the cached drawn region used for TransparentMouse hit-testing.
        // It will be repopulated on the next Draw() pass.
        CachedDrawnRegion = null;

        // If we are at the top of the hierarchy, we're a runnable,
        // and we need to ensure any other Runnables get redrawn.
        if (App?.TopRunnableView == this && App is { })
        {
            //App?.ClearScreenNextIteration = true;
            List<View?> runnables = [.. App?.SessionStack?.Select (r => r.Runnable as View).Where (v => v != this && v?.NeedsDraw == false)!];

            foreach (View? runnable in runnables)
            {
                runnable?.SetNeedsDraw ();
            }
        }

        if (!Visible)
        {
            return;
        }

        if (NeedsDrawRect.IsEmpty)
        {
            NeedsDrawRect = viewPortRelativeRegion;
        }
        else
        {
            // Union NeedsDrawRect with the incoming region. The previous formula unioned
            // against Viewport (a bug — it widened to nearly viewport-size on every call),
            // which made NeedsDrawRect useless for narrowing draw work. Issue #5358
            // requires an accurate dirty rect so the region-aware ClearViewport and
            // SetNeedsDraw cascade can stay narrow.
            NeedsDrawRect = Rectangle.Union (NeedsDrawRect, viewPortRelativeRegion);
        }

        // Do not set on Margin - it will be drawn in a separate pass.

        if (Border.Thickness != Thickness.Empty)
        {
            Border.View?.SetNeedsDraw ();
        }

        if (Padding.Thickness != Thickness.Empty)
        {
            Padding.View?.SetNeedsDraw ();
        }

        SuperView?.SetSubViewNeedsDrawDownHierarchy ();

        if (this is AdornmentView adornment)
        {
            adornment.Adornment?.Parent?.SetSubViewNeedsDrawDownHierarchy ();
        }

        // Cascade the dirty region into intersecting SubViews. Coordinate conversion chain
        // (issue #5359 — every step is needed for correctness under scroll/adornments):
        //   1. viewPortRelativeRegion is in THIS view's viewport-local coords; translate to
        //      this view's content coords by adding Viewport.Location (the scroll offset).
        //   2. subview.Frame is in this view's content coords; intersect there.
        //   3. Translate the intersection to subview-frame-local (subtract Frame.Location).
        //   4. Translate to subview-VIEWPORT-local by subtracting the subview's adornment
        //      offset (GetViewportOffsetFromFrame() — the combined Margin/Border/Padding inset).
        //      Do NOT also subtract the subview's Viewport.Location:
        //      viewport-local coords are scroll-INDEPENDENT — (0, 0) is always the top-left
        //      visible cell regardless of which content cell the scroll maps there. The dirty
        //      ON-SCREEN cells are what we propagate, not the content cells they're showing.
        //   5. Clip to the subview's visible viewport bounds; anything outside is in adornment
        //      territory. If nothing is left, fall back to a full subview invalidation so the
        //      subview at least redraws itself safely (and flags its adornments).
        Point thisScroll = Viewport.Location;
        Rectangle contentRegion = viewPortRelativeRegion;
        contentRegion.Offset (thisScroll.X, thisScroll.Y);

        foreach (View subview in InternalSubViews.Snapshot ())
        {
            if (!subview.Frame.IntersectsWith (contentRegion))
            {
                continue;
            }

            Rectangle subviewFrameRegion = Rectangle.Intersect (subview.Frame, contentRegion);
            subviewFrameRegion.Offset (-subview.Frame.X, -subview.Frame.Y);

            Point subviewAdornmentOffset = subview.GetViewportOffsetFromFrame ();
            subviewFrameRegion.Offset (-subviewAdornmentOffset.X, -subviewAdornmentOffset.Y);

            Rectangle subviewViewportBounds = new (Point.Empty, subview.Viewport.Size);
            Rectangle subviewViewportRegion = Rectangle.Intersect (subviewViewportBounds, subviewFrameRegion);

            if (subviewViewportRegion.IsEmpty)
            {
                // Dirty region overlaps the subview's frame but only its adornment area. The
                // subview's own viewport isn't dirty; a no-arg SetNeedsDraw is the safe
                // fallback (also flags adornments so any border/padding the dirty region
                // touched gets repainted).
                subview.SetNeedsDraw ();

                continue;
            }

            subview.SetNeedsDraw (subviewViewportRegion);
        }
    }

    /// <summary>INTERNAL: Clears <see cref="NeedsDraw"/> and <see cref="SubViewNeedsDraw"/> for this view and all SubViews.</summary>
    /// <remarks>
    ///     See <see cref="SubViewNeedsDraw"/> is a cached value that is set when any subview or adornment requests a redraw.
    ///     It may not always be in sync with the actual state of the subviews.
    /// </remarks>
    internal void ClearNeedsDraw ()
    {
        NeedsDrawRect = Rectangle.Empty;

        Margin.View?.ClearNeedsDraw ();
        Border.View?.ClearNeedsDraw ();
        Padding.View?.ClearNeedsDraw ();

        foreach (View subview in InternalSubViews.Snapshot ())
        {
            subview.ClearNeedsDraw ();
        }

        SubViewNeedsDraw = false;

        // This ensures LineCanvas' get redrawn.
        // AdornmentViews skip this because their LC may hold merged SubView lines
        // that haven't been consumed by the parent's DoDrawAdornmentsSubViews yet.
        // Those lines are cleared in DoDrawAdornmentsSubViews after merging into the parent's LC.
        if (!SuperViewRendersLineCanvas && this is not AdornmentView)
        {
            LineCanvas.Clear ();
        }
    }

    // NOTE: SubViewNeedsDraw is decoupled from the actual state of the subviews (and adornments).
    //       It is a performance optimization to avoid having to traverse all subviews and adornments to check if any need redraw.
    //       As a result the code is fragile and can get out of sync; care must be taken to ensure it is set and cleared correctly.
    /// <summary>
    ///     INTERNAL: Gets whether any SubViews need to be redrawn.
    /// </summary>
    /// <remarks>
    ///     See <see cref="SubViewNeedsDraw"/> is a cached value that is set when any subview or adornment requests a redraw.
    ///     It may not always be in sync with the actual state of the subviews.
    /// </remarks>
    internal bool SubViewNeedsDraw { get; private set; }

    /// <summary>INTERNAL: Sets <see cref="SubViewNeedsDraw"/> to <see langword="true"/> for this View and all Superviews.</summary>
    /// <remarks>
    ///     See <see cref="SubViewNeedsDraw"/> is a cached value that is set when any subview or adornment requests a redraw.
    ///     It may not always be in sync with the actual state of the subviews.
    /// </remarks>
    internal void SetSubViewNeedsDrawDownHierarchy ()
    {
        if (!Visible)
        {
            return;
        }

        SubViewNeedsDraw = true;

        if (this is AdornmentView adornment)
        {
            adornment.Adornment?.Parent?.SetSubViewNeedsDrawDownHierarchy ();
        }

        if (SuperView is { SubViewNeedsDraw: false })
        {
            SuperView.SetSubViewNeedsDrawDownHierarchy ();
        }
    }
}
