#nullable enable
namespace UICatalog;

public struct UICatalogCommandLineOptions
{
    public string Driver { get; set; }

    public bool DontEnableConfigurationManagement { get; set; }

    public string Scenario { get; set; }

    public uint BenchmarkTimeout { get; set; }

    public bool Benchmark { get; set; }

    public string ResultsFile { get; set; }

    public string DebugLogLevel { get; set; }

    /// <summary>
    ///     Gets or sets whether to force 16-color mode. Null means use default/config setting.
    /// </summary>
    public bool? Force16Colors { get; set; }
}
