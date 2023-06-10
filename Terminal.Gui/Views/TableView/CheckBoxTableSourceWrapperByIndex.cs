using System.Collections.Generic;
using System.Linq;

namespace Terminal.Gui {
	/// <summary>
	/// Implementation of <see cref="CheckBoxTableSourceWrapperBase"/> which records toggled rows
	/// by their row number.
	/// </summary>
	public class CheckBoxTableSourceWrapperByIndex : CheckBoxTableSourceWrapperBase {

		/// <inheritdoc/>
		public CheckBoxTableSourceWrapperByIndex (TableView tableView, ITableSource toWrap) : base (tableView, toWrap)
		{
		}

		/// <summary>
		/// Gets the collection of all the checked rows in the
		/// <see cref="CheckBoxTableSourceWrapperBase.Wrapping"/> <see cref="ITableSource"/>.
		/// </summary>
		public HashSet<int> CheckedRows { get; private set; } = new HashSet<int> ();

		/// <inheritdoc/>
		protected override bool IsChecked (int row)
		{
			return CheckedRows.Contains (row);
		}

		/// <inheritdoc/>
		protected override void ToggleRows (int [] range)
		{
			// if all are ticked untick them
			if (range.All (CheckedRows.Contains)) {
				// select none
				foreach (var r in range) {
					CheckedRows.Remove (r);
				}
			} else {
				// otherwise tick all
				foreach (var r in range) {
					CheckedRows.Add (r);
				}
			}
		}

		/// <inheritdoc/>
		protected override void ToggleRow (int row)
		{
			if (CheckedRows.Contains (row)) {
				CheckedRows.Remove (row);
			} else {
				CheckedRows.Add (row);
			}
		}

		/// <inheritdoc/>
		protected override void ToggleAllRows ()
		{
			if (CheckedRows.Count == Rows) {
				// select none
				ClearAllToggles ();
			} else {
				// select all
				CheckedRows = new HashSet<int> (Enumerable.Range (0, Rows));
			}
		}

		/// <inheritdoc/>
		protected override void ClearAllToggles ()
		{
			CheckedRows.Clear ();
		}
	}
}
