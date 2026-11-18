using System.Collections.Concurrent;
using Moq;

namespace ApplicationTests;

// Copilot

[Collection ("Application Tests")]
public class ApplicationMainLoopTests
{
    [Fact]
    public void IterationImpl_DefersFirstRender_UntilStartupGateIsReady ()
    {
        DateTime nowUtc = DateTime.UtcNow;
        AnsiStartupGate startupGate = new (() => nowUtc);
        startupGate.RegisterQuery (AnsiStartupQuery.TerminalSize, TimeSpan.FromSeconds (1));

        Mock<IAnsiResponseParser> parserMock = new ();
        Mock<IInputProcessor> inputProcessorMock = new ();
        inputProcessorMock.Setup (p => p.GetParser ()).Returns (parserMock.Object);
        inputProcessorMock.Setup (p => p.ProcessQueue ());

        Mock<IOutput> outputMock = new ();
        outputMock.Setup (o => o.GetSize ()).Returns (new Size (80, 25));

        Mock<ISizeMonitor> sizeMonitorMock = new ();
        sizeMonitorMock.Setup (s => s.Poll ()).Returns (false);
        sizeMonitorMock.Setup (s => s.Initialize (It.IsAny<IDriver?> ()));
        sizeMonitorMock.SetupAdd (s => s.SizeChanged += It.IsAny<EventHandler<SizeChangedEventArgs>> ());
        sizeMonitorMock.SetupRemove (s => s.SizeChanged -= It.IsAny<EventHandler<SizeChangedEventArgs>> ());

        Mock<IComponentFactory<char>> componentFactoryMock = new ();
        componentFactoryMock.Setup (f => f.CreateSizeMonitor (outputMock.Object, It.IsAny<IOutputBuffer> ())).Returns (sizeMonitorMock.Object);

        ApplicationMainLoop<char> loop = new ();
        Mock<IApplication> appMock = new ();

        appMock.Setup (a => a.LayoutAndDraw (false));
        appMock.SetupGet (a => a.Navigation).Returns ((ApplicationNavigation?)null);
        appMock.SetupProperty (a => a.Driver);
        appMock.SetupProperty (a => a.AnsiStartupGate, startupGate);

        loop.Initialize (new TimedEvents (),
                         new ConcurrentQueue<char> (),
                         inputProcessorMock.Object,
                         outputMock.Object,
                         componentFactoryMock.Object,
                         appMock.Object);

        Mock<IComponentFactory> driverComponentFactoryMock = new ();
        DriverImpl driver = new (driverComponentFactoryMock.Object,
                                 inputProcessorMock.Object,
                                 loop.OutputBuffer,
                                 outputMock.Object,
                                 loop.AnsiRequestScheduler,
                                 sizeMonitorMock.Object,
                                 startupGate);

        appMock.Object.Driver = driver;

        loop.IterationImpl ();

        appMock.Verify (a => a.LayoutAndDraw (false), Times.Never);

        startupGate.MarkComplete (AnsiStartupQuery.TerminalSize);
        loop.IterationImpl ();

        appMock.Verify (a => a.LayoutAndDraw (false), Times.Once);
    }
}
