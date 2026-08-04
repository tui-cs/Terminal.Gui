using System;
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

        if (OperatingSystem.IsMacOS () ||
            OperatingSystem.IsMacCatalyst () ||
            OperatingSystem.IsIOS () ||
            OperatingSystem.IsTvOS () ||
            OperatingSystem.IsWatchOS () ||
            OperatingSystem.IsFreeBSD () ||
            RuntimeInformation.IsOSPlatform (OSPlatform.Create ("NETBSD")) ||
            RuntimeInformation.IsOSPlatform (OSPlatform.Create ("OPENBSD")))
        {
            _suspendSignal = 18;
        }
        else if (OperatingSystem.IsLinux () ||
                 OperatingSystem.IsAndroid ())
        {
            _suspendSignal = 20;
        }
        else if (RuntimeInformation.IsOSPlatform (OSPlatform.Create ("SOLARIS")) ||
                 RuntimeInformation.IsOSPlatform (OSPlatform.Create ("ILLUMOS")))
        {
            _suspendSignal = 24;
        }
        else if (RuntimeInformation.IsOSPlatform (OSPlatform.Create ("HAIKU")))
        {
            _suspendSignal = 13;
        }
        else
        {
            _suspendSignal = -1;
        }

        return _suspendSignal;
    }

    [DllImport ("libc", SetLastError = true)]
    private static extern int killpg (int pgrp, int sig);
}
