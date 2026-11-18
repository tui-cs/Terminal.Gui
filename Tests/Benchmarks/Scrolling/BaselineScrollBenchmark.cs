using System.Drawing;
using BenchmarkDotNet.Attributes;
using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Terminal.Gui.Benchmarks.Scrolling;

/// <summary>
///     Measures framework-level scrolling overhead using a minimal <see cref="View"/> subclass that has a large
///     <see cref="View.ContentSize"/> but performs no custom rendering. Isolates the viewport-math, layout, and
///     draw-dispatch costs from any view-specific rendering work.
/// </summary>
/// <remarks>
///     <para>
///         Run:
///         <code>dotnet run --project Tests/Benchmarks -c Release -- --filter '*BaselineScroll*'</code>
///     </para>
/// </remarks>
[MemoryDiagnoser]
[BenchmarkCategory ("Scrolling", "Baseline")]
public class BaselineScrollBenchmark
{
    private const int SCREEN_HEIGHT = 25;
    private const int SCREEN_WIDTH = 80;

    private IApplication _app = null!;
    private Runnable _runnable = null!;
    private SessionToken? _session;
    private View _view = null!;

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

    /// <summary>Total virtual content height of the view (rows).</summary>
    [Params (1_000, 10_000)]
    public int ContentHeight { get; set; }

    /// <summary>
    ///     Positions the viewport at the mid-point of the document so that each benchmark iteration
    ///     scrolls from a stable, representative location.
    /// </summary>
    [IterationSetup]
    public void IterationSetup ()
    {
        _view.Viewport = _view.Viewport with { Y = ContentHeight / 2 };
        _view.SetNeedsDraw ();
    }

    /// <summary>Creates the application context, view hierarchy, and performs one warm-up draw.</summary>
    [GlobalSetup]
    public void Setup ()
    {
        _app = Application.Create ();
        _app.Init (DriverRegistry.Names.ANSI);
        _app.Driver!.SetScreenSize (SCREEN_WIDTH, SCREEN_HEIGHT);

        _runnable = new Runnable { Width = SCREEN_WIDTH, Height = SCREEN_HEIGHT };
        _session = _app.Begin (_runnable);

        _view = new View
        {
            X = 0,
            Y = 0,
            Width = SCREEN_WIDTH,
            Height = SCREEN_HEIGHT,
            ViewportSettings = ViewportSettingsFlags.HasVerticalScrollBar
        };
        _view.SetContentSize (new Size (SCREEN_WIDTH, ContentHeight));
        _runnable.Add (_view);

        // Warm up: prime JIT and layout caches before measurement.
        _app.LayoutAndDraw (true);
    }

    /// <summary>
    ///     Scrolls the viewport down by one row and redraws.
    ///     Measures the minimal per-row cost of the layout+draw pipeline with no content rendering.
    /// </summary>
    [Benchmark (Baseline = true)]
    public void ViewportScroll_Down ()
    {
        _view.Viewport = _view.Viewport with { Y = _view.Viewport.Y + 1 };
        _app.LayoutAndDraw ();
    }

    /// <summary>Scrolls the viewport down by one page (ScreenHeight rows) and redraws.</summary>
    [Benchmark]
    public void ViewportScroll_PageDown ()
    {
        int newY = Math.Min (_view.Viewport.Y + SCREEN_HEIGHT, ContentHeight - SCREEN_HEIGHT);
        _view.Viewport = _view.Viewport with { Y = newY };
        _app.LayoutAndDraw ();
    }

    /// <summary>Scrolls the viewport up by one row and redraws.</summary>
    [Benchmark]
    public void ViewportScroll_Up ()
    {
        _view.Viewport = _view.Viewport with { Y = _view.Viewport.Y - 1 };
        _app.LayoutAndDraw ();
    }
}
