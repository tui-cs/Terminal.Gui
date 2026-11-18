// Copilot

using UnitTests;

namespace IntegrationTests;

/// <summary>
///     Verifies that all View types dispose properly after being run in an Application.
///     This test must run in IntegrationTests (non-parallel) because View.Instances is static.
/// </summary>
public class AllViewsTests (ITestOutputHelper output) : TestsAllViews
{
    [Theory]
    [MemberData (nameof (AllViewTypes))]
    public void AllViews_Dispose_Properly (Type viewType)
    {
        View? view = CreateInstanceIfNotGeneric (viewType);

        if (view is null)
        {
            output.WriteLine ($"Ignoring {viewType} - It's a Generic");

            return;
        }

#if DEBUG_IDISPOSABLE
        View.EnableDebugIDisposableAsserts = true;
        View.Instances.Clear ();
#endif

        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.StopAfterFirstIteration = true;

        Runnable runnable = new ();
        runnable.Add (view);

        app.Run (runnable);

        runnable.Dispose ();
        app.Dispose ();

#if DEBUG_IDISPOSABLE
        List<View> leakedViews = View.Instances.Where (v => !v.WasDisposed).ToList ();

        foreach (View leaked in leakedViews)
        {
            output.WriteLine ($"  NOT DISPOSED: {leaked.GetType ().Name} - {leaked.ToDebugString ()}");
        }

        View.Instances.Clear ();

        Assert.Empty (leakedViews);
#endif
    }
}
