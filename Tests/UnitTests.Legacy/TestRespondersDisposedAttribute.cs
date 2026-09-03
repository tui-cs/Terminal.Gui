using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Xunit.v3;

namespace UnitTests;

/// <summary>
///     Enables test functions annotated with the [TestRespondersDisposed] attribute to ensure all Views are disposed.
/// </summary>
/// <remarks>
///     On Before, sets Configuration.Locations to ConfigLocations.DefaultOnly.
///     On After, sets Configuration.Locations to ConfigLocations.All.
/// </remarks>
[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method)]
public class TestRespondersDisposedAttribute : BeforeAfterTestAttribute
{
    public TestRespondersDisposedAttribute () => CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo ("en-US");

    public override void After (MethodInfo methodUnderTest, IXunitTest test)
    {
        Debug.WriteLine ($"After: {methodUnderTest.Name}");

        base.After (methodUnderTest, test);

#if DEBUG_IDISPOSABLE
        Assert.True (View.EnableDebugIDisposableAsserts);
        Assert.Empty (View.Instances);
#endif
    }

    public override void Before (MethodInfo methodUnderTest, IXunitTest test)
    {
        Debug.WriteLine ($"Before: {methodUnderTest.Name}");

        base.Before (methodUnderTest, test);
#if DEBUG_IDISPOSABLE
        View.EnableDebugIDisposableAsserts = true;
        // Clear out any lingering Responder instances from previous tests
        View.Instances.Clear ();
        Assert.Empty (View.Instances);
#endif
    }
}