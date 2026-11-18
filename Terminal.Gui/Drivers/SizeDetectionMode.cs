namespace Terminal.Gui.Drivers;

/// <summary>
///     Controls how the ANSI driver detects the terminal's window size.
/// </summary>
public enum SizeDetectionMode
{
    /// <summary>
    ///     Sends a <c>CSI 18t</c> ANSI escape-sequence query and parses the
    ///     <c>ESC [ 8 ; height ; width t</c> response. Works over SSH or any
    ///     ANSI-compatible terminal. Use this as an explicit fallback when a
    ///     native size query is unavailable or does not report the transport's size.
    /// </summary>
    AnsiQuery,

    /// <summary>
    ///     Uses <c>ioctl(TIOCGWINSZ)</c> on Unix/macOS or the Console API on Windows.
    ///     This is the default on supported local platforms because it is synchronous,
    ///     immediate, and does not generate terminal query traffic.
    /// </summary>
    Polling
}
