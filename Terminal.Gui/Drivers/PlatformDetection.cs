using System.Runtime.InteropServices;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Helper class for detecting platform-specific features.
/// </summary>
public static class PlatformDetection
{
    /// <summary>
    ///     Determines if the current platform is WSL (Windows Subsystem for Linux).
    /// </summary>
    /// <returns>True if running on WSL, false otherwise.</returns>
    public static bool IsWSL ()
    {
        const string PROC_VERSION = "/proc/version";

        try
        {
            return File.ReadAllText (PROC_VERSION).Contains ("microsoft", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
