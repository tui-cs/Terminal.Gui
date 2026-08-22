namespace Terminal.Gui.Views;

/// <summary>
///     A read-only view that renders a single Markdown table with box-drawing borders via
///     <see cref="LineCanvas"/> and styled header/body text with inline Markdown formatting.
/// </summary>
/// <remarks>
///     <para>
///         When used inside a <see cref="Markdown"/>, instances are created automatically during
///         layout and positioned as SubViews at the correct content coordinate so that they scroll
///         naturally with the parent's viewport.
///     </para>
///     <para>
///         Borders are rendered using <see cref="LineStyle.Single"/>.
///     </para>
///     <para>
///         Header cells are bold; body cells support inline Markdown formatting (bold, italic, code,
///         links) via <see cref="MarkdownInlineParser"/>. Column alignment (left, center, right)
///         parsed from the Markdown separator row is respected. Cells that exceed their column width
///         are word-wrapped, and row heights expand to accommodate wrapped content.
///     </para>
///     <para>
///         This view can also be used standalone. Use the parameterless constructor and set
///         <see cref="TableData"/> to provide table content.
///     </para>
/// </remarks>
public sealed class MarkdownTable : View, IDesignable
{
    private TableData _data = _emptyData;
    private int [] _columnWidths = [];

    // Pre-parsed inline segments for each cell
    private List<StyledSegment> [] _headerSegments = [];
    private List<StyledSegment> [] [] _rowSegments = [];

    // Pre-computed wrapped line counts per row
    private int _headerRowHeight;
    private int [] _bodyRowHeights = [];

    // Last width used for column computation — tracks when recalculation is needed
    private int _lastComputedWidth;

    // Link regions within the table, built after layout
    private readonly List<TableLinkRegion> _linkRegions = [];
    private int _activeLinkIndex = -1;

    private static readonly TableData _emptyData = new ([], [], []);

    /// <summary>Initializes a new empty <see cref="MarkdownTable"/>.</summary>
    public MarkdownTable ()
    {
        CanFocus = false;
        TabStop = TabBehavior.NoStop;

        // No adornments — we draw everything ourselves
        BorderStyle = LineStyle.None;
        Border.Thickness = new Thickness (0);
        Padding.Thickness = new Thickness (0);
        Margin.Thickness = new Thickness (0);

        MouseBindings.ReplaceCommands (MouseFlags.LeftButtonClicked, Command.Activate);
        AddCommand (Command.Accept, () => ActivateCurrentLink ());
    }

    /// <summary>
    ///     Raised when a hyperlink inside a table cell is clicked.
    ///     Set <see cref="MarkdownLinkEventArgs.Handled"/> to prevent default navigation.
    /// </summary>
    public event EventHandler<MarkdownLinkEventArgs>? LinkClicked;

    /// <summary>
    ///     Gets or sets an optional syntax highlighter used to resolve theme-based attributes for
    ///     inline styling roles (emphasis, code spans, links, etc.). When set, the table queries
    ///     the highlighter for scope-derived colors before falling back to <see cref="TextStyle"/>-based defaults.
    /// </summary>
    public ISyntaxHighlighter? SyntaxHighlighter { get; set; }

    /// <summary>
    ///     Gets or sets whether the table uses the syntax highlighting theme's background color
    ///     for cell and border fills. Defaults to <see langword="false"/>.
    /// </summary>
    public bool UseThemeBackground { get; set; }

    /// <summary>
    ///     Gets or sets the table content as a pipe-delimited Markdown table string.
    ///     The setter parses the text via <see cref="TableData.TryParse"/> and updates <see cref="TableData"/>.
    ///     Invalid or empty text clears the table.
    /// </summary>
    protected override void OnTextChanged ()
    {
        string value = Text;

        if (string.IsNullOrWhiteSpace (value))
        {
            TableData = _emptyData;
        }
        else
        {
            string [] lines = value.Split ('\n', StringSplitOptions.RemoveEmptyEntries);
            TableData? parsed = TableData.TryParse (lines);
            TableData = parsed ?? _emptyData;
        }

        base.OnTextChanged ();
    }

    /// <inheritdoc/>
    protected override void UpdateTextFormatterText ()
    {
        TextFormatter.Text = string.Empty;
    }

    private string BuildTableText ()
    {
        if (_data.ColumnCount == 0)
        {
            return string.Empty;
        }

        // Reconstruct pipe-delimited table text
        List<string> lines = [$"| {string.Join (" | ", _data.Headers)} |"];

        // Separator row
        string [] seps = new string [_data.ColumnCount];

        for (var i = 0; i < _data.ColumnCount; i++)
        {
            seps [i] = _data.ColumnAlignments [i] switch
                       {
                           Alignment.Center => ":---:",
                           Alignment.End => "---:",
                           _ => "---"
                       };
        }

        lines.Add ($"| {string.Join (" | ", seps)} |");

        foreach (string [] row in _data.Rows)
        {
            lines.Add ($"| {string.Join (" | ", row)} |");
        }

        return string.Join ("\n", lines);
    }

    /// <summary>
    ///     Gets or sets the <see cref="Views.TableData"/> that defines the table content. Setting this
    ///     recomputes column widths, row heights, and redraws the table.
    /// </summary>
    public TableData TableData
    {
        get => _data;
        set
        {
            _data = value;
            _headerSegments = ParseCellSegments (value.Headers, MarkdownStyleRole.Heading);
            _rowSegments = new List<StyledSegment> [value.Rows.Length] [];

            for (var r = 0; r < value.Rows.Length; r++)
            {
                _rowSegments [r] = ParseCellSegments (value.Rows [r], MarkdownStyleRole.Normal);
            }

            // Keep Text in sync with TableData
            SetTextDirect (BuildTableText ());

            // Compute initial layout using current Frame width (or a default for standalone use)
            int initialWidth = Frame.Width > 0 ? Frame.Width : 80;
            _lastComputedWidth = -1;
            Recalculate (initialWidth);
            SetNeedsLayout ();
            SetNeedsDraw ();
        }
    }

    /// <summary>Recalculates column widths and row heights for the given available width.</summary>
    internal void Recalculate (int maxWidth)
    {
        if (maxWidth == _lastComputedWidth)
        {
            return;
        }

        _lastComputedWidth = maxWidth;
        _columnWidths = ComputeColumnWidths (_data, maxWidth);

        _headerRowHeight = ComputeRowHeight (_headerSegments, _columnWidths);

        if (_bodyRowHeights.Length != _rowSegments.Length)
        {
            _bodyRowHeights = new int [_rowSegments.Length];
        }

        for (var r = 0; r < _rowSegments.Length; r++)
        {
            _bodyRowHeights [r] = ComputeRowHeight (_rowSegments [r], _columnWidths);
        }

        RenderedHeight = CalculateTableHeightWrapped (_headerRowHeight, _bodyRowHeights);
        Height = RenderedHeight;

        BuildLinkRegions ();
    }

    /// <summary>
    ///     Gets the computed rendered height (in lines) after <see cref="Recalculate"/> runs.
    ///     This value is always current, unlike <see cref="View.Frame"/> which updates only after layout.
    /// </summary>
    internal int RenderedHeight { get; private set; }

    /// <summary>Gets whether this table contains any navigable link regions.</summary>
    internal bool HasLinks => _linkRegions.Count > 0;

    /// <summary>Gets the total rendered height of this table in lines (simple estimate).</summary>
    /// <remarks>
    ///     This simple estimation assumes single-line rows. Used by external callers that don't have
    ///     wrapped row heights. For the actual rendered height, use the instance's <see cref="View.Height"/>.
    /// </remarks>
    public static int CalculateTableHeight (TableData data) =>

        // top border + header + header separator + body rows + bottom border
        data.Rows.Length + 4;

    /// <inheritdoc/>
    protected override void OnSubViewLayout (LayoutEventArgs args)
    {
        base.OnSubViewLayout (args);

        Recalculate (Frame.Width);
    }

    /// <inheritdoc/>
    protected override void OnActivated (ICommandContext? ctx)
    {
        if (ctx?.Binding is not MouseBinding { MouseEvent: { } mouse, MouseEvent.Position: { } pos })
        {
            return;
        }

        if (!mouse.Flags.FastHasFlags (MouseFlags.LeftButtonClicked))
        {
            return;
        }

        if (!HasFocus && CanFocus)
        {
            SetFocus ();
        }

        int linkIndex = HitTestLinkIndex (pos.X, pos.Y);

        if (linkIndex < 0)
        {
            return;
        }

        // Set active link to the clicked link region
        _activeLinkIndex = linkIndex;
        SetNeedsDraw ();

        RaiseLinkClicked (_linkRegions [linkIndex].Url);
    }

    /// <inheritdoc/>
    /// <remarks>
    ///     Cycles through link regions on Tab / Shift+Tab. Returns <see langword="false"/>
    ///     when there are no more links in that direction, allowing focus to leave the view.
    /// </remarks>
    protected override bool OnAdvancingFocus (NavigationDirection direction, TabBehavior? behavior)
    {
        if (behavior is { } && behavior != TabStop)
        {
            return false;
        }

        if (_linkRegions.Count == 0)
        {
            return false;
        }

        int delta = direction == NavigationDirection.Forward ? 1 : -1;

        if (_activeLinkIndex < 0)
        {
            // First entry — select first or last link
            _activeLinkIndex = delta > 0 ? 0 : _linkRegions.Count - 1;
            SetNeedsDraw ();

            return true;
        }

        int next = _activeLinkIndex + delta;

        // If we've gone past either end, clear selection and let focus leave
        if (next < 0 || next >= _linkRegions.Count)
        {
            _activeLinkIndex = -1;
            SetNeedsDraw ();

            return false;
        }

        _activeLinkIndex = next;
        SetNeedsDraw ();

        return true;
    }

    /// <inheritdoc/>
    protected override void OnHasFocusChanged (bool newHasFocus, View? previousFocusedView, View? focusedView)
    {
        if (!newHasFocus)
        {
            _activeLinkIndex = -1;
            SetNeedsDraw ();
        }

        base.OnHasFocusChanged (newHasFocus, previousFocusedView, focusedView);
    }

    /// <summary>Activates the currently highlighted link (Enter key).</summary>
    private bool ActivateCurrentLink ()
    {
        if (_activeLinkIndex < 0 || _activeLinkIndex >= _linkRegions.Count)
        {
            return false;
        }

        RaiseLinkClicked (_linkRegions [_activeLinkIndex].Url);

        return true;
    }

    /// <summary>Raises <see cref="LinkClicked"/> and opens the URL if not handled.</summary>
    private void RaiseLinkClicked (string url)
    {
        MarkdownLinkEventArgs args = new (url);
        LinkClicked?.Invoke (this, args);

        if (!args.Handled && !url.StartsWith ('#'))
        {
            Link.OpenUrl (url);
        }
    }

    /// <summary>
    ///     Returns the link region index at the given viewport position, or -1 if none.
    /// </summary>
    private int HitTestLinkIndex (int viewportX, int viewportY)
    {
        (string url, int rowIndex, int colIndex)? hit = HitTestLinkRegion (viewportX, viewportY);

        if (hit is null)
        {
            return -1;
        }

        for (var i = 0; i < _linkRegions.Count; i++)
        {
            TableLinkRegion region = _linkRegions [i];

            if (region.RowIndex == hit.Value.rowIndex && region.ColIndex == hit.Value.colIndex && region.Url == hit.Value.url)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    ///     Returns the URL and cell position (row/col) of the link at the given viewport position,
    ///     or <see langword="null"/> if no link segment occupies that position.
    /// </summary>
    private (string url, int rowIndex, int colIndex)? HitTestLinkRegion (int viewportX, int viewportY)
    {
        if (_columnWidths.Length == 0)
        {
            return null;
        }

        // Determine which row the click is in.
        // Layout: row 0 = top border, rows 1..headerRowHeight = header,
        //         headerRowHeight+1 = separator, then body rows, last row = bottom border.
        int y = viewportY;
        y--; // skip top border

        if (y < 0)
        {
            return null;
        }

        List<StyledSegment> []? cellSegments = null;
        int rowIndex = -1; // -1 = header

        if (y < _headerRowHeight)
        {
            // Click in the header row
            cellSegments = _headerSegments;
            rowIndex = -1;
        }
        else
        {
            y -= _headerRowHeight;
            y--; // skip separator line

            if (y < 0)
            {
                return null;
            }

            // Walk body rows
            for (var r = 0; r < _bodyRowHeights.Length; r++)
            {
                if (y < _bodyRowHeights [r])
                {
                    cellSegments = _rowSegments [r];
                    rowIndex = r;

                    break;
                }

                y -= _bodyRowHeights [r];
            }
        }

        if (cellSegments is null)
        {
            return null;
        }

        // Determine which column the click is in.
        int x = viewportX;
        x--; // skip left border
        int col = -1;

        for (var c = 0; c < _columnWidths.Length; c++)
        {
            if (x < _columnWidths [c])
            {
                col = c;

                break;
            }

            x -= _columnWidths [c] + 1; // column width + separator
        }

        if (col < 0 || col >= cellSegments.Length)
        {
            return null;
        }

        // Word-wrap the cell and find the segment at the click position within the line.
        int innerWidth = _columnWidths [col] - 2;
        List<List<StyledSegment>> wrappedLines = WrapSegments (cellSegments [col], innerWidth);

        // 'y' is now the line-in-row offset (already computed above for multi-line rows)
        if (y < 0 || y >= wrappedLines.Count)
        {
            return null;
        }

        List<StyledSegment> lineSegs = wrappedLines [y];

        // Compute text width for alignment padding
        var textWidth = 0;

        foreach (StyledSegment seg in lineSegs)
        {
            textWidth += seg.Text.GetColumns ();
        }

        Alignment alignment = col < _data.ColumnAlignments.Length ? _data.ColumnAlignments [col] : Alignment.Start;
        int padLeft = CalculateLeftPadding (_columnWidths [col], Math.Min (textWidth, innerWidth), alignment);

        // x is relative to column start; subtract padding to get text-relative position.
        int textX = x - padLeft;

        if (textX < 0)
        {
            return null;
        }

        var runX = 0;

        foreach (StyledSegment seg in lineSegs)
        {
            int segWidth = seg.Text.GetColumns ();

            if (textX >= runX && textX < runX + segWidth && !string.IsNullOrWhiteSpace (seg.Url))
            {
                return (seg.Url, rowIndex, col);
            }

            runX += segWidth;
        }

        return null;
    }

    /// <inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        // Fill the entire viewport with theme background before drawing borders/cells.
        // DrawWrappedRow only fills column widths, so trailing space (right of last column)
        // on cell content rows would otherwise retain the ClearViewport bg (from the scheme).
        if (UseThemeBackground && SyntaxHighlighter?.DefaultBackground is { } fillBg)
        {
            Attribute fillAttr = GetAttributeForRole (VisualRole.Normal) with { Background = fillBg };
            SetAttribute (fillAttr);
            FillRect (Viewport with { X = 0, Y = 0 }, (Rune)' ');
        }

        DrawBorders ();
        DrawCellContents ();

        return true;
    }

    private void DrawCellContents ()
    {
        var y = 1; // Below top border

        // Header row
        DrawWrappedRow (_headerSegments, _data.ColumnAlignments, y, _headerRowHeight, true, -1);
        y += _headerRowHeight;

        // Skip header separator (1 line)
        y++;

        // Body rows
        for (var r = 0; r < _rowSegments.Length; r++)
        {
            DrawWrappedRow (_rowSegments [r], _data.ColumnAlignments, y, _bodyRowHeights [r], false, r);
            y += _bodyRowHeights [r];
        }
    }

    private void DrawWrappedRow (List<StyledSegment> [] cellSegments, Alignment [] alignments, int startY, int rowHeight, bool isHeader, int rowIndex)
    {
        Attribute normal = GetAttributeForRole (VisualRole.Normal);
        Color? themeBg = UseThemeBackground ? SyntaxHighlighter?.DefaultBackground : null;

        if (themeBg is { } bg)
        {
            normal = normal with { Background = bg };
        }

        for (var lineInRow = 0; lineInRow < rowHeight; lineInRow++)
        {
            int y = startY + lineInRow;
            var x = 1; // After left border

            for (var col = 0; col < _columnWidths.Length; col++)
            {
                int colWidth = _columnWidths [col];
                int innerWidth = colWidth - 2; // 1 space padding each side

                List<StyledSegment> segments = col < cellSegments.Length ? cellSegments [col] : [];

                // Word-wrap segments into lines for this cell
                List<List<StyledSegment>> wrappedLines = WrapSegments (segments, innerWidth);

                // Fill the cell area with spaces first
                SetAttribute (normal);

                for (var i = 0; i < colWidth; i++)
                {
                    AddStr (x + i, y, " ");
                }

                // Draw this line's content (if we have it)
                if (lineInRow >= wrappedLines.Count)
                {
                    // Advance past column width + separator character
                    x += colWidth + 1;

                    continue;
                }

                List<StyledSegment> lineSegs = wrappedLines [lineInRow];

                // Calculate text width for alignment
                var textWidth = 0;

                foreach (StyledSegment seg in lineSegs)
                {
                    textWidth += seg.Text.GetColumns ();
                }

                Alignment alignment = col < alignments.Length ? alignments [col] : Alignment.Start;
                int padLeft = CalculateLeftPadding (colWidth, Math.Min (textWidth, innerWidth), alignment);

                int drawX = x + padLeft;

                foreach (StyledSegment seg in lineSegs)
                {
                    Attribute attr = MarkdownAttributeHelper.GetAttributeForSegment (this, seg, SyntaxHighlighter, themeBg);

                    if (isHeader)
                    {
                        attr = attr with { Style = attr.Style | TextStyle.Bold };
                    }

                    bool hasUrl = !string.IsNullOrWhiteSpace (seg.Url);
                    bool isAbsoluteUrl = hasUrl && MarkdownAttributeHelper.TryCreateSafeAbsoluteUri (seg.Url, out _);
                    bool isActive = hasUrl && HasFocus && IsActiveLinkAt (rowIndex, col, seg.Url!);

                    if (isActive)
                    {
                        attr = new Attribute (attr.Background, attr.Foreground, attr.Style);
                    }

                    // Set OSC8 URL for link segments (only absolute URIs)
                    if (isAbsoluteUrl && Driver is { })
                    {
                        Driver.CurrentUrl = seg.Url;
                    }

                    try
                    {
                        SetAttribute (attr);

                        foreach (string grapheme in GraphemeHelper.GetGraphemes (seg.Text))
                        {
                            int gw = Math.Max (grapheme.GetColumns (), 1);

                            if (drawX - x >= colWidth - 1)
                            {
                                break;
                            }

                            AddStr (drawX, y, grapheme);
                            drawX += gw;
                        }
                    }
                    finally
                    {
                        if (isAbsoluteUrl && Driver is { })
                        {
                            Driver.CurrentUrl = null;
                        }
                    }
                }

                // Advance past column width + separator character
                x += colWidth + 1;
            }
        }
    }

    /// <summary>
    ///     Adds border lines to <see cref="View.LineCanvas"/> using screen coordinates so that the
    ///     parent view can merge and render them via <see cref="View.SuperViewRendersLineCanvas"/>.
    /// </summary>
    private void DrawBorders ()
    {
        int tableWidth = Frame.Width;
        int tableHeight = CalculateTableHeightWrapped (_headerRowHeight, _bodyRowHeights);

        Point screenOrigin = ViewportToScreen (Viewport).Location;
        Attribute borderAttr = GetAttributeForRole (VisualRole.Normal);

        if (UseThemeBackground && SyntaxHighlighter?.DefaultBackground is { } themeBg)
        {
            borderAttr = borderAttr with { Background = themeBg };
        }

        // Top border (row 0)
        LineCanvas.AddLine (screenOrigin, tableWidth, Orientation.Horizontal, LineStyle.Single, borderAttr);

        // Header separator (below header rows)
        int headerSepY = screenOrigin.Y + 1 + _headerRowHeight;
        LineCanvas.AddLine (screenOrigin with { Y = headerSepY }, tableWidth, Orientation.Horizontal, LineStyle.Single, borderAttr);

        // Bottom border (last row)
        LineCanvas.AddLine (screenOrigin with { Y = screenOrigin.Y + tableHeight - 1 }, tableWidth, Orientation.Horizontal, LineStyle.Single, borderAttr);

        // Left border (full height)
        LineCanvas.AddLine (screenOrigin, tableHeight, Orientation.Vertical, LineStyle.Single, borderAttr);

        // Right border (full height)
        LineCanvas.AddLine (screenOrigin with { X = screenOrigin.X + tableWidth - 1 }, tableHeight, Orientation.Vertical, LineStyle.Single, borderAttr);

        // Column separators (vertical lines between columns)
        var xOffset = 0;

        for (var col = 0; col < _columnWidths.Length; col++)
        {
            xOffset += _columnWidths [col] + 1; // column width + border char

            if (col < _columnWidths.Length - 1)
            {
                LineCanvas.AddLine (screenOrigin with { X = screenOrigin.X + xOffset }, tableHeight, Orientation.Vertical, LineStyle.Single, borderAttr);
            }
        }
    }

    /// <summary>
    ///     Word-wraps a list of styled segments into multiple lines, each fitting within
    ///     <paramref name="maxWidth"/> display columns.
    /// </summary>
    internal static List<List<StyledSegment>> WrapSegments (List<StyledSegment> segments, int maxWidth)
    {
        if (maxWidth <= 0)
        {
            return [ []];
        }

        List<List<StyledSegment>> lines = [];
        List<StyledSegment> currentLine = [];
        var currentWidth = 0;

        foreach (StyledSegment segment in segments)
        {
            string remaining = segment.Text;

            while (remaining.Length > 0)
            {
                int spaceIdx = remaining.IndexOf (' ');
                string word;
                string trailing;

                if (spaceIdx >= 0)
                {
                    word = remaining [..spaceIdx];
                    trailing = " ";
                    remaining = remaining [(spaceIdx + 1)..];
                }
                else
                {
                    word = remaining;
                    trailing = "";
                    remaining = "";
                }

                string chunk = word + trailing;
                int chunkWidth = chunk.GetColumns ();

                // If adding this word would exceed the line, wrap
                if (currentWidth > 0 && currentWidth + chunkWidth > maxWidth)
                {
                    lines.Add (currentLine);
                    currentLine = [];
                    currentWidth = 0;
                }

                // If a single word is wider than maxWidth, hard-break it
                if (chunkWidth > maxWidth && currentWidth == 0)
                {
                    string hardChunk = TruncateToWidth (chunk, maxWidth);

                    // If maxWidth is too narrow for even one grapheme, take the first grapheme
                    // to guarantee forward progress and avoid an infinite loop.
                    if (hardChunk.Length == 0)
                    {
                        string firstGrapheme = GraphemeHelper.GetGraphemes (chunk).FirstOrDefault () ?? chunk [..1];
                        hardChunk = firstGrapheme;
                    }

                    currentLine.Add (new StyledSegment (hardChunk, segment.StyleRole, segment.Url, segment.ImageSource, segment.Attribute, segment.Role));
                    lines.Add (currentLine);
                    currentLine = [];
                    currentWidth = 0;

                    int usedChars = hardChunk.TrimEnd ().Length;
                    remaining = chunk [usedChars..].TrimStart () + remaining;

                    continue;
                }

                currentLine.Add (new StyledSegment (chunk, segment.StyleRole, segment.Url, segment.ImageSource, segment.Attribute, segment.Role));
                currentWidth += chunkWidth;
            }
        }

        if (currentLine.Count > 0)
        {
            lines.Add (currentLine);
        }

        if (lines.Count == 0)
        {
            lines.Add ([]);
        }

        return lines;
    }

    private static List<StyledSegment> [] ParseCellSegments (string [] cells, MarkdownStyleRole defaultRole)
    {
        List<StyledSegment> [] result = new List<StyledSegment> [cells.Length];

        for (var i = 0; i < cells.Length; i++)
        {
            List<InlineRun> runs = MarkdownInlineParser.ParseInlines (cells [i], defaultRole);
            result [i] = MarkdownAttributeHelper.ToStyledSegments (runs);
        }

        return result;
    }

    /// <summary>Computes the row height needed for a row given word wrapping of cell contents.</summary>
    private static int ComputeRowHeight (List<StyledSegment> [] cellSegments, int [] columnWidths)
    {
        var maxLines = 1;

        for (var col = 0; col < columnWidths.Length && col < cellSegments.Length; col++)
        {
            int innerWidth = columnWidths [col] - 2;
            List<List<StyledSegment>> wrapped = WrapSegments (cellSegments [col], innerWidth);
            maxLines = Math.Max (maxLines, wrapped.Count);
        }

        return maxLines;
    }

    /// <summary>Gets the total rendered height given pre-computed row heights.</summary>
    private static int CalculateTableHeightWrapped (int headerHeight, int [] bodyRowHeights)
    {
        // top border (1) + header rows + header separator (1) + body rows + bottom border (1)
        int total = 3 + headerHeight;

        foreach (int h in bodyRowHeights)
        {
            total += h;
        }

        return total;
    }

    /// <summary>
    ///     Computes column widths using a Rich-style collapse algorithm:
    ///     <list type="number">
    ///         <item>Measure each column's min (longest word + padding) and max (full content + padding) widths.</item>
    ///         <item>If total max fits within <paramref name="maxWidth"/>, use max widths.</item>
    ///         <item>
    ///             Otherwise, iteratively collapse the widest column toward the second-widest.
    ///             Left columns win ties (preserving their width).
    ///         </item>
    ///         <item>Never shrink below min width (longest word) if possible.</item>
    ///         <item>Last resort: reduce all columns evenly if still over budget.</item>
    ///     </list>
    /// </summary>
    internal static int [] ComputeColumnWidths (TableData data, int maxWidth)
    {
        int cols = data.ColumnCount;
        var maxWidths = new int [cols];
        var minWidths = new int [cols];

        // Measure max (full content) and min (longest word) for each column
        for (var c = 0; c < cols; c++)
        {
            int headerMax = MeasureRenderedWidth (data.Headers [c]);
            int headerMin = MeasureLongestWord (data.Headers [c]);
            maxWidths [c] = headerMax;
            minWidths [c] = headerMin;
        }

        foreach (string [] row in data.Rows)
        {
            for (var c = 0; c < cols && c < row.Length; c++)
            {
                int cellMax = MeasureRenderedWidth (row [c]);
                int cellMin = MeasureLongestWord (row [c]);
                maxWidths [c] = Math.Max (maxWidths [c], cellMax);
                minWidths [c] = Math.Max (minWidths [c], cellMin);
            }
        }

        // Add padding (1 space each side)
        for (var c = 0; c < cols; c++)
        {
            maxWidths [c] = Math.Max (maxWidths [c] + 2, 3);
            minWidths [c] = Math.Max (minWidths [c] + 2, 3);
        }

        // If total max fits, use max widths
        var widths = (int [])maxWidths.Clone ();
        int borderChars = cols + 1; // left border + separators + right border

        if (widths.Sum () + borderChars <= maxWidth)
        {
            return widths;
        }

        // Iteratively collapse the widest column toward the second-widest (Rich-style).
        // Left columns win ties: when multiple columns share the max width,
        // we shrink the rightmost one first.
        int available = maxWidth - borderChars;

        if (available < cols * 3)
        {
            // Not enough room even for minimum columns — give each 3
            for (var c = 0; c < cols; c++)
            {
                widths [c] = 3;
            }

            return widths;
        }

        CollapseWidths (widths, minWidths, available);

        // Last resort: if still over budget, reduce all evenly
        int total = widths.Sum ();

        if (total <= available)
        {
            return widths;
        }

        int excess = total - available;
        ReduceEvenly (widths, minWidths, excess);

        return widths;
    }

    /// <summary>
    ///     Iteratively collapses the widest wrappable column toward the second-widest.
    ///     When multiple columns tie for widest, the rightmost is reduced first (left-wins).
    /// </summary>
    internal static void CollapseWidths (int [] widths, int [] minWidths, int available)
    {
        while (widths.Sum () > available)
        {
            // Find the widest column (rightmost if tied — left-wins means we shrink right first)
            var maxVal = 0;
            int maxIdx = -1;

            for (var c = 0; c < widths.Length; c++)
            {
                if (widths [c] < maxVal)
                {
                    continue;
                }
                maxVal = widths [c];
                maxIdx = c;
            }

            if (maxIdx < 0)
            {
                break;
            }

            // Find the second-widest value (excluding columns at maxVal)
            var secondMax = 0;

            foreach (int t in widths)
            {
                if (t < maxVal && t > secondMax)
                {
                    secondMax = t;
                }
            }

            // Can't reduce below min width
            int floor = Math.Max (minWidths [maxIdx], secondMax);

            if (floor >= maxVal)
            {
                // This column is already at its minimum or can't shrink further.
                // Try the next widest non-minimum column (scan right-to-left for left-wins).
                var shrank = false;

                for (int c = widths.Length - 1; c >= 0; c--)
                {
                    if (widths [c] <= minWidths [c])
                    {
                        continue;
                    }
                    widths [c]--;
                    shrank = true;

                    break;
                }

                if (!shrank)
                {
                    break;
                }

                continue;
            }

            // Reduce to the greater of secondMax and min width, but don't overshoot available
            int excess = widths.Sum () - available;
            int reduction = Math.Min (maxVal - floor, excess);
            widths [maxIdx] = maxVal - reduction;
        }
    }

    /// <summary>
    ///     Reduces all columns evenly as a last resort, respecting minimum widths first,
    ///     then going below minimum (floor at 3) if necessary.
    /// </summary>
    private static void ReduceEvenly (int [] widths, int [] minWidths, int excess)
    {
        // First pass: reduce above min widths (right-to-left for left-wins)
        while (excess > 0)
        {
            var reduced = false;

            for (int c = widths.Length - 1; c >= 0 && excess > 0; c--)
            {
                if (widths [c] <= minWidths [c])
                {
                    continue;
                }
                widths [c]--;
                excess--;
                reduced = true;
            }

            if (!reduced)
            {
                break;
            }
        }

        // Second pass: reduce below min (absolute last resort, floor at 3)
        while (excess > 0)
        {
            var reduced = false;

            for (int c = widths.Length - 1; c >= 0 && excess > 0; c--)
            {
                if (widths [c] <= 3)
                {
                    continue;
                }
                widths [c]--;
                excess--;
                reduced = true;
            }

            if (!reduced)
            {
                break;
            }
        }
    }

    /// <summary>Measures the display width of a cell's rendered text (stripping markdown formatting).</summary>
    internal static int MeasureRenderedWidth (string cellText)
    {
        List<InlineRun> runs = MarkdownInlineParser.ParseInlines (cellText, MarkdownStyleRole.Normal);
        var width = 0;

        foreach (InlineRun run in runs)
        {
            width += run.Text.GetColumns ();
        }

        return Math.Max (width, 1);
    }

    /// <summary>
    ///     Measures the display width of the longest single word in a cell's rendered text.
    ///     This determines the minimum column width (below which words would be hard-broken).
    /// </summary>
    internal static int MeasureLongestWord (string cellText)
    {
        List<InlineRun> runs = MarkdownInlineParser.ParseInlines (cellText, MarkdownStyleRole.Normal);
        var longest = 1;

        foreach (InlineRun run in runs)
        {
            string [] words = run.Text.Split (' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (string word in words)
            {
                int w = word.GetColumns ();
                longest = Math.Max (longest, w);
            }
        }

        return longest;
    }

    private static int CalculateLeftPadding (int cellWidth, int textWidth, Alignment alignment)
    {
        int innerWidth = cellWidth - 2;
        int usableTextWidth = Math.Min (textWidth, innerWidth);

        return alignment switch
        {
            Alignment.Center => 1 + Math.Max ((innerWidth - usableTextWidth) / 2, 0),
            Alignment.End => 1 + Math.Max (innerWidth - usableTextWidth, 0),
            _ => 1
        };
    }

    private static string TruncateToWidth (string text, int maxWidth)
    {
        if (maxWidth <= 0)
        {
            return string.Empty;
        }

        var width = 0;
        var charCount = 0;

        foreach (string grapheme in GraphemeHelper.GetGraphemes (text))
        {
            int gw = Math.Max (grapheme.GetColumns (), 1);

            if (width + gw > maxWidth)
            {
                break;
            }

            width += gw;
            charCount += grapheme.Length;
        }

        return text [..charCount];
    }

    /// <summary>
    ///     Returns <see langword="true"/> if the segment at the given row/col with the given URL is the currently active
    ///     link.
    /// </summary>
    private bool IsActiveLinkAt (int rowIndex, int colIndex, string url)
    {
        if (_activeLinkIndex < 0 || _activeLinkIndex >= _linkRegions.Count)
        {
            return false;
        }

        TableLinkRegion active = _linkRegions [_activeLinkIndex];

        return active.RowIndex == rowIndex && active.ColIndex == colIndex && active.Url == url;
    }

    /// <summary>
    ///     Builds the list of navigable link regions by scanning all cell segments.
    ///     Called after <see cref="Recalculate"/>.
    /// </summary>
    private void BuildLinkRegions ()
    {
        _linkRegions.Clear ();
        _activeLinkIndex = -1;

        ScanCellSegments (_headerSegments, -1);

        for (var r = 0; r < _rowSegments.Length; r++)
        {
            ScanCellSegments (_rowSegments [r], r);
        }

        bool hasLinks = _linkRegions.Count > 0;

        // For standalone usage (SuperView is NOT a Markdown), setting CanFocus here enables
        // keyboard navigation to the table. When hosted inside MarkdownView, BuildRenderedLines
        // explicitly overrides CanFocus=false before Add() so these lines have no lasting effect.
        CanFocus = hasLinks;
        TabStop = hasLinks ? TabBehavior.TabStop : TabBehavior.NoStop;

        return;

        void ScanCellSegments (List<StyledSegment> [] cellSegments, int rowIndex)
        {
            for (var col = 0; col < cellSegments.Length; col++)
            {
                string? lastUrlInCell = null;

                foreach (StyledSegment seg in cellSegments [col])
                {
                    if (string.IsNullOrWhiteSpace (seg.Url))
                    {
                        lastUrlInCell = null;

                        continue;
                    }

                    // Avoid duplicates from multi-segment rendering of the same link within one cell
                    if (seg.Url == lastUrlInCell)
                    {
                        continue;
                    }

                    lastUrlInCell = seg.Url;
                    _linkRegions.Add (new TableLinkRegion { Url = seg.Url!, RowIndex = rowIndex, ColIndex = col });
                }
            }
        }
    }

    /// <summary>A navigable link within the table. Tracks URL and cell position for unique identification.</summary>
    private sealed class TableLinkRegion
    {
        /// <summary>The target URL of the link.</summary>
        public string Url { get; init; } = "";

        /// <summary>Row index: -1 for header, 0..N for body rows.</summary>
        public int RowIndex { get; init; }

        /// <summary>Column index within the row.</summary>
        public int ColIndex { get; init; }
    }

    /// <inheritdoc/>
    bool IDesignable.EnableForDesign ()
    {
        SyntaxHighlighter = new TextMateSyntaxHighlighter ();

        Text = """
               | Feature | *Status (centered)* | **Owner** |
               |---------|:-----------------:|-------|
               | [Markdown](https://tui-cs.github.io/Terminal.Gui/api/Terminal.Gui.Views.MarkdownTable.html) | ✅ Totally! | @tig |
               | *Tables*     | ✅ For **sure!** | [tig](https://github.com/tig) |
               | `Code`       | ✅ `printf ("Awesome!");` | ??? |
               """;

        // Opt-in: prevent Link.OpenUrl from being called in the designer.
        LinkClicked += (_, e) => e.Handled = true;

        return true;
    }
}
