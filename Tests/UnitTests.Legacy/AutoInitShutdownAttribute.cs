using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Xunit.v3;

namespace UnitTests;

/// <summary>
///     Enables test functions annotated with the [AutoInitShutdown] attribute to
///     automatically call Application.Init at start of the test and Application.Shutdown after the
///     test exits.
///     This is necessary because a) Application is a singleton and Init/Shutdown must be called
///     as a pair, and b) all unit test functions should be atomic..
/// </summary>
[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method)]
public class AutoInitShutdownAttribute : BeforeAfterTestAttribute
{
    /// <summary>
    ///     Initializes a [AutoInitShutdown] attribute, which determines if/how Application.Init and Application.Shutdown
    ///     are automatically called Before/After a test runs.
    /// </summary>
    /// <param name="autoInit">If true, Application.Init will be called Before the test runs.</param>
    /// <param name="forceDriver">
    ///     Forces the specified driver to be used when Application.Init is called. If not specified ANSI Driver will be used.
    ///     Only valid if
    ///     <paramref name="autoInit"/> is true.
    /// </param>
    public AutoInitShutdownAttribute (
        bool autoInit = true,
        string forceDriver = null
    )
    {
        _autoInit = autoInit;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo ("en-US");
        _forceDriver = forceDriver;
    }

    private readonly string _forceDriver;
    private IDisposable _v2Cleanup;

    public override void After (MethodInfo methodUnderTest, IXunitTest test)
    {
        Debug.WriteLine ($"After: {methodUnderTest?.Name ?? "Unknown Test"}");

        // Turn off diagnostic flags in case some test left them on
        View.Diagnostics = ViewDiagnosticFlags.Off;

        _v2Cleanup?.Dispose ();

        if (_autoInit)
        {
            try
            {
                Application.Shutdown ();
#if DEBUG_IDISPOSABLE
                if (View.Instances.Count == 0)
                {
                    Assert.Empty (View.Instances);
                }
                else
                {
                    View.Instances.Clear ();
                }
#endif
            }

            //catch (Exception e)
            //{
            //    Debug.WriteLine ($"Application.Shutdown threw an exception after the test exited: {e}");
            //}
            finally
            {
#if DEBUG_IDISPOSABLE
                View.Instances.Clear ();
                Application.ResetState (true);
#endif
                ApplicationImpl.SetInstance (null);
            }
        }

    }

    public override void Before (MethodInfo methodUnderTest, IXunitTest test)
    {
        Debug.WriteLine ($"Before: {methodUnderTest?.Name ?? "Unknown Test"}");



        if (_autoInit)
        {
#if DEBUG_IDISPOSABLE
            View.EnableDebugIDisposableAsserts = true;

            // Clear out any lingering Responder instances from previous tests
            if (View.Instances.Count == 0)
            {
                Assert.Empty (View.Instances);
            }
            else
            {
                View.Instances.Clear ();
            }
#endif
            if (string.IsNullOrEmpty (_forceDriver) || _forceDriver.ToLowerInvariant () == DriverRegistry.Names.ANSI)
            {
                var fa = new FakeApplicationFactory ();
                _v2Cleanup = fa.SetupFakeApplication ();
            }
            else
            {
                Assert.Fail ("Specifying driver name not yet supported");

                //Application.Init ((IDriver)Activator.CreateInstance (_forceDriver));
            }
        }
    }

    private bool _autoInit { get; }

    /// <summary>
    ///     Runs a single iteration of the main loop (layout, draw, run timed events etc.)
    /// </summary>
    public static void RunIteration ()
    {
        var a = (ApplicationImpl)ApplicationImpl.Instance;
        a.Coordinator?.RunIteration ();
    }
}
