#nullable enable

namespace UnitTests;

/// <summary>
///     Enables tests to create an instance of the ANSI driver configured for testing purposes.
///     The driver will use the <see cref="FakeClipboard"/>.
/// </summary>

public abstract class TestDriverBase
{
    /// <summary>
    ///     Gets all registered driver names for use in Theory tests.
    /// </summary>
    public static IEnumerable<object []> GetAllDriverNames ()
    {
        return DriverRegistry.GetDriverNames ().Select (name => new object [] { name });
    }

    /// <summary>
    ///     Creates a new ANSI driver instance with the specified buffer size.
    ///     This is a convenience method for tests that need to use Draw() and DriverAssert
    ///     without relying on Application.Driver.
    /// </summary>
    /// <param name="width">Width of the driver buffer</param>
    /// <param name="height">Height of the driver buffer</param>
    /// <returns>A configured IDriver instance</returns>
    protected static IDriver CreateTestDriver (int width = 80, int height = 25)
    {
        SystemTimeProvider timeProvider = new ();
        AnsiOutput output = new ();
        AnsiComponentFactory factory = new (null, output, null);
        AnsiResponseParser parser = new (timeProvider);
        AnsiRequestScheduler scheduler = new (parser);
        ISizeMonitor sizeMonitor = factory.CreateSizeMonitor (output, new OutputBufferImpl ());

        DriverImpl driver = new (
                                 factory,
                                 new AnsiInputProcessor (null!, timeProvider),
                                 new OutputBufferImpl (),
                                 output,
                                 scheduler,
                                 sizeMonitor);

        // Initialize the size monitor with the driver (generic pattern for all drivers)
        sizeMonitor.Initialize (driver);

        driver.SetScreenSize (width, height);
        driver.Clipboard = new FakeClipboard ();

        return driver;
    }

    protected static IApplication RunTestApplication (int width = 80, int height = 25, EventHandler<EventArgs<IApplication?>>? iterationHandler = null, bool stopAfterFirstIteration = true, ITestOutputHelper? output = null)
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (width, height);
        // Use fake clipboard for testing to avoid contamination of the system clipboard
        app.Driver!.Clipboard = new FakeClipboard ();
        using Runnable runnable = new Runnable ();
        // Give the runnable a border to help visually separate it
        runnable.BorderStyle = LineStyle.Dotted;

        app.StopAfterFirstIteration = stopAfterFirstIteration;

        try
        {
            app.Iteration += iterationHandler;
            app.Run (runnable);
        }
        catch (Exception ex)
        {
            output?.WriteLine ($"Exception: {ex}");

            throw;
        }
        finally
        {
            app.Iteration -= iterationHandler;

        }
        return app;
    }
}
