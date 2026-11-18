namespace DriverTests.AnsiHandling;

/// <summary>
///     Tests for <see cref="AnsiKeyboardEncoder"/> - verifies Key → ANSI sequence conversion.
/// </summary>
/// <remarks>
///     CoPilot - GitHub Copilot (GPT-4)
/// </remarks>
[Trait ("Category", "AnsiHandling")]
[Collection ("Driver Tests")]
public class AnsiKeyboardEncoderTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    #region Special Keys Tests

    [Theory]
    [InlineData (KeyCode.CursorUp, "\u001B[A")]
    [InlineData (KeyCode.CursorDown, "\u001B[B")]
    [InlineData (KeyCode.CursorRight, "\u001B[C")]
    [InlineData (KeyCode.CursorLeft, "\u001B[D")]
    [InlineData (KeyCode.Home, "\u001B[H")]
    [InlineData (KeyCode.End, "\u001B[F")]
    public void Encode_CursorKeys_ProducesCorrectSequence (KeyCode keyCode, string expectedSequence)
    {
        // Arrange
        Key key = new (keyCode);

        // Act
        string result = AnsiKeyboardEncoder.Encode (key);

        // Assert
        _output.WriteLine ($"KeyCode: {keyCode} → {result.Replace ("\u001B", "ESC")}");
        Assert.Equal (expectedSequence, result);
    }

    [Theory]
    [InlineData (KeyCode.F1, "\u001BOP")]   // SS3 format
    [InlineData (KeyCode.F2, "\u001BOQ")]
    [InlineData (KeyCode.F3, "\u001BOR")]
    [InlineData (KeyCode.F4, "\u001BOS")]
    [InlineData (KeyCode.F5, "\u001B[15~")] // CSI format
    [InlineData (KeyCode.F6, "\u001B[17~")]
    [InlineData (KeyCode.F7, "\u001B[18~")]
    [InlineData (KeyCode.F8, "\u001B[19~")]
    [InlineData (KeyCode.F9, "\u001B[20~")]
    [InlineData (KeyCode.F10, "\u001B[21~")]
    [InlineData (KeyCode.F11, "\u001B[23~")]
    [InlineData (KeyCode.F12, "\u001B[24~")]
    public void Encode_FunctionKeys_ProducesCorrectSequence (KeyCode keyCode, string expectedSequence)
    {
        // Arrange
        Key key = new (keyCode);

        // Act
        string result = AnsiKeyboardEncoder.Encode (key);

        // Assert
        _output.WriteLine ($"KeyCode: {keyCode} → {result.Replace ("\u001B", "ESC")}");
        Assert.Equal (expectedSequence, result);
    }

    [Theory]
    // Shift modifier (modifier = 2)
    [InlineData (KeyCode.F1 | KeyCode.ShiftMask, "\u001B[1;2P")]   // SS3 → CSI with modifier
    [InlineData (KeyCode.F5 | KeyCode.ShiftMask, "\u001B[15;2~")]
    [InlineData (KeyCode.F6 | KeyCode.ShiftMask, "\u001B[17;2~")]
    [InlineData (KeyCode.F12 | KeyCode.ShiftMask, "\u001B[24;2~")]
    // Ctrl modifier (modifier = 5)
    [InlineData (KeyCode.F1 | KeyCode.CtrlMask, "\u001B[1;5P")]
    [InlineData (KeyCode.F5 | KeyCode.CtrlMask, "\u001B[15;5~")]
    [InlineData (KeyCode.F6 | KeyCode.CtrlMask, "\u001B[17;5~")]
    [InlineData (KeyCode.F12 | KeyCode.CtrlMask, "\u001B[24;5~")]
    // Ctrl+Shift modifier (modifier = 6)
    [InlineData (KeyCode.F1 | KeyCode.CtrlMask | KeyCode.ShiftMask, "\u001B[1;6P")]
    [InlineData (KeyCode.F6 | KeyCode.CtrlMask | KeyCode.ShiftMask, "\u001B[17;6~")]
    [InlineData (KeyCode.F12 | KeyCode.CtrlMask | KeyCode.ShiftMask, "\u001B[24;6~")]
    // Ctrl+Alt modifier (modifier = 7)
    [InlineData (KeyCode.F6 | KeyCode.CtrlMask | KeyCode.AltMask, "\u001B[17;7~")]
    [InlineData (KeyCode.F12 | KeyCode.CtrlMask | KeyCode.AltMask, "\u001B[24;7~")]
    // Ctrl+Shift+Alt modifier (modifier = 8)
    [InlineData (KeyCode.F6 | KeyCode.CtrlMask | KeyCode.ShiftMask | KeyCode.AltMask, "\u001B[17;8~")]
    [InlineData (KeyCode.F12 | KeyCode.CtrlMask | KeyCode.ShiftMask | KeyCode.AltMask, "\u001B[24;8~")]
    public void Encode_FunctionKeysWithModifiers_ProducesCorrectSequence (KeyCode keyCode, string expectedSequence)
    {
        // Arrange
        Key key = new (keyCode);

        // Act
        string result = AnsiKeyboardEncoder.Encode (key);

        // Assert
        _output.WriteLine ($"KeyCode: {keyCode} → {result.Replace ("\u001B", "ESC")}");
        Assert.Equal (expectedSequence, result);
    }

    [Theory]
    // Shift modifier (modifier = 2)
    [InlineData (KeyCode.CursorUp | KeyCode.ShiftMask, "\u001B[1;2A")]
    [InlineData (KeyCode.CursorDown | KeyCode.ShiftMask, "\u001B[1;2B")]
    [InlineData (KeyCode.CursorRight | KeyCode.ShiftMask, "\u001B[1;2C")]
    [InlineData (KeyCode.CursorLeft | KeyCode.ShiftMask, "\u001B[1;2D")]
    [InlineData (KeyCode.Home | KeyCode.ShiftMask, "\u001B[1;2H")]
    [InlineData (KeyCode.End | KeyCode.ShiftMask, "\u001B[1;2F")]
    // Ctrl modifier (modifier = 5)
    [InlineData (KeyCode.CursorUp | KeyCode.CtrlMask, "\u001B[1;5A")]
    [InlineData (KeyCode.CursorDown | KeyCode.CtrlMask, "\u001B[1;5B")]
    [InlineData (KeyCode.Home | KeyCode.CtrlMask, "\u001B[1;5H")]
    [InlineData (KeyCode.End | KeyCode.CtrlMask, "\u001B[1;5F")]
    // Ctrl+Shift modifier (modifier = 6)
    [InlineData (KeyCode.CursorUp | KeyCode.CtrlMask | KeyCode.ShiftMask, "\u001B[1;6A")]
    [InlineData (KeyCode.Home | KeyCode.CtrlMask | KeyCode.ShiftMask, "\u001B[1;6H")]
    // Ctrl+Alt modifier (modifier = 7)
    [InlineData (KeyCode.CursorUp | KeyCode.CtrlMask | KeyCode.AltMask, "\u001B[1;7A")]
    [InlineData (KeyCode.Home | KeyCode.CtrlMask | KeyCode.AltMask, "\u001B[1;7H")]
    // Ctrl+Shift+Alt modifier (modifier = 8)
    [InlineData (KeyCode.CursorUp | KeyCode.CtrlMask | KeyCode.ShiftMask | KeyCode.AltMask, "\u001B[1;8A")]
    [InlineData (KeyCode.Home | KeyCode.CtrlMask | KeyCode.ShiftMask | KeyCode.AltMask, "\u001B[1;8H")]
    public void Encode_CursorKeysWithModifiers_ProducesCorrectSequence (KeyCode keyCode, string expectedSequence)
    {
        // Arrange
        Key key = new (keyCode);

        // Act
        string result = AnsiKeyboardEncoder.Encode (key);

        // Assert
        _output.WriteLine ($"KeyCode: {keyCode} → {result.Replace ("\u001B", "ESC")}");
        Assert.Equal (expectedSequence, result);
    }

    [Theory]
    // Shift modifier (modifier = 2)
    [InlineData (KeyCode.Insert | KeyCode.ShiftMask, "\u001B[2;2~")]
    [InlineData (KeyCode.Delete | KeyCode.ShiftMask, "\u001B[3;2~")]
    [InlineData (KeyCode.PageUp | KeyCode.ShiftMask, "\u001B[5;2~")]
    [InlineData (KeyCode.PageDown | KeyCode.ShiftMask, "\u001B[6;2~")]
    // Ctrl modifier (modifier = 5)
    [InlineData (KeyCode.Insert | KeyCode.CtrlMask, "\u001B[2;5~")]
    [InlineData (KeyCode.Delete | KeyCode.CtrlMask, "\u001B[3;5~")]
    [InlineData (KeyCode.PageUp | KeyCode.CtrlMask, "\u001B[5;5~")]
    [InlineData (KeyCode.PageDown | KeyCode.CtrlMask, "\u001B[6;5~")]
    // Ctrl+Shift modifier (modifier = 6)
    [InlineData (KeyCode.Delete | KeyCode.CtrlMask | KeyCode.ShiftMask, "\u001B[3;6~")]
    [InlineData (KeyCode.PageUp | KeyCode.CtrlMask | KeyCode.ShiftMask, "\u001B[5;6~")]
    // Ctrl+Alt modifier (modifier = 7)
    [InlineData (KeyCode.Delete | KeyCode.CtrlMask | KeyCode.AltMask, "\u001B[3;7~")]
    [InlineData (KeyCode.PageUp | KeyCode.CtrlMask | KeyCode.AltMask, "\u001B[5;7~")]
    // Ctrl+Shift+Alt modifier (modifier = 8)
    [InlineData (KeyCode.Delete | KeyCode.CtrlMask | KeyCode.ShiftMask | KeyCode.AltMask, "\u001B[3;8~")]
    [InlineData (KeyCode.PageDown | KeyCode.CtrlMask | KeyCode.ShiftMask | KeyCode.AltMask, "\u001B[6;8~")]
    public void Encode_EditingKeysWithModifiers_ProducesCorrectSequence (KeyCode keyCode, string expectedSequence)
    {
        // Arrange
        Key key = new (keyCode);

        // Act
        string result = AnsiKeyboardEncoder.Encode (key);

        // Assert
        _output.WriteLine ($"KeyCode: {keyCode} → {result.Replace ("\u001B", "ESC")}");
        Assert.Equal (expectedSequence, result);
    }

    [Theory]
    [InlineData (KeyCode.Insert, "\u001B[2~")]
    [InlineData (KeyCode.Delete, "\u001B[3~")]
    [InlineData (KeyCode.PageUp, "\u001B[5~")]
    [InlineData (KeyCode.PageDown, "\u001B[6~")]
    public void Encode_EditingKeys_ProducesCorrectSequence (KeyCode keyCode, string expectedSequence)
    {
        // Arrange
        Key key = new (keyCode);

        // Act
        string result = AnsiKeyboardEncoder.Encode (key);

        // Assert
        _output.WriteLine ($"KeyCode: {keyCode} → {result.Replace ("\u001B", "ESC")}");
        Assert.Equal (expectedSequence, result);
    }

    [Theory]
    [InlineData (KeyCode.Tab, "\t")]
    [InlineData (KeyCode.Enter, "\r")]
    [InlineData (KeyCode.Backspace, "\x7F")]
    [InlineData (KeyCode.Esc, "\u001B")]
    public void Encode_SpecialCharacters_ProducesCorrectSequence (KeyCode keyCode, string expectedSequence)
    {
        // Arrange
        Key key = new (keyCode);

        // Act
        string result = AnsiKeyboardEncoder.Encode (key);

        // Assert
        _output.WriteLine ($"KeyCode: {keyCode} → Hex: {BitConverter.ToString (System.Text.Encoding.ASCII.GetBytes (result))}");
        Assert.Equal (expectedSequence, result);
    }

    #endregion

    #region Regular Character Tests

    [Theory]
    [InlineData (KeyCode.A, "a")]
    [InlineData (KeyCode.B, "b")]
    [InlineData (KeyCode.Z, "z")]
    public void Encode_LettersWithoutShift_ProducesLowercase (KeyCode keyCode, string expectedChar)
    {
        // Arrange
        Key key = new (keyCode);

        // Act
        string result = AnsiKeyboardEncoder.Encode (key);

        // Assert
        _output.WriteLine ($"KeyCode: {keyCode} → {result}");
        Assert.Equal (expectedChar, result);
    }

    [Theory]
    [InlineData (KeyCode.A, "A")]
    [InlineData (KeyCode.B, "B")]
    [InlineData (KeyCode.Z, "Z")]
    public void Encode_LettersWithShift_ProducesUppercase (KeyCode keyCode, string expectedChar)
    {
        // Arrange
        Key key = new Key (keyCode).WithShift;

        // Act
        string result = AnsiKeyboardEncoder.Encode (key);

        // Assert
        _output.WriteLine ($"KeyCode: {keyCode} + Shift → {result}");
        Assert.Equal (expectedChar, result);
    }

    #endregion

    #region Modifier Tests

    [Theory]
    [InlineData (KeyCode.A, 1)]   // Ctrl+A = 0x01
    [InlineData (KeyCode.B, 2)]   // Ctrl+B = 0x02
    [InlineData (KeyCode.C, 3)]   // Ctrl+C = 0x03
    [InlineData (KeyCode.Z, 26)]  // Ctrl+Z = 0x1A
    public void Encode_CtrlLetters_ProducesControlCode (KeyCode keyCode, int expectedControlCode)
    {
        // Arrange
        Key key = new Key (keyCode).WithCtrl;

        // Act
        string result = AnsiKeyboardEncoder.Encode (key);

        // Assert
        _output.WriteLine ($"Ctrl+{keyCode} → 0x{(int)result [0]:X2}");
        Assert.Single (result);
        Assert.Equal (expectedControlCode, result [0]);
    }

    [Theory]
    [InlineData (KeyCode.A, "a")]
    [InlineData (KeyCode.B, "b")]
    [InlineData (KeyCode.Z, "z")]
    public void Encode_AltLetters_ProducesEscPrefixed (KeyCode keyCode, string expectedChar)
    {
        // Arrange
        Key key = new Key (keyCode).WithAlt;

        // Act
        string result = AnsiKeyboardEncoder.Encode (key);

        // Assert
        _output.WriteLine ($"Alt+{keyCode} → {result.Replace ("\u001B", "ESC")}");
        Assert.Equal (2, result.Length);
        Assert.Equal ('\u001B', result [0]);
        Assert.Equal (expectedChar, result [1].ToString ());
    }

    [Theory]
    [InlineData (KeyCode.A, 1)]
    [InlineData (KeyCode.C, 3)]
    public void Encode_CtrlAltLetters_ProducesEscPrefixedControlCode (KeyCode keyCode, int expectedControlCode)
    {
        // Arrange
        Key key = new Key (keyCode).WithCtrl.WithAlt;

        // Act
        string result = AnsiKeyboardEncoder.Encode (key);

        // Assert
        _output.WriteLine ($"Ctrl+Alt+{keyCode} → ESC + 0x{(int)result [1]:X2}");
        Assert.Equal (2, result.Length);
        Assert.Equal ('\u001B', result [0]);
        Assert.Equal (expectedControlCode, result [1]);
    }

    [Theory]
    [InlineData (KeyCode.CursorUp)]
    [InlineData (KeyCode.F1)]
    [InlineData (KeyCode.Delete)]
    public void Encode_AltSpecialKeys_ProducesDoubleEscPrefixed (KeyCode keyCode)
    {
        // Arrange
        Key key = new Key (keyCode).WithAlt;
        string baseSequence = AnsiKeyboardEncoder.Encode (new (keyCode));

        // Act
        string result = AnsiKeyboardEncoder.Encode (key);

        // Assert
        _output.WriteLine ($"Alt+{keyCode} → {result.Replace ("\u001B", "ESC")}");
        Assert.StartsWith ("\u001B", result);
        Assert.Equal ($"\u001B{baseSequence}", result);
    }

    #endregion

    #region Round-Trip Tests

    [Theory]
    [InlineData (KeyCode.CursorUp)]
    [InlineData (KeyCode.CursorDown)]
    [InlineData (KeyCode.F1)]
    [InlineData (KeyCode.F5)]
    [InlineData (KeyCode.F12)]
    [InlineData (KeyCode.Delete)]
    [InlineData (KeyCode.Home)]
    [InlineData (KeyCode.End)]
    public void Encode_RoundTrip_MatchesParserOutput (KeyCode keyCode)
    {
        // Arrange
        Key originalKey = new (keyCode);
        AnsiKeyboardParser parser = new ();

        // Act - Encode Key → ANSI
        string ansiSequence = AnsiKeyboardEncoder.Encode (originalKey);
        _output.WriteLine ($"{keyCode} → {ansiSequence.Replace ("\u001B", "ESC")}");

        // Act - Parse ANSI → Key
        AnsiKeyboardParserPattern? pattern = parser.IsKeyboard (ansiSequence);
        Key? parsedKey = pattern?.GetKey (ansiSequence);

        // Assert
        Assert.NotNull (parsedKey);
        Assert.Equal (originalKey.KeyCode, parsedKey.KeyCode);
    }

    [Theory]
    // Function keys with Shift
    [InlineData (KeyCode.F1 | KeyCode.ShiftMask)]
    [InlineData (KeyCode.F6 | KeyCode.ShiftMask)]
    [InlineData (KeyCode.F12 | KeyCode.ShiftMask)]
    // Function keys with Ctrl
    [InlineData (KeyCode.F1 | KeyCode.CtrlMask)]
    [InlineData (KeyCode.F6 | KeyCode.CtrlMask)]
    [InlineData (KeyCode.F12 | KeyCode.CtrlMask)]
    // Function keys with Ctrl+Shift
    [InlineData (KeyCode.F6 | KeyCode.CtrlMask | KeyCode.ShiftMask)]
    [InlineData (KeyCode.F12 | KeyCode.CtrlMask | KeyCode.ShiftMask)]
    // Cursor keys with Shift
    [InlineData (KeyCode.CursorUp | KeyCode.ShiftMask)]
    [InlineData (KeyCode.Home | KeyCode.ShiftMask)]
    [InlineData (KeyCode.End | KeyCode.ShiftMask)]
    // Cursor keys with Ctrl
    [InlineData (KeyCode.CursorUp | KeyCode.CtrlMask)]
    [InlineData (KeyCode.Home | KeyCode.CtrlMask)]
    // Editing keys with Shift
    [InlineData (KeyCode.Delete | KeyCode.ShiftMask)]
    [InlineData (KeyCode.PageUp | KeyCode.ShiftMask)]
    // Editing keys with Ctrl
    [InlineData (KeyCode.Delete | KeyCode.CtrlMask)]
    [InlineData (KeyCode.PageDown | KeyCode.CtrlMask)]
    // Complex combinations
    [InlineData (KeyCode.F6 | KeyCode.CtrlMask | KeyCode.AltMask)]
    [InlineData (KeyCode.Delete | KeyCode.CtrlMask | KeyCode.ShiftMask)]
    [InlineData (KeyCode.CursorUp | KeyCode.CtrlMask | KeyCode.ShiftMask | KeyCode.AltMask)]
    public void Encode_RoundTripWithModifiers_MatchesParserOutput (KeyCode keyCode)
    {
        // Arrange
        Key originalKey = new (keyCode);
        AnsiKeyboardParser parser = new ();

        // Act - Encode Key → ANSI
        string ansiSequence = AnsiKeyboardEncoder.Encode (originalKey);
        _output.WriteLine ($"{keyCode} → {ansiSequence.Replace ("\u001B", "ESC")}");

        // Act - Parse ANSI → Key
        AnsiKeyboardParserPattern? pattern = parser.IsKeyboard (ansiSequence);
        Key? parsedKey = pattern?.GetKey (ansiSequence);

        // Assert
        Assert.NotNull (parsedKey);
        Assert.Equal (originalKey.KeyCode, parsedKey.KeyCode);
    }

    #endregion
}
