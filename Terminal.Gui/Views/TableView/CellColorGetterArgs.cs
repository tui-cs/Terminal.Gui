namespace Terminal.Gui; 

/// <summary>
/// Arguments for a <see cref="CellColorGetterDelegate"/>.  Describes a cell for which a rendering
/// <see cref="ColorScheme"/> is being sought
/// </summary>
public class CellColorGetterArgs {

	/// <summary>
	/// The data table hosted by the <see cref="TableView"/> control.
	/// </summary>
	public ITableSource Table { get; }

	/// <summary>
	/// The index of the row in <see cref="Table"/> for which color is needed
	/// </summary>
	public int RowIndex { get; }

	/// <summary>
	/// The index of column in <see cref="Table"/> for which color is needed
	/// </summary>
	public int ColIdex { get; }

	/// <summary>
	/// The hard typed value being rendered in the cell for which color is needed
	/// </summary>
	public object CellValue { get; }

	/// <summary>
	/// The textual representation of <see cref="CellValue"/> (what will actually be drawn to the screen)
	/// </summary>
	public string Representation { get; }

	/// <summary>
	/// the color scheme that is going to be used to render the cell if no cell specific color scheme is returned
	/// </summary>
	public ColorScheme RowScheme { get; }

	internal CellColorGetterArgs (ITableSource table, int rowIdx, int colIdx, object cellValue, string representation, ColorScheme rowScheme)
	{
		Table = table;
		RowIndex = rowIdx;
		ColIdex = colIdx;
		CellValue = cellValue;
		Representation = representation;
		RowScheme = rowScheme;
	}

}