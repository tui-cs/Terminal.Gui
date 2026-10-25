namespace Terminal.Gui.Views;

/// <summary>Describes a logical table row and its originating cells.</summary>
public sealed class TableRowData
{
    /// <summary>Initializes a row, taking a snapshot of its cell collection.</summary>
    /// <param name="isHeader">Whether this row receives header styling.</param>
    /// <param name="cells">Cells beginning in this row, excluding positions covered by earlier spans.</param>
    public TableRowData (bool isHeader, IEnumerable<TableCellData> cells)
    {
        ArgumentNullException.ThrowIfNull (cells);
        TableCellData [] snapshot = [.. cells];
        if (snapshot.Any (cell => cell is null))
        {
            throw new ArgumentException ("Cells cannot contain null entries.", nameof (cells));
        }

        IsHeader = isHeader;
        Cells = Array.AsReadOnly (snapshot);
    }

    /// <summary>Gets whether the row is an explicit header.</summary>
    public bool IsHeader { get; }

    /// <summary>Gets the originating cells in source order.</summary>
    public IReadOnlyList<TableCellData> Cells { get; }
}
