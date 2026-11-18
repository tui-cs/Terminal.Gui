#nullable enable
using UnitTests;

namespace DriverTests;

[Collection ("Driver Tests")]
public class LegacyConsoleTests : TestDriverBase
{
    [Fact]
    public void IsLegacyConsole_Returns_Expected_Values ()
    {
        IDriver? driver = CreateTestDriver ();
        Assert.False (driver.IsLegacyConsole);
    }

    [Fact]
    public void IsLegacyConsole_False_Force16Colors_True_False ()
    {
        IDriver? driver = CreateTestDriver ();

        Assert.False (driver.IsLegacyConsole);
        Assert.False (driver.Force16Colors);

        driver.Force16Colors = true;
        Assert.False (driver.IsLegacyConsole);
        Assert.True (driver.Force16Colors);
    }

    [Fact]
    public void IsLegacyConsole_True_Force16Colors_Is_Always_True ()
    {
        IDriver? driver = CreateTestDriver ();

        Assert.False (driver.IsLegacyConsole);
        Assert.False (driver.Force16Colors);

        driver.IsLegacyConsole = true;
        Assert.True (driver.Force16Colors);

        driver.Force16Colors = false;
        Assert.True (driver.Force16Colors);
    }

    [Fact]
    public void IsLegacyConsole_True_False_SupportsTrueColor_Is_Always_True_False ()
    {
        IDriver? driver = CreateTestDriver ();

        Assert.False (driver.IsLegacyConsole);
        Assert.True (driver.SupportsTrueColor);

        driver.IsLegacyConsole = true;
        Assert.False (driver.SupportsTrueColor);
    }
}
