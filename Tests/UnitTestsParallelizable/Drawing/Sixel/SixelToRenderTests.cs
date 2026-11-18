#nullable enable
using System.Collections.Concurrent;
using Moq;

namespace DrawingTests;

public class SixelToRenderTests
{
    [Fact]
    public void SixelToRender_Properties_AreGettableAndSettable ()
    {
        SixelToRender s = new SixelToRender
        {
            SixelData = "SIXEL-DATA",
            ScreenPosition = new (3, 5)
        };

        Assert.Equal ("SIXEL-DATA", s.SixelData);
        Assert.Equal (3, s.ScreenPosition.X);
        Assert.Equal (5, s.ScreenPosition.Y);
    }

    [Fact]
    public void SixelSupportResult_DefaultValues_AreExpected ()
    {
        var r = new SixelSupportResult ();

        Assert.False (r.IsSupported);
        Assert.Equal (10, r.Resolution.Width);
        Assert.Equal (20, r.Resolution.Height);
        Assert.Equal (256, r.MaxPaletteColors);
        Assert.False (r.SupportsTransparency);
    }

    [Fact]
    public void Detect_WhenDeviceAttributesIndicateSupport_GetsResolutionDirectly ()
    {
        // Arrange
        Mock<IDriver> driverMock = new (MockBehavior.Strict);

        // Setup IsLegacyConsole - false means modern terminal with ANSI support
        driverMock.Setup (d => d.IsLegacyConsole).Returns (false);

        driverMock.Setup (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()))
                  .Callback<AnsiEscapeSequenceRequest> (req =>
                                                      {
                                                          if (req.Request == EscSeqUtils.CSI_SendDeviceAttributes.Request)
                                                          {
                                                              // Response contains "4" -> indicates sixel support
                                                              req.ResponseReceived.Invoke ("?1;4;7c");
                                                          }
                                                          else if (req.Request == EscSeqUtils.CSI_RequestSixelResolution.Request)
                                                          {
                                                              // Return resolution: "[6;20;10t" (group1=20 -> ry, group2=10 -> rx)
                                                              req.ResponseReceived.Invoke ("[6;20;10t");
                                                          }
                                                          else
                                                          {
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
        Assert.True (final.IsSupported);
        Assert.Equal (10, final.Resolution.Width);
        Assert.Equal (20, final.Resolution.Height);

        driverMock.Verify (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()), Times.AtLeast (2));
    }

    [Fact]
    public void Detect_WhenDirectResolutionFails_ComputesResolutionFromWindowSizes ()
    {
        // Arrange
        Mock<IDriver> driverMock = new (MockBehavior.Strict);

        // Setup IsLegacyConsole - false means modern terminal with ANSI support
        driverMock.Setup (d => d.IsLegacyConsole).Returns (false);

        driverMock.Setup (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()))
                  .Callback<AnsiEscapeSequenceRequest> (req =>
                                                      {
                                                          switch (req.Request)
                                                          {
                                                              case var r when r == EscSeqUtils.CSI_SendDeviceAttributes.Request:
                                                                  // Indicate sixel support so flow continues to try resolution
                                                                  req.ResponseReceived.Invoke ("?1;4;7c");
                                                                  break;

                                                              case var r when r == EscSeqUtils.CSI_RequestSixelResolution.Request:
                                                                  // Simulate failure to return resolution directly
                                                                  req.Abandoned?.Invoke ();
                                                                  break;

                                                              case var r when r == EscSeqUtils.CSI_RequestWindowSizeInPixels.Request:
                                                                  // Pixel dimensions reply: [4;600;1200t  -> pixelHeight=600; pixelWidth=1200
                                                                  req.ResponseReceived.Invoke ("[4;600;1200t");
                                                                  break;

                                                              case var r when r == EscSeqUtils.CSI_ReportWindowSizeInChars.Request:
                                                                  // Character dimensions reply: [8;30;120t -> charHeight=30; charWidth=120
                                                                  req.ResponseReceived.Invoke ("[8;30;120t");
                                                                  break;

                                                              default:
                                                                  req.Abandoned?.Invoke ();
                                                                  break;
                                                          }
                                                      })
                  .Verifiable ();

        var detector = new SixelSupportDetector (driverMock.Object);

        SixelSupportResult? final = null;

        // Act
        detector.Detect (r => final = r);

        // Assert
        Assert.NotNull (final);
        Assert.True (final.IsSupported);
        // Expect cell width = round(1200 / 120) = 10, cell height = round(600 / 30) = 20
        Assert.Equal (10, final.Resolution.Width);
        Assert.Equal (20, final.Resolution.Height);

        driverMock.Verify (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()), Times.AtLeast (3));
    }

    [Fact]
    public void Detect_WhenDeviceAttributesDoNotIndicateSupport_ReturnsNotSupported ()
    {
        // Arrange
        Mock<IDriver> driverMock = new (MockBehavior.Strict);

        // Setup IsLegacyConsole - false means modern terminal with ANSI support
        driverMock.Setup (d => d.IsLegacyConsole).Returns (false);

        driverMock.Setup (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()))
                  .Callback<AnsiEscapeSequenceRequest> (req =>
                                                      {
                                                          if (req.Request == EscSeqUtils.CSI_SendDeviceAttributes.Request)
                                                          {
                                                              // Response does NOT contain "4"
                                                              req.ResponseReceived.Invoke ("?1;0;7c");
                                                          }
                                                          else
                                                          {
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

        driverMock.Verify (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()), Times.AtLeastOnce ());
    }

    [Theory]
    [InlineData ("", true, false, false, false)]
    [InlineData ("", true, true, false, false)]
    [InlineData ("?1;0;7c", false, false, false, true)]
    [InlineData ("?1;0;7c", false, true, false, true)]
    [InlineData ("?1;4;0;7c", false, false, true, true)]
    [InlineData ("?1;4;0;7c", false, true, true, true)]
    public void Detect_WhenXtermEnvironmentIndicatesTransparency_SupportsTransparencyEvenIfDAReturnsNo4 (
        string darResponse,
        bool isLegacyConsole,
        bool isXtermWithTransparency,
        bool expectedIsSupported,
        bool expectedSupportsTransparency
    )
    {
        // Arrange - set XTERM_VERSION env var to indicate real xterm with transparency
        string? prev = Environment.GetEnvironmentVariable ("XTERM_VERSION");

        try
        {
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
                                                                  // Response does NOT contain "4" (so DAR indicates no sixel)
                                                                  req.ResponseReceived.Invoke (darResponse);
                                                              }
                                                              else
                                                              {
                                                                  req.Abandoned?.Invoke ();
                                                              }
                                                          })
                      .Verifiable ();

            var detector = new SixelSupportDetector (driverMock.Object);

            SixelSupportResult? final = null;

            if (isXtermWithTransparency)
            {
                Environment.SetEnvironmentVariable ("XTERM_VERSION", "370");
            }

            // Act
            detector.Detect (r => final = r);

            // Assert
            Assert.NotNull (final);
            Assert.Equal (isLegacyConsole, driverMock.Object.IsLegacyConsole);

            // DAR did not indicate sixel support
            Assert.Equal (expectedIsSupported, final.IsSupported);

            // But because XTERM_VERSION >= 370 we expect SupportsTransparency to have been initially true and remain true
            Assert.Equal (expectedSupportsTransparency, final.SupportsTransparency);

            driverMock.Verify (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()), Times.AtLeastOnce ());
        }
        finally
        {
            // Restore environment
            Environment.SetEnvironmentVariable ("XTERM_VERSION", prev);
        }
    }

    [Fact]
    public void ConcurrentQueue_Allows_Mixed_SixelToRender_With_And_Without_Id ()
    {
        var queue = new ConcurrentQueue<SixelToRender> ();

        var withId = new SixelToRender
        {
            Id = "render-1",
            ScreenPosition = new Point (0, 0),
            SixelData = "sixel-1"
        };

        var withoutId = new SixelToRender
        {
            ScreenPosition = new Point (1, 0),
            SixelData = "sixel-2"
            // Id intentionally left null
        };

        var withIdAgain = new SixelToRender
        {
            Id = "render-2",
            ScreenPosition = new Point (2, 0),
            SixelData = "sixel-3"
        };

        // Enqueue mixed items
        queue.Enqueue (withId);
        queue.Enqueue (withoutId);
        queue.Enqueue (withIdAgain);

        // Dequeue and validate order and Id semantics
        Assert.True (queue.TryDequeue (out var first));
        Assert.Equal ("render-1", first.Id);

        Assert.True (queue.TryDequeue (out var second));
        Assert.Null (second.Id);

        Assert.True (queue.TryDequeue (out var third));
        Assert.Equal ("render-2", third.Id);
    }
}