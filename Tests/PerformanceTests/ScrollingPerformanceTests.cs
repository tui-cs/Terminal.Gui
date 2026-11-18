// Copilot

using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Text;

namespace PerformanceTests;

/// <summary>
///     Stopwatch-based smoke tests that catch catastrophic rendering regressions.
///     Thresholds are intentionally generous (~50–100× typical) so these never flake on slow CI runners
///     but still catch accidental O(n²) regressions where a single viewport draw inadvertently
///     iterates over the entire document instead of just the visible rows.
/// </summary>
/// <remarks>
///     Each test measures the cost of a SINGLE viewport draw on a large document so that an
///     O(document-size) regression is immediately detectable without needing a full scroll loop.
///     These are Layer 1 of the two-layer CI performance gate.
///     Layer 2 (BenchmarkDotNet baseline comparison) lives in Tests/Benchmarks/.
///     This project runs exclusively on Linux via the perf-gate CI workflow.
/// </remarks>
[Trait ("Category", "Performance")]
public class ScrollingPerformanceTests : TestDriverBase
{
    // ──────────────────────────────────────────────────────────────────────────
    // ListView smoke tests
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    ///     Builds a 100 000-item <see cref="ListView"/> and renders a single viewport.
    ///     Asserts layout + one full draw completes under a generous threshold.
    /// </summary>
    [Fact]
    public void ListView_LayoutAndDraw_100K_Items_UnderThreshold ()
    {
        const int ITEM_COUNT = 100_000;
        const int SCREEN_WIDTH = 80;
        const int SCREEN_HEIGHT = 30;

        IDriver driver = CreateTestDriver (SCREEN_WIDTH, SCREEN_HEIGHT);

        ListView listView = new ()
        {
            X = 0,
            Y = 0,
            Width = SCREEN_WIDTH,
            Height = SCREEN_HEIGHT,
            Driver = driver
        };
        listView.SetSource (new ObservableCollection<string> (BuildListItems (ITEM_COUNT)));
        listView.BeginInit ();
        listView.EndInit ();
        listView.Layout ();

        // Warm up.
        listView.SetNeedsDraw ();
        listView.Draw ();

        var sw = Stopwatch.StartNew ();
        listView.SetNeedsDraw ();
        listView.Draw ();
        sw.Stop ();

        // 300 ms is ~150× what a single viewport draw takes on a typical machine.
        Assert.True (sw.Elapsed < TimeSpan.FromMilliseconds (300),
                     $"ListView layout+draw ({ITEM_COUNT} items) took {sw.Elapsed.TotalMilliseconds:F0} ms, expected < 300 ms");
    }

    /// <summary>
    ///     Scrolls a 100 000-item <see cref="ListView"/> to the mid-point and measures the cost of
    ///     a single redraw.  Detects O(total-items) regressions in the per-draw path.
    /// </summary>
    [Fact]
    public void ListView_SingleViewportDraw_Mid_100K_Items_UnderThreshold ()
    {
        const int ITEM_COUNT = 100_000;
        const int SCREEN_WIDTH = 80;
        const int SCREEN_HEIGHT = 30;

        IDriver driver = CreateTestDriver (SCREEN_WIDTH, SCREEN_HEIGHT);

        ListView listView = new ()
        {
            X = 0,
            Y = 0,
            Width = SCREEN_WIDTH,
            Height = SCREEN_HEIGHT,
            Driver = driver
        };
        listView.SetSource (new ObservableCollection<string> (BuildListItems (ITEM_COUNT)));
        listView.BeginInit ();
        listView.EndInit ();
        listView.Layout ();

        // Warm up.
        listView.SetNeedsDraw ();
        listView.Draw ();

        // Scroll to the mid-point of the list.
        listView.Viewport = listView.Viewport with { Y = ITEM_COUNT / 2 };

        var sw = Stopwatch.StartNew ();
        listView.SetNeedsDraw ();
        listView.Draw ();
        sw.Stop ();

        // 300 ms threshold. A genuine O(100 000) regression would scan all items and take >> 1 s.
        Assert.True (sw.Elapsed < TimeSpan.FromMilliseconds (300),
                     $"ListView mid-doc viewport draw ({ITEM_COUNT} items) took {sw.Elapsed.TotalMilliseconds:F0} ms, expected < 300 ms");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // TableView smoke tests
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    ///     Builds a 10 000-row <see cref="TableView"/> and renders a single viewport.
    ///     Asserts layout + one full draw completes under a generous threshold.
    /// </summary>
    [Fact]
    public void TableView_LayoutAndDraw_10K_Rows_UnderThreshold ()
    {
        const int ROW_COUNT = 10_000;
        const int COL_COUNT = 10;
        const int SCREEN_WIDTH = 120;
        const int SCREEN_HEIGHT = 30;

        IDriver driver = CreateTestDriver (SCREEN_WIDTH, SCREEN_HEIGHT);

        TableView tableView = new (new DataTableSource (BuildDataTable (ROW_COUNT, COL_COUNT)))
        {
            X = 0,
            Y = 0,
            Width = SCREEN_WIDTH,
            Height = SCREEN_HEIGHT,
            Driver = driver
        };
        tableView.BeginInit ();
        tableView.EndInit ();
        tableView.Layout ();

        // Warm up.
        tableView.SetNeedsDraw ();
        tableView.Draw ();

        var sw = Stopwatch.StartNew ();
        tableView.SetNeedsDraw ();
        tableView.Draw ();
        sw.Stop ();

        // 200 ms is ~50× what a single viewport draw takes on a typical machine.
        Assert.True (sw.Elapsed < TimeSpan.FromMilliseconds (200),
                     $"TableView layout+draw ({ROW_COUNT} rows) took {sw.Elapsed.TotalMilliseconds:F0} ms, expected < 200 ms");
    }

    /// <summary>
    ///     Scrolls a <see cref="TableView"/> to the mid-point of a 10 000-row table and measures
    ///     the cost of a single redraw from that offset.
    ///     Detects O(total-rows) regressions in the per-draw path.
    /// </summary>
    [Fact]
    public void TableView_SingleViewportDraw_Mid_10K_Rows_UnderThreshold ()
    {
        const int ROW_COUNT = 10_000;
        const int COL_COUNT = 10;
        const int SCREEN_WIDTH = 120;
        const int SCREEN_HEIGHT = 30;

        IDriver driver = CreateTestDriver (SCREEN_WIDTH, SCREEN_HEIGHT);

        TableView tableView = new (new DataTableSource (BuildDataTable (ROW_COUNT, COL_COUNT)))
        {
            X = 0,
            Y = 0,
            Width = SCREEN_WIDTH,
            Height = SCREEN_HEIGHT,
            Driver = driver
        };
        tableView.BeginInit ();
        tableView.EndInit ();
        tableView.Layout ();

        // Warm up.
        tableView.SetNeedsDraw ();
        tableView.Draw ();

        // Scroll to mid-document.
        tableView.RowOffset = ROW_COUNT / 2;

        var sw = Stopwatch.StartNew ();
        tableView.SetNeedsDraw ();
        tableView.Draw ();
        sw.Stop ();

        // 200 ms threshold — an O(total-rows) regression would scan 10 000 rows.
        Assert.True (sw.Elapsed < TimeSpan.FromMilliseconds (200),
                     $"TableView single viewport draw at mid ({ROW_COUNT} rows) took {sw.Elapsed.TotalMilliseconds:F0} ms, expected < 200 ms");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // TextView smoke tests
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    ///     Builds a 1 000-line <see cref="TextView"/> and measures the cost of a single viewport
    ///     draw after scrolling to the middle of the document.
    ///     An O(document-size) regression in the rendering path would exceed the threshold even
    ///     on a slow CI runner.
    /// </summary>
    [Fact]
    public void TextView_SingleViewportDraw_1K_Lines_UnderThreshold ()
    {
        const int THRESHOLD_MS = 1000;
        const int LINE_COUNT = 1_000;
        const int SCREEN_WIDTH = 80;
        const int SCREEN_HEIGHT = 25;

        IDriver driver = CreateTestDriver ();

        TextView tv = new ()
        {
            X = 0,
            Y = 0,
            Width = SCREEN_WIDTH,
            Height = SCREEN_HEIGHT,
            Text = BuildTextViewContent (LINE_COUNT),
            ReadOnly = true,
            WordWrap = false,
            Driver = driver
        };
        tv.BeginInit ();
        tv.EndInit ();
        tv.Layout ();

        // Warm up: prime JIT and layout caches.
        tv.SetNeedsDraw ();
        tv.Draw ();

        // Scroll to the middle of the document.
        tv.Viewport = tv.Viewport with { Y = LINE_COUNT / 2 };

        var sw = Stopwatch.StartNew ();
        tv.SetNeedsDraw ();
        tv.Draw ();
        sw.Stop ();

        // 1000 ms is generous even in debug/slow-CI mode. An O(lineCount) regression
        // scanning 1 000 lines in the draw path would take at least 5–10× longer.
        Assert.True (sw.Elapsed < TimeSpan.FromMilliseconds (THRESHOLD_MS),
                     $"TextView single viewport draw ({LINE_COUNT} lines, mid-doc) took {sw.Elapsed.TotalMilliseconds:F0} ms, expected < {THRESHOLD_MS}ms");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static DataTable BuildDataTable (int rows, int cols)
    {
        DataTable dt = new ();

        for (var colIndex = 0; colIndex < cols; colIndex++)
        {
            dt.Columns.Add ($"Col{colIndex}", typeof (string));
        }

        for (var rowIndex = 0; rowIndex < rows; rowIndex++)
        {
            var row = new object [cols];

            for (var colIndex = 0; colIndex < cols; colIndex++)
            {
                row [colIndex] = $"R{rowIndex}C{colIndex}";
            }

            dt.Rows.Add (row);
        }

        return dt;
    }

    private static List<string> BuildListItems (int count)
    {
        List<string> items = new (count);

        for (var itemIndex = 0; itemIndex < count; itemIndex++)
        {
            items.Add ($"Item {itemIndex,6}: value = {itemIndex * 17 % 100:D3}");
        }

        return items;
    }

    private static string BuildTextViewContent (int lineCount)
    {
        StringBuilder sb = new (lineCount * 85);

        for (var lineIndex = 0; lineIndex < lineCount; lineIndex++)
        {
            sb.AppendLine ($"Line {lineIndex,6}: The quick brown fox jumps over the lazy dog. Extra padding {lineIndex % 10}.");
        }

        return sb.ToString ();
    }
}
