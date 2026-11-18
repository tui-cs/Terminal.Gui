using System.Diagnostics;
using System.Text;

namespace DriverTests.AnsiHandling;

// BUGBUG: These tests use TInputRecord of `int`, but that's not a realistic type for keyboard input.
[Collection ("Driver Tests")]
public class AnsiResponseParserTests (ITestOutputHelper output)
{
    private readonly AnsiResponseParser<int> _parser1 = new (new SystemTimeProvider ());
    private readonly AnsiResponseParser _parser2 = new (new SystemTimeProvider ());

    /// <summary>
    ///     Used for the T value in batches that are passed to the  AnsiResponseParser&lt;int&gt;  (parser1)
    /// </summary>
    private int _tIndex;

    [Fact]
    public void TestInputProcessing ()
    {
        string ansiStream = "\u001b[<0;10;20M"
                            + // ANSI escape for mouse move at (10, 20)
                            "Hello"
                            + // User types "Hello"
                            "\u001b[0c"; // Device Attributes response (e.g., terminal identification i.e. DAR)

        string? response1 = null;
        string? response2 = null;

        var i = 0;

        // Imagine that we are expecting a DAR
        _parser1.ExpectResponse ("c", null, s => response1 = s, null, false);
        _parser2.ExpectResponse ("c", null, s => response2 = s, null, false);

        // First char is Escape which we must consume incase what follows is the DAR
        AssertConsumed (ansiStream, ref i); // Esc

        for (var c = 0; c < "[<0;10;20".Length; c++)
        {
            AssertConsumed (ansiStream, ref i);
        }

        // We see the M terminator
        AssertReleased (ansiStream, ref i, "\u001b[<0;10;20M");

        // Regular user typing
        for (var c = 0; c < "Hello".Length; c++)
        {
            AssertIgnored (ansiStream, "Hello" [c], ref i);
        }

        // Now we have entered the actual DAR we should be consuming these
        for (var c = 0; c < "\u001b[0".Length; c++)
        {
            AssertConsumed (ansiStream, ref i);
        }

        // Consume the terminator 'c' and expect this to call the above event
        Assert.Null (response1);
        Assert.Null (response1);
        AssertConsumed (ansiStream, ref i);
        Assert.NotNull (response2);
        Assert.Equal ("\u001b[0c", response2);
        Assert.NotNull (response2);
        Assert.Equal ("\u001b[0c", response2);
    }

    [Theory]
    [InlineData ("\u001b[<0;10;20MHi\u001b[0c", "c", "\u001b[0c", "\u001b[<0;10;20MHi")]
    [InlineData ("\u001b[<1;15;25MYou\u001b[1c", "c", "\u001b[1c", "\u001b[<1;15;25MYou")]
    [InlineData ("\u001b[0cHi\u001b[0c", "c", "\u001b[0c", "Hi\u001b[0c")]
    [InlineData ("\u001b[<0;0;0MHe\u001b[3c", "c", "\u001b[3c", "\u001b[<0;0;0MHe")]
    [InlineData ("\u001b[<0;1;2Da\u001b[0c\u001b[1c", "c", "\u001b[0c", "\u001b[<0;1;2Da\u001b[1c")]
    [InlineData ("\u001b[1;1M\u001b[3cAn", "c", "\u001b[3c", "\u001b[1;1MAn")]
    [InlineData ("hi\u001b[2c\u001b[<5;5;5m", "c", "\u001b[2c", "hi\u001b[<5;5;5m")]
    [InlineData ("\u001b[3c\u001b[4c\u001b[<0;0;0MIn", "c", "\u001b[3c", "\u001b[4c\u001b[<0;0;0MIn")]
    [InlineData ("\u001b[<1;2;3M\u001b[0c\u001b[<1;2;3M\u001b[2c", "c", "\u001b[0c", "\u001b[<1;2;3M\u001b[<1;2;3M\u001b[2c")]
    [InlineData ("\u001b[<0;1;1MHi\u001b[6c\u001b[2c\u001b[<1;0;0MT", "c", "\u001b[6c", "\u001b[<0;1;1MHi\u001b[2c\u001b[<1;0;0MT")]
    [InlineData ("Te\u001b[<2;2;2M\u001b[7c", "c", "\u001b[7c", "Te\u001b[<2;2;2M")]
    [InlineData ("\u001b[0c\u001b[<0;0;0M\u001b[3c\u001b[0c\u001b[1;0MT", "c", "\u001b[0c", "\u001b[<0;0;0M\u001b[3c\u001b[0c\u001b[1;0MT")]
    [InlineData ("\u001b[0;0M\u001b[<0;0;0M\u001b[3cT\u001b[1c", "c", "\u001b[3c", "\u001b[0;0M\u001b[<0;0;0MT\u001b[1c")]
    [InlineData ("\u001b[3c\u001b[<0;0;0M\u001b[0c\u001b[<1;1;1MIn\u001b[1c", "c", "\u001b[3c", "\u001b[<0;0;0M\u001b[0c\u001b[<1;1;1MIn\u001b[1c")]
    [InlineData ("\u001b[<5;5;5M\u001b[7cEx\u001b[8c", "c", "\u001b[7c", "\u001b[<5;5;5MEx\u001b[8c")]

    // Random characters and mixed inputs
    [InlineData ("\u001b[<1;1;1MJJ\u001b[9c", "c", "\u001b[9c", "\u001b[<1;1;1MJJ")] // Mixed text
    [InlineData ("Be\u001b[0cAf", "c", "\u001b[0c", "BeAf")] // Escape in the middle of the string
    [InlineData ("\u001b[<0;0;0M\u001b[2cNot e", "c", "\u001b[2c", "\u001b[<0;0;0MNot e")] // Unexpected sequence followed by text
    [InlineData ("Just te\u001b[<0;0;0M\u001b[3c\u001b[2c\u001b[4c",
                 "c",
                 "\u001b[3c",
                 "Just te\u001b[<0;0;0M\u001b[2c\u001b[4c")] // Multiple unexpected responses
    [InlineData ("\u001b[1;2;3M\u001b[0c\u001b[2;2M\u001b[0;0;0MTe",
                 "c",
                 "\u001b[0c",
                 "\u001b[1;2;3M\u001b[2;2M\u001b[0;0;0MTe")] // Multiple commands with responses
    [InlineData ("\u001b[<3;3;3Mabc\u001b[4cde", "c", "\u001b[4c", "\u001b[<3;3;3Mabcde")] // Escape sequences mixed with regular text

    // Edge cases
    [InlineData ("\u001b[0c\u001b[0c\u001b[0c", "c", "\u001b[0c", "\u001b[0c\u001b[0c")] // Multiple identical responses
    [InlineData ("", "c", "", "")] // Empty input
    [InlineData ("Normal", "c", "", "Normal")] // No escape sequences
    [InlineData ("\u001b[<0;0;0M", "c", "", "\u001b[<0;0;0M")] // Escape sequence only
    [InlineData ("\u001b[1;2;3M\u001b[0c", "c", "\u001b[0c", "\u001b[1;2;3M")] // Last response consumed
    [InlineData ("Inpu\u001b[0c\u001b[1;0;0M", "c", "\u001b[0c", "Inpu\u001b[1;0;0M")] // Single input followed by escape
    [InlineData ("\u001b[2c\u001b[<5;6;7MDa", "c", "\u001b[2c", "\u001b[<5;6;7MDa")] // Multiple escape sequences followed by text
    [InlineData ("\u001b[0cHi\u001b[1cGo", "c", "\u001b[0c", "Hi\u001b[1cGo")] // Normal text with multiple escape sequences
    [InlineData ("\u001b[<1;1;1MTe", "c", "", "\u001b[<1;1;1MTe")]

    // Add more test cases here...
    public void TestInputSequences (string ansiStream, string? expectedTerminator, string expectedResponse, string expectedOutput)
    {
        var swGenBatches = Stopwatch.StartNew ();
        var tests = 0;

        string [] [] permutations = GetBatchPermutations (ansiStream, 5).ToArray ();

        swGenBatches.Stop ();
        var swRunTest = Stopwatch.StartNew ();

        foreach (string [] batchSet in permutations)
        {
            _tIndex = 0;
            var response1 = string.Empty;
            var response2 = string.Empty;

            // Register the expected response with the given terminator
            _parser1.ExpectResponse (expectedTerminator, null, s => response1 = s, null, false);
            _parser2.ExpectResponse (expectedTerminator, null, s => response2 = s, null, false);

            // Process the input
            var actualOutput1 = new StringBuilder ();
            var actualOutput2 = new StringBuilder ();

            foreach (string batch in batchSet)
            {
                IEnumerable<Tuple<char, int>> output1 = _parser1.ProcessInput (StringToBatch (batch));
                actualOutput1.Append (BatchToString (output1));

                string output2 = _parser2.ProcessInput (batch);
                actualOutput2.Append (output2);
            }

            // Assert the final output minus the expected response
            Assert.Equal (expectedOutput, actualOutput1.ToString ());
            Assert.Equal (expectedResponse, response1);
            Assert.Equal (expectedOutput, actualOutput2.ToString ());
            Assert.Equal (expectedResponse, response2);
            tests++;
        }

        output.WriteLine ($"Tested {tests} in {swRunTest.ElapsedMilliseconds} ms (gen batches took {swGenBatches.ElapsedMilliseconds} ms)");
    }

    public static IEnumerable<object? []> TestInputSequencesExact_Cases ()
    {
        yield return ["Esc Only", null, new [] { new StepExpectation ('\u001b', AnsiResponseParserState.ExpectingEscapeSequence, string.Empty) }];

        yield return
        [
            "Esc Hi with intermediate",
            'c',
            new []
            {
                new StepExpectation ('\u001b', AnsiResponseParserState.ExpectingEscapeSequence, string.Empty),
                new StepExpectation ('H',
                                     AnsiResponseParserState.InResponse,
                                     string.Empty), // H is known terminator and not expected one so here we release both chars
                new StepExpectation ('\u001b', AnsiResponseParserState.ExpectingEscapeSequence, "\u001bH"),
                new StepExpectation ('[', AnsiResponseParserState.InResponse, string.Empty),
                new StepExpectation ('0', AnsiResponseParserState.InResponse, string.Empty),
                new StepExpectation ('c',
                                     AnsiResponseParserState.Normal,
                                     string.Empty,
                                     "\u001b[0c"), // c is expected terminator so here we swallow input and populate expected response
                new StepExpectation ('\u001b', AnsiResponseParserState.ExpectingEscapeSequence, string.Empty)
            }
        ];
    }

    public class StepExpectation ()
    {
        /// <summary>
        ///     The input character to feed into the parser at this step of the test
        /// </summary>
        public char Input { get; }

        /// <summary>
        ///     What should the state of the parser be after the <see cref="Input"/>
        ///     is fed in.
        /// </summary>
        public AnsiResponseParserState ExpectedStateAfterOperation { get; }

        /// <summary>
        ///     If this step should release one or more characters, put them here.
        /// </summary>
        public string ExpectedRelease { get; } = string.Empty;

        /// <summary>
        ///     If this step should result in a completing of detection of ANSI response
        ///     then put the expected full response sequence here.
        /// </summary>
        public string ExpectedAnsiResponse { get; } = string.Empty;

        public StepExpectation (char input,
                                AnsiResponseParserState expectedStateAfterOperation,
                                string expectedRelease = "",
                                string expectedAnsiResponse = "") : this ()
        {
            Input = input;
            ExpectedStateAfterOperation = expectedStateAfterOperation;
            ExpectedRelease = expectedRelease;
            ExpectedAnsiResponse = expectedAnsiResponse;
        }
    }

    [MemberData (nameof (TestInputSequencesExact_Cases))]
    [Theory]
    public void TestInputSequencesExact (string caseName, char? terminator, IEnumerable<StepExpectation> expectedStates)
    {
        output.WriteLine ("Running test case:" + caseName);

        var parser = new AnsiResponseParser (new SystemTimeProvider ());
        string? response = null;

        if (terminator.HasValue)
        {
            parser.ExpectResponse (terminator.Value.ToString (), null, s => response = s, null, false);
        }

        var step = 0;

        foreach (StepExpectation state in expectedStates)
        {
            step++;

            // If we expect the response to be detected at this step
            if (!string.IsNullOrWhiteSpace (state.ExpectedAnsiResponse))
            {
                // Then before passing input it should be null
                Assert.Null (response);
            }

            string actual = parser.ProcessInput (state.Input.ToString ());

            Assert.Equal (state.ExpectedRelease, actual);
            Assert.Equal (state.ExpectedStateAfterOperation, parser.State);

            // If we expect the response to be detected at this step
            if (!string.IsNullOrWhiteSpace (state.ExpectedAnsiResponse))
            {
                // And after passing input it shuld be the expected value
                Assert.Equal (state.ExpectedAnsiResponse, response);
            }

            output.WriteLine ($"Step {step} passed");
        }
    }

    [Fact]
    public void ReleasesEscapeAfterTimeout ()
    {
        var input = "\u001b";
        var i = 0;

        // Esc on its own looks like it might be an esc sequence so should be consumed
        AssertConsumed (input, ref i);

        // We should know when the state changed
        Assert.Equal (AnsiResponseParserState.ExpectingEscapeSequence, _parser1.State);
        Assert.Equal (AnsiResponseParserState.ExpectingEscapeSequence, _parser2.State);

        Assert.Equal (DateTime.Now.Date, _parser1.StateChangedAt.Date);
        Assert.Equal (DateTime.Now.Date, _parser2.StateChangedAt.Date);

        AssertManualReleaseIs (input);
    }

    [Fact]
    public void TwoEscapesInARow ()
    {
        // Example user presses Esc key then a DAR comes in
        var input = "\u001b\u001b";
        var i = 0;

        // First Esc gets grabbed
        AssertConsumed (input, ref i);

        // Upon getting the second Esc we should release the first
        AssertReleased (input, ref i, "\u001b", 0);

        // Assume 50ms or something has passed, lets force release as no new content

        // It should be the second escape that gets released (i.e. index 1)
        AssertManualReleaseIs ("\u001b", 1);
    }

    [Fact]
    public void TestLateResponses ()
    {
        var p = new AnsiResponseParser (new SystemTimeProvider ());

        string? responseA = null;
        string? responseB = null;

        p.ExpectResponse ("z", null, r => responseA = r, null, false);

        // Some time goes by without us seeing a response
        p.StopExpecting ("z", null, false);

        // Send our new request
        p.ExpectResponse ("z", null, r => responseB = r, null, false);

        // Because we gave up on getting A, we should expect the response to be to our new request
        Assert.Empty (p.ProcessInput ("\u001b[<1;2z"));
        Assert.Null (responseA);
        Assert.Equal ("\u001b[<1;2z", responseB);

        // Oh looks like we got one late after all - swallow it
        Assert.Empty (p.ProcessInput ("\u001b[0000z"));

        // Do not expect late responses to be populated back to your variable
        Assert.Null (responseA);
        Assert.Equal ("\u001b[<1;2z", responseB);

        // We now have no outstanding requests (late or otherwise) so new ansi codes should just fall through
        Assert.Equal ("\u001b[111z", p.ProcessInput ("\u001b[111z"));
    }

    [Fact]
    public void TestPersistentResponses ()
    {
        var p = new AnsiResponseParser (new SystemTimeProvider ());

        var m = 0;
        var M = 1;

        p.ExpectResponse ("m", null, _ => m++, null, true);
        p.ExpectResponse ("M", null, _ => M++, null, true);

        // Act - Feed input strings containing ANSI sequences
        p.ProcessInput ("\u001b[<0;10;10m"); // Should match and increment `m`
        p.ProcessInput ("\u001b[<0;20;20m"); // Should match and increment `m`
        p.ProcessInput ("\u001b[<0;30;30M"); // Should match and increment `M`
        p.ProcessInput ("\u001b[<0;40;40M"); // Should match and increment `M`
        p.ProcessInput ("\u001b[<0;50;50M"); // Should match and increment `M`

        // Assert - Verify that counters reflect the expected counts of each terminator
        Assert.Equal (2, m); // Expected two `m` responses
        Assert.Equal (4, M); // Expected three `M` responses plus the initial value of 1
    }

    [Fact]
    public void TestPersistentResponses_WithMetadata ()
    {
        AnsiResponseParser<int> p = new (new SystemTimeProvider ());

        // ReSharper disable once NotAccessedVariable
        var m = 0;

        List<Tuple<char, int>> result = [];

        p.ExpectResponseT ("m",
                           null,
                           r =>
                           {
                               result = r.ToList ();
                               m++;
                           },
                           null,
                           true);

        // Act - Feed input strings containing ANSI sequences
        p.ProcessInput (StringToBatch ("\u001b[<0;10;10m")); // Should match and increment `m`

        // Prepare expected result:
        List<Tuple<char, int>> expected =
        [
            Tuple.Create ('\u001b', 0), // Escape character
            Tuple.Create ('[', 1),
            Tuple.Create ('<', 2),
            Tuple.Create ('0', 3),
            Tuple.Create (';', 4),
            Tuple.Create ('1', 5),
            Tuple.Create ('0', 6),
            Tuple.Create (';', 7),
            Tuple.Create ('1', 8),
            Tuple.Create ('0', 9),
            Tuple.Create ('m', 10)
        ];

        Assert.Equal (expected.Count, result.Count); // Ensure the count is as expected
        Assert.True (expected.SequenceEqual (result), "The result does not match the expected output."); // Check the actual content
    }

    [Fact]
    public void ShouldSwallowUnknownResponses_WhenDelegateSaysSo ()
    {
        // Swallow all unknown escape codes
        _parser1.UnexpectedResponseHandler = _ => true;
        _parser2.UnknownResponseHandler = _ => true;

        AssertReleased ("Just te\u001b[<0;0;0M\u001b[3c\u001b[2c\u001b[4cst",
                        "Just test",
                        0,
                        1,
                        2,
                        3,
                        4,
                        5,
                        6,
                        28,
                        29);
    }

    [Fact]
    public void UnknownResponses_ParameterShouldMatch ()
    {
        // Track unknown responses passed to the UnexpectedResponseHandler
        List<string> unknownResponses = [];

        // Set up the UnexpectedResponseHandler to log each unknown response
        _parser1.UnexpectedResponseHandler = r1 =>
                                             {
                                                 unknownResponses.Add (BatchToString (r1));

                                                 return true; // Return true to swallow unknown responses
                                             };

        _parser2.UnknownResponseHandler = r2 =>
                                          {
                                              // parsers should be agreeing on what these responses are!
                                              Assert.Equal (unknownResponses.Last (), r2);

                                              return true; // Return true to swallow unknown responses
                                          };

        // Input with known and unknown responses
        AssertReleased ("Just te\u001b[<0;0;0M\u001b[3c\u001b[2c\u001b[4cst", "Just test");

        // Expected unknown responses (ANSI sequences that are unknown)
        List<string> expectedUnknownResponses = ["\u001b[<0;0;0M", "\u001b[3c", "\u001b[2c", "\u001b[4c"];

        // Assert that the UnexpectedResponseHandler was called with the correct unknown responses
        Assert.Equal (expectedUnknownResponses.Count, unknownResponses.Count);
        Assert.Equal (expectedUnknownResponses, unknownResponses);
    }

    [Fact]
    public void ParserDetectsMouse ()
    {
        // ANSI escape sequence for mouse down (using a generic format example)
        const string MOUSE_DOWN = "\u001B[<0;12;32M";

        // ANSI escape sequence for Device Attribute Response (e.g., Terminal identifying itself)
        const string DEVICE_ATTRIBUTE_RESPONSE = "\u001B[?1;2c";

        // ANSI escape sequence for mouse up (using a generic format example)
        const string MOUSE_UP = "\u001B[<0;25;50m";

        AnsiResponseParser parser = new (new SystemTimeProvider ()) { HandleMouse = true };

        string? foundDar = null;
        List<Mouse> mouseEventArgs = [];

        parser.Mouse += (s, e) => mouseEventArgs.Add (e);
        parser.ExpectResponse ("c", null, dar => foundDar = dar, null, false);
        string released = parser.ProcessInput ("a" + MOUSE_DOWN + "asdf" + DEVICE_ATTRIBUTE_RESPONSE + "bbcc" + MOUSE_UP + "sss");

        Assert.Equal ("aasdfbbccsss", released);

        Assert.Equal (2, mouseEventArgs.Count);

        Assert.NotNull (foundDar);
        Assert.Equal (DEVICE_ATTRIBUTE_RESPONSE, foundDar);

        Assert.True (mouseEventArgs [0].IsPressed);

        // Mouse positions in ANSI are 1 based so actual Terminal.Gui Screen positions are x-1,y-1
        Assert.Equal (11, mouseEventArgs [0].ScreenPosition.X);
        Assert.Equal (31, mouseEventArgs [0].ScreenPosition.Y);

        Assert.True (mouseEventArgs [1].IsReleased);
        Assert.Equal (24, mouseEventArgs [1].ScreenPosition.X);
        Assert.Equal (49, mouseEventArgs [1].ScreenPosition.Y);
    }

    [Fact]
    public void ParserDetectsKeyboard ()
    {
        // ANSI escape sequence for cursor left
        const string LEFT = "\u001b[D";

        // ANSI escape sequence for Device Attribute Response (e.g., Terminal identifying itself)
        const string DEVICE_ATTRIBUTE_RESPONSE = "\u001B[?1;2c";

        // ANSI escape sequence for cursor up (while shift held down)
        const string SHIFT_UP = "\u001b[1;2A";

        var parser = new AnsiResponseParser (new SystemTimeProvider ());

        parser.HandleKeyboard = true;
        string? foundDar = null;
        List<Key> keys = [];

        parser.Keyboard += (s, e) => keys.Add (e);
        parser.ExpectResponse ("c", null, dar => foundDar = dar, null, false);
        string released = parser.ProcessInput ("a" + LEFT + "asdf" + DEVICE_ATTRIBUTE_RESPONSE + "bbcc" + SHIFT_UP + "sss");

        Assert.Equal ("aasdfbbccsss", released);

        Assert.Equal (2, keys.Count);

        Assert.NotNull (foundDar);
        Assert.Equal (DEVICE_ATTRIBUTE_RESPONSE, foundDar);

        Assert.Equal (Key.CursorLeft, keys [0]);
        Assert.Equal (Key.CursorUp.WithShift, keys [1]);
    }

    // Claude - Opus 4.6
    // Regression #4952 follow-up: A pending CPR request (terminator R) must not be
    // stolen by CsiCursorPattern, which happens to match ESC[1;<col>R as "F3 with modifier".
    // Before the fix, when the cursor was on row 1 the standard CPR response
    // ESC[1;1R was dispatched as Key.F3 instead of routed to the expectation.
    [Fact]
    public void ExpectedCprResponse_IsNotMisinterpretedAsF3 ()
    {
        AnsiResponseParser parser = new (new SystemTimeProvider ());
        parser.HandleKeyboard = true;

        string? cprResponse = null;
        List<Key> keys = [];

        parser.Keyboard += (_, e) => keys.Add (e);
        parser.ExpectResponse ("R", null, s => cprResponse = s, null, false);

        // Standard CPR response when cursor is at row 1, col 1.
        string released = parser.ProcessInput ("\u001b[1;1R");

        Assert.Equal ("\u001b[1;1R", cprResponse);
        Assert.Empty (keys);
        Assert.Equal (string.Empty, released);
    }

    public static IEnumerable<object []> ParserDetects_FunctionKeys_Cases ()
    {
        // These are VT100 escape codes for F1-4
        yield return ["\u001bOP", Key.F1];

        yield return ["\u001bOQ", Key.F2];

        yield return ["\u001bOR", Key.F3];

        yield return ["\u001bOS", Key.F4];

        // These are also F keys
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
    }

    [MemberData (nameof (ParserDetects_FunctionKeys_Cases))]
    [Theory]
    public void ParserDetects_FunctionKeys (string input, Key expectedKey)
    {
        var parser = new AnsiResponseParser (new SystemTimeProvider ());

        parser.HandleKeyboard = true;
        List<Key> keys = [];

        parser.Keyboard += (s, e) => keys.Add (e);

        foreach (char ch in input)
        {
            parser.ProcessInput (new string (ch, 1));
        }

        Key k = Assert.Single (keys);

        Assert.Equal (k, expectedKey);
    }

    private Tuple<char, int> [] StringToBatch (string batch) => batch.Select (k => Tuple.Create (k, _tIndex++)).ToArray ();

    public static IEnumerable<string []> GetBatchPermutations (string input, int maxDepth = 3) =>

        // Call the recursive method to generate batches with an initial depth of 0
        GenerateBatches (input, 0, maxDepth, 0);

    private static IEnumerable<string []> GenerateBatches (string input, int start, int maxDepth, int currentDepth)
    {
        // If we have reached the maximum recursion depth, return no results
        if (currentDepth >= maxDepth)
        {
            yield break; // No more batches can be generated at this depth
        }

        // If we have reached the end of the string, return an empty list
        if (start >= input.Length)
        {
            yield return [];

            yield break;
        }

        // Iterate over the input string to create batches
        for (int i = start + 1; i <= input.Length; i++)
        {
            // Take a batch from 'start' to 'i'
            string batch = input.Substring (start, i - start);

            // Recursively get batches from the remaining substring, increasing the depth
            foreach (string [] remainingBatches in GenerateBatches (input, i, maxDepth, currentDepth + 1))
            {
                // Combine the current batch with the remaining batches
                var result = new string [1 + remainingBatches.Length];
                result [0] = batch;
                Array.Copy (remainingBatches, 0, result, 1, remainingBatches.Length);

                yield return result;
            }
        }
    }

    private void AssertIgnored (string ansiStream, char expected, ref int i)
    {
        char c2 = ansiStream [i];
        Tuple<char, int> [] c1 = NextChar (ansiStream, ref i);

        // Parser does not grab this key (i.e. driver can continue with regular operations)
        Assert.Equal (c1, _parser1.ProcessInput (c1));
        Assert.Equal (expected, c1.Single ().Item1);

        Assert.Equal (c2, _parser2.ProcessInput (c2.ToString ()).Single ());
        Assert.Equal (expected, c2);
    }

    private void AssertConsumed (string ansiStream, ref int i)
    {
        // Parser grabs this key
        char c2 = ansiStream [i];
        Tuple<char, int> [] c1 = NextChar (ansiStream, ref i);

        Assert.Empty (_parser1.ProcessInput (c1));
        Assert.Empty (_parser2.ProcessInput (c2.ToString ()));
    }

    /// <summary>
    ///     Overload that fully exhausts <paramref name="ansiStream"/> and asserts
    ///     that the final released content across whole processing is <paramref name="expectedRelease"/>
    /// </summary>
    /// <param name="ansiStream"></param>
    /// <param name="expectedRelease"></param>
    /// <param name="expectedTValues"></param>
    private void AssertReleased (string ansiStream, string expectedRelease, params int [] expectedTValues)
    {
        var sb = new StringBuilder ();
        List<int> tValues = [];

        var i = 0;

        while (i < ansiStream.Length)
        {
            char c2 = ansiStream [i];
            Tuple<char, int> [] c1 = NextChar (ansiStream, ref i);

            Tuple<char, int> [] released1 = _parser1.ProcessInput (c1).ToArray ();
            tValues.AddRange (released1.Select (kv => kv.Item2));

            string released2 = _parser2.ProcessInput (c2.ToString ());

            // Both parsers should have same chars so release chars consistently with each other
            Assert.Equal (BatchToString (released1), released2);

            sb.Append (released2);
        }

        Assert.Equal (expectedRelease, sb.ToString ());

        if (expectedTValues.Length > 0)
        {
            Assert.True (expectedTValues.SequenceEqual (tValues));
        }
    }

    /// <summary>
    ///     Asserts that <paramref name="i"/> index of <see cref="ansiStream"/> when consumed will release
    ///     <paramref name="expectedRelease"/>. Results in implicit increment of <paramref name="i"/>.
    ///     <remarks>Note that this does NOT iteratively consume all the stream, only 1 char at <paramref name="i"/></remarks>
    /// </summary>
    /// <param name="ansiStream"></param>
    /// <param name="i"></param>
    /// <param name="expectedRelease"></param>
    /// <param name="expectedTValues"></param>
    private void AssertReleased (string ansiStream, ref int i, string expectedRelease, params int [] expectedTValues)
    {
        char c2 = ansiStream [i];
        Tuple<char, int> [] c1 = NextChar (ansiStream, ref i);

        // Parser realizes it has grabbed content that does not belong to an outstanding request
        // Parser returns false to indicate to continue
        Tuple<char, int> [] released1 = _parser1.ProcessInput (c1).ToArray ();
        Assert.Equal (expectedRelease, BatchToString (released1));

        if (expectedTValues.Length > 0)
        {
            Assert.True (expectedTValues.SequenceEqual (released1.Select (kv => kv.Item2)));
        }

        Assert.Equal (expectedRelease, _parser2.ProcessInput (c2.ToString ()));
    }

    private string BatchToString (IEnumerable<Tuple<char, int>> processInput) => new (processInput.Select (a => a.Item1).ToArray ());

    private Tuple<char, int> [] NextChar (string ansiStream, ref int i) => StringToBatch (ansiStream [i++].ToString ());

    private void AssertManualReleaseIs (string expectedRelease, params int [] expectedTValues)
    {
        // Consumer is responsible for determining this based on  e.g. after 50ms
        Tuple<char, int> [] released1 = _parser1.Release ().ToArray ();
        Assert.Equal (expectedRelease, BatchToString (released1));

        if (expectedTValues.Length > 0)
        {
            Assert.True (expectedTValues.SequenceEqual (released1.Select (kv => kv.Item2)));
        }

        Assert.Equal (expectedRelease, _parser2.Release ());

        Assert.Equal (AnsiResponseParserState.Normal, _parser1.State);
        Assert.Equal (AnsiResponseParserState.Normal, _parser2.State);
    }
}
