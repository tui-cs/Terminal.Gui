#nullable enable

using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using CsvHelper;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Csv Editor", "Open and edit simple CSV files using the TableView class.")]
[ScenarioCategory ("TableView")]
[ScenarioCategory ("Editor")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Dialogs")]
[ScenarioCategory ("Text and Formatting")]
[ScenarioCategory ("Arrangement")]
[ScenarioCategory ("Files and IO")]
public class CsvEditor : Scenario
{
    private string? _currentFile;
    private DataTable? _currentTable;
    private CheckBox? _miCenteredCheckBox;
    private CheckBox? _miLeftCheckBox;
    private CheckBox? _miRightCheckBox;
    private TextField? _selectedCellTextField;
    private TableView? _tableView;
    private IApplication? _app;

    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();
        _app = app;

        using Window appWindow = new ();
        appWindow.Title = GetName ();

        // MenuBar
        MenuBar menu = new ();

        _tableView = new TableView { X = 0, Y = Pos.Bottom (menu), Width = Dim.Fill (), Height = Dim.Fill (1) };

        _selectedCellTextField = new TextField { Text = "0,0", Width = 10, Height = 1 };
        _selectedCellTextField.TextChanged += SelectedCellLabel_TextChanged;

        // StatusBar
        StatusBar statusBar = new ([
                                       new Shortcut (Application.GetDefaultKey (Command.Quit), "Quit", Quit, "Quit!"),
                                       new Shortcut (Key.O.WithCtrl, "Open", Open, "Open a file."),
                                       new Shortcut (Key.S.WithCtrl, "Save", Save, "Save current."),
                                       new Shortcut
                                       {
                                           HelpText = "Cell:",
                                           CommandView = _selectedCellTextField,
                                           AlignmentModes = AlignmentModes.StartToEnd | AlignmentModes.IgnoreFirstOrLast,
                                           Enabled = false
                                       }
                                   ]) { AlignmentModes = AlignmentModes.IgnoreFirstOrLast };

        // Setup menu checkboxes for alignment
        _miLeftCheckBox = new CheckBox { Title = "_Align Left" };
        _miLeftCheckBox.ValueChanged += (_, _) => Align (Alignment.Start);

        _miRightCheckBox = new CheckBox { Title = "_Align Right" };
        _miRightCheckBox.ValueChanged += (_, _) => Align (Alignment.End);

        _miCenteredCheckBox = new CheckBox { Title = "_Align Centered" };
        _miCenteredCheckBox.ValueChanged += (_, _) => Align (Alignment.Center);

        MenuBarItem fileMenu = new (Strings.menuFile,
                                    [
                                        new MenuItem { Title = "_Open CSV", Action = Open },
                                        new MenuItem { Title = Strings.cmdSave, Action = Save },
                                        new MenuItem { Title = Strings.cmdQuit, HelpText = "Quits The App", Action = Quit }
                                    ]);

        menu.Add (fileMenu);

        menu.Add (new MenuBarItem ("_Edit",
                                   [
                                       new MenuItem { Title = "_New Column", Action = AddColumn },
                                       new MenuItem { Title = "_New Row", Action = AddRow },
                                       new MenuItem { Title = "_Rename Column", Action = RenameColumn },
                                       new MenuItem { Title = "_Delete Column", Action = DeleteColum },
                                       new MenuItem { Title = "_Move Column", Action = MoveColumn },
                                       new MenuItem { Title = "_Move Row", Action = MoveRow },
                                       new MenuItem { Title = "_Sort Asc", Action = () => Sort (true) },
                                       new MenuItem { Title = "_Sort Desc", Action = () => Sort (false) }
                                   ]));

        menu.Add (new MenuBarItem ("_View",
                                   [
                                       new MenuItem { CommandView = _miLeftCheckBox },
                                       new MenuItem { CommandView = _miRightCheckBox },
                                       new MenuItem { CommandView = _miCenteredCheckBox },
                                       new MenuItem { Title = "_Set Format Pattern", Action = SetFormat }
                                   ]));

        appWindow.Add (menu, _tableView, statusBar);

        _tableView.ValueChanged += OnValueChanged;
        _tableView.Accepted += EditCurrentCell;
        _tableView.KeyDown += TableViewKeyPress;

        app.Run (appWindow);
    }

    private void AddColumn ()
    {
        if (NoTableLoaded () || _tableView is null || _currentTable is null)
        {
            return;
        }

        if (!GetText ("Enter column name", "Name:", "", out string colName))
        {
            return;
        }
        DataColumn col = new (colName);

        int newColIdx = Math.Min (Math.Max (0, (_tableView.Value?.SelectedCell.X ?? 0) + 1), _tableView.Table!.Columns);

        int? result = MessageBox.Query (_tableView.App!, "Column Type", "Pick a data type for the column", "Date", "Integer", "Double", "Text", "Cancel");

        if (result is null or >= 4)
        {
            return;
        }

        switch (result)
        {
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

        _currentTable.Columns.Add (col);
        col.SetOrdinal (newColIdx);
        _tableView.Update ();
    }

    private void AddRow ()
    {
        if (NoTableLoaded () || _currentTable is null || _tableView is null)
        {
            return;
        }

        DataRow newRow = _currentTable.NewRow ();

        int newRowIdx = Math.Min (Math.Max (0, (_tableView.Value?.SelectedCell.Y ?? 0) + 1), _tableView.Table!.Rows);

        _currentTable.Rows.InsertAt (newRow, newRowIdx);
        _tableView.Update ();
    }

    private void Align (Alignment newAlignment)
    {
        if (NoTableLoaded () || _tableView is null)
        {
            return;
        }

        ColumnStyle style = _tableView.Style.GetOrCreateColumnStyle (_tableView.Value?.SelectedCell.X ?? 0);
        style.Alignment = newAlignment;

        _miLeftCheckBox?.Value = style.Alignment == Alignment.Start ? CheckState.Checked : CheckState.UnChecked;

        _miRightCheckBox?.Value = style.Alignment == Alignment.End ? CheckState.Checked : CheckState.UnChecked;

        _miCenteredCheckBox?.Value = style.Alignment == Alignment.Center ? CheckState.Checked : CheckState.UnChecked;

        _tableView.Update ();
    }

    private void DeleteColum ()
    {
        if (NoTableLoaded () || _tableView is null || _currentTable is null)
        {
            return;
        }

        if (_tableView.Value is null)
        {
            MessageBox.ErrorQuery (_tableView!.App!, "No Column", "No column selected", "Ok");

            return;
        }

        try
        {
            _currentTable.Columns.RemoveAt (_tableView.Value.SelectedCell.X);
            _tableView.Update ();
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery (_tableView!.App!, "Could not remove column", ex.Message, "Ok");
        }
    }

    private void EditCurrentCell (object? sender, CommandEventArgs e)
    {
        if (_tableView?.Table is null || _currentTable is null)
        {
            return;
        }

        int col = _tableView.Value?.SelectedCell.X ?? 0;
        int row = _tableView.Value?.SelectedCell.Y ?? 0;

        var oldValue = _currentTable.Rows [row] [col].ToString ();

        if (!GetText ("Enter new value", _currentTable.Columns [col].ColumnName, oldValue ?? "", out string newText))
        {
            return;
        }

        try
        {
            _currentTable.Rows [row] [col] = string.IsNullOrWhiteSpace (newText) ? DBNull.Value : newText;
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery (_tableView!.App!, "Failed to set text", ex.Message, "Ok");
        }

        _tableView.Update ();
    }

    private bool GetText (string title, string label, string initialText, out string enteredText)
    {
        Button cancel = new () { Text = "Cancel" };
        Button ok = new () { Text = "Ok" };

        Dialog d = new () { Title = title };
        d.Buttons = [cancel, ok];

        Label lbl = new () { X = 0, Y = 1, Text = label };

        TextField tf = new () { Text = initialText, X = 0, Y = 2, Width = Dim.Fill (0, 50) };

        d.Add (lbl, tf);
        tf.SetFocus ();

        _app?.Run (d);
        bool accepted = !d.Canceled;
        d.Dispose ();

        enteredText = accepted ? tf.Text : string.Empty;

        return accepted;
    }

    private void MoveColumn ()
    {
        if (NoTableLoaded () || _currentTable is null || _tableView is null)
        {
            return;
        }

        if (_tableView.Value is null)
        {
            MessageBox.ErrorQuery (_tableView!.App!, "No Column", "No column selected", "Ok");

            return;
        }

        try
        {
            DataColumn currentCol = _currentTable.Columns [_tableView.Value.SelectedCell.X];

            if (!GetText ("Move Column", "New Index:", currentCol.Ordinal.ToString (), out string newOrdinal))
            {
                return;
            }
            int newIdx = Math.Min (Math.Max (0, int.Parse (newOrdinal)), _tableView.Table!.Columns - 1);

            currentCol.SetOrdinal (newIdx);

            _tableView.SetSelection (newIdx, _tableView.Value!.SelectedCell.Y, false);
            _tableView.EnsureCursorIsVisible ();
            _tableView.SetNeedsDraw ();
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery (_tableView!.App!, "Error moving column", ex.Message, "Ok");
        }
    }

    private void MoveRow ()
    {
        if (NoTableLoaded () || _currentTable is null || _tableView is null)
        {
            return;
        }

        if (_tableView.Value is null)
        {
            MessageBox.ErrorQuery (_tableView!.App!, "No Rows", "No row selected", "Ok");

            return;
        }

        try
        {
            int oldIdx = _tableView.Value.SelectedCell.Y;

            DataRow currentRow = _currentTable.Rows [oldIdx];

            if (!GetText ("Move Row", "New Row:", oldIdx.ToString (), out string newOrdinal))
            {
                return;
            }
            int newIdx = Math.Min (Math.Max (0, int.Parse (newOrdinal)), _tableView.Table!.Rows - 1);

            if (newIdx == oldIdx)
            {
                return;
            }

            object? [] arrayItems = currentRow.ItemArray;
            _currentTable.Rows.Remove (currentRow);

            // Removing and Inserting the same DataRow seems to result in it loosing its values so we have to create a new instance
            DataRow newRow = _currentTable.NewRow ();
            newRow.ItemArray = arrayItems;

            _currentTable.Rows.InsertAt (newRow, newIdx);

            _tableView.SetSelection (_tableView.Value!.SelectedCell.X, newIdx, false);
            _tableView.EnsureCursorIsVisible ();
            _tableView.SetNeedsDraw ();
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery (_tableView!.App!, "Error moving column", ex.Message, "Ok");
        }
    }

    private bool NoTableLoaded ()
    {
        if (_tableView?.Table is { })
        {
            return false;
        }
        MessageBox.ErrorQuery (_tableView!.App!, "No Table Loaded", "No table has currently be opened", "Ok");

        return true;
    }

    private void OnValueChanged (object? sender, ValueChangedEventArgs<TableSelection?> e)
    {
        if (_selectedCellTextField is null || _tableView is null)
        {
            return;
        }

        int cursorRow = _tableView.Value?.SelectedCell.Y ?? 0;
        int cursorCol = _tableView.Value?.SelectedCell.X ?? 0;

        // only update the text box if the user is not manually editing it
        if (!_selectedCellTextField.HasFocus)
        {
            _selectedCellTextField.Text = $"{cursorRow},{cursorCol}";
        }

        if (_tableView.Table is null || _tableView.Value is null)
        {
            return;
        }

        ColumnStyle? style = _tableView.Style.GetColumnStyleIfAny (cursorCol);

        _miLeftCheckBox?.Value = style?.Alignment == Alignment.Start ? CheckState.Checked : CheckState.UnChecked;

        _miRightCheckBox?.Value = style?.Alignment == Alignment.End ? CheckState.Checked : CheckState.UnChecked;

        _miCenteredCheckBox?.Value = style?.Alignment == Alignment.Center ? CheckState.Checked : CheckState.UnChecked;
    }

    private void Open ()
    {
        FileDialog ofd = new () { AllowedTypes = [new AllowedType ("Comma Separated Values", ".csv")] };
        ofd.Style.OkButtonText = "Open";

        _app?.Run (ofd);

        if (!ofd.Canceled && !string.IsNullOrWhiteSpace (ofd.Path))
        {
            Open (ofd.Path);
        }

        ofd.Dispose ();
    }

    private void Open (string filename)
    {
        var lineNumber = 0;
        _currentFile = null;

        try
        {
            using CsvReader reader = new (File.OpenText (filename), CultureInfo.InvariantCulture);

            DataTable dt = new ();

            reader.Read ();

            if (reader.ReadHeader () && reader.HeaderRecord is { })
            {
                foreach (string h in reader.HeaderRecord)
                {
                    dt.Columns.Add (h);
                }
            }

            while (reader.Read ())
            {
                lineNumber++;

                DataRow newRow = dt.Rows.Add ();

                for (var i = 0; i < dt.Columns.Count; i++)
                {
                    newRow [i] = reader [i];
                }
            }

            SetTable (dt);

            // Only set the current filename if we successfully loaded the entire file
            _currentFile = filename;

            _selectedCellTextField?.SuperView?.Enabled = true;

            _app?.TopRunnableView?.Title = $"{GetName ()} - {Path.GetFileName (_currentFile)}";
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery (_tableView!.App!, "Open Failed", $"Error on line {lineNumber}{Environment.NewLine}{ex.Message}", "Ok");
        }
    }

    private void Quit () => _tableView?.App?.RequestStop ();

    private void RenameColumn ()
    {
        if (NoTableLoaded () || _currentTable is null || _tableView is null)
        {
            return;
        }

        DataColumn currentCol = _currentTable.Columns [_tableView.Value?.SelectedCell.X ?? 0];

        if (!GetText ("Rename Column", "Name:", currentCol.ColumnName, out string newName))
        {
            return;
        }
        currentCol.ColumnName = newName;
        _tableView.Update ();
    }

    private void Save ()
    {
        if (_tableView?.Table is null || string.IsNullOrWhiteSpace (_currentFile) || _currentTable is null)
        {
            MessageBox.ErrorQuery (_tableView!.App!, "No file loaded", "No file is currently loaded", "Ok");

            return;
        }

        using CsvWriter writer = new (new StreamWriter (File.OpenWrite (_currentFile)), CultureInfo.InvariantCulture);

        foreach (string col in _currentTable.Columns.Cast<DataColumn> ().Select (c => c.ColumnName))
        {
            writer.WriteField (col);
        }

        writer.NextRecord ();

        foreach (DataRow row in _currentTable.Rows)
        {
            foreach (object? item in row.ItemArray)
            {
                writer.WriteField (item);
            }

            writer.NextRecord ();
        }
    }

    private void SelectedCellLabel_TextChanged (object? sender, EventArgs e)
    {
        if (_selectedCellTextField is null || _tableView is null)
        {
            return;
        }

        // if user is in the text control and editing the selected cell
        if (!_selectedCellTextField.HasFocus)
        {
            return;
        }

        // change selected cell to the one the user has typed into the box
        Match match = Regex.Match (_selectedCellTextField.Text, "^(\\d+),(\\d+)$");

        if (match.Success)
        {
            _tableView.SetSelection (int.Parse (match.Groups [2].Value), int.Parse (match.Groups [1].Value), false);
        }
    }

    private void SetFormat ()
    {
        if (NoTableLoaded () || _currentTable is null || _tableView is null)
        {
            return;
        }

        DataColumn col = _currentTable.Columns [_tableView.Value?.SelectedCell.X ?? 0];

        if (col.DataType == typeof (string))
        {
            MessageBox.ErrorQuery (_tableView!.App!,
                                   "Cannot Format Column",
                                   "String columns cannot be Formatted, try adding a new column to the table with a date/numerical Type",
                                   "Ok");

            return;
        }

        ColumnStyle style = _tableView.Style.GetOrCreateColumnStyle (col.Ordinal);

        if (!GetText ("Format", "Pattern:", style.Format ?? "", out string newPattern))
        {
            return;
        }
        style.Format = newPattern;
        _tableView.Update ();
    }

    private void SetTable (DataTable dataTable) => _tableView?.Table = new DataTableSource (_currentTable = dataTable);

    private void Sort (bool asc)
    {
        if (NoTableLoaded () || _currentTable is null || _tableView is null)
        {
            return;
        }

        if (_tableView.Value is null)
        {
            MessageBox.ErrorQuery (_tableView!.App!, "No Column", "No column selected", "Ok");

            return;
        }

        string colName = _tableView.Table!.ColumnNames [_tableView.Value.SelectedCell.X];

        _currentTable.DefaultView.Sort = colName + (asc ? " asc" : " desc");
        SetTable (_currentTable.DefaultView.ToTable ());
    }

    private void TableViewKeyPress (object? sender, Key e)
    {
        if (_currentTable is null || _tableView is null)
        {
            return;
        }

        if (e.KeyCode != Key.Delete)
        {
            return;
        }

        if (_tableView.FullRowSelect)
        {
            // Delete button deletes all rows when in full row mode
            foreach (int toRemove in _tableView.GetAllSelectedCells ().Select (p => p.Y).Distinct ().OrderByDescending (i => i))
            {
                _currentTable.Rows.RemoveAt (toRemove);
            }
        }
        else
        {
            // otherwise set all selected cells to null
            foreach (Point pt in _tableView.GetAllSelectedCells ())
            {
                _currentTable.Rows [pt.Y] [pt.X] = DBNull.Value;
            }
        }

        _tableView.Update ();
        e.Handled = true;
    }
}
