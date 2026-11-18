namespace Terminal.Gui.Views;

/// <summary>
///     A read-only view that renders a single Markdown table with box-drawing borders via
///     <see cref="LineCanvas"/> and styled header/body text with inline Markdown formatting.
/// </summary>
/// <remarks>
///     <para>
///         When used inside a <see cref="Markdown"/>, instances are created automatically during
///         layout and positioned as SubViews at the correct content coordinate so that they scroll
///         naturally with the SuperView's viewport.
///     </para>
///     <para>
///         Borders are rendered using <see cref="LineStyle.Single"/>.
///     </para>
///     <para>
///         Header cells are bold; body cells support inline Markdown formatting (bold, italic, code,
///         links). Tables created by <see cref="Markdown"/> retain parsed inline segments; standalone
///         text uses <see cref="MarkdownInlineParser"/>. Column alignment (left, center, right)
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
    internal MarkdownTableContent Content { get; private set; } = new ([], []);
    internal MarkdownTableLayout TableLayout { get; private set; } = new ();
    internal int? LayoutConstraint { get; set; }
    private MarkdownTableContent? _unhighlightedContent;
    private int _lastComputedWidth = -1;
    private IReadOnlyList<MarkdownTableLink> _linkRegions = [];
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

    private ISyntaxHighlighter? _syntaxHighlighter;

    /// <summary>
    ///     Gets or sets an optional syntax highlighter used to resolve theme-based attributes for
    ///     inline styling roles (emphasis, code spans, links, etc.). When set, the table queries
    ///     the highlighter for scope-derived colors before falling back to <see cref="TextStyle"/>-based defaults.
    /// </summary>
    public ISyntaxHighlighter? SyntaxHighlighter
    {
        get => _syntaxHighlighter;
        set
        {
            if (ReferenceEquals (_syntaxHighlighter, value))
            {
                return;
            }

            _syntaxHighlighter = value;
            Content = _unhighlightedContent?.WithHighlighter (value) ?? Content;
            _lastComputedWidth = -1;
            SetNeedsLayout ();
            SetNeedsDraw ();
        }
    }

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

    private string BuildTableText () => MarkdownTableSerializer.Serialize (Content);

    private TableData _data = _emptyData;

    /// <summary>
    ///     Gets or sets the <see cref="Views.TableData"/> that defines the table content. Setting this
    ///     recomputes column widths, row heights, and redraws the table.
    /// </summary>
    public TableData TableData
    {
        get => _data;
        set => SetContent (value, null, Frame.Width > 0 ? Frame.Width : 80);
    }

    internal void SetContent (TableData data, MarkdownTableContent? content, int width)
    {
        ArgumentNullException.ThrowIfNull (data);
        _data = data;
        _unhighlightedContent = content ?? MarkdownTableCellParser.CreateContent (data);
        Content = _unhighlightedContent.WithHighlighter (SyntaxHighlighter);

        // Keep Text in sync without parsing or laying out discarded intermediate content.
        SetTextDirect (BuildTableText ());
        _lastComputedWidth = -1;
        Recalculate (width);
        SetNeedsLayout ();
        SetNeedsDraw ();
    }

    /// <summary>Recalculates column widths and row heights for the given available width.</summary>
    internal void Recalculate (int maxWidth)
    {
        if (maxWidth == _lastComputedWidth)
        {
            return;
        }

        _lastComputedWidth = maxWidth;
        MarkdownTableLink? active = _activeLinkIndex >= 0 && _activeLinkIndex < _linkRegions.Count ? _linkRegions [_activeLinkIndex] : null;
        TableLayout = MarkdownTableLayoutEngine.Build (Content, maxWidth);
        RenderedHeight = TableLayout.Height;
        Height = RenderedHeight;
        SetContentSize (new Size (TableLayout.Width, TableLayout.Height));
        _linkRegions = TableLayout.Links;
        _activeLinkIndex = active is null ? -1 : Enumerable.Range (0, _linkRegions.Count).FirstOrDefault (index => _linkRegions [index] == active, -1);
        CanFocus = _linkRegions.Count > 0;
        TabStop = CanFocus ? TabBehavior.TabStop : TabBehavior.NoStop;
    }

    /// <summary>
    ///     Gets the computed rendered height (in lines) after <see cref="Recalculate"/> runs.
    ///     This value is always current, unlike <see cref="View.Frame"/> which updates only after layout.
    /// </summary>
    internal int RenderedHeight { get; private set; }

    /// <summary>Gets whether this table contains any navigable link regions.</summary>
    internal bool HasLinks => _linkRegions.Count > 0;

    private int _activeLinkIndex = -1;

    internal int ActiveLinkIndex
    {
        get => _activeLinkIndex;
        set
        {
            _activeLinkIndex = value >= 0 && value < _linkRegions.Count ? value : -1;
            SetNeedsDraw ();
        }
    }

    /// <summary>Gets the total rendered height of this table in lines (simple estimate).</summary>
    /// <remarks>
    ///     This simple estimation assumes single-line rows. Used by external callers that don't have
    ///     wrapped row heights. For the actual rendered height, use the instance's <see cref="View.Height"/>.
    /// </remarks>
    public static int CalculateTableHeight (TableData data)
    {
        if (!data.IsStructured)
        {
            return data.Rows.Length + 4;
        }

        IReadOnlyList<TableRowData> rows = data.TableRows;

        if (data.ColumnCount == 0 || rows.Count == 0)
        {
            return 0;
        }

        int separators = data.HasRowSeparators ? rows.Count - 1
                                              : Enumerable.Range (1, rows.Count - 1).Count (row => rows [row - 1].IsHeader != rows [row].IsHeader);

        int trailingHeaderSeparator = !data.HasRowSeparators && rows [^1].IsHeader ? 1 : 0;

        return rows.Count + separators + trailingHeaderSeparator + 2;
    }

    /// <inheritdoc/>
    protected override void OnSubViewLayout (LayoutEventArgs args)
    {
        base.OnSubViewLayout (args);

        Recalculate (LayoutConstraint ?? Frame.Width);
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
            ScrollToActiveLink ();
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
        ScrollToActiveLink ();
        SetNeedsDraw ();

        return true;
    }

    private void ScrollToActiveLink ()
    {
        MarkdownTableGlyph? glyph = TableLayout.Glyphs.FirstOrDefault (glyph => glyph.LinkIndex == _activeLinkIndex);

        if (glyph is null)
        {
            return;
        }

        View scrollView = SuperView is Markdown ? SuperView : this;
        int x = glyph.Position.X + (ReferenceEquals (scrollView, this) ? 0 : Frame.X);
        int y = glyph.Position.Y + (ReferenceEquals (scrollView, this) ? 0 : Frame.Y);
        int left = scrollView.Viewport.X;
        int top = scrollView.Viewport.Y;

        if (x < left || x + glyph.Width > left + scrollView.Viewport.Width)
        {
            left = x < left ? x : x + glyph.Width - scrollView.Viewport.Width;
        }

        if (y < top || y >= top + scrollView.Viewport.Height)
        {
            top = y < top ? y : y - scrollView.Viewport.Height + 1;
        }

        scrollView.Viewport = scrollView.Viewport with { X = Math.Max (0, left), Y = Math.Max (0, top) };
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
    internal int HitTestLinkIndex (int viewportX, int viewportY)
    {
        if (viewportX < 0 || viewportY < 0 || viewportX >= Viewport.Width || viewportY >= Viewport.Height)
        {
            return -1;
        }

        Point position = new (viewportX + Viewport.X, viewportY + Viewport.Y);
        MarkdownTableGlyph? glyph = TableLayout.GetLine (position.Y).FirstOrDefault (glyph => position.X >= glyph.Position.X
                                                                             && position.X < glyph.Position.X + glyph.Width);

        if (glyph is null || glyph.Position.X < Viewport.X || glyph.Position.X + glyph.Width > Viewport.Right)
        {
            return -1;
        }

        return glyph.LinkIndex;
    }

    /// <inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        Attribute normal = GetAttributeForRole (VisualRole.Normal);
        Color? themeBackground = UseThemeBackground ? SyntaxHighlighter?.DefaultBackground : null;

        if (themeBackground is { } background)
        {
            normal = normal with { Background = background };
        }

        SetAttribute (normal);
        FillRect (new Rectangle (0, 0, Viewport.Width, Viewport.Height), (Rune)' ');

        // Resolve each style once per draw so theme changes remain visible without per-glyph lookups.
        Dictionary<(MarkdownStyleRole Style, VisualRole? Role, Attribute? Explicit, TextStyle Additional, string? Url), Attribute> attributes = [];

        foreach (MarkdownTableGlyph glyph in TableLayout.Glyphs)
        {
            int x = glyph.Position.X - Viewport.X;
            int y = glyph.Position.Y - Viewport.Y;

            if (x < 0 || y < 0 || x + glyph.Width > Viewport.Width || y >= Viewport.Height)
            {
                continue;
            }

            StyledSegment segment = glyph.Segment;
            (MarkdownStyleRole, VisualRole?, Attribute?, TextStyle, string?) key = (segment.StyleRole, segment.Role, segment.Attribute, segment.AdditionalStyle, segment.Url);

            if (!attributes.TryGetValue (key, out Attribute attribute))
            {
                attribute = MarkdownAttributeHelper.GetAttributeForSegment (this, segment, SyntaxHighlighter, themeBackground);
                attributes.Add (key, attribute);
            }

            if (glyph.IsHeader)
            {
                attribute = attribute with { Style = attribute.Style | TextStyle.Bold };
            }

            if (HasFocus && glyph.LinkIndex >= 0 && glyph.LinkIndex == _activeLinkIndex)
            {
                attribute = new (attribute.Background, attribute.Foreground, attribute.Style);
            }

            bool absolute = glyph.LinkIndex >= 0 && TableLayout.Links [glyph.LinkIndex].IsSafeAbsolute;

            if (absolute && Driver is { })
            {
                Driver.CurrentUrl = glyph.Segment.Url;
            }

            try
            {
                SetAttribute (attribute);
                AddStr (x, y, glyph.Text);
            }
            finally
            {
                if (absolute && Driver is { })
                {
                    Driver.CurrentUrl = null;
                }
            }
        }

        Point origin = ViewportToScreen (new Point (-Viewport.X, -Viewport.Y));

        foreach ((Point start, int length, Orientation orientation) in TableLayout.Borders)
        {
            LineCanvas.AddLine (new (origin.X + start.X, origin.Y + start.Y), length, orientation, LineStyle.Single, normal);
        }

        return true;
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
        List<StyledSegment> word = [];
        var currentWidth = 0;

        foreach (StyledSegment segment in segments)
        {
            var start = 0;

            for (var index = 0; index < segment.Text.Length; index++)
            {
                char character = segment.Text [index];

                if (character is not (' ' or '\t' or '\n'))
                {
                    continue;
                }

                if (index > start)
                {
                    word.Add (segment.WithText (segment.Text [start..index]));
                }

                FlushWord ();
                start = index + 1;

                if (character == '\n')
                {
                    FlushLine ();

                    continue;
                }

                if (currentWidth < maxWidth)
                {
                    currentLine.Add (segment.WithText (" "));
                    currentWidth++;
                }
            }

            if (start < segment.Text.Length)
            {
                word.Add (segment.WithText (segment.Text [start..]));
            }
        }

        FlushWord ();

        if (currentLine.Count > 0)
        {
            FlushLine ();
        }

        if (lines.Count == 0)
        {
            lines.Add ([]);
        }

        return lines;

        void FlushLine ()
        {
            lines.Add (currentLine);
            currentLine = [];
            currentWidth = 0;
        }

        void FlushWord ()
        {
            if (word.Count == 0)
            {
                return;
            }

            int wordWidth = string.Concat (word.Select (part => part.Text)).GetColumns ();

            if (currentWidth > 0 && currentWidth + wordWidth > maxWidth)
            {
                FlushLine ();
            }

            foreach (StyledSegment part in word)
            {
                string remaining = part.Text;

                while (remaining.Length > 0)
                {
                    int remainingWidth = remaining.GetColumns ();
                    string chunk = remainingWidth == 0 || remainingWidth <= maxWidth - currentWidth
                                       ? remaining
                                       : TruncateToWidth (remaining, maxWidth - currentWidth);

                    if (chunk.Length == 0 && currentWidth > 0)
                    {
                        FlushLine ();

                        continue;
                    }

                    // Keep a wide grapheme intact even when the column is narrower than it.
                    if (chunk.Length == 0)
                    {
                        chunk = GraphemeHelper.GetGraphemes (remaining).First ();
                    }

                    currentLine.Add (part.WithText (chunk));
                    currentWidth += chunk.GetColumns ();
                    remaining = remaining [chunk.Length..];
                }
            }

            word.Clear ();
        }
    }

    /// <summary>
    ///     Computes column widths using a Rich-style collapse algorithm:
    ///     <list type="number">
    ///         <item>Use each column's cached min (longest word + padding) and max (full content + padding) widths.</item>
    ///         <item>If total max fits within <paramref name="maxWidth"/>, use max widths.</item>
    ///         <item>
    ///             Otherwise, iteratively collapse the widest column toward the second-widest.
    ///             Left columns win ties (preserving their width).
    ///         </item>
    ///         <item>Never shrink below min width (longest word) if possible.</item>
    ///         <item>Last resort: reduce all columns evenly if still over budget.</item>
    ///     </list>
    /// </summary>
    internal static int [] ComputeColumnWidths (MarkdownTableContent content, int maxWidth) =>
        MarkdownTableLayoutEngine.AllocateWidths (content, maxWidth);

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

    /// <summary>Measure the display width of a cell's styled content.</summary>
    internal static int MeasureRenderedWidth (IReadOnlyList<StyledSegment> segments) =>
        Math.Max (string.Concat (segments.Select (segment => segment.Text)).Split ('\n').Max (line => line.GetColumns ()), 1);

    /// <summary>Measure the longest displayed word across inline style boundaries.</summary>
    internal static int MeasureLongestWord (IReadOnlyList<StyledSegment> segments)
    {
        string text = string.Concat (segments.Select (segment => segment.Text));
        string [] words = text.Split ([' ', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries);

        return words.Length == 0 ? 1 : Math.Max (words.Max (word => word.GetColumns ()), 1);
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
