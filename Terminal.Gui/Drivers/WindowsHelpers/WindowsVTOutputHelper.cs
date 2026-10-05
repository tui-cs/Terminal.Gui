using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Helper class for outputting to the with the Windows Console in Virtual Terminal Sequence (VTS) mode using only
///     low-level Windows APIs.
/// </summary>
/// <remarks>
///     <para>
///         When Virtual Terminal Sequences (VTS) Output mode is enabled via <c>ENABLE_VIRTUAL_TERMINAL_PROCESSING</c>,
///         with WriteFile or WriteConsole, characters are parsed for VT100 and similar control character sequences that
///         control cursor movement, color/font mode, and other operations that can also be performed via the existing
///         Console APIs. Ensure <c>ENABLE_PROCESSED_OUTPUT</c> is set when using this flag
///     </para>
/// </remarks>
internal sealed partial class WindowsVTOutputHelper : IDisposable
{
    #region P/Invoke Declarations

    private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 4;
    private const uint ENABLE_PROCESSED_OUTPUT = 1;

    [DllImport ("kernel32.dll")]
    private static extern bool SetConsoleMode (nint hConsoleHandle, uint dwMode);

    [DllImport ("kernel32.dll")]
    private static extern bool GetConsoleMode (nint hConsoleHandle, out uint lpMode);

    [DllImport ("kernel32.dll")]
    private static extern uint GetLastError ();

    [LibraryImport ("kernel32.dll", SetLastError = true)]
    [return: MarshalAs (UnmanagedType.Bool)]
    private static partial bool WriteFile (nint hConsoleHandle,
                                           ReadOnlySpan<byte> lpBuffer,
                                           uint nNumberOfBytesToWrite,
                                           out uint lpNumberOfBytesWritten,
                                           nint lpOverlapped);

    [DllImport ("kernel32.dll", SetLastError = true)]
    [return: MarshalAs (UnmanagedType.Bool)]
    private static extern bool FlushFileBuffers (nint hFile);

    #endregion

    private uint _originalConsoleMode;
    private bool _disposed;

    internal delegate bool WriteFileDelegate (ReadOnlySpan<byte> bytes, out uint written, out int error);
    private readonly WriteFileDelegate? _writer;

    internal WindowsVTOutputHelper (WriteFileDelegate? writer = null) => _writer = writer;

    /// <summary>
    ///     Gets whether VTS output mode was successfully enabled.
    /// </summary>
    public bool IsEnabled
    {
        get;
        private set;

        //Logging.Trace ($"{value}");
    }

    /// <summary>
    ///     Gets the Windows console output handle.
    /// </summary>
    public nint OutputHandle { get; private set; }

    /// <summary>
    ///     Attempts to enable Windows Virtual Terminal Output mode.
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
            // Use the controlling terminal output handle: when stdout is redirected (e.g.
            // `myapp | jq`) this is the handle obtained from CONOUT$ rather than the standard
            // output handle, so VT sequences still reach the real console.
            OutputHandle = TerminalDevice.OutputHandle;

            if (OutputHandle == nint.Zero || OutputHandle == new nint (-1))
            {
                Logging.Warning ("Failed to get Windows console output handle.");

                return false;
            }

            if (!GetConsoleMode (OutputHandle, out _originalConsoleMode))
            {
                Logging.Warning ("Failed to get Windows console mode.");

                return false;
            }

            uint newMode = _originalConsoleMode;

            if ((newMode & ENABLE_VIRTUAL_TERMINAL_PROCESSING) == 0)
            {
                newMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;

                if (!SetConsoleMode (OutputHandle, newMode))
                {
                    throw new ApplicationException ($"Failed to set output console mode, error code: {GetLastError ()}.");
                }
            }

            if ((newMode & ENABLE_PROCESSED_OUTPUT) == 0)
            {
                newMode |= ENABLE_PROCESSED_OUTPUT;

                if (!SetConsoleMode (OutputHandle, newMode))
                {
                    throw new ApplicationException ($"Failed to set output console mode, error code: {GetLastError ()}.");
                }
            }

            IsEnabled = true;

            //Logging.Information ($"Windows VTS output mode enabled successfully. Mode: 0x{newMode:X} (was 0x{_originalConsoleMode:X})");

            return true;
        }
        catch (Exception ex)
        {
            Logging.Warning ($"Failed to enable Windows VTS mode: {ex.Message}");

            return false;
        }
    }

    /// <summary>
    ///     Restores the console to its original mode.
    /// </summary>
    public void Restore ()
    {
        if (!IsEnabled || _disposed || OutputHandle == nint.Zero)
        {
            return;
        }

        try
        {
            SetConsoleMode (OutputHandle, _originalConsoleMode);
            IsEnabled = false;

            //Logging.Information ("Windows console mode restored.");
        }
        catch (Exception ex)
        {
            Logging.Warning ($"Failed to restore Windows console mode: {ex.Message}");
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

    public void Write (StringBuilder output) => Write (Encoding.UTF8.GetBytes (output.ToString ()));

    /// <summary>Writes UTF-8 directly, retrying short writes without copying their tail.</summary>
    public void Write (ReadOnlySpan<byte> utf8)
    {
        while (!utf8.IsEmpty)
        {
            uint written;
            int error;
            bool succeeded;

            if (_writer is { })
            {
                succeeded = _writer (utf8, out written, out error);
            }
            else
            {
                succeeded = WriteFile (OutputHandle, utf8, (uint)utf8.Length, out written, nint.Zero);
                error = Marshal.GetLastWin32Error ();
            }

            if (!succeeded)
            {
                if (error == 0)
                {
                    throw new IOException ("WriteFile failed without an error code.");
                }

                throw new Win32Exception (error);
            }

            if (written == 0 || written > utf8.Length)
            {
                throw new IOException ($"WriteFile reported {written} of {utf8.Length} bytes.");
            }

            utf8 = utf8 [(int)written..];
        }
    }

    /// <summary>
    ///     Flushes the controlling console output handle via <c>FlushFileBuffers</c>.
    ///     No-op when no terminal output device is available.
    /// </summary>
    public static void FlushStdout ()
    {
        nint h = TerminalDevice.OutputHandle;

        if (h != nint.Zero && h != new nint (-1))
        {
            FlushFileBuffers (h);
        }
    }
}
