#nullable enable
using System.Data;
using System.Reflection;
using System.Text.Json;

namespace UICatalog;

/// <summary>
///     Provides functionality for running and benchmarking Terminal.Gui <see cref="Scenario"/>s.
/// </summary>
public class Runner
{
    /// <summary>
    ///     Sets <see cref="TuiConfigurationBuilder.RuntimeConfig"/> with Application.ForceDriver and Driver.Force16Colors.
    /// </summary>
    /// <param name="forceDriver">The driver to use, or null to use the default.</param>
    /// <param name="force16Colors">
    ///     Whether to force 16-color mode. If null, the current setting is preserved.
    /// </param>
    public void SetRuntimeConfig (string? forceDriver = null, bool? force16Colors = null)
    {
        // Create runtime config JSON containing "Application.ForceDriver" and "Driver.Force16Colors" if specified
        Dictionary<string, object> runtimeConfig = new ();

        if (!string.IsNullOrEmpty (forceDriver))
        {
            runtimeConfig ["Application"] = new Dictionary<string, object> { ["ForceDriver"] = forceDriver };
        }

        if (force16Colors.HasValue)
        {
            runtimeConfig ["Driver"] = new Dictionary<string, object> { ["Force16Colors"] = force16Colors.Value };
        }

        if (runtimeConfig.Count == 0)
        {
            return;
        }

        TuiConfigurationBuilder.Shared.RuntimeConfig = JsonSerializer.Serialize (runtimeConfig);
        TuiConfigurationBuilder.Shared.ApplyToStaticFacades ();
    }

    /// <summary>
    ///     Runs a single scenario with optional benchmarking.
    /// </summary>
    /// <param name="scenarioName"></param>
    /// <param name="benchmark">Whether to collect benchmark metrics.</param>
    /// <returns>Benchmark results if benchmarking was enabled, otherwise null.</returns>
    public BenchmarkResults? RunScenario (string scenarioName, bool benchmark)
    {
        // Mark log position so we can capture logs for just this scenario
        UICatalog.LogCapture.MarkScenarioStart ();

        // Create instance of the scenario
        var scenario = (Scenario)Activator.CreateInstance (Scenario.GetScenarios ()
                                                                   .FirstOrDefault (s => s.GetName ().Equals (scenarioName, StringComparison.OrdinalIgnoreCase))
                                                                   !.GetType ())!;

        if (benchmark)
        {
            scenario.StartBenchmark ();
        }

        Logging.Information ($"Calling {scenario.GetName ()}.Main()");
        scenario.Main ();
        Logging.Information ($"Returned from {scenario.GetName ()}.Main()");

        BenchmarkResults? results = null;

        if (benchmark)
        {
            results = scenario.EndBenchmark ();
        }

        scenario.Dispose ();

        // Check for undisposed views (logs errors if DEBUG_IDISPOSABLE is defined)
#if DEBUG_IDISPOSABLE
        View.VerifyViewsWereDisposed ();
#endif

        return results;
    }

    /// <summary>
    ///     Runs benchmarks for all provided scenarios.
    /// </summary>
    /// <param name="scenarios">The scenarios to benchmark.</param>
    /// <returns>List of benchmark results for all scenarios.</returns>
    public List<BenchmarkResults> BenchmarkAllScenarios (IEnumerable<Scenario> scenarios)
    {
        List<BenchmarkResults> resultsList = [];

        foreach (Scenario s in scenarios)
        {
            BenchmarkResults? result = RunScenario (s.GetName (), true);

            if (result is { })
            {
                resultsList.Add (result);
            }
        }

        return resultsList;
    }

    /// <summary>
    ///     Saves benchmark results to a JSON file.
    /// </summary>
    /// <param name="results">The results to save.</param>
    /// <param name="filePath">The file path to write to.</param>
    public static void SaveResultsToFile (List<BenchmarkResults> results, string filePath)
    {
        string output = JsonSerializer.Serialize (results, new JsonSerializerOptions { WriteIndented = true });

        using StreamWriter file = File.CreateText (filePath);
        file.Write (output);
        file.Close ();
    }

    /// <summary>
    ///     Displays benchmark results in a TableView UI.
    /// </summary>
    /// <param name="results">The results to display.</param>
    public static void DisplayResultsUI (List<BenchmarkResults> results)
    {
        if (results.Count <= 0)
        {
            return;
        }

        using IApplication app = Application.Create ();
        app.Init ();

        using Window benchmarkWindow = new ();
        benchmarkWindow.Title = "Benchmark Results";

        if (benchmarkWindow.Border is { })
        {
            benchmarkWindow.Border.Thickness = new Thickness (0, 0, 0, 0);
        }

        TableView resultsTableView = new () { Width = Dim.Fill (), Height = Dim.Fill () };

        // TableView provides many options for table headers. For simplicity we turn all
        // of these off. By enabling FullRowSelect and turning off headers, TableView looks just
        // like a ListView
        resultsTableView.FullRowSelect = true;
        resultsTableView.Style.ShowHeaders = true;
        resultsTableView.Style.ShowHorizontalHeaderOverline = false;
        resultsTableView.Style.ShowHorizontalHeaderUnderline = true;
        resultsTableView.Style.ShowHorizontalBottomLine = false;
        resultsTableView.Style.ShowVerticalCellLines = true;
        resultsTableView.Style.ShowVerticalHeaderLines = true;

        // TableView typically is a grid where nav keys are biased for moving left/right.
        resultsTableView.KeyBindings.Remove (Key.Home);
        resultsTableView.KeyBindings.Add (Key.Home, Command.Start);
        resultsTableView.KeyBindings.Remove (Key.End);
        resultsTableView.KeyBindings.Add (Key.End, Command.End);

        // Ideally, TableView.MultiSelect = false would turn off any keybindings for
        // multi-select options. But it currently does not.
        resultsTableView.MultiSelect = false;

        DataTable dt = new ();

        dt.Columns.Add (new DataColumn ("Scenario", typeof (string)));
        dt.Columns.Add (new DataColumn ("Duration", typeof (TimeSpan)));
        dt.Columns.Add (new DataColumn ("Refreshed", typeof (int)));
        dt.Columns.Add (new DataColumn ("LaidOut", typeof (int)));
        dt.Columns.Add (new DataColumn ("ClearedContent", typeof (int)));
        dt.Columns.Add (new DataColumn ("DrawComplete", typeof (int)));
        dt.Columns.Add (new DataColumn ("Updated", typeof (int)));
        dt.Columns.Add (new DataColumn ("Iterations", typeof (int)));

        foreach (BenchmarkResults r in results)
        {
            dt.Rows.Add (r.Scenario,
                         r.Duration,
                         r.RefreshedCount,
                         r.LaidOutCount,
                         r.ClearedContentCount,
                         r.DrawCompleteCount,
                         r.UpdatedCount,
                         r.IterationCount);
        }

        BenchmarkResults totalRow = new ()
        {
            Scenario = "TOTAL",
            Duration = new TimeSpan (results.Sum (r => r.Duration.Ticks)),
            RefreshedCount = results.Sum (r => r.RefreshedCount),
            LaidOutCount = results.Sum (r => r.LaidOutCount),
            ClearedContentCount = results.Sum (r => r.ClearedContentCount),
            DrawCompleteCount = results.Sum (r => r.DrawCompleteCount),
            UpdatedCount = results.Sum (r => r.UpdatedCount),
            IterationCount = results.Sum (r => r.IterationCount)
        };

        dt.Rows.Add (totalRow.Scenario,
                     totalRow.Duration,
                     totalRow.RefreshedCount,
                     totalRow.LaidOutCount,
                     totalRow.ClearedContentCount,
                     totalRow.DrawCompleteCount,
                     totalRow.UpdatedCount,
                     totalRow.IterationCount);

        dt.DefaultView.Sort = "Duration";
        DataTable sortedCopy = dt.DefaultView.ToTable ();

        resultsTableView.Table = new DataTableSource (sortedCopy);

        benchmarkWindow.Add (resultsTableView);

        app.Run (benchmarkWindow);
    }

    #region Interactive Mode

    private static readonly FileSystemWatcher _currentDirWatcher = new ();
    private static readonly FileSystemWatcher _homeDirWatcher = new ();

    private bool _configWatcherStarted;

    /// <summary>
    ///     Runs in interactive mode, showing a UI to select scenarios and running them in a loop.
    /// </summary>
    /// <typeparam name="T">The Runnable type to use as the scenario browser UI.</typeparam>
    /// <param name="enableConfigWatcher">Whether to enable config file watching.</param>
    public void RunInteractive<T> (bool enableConfigWatcher = true) where T : Runnable, new ()
    {
        Logging.Information ($"{typeof (T).Name}");

#if DEBUG_IDISPOSABLE
        View.EnableDebugIDisposableAsserts = true;
#endif

        if (enableConfigWatcher)
        {
            StartConfigWatcher ();
        }

        try
        {
            // Show browser UI, get selected scenario, run it, repeat until user quits
            while (true)
            {
                IApplication app = RunBrowserUI<T> ();
                var selectedScenarioName = app.GetResult<string> ();

                //Logging.Trace($"Disposing app");
                app.Dispose ();

                if (string.IsNullOrEmpty (selectedScenarioName))
                {
                    // User wants to quit
                    break;
                }

                RunScenario (selectedScenarioName, false);
            }
        }
        finally
        {
            if (enableConfigWatcher)
            {
                StopConfigWatcher ();
            }

#if DEBUG_IDISPOSABLE
            View.VerifyViewsWereDisposed ();
#endif
        }
    }

    /// <summary>
    ///     Runs the browser UI. The browser UI should set <see cref="IRunnable.Result"/> to the selected scenario name
    /// </summary>
    /// <typeparam name="T">The Runnable type to use as the browser UI.</typeparam>
    private IApplication RunBrowserUI<T> () where T : Runnable, new ()
    {
        IApplication app = Application.Create ();
        app.Init ();

        Logging.Information ($"{typeof (T).Name}");
        app.Run<T> ();
        Logging.Information ($"{typeof (T).Name} Result: {app.GetResult<string> ()}");

        //VerifyObjectsWereDisposed ();
        return app;
    }

    /// <summary>
    ///     Starts watching for configuration file changes.
    /// </summary>
    public void StartConfigWatcher ()
    {
        if (_configWatcherStarted)
        {
            return;
        }

        // Set up a file system watcher for `./.tui/`
        _currentDirWatcher.NotifyFilter = NotifyFilters.LastWrite;

        string assemblyLocation = Assembly.GetExecutingAssembly ().Location;
        string tuiDir;

        if (!string.IsNullOrEmpty (assemblyLocation))
        {
            FileInfo assemblyFile = new (assemblyLocation);
            tuiDir = Path.Combine (assemblyFile.Directory!.FullName, ".tui");
        }
        else
        {
            tuiDir = Path.Combine (AppContext.BaseDirectory, ".tui");
        }

        if (!Directory.Exists (tuiDir))
        {
            Directory.CreateDirectory (tuiDir);
        }

        _currentDirWatcher.Path = tuiDir;
        _currentDirWatcher.Filter = "*config.json";

        // Set up a file system watcher for `~/.tui/`
        _homeDirWatcher.NotifyFilter = NotifyFilters.LastWrite;
        FileInfo homeDir = new (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile));
        tuiDir = Path.Combine (homeDir.FullName, ".tui");

        if (!Directory.Exists (tuiDir))
        {
            Directory.CreateDirectory (tuiDir);
        }

        _homeDirWatcher.Path = tuiDir;
        _homeDirWatcher.Filter = "*config.json";

        _currentDirWatcher.Changed += ConfigFileChanged;
        _currentDirWatcher.EnableRaisingEvents = true;

        _homeDirWatcher.Changed += ConfigFileChanged;
        _homeDirWatcher.EnableRaisingEvents = true;

        ThemeManager.ThemeChanged += ThemeManagerOnThemeChanged;

        _configWatcherStarted = true;
    }

    /// <summary>
    ///     Stops watching for configuration file changes.
    /// </summary>
    public void StopConfigWatcher ()
    {
        if (!_configWatcherStarted)
        {
            return;
        }

        ThemeManager.ThemeChanged -= ThemeManagerOnThemeChanged;

        _currentDirWatcher.EnableRaisingEvents = false;
        _currentDirWatcher.Changed -= ConfigFileChanged;

        _homeDirWatcher.EnableRaisingEvents = false;
        _homeDirWatcher.Changed -= ConfigFileChanged;

        _configWatcherStarted = false;
    }

    private static void ThemeManagerOnThemeChanged (object? sender, EventArgs<string> e) =>
        TuiConfigurationBuilder.Shared.ApplyActiveThemeOverlays ();

    private static void ConfigFileChanged (object sender, FileSystemEventArgs e)
    {
        Logging.Debug ($"{e.FullPath} {e.ChangeType} - Loading and Applying");
        TuiConfigurationBuilder.Shared.Reload ();
        TuiConfigurationBuilder.Shared.ApplyToStaticFacades ();
    }

    #endregion Interactive Mode
}
