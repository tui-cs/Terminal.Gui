// Copilot - Claude Sonnet 4

using System.Text;

namespace DriverTests.AnsiHandling;

/// <summary>
///     Tests that verify the ANSI parser guards against unbounded memory growth
///     from malformed or malicious unterminated escape sequences.
/// </summary>
[Collection ("Driver Tests")]
public class AnsiParserSecurityTests
{
    [Fact]
    public void Parser_ReleasesHeldContent_WhenMaxLengthExceeded_CSI ()
    {
        AnsiResponseParser parser = new (new SystemTimeProvider ());

        // Build an unterminated CSI sequence longer than the max held length.
        // CSI starts with ESC [ then we fill with parameter bytes (digits/semicolons) without a terminator.
        StringBuilder input = new ();
        input.Append ("\x1b["); // CSI introducer

        // Fill with parameter bytes beyond the max held length
        int fillLength = AnsiResponseParserBase.MaxHeldLength + 100;

        for (var i = 0; i < fillLength; i++)
        {
            input.Append ('0');
        }

        // Process the input — should not throw and should not accumulate unbounded memory
        string released = parser.ProcessInput (input.ToString ());

        // The parser should have released the held content once it exceeded the limit
        // The released output should contain the original characters (released back as output)
        Assert.True (released.Length > 0);

        // Parser should be back in Normal state after release
        Assert.Equal (AnsiResponseParserState.Normal, parser.State);
    }

    [Fact]
    public void Parser_ReleasesHeldContent_WhenMaxLengthExceeded_OSC ()
    {
        AnsiResponseParser parser = new (new SystemTimeProvider ());

        // Build an unterminated OSC sequence longer than the max held length.
        // OSC starts with ESC ] then we fill with arbitrary content without a terminator (BEL or ST).
        StringBuilder input = new ();
        input.Append ("\x1b]"); // OSC introducer

        int fillLength = AnsiResponseParserBase.MaxHeldLength + 100;

        for (var i = 0; i < fillLength; i++)
        {
            input.Append ('x');
        }

        string released = parser.ProcessInput (input.ToString ());

        // The parser should have released the held content once it exceeded the limit
        Assert.True (released.Length > 0);
        Assert.Equal (AnsiResponseParserState.Normal, parser.State);
    }

    [Fact]
    public void Parser_NormalSequences_StillWork_AfterOverflow ()
    {
        AnsiResponseParser parser = new (new SystemTimeProvider ()) { HandleMouse = true };

        // First, overflow the parser with an unterminated sequence
        StringBuilder overflow = new ();
        overflow.Append ("\x1b[");

        for (var i = 0; i < AnsiResponseParserBase.MaxHeldLength + 10; i++)
        {
            overflow.Append ('0');
        }

        parser.ProcessInput (overflow.ToString ());

        // Now send a valid mouse sequence — it should still be detected
        List<Mouse> mouseEvents = [];
        parser.Mouse += (_, e) => mouseEvents.Add (e);

        parser.ProcessInput ("\x1b[<0;10;20M");

        Assert.Single (mouseEvents);
    }

    [Fact]
    public void MouseParser_RejectsOversizedInput_IsMouse ()
    {
        AnsiMouseParser parser = new ();

        // Normal mouse sequence
        Assert.True (parser.IsMouse ("\x1b[<0;10;20M"));

        // Oversized input should be rejected
        string oversized = "\x1b[<" + new string ('0', AnsiMouseParser.MaxMouseSequenceLength + 10) + "M";
        Assert.False (parser.IsMouse (oversized));
    }

    [Fact]
    public void MouseParser_RejectsOversizedInput_ProcessMouseInput ()
    {
        AnsiMouseParser parser = new ();

        // Normal mouse sequence should work
        Mouse? result = parser.ProcessMouseInput ("\x1b[<0;10;20M");
        Assert.NotNull (result);

        // Oversized input should return null
        string oversized = "\x1b[<" + new string ('0', AnsiMouseParser.MaxMouseSequenceLength + 10) + ";10;20M";
        result = parser.ProcessMouseInput (oversized);
        Assert.Null (result);
    }

    [Fact]
    public void MouseParser_RejectsNull_ProcessMouseInput ()
    {
        AnsiMouseParser parser = new ();
        Mouse? result = parser.ProcessMouseInput (null);
        Assert.Null (result);
    }

    [Fact]
    public void KeyboardParser_RejectsOversizedInput ()
    {
        AnsiKeyboardParser parser = new ();

        // Normal keyboard sequence should work
        AnsiKeyboardParserPattern? result = parser.IsKeyboard ("\x1b[A");
        Assert.NotNull (result);

        // Oversized input should return null
        string oversized = "\x1b[" + new string ('1', AnsiKeyboardParser.MaxKeyboardSequenceLength + 10) + "A";
        result = parser.IsKeyboard (oversized);
        Assert.Null (result);
    }

    [Fact]
    public void KeyboardParser_RejectsNull ()
    {
        AnsiKeyboardParser parser = new ();
        AnsiKeyboardParserPattern? result = parser.IsKeyboard (null);
        Assert.Null (result);
    }

    [Fact]
    public void Parser_GenericVariant_ReleasesHeldContent_WhenMaxLengthExceeded ()
    {
        AnsiResponseParser<int> parser = new (new SystemTimeProvider ());

        // Build unterminated CSI sequence
        List<Tuple<char, int>> input = [];
        input.Add (Tuple.Create ('\x1b', 0));
        input.Add (Tuple.Create ('[', 1));

        int fillLength = AnsiResponseParserBase.MaxHeldLength + 100;

        for (var i = 0; i < fillLength; i++)
        {
            input.Add (Tuple.Create ('0', i + 2));
        }

        IEnumerable<Tuple<char, int>> released = parser.ProcessInput (input.ToArray ());

        // Should release content and not accumulate unbounded memory
        Assert.True (released.Any ());
        Assert.Equal (AnsiResponseParserState.Normal, parser.State);
    }
}
