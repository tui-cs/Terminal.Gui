using System.Runtime.InteropServices;

// ReSharper disable InconsistentNaming

namespace Terminal.Gui.Drivers;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
///     Definitions for Windows Console API structures and constants.
/// </summary>
public class WindowsConsole
{
    [Flags]
    public enum ButtonState
    {
        NoButtonPressed = 0,
        Button1Pressed = 1,
        Button2Pressed = 4,
        Button3Pressed = 8,
        Button4Pressed = 16,
        RightmostButtonPressed = 2
    }

    /// <summary>
    ///     Windows Console mode flags.
    /// </summary>
    [Flags]
    public enum ConsoleModes : uint
    {
        EnableProcessedInput = 1,
        EnableVirtualTerminalProcessing = 4,
        EnableMouseInput = 16,
        EnableQuickEditMode = 64,
        EnableExtendedFlags = 128
    }

    [Flags]
    public enum ControlKeyState
    {
        NoControlKeyPressed = 0,
        RightAltPressed = 1,
        LeftAltPressed = 2,
        RightControlPressed = 4,
        LeftControlPressed = 8,
        ShiftPressed = 16,
        NumlockOn = 32,
        ScrolllockOn = 64,
        CapslockOn = 128,
        EnhancedKey = 256
    }

    [Flags]
    public enum EventFlags
    {
        NoEvent = 0,
        MouseMoved = 1,
        DoubleClick = 2,
        MouseWheeled = 4,
        MouseHorizontalWheeled = 8
    }

    public enum EventType : ushort
    {
        Focus = 0x10,
        Key = 0x1,
        Menu = 0x8,
        Mouse = 2,
        WindowBufferSize = 4
    }

    /// <summary>
    ///     Standard input handle constant.
    /// </summary>
    public const int STD_INPUT_HANDLE = -10;

    public static ConsoleKeyInfoEx ToConsoleKeyInfoEx (KeyEventRecord keyEvent)
    {
        ControlKeyState state = keyEvent.dwControlKeyState;

        bool shift = (state & ControlKeyState.ShiftPressed) != 0;
        bool alt = (state & (ControlKeyState.LeftAltPressed | ControlKeyState.RightAltPressed)) != 0;
        bool control = (state & (ControlKeyState.LeftControlPressed | ControlKeyState.RightControlPressed)) != 0;
        bool capslock = (state & ControlKeyState.CapslockOn) != 0;
        bool numlock = (state & ControlKeyState.NumlockOn) != 0;
        bool scrolllock = (state & ControlKeyState.ScrolllockOn) != 0;

        var cki = new ConsoleKeyInfo (keyEvent.UnicodeChar, (ConsoleKey)keyEvent.wVirtualKeyCode, shift, alt, control);

        return new (cki, capslock, numlock, scrolllock);
    }

    [StructLayout (LayoutKind.Explicit, CharSet = CharSet.Unicode)]
    public struct CharInfo
    {
        [FieldOffset (0)]
        public CharUnion Char;

        [FieldOffset (2)]
        public ushort Attributes;
    }

    [StructLayout (LayoutKind.Explicit, CharSet = CharSet.Unicode)]
    public struct CharUnion
    {
        [FieldOffset (0)]
        public char UnicodeChar;

        [FieldOffset (0)]
        public byte AsciiChar;
    }

    [StructLayout (LayoutKind.Explicit, Size = 4)]
    public struct COLORREF
    {
        public COLORREF (byte r, byte g, byte b)
        {
            Value = 0;
            R = r;
            G = g;
            B = b;
        }

        public COLORREF (uint value)
        {
            R = 0;
            G = 0;
            B = 0;
            Value = value & 0x00FFFFFF;
        }

        [FieldOffset (0)]
        public byte R;

        [FieldOffset (1)]
        public byte G;

        [FieldOffset (2)]
        public byte B;

        [FieldOffset (0)]
        public uint Value;
    }

    // See: https://github.com/tui-cs/Terminal.Gui/issues/357

    [StructLayout (LayoutKind.Sequential)]
    public struct CONSOLE_SCREEN_BUFFER_INFOEX
    {
        public uint cbSize;
        public Coord dwSize;
        public Coord dwCursorPosition;
        public ushort wAttributes;
        public SmallRect srWindow;
        public Coord dwMaximumWindowSize;
        public ushort wPopupAttributes;
        public bool bFullscreenSupported;

        [MarshalAs (UnmanagedType.ByValArray, SizeConst = 16)]
        public COLORREF [] ColorTable;
    }

    [StructLayout (LayoutKind.Sequential)]
    public struct ConsoleCursorInfo
    {
        /// <summary>
        ///     The percentage of the character cell that is filled by the cursor.This value is between 1 and 100.
        ///     The cursor appearance varies, ranging from completely filling the cell to showing up as a horizontal
        ///     line at the bottom of the cell.
        /// </summary>
        public uint dwSize;

        public bool bVisible;
    }

    [StructLayout (LayoutKind.Sequential)]
    public struct ConsoleKeyInfoEx
    {
        public ConsoleKeyInfoEx (ConsoleKeyInfo consoleKeyInfo, bool capslock, bool numlock, bool scrolllock)
        {
            ConsoleKeyInfo = consoleKeyInfo;
            CapsLock = capslock;
            NumLock = numlock;
            ScrollLock = scrolllock;
        }

        public ConsoleKeyInfo ConsoleKeyInfo;
        public bool CapsLock;
        public bool NumLock;
        public bool ScrollLock;

        /// <summary>
        ///     Prints a ConsoleKeyInfoEx structure
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public readonly string ToString (ConsoleKeyInfoEx ex)
        {
            var ke = new Key ((KeyCode)ex.ConsoleKeyInfo.KeyChar);
            var sb = new StringBuilder ();
            sb.Append ($"Key: {(KeyCode)ex.ConsoleKeyInfo.Key} ({ex.ConsoleKeyInfo.Key})");
            sb.Append ((ex.ConsoleKeyInfo.Modifiers & ConsoleModifiers.Shift) != 0 ? " | Shift" : string.Empty);
            sb.Append ((ex.ConsoleKeyInfo.Modifiers & ConsoleModifiers.Control) != 0 ? " | Control" : string.Empty);
            sb.Append ((ex.ConsoleKeyInfo.Modifiers & ConsoleModifiers.Alt) != 0 ? " | Alt" : string.Empty);
            sb.Append ($", KeyChar: {ke.AsRune.MakePrintable ()} ({(uint)ex.ConsoleKeyInfo.KeyChar}) ");
            sb.Append (ex.CapsLock ? "caps," : string.Empty);
            sb.Append (ex.NumLock ? "num," : string.Empty);
            sb.Append (ex.ScrollLock ? "scroll," : string.Empty);
            string s = sb.ToString ().TrimEnd (',').TrimEnd (' ');

            return $"[ConsoleKeyInfoEx({s})]";
        }
    }

    [StructLayout (LayoutKind.Sequential)]
    public struct Coord
    {
        public Coord (short x, short y)
        {
            X = x;
            Y = y;
        }

        public short X;
        public short Y;

        public readonly override string ToString () { return $"({X},{Y})"; }
    }

    [StructLayout (LayoutKind.Sequential)]
    public struct FocusEventRecord
    {
        public uint bSetFocus;
    }

    [StructLayout (LayoutKind.Explicit)]
    public struct InputRecord
    {
        [FieldOffset (0)]
        public EventType EventType;

        [FieldOffset (4)]
        public KeyEventRecord KeyEvent;

        [FieldOffset (4)]
        public MouseEventRecord MouseEvent;

        [FieldOffset (4)]
        public WindowBufferSizeRecord WindowBufferSizeEvent;

        [FieldOffset (4)]
        public MenuEventRecord MenuEvent;

        [FieldOffset (4)]
        public FocusEventRecord FocusEvent;

        public readonly override string ToString ()
        {
            return (EventType switch
            {
                EventType.Focus => FocusEvent.ToString (),
                EventType.Key => KeyEvent.ToString (),
                EventType.Menu => MenuEvent.ToString (),
                EventType.Mouse => MouseEvent.ToString (),
                EventType.WindowBufferSize => WindowBufferSizeEvent.ToString (),
                _ => "Unknown event type: " + EventType
            })!;
        }
    }

    /// <summary>
    ///     Key event record structure.
    /// </summary>
    [StructLayout (LayoutKind.Explicit, CharSet = CharSet.Unicode)]
    public struct KeyEventRecord
    {
        [FieldOffset (0)]
        [MarshalAs (UnmanagedType.Bool)]
        public bool bKeyDown;

        [FieldOffset (4)]
        [MarshalAs (UnmanagedType.U2)]
        public ushort wRepeatCount;

        [FieldOffset (6)]
        [MarshalAs (UnmanagedType.U2)]
        public VK wVirtualKeyCode;

        [FieldOffset (8)]
        [MarshalAs (UnmanagedType.U2)]
        public ushort wVirtualScanCode;

        [FieldOffset (10)]
        public char UnicodeChar;

        [FieldOffset (12)]
        [MarshalAs (UnmanagedType.U4)]
        public ControlKeyState dwControlKeyState;

        public readonly override string ToString ()
        {
            return
                $"[KeyEventRecord({(bKeyDown ? "down" : "up")},{wRepeatCount},{wVirtualKeyCode},{wVirtualScanCode},{new Rune (UnicodeChar).MakePrintable ()},{dwControlKeyState})]";
        }
    }

    [StructLayout (LayoutKind.Sequential)]
    public struct MenuEventRecord
    {
        public uint dwCommandId;
    }

    [StructLayout (LayoutKind.Explicit)]
    public struct MouseEventRecord
    {
        [FieldOffset (0)]
        public Coord MousePosition;

        [FieldOffset (4)]
        public ButtonState ButtonState;

        [FieldOffset (8)]
        public ControlKeyState ControlKeyState;

        [FieldOffset (12)]
        public EventFlags EventFlags;

        public readonly override string ToString () { return $"[Mouse{MousePosition},{ButtonState},{ControlKeyState},{EventFlags}]"; }
    }

    [StructLayout (LayoutKind.Sequential)]
    public struct SmallRect
    {
        public SmallRect (short left, short top, short right, short bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public short Left;
        public short Top;
        public short Right;
        public short Bottom;

        public static void MakeEmpty (ref SmallRect rect) { rect.Left = -1; }

        public readonly override string ToString () { return $"Left={Left},Top={Top},Right={Right},Bottom={Bottom}"; }

        public static void Update (ref SmallRect rect, short col, short row)
        {
            if (rect.Left == -1)
            {
                rect.Left = rect.Right = col;
                rect.Bottom = rect.Top = row;

                return;
            }

            if (col >= rect.Left && col <= rect.Right && row >= rect.Top && row <= rect.Bottom)
            {
                return;
            }

            if (col < rect.Left)
            {
                rect.Left = col;
            }

            if (col > rect.Right)
            {
                rect.Right = col;
            }

            if (row < rect.Top)
            {
                rect.Top = row;
            }

            if (row > rect.Bottom)
            {
                rect.Bottom = row;
            }
        }
    }

    public struct WindowBufferSizeRecord (short x, short y)
    {
        public Coord _size = new (x, y);

        public readonly override string ToString () { return $"[WindowBufferSize{_size}"; }
    }
}
