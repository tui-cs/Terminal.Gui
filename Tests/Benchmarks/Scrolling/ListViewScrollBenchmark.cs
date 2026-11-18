using System.Collections.ObjectModel;
using BenchmarkDotNet.Attributes;
using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using Terminal.Gui.Testing;
using Terminal.Gui.Views;

namespace Terminal.Gui.Benchmarks.Scrolling;

/// <summary>
///     Measures end-to-end scrolling performance for <see cref="ListView"/>.
///     Covers item rendering with mark glyphs, selection role highlighting,
///     and per-item draw cost at varying collection sizes.
/// </summary>
/// <remarks>
///     <para>
///         Run:
///         <code>dotnet run --project Tests/Benchmarks -c Release -- --filter '*ListViewScroll*'</code>
///     </para>
/// </remarks>
[MemoryDiagnoser]
[BenchmarkCategory ("Scrolling", "ListView")]
public class ListViewScrollBenchmark
{
    private const int SCREEN_HEIGHT = 25;
    private const int SCREEN_WIDTH = 80;

    private IApplication _app = null!;
    private IInputInjector _injector = null!;
    private ListView _listView = null!;
    private Runnable _runnable = null!;
    private SessionToken? _session;

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

    /// <summary>Total number of items loaded into the <see cref="ListView"/>.</summary>
    [Params (1_000, 10_000)]
    public int Items { get; set; }

    /// <summary>
    ///     Positions the selection at the last visible row so the next down-arrow triggers a scroll.
    /// </summary>
    [IterationSetup]
    public void IterationSetup ()
    {
        _listView.SelectedItem = SCREEN_HEIGHT - 1;
        _listView.SetNeedsDraw ();
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

    /// <summary>
    ///     Injects a single <see cref="Key.CursorDown"/> keystroke and redraws.
    ///     With the selection at the viewport boundary this always scrolls.
    /// </summary>
    [Benchmark (Baseline = true)]
    public void ScrollDown_OneStep ()
    {
        _injector.InjectKey (Key.CursorDown);
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

    /// <summary>Creates the application, populates the list, and warms up the view.</summary>
    [GlobalSetup]
    public void Setup ()
    {
        _app = Application.Create ();
        _app.Init (DriverRegistry.Names.ANSI);
        _app.Driver!.SetScreenSize (SCREEN_WIDTH, SCREEN_HEIGHT);

        _runnable = new Runnable { Width = SCREEN_WIDTH, Height = SCREEN_HEIGHT };
        _session = _app.Begin (_runnable);

        _listView = new ListView { X = 0, Y = 0, Width = SCREEN_WIDTH, Height = SCREEN_HEIGHT };
        _listView.SetSource (new ObservableCollection<string> (BuildItems (Items)));
        _runnable.Add (_listView);

        // Focus so key bindings resolve.
        _listView.SetFocus ();

        // Cache injector to avoid per-call lookup overhead.
        _injector = _app.GetInputInjector ();

        // Warm up.
        _app.LayoutAndDraw (true);
    }

    private static List<string> BuildItems (int count)
    {
        List<string> items = new (count);

        for (var itemIndex = 0; itemIndex < count; itemIndex++)
        {
            items.Add ($"Item {itemIndex,6}: data value = {itemIndex * 17 % 100:D3}");
        }

        return items;
    }
}
