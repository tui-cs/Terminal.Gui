using System.Data;
using System.Drawing;
using BenchmarkDotNet.Attributes;
using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using Terminal.Gui.Testing;
using Terminal.Gui.Views;

namespace Terminal.Gui.Benchmarks.Scrolling;

/// <summary>
///     Measures end-to-end scrolling performance for <see cref="TableView"/>.
///     Covers cell rendering, column alignment, and header drawing at varying row counts.
/// </summary>
/// <remarks>
///     <para>
///         Run:
///         <code>dotnet run --project Tests/Benchmarks -c Release -- --filter '*TableViewScroll*'</code>
///     </para>
/// </remarks>
[MemoryDiagnoser]
[BenchmarkCategory ("Scrolling", "TableView")]
public class TableViewScrollBenchmark
{
    private const int COLUMN_COUNT = 10;
    private const int SCREEN_HEIGHT = 25;
    private const int SCREEN_WIDTH = 120;

    private IApplication _app = null!;
    private IInputInjector _injector = null!;
    private Runnable _runnable = null!;
    private SessionToken? _session;
    private TableView _tableView = null!;

    /// <summary>Disposes the application after all iterations.</summary>
    [GlobalCleanup]
    public void Cleanup ()
    {
        if (_session is { })
        {
            _app.End (_session);
        }

        _app.Dispose ();
    }

    /// <summary>
    ///     Positions the selected row at the last visible data row so the next down-arrow or page-down scrolls.
    /// </summary>
    [IterationSetup (Targets = [nameof (ScrollDown_OneStep), nameof (PageDown_OneStep)])]
    public void IterationSetup_ScrollDown ()
    {
        // TableView reserves row 0 for the header; data rows start at display row 1.
        // Place selection at the last visible data row so the next scroll action triggers rendering.
        int visibleDataRows = SCREEN_HEIGHT - 1; // subtract header row
        _tableView.RowOffset = 0;
        _tableView.Value = new TableSelection (new Point (0, Math.Min (visibleDataRows - 1, Rows - 1)));
        _tableView.SetNeedsDraw ();
    }

    /// <summary>
    ///     Positions the selection at the last fully-visible column so the next right-arrow triggers horizontal scrolling.
    /// </summary>
    [IterationSetup (Targets = [nameof (ScrollRight_OneStep)])]
    public void IterationSetup_ScrollRight ()
    {
        // With wide cell data (16+ chars per column) and a 120-cell viewport,
        // only ~7 columns are visible. Position at the last visible column so
        // CursorRight forces a horizontal scroll to reveal the next off-screen column.
        int lastVisibleCol = Math.Min (SCREEN_WIDTH / 16, COLUMN_COUNT - 2);
        _tableView.RowOffset = 0;
        _tableView.Value = new TableSelection (new Point (lastVisibleCol, 0));
        _tableView.SetNeedsDraw ();
    }

    /// <summary>
    ///     Positions the selected row at the first visible data row with a non-zero offset so the next up-arrow scrolls.
    /// </summary>
    [IterationSetup (Targets = [nameof (ScrollUp_OneStep)])]
    public void IterationSetup_ScrollUp ()
    {
        // Place selection at the first visible data row with RowOffset mid-table so up-arrow triggers a scroll.
        int midRow = Rows / 2;
        _tableView.RowOffset = midRow;
        _tableView.Value = new TableSelection (new Point (0, midRow));
        _tableView.SetNeedsDraw ();
    }

    /// <summary>
    ///     Injects a single <see cref="Key.PageDown"/> keystroke and redraws.
    /// </summary>
    [Benchmark]
    public void PageDown_OneStep ()
    {
        _injector.InjectKey (Key.PageDown);
        _app.LayoutAndDraw ();
    }

    /// <summary>Total number of data rows loaded into the <see cref="TableView"/>.</summary>
    [Params (100, 1_000)]
    public int Rows { get; set; }

    /// <summary>
    ///     Injects a single <see cref="Key.CursorDown"/> keystroke and redraws.
    ///     With the selection at the viewport boundary this triggers a row scroll.
    /// </summary>
    [Benchmark (Baseline = true)]
    public void ScrollDown_OneStep ()
    {
        _injector.InjectKey (Key.CursorDown);
        _app.LayoutAndDraw ();
    }

    /// <summary>
    ///     Injects a single <see cref="Key.CursorRight"/> keystroke and redraws.
    ///     With wide cell data and the selection at the last visible column, this triggers a horizontal scroll.
    /// </summary>
    [Benchmark]
    public void ScrollRight_OneStep ()
    {
        _injector.InjectKey (Key.CursorRight);
        _app.LayoutAndDraw ();
    }

    /// <summary>
    ///     Injects a single <see cref="Key.CursorUp"/> keystroke and redraws.
    /// </summary>
    [Benchmark]
    public void ScrollUp_OneStep ()
    {
        _injector.InjectKey (Key.CursorUp);
        _app.LayoutAndDraw ();
    }

    /// <summary>Creates the application, builds the data table, and warms up the view.</summary>
    [GlobalSetup]
    public void Setup ()
    {
        _app = Application.Create ();
        _app.Init (DriverRegistry.Names.ANSI);
        _app.Driver!.SetScreenSize (SCREEN_WIDTH, SCREEN_HEIGHT);

        _runnable = new Runnable { Width = SCREEN_WIDTH, Height = SCREEN_HEIGHT };
        _session = _app.Begin (_runnable);

        DataTable dt = BuildDataTable (Rows, COLUMN_COUNT);
        _tableView = new TableView (new DataTableSource (dt)) { X = 0, Y = 0, Width = SCREEN_WIDTH, Height = SCREEN_HEIGHT };
        _runnable.Add (_tableView);

        // Focus so key bindings resolve.
        _tableView.SetFocus ();

        // Cache injector to avoid per-call lookup overhead.
        _injector = _app.GetInputInjector ();

        // Warm up.
        _app.LayoutAndDraw (true);
    }

    private static DataTable BuildDataTable (int rows, int cols)
    {
        DataTable dt = new ();

        for (var colIndex = 0; colIndex < cols; colIndex++)
        {
            dt.Columns.Add ($"Column_{colIndex:D2}", typeof (string));
        }

        for (var rowIndex = 0; rowIndex < rows; rowIndex++)
        {
            var row = new object [cols];

            for (var colIndex = 0; colIndex < cols; colIndex++)
            {
                row [colIndex] = $"R{rowIndex:D4}_C{colIndex:D2}_Data";
            }

            dt.Rows.Add (row);
        }

        return dt;
    }
}
