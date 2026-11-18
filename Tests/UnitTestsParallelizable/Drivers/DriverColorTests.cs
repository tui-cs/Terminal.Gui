// Alias Console to MockConsole so we don't accidentally use Console

using UnitTests;

namespace DriverTests;

[Collection ("Driver Tests")]
public class DriverColorTests : TestDriverBase
{
    [Fact]
    public void Force16Colors_Sets ()
    {
        IDriver driver = CreateTestDriver ();

        driver.Force16Colors = true;
        Assert.True (driver.Force16Colors);

        driver.Dispose ();
    }
}
