namespace Terminal.Gui.Drivers;

/// <summary>
///     Provides methods to create and manage a fake application for testing purposes.
/// </summary>
public class FakeApplicationFactory
{
    /// <summary>
    ///     Creates an initialized fake application which will be cleaned up when result object
    ///     is disposed.
    /// </summary>
    /// <returns></returns>
    public IDisposable SetupFakeApplication ()
    {
        CancellationTokenSource hardStopTokenSource = new CancellationTokenSource ();
        AnsiInput ansiInput = new AnsiInput ();
        ansiInput.ExternalCancellationTokenSource = hardStopTokenSource;
        AnsiOutput output = new ();
        output.SetSize (80, 25);

        SizeMonitorImpl sizeMonitor = new (output);

        ApplicationImpl impl = new (new AnsiComponentFactory (ansiInput, output, sizeMonitor));
        ApplicationImpl.SetInstance (impl);

        // Initialize with a ANSI driver
        impl.Init (DriverRegistry.Names.ANSI);
        impl.Driver!.Clipboard = new FakeClipboard ();

        return new FakeApplicationLifecycle (impl, hardStopTokenSource);
    }
}
