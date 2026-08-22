#nullable enable
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("ListView With Selection", "ListView with custom rendering, columns, and selection")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("ListView")]
[ScenarioCategory ("Scrolling")]
public class ListViewWithSelection : Scenario
{
    private CheckBox? _showMarksCb;
    private CheckBox? _markMultipleCb;
    private CheckBox? _customRenderCb;
    private ListView? _listView;
    private ObservableCollection<Scenario>? _scenarios;
    private Window? _appWindow;
    private ViewportSettingsEditor? _viewportSettingsEditor;

    private ObservableCollection<string> _eventList = [];
    private ListView? _eventListView;

    /// <inheritdoc/>
    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();

        _appWindow = new Window { Title = GetQuitKeyAndName () };

        _scenarios = GetScenarios ();

        _customRenderCb = new CheckBox { X = 0, Y = 0, Text = "Custom _Rendering", Value = CheckState.UnChecked };
        _appWindow.Add (_customRenderCb);
        _customRenderCb.ValueChanging += CustomRenderCB_Toggle;

        _showMarksCb = new CheckBox { X = Pos.Right (_customRenderCb) + 1, Y = 0, Text = "Show_Marks", AllowCheckStateNone = false };
        _appWindow.Add (_showMarksCb);
        _showMarksCb.ValueChanging += ShowMarksCB_Toggle;

        _markMultipleCb = new CheckBox { X = Pos.Right (_showMarksCb) + 1, Y = 0, Text = "Mark_Multiple" };
        _appWindow.Add (_markMultipleCb);
        _markMultipleCb.ValueChanging += MarkMultipleCB_Toggle;

        _viewportSettingsEditor = new ViewportSettingsEditor
        {
            Title = "_ViewportSettings",
            Y = Pos.Bottom (_markMultipleCb),
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            CanFocus = true,
            AutoSelectViewToEdit = false,
            AutoSelectAdornments = false,
            SuperViewRendersLineCanvas = true
        };
        _appWindow.Add (_viewportSettingsEditor);

        _listView = new ListView
        {
            Title = "_ListView",
            X = 0,
            Y = Pos.Bottom (_viewportSettingsEditor),
            Width = Dim.Func (_ => _listView?.MaxItemLength ?? 10),
            Height = Dim.Fill (),
            ShowMarks = false,
            MarkMultiple = false,
            BorderStyle = LineStyle.Dotted,
            Arrangement = ViewArrangement.Resizable
        };
        _listView?.RowRender += ListView_RowRender;
        _appWindow.Add (_listView);

        _listView?.SetSource (_scenarios);

        _eventList = new ObservableCollection<string> ();

        _eventListView = new ListView
        {
            X = Pos.Right (_listView!) + 1,
            Y = Pos.Top (_listView!),
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Source = new ListWrapper<string> (_eventList)
        };
        _eventListView.SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Accent);
        _appWindow.Add (_eventListView);

        _listView?.ValueChanged += (_, a) => LogEvent ($"ValueChanged: {a.OldValue} -> {a.NewValue}");
        _listView?.CollectionChanged += (_, a) => LogEvent ($"CollectionChanged: {a}");
        _listView?.Accepting += (_, a) => LogEvent ($"Accept: {a}");
        _listView?.Activating += (_, a) => LogEvent ($"Activate: {a}");

        _listView?.ViewportSettings |= ViewportSettingsFlags.HasScrollBars;

        _listView?.Initialized += (_, _) => _viewportSettingsEditor.ViewToEdit = _listView;

        app.Run (_appWindow);
        _appWindow.Dispose ();

        return;

        void LogEvent (string message)
        {
            var msg = $"{message}";
            _eventList.Add (msg);
            _eventListView.MoveDown ();
        }
    }

    private void CustomRenderCB_Toggle (object? sender, ValueChangingEventArgs<CheckState> stateEventArgs)
    {
        if (stateEventArgs.NewValue != CheckState.Checked)
        {
            // Scenario.GetString automatically pads the name to the width of the longest name
            _listView?.SetSource (_scenarios);
        }
        else
        {
            _listView?.Source = new CustomRenderListDataSource (_scenarios!);
        }

        _appWindow?.SetNeedsDraw ();
    }

    private void ShowMarksCB_Toggle (object? sender, ValueChangingEventArgs<CheckState> stateEventArgs)
    {
        _listView?.ShowMarks = stateEventArgs.NewValue == CheckState.Checked;
        _appWindow?.SetNeedsDraw ();
    }

    private void MarkMultipleCB_Toggle (object? sender, ValueChangingEventArgs<CheckState> stateEventArgs)
    {
        _listView?.MarkMultiple = stateEventArgs.NewValue == CheckState.Checked;
        _appWindow?.SetNeedsDraw ();
    }

    private void ListView_RowRender (object? sender, ListViewRowEventArgs obj)

    {
        //if (_customRenderCb.Value == CheckState.Checked)
        //{
        //    // Only use the built-in RowRender event when we're not using custom rendering
        //    return;
        //}

        //if (obj.Row == _listView?.SelectedItem)
        //{
        //    return;
        //}

        //if (_listView?.ShowMarks && _listView?.Source!.IsMarked (obj.Row))
        //{
        //    obj.RowAttribute = _listView?.GetAttributeForRole (VisualRole.Highlight);

        //    return;
        //}

        //if (obj.Row % 2 == 0)
        //{
        //    obj.RowAttribute = _listView?.GetAttributeForRole (VisualRole.Active);
        //}
        //else
        //{
        //    obj.RowAttribute = _listView?.GetAttributeForRole (VisualRole.Normal);
        //}
    }

    // This is basically the same implementation used by the UICatalog main window
    internal class CustomRenderListDataSource : IListDataSource
    {
        private int _count;
        private BitArray? _marks;
        private ObservableCollection<Scenario>? _scenarios;
        public CustomRenderListDataSource (ObservableCollection<Scenario> itemList) => Scenarios = itemList;

        public ObservableCollection<Scenario>? Scenarios
        {
            get => _scenarios;
            set
            {
                if (value == null)
                {
                    return;
                }
                _count = value.Count;
                _marks = new BitArray (_count);
                _scenarios = value;
                _maxItemLength = GetMaxItemLength ();
            }
        }

#pragma warning disable CS0067
        /// <inheritdoc/>
        public event NotifyCollectionChangedEventHandler? CollectionChanged;
#pragma warning restore CS0067

        #region IListDataSource members

        bool IListDataSource.IsMarked (int item)
        {
            if (item >= 0 && item < _count)
            {
                return _marks? [item] ?? false;
            }

            return false;
        }

        int IListDataSource.Count => Scenarios?.Count ?? 0;

        private int _maxItemLength;

        int IListDataSource.MaxItemLength => _maxItemLength;

        public bool SuspendCollectionChangedEvent { get => throw new NotImplementedException (); set => throw new NotImplementedException (); }

        void IListDataSource.SetMark (int item, bool value)
        {
            if (item >= 0 && item < _count)
            {
                _marks? [item] = value;
            }
        }

        IList IListDataSource.ToList () => Scenarios!;

        void IListDataSource.Render (ListView listView, bool selected, int item, int col, int row, int width, int viewportX)
        {
            listView.Move (col - viewportX, row);

            Attribute rowAttribute;

            if (item % 2 == 0)
            {
                rowAttribute = listView.GetAttributeForRole (VisualRole.Active);
            }
            else
            {
                rowAttribute = listView.GetAttributeForRole (VisualRole.Normal);
            }

            if (item == listView.SelectedItem)
            {
                rowAttribute = listView.GetAttributeForRole (VisualRole.Focus);
            }

            var used = 0;

            used = RenderString (listView, used, width, rowAttribute, $"{Scenarios! [item].GetName ()}: ");

            used = RenderString (listView, used, width + viewportX, rowAttribute with { Style = TextStyle.Italic }, $"{Scenarios [item].GetDescription ()}");

            while (used < width + viewportX)
            {
                listView.AddRune ((Rune)' ');
                used++;
            }

            // Reset attributes to normal; otherwise checks will be rendered with the last used attribute
            listView.SetAttribute (listView.GetAttributeForRole (VisualRole.Normal));
        }

        private static int RenderString (ListView listView, int used, int remaining, Attribute attribute, string str)
        {
            var index = 0;
            listView.SetAttribute (attribute);

            while (index < str.Length)
            {
                (Rune rune, int size) = str.DecodeRune (index, index - str.Length);
                int count = rune.GetColumns ();

                if (used + count > remaining)
                {
                    break;
                }

                listView.AddRune (rune);
                used += count;
                index += size;
            }

            return used;
        }

        #endregion

        private int GetMaxItemLength ()
        {
            if (_scenarios?.Count == 0)
            {
                return 0;
            }

            var maxLength = 0;

            for (var i = 0; i < _scenarios?.Count; i++)
            {
                string sc = FormatRow (i);
                int l = sc.Length;

                if (l > maxLength)
                {
                    maxLength = l;
                }
            }

            return maxLength;
        }

        private string FormatRow (int item) => $"{Scenarios! [item].GetName ()}: {Scenarios! [item].GetDescription ()}";

        public void Dispose () => _scenarios = null;
    }
}
