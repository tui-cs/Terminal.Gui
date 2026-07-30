using System.Runtime.InteropServices;

namespace Terminal.Gui.Drivers;

internal static class SuspendHelper
{
    private static int _suspendSignal;

    /// <summary>Suspends the process by sending SIGTSTP to the process group.</summary>
    /// <returns>True if the suspension was successful.</returns>
    public static bool Suspend ()
    {
        int signal = GetSuspendSignal ();

        Logging.Information ($"SuspendHelper.Suspend: signal={signal}");

        if (signal == -1)
        {
            Logging.Warning ("SuspendHelper.Suspend: No suspend signal for this platform");

            return false;
        }

        Logging.Information ($"SuspendHelper.Suspend: Calling killpg(0, {signal}) [SIGTSTP]...");
        int result = killpg (0, signal);
        int errno = Marshal.GetLastWin32Error ();
        Logging.Information ($"SuspendHelper.Suspend: killpg returned {result}, errno={errno}");

        return true;
    }

    private static int GetSuspendSignal ()
    {
        if (_suspendSignal != 0)
        {
            return _suspendSignal;
        }

        _suspendSignal = MapSuspendSignal (PlatformDetection.GetPlatformName ());

        return _suspendSignal;
    }

    /// <summary>
    ///     Maps a .NET OS platform name to that platform's <c>SIGTSTP</c> signal number.
    /// </summary>
    /// <param name="platformName">
    ///     A platform name from <see cref="PlatformDetection.KnownPlatformNames"/>, as returned by
    ///     <see cref="PlatformDetection.GetPlatformName"/>.
    /// </param>
    /// <returns>The <c>SIGTSTP</c> number, or -1 when the platform has no known suspend signal.</returns>
    /// <remarks>
    ///     Values come from each platform's <c>signal.h</c>. Two entries are easy to get wrong:
    ///     <list type="bullet">
    ///         <item>
    ///             Haiku's <c>SIGTSTP</c> is 13. 21 is <c>SIGKILLTHR</c> there, so sending 21 would kill threads
    ///             instead of suspending.
    ///         </item>
    ///         <item>
    ///             Android needs its own entry because <see cref="OperatingSystem.IsLinux"/> returns
    ///             <see langword="false"/> on Android.
    ///         </item>
    ///     </list>
    /// </remarks>
    internal static int MapSuspendSignal (string platformName) =>
        platformName switch
        {
            // Darwin (all Apple platforms) and the BSDs share the historical BSD signal numbering.
            "OSX" or "MACCATALYST" or "IOS" or "TVOS" or "FREEBSD" or "NETBSD" or "OPENBSD" => 18,

            // Linux, on every architecture .NET targets. (Linux/MIPS uses 24, but .NET has no MIPS target.)
            "LINUX" or "ANDROID" => 20,
            "SOLARIS" or "ILLUMOS" => 24,
            "HAIKU" => 13,
            _ => -1
        };

    [DllImport ("libc", SetLastError = true)]
    private static extern int killpg (int pgrp, int sig);
}
