using BenchmarkDotNet.Attributes;
using Terminal.Gui.Drawing;
using TgAttribute = Terminal.Gui.Drawing.Attribute;

namespace Terminal.Gui.Benchmarks.Configuration;

/// <summary>
///     Measures <see cref="Scheme.GetAttributeForRole"/> for roles at different depths of the derivation chain:
///     <list type="bullet">
///         <item><see cref="VisualRole.Normal"/> — explicitly set (O(1) lookup)</item>
///         <item><see cref="VisualRole.HotFocus"/> — derived from <see cref="VisualRole.Focus"/></item>
///         <item><see cref="VisualRole.Code"/> — deepest derivation (<c>Code → Editable → Normal</c>)</item>
///     </list>
///     No configuration pipeline required; operates on a standalone
///     <see cref="Scheme"/> instance.
/// </summary>
/// <remarks>
///     <para>
///         Run:
///         <code>dotnet run --project Tests/Benchmarks -c Release -- --filter '*SchemeAttribute*'</code>
///     </para>
/// </remarks>
[MemoryDiagnoser]
[BenchmarkCategory ("Configuration", "Scheme")]
public class SchemeAttributeBenchmark
{
    private Scheme _scheme = null!;

    /// <summary>Creates a scheme with only <see cref="VisualRole.Normal"/> explicitly set.</summary>
    [GlobalSetup]
    public void Setup ()
    {
        _scheme = new Scheme { Normal = new TgAttribute (Color.White, Color.Black) };
    }

    /// <summary>Lookup for an explicitly-set role — the fastest path.</summary>
    [Benchmark (Baseline = true)]
    public TgAttribute GetNormal () => _scheme.GetAttributeForRole (VisualRole.Normal);

    /// <summary>Lookup for a role derived from Focus (which itself is derived from Normal).</summary>
    [Benchmark]
    public TgAttribute GetHotFocus () => _scheme.GetAttributeForRole (VisualRole.HotFocus);

    /// <summary>Lookup for the deepest derivation path: Code → Editable → Normal.</summary>
    [Benchmark]
    public TgAttribute GetCode () => _scheme.GetAttributeForRole (VisualRole.Code);
}
