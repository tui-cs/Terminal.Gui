using BenchmarkDotNet.Attributes;
using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;

namespace Terminal.Gui.Benchmarks.EnumExtensions;

/// <summary>
///     Benchmarks comparing <c>Enum.HasFlag</c> (boxing) with generated <c>FastHasFlags</c>
///     (zero-allocation) for the 5 most performance-critical <c>[Flags]</c> enums.
/// </summary>
/// <remarks>
///     <para>
///         <c>HasFlag</c> boxes both operands on every call.  <c>FastHasFlags</c> uses
///         <c>Unsafe.As</c> to reinterpret the enum as an <c>int</c>/<c>uint</c> and performs
///         a simple bitwise AND — no allocation, no virtual dispatch.
///     </para>
///     <para>
///         Instance fields are used (not <c>const</c>) to prevent constant-folding by the JIT.
///         Each benchmark tests 1,000 iterations to accumulate measurable work.
///     </para>
/// </remarks>
[MemoryDiagnoser]
[BenchmarkCategory ("EnumExtensions")]
public class HasFlagVsFastHasFlagsBenchmark
{
    private const int Iterations = 1_000;

    // Instance fields — not const — prevent JIT constant folding.
    private MouseFlags _mouseValue;
    private MouseFlags _mouseCheck;
    private ViewArrangement _arrangementValue;
    private ViewArrangement _arrangementCheck;
    private ViewportSettingsFlags _viewportValue;
    private ViewportSettingsFlags _viewportCheck;
    private MouseState _mouseStateValue;
    private MouseState _mouseStateCheck;
    private KeyCode _keyCodeValue;
    private KeyCode _keyCodeCheck;

    [GlobalSetup]
    public void Setup ()
    {
        _mouseValue = MouseFlags.LeftButtonPressed | MouseFlags.Shift | MouseFlags.PositionReport;
        _mouseCheck = MouseFlags.LeftButtonPressed;

        _arrangementValue = ViewArrangement.Movable | ViewArrangement.Resizable | ViewArrangement.Overlapped;
        _arrangementCheck = ViewArrangement.Overlapped;

        _viewportValue = ViewportSettingsFlags.AllowNegativeX | ViewportSettingsFlags.AllowNegativeY | ViewportSettingsFlags.Transparent;
        _viewportCheck = ViewportSettingsFlags.Transparent;

        _mouseStateValue = MouseState.In | MouseState.Pressed;
        _mouseStateCheck = MouseState.Pressed;

        _keyCodeValue = KeyCode.A | KeyCode.ShiftMask | KeyCode.CtrlMask;
        _keyCodeCheck = KeyCode.ShiftMask;
    }

    // ── MouseFlags (Rank 1 — 122 call sites in mouse input hot path) ──────────

    [Benchmark (Baseline = true)]
    public int MouseFlags_HasFlag ()
    {
        var count = 0;

        for (var i = 0; i < Iterations; i++)
        {
            if (_mouseValue.HasFlag (_mouseCheck))
            {
                count++;
            }
        }

        return count;
    }

    [Benchmark]
    public int MouseFlags_FastHasFlags ()
    {
        var count = 0;

        for (var i = 0; i < Iterations; i++)
        {
            if (_mouseValue.FastHasFlags (_mouseCheck))
            {
                count++;
            }
        }

        return count;
    }

    // ── ViewArrangement (Rank 2 — 42 call sites in layout / arrangement) ──────

    [Benchmark]
    public int ViewArrangement_HasFlag ()
    {
        var count = 0;

        for (var i = 0; i < Iterations; i++)
        {
            if (_arrangementValue.HasFlag (_arrangementCheck))
            {
                count++;
            }
        }

        return count;
    }

    [Benchmark]
    public int ViewArrangement_FastHasFlags ()
    {
        var count = 0;

        for (var i = 0; i < Iterations; i++)
        {
            if (_arrangementValue.FastHasFlags (_arrangementCheck))
            {
                count++;
            }
        }

        return count;
    }

    // ── ViewportSettingsFlags (Rank 3 — 31 call sites in drawing / scrolling) ─

    [Benchmark]
    public int ViewportSettingsFlags_HasFlag ()
    {
        var count = 0;

        for (var i = 0; i < Iterations; i++)
        {
            if (_viewportValue.HasFlag (_viewportCheck))
            {
                count++;
            }
        }

        return count;
    }

    [Benchmark]
    public int ViewportSettingsFlags_FastHasFlags ()
    {
        var count = 0;

        for (var i = 0; i < Iterations; i++)
        {
            if (_viewportValue.FastHasFlags (_viewportCheck))
            {
                count++;
            }
        }

        return count;
    }

    // ── MouseState (Rank 4 — 14 call sites in mouse state tracking) ───────────

    [Benchmark]
    public int MouseState_HasFlag ()
    {
        var count = 0;

        for (var i = 0; i < Iterations; i++)
        {
            if (_mouseStateValue.HasFlag (_mouseStateCheck))
            {
                count++;
            }
        }

        return count;
    }

    [Benchmark]
    public int MouseState_FastHasFlags ()
    {
        var count = 0;

        for (var i = 0; i < Iterations; i++)
        {
            if (_mouseStateValue.FastHasFlags (_mouseStateCheck))
            {
                count++;
            }
        }

        return count;
    }

    // ── KeyCode (Rank 5 — 11 call sites in keyboard input) ────────────────────

    [Benchmark]
    public int KeyCode_HasFlag ()
    {
        var count = 0;

        for (var i = 0; i < Iterations; i++)
        {
            if (_keyCodeValue.HasFlag (_keyCodeCheck))
            {
                count++;
            }
        }

        return count;
    }

    [Benchmark]
    public int KeyCode_FastHasFlags ()
    {
        var count = 0;

        for (var i = 0; i < Iterations; i++)
        {
            if (_keyCodeValue.FastHasFlags (_keyCodeCheck))
            {
                count++;
            }
        }

        return count;
    }
}
