using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Terminal.Gui;
using Xunit;
using System.Globalization;
using Xunit.Abstractions;
using System.Reflection;
using Terminal.Gui.ViewTests;
using System.Collections;
using static Terminal.Gui.SpinnerStyle;

namespace Terminal.Gui.ViewsTests {

	public class TableViewTests {
		readonly ITestOutputHelper output;

		public TableViewTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		public void EnsureValidScrollOffsets_WithNoCells ()
		{
			var tableView = new TableView ();

			Assert.Equal (0, tableView.RowOffset);
			Assert.Equal (0, tableView.ColumnOffset);

			// Set empty table
			tableView.Table = new DataTableSource (new DataTable ());

			// Since table has no rows or columns scroll offset should default to 0
			tableView.EnsureValidScrollOffsets ();
			Assert.Equal (0, tableView.RowOffset);
			Assert.Equal (0, tableView.ColumnOffset);
		}


		[Fact]
		public void EnsureValidScrollOffsets_LoadSmallerTable ()
		{
			var tableView = new TableView ();
			tableView.BeginInit (); tableView.EndInit ();
			tableView.Bounds = new Rect (0, 0, 25, 10);

			Assert.Equal (0, tableView.RowOffset);
			Assert.Equal (0, tableView.ColumnOffset);

			// Set big table
			tableView.Table = BuildTable (25, 50);

			// Scroll down and along
			tableView.RowOffset = 20;
			tableView.ColumnOffset = 10;

			tableView.EnsureValidScrollOffsets ();

			// The scroll should be valid at the moment
			Assert.Equal (20, tableView.RowOffset);
			Assert.Equal (10, tableView.ColumnOffset);

			// Set small table
			tableView.Table = BuildTable (2, 2);

			// Setting a small table should automatically trigger fixing the scroll offsets to ensure valid cells
			Assert.Equal (0, tableView.RowOffset);
			Assert.Equal (0, tableView.ColumnOffset);

			// Trying to set invalid indexes should not be possible
			tableView.RowOffset = 20;
			tableView.ColumnOffset = 10;

			Assert.Equal (1, tableView.RowOffset);
			Assert.Equal (1, tableView.ColumnOffset);
		}

		[Fact]
		[AutoInitShutdown]
		public void Redraw_EmptyTable ()
		{
			var tableView = new TableView ();
			tableView.ColorScheme = new ColorScheme ();
			tableView.Bounds = new Rect (0, 0, 25, 10);

			// Set a table with 1 column
			tableView.Table = BuildTable (1, 50, out var dt);
			tableView.Draw ();

			dt.Columns.Remove (dt.Columns [0]);
			tableView.Draw ();
		}

		[Fact]
		public void SelectedCellChanged_NotFiredForSameValue ()
		{
			var tableView = new TableView () {
				Table = BuildTable (25, 50)
			};

			bool called = false;
			tableView.SelectedCellChanged += (s, e) => { called = true; };

			Assert.Equal (0, tableView.SelectedColumn);
			Assert.False (called);

			// Changing value to same as it already was should not raise an event
			tableView.SelectedColumn = 0;

			Assert.False (called);

			tableView.SelectedColumn = 10;
			Assert.True (called);
		}


		[Fact]
		public void SelectedCellChanged_SelectedColumnIndexesCorrect ()
		{
			var tableView = new TableView () {
				Table = BuildTable (25, 50)
			};

			bool called = false;
			tableView.SelectedCellChanged += (s, e) => {
				called = true;
				Assert.Equal (0, e.OldCol);
				Assert.Equal (10, e.NewCol);
			};

			tableView.SelectedColumn = 10;
			Assert.True (called);
		}

		[Fact]
		public void SelectedCellChanged_SelectedRowIndexesCorrect ()
		{
			var tableView = new TableView () {
				Table = BuildTable (25, 50)
			};

			bool called = false;
			tableView.SelectedCellChanged += (s, e) => {
				called = true;
				Assert.Equal (0, e.OldRow);
				Assert.Equal (10, e.NewRow);
			};

			tableView.SelectedRow = 10;
			Assert.True (called);
		}

		[Fact]
		public void Test_SumColumnWidth_UnicodeLength ()
		{
			Assert.Equal (11, "hello there".EnumerateRunes ().Sum (c => c.GetColumns ()));

			// Creates a string with the peculiar (french?) r symbol
			String surrogate = "Les Mise" + Char.ConvertFromUtf32 (Int32.Parse ("0301", NumberStyles.HexNumber)) + "rables";

			// The unicode width of this string is shorter than the string length! 
			Assert.Equal (14, surrogate.EnumerateRunes ().Sum (c => c.GetColumns ()));
			Assert.Equal (15, surrogate.Length);
		}

		[Fact]
		public void IsSelected_MultiSelectionOn_Vertical ()
		{
			var tableView = new TableView () {
				Table = BuildTable (25, 50),
				MultiSelect = true
			};

			// 3 cell vertical selection
			tableView.SetSelection (1, 1, false);
			tableView.SetSelection (1, 3, true);

			Assert.False (tableView.IsSelected (0, 0));
			Assert.False (tableView.IsSelected (1, 0));
			Assert.False (tableView.IsSelected (2, 0));

			Assert.False (tableView.IsSelected (0, 1));
			Assert.True (tableView.IsSelected (1, 1));
			Assert.False (tableView.IsSelected (2, 1));

			Assert.False (tableView.IsSelected (0, 2));
			Assert.True (tableView.IsSelected (1, 2));
			Assert.False (tableView.IsSelected (2, 2));

			Assert.False (tableView.IsSelected (0, 3));
			Assert.True (tableView.IsSelected (1, 3));
			Assert.False (tableView.IsSelected (2, 3));

			Assert.False (tableView.IsSelected (0, 4));
			Assert.False (tableView.IsSelected (1, 4));
			Assert.False (tableView.IsSelected (2, 4));
		}

		[Fact]
		public void IsSelected_MultiSelectionOn_Horizontal ()
		{
			var tableView = new TableView () {
				Table = BuildTable (25, 50),
				MultiSelect = true
			};

			// 2 cell horizontal selection
			tableView.SetSelection (1, 0, false);
			tableView.SetSelection (2, 0, true);

			Assert.False (tableView.IsSelected (0, 0));
			Assert.True (tableView.IsSelected (1, 0));
			Assert.True (tableView.IsSelected (2, 0));
			Assert.False (tableView.IsSelected (3, 0));

			Assert.False (tableView.IsSelected (0, 1));
			Assert.False (tableView.IsSelected (1, 1));
			Assert.False (tableView.IsSelected (2, 1));
			Assert.False (tableView.IsSelected (3, 1));
		}


		[Fact]
		public void IsSelected_MultiSelectionOn_BoxSelection ()
		{
			var tableView = new TableView () {
				Table = BuildTable (25, 50),
				MultiSelect = true
			};

			// 4 cell horizontal in box 2x2
			tableView.SetSelection (0, 0, false);
			tableView.SetSelection (1, 1, true);

			Assert.True (tableView.IsSelected (0, 0));
			Assert.True (tableView.IsSelected (1, 0));
			Assert.False (tableView.IsSelected (2, 0));

			Assert.True (tableView.IsSelected (0, 1));
			Assert.True (tableView.IsSelected (1, 1));
			Assert.False (tableView.IsSelected (2, 1));

			Assert.False (tableView.IsSelected (0, 2));
			Assert.False (tableView.IsSelected (1, 2));
			Assert.False (tableView.IsSelected (2, 2));
		}

		[AutoInitShutdown]
		[Fact]
		public void PageDown_ExcludesHeaders ()
		{
			var tableView = new TableView () {
				Table = BuildTable (25, 50),
				MultiSelect = true,
				Bounds = new Rect (0, 0, 10, 5)
			};

			// Header should take up 2 lines
			tableView.Style.ShowHorizontalHeaderOverline = false;
			tableView.Style.ShowHorizontalHeaderUnderline = true;
			tableView.Style.AlwaysShowHeaders = false;

			// ensure that TableView has the input focus
			Application.Top.Add (tableView);
			Application.Begin (Application.Top);

			Application.Top.FocusFirst ();
			Assert.True (tableView.HasFocus);

			Assert.Equal (0, tableView.RowOffset);

			tableView.ProcessKey (new KeyEvent (Key.PageDown, new KeyModifiers ()));

			// window height is 5 rows 2 are header so page down should give 3 new rows
			Assert.Equal (3, tableView.SelectedRow);
			Assert.Equal (1, tableView.RowOffset);

			// header is no longer visible so page down should give 5 new rows
			tableView.ProcessKey (new KeyEvent (Key.PageDown, new KeyModifiers ()));

			Assert.Equal (8, tableView.SelectedRow);
			Assert.Equal (4, tableView.RowOffset);
		}

		[Fact]
		public void DeleteRow_SelectAll_AdjustsSelectionToPreventOverrun ()
		{
			// create a 4 by 4 table
			var tableView = new TableView () {
				Table = BuildTable (4, 4, out var dt),
				MultiSelect = true,
				Bounds = new Rect (0, 0, 10, 5)
			};
			tableView.BeginInit (); tableView.EndInit ();

			tableView.SelectAll ();
			Assert.Equal (16, tableView.GetAllSelectedCells ().Count ());

			// delete one of the columns
			dt.Columns.RemoveAt (2);

			// table should now be 3x4
			Assert.Equal (12, tableView.GetAllSelectedCells ().Count ());

			// remove a row
			dt.Rows.RemoveAt (1);

			// table should now be 3x3
			Assert.Equal (9, tableView.GetAllSelectedCells ().Count ());
		}

		[Fact]
		public void DeleteRow_SelectLastRow_AdjustsSelectionToPreventOverrun ()
		{
			// create a 4 by 4 table
			var tableView = new TableView () {
				Table = BuildTable (4, 4, out var dt),
				MultiSelect = true,
				Bounds = new Rect (0, 0, 10, 5)
			};
			tableView.BeginInit (); tableView.EndInit ();

			tableView.ChangeSelectionToEndOfTable (false);

			// select the last row
			tableView.MultiSelectedRegions.Clear ();
			tableView.MultiSelectedRegions.Push (new TableSelection (new Point (0, 3), new Rect (0, 3, 4, 1)));

			Assert.Equal (4, tableView.GetAllSelectedCells ().Count ());

			// remove a row
			dt.Rows.RemoveAt (0);

			tableView.EnsureValidSelection ();

			// since the selection no longer exists it should be removed
			Assert.Empty (tableView.MultiSelectedRegions);
		}

		[Theory]
		[InlineData (true)]
		[InlineData (false)]
		public void GetAllSelectedCells_SingleCellSelected_ReturnsOne (bool multiSelect)
		{
			var tableView = new TableView () {
				Table = BuildTable (3, 3),
				MultiSelect = multiSelect,
				Bounds = new Rect (0, 0, 10, 5)
			};
			tableView.BeginInit (); tableView.EndInit ();

			tableView.SetSelection (1, 1, false);

			Assert.Single (tableView.GetAllSelectedCells ());
			Assert.Equal (new Point (1, 1), tableView.GetAllSelectedCells ().Single ());
		}

		[Fact]
		public void GetAllSelectedCells_SquareSelection_ReturnsFour ()
		{
			var tableView = new TableView () {
				Table = BuildTable (3, 3),
				MultiSelect = true,
				Bounds = new Rect (0, 0, 10, 5)
			};
			tableView.BeginInit (); tableView.EndInit ();

			// move cursor to 1,1
			tableView.SetSelection (1, 1, false);
			// spread selection across to 2,2 (e.g. shift+right then shift+down)
			tableView.SetSelection (2, 2, true);

			var selected = tableView.GetAllSelectedCells ().ToArray ();

			Assert.Equal (4, selected.Length);
			Assert.Equal (new Point (1, 1), selected [0]);
			Assert.Equal (new Point (2, 1), selected [1]);
			Assert.Equal (new Point (1, 2), selected [2]);
			Assert.Equal (new Point (2, 2), selected [3]);
		}

		[Fact]
		public void GetAllSelectedCells_SquareSelection_FullRowSelect ()
		{
			var tableView = new TableView () {
				Table = BuildTable (3, 3),
				MultiSelect = true,
				FullRowSelect = true,
				Bounds = new Rect (0, 0, 10, 5)
			};
			tableView.BeginInit (); tableView.EndInit ();

			// move cursor to 1,1
			tableView.SetSelection (1, 1, false);
			// spread selection across to 2,2 (e.g. shift+right then shift+down)
			tableView.SetSelection (2, 2, true);

			var selected = tableView.GetAllSelectedCells ().ToArray ();

			Assert.Equal (6, selected.Length);
			Assert.Equal (new Point (0, 1), selected [0]);
			Assert.Equal (new Point (1, 1), selected [1]);
			Assert.Equal (new Point (2, 1), selected [2]);
			Assert.Equal (new Point (0, 2), selected [3]);
			Assert.Equal (new Point (1, 2), selected [4]);
			Assert.Equal (new Point (2, 2), selected [5]);
		}

		[Fact]
		public void GetAllSelectedCells_TwoIsolatedSelections_ReturnsSix ()
		{
			var tableView = new TableView () {
				Table = BuildTable (20, 20),
				MultiSelect = true,
				Bounds = new Rect (0, 0, 10, 5)
			};
			tableView.BeginInit (); tableView.EndInit ();

			/*  
				Sets up disconnected selections like:

				00000000000
				01100000000
				01100000000
				00000001100
				00000000000
			*/

			tableView.MultiSelectedRegions.Clear ();
			tableView.MultiSelectedRegions.Push (new TableSelection (new Point (1, 1), new Rect (1, 1, 2, 2)));
			tableView.MultiSelectedRegions.Push (new TableSelection (new Point (7, 3), new Rect (7, 3, 2, 1)));

			tableView.SelectedColumn = 8;
			tableView.SelectedRow = 3;

			var selected = tableView.GetAllSelectedCells ().ToArray ();

			Assert.Equal (6, selected.Length);

			Assert.Equal (new Point (1, 1), selected [0]);
			Assert.Equal (new Point (2, 1), selected [1]);
			Assert.Equal (new Point (1, 2), selected [2]);
			Assert.Equal (new Point (2, 2), selected [3]);
			Assert.Equal (new Point (7, 3), selected [4]);
			Assert.Equal (new Point (8, 3), selected [5]);
		}

		[Fact, AutoInitShutdown]
		public void TableView_ShowHeadersFalse_AndNoHeaderLines ()
		{
			var tv = GetABCDEFTableView (out _);
			tv.Bounds = new Rect (0, 0, 5, 5);

			tv.Style.ShowHeaders = false;
			tv.Style.ShowHorizontalHeaderOverline = false;
			tv.Style.ShowHorizontalHeaderUnderline = false;

			tv.Draw ();

			string expected = @"
│1│2│
";
			TestHelpers.AssertDriverContentsAre (expected, output);
		}
		[Fact, AutoInitShutdown]
		public void TableView_ShowHeadersFalse_OverlineTrue ()
		{
			var tv = GetABCDEFTableView (out _);
			tv.Bounds = new Rect (0, 0, 5, 5);

			tv.Style.ShowHeaders = false;
			tv.Style.ShowHorizontalHeaderOverline = true;
			tv.Style.ShowHorizontalHeaderUnderline = false;

			tv.Draw ();

			string expected = @"
┌─┬─┐
│1│2│
";
			TestHelpers.AssertDriverContentsAre (expected, output);
		}
		[Fact, AutoInitShutdown]
		public void TableView_ShowHeadersFalse_UnderlineTrue ()
		{
			var tv = GetABCDEFTableView (out _);
			tv.Bounds = new Rect (0, 0, 5, 5);

			tv.Style.ShowHeaders = false;
			tv.Style.ShowHorizontalHeaderOverline = false;
			tv.Style.ShowHorizontalHeaderUnderline = true;
			// Horizontal scrolling option is part of the underline
			tv.Style.ShowHorizontalScrollIndicators = true;


			tv.Draw ();

			string expected = @"
├─┼─►
│1│2│
";
			TestHelpers.AssertDriverContentsAre (expected, output);
		}

		[Fact, AutoInitShutdown]
		public void TableView_ShowHeadersFalse_AllLines ()
		{
			var tv = GetABCDEFTableView (out _);
			tv.Bounds = new Rect (0, 0, 5, 5);

			tv.Style.ShowHeaders = false;
			tv.Style.ShowHorizontalHeaderOverline = true;
			tv.Style.ShowHorizontalHeaderUnderline = true;
			// Horizontal scrolling option is part of the underline
			tv.Style.ShowHorizontalScrollIndicators = true;


			tv.Draw ();

			string expected = @"
┌─┬─┐
├─┼─►
│1│2│
";
			TestHelpers.AssertDriverContentsAre (expected, output);
		}

		[Fact, AutoInitShutdown]
		public void TableView_ExpandLastColumn_True ()
		{
			var tv = SetUpMiniTable ();

			// the thing we are testing
			tv.Style.ExpandLastColumn = true;

			tv.Draw ();

			string expected = @"
┌─┬──────┐
│A│B     │
├─┼──────┤
│1│2     │
";
			TestHelpers.AssertDriverContentsAre (expected, output);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		[Fact, AutoInitShutdown]
		public void TableView_ExpandLastColumn_False ()
		{
			var tv = SetUpMiniTable ();

			// the thing we are testing
			tv.Style.ExpandLastColumn = false;

			tv.Draw ();

			string expected = @"
┌─┬─┬────┐
│A│B│    │
├─┼─┼────┤
│1│2│    │
";
			TestHelpers.AssertDriverContentsAre (expected, output);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		[Fact, AutoInitShutdown]
		public void TableView_ExpandLastColumn_False_ExactBounds ()
		{
			var tv = SetUpMiniTable ();

			// the thing we are testing
			tv.Style.ExpandLastColumn = false;
			// width exactly matches the max col widths
			tv.Bounds = new Rect (0, 0, 5, 4);

			tv.Draw ();

			string expected = @"
┌─┬─┐
│A│B│
├─┼─┤
│1│2│
";
			TestHelpers.AssertDriverContentsAre (expected, output);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		[Fact]
		[AutoInitShutdown]
		public void TableView_Activate ()
		{
			string activatedValue = null;
			var tv = new TableView (BuildTable (1, 1));
			tv.CellActivated += (s, c) => activatedValue = c.Table [c.Row, c.Col].ToString ();

			Application.Top.Add (tv);
			Application.Begin (Application.Top);

			// pressing enter should activate the first cell (selected cell)
			tv.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ()));
			Assert.Equal ("R0C0", activatedValue);

			// reset the test
			activatedValue = null;

			// clear keybindings and ensure that Enter does not trigger the event anymore
			tv.ClearKeyBindings ();
			tv.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ()));
			Assert.Null (activatedValue);

			// New method for changing the activation key
			tv.AddKeyBinding (Key.z, Command.Accept);
			tv.ProcessKey (new KeyEvent (Key.z, new KeyModifiers ()));
			Assert.Equal ("R0C0", activatedValue);

			// reset the test
			activatedValue = null;
			tv.ClearKeyBindings ();

			// Old method for changing the activation key
			tv.CellActivationKey = Key.z;
			tv.ProcessKey (new KeyEvent (Key.z, new KeyModifiers ()));
			Assert.Equal ("R0C0", activatedValue);
		}

		[Fact, AutoInitShutdown]
		public void TableViewMultiSelect_CannotFallOffLeft ()
		{
			var tv = SetUpMiniTable (out var dt);
			dt.Rows.Add (1, 2); // add another row (brings us to 2 rows)

			tv.MultiSelect = true;
			tv.SelectedColumn = 1;
			tv.SelectedRow = 1;
			tv.ProcessKey (new KeyEvent (Key.CursorLeft | Key.ShiftMask, new KeyModifiers { Shift = true }));

			Assert.Equal (new Rect (0, 1, 2, 1), tv.MultiSelectedRegions.Single ().Rect);

			// this next shift left should be ignored because we are already at the bounds
			tv.ProcessKey (new KeyEvent (Key.CursorLeft | Key.ShiftMask, new KeyModifiers { Shift = true }));

			Assert.Equal (new Rect (0, 1, 2, 1), tv.MultiSelectedRegions.Single ().Rect);

			Assert.Equal (0, tv.SelectedColumn);
			Assert.Equal (1, tv.SelectedRow);

			Application.Shutdown ();
		}
		[Fact, AutoInitShutdown]
		public void TableViewMultiSelect_CannotFallOffRight ()
		{
			var tv = SetUpMiniTable (out var dt);
			dt.Rows.Add (1, 2); // add another row (brings us to 2 rows)

			tv.MultiSelect = true;
			tv.SelectedColumn = 0;
			tv.SelectedRow = 1;
			tv.ProcessKey (new KeyEvent (Key.CursorRight | Key.ShiftMask, new KeyModifiers { Shift = true }));

			Assert.Equal (new Rect (0, 1, 2, 1), tv.MultiSelectedRegions.Single ().Rect);

			// this next shift right should be ignored because we are already at the right bounds
			tv.ProcessKey (new KeyEvent (Key.CursorRight | Key.ShiftMask, new KeyModifiers { Shift = true }));

			Assert.Equal (new Rect (0, 1, 2, 1), tv.MultiSelectedRegions.Single ().Rect);

			Assert.Equal (1, tv.SelectedColumn);
			Assert.Equal (1, tv.SelectedRow);

			Application.Shutdown ();
		}
		[Fact, AutoInitShutdown]
		public void TableViewMultiSelect_CannotFallOffBottom ()
		{
			var tv = SetUpMiniTable (out var dt);
			dt.Rows.Add (1, 2); // add another row (brings us to 2 rows)

			tv.MultiSelect = true;
			tv.SelectedColumn = 0;
			tv.SelectedRow = 0;
			tv.ProcessKey (new KeyEvent (Key.CursorRight | Key.ShiftMask, new KeyModifiers { Shift = true }));
			tv.ProcessKey (new KeyEvent (Key.CursorDown | Key.ShiftMask, new KeyModifiers { Shift = true }));

			Assert.Equal (new Rect (0, 0, 2, 2), tv.MultiSelectedRegions.Single ().Rect);

			// this next moves should be ignored because we already selected the whole table
			tv.ProcessKey (new KeyEvent (Key.CursorRight | Key.ShiftMask, new KeyModifiers { Shift = true }));
			tv.ProcessKey (new KeyEvent (Key.CursorDown | Key.ShiftMask, new KeyModifiers { Shift = true }));

			Assert.Equal (new Rect (0, 0, 2, 2), tv.MultiSelectedRegions.Single ().Rect);
			Assert.Equal (1, tv.SelectedColumn);
			Assert.Equal (1, tv.SelectedRow);

			Application.Shutdown ();
		}

		[Fact, AutoInitShutdown]
		public void TableViewMultiSelect_CannotFallOffTop ()
		{
			var tv = SetUpMiniTable (out var dt);
			dt.Rows.Add (1, 2); // add another row (brings us to 2 rows)
			tv.LayoutSubviews ();

			tv.MultiSelect = true;
			tv.SelectedColumn = 1;
			tv.SelectedRow = 1;
			tv.ProcessKey (new KeyEvent (Key.CursorLeft | Key.ShiftMask, new KeyModifiers { Shift = true }));
			tv.ProcessKey (new KeyEvent (Key.CursorUp | Key.ShiftMask, new KeyModifiers { Shift = true }));

			Assert.Equal (new Rect (0, 0, 2, 2), tv.MultiSelectedRegions.Single ().Rect);

			// this next moves should be ignored because we already selected the whole table
			tv.ProcessKey (new KeyEvent (Key.CursorLeft | Key.ShiftMask, new KeyModifiers { Shift = true }));
			tv.ProcessKey (new KeyEvent (Key.CursorUp | Key.ShiftMask, new KeyModifiers { Shift = true }));

			Assert.Equal (new Rect (0, 0, 2, 2), tv.MultiSelectedRegions.Single ().Rect);
			Assert.Equal (0, tv.SelectedColumn);
			Assert.Equal (0, tv.SelectedRow);

			Application.Shutdown ();
		}

		[Fact, AutoInitShutdown]
		public void TestShiftClick_MultiSelect_TwoRowTable_FullRowSelect ()
		{
			var tv = GetTwoRowSixColumnTable ();
			tv.LayoutSubviews ();

			tv.MultiSelect = true;

			// Clicking in bottom row
			tv.MouseEvent (new MouseEvent {
				X = 1,
				Y = 3,
				Flags = MouseFlags.Button1Clicked
			});

			// should select that row
			Assert.Equal (1, tv.SelectedRow);

			// shift clicking top row
			tv.MouseEvent (new MouseEvent {
				X = 1,
				Y = 2,
				Flags = MouseFlags.Button1Clicked | MouseFlags.ButtonShift
			});

			// should extend the selection
			Assert.Equal (0, tv.SelectedRow);

			var selected = tv.GetAllSelectedCells ().ToArray ();

			Assert.Contains (new Point (0, 0), selected);
			Assert.Contains (new Point (0, 1), selected);
		}

		[Fact, AutoInitShutdown]
		public void TestControlClick_MultiSelect_ThreeRowTable_FullRowSelect ()
		{
			var tv = GetTwoRowSixColumnTable (out var dt);
			dt.Rows.Add (1, 2, 3, 4, 5, 6);
			tv.LayoutSubviews ();

			tv.MultiSelect = true;

			// Clicking in bottom row
			tv.MouseEvent (new MouseEvent {
				X = 1,
				Y = 4,
				Flags = MouseFlags.Button1Clicked
			});

			// should select that row
			Assert.Equal (2, tv.SelectedRow);

			// shift clicking top row
			tv.MouseEvent (new MouseEvent {
				X = 1,
				Y = 2,
				Flags = MouseFlags.Button1Clicked | MouseFlags.ButtonCtrl
			});

			// should extend the selection
			// to include bottom and top row but not middle
			Assert.Equal (0, tv.SelectedRow);

			var selected = tv.GetAllSelectedCells ().ToArray ();

			Assert.Contains (new Point (0, 0), selected);
			Assert.DoesNotContain (new Point (0, 1), selected);
			Assert.Contains (new Point (0, 2), selected);
		}

		[Theory, AutoInitShutdown]
		[InlineData (false)]
		[InlineData (true)]
		public void TableView_ColorTests_FocusedOrNot (bool focused)
		{
			var tv = SetUpMiniTable ();
			tv.LayoutSubviews ();

			// width exactly matches the max col widths
			tv.Bounds = new Rect (0, 0, 5, 4);

			// private method for forcing the view to be focused/not focused
			var setFocusMethod = typeof (View).GetMethod ("SetHasFocus", BindingFlags.Instance | BindingFlags.NonPublic);

			// when the view is/isn't focused 
			setFocusMethod.Invoke (tv, new object [] { focused, tv, true });

			tv.Draw ();

			string expected = @"
┌─┬─┐
│A│B│
├─┼─┤
│1│2│
";
			TestHelpers.AssertDriverContentsAre (expected, output);

			string expectedColors = @"
00000
00000
00000
01000
";

			TestHelpers.AssertDriverColorsAre (expectedColors, new Attribute [] {
				// 0
				tv.ColorScheme.Normal,				
				// 1
				focused ? tv.ColorScheme.HotFocus : tv.ColorScheme.HotNormal});

			Application.Shutdown ();
		}

		[Theory, AutoInitShutdown]
		[InlineData (false)]
		[InlineData (true)]
		public void TableView_ColorTests_InvertSelectedCellFirstCharacter (bool focused)
		{
			var tv = SetUpMiniTable ();
			tv.Style.InvertSelectedCellFirstCharacter = true;
			tv.LayoutSubviews ();

			// width exactly matches the max col widths
			tv.Bounds = new Rect (0, 0, 5, 4);

			// private method for forcing the view to be focused/not focused
			var setFocusMethod = typeof (View).GetMethod ("SetHasFocus", BindingFlags.Instance | BindingFlags.NonPublic);

			// when the view is/isn't focused 
			setFocusMethod.Invoke (tv, new object [] { focused, tv, true });

			tv.Draw ();

			string expected = @"
┌─┬─┐
│A│B│
├─┼─┤
│1│2│
";
			TestHelpers.AssertDriverContentsAre (expected, output);

			string expectedColors = @"
00000
00000
00000
01000
";

			var invertHotFocus = new Attribute (tv.ColorScheme.HotFocus.Background, tv.ColorScheme.HotFocus.Foreground);
			var invertHotNormal = new Attribute (tv.ColorScheme.HotNormal.Background, tv.ColorScheme.HotNormal.Foreground);

			TestHelpers.AssertDriverColorsAre (expectedColors, new Attribute [] {
				// 0
				tv.ColorScheme.Normal,				
				// 1
				focused ?  invertHotFocus : invertHotNormal});

			Application.Shutdown ();
		}

		[Theory, AutoInitShutdown]
		[InlineData (false)]
		[InlineData (true)]
		public void TableView_ColorsTest_RowColorGetter (bool focused)
		{
			var tv = SetUpMiniTable (out DataTable dt);
			tv.LayoutSubviews ();

			// width exactly matches the max col widths
			tv.Bounds = new Rect (0, 0, 5, 4);

			var rowHighlight = new ColorScheme () {
				Normal = Attribute.Make (Color.BrightCyan, Color.DarkGray),
				HotNormal = Attribute.Make (Color.Green, Color.Blue),
				HotFocus = Attribute.Make (Color.BrightYellow, Color.White),
				Focus = Attribute.Make (Color.Cyan, Color.Magenta),
			};

			// when B is 2 use the custom highlight colour for the row
			tv.Style.RowColorGetter += (e) => Convert.ToInt32 (e.Table [e.RowIndex, 1]) == 2 ? rowHighlight : null;

			// private method for forcing the view to be focused/not focused
			var setFocusMethod = typeof (View).GetMethod ("SetHasFocus", BindingFlags.Instance | BindingFlags.NonPublic);

			// when the view is/isn't focused 
			setFocusMethod.Invoke (tv, new object [] { focused, tv, true });

			tv.Draw ();

			string expected = @"
┌─┬─┐
│A│B│
├─┼─┤
│1│2│
";
			TestHelpers.AssertDriverContentsAre (expected, output);

			string expectedColors = @"
00000
00000
00000
21222
";

			TestHelpers.AssertDriverColorsAre (expectedColors, new Attribute [] {
				// 0
				tv.ColorScheme.Normal,				
				// 1
				focused ? rowHighlight.HotFocus : rowHighlight.HotNormal,
				// 2
				rowHighlight.Normal});

			// change the value in the table so that
			// it no longer matches the RowColorGetter
			// delegate conditional ( which checks for
			// the value 2)
			dt.Rows [0] [1] = 5;

			tv.Draw ();
			expected = @"
┌─┬─┐
│A│B│
├─┼─┤
│1│5│
";
			TestHelpers.AssertDriverContentsAre (expected, output);

			expectedColors = @"
00000
00000
00000
01000
";

			// now we only see 2 colors used (the selected cell color and Normal
			// rowHighlight should no longer be used because the delegate returned null
			// (now that the cell value is 5 - which does not match the conditional)
			TestHelpers.AssertDriverColorsAre (expectedColors, new Attribute [] {
				// 0
				tv.ColorScheme.Normal,
				// 1
				focused ? tv.ColorScheme.HotFocus : tv.ColorScheme.HotNormal });

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		[Theory, AutoInitShutdown]
		[InlineData (false)]
		[InlineData (true)]
		public void TableView_ColorsTest_ColorGetter (bool focused)
		{
			var tv = SetUpMiniTable (out var dt);
			tv.LayoutSubviews ();

			// width exactly matches the max col widths
			tv.Bounds = new Rect (0, 0, 5, 4);

			// Create a style for column B
			var bStyle = tv.Style.GetOrCreateColumnStyle (1);

			// when B is 2 use the custom highlight colour
			var cellHighlight = new ColorScheme () {
				Normal = Attribute.Make (Color.BrightCyan, Color.DarkGray),
				HotNormal = Attribute.Make (Color.Green, Color.Blue),
				HotFocus = Attribute.Make (Color.BrightYellow, Color.White),
				Focus = Attribute.Make (Color.Cyan, Color.Magenta),
			};

			bStyle.ColorGetter = (a) => Convert.ToInt32 (a.CellValue) == 2 ? cellHighlight : null;

			// private method for forcing the view to be focused/not focused
			var setFocusMethod = typeof (View).GetMethod ("SetHasFocus", BindingFlags.Instance | BindingFlags.NonPublic);

			// when the view is/isn't focused 
			setFocusMethod.Invoke (tv, new object [] { focused, tv, true });

			tv.Draw ();

			string expected = @"
┌─┬─┐
│A│B│
├─┼─┤
│1│2│
";
			TestHelpers.AssertDriverContentsAre (expected, output);

			string expectedColors = @"
00000
00000
00000
01020
";

			TestHelpers.AssertDriverColorsAre (expectedColors, new Attribute [] {
				// 0
				tv.ColorScheme.Normal,				
				// 1
				focused ? tv.ColorScheme.HotFocus : tv.ColorScheme.HotNormal,
				// 2
				cellHighlight.Normal});

			// change the value in the table so that
			// it no longer matches the ColorGetter
			// delegate conditional ( which checks for
			// the value 2)
			dt.Rows [0] [1] = 5;

			tv.Draw ();
			expected = @"
┌─┬─┐
│A│B│
├─┼─┤
│1│5│
";
			TestHelpers.AssertDriverContentsAre (expected, output);

			expectedColors = @"
00000
00000
00000
01000
";

			// now we only see 2 colors used (the selected cell color and Normal
			// cellHighlight should no longer be used because the delegate returned null
			// (now that the cell value is 5 - which does not match the conditional)
			TestHelpers.AssertDriverColorsAre (expectedColors, new Attribute [] {
				// 0
				tv.ColorScheme.Normal,				
				// 1
				focused ? tv.ColorScheme.HotFocus : tv.ColorScheme.HotNormal });

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		private TableView SetUpMiniTable ()
		{
			return SetUpMiniTable (out _);
		}
		private TableView SetUpMiniTable (out DataTable dt)
		{
			var tv = new TableView ();
			tv.BeginInit (); tv.EndInit ();
			tv.Bounds = new Rect (0, 0, 10, 4);

			dt = new DataTable ();
			dt.Columns.Add ("A");
			dt.Columns.Add ("B");
			dt.Rows.Add (1, 2);

			tv.Table = new DataTableSource (dt);
			tv.Style.GetOrCreateColumnStyle (0).MinWidth = 1;
			tv.Style.GetOrCreateColumnStyle (0).MinWidth = 1;
			tv.Style.GetOrCreateColumnStyle (1).MaxWidth = 1;
			tv.Style.GetOrCreateColumnStyle (1).MaxWidth = 1;

			tv.ColorScheme = Colors.Base;
			return tv;
		}

		[Fact]
		[AutoInitShutdown]
		public void ScrollDown_OneLineAtATime ()
		{
			var tableView = new TableView ();
			tableView.BeginInit (); tableView.EndInit ();

			// Set big table
			tableView.Table = BuildTable (25, 50);

			// 1 header + 4 rows visible
			tableView.Bounds = new Rect (0, 0, 25, 5);
			tableView.Style.ShowHorizontalHeaderUnderline = false;
			tableView.Style.ShowHorizontalHeaderOverline = false;
			tableView.Style.AlwaysShowHeaders = true;

			// select last row
			tableView.SelectedRow = 3; // row is 0 indexed so this is the 4th visible row

			// Scroll down
			tableView.ProcessKey (new KeyEvent () { Key = Key.CursorDown });

			// Scrolled off the page by 1 row so it should only have moved down 1 line of RowOffset
			Assert.Equal (4, tableView.SelectedRow);
			Assert.Equal (1, tableView.RowOffset);
		}

		[Fact, AutoInitShutdown]
		public void ScrollRight_SmoothScrolling ()
		{

			var tableView = new TableView ();
			tableView.BeginInit (); tableView.EndInit ();

			tableView.ColorScheme = Colors.TopLevel;
			tableView.LayoutSubviews ();

			// 3 columns are visibile
			tableView.Bounds = new Rect (0, 0, 7, 5);
			tableView.Style.ShowHorizontalHeaderUnderline = false;
			tableView.Style.ShowHorizontalHeaderOverline = false;
			tableView.Style.AlwaysShowHeaders = true;
			tableView.Style.SmoothHorizontalScrolling = true;

			var dt = new DataTable ();
			dt.Columns.Add ("A");
			dt.Columns.Add ("B");
			dt.Columns.Add ("C");
			dt.Columns.Add ("D");
			dt.Columns.Add ("E");
			dt.Columns.Add ("F");

			dt.Rows.Add (1, 2, 3, 4, 5, 6);

			tableView.Table = new DataTableSource (dt);

			// select last visible column
			tableView.SelectedColumn = 2; // column C

			tableView.Draw ();

			string expected =
				@"
│A│B│C│
│1│2│3│";

			TestHelpers.AssertDriverContentsAre (expected, output);

			// Scroll right
			tableView.ProcessKey (new KeyEvent () { Key = Key.CursorRight });

			tableView.Draw ();

			// Note that with SmoothHorizontalScrolling only a single new column
			// is exposed when scrolling right.  This is not always the case though
			// sometimes if the leftmost column is long (i.e. A is a long column)
			// then when A is pushed off the screen multiple new columns could be exposed
			// (not just D but also E and F).  This is because TableView never shows
			// 'half cells' or scrolls by console unit (scrolling is done by table row/column increments).

			expected =
				@"
│B│C│D│
│2│3│4│";

			TestHelpers.AssertDriverContentsAre (expected, output);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		[Fact, AutoInitShutdown]
		public void ScrollRight_WithoutSmoothScrolling ()
		{
			var tableView = new TableView ();
			tableView.BeginInit (); tableView.EndInit ();
			tableView.ColorScheme = Colors.TopLevel;

			// 3 columns are visibile
			tableView.Bounds = new Rect (0, 0, 7, 5);
			tableView.Style.ShowHorizontalHeaderUnderline = false;
			tableView.Style.ShowHorizontalHeaderOverline = false;
			tableView.Style.AlwaysShowHeaders = true;
			tableView.Style.SmoothHorizontalScrolling = false;

			var dt = new DataTable ();
			dt.Columns.Add ("A");
			dt.Columns.Add ("B");
			dt.Columns.Add ("C");
			dt.Columns.Add ("D");
			dt.Columns.Add ("E");
			dt.Columns.Add ("F");

			dt.Rows.Add (1, 2, 3, 4, 5, 6);

			tableView.Table = new DataTableSource (dt);

			// select last visible column
			tableView.SelectedColumn = 2; // column C

			tableView.Draw ();

			string expected =
				@"
│A│B│C│
│1│2│3│";

			TestHelpers.AssertDriverContentsAre (expected, output);

			// Scroll right
			tableView.ProcessKey (new KeyEvent () { Key = Key.CursorRight });

			tableView.Draw ();

			// notice that without smooth scrolling we just update the first column
			// rendered in the table to the newly exposed column (D).  This is fast
			// since we don't have to worry about repeatedly measuring the content
			// area as we scroll until the new column (D) is exposed.  But it makes
			// the view 'jump' to expose all new columns

			expected =
				@"
│D│E│F│
│4│5│6│";

			TestHelpers.AssertDriverContentsAre (expected, output);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		private TableView GetABCDEFTableView (out DataTable dt)
		{
			var tableView = new TableView ();
			tableView.BeginInit (); tableView.EndInit ();

			tableView.ColorScheme = Colors.TopLevel;

			// 3 columns are visible
			tableView.Bounds = new Rect (0, 0, 7, 5);
			tableView.Style.ShowHorizontalHeaderUnderline = false;
			tableView.Style.ShowHorizontalHeaderOverline = false;
			tableView.Style.AlwaysShowHeaders = true;
			tableView.Style.SmoothHorizontalScrolling = false;

			dt = new DataTable ();
			dt.Columns.Add ("A");
			dt.Columns.Add ("B");
			dt.Columns.Add ("C");
			dt.Columns.Add ("D");
			dt.Columns.Add ("E");
			dt.Columns.Add ("F");

			dt.Rows.Add (1, 2, 3, 4, 5, 6);
			tableView.Table = new DataTableSource (dt);

			return tableView;
		}

		[Fact, AutoInitShutdown]
		public void TestColumnStyle_VisibleFalse_IsNotRendered ()
		{
			var tableView = GetABCDEFTableView (out _);

			tableView.Style.GetOrCreateColumnStyle (1).Visible = false;
			tableView.LayoutSubviews ();
			tableView.Draw ();

			string expected =
				@"
│A│C│D│
│1│3│4│";

			TestHelpers.AssertDriverContentsAre (expected, output);
		}

		[Fact, AutoInitShutdown]
		public void TestColumnStyle_FirstColumnVisibleFalse_IsNotRendered ()
		{
			var tableView = GetABCDEFTableView (out DataTable dt);

			tableView.Style.ShowHorizontalScrollIndicators = true;
			tableView.Style.ShowHorizontalHeaderUnderline = true;
			tableView.Style.GetOrCreateColumnStyle (0).Visible = false;

			tableView.LayoutSubviews ();
			tableView.Draw ();

			string expected =
				@"
│B│C│D│
├─┼─┼─►
│2│3│4│";

			TestHelpers.AssertDriverContentsAre (expected, output);
		}

		[Fact, AutoInitShutdown]
		public void TestColumnStyle_AllColumnsVisibleFalse_BehavesAsTableNull ()
		{
			var tableView = GetABCDEFTableView (out DataTable dt);

			for (int i = 0; i < 6; i++) {
				tableView.Style.GetOrCreateColumnStyle (i).Visible = false;
			}
			tableView.LayoutSubviews ();

			// expect nothing to be rendered when all columns are invisible
			string expected =
				@"
";

			tableView.Draw ();
			TestHelpers.AssertDriverContentsAre (expected, output);

			// expect behavior to match when Table is null
			tableView.Table = null;

			tableView.Draw ();
			TestHelpers.AssertDriverContentsAre (expected, output);
		}

		[Fact, AutoInitShutdown]
		public void TestColumnStyle_RemainingColumnsInvisible_NoScrollIndicator ()
		{
			var tableView = GetABCDEFTableView (out DataTable dt);

			tableView.Style.ShowHorizontalScrollIndicators = true;
			tableView.Style.ShowHorizontalHeaderUnderline = true;
			tableView.LayoutSubviews ();
			tableView.Draw ();

			// normally we should have scroll indicators because DEF are of screen
			string expected =
				@"
│A│B│C│
├─┼─┼─►
│1│2│3│";

			TestHelpers.AssertDriverContentsAre (expected, output);

			// but if DEF are invisible we shouldn't be showing the indicator
			tableView.Style.GetOrCreateColumnStyle (3).Visible = false;
			tableView.Style.GetOrCreateColumnStyle (4).Visible = false;
			tableView.Style.GetOrCreateColumnStyle (5).Visible = false;

			expected =
			       @"
│A│B│C│
├─┼─┼─┤
│1│2│3│";
			tableView.Draw ();
			TestHelpers.AssertDriverContentsAre (expected, output);
		}

		[Fact, AutoInitShutdown]
		public void TestColumnStyle_PreceedingColumnsInvisible_NoScrollIndicator ()
		{
			var tableView = GetABCDEFTableView (out DataTable dt);

			tableView.Style.ShowHorizontalScrollIndicators = true;
			tableView.Style.ShowHorizontalHeaderUnderline = true;

			tableView.ColumnOffset = 1;
			tableView.LayoutSubviews ();
			tableView.Draw ();

			// normally we should have scroll indicators because A,E and F are of screen
			string expected =
				@"
│B│C│D│
◄─┼─┼─►
│2│3│4│";

			TestHelpers.AssertDriverContentsAre (expected, output);

			// but if E and F are invisible so we shouldn't show right
			tableView.Style.GetOrCreateColumnStyle (4).Visible = false;
			tableView.Style.GetOrCreateColumnStyle (5).Visible = false;

			expected =
			       @"
│B│C│D│
◄─┼─┼─┤
│2│3│4│";
			tableView.Draw ();
			TestHelpers.AssertDriverContentsAre (expected, output);

			// now also A is invisible so we cannot scroll in either direction
			tableView.Style.GetOrCreateColumnStyle (0).Visible = false;

			expected =
			       @"
│B│C│D│
├─┼─┼─┤
│2│3│4│";
			tableView.Draw ();
			TestHelpers.AssertDriverContentsAre (expected, output);
		}
		[Fact, AutoInitShutdown]
		public void TestColumnStyle_VisibleFalse_CursorStepsOverInvisibleColumns ()
		{
			var tableView = GetABCDEFTableView (out var dt);
			tableView.LayoutSubviews ();

			tableView.Style.GetOrCreateColumnStyle (1).Visible = false;
			tableView.SelectedColumn = 0;

			tableView.ProcessKey (new KeyEvent { Key = Key.CursorRight });

			// Expect the cursor navigation to skip over the invisible column(s)
			Assert.Equal (2, tableView.SelectedColumn);

			tableView.ProcessKey (new KeyEvent { Key = Key.CursorLeft });

			// Expect the cursor navigation backwards to skip over invisible column too
			Assert.Equal (0, tableView.SelectedColumn);
		}

		[InlineData (true)]
		[InlineData (false)]
		[Theory, AutoInitShutdown]
		public void TestColumnStyle_FirstColumnVisibleFalse_CursorStaysAt1 (bool useHome)
		{
			var tableView = GetABCDEFTableView (out var dt);
			tableView.LayoutSubviews ();

			tableView.Style.GetOrCreateColumnStyle (0).Visible = false;
			tableView.SelectedColumn = 0;

			Assert.Equal (0, tableView.SelectedColumn);

			// column 0 is invisible so this method should move to 1
			tableView.EnsureValidSelection ();
			Assert.Equal (1, tableView.SelectedColumn);

			tableView.ProcessKey (new KeyEvent {
				Key = useHome ? Key.Home : Key.CursorLeft
			});

			// Expect the cursor to stay at 1
			Assert.Equal (1, tableView.SelectedColumn);
		}

		[InlineData (true)]
		[InlineData (false)]
		[Theory, AutoInitShutdown]
		public void TestMoveStartEnd_WithFullRowSelect (bool withFullRowSelect)
		{
			var tableView = GetTwoRowSixColumnTable ();
			tableView.LayoutSubviews ();
			tableView.FullRowSelect = withFullRowSelect;

			tableView.SelectedRow = 1;
			tableView.SelectedColumn = 1;

			tableView.ProcessKey (new KeyEvent {
				Key = Key.Home | Key.CtrlMask
			});

			if (withFullRowSelect) {
				// Should not be any horizontal movement when
				// using navigate to Start/End and FullRowSelect
				Assert.Equal (1, tableView.SelectedColumn);
				Assert.Equal (0, tableView.SelectedRow);
			} else {
				Assert.Equal (0, tableView.SelectedColumn);
				Assert.Equal (0, tableView.SelectedRow);
			}

			tableView.ProcessKey (new KeyEvent {
				Key = Key.End | Key.CtrlMask
			});

			if (withFullRowSelect) {
				Assert.Equal (1, tableView.SelectedColumn);
				Assert.Equal (1, tableView.SelectedRow);
			} else {
				Assert.Equal (5, tableView.SelectedColumn);
				Assert.Equal (1, tableView.SelectedRow);
			}

		}

		[InlineData (true)]
		[InlineData (false)]
		[Theory, AutoInitShutdown]
		public void TestColumnStyle_LastColumnVisibleFalse_CursorStaysAt2 (bool useEnd)
		{
			var tableView = GetABCDEFTableView (out var dt);
			tableView.LayoutSubviews ();

			// select D 
			tableView.SelectedColumn = 3;
			Assert.Equal (3, tableView.SelectedColumn);

			tableView.Style.GetOrCreateColumnStyle (3).Visible = false;
			tableView.Style.GetOrCreateColumnStyle (4).Visible = false;
			tableView.Style.GetOrCreateColumnStyle (5).Visible = false;

			// column D is invisible so this method should move to 2 (C)
			tableView.EnsureValidSelection ();
			Assert.Equal (2, tableView.SelectedColumn);

			tableView.ProcessKey (new KeyEvent {
				Key = useEnd ? Key.End : Key.CursorRight
			});

			// Expect the cursor to stay at 2
			Assert.Equal (2, tableView.SelectedColumn);
		}

		[Fact, AutoInitShutdown]
		public void TestColumnStyle_VisibleFalse_MultiSelected ()
		{
			var tableView = GetABCDEFTableView (out var dt);
			tableView.LayoutSubviews ();

			// user has rectangular selection 
			tableView.MultiSelectedRegions.Push (
				new TableSelection (
				new Point (0, 0),
				new Rect (0, 0, 3, 1))
				);

			Assert.Equal (3, tableView.GetAllSelectedCells ().Count ());
			Assert.True (tableView.IsSelected (0, 0));
			Assert.True (tableView.IsSelected (1, 0));
			Assert.True (tableView.IsSelected (2, 0));
			Assert.False (tableView.IsSelected (3, 0));

			// if middle column is invisible
			tableView.Style.GetOrCreateColumnStyle (1).Visible = false;

			// it should not be included in the selection
			Assert.Equal (2, tableView.GetAllSelectedCells ().Count ());
			Assert.True (tableView.IsSelected (0, 0));
			Assert.False (tableView.IsSelected (1, 0));
			Assert.True (tableView.IsSelected (2, 0));
			Assert.False (tableView.IsSelected (3, 0));

			Assert.DoesNotContain (new Point (1, 0), tableView.GetAllSelectedCells ());
		}

		[Fact, AutoInitShutdown]
		public void TestColumnStyle_VisibleFalse_MultiSelectingStepsOverInvisibleColumns ()
		{
			var tableView = GetABCDEFTableView (out _);
			tableView.LayoutSubviews ();

			// if middle column is invisible
			tableView.Style.GetOrCreateColumnStyle (1).Visible = false;

			tableView.ProcessKey (new KeyEvent { Key = Key.CursorRight | Key.ShiftMask });

			// Selection should extend from A to C but skip B
			Assert.Equal (2, tableView.GetAllSelectedCells ().Count ());
			Assert.True (tableView.IsSelected (0, 0));
			Assert.False (tableView.IsSelected (1, 0));
			Assert.True (tableView.IsSelected (2, 0));
			Assert.False (tableView.IsSelected (3, 0));

			Assert.DoesNotContain (new Point (1, 0), tableView.GetAllSelectedCells ());
		}

		[Fact, AutoInitShutdown]
		public void TestToggleCells_MultiSelectOn ()
		{
			// 2 row table
			var tableView = GetABCDEFTableView (out var dt);
			tableView.LayoutSubviews ();
			dt.Rows.Add (1, 2, 3, 4, 5, 6);

			tableView.MultiSelect = true;
			tableView.AddKeyBinding (Key.Space, Command.ToggleChecked);

			var selectedCell = tableView.GetAllSelectedCells ().Single ();
			Assert.Equal (0, selectedCell.X);
			Assert.Equal (0, selectedCell.Y);

			// Go Right
			tableView.ProcessKey (new KeyEvent { Key = Key.CursorRight });

			selectedCell = tableView.GetAllSelectedCells ().Single ();
			Assert.Equal (1, selectedCell.X);
			Assert.Equal (0, selectedCell.Y);

			// Toggle Select
			tableView.ProcessKey (new KeyEvent { Key = Key.Space });
			var m = tableView.MultiSelectedRegions.Single ();
			Assert.True (m.IsToggled);
			Assert.Equal (1, m.Origin.X);
			Assert.Equal (0, m.Origin.Y);
			selectedCell = tableView.GetAllSelectedCells ().Single ();
			Assert.Equal (1, selectedCell.X);
			Assert.Equal (0, selectedCell.Y);

			// Go Left
			tableView.ProcessKey (new KeyEvent { Key = Key.CursorLeft });

			// Both Toggled and Moved to should be selected
			Assert.Equal (2, tableView.GetAllSelectedCells ().Count ());
			var s1 = tableView.GetAllSelectedCells ().ElementAt (0);
			var s2 = tableView.GetAllSelectedCells ().ElementAt (1);
			Assert.Equal (1, s1.X);
			Assert.Equal (0, s1.Y);
			Assert.Equal (0, s2.X);
			Assert.Equal (0, s2.Y);

			// Go Down
			tableView.ProcessKey (new KeyEvent { Key = Key.CursorDown });

			// Both Toggled and Moved to should be selected but not 0,0
			// which we moved down from
			Assert.Equal (2, tableView.GetAllSelectedCells ().Count ());
			s1 = tableView.GetAllSelectedCells ().ElementAt (0);
			s2 = tableView.GetAllSelectedCells ().ElementAt (1);
			Assert.Equal (1, s1.X);
			Assert.Equal (0, s1.Y);
			Assert.Equal (0, s2.X);
			Assert.Equal (1, s2.Y);

			// Go back to the toggled cell
			tableView.ProcessKey (new KeyEvent { Key = Key.CursorRight });
			tableView.ProcessKey (new KeyEvent { Key = Key.CursorUp });

			// Toggle off 
			tableView.ProcessKey (new KeyEvent { Key = Key.Space });

			// Go Left
			tableView.ProcessKey (new KeyEvent { Key = Key.CursorLeft });

			selectedCell = tableView.GetAllSelectedCells ().Single ();
			Assert.Equal (0, selectedCell.X);
			Assert.Equal (0, selectedCell.Y);
		}

		[Fact, AutoInitShutdown]
		public void TestToggleCells_MultiSelectOn_FullRowSelect ()
		{
			// 2 row table
			var tableView = GetABCDEFTableView (out var dt);
			tableView.LayoutSubviews ();
			dt.Rows.Add (1, 2, 3, 4, 5, 6);
			tableView.FullRowSelect = true;
			tableView.MultiSelect = true;
			tableView.AddKeyBinding (Key.Space, Command.ToggleChecked);

			// Toggle Select Cell 0,0
			tableView.ProcessKey (new KeyEvent { Key = Key.Space });

			// Go Down
			tableView.ProcessKey (new KeyEvent { Key = Key.CursorDown });

			var m = tableView.MultiSelectedRegions.Single ();
			Assert.True (m.IsToggled);
			Assert.Equal (0, m.Origin.X);
			Assert.Equal (0, m.Origin.Y);

			//First row toggled and Second row active = 12 selected cells
			Assert.Equal (12, tableView.GetAllSelectedCells ().Count ());

			tableView.ProcessKey (new KeyEvent { Key = Key.CursorRight });
			tableView.ProcessKey (new KeyEvent { Key = Key.CursorUp });

			Assert.Single (tableView.MultiSelectedRegions.Where (r => r.IsToggled));

			// Can untoggle at 1,0 even though 0,0 was initial toggle because FullRowSelect is on
			tableView.ProcessKey (new KeyEvent { Key = Key.Space });

			Assert.Empty (tableView.MultiSelectedRegions.Where (r => r.IsToggled));

		}

		[Fact, AutoInitShutdown]
		public void TestToggleCells_MultiSelectOn_SquareSelectToggled ()
		{
			// 3 row table
			var tableView = GetABCDEFTableView (out var dt);
			tableView.LayoutSubviews ();
			dt.Rows.Add (1, 2, 3, 4, 5, 6);
			dt.Rows.Add (1, 2, 3, 4, 5, 6);
			tableView.MultiSelect = true;
			tableView.AddKeyBinding (Key.Space, Command.ToggleChecked);

			// Make a square selection
			tableView.ProcessKey (new KeyEvent { Key = Key.ShiftMask | Key.CursorDown });
			tableView.ProcessKey (new KeyEvent { Key = Key.ShiftMask | Key.CursorRight });

			Assert.Equal (4, tableView.GetAllSelectedCells ().Count ());

			// Toggle the square selected region on
			tableView.ProcessKey (new KeyEvent { Key = Key.Space });

			// Go Right
			tableView.ProcessKey (new KeyEvent { Key = Key.CursorRight });

			//Toggled on square + the active cell (x=2,y=1)
			Assert.Equal (5, tableView.GetAllSelectedCells ().Count ());
			Assert.Equal (2, tableView.SelectedColumn);
			Assert.Equal (1, tableView.SelectedRow);

			// Untoggle the rectangular region by hitting toggle in
			// any cell in that rect
			tableView.ProcessKey (new KeyEvent { Key = Key.CursorUp });
			tableView.ProcessKey (new KeyEvent { Key = Key.CursorLeft });

			Assert.Equal (4, tableView.GetAllSelectedCells ().Count ());
			tableView.ProcessKey (new KeyEvent { Key = Key.Space });
			Assert.Single (tableView.GetAllSelectedCells ());
		}

		[Fact, AutoInitShutdown]
		public void TestToggleCells_MultiSelectOn_Two_SquareSelects_BothToggled ()
		{
			// 6 row table
			var tableView = GetABCDEFTableView (out var dt);
			tableView.LayoutSubviews ();
			dt.Rows.Add (1, 2, 3, 4, 5, 6);
			dt.Rows.Add (1, 2, 3, 4, 5, 6);
			dt.Rows.Add (1, 2, 3, 4, 5, 6);
			dt.Rows.Add (1, 2, 3, 4, 5, 6);
			dt.Rows.Add (1, 2, 3, 4, 5, 6);
			tableView.MultiSelect = true;
			tableView.AddKeyBinding (Key.Space, Command.ToggleChecked);

			// Make first square selection (0,0 to 1,1)
			tableView.ProcessKey (new KeyEvent { Key = Key.ShiftMask | Key.CursorDown });
			tableView.ProcessKey (new KeyEvent { Key = Key.ShiftMask | Key.CursorRight });
			tableView.ProcessKey (new KeyEvent { Key = Key.Space });
			Assert.Equal (4, tableView.GetAllSelectedCells ().Count ());

			// Make second square selection leaving 1 unselected line between them
			tableView.ProcessKey (new KeyEvent { Key = Key.CursorLeft });
			tableView.ProcessKey (new KeyEvent { Key = Key.CursorDown });
			tableView.ProcessKey (new KeyEvent { Key = Key.CursorDown });
			tableView.ProcessKey (new KeyEvent { Key = Key.ShiftMask | Key.CursorDown });
			tableView.ProcessKey (new KeyEvent { Key = Key.ShiftMask | Key.CursorRight });

			// 2 square selections
			Assert.Equal (8, tableView.GetAllSelectedCells ().Count ());
		}

		[Theory, AutoInitShutdown]
		[InlineData (new object [] { true, true })]
		[InlineData (new object [] { false, true })]
		[InlineData (new object [] { true, false })]
		[InlineData (new object [] { false, false })]
		public void TestColumnStyle_VisibleFalse_DoesNotEffect_EnsureSelectedCellIsVisible (bool smooth, bool invisibleCol)
		{
			var tableView = GetABCDEFTableView (out var dt);
			tableView.LayoutSubviews ();
			tableView.Style.SmoothHorizontalScrolling = smooth;

			if (invisibleCol) {
				tableView.Style.GetOrCreateColumnStyle (3).Visible = false;
			}

			// New TableView should have first cell selected 
			Assert.Equal (0, tableView.SelectedColumn);
			// With no scrolling
			Assert.Equal (0, tableView.ColumnOffset);

			// A,B and C are visible on screen at the moment so these should have no effect
			tableView.SelectedColumn = 1;
			tableView.EnsureSelectedCellIsVisible ();
			Assert.Equal (0, tableView.ColumnOffset);

			tableView.SelectedColumn = 2;
			tableView.EnsureSelectedCellIsVisible ();
			Assert.Equal (0, tableView.ColumnOffset);

			// Selecting D should move the visible table area to fit D onto the screen
			tableView.SelectedColumn = 3;
			tableView.EnsureSelectedCellIsVisible ();
			Assert.Equal (smooth ? 1 : 3, tableView.ColumnOffset);
		}

		[Fact, AutoInitShutdown]
		public void LongColumnTest ()
		{
			var tableView = new TableView ();

			Application.Top.Add (tableView);
			Application.Begin (Application.Top);

			tableView.ColorScheme = Colors.TopLevel;

			// 25 characters can be printed into table
			tableView.Bounds = new Rect (0, 0, 25, 5);
			tableView.Style.ShowHorizontalHeaderUnderline = true;
			tableView.Style.ShowHorizontalHeaderOverline = false;
			tableView.Style.AlwaysShowHeaders = true;
			tableView.Style.SmoothHorizontalScrolling = true;

			var dt = new DataTable ();
			dt.Columns.Add ("A");
			dt.Columns.Add ("B");
			dt.Columns.Add ("Very Long Column");

			dt.Rows.Add (1, 2, new string ('a', 500));
			dt.Rows.Add (1, 2, "aaa");

			tableView.Table = new DataTableSource (dt);
			tableView.LayoutSubviews ();
			tableView.Draw ();

			// default behaviour of TableView is not to render
			// columns unless there is sufficient space
			string expected =
				@"
│A│B                    │
├─┼─────────────────────►
│1│2                    │
│1│2                    │
";

			TestHelpers.AssertDriverContentsAre (expected, output);

			// get a style for the long column
			var style = tableView.Style.GetOrCreateColumnStyle (2);

			// one way the API user can fix this for long columns
			// is to specify a MinAcceptableWidth for the column
			style.MaxWidth = 10;

			tableView.LayoutSubviews ();
			tableView.Draw ();
			expected =
				@"
│A│B│Very Long Column   │
├─┼─┼───────────────────┤
│1│2│aaaaaaaaaaaaaaaaaaa│
│1│2│aaa                │
";
			TestHelpers.AssertDriverContentsAre (expected, output);

			// revert the style change
			style.MaxWidth = TableView.DefaultMaxCellWidth;

			// another way API user can fix problem is to implement
			// RepresentationGetter and apply max length there

			style.RepresentationGetter = (s) => {
				return s.ToString ().Length < 15 ? s.ToString () : s.ToString ().Substring (0, 13) + "...";
			};

			tableView.LayoutSubviews ();
			tableView.Draw ();
			expected =
				@"
│A│B│Very Long Column   │
├─┼─┼───────────────────┤
│1│2│aaaaaaaaaaaaa...   │
│1│2│aaa                │
";
			TestHelpers.AssertDriverContentsAre (expected, output);

			// revert style change
			style.RepresentationGetter = null;

			// Both of the above methods rely on having a fixed
			// size limit for the column.  These are awkward if a
			// table is resizeable e.g. Dim.Fill().  Ideally we want
			// to render in any space available and truncate the content
			// of the column dynamically so it fills the free space at
			// the end of the table.

			// We can now specify that the column can be any length
			// (Up to MaxWidth) but the renderer can accept using
			// less space down to this limit
			style.MinAcceptableWidth = 5;

			tableView.LayoutSubviews ();
			tableView.Draw ();
			expected =
				@"
│A│B│Very Long Column   │
├─┼─┼───────────────────┤
│1│2│aaaaaaaaaaaaaaaaaaa│
│1│2│aaa                │
";
			TestHelpers.AssertDriverContentsAre (expected, output);

			// Now test making the width too small for the MinAcceptableWidth
			// the Column won't fit so should not be rendered
			var driver = ((FakeDriver)Application.Driver);
			driver.UpdateOffScreen ();


			tableView.Bounds = new Rect (0, 0, 9, 5);
			tableView.LayoutSubviews ();
			tableView.Draw ();
			expected =
@"
│A│B    │
├─┼─────►
│1│2    │
│1│2    │

";
			TestHelpers.AssertDriverContentsAre (expected, output);

			// setting width to 10 leaves just enough space for the column to
			// meet MinAcceptableWidth of 5.  Column width includes terminator line
			// symbol (e.g. ┤ or │)
			tableView.Bounds = new Rect (0, 0, 10, 5);
			tableView.LayoutSubviews ();
			tableView.Draw ();
			expected =
@"
│A│B│Very│
├─┼─┼────┤
│1│2│aaaa│
│1│2│aaa │
";
			TestHelpers.AssertDriverContentsAre (expected, output);

			tableView.Bounds = new Rect (0, 0, 25, 5);

			// revert style change
			style.MinAcceptableWidth = TableView.DefaultMinAcceptableWidth;

			// Now let's test the global MaxCellWidth and MinCellWidth
			tableView.Style.ExpandLastColumn = false;
			tableView.MaxCellWidth = 10;
			tableView.MinCellWidth = 3;

			tableView.LayoutSubviews ();
			tableView.Draw ();
			expected =
@"
│A  │B  │Very Long │    │
├───┼───┼──────────┼────┤
│1  │2  │aaaaaaaaaa│    │
│1  │2  │aaa       │    │
";
			TestHelpers.AssertDriverContentsAre (expected, output);

			// MaxCellWidth limits MinCellWidth
			tableView.MaxCellWidth = 5;
			tableView.MinCellWidth = 10;

			tableView.LayoutSubviews ();
			tableView.Draw ();
			expected =
@"
│A    │B    │Very │     │
├─────┼─────┼─────┼─────┤
│1    │2    │aaaaa│     │
│1    │2    │aaa  │     │
";
			TestHelpers.AssertDriverContentsAre (expected, output);

			Application.Shutdown ();
		}

		[Fact, AutoInitShutdown]
		public void ScrollIndicators ()
		{
			var tableView = new TableView ();
			tableView.BeginInit (); tableView.EndInit ();

			tableView.ColorScheme = Colors.TopLevel;

			// 3 columns are visibile
			tableView.Bounds = new Rect (0, 0, 7, 5);
			tableView.Style.ShowHorizontalHeaderUnderline = true;
			tableView.Style.ShowHorizontalHeaderOverline = false;
			tableView.Style.AlwaysShowHeaders = true;
			tableView.Style.SmoothHorizontalScrolling = true;

			var dt = new DataTable ();
			dt.Columns.Add ("A");
			dt.Columns.Add ("B");
			dt.Columns.Add ("C");
			dt.Columns.Add ("D");
			dt.Columns.Add ("E");
			dt.Columns.Add ("F");

			dt.Rows.Add (1, 2, 3, 4, 5, 6);

			tableView.Table = new DataTableSource (dt);

			// select last visible column
			tableView.SelectedColumn = 2; // column C

			tableView.Draw ();

			// user can only scroll right so sees right indicator
			// Because first column in table is A
			string expected =
				@"
│A│B│C│
├─┼─┼─►
│1│2│3│";

			TestHelpers.AssertDriverContentsAre (expected, output);

			// Scroll right
			tableView.ProcessKey (new KeyEvent () { Key = Key.CursorRight });

			// since A is now pushed off screen we get indicator showing
			// that user can scroll left to see first column
			tableView.Draw ();

			expected =
				@"
│B│C│D│
◄─┼─┼─►
│2│3│4│";

			TestHelpers.AssertDriverContentsAre (expected, output);

			// Scroll right twice more (to end of columns)
			tableView.ProcessKey (new KeyEvent () { Key = Key.CursorRight });
			tableView.ProcessKey (new KeyEvent () { Key = Key.CursorRight });

			tableView.Draw ();

			expected =
				@"
│D│E│F│
◄─┼─┼─┤
│4│5│6│";

			TestHelpers.AssertDriverContentsAre (expected, output);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		[Fact, AutoInitShutdown]
		public void CellEventsBackgroundFill ()
		{
			var tv = new TableView () {
				Width = 20,
				Height = 4
			};

			var dt = new DataTable ();
			dt.Columns.Add ("C1");
			dt.Columns.Add ("C2");
			dt.Columns.Add ("C3");

			dt.Rows.Add ("Hello", DBNull.Value, "f");

			tv.Table = new DataTableSource (dt);
			tv.NullSymbol = string.Empty;

			Application.Top.Add (tv);
			Application.Begin (Application.Top);

			tv.Draw ();

			var expected =
				@"
┌─────┬──┬─────────┐
│C1   │C2│C3       │
├─────┼──┼─────────┤
│Hello│  │f        │
";

			TestHelpers.AssertDriverContentsAre (expected, output);

			var color = new Attribute (Color.Magenta, Color.BrightBlue);

			var scheme = new ColorScheme {
				Normal = color,
				HotFocus = color,
				Focus = color,
				Disabled = color,
				HotNormal = color,
			};

			// Now the thing we really want to test is the styles!
			// All cells in the column have a column style that says
			// the cell is pink!
			for (int i = 0; i < dt.Columns.Count; i++) {

				var style = tv.Style.GetOrCreateColumnStyle (i);
				style.ColorGetter = (e) => {
					return scheme;
				};

			}

			tv.Draw ();
			expected =
							@"
00000000000000000000
00000000000000000000
00000000000000000000
01111101101111111110
";
			TestHelpers.AssertDriverColorsAre (expected, new Attribute [] { tv.ColorScheme.Normal, color });

		}


		[Fact, AutoInitShutdown]
		public void ShowHorizontalBottomLine_WithVerticalCellLines ()
		{
			var tableView = GetABCDEFTableView (out _);
			tableView.BeginInit (); tableView.EndInit ();

			tableView.ColorScheme = Colors.TopLevel;

			// 3 columns are visibile
			tableView.Bounds = new Rect (0, 0, 7, 5);
			tableView.Style.ShowHorizontalHeaderUnderline = true;
			tableView.Style.ShowHorizontalHeaderOverline = false;
			tableView.Style.AlwaysShowHeaders = true;
			tableView.Style.SmoothHorizontalScrolling = true;
			tableView.Style.ShowHorizontalBottomline = true;

			tableView.Draw ();

			// user can only scroll right so sees right indicator
			// Because first column in table is A
			string expected =
				@"
│A│B│C│
├─┼─┼─►
│1│2│3│
└─┴─┴─┘";

			TestHelpers.AssertDriverContentsAre (expected, output);
		}
		[Fact, AutoInitShutdown]
		public void ShowHorizontalBottomLine_NoCellLines ()
		{
			var tableView = GetABCDEFTableView (out _);
			tableView.BeginInit (); tableView.EndInit ();

			tableView.ColorScheme = Colors.TopLevel;

			// 3 columns are visibile
			tableView.Bounds = new Rect (0, 0, 7, 5);
			tableView.Style.ShowHorizontalHeaderUnderline = true;
			tableView.Style.ShowHorizontalHeaderOverline = false;
			tableView.Style.AlwaysShowHeaders = true;
			tableView.Style.SmoothHorizontalScrolling = true;
			tableView.Style.ShowHorizontalBottomline = true;
			tableView.Style.ShowVerticalCellLines = false;

			tableView.Draw ();

			// user can only scroll right so sees right indicator
			// Because first column in table is A
			string expected =
				@"
│A│B│C│
└─┴─┴─►
 1 2 3
───────";

			TestHelpers.AssertDriverContentsAre (expected, output);
		}

		[Fact, AutoInitShutdown]
		public void TestFullRowSelect_SelectionColorStopsAtTableEdge_WithCellLines ()
		{
			var tv = GetTwoRowSixColumnTable (out var dt);
			dt.Rows.Add (1, 2, 3, 4, 5, 6);

			tv.Bounds = new Rect (0, 0, 7, 6);
			tv.Frame = new Rect (0, 0, 7, 6);
			tv.LayoutSubviews ();


			tv.FullRowSelect = true;
			tv.Style.ShowHorizontalBottomline = true;

			// Clicking in bottom row
			tv.MouseEvent (new MouseEvent {
				X = 1,
				Y = 4,
				Flags = MouseFlags.Button1Clicked
			});

			// should select that row
			Assert.Equal (2, tv.SelectedRow);


			tv.OnDrawContent (tv.Bounds);

			string expected =
				@"
│A│B│C│
├─┼─┼─►
│1│2│3│
│1│2│3│
│1│2│3│
└─┴─┴─┘";

			TestHelpers.AssertDriverContentsAre (expected, output);

			var normal = tv.ColorScheme.Normal;
			var focus = tv.ColorScheme.HotFocus = new Attribute (Color.Magenta, Color.White);

			tv.Draw ();

			// HotFocus color (1) should be used for rendering the selected line
			// But should not spill into the borders.  Normal color (0) should be
			// used for the rest.
			expected =
				@"
0000000
0000000
0000000
0000000
0111110
0000000";

			TestHelpers.AssertDriverColorsAre (expected, normal, focus);
		}

		[Fact, AutoInitShutdown]
		public void TestFullRowSelect_AlwaysUseNormalColorForVerticalCellLines ()
		{
			var tv = GetTwoRowSixColumnTable (out var dt);
			dt.Rows.Add (1, 2, 3, 4, 5, 6);

			tv.Bounds = new Rect (0, 0, 7, 6);
			tv.Frame = new Rect (0, 0, 7, 6);
			tv.LayoutSubviews ();

			tv.FullRowSelect = true;
			tv.Style.ShowHorizontalBottomline = true;
			tv.Style.AlwaysUseNormalColorForVerticalCellLines = true;

			// Clicking in bottom row
			tv.MouseEvent (new MouseEvent {
				X = 1,
				Y = 4,
				Flags = MouseFlags.Button1Clicked
			});

			// should select that row
			Assert.Equal (2, tv.SelectedRow);


			tv.OnDrawContent (tv.Bounds);

			string expected =
				@"
│A│B│C│
├─┼─┼─►
│1│2│3│
│1│2│3│
│1│2│3│
└─┴─┴─┘";

			TestHelpers.AssertDriverContentsAre (expected, output);

			var normal = tv.ColorScheme.Normal;
			var focus = tv.ColorScheme.HotFocus = new Attribute (Color.Magenta, Color.White);

			tv.Draw ();

			// HotFocus color (1) should be used for cells only because
			// AlwaysUseNormalColorForVerticalCellLines is true
			expected =
				@"
0000000
0000000
0000000
0000000
0101010
0000000";

			TestHelpers.AssertDriverColorsAre (expected, normal, focus);
		}

		[Fact, AutoInitShutdown]
		public void TestTableViewCheckboxes_Simple()
		{

			var tv = GetTwoRowSixColumnTable (out var dt);
			dt.Rows.Add (1, 2, 3, 4, 5, 6);
			tv.LayoutSubviews ();

			var wrapper = new CheckBoxTableSourceWrapperByIndex (tv, tv.Table);
			tv.Table = wrapper;


			tv.Draw ();

			string expected =
				@"
│ │A│B│
├─┼─┼─►
│☐│1│2│
│☐│1│2│
│☐│1│2│";

			TestHelpers.AssertDriverContentsAre (expected, output);

			Assert.Empty (wrapper.CheckedRows);

			//toggle the top cell
			tv.ProcessKey (new KeyEvent (Key.Space, new KeyModifiers ()));

			Assert.Single (wrapper.CheckedRows, 0);

			tv.Draw();

			expected =
				@"
│ │A│B│
├─┼─┼─►
│☑│1│2│
│☐│1│2│
│☐│1│2│";

			TestHelpers.AssertDriverContentsAre (expected, output);

			tv.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ()));
			tv.ProcessKey (new KeyEvent (Key.Space, new KeyModifiers ()));


			Assert.Contains (0,wrapper.CheckedRows);
			Assert.Contains (1,wrapper.CheckedRows);
			Assert.Equal (2, wrapper.CheckedRows.Count);


			tv.Draw();

			expected =
				@"
│ │A│B│
├─┼─┼─►
│☑│1│2│
│☑│1│2│
│☐│1│2│";

			TestHelpers.AssertDriverContentsAre (expected, output);

			// untoggle top one
			tv.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ()));
			tv.ProcessKey (new KeyEvent (Key.Space, new KeyModifiers ()));

			Assert.Single (wrapper.CheckedRows, 1);

			tv.Draw();

			expected =
				@"
│ │A│B│
├─┼─┼─►
│☐│1│2│
│☑│1│2│
│☐│1│2│";

			TestHelpers.AssertDriverContentsAre (expected, output);
		}

		[Fact, AutoInitShutdown]
		public void TestTableViewCheckboxes_SelectAllToggle ()
		{

			var tv = GetTwoRowSixColumnTable (out var dt);
			dt.Rows.Add (1, 2, 3, 4, 5, 6);
			tv.LayoutSubviews ();

			var wrapper = new CheckBoxTableSourceWrapperByIndex (tv, tv.Table);
			tv.Table = wrapper;

			//toggle all cells
			tv.ProcessKey (new KeyEvent (Key.A | Key.CtrlMask, new KeyModifiers { Ctrl = true }));
			tv.ProcessKey (new KeyEvent (Key.Space, new KeyModifiers ()));

			tv.Draw();

			string expected =
				@"
│ │A│B│
├─┼─┼─►
│☑│1│2│
│☑│1│2│
│☑│1│2│";

			TestHelpers.AssertDriverContentsAre (expected, output);
			Assert.Contains (0, wrapper.CheckedRows);
			Assert.Contains (1, wrapper.CheckedRows);
			Assert.Contains (2, wrapper.CheckedRows);
			Assert.Equal (3, wrapper.CheckedRows.Count);

			// Untoggle all again
			tv.ProcessKey (new KeyEvent (Key.Space, new KeyModifiers ()));

			tv.Draw();

			expected =
				@"
│ │A│B│
├─┼─┼─►
│☐│1│2│
│☐│1│2│
│☐│1│2│";

			TestHelpers.AssertDriverContentsAre (expected, output);

			Assert.Empty (wrapper.CheckedRows);
		}

		[Fact, AutoInitShutdown]
		public void TestTableViewCheckboxes_MultiSelectIsUnion_WhenToggling ()
		{
			var tv = GetTwoRowSixColumnTable (out var dt);
			dt.Rows.Add (1, 2, 3, 4, 5, 6);
			tv.LayoutSubviews ();

			var wrapper = new CheckBoxTableSourceWrapperByIndex (tv, tv.Table);
			tv.Table = wrapper;
			wrapper.CheckedRows.Add (0);
			wrapper.CheckedRows.Add (2);

			tv.Draw();

			string expected =
				@"
│ │A│B│
├─┼─┼─►
│☑│1│2│
│☐│1│2│
│☑│1│2│";
			//toggle top two at once
			tv.ProcessKey (new KeyEvent (Key.CursorDown | Key.ShiftMask, new KeyModifiers { Shift = true }));
			Assert.True (tv.IsSelected (0, 0));
			Assert.True (tv.IsSelected (0, 1));
			tv.ProcessKey (new KeyEvent (Key.Space, new KeyModifiers ()));

			// Because at least 1 of the rows is not yet ticked we toggle them all to ticked
			TestHelpers.AssertDriverContentsAre (expected, output);
			Assert.Contains (0, wrapper.CheckedRows);
			Assert.Contains (1, wrapper.CheckedRows);
			Assert.Contains (2, wrapper.CheckedRows);
			Assert.Equal (3, wrapper.CheckedRows.Count);

			tv.Draw();

			expected =
				@"
│ │A│B│
├─┼─┼─►
│☑│1│2│
│☑│1│2│
│☑│1│2│";

			TestHelpers.AssertDriverContentsAre (expected, output);

			// Untoggle the top 2
			tv.ProcessKey (new KeyEvent (Key.Space, new KeyModifiers ()));

			tv.Draw();

			expected =
				@"
│ │A│B│
├─┼─┼─►
│☐│1│2│
│☐│1│2│
│☑│1│2│";
			TestHelpers.AssertDriverContentsAre (expected, output);
			Assert.Single (wrapper.CheckedRows, 2);
		}


		[Fact, AutoInitShutdown]
		public void TestTableViewCheckboxes_ByObject ()
		{
			var tv = GetPetTable (out var source);
			tv.LayoutSubviews ();
			var pets = source.Data;

			var wrapper = new CheckBoxTableSourceWrapperByObject<PickablePet>(
				tv,
				source,
				(p)=>p.IsPicked,
				(p,b)=>p.IsPicked = b);

			tv.Table = wrapper;

			tv.Draw();

			string expected =
				@"
┌─┬───────┬─────────────┐
│ │Name   │Kind         │
├─┼───────┼─────────────┤
│☐│Tammy  │Cat          │
│☐│Tibbles│Cat          │
│☐│Ripper │Dog          │";

			TestHelpers.AssertDriverContentsAre (expected, output);

			Assert.Empty (pets.Where(p=>p.IsPicked));

			tv.ProcessKey (new KeyEvent (Key.Space, new KeyModifiers ()));
			
			Assert.True (pets.First ().IsPicked);

			tv.Draw();

			expected =
				@"
┌─┬───────┬─────────────┐
│ │Name   │Kind         │
├─┼───────┼─────────────┤
│☑│Tammy  │Cat          │
│☐│Tibbles│Cat          │
│☐│Ripper │Dog          │";

			TestHelpers.AssertDriverContentsAre (expected, output);


			tv.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ()));
			tv.ProcessKey (new KeyEvent (Key.Space, new KeyModifiers ()));

			Assert.True (pets.ElementAt(0).IsPicked);
			Assert.True (pets.ElementAt (1).IsPicked);
			Assert.False (pets.ElementAt (2).IsPicked);

			tv.Draw();

			expected =
				@"
┌─┬───────┬─────────────┐
│ │Name   │Kind         │
├─┼───────┼─────────────┤
│☑│Tammy  │Cat          │
│☑│Tibbles│Cat          │
│☐│Ripper │Dog          │";

			TestHelpers.AssertDriverContentsAre (expected, output);


			tv.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ()));
			tv.ProcessKey (new KeyEvent (Key.Space, new KeyModifiers ()));


			Assert.False (pets.ElementAt (0).IsPicked);
			Assert.True (pets.ElementAt (1).IsPicked);
			Assert.False (pets.ElementAt (2).IsPicked);

			tv.Draw();

			expected =
				@"
┌─┬───────┬─────────────┐
│ │Name   │Kind         │
├─┼───────┼─────────────┤
│☐│Tammy  │Cat          │
│☑│Tibbles│Cat          │
│☐│Ripper │Dog          │";

			TestHelpers.AssertDriverContentsAre (expected, output);

		}

		[Fact, AutoInitShutdown]
		public void TestTableViewCheckboxes_SelectAllToggle_ByObject ()
		{

			var tv = GetPetTable (out var source);
			tv.LayoutSubviews ();
			var pets = source.Data;

			var wrapper = new CheckBoxTableSourceWrapperByObject<PickablePet> (
				tv,
				source,
				(p) => p.IsPicked,
				(p, b) => p.IsPicked = b);

			tv.Table = wrapper;


			Assert.DoesNotContain (pets, p => p.IsPicked);

			//toggle all cells
			tv.ProcessKey (new KeyEvent (Key.A | Key.CtrlMask, new KeyModifiers { Ctrl = true }));
			tv.ProcessKey (new KeyEvent (Key.Space, new KeyModifiers ()));

			Assert.True (pets.All (p => p.IsPicked));

			tv.Draw();

			string expected =
				@"
┌─┬───────┬─────────────┐
│ │Name   │Kind         │
├─┼───────┼─────────────┤
│☑│Tammy  │Cat          │
│☑│Tibbles│Cat          │
│☑│Ripper │Dog          │";

			TestHelpers.AssertDriverContentsAre (expected, output);


			tv.ProcessKey (new KeyEvent (Key.Space, new KeyModifiers ()));

			Assert.Empty (pets.Where (p => p.IsPicked));

			tv.Draw();

			expected =
				@"
┌─┬───────┬─────────────┐
│ │Name   │Kind         │
├─┼───────┼─────────────┤
│☐│Tammy  │Cat          │
│☐│Tibbles│Cat          │
│☐│Ripper │Dog          │
";

			TestHelpers.AssertDriverContentsAre (expected, output);
		}

		[Fact, AutoInitShutdown]
		public void TestTableViewRadioBoxes_Simple_ByObject ()
		{

			var tv = GetPetTable (out var source);
			tv.LayoutSubviews ();
			var pets = source.Data;

			var wrapper = new CheckBoxTableSourceWrapperByObject<PickablePet> (
				tv,
				source,
				(p) => p.IsPicked,
				(p, b) => p.IsPicked = b);

			wrapper.UseRadioButtons = true;

			tv.Table = wrapper;
			tv.Draw();

			string expected =
				@"
┌─┬───────┬─────────────┐
│ │Name   │Kind         │
├─┼───────┼─────────────┤
│○│Tammy  │Cat          │
│○│Tibbles│Cat          │
│○│Ripper │Dog          │
";

			TestHelpers.AssertDriverContentsAre (expected, output);

			Assert.Empty (pets.Where (p => p.IsPicked));

			tv.ProcessKey (new KeyEvent (Key.Space, new KeyModifiers ()));

			Assert.True (pets.First ().IsPicked);

			tv.Draw();

			expected =
				@"
┌─┬───────┬─────────────┐
│ │Name   │Kind         │
├─┼───────┼─────────────┤
│◉│Tammy  │Cat          │
│○│Tibbles│Cat          │
│○│Ripper │Dog          │";

			TestHelpers.AssertDriverContentsAre (expected, output);


			tv.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ()));
			tv.ProcessKey (new KeyEvent (Key.Space, new KeyModifiers ()));

			Assert.False (pets.ElementAt (0).IsPicked);
			Assert.True (pets.ElementAt (1).IsPicked);
			Assert.False (pets.ElementAt (2).IsPicked);

			tv.Draw();

			expected =
				@"
┌─┬───────┬─────────────┐
│ │Name   │Kind         │
├─┼───────┼─────────────┤
│○│Tammy  │Cat          │
│◉│Tibbles│Cat          │
│○│Ripper │Dog          │";

			TestHelpers.AssertDriverContentsAre (expected, output);


			tv.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ()));
			tv.ProcessKey (new KeyEvent (Key.Space, new KeyModifiers ()));


			Assert.True (pets.ElementAt (0).IsPicked);
			Assert.False (pets.ElementAt (1).IsPicked);
			Assert.False (pets.ElementAt (2).IsPicked);

			tv.Draw();

			expected =
				@"
┌─┬───────┬─────────────┐
│ │Name   │Kind         │
├─┼───────┼─────────────┤
│◉│Tammy  │Cat          │
│○│Tibbles│Cat          │
│○│Ripper │Dog          │";

			TestHelpers.AssertDriverContentsAre (expected, output);
		}

		[Fact, AutoInitShutdown]
		public void TestFullRowSelect_SelectionColorDoesNotStop_WhenShowVerticalCellLinesIsFalse ()
		{
			var tv = GetTwoRowSixColumnTable (out var dt);
			dt.Rows.Add (1, 2, 3, 4, 5, 6);
			tv.LayoutSubviews ();


			tv.Bounds = new Rect (0, 0, 7, 6);

			tv.FullRowSelect = true;
			tv.Style.ShowVerticalCellLines = false;
			tv.Style.ShowVerticalHeaderLines = false;

			// Clicking in bottom row
			tv.MouseEvent (new MouseEvent {
				X = 1,
				Y = 4,
				Flags = MouseFlags.Button1Clicked
			});

			// should select that row
			Assert.Equal (2, tv.SelectedRow);


			tv.Draw ();

			string expected =
				@"
A B C
───────
1 2 3
1 2 3
1 2 3";

			TestHelpers.AssertDriverContentsAre (expected, output);

			var normal = tv.ColorScheme.Normal;
			var focus = tv.ColorScheme.HotFocus = new Attribute (Color.Magenta, Color.White);

			tv.Draw ();

			// HotFocus color (1) should be used for rendering the selected line
			// Note that because there are no vertical cell lines we use the hot focus
			// color for the whole row
			expected =
				@"
000000
000000
000000
000000
111111";

			TestHelpers.AssertDriverColorsAre (expected, normal, focus);
		}

		public static DataTableSource BuildTable (int cols, int rows)
		{
			return BuildTable (cols, rows, out _);
		}

		/// <summary>
		/// Builds a simple table of string columns with the requested number of columns and rows
		/// </summary>
		/// <param name="cols"></param>
		/// <param name="rows"></param>
		/// <returns></returns>
		public static DataTableSource BuildTable (int cols, int rows, out DataTable dt)
		{
			dt = new DataTable ();

			for (int c = 0; c < cols; c++) {
				dt.Columns.Add ("Col" + c);
			}

			for (int r = 0; r < rows; r++) {
				var newRow = dt.NewRow ();

				for (int c = 0; c < cols; c++) {
					newRow [c] = $"R{r}C{c}";
				}

				dt.Rows.Add (newRow);
			}

			return new DataTableSource (dt);
		}

		[Fact, AutoInitShutdown]
		public void Test_ScreenToCell ()
		{
			var tableView = GetTwoRowSixColumnTable ();
			tableView.BeginInit (); tableView.EndInit ();
			tableView.LayoutSubviews ();

			tableView.Draw ();

			// user can only scroll right so sees right indicator
			// Because first column in table is A
			string expected =
				@"
│A│B│C│
├─┼─┼─►
│1│2│3│
│1│2│3│";

			TestHelpers.AssertDriverContentsAre (expected, output);

			// ---------------- X=0 -----------------------
			// click is before first cell
			Assert.Null (tableView.ScreenToCell (0, 0));
			Assert.Null (tableView.ScreenToCell (0, 1));
			Assert.Null (tableView.ScreenToCell (0, 2));
			Assert.Null (tableView.ScreenToCell (0, 3));
			Assert.Null (tableView.ScreenToCell (0, 4));

			// ---------------- X=1 -----------------------
			// click in header
			Assert.Null (tableView.ScreenToCell (1, 0));
			// click in header row line
			Assert.Null (tableView.ScreenToCell (1, 1));
			// click in cell 0,0
			Assert.Equal (new Point (0, 0), tableView.ScreenToCell (1, 2));
			// click in cell 0,1
			Assert.Equal (new Point (0, 1), tableView.ScreenToCell (1, 3));
			// after last row
			Assert.Null (tableView.ScreenToCell (1, 4));

			// ---------------- X=2 -----------------------
			// ( even though there is a horizontal dividing line here we treat it as a hit on the cell before)
			// click in header
			Assert.Null (tableView.ScreenToCell (2, 0));
			// click in header row line
			Assert.Null (tableView.ScreenToCell (2, 1));
			// click in cell 0,0
			Assert.Equal (new Point (0, 0), tableView.ScreenToCell (2, 2));
			// click in cell 0,1
			Assert.Equal (new Point (0, 1), tableView.ScreenToCell (2, 3));
			// after last row
			Assert.Null (tableView.ScreenToCell (2, 4));

			// ---------------- X=3 -----------------------
			// click in header
			Assert.Null (tableView.ScreenToCell (3, 0));
			// click in header row line
			Assert.Null (tableView.ScreenToCell (3, 1));
			// click in cell 1,0
			Assert.Equal (new Point (1, 0), tableView.ScreenToCell (3, 2));
			// click in cell 1,1
			Assert.Equal (new Point (1, 1), tableView.ScreenToCell (3, 3));
			// after last row
			Assert.Null (tableView.ScreenToCell (3, 4));
		}

		[Fact, AutoInitShutdown]
		public void Test_ScreenToCell_DataColumnOverload ()
		{
			var tableView = GetTwoRowSixColumnTable ();
			tableView.LayoutSubviews ();

			tableView.Draw ();

			// user can only scroll right so sees right indicator
			// Because first column in table is A
			string expected =
				@"
│A│B│C│
├─┼─┼─►
│1│2│3│
│1│2│3│";

			TestHelpers.AssertDriverContentsAre (expected, output);
			int? col;

			// ---------------- X=0 -----------------------
			// click is before first cell
			Assert.Null (tableView.ScreenToCell (0, 0, out col));
			Assert.Null (col);
			Assert.Null (tableView.ScreenToCell (0, 1, out col));
			Assert.Null (col);
			Assert.Null (tableView.ScreenToCell (0, 2, out col));
			Assert.Null (col);
			Assert.Null (tableView.ScreenToCell (0, 3, out col));
			Assert.Null (col);
			Assert.Null (tableView.ScreenToCell (0, 4, out col));
			Assert.Null (col);

			// ---------------- X=1 -----------------------
			// click in header
			Assert.Null (tableView.ScreenToCell (1, 0, out col));
			Assert.Equal ("A", tableView.Table.ColumnNames [col.Value]);
			// click in header row line  (click in the horizontal line below header counts as click in header above - consistent with the column hit box)
			Assert.Null (tableView.ScreenToCell (1, 1, out col));
			Assert.Equal ("A", tableView.Table.ColumnNames [col.Value]);
			// click in cell 0,0
			Assert.Equal (new Point (0, 0), tableView.ScreenToCell (1, 2, out col));
			Assert.Null (col);
			// click in cell 0,1
			Assert.Equal (new Point (0, 1), tableView.ScreenToCell (1, 3, out col));
			Assert.Null (col);
			// after last row
			Assert.Null (tableView.ScreenToCell (1, 4, out col));
			Assert.Null (col);

			// ---------------- X=2 -----------------------
			// click in header
			Assert.Null (tableView.ScreenToCell (2, 0, out col));
			Assert.Equal ("A", tableView.Table.ColumnNames [col.Value]);
			// click in header row line
			Assert.Null (tableView.ScreenToCell (2, 1, out col));
			Assert.Equal ("A", tableView.Table.ColumnNames [col.Value]);
			// click in cell 0,0
			Assert.Equal (new Point (0, 0), tableView.ScreenToCell (2, 2, out col));
			Assert.Null (col);
			// click in cell 0,1
			Assert.Equal (new Point (0, 1), tableView.ScreenToCell (2, 3, out col));
			Assert.Null (col);
			// after last row
			Assert.Null (tableView.ScreenToCell (2, 4, out col));
			Assert.Null (col);

			// ---------------- X=3 -----------------------
			// click in header
			Assert.Null (tableView.ScreenToCell (3, 0, out col));
			Assert.Equal ("B", tableView.Table.ColumnNames [col.Value]);
			// click in header row line
			Assert.Null (tableView.ScreenToCell (3, 1, out col));
			Assert.Equal ("B", tableView.Table.ColumnNames [col.Value]);
			// click in cell 1,0
			Assert.Equal (new Point (1, 0), tableView.ScreenToCell (3, 2, out col));
			Assert.Null (col);
			// click in cell 1,1
			Assert.Equal (new Point (1, 1), tableView.ScreenToCell (3, 3, out col));
			Assert.Null (col);
			// after last row
			Assert.Null (tableView.ScreenToCell (3, 4, out col));
			Assert.Null (col);
		}

		/// <summary>
		/// Builds a simple list with the requested number of string items
		/// </summary>
		/// <param name="items"></param>
		/// <returns></returns>
		public static IList BuildList (int items)
		{
			var list = new List<string> ();
			for (int i = 0; i < items; i++) {
				list.Add ("Item " + i);
			}
			return list.ToArray ();
		}

		[Theory, AutoInitShutdown]
		[InlineData (new object [] { Orientation.Horizontal, false })]
		[InlineData (new object [] { Orientation.Vertical, false })]
		[InlineData (new object [] { Orientation.Horizontal, true })]
		[InlineData (new object [] { Orientation.Vertical, true })]
		public void TestListTableSource (Orientation orient, bool parallel)
		{
			var list = BuildList (16);

			var tv = new TableView ();
			//tv.BeginInit (); tv.EndInit ();
			tv.ColorScheme = Colors.TopLevel;
			tv.Bounds = new Rect (0, 0, 25, 4);
			tv.Style = new () {
				ShowHeaders = false,
				ShowHorizontalHeaderOverline = false,
				ShowHorizontalHeaderUnderline = false
			};
			var listStyle = new ListColumnStyle () {
				Orientation = orient,
				ScrollParallel = parallel
			};

			tv.Table = new ListTableSource (list, tv, listStyle);

			tv.LayoutSubviews ();

			tv.Draw ();

			string horizPerpExpected =
				@"
│Item 0│Item 1          │
│Item 2│Item 3          │
│Item 4│Item 5          │
│Item 6│Item 7          │";

			string horizParaExpected =
				@"
│Item 0 │Item 1 │Item 2 │
│Item 4 │Item 5 │Item 6 │
│Item 8 │Item 9 │Item 10│
│Item 12│Item 13│Item 14│";

			string vertPerpExpected =
				@"
│Item 0│Item 4│Item 8   │
│Item 1│Item 5│Item 9   │
│Item 2│Item 6│Item 10  │
│Item 3│Item 7│Item 11  │";

			string vertParaExpected =
				@"
│Item 0│Item 8          │
│Item 1│Item 9          │
│Item 2│Item 10         │
│Item 3│Item 11         │";

			string expected;
			if (orient == Orientation.Vertical)
				if (parallel) {
					expected = vertParaExpected;
				} else {
					expected = vertPerpExpected;
				}
			else {
				if (parallel) {
					expected = horizParaExpected;
				} else {
					expected = horizPerpExpected;
				}
			}

			TestHelpers.AssertDriverContentsAre (expected, output);
		}

		[Fact, AutoInitShutdown]
		public void TestEnumerableDataSource_BasicTypes ()
		{
			var tv = new TableView ();
			tv.ColorScheme = Colors.TopLevel;
			tv.Bounds = new Rect (0, 0, 50, 6);

			tv.Table = new EnumerableTableSource<Type> (
				new Type [] { typeof (string), typeof (int), typeof (float) },
				new () {
					{ "Name", (t)=>t.Name},
					{ "Namespace", (t)=>t.Namespace},
					{ "BaseType", (t)=>t.BaseType}
				});

			tv.LayoutSubviews ();

			tv.Draw ();

			string expected =
				@"
┌──────┬─────────┬───────────────────────────────┐
│Name  │Namespace│BaseType                       │
├──────┼─────────┼───────────────────────────────┤
│String│System   │System.Object                  │
│Int32 │System   │System.ValueType               │
│Single│System   │System.ValueType               │";

			TestHelpers.AssertDriverContentsAre (expected, output);
		}
		[Fact, AutoInitShutdown]
		public void Test_CollectionNavigator ()
		{
			var tv = new TableView ();
			tv.ColorScheme = Colors.TopLevel;
			tv.Bounds = new Rect (0, 0, 50, 7);

			tv.Table = new EnumerableTableSource<string> (
				new string [] { "fish", "troll", "trap", "zoo" },
				new () {
					{ "Name", (t)=>t},
					{ "EndsWith", (t)=>t.Last()}
				});

			tv.LayoutSubviews ();

			tv.Draw ();

			string expected =
				@"
┌─────┬──────────────────────────────────────────┐
│Name │EndsWith                                  │
├─────┼──────────────────────────────────────────┤
│fish │h                                         │
│troll│l                                         │
│trap │p                                         │
│zoo  │o                                         │";

			TestHelpers.AssertDriverContentsAre (expected, output);

			Assert.Equal (0, tv.SelectedRow);

			// this test assumes no focus
			Assert.False (tv.HasFocus);

			// already on fish
			tv.ProcessKey (new KeyEvent { Key = Key.f });
			Assert.Equal (0, tv.SelectedRow);

			// not focused
			tv.ProcessKey (new KeyEvent { Key = Key.z });
			Assert.Equal (0, tv.SelectedRow);

			// ensure that TableView has the input focus
			Application.Top.Add (tv);
			Application.Begin (Application.Top);

			Application.Top.FocusFirst ();
			Assert.True (tv.HasFocus);

			// already on fish
			tv.ProcessKey (new KeyEvent { Key = Key.f });
			Assert.Equal (0, tv.SelectedRow);

			// move to zoo
			tv.ProcessKey (new KeyEvent { Key = Key.z });
			Assert.Equal (3, tv.SelectedRow);

			// move to troll
			tv.ProcessKey (new KeyEvent { Key = Key.t });
			Assert.Equal (1, tv.SelectedRow);

			// move to trap
			tv.ProcessKey (new KeyEvent { Key = Key.t });
			Assert.Equal (2, tv.SelectedRow);

			// change columns to navigate by column 2
			Assert.Equal (0, tv.SelectedColumn);
			Assert.Equal (2, tv.SelectedRow);
			tv.ProcessKey (new KeyEvent { Key = Key.CursorRight });
			Assert.Equal (1, tv.SelectedColumn);
			Assert.Equal (2, tv.SelectedRow);

			// nothing ends with t so stay where you are
			tv.ProcessKey (new KeyEvent { Key = Key.t });
			Assert.Equal (2, tv.SelectedRow);

			//jump to fish which ends in h
			tv.ProcessKey (new KeyEvent { Key = Key.h });
			Assert.Equal (0, tv.SelectedRow);

			// jump to zoo which ends in o
			tv.ProcessKey (new KeyEvent { Key = Key.o });
			Assert.Equal (3, tv.SelectedRow);


		}
		private TableView GetTwoRowSixColumnTable ()
		{
			return GetTwoRowSixColumnTable (out _);
		}
		private TableView GetTwoRowSixColumnTable (out DataTable dt)
		{
			var tableView = new TableView ();
			tableView.ColorScheme = Colors.TopLevel;

			// 3 columns are visible
			tableView.Bounds = new Rect (0, 0, 7, 5);
			tableView.Style.ShowHorizontalHeaderUnderline = true;
			tableView.Style.ShowHorizontalHeaderOverline = false;
			tableView.Style.AlwaysShowHeaders = true;
			tableView.Style.SmoothHorizontalScrolling = true;

			dt = new DataTable ();
			dt.Columns.Add ("A");
			dt.Columns.Add ("B");
			dt.Columns.Add ("C");
			dt.Columns.Add ("D");
			dt.Columns.Add ("E");
			dt.Columns.Add ("F");

			dt.Rows.Add (1, 2, 3, 4, 5, 6);
			dt.Rows.Add (1, 2, 3, 4, 5, 6);

			tableView.Table = new DataTableSource (dt);
			return tableView;
		}


		private class PickablePet {
			public bool IsPicked { get; set; }
			public string Name{ get; set; }
			public string Kind { get; set; }

			public PickablePet (bool isPicked, string name, string kind)
			{
				IsPicked = isPicked;
				Name = name;
				Kind = kind;
			}
		}

		private TableView GetPetTable (out EnumerableTableSource<PickablePet> source)
		{
			var tv = new TableView ();
			tv.ColorScheme = Colors.TopLevel;
			tv.Bounds = new Rect (0, 0, 25, 6);

			var pets = new List<PickablePet> {
				new PickablePet(false,"Tammy","Cat"),
				new PickablePet(false,"Tibbles","Cat"),
				new PickablePet(false,"Ripper","Dog")};

			tv.Table = source = new EnumerableTableSource<PickablePet> (
				pets,
				new () {
					{ "Name", (p) => p.Name},
					{ "Kind", (p) => p.Kind},
				});

			tv.LayoutSubviews ();

			return tv;
		}

	}
}