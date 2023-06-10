using System;
using System.Data;
using System.Linq;
using System.Text;

namespace Terminal.Gui {

	/// <summary>
	/// <see cref="ITableSource"/> for a <see cref="TableView"/> which adds a
	/// checkbox column as an additional column in the table.
	/// </summary>
	/// <remarks>This class wraps another <see cref="ITableSource"/> and dynamically
	/// serves its rows/cols plus an extra column. Data in the wrapped source can be
	/// dynamic (change over time).</remarks>
	public abstract class CheckBoxTableSourceWrapperBase : ITableSource {

		private readonly TableView tableView;

		/// <summary>
		/// Creates a new instance of the class presenting the data in <paramref name="toWrap"/>
		/// plus an additional checkbox column.
		/// </summary>
		/// <param name="tableView">The <see cref="TableView"/> this source will be used with.
		/// This is required for event registration.</param>
		/// <param name="toWrap">The original data source of the <see cref="TableView"/> that you
		/// want to add checkboxes to.</param>
		public CheckBoxTableSourceWrapperBase (TableView tableView, ITableSource toWrap)
		{
			this.Wrapping = toWrap;
			this.tableView = tableView;

			tableView.AddKeyBinding (Key.Space, Command.ToggleChecked);

			tableView.MouseClick += TableView_MouseClick;
			tableView.CellToggled += TableView_CellToggled;
		}


		/// <summary>
		/// Gets or sets the character to use for checked entries. Defaults to <see cref="GlyphDefinitions.Checked"/>
		/// </summary>
		public Rune CheckedRune { get; set; } = CM.Glyphs.Checked;

		/// <summary>
		/// Gets or sets the character to use for UnChecked entries. Defaults to <see cref="GlyphDefinitions.UnChecked"/>
		/// </summary>
		public Rune UnCheckedRune { get; set; } = CM.Glyphs.UnChecked;

		/// <summary>
		/// Gets or sets whether to only allow a single row to be toggled at once (Radio button).
		/// </summary>
		public bool UseRadioButtons { get; set; }

		/// <summary>
		/// Gets or sets the character to use for checked entry when <see cref="UseRadioButtons"/> is true.
		/// Defaults to <see cref="GlyphDefinitions.Selected"/>
		/// </summary>
		public Rune RadioCheckedRune { get; set; } = CM.Glyphs.Selected;

		/// <summary>
		/// Gets or sets the character to use for unchecked entries when <see cref="UseRadioButtons"/> is true.
		/// Defaults to <see cref="GlyphDefinitions.UnSelected"/>
		/// </summary>
		public Rune RadioUnCheckedRune { get; set; } = CM.Glyphs.UnSelected;

		/// <summary>
		/// Gets the <see cref="ITableSource"/> that this instance is wrapping.
		/// </summary>
		public ITableSource Wrapping { get; }


		/// <inheritdoc/>
		public object this [int row, int col] {
			get {
				if (col == 0) {
					if(UseRadioButtons) {
						return IsChecked (row) ? RadioCheckedRune : RadioUnCheckedRune;
					}

					return IsChecked(row) ? CheckedRune : UnCheckedRune;
				}

				return Wrapping [row, col - 1];
			}
		}


		/// <inheritdoc/>
		public int Rows => Wrapping.Rows;

		/// <inheritdoc/>
		public int Columns => Wrapping.Columns + 1;

		/// <inheritdoc/>
		public string [] ColumnNames {
			get {
				var toReturn = Wrapping.ColumnNames.ToList ();
				toReturn.Insert (0, " ");
				return toReturn.ToArray ();
			}
		}

		private void TableView_MouseClick (object sender, MouseEventEventArgs e)
		{
			// we only care about clicks (not movements)
			if(!e.MouseEvent.Flags.HasFlag(MouseFlags.Button1Clicked)) {
				return;
			}

			var hit = tableView.ScreenToCell (e.MouseEvent.X,e.MouseEvent.Y, out int? headerIfAny);

			if(headerIfAny.HasValue && headerIfAny.Value == 0) {

				// clicking in header with radio buttons does nothing
				if(UseRadioButtons) {
					return;
				}

				// otherwise it ticks all rows
				ToggleAllRows ();
				e.Handled = true;
				tableView.SetNeedsDisplay ();
			}
			else
			if(hit.HasValue && hit.Value.X == 0) {

				if(UseRadioButtons) {

					ClearAllToggles ();
					ToggleRow (hit.Value.Y);
				} else {
					ToggleRow (hit.Value.Y);
				}

				e.Handled = true;
				tableView.SetNeedsDisplay ();
			}
		}

		private void TableView_CellToggled (object sender, CellToggledEventArgs e)
		{
			// Suppress default toggle behavior when using checkboxes
			// and instead handle ourselves
			var range = tableView.GetAllSelectedCells ().Select (c => c.Y).Distinct ().ToArray();

			if(UseRadioButtons) {
				
				// multi selection makes it unclear what to toggle in this situation
				if(range.Length != 1) {
					e.Cancel = true;
					return;
				}

				ClearAllToggles ();
				ToggleRow (range.Single ());
			}
			else {
				ToggleRows (range);
			}

			e.Cancel = true;
			tableView.SetNeedsDisplay ();
		}

		/// <summary>
		/// Returns true if <paramref name="row"/> is checked.
		/// </summary>
		/// <param name="row"></param>
		/// <returns></returns>
		protected abstract bool IsChecked (int row);

		/// <summary>
		/// Flips the checked state for a collection of rows. If
		/// some (but not all) are selected they should flip to all
		/// selected.
		/// </summary>
		/// <param name="range"></param>
		protected abstract void ToggleRows (int [] range);

		/// <summary>
		/// Flips the checked state of the given <paramref name="row"/>/
		/// </summary>
		/// <param name="row"></param>
		protected abstract void ToggleRow (int row);

		/// <summary>
		/// Called when the 'toggled all' action is performed.
		/// This should change state from 'some selected' to
		/// 'all selected' or clear selection if all area already
		/// selected.
		/// </summary>
		protected abstract void ToggleAllRows ();

		/// <summary>
		/// Clears the toggled state of all rows.
		/// </summary>
		protected abstract void ClearAllToggles ();
	}
}
