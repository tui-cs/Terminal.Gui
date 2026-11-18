#nullable enable
namespace DriverTests.AnsiHandling;

[Collection ("Driver Tests")]
public class AnsiKeyboardParserTests
{
    private readonly AnsiKeyboardParser _parser = new ();

    public static IEnumerable<object? []> GetKeyboardTestData ()
    {
        // Test data for various ANSI escape sequences and their expected Key values
        yield return ["\u001b[A", Key.CursorUp];
        yield return ["\u001b[B", Key.CursorDown];
        yield return ["\u001b[C", Key.CursorRight];
        yield return ["\u001b[D", Key.CursorLeft];

        // Valid inputs with modifiers
        yield return ["\u001b[1;2A", Key.CursorUp.WithShift];
        yield return ["\u001b[1;3A", Key.CursorUp.WithAlt];
        yield return ["\u001b[1;4A", Key.CursorUp.WithAlt.WithShift];
        yield return ["\u001b[1;5A", Key.CursorUp.WithCtrl];
        yield return ["\u001b[1;6A", Key.CursorUp.WithCtrl.WithShift];
        yield return ["\u001b[1;7A", Key.CursorUp.WithCtrl.WithAlt];
        yield return ["\u001b[1;8A", Key.CursorUp.WithCtrl.WithAlt.WithShift];

        yield return ["\u001b[1;2B", Key.CursorDown.WithShift];
        yield return ["\u001b[1;3B", Key.CursorDown.WithAlt];
        yield return ["\u001b[1;4B", Key.CursorDown.WithAlt.WithShift];
        yield return ["\u001b[1;5B", Key.CursorDown.WithCtrl];
        yield return ["\u001b[1;6B", Key.CursorDown.WithCtrl.WithShift];
        yield return ["\u001b[1;7B", Key.CursorDown.WithCtrl.WithAlt];
        yield return ["\u001b[1;8B", Key.CursorDown.WithCtrl.WithAlt.WithShift];

        yield return ["\u001b[1;2C", Key.CursorRight.WithShift];
        yield return ["\u001b[1;3C", Key.CursorRight.WithAlt];
        yield return ["\u001b[1;4C", Key.CursorRight.WithAlt.WithShift];
        yield return ["\u001b[1;5C", Key.CursorRight.WithCtrl];
        yield return ["\u001b[1;6C", Key.CursorRight.WithCtrl.WithShift];
        yield return ["\u001b[1;7C", Key.CursorRight.WithCtrl.WithAlt];
        yield return ["\u001b[1;8C", Key.CursorRight.WithCtrl.WithAlt.WithShift];

        yield return ["\u001b[1;2D", Key.CursorLeft.WithShift];
        yield return ["\u001b[1;3D", Key.CursorLeft.WithAlt];
        yield return ["\u001b[1;4D", Key.CursorLeft.WithAlt.WithShift];
        yield return ["\u001b[1;5D", Key.CursorLeft.WithCtrl];
        yield return ["\u001b[1;6D", Key.CursorLeft.WithCtrl.WithShift];
        yield return ["\u001b[1;7D", Key.CursorLeft.WithCtrl.WithAlt];
        yield return ["\u001b[1;8D", Key.CursorLeft.WithCtrl.WithAlt.WithShift];

        // Invalid inputs
        yield return ["\u001b[invalid", null!];
        yield return ["\u001b[1", null!];
        yield return ["\u001b[AB", null!];
        yield return ["\u001b[;A", null!];

        // Test data for various ANSI escape sequences and their expected Key values
        yield return ["\u001b[3;5~", Key.Delete.WithCtrl];

        // Additional special keys
        yield return ["\u001b[H", Key.Home];
        yield return ["\u001b[F", Key.End];
        yield return ["\u001b[2~", Key.InsertChar];
        yield return ["\u001b[5~", Key.PageUp];
        yield return ["\u001b[6~", Key.PageDown];
        yield return ["\u001b[Z", Key.Tab.WithShift];

        // Home, End with modifiers
        yield return ["\u001b[1;2H", Key.Home.WithShift];
        yield return ["\u001b[1;3H", Key.Home.WithAlt];
        yield return ["\u001b[1;5F", Key.End.WithCtrl];

        // Insert with modifiers
        yield return ["\u001b[2;2~", Key.InsertChar.WithShift];
        yield return ["\u001b[2;3~", Key.InsertChar.WithAlt];
        yield return ["\u001b[2;5~", Key.InsertChar.WithCtrl];

        // PageUp/PageDown with modifiers
        yield return ["\u001b[5;2~", Key.PageUp.WithShift];
        yield return ["\u001b[6;3~", Key.PageDown.WithAlt];
        yield return ["\u001b[6;5~", Key.PageDown.WithCtrl];

        // Function keys F1-F4 (common ESC O sequences)
        yield return ["\u001bOP", Key.F1];
        yield return ["\u001bOQ", Key.F2];
        yield return ["\u001bOR", Key.F3];
        yield return ["\u001bOS", Key.F4];

        // Extended function keys F1-F12 with CSI sequences
        yield return ["\u001b[11~", Key.F1];
        yield return ["\u001b[12~", Key.F2];
        yield return ["\u001b[13~", Key.F3];
        yield return ["\u001b[14~", Key.F4];
        yield return ["\u001b[15~", Key.F5];
        yield return ["\u001b[17~", Key.F6];
        yield return ["\u001b[18~", Key.F7];
        yield return ["\u001b[19~", Key.F8];
        yield return ["\u001b[20~", Key.F9];
        yield return ["\u001b[21~", Key.F10];
        yield return ["\u001b[23~", Key.F11];
        yield return ["\u001b[24~", Key.F12];

        // Function keys with modifiers
        yield return ["\u001b[1;2P", Key.F1.WithShift];
        yield return ["\u001b[1;3Q", Key.F2.WithAlt];
        yield return ["\u001b[1;5R", Key.F3.WithCtrl];

        // Kitty CSI u printable keys
        yield return ["\u001b[97u", Key.A];
        yield return ["\u001b[65;2u", Key.A.WithShift];
        yield return ["\u001b[97;3u", Key.A.WithAlt];
        yield return ["\u001b[97;5u", Key.A.WithCtrl];
        yield return ["\u001b[49;6u", Key.D1.WithCtrl.WithShift];

        // Kitty CSI u special keys
        yield return ["\u001b[9u", Key.Tab];
        yield return ["\u001b[13u", Key.Enter];
        yield return ["\u001b[127u", Key.Backspace];
        yield return ["\u001b[57344u", Key.CursorUp];
        yield return ["\u001b[57345;3u", Key.CursorDown.WithAlt];
        yield return ["\u001b[57346;5u", Key.CursorLeft.WithCtrl];
        yield return ["\u001b[57347;2u", Key.CursorRight.WithShift];
        yield return ["\u001b[57348u", Key.PageUp];
        yield return ["\u001b[57349;6u", Key.PageDown.WithCtrl.WithShift];
        yield return ["\u001b[57350u", Key.Home];
        yield return ["\u001b[57351;4u", Key.End.WithAlt.WithShift];
        yield return ["\u001b[57352u", Key.InsertChar];
        yield return ["\u001b[57353;5u", Key.Delete.WithCtrl];
        yield return ["\u001b[57354u", Key.Clear];
        yield return ["\u001b[57364u", Key.F1];
        yield return ["\u001b[57368;2u", Key.F5.WithShift];
        yield return ["\u001b[57375;5u", Key.F12.WithCtrl];
        yield return ["\u001b[57376u", Key.F13];
        yield return ["\u001b[57387;3u", Key.F24.WithAlt];

        // Malformed kitty sequences
        yield return ["\u001b[?31u", null!];
        yield return ["\u001b[notkittyu", null!];

        // Keys with Alt modifiers
        yield return ["\u001ba", Key.A.WithAlt, true];
        yield return ["\u001bA", Key.A.WithShift.WithAlt, true];
        yield return ["\u001b1", Key.D1.WithAlt, true];

        // Keys with Ctrl and Alt modifiers
        yield return ["\u001b\u0001", Key.A.WithCtrl.WithAlt, true];
        yield return ["\u001b\u001a", Key.Z.WithCtrl.WithAlt, true];

        // Keys with Ctrl, Shift and Alt modifiers
        yield return ["\u001b\u001f", Key.D7.WithCtrl.WithShift.WithAlt, true];
    }

    // Consolidated test for all keyboard events (e.g., arrow keys)
    [Theory]
    [MemberData (nameof (GetKeyboardTestData))]
    public void ProcessKeyboardInput_ReturnsCorrectKey (string? input, Key? expectedKey, bool isLastMinute = false)
    {
        // Act
        Key? result = _parser.IsKeyboard (input, isLastMinute)?.GetKey (input);

        // Assert
        Assert.Equal (expectedKey, result); // Verify the returned key matches the expected one
    }
}
