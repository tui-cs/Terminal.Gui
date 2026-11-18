using System.Runtime.InteropServices;

namespace Terminal.Gui.Drawing;

/// <summary>
///     A canvas for composing box-drawing and line-art characters with automatic intersection resolution. See
///     <see href="../docs/drawing.md">Drawing Deep Dive</see> for an in-depth look at the design and usage of this class.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="LineCanvas"/> is the core rendering primitive for borders, frames, and any box-drawing art
///         in Terminal.Gui. Lines are added via <see cref="AddLine(Point, int, Orientation, LineStyle, Attribute?)"/>
///         and the canvas automatically resolves intersections — where two lines cross or meet, the correct Unicode
///         junction glyph (T, cross, corner, etc.) is produced. This makes it trivial to compose complex bordered
///         layouts without manually computing junction characters.
///     </para>
///     <para>
///         <b>Merging and SuperViewRendersLineCanvas.</b> When <see cref="View.SuperViewRendersLineCanvas"/> is
///         <see langword="true"/> on a SubView, the SubView's <see cref="LineCanvas"/> is merged into the
///         SuperView's canvas via <see cref="Merge(LineCanvas)"/>. All lines then participate in a single
///         intersection-resolution pass, producing seamless junctions across view boundaries. This is how
///         adjacent tab headers, nested frames, and other multi-view border compositions achieve connected
///         line art.
///     </para>
///     <para>
///         <b>Exclusion regions</b> (<see cref="Exclude"/>). Prevents resolved cells from appearing in
///         <see cref="GetCellMap"/> output. Lines still exist in the canvas and still participate in
///         intersection resolution (auto-join), but excluded positions are filtered out of the final
///         output. Use this when something else has already been drawn at a position — for example,
///         a title label on a border, or a SubView that renders its own <see cref="LineCanvas"/>
///         independently.
///     </para>
///     <para>
///         <b>Reserved cells</b> (<see cref="Reserve"/>). Marks positions as intentionally empty —
///         no line exists here and none should be rendered by other canvases either. Unlike
///         <see cref="Exclude"/>, reserved cells have no effect on this canvas's resolution or
///         output. They are metadata consumed during multi-canvas compositing
///         (see <see cref="View.RenderLineCanvas"/>): when multiple independently-resolved canvases
///         are layered, reserved cells claim their positions so that cells from canvases composited
///         later do not show through. Use this for intentional gaps in borders, such as the opening
///         where a focused tab header connects to the content area.
///     </para>
///     <para>
///         <b>Clipped merge.</b> The <c>Merge(LineCanvas, Region?)</c> overload supports merging with
///         an exclusion region that clips incoming lines at the line level — before intersection resolution.
///         Excluded cells are not added as lines and therefore do not participate in auto-join. Note that this
///         can fragment lines and produce incorrect junction glyphs; prefer per-canvas resolution with
///         <see cref="Reserve"/> and compositing for overlapped views.
///     </para>
///     <para>
///         <b>Output.</b> Call <see cref="GetCellMap"/> (or <see cref="GetMap()"/>) to resolve all intersections
///         and produce a dictionary mapping screen coordinates to the glyphs (with attributes) to render.
///         <see cref="GetCellMapWithRegion"/> additionally returns a <see cref="Region"/> covering the drawn
///         cells, which is used for transparency tracking.
///     </para>
///     <para>
///         Does not support diagonal lines. All lines are axis-aligned (horizontal or vertical).
///     </para>
/// </remarks>
public class LineCanvas : IDisposable
{
    /// <summary>Creates a new instance.</summary>
    public LineCanvas () =>

        // Subscribes to theme changes so cached glyphs/attributes can be refreshed. See Issue #2871.
        ThemeChanges.ThemeChanged += OnThemeChanged;

    private readonly List<StraightLine> _lines = [];

    /// <summary>Creates a new instance with the given <paramref name="lines"/>.</summary>
    /// <param name="lines">Initial lines for the canvas.</param>
    public LineCanvas (IEnumerable<StraightLine> lines) : this () => _lines = lines.ToList ();

    /// <summary>
    ///     Optional <see cref="FillPair"/> which when present overrides the <see cref="StraightLine.Attribute"/>
    ///     (colors) of lines in the canvas. This can be used e.g. to apply a global <see cref="GradientFill"/>
    ///     across all lines.
    /// </summary>
    public FillPair? Fill { get; set; }

    private Rectangle _cachedBounds;

    /// <summary>
    ///     Gets the rectangle that describes the bounds of the canvas. Location is the coordinates of the line that is
    ///     the furthest left/top and Size is defined by the line that extends the furthest right/bottom.
    /// </summary>
    public Rectangle Bounds
    {
        get
        {
            if (!_cachedBounds.IsEmpty || _lines.Count == 0)
            {
                return _cachedBounds;
            }

            Rectangle bounds = _lines [0].Bounds;

            for (var i = 1; i < _lines.Count; i++)
            {
                bounds = Rectangle.Union (bounds, _lines [i].Bounds);
            }

            if (bounds is { Width: 0 } or { Height: 0 })
            {
                bounds = bounds with { Width = Math.Clamp (bounds.Width, 1, short.MaxValue), Height = Math.Clamp (bounds.Height, 1, short.MaxValue) };
            }

            _cachedBounds = bounds;

            return _cachedBounds;
        }
    }

    /// <summary>Gets the lines in the canvas.</summary>
    public IReadOnlyCollection<StraightLine> Lines => _lines.AsReadOnly ();

    /// <summary>
    ///     <para>Adds a new <paramref name="length"/> long line to the canvas starting at <paramref name="start"/>.</para>
    ///     <para>
    ///         Use positive <paramref name="length"/> for the line to extend Right and negative for Left when
    ///         <see cref="Orientation"/> is <see cref="Orientation.Horizontal"/>.
    ///     </para>
    ///     <para>
    ///         Use positive <paramref name="length"/> for the line to extend Down and negative for Up when
    ///         <see cref="Orientation"/> is <see cref="Orientation.Vertical"/>.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="LineStyle.None"/> has no special handling inside <see cref="LineCanvas"/>. A line
    ///         added with <see cref="LineStyle.None"/> is stored and participates in intersection resolution
    ///         like any other line; because <see cref="LineStyle.None"/> does not match any styled-glyph check,
    ///         it falls through to the default glyphs and renders identically to <see cref="LineStyle.Single"/>.
    ///     </para>
    ///     <para>
    ///         To erase geometry, do not add a <see cref="LineStyle.None"/> line. Instead, use
    ///         <see cref="StraightLineExtensions.Exclude(IEnumerable{StraightLine}, Point, int, Orientation)"/>
    ///         to physically split or remove overlapping lines from the <see cref="Lines"/> collection. To
    ///         suppress output without removing geometry (e.g., for a title label), use
    ///         <see cref="Exclude"/>. To claim positions during multi-canvas compositing, use
    ///         <see cref="Reserve"/>.
    ///     </para>
    ///     <para>
    ///         See the <a href="../docs/drawing.md">Drawing Deep Dive</a> for a detailed comparison of
    ///         <see cref="LineStyle.None"/>, <see cref="Exclude"/>, and <see cref="Reserve"/>.
    ///     </para>
    /// </remarks>
    /// <param name="start">Starting point.</param>
    /// <param name="length">
    ///     The length of line. 0 for an intersection (cross or T). Positive for Down/Right. Negative for
    ///     Up/Left.
    /// </param>
    /// <param name="orientation">The direction of the line.</param>
    /// <param name="style">The style of line to use.</param>
    /// <param name="attribute">The color attribute for the line, or <see langword="null"/> to inherit.</param>
    public void AddLine (Point start, int length, Orientation orientation, LineStyle style, Attribute? attribute = null)
    {
        _cachedBounds = Rectangle.Empty;
        _lines.Add (new StraightLine (start, length, orientation, style, attribute));
    }

    /// <summary>Adds a new line to the canvas</summary>
    /// <param name="line"></param>
    public void AddLine (StraightLine line)
    {
        _cachedBounds = Rectangle.Empty;
        _lines.Add (line);
    }

    private Region? _exclusionRegion;

    /// <summary>
    ///     Positions marked as intentionally empty by <see cref="Reserve"/>. These have no effect on
    ///     this canvas's resolution or output — they are metadata consumed during multi-canvas
    ///     compositing in <see cref="View.RenderLineCanvas"/>.
    /// </summary>
    private HashSet<Point>? _reservedCells;

    /// <summary>
    ///     Reserves a rectangular region of cells. Reserved cells do not produce visible output and
    ///     do not affect this canvas's intersection resolution or <see cref="GetCellMap"/> output.
    ///     They are metadata consumed during multi-canvas compositing
    ///     (see <see cref="View.RenderLineCanvas"/>): reserved cells claim their positions so that
    ///     cells from canvases composited later do not show through.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Use this for intentional gaps in borders — positions where a line is deliberately
    ///         absent and lines from other canvases should not show through. For example, the open
    ///         gap in a focused tab's border where the tab header connects to the content area.
    ///     </para>
    ///     <para>
    ///         Compare with <see cref="Exclude"/>: Exclude filters this canvas's own resolved output
    ///         (lines still auto-join through excluded positions). Reserve has no effect on this
    ///         canvas — it only affects how multiple canvases are layered during compositing.
    ///     </para>
    ///     <para>
    ///         Reserved cells are cleared when <see cref="Clear"/> is called.
    ///     </para>
    /// </remarks>
    /// <param name="rect">The rectangle of cells to reserve, in canvas coordinates.</param>
    public void Reserve (Rectangle rect)
    {
        _reservedCells ??= [];

        for (int y = rect.Y; y < rect.Bottom; y++)
        {
            for (int x = rect.X; x < rect.Right; x++)
            {
                _reservedCells.Add (new Point (x, y));
            }
        }
    }

    /// <summary>
    ///     Gets the set of reserved cells, or <see langword="null"/> if none have been reserved.
    /// </summary>
    public HashSet<Point>? GetReservedCells () => _reservedCells;

    /// <summary>
    ///     Causes the provided region to be excluded from <see cref="GetCellMap"/> and <see cref="GetMap()"/>.
    ///     Lines at excluded positions still exist in the canvas and still participate in intersection
    ///     resolution (auto-join), but the resolved cells are filtered out of the output.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Use this when something else has already been drawn at a position and the line-art glyph
    ///         should not overwrite it — for example, a title label drawn on a border line, or a SubView
    ///         that renders its own <see cref="LineCanvas"/> independently.
    ///     </para>
    ///     <para>
    ///         Each call to this method will add to the exclusion region. To clear the exclusion region, call
    ///         <see cref="ClearCache"/>.
    ///     </para>
    ///     <para>
    ///         Compare with <see cref="Reserve"/>: Exclude filters this canvas's own output; Reserve marks
    ///         positions as claimed during multi-canvas compositing (see <see cref="View.RenderLineCanvas"/>).
    ///     </para>
    /// </remarks>
    public void Exclude (Region region)
    {
        _exclusionRegion ??= new Region ();
        _exclusionRegion.Union (region);
    }

    /// <summary>
    ///     Clears the exclusion region. After calling this method, <see cref="GetCellMap"/> and <see cref="GetMap()"/> will
    ///     return all points in the canvas.
    /// </summary>
    public void ClearExclusions () => _exclusionRegion = null;

    /// <summary>Clears all lines from the LineCanvas.</summary>
    public void Clear ()
    {
        _cachedBounds = Rectangle.Empty;
        _lines.Clear ();
        ClearExclusions ();
        _reservedCells = null;
    }

    /// <summary>
    ///     Clears any cached states from the canvas. Call this method if you make changes to lines that have already been
    ///     added.
    /// </summary>
    public void ClearCache () => _cachedBounds = Rectangle.Empty;

    /// <summary>
    ///     Evaluates the lines that have been added to the canvas and returns a map containing the glyphs and their
    ///     locations. The glyphs are the characters that should be rendered so that all lines connect up with the appropriate
    ///     intersection symbols.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Only the points within the <see cref="Bounds"/> of the canvas that are not in the exclusion region will be
    ///         returned. To exclude points from the map, use <see cref="Exclude"/>.
    ///     </para>
    /// </remarks>
    /// <returns>A map of all the points within the canvas.</returns>
    public Dictionary<Point, Cell?> GetCellMap ()
    {
        Dictionary<Point, Cell?> map = new ();

        List<IntersectionDefinition> intersectionsBufferList = [];

        // walk through each pixel of the bitmap
        for (int y = Bounds.Y; y < Bounds.Y + Bounds.Height; y++)
        {
            for (int x = Bounds.X; x < Bounds.X + Bounds.Width; x++)
            {
                intersectionsBufferList.Clear ();

                foreach (StraightLine line in _lines)
                {
                    if (line.Intersects (x, y) is { } intersect)
                    {
                        intersectionsBufferList.Add (intersect);
                    }
                }

                // Safe as long as the list is not modified while the span is in use.
                ReadOnlySpan<IntersectionDefinition> intersects = CollectionsMarshal.AsSpan (intersectionsBufferList);
                Cell? cell = GetCellForIntersects (intersects);

                // TODO: Can we skip the whole nested looping if _exclusionRegion is null?
                if (cell is { } && _exclusionRegion?.Contains (x, y) is null or false)
                {
                    map.Add (new Point (x, y), cell);
                }
            }
        }

        return map;
    }

    /// <summary>
    ///     Evaluates the lines and returns both the cell map and a Region encompassing the drawn cells.
    ///     This is more efficient than calling <see cref="GetCellMap"/> and <see cref="GetRegion"/> separately
    ///     as it builds both in a single pass through the canvas bounds.
    /// </summary>
    /// <returns>A tuple containing the cell map and the Region of drawn cells</returns>
    public (Dictionary<Point, Cell?> CellMap, Region Region) GetCellMapWithRegion ()
    {
        Dictionary<Point, Cell?> map = new ();
        Region region = new ();

        List<IntersectionDefinition> intersectionsBufferList = [];
        List<int> rowXValues = [];

        // walk through each pixel of the bitmap, row by row
        for (int y = Bounds.Y; y < Bounds.Y + Bounds.Height; y++)
        {
            rowXValues.Clear ();

            for (int x = Bounds.X; x < Bounds.X + Bounds.Width; x++)
            {
                intersectionsBufferList.Clear ();

                foreach (StraightLine line in _lines)
                {
                    if (line.Intersects (x, y) is { } intersect)
                    {
                        intersectionsBufferList.Add (intersect);
                    }
                }

                // Safe as long as the list is not modified while the span is in use.
                ReadOnlySpan<IntersectionDefinition> intersects = CollectionsMarshal.AsSpan (intersectionsBufferList);
                Cell? cell = GetCellForIntersects (intersects);

                if (cell is null || _exclusionRegion?.Contains (x, y) is not (null or false))
                {
                    continue;
                }
                map.Add (new Point (x, y), cell);
                rowXValues.Add (x);
            }

            // Build Region spans for this completed row
            if (rowXValues.Count <= 0)
            {
                continue;
            }

            // X values are already sorted (inner loop iterates x in order)
            int spanStart = rowXValues [0];
            int spanEnd = rowXValues [0];

            for (var i = 1; i < rowXValues.Count; i++)
            {
                if (rowXValues [i] != spanEnd + 1)
                {
                    // End the current span and add it to the region
                    region.Combine (new Rectangle (spanStart, y, spanEnd - spanStart + 1, 1), RegionOp.Union);
                    spanStart = rowXValues [i];
                }
                spanEnd = rowXValues [i];
            }

            // Add the final span for this row
            region.Combine (new Rectangle (spanStart, y, spanEnd - spanStart + 1, 1), RegionOp.Union);
        }

        return (map, region);
    }

    /// <summary>
    ///     Efficiently builds a <see cref="Region"/> from line cells by grouping contiguous horizontal spans.
    ///     This avoids the performance overhead of adding each cell individually while accurately
    ///     representing the non-rectangular shape of the lines.
    /// </summary>
    /// <param name="cellMap">Dictionary of points where line cells are drawn. If empty, returns an empty Region.</param>
    /// <returns>A Region encompassing all the line cells, or an empty Region if cellMap is empty</returns>
    public static Region GetRegion (Dictionary<Point, Cell?> cellMap)
    {
        // Group cells by row for efficient horizontal span detection
        // Sort by Y then X so that within each row group, X values are in order
        IEnumerable<IGrouping<int, Point>> rowGroups = cellMap.Keys.OrderBy (p => p.Y).ThenBy (p => p.X).GroupBy (p => p.Y);

        Region region = new ();

        foreach (IGrouping<int, Point> row in rowGroups)
        {
            int y = row.Key;

            // X values are sorted due to ThenBy above
            List<int> xValues = row.Select (p => p.X).ToList ();

            // Note: GroupBy on non-empty Keys guarantees non-empty groups, but check anyway for safety
            if (xValues.Count == 0)
            {
                continue;
            }

            // Merge contiguous x values into horizontal spans
            int spanStart = xValues [0];
            int spanEnd = xValues [0];

            for (var i = 1; i < xValues.Count; i++)
            {
                if (xValues [i] == spanEnd + 1)
                {
                    // Continue the span
                    spanEnd = xValues [i];
                }
                else
                {
                    // End the current span and add it to the region
                    region.Combine (new Rectangle (spanStart, y, spanEnd - spanStart + 1, 1), RegionOp.Union);
                    spanStart = xValues [i];
                    spanEnd = xValues [i];
                }
            }

            // Add the final span for this row
            region.Combine (new Rectangle (spanStart, y, spanEnd - spanStart + 1, 1), RegionOp.Union);
        }

        return region;
    }

    // TODO: Unless there's an obvious use case for this API we should delete it in favor of the
    // simpler version that doesn't take an area.
    /// <summary>
    ///     Evaluates the lines that have been added to the canvas and returns a map containing the glyphs and their
    ///     locations. The glyphs are the characters that should be rendered so that all lines connect up with the appropriate
    ///     intersection symbols.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Only the points within the <paramref name="inArea"/> of the canvas that are not in the exclusion region will be
    ///         returned. To exclude points from the map, use <see cref="Exclude"/>.
    ///     </para>
    /// </remarks>
    /// <param name="inArea">A rectangle to constrain the search by.</param>
    /// <returns>A map of the points within the canvas that intersect with <paramref name="inArea"/>.</returns>
    public Dictionary<Point, Rune> GetMap (Rectangle inArea)
    {
        Dictionary<Point, Rune> map = new ();

        List<IntersectionDefinition> intersectionsBufferList = [];

        // walk through each pixel of the bitmap
        for (int y = inArea.Y; y < inArea.Y + inArea.Height; y++)
        {
            for (int x = inArea.X; x < inArea.X + inArea.Width; x++)
            {
                intersectionsBufferList.Clear ();

                foreach (StraightLine line in _lines)
                {
                    if (line.Intersects (x, y) is { } intersect)
                    {
                        intersectionsBufferList.Add (intersect);
                    }
                }

                // Safe as long as the list is not modified while the span is in use.
                ReadOnlySpan<IntersectionDefinition> intersects = CollectionsMarshal.AsSpan (intersectionsBufferList);

                Rune? rune = GetRuneForIntersects (intersects);

                if (rune is { } && _exclusionRegion?.Contains (x, y) is null or false)
                {
                    map.Add (new Point (x, y), rune.Value);
                }
            }
        }

        return map;
    }

    /// <summary>
    ///     Evaluates the lines that have been added to the canvas and returns a map containing the glyphs and their
    ///     locations. The glyphs are the characters that should be rendered so that all lines connect up with the appropriate
    ///     intersection symbols.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Only the points within the <see cref="Bounds"/> of the canvas that are not in the exclusion region will be
    ///         returned. To exclude points from the map, use <see cref="Exclude"/>.
    ///     </para>
    /// </remarks>
    /// <returns>A map of all the points within the canvas.</returns>
    public Dictionary<Point, Rune> GetMap () => GetMap (Bounds);

    /// <summary>Merges one line canvas into this one.</summary>
    /// <param name="lineCanvas"></param>
    public void Merge (LineCanvas lineCanvas)
    {
        foreach (StraightLine line in lineCanvas._lines)
        {
            AddLine (line);
        }

        if (lineCanvas._exclusionRegion is null)
        {
            return;
        }

        _exclusionRegion ??= new Region ();
        _exclusionRegion.Union (lineCanvas._exclusionRegion);
    }

    /// <summary>Merges one line canvas into this one, clipping all lines to the specified bounds.</summary>
    /// <remarks>
    ///     Lines that fall entirely outside <paramref name="clipBounds"/> are discarded.
    ///     Lines that partially overlap are trimmed to fit within the bounds.
    /// </remarks>
    /// <param name="lineCanvas">The source canvas whose lines will be merged.</param>
    /// <param name="clipBounds">The screen-relative rectangle to clip incoming lines to.</param>
    public void Merge (LineCanvas lineCanvas, Rectangle clipBounds)
    {
        foreach (StraightLine line in lineCanvas._lines)
        {
            StraightLine? clipped = ClipLine (line, clipBounds);

            if (clipped is { })
            {
                AddLine (clipped);
            }
        }

        if (lineCanvas._exclusionRegion is null)
        {
            return;
        }

        Region clippedExclusion = lineCanvas._exclusionRegion.Clone ();
        clippedExclusion.Intersect (clipBounds);
        _exclusionRegion ??= new Region ();
        _exclusionRegion.Union (clippedExclusion);
    }

    /// <summary>Clips a <see cref="StraightLine"/> to the specified bounds rectangle.</summary>
    /// <returns>A new clipped line, or <see langword="null"/> if the line falls entirely outside bounds.</returns>
    private static StraightLine? ClipLine (StraightLine line, Rectangle bounds)
    {
        Rectangle lineBounds = line.Bounds;

        if (line.Orientation == Orientation.Horizontal)
        {
            // Line is at a fixed Y. If Y is outside bounds, discard.
            if (lineBounds.Y < bounds.Y || lineBounds.Y >= bounds.Bottom)
            {
                return null;
            }

            // Clamp horizontal extent to bounds
            int clippedLeft = Math.Max (lineBounds.Left, bounds.Left);
            int clippedRight = Math.Min (lineBounds.Right, bounds.Right);

            if (clippedLeft >= clippedRight)
            {
                return null;
            }

            // Determine new start and length, preserving the sign convention
            int newLength = clippedRight - clippedLeft;
            Point newStart;

            if (line.Length >= 0)
            {
                newStart = new Point (clippedLeft, lineBounds.Y);
            }
            else
            {
                // Negative length: start is at the right end, length is negative
                newStart = new Point (clippedRight - 1, lineBounds.Y);
                newLength = -newLength;
            }

            return new StraightLine (newStart, newLength, Orientation.Horizontal, line.Style, line.Attribute);
        }
        else
        {
            // Vertical line at a fixed X. If X is outside bounds, discard.
            if (lineBounds.X < bounds.X || lineBounds.X >= bounds.Right)
            {
                return null;
            }

            // Clamp vertical extent to bounds
            int clippedTop = Math.Max (lineBounds.Top, bounds.Top);
            int clippedBottom = Math.Min (lineBounds.Bottom, bounds.Bottom);

            if (clippedTop >= clippedBottom)
            {
                return null;
            }

            int newLength = clippedBottom - clippedTop;
            Point newStart;

            if (line.Length >= 0)
            {
                newStart = new Point (lineBounds.X, clippedTop);
            }
            else
            {
                newStart = new Point (lineBounds.X, clippedBottom - 1);
                newLength = -newLength;
            }

            return new StraightLine (newStart, newLength, Orientation.Vertical, line.Style, line.Attribute);
        }
    }

    /// <summary>Removes the last line added to the canvas</summary>
    /// <returns></returns>
    public StraightLine RemoveLastLine ()
    {
        StraightLine? l = _lines.LastOrDefault ();

        if (l is { })
        {
            _lines.Remove (l);
        }

        return l!;
    }

    /// <summary>
    ///     Returns the contents of the line canvas rendered to a string. The string will include all columns and rows,
    ///     even if <see cref="Bounds"/> has negative coordinates. For example, if the canvas contains a single line that
    ///     starts at (-1,-1) with a length of 2, the rendered string will have a length of 2.
    /// </summary>
    /// <returns>The canvas rendered to a string.</returns>
    public override string ToString ()
    {
        if (Bounds.IsEmpty)
        {
            return string.Empty;
        }

        // Generate the rune map for the entire canvas
        Dictionary<Point, Rune> runeMap = GetMap ();

        // Create the rune canvas
        Rune [,] canvas = new Rune [Bounds.Height, Bounds.Width];

        // Copy the rune map to the canvas, adjusting for any negative coordinates
        foreach (KeyValuePair<Point, Rune> kvp in runeMap)
        {
            int x = kvp.Key.X - Bounds.X;
            int y = kvp.Key.Y - Bounds.Y;
            canvas [y, x] = kvp.Value;
        }

        // Convert the canvas to a string
        var sb = new StringBuilder ();

        for (var y = 0; y < canvas.GetLength (0); y++)
        {
            for (var x = 0; x < canvas.GetLength (1); x++)
            {
                Rune r = canvas [y, x];
                sb.Append (r.Value == 0 ? ' ' : r.ToString ());
            }

            if (y < canvas.GetLength (0) - 1)
            {
                sb.AppendLine ();
            }
        }

        return sb.ToString ();
    }

    private static bool All (ReadOnlySpan<IntersectionDefinition> intersects, Orientation orientation)
    {
        foreach (IntersectionDefinition intersect in intersects)
        {
            if (intersect.Line.Orientation != orientation)
            {
                return false;
            }
        }

        return true;
    }

    private void OnThemeChanged (object? sender, App.EventArgs<string> e)
    {
        foreach (KeyValuePair<IntersectionRuneType, IntersectionRuneResolver> irr in _runeResolvers)
        {
            irr.Value.SetGlyphs ();
        }
    }

    /// <summary>
    ///     Returns true if all requested <paramref name="types"/> appear in <paramref name="intersects"/> and there are
    ///     no additional <see cref="IntersectionRuneType"/>
    /// </summary>
    /// <param name="intersects"></param>
    /// <param name="types"></param>
    /// <returns></returns>
    private static bool Exactly (HashSet<IntersectionType> intersects, params IntersectionType [] types) => intersects.SetEquals (types);

    private Attribute? GetAttributeForIntersects (ReadOnlySpan<IntersectionDefinition> intersects) =>
        Fill?.GetAttribute (intersects [0].Point) ?? intersects [0].Line.Attribute;

    private readonly Dictionary<IntersectionRuneType, IntersectionRuneResolver> _runeResolvers = new ()
    {
        { IntersectionRuneType.ULCorner, new ULIntersectionRuneResolver () },
        { IntersectionRuneType.URCorner, new URIntersectionRuneResolver () },
        { IntersectionRuneType.LLCorner, new LLIntersectionRuneResolver () },
        { IntersectionRuneType.LRCorner, new LRIntersectionRuneResolver () },
        { IntersectionRuneType.TopTee, new TopTeeIntersectionRuneResolver () },
        { IntersectionRuneType.LeftTee, new LeftTeeIntersectionRuneResolver () },
        { IntersectionRuneType.RightTee, new RightTeeIntersectionRuneResolver () },
        { IntersectionRuneType.BottomTee, new BottomTeeIntersectionRuneResolver () },
        { IntersectionRuneType.Cross, new CrossIntersectionRuneResolver () }

        // TODO: Add other resolvers
    };

    private Cell? GetCellForIntersects (ReadOnlySpan<IntersectionDefinition> intersects)
    {
        if (intersects.IsEmpty)
        {
            return null;
        }

        var cell = new Cell ();
        Rune? rune = GetRuneForIntersects (intersects);

        if (rune.HasValue)
        {
            cell.Grapheme = rune.ToString ()!;
        }

        cell.Attribute = GetAttributeForIntersects (intersects);

        return cell;
    }

    private Rune? GetRuneForIntersects (ReadOnlySpan<IntersectionDefinition> intersects)
    {
        if (intersects.IsEmpty)
        {
            return null;
        }

        IntersectionRuneType runeType = GetRuneTypeForIntersects (intersects);

        if (_runeResolvers.TryGetValue (runeType, out IntersectionRuneResolver? resolver))
        {
            return resolver.GetRuneForIntersects (intersects);
        }

        // TODO: Remove these once we have all of the below ported to IntersectionRuneResolvers
        bool useDouble = AnyLineStyles (intersects, [LineStyle.Double]);
        bool useDashed = AnyLineStyles (intersects, [LineStyle.Dashed, LineStyle.RoundedDashed]);
        bool useDotted = AnyLineStyles (intersects, [LineStyle.Dotted, LineStyle.RoundedDotted]);

        // horiz and vert lines same as Single for Rounded
        bool useThick = AnyLineStyles (intersects, [LineStyle.Heavy]);
        bool useThickDashed = AnyLineStyles (intersects, [LineStyle.HeavyDashed]);
        bool useThickDotted = AnyLineStyles (intersects, [LineStyle.HeavyDotted]);

        // TODO: Support ruler
        //var useRuler = intersects.Any (i => i.Line.Style == LineStyle.Ruler && i.Line.Length != 0);

        // TODO: maybe make these resolvers too for simplicity?
        switch (runeType)
        {
            case IntersectionRuneType.None:
                return null;

            case IntersectionRuneType.Dot:
                return Glyphs.Dot;

            case IntersectionRuneType.HLine:
                if (useDouble)
                {
                    return Glyphs.HLineDbl;
                }

                if (useDashed)
                {
                    return Glyphs.HLineDa2;
                }

                if (useDotted)
                {
                    return Glyphs.HLineDa3;
                }

                return useThick ? Glyphs.HLineHv : useThickDashed ? Glyphs.HLineHvDa2 : useThickDotted ? Glyphs.HLineHvDa3 : Glyphs.HLine;

            case IntersectionRuneType.VLine:
                if (useDouble)
                {
                    return Glyphs.VLineDbl;
                }

                if (useDashed)
                {
                    return Glyphs.VLineDa3;
                }

                if (useDotted)
                {
                    return Glyphs.VLineDa4;
                }

                return useThick ? Glyphs.VLineHv : useThickDashed ? Glyphs.VLineHvDa3 : useThickDotted ? Glyphs.VLineHvDa4 : Glyphs.VLine;

            default:
                throw new Exception ("Could not find resolver or switch case for " + nameof (runeType) + ":" + runeType);
        }

        static bool AnyLineStyles (ReadOnlySpan<IntersectionDefinition> intersects, ReadOnlySpan<LineStyle> lineStyles)
        {
            foreach (IntersectionDefinition intersect in intersects)
            {
                foreach (LineStyle style in lineStyles)
                {
                    if (intersect.Line.Style == style)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    private IntersectionRuneType GetRuneTypeForIntersects (ReadOnlySpan<IntersectionDefinition> intersects)
    {
        HashSet<IntersectionType> set = new (intersects.Length);

        foreach (IntersectionDefinition intersect in intersects)
        {
            set.Add (intersect.Type);
        }

        #region Cross Conditions

        if (Has (set, [IntersectionType.PassOverHorizontal, IntersectionType.PassOverVertical]))
        {
            return IntersectionRuneType.Cross;
        }

        if (Has (set, [IntersectionType.PassOverVertical, IntersectionType.StartLeft, IntersectionType.StartRight]))
        {
            return IntersectionRuneType.Cross;
        }

        if (Has (set, [IntersectionType.PassOverHorizontal, IntersectionType.StartUp, IntersectionType.StartDown]))
        {
            return IntersectionRuneType.Cross;
        }

        if (Has (set, [IntersectionType.StartLeft, IntersectionType.StartRight, IntersectionType.StartUp, IntersectionType.StartDown]))
        {
            return IntersectionRuneType.Cross;
        }

        #endregion

        #region Corner Conditions

        if (Exactly (set, CornerIntersections.UpperLeft))
        {
            return IntersectionRuneType.ULCorner;
        }

        if (Exactly (set, CornerIntersections.UpperRight))
        {
            return IntersectionRuneType.URCorner;
        }

        if (Exactly (set, CornerIntersections.LowerRight))
        {
            return IntersectionRuneType.LRCorner;
        }

        if (Exactly (set, CornerIntersections.LowerLeft))
        {
            return IntersectionRuneType.LLCorner;
        }

        #endregion Corner Conditions

        #region T Conditions

        if (Has (set, [IntersectionType.PassOverHorizontal, IntersectionType.StartDown])
            || Has (set, [IntersectionType.StartRight, IntersectionType.StartLeft, IntersectionType.StartDown]))
        {
            return IntersectionRuneType.TopTee;
        }

        if (Has (set, [IntersectionType.PassOverHorizontal, IntersectionType.StartUp])
            || Has (set, [IntersectionType.StartRight, IntersectionType.StartLeft, IntersectionType.StartUp]))
        {
            return IntersectionRuneType.BottomTee;
        }

        if (Has (set, [IntersectionType.PassOverVertical, IntersectionType.StartRight])
            || Has (set, [IntersectionType.StartRight, IntersectionType.StartDown, IntersectionType.StartUp]))
        {
            return IntersectionRuneType.LeftTee;
        }

        if (Has (set, [IntersectionType.PassOverVertical, IntersectionType.StartLeft])
            || Has (set, [IntersectionType.StartLeft, IntersectionType.StartDown, IntersectionType.StartUp]))
        {
            return IntersectionRuneType.RightTee;
        }

        #endregion

        if (All (intersects, Orientation.Horizontal))
        {
            return IntersectionRuneType.HLine;
        }

        return All (intersects, Orientation.Vertical) ? IntersectionRuneType.VLine : IntersectionRuneType.Dot;
    }

    /// <summary>
    ///     Returns true if the <paramref name="intersects"/> collection has all the <paramref name="types"/> specified
    ///     (i.e. AND).
    /// </summary>
    /// <param name="intersects"></param>
    /// <param name="types"></param>
    /// <returns></returns>
    private bool Has (HashSet<IntersectionType> intersects, ReadOnlySpan<IntersectionType> types)
    {
        foreach (IntersectionType type in types)
        {
            if (!intersects.Contains (type))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     Preallocated arrays for <see cref="GetRuneTypeForIntersects"/> calls to <see cref="Exactly"/>.
    /// </summary>
    /// <remarks>
    ///     Optimization to avoid array allocation for each call from array params. Please do not edit the arrays at runtime.
    ///     :)
    ///     More ideal solution would be to change <see cref="Exactly"/> to take ReadOnlySpan instead of an array
    ///     but that would require replacing the HashSet.SetEquals call.
    /// </remarks>
    private static class CornerIntersections
    {
        // Names matching #region "Corner Conditions" IntersectionRuneType
        internal static readonly IntersectionType [] UpperLeft = [IntersectionType.StartRight, IntersectionType.StartDown];
        internal static readonly IntersectionType [] UpperRight = [IntersectionType.StartLeft, IntersectionType.StartDown];
        internal static readonly IntersectionType [] LowerRight = [IntersectionType.StartUp, IntersectionType.StartLeft];
        internal static readonly IntersectionType [] LowerLeft = [IntersectionType.StartUp, IntersectionType.StartRight];
    }

    private class BottomTeeIntersectionRuneResolver : IntersectionRuneResolver
    {
        public override void SetGlyphs ()
        {
            _round = Glyphs.BottomTee;
            _doubleH = Glyphs.BottomTeeDblH;
            _doubleV = Glyphs.BottomTeeDblV;
            _doubleBoth = Glyphs.BottomTeeDbl;
            _thickH = Glyphs.BottomTeeHvH;
            _thickV = Glyphs.BottomTeeHvV;
            _thickBoth = Glyphs.BottomTeeHvDblH;
            _normal = Glyphs.BottomTee;
        }
    }

    private class CrossIntersectionRuneResolver : IntersectionRuneResolver
    {
        public override void SetGlyphs ()
        {
            _round = Glyphs.Cross;
            _doubleH = Glyphs.CrossDblH;
            _doubleV = Glyphs.CrossDblV;
            _doubleBoth = Glyphs.CrossDbl;
            _thickH = Glyphs.CrossHvH;
            _thickV = Glyphs.CrossHvV;
            _thickBoth = Glyphs.CrossHv;
            _normal = Glyphs.Cross;
        }
    }

    private abstract class IntersectionRuneResolver
    {
        internal Rune _doubleBoth;
        internal Rune _doubleH;
        internal Rune _doubleV;
        internal Rune _normal;
        internal Rune _round;
        internal Rune _thickBoth;
        internal Rune _thickH;
        internal Rune _thickV;

        protected IntersectionRuneResolver () => SetGlyphs ();

        public Rune? GetRuneForIntersects (ReadOnlySpan<IntersectionDefinition> intersects)
        {
            // Note that there aren't any glyphs for intersections of double lines with heavy lines

            bool doubleHorizontal = AnyWithOrientationAndAnyLineStyle (intersects, Orientation.Horizontal, [LineStyle.Double]);
            bool doubleVertical = AnyWithOrientationAndAnyLineStyle (intersects, Orientation.Vertical, [LineStyle.Double]);

            if (doubleHorizontal)
            {
                return doubleVertical ? _doubleBoth : _doubleH;
            }

            if (doubleVertical)
            {
                return _doubleV;
            }

            bool thickHorizontal =
                AnyWithOrientationAndAnyLineStyle (intersects, Orientation.Horizontal, [LineStyle.Heavy, LineStyle.HeavyDashed, LineStyle.HeavyDotted]);

            bool thickVertical =
                AnyWithOrientationAndAnyLineStyle (intersects, Orientation.Vertical, [LineStyle.Heavy, LineStyle.HeavyDashed, LineStyle.HeavyDotted]);

            if (thickHorizontal)
            {
                return thickVertical ? _thickBoth : _thickH;
            }

            if (thickVertical)
            {
                return _thickV;
            }

            return UseRounded (intersects) ? _round : _normal;

            static bool UseRounded (ReadOnlySpan<IntersectionDefinition> intersects)
            {
                foreach (IntersectionDefinition intersect in intersects)
                {
                    if (intersect.Line.Length == 0)
                    {
                        continue;
                    }

                    if (intersect.Line.Style is LineStyle.Rounded or LineStyle.RoundedDashed or LineStyle.RoundedDotted)
                    {
                        return true;
                    }
                }

                return false;
            }

            static bool AnyWithOrientationAndAnyLineStyle (ReadOnlySpan<IntersectionDefinition> intersects,
                                                           Orientation orientation,
                                                           ReadOnlySpan<LineStyle> lineStyles)
            {
                foreach (IntersectionDefinition i in intersects)
                {
                    if (i.Line.Orientation != orientation)
                    {
                        continue;
                    }

                    // Any line style
                    foreach (LineStyle style in lineStyles)
                    {
                        if (i.Line.Style == style)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        /// <summary>
        ///     Sets the glyphs used. Call this method after construction and any time theme overlays have updated the
        ///     settings.
        /// </summary>
        public abstract void SetGlyphs ();
    }

    private class LeftTeeIntersectionRuneResolver : IntersectionRuneResolver
    {
        public override void SetGlyphs ()
        {
            _round = Glyphs.LeftTee;
            _doubleH = Glyphs.LeftTeeDblH;
            _doubleV = Glyphs.LeftTeeDblV;
            _doubleBoth = Glyphs.LeftTeeDbl;
            _thickH = Glyphs.LeftTeeHvH;
            _thickV = Glyphs.LeftTeeHvV;
            _thickBoth = Glyphs.LeftTeeHvDblH;
            _normal = Glyphs.LeftTee;
        }
    }

    private class LLIntersectionRuneResolver : IntersectionRuneResolver
    {
        public override void SetGlyphs ()
        {
            _round = Glyphs.LLCornerR;
            _doubleH = Glyphs.LLCornerSingleDbl;
            _doubleV = Glyphs.LLCornerDblSingle;
            _doubleBoth = Glyphs.LLCornerDbl;
            _thickH = Glyphs.LLCornerLtHv;
            _thickV = Glyphs.LLCornerHvLt;
            _thickBoth = Glyphs.LLCornerHv;
            _normal = Glyphs.LLCorner;
        }
    }

    private class LRIntersectionRuneResolver : IntersectionRuneResolver
    {
        public override void SetGlyphs ()
        {
            _round = Glyphs.LRCornerR;
            _doubleH = Glyphs.LRCornerSingleDbl;
            _doubleV = Glyphs.LRCornerDblSingle;
            _doubleBoth = Glyphs.LRCornerDbl;
            _thickH = Glyphs.LRCornerLtHv;
            _thickV = Glyphs.LRCornerHvLt;
            _thickBoth = Glyphs.LRCornerHv;
            _normal = Glyphs.LRCorner;
        }
    }

    private class RightTeeIntersectionRuneResolver : IntersectionRuneResolver
    {
        public override void SetGlyphs ()
        {
            _round = Glyphs.RightTee;
            _doubleH = Glyphs.RightTeeDblH;
            _doubleV = Glyphs.RightTeeDblV;
            _doubleBoth = Glyphs.RightTeeDbl;
            _thickH = Glyphs.RightTeeHvH;
            _thickV = Glyphs.RightTeeHvV;
            _thickBoth = Glyphs.RightTeeHvDblH;
            _normal = Glyphs.RightTee;
        }
    }

    private class TopTeeIntersectionRuneResolver : IntersectionRuneResolver
    {
        public override void SetGlyphs ()
        {
            _round = Glyphs.TopTee;
            _doubleH = Glyphs.TopTeeDblH;
            _doubleV = Glyphs.TopTeeDblV;
            _doubleBoth = Glyphs.TopTeeDbl;
            _thickH = Glyphs.TopTeeHvH;
            _thickV = Glyphs.TopTeeHvV;
            _thickBoth = Glyphs.TopTeeHvDblH;
            _normal = Glyphs.TopTee;
        }
    }

    private class ULIntersectionRuneResolver : IntersectionRuneResolver
    {
        public override void SetGlyphs ()
        {
            _round = Glyphs.ULCornerR;
            _doubleH = Glyphs.ULCornerSingleDbl;
            _doubleV = Glyphs.ULCornerDblSingle;
            _doubleBoth = Glyphs.ULCornerDbl;
            _thickH = Glyphs.ULCornerLtHv;
            _thickV = Glyphs.ULCornerHvLt;
            _thickBoth = Glyphs.ULCornerHv;
            _normal = Glyphs.ULCorner;
        }
    }

    private class URIntersectionRuneResolver : IntersectionRuneResolver
    {
        public override void SetGlyphs ()
        {
            _round = Glyphs.URCornerR;
            _doubleH = Glyphs.URCornerSingleDbl;
            _doubleV = Glyphs.URCornerDblSingle;
            _doubleBoth = Glyphs.URCornerDbl;
            _thickH = Glyphs.URCornerHvLt;
            _thickV = Glyphs.URCornerLtHv;
            _thickBoth = Glyphs.URCornerHv;
            _normal = Glyphs.URCorner;
        }
    }

    /// <summary>
    ///     Maps a box-drawing grapheme to its line directions. Used during overlapped LC compositing
    ///     to determine whether a lower-Z cell adds directions that don't point toward reserved gaps.
    /// </summary>
    public static LineDirections GetLineDirections (string? grapheme)
    {
        if (string.IsNullOrEmpty (grapheme) || grapheme.Length == 0)
        {
            return LineDirections.None;
        }

        char ch = grapheme [0];

        return ch switch
        {
            // Horizontal lines
            '─' or '━' or '═' => LineDirections.Left | LineDirections.Right,

            // Vertical lines
            '│' or '┃' or '║' => LineDirections.Up | LineDirections.Down,

            // Corners (single, rounded, double, heavy)
            '┌' or '╭' or '╔' or '┏' => LineDirections.Right | LineDirections.Down,
            '┐' or '╮' or '╗' or '┓' => LineDirections.Left | LineDirections.Down,
            '└' or '╰' or '╚' or '┗' => LineDirections.Right | LineDirections.Up,
            '┘' or '╯' or '╝' or '┛' => LineDirections.Left | LineDirections.Up,

            // T-junctions (single, double, heavy)
            '├' or '╠' or '┣' => LineDirections.Up | LineDirections.Down | LineDirections.Right,
            '┤' or '╣' or '┫' => LineDirections.Up | LineDirections.Down | LineDirections.Left,
            '┬' or '╦' or '┳' => LineDirections.Left | LineDirections.Right | LineDirections.Down,
            '┴' or '╩' or '┻' => LineDirections.Left | LineDirections.Right | LineDirections.Up,

            // Cross (single, double, heavy)
            '┼' or '╬' or '╋' => LineDirections.Up | LineDirections.Down | LineDirections.Left | LineDirections.Right,

            _ => LineDirections.None
        };
    }

    /// <inheritdoc/>
    public void Dispose ()
    {
        ThemeChanges.ThemeChanged -= OnThemeChanged;
        GC.SuppressFinalize (this);
    }
}
