using Terminal.Gui.App;
using UICatalog;

namespace Terminal.Gui.Benchmarks.ViewBase;

/// <summary>
///     Measures memory allocated when running each UICatalog <see cref="Scenario"/>.
///     Uses <see cref="Scenario.StartBenchmark"/> for auto-quit with timeout,
///     plus <see cref="IApplication.StopAfterFirstIteration"/> to exit after one main-loop iteration.
/// </summary>
/// <remarks>
///     <para>
///         Run:
///         <code>dotnet run --project Tests/Benchmarks -c Release -- scenarios</code>
///     </para>
///     <para>
///         Export to file for comparison:
///         <code>dotnet run --project Tests/Benchmarks -c Release -- scenarios &gt; scenarios.md</code>
///     </para>
/// </remarks>
public static class ScenarioMemoryBenchmark
{
    private const uint TIMEOUT_MS = 5000;

    /// <summary>
    ///     Runs each UICatalog scenario for one iteration and reports memory allocated.
    /// </summary>
    public static void Run ()
    {
        Scenario.BenchmarkTimeout = TIMEOUT_MS;

        List<Scenario> scenarios = [.. Scenario.GetScenarios ().OrderBy (s => s.GetName ())];

        List<(string Name, long Bytes, bool Failed)> results = [];

        foreach (Scenario scenarioTemplate in scenarios)
        {
            string name = scenarioTemplate.GetName ();

            // Create a fresh instance for each run
            Scenario? scenario = Activator.CreateInstance (scenarioTemplate.GetType ()) as Scenario;

            if (scenario is null)
            {
                continue;
            }

            IApplication? appInstance = null;

            void onInitialized (object? sender, EventArgs<IApplication> e)
            {
                appInstance = e.Value;
                appInstance.StopAfterFirstIteration = true;
            }

            Application.InstanceInitialized += onInitialized;

            // Hard timeout: if StopAfterFirstIteration doesn't work, force RequestStop
            using CancellationTokenSource cts = new (TimeSpan.FromMilliseconds (TIMEOUT_MS));

            cts.Token.Register (() =>
            {
                try
                {
                    appInstance?.RequestStop ();
                }
                catch
                {
                    // Ignore — app may already be disposed
                }
            });

            try
            {
                scenario.StartBenchmark ();

                GC.Collect ();
                GC.WaitForPendingFinalizers ();
                GC.Collect ();

                long before = GC.GetAllocatedBytesForCurrentThread ();
                scenario.Main ();
                long after = GC.GetAllocatedBytesForCurrentThread ();

                scenario.EndBenchmark ();

                results.Add ((name, after - before, false));
                Console.Error.Write (".");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine ($"\n  FAILED: {name} — {ex.GetType ().Name}: {ex.Message}");
                results.Add ((name, 0, true));
            }
            finally
            {
                Application.InstanceInitialized -= onInitialized;
                scenario.Dispose ();
                appInstance = null;
            }
        }

        Console.Error.WriteLine ();

        // Print markdown table sorted by allocation size descending
        List<(string Name, long Bytes, bool Failed)> succeeded = results.Where (r => !r.Failed).ToList ();
        succeeded.Sort ((a, b) => b.Bytes.CompareTo (a.Bytes));

        Console.WriteLine ("| Scenario | Allocated (bytes) |");
        Console.WriteLine ("|----------|------------------:|");

        long total = 0;

        foreach ((string name, long bytes, _) in succeeded)
        {
            Console.WriteLine ($"| {name,-50} | {bytes,17:N0} |");
            total += bytes;
        }

        if (results.Any (r => r.Failed))
        {
            Console.WriteLine ($"| {"**Failed**",-50} | {"N/A",17} |");

            foreach ((string name, _, _) in results.Where (r => r.Failed))
            {
                Console.WriteLine ($"| {"  " + name,-50} | {"—",17} |");
            }
        }

        Console.WriteLine ($"| {"**Total**",-50} | {total,17:N0} |");
        Console.WriteLine ($"| {"**Average**",-50} | {(succeeded.Count > 0 ? total / succeeded.Count : 0),17:N0} |");
        Console.WriteLine ($"| {"**Scenario count**",-50} | {succeeded.Count,17:N0} |");
    }
}
