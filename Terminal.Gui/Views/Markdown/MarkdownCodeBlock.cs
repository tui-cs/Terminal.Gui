namespace Terminal.Gui.Views;

/// <summary>
///     A read-only view that renders a single Markdown fenced code block with a dimmed background
///     and an optional copy button.
/// </summary>
/// <remarks>
///     <para>
///         When used inside a <see cref="Markdown"/>, instances are created automatically during
///         layout and positioned as SubViews at the correct content coordinate so that they scroll
///         naturally with the parent's viewport.
///     </para>
///     <para>
///         The dimmed background fills the full width of the view (via <c>Width = Dim.Fill()</c>),
///         so it automatically resizes when the content area width changes.
///     </para>
///     <para>
///         This view can also be used standalone. Set <see cref="View.Text"/> to a fenced code block
///         (e.g. <c>```csharp\ncode\n```</c>) or plain text. The language is extracted automatically
///         from the opening fence.
///     </para>
/// </remarks>
public class MarkdownCodeBlock : View, IDesignable
{
    private IReadOnlyList<IReadOnlyList<StyledSegment>> _lines = [];

    // Tracks the last Height/Width Dim instances assigned by UpdateContentSize () so
    // content-driven resizing can continue until the user explicitly assigns dimensions.
    private Dim? _heightAssignedByContent;
    private Dim? _widthAssignedByContent;

    /// <summary>Initializes a new <see cref="MarkdownCodeBlock"/>.</summary>
    public MarkdownCodeBlock ()
    {
        CanFocus = false;
        TabStop = TabBehavior.NoStop;
        Width = Dim.Auto (DimAutoStyle.Content);
        Height = Dim.Auto (DimAutoStyle.Content);
    }

    /// <summary>
    ///     Gets or sets the code block content. The setter accepts fenced code block format
    ///     (<c>```lang\ncode\n```</c>) and extracts the language automatically. Plain text
    ///     (without fences) is also accepted and treated as language-less code.
    /// </summary>
    protected override void OnTextChanged ()
    {
        ParseFencedText (Text);

        base.OnTextChanged ();
    }

    /// <inheritdoc/>
    protected override void UpdateTextFormatterText ()
    {
        TextFormatter.Text = string.Empty;
    }

    /// <summary>
    ///     Gets or sets whether the copy button is shown in the top-right corner.
    ///     Defaults to <see langword="true"/>.
    /// </summary>
    public bool ShowCopyButton { get; set; } = true;

    /// <summary>
    ///     Gets or sets an override background color from the syntax highlighting theme.
    ///     When set, the code block viewport uses this instead of <see cref="VisualRole.Code"/>.
    /// </summary>
    public Color? ThemeBackground { get; set; }

    /// <summary>
    ///     Gets or sets an optional syntax highlighter. When set together with <see cref="Language"/>,
    ///     setting <see cref="CodeLines"/> re-highlights the content through the highlighter.
    /// </summary>
    public ISyntaxHighlighter? SyntaxHighlighter { get; set; }

    /// <summary>
    ///     Gets or sets the language identifier for syntax highlighting (e.g. <c>"csharp"</c>, <c>"python"</c>).
    ///     Used together with <see cref="SyntaxHighlighter"/> when setting <see cref="CodeLines"/>.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    ///     Gets or sets the styled line segments. Used internally by <see cref="Markdown"/> to set
    ///     pre-parsed styled content directly.
    /// </summary>
    internal IReadOnlyList<IReadOnlyList<StyledSegment>> StyledLines
    {
        get => _lines;
        set
        {
            _lines = value;
            UpdateContentSize ();
        }
    }

    /// <summary>
    ///     Gets or sets the plain-text code lines. Setting this re-creates the internal styled
    ///     segments. When <see cref="SyntaxHighlighter"/> and <see cref="Language"/> are both set,
    ///     lines are syntax-highlighted; otherwise they use <see cref="MarkdownStyleRole.CodeBlock"/> styling.
    /// </summary>
    public IReadOnlyList<string> CodeLines
    {
        get
        {
            List<string> result = [];
            result.AddRange (_lines.Select (segments => string.Concat (segments.Select (s => s.Text))));

            return result;
        }
        set
        {
            if (SyntaxHighlighter is { } highlighter && !string.IsNullOrEmpty (Language))
            {
                highlighter.ResetState ();
                List<IReadOnlyList<StyledSegment>> segments = [];

                foreach (string line in value)
                {
                    segments.Add (highlighter.Highlight (line, Language));
                }

                _lines = segments;
                ThemeBackground = highlighter.DefaultBackground;
            }
            else
            {
                List<IReadOnlyList<StyledSegment>> segments = [];
                segments.AddRange (value.Select (line => (IReadOnlyList<StyledSegment>)[new StyledSegment (line, MarkdownStyleRole.CodeBlock, role: VisualRole.Code)]));

                _lines = segments;
            }

            UpdateContentSize ();
        }
    }

    private void UpdateContentSize ()
    {
        var maxWidth = 0;

        foreach (IReadOnlyList<StyledSegment> line in _lines)
        {
            int lineWidth = line.Sum (s => s.Text.GetColumns ());
            maxWidth = Math.Max (maxWidth, lineWidth);
        }

        // Set explicit dimensions based on content.
        // We avoid SetContentSize because it sets ContentSizeTracksViewport = false,
        // which restricts Viewport width when Width = Dim.Fill() (embedded in MarkdownView).
        if (Width is DimAuto || ReferenceEquals (Width, _widthAssignedByContent))
        {
            Width = maxWidth;
            _widthAssignedByContent = Width;
        }
        else
        {
            _widthAssignedByContent = null;
        }

        if (Height is DimAuto || ReferenceEquals (Height, _heightAssignedByContent))
        {
            Height = _lines.Count;
            _heightAssignedByContent = Height;
        }
        else
        {
            _heightAssignedByContent = null;
        }

        SetNeedsLayout ();
        SetNeedsDraw ();
    }

    /// <summary>
    ///     Parses text that may be in fenced code block format. If the text starts with <c>```</c>,
    ///     the language is extracted from the opening fence and the fences are stripped. Otherwise,
    ///     the text is treated as plain code lines.
    /// </summary>
    private void ParseFencedText (string text)
    {
        if (string.IsNullOrEmpty (text))
        {
            Language = null;
            CodeLines = [];

            return;
        }

        string [] allLines = text.ReplaceLineEndings ("\n").Split ('\n');

        // Check if the first line is a fence opener (``` or ```lang)
        if (allLines.Length >= 2 && allLines [0].TrimStart ().StartsWith ("```", StringComparison.Ordinal))
        {
            string fenceLine = allLines [0].TrimStart ();
            string lang = fenceLine.Length > 3 ? fenceLine [3..].Trim () : string.Empty;
            Language = string.IsNullOrEmpty (lang) ? null : lang;

            // Find the closing fence
            int endIndex = allLines.Length;

            for (int i = allLines.Length - 1; i >= 1; i--)
            {
                if (!allLines [i].TrimStart ().StartsWith ("```", StringComparison.Ordinal))
                {
                    continue;
                }
                endIndex = i;

                break;
            }

            // Extract code lines between fences
            string [] codeLines = allLines [1..endIndex];
            CodeLines = codeLines;
        }
        else
        {
            // No fences — treat as plain code
            Language = null;
            CodeLines = allLines;
        }
    }

    /// <inheritdoc/>
    protected override bool OnMouseEvent (Mouse mouse)
    {
        if (!ShowCopyButton)
        {
            return false;
        }

        if (!mouse.Flags.HasFlag (MouseFlags.LeftButtonClicked))
        {
            return false;
        }

        if (mouse.Position is not { } pos)
        {
            return false;
        }

        int copyGlyphX = Viewport.Width - 2;

        if ((pos.X != copyGlyphX && pos.X != copyGlyphX + 1) || pos.Y != 0)
        {
            return false;
        }

        string codeText = ExtractText ();
        App?.Clipboard?.TrySetClipboardData (codeText);

        return true;
    }

    /// <inheritdoc/>
    protected override bool OnClearingViewport ()
    {
        if (base.OnClearingViewport ())
        {
            return true;
        }

        // Fill entire area with code block background
        Attribute normal = GetAttributeForRole (VisualRole.Code);
        Color codeBg = ThemeBackground ?? normal.Background;
        Attribute codeAttr = new (normal.Foreground, codeBg);
        SetAttribute (codeAttr);
        ClearViewport ();

        return true;
    }

    /// <inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        // Fill entire area with code block background
        Attribute normal = GetAttributeForRole (VisualRole.Code);
        Color codeBg = ThemeBackground ?? normal.Background;
        Attribute codeAttr = new (normal.Foreground, codeBg);

        for (var y = 0; y < _lines.Count && y < Viewport.Height; y++)
        {
            IReadOnlyList<StyledSegment> segments = _lines [y];
            var x = 0;

            foreach (StyledSegment segment in segments)
            {
                Attribute attr = MarkdownAttributeHelper.GetAttributeForSegment (this, segment, SyntaxHighlighter, codeBg);
                SetAttribute (attr);

                foreach (string grapheme in GraphemeHelper.GetGraphemes (segment.Text))
                {
                    int gw = Math.Max (grapheme.GetColumns (), 1);

                    if (x >= Viewport.Width)
                    {
                        break;
                    }

                    AddStr (x, y, grapheme);
                    x += gw;
                }
            }
        }

        // Draw the copy glyph in the top-right corner
        if (!ShowCopyButton || Viewport.Width <= 0 || Viewport.Height <= 0)
        {
            return true;
        }

        SetAttribute (codeAttr);
        AddStr (Viewport.Width - 2, 0, Glyphs.Copy.ToString ());

        return true;
    }

    /// <summary>Extracts the plain text of this code block.</summary>
    public string ExtractText ()
    {
        List<string> lineTexts = [];
        lineTexts.AddRange (_lines.Select (segments => string.Concat (segments.Select (s => s.Text))));

        return string.Join (Environment.NewLine, lineTexts);
    }

    /// <inheritdoc/>
    bool IDesignable.EnableForDesign ()
    {
        SyntaxHighlighter = new TextMateSyntaxHighlighter ();

        Text = """
               ```csharp
               using IApplication app = Application.Create ();
               app.Init ();
               using ExampleWindow exampleWindow = new ();
               app.Run (exampleWindow)
               ```
               """;

        return true;
    }
}
