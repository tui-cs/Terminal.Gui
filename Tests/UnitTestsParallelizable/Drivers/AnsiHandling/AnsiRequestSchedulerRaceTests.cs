namespace DriverTests.AnsiHandling;

[Collection ("Driver Tests")]
public class AnsiRequestSchedulerRaceTests
{
    [Fact]
    public void Scheduler_Dispatches_Response_To_Correct_Request_By_Value ()
    {
        // Control time to make behavior deterministic
        DateTime now = DateTime.UtcNow;

        var parser = new AnsiResponseParser (new SystemTimeProvider ());
        var scheduler = new AnsiRequestScheduler (parser, () => now);

        string? firstResponse = null;
        string? secondResponse = null;

        var requestWindowSizeChars = new AnsiEscapeSequenceRequest
        {
            Request = EscSeqUtils.CSI_ReportWindowSizeInChars.Request,
            Terminator = EscSeqUtils.CSI_ReportWindowSizeInChars.Terminator,
            Value = EscSeqUtils.CSI_ReportWindowSizeInChars.Value,
            ResponseReceived = r => firstResponse = r
        };

        var requestSixelResolution = new AnsiEscapeSequenceRequest
        {
            Request = EscSeqUtils.CSI_RequestSixelResolution.Request,
            Terminator = EscSeqUtils.CSI_RequestSixelResolution.Terminator,
            Value = EscSeqUtils.CSI_RequestSixelResolution.Value,
            ResponseReceived = r => secondResponse = r
        };

        // Send first request (should be sent immediately)
        bool sentFirst = scheduler.SendOrSchedule (null, requestWindowSizeChars);

        // Immediately attempt to send second request - should be queued due to throttle on same terminator
        bool sentSecond = scheduler.SendOrSchedule (null, requestSixelResolution);

        Assert.True (sentFirst, "First request must be sent immediately");
        Assert.True (sentSecond, "Second request with different value should be sent immediately");

        // Terminal replies for the SIXEL resolution.
        parser.ProcessInput ("\u001B[6;20;10t");

        // The first request expects Value="8" and must NOT receive this "6" response.
        Assert.Null (firstResponse);
        Assert.Equal ("\u001B[6;20;10t", secondResponse);

        // No queued requests remain
        Assert.Empty (scheduler.QueuedRequests);
    }
}
