// Copilot

using System.Data;
using UnitTests;

namespace ViewsTests;

/// <summary>
///     Tests ported from the legacy UnitTests.Legacy/Views/TableViewTests.cs.
///     Covers unique behaviors not already present in TableViewTests.cs.
/// </summary>
public class TableViewLegacyTests : TestDriverBase
{
    private static DataTableSource BuildTable (int cols, int rows)
    {
        DataTable dt = new ();

        for (var c = 0; c < cols; c++)
        {
            dt.Columns.Add ("Col" + c);
        }

        for (var r = 0; r < rows; r++)
        {
            DataRow newRow = dt.NewRow ();

            for (var c = 0; c < cols; c++)
            {
                newRow [c] = $"R{r}C{c}";
            }

            dt.Rows.Add (newRow);
        }

        return new DataTableSource (dt);
    }

    [Fact]
    public void DeleteRow_SelectAll_AdjustsSelectionToPreventOverrun ()
    {
        TableView tableView = new () { Table = BuildTable (4, 4, out DataTable dt), MultiSelect = true, Viewport = new Rectangle (0, 0, 10, 5) };
        tableView.BeginInit ();
        tableView.EndInit ();

        tableView.SelectAll ();
        Assert.Equal (16, tableView.GetAllSelectedCells ().Count ());

        dt.Columns.RemoveAt (2);
        Assert.Equal (12, tableView.GetAllSelectedCells ().Count ());

        dt.Rows.RemoveAt (1);
        Assert.Equal (9, tableView.GetAllSelectedCells ().Count ());
    }

    [Fact]
    public void DeleteRow_SelectLastRow_AdjustsSelectionToPreventOverrun ()
    {
        TableView tableView = new () { Table = BuildTable (4, 4, out DataTable dt), MultiSelect = true, Viewport = new Rectangle (0, 0, 10, 5) };
        tableView.BeginInit ();
        tableView.EndInit ();

        tableView.MoveCursorToEndOfTable (false, null);
        tableView.MultiSelectedRegions.Clear ();
        tableView.MultiSelectedRegions.Push (new TableSelectionRegion (new Point (0, 3), new Rectangle (0, 3, 4, 1)));

        Assert.Equal (4, tableView.GetAllSelectedCells ().Count ());

        dt.Rows.RemoveAt (0);
        tableView.EnsureValidSelection ();

        Assert.Empty (tableView.MultiSelectedRegions);
    }

    [Fact]
    public void EnsureValidScrollOffsets_LoadSmallerTable ()
    {
        TableView tableView = new ();
        tableView.BeginInit ();
        tableView.EndInit ();
        tableView.Viewport = new Rectangle (0, 0, 25, 10);

        tableView.Table = BuildTable (25, 50);
        tableView.RowOffset = 20;
        tableView.ColumnOffset = 10;
        tableView.EnsureValidScrollOffsets ();

        Assert.Equal (20, tableView.RowOffset);
        Assert.Equal (10, tableView.ColumnOffset);

        tableView.Table = BuildTable (2, 2);

        Assert.Equal (0, tableView.RowOffset);
        Assert.Equal (0, tableView.ColumnOffset);
    }

    [Fact]
    public void EnsureValidScrollOffsets_WithNoCells ()
    {
        TableView tableView = new ();
        tableView.Table = new DataTableSource (new DataTable ());

        tableView.EnsureValidScrollOffsets ();

        Assert.Equal (0, tableView.RowOffset);
        Assert.Equal (0, tableView.ColumnOffset);
    }

    [Fact]
    public void GetAllSelectedCells_TwoIsolatedSelections_ReturnsSix ()
    {
        TableView tableView = new () { Table = BuildTable (20, 20), MultiSelect = true, Viewport = new Rectangle (0, 0, 10, 5) };
        tableView.BeginInit ();
        tableView.EndInit ();

        tableView.MultiSelectedRegions.Clear ();
        tableView.MultiSelectedRegions.Push (new TableSelectionRegion (new Point (1, 1), new Rectangle (1, 1, 2, 2)) { IsExtended = true });
        tableView.MultiSelectedRegions.Push (new TableSelectionRegion (new Point (7, 3), new Rectangle (7, 3, 2, 1)) { IsExtended = true });
        tableView.SetSelection (8, 3, false);

        Point [] selected = tableView.GetAllSelectedCells ().ToArray ();

        Assert.Equal (6, selected.Length);
        Assert.Equal (new Point (1, 1), selected [0]);
        Assert.Equal (new Point (2, 1), selected [1]);
        Assert.Equal (new Point (1, 2), selected [2]);
        Assert.Equal (new Point (2, 2), selected [3]);
        Assert.Equal (new Point (7, 3), selected [4]);
        Assert.Equal (new Point (8, 3), selected [5]);
    }

    [Fact]
    public void IsSelected_MultiSelectionOn_BoxSelection ()
    {
        TableView tableView = new () { Table = BuildTable (25, 50), MultiSelect = true };

        tableView.SetSelection (0, 0, false);
        tableView.SetSelection (1, 1, true);

        Assert.True (tableView.IsSelected (0, 0));
        Assert.True (tableView.IsSelected (1, 0));
        Assert.False (tableView.IsSelected (2, 0));
        Assert.True (tableView.IsSelected (0, 1));
        Assert.True (tableView.IsSelected (1, 1));
        Assert.False (tableView.IsSelected (2, 1));
        Assert.False (tableView.IsSelected (0, 2));
    }

    [Fact]
    public void ValueChanged_NotFiredForSameValue ()
    {
        TableView tableView = new () { Table = BuildTable (25, 50) };

        var called = false;
        tableView.ValueChanged += (_, _) => { called = true; };

        // Initial value is already at (0,0), setting same should not fire
        tableView.SetSelection (0, 0, false);
        Assert.False (called);

        tableView.SetSelection (10, 0, false);
        Assert.True (called);
    }

    [Fact]
    public void ValueChanged_CursorIndexesCorrect ()
    {
        TableView tableView = new () { Table = BuildTable (25, 50) };

        var called = false;

        tableView.ValueChanged += (_, e) =>
                                  {
                                      called = true;
                                      Assert.Equal (0, e.OldValue!.SelectedCell.X);
                                      Assert.Equal (10, e.NewValue!.SelectedCell.X);
                                  };

        tableView.SetSelection (10, 0, false);
        Assert.True (called);
    }

    [Fact]
    public void TestDataColumnCaption ()
    {
        DataTable dt = new ();
        dt.Columns.Add (new DataColumn { Caption = "Caption 1", ColumnName = "Column Name 1" });
        dt.Columns.Add (new DataColumn { ColumnName = "Column Name 2" });

        DataTableSource dts = new (dt);
        string [] cn = dts.ColumnNames;

        Assert.Equal ("Caption 1", cn [0]);
        Assert.Equal ("Column Name 2", cn [1]);
    }

    [Theory]
    [InlineData (true, 0, 1)]
    [InlineData (true, 1, 1)]
    [InlineData (false, 0, 1)]
    [InlineData (false, 1, 0)]
    public void TableCollectionNavigator_FullRowSelect_True_False (bool fullRowSelect, int selectedCol, int expectedRow)
    {
        TableView tableView = new () { FullRowSelect = fullRowSelect };
        tableView.BeginInit ();
        tableView.EndInit ();

        DataTable dt = new ();
        dt.Columns.Add ("A");
        dt.Columns.Add ("B");
        dt.Rows.Add (1, 2);
        dt.Rows.Add (3, 4);
        tableView.Table = new DataTableSource (dt);
        tableView.SetSelection (selectedCol, tableView.Value?.SelectedCell.Y ?? 0, false);

        Assert.Equal (expectedRow, tableView.CollectionNavigator!.GetNextMatchingItem (0, "3".ToCharArray () [0]));
    }

    [Fact]
    public void EnumerableTableSource_ColumnNamesAndRowCount ()
    {
        EnumerableTableSource<Type> source = new ([typeof (string), typeof (int), typeof (float)],
                                                  new Dictionary<string, Func<Type, object>> { { "Name", t => t.Name }, { "Namespace", t => t.Namespace! } });

        Assert.Equal (2, source.Columns);
        Assert.Equal (3, source.Rows);
        Assert.Equal ("Name", source.ColumnNames [0]);
        Assert.Equal ("Namespace", source.ColumnNames [1]);
        Assert.Equal ("String", source [0, 0]);
        Assert.Equal ("System", source [0, 1]);
    }

    [Fact]
    public void CheckBoxTableSourceWrapperByIndex_TogglesRow ()
    {
        DataTable dt = new ();
        dt.Columns.Add ("A");
        dt.Rows.Add (1);
        dt.Rows.Add (2);

        TableView tv = new () { Viewport = new Rectangle (0, 0, 20, 5) };
        tv.BeginInit ();
        tv.EndInit ();
        tv.Table = new DataTableSource (dt);

        CheckBoxTableSourceWrapperByIndex wrapper = new (tv, tv.Table);
        tv.Table = wrapper;

        Assert.Empty (wrapper.CheckedRows);

        tv.NewKeyDownEvent (Key.Space);
        Assert.Single (wrapper.CheckedRows, 0);

        tv.NewKeyDownEvent (Key.CursorDown);
        tv.NewKeyDownEvent (Key.Space);
        Assert.Contains (0, wrapper.CheckedRows);
        Assert.Contains (1, wrapper.CheckedRows);

        tv.NewKeyDownEvent (Key.CursorUp);
        tv.NewKeyDownEvent (Key.Space);
        Assert.Single (wrapper.CheckedRows, 1);
    }

    private static DataTableSource BuildTable (int cols, int rows, out DataTable dt)
    {
        dt = new DataTable ();

        for (var c = 0; c < cols; c++)
        {
            dt.Columns.Add ("Col" + c);
        }

        for (var r = 0; r < rows; r++)
        {
            DataRow newRow = dt.NewRow ();

            for (var c = 0; c < cols; c++)
            {
                newRow [c] = $"R{r}C{c}";
            }

            dt.Rows.Add (newRow);
        }

        return new DataTableSource (dt);
    }
}
