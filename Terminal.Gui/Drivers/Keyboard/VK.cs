// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
// ReSharper disable IdentifierTypo
namespace Terminal.Gui.Drivers;

/// <summary>Generated from winuser.h. See https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes</summary>
public enum VK : ushort
{
    /// <summary>Left mouse button.</summary>
    LBUTTON = 0x01,

    /// <summary>Right mouse button.</summary>
    RBUTTON = 0x02,

    /// <summary>Control-break processing.</summary>
    CANCEL = 0x03,

    /// <summary>Middle mouse button (three-button mouse).</summary>
    MBUTTON = 0x04,

    /// <summary>X1 mouse button.</summary>
    XLeftButton = 0x05,

    /// <summary>X2 mouse button.</summary>
    XMiddleButton = 0x06,

    /// <summary>BACKSPACE key.</summary>
    BACK = 0x08,

    /// <summary>TAB key.</summary>
    TAB = 0x09,

    /// <summary>CLEAR key.</summary>
    CLEAR = 0x0C,

    /// <summary>ENTER key.</summary>
    RETURN = 0x0D,

    /// <summary>SHIFT key.</summary>
    SHIFT = 0x10,

    /// <summary>CTRL key.</summary>
    CONTROL = 0x11,

    /// <summary>ALT key.</summary>
    MENU = 0x12,

    /// <summary>PAUSE key.</summary>
    PAUSE = 0x13,

    /// <summary>CAPS LOCK key.</summary>
    CAPITAL = 0x14,

    /// <summary>IME Kana mode.</summary>
    KANA = 0x15,

    /// <summary>IME Hangul mode.</summary>
    HANGUL = 0x15,

    /// <summary>IME Junja mode.</summary>
    JUNJA = 0x17,

    /// <summary>IME final mode.</summary>
    FINAL = 0x18,

    /// <summary>IME Hanja mode.</summary>
    HANJA = 0x19,

    /// <summary>IME Kanji mode.</summary>
    KANJI = 0x19,

    /// <summary>ESC key.</summary>
    ESCAPE = 0x1B,

    /// <summary>IME convert.</summary>
    CONVERT = 0x1C,

    /// <summary>IME nonconvert.</summary>
    NONCONVERT = 0x1D,

    /// <summary>IME accept.</summary>
    ACCEPT = 0x1E,

    /// <summary>IME mode change request.</summary>
    MODECHANGE = 0x1F,

    /// <summary>SPACEBAR.</summary>
    SPACE = 0x20,

    /// <summary>PAGE UP key.</summary>
    PRIOR = 0x21,

    /// <summary>PAGE DOWN key.</summary>
    NEXT = 0x22,

    /// <summary>END key.</summary>
    END = 0x23,

    /// <summary>HOME key.</summary>
    HOME = 0x24,

    /// <summary>LEFT ARROW key.</summary>
    LEFT = 0x25,

    /// <summary>UP ARROW key.</summary>
    UP = 0x26,

    /// <summary>RIGHT ARROW key.</summary>
    RIGHT = 0x27,

    /// <summary>DOWN ARROW key.</summary>
    DOWN = 0x28,

    /// <summary>SELECT key.</summary>
    SELECT = 0x29,

    /// <summary>PRINT key.</summary>
    PRINT = 0x2A,

    /// <summary>EXECUTE key</summary>
    EXECUTE = 0x2B,

    /// <summary>PRINT SCREEN key</summary>
    SNAPSHOT = 0x2C,

    /// <summary>INS key</summary>
    INSERT = 0x2D,

    /// <summary>DEL key</summary>
    DELETE = 0x2E,

    /// <summary>HELP key</summary>
    HELP = 0x2F,

    /// <summary>Left Windows key (Natural keyboard)</summary>
    LWIN = 0x5B,

    /// <summary>Right Windows key (Natural keyboard)</summary>
    RWIN = 0x5C,

    /// <summary>Applications key (Natural keyboard)</summary>
    APPS = 0x5D,

    /// <summary>Computer Sleep key</summary>
    SLEEP = 0x5F,

    /// <summary>Numeric keypad 0 key</summary>
    NUMPAD0 = 0x60,

    /// <summary>Numeric keypad 1 key</summary>
    NUMPAD1 = 0x61,

    /// <summary>Numeric keypad 2 key</summary>
    NUMPAD2 = 0x62,

    /// <summary>Numeric keypad 3 key</summary>
    NUMPAD3 = 0x63,

    /// <summary>Numeric keypad 4 key</summary>
    NUMPAD4 = 0x64,

    /// <summary>Numeric keypad 5 key</summary>
    NUMPAD5 = 0x65,

    /// <summary>Numeric keypad 6 key</summary>
    NUMPAD6 = 0x66,

    /// <summary>Numeric keypad 7 key</summary>
    NUMPAD7 = 0x67,

    /// <summary>Numeric keypad 8 key</summary>
    NUMPAD8 = 0x68,

    /// <summary>Numeric keypad 9 key</summary>
    NUMPAD9 = 0x69,

    /// <summary>Multiply key</summary>
    MULTIPLY = 0x6A,

    /// <summary>Add key</summary>
    ADD = 0x6B,

    /// <summary>Separator key</summary>
    SEPARATOR = 0x6C,

    /// <summary>Subtract key</summary>
    SUBTRACT = 0x6D,

    /// <summary>Decimal key</summary>
    DECIMAL = 0x6E,

    /// <summary>Divide key</summary>
    DIVIDE = 0x6F,

    /// <summary>F1 key</summary>
    F1 = 0x70,

    /// <summary>F2 key</summary>
    F2 = 0x71,

    /// <summary>F3 key</summary>
    F3 = 0x72,

    /// <summary>F4 key</summary>
    F4 = 0x73,

    /// <summary>F5 key</summary>
    F5 = 0x74,

    /// <summary>F6 key</summary>
    F6 = 0x75,

    /// <summary>F7 key</summary>
    F7 = 0x76,

    /// <summary>F8 key</summary>
    F8 = 0x77,

    /// <summary>F9 key</summary>
    F9 = 0x78,

    /// <summary>F10 key</summary>
    F10 = 0x79,

    /// <summary>F11 key</summary>
    F11 = 0x7A,

    /// <summary>F12 key</summary>
    F12 = 0x7B,

    /// <summary>F13 key</summary>
    F13 = 0x7C,

    /// <summary>F14 key</summary>
    F14 = 0x7D,

    /// <summary>F15 key</summary>
    F15 = 0x7E,

    /// <summary>F16 key</summary>
    F16 = 0x7F,

    /// <summary>F17 key</summary>
    F17 = 0x80,

    /// <summary>F18 key</summary>
    F18 = 0x81,

    /// <summary>F19 key</summary>
    F19 = 0x82,

    /// <summary>F20 key</summary>
    F20 = 0x83,

    /// <summary>F21 key</summary>
    F21 = 0x84,

    /// <summary>F22 key</summary>
    F22 = 0x85,

    /// <summary>F23 key</summary>
    F23 = 0x86,

    /// <summary>F24 key</summary>
    F24 = 0x87,

    /// <summary>NUM LOCK key</summary>
    NUMLOCK = 0x90,

    /// <summary>SCROLL LOCK key</summary>
    SCROLL = 0x91,

    /// <summary>NEC PC-9800 kbd definition: '=' key on numpad</summary>
    OEM_NEC_EQUAL = 0x92,

    /// <summary>Fujitsu/OASYS kbd definition: 'Dictionary' key</summary>
    OEM_FJ_JISHO = 0x92,

    /// <summary>Fujitsu/OASYS kbd definition: 'Unregister word' key</summary>
    OEM_FJ_MASSHOU = 0x93,

    /// <summary>Fujitsu/OASYS kbd definition: 'Register word' key</summary>
    OEM_FJ_TOUROKU = 0x94,

    /// <summary>Fujitsu/OASYS kbd definition: 'Left OYAYUBI' key</summary>
    OEM_FJ_LOYA = 0x95,

    /// <summary>Fujitsu/OASYS kbd definition: 'Right OYAYUBI' key</summary>
    OEM_FJ_ROYA = 0x96,

    /// <summary>Left SHIFT key</summary>
    LSHIFT = 0xA0,

    /// <summary>Right SHIFT key</summary>
    RSHIFT = 0xA1,

    /// <summary>Left CONTROL key</summary>
    LCONTROL = 0xA2,

    /// <summary>Right CONTROL key</summary>
    RCONTROL = 0xA3,

    /// <summary>Left MENU key (Left Alt key)</summary>
    LMENU = 0xA4,

    /// <summary>Right MENU key (Right Alt key)</summary>
    RMENU = 0xA5,

    /// <summary>Browser Back key</summary>
    BROWSER_BACK = 0xA6,

    /// <summary>Browser Forward key</summary>
    BROWSER_FORWARD = 0xA7,

    /// <summary>Browser Refresh key</summary>
    BROWSER_REFRESH = 0xA8,

    /// <summary>Browser Stop key</summary>
    BROWSER_STOP = 0xA9,

    /// <summary>Browser Search key</summary>
    BROWSER_SEARCH = 0xAA,

    /// <summary>Browser Favorites key</summary>
    BROWSER_FAVORITES = 0xAB,

    /// <summary>Browser Home key</summary>
    BROWSER_HOME = 0xAC,

    /// <summary>Volume Mute key</summary>
    VOLUME_MUTE = 0xAD,

    /// <summary>Volume Down key</summary>
    VOLUME_DOWN = 0xAE,

    /// <summary>Volume Up key</summary>
    VOLUME_UP = 0xAF,

    /// <summary>Next Track key</summary>
    MEDIA_NEXT_TRACK = 0xB0,

    /// <summary>Previous Track key</summary>
    MEDIA_PREV_TRACK = 0xB1,

    /// <summary>Stop Media key</summary>
    MEDIA_STOP = 0xB2,

    /// <summary>Play/Pause Media key</summary>
    MEDIA_PLAY_PAUSE = 0xB3,

    /// <summary>Start Mail key</summary>
    LAUNCH_MAIL = 0xB4,

    /// <summary>Select Media key</summary>
    LAUNCH_MEDIA_SELECT = 0xB5,

    /// <summary>Start Application 1 key</summary>
    LAUNCH_APP1 = 0xB6,

    /// <summary>Start Application 2 key</summary>
    LAUNCH_APP2 = 0xB7,

    /// <summary>Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the ';:' key</summary>
    OEM_1 = 0xBA,

    /// <summary>For any country/region, the '+' key</summary>
    OEM_PLUS = 0xBB,

    /// <summary>For any country/region, the ',' key</summary>
    OEM_COMMA = 0xBC,

    /// <summary>For any country/region, the '-' key</summary>
    OEM_MINUS = 0xBD,

    /// <summary>For any country/region, the '.' key</summary>
    OEM_PERIOD = 0xBE,

    /// <summary>Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the '/?' key</summary>
    OEM_2 = 0xBF,

    /// <summary>Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the '`~' key</summary>
    OEM_3 = 0xC0,

    /// <summary>Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the '[{' key</summary>
    OEM_4 = 0xDB,

    /// <summary>Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the '\|' key</summary>
    OEM_5 = 0xDC,

    /// <summary>Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the ']}' key</summary>
    OEM_6 = 0xDD,

    /// <summary>
    ///     Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the
    ///     'single-quote/double-quote' key
    /// </summary>
    OEM_7 = 0xDE,

    /// <summary>Used for miscellaneous characters; it can vary by keyboard.</summary>
    OEM_8 = 0xDF,

    /// <summary>'AX' key on Japanese AX kbd</summary>
    OEM_AX = 0xE1,

    /// <summary>Either the angle bracket key or the backslash key on the RT 102-key keyboard</summary>
    OEM_102 = 0xE2,

    /// <summary>Help key on ICO</summary>
    ICO_HELP = 0xE3,

    /// <summary>00 key on ICO</summary>
    ICO_00 = 0xE4,

    /// <summary>Process key</summary>
    PROCESSKEY = 0xE5,

    /// <summary>Clear key on ICO</summary>
    ICO_CLEAR = 0xE6,

    /// <summary>Packet key to be used to pass Unicode characters as if they were keystrokes</summary>
    PACKET = 0xE7,

    /// <summary>Reset key</summary>
    OEM_RESET = 0xE9,

    /// <summary>Jump key</summary>
    OEM_JUMP = 0xEA,

    /// <summary>PA1 key</summary>
    OEM_PA1 = 0xEB,

    /// <summary>PA2 key</summary>
    OEM_PA2 = 0xEC,

    /// <summary>PA3 key</summary>
    OEM_PA3 = 0xED,

    /// <summary>WsCtrl key</summary>
    OEM_WSCTRL = 0xEE,

    /// <summary>CuSel key</summary>
    OEM_CUSEL = 0xEF,

    /// <summary>Attn key</summary>
    OEM_ATTN = 0xF0,

    /// <summary>Finish key</summary>
    OEM_FINISH = 0xF1,

    /// <summary>Copy key</summary>
    OEM_COPY = 0xF2,

    /// <summary>Auto key</summary>
    OEM_AUTO = 0xF3,

    /// <summary>Enlw key</summary>
    OEM_ENLW = 0xF4,

    /// <summary>BackTab key</summary>
    OEM_BACKTAB = 0xF5,

    /// <summary>Attn key</summary>
    ATTN = 0xF6,

    /// <summary>CrSel key</summary>
    CRSEL = 0xF7,

    /// <summary>ExSel key</summary>
    EXSEL = 0xF8,

    /// <summary>Erase EOF key</summary>
    EREOF = 0xF9,

    /// <summary>Play key</summary>
    PLAY = 0xFA,

    /// <summary>Zoom key</summary>
    ZOOM = 0xFB,

    /// <summary>Reserved</summary>
    NONAME = 0xFC,

    /// <summary>PA1 key</summary>
    PA1 = 0xFD,

    /// <summary>Clear key</summary>
    OEM_CLEAR = 0xFE
}
