using System.Runtime.InteropServices;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Helper class for detecting platform-specific features.
/// </summary>
public static class PlatformDetection
{
    /// <summary>
    ///     The .NET OS platform names Terminal.Gui carries platform-specific data for, in probe order.
    /// </summary>
    /// <remarks>
    ///     These are the names <see cref="OperatingSystem.IsOSPlatform"/> matches against. .NET reports exactly one
    ///     of them for any given runtime. Note that <c>ANDROID</c> is distinct from <c>LINUX</c>:
    ///     <see cref="OperatingSystem.IsLinux"/> returns <see langword="false"/> on Android even though Android
    ///     runs a Linux kernel.
    /// </remarks>
    internal static readonly string [] KnownPlatformNames =
    [
        "WINDOWS",
        "LINUX",
        "ANDROID",
        "OSX",
        "FREEBSD",
        "NETBSD",
        "OPENBSD",
        "SOLARIS",
        "ILLUMOS",
        "HAIKU"
    ];

    /// <summary>
    ///     Gets the .NET OS platform name for the current platform (for example <c>LINUX</c>, <c>OSX</c>, or
    ///     <c>ANDROID</c>).
    /// </summary>
    /// <returns>
    ///     The matching entry from <see cref="KnownPlatformNames"/>, or <see cref="string.Empty"/> when the current
    ///     platform is not one Terminal.Gui has platform-specific data for (for example <c>BROWSER</c>).
    /// </returns>
    internal static string GetPlatformName ()
    {
        foreach (string name in KnownPlatformNames)
        {
            if (!RuntimeInformation.IsOSPlatform (OSPlatform.Create (name)))
            {
                continue;
            }

            return name;
        }

        return string.Empty;
    }

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
