using System.Collections.Concurrent;
using Moq;

#nullable enable

namespace UnitTests.NonParallelizable.ApplicationTests;

/// <summary>
///     Tests coordinator color callbacks with process-wide terminal capability settings.
/// </summary>
public class MainLoopCoordinatorColorTests
{
    // CoPilot - GPT-6
    [Fact]
    public async Task LateColorResponses_AfterFirstDraw_UpdateDefaultAttribute_AndInvalidateRunnable ()
    {
        // Capability detection reads process-wide environment variables, so this test must run serially.
        string? previousTerm = Environment.GetEnvironmentVariable ("TERM");
        string? previousColorTerm = Environment.GetEnvironmentVariable ("COLORTERM");
        string? previousNoColor = Environment.GetEnvironmentVariable ("NO_COLOR");

        try
        {
            Environment.SetEnvironmentVariable ("TERM", "xterm-256color");
            Environment.SetEnvironmentVariable ("COLORTERM", "truecolor");
            Environment.SetEnvironmentVariable ("NO_COLOR", null);

            ConcurrentQueue<char> inputQueue = new ();
            VirtualTimeProvider time = new ();
            DateTime now = DateTime.Now;
            AnsiStartupGate startupGate = new (() => now);
            ApplicationMainLoop<char> loop = new ();
            Mock<IInput<char>> inputMock = new ();
            inputMock.Setup (input => input.Run (It.IsAny<CancellationToken> ()))
                     .Callback<CancellationToken> (cancellation => cancellation.WaitHandle.WaitOne ());
            using AnsiOutput output = new ();
            TestAnsiComponentFactory factory = new (inputMock.Object, output);
            MainLoopCoordinator<char> coordinator = new (new TimedEvents (), inputQueue, loop, factory, time);
            Mock<IApplication> appMock = new ();
            using Runnable runnable = new () { App = appMock.Object, Width = 20, Height = 3, Text = "Late colors" };

            appMock.SetupProperty (app => app.Driver);
            appMock.SetupProperty (app => app.AnsiStartupGate, startupGate);
            appMock.SetupGet (app => app.AppModel).Returns (AppModel.Inline);
            appMock.SetupGet (app => app.TopRunnableView).Returns (runnable);
            ConcurrentStack<SessionToken> sessions = new ();
            appMock.SetupGet (app => app.SessionStack).Returns (sessions);
            appMock.SetupGet (app => app.Screen).Returns (new Rectangle (0, 0, 80, 25));
            appMock.Setup (app => app.LayoutAndDraw (false)).Callback (() => runnable.Draw ());

            try
            {
                await coordinator.StartInputTaskAsync (appMock.Object);
                loop.AnsiRequestScheduler.Now = () => now;
                DriverImpl driver = Assert.IsType<DriverImpl> (appMock.Object.Driver);
                Assert.Contains (AnsiStartupQuery.TerminalColors, startupGate.PendingQueries);

                EnqueueResponse ("\u001B[?1;0c");
                loop.InputProcessor.ProcessQueue ();
                Assert.True (startupGate.IsReady);

                runnable.BeginInit ();
                runnable.EndInit ();
                runnable.LayoutSubViews ();
                loop.IterationImpl ();
                appMock.Verify (app => app.LayoutAndDraw (false), Times.Once);
                Assert.False (appMock.Object.TopRunnableView!.NeedsDraw);
                Assert.Null (driver.DefaultAttribute);

                EnqueueResponse ("\u001B]10;rgb:1212/3434/5656\u001B\\");
                loop.InputProcessor.ProcessQueue ();
                Assert.Null (driver.DefaultAttribute);
                Assert.False (runnable.NeedsDraw);

                EnqueueResponse ("\u001B]11;rgb:aaaa/bbbb/cccc\u001B\\");
                loop.InputProcessor.ProcessQueue ();

                Assert.Equal (new Attribute (new Color (0x12, 0x34, 0x56), new Color (0xaa, 0xbb, 0xcc)), driver.DefaultAttribute);
                Assert.True (appMock.Object.TopRunnableView!.NeedsDraw);
                Assert.True (startupGate.IsReady);

                loop.IterationImpl ();
                appMock.Verify (app => app.LayoutAndDraw (false), Times.Exactly (2));
                Assert.False (runnable.NeedsDraw);
            }
            finally
            {
                runnable.App = null;
                coordinator.Stop ();
                appMock.Object.Driver?.Dispose ();
            }

            void EnqueueResponse (string response)
            {
                foreach (char ch in response)
                {
                    inputQueue.Enqueue (ch);
                }
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable ("TERM", previousTerm);
            Environment.SetEnvironmentVariable ("COLORTERM", previousColorTerm);
            Environment.SetEnvironmentVariable ("NO_COLOR", previousNoColor);
        }
    }

    private sealed class TestAnsiComponentFactory (IInput<char> input, AnsiOutput output) : ComponentFactoryImpl<char>
    {
        public override string GetDriverName () => DriverRegistry.Names.ANSI;

        public override IInput<char> CreateInput () => input;

        public override IInputProcessor CreateInputProcessor (ConcurrentQueue<char> inputBuffer, ITimeProvider? timeProvider = null) =>
            new AnsiInputProcessor (inputBuffer, timeProvider);

        public override IOutput CreateOutput () => output;

        public override ISizeMonitor CreateSizeMonitor (IOutput consoleOutput, IOutputBuffer outputBuffer) => new SizeMonitorImpl (consoleOutput);
    }
}
