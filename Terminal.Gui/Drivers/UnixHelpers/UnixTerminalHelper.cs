using System.Diagnostics;
using System.Runtime.InteropServices;
using Trace = Terminal.Gui.Tracing.Trace;

namespace Terminal.Gui.Drivers;

internal static class UnixTerminalHelper
{
    // Use a raw buffer for termios save/restore since the struct layout differs between
    // macOS (72 bytes: ulong flags, NCCS=20, ulong speeds) and Linux (56 bytes: uint flags, NCCS=32, uint speeds).
    // We never interpret individual fields; we just save and restore the entire blob.
    private const int TermiosBufferSize = 128;
    private static nint _origTermios;
    private static bool _savedTermios;
    private static int _ttyFd = -1;

    public static void SaveTerminalState ()
    {
        if (_ttyFd == -1)
        {
            _ttyFd = open ("/dev/tty", O_RDWR);
        }

        if (_origTermios == nint.Zero)
        {
            _origTermios = Marshal.AllocHGlobal (TermiosBufferSize);
        }

        if (_ttyFd != -1 && tcgetattr (_ttyFd, _origTermios) == 0)
        {
            _savedTermios = true;
        }
        else
        {
            try
            {
                _ = close (_ttyFd);
            }
            catch
            {
                // Ignore any exceptions during close, as we're already in a cleanup phase
            }

            _ttyFd = -1;
        }
    }

    public static void RestoreTerminalState ()
    {
        if (_ttyFd != -1)
        {
            if (_savedTermios && _origTermios != nint.Zero)
            {
                if (tcsetattr (_ttyFd, TCSANOW, _origTermios) != 0)
                {
                    // fallback to stty sane
                    RunSttySane ();
                }
            }
            else
            {
                // fallback to stty sane
                RunSttySane ();
            }

            // close the fd we opened earlier
            try
            {
                _ = close (_ttyFd);
            }
            catch
            {
                // Ignore any exceptions during close, as we're already in a cleanup phase
            }
            _ttyFd = -1;
            _savedTermios = false;
        }
        else
        {
            // fallback to stty sane
            RunSttySane ();
        }
    }

    private static void RunSttySane ()
    {
        try
        {
            var psi = new ProcessStartInfo ("/bin/sh", "-c \"stty sane < /dev/tty\"")
            {
                RedirectStandardOutput = false, RedirectStandardError = false, UseShellExecute = false, CreateNoWindow = true
            };
            Process.Start (psi)?.WaitForExit ();
        }
        catch
        {
            // Ignore any exceptions, as this is a best-effort attempt to restore terminal state
        }
    }

    public static void Suspend (IOutput output)
    {
        if (OperatingSystem.IsWindows ())
        {
            return;
        }

        try
        {
            // Disable bracketed paste and mouse events while suspended.
            output.Write (EscSeqUtils.CSI_DisableBracketedPaste);
            output.Write (EscSeqUtils.CSI_DisableMouseEvents);

            // Check if we have a real console first
            if (!Driver.IsAttachedToTerminal (out bool inputAttached, out bool outputAttached))
            {
                Trace.Lifecycle (nameof (UnixTerminalHelper),
                                 "Suspend",
                                 $"Console redirected (Output: {outputAttached}, Input: {inputAttached}). Running in degraded mode.");

                return;
            }

            // Save the current terminal state (raw mode) so we can restore it after resume.
            SaveTerminalState ();

            // Leave the alternate screen buffer and show cursor.
            output.Write (EscSeqUtils.CSI_RestoreCursorAndRestoreAltBufferWithBackscroll);
            output.Write (EscSeqUtils.CSI_ShowCursor);

            // Restore terminal to cooked mode so the shell can function while we're stopped.
            // The raw mode state was saved above and will be restored after resume.
            RunSttySane ();

            Logging.Information ("UnixTerminalHelper.Suspend: Terminal restored to cooked mode, calling SuspendHelper.Suspend...");

            if (!SuspendHelper.Suspend ())
            {
                Logging.Warning ("UnixTerminalHelper.Suspend: SuspendHelper.Suspend() returned false");

                return;
            }

            Logging.Information ("UnixTerminalHelper.Suspend: Resumed from suspend! Restoring raw mode...");

            // Restore the saved raw mode terminal state.
            RestoreTerminalState ();

            // Re-enter the alternate screen buffer.
            output.Write (EscSeqUtils.CSI_SaveCursorAndActivateAltBufferNoBackscroll);
        }
        catch (Exception ex)
        {
            Trace.Lifecycle (nameof (UnixTerminalHelper), "Suspend", $"Error suspending terminal: {ex.Message}");
        }
        finally
        {
            // Re-enable mouse events and bracketed paste after resume.
            output.Write (EscSeqUtils.CSI_EnableMouseEvents);
            output.Write (EscSeqUtils.CSI_EnableBracketedPaste);
        }
    }

    // P/Invoke and types
    // ReSharper disable IdentifierTypo
    private const int O_RDWR = 2;
    private const int TCSANOW = 0;

    [DllImport ("libc", SetLastError = true)]
    private static extern int open (string path, int oflag);

    [DllImport ("libc", SetLastError = true)]
    private static extern int close (int fd);

    [DllImport ("libc", SetLastError = true)]
    private static extern int tcgetattr (int fd, nint termios_p);

    [DllImport ("libc", SetLastError = true)]
    private static extern int tcsetattr (int fd, int optional_actions, nint termios_p);
}
