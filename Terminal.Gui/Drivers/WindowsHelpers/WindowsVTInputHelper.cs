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
///         can be read via standard input APIs like <c>ReadFile</c> or <c>ReadConsoleW</c>.
///     </para>
///     <para>
///         Input is read as UTF-16 with <c>ReadConsoleW</c>. <c>ReadFile</c> asks the console host to convert input
///         to the input code page, and that conversion turns each half of a surrogate pair (e.g. emoji) into
///         U+FFFD even when the code page is UTF-8.
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
    // If this were the case, the only API the ANSI driver would require on Windows is GetStdHandle and ReadConsoleW.

    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleMode (nint hConsoleHandle, out uint lpMode);

    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleMode (nint hConsoleHandle, uint dwMode);

    // Equivalent of poll() on Unix — needed because ReadConsoleW blocks and the input loop
    // requires a non-blocking availability check for throttling and cancellation.
    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern bool GetNumberOfConsoleInputEvents (nint hConsoleInput, out uint lpcNumberOfEvents);

    [DllImport ("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool ReadConsoleW (nint hConsoleInput,
                                             [Out] char [] lpBuffer,
                                             uint nNumberOfCharsToRead,
                                             out uint lpNumberOfCharsRead,
                                             nint pInputControl);

    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern bool CancelIoEx (nint hFile, nint lpOverlapped);

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

    /// <summary>
    ///     Gets whether VTS input mode was successfully enabled.
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    ///     Gets the Windows console input handle.
    /// </summary>
    public nint InputHandle { get; private set; }

    /// <summary>
    ///     Attempts to enable Windows Virtual Terminal Input mode.
    /// </summary>
    /// <param name="isMouseDisabled">Enables native selection instead of console mouse input.</param>
    /// <returns>True if VTS mode was enabled successfully; false otherwise.</returns>
    public bool TryEnable (bool isMouseDisabled = false)
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

            // Keep raw keyboard input while allowing native selection when mouse reporting is disabled.
            uint newMode = _originalConsoleMode | ENABLE_VIRTUAL_TERMINAL_INPUT;
            newMode &= ~(ENABLE_PROCESSED_INPUT | ENABLE_LINE_INPUT | ENABLE_ECHO_INPUT);
            newMode = GetMouseMode (newMode, isMouseDisabled);

            if (!SetConsoleMode (InputHandle, newMode))
            {
                Logging.Warning ($"{nameof (WindowsVTInputHelper)}: Failed to set Windows VTS console mode.");

                return false;
            }

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

    internal static uint GetMouseMode (uint mode, bool disabled) =>
        disabled
            ? (mode | ENABLE_EXTENDED_FLAGS | ENABLE_QUICK_EDIT_MODE) & ~ENABLE_MOUSE_INPUT
            : (mode | ENABLE_EXTENDED_FLAGS | ENABLE_MOUSE_INPUT) & ~ENABLE_QUICK_EDIT_MODE;

    internal void SetMouseDisabled (bool disabled)
    {
        if (!IsEnabled || _disposed)
        {
            return;
        }

        if (!GetConsoleMode (InputHandle, out uint mode) || !SetConsoleMode (InputHandle, GetMouseMode (mode, disabled)))
        {
            throw new System.ComponentModel.Win32Exception (Marshal.GetLastWin32Error (), "Failed to change Windows console mouse mode.");
        }
    }

    /// <summary>
    ///     Reads ANSI input sequences from the console.
    /// </summary>
    /// <param name="buffer">Buffer to read UTF-16 code units into.</param>
    /// <param name="charsRead">Number of UTF-16 code units actually read.</param>
    /// <returns>True if at least one code unit was read; false if the read failed, was cancelled, or returned no input.</returns>
    /// <remarks>
    ///     A surrogate pair may be split across two reads; callers must treat the result as a stream of code units.
    /// </remarks>
    public bool TryRead (char [] buffer, out int charsRead)
    {
        charsRead = 0;

        if (!IsEnabled || InputHandle == nint.Zero)
        {
            return false;
        }

        try
        {
            // Read the VT stream as UTF-16 via ReadConsoleW. With ENABLE_VIRTUAL_TERMINAL_INPUT
            // enabled, the Windows console converts all input (keyboard, mouse, etc.)
            // into ANSI escape sequences in this stream. ReadFile is not used because the console
            // host's code page conversion replaces non-BMP characters (surrogate pairs) with U+FFFD.
            bool success = ReadConsoleW (InputHandle, buffer, (uint)buffer.Length, out uint numCharsRead, nint.Zero);

            if (!success)
            {
                int error = Marshal.GetLastWin32Error ();

                if (error == ERROR_OPERATION_ABORTED)
                {
                    return false;
                }

                Logging.Warning ($"{nameof (WindowsVTInputHelper)}: ReadConsoleW failed with error code: {error}");

                return false;
            }

            // Zero characters means the read was cancelled by WakePendingRead (CancelIoEx can complete
            // ReadConsoleW successfully with no data). Unlike ReadFile, ReadConsoleW never converts Ctrl+Z
            // into a zero-length EOF read: the console host only applies that to raw (ReadFile) reads, so
            // Ctrl+Z arrives as 0x1A. See https://github.com/microsoft/terminal/issues/4958
            if (numCharsRead == 0)
            {
                return false;
            }

            charsRead = (int)numCharsRead;

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
    ///     Cancels any pending synchronous <see cref="ReadConsoleW"/> on the Windows console input handle.
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
