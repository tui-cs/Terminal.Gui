using BenchmarkDotNet.Attributes;
using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Terminal.Gui.Benchmarks.Views;

/// <summary>
///     Benchmarks for <see cref="Button"/> draw performance, focused on the cost of
///     <c>_interiorTextFormatter</c> property assignments in <c>OnDrawingText</c>.
/// </summary>
/// <remarks>
///     <para>
///         Prior to the guards introduced in PR #5279, every call to <c>OnDrawingText</c> set all
///         <c>_interiorTextFormatter</c> properties unconditionally, which always set
///         <c>NeedsFormat = true</c> and forced the formatter to re-allocate on the next access —
///         even when nothing had changed.  After the fix, unchanged values are skipped.
///     </para>
///     <para>
///         <b>How to read results:</b>
///         <list type="bullet">
///             <item><see cref="DrawButton_Unchanged"/> represents the guard-optimized steady-state path.</item>
///             <item><see cref="DrawButton_TextChanging"/> forces a real reformat each iteration,
///             approximating the old unguarded behaviour.</item>
///         </list>
///         The allocation difference between the two methods shows the overhead that the guards eliminate.
///     </para>
///     <para>
///         Run:
///         <code>dotnet run --project Tests/Benchmarks -c Release -- --filter "*ButtonDraw*"</code>
///     </para>
/// </remarks>
[MemoryDiagnoser]
[BenchmarkCategory ("Views", "Button")]
public class ButtonDrawBenchmark
{
    // Four interior texts of equal display width so layout stays stable between iterations.
    private static readonly string [] _changingTexts = ["_OK", "_No", "_Go", "_Up"];

    private IApplication _app = null!;
    private Button _button = null!;
    private int _textIndex;
    private SessionToken? _session;

    /// <summary>Fixed width of the button under test.</summary>
    [Params (10, 40)]
    public int Width { get; set; }

    /// <summary>Whether the button is the default button (adds extra decoration characters).</summary>
    [Params (false, true)]
    public bool IsDefault { get; set; }

    /// <summary>Create the application and button once per parameter combination.</summary>
    [GlobalSetup]
    public void Setup ()
    {
        _app = Application.Create ();
        _app.Init (DriverRegistry.Names.ANSI);
        _app.Driver!.SetScreenSize (Width, 1);

        Runnable runnable = new () { Width = Width, Height = 1 };
        _session = _app.Begin (runnable);

        _button = new ()
        {
            Text = "_OK",
            X = 0,
            Y = 0,
            Width = Width,
            Height = 1,
            IsDefault = IsDefault,
            ShadowStyle = ShadowStyles.None
        };

        runnable.Add (_button);

        // Warm-up draw so caches and JIT paths are primed before measurement.
        _app.LayoutAndDraw ();
    }

    /// <summary>Mark the button as needing a redraw before each iteration.</summary>
    /// <remarks>This ensures <c>OnDrawingText</c> is always called, while keeping all
    /// formatter property values identical to the previous draw — isolating the guard benefit.</remarks>
    [IterationSetup]
    public void MarkNeedsDraw ()
    {
        _button.SetNeedsDraw ();
    }

    /// <summary>
    ///     Draws the button with no property changes since the last draw.
    ///     Guards on <c>_interiorTextFormatter</c> skip all assignments, so <c>NeedsFormat</c>
    ///     is not set and the formatter does not re-allocate.
    /// </summary>
    [Benchmark (Baseline = true)]
    public void DrawButton_Unchanged ()
    {
        _app.LayoutAndDraw ();
    }

    /// <summary>
    ///     Rotates the button <see cref="View.Text"/> before each draw.
    ///     The interior text changes, so the <c>Text</c> guard fires, <c>NeedsFormat</c> is
    ///     set, and the formatter re-allocates — approximating the old unguarded behaviour.
    /// </summary>
    [Benchmark]
    public void DrawButton_TextChanging ()
    {
        _button.Text = _changingTexts [_textIndex++ % _changingTexts.Length];
        _app.LayoutAndDraw ();
    }

    /// <summary>Dispose the application after all iterations.</summary>
    [GlobalCleanup]
    public void Cleanup ()
    {
        if (_session is not null)
        {
            _app.End (_session);
        }

        _app.Dispose ();
    }
}
