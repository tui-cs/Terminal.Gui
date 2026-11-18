#nullable enable
using Moq;

namespace DrawingTests;

public class SixelSupportDetectorTests
{
    [Fact]
    public void Detect_SetsSupportedAndResolution_WhenDeviceAttributesContain4_AndResolutionResponds()
    {
        // Arrange
        Mock<IDriver> driverMock = new (MockBehavior.Strict);

        // Setup IsLegacyConsole - false means modern terminal with ANSI support
        driverMock.Setup (d => d.IsLegacyConsole).Returns (false);

        // Expect QueueAnsiRequest to be called at least twice:
        // 1) CSI_SendDeviceAttributes (terminator "c")
        // 2) CSI_RequestSixelResolution (terminator "t")
        driverMock.Setup (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()))
                  .Callback<AnsiEscapeSequenceRequest> (req =>
                                                      {
                                                          // Respond to the SendDeviceAttributes request with a value that indicates support (contains "4")
                                                          if (req.Request == EscSeqUtils.CSI_SendDeviceAttributes.Request)
                                                          {
                                                              req.ResponseReceived.Invoke ("1;4;7c");
                                                          }
                                                          else if (req.Request == EscSeqUtils.CSI_RequestSixelResolution.Request)
                                                          {
                                                              // Reply with a resolution response matching regex "\[\d+;(\d+);(\d+)t$"
                                                              // Group 1 -> ry, Group 2 -> rx. The detector constructs resolution as new(rx, ry)
                                                              req.ResponseReceived.Invoke ("[6;20;10t");
                                                          }
                                                          else
                                                          {
                                                              // Any other request - call abandoned to avoid hanging
                                                              req.Abandoned?.Invoke ();
                                                          }
                                                      })
                  .Verifiable ();

        var detector = new SixelSupportDetector (driverMock.Object);

        SixelSupportResult? final = null;

        // Act
        detector.Detect (r => final = r);

        // Assert
        Assert.NotNull (final);
        Assert.True (final.IsSupported); // Response contained "4"
        // Resolution should be constructed as new(rx, ry) where rx=10, ry=20 from our reply "[6;20;10t"
        Assert.Equal (10, final.Resolution.Width);
        Assert.Equal (20, final.Resolution.Height);

        driverMock.Verify (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()), Times.AtLeast (2));
    }

    [Fact]
    public void Detect_DoesNotSetSupported_WhenDeviceAttributesDoNotContain4()
    {
        // Arrange
        var driverMock = new Mock<IDriver>(MockBehavior.Strict);

        // Setup IsLegacyConsole - false means modern terminal with ANSI support
        driverMock.Setup (d => d.IsLegacyConsole).Returns (false);

        driverMock.Setup (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()))
                  .Callback<AnsiEscapeSequenceRequest> (req =>
                                                      {
                                                          // SendDeviceAttributes -> reply without "4"
                                                          if (req.Request == EscSeqUtils.CSI_SendDeviceAttributes.Request)
                                                          {
                                                              req.ResponseReceived.Invoke ("1;0;7c");
                                                          }
                                                          else
                                                          {
                                                              // Any other requests should be abandoned
                                                              req.Abandoned?.Invoke ();
                                                          }
                                                      })
                  .Verifiable ();

        var detector = new SixelSupportDetector (driverMock.Object);

        SixelSupportResult? final = null;

        // Act
        detector.Detect (r => final = r);

        // Assert
        Assert.NotNull (final);
        Assert.False (final.IsSupported);
        // On no support, the direct resolution request path isn't followed so resolution remains the default
        Assert.Equal (10, final.Resolution.Width);
        Assert.Equal (20, final.Resolution.Height);

        driverMock.Verify (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()), Times.AtLeast (1));
    }

    [Theory]
    [InlineData (true)]
    [InlineData (false)]
    public void Detect_SetsSupported_WhenIsLegacyConsoleIsFalseAndResponseContain4OrFalse (bool isLegacyConsole)
    {
        // Arrange
        var responseReceived = false;
        var output = new AnsiOutput ();
        output.IsLegacyConsole = isLegacyConsole;

        Mock<DriverImpl> driverMock = new (
                                           MockBehavior.Strict,
                                           new AnsiComponentFactory (),
                                           new AnsiInputProcessor (null!),
                                           new OutputBufferImpl (),
                                           output,
                                           new AnsiRequestScheduler (new AnsiResponseParser (new SystemTimeProvider ())),
                                           new SizeMonitorImpl (output)
                                          );
        driverMock.Setup (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()))
                  .Callback<AnsiEscapeSequenceRequest> (req =>
                                                        {
                                                            if (req.Request == EscSeqUtils.CSI_SendDeviceAttributes.Request)
                                                            {
                                                                responseReceived = true;

                                                                if (!isLegacyConsole)
                                                                {
                                                                    // Response does contain "4" (so DAR indicates has sixel)
                                                                    req.ResponseReceived.Invoke ("?1;4;0;7c");
                                                                }
                                                                else
                                                                {
                                                                    // Response does NOT contain "4" (so DAR indicates no sixel)
                                                                    req.ResponseReceived.Invoke ("");
                                                                }
                                                            }
                                                            else
                                                            {
                                                                // Abandon all requests
                                                                req.Abandoned?.Invoke ();
                                                            }
                                                        })
                  .Verifiable ();

        var detector = new SixelSupportDetector (driverMock.Object);
        SixelSupportResult? final = null;

        // Act
        detector.Detect (r => final = r);

        // Assert
        Assert.Equal (isLegacyConsole, driverMock.Object.IsLegacyConsole);
        Assert.NotNull (final);

        if (!isLegacyConsole)
        {
            Assert.True (final.IsSupported);
        }
        else
        {
            // Not a real VT, so should be supported
            Assert.False (final.IsSupported);
        }
        Assert.True (responseReceived);
        driverMock.Verify (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()), Times.AtLeast (1));
    }

    [Theory]
    [InlineData (true)]
    [InlineData (false)]
    public void Detect_SetsSupported_WhenIsLegacyConsoleIsTrueOrFalse_With_Response (bool isLegacyConsole)
    {
        // Arrange
        var responseReceived = false;
        var output = new AnsiOutput ();
        output.IsLegacyConsole = isLegacyConsole;

        Mock<DriverImpl> driverMock = new (
                                           MockBehavior.Strict,
                                           new AnsiComponentFactory (),
                                           new AnsiInputProcessor (null!),
                                           new OutputBufferImpl (),
                                           output,
                                           new AnsiRequestScheduler (new AnsiResponseParser (new SystemTimeProvider ())),
                                           new SizeMonitorImpl (output)
                                          );

        driverMock.Setup (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()))
                  .Callback<AnsiEscapeSequenceRequest> (req =>
                                                        {
                                                            if (req.Request == EscSeqUtils.CSI_SendDeviceAttributes.Request)
                                                            {
                                                                responseReceived = true;

                                                                // Respond to the SendDeviceAttributes request with a value that indicates support (contains "4")
                                                                // Respond to the SendDeviceAttributes request with an empty value that indicates non-support
                                                                req.ResponseReceived.Invoke (!driverMock.Object.IsLegacyConsole ? "1;4;7c" : "");
                                                            }

                                                            // Abandon all requests
                                                            req.Abandoned?.Invoke ();
                                                        })
                  .Verifiable ();

        var detector = new SixelSupportDetector (driverMock.Object);
        SixelSupportResult? final = null;

        // Act
        detector.Detect (r => final = r);

        // Assert
        Assert.Equal (isLegacyConsole, driverMock.Object.IsLegacyConsole);
        Assert.NotNull (final);

        if (!isLegacyConsole)
        {
            Assert.True (final.IsSupported);
            Assert.True (final.SupportsTransparency);
        }
        else
        {
            // Not a real VT, so shouldn't be supported
            Assert.False (final.IsSupported);
            Assert.False (final.SupportsTransparency);
        }

        Assert.True (responseReceived);
    }
}
