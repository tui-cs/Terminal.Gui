namespace Terminal.Gui.Views;

public partial class TableView
{
    #region Cursor

    private int _cursorColumn = -1;
    private int _cursorRow = -1;

    /// <summary>
    ///     Moves the cursor by the provided offsets. Optionally starting a box selection (see <see cref="MultiSelect"/>).
    /// </summary>
    /// <param name="offsetX">Offset in number of columns</param>
    /// <param name="offsetY">Offset in number of rows</param>
    /// <param name="extendExistingSelection">True to create a multi cell selection or adjust an existing one</param>
    /// <param name="ctx">The command context.</param>
    public bool MoveCursorByOffset (int offsetX, int offsetY, bool extendExistingSelection, ICommandContext? ctx)
    {
        SetSelection (_cursorColumn + offsetX, _cursorRow + offsetY, extendExistingSelection, ctx);
        Update ();

        return true;
    }

    /// <summary>Moves the cursor (or extends the selection) to the last cell in the current row.</summary>
    /// <param name="extend">true to extend the current selection (if any) instead of replacing</param>
    /// <param name="ctx">The command context</param>
    public bool MoveCursorToEndOfRow (bool extend, ICommandContext? ctx)
    {
        if (TableIsNullOrInvisible ())
        {
            return false;
        }

        SetSelection (Table!.Columns - 1, _cursorRow, extend, ctx);
        Update ();

        return true;
    }

    /// <summary>Moves the cursor (or extends the selection) to the first cell in the current row.</summary>
    /// <param name="extend">true to extend the current selection (if any) instead of replacing</param>
    /// <param name="ctx">The command context</param>
    public bool MoveCursorToStartOfRow (bool extend, ICommandContext? ctx)
    {
        if (TableIsNullOrInvisible ())
        {
            return false;
        }

        SetSelection (0, _cursorRow, extend, ctx);
        Update ();

        return true;
    }

    /// <summary>
    ///     Private override of <see cref="MoveCursorByOffset"/> that returns <see langword="true"/> if the
    ///     <see cref="Value"/> changed as a result of moving the cursor.
    /// </summary>
    /// <param name="offsetX"></param>
    /// <param name="offsetY"></param>
    /// <param name="ctx">The command context.</param>
    /// <returns></returns>
    private bool MoveCursorByOffsetWithReturn (int offsetX, int offsetY, ICommandContext? ctx)
    {
        TableSelection? oldValue = Value;
        SetSelection (_cursorColumn + offsetX, _cursorRow + offsetY, false, ctx);
        Update ();

        return !Equals (oldValue, Value);
    }

    /// <summary>
    ///     Moves the cursor (or extends the selection) to the final cell in the table (nX,nY). If
    ///     <see cref="FullRowSelect"/> is enabled then the cursor instead moves to (cursor.X, nY) — no horizontal scrolling.
    /// </summary>
    /// <param name="extend">true to extend the current selection (if any) instead of replacing</param>
    /// <param name="ctx">The command context</param>
    public bool MoveCursorToEndOfTable (bool extend, ICommandContext? ctx)
    {
        if (TableIsNullOrInvisible ())
        {
            return false;
        }

        int finalColumn = Table!.Columns - 1;
        SetSelection (FullRowSelect ? _cursorColumn : finalColumn, Table.Rows - 1, extend, ctx);
        Update ();

        return true;
    }

    /// <summary>
    ///     Moves the cursor (or extends the selection) to the first cell in the table (0,0). If
    ///     <see cref="FullRowSelect"/> is enabled then the cursor instead moves to (cursor.X, 0) — no horizontal scrolling.
    /// </summary>
    /// <param name="extend">true to extend the current selection (if any) instead of replacing</param>
    /// <param name="ctx">The command context</param>
    public bool MoveCursorToStartOfTable (bool extend, ICommandContext? ctx)
    {
        if (TableIsNullOrInvisible ())
        {
            return false;
        }

        SetSelection (FullRowSelect ? _cursorColumn : 0, 0, extend, ctx);
        Update ();

        return true;
    }

    /// <summary>
    ///     Updates scroll offsets to ensure that the cursor cell is visible.  Has no effect if <see cref="Table"/> has
    ///     not been set.
    /// </summary>
    /// <remarks>
    ///     Changes will not be immediately visible in the display until you call <see cref="View.SetNeedsDraw()"/>
    /// </remarks>
    public void EnsureCursorIsVisible ()
    {
        if (Table is null || Table.Columns <= 0)
        {
            return;
        }

        ColumnToRender [] cellInfos = NonHiddenCellInfos ();
        int headerHeight = GetHeaderHeightIfAny ();

        ColumnToRender? cursorColToRender = cellInfos.FirstOrDefault (c => c.Column == _cursorColumn);

        if (_cursorColumn < 0 || cursorColToRender == null || _cursorRow < 0 || _cursorRow >= Table.Rows)
        {
            return;
        }

        int rowStart = Style.AlwaysShowHeaders ? Viewport.Y : Math.Max (Viewport.Y - headerHeight, 0);
        int rowEnd = Viewport.Y + Viewport.Height - headerHeight - 1;

        if (rowEnd < rowStart)
        {
            return;
        }

        if (_cursorRow < rowStart)
        {
            Viewport = Viewport with { Y = Viewport.Y - (rowStart - _cursorRow) };
        }

        if (_cursorRow > rowEnd)
        {
            Viewport = Viewport with { Y = Viewport.Y + (_cursorRow - rowEnd) };
        }

        //first column that is visible from start
        ColumnToRender? colStart = cellInfos.FirstOrDefault (c => c.X - 1 > Viewport.Left);

        //last column that is visible (at least the start)
        ColumnToRender? colEnd = cellInfos.LastOrDefault (c => c.X < Viewport.Right);

        if (colEnd is { } && _cursorColumn >= colEnd.Column)
        {
            if (Style.SmoothHorizontalScrolling)
            {
                //bring cursor col into view
                Viewport = Viewport with { X = Math.Min (cursorColToRender.X, cursorColToRender.X + cursorColToRender.Width - Viewport.Width) };
            }
            else
            {
                //bring cursor col to start of viewport
                Viewport = Viewport with { X = cursorColToRender.X };
            }
        }

        if (colStart is { } && _cursorColumn >= colStart.Column)
        {
            return;
        }

        if (Style.SmoothHorizontalScrolling)
        {
            //bring cursor col into view
            Viewport = Viewport with { X = cursorColToRender.X - 1 };
        }
        else
        {
            //bring cursor col to end of viewport
            Viewport = Viewport with { X = cursorColToRender.X - Math.Max (Viewport.Width - cursorColToRender.Width, 0) };
        }
    }

    /// <summary>
    ///     Syncs the internal cursor and <see cref="MultiSelectedRegions"/> from the current <see cref="Value"/>.
    /// </summary>
    private void SyncCursorFromValue ()
    {
        if (_value is null)
        {
            _cursorColumn = -1;
            _cursorRow = -1;
            MultiSelectedRegions.Clear ();

            return;
        }

        _cursorColumn = _value.SelectedCell.X;
        _cursorRow = _value.SelectedCell.Y;

        // Rebuild MultiSelectedRegions from Value.Regions (deep copy)
        MultiSelectedRegions.Clear ();

        foreach (TableSelectionRegion region in _value.Regions)
        {
            MultiSelectedRegions.Push (new TableSelectionRegion (region.Origin, region.Rectangle) { IsExtended = region.IsExtended });
        }
    }

    #endregion Cursor

    #region Selection

    /// <summary>
    ///     Updates the cursor position, the <see cref="MultiSelectedRegions"/>, and <see cref="Value"/> to ensure they are
    ///     within the bounds of the table (by adjusting them to the nearest existing cell). Has no effect if
    ///     <see cref="Table"/> has not been set.
    /// </summary>
    /// <remarks>
    ///     Changes will not be immediately visible in the display until you call <see cref="View.SetNeedsDraw()"/>
    /// </remarks>
    public void EnsureValidSelection ()
    {
        if (TableIsNullOrInvisible ())
        {
            // Table doesn't exist, we should probably clear those selections
            ClearMultiSelectedRegions (false);

            return;
        }

        _cursorColumn = Math.Max (Math.Min (_cursorColumn, Table!.Columns - 1), 0);
        _cursorRow = Math.Max (Math.Min (_cursorRow, Table.Rows - 1), 0);

        // If _cursorColumn is invisible move it to a visible one
        _cursorColumn = GetNearestVisibleColumn (_cursorColumn, true, true);
        IEnumerable<TableSelectionRegion> oldRegions = MultiSelectedRegions.ToArray ().Reverse ();
        MultiSelectedRegions.Clear ();

        // evaluate
        foreach (TableSelectionRegion region in oldRegions)
        {
            // ignore regions entirely below current table state
            if (region.Rectangle.Top >= Table.Rows)
            {
                continue;
            }

            // ignore regions entirely too far right of table columns
            if (region.Rectangle.Left >= Table.Columns)
            {
                continue;
            }

            // Clamp region to table bounds
            Point clampedOrigin = new (Math.Max (Math.Min (region.Origin.X, Table.Columns - 1), 0), Math.Max (Math.Min (region.Origin.Y, Table.Rows - 1), 0));

            Rectangle clampedRect = Rectangle.FromLTRB (region.Rectangle.Left,
                                                        region.Rectangle.Top,
                                                        Math.Max (Math.Min (region.Rectangle.Right, Table.Columns), 0),
                                                        Math.Max (Math.Min (region.Rectangle.Bottom, Table.Rows), 0));

            MultiSelectedRegions.Push (new TableSelectionRegion (clampedOrigin, clampedRect) { IsExtended = region.IsExtended });
        }
    }

    /// <summary>True to select the entire row at once. False to select individual cells. Defaults to <see langword="false"/>.</summary>
    public bool FullRowSelect { get; set; }

    /// <summary>
    ///     Returns all cells in any <see cref="MultiSelectedRegions"/> (if <see cref="MultiSelect"/> is enabled) and the
    ///     cursor cell.
    /// </summary>
    public IEnumerable<Point> GetAllSelectedCells ()
    {
        if (TableIsNullOrInvisible () || Table!.Rows == 0)
        {
            return Enumerable.Empty<Point> ();
        }

        EnsureValidSelection ();
        HashSet<Point> toReturn = [];

        // If there are one or more rectangular selections
        if (MultiSelect && MultiSelectedRegions.Count > 0)
        {
            // Quiz any cells for whether they are selected.  For performance, we only need to check those between the top left and lower right vertex of
            // selection regions
            int yMin = MultiSelectedRegions.Min (r => r.Rectangle.Top);
            int yMax = MultiSelectedRegions.Max (r => r.Rectangle.Bottom);
            int xMin = FullRowSelect ? 0 : MultiSelectedRegions.Min (r => r.Rectangle.Left);
            int xMax = FullRowSelect ? Table.Columns : MultiSelectedRegions.Max (r => r.Rectangle.Right);

            for (int y = yMin; y < yMax; y++)
            {
                for (int x = xMin; x < xMax; x++)
                {
                    if (IsSelected (x, y))
                    {
                        toReturn.Add (new Point (x, y));
                    }
                }
            }
        }

        // if there are no region selections then it is just the cursor cell
        // if we are selecting the full row
        if (FullRowSelect)
        {
            // all cells in cursor row are selected
            for (var x = 0; x < Table.Columns; x++)
            {
                toReturn.Add (new Point (x, _cursorRow));
            }
        }
        else
        {
            // Not full row select and no multi selections
            toReturn.Add (new Point (_cursorColumn, _cursorRow));
        }

        return toReturn;
    }

    /// <summary>
    ///     <para>
    ///         Returns true if the given cell is selected either because it is the cursor cell or part of a multi cell
    ///         selection (e.g. <see cref="FullRowSelect"/>).
    ///     </para>
    ///     <remarks>Returns <see langword="false"/> if <see cref="ColumnStyle.Visible"/> is <see langword="false"/>.</remarks>
    /// </summary>
    /// <param name="col"></param>
    /// <param name="row"></param>
    /// <returns></returns>
    public bool IsSelected (int col, int row)
    {
        if (!IsColumnVisible (col))
        {
            return false;
        }

        if (GetMultiSelectedRegionsContaining (col, row).Any ())
        {
            return true;
        }

        return row == _cursorRow && (col == _cursorColumn || FullRowSelect);
    }

    /// <summary>True to allow multi-cell region selections. Defaults to <see langword="true"/>.</summary>
    public bool MultiSelect { get; set; } = true;

    /// <summary>
    ///     When <see cref="MultiSelect"/> is enabled, contains all rectangles of selected cells.  Rectangles
    ///     describe column/row regions selected in <see cref="Table"/> (not screen coordinates).
    ///     Use <see cref="Value"/> to read the current selection state (cursor + regions).
    /// </summary>
    public Stack<TableSelectionRegion> MultiSelectedRegions { get; } = new ();

    /// <summary>
    ///     When <see cref="MultiSelect"/> is on, creates selection over all cells in the table (replacing any old
    ///     selection regions)
    /// </summary>
    public bool SelectAll ()
    {
        if (TableIsNullOrInvisible () || !MultiSelect || Table!.Rows == 0)
        {
            return false;
        }

        ClearMultiSelectedRegions (true);

        // Create a single region over entire table, set the origin to the cursor cell so that a followup spread selection e.g. shift-right
        // behaves properly
        MultiSelectedRegions.Push (new TableSelectionRegion (new Point (_cursorColumn, _cursorRow), new Rectangle (0, 0, Table.Columns, _table!.Rows)));
        CommitSelectionState ();
        Update ();

        return true;
    }

    /// <summary>
    ///     Moves the cursor to the given col/row in <see cref="Table"/>.
    ///     Optionally starts a box selection (see <see cref="MultiSelect"/>).
    /// </summary>
    /// <param name="col">Column index.</param>
    /// <param name="row">Row index.</param>
    /// <param name="extendExistingSelection">True to create a multi cell selection or adjust an existing one</param>
    /// <param name="ctx">The command context.</param>
    public void SetSelection (int col, int row, bool extendExistingSelection, ICommandContext? ctx = null)
    {
        // if we are trying to increase the column index then
        // we are moving right otherwise we are moving left
        bool lookRight = col > _cursorColumn;
        col = GetNearestVisibleColumn (col, lookRight, true);

        if (!MultiSelect || !extendExistingSelection)
        {
            ClearMultiSelectedRegions (true);
        }

        if (extendExistingSelection)
        {
            // If we are extending current selection but there isn't one
            if (MultiSelectedRegions.Count == 0 || MultiSelectedRegions.All (m => m.IsExtended))
            {
                // Create a new region between the old cursor cell and the new cell
                TableSelectionRegion rect = CreateTableSelectionRegion (_cursorColumn, _cursorRow, col, row);
                MultiSelectedRegions.Push (rect);
            }
            else
            {
                // Extend the current head selection to include the new cell
                TableSelectionRegion head = MultiSelectedRegions.Pop ();
                TableSelectionRegion newRect = CreateTableSelectionRegion (head.Origin.X, head.Origin.Y, col, row);
                MultiSelectedRegions.Push (newRect);
            }
        }

        // Write backing fields directly and commit once to avoid double-fire
        _cursorColumn = TableIsNullOrInvisible () ? 0 : Math.Min (Table!.Columns - 1, Math.Max (0, col));
        _cursorRow = TableIsNullOrInvisible () ? 0 : Math.Min (Table!.Rows - 1, Math.Max (0, row));
        CommitSelectionState ();
    }

    /// <summary>
    ///     Returns <paramref name="columnIndex"/> unless the <see cref="ColumnStyle.Visible"/> is false for the indexed
    ///     column.  If so then the index returned is nudged to the nearest visible column.
    /// </summary>
    /// <remarks>Returns <paramref name="columnIndex"/> unchanged if it is invalid (e.g. out of bounds).</remarks>
    /// <param name="columnIndex">The input column index.</param>
    /// <param name="lookRight">
    ///     When nudging invisible selections look right first. <see langword="true"/> to look right,
    ///     <see langword="false"/> to look left.
    /// </param>
    /// <param name="allowBumpingInOppositeDirection">
    ///     If we cannot find anything visible when looking in direction of
    ///     <paramref name="lookRight"/> then should we look in the opposite direction instead? Use true if you want to push a
    ///     selection to a valid index no matter what. Use false if you are primarily interested in learning about directional
    ///     column visibility.
    /// </param>
    private int GetNearestVisibleColumn (int columnIndex, bool lookRight, bool allowBumpingInOppositeDirection) =>
        TryGetNearestVisibleColumn (columnIndex, lookRight, allowBumpingInOppositeDirection, out int answer) ? answer : columnIndex;

    private bool TryGetNearestVisibleColumn (int columnIndex, bool lookRight, bool allowBumpingInOppositeDirection, out int idx)
    {
        // if the column index provided is out of bounds
        if (_table is null || columnIndex < 0 || columnIndex >= _table.Columns)
        {
            idx = columnIndex;

            return false;
        }

        // get the column visibility by index (if no style visible is true)
        bool [] columnVisibility = Enumerable.Range (0, Table!.Columns).Select (c => Style.GetColumnStyleIfAny (c)?.Visible ?? true).ToArray ();

        // column is visible
        if (columnVisibility [columnIndex])
        {
            idx = columnIndex;

            return true;
        }

        int increment = lookRight ? 1 : -1;

        // move in that direction
        for (int i = columnIndex; i >= 0 && i < columnVisibility.Length; i += increment)
        {
            // if we find a visible column
            if (!columnVisibility [i])
            {
                continue;
            }

            idx = i;

            return true;
        }

        // Caller only wants to look in one direction, and we did not find any
        // visible columns in that direction
        if (!allowBumpingInOppositeDirection)
        {
            idx = columnIndex;

            return false;
        }

        // Caller will let us look in the other direction so
        // now look other way
        increment = -increment;

        for (int i = columnIndex; i >= 0 && i < columnVisibility.Length; i += increment)
        {
            // if we find a visible column
            if (!columnVisibility [i])
            {
                continue;
            }

            idx = i;

            return true;
        }

        // nothing seems to be visible so just return input index
        idx = columnIndex;

        return false;
    }

    /// <summary>
    ///     Returns a new rectangle between the two points with positive width/height regardless of relative positioning
    ///     of the points.  pt1 is always considered the <see cref="TableSelectionRegion.Origin"/> point
    /// </summary>
    /// <param name="pt1X">Origin point for the selection in X</param>
    /// <param name="pt1Y">Origin point for the selection in Y</param>
    /// <param name="pt2X">End point for the selection in X</param>
    /// <param name="pt2Y">End point for the selection in Y</param>
    /// <param name="toggle">True if selection is result of <see cref="Command.ToggleExtend"/></param>
    /// <returns></returns>
    private static TableSelectionRegion CreateTableSelectionRegion (int pt1X, int pt1Y, int pt2X, int pt2Y, bool toggle = false)
    {
        int top = Math.Max (Math.Min (pt1Y, pt2Y), 0);
        int bot = Math.Max (Math.Max (pt1Y, pt2Y), 0);
        int left = Math.Max (Math.Min (pt1X, pt2X), 0);
        int right = Math.Max (Math.Max (pt1X, pt2X), 0);

        // Rect class is inclusive of Top Left but exclusive of Bottom Right so extend by 1
        return new TableSelectionRegion (new Point (pt1X, pt1Y), new Rectangle (left, top, right - left + 1, bot - top + 1)) { IsExtended = toggle };
    }

    /// <summary>Returns a single point as a <see cref="TableSelectionRegion"/></summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    private static TableSelectionRegion CreateTableSelectionRegion (int x, int y) => CreateTableSelectionRegion (x, y, x, y);

    private void ClearMultiSelectedRegions (bool keepToggledSelections)
    {
        if (!keepToggledSelections)
        {
            MultiSelectedRegions.Clear ();

            return;
        }

        IEnumerable<TableSelectionRegion> oldRegions = MultiSelectedRegions.ToArray ().Reverse ();
        MultiSelectedRegions.Clear ();

        foreach (TableSelectionRegion region in oldRegions)
        {
            if (region.IsExtended)
            {
                MultiSelectedRegions.Push (region);
            }
        }
    }

    /// <summary>Syncs the <see cref="Value"/> from the internal cursor/region state.</summary>
    private void CommitSelectionState () => UpdateValueFromInternalState ();

    private IEnumerable<TableSelectionRegion> GetMultiSelectedRegionsContaining (int col, int row)
    {
        if (!MultiSelect)
        {
            return [];
        }

        return FullRowSelect
                   ? MultiSelectedRegions.Where (r => r.Rectangle.Bottom > row && r.Rectangle.Top <= row)
                   : MultiSelectedRegions.Where (r => r.Rectangle.Contains (col, row));
    }

    /// <summary>
    ///     Handles <see cref="Command.ToggleExtend"/>: extends or un-extends a cell from the multi-selection.
    ///     For keyboard (Space): toggles the current cell's extended state.
    ///     For mouse with Ctrl: unions the clicked cell into the selection.
    ///     For mouse with Alt: extends/creates a rectangular region to the clicked cell.
    /// </summary>
    private bool? ToggleExtend (ICommandContext? ctx)
    {
        // Mouse-based extend (Ctrl+Click or Alt+Click)
        if (ctx?.Binding is MouseBinding { MouseEvent: { } } mouseBinding)
        {
            return ToggleExtendMouse (mouseBinding);
        }

        // Keyboard-based toggle (Space)
        return ToggleExtendKeyboard ();
    }

    /// <summary>Handles keyboard-based ToggleExtend (Space key): toggles the current cell's extended state.</summary>
    private bool? ToggleExtendKeyboard ()
    {
        if (!MultiSelect)
        {
            return null;
        }

        TableSelectionRegion [] regions = GetMultiSelectedRegionsContaining (_cursorColumn, _cursorRow).ToArray ();
        TableSelectionRegion [] extendedAtCursor = regions.Where (s => s.IsExtended).ToArray ();

        if (extendedAtCursor.Length > 0)
        {
            // Toggle OFF: remove extended regions that contain the cursor cell
            IEnumerable<TableSelectionRegion> oldRegions = MultiSelectedRegions.ToArray ().Reverse ();
            MultiSelectedRegions.Clear ();

            foreach (TableSelectionRegion region in oldRegions)
            {
                if (!extendedAtCursor.Contains (region))
                {
                    MultiSelectedRegions.Push (region);
                }
            }
        }
        else if (regions.Length > 0)
        {
            // Cursor is inside a non-extended rectangular region — mark matching regions as extended
            IEnumerable<TableSelectionRegion> oldRegions = MultiSelectedRegions.ToArray ().Reverse ();
            MultiSelectedRegions.Clear ();

            foreach (TableSelectionRegion region in oldRegions)
            {
                MultiSelectedRegions.Push (regions.Contains (region)
                                               ? new TableSelectionRegion (region.Origin, region.Rectangle) { IsExtended = true }
                                               : region);
            }
        }
        else
        {
            // No region contains the cursor — toggle ON a single-cell extended region
            MultiSelectedRegions.Push (CreateTableSelectionRegion (_cursorColumn, _cursorRow, _cursorColumn, _cursorRow, true));
        }

        return true;
    }

    /// <summary>Handles mouse-based ToggleExtend: Ctrl+Click unions, Alt+Click extends.</summary>
    private bool? ToggleExtendMouse (MouseBinding mouseBinding)
    {
        int boundsX = mouseBinding.MouseEvent!.Position!.Value.X;
        int boundsY = mouseBinding.MouseEvent.Position!.Value.Y;
        Point? hit = ScreenToCell (boundsX, boundsY);

        if (hit is null || !MultiSelect)
        {
            return false;
        }

        if (mouseBinding.MouseEvent.Flags.FastHasFlags (MouseFlags.Ctrl))
        {
            UnionSelection (hit.Value.X, hit.Value.Y);
            Update ();

            return true;
        }

        if (!mouseBinding.MouseEvent.Flags.FastHasFlags (MouseFlags.Alt))
        {
            return false;
        }

        SetSelection (hit.Value.X, hit.Value.Y, true);
        Update ();

        return false;
    }

    /// <summary>
    ///     Toggles the provided cell in/out of the multi-selection. If the cell is already covered by a region,
    ///     that region is removed (toggle off). Otherwise, a new single-cell region is added (toggle on) and the
    ///     previous cursor position is preserved.
    /// </summary>
    private void UnionSelection (int col, int row)
    {
        if (!MultiSelect || TableIsNullOrInvisible ())
        {
            return;
        }

        EnsureValidSelection ();

        // Check if the target cell is already covered by an existing region
        TableSelectionRegion [] existingRegions = GetMultiSelectedRegionsContaining (col, row).ToArray ();

        if (existingRegions.Length > 0)
        {
            // Toggle OFF: remove all regions that contain the target cell
            IEnumerable<TableSelectionRegion> oldRegions = MultiSelectedRegions.ToArray ().Reverse ();
            MultiSelectedRegions.Clear ();

            foreach (TableSelectionRegion region in oldRegions)
            {
                if (!existingRegions.Contains (region))
                {
                    MultiSelectedRegions.Push (region);
                }
            }
        }
        else
        {
            // Toggle ON: add a region for the new cell
            int oldColumn = _cursorColumn;
            int oldRow = _cursorRow;

            _cursorColumn = col;
            _cursorRow = row;
            MultiSelectedRegions.Push (CreateTableSelectionRegion (col, row));

            // Retain the old cursor position in the selection if it's not already covered
            if (!IsSelected (oldColumn, oldRow))
            {
                MultiSelectedRegions.Push (CreateTableSelectionRegion (oldColumn, oldRow));
            }
        }

        CommitSelectionState ();
    }

    #endregion Selection

    #region IValue<TableSelection?> Implementation

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<object?>>? ValueChangedUntyped;

    /// <inheritdoc/>
    public event EventHandler<ValueChangingEventArgs<TableSelection?>>? ValueChanging;

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<TableSelection?>>? ValueChanged;

    /// <summary>
    ///     Called when <see cref="Value"/> is about to change. Return <see langword="true"/> to cancel the change.
    /// </summary>
    protected virtual bool OnValueChanging (ValueChangingEventArgs<TableSelection?> args) => false;

    /// <summary>
    ///     Called when <see cref="Value"/> has changed.
    /// </summary>
    protected virtual void OnValueChanged (ValueChangedEventArgs<TableSelection?> args) { }

    private TableSelection? _value;

    /// <inheritdoc/>
    public TableSelection? Value
    {
        get => _value;
        set
        {
            if (Equals (_value, value))
            {
                return;
            }

            TableSelection? oldValue = _value;
            ValueChangingEventArgs<TableSelection?> changingArgs = new (oldValue, value);

            if (OnValueChanging (changingArgs) || changingArgs.Handled)
            {
                return;
            }

            ValueChanging?.Invoke (this, changingArgs);

            if (changingArgs.Handled)
            {
                return;
            }

            _value = changingArgs.NewValue;
            SetNeedsDraw ();

            // Sync internal cursor state from Value
            SyncCursorFromValue ();

            ValueChangedEventArgs<TableSelection?> changedArgs = new (oldValue, _value);
            OnValueChanged (changedArgs);
            ValueChanged?.Invoke (this, changedArgs);
            ValueChangedUntyped?.Invoke (this, new ValueChangedEventArgs<object?> (oldValue, _value));
        }
    }

    /// <summary>
    ///     Builds a <see cref="TableSelection"/> from the current internal state and sets <see cref="Value"/>.
    /// </summary>
    private void UpdateValueFromInternalState ()
    {
        if (TableIsNullOrInvisible ())
        {
            Value = null;

            return;
        }

        // Deep-copy regions so Value snapshots are immutable
        List<TableSelectionRegion> regions = MultiSelectedRegions.Reverse ()
                                                                 .Select (r => new TableSelectionRegion (r.Origin, r.Rectangle) { IsExtended = r.IsExtended })
                                                                 .ToList ();
        TableSelection newSelection = new (new Point (_cursorColumn, _cursorRow), regions);
        Value = newSelection;
    }

    #endregion IValue<TableSelection?> Implementation
}
