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
        "MACCATALYST",
        "IOS",
        "TVOS",
        "FREEBSD",
        "NETBSD",
        "OPENBSD",
        "SOLARIS",
        "ILLUMOS",
        "HAIKU"
    ];

    /// <summary>
    ///     Determines whether a platform name identifies an Apple (Darwin) platform.
    /// </summary>
    /// <param name="platformName">A platform name, as returned by <see cref="GetPlatformName"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="platformName"/> is an Apple platform.</returns>
    /// <remarks>
    ///     Apple platforms share Darwin's kernel interfaces and, on ARM64, Apple's variadic calling convention, so
    ///     they must be treated as a group rather than testing for macOS alone. watchOS is absent because .NET has no
    ///     <c>WATCHOS</c> platform name — it never reports one, so there is nothing to match.
    /// </remarks>
    internal static bool IsApplePlatformName (string platformName) => platformName is "OSX" or "MACCATALYST" or "IOS" or "TVOS";

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
    ///     Determines whether the current operating system is Linux.
    /// </summary>
    /// <remarks>
    ///     This method returns <see langword="true"/> only when running on a Linux distribution. Other Unix-like
    ///     platforms such as macOS and FreeBSD return <see langword="false"/>, as does Android.
    /// </remarks>
    /// <returns><see langword="true"/> if the operating system is Linux; otherwise, <see langword="false"/>.</returns>
    [Obsolete ("Use OperatingSystem.IsLinux () instead. This shim exists for binary compatibility with v2.4.17 and earlier.")]
    public static bool IsLinux () => OperatingSystem.IsLinux ();

    /// <summary>
    ///     Determines whether the current operating system is macOS.
    /// </summary>
    /// <returns><see langword="true"/> if the current operating system is macOS; otherwise, <see langword="false"/>.</returns>
    [Obsolete ("Use OperatingSystem.IsMacOS () instead. This shim exists for binary compatibility with v2.4.17 and earlier.")]
    public static bool IsMac () => OperatingSystem.IsMacOS ();

    /// <summary>
    ///     Determines if the current platform is Windows.
    /// </summary>
    /// <returns><see langword="true"/> if the operating system is Windows; otherwise, <see langword="false"/>.</returns>
    [Obsolete ("Use OperatingSystem.IsWindows () instead. This shim exists for binary compatibility with v2.4.17 and earlier.")]
    public static bool IsWindows () => OperatingSystem.IsWindows ();

    /// <summary>
    ///     Determines whether the current operating system is a Unix-like platform.
    /// </summary>
    /// <remarks>
    ///     Returns <see langword="true"/> for Linux, macOS (Darwin), and FreeBSD only. Other Unix platforms .NET can
    ///     report — NetBSD, OpenBSD, Solaris, illumos, Haiku — return <see langword="false"/>, which is why driver
    ///     code should test <c>!OperatingSystem.IsWindows ()</c> rather than call this.
    /// </remarks>
    /// <returns>
    ///     <see langword="true"/> if the operating system is Linux, macOS, or FreeBSD; otherwise,
    ///     <see langword="false"/>.
    /// </returns>
    [Obsolete ("Test !OperatingSystem.IsWindows () instead; this method excludes NetBSD, OpenBSD, Solaris, illumos and Haiku. "
               + "This shim exists for binary compatibility with v2.4.17 and earlier.")]
    public static bool IsUnixLike () => OperatingSystem.IsLinux () || OperatingSystem.IsMacOS () || OperatingSystem.IsFreeBSD ();

    /// <summary>Returns the <see cref="TuiPlatform"/> for the current operating system.</summary>
    /// <remarks>Any platform that is neither Windows nor macOS reports <see cref="TuiPlatform.Linux"/>.</remarks>
    [Obsolete ("Use OperatingSystem.IsWindows ()/IsMacOS ()/IsLinux () instead. This shim exists for binary compatibility "
               + "with v2.4.17 and earlier.")]
    public static TuiPlatform GetCurrentPlatform ()
    {
        if (OperatingSystem.IsWindows ())
        {
            return TuiPlatform.Windows;
        }

        if (OperatingSystem.IsMacOS ())
        {
            return TuiPlatform.Macos;
        }

        return TuiPlatform.Linux;
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
