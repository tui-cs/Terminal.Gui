namespace Terminal.Gui.Views;

public partial class TableView
{
    /// <summary>
    ///     Calculates the current header height based on what is visible.
    ///     This respects the viewport Y position and the <see cref="TableStyle.AlwaysShowHeaders"/> style.
    /// </summary>
    /// <returns>Header height in rows.</returns>
    protected int CurrentHeaderHeightVisible ()
    {
        if (!ShouldRenderHeaders ())
        {
            return 0;
        }

        if (Style.AlwaysShowHeaders)
        {
            return Math.Min (GetHeaderHeight (), Viewport.Height);
        }

        return Math.Min (Math.Max (GetHeaderHeight () - Viewport.Y, 0), Viewport.Height);
    }

    /// <summary>Returns the amount of vertical space required to display the header</summary>
    /// <returns></returns>
    internal int GetHeaderHeight ()
    {
        int heightRequired = Style.ShowHeaders ? 1 : 0;

        if (Style.ShowHorizontalHeaderOverline)
        {
            heightRequired++;
        }

        if (Style.ShowHorizontalHeaderUnderline)
        {
            heightRequired++;
        }

        return heightRequired;
    }

    /// <summary>Returns the amount of vertical space currently occupied by the header or 0 if it is not visible.</summary>
    /// <returns></returns>
    internal int GetHeaderHeightIfAny () => ShouldRenderHeaders () ? GetHeaderHeight () : 0;

    ///<inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        Move (0, 0);

        // What columns to render at what X offset in viewport
        ColumnToRender [] cellInfos = NonHiddenCellInfos ();
        SetAttribute (GetAttributeForRole (VisualRole.Normal));

        // invalidate current row (prevents scrolling around leaving old characters in the frame
        AddStr (new string (' ', Viewport.Width));
        var line = 0;
        var headerLinesHandled = 0;
        int availableWidth = GetContentWidth ();

        if (ShouldRenderHeaders ())
        {
            // Render something like:
            /*
                ┌────────────────────┬─────────┬────────────┬──────────────┬──────────┐
                │ArithmeticComparator│chi      │Health board│Interpretation│Lab number│
                └────────────────────┴─────────┴────────────┴──────────────┴──────────┘
            */

            bool ShouldRenderNextHeaderLine () =>

                //is the header line not scrolled or shall it always be shown? and do we have space to render it?
                (Viewport.Y <= headerLinesHandled || Style.AlwaysShowHeaders) && line < Viewport.Height;

            if (Style.ShowHorizontalHeaderOverline)
            {
                if (ShouldRenderNextHeaderLine ())
                {
                    RenderHeaderOverline (line, availableWidth, cellInfos);
                    line++;
                }
                headerLinesHandled++;
            }

            if (Style.ShowHeaders)
            {
                if (ShouldRenderNextHeaderLine ())
                {
                    RenderHeaderMidline (line, availableWidth, cellInfos);
                    line++;
                }
                headerLinesHandled++;
            }

            if (Style.ShowHorizontalHeaderUnderline)
            {
                if (ShouldRenderNextHeaderLine ())
                {
                    RenderHeaderUnderline (line, availableWidth, cellInfos);
                    line++;
                }
                headerLinesHandled++;
            }
        }

        int headerLinesConsumed = line;

        int locRowOffset = Style.AlwaysShowHeaders ? Viewport.Y : Math.Max (Viewport.Y - headerLinesHandled, 0);

        // render the cells
        for (; line < Viewport.Height; line++)
        {
            ClearLine (line, Viewport.Width);

            // work out what Row to render
            int rowToRender = locRowOffset + (line - headerLinesConsumed);

            // if we have run off the end of the table
            if (TableIsNullOrInvisible () || rowToRender < 0)
            {
                continue;
            }

            // No more data
            if (rowToRender >= Table!.Rows)
            {
                if (rowToRender == Table.Rows && Style.ShowHorizontalBottomLine)
                {
                    RenderBottomLine (line, availableWidth, cellInfos);
                }

                continue;
            }

            RenderRow (line, rowToRender, cellInfos);
        }

        return true;
    }

    /// <summary>
    ///     Override to provide custom multi-coloring to cells. Use methods like <see cref="View.AddStr(string)"/>.
    ///     The terminal cursor will already be in the correct position when rendering. You must render the full
    ///     <paramref name="render"/> or the view will not look right. For simpler color provision use
    ///     <see cref="ColumnStyle.ColorGetter"/>. For changing the content that is rendered use
    ///     <see cref="ColumnStyle.RepresentationGetter"/>.
    /// </summary>
    /// <param name="cellAttribute">The <see cref="Attribute"/> to use for the cell.</param>
    /// <param name="render"></param>
    /// <param name="isPrimaryCell">True if this cell is the cursor cell (used for inversion).</param>
    protected virtual void RenderCell (Attribute cellAttribute, string render, bool isPrimaryCell)
    {
        // If the cell is the selected col/row then draw the first rune in inverted colors
        // this allows the user to track which cell is the active one during a multi cell
        // selection or in full row select mode
        if (Style.InvertSelectedCellFirstCharacter && isPrimaryCell)
        {
            if (render.Length == 0)
            {
                return;
            }

            // invert the color of the current cell for the first character
            SetAttribute (new Attribute (cellAttribute.Foreground, cellAttribute.Background, TextStyle.Reverse));
            AddRune (render [0]);

            if (render.Length <= 1)
            {
                return;
            }

            SetAttribute (cellAttribute);
            AddStr (render [1..]);
        }
        else
        {
            SetAttribute (cellAttribute);
            AddStr (render);
        }
    }

    private void RenderBottomLine (int row, int availableWidth, ColumnToRender [] columnsToRender)
    {
        // Renders a line at the bottom of the table after all the data like:
        // └─────────────────────────────────┴──────────┴──────┴──────────┴────────┴────────────────────────────────────────────┘
        bool renderFirstOuterVerticalLine = Style.ShowVerticalCellLines && Style.ShowVerticalCellLineForFirstColumn;
        bool renderLastOuterVerticalLine = Style.ShowVerticalCellLines && Style.ShowVerticalCellLineForLastColumn;

        for (var c = 0; c < availableWidth; c++)
        {
            // Start by assuming we just draw a straight line the
            // whole way but update to instead draw BottomTee / Corner etc
            Rune rune = Glyphs.HLine;

            if (Style.ShowVerticalCellLines)
            {
                if (c == 0)
                {
                    if (renderFirstOuterVerticalLine)
                    {
                        rune = Glyphs.LLCorner;
                    }
                }
                else if (columnsToRender.Any (r => r.X == c + 1))
                {
                    // if the next column is the start of a header
                    rune = Glyphs.BottomTee;
                }
                else if (c == availableWidth - 1)
                {
                    if (renderLastOuterVerticalLine)
                    {
                        rune = Glyphs.LRCorner;
                    }
                }
                else if (!Style.ExpandLastColumn && columnsToRender.Any (r => r.IsVeryLast && r.X + r.Width - 1 == c))
                {
                    // if the next console column is the last column's end
                    rune = Glyphs.BottomTee;
                }
            }

            RenderRune (c, row, rune);
        }
    }

    private void RenderRune (int col, int row, Rune rune)
    {
        if (col >= Viewport.X && col < Viewport.X + Viewport.Width)
        {
            AddRuneAt (col - Viewport.X, row, rune);
        }
    }

    private void RenderHeaderMidline (int row, int availableWidth, ColumnToRender [] columnsToRender)
    {
        // Renders something like:
        // │ArithmeticComparator│chi       │Health board│Interpretation│Lab number│
        ClearLine (row, Viewport.Width);

        // render start of line
        if (_style.ShowVerticalHeaderLines && Style.ShowVerticalCellLineForFirstColumn)
        {
            RenderRune (0, row, Glyphs.VLine);
        }

        foreach (ColumnToRender current in columnsToRender)
        {
            ColumnStyle? colStyle = Style.GetColumnStyleIfAny (current.Column);
            string colName = _table!.ColumnNames [current.Column];

            // Determine header color
            Scheme baseScheme = Style.HeaderScheme ?? GetScheme ();
            Scheme? headerScheme = colStyle?.HeaderColorGetter?.Invoke (
                new HeaderColorGetterArgs (_table, current.Column, colName, baseScheme));
            Scheme effectiveScheme = headerScheme ?? baseScheme;

            // Draw separator lines with normal attribute so focus colors don't bleed into lines
            SetAttribute (GetAttributeForRole (VisualRole.Normal));
            RenderSeparator (current.X - 1, row, true);

            // Now set the header text attribute
            SetAttribute (HasFocus ? effectiveScheme.Focus : effectiveScheme.Normal);
            Move (current.X - Viewport.X, row);
            AddStr (TruncateOrPad (colName, colName, current.Width, colStyle));

            if (!Style.ExpandLastColumn && current.IsVeryLast && Style.ShowVerticalCellLineForLastColumn)
            {
                SetAttribute (GetAttributeForRole (VisualRole.Normal));
                RenderSeparator (current.X + current.Width - 1, row, true);
            }
        }

        // Reset attribute after headers
        SetAttribute (GetAttributeForRole (VisualRole.Normal));

        // render end of line
        if (_style.ShowVerticalHeaderLines && Style.ShowVerticalCellLineForLastColumn)
        {
            RenderRune (availableWidth - 1, row, Glyphs.VLine);
        }
    }

    private void RenderHeaderOverline (int row, int availableWidth, ColumnToRender [] columnsToRender)
    {
        // Renders a line above table headers (when visible) like:
        // ┌────────────────────┬──────────┬───────────┬──────────────┬─────────┐
        bool renderFirstOuterVerticalLine = Style.ShowVerticalHeaderLines && Style.ShowVerticalCellLineForFirstColumn;
        bool renderLastOuterVerticalLine = Style.ShowVerticalHeaderLines && Style.ShowVerticalCellLineForLastColumn;

        for (var c = 0; c < availableWidth; c++)
        {
            Rune rune = Glyphs.HLine;

            if (Style.ShowVerticalHeaderLines)
            {
                if (c == 0)
                {
                    if (renderFirstOuterVerticalLine)
                    {
                        rune = Glyphs.ULCorner;
                    }
                }

                // if the next column is the start of a header
                else if (columnsToRender.Any (r => r.X == c + 1))
                {
                    rune = Glyphs.TopTee;
                }
                else if (c == availableWidth - 1)
                {
                    if (renderLastOuterVerticalLine)
                    {
                        rune = Glyphs.URCorner;
                    }
                }

                // if the next console column is the last column's end
                else if (!Style.ExpandLastColumn && columnsToRender.Any (r => r.IsVeryLast && r.X + r.Width - 1 == c))
                {
                    rune = Glyphs.TopTee;
                }
            }

            if (App?.Screen.Height > 0)
            {
                RenderRune (c, row, rune);
            }
        }
    }

    private void RenderHeaderUnderline (int row, int availableWidth, ColumnToRender [] columnsToRender)
    {
        /*
         *  Now lets draw the line itself
         */
        // Renders a line below the table headers (when visible) like:
        // ├──────────┼───────────┼───────────────────┼──────────┼────────┼─────────────┤
        bool renderFirstOuterVerticalLine = Style.ShowVerticalHeaderLines && Style.ShowVerticalCellLineForFirstColumn;
        bool renderLastOuterVerticalLine = Style.ShowVerticalHeaderLines && Style.ShowVerticalCellLineForLastColumn;

        for (var c = 0; c < availableWidth; c++)
        {
            // Start by assuming we just draw a straight line the
            // whole way but update to instead draw a header indicator
            // or scroll arrow etc
            Rune rune = Glyphs.HLine;

            if (Style.ShowVerticalHeaderLines)
            {
                if (c == 0)
                {
                    if (renderFirstOuterVerticalLine)
                    {
                        rune = Style.ShowVerticalCellLines ? Glyphs.LeftTee : Glyphs.LLCorner;
                    }
                }

                // if the next column is the start of a header
                else if (columnsToRender.Any (r => r.X == c + 1))
                {
                    rune = Style.ShowVerticalCellLines ? Glyphs.Cross : Glyphs.BottomTee;
                }
                else if (c == availableWidth - 1)
                {
                    if (renderLastOuterVerticalLine)
                    {
                        rune = Style.ShowVerticalCellLines ? Glyphs.RightTee : Glyphs.LRCorner;
                    }
                }

                // if the next console column is the last column's end
                else if (!Style.ExpandLastColumn && columnsToRender.Any (r => r.IsVeryLast && r.X + r.Width - 1 == c))
                {
                    rune = Style.ShowVerticalCellLines ? Glyphs.Cross : Glyphs.BottomTee;
                }
            }

            RenderRune (c, row, rune);
        }
    }

    private void RenderRow (int row, int rowToRender, ColumnToRender [] columnsToRender)
    {
        bool focused = HasFocus;
        Scheme rowScheme = Style.RowColorGetter?.Invoke (new RowColorGetterArgs (Table!, rowToRender)) ?? GetScheme ();

        // start by clearing the entire line
        // not needed, see Attribute below:
        ClearLine (row, Viewport.Width);
        Move (0, row);
        Attribute? attribute;

        if (FullRowSelect && IsSelected (0, rowToRender))
        {
            attribute = focused ? rowScheme.Focus : rowScheme.Active;
        }
        else
        {
            attribute = Enabled ? rowScheme.Normal : rowScheme.Active;
        }

        SetAttribute (attribute.Value);
        AddStr (new string (' ', Viewport.Width));

        // Render cells for each visible header for the current row
        foreach (ColumnToRender current in columnsToRender)
        {
            ColumnStyle? colStyle = Style.GetColumnStyleIfAny (current.Column);

            // Set scheme based on whether the current cell is the selected one
            bool isSelectedCell = IsSelected (current.Column, rowToRender);
            object val = Table! [rowToRender, current.Column];

            // Render the (possibly truncated) cell value
            string representation = GetRepresentation (val, colStyle);

            // to get the colour scheme
            CellColorGetterDelegate? schemeGetter = colStyle?.ColorGetter;
            Scheme scheme;

            if (schemeGetter is { })
            {
                // user has a delegate for defining row color per cell, call it
                // if users custom color getter returned null, use the row scheme
                scheme = schemeGetter (new CellColorGetterArgs (Table, rowToRender, current.Column, val, representation, rowScheme)) ?? rowScheme;
            }
            else
            {
                // There is no custom cell coloring delegate so use the scheme for the row
                scheme = rowScheme;
            }

            Attribute cellColor = isSelectedCell ? focused ? scheme.Focus : scheme.Active : Enabled ? scheme.Normal : scheme.Disabled;
            string render = TruncateOrPad (val, representation, current.Width, colStyle);

            // While many cells can be selected (see MultiSelectedRegions) only one cell is the primary (drives navigation etc.)
            bool isPrimaryCell = current.Column == _cursorColumn && rowToRender == _cursorRow;
            Move (current.X - Viewport.X, row);
            RenderCell (cellColor, render, isPrimaryCell);

            // Reset scheme to normal for drawing separators if we drew text with custom scheme
            if (scheme != rowScheme)
            {
                if (isSelectedCell)
                {
                    attribute = focused ? rowScheme.Focus : rowScheme.Active;
                }
                else
                {
                    attribute = Enabled ? rowScheme.Normal : rowScheme.Disabled;
                }

                SetAttribute (attribute.Value);
            }

            // If not in full row select mode always, reset scheme to normal and render the vertical line (or space) at the end of the cell
            if (!FullRowSelect)
            {
                SetAttribute (Enabled ? rowScheme.Normal : rowScheme.Disabled);
            }

            if (_style is { AlwaysUseNormalColorForVerticalCellLines: true, ShowVerticalCellLines: true })
            {
                SetAttribute (rowScheme.Normal);
            }

            RenderSeparator (current.X - 1, row, false);

            if (!Style.ExpandLastColumn && current.IsVeryLast && Style.ShowVerticalCellLineForLastColumn)
            {
                RenderSeparator (current.X + current.Width - 1, row, false);
            }

            // When vertical cell lines are not rendered AND the separator symbol is the default
            // invisible space, extend the cell's background color into the 1-char gap at the
            // cell's right edge. The cell render itself only fills (Width - 1) chars (TruncateOrPad
            // intentionally leaves 1 char for the cell boundary). Without this, the gap retains
            // the row-clear's rowScheme color, producing visible "stripes" between cells when a
            // custom ColumnStyle.ColorGetter is in use (e.g. FileDialog). See issue #5075.
            if (!_style.ShowVerticalCellLines && SeparatorSymbol == ' ')
            {
                int gapX = current.X + current.Width - 1;

                if (gapX >= Viewport.X && gapX < Viewport.X + Viewport.Width)
                {
                    SetAttribute (cellColor);
                    AddRuneAt (gapX - Viewport.X, row, (Rune)' ');
                }
            }
        }

        if (!_style.ShowVerticalCellLines)
        {
            return;
        }

        SetAttribute (rowScheme.Normal);

        // render start and end of line
        if (Style.ShowVerticalCellLineForFirstColumn)
        {
            RenderRune (0, row, Glyphs.VLine);
        }

        ColumnToRender? lastCol = columnsToRender.LastOrDefault ();

        if (lastCol != null && Style.ShowVerticalCellLineForLastColumn)
        {
            RenderRune (lastCol.X + lastCol.Width - 1, row, Glyphs.VLine);
        }
    }

    private void RenderSeparator (int col, int row, bool isHeader)
    {
        if (col < 0)
        {
            return;
        }

        bool renderLines = isHeader ? _style.ShowVerticalHeaderLines : _style.ShowVerticalCellLines;

        // When vertical lines are not rendered AND the separator symbol is the default space,
        // skip drawing here. RenderRow handles the gap between cells separately by extending
        // the cell's own background into the gap position (preserves custom ColorGetter colors).
        // Drawing a space here with the row's normal scheme would overwrite that. See issue #5075.
        if (!renderLines && SeparatorSymbol == ' ')
        {
            return;
        }

        Rune symbol = renderLines ? Glyphs.VLine : (Rune)SeparatorSymbol;
        RenderRune (col, row, symbol);
    }

    /// <summary>
    ///     Determines whether headers should be rendered based on current viewport state.
    /// </summary>
    private bool ShouldRenderHeaders () => !TableIsNullOrInvisible ();


    private void AddRuneAt (int col, int row, Rune ch)
    {
        Move (col, row);
        AddRune (ch);
    }

    /// <summary>
    ///     Returns the maximum of the <paramref name="col"/> name and the maximum length of data that will be rendered
    ///     starting at <see cref="RowOffset"/> and rendering <paramref name="rowsToRender"/>
    /// </summary>
    /// <param name="col">ColumnIndex</param>
    /// <param name="colStyle"></param>
    /// <param name="startRow">index of first row</param>
    /// <param name="rowsToRender">Count of rows to inspect</param>
    /// <returns></returns>
    private int CalculateMaxCellWidth (int col, ColumnStyle? colStyle, int startRow, int rowsToRender)
    {
        int spaceRequired = _table!.ColumnNames [col].GetColumns ();

        // if table has no rows
        if (Table is not { Rows: > 0 })
        {
            return spaceRequired;
        }

        for (int i = startRow; i < startRow + rowsToRender; i++)
        {
            // expand required space if cell is bigger than the last biggest cell or header
            spaceRequired = Math.Max (spaceRequired, GetRepresentation (Table [i, col], colStyle).GetColumns ());
        }

        // Don't require more space than the style allows
        if (colStyle is { })
        {
            // enforce maximum cell width based on style
            if (spaceRequired > colStyle.MaxWidth)
            {
                spaceRequired = colStyle.MaxWidth;
            }

            // enforce minimum cell width based on style
            if (spaceRequired < colStyle.MinWidth)
            {
                spaceRequired = colStyle.MinWidth;
            }
        }

        // enforce maximum cell width based on global table style
        if (spaceRequired > MaxCellWidth)
        {
            spaceRequired = MaxCellWidth;
        }

        return spaceRequired;
    }

    /// <summary>
    ///     Returns the value that should be rendered to best represent a strongly typed <paramref name="value"/> read
    ///     from <see cref="Table"/>
    /// </summary>
    /// <param name="value"></param>
    /// <param name="colStyle">Optional style defining how to represent cell values</param>
    /// <returns></returns>
    private string GetRepresentation (object value, ColumnStyle? colStyle)
    {
        if (value == DBNull.Value)
        {
            return NullSymbol;
        }

        return colStyle is { } ? colStyle.GetRepresentation (value) : value.ToString () ?? string.Empty;
    }

    /// <summary>
    ///     Truncates or pads <paramref name="representation"/> so that it occupies exactly
    ///     <paramref name="availableHorizontalSpace"/> using the alignment specified in <paramref name="colStyle"/> (or left
    ///     if no style is defined)
    /// </summary>
    /// <param name="originalCellValue">The object in this cell of the <see cref="Table"/></param>
    /// <param name="representation">The string representation of <paramref name="originalCellValue"/></param>
    /// <param name="availableHorizontalSpace"></param>
    /// <param name="colStyle">Optional style indicating custom alignment for the cell</param>
    /// <returns></returns>
    private static string TruncateOrPad (object originalCellValue, string representation, int availableHorizontalSpace, ColumnStyle? colStyle)
    {
        if (string.IsNullOrEmpty (representation))
        {
            return new string (' ', availableHorizontalSpace);
        }

        // if value is too wide, truncate by grapheme cluster to avoid splitting surrogate pairs
        if (representation.GetColumns () >= availableHorizontalSpace)
        {
            string indicator = colStyle?.TruncationIndicator ?? string.Empty;
            int indicatorWidth = indicator.GetColumns ();

            // Only draw the indicator if it leaves room for at least one cell of content
            // (otherwise fall back to silent clipping)
            bool useIndicator = indicatorWidth > 0 && indicatorWidth < availableHorizontalSpace - 1;
            int reserve = useIndicator ? indicatorWidth : 0;

            StringBuilder sb = new ();
            int remaining = availableHorizontalSpace;

            foreach (string grapheme in GraphemeHelper.GetGraphemes (representation))
            {
                int w = grapheme.GetColumns ();

                if (remaining - w - reserve <= 0)
                {
                    break;
                }

                sb.Append (grapheme);
                remaining -= w;
            }

            if (useIndicator)
            {
                sb.Append (indicator);
            }

            return sb.ToString ();
        }

        // pad it out with spaces to the given alignment
        int toPad = availableHorizontalSpace - (representation.GetColumns () + 1 /*leave 1 space for cell boundary*/);

        return (colStyle?.GetAlignment (originalCellValue) ?? Alignment.Start) switch
        {
            Alignment.Start => representation + new string (' ', toPad),
            Alignment.End => new string (' ', toPad) + representation,

            // TODO: With single line cells, centered and justified are the same right?
            Alignment.Center or Alignment.Fill => new string (' ', (int)Math.Floor (toPad / 2.0))
                                                  + // round down
                                                  representation
                                                  + new string (' ', (int)Math.Ceiling (toPad / 2.0)), // round up
            _ => representation + new string (' ', toPad)
        };
    }

    /// <summary>
    ///     Returns the cells that shall be shown (all cells except the hidden ones)
    /// </summary>
    /// <returns></returns>
    private ColumnToRender [] NonHiddenCellInfos ()
    {
        if (TableIsNullOrInvisible ())
        {
            return [];
        }

        if (_columnsToRenderCache == null)
        {
            RefreshContentSize ();
        }

        return _columnsToRenderCache ?? [];
    }

    /// <summary>Clears a line of the console by filling it with spaces</summary>
    /// <param name="row"></param>
    /// <param name="width"></param>
    private void ClearLine (int row, int width)
    {
        if (App?.Screen.Height == 0)
        {
            return;
        }

        Move (0, row);
        SetAttribute (GetAttributeForRole (VisualRole.Normal));
        AddStr (new string (' ', width));
    }

    /// <summary>Describes a desire to render a column at a given horizontal position in the UI</summary>
    internal class ColumnToRender (int col, int x, int width, bool isVeryLast)
    {
        /// <summary>The column to render</summary>
        public int Column { get; set; } = col;

        /// <summary>True if this column is the very last column in the <see cref="Table"/> (not just the last visible column)</summary>
        public bool IsVeryLast { get; } = isVeryLast;

        /// <summary>
        ///     The width that the column should occupy as calculated by <see cref="TableView.CalculateContentSize"/>.  Note
        ///     that this includes space for padding i.e. the separator between columns.
        /// </summary>
        public int Width { get; internal set; } = width;

        /// <summary>The horizontal position to begin rendering the column at</summary>
        public int X { get; set; } = x;
    }
}
