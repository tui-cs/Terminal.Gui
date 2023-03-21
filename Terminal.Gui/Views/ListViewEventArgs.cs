using System;

namespace Terminal.Gui {
	/// <summary>
	/// <see cref="EventArgs"/> for <see cref="ListView"/> events.
	/// </summary>
	public class ListViewItemEventArgs : EventArgs {
		/// <summary>
		/// The index of the <see cref="ListView"/> item.
		/// </summary>
		public int Item { get; }
		/// <summary>
		/// The <see cref="ListView"/> item.
		/// </summary>
		public object Value { get; }

		/// <summary>
		/// Initializes a new instance of <see cref="ListViewItemEventArgs"/>
		/// </summary>
		/// <param name="item">The index of the <see cref="ListView"/> item.</param>
		/// <param name="value">The <see cref="ListView"/> item</param>
		public ListViewItemEventArgs (int item, object value)
		{
			Item = item;
			Value = value;
		}
	}

	/// <summary>
	/// <see cref="EventArgs"/> used by the <see cref="ListView.RowRender"/> event.
	/// </summary>
	public class ListViewRowEventArgs : EventArgs {
		/// <summary>
		/// The current row being rendered.
		/// </summary>
		public int Row { get; }
		/// <summary>
		/// The <see cref="Attribute"/> used by current row or
		/// null to maintain the current attribute.
		/// </summary>
		public Attribute? RowAttribute { get; set; }

		/// <summary>
		/// Initializes with the current row.
		/// </summary>
		/// <param name="row"></param>
		public ListViewRowEventArgs (int row)
		{
			Row = row;
		}
	}
}
