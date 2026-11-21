namespace Terminal.Gui.Views;

/// <summary>Describes an originating grid cell; covered positions do not need separate cells.</summary>
public sealed class TableCellData
{
    /// <summary>Initializes a grid cell with Markdown block source.</summary>
    /// <param name="columnIndex">Zero-based logical column.</param>
    /// <param name="markdown">Markdown block source, including paragraphs, lists, or code.</param>
    /// <param name="columnSpan">Number of covered columns.</param>
    /// <param name="rowSpan">Number of covered rows.</param>
    /// <exception cref="ArgumentOutOfRangeException">A coordinate is negative or a span is not positive.</exception>
    public TableCellData (int columnIndex, string markdown, int columnSpan = 1, int rowSpan = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegative (columnIndex);
        ArgumentOutOfRangeException.ThrowIfLessThan (columnSpan, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan (rowSpan, 1);
        ArgumentNullException.ThrowIfNull (markdown);
        ColumnIndex = columnIndex;
        Markdown = markdown;
        ColumnSpan = columnSpan;
        RowSpan = rowSpan;
    }

    /// <summary>Gets the starting column.</summary>
    public int ColumnIndex { get; }

    /// <summary>Gets the Markdown source. Legacy constructor cells retain inline-source semantics.</summary>
    public string Markdown { get; }

    /// <summary>Gets the number of covered columns.</summary>
    public int ColumnSpan { get; }

    /// <summary>Gets the number of covered rows.</summary>
    public int RowSpan { get; }
}
