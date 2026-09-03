using AppTestHelpers;

namespace AppTestHelpersTests;

public class AppTestHelperShutdownTests
{
    // CoPilot - Grok 4.6
    /// <summary>
    ///     Regression for the macOS IntegrationTests NRE on #5657:
    ///     WaitIteration used ExternalCancellationTokenSource! during Stop/Dispose.
    ///     CleanupApplication can null that source before Finished is set.
    /// </summary>
    [Fact]
    public void WaitIteration_WhenExternalCancellationTokenSourceIsNull_DoesNotThrow ()
    {
        using AppTestHelper helper = new (DriverRegistry.Names.DOTNET);

        helper.ClearExternalCancellationTokenSourceForTests ();

        Exception? ex = Record.Exception (() => helper.WaitIteration ());

        Assert.Null (ex);
    }
}
