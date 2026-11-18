using System.Runtime.InteropServices;

namespace DriverTests.Windows;


[Trait ("Platform", "Windows")]
[Collection ("Driver Tests")]
public class WindowsKeyConverterTests
{
    private readonly WindowsKeyConverter _converter = new ();

    #region ToKey Tests - Basic Characters

    [Theory]
    [InlineData ('a', ConsoleKey.A, false, false, false, KeyCode.A)] // lowercase a
    [InlineData ('A', ConsoleKey.A, true, false, false, KeyCode.A | KeyCode.ShiftMask)] // uppercase A
    [InlineData ('z', ConsoleKey.Z, false, false, false, KeyCode.Z)]
    [InlineData ('Z', ConsoleKey.Z, true, false, false, KeyCode.Z | KeyCode.ShiftMask)]
    public void ToKey_LetterKeys_ReturnsExpectedKeyCode (
        char unicodeChar,
        ConsoleKey consoleKey,
        bool shift,
        bool alt,
        bool ctrl,
        KeyCode expectedKeyCode
    )
    {
        if (!OperatingSystem.IsWindows ())
        {
            return;
        }

        // Arrange
        WindowsConsole.InputRecord inputRecord = CreateInputRecord (unicodeChar, consoleKey, shift, alt, ctrl);

        // Act
        var result = _converter.ToKey (inputRecord);

        // Assert
        Assert.Equal (expectedKeyCode, result.KeyCode);
    }

    [Theory]
    [InlineData ('0', ConsoleKey.D0, false, false, false, KeyCode.D0)]
    [InlineData ('1', ConsoleKey.D1, false, false, false, KeyCode.D1)]
    [InlineData ('9', ConsoleKey.D9, false, false, false, KeyCode.D9)]
    public void ToKey_NumberKeys_ReturnsExpectedKeyCode (
        char unicodeChar,
        ConsoleKey consoleKey,
        bool shift,
        bool alt,
        bool ctrl,
        KeyCode expectedKeyCode
    )
    {
        if (!OperatingSystem.IsWindows ())
        {
            return;
        }

        // Arrange
        WindowsConsole.InputRecord inputRecord = CreateInputRecord (unicodeChar, consoleKey, shift, alt, ctrl);

        // Act
        var result = _converter.ToKey (inputRecord);

        // Assert
        Assert.Equal (expectedKeyCode, result.KeyCode);
    }

    #endregion

    #region ToKey Tests - Modifiers

    [Theory]
    [InlineData ('a', ConsoleKey.A, false, false, true, KeyCode.A | KeyCode.CtrlMask)] // Ctrl+A
    [InlineData ('A', ConsoleKey.A, true, false, true, KeyCode.A | KeyCode.ShiftMask | KeyCode.CtrlMask)] // Ctrl+Shift+A (Windows keeps ShiftMask)
    [InlineData ('a', ConsoleKey.A, false, true, false, KeyCode.A | KeyCode.AltMask)] // Alt+A
    [InlineData ('A', ConsoleKey.A, true, true, false, KeyCode.A | KeyCode.ShiftMask | KeyCode.AltMask)] // Alt+Shift+A
    [InlineData ('a', ConsoleKey.A, false, true, true, KeyCode.A | KeyCode.CtrlMask | KeyCode.AltMask)] // Ctrl+Alt+A
    public void ToKey_WithModifiers_ReturnsExpectedKeyCode (
        char unicodeChar,
        ConsoleKey consoleKey,
        bool shift,
        bool alt,
        bool ctrl,
        KeyCode expectedKeyCode
    )
    {
        if (!OperatingSystem.IsWindows ())
        {
            return;
        }

        // Arrange
        WindowsConsole.InputRecord inputRecord = CreateInputRecord (unicodeChar, consoleKey, shift, alt, ctrl);

        // Act
        var result = _converter.ToKey (inputRecord);

        // Assert
        Assert.Equal (expectedKeyCode, result.KeyCode);
    }

    #endregion

    #region ToKey Tests - Special Keys

    [Theory]
    [InlineData (ConsoleKey.Enter, KeyCode.Enter)]
    [InlineData (ConsoleKey.Escape, KeyCode.Esc)]
    [InlineData (ConsoleKey.Tab, KeyCode.Tab)]
    [InlineData (ConsoleKey.Backspace, KeyCode.Backspace)]
    [InlineData (ConsoleKey.Delete, KeyCode.Delete)]
    [InlineData (ConsoleKey.Insert, KeyCode.Insert)]
    [InlineData (ConsoleKey.Home, KeyCode.Home)]
    [InlineData (ConsoleKey.End, KeyCode.End)]
    [InlineData (ConsoleKey.PageUp, KeyCode.PageUp)]
    [InlineData (ConsoleKey.PageDown, KeyCode.PageDown)]
    [InlineData (ConsoleKey.UpArrow, KeyCode.CursorUp)]
    [InlineData (ConsoleKey.DownArrow, KeyCode.CursorDown)]
    [InlineData (ConsoleKey.LeftArrow, KeyCode.CursorLeft)]
    [InlineData (ConsoleKey.RightArrow, KeyCode.CursorRight)]
    public void ToKey_SpecialKeys_ReturnsExpectedKeyCode (ConsoleKey consoleKey, KeyCode expectedKeyCode)
    {
        if (!OperatingSystem.IsWindows ())
        {
            return;
        }

        // Arrange
        char unicodeChar = consoleKey switch
                           {
                               ConsoleKey.Enter => '\r',
                               ConsoleKey.Escape => '\u001B',
                               ConsoleKey.Tab => '\t',
                               ConsoleKey.Backspace => '\b',
                               _ => '\0'
                           };
        WindowsConsole.InputRecord inputRecord = CreateInputRecord (unicodeChar, consoleKey, false, false, false);

        // Act
        var result = _converter.ToKey (inputRecord);

        // Assert
        Assert.Equal (expectedKeyCode, result.KeyCode);
    }

    [Theory]
    [InlineData (ConsoleKey.F1, KeyCode.F1)]
    [InlineData (ConsoleKey.F2, KeyCode.F2)]
    [InlineData (ConsoleKey.F3, KeyCode.F3)]
    [InlineData (ConsoleKey.F4, KeyCode.F4)]
    [InlineData (ConsoleKey.F5, KeyCode.F5)]
    [InlineData (ConsoleKey.F6, KeyCode.F6)]
    [InlineData (ConsoleKey.F7, KeyCode.F7)]
    [InlineData (ConsoleKey.F8, KeyCode.F8)]
    [InlineData (ConsoleKey.F9, KeyCode.F9)]
    [InlineData (ConsoleKey.F10, KeyCode.F10)]
    [InlineData (ConsoleKey.F11, KeyCode.F11)]
    [InlineData (ConsoleKey.F12, KeyCode.F12)]
    public void ToKey_FunctionKeys_ReturnsExpectedKeyCode (ConsoleKey consoleKey, KeyCode expectedKeyCode)
    {
        if (!OperatingSystem.IsWindows ())
        {
            return;
        }

        // Arrange
        WindowsConsole.InputRecord inputRecord = CreateInputRecord ('\0', consoleKey, false, false, false);

        // Act
        var result = _converter.ToKey (inputRecord);

        // Assert
        Assert.Equal (expectedKeyCode, result.KeyCode);
    }

    #endregion

    #region ToKey Tests - VK_PACKET (Unicode/IME)

    [Theory]
    [InlineData ('中')] // Chinese character
    [InlineData ('日')] // Japanese character
    [InlineData ('한')] // Korean character
    [InlineData ('é')] // Accented character
    [InlineData ('€')] // Euro symbol
    [InlineData ('Ω')] // Greek character
    public void ToKey_VKPacket_Unicode_ReturnsExpectedCharacter (char unicodeChar)
    {
        if (!OperatingSystem.IsWindows ())
        {
            return;
        }

        // Arrange
        WindowsConsole.InputRecord inputRecord = CreateVKPacketInputRecord (unicodeChar);

        // Act
        var result = _converter.ToKey (inputRecord);

        // Assert
        Assert.Equal ((KeyCode)unicodeChar, result.KeyCode);
    }

    [Fact]
    public void ToKey_VKPacket_ZeroChar_ReturnsNull ()
    {
        if (!OperatingSystem.IsWindows ())
        {
            return;
        }

        // Arrange
        WindowsConsole.InputRecord inputRecord = CreateVKPacketInputRecord ('\0');

        // Act
        var result = _converter.ToKey (inputRecord);

        // Assert
        Assert.Equal (KeyCode.Null, result.KeyCode);
    }

    [Fact]
    public void ToKey_VKPacket_SurrogatePair_DocumentsCurrentLimitation ()
    {
        if (!OperatingSystem.IsWindows ())
        {
            return;
        }

        // Emoji '😀' (U+1F600) requires a surrogate pair: High=U+D83D, Low=U+DE00
        // Windows sends this as TWO consecutive VK_PACKET events (one for each char)
        // because KeyEventRecord.UnicodeChar is a single 16-bit char field.
        // 
        // CURRENT LIMITATION: WindowsKeyConverter processes each event independently
        // and does not combine surrogate pairs into a single Rune/KeyCode.
        // This test documents the current (incorrect) behavior.
        //
        // TODO: Implement proper surrogate pair handling at the InputProcessor level
        // to combine consecutive high+low surrogate events into a single Key with the
        // complete Unicode codepoint.
        // See: https://docs.microsoft.com/en-us/windows/console/key-event-record

        var highSurrogate = '\uD83D'; // High surrogate for 😀
        var lowSurrogate = '\uDE00'; // Low surrogate for 😀

        // First event with high surrogate
        WindowsConsole.InputRecord highRecord = CreateVKPacketInputRecord (highSurrogate);
        var highResult = _converter.ToKey (highRecord);

        // Second event with low surrogate
        WindowsConsole.InputRecord lowRecord = CreateVKPacketInputRecord (lowSurrogate);
        var lowResult = _converter.ToKey (lowRecord);

        // Currently each surrogate half is processed independently as invalid KeyCodes
        // These assertions document the current (broken) behavior
        Assert.Equal ((KeyCode)highSurrogate, highResult.KeyCode);
        Assert.Equal ((KeyCode)lowSurrogate, lowResult.KeyCode);

        // What SHOULD happen (future fix):
        // The InputProcessor should detect the surrogate pair and combine them:
        // var expectedRune = new Rune(0x1F600); // 😀
        // Assert.Equal((KeyCode)expectedRune.Value, combinedResult.KeyCode);
    }

    #endregion

    #region ToKey Tests - OEM Keys

    [Theory]
    //[InlineData (';', ConsoleKey.Oem1, false, (KeyCode)';')] // Keyboard layout dependent and shifted key is needed to produce ';' (Pt)
    [InlineData (':', ConsoleKey.Oem1, true, (KeyCode)':')]
    //[InlineData ('/', ConsoleKey.Oem2, false, (KeyCode)'/')] // Keyboard layout dependent and shifted key is needed to produce '/' (Pt)
    [InlineData ('?', ConsoleKey.Oem2, true, (KeyCode)'?')]
    [InlineData (',', ConsoleKey.OemComma, false, (KeyCode)',')]
    [InlineData ('<', ConsoleKey.OemComma, true, (KeyCode)'<')]
    [InlineData ('.', ConsoleKey.OemPeriod, false, (KeyCode)'.')]
    [InlineData ('>', ConsoleKey.OemPeriod, true, (KeyCode)'>')]
    //[InlineData ('=', ConsoleKey.OemPlus, false, (KeyCode)'=')] // Keyboard layout dependent and shifted key is needed to produce '=' (Pt)
    [InlineData ('+', ConsoleKey.OemPlus, true, (KeyCode)'+')] // Shifted OemPlus is '+'
    [InlineData ('-', ConsoleKey.OemMinus, false, (KeyCode)'-')]
    [InlineData ('_', ConsoleKey.OemMinus, true, (KeyCode)'_')] // Shifted OemMinus is '_'
    public void ToKey_OEMKeys_ReturnsExpectedKeyCode (
        char unicodeChar,
        ConsoleKey consoleKey,
        bool shift,
        KeyCode expectedKeyCode
    )
    {
        if (!OperatingSystem.IsWindows ())
        {
            return;
        }

        // Arrange
        WindowsConsole.InputRecord inputRecord = CreateInputRecord (unicodeChar, consoleKey, shift, false, false);

        // Act
        var result = _converter.ToKey (inputRecord);

        // Assert
        Assert.Equal (expectedKeyCode, result.KeyCode);
    }

    #endregion

    #region ToKey Tests - NumPad

    [Theory]
    [InlineData ('0', ConsoleKey.NumPad0, KeyCode.D0)]
    [InlineData ('1', ConsoleKey.NumPad1, KeyCode.D1)]
    [InlineData ('5', ConsoleKey.NumPad5, KeyCode.D5)]
    [InlineData ('9', ConsoleKey.NumPad9, KeyCode.D9)]
    public void ToKey_NumPadKeys_ReturnsExpectedKeyCode (
        char unicodeChar,
        ConsoleKey consoleKey,
        KeyCode expectedKeyCode
    )
    {
        if (!OperatingSystem.IsWindows ())
        {
            return;
        }

        // Arrange
        WindowsConsole.InputRecord inputRecord = CreateInputRecord (unicodeChar, consoleKey, false, false, false);

        // Act
        var result = _converter.ToKey (inputRecord);

        // Assert
        Assert.Equal (expectedKeyCode, result.KeyCode);
    }

    [Theory]
    [InlineData ('*', ConsoleKey.Multiply, (KeyCode)'*')]
    [InlineData ('+', ConsoleKey.Add, (KeyCode)'+')]
    [InlineData ('-', ConsoleKey.Subtract, (KeyCode)'-')]
    [InlineData ('.', ConsoleKey.Decimal, (KeyCode)'.')]
    [InlineData ('/', ConsoleKey.Divide, (KeyCode)'/')]
    public void ToKey_NumPadOperators_ReturnsExpectedKeyCode (
        char unicodeChar,
        ConsoleKey consoleKey,
        KeyCode expectedKeyCode
    )
    {
        if (!OperatingSystem.IsWindows ())
        {
            return;
        }

        // Arrange
        WindowsConsole.InputRecord inputRecord = CreateInputRecord (unicodeChar, consoleKey, false, false, false);

        // Act
        var result = _converter.ToKey (inputRecord);

        // Assert
        Assert.Equal (expectedKeyCode, result.KeyCode);
    }

    #endregion

    #region ToKey Tests - Null/Empty

    [Fact]
    public void ToKey_NullKey_ReturnsEmpty ()
    {
        if (!OperatingSystem.IsWindows ())
        {
            return;
        }

        // Arrange
        WindowsConsole.InputRecord inputRecord = CreateInputRecord ('\0', ConsoleKey.None, false, false, false);

        // Act
        var result = _converter.ToKey (inputRecord);

        // Assert
        Assert.Equal (Key.Empty, result);
    }

    #endregion

    #region ToKeyInfo Tests - Basic Keys

    [Theory]
    [InlineData (KeyCode.A, ConsoleKey.A, 'a')]
    [InlineData (KeyCode.A | KeyCode.ShiftMask, ConsoleKey.A, 'A')]
    [InlineData (KeyCode.Z, ConsoleKey.Z, 'z')]
    [InlineData (KeyCode.Z | KeyCode.ShiftMask, ConsoleKey.Z, 'Z')]
    public void ToKeyInfo_LetterKeys_ReturnsExpectedInputRecord (
        KeyCode keyCode,
        ConsoleKey expectedConsoleKey,
        char expectedChar
    )
    {
        if (!OperatingSystem.IsWindows ())
        {
            return;
        }

        // Arrange
        var key = new Key (keyCode);

        // Act
        WindowsConsole.InputRecord result = _converter.ToKeyInfo (key);

        // Assert
        Assert.Equal (WindowsConsole.EventType.Key, result.EventType);
        Assert.Equal ((VK)expectedConsoleKey, result.KeyEvent.wVirtualKeyCode);
        Assert.Equal (expectedChar, result.KeyEvent.UnicodeChar);
        Assert.True (result.KeyEvent.bKeyDown);
        Assert.Equal ((ushort)1, result.KeyEvent.wRepeatCount);
    }

    [Theory]
    [InlineData (KeyCode.D0, ConsoleKey.D0, '0')]
    [InlineData (KeyCode.D1, ConsoleKey.D1, '1')]
    [InlineData (KeyCode.D9, ConsoleKey.D9, '9')]
    public void ToKeyInfo_NumberKeys_ReturnsExpectedInputRecord (
        KeyCode keyCode,
        ConsoleKey expectedConsoleKey,
        char expectedChar
    )
    {
        if (!OperatingSystem.IsWindows ())
        {
            return;
        }

        // Arrange
        var key = new Key (keyCode);

        // Act
        WindowsConsole.InputRecord result = _converter.ToKeyInfo (key);

        // Assert
        Assert.Equal ((VK)expectedConsoleKey, result.KeyEvent.wVirtualKeyCode);
        Assert.Equal (expectedChar, result.KeyEvent.UnicodeChar);
    }

    #endregion

    #region ToKeyInfo Tests - Special Keys

    [Theory]
    [InlineData (KeyCode.Enter, ConsoleKey.Enter, '\r')]
    [InlineData (KeyCode.Esc, ConsoleKey.Escape, '\u001B')]
    [InlineData (KeyCode.Tab, ConsoleKey.Tab, '\t')]
    [InlineData (KeyCode.Backspace, ConsoleKey.Backspace, '\b')]
    [InlineData (KeyCode.Space, ConsoleKey.Spacebar, ' ')]
    public void ToKeyInfo_SpecialKeys_ReturnsExpectedInputRecord (
        KeyCode keyCode,
        ConsoleKey expectedConsoleKey,
        char expectedChar
    )
    {
        if (!OperatingSystem.IsWindows ())
        {
            return;
        }

        // Arrange
        var key = new Key (keyCode);

        // Act
        WindowsConsole.InputRecord result = _converter.ToKeyInfo (key);

        // Assert
        Assert.Equal ((VK)expectedConsoleKey, result.KeyEvent.wVirtualKeyCode);
        Assert.Equal (expectedChar, result.KeyEvent.UnicodeChar);
    }

    [Theory]
    [InlineData (KeyCode.Delete, ConsoleKey.Delete)]
    [InlineData (KeyCode.Insert, ConsoleKey.Insert)]
    [InlineData (KeyCode.Home, ConsoleKey.Home)]
    [InlineData (KeyCode.End, ConsoleKey.End)]
    [InlineData (KeyCode.PageUp, ConsoleKey.PageUp)]
    [InlineData (KeyCode.PageDown, ConsoleKey.PageDown)]
    [InlineData (KeyCode.CursorUp, ConsoleKey.UpArrow)]
    [InlineData (KeyCode.CursorDown, ConsoleKey.DownArrow)]
    [InlineData (KeyCode.CursorLeft, ConsoleKey.LeftArrow)]
    [InlineData (KeyCode.CursorRight, ConsoleKey.RightArrow)]
    public void ToKeyInfo_NavigationKeys_ReturnsExpectedInputRecord (KeyCode keyCode, ConsoleKey expectedConsoleKey)
    {
        if (!OperatingSystem.IsWindows ())
        {
            return;
        }

        // Arrange
        var key = new Key (keyCode);

        // Act
        WindowsConsole.InputRecord result = _converter.ToKeyInfo (key);

        // Assert
        Assert.Equal ((VK)expectedConsoleKey, result.KeyEvent.wVirtualKeyCode);
    }

    [Theory]
    [InlineData (KeyCode.F1, ConsoleKey.F1)]
    [InlineData (KeyCode.F5, ConsoleKey.F5)]
    [InlineData (KeyCode.F10, ConsoleKey.F10)]
    [InlineData (KeyCode.F12, ConsoleKey.F12)]
    public void ToKeyInfo_FunctionKeys_ReturnsExpectedInputRecord (KeyCode keyCode, ConsoleKey expectedConsoleKey)
    {
        if (!OperatingSystem.IsWindows ())
        {
            return;
        }

        // Arrange
        var key = new Key (keyCode);

        // Act
        WindowsConsole.InputRecord result = _converter.ToKeyInfo (key);

        // Assert
        Assert.Equal ((VK)expectedConsoleKey, result.KeyEvent.wVirtualKeyCode);
    }

    #endregion

    #region ToKeyInfo Tests - Modifiers

    [Theory]
    [InlineData (KeyCode.A | KeyCode.ShiftMask, WindowsConsole.ControlKeyState.ShiftPressed)]
    [InlineData (KeyCode.A | KeyCode.CtrlMask, WindowsConsole.ControlKeyState.LeftControlPressed)]
    [InlineData (KeyCode.A | KeyCode.AltMask, WindowsConsole.ControlKeyState.LeftAltPressed)]
    [InlineData (
                    KeyCode.A | KeyCode.CtrlMask | KeyCode.AltMask,
                    WindowsConsole.ControlKeyState.LeftControlPressed | WindowsConsole.ControlKeyState.LeftAltPressed)]
    [InlineData (
                    KeyCode.A | KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask,
                    WindowsConsole.ControlKeyState.ShiftPressed
                    | WindowsConsole.ControlKeyState.LeftControlPressed
                    | WindowsConsole.ControlKeyState.LeftAltPressed)]
    public void ToKeyInfo_WithModifiers_ReturnsExpectedControlKeyState (
        KeyCode keyCode,
        WindowsConsole.ControlKeyState expectedState
    )
    {
        if (!OperatingSystem.IsWindows ())
        {
            return;
        }

        // Arrange
        var key = new Key (keyCode);

        // Act
        WindowsConsole.InputRecord result = _converter.ToKeyInfo (key);

        // Assert
        Assert.Equal (expectedState, result.KeyEvent.dwControlKeyState);
    }

    #endregion

    #region ToKeyInfo Tests - Scan Codes

    [Theory]
    [InlineData (KeyCode.A, 30)]
    [InlineData (KeyCode.Enter, 28)]
    [InlineData (KeyCode.Esc, 1)]
    [InlineData (KeyCode.Space, 57)]
    [InlineData (KeyCode.F1, 59)]
    [InlineData (KeyCode.F10, 68)]
    [InlineData (KeyCode.CursorUp, 72)]
    [InlineData (KeyCode.Home, 71)]
    public void ToKeyInfo_ScanCodes_ReturnsExpectedScanCode (KeyCode keyCode, ushort expectedScanCode)
    {
        if (!OperatingSystem.IsWindows ())
        {
            return;
        }

        // Arrange
        var key = new Key (keyCode);

        // Act
        WindowsConsole.InputRecord result = _converter.ToKeyInfo (key);

        // Assert
        Assert.Equal (expectedScanCode, result.KeyEvent.wVirtualScanCode);
    }

    #endregion

    #region Round-Trip Tests

    [Theory]
    [InlineData (KeyCode.A)]
    [InlineData (KeyCode.A | KeyCode.ShiftMask)]
    [InlineData (KeyCode.A | KeyCode.CtrlMask)]
    [InlineData (KeyCode.Enter)]
    [InlineData (KeyCode.F1)]
    [InlineData (KeyCode.CursorUp)]
    [InlineData (KeyCode.Delete)]
    [InlineData (KeyCode.D5)]
    [InlineData (KeyCode.Space)]
    public void RoundTrip_ToKeyInfo_ToKey_PreservesKeyCode (KeyCode originalKeyCode)
    {
        if (!OperatingSystem.IsWindows ())
        {
            return;
        }

        // Arrange
        var originalKey = new Key (originalKeyCode);

        // Act
        WindowsConsole.InputRecord inputRecord = _converter.ToKeyInfo (originalKey);
        var roundTrippedKey = _converter.ToKey (inputRecord);

        // Assert
        Assert.Equal (originalKeyCode, roundTrippedKey.KeyCode);
    }

    [Theory]
    [InlineData ('a', ConsoleKey.A, false, false, false)]
    [InlineData ('A', ConsoleKey.A, true, false, false)]
    [InlineData ('a', ConsoleKey.A, false, false, true)] // Ctrl+A
    [InlineData ('0', ConsoleKey.D0, false, false, false)]
    public void RoundTrip_ToKey_ToKeyInfo_PreservesData (
        char unicodeChar,
        ConsoleKey consoleKey,
        bool shift,
        bool alt,
        bool ctrl
    )
    {
        if (!OperatingSystem.IsWindows ())
        {
            return;
        }

        // Arrange
        WindowsConsole.InputRecord originalRecord = CreateInputRecord (unicodeChar, consoleKey, shift, alt, ctrl);

        // Act
        var key = _converter.ToKey (originalRecord);
        WindowsConsole.InputRecord roundTrippedRecord = _converter.ToKeyInfo (key);

        // Assert
        Assert.Equal ((VK)consoleKey, roundTrippedRecord.KeyEvent.wVirtualKeyCode);

        // Check modifiers match
        var expectedState = WindowsConsole.ControlKeyState.NoControlKeyPressed;

        if (shift)
        {
            expectedState |= WindowsConsole.ControlKeyState.ShiftPressed;
        }

        if (alt)
        {
            expectedState |= WindowsConsole.ControlKeyState.LeftAltPressed;
        }

        if (ctrl)
        {
            expectedState |= WindowsConsole.ControlKeyState.LeftControlPressed;
        }

        Assert.True (roundTrippedRecord.KeyEvent.dwControlKeyState.HasFlag (expectedState));
    }

    #endregion

    #region CapsLock/NumLock Tests

    [Theory]
    [InlineData ('a', ConsoleKey.A, false, true)] // CapsLock on, no shift
    [InlineData ('A', ConsoleKey.A, true, true)] // CapsLock on, shift (should be lowercase from mapping)
    public void ToKey_WithCapsLock_ReturnsExpectedKeyCode (
        char unicodeChar,
        ConsoleKey consoleKey,
        bool shift,
        bool capsLock
    )
    {
        if (!OperatingSystem.IsWindows ())
        {
            return;
        }

        WindowsConsole.InputRecord inputRecord = CreateInputRecordWithLockStates (
                                                                                  unicodeChar,
                                                                                  consoleKey,
                                                                                  shift,
                                                                                  false,
                                                                                  false,
                                                                                  capsLock,
                                                                                  false,
                                                                                  false);

        // Act
        var result = _converter.ToKey (inputRecord);

        // Assert
        // The mapping should handle CapsLock properly via WindowsKeyHelper.MapKey
        Assert.NotEqual (KeyCode.Null, result.KeyCode);
    }

    #endregion

    #region Helper Methods

    private static WindowsConsole.InputRecord CreateInputRecord (
        char unicodeChar,
        ConsoleKey consoleKey,
        bool shift,
        bool alt,
        bool ctrl
    ) =>
        CreateInputRecordWithLockStates (unicodeChar, consoleKey, shift, alt, ctrl, false, false, false);

    private static WindowsConsole.InputRecord CreateInputRecordWithLockStates (
        char unicodeChar,
        ConsoleKey consoleKey,
        bool shift,
        bool alt,
        bool ctrl,
        bool capsLock,
        bool numLock,
        bool scrollLock
    )
    {
        var controlKeyState = WindowsConsole.ControlKeyState.NoControlKeyPressed;

        if (shift)
        {
            controlKeyState |= WindowsConsole.ControlKeyState.ShiftPressed;
        }

        if (alt)
        {
            controlKeyState |= WindowsConsole.ControlKeyState.LeftAltPressed;
        }

        if (ctrl)
        {
            controlKeyState |= WindowsConsole.ControlKeyState.LeftControlPressed;
        }

        if (capsLock)
        {
            controlKeyState |= WindowsConsole.ControlKeyState.CapslockOn;
        }

        if (numLock)
        {
            controlKeyState |= WindowsConsole.ControlKeyState.NumlockOn;
        }

        if (scrollLock)
        {
            controlKeyState |= WindowsConsole.ControlKeyState.ScrolllockOn;
        }

        return new ()
        {
            EventType = WindowsConsole.EventType.Key,
            KeyEvent = new ()
            {
                bKeyDown = true,
                wRepeatCount = 1,
                wVirtualKeyCode = (VK)consoleKey,
                wVirtualScanCode = 0,
                UnicodeChar = unicodeChar,
                dwControlKeyState = controlKeyState
            }
        };
    }

    private static WindowsConsole.InputRecord CreateVKPacketInputRecord (char unicodeChar) =>
        new ()
        {
            EventType = WindowsConsole.EventType.Key,
            KeyEvent = new ()
            {
                bKeyDown = true,
                wRepeatCount = 1,
                wVirtualKeyCode = VK.PACKET,
                wVirtualScanCode = 0,
                UnicodeChar = unicodeChar,
                dwControlKeyState = WindowsConsole.ControlKeyState.NoControlKeyPressed
            }
        };

    #endregion
}
