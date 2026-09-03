#nullable enable
using System.Collections.ObjectModel;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Generic ListView<T>", "Demonstrates ListView<T> with typed Value, SelectedItem, Index, and RowRender for custom row coloring")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("ListView")]
public class GenericListView : Scenario
{
    private ListView<Country>? _listView;
    private ObservableCollection<string> _eventList = [];
    private ListView? _eventListView;
    private Label? _nameLabel;
    private Label? _capitalLabel;
    private Label? _populationLabel;
    private Label? _indexLabel;
    private CheckBox? _cancelNextCb;
    private bool _cancelNext;

    /// <inheritdoc/>
    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();

        using Window appWindow = new ();
        appWindow.Title = GetQuitKeyAndName ();

        ObservableCollection<Country> countries =
        [
            new ("Australia", "Canberra", 26_000_000),
            new ("Brazil", "Brasília", 215_000_000),
            new ("Canada", "Ottawa", 38_000_000),
            new ("Denmark", "Copenhagen", 5_900_000),
            new ("Egypt", "Cairo", 104_000_000),
            new ("France", "Paris", 68_000_000),
            new ("Germany", "Berlin", 84_000_000),
            new ("Hungary", "Budapest", 9_700_000),
            new ("India", "New Delhi", 1_428_000_000),
            new ("Japan", "Tokyo", 124_000_000)
        ];

        _cancelNextCb = new CheckBox
        {
            X = 0,
            Y = 0,
            Text = "C_ancel next selection change"
        };
        _cancelNextCb.ValueChanging += (_, args) => _cancelNext = args.NewValue == CheckState.Checked;
        appWindow.Add (_cancelNextCb);

        _listView = new ListView<Country>
        {
            Title = "_Countries",
            X = 0,
            Y = Pos.Bottom (_cancelNextCb) + 1,
            Width = 22,
            Height = Dim.Fill (),
            BorderStyle = LineStyle.Single
        };
        _listView.SetSource (countries);
        appWindow.Add (_listView);

        // Build highlight scheme by inheriting the resolved scheme and only
        // overriding foreground colors so the background stays theme-consistent.
        Scheme baseScheme = _listView.GetScheme ();
        Scheme highlightScheme = new ()
        {
            Normal = baseScheme.Normal with { Foreground = Color.BrightRed },
            Focus  = baseScheme.Focus  with { Foreground = Color.BrightRed, Style = TextStyle.Bold }
        };

        // Use RowRender to color rows with population > 100M (demonstrates
        // how to achieve per-row coloring).
        _listView.RowRender += (_, args) =>
        {
            if (args.Row < countries.Count && countries [args.Row].Population > 100_000_000)
            {
                bool isSelected = args.Row == _listView.Index;
                args.RowAttribute = isSelected ? highlightScheme.Focus : highlightScheme.Normal;
            }
        };

        FrameView detailPanel = new ()
        {
            Title = "_Selected",
            X = Pos.Right (_listView) + 1,
            Y = Pos.Top (_listView),
            Width = Dim.Fill (),
            Height = 9
        };
        appWindow.Add (detailPanel);

        Label nameTitleLbl = new () { X = 1, Y = 1, Text = "Name:      " };
        detailPanel.Add (nameTitleLbl);
        _nameLabel = new () { X = Pos.Right (nameTitleLbl), Y = 1, Width = Dim.Fill (1), Text = "(none)" };
        detailPanel.Add (_nameLabel);

        Label capitalTitleLbl = new () { X = 1, Y = 2, Text = "Capital:   " };
        detailPanel.Add (capitalTitleLbl);
        _capitalLabel = new () { X = Pos.Right (capitalTitleLbl), Y = 2, Width = Dim.Fill (1), Text = "" };
        detailPanel.Add (_capitalLabel);

        Label populationTitleLbl = new () { X = 1, Y = 3, Text = "Population:" };
        detailPanel.Add (populationTitleLbl);
        _populationLabel = new () { X = Pos.Right (populationTitleLbl), Y = 3, Width = Dim.Fill (1), Text = "" };
        detailPanel.Add (_populationLabel);

        Label indexTitleLbl = new () { X = 1, Y = 5, Text = "Index:     " };
        detailPanel.Add (indexTitleLbl);
        _indexLabel = new () { X = Pos.Right (indexTitleLbl), Y = 5, Width = Dim.Fill (1), Text = "" };
        detailPanel.Add (_indexLabel);

        _eventList = [];
        _eventListView = new ListView
        {
            Title = "_Events",
            X = Pos.Right (_listView) + 1,
            Y = Pos.Bottom (detailPanel) + 1,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Source = new ListWrapper<string> (_eventList),
            BorderStyle = LineStyle.Single
        };
        appWindow.Add (_eventListView);

        _listView.ValueChanging += OnValueChanging;
        _listView.ValueChanged += OnValueChanged;

        app.Run (appWindow);
    }

    private void OnValueChanging (object? sender, ValueChangingEventArgs<Country?> args)
    {
        if (_cancelNext)
        {
            args.Handled = true;
            _cancelNext = false;

            _cancelNextCb?.Value = CheckState.UnChecked;

            LogEvent ($"ValueChanging CANCELLED: {FormatCountry (args.CurrentValue)} -> {FormatCountry (args.NewValue)}");

            return;
        }

        LogEvent ($"ValueChanging: {FormatCountry (args.CurrentValue)} -> {FormatCountry (args.NewValue)}");
    }

    private void OnValueChanged (object? sender, ValueChangedEventArgs<Country?> args)
    {
        UpdateDetail (args.NewValue);
        LogEvent ($"ValueChanged:  {FormatCountry (args.OldValue)} -> {FormatCountry (args.NewValue)}");
    }

    private void UpdateDetail (Country? country)
    {
        if (_nameLabel is null)
        {
            return;
        }

        if (country is null)
        {
            _nameLabel.Text = "(none)";
            _capitalLabel!.Text = "";
            _populationLabel!.Text = "";
            _indexLabel!.Text = "";

            return;
        }

        _nameLabel.Text = country.Name;
        _capitalLabel!.Text = country.Capital;
        _populationLabel!.Text = $"{country.Population:N0}";
        _indexLabel!.Text = _listView?.Index?.ToString () ?? "";
    }

    private void LogEvent (string message)
    {
        _eventList.Add (message);

        _eventListView?.MoveEnd ();
    }

    private static string FormatCountry (Country? c) { return c is null ? "null" : c.Name; }
}

/// <summary>A simple record used to demonstrate <see cref="ListView{T}"/>.</summary>
internal record Country (string Name, string Capital, int Population)
{
    // Overriding ToString() so ListView<Country> only displays the country name.
    public override string ToString () { return Name; }
}
