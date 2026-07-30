using System.Runtime.InteropServices;

namespace Terminal.Gui.Drivers;

internal static class SuspendHelper
{
    private static int _suspendSignal;

    /// <summary>Suspends the process by sending <c>SIGTSTP</c> to the process group.</summary>
    /// <returns>
    ///     <see langword="true"/> if the process group was stopped and has since resumed. <see langword="false"/> if
    ///     the platform has no suspend signal, or if <c>killpg</c> failed.
    /// </returns>
    /// <remarks>
    ///     A <see langword="false"/> return means the process was never stopped. Callers that tore down terminal state
    ///     before calling this — leaving the alternate buffer, returning to cooked mode — must restore it either way,
    ///     because there is nothing to resume from and the torn-down state would otherwise persist.
    /// </remarks>
    public static bool Suspend () => Suspend (GetSuspendSignal (), killpg);

    /// <summary>Testable core of <see cref="Suspend()"/>, with the syscall supplied by the caller.</summary>
    /// <param name="signal">The signal to send, or -1 when the platform has no suspend signal.</param>
    /// <param name="killProcessGroup">
    ///     The <c>killpg</c> implementation, taking the process group and signal and returning 0 on success. Blocks
    ///     until the process group resumes when it really does stop the process.
    /// </param>
    /// <returns><see langword="true"/> only if <paramref name="killProcessGroup"/> reported success.</returns>
    internal static bool Suspend (int signal, Func<int, int, int> killProcessGroup)
    {
        Logging.Information ($"SuspendHelper.Suspend: signal={signal}");

        if (signal == -1)
        {
            Logging.Warning ("SuspendHelper.Suspend: No suspend signal for this platform");

            return false;
        }

        Logging.Information ($"SuspendHelper.Suspend: Calling killpg(0, {signal}) [SIGTSTP]...");
        int result = killProcessGroup (0, signal);

        if (result != 0)
        {
            // errno is only meaningful for the real P/Invoke, which sets SetLastError.
            Logging.Warning ($"SuspendHelper.Suspend: killpg returned {result} (errno={Marshal.GetLastWin32Error ()}). Process was not stopped.");

            return false;
        }

        Logging.Information ("SuspendHelper.Suspend: killpg succeeded; process group has resumed.");

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

            // Linux, on every architecture .NET targets — including ppc64le, which follows the generic Linux signal
            // numbering even though its ioctl numbering differs. (MIPS uses 24, SPARC and Alpha 18; none is a .NET
            // target, so unlike TIOCGWINSZ this needs no architecture.)
            "LINUX" or "ANDROID" => 20,
            "SOLARIS" or "ILLUMOS" => 24,
            "HAIKU" => 13,
            _ => -1
        };

    [DllImport ("libc", SetLastError = true)]
    private static extern int killpg (int pgrp, int sig);
}
