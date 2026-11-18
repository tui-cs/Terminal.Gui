namespace Terminal.Gui.Drawing;

/// <summary>
///     Detects whether the active terminal supports the Kitty graphics protocol by inspecting
///     well-known environment variables set by Kitty-compatible terminals.
/// </summary>
/// <remarks>
///     <para>
///         Detection checks the following environment variables in order:
///         <list type="number">
///             <item><c>KITTY_WINDOW_ID</c> — set by the Kitty terminal emulator.</item>
///             <item><c>TERM_PROGRAM</c> equal to <c>kitty</c> or <c>ghostty</c>.</item>
///         </list>
///     </para>
///     <para>
///         When support is confirmed, the detector attempts to derive the pixel-per-cell
///         <see cref="KittyGraphicsSupportResult.Resolution"/> via ANSI window-size queries
///         (the same fallback used by <see cref="SixelSupportDetector"/>).
///     </para>
/// </remarks>
public class KittyGraphicsSupportDetector
{
    private readonly IDriver? _driver;

    /// <summary>
    ///     Creates a new instance of the <see cref="KittyGraphicsSupportDetector"/> class.
    /// </summary>
    public KittyGraphicsSupportDetector () { }

    /// <summary>
    ///     Creates a new instance of the <see cref="KittyGraphicsSupportDetector"/> class
    ///     bound to the specified driver (used for resolution queries).
    /// </summary>
    /// <param name="driver">The driver to send ANSI requests through.</param>
    public KittyGraphicsSupportDetector (IDriver driver)
    {
        ArgumentNullException.ThrowIfNull (driver);
        _driver = driver;
    }

    /// <summary>
    ///     Detects Kitty graphics protocol support and invokes <paramref name="resultCallback"/>
    ///     with the result.  When support is detected the detector also attempts to resolve
    ///     <see cref="KittyGraphicsSupportResult.Resolution"/> via ANSI queries; if those are
    ///     unavailable the default 10×20 resolution is used.
    /// </summary>
    /// <param name="resultCallback">Called when detection is complete.</param>
    public void Detect (Action<KittyGraphicsSupportResult> resultCallback)
    {
        ArgumentNullException.ThrowIfNull (resultCallback);

        KittyGraphicsSupportResult result = new ()
        {
            IsSupported = IsKittySupportedByEnvironment ()
        };

        if (!result.IsSupported || _driver is null)
        {
            resultCallback (result);

            return;
        }

        TryComputeResolution (result, resultCallback);
    }

    private static bool IsKittySupportedByEnvironment ()
    {
        string? kittyWindowId = Environment.GetEnvironmentVariable ("KITTY_WINDOW_ID");

        if (!string.IsNullOrWhiteSpace (kittyWindowId))
        {
            return true;
        }

        string? termProgram = Environment.GetEnvironmentVariable ("TERM_PROGRAM");

        return string.Equals (termProgram, "kitty", StringComparison.OrdinalIgnoreCase)
               || string.Equals (termProgram, "ghostty", StringComparison.OrdinalIgnoreCase);
    }

    private void TryComputeResolution (KittyGraphicsSupportResult result, Action<KittyGraphicsSupportResult> resultCallback)
    {
        string? consoleSize;

        QueueRequest (EscSeqUtils.CSI_RequestWindowSizeInPixels,
                      r1 =>
                      {
                          consoleSize = r1;

                          QueueRequest (EscSeqUtils.CSI_ReportWindowSizeInChars,
                                        r2 =>
                                        {
                                            if (consoleSize is { })
                                            {
                                                ComputeResolution (result, consoleSize, r2);
                                            }

                                            resultCallback (result);
                                        },
                                        () => resultCallback (result));
                      },
                      () => resultCallback (result));
    }

    private static void ComputeResolution (KittyGraphicsSupportResult result, string consoleSize, string sizeInChars)
    {
        System.Text.RegularExpressions.Match pixelMatch =
            System.Text.RegularExpressions.Regex.Match (consoleSize, @"\[\d+;(\d+);(\d+)t$");

        System.Text.RegularExpressions.Match charMatch =
            System.Text.RegularExpressions.Regex.Match (sizeInChars, @"\[\d+;(\d+);(\d+)t$");

        if (!pixelMatch.Success
            || !charMatch.Success
            || !int.TryParse (pixelMatch.Groups [1].Value, out int pixelHeight)
            || !int.TryParse (pixelMatch.Groups [2].Value, out int pixelWidth)
            || !int.TryParse (charMatch.Groups [1].Value, out int charHeight)
            || !int.TryParse (charMatch.Groups [2].Value, out int charWidth)
            || charWidth == 0
            || charHeight == 0)
        {
            return;
        }

        result.Resolution = new Size (
            (int)Math.Round ((double)pixelWidth / charWidth),
            (int)Math.Round ((double)pixelHeight / charHeight));
    }

    private void QueueRequest (AnsiEscapeSequence req, Action<string> responseCallback, Action abandoned)
    {
        AnsiEscapeSequenceRequest newRequest = new ()
        {
            Request = req.Request,
            Value = req.Value,
            Terminator = req.Terminator,
            ResponseReceived = responseCallback!,
            Abandoned = abandoned
        };

        _driver?.QueueAnsiRequest (newRequest);
    }
}
