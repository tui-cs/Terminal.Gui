namespace Terminal.Gui.Views;

public partial class TextView
{
    /// <summary>
    ///     If <see langword="true"/> and the current <see cref="Cell.Attribute"/> is null will inherit from the
    ///     previous, otherwise if <see langword="false"/> (default) do nothing. If the text is load with
    ///     <see cref="Load(List{Cell})"/> this property is automatically sets to <see langword="true"/>.
    /// </summary>
    public bool InheritsPreviousAttribute { get; set; }

    private bool _isDrawing;

    /// <inheritdoc/>
    protected override bool OnDrawingText (DrawContext? context) => true;

    /// <inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        _isDrawing = true;

        SetAttributeForRole (Enabled ? VisualRole.Editable : VisualRole.Disabled);

        int right = Viewport.Width;
        int bottom = Viewport.Height;
        var row = 0;

        for (int idxRow = Viewport.Y; idxRow < _model.Count; idxRow++)
        {
            List<Cell> line = _model.GetLine (idxRow);
            int lineRuneCount = line.Count;
            _ = TextModel.CursorColumn (TextModel.CellsToStringList (line), Viewport.X, TabWidth, out List<int> glyphWidths, out _);
            int colWidths = TextModel.GetColumnWidthsBeforeStart (glyphWidths, Viewport.X, out int col, out int viewportX);
            bool wasPreviousWideGlyphNegativeCol = false;

            Move (0, row);

            for (int idxCol = viewportX; idxCol < lineRuneCount; idxCol++)
            {
                string text = idxCol >= lineRuneCount ? " " : line [idxCol].Grapheme;
                int cols = text.GetColumns (false);

                if (idxCol < line.Count && IsSelecting && PointInSelection (idxCol, idxRow))
                {
                    OnDrawSelectionColor (line, idxCol, idxRow);
                }
                else if (idxCol == CurrentColumn && idxRow == CurrentRow && !IsSelecting && !Used && HasFocus && idxCol < lineRuneCount)
                {
                    OnDrawUsedColor (line, idxCol, idxRow);
                }
                else if (ReadOnly)
                {
                    OnDrawReadOnlyColor (line, idxCol, idxRow);
                }
                else
                {
                    OnDrawNormalColor (line, idxCol, idxRow);
                }

                if (text == "\t")
                {
                    if (TabWidth > 0)
                    {
                        // Calculate columns to next tab stop
                        // Tab stops are at multiples of TabWidth (0, 4, 8, 12, ...)
                        cols = TabWidth - colWidths % TabWidth;
                    }
                    else
                    {
                        // When TabWidth is 0, tabs are invisible (0 columns)
                        cols = 0;
                    }

                    if (col + cols > right)
                    {
                        cols = right - col;
                    }

                    for (var i = 0; i < cols; i++)
                    {
                        if (col + i < right)
                        {
                            AddRune (col + i, row, (Rune)' ');
                        }
                    }
                }
                else
                {
                    if (col < 0 && cols > 1)
                    {
                        wasPreviousWideGlyphNegativeCol = true;
                    }
                    else
                    {
                        AddStr (col, row, text);
                    }

                    // Ensures that cols less than 0 to be 1 because it will be converted to a printable rune
                    cols = Math.Max (cols, 1);
                }

                if (!TextModel.SetCol (ref col, Viewport.Right, cols))
                {
                    break;
                }

                colWidths += cols;

                if (idxCol + 1 < lineRuneCount && col + line [idxCol + 1].Grapheme.GetColumns () > right)
                {
                    break;
                }
            }

            if (wasPreviousWideGlyphNegativeCol)
            {
                AddStr (0, row, " ");
            }

            if (col < right)
            {
                SetAttributeForRole (ReadOnly ? VisualRole.ReadOnly : VisualRole.Editable);
                ClearRegion (col, row, right, row + 1);
            }

            row++;
        }

        if (row < bottom)
        {
            SetAttributeForRole (ReadOnly ? VisualRole.ReadOnly : VisualRole.Editable);
            ClearRegion (0, row, right, bottom);
        }

        _isDrawing = false;

        return false;
    }

    // TODO: These events should be refactored to use CWP

    /// <summary>Invoked when the normal color is drawn.</summary>
    public event EventHandler<CellEventArgs>? DrawNormalColor;

    /// <summary>
    ///     Sets the <see cref="View.Driver"/> to an appropriate color for rendering the given <paramref name="idxCol"/>
    ///     of the current <paramref name="line"/>. Override to provide custom coloring by calling
    ///     <see cref="View.SetAttribute"/> Defaults to <see cref="Scheme.Normal"/>.
    /// </summary>
    /// <param name="line">The line.</param>
    /// <param name="idxCol">The col index.</param>
    /// <param name="idxRow">The row index.</param>
    protected virtual void OnDrawNormalColor (List<Cell> line, int idxCol, int idxRow)
    {
        (int Row, int Col) unwrappedPos = GetUnwrappedPosition (idxRow, idxCol);
        var ev = new CellEventArgs (line, idxCol, unwrappedPos);
        DrawNormalColor?.Invoke (this, ev);

        if (line [idxCol].Attribute is { })
        {
            Attribute? attribute = line [idxCol].Attribute;
            SetAttribute ((Attribute)attribute!);
        }
        else
        {
            SetAttribute (GetAttributeForRole (VisualRole.Editable));
        }
    }

    /// <summary>Invoked when the ready only color is drawn.</summary>
    public event EventHandler<CellEventArgs>? DrawReadOnlyColor;

    /// <summary>
    ///     Sets the <see cref="View.Driver"/> to an appropriate color for rendering the given <paramref name="idxCol"/>
    ///     of the current <paramref name="line"/>. Override to provide custom coloring by calling
    ///     <see cref="View.SetAttribute(Attribute)"/> Defaults to <see cref="Scheme.Focus"/>.
    /// </summary>
    /// <param name="line">The line.</param>
    /// <param name="idxCol">The col index.</param>
    /// ///
    /// <param name="idxRow">The row index.</param>
    protected virtual void OnDrawReadOnlyColor (List<Cell> line, int idxCol, int idxRow)
    {
        (int Row, int Col) unwrappedPos = GetUnwrappedPosition (idxRow, idxCol);
        var ev = new CellEventArgs (line, idxCol, unwrappedPos);
        DrawReadOnlyColor?.Invoke (this, ev);

        Attribute? cellAttribute = line [idxCol].Attribute is { } ? line [idxCol].Attribute : GetAttributeForRole (VisualRole.ReadOnly);

        if (cellAttribute!.Value.Foreground == cellAttribute.Value.Background)
        {
            SetAttribute (new Attribute (cellAttribute.Value.Foreground, cellAttribute.Value.Background, cellAttribute.Value.Style));
        }
        else
        {
            SetAttributeForRole (VisualRole.ReadOnly);
        }
    }

    /// <summary>Invoked when the selection color is drawn.</summary>
    public event EventHandler<CellEventArgs>? DrawSelectionColor;

    /// <summary>
    ///     Sets the <see cref="View.Driver"/> to an appropriate color for rendering the given <paramref name="idxCol"/>
    ///     of the current <paramref name="line"/>. Override to provide custom coloring by calling
    ///     <see cref="View.SetAttribute(Attribute)"/> Defaults to <see cref="Scheme.Focus"/>.
    /// </summary>
    /// <param name="line">The line.</param>
    /// <param name="idxCol">The col index.</param>
    /// ///
    /// <param name="idxRow">The row index.</param>
    protected virtual void OnDrawSelectionColor (List<Cell> line, int idxCol, int idxRow)
    {
        (int Row, int Col) unwrappedPos = GetUnwrappedPosition (idxRow, idxCol);
        CellEventArgs ev = new (line, idxCol, unwrappedPos);
        DrawSelectionColor?.Invoke (this, ev);

        if (line [idxCol].Attribute is { })
        {
            Attribute? attribute = line [idxCol].Attribute;
            Attribute? active = GetAttributeForRole (VisualRole.Active);
            SetAttribute (new Attribute (active.Value.Foreground, active.Value.Background, attribute!.Value.Style));
        }
        else
        {
            SetAttributeForRole (VisualRole.Active);
        }
    }

    /// <summary>
    ///     Invoked when the used color is drawn. The Used Color is used to indicate if the <see cref="Key.InsertChar"/>
    ///     was pressed and enabled.
    /// </summary>
    public event EventHandler<CellEventArgs>? DrawUsedColor;

    /// <summary>
    ///     Sets the <see cref="View.Driver"/> to an appropriate color for rendering the given <paramref name="idxCol"/>
    ///     of the current <paramref name="line"/>. Override to provide custom coloring by calling
    ///     <see cref="View.SetAttribute(Attribute)"/> Defaults to <see cref="Scheme.HotFocus"/>.
    /// </summary>
    /// <param name="line">The line.</param>
    /// <param name="idxCol">The col index.</param>
    /// ///
    /// <param name="idxRow">The row index.</param>
    protected virtual void OnDrawUsedColor (List<Cell> line, int idxCol, int idxRow)
    {
        (int Row, int Col) unwrappedPos = GetUnwrappedPosition (idxRow, idxCol);
        CellEventArgs ev = new (line, idxCol, unwrappedPos);
        DrawUsedColor?.Invoke (this, ev);

        if (line [idxCol].Attribute is { })
        {
            Attribute? attribute = line [idxCol].Attribute;
            SetValidUsedColor (attribute!);
        }
        else
        {
            SetValidUsedColor (GetAttributeForRole (VisualRole.Focus));
        }
    }

    private void SetValidUsedColor (Attribute? attribute) =>
        SetAttribute (new Attribute (attribute!.Value.Background, attribute.Value.Foreground, attribute.Value.Style));

    /// <inheritdoc/>
    protected override bool OnGettingAttributeForRole (in VisualRole role, ref Attribute currentAttribute)
    {
        if (role != VisualRole.Normal)
        {
            return base.OnGettingAttributeForRole (role, ref currentAttribute);
        }

        return base.OnGettingAttributeForRole (VisualRole.Editable, ref currentAttribute);
    }

    private void ClearRegion (int left, int top, int right, int bottom)
    {
        for (int row = top; row < bottom; row++)
        {
            Move (left, row);

            for (int col = left; col < right; col++)
            {
                AddRune (col, row, (Rune)' ');
            }
        }
    }

    private Attribute? GetSelectedAttribute (int row, int col)
    {
        if (!InheritsPreviousAttribute || (Lines == 1 && GetLine (Lines).Count == 0))
        {
            return null;
        }

        List<Cell> line = GetLine (row);
        int foundRow = row;

        while (line.Count == 0)
        {
            if (foundRow == 0 && line.Count == 0)
            {
                return null;
            }

            foundRow--;
            line = GetLine (foundRow);
        }

        int foundCol = foundRow < row ? line.Count - 1 : Math.Min (col, line.Count - 1);

        Cell cell = line [foundCol];

        return cell.Attribute;
    }

    // If InheritsPreviousScheme is enabled this method will check if the rune cell on
    // the row and col location and around has a not null scheme. If it's null will set it with
    // the very most previous valid scheme.
    private void ProcessInheritsPreviousScheme (int row, int col)
    {
        if (!InheritsPreviousAttribute || (Lines == 1 && GetLine (Lines).Count == 0))
        {
            return;
        }

        List<Cell> line = GetLine (row);
        List<Cell> lineToSet = line;

        while (line.Count == 0)
        {
            if (row == 0 && line.Count == 0)
            {
                return;
            }

            row--;
            line = GetLine (row);
            lineToSet = line;
        }

        int colWithColor = Math.Max (Math.Min (col - 2, line.Count - 1), 0);
        Cell cell = line [colWithColor];
        int colWithoutColor = Math.Max (col - 1, 0);

        Cell lineTo = lineToSet [colWithoutColor];

        switch (cell.Attribute)
        {
            case { } when colWithColor == 0 && lineTo.Attribute is { }:
                {
                    for (int r = row - 1; r > -1; r--)
                    {
                        List<Cell> l = GetLine (r);

                        for (int c = l.Count - 1; c > -1; c--)
                        {
                            Cell cell1 = l [c];

                            if (cell1.Attribute is null)
                            {
                                cell1.Attribute = cell.Attribute;
                                l [c] = cell1;
                            }
                            else
                            {
                                return;
                            }
                        }
                    }

                    return;
                }

            case null:
                {
                    for (int r = row; r > -1; r--)
                    {
                        List<Cell> l = GetLine (r);

                        colWithColor = l.FindLastIndex (colWithColor > -1 ? colWithColor : l.Count - 1, c => c.Attribute != null);

                        if (colWithColor <= -1 || l [colWithColor].Attribute is null)
                        {
                            continue;
                        }
                        cell = l [colWithColor];

                        break;
                    }

                    break;
                }

            default:
                {
                    int cRow = row;

                    while (cell.Attribute is null)
                    {
                        if ((colWithColor == 0 || cell.Attribute is null) && cRow > 0)
                        {
                            line = GetLine (--cRow);
                            colWithColor = line.Count - 1;
                            cell = line [colWithColor];
                        }
                        else if (cRow == 0 && colWithColor < line.Count)
                        {
                            cell = line [colWithColor + 1];
                        }
                    }

                    break;
                }
        }

        if (cell.Attribute is null || colWithColor <= -1 || colWithoutColor >= lineToSet.Count || lineTo.Attribute is { })
        {
            return;
        }

        while (lineTo.Attribute is null)
        {
            lineTo.Attribute = cell.Attribute;
            lineToSet [colWithoutColor] = lineTo;
            colWithoutColor--;

            if (colWithoutColor != -1 || row <= 0)
            {
                continue;
            }
            lineToSet = GetLine (--row);
            colWithoutColor = lineToSet.Count - 1;
        }
    }

    internal void ApplyCellsAttribute (Attribute attribute)
    {
        if (ReadOnly || SelectedLength <= 0)
        {
            return;
        }
        int startRow = Math.Min (SelectionStartRow, CurrentRow);
        int endRow = Math.Max (CurrentRow, SelectionStartRow);
        int startCol = SelectionStartRow <= CurrentRow ? SelectionStartColumn : CurrentColumn;
        int endCol = CurrentRow >= SelectionStartRow ? CurrentColumn : SelectionStartColumn;
        List<List<Cell>> selectedCellsOriginal = [];
        List<List<Cell>> selectedCellsChanged = [];

        for (int r = startRow; r <= endRow; r++)
        {
            List<Cell> line = GetLine (r);

            selectedCellsOriginal.Add ([.. line]);

            for (int c = r == startRow ? startCol : 0; c < (r == endRow ? endCol : line.Count); c++)
            {
                Cell cell = line [c]; // Copy value to a new variable
                cell.Attribute = attribute; // Modify the copy
                line [c] = cell; // Assign the modified copy back
            }

            selectedCellsChanged.Add ([.. GetLine (r)]);
        }

        GetSelectedRegion ();
        IsSelecting = false;

        _historyText.Add ([.. selectedCellsOriginal], new Point (startCol, startRow));

        _historyText.Add ([.. selectedCellsChanged], new Point (startCol, startRow), TextEditingLineStatus.Attribute);
    }

    private Attribute? GetSelectedCellAttribute ()
    {
        List<Cell> line;

        if (SelectedLength > 0)
        {
            line = GetLine (SelectionStartRow);

            if (line [Math.Min (SelectionStartColumn, line.Count - 1)].Attribute is { } attributeSel)
            {
                return new Attribute (attributeSel);
            }

            return GetAttributeForRole (VisualRole.Active);
        }

        line = GetCurrentLine ();

        if (line [Math.Min (CurrentColumn, line.Count - 1)].Attribute is { } attribute)
        {
            return new Attribute (attribute);
        }

        return GetAttributeForRole (VisualRole.Active);
    }
}
