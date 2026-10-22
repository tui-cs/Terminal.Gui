namespace Terminal.Gui.Views;

public partial class Markdown
{
    private void BuildRenderedLines ()
    {
        _renderedLines.Clear ();
        _headingAnchors.Clear ();
        _maxLineWidth = 0;

        int viewportWidth = Math.Max (GetEffectiveLayoutWidth (), MIN_WRAP_WIDTH);

        // Pre-scan: compute _maxLineWidth from unwrapped blocks so that tables can be
        // sized using the final content width (which may be wider than the viewport when
        // long unwrapped lines — e.g. code — expand the content area).
        foreach (IntermediateBlock block in _blocks)
        {
            if (block.IsThematicBreak || block.IsTable || block.Wrap)
            {
                continue;
            }

            int width = CalculateWidth (MarkdownAttributeHelper.ToStyledSegments (block.Runs));

            if (!string.IsNullOrEmpty (block.Prefix))
            {
                width += block.Prefix.GetColumns ();
            }

            _maxLineWidth = Math.Max (_maxLineWidth, width);
        }

        // Freeze the table constraint before including table overflow in the document extent.
        // Unwrapped document lines still contribute to this constraint.
        int tableLayoutWidth = Math.Max (viewportWidth, _maxLineWidth);

        foreach (IntermediateBlock block in _blocks)
        {
            // Record heading anchor → rendered-line index before adding lines
            if (!string.IsNullOrEmpty (block.Anchor))
            {
                _headingAnchors [block.Anchor!] = _renderedLines.Count;
            }

            // Thematic breaks get a Line SubView
            if (block.IsThematicBreak)
            {
                int lineY = _renderedLines.Count;

                Line lineView = new ()
                {
                    X = 1,
                    Y = lineY,
                    Width = Dim.Fill (1),
                    Height = 1,
                    CanFocus = false
                };

                // Apply theme background to the Line SubView when enabled
                if (UseThemeBackground && SyntaxHighlighter?.DefaultBackground is { } lineThemeBg)
                {
                    Attribute lineNormal = GetAttributeForRole (VisualRole.Normal) with { Background = lineThemeBg };
                    lineView.SetScheme (new Scheme (lineNormal));
                }

                _thematicBreakViews.Add (lineView);
                Add (lineView);

                // Reserve a placeholder line
                _renderedLines.Add (new RenderedLine ([new StyledSegment ("", MarkdownStyleRole.ThematicBreak)], false, 0, isThematicBreak: true));

                continue;
            }

            // Table blocks get a MarkdownTable SubView and placeholder lines
            if (block is { IsTable: true, TableData: { } tableData })
            {
                int startLine = _renderedLines.Count;

                MarkdownTable tableView = new ()
                {
                    SyntaxHighlighter = SyntaxHighlighter,
                    UseThemeBackground = UseThemeBackground,
                    X = 0,
                    Y = startLine,
                    LayoutConstraint = tableLayoutWidth
                };
                tableView.SetContent (tableData, block.TableContent, tableLayoutWidth);
                tableView.Width = tableView.TableLayout.Width;
                _maxLineWidth = Math.Max (_maxLineWidth, tableView.TableLayout.Width);

                // Capture the rendered height BEFORE Add() — Add triggers EndInit → Layout
                // which may recalculate the table at a stale content width, corrupting RenderedHeight.
                int tableHeight = tableView.RenderedHeight;

                // Forward link clicks from the table to this Markdown view's LinkClicked event.
                tableView.LinkClicked += (_, e) =>
                                         {
                                             // Handle anchor links the same way as paragraph links
                                             if (e.Url.StartsWith ('#'))
                                             {
                                                 ScrollToAnchor (e.Url);
                                             }

                                             if (!RaiseLinkClicked (e.Url))
                                             {
                                                 return;
                                             }

                                             e.Handled = true;
                                         };

                // Temporarily disable CanFocus before Add() to prevent AddAt() from
                // auto-focusing this table when MarkdownView currently has focus.
                // CanFocus is re-enabled safely after all tables are added (see below).
                tableView.CanFocus = false;

                _tableViews.Add (tableView);
                Add (tableView);

                // Reserve placeholder lines so content height is correct
                for (var i = 0; i < tableHeight; i++)
                {
                    _renderedLines.Add (new RenderedLine ([], false, tableView.TableLayout.Width, isTable: true)
                    {
                        TableView = tableView,
                        TableRow = i
                    });
                }

                continue;
            }

            if (!block.Wrap)
            {
                RenderedLine unwrapped = CreateUnwrappedLine (block);
                _renderedLines.Add (unwrapped);
                _maxLineWidth = Math.Max (_maxLineWidth, unwrapped.Width);

                continue;
            }

            List<RenderedLine> wrapped = WrapBlock (block, viewportWidth);
            string? sourceText = block.SourceText;

            foreach (RenderedLine line in wrapped)
            {
                line.SourceText = sourceText;
                sourceText = sourceText is null ? null : string.Empty;
                _renderedLines.Add (line);
                _maxLineWidth = Math.Max (_maxLineWidth, line.Width);
            }
        }

        if (_renderedLines.Count == 0)
        {
            _renderedLines.Add (new RenderedLine ([new StyledSegment ("", MarkdownStyleRole.Normal)], true, 0));
        }

        SyncCodeBlockViews ();
        BuildLinkRegions ();

        // Re-enable CanFocus on tables that have links. A temporary HasFocusChanging
        // handler cancels any auto-focus triggered by the CanFocus setter's guard
        // (which fires when SuperView.Focused is null). This keeps tables navigable
        // via Tab without stealing focus during layout.
        foreach (MarkdownTable table in _tableViews)
        {
            if (!table.HasLinks || GetEffectiveLayoutWidth () < MIN_WRAP_WIDTH)
            {
                continue;
            }

            table.HasFocusChanging += CancelFocusDuringLayout;
            table.CanFocus = true;
            table.TabStop = TabBehavior.TabStop;
            table.HasFocusChanging -= CancelFocusDuringLayout;
        }

        return;

        static void CancelFocusDuringLayout (object? sender, HasFocusEventArgs e)
        {
            e.Cancel = true;
        }
    }

    /// <summary>
    ///     Scans rendered lines for contiguous code block runs and creates a
    ///     <see cref="MarkdownCodeBlock"/> SubView for each.
    /// </summary>
    private void SyncCodeBlockViews ()
    {
        RemoveCodeBlockViews ();

        var i = 0;

        while (i < _renderedLines.Count)
        {
            if (!_renderedLines [i].IsCodeBlock)
            {
                i++;

                continue;
            }

            int start = i;

            while (i < _renderedLines.Count && _renderedLines [i].IsCodeBlock)
            {
                i++;
            }

            // Gather segments per line for this code block
            List<IReadOnlyList<StyledSegment>> codeLines = [];

            for (int j = start; j < i; j++)
            {
                codeLines.Add (_renderedLines [j].Segments);
            }

            MarkdownCodeBlock codeBlock = new ()
            {
                SyntaxHighlighter = SyntaxHighlighter,
                StyledLines = codeLines,
                X = 0,
                Y = start,
                Width = Dim.Fill (),
                ShowCopyButton = ShowCopyButtons
            };

            // When a syntax highlighter provides a default background, compute a
            // slightly shifted variant and set it as the code block's ThemeBackground.
            // This ensures code blocks are visually distinct from body text AND
            // compatible with the highlighter's token foreground colors.
            //
            // We pass !isDark to GetDimmerColor so the bg shifts *away* from the
            // body background: dark themes get a slightly lighter code block bg,
            // light themes get a slightly darker one. Passing isDark (the intuitive
            // direction) caused light-theme code blocks to wash out to medium gray
            // because white (L≥90) hit the fallback in GetDimmerColor.
            //
            // We compute the color directly rather than using Scheme/VisualRole.Code
            // because scheme resolution depends on view tree init state, but this
            // code runs during layout before the new SubView is fully initialised.
            if (SyntaxHighlighter?.DefaultBackground is { } highlighterBg)
            {
                bool isDark = highlighterBg.IsDarkColor ();
                codeBlock.ThemeBackground = highlighterBg.GetDimmerColor (0.2, !isDark);
            }

            _codeBlockViews.Add (codeBlock);
            Add (codeBlock);
        }
    }

    private static RenderedLine CreateUnwrappedLine (IntermediateBlock block)
    {
        List<StyledSegment> segments = [];

        if (!string.IsNullOrEmpty (block.Prefix))
        {
            segments.Add (new StyledSegment (block.Prefix, MarkdownStyleRole.ListMarker));
        }

        segments.AddRange (MarkdownAttributeHelper.ToStyledSegments (block.Runs));

        int width = CalculateWidth (segments);

        return new RenderedLine (segments, false, width, block.IsCodeBlock, block.IsThematicBreak, codeLanguage: block.Language);
    }

    internal static List<RenderedLine> WrapBlock (IntermediateBlock block, int viewportWidth)
    {
        List<RenderedLine> lines = [];

        string firstPrefix = block.Prefix;
        string continuationPrefix = string.IsNullOrEmpty (block.ContinuationPrefix) ? firstPrefix : block.ContinuationPrefix;

        List<StyledSegment> currentSegments = [];
        var currentWidth = 0;
        var firstLine = true;
        var endedWithHardBreak = false;

        if (!string.IsNullOrEmpty (firstPrefix))
        {
            currentSegments.Add (new StyledSegment (firstPrefix, MarkdownStyleRole.ListMarker));
            currentWidth = firstPrefix.GetColumns ();
        }

        foreach (InlineRun run in block.Runs)
        {
            foreach (string grapheme in GraphemeHelper.GetGraphemes (run.Text))
            {
                if (grapheme == "\n")
                {
                    if (currentSegments.Count == 0)
                    {
                        currentSegments.Add (new StyledSegment ("", MarkdownStyleRole.Normal));
                    }

                    lines.Add (new RenderedLine ([.. currentSegments], true, currentWidth));
                    currentSegments.Clear ();
                    currentWidth = 0;
                    firstLine = false;
                    endedWithHardBreak = true;

                    if (!string.IsNullOrEmpty (continuationPrefix))
                    {
                        currentSegments.Add (new StyledSegment (continuationPrefix, MarkdownStyleRole.ListMarker));
                        currentWidth += continuationPrefix.GetColumns ();
                    }

                    continue;
                }

                endedWithHardBreak = false;
                int graphemeWidth = Math.Max (grapheme.GetColumns (), 1);

                if (currentWidth + graphemeWidth > viewportWidth && currentSegments.Count > 0)
                {
                    // Find last whitespace for word-boundary wrap (skip prefix segments).
                    // Avoid breaking inside parentheses — for each candidate space, verify
                    // it is not inside unclosed parens by forward-scanning from the start.
                    int breakIdx = -1;

                    for (int s = currentSegments.Count - 1; s >= 0; s--)
                    {
                        if (currentSegments [s].StyleRole == MarkdownStyleRole.ListMarker)
                        {
                            break;
                        }

                        if (!string.IsNullOrWhiteSpace (currentSegments [s].Text))
                        {
                            continue;
                        }

                        // Check paren depth at this position via forward scan
                        var depth = 0;

                        for (var j = 0; j <= s; j++)
                        {
                            if (currentSegments [j].Text == "(")
                            {
                                depth++;
                            }
                            else if (currentSegments [j].Text == ")")
                            {
                                depth--;
                            }
                        }

                        if (depth > 0)
                        {
                            continue;
                        }

                        breakIdx = s;

                        break;
                    }

                    if (breakIdx >= 0)
                    {
                        // Word wrap: emit up to and including the space
                        List<StyledSegment> lineSegments = currentSegments.GetRange (0, breakIdx + 1);
                        List<StyledSegment> overflow = currentSegments.GetRange (breakIdx + 1, currentSegments.Count - breakIdx - 1);

                        lines.Add (new RenderedLine ([.. lineSegments], true, CalculateWidth (lineSegments)));

                        currentSegments = [.. overflow];
                        currentWidth = CalculateWidth (currentSegments);
                    }
                    else
                    {
                        // No word boundary found — hard break at current position
                        lines.Add (new RenderedLine ([.. currentSegments], true, currentWidth));
                        currentSegments.Clear ();
                        currentWidth = 0;
                    }

                    firstLine = false;

                    if (!string.IsNullOrEmpty (continuationPrefix))
                    {
                        currentSegments.Insert (0, new StyledSegment (continuationPrefix, MarkdownStyleRole.ListMarker));
                        currentWidth += continuationPrefix.GetColumns ();
                    }
                }

                currentSegments.Add (run.ToStyledSegment (grapheme));
                currentWidth += graphemeWidth;
            }
        }

        // A terminal hard break ends the current line; it does not start a phantom prefix-only line.
        if (endedWithHardBreak)
        {
            return lines;
        }

        if (currentSegments.Count == 0)
        {
            string prefix = firstLine ? firstPrefix : continuationPrefix;

            List<StyledSegment> emptySegments = string.IsNullOrEmpty (prefix)
                                                    ? [new StyledSegment ("", MarkdownStyleRole.Normal)]
                                                    : [new StyledSegment (prefix, MarkdownStyleRole.ListMarker)];
            int width = string.IsNullOrEmpty (prefix) ? 0 : prefix.GetColumns ();
            lines.Add (new RenderedLine (emptySegments, true, width));

            return lines;
        }

        lines.Add (new RenderedLine ([.. currentSegments], true, currentWidth));

        return lines;
    }

    private static int CalculateWidth (IReadOnlyList<StyledSegment> segments) =>
        segments.SelectMany (segment => GraphemeHelper.GetGraphemes (segment.Text)).Sum (grapheme => Math.Max (grapheme.GetColumns (), 1));
}
