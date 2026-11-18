// Copilot

using UICatalog;

namespace IntegrationTests;

/// <summary>
///     Verifies that all UICatalog scenarios properly dispose all Views on exit.
/// </summary>
public class UICatalogScenarioTests (ITestOutputHelper output)
{
    [Theory]
    [MemberData (nameof (AllScenarioTypes))]
    public void All_Scenarios_Dispose_Properly (Type scenarioType)
    {
#if DEBUG_IDISPOSABLE
        View.EnableDebugIDisposableAsserts = true;
        View.Instances.Clear ();
#endif
        Application.ResetState (true);

        uint abortTime = 3000;
        IApplication? app = null;
        var iterationCount = 0;

        output.WriteLine ($"Running Scenario '{scenarioType.Name}'");
        Scenario scenario = (Scenario)Activator.CreateInstance (scenarioType)!;

        Application.InstanceInitialized += OnInit;
        Application.InstanceDisposed += OnDisposed;
        Application.ForceDriver = DriverRegistry.Names.ANSI;

        scenario.Main ();
        scenario.Dispose ();

        Application.ForceDriver = string.Empty;
        Application.InstanceInitialized -= OnInit;
        Application.InstanceDisposed -= OnDisposed;

#if DEBUG_IDISPOSABLE
        List<View> leakedViews = View.Instances.Where (v => !v.WasDisposed).ToList ();

        foreach (View leaked in leakedViews)
        {
            output.WriteLine ($"  NOT DISPOSED: {leaked.GetType ().Name} - {leaked.ToDebugString ()}");
        }

        View.Instances.Clear ();

        Assert.Empty (leakedViews);
#endif

        return;

        void OnInit (object? s, EventArgs<IApplication> a)
        {
            app = a.Value;
            app.AddTimeout (TimeSpan.FromMilliseconds (abortTime), ForceClose);
            app.Iteration += OnIteration;
        }

        void OnDisposed (object? s, EventArgs<IApplication> a)
        {
            if (app is { })
            {
                app.Iteration -= OnIteration;
            }
        }

        void OnIteration (object? s, EventArgs<IApplication?> a)
        {
            iterationCount++;

            if (iterationCount > 5)
            {
                app?.RequestStop ();
            }
        }

        bool ForceClose ()
        {
            output.WriteLine ($"  Force-closing '{scenarioType.Name}' after {abortTime}ms");
            app?.RequestStop ();

            return false;
        }
    }

    public static IEnumerable<object []> AllScenarioTypes =>
        typeof (Scenario).Assembly.GetTypes ()
                         .Where (type => type is { IsClass: true, IsAbstract: false } && type.IsSubclassOf (typeof (Scenario)))
                         .Select (type => new object [] { type });
}
