using System.Linq;
namespace Terminal.Gui.Views;

public partial class Markdown
{
    /// <inheritdoc/>
    /// <remarks>
    ///     Draws the background and styled lines only when this view's content needs repainting,
    ///     before SubViews are drawn. Scrollbar hover redraws leave the existing content intact.
    /// </remarks>
    protected override bool OnClearingViewport ()
    {
        Attribute fillAttr = GetAttributeForRole (VisualRole.Normal);

        if (UseThemeBackground && SyntaxHighlighter?.DefaultBackground is { } themeBg)
        {
            fillAttr = fillAttr with { Background = themeBg };
        }

        SetAttribute (fillAttr);

        // Clearing the full viewport also invalidates every visible SubView so tables,
        // code blocks, and thematic breaks repaint any cells cleared outside a partial dirty region.
        ClearViewport ();

        _visibleSixelIds.Clear ();

        int startRow = Viewport.Y;
        int endRow = Math.Min (Viewport.Y + Viewport.Height, _renderedLines.Count);

        for (int contentRow = startRow; contentRow < endRow; contentRow++)
        {
            int drawRow = contentRow - Viewport.Y;
            DrawRenderedLine (_renderedLines [contentRow], contentRow, drawRow);
        }

        // Cleanup sixels that are no longer visible
        var toRemove = _sixelRenderMap.Keys.Where (id => !_visibleSixelIds.Contains (id)).ToList ();
        foreach (var id in toRemove)
        {
            _sixelRenderMap [id].SixelData = null;
            _sixelRenderMap [id].IsDirty = false;
            _sixelRenderMap.Remove (id);
        }

        return true;
    }

    /// <inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        // All visible content was drawn in OnClearingViewport; just register the drawn region.
        context?.AddDrawnRegion (new Region (new Rectangle (ContentToScreen (Point.Empty), Viewport.Size)));

        // OnDrawingContent is called AFTER SubViews have drawn.  Plain rendered lines are
        // highlighted during DrawRenderedLine (in OnClearingViewport), but table and code-block
        // rows are owned by their SubViews and don't receive that pass.  Draw the selection
        // overlay for those rows here, on top of what the SubViews rendered.
        if (_isSelecting)
        {
            DrawSelectionOverlayOnSubViewRows ();
        }

        return true;
    }

    /// <summary>
    ///     Draws the selection highlight over table and fenced-code-block rows.
    ///     Those rows are owned by SubViews (<see cref="MarkdownTable"/> /
    ///     <see cref="MarkdownCodeBlock"/>) that draw after
    ///     <see cref="OnClearingViewport"/> returns.  This pass reads the graphemes
    ///     that the SubViews already placed in the screen buffer and re-draws them
    ///     with the selection attribute, preserving the rendered characters while
    ///     applying the selection background.
    /// </summary>
    private void DrawSelectionOverlayOnSubViewRows ()
    {
        Cell [,]? contents = ScreenContents;

        if (contents is null)
        {
            return;
        }

        Attribute selAttr = GetAttributeForRole (VisualRole.Focus);
        (Point start, Point end) = GetNormalizedSelection ();

        int startRow = Math.Max (start.Y, Viewport.Y);
        int endRow = Math.Min (end.Y, Viewport.Y + Viewport.Height - 1);

        var anySubViewRows = false;

        for (int lineIdx = startRow; lineIdx <= Math.Min (endRow, _renderedLines.Count - 1); lineIdx++)
        {
            if (!_renderedLines [lineIdx].IsTable && !_renderedLines [lineIdx].IsCodeBlock)
            {
                continue;
            }
            anySubViewRows = true;

            break;
        }

        if (!anySubViewRows)
        {
            return;
        }

        // After DoDrawSubViews each SubView calls DoDrawComplete which excludes its screen
        // area from Driver.Clip.  DoDrawContent (OnDrawingContent) runs with those exclusions
        // still active, so drawing would silently no-op on SubView areas.
        // Reset the clip to the raw viewport rectangle to allow the overlay to appear.
        Region? savedClip = GetClip ();
        Rectangle viewportScreen = ViewportToScreen (new Rectangle (Point.Empty, Viewport.Size));
        SetClip (new Region (viewportScreen));

        // Popovers draw before the MarkdownView in the application draw loop, so their menu
        // items are already written to the screen buffer when we run.  The SetClip call above
        // resets the clip to allow drawing over SubView areas, but it also undoes the clip
        // exclusion that the popover's DoDrawComplete registered for its drawn cells.  Without
        // a guard, we would overwrite those cells with stale ScreenContents graphemes, erasing
        // the popover.  (Paragraph-text selection is drawn in DrawRenderedLine / OnClearingViewport
        // before the clip reset, so it naturally inherits the popover's exclusion and is safe.)
        // Compute the popover's content rect (screen-relative) and skip any cells inside it.
        Rectangle? popoverScreenRect = null;

        if (App?.Popovers?.GetActivePopover () is View { Visible: true } popoverView)
        {
            View? popoverContent = popoverView.SubViews.FirstOrDefault (v => v.Visible);

            if (popoverContent is { })
            {
                popoverScreenRect = popoverContent.Frame;
            }
        }

        for (int lineIdx = startRow; lineIdx <= Math.Min (endRow, _renderedLines.Count - 1); lineIdx++)
        {
            RenderedLine line = _renderedLines [lineIdx];

            if (line is { IsTable: false, IsCodeBlock: false })
            {
                continue;
            }

            int drawRow = lineIdx - Viewport.Y;
            Point screenOrigin = ContentToScreen (new Point (Viewport.X, lineIdx));
            int screenRow = screenOrigin.Y;
            int screenStartCol = screenOrigin.X;
            int cols = Viewport.Width;

            for (var col = 0; col < cols; col++)
            {
                int sc = screenStartCol + col;

                if (screenRow < 0 || screenRow >= contents.GetLength (0) || sc < 0 || sc >= contents.GetLength (1))
                {
                    continue;
                }

                if (popoverScreenRect is { } psr && psr.Contains (new Point (sc, screenRow)))
                {
                    continue;
                }

                int contentX = col + Viewport.X;
                MarkdownTableGlyph? tableGlyph = line.TableView?.TableLayout.GetLine (line.TableRow)
                    .FirstOrDefault (glyph => contentX >= glyph.Position.X && contentX < glyph.Position.X + glyph.Width);

                if (tableGlyph is { })
                {
                    if (contentX != tableGlyph.Position.X)
                    {
                        continue;
                    }

                    bool selected = Enumerable.Range (tableGlyph.Position.X, tableGlyph.Width).Any (column => IsInSelection (lineIdx, column));

                    if (!selected)
                    {
                        continue;
                    }

                    SetAttribute (selAttr);
                    AddStr (col, drawRow, tableGlyph.Text);
                    col += tableGlyph.Width - 1;

                    continue;
                }

                if (!IsInSelection (lineIdx, contentX))
                {
                    continue;
                }

                Cell cell = contents [screenRow, sc];
                string grapheme = string.IsNullOrEmpty (cell.Grapheme) ? " " : cell.Grapheme;

                SetAttribute (selAttr);
                AddStr (col, drawRow, grapheme);
            }
        }

        SetClip (savedClip);
    }

    private void DrawRenderedLine (RenderedLine line, int contentRow, int drawRow)
    {
        // Thematic breaks are drawn by Line SubViews
        if (line.IsThematicBreak)
        {
            return;
        }

        // Table lines are drawn by MarkdownTable SubViews
        if (line.IsTable)
        {
            return;
        }

        // Code block lines are drawn by MarkdownCodeBlock SubViews
        if (line.IsCodeBlock)
        {
            return;
        }

        var contentX = 0;

        foreach (StyledSegment segment in line.Segments)
        {
            foreach (string grapheme in GraphemeHelper.GetGraphemes (segment.Text))
            {
                int graphemeWidth = Math.Max (grapheme.GetColumns (), 1);
                bool visible = contentX + graphemeWidth > Viewport.X && contentX < Viewport.X + Viewport.Width;

                if (!visible)
                {
                    contentX += graphemeWidth;

                    continue;
                }

                int drawCol = contentX - Viewport.X;

                if (drawCol < 0 || drawCol >= Viewport.Width)
                {
                    contentX += graphemeWidth;

                    continue;
                }

                // If this grapheme is in the active link and the view has focus, use reversed highlight
                if (HasFocus && IsActiveLinkAt (contentRow, contentX))
                {
                    Attribute linkAttr = GetAttributeForSegment (segment);
                    Attribute reversed = new (linkAttr.Background, linkAttr.Foreground, linkAttr.Style);
                    bool isSafeAbsoluteLink = MarkdownAttributeHelper.TryCreateSafeAbsoluteUri (segment.Url, out _);

                    if (isSafeAbsoluteLink && Driver is { })
                    {
                        Driver.CurrentUrl = segment.Url;

                        try
                        {
                            SetAttribute (reversed);
                            AddStr (drawCol, drawRow, grapheme);
                        }
                        finally
                        {
                            Driver.CurrentUrl = null;
                        }
                    }
                    else
                    {
                        SetAttribute (reversed);
                        AddStr (drawCol, drawRow, grapheme);
                    }
                }
                else if (IsInSelection (contentRow, contentX))
                {
                    // Use the scheme's Focus attribute for selection highlight — it provides
                    // reliable contrast regardless of per-segment colours.
                    SetAttribute (GetAttributeForRole (VisualRole.Focus));
                    AddStr (drawCol, drawRow, grapheme);
                }
                else
                {
                    DrawGrapheme (segment, grapheme, drawCol, drawRow);
                }

                if (!string.IsNullOrWhiteSpace (segment.ImageSource))
                {
                    TryQueueSixel (segment.ImageSource!, new Point (drawCol, drawRow));
                }

                contentX += graphemeWidth;
            }
        }
    }

    private void DrawGrapheme (StyledSegment segment, string grapheme, int x, int y)
    {
        Attribute attr = GetAttributeForSegment (segment);

        if (MarkdownAttributeHelper.TryCreateSafeAbsoluteUri (segment.Url, out _) && Driver is { })
        {
            Driver.CurrentUrl = segment.Url;

            try
            {
                SetAttribute (attr);
                AddStr (x, y, grapheme);
            }
            finally
            {
                Driver.CurrentUrl = null;
            }

            return;
        }

        SetAttribute (attr);
        AddStr (x, y, grapheme);
    }

    private Attribute GetAttributeForSegment (StyledSegment segment) =>
        MarkdownAttributeHelper.GetAttributeForSegment (this, segment, SyntaxHighlighter, UseThemeBackground ? SyntaxHighlighter?.DefaultBackground : null);

    private void TryQueueSixel (string imageSource, Point viewPosition)
    {
        if (!EnableSixelImages || Driver is null)
        {
            return;
        }

        if (!MarkdownImageResolver.TryGetSixelData (ImageLoader, imageSource, out string sixelData))
        {
            return;
        }

        var queueId = $"{imageSource}:{viewPosition.X}:{viewPosition.Y}";

        if (!_visibleSixelIds.Add (queueId))
        {
            return;
        }

        if (_sixelRenderMap.TryGetValue (queueId, out SixelToRender? render))
        {
            render.IsDirty = true;

            return;
        }

        var newRender = new SixelToRender
        {
            Id = queueId,
            ScreenPosition = ContentToScreen (viewPosition),
            SixelData = sixelData,
            AlwaysRender = false,
            IsDirty = true
        };
        _sixelRenderMap [queueId] = newRender;
        Driver.GetSixels ().Enqueue (newRender);
    }
}
