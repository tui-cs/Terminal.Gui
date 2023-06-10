using System.Data;
using System.Linq;

namespace Terminal.Gui {
	/// <summary>
	/// <see cref="ITableSource"/> implementation that wraps 
	/// a <see cref="System.Data.DataTable"/>.  This class is
	/// mutable: changes are permitted to the wrapped <see cref="System.Data.DataTable"/>.
	/// </summary>
	public class DataTableSource : ITableSource
	{
		/// <summary>
		/// The data table this source wraps.
		/// </summary>
		public DataTable DataTable { get; private set; }

		/// <summary>
		/// Creates a new instance based on the data in <paramref name="table"/>.
		/// </summary>
		/// <param name="table"></param>
		public DataTableSource(DataTable table)
		{
			this.DataTable = table;
		}

		/// <inheritdoc/>
		public object this [int row, int col] => DataTable.Rows[row][col];

		/// <inheritdoc/>
		public int Rows => DataTable.Rows.Count;

		/// <inheritdoc/>
		public int Columns => DataTable.Columns.Count;

		/// <inheritdoc/>
		public string [] ColumnNames => DataTable.Columns.Cast<DataColumn>().Select (c => c.ColumnName).ToArray ();
	}
}
