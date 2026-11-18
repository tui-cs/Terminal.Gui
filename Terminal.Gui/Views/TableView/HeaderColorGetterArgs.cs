#nullable enable

namespace Terminal.Gui.Views;

/// <summary>
///     Arguments for a <see cref="HeaderColorGetterDelegate"/>. Describes a column header for which a rendering
///     <see cref="Scheme"/> is being sought.
/// </summary>
public class HeaderColorGetterArgs
{
    internal HeaderColorGetterArgs (ITableSource table, int column, string columnName, Scheme rowScheme)
    {
        Table = table;
        Column = column;
        ColumnName = columnName;
        RowScheme = rowScheme;
    }

    /// <summary>The index of the column in <see cref="Table"/> for which header color is needed.</summary>
    public int Column { get; }

    /// <summary>The name of the column header being rendered.</summary>
    public string ColumnName { get; }

    /// <summary>The default scheme that would be used if no override is provided.</summary>
    public Scheme RowScheme { get; }

    /// <summary>The data table hosted by the <see cref="TableView"/> control.</summary>
    public ITableSource Table { get; }
}
