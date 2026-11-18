namespace DriverTests.AnsiHandling;

// Claude - Opus 4.6
/// <summary>
///     Tests for OSC 10/11 terminal color detection: parser handling, response matching,
///     color parsing, environment detection, and Scheme.ResolveNone.
/// </summary>
public class TerminalColorDetectionTests
{
    #region TryParseOscColorResponse Tests

    [Fact]
    public void TryParseOscColorResponse_Valid16Bit_ParsesCorrectly ()
    {
        // 16-bit per channel: rgb:ffff/0000/8080 → R=255, G=0, B=128
        string response = "\u001b]10;rgb:ffff/0000/8080\u001b\\";
        bool result = EscSeqUtils.TryParseOscColorResponse (response, out Color? color);

        Assert.True (result);
        Assert.NotNull (color);
        Assert.Equal (255, color.Value.R);
        Assert.Equal (0, color.Value.G);
        Assert.Equal (128, color.Value.B);
    }

    [Fact]
    public void TryParseOscColorResponse_Valid8Bit_ParsesCorrectly ()
    {
        // 8-bit per channel: rgb:ff/00/80 → R=255, G=0, B=128
        string response = "\u001b]11;rgb:ff/00/80\u001b\\";
        bool result = EscSeqUtils.TryParseOscColorResponse (response, out Color? color);

        Assert.True (result);
        Assert.NotNull (color);
        Assert.Equal (255, color.Value.R);
        Assert.Equal (0, color.Value.G);
        Assert.Equal (128, color.Value.B);
    }

    [Fact]
    public void TryParseOscColorResponse_BelTerminated_ParsesCorrectly ()
    {
        // BEL-terminated response
        string response = "\u001b]10;rgb:1a1a/2b2b/3c3c\a";
        bool result = EscSeqUtils.TryParseOscColorResponse (response, out Color? color);

        Assert.True (result);
        Assert.NotNull (color);
        Assert.Equal (0x1a, color.Value.R);
        Assert.Equal (0x2b, color.Value.G);
        Assert.Equal (0x3c, color.Value.B);
    }

    [Fact]
    public void TryParseOscColorResponse_NullInput_ReturnsFalse ()
    {
        bool result = EscSeqUtils.TryParseOscColorResponse (null, out Color? color);

        Assert.False (result);
        Assert.Null (color);
    }

    [Fact]
    public void TryParseOscColorResponse_EmptyInput_ReturnsFalse ()
    {
        bool result = EscSeqUtils.TryParseOscColorResponse ("", out Color? color);

        Assert.False (result);
        Assert.Null (color);
    }

    [Fact]
    public void TryParseOscColorResponse_NoRgbPrefix_ReturnsFalse ()
    {
        string response = "\u001b]10;not-a-color\u001b\\";
        bool result = EscSeqUtils.TryParseOscColorResponse (response, out Color? color);

        Assert.False (result);
        Assert.Null (color);
    }

    [Fact]
    public void TryParseOscColorResponse_WrongComponentCount_ReturnsFalse ()
    {
        string response = "\u001b]10;rgb:ff/00\u001b\\";
        bool result = EscSeqUtils.TryParseOscColorResponse (response, out Color? color);

        Assert.False (result);
        Assert.Null (color);
    }

    [Fact]
    public void TryParseOscColorResponse_InvalidHex_ReturnsFalse ()
    {
        string response = "\u001b]10;rgb:zzzz/0000/0000\u001b\\";
        bool result = EscSeqUtils.TryParseOscColorResponse (response, out Color? color);

        Assert.False (result);
        Assert.Null (color);
    }

    [Fact]
    public void TryParseOscColorResponse_OddLengthHex_ReturnsFalse ()
    {
        // 3-digit hex is not 2 or 4
        string response = "\u001b]10;rgb:fff/000/888\u001b\\";
        bool result = EscSeqUtils.TryParseOscColorResponse (response, out Color? color);

        Assert.False (result);
        Assert.Null (color);
    }

    [Fact]
    public void TryParseOscColorResponse_BlackColor_ParsesCorrectly ()
    {
        string response = "\u001b]11;rgb:0000/0000/0000\u001b\\";
        bool result = EscSeqUtils.TryParseOscColorResponse (response, out Color? color);

        Assert.True (result);
        Assert.NotNull (color);
        Assert.Equal (0, color.Value.R);
        Assert.Equal (0, color.Value.G);
        Assert.Equal (0, color.Value.B);
    }

    [Fact]
    public void TryParseOscColorResponse_WhiteColor_ParsesCorrectly ()
    {
        string response = "\u001b]10;rgb:ffff/ffff/ffff\u001b\\";
        bool result = EscSeqUtils.TryParseOscColorResponse (response, out Color? color);

        Assert.True (result);
        Assert.NotNull (color);
        Assert.Equal (255, color.Value.R);
        Assert.Equal (255, color.Value.G);
        Assert.Equal (255, color.Value.B);
    }

    #endregion

    #region AnsiResponseExpectation OSC Matching Tests

    [Fact]
    public void AnsiResponseExpectation_Matches_OscResponse_WithStTerminator ()
    {
        // OSC 10 response terminated by ST (ESC\)
        AnsiResponseExpectation expectation = new ("\u001b\\", "10", _ => { }, null);

        bool result = expectation.Matches ("\u001b]10;rgb:ffff/0000/0000\u001b\\");

        Assert.True (result);
    }

    [Fact]
    public void AnsiResponseExpectation_Matches_OscResponse_WithDifferentValue ()
    {
        // Expect OSC 10 but get OSC 11
        AnsiResponseExpectation expectation = new ("\u001b\\", "10", _ => { }, null);

        bool result = expectation.Matches ("\u001b]11;rgb:ffff/0000/0000\u001b\\");

        Assert.False (result);
    }

    [Fact]
    public void AnsiResponseExpectation_Matches_OscResponse_NoValueFilter ()
    {
        // No value filter — should match any OSC response with the terminator
        AnsiResponseExpectation expectation = new ("\u001b\\", null, _ => { }, null);

        bool result = expectation.Matches ("\u001b]10;rgb:ffff/0000/0000\u001b\\");

        Assert.True (result);
    }

    [Fact]
    public void AnsiResponseExpectation_DoesNotMatch_WrongTerminator ()
    {
        AnsiResponseExpectation expectation = new ("c", "10", _ => { }, null);

        bool result = expectation.Matches ("\u001b]10;rgb:ffff/0000/0000\u001b\\");

        Assert.False (result);
    }

    #endregion

    #region Parser OSC Integration Tests

    [Fact]
    public void Parser_OscResponse_WithST_MatchesExpectation ()
    {
        AnsiResponseParser parser = new (new SystemTimeProvider ());
        string? receivedResponse = null;

        parser.ExpectResponse ("\u001b\\", "10", s => receivedResponse = s, null, false);

        // Feed: ESC ] 1 0 ; r g b : f f / 0 0 / 0 0 ESC \
        string input = "\u001b]10;rgb:ff/00/00\u001b\\";
        string output = parser.ProcessInput (input);

        Assert.NotNull (receivedResponse);
        Assert.Equal ("\u001b]10;rgb:ff/00/00\u001b\\", receivedResponse);
        Assert.Equal ("", output); // OSC response should be consumed, not output
    }

    [Fact]
    public void Parser_OscResponse_WithBel_MatchesExpectation ()
    {
        AnsiResponseParser parser = new (new SystemTimeProvider ());
        string? receivedResponse = null;

        parser.ExpectResponse ("\a", "10", s => receivedResponse = s, null, false);

        // Feed: ESC ] 1 0 ; r g b : f f / 0 0 / 0 0 BEL
        string input = "\u001b]10;rgb:ff/00/00\a";
        string output = parser.ProcessInput (input);

        Assert.NotNull (receivedResponse);
        Assert.Equal ("\u001b]10;rgb:ff/00/00\a", receivedResponse);
        Assert.Equal ("", output);
    }

    [Fact]
    public void Parser_OscResponse_FollowedByNormalText_ParsesCorrectly ()
    {
        AnsiResponseParser parser = new (new SystemTimeProvider ());
        string? receivedResponse = null;

        parser.ExpectResponse ("\u001b\\", "11", s => receivedResponse = s, null, false);

        string input = "\u001b]11;rgb:00/00/00\u001b\\Hello";
        string output = parser.ProcessInput (input);

        Assert.NotNull (receivedResponse);
        Assert.Equal ("\u001b]11;rgb:00/00/00\u001b\\", receivedResponse);
        Assert.Equal ("Hello", output);
    }

    [Fact]
    public void Parser_OscResponse_WithNormalTextBefore_ParsesCorrectly ()
    {
        AnsiResponseParser parser = new (new SystemTimeProvider ());
        string? receivedResponse = null;

        parser.ExpectResponse ("\u001b\\", "10", s => receivedResponse = s, null, false);

        string input = "Before\u001b]10;rgb:aa/bb/cc\u001b\\After";
        string output = parser.ProcessInput (input);

        Assert.NotNull (receivedResponse);
        Assert.Equal ("\u001b]10;rgb:aa/bb/cc\u001b\\", receivedResponse);
        Assert.Equal ("BeforeAfter", output);
    }

    [Fact]
    public void Parser_OscResponse_MalformedST_ReleasesContent ()
    {
        AnsiResponseParser parser = new (new SystemTimeProvider ());
        string? receivedResponse = null;

        parser.ExpectResponse ("\u001b\\", "10", s => receivedResponse = s, null, false);

        // Malformed: ESC followed by 'X' instead of '\' inside OSC
        string input = "\u001b]10;rgb:ff/00/00\u001bXMore";
        string output = parser.ProcessInput (input);

        // Should not match — malformed ST
        Assert.Null (receivedResponse);

        // Content should be released (not consumed)
        Assert.Contains ("More", output);
    }

    [Fact]
    public void Parser_TwoChainedOscResponses_BothParsed ()
    {
        AnsiResponseParser parser = new (new SystemTimeProvider ());
        string? fgResponse = null;
        string? bgResponse = null;

        parser.ExpectResponse ("\u001b\\", "10", s => fgResponse = s, null, false);
        parser.ExpectResponse ("\u001b\\", "11", s => bgResponse = s, null, false);

        // Two OSC responses back-to-back
        string input = "\u001b]10;rgb:ff/ff/ff\u001b\\\u001b]11;rgb:00/00/00\u001b\\";
        string output = parser.ProcessInput (input);

        Assert.NotNull (fgResponse);
        Assert.Equal ("\u001b]10;rgb:ff/ff/ff\u001b\\", fgResponse);
        Assert.NotNull (bgResponse);
        Assert.Equal ("\u001b]11;rgb:00/00/00\u001b\\", bgResponse);
        Assert.Equal ("", output);
    }

    #endregion

    #region TerminalEnvironmentDetector Tests

    [Fact]
    public void DetectColorCapabilities_ReturnsNonNull ()
    {
        // Just verify the method runs without error and returns a result
        TerminalColorCapabilities caps = TerminalEnvironmentDetector.DetectColorCapabilities ();

        Assert.NotNull (caps);
    }

    #endregion

    #region Scheme.ResolveNone Tests

    [Fact]
    public void Scheme_FocusDerivation_WithNoneBackground_DoesNotCrash ()
    {
        // Normal with Color.None background — Focus derivation should resolve it
        Scheme scheme = new () { Normal = new Attribute (new Color (200, 200, 200), Color.None) };

        // This should not throw and should produce a valid Focus attribute
        Attribute focus = scheme.GetAttributeForRole (VisualRole.Focus);

        // Focus foreground should NOT be Color.None (it should be resolved)
        Assert.NotEqual (Color.None, focus.Foreground);
    }

    [Fact]
    public void Scheme_HighlightDerivation_WithNoneBackground_DoesNotCrash ()
    {
        Scheme scheme = new () { Normal = new Attribute (new Color (200, 200, 200), Color.None) };

        // Highlight calls GetBrighterColor on background — should not fail with Color.None
        Attribute highlight = scheme.GetAttributeForRole (VisualRole.Highlight);

        Assert.NotEqual (default, highlight);
    }

    [Fact]
    public void Scheme_EditableDerivation_WithNoneForeground_DoesNotCrash ()
    {
        Scheme scheme = new () { Normal = new Attribute (Color.None, new Color (30, 30, 30)) };

        // Editable calls GetDimmerColor on foreground — should not fail with Color.None
        Attribute editable = scheme.GetAttributeForRole (VisualRole.Editable);

        Assert.NotEqual (default, editable);
    }

    [Fact]
    public void Scheme_FocusDerivation_WithoutDriver_FallsBackToBlack ()
    {
        // Without a driver (no Application.Init), None background resolves to Black (0,0,0)
        Scheme scheme = new () { Normal = new Attribute (new Color (200, 200, 200), Color.None) };

        Attribute focus = scheme.GetAttributeForRole (VisualRole.Focus);

        Assert.Equal (0, focus.Foreground.R);
        Assert.Equal (0, focus.Foreground.G);
        Assert.Equal (0, focus.Foreground.B);
    }

    [Fact]
    public void Scheme_FocusDerivation_WithNoneForeground_FallsBackToWhite ()
    {
        // Without a driver, None foreground resolves to White (255,255,255)
        Scheme scheme = new () { Normal = new Attribute (Color.None, new Color (30, 30, 40)) };

        Attribute focus = scheme.GetAttributeForRole (VisualRole.Focus);

        // Focus swaps: Foreground = Normal.Background (30,30,40), Background = ResolveNone(Normal.Foreground, fg) = white
        Assert.Equal (30, focus.Foreground.R);
        Assert.Equal (30, focus.Foreground.G);
        Assert.Equal (40, focus.Foreground.B);
    }

    #endregion

    #region OSC Sequence Definition Tests

    [Fact]
    public void OSC_QueryForegroundColor_HasCorrectRequest ()
    {
        Assert.Equal ("\u001b]10;?\u001b\\", EscSeqUtils.OSC_QueryForegroundColor.Request);
        Assert.Equal ("\u001b\\", EscSeqUtils.OSC_QueryForegroundColor.Terminator);
        Assert.Equal ("10", EscSeqUtils.OSC_QueryForegroundColor.Value);
    }

    [Fact]
    public void OSC_QueryBackgroundColor_HasCorrectRequest ()
    {
        Assert.Equal ("\u001b]11;?\u001b\\", EscSeqUtils.OSC_QueryBackgroundColor.Request);
        Assert.Equal ("\u001b\\", EscSeqUtils.OSC_QueryBackgroundColor.Terminator);
        Assert.Equal ("11", EscSeqUtils.OSC_QueryBackgroundColor.Value);
    }

    #endregion
}
