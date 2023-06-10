using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using Terminal.Gui;
using static Terminal.Gui.TableView;

namespace UICatalog.Scenarios {

	[ScenarioMetadata (Name: "ListColumns", Description: "Implements a columned list via a data table.")]
	[ScenarioCategory ("TableView")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Dialogs")]
	[ScenarioCategory ("Text and Formatting")]
	[ScenarioCategory ("Top Level Windows")]
	public class ListColumns : Scenario {
		TableView listColView;
		DataTable currentTable;
		private MenuItem _miCellLines;
		private MenuItem _miExpandLastColumn;
		private MenuItem _miAlwaysUseNormalColorForVerticalCellLines;
		private MenuItem _miSmoothScrolling;
		private MenuItem _miAlternatingColors;
		private MenuItem _miCursor;
		private MenuItem _miTopline;
		private MenuItem _miBottomline;
		private MenuItem _miOrientVertical;
		private MenuItem _miScrollParallel;

		ColorScheme alternatingColorScheme;

		public override void Setup ()
		{
			Win.Title = this.GetName ();
			Win.Y = 1; // menu
			Win.Height = Dim.Fill (1); // status bar
			Application.Top.LayoutSubviews ();

			this.listColView = new TableView () {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (1),
				Style = new TableStyle {
					ShowHeaders = false,
					ShowHorizontalHeaderOverline = false,
					ShowHorizontalHeaderUnderline = false,
					ShowHorizontalBottomline = false,
					ExpandLastColumn = false,
				}
			};
			var listColStyle = new ListColumnStyle ();

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("Open_BigListExample", "", () => OpenSimpleList (true)),
					new MenuItem ("Open_SmListExample", "", () => OpenSimpleList (false)),
					new MenuItem ("_CloseExample", "", () => CloseExample ()),
					new MenuItem ("_Quit", "", () => Quit()),
				}),
				new MenuBarItem ("_View", new MenuItem [] {
					_miTopline = new MenuItem ("_TopLine", "", () => ToggleTopline ()) { Checked = listColView.Style.ShowHorizontalHeaderOverline, CheckType = MenuItemCheckStyle.Checked },
					_miBottomline = new MenuItem ("_BottomLine", "", () => ToggleBottomline ()) { Checked = listColView.Style.ShowHorizontalBottomline, CheckType = MenuItemCheckStyle.Checked },
					_miCellLines = new MenuItem ("_CellLines", "", () => ToggleCellLines ()) { Checked = listColView.Style.ShowVerticalCellLines, CheckType = MenuItemCheckStyle.Checked },
					_miExpandLastColumn = new MenuItem ("_ExpandLastColumn", "", () => ToggleExpandLastColumn ()) { Checked = listColView.Style.ExpandLastColumn, CheckType = MenuItemCheckStyle.Checked },
					_miAlwaysUseNormalColorForVerticalCellLines = new MenuItem ("_AlwaysUseNormalColorForVerticalCellLines", "", () => ToggleAlwaysUseNormalColorForVerticalCellLines ()) { Checked = listColView.Style.AlwaysUseNormalColorForVerticalCellLines, CheckType = MenuItemCheckStyle.Checked },
					_miSmoothScrolling = new MenuItem ("_SmoothHorizontalScrolling", "", () => ToggleSmoothScrolling ()) { Checked = listColView.Style.SmoothHorizontalScrolling, CheckType = MenuItemCheckStyle.Checked },
					_miAlternatingColors = new MenuItem ("Alternating Colors", "", () => ToggleAlternatingColors ()) { CheckType = MenuItemCheckStyle.Checked},
					_miCursor = new MenuItem ("Invert Selected Cell First Character", "", () => ToggleInvertSelectedCellFirstCharacter ()) { Checked = listColView.Style.InvertSelectedCellFirstCharacter,CheckType = MenuItemCheckStyle.Checked},
				}),
				new MenuBarItem ("_List", new MenuItem [] {
					//new MenuItem ("_Hide Headers", "", HideHeaders),
					_miOrientVertical = new MenuItem ("_OrientVertical", "", () => ToggleVerticalOrientation ()) { Checked = listColStyle.Orientation == Orientation.Vertical, CheckType = MenuItemCheckStyle.Checked },
					_miScrollParallel = new MenuItem ("_ScrollParallel", "", () => ToggleScrollParallel ()) { Checked = listColStyle.ScrollParallel, CheckType = MenuItemCheckStyle.Checked },
					new MenuItem ("Set _Max Cell Width", "", SetListMaxWidth),
					new MenuItem ("Set Mi_n Cell Width", "", SetListMinWidth),
				}),
			});

			Application.Top.Add (menu);

			var statusBar = new StatusBar (new StatusItem [] {
				new StatusItem(Key.F2, "~F2~ OpenBigListEx", () => OpenSimpleList (true)),
				new StatusItem(Key.F3, "~F3~ CloseExample", () => CloseExample ()),
				new StatusItem(Key.F4, "~F4~ OpenSmListEx", () => OpenSimpleList (false)),
				new StatusItem(Application.QuitKey, $"{Application.QuitKey} to Quit", () => Quit()),
			});
			Application.Top.Add (statusBar);

			Win.Add (listColView);

			var selectedCellLabel = new Label () {
				X = 0,
				Y = Pos.Bottom (listColView),
				Text = "0,0",
				Width = Dim.Fill (),
				TextAlignment = TextAlignment.Right

			};

			Win.Add (selectedCellLabel);

			listColView.SelectedCellChanged += (s, e) => { selectedCellLabel.Text = $"{listColView.SelectedRow},{listColView.SelectedColumn}"; };
			listColView.KeyPress += TableViewKeyPress;

			SetupScrollBar ();

			alternatingColorScheme = new ColorScheme () {

				Disabled = Win.ColorScheme.Disabled,
				HotFocus = Win.ColorScheme.HotFocus,
				Focus = Win.ColorScheme.Focus,
				Normal = Application.Driver.MakeAttribute (Color.White, Color.BrightBlue)
			};

			// if user clicks the mouse in TableView
			listColView.MouseClick += (s, e) => {

				listColView.ScreenToCell (e.MouseEvent.X, e.MouseEvent.Y, out int? clickedCol);
			};

			listColView.AddKeyBinding (Key.Space, Command.ToggleChecked);
		}

		private void SetupScrollBar ()
		{
			var scrollBar = new ScrollBarView (listColView, true); // (listColView, true, true);

			scrollBar.ChangedPosition += (s, e) => {
				listColView.RowOffset = scrollBar.Position;
				if (listColView.RowOffset != scrollBar.Position) {
					scrollBar.Position = listColView.RowOffset;
				}
				listColView.SetNeedsDisplay ();
			};
			/*
			scrollBar.OtherScrollBarView.ChangedPosition += (s,e) => {
				listColView.ColumnOffset = scrollBar.OtherScrollBarView.Position;
				if (listColView.ColumnOffset != scrollBar.OtherScrollBarView.Position) {
					scrollBar.OtherScrollBarView.Position = listColView.ColumnOffset;
				}
				listColView.SetNeedsDisplay ();
			};
			*/

			listColView.DrawContent += (s, e) => {
				scrollBar.Size = listColView.Table?.Rows ?? 0;
				scrollBar.Position = listColView.RowOffset;
				//scrollBar.OtherScrollBarView.Size = listColView.Table?.Columns - 1 ?? 0;
				//scrollBar.OtherScrollBarView.Position = listColView.ColumnOffset;
				scrollBar.Refresh ();
			};

		}

		private void TableViewKeyPress (object sender, KeyEventEventArgs e)
		{
			if (e.KeyEvent.Key == Key.DeleteChar) {

				// set all selected cells to null
				foreach (var pt in listColView.GetAllSelectedCells ()) {
					currentTable.Rows [pt.Y] [pt.X] = DBNull.Value;
				}

				listColView.Update ();
				e.Handled = true;
			}

		}

		private void ToggleTopline ()
		{
			_miTopline.Checked = !_miTopline.Checked;
			listColView.Style.ShowHorizontalHeaderOverline = (bool)_miTopline.Checked;
			listColView.Update ();
		}
		private void ToggleBottomline ()
		{
			_miBottomline.Checked = !_miBottomline.Checked;
			listColView.Style.ShowHorizontalBottomline = (bool)_miBottomline.Checked;
			listColView.Update ();
		}
		private void ToggleExpandLastColumn ()
		{
			_miExpandLastColumn.Checked = !_miExpandLastColumn.Checked;
			listColView.Style.ExpandLastColumn = (bool)_miExpandLastColumn.Checked;

			listColView.Update ();

		}

		private void ToggleAlwaysUseNormalColorForVerticalCellLines ()
		{
			_miAlwaysUseNormalColorForVerticalCellLines.Checked = !_miAlwaysUseNormalColorForVerticalCellLines.Checked;
			listColView.Style.AlwaysUseNormalColorForVerticalCellLines = (bool)_miAlwaysUseNormalColorForVerticalCellLines.Checked;

			listColView.Update ();
		}
		private void ToggleSmoothScrolling ()
		{
			_miSmoothScrolling.Checked = !_miSmoothScrolling.Checked;
			listColView.Style.SmoothHorizontalScrolling = (bool)_miSmoothScrolling.Checked;

			listColView.Update ();

		}
		private void ToggleCellLines ()
		{
			_miCellLines.Checked = !_miCellLines.Checked;
			listColView.Style.ShowVerticalCellLines = (bool)_miCellLines.Checked;
			listColView.Update ();
		}
		private void ToggleAlternatingColors ()
		{
			//toggle menu item
			_miAlternatingColors.Checked = !_miAlternatingColors.Checked;

			if (_miAlternatingColors.Checked == true) {
				listColView.Style.RowColorGetter = (a) => { return a.RowIndex % 2 == 0 ? alternatingColorScheme : null; };
			} else {
				listColView.Style.RowColorGetter = null;
			}
			listColView.SetNeedsDisplay ();
		}

		private void ToggleInvertSelectedCellFirstCharacter ()
		{
			//toggle menu item
			_miCursor.Checked = !_miCursor.Checked;
			listColView.Style.InvertSelectedCellFirstCharacter = (bool)_miCursor.Checked;
			listColView.SetNeedsDisplay ();
		}

		private void ToggleVerticalOrientation ()
		{
			_miOrientVertical.Checked = !_miOrientVertical.Checked;
			if ((ListTableSource)listColView.Table != null) {
				((ListTableSource)listColView.Table).Style.Orientation = (bool)_miOrientVertical.Checked ? Orientation.Vertical : Orientation.Horizontal;
				listColView.SetNeedsDisplay ();
			}
		}

		private void ToggleScrollParallel ()
		{
			_miScrollParallel.Checked = !_miScrollParallel.Checked;
			if ((ListTableSource)listColView.Table != null) {
				((ListTableSource)listColView.Table).Style.ScrollParallel = (bool)_miScrollParallel.Checked;
				listColView.SetNeedsDisplay ();
			}
		}

		private void SetListMinWidth ()
		{
			RunListWidthDialog ("MinCellWidth", (s, v) => s.MinCellWidth = v, (s) => s.MinCellWidth);
			listColView.SetNeedsDisplay ();
		}

		private void SetListMaxWidth ()
		{
			RunListWidthDialog ("MaxCellWidth", (s, v) => s.MaxCellWidth = v, (s) => s.MaxCellWidth);
			listColView.SetNeedsDisplay ();
		}

		private void RunListWidthDialog (string prompt, Action<TableView, int> setter, Func<TableView, int> getter)
		{
			var accepted = false;
			var ok = new Button ("Ok", is_default: true);
			ok.Clicked += (s, e) => { accepted = true; Application.RequestStop (); };
			var cancel = new Button ("Cancel");
			cancel.Clicked += (s, e) => { Application.RequestStop (); };
			var d = new Dialog (ok, cancel) { Title = prompt };

			var tf = new TextField () {
				Text = getter (listColView).ToString (),
				X = 0,
				Y = 1,
				Width = Dim.Fill ()
			};

			d.Add (tf);
			tf.SetFocus ();

			Application.Run (d);

			if (accepted) {

				try {
					setter (listColView, int.Parse (tf.Text));
				} catch (Exception ex) {
					MessageBox.ErrorQuery (60, 20, "Failed to set", ex.Message, "Ok");
				}
			}
		}

		private void CloseExample ()
		{
			listColView.Table = null;
		}

		private void Quit ()
		{
			Application.RequestStop ();
		}

		private void OpenSimpleList (bool big)
		{
			SetTable (BuildSimpleList (big ? 1023 : 31));
		}

		private void SetTable (IList list)
		{
			listColView.Table = new ListTableSource (list, listColView);
			if ((ListTableSource)listColView.Table != null) {
				currentTable = ((ListTableSource)listColView.Table).DataTable;
			}
		}

		/// <summary>
		/// Builds a simple list in which values are the index.  This helps testing that scrolling etc is working correctly and not skipping out values when paging
		/// </summary>
		/// <param name="items"></param>
		/// <returns></returns>
		public static IList BuildSimpleList (int items)
		{
			var list = new List<object> ();

			for (int i = 0; i < items; i++) {
				list.Add ("Item " + i);
			}

			return list;
		}
	}
}