using System.Diagnostics;
using UICatalog;

namespace StressTests;

public class ScenariosStressTests
{
    public ScenariosStressTests (ITestOutputHelper output)
    {
#if DEBUG_IDISPOSABLE
        View.EnableDebugIDisposableAsserts = true;
        View.Instances.Clear ();
#endif
        _output = output;
    }

    private readonly ITestOutputHelper _output;

    private object? _timeoutLock;

    /// <summary>
    ///     <para>
    ///         This runs through all Scenarios defined in UI Catalog, calling Init, Setup, and Run and measuring the perf of
    ///         each.
    ///     </para>
    /// </summary>
    [Theory]
    [MemberData (nameof (AllScenarioTypes))]
    public void All_Scenarios_Benchmark (Type scenarioType)
    {
        Assert.Null (_timeoutLock);
        _timeoutLock = new object ();

        // If a previous test failed, this will ensure that the Application is in a clean state
        Application.ResetState (true);

        uint maxIterations = 25;
        uint abortTime = 2000;
        object? timeout = null;
        IApplication? app = null;

        var iterationCount = 0;
        var clearedContentCount = 0;
        var refreshedCount = 0;
        var updatedCount = 0;
        var drawCompleteCount = 0;

        var addedCount = 0;
        var laidOutCount = 0;

        _output.WriteLine ($"Running Scenario '{scenarioType}'");
        var scenario = (Scenario)Activator.CreateInstance (scenarioType)!;
        string scenarioName = scenario.GetName ();

        Stopwatch? stopwatch = null;

        Application.InstanceInitialized += OnApplicationInstanceInitialized;
        Application.InstanceDisposed += OnApplicationInstanceDisposed;
        Application.ForceDriver = DriverRegistry.Names.ANSI;
        scenario.Main ();
        scenario.Dispose ();
        Application.ForceDriver = string.Empty;
        Application.InstanceInitialized -= OnApplicationInstanceInitialized;
        Application.InstanceDisposed -= OnApplicationInstanceDisposed;

        lock (_timeoutLock)
        {
            if (timeout is { })
            {
                timeout = null;
            }
        }

        lock (_timeoutLock)
        {
            _timeoutLock = null;
        }

        _output.WriteLine ($"Scenario {scenarioType}");
        _output.WriteLine ($"  took {stopwatch!.ElapsedMilliseconds} ms to run.");
        _output.WriteLine ($"  called Driver.ClearContents {clearedContentCount} times.");
        _output.WriteLine ($"  called Driver.Refresh {refreshedCount} times.");
        _output.WriteLine ($"    which updated the screen {updatedCount} times.");
        _output.WriteLine ($"  called View.Draw {drawCompleteCount} times.");
        _output.WriteLine ($"  added {addedCount} views.");
        _output.WriteLine ($"  called View.LayoutComplete {laidOutCount} times.");

        return;

        void OnApplicationInstanceInitialized (object? s, EventArgs<IApplication> a)
        {
            app = a.Value;

            lock (_timeoutLock)
            {
                timeout = app.AddTimeout (TimeSpan.FromMilliseconds (abortTime), ForceCloseCallback);
            }

            app.Iteration += OnApplicationOnIteration;

            if (app.Driver is { })
            {
                app.Driver.ClearedContents += OnClearedContents;
            }

            app.SessionBegun += OnApplicationSessionBegun;

            stopwatch = Stopwatch.StartNew ();
            _output.WriteLine ("Application instance initialized");
        }

        void OnClearedContents (object? sender, EventArgs args) => clearedContentCount++;

        void OnApplicationInstanceDisposed (object? s, EventArgs<IApplication> a)
        {
            if (app is null)
            {
                return;
            }

            if (app.Driver is { })
            {
                app.Driver.ClearedContents -= OnClearedContents;
            }

            app.SessionBegun -= OnApplicationSessionBegun;
            app.Iteration -= OnApplicationOnIteration;

            stopwatch?.Stop ();

            _output.WriteLine ("Application instance disposed");
        }

        void OnApplicationOnIteration (object? s, EventArgs<IApplication?> a)
        {
            iterationCount++;

            if (iterationCount <= maxIterations)
            {
                return;
            }

            // Press QuitKey
            _output.WriteLine ("Attempting to quit scenario with RequestStop");
            app?.RequestStop ();
        }

        void OnApplicationSessionBegun (object? sender, SessionTokenEventArgs e)
        {
            if (app?.TopRunnableView is { })
            {
                SubscribeAllSubViews (app.TopRunnableView);
            }

            return;

            // Get a list of all subviews under Application.TopRunnable (and their subviews, etc.)
            // and subscribe to their DrawComplete event
            void SubscribeAllSubViews (View view)
            {
                view.DrawComplete += (_, _) => drawCompleteCount++;
                view.SubViewsLaidOut += (_, _) => laidOutCount++;
                view.SuperViewChanged += (_, _) => addedCount++;

                foreach (View subview in view.SubViews)
                {
                    SubscribeAllSubViews (subview);
                }
            }
        }

        // If the scenario doesn't close within the abort time, this will force it to quit
        bool ForceCloseCallback ()
        {
            lock (_timeoutLock)
            {
                if (timeout is { })
                {
                    timeout = null;
                }
            }

            _output.WriteLine ($"'{
                scenarioName
            }' failed to Quit with {
                Application.GetDefaultKey (Command.Quit)
            } after {
                abortTime
            }ms and {
                iterationCount
            } iterations. Force quit.");

            app?.RequestStop ();

            return false;
        }
    }

    public static IEnumerable<object []> AllScenarioTypes =>
        typeof (Scenario).Assembly.GetTypes ()
                         .Where (type => type is { IsClass: true, IsAbstract: false } && type.IsSubclassOf (typeof (Scenario)))
                         .Select (type => new object [] { type });
}
