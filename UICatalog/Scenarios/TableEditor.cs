using System;
using System.Collections.Generic;
using System.Data;
using Terminal.Gui;
using System.Linq;
using System.Globalization;
using static Terminal.Gui.TableView;
using System.Text;

namespace UICatalog.Scenarios {

	[ScenarioMetadata (Name: "TableEditor", Description: "Implements data table editor using the TableView control.")]
	[ScenarioCategory ("TableView")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Dialogs")]
	[ScenarioCategory ("Text and Formatting")]
	[ScenarioCategory ("Top Level Windows")]
	public class TableEditor : Scenario {
		TableView tableView;
		DataTable currentTable;
    
		private MenuItem _miShowHeaders;
		private MenuItem _miAlwaysShowHeaders;
		private MenuItem _miHeaderOverline;
		private MenuItem _miHeaderMidline;
		private MenuItem _miHeaderUnderline;
		private MenuItem _miShowHorizontalScrollIndicators;
		private MenuItem _miCellLines;
		private MenuItem _miFullRowSelect;
		private MenuItem _miExpandLastColumn;
		private MenuItem _miAlwaysUseNormalColorForVerticalCellLines;
		private MenuItem _miSmoothScrolling;
		private MenuItem _miAlternatingColors;
		private MenuItem _miCursor;
		private MenuItem _miBottomline;
		private MenuItem _miCheckboxes;
		private MenuItem _miRadioboxes;

		ColorScheme redColorScheme;
		ColorScheme redColorSchemeAlt;
		ColorScheme alternatingColorScheme;

		public override void Setup ()
		{
			Win.Title = this.GetName ();
			Win.Y = 1; // menu
			Win.Height = Dim.Fill (1); // status bar
			Application.Top.LayoutSubviews ();

			this.tableView = new TableView () {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (1),
			};

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_OpenBigExample", "", () => OpenExample(true)),
					new MenuItem ("_OpenSmallExample", "", () => OpenExample(false)),
					new MenuItem ("OpenCharacter_Map","",()=>OpenUnicodeMap()),
					new MenuItem ("_CloseExample", "", () => CloseExample()),
					new MenuItem ("_Quit", "", () => Quit()),
				}),
				new MenuBarItem ("_View", new MenuItem [] {
					_miShowHeaders = new MenuItem ("_ShowHeaders", "", () => ToggleShowHeaders()){Checked = tableView.Style.ShowHeaders, CheckType = MenuItemCheckStyle.Checked },
					_miAlwaysShowHeaders = new MenuItem ("_AlwaysShowHeaders", "", () => ToggleAlwaysShowHeaders()){Checked = tableView.Style.AlwaysShowHeaders, CheckType = MenuItemCheckStyle.Checked },
					_miHeaderOverline = new MenuItem ("_HeaderOverLine", "", () => ToggleOverline()){Checked = tableView.Style.ShowHorizontalHeaderOverline, CheckType = MenuItemCheckStyle.Checked },
					_miHeaderMidline = new MenuItem ("_HeaderMidLine", "", () => ToggleHeaderMidline()){Checked = tableView.Style.ShowVerticalHeaderLines, CheckType = MenuItemCheckStyle.Checked },
					_miHeaderUnderline = new MenuItem ("_HeaderUnderLine", "", () => ToggleUnderline()){Checked = tableView.Style.ShowHorizontalHeaderUnderline, CheckType = MenuItemCheckStyle.Checked },
					_miBottomline = new MenuItem ("_BottomLine", "", () => ToggleBottomline()){Checked = tableView.Style.ShowHorizontalBottomline, CheckType = MenuItemCheckStyle.Checked },
					_miShowHorizontalScrollIndicators = new MenuItem ("_HorizontalScrollIndicators", "", () => ToggleHorizontalScrollIndicators()){Checked = tableView.Style.ShowHorizontalScrollIndicators, CheckType = MenuItemCheckStyle.Checked },
					_miFullRowSelect =new MenuItem ("_FullRowSelect", "", () => ToggleFullRowSelect()){Checked = tableView.FullRowSelect, CheckType = MenuItemCheckStyle.Checked },
					_miCellLines =new MenuItem ("_CellLines", "", () => ToggleCellLines()){Checked = tableView.Style.ShowVerticalCellLines, CheckType = MenuItemCheckStyle.Checked },
					_miExpandLastColumn = new MenuItem ("_ExpandLastColumn", "", () => ToggleExpandLastColumn()){Checked = tableView.Style.ExpandLastColumn, CheckType = MenuItemCheckStyle.Checked },
					_miAlwaysUseNormalColorForVerticalCellLines = new MenuItem ("_AlwaysUseNormalColorForVerticalCellLines", "", () => ToggleAlwaysUseNormalColorForVerticalCellLines()){Checked = tableView.Style.AlwaysUseNormalColorForVerticalCellLines, CheckType = MenuItemCheckStyle.Checked },
					_miSmoothScrolling = new MenuItem ("_SmoothHorizontalScrolling", "", () => ToggleSmoothScrolling()){Checked = tableView.Style.SmoothHorizontalScrolling, CheckType = MenuItemCheckStyle.Checked },
					new MenuItem ("_AllLines", "", () => ToggleAllCellLines()),
					new MenuItem ("_NoLines", "", () => ToggleNoCellLines()),
					_miCheckboxes = new MenuItem ("_Checkboxes", "", () => ToggleCheckboxes(false)){Checked = false, CheckType = MenuItemCheckStyle.Checked },
					_miRadioboxes = new MenuItem ("_Radioboxes", "", () => ToggleCheckboxes(true)){Checked = false, CheckType = MenuItemCheckStyle.Checked },
					_miAlternatingColors = new MenuItem ("Alternating Colors", "", () => ToggleAlternatingColors()){CheckType = MenuItemCheckStyle.Checked},
					_miCursor = new MenuItem ("Invert Selected Cell First Character", "", () => ToggleInvertSelectedCellFirstCharacter()){Checked = tableView.Style.InvertSelectedCellFirstCharacter,CheckType = MenuItemCheckStyle.Checked},
					new MenuItem ("_ClearColumnStyles", "", () => ClearColumnStyles()),
					new MenuItem ("Sho_w All Columns", "", ()=>ShowAllColumns())
				}),
				new MenuBarItem ("_Column", new MenuItem [] {
					new MenuItem ("_Set Max Width", "", SetMaxWidth),
					new MenuItem ("_Set Min Width", "", SetMinWidth),
					new MenuItem ("_Set MinAcceptableWidth", "",SetMinAcceptableWidth),
					new MenuItem ("_Set All MinAcceptableWidth=1", "",SetMinAcceptableWidthToOne),
				}),
			});

			Application.Top.Add (menu);

			var statusBar = new StatusBar (new StatusItem [] {
				new StatusItem(Key.F2, "~F2~ OpenExample", () => OpenExample(true)),
				new StatusItem(Key.F3, "~F3~ CloseExample", () => CloseExample()),
				new StatusItem(Key.F4, "~F4~ OpenSimple", () => OpenSimple(true)),
				new StatusItem(Application.QuitKey, $"{Application.QuitKey} to Quit", () => Quit()),
			});
			Application.Top.Add (statusBar);

			Win.Add (tableView);

			var selectedCellLabel = new Label () {
				X = 0,
				Y = Pos.Bottom (tableView),
				Text = "0,0",
				Width = Dim.Fill (),
				TextAlignment = TextAlignment.Right

			};

			Win.Add (selectedCellLabel);

			tableView.SelectedCellChanged += (s, e) => { selectedCellLabel.Text = $"{tableView.SelectedRow},{tableView.SelectedColumn}"; };
			tableView.CellActivated += EditCurrentCell;
			tableView.KeyPress += TableViewKeyPress;

			SetupScrollBar ();

			redColorScheme = new ColorScheme () {
				Disabled = Win.ColorScheme.Disabled,
				HotFocus = Win.ColorScheme.HotFocus,
				Focus = Win.ColorScheme.Focus,
				Normal = Application.Driver.MakeAttribute (Color.Red, Win.ColorScheme.Normal.Background)
			};

			alternatingColorScheme = new ColorScheme () {

				Disabled = Win.ColorScheme.Disabled,
				HotFocus = Win.ColorScheme.HotFocus,
				Focus = Win.ColorScheme.Focus,
				Normal = Application.Driver.MakeAttribute (Color.White, Color.BrightBlue)
			};
			redColorSchemeAlt = new ColorScheme () {

				Disabled = Win.ColorScheme.Disabled,
				HotFocus = Win.ColorScheme.HotFocus,
				Focus = Win.ColorScheme.Focus,
				Normal = Application.Driver.MakeAttribute (Color.Red, Color.BrightBlue)
			};

			// if user clicks the mouse in TableView
			tableView.MouseClick += (s, e) => {

				tableView.ScreenToCell (e.MouseEvent.X, e.MouseEvent.Y, out int? clickedCol);

				if (clickedCol != null) {
					if (e.MouseEvent.Flags.HasFlag (MouseFlags.Button1Clicked)) {

						// left click in a header
						SortColumn (clickedCol.Value);
					} else if (e.MouseEvent.Flags.HasFlag (MouseFlags.Button3Clicked)) {

						// right click in a header
						ShowHeaderContextMenu (clickedCol.Value, e);
					}
				}
			};

			tableView.AddKeyBinding (Key.Space, Command.ToggleChecked);
		}

		private void ShowAllColumns ()
		{
			foreach (var colStyle in tableView.Style.ColumnStyles) {
				colStyle.Value.Visible = true;
			}
			tableView.Update ();
		}

		private void SortColumn (int clickedCol)
		{
			var sort = GetProposedNewSortOrder (clickedCol, out var isAsc);

			// don't try to sort on the toggled column
			if (HasCheckboxes () && clickedCol == 0) {
				return;
			}
				

			SortColumn (clickedCol, sort, isAsc);
		}

		private void SortColumn (int clickedCol, string sort, bool isAsc)
		{
			// set a sort order
			currentTable.DefaultView.Sort = sort;

			// copy the rows from the view
			var sortedCopy = currentTable.DefaultView.ToTable ();
			currentTable.Rows.Clear ();
			foreach (DataRow r in sortedCopy.Rows) {
				currentTable.ImportRow (r);
			}

			foreach (DataColumn col in currentTable.Columns) {

				// remove any lingering sort indicator
				col.ColumnName = TrimArrows (col.ColumnName);

				// add a new one if this the one that is being sorted
				if (col.Ordinal == clickedCol) {
					col.ColumnName += isAsc ? CM.Glyphs.UpArrow : CM.Glyphs.DownArrow;
				}
			}

			tableView.Update ();
		}

		private string TrimArrows (string columnName)
		{
			return columnName.TrimEnd ((char)CM.Glyphs.UpArrow.Value, (char)CM.Glyphs.DownArrow.Value);
		}
		private string StripArrows (string columnName)
		{
			return columnName.Replace ($"{CM.Glyphs.DownArrow}", "").Replace ($"{CM.Glyphs.UpArrow}", "");
		}
		private string GetProposedNewSortOrder (int clickedCol, out bool isAsc)
		{
			// work out new sort order
			var sort = currentTable.DefaultView.Sort;
			var colName = tableView.Table.ColumnNames[clickedCol];

			if (sort?.EndsWith ("ASC") ?? false) {
				sort = $"{colName} DESC";
				isAsc = false;
			} else {
				sort = $"{colName} ASC";
				isAsc = true;
			}

			return sort;
		}

		private void ShowHeaderContextMenu (int clickedCol, MouseEventEventArgs e)
		{
			if(HasCheckboxes() && clickedCol == 0) {
				return;
			}

			var sort = GetProposedNewSortOrder (clickedCol, out var isAsc);
			var colName = tableView.Table.ColumnNames[clickedCol];

			var contextMenu = new ContextMenu (e.MouseEvent.X + 1, e.MouseEvent.Y + 1,
				new MenuBarItem (new MenuItem [] {
					new MenuItem ($"Hide {TrimArrows(colName)}", "", () => HideColumn(clickedCol)),
					new MenuItem ($"Sort {StripArrows(sort)}","",()=>SortColumn(clickedCol,sort,isAsc)),
				})
			);

			contextMenu.Show ();
		}

		private void HideColumn (int clickedCol)
		{
			var style = tableView.Style.GetOrCreateColumnStyle (clickedCol);
			style.Visible = false;
			tableView.Update ();
		}

		private int? GetColumn ()
		{
			if (tableView.Table == null)
				return null;

			if (tableView.SelectedColumn < 0 || tableView.SelectedColumn > tableView.Table.Columns)
				return null;

			return tableView.SelectedColumn;
		}

		private void SetMinAcceptableWidthToOne ()
		{
			foreach (DataColumn c in currentTable.Columns) {
				var style = tableView.Style.GetOrCreateColumnStyle (c.Ordinal);
				style.MinAcceptableWidth = 1;
			}
		}
		private void SetMinAcceptableWidth ()
		{
			var col = GetColumn ();
			RunColumnWidthDialog (col, "MinAcceptableWidth", (s, v) => s.MinAcceptableWidth = v, (s) => s.MinAcceptableWidth);
		}

		private void SetMinWidth ()
		{
			var col = GetColumn ();
			RunColumnWidthDialog (col, "MinWidth", (s, v) => s.MinWidth = v, (s) => s.MinWidth);
		}

		private void SetMaxWidth ()
		{
			var col = GetColumn ();
			RunColumnWidthDialog (col, "MaxWidth", (s, v) => s.MaxWidth = v, (s) => s.MaxWidth);
		}

		private void RunColumnWidthDialog (int? col, string prompt, Action<ColumnStyle, int> setter, Func<ColumnStyle, int> getter)
		{
			if(col == null) {
				return;
			}

			var accepted = false;
			var ok = new Button ("Ok", is_default: true);
			ok.Clicked += (s, e) => { accepted = true; Application.RequestStop (); };
			var cancel = new Button ("Cancel");
			cancel.Clicked += (s,e) => { Application.RequestStop (); };
			var d = new Dialog (ok, cancel) { Title = prompt };

			var style = tableView.Style.GetOrCreateColumnStyle (col.Value);

			var lbl = new Label () {
				X = 0,
				Y = 1,
				Text = tableView.Table.ColumnNames[col.Value]
			};

			var tf = new TextField () {
				Text = getter (style).ToString (),
				X = 0,
				Y = 2,
				Width = Dim.Fill ()
			};

			d.Add (lbl, tf);
			tf.SetFocus ();

			Application.Run (d);

			if (accepted) {

				try {
					setter (style, int.Parse (tf.Text));
				} catch (Exception ex) {
					MessageBox.ErrorQuery (60, 20, "Failed to set", ex.Message, "Ok");
				}

				tableView.Update ();
			}
		}

		private void SetupScrollBar ()
		{
			var scrollBar = new ScrollBarView (tableView, true);

			scrollBar.ChangedPosition += (s,e) => {
				tableView.RowOffset = scrollBar.Position;
				if (tableView.RowOffset != scrollBar.Position) {
					scrollBar.Position = tableView.RowOffset;
				}
				tableView.SetNeedsDisplay ();
			};
			
			scrollBar.OtherScrollBarView.ChangedPosition += (s,e) => {
				tableView.ColumnOffset = scrollBar.OtherScrollBarView.Position;
				if (tableView.ColumnOffset != scrollBar.OtherScrollBarView.Position) {
					scrollBar.OtherScrollBarView.Position = tableView.ColumnOffset;
				}
				tableView.SetNeedsDisplay ();
			};

			tableView.DrawContent += (s,e) => {
				scrollBar.Size = tableView.Table?.Rows ?? 0;
				scrollBar.Position = tableView.RowOffset;
				//scrollBar.OtherScrollBarView.Size = tableView.Maxlength - 1;
				//scrollBar.OtherScrollBarView.Position = tableView.LeftItem;
				scrollBar.Refresh ();
			};
		}

		private void TableViewKeyPress (object sender, KeyEventEventArgs e)
		{
			if (e.KeyEvent.Key == Key.DeleteChar) {

				if (tableView.FullRowSelect) {
					// Delete button deletes all rows when in full row mode
					foreach (int toRemove in tableView.GetAllSelectedCells ().Select (p => p.Y).Distinct ().OrderByDescending (i => i))
						currentTable.Rows.RemoveAt (toRemove);
				} else {

					// otherwise set all selected cells to null
					foreach (var pt in tableView.GetAllSelectedCells ()) {
						currentTable.Rows [pt.Y] [pt.X] = DBNull.Value;
					}
				}

				tableView.Update ();
				e.Handled = true;
			}

		}

		private void ClearColumnStyles ()
		{
			tableView.Style.ColumnStyles.Clear ();
			tableView.Update ();
		}

		private void ToggleShowHeaders ()
		{
			_miShowHeaders.Checked = !_miShowHeaders.Checked;
			tableView.Style.ShowHeaders = (bool)_miShowHeaders.Checked;
			tableView.Update ();
		}

		private void ToggleAlwaysShowHeaders ()
		{
			_miAlwaysShowHeaders.Checked = !_miAlwaysShowHeaders.Checked;
			tableView.Style.AlwaysShowHeaders = (bool)_miAlwaysShowHeaders.Checked;
			tableView.Update ();
		}

		private void ToggleOverline ()
		{
			_miHeaderOverline.Checked = !_miHeaderOverline.Checked;
			tableView.Style.ShowHorizontalHeaderOverline = (bool)_miHeaderOverline.Checked;
			tableView.Update ();
		}
		private void ToggleHeaderMidline ()
		{
			_miHeaderMidline.Checked = !_miHeaderMidline.Checked;
			tableView.Style.ShowVerticalHeaderLines = (bool)_miHeaderMidline.Checked;
			tableView.Update ();
		}
		private void ToggleUnderline ()
		{
			_miHeaderUnderline.Checked = !_miHeaderUnderline.Checked;
			tableView.Style.ShowHorizontalHeaderUnderline = (bool)_miHeaderUnderline.Checked;
			tableView.Update ();
		}
		private void ToggleBottomline()
		{
			_miBottomline.Checked = !_miBottomline.Checked;
			tableView.Style.ShowHorizontalBottomline = (bool)_miBottomline.Checked;
			tableView.Update ();
		}
		private void ToggleHorizontalScrollIndicators ()
		{
			_miShowHorizontalScrollIndicators.Checked = !_miShowHorizontalScrollIndicators.Checked;
			tableView.Style.ShowHorizontalScrollIndicators = (bool)_miShowHorizontalScrollIndicators.Checked;
			tableView.Update ();
		}
		private void ToggleFullRowSelect ()
		{
			_miFullRowSelect.Checked = !_miFullRowSelect.Checked;
			tableView.FullRowSelect = (bool)_miFullRowSelect.Checked;
			tableView.Update ();
		}

		private void ToggleExpandLastColumn ()
		{
			_miExpandLastColumn.Checked = !_miExpandLastColumn.Checked;
			tableView.Style.ExpandLastColumn = (bool)_miExpandLastColumn.Checked;

			tableView.Update ();

		}

		private void ToggleCheckboxes (bool radio)
		{
			if (tableView.Table is CheckBoxTableSourceWrapperByIndex wrapper) {

				// unwrap it to remove check boxes
				tableView.Table = wrapper.Wrapping;

				_miCheckboxes.Checked = false;
				_miRadioboxes.Checked = false;

				// if toggling off checkboxes/radio
				if(wrapper.UseRadioButtons == radio) {
					return;
				}
			}
			
			// Either toggling on checkboxes/radio or switching from radio to checkboxes (or vice versa)
			
			var source = new CheckBoxTableSourceWrapperByIndex (tableView, tableView.Table) {
				UseRadioButtons = radio
			};
			tableView.Table = source;


			if (radio) {
				_miRadioboxes.Checked = true;
				_miCheckboxes.Checked = false;
			}
			else {

				_miRadioboxes.Checked = false;
				_miCheckboxes.Checked = true;
			}
			
		}

		private void ToggleAlwaysUseNormalColorForVerticalCellLines()
		{
			_miAlwaysUseNormalColorForVerticalCellLines.Checked = !_miAlwaysUseNormalColorForVerticalCellLines.Checked;
			tableView.Style.AlwaysUseNormalColorForVerticalCellLines = (bool)_miAlwaysUseNormalColorForVerticalCellLines.Checked;

			tableView.Update ();
		}
		private void ToggleSmoothScrolling ()
		{
			_miSmoothScrolling.Checked = !_miSmoothScrolling.Checked;
			tableView.Style.SmoothHorizontalScrolling = (bool)_miSmoothScrolling.Checked;

			tableView.Update ();

		}
		private void ToggleCellLines ()
		{
			_miCellLines.Checked = !_miCellLines.Checked;
			tableView.Style.ShowVerticalCellLines = (bool)_miCellLines.Checked;
			tableView.Update ();
		}
		private void ToggleAllCellLines ()
		{
			tableView.Style.ShowHorizontalHeaderOverline = true;
			tableView.Style.ShowVerticalHeaderLines = true;
			tableView.Style.ShowHorizontalHeaderUnderline = true;
			tableView.Style.ShowVerticalCellLines = true;

			_miHeaderOverline.Checked = true;
			_miHeaderMidline.Checked = true;
			_miHeaderUnderline.Checked = true;
			_miCellLines.Checked = true;

			tableView.Update ();
		}
		private void ToggleNoCellLines ()
		{
			tableView.Style.ShowHorizontalHeaderOverline = false;
			tableView.Style.ShowVerticalHeaderLines = false;
			tableView.Style.ShowHorizontalHeaderUnderline = false;
			tableView.Style.ShowVerticalCellLines = false;

			_miHeaderOverline.Checked = false;
			_miHeaderMidline.Checked = false;
			_miHeaderUnderline.Checked = false;
			_miCellLines.Checked = false;

			tableView.Update ();
		}

		private void ToggleAlternatingColors ()
		{
			//toggle menu item
			_miAlternatingColors.Checked = !_miAlternatingColors.Checked;

			if (_miAlternatingColors.Checked == true) {
				tableView.Style.RowColorGetter = (a) => { return a.RowIndex % 2 == 0 ? alternatingColorScheme : null; };
			} else {
				tableView.Style.RowColorGetter = null;
			}
			tableView.SetNeedsDisplay ();
		}

		private void ToggleInvertSelectedCellFirstCharacter ()
		{
			//toggle menu item
			_miCursor.Checked = !_miCursor.Checked;
			tableView.Style.InvertSelectedCellFirstCharacter = (bool)_miCursor.Checked;
			tableView.SetNeedsDisplay ();
		}
		private void CloseExample ()
		{
			tableView.Table = null;
		}

		private void Quit ()
		{
			Application.RequestStop ();
		}

		private void OpenExample (bool big)
		{
			SetTable(BuildDemoDataTable (big ? 30 : 5, big ? 1000 : 5));
			SetDemoTableStyles ();
		}

		private void SetTable (DataTable dataTable)
		{
			tableView.Table = new DataTableSource(currentTable = dataTable);
		}

		private void OpenUnicodeMap ()
		{
			SetTable(BuildUnicodeMap ());
			tableView.Update ();
		}

		private DataTable BuildUnicodeMap ()
		{
			var dt = new DataTable ();

			// add cols called 0 to 9
			for (int i = 0; i < 10; i++) {

				var col = dt.Columns.Add (i.ToString (), typeof (uint));
				var style = tableView.Style.GetOrCreateColumnStyle (col.Ordinal);
				style.RepresentationGetter = (o) => new Rune ((uint)o).ToString ();
			}

			// add cols called a to z
			for (int i = 'a'; i < 'a' + 26; i++) {

				var col = dt.Columns.Add (((char)i).ToString (), typeof (uint));
				var style = tableView.Style.GetOrCreateColumnStyle (col.Ordinal);
				style.RepresentationGetter = (o) => new Rune ((uint)o).ToString ();
			}

			// now add table contents
			List<uint> runes = new List<uint> ();

			foreach (var range in Ranges) {
				for (uint i = range.Start; i <= range.End; i++) {
					runes.Add (i);
				}
			}

			DataRow dr = null;

			for (int i = 0; i < runes.Count; i++) {
				if (dr == null || i % dt.Columns.Count == 0) {
					dr = dt.Rows.Add ();
				}
				dr [i % dt.Columns.Count] = runes [i].ToString ();
			}

			return dt;
		}
		class UnicodeRange {
			public uint Start;
			public uint End;
			public string Category;
			public UnicodeRange (uint start, uint end, string category)
			{
				this.Start = start;
				this.End = end;
				this.Category = category;
			}
		}

		List<UnicodeRange> Ranges = new List<UnicodeRange> {
			new UnicodeRange (0x0000, 0x001F, "ASCII Control Characters"),
			new UnicodeRange (0x0080, 0x009F, "C0 Control Characters"),
			new UnicodeRange(0x1100, 0x11ff,"Hangul Jamo"),	// This is where wide chars tend to start
			new UnicodeRange(0x20A0, 0x20CF,"Currency Symbols"),
			new UnicodeRange(0x2100, 0x214F,"Letterlike Symbols"),
			new UnicodeRange(0x2190, 0x21ff,"Arrows" ),
			new UnicodeRange(0x2200, 0x22ff,"Mathematical symbols"),
			new UnicodeRange(0x2300, 0x23ff,"Miscellaneous Technical"),
			new UnicodeRange(0x2500, 0x25ff,"Box Drawing & Geometric Shapes"),
			new UnicodeRange(0x2600, 0x26ff,"Miscellaneous Symbols"),
			new UnicodeRange(0x2700, 0x27ff,"Dingbats"),
			new UnicodeRange(0x2800, 0x28ff,"Braille"),
			new UnicodeRange(0x2b00, 0x2bff,"Miscellaneous Symbols and Arrows"),
			new UnicodeRange(0xFB00, 0xFb4f,"Alphabetic Presentation Forms"),
			new UnicodeRange(0x12400, 0x1240f,"Cuneiform Numbers and Punctuation"),
			new UnicodeRange(0x1FA00, 0x1FA0f,"Chess Symbols"),
			new UnicodeRange((uint)(CharMap.MaxCodePoint - 16), (uint)CharMap.MaxCodePoint,"End"),

			new UnicodeRange (0x0020 ,0x007F        ,"Basic Latin"),
			new UnicodeRange (0x00A0 ,0x00FF        ,"Latin-1 Supplement"),
			new UnicodeRange (0x0100 ,0x017F        ,"Latin Extended-A"),
			new UnicodeRange (0x0180 ,0x024F        ,"Latin Extended-B"),
			new UnicodeRange (0x0250 ,0x02AF        ,"IPA Extensions"),
			new UnicodeRange (0x02B0 ,0x02FF        ,"Spacing Modifier Letters"),
			new UnicodeRange (0x0300 ,0x036F        ,"Combining Diacritical Marks"),
			new UnicodeRange (0x0370 ,0x03FF        ,"Greek and Coptic"),
			new UnicodeRange (0x0400 ,0x04FF        ,"Cyrillic"),
			new UnicodeRange (0x0500 ,0x052F        ,"Cyrillic Supplementary"),
			new UnicodeRange (0x0530 ,0x058F        ,"Armenian"),
			new UnicodeRange (0x0590 ,0x05FF        ,"Hebrew"),
			new UnicodeRange (0x0600 ,0x06FF        ,"Arabic"),
			new UnicodeRange (0x0700 ,0x074F        ,"Syriac"),
			new UnicodeRange (0x0780 ,0x07BF        ,"Thaana"),
			new UnicodeRange (0x0900 ,0x097F        ,"Devanagari"),
			new UnicodeRange (0x0980 ,0x09FF        ,"Bengali"),
			new UnicodeRange (0x0A00 ,0x0A7F        ,"Gurmukhi"),
			new UnicodeRange (0x0A80 ,0x0AFF        ,"Gujarati"),
			new UnicodeRange (0x0B00 ,0x0B7F        ,"Oriya"),
			new UnicodeRange (0x0B80 ,0x0BFF        ,"Tamil"),
			new UnicodeRange (0x0C00 ,0x0C7F        ,"Telugu"),
			new UnicodeRange (0x0C80 ,0x0CFF        ,"Kannada"),
			new UnicodeRange (0x0D00 ,0x0D7F        ,"Malayalam"),
			new UnicodeRange (0x0D80 ,0x0DFF        ,"Sinhala"),
			new UnicodeRange (0x0E00 ,0x0E7F        ,"Thai"),
			new UnicodeRange (0x0E80 ,0x0EFF        ,"Lao"),
			new UnicodeRange (0x0F00 ,0x0FFF        ,"Tibetan"),
			new UnicodeRange (0x1000 ,0x109F        ,"Myanmar"),
			new UnicodeRange (0x10A0 ,0x10FF        ,"Georgian"),
			new UnicodeRange (0x1100 ,0x11FF        ,"Hangul Jamo"),
			new UnicodeRange (0x1200 ,0x137F        ,"Ethiopic"),
			new UnicodeRange (0x13A0 ,0x13FF        ,"Cherokee"),
			new UnicodeRange (0x1400 ,0x167F        ,"Unified Canadian Aboriginal Syllabics"),
			new UnicodeRange (0x1680 ,0x169F        ,"Ogham"),
			new UnicodeRange (0x16A0 ,0x16FF        ,"Runic"),
			new UnicodeRange (0x1700 ,0x171F        ,"Tagalog"),
			new UnicodeRange (0x1720 ,0x173F        ,"Hanunoo"),
			new UnicodeRange (0x1740 ,0x175F        ,"Buhid"),
			new UnicodeRange (0x1760 ,0x177F        ,"Tagbanwa"),
			new UnicodeRange (0x1780 ,0x17FF        ,"Khmer"),
			new UnicodeRange (0x1800 ,0x18AF        ,"Mongolian"),
			new UnicodeRange (0x1900 ,0x194F        ,"Limbu"),
			new UnicodeRange (0x1950 ,0x197F        ,"Tai Le"),
			new UnicodeRange (0x19E0 ,0x19FF        ,"Khmer Symbols"),
			new UnicodeRange (0x1D00 ,0x1D7F        ,"Phonetic Extensions"),
			new UnicodeRange (0x1E00 ,0x1EFF        ,"Latin Extended Additional"),
			new UnicodeRange (0x1F00 ,0x1FFF        ,"Greek Extended"),
			new UnicodeRange (0x2000 ,0x206F        ,"General Punctuation"),
			new UnicodeRange (0x2070 ,0x209F        ,"Superscripts and Subscripts"),
			new UnicodeRange (0x20A0 ,0x20CF        ,"Currency Symbols"),
			new UnicodeRange (0x20D0 ,0x20FF        ,"Combining Diacritical Marks for Symbols"),
			new UnicodeRange (0x2100 ,0x214F        ,"Letterlike Symbols"),
			new UnicodeRange (0x2150 ,0x218F        ,"Number Forms"),
			new UnicodeRange (0x2190 ,0x21FF        ,"Arrows"),
			new UnicodeRange (0x2200 ,0x22FF        ,"Mathematical Operators"),
			new UnicodeRange (0x2300 ,0x23FF        ,"Miscellaneous Technical"),
			new UnicodeRange (0x2400 ,0x243F        ,"Control Pictures"),
			new UnicodeRange (0x2440 ,0x245F        ,"Optical Character Recognition"),
			new UnicodeRange (0x2460 ,0x24FF        ,"Enclosed Alphanumerics"),
			new UnicodeRange (0x2500 ,0x257F        ,"Box Drawing"),
			new UnicodeRange (0x2580 ,0x259F        ,"Block Elements"),
			new UnicodeRange (0x25A0 ,0x25FF        ,"Geometric Shapes"),
			new UnicodeRange (0x2600 ,0x26FF        ,"Miscellaneous Symbols"),
			new UnicodeRange (0x2700 ,0x27BF        ,"Dingbats"),
			new UnicodeRange (0x27C0 ,0x27EF        ,"Miscellaneous Mathematical Symbols-A"),
			new UnicodeRange (0x27F0 ,0x27FF        ,"Supplemental Arrows-A"),
			new UnicodeRange (0x2800 ,0x28FF        ,"Braille Patterns"),
			new UnicodeRange (0x2900 ,0x297F        ,"Supplemental Arrows-B"),
			new UnicodeRange (0x2980 ,0x29FF        ,"Miscellaneous Mathematical Symbols-B"),
			new UnicodeRange (0x2A00 ,0x2AFF        ,"Supplemental Mathematical Operators"),
			new UnicodeRange (0x2B00 ,0x2BFF        ,"Miscellaneous Symbols and Arrows"),
			new UnicodeRange (0x2E80 ,0x2EFF        ,"CJK Radicals Supplement"),
			new UnicodeRange (0x2F00 ,0x2FDF        ,"Kangxi Radicals"),
			new UnicodeRange (0x2FF0 ,0x2FFF        ,"Ideographic Description Characters"),
			new UnicodeRange (0x3000 ,0x303F        ,"CJK Symbols and Punctuation"),
			new UnicodeRange (0x3040 ,0x309F        ,"Hiragana"),
			new UnicodeRange (0x30A0 ,0x30FF        ,"Katakana"),
			new UnicodeRange (0x3100 ,0x312F        ,"Bopomofo"),
			new UnicodeRange (0x3130 ,0x318F        ,"Hangul Compatibility Jamo"),
			new UnicodeRange (0x3190 ,0x319F        ,"Kanbun"),
			new UnicodeRange (0x31A0 ,0x31BF        ,"Bopomofo Extended"),
			new UnicodeRange (0x31F0 ,0x31FF        ,"Katakana Phonetic Extensions"),
			new UnicodeRange (0x3200 ,0x32FF        ,"Enclosed CJK Letters and Months"),
			new UnicodeRange (0x3300 ,0x33FF        ,"CJK Compatibility"),
			new UnicodeRange (0x3400 ,0x4DBF        ,"CJK Unified Ideographs Extension A"),
			new UnicodeRange (0x4DC0 ,0x4DFF        ,"Yijing Hexagram Symbols"),
			new UnicodeRange (0x4E00 ,0x9FFF        ,"CJK Unified Ideographs"),
			new UnicodeRange (0xA000 ,0xA48F        ,"Yi Syllables"),
			new UnicodeRange (0xA490 ,0xA4CF        ,"Yi Radicals"),
			new UnicodeRange (0xAC00 ,0xD7AF        ,"Hangul Syllables"),
			new UnicodeRange (0xD800 ,0xDB7F        ,"High Surrogates"),
			new UnicodeRange (0xDB80 ,0xDBFF        ,"High Private Use Surrogates"),
			new UnicodeRange (0xDC00 ,0xDFFF        ,"Low Surrogates"),
			new UnicodeRange (0xE000 ,0xF8FF        ,"Private Use Area"),
			new UnicodeRange (0xF900 ,0xFAFF        ,"CJK Compatibility Ideographs"),
			new UnicodeRange (0xFB00 ,0xFB4F        ,"Alphabetic Presentation Forms"),
			new UnicodeRange (0xFB50 ,0xFDFF        ,"Arabic Presentation Forms-A"),
			new UnicodeRange (0xFE00 ,0xFE0F        ,"Variation Selectors"),
			new UnicodeRange (0xFE20 ,0xFE2F        ,"Combining Half Marks"),
			new UnicodeRange (0xFE30 ,0xFE4F        ,"CJK Compatibility Forms"),
			new UnicodeRange (0xFE50 ,0xFE6F        ,"Small Form Variants"),
			new UnicodeRange (0xFE70 ,0xFEFF        ,"Arabic Presentation Forms-B"),
			new UnicodeRange (0xFF00 ,0xFFEF        ,"Halfwidth and Fullwidth Forms"),
			new UnicodeRange (0xFFF0 ,0xFFFF        ,"Specials"),
			new UnicodeRange (0x10000, 0x1007F   ,"Linear B Syllabary"),
			new UnicodeRange (0x10080, 0x100FF   ,"Linear B Ideograms"),
			new UnicodeRange (0x10100, 0x1013F   ,"Aegean Numbers"),
			new UnicodeRange (0x10300, 0x1032F   ,"Old Italic"),
			new UnicodeRange (0x10330, 0x1034F   ,"Gothic"),
			new UnicodeRange (0x10380, 0x1039F   ,"Ugaritic"),
			new UnicodeRange (0x10400, 0x1044F   ,"Deseret"),
			new UnicodeRange (0x10450, 0x1047F   ,"Shavian"),
			new UnicodeRange (0x10480, 0x104AF   ,"Osmanya"),
			new UnicodeRange (0x10800, 0x1083F   ,"Cypriot Syllabary"),
			new UnicodeRange (0x1D000, 0x1D0FF   ,"Byzantine Musical Symbols"),
			new UnicodeRange (0x1D100, 0x1D1FF   ,"Musical Symbols"),
			new UnicodeRange (0x1D300, 0x1D35F   ,"Tai Xuan Jing Symbols"),
			new UnicodeRange (0x1D400, 0x1D7FF   ,"Mathematical Alphanumeric Symbols"),
			new UnicodeRange (0x1F600, 0x1F532   ,"Emojis Symbols"),
			new UnicodeRange (0x20000, 0x2A6DF   ,"CJK Unified Ideographs Extension B"),
			new UnicodeRange (0x2F800, 0x2FA1F   ,"CJK Compatibility Ideographs Supplement"),
			new UnicodeRange (0xE0000, 0xE007F   ,"Tags"),
		};
		private void SetDemoTableStyles ()
		{
			tableView.Style.ColumnStyles.Clear();

			var alignMid = new ColumnStyle () {
				Alignment = TextAlignment.Centered
			};
			var alignRight = new ColumnStyle () {
				Alignment = TextAlignment.Right
			};

			var dateFormatStyle = new ColumnStyle () {
				Alignment = TextAlignment.Right,
				RepresentationGetter = (v) => v is DateTime d ? d.ToString ("yyyy-MM-dd") : v.ToString ()
			};

			var negativeRight = new ColumnStyle () {

				Format = "0.##",
				MinWidth = 10,
				AlignmentGetter = (v) => v is double d ?
								// align negative values right
								d < 0 ? TextAlignment.Right :
								// align positive values left
								TextAlignment.Left :
								// not a double
								TextAlignment.Left,

				ColorGetter = (a) => a.CellValue is double d ?
								// color 0 and negative values red
								d <= 0.0000001 ? a.RowIndex % 2 == 0 && _miAlternatingColors.Checked == true ? redColorSchemeAlt : redColorScheme :
								// use normal scheme for positive values
								null :
								// not a double
								null
			};

			tableView.Style.ColumnStyles.Add (currentTable.Columns ["DateCol"].Ordinal, dateFormatStyle);
			tableView.Style.ColumnStyles.Add (currentTable.Columns ["DoubleCol"].Ordinal, negativeRight);
			tableView.Style.ColumnStyles.Add (currentTable.Columns ["NullsCol"].Ordinal, alignMid);
			tableView.Style.ColumnStyles.Add (currentTable.Columns ["IntCol"].Ordinal, alignRight);

			tableView.Update ();
		}

		private void OpenSimple (bool big)
		{
			SetTable(BuildSimpleDataTable (big ? 30 : 5, big ? 1000 : 5));
		}

		private void EditCurrentCell (object sender, CellActivatedEventArgs e)
		{
			if (e.Table == null)
				return;

			var tableCol = ToTableCol(e.Col);
			if (tableCol < 0) {
				return;
			}

			var o = currentTable.Rows [e.Row] [tableCol];

			var title = o is uint u ? GetUnicodeCategory (u) + $"(0x{o:X4})" : "Enter new value";

			var oldValue = currentTable.Rows [e.Row] [tableCol].ToString ();
			bool okPressed = false;

			var ok = new Button ("Ok", is_default: true);
			ok.Clicked += (s, e) => { okPressed = true; Application.RequestStop (); };
			var cancel = new Button ("Cancel");
			cancel.Clicked += (s,e) => { Application.RequestStop (); };
			var d = new Dialog (ok, cancel) { Title = title };

			var lbl = new Label () {
				X = 0,
				Y = 1,
				Text = tableView.Table.ColumnNames[e.Col]
			};

			var tf = new TextField () {
				Text = oldValue,
				X = 0,
				Y = 2,
				Width = Dim.Fill ()
			};

			d.Add (lbl, tf);
			tf.SetFocus ();

			Application.Run (d);

			if (okPressed) {

				try {
					currentTable.Rows [e.Row] [tableCol] = string.IsNullOrWhiteSpace (tf.Text) ? DBNull.Value : (object)tf.Text;
				} catch (Exception ex) {
					MessageBox.ErrorQuery (60, 20, "Failed to set text", ex.Message, "Ok");
				}

				tableView.Update ();
			}
		}

		private int ToTableCol (int col)
		{
			if (HasCheckboxes ()) {
				return col - 1;
			}

			return col;
		}

		private bool HasCheckboxes ()
		{
			return tableView.Table is CheckBoxTableSourceWrapperBase;
		}

		private string GetUnicodeCategory (uint u)
		{
			return Ranges.FirstOrDefault (r => u >= r.Start && u <= r.End)?.Category ?? "Unknown";
		}

		/// <summary>
		/// Generates a new demo <see cref="DataTable"/> with the given number of <paramref name="cols"/> (min 5) and <paramref name="rows"/>
		/// </summary>
		/// <param name="cols"></param>
		/// <param name="rows"></param>
		/// <returns></returns>
		public static DataTable BuildDemoDataTable (int cols, int rows)
		{
			var dt = new DataTable ();

			int explicitCols = 6;
			dt.Columns.Add (new DataColumn ("StrCol", typeof (string)));
			dt.Columns.Add (new DataColumn ("DateCol", typeof (DateTime)));
			dt.Columns.Add (new DataColumn ("IntCol", typeof (int)));
			dt.Columns.Add (new DataColumn ("DoubleCol", typeof (double)));
			dt.Columns.Add (new DataColumn ("NullsCol", typeof (string)));
			dt.Columns.Add (new DataColumn ("Unicode", typeof (string)));

			for (int i = 0; i < cols - explicitCols; i++) {
				dt.Columns.Add ("Column" + (i + explicitCols));
			}

			var r = new Random (100);

			for (int i = 0; i < rows; i++) {

				List<object> row = new List<object> (){
					"Some long text that is super cool",
					new DateTime(2000+i,12,25),
					r.Next(i),
					(r.NextDouble()*i)-0.5 /*add some negatives to demo styles*/,
					DBNull.Value,
					"Les Mise" + Char.ConvertFromUtf32(Int32.Parse("0301", NumberStyles.HexNumber)) + "rables"
				};

				for (int j = 0; j < cols - explicitCols; j++) {
					row.Add ("SomeValue" + r.Next (100));
				}

				dt.Rows.Add (row.ToArray ());
			}

			return dt;
		}

		/// <summary>
		/// Builds a simple table in which cell values contents are the index of the cell.  This helps testing that scrolling etc is working correctly and not skipping out any rows/columns when paging
		/// </summary>
		/// <param name="cols"></param>
		/// <param name="rows"></param>
		/// <returns></returns>
		public static DataTable BuildSimpleDataTable (int cols, int rows)
		{
			var dt = new DataTable ();

			for (int c = 0; c < cols; c++) {
				dt.Columns.Add ("Col" + c);
			}

			for (int r = 0; r < rows; r++) {
				var newRow = dt.NewRow ();

				for (int c = 0; c < cols; c++) {
					newRow [c] = $"R{r}C{c}";
				}

				dt.Rows.Add (newRow);
			}

			return dt;
		}
	}
}
