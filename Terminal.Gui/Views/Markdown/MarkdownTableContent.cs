namespace Terminal.Gui.Views;

/// <summary>Retains parsed table segments and their width bounds independently of the current viewport.</summary>
internal sealed class MarkdownTableContent
{
    public MarkdownTableContent (List<StyledSegment> [] headers, List<StyledSegment> [] [] rows)
    {
        Headers = headers;
        Rows = rows;
        List<MarkdownTableCellContent> cells = [];
        AddRow (headers, 0);

        for (int row = 0; row < rows.Length; row++)
        {
            AddRow (rows [row], row + 1);
        }

        // This constructor is also used by existing sizing tests without public source data.
        Data = new (Enumerable.Range (0, headers.Length).Select (_ => new TableColumnData ()),
                    Enumerable.Range (0, rows.Length + 1).Select (row => new TableRowData (row == 0, [])), false, false, null);
        Cells = cells;
        Columns = Data.Columns;
        TableRows = Data.TableRows;
        Occupancy = BuildOccupancy (Data, cells);
        (HardMinimumWidths, MinimumWidths, MaximumWidths) = MarkdownTableLayoutEngine.MeasureBounds (this);

        return;

        void AddRow (List<StyledSegment> [] segments, int row)
        {
            for (int column = 0; column < headers.Length; column++)
            {
                List<StyledSegment> cell = column < segments.Length ? segments [column] : [];
                cells.Add (new (row, new (column, string.Empty), [new ([.. cell.Select (InlineRun.FromStyledSegment)], true)]));
            }
        }
    }

    public MarkdownTableContent (TableData data, IReadOnlyList<MarkdownTableCellContent> cells)
    {
        Data = data;
        Columns = data.Columns;
        TableRows = data.TableRows;
        List<MarkdownTableCellContent> normalized = [.. cells];
        Occupancy = BuildOccupancy (data, normalized);

        for (int row = 0; row < TableRows.Count; row++)
        {
            for (int column = 0; column < data.ColumnCount; column++)
            {
                if (Occupancy [row, column] is { })
                {
                    continue;
                }

                MarkdownTableCellContent empty = new (row, new (column, string.Empty), []);
                normalized.Add (empty);
                Occupancy [row, column] = empty;
            }
        }

        Cells = normalized;
        int header = Enumerable.Range (0, TableRows.Count).FirstOrDefault (row => TableRows [row].IsHeader, -1);
        Headers = header >= 0 ? ProjectRow (header) : [];
        Rows = [.. Enumerable.Range (0, TableRows.Count).Where (row => row != header).Select (ProjectRow)];
        (HardMinimumWidths, MinimumWidths, MaximumWidths) = MarkdownTableLayoutEngine.MeasureBounds (this);

        return;

        List<StyledSegment> [] ProjectRow (int row) => [.. Enumerable.Range (0, data.ColumnCount)
                                                                 .Select (column => Occupancy [row, column] is { } cell
                                                                                   && cell.Row == row && cell.Definition.ColumnIndex == column ? cell.Segments : [])];
    }

    private static MarkdownTableCellContent? [,] BuildOccupancy (TableData data, IReadOnlyList<MarkdownTableCellContent> cells)
    {
        MarkdownTableCellContent? [,] occupancy = new MarkdownTableCellContent? [data.TableRows.Count, data.ColumnCount];

        foreach (MarkdownTableCellContent cell in cells)
        {
            for (int row = cell.Row; row < cell.Row + cell.Definition.RowSpan; row++)
            {
                for (int column = cell.Definition.ColumnIndex; column < cell.Definition.ColumnIndex + cell.Definition.ColumnSpan; column++)
                {
                    occupancy [row, column] = cell;
                }
            }
        }

        return occupancy;
    }

    public TableData Data { get; }
    public IReadOnlyList<TableColumnData> Columns { get; }
    public IReadOnlyList<TableRowData> TableRows { get; }
    public IReadOnlyList<MarkdownTableCellContent> Cells { get; }
    public MarkdownTableCellContent? [,] Occupancy { get; }

    public List<StyledSegment> [] Headers { get; }
    public List<StyledSegment> [] [] Rows { get; }
    public int [] HardMinimumWidths { get; }
    public int [] MaximumWidths { get; }
    public int [] MinimumWidths { get; }

    private ISyntaxHighlighter? _highlightedWith;
    private MarkdownTableContent? _highlightedContent;

    public MarkdownTableContent WithHighlighter (ISyntaxHighlighter? highlighter)
    {
        if (highlighter is null)
        {
            return this;
        }

        // The owning parsed block retains this content across width-only rebuilds.
        if (ReferenceEquals (_highlightedWith, highlighter) && _highlightedContent is { } cached)
        {
            return cached;
        }

        if (!Cells.Any (cell => cell.Blocks.Any (block => block.IsCodeBlock)))
        {
            return this;
        }

        List<MarkdownTableCellContent> highlighted = [];

        foreach (MarkdownTableCellContent cell in Cells)
        {
            List<IntermediateBlock> blocks = [];
            bool inCode = false;

            foreach (IntermediateBlock block in cell.Blocks)
            {
                if (!block.IsCodeBlock)
                {
                    inCode = false;
                    blocks.Add (block);

                    continue;
                }

                if (!inCode)
                {
                    highlighter.ResetState ();
                }

                inCode = true;
                string text = string.Concat (block.Runs.Select (run => run.Text));
                blocks.Add (new ([.. highlighter.Highlight (text, block.Language).Select (InlineRun.FromStyledSegment)], false,
                                 block.Prefix, block.ContinuationPrefix, isCodeBlock: true, language: block.Language));
            }

            highlighted.Add (new (cell.Row, cell.Definition, blocks));
        }

        _highlightedWith = highlighter;
        _highlightedContent = new (Data, highlighted);

        return _highlightedContent;
    }
}
