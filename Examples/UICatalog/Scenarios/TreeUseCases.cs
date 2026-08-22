#nullable enable

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Tree View", "Demonstrates TreeView")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("TreeView")]
public partial class TreeUseCases : Scenario
{
    private CheckBox? _customArmyColorsCheckBox;
    private EventLog? _eventLog;
    private Runnable? _appWindow;
    private TreeView<GameObject>? _armyTree;
    private TreeViewEditor? _treeViewEditor;
    private ViewportSettingsEditor? _viewportSettingsEditor;

    public override void Main ()
    {

        using IApplication app = Application.Create ();
        app.Init ();

        _appWindow = new Runnable ();

        // MenuBar
        MenuBar menu = new ();

        menu.Add (new MenuBarItem (Strings.menuFile, [new MenuItem { Title = Strings.cmdQuit, Action = Quit }]));

        menu.Add (new MenuBarItem ("_Scenarios",
                                   [
                                       new MenuItem { Title = "_EnableForDesign", Action = LoadEnableForDesign },
                                       new MenuItem { Title = "_Rooms", Action = LoadRooms },
                                       new MenuItem { Title = "Armies With _Builder", Action = () => LoadArmies (false) },
                                       new MenuItem { Title = "Armies With _Delegate", Action = () => LoadArmies (true) }
                                   ]));

        _customArmyColorsCheckBox = new CheckBox
        {
            Title = "_Army Type Colors",
            Value = CheckState.Checked
        };
        _customArmyColorsCheckBox.ValueChanged += (_, _) => SetArmyTypeColors ();

        menu.Add (new MenuBarItem ("_Style", [new MenuItem { CommandView = _customArmyColorsCheckBox }]));

        // EventLog on the right
        _eventLog = new EventLog
        {
            X = Pos.AnchorEnd (),
            Y = 0,
            Width = Dim.Percent (25),
            Height = Dim.Fill (),
            Arrangement = ViewArrangement.LeftResizable,
            BorderStyle = LineStyle.Double
        };

        // TreeViewEditor above the ViewportSettingsEditor
        _treeViewEditor = new TreeViewEditor
        {
            Title = "TreeViewSettings",
            X = 0,
            Y = Pos.Bottom (menu),
            Width = Dim.Fill (_eventLog),
            Height = Dim.Auto (),
            CanFocus = true,
            AutoSelectViewToEdit = false,
            AutoSelectAdornments = false,
            Arrangement = ViewArrangement.Movable | ViewArrangement.Overlapped,
            BorderStyle = LineStyle.Single
        };

        // ViewportSettingsEditor at the bottom-left (below tree area)
        _viewportSettingsEditor = new ViewportSettingsEditor
        {
            Title = "ViewportSettings",
            X = 0,
            Y = Pos.AnchorEnd (),
            Width = Dim.Fill (_eventLog),
            Height = Dim.Auto (),
            CanFocus = true,
            AutoSelectViewToEdit = false,
            AutoSelectAdornments = false,
            Arrangement = ViewArrangement.Movable | ViewArrangement.Overlapped,
            BorderStyle = LineStyle.Single
        };

        _appWindow?.Add (menu, _treeViewEditor, _eventLog, _viewportSettingsEditor);

        _appWindow?.IsModalChanged += (_, args) =>
                                      {
                                          if (args.Value)
                                          {
                                              // Start with the most basic use case
                                              LoadEnableForDesign ();
                                          }
                                      };

        app.Run (_appWindow!);
        _appWindow?.Dispose ();
    }

    private View? CurrentTree
    {
        set
        {
            if (field == value)
            {
                return;
            }

            if (field is { })
            {
                field?.Dispose ();
                _appWindow?.Remove (field);
            }

            field = value;

            if (field is null)
            {
                return;
            }

            field.X = 0;
            field.Y = Pos.Bottom (_treeViewEditor!);
            field.Width = Dim.Fill (_eventLog!);
            field.Height = Dim.Fill (_viewportSettingsEditor!);
            field.BorderStyle = LineStyle.Single;
            field.Arrangement = ViewArrangement.Resizable | ViewArrangement.Movable;
            field.ViewportSettings |= ViewportSettingsFlags.HasScrollBars;
            field.SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Accent);

            _appWindow?.Add (field);

            // Gets added to end; move so that it's 2nd item (after menu)
            _appWindow?.MoveSubViewToStart (field);
            _appWindow?.MoveSubViewTowardsEnd (field);

            _eventLog!.ViewToLog = field;
            _treeViewEditor!.ViewToEdit = field;
            _viewportSettingsEditor!.ViewToEdit = field;

            field?.SetFocus ();
        }
    }

    private void LoadArmies (bool useDelegate)
    {
        Army army = CreateMiddleEarthArmy ();

        TreeView<GameObject> tree = new ();

        if (useDelegate)
        {
            tree.TreeBuilder = new DelegateTreeBuilder<GameObject> (o => o.GetChildren (), o => o.GetChildren ().Any ());
            tree.Title = "Armies With _Delegate";
        }
        else
        {
            tree.TreeBuilder = new GameObjectTreeBuilder ();
            tree.Title = "Armies With _Builder";
        }

        tree.AddObject (army);
        _armyTree = tree;
        SetArmyTypeColors ();

        CurrentTree = tree;
    }

    private void LoadRooms ()
    {
        _armyTree = null;

        House myHouse = new ()
        {
            Address = "23 Nowhere Street", Rooms = [new Room { Name = "Ballroom" }, new Room { Name = "Bedroom 1" }, new Room { Name = "Bedroom 2" }]
        };

        TreeView tree = new ();
        tree.Title = "_Rooms";

        tree.AddObject (myHouse);

        CurrentTree = tree;
    }

    private void LoadEnableForDesign ()
    {
        _armyTree = null;

        TreeView tree = new ();
        tree.EnableForDesign ();
        tree.Title = "_EnableForDesign";

        CurrentTree = tree;
    }

    private void Quit () => _appWindow?.RequestStop ();

    private void SetArmyTypeColors ()
    {
        if (_armyTree is null || _customArmyColorsCheckBox is null)
        {
            return;
        }

        if (_customArmyColorsCheckBox.Value != CheckState.Checked)
        {
            _armyTree.ColorGetter = null;
            _armyTree.SetNeedsDraw ();

            return;
        }

        _armyTree.ColorGetter = model => model switch
                                  {
                                      Army => CreateArmyTypeScheme (_armyTree, Color.BrightYellow),
                                      CorpsObject => CreateArmyTypeScheme (_armyTree, Color.BrightCyan),
                                      Division => CreateArmyTypeScheme (_armyTree, Color.BrightGreen),
                                      Brigade => CreateArmyTypeScheme (_armyTree, Color.BrightMagenta),
                                      Unit => CreateArmyTypeScheme (_armyTree, Color.Gray),
                                      _ => null
                                  };

        _armyTree.SetNeedsDraw ();
    }

    private static Scheme CreateArmyTypeScheme (TreeView<GameObject> tree, Color foreground) =>
        new ()
        {
            Normal = tree.GetAttributeForRole (VisualRole.Normal) with { Foreground = foreground },
            Focus = tree.GetAttributeForRole (VisualRole.Focus) with { Foreground = foreground },
            Active = tree.GetAttributeForRole (VisualRole.Active) with { Foreground = foreground }
        };

    // ── House / Room model (unchanged) ─────────────────────────────────────

    private class House : TreeNode
    {
        public string Address { get; set; } = string.Empty;

        public override IList<ITreeNode> Children => Rooms.Cast<ITreeNode> ().ToList ();
        public List<Room> Rooms { get; init; } = [];

        public override string Text { get => Address; set => Address = value; }
    }

    private class Room : TreeNode
    {
        public string Name { get; set; } = string.Empty;

        public override string Text { get => Name; set => Name = value; }
    }
}
