using System;
using System.Data;

namespace Terminal.Gui {

	/// <summary>
	/// Defines the event arguments for <see cref="TableView.SelectedCellChanged"/> 
	/// </summary>
	public class SelectedCellChangedEventArgs : EventArgs {
		/// <summary>
		/// The current table to which the new indexes refer.  May be null e.g. if selection change is the result of clearing the table from the view
		/// </summary>
		/// <value></value>
		public DataTable Table { get; }

		/// <summary>
		/// The previous selected column index.  May be invalid e.g. when the selection has been changed as a result of replacing the existing Table with a smaller one
		/// </summary>
		/// <value></value>
		public int OldCol { get; }

		/// <summary>
		/// The newly selected column index.
		/// </summary>
		/// <value></value>
		public int NewCol { get; }

		/// <summary>
		/// The previous selected row index.  May be invalid e.g. when the selection has been changed as a result of deleting rows from the table
		/// </summary>
		/// <value></value>
		public int OldRow { get; }

		/// <summary>
		/// The newly selected row index.
		/// </summary>
		/// <value></value>
		public int NewRow { get; }

		/// <summary>
		/// Creates a new instance of arguments describing a change in selected cell in a <see cref="TableView"/>
		/// </summary>
		/// <param name="t"></param>
		/// <param name="oldCol"></param>
		/// <param name="newCol"></param>
		/// <param name="oldRow"></param>
		/// <param name="newRow"></param>
		public SelectedCellChangedEventArgs (DataTable t, int oldCol, int newCol, int oldRow, int newRow)
		{
			Table = t;
			OldCol = oldCol;
			NewCol = newCol;
			OldRow = oldRow;
			NewRow = newRow;
		}
	}

}
