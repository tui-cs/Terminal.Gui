using System.Runtime.InteropServices;
using Terminal.Gui.Tracing;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Helper class for getting input from the Windows Console in Virtual Terminal Sequence (VTS) mode using only
///     low-level Windows APIs.
/// </summary>
/// <remarks>
///     <para>
///         When Virtual Terminal Sequences (VTS) Input mode is enabled via <c>ENABLE_VIRTUAL_TERMINAL_INPUT</c>,
///         the Windows Console converts user input (keyboard, mouse) into ANSI escape sequences that
///         can be read via standard input APIs like <c>ReadFile</c> or <c>Console.OpenStandardInput()</c>.
///     </para>
///     <para>
///         This provides a unified, cross-platform ANSI input mechanism where:
///         <list type="bullet">
///             <item>Keyboard input becomes ANSI sequences (e.g., Arrow Up = ESC[A)</item>
///             <item>Mouse input becomes SGR format sequences (e.g., ESC[&lt;0;10;5M)</item>
///             <item>All input can be parsed uniformly with <see cref="AnsiResponseParser"/></item>
///         </list>
///     </para>
/// </remarks>
internal sealed class WindowsVTInputHelper : IDisposable
{
    #region P/Invoke Declarations

    // In ideal world, Windows Console would have a way of setting VTS mode without having to use SetConsoleMode.
    // It would also provide a non-blocking API for reading input bytes directly as ANSI sequences without having
    // to use GetNumberOfConsoleInputEvents to poll for availability. With such APIs, this helper class would be unnecessary.
    // If this were the case, the only API the ANSI driver would require on Windows is GetStdHandle and ReadFile.

    [DllImport ("kernel32.dll")]
    private static extern bool GetConsoleMode (nint hConsoleHandle, out uint lpMode);

    [DllImport ("kernel32.dll")]
    private static extern bool SetConsoleMode (nint hConsoleHandle, uint dwMode);

    // Equivalent of poll() on Unix — needed because ReadFile blocks and the input loop
    // requires a non-blocking availability check for throttling and cancellation.
    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern bool GetNumberOfConsoleInputEvents (nint hConsoleInput, out uint lpcNumberOfEvents);

    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern bool ReadFile (nint hFile, byte [] lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, nint lpOverlapped);

    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern bool CancelIoEx (nint hFile, nint lpOverlapped);

    [DllImport ("kernel32.dll")]
    private static extern uint GetConsoleCP ();

    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleCP (uint wCodePageID);

    #endregion

    private const int ERROR_NOT_FOUND = 1168;
    private const int ERROR_OPERATION_ABORTED = 995;

    // Console mode flags
    private const uint ENABLE_VIRTUAL_TERMINAL_INPUT = 0x0200;
    private const uint ENABLE_PROCESSED_INPUT = 0x0001;
    private const uint ENABLE_LINE_INPUT = 0x0002;
    private const uint ENABLE_ECHO_INPUT = 0x0004;
    private const uint ENABLE_MOUSE_INPUT = 0x0010;
    private const uint ENABLE_QUICK_EDIT_MODE = 0x0040;
    private const uint ENABLE_EXTENDED_FLAGS = 0x0080;

    private uint _originalConsoleMode;
    private bool _disposed;

    private const uint CP_UTF8 = 65001;

    private readonly uint _originalConsoleCP;

    public WindowsVTInputHelper ()
    {
        _originalConsoleCP = GetConsoleCP ();

        if (_originalConsoleCP == CP_UTF8 || SetConsoleCP (CP_UTF8))
        {
            return;
        }
        int error = Marshal.GetLastWin32Error ();
        Logging.Warning ($"{nameof (WindowsVTInputHelper)}: Failed to set console input code page to UTF-8. Win32 error: {error}");
    }

    /// <summary>
    ///     Gets whether VTS input mode was successfully enabled.
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    ///     Gets the Windows console input handle.
    /// </summary>
    public nint InputHandle { get; private set; }

    /// <summary>
    ///     Gets the encoding for the console's input code page.
    ///     On Windows, <c>ReadFile</c> returns bytes in the console's input code page (e.g., Windows-1252),
    ///     not UTF-8. This encoding must be used to correctly decode those bytes.
    /// </summary>
    public Encoding ConsoleInputEncoding
    {
        get
        {
            uint codePage = GetConsoleCP ();

            return Encoding.GetEncoding ((int)codePage);
        }
    }

    /// <summary>
    ///     Attempts to enable Windows Virtual Terminal Input mode.
    /// </summary>
    /// <returns>True if VTS mode was enabled successfully; false otherwise.</returns>
    public bool TryEnable ()
    {
        if (IsEnabled)
        {
            return true;
        }

        // Only attempt on Windows
        if (!OperatingSystem.IsWindows ())
        {
            return false;
        }

        try
        {
            // Use the controlling terminal input handle: when stdin is redirected (e.g. piping
            // input into the app) this is the handle obtained from CONIN$ rather than the
            // standard input handle, so VT input still reaches the real console.
            InputHandle = TerminalDevice.InputHandle;

            if (InputHandle == nint.Zero || InputHandle == new nint (-1))
            {
                Logging.Warning ($"{nameof (WindowsVTInputHelper)}: Failed to get Windows console input handle.");

                return false;
            }

            if (!GetConsoleMode (InputHandle, out _originalConsoleMode))
            {
                Logging.Warning ($"{nameof (WindowsVTInputHelper)}: Failed to get Windows console mode.");

                return false;
            }

            // Configure VTS input mode:
            // - Enable: VTS input, mouse input, extended flags
            // - Disable: processed input, line input, echo, quick edit
            // This allows raw ANSI sequence reading
            uint newMode = _originalConsoleMode;
            newMode |= ENABLE_VIRTUAL_TERMINAL_INPUT | ENABLE_MOUSE_INPUT | ENABLE_EXTENDED_FLAGS;
            newMode &= ~(ENABLE_PROCESSED_INPUT | ENABLE_LINE_INPUT | ENABLE_ECHO_INPUT | ENABLE_QUICK_EDIT_MODE);

            if (!SetConsoleMode (InputHandle, newMode))
            {
                Logging.Warning ($"{nameof (WindowsVTInputHelper)}: Failed to set Windows VTS console mode.");

                return false;
            }

            Encoding.RegisterProvider (CodePagesEncodingProvider.Instance);

            IsEnabled = true;

            Trace.Lifecycle (nameof (WindowsVTInputHelper),
                             "Init",
                             $"Windows VTS input mode enabled successfully. Mode: 0x{newMode:X} (was 0x{_originalConsoleMode:X})");

            return true;
        }
        catch (Exception ex)
        {
            Logging.Warning ($"{nameof (WindowsVTInputHelper)}: Failed to enable Windows VTS mode: {ex.Message}");

            return false;
        }
    }

    /// <summary>
    ///     Reads ANSI input sequences from the console.
    /// </summary>
    /// <param name="buffer">Buffer to read into.</param>
    /// <param name="bytesRead">Number of bytes actually read.</param>
    /// <returns>True if read succeeded; false otherwise.</returns>
    public bool TryRead (byte [] buffer, out int bytesRead)
    {
        bytesRead = 0;

        if (!IsEnabled || InputHandle == nint.Zero)
        {
            return false;
        }

        try
        {
            // Read the VT byte stream via ReadFile. With ENABLE_VIRTUAL_TERMINAL_INPUT
            // enabled, the Windows console converts all input (keyboard, mouse, etc.)
            // into ANSI escape sequences in this stream.
            bool success = ReadFile (InputHandle, buffer, (uint)buffer.Length, out uint numBytesRead, nint.Zero);

            if (!success)
            {
                int error = Marshal.GetLastWin32Error ();

                if (error == ERROR_OPERATION_ABORTED)
                {
                    return false;
                }

                Logging.Warning ($"{nameof (WindowsVTInputHelper)}: ReadFile failed with error code: {error}");

                return false;
            }

            if (numBytesRead == 0)
            {
                // Workaround for Windows bug (since Win8, fix pending in microsoft/terminal#19940):
                // ReadFile unconditionally treats Ctrl+Z as EOF and returns 0 bytes, even when
                // ENABLE_PROCESSED_INPUT is disabled. Since we have a live console handle with
                // processed input disabled, 0-byte success can only mean this bug.
                // Synthesize the 0x1A (SUB) byte that ReadFile should have returned.
                // See https://github.com/microsoft/terminal/issues/4958
                buffer [0] = 0x1A;
                bytesRead = 1;

                return true;
            }

            bytesRead = (int)numBytesRead;

            return true;
        }
        catch (Exception ex)
        {
            Logging.Warning ($"{nameof (WindowsVTInputHelper)}: Error reading Windows console input: {ex.Message}");

            return false;
        }
    }

    /// <summary>
    ///     Restores the console to its original mode.
    /// </summary>
    public void Restore ()
    {
        if (!IsEnabled || _disposed || InputHandle == nint.Zero)
        {
            return;
        }

        try
        {
            SetConsoleCP (_originalConsoleCP);
            SetConsoleMode (InputHandle, _originalConsoleMode);
            IsEnabled = false;
        }
        catch (Exception ex)
        {
            Logging.Warning ($"{nameof (WindowsVTInputHelper)}: Failed to restore Windows console mode: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public void Dispose ()
    {
        if (_disposed)
        {
            return;
        }

        Restore ();
        _disposed = true;
    }

    /// <summary>
    ///     Checks whether input is available without consuming it.
    /// </summary>
    /// <returns><c>true</c> if there is at least one input event available.</returns>
    public bool Peek () => GetNumberOfConsoleInputEvents (InputHandle, out uint count) && count > 0;

    /// <summary>
    ///     Cancels any pending synchronous <see cref="ReadFile"/> on the Windows console input handle.
    /// </summary>
    public static void WakePendingRead ()
    {
        if (!OperatingSystem.IsWindows ())
        {
            return;
        }

        nint inputHandle = TerminalDevice.InputHandle;

        if (inputHandle == nint.Zero || inputHandle == new nint (-1))
        {
            return;
        }

        if (CancelIoEx (inputHandle, nint.Zero))
        {
            return;
        }

        int error = Marshal.GetLastWin32Error ();

        if (error != ERROR_NOT_FOUND)
        {
            Logging.Warning ($"{nameof (WindowsVTInputHelper)}: CancelIoEx failed with error code: {error}");
        }
    }

    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern bool FlushConsoleInputBuffer (nint hConsoleInput);

    /// <summary>
    ///     Flushes all pending input from the console input buffer.
    ///     This prevents protocol-specific sequences (e.g. Kitty keyboard key-release events)
    ///     from leaking into the shell after the app exits.
    /// </summary>
    public void Flush ()
    {
        if (!IsEnabled || _disposed || InputHandle == nint.Zero)
        {
            return;
        }

        FlushConsoleInputBuffer (InputHandle);
    }
}
