namespace DriverTests.AnsiHandling;

public class AnsiRequestSchedulerCollisionTests
{
    [Fact]
    public void Scheduler_Matches_FirstExpectation_WhenTwoRequestsShareSameTerminator ()
    {
        // Arrange
        var parser = new AnsiResponseParser (new SystemTimeProvider ());
        var scheduler = new AnsiRequestScheduler (parser);

        string? firstResponse = null;
        string? secondResponse = null;

        var requestWindowSizeChars = new AnsiEscapeSequenceRequest
        {
            // CSI Report Window Size in Chars -> terminator 't'
            Request = EscSeqUtils.CSI_ReportWindowSizeInChars.Request,
            Value = EscSeqUtils.CSI_ReportWindowSizeInChars.Value,
            Terminator = EscSeqUtils.CSI_ReportWindowSizeInChars.Terminator,
            ResponseReceived = r => firstResponse = r
        };

        var requestSixelResolution = new AnsiEscapeSequenceRequest
        {
            // CSI Request Sixel Resolution -> terminator 't', Value = "6"
            Request = EscSeqUtils.CSI_RequestSixelResolution.Request,
            Value = EscSeqUtils.CSI_RequestSixelResolution.Value,
            Terminator = EscSeqUtils.CSI_RequestSixelResolution.Terminator,
            ResponseReceived = r => secondResponse = r
        };

        // Act
        // Send first request (should register expectation and "send")
        bool sentFirst = scheduler.SendOrSchedule (null, requestWindowSizeChars);

        // Ensure parser registered expectation with Value == "8"
        Assert.True (parser.IsExpecting (requestWindowSizeChars.Terminator, "8"),
                     "Parser should be expecting terminator 't' with value '8' after first request");

        // Send second request. With value-aware scheduling the second request (Value="6") is independent
        // and should be sent (not queued).
        bool sentSecond = scheduler.SendOrSchedule (null, requestSixelResolution);

        // Now simulate arrival of a device response that belongs to the SIXEL resolution:
        // NOTE: must include ESC prefix so parser recognizes it as an ANSI response
        parser.ProcessInput ("\u001B[6;20;10t");

        // Assert
        Assert.True (sentFirst, "First request should have been sent immediately");
        Assert.True (sentSecond, "Second request with different value should be sent immediately (no collision)");

        // Correct behavior: the SIXEL response (value "6") must be routed to the request that asked for value "6".
        Assert.Null (firstResponse);
        Assert.Equal ("\u001B[6;20;10t", secondResponse);

        // No queued requests remain
        Assert.Empty (scheduler.QueuedRequests);
    }
}
