namespace Terminal.Gui.ViewBase;

public partial class View
{
    private SharedLineCanvasCache? _sharedLineCanvasCache;
    private bool _contentChangesLineCanvas;

    private void RebuildSharedLineCanvas ()
    {
        if (_contentChangesLineCanvas)
        {
            SetNeedsDraw ();

            return;
        }

        SetSubViewNeedsDrawDownHierarchy ();
    }

    /// <summary>The canvas that any line drawing that is to be shared by SubViews of this view should add lines to.</summary>
    /// <remarks><see cref="Border"/> adds lines to this LineCanvas.</remarks>
    public LineCanvas LineCanvas { get; } = new ();

    /// <summary>
    ///     Gets or sets whether this View will use its SuperView's <see cref="LineCanvas"/> for rendering any
    ///     lines. If <see langword="true"/> the rendering of any borders drawn by this view will be done by its
    ///     SuperView. If <see langword="false"/> (the default) this View's <see cref="OnDrawingAdornments"/> method will
    ///     be called to render the borders.
    /// </summary>
    public virtual bool SuperViewRendersLineCanvas { get; set; } = false;

    /// <summary>
    ///     Called when the <see cref="View.LineCanvas"/> is to be rendered. See <see cref="RenderLineCanvas"/>.
    /// </summary>
    /// <returns><see langword="true"/> to stop further drawing of <see cref="LineCanvas"/>.</returns>
    protected virtual bool OnRenderingLineCanvas () => false;

    /// <summary>
    ///     Causes the contents of <see cref="LineCanvas"/> to be drawn.
    ///     If <see cref="SuperViewRendersLineCanvas"/> is true, only the
    ///     <see cref="LineCanvas"/> of this view's SubViews will be rendered. If <see cref="SuperViewRendersLineCanvas"/> is
    ///     false (the default), this method will cause the <see cref="LineCanvas"/> to be rendered.
    /// </summary>
    /// <param name="context"></param>
    public void RenderLineCanvas (DrawContext? context)
    {
        if (Driver is null)
        {
            return;
        }

        bool hasOverlapped = _pendingOverlappedCellMaps is { Count: > 0 };

        if (SuperViewRendersLineCanvas || (LineCanvas.Bounds == Rectangle.Empty && !hasOverlapped))
        {
            return;
        }

        // Resolve the parent's own LineCanvas (includes tiled SubViews' merged lines).
        (Dictionary<Point, Cell?> cellMap, Region lineRegion) = LineCanvas.GetCellMapWithRegion ();

        // Render the parent's resolved cell map (base layer).
        foreach (KeyValuePair<Point, Cell?> p in cellMap)
        {
            if (p.Value is null)
            {
                continue;
            }

            SetAttribute (p.Value.Value.Attribute ?? GetAttributeForRole (VisualRole.Normal));
            Driver.Move (p.Key.X, p.Key.Y);

            // TODO: #2616 - Support combining sequences that don't normalize
            AddStr (p.Value.Value.Grapheme);
        }

        // Composite overlapped SubViews' cell maps via painters' algorithm.
        // The list is ordered highest-Z first. We iterate from index 0 (highest Z)
        // to the end (lowest Z). A higher-Z LC cell at a given position suppresses
        // all lower-Z LC cells at that same position, UNLESS the lower-Z cell is a
        // richer junction (more line directions) and the additional directions don't
        // point toward reserved (gap) cells of any higher-Z view.
        if (hasOverlapped)
        {
            // Track cells already rendered by higher-Z views and the cell value at each position.
            Dictionary<Point, Cell> renderedCells = new ();

            // Collect all reserved cells from all views for adjacency checks.
            HashSet<Point> allReserved = [];

            if (_pendingOverlappedCellMaps is { })
            {
                for (var i = 0; i < _pendingOverlappedCellMaps.Count; i++)
                {
                    HashSet<Point>? reservedCells = _pendingOverlappedCellMaps [i].Reserved;

                    if (reservedCells is { Count: > 0 })
                    {
                        allReserved.UnionWith (reservedCells);
                    }
                }

                foreach ((Dictionary<Point, Cell?> overlapCellMap, HashSet<Point>? reservedCells) in _pendingOverlappedCellMaps)
                {
                    if (reservedCells is { Count: > 0 })
                    {
                        foreach (Point rp in reservedCells)
                        {
                            renderedCells.TryAdd (rp, default (Cell));
                        }
                    }

                    foreach (KeyValuePair<Point, Cell?> p in overlapCellMap)
                    {
                        if (p.Value is null)
                        {
                            continue;
                        }

                        if (renderedCells.TryGetValue (p.Key, out Cell existingCell))
                        {
                            // Position already claimed. Check if this lower-Z cell should upgrade.
                            if (existingCell.Grapheme is null or "")
                            {
                                // Reserved cell — never upgrade.
                                continue;
                            }

                            LineDirections existingDirs = LineCanvas.GetLineDirections (existingCell.Grapheme);
                            LineDirections newDirs = LineCanvas.GetLineDirections (p.Value.Value.Grapheme);

                            // Lower-Z cell must be a strict superset of the higher-Z cell's directions:
                            // it must contain ALL existing directions plus at least one more.
                            if ((newDirs & existingDirs) != existingDirs)
                            {
                                // Not a superset — lower-Z cell removes some directions. Skip.
                                continue;
                            }

                            LineDirections additionalDirs = newDirs & ~existingDirs;

                            if (additionalDirs == LineDirections.None)
                            {
                                // Lower-Z cell doesn't add any directions — skip.
                                continue;
                            }

                            // Check if any additional direction points toward a reserved cell.
                            var pointsToReserved = false;

                            if (additionalDirs.HasFlag (LineDirections.Up) && allReserved.Contains (p.Key with { Y = p.Key.Y - 1 }))
                            {
                                pointsToReserved = true;
                            }

                            if (!pointsToReserved && additionalDirs.HasFlag (LineDirections.Down) && allReserved.Contains (p.Key with { Y = p.Key.Y + 1 }))
                            {
                                pointsToReserved = true;
                            }

                            if (!pointsToReserved && additionalDirs.HasFlag (LineDirections.Left) && allReserved.Contains (p.Key with { X = p.Key.X - 1 }))
                            {
                                pointsToReserved = true;
                            }

                            if (!pointsToReserved && additionalDirs.HasFlag (LineDirections.Right) && allReserved.Contains (p.Key with { X = p.Key.X + 1 }))
                            {
                                pointsToReserved = true;
                            }

                            if (pointsToReserved)
                            {
                                // Additional direction points into a gap — keep higher-Z cell.
                                continue;
                            }

                            // Upgrade to the richer junction from the lower-Z view.
                            renderedCells [p.Key] = p.Value.Value;
                            SetAttribute (p.Value.Value.Attribute ?? GetAttributeForRole (VisualRole.Normal));
                            Driver.Move (p.Key.X, p.Key.Y);
                            AddStr (p.Value.Value.Grapheme);

                            continue;
                        }

                        SetAttribute (p.Value.Value.Attribute ?? GetAttributeForRole (VisualRole.Normal));
                        Driver.Move (p.Key.X, p.Key.Y);
                        AddStr (p.Value.Value.Grapheme);

                        renderedCells [p.Key] = p.Value.Value;
                        lineRegion.Union (new Rectangle (p.Key.X, p.Key.Y, 1, 1));
                    }
                }
            }

            _pendingOverlappedCellMaps = null;
        }

        // Report the drawn region for transparency support.
        if (context is { } && (cellMap.Count > 0 || hasOverlapped))
        {
            context.AddDrawnRegion (lineRegion);
        }

        // Cache the line canvas region for use by Border's CachedDrawnRegion.
        _lastLineCanvasRegion = cellMap.Count > 0 || hasOverlapped ? lineRegion : null;

        LineCanvas.Clear ();
    }

    /// <summary>
    ///     Retains a shared canvas contribution after its source is consumed. Resolved
    ///     cells preserve overlapped gaps; tiled geometry preserves cross-view auto-join.
    /// </summary>
    private sealed class SharedLineCanvasCache
    {
        private bool _isValid;
        private Rectangle _frame;
        private Rectangle _viewport;
        private bool _overlapped;
        private int _themeGeneration;
        private Scheme? _scheme;
        private Attribute _borderAttribute;
        private bool _hasFocus;
        private bool _enabled;
        private Thickness _borderThickness;
        private LineStyle? _borderStyle;
        private string? _title;

        public (Dictionary<Point, Cell?> CellMap, HashSet<Point>? Reserved) OverlappedCells { get; private set; } = ( [], null);
        public LineCanvasSnapshot? Geometry { get; private set; }
        public bool PendingDraw { get; set; }
        public int SourceVersion { get; private set; }

        public bool Matches (View view) =>
            _isValid
            && _frame == view.FrameToScreen ()
            && _viewport == view.Viewport
            && _overlapped == view.Arrangement.FastHasFlags (ViewArrangement.Overlapped)
            && _themeGeneration == ThemeChanges.Generation
            && _hasFocus == view.HasFocus
            && _enabled == view.Enabled
            && _borderThickness == view.Border.Thickness
            && _borderStyle == view.Border.LineStyle
            && _title == view.Title
            && _scheme == view.GetScheme ()
            && _borderAttribute == (view.Border.View ?? view).GetAttributeForRole (VisualRole.Normal);

        public void Invalidate () => _isValid = false;

        public void Update (View view)
        {
            _frame = view.FrameToScreen ();
            _viewport = view.Viewport;
            _overlapped = view.Arrangement.FastHasFlags (ViewArrangement.Overlapped);
            _themeGeneration = ThemeChanges.Generation;
            _hasFocus = view.HasFocus;
            _enabled = view.Enabled;
            _borderThickness = view.Border.Thickness;
            _borderStyle = view.Border.LineStyle;
            _title = view.Title;
            _scheme = view.GetScheme ();
            _borderAttribute = (view.Border.View ?? view).GetAttributeForRole (VisualRole.Normal);
            Geometry = view.LineCanvas.Capture ();
            SourceVersion = view.LineCanvas.Version;
            OverlappedCells = _overlapped ? (view.LineCanvas.GetCellMap (), Geometry.Reserved) : ( [], null);
            _isValid = true;
        }
    }
}
