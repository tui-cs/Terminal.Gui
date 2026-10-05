using Moq;

namespace DriverTests.AnsiHandling;

public class KittyKeyboardProtocolDetectorTests
{
    // Copilot
    [Fact]
    public void Enable_QueuesDetect_AndUpdatesDriverFlags_FromDetectionResponse ()
    {
        Mock<IDriver> driverMock = new (MockBehavior.Strict);
        KittyKeyboardCapabilities existingCapabilities = new () { IsSupported = true, Flags = KittyKeyboardFlags.None };
        driverMock.Setup (d => d.IsLegacyConsole).Returns (false);
        driverMock.Setup (d => d.WriteRaw (It.IsAny<string> ()));
        driverMock.Setup (d => d.KittyKeyboardCapabilities).Returns (existingCapabilities);

        driverMock.Setup (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()))
                  .Callback<AnsiEscapeSequenceRequest> (request => request.ResponseReceived ("\u001B[?31u"));

        KittyKeyboardProtocolDetector detector = new (driverMock.Object);

        detector.Enable (EscSeqUtils.KittyKeyboardRequestedFlags);

        Assert.NotNull (existingCapabilities);
        Assert.True (existingCapabilities.IsSupported);
        Assert.Equal (EscSeqUtils.KittyKeyboardRequestedFlags, existingCapabilities.Flags);
        driverMock.Verify (d => d.WriteRaw (EscSeqUtils.CSI_EnableKittyKeyboardFlags (EscSeqUtils.KittyKeyboardRequestedFlags)), Times.Once);
        driverMock.Verify (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()), Times.Once);
    }

    [Fact]
    public void Detect_QueuesKittyQuery_AndReturnsSupportedResult_WhenTerminalResponds ()
    {
        Mock<IDriver> driverMock = new (MockBehavior.Strict);
        driverMock.Setup (d => d.IsLegacyConsole).Returns (false);

        driverMock.Setup (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()))
                  .Callback<AnsiEscapeSequenceRequest> (request =>
                                                        {
                                                            Assert.Equal (EscSeqUtils.CSI_QueryKittyKeyboardFlags.Request, request.Request);
                                                            Assert.Equal (EscSeqUtils.CSI_QueryKittyKeyboardFlags.Terminator, request.Terminator);
                                                            Assert.Equal (EscSeqUtils.CSI_QueryKittyKeyboardFlags.Value, request.Value);
                                                            request.ResponseReceived ("\u001B[?31u");
                                                        });

        KittyKeyboardProtocolDetector detector = new (driverMock.Object);

        KittyKeyboardCapabilities? result = null;

        detector.Detect (r => result = r);

        Assert.NotNull (result);
        Assert.True (result.IsSupported);
        Assert.Equal (EscSeqUtils.KittyKeyboardRequestedFlags, result.Flags);
        driverMock.Verify (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()), Times.Once);
    }

    [Fact]
    public void Detect_Registers_And_Completes_StartupGate ()
    {
        DateTime nowUtc = DateTime.UtcNow;
        AnsiStartupGate startupGate = new (() => nowUtc);
        AnsiEscapeSequenceRequest? capturedRequest = null;

        Mock<IDriver> driverMock = new (MockBehavior.Strict);
        driverMock.Setup (d => d.IsLegacyConsole).Returns (false);
        driverMock.Setup (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()))
                  .Callback<AnsiEscapeSequenceRequest> (request => capturedRequest = request);

        KittyKeyboardProtocolDetector detector = new (driverMock.Object, startupGate);

        detector.Detect (_ => { });

        Assert.NotNull (capturedRequest);
        Assert.False (startupGate.IsReady);
        Assert.Equal ([AnsiStartupQuery.KittyKeyboard], startupGate.PendingQueries);

        capturedRequest!.ResponseReceived ("\u001B[?31u");

        Assert.True (startupGate.IsReady);
        Assert.Empty (startupGate.PendingQueries);
    }

    [Fact]
    public void Detect_ReturnsUnsupportedResult_WhenTerminalDoesNotRespond ()
    {
        Mock<IDriver> driverMock = new (MockBehavior.Strict);
        driverMock.Setup (d => d.IsLegacyConsole).Returns (false);

        driverMock.Setup (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()))
                  .Callback<AnsiEscapeSequenceRequest> (request => request.Abandoned?.Invoke ());

        KittyKeyboardProtocolDetector detector = new (driverMock.Object);

        KittyKeyboardCapabilities? result = null;

        detector.Detect (r => result = r);

        Assert.NotNull (result);
        Assert.False (result.IsSupported);
        Assert.Equal (0, (int)result.Flags);
    }

    [Fact]
    public void Detect_SkipsLegacyConsole ()
    {
        Mock<IDriver> driverMock = new (MockBehavior.Strict);
        driverMock.Setup (d => d.IsLegacyConsole).Returns (true);

        KittyKeyboardProtocolDetector detector = new (driverMock.Object);

        KittyKeyboardCapabilities? result = null;

        detector.Detect (r => result = r);

        Assert.NotNull (result);
        Assert.False (result.IsSupported);
        driverMock.Verify (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()), Times.Never);
    }

    [Theory]
    [InlineData ("\u001B[?0u", true, 0)]
    [InlineData ("\u001B[?31u", true, 31)]
    [InlineData ("[?1u", true, 1)]
    [InlineData ("\u001B[1;5u", false, 0)]
    [InlineData ("", false, 0)]
    public void ParseResponse_ReturnsExpectedResult (string response, bool isSupported, int supportedFlags)
    {
        KittyKeyboardCapabilities result = KittyKeyboardProtocolDetector.ParseResponse (response);

        Assert.Equal (isSupported, result.IsSupported);
        Assert.Equal ((KittyKeyboardFlags)supportedFlags, result.Flags);
    }
}
