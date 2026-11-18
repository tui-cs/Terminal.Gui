#nullable enable
namespace Terminal.Gui.Views;

/// <summary>
///     Arguments for <see cref="RowColorGetterDelegate"/>. Describes a row of data in a <see cref="ITableSource"/>
///     for which <see cref="Scheme"/> is sought.
/// </summary>
public class RowColorGetterArgs
{
    internal RowColorGetterArgs (ITableSource table, int rowIdx)
    {
        Table = table;
        RowIndex = rowIdx;
    }

    /// <summary>The index of the row in <see cref="Table"/> for which color is needed</summary>
    public int RowIndex { get; }

    /// <summary>The data table hosted by the <see cref="TableView"/> control.</summary>
    public ITableSource Table { get; }
}
