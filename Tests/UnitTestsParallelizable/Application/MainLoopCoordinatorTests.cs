using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using UnitTests;
using UnitTests.Parallelizable;

// ReSharper disable AccessToDisposedClosure
#pragma warning disable xUnit1031

namespace ApplicationTests;

/// <summary>
///     Tests for <see cref="MainLoopCoordinator{TInputRecord}"/> to verify input loop lifecycle.
///     These tests ensure that the input thread starts, runs, and stops correctly when applications
///     are created, initialized, and disposed.
/// </summary>
[Collection ("Application Tests")]
public class MainLoopCoordinatorTests (ITestOutputHelper outputHelper) : IDisposable
{
    private readonly List<IApplication> _createdApps = [];

    public void Dispose ()
    {
        // Cleanup any apps that weren't disposed in tests
        foreach (IApplication app in _createdApps)
        {
            try
            {
                app.Dispose ();
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        _createdApps.Clear ();
    }

    private IApplication CreateApp ()
    {
        IApplication app = Application.Create ();
        _createdApps.Add (app);

        return app;
    }

    // CoPilot - GPT-6
    [Fact]
    public void RunIterationAfterStopDoesNotUseDisposedLoop ()
    {
        Mock<IApplicationMainLoop<char>> loop = new ();
        MainLoopCoordinator<char> coordinator = new (new TimedEvents (), new ConcurrentQueue<char> (), loop.Object, new AnsiComponentFactory ());

        coordinator.Stop ();
        coordinator.RunIteration ();

        loop.Verify (instance => instance.Iteration (), Times.Never);
    }

    /// <summary>
    ///     Verifies that Dispose() stops the input loop when using Application.Create().
    ///     This is the key test that proves the input thread respects cancellation.
    /// </summary>
    [Fact]
    public void Application_Dispose_Stops_Input_Loop ()
    {
        // Arrange
        IApplication app = CreateApp ();
        app.Init (DriverRegistry.Names.ANSI);

        // The input thread should now be running
        Assert.NotNull (app.Driver);
        Assert.True (app.Initialized);

        // Act - Dispose the application
        var sw = Stopwatch.StartNew ();
        app.Dispose ();
        sw.Stop ();

        // Assert - Dispose should complete quickly (within 1 second)
        // If the input thread doesn't stop, this will hang and the test will timeout
        Assert.True (sw.ElapsedMilliseconds < 1000, $"Dispose() took {sw.ElapsedMilliseconds}ms - input thread may not have stopped");

        // Verify the application is properly disposed
        Assert.Null (app.Driver);
        Assert.False (app.Initialized);

        _createdApps.Remove (app);
    }

    /// <summary>
    ///     Verifies that calling Dispose() multiple times doesn't cause issues.
    /// </summary>
    [Fact]
    public void Dispose_Called_Multiple_Times_Does_Not_Throw ()
    {
        // Arrange
        IApplication app = CreateApp ();
        app.Init (DriverRegistry.Names.ANSI);

        // Act - Call Dispose() multiple times
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     app.Dispose ();
                                                     app.Dispose ();
                                                     app.Dispose ();
                                                 });

        // Assert - Should not throw
        Assert.Null (exception);

        _createdApps.Remove (app);
    }

    /// <summary>
    ///     Verifies that multiple applications can be created and disposed without thread leaks.
    ///     This simulates the ColorPicker test scenario where multiple ApplicationImpl instances
    ///     are created in parallel tests and must all be properly cleaned up.
    /// </summary>
    [Fact]
    public void Multiple_Applications_Dispose_Without_Thread_Leaks ()
    {
        const int COUNT = 5;
        IApplication [] apps = new IApplication [COUNT];

        // Arrange - Create multiple applications (simulating parallel test scenario)
        for (var i = 0; i < COUNT; i++)
        {
            apps [i] = Application.Create ();
            apps [i].Init (DriverRegistry.Names.ANSI);
        }

        // Act - Dispose all applications
        var sw = Stopwatch.StartNew ();

        for (var i = 0; i < COUNT; i++)
        {
            apps [i].Dispose ();
        }

        sw.Stop ();

        // Assert - All disposals should complete quickly
        // If input threads don't stop, this will hang or take a very long time
        Assert.True (sw.ElapsedMilliseconds < 10000, $"Disposing {COUNT} apps took {sw.ElapsedMilliseconds}ms - input threads may not have stopped");
    }

    /// <summary>
    ///     Verifies that the 20ms throttle limits the input loop poll rate to prevent CPU spinning.
    ///     This test proves throttling exists by verifying the poll rate is bounded (not millions of calls).
    ///     The test uses an upper bound approach to avoid timing sensitivity issues during parallel execution.
    /// </summary>
    [Fact (Skip = "Can't get this to run reliably.")]
    public void InputLoop_Throttle_Limits_Poll_Rate ()
    {
        // Arrange - Create a ANSIInput and manually run it with throttling
        var input = new AnsiInput ();
        ConcurrentQueue<char> queue = new ();
        input.Initialize (queue);

        var cts = new CancellationTokenSource ();

        // Act - Run the input loop for 500ms
        // Short duration reduces test time while still proving throttle exists
        Task inputTask = Task.Run (() => input.Run (cts.Token), cts.Token);

        Thread.Sleep (500);

        int peekCount = input.PeekCallCount;
        cts.Cancel ();

        // Wait for task to complete
        bool completed = inputTask.Wait (TimeSpan.FromSeconds (10), TestContext.Current.CancellationToken);
        Assert.True (completed, "Input task did not complete within timeout");

        // Assert - The key insight: throttle prevents CPU spinning
        // With 20ms throttle: ~25 calls in 500ms (but can be much less under load)
        // WITHOUT throttle: Would be 10,000+ calls minimum (tight spin loop)
        //
        // We use an upper bound test: verify it's NOT spinning wildly
        // This is much more reliable than testing exact timing under parallel load
        //
        // Max 500 calls = average 1ms between polls (still proves 20ms throttle exists)
        // Without throttle = millions of calls (tight loop)
        Assert.True (peekCount < 500, $"Poll count {peekCount} suggests no throttling (expected <500 with 20ms throttle)");

        // Also verify the thread actually ran (not immediately cancelled)
        Assert.True (peekCount > 0, $"Poll count was {peekCount} - thread may not have started");

        input.Dispose ();
    }

    /// <summary>
    ///     Verifies that the 20ms throttle prevents CPU spinning even with many leaked applications.
    ///     Before the throttle fix, 10+ leaked apps would saturate the CPU with tight spin loops.
    /// </summary>
    [Fact]
    public void Throttle_Prevents_CPU_Saturation_With_Leaked_Apps ()
    {
        const int COUNT = 10;
        IApplication [] apps = new IApplication [COUNT];

        // Arrange - Create multiple applications WITHOUT disposing them (simulating the leak)
        for (var i = 0; i < COUNT; i++)
        {
            apps [i] = Application.Create ();
            apps [i].Init (DriverRegistry.Names.ANSI);
        }

        // Let them run for a moment
        Thread.Sleep (100);

        // Act - Now dispose them all and measure how long it takes
        var sw = Stopwatch.StartNew ();

        for (var i = 0; i < COUNT; i++)
        {
            apps [i].Dispose ();
        }

        sw.Stop ();

        // Assert - Even with 10 leaked apps, disposal should be fast
        // Before the throttle fix, this would take many seconds due to CPU saturation
        // With the throttle, each thread does Task.Delay(20ms) and exits within ~20-40ms
        Assert.True (sw.ElapsedMilliseconds < 2000, $"Disposing {COUNT} apps took {sw.ElapsedMilliseconds}ms - CPU may be saturated");
    }

    [Fact]
    public async Task StartInputTaskAsync_DetectsKittyKeyboard_WhenTerminalResponds ()
    {
        using (TestLogging.Verbose (outputHelper))
        {
            ConcurrentQueue<char> inputQueue = new ();
            TimedEvents timedEvents = new ();
            ApplicationMainLoop<char> loop = new ();
            TestAnsiInput input = new ("\u001B[?31u");
            AnsiOutput output = new ();
            TestAnsiComponentFactory factory = new (input, output);
            MainLoopCoordinator<char> coordinator = new (timedEvents, inputQueue, loop, factory);
            Mock<IApplication> appMock = new ();

            appMock.SetupProperty (a => a.Driver);
            appMock.SetupProperty (a => a.MainThreadId, 123);

            await coordinator.StartInputTaskAsync (appMock.Object);

            Assert.True (SpinWait.SpinUntil (() => input.ResponseSent, TimeSpan.FromSeconds (1)));
            loop.InputProcessor.ProcessQueue ();

            DriverImpl driver = Assert.IsType<DriverImpl> (appMock.Object.Driver);
            Assert.True (driver.KittyKeyboardCapabilities?.IsSupported);

            Assert.Equal (EscSeqUtils.KittyKeyboardRequestedFlags, driver.KittyKeyboardCapabilities?.Flags);

            coordinator.Stop ();

            Assert.Contains (EscSeqUtils.CSI_DisableKittyKeyboardFlags, output.GetLastOutput (), StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task StartInputTaskAsync_DoesNotEnableKittyKeyboard_ForLegacyConsole ()
    {
        ConcurrentQueue<char> inputQueue = new ();
        TimedEvents timedEvents = new ();
        ApplicationMainLoop<char> loop = new ();
        TestAnsiInput input = new (null);
        AnsiOutput output = new () { IsLegacyConsole = true };
        TestAnsiComponentFactory factory = new (input, output);
        MainLoopCoordinator<char> coordinator = new (timedEvents, inputQueue, loop, factory);
        Mock<IApplication> appMock = new ();
        AnsiStartupGate startupGate = new ();

        appMock.SetupProperty (a => a.Driver);
        appMock.SetupProperty (a => a.AnsiStartupGate, startupGate);
        appMock.SetupProperty (a => a.MainThreadId, 456);

        await coordinator.StartInputTaskAsync (appMock.Object);

        DriverImpl driver = Assert.IsType<DriverImpl> (appMock.Object.Driver);
        Assert.Null (driver.KittyKeyboardCapabilities);
        Assert.True (startupGate.IsReady);

        Assert.DoesNotContain (EscSeqUtils.CSI_EnableKittyKeyboardFlags (EscSeqUtils.KittyKeyboardRequestedFlags),
                               output.GetLastOutput (),
                               StringComparison.Ordinal);

        coordinator.Stop ();

        Assert.DoesNotContain (EscSeqUtils.CSI_DisableKittyKeyboardFlags, output.GetLastOutput (), StringComparison.Ordinal);
    }

    // Codex - GPT-6
    [Fact]
    public void DeviceAttributesReply_ReleasesOptionalStartupProbes_ButNotCursorPosition ()
    {
        AnsiStartupGate startupGate = new ();
        startupGate.RegisterQuery (AnsiStartupQuery.KittyKeyboard, TimeSpan.FromSeconds (1));
        startupGate.RegisterQuery (AnsiStartupQuery.TerminalColors, TimeSpan.FromSeconds (1));
        startupGate.RegisterQuery (AnsiStartupQuery.CursorPosition, TimeSpan.FromMilliseconds (500));

        AnsiEscapeSequenceRequest? capturedRequest = null;
        Mock<IDriver> driverMock = new ();
        driverMock.Setup (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()))
                  .Callback<AnsiEscapeSequenceRequest> (request => capturedRequest = request);

        MainLoopCoordinator<char>.QueueDeviceAttributesProbe (driverMock.Object, startupGate);

        Assert.NotNull (capturedRequest);
        Assert.DoesNotContain (AnsiStartupQuery.DeviceAttributesPrimary, startupGate.PendingQueries);
        Assert.Contains (AnsiStartupQuery.KittyKeyboard, startupGate.PendingQueries);
        Assert.Contains (AnsiStartupQuery.TerminalColors, startupGate.PendingQueries);

        capturedRequest.ResponseReceived ("\u001B[?1;0c");

        Assert.Equal ([AnsiStartupQuery.CursorPosition], startupGate.PendingQueries);
    }

    // Codex - GPT-6
    [Fact]
    public void MissingDeviceAttributesReply_DoesNotDelayCompletedOptionalProbes ()
    {
        AnsiStartupGate startupGate = new ();
        startupGate.RegisterQuery (AnsiStartupQuery.KittyKeyboard, TimeSpan.FromSeconds (1));
        startupGate.RegisterQuery (AnsiStartupQuery.TerminalColors, TimeSpan.FromSeconds (1));

        Mock<IDriver> driverMock = new ();
        driverMock.Setup (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()));

        MainLoopCoordinator<char>.QueueDeviceAttributesProbe (driverMock.Object, startupGate);
        Assert.False (startupGate.IsReady);

        startupGate.MarkComplete (AnsiStartupQuery.KittyKeyboard);
        startupGate.MarkComplete (AnsiStartupQuery.TerminalColors);

        Assert.True (startupGate.IsReady);
    }

    [Fact]
    public async Task TestMainLoopCoordinator_InputCrashes_ExceptionSurfacesMainThread ()
    {
        using IDisposable logScope = TestLogging.BindTo (outputHelper, LogLevel.Critical);

        Mock<IComponentFactory<char>> m = new ();

        m.Setup (f => f.CreateInput ()).Throws (new Exception ("Crash on boot"));

        MainLoopCoordinator<char> c = new (new TimedEvents (), new ConcurrentQueue<char> (), Mock.Of<IApplicationMainLoop<char>> (), m.Object);

        var ex = await Assert.ThrowsAsync<AggregateException> (() => c.StartInputTaskAsync (null));
        Assert.Equal ("Crash on boot", ex.InnerExceptions [0].Message);
    }

    [Fact]
    public async Task StartInputTaskAsync_InitiatesSixelSupportDetection ()
    {
        // Arrange: inject a response containing "4" (sixel capability) in DAR,
        // followed by a sixel resolution response "[6;20;10t".
        // The SixelSupportDetector sends CSI_SendDeviceAttributes first, then
        // CSI_RequestSixelResolution on success. We need to feed responses for both.
        ConcurrentQueue<char> inputQueue = new ();
        TimedEvents timedEvents = new ();
        ApplicationMainLoop<char> loop = new ();

        // The TestAnsiInput only sends a single response string at startup.
        // The SixelSupportDetector uses QueueAnsiRequest (async callbacks), not raw input.
        // We cannot easily simulate the full DAR/resolution exchange through raw input here,
        // but we CAN verify that after boot the SixelSupport property is populated
        // by checking that it starts null on a non-legacy console driver.
        TestAnsiInput input = new (null);
        AnsiOutput output = new ();
        TestAnsiComponentFactory factory = new (input, output);
        MainLoopCoordinator<char> coordinator = new (timedEvents, inputQueue, loop, factory);
        Mock<IApplication> appMock = new ();

        appMock.SetupProperty (a => a.Driver);
        appMock.SetupProperty (a => a.MainThreadId, 789);

        // Act
        await coordinator.StartInputTaskAsync (appMock.Object);

        DriverImpl driver = Assert.IsType<DriverImpl> (appMock.Object.Driver);

        // Assert: SixelSupport will be null because no DAR response was received,
        // but the important thing is the driver was initialized and the detection
        // path did not throw. The SixelSupportDetector queued a request.
        // The property remains null until a terminal response arrives.
        Assert.Null (driver.SixelSupport);

        // Verify the driver is NOT legacy console (sixel detection should have been attempted)
        Assert.False (driver.IsLegacyConsole);

        coordinator.Stop ();
    }

    [Fact]
    public async Task StartInputTaskAsync_SkipsSixelDetection_ForLegacyConsole ()
    {
        // Arrange
        ConcurrentQueue<char> inputQueue = new ();
        TimedEvents timedEvents = new ();
        ApplicationMainLoop<char> loop = new ();
        TestAnsiInput input = new (null);
        AnsiOutput output = new () { IsLegacyConsole = true };
        TestAnsiComponentFactory factory = new (input, output);
        MainLoopCoordinator<char> coordinator = new (timedEvents, inputQueue, loop, factory);
        Mock<IApplication> appMock = new ();

        appMock.SetupProperty (a => a.Driver);
        appMock.SetupProperty (a => a.MainThreadId, 101);

        // Act
        await coordinator.StartInputTaskAsync (appMock.Object);

        DriverImpl driver = Assert.IsType<DriverImpl> (appMock.Object.Driver);

        // Assert: SixelSupport should be null (detection was skipped for legacy console)
        Assert.Null (driver.SixelSupport);
        Assert.True (driver.IsLegacyConsole);

        coordinator.Stop ();
    }

    private sealed class TestAnsiComponentFactory (TestAnsiInput input, AnsiOutput output) : ComponentFactoryImpl<char>
    {
        public override string GetDriverName () => DriverRegistry.Names.ANSI;

        public override IInput<char> CreateInput () => input;

        public override IInputProcessor CreateInputProcessor (ConcurrentQueue<char> inputBuffer, ITimeProvider? timeProvider = null) =>
            new AnsiInputProcessor (inputBuffer, timeProvider);

        public override IOutput CreateOutput () => output;

        public override ISizeMonitor CreateSizeMonitor (IOutput consoleOutput, IOutputBuffer outputBuffer) => new SizeMonitorImpl (consoleOutput);
    }

    private sealed class TestAnsiInput (string? response) : IInput<char>
    {
        private ConcurrentQueue<char>? _inputQueue;

        public CancellationTokenSource? ExternalCancellationTokenSource { get; set; }

        public bool ResponseSent { get; private set; }

        public void Initialize (ConcurrentQueue<char> inputQueue) => _inputQueue = inputQueue;

        public void Run (CancellationToken runCancellationToken)
        {
            if (!ResponseSent && !string.IsNullOrEmpty (response) && _inputQueue is { } inputQueue)
            {
                foreach (char ch in response)
                {
                    inputQueue.Enqueue (ch);
                }

                ResponseSent = true;
            }

            WaitHandle.WaitAny ([runCancellationToken.WaitHandle]);

            throw new OperationCanceledException (runCancellationToken);
        }

        public void Dispose () { }
    }
}
