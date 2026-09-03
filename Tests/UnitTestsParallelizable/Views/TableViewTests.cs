using System.Data;
using System.Reflection;
using JetBrains.Annotations;
using UnitTests;

namespace ViewsTests;

[TestSubject (typeof (TableView))]
public class TableViewTests : TestDriverBase
{
    [Fact]
    public void CanTabOutOfTableViewUsingCursor_Left ()
    {
        GetTableViewWithSiblings (out TextField tf1, out TableView tableView, out TextField _);

        // Make the selected cell one in
        tableView.SetSelection (1, tableView.Value?.SelectedCell.Y ?? 0, false);

        // Pressing left should move us to the first column without changing focus
        tableView.App!.Keyboard.RaiseKeyDownEvent (Key.CursorLeft);
        Assert.Same (tableView, tableView.App!.TopRunnableView!.MostFocused);
        Assert.True (tableView.HasFocus);

        // Because we are now on the leftmost cell a further left press should move focus
        tableView.App!.Keyboard.RaiseKeyDownEvent (Key.CursorLeft);

        Assert.NotSame (tableView, tableView.App!.TopRunnableView!.MostFocused);
        Assert.False (tableView.HasFocus);

        Assert.Same (tf1, tableView.App!.TopRunnableView.MostFocused);
        Assert.True (tf1.HasFocus);
    }

    [Fact]
    public void CanTabOutOfTableViewUsingCursor_Up ()
    {
        GetTableViewWithSiblings (out TextField tf1, out TableView tableView, out TextField _);

        // Make the selected cell one in
        tableView.SetSelection (tableView.Value?.SelectedCell.X ?? 0, 1, false);

        // First press should move us up
        tableView.App!.Keyboard.RaiseKeyDownEvent (Key.CursorUp);
        Assert.Same (tableView, tableView.App!.TopRunnableView!.MostFocused);
        Assert.True (tableView.HasFocus);

        // Because we are now on the top row a further press should move focus
        tableView.App!.Keyboard.RaiseKeyDownEvent (Key.CursorUp);

        Assert.NotSame (tableView, tableView.App!.TopRunnableView!.MostFocused);
        Assert.False (tableView.HasFocus);

        Assert.Same (tf1, tableView.App!.TopRunnableView!.MostFocused);
        Assert.True (tf1.HasFocus);
    }

    [Fact]
    public void CanTabOutOfTableViewUsingCursor_Right ()
    {
        GetTableViewWithSiblings (out TextField _, out TableView tableView, out TextField tf2);

        // Make the selected cell one in from the rightmost column
        tableView.SetSelection (tableView.Table!.Columns - 2, tableView.Value?.SelectedCell.Y ?? 0, false);

        // First press should move us to the rightmost column without changing focus
        tableView.App!.Keyboard.RaiseKeyDownEvent (Key.CursorRight);
        Assert.Same (tableView, tableView.App!.TopRunnableView!.MostFocused);
        Assert.True (tableView.HasFocus);

        // Because we are now on the rightmost cell, a further right press should move focus
        tableView.App!.Keyboard.RaiseKeyDownEvent (Key.CursorRight);

        Assert.NotSame (tableView, tableView.App!.TopRunnableView!.MostFocused);
        Assert.False (tableView.HasFocus);

        Assert.Same (tf2, tableView.App!.TopRunnableView!.MostFocused);
        Assert.True (tf2.HasFocus);
    }

    [Fact]
    public void CanTabOutOfTableViewUsingCursor_Down ()
    {
        GetTableViewWithSiblings (out TextField _, out TableView tableView, out TextField tf2);

        // Make the selected cell one in from the bottommost row
        tableView.SetSelection (tableView.Value?.SelectedCell.X ?? 0, tableView.Table!.Rows - 2, false);

        // First press should move us to the bottommost row without changing focus
        tableView.App!.Keyboard.RaiseKeyDownEvent (Key.CursorDown);
        Assert.Same (tableView, tableView.App!.TopRunnableView!.MostFocused);
        Assert.True (tableView.HasFocus);

        // Because we are now on the bottommost cell, a further down press should move focus
        tableView.App!.Keyboard.RaiseKeyDownEvent (Key.CursorDown);

        Assert.NotSame (tableView, tableView.App!.TopRunnableView!.MostFocused);
        Assert.False (tableView.HasFocus);

        Assert.Same (tf2, tableView.App!.TopRunnableView!.MostFocused);
        Assert.True (tf2.HasFocus);
    }

    [Fact]
    public void CanTabOutOfTableViewUsingCursor_Left_ClearsSelectionFirst ()
    {
        GetTableViewWithSiblings (out TextField tf1, out TableView tableView, out TextField _);

        // Make the selected cell one in
        tableView.SetSelection (1, tableView.Value?.SelectedCell.Y ?? 0, false);

        // Pressing shift-left should give us a multi selection
        tableView.App!.Keyboard.RaiseKeyDownEvent (Key.CursorLeft.WithShift);
        Assert.Same (tableView, tableView.App!.TopRunnableView!.MostFocused);
        Assert.True (tableView.HasFocus);
        Assert.Equal (2, tableView.GetAllSelectedCells ().Count ());

        // Because we are now on the leftmost cell a further left press would normally move focus
        // However there is an ongoing selection so instead the operation clears the selection and
        // gets swallowed (not resulting in a focus change)
        tableView.App!.Keyboard.RaiseKeyDownEvent (Key.CursorLeft);

        // Selection 'clears' just to the single cell and we remain focused
        Assert.Single (tableView.GetAllSelectedCells ());
        Assert.Same (tableView, tableView.App!.TopRunnableView!.MostFocused);
        Assert.True (tableView.HasFocus);

        // A further left will switch focus
        tableView.App!.Keyboard.RaiseKeyDownEvent (Key.CursorLeft);

        Assert.NotSame (tableView, tableView.App!.TopRunnableView!.MostFocused);
        Assert.False (tableView.HasFocus);

        Assert.Same (tf1, tableView.App!.TopRunnableView!.MostFocused);
        Assert.True (tf1.HasFocus);
    }

    /// <summary>
    ///     Creates 3 views on <see cref="Application.TopRunnableView"/> with the focus in the
    ///     <see cref="TableView"/>.  This is a helper method to set up tests that want to
    ///     explore moving input focus out of a tableview.
    /// </summary>
    /// <param name="tf1"></param>
    /// <param name="tableView"></param>
    /// <param name="tf2"></param>
    private static void GetTableViewWithSiblings (out TextField tf1, out TableView tableView, out TextField tf2)
    {
        IApplication app = Application.Create ();
        Runnable<bool> runnable = new ();
        app.Begin (runnable);

        tableView = new TableView ();
        tableView.Viewport = new Rectangle (0, 0, 25, 10);

        tf1 = new TextField ();
        tf2 = new TextField ();
        runnable.Add (tf1);
        runnable.Add (tableView);
        runnable.Add (tf2);

        tableView.SetFocus ();

        Assert.Same (tableView, runnable.MostFocused);
        Assert.True (tableView.HasFocus);

        // Set big table
        tableView.Table = BuildTable (25, 50);
    }

    public static DataTableSource BuildTable (int cols, int rows) => BuildTable (cols, rows, out _);

    /// <summary>Builds a simple table of string columns with the requested number of columns and rows</summary>
    /// <param name="cols"></param>
    /// <param name="rows"></param>
    /// <param name="dt"></param>
    /// <returns></returns>
    public static DataTableSource BuildTable (int cols, int rows, out DataTable dt)
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

    [Fact]
    public void TableView_CollectionNavigatorMatcher_KeybindingsOverrideNavigator ()
    {
        var dt = new DataTable ();
        dt.Columns.Add ("blah");

        dt.Rows.Add ("apricot");
        dt.Rows.Add ("arm");
        dt.Rows.Add ("bat");
        dt.Rows.Add ("batman");
        dt.Rows.Add ("bates hotel");
        dt.Rows.Add ("candle");

        var tableView = new TableView ();
        tableView.Table = new DataTableSource (dt);
        tableView.HasFocus = true;
        tableView.KeyBindings.Add (Key.B, Command.Down);

        Assert.Equal (0, tableView.Value!.SelectedCell.Y);

        // Keys should be consumed to move down the navigation i.e. to apricot
        Assert.True (tableView.NewKeyDownEvent (Key.B));
        Assert.Equal (1, tableView.Value!.SelectedCell.Y);

        Assert.True (tableView.NewKeyDownEvent (Key.B));
        Assert.Equal (2, tableView.Value!.SelectedCell.Y);

        // There is no keybinding for Key.C so it hits collection navigator i.e. we jump to candle
        Assert.True (tableView.NewKeyDownEvent (Key.C));
        Assert.Equal (5, tableView.Value!.SelectedCell.Y);
    }

    // Copilot
    [Fact]
    public void TableView_ContentSize_UsesWideCellContent_WhenContentExceedsViewport ()
    {
        DataTable dt = new ();
        dt.Columns.Add ("A");
        dt.Columns.Add ("Description");
        dt.Columns.Add ("C");
        dt.Rows.Add ("x", new string ('d', 120), "y");

        TableView tableView = new ()
        {
            Frame = new Rectangle (0, 0, 20, 5),
            ViewportSettings = ViewportSettingsFlags.HasHorizontalScrollBar,
            Table = new DataTableSource (dt)
        };

        tableView.BeginInit ();
        tableView.EndInit ();

        Size contentSize = tableView.GetContentSize ();

        Assert.True (contentSize.Width > tableView.Viewport.Width);
        Assert.True (tableView.HorizontalScrollBar.Visible);
    }

    [Fact]
    public void TableView_CollectionNavigatorMatcher_HotKey_Finds_Item ()
    {
        var dt = new DataTable ();
        dt.Columns.Add ("blah");

        dt.Rows.Add ("apricot");
        dt.Rows.Add ("arm");
        dt.Rows.Add ("bat");
        dt.Rows.Add ("batman");
        dt.Rows.Add ("bates hotel");
        dt.Rows.Add ("candle");

        var tableView = new TableView ();
        tableView.HotKey = Key.B;
        tableView.Table = new DataTableSource (dt);
        tableView.HasFocus = true;

        Assert.Equal (0, tableView.Value!.SelectedCell.Y);

        Assert.True (tableView.NewKeyDownEvent (Key.B));
        Assert.Equal (2, tableView.Value!.SelectedCell.Y);
    }

    // Claude - Opus 4.7
    // Regression: setting CollectionNavigator to null disables type-to-search and lets printable keys bubble
    [Fact]
    public void CollectionNavigator_Null_DisablesTypeToSearch_KeyNotConsumed ()
    {
        var dt = new DataTable ();
        dt.Columns.Add ("blah");
        dt.Rows.Add ("apricot");
        dt.Rows.Add ("bat");
        dt.Rows.Add ("candle");

        TableView tableView = new () { Table = new DataTableSource (dt), CollectionNavigator = null };
        tableView.HasFocus = true;

        Assert.Equal (0, tableView.Value!.SelectedCell.Y);

        // Key.B would normally jump to "bat" (row 1) — with navigator disabled, selection must not move
        // and the key event must not be consumed (returns false so it can bubble to the SuperView).
        bool consumed = tableView.NewKeyDownEvent (Key.B);

        Assert.False (consumed);
        Assert.Equal (0, tableView.Value!.SelectedCell.Y);
    }

    // Claude - Opus 4.7
    // Regression: with CollectionNavigator = null, HotKey path must not throw
    [Fact]
    public void CollectionNavigator_Null_HotKey_DoesNotThrow ()
    {
        var dt = new DataTable ();
        dt.Columns.Add ("blah");
        dt.Rows.Add ("apricot");
        dt.Rows.Add ("bat");

        TableView tableView = new ()
        {
            Table = new DataTableSource (dt),
            CollectionNavigator = null,
            HotKey = Key.B
        };
        tableView.HasFocus = true;

        Exception? ex = Record.Exception (() => tableView.NewKeyDownEvent (Key.B));

        Assert.Null (ex);
        Assert.Equal (0, tableView.Value!.SelectedCell.Y);
    }

    // Copilot
    // Behavior: Space toggles multi-selection via ToggleExtend command
    [Fact]
    public void TableView_ToggleExtend_TogglesSelection ()
    {
        var dt = new DataTable ();
        dt.Columns.Add ("Col1");
        dt.Rows.Add ("Data1");
        dt.Rows.Add ("Data2");

        TableView tableView = new () { Table = new DataTableSource (dt) };
        tableView.BeginInit ();
        tableView.EndInit ();

        tableView.InvokeCommand (Command.ToggleExtend);

        Assert.True (tableView.MultiSelectedRegions.Count > 0);

        tableView.Dispose ();
    }

    // Copilot
    [Fact]
    public void TableView_Command_Accept_FiresAccepted ()
    {
        var dt = new DataTable ();
        dt.Columns.Add ("Col1");
        dt.Rows.Add ("Data1");

        TableView tableView = new () { Table = new DataTableSource (dt) };
        var acceptedFired = false;

        tableView.Accepted += (_, _) => acceptedFired = true;

        tableView.InvokeCommand (Command.Accept);

        Assert.True (acceptedFired);

        tableView.Dispose ();
    }

    // Copilot
    [Fact]
    public void TableView_Space_AddsToMultiSelectedRegions ()
    {
        var dt = new DataTable ();
        dt.Columns.Add ("Col1");
        dt.Rows.Add ("Data1");

        TableView tableView = new () { Table = new DataTableSource (dt) };
        tableView.BeginInit ();
        tableView.EndInit ();

        tableView.NewKeyDownEvent (Key.Space);

        Assert.True (tableView.MultiSelectedRegions.Count > 0);

        tableView.Dispose ();
    }

    // Copilot
    [Fact]
    public void TableView_Enter_FiresAccepted ()
    {
        var dt = new DataTable ();
        dt.Columns.Add ("Col1");
        dt.Rows.Add ("Data1");

        TableView tableView = new () { Table = new DataTableSource (dt) };
        var acceptedFired = false;

        tableView.Accepted += (_, _) => acceptedFired = true;

        tableView.NewKeyDownEvent (Key.Enter);

        Assert.True (acceptedFired);

        tableView.Dispose ();
    }

    // Copilot - regression: TableCollectionNavigator must not throw when table is null and view is focused
    [Fact]
    public void TableCollectionNavigator_NullTable_HasFocus_DoesNotThrow ()
    {
        TableView tableView = new ();
        tableView.HasFocus = true;

        // Table is null + HasFocus=true - keystroke navigation reached via OnKeyDownNotHandled
        // should not throw InvalidOperationException from GetCollectionLength
        Exception? ex = Record.Exception (() => tableView.NewKeyDownEvent (Key.A));

        Assert.Null (ex);
    }

    // Copilot - regression: TableCollectionNavigator must not throw when a cell value is null (custom ITableSource)
    [Fact]
    public void TableCollectionNavigator_NullCellValue_DoesNotThrow ()
    {
        // Use a custom ITableSource that can return null for cell values
        // (DataTable wraps null as DBNull.Value, so we need a custom source to test actual null)
        TableView tableView = new () { Table = new NullCellTableSource () };
        tableView.HasFocus = true;

        // Pressing 'a' triggers keystroke navigation; row 0 has null cell, row 1 has "apple"
        // Should not throw InvalidOperationException from ElementAt
        Exception? ex = Record.Exception (() => tableView.NewKeyDownEvent (Key.A));

        Assert.Null (ex);

        // Should land on "apple" (row 1), skipping the null-cell row gracefully
        Assert.Equal (1, tableView.Value!.SelectedCell.Y);

        tableView.Dispose ();
    }

    // Copilot - regression: TableCollectionNavigator returns string.Empty for DBNull cells
    [Fact]
    public void TableCollectionNavigator_DBNullCellValue_DoesNotThrow ()
    {
        DataTable dt = new ();
        dt.Columns.Add ("Col1");
        dt.Rows.Add (DBNull.Value); // DataTable stores this as DBNull.Value
        dt.Rows.Add ("banana");
        dt.Rows.Add ("berry");

        TableView tableView = new () { Table = new DataTableSource (dt) };
        tableView.HasFocus = true;

        Exception? ex = Record.Exception (() => tableView.NewKeyDownEvent (Key.B));

        Assert.Null (ex);
        Assert.Equal (1, tableView.Value!.SelectedCell.Y);

        tableView.Dispose ();
    }

    /// <summary>A minimal <see cref="ITableSource"/> that returns <see langword="null"/> for the first cell.</summary>
    private sealed class NullCellTableSource : ITableSource
    {
        // Row 0 intentionally holds null to exercise null-cell handling in TableCollectionNavigator
        private readonly object? [] _data = [null, "apple", "apricot"];

        public object this [int row, int col]
        {
#pragma warning disable CS8603 // Possible null reference return - intentional for testing null-cell handling
            get => _data [row];
#pragma warning restore CS8603
        }

        public int Rows => _data.Length;

        public int Columns => 1;

        public string [] ColumnNames => ["Col1"];
    }

    // Copilot - regression: ColumnOffset setter must not throw when all columns are hidden (0 visible columns)
    [Fact]
    public void ColumnOffset_AllColumnsHidden_DoesNotThrow ()
    {
        DataTable dt = new ();
        dt.Columns.Add ("Col1");
        dt.Rows.Add ("a");

        TableView tableView = new () { Table = new DataTableSource (dt) };
        tableView.BeginInit ();
        tableView.EndInit ();

        // Hide the only column — this makes the cache empty (0 visible columns)
        tableView.Style.GetOrCreateColumnStyle (0).Visible = false;
        tableView.Update ();

        // Setting ColumnOffset=0 with an empty render cache previously computed value=-1
        // and then indexed _columnsToRenderCache![-1], causing IndexOutOfRangeException
        Exception? ex = Record.Exception (() => tableView.ColumnOffset = 0);

        Assert.Null (ex);
        Assert.Equal (0, tableView.ColumnOffset);

        tableView.Dispose ();
    }

    // Copilot - regression: ColumnOffset setter must not throw when table is null
    [Fact]
    public void ColumnOffset_NullTable_DoesNotThrow ()
    {
        TableView tableView = new ();
        tableView.BeginInit ();
        tableView.EndInit ();

        Exception? ex = Record.Exception (() => tableView.ColumnOffset = 0);

        Assert.Null (ex);
        Assert.Equal (0, tableView.ColumnOffset);

        tableView.Dispose ();
    }

    [Fact]
    public void Test_SumColumnWidth_GraphemeClusters ()
    {
        var family = "\U0001F468\u200D\U0001F469\u200D\U0001F466\u200D\U0001F466"; // 👨‍👩‍👦‍👦
        Assert.Equal (8, family.EnumerateRunes ().Sum (c => c.GetColumns ()));
        Assert.Equal (2, family.GetColumns ());

        var technologist = "\U0001F469\u200D\U0001F4BB"; // 👩‍💻
        Assert.Equal (4, technologist.EnumerateRunes ().Sum (c => c.GetColumns ()));
        Assert.Equal (2, technologist.GetColumns ());
    }

    // Copilot
    [Fact]
    public void TruncateOrPad_SurrogatePairs_DoesNotThrowOrCorrupt ()
    {
        // TruncateOrPad iterates `char` values and casts each to `Rune`.
        // Surrogate pairs (emoji, CJK supplementary) are two `char`s in UTF-16.
        // Casting an isolated high/low surrogate to Rune throws ArgumentOutOfRangeException.
        const string CELL_VALUE = "\U0001F389Hello"; // 🎉Hello — emoji is a surrogate pair

        // Sanity checks
        Assert.True (char.IsHighSurrogate (CELL_VALUE [0]));
        Assert.True (char.IsLowSurrogate (CELL_VALUE [1]));
        Assert.Equal (7, CELL_VALUE.GetColumns ()); // emoji=2 + Hello=5

        // Call private static TruncateOrPad via reflection with availableHorizontalSpace < string width
        MethodInfo? method = typeof (TableView).GetMethod ("TruncateOrPad", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull (method);

        // availableHorizontalSpace=4 forces the truncation branch (7 >= 4)
        Exception? ex = Record.Exception (() => method.Invoke (null, [CELL_VALUE, CELL_VALUE, 4, null]));

        // Bug: this throws TargetInvocationException wrapping ArgumentOutOfRangeException
        // because (Rune)highSurrogate is invalid
        Assert.Null (ex);

        var result = (string)method.Invoke (null, [CELL_VALUE, CELL_VALUE, 4, null])!;

        // Result must not contain isolated surrogates (paired surrogates in emoji are fine)
        for (var i = 0; i < result.Length; i++)
        {
            if (char.IsHighSurrogate (result [i]))
            {
                Assert.True (i + 1 < result.Length && char.IsLowSurrogate (result [i + 1]), $"Isolated high surrogate at index {i}");
                i++; // skip the low surrogate
            }
            else
            {
                Assert.False (char.IsLowSurrogate (result [i]), $"Isolated low surrogate 0x{(int)result [i]:X4} at index {i}");
            }
        }

        // Result width should not exceed available space
        Assert.True (result.GetColumns () <= 4, $"Truncated result '{result}' exceeds available space");
    }

    // Claude - Opus 4.7
    [Fact]
    public void TruncationIndicator_DefaultIsHorizontalEllipsis ()
    {
        ColumnStyle style = new ();

        Assert.Equal (Glyphs.HorizontalEllipsis.ToString (), style.TruncationIndicator);
    }

    // Claude - Opus 4.7
    // Verifies fix for #5068: TableView appends a truncation indicator (default "…")
    // when cell content exceeds the available column width.
    [Fact]
    public void TruncateOrPad_DefaultIndicator_AppendsEllipsisWhenTruncated ()
    {
        MethodInfo method = typeof (TableView).GetMethod ("TruncateOrPad", BindingFlags.NonPublic | BindingFlags.Static)!;
        ColumnStyle style = new ();

        // "Hello" is 5 cols wide, available space is 5 → truncation branch (5 >= 5).
        // Visible budget is availableHorizontalSpace - 1 = 4 (1 cell reserved for boundary).
        // With 1-col indicator, content budget is 3 → "Hel" + "…" = "Hel…" (4 cols).
        var result = (string)method.Invoke (null, ["Hello", "Hello", 5, style])!;

        Assert.Equal ("Hel…", result);
        Assert.Equal (4, result.GetColumns ());
    }

    // Claude - Opus 4.7
    [Fact]
    public void TruncateOrPad_NullIndicator_SilentlyClips ()
    {
        MethodInfo method = typeof (TableView).GetMethod ("TruncateOrPad", BindingFlags.NonPublic | BindingFlags.Static)!;
        ColumnStyle style = new () { TruncationIndicator = null };

        var result = (string)method.Invoke (null, ["Hello", "Hello", 5, style])!;

        Assert.Equal ("Hell", result);
    }

    // Claude - Opus 4.7
    [Fact]
    public void TruncateOrPad_EmptyIndicator_SilentlyClips ()
    {
        MethodInfo method = typeof (TableView).GetMethod ("TruncateOrPad", BindingFlags.NonPublic | BindingFlags.Static)!;
        ColumnStyle style = new () { TruncationIndicator = string.Empty };

        var result = (string)method.Invoke (null, ["Hello", "Hello", 5, style])!;

        Assert.Equal ("Hell", result);
    }

    // Claude - Opus 4.7
    // When colStyle is null no indicator is configured so existing silent-clip behavior is preserved.
    [Fact]
    public void TruncateOrPad_NullColumnStyle_SilentlyClips ()
    {
        MethodInfo method = typeof (TableView).GetMethod ("TruncateOrPad", BindingFlags.NonPublic | BindingFlags.Static)!;

        var result = (string)method.Invoke (null, ["Hello", "Hello", 5, null])!;

        Assert.Equal ("Hell", result);
    }

    // Claude - Opus 4.7
    [Fact]
    public void TruncateOrPad_CustomMultiColumnIndicator_Appended ()
    {
        MethodInfo method = typeof (TableView).GetMethod ("TruncateOrPad", BindingFlags.NonPublic | BindingFlags.Static)!;
        ColumnStyle style = new () { TruncationIndicator = "..." };

        // availableHorizontalSpace=8, "Hello World" is 11 cols → truncation branch.
        // Visible budget is 7 (8-1). Reserve 3 for "...". Content budget = 4 → "Hell" + "..." = "Hell..." (7 cols).
        var result = (string)method.Invoke (null, ["Hello World", "Hello World", 8, style])!;

        Assert.Equal ("Hell...", result);
        Assert.Equal (7, result.GetColumns ());
    }

    // Claude - Opus 4.7
    // When the indicator is wider than the visible column budget, fall back to silent clipping
    // rather than producing an oversize result.
    [Fact]
    public void TruncateOrPad_IndicatorTooWide_SilentlyClips ()
    {
        MethodInfo method = typeof (TableView).GetMethod ("TruncateOrPad", BindingFlags.NonPublic | BindingFlags.Static)!;
        ColumnStyle style = new () { TruncationIndicator = "..." };

        // availableHorizontalSpace=3 → visible budget is 2 → 3-col indicator does not fit.
        var result = (string)method.Invoke (null, ["Hello", "Hello", 3, style])!;

        Assert.Equal ("He", result);
        Assert.True (result.GetColumns () <= 2);
    }

    // Claude - Opus 4.7
    [Fact]
    public void TruncateOrPad_IndicatorEqualsBudget_SilentlyClips ()
    {
        MethodInfo method = typeof (TableView).GetMethod ("TruncateOrPad", BindingFlags.NonPublic | BindingFlags.Static)!;
        ColumnStyle style = new () { TruncationIndicator = "…" };

        // availableHorizontalSpace=2 → visible budget is 1 → indicator (1 col) would consume the entire
        // budget and leave nothing for content. Falls back to silent clip.
        var result = (string)method.Invoke (null, ["Hello", "Hello", 2, style])!;

        Assert.Equal ("H", result);
    }

    // Claude - Opus 4.7
    // No truncation occurs when content fits, so the indicator should not appear and the value
    // should be padded according to the column style alignment.
    [Fact]
    public void TruncateOrPad_ContentFits_NoIndicatorAdded ()
    {
        MethodInfo method = typeof (TableView).GetMethod ("TruncateOrPad", BindingFlags.NonPublic | BindingFlags.Static)!;
        ColumnStyle style = new ();

        // "Hi" is 2 cols, availableHorizontalSpace=10 → no truncation, padded to 9 cols (10-1 boundary).
        var result = (string)method.Invoke (null, ["Hi", "Hi", 10, style])!;

        Assert.DoesNotContain ('…', result);
        Assert.StartsWith ("Hi", result);
    }

    // Claude - Opus 4.7
    // Indicator must be appended after a complete grapheme cluster and must not split surrogate pairs
    // or combining sequences.
    [Fact]
    public void TruncateOrPad_GraphemeContent_IndicatorAppendedCleanly ()
    {
        MethodInfo method = typeof (TableView).GetMethod ("TruncateOrPad", BindingFlags.NonPublic | BindingFlags.Static)!;
        ColumnStyle style = new ();

        // "🎉Hello" is 7 cols (emoji=2 + Hello=5). availableHorizontalSpace=5 → truncation.
        // Visible budget = 4. Reserve 1 for indicator → content budget = 3.
        // Add 🎉 (w=2, remaining content budget=1), add 'H' (w=1, content budget=0), break.
        // Result = "🎉H" + "…" = "🎉H…" (4 cols).
        var result = (string)method.Invoke (null, ["\U0001F389Hello", "\U0001F389Hello", 5, style])!;

        Assert.Equal ("\U0001F389H…", result);
        Assert.Equal (4, result.GetColumns ());

        // No isolated surrogates
        for (var i = 0; i < result.Length; i++)
        {
            if (char.IsHighSurrogate (result [i]))
            {
                Assert.True (i + 1 < result.Length && char.IsLowSurrogate (result [i + 1]));
                i++;
            }
            else
            {
                Assert.False (char.IsLowSurrogate (result [i]));
            }
        }
    }

    // Claude - Opus 4.5 — regression for issue #5075
    // When ShowVerticalCellLines is false AND a custom ColorGetter is used, the position between cells
    // (the separator column, current.X - 1) was being overdrawn with a space using the row's normal
    // scheme. This punched a 1-cell hole in the cell's custom color. The fix skips the separator draw
    // when the symbol would be the default space and lines are off — the cell padding has already
    // filled that position with the cell's color.
    [Fact]
    public void ShowVerticalCellLines_False_WithCustomColorGetter_PreservesCellColorAtSeparator ()
    {
        IDriver driver = CreateTestDriver (40, 5);

        TableView tableView = new () { Driver = driver };
        tableView.BeginInit ();
        tableView.EndInit ();
        tableView.SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Base);
        tableView.Viewport = new Rectangle (0, 0, 40, 5);

        tableView.Style.ShowHeaders = true;
        tableView.Style.ShowHorizontalHeaderUnderline = false;
        tableView.Style.ShowHorizontalHeaderOverline = false;
        tableView.Style.AlwaysShowHeaders = true;
        tableView.Style.ShowVerticalCellLines = false;
        tableView.Style.ShowVerticalHeaderLines = false;
        tableView.Style.ExpandLastColumn = false;
        tableView.FullRowSelect = false;

        // A custom scheme that is visibly distinct from the row scheme.
        Scheme customScheme = new ()
        {
            Normal = new Attribute (Color.White, Color.Red),
            Focus = new Attribute (Color.Red, Color.White),
            HotNormal = new Attribute (Color.White, Color.Red),
            HotFocus = new Attribute (Color.Red, Color.White),
            Disabled = new Attribute (Color.White, Color.Red),
            Active = new Attribute (Color.White, Color.Red)
        };

        tableView.Style.GetOrCreateColumnStyle (0).ColorGetter = _ => customScheme;
        tableView.Style.GetOrCreateColumnStyle (1).ColorGetter = _ => customScheme;

        DataTable dt = new ();
        dt.Columns.Add ("A");
        dt.Columns.Add ("B");
        dt.Rows.Add ("aa", "bb");
        tableView.Table = new DataTableSource (dt);

        tableView.Layout ();
        tableView.SetClipToScreen ();
        tableView.Draw ();

        // Find the row that contains the data ("aa" then "bb").
        Cell [,] contents = driver.Contents!;
        var dataRow = -1;

        for (var r = 0; r < 5; r++)
        {
            var rowText = string.Empty;

            for (var c = 0; c < 10; c++)
            {
                rowText += contents [r, c].Grapheme;
            }

            if (rowText.Contains ("aa") && rowText.Contains ("bb"))
            {
                dataRow = r;

                break;
            }
        }

        Assert.True (dataRow >= 0, "Expected a rendered data row containing 'aa' and 'bb'");

        // Locate the columns for "aa" and "bb".
        var aaCol = -1;
        var bbCol = -1;

        for (var c = 0; c < 39; c++)
        {
            if (aaCol < 0 && contents [dataRow, c].Grapheme == "a" && contents [dataRow, c + 1].Grapheme == "a")
            {
                aaCol = c;
            }

            if (bbCol < 0 && contents [dataRow, c].Grapheme == "b" && contents [dataRow, c + 1].Grapheme == "b")
            {
                bbCol = c;
            }
        }

        Assert.True (aaCol >= 0 && bbCol > aaCol + 1, $"Expected 'aa' before 'bb'. aaCol={aaCol}, bbCol={bbCol}");

        // The cells "aa" and "bb" must use the custom red background.
        Assert.Equal (customScheme.Normal, contents [dataRow, aaCol].Attribute);
        Assert.Equal (customScheme.Normal, contents [dataRow, bbCol].Attribute);

        // The separator position is the gap between the end of "aa" and the start of "bb".
        // Before the fix, this position was overdrawn with the row scheme attribute.
        // After the fix, the cell padding's custom red attribute remains.
        for (int c = aaCol + 2; c < bbCol; c++)
        {
            Assert.Equal (customScheme.Normal, contents [dataRow, c].Attribute);
        }

        tableView.Dispose ();
    }

    [Fact]
    public void Test_CalculateMaxCellWidth_UsesGraphemeWidth ()
    {
        // setup
        IDriver driver = CreateTestDriver ();
        var family = "\U0001F468\u200D\U0001F469\u200D\U0001F466\u200D\U0001F466"; // 👨‍👩‍👦‍👦

        var tableView = new TableView { Driver = driver };
        tableView.BeginInit ();
        tableView.EndInit ();
        tableView.SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Accent);
        tableView.Viewport = new Rectangle (0, 0, 25, 5);
        tableView.Style.ShowHorizontalHeaderUnderline = true;
        tableView.Style.ShowHorizontalHeaderOverline = false;
        tableView.Style.AlwaysShowHeaders = true;

        var dt = new DataTable ();
        dt.Columns.Add ("A");
        dt.Columns.Add ("B");
        dt.Rows.Add (family, "ok");
        tableView.Table = new DataTableSource (dt);

        // execute
        tableView.Layout ();
        tableView.SetClipToScreen ();
        tableView.Draw ();

        // verify
        var actual = driver.ToString ();
        string [] lines = actual.Replace ("\r\n", "\n").Split ('\n');
        string headerRow = lines.First (l => l.Contains ('A') && l.Contains ('B'));
        int separatorIndex = headerRow.IndexOf ('│', 1);
        int separatorColumn = headerRow [..separatorIndex].GetColumns ();

        Assert.True (separatorColumn <= 5,
                     $"Column A should be narrow (grapheme width 2), but separator at column {
                         separatorColumn
                     } suggests over-sized column. Header: '{
                         headerRow
                     }'");
    }

    // Copilot
    // Verifies fix for #5072: a wide column should be allowed to grow based on its content so that
    // the table can establish horizontal overflow and keep later columns reachable via scrolling.
    [Fact]
    public void Calculate_WideColumn_DoesNotStarveLaterColumns ()
    {
        DataTable dt = new ();
        dt.Columns.Add ("Description");
        dt.Columns.Add ("Status");
        dt.Columns.Add ("Owner");
        dt.Rows.Add (new string ('x', 200), "ok", "me");

        using TableView tableView = new ()
        {
            Table = new DataTableSource (dt),
            Viewport = new Rectangle (0, 0, 40, 5)
        };
        tableView.BeginInit ();
        tableView.EndInit ();
        tableView.RefreshContentSize ();

        TableView.ColumnToRender [] columns = GetColumnsToRender (tableView);

        Assert.Equal (3, columns.Length);

        // The wide column should be allowed to extend beyond the viewport so the table can scroll horizontally.
        TableView.ColumnToRender description = columns [0];
        TableView.ColumnToRender status = columns [1];
        TableView.ColumnToRender owner = columns [2];

        Assert.True (description.X >= 0);
        Assert.True (status.X > description.X);
        Assert.True (owner.X > status.X);
        Assert.True (description.X + description.Width - 1 >= tableView.Viewport.Width,
                     $"Description right edge {description.X + description.Width - 1} should exceed viewport {tableView.Viewport.Width}");

        // Status and Owner each must have at least header-width room (excluding separator)
        Assert.True (status.Width - 1 >= "Status".Length, $"Status got width {status.Width - 1}");
        Assert.True (owner.Width - 1 >= "Owner".Length, $"Owner got width {owner.Width - 1}");
    }

    // Copilot
    // When the viewport is too small to fit even minimum widths for every column, layout falls back
    // to the prior left-to-right packing (columns may extend past the viewport, accessible via
    // horizontal scrolling).
    [Fact]
    public void Calculate_NarrowViewport_StillProducesLayout ()
    {
        DataTable dt = new ();
        dt.Columns.Add ("Description");
        dt.Columns.Add ("Status");
        dt.Columns.Add ("Owner");
        dt.Rows.Add (new string ('x', 50), "ok", "me");

        using TableView tableView = new ()
        {
            Table = new DataTableSource (dt),
            Viewport = new Rectangle (0, 0, 10, 5)
        };
        tableView.BeginInit ();
        tableView.EndInit ();
        tableView.RefreshContentSize ();

        TableView.ColumnToRender [] columns = GetColumnsToRender (tableView);

        Assert.Equal (3, columns.Length);

        // Each column should have a positive width
        Assert.All (columns, c => Assert.True (c.Width > 0, $"Column {c.Column} got non-positive width {c.Width}"));
    }

    // Copilot
    // Single-column tables should still expand to fill the viewport when ExpandLastColumn is true.
    [Fact]
    public void Calculate_SingleColumn_StillExpandsLastColumn ()
    {
        DataTable dt = new ();
        dt.Columns.Add ("Only");
        dt.Rows.Add ("hi");

        using TableView tableView = new ()
        {
            Table = new DataTableSource (dt),
            Viewport = new Rectangle (0, 0, 30, 5)
        };
        tableView.BeginInit ();
        tableView.EndInit ();
        tableView.RefreshContentSize ();

        TableView.ColumnToRender [] columns = GetColumnsToRender (tableView);

        Assert.Single (columns);
        Assert.True (columns [0].Width >= tableView.Viewport.Width - 2,
                     $"Single column width {columns [0].Width} should fill viewport {tableView.Viewport.Width}");
    }

    private static TableView.ColumnToRender [] GetColumnsToRender (TableView tableView)
    {
        FieldInfo? field = typeof (TableView).GetField ("_columnsToRenderCache", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull (field);

        return (TableView.ColumnToRender []?)field!.GetValue (tableView) ?? [];
    }

    // Copilot
    [Fact]
    public void HeaderColorGetter_AppliesCustomSchemeToColumnHeader ()
    {
        IDriver driver = CreateTestDriver (40, 5);

        TableView tableView = new () { Driver = driver };
        tableView.BeginInit ();
        tableView.EndInit ();
        tableView.SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Base);
        tableView.Viewport = new Rectangle (0, 0, 40, 5);

        tableView.Style.ShowHeaders = true;
        tableView.Style.ShowHorizontalHeaderUnderline = false;
        tableView.Style.ShowHorizontalHeaderOverline = false;
        tableView.Style.AlwaysShowHeaders = true;
        tableView.Style.ShowVerticalCellLines = true;
        tableView.Style.ShowVerticalHeaderLines = true;

        Scheme headerScheme = new ()
        {
            Normal = new Attribute (Color.BrightYellow, Color.DarkGray),
            Focus = new Attribute (Color.BrightYellow, Color.DarkGray)
        };

        // Only column 0 gets custom header color
        tableView.Style.GetOrCreateColumnStyle (0).HeaderColorGetter = _ => headerScheme;

        DataTable dt = new ();
        dt.Columns.Add ("Name");
        dt.Columns.Add ("Value");
        dt.Rows.Add ("test", "123");
        tableView.Table = new DataTableSource (dt);

        tableView.Layout ();
        tableView.SetClipToScreen ();
        tableView.Draw ();

        Cell [,] contents = driver.Contents!;

        // Find row 0 which has headers (since overline is off, headers are on line 0)
        // Look for 'N' from "Name" header
        var nameCol = -1;
        var valueCol = -1;

        for (var c = 0; c < 40; c++)
        {
            if (nameCol < 0 && contents [0, c].Grapheme == "N")
            {
                nameCol = c;
            }

            if (valueCol < 0 && contents [0, c].Grapheme == "V")
            {
                valueCol = c;
            }
        }

        Assert.True (nameCol >= 0, "Expected to find 'N' from 'Name' header");
        Assert.True (valueCol >= 0, "Expected to find 'V' from 'Value' header");

        // Column 0 header should use the custom scheme
        Assert.Equal (headerScheme.Normal, contents [0, nameCol].Attribute);

        // Column 1 header should NOT use the custom scheme (no HeaderColorGetter set)
        Assert.NotEqual (headerScheme.Normal, contents [0, valueCol].Attribute);

        tableView.Dispose ();
    }

    // Copilot
    [Fact]
    public void TableStyle_HeaderScheme_AppliesBaseSchemeToAllHeaders ()
    {
        IDriver driver = CreateTestDriver (40, 5);

        TableView tableView = new () { Driver = driver };
        tableView.BeginInit ();
        tableView.EndInit ();
        tableView.SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Base);
        tableView.Viewport = new Rectangle (0, 0, 40, 5);

        tableView.Style.ShowHeaders = true;
        tableView.Style.ShowHorizontalHeaderUnderline = false;
        tableView.Style.ShowHorizontalHeaderOverline = false;
        tableView.Style.AlwaysShowHeaders = true;
        tableView.Style.ShowVerticalCellLines = true;
        tableView.Style.ShowVerticalHeaderLines = true;

        Scheme globalHeaderScheme = new ()
        {
            Normal = new Attribute (Color.Green, Color.Black),
            Focus = new Attribute (Color.Green, Color.Black)
        };

        tableView.Style.HeaderScheme = globalHeaderScheme;

        DataTable dt = new ();
        dt.Columns.Add ("Name");
        dt.Columns.Add ("Value");
        dt.Rows.Add ("test", "123");
        tableView.Table = new DataTableSource (dt);

        tableView.Layout ();
        tableView.SetClipToScreen ();
        tableView.Draw ();

        Cell [,] contents = driver.Contents!;

        // Find header characters
        var nameCol = -1;
        var valueCol = -1;

        for (var c = 0; c < 40; c++)
        {
            if (nameCol < 0 && contents [0, c].Grapheme == "N")
            {
                nameCol = c;
            }

            if (valueCol < 0 && contents [0, c].Grapheme == "V")
            {
                valueCol = c;
            }
        }

        Assert.True (nameCol >= 0, "Expected to find 'N' from 'Name' header");
        Assert.True (valueCol >= 0, "Expected to find 'V' from 'Value' header");

        // Both headers should use the global header scheme
        Assert.Equal (globalHeaderScheme.Normal, contents [0, nameCol].Attribute);
        Assert.Equal (globalHeaderScheme.Normal, contents [0, valueCol].Attribute);

        tableView.Dispose ();
    }

    // Copilot
    [Fact]
    public void HeaderSeparatorLines_DoNotUseFocusAttribute_WhenTableHasFocus ()
    {
        IDriver driver = CreateTestDriver (40, 5);

        TableView tableView = new () { Driver = driver };
        tableView.BeginInit ();
        tableView.EndInit ();
        tableView.SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Base);
        tableView.Viewport = new Rectangle (0, 0, 40, 5);

        tableView.Style.ShowHeaders = true;
        tableView.Style.ShowHorizontalHeaderUnderline = false;
        tableView.Style.ShowHorizontalHeaderOverline = false;
        tableView.Style.AlwaysShowHeaders = true;
        tableView.Style.ShowVerticalCellLines = true;
        tableView.Style.ShowVerticalHeaderLines = true;

        DataTable dt = new ();
        dt.Columns.Add ("Name");
        dt.Columns.Add ("Value");
        dt.Rows.Add ("test", "123");
        tableView.Table = new DataTableSource (dt);

        // Simulate focus so headers render with Focus attribute
        tableView.HasFocus = true;

        tableView.Layout ();
        tableView.SetClipToScreen ();
        tableView.Draw ();

        Cell [,] contents = driver.Contents!;

        // Find the separator column (│) between the two headers on row 0
        var separatorCol = -1;

        for (var c = 1; c < 39; c++)
        {
            if (contents [0, c].Grapheme == Glyphs.VLine.ToString ())
            {
                separatorCol = c;

                break;
            }
        }

        Assert.True (separatorCol >= 0, "Expected to find a vertical separator '│' between headers");

        // The separator should use Normal attribute, not Focus
        Scheme viewScheme = tableView.GetScheme ();
        Attribute normalAttr = viewScheme.Normal;
        Attribute focusAttr = viewScheme.Focus;

        Attribute? separatorAttribute = contents [0, separatorCol].Attribute;

        // The separator must NOT use focus colors
        Assert.NotEqual (focusAttr, separatorAttribute);

        // The separator should use normal attribute
        Assert.Equal (normalAttr, separatorAttribute);

        // Also verify that actual header text DOES use focus attribute
        var nameCol = -1;

        for (var c = 0; c < 40; c++)
        {
            if (contents [0, c].Grapheme == "N")
            {
                nameCol = c;

                break;
            }
        }

        Assert.True (nameCol >= 0, "Expected to find 'N' from 'Name' header");
        Assert.Equal (focusAttr, contents [0, nameCol].Attribute);

        tableView.Dispose ();
    }

    // Copilot
    [Fact]
    public void ShowVerticalCellLines_CanHideOuterBorders_AndPreserveInnerSeparators ()
    {
        IDriver driver = CreateTestDriver (20, 5);

        TableView tableView = new () { Driver = driver };
        tableView.BeginInit ();
        tableView.EndInit ();
        tableView.SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Base);
        tableView.Viewport = new Rectangle (0, 0, 20, 5);

        tableView.Style.ShowHeaders = true;
        tableView.Style.ShowHorizontalHeaderUnderline = false;
        tableView.Style.ShowHorizontalHeaderOverline = false;
        tableView.Style.AlwaysShowHeaders = true;
        tableView.Style.ShowVerticalCellLines = true;
        tableView.Style.ShowVerticalHeaderLines = true;
        tableView.Style.ExpandLastColumn = false;
        tableView.Style.ShowVerticalCellLineForFirstColumn = false;
        tableView.Style.ShowVerticalCellLineForLastColumn = false;

        DataTable dt = new ();
        dt.Columns.Add ("Name");
        dt.Columns.Add ("Value");
        dt.Rows.Add ("A", "B");
        tableView.Table = new DataTableSource (dt);

        tableView.Layout ();
        tableView.SetClipToScreen ();
        tableView.Draw ();

        Cell [,] contents = driver.Contents!;
        TableView.ColumnToRender [] columns = GetColumnsToRender (tableView);

        Assert.Equal (2, columns.Length);

        int leftBorderCol = 0;
        int innerSeparatorCol = columns [1].X - 1;
        int rightBorderCol = columns [1].X + columns [1].Width - 1;

        Assert.NotEqual (Glyphs.VLine.ToString (), contents [0, leftBorderCol].Grapheme);
        Assert.Equal (Glyphs.VLine.ToString (), contents [0, innerSeparatorCol].Grapheme);
        Assert.NotEqual (Glyphs.VLine.ToString (), contents [0, rightBorderCol].Grapheme);

        Assert.NotEqual (Glyphs.VLine.ToString (), contents [1, leftBorderCol].Grapheme);
        Assert.Equal (Glyphs.VLine.ToString (), contents [1, innerSeparatorCol].Grapheme);
        Assert.NotEqual (Glyphs.VLine.ToString (), contents [1, rightBorderCol].Grapheme);

        tableView.Dispose ();
    }

    // Claude - Opus 4.7
    // Regression test for https://github.com/tui-cs/Terminal.Gui/issues/5126
    // Clicking outside the row area of a TableView (e.g. below the last row) must
    // not raise the Activating event.
    [Fact]
    public void Click_OutsideRows_DoesNotRaise_Activating ()
    {
        TableView tableView = new () { Viewport = new Rectangle (0, 0, 25, 10) };
        tableView.Table = BuildTable (2, 2);

        var activatingFired = 0;
        tableView.Activating += (_, _) => activatingFired++;

        // Construct a left-click well below the last data row. With a 2-row table,
        // y=9 is past the rows so ScreenToCell returns null.
        Mouse mouseEvent = new ()
        {
            Position = new Point (1, 9),
            Flags = MouseFlags.LeftButtonClicked
        };

        MouseBinding binding = new ([Command.Activate], mouseEvent);

        tableView.InvokeCommand (Command.Activate, binding);

        Assert.Equal (0, activatingFired);

        tableView.Dispose ();
    }

    // Copilot
    [Fact]
    public void CalculateContentSize_HidingOuterVerticalCellLines_ReclaimsBothOuterColumns ()
    {
        DataTable dt = new ();
        dt.Columns.Add ("Name");
        dt.Columns.Add ("Value");
        dt.Rows.Add ("A", "B");

        using TableView tableViewWithOuterBorders = new ()
        {
            Table = new DataTableSource (dt),
            Viewport = new Rectangle (0, 0, 20, 5)
        };
        tableViewWithOuterBorders.BeginInit ();
        tableViewWithOuterBorders.EndInit ();
        tableViewWithOuterBorders.Style.ShowHeaders = true;
        tableViewWithOuterBorders.Style.ShowVerticalCellLines = true;
        tableViewWithOuterBorders.Style.ShowVerticalHeaderLines = true;
        tableViewWithOuterBorders.Style.ExpandLastColumn = false;
        tableViewWithOuterBorders.RefreshContentSize ();

        using TableView tableViewWithoutOuterBorders = new ()
        {
            Table = new DataTableSource (dt),
            Viewport = new Rectangle (0, 0, 20, 5)
        };
        tableViewWithoutOuterBorders.BeginInit ();
        tableViewWithoutOuterBorders.EndInit ();
        tableViewWithoutOuterBorders.Style.ShowHeaders = true;
        tableViewWithoutOuterBorders.Style.ShowVerticalCellLines = true;
        tableViewWithoutOuterBorders.Style.ShowVerticalHeaderLines = true;
        tableViewWithoutOuterBorders.Style.ExpandLastColumn = false;
        tableViewWithoutOuterBorders.Style.ShowVerticalCellLineForFirstColumn = false;
        tableViewWithoutOuterBorders.Style.ShowVerticalCellLineForLastColumn = false;
        tableViewWithoutOuterBorders.RefreshContentSize ();

        Assert.Equal (tableViewWithOuterBorders.GetContentSize ().Width - 2, tableViewWithoutOuterBorders.GetContentSize ().Width);
    }

    // Claude - Opus 4.7
    // Companion to Click_OutsideRows_DoesNotRaise_Activating: clicking on a real
    // cell still raises Activating as before.
    [Fact]
    public void Click_OnRow_Raises_Activating ()
    {
        TableView tableView = new () { Viewport = new Rectangle (0, 0, 25, 10) };
        tableView.Table = BuildTable (2, 2);

        var activatingFired = 0;
        tableView.Activating += (_, _) => activatingFired++;

        // y=3 lands on the first data row. The default header style consumes 3 lines
        // (overline + header text + underline), so y=0..2 is header and y=3 is the
        // first data row.
        Mouse mouseEvent = new ()
        {
            Position = new Point (1, 3),
            Flags = MouseFlags.LeftButtonClicked
        };

        MouseBinding binding = new ([Command.Activate], mouseEvent);

        tableView.InvokeCommand (Command.Activate, binding);

        Assert.Equal (1, activatingFired);

        tableView.Dispose ();
    }

    // Claude - Opus 4.7
    // Click in horizontal whitespace (right of the last rendered column) must not
    // raise Activating. Disable ExpandLastColumn so the last column doesn't fill
    // the viewport and there is real whitespace to the right.
    [Fact]
    public void Click_RightOfLastColumn_DoesNotRaise_Activating ()
    {
        TableView tableView = new () { Viewport = new Rectangle (0, 0, 40, 10) };
        tableView.Style.ExpandLastColumn = false;
        tableView.Table = BuildTable (2, 2);

        var activatingFired = 0;
        tableView.Activating += (_, _) => activatingFired++;

        // BuildTable column "Col0" / "Col1" rendered with values like "R0C0" gives
        // narrow columns. With ExpandLastColumn=false and viewport width 40, x=35 is
        // well past the right edge of the last rendered column.
        Mouse mouseEvent = new ()
        {
            Position = new Point (35, 3),
            Flags = MouseFlags.LeftButtonClicked
        };

        MouseBinding binding = new ([Command.Activate], mouseEvent);

        tableView.InvokeCommand (Command.Activate, binding);

        Assert.Equal (0, activatingFired);

        tableView.Dispose ();
    }

    // Claude - Opus 4.7
    // Click on the column header area must not raise Activating — the click
    // doesn't correspond to a data cell.
    [Fact]
    public void Click_OnHeader_DoesNotRaise_Activating ()
    {
        TableView tableView = new () { Viewport = new Rectangle (0, 0, 25, 10) };
        tableView.Table = BuildTable (2, 2);

        var activatingFired = 0;
        tableView.Activating += (_, _) => activatingFired++;

        // The header occupies y=0..2 (overline + header text + underline). y=1 is
        // the header text line.
        Mouse mouseEvent = new ()
        {
            Position = new Point (1, 1),
            Flags = MouseFlags.LeftButtonClicked
        };

        MouseBinding binding = new ([Command.Activate], mouseEvent);

        tableView.InvokeCommand (Command.Activate, binding);

        Assert.Equal (0, activatingFired);

        tableView.Dispose ();
    }

    // Claude - Opus 4.7
    // Click on a TableView with no Table set must not raise Activating.
    [Fact]
    public void Click_EmptyTable_DoesNotRaise_Activating ()
    {
        TableView tableView = new () { Viewport = new Rectangle (0, 0, 25, 10) };

        // Intentionally no Table assigned.
        var activatingFired = 0;
        tableView.Activating += (_, _) => activatingFired++;

        Mouse mouseEvent = new ()
        {
            Position = new Point (5, 5),
            Flags = MouseFlags.LeftButtonClicked
        };

        MouseBinding binding = new ([Command.Activate], mouseEvent);

        tableView.InvokeCommand (Command.Activate, binding);

        Assert.Equal (0, activatingFired);

        tableView.Dispose ();
    }

    // Claude - Opus 4.7
    // Click on a Table that has zero rows (header rendered but no data) must not
    // raise Activating regardless of where in the data area the user clicks.
    [Fact]
    public void Click_TableWithZeroRows_DoesNotRaise_Activating ()
    {
        TableView tableView = new () { Viewport = new Rectangle (0, 0, 25, 10) };
        tableView.Table = BuildTable (2, 0);

        var activatingFired = 0;
        tableView.Activating += (_, _) => activatingFired++;

        // y=3 is just past the header, in what would be the first data row if any
        // existed. With zero rows, ScreenToCell must return null.
        Mouse mouseEvent = new ()
        {
            Position = new Point (1, 3),
            Flags = MouseFlags.LeftButtonClicked
        };

        MouseBinding binding = new ([Command.Activate], mouseEvent);

        tableView.InvokeCommand (Command.Activate, binding);

        Assert.Equal (0, activatingFired);

        tableView.Dispose ();
    }
}
