using System;
using System.Data;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
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
		private string _currentFile;
		DataTable currentTable;
		private MenuItem _miLeft;
		private MenuItem _miRight;
		private MenuItem _miCentered;
		private TextField _selectedCellLabel;

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
					_miLeft = new MenuItem ("_Align Left", "", () => Align(TextAlignment.Left)),
					_miRight = new MenuItem ("_Align Right", "", () => Align(TextAlignment.Right)),
					_miCentered = new MenuItem ("_Align Centered", "", () => Align(TextAlignment.Centered)),
					
					// Format requires hard typed data table, when we read a CSV everything is untyped (string) so this only works for new columns in this demo
					_miCentered = new MenuItem ("_Set Format Pattern", "", () => SetFormat()),
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

			_selectedCellLabel = new TextField () {
				X = 0,
				Y = Pos.Bottom (tableView),
				Text = "0,0",
				Width = Dim.Fill (),
				TextAlignment = TextAlignment.Right
			};
			_selectedCellLabel.TextChanged += SelectedCellLabel_TextChanged;

			Win.Add (_selectedCellLabel);

			tableView.SelectedCellChanged += OnSelectedCellChanged;
			tableView.CellActivated += EditCurrentCell;
			tableView.KeyPress += TableViewKeyPress;

			SetupScrollBar ();
		}

		private void SelectedCellLabel_TextChanged (object sender, TextChangedEventArgs e)
		{
			// if user is in the text control and editing the selected cell
			if (!_selectedCellLabel.HasFocus)
				return;

			// change selected cell to the one the user has typed into the box
			var match = Regex.Match (_selectedCellLabel.Text, "^(\\d+),(\\d+)$");
			if (match.Success) {

				tableView.SelectedColumn = int.Parse (match.Groups [1].Value);
				tableView.SelectedRow = int.Parse (match.Groups [2].Value);
			}
		}

		private void OnSelectedCellChanged (object sender, SelectedCellChangedEventArgs e)
		{
			// only update the text box if the user is not manually editing it
			if (!_selectedCellLabel.HasFocus)
				_selectedCellLabel.Text = $"{tableView.SelectedRow},{tableView.SelectedColumn}";

			if (tableView.Table == null || tableView.SelectedColumn == -1)
				return;

			var style = tableView.Style.GetColumnStyleIfAny (tableView.SelectedColumn);

			_miLeft.Checked = style?.Alignment == TextAlignment.Left;
			_miRight.Checked = style?.Alignment == TextAlignment.Right;
			_miCentered.Checked = style?.Alignment == TextAlignment.Centered;
		}

		private void RenameColumn ()
		{
			if (NoTableLoaded ()) {
				return;
			}

			var currentCol = currentTable.Columns [tableView.SelectedColumn];

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
				currentTable.Columns.RemoveAt (tableView.SelectedColumn);
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

				var currentCol = currentTable.Columns [tableView.SelectedColumn];

				if (GetText ("Move Column", "New Index:", currentCol.Ordinal.ToString (), out string newOrdinal)) {

					var newIdx = Math.Min (Math.Max (0, int.Parse (newOrdinal)), tableView.Table.Columns - 1);

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

			var colName = tableView.Table.ColumnNames [tableView.SelectedColumn];

			currentTable.DefaultView.Sort = colName + (asc ? " asc" : " desc");
			SetTable(currentTable.DefaultView.ToTable ());
		}

		private void SetTable (DataTable dataTable)
		{			
			tableView.Table = new DataTableSource(currentTable = dataTable);
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

				var currentRow = currentTable.Rows [oldIdx];

				if (GetText ("Move Row", "New Row:", oldIdx.ToString (), out string newOrdinal)) {

					var newIdx = Math.Min (Math.Max (0, int.Parse (newOrdinal)), tableView.Table.Rows - 1);

					if (newIdx == oldIdx)
						return;

					var arrayItems = currentRow.ItemArray;
					currentTable.Rows.Remove (currentRow);

					// Removing and Inserting the same DataRow seems to result in it loosing its values so we have to create a new instance
					var newRow = currentTable.NewRow ();
					newRow.ItemArray = arrayItems;

					currentTable.Rows.InsertAt (newRow, newIdx);

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

			var style = tableView.Style.GetOrCreateColumnStyle (tableView.SelectedColumn);
			style.Alignment = newAlignment;

			_miLeft.Checked = style.Alignment == TextAlignment.Left;
			_miRight.Checked = style.Alignment == TextAlignment.Right;
			_miCentered.Checked = style.Alignment == TextAlignment.Centered;

			tableView.Update ();
		}

		private void SetFormat ()
		{
			if (NoTableLoaded ()) {
				return;
			}

			var col = currentTable.Columns [tableView.SelectedColumn];

			if (col.DataType == typeof (string)) {
				MessageBox.ErrorQuery ("Cannot Format Column", "String columns cannot be Formatted, try adding a new column to the table with a date/numerical Type", "Ok");
				return;
			}

			var style = tableView.Style.GetOrCreateColumnStyle (col.Ordinal);

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

			var newRow = currentTable.NewRow ();

			var newRowIdx = Math.Min (Math.Max (0, tableView.SelectedRow + 1), tableView.Table.Rows);

			currentTable.Rows.InsertAt (newRow, newRowIdx);
			tableView.Update ();
		}

		private void AddColumn ()
		{
			if (NoTableLoaded ()) {
				return;
			}

			if (GetText ("Enter column name", "Name:", "", out string colName)) {

				var col = new DataColumn (colName);

				var newColIdx = Math.Min (Math.Max (0, tableView.SelectedColumn + 1), tableView.Table.Columns);

				int result = MessageBox.Query ("Column Type", "Pick a data type for the column", new string [] { "Date", "Integer", "Double", "Text", "Cancel" });

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

				currentTable.Columns.Add (col);
				col.SetOrdinal (newColIdx);
				tableView.Update ();
			}


		}

		private void Save ()
		{
			if (tableView.Table == null || string.IsNullOrWhiteSpace (_currentFile)) {
				MessageBox.ErrorQuery ("No file loaded", "No file is currently loaded", "Ok");
				return;
			}
			using var writer = new CsvWriter (
				new StreamWriter (File.OpenWrite (_currentFile)),
				CultureInfo.InvariantCulture);

			foreach (var col in currentTable.Columns.Cast<DataColumn> ().Select (c => c.ColumnName)) {
				writer.WriteField (col);
			}

			writer.NextRecord ();

			foreach (DataRow row in currentTable.Rows) {
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

			if (!ofd.Canceled && !string.IsNullOrWhiteSpace (ofd.Path)) {
				Open (ofd.Path);
			}
		}

		private void Open (string filename)
		{

			int lineNumber = 0;
			_currentFile = null;

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

				SetTable(dt);

				// Only set the current filename if we successfully loaded the entire file
				_currentFile = filename;
				Win.Title = $"{this.GetName ()} - {Path.GetFileName (_currentFile)}";

			} catch (Exception ex) {
				MessageBox.ErrorQuery ("Open Failed", $"Error on line {lineNumber}{Environment.NewLine}{ex.Message}", "Ok");
			}
		}
		private void SetupScrollBar ()
		{
			var scrollBar = new ScrollBarView (tableView, true);

			scrollBar.ChangedPosition += (s, e) => {
				tableView.RowOffset = scrollBar.Position;
				if (tableView.RowOffset != scrollBar.Position) {
					scrollBar.Position = tableView.RowOffset;
				}
				tableView.SetNeedsDisplay ();
			};
			/*
			scrollBar.OtherScrollBarView.ChangedPosition += (s,e) => {
				tableView.LeftItem = scrollBar.OtherScrollBarView.Position;
				if (tableView.LeftItem != scrollBar.OtherScrollBarView.Position) {
					scrollBar.OtherScrollBarView.Position = tableView.LeftItem;
				}
				tableView.SetNeedsDisplay ();
			};*/

			tableView.DrawContent += (s, e) => {
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

			enteredText = okPressed ? tf.Text : null;
			return okPressed;
		}
		private void EditCurrentCell (object sender, CellActivatedEventArgs e)
		{
			if (e.Table == null)
				return;

			var oldValue = currentTable.Rows [e.Row] [e.Col].ToString ();

			if (GetText ("Enter new value", currentTable.Columns [e.Col].ColumnName, oldValue, out string newText)) {
				try {
					currentTable.Rows [e.Row] [e.Col] = string.IsNullOrWhiteSpace (newText) ? DBNull.Value : (object)newText;
				} catch (Exception ex) {
					MessageBox.ErrorQuery (60, 20, "Failed to set text", ex.Message, "Ok");
				}

				tableView.Update ();
			}
		}
	}
}
