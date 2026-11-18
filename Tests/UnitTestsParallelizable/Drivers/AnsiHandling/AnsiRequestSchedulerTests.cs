using Moq;

namespace DriverTests.AnsiHandling;

[Collection ("Driver Tests")]
public class AnsiRequestSchedulerTests
{
    private readonly Mock<IAnsiResponseParser> _parserMock;
    private readonly AnsiRequestScheduler _scheduler;

    private static DateTime _staticNow; // Static value to hold the current time

    public AnsiRequestSchedulerTests ()
    {
        _parserMock = new Mock<IAnsiResponseParser> (MockBehavior.Strict);
        _staticNow = DateTime.UtcNow; // Initialize static time
        _scheduler = new AnsiRequestScheduler (_parserMock.Object, () => _staticNow);
    }

    [Fact]
    public void SendOrSchedule_SendsDeviceAttributeRequest_WhenNoOutstandingRequests ()
    {
        // Arrange
        var request = new AnsiEscapeSequenceRequest
        {
            Request = "\u001b[0c", // ESC [ c
            Terminator = "c",
            ResponseReceived = r => { }
        };

        // we have no outstanding for c already
        _parserMock.Setup (p => p.IsExpecting ("c", null)).Returns (false).Verifiable (Times.Once);

        // then we should execute our request
        _parserMock.Setup (p => p.ExpectResponse ("c", null, It.IsAny<Action<string?>> ()!, null, false)).Verifiable (Times.Once);

        // Act
        bool result = _scheduler.SendOrSchedule (null, request);

        // Assert
        Assert.Empty (_scheduler.QueuedRequests); // We sent it i.e. we did not queue it for later
        Assert.True (result); // Should send immediately
        _parserMock.Verify ();
    }

    [Fact]
    public void SendOrSchedule_QueuesRequest_WhenOutstandingRequestExists ()
    {
        // Arrange
        var request1 = new AnsiEscapeSequenceRequest
        {
            Request = "\u001b[0c", // ESC [ 0 c
            Terminator = "c",
            ResponseReceived = r => { }
        };

        // Parser already has an ongoing request for "c"
        _parserMock.Setup (p => p.IsExpecting ("c", null)).Returns (true).Verifiable (Times.Once);

        // Act
        bool result = _scheduler.SendOrSchedule (null, request1);

        // Assert
        Assert.Single (_scheduler.QueuedRequests); // Ensure only one request is in the queue
        Assert.False (result); // Should be queued
        _parserMock.Verify ();
    }

    [Fact]
    public void RunSchedule_ThrottleNotExceeded_AllowSend ()
    {
        // Arrange
        var request = new AnsiEscapeSequenceRequest
        {
            Request = "\u001b[0c", // ESC [ 0 c
            Terminator = "c",
            ResponseReceived = r => { }
        };

        // Set up to expect no outstanding request for "c" i.e. parser instantly gets response and resolves it
        _parserMock.Setup (p => p.IsExpecting ("c", null)).Returns (false).Verifiable (Times.Exactly (2));
        _parserMock.Setup (p => p.ExpectResponse ("c", null, It.IsAny<Action<string?>> ()!, null, false)).Verifiable (Times.Exactly (2));

        _scheduler.SendOrSchedule (null, request);

        // Simulate time passing beyond throttle
        SetTime (101); // Exceed throttle limit

        // Act

        // Send another request after the throttled time limit
        bool result = _scheduler.SendOrSchedule (null, request);

        // Assert
        Assert.Empty (_scheduler.QueuedRequests); // Should send and clear the request
        Assert.True (result); // Should have found and sent the request
        _parserMock.Verify ();
    }

    [Fact]
    public void RunSchedule_ThrottleExceeded_QueueRequest ()
    {
        // Arrange
        var request = new AnsiEscapeSequenceRequest
        {
            Request = "\u001b[0c", // ESC [ 0 c
            Terminator = "c",
            ResponseReceived = r => { }
        };

        // Set up to expect no outstanding request for "c" i.e. parser instantly gets response and resolves it
        _parserMock.Setup (p => p.IsExpecting ("c", null)).Returns (false).Verifiable (Times.Exactly (2));
        _parserMock.Setup (p => p.ExpectResponse ("c", null, It.IsAny<Action<string?>> ()!, null, false)).Verifiable (Times.Exactly (2));

        _scheduler.SendOrSchedule (null, request);

        // Simulate time passing
        SetTime (55); // Does not exceed throttle limit

        // Act

        // Send another request after the throttled time limit
        bool result = _scheduler.SendOrSchedule (null, request);

        // Assert
        Assert.Single (_scheduler.QueuedRequests); // Should have been queued
        Assert.False (result); // Should have been queued

        // Throttle still not exceeded
        Assert.False (_scheduler.RunSchedule (null));

        SetTime (90);

        // Throttle still not exceeded
        Assert.False (_scheduler.RunSchedule (null));

        SetTime (105);

        // Throttle exceeded - so send the request
        Assert.True (_scheduler.RunSchedule (null));

        _parserMock.Verify ();
    }

    [Fact]
    public void EvictStaleRequests_RemovesStaleRequest_AfterTimeout ()
    {
        // Arrange
        var request1 = new AnsiEscapeSequenceRequest { Request = "\u001b[0c", Terminator = "c", ResponseReceived = r => { } };

        // Send
        _parserMock.Setup (p => p.IsExpecting ("c", null)).Returns (false).Verifiable (Times.Once);
        _parserMock.Setup (p => p.ExpectResponse ("c", null, It.IsAny<Action<string?>> ()!, null, false)).Verifiable (Times.Exactly (2));

        Assert.True (_scheduler.SendOrSchedule (null, request1));

        // Parser already has an ongoing request for "c"
        _parserMock.Setup (p => p.IsExpecting ("c", null)).Returns (true).Verifiable (Times.Exactly (2));

        // Cannot send because there is already outstanding request
        Assert.False (_scheduler.SendOrSchedule (null, request1));
        Assert.Single (_scheduler.QueuedRequests);

        // Simulate request going stale
        SetTime (5001); // Exceeds stale timeout

        // Parser should be told to give up on this one (evicted)
        _parserMock.Setup (p => p.StopExpecting ("c", null, false))
                   .Callback (() =>
                              {
                                  // When we tell parser to evict - it should now tell us it is no longer expecting
                                  _parserMock.Setup (p => p.IsExpecting ("c", null)).Returns (false).Verifiable (Times.Once);
                              })
                   .Verifiable ();

        // When we send again the evicted one should be
        bool evicted = _scheduler.RunSchedule (null);

        Assert.True (evicted); // Stale request should be evicted
        Assert.Empty (_scheduler.QueuedRequests);

        // Assert
        _parserMock.Verify ();
    }

    [Fact]
    public void RunSchedule_DoesNothing_WhenQueueIsEmpty ()
    {
        // Act
        bool result = _scheduler.RunSchedule (null);

        // Assert
        Assert.False (result); // No requests to process
        Assert.Empty (_scheduler.QueuedRequests);
    }

    [Fact]
    public void SendOrSchedule_ManagesIndependentTerminatorsCorrectly ()
    {
        // Arrange
        var request1 = new AnsiEscapeSequenceRequest { Request = "\u001b[0c", Terminator = "c", ResponseReceived = r => { } };
        var request2 = new AnsiEscapeSequenceRequest { Request = "\u001b[0x", Terminator = "x", ResponseReceived = r => { } };

        // Already have a 'c' ongoing
        _parserMock.Setup (p => p.IsExpecting ("c", null)).Returns (true).Verifiable (Times.Once);

        // 'x' is free
        _parserMock.Setup (p => p.IsExpecting ("x", null)).Returns (false).Verifiable (Times.Once);
        _parserMock.Setup (p => p.ExpectResponse ("x", null, It.IsAny<Action<string?>> ()!, null, false)).Verifiable (Times.Once);

        // Act
        bool a = _scheduler.SendOrSchedule (null, request1);
        bool b = _scheduler.SendOrSchedule (null, request2);

        // Assert
        Assert.False (a);
        Assert.True (b);
        Assert.Equal (request1, Assert.Single (_scheduler.QueuedRequests));
        _parserMock.Verify ();
    }

    private void SetTime (int milliseconds)
    {
        // This simulates the passing of time by setting the Now function to return a specific time.
        DateTime newNow = _staticNow.AddMilliseconds (milliseconds);
        _scheduler.Now = () => newNow;
    }
}
