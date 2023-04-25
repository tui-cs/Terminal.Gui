using System;
using System.Data;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NStack;
using Terminal.Gui;
using CsvHelper;
using System.Collections.Generic;

namespace UICatalog.Scenarios {

	[ScenarioMetadata (Name: "Csv Editor", Description: "Open and edit simple CSV files using the TableView class.")]
	[ScenarioCategory ("TableView")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Dialogs")]
	[ScenarioCategory ("Text and Formatting")]
	[ScenarioCategory ("Dialogs")]
	[ScenarioCategory ("Top Level Windows")]
	[ScenarioCategory ("Files and IO")]
	public class CsvEditor : Scenario {
		TableView tableView;
		private string currentFile;
		private MenuItem miLeft;
		private MenuItem miRight;
		private MenuItem miCentered;
		private TextField selectedCellLabel;

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

			var fileMenu = new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_Open CSV", "", () => Open()),
					new MenuItem ("_Save", "", () => Save()),
					new MenuItem ("_Quit", "Quits The App", () => Quit()),
				});
			//fileMenu.Help = "Help";
			var menu = new MenuBar (new MenuBarItem [] {
				fileMenu,
				new MenuBarItem ("_Edit", new MenuItem [] {
					new MenuItem ("_New Column", "", () => AddColumn()),
					new MenuItem ("_New Row", "", () => AddRow()),
					new MenuItem ("_Rename Column", "", () => RenameColumn()),
					new MenuItem ("_Delete Column", "", () => DeleteColum()),
					new MenuItem ("_Move Column", "", () => MoveColumn()),
					new MenuItem ("_Move Row", "", () => MoveRow()),
					new MenuItem ("_Sort Asc", "", () => Sort(true)),
					new MenuItem ("_Sort Desc", "", () => Sort(false)),
				}),
				new MenuBarItem ("_View", new MenuItem [] {
					miLeft = new MenuItem ("_Align Left", "", () => Align(TextAlignment.Left)),
					miRight = new MenuItem ("_Align Right", "", () => Align(TextAlignment.Right)),
					miCentered = new MenuItem ("_Align Centered", "", () => Align(TextAlignment.Centered)),
					
					// Format requires hard typed data table, when we read a CSV everything is untyped (string) so this only works for new columns in this demo
					miCentered = new MenuItem ("_Set Format Pattern", "", () => SetFormat()),
				})
			});
			Application.Top.Add (menu);

			var statusBar = new StatusBar (new StatusItem [] {
				new StatusItem(Key.CtrlMask | Key.O, "~^O~ Open", () => Open()),
				new StatusItem(Key.CtrlMask | Key.S, "~^S~ Save", () => Save()),
				new StatusItem(Application.QuitKey, $"{Application.QuitKey} to Quit", () => Quit()),
			});
			Application.Top.Add (statusBar);

			Win.Add (tableView);

			selectedCellLabel = new TextField () {
				X = 0,
				Y = Pos.Bottom (tableView),
				Text = "0,0",
				Width = Dim.Fill (),
				TextAlignment = TextAlignment.Right
			};
			selectedCellLabel.TextChanged += SelectedCellLabel_TextChanged;

			Win.Add (selectedCellLabel);

			tableView.SelectedCellChanged += OnSelectedCellChanged;
			tableView.CellActivated += EditCurrentCell;
			tableView.KeyPress += TableViewKeyPress;

			SetupScrollBar ();
		}

		private void SelectedCellLabel_TextChanged (object sender, TextChangedEventArgs e)
		{
			// if user is in the text control and editing the selected cell
			if (!selectedCellLabel.HasFocus)
				return;

			// change selected cell to the one the user has typed into the box
			var match = Regex.Match (selectedCellLabel.Text.ToString (), "^(\\d+),(\\d+)$");
			if (match.Success) {

				tableView.SelectedColumn = int.Parse (match.Groups [1].Value);
				tableView.SelectedRow = int.Parse (match.Groups [2].Value);
			}
		}

		private void OnSelectedCellChanged (object sender, SelectedCellChangedEventArgs e)
		{
			// only update the text box if the user is not manually editing it
			if (!selectedCellLabel.HasFocus)
				selectedCellLabel.Text = $"{tableView.SelectedRow},{tableView.SelectedColumn}";

			if (tableView.Table == null || tableView.SelectedColumn == -1)
				return;

			var col = tableView.Table.Columns [tableView.SelectedColumn];

			var style = tableView.Style.GetColumnStyleIfAny (col);

			miLeft.Checked = style?.Alignment == TextAlignment.Left;
			miRight.Checked = style?.Alignment == TextAlignment.Right;
			miCentered.Checked = style?.Alignment == TextAlignment.Centered;
		}

		private void RenameColumn ()
		{
			if (NoTableLoaded ()) {
				return;
			}

			var currentCol = tableView.Table.Columns [tableView.SelectedColumn];

			if (GetText ("Rename Column", "Name:", currentCol.ColumnName, out string newName)) {
				currentCol.ColumnName = newName;
				tableView.Update ();
			}
		}

		private void DeleteColum ()
		{
			if (NoTableLoaded ()) {
				return;
			}

			if (tableView.SelectedColumn == -1) {

				MessageBox.ErrorQuery ("No Column", "No column selected", "Ok");
				return;
			}

			try {
				tableView.Table.Columns.RemoveAt (tableView.SelectedColumn);
				tableView.Update ();

			} catch (Exception ex) {
				MessageBox.ErrorQuery ("Could not remove column", ex.Message, "Ok");
			}
		}

		private void MoveColumn ()
		{
			if (NoTableLoaded ()) {
				return;
			}

			if (tableView.SelectedColumn == -1) {

				MessageBox.ErrorQuery ("No Column", "No column selected", "Ok");
				return;
			}

			try {

				var currentCol = tableView.Table.Columns [tableView.SelectedColumn];

				if (GetText ("Move Column", "New Index:", currentCol.Ordinal.ToString (), out string newOrdinal)) {

					var newIdx = Math.Min (Math.Max (0, int.Parse (newOrdinal)), tableView.Table.Columns.Count - 1);

					currentCol.SetOrdinal (newIdx);

					tableView.SetSelection (newIdx, tableView.SelectedRow, false);
					tableView.EnsureSelectedCellIsVisible ();
					tableView.SetNeedsDisplay ();
				}

			} catch (Exception ex) {
				MessageBox.ErrorQuery ("Error moving column", ex.Message, "Ok");
			}
		}
		private void Sort (bool asc)
		{

			if (NoTableLoaded ()) {
				return;
			}

			if (tableView.SelectedColumn == -1) {

				MessageBox.ErrorQuery ("No Column", "No column selected", "Ok");
				return;
			}

			var colName = tableView.Table.Columns [tableView.SelectedColumn].ColumnName;

			tableView.Table.DefaultView.Sort = colName + (asc ? " asc" : " desc");
			tableView.Table = tableView.Table.DefaultView.ToTable ();
		}

		private void MoveRow ()
		{
			if (NoTableLoaded ()) {
				return;
			}

			if (tableView.SelectedRow == -1) {

				MessageBox.ErrorQuery ("No Rows", "No row selected", "Ok");
				return;
			}

			try {

				int oldIdx = tableView.SelectedRow;

				var currentRow = tableView.Table.Rows [oldIdx];

				if (GetText ("Move Row", "New Row:", oldIdx.ToString (), out string newOrdinal)) {

					var newIdx = Math.Min (Math.Max (0, int.Parse (newOrdinal)), tableView.Table.Rows.Count - 1);

					if (newIdx == oldIdx)
						return;

					var arrayItems = currentRow.ItemArray;
					tableView.Table.Rows.Remove (currentRow);

					// Removing and Inserting the same DataRow seems to result in it loosing its values so we have to create a new instance
					var newRow = tableView.Table.NewRow ();
					newRow.ItemArray = arrayItems;

					tableView.Table.Rows.InsertAt (newRow, newIdx);

					tableView.SetSelection (tableView.SelectedColumn, newIdx, false);
					tableView.EnsureSelectedCellIsVisible ();
					tableView.SetNeedsDisplay ();
				}

			} catch (Exception ex) {
				MessageBox.ErrorQuery ("Error moving column", ex.Message, "Ok");
			}
		}

		private void Align (TextAlignment newAlignment)
		{
			if (NoTableLoaded ()) {
				return;
			}

			var col = tableView.Table.Columns [tableView.SelectedColumn];

			var style = tableView.Style.GetOrCreateColumnStyle (col);
			style.Alignment = newAlignment;

			miLeft.Checked = style.Alignment == TextAlignment.Left;
			miRight.Checked = style.Alignment == TextAlignment.Right;
			miCentered.Checked = style.Alignment == TextAlignment.Centered;

			tableView.Update ();
		}

		private void SetFormat ()
		{
			if (NoTableLoaded ()) {
				return;
			}

			var col = tableView.Table.Columns [tableView.SelectedColumn];

			if (col.DataType == typeof (string)) {
				MessageBox.ErrorQuery ("Cannot Format Column", "String columns cannot be Formatted, try adding a new column to the table with a date/numerical Type", "Ok");
				return;
			}

			var style = tableView.Style.GetOrCreateColumnStyle (col);

			if (GetText ("Format", "Pattern:", style.Format ?? "", out string newPattern)) {
				style.Format = newPattern;
				tableView.Update ();
			}
		}

		private bool NoTableLoaded ()
		{
			if (tableView.Table == null) {
				MessageBox.ErrorQuery ("No Table Loaded", "No table has currently be opened", "Ok");
				return true;
			}

			return false;
		}

		private void AddRow ()
		{
			if (NoTableLoaded ()) {
				return;
			}

			var newRow = tableView.Table.NewRow ();

			var newRowIdx = Math.Min (Math.Max (0, tableView.SelectedRow + 1), tableView.Table.Rows.Count);

			tableView.Table.Rows.InsertAt (newRow, newRowIdx);
			tableView.Update ();
		}

		private void AddColumn ()
		{
			if (NoTableLoaded ()) {
				return;
			}

			if (GetText ("Enter column name", "Name:", "", out string colName)) {

				var col = new DataColumn (colName);

				var newColIdx = Math.Min (Math.Max (0, tableView.SelectedColumn + 1), tableView.Table.Columns.Count);

				int result = MessageBox.Query ("Column Type", "Pick a data type for the column", new ustring [] { "Date", "Integer", "Double", "Text", "Cancel" });

				if (result <= -1 || result >= 4)
					return;
				switch (result) {
				case 0:
					col.DataType = typeof (DateTime);
					break;
				case 1:
					col.DataType = typeof (int);
					break;
				case 2:
					col.DataType = typeof (double);
					break;
				case 3:
					col.DataType = typeof (string);
					break;
				}

				tableView.Table.Columns.Add (col);
				col.SetOrdinal (newColIdx);
				tableView.Update ();
			}


		}

		private void Save ()
		{
			if (tableView.Table == null || string.IsNullOrWhiteSpace (currentFile)) {
				MessageBox.ErrorQuery ("No file loaded", "No file is currently loaded", "Ok");
				return;
			}
			using var writer = new CsvWriter (
				new StreamWriter (File.OpenWrite (currentFile)),
				CultureInfo.InvariantCulture);

			foreach (var col in tableView.Table.Columns.Cast<DataColumn> ().Select (c => c.ColumnName)) {
				writer.WriteField (col);
			}

			writer.NextRecord ();

			foreach (DataRow row in tableView.Table.Rows) {
				foreach (var item in row.ItemArray) {
					writer.WriteField (item);
				}
				writer.NextRecord ();
			}

		}
		
		private void Open ()
		{
			var ofd = new FileDialog () {
				AllowedTypes = new List<IAllowedType> { new AllowedType("Comma Separated Values", ".csv") }
			};
			ofd.Style.OkButtonText = "Open";

			Application.Run (ofd);

			if (!ofd.Canceled && !string.IsNullOrWhiteSpace (ofd.Path?.ToString ())) {
				Open (ofd.Path.ToString ());
			}
		}

		private void Open (string filename)
		{

			int lineNumber = 0;
			currentFile = null;

			try {
				using var reader = new CsvReader (File.OpenText (filename), CultureInfo.InvariantCulture);

				var dt = new DataTable ();

				reader.Read ();

				if (reader.ReadHeader ()) {
					foreach (var h in reader.HeaderRecord) {
						dt.Columns.Add (h);
					}
				}

				while (reader.Read ()) {
					lineNumber++;

					var newRow = dt.Rows.Add ();
					for (int i = 0; i < dt.Columns.Count; i++) {
						newRow [i] = reader [i];
					}
				}

				tableView.Table = dt;

				// Only set the current filename if we successfully loaded the entire file
				currentFile = filename;
				Win.Title = $"{this.GetName ()} - {Path.GetFileName (currentFile)}";

			} catch (Exception ex) {
				MessageBox.ErrorQuery ("Open Failed", $"Error on line {lineNumber}{Environment.NewLine}{ex.Message}", "Ok");
			}
		}
		private void SetupScrollBar ()
		{
			var _scrollBar = new ScrollBarView (tableView, true);

			_scrollBar.ChangedPosition += (s, e) => {
				tableView.RowOffset = _scrollBar.Position;
				if (tableView.RowOffset != _scrollBar.Position) {
					_scrollBar.Position = tableView.RowOffset;
				}
				tableView.SetNeedsDisplay ();
			};
			/*
			_scrollBar.OtherScrollBarView.ChangedPosition += (s,e) => {
				_listView.LeftItem = _scrollBar.OtherScrollBarView.Position;
				if (_listView.LeftItem != _scrollBar.OtherScrollBarView.Position) {
					_scrollBar.OtherScrollBarView.Position = _listView.LeftItem;
				}
				_listView.SetNeedsDisplay ();
			};*/

			tableView.DrawContent += (s, e) => {
				_scrollBar.Size = tableView.Table?.Rows?.Count ?? 0;
				_scrollBar.Position = tableView.RowOffset;
				//	_scrollBar.OtherScrollBarView.Size = _listView.Maxlength - 1;
				//	_scrollBar.OtherScrollBarView.Position = _listView.LeftItem;
				_scrollBar.Refresh ();
			};

		}

		private void TableViewKeyPress (object sender, KeyEventEventArgs e)
		{
			if (e.KeyEvent.Key == Key.DeleteChar) {

				if (tableView.FullRowSelect) {
					// Delete button deletes all rows when in full row mode
					foreach (int toRemove in tableView.GetAllSelectedCells ().Select (p => p.Y).Distinct ().OrderByDescending (i => i))
						tableView.Table.Rows.RemoveAt (toRemove);
				} else {

					// otherwise set all selected cells to null
					foreach (var pt in tableView.GetAllSelectedCells ()) {
						tableView.Table.Rows [pt.Y] [pt.X] = DBNull.Value;
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

		private void CloseExample ()
		{
			tableView.Table = null;
		}

		private void Quit ()
		{
			Application.RequestStop ();
		}
		private bool GetText (string title, string label, string initialText, out string enteredText)
		{
			bool okPressed = false;

			var ok = new Button ("Ok", is_default: true);
			ok.Clicked += (s, e) => { okPressed = true; Application.RequestStop (); };
			var cancel = new Button ("Cancel");
			cancel.Clicked += (s, e) => { Application.RequestStop (); };
			var d = new Dialog (ok, cancel) { Title = title };

			var lbl = new Label () {
				X = 0,
				Y = 1,
				Text = label
			};

			var tf = new TextField () {
				Text = initialText,
				X = 0,
				Y = 2,
				Width = Dim.Fill ()
			};

			d.Add (lbl, tf);
			tf.SetFocus ();

			Application.Run (d);

			enteredText = okPressed ? tf.Text.ToString () : null;
			return okPressed;
		}
		private void EditCurrentCell (object sender, CellActivatedEventArgs e)
		{
			if (e.Table == null)
				return;

			var oldValue = e.Table.Rows [e.Row] [e.Col].ToString ();

			if (GetText ("Enter new value", e.Table.Columns [e.Col].ColumnName, oldValue, out string newText)) {
				try {
					e.Table.Rows [e.Row] [e.Col] = string.IsNullOrWhiteSpace (newText) ? DBNull.Value : (object)newText;
				} catch (Exception ex) {
					MessageBox.ErrorQuery (60, 20, "Failed to set text", ex.Message, "Ok");
				}

				tableView.Update ();
			}
		}
	}
}
