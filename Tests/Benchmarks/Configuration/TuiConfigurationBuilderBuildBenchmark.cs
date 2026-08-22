using BenchmarkDotNet.Attributes;
using Terminal.Gui.Configuration;

namespace Terminal.Gui.Benchmarks.Configuration;

/// <summary>
///     Measures the cold-start cost of building the MEC source chain and applying
///     settings facades from the embedded library <c>config.json</c>.
/// </summary>
/// <remarks>
///     <para>
///         Run:
///         <code>dotnet run --project Tests/Benchmarks -c Release -- --filter '*TuiConfigurationBuilderBuild*'</code>
///     </para>
/// </remarks>
[MemoryDiagnoser]
[BenchmarkCategory ("Configuration", "Startup")]
public class TuiConfigurationBuilderBuildBenchmark
{
    /// <summary>
    ///     Builds a new <see cref="TuiConfigurationBuilder"/> and applies library defaults
    ///     plus any remaining sources to the static settings facades.
    /// </summary>
    [Benchmark]
    public TuiConfigurationBuilder BuildAndApply ()
    {
        TuiConfigurationBuilder builder = new ();
        builder.ApplyToStaticFacades ();

        return builder;
    }
}
