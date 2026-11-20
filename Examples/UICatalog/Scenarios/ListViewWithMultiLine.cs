#nullable enable

using System.Collections.ObjectModel;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("ListView With MultiLine", "ListView items taller than one row, with typed column headers")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("ListView")]
public class ListViewWithMultiLine : Scenario
{
    private readonly string [] _modeNames =
    [
        "Single column with line breaks",
        "Single column with padding",
        "Multiple columns with two-lines",
        "Typed columns",
        "String array columns",
        "Empty list",
        "Headers only"
    ];

    private IApplication? _app;
    private ListView? _listView;
    private TextField? _itemHeightField;
    private Label? _modeLabel;
    private Label? _headerAlignLabel;
    private Label? _itemAlignLabel;
    private int _mode;
    private bool _updatingHeight;
    private ObservableCollection<Scenario>? _scenarios;

    /// <inheritdoc/>
    public override void Main ()
    {
        _app = Application.Create ();
        _app.Init ();

        using Window appWindow = new ();
        appWindow.Title = GetQuitKeyAndName ();

        _scenarios = GetScenarios ();

        Button nextMode = new ()
        {
            X = 0,
            Y = 0,
            Text = "Next _mode"
        };
        nextMode.Accepting += (_, e) =>
                              {
                                  e.Handled = true;
                                  _mode = (_mode + 1) % _modeNames.Length;
                                  ApplyMode ();
                              };
        appWindow.Add (nextMode);

        _modeLabel = new Label
        {
            X = Pos.Right (nextMode) + 1,
            Y = 0,
            Text = _modeNames [0]
        };
        appWindow.Add (_modeLabel);

        CheckBox showHeaders = new ()
        {
            X = 0,
            Y = Pos.Bottom (nextMode),
            Text = "Show _headers"
        };
        showHeaders.ValueChanging += (_, e) =>
                                     {
                                         if (_listView is null)
                                         {
                                             return;
                                         }

                                         _listView.ShowColumnHeaders = e.NewValue == CheckState.Checked;
                                     };
        appWindow.Add (showHeaders);

        CheckBox showMarks = new ()
        {
            X = Pos.Right (showHeaders) + 1,
            Y = Pos.Top (showHeaders),
            Text = "Show mar_ks"
        };
        showMarks.ValueChanging += (_, e) =>
                                   {
                                       if (_listView is null)
                                       {
                                           return;
                                       }

                                       _listView.ShowMarks = e.NewValue == CheckState.Checked;
                                   };
        appWindow.Add (showMarks);

        CheckBox markMultiple = new ()
        {
            X = Pos.Right (showMarks) + 1,
            Y = Pos.Top (showMarks),
            Text = "Mark _multiple"
        };
        markMultiple.ValueChanging += (_, e) =>
                                      {
                                          if (_listView is null)
                                          {
                                              return;
                                          }

                                          _listView.MarkMultiple = e.NewValue == CheckState.Checked;
                                      };
        appWindow.Add (markMultiple);

        CheckBox showHorizontal = new ()
        {
            X = Pos.Right (markMultiple) + 1,
            Y = Pos.Top (markMultiple),
            Text = "Show hori_zontal"
        };
        showHorizontal.ValueChanging += (_, e) =>
                                        {
                                            if (_listView is null)
                                            {
                                                return;
                                            }

                                            if (e.NewValue == CheckState.Checked)
                                            {
                                                _listView.ViewportSettings |= ViewportSettingsFlags.HasHorizontalScrollBar;
                                            }
                                            else
                                            {
                                                _listView.ViewportSettings &= ~ViewportSettingsFlags.HasHorizontalScrollBar;
                                            }
                                        };
        appWindow.Add (showHorizontal);

        CheckBox showVertical = new ()
        {
            X = Pos.Right (showHorizontal) + 1,
            Y = Pos.Top (markMultiple),
            Text = "Show _vertical"
        };
        showVertical.ValueChanging += (_, e) =>
                                      {
                                          if (_listView is null)
                                          {
                                              return;
                                          }

                                          if (e.NewValue == CheckState.Checked)
                                          {
                                              _listView.ViewportSettings |= ViewportSettingsFlags.HasVerticalScrollBar;
                                          }
                                          else
                                          {
                                              _listView.ViewportSettings &= ~ViewportSettingsFlags.HasVerticalScrollBar;
                                          }
                                      };
        appWindow.Add (showVertical);

        Label heightLabel = new ()
        {
            X = 0,
            Y = Pos.Bottom (showHeaders),
            Text = "Item _height:"
        };
        appWindow.Add (heightLabel);

        _itemHeightField = new TextField
        {
            X = Pos.Right (heightLabel) + 1,
            Y = Pos.Top (heightLabel),
            Width = 4,
            Text = "1"
        };
        _itemHeightField.TextChanged += (_, _) => OnItemHeightChanged ();
        appWindow.Add (_itemHeightField);

        Button headerAlign = new ()
        {
            X = Pos.Right (_itemHeightField) + 2,
            Y = Pos.Top (heightLabel),
            Text = "Header _align"
        };
        headerAlign.Accepting += (_, e) =>
                                 {
                                     e.Handled = true;
                                     CycleAlignment (header: true);
                                 };
        appWindow.Add (headerAlign);

        _headerAlignLabel = new Label
        {
            X = Pos.Right (headerAlign) + 1,
            Y = Pos.Top (heightLabel),
            Width = 6,
            Text = nameof (Alignment.Center)
        };
        appWindow.Add (_headerAlignLabel);

        Button itemAlign = new ()
        {
            X = Pos.Right (_headerAlignLabel) + 1,
            Y = Pos.Top (heightLabel),
            Text = "Item a_lign"
        };
        itemAlign.Accepting += (_, e) =>
                               {
                                   e.Handled = true;
                                   CycleAlignment (header: false);
                               };
        appWindow.Add (itemAlign);

        _itemAlignLabel = new Label
        {
            X = Pos.Right (itemAlign) + 1,
            Y = Pos.Top (heightLabel),
            Width = 6,
            Text = nameof (Alignment.Start)
        };
        appWindow.Add (_itemAlignLabel);

        _listView = new ListView
        {
            X = 0,
            Y = Pos.Bottom (heightLabel),
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };
        _listView.RowRender += OnRowRender;
        appWindow.Add (_listView);

        ApplyMode ();
        _app.Run (appWindow);
        _app.Dispose ();
    }

    private void ApplyMode ()
    {
        if (_listView is null || _modeLabel is null || _itemHeightField is null || _scenarios is null)
        {
            return;
        }

        _modeLabel.Text = $"{_mode}-{_modeNames [_mode]}";

        switch (_mode)
        {
            case 0:
                ObservableCollection<string> colLines = [];

                foreach (Scenario scenario in _scenarios)
                {
                    colLines.Add ($"{scenario.GetName ()}\n{scenario.GetDescription ()}");
                }

                _listView.ItemHeight = 2;
                _listView.ColumnHeaders = null;
                _listView.SetSource (colLines);

                break;

            case 1:
                _listView.ItemHeight = 1;
                _listView.ColumnHeaders = null;
                _listView.SetSource (_scenarios);

                break;

            case 2:
                ObservableCollection<ScenarioLine> lines = [];

                foreach (Scenario scenario in _scenarios)
                {
                    lines.Add (new ScenarioLine { Name = scenario.GetName (), Description = scenario.GetDescription () });
                }

                _listView.ItemHeight = 2;
                _listView.ColumnHeaders =
                [
                    new ListView.ColumnHeader { HeaderName = "Scenario", MemberName = "Name" },
                    new ListView.ColumnHeader { MemberName = "Description" }
                ];
                _listView.SetSource (lines);

                break;

            case 3:
                ObservableCollection<NumericLine> numbers = [];

                for (int i = 0; i < 40; i++)
                {
                    numbers.Add (new NumericLine
                    {
                        Item = $"Item{i}",
                        IntValue = i,
                        FloatValue = i / 2f,
                        DoubleValue = i / 3d,
                        DecimalValue = i / 4m,
                        CurrencyValue = i / 5m
                    });
                }

                _listView.ItemHeight = 1;
                _listView.ColumnHeaders =
                [
                    new ListView.ColumnHeader { MemberName = "Item" },
                    new ListView.ColumnHeader { MemberName = "IntValue", MemberType = typeof (int), MemberAlignment = Alignment.End },
                    new ListView.ColumnHeader
                    {
                        MemberName = "FloatValue", MemberType = typeof (float), MemberFormat = "{0:F3}", MemberAlignment = Alignment.End
                    },
                    new ListView.ColumnHeader
                    {
                        MemberName = "DoubleValue", MemberType = typeof (double), MemberFormat = "{0:N4}", MemberAlignment = Alignment.End
                    },
                    new ListView.ColumnHeader
                    {
                        MemberName = "DecimalValue", MemberType = typeof (decimal), MemberFormat = "{0:N2}", MemberAlignment = Alignment.End
                    },
                    new ListView.ColumnHeader
                    {
                        MemberName = "CurrencyValue", MemberType = typeof (decimal), MemberFormat = "{0:C2}", MemberAlignment = Alignment.End
                    }
                ];
                _listView.SetSource (numbers);

                break;

            case 4:
                ObservableCollection<string []> strings = [];
                int index = 0;

                foreach (Scenario scenario in _scenarios)
                {
                    strings.Add ([$"{index}", $"{scenario.GetName ()}", $"{scenario.GetDescription ()}"]);
                    index++;
                }

                _listView.ItemHeight = 1;
                _listView.ColumnHeaders =
                [
                    new ListView.ColumnHeader
                    {
                        HeaderName = "Index",
                        HeaderAlignment = Alignment.Center,
                        MemberAlignment = Alignment.Center,
                        MemberType = typeof (int),
                        MemberFormat = "{0:00}"
                    },
                    new ListView.ColumnHeader { HeaderName = "Scenario", HeaderAlignment = Alignment.Center },
                    new ListView.ColumnHeader { HeaderName = "Description", HeaderAlignment = Alignment.Center }
                ];
                _listView.SetSource (strings);

                break;

            case 5:
                _listView.ItemHeight = 1;
                _listView.ColumnHeaders = null;
                _listView.SetSource<string> (null);

                break;

            default:
                _listView.ItemHeight = 1;
                _listView.ColumnHeaders =
                [
                    new ListView.ColumnHeader { HeaderName = "Name", HeaderAlignment = Alignment.Center },
                    new ListView.ColumnHeader { HeaderName = "Description", HeaderAlignment = Alignment.Center }
                ];
                _listView.SetSource<string> (null);

                break;
        }

        _updatingHeight = true;
        _itemHeightField.Text = _listView.ItemHeight.ToString ();
        _updatingHeight = false;
        AdoptColumnAlignment ();
        ShowAlignment ();
    }

    private void AdoptColumnAlignment ()
    {
        if (_listView?.ColumnHeaders is not { Count: > 0 } headers)
        {
            return;
        }

        _listView.HeadersAlignment = headers [0].HeaderAlignment;
        _listView.ItemsAlignment = headers [0].MemberAlignment;
    }

    private void CycleAlignment (bool header)
    {
        if (_listView is null)
        {
            return;
        }

        if (header)
        {
            _listView.HeadersAlignment = Next (_listView.HeadersAlignment);
        }
        else
        {
            _listView.ItemsAlignment = Next (_listView.ItemsAlignment);
        }

        ShowAlignment ();

        if (_listView.ColumnHeaders is null)
        {
            _listView.SetNeedsDraw ();

            return;
        }

        foreach (ListView.ColumnHeader columnHeader in _listView.ColumnHeaders)
        {
            if (header)
            {
                columnHeader.HeaderAlignment = _listView.HeadersAlignment;
            }
            else
            {
                columnHeader.MemberAlignment = _listView.ItemsAlignment;
            }
        }

        _listView.SetNeedsDraw ();
    }

    private void ShowAlignment ()
    {
        if (_listView is null)
        {
            return;
        }

        if (_headerAlignLabel is not null)
        {
            _headerAlignLabel.Text = _listView.HeadersAlignment.ToString ();
        }

        if (_itemAlignLabel is not null)
        {
            _itemAlignLabel.Text = _listView.ItemsAlignment.ToString ();
        }
    }

    private static Alignment Next (Alignment alignment)
    {
        Alignment [] values = Enum.GetValues<Alignment> ();
        int index = Array.IndexOf (values, alignment);

        return values [(index + 1) % values.Length];
    }

    private void OnItemHeightChanged ()
    {
        if (_updatingHeight || _listView is null || _itemHeightField is null || _app is null)
        {
            return;
        }

        if (!int.TryParse (_itemHeightField.Text, out int height))
        {
            return;
        }

        try
        {
            _listView.ItemHeight = height;
        }
        catch (ArgumentOutOfRangeException ex)
        {
            MessageBox.ErrorQuery (_app, "Error", ex.Message, "Ok");
            _updatingHeight = true;
            _itemHeightField.Text = _listView.ItemHeight.ToString ();
            _updatingHeight = false;
        }
    }

    private void OnRowRender (object? sender, ListViewRowEventArgs e)
    {
        if (_listView is null || e.Row == _listView.SelectedItem)
        {
            return;
        }

        if (_listView.ShowMarks && _listView.Source?.IsMarked (e.Row) == true)
        {
            e.RowAttribute = new Attribute (Color.BrightRed, Color.BrightYellow);

            return;
        }

        e.RowAttribute = e.Row % 2 == 0
                             ? new Attribute (Color.Green, Color.Magenta)
                             : new Attribute (Color.Magenta, Color.Green);
    }

    private sealed class ScenarioColumn
    {
        public string Scenario { get; init; } = "";

        public string Description { get; init; } = "";
    }

    private sealed class ScenarioLine
    {
        public string Name { get; init; } = "";

        public string Description { get; init; } = "";
    }

    private sealed class NumericLine
    {
        public string Item { get; init; } = "";

        public int IntValue { get; init; }

        public float FloatValue { get; init; }

        public double DoubleValue { get; init; }

        public decimal DecimalValue { get; init; }

        public decimal CurrencyValue { get; init; }
    }
}
