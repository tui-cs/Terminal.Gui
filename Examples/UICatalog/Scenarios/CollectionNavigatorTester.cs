#nullable enable

using System.Collections.ObjectModel;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Collection Navigator", "Demonstrates keyboard navigation in ListView & TreeView (CollectionNavigator).")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("ListView")]
[ScenarioCategory ("TreeView")]
[ScenarioCategory ("Text and Formatting")]
[ScenarioCategory ("Mouse and Keyboard")]
public class CollectionNavigatorTester : Scenario
{
    private ObservableCollection<string> _items = new ([
                                                           "a",
                                                           "b",
                                                           "bb",
                                                           "c",
                                                           "ccc",
                                                           "ccc",
                                                           "cccc",
                                                           "ddd",
                                                           "dddd",
                                                           "dddd",
                                                           "ddddd",
                                                           "dddddd",
                                                           "ddddddd",
                                                           "this",
                                                           "this is a test",
                                                           "this was a test",
                                                           "this and",
                                                           "that and that",
                                                           "the",
                                                           "think",
                                                           "thunk",
                                                           "thunks",
                                                           "zip",
                                                           "zap",
                                                           "zoo",
                                                           "@jack",
                                                           "@sign",
                                                           "@at",
                                                           "@ateme",
                                                           "n@",
                                                           "n@brown",
                                                           ".net",
                                                           "$100.00",
                                                           "$101.00",
                                                           "$101.10",
                                                           "$101.11",
                                                           "$200.00",
                                                           "$210.99",
                                                           "$$",
                                                           "apricot",
                                                           "arm",
                                                           "丗丙业丞",
                                                           "丗丙丛",
                                                           "text",
                                                           "egg",
                                                           "candle",
                                                           " <- space",
                                                           "\t<- tab",
                                                           "\n<- newline",
                                                           "\r<- formfeed",
                                                           "q",
                                                           "quit",
                                                           "quitter"
                                                       ]);

    private Window? _top;
    private ListView? _listView;
    private TreeView? _treeView;
    private CheckBox? _allowMarkingCheckBox;
    private CheckBox? _allowMultiSelectionCheckBox;

    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();

        using Window top = new ();
        top.SchemeName = "Base";
        _top = top;

        // MenuBar
        MenuBar menu = new ();

        _allowMarkingCheckBox = new CheckBox { Title = "Allow _Marking" };

        _allowMarkingCheckBox.ValueChanged += (_, _) =>
                                              {
                                                  _listView?.ShowMarks = _allowMarkingCheckBox.Value == CheckState.Checked;

                                                  _allowMultiSelectionCheckBox?.Enabled = _allowMarkingCheckBox.Value == CheckState.Checked;
                                              };

        _allowMultiSelectionCheckBox = new CheckBox { Title = "Allow Multi _Selection", Enabled = false };

        _allowMultiSelectionCheckBox.ValueChanged += (_, _) => { _listView?.MarkMultiple = _allowMultiSelectionCheckBox.Value == CheckState.Checked; };

        menu.Add (new MenuBarItem ("_Configure",
                                   [
                                       new MenuItem { CommandView = _allowMarkingCheckBox },
                                       new MenuItem { CommandView = _allowMultiSelectionCheckBox },
                                       new MenuItem { Title = Strings.cmdQuit, Key = Application.GetDefaultKey (Command.Quit), Action = Quit }
                                   ]));

        menu.Add (new MenuBarItem (Strings.cmdQuit, [new MenuItem { Title = Strings.cmdQuit, Key = Application.GetDefaultKey (Command.Quit), Action = Quit }]));

        top.Add (menu);

        _items = new ObservableCollection<string> (_items.OrderBy (i => i, StringComparer.OrdinalIgnoreCase));

        CreateListView ();

        Line vsep = new () { Orientation = Orientation.Vertical, X = Pos.Right (_listView!), Y = 1, Height = Dim.Fill () };
        top.Add (vsep);
        CreateTreeView ();

        app.Run (top);
    }

    private void CreateListView ()
    {
        if (_top is null)
        {
            return;
        }

        Label label = new ()
        {
            Text = "ListView",
            TextAlignment = Alignment.Center,
            X = 0,
            Y = 1, // for menu
            Width = Dim.Percent (50),
            Height = 1
        };
        _top.Add (label);

        _listView = new ListView
        {
            X = 0,
            Y = Pos.Bottom (label),
            Width = Dim.Percent (50) - 1,
            Height = Dim.Fill (),
            ShowMarks = false,
            MarkMultiple = false
        };
        _top.Add (_listView);

        _listView.SetSource (_items);

        _listView.KeystrokeNavigator?.SearchStringChanged += (_, e) => { label.Text = $"ListView: {e.SearchString}"; };
    }

    private void CreateTreeView ()
    {
        if (_top is null || _listView is null)
        {
            return;
        }

        Label label = new ()
        {
            Text = "TreeView",
            TextAlignment = Alignment.Center,
            X = Pos.Right (_listView) + 2,
            Y = 1, // for menu
            Width = Dim.Percent (50),
            Height = 1
        };
        _top.Add (label);

        _treeView = new TreeView { X = Pos.Right (_listView) + 1, Y = Pos.Bottom (label), Width = Dim.Fill (), Height = Dim.Fill () };
        _treeView.Style.HighlightModelTextOnly = true;
        _top.Add (_treeView);

        TreeNode root = new () { Text = "IsLetterOrDigit examples", Children = _items.Where (i => char.IsLetterOrDigit (i [0])).Select (i => new TreeNode { Text = i }).Cast<ITreeNode> ().ToList () };

        _treeView.AddObject (root);
        root = new TreeNode { Text = "Non-IsLetterOrDigit examples", Children = _items.Where (i => !char.IsLetterOrDigit (i [0])).Select (i => new TreeNode { Text = i }).Cast<ITreeNode> ().ToList () };

        _treeView.AddObject (root);
        _treeView.ExpandAll ();
        _treeView.GoToFirst ();

        _treeView.KeystrokeNavigator.SearchStringChanged += (_, e) => { label.Text = $"TreeView: {e.SearchString}"; };
    }

    private void Quit () => _top?.RequestStop ();
}
