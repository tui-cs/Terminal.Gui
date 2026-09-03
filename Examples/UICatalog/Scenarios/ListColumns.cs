#nullable enable

using System.Collections;
using System.Data;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("ListColumns", "Implements a columned list via a data table.")]
[ScenarioCategory ("TableView")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Dialogs")]
[ScenarioCategory ("Text and Formatting")]
[ScenarioCategory ("Scrolling")]
public class ListColumns : Scenario
{
    private IApplication? _app;
    private Scheme? _alternatingScheme;
    private DataTable? _currentTable;
    private TableView? _listColView;
    private CheckBox? _alternatingColorsCheckBox;
    private CheckBox? _alwaysUseNormalColorForVerticalCellLinesCheckBox;
    private CheckBox? _bottomLineCheckBox;
    private CheckBox? _cellLinesCheckBox;
    private CheckBox? _cursorCheckBox;
    private CheckBox? _expandLastColumnCheckBox;
    private CheckBox? _orientVerticalCheckBox;
    private CheckBox? _scrollParallelCheckBox;
    private CheckBox? _smoothScrollingCheckBox;
    private CheckBox? _topLineCheckBox;

    /// <summary>
    ///     Builds a simple list in which values are the index. This helps test that scrolling etc. is working
    ///     correctly and not skipping out values when paging
    /// </summary>
    /// <param name="items"></param>
    /// <returns></returns>
    public static IList BuildSimpleList (int items)
    {
        List<object> list = [];

        for (var i = 0; i < items; i++)
        {
            list.Add ("Item " + i);
        }

        return list;
    }

    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();
        _app = app;

        using Window appWindow = new () { Title = GetQuitKeyAndName (), BorderStyle = LineStyle.None };

        // MenuBar
        MenuBar menuBar = new ();

        _listColView = new TableView
        {
            Y = Pos.Bottom (menuBar),
            Width = Dim.Fill (),
            Height = Dim.Fill (1),
            Style = new TableStyle
            {
                ShowHeaders = false,
                ShowHorizontalHeaderOverline = false,
                ShowHorizontalHeaderUnderline = false,
                ShowHorizontalBottomLine = false,
                ExpandLastColumn = false
            }
        };
        ListColumnStyle listColStyle = new ();

        // Status Bar
        StatusBar statusBar = new ([
                                       new Shortcut (Key.F2, "OpenBigListEx", () => OpenSimpleList (true)),
                                       new Shortcut (Key.F3, "CloseExample", CloseExample),
                                       new Shortcut (Key.F4, "OpenSmListEx", () => OpenSimpleList (false)),
                                       new Shortcut (Application.GetDefaultKey (Command.Quit), "Quit", Quit)
                                   ]);

        // Selected cell label
        Label selectedCellLabel = new ()
        {
            X = 0,
            Y = Pos.Bottom (_listColView),
            Text = "0,0",
            Width = Dim.Fill (),
            TextAlignment = Alignment.End
        };

        _listColView.ValueChanged += (s, e) =>
                                     {
                                         if (_listColView is { })
                                         {
                                             selectedCellLabel.Text = $"{_listColView.Value?.SelectedCell.Y ?? 0},{_listColView.Value?.SelectedCell.X ?? 0}";
                                         }
                                     };
        _listColView.KeyDown += TableViewKeyPress;

        _alternatingScheme = new Scheme
        {
            Disabled = appWindow.GetAttributeForRole (VisualRole.Disabled),
            HotFocus = appWindow.GetAttributeForRole (VisualRole.HotFocus),
            Focus = appWindow.GetAttributeForRole (VisualRole.Focus),
            Normal = new Attribute (Color.White, Color.BrightBlue)
        };

        _listColView.KeyBindings.ReplaceCommands (Key.Space, Command.Accept);

        // Setup menu checkboxes
        _topLineCheckBox = new CheckBox
        {
            Title = "_TopLine", Value = _listColView.Style.ShowHorizontalHeaderOverline ? CheckState.Checked : CheckState.UnChecked
        };
        _topLineCheckBox.ValueChanged += (s, e) => ToggleTopline ();

        _bottomLineCheckBox = new CheckBox
        {
            Title = "_BottomLine", Value = _listColView.Style.ShowHorizontalBottomLine ? CheckState.Checked : CheckState.UnChecked
        };
        _bottomLineCheckBox.ValueChanged += (s, e) => ToggleBottomline ();

        _cellLinesCheckBox = new CheckBox
        {
            Title = "_CellLines", Value = _listColView.Style.ShowVerticalCellLines ? CheckState.Checked : CheckState.UnChecked
        };
        _cellLinesCheckBox.ValueChanged += (s, e) => ToggleCellLines ();

        _expandLastColumnCheckBox = new CheckBox
        {
            Title = "_ExpandLastColumn", Value = _listColView.Style.ExpandLastColumn ? CheckState.Checked : CheckState.UnChecked
        };
        _expandLastColumnCheckBox.ValueChanged += (s, e) => ToggleExpandLastColumn ();

        _alwaysUseNormalColorForVerticalCellLinesCheckBox = new CheckBox
        {
            Title = "_AlwaysUseNormalColorForVerticalCellLines",
            Value = _listColView.Style.AlwaysUseNormalColorForVerticalCellLines ? CheckState.Checked : CheckState.UnChecked
        };
        _alwaysUseNormalColorForVerticalCellLinesCheckBox.ValueChanged += (s, e) => ToggleAlwaysUseNormalColorForVerticalCellLines ();

        _smoothScrollingCheckBox = new CheckBox
        {
            Title = "_SmoothHorizontalScrolling", Value = _listColView.Style.SmoothHorizontalScrolling ? CheckState.Checked : CheckState.UnChecked
        };
        _smoothScrollingCheckBox.ValueChanged += (s, e) => ToggleSmoothScrolling ();

        _alternatingColorsCheckBox = new CheckBox { Title = "Alternating Colors" };
        _alternatingColorsCheckBox.ValueChanged += (s, e) => ToggleAlternatingColors ();

        _cursorCheckBox = new CheckBox
        {
            Title = "Invert Selected Cell First Character",
            Value = _listColView.Style.InvertSelectedCellFirstCharacter ? CheckState.Checked : CheckState.UnChecked
        };
        _cursorCheckBox.ValueChanged += (s, e) => ToggleInvertSelectedCellFirstCharacter ();

        _orientVerticalCheckBox = new CheckBox
        {
            Title = "_OrientVertical", Value = listColStyle.Orientation == Orientation.Vertical ? CheckState.Checked : CheckState.UnChecked
        };
        _orientVerticalCheckBox.ValueChanged += (s, e) => ToggleVerticalOrientation ();

        _scrollParallelCheckBox = new CheckBox { Title = "_ScrollParallel", Value = listColStyle.ScrollParallel ? CheckState.Checked : CheckState.UnChecked };
        _scrollParallelCheckBox.ValueChanged += (s, e) => ToggleScrollParallel ();

        menuBar.Add (new MenuBarItem (Strings.menuFile,
                                      [
                                          new MenuItem { Title = "Open_BigListExample", Action = () => OpenSimpleList (true) },
                                          new MenuItem { Title = "Open_SmListExample", Action = () => OpenSimpleList (false) },
                                          new MenuItem { Title = "_CloseExample", Action = CloseExample },
                                          new MenuItem { Title = Strings.cmdQuit, Action = Quit }
                                      ]));

        menuBar.Add (new MenuBarItem ("_View",
                                      [
                                          new MenuItem { CommandView = _topLineCheckBox },
                                          new MenuItem { CommandView = _bottomLineCheckBox },
                                          new MenuItem { CommandView = _cellLinesCheckBox },
                                          new MenuItem { CommandView = _expandLastColumnCheckBox },
                                          new MenuItem { CommandView = _alwaysUseNormalColorForVerticalCellLinesCheckBox },
                                          new MenuItem { CommandView = _smoothScrollingCheckBox },
                                          new MenuItem { CommandView = _alternatingColorsCheckBox },
                                          new MenuItem { CommandView = _cursorCheckBox }
                                      ]));

        menuBar.Add (new MenuBarItem ("_List",
                                      [
                                          new MenuItem { CommandView = _orientVerticalCheckBox },
                                          new MenuItem { CommandView = _scrollParallelCheckBox },
                                          new MenuItem { Title = "Set _Max Cell Width", Action = SetListMaxWidth },
                                          new MenuItem { Title = "Set Mi_n Cell Width", Action = SetListMinWidth }
                                      ]));

        // Add views in order of visual appearance
        appWindow.Add (menuBar, _listColView, selectedCellLabel, statusBar);

        app.Run (appWindow);
    }

    private void CloseExample ()
    {
        if (_listColView is { })
        {
            _listColView.Table = null;
        }
    }

    private void OpenSimpleList (bool big) => SetTable (BuildSimpleList (big ? 1023 : 31));

    private void Quit () => _listColView?.App?.RequestStop ();

    private void RunListWidthDialog (string prompt, Action<TableView, int> setter, Func<TableView, int> getter)
    {
        if (_listColView is null)
        {
            return;
        }

        var accepted = false;
        var d = new Dialog { Title = prompt, Buttons = [new Button { Title = Strings.btnCancel }, new Button { Title = Strings.btnOk }] };

        TextField tf = new () { Text = getter (_listColView).ToString (), X = 0, Y = 0, Width = Dim.Fill (0, 50) };

        d.Add (tf);
        tf.SetFocus ();

        _app?.Run (d);
        accepted = d.Result == 1;
        d.Dispose ();

        if (accepted)
        {
            try
            {
                setter (_listColView, int.Parse (tf.Text));
            }
            catch (Exception ex)
            {
                MessageBox.ErrorQuery (_app!, "Failed to set", ex.Message, "Ok");
            }
        }
    }

    private void SetListMaxWidth ()
    {
        RunListWidthDialog ("MaxCellWidth", (s, v) => s.MaxCellWidth = v, s => s.MaxCellWidth);
        _listColView?.SetNeedsDraw ();
    }

    private void SetListMinWidth ()
    {
        RunListWidthDialog ("MinCellWidth", (s, v) => s.MinCellWidth = v, s => s.MinCellWidth);
        _listColView?.SetNeedsDraw ();
    }

    private void SetTable (IList list)
    {
        if (_listColView is null)
        {
            return;
        }

        _listColView.Table = new ListTableSource (list, _listColView);

        if (_listColView.Table is ListTableSource listTableSource)
        {
            _currentTable = listTableSource.DataTable;
        }
    }

    private void TableViewKeyPress (object? sender, Key e)
    {
        if (_currentTable is null || _listColView is null)
        {
            return;
        }

        if (e.KeyCode == Key.Delete)
        {
            // set all selected cells to null
            foreach (Point pt in _listColView.GetAllSelectedCells ())
            {
                _currentTable.Rows [pt.Y] [pt.X] = DBNull.Value;
            }

            _listColView.Update ();
            e.Handled = true;
        }
    }

    private void ToggleAlternatingColors ()
    {
        if (_listColView is null || _alternatingColorsCheckBox is null)
        {
            return;
        }

        if (_alternatingColorsCheckBox.Value == CheckState.Checked)
        {
            _listColView.Style.RowColorGetter = a => a.RowIndex % 2 == 0 ? _alternatingScheme : null;
        }
        else
        {
            _listColView.Style.RowColorGetter = null;
        }

        _listColView.SetNeedsDraw ();
    }

    private void ToggleAlwaysUseNormalColorForVerticalCellLines ()
    {
        if (_listColView is null || _alwaysUseNormalColorForVerticalCellLinesCheckBox is null)
        {
            return;
        }

        _listColView.Style.AlwaysUseNormalColorForVerticalCellLines = _alwaysUseNormalColorForVerticalCellLinesCheckBox.Value == CheckState.Checked;

        _listColView.Update ();
    }

    private void ToggleBottomline ()
    {
        if (_listColView is null || _bottomLineCheckBox is null)
        {
            return;
        }

        _listColView.Style.ShowHorizontalBottomLine = _bottomLineCheckBox.Value == CheckState.Checked;
        _listColView.Update ();
    }

    private void ToggleCellLines ()
    {
        if (_listColView is null || _cellLinesCheckBox is null)
        {
            return;
        }

        _listColView.Style.ShowVerticalCellLines = _cellLinesCheckBox.Value == CheckState.Checked;
        _listColView.Update ();
    }

    private void ToggleExpandLastColumn ()
    {
        if (_listColView is null || _expandLastColumnCheckBox is null)
        {
            return;
        }

        _listColView.Style.ExpandLastColumn = _expandLastColumnCheckBox.Value == CheckState.Checked;

        _listColView.Update ();
    }

    private void ToggleInvertSelectedCellFirstCharacter ()
    {
        if (_listColView is null || _cursorCheckBox is null)
        {
            return;
        }

        _listColView.Style.InvertSelectedCellFirstCharacter = _cursorCheckBox.Value == CheckState.Checked;
        _listColView.SetNeedsDraw ();
    }

    private void ToggleScrollParallel ()
    {
        if (_listColView?.Table is not ListTableSource listTableSource || _scrollParallelCheckBox is null)
        {
            return;
        }

        listTableSource.Style.ScrollParallel = _scrollParallelCheckBox.Value == CheckState.Checked;
        _listColView.SetNeedsDraw ();
    }

    private void ToggleSmoothScrolling ()
    {
        if (_listColView is null || _smoothScrollingCheckBox is null)
        {
            return;
        }

        _listColView.Style.SmoothHorizontalScrolling = _smoothScrollingCheckBox.Value == CheckState.Checked;

        _listColView.Update ();
    }

    private void ToggleTopline ()
    {
        if (_listColView is null || _topLineCheckBox is null)
        {
            return;
        }

        _listColView.Style.ShowHorizontalHeaderOverline = _topLineCheckBox.Value == CheckState.Checked;
        _listColView.Update ();
    }

    private void ToggleVerticalOrientation ()
    {
        if (_listColView?.Table is not ListTableSource listTableSource || _orientVerticalCheckBox is null)
        {
            return;
        }

        listTableSource.Style.Orientation = _orientVerticalCheckBox.Value == CheckState.Checked ? Orientation.Vertical : Orientation.Horizontal;
        _listColView.SetNeedsDraw ();
    }
}
