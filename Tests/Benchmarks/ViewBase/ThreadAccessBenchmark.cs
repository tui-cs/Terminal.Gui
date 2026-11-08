using BenchmarkDotNet.Attributes;
using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using Terminal.Gui.ViewBase;

namespace Terminal.Gui.Benchmarks.ViewBase;

/// <summary>Measures the time and allocation cost of UI-thread checks and invalidation with diagnostics on and off.</summary>
/// <remarks>Run with <c>dotnet run --project Tests/Benchmarks -c Release -- --filter "*ThreadAccess*"</c>.</remarks>
[MemoryDiagnoser]
public class ThreadAccessBenchmark
{
    private IApplication _app = null!;
    private View _root = null!;
    private View _view = null!;

    /// <summary>Gets or sets whether automatic thread checks are enabled.</summary>
    [Params (false, true)]
    public bool Enabled { get; set; }

    /// <summary>Gets or sets the number of SuperViews through which the measured view inherits its application.</summary>
    [Params (0, 10, 30)]
    public int Depth { get; set; }

    /// <summary>Creates an associated view on the UI thread.</summary>
    [GlobalSetup]
    public void Setup ()
    {
        _app = Application.Create ().EnableThreadAccessChecks (Enabled).Init (DriverRegistry.Names.ANSI);
        _root = new () { Width = 10, Height = 5, App = _app };
        _view = _root;

        for (var index = 0; index < Depth; index++)
        {
            View subView = new () { Width = 10, Height = 5 };
            _view.Add (subView);
            _view = subView;
        }
    }

    /// <summary>Measures the guard independently of the guarded mutation.</summary>
    [Benchmark]
    public void Check () => _view.VerifyAccessIfEnabled ();

    /// <summary>Measures layout invalidation, including its guard.</summary>
    [Benchmark]
    public void LayoutInvalidation () => _view.SetNeedsLayout ();

    /// <summary>Measures draw invalidation, including its guard.</summary>
    [Benchmark]
    public void DrawInvalidation () => _view.SetNeedsDraw ();

    private int _width;

    /// <summary>Measures a property mutation that propagates layout changes through the hierarchy.</summary>
    [Benchmark]
    public void WidthAssignment () => _view.Width = 10 + (_width++ & 1);

    /// <summary>Releases the view and application.</summary>
    [GlobalCleanup]
    public void Cleanup ()
    {
        _root.Dispose ();
        _app.Dispose ();
    }
}
