using System;

namespace Terminal.Gui {
	/// <summary>
	/// Event args for the <see cref="TableView.CellToggled"/> event.
	/// </summary>
	public class CellToggledEventArgs : EventArgs
	{
		/// <summary>
		/// The current table to which the new indexes refer.  May be null e.g. if selection change is the result of clearing the table from the view
		/// </summary>
		/// <value></value>
		public ITableSource Table { get; }

		/// <summary>
		/// The column index of the <see cref="Table"/> cell that is being toggled
		/// </summary>
		/// <value></value>
		public int Col { get; }

		/// <summary>
		/// The row index of the <see cref="Table"/> cell that is being toggled
		/// </summary>
		/// <value></value>
		public int Row { get; }

		/// <summary>
		/// Gets or sets whether to cancel the processing of this event
		/// </summary>
		public bool Cancel { get; set; }

		/// <summary>
		/// Creates a new instance of arguments describing a cell being toggled in <see cref="TableView"/>
		/// </summary>
		/// <param name="t"></param>
		/// <param name="col"></param>
		/// <param name="row"></param>
		public CellToggledEventArgs (ITableSource t, int col, int row)
		{
			Table = t;
			Col = col;
			Row = row;
		}
	}
}
