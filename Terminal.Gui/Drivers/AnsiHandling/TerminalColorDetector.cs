namespace Terminal.Gui.Drivers;

/// <summary>
///     Uses OSC 10/11 ANSI escape sequences to query the terminal's actual default foreground
///     and background colors. Follows the same async callback pattern as <see cref="Drawing.SixelSupportDetector"/>.
/// </summary>
public class TerminalColorDetector
{
    private static readonly TimeSpan StartupTerminalColorsQueryTimeout = TimeSpan.FromSeconds (1);

    private readonly IDriver? _driver;
    private readonly IAnsiStartupGate? _startupGate;

    /// <summary>
    ///     Creates a new instance of the <see cref="TerminalColorDetector"/> class.
    /// </summary>
    /// <param name="driver">The driver to send ANSI requests through.</param>
    /// <param name="startupGate">Optional startup gate to mark query readiness.</param>
    public TerminalColorDetector (IDriver? driver, IAnsiStartupGate? startupGate = null)
    {
        ArgumentNullException.ThrowIfNull (driver);
        _driver = driver;
        _startupGate = startupGate;
    }

    /// <summary>
    ///     Sends OSC 10 and OSC 11 queries to detect the terminal's default foreground and background colors.
    ///     Results are delivered asynchronously via the <paramref name="resultCallback"/>.
    /// </summary>
    /// <param name="resultCallback">
    ///     Called when detection is complete. Parameters are (foreground, background),
    ///     either of which may be <see langword="null"/> if the terminal did not respond.
    /// </param>
    public void Detect (Action<Color?, Color?> resultCallback)
    {
        // Skip on legacy console — it doesn't support ANSI escape sequences
        if (_driver is { IsLegacyConsole: true })
        {
            resultCallback (null, null);

            return;
        }

        IDisposable? terminalColorsQueryCompletionHandle = _startupGate?.RegisterQuery (AnsiStartupQuery.TerminalColors,
                                                                                          StartupTerminalColorsQueryTimeout);

        // Query foreground first (OSC 10), then background (OSC 11)
        QueryForeground ((fg, bg) =>
                          {
                              terminalColorsQueryCompletionHandle?.Dispose ();
                              resultCallback (fg, bg);
                          });
    }

    private void QueryForeground (Action<Color?, Color?> resultCallback) =>
        QueueRequest (EscSeqUtils.OSC_QueryForegroundColor,
                      response =>
                      {
                          EscSeqUtils.TryParseOscColorResponse (response, out Color? fg);

                          // Chain: now query background
                          QueryBackground (fg, resultCallback);
                      },
                      () =>
                      {
                          // Foreground query abandoned — still try background
                          QueryBackground (null, resultCallback);
                      });

    private void QueryBackground (Color? fg, Action<Color?, Color?> resultCallback) =>
        QueueRequest (EscSeqUtils.OSC_QueryBackgroundColor,
                      response =>
                      {
                          EscSeqUtils.TryParseOscColorResponse (response, out Color? bg);
                          resultCallback (fg, bg);
                      },
                      () =>
                      {
                          // Background query also abandoned
                          resultCallback (fg, null);
                      });

    private void QueueRequest (AnsiEscapeSequence req, Action<string> responseCallback, Action abandoned)
    {
        AnsiEscapeSequenceRequest request = new ()
        {
            Request = req.Request,
            Value = req.Value,
            Terminator = req.Terminator,
            ResponseReceived = r => responseCallback (r ?? string.Empty),
            Abandoned = abandoned
        };

        _driver?.QueueAnsiRequest (request);
    }
}
