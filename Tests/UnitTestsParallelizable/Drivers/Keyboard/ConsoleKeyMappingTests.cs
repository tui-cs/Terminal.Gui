namespace DriverTests.Keyboard;

[Collection ("Driver Tests")]
public class ConsoleKeyMappingTests
{
    [Theory]
    [InlineData ('a', ConsoleKey.A, false, false, false, (KeyCode)'a')]
    [InlineData ('A', ConsoleKey.A, true, false, false, KeyCode.A | KeyCode.ShiftMask)]
    [InlineData ('á', ConsoleKey.A, false, false, false, (KeyCode)'á')]
    [InlineData ('Á', ConsoleKey.A, true, false, false, (KeyCode)'Á' | KeyCode.ShiftMask)]
    [InlineData ('à', ConsoleKey.A, false, false, false, (KeyCode)'à')]
    [InlineData ('À', ConsoleKey.A, true, false, false, (KeyCode)'À' | KeyCode.ShiftMask)]
    [InlineData ('5', ConsoleKey.D5, false, false, false, KeyCode.D5)]
    [InlineData ('%', ConsoleKey.D5, true, false, false, (KeyCode)'%' | KeyCode.ShiftMask)]
    [InlineData ('€', ConsoleKey.D5, false, true, true, (KeyCode)'€' | KeyCode.AltMask | KeyCode.CtrlMask)]
    [InlineData ('?', ConsoleKey.Oem4, true, false, false, (KeyCode)'?' | KeyCode.ShiftMask)]
    [InlineData ('\'', ConsoleKey.Oem4, false, false, false, (KeyCode)'\'')]
    [InlineData ('q', ConsoleKey.Q, false, false, false, (KeyCode)'q')]
    [InlineData ('\0', ConsoleKey.F2, false, false, false, KeyCode.F2)]
    [InlineData ('英', ConsoleKey.None, false, false, false, (KeyCode)'英')]
    [InlineData ('\r', ConsoleKey.Enter, false, false, false, KeyCode.Enter)]
    public void MapConsoleKeyInfoToKeyCode_Also_Return_Modifiers (
        char keyChar,
        ConsoleKey consoleKey,
        bool shift,
        bool alt,
        bool control,
        KeyCode expectedKeyCode
    )
    {
        var consoleKeyInfo = new ConsoleKeyInfo (keyChar, consoleKey, shift, alt, control);
        KeyCode keyCode = ConsoleKeyMapping.MapConsoleKeyInfoToKeyCode (consoleKeyInfo);

        Assert.Equal (keyCode, expectedKeyCode);
    }

    [Theory]
    [InlineData ('a', false, false, false, (KeyCode)'a')]
    [InlineData ('A', true, false, false, KeyCode.A | KeyCode.ShiftMask)]
    [InlineData ('á', false, false, false, (KeyCode)'á')]
    [InlineData ('Á', true, false, false, (KeyCode)'Á' | KeyCode.ShiftMask)]
    [InlineData ('à', false, false, false, (KeyCode)'à')]
    [InlineData ('À', true, false, false, (KeyCode)'À' | KeyCode.ShiftMask)]
    [InlineData ('5', false, false, false, KeyCode.D5)]
    [InlineData ('%', true, false, false, (KeyCode)'%' | KeyCode.ShiftMask)]
    [InlineData ('€', false, true, true, (KeyCode)'€' | KeyCode.AltMask | KeyCode.CtrlMask)]
    [InlineData ('?', true, false, false, (KeyCode)'?' | KeyCode.ShiftMask)]
    [InlineData ('\'', false, false, false, (KeyCode)'\'')]
    [InlineData ('q', false, false, false, (KeyCode)'q')]
    [InlineData ((uint)KeyCode.F2, false, false, false, KeyCode.F2)]
    [InlineData ('英', false, false, false, (KeyCode)'英')]
    [InlineData ('\r', false, false, false, KeyCode.Enter)]
    [InlineData ('\n', false, false, false, (KeyCode)'\n')]
    public void MapToKeyCodeModifiers_Tests (
        uint keyChar,
        bool shift,
        bool alt,
        bool control,
        KeyCode expectedKeyCode
    )
    {
        ConsoleModifiers modifiers = ConsoleKeyMapping.GetModifiers (shift, alt, control);
        var keyCode = (KeyCode)keyChar;
        keyCode = ConsoleKeyMapping.MapToKeyCodeModifiers (modifiers, keyCode);

        Assert.Equal (keyCode, expectedKeyCode);
    }

    public static IEnumerable<object []> UnShiftedChars =>
        new List<object []>
        {
            new object [] { 'a', 'A', KeyCode.A | KeyCode.ShiftMask },
            new object [] { 'z', 'Z', KeyCode.Z | KeyCode.ShiftMask },
            new object [] { 'á', 'Á', (KeyCode)'Á' | KeyCode.ShiftMask },
            new object [] { 'à', 'À', (KeyCode)'À' | KeyCode.ShiftMask },
            new object [] { 'ý', 'Ý', (KeyCode)'Ý' | KeyCode.ShiftMask },
            new object [] { '1', '!', (KeyCode)'!' | KeyCode.ShiftMask },
            new object [] { '2', '"', (KeyCode)'"' | KeyCode.ShiftMask },
            new object [] { '3', '#', (KeyCode)'#' | KeyCode.ShiftMask },
            new object [] { '4', '$', (KeyCode)'$' | KeyCode.ShiftMask },
            new object [] { '5', '%', (KeyCode)'%' | KeyCode.ShiftMask },
            new object [] { '6', '&', (KeyCode)'&' | KeyCode.ShiftMask },
            new object [] { '7', '/', (KeyCode)'/' | KeyCode.ShiftMask },
            new object [] { '8', '(', (KeyCode)'(' | KeyCode.ShiftMask },
            new object [] { '9', ')', (KeyCode)')' | KeyCode.ShiftMask },
            new object [] { '0', '=', (KeyCode)'=' | KeyCode.ShiftMask },
            new object [] { '\\', '|', (KeyCode)'|' | KeyCode.ShiftMask },
            new object [] { '\'', '?', (KeyCode)'?' | KeyCode.ShiftMask },
            new object [] { '«', '»', (KeyCode)'»' | KeyCode.ShiftMask },
            new object [] { '+', '*', (KeyCode)'*' | KeyCode.ShiftMask },
            new object [] { '´', '`', (KeyCode)'`' | KeyCode.ShiftMask },
            new object [] { 'º', 'ª', (KeyCode)'ª' | KeyCode.ShiftMask },
            new object [] { '~', '^', (KeyCode)'^' | KeyCode.ShiftMask },
            new object [] { '<', '>', (KeyCode)'>' | KeyCode.ShiftMask },
            new object [] { ',', ';', (KeyCode)';' | KeyCode.ShiftMask },
            new object [] { '.', ':', (KeyCode)':' | KeyCode.ShiftMask },
            new object [] { '-', '_', (KeyCode)'_' | KeyCode.ShiftMask }
        };

    [Theory]
    [InlineData (KeyCode.A, false, false, false)] // Unshifted A (lowercase)
    [InlineData (KeyCode.B, false, false, false)]
    [InlineData (KeyCode.Z, false, false, false)]
    [InlineData (KeyCode.A | KeyCode.ShiftMask, true, false, false)] // Shifted A (uppercase)
    [InlineData (KeyCode.Z | KeyCode.ShiftMask, true, false, false)]
    [InlineData (KeyCode.A | KeyCode.CtrlMask, false, false, true)] // Ctrl+A
    [InlineData (KeyCode.A | KeyCode.AltMask, false, true, false)] // Alt+A
    [InlineData (KeyCode.A | KeyCode.ShiftMask | KeyCode.CtrlMask, true, false, true)] // Ctrl+Shift+A
    [InlineData (KeyCode.A | KeyCode.ShiftMask | KeyCode.AltMask, true, true, false)] // Alt+Shift+A
    [InlineData (KeyCode.A | KeyCode.CtrlMask | KeyCode.AltMask, false, true, true)] // Ctrl+Alt+A
    [InlineData (KeyCode.A | KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask, true, true, true)] // All modifiers
    public void MapToConsoleModifiers_LetterKeys_ReturnsCorrectModifiers (
       KeyCode key,
       bool expectedShift,
       bool expectedAlt,
       bool expectedControl
   )
    {
        // Act
        ConsoleModifiers result = ConsoleKeyMapping.MapToConsoleModifiers (key);

        // Assert
        Assert.Equal (expectedShift, result.HasFlag (ConsoleModifiers.Shift));
        Assert.Equal (expectedAlt, result.HasFlag (ConsoleModifiers.Alt));
        Assert.Equal (expectedControl, result.HasFlag (ConsoleModifiers.Control));
    }

    [Theory]
    [InlineData (KeyCode.A)] // 65 = 'A' in ASCII, but represents unshifted key
    [InlineData (KeyCode.B)]
    [InlineData (KeyCode.M)]
    [InlineData (KeyCode.Z)]
    public void MapToConsoleModifiers_UnshiftedLetterKeys_DoesNotSetShiftFlag (KeyCode key)
    {
        // This test verifies the BUGFIX: KeyCode.A-Z (65-90) represent UNSHIFTED keys,
        // even though their numeric values match uppercase ASCII characters.
        // The old code incorrectly checked char.IsUpper((char)key) which would fail this test.

        // Act
        ConsoleModifiers result = ConsoleKeyMapping.MapToConsoleModifiers (key);

        // Assert - Shift should NOT be set for unshifted letter keys
        Assert.False (result.HasFlag (ConsoleModifiers.Shift),
            $"Shift should not be set for unshifted {key}. The KeyCode value {(int)key} represents a lowercase, unshifted key.");
    }

    [Theory]
    [InlineData (KeyCode.D1, false)] // Unshifted number keys
    [InlineData (KeyCode.D5, false)]
    [InlineData (KeyCode.Space, false)]
    [InlineData (KeyCode.Enter, false)]
    [InlineData (KeyCode.Tab, false)]
    [InlineData (KeyCode.D1 | KeyCode.ShiftMask, true)] // Shifted number keys
    public void MapToConsoleModifiers_NonLetterKeys_ReturnsCorrectShiftState (KeyCode key, bool expectedShift)
    {
        // Act
        ConsoleModifiers result = ConsoleKeyMapping.MapToConsoleModifiers (key);

        // Assert
        Assert.Equal (expectedShift, result.HasFlag (ConsoleModifiers.Shift));
    }

    [Theory]
    [InlineData (KeyCode.A, 'a', ConsoleKey.A, false)] // Unshifted A = lowercase 'a'
    [InlineData (KeyCode.B, 'b', ConsoleKey.B, false)]
    [InlineData (KeyCode.M, 'm', ConsoleKey.M, false)]
    [InlineData (KeyCode.Z, 'z', ConsoleKey.Z, false)]
    public void GetConsoleKeyInfoFromKeyCode_UnshiftedLetterKeys_ReturnsLowercaseChar (
        KeyCode key,
        char expectedChar,
        ConsoleKey expectedKey,
        bool expectedShift
    )
    {
        // This test verifies the BUGFIX: Key.A through Key.Z should produce lowercase characters
        // when no ShiftMask is set. The old code would incorrectly return uppercase 'A'.

        // Act
        ConsoleKeyInfo result = ConsoleKeyMapping.GetConsoleKeyInfoFromKeyCode (key);

        // Assert
        Assert.Equal (expectedChar, result.KeyChar);
        Assert.Equal (expectedKey, result.Key);
        Assert.Equal (expectedShift, (result.Modifiers & ConsoleModifiers.Shift) != 0);
    }

    [Theory]
    [InlineData (KeyCode.A | KeyCode.ShiftMask, 'A', ConsoleKey.A, true)] // Shifted A = uppercase 'A'
    [InlineData (KeyCode.B | KeyCode.ShiftMask, 'B', ConsoleKey.B, true)]
    [InlineData (KeyCode.M | KeyCode.ShiftMask, 'M', ConsoleKey.M, true)]
    [InlineData (KeyCode.Z | KeyCode.ShiftMask, 'Z', ConsoleKey.Z, true)]
    public void GetConsoleKeyInfoFromKeyCode_ShiftedLetterKeys_ReturnsUppercaseChar (
        KeyCode key,
        char expectedChar,
        ConsoleKey expectedKey,
        bool expectedShift
    )
    {
        // Act
        ConsoleKeyInfo result = ConsoleKeyMapping.GetConsoleKeyInfoFromKeyCode (key);

        // Assert
        Assert.Equal (expectedChar, result.KeyChar);
        Assert.Equal (expectedKey, result.Key);
        Assert.Equal (expectedShift, (result.Modifiers & ConsoleModifiers.Shift) != 0);
    }

    [Theory]
    [InlineData (KeyCode.A | KeyCode.CtrlMask, ConsoleKey.A, false, false, true)]
    [InlineData (KeyCode.A | KeyCode.AltMask, ConsoleKey.A, false, true, false)]
    [InlineData (KeyCode.A | KeyCode.ShiftMask | KeyCode.CtrlMask, ConsoleKey.A, true, false, true)]
    [InlineData (KeyCode.Z | KeyCode.CtrlMask, ConsoleKey.Z, false, false, true)]
    public void GetConsoleKeyInfoFromKeyCode_LetterKeysWithModifiers_ReturnsCorrectModifiers (
        KeyCode key,
        ConsoleKey expectedConsoleKey,
        bool expectedShift,
        bool expectedAlt,
        bool expectedControl
    )
    {
        // Act
        ConsoleKeyInfo result = ConsoleKeyMapping.GetConsoleKeyInfoFromKeyCode (key);

        // Assert
        Assert.Equal (expectedConsoleKey, result.Key);
        Assert.Equal (expectedShift, (result.Modifiers & ConsoleModifiers.Shift) != 0);
        Assert.Equal (expectedAlt, (result.Modifiers & ConsoleModifiers.Alt) != 0);
        Assert.Equal (expectedControl, (result.Modifiers & ConsoleModifiers.Control) != 0);
    }

    [Theory]
    [InlineData (KeyCode.Enter, '\r', ConsoleKey.Enter)]
    [InlineData (KeyCode.Tab, '\t', ConsoleKey.Tab)]
    [InlineData (KeyCode.Esc, '\u001B', ConsoleKey.Escape)]
    [InlineData (KeyCode.Backspace, '\b', ConsoleKey.Backspace)]
    [InlineData (KeyCode.Space, ' ', ConsoleKey.Spacebar)]
    public void GetConsoleKeyInfoFromKeyCode_SpecialKeys_ReturnsCorrectKeyChar (
        KeyCode key,
        char expectedChar,
        ConsoleKey expectedKey
    )
    {
        // Act
        ConsoleKeyInfo result = ConsoleKeyMapping.GetConsoleKeyInfoFromKeyCode (key);

        // Assert
        Assert.Equal (expectedChar, result.KeyChar);
        Assert.Equal (expectedKey, result.Key);
    }

    [Theory]
    [InlineData (KeyCode.F1, ConsoleKey.F1)]
    [InlineData (KeyCode.F5, ConsoleKey.F5)]
    [InlineData (KeyCode.F12, ConsoleKey.F12)]
    [InlineData (KeyCode.CursorUp, ConsoleKey.UpArrow)]
    [InlineData (KeyCode.CursorDown, ConsoleKey.DownArrow)]
    [InlineData (KeyCode.CursorLeft, ConsoleKey.LeftArrow)]
    [InlineData (KeyCode.CursorRight, ConsoleKey.RightArrow)]
    [InlineData (KeyCode.Home, ConsoleKey.Home)]
    [InlineData (KeyCode.End, ConsoleKey.End)]
    [InlineData (KeyCode.PageUp, ConsoleKey.PageUp)]
    [InlineData (KeyCode.PageDown, ConsoleKey.PageDown)]
    [InlineData (KeyCode.Delete, ConsoleKey.Delete)]
    [InlineData (KeyCode.Insert, ConsoleKey.Insert)]
    public void GetConsoleKeyInfoFromKeyCode_NavigationAndFunctionKeys_ReturnsCorrectConsoleKey (
        KeyCode key,
        ConsoleKey expectedKey
    )
    {
        // Act
        ConsoleKeyInfo result = ConsoleKeyMapping.GetConsoleKeyInfoFromKeyCode (key);

        // Assert
        Assert.Equal (expectedKey, result.Key);
    }

    [Theory]
    [InlineData (KeyCode.D0, '0', ConsoleKey.D0)]
    [InlineData (KeyCode.D1, '1', ConsoleKey.D1)]
    [InlineData (KeyCode.D5, '5', ConsoleKey.D5)]
    [InlineData (KeyCode.D9, '9', ConsoleKey.D9)]
    public void GetConsoleKeyInfoFromKeyCode_NumberKeys_ReturnsCorrectKeyChar (
        KeyCode key,
        char expectedChar,
        ConsoleKey expectedKey
    )
    {
        // Act
        ConsoleKeyInfo result = ConsoleKeyMapping.GetConsoleKeyInfoFromKeyCode (key);

        // Assert
        Assert.Equal (expectedChar, result.KeyChar);
        Assert.Equal (expectedKey, result.Key);
    }

    [Fact]
    public void MapToConsoleModifiers_AllLetterKeys_UnshiftedDoesNotSetShift ()
    {
        // This comprehensive test ensures ALL letter keys A-Z without ShiftMask
        // do not have Shift set in the returned modifiers.
        // This will fail if the old "char.IsUpper" check is present.

        for (KeyCode key = KeyCode.A; key <= KeyCode.Z; key++)
        {
            // Act
            ConsoleModifiers result = ConsoleKeyMapping.MapToConsoleModifiers (key);

            // Assert
            Assert.False (
                result.HasFlag (ConsoleModifiers.Shift),
                $"Shift should not be set for unshifted {key} (value {(int)key}). " +
                $"KeyCode.{key} represents a lowercase, unshifted key even though its numeric value is {(int)key}."
            );
        }
    }

    [Fact]
    public void MapToConsoleModifiers_AllLetterKeysShifted_SetsShift ()
    {
        // Verify that WITH ShiftMask, all letter keys DO set Shift

        for (KeyCode key = KeyCode.A; key <= KeyCode.Z; key++)
        {
            KeyCode shiftedKey = key | KeyCode.ShiftMask;

            // Act
            ConsoleModifiers result = ConsoleKeyMapping.MapToConsoleModifiers (shiftedKey);

            // Assert
            Assert.True (
                result.HasFlag (ConsoleModifiers.Shift),
                $"Shift should be set for {shiftedKey}"
            );
        }
    }

    [Fact]
    public void GetConsoleKeyInfoFromKeyCode_AllUnshiftedLetterKeys_ReturnLowercaseChars ()
    {
        // This comprehensive test verifies ALL letter keys A-Z produce lowercase characters
        // when no ShiftMask is set. This is the KEY test that will fail with the old code.

        for (KeyCode key = KeyCode.A; key <= KeyCode.Z; key++)
        {
            // Act
            ConsoleKeyInfo result = ConsoleKeyMapping.GetConsoleKeyInfoFromKeyCode (key);

            // Calculate expected lowercase character
            char expectedChar = (char)('a' + (key - KeyCode.A));

            // Assert
            Assert.True (
                char.IsLower (result.KeyChar),
                $"KeyChar for unshifted {key} should be lowercase, but got '{result.KeyChar}' (0x{(int)result.KeyChar:X2}). " +
                $"Expected lowercase '{expectedChar}' (0x{(int)expectedChar:X2})."
            );

            Assert.Equal (
                expectedChar,
                result.KeyChar
            );

            Assert.False (
                (result.Modifiers & ConsoleModifiers.Shift) != 0,
                $"Shift modifier should not be set for unshifted {key}"
            );
        }
    }

    [Fact]
    public void GetConsoleKeyInfoFromKeyCode_AllShiftedLetterKeys_ReturnUppercaseChars ()
    {
        // Verify that WITH ShiftMask, all letter keys produce uppercase characters

        for (KeyCode key = KeyCode.A; key <= KeyCode.Z; key++)
        {
            KeyCode shiftedKey = key | KeyCode.ShiftMask;

            // Act
            ConsoleKeyInfo result = ConsoleKeyMapping.GetConsoleKeyInfoFromKeyCode (shiftedKey);

            // Calculate expected uppercase character
            char expectedChar = (char)('A' + (key - KeyCode.A));

            // Assert
            Assert.True (
                char.IsUpper (result.KeyChar),
                $"KeyChar for shifted {shiftedKey} should be uppercase, but got '{result.KeyChar}'"
            );

            Assert.Equal (expectedChar, result.KeyChar);

            Assert.True (
                (result.Modifiers & ConsoleModifiers.Shift) != 0,
                $"Shift modifier should be set for {shiftedKey}"
            );
        }
    }

    [Theory]
    [InlineData (KeyCode.A, KeyCode.A | KeyCode.ShiftMask, 'a', 'A')] // Without vs With Shift
    [InlineData (KeyCode.M, KeyCode.M | KeyCode.ShiftMask, 'm', 'M')]
    [InlineData (KeyCode.Z, KeyCode.Z | KeyCode.ShiftMask, 'z', 'Z')]
    public void GetConsoleKeyInfoFromKeyCode_ShiftMaskChangesCase (
        KeyCode unshifted,
        KeyCode shifted,
        char expectedUnshiftedChar,
        char expectedShiftedChar
    )
    {
        // Act
        ConsoleKeyInfo unshiftedResult = ConsoleKeyMapping.GetConsoleKeyInfoFromKeyCode (unshifted);
        ConsoleKeyInfo shiftedResult = ConsoleKeyMapping.GetConsoleKeyInfoFromKeyCode (shifted);

        // Assert - Unshifted should be lowercase
        Assert.Equal (expectedUnshiftedChar, unshiftedResult.KeyChar);
        Assert.False ((unshiftedResult.Modifiers & ConsoleModifiers.Shift) != 0);

        // Assert - Shifted should be uppercase
        Assert.Equal (expectedShiftedChar, shiftedResult.KeyChar);
        Assert.True ((shiftedResult.Modifiers & ConsoleModifiers.Shift) != 0);
    }
}
