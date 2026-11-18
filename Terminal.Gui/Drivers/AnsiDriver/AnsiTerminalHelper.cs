namespace Terminal.Gui.Drivers;

/// <summary>
///     Dispatches platform-specific terminal operations to the appropriate helper.
///     Contains zero P/Invoke — all native calls live in <see cref="UnixIOHelper"/> and
///     <see cref="WindowsVTOutputHelper"/>.
/// </summary>
internal static class AnsiTerminalHelper
{
    /// <summary>
    ///     Flushes stdout using the platform-appropriate native mechanism.
    /// </summary>
    /// <param name="platform">The detected platform.</param>
    public static void FlushNative (AnsiPlatform platform)
    {
        try
        {
            switch (platform)
            {
                case AnsiPlatform.UnixRaw:
                    UnixIOHelper.FlushStdout ();

                    break;

                case AnsiPlatform.WindowsVT:
                    WindowsVTOutputHelper.FlushStdout ();

                    break;
            }
        }
        catch
        {
            // Ignore exceptions during flush — don't crash the app if flush fails in unit tests.
        }
    }
}
