#nullable disable
using System.Runtime.InteropServices;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Windows-specific keyboard layout information using P/Invoke.
///     This class encapsulates all Windows API calls for keyboard layout operations.
/// </summary>
internal static class WindowsKeyboardLayout
{
#if !WT_ISSUE_8871_FIXED // https://github.com/microsoft/terminal/issues/8871
    /// <summary>
    ///     Translates (maps) a virtual-key code into a scan code or character value, or translates a scan code into a
    ///     virtual-key code.
    /// </summary>
    /// <param name="vk"></param>
    /// <param name="uMapType">
    ///     If MAPVK_VK_TO_CHAR (2) - The uCode parameter is a virtual-key code and is translated into an
    ///     un-shifted character value in the low order word of the return value.
    /// </param>
    /// <param name="dwhkl"></param>
    /// <returns>
    ///     An un-shifted character value in the low order word of the return value. Dead keys (diacritics) are indicated
    ///     by setting the top bit of the return value. If there is no translation, the function returns 0. See Remarks.
    /// </returns>
    [DllImport ("user32.dll", EntryPoint = "MapVirtualKeyExW", CharSet = CharSet.Unicode)]
    private static extern uint MapVirtualKeyEx (VK vk, uint uMapType, nint dwhkl);

    /// <summary>Retrieves the active input locale identifier (formerly called the keyboard layout).</summary>
    /// <param name="idThread">0 for current thread</param>
    /// <returns>
    ///     The return value is the input locale identifier for the thread. The low word contains a Language Identifier
    ///     for the input language and the high word contains a device handle to the physical layout of the keyboard.
    /// </returns>
    [DllImport ("user32.dll", EntryPoint = "GetKeyboardLayout", CharSet = CharSet.Unicode)]
    private static extern nint GetKeyboardLayout (nint idThread);

    [DllImport ("user32.dll")]
    private static extern nint GetForegroundWindow ();

    [DllImport ("user32.dll")]
    private static extern nint GetWindowThreadProcessId (nint hWnd, nint ProcessId);

    /// <summary>
    ///     Translates the specified virtual-key code and keyboard state to the corresponding Unicode character or
    ///     characters using the Win32 API MapVirtualKey.
    /// </summary>
    /// <param name="vk"></param>
    /// <returns>
    ///     An un-shifted character value in the low order word of the return value. Dead keys (diacritics) are indicated
    ///     by setting the top bit of the return value. If there is no translation, the function returns 0.
    /// </returns>
    public static uint MapVKtoChar (VK vk)
    {
        if (Environment.OSVersion.Platform != PlatformID.Win32NT)
        {
            return 0;
        }

        nint tid = GetWindowThreadProcessId (GetForegroundWindow (), 0);
        nint hkl = GetKeyboardLayout (tid);

        return MapVirtualKeyEx (vk, 2, hkl);
    }
#else
    /// <summary>
    /// Translates (maps) a virtual-key code into a scan code or character value, or translates a scan code into a virtual-key code.
    /// </summary>
    /// <param name="vk"></param>
    /// <param name="uMapType">
    /// If MAPVK_VK_TO_CHAR (2) - The uCode parameter is a virtual-key code and is translated into an unshifted
    /// character value in the low order word of the return value. 
    /// </param>
    /// <returns>An unshifted character value in the low order word of the return value. Dead keys (diacritics)
    /// are indicated by setting the top bit of the return value. If there is no translation,
    /// the function returns 0. See Remarks.</returns>
    [DllImport ("user32.dll", EntryPoint = "MapVirtualKeyW", CharSet = CharSet.Unicode)]
    private static extern uint MapVirtualKey (VK vk, uint uMapType = 2);

    public static uint MapVKtoChar (VK vk) => MapVirtualKey (vk, 2);
#endif

    /// <summary>
    ///     Retrieves the name of the active input locale identifier (formerly called the keyboard layout) for the calling
    ///     thread.
    /// </summary>
    /// <param name="pwszKLID"></param>
    /// <returns></returns>
    [DllImport ("user32.dll")]
    private static extern bool GetKeyboardLayoutName ([Out] StringBuilder pwszKLID);

    /// <summary>
    ///     Retrieves the name of the active input locale identifier (formerly called the keyboard layout) for the calling
    ///     thread.
    /// </summary>
    /// <returns></returns>
    public static string GetKeyboardLayoutName ()
    {
        if (Environment.OSVersion.Platform != PlatformID.Win32NT)
        {
            return "none";
        }

        var klidSB = new StringBuilder ();
        GetKeyboardLayoutName (klidSB);

        return klidSB.ToString ();
    }
}
