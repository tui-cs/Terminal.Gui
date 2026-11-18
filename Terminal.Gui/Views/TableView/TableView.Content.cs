namespace Terminal.Gui.Views;

public partial class TableView
{
    /// <summary>
    ///     Gets or sets whether all rows should be used when calculating content size. When <see langword="false"/>,
    ///     only visible rows are used for column width calculations.
    /// </summary>
    public bool UseAllRowsForContentCalculation
    {
        get;
        set
        {
            field = value;
            RefreshContentSize ();
        }
    }

    private ColumnToRender []? _columnsToRenderCache;

    /// <summary>
    ///     Horizontal scroll offset.  The index of the first column in <see cref="Table"/> to display when rendering
    ///     the view.
    /// </summary>
    /// <remarks>This property allows very wide tables to be rendered with horizontal scrolling</remarks>
    public int ColumnOffset
    {
        get => _columnsToRenderCache?.Count (c => c.X + c.Width <= Viewport.X) ?? 0;
        set
        {
            if (value < 0)
            {
                value = 0;
            }

            if (_columnsToRenderCache == null)
            {
                CalculateContentSize ();
            }

            int cacheLength = _columnsToRenderCache?.Length ?? 0;

            if (cacheLength == 0)
            {
                // No visible columns — early return leaves Viewport.X unchanged
                return;
            }

            if (value >= cacheLength)
            {
                value = cacheLength - 1;
            }

            int prev = ColumnOffset;
            Viewport = Viewport with { X = _columnsToRenderCache! [value].X };

            if (prev != ColumnOffset)
            {
                SetNeedsDraw ();
            }
        }
    }

    /// <summary>
    ///     Vertical scroll offset.  The index of the first row in <see cref="Table"/> to display in the first non header
    ///     line of the control when rendering the view.
    /// </summary>
    public int RowOffset
    {
        get => Style.AlwaysShowHeaders ? Viewport.Y : Math.Max (Viewport.Y - GetHeaderHeightIfAny (), 0);
        set
        {
            int oldViewportY = Viewport.Y;

            Viewport = Viewport with { Y = value == 0 ? 0 : Style.AlwaysShowHeaders ? value : GetHeaderHeightIfAny () + value };

            if (Viewport.Y != oldViewportY)
            {
                SetNeedsDraw ();
            }
        }
    }

    /// <summary>
    ///     Recalculates and updates the content size based on the current state.
    /// </summary>
    /// <remarks>
    ///     Call this method after making changes that affect the content's dimensions to ensure the
    ///     layout remains accurate.
    ///     Also call this if data in Table has changed.
    /// </remarks>
    public void RefreshContentSize () => SetContentSize (CalculateContentSize ());

    private bool _inCalculatingContentSize;

    /// <inheritdoc/>
    protected override void OnViewportChanged (DrawEventArgs e)
    {
        base.OnViewportChanged (e);

        if (_inCalculatingContentSize)
        {
            return;
        }

        if (e.OldViewport.Size != e.NewViewport.Size || (!UseAllRowsForContentCalculation && e.OldViewport.Y != e.NewViewport.Y))
        {
            RefreshContentSize ();
        }
    }

    /// <summary>
    ///     Gets the maximum top-left coordinates to which the viewport can be scrolled within the content area.
    /// </summary>
    /// <remarks>
    ///     The returned point represents the largest X and Y values for the viewport's position such
    ///     that the entire viewport remains within the bounds of the content.
    /// </remarks>
    public Point MaxViewPort ()
    {
        Size contentSize = GetContentSize ();
        int maxX = Math.Max (contentSize.Width - Viewport.Width, 0);
        int maxY = Math.Max (contentSize.Height - Viewport.Height, 0);

        return new Point (maxX, maxY);
    }

    /// <summary>
    ///     Updates <see cref="ColumnOffset"/> and <see cref="RowOffset"/> where they are outside the bounds of the table
    ///     (by adjusting them to the nearest existing cell).  Has no effect if <see cref="Table"/> has not been set.
    /// </summary>
    /// <remarks>
    ///     Changes will not be immediately visible in the display until you call <see cref="View.SetNeedsDraw()"/>
    /// </remarks>
    public void EnsureValidScrollOffsets ()
    {
        if (TableIsNullOrInvisible ())
        {
            return;
        }

        Point maxViewPort = MaxViewPort ();

        if (Viewport.Y > maxViewPort.Y)
        {
            Viewport = Viewport with { Y = Math.Max (maxViewPort.Y, 0) };
        }

        if (Viewport.X > maxViewPort.X)
        {
            Viewport = Viewport with { X = Math.Max (maxViewPort.X, 0) };
        }
    }

    private Size? CalculateContentSize ()
    {
        var contentSize = new Size (0, 0);
        _inCalculatingContentSize = true;

        try
        {
            int headerHeight = GetHeaderHeightIfAny ();
            int headerHeightVisible = CurrentHeaderHeightVisible ();
            int leftOuterBorderWidth = ShouldRenderFirstOuterVerticalLine () ? 1 : 0;
            int rightOuterBorderWidth = ShouldRenderLastOuterVerticalLine () ? 1 : 0;
            contentSize.Height += headerHeight + Table?.Rows ?? 0;

            if (Style.ShowHorizontalBottomLine)
            {
                contentSize.Height++;
            }

            // we assume that padding is 0 here
            var padding = 0;
            List<ColumnToRender> columnsToRender = new ();

            if (Table != null)
            {
                List<(int colIdx, ColumnStyle? colStyle)> nonHiddenColumns = Enumerable.Range (0, Table.Columns)
                                                                                       .Select (c => (colIdx: c, colStyle: Style.GetColumnStyleIfAny (c)))
                                                                                       .Where (e => e.colStyle?.Visible != false)
                                                                                       .ToList ();

                int lastColIdx = nonHiddenColumns.Any () ? nonHiddenColumns.Last ().colIdx : -1;

                contentSize.Width += leftOuterBorderWidth;

                var startRow = 0;
                int rowsToRender = Table.Rows;

                if (!UseAllRowsForContentCalculation)
                {
                    startRow = Style.AlwaysShowHeaders ? Viewport.Y : Math.Max (Viewport.Y - headerHeight, 0);

                    rowsToRender = Math.Min (Viewport.Height - headerHeightVisible, Table.Rows - startRow);
                }

                // Calculate the content size based on the table's data
                foreach ((int colIdx, ColumnStyle? colStyle) in nonHiddenColumns)
                {
                    int maxContentSize = CalculateMaxCellWidth (colIdx, colStyle, startRow, rowsToRender) + padding;
                    int colWidth = maxContentSize + padding;

                    if (MinCellWidth > 0 && colWidth < MinCellWidth + padding)
                    {
                        if (MinCellWidth > MaxCellWidth)
                        {
                            colWidth = MaxCellWidth + padding;
                        }
                        else
                        {
                            colWidth = MinCellWidth + padding;
                        }
                    }

                    bool isVeryLast = colIdx == lastColIdx;

                    if (isVeryLast)
                    {
                        // Remaining space for the last column is only used when the table would otherwise be smaller
                        // than the viewport; otherwise content size should remain based on the actual column widths.
                        int remainingSpace = Viewport.Width - contentSize.Width - rightOuterBorderWidth;

                        if (Style.ExpandLastColumn && colWidth < remainingSpace)
                        {
                            colWidth = remainingSpace;
                        }
                    }

                    columnsToRender.Add (new ColumnToRender (colIdx, contentSize.Width, colWidth + 1, lastColIdx == colIdx));

                    contentSize.Width += colWidth;

                    if (!isVeryLast)
                    {
                        // for separator symbols between columns
                        contentSize.Width += 1;
                    }
                }

                contentSize.Width += rightOuterBorderWidth;
            }
            else
            {
                contentSize.Width = 0;
            }

            _columnsToRenderCache = columnsToRender.ToArray ();

            //check if it makes sense to scroll to left or up if the scrolled viewport is bigger than needed
            if (Viewport.X + Viewport.Width > contentSize.Width)
            {
                Viewport = Viewport with { X = Math.Max (contentSize.Width - Viewport.Width, 0) };
            }

            if (Viewport.Y + Viewport.Height > contentSize.Height)
            {
                Viewport = Viewport with { Y = Math.Max (contentSize.Height - Viewport.Height, 0) };
            }
        }
        finally
        {
            _inCalculatingContentSize = false;
        }

        return contentSize;
    }

    /// <summary>
    ///     Returns the minimum render width to reserve for a column that has not yet been laid out, based on its header
    ///     width and any configured minimum (clamped to <see cref="MaxCellWidth"/> and
    ///     <see cref="ColumnStyle.MaxWidth"/>). This intentionally does not inspect cell data — it is O(1) per column to
    ///     keep large or paginated <see cref="ITableSource"/> implementations performant.
    /// </summary>
    private int MinimumWidthFor (int colIdx, ColumnStyle? colStyle)
    {
        int min = _table!.ColumnNames [colIdx].GetColumns ();

        if (min < 1)
        {
            min = 1;
        }

        if (colStyle is { MinWidth: > 0 } && colStyle.MinWidth > min)
        {
            min = colStyle.MinWidth;
        }

        if (MinCellWidth > 0 && MinCellWidth > min)
        {
            min = MinCellWidth;
        }

        // Don't reserve more than the column's own ceiling
        int ceiling = MaxCellWidth;

        if (colStyle is { } && colStyle.MaxWidth < ceiling)
        {
            ceiling = colStyle.MaxWidth;
        }

        if (ceiling < 1)
        {
            ceiling = 1;
        }

        if (min > ceiling)
        {
            min = ceiling;
        }

        return min;
    }

    private bool ShouldRenderFirstOuterVerticalLine ()
    {
        if (!Style.ShowVerticalCellLineForFirstColumn)
        {
            return false;
        }

        return Style.ShowVerticalHeaderLines || Style.ShowVerticalCellLines;
    }

    private bool ShouldRenderLastOuterVerticalLine ()
    {
        if (!Style.ShowVerticalCellLineForLastColumn)
        {
            return false;
        }

        return Style.ShowVerticalHeaderLines || Style.ShowVerticalCellLines;
    }
}
