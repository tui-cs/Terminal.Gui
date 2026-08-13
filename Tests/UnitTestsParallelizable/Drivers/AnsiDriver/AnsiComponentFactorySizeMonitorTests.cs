using Moq;
using Terminal.Gui.Configuration;

namespace DriverTests.Ansi;

// Copilot

/// <summary>
///     Tests for <see cref="AnsiComponentFactory.CreateSizeMonitor"/> to verify the correct
///     <see cref="ISizeMonitor"/> implementation is selected based on <see cref="Driver.SizeDetection"/>
///     and whether an injected monitor is present.
/// </summary>
[Collection ("Driver Tests")]
public class AnsiComponentFactorySizeMonitorTests
{
    /// <summary>
    ///     When a size monitor is provided to the constructor it must be returned as-is,
    ///     regardless of <see cref="Driver.SizeDetection"/>.
    /// </summary>
    [Fact]
    public void CreateSizeMonitor_InjectedMonitor_IsReturnedDirectly ()
    {
        AnsiOutput output = new ();
        output.SetSize (80, 25);

        Mock<ISizeMonitor> injected = new ();

        AnsiComponentFactory factory = new (null, output, injected.Object);

        ISizeMonitor result = factory.CreateSizeMonitor (output, Mock.Of<IOutputBuffer> ());

        Assert.Same (injected.Object, result);
    }

    /// <summary>
    ///     When <see cref="SizeDetectionMode.AnsiQuery"/> is explicitly selected,
    ///     the size monitor must queue an ANSI terminal-size query when initialized.
    /// </summary>
    [Fact]
    public void CreateSizeMonitor_AnsiQuery_QueuesAnsiSizeQuery ()
    {
        SizeDetectionMode saved = Driver.SizeDetection;

        try
        {
            Driver.SizeDetection = SizeDetectionMode.AnsiQuery;

            AnsiOutput output = new ();
            output.SetSize (80, 25);

            AnsiComponentFactory factory = new ();

            ISizeMonitor monitor = factory.CreateSizeMonitor (output, Mock.Of<IOutputBuffer> ());

            List<AnsiEscapeSequenceRequest> queuedRequests = [];
            Mock<IDriver> driver = new ();
            driver.Setup (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()))
                  .Callback<AnsiEscapeSequenceRequest> (queuedRequests.Add);

            monitor.Initialize (driver.Object);

            Assert.Contains (queuedRequests,
                             request => request.Request == EscSeqUtils.CSI_ReportWindowSizeInChars.Request);
        }
        finally
        {
            Driver.SizeDetection = saved;
        }
    }

    // Codex - GPT-5 Codex
    /// <summary>
    ///     The default ANSI size monitor must observe native size changes without queuing
    ///     terminal-size queries.
    /// </summary>
    [Fact]
    public void CreateSizeMonitor_Default_DetectsNativeResize_WithoutAnsiSizeQuery ()
    {
        SizeDetectionMode saved = Driver.SizeDetection;

        try
        {
            DriverSettings defaults = new ();
            Driver.SizeDetection = defaults.SizeDetection;

            AnsiOutput output = new ();
            Size reportedSize = new (80, 25);

            AnsiComponentFactory factory = new (null, output, null, () => reportedSize);
            ISizeMonitor monitor = factory.CreateSizeMonitor (output, Mock.Of<IOutputBuffer> ());

            List<AnsiEscapeSequenceRequest> queuedRequests = [];
            Mock<IDriver> driver = new ();
            driver.Setup (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()))
                  .Callback<AnsiEscapeSequenceRequest> (queuedRequests.Add);

            List<SizeChangedEventArgs> sizeChanges = [];
            monitor.SizeChanged += (_, e) => sizeChanges.Add (e);
            monitor.Initialize (driver.Object);

            Assert.Equal (reportedSize, output.GetSize ());
            Assert.False (monitor.Poll ());

            reportedSize = new Size (120, 40);

            Assert.True (monitor.Poll ());
            Assert.Single (sizeChanges);
            Assert.Equal (reportedSize, sizeChanges [0].Size);
            Assert.DoesNotContain (queuedRequests,
                                   request => request.Request == EscSeqUtils.CSI_ReportWindowSizeInChars.Request);
        }
        finally
        {
            Driver.SizeDetection = saved;
        }
    }

    // Codex - GPT-5 Codex
    [Fact]
    public void CreateOutput_Default_InitializesNativeSizeBeforeMainLoopSizing ()
    {
        SizeDetectionMode saved = Driver.SizeDetection;

        try
        {
            DriverSettings defaults = new ();
            Driver.SizeDetection = defaults.SizeDetection;

            AnsiOutput output = new ();
            Size reportedSize = new (120, 40);
            AnsiComponentFactory factory = new (null, output, null, () => reportedSize);

            IOutput createdOutput = factory.CreateOutput ();

            Assert.Same (output, createdOutput);
            Assert.Equal (reportedSize, createdOutput.GetSize ());
        }
        finally
        {
            Driver.SizeDetection = saved;
        }
    }

    /// <summary>
    ///     When <see cref="SizeDetectionMode.Polling"/> is active,
    ///     <see cref="AnsiComponentFactory.CreateSizeMonitor"/> should return an ANSI size monitor
    ///     configured to query the OS without sending terminal-size requests.
    /// </summary>
    [Fact]
    public void CreateSizeMonitor_Polling_ReturnsAnsiSizeMonitor_AndSetsNativeSizeQuery ()
    {
        SizeDetectionMode saved = Driver.SizeDetection;

        try
        {
            Driver.SizeDetection = SizeDetectionMode.Polling;

            AnsiOutput output = new ();
            Size reportedSize = new (80, 25);

            AnsiComponentFactory factory = new (null, output, null, () => reportedSize);

            ISizeMonitor result = factory.CreateSizeMonitor (output, Mock.Of<IOutputBuffer> ());

            Assert.IsType<AnsiSizeMonitor> (result);
            Assert.NotNull (output.NativeSizeQuery);
        }
        finally
        {
            Driver.SizeDetection = saved;
        }
    }

    // Codex - GPT-5 Codex
    [Fact]
    public void CreateSizeMonitor_PollingWithoutNativeSize_FallsBackToAnsiQuery ()
    {
        SizeDetectionMode saved = Driver.SizeDetection;

        try
        {
            Driver.SizeDetection = SizeDetectionMode.Polling;
            AnsiOutput output = new ();
            AnsiComponentFactory factory = new (null, output, null, () => null);
            ISizeMonitor monitor = factory.CreateSizeMonitor (output, Mock.Of<IOutputBuffer> ());

            List<AnsiEscapeSequenceRequest> queuedRequests = [];
            Mock<IDriver> driver = new ();
            driver.Setup (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()))
                  .Callback<AnsiEscapeSequenceRequest> (queuedRequests.Add);

            monitor.Initialize (driver.Object);

            Assert.Contains (queuedRequests,
                             request => request.Request == EscSeqUtils.CSI_ReportWindowSizeInChars.Request);
        }
        finally
        {
            Driver.SizeDetection = saved;
        }
    }

    /// <summary>
    ///     In <see cref="SizeDetectionMode.Polling"/> mode the <c>NativeSizeQuery</c> delegate
    ///     causes <see cref="AnsiOutput.GetSize"/> to return the OS-provided size rather than the
    ///     stale 80×25 cache, so <see cref="SizeMonitorImpl"/> correctly detects terminal resizes.
    /// </summary>
    [Fact]
    public void Polling_NativeSizeQuery_OverridesStaleCache ()
    {
        AnsiOutput output = new ();
        output.SetSize (80, 25); // cached, stale

        // Simulate OS reporting 120×40
        Size fakeOsSize = new (120, 40);
        output.NativeSizeQuery = () => fakeOsSize;

        Assert.Equal (fakeOsSize, output.GetSize ());
    }

    /// <summary>
    ///     Verifies that <see cref="AnsiComponentFactory.CreateNativeSizeQuery"/> returns a callable
    ///     delegate (non-null) on every supported platform.
    /// </summary>
    [Fact]
    public void CreateNativeSizeQuery_ReturnsNonNullDelegate ()
    {
        Func<Size?> query = AnsiComponentFactory.CreateNativeSizeQuery ();

        Assert.NotNull (query);

        // The delegate must be callable without throwing in a test environment.
        // It may return null when there is no real terminal, and that is fine.
        Size? size = null;
        Exception? ex = Record.Exception (() => { size = query (); });
        Assert.Null (ex);
    }

    /// <summary>
    ///     Validates the full pipeline: in <see cref="SizeDetectionMode.Polling"/> mode,
    ///     the <see cref="SizeMonitorImpl"/> wrapping the <see cref="AnsiOutput"/> fires
    ///     <see cref="ISizeMonitor.SizeChanged"/> when the OS size changes.
    /// </summary>
    [Fact]
    public void Polling_SizeMonitorImpl_FiresSizeChanged_WhenNativeSizeChanges ()
    {
        // Test the SizeMonitorImpl+AnsiOutput pipeline directly with a controllable NativeSizeQuery.
        AnsiOutput output = new ();
        output.SetSize (80, 25);

        // Wire up a fake native size query that starts at 80x25.
        Size reportedSize = new (80, 25);
        output.NativeSizeQuery = () => reportedSize;

        // Constructor captures current size so first Poll() is a no-op.
        SizeMonitorImpl monitor = new (output);

        List<SizeChangedEventArgs> events = [];
        monitor.SizeChanged += (_, e) => events.Add (e);

        // First poll: size unchanged (80x25) → no event.
        monitor.Poll ();
        Assert.Empty (events);

        // Simulate a terminal resize reported by the OS.
        reportedSize = new Size (120, 40);

        monitor.Poll ();

        Assert.Single (events);
        Assert.Equal (new Size (120, 40), events [0].Size);
    }

    /// <summary>
    ///     In <see cref="SizeDetectionMode.AnsiQuery"/> mode the injected-monitor code path
    ///     is still respected — injected monitors are always returned regardless of mode.
    /// </summary>
    [Fact]
    public void CreateSizeMonitor_InjectedMonitor_WinsOverMode ()
    {
        SizeDetectionMode saved = Driver.SizeDetection;

        try
        {
            foreach (SizeDetectionMode mode in Enum.GetValues<SizeDetectionMode> ())
            {
                Driver.SizeDetection = mode;

                AnsiOutput output = new ();
                output.SetSize (80, 25);

                Mock<ISizeMonitor> injected = new ();

                AnsiComponentFactory factory = new (null, output, injected.Object);

                ISizeMonitor result = factory.CreateSizeMonitor (output, Mock.Of<IOutputBuffer> ());

                Assert.Same (injected.Object, result);
            }
        }
        finally
        {
            Driver.SizeDetection = saved;
        }
    }
}
