namespace Terminal.Gui.Views;

public partial class ListView
{
    /// <inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        if (Source is IListViewLayout layout)
        {
            int lengthBefore = MaxItemLength;
            layout.PrepareRender (this, Math.Max (0, Viewport.Width - MarkWidth));

            if (MaxItemLength != lengthBefore)
            {
                UpdateContentSize ();
            }
        }

        Attribute current = Attribute.Default;
        int item = Viewport.Y;

        // Reserve mark glyph + separator column. If a configured mark glyph is wide, it can consume the separator.
        int reservedMarkColumns = ShowMarks ? 2 : 0;
        int headerRows = VisibleHeaderRows;
        int row = 0;
        Move (0, 0);

        if (headerRows > 0 && Viewport.Height > 0)
        {
            current = DrawColumnHeader (current);
            row = headerRows;
        }

        if (Source is null)
        {
            DrawDetachedFooter (current);

            return base.OnDrawingContent (context);
        }

        int itemBottom = PinnedFooterRows > 0
                             ? Math.Max (headerRows, Viewport.Height - PinnedFooterRows)
                             : Viewport.Height;
        bool drewTrailingFooter = false;

        for (; row < itemBottom; item++)
        {
            if (!drewTrailingFooter
                && TrailingFooterRows > 0
                && item >= (Source?.Count ?? 0))
            {
                drewTrailingFooter = true;
                current = DrawColumnFooter (current, row);
                row += FooterRowCount;

                continue;
            }

            int rowStep = RowsForItem (item);
            bool isSelected = item == SelectedItem;
            bool isMarked = item < Source?.Count && Source?.IsMarked (item) is true;

            // Determine visual role based on the 4 combinations of ShowMarks and MarkMultiple
            VisualRole role;
            var applyHighlightStyle = false;

            switch (ShowMarks)
            {
                case false when !MarkMultiple:
                    // Combination 1: Standard selection mode (no marking)
                    // Mark glyphs: None (MarkWidth = 0)
                    // Visual roles: SelectedItem uses Focus (focused) or Active (not focused)
                    role = isSelected ? HasFocus ? VisualRole.Focus : VisualRole.Active : VisualRole.Normal;

                    break;

                case false when MarkMultiple:
                    {
                        switch (isSelected)
                        {
                            // Combination 2: Hidden marks with visual role indicators
                            // Mark glyphs: None (MarkWidth = 0) - marks exist internally
                            // Visual roles use Highlight for marked items; compose TextStyle when marked+selected+focused
                            case true when isMarked:
                                role = HasFocus ? VisualRole.Focus : VisualRole.Highlight;
                                applyHighlightStyle = HasFocus; // Apply Highlight's TextStyle to Focus

                                break;

                            case true: role = HasFocus ? VisualRole.Focus : VisualRole.Normal; break;

                            default:
                                {
                                    role = isMarked ? VisualRole.Highlight : VisualRole.Normal;

                                    break;
                                }
                        }

                        break;
                    }

                case true when !MarkMultiple:
                    // Combination 3: Radio button style
                    // Mark glyphs: Radio-button style (◉ marked, ○ unmarked)
                    // Visual roles: Standard selection (mark glyphs provide visual indication)
                    role = isSelected ? HasFocus ? VisualRole.Focus : VisualRole.Active : VisualRole.Normal;

                    break;

                default:
                    // Combination 4: Checkbox style
                    // Mark glyphs: Checkbox style (☑ marked, ☐ unmarked)
                    // Visual roles: Standard selection (mark glyphs provide visual indication)
                    role = isSelected ? HasFocus ? VisualRole.Focus : VisualRole.Active : VisualRole.Normal;

                    break;
            }

            Attribute newAttribute = GetAttributeForRole (role);

            // Apply Highlight's TextStyle if needed (combination 2 only, when marked+selected+focused)
            if (applyHighlightStyle)
            {
                Attribute highlightAttr = GetAttributeForRole (VisualRole.Highlight);
                newAttribute = newAttribute with { Style = highlightAttr.Style };
            }

            if (newAttribute != current)
            {
                SetAttribute (newAttribute);
                current = newAttribute;
            }

            Move (0, row);

            if (Source is null || item >= Source.Count)
            {
                for (var c = 0; c < Viewport.Width; c++)
                {
                    AddRune ((Rune)' ');
                }

                for (int extra = 1; extra < rowStep && row + extra < Viewport.Height; extra++)
                {
                    Move (0, row + extra);

                    for (int c = 0; c < Viewport.Width; c++)
                    {
                        AddRune ((Rune)' ');
                    }
                }
            }
            else
            {
                var rowEventArgs = new ListViewRowEventArgs (item);
                OnRowRender (rowEventArgs);

                if (rowEventArgs.RowAttribute is { } && current != rowEventArgs.RowAttribute)
                {
                    current = (Attribute)rowEventArgs.RowAttribute;
                    SetAttribute (current);
                }

                var markWidth = 0;

                if (ShowMarks)
                {
                    // Try custom mark rendering first
                    bool customRendered = Source.RenderMark (this, item, row, Source.IsMarked (item), MarkMultiple);

                    if (!customRendered)
                    {
                        // Default rendering: marks with Normal attribute for visual clarity
                        Attribute savedAttr = current;
                        Attribute normalAttr = GetAttributeForRole (VisualRole.Normal);

                        if (current != normalAttr)
                        {
                            SetAttribute (normalAttr);
                            current = normalAttr;
                        }

                        AddRune (Source.IsMarked (item) ? MarkMultiple ? Glyphs.CheckStateChecked : Glyphs.Selected :
                                 MarkMultiple ? Glyphs.CheckStateUnChecked : Glyphs.UnSelected);
                        AddRune ((Rune)' ');
                        markWidth = 2;

                        // Restore attribute for content rendering
                        if (current != savedAttr)
                        {
                            SetAttribute (savedAttr);
                            current = savedAttr;
                        }
                    }
                }

                if (ShowMarks && rowStep > 1)
                {
                    for (int extra = 1; extra < rowStep && row + extra < Viewport.Height; extra++)
                    {
                        Move (0, row + extra);
                        AddRune ((Rune)' ');
                        AddRune ((Rune)' ');
                    }
                }

                int contentCol = reservedMarkColumns > 0 ? reservedMarkColumns : markWidth;
                Source.Render (this, isSelected, item, contentCol, row, Viewport.Width - contentCol, Viewport.X);
            }

            row += rowStep;
        }

        if (PinnedFooterRows > 0)
        {
            int footerRow = Math.Max (headerRows, Viewport.Height - PinnedFooterRows);

            if (footerRow < Viewport.Height)
            {
                DrawColumnFooter (current, footerRow);
            }
        }

        return true;
    }

    private void DrawDetachedFooter (Attribute current)
    {
        if (!ShowColumnFooters || FooterRowCount <= 0)
        {
            return;
        }

        int headerRows = VisibleHeaderRows;
        int footerRow = FooterPlacement == ColumnFooterPlacement.Bottom
                            ? Math.Max (headerRows, Viewport.Height - FooterRowCount)
                            : headerRows;

        if (footerRow >= Viewport.Height)
        {
            return;
        }

        DrawColumnFooter (current, footerRow);
    }

    private Attribute DrawColumnHeader (Attribute current)
    {
        Attribute headerAttribute = GetAttributeForRole (VisualRole.ReadOnly);

        if (headerAttribute != current)
        {
            SetAttribute (headerAttribute);
            current = headerAttribute;
        }

        int band = HeaderRowCount;

        if (ShowMarks)
        {
            for (int r = 0; r < band && r < Viewport.Height; r++)
            {
                Move (0, r);
                AddRune ((Rune)' ');
                AddRune ((Rune)' ');
            }
        }

        int contentCol = MarkWidth;
        int headerWidth = Math.Max (0, Viewport.Width - contentCol);

        if (Source is null)
        {
            WriteDetachedHeader (contentCol, headerWidth, Viewport.X);

            return current;
        }

        Source.Render (this, false, Viewport.Y, contentCol, 0, headerWidth, Viewport.X);

        return current;
    }

    private Attribute DrawColumnFooter (Attribute current, int row)
    {
        Attribute footerAttribute = GetAttributeForRole (VisualRole.ReadOnly);

        if (footerAttribute != current)
        {
            SetAttribute (footerAttribute);
            current = footerAttribute;
        }

        int band = FooterRowCount;

        if (ShowMarks)
        {
            for (int r = 0; r < band && row + r < Viewport.Height; r++)
            {
                Move (0, row + r);
                AddRune ((Rune)' ');
                AddRune ((Rune)' ');
            }
        }

        int contentCol = MarkWidth;
        int footerWidth = Math.Max (0, Viewport.Width - contentCol);

        if (Source is IListViewLayout layout)
        {
            layout.RenderColumnFooter (this, contentCol, row, footerWidth, Viewport.X);

            return current;
        }

        WriteDetachedFooter (contentCol, row, footerWidth, Viewport.X);

        return current;
    }

    /// <summary>
    ///     Footer band used when <see cref="Source"/> is null or does not measure columns.
    /// </summary>
    private void WriteDetachedFooter (int col, int row, int width, int viewportX)
    {
        int band = FooterRowCount;

        for (int r = 0; r < band && row + r < Viewport.Height; r++)
        {
            string text = FormatDetachedFooter (width, r);
            Move (Math.Max (col - viewportX, 0), row + r);
            WriteHeaderText (text, width, viewportX);
        }
    }

    private string FormatDetachedFooter (int width, int row)
    {
        if (ColumnFooters is not { Count: > 0 } footers)
        {
            if (row > 0)
            {
                return string.Empty;
            }

            return AlignHeaderText ("Footer0", Math.Max (width, 0), FootersHorizontalAlignment);
        }

        if (ColumnHeaders is not { Count: > 0 } headers)
        {
            return FormatDetachedFooterCell (footers [0], null, width, row);
        }

        (int [] starts, int [] widths) = LayoutDetachedColumns (width);
        int columnCount = headers.Count;
        int margin = Math.Max (0, ColumnMargin);
        string rowText = string.Empty;

        for (int c = 0; c < columnCount; c++)
        {
            if (c >= starts.Length || starts [c] < 0)
            {
                continue;
            }

            int gap = c < columnCount - 1 ? margin : 0;
            int contentWidth = Math.Max (0, widths [c] - gap);
            ColumnFooter? footer = c < footers.Count ? footers [c] : null;
            rowText += footer is null
                           ? AlignHeaderText (string.Empty, contentWidth, FootersHorizontalAlignment)
                           : FormatDetachedFooterCell (footer, headers [c], contentWidth, row);

            if (gap > 0)
            {
                rowText += new string (' ', gap);
            }
        }

        return rowText;
    }

    private string FormatDetachedFooterCell (ColumnFooter footer, ColumnHeader? header, int width, int row)
    {
        int footerHeight = Math.Max (1, footer.Height);

        if (row < 0 || row >= footerHeight || width <= 0)
        {
            return AlignHeaderText (string.Empty, Math.Max (width, 0), footer.ResolveHorizontalAlignment (header, FootersHorizontalAlignment));
        }

        string formatted = FormatFooterText (footer, header);
        string line = ListWrapper<string>.HeaderTextLine (
                                                           formatted,
                                                           row,
                                                           footerHeight,
                                                           width,
                                                           footer.ResolveVerticalAlignment (header, FootersVerticalAlignment));

        return AlignHeaderText (line, width, footer.ResolveHorizontalAlignment (header, FootersHorizontalAlignment));
    }

    /// <summary>
    ///     Footer text after the resolved format. A format set on the footer wins. Otherwise the header's
    ///     <see cref="ColumnHeader.MemberFormat"/> is used. A value that cannot be converted is left unchanged.
    /// </summary>
    internal static string FormatFooterText (ColumnFooter footer, ColumnHeader? header)
    {
        string text = footer.FooterName ?? string.Empty;
        string? format = footer.ResolveFormat (header);

        if (string.IsNullOrEmpty (format))
        {
            return text;
        }

        try
        {
            Type memberType = header?.MemberType ?? typeof (string);

            if (memberType == typeof (string))
            {
                return string.Format (format, text);
            }

            object converted = Convert.ChangeType (text, memberType);

            return string.Format (format, converted);
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException or ArgumentException or NotSupportedException)
        {
            return text;
        }
    }

    /// <summary>
    ///     Header row used when <see cref="Source"/> is null. Column widths come from the headers, because there are no
    ///     items to measure.
    /// </summary>
    private void WriteDetachedHeader (int col, int width, int viewportX)
    {
        int band = HeaderRowCount;

        for (int r = 0; r < band && r < Viewport.Height; r++)
        {
            string text = FormatDetachedHeader (width, r);
            Move (Math.Max (col - viewportX, 0), r);
            WriteHeaderText (text, width, viewportX);
        }
    }

    /// <summary>Column starts and widths for a header or footer drawn without a source.</summary>
    private (int [] Starts, int [] Widths) LayoutDetachedColumns (int width)
    {
        if (ColumnHeaders is not { Count: > 0 } headers)
        {
            return ([0], [Math.Max (width, 0)]);
        }

        int columnCount = headers.Count;
        int margin = Math.Max (0, ColumnMargin);
        int [] starts = new int [columnCount];
        int [] widths = new int [columnCount];
        int used = 0;

        for (int c = 0; c < columnCount; c++)
        {
            ColumnHeader header = headers [c];

            if (header.Width == -1)
            {
                starts [c] = -1;
                widths [c] = 0;

                continue;
            }

            int contentWidth = TextFormatter.GetWidestLineLength (DetachedHeaderTitle (header, c));
            int columnWidth;
            int remaining = Math.Max (0, width - used);

            if (header.Width == 0)
            {
                if (c == columnCount - 1)
                {
                    columnWidth = Math.Max (contentWidth, remaining);

                    if (ItemHeight > 1 && remaining > 0 && columnWidth > remaining)
                    {
                        columnWidth = remaining;
                    }
                }
                else
                {
                    columnWidth = contentWidth;

                    if (ItemHeight > 1 && columnWidth > 0)
                    {
                        int later = columnCount - c - 1;
                        int reserved = later * (1 + margin);
                        int available = Math.Max (1, remaining - reserved);

                        if (columnWidth > available)
                        {
                            columnWidth = available;
                        }
                    }
                }
            }
            else
            {
                columnWidth = Math.Max (header.Width, 0);
            }

            int gap = c < columnCount - 1 ? margin : 0;
            starts [c] = used;
            widths [c] = columnWidth + gap;
            used += widths [c];
        }

        return (starts, widths);
    }

    private string FormatDetachedHeader (int width, int row)
    {
        if (ColumnHeaders is not { Count: > 0 } headers)
        {
            if (row > 0)
            {
                return string.Empty;
            }

            return AlignHeaderText ("Col0", Math.Max (width, 0), HeadersAlignment);
        }

        int columnCount = headers.Count;
        int margin = Math.Max (0, ColumnMargin);
        (int [] starts, int [] widths) = LayoutDetachedColumns (width);
        string rowText = string.Empty;

        for (int c = 0; c < columnCount; c++)
        {
            if (starts [c] < 0)
            {
                continue;
            }

            int gap = c < columnCount - 1 ? margin : 0;
            int contentWidth = Math.Max (0, widths [c] - gap);
            string line = ListWrapper<string>.HeaderTextLine (
                                                               DetachedHeaderTitle (headers [c], c),
                                                               row,
                                                               Math.Max (1, headers [c].Height),
                                                               contentWidth,
                                                               headers [c].HeaderVerticalAlignment);
            rowText += AlignHeaderText (line, contentWidth, headers [c].HeaderHorizontalAlignment);

            if (gap > 0)
            {
                rowText += new string (' ', gap);
            }
        }

        return rowText;
    }

    private static string DetachedHeaderTitle (ColumnHeader header, int index)
    {
        if (!string.IsNullOrEmpty (header.HeaderName))
        {
            return header.HeaderName;
        }

        if (!string.IsNullOrEmpty (header.MemberName))
        {
            return header.MemberName;
        }

        return $"Col{index}";
    }

    private static string AlignHeaderText (string? text, int width, Alignment alignment)
    {
        if (width <= 0)
        {
            return string.Empty;
        }

        text ??= string.Empty;

        if (alignment == Alignment.Fill)
        {
            string filled = TextFormatter.ClipAndJustify (text, width, true);

            if (filled.GetColumns () > width)
            {
                filled = TextFormatter.ClipAndJustify (filled, width, false);
            }

            int missing = Math.Max (0, width - filled.GetColumns ());

            return filled + new string (' ', missing);
        }

        string clipped = text.GetColumns () > width
                             ? TextFormatter.ClipAndJustify (text, width, false)
                             : text;
        int pad = Math.Max (0, width - clipped.GetColumns ());

        if (alignment == Alignment.End)
        {
            return new string (' ', pad) + clipped;
        }

        if (alignment == Alignment.Center)
        {
            int left = pad / 2;

            return new string (' ', left) + clipped + new string (' ', pad - left);
        }

        return clipped + new string (' ', pad);
    }

    private void WriteHeaderText (string str, int width, int viewportX)
    {
        if (string.IsNullOrEmpty (str) || viewportX >= str.GetColumns ())
        {
            for (int i = 0; i < width; i++)
            {
                AddRune ((Rune)' ');
            }

            return;
        }

        int runeLength = str.ToRunes ().Length;
        int startIndex = Math.Min (viewportX, Math.Max (0, runeLength - 1));
        string substring = str [startIndex..];
        string visible = TextFormatter.ClipAndJustify (substring, width, Alignment.Start);
        AddStr (visible);
        width -= visible.GetColumns ();

        while (width-- > 0)
        {
            AddRune ((Rune)' ');
        }
    }

    // TODO: RowRender should follow CWP.
    // TODO: All this lets you do is customize the Attribute per row. It should be renamed
    // TODO: to RowAttributeRender or something similar.
    // TODO: Consider adding a set of events for more customization; but that's what's IListDataSource.Render is for
    /// <summary>Virtual method that will invoke the <see cref="RowRender"/>.</summary>
    /// <param name="rowEventArgs"></param>
    public virtual void OnRowRender (ListViewRowEventArgs rowEventArgs) => RowRender?.Invoke (this, rowEventArgs);

    /// <summary>This event is invoked when this <see cref="ListView"/> is being drawn before rendering.</summary>
    public event EventHandler<ListViewRowEventArgs>? RowRender;
}
