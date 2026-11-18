// Copilot

using JetBrains.Annotations;
using UnitTests;

namespace ViewsTests;

/// <summary>
///     Parallelizable tests for <see cref="TreeTableSource{T}"/>.
/// </summary>
[TestSubject (typeof (TreeTableSource<>))]
public class TreeTableSourceTests : TestDriverBase
{
    [Fact]
    public void CursorRight_ExpandsTreeNode_IncreaseRowCount ()
    {
        TableView tv = GetTreeTableView (out _);

        // Initially 2 root nodes visible.
        Assert.Equal (2, tv.Table?.Rows);

        // CursorRight expands the selected (first) root node.
        tv.NewKeyDownEvent (Key.CursorRight);

        // Lost Highway has 2 child cars, so total rows = 4.
        Assert.Equal (4, tv.Table?.Rows);
    }

    [Fact]
    public void CursorLeft_CollapsesExpandedNode_RestoresRowCount ()
    {
        TableView tv = GetTreeTableView (out _);

        // Expand the first root node.
        tv.NewKeyDownEvent (Key.CursorRight);
        Assert.Equal (4, tv.Table?.Rows);

        // CursorLeft collapses it.
        tv.NewKeyDownEvent (Key.CursorLeft);
        Assert.Equal (2, tv.Table?.Rows);
    }

    [Fact]
    public void MouseClick_OnExpandIndicator_ExpandsTreeNode ()
    {
        TableView tv = GetTreeTableView (out _);

        // Header occupies rows 0 and 1; first data row is at screen row 2.
        // Column 2 is the '+' expand indicator within the Name cell.
        tv.NewMouseEvent (new Mouse { Position = new Point (2, 2), Flags = MouseFlags.LeftButtonClicked });

        Assert.Equal (4, tv.Table?.Rows);

        // Clicking the same spot again collapses.
        tv.NewMouseEvent (new Mouse { Position = new Point (2, 2), Flags = MouseFlags.LeftButtonClicked });

        Assert.Equal (2, tv.Table?.Rows);
    }

    [Fact]
    public void Space_WithCheckBoxWrapper_TogglesCheckState ()
    {
        TableView tv = GetTreeTableView (out _);
        CheckBoxTableSourceWrapperByIndex checkSource = new (tv, tv.Table!);
        tv.Table = checkSource;

        // Initially no checked rows.
        Assert.Empty (checkSource.CheckedRows);

        // Press Space on the currently selected row (row 0 = "Lost Highway").
        tv.NewKeyDownEvent (Key.Space);

        Assert.Single (checkSource.CheckedRows);
        Assert.Contains (0, checkSource.CheckedRows);
    }

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private interface IDescribedThing
    {
        string Description { get; }
        string Name { get; }
    }

    private class Road : IDescribedThing
    {
        public List<Car> Traffic { get; set; } = [];
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    private class Car : IDescribedThing
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    private TableView GetTreeTableView (out TreeView<IDescribedThing> treeView)
    {
        IDriver driver = CreateTestDriver ();
        driver.Clip = new Region (driver.Screen);

        TableView tableView = new () { Driver = driver, X = 0, Y = 0 };
        tableView.SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Accent);
        tableView.Viewport = new Rectangle (0, 0, 40, 6);
        tableView.Style.ShowHorizontalHeaderUnderline = true;
        tableView.Style.ShowHorizontalHeaderOverline = false;
        tableView.Style.AlwaysShowHeaders = true;
        tableView.Style.SmoothHorizontalScrolling = true;

        treeView = new TreeView<IDescribedThing> ();
        treeView.AspectGetter = d => d.Name;
        treeView.TreeBuilder = new DelegateTreeBuilder<IDescribedThing> (d => d is Road r ? r.Traffic : Enumerable.Empty<IDescribedThing> (), o => true);

        treeView.AddObject (new Road
        {
            Name = "Lost Highway",
            Description = "Exciting night road",
            Traffic =
            [
                new Car { Name = "Ford Trans-Am", Description = "Talking thunderbird car" },
                new Car { Name = "DeLorean", Description = "Time travelling car" }
            ]
        });

        treeView.AddObject (new Road
        {
            Name = "Route 66",
            Description = "Great race course",
            Traffic =
            [
                new Car { Name = "Pink Compact", Description = "Penelope Pitstop's car" },
                new Car { Name = "Mean Machine", Description = "Dick Dastardly's car" }
            ]
        });

        tableView.Table = new TreeTableSource<IDescribedThing> (tableView,
                                                                "Name",
                                                                treeView,
                                                                new Dictionary<string, Func<IDescribedThing, object>>
                                                                {
                                                                    { "Description", d => d.Description }
                                                                });

        tableView.BeginInit ();
        tableView.EndInit ();
        tableView.LayoutSubViews ();

        return tableView;
    }
}
