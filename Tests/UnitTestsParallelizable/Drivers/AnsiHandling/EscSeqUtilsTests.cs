using System.Text;

// ReSharper disable HeuristicUnreachableCode

namespace DriverTests.AnsiHandling;

[Collection ("Driver Tests")]
public class EscSeqUtilsTests
{
    [Fact]
    public void Defaults_Values ()
    {
        Assert.Equal ('\x1b', EscSeqUtils.KeyEsc);
        Assert.Equal ("\x1b[", EscSeqUtils.CSI);
        Assert.Equal ("\x1b[?1003h", EscSeqUtils.CSI_EnableAnyEventMouse);
        Assert.Equal ("\x1b[?1006h", EscSeqUtils.CSI_EnableSgrExtModeMouse);
        Assert.Equal ("\x1b[?1015h", EscSeqUtils.CSI_EnableUrxvtExtModeMouse);
        Assert.Equal ("\x1b[?1003l", EscSeqUtils.CSI_DisableAnyEventMouse);
        Assert.Equal ("\x1b[?1006l", EscSeqUtils.CSI_DisableSgrExtModeMouse);
        Assert.Equal ("\x1b[?1015l", EscSeqUtils.CSI_DisableUrxvtExtModeMouse);
        Assert.Equal ("\x1b[?1003h\x1b[?1015h\u001b[?1006h", EscSeqUtils.CSI_EnableMouseEvents);
        Assert.Equal ("\x1b[?1003l\x1b[?1015l\u001b[?1006l", EscSeqUtils.CSI_DisableMouseEvents);
        Assert.Equal ($"{EscSeqUtils.CSI}6n", EscSeqUtils.CSI_RequestCursorPositionReport.Request);
        Assert.Equal ("R", EscSeqUtils.CSI_RequestCursorPositionReport.Terminator);

        // Claude - Opus 4.7
        Assert.Equal ("\x1b[?2004h", EscSeqUtils.CSI_EnableBracketedPaste);
        Assert.Equal ("\x1b[?2004l", EscSeqUtils.CSI_DisableBracketedPaste);
        Assert.Equal ("\x1b[200~", EscSeqUtils.CSI_BracketedPasteStart);
        Assert.Equal ("\x1b[201~", EscSeqUtils.CSI_BracketedPasteEnd);
    }

    [Fact]
    public void GetConsoleInputKey_ConsoleKeyInfo ()
    {
        var cki = new ConsoleKeyInfo ('r', 0, false, false, false);
        var expectedCki = new ConsoleKeyInfo ('r', ConsoleKey.R, false, false, false);
        Assert.Equal (expectedCki.ToString(), EscSeqUtils.MapConsoleKeyInfo (cki).ToString ());

        cki = new ('r', 0, true, false, false);
        expectedCki = new ('r', ConsoleKey.R, true, false, false);
        Assert.Equal (expectedCki.ToString(), EscSeqUtils.MapConsoleKeyInfo (cki).ToString ());

        cki = new ('r', 0, false, true, false);
        expectedCki = new ('r', ConsoleKey.R, false, true, false);
        Assert.Equal (expectedCki.ToString(), EscSeqUtils.MapConsoleKeyInfo (cki).ToString ());

        cki = new ('r', 0, false, false, true);
        expectedCki = new ('r', ConsoleKey.R, false, false, true);
        Assert.Equal (expectedCki.ToString(), EscSeqUtils.MapConsoleKeyInfo(cki).ToString());

        cki = new ('r', 0, true, true, false);
        expectedCki = new ('r', ConsoleKey.R, true, true, false);
        Assert.Equal (expectedCki.ToString(), EscSeqUtils.MapConsoleKeyInfo (cki).ToString ());

        cki = new ('r', 0, false, true, true);
        expectedCki = new ('r', ConsoleKey.R, false, true, true);
        Assert.Equal (expectedCki.ToString(), EscSeqUtils.MapConsoleKeyInfo(cki).ToString());

        cki = new ('r', 0, true, true, true);
        expectedCki = new ('r', ConsoleKey.R, true, true, true);
        Assert.Equal (expectedCki.ToString(), EscSeqUtils.MapConsoleKeyInfo(cki).ToString());

        cki = new ('\u0012', 0, false, false, false);
        expectedCki = new ('\u0012', ConsoleKey.R, false, false, true);
        Assert.Equal (expectedCki.ToString(), EscSeqUtils.MapConsoleKeyInfo(cki).ToString());

        cki = new ('\0', (ConsoleKey)64, false, false, true);
        expectedCki = new ('\0', ConsoleKey.Spacebar, false, false, true);
        Assert.Equal (expectedCki.ToString(), EscSeqUtils.MapConsoleKeyInfo(cki).ToString());

        cki = new ('\r', 0, false, false, false);
        expectedCki = new ('\r', ConsoleKey.Enter, false, false, false);
        Assert.Equal (expectedCki.ToString(), EscSeqUtils.MapConsoleKeyInfo(cki).ToString());

        cki = new ('\u007f', 0, false, false, false);
        expectedCki = new ('\u007f', ConsoleKey.Backspace, false, false, false);
        Assert.Equal (expectedCki.ToString(), EscSeqUtils.MapConsoleKeyInfo(cki).ToString());

        cki = new ('R', 0, false, false, false);
        expectedCki = new ('R', ConsoleKey.R, true, false, false);
        Assert.Equal (expectedCki.ToString(), EscSeqUtils.MapConsoleKeyInfo(cki).ToString());
    }

    [Theory]
    [InlineData (0, 0, $"{EscSeqUtils.CSI}0;0H")]
    [InlineData (int.MaxValue, int.MaxValue, $"{EscSeqUtils.CSI}2147483647;2147483647H")]
    [InlineData (int.MinValue, int.MinValue, $"{EscSeqUtils.CSI}-2147483648;-2147483648H")]
    public void CSI_WriteCursorPosition_ReturnsCorrectEscSeq (int row, int col, string expected)
    {
        StringBuilder builder = new();
        using StringWriter writer = new(builder);

        EscSeqUtils.CSI_WriteCursorPosition (writer, row, col);

        string actual = builder.ToString();
        Assert.Equal (expected, actual);
    }

    [Theory]
    [InlineData (-1, $"{EscSeqUtils.OSC}9;4;1;0{EscSeqUtils.ST}")]
    [InlineData (50, $"{EscSeqUtils.OSC}9;4;1;50{EscSeqUtils.ST}")]
    [InlineData (101, $"{EscSeqUtils.OSC}9;4;1;100{EscSeqUtils.ST}")]
    public void OSC_SetProgressValue_Clamps_Progress (int progress, string expected)
    {
        Assert.Equal (expected, EscSeqUtils.OSC_SetProgressValue (progress));
    }

    [Fact]
    public void OSC_ProgressHelpers_ReturnExpectedSequences ()
    {
        Assert.Equal ($"{EscSeqUtils.OSC}9;4;0;0{EscSeqUtils.ST}", EscSeqUtils.OSC_ClearProgress ());
        Assert.Equal ($"{EscSeqUtils.OSC}9;4;2;10{EscSeqUtils.ST}", EscSeqUtils.OSC_SetProgressError (10));
        Assert.Equal ($"{EscSeqUtils.OSC}9;4;3;0{EscSeqUtils.ST}", EscSeqUtils.OSC_SetProgressIndeterminate ());
        Assert.Equal ($"{EscSeqUtils.OSC}9;4;4;25{EscSeqUtils.ST}", EscSeqUtils.OSC_SetProgressPaused (25));
    }

    // Copilot
    [Theory]
    [InlineData (-1, "title", $"{EscSeqUtils.OSC}0;title{EscSeqUtils.ST}")]
    [InlineData (1, "icon", $"{EscSeqUtils.OSC}1;icon{EscSeqUtils.ST}")]
    [InlineData (2, "window", $"{EscSeqUtils.OSC}2;window{EscSeqUtils.ST}")]
    [InlineData (99, "title", $"{EscSeqUtils.OSC}2;title{EscSeqUtils.ST}")]
    [InlineData (0, "\u001bA\u0007B", $"{EscSeqUtils.OSC}0;AB{EscSeqUtils.ST}")]
    public void OSC_SetWindowTitle_ReturnsExpectedSequence (int mode, string title, string expected)
    {
        Assert.Equal (expected, EscSeqUtils.OSC_SetWindowTitle (title, mode));
    }

    [Theory]
    [InlineData ('\u001B', KeyCode.Esc)]
    [InlineData ('\r', KeyCode.Enter)]
    [InlineData ('1', KeyCode.D1)]
    [InlineData ('!', (KeyCode)'!')]
    [InlineData ('a', KeyCode.A)]
    [InlineData ('A', KeyCode.A | KeyCode.ShiftMask)]
    public void MapChar_Returns_Modifiers_If_Needed (char ch, KeyCode keyCode)
    {
        ConsoleKeyInfo cki = EscSeqUtils.MapChar (ch);
        Key key = EscSeqUtils.MapKey (cki);
        Key expectedKey = keyCode;

        Assert.Equal (key, expectedKey);
    }
}
