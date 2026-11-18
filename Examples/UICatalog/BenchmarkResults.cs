using System.Text.Json.Serialization;

namespace UICatalog;

/// <summary>
///     Contains metrics collected during a benchmark run of a <see cref="Scenario"/>.
/// </summary>
/// <remarks>
///     These results are collected when running scenarios with the <c>-b</c> or <c>--benchmark</c> flag
///     and can be serialized to JSON for analysis.
/// </remarks>
public class BenchmarkResults
{
    /// <summary>
    ///     Gets or sets the name of the scenario that was benchmarked.
    /// </summary>
    [JsonInclude]
    public string Scenario { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the total duration of the benchmark run.
    /// </summary>
    [JsonInclude]
    public TimeSpan Duration { get; set; }

    /// <summary>
    ///     Gets or sets the number of main loop iterations that occurred during the benchmark.
    /// </summary>
    [JsonInclude]
    public int IterationCount { get; set; }

    /// <summary>
    ///     Gets or sets the number of times the driver's content was cleared during the benchmark.
    /// </summary>
    [JsonInclude]
    public int ClearedContentCount { get; set; }

    /// <summary>
    ///     Gets or sets the number of screen refresh operations during the benchmark.
    /// </summary>
    [JsonInclude]
    public int RefreshedCount { get; set; }

    /// <summary>
    ///     Gets or sets the number of update operations during the benchmark.
    /// </summary>
    [JsonInclude]
    public int UpdatedCount { get; set; }

    /// <summary>
    ///     Gets or sets the number of times view drawing completed during the benchmark.
    /// </summary>
    [JsonInclude]
    public int DrawCompleteCount { get; set; }

    /// <summary>
    ///     Gets or sets the number of times SubViews were laid out during the benchmark.
    /// </summary>
    [JsonInclude]
    public int LaidOutCount { get; set; }
}
