using System;
using System.Linq;

namespace Terminal.Gui {
	/// <summary>
	/// Implementation of <see cref="CheckBoxTableSourceWrapperBase"/> which records toggled rows
	/// by a property on row objects.
	/// </summary>
	public class CheckBoxTableSourceWrapperByObject<T> : CheckBoxTableSourceWrapperBase {
		private readonly EnumerableTableSource<T> toWrap;
		readonly Func<T, bool> getter;
		readonly Action<T, bool> setter;

		/// <summary>
		/// Creates a new instance of the class wrapping the collection <see cref="toWrap"/>.
		/// </summary>
		/// <param name="tableView">The table you will use the source with.</param>
		/// <param name="toWrap">The collection of objects you will record checked state for</param>
		/// <param name="getter">Delegate method for retrieving checked state from your objects of type <typeparamref name="T"/>.</param>
		/// <param name="setter">Delegate method for setting new checked states on your objects of type <typeparamref name="T"/>.</param>
		public CheckBoxTableSourceWrapperByObject (
			TableView tableView,
			EnumerableTableSource<T> toWrap,
			Func<T,bool> getter,
			Action<T,bool> setter) : base (tableView, toWrap)
		{
			this.toWrap = toWrap;
			this.getter = getter;
			this.setter = setter;
		}

		/// <inheritdoc/>
		protected override bool IsChecked (int row)
		{
			return getter (toWrap.Data.ElementAt (row));
		}

		/// <inheritdoc/>
		protected override void ToggleAllRows ()
		{
			ToggleRows (Enumerable.Range (0, toWrap.Rows).ToArray());
		}

		/// <inheritdoc/>
		protected override void ToggleRow (int row)
		{
			var d = toWrap.Data.ElementAt (row);
			setter (d, !getter(d));
		}
		
		/// <inheritdoc/>
		protected override void ToggleRows (int [] range)
		{
			// if all are ticked untick them
			if (range.All (IsChecked)) {
				// select none
				foreach(var r in range) {
					setter (toWrap.Data.ElementAt (r), false);
				}
			} else {
				// otherwise tick all
				foreach (var r in range) {
					setter (toWrap.Data.ElementAt (r), true);
				}
			}
		}

		/// <inheritdoc/>
		protected override void ClearAllToggles ()
		{
			foreach (var e in toWrap.Data) {
				setter (e, false);
			}
		}
	}
}
