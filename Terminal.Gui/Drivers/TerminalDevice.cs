using System.Runtime.InteropServices;

// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo
// ReSharper disable InconsistentNaming

namespace Terminal.Gui.Drivers;

/// <summary>
///     Resolves the controlling terminal device for input and output, preferring the standard
///     streams (stdin/stdout) when they are connected to a terminal, and falling back to the
///     controlling TTY (<c>/dev/tty</c> on Unix, <c>CONIN$</c>/<c>CONOUT$</c> on Windows) when
///     either stream is redirected.
/// </summary>
/// <remarks>
///     <para>
///         Tools such as <c>fzf</c>, <c>gum</c>, and <c>dialog</c> use this technique so a TUI
///         can still render and read input even when the application's stdout or stdin participates
///         in a shell pipeline (e.g. <c>result=$(myapp)</c> or <c>myapp | jq</c>).
///     </para>
///     <para>
///         All members are lazily initialized and cached for the lifetime of the process.
///     </para>
/// </remarks>
internal static class TerminalDevice
{
    private static readonly Lock _lock = new ();
    private static bool _initialized;

    // Unix-side resolved file descriptors. -1 means "no terminal device available".
    private static int _inputFd = -1;
    private static int _outputFd = -1;

    // Did we open /dev/tty ourselves (so we should close it on dispose)?
    private static int _ownedInputFd = -1;
    private static int _ownedOutputFd = -1;

    // Windows-side resolved handles. nint.Zero means "no terminal device available".
    private static nint _inputHandle = nint.Zero;
    private static nint _outputHandle = nint.Zero;

    // Did we open CONIN$/CONOUT$ ourselves (so we should close them on dispose)?
    private static nint _ownedInputHandle = nint.Zero;
    private static nint _ownedOutputHandle = nint.Zero;

    private static bool _inputAttached;
    private static bool _outputAttached;

    /// <summary>
    ///     Gets a Unix file descriptor that can be used to read terminal input.
    ///     Returns <see cref="UnixIOHelper.STDIN_FILENO"/> when stdin is a tty, the fd of an
    ///     opened <c>/dev/tty</c> when stdin is redirected but a controlling terminal exists,
    ///     or <c>-1</c> when no terminal device is available.
    /// </summary>
    public static int InputFd
    {
        get
        {
            EnsureInitialized ();

            return _inputFd;
        }
    }

    /// <summary>
    ///     Gets a Unix file descriptor that can be used to write terminal output.
    ///     Returns <see cref="UnixIOHelper.STDOUT_FILENO"/> when stdout is a tty, the fd of an
    ///     opened <c>/dev/tty</c> when stdout is redirected but a controlling terminal exists,
    ///     or <c>-1</c> when no terminal device is available.
    /// </summary>
    public static int OutputFd
    {
        get
        {
            EnsureInitialized ();

            return _outputFd;
        }
    }

    /// <summary>
    ///     Gets a Windows handle that can be used to read terminal input. Returns the standard
    ///     input handle when stdin is a console, a handle opened via <c>CONIN$</c> when stdin is
    ///     redirected but a console exists, or <see cref="nint.Zero"/> when no console is
    ///     available.
    /// </summary>
    public static nint InputHandle
    {
        get
        {
            EnsureInitialized ();

            return _inputHandle;
        }
    }

    /// <summary>
    ///     Gets a Windows handle that can be used to write terminal output. Returns the standard
    ///     output handle when stdout is a console, a handle opened via <c>CONOUT$</c> when stdout
    ///     is redirected but a console exists, or <see cref="nint.Zero"/> when no console is
    ///     available.
    /// </summary>
    public static nint OutputHandle
    {
        get
        {
            EnsureInitialized ();

            return _outputHandle;
        }
    }

    /// <summary>
    ///     Gets whether a terminal input device (the standard input or <c>/dev/tty</c>/<c>CONIN$</c>)
    ///     is available.
    /// </summary>
    public static bool IsInputAttached
    {
        get
        {
            EnsureInitialized ();

            return _inputAttached;
        }
    }

    /// <summary>
    ///     Gets whether a terminal output device (the standard output or <c>/dev/tty</c>/<c>CONOUT$</c>)
    ///     is available.
    /// </summary>
    public static bool IsOutputAttached
    {
        get
        {
            EnsureInitialized ();

            return _outputAttached;
        }
    }

    /// <summary>
    ///     Resets the cached terminal device state so the next access re-resolves it.
    ///     Intended for testing.
    /// </summary>
    internal static void ResetForTesting ()
    {
        lock (_lock)
        {
            CloseOwnedHandles ();

            _initialized = false;
            _inputFd = -1;
            _outputFd = -1;
            _ownedInputFd = -1;
            _ownedOutputFd = -1;
            _inputHandle = nint.Zero;
            _outputHandle = nint.Zero;
            _ownedInputHandle = nint.Zero;
            _ownedOutputHandle = nint.Zero;
            _inputAttached = false;
            _outputAttached = false;
        }
    }

    private static void EnsureInitialized ()
    {
        if (_initialized)
        {
            return;
        }

        lock (_lock)
        {
            if (_initialized)
            {
                return;
            }

            // When the test harness sets DisableRealDriverIO, skip real terminal detection entirely.
            if (string.Equals (Environment.GetEnvironmentVariable ("DisableRealDriverIO"), "1", StringComparison.Ordinal))
            {
                _initialized = true;

                return;
            }

            try
            {
                if (OperatingSystem.IsWindows ())
                {
                    InitializeWindows ();
                }
                else
                {
                    InitializeUnix ();
                }
            }
            catch
            {
                // Best effort: any failure leaves us in the "no terminal" state.
            }

            _initialized = true;

            // Make sure any owned descriptors get closed when the process exits.
            try
            {
                AppDomain.CurrentDomain.ProcessExit += (_, _) => CloseOwnedHandles ();
            }
            catch
            {
                // ignore
            }
        }
    }

    private static void InitializeUnix ()
    {
        // Prefer the standard streams when they are connected to a terminal.
        if (UnixIOHelper.IsTerminal (UnixIOHelper.STDIN_FILENO))
        {
            _inputFd = UnixIOHelper.STDIN_FILENO;
            _inputAttached = true;
        }

        if (UnixIOHelper.IsTerminal (UnixIOHelper.STDOUT_FILENO))
        {
            _outputFd = UnixIOHelper.STDOUT_FILENO;
            _outputAttached = true;
        }

        // If either side is missing, try to open the controlling terminal directly.
        if (_inputFd != -1 && _outputFd != -1)
        {
            return;
        }

        try
        {
            int ttyFd = open ("/dev/tty", O_RDWR | O_NOCTTY);

            if (ttyFd < 0)
            {
                return;
            }

            // Verify it is actually a terminal.
            if (!UnixIOHelper.IsTerminal (ttyFd))
            {
                _ = close (ttyFd);

                return;
            }

            // Since we reached this point, at least one of (_inputFd, _outputFd) is -1 (the
            // early return above would have fired otherwise), guaranteeing that ttyFd is
            // assigned below.
            if (_inputFd == -1)
            {
                // stdin was redirected: claim the /dev/tty fd we just opened for input.
                _inputFd = ttyFd;
                _ownedInputFd = ttyFd;
                _inputAttached = true;
            }

            if (_outputFd == -1)
            {
                if (_ownedInputFd == ttyFd)
                {
                    // We opened /dev/tty for input above; reuse the same fd for write — it was
                    // opened O_RDWR and we avoid burning a second fd on the same device.
                    _outputFd = ttyFd;
                    _outputAttached = true;
                }
                else
                {
                    // stdin was already a real terminal, so ttyFd was not claimed for input.
                    // Claim it for output now (this is the `myapp | jq` case).
                    _outputFd = ttyFd;
                    _ownedOutputFd = ttyFd;
                    _outputAttached = true;
                }
            }
        }
        catch (DllNotFoundException)
        {
            // libc not available; nothing more we can do.
        }
    }

    private static void InitializeWindows ()
    {
        nint stdIn = GetStdHandle (STD_INPUT_HANDLE);
        nint stdOut = GetStdHandle (STD_OUTPUT_HANDLE);

        if (stdIn != nint.Zero && stdIn != new nint (-1) && GetConsoleMode (stdIn, out _))
        {
            _inputHandle = stdIn;
            _inputAttached = true;
        }

        if (stdOut != nint.Zero && stdOut != new nint (-1) && GetConsoleMode (stdOut, out _))
        {
            _outputHandle = stdOut;
            _outputAttached = true;
        }

        if (_inputHandle != nint.Zero && _outputHandle != nint.Zero)
        {
            return;
        }

        // Fall back to opening the console directly.
        if (_inputHandle == nint.Zero)
        {
            nint h = CreateFile (
                                 "CONIN$",
                                 GENERIC_READ | GENERIC_WRITE,
                                 FILE_SHARE_READ | FILE_SHARE_WRITE,
                                 nint.Zero,
                                 OPEN_EXISTING,
                                 0,
                                 nint.Zero);

            if (h != new nint (-1) && h != nint.Zero && GetConsoleMode (h, out _))
            {
                _inputHandle = h;
                _ownedInputHandle = h;
                _inputAttached = true;
            }
            else if (h != new nint (-1) && h != nint.Zero)
            {
                _ = CloseHandle (h);
            }
        }

        if (_outputHandle == nint.Zero)
        {
            nint h = CreateFile (
                                 "CONOUT$",
                                 GENERIC_READ | GENERIC_WRITE,
                                 FILE_SHARE_READ | FILE_SHARE_WRITE,
                                 nint.Zero,
                                 OPEN_EXISTING,
                                 0,
                                 nint.Zero);

            if (h != new nint (-1) && h != nint.Zero && GetConsoleMode (h, out _))
            {
                _outputHandle = h;
                _ownedOutputHandle = h;
                _outputAttached = true;
            }
            else if (h != new nint (-1) && h != nint.Zero)
            {
                _ = CloseHandle (h);
            }
        }
    }

    private static void CloseOwnedHandles ()
    {
        if (_ownedInputFd != -1)
        {
            try
            {
                _ = close (_ownedInputFd);
            }
            catch
            {
                // ignore
            }

            _ownedInputFd = -1;
        }

        // Guard against double-closing: when stdin and stdout both fall back to /dev/tty we
        // share a single fd between them, so it is recorded as the owned fd for both ends.
        // _ownedInputFd was already closed above (and reset to -1 there), so we only need to
        // close _ownedOutputFd when it refers to a distinct descriptor.
        if (_ownedOutputFd != -1 && _ownedOutputFd != _ownedInputFd)
        {
            try
            {
                _ = close (_ownedOutputFd);
            }
            catch
            {
                // ignore
            }

            _ownedOutputFd = -1;
        }

        if (_ownedInputHandle != nint.Zero)
        {
            try
            {
                _ = CloseHandle (_ownedInputHandle);
            }
            catch
            {
                // ignore
            }

            _ownedInputHandle = nint.Zero;
        }

        if (_ownedOutputHandle != nint.Zero)
        {
            try
            {
                _ = CloseHandle (_ownedOutputHandle);
            }
            catch
            {
                // ignore
            }

            _ownedOutputHandle = nint.Zero;
        }
    }

    #region P/Invoke (Unix)

    private const int O_RDWR = 2;
    private const int O_NOCTTY = 0x100;

    [DllImport ("libc", SetLastError = true)]
    private static extern int open (string path, int oflag);

    [DllImport ("libc", SetLastError = true)]
    private static extern int close (int fd);

    #endregion

    #region P/Invoke (Windows)

    private const int STD_INPUT_HANDLE = -10;
    private const int STD_OUTPUT_HANDLE = -11;
    private const uint GENERIC_READ = 0x80000000;
    private const uint GENERIC_WRITE = 0x40000000;
    private const uint FILE_SHARE_READ = 0x00000001;
    private const uint FILE_SHARE_WRITE = 0x00000002;
    private const uint OPEN_EXISTING = 3;

    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern nint GetStdHandle (int nStdHandle);

    [DllImport ("kernel32.dll")]
    private static extern bool GetConsoleMode (nint hConsoleHandle, out uint lpMode);

    [DllImport ("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern nint CreateFile (
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        nint lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        nint hTemplateFile);

    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle (nint hObject);

    #endregion
}
