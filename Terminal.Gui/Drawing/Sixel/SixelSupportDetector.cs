using System.Text.RegularExpressions;

namespace Terminal.Gui.Drawing;

/// <summary>
///     Uses Ansi escape sequences to detect whether sixel is supported
///     by the terminal.
/// </summary>
public class SixelSupportDetector ()
{
    private readonly IDriver? _driver;

    /// <summary>
    ///     Creates a new instance of the <see cref="SixelSupportDetector"/> class.
    /// </summary>
    /// <param name="driver"></param>
    public SixelSupportDetector (IDriver? driver) : this ()
    {
        ArgumentNullException.ThrowIfNull (driver);

        _driver = driver;
    }

    /// <summary>
    ///     Sends Ansi escape sequences to the console to determine whether
    ///     sixel is supported (and <see cref="SixelSupportResult.Resolution"/>
    ///     etc).
    /// </summary>
    /// <returns>
    ///     Description of sixel support, may include assumptions where
    ///     expected response codes are not returned by console.
    /// </returns>
    public void Detect (Action<SixelSupportResult> resultCallback)
    {
        var result = new SixelSupportResult ();
        bool isLegacyConsole = IsLegacyConsole ();
        result.SupportsTransparency = !isLegacyConsole || (!isLegacyConsole && IsXtermWithTransparency ());
        IsSixelSupportedByDar (result, resultCallback);
    }

    private void TryGetResolutionDirectly (SixelSupportResult result, Action<SixelSupportResult> resultCallback) =>

        // Expect something like:
        //<esc>[6;20;10t
        QueueRequest (EscSeqUtils.CSI_RequestSixelResolution,
                      r =>
                      {
                          // Terminal supports directly responding with resolution
                          Match match = Regex.Match (r, @"\[\d+;(\d+);(\d+)t$");

                          if (match.Success)
                          {
                              if (int.TryParse (match.Groups [1].Value, out int ry) && int.TryParse (match.Groups [2].Value, out int rx))
                              {
                                  result.Resolution = new Size (rx, ry);
                              }
                          }

                          // Finished
                          resultCallback.Invoke (result);

                          //Logging.Information ($"Receive resolution response {result.Resolution}");
                      },

                      // Request failed, so try to compute instead
                      () => TryComputeResolution (result, resultCallback));

    //Logging.Information ($"Sent resolution request {EscSeqUtils.CSI_RequestSixelResolution.Request}");
    private void TryComputeResolution (SixelSupportResult result, Action<SixelSupportResult> resultCallback)
    {
        string consoleSize;
        string sizeInChars;

        QueueRequest (EscSeqUtils.CSI_RequestWindowSizeInPixels,
                      r1 =>
                      {
                          consoleSize = r1;

                          QueueRequest (EscSeqUtils.CSI_ReportWindowSizeInChars,
                                        r2 =>
                                        {
                                            sizeInChars = r2;
                                            ComputeResolution (result, consoleSize, sizeInChars);
                                            resultCallback (result);
                                        },
                                        () => resultCallback (result));
                      },
                      () => resultCallback (result));

        //Logging.Information ($"Sent pixel size request {EscSeqUtils.CSI_RequestWindowSizeInPixels.Request}");
    }

    private void ComputeResolution (SixelSupportResult result, string consoleSize, string sizeInChars)
    {
        // Fallback to window size in pixels and characters
        // Example [4;600;1200t
        Match pixelMatch = Regex.Match (consoleSize, @"\[\d+;(\d+);(\d+)t$");

        // Example [8;30;120t
        Match charMatch = Regex.Match (sizeInChars, @"\[\d+;(\d+);(\d+)t$");

        if (pixelMatch.Success
            && charMatch.Success
            && int.TryParse (pixelMatch.Groups [1].Value, out int pixelHeight)
            && int.TryParse (pixelMatch.Groups [2].Value, out int pixelWidth)
            &&

            // Extract character dimensions
            int.TryParse (charMatch.Groups [1].Value, out int charHeight)
            && int.TryParse (charMatch.Groups [2].Value, out int charWidth)
            && charWidth != 0
            && charHeight != 0)
        { // Extract pixel dimensions
            // Avoid divide by zero
            // Calculate the character cell size in pixels
            var cellWidth = (int)Math.Round ((double)pixelWidth / charWidth);
            var cellHeight = (int)Math.Round ((double)pixelHeight / charHeight);

            // Set the resolution based on the character cell size
            result.Resolution = new Size (cellWidth, cellHeight);
        }
    }

    private void IsSixelSupportedByDar (SixelSupportResult result, Action<SixelSupportResult> resultCallback) =>
        QueueRequest (EscSeqUtils.CSI_SendDeviceAttributes,
                      r =>
                      {
                          result.IsSupported = ResponseIndicatesSupport (r);

                          //Logging.Information ($"Receive dar response {result.IsSupported}");

                          if (result.IsSupported)
                          {
                              TryGetResolutionDirectly (result, resultCallback);
                          }
                          else
                          {
                              resultCallback (result);
                          }
                      },
                      () => resultCallback (result));

    //Logging.Information ($"Sent dar request {EscSeqUtils.CSI_SendDeviceAttributes.Request}");
    private void QueueRequest (AnsiEscapeSequence req, Action<string> responseCallback, Action abandoned)
    {
        var newRequest = new AnsiEscapeSequenceRequest
        {
            Request = req.Request,
            Value = req.Value,
            Terminator = req.Terminator,
            ResponseReceived = responseCallback!,
            Abandoned = abandoned
        };

        _driver?.QueueAnsiRequest (newRequest);
    }

    private static bool ResponseIndicatesSupport (string response) => response.Split (';').Contains ("4");

    private bool IsLegacyConsole () => _driver is { IsLegacyConsole: true };

    private static bool IsXtermWithTransparency ()
    {
        // Check if running in real xterm (XTERM_VERSION is more reliable than TERM)
        string xtermVersionStr = Environment.GetEnvironmentVariable (@"XTERM_VERSION")!;

        // If XTERM_VERSION exists, we are in a real xterm
        if (!string.IsNullOrWhiteSpace (xtermVersionStr) && int.TryParse (xtermVersionStr, out int xtermVersion) && xtermVersion >= 370)
        {
            return true;
        }

        return false;
    }
}
