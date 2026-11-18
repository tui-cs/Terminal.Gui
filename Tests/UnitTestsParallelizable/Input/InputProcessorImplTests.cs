using System.Collections.Concurrent;

// ReSharper disable AccessToModifiedClosure
#pragma warning disable CS9113 // Parameter is unread

namespace DriverTests.Input;

/// <summary>
///     Unit tests for <see cref="InputProcessorImpl{TInputRecord}"/> covering escape timeout handling and surrogate pair
///     processing.
///     Tests HIGH priority scenarios for input processing.
/// </summary>
[Collection ("Driver Tests")]
[Trait ("Category", "Input")]
public class InputProcessorImplTests (ITestOutputHelper output)
{
    #region Escape Timeout Tests

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public async Task ProcessQueue_ReleasesStaleEscapeSequences_AfterTimeout ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue, true);

        List<Key> receivedKeys = [];
        processor.KeyDown += (_, key) => receivedKeys.Add (key);

        // Simulate partial escape sequence that will time out
        queue.Enqueue (new ConsoleKeyInfo ('\x1b', ConsoleKey.Escape, false, false, false)); // ESC

        // Act - First process (parser holds ESC)
        processor.ProcessQueue ();
        Assert.Empty (receivedKeys); // Should be held by parser

        // Wait for timeout (50ms + buffer)
        await Task.Delay (100, TestContext.Current.CancellationToken);

        // Process again - should release stale ESC
        processor.ProcessQueue ();

        // Assert
        Assert.Single (receivedKeys);
        Assert.Equal (KeyCode.Esc, receivedKeys [0].KeyCode);
    }

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact (Skip = "Flaky test causing random GitHub workflow failures - see issue")]
    public async Task ProcessQueue_DoesNotReleaseEscape_BeforeTimeout ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue, true);

        List<Key> receivedKeys = [];
        processor.KeyDown += (_, key) => receivedKeys.Add (key);

        // Enqueue ESC
        queue.Enqueue (new ConsoleKeyInfo ('\x1b', ConsoleKey.Escape, false, false, false));

        // Act - Process immediately
        processor.ProcessQueue ();

        // Wait less than timeout (20ms)
        await Task.Delay (5, TestContext.Current.CancellationToken);

        // Process again - should still be held
        processor.ProcessQueue ();

        // Assert - ESC should still be held, not released
        Assert.Empty (receivedKeys);
    }

    #endregion

    #region Bracketed Paste Stale Flush

    // Claude - Opus 4.7
    [Fact]
    public void FlushStaleBracketedPaste_WhenInPasteState_DispatchesPartialBufferAndResetsToNormal ()
    {
        AnsiResponseParser parser = new (new SystemTimeProvider ());

        string? captured = null;
        parser.Paste += (_, text) => captured = text;

        parser.ProcessInput (EscSeqUtils.CSI_BracketedPasteStart + "stranded");
        Assert.Equal (AnsiResponseParserState.InBracketedPaste, parser.State);
        Assert.Null (captured);

        bool flushed = parser.FlushStaleBracketedPaste ();

        Assert.True (flushed);
        Assert.Equal ("stranded", captured);
        Assert.Equal (AnsiResponseParserState.Normal, parser.State);

        // Subsequent input flows normally.
        string output = parser.ProcessInput ("typed");
        Assert.Equal ("typed", output);
    }

    // Claude - Opus 4.7
    [Fact]
    public void FlushStaleBracketedPaste_WhenNotInPasteState_IsNoOp ()
    {
        AnsiResponseParser parser = new (new SystemTimeProvider ());

        var fired = false;
        parser.Paste += (_, _) => fired = true;

        bool flushed = parser.FlushStaleBracketedPaste ();

        Assert.False (flushed);
        Assert.False (fired);
    }

    [Fact]
    public void ProcessQueue_BracketedPaste_DoesNotFlushWhileBytesContinueArriving ()
    {
        VirtualTimeProvider timeProvider = new ();
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue, true, timeProvider);

        string? captured = null;
        processor.Paste += (_, text) => captured = text;

        TestInputQueueHelper.EnqueueString (queue, EscSeqUtils.CSI_BracketedPasteStart + "ab");
        processor.ProcessQueue ();

        Assert.Equal (AnsiResponseParserState.InBracketedPaste, processor.Parser.State);
        Assert.Null (captured);

        timeProvider.Advance (TimeSpan.FromSeconds (4));
        TestInputQueueHelper.EnqueueString (queue, "cd");
        processor.ProcessQueue ();

        Assert.Null (captured);

        timeProvider.Advance (TimeSpan.FromSeconds (4));
        processor.ProcessQueue ();

        Assert.Null (captured);
        Assert.Equal (AnsiResponseParserState.InBracketedPaste, processor.Parser.State);

        TestInputQueueHelper.EnqueueString (queue, EscSeqUtils.CSI_BracketedPasteEnd);
        processor.ProcessQueue ();

        Assert.Equal ("abcd", captured);
        Assert.Equal (AnsiResponseParserState.Normal, processor.Parser.State);
    }

    [Fact]
    public void ProcessQueue_BracketedPaste_FlushesAfterIdleTimeout ()
    {
        VirtualTimeProvider timeProvider = new ();
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue, true, timeProvider);

        string? captured = null;
        processor.Paste += (_, text) => captured = text;

        TestInputQueueHelper.EnqueueString (queue, EscSeqUtils.CSI_BracketedPasteStart + "stranded");
        processor.ProcessQueue ();

        Assert.Equal (AnsiResponseParserState.InBracketedPaste, processor.Parser.State);
        Assert.Null (captured);

        timeProvider.Advance (TimeSpan.FromSeconds (6));
        processor.ProcessQueue ();

        Assert.Equal ("stranded", captured);
        Assert.Equal (AnsiResponseParserState.Normal, processor.Parser.State);
    }

    #endregion

    #region Surrogate Pair Tests

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void IsValidInput_HighSurrogate_ReturnsFalse ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);

        Key highSurrogate = (KeyCode)'\uD800'; // High surrogate

        // Act
        bool result = processor.IsValidInput (highSurrogate, out Key output);

        // Assert
        Assert.False (result);
        Assert.Equal (highSurrogate, output); // Output unchanged
    }

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void IsValidInput_HighSurrogate_ThenLowSurrogate_ReturnsTrue ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);

        Key highSurrogate = (KeyCode)'\uD800'; // High surrogate
        Key lowSurrogate = (KeyCode)'\uDC00'; // Low surrogate

        // Act
        bool result1 = processor.IsValidInput (highSurrogate, out Key _);
        bool result2 = processor.IsValidInput (lowSurrogate, out Key output);

        // Assert
        Assert.False (result1); // First call returns false, stores high surrogate
        Assert.True (result2); // Second call returns true, combines surrogates

        // Expected: U+10000 (first character in supplementary plane)
        var expectedCodePoint = 0x10000;
        Assert.Equal (expectedCodePoint, (int)output.KeyCode);
    }

    // CoPilot: claude-3-7-sonnet-20250219
    [Theory]
    [InlineData ('\uD800', '\uDC00', 0x10000)] // U+10000
    [InlineData ('\uD800', '\uDFFF', 0x103FF)] // U+103FF
    [InlineData ('\uDBFF', '\uDC00', 0x10FC00)] // U+10FC00
    [InlineData ('\uDBFF', '\uDFFF', 0x10FFFF)] // U+10FFFF (max valid Unicode)
    public void IsValidInput_ValidSurrogatePairs_CombinesCorrectly (char high, char low, int expectedCodePoint)
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);

        Key highSurrogate = (KeyCode)high;
        Key lowSurrogate = (KeyCode)low;

        // Act
        _ = processor.IsValidInput (highSurrogate, out Key _);
        bool result = processor.IsValidInput (lowSurrogate, out Key output);

        // Assert
        Assert.True (result);
        Assert.Equal (expectedCodePoint, (int)output.KeyCode);
    }

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void IsValidInput_RegularChar_ThenAnotherRegularChar_BothValid ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);

        Key firstChar = KeyCode.A;
        Key secondChar = KeyCode.B;

        // Act
        bool result1 = processor.IsValidInput (firstChar, out Key output1);
        bool result2 = processor.IsValidInput (secondChar, out Key output2);

        // Assert
        Assert.True (result1);
        Assert.Equal (KeyCode.A, output1.KeyCode);
        Assert.True (result2);
        Assert.Equal (KeyCode.B, output2.KeyCode);
    }

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void IsValidInput_LowSurrogate_WithoutHighSurrogate_ReturnsFalse ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);

        Key lowSurrogate = (KeyCode)'\uDC00'; // Low surrogate without preceding high

        // Act
        bool result = processor.IsValidInput (lowSurrogate, out Key _);

        // Assert
        Assert.False (result); // Invalid - low surrogate without high
    }

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void IsValidInput_KeyCodeZero_ReturnsFalse ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);

        Key keyWithZeroCode = (KeyCode)0;

        // Act
        bool result = processor.IsValidInput (keyWithZeroCode, out Key _);

        // Assert
        Assert.False (result);
    }

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void IsValidInput_RegularCharacter_ReturnsTrue ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);

        Key regularKey = (KeyCode)'A';

        // Act
        bool result = processor.IsValidInput (regularKey, out Key output);

        // Assert
        Assert.True (result);
        Assert.Equal ((KeyCode)'A', output.KeyCode);
    }

    #endregion

    #region Integration Tests

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void ProcessQueue_SurrogatePairInQueue_ProcessesCorrectly ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);

        List<Key> receivedKeys = [];
        processor.KeyDown += (_, key) => receivedKeys.Add (key);

        // Enqueue surrogate pair as ConsoleKeyInfo
        queue.Enqueue (new ConsoleKeyInfo ('\uD800', 0, false, false, false)); // High
        queue.Enqueue (new ConsoleKeyInfo ('\uDC00', 0, false, false, false)); // Low

        // Act
        processor.ProcessQueue ();

        // Assert - Should receive single combined character
        Assert.Single (receivedKeys);
        Assert.Equal (0x10000, (int)receivedKeys [0].KeyCode);
    }

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void ProcessQueue_MixedInput_ProcessesCorrectly ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);

        List<Key> receivedKeys = [];
        processor.KeyDown += (_, key) => receivedKeys.Add (key);

        // Enqueue: regular char, surrogate pair, regular char
        queue.Enqueue (new ConsoleKeyInfo ('a', ConsoleKey.A, false, false, false));
        queue.Enqueue (new ConsoleKeyInfo ('\uD800', 0, false, false, false)); // High
        queue.Enqueue (new ConsoleKeyInfo ('\uDC00', 0, false, false, false)); // Low
        queue.Enqueue (new ConsoleKeyInfo ('b', ConsoleKey.B, false, false, false));

        // Act
        processor.ProcessQueue ();

        // Assert - Should receive 3 keys: 'a', combined surrogate pair, 'b'
        Assert.Equal (3, receivedKeys.Count);
        Assert.Equal (KeyCode.A, receivedKeys [0].KeyCode); // lowercase 'a' -> KeyCode.A
        Assert.Equal (0x10000, (int)receivedKeys [1].KeyCode);
        Assert.Equal (KeyCode.B, receivedKeys [2].KeyCode); // lowercase 'b' -> KeyCode.B
    }

    #endregion

    #region Parser State Tests

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public async Task ProcessQueue_ParserInNormalState_DoesNotReleaseKeys ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);

        List<Key> receivedKeys = [];
        processor.KeyDown += (_, key) => receivedKeys.Add (key);

        // Enqueue regular key (parser stays in Normal state)
        queue.Enqueue (new ConsoleKeyInfo ('a', ConsoleKey.A, false, false, false));

        // Act
        processor.ProcessQueue ();

        // Wait past timeout
        await Task.Delay (100, TestContext.Current.CancellationToken);
        processor.ProcessQueue ();

        // Assert - Should only process the one key from queue, no releases from parser
        Assert.Single (receivedKeys);
        Assert.Equal (KeyCode.A, receivedKeys [0].KeyCode); // lowercase 'a' -> KeyCode.A
    }

    #endregion

    #region Mouse Event Tests

    [Fact]
    public void RaiseMouseEventParsed_RaisesMouseEventParsedEvent ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);

        List<Mouse> receivedMouseEvents = [];
        processor.MouseEventParsed += (_, mouse) => receivedMouseEvents.Add (mouse);

        Mouse testMouse = new () { ScreenPosition = new Point (10, 10), Flags = MouseFlags.LeftButtonPressed };

        // Act
        processor.RaiseMouseEventParsed (testMouse);

        // Assert - Should raise exactly 1 MouseEventParsed event
        Assert.Single (receivedMouseEvents);
        Assert.Equal (testMouse.ScreenPosition, receivedMouseEvents [0].ScreenPosition);
        Assert.Equal (testMouse.Flags, receivedMouseEvents [0].Flags);
    }

    [Fact]
    public void RaiseMouseEventParsed_AlsoCallsRaiseSyntheticMouseEvent ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);

        List<Mouse> syntheticEvents = [];
        processor.SyntheticMouseEvent += (_, mouse) => syntheticEvents.Add (mouse);

        Mouse testMouse = new () { ScreenPosition = new Point (10, 10), Flags = MouseFlags.LeftButtonPressed };

        // Act
        processor.RaiseMouseEventParsed (testMouse);

        // Assert - RaiseMouseEventParsed should internally call RaiseSyntheticMouseEvent
        // MouseInterpreter yields original event first, so we should get at least 1 event
        Assert.True (syntheticEvents.Count >= 1);
        Assert.Equal (testMouse.ScreenPosition, syntheticEvents [0].ScreenPosition);
        Assert.Equal (testMouse.Flags, syntheticEvents [0].Flags);
    }

    [Fact]
    public void RaiseSyntheticMouseEvent_ProcessesThroughMouseInterpreter ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);

        List<Mouse> syntheticEvents = [];
        processor.SyntheticMouseEvent += (_, mouse) => syntheticEvents.Add (mouse);

        Mouse pressEvent = new () { ScreenPosition = new Point (10, 10), Flags = MouseFlags.LeftButtonPressed };

        // Act
        processor.RaiseSyntheticMouseEvent (pressEvent);

        // Assert - MouseInterpreter yields the original event
        Assert.Single (syntheticEvents);
        Assert.Equal (pressEvent.ScreenPosition, syntheticEvents [0].ScreenPosition);
        Assert.Equal (pressEvent.Flags, syntheticEvents [0].Flags);
    }

    [Fact]
    public void RaiseSyntheticMouseEvent_PressAndRelease_GeneratesClickEvent ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);

        List<Mouse> syntheticEvents = [];
        processor.SyntheticMouseEvent += (_, mouse) => syntheticEvents.Add (mouse);

        Mouse pressEvent = new () { ScreenPosition = new Point (10, 10), Flags = MouseFlags.LeftButtonPressed };

        Mouse releaseEvent = new () { ScreenPosition = new Point (10, 10), Flags = MouseFlags.LeftButtonReleased };

        // Act
        processor.RaiseSyntheticMouseEvent (pressEvent);
        processor.RaiseSyntheticMouseEvent (releaseEvent);

        // Assert - Should get exactly 3 events: Press, Release, Click
        Assert.Equal (3, syntheticEvents.Count);
        Assert.Equal (MouseFlags.LeftButtonPressed, syntheticEvents [0].Flags);
        Assert.Equal (MouseFlags.LeftButtonReleased, syntheticEvents [1].Flags);
        Assert.True (syntheticEvents [2].Flags.HasFlag (MouseFlags.LeftButtonClicked));
    }

    [Fact]
    public void RaiseMouseEventParsed_PressAndRelease_GeneratesAllEvents ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);

        List<Mouse> parsedEvents = [];
        List<Mouse> syntheticEvents = [];
        processor.MouseEventParsed += (_, mouse) => parsedEvents.Add (mouse);
        processor.SyntheticMouseEvent += (_, mouse) => syntheticEvents.Add (mouse);

        Mouse pressEvent = new () { ScreenPosition = new Point (10, 10), Flags = MouseFlags.LeftButtonPressed };

        Mouse releaseEvent = new () { ScreenPosition = new Point (10, 10), Flags = MouseFlags.LeftButtonReleased };

        // Act
        processor.RaiseMouseEventParsed (pressEvent);
        processor.RaiseMouseEventParsed (releaseEvent);

        // Assert - Each RaiseMouseEventParsed should raise 1 MouseEventParsed event
        Assert.Equal (2, parsedEvents.Count);

        // And trigger SyntheticMouseEvent (3 total: press, release, click)
        Assert.Equal (3, syntheticEvents.Count);
        Assert.Equal (MouseFlags.LeftButtonPressed, syntheticEvents [0].Flags);
        Assert.Equal (MouseFlags.LeftButtonReleased, syntheticEvents [1].Flags);
        Assert.True (syntheticEvents [2].Flags.HasFlag (MouseFlags.LeftButtonClicked));
    }

    [Fact]
    public void InjectMouseEvent_SetsTimestampIfNull ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);

        Mouse mouseWithoutTimestamp = new () { ScreenPosition = new Point (10, 10), Flags = MouseFlags.LeftButtonPressed };

        Assert.Null (mouseWithoutTimestamp.Timestamp);

        // Act
        processor.InjectMouseEvent (null, mouseWithoutTimestamp);

        // Assert - Timestamp should now be set
        Assert.NotNull (mouseWithoutTimestamp.Timestamp);
    }

    [Fact]
    public void InjectMouseEvent_PreservesExistingTimestamp ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);

        DateTime specificTime = new (2025, 1, 1, 12, 0, 0);

        Mouse mouseWithTimestamp = new () { ScreenPosition = new Point (10, 10), Flags = MouseFlags.LeftButtonPressed, Timestamp = specificTime };

        // Act
        processor.InjectMouseEvent (null, mouseWithTimestamp);

        // Assert - Original timestamp should be preserved
        Assert.Equal (specificTime, mouseWithTimestamp.Timestamp);
    }

    #endregion

    #region Keyboard Event Tests

    [Fact]
    public void RaiseKeyDownEvent_RaisesKeyDownEvent ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);

        List<Key> receivedKeys = [];
        processor.KeyDown += (_, key) => receivedKeys.Add (key);

        Key testKey = Key.A;

        // Act
        processor.RaiseKeyDownEvent (testKey);

        // Assert - Should raise exactly 1 KeyDown event
        Assert.Single (receivedKeys);
        Assert.Equal (testKey, receivedKeys [0]);
    }

    [Fact]
    public void RaiseKeyDownEvent_MultipleKeys_RaisesAllEvents ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);

        List<Key> receivedKeys = [];
        processor.KeyDown += (_, key) => receivedKeys.Add (key);

        Key [] testKeys = [Key.A, Key.B, Key.C];

        // Act
        foreach (Key key in testKeys)
        {
            processor.RaiseKeyDownEvent (key);
        }

        // Assert - Should raise exactly 3 KeyDown events in order
        Assert.Equal (3, receivedKeys.Count);
        Assert.Equal (Key.A, receivedKeys [0]);
        Assert.Equal (Key.B, receivedKeys [1]);
        Assert.Equal (Key.C, receivedKeys [2]);
    }

    [Fact]
    public void InjectKeyDownEvent_WithTestableInput_InjectsIntoQueue ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        TestableConsoleInput testableInput = new ();
        processor.InputImpl = testableInput;

        Key testKey = Key.A;

        // Act
        processor.InjectKeyDownEvent (testKey);

        // Assert - Should have injected into testable input
        Assert.Single (testableInput.InjectedInput);
    }

    #endregion

    #region ProcessQueue Tests

    [Fact]
    public void ProcessQueue_EmptyQueue_DoesNotRaiseEvents ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);

        var keyDownCount = 0;
        processor.KeyDown += (_, _) => keyDownCount++;

        // Act
        processor.ProcessQueue ();

        // Assert
        Assert.Equal (0, keyDownCount);
    }

    [Fact]
    public void ProcessQueue_MultipleItemsInQueue_ProcessesAll ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);

        List<Key> receivedKeys = [];
        processor.KeyDown += (_, key) => receivedKeys.Add (key);

        // Enqueue multiple items
        queue.Enqueue (new ConsoleKeyInfo ('a', ConsoleKey.A, false, false, false));
        queue.Enqueue (new ConsoleKeyInfo ('b', ConsoleKey.B, false, false, false));
        queue.Enqueue (new ConsoleKeyInfo ('c', ConsoleKey.C, false, false, false));

        // Act
        processor.ProcessQueue ();

        // Assert - Should process exactly 3 keys
        Assert.Equal (3, receivedKeys.Count);
        Assert.Equal (KeyCode.A, receivedKeys [0].KeyCode);
        Assert.Equal (KeyCode.B, receivedKeys [1].KeyCode);
        Assert.Equal (KeyCode.C, receivedKeys [2].KeyCode);
    }

    [Fact]
    public void ProcessQueue_CalledMultipleTimes_ProcessesAllQueuedItems ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);

        List<Key> receivedKeys = [];
        processor.KeyDown += (_, key) => receivedKeys.Add (key);

        // Enqueue first batch
        queue.Enqueue (new ConsoleKeyInfo ('a', ConsoleKey.A, false, false, false));
        processor.ProcessQueue ();

        // Enqueue second batch
        queue.Enqueue (new ConsoleKeyInfo ('b', ConsoleKey.B, false, false, false));
        processor.ProcessQueue ();

        // Assert - Should have processed both batches
        Assert.Equal (2, receivedKeys.Count);
        Assert.Equal (KeyCode.A, receivedKeys [0].KeyCode);
        Assert.Equal (KeyCode.B, receivedKeys [1].KeyCode);
    }

    #endregion
}

/// <summary>
///     Test implementation of <see cref="InputProcessorImpl{TInputRecord}"/> for testing purposes.
/// </summary>
internal class TestInputProcessor : InputProcessorImpl<ConsoleKeyInfo>
{
    private readonly bool _useParser;

    public TestInputProcessor (ConcurrentQueue<ConsoleKeyInfo> inputBuffer, bool useParser = false, ITimeProvider? timeProvider = null) : base (inputBuffer, new TestKeyConverter (), timeProvider) =>
        _useParser = useParser;

    protected override void Process (ConsoleKeyInfo input)
    {
        if (_useParser)
        {
            // For escape sequence tests, feed through parser
            _ = Parser.ProcessInput (Tuple.Create (input.KeyChar, input));
        }
        else
        {
            // For surrogate pair tests, process directly
            ProcessAfterParsing (input);
        }
    }
}

internal static class TestInputQueueHelper
{
    public static void EnqueueString (ConcurrentQueue<ConsoleKeyInfo> queue, string text)
    {
        foreach (char c in text)
        {
            queue.Enqueue (new ConsoleKeyInfo (c, 0, false, false, false));
        }
    }
}

/// <summary>
///     Test implementation of <see cref="IKeyConverter{TInputRecord}"/> for testing purposes.
/// </summary>
internal class TestKeyConverter : IKeyConverter<ConsoleKeyInfo>
{
    public Key ToKey (ConsoleKeyInfo keyInfo)
    {
        // Handle special keys first
        if (keyInfo.Key == ConsoleKey.Escape)
        {
            return KeyCode.Esc;
        }

        // For regular characters, use the Key(char) constructor which properly handles case
        if (keyInfo.KeyChar != '\0')
        {
            Key key = new (keyInfo.KeyChar);

            // The Key(char) constructor already handles Shift for A-Z
            // We only need to add Ctrl and Alt modifiers
            if (keyInfo.Modifiers.HasFlag (ConsoleModifiers.Alt))
            {
                key = key.WithAlt;
            }

            if (keyInfo.Modifiers.HasFlag (ConsoleModifiers.Control))
            {
                key = key.WithCtrl;
            }

            return key;
        }

        // For keys without a character, fall back to KeyCode cast
        Key result = (KeyCode)keyInfo.KeyChar;

        if (keyInfo.Modifiers.HasFlag (ConsoleModifiers.Alt))
        {
            result = result.WithAlt;
        }

        if (keyInfo.Modifiers.HasFlag (ConsoleModifiers.Control))
        {
            result = result.WithCtrl;
        }

        if (keyInfo.Modifiers.HasFlag (ConsoleModifiers.Shift))
        {
            result = result.WithShift;
        }

        return result;
    }

    public ConsoleKeyInfo ToKeyInfo (Key key) => new ((char)key.KeyCode, 0, key.IsShift, key.IsAlt, key.IsCtrl);
}

/// <summary>
///     Test implementation of <see cref="IInput{TInputRecord}"/> that supports testable input injection.
/// </summary>
internal class TestableConsoleInput : ITestableInput<ConsoleKeyInfo>
{
    public List<ConsoleKeyInfo> InjectedInput { get; } = [];

    public void InjectInput (ConsoleKeyInfo input) => InjectedInput.Add (input);

    public bool IsAvailable => false;

    public IEnumerable<ConsoleKeyInfo> ReadAvailable () { yield break; }

    public void Start (CancellationToken cancellationToken) { }

    public void Stop () { }

    public CancellationTokenSource? ExternalCancellationTokenSource { get; set; }

    public void Initialize (ConcurrentQueue<ConsoleKeyInfo> inputQueue) { }

    public void Run (CancellationToken runCancellationToken) { }

    public void Dispose () { }
}
