namespace Terminal.Gui; 

/// <summary>
/// Arguments for <see cref="RowColorGetterDelegate"/>. Describes a row of data in a <see cref="ITableSource"/>
/// for which <see cref="ColorScheme"/> is sought.
/// </summary>
public class RowColorGetterArgs {

	/// <summary>
	/// The data table hosted by the <see cref="TableView"/> control.
	/// </summary>
	public ITableSource Table { get; }

	/// <summary>
	/// The index of the row in <see cref="Table"/> for which color is needed
	/// </summary>
	public int RowIndex { get; }

	internal RowColorGetterArgs (ITableSource table, int rowIdx)
	{
		Table = table;
		RowIndex = rowIdx;
	}
}