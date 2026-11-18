using System.Globalization;
using System.Reflection;
using Xunit.v3;

namespace UnitTests_Parallelizable;

[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method)]
public class TestDateAttribute : BeforeAfterTestAttribute
{
    public TestDateAttribute () => CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
    private readonly CultureInfo _currentCulture = CultureInfo.CurrentCulture;

    public override void After (MethodInfo methodUnderTest, IXunitTest test)
    {
        CultureInfo.CurrentCulture = _currentCulture;
        Assert.Equal (CultureInfo.CurrentCulture, _currentCulture);
    }

    public override void Before (MethodInfo methodUnderTest, IXunitTest test)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        Assert.Equal (CultureInfo.CurrentCulture, CultureInfo.InvariantCulture);
    }
}
