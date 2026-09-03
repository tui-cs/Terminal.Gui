using System.ComponentModel;
using System.Runtime.InteropServices;
using Terminal.Gui.Tracing;

namespace Terminal.Gui.Drivers;

internal partial class WindowsOutput : OutputBase, IOutput
{
    [LibraryImport ("kernel32.dll", EntryPoint = "WriteConsoleW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs (UnmanagedType.Bool)]
    private static partial bool WriteConsole (nint hConsoleOutput,
                                              ReadOnlySpan<char> lpBuffer,
                                              uint numberOfCharsToWrite,
                                              out uint lpNumberOfCharsWritten,
                                              nint lpReserved);

    [LibraryImport ("kernel32.dll", SetLastError = true)]
    private static partial nint GetStdHandle (int nStdHandle);

    [LibraryImport ("kernel32.dll", SetLastError = true)]
    [return: MarshalAs (UnmanagedType.Bool)]
    private static partial bool CloseHandle (nint handle);

    [LibraryImport ("kernel32.dll", SetLastError = true)]
    private static partial nint CreateConsoleScreenBuffer (DesiredAccess dwDesiredAccess,
                                                           ShareMode dwShareMode,
                                                           nint securityAttributes,
                                                           uint flags,
                                                           nint screenBufferData);

    [DllImport ("kernel32.dll", SetLastError = true)]
    [return: MarshalAs (UnmanagedType.Bool)]
    private static extern bool GetConsoleScreenBufferInfoEx (nint hConsoleOutput, ref WindowsConsole.CONSOLE_SCREEN_BUFFER_INFOEX csbi);

    [Flags]
    private enum ShareMode : uint
    {
        FileShareRead = 1,
        FileShareWrite = 2
    }

    [Flags]
    private enum DesiredAccess : uint
    {
        GenericRead = 2147483648,
        GenericWrite = 1073741824
    }

    internal static nint INVALID_HANDLE_VALUE = new (-1);

    [LibraryImport ("kernel32.dll", SetLastError = true)]
    [return: MarshalAs (UnmanagedType.Bool)]
    private static partial bool SetConsoleActiveScreenBuffer (nint handle);

    [LibraryImport ("kernel32.dll")]
    [return: MarshalAs (UnmanagedType.Bool)]
    private static partial bool SetConsoleCursorPosition (nint hConsoleOutput, WindowsConsole.Coord dwCursorPosition);

    [DllImport ("kernel32.dll", SetLastError = true)]
    [return: MarshalAs (UnmanagedType.Bool)]
    private static extern bool SetConsoleCursorInfo (nint hConsoleOutput, [In] ref WindowsConsole.ConsoleCursorInfo lpConsoleCursorInfo);

    [LibraryImport ("kernel32.dll", SetLastError = true)]
    [return: MarshalAs (UnmanagedType.Bool)]
    public static partial bool SetConsoleTextAttribute (nint hConsoleOutput, ushort wAttributes);

    [LibraryImport ("kernel32.dll")]
    [return: MarshalAs (UnmanagedType.Bool)]
    private static partial bool GetConsoleMode (nint hConsoleHandle, out uint lpMode);

    [LibraryImport ("kernel32.dll")]
    [return: MarshalAs (UnmanagedType.Bool)]
    private static partial bool SetConsoleMode (nint hConsoleHandle, uint dwMode);

    [LibraryImport ("kernel32.dll", SetLastError = true)]
    private static partial WindowsConsole.Coord GetLargestConsoleWindowSize (nint hConsoleOutput);

    [DllImport ("kernel32.dll", SetLastError = true)]
    [return: MarshalAs (UnmanagedType.Bool)]
    private static extern bool SetConsoleScreenBufferInfoEx (nint hConsoleOutput, ref WindowsConsole.CONSOLE_SCREEN_BUFFER_INFOEX consoleScreenBufferInfo);

    [DllImport ("kernel32.dll", SetLastError = true)]
    [return: MarshalAs (UnmanagedType.Bool)]
    private static extern bool SetConsoleWindowInfo (nint hConsoleOutput, bool bAbsolute, [In] ref WindowsConsole.SmallRect lpConsoleWindow);

    private const int STD_OUTPUT_HANDLE = -11;
    private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
    private readonly nint _outputHandle;
    private nint _screenBuffer;
    private readonly ConsoleColor _foreground;
    private readonly ConsoleColor _background;

    public WindowsOutput ()
    {
        //Logging.Information ($"Creating {nameof (WindowsOutput)}");

        if (!IsAttachedToTerminal)
        {
            Trace.Lifecycle (nameof (WindowsOutput), "Init", "No real terminal attached. Output operations will be no-op.");

            return;
        }

        if (!OperatingSystem.IsWindows ())
        {
            return;
        }

        // Get the standard output handle which is the current screen buffer.
        _outputHandle = GetStdHandle (STD_OUTPUT_HANDLE);
        GetConsoleMode (_outputHandle, out uint mode);
        IsLegacyConsole = (mode & ENABLE_VIRTUAL_TERMINAL_PROCESSING) == 0;

        if (IsLegacyConsole)
        {
            CreateScreenBuffer ();

            if (!GetConsoleMode (_screenBuffer, out mode))
            {
                throw new ApplicationException ($"Failed to get screenBuffer console mode, error code: {Marshal.GetLastWin32Error ()}.");
            }

#pragma warning disable IDE1006 // Naming Styles
            const uint ENABLE_WRAP_AT_EOL_OUTPUT = 0x0002;
#pragma warning restore IDE1006 // Naming Styles

            mode &= ~ENABLE_WRAP_AT_EOL_OUTPUT; // Disable wrap

            if (!SetConsoleMode (_screenBuffer, mode))
            {
                throw new ApplicationException ($"Failed to set screenBuffer console mode, error code: {Marshal.GetLastWin32Error ()}.");
            }
        }
        else
        {
            if (Environment.GetEnvironmentVariable ("VSAPPIDNAME") is null)
            {
                //Enable alternative screen buffer.
                Console.Out.Write (EscSeqUtils.CSI_SaveCursorAndActivateAltBufferNoBackscroll);
            }
            else
            {
                _foreground = Console.ForegroundColor;
                _background = Console.BackgroundColor;
            }
        }

        GetSize ();
    }

    private Cursor _currentCursor = new ();

    /// <inheritdoc/>
    public Cursor GetCursor () => _currentCursor;

    // <inheritdoc />
    public void SetCursor (Cursor cursor)
    {
        try
        {
            if (IsLegacyConsole)
            {
                WindowsConsole.ConsoleCursorInfo cursorInfo = new ();

                if (!cursor.IsVisible)
                {
                    cursorInfo.bVisible = false;
                    cursorInfo.dwSize = 0;
                }
                else
                {
                    cursorInfo.bVisible = true;

                    cursorInfo.dwSize = cursor.Style switch
                    {
                        CursorStyle.BlinkingBlock => 100,
                        CursorStyle.SteadyBlock => 100,
                        CursorStyle.BlinkingUnderline => 15,
                        CursorStyle.SteadyUnderline => 15,
                        CursorStyle.BlinkingBar => 15,
                        CursorStyle.SteadyBar => 15,
                        _ => 100
                    };
                }

                SetConsoleCursorInfo (!IsLegacyConsole ? _outputHandle : _screenBuffer, ref cursorInfo);
            }
            else
            {
                if (!cursor.IsVisible)
                {
                    Write (EscSeqUtils.CSI_HideCursor);
                }
                else
                {
                    if (_currentCursor.Style != cursor.Style)
                    {
                        Write (EscSeqUtils.CSI_SetCursorStyle (cursor.Style));
                    }

                    Write (EscSeqUtils.CSI_ShowCursor);
                }
            }
        }
        catch
        {
            // Ignore any exceptions
        }
        finally
        {
            SetCursorPositionImpl (cursor.Position?.X ?? 0, cursor.Position?.Y ?? 0);
            _currentCursor = cursor;
        }
    }

    /// <inheritdoc/>
    protected override bool SetCursorPositionImpl (int screenPositionX, int screenPositionY)
    {
        if (Force16Colors && IsLegacyConsole)
        {
            SetConsoleCursorPosition (_screenBuffer, new WindowsConsole.Coord ((short)screenPositionX, (short)screenPositionY));
        }
        else
        {
            var sb = new StringBuilder ();
            EscSeqUtils.CSI_AppendCursorPosition (sb, screenPositionY + 1, screenPositionX + 1);
            Write (sb.ToString ());
        }

        return true;
    }

    private void CreateScreenBuffer ()
    {
        _screenBuffer = CreateConsoleScreenBuffer (DesiredAccess.GenericRead | DesiredAccess.GenericWrite,
                                                   ShareMode.FileShareRead | ShareMode.FileShareWrite,
                                                   nint.Zero,
                                                   1,
                                                   nint.Zero);

        if (_screenBuffer == INVALID_HANDLE_VALUE)
        {
            int err = Marshal.GetLastWin32Error ();

            if (err != 0)
            {
                throw new Win32Exception (err);
            }
        }

        if (!SetConsoleActiveScreenBuffer (_screenBuffer))
        {
            throw new Win32Exception (Marshal.GetLastWin32Error ());
        }
    }

    public void Write (ReadOnlySpan<char> str)
    {
        if (!IsAttachedToTerminal)
        {
            return;
        }

        if (!OperatingSystem.IsWindows ())
        {
            return;
        }

        if (!WriteConsole (!IsLegacyConsole ? _outputHandle : _screenBuffer, str, (uint)str.Length, out uint _, nint.Zero))
        {
            // Don't throw in unit tests
        }
    }

    public Size ResizeBuffer (Size size)
    {
        Size newSize = size;

        try
        {
            newSize = SetConsoleWindow ((short)Math.Max (size.Width, 0), (short)Math.Max (size.Height, 0));
        }
        catch
        {
            // Do nothing; unit tests
        }

        return newSize;
    }

    internal Size SetConsoleWindow (short cols, short rows)
    {
        if (!OperatingSystem.IsWindows () || !IsAttachedToTerminal)
        {
            return new Size (cols, rows);
        }

        var csbi = new WindowsConsole.CONSOLE_SCREEN_BUFFER_INFOEX ();
        csbi.cbSize = (uint)Marshal.SizeOf (csbi);

        if (!GetConsoleScreenBufferInfoEx (!IsLegacyConsole ? _outputHandle : _screenBuffer, ref csbi))
        {
            throw new Win32Exception (Marshal.GetLastWin32Error ());
        }

        // Use the requested size directly. GetLargestConsoleWindowSize can underreport
        // in modern terminals with non-default font sizes, causing the buffer to be
        // clamped smaller than the actual window (visible as a gap at the bottom/right).
        csbi.dwSize = new WindowsConsole.Coord (cols, Math.Max (rows, (short)1));
        csbi.srWindow = new WindowsConsole.SmallRect (0, 0, cols, rows);
        csbi.dwMaximumWindowSize = new WindowsConsole.Coord (cols, rows);

        if (!SetConsoleScreenBufferInfoEx (!IsLegacyConsole ? _outputHandle : _screenBuffer, ref csbi))
        {
            throw new Win32Exception (Marshal.GetLastWin32Error ());
        }

        var winRect = new WindowsConsole.SmallRect (0, 0, (short)(cols - 1), (short)Math.Max (rows - 1, 0));

        if (!SetConsoleWindowInfo (_outputHandle, true, ref winRect))
        {
            Trace.Lifecycle (nameof (WindowsOutput), "SetConsoleWindow", $"Failed to set console window size, error code: {Marshal.GetLastWin32Error ()}. Returning requested size without resizing.");
            return new Size (cols, rows);
        }

        SetConsoleOutputWindow (csbi);

        return new Size (winRect.Right + 1, rows - 1 < 0 ? 0 : winRect.Bottom + 1);
    }

    private void SetConsoleOutputWindow (WindowsConsole.CONSOLE_SCREEN_BUFFER_INFOEX csbi)
    {
        if ((!IsLegacyConsole ? _outputHandle : _screenBuffer) != nint.Zero
            && !SetConsoleScreenBufferInfoEx (!IsLegacyConsole ? _outputHandle : _screenBuffer, ref csbi))
        {
            throw new Win32Exception (Marshal.GetLastWin32Error ());
        }
    }

    public override void Write (IOutputBuffer outputBuffer)
    {
        _everythingStringBuilder.Clear ();

        // for 16 color mode we will write to a backing buffer, then flip it to the active one at the end to avoid jitter.
        _consoleBuffer = 0;

        if (Force16Colors)
        {
            _consoleBuffer = !IsLegacyConsole ? _outputHandle : _screenBuffer;
        }
        else
        {
            _consoleBuffer = _outputHandle;
        }

        try
        {
            base.Write (outputBuffer);

            if (!IsAttachedToTerminal)
            {
                return;
            }

            ReadOnlySpan<char> span = _everythingStringBuilder.ToString ().AsSpan (); // still allocates the string

            bool result = WriteConsole (_consoleBuffer, span, (uint)span.Length, out _, nint.Zero);

            if (result)
            {
                return;
            }
            int err = Marshal.GetLastWin32Error ();

            if (err == 1)
            {
                Logging.Error ($"Error: {Marshal.GetLastWin32Error ()} in {nameof (WindowsOutput)}");

                return;
            }

            if (err != 0)
            {
                throw new Win32Exception (err);
            }
        }
        catch (DllNotFoundException)
        {
            // Running unit tests or in an environment where writing is not possible.
        }
        catch (Exception e)
        {
            Logging.Error ($"Error: {e.Message} in {nameof (WindowsOutput)}");

            if (OperatingSystem.IsWindows ())
            {
                throw;
            }
        }
    }

    /// <inheritdoc/>
    protected override void Write (StringBuilder output)
    {
        if (output.Length == 0)
        {
            return;
        }

        base.Write (output);

        if (!IsAttachedToTerminal)
        {
            return;
        }

        var str = output.ToString ();

        if (Force16Colors && IsLegacyConsole)
        {
            char [] a = str.ToCharArray ();
            WriteConsole (_screenBuffer, a, (uint)a.Length, out _, nint.Zero);
        }
        else
        {
            try
            {
                ReadOnlySpan<char> span = str.AsSpan (); // still allocates the string

                bool result = WriteConsole (_outputHandle, span, (uint)span.Length, out _, nint.Zero);

                if (result)
                {
                    return;
                }
                int err = Marshal.GetLastWin32Error ();

                if (err == 1)
                {
                    Logging.Error ($"Error: {Marshal.GetLastWin32Error ()} in {nameof (WindowsOutput)}");

                    return;
                }

                if (err != 0)
                {
                    throw new Win32Exception (err);
                }
            }
            catch (DllNotFoundException)
            {
                // Running unit tests or in an environment where writing is not possible.
            }
            catch (Exception e)
            {
                Logging.Error ($"Error: {e.Message} in {nameof (WindowsOutput)}");

                if (OperatingSystem.IsWindows ())
                {
                    throw;
                }
            }
        }
    }

    /// <inheritdoc/>
    protected override void AppendOrWriteAttribute (StringBuilder output, Attribute attr, TextStyle redrawTextStyle)
    {
        if (Force16Colors && IsLegacyConsole)
        {
            // Legacy Windows console doesn't support ANSI — use Win32 API directly
            Write (output);
            output.Clear ();
            var as16ColorInt = (ushort)((int)attr.Foreground.GetClosestNamedColor16 () | ((int)attr.Background.GetClosestNamedColor16 () << 4));
            SetConsoleTextAttribute (_screenBuffer, as16ColorInt);
        }
        else
        {
            base.AppendOrWriteAttribute (output, attr, redrawTextStyle);
        }
    }

    private Size? _lastSize;
    private Size? _lastWindowSizeBeforeMaximized;
    private bool _lockResize;

    public Size GetSize ()
    {
        if (_lockResize)
        {
            return _lastSize!.Value;
        }

        Size newSize = GetWindowSize (out _);
        Size largestWindowSize = GetLargestConsoleWindowSize ();

        if (_lastWindowSizeBeforeMaximized is null && newSize == largestWindowSize)
        {
            _lastWindowSizeBeforeMaximized = _lastSize;
        }
        else if (_lastWindowSizeBeforeMaximized is { } && newSize != largestWindowSize)
        {
            if (newSize != _lastWindowSizeBeforeMaximized)
            {
                newSize = _lastWindowSizeBeforeMaximized.Value;
            }

            _lastWindowSizeBeforeMaximized = null;
        }

        if (_lastSize == newSize)
        {
            return newSize;
        }

        // User is resizing the screen, they can only ever resize the active
        // buffer since. We now however have issue because background offscreen
        // buffer will be wrong size, recreate it to ensure it doesn't result in
        // differing active and back buffer sizes (which causes flickering of window size)
        Size? bufSize = null;
        var retries = 0;

        while (bufSize != newSize && retries < 5)
        {
            _lockResize = true;
            bufSize = ResizeBuffer (newSize);
            retries++;
        }

        _lockResize = false;
        _lastSize = newSize;

        return newSize;
    }

    public Size GetWindowSize (out WindowsConsole.Coord cursorPosition)
    {
        if (!IsAttachedToTerminal)
        {
            cursorPosition = default (WindowsConsole.Coord);

            return new Size (80, 25);
        }

        try
        {
            var csbi = new WindowsConsole.CONSOLE_SCREEN_BUFFER_INFOEX ();
            csbi.cbSize = (uint)Marshal.SizeOf (csbi);

            if (!GetConsoleScreenBufferInfoEx (!IsLegacyConsole ? _outputHandle : _screenBuffer, ref csbi))
            {
                //throw new System.ComponentModel.Win32Exception (Marshal.GetLastWin32Error ());
                cursorPosition = default (WindowsConsole.Coord);

                return Size.Empty;
            }

            Size sz = new (csbi.srWindow.Right - csbi.srWindow.Left + 1, csbi.srWindow.Bottom - csbi.srWindow.Top + 1);

            cursorPosition = csbi.dwCursorPosition;

            return sz;
        }
        catch
        {
            cursorPosition = default (WindowsConsole.Coord);
        }

        return new Size (80, 25);
    }

    private Size GetLargestConsoleWindowSize ()
    {
        if (!IsAttachedToTerminal)
        {
            return new Size (80, 25);
        }

        WindowsConsole.Coord maxWinSize;

        try
        {
            maxWinSize = GetLargestConsoleWindowSize (!IsLegacyConsole ? _outputHandle : _screenBuffer);
        }
        catch
        {
            maxWinSize = new WindowsConsole.Coord (80, 25);
        }

        return new Size (maxWinSize.X, maxWinSize.Y);
    }

    /// <inheritdoc/>
    public void SetSize (int width, int height)
    {
        // Do Nothing.
    }

    private bool _isDisposed;
    private nint _consoleBuffer;
    private readonly StringBuilder _everythingStringBuilder = new ();

    /// <inheritdoc/>
    public void Dispose ()
    {
        if (_isDisposed)
        {
            return;
        }

        if (!IsAttachedToTerminal)
        {
            _isDisposed = true;

            return;
        }

        if (IsLegacyConsole)
        {
            if (_screenBuffer != nint.Zero)
            {
                CloseHandle (_screenBuffer);
            }

            _screenBuffer = nint.Zero;
        }
        else
        {
            if (Environment.GetEnvironmentVariable ("VSAPPIDNAME") is null)
            {
                //Disable alternative screen buffer.
                Console.Out.Write (EscSeqUtils.CSI_RestoreCursorAndRestoreAltBufferWithBackscroll);

                //Set cursor key to cursor.
                Write (EscSeqUtils.CSI_ShowCursor);
            }
            else
            {
                // Simulate restoring the color and clearing the screen.
                Console.ForegroundColor = _foreground;
                Console.BackgroundColor = _background;
                Console.Clear ();
            }
        }

        _isDisposed = true;
    }

    /// <inheritdoc/>
    public void Suspend ()
    {
        // Suspends are not supported on Windows consoles in this implementation.
        // No-op to match prior behavior where DriverImpl skipped suspend on Windows.
    }
}
