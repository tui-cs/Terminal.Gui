namespace Terminal.Gui.Views;

public partial class Markdown
{
    private bool _isSelecting;
    private Point _selectionAnchor;
    private Point _selectionCurrent;
    private bool _isDragging;

    /// <summary>Gets the context menu for this view.</summary>
    public PopoverMenu? ContextMenu { get; private set; }

    /// <summary>Selects all rendered content.</summary>
    /// <returns><see langword="true"/> if the operation succeeded.</returns>
    public bool SelectAll ()
    {
        if (_renderedLines.Count == 0)
        {
            return true;
        }

        _isSelecting = true;
        _selectionAnchor = new Point (0, 0);
        int lastLine = _renderedLines.Count - 1;
        _selectionCurrent = new Point (GetLineDisplayWidth (lastLine), lastLine);
        SetNeedsDraw ();

        return true;
    }

    /// <summary>
    ///     Gets the text that corresponds to the current selection, rendered as plain text from
    ///     the displayed content.  Returns <see langword="null"/> when no selection is active.
    /// </summary>
    /// <remarks>
    ///     The returned string reflects the on-screen representation (display text) of the selected
    ///     region — not the original markdown source.  Markdown structure such as bullet-list
    ///     markers (<c>- </c>), fenced code-block delimiters (<c>```</c>), and heading hashes
    ///     (<c>#</c>) may differ from the source document.
    /// </remarks>
    public string? SelectedText => _isSelecting ? GetSelectedText () : null;

    /// <summary>
    ///     Copies the current selection, or the entire markdown document if nothing is selected, to the clipboard.
    /// </summary>
    /// <returns><see langword="true"/> if the copy was performed.</returns>
    public bool Copy ()
    {
        string text = _isSelecting ? GetSelectedText () : _markdown;
        App?.Clipboard?.TrySetClipboardData (text);

        return true;
    }

    /// <summary>Clears the current selection.</summary>
    public void ClearSelection ()
    {
        if (!_isSelecting)
        {
            return;
        }

        _isSelecting = false;
        SetNeedsDraw ();
    }

    /// <summary>
    ///     Returns <see langword="true"/> if the cell at (<paramref name="lineIdx"/>, <paramref name="x"/>)
    ///     falls within the current selection.
    /// </summary>
    /// <param name="lineIdx">The rendered-line index (content Y coordinate).</param>
    /// <param name="x">The display column (content X coordinate).</param>
    internal bool IsInSelection (int lineIdx, int x)
    {
        if (!_isSelecting)
        {
            return false;
        }

        (Point start, Point end) = GetNormalizedSelection ();

        if (lineIdx < start.Y || lineIdx > end.Y)
        {
            return false;
        }

        if (start.Y == end.Y)
        {
            return x >= start.X && x < end.X;
        }

        if (lineIdx == start.Y)
        {
            return x >= start.X;
        }

        if (lineIdx == end.Y)
        {
            return x < end.X;
        }

        return true;
    }

    private (Point Start, Point End) GetNormalizedSelection ()
    {
        if (_selectionAnchor.Y < _selectionCurrent.Y || (_selectionAnchor.Y == _selectionCurrent.Y && _selectionAnchor.X <= _selectionCurrent.X))
        {
            return (_selectionAnchor, _selectionCurrent);
        }

        return (_selectionCurrent, _selectionAnchor);
    }

    private string GetSelectedText ()
    {
        if (_renderedLines.Count == 0)
        {
            return string.Empty;
        }

        // When the entire document is selected return the original source markdown.
        // _renderedLines is a display buffer: it cannot carry inline formatting markers,
        // heading hashes, table syntax, or thematic-break syntax.  Returning _markdown
        // preserves everything and is always correct for a full-document selection.
        if (IsFullDocumentSelected ())
        {
            return _markdown;
        }

        (Point start, Point end) = GetNormalizedSelection ();
        List<string> outputLines = [];
        var inCodeBlock = false;
        string? currentCodeLanguage = null;

        // Fences are injected only when the selection crosses a code-block boundary:
        //   • Opening fence: emitted when entering a code block after selected non-code
        //     content, and also when transitioning directly to an adjacent selected code
        //     block whose opening fence has not yet been written.
        //   • Closing fence: always when the selection crosses out of the code block into
        //     non-code content — regardless of whether an opening fence was emitted.
        //   • No trailing fence: when the selection ends inside a code block, no closing
        //     fence is added; the selection ends mid-block.
        // This produces no fences for a selection entirely within a code block, matching
        // the behaviour of the copy-button on MarkdownCodeBlock. codeOpenFenceEmitted
        // tracks whether the current selected code block already has its opening fence.
        var selectionHasNonCodeContent = false;
        var codeOpenFenceEmitted = false;

        for (int lineIdx = start.Y; lineIdx <= Math.Min (end.Y, _renderedLines.Count - 1); lineIdx++)
        {
            RenderedLine line = _renderedLines [lineIdx];

            if (line.IsCodeBlock)
            {
                string? nextCodeLanguage = line.CodeLanguage;

                if (!inCodeBlock)
                {
                    inCodeBlock = true;
                    currentCodeLanguage = nextCodeLanguage;

                    // Only inject the opening fence when non-code content has already been
                    // output — that is, the selection crosses from outside into this code block.
                    if (selectionHasNonCodeContent)
                    {
                        outputLines.Add ($"```{nextCodeLanguage ?? string.Empty}");
                        codeOpenFenceEmitted = true;
                    }
                    else
                    {
                        codeOpenFenceEmitted = false;
                    }
                }
                else if (!string.Equals (currentCodeLanguage, nextCodeLanguage, StringComparison.Ordinal))
                {
                    // Transitioning directly between two adjacent code blocks of different
                    // languages: close the current fence (if opened) and open the next one.
                    if (codeOpenFenceEmitted)
                    {
                        outputLines.Add ("```");
                    }

                    outputLines.Add ($"```{nextCodeLanguage ?? string.Empty}");
                    codeOpenFenceEmitted = true;
                    currentCodeLanguage = nextCodeLanguage;
                }
            }
            else if (inCodeBlock)
            {
                // Leaving a code block into non-code content: always inject the closing fence.
                // The selection crosses the block's end boundary regardless of whether the
                // opening fence was emitted (e.g., when the selection started inside the block).
                outputLines.Add ("```");

                inCodeBlock = false;
                codeOpenFenceEmitted = false;
                currentCodeLanguage = null;
                selectionHasNonCodeContent = true;
            }
            else
            {
                selectionHasNonCodeContent = true;
            }

            if (line.TableView is { } table)
            {
                int first = lineIdx - line.TableRow;
                int last = first + table.TableLayout.Height - 1;
                bool complete = (start.Y < first || (start.Y == first && start.X <= 1))
                                && (end.Y > last || (end.Y == last && end.X >= table.TableLayout.Width - 1));

                if (complete)
                {
                    outputLines.Add (MarkdownTableSerializer.Serialize (table.Content));
                    lineIdx = last;

                    continue;
                }

                int firstColumn = lineIdx == start.Y ? start.X : 0;
                int lastColumn = lineIdx == end.Y ? end.X : table.TableLayout.Width;
                outputLines.Add (table.TableLayout.SelectLine (line.TableRow, firstColumn, lastColumn));

                continue;
            }

            int lineStartX = lineIdx == start.Y ? start.X : 0;
            int lineEndX = lineIdx == end.Y ? end.X : int.MaxValue;

            if (line.SourceText is { Length: > 0 } sourceText && lineStartX == 0 && lineEndX >= line.Width)
            {
                int lastSourceLine = lineIdx;

                while (lastSourceLine + 1 < _renderedLines.Count && _renderedLines [lastSourceLine + 1].SourceText == string.Empty)
                {
                    lastSourceLine++;
                }

                if (lastSourceLine < end.Y || (lastSourceLine == end.Y && end.X >= _renderedLines [lastSourceLine].Width))
                {
                    outputLines.Add (sourceText);
                    lineIdx = lastSourceLine;

                    continue;
                }
            }

            StringBuilder lineSb = new ();
            AppendLineText (lineSb, line, lineStartX, lineEndX);
            outputLines.Add (lineSb.ToString ());
        }

        return string.Join ("\n", outputLines);
    }

    /// <summary>
    ///     Returns <see langword="true"/> when the selection spans the entire rendered document
    ///     from the first character to the last, so that <see cref="GetSelectedText"/> can
    ///     return the original markdown source instead of the lossy display representation.
    /// </summary>
    private bool IsFullDocumentSelected ()
    {
        (Point start, Point end) = GetNormalizedSelection ();

        if (start.X != 0 || start.Y != 0)
        {
            return false;
        }

        int lastLine = _renderedLines.Count - 1;

        // Table rows carry their layout width; reaching their first column is not SelectAll.
        // A thematic break has no text width, so any position on its row reaches its end.
        return end.Y >= lastLine && end.X >= GetLineDisplayWidth (lastLine);
    }

    private static void AppendLineText (StringBuilder sb, RenderedLine line, int startX, int endX)
    {
        var contentX = 0;

        foreach (StyledSegment segment in line.Segments)
        {
            string text = segment.StyleRole == MarkdownStyleRole.ListMarker ? TranslateListMarkerText (segment.Text) : segment.Text;

            foreach (string grapheme in GraphemeHelper.GetGraphemes (text))
            {
                int gw = Math.Max (grapheme.GetColumns (), 1);

                if (contentX + gw <= startX)
                {
                    contentX += gw;

                    continue;
                }

                if (contentX >= endX)
                {
                    return;
                }

                sb.Append (grapheme);
                contentX += gw;
            }
        }
    }

    /// <summary>Converts rendered list marker text back to Markdown source marker text for selection and clipboard operations.</summary>
    /// <param name="text">The rendered list marker text.</param>
    /// <returns>The Markdown source marker text, or <paramref name="text"/> when it is not a rendered list marker.</returns>
    private static string TranslateListMarkerText (string text)
    {
        const string BULLET_PREFIX = "• ";

        if (!text.StartsWith (BULLET_PREFIX, StringComparison.Ordinal))
        {
            return text;
        }

        string remainder = text [BULLET_PREFIX.Length..];

        var checkedGlyph = $"{Glyphs.CheckStateChecked} ";
        var uncheckedGlyph = $"{Glyphs.CheckStateUnChecked} ";

        if (remainder.StartsWith (checkedGlyph, StringComparison.Ordinal))
        {
            return "- [x] " + remainder [checkedGlyph.Length..];
        }

        if (remainder.StartsWith (uncheckedGlyph, StringComparison.Ordinal))
        {
            return "- [ ] " + remainder [uncheckedGlyph.Length..];
        }

        return "- " + remainder;
    }

    private int GetLineDisplayWidth (int lineIdx)
    {
        if (lineIdx < 0 || lineIdx >= _renderedLines.Count)
        {
            return 0;
        }

        return _renderedLines [lineIdx].Width;
    }

    private void CreateContextMenu ()
    {
        DisposeContextMenu ();

        List<View?> menuItems = [new MenuItem (this, Command.SelectAll), new MenuItem (this, Command.Copy)];

        if (_contextMenuLinkUrl is { } url)
        {
            menuItems.Insert (0, null);
            menuItems.Insert (0, new MenuItem ("Copy _Link", action: () => App?.Clipboard?.TrySetClipboardData (url)));
        }

        PopoverMenu menu = new (menuItems)
        {
#if DEBUG
            Id = "markdownContextMenu"
#endif
        };

        HotKeyBindings.Remove (menu.Key);
        HotKeyBindings.Add (menu.Key, Command.Context);
        menu.KeyChanged += ContextMenuOnKeyChanged;

        ContextMenu = menu;
        App?.Popovers?.Register (ContextMenu);
    }

    private void DisposeContextMenu ()
    {
        if (ContextMenu is null)
        {
            return;
        }

        ContextMenu.Visible = false;
        App?.Popovers?.DeRegister (ContextMenu);
        ContextMenu.KeyChanged -= ContextMenuOnKeyChanged;
        ContextMenu.Dispose ();
        ContextMenu = null;
    }

    private void ContextMenuOnKeyChanged (object? sender, KeyChangedEventArgs e) => KeyBindings.Replace (e.OldKey.KeyCode, e.NewKey.KeyCode);

    private Point GetContextMenuScreenPosition ()
    {
        Point viewportPosition = _isSelecting ? _selectionCurrent : new Point (0, 0);

        return ViewportToScreen (viewportPosition);
    }

    private bool ShowContextMenu (Point? screenPosition = null)
    {
        if (screenPosition is { } pos)
        {
            Point viewportPos = ScreenToViewport (pos);
            int contentX = Viewport.X + viewportPos.X;
            int contentY = Viewport.Y + viewportPos.Y;
            _contextMenuLinkUrl = FindLinkUrlAt (contentX, contentY);
            CreateContextMenu ();
        }

        Point menuPosition = screenPosition ?? GetContextMenuScreenPosition ();
        ContextMenu?.MakeVisible (menuPosition);

        return true;
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            DisposeContextMenu ();
        }

        base.Dispose (disposing);
    }
}
