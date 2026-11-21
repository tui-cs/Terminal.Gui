namespace Terminal.Gui.Views;

/// <summary>Allocates table tracks and produces all display and interaction geometry in one pass.</summary>
internal static class MarkdownTableLayoutEngine
{
    public static MarkdownTableLayout Build (MarkdownTableContent content, int width)
    {
        TableData data = content.Data;
        int columns = data.ColumnCount;
        int rows = content.TableRows.Count;

        if (columns == 0 || rows == 0)
        {
            return new ();
        }

        int [] paddedWidths = AllocateWidths (content, width);
        int [] edges = new int [columns + 1];

        for (int column = 0; column < columns; column++)
        {
            edges [column + 1] = checked(edges [column] + paddedWidths [column] + 1);
        }

        bool [] separators = new bool [rows + 1];
        separators [0] = true;
        separators [rows] = true;

        for (int row = 1; row < rows; row++)
        {
            separators [row] = data.HasRowSeparators || content.TableRows [row - 1].IsHeader != content.TableRows [row].IsHeader;
        }

        Dictionary<MarkdownTableCellContent, List<List<StyledSegment>>> wrapped = [];
        int [] heights = [.. Enumerable.Repeat (1, rows)];

        foreach (MarkdownTableCellContent cell in content.Cells)
        {
            TableCellData definition = cell.Definition;
            int innerWidth = edges [definition.ColumnIndex + definition.ColumnSpan] - edges [definition.ColumnIndex] - 3;
            List<List<StyledSegment>> lines = LayoutBlocks (cell.Blocks, innerWidth);
            wrapped.Add (cell, lines);

            if (definition.RowSpan != 1)
            {
                continue;
            }

            heights [cell.Row] = Math.Max (heights [cell.Row], lines.Count);
        }

        foreach (MarkdownTableCellContent cell in content.Cells.OrderBy (cell => cell.Definition.RowSpan).ThenBy (cell => cell.Row).ThenBy (cell => cell.Definition.ColumnIndex))
        {
            int end = cell.Row + cell.Definition.RowSpan;
            int available = Enumerable.Range (cell.Row, cell.Definition.RowSpan).Sum (row => heights [row]);
            available += Enumerable.Range (cell.Row + 1, cell.Definition.RowSpan - 1).Count (row => separators [row]);
            DistributeDeficit (heights, cell.Row, end, wrapped [cell].Count - available);
        }

        int [] starts = new int [rows];
        int [] ends = new int [rows];
        int cursor = 0;

        for (int row = 0; row < rows; row++)
        {
            cursor += separators [row] ? 1 : 0;
            starts [row] = cursor;
            cursor += heights [row];
            ends [row] = cursor;
        }

        bool headerOnlyEnd = !data.HasRowSeparators && content.TableRows [^1].IsHeader;
        int totalHeight = cursor + (headerOnlyEnd ? 2 : 1);
        int totalWidth = Math.Max (Math.Max (0, width), edges [columns] + 1);
        List<MarkdownTableCellLayout> cellLayouts = [];
        List<MarkdownTableGlyph> glyphs = [];
        List<MarkdownTableLink> links = [];

        foreach (MarkdownTableCellContent cell in content.Cells.OrderBy (cell => cell.Row).ThenBy (cell => cell.Definition.ColumnIndex))
        {
            TableCellData definition = cell.Definition;
            Rectangle bounds = new (edges [definition.ColumnIndex] + 1, starts [cell.Row],
                                    edges [definition.ColumnIndex + definition.ColumnSpan] - edges [definition.ColumnIndex] - 1,
                                    ends [cell.Row + definition.RowSpan - 1] - starts [cell.Row]);
            Rectangle inner = new (bounds.X + 1, bounds.Y, bounds.Width - 2, bounds.Height);
            int cellIndex = cellLayouts.Count;
            cellLayouts.Add (new (cell, bounds, inner));
            Dictionary<(int Identity, string Url), int> cellLinks = [];
            int fallbackOccurrence = 0;
            string? previousUrl = null;
            int lineIndex = 0;

            foreach (List<StyledSegment> line in wrapped [cell])
            {
                List<StyledSegment> complete = CompleteGraphemes (line);
                int textWidth = string.Concat (complete.Select (segment => segment.Text)).GetColumns ();
                Alignment alignment = content.Columns [definition.ColumnIndex].Alignment;
                int offset = alignment switch
                {
                    Alignment.Center => Math.Max (0, (inner.Width - textWidth) / 2),
                    Alignment.End => Math.Max (0, inner.Width - textWidth),
                    _ => 0
                };
                int x = inner.X + offset;

                foreach (StyledSegment segment in complete)
                {
                    if (previousUrl != segment.Url)
                    {
                        fallbackOccurrence++;
                    }

                    previousUrl = segment.Url;
                    int link = -1;

                    if (!string.IsNullOrWhiteSpace (segment.Url))
                    {
                        (int Identity, string Url) key = (segment.LinkIdentity >= 0 ? segment.LinkIdentity : -fallbackOccurrence - 1, segment.Url);

                        if (!cellLinks.TryGetValue (key, out link))
                        {
                            link = links.Count;
                            cellLinks.Add (key, link);
                            links.Add (new (segment.Url, cellIndex, key.Identity));
                        }
                    }

                    int glyphWidth = segment.Text.GetColumns ();
                    glyphs.Add (new (segment.Text, segment, new (x, inner.Y + lineIndex), glyphWidth, content.TableRows [cell.Row].IsHeader, link));
                    x += glyphWidth;
                }

                lineIndex++;
            }
        }

        List<(Point Start, int Length, Orientation Orientation)> borders = [];
        borders.Add ((new (0, 0), totalWidth, Orientation.Horizontal));
        borders.Add ((new (0, totalHeight - 1), totalWidth, Orientation.Horizontal));
        borders.Add ((new (0, 0), totalHeight, Orientation.Vertical));
        borders.Add ((new (totalWidth - 1, 0), totalHeight, Orientation.Vertical));

        if (headerOnlyEnd)
        {
            borders.Add ((new (0, cursor), totalWidth, Orientation.Horizontal));
        }

        for (int row = 0; row < rows; row++)
        {
            for (int column = 1; column < columns; column++)
            {
                if (ReferenceEquals (content.Occupancy [row, column - 1], content.Occupancy [row, column]))
                {
                    continue;
                }

                int top = starts [row] - (separators [row] ? 1 : 0);
                int bottom = ends [row] + (separators [row + 1] ? 1 : 0);
                borders.Add ((new (edges [column], top), bottom - top, Orientation.Vertical));
            }

            if (row == 0 || !separators [row])
            {
                continue;
            }

            for (int column = 0; column < columns; column++)
            {
                if (ReferenceEquals (content.Occupancy [row - 1, column], content.Occupancy [row, column]))
                {
                    continue;
                }

                int right = column == columns - 1 ? totalWidth - 1 : edges [column + 1];
                borders.Add ((new (edges [column], starts [row] - 1), right - edges [column] + 1, Orientation.Horizontal));
            }
        }

        return new ()
        {
            ColumnWidths = paddedWidths,
            RowHeights = heights,
            Width = totalWidth,
            Height = totalHeight,
            Cells = cellLayouts,
            Glyphs = glyphs,
            GlyphsByRow = glyphs.GroupBy (glyph => glyph.Position.Y).ToDictionary (group => group.Key,
                group => (IReadOnlyList<MarkdownTableGlyph>)[.. group.OrderBy (glyph => glyph.Position.X)]),
            Links = links,
            Borders = borders
        };
    }

    internal static int [] AllocateWidths (MarkdownTableContent content, int width)
    {
        width = Math.Max (0, width);
        int columns = content.Data.ColumnCount;
        int [] hard = content.HardMinimumWidths;
        int [] minimum = content.MinimumWidths;
        int [] natural = content.MaximumWidths;

        int budget = Math.Max (hard.Sum (), width - checked(3 * columns + 1));
        bool hinted = content.Columns.Any (column => column.PreferredWidthPercentage > 0);

        if (!hinted)
        {
            int [] widths = [.. natural.Select (value => value + 2)];
            int [] floors = [.. minimum.Select ((value, index) => Math.Max (value, hard [index]) + 2)];
            MarkdownTable.CollapseWidths (widths, floors, budget + 2 * columns);

            // Preserve the existing right-to-left reduction below preferred minima.
            int excess = widths.Sum () - budget - 2 * columns;

            while (excess > 0)
            {
                bool reduced = false;

                for (int column = columns - 1; column >= 0 && excess > 0; column--)
                {
                    if (widths [column] <= hard [column] + 2)
                    {
                        continue;
                    }

                    widths [column]--;
                    excess--;
                    reduced = true;
                }

                if (!reduced)
                {
                    break;
                }
            }

            return widths;
        }

        int [] lower = minimum.Sum () <= budget ? [.. minimum] : [.. hard];
        double [] weights = [.. content.Columns.Select (column => column.PreferredWidthPercentage ?? 0)];
        double specified = weights.Sum ();
        int autoNatural = Enumerable.Range (0, columns).Where (column => weights [column] == 0).Sum (column => natural [column]);

        for (int column = 0; column < columns; column++)
        {
            if (weights [column] > 0)
            {
                continue;
            }

            weights [column] = specified < 100 ? (100 - specified) * natural [column] / Math.Max (1, autoNatural) : 0;
        }

        double largestWeight = weights.Max ();

        for (int column = 0; column < columns; column++)
        {
            weights [column] /= largestWeight;
        }

        return [.. AllocateProportions (budget, lower, weights).Select (value => value + 2)];
    }

    internal static (int [] Hard, int [] Minimum, int [] Natural) MeasureBounds (MarkdownTableContent content)
    {
        int columns = content.Data.ColumnCount;
        int [] hard = [.. Enumerable.Repeat (1, columns)];
        int [] minimum = [.. Enumerable.Repeat (1, columns)];
        int [] natural = [.. Enumerable.Repeat (1, columns)];

        foreach (MarkdownTableCellContent cell in content.Cells.OrderBy (cell => cell.Definition.ColumnSpan).ThenBy (cell => cell.Row).ThenBy (cell => cell.Definition.ColumnIndex))
        {
            int first = cell.Definition.ColumnIndex;
            int end = first + cell.Definition.ColumnSpan;
            int reclaimed = 3 * (cell.Definition.ColumnSpan - 1);
            List<StyledSegment> segments = [];
            int minimumWidth = 1;
            int naturalWidth = 1;

            foreach (IntermediateBlock block in cell.Blocks)
            {
                List<StyledSegment> runs = [.. block.Runs.Select (run => run.ToStyledSegment ())];
                int indent = Math.Max (block.Prefix.GetColumns (), block.ContinuationPrefix.GetColumns ());
                minimumWidth = Math.Max (minimumWidth, indent + MarkdownTable.MeasureLongestWord (runs));
                naturalWidth = Math.Max (naturalWidth, indent + MarkdownTable.MeasureRenderedWidth (runs));
                segments.Add (new (block.Prefix, MarkdownStyleRole.Normal));
                segments.AddRange (runs);
                segments.Add (new ("\n", MarkdownStyleRole.Normal));
            }

            string text = string.Concat (segments.Select (segment => segment.Text));
            int hardWidth = GraphemeHelper.GetGraphemes (text).Select (grapheme => grapheme.GetColumns ()).DefaultIfEmpty (1).Max ();
            Constrain (hard, Math.Max (1, hardWidth));
            Constrain (minimum, Math.Max (hardWidth, minimumWidth));
            Constrain (natural, Math.Max (hardWidth, naturalWidth));

            void Constrain (int [] bounds, int required)
            {
                int existing = Enumerable.Range (first, end - first).Sum (column => bounds [column]) + reclaimed;
                DistributeDeficit (bounds, first, end, required - existing);
            }
        }

        for (int column = 0; column < columns; column++)
        {
            minimum [column] = Math.Max (minimum [column], hard [column]);
            natural [column] = Math.Max (natural [column], minimum [column]);
        }

        return (hard, minimum, natural);
    }

    private static int [] AllocateProportions (int budget, int [] minimum, double [] weights)
    {
        int count = minimum.Length;
        double [] targets = new double [count];
        bool [] pinned = new bool [count];
        int remaining = budget;

        // Every pass pins at least one column or finishes allocation.
        for (int pass = 0; pass <= count; pass++)
        {
            double totalWeight = Enumerable.Range (0, count).Where (column => !pinned [column]).Sum (column => weights [column]);
            int free = pinned.Count (value => !value);
            int passRemaining = remaining;
            bool changed = false;

            for (int column = 0; column < count; column++)
            {
                if (pinned [column])
                {
                    continue;
                }

                targets [column] = totalWeight > 0 ? passRemaining * weights [column] / totalWeight : (double)passRemaining / Math.Max (1, free);

                if (targets [column] >= minimum [column])
                {
                    continue;
                }

                targets [column] = minimum [column];
                remaining -= minimum [column];
                pinned [column] = true;
                changed = true;
            }

            if (!changed)
            {
                break;
            }
        }

        int [] result = [.. targets.Select (target => (int)Math.Floor (target))];
        int leftover = budget - result.Sum ();

        foreach (int column in Enumerable.Range (0, count).OrderByDescending (column => targets [column] - result [column]).ThenBy (column => column).Take (leftover))
        {
            result [column]++;
        }

        return result;
    }

    private static void DistributeDeficit (int [] values, int first, int end, int deficit)
    {
        if (deficit <= 0)
        {
            return;
        }

        int count = end - first;

        for (int column = first; column < end; column++)
        {
            values [column] = checked(values [column] + deficit / count + (column - first < deficit % count ? 1 : 0));
        }
    }

    private static List<List<StyledSegment>> LayoutBlocks (IReadOnlyList<IntermediateBlock> blocks, int width)
    {
        List<List<StyledSegment>> lines = [];

        foreach (IntermediateBlock block in blocks)
        {
            List<StyledSegment> segments = [.. block.Runs.Select (run => run.ToStyledSegment ())];

            if (block.IsCodeBlock)
            {
                List<StyledSegment> line = [];
                int used = 0;

                foreach (StyledSegment glyph in CompleteGraphemes ([new (block.Prefix, MarkdownStyleRole.Normal), .. segments]))
                {
                    int columns = glyph.Text.GetColumns ();

                    if (used > 0 && used + columns > width)
                    {
                        lines.Add (line);
                        line = [];
                        used = 0;
                    }

                    line.Add (glyph);
                    used += columns;
                }

                lines.Add (line);

                continue;
            }

            int indent = Math.Max (block.Prefix.GetColumns (), block.ContinuationPrefix.GetColumns ());

            if (indent >= width)
            {
                lines.AddRange (MarkdownTable.WrapSegments ([new (block.Prefix, MarkdownStyleRole.Normal), .. CompleteGraphemes (segments)], width));

                continue;
            }

            List<List<StyledSegment>> wrapped = MarkdownTable.WrapSegments (CompleteGraphemes (segments), width - indent);

            for (int index = 0; index < wrapped.Count; index++)
            {
                string prefix = index == 0 ? block.Prefix : block.ContinuationPrefix;
                lines.Add (string.IsNullOrEmpty (prefix) ? wrapped [index] : [new (prefix, MarkdownStyleRole.Normal), .. wrapped [index]]);
            }
        }

        return lines.Count == 0 ? [ []] : lines;
    }

    internal static List<StyledSegment> CompleteGraphemes (IReadOnlyList<StyledSegment> segments)
    {
        string text = string.Concat (segments.Select (segment => segment.Text));
        List<StyledSegment> result = [];
        int index = 0;
        int segmentIndex = 0;
        int segmentEnd = segments.Count == 0 ? 0 : segments [0].Text.Length;

        foreach (string grapheme in GraphemeHelper.GetGraphemes (text))
        {
            while (segmentIndex < segments.Count - 1 && index >= segmentEnd)
            {
                segmentIndex++;
                segmentEnd += segments [segmentIndex].Text.Length;
            }

            result.Add (segments [segmentIndex].WithText (grapheme));
            index += grapheme.Length;
        }

        return result;
    }
}
