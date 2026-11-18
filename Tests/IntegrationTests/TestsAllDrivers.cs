namespace IntegrationTests;

public abstract class TestsAllDrivers
{
    /// <summary>
    ///     Gets all registered driver names for use in Theory tests.
    /// </summary>
    public static IEnumerable<object []> GetAllDriverNames ()
    {
        return DriverRegistry.GetDriverNames ().Select (name => new object [] { name });
    }
}
