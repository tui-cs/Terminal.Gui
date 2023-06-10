using System.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Terminal.Gui {

	/// <summary>
	/// Delegate for providing color to <see cref="TableView"/> cells based on the value being rendered
	/// </summary>
	/// <param name="args">Contains information about the cell for which color is needed</param>
	/// <returns></returns>
	public delegate ColorScheme CellColorGetterDelegate (CellColorGetterArgs args);

	/// <summary>
	/// Delegate for providing color for a whole row of a <see cref="TableView"/>
	/// </summary>
	/// <param name="args"></param>
	/// <returns></returns>
	public delegate ColorScheme RowColorGetterDelegate (RowColorGetterArgs args);

	/// <summary>
	/// View for tabular data based on a <see cref="ITableSource"/>.
	/// 
	/// <a href="https://gui-cs.github.io/Terminal.Gui/articles/tableview.html">See TableView Deep Dive for more information</a>.
	/// </summary>
	public class TableView : View {

		private int columnOffset;
		private int rowOffset;
		private int selectedRow;
		private int selectedColumn;
		private ITableSource table;
		private TableStyle style = new TableStyle ();
		private Key cellActivationKey = Key.Enter;

		Point? scrollLeftPoint;
		Point? scrollRightPoint;

		/// <summary>
		/// The default maximum cell width for <see cref="TableView.MaxCellWidth"/> and <see cref="ColumnStyle.MaxWidth"/>
		/// </summary>
		public const int DefaultMaxCellWidth = 100;

		/// <summary>
		/// The default minimum cell width for <see cref="ColumnStyle.MinAcceptableWidth"/>
		/// </summary>
		public const int DefaultMinAcceptableWidth = 100;

		/// <summary>
		/// The data table to render in the view.  Setting this property automatically updates and redraws the control.
		/// </summary>
		public ITableSource Table {
			get => table;
			set {
				table = value;
				Update ();
			}
		}

		/// <summary>
		/// Contains options for changing how the table is rendered
		/// </summary>
		public TableStyle Style { get => style; set { style = value; Update (); } }

		/// <summary>
		/// True to select the entire row at once.  False to select individual cells.  Defaults to false
		/// </summary>
		public bool FullRowSelect { get; set; }

		/// <summary>
		/// True to allow regions to be selected 
		/// </summary>
		/// <value></value>
		public bool MultiSelect { get; set; } = true;

		/// <summary>
		/// When <see cref="MultiSelect"/> is enabled this property contain all rectangles of selected cells.  Rectangles describe column/rows selected in <see cref="Table"/> (not screen coordinates)
		/// </summary>
		/// <returns></returns>
		public Stack<TableSelection> MultiSelectedRegions { get; private set; } = new Stack<TableSelection> ();

		/// <summary>
		/// Horizontal scroll offset.  The index of the first column in <see cref="Table"/> to display when when rendering the view.
		/// </summary>
		/// <remarks>This property allows very wide tables to be rendered with horizontal scrolling</remarks>
		public int ColumnOffset {
			get => columnOffset;

			//try to prevent this being set to an out of bounds column
			set => columnOffset = TableIsNullOrInvisible () ? 0 : Math.Max (0, Math.Min (Table.Columns - 1, value));
		}

		/// <summary>
		/// Vertical scroll offset.  The index of the first row in <see cref="Table"/> to display in the first non header line of the control when rendering the view.
		/// </summary>
		public int RowOffset {
			get => rowOffset;
			set => rowOffset = TableIsNullOrInvisible () ? 0 : Math.Max (0, Math.Min (Table.Rows - 1, value));
		}

		/// <summary>
		/// The index of <see cref="DataTable.Columns"/> in <see cref="Table"/> that the user has currently selected
		/// </summary>
		public int SelectedColumn {
			get => selectedColumn;

			set {
				var oldValue = selectedColumn;

				//try to prevent this being set to an out of bounds column
				selectedColumn = TableIsNullOrInvisible () ? 0 : Math.Min (Table.Columns - 1, Math.Max (0, value));

				if (oldValue != selectedColumn)
					OnSelectedCellChanged (new SelectedCellChangedEventArgs (Table, oldValue, SelectedColumn, SelectedRow, SelectedRow));
			}
		}

		/// <summary>
		/// The index of <see cref="DataTable.Rows"/> in <see cref="Table"/> that the user has currently selected
		/// </summary>
		public int SelectedRow {
			get => selectedRow;
			set {

				var oldValue = selectedRow;

				selectedRow = TableIsNullOrInvisible () ? 0 : Math.Min (Table.Rows - 1, Math.Max (0, value));

				if (oldValue != selectedRow)
					OnSelectedCellChanged (new SelectedCellChangedEventArgs (Table, SelectedColumn, SelectedColumn, oldValue, selectedRow));
			}
		}

		/// <summary>
		/// The minimum number of characters to render in any given column.
		/// </summary>
		public int MinCellWidth { get; set; }

		/// <summary>
		/// The maximum number of characters to render in any given column.  This prevents one long column from pushing out all the others
		/// </summary>
		public int MaxCellWidth { get; set; } = DefaultMaxCellWidth;

		/// <summary>
		/// The text representation that should be rendered for cells with the value <see cref="DBNull.Value"/>
		/// </summary>
		public string NullSymbol { get; set; } = "-";

		/// <summary>
		/// The symbol to add after each cell value and header value to visually seperate values (if not using vertical gridlines)
		/// </summary>
		public char SeparatorSymbol { get; set; } = ' ';

		/// <summary>
		/// This event is raised when the selected cell in the table changes.
		/// </summary>
		public event EventHandler<SelectedCellChangedEventArgs> SelectedCellChanged;

		/// <summary>
		/// This event is raised when a cell is activated e.g. by double clicking or pressing <see cref="CellActivationKey"/>
		/// </summary>
		public event EventHandler<CellActivatedEventArgs> CellActivated;

		/// <summary>
		/// This event is raised when a cell is toggled (see <see cref="Command.ToggleChecked"/>
		/// </summary>
		public event EventHandler<CellToggledEventArgs> CellToggled;

		/// <summary>
		/// The key which when pressed should trigger <see cref="CellActivated"/> event.  Defaults to Enter.
		/// </summary>
		public Key CellActivationKey {
			get => cellActivationKey;
			set {
				if (cellActivationKey != value) {
					ReplaceKeyBinding (cellActivationKey, value);

					// of API user is mixing and matching old and new methods of keybinding then they may have lost
					// the old binding (e.g. with ClearKeybindings) so ReplaceKeyBinding alone will fail
					AddKeyBinding (value, Command.Accept);
					cellActivationKey = value;
				}
			}
		}

		/// <summary>
		/// Navigator for cycling the selected item in the table by typing.
		/// Set to null to disable this feature.
		/// </summary>
		public CollectionNavigatorBase CollectionNavigator { get; set; }

		/// <summary>
		/// Initializes a <see cref="TableView"/> class using <see cref="LayoutStyle.Computed"/> layout. 
		/// </summary>
		/// <param name="table">The table to display in the control</param>
		public TableView (ITableSource table) : this ()
		{
			this.Table = table;
		}

		/// <summary>
		/// Initializes a <see cref="TableView"/> class using <see cref="LayoutStyle.Computed"/> layout. Set the <see cref="Table"/> property to begin editing
		/// </summary>
		public TableView () : base ()
		{
			CanFocus = true;

			this.CollectionNavigator = new TableCollectionNavigator (this);

			// Things this view knows how to do
			AddCommand (Command.Right, () => { ChangeSelectionByOffset (1, 0, false); return true; });
			AddCommand (Command.Left, () => { ChangeSelectionByOffset (-1, 0, false); return true; });
			AddCommand (Command.LineUp, () => { ChangeSelectionByOffset (0, -1, false); return true; });
			AddCommand (Command.LineDown, () => { ChangeSelectionByOffset (0, 1, false); return true; });
			AddCommand (Command.PageUp, () => { PageUp (false); return true; });
			AddCommand (Command.PageDown, () => { PageDown (false); return true; });
			AddCommand (Command.LeftHome, () => { ChangeSelectionToStartOfRow (false); return true; });
			AddCommand (Command.RightEnd, () => { ChangeSelectionToEndOfRow (false); return true; });
			AddCommand (Command.TopHome, () => { ChangeSelectionToStartOfTable (false); return true; });
			AddCommand (Command.BottomEnd, () => { ChangeSelectionToEndOfTable (false); return true; });

			AddCommand (Command.RightExtend, () => { ChangeSelectionByOffset (1, 0, true); return true; });
			AddCommand (Command.LeftExtend, () => { ChangeSelectionByOffset (-1, 0, true); return true; });
			AddCommand (Command.LineUpExtend, () => { ChangeSelectionByOffset (0, -1, true); return true; });
			AddCommand (Command.LineDownExtend, () => { ChangeSelectionByOffset (0, 1, true); return true; });
			AddCommand (Command.PageUpExtend, () => { PageUp (true); return true; });
			AddCommand (Command.PageDownExtend, () => { PageDown (true); return true; });
			AddCommand (Command.LeftHomeExtend, () => { ChangeSelectionToStartOfRow (true); return true; });
			AddCommand (Command.RightEndExtend, () => { ChangeSelectionToEndOfRow (true); return true; });
			AddCommand (Command.TopHomeExtend, () => { ChangeSelectionToStartOfTable (true); return true; });
			AddCommand (Command.BottomEndExtend, () => { ChangeSelectionToEndOfTable (true); return true; });

			AddCommand (Command.SelectAll, () => { SelectAll (); return true; });
			AddCommand (Command.Accept, () => { OnCellActivated (new CellActivatedEventArgs (Table, SelectedColumn, SelectedRow)); return true; });

			AddCommand (Command.ToggleChecked, () => { ToggleCurrentCellSelection (); return true; });

			// Default keybindings for this view
			AddKeyBinding (Key.CursorLeft, Command.Left);
			AddKeyBinding (Key.CursorRight, Command.Right);
			AddKeyBinding (Key.CursorUp, Command.LineUp);
			AddKeyBinding (Key.CursorDown, Command.LineDown);
			AddKeyBinding (Key.PageUp, Command.PageUp);
			AddKeyBinding (Key.PageDown, Command.PageDown);
			AddKeyBinding (Key.Home, Command.LeftHome);
			AddKeyBinding (Key.End, Command.RightEnd);
			AddKeyBinding (Key.Home | Key.CtrlMask, Command.TopHome);
			AddKeyBinding (Key.End | Key.CtrlMask, Command.BottomEnd);

			AddKeyBinding (Key.CursorLeft | Key.ShiftMask, Command.LeftExtend);
			AddKeyBinding (Key.CursorRight | Key.ShiftMask, Command.RightExtend);
			AddKeyBinding (Key.CursorUp | Key.ShiftMask, Command.LineUpExtend);
			AddKeyBinding (Key.CursorDown | Key.ShiftMask, Command.LineDownExtend);
			AddKeyBinding (Key.PageUp | Key.ShiftMask, Command.PageUpExtend);
			AddKeyBinding (Key.PageDown | Key.ShiftMask, Command.PageDownExtend);
			AddKeyBinding (Key.Home | Key.ShiftMask, Command.LeftHomeExtend);
			AddKeyBinding (Key.End | Key.ShiftMask, Command.RightEndExtend);
			AddKeyBinding (Key.Home | Key.CtrlMask | Key.ShiftMask, Command.TopHomeExtend);
			AddKeyBinding (Key.End | Key.CtrlMask | Key.ShiftMask, Command.BottomEndExtend);

			AddKeyBinding (Key.A | Key.CtrlMask, Command.SelectAll);
			AddKeyBinding (CellActivationKey, Command.Accept);
		}

		///<inheritdoc/>
		public override void OnDrawContent (Rect contentArea)
		{
			base.OnDrawContent (contentArea);

			Move (0, 0);

			scrollRightPoint = null;
			scrollLeftPoint = null;

			// What columns to render at what X offset in viewport
			var columnsToRender = CalculateViewport (Bounds).ToArray ();

			Driver.SetAttribute (GetNormalColor ());

			//invalidate current row (prevents scrolling around leaving old characters in the frame
			Driver.AddStr (new string (' ', Bounds.Width));

			int line = 0;

			if (ShouldRenderHeaders ()) {
				// Render something like:
				/*
					┌────────────────────┬──────────┬───────────┬──────────────┬─────────┐
					│ArithmeticComparator│chi       │Healthboard│Interpretation│Labnumber│
					└────────────────────┴──────────┴───────────┴──────────────┴─────────┘
				*/
				if (Style.ShowHorizontalHeaderOverline) {
					RenderHeaderOverline (line, Bounds.Width, columnsToRender);
					line++;
				}

				if (Style.ShowHeaders) {
					RenderHeaderMidline (line, columnsToRender);
					line++;
				}

				if (Style.ShowHorizontalHeaderUnderline) {
					RenderHeaderUnderline (line, Bounds.Width, columnsToRender);
					line++;
				}
			}

			int headerLinesConsumed = line;

			//render the cells
			for (; line < Bounds.Height; line++) {

				ClearLine (line, Bounds.Width);

				//work out what Row to render
				var rowToRender = RowOffset + (line - headerLinesConsumed);

				//if we have run off the end of the table
				if (TableIsNullOrInvisible () || rowToRender < 0)
					continue;

				// No more data
				if (rowToRender >= Table.Rows) {

					if (rowToRender == Table.Rows && Style.ShowHorizontalBottomline) {
						RenderBottomLine (line, Bounds.Width, columnsToRender);
					}

					continue;
				}

				RenderRow (line, rowToRender, columnsToRender);
			}
		}

		/// <summary>
		/// Clears a line of the console by filling it with spaces
		/// </summary>
		/// <param name="row"></param>
		/// <param name="width"></param>
		private void ClearLine (int row, int width)
		{
			Move (0, row);
			Driver.SetAttribute (GetNormalColor ());
			Driver.AddStr (new string (' ', width));
		}

		/// <summary>
		/// Returns the amount of vertical space currently occupied by the header or 0 if it is not visible.
		/// </summary>
		/// <returns></returns>
		internal int GetHeaderHeightIfAny ()
		{
			return ShouldRenderHeaders () ? GetHeaderHeight () : 0;
		}

		/// <summary>
		/// Returns the amount of vertical space required to display the header
		/// </summary>
		/// <returns></returns>
		internal int GetHeaderHeight ()
		{
			int heightRequired = Style.ShowHeaders ? 1 : 0;

			if (Style.ShowHorizontalHeaderOverline)
				heightRequired++;

			if (Style.ShowHorizontalHeaderUnderline)
				heightRequired++;

			return heightRequired;
		}

		private void RenderHeaderOverline (int row, int availableWidth, ColumnToRender [] columnsToRender)
		{
			// Renders a line above table headers (when visible) like:
			// ┌────────────────────┬──────────┬───────────┬──────────────┬─────────┐

			for (int c = 0; c < availableWidth; c++) {

				var rune = CM.Glyphs.HLine;

				if (Style.ShowVerticalHeaderLines) {

					if (c == 0) {
						rune = CM.Glyphs.ULCorner;
					}
					// if the next column is the start of a header
					else if (columnsToRender.Any (r => r.X == c + 1)) {
						rune = CM.Glyphs.TopTee;
					} else if (c == availableWidth - 1) {
						rune = CM.Glyphs.URCorner;
					}
					  // if the next console column is the lastcolumns end
					  else if (Style.ExpandLastColumn == false &&
						columnsToRender.Any (r => r.IsVeryLast && r.X + r.Width - 1 == c)) {
						rune = CM.Glyphs.TopTee;
					}
				}

				AddRuneAt (Driver, c, row, rune);
			}
		}

		private void RenderHeaderMidline (int row, ColumnToRender [] columnsToRender)
		{
			// Renders something like:
			// │ArithmeticComparator│chi       │Healthboard│Interpretation│Labnumber│

			ClearLine (row, Bounds.Width);

			//render start of line
			if (style.ShowVerticalHeaderLines)
				AddRune (0, row, CM.Glyphs.VLine);

			for (int i = 0; i < columnsToRender.Length; i++) {

				var current = columnsToRender [i];

				var colStyle = Style.GetColumnStyleIfAny (current.Column);
				var colName = table.ColumnNames [current.Column];

				RenderSeparator (current.X - 1, row, true);

				Move (current.X, row);

				Driver.AddStr (TruncateOrPad (colName, colName, current.Width, colStyle));

				if (Style.ExpandLastColumn == false && current.IsVeryLast) {
					RenderSeparator (current.X + current.Width - 1, row, true);
				}
			}

			//render end of line
			if (style.ShowVerticalHeaderLines)
				AddRune (Bounds.Width - 1, row, CM.Glyphs.VLine);
		}

		private void RenderHeaderUnderline (int row, int availableWidth, ColumnToRender [] columnsToRender)
		{
			/*
			*  First lets work out if we should be rendering scroll indicators
			*/

			// are there are visible columns to the left that have been pushed
			// off the screen due to horizontal scrolling?
			bool moreColumnsToLeft = ColumnOffset > 0;

			// if we moved left would we find a new column (or are they all invisible?)
			if (!TryGetNearestVisibleColumn (ColumnOffset - 1, false, false, out _)) {
				moreColumnsToLeft = false;
			}

			// are there visible columns to the right that have not yet been reached?
			// lets find out, what is the column index of the last column we are rendering
			int lastColumnIdxRendered = ColumnOffset + columnsToRender.Length - 1;

			// are there more valid indexes?
			bool moreColumnsToRight = lastColumnIdxRendered < Table.Columns;

			// if we went right from the last column would we find a new visible column?
			if (!TryGetNearestVisibleColumn (lastColumnIdxRendered + 1, true, false, out _)) {
				// no we would not
				moreColumnsToRight = false;
			}

			/*
			*  Now lets draw the line itself
			*/

			// Renders a line below the table headers (when visible) like:
			// ├──────────┼───────────┼───────────────────┼──────────┼────────┼─────────────┤

			for (int c = 0; c < availableWidth; c++) {

				// Start by assuming we just draw a straight line the
				// whole way but update to instead draw a header indicator
				// or scroll arrow etc
				var rune = CM.Glyphs.HLine;

				if (Style.ShowVerticalHeaderLines) {
					if (c == 0) {
						// for first character render line
						rune = Style.ShowVerticalCellLines ? CM.Glyphs.LeftTee : CM.Glyphs.LLCorner;

						// unless we have horizontally scrolled along
						// in which case render an arrow, to indicate user
						// can scroll left
						if (Style.ShowHorizontalScrollIndicators && moreColumnsToLeft) {
							rune = CM.Glyphs.LeftArrow;
							scrollLeftPoint = new Point (c, row);
						}

					}
					// if the next column is the start of a header
					else if (columnsToRender.Any (r => r.X == c + 1)) {

						/*TODO: is ┼ symbol in Driver?*/
						rune = Style.ShowVerticalCellLines ? CM.Glyphs.Cross : CM.Glyphs.BottomTee;
					} else if (c == availableWidth - 1) {

						// for the last character in the table
						rune = Style.ShowVerticalCellLines ? CM.Glyphs.RightTee : CM.Glyphs.LRCorner;

						// unless there is more of the table we could horizontally
						// scroll along to see. In which case render an arrow,
						// to indicate user can scroll right
						if (Style.ShowHorizontalScrollIndicators && moreColumnsToRight) {
							rune = CM.Glyphs.RightArrow;
							scrollRightPoint = new Point (c, row);
						}

					}
					  // if the next console column is the lastcolumns end
					  else if (Style.ExpandLastColumn == false &&
						columnsToRender.Any (r => r.IsVeryLast && r.X + r.Width - 1 == c)) {
						rune = Style.ShowVerticalCellLines ? CM.Glyphs.Cross : CM.Glyphs.BottomTee;
					}
				}

				AddRuneAt (Driver, c, row, rune);
			}

		}

		private void RenderBottomLine (int row, int availableWidth, ColumnToRender [] columnsToRender)
		{
			// Renders a line at the bottom of the table after all the data like:
			// └─────────────────────────────────┴──────────┴──────┴──────────┴────────┴────────────────────────────────────────────┘

			for (int c = 0; c < availableWidth; c++) {

				// Start by assuming we just draw a straight line the
				// whole way but update to instead draw BottomTee / Corner etc
				var rune = CM.Glyphs.HLine;

				if (Style.ShowVerticalCellLines) {
					if (c == 0) {
						// for first character render line
						rune = CM.Glyphs.LLCorner;

					} else if (columnsToRender.Any (r => r.X == c + 1)) {
						// if the next column is the start of a header
						rune = CM.Glyphs.BottomTee;
					} else if (c == availableWidth - 1) {
						// for the last character in the table
						rune = CM.Glyphs.LRCorner;

					} else if (Style.ExpandLastColumn == false &&
						  columnsToRender.Any (r => r.IsVeryLast && r.X + r.Width - 1 == c)) {
						// if the next console column is the lastcolumns end
						rune = CM.Glyphs.BottomTee;
					}
				}

				AddRuneAt (Driver, c, row, rune);
			}
		}
		private void RenderRow (int row, int rowToRender, ColumnToRender [] columnsToRender)
		{
			var focused = HasFocus;

			var rowScheme = (Style.RowColorGetter?.Invoke (
				new RowColorGetterArgs (Table, rowToRender))) ?? ColorScheme;

			//start by clearing the entire line
			Move (0, row);

			Attribute color;

			if (FullRowSelect && IsSelected (0, rowToRender)) {
				color = focused ? rowScheme.HotFocus : rowScheme.HotNormal;
			} else {
				color = Enabled ? rowScheme.Normal : rowScheme.Disabled;
			}

			Driver.SetAttribute (color);
			Driver.AddStr (new string (' ', Bounds.Width));

			// Render cells for each visible header for the current row
			for (int i = 0; i < columnsToRender.Length; i++) {

				var current = columnsToRender [i];

				var colStyle = Style.GetColumnStyleIfAny (current.Column);

				// move to start of cell (in line with header positions)
				Move (current.X, row);

				// Set color scheme based on whether the current cell is the selected one
				bool isSelectedCell = IsSelected (current.Column, rowToRender);

				var val = Table [rowToRender, current.Column];

				// Render the (possibly truncated) cell value
				var representation = GetRepresentation (val, colStyle);

				// to get the colour scheme
				var colorSchemeGetter = colStyle?.ColorGetter;

				ColorScheme scheme;
				if (colorSchemeGetter != null) {
					// user has a delegate for defining row color per cell, call it
					scheme = colorSchemeGetter (
						new CellColorGetterArgs (Table, rowToRender, current.Column, val, representation, rowScheme));

					// if users custom color getter returned null, use the row scheme
					if (scheme == null) {
						scheme = rowScheme;
					}
				} else {
					// There is no custom cell coloring delegate so use the scheme for the row
					scheme = rowScheme;
				}

				Attribute cellColor;
				if (isSelectedCell) {
					cellColor = focused ? scheme.HotFocus : scheme.HotNormal;
				} else {
					cellColor = Enabled ? scheme.Normal : scheme.Disabled;
				}

				var render = TruncateOrPad (val, representation, current.Width, colStyle);

				// While many cells can be selected (see MultiSelectedRegions) only one cell is the primary (drives navigation etc)
				bool isPrimaryCell = current.Column == selectedColumn && rowToRender == selectedRow;

				RenderCell (cellColor, render, isPrimaryCell);

				// Reset color scheme to normal for drawing separators if we drew text with custom scheme
				if (scheme != rowScheme) {

					if (isSelectedCell) {
						color = focused ? rowScheme.HotFocus : rowScheme.HotNormal;
					} else {
						color = Enabled ? rowScheme.Normal : rowScheme.Disabled;
					}
					Driver.SetAttribute (color);
				}

				// If not in full row select mode always, reset color scheme to normal and render the vertical line (or space) at the end of the cell
				if (!FullRowSelect)
					Driver.SetAttribute (Enabled ? rowScheme.Normal : rowScheme.Disabled);

				if (style.AlwaysUseNormalColorForVerticalCellLines && style.ShowVerticalCellLines) {

					Driver.SetAttribute (rowScheme.Normal);
				}

				RenderSeparator (current.X - 1, row, false);

				if (Style.ExpandLastColumn == false && current.IsVeryLast) {
					RenderSeparator (current.X + current.Width - 1, row, false);
				}
			}

			if (style.ShowVerticalCellLines) {

				Driver.SetAttribute (rowScheme.Normal);

				//render start and end of line
				AddRune (0, row, CM.Glyphs.VLine);
				AddRune (Bounds.Width - 1, row, CM.Glyphs.VLine);
			}

		}

		/// <summary>
		/// Override to provide custom multi colouring to cells.  Use <see cref="View.Driver"/> to
		/// with <see cref="ConsoleDriver.AddStr(string)"/>.  The driver will already be
		/// in the correct place when rendering and you must render the full <paramref name="render"/>
		/// or the view will not look right.  For simpler provision of color use <see cref="ColumnStyle.ColorGetter"/>
		/// For changing the content that is rendered use <see cref="ColumnStyle.RepresentationGetter"/>
		/// </summary>
		/// <param name="cellColor"></param>
		/// <param name="render"></param>
		/// <param name="isPrimaryCell"></param>
		protected virtual void RenderCell (Attribute cellColor, string render, bool isPrimaryCell)
		{
			// If the cell is the selected col/row then draw the first rune in inverted colors
			// this allows the user to track which cell is the active one during a multi cell
			// selection or in full row select mode
			if (Style.InvertSelectedCellFirstCharacter && isPrimaryCell) {

				if (render.Length > 0) {
					// invert the color of the current cell for the first character
					Driver.SetAttribute (Driver.MakeAttribute (cellColor.Background, cellColor.Foreground));
					Driver.AddRune ((Rune)render [0]);

					if (render.Length > 1) {
						Driver.SetAttribute (cellColor);
						Driver.AddStr (render.Substring (1));
					}
				}
			} else {
				Driver.SetAttribute (cellColor);
				Driver.AddStr (render);
			}
		}

		private void RenderSeparator (int col, int row, bool isHeader)
		{
			if (col < 0)
				return;

			var renderLines = isHeader ? style.ShowVerticalHeaderLines : style.ShowVerticalCellLines;

			Rune symbol = renderLines ? CM.Glyphs.VLine : (Rune)SeparatorSymbol;
			AddRune (col, row, symbol);
		}

		void AddRuneAt (ConsoleDriver d, int col, int row, Rune ch)
		{
			Move (col, row);
			d.AddRune (ch);
		}

		/// <summary>
		/// Truncates or pads <paramref name="representation"/> so that it occupies a exactly <paramref name="availableHorizontalSpace"/> using the alignment specified in <paramref name="colStyle"/> (or left if no style is defined)
		/// </summary>
		/// <param name="originalCellValue">The object in this cell of the <see cref="Table"/></param>
		/// <param name="representation">The string representation of <paramref name="originalCellValue"/></param>
		/// <param name="availableHorizontalSpace"></param>
		/// <param name="colStyle">Optional style indicating custom alignment for the cell</param>
		/// <returns></returns>
		private string TruncateOrPad (object originalCellValue, string representation, int availableHorizontalSpace, ColumnStyle colStyle)
		{
			if (string.IsNullOrEmpty (representation))
				return new string (' ', availableHorizontalSpace);

			// if value is not wide enough
			if (representation.EnumerateRunes ().Sum (c => c.GetColumns ()) < availableHorizontalSpace) {

				// pad it out with spaces to the given alignment
				int toPad = availableHorizontalSpace - (representation.EnumerateRunes ().Sum (c => c.GetColumns ()) + 1 /*leave 1 space for cell boundary*/);

				switch (colStyle?.GetAlignment (originalCellValue) ?? TextAlignment.Left) {

				case TextAlignment.Left:
					return representation + new string (' ', toPad);
				case TextAlignment.Right:
					return new string (' ', toPad) + representation;

				// TODO: With single line cells, centered and justified are the same right?
				case TextAlignment.Centered:
				case TextAlignment.Justified:
					return
						new string (' ', (int)Math.Floor (toPad / 2.0)) + // round down
						representation +
						new string (' ', (int)Math.Ceiling (toPad / 2.0)); // round up
				}
			}

			// value is too wide
			return new string (representation.TakeWhile (c => (availableHorizontalSpace -= ((Rune)c).GetColumns ()) > 0).ToArray ());
		}



		/// <inheritdoc/>
		public override bool ProcessKey (KeyEvent keyEvent)
		{
			if (TableIsNullOrInvisible ()) {
				PositionCursor ();
				return false;
			}

			var result = InvokeKeybindings (keyEvent);
			if (result != null) {
				PositionCursor ();
				return true;
			}

			if (CollectionNavigator != null &&
				this.HasFocus &&
				Table.Rows != 0 &&
				Terminal.Gui.CollectionNavigator.IsCompatibleKey (keyEvent) &&
				!keyEvent.Key.HasFlag (Key.CtrlMask) &&
				!keyEvent.Key.HasFlag (Key.AltMask) &&
				char.IsLetterOrDigit ((char)keyEvent.KeyValue)) {
				return CycleToNextTableEntryBeginningWith (keyEvent);
			}

			return false;
		}

		private bool CycleToNextTableEntryBeginningWith (KeyEvent keyEvent)
		{
			var row = SelectedRow;

			// There is a multi select going on and not just for the current row
			if (GetAllSelectedCells ().Any (c => c.Y != row)) {
				return false;
			}

			int match = CollectionNavigator.GetNextMatchingItem (row, (char)keyEvent.KeyValue);

			if (match != -1) {
				SelectedRow = match;
				EnsureValidSelection ();
				EnsureSelectedCellIsVisible ();
				return true;
			}

			return false;
		}

		/// <summary>
		/// Moves the <see cref="SelectedRow"/> and <see cref="SelectedColumn"/> to the given col/row in <see cref="Table"/>. Optionally starting a box selection (see <see cref="MultiSelect"/>)
		/// </summary>
		/// <param name="col"></param>
		/// <param name="row"></param>
		/// <param name="extendExistingSelection">True to create a multi cell selection or adjust an existing one</param>
		public void SetSelection (int col, int row, bool extendExistingSelection)
		{
			// if we are trying to increase the column index then
			// we are moving right otherwise we are moving left
			bool lookRight = col > selectedColumn;

			col = GetNearestVisibleColumn (col, lookRight, true);

			if (!MultiSelect || !extendExistingSelection) {
				ClearMultiSelectedRegions (true);
			}

			if (extendExistingSelection) {

				// If we are extending current selection but there isn't one
				if (MultiSelectedRegions.Count == 0 || MultiSelectedRegions.All (m => m.IsToggled)) {
					// Create a new region between the old active cell and the new cell
					var rect = CreateTableSelection (SelectedColumn, SelectedRow, col, row);
					MultiSelectedRegions.Push (rect);
				} else {
					// Extend the current head selection to include the new cell
					var head = MultiSelectedRegions.Pop ();
					var newRect = CreateTableSelection (head.Origin.X, head.Origin.Y, col, row);
					MultiSelectedRegions.Push (newRect);
				}
			}

			SelectedColumn = col;
			SelectedRow = row;
		}

		private void ClearMultiSelectedRegions (bool keepToggledSelections)
		{
			if (!keepToggledSelections) {
				MultiSelectedRegions.Clear ();
				return;
			}

			var oldRegions = MultiSelectedRegions.ToArray ().Reverse ();

			MultiSelectedRegions.Clear ();

			foreach (var region in oldRegions) {
				if (region.IsToggled) {
					MultiSelectedRegions.Push (region);
				}
			}
		}

		/// <summary>
		/// Unions the current selected cell (and/or regions) with the provided cell and makes
		/// it the active one.
		/// </summary>
		/// <param name="col"></param>
		/// <param name="row"></param>
		private void UnionSelection (int col, int row)
		{
			if (!MultiSelect || TableIsNullOrInvisible ()) {
				return;
			}

			EnsureValidSelection ();

			var oldColumn = SelectedColumn;
			var oldRow = SelectedRow;

			// move us to the new cell
			SelectedColumn = col;
			SelectedRow = row;
			MultiSelectedRegions.Push (
				CreateTableSelection (col, row)
				);

			// if the old cell was not part of a rectangular select
			// or otherwise selected we need to retain it in the selection

			if (!IsSelected (oldColumn, oldRow)) {
				MultiSelectedRegions.Push (
					CreateTableSelection (oldColumn, oldRow)
					);
			}
		}

		/// <summary>
		/// Moves the <see cref="SelectedRow"/> and <see cref="SelectedColumn"/> by the provided offsets. Optionally starting a box selection (see <see cref="MultiSelect"/>)
		/// </summary>
		/// <param name="offsetX">Offset in number of columns</param>
		/// <param name="offsetY">Offset in number of rows</param>
		/// <param name="extendExistingSelection">True to create a multi cell selection or adjust an existing one</param>
		public void ChangeSelectionByOffset (int offsetX, int offsetY, bool extendExistingSelection)
		{
			SetSelection (SelectedColumn + offsetX, SelectedRow + offsetY, extendExistingSelection);
			Update ();
		}

		/// <summary>
		/// Moves the selection up by one page
		/// </summary>
		/// <param name="extend">true to extend the current selection (if any) instead of replacing</param>
		public void PageUp (bool extend)
		{
			ChangeSelectionByOffset (0, -(Bounds.Height - GetHeaderHeightIfAny ()), extend);
			Update ();
		}

		/// <summary>
		/// Moves the selection down by one page
		/// </summary>
		/// <param name="extend">true to extend the current selection (if any) instead of replacing</param>
		public void PageDown (bool extend)
		{
			ChangeSelectionByOffset (0, Bounds.Height - GetHeaderHeightIfAny (), extend);
			Update ();
		}

		/// <summary>
		/// Moves or extends the selection to the first cell in the table (0,0).
		/// If <see cref="FullRowSelect"/> is enabled then selection instead moves
		/// to (<see cref="SelectedColumn"/>,0) i.e. no horizontal scrolling.
		/// </summary>
		/// <param name="extend">true to extend the current selection (if any) instead of replacing</param>
		public void ChangeSelectionToStartOfTable (bool extend)
		{
			SetSelection (FullRowSelect ? SelectedColumn : 0, 0, extend);
			Update ();
		}

		/// <summary>
		/// Moves or extends the selection to the final cell in the table (nX,nY).
		/// If <see cref="FullRowSelect"/> is enabled then selection instead moves
		/// to (<see cref="SelectedColumn"/>,nY) i.e. no horizontal scrolling.
		/// </summary>
		/// <param name="extend">true to extend the current selection (if any) instead of replacing</param>
		public void ChangeSelectionToEndOfTable (bool extend)
		{
			var finalColumn = Table.Columns - 1;

			SetSelection (FullRowSelect ? SelectedColumn : finalColumn, Table.Rows - 1, extend);
			Update ();
		}

		/// <summary>
		/// Moves or extends the selection to the last cell in the current row
		/// </summary>
		/// <param name="extend">true to extend the current selection (if any) instead of replacing</param>
		public void ChangeSelectionToEndOfRow (bool extend)
		{
			SetSelection (Table.Columns - 1, SelectedRow, extend);
			Update ();
		}

		/// <summary>
		/// Moves or extends the selection to the first cell in the current row
		/// </summary>
		/// <param name="extend">true to extend the current selection (if any) instead of replacing</param>
		public void ChangeSelectionToStartOfRow (bool extend)
		{
			SetSelection (0, SelectedRow, extend);
			Update ();
		}

		/// <summary>
		/// When <see cref="MultiSelect"/> is on, creates selection over all cells in the table (replacing any old selection regions)
		/// </summary>
		public void SelectAll ()
		{
			if (TableIsNullOrInvisible () || !MultiSelect || Table.Rows == 0)
				return;

			ClearMultiSelectedRegions (true);

			// Create a single region over entire table, set the origin of the selection to the active cell so that a followup spread selection e.g. shift-right behaves properly
			MultiSelectedRegions.Push (new TableSelection (new Point (SelectedColumn, SelectedRow), new Rect (0, 0, Table.Columns, table.Rows)));
			Update ();
		}

		/// <summary>
		/// Returns all cells in any <see cref="MultiSelectedRegions"/> (if <see cref="MultiSelect"/> is enabled) and the selected cell
		/// </summary>
		/// <returns></returns>
		public IEnumerable<Point> GetAllSelectedCells ()
		{
			if (TableIsNullOrInvisible () || Table.Rows == 0) {
				return Enumerable.Empty<Point> ();
			}

			EnsureValidSelection ();

			var toReturn = new HashSet<Point> ();

			// If there are one or more rectangular selections
			if (MultiSelect && MultiSelectedRegions.Any ()) {

				// Quiz any cells for whether they are selected.  For performance we only need to check those between the top left and lower right vertex of selection regions
				var yMin = MultiSelectedRegions.Min (r => r.Rect.Top);
				var yMax = MultiSelectedRegions.Max (r => r.Rect.Bottom);

				var xMin = FullRowSelect ? 0 : MultiSelectedRegions.Min (r => r.Rect.Left);
				var xMax = FullRowSelect ? Table.Columns : MultiSelectedRegions.Max (r => r.Rect.Right);

				for (int y = yMin; y < yMax; y++) {
					for (int x = xMin; x < xMax; x++) {
						if (IsSelected (x, y)) {
							toReturn.Add (new Point (x, y));
						}
					}
				}
			}

			// if there are no region selections then it is just the active cell

			// if we are selecting the full row
			if (FullRowSelect) {
				// all cells in active row are selected
				for (int x = 0; x < Table.Columns; x++) {
					toReturn.Add (new Point (x, SelectedRow));
				}
			} else {
				// Not full row select and no multi selections
				toReturn.Add (new Point (SelectedColumn, SelectedRow));
			}

			return toReturn;
		}

		/// <summary>
		/// Returns a new rectangle between the two points with positive width/height regardless of relative positioning of the points.  pt1 is always considered the <see cref="TableSelection.Origin"/> point
		/// </summary>
		/// <param name="pt1X">Origin point for the selection in X</param>
		/// <param name="pt1Y">Origin point for the selection in Y</param>
		/// <param name="pt2X">End point for the selection in X</param>
		/// <param name="pt2Y">End point for the selection in Y</param>
		/// <param name="toggle">True if selection is result of <see cref="Command.ToggleChecked"/></param>
		/// <returns></returns>
		private TableSelection CreateTableSelection (int pt1X, int pt1Y, int pt2X, int pt2Y, bool toggle = false)
		{
			var top = Math.Max (Math.Min (pt1Y, pt2Y), 0);
			var bot = Math.Max (Math.Max (pt1Y, pt2Y), 0);

			var left = Math.Max (Math.Min (pt1X, pt2X), 0);
			var right = Math.Max (Math.Max (pt1X, pt2X), 0);

			// Rect class is inclusive of Top Left but exclusive of Bottom Right so extend by 1
			return new TableSelection (new Point (pt1X, pt1Y), new Rect (left, top, right - left + 1, bot - top + 1)) {
				IsToggled = toggle
			};
		}

		private void ToggleCurrentCellSelection ()
		{

			var e = new CellToggledEventArgs (Table, selectedColumn, selectedRow);
			OnCellToggled (e);
			if (e.Cancel) {
				return;
			}

			if (!MultiSelect) {
				return;
			}

			var regions = GetMultiSelectedRegionsContaining (selectedColumn, selectedRow).ToArray ();
			var toggles = regions.Where (s => s.IsToggled).ToArray ();

			// Toggle it off
			if (toggles.Any ()) {

				var oldRegions = MultiSelectedRegions.ToArray ().Reverse ();
				MultiSelectedRegions.Clear ();

				foreach (var region in oldRegions) {
					if (!toggles.Contains (region))
						MultiSelectedRegions.Push (region);
				}
			} else {

				// user is toggling selection within a rectangular
				// select.  So toggle the full region
				if (regions.Any ()) {
					foreach (var r in regions) {
						r.IsToggled = true;
					}
				} else {
					// Toggle on a single cell selection
					MultiSelectedRegions.Push (
					CreateTableSelection (selectedColumn, SelectedRow, selectedColumn, selectedRow, true)
					);
				}

			}
		}

		/// <summary>
		/// Returns a single point as a <see cref="TableSelection"/>
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		private TableSelection CreateTableSelection (int x, int y)
		{
			return CreateTableSelection (x, y, x, y);
		}
		/// <summary>
		/// <para>
		/// Returns true if the given cell is selected either because it is the active cell or part of a multi cell selection (e.g. <see cref="FullRowSelect"/>).
		/// </para>
		/// <remarks>Returns <see langword="false"/> if <see cref="ColumnStyle.Visible"/> is <see langword="false"/>.</remarks>
		/// </summary>
		/// <param name="col"></param>
		/// <param name="row"></param>
		/// <returns></returns>
		public bool IsSelected (int col, int row)
		{
			if (!IsColumnVisible (col)) {
				return false;
			}

			if (GetMultiSelectedRegionsContaining (col, row).Any ()) {
				return true;
			}

			return row == SelectedRow &&
				(col == SelectedColumn || FullRowSelect);
		}

		private IEnumerable<TableSelection> GetMultiSelectedRegionsContaining (int col, int row)
		{
			if (!MultiSelect) {
				return Enumerable.Empty<TableSelection> ();
			}

			if (FullRowSelect) {
				return MultiSelectedRegions.Where (r => r.Rect.Bottom > row && r.Rect.Top <= row);
			} else {
				return MultiSelectedRegions.Where (r => r.Rect.Contains (col, row));
			}
		}

		/// <summary>
		/// Returns true if the given <paramref name="columnIndex"/> indexes a visible
		/// column otherwise false.  Returns false for indexes that are out of bounds.
		/// </summary>
		/// <param name="columnIndex"></param>
		/// <returns></returns>
		private bool IsColumnVisible (int columnIndex)
		{
			// if the column index provided is out of bounds
			if (columnIndex < 0 || columnIndex >= table.Columns) {
				return false;
			}

			return this.Style.GetColumnStyleIfAny (columnIndex)?.Visible ?? true;
		}

		/// <summary>
		/// Positions the cursor in the area of the screen in which the start of the active cell is rendered.  Calls base implementation if active cell is not visible due to scrolling or table is loaded etc
		/// </summary>
		public override void PositionCursor ()
		{
			if (TableIsNullOrInvisible ()) {
				base.PositionCursor ();
				return;
			}

			var screenPoint = CellToScreen (SelectedColumn, SelectedRow);

			if (screenPoint != null)
				Move (screenPoint.Value.X, screenPoint.Value.Y);
		}

		///<inheritdoc/>
		public override bool MouseEvent (MouseEvent me)
		{
			if (!me.Flags.HasFlag (MouseFlags.Button1Clicked) && !me.Flags.HasFlag (MouseFlags.Button1DoubleClicked) &&
				me.Flags != MouseFlags.WheeledDown && me.Flags != MouseFlags.WheeledUp &&
				me.Flags != MouseFlags.WheeledLeft && me.Flags != MouseFlags.WheeledRight)
				return false;

			if (!HasFocus && CanFocus) {
				SetFocus ();
			}

			if (TableIsNullOrInvisible ()) {
				return false;
			}

			// Scroll wheel flags
			switch (me.Flags) {
			case MouseFlags.WheeledDown:
				RowOffset++;
				EnsureValidScrollOffsets ();
				SetNeedsDisplay ();
				return true;

			case MouseFlags.WheeledUp:
				RowOffset--;
				EnsureValidScrollOffsets ();
				SetNeedsDisplay ();
				return true;

			case MouseFlags.WheeledRight:
				ColumnOffset++;
				EnsureValidScrollOffsets ();
				SetNeedsDisplay ();
				return true;

			case MouseFlags.WheeledLeft:
				ColumnOffset--;
				EnsureValidScrollOffsets ();
				SetNeedsDisplay ();
				return true;
			}

			// TODO: Revert this (or not) once #2578 is solved
			var boundsX = me.X - GetFramesThickness ().Left;
			var boundsY = me.Y - GetFramesThickness ().Top;

			if (me.Flags.HasFlag (MouseFlags.Button1Clicked)) {

				if (scrollLeftPoint != null
					&& scrollLeftPoint.Value.X == boundsX
					&& scrollLeftPoint.Value.Y == boundsY) {
					ColumnOffset--;
					EnsureValidScrollOffsets ();
					SetNeedsDisplay ();
				}

				if (scrollRightPoint != null
					&& scrollRightPoint.Value.X == boundsX
					&& scrollRightPoint.Value.Y == boundsY) {
					ColumnOffset++;
					EnsureValidScrollOffsets ();
					SetNeedsDisplay ();
				}

				var hit = ScreenToCell (boundsX, boundsY);
				if (hit != null) {

					if (MultiSelect && HasControlOrAlt (me)) {
						UnionSelection (hit.Value.X, hit.Value.Y);
					} else {
						SetSelection (hit.Value.X, hit.Value.Y, me.Flags.HasFlag (MouseFlags.ButtonShift));
					}

					Update ();
				}
			}

			// Double clicking a cell activates
			if (me.Flags == MouseFlags.Button1DoubleClicked) {
				var hit = ScreenToCell (boundsX, boundsY);
				if (hit != null) {
					OnCellActivated (new CellActivatedEventArgs (Table, hit.Value.X, hit.Value.Y));
				}
			}

			return false;
		}

		private bool HasControlOrAlt (MouseEvent me)
		{
			return me.Flags.HasFlag (MouseFlags.ButtonAlt) || me.Flags.HasFlag (MouseFlags.ButtonCtrl);
		}

		/// <summary>.
		/// Returns the column and row of <see cref="Table"/> that corresponds to a given point 
		/// on the screen (relative to the control client area).  Returns null if the point is
		/// in the header, no table is loaded or outside the control bounds.
		/// </summary>
		/// <param name="clientX">X offset from the top left of the control.</param>
		/// <param name="clientY">Y offset from the top left of the control.</param>
		/// <returns>Cell clicked or null.</returns>
		public Point? ScreenToCell (int clientX, int clientY)
		{
			return ScreenToCell (clientX, clientY, out _);
		}

		/// <inheritdoc cref="ScreenToCell(int, int)"/>
		/// <param name="clientX">X offset from the top left of the control.</param>
		/// <param name="clientY">Y offset from the top left of the control.</param>
		/// <param name="headerIfAny">If the click is in a header this is the column clicked.</param>
		public Point? ScreenToCell (int clientX, int clientY, out int? headerIfAny)
		{
			headerIfAny = null;

			if (TableIsNullOrInvisible ())
				return null;

			var viewPort = CalculateViewport (Bounds);

			var headerHeight = GetHeaderHeightIfAny ();

			var col = viewPort.LastOrDefault (c => c.X <= clientX);

			// Click is on the header section of rendered UI
			if (clientY < headerHeight) {
				headerIfAny = col?.Column;
				return null;
			}

			var rowIdx = RowOffset - headerHeight + clientY;

			// if click is off bottom of the rows don't give an
			// invalid index back to user!
			if (rowIdx >= Table.Rows) {
				return null;
			}

			if (col != null && rowIdx >= 0) {

				return new Point (col.Column, rowIdx);
			}

			return null;
		}

		/// <summary>
		/// Returns the screen position (relative to the control client area) that the given cell is rendered or null if it is outside the current scroll area or no table is loaded
		/// </summary>
		/// <param name="tableColumn">The index of the <see cref="Table"/> column you are looking for</param>
		/// <param name="tableRow">The index of the row in <see cref="Table"/> that you are looking for</param>
		/// <returns></returns>
		public Point? CellToScreen (int tableColumn, int tableRow)
		{
			if (TableIsNullOrInvisible ())
				return null;

			var viewPort = CalculateViewport (Bounds);

			var headerHeight = GetHeaderHeightIfAny ();

			var colHit = viewPort.FirstOrDefault (c => c.Column == tableColumn);

			// current column is outside the scroll area
			if (colHit == null)
				return null;

			// the cell is too far up above the current scroll area
			if (RowOffset > tableRow)
				return null;

			// the cell is way down below the scroll area and off the screen
			if (tableRow > RowOffset + (Bounds.Height - headerHeight))
				return null;

			return new Point (colHit.X, tableRow + headerHeight - RowOffset);
		}
		/// <summary>
		/// Updates the view to reflect changes to <see cref="Table"/> and to (<see cref="ColumnOffset"/> / <see cref="RowOffset"/>) etc
		/// </summary>
		/// <remarks>This always calls <see cref="View.SetNeedsDisplay()"/></remarks>
		public void Update ()
		{
			if (!IsInitialized || TableIsNullOrInvisible ()) {
				SetNeedsDisplay ();
				return;
			}

			EnsureValidScrollOffsets ();
			EnsureValidSelection ();

			EnsureSelectedCellIsVisible ();

			SetNeedsDisplay ();
		}

		/// <summary>
		/// Updates <see cref="ColumnOffset"/> and <see cref="RowOffset"/> where they are outside the bounds of the table (by adjusting them to the nearest existing cell).  Has no effect if <see cref="Table"/> has not been set.
		/// </summary>
		/// <remarks>Changes will not be immediately visible in the display until you call <see cref="View.SetNeedsDisplay()"/></remarks>
		public void EnsureValidScrollOffsets ()
		{
			if (TableIsNullOrInvisible ()) {
				return;
			}

			ColumnOffset = Math.Max (Math.Min (ColumnOffset, Table.Columns - 1), 0);
			RowOffset = Math.Max (Math.Min (RowOffset, Table.Rows - 1), 0);
		}

		/// <summary>
		/// Updates <see cref="SelectedColumn"/>, <see cref="SelectedRow"/> and <see cref="MultiSelectedRegions"/> where they are outside the bounds of the table (by adjusting them to the nearest existing cell).  Has no effect if <see cref="Table"/> has not been set.
		/// </summary>
		/// <remarks>Changes will not be immediately visible in the display until you call <see cref="View.SetNeedsDisplay()"/></remarks>
		public void EnsureValidSelection ()
		{
			if (TableIsNullOrInvisible ()) {

				// Table doesn't exist, we should probably clear those selections
				ClearMultiSelectedRegions (false);
				return;
			}

			SelectedColumn = Math.Max (Math.Min (SelectedColumn, Table.Columns - 1), 0);
			SelectedRow = Math.Max (Math.Min (SelectedRow, Table.Rows - 1), 0);

			// If SelectedColumn is invisible move it to a visible one
			SelectedColumn = GetNearestVisibleColumn (SelectedColumn, lookRight: true, true);

			var oldRegions = MultiSelectedRegions.ToArray ().Reverse ();

			MultiSelectedRegions.Clear ();

			// evaluate 
			foreach (var region in oldRegions) {
				// ignore regions entirely below current table state
				if (region.Rect.Top >= Table.Rows)
					continue;

				// ignore regions entirely too far right of table columns
				if (region.Rect.Left >= Table.Columns)
					continue;

				// ensure region's origin exists
				region.Origin = new Point (
					Math.Max (Math.Min (region.Origin.X, Table.Columns - 1), 0),
					Math.Max (Math.Min (region.Origin.Y, Table.Rows - 1), 0));

				// ensure regions do not go over edge of table bounds
				region.Rect = Rect.FromLTRB (region.Rect.Left,
					region.Rect.Top,
					Math.Max (Math.Min (region.Rect.Right, Table.Columns), 0),
					Math.Max (Math.Min (region.Rect.Bottom, Table.Rows), 0)
					);

				MultiSelectedRegions.Push (region);
			}
		}

		/// <summary>
		/// Returns true if the <see cref="Table"/> is not set or all the
		/// columns in the <see cref="Table"/> have an explicit
		/// <see cref="ColumnStyle"/> that marks them <see cref="ColumnStyle.visible"/>
		/// <see langword="false"/>.
		/// </summary>
		/// <returns></returns>
		private bool TableIsNullOrInvisible ()
		{
			return Table == null ||
				Table.Columns <= 0 ||
				Enumerable.Range (0, Table.Columns).All (
				c => (Style.GetColumnStyleIfAny (c)?.Visible ?? true) == false);
		}

		/// <summary>
		/// Returns <paramref name="columnIndex"/> unless the <see cref="ColumnStyle.Visible"/> is false for
		/// the indexed column.  If so then the index returned is nudged to the nearest visible
		/// column.
		/// </summary>
		/// <remarks>Returns <paramref name="columnIndex"/> unchanged if it is invalid (e.g. out of bounds).</remarks>
		/// <param name="columnIndex">The input column index.</param>
		/// <param name="lookRight">When nudging invisible selections look right first.
		/// <see langword="true"/> to look right, <see langword="false"/> to look left.</param>
		/// <param name="allowBumpingInOppositeDirection">If we cannot find anything visible when
		/// looking in direction of <paramref name="lookRight"/> then should we look in the opposite
		/// direction instead? Use true if you want to push a selection to a valid index no matter what.
		/// Use false if you are primarily interested in learning about directional column visibility.</param>
		private int GetNearestVisibleColumn (int columnIndex, bool lookRight, bool allowBumpingInOppositeDirection)
		{
			if (TryGetNearestVisibleColumn (columnIndex, lookRight, allowBumpingInOppositeDirection, out var answer)) {
				return answer;
			}

			return columnIndex;
		}

		private bool TryGetNearestVisibleColumn (int columnIndex, bool lookRight, bool allowBumpingInOppositeDirection, out int idx)
		{
			// if the column index provided is out of bounds
			if (columnIndex < 0 || columnIndex >= table.Columns) {

				idx = columnIndex;
				return false;
			}

			// get the column visibility by index (if no style visible is true)
			bool [] columnVisibility =
				Enumerable.Range (0, Table.Columns)
				.Select (c => this.Style.GetColumnStyleIfAny (c)?.Visible ?? true)
				.ToArray ();

			// column is visible
			if (columnVisibility [columnIndex]) {
				idx = columnIndex;
				return true;
			}

			int increment = lookRight ? 1 : -1;

			// move in that direction
			for (int i = columnIndex; i >= 0 && i < columnVisibility.Length; i += increment) {
				// if we find a visible column
				if (columnVisibility [i]) {
					idx = i;
					return true;
				}
			}

			// Caller only wants to look in one direction and we did not find any
			// visible columns in that direction
			if (!allowBumpingInOppositeDirection) {
				idx = columnIndex;
				return false;
			}

			// Caller will let us look in the other direction so
			// now look other way
			increment = -increment;

			for (int i = columnIndex; i >= 0 && i < columnVisibility.Length; i += increment) {
				// if we find a visible column
				if (columnVisibility [i]) {
					idx = i;
					return true;
				}
			}

			// nothing seems to be visible so just return input index
			idx = columnIndex;
			return false;
		}

		/// <summary>
		/// Updates scroll offsets to ensure that the selected cell is visible.  Has no effect if <see cref="Table"/> has not been set.
		/// </summary>
		/// <remarks>Changes will not be immediately visible in the display until you call <see cref="View.SetNeedsDisplay()"/></remarks>
		public void EnsureSelectedCellIsVisible ()
		{
			if (Table == null || Table.Columns <= 0) {
				return;
			}

			var columnsToRender = CalculateViewport (Bounds).ToArray ();
			var headerHeight = GetHeaderHeightIfAny ();

			//if we have scrolled too far to the left 
			if (SelectedColumn < columnsToRender.Min (r => r.Column)) {
				ColumnOffset = SelectedColumn;
			}

			//if we have scrolled too far to the right
			if (SelectedColumn > columnsToRender.Max (r => r.Column)) {

				if (Style.SmoothHorizontalScrolling) {

					// Scroll right 1 column at a time until the users selected column is visible
					while (SelectedColumn > columnsToRender.Max (r => r.Column)) {

						ColumnOffset++;
						columnsToRender = CalculateViewport (Bounds).ToArray ();

						// if we are already scrolled to the last column then break
						// this will prevent any theoretical infinite loop
						if (ColumnOffset >= Table.Columns - 1)
							break;

					}
				} else {
					ColumnOffset = SelectedColumn;
				}

			}

			//if we have scrolled too far down
			if (SelectedRow >= RowOffset + (Bounds.Height - headerHeight)) {
				RowOffset = SelectedRow - (Bounds.Height - headerHeight) + 1;
			}
			//if we have scrolled too far up
			if (SelectedRow < RowOffset) {
				RowOffset = SelectedRow;
			}
		}

		/// <summary>
		/// Invokes the <see cref="SelectedCellChanged"/> event
		/// </summary>
		protected virtual void OnSelectedCellChanged (SelectedCellChangedEventArgs args)
		{
			SelectedCellChanged?.Invoke (this, args);
		}

		/// <summary>
		/// Invokes the <see cref="CellActivated"/> event
		/// </summary>
		/// <param name="args"></param>
		protected virtual void OnCellActivated (CellActivatedEventArgs args)
		{
			CellActivated?.Invoke (this, args);
		}
		/// <summary>
		/// Invokes the <see cref="CellToggled"/> event
		/// </summary>
		/// <param name="args"></param>
		protected virtual void OnCellToggled (CellToggledEventArgs args)
		{
			CellToggled?.Invoke (this, args);
		}

		/// <summary>
		/// Calculates which columns should be rendered given the <paramref name="bounds"/> in which to display and the <see cref="ColumnOffset"/>
		/// </summary>
		/// <param name="bounds"></param>
		/// <param name="padding"></param>
		/// <returns></returns>
		private IEnumerable<ColumnToRender> CalculateViewport (Rect bounds, int padding = 1)
		{
			if (TableIsNullOrInvisible ()) {
				return Enumerable.Empty<ColumnToRender> ();
			}

			var toReturn = new List<ColumnToRender> ();
			int usedSpace = 0;

			//if horizontal space is required at the start of the line (before the first header)
			if (Style.ShowVerticalHeaderLines || Style.ShowVerticalCellLines)
				usedSpace += 1;

			int availableHorizontalSpace = bounds.Width;
			int rowsToRender = bounds.Height;

			// reserved for the headers row
			if (ShouldRenderHeaders ())
				rowsToRender -= GetHeaderHeight ();

			bool first = true;
			var lastColumn = Table.Columns - 1;

			// TODO : Maybe just a for loop?
			foreach (var col in Enumerable.Range (0, Table.Columns).Skip (ColumnOffset)) {

				int startingIdxForCurrentHeader = usedSpace;
				var colStyle = Style.GetColumnStyleIfAny (col);
				int colWidth;

				// if column is not being rendered
				if (colStyle?.Visible == false) {
					// do not add it to the returned columns
					continue;
				}

				// is there enough space for this column (and it's data)?
				colWidth = CalculateMaxCellWidth (col, rowsToRender, colStyle) + padding;

				if (MinCellWidth > 0 && colWidth < (MinCellWidth + padding)) {
					if (MinCellWidth > MaxCellWidth) {
						colWidth = MaxCellWidth + padding;
					} else {
						colWidth = MinCellWidth + padding;
					}
				}

				// there is not enough space for this columns 
				// visible content
				if (usedSpace + colWidth > availableHorizontalSpace) {
					bool showColumn = false;

					// if this column accepts flexible width rendering and
					// is therefore happy rendering into less space
					if (colStyle != null && colStyle.MinAcceptableWidth > 0 &&
						// is there enough space to meet the MinAcceptableWidth
						(availableHorizontalSpace - usedSpace) >= colStyle.MinAcceptableWidth) {
						// show column and use use whatever space is 
						// left for rendering it
						showColumn = true;
						colWidth = availableHorizontalSpace - usedSpace;
					}

					// If its the only column we are able to render then
					// accept it anyway (that must be one massively wide column!)
					if (first) {
						showColumn = true;
					}

					// no special exceptions and we are out of space
					// so stop accepting new columns for the render area
					if (!showColumn)
						break;
				}

				usedSpace += colWidth;

				// required for if we end up here because first == true i.e. we have a single massive width (overspilling bounds) column to present
				colWidth = Math.Min (availableHorizontalSpace, colWidth);
				var isVeryLast = lastColumn == col;

				// there is space
				toReturn.Add (new ColumnToRender (col, startingIdxForCurrentHeader, colWidth, isVeryLast));
				first = false;
			}

			if (Style.ExpandLastColumn) {
				var last = toReturn.Last ();
				last.Width = Math.Max (last.Width, availableHorizontalSpace - last.X);
			}

			return toReturn;
		}

		private bool ShouldRenderHeaders ()
		{
			if (TableIsNullOrInvisible ())
				return false;

			return Style.AlwaysShowHeaders || rowOffset == 0;
		}

		/// <summary>
		/// Returns the maximum of the <paramref name="col"/> name and the maximum length of data that will be rendered starting at <see cref="RowOffset"/> and rendering <paramref name="rowsToRender"/>
		/// </summary>
		/// <param name="col"></param>
		/// <param name="rowsToRender"></param>
		/// <param name="colStyle"></param>
		/// <returns></returns>
		private int CalculateMaxCellWidth (int col, int rowsToRender, ColumnStyle colStyle)
		{
			int spaceRequired = table.ColumnNames [col].EnumerateRunes ().Sum (c => c.GetColumns ());

			// if table has no rows
			if (RowOffset < 0)
				return spaceRequired;


			for (int i = RowOffset; i < RowOffset + rowsToRender && i < Table.Rows; i++) {

				//expand required space if cell is bigger than the last biggest cell or header
				spaceRequired = Math.Max (
					spaceRequired,
					GetRepresentation (Table [i, col], colStyle).EnumerateRunes ().Sum (c => c.GetColumns ()));
			}

			// Don't require more space than the style allows
			if (colStyle != null) {

				// enforce maximum cell width based on style
				if (spaceRequired > colStyle.MaxWidth) {
					spaceRequired = colStyle.MaxWidth;
				}

				// enforce minimum cell width based on style
				if (spaceRequired < colStyle.MinWidth) {
					spaceRequired = colStyle.MinWidth;
				}
			}

			// enforce maximum cell width based on global table style
			if (spaceRequired > MaxCellWidth)
				spaceRequired = MaxCellWidth;

			return spaceRequired;
		}

		/// <summary>
		/// Returns the value that should be rendered to best represent a strongly typed <paramref name="value"/> read from <see cref="Table"/>
		/// </summary>
		/// <param name="value"></param>
		/// <param name="colStyle">Optional style defining how to represent cell values</param>
		/// <returns></returns>
		private string GetRepresentation (object value, ColumnStyle colStyle)
		{
			if (value == null || value == DBNull.Value) {
				return NullSymbol;
			}

			return colStyle != null ? colStyle.GetRepresentation (value) : value.ToString ();
		}

		

		/// <summary>
		/// Describes a desire to render a column at a given horizontal position in the UI
		/// </summary>
		internal class ColumnToRender {

			/// <summary>
			/// The column to render
			/// </summary>
			public int Column { get; set; }

			/// <summary>
			/// The horizontal position to begin rendering the column at
			/// </summary>
			public int X { get; set; }

			/// <summary>
			/// The width that the column should occupy as calculated by <see cref="CalculateViewport(Rect, int)"/>.  Note that this includes
			/// space for padding i.e. the separator between columns.
			/// </summary>
			public int Width { get; internal set; }

			/// <summary>
			/// True if this column is the very last column in the <see cref="Table"/> (not just the last visible column)
			/// </summary>
			public bool IsVeryLast { get; }

			public ColumnToRender (int col, int x, int width, bool isVeryLast)
			{
				Column = col;
				X = x;
				Width = width;
				IsVeryLast = isVeryLast;
			}

		}
	}
}