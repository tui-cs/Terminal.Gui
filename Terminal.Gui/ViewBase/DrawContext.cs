namespace Terminal.Gui.ViewBase;

/// <summary>
///     Tracks the region that has been drawn during <see cref="View.Draw(DrawContext?)"/>. This is primarily
///     in support of <see cref="ViewportSettingsFlags.Transparent"/>.
/// </summary>
/// <remarks>
///     <para>
///         When a <see cref="View"/> has <see cref="ViewportSettingsFlags.Transparent"/> set, the
///         <see cref="DrawContext"/>
///         is used to track exactly which areas of the screen have been drawn to. After drawing is complete, these drawn
///         regions are excluded from the clip region, allowing views beneath the transparent view to show through in
///         the areas that were not drawn.
///     </para>
///     <para>
///         All coordinates tracked by <see cref="DrawContext"/> are in <b>screen-relative coordinates</b>. When reporting
///         drawn areas from within <see cref="View.OnDrawingContent(DrawContext?)"/>, use
///         <see cref="View.ViewportToScreen(in Rectangle)"/>
///         or <see cref="View.ContentToScreen(in Point)"/> to convert viewport-relative or content-relative coordinates to
///         screen-relative coordinates before calling <see cref="AddDrawnRectangle"/> or <see cref="AddDrawnRegion"/>.
///     </para>
///     <para>
///         Example of reporting a non-rectangular drawn region for transparency:
///     </para>
///     <code>
///         protected override bool OnDrawingContent (DrawContext? context)
///         {
///             // Draw some content in viewport-relative coordinates
///             Rectangle rect1 = new Rectangle (5, 5, 10, 3);
///             Rectangle rect2 = new Rectangle (8, 8, 4, 7);
///             FillRect (rect1, Glyphs.BlackCircle);
///             FillRect (rect2, Glyphs.BlackCircle);
/// 
///             // Report the drawn region in screen-relative coordinates
///             Region drawnRegion = new Region (ViewportToScreen (rect1));
///             drawnRegion.Union (ViewportToScreen (rect2));
///             context?.AddDrawnRegion (drawnRegion);
/// 
///             return true;
///         }
///     </code>
/// </remarks>
public class DrawContext
{
    private readonly Region _drawnRegion = new ();

    /// <summary>
    ///     Gets a copy of the region drawn so far in this context.
    /// </summary>
    /// <remarks>
    ///     The returned region contains all areas that have been reported as drawn via <see cref="AddDrawnRectangle"/>
    ///     or <see cref="AddDrawnRegion"/>, in screen-relative coordinates.
    /// </remarks>
    public Region GetDrawnRegion () => _drawnRegion.Clone ();

    /// <summary>
    ///     Reports that a rectangle has been drawn.
    /// </summary>
    /// <param name="rect">The rectangle that was drawn, in screen-relative coordinates.</param>
    /// <remarks>
    ///     When called from within <see cref="View.OnDrawingContent(DrawContext?)"/>, ensure the rectangle is in
    ///     screen-relative coordinates by using <see cref="View.ViewportToScreen(in Rectangle)"/> or similar methods.
    /// </remarks>
    public void AddDrawnRectangle (Rectangle rect) => _drawnRegion.Combine (rect, RegionOp.Union);

    /// <summary>
    ///     Reports that a region has been drawn.
    /// </summary>
    /// <param name="region">The region that was drawn, in screen-relative coordinates.</param>
    /// <remarks>
    ///     <para>
    ///         This method is useful for reporting non-rectangular drawn areas, which is important for
    ///         proper transparency support with <see cref="ViewportSettingsFlags.Transparent"/>.
    ///     </para>
    ///     <para>
    ///         When called from within <see cref="View.OnDrawingContent(DrawContext?)"/>, ensure the region is in
    ///         screen-relative coordinates by using <see cref="View.ViewportToScreen(in Rectangle)"/> to convert each
    ///         rectangle in the region.
    ///     </para>
    /// </remarks>
    public void AddDrawnRegion (Region region) => _drawnRegion.Combine (region, RegionOp.Union);

    /// <summary>
    ///     Clips (intersects) the drawn region with the specified rectangle.
    ///     This modifies the internal drawn region directly.
    /// </summary>
    /// <param name="clipRect">The clipping rectangle, in screen-relative coordinates.</param>
    public void ClipDrawnRegion (Rectangle clipRect) => _drawnRegion.Intersect (clipRect);

    /// <summary>
    ///     Clips (intersects) the drawn region with the specified region.
    ///     This modifies the internal drawn region directly.
    /// </summary>
    /// <param name="clipRegion">The clipping region, in screen-relative coordinates.</param>
    public void ClipDrawnRegion (Region clipRegion) => _drawnRegion.Intersect (clipRegion);
}
