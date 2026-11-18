#nullable enable
using System.Collections.ObjectModel;
using System.Diagnostics;
using Terminal.Gui.Testing;

namespace UICatalog;

/// <summary>
///     <para>Base class for each demo/scenario.</para>
///     <para>
///         To define a new scenario:
///         <list type="number">
///             <item>
///                 <description>
///                     Create a new <c>.cs</c> file in the <cs>Scenarios</cs> directory that derives from
///                     <see cref="Scenario"/>.
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     Annotate the <see cref="Scenario"/> derived class with a
///                     <see cref="ScenarioMetadata"/> attribute specifying the scenario's name and description.
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     Add one or more <see cref="ScenarioCategory"/> attributes to the class specifying
///                     which categories the scenario belongs to. If you don't specify a category the scenario will show up
///                     in "_All".
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     Implement the <see cref="Main"/> override which will be called when a user selects the
///                     scenario to run.
///                 </description>
///             </item>
///         </list>
///     </para>
///     <para>
///         The UI Catalog program uses reflection to find all scenarios and adds them to the ListViews. Press ENTER to
///         run the selected scenario. Press the default quit key to quit.
///     </para>
/// </summary>
public class Scenario : IDisposable
{
    private static int _maxScenarioNameLen = 0;

    /// <summary>
    ///     Gets the benchmark results collected during benchmarking mode.
    /// </summary>
    /// <remarks>
    ///     This property is populated when <see cref="StartBenchmark"/> is called before <see cref="Main"/>
    ///     and <see cref="EndBenchmark"/> is called after. The results include iteration counts, timing data,
    ///     and various rendering metrics.
    /// </remarks>
    public BenchmarkResults BenchmarkResults { get; } = new ();

    private bool _disposedValue;

    /// <summary>
    ///     Helper function to get the list of categories a <see cref="Scenario"/> belongs to (defined in
    ///     <see cref="ScenarioCategory"/>)
    /// </summary>
    /// <returns>list of category names</returns>
    public List<string> GetCategories () => ScenarioCategory.GetCategories (GetType ());

    /// <summary>Helper to get the <see cref="Scenario"/> Description (defined in <see cref="ScenarioMetadata"/>)</summary>
    /// <returns></returns>
    public string GetDescription () => ScenarioMetadata.GetDescription (GetType ());

    /// <summary>Helper to get the <see cref="Scenario"/> Name (defined in <see cref="ScenarioMetadata"/>)</summary>
    /// <returns></returns>
    public string GetName () => ScenarioMetadata.GetName (GetType ());

    /// <summary>
    ///     Helper to get the <see cref="Application.GetDefaultKey (Command.Quit)"/> and the <see cref="Scenario"/> Name (defined in
    ///     <see cref="ScenarioMetadata"/>)
    /// </summary>
    /// <returns></returns>
    public string GetQuitKeyAndName () => $"{Application.GetDefaultKey (Command.Quit)} to Quit - Scenario: {GetName ()}";

    /// <summary>
    ///     Returns a list of all <see cref="Scenario"/> instanaces defined in the project, sorted by
    ///     <see cref="ScenarioMetadata.Name"/>.
    ///     https://stackoverflow.com/questions/5411694/get-all-inherited-classes-of-an-abstract-class
    /// </summary>
    public static ObservableCollection<Scenario> GetScenarios ()
    {
        List<Scenario> objects = [];

        foreach (Type type in typeof (Scenario).Assembly.ExportedTypes.Where (myType => myType is { IsClass: true, IsAbstract: false }
                                                                                        && myType.IsSubclassOf (typeof (Scenario))))
        {
            if (Activator.CreateInstance (type) is not Scenario { } scenario)
            {
                continue;
            }

            objects.Add (scenario);
            _maxScenarioNameLen = Math.Max (_maxScenarioNameLen, scenario.GetName ().Length);
        }

        return new ObservableCollection<Scenario> (objects.OrderBy (s => s.GetName ()).ToList ());
    }

    /// <summary>
    ///     Called by UI Catalog to run the <see cref="Scenario"/>. This is the main entry point for the <see cref="Scenario"/>
    ///     .
    /// </summary>
    public virtual void Main () { }

    private const uint BENCHMARK_MAX_NATURAL_ITERATIONS = 500; // not including needed for demo keys
    private const int BENCHMARK_KEY_PACING = 10; // Must be non-zero

    /// <summary>
    ///     Gets or sets the maximum time in milliseconds to run a benchmark before forcing the scenario to quit.
    /// </summary>
    /// <remarks>
    ///     If the scenario does not exit within this timeout, <see cref="IApplication.RequestStop()"/> will be called
    ///     to force it to quit. The default value is 2500ms.
    /// </remarks>
    public static uint BenchmarkTimeout { get; set; } = 2500;

    private readonly object _timeoutLock = new ();
    private object? _timeout;
    private Stopwatch? _stopwatch;
    private IApplication? _benchmarkApp;

    /// <summary>
    ///     Starts benchmarking mode for this scenario.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Call this method before calling <see cref="Main"/> to enable benchmark metrics collection.
    ///         This subscribes to <see cref="Application.InstanceInitialized"/> and <see cref="Application.InstanceDisposed"/>
    ///         to track the application instance created by the scenario.
    ///     </para>
    ///     <para>
    ///         When benchmarking is enabled, the scenario will automatically quit after <see cref="BenchmarkTimeout"/>
    ///         milliseconds or after a maximum number of iterations, whichever comes first.
    ///     </para>
    /// </remarks>
    /// <seealso cref="EndBenchmark"/>
    /// <seealso cref="BenchmarkResults"/>
    public void StartBenchmark ()
    {
        BenchmarkResults.Scenario = GetName ();
        Application.InstanceInitialized += OnApplicationInstanceInitialized;
        Application.InstanceDisposed += OnApplicationInstanceDisposed;
    }

    /// <summary>
    ///     Ends benchmarking mode and returns the collected results.
    /// </summary>
    /// <returns>The <see cref="BenchmarkResults"/> containing metrics collected during the benchmark run.</returns>
    /// <remarks>
    ///     Call this method after <see cref="Main"/> returns to clean up benchmark subscriptions and
    ///     retrieve the collected metrics.
    /// </remarks>
    /// <seealso cref="StartBenchmark"/>
    public BenchmarkResults EndBenchmark ()
    {
        Application.InstanceInitialized -= OnApplicationInstanceInitialized;
        Application.InstanceDisposed -= OnApplicationInstanceDisposed;

        lock (_timeoutLock)
        {
            if (_timeout is { })
            {
                _timeout = null;
            }
        }

        _benchmarkApp = null;

        return BenchmarkResults;
    }

    private List<Key>? _demoKeys;
    private int _currentDemoKey;

    private void OnApplicationInstanceInitialized (object? s, EventArgs<IApplication> a)
    {
        _benchmarkApp = a.Value;

        lock (_timeoutLock)
        {
            _timeout = _benchmarkApp.AddTimeout (TimeSpan.FromMilliseconds (BenchmarkTimeout), ForceCloseCallback);
        }

        _benchmarkApp.Iteration += OnApplicationOnIteration;
        _benchmarkApp.Driver!.ClearedContents += OnClearedContents;
        _benchmarkApp.SessionBegun += OnApplicationSessionBegun;

        _stopwatch = Stopwatch.StartNew ();
    }

    private void OnClearedContents (object? sender, EventArgs args) => BenchmarkResults.ClearedContentCount++;

    private void OnApplicationInstanceDisposed (object? s, EventArgs<IApplication> a)
    {
        if (a.Value != _benchmarkApp)
        {
            return;
        }

        _benchmarkApp.Driver!.ClearedContents -= OnClearedContents;
        _benchmarkApp.SessionBegun -= OnApplicationSessionBegun;
        _benchmarkApp.Iteration -= OnApplicationOnIteration;
        BenchmarkResults.Duration = _stopwatch!.Elapsed;
        _stopwatch?.Stop ();
    }

    private void OnApplicationOnIteration (object? s, EventArgs<IApplication?> a)
    {
        BenchmarkResults.IterationCount++;

        if (BenchmarkResults.IterationCount > BENCHMARK_MAX_NATURAL_ITERATIONS + _demoKeys!.Count * BENCHMARK_KEY_PACING)
        {
            a.Value?.RequestStop ();
        }
    }

    private void OnApplicationSessionBegun (object? sender, SessionTokenEventArgs e)
    {
        SubscribeAllSubViews (_benchmarkApp!.TopRunnableView!);

        _demoKeys = GetDemoKeyStrokes (_benchmarkApp);

        _benchmarkApp.AddTimeout (new TimeSpan (0, 0, 0, 0, BENCHMARK_KEY_PACING),
                                  () =>
                                  {
                                      if (_currentDemoKey >= _demoKeys.Count)
                                      {
                                          return false;
                                      }

                                      _benchmarkApp.InjectKey (_demoKeys [_currentDemoKey++]);

                                      return true;
                                  });

        return;

        // Get a list of all subviews under Application.TopRunnable (and their subviews, etc.)
        // and subscribe to their DrawComplete event
        void SubscribeAllSubViews (View view)
        {
            view.DrawComplete += (_, _) => BenchmarkResults.DrawCompleteCount++;
            view.SubViewsLaidOut += (_, _) => BenchmarkResults.LaidOutCount++;

            foreach (View subview in view.SubViews)
            {
                SubscribeAllSubViews (subview);
            }
        }
    }

    // If the scenario doesn't close within the abort time, this will force it to quit
    private bool ForceCloseCallback ()
    {
        lock (_timeoutLock)
        {
            if (_timeout is { })
            {
                _timeout = null;
            }
        }

        Logging.Warning ($@"  Failed to Quit with {
            Application.GetDefaultKey (Command.Quit)
        } after {
            BenchmarkTimeout
        }ms and {
            BenchmarkResults.IterationCount
        } iterations. Force quit.");

        _benchmarkApp?.RequestStop ();

        return false;
    }

    /// <summary>Gets the Scenario Name + Description with the Description padded based on the longest known Scenario name.</summary>
    /// <returns></returns>
    public override string ToString () => $"{GetName ().PadRight (_maxScenarioNameLen)} {GetDescription ()}";

    #region IDispose

    public void Dispose ()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose (true);
        GC.SuppressFinalize (this);
    }

    protected virtual void Dispose (bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            { }

            _disposedValue = true;
        }
    }

    #endregion IDispose

    /// <summary>Returns a list of all Categories set by all of the <see cref="Scenario"/>s defined in the project.</summary>
    internal static ObservableCollection<string> GetAllCategories ()
    {
        List<string> aCategories = [];

        aCategories = typeof (Scenario).Assembly.GetTypes ()
                                       .Where (myType => myType is { IsClass: true, IsAbstract: false } && myType.IsSubclassOf (typeof (Scenario)))
                                       .Select (type => System.Attribute.GetCustomAttributes (type).ToList ())
                                       .Aggregate (aCategories,
                                                   (current, attrs) => current
                                                                       .Union (attrs.Where (a => a is ScenarioCategory)
                                                                                    .Select (a => ((ScenarioCategory)a).Name))
                                                                       .ToList ());

        // Sort
        ObservableCollection<string> categories = new (aCategories.OrderBy (c => c).ToList ());

        // Put "All" at the top
        categories.Insert (0, "All Scenarios");

        return categories;
    }

    /// <summary>
    ///     Gets a list of keystrokes to simulate during benchmarking to exercise the scenario's UI.
    /// </summary>
    /// <param name="app">The application instance running the scenario.</param>
    /// <returns>
    ///     A list of <see cref="Key"/> values to be simulated during benchmarking. Override this method
    ///     to provide scenario-specific key sequences that exercise the UI.
    /// </returns>
    /// <remarks>
    ///     During benchmarking, these keys are raised at intervals defined by the benchmark pacing constant
    ///     to simulate user interaction. This allows scenarios to demonstrate their full functionality
    ///     during automated benchmark runs.
    /// </remarks>
    public virtual List<Key> GetDemoKeyStrokes (IApplication? app) => [];
}
