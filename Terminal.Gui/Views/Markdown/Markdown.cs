using System.Text.RegularExpressions;
using Markdig;

namespace Terminal.Gui.Views;

/// <summary>
///     A read-only view that renders Markdown-formatted text with styled headings, lists, links, code blocks, and
///     more.
/// </summary>
/// <remarks>
/// <img src="../images/views/Markdown.gif" alt="Markdown demo"/>
///     <para>
///         Set the <see cref="Text"/> property to supply content. The view parses the Markdown,
///         performs word-wrap layout, and draws styled output. Fenced code blocks receive a
///         full-width dimmed background; inline code, emphasis, strong, and other elements are
///         rendered with appropriate text styles and colors.
///     </para>
///     <para>
///         Hyperlinks raise the <see cref="LinkClicked"/> event. Anchor links (URLs beginning with
///         <c>#</c>) are handled automatically by scrolling to the matching heading.
///     </para>
///     <para>Default key bindings:</para>
///     <list type="table">
///         <listheader>
///             <term>Key</term> <description>Action</description>
///         </listheader>
///         <item>
///             <term>Ctrl+A</term>
///             <description>Selects all rendered content (<see cref="Command.SelectAll"/>).</description>
///         </item>
///         <item>
///             <term>Ctrl+C</term>
///             <description>
///                 Copies the current selection to the clipboard, or the entire markdown source if nothing is selected
///                 (<see cref="Command.Copy"/>).
///             </description>
///         </item>
///         <item>
///             <term>Shift+F10 / Right-click</term>
///             <description>Opens a context menu with <b>Select All</b> and <b>Copy</b> items. Right-clicking on a hyperlink also adds a <b>Copy Link</b> item that copies the URL to the clipboard.</description>
///         </item>
///     </list>
///     <para>Default mouse bindings:</para>
///     <list type="table">
///         <listheader>
///             <term>Mouse Event</term> <description>Action</description>
///         </listheader>
///         <item>
///             <term>Left-button drag</term> <description>Selects text by dragging the mouse.</description>
///         </item>
///         <item>
///             <term>Left-button click</term>
///             <description>Clears the selection and activates a hyperlink if one is under the cursor.</description>
///         </item>
///         <item>
///             <term>Right-button click</term> <description>Opens the context menu.</description>
///         </item>
///     </list>
/// </remarks>
public partial class Markdown : View, IDesignable
{
    private const int MIN_WRAP_WIDTH = 4;

    private readonly List<IntermediateBlock> _blocks = [];
    private readonly List<RenderedLine> _renderedLines = [];
    private readonly List<MarkdownLinkRegion> _linkRegions = [];
    private readonly Dictionary<string, SixelToRender> _sixelRenderMap = [];
    private readonly HashSet<string> _visibleSixelIds = [];
    private readonly Dictionary<string, int> _headingAnchors = new (StringComparer.OrdinalIgnoreCase);
    private readonly List<MarkdownCodeBlock> _codeBlockViews = [];
    private readonly List<MarkdownTable> _tableViews = [];
    private readonly List<Line> _thematicBreakViews = [];

    private string _markdown = string.Empty;
    private bool _parsed;
    private int _layoutWidth = -1;
    private int _maxLineWidth;
    private int _activeLinkIndex = -1;
    private string? _contextMenuLinkUrl;
    private bool _inLayout;
    private bool _scrollToTopPending;
    private int _externalContentWidth;

    /// <summary>Initializes a new instance of the <see cref="Markdown"/> class.</summary>
    public Markdown ()
    {
        CanFocus = true;
        ViewportSettings |= ViewportSettingsFlags.HasVerticalScrollBar;

        SetupBindingsAndCommands ();
    }

    /// <inheritdoc/>
    /// <remarks>
    ///     If <see cref="View.CanFocus"/> is <see langword="false"/> and a valid <see cref="View.HotKey"/>
    ///     is set, the hotkey is forwarded to the next peer in <see cref="View.SuperView"/>'s
    ///     <see cref="View.SubViews"/> — mirroring <see cref="Label"/> so that a non-focusable
    ///     <see cref="Markdown"/> describing a focusable view (e.g. a <see cref="TextField"/>) moves
    ///     focus to that view when its hotkey is pressed.
    /// </remarks>
    protected override bool OnActivating (CommandEventArgs args)
    {
        // If Markdown can't focus, forward HotKey to the next peer in the SubView list
        if (CanFocus || !HotKey.IsValid)
        {
            return base.OnActivating (args);
        }
        int me = SuperView?.SubViews.IndexOf (this) ?? -1;

        if (me == -1 || !(me < SuperView?.SubViews.Count - 1))
        {
            return base.OnActivating (args);
        }
        bool handled = SuperView?.SubViews.ElementAt (me + 1).InvokeCommand (Command.HotKey) == true;

        if (!handled)
        {
            return base.OnActivating (args);
        }
        args.Handled = true;

        return true;
    }

    /// <summary>Gets or sets the Markdown-formatted text displayed by this view.</summary>
    /// <value>The raw Markdown string. Setting this property triggers reparsing, re-layout, and a redraw.</value>
    protected override void OnTextChanged ()
    {
        SetMarkdown (Text);

        base.OnTextChanged ();
    }

    /// <inheritdoc/>
    /// <remarks>
    ///     <see cref="Markdown"/> does not use <see cref="View.TextFormatter"/> for rendering.
    ///     It uses its own styled-line pipeline. Clearing the formatter text prevents the base
    ///     <see cref="View"/> from drawing raw markdown as plain text.
    /// </remarks>
    protected override void UpdateTextFormatterText ()
    {
        TextFormatter.Text = string.Empty;
    }

    /// <inheritdoc/>
    /// <remarks>
    ///     Unlike <see cref="Label"/>, <see cref="Markdown"/> derives <see cref="View.HotKey"/>
    ///     from <see cref="Text"/> (the raw markdown) rather than <see cref="View.Title"/>,
    ///     because <see cref="Text"/> does not flow through <c>Title</c>.
    /// </remarks>
    public override Rune HotKeySpecifier
    {
        get => base.HotKeySpecifier;
        set
        {
            TitleTextFormatter.HotKeySpecifier = TextFormatter.HotKeySpecifier = value;
            UpdateHotKeyFromMarkdown ();
        }
    }

    /// <summary>Gets or sets the Markdig <see cref="Markdig.MarkdownPipeline"/> used for parsing.</summary>
    /// <value>
    ///     A custom pipeline, or <see langword="null"/> to use the default pipeline
    ///     (built with <c>UseAdvancedExtensions</c>).
    /// </value>
    public MarkdownPipeline? MarkdownPipeline
    {
        get;
        set
        {
            if (ReferenceEquals (field, value))
            {
                return;
            }

            field = value;
            InvalidateParsedAndLayout ();
        }
    }

    /// <summary>Gets or sets an optional syntax highlighter for fenced code blocks.</summary>
    /// <value>An <see cref="ISyntaxHighlighter"/> implementation, or <see langword="null"/> for plain-text code blocks.</value>
    public ISyntaxHighlighter? SyntaxHighlighter
    {
        get;
        set
        {
            if (ReferenceEquals (field, value))
            {
                return;
            }

            field = value;
            InvalidateParsedAndLayout ();
        }
    }

    /// <summary>
    ///     Gets or sets whether the view fills its background with the syntax highlighting theme's
    ///     editor background color. When <see langword="true"/> and a <see cref="SyntaxHighlighter"/>
    ///     is set, the theme's <see cref="ISyntaxHighlighter.DefaultBackground"/> is used for the
    ///     entire viewport, headings, body text, and table cells. Defaults to <see langword="true"/>.
    /// </summary>
    public bool UseThemeBackground
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            InvalidateParsedAndLayout ();
        }
    } = true;

    /// <summary>
    ///     Gets or sets whether heading lines include the <c>#</c> prefix (e.g. <c># </c>, <c>## </c>).
    ///     When <see langword="true"/> (default), the hash markers are displayed so that heading levels
    ///     are visually distinguishable. When <see langword="false"/>, only the heading text is shown.
    /// </summary>
    public bool ShowHeadingPrefix
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            InvalidateParsedAndLayout ();
        }
    } = true;

    /// <summary>Gets or sets an optional callback that loads image data as UTF-8 encoded sixel payloads.</summary>
    /// <value>A function that accepts an image source path and returns sixel bytes, or <see langword="null"/>.</value>
    public Func<string, byte []?>? ImageLoader { get; set; }

    /// <summary>
    ///     Gets or sets whether code blocks display a copy button in the top-right corner.
    ///     Defaults to <see langword="true"/>.
    /// </summary>
    public bool ShowCopyButtons { get; set; } = true;

    /// <summary>Gets or sets whether sixel image rendering is enabled.</summary>
    /// <value><see langword="true"/> to attempt sixel rendering for images; otherwise <see langword="false"/>.</value>
    public bool EnableSixelImages { get; set; }

    /// <summary>Gets the total number of rendered lines after parsing and word-wrap layout.</summary>
    public int LineCount => _renderedLines.Count;

    /// <summary>
    ///     Raised when a hyperlink is clicked. Set <see cref="MarkdownLinkEventArgs.Handled"/> to prevent default
    ///     navigation.
    /// </summary>
    public event EventHandler<MarkdownLinkEventArgs>? LinkClicked;

    /// <summary>Raised after the <see cref="Text"/> property changes and the content has been reparsed.</summary>
    public event EventHandler<EventArgs>? MarkdownChanged;

    /// <summary>Called when a hyperlink is clicked, before the <see cref="LinkClicked"/> event is raised.</summary>
    /// <param name="args">The event data containing the link URL.</param>
    /// <returns><see langword="true"/> if the link click was handled and no further processing should occur.</returns>
    protected virtual bool OnLinkClicked (MarkdownLinkEventArgs args) => false;

    /// <summary>Called after the <see cref="Text"/> property changes, before <see cref="MarkdownChanged"/> is raised.</summary>
    protected virtual void OnMarkdownChanged () { }

    /// <inheritdoc/>
    protected override void OnContentSizeChanged (ValueChangedEventArgs<Size?> args)
    {
        base.OnContentSizeChanged (args);

        if (_inLayout)
        {
            return;
        }

        // External caller set ContentSize — use that width for layout
        _externalContentWidth = args.NewValue?.Width ?? 0;

        int effectiveWidth = GetEffectiveLayoutWidth ();

        if (_layoutWidth == effectiveWidth)
        {
            return;
        }

        // Width changed — mark stale so OnSubViewLayout rebuilds
        _layoutWidth = -1;
        SetNeedsLayout ();
    }

    /// <summary>Scrolls the viewport so that the heading matching the given anchor slug is visible at the top.</summary>
    /// <param name="anchor">
    ///     The anchor identifier (with or without a leading <c>#</c>). Anchors are generated from heading text
    ///     using GitHub-style slug rules: lowercase, spaces become hyphens, non-alphanumeric characters are removed,
    ///     and duplicate headings receive <c>-1</c>, <c>-2</c>, etc. suffixes.
    /// </param>
    /// <returns>
    ///     <see langword="true"/> if a matching heading was found and the viewport was scrolled; otherwise
    ///     <see langword="false"/>.
    /// </returns>
    public bool ScrollToAnchor (string anchor)
    {
        if (string.IsNullOrEmpty (anchor))
        {
            return false;
        }

        string slug = anchor.StartsWith ('#') ? anchor [1..] : anchor;

        if (!_headingAnchors.TryGetValue (slug, out int lineIndex))
        {
            return false;
        }

        Viewport = Viewport with { Y = Math.Min (lineIndex, Math.Max (_renderedLines.Count - Viewport.Height, 0)) };
        SetNeedsDraw ();

        return true;
    }

    private void SetMarkdown (string value)
    {
        if (_markdown == value)
        {
            return;
        }

        _markdown = value;
        UpdateHotKeyFromMarkdown ();
        _scrollToTopPending = true;
        InvalidateParsedAndLayout ();

        OnMarkdownChanged ();
        MarkdownChanged?.Invoke (this, EventArgs.Empty);
    }

    private void UpdateHotKeyFromMarkdown ()
    {
        if (HotKeySpecifier == new Rune ('\xFFFF'))
        {
            HotKey = Key.Empty;

            return;
        }

        if (TextFormatter.FindHotKey (_markdown, HotKeySpecifier, out _, out Key hotKey))
        {
            if (HotKey != hotKey)
            {
                HotKey = hotKey;
            }

            return;
        }

        HotKey = Key.Empty;
    }

    private void InvalidateParsedAndLayout ()
    {
        _parsed = false;
        _layoutWidth = -1;
        _blocks.Clear ();
        _renderedLines.Clear ();
        _linkRegions.Clear ();
        foreach (var render in _sixelRenderMap.Values) { render.SixelData = null; }
        _sixelRenderMap.Clear ();
        _visibleSixelIds.Clear ();
        _activeLinkIndex = -1;
        _headingAnchors.Clear ();
        RemoveCodeBlockViews ();
        RemoveTableViews ();
        RemoveThematicBreakViews ();
        _maxLineWidth = 0;
        _isSelecting = false;

        SetNeedsLayout ();
        SetNeedsDraw ();
    }

    /// <summary>Returns the effective width used for text wrapping and table layout.</summary>
    /// <remarks>
    ///     When <see cref="View.SetContentSize"/> has been called externally to set a specific
    ///     content width (e.g. via a NumericUpDown), that width is used. Otherwise
    ///     <see cref="View.Viewport"/> width is used.
    /// </remarks>
    private int GetEffectiveLayoutWidth () => _externalContentWidth > 0 ? _externalContentWidth : Viewport.Width;

    /// <inheritdoc/>
    /// <remarks>
    ///     Performs the heavy lifting of markdown rendering: parses the document (if needed),
    ///     builds the word-wrapped rendered lines, and creates/removes SubViews for tables,
    ///     code blocks, and thematic breaks. This runs during the layout pass — never during draw.
    /// </remarks>
    protected override void OnSubViewLayout (LayoutEventArgs args)
    {
        base.OnSubViewLayout (args);

        EnsureParsed ();

        int effectiveWidth = GetEffectiveLayoutWidth ();

        if (effectiveWidth < MIN_WRAP_WIDTH || _layoutWidth == effectiveWidth)
        {
            return;
        }

        _layoutWidth = effectiveWidth;

        RemoveCodeBlockViews ();
        RemoveTableViews ();
        RemoveThematicBreakViews ();

        BuildRenderedLines ();

        // Clear any auto-focus that Add() triggered when a focusable table was added
        // while this view has focus. Tables should only receive focus via explicit Tab.
        // The cascade from HasFocus=false calls OnAdvancingFocus(Forward, TabStop) which
        // just sets _activeLinkIndex without focusing another table — safe here.
        if (Focused is MarkdownTable)
        {
            Focused.HasFocus = false;
            _activeLinkIndex = -1;
        }

        // Update content size so the viewport knows the scrollable extent
        int contentWidth = Math.Max (effectiveWidth, _maxLineWidth);
        _inLayout = true;
        SetContentSize (new Size (contentWidth, _renderedLines.Count));
        _inLayout = false;

        // After rebuilding for new content, reset scroll position to the top.
        // This must happen AFTER SetContentSize so the viewport clamp logic sees
        // the correct content height and doesn't re-adjust the position.
        if (!_scrollToTopPending)
        {
            return;
        }
        _scrollToTopPending = false;
        Viewport = Viewport with { X = 0, Y = 0 };
    }

    private void RemoveCodeBlockViews ()
    {
        foreach (MarkdownCodeBlock cb in _codeBlockViews)
        {
            Remove (cb);
            cb.Dispose ();
        }

        _codeBlockViews.Clear ();
    }

    private void RemoveTableViews ()
    {
        foreach (MarkdownTable table in _tableViews)
        {
            Remove (table);
            table.Dispose ();
        }

        _tableViews.Clear ();
    }

    private void RemoveThematicBreakViews ()
    {
        foreach (Line line in _thematicBreakViews)
        {
            Remove (line);
            line.Dispose ();
        }

        _thematicBreakViews.Clear ();
    }

    private bool RaiseLinkClicked (string url)
    {
        MarkdownLinkEventArgs args = new (url);

        if (OnLinkClicked (args) || args.Handled)
        {
            return true;
        }

        LinkClicked?.Invoke (this, args);

        return args.Handled;
    }

    /// <summary>Generates a GitHub-style anchor slug from heading text.</summary>
    internal static string GenerateAnchorSlug (string headingText)
    {
        string lower = headingText.Trim ().ToLowerInvariant ();

        // Remove non-alphanumeric, non-hyphen, non-space, non-underscore chars
        string slug = Regex.Replace (lower, @"[^\w\s-]", "", RegexOptions.None);

        // Replace each space with a hyphen individually (GitHub does NOT collapse runs)
        slug = slug.Replace (' ', '-');

        return slug.Trim ('-');
    }

    /// <inheritdoc/>
    string? IDesignable.GetDemoKeyStrokes () => "wait:300," + string.Join (",", Enumerable.Repeat ("CursorDown,wait:80", 50)) + ",wait:800";

    /// <inheritdoc/>
    bool IDesignable.EnableForDesign ()
    {
        SyntaxHighlighter = new TextMateSyntaxHighlighter ();
        Text = DefaultMarkdownSample;

        // Opt-in: prevent Link.OpenUrl from being called in the designer.
        LinkClicked += (_, e) => e.Handled = true;

        return true;
    }

    /// <summary>Gets a short but comprehensive Markdown sample covering common features.</summary>
    public static string DefaultMarkdownSample { get; } = """
                                                          # Terminal.Gui Markdown Sample 🚀

                                                          ## TOC

                                                          * [Basic Formatting](#basic-formatting)
                                                          * [Links](#links)
                                                          * [Checklist](#checklist)
                                                          * [Code Blocks](#code-blocks)
                                                          * [Tables](#tables)
                                                          * [Separators](#separators)
                                                          * [Block Quotes](#block-quotes)

                                                          ## Basic Formatting

                                                          Rich text with **bold**, *italic*, `inline code`, and ~~strikethrough~~.

                                                          ## Links

                                                          * [Markdown API docs](https://tui-cs.github.io/Terminal.Gui/api/Terminal.Gui.Views.Markdown.html) for more info.
                                                          * [Relative local link](docs/getting-started.md) renders as a link without opening a URI handler by default.

                                                          ## Checklist

                                                          - [x] Text with **bold**, *italic*, `inline code`, and ~~strikethrough~~ ✅
                                                          - [x] Inline `Code` 🔧
                                                          - [x] [Links](https://github.com/tui-cs) 🎉
                                                          - [ ] Images 😒

                                                          ## Code Blocks

                                                          **csharp** code block with syntax highlighting:

                                                          ```csharp
                                                          Console.WriteLine ("Hello, Terminal.Gui! 🌍");
                                                          var x = 42;
                                                          ```

                                                          **markdown** code block illustrating nested markdown:

                                                          ```md
                                                          # Heading 1

                                                          Plain text. *Formatted text* with **bold** and `inline code`.

                                                          Link:  [SyntaxHighlighting](https://tui-cs.github.io/Terminal.Gui/api/Terminal.Gui.SyntaxHighlighting.html).

                                                          - [x] Checked

                                                          | Col | Col2 |
                                                          |-----|:----:|
                                                          | A   | One  |
                                                          | B   | Two  |
                                                          ```

                                                          ## Tables

                                                          **table** with links, emojis, and markdown in cells:

                                                          | Feature        | Status        |
                                                          |----------------|---------------|
                                                          | [Links](https://tui-cs.github.io/Terminal.Gui/api/Terminal.Gui.Views.MarkdownTable.html) | ✅ Totally! |
                                                          | Inline `code`  | ✅ *Awesome!*   |
                                                          | Emojis 🎉      | ✅ **Whoa!**      |

                                                          **table** with different alignments:

                                                          | First         | Second |
                                                          |---------------|:------:|
                                                          | Row 1         | Czech (✅) me out. I'm long and centered. |
                                                          | Row 2 👋     | 🔛 I'm shorter but still centered 🔛 |

                                                          ## Separators

                                                          This text is before the thematic break.

                                                          ---

                                                          And this text is after. Thematic breaks are rendered as full-width horizontal lines that automatically adjust to the layout width.

                                                          ## Block Quotes

                                                          > **Tip:** This is a block quote with *inline formatting*.

                                                          Here's a multi-line block quote with a link, code, and more:

                                                          > **Tip:** Block quotes can contain *inline formatting*, **bold text**,
                                                          > `inline code`, and [links](https://example.com).
                                                          >
                                                          > They can also span multiple lines with blank quote lines between paragraphs.

                                                          That's all folks! 👋
                                                          """;
}
