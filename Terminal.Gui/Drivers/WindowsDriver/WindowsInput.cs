using System.Runtime.InteropServices;
using Terminal.Gui.Tracing;
using static Terminal.Gui.Drivers.WindowsConsole;

namespace Terminal.Gui.Drivers;

internal class WindowsInput : InputImpl<InputRecord>, IWindowsInput
{
    private readonly nint _inputHandle;

    [DllImport ("kernel32.dll", EntryPoint = "ReadConsoleInputW", CharSet = CharSet.Unicode)]
    public static extern bool ReadConsoleInput (nint hConsoleInput, nint lpBuffer, uint nLength, out uint lpNumberOfEventsRead);

    [DllImport ("kernel32.dll", EntryPoint = "PeekConsoleInputW", CharSet = CharSet.Unicode)]
    public static extern bool PeekConsoleInput (nint hConsoleInput, nint lpBuffer, uint nLength, out uint lpNumberOfEventsRead);

    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern nint GetStdHandle (int nStdHandle);

    [DllImport ("kernel32.dll")]
    private static extern bool GetConsoleMode (nint hConsoleHandle, out uint lpMode);

    [DllImport ("kernel32.dll")]
    private static extern bool SetConsoleMode (nint hConsoleHandle, uint dwMode);

    private readonly uint _originalConsoleMode;

    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern bool FlushConsoleInputBuffer (nint hConsoleInput);

    public WindowsInput ()
    {
        // Check if we have a real console first
        if (!IsAttachedToTerminal)
        {
            Trace.Lifecycle (nameof (WindowsInput), "Init", "Console is not attached to a terminal. Running in degraded mode.");

            return;
        }

        try
        {
            _inputHandle = GetStdHandle (STD_INPUT_HANDLE);

            GetConsoleMode (_inputHandle, out uint v);
            _originalConsoleMode = v;

            uint newConsoleMode = _originalConsoleMode;
            newConsoleMode |= (uint)(ConsoleModes.EnableMouseInput | ConsoleModes.EnableExtendedFlags);
            newConsoleMode &= ~(uint)ConsoleModes.EnableQuickEditMode;
            newConsoleMode &= ~(uint)ConsoleModes.EnableProcessedInput;
            SetConsoleMode (_inputHandle, newConsoleMode);
        }
        catch
        {
            // ignore errors during unit tests
        }
    }

    public override bool Peek ()
    {
        const int BUFFER_SIZE = 1; // We only need to check if there's at least one event
        nint pRecord = Marshal.AllocHGlobal (Marshal.SizeOf<InputRecord> () * BUFFER_SIZE);

        try
        {
            // Use PeekConsoleInput to inspect the input buffer without removing events
            if (PeekConsoleInput (_inputHandle, pRecord, BUFFER_SIZE, out uint numberOfEventsRead))
            {
                // Return true if there's at least one event in the buffer
                return numberOfEventsRead > 0;
            }
            else
            {
                // Handle the failure of PeekConsoleInput
                throw new InvalidOperationException ("Failed to peek console input.");
            }
        }
        catch (Exception ex)
        {
            // Optionally log the exception
            Logging.Error (@$"Error in Peek: {ex.Message}");

            return false;
        }
        finally
        {
            // Free the allocated memory
            Marshal.FreeHGlobal (pRecord);
        }
    }

    public override IEnumerable<InputRecord> Read ()
    {
        const int BUFFER_SIZE = 1;
        nint pRecord = Marshal.AllocHGlobal (Marshal.SizeOf<InputRecord> () * BUFFER_SIZE);

        try
        {
            ReadConsoleInput (_inputHandle, pRecord, BUFFER_SIZE, out uint numberEventsRead);

            return numberEventsRead == 0 ? [] : new [] { Marshal.PtrToStructure<InputRecord> (pRecord) };
        }
        catch (Exception)
        {
            Logging.Error ($"Error reading console input, error code: {Marshal.GetLastWin32Error ()}.");

            return [];
        }
        finally
        {
            Marshal.FreeHGlobal (pRecord);
        }
    }

    public override void Dispose ()
    {
        try
        {
            if (!FlushConsoleInputBuffer (_inputHandle))
            {
                throw new ApplicationException ($"Failed to flush input buffer, error code: {Marshal.GetLastWin32Error ()}.");
            }

            SetConsoleMode (_inputHandle, _originalConsoleMode);
        }
        catch
        {
            // ignore errors during unit tests
        }
    }
}
