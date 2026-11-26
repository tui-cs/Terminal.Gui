#nullable enable

using System.Collections.ObjectModel;
using System.Globalization;

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
        "Headers/Footers only",
        "Column wrap"
    ];

    private IApplication? _app;
    private ListView? _listView;
    private TextField? _itemHeightField;
    private TextField? _headerHeightField;
    private TextField? _footerHeightField;
    private Label? _modeLabel;
    private Label? _headerAlignLabel;
    private Label? _headerVerticalAlignLabel;
    private Label? _itemHorizontalAlignLabel;
    private Label? _itemVerticalAlignLabel;
    private Label? _footerAlignLabel;
    private Label? _footerVerticalAlignLabel;
    private Label? _footerPlaceLabel;
    private CheckBox? _showHeaders;
    private CheckBox? _showFooters;
    private int _mode;
    private bool _updatingHeight;
    private bool _updatingHeaderHeight;
    private bool _updatingFooterHeight;
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

        _showHeaders = new CheckBox
        {
            X = 0,
            Y = Pos.Bottom (nextMode),
            Text = "Show _headers"
        };
        _showHeaders.ValueChanging += (_, e) =>
                                     {
                                         if (_listView is null)
                                         {
                                             return;
                                         }

                                         _listView.ShowColumnHeaders = e.NewValue == CheckState.Checked;
                                     };
        appWindow.Add (_showHeaders);

        _showFooters = new CheckBox
        {
            X = Pos.Right (_showHeaders) + 1,
            Y = Pos.Top (_showHeaders),
            Text = "Show _footers"
        };
        _showFooters.ValueChanging += (_, e) =>
                                      {
                                          if (_listView is null)
                                          {
                                              return;
                                          }

                                          _listView.ShowColumnFooters = e.NewValue == CheckState.Checked;
                                      };
        appWindow.Add (_showFooters);

        CheckBox allowsSorting = new ()
        {
            X = Pos.Right (_showFooters) + 1,
            Y = Pos.Top (_showHeaders),
            Text = "Allow _sorting"
        };
        allowsSorting.ValueChanging += (_, e) =>
                                      {
                                          if (_listView is null)
                                          {
                                              return;
                                          }

                                          _listView.AllowsSorting = e.NewValue == CheckState.Checked;
                                      };
        appWindow.Add (allowsSorting);

        CheckBox showMarks = new ()
        {
            X = Pos.Right (allowsSorting) + 1,
            Y = Pos.Top (_showHeaders),
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
            Y = Pos.Bottom (_showHeaders),
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

        CheckBox itemAutoHeight = new ()
        {
            X = Pos.Right (_itemHeightField) + 1,
            Y = Pos.Top (heightLabel),
            Text = "Item _auto height"
        };
        itemAutoHeight.ValueChanging += (_, e) =>
                                        {
                                            if (_listView is null)
                                            {
                                                return;
                                            }

                                            _listView.ItemAutoHeight = e.NewValue == CheckState.Checked;
                                        };
        appWindow.Add (itemAutoHeight);

        Button itemHorizontalAlign = new ()
        {
            X = Pos.Right (itemAutoHeight) + 2,
            Y = Pos.Top (heightLabel),
            Text = "Item _H align"
        };
        itemHorizontalAlign.Accepting += (_, e) =>
                                         {
                                             e.Handled = true;
                                             CycleAlignment (header: false);
                                         };
        appWindow.Add (itemHorizontalAlign);

        _itemHorizontalAlignLabel = new Label
        {
            X = Pos.Right (itemHorizontalAlign) + 1,
            Y = Pos.Top (heightLabel),
            Width = 6,
            Text = nameof (Alignment.Start)
        };
        appWindow.Add (_itemHorizontalAlignLabel);

        Button itemVerticalAlign = new ()
        {
            X = Pos.Right (_itemHorizontalAlignLabel) + 1,
            Y = Pos.Top (heightLabel),
            Text = "Item _V align"
        };
        itemVerticalAlign.Accepting += (_, e) =>
                                       {
                                           e.Handled = true;
                                           CycleVerticalAlignment ();
                                       };
        appWindow.Add (itemVerticalAlign);

        _itemVerticalAlignLabel = new Label
        {
            X = Pos.Right (itemVerticalAlign) + 1,
            Y = Pos.Top (heightLabel),
            Width = 6,
            Text = nameof (Alignment.Start)
        };
        appWindow.Add (_itemVerticalAlignLabel);

        Label headerHeightLabel = new ()
        {
            X = 0,
            Y = Pos.Bottom (heightLabel),
            Text = "Header hei_ght:"
        };
        appWindow.Add (headerHeightLabel);

        _headerHeightField = new TextField
        {
            X = Pos.Right (headerHeightLabel) + 1,
            Y = Pos.Top (headerHeightLabel),
            Width = 4,
            Text = "1"
        };
        _headerHeightField.TextChanged += (_, _) => OnHeaderHeightChanged ();
        appWindow.Add (_headerHeightField);

        Button headerAlign = new ()
        {
            X = Pos.Right (_headerHeightField) + 2,
            Y = Pos.Top (headerHeightLabel),
            Text = "Header _H align"
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
            Y = Pos.Top (headerHeightLabel),
            Width = 6,
            Text = nameof (Alignment.Center)
        };
        appWindow.Add (_headerAlignLabel);

        Button headerVerticalAlign = new ()
        {
            X = Pos.Right (_headerAlignLabel) + 1,
            Y = Pos.Top (headerHeightLabel),
            Text = "Header _V align"
        };
        headerVerticalAlign.Accepting += (_, e) =>
                                         {
                                             e.Handled = true;
                                             CycleHeaderVerticalAlignment ();
                                         };
        appWindow.Add (headerVerticalAlign);

        _headerVerticalAlignLabel = new Label
        {
            X = Pos.Right (headerVerticalAlign) + 1,
            Y = Pos.Top (headerHeightLabel),
            Width = 6,
            Text = nameof (Alignment.Start)
        };
        appWindow.Add (_headerVerticalAlignLabel);

        Label footerHeightLabel = new ()
        {
            X = 0,
            Y = Pos.Bottom (headerHeightLabel),
            Text = "Footer h_eight:"
        };
        appWindow.Add (footerHeightLabel);

        _footerHeightField = new TextField
        {
            X = Pos.Right (footerHeightLabel) + 1,
            Y = Pos.Top (footerHeightLabel),
            Width = 4,
            Text = "1"
        };
        _footerHeightField.TextChanged += (_, _) => OnFooterHeightChanged ();
        appWindow.Add (_footerHeightField);

        Button footerAlign = new ()
        {
            X = Pos.Right (_footerHeightField) + 2,
            Y = Pos.Top (footerHeightLabel),
            Text = "Footer _H align"
        };
        footerAlign.Accepting += (_, e) =>
                                 {
                                     e.Handled = true;
                                     CycleFooterAlignment ();
                                 };
        appWindow.Add (footerAlign);

        _footerAlignLabel = new Label
        {
            X = Pos.Right (footerAlign) + 1,
            Y = Pos.Top (footerHeightLabel),
            Width = 6,
            Text = nameof (Alignment.Start)
        };
        appWindow.Add (_footerAlignLabel);

        Button footerVerticalAlign = new ()
        {
            X = Pos.Right (_footerAlignLabel) + 1,
            Y = Pos.Top (footerHeightLabel),
            Text = "Footer _V align"
        };
        footerVerticalAlign.Accepting += (_, e) =>
                                         {
                                             e.Handled = true;
                                             CycleFooterVerticalAlignment ();
                                         };
        appWindow.Add (footerVerticalAlign);

        _footerVerticalAlignLabel = new Label
        {
            X = Pos.Right (footerVerticalAlign) + 1,
            Y = Pos.Top (footerHeightLabel),
            Width = 6,
            Text = nameof (Alignment.Start)
        };
        appWindow.Add (_footerVerticalAlignLabel);

        Button footerPlace = new ()
        {
            X = Pos.Right (_footerVerticalAlignLabel) + 1,
            Y = Pos.Top (footerHeightLabel),
            Text = "Footer _place"
        };
        footerPlace.Accepting += (_, e) =>
                                 {
                                     e.Handled = true;
                                     CycleFooterPlacement ();
                                 };
        appWindow.Add (footerPlace);

        _footerPlaceLabel = new Label
        {
            X = Pos.Right (footerPlace) + 1,
            Y = Pos.Top (footerHeightLabel),
            Width = 10,
            Text = "Bottom"
        };
        appWindow.Add (_footerPlaceLabel);

        _listView = new ListView
        {
            X = 0,
            Y = Pos.Bottom (footerHeightLabel),
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

                _showHeaders?.Value = CheckState.UnChecked;
                _showFooters?.Value = CheckState.UnChecked;
                _listView.ItemsHorizontalAlignment = Alignment.Start;
                _listView.ItemHeight = 2;
                _listView.ColumnHeaders = null;
                _listView.SetSource (colLines);

                break;

            case 1:
                _showHeaders?.Value = CheckState.UnChecked;
                _listView.ItemHeight = 1;
                _listView.ColumnHeaders = null;
                _listView.SetSource (_scenarios);

                break;

            case 2:
                ObservableCollection<ScenarioLine> lines = [];
                int descriptionChars = 0;

                foreach (Scenario scenario in _scenarios)
                {
                    string description = scenario.GetDescription ();
                    descriptionChars += description.Length;
                    lines.Add (new ScenarioLine { Name = scenario.GetName (), Description = description });
                }

                _showHeaders?.Value = CheckState.Checked;
                _showFooters?.Value = CheckState.Checked;
                _listView.ItemHeight = 2;
                _listView.ColumnHeaders =
                [
                    new ListView.ColumnHeader { HeaderName = "Scenario\nName", MemberName = "Name" },
                    new ListView.ColumnHeader { HeaderName = "Description\nText", MemberName = "Description" }
                ];
                EnableFooters (
                [
                    new ListView.ColumnFooter { FooterName = $"Names\n{lines.Count}" },
                    new ListView.ColumnFooter { FooterName = $"Chars\n{descriptionChars}" }
                ]);
                _listView.SetSource (lines);

                break;

            case 3:
                ObservableCollection<NumericLine> numbers = [];
                int intSum = 0;
                float floatSum = 0;
                double doubleSum = 0;
                decimal decimalSum = 0;
                decimal currencySum = 0;

                for (int i = 0; i < 40; i++)
                {
                    NumericLine line = new ()
                    {
                        Item = $"Item{i}",
                        IntValue = i,
                        FloatValue = i / 2f,
                        DoubleValue = i / 3d,
                        DecimalValue = i / 4m,
                        CurrencyValue = i / 5m
                    };
                    intSum += line.IntValue;
                    floatSum += line.FloatValue;
                    doubleSum += line.DoubleValue;
                    decimalSum += line.DecimalValue;
                    currencySum += line.CurrencyValue;
                    numbers.Add (line);
                }

                _showHeaders?.Value = CheckState.Checked;
                _listView.ItemHeight = 1;
                _listView.ColumnHeaders =
                [
                    new ListView.ColumnHeader { MemberName = "Item" },
                    new ListView.ColumnHeader { MemberName = "IntValue", MemberType = typeof (int), MemberHorizontalAlignment = Alignment.End },
                    new ListView.ColumnHeader
                    {
                        MemberName = "FloatValue", MemberType = typeof (float), MemberFormat = "{0:F3}", MemberHorizontalAlignment = Alignment.End
                    },
                    new ListView.ColumnHeader
                    {
                        MemberName = "DoubleValue", MemberType = typeof (double), MemberFormat = "{0:N4}", MemberHorizontalAlignment = Alignment.End
                    },
                    new ListView.ColumnHeader
                    {
                        MemberName = "DecimalValue", MemberType = typeof (decimal), MemberFormat = "{0:N2}", MemberHorizontalAlignment = Alignment.End
                    },
                    new ListView.ColumnHeader
                    {
                        MemberName = "CurrencyValue", MemberType = typeof (decimal), MemberFormat = "{0:C2}", MemberHorizontalAlignment = Alignment.End
                    }
                ];

                EnableFooters (
                [
                    new ListView.ColumnFooter { FooterName = "Total" },
                    new ListView.ColumnFooter { FooterName = intSum.ToString () },
                    // ReSharper disable InvocationIsCultureSensitive
                    new ListView.ColumnFooter { FooterName = floatSum.ToString () },
                    new ListView.ColumnFooter { FooterName = doubleSum.ToString () },
                    new ListView.ColumnFooter { FooterName = decimalSum.ToString () },
                    new ListView.ColumnFooter { FooterName = currencySum.ToString () }
                    // ReSharper restore InvocationIsCultureSensitive
                ]);
                _listView.SetSource (numbers);

                break;

            case 4:
                ObservableCollection<string []> strings = [];
                int index = 0;
                int indexSum = 0;
                int nameChars = 0;
                int textChars = 0;

                foreach (Scenario scenario in _scenarios)
                {
                    string name = scenario.GetName ();
                    string description = scenario.GetDescription ();
                    strings.Add ([$"{index}", name, description]);
                    indexSum += index;
                    nameChars += name.Length;
                    textChars += description.Length;
                    index++;
                }

                _showHeaders?.Value = CheckState.Checked;
                _listView.ItemHeight = 1;
                _listView.ColumnHeaders =
                [
                    new ListView.ColumnHeader
                    {
                        HeaderName = "Index\nNo",
                        HeaderHorizontalAlignment = Alignment.Center,
                        MemberHorizontalAlignment = Alignment.Center,
                        MemberType = typeof (int),
                        MemberFormat = "{0:00}"
                    },
                    new ListView.ColumnHeader { HeaderName = "Scenario\nName", HeaderHorizontalAlignment = Alignment.Center },
                    new ListView.ColumnHeader { HeaderName = "Description\nText", HeaderHorizontalAlignment = Alignment.Center }
                ];
                EnableFooters (
                [
                    new ListView.ColumnFooter { FooterName = $"Sum\n{indexSum:00}", Format = "" },
                    new ListView.ColumnFooter { FooterName = $"Names\n{nameChars}" },
                    new ListView.ColumnFooter { FooterName = $"Text\n{textChars}" }
                ]);
                _listView.SetSource (strings);

                break;

            case 5:
                _showHeaders?.Value = CheckState.UnChecked;
                _showFooters?.Value = CheckState.UnChecked;
                _listView.ItemHeight = 1;
                _listView.ColumnHeaders = null;
                _listView.SetSource<string> (null);

                break;

            case 6:
                _showHeaders?.Value = CheckState.Checked;
                _listView.ItemHeight = 1;
                _listView.ColumnHeaders =
                [
                    new ListView.ColumnHeader { HeaderName = "Name\nCol", HeaderHorizontalAlignment = Alignment.Center },
                    new ListView.ColumnHeader { HeaderName = "Description\nText", HeaderHorizontalAlignment = Alignment.Center }
                ];
                EnableFooters (
                [
                    new ListView.ColumnFooter { FooterName = "Rows\n0" },
                    new ListView.ColumnFooter { FooterName = "Text\n0" }
                ]);
                _listView.SetSource<string> (null);

                break;

            case 7:
                ObservableCollection<string []> wrapped = [];
                int wrappedIndex = 0;
                int wrappedNameChars = 0;
                int wrappedTextChars = 0;

                foreach (Scenario scenario in _scenarios)
                {
                    string name = scenario.GetName ();
                    string description = scenario.GetDescription ();
                    wrapped.Add ([$"{wrappedIndex}", name, description]);
                    wrappedNameChars += name.Length;
                    wrappedTextChars += description.Length;
                    wrappedIndex++;
                }

                // Width is narrower than the header and footer text, and Height is 2, so each one wraps.
                _listView.ItemHeight = 2;
                _listView.ColumnHeaders =
                [
                    new ListView.ColumnHeader
                    {
                        HeaderName = "Index number",
                        Width = 6,
                        Height = 2,
                        HeaderHorizontalAlignment = Alignment.Center,
                        MemberHorizontalAlignment = Alignment.Center,
                        MemberType = typeof (int),
                        MemberFormat = "{0:00}"
                    },
                    new ListView.ColumnHeader
                    {
                        HeaderName = "Scenario name",
                        Width = 10,
                        Height = 2,
                        HeaderHorizontalAlignment = Alignment.Start
                    },
                    new ListView.ColumnHeader
                    {
                        HeaderName = "Long description text",
                        Width = 16,
                        Height = 2,
                        HeaderHorizontalAlignment = Alignment.Start
                    }
                ];
                EnableFooters (
                [
                    new ListView.ColumnFooter { FooterName = $"Total rows {wrapped.Count}", Format = "", Height = 2 },
                    new ListView.ColumnFooter { FooterName = $"Name chars {wrappedNameChars}", Format = "", Height = 2 },
                    new ListView.ColumnFooter { FooterName = $"Description chars {wrappedTextChars}", Format = "", Height = 2 }
                ]);
                _listView.SetSource (wrapped);

                if (_headerHeightField is not null)
                {
                    _updatingHeaderHeight = true;
                    _headerHeightField.Text = "2";
                    _updatingHeaderHeight = false;
                }

                if (_footerHeightField is not null)
                {
                    _updatingFooterHeight = true;
                    _footerHeightField.Text = "2";
                    _updatingFooterHeight = false;
                }

                _showHeaders?.Value = CheckState.Checked;

                break;
        }

        _updatingHeight = true;
        _itemHeightField.Text = _listView.ItemHeight.ToString ();
        _updatingHeight = false;
        ApplyHeaderHeightFromField ();
        ApplyFooterHeightFromField ();
        AdoptColumnAlignment ();
        ShowAlignment ();
    }

    private void EnableFooters (ListView.ColumnFooter [] footers)
    {
        if (_listView is null)
        {
            return;
        }

        _listView.ColumnFooters = [.. footers];
        _showFooters?.Value = CheckState.Checked;
    }

    private void ApplyFooterHeightFromField ()
    {
        if (_listView is null || _footerHeightField is null || !int.TryParse (_footerHeightField.Text, out int height))
        {
            return;
        }

        try
        {
            ApplyFooterHeight (height);
        }
        catch (ArgumentOutOfRangeException)
        {
            // OnFooterHeightChanged reports a height typed while the scenario is running.
        }
    }

    private void ApplyHeaderHeightFromField ()
    {
        if (_listView is null || _headerHeightField is null || !int.TryParse (_headerHeightField.Text, out int height))
        {
            return;
        }

        try
        {
            ApplyHeaderHeight (height);
        }
        catch (ArgumentOutOfRangeException)
        {
            // OnHeaderHeightChanged reports a height typed while the scenario is running.
        }
    }

    private void AdoptColumnAlignment ()
    {
        if (_listView?.ColumnHeaders is not { Count: > 0 } headers)
        {
            return;
        }

        _listView.HeadersAlignment = headers [0].HeaderHorizontalAlignment;
        _listView.HeadersVerticalAlignment = headers [0].HeaderVerticalAlignment;
        _listView.ItemsHorizontalAlignment = headers [0].MemberHorizontalAlignment;
        _listView.ItemsVerticalAlignment = headers [0].MemberVerticalAlignment;
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
            _listView.ItemsHorizontalAlignment = Next (_listView.ItemsHorizontalAlignment);
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
                columnHeader.HeaderHorizontalAlignment = _listView.HeadersAlignment;
            }
            else
            {
                columnHeader.MemberHorizontalAlignment = _listView.ItemsHorizontalAlignment;
            }
        }

        _listView.SetNeedsDraw ();
    }

    private void CycleVerticalAlignment ()
    {
        if (_listView is null)
        {
            return;
        }

        _listView.ItemsVerticalAlignment = Next (_listView.ItemsVerticalAlignment);
        ApplyMemberVerticalAlignment ();
        ShowAlignment ();
    }

    private void CycleHeaderVerticalAlignment ()
    {
        if (_listView is null)
        {
            return;
        }

        _listView.HeadersVerticalAlignment = Next (_listView.HeadersVerticalAlignment);
        ApplyHeaderVerticalAlignment ();
        ShowAlignment ();
    }

    private void CycleFooterAlignment ()
    {
        if (_listView is null)
        {
            return;
        }

        Alignment next = Next (CurrentFooterHorizontalAlignment ());
        _listView.FootersHorizontalAlignment = next;
        ApplyFooterHorizontalAlignment (next);
        ShowAlignment ();
    }

    private void CycleFooterVerticalAlignment ()
    {
        if (_listView is null)
        {
            return;
        }

        Alignment next = Next (CurrentFooterVerticalAlignment ());
        _listView.FootersVerticalAlignment = next;
        ApplyFooterVerticalAlignment (next);
        ShowAlignment ();
    }

    private void CycleFooterPlacement ()
    {
        if (_listView is null)
        {
            return;
        }

        _listView.FooterPlacement = _listView.FooterPlacement == ListView.ColumnFooterPlacement.Bottom
                                        ? ListView.ColumnFooterPlacement.AfterLastItem
                                        : ListView.ColumnFooterPlacement.Bottom;
        ShowAlignment ();
    }

    private void ApplyFooterHorizontalAlignment (Alignment alignment)
    {
        if (_listView?.ColumnFooters is not { } footers)
        {
            _listView?.SetNeedsDraw ();

            return;
        }

        foreach (ListView.ColumnFooter footer in footers)
        {
            footer.HorizontalAlignment = alignment;
        }

        _listView.SetNeedsDraw ();
    }

    private void ApplyFooterVerticalAlignment (Alignment alignment)
    {
        if (_listView?.ColumnFooters is not { } footers)
        {
            _listView?.SetNeedsDraw ();

            return;
        }

        foreach (ListView.ColumnFooter footer in footers)
        {
            footer.VerticalAlignment = alignment;
        }

        _listView.SetNeedsDraw ();
    }

    private Alignment CurrentFooterHorizontalAlignment ()
    {
        if (_listView?.ColumnFooters is { Count: > 0 } footers)
        {
            ListView.ColumnHeader? header = _listView.ColumnHeaders is { Count: > 0 } headers ? headers [0] : null;

            return footers [0].ResolveHorizontalAlignment (header, _listView.FootersHorizontalAlignment);
        }

        return _listView?.FootersHorizontalAlignment ?? Alignment.Start;
    }

    private Alignment CurrentFooterVerticalAlignment ()
    {
        if (_listView?.ColumnFooters is { Count: > 0 } footers)
        {
            ListView.ColumnHeader? header = _listView.ColumnHeaders is { Count: > 0 } headers ? headers [0] : null;

            return footers [0].ResolveVerticalAlignment (header, _listView.FootersVerticalAlignment);
        }

        return _listView?.FootersVerticalAlignment ?? Alignment.Start;
    }

    private void ApplyMemberVerticalAlignment ()
    {
        if (_listView is null)
        {
            return;
        }

        if (_listView.ColumnHeaders is not { } headers)
        {
            _listView.SetNeedsDraw ();

            return;
        }

        foreach (ListView.ColumnHeader columnHeader in headers)
        {
            columnHeader.MemberVerticalAlignment = _listView.ItemsVerticalAlignment;
        }

        _listView.SetNeedsDraw ();
    }

    private void ApplyHeaderVerticalAlignment ()
    {
        if (_listView is null)
        {
            return;
        }

        if (_listView.ColumnHeaders is not { } headers)
        {
            _listView.SetNeedsDraw ();

            return;
        }

        foreach (ListView.ColumnHeader columnHeader in headers)
        {
            columnHeader.HeaderVerticalAlignment = _listView.HeadersVerticalAlignment;
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

        _headerVerticalAlignLabel?.Text = _listView.HeadersVerticalAlignment.ToString ();
        _itemHorizontalAlignLabel?.Text = _listView.ItemsHorizontalAlignment.ToString ();
        _itemVerticalAlignLabel?.Text = _listView.ItemsVerticalAlignment.ToString ();
        _footerAlignLabel?.Text = CurrentFooterHorizontalAlignment ().ToString ();
        _footerVerticalAlignLabel?.Text = CurrentFooterVerticalAlignment ().ToString ();
        _footerPlaceLabel?.Text = FooterPlaceText (_listView.FooterPlacement);
    }

    private static string FooterPlaceText (ListView.ColumnFooterPlacement placement)
    {
        if (placement == ListView.ColumnFooterPlacement.AfterLastItem)
        {
            return "After item";
        }

        return "Bottom";
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

    private void OnHeaderHeightChanged ()
    {
        if (_updatingHeaderHeight || _listView is null || _headerHeightField is null || _app is null)
        {
            return;
        }

        if (!int.TryParse (_headerHeightField.Text, out int height))
        {
            return;
        }

        try
        {
            ApplyHeaderHeight (height);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            MessageBox.ErrorQuery (_app, "Error", ex.Message, "Ok");
            _updatingHeaderHeight = true;
            _headerHeightField.Text = CurrentHeaderHeight ().ToString ();
            _updatingHeaderHeight = false;
        }
    }

    private void OnFooterHeightChanged ()
    {
        if (_updatingFooterHeight || _listView is null || _footerHeightField is null || _app is null)
        {
            return;
        }

        if (!int.TryParse (_footerHeightField.Text, out int height))
        {
            return;
        }

        try
        {
            ApplyFooterHeight (height);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            MessageBox.ErrorQuery (_app, "Error", ex.Message, "Ok");
            _updatingFooterHeight = true;
            _footerHeightField.Text = CurrentFooterHeight ().ToString ();
            _updatingFooterHeight = false;
        }
    }

    private void ApplyFooterHeight (int height)
    {
        if (_listView?.ColumnFooters is not { Count: > 0 } footers)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan (height, 1);

            return;
        }

        foreach (ListView.ColumnFooter footer in footers)
        {
            footer.Height = height;
        }
    }

    private int CurrentFooterHeight ()
    {
        if (_listView?.ColumnFooters is { Count: > 0 } footers)
        {
            return footers [0].Height;
        }

        return 1;
    }

    private void ApplyHeaderHeight (int height)
    {
        if (_listView?.ColumnHeaders is not { Count: > 0 } headers)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan (height, 1);

            return;
        }

        foreach (ListView.ColumnHeader columnHeader in headers)
        {
            columnHeader.Height = height;
        }
    }

    private int CurrentHeaderHeight ()
    {
        if (_listView?.ColumnHeaders is { Count: > 0 } headers)
        {
            return headers [0].Height;
        }

        return 1;
    }

    private void OnRowRender (object? sender, ListViewRowEventArgs e)
    {
        if (_listView is null || e.Row == _listView.SelectedItem)
        {
            return;
        }

        if (_listView.ShowMarks && _listView.Source?.IsMarked (e.Row) == true)
        {
            e.RowAttribute = _listView.GetAttributeForRole (VisualRole.HotFocus);

            return;
        }

        e.RowAttribute = e.Row % 2 == 0
                             ? _listView.GetAttributeForRole (VisualRole.Active)
                             : _listView.GetAttributeForRole (VisualRole.Normal);
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
