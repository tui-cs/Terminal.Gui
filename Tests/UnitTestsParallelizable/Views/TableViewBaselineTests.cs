// Baseline tests for TableView. These lock in current correct behavior
// so that the upcoming redesign (Issue #5064) doesn't introduce silent regressions.
// All tests in this file MUST PASS against the current (pre-refactor) code.

using System.Data;
using JetBrains.Annotations;
using UnitTests;

// ReSharper disable PossibleMultipleEnumeration
#pragma warning disable xUnit2012

namespace ViewsTests;

[TestSubject (typeof (TableView))]
public class TableViewBaselineTests : TestDriverBase
{
    #region Helpers

    private static DataTableSource BuildTable (int cols, int rows) => BuildTable (cols, rows, out _);

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

    /// <summary>Creates a TableView with the given dimensions and data, fully initialized.</summary>
    private static TableView CreateTableView (int cols, int rows, int viewportWidth = 25, int viewportHeight = 5)
    {
        TableView tv = new () { Table = BuildTable (cols, rows), MultiSelect = true, Viewport = new Rectangle (0, 0, viewportWidth, viewportHeight) };
        tv.BeginInit ();
        tv.EndInit ();

        return tv;
    }

    #endregion

    #region A. Arrow Key Cell Movement

    [Fact]
    public void ArrowRight_MovesCursorRight ()
    {
        TableView tv = CreateTableView (5, 10);

        // Table setter puts us at (0,0)
        Assert.Equal (0, tv.Value!.SelectedCell.X);
        Assert.Equal (0, tv.Value!.SelectedCell.Y);

        tv.NewKeyDownEvent (Key.CursorRight);
        Assert.Equal (1, tv.Value!.SelectedCell.X);
        Assert.Equal (0, tv.Value!.SelectedCell.Y);
    }

    [Fact]
    public void ArrowDown_MovesCursorDown ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.NewKeyDownEvent (Key.CursorDown);
        Assert.Equal (0, tv.Value!.SelectedCell.X);
        Assert.Equal (1, tv.Value!.SelectedCell.Y);
    }

    [Fact]
    public void ArrowLeft_AtColumn0_DoesNotGoNegative ()
    {
        TableView tv = CreateTableView (5, 10);
        Assert.Equal (0, tv.Value!.SelectedCell.X);

        // Left at col 0 — should not go negative
        // HACK: Without Application/focus context, the command returns false
        // and doesn't transfer focus. The key assertion is column stays at 0.
        tv.NewKeyDownEvent (Key.CursorLeft);
        Assert.Equal (0, tv.Value!.SelectedCell.X);
    }

    [Fact]
    public void ArrowUp_AtRow0_DoesNotGoNegative ()
    {
        TableView tv = CreateTableView (5, 10);
        Assert.Equal (0, tv.Value!.SelectedCell.Y);

        tv.NewKeyDownEvent (Key.CursorUp);
        Assert.Equal (0, tv.Value!.SelectedCell.Y);
    }

    [Fact]
    public void ArrowRight_AtLastColumn_ClampsToLastColumn ()
    {
        TableView tv = CreateTableView (3, 5);
        tv.SetSelection (2, tv.Value?.SelectedCell.Y ?? 0, false); // last column (0-indexed)
        tv.NewKeyDownEvent (Key.CursorRight);
        Assert.Equal (2, tv.Value!.SelectedCell.X);
    }

    [Fact]
    public void ArrowDown_AtLastRow_ClampsToLastRow ()
    {
        TableView tv = CreateTableView (3, 5);
        tv.SetSelection (tv.Value?.SelectedCell.X ?? 0, 4, false); // last row (0-indexed)
        tv.NewKeyDownEvent (Key.CursorDown);
        Assert.Equal (4, tv.Value!.SelectedCell.Y);
    }

    [Fact]
    public void ArrowKeys_MultipleSteps_TraversesGrid ()
    {
        TableView tv = CreateTableView (5, 10);

        // Move to (2, 3)
        tv.NewKeyDownEvent (Key.CursorRight);
        tv.NewKeyDownEvent (Key.CursorRight);
        tv.NewKeyDownEvent (Key.CursorDown);
        tv.NewKeyDownEvent (Key.CursorDown);
        tv.NewKeyDownEvent (Key.CursorDown);

        Assert.Equal (2, tv.Value!.SelectedCell.X);
        Assert.Equal (3, tv.Value!.SelectedCell.Y);
    }

    #endregion

    #region B. Page/Home/End Navigation

    [Fact]
    public void PageDown_MovesByViewportHeight ()
    {
        TableView tv = CreateTableView (3, 50, viewportHeight: 10);
        Assert.Equal (0, tv.Value!.SelectedCell.Y);

        tv.PageDown (false, null);
        Assert.Equal (10, tv.Value!.SelectedCell.Y);
    }

    [Fact]
    public void PageUp_MovesByViewportHeight ()
    {
        TableView tv = CreateTableView (3, 50, viewportHeight: 10);
        tv.SetSelection (tv.Value?.SelectedCell.X ?? 0, 20, false);

        tv.PageUp (false, null);
        Assert.Equal (10, tv.Value!.SelectedCell.Y);
    }

    [Fact]
    public void PageDown_ClampsAtLastRow ()
    {
        TableView tv = CreateTableView (3, 5, viewportHeight: 10);
        Assert.Equal (0, tv.Value!.SelectedCell.Y);

        tv.PageDown (false, null);
        Assert.Equal (4, tv.Value!.SelectedCell.Y); // last row is 4 (0-indexed, 5 rows)
    }

    [Fact]
    public void PageUp_ClampsAtRow0 ()
    {
        TableView tv = CreateTableView (3, 50, viewportHeight: 10);
        tv.SetSelection (tv.Value?.SelectedCell.X ?? 0, 3, false);

        tv.PageUp (false, null);
        Assert.Equal (0, tv.Value!.SelectedCell.Y);
    }

    [Fact]
    public void Home_Key_MovesToStartOfRow ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SetSelection (3, tv.Value?.SelectedCell.Y ?? 0, false);

        tv.NewKeyDownEvent (Key.Home);
        Assert.Equal (0, tv.Value!.SelectedCell.X);
        Assert.Equal (0, tv.Value!.SelectedCell.Y); // row unchanged
    }

    [Fact]
    public void End_Key_MovesToEndOfRow ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SetSelection (1, tv.Value?.SelectedCell.Y ?? 0, false);

        tv.NewKeyDownEvent (Key.End);
        Assert.Equal (4, tv.Value!.SelectedCell.X); // last column (0-indexed, 5 cols)
        Assert.Equal (0, tv.Value!.SelectedCell.Y); // row unchanged
    }

    [Fact]
    public void MoveCursorToStartOfTable_MovesToOrigin ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SetSelection (3, 7, false);

        tv.MoveCursorToStartOfTable (false, null);
        Assert.Equal (0, tv.Value!.SelectedCell.X);
        Assert.Equal (0, tv.Value!.SelectedCell.Y);
    }

    [Fact]
    public void MoveCursorToEndOfTable_MovesToLastCell ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.MoveCursorToEndOfTable (false, null);
        Assert.Equal (4, tv.Value!.SelectedCell.X);
        Assert.Equal (9, tv.Value!.SelectedCell.Y);
    }

    [Fact]
    public void MoveCursorToEndOfTable_FullRowSelect_KeepsColumn ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.FullRowSelect = true;
        tv.SetSelection (2, tv.Value?.SelectedCell.Y ?? 0, false);

        tv.MoveCursorToEndOfTable (false, null);
        Assert.Equal (2, tv.Value!.SelectedCell.X); // column preserved with FullRowSelect
        Assert.Equal (9, tv.Value!.SelectedCell.Y);
    }

    [Fact]
    public void MoveCursorToStartOfRow_API ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SetSelection (3, 5, false);

        tv.MoveCursorToStartOfRow (false, null);
        Assert.Equal (0, tv.Value!.SelectedCell.X);
        Assert.Equal (5, tv.Value!.SelectedCell.Y); // row unchanged
    }

    [Fact]
    public void MoveCursorToEndOfRow_API ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SetSelection (1, 5, false);

        tv.MoveCursorToEndOfRow (false, null);
        Assert.Equal (4, tv.Value!.SelectedCell.X);
        Assert.Equal (5, tv.Value!.SelectedCell.Y);
    }

    #endregion

    #region C. Selection Changed Events

    [Fact]
    public void ArrowDown_FiresValueChanged ()
    {
        TableView tv = CreateTableView (5, 10);
        var fired = false;
        Point? oldCursor = null;
        Point? newCursor = null;

        tv.ValueChanged += (_, e) =>
                           {
                               fired = true;
                               oldCursor = e.OldValue?.SelectedCell;
                               newCursor = e.NewValue?.SelectedCell;
                           };

        tv.NewKeyDownEvent (Key.CursorDown);
        Assert.True (fired);
        Assert.Equal (new Point (0, 0), oldCursor);
        Assert.Equal (new Point (0, 1), newCursor);
    }

    [Fact]
    public void SetSelection_SameValue_DoesNotFireEvent ()
    {
        TableView tv = CreateTableView (5, 10);
        var fireCount = 0;
        tv.ValueChanged += (_, _) => fireCount++;

        // Setting to same value should not fire
        tv.SetSelection (0, 0, false);
        Assert.Equal (0, fireCount);
    }

    [Fact]
    public void SelectedColumn_Set_FiresValueChanged ()
    {
        TableView tv = CreateTableView (5, 10);
        var fired = false;
        tv.ValueChanged += (_, _) => fired = true;

        tv.SetSelection (2, 0, false);
        Assert.True (fired);
    }

    [Fact]
    public void SelectedRow_Set_FiresValueChanged ()
    {
        TableView tv = CreateTableView (5, 10);
        var fired = false;
        tv.ValueChanged += (_, _) => fired = true;

        tv.SetSelection (0, 3, false);
        Assert.True (fired);
    }

    #endregion

    #region D. Multi-Select Baseline

    [Fact]
    public void Toggle_AddsCurrentCellToMultiSelect ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SetSelection (1, 2, false);

        tv.InvokeCommand (Command.ToggleExtend);
        Assert.True (tv.IsSelected (1, 2));
        Assert.Single (tv.MultiSelectedRegions);
    }

    [Fact]
    public void Toggle_TwiceOnSameCell_RemovesFromMultiSelect ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SetSelection (1, 2, false);

        tv.InvokeCommand (Command.ToggleExtend);
        Assert.True (tv.MultiSelectedRegions.Any (r => r.IsExtended));

        tv.InvokeCommand (Command.ToggleExtend);

        // After toggling off, the toggled region should be removed
        Assert.DoesNotContain (tv.MultiSelectedRegions, r => r.IsExtended && r.Rectangle.Contains (1, 2));
    }

    [Fact]
    public void Toggle_MultiSelectFalse_SelectionUnchanged ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.MultiSelect = false;
        tv.SetSelection (1, 2, false);

        tv.InvokeCommand (Command.ToggleExtend);

        // With MultiSelect=false, toggle should not add regions
        Assert.Empty (tv.MultiSelectedRegions);
    }

    [Fact]
    public void Space_Key_TogglesSelection ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SetSelection (0, 0, false);

        tv.NewKeyDownEvent (Key.Space);
        Assert.True (tv.MultiSelectedRegions.Count > 0);
    }

    [Fact]
    public void SelectAll_SelectsEntireTable ()
    {
        TableView tv = CreateTableView (4, 4);
        tv.SelectAll ();
        Assert.Equal (16, tv.GetAllSelectedCells ().Count ());
    }

    [Fact]
    public void SelectAll_MultiSelectFalse_NoEffect ()
    {
        TableView tv = CreateTableView (4, 4);
        tv.MultiSelect = false;
        tv.SelectAll ();

        // Without multi-select, SelectAll is a no-op
        Assert.Empty (tv.MultiSelectedRegions);
    }

    [Fact]
    public void GetAllSelectedCells_NoCursorRegion_ReturnsCursorOnly ()
    {
        TableView tv = CreateTableView (5, 10);
        IEnumerable<Point> cells = tv.GetAllSelectedCells ();
        Assert.Single (cells);
        Assert.Contains (new Point (0, 0), cells);
    }

    [Fact]
    public void IsSelected_CursorCell_ReturnsTrue ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SetSelection (2, 3, false);
        Assert.True (tv.IsSelected (2, 3));
    }

    [Fact]
    public void IsSelected_NonCursorCell_ReturnsFalse ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SetSelection (0, 0, false);
        Assert.False (tv.IsSelected (1, 1));
    }

    [Fact]
    public void FullRowSelect_IsSelected_ReturnsTrueForEntireRow ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.FullRowSelect = true;
        tv.SetSelection (tv.Value?.SelectedCell.X ?? 0, 3, false);

        for (var col = 0; col < 5; col++)
        {
            Assert.True (tv.IsSelected (col, 3), $"Column {col} in selected row should be selected");
        }

        Assert.False (tv.IsSelected (0, 4), "Cell in non-selected row should not be selected");
    }

    [Fact]
    public void ExtendSelection_ShiftRight_CreatesRegion ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SetSelection (1, 1, false);

        tv.MoveCursorByOffset (1, 0, true, null);

        Assert.True (tv.IsSelected (1, 1), "Origin cell should be selected");
        Assert.True (tv.IsSelected (2, 1), "Extended cell should be selected");
        Assert.Equal (2, tv.Value!.SelectedCell.X);
    }

    [Fact]
    public void ExtendSelection_ShiftDown_CreatesRegion ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SetSelection (0, 0, false);

        tv.MoveCursorByOffset (0, 2, true, null);

        Assert.True (tv.IsSelected (0, 0));
        Assert.True (tv.IsSelected (0, 1));
        Assert.True (tv.IsSelected (0, 2));
        Assert.Equal (2, tv.Value!.SelectedCell.Y);
    }

    #endregion

    #region D2. Ctrl+Click Toggle (Mouse-based ToggleExtend)

    [Fact]
    public void CtrlClick_AddsRegionAtClickedCell () // Copilot
    {
        // Test that Ctrl+Click (UnionSelection path) adds a region at the clicked cell.
        TableView tv = CreateTableView (5, 10);
        tv.SetSelection (0, 0, false);
        tv.RefreshContentSize ();

        // Find what cell position (1, 3) maps to
        Point? cell = tv.ScreenToCell (1, 3);
        Assert.NotNull (cell);

        // Invoke ToggleExtend with a mouse binding context simulating Ctrl+Click
        MouseBinding mouseBinding = new ([Command.ToggleExtend], new Mouse { Position = new Point (1, 3), Flags = MouseFlags.LeftButtonClicked | MouseFlags.Ctrl });
        CommandContext ctx = new () { Command = Command.ToggleExtend, Source = new WeakReference<View> (tv), Binding = mouseBinding };
        tv.InvokeCommand (Command.ToggleExtend, ctx);

        Assert.True (tv.IsSelected (cell.Value.X, cell.Value.Y), "Ctrl+Click should select the clicked cell");
        Assert.True (tv.MultiSelectedRegions.Count > 0, "Ctrl+Click should add a region");
    }

    [Fact]
    public void CtrlClick_TwiceOnSameCell_RemovesRegion () // Copilot
    {
        // Bug: UnionSelection (Ctrl+Click path) always adds regions but never removes them.
        // Ctrl+Clicking the same cell twice should toggle it OFF (remove the region).
        TableView tv = CreateTableView (5, 10);
        tv.SetSelection (0, 0, false);
        tv.RefreshContentSize ();

        Point? cell = tv.ScreenToCell (1, 3);
        Assert.NotNull (cell);
        int clickedCol = cell.Value.X;
        int clickedRow = cell.Value.Y;

        // First Ctrl+Click — adds region
        MouseBinding mouseBinding1 = new ([Command.ToggleExtend], new Mouse { Position = new Point (1, 3), Flags = MouseFlags.LeftButtonClicked | MouseFlags.Ctrl });
        CommandContext ctx1 = new () { Command = Command.ToggleExtend, Source = new WeakReference<View> (tv), Binding = mouseBinding1 };
        tv.InvokeCommand (Command.ToggleExtend, ctx1);
        Assert.Contains (tv.MultiSelectedRegions, r => r.Rectangle.Contains (clickedCol, clickedRow));

        // Second Ctrl+Click on the same cell — should toggle OFF (remove the region)
        MouseBinding mouseBinding2 = new ([Command.ToggleExtend], new Mouse { Position = new Point (1, 3), Flags = MouseFlags.LeftButtonClicked | MouseFlags.Ctrl });
        CommandContext ctx2 = new () { Command = Command.ToggleExtend, Source = new WeakReference<View> (tv), Binding = mouseBinding2 };
        tv.InvokeCommand (Command.ToggleExtend, ctx2);

        // The region at the clicked cell should be removed (cursor may still be there, but no region)
        Assert.DoesNotContain (tv.MultiSelectedRegions, r => r.Rectangle.Contains (clickedCol, clickedRow));
    }

    #endregion

    #region E. Edge Cases

    [Fact]
    public void NullTable_ArrowKeysDoNotThrow ()
    {
        TableView tv = new () { Viewport = new Rectangle (0, 0, 25, 5) };
        tv.BeginInit ();
        tv.EndInit ();

        // Arrow keys are safe with null Table
        tv.NewKeyDownEvent (Key.CursorRight);
        tv.NewKeyDownEvent (Key.CursorDown);
        tv.NewKeyDownEvent (Key.CursorLeft);
        tv.NewKeyDownEvent (Key.CursorUp);
    }

    [Fact]
    public void NullTable_HomeEnd_DoesNotThrow ()
    {
        // Previously this threw NullReferenceException because MoveCursorToEndOfRow
        // used Table! without null check. Now fixed with null guard.
        TableView tv = new () { Viewport = new Rectangle (0, 0, 25, 5) };
        tv.BeginInit ();
        tv.EndInit ();

        tv.NewKeyDownEvent (Key.Home);
        tv.NewKeyDownEvent (Key.End);
    }

    [Fact]
    public void NullTable_SelectedColumnAndRow_AreDefaults ()
    {
        TableView tv = new ();
        Assert.Null (tv.Value);
    }

    [Fact]
    public void EmptyTable_NoRows_NavigationDoesNotThrow ()
    {
        DataTable dt = new ();
        dt.Columns.Add ("Col0");

        // 0 rows

        TableView tv = new () { Table = new DataTableSource (dt), Viewport = new Rectangle (0, 0, 25, 5) };
        tv.BeginInit ();
        tv.EndInit ();

        tv.NewKeyDownEvent (Key.CursorDown);
        tv.NewKeyDownEvent (Key.CursorRight);
    }

    [Fact]
    public void SingleCell_Table_BoundaryNavigation ()
    {
        TableView tv = CreateTableView (1, 1);
        Assert.Equal (0, tv.Value!.SelectedCell.X);
        Assert.Equal (0, tv.Value!.SelectedCell.Y);

        // Can't move anywhere
        tv.NewKeyDownEvent (Key.CursorRight);
        Assert.Equal (0, tv.Value!.SelectedCell.X);

        tv.NewKeyDownEvent (Key.CursorDown);
        Assert.Equal (0, tv.Value!.SelectedCell.Y);
    }

    [Fact]
    public void SelectedColumn_SetBeyondBounds_Clamped ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SetSelection (100, tv.Value?.SelectedCell.Y ?? 0, false);
        Assert.Equal (4, tv.Value!.SelectedCell.X); // clamped to last column
    }

    [Fact]
    public void SelectedRow_SetBeyondBounds_Clamped ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SetSelection (tv.Value?.SelectedCell.X ?? 0, 100, false);
        Assert.Equal (9, tv.Value!.SelectedCell.Y); // clamped to last row
    }

    [Fact]
    public void SelectedColumn_SetNegative_ClampedToZero ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SetSelection (-5, tv.Value?.SelectedCell.Y ?? 0, false);
        Assert.Equal (0, tv.Value!.SelectedCell.X);
    }

    [Fact]
    public void SelectedRow_SetNegative_ClampedToZero ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SetSelection (tv.Value?.SelectedCell.X ?? 0, -5, false);
        Assert.Equal (0, tv.Value!.SelectedCell.Y);
    }

    [Fact]
    public void SetTable_SetsSelectionToOrigin ()
    {
        TableView tv = new ();
        Assert.Null (tv.Value);

        tv.Table = BuildTable (5, 10);

        // Table setter calls SetSelection(0, 0, false)
        Assert.Equal (0, tv.Value!.SelectedCell.X);
        Assert.Equal (0, tv.Value!.SelectedCell.Y);
    }

    [Fact]
    public void SetTable_Null_AfterHavingData ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SetSelection (3, 7, false);

        tv.Table = null;

        // With null Table, Value becomes null and cursor resets to -1.
        Assert.Null (tv.Value);
    }

    [Fact]
    public void GetAllSelectedCells_EmptyTable_ReturnsEmpty ()
    {
        DataTable dt = new ();
        dt.Columns.Add ("Col0");

        // 0 rows

        TableView tv = new () { Table = new DataTableSource (dt), Viewport = new Rectangle (0, 0, 25, 5) };
        tv.BeginInit ();
        tv.EndInit ();

        IEnumerable<Point> cells = tv.GetAllSelectedCells ();
        Assert.Empty (cells);
    }

    #endregion

    #region F. IValue<TableSelection?> Baseline

    [Fact]
    public void Value_ReflectsCursorPosition ()
    {
        TableView tv = CreateTableView (5, 10);
        Assert.NotNull (tv.Value);
        Assert.Equal (new Point (0, 0), tv.Value!.SelectedCell);

        tv.SetSelection (2, tv.Value?.SelectedCell.Y ?? 0, false);
        Assert.Equal (new Point (2, 0), tv.Value!.SelectedCell);

        tv.SetSelection (tv.Value?.SelectedCell.X ?? 0, 3, false);
        Assert.Equal (new Point (2, 3), tv.Value!.SelectedCell);
    }

    [Fact]
    public void Value_UpdatedByNavigation ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.NewKeyDownEvent (Key.CursorRight);
        tv.NewKeyDownEvent (Key.CursorDown);
        Assert.NotNull (tv.Value);
        Assert.Equal (new Point (1, 1), tv.Value!.SelectedCell);
    }

    [Fact]
    public void Value_SetByTableSetter ()
    {
        TableView tv = new ();

        // Before Table is set, Value is null (no selection).
        Assert.Null (tv.Value);

        tv.Table = BuildTable (5, 10);
        Assert.NotNull (tv.Value);
        Assert.Equal (new Point (0, 0), tv.Value!.SelectedCell);
    }

    [Fact]
    public void ValueChanged_FiresOnNavigation ()
    {
        TableView tv = CreateTableView (5, 10);
        var fired = false;
        TableSelection? oldVal = null;
        TableSelection? newVal = null;

        tv.ValueChanged += (_, e) =>
                           {
                               fired = true;
                               oldVal = e.OldValue;
                               newVal = e.NewValue;
                           };

        tv.NewKeyDownEvent (Key.CursorDown);
        Assert.True (fired);
        Assert.NotNull (oldVal);
        Assert.Equal (new Point (0, 0), oldVal!.SelectedCell);
        Assert.NotNull (newVal);
        Assert.Equal (new Point (0, 1), newVal!.SelectedCell);
    }

    #endregion

    #region G. Accept / Accepted

    [Fact]
    public void Accept_Command_FiresAccepted ()
    {
        TableView tv = CreateTableView (5, 10);
        var fired = false;
        tv.Accepted += (_, _) => fired = true;

        tv.InvokeCommand (Command.Accept);
        Assert.True (fired);
    }

    [Fact]
    public void Enter_Key_FiresAccepted ()
    {
        TableView tv = CreateTableView (5, 10);
        var fired = false;
        tv.Accepted += (_, _) => fired = true;

        tv.NewKeyDownEvent (Key.Enter);
        Assert.True (fired);
    }

    #endregion

    #region H. EnsureCursorIsVisible

    [Fact]
    public void EnsureCursorIsVisible_NullTable_DoesNotThrow ()
    {
        TableView tv = new () { Viewport = new Rectangle (0, 0, 25, 5) };

        // Should not throw
        tv.EnsureCursorIsVisible ();
    }

    [Fact]
    public void EnsureCursorIsVisible_ScrollsRowIntoView ()
    {
        TableView tv = CreateTableView (3, 50, viewportHeight: 5);

        // Move to a row that is beyond viewport
        tv.SetSelection (tv.Value?.SelectedCell.X ?? 0, 20, false);
        tv.EnsureCursorIsVisible ();

        // After ensuring visibility, Viewport.Y should have adjusted
        // so that row 20 is visible (i.e., Viewport.Y <= 20 < Viewport.Y + Viewport.Height)
        Assert.True (tv.Viewport.Y <= 20, $"Viewport.Y ({tv.Viewport.Y}) should be <= 20");

        // HACK: The exact Viewport.Y depends on header height calculation.
        // We just assert the row is in the visible range.
        int visibleEnd = tv.Viewport.Y + tv.Viewport.Height - 1;
        Assert.True (visibleEnd >= 20, $"Visible end ({visibleEnd}) should be >= 20");
    }

    #endregion

    #region I. MoveCursorByOffset

    [Fact]
    public void MoveCursorByOffset_Positive_MovesRight ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.MoveCursorByOffset (2, 0, false, null);
        Assert.Equal (2, tv.Value!.SelectedCell.X);
        Assert.Equal (0, tv.Value!.SelectedCell.Y);
    }

    [Fact]
    public void MoveCursorByOffset_Negative_MovesLeft ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SetSelection (3, tv.Value?.SelectedCell.Y ?? 0, false);
        tv.MoveCursorByOffset (-2, 0, false, null);
        Assert.Equal (1, tv.Value!.SelectedCell.X);
    }

    [Fact]
    public void MoveCursorByOffset_Extend_CreatesMultiSelectRegion ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SetSelection (0, 0, false);

        tv.MoveCursorByOffset (2, 2, true, null);

        Assert.Equal (2, tv.Value!.SelectedCell.X);
        Assert.Equal (2, tv.Value!.SelectedCell.Y);
        Assert.True (tv.IsSelected (0, 0), "Origin should still be selected");
        Assert.True (tv.IsSelected (2, 2), "New position should be selected");
        Assert.True (tv.IsSelected (1, 1), "Cell in between should be selected");
    }

    [Fact]
    public void MoveCursorByOffset_ClampsAtBounds ()
    {
        TableView tv = CreateTableView (3, 5);
        tv.SetSelection (2, 4, false);

        tv.MoveCursorByOffset (5, 5, false, null);
        Assert.Equal (2, tv.Value!.SelectedCell.X); // clamped
        Assert.Equal (4, tv.Value!.SelectedCell.Y); // clamped
    }

    #endregion

    #region J. SetSelection

    [Fact]
    public void SetSelection_MovesToSpecifiedCell ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SetSelection (3, 7, false);

        Assert.Equal (3, tv.Value!.SelectedCell.X);
        Assert.Equal (7, tv.Value!.SelectedCell.Y);
    }

    [Fact]
    public void SetSelection_Extend_KeepsRegion ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SetSelection (1, 1, false);
        tv.SetSelection (3, 3, true);

        Assert.Equal (3, tv.Value!.SelectedCell.X);
        Assert.Equal (3, tv.Value!.SelectedCell.Y);
        Assert.True (tv.IsSelected (1, 1), "Origin of extend should be selected");
        Assert.True (tv.IsSelected (2, 2), "Interior cell should be selected");
        Assert.True (tv.IsSelected (3, 3), "End of extend should be selected");
    }

    [Fact]
    public void SetSelection_NoExtend_ClearsOldRegions ()
    {
        TableView tv = CreateTableView (5, 10);

        // Create a multi-select region
        tv.SetSelection (0, 0, false);
        tv.SetSelection (2, 2, true);
        Assert.True (tv.MultiSelectedRegions.Count > 0);

        // Non-extend set clears regions (except toggled ones)
        tv.SetSelection (4, 4, false);
        Assert.False (tv.IsSelected (0, 0), "Old origin should no longer be selected");
        Assert.False (tv.IsSelected (2, 2), "Old extent should no longer be selected");
        Assert.True (tv.IsSelected (4, 4), "New position should be selected");
    }

    #endregion
}
