using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Terminal.Gui.Drivers;

internal class WindowsClipboard : ClipboardBase
{
    private const uint CF_UNICODE_TEXT = 13;

    public override bool IsSupported { get; } = CheckClipboardIsAvailable ();

    private static bool CheckClipboardIsAvailable ()
    {
        // Attempt to open the clipboard
        if (OpenClipboard (nint.Zero))
        {
            // Clipboard is available
            // Close the clipboard after use
            CloseClipboard ();

            return true;
        }
        // Clipboard is not available
        return false;
    }

    protected override string GetClipboardDataImpl ()
    {
        try
        {
            if (!OpenClipboard (nint.Zero))
            {
                return string.Empty;
            }

            nint handle = GetClipboardData (CF_UNICODE_TEXT);

            if (handle == nint.Zero)
            {
                return string.Empty;
            }

            nint pointer = nint.Zero;

            try
            {
                pointer = GlobalLock (handle);

                if (pointer == nint.Zero)
                {
                    return string.Empty;
                }

                int size = GlobalSize (handle);
                var buff = new byte [size];

                Marshal.Copy (pointer, buff, 0, size);

                return Encoding.Unicode.GetString (buff).TrimEnd ('\0');
            }
            finally
            {
                if (pointer != nint.Zero)
                {
                    GlobalUnlock (handle);
                }
            }
        }
        finally
        {
            CloseClipboard ();
        }
    }

    protected override void SetClipboardDataImpl (string text)
    {
        OpenClipboard ();

        EmptyClipboard ();
        nint hGlobal = default;

        try
        {
            int bytes = (text.Length + 1) * 2;
            hGlobal = Marshal.AllocHGlobal (bytes);

            if (hGlobal == default (nint))
            {
                ThrowWin32 ();
            }

            nint target = GlobalLock (hGlobal);

            if (target == default (nint))
            {
                ThrowWin32 ();
            }

            try
            {
                Marshal.Copy (text.ToCharArray (), 0, target, text.Length);
            }
            finally
            {
                GlobalUnlock (target);
            }

            if (SetClipboardData (CF_UNICODE_TEXT, hGlobal) == default (nint))
            {
                ThrowWin32 ();
            }

            hGlobal = default (nint);
        }
        finally
        {
            if (hGlobal != default (nint))
            {
                Marshal.FreeHGlobal (hGlobal);
            }

            CloseClipboard ();
        }
    }

    [DllImport ("user32.dll", SetLastError = true)]
    [return: MarshalAs (UnmanagedType.Bool)]
    private static extern bool CloseClipboard ();

    [DllImport ("user32.dll")]
    private static extern bool EmptyClipboard ();

    [DllImport ("user32.dll", SetLastError = true)]
    private static extern nint GetClipboardData (uint uFormat);

    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern nint GlobalLock (nint hMem);

    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern int GlobalSize (nint handle);

    [DllImport ("kernel32.dll", SetLastError = true)]
    [return: MarshalAs (UnmanagedType.Bool)]
    private static extern bool GlobalUnlock (nint hMem);

    [DllImport ("User32.dll", SetLastError = true)]
    [return: MarshalAs (UnmanagedType.Bool)]
    private static extern bool IsClipboardFormatAvailable (uint format);

    private void OpenClipboard ()
    {
        var num = 10;

        while (true)
        {
            if (OpenClipboard (default (nint)))
            {
                break;
            }

            if (--num == 0)
            {
                ThrowWin32 ();
            }

            Thread.Sleep (100);
        }
    }

    [DllImport ("user32.dll", SetLastError = true)]
    [return: MarshalAs (UnmanagedType.Bool)]
    private static extern bool OpenClipboard (nint hWndNewOwner);

    [DllImport ("user32.dll", SetLastError = true)]
    private static extern nint SetClipboardData (uint uFormat, nint data);

    private void ThrowWin32 () { throw new Win32Exception (Marshal.GetLastWin32Error ()); }
}
