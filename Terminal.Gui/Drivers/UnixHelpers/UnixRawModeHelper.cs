using System.Runtime.InteropServices;

// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo
// ReSharper disable InconsistentNaming
// ReSharper disable CommentTypo

namespace Terminal.Gui.Drivers;

/// <summary>
///     Helper class for enabling Unix/Mac terminal raw mode using termios.
/// </summary>
/// <remarks>
///     Raw mode disables:
///     <list type="bullet">
///         <item>Line buffering (ICANON) - characters available immediately</item>
///         <item>Echo (ECHO) - typed characters don't appear on screen</item>
///         <item>Signal generation (ISIG) - Ctrl+C doesn't send SIGINT</item>
///         <item>Special character processing (IEXTEN, IXON, ICRNL, etc.)</item>
///     </list>
///     This allows the application to receive raw keyboard input and process all keys,
///     including control sequences and special keys as ANSI escape sequences.
///     <para>
///         The primary restore path is an explicit call to <see cref="Dispose"/> (or
///         <see cref="Restore"/>). To guarantee the terminal is restored when the process
///         exits without disposing the helper, <see cref="TryEnable"/> also registers an
///         <see cref="AppDomain.ProcessExit"/> hook that calls <see cref="Restore"/>. See
///         issue #5164. A <see cref="Console.CancelKeyPress"/> handler is registered as a
///         best-effort safety net, but disabling ISIG means a keyboard Ctrl+C is delivered
///         as a 0x03 byte rather than SIGINT, so that handler is unlikely to fire while
///         raw mode is active; it still helps for externally-delivered SIGINT/CTRL_C events
///         and for hosts that do not disable ISIG.
///     </para>
/// </remarks>
internal sealed class UnixRawModeHelper : IDisposable
{
    private Termios _originalTermios;
    private bool _haveSavedTermios;
    private bool _disposed;
    private int _termiosFd = -1;
    private EventHandler? _processExitHandler;
    private ConsoleCancelEventHandler? _cancelKeyHandler;

    /// <summary>
    ///     Gets whether raw mode was successfully enabled.
    /// </summary>
    public bool IsRawModeEnabled { get; private set; }

    /// <summary>
    ///     Attempts to enable raw mode on the terminal.
    /// </summary>
    /// <returns>True if raw mode was enabled successfully; false otherwise.</returns>
    public bool TryEnable ()
    {
        if (IsRawModeEnabled)
        {
            return true;
        }

        // Only attempt on Unix-like platforms
        if (OperatingSystem.IsWindows ())
        {
            return false;
        }

        try
        {
            // Use the controlling terminal input fd: when stdin is redirected (e.g. `myapp | jq`)
            // this is /dev/tty rather than STDIN_FILENO, so termios settings still apply to the
            // real terminal device.
            int fd = TerminalDevice.InputFd;

            if (fd < 0)
            {
                Logging.Warning ("No terminal input device available. Cannot enable raw mode.");

                return false;
            }

            _termiosFd = fd;

            // Get current terminal attributes
            int result = tcgetattr (_termiosFd, out _originalTermios);

            if (result != 0)
            {
                int errno = Marshal.GetLastWin32Error ();
                Logging.Warning ($"tcgetattr failed (errno={errno}). Cannot enable raw mode.");

                return false;
            }

            // Mark that _originalTermios contains a valid snapshot. Without this guard,
            // a later Restore() (after a TryEnable failure path or before any successful
            // call) could write uninitialized struct contents back to the terminal.
            _haveSavedTermios = true;

            // Create modified attributes for raw mode
            Termios raw = _originalTermios;

            try
            {
                // Try using cfmakeraw if available (cleaner, platform-specific implementation)
                cfmakeraw_ref (ref raw);
            }
            catch (EntryPointNotFoundException)
            {
                // Manually configure raw mode if cfmakeraw not available
                // This is equivalent to cfmakeraw's behavior
                raw.c_iflag &= ~(BRKINT | ICRNL | INPCK | ISTRIP | IXON);
                raw.c_oflag &= ~OPOST;
                raw.c_cflag |= CS8;
                raw.c_lflag &= ~(ECHO | ICANON | IEXTEN | ISIG);
            }

            // Apply raw mode settings
            result = tcsetattr (_termiosFd, TCSANOW, ref raw);

            if (result != 0)
            {
                int errno = Marshal.GetLastWin32Error ();
                Logging.Warning ($"tcsetattr failed (errno={errno}). Cannot enable raw mode.");

                return false;
            }

            IsRawModeEnabled = true;

            // Wire safety nets so the terminal is restored even if the input thread
            // crashes or the process exits without going through Dispose().
            HookProcessExit ();

            Logging.Information ("Unix raw mode enabled successfully.");

            return true;
        }
        catch (DllNotFoundException)
        {
            // libc not available - expected on non-Unix platforms
            return false;
        }
        catch (Exception ex)
        {
            Logging.Warning ($"Failed to enable Unix raw mode: {ex.Message}");

            return false;
        }
    }

    /// <summary>
    ///     Restores the terminal to its original state.
    /// </summary>
    /// <remarks>
    ///     A no-op if raw mode was never successfully enabled, if the original
    ///     <c>termios</c> snapshot was never captured, or if the helper has already
    ///     been disposed. This guard prevents writing an uninitialized
    ///     <c>termios</c> struct back to the terminal after a failed
    ///     <see cref="TryEnable"/>.
    /// </remarks>
    public void Restore ()
    {
        if (_disposed || !_haveSavedTermios || !IsRawModeEnabled)
        {
            return;
        }

        try
        {
            int result = tcsetattr (_termiosFd, TCSANOW, ref _originalTermios);

            if (result != 0)
            {
                int errno = Marshal.GetLastWin32Error ();
                Logging.Warning ($"tcsetattr failed during restore (errno={errno}). Terminal may still be in raw mode.");

                // Leave IsRawModeEnabled set to true so callers know the terminal was not
                // successfully restored.
                return;
            }

            IsRawModeEnabled = false;
            Logging.Information ("Unix terminal settings restored.");
        }
        catch (Exception ex)
        {
            Logging.Warning ($"Failed to restore Unix terminal settings: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public void Dispose ()
    {
        if (_disposed)
        {
            return;
        }

        UnhookProcessExit ();
        Restore ();
        _disposed = true;
    }

    private void HookProcessExit ()
    {
        if (_processExitHandler is not null)
        {
            return;
        }

        _processExitHandler = (_, _) => Restore ();
        AppDomain.CurrentDomain.ProcessExit += _processExitHandler;

        try
        {
            // Belt-and-braces: most Unix raw-mode entry disables ISIG, so Ctrl+C produces a
            // literal 0x03 byte on stdin rather than a SIGINT, meaning this handler is unlikely
            // to fire from a keyboard Ctrl+C while raw mode is active. It still helps for
            // externally-delivered SIGINT/CTRL_C events and for hosts that do not disable ISIG.
            // The primary safety net is the AppDomain.ProcessExit hook.
            _cancelKeyHandler = (_, _) => Restore ();
            Console.CancelKeyPress += _cancelKeyHandler;
        }
        catch (Exception ex)
        {
            // Console.CancelKeyPress may throw on some hosts (e.g. when stdin is
            // redirected in unusual ways). The ProcessExit hook is still in place.
            Logging.Warning ($"Could not hook Console.CancelKeyPress: {ex.Message}");
            _cancelKeyHandler = null;
        }
    }

    private void UnhookProcessExit ()
    {
        if (_processExitHandler is not null)
        {
            try
            {
                AppDomain.CurrentDomain.ProcessExit -= _processExitHandler;
            }
            catch
            {
                // Ignore: nothing useful we can do here.
            }

            _processExitHandler = null;
        }

        if (_cancelKeyHandler is not null)
        {
            try
            {
                Console.CancelKeyPress -= _cancelKeyHandler;
            }
            catch
            {
                // Ignore: nothing useful we can do here.
            }

            _cancelKeyHandler = null;
        }
    }

    #region P/Invoke Declarations

    [StructLayout (LayoutKind.Sequential)]
    private struct Termios
    {
        public uint c_iflag;
        public uint c_oflag;
        public uint c_cflag;
        public uint c_lflag;

        [MarshalAs (UnmanagedType.ByValArray, SizeConst = 32)]
        public byte [] c_cc;

        public uint c_ispeed;
        public uint c_ospeed;
    }

    // Terminal attribute flags
    private const int STDIN_FILENO = 0;
    private const int TCSANOW = 0;

    // Input flags
    private const uint BRKINT = 0x00000002; // Signal on break
    private const uint ICRNL = 0x00000100; // Map CR to NL
    private const uint INPCK = 0x00000010; // Enable parity checking
    private const uint ISTRIP = 0x00000020; // Strip 8th bit
    private const uint IXON = 0x00000400; // Enable XON/XOFF flow control

    // Output flags
    private const uint OPOST = 0x00000001; // Post-process output

    // Control flags
    private const uint CS8 = 0x00000030; // 8-bit characters

    // Local flags
    private const uint ECHO = 0x00000008; // Echo input
    private const uint ICANON = 0x00000100; // Canonical mode (line buffering)
    private const uint IEXTEN = 0x00008000; // Extended input processing
    private const uint ISIG = 0x00000001; // Generate signals

    [DllImport ("libc", SetLastError = true)]
    private static extern int tcgetattr (int fd, out Termios termios);

    [DllImport ("libc", SetLastError = true)]
    private static extern int tcsetattr (int fd, int optional_actions, ref Termios termios);

    [DllImport ("libc", EntryPoint = "cfmakeraw", SetLastError = false)]
    private static extern void cfmakeraw_ref (ref Termios termios);

    #endregion
}
