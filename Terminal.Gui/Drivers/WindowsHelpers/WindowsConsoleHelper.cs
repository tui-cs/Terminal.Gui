using System.Runtime.InteropServices;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Shared helper for low-level Windows Console API calls that are
///     not specific to the VT input or output helpers.
/// </summary>
internal static class WindowsConsoleHelper
{
    private const int STD_INPUT_HANDLE = -10;
    private const int STD_OUTPUT_HANDLE = -11;

    /// <summary>
    ///     Tests whether both stdin and stdout are connected to a real console device.
    /// </summary>
    /// <param name="inputAttached">
    ///     <see langword="true"/> if stdin is connected to a console device.
    /// </param>
    /// <param name="outputAttached">
    ///     <see langword="true"/> if stdout is connected to a console device.
    /// </param>
    /// <returns><see langword="true"/> if both handles are attached.</returns>
    public static bool IsAttachedToTerminal (out bool inputAttached, out bool outputAttached)
    {
        nint inH = GetStdHandle (STD_INPUT_HANDLE);
        nint outH = GetStdHandle (STD_OUTPUT_HANDLE);

        inputAttached = inH != nint.Zero && GetConsoleMode (inH, out _);
        outputAttached = outH != nint.Zero && GetConsoleMode (outH, out _);

        return inputAttached && outputAttached;
    }

    #region P/Invoke

    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern nint GetStdHandle (int nStdHandle);

    [DllImport ("kernel32.dll")]
    private static extern bool GetConsoleMode (nint hConsoleHandle, out uint lpMode);

    #endregion
}
