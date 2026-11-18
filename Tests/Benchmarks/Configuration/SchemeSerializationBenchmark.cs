using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Terminal.Gui.Configuration;
using Terminal.Gui.Drawing;
using TgAttribute = Terminal.Gui.Drawing.Attribute;

namespace Terminal.Gui.Benchmarks.Configuration;

/// <summary>
///     Measures serialize-then-deserialize of a representative <c>Base</c> <see cref="Scheme"/> via
///     <see cref="JsonSerializer"/> and <see cref="SchemeJsonConverter"/>. Catches regressions in the JSON
///     code paths when future PRs add fields to <see cref="Scheme"/>.
/// </summary>
/// <remarks>
///     <para>
///         Run:
///         <code>dotnet run --project Tests/Benchmarks -c Release -- --filter '*SchemeSerialization*'</code>
///     </para>
/// </remarks>
[MemoryDiagnoser]
[BenchmarkCategory ("Configuration", "Scheme")]
public class SchemeSerializationBenchmark
{
    private Scheme _scheme = null!;
    private string _json = null!;
    private JsonSerializerOptions _options = null!;

    /// <summary>
    ///     Creates a representative <c>Base</c> scheme with only <see cref="VisualRole.Normal"/> explicitly set
    ///     and prepares serialization options with the <see cref="SchemeJsonConverter"/>.
    /// </summary>
    [GlobalSetup]
    public void Setup ()
    {
        _scheme = new Scheme { Normal = new TgAttribute (Color.White, Color.Black) };

        _options = new JsonSerializerOptions
                   {
                       Converters = { new SchemeJsonConverter () },
                       PropertyNameCaseInsensitive = true
                   };

        // Pre-serialize to have a stable JSON string for deserialization benchmarks.
        _json = JsonSerializer.Serialize (_scheme, _options);
    }

    /// <summary>Serializes a <see cref="Scheme"/> to JSON.</summary>
    [Benchmark]
    public string Serialize () => JsonSerializer.Serialize (_scheme, _options);

    /// <summary>Deserializes a <see cref="Scheme"/> from JSON.</summary>
    [Benchmark]
    public Scheme? Deserialize () => JsonSerializer.Deserialize<Scheme> (_json, _options);

    /// <summary>Full round-trip: serialize then immediately deserialize.</summary>
    [Benchmark (Baseline = true)]
    public Scheme? RoundTrip ()
    {
        string json = JsonSerializer.Serialize (_scheme, _options);

        return JsonSerializer.Deserialize<Scheme> (json, _options);
    }
}
