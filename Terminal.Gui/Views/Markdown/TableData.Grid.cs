namespace Terminal.Gui.Views;

public sealed partial class TableData
{
    private readonly IReadOnlyList<TableColumnData>? _columns;

    /// <summary>Gets the complete column descriptions. Legacy array edits are read on reassignment to a table.</summary>
    public IReadOnlyList<TableColumnData> Columns => _columns ?? Array.AsReadOnly<TableColumnData> ([.. Enumerable.Range (0, ColumnCount)
        .Select (index => new TableColumnData (index < ColumnAlignments.Length ? ColumnAlignments [index] : Alignment.Start))]);

    private readonly IReadOnlyList<TableRowData>? _tableRows;

    /// <summary>Gets every logical row, including all headers and cell spans.</summary>
    /// <remarks>For structured tables, <see cref="Headers"/> and <see cref="Rows"/> are lossy compatibility projections.</remarks>
    public IReadOnlyList<TableRowData> TableRows => _tableRows ?? Array.AsReadOnly<TableRowData> ([CreateLegacyRow (Headers, true),
        .. Rows.Select (row => CreateLegacyRow (row, false))]);

    internal bool HasRowSeparators { get; }
    internal bool IsStructured => _tableRows is { };
    internal bool CellSourcesAreBlocks { get; }
    internal string? OriginalSource { get; }

    /// <summary>Creates a grid table with explicit headers, spans, and Markdown block cells.</summary>
    /// <param name="columns">Logical column definitions. The collection is copied.</param>
    /// <param name="rows">Logical rows, including rows occupied entirely by spans. The collection is copied.</param>
    /// <returns>A validated grid table. Unoccupied positions render as empty cells.</returns>
    /// <exception cref="ArgumentException">Cells overlap or extend outside the supplied grid.</exception>
    /// <remarks>
    ///     To render rich cells, assign the result to <see cref="MarkdownTable.TableData"/>.
    ///     Existing string-array constructor cells continue to use inline source.
    ///     See <a href="../docs/markdown-tables.md">Markdown tables</a> for sizing and clipboard rules.
    /// </remarks>
    /// <example>
    /// <code>
    /// TableData data = TableData.CreateGrid ([new (), new ()],
    ///     [new (false, [new (0, "A spanning cell", columnSpan: 2)])]);
    /// </code>
    /// </example>
    public static TableData CreateGrid (IEnumerable<TableColumnData> columns, IEnumerable<TableRowData> rows) =>
        new (columns, rows, true, true, null);

    internal TableData (IEnumerable<TableColumnData> columns, IEnumerable<TableRowData> rows, bool rowSeparators, bool blockSources, string? originalSource)
    {
        ArgumentNullException.ThrowIfNull (columns);
        ArgumentNullException.ThrowIfNull (rows);
        TableColumnData [] columnSnapshot = [.. columns];
        TableRowData [] rowSnapshot = [.. rows];

        if (columnSnapshot.Any (column => column is null) || rowSnapshot.Any (row => row is null))
        {
            throw new ArgumentException ("Table descriptions cannot contain null entries.");
        }

        HashSet<(int Row, int Column)> occupied = [];

        for (int row = 0; row < rowSnapshot.Length; row++)
        {
            foreach (TableCellData cell in rowSnapshot [row].Cells)
            {
                if (cell.ColumnIndex >= columnSnapshot.Length
                    || cell.ColumnSpan > columnSnapshot.Length - cell.ColumnIndex
                    || cell.RowSpan > rowSnapshot.Length - row)
                {
                    throw new ArgumentException ("A cell extends outside the table grid.", nameof (rows));
                }

                for (int y = row; y < row + cell.RowSpan; y++)
                {
                    for (int x = cell.ColumnIndex; x < cell.ColumnIndex + cell.ColumnSpan; x++)
                    {
                        if (!occupied.Add ((y, x)))
                        {
                            throw new ArgumentException ("Table cells overlap.", nameof (rows));
                        }
                    }
                }
            }
        }

        _columns = Array.AsReadOnly (columnSnapshot);
        _tableRows = Array.AsReadOnly (rowSnapshot);
        ColumnCount = columnSnapshot.Length;
        ColumnAlignments = [.. columnSnapshot.Select (column => column.Alignment)];
        HasRowSeparators = rowSeparators;
        CellSourcesAreBlocks = blockSources;
        OriginalSource = originalSource;
        int header = Array.FindIndex (rowSnapshot, row => row.IsHeader);
        Headers = header >= 0 ? ProjectRow (rowSnapshot [header]) : [];
        Rows = [.. rowSnapshot.Where ((_, index) => index != header).Select (ProjectRow)];

        return;

        string [] ProjectRow (TableRowData row)
        {
            string [] result = [.. Enumerable.Repeat (string.Empty, ColumnCount)];

            foreach (TableCellData cell in row.Cells)
            {
                result [cell.ColumnIndex] = cell.Markdown;
            }

            return result;
        }
    }

    private TableRowData CreateLegacyRow (string [] cells, bool isHeader) =>
        new (isHeader, cells.Take (ColumnCount).Select ((source, index) => new TableCellData (index, source)));
}
