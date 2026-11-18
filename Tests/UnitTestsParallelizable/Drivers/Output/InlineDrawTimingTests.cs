// Copilot
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Moq;
using Terminal.Gui.Tracing;
using UnitTests;

namespace DriverTests.Output;

#if DEBUG

/// <summary>
///     Diagnostic tests that use <see cref="Trace"/> with a <see cref="ListBackend"/> to capture
///     the exact sequence of lifecycle and draw events during inline-mode iteration. These tests
///     reveal timing issues where <c>LayoutAndDraw</c> fires before the ANSI size response arrives.
/// </summary>
[Collection ("Driver Tests")]
public class InlineDrawTimingTests (ITestOutputHelper output)
{
    /// <summary>
    ///     Verifies that <c>IterationImpl</c> defers drawing when the <see cref="IAnsiStartupGate"/>
    ///     is not ready (simulating pending startup queries).
    /// </summary>
    [Fact]
    public void IterationImpl_Inline_DefersDrawUntilSizeReceived ()
    {
        using IDisposable logScope = TestLogging.Verbose (output, TraceCategory.Lifecycle | TraceCategory.Draw);
        ListBackend backend = new ();

        using (Trace.PushScope (TraceCategory.Lifecycle | TraceCategory.Draw, backend))
        {
            // Create an ApplicationMainLoop with a fake size monitor that hasn't received size yet
            ConcurrentQueue<char> inputQueue = new ();
            ApplicationMainLoop<char> loop = new ();
            AnsiOutput ansiOutput = new ();
            ansiOutput.SetSize (80, 25); // default fallback

            // AnsiSizeMonitor starts with InitialSizeReceived = false
            AnsiSizeMonitor sizeMonitor = new (ansiOutput);
            Assert.False (sizeMonitor.InitialSizeReceived);

            // Create a startup gate with a pending query so IsReady = false
            AnsiStartupGate startupGate = new (() => DateTime.UtcNow);
            startupGate.RegisterQuery (AnsiStartupQuery.TerminalSize, TimeSpan.FromSeconds (30));
            Assert.False (startupGate.IsReady);

            // Create a mock IDriver that returns the startup gate
            Mock<IDriver> driverMock = new ();
            driverMock.SetupGet (d => d.AnsiStartupGate).Returns (startupGate);

            // Create a mock IApplication with instance-based AppModel (no global static)
            var layoutAndDrawCallCount = 0;
            Mock<IApplication> appMock = new ();
            appMock.Setup (a => a.LayoutAndDraw (It.IsAny<bool> ())).Callback (() => layoutAndDrawCallCount++);
            appMock.SetupGet (a => a.Screen).Returns (new Rectangle (0, 0, 80, 25));
            appMock.SetupGet (a => a.AppModel).Returns (AppModel.Inline);
            appMock.SetupGet (a => a.Driver).Returns (driverMock.Object);

            // We need a real InputProcessor to avoid nulls
            AnsiInputProcessor inputProcessor = new (inputQueue);

            // Create a component factory that returns our AnsiSizeMonitor
            Mock<IComponentFactory<char>> factoryMock = new ();
            factoryMock.Setup (f => f.CreateSizeMonitor (It.IsAny<IOutput> (), It.IsAny<IOutputBuffer> ())).Returns (sizeMonitor);

            // Initialize the loop
            loop.Initialize (new TimedEvents (), inputQueue, inputProcessor, ansiOutput, factoryMock.Object, appMock.Object);

            output.WriteLine ($"SizeMonitor type: {loop.SizeMonitor.GetType ().Name}");
            output.WriteLine ($"InitialSizeReceived: {loop.SizeMonitor.InitialSizeReceived}");

            // Act: Run several iterations — draw should be deferred by the startup gate
            for (var i = 0; i < 3; i++)
            {
                loop.IterationImpl ();
            }

            output.WriteLine ($"LayoutAndDraw calls after 3 iterations (gate not ready): {layoutAndDrawCallCount}");

            // Assert: No draws should have happened yet
            Assert.Equal (0, layoutAndDrawCallCount);

            // No IterationDraw entries should exist
            List<TraceEntry> drawEntries = backend.Entries
                                                  .Where (e => e.Phase == "IterationDraw")
                                                  .ToList ();
            Assert.Empty (drawEntries);
        }
    }

    /// <summary>
    ///     Verifies that once the ANSI startup gate becomes ready (all queries answered),
    ///     subsequent iterations do draw.
    /// </summary>
    [Fact]
    public void IterationImpl_Inline_DrawsAfterSizeResponseArrives ()
    {
        using IDisposable logScope = TestLogging.Verbose (output, TraceCategory.Lifecycle | TraceCategory.Draw);
        ListBackend backend = new ();

        using (Trace.PushScope (TraceCategory.Lifecycle | TraceCategory.Draw, backend))
        {
            ConcurrentQueue<char> inputQueue = new ();
            ApplicationMainLoop<char> loop = new ();
            AnsiOutput ansiOutput = new ();
            ansiOutput.SetSize (80, 25);

            // Use AnsiSizeMonitor with a captured request callback
            AnsiEscapeSequenceRequest? capturedRequest = null;
            AnsiSizeMonitor sizeMonitor = new (ansiOutput, req => capturedRequest = req);

            // Create a startup gate with a pending TerminalSize query
            AnsiStartupGate startupGate = new (() => DateTime.UtcNow);
            IDisposable sizeHandle = startupGate.RegisterQuery (AnsiStartupQuery.TerminalSize, TimeSpan.FromSeconds (30));
            Assert.False (startupGate.IsReady);

            // Create a mock IDriver that returns the startup gate and supports InlinePosition
            Mock<IDriver> driverMock = new ();
            driverMock.SetupGet (d => d.AnsiStartupGate).Returns (startupGate);
            Point inlinePosition = Point.Empty;
            driverMock.SetupGet (d => d.InlinePosition).Returns (inlinePosition);
            driverMock.SetupSet (d => d.InlinePosition = It.IsAny<Point> ()).Callback<Point> (p => inlinePosition = p);

            // Instance-based AppModel on the mock (no global static)
            var layoutAndDrawCallCount = 0;
            Mock<IApplication> appMock = new ();
            appMock.Setup (a => a.LayoutAndDraw (It.IsAny<bool> ())).Callback (() => layoutAndDrawCallCount++);
            appMock.SetupGet (a => a.Screen).Returns (new Rectangle (0, 0, 120, 50));
            appMock.SetupGet (a => a.AppModel).Returns (AppModel.Inline);
            appMock.SetupGet (a => a.Driver).Returns (driverMock.Object);

            AnsiInputProcessor inputProcessor = new (inputQueue);

            Mock<IComponentFactory<char>> factoryMock = new ();
            factoryMock.Setup (f => f.CreateSizeMonitor (It.IsAny<IOutput> (), It.IsAny<IOutputBuffer> ())).Returns (sizeMonitor);

            loop.Initialize (new TimedEvents (), inputQueue, inputProcessor, ansiOutput, factoryMock.Object, appMock.Object);

            // First iteration — deferred (gate not ready)
            loop.IterationImpl ();
            Assert.Equal (0, layoutAndDrawCallCount);

            // Mark the startup gate query as complete (simulating size response arrival)
            sizeHandle.Dispose ();
            Assert.True (startupGate.IsReady);

            output.WriteLine ($"After gate ready: IsReady={startupGate.IsReady}");

            // Next iteration — should now draw
            loop.IterationImpl ();

            output.WriteLine ($"LayoutAndDraw calls after gate ready + iteration: {layoutAndDrawCallCount}");
            Assert.Equal (1, layoutAndDrawCallCount);

            // Check trace shows confirmation
            List<TraceEntry> confirmedEntries = backend.Entries
                                                       .Where (e => e.Phase == "InlineSizeConfirmed")
                                                       .ToList ();
            output.WriteLine ($"InlineSizeConfirmed trace entries: {confirmedEntries.Count}");

            foreach (TraceEntry entry in confirmedEntries)
            {
                output.WriteLine ($"  {entry.Phase}: {entry.Message}");
            }

            Assert.Single (confirmedEntries);

            // And a draw entry
            List<TraceEntry> drawEntries = backend.Entries
                                                  .Where (e => e.Phase == "IterationDraw")
                                                  .ToList ();
            Assert.Single (drawEntries);
        }
    }

    /// <summary>
    ///     Verifies that <see cref="SizeMonitorImpl"/> (non-ANSI) has
    ///     <see cref="ISizeMonitor.InitialSizeReceived"/> default to <see langword="true"/>
    ///     and therefore does NOT defer drawing.
    /// </summary>
    [Fact]
    public void IterationImpl_Inline_NonAnsiMonitor_DrawsImmediately ()
    {
        using IDisposable logScope = TestLogging.Verbose (output, TraceCategory.Lifecycle | TraceCategory.Draw);
        ListBackend backend = new ();

        using (Trace.PushScope (TraceCategory.Lifecycle | TraceCategory.Draw, backend))
        {
            ConcurrentQueue<char> inputQueue = new ();
            ApplicationMainLoop<char> loop = new ();
            AnsiOutput ansiOutput = new ();
            ansiOutput.SetSize (120, 50);

            // SizeMonitorImpl.InitialSizeReceived defaults to true (interface default)
            SizeMonitorImpl sizeMonitor = new (ansiOutput);
            Assert.True (((ISizeMonitor)sizeMonitor).InitialSizeReceived);

            // Instance-based AppModel on the mock (no global static)
            var layoutAndDrawCallCount = 0;
            Mock<IApplication> appMock = new ();
            appMock.Setup (a => a.LayoutAndDraw (It.IsAny<bool> ())).Callback (() => layoutAndDrawCallCount++);
            appMock.SetupGet (a => a.Screen).Returns (new Rectangle (0, 0, 120, 50));
            appMock.SetupGet (a => a.AppModel).Returns (AppModel.Inline);

            AnsiInputProcessor inputProcessor = new (inputQueue);

            Mock<IComponentFactory<char>> factoryMock = new ();
            factoryMock.Setup (f => f.CreateSizeMonitor (It.IsAny<IOutput> (), It.IsAny<IOutputBuffer> ())).Returns (sizeMonitor);

            loop.Initialize (new TimedEvents (), inputQueue, inputProcessor, ansiOutput, factoryMock.Object, appMock.Object);

            // First iteration — should draw immediately since SizeMonitorImpl reports size received
            loop.IterationImpl ();

            output.WriteLine ($"LayoutAndDraw calls after first iteration with SizeMonitorImpl: {layoutAndDrawCallCount}");
            Assert.Equal (1, layoutAndDrawCallCount);

            // No deferred entries
            List<TraceEntry> deferEntries = backend.Entries
                                                   .Where (e => e.Phase == "InlineSizeDeferred")
                                                   .ToList ();
            Assert.Empty (deferEntries);
        }
    }

    /// <summary>
    ///     Verifies the full timeline: deferred iterations → gate ready → first draw.
    ///     Dumps all trace entries to the test output for diagnostic visibility.
    /// </summary>
    [Fact (Skip = "Bogus test; do not use tracing for test results.")]
    public void IterationImpl_Inline_FullTimeline_TraceDump ()
    {
        using IDisposable logScope = TestLogging.Verbose (output, TraceCategory.Lifecycle | TraceCategory.Draw);
        ListBackend backend = new ();

        using (Trace.PushScope (TraceCategory.Lifecycle | TraceCategory.Draw, backend))
        {
            ConcurrentQueue<char> inputQueue = new ();
            ApplicationMainLoop<char> loop = new ();
            AnsiOutput ansiOutput = new ();
            ansiOutput.SetSize (80, 25);

            AnsiEscapeSequenceRequest? capturedRequest = null;
            AnsiSizeMonitor sizeMonitor = new (ansiOutput, req => capturedRequest = req);

            // Create a startup gate with a pending TerminalSize query
            AnsiStartupGate startupGate = new (() => DateTime.UtcNow);
            IDisposable sizeHandle = startupGate.RegisterQuery (AnsiStartupQuery.TerminalSize, TimeSpan.FromSeconds (30));

            // Create a mock IDriver
            Mock<IDriver> driverMock = new ();
            driverMock.SetupGet (d => d.AnsiStartupGate).Returns (startupGate);
            Point inlinePosition = Point.Empty;
            driverMock.SetupGet (d => d.InlinePosition).Returns (inlinePosition);
            driverMock.SetupSet (d => d.InlinePosition = It.IsAny<Point> ()).Callback<Point> (p => inlinePosition = p);

            // Instance-based AppModel on the mock (no global static)
            var layoutAndDrawCallCount = 0;
            Mock<IApplication> appMock = new ();
            appMock.Setup (a => a.LayoutAndDraw (It.IsAny<bool> ())).Callback (() => layoutAndDrawCallCount++);
            appMock.SetupGet (a => a.Screen).Returns (new Rectangle (0, 0, 80, 25));
            appMock.SetupGet (a => a.AppModel).Returns (AppModel.Inline);
            appMock.SetupGet (a => a.Driver).Returns (driverMock.Object);

            AnsiInputProcessor inputProcessor = new (inputQueue);

            Mock<IComponentFactory<char>> factoryMock = new ();
            factoryMock.Setup (f => f.CreateSizeMonitor (It.IsAny<IOutput> (), It.IsAny<IOutputBuffer> ())).Returns (sizeMonitor);

            loop.Initialize (new TimedEvents (), inputQueue, inputProcessor, ansiOutput, factoryMock.Object, appMock.Object);

            output.WriteLine ("=== Iteration 1 (gate not ready) ===");
            loop.IterationImpl ();
            output.WriteLine ($"  DrawCalls={layoutAndDrawCallCount}, IsReady={startupGate.IsReady}");

            output.WriteLine ("=== Iteration 2 (gate not ready) ===");
            loop.IterationImpl ();
            output.WriteLine ($"  DrawCalls={layoutAndDrawCallCount}, IsReady={startupGate.IsReady}");

            // Mark gate ready
            output.WriteLine ("=== Marking startup gate ready ===");
            sizeHandle.Dispose ();
            output.WriteLine ($"  IsReady={startupGate.IsReady}");

            output.WriteLine ("=== Iteration 3 (gate now ready) ===");
            loop.IterationImpl ();
            output.WriteLine ($"  DrawCalls={layoutAndDrawCallCount}, IsReady={startupGate.IsReady}");

            output.WriteLine ("=== Iteration 4 (subsequent draw) ===");
            loop.IterationImpl ();
            output.WriteLine ($"  DrawCalls={layoutAndDrawCallCount}");

            // Dump all trace entries
            output.WriteLine ("");
            output.WriteLine ("=== TRACE ENTRIES ===");

            foreach (TraceEntry entry in backend.Entries)
            {
                output.WriteLine ($"  [{entry.Category}] {entry.Id}.{entry.Phase}: {entry.Message}");
            }

            // Verify: at least 1 deferral log, 1 confirmed, 2 draws
            Assert.Equal (1, backend.Entries.Count (e => e.Phase == "InlineSizeConfirmed"));
            Assert.Equal (2, backend.Entries.Count (e => e.Phase == "IterationDraw"));
            Assert.Equal (2, layoutAndDrawCallCount);
        }
    }
}

#endif
