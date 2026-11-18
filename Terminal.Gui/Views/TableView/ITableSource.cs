#nullable enable
namespace Terminal.Gui.Views;

/// <summary>Tabular matrix of data to be displayed in a <see cref="TableView"/>.</summary>
public interface ITableSource
{
    /// <summary>Gets the label for each column.</summary>
    string [] ColumnNames { get; }

    /// <summary>Gets the number of columns in the table.</summary>
    int Columns { get; }

    /// <summary>Returns the data at the given indexes of the table (row, column).</summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    object this [int row, int col] { get; }

    /// <summary>Gets the number of rows in the table.</summary>
    int Rows { get; }
}
