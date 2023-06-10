using System;
using System.Collections;
using System.Data;
using System.Linq;

namespace Terminal.Gui {
	/// <summary>
	/// <see cref="ITableSource"/> implementation that wraps 
	/// a <see cref="System.Collections.IList"/>.  This class is
	/// mutable: changes are permitted to the wrapped <see cref="IList"/>.
	/// </summary>
	public class ListTableSource : ITableSource {
		/// <summary>
		/// The list this source wraps.
		/// </summary>
		public IList List;

		/// <summary>
		/// The style this source uses.
		/// </summary>
		public ListColumnStyle Style;

		/// <summary>
		/// The data table this source wraps.
		/// </summary>
		public DataTable DataTable { get; private set; }

		private TableView _tableView;

		private Rect _lastBounds;
		private int _lastMaxCellWidth;
		private int _lastMinCellWidth;
		private ListColumnStyle _lastStyle;
		private IList _lastList;

		/// <summary>
		/// Creates a new columned list table instance based on the data in <paramref name="list"/>
		/// and dimensions from <paramref name="tableView"/>.
		/// </summary>
		/// <param name="list"></param>
		/// <param name="tableView"></param>
		/// <param name="style"></param>
		public ListTableSource (IList list, TableView tableView, ListColumnStyle style)
		{
			this.List = list;
			this._tableView = tableView;
			Style = style;

			this.DataTable = CreateTable (CalculateColumns ());

			// TODO: Determine the best event for this
			tableView.DrawContent += TableView_DrawContent;
		}

		/// <inheritdoc/>
		public ListTableSource (IList list, TableView tableView) : this (list, tableView, new ListColumnStyle ()) { }

		private void TableView_DrawContent (object sender, DrawEventArgs e)
		{
			if ((!_tableView.Bounds.Equals (_lastBounds)) ||
				_tableView.MaxCellWidth != _lastMaxCellWidth ||
				_tableView.MinCellWidth != _lastMinCellWidth ||
				Style != _lastStyle ||
				this.List != _lastList) {

				this.DataTable = CreateTable (CalculateColumns ());
			}
			_lastBounds = _tableView.Bounds;
			_lastMinCellWidth = _tableView.MaxCellWidth;
			_lastMaxCellWidth = _tableView.MaxCellWidth;
			_lastStyle = Style;
			_lastList = this.List;
		}

		/// <inheritdoc/>
		public object this [int row, int col] {
			get {
				int idx;
				if (Style.Orientation == Orientation.Vertical) {
					idx = (col * Rows) + row;
				} else {
					idx = (row * Columns) + col;
				}
				if (idx < 0 || idx >= Count) {
					return null;
				}
				return this.List [idx];
			}
		}

		/// <summary>
		/// The number of items in the IList source
		/// </summary>
		public int Count => this.List.Count;

		/// <inheritdoc/>
		public int Rows => this.DataTable.Rows.Count;

		/// <inheritdoc/>
		public int Columns => this.DataTable.Columns.Count;

		/// <inheritdoc/>
		public string [] ColumnNames => Enumerable.Range (0, Columns).Select (n => n.ToString ()).ToArray ();

		/// <summary>
		/// Creates a DataTable from an IList to display in a <see cref="TableView"/>
		/// </summary>
		private DataTable CreateTable (int cols = 1)
		{
			var table = new DataTable ();
			for (int col = 0; col < cols; col++) {
				table.Columns.Add (new DataColumn (col.ToString ()));
			}
			for (int row = 0; row < (Count / table.Columns.Count); row++) {
				table.Rows.Add ();
			}
			// return partial row
			if (Count % table.Columns.Count != 0) {
				table.Rows.Add ();
			}

			return table;
		}

		/// <summary>
		/// Returns the size in characters of the longest value read from <see cref="ListTableSource.List"/>
		/// </summary>
		/// <returns></returns>
		private int CalculateMaxLength ()
		{
			if (List == null || Count == 0) {
				return 0;
			}

			int maxLength = 0;
			foreach (var t in List) {
				int l;
				if (t is string s) {
					l = s.GetColumns ();
				} else {
					l = t.ToString ().Length;
				}

				if (l > maxLength) {
					maxLength = l;
				}
			}

			return maxLength;
		}

		private int CalculateColumns ()
		{
			int cols;

			int colWidth = CalculateMaxLength ();
			if (colWidth > _tableView.MaxCellWidth) {
				colWidth = _tableView.MaxCellWidth;
			}

			if (_tableView.MinCellWidth > 0 && colWidth < _tableView.MinCellWidth) {
				if (_tableView.MinCellWidth > _tableView.MaxCellWidth) {
					colWidth = _tableView.MaxCellWidth;
				} else {
					colWidth = _tableView.MinCellWidth;
				}
			}
			if ((Style.Orientation == Orientation.Vertical) != Style.ScrollParallel) {
				float f = (float)_tableView.Bounds.Height - _tableView.GetHeaderHeight ();
				cols = (int)Math.Ceiling (Count / f);
			} else {
				cols = ((int)Math.Ceiling (((float)_tableView.Bounds.Width - 1) / colWidth)) - 2;
			}

			return (cols > 1) ? cols : 1;
		}
	}
}
