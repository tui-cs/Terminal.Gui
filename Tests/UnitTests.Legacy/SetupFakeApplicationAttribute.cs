#nullable enable
using System.Diagnostics;
using System.Reflection;
using Xunit.v3;

namespace UnitTests;

/// <summary>
///     Enables test functions annotated with the [SetupFakeApplication] attribute to set Application.Driver to new
///     ANSI driver. The driver is set up with 80 rows and 25 columns.
/// </summary>
[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method)]
public class SetupFakeApplicationAttribute : BeforeAfterTestAttribute
{
    private IDisposable? _appDispose;

    public override void Before (MethodInfo methodUnderTest, IXunitTest test)
    {
        Debug.WriteLine ($"Before: {methodUnderTest.Name}");

        var appFactory = new FakeApplicationFactory ();
        _appDispose = appFactory.SetupFakeApplication ();

        base.Before (methodUnderTest, test);
    }

    public override void After (MethodInfo methodUnderTest, IXunitTest test)
    {
        Debug.WriteLine ($"After: {methodUnderTest.Name}");

        _appDispose?.Dispose ();
        _appDispose = null;

        // TODO: This is troublesome; it seems to cause tests to hang when enabled, but shouldn't have any impact.
        // TODO: Uncomment after investigation.
        //ApplicationImpl.SetInstance (null);

        base.After (methodUnderTest, test);
    }

    /// <summary>
    ///     Runs a single iteration of the main loop (layout, draw, run timed events etc.)
    /// </summary>
    public static void RunIteration () { ((ApplicationImpl)ApplicationImpl.Instance).Coordinator?.RunIteration (); }
}
