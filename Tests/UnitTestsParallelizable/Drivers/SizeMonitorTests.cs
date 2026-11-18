using Moq;

namespace DriverTests;

[Collection ("Driver Tests")]
public class SizeMonitorTests
{
    // Copilot

    [Fact]
    public void SizeMonitorImpl_DoesNotRaiseEvent_OnFirstPoll_WhenSizeUnchanged ()
    {
        // Arrange: initial GetSize() returns 30x20 (captured in constructor).
        // Poll returns the same size → no event should fire.
        Mock<IOutput> consoleOutput = new ();

        Queue<Size> queue = new ([
                                     new Size (30, 20), // constructor call
                                     new Size (30, 20) // Poll 1 – unchanged
                                 ]);

        consoleOutput.Setup (m => m.GetSize ()).Returns (queue.Dequeue);

        SizeMonitorImpl monitor = new (consoleOutput.Object);

        List<SizeChangedEventArgs> result = [];
        monitor.SizeChanged += (_, e) => { result.Add (e); };

        monitor.Poll ();

        Assert.Empty (result);
    }

    [Fact]
    public void SizeMonitorImpl_RaisesEvent_WhenSizeChanges ()
    {
        // Arrange: initial size 30x20, then size changes to 40x25.
        Mock<IOutput> consoleOutput = new ();

        Queue<Size> queue = new ([
                                     new Size (30, 20), // constructor call
                                     new Size (40, 25), // Poll 1 – changed
                                     new Size (40, 25) // Poll 2 – unchanged
                                 ]);

        consoleOutput.Setup (m => m.GetSize ()).Returns (queue.Dequeue);

        SizeMonitorImpl monitor = new (consoleOutput.Object);

        List<SizeChangedEventArgs> result = [];
        monitor.SizeChanged += (_, e) => { result.Add (e); };

        monitor.Poll ();

        Assert.Single (result);
        Assert.Equal (new Size (40, 25), result [0].Size);

        monitor.Poll ();

        Assert.Single (result); // second poll: no further change
    }

    [Fact]
    public void SizeMonitorImpl_RaisesEvent_OnEachDistinctSizeChange ()
    {
        Mock<IOutput> consoleOutput = new ();

        Queue<Size> queue = new ([
                                     new Size (30, 20), // constructor call
                                     new Size (40, 25), // Poll 1 – changed
                                     new Size (50, 30) // Poll 2 – changed again
                                 ]);

        consoleOutput.Setup (m => m.GetSize ()).Returns (queue.Dequeue);

        SizeMonitorImpl monitor = new (consoleOutput.Object);

        List<SizeChangedEventArgs> result = [];
        monitor.SizeChanged += (_, e) => { result.Add (e); };

        monitor.Poll ();
        monitor.Poll ();

        Assert.Equal (2, result.Count);
        Assert.Equal (new Size (40, 25), result [0].Size);
        Assert.Equal (new Size (50, 30), result [1].Size);
    }
}
