using System.Collections.Concurrent;

namespace DriverTests.AnsiHandling;

/// <summary>
///     Tests for ITestableInput implementation in AnsiInput.
/// </summary>
[Collection ("Driver Tests")]
public class AnsiInputTestableTests
{
    #region Helper Methods

    /// <summary>
    ///     Simulates the input thread by manually draining AnsiInput's internal test queue
    ///     and moving items to the InputBuffer. This is needed because tests don't
    ///     start the actual input thread via Run().
    /// </summary>
    private static void SimulateInputThread (AnsiInput ansiInput, ConcurrentQueue<char> inputBuffer)
    {
        // AnsiInput's Peek() checks _testInput first
        while (ansiInput.Peek ())
        {
            // Read() drains _testInput first and returns items
            foreach (char item in ansiInput.Read ())
            {
                // Manually add to InputBuffer (simulating what Run() would do)
                inputBuffer.Enqueue (item);
            }
        }
    }

    /// <summary>
    ///     Processes the input queue with support for keys that may be held by the ANSI parser (like Esc).
    ///     The parser holds Esc for 50ms waiting to see if it's part of an escape sequence.
    /// </summary>
    private static void ProcessQueueWithEscapeHandling (AnsiInputProcessor processor, int maxAttempts = 3)
    {
        // First attempt - process immediately
        processor.ProcessQueue ();

        // For escape sequences, we may need to wait and process again
        // The parser holds escape for 50ms before releasing
        for (var attempt = 1; attempt < maxAttempts; attempt++)
        {
            Thread.Sleep (60); // Wait longer than the 50ms escape timeout
            processor.ProcessQueue (); // This should release any held escape keys
        }
    }

    #endregion

    [Fact]
    public void AnsiInput_ImplementsITestableInput ()
    {
        // Arrange & Act
        AnsiInput ansiInput = new ();

        // Assert
        Assert.IsAssignableFrom<ITestableInput<char>> (ansiInput);
    }

    [Fact]
    public void AnsiInput_InjectInput_EnqueuesCharacter ()
    {
        // Arrange
        AnsiInput ansiInput = new ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        ITestableInput<char> testableInput = ansiInput;

        // Act
        testableInput.InjectInput ('a');

        // Assert
        Assert.True (ansiInput.Peek ());
        List<char> read = ansiInput.Read ().ToList ();
        Assert.Single (read);
        Assert.Equal ('a', read [0]);
    }

    [Fact]
    public void AnsiInput_InjectInput_SupportsMultipleCharacters ()
    {
        // Arrange
        AnsiInput ansiInput = new ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        ITestableInput<char> testableInput = ansiInput;

        // Act
        testableInput.InjectInput ('a');
        testableInput.InjectInput ('b');
        testableInput.InjectInput ('c');

        // Assert
        List<char> read = ansiInput.Read ().ToList ();
        Assert.Equal (3, read.Count);
        Assert.Equal (new [] { 'a', 'b', 'c' }, read);
    }

    [Fact]
    public void AnsiInput_Peek_ReturnsTrueWhenTestInputAvailable ()
    {
        // Arrange
        AnsiInput ansiInput = new ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        ITestableInput<char> testableInput = ansiInput;

        // Act & Assert - Initially false
        Assert.False (ansiInput.Peek ());

        // Add input
        testableInput.InjectInput ('x');

        // Assert - Now true
        Assert.True (ansiInput.Peek ());
    }

    [Fact]
    public void AnsiInput_TestInput_HasPriorityOverRealInput ()
    {
        // This test verifies that test input is returned before any real terminal input
        // Since we can't easily simulate real terminal input in a unit test,
        // we just verify the order of test inputs

        // Arrange
        AnsiInput ansiInput = new ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        ITestableInput<char> testableInput = ansiInput;

        // Act - Add inputs in specific order
        testableInput.InjectInput ('1');
        testableInput.InjectInput ('2');
        testableInput.InjectInput ('3');

        // Assert - Should come out in FIFO order
        List<char> read = ansiInput.Read ().ToList ();
        Assert.Equal (new [] { '1', '2', '3' }, read);
    }

    [Fact]
    public void AnsiInputProcessor_InjectKeyDownEvent_WorksWithTestableInput ()
    {
        // Arrange
        AnsiInput ansiInput = new ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        var processor = new AnsiInputProcessor (queue);
        processor.InputImpl = ansiInput;

        List<Key> receivedKeys = [];
        processor.KeyDown += (_, k) => receivedKeys.Add (k);

        // Act
        processor.InjectKeyDownEvent (Key.A);

        // Simulate the input thread moving items from _testInput to InputBuffer
        SimulateInputThread (ansiInput, queue);

        // Process the queue
        processor.ProcessQueue ();

        // Assert
        Assert.Single (receivedKeys);
        Assert.Equal (Key.A, receivedKeys [0]);
    }

    [Fact]
    public void AnsiInputProcessor_InjectMouseEvent_GeneratesAnsiSequence ()
    {
        // Arrange
        AnsiInput ansiInput = new ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        var processor = new AnsiInputProcessor (queue);
        processor.InputImpl = ansiInput;

        List<Mouse> receivedMouse = [];
        processor.SyntheticMouseEvent += (_, m) => receivedMouse.Add (m);

        var mouse = new Mouse { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new Point (10, 20) };

        // Act
        processor.InjectMouseEvent (null, mouse);

        // Simulate the input thread
        SimulateInputThread (ansiInput, queue);

        // Process the queue
        processor.ProcessQueue ();

        // Assert - Should have received the mouse event back
        Assert.NotEmpty (receivedMouse);

        // Find the pressed event (original + clicked)
        Mouse? pressedEvent = receivedMouse.FirstOrDefault (m => m.Flags.HasFlag (MouseFlags.LeftButtonPressed));
        Assert.NotNull (pressedEvent);
        Assert.Equal (new Point (10, 20), pressedEvent.ScreenPosition);
    }

    [Fact]
    public void AnsiInputProcessor_InjectMouseEvent_SupportsRelease ()
    {
        // Arrange
        AnsiInput ansiInput = new ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        var processor = new AnsiInputProcessor (queue);
        processor.InputImpl = ansiInput;

        List<Mouse> receivedMouse = [];
        processor.SyntheticMouseEvent += (_, m) => receivedMouse.Add (m);

        var mouse = new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = new Point (10, 20) };

        // Act
        processor.InjectMouseEvent (null, mouse);

        // Simulate the input thread
        SimulateInputThread (ansiInput, queue);

        processor.ProcessQueue ();

        // Assert
        Mouse? releasedEvent = receivedMouse.FirstOrDefault (m => m.Flags.HasFlag (MouseFlags.LeftButtonReleased));
        Assert.NotNull (releasedEvent);
        Assert.Equal (new Point (10, 20), releasedEvent.ScreenPosition);
    }

    [Fact]
    public void AnsiInputProcessor_InjectMouseEvent_SupportsModifiers ()
    {
        // Arrange
        AnsiInput ansiInput = new ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        var processor = new AnsiInputProcessor (queue);
        processor.InputImpl = ansiInput;

        List<Mouse> receivedMouse = [];
        processor.SyntheticMouseEvent += (_, m) => { receivedMouse.Add (m); };

        // Test Ctrl+Alt (button code 24 for left button)
        var mouse = new Mouse { Flags = MouseFlags.LeftButtonPressed | MouseFlags.Ctrl | MouseFlags.Alt, ScreenPosition = new Point (5, 5) };

        // Act
        processor.InjectMouseEvent (null, mouse);

        // Debug: check what's in the queue
        List<char> inputChars = [];

        while (ansiInput.Peek ())
        {
            inputChars.AddRange (ansiInput.Read ());
        }
        string ansiSeq = new (inputChars.ToArray ());

        // Re-add to queue
        foreach (char ch in ansiSeq)
        {
            queue.Enqueue (ch);
        }

        processor.ProcessQueue ();

        // Assert
        Mouse? event1 = receivedMouse.FirstOrDefault (m => m.Flags.HasFlag (MouseFlags.LeftButtonPressed));
        Assert.NotNull (event1);
        Assert.True (event1.Flags.HasFlag (MouseFlags.Ctrl), $"Expected Ctrl flag, got: {event1.Flags}");
        Assert.True (event1.Flags.HasFlag (MouseFlags.Alt), $"Expected Alt flag, got: {event1.Flags}");
    }

    [Theory]
    [InlineData (MouseFlags.WheeledUp)]
    [InlineData (MouseFlags.WheeledDown)]

    // Note: WheeledLeft and WheeledRight (codes 68/69) have complex ANSI encoding with Shift+Ctrl variations
    // These are tested separately in AnsiMouseParserDebugTests
    public void AnsiInputProcessor_InjectMouseEvent_SupportsWheelEvents (MouseFlags wheelFlag)
    {
        // Arrange
        AnsiInput ansiInput = new ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        var processor = new AnsiInputProcessor (queue);
        processor.InputImpl = ansiInput;

        List<Mouse> receivedMouse = [];
        processor.SyntheticMouseEvent += (_, m) => receivedMouse.Add (m);

        var mouse = new Mouse { Flags = wheelFlag, ScreenPosition = new Point (15, 15) };

        // Act
        processor.InjectMouseEvent (null, mouse);

        // Simulate the input thread
        SimulateInputThread (ansiInput, queue);

        processor.ProcessQueue ();

        // Assert
        Mouse? wheelEvent = receivedMouse.FirstOrDefault (m => m.Flags.HasFlag (wheelFlag));
        Assert.NotNull (wheelEvent);
        Assert.Equal (new Point (15, 15), wheelEvent.ScreenPosition);
    }

    [Fact]
    public void Read_ReturnsInjectedTestInput ()
    {
        var input = new AnsiInput ();
        ConcurrentQueue<char> queue = new ();
        input.Initialize (queue);

        ITestableInput<char> testable = input;

        // Inject characters, including Ctrl+Z
        testable.InjectInput ('a');
        testable.InjectInput ('\x1A');

        // Peek should report available input
        Assert.True (input.Peek ());

        // Read should return injected characters in FIFO order
        List<char> read = input.Read ().ToList ();
        Assert.Equal (2, read.Count);
        Assert.Equal ('a', read [0]);
        Assert.Equal ('\x1A', read [1]);

        input.Dispose ();
    }

    #region AnsiInput InjectKeyDownEvent Tests

    [Fact]
    public void AnsiInput_InjectKeyDownEvent_AddsSingleKeyToQueue ()
    {
        // Arrange
        AnsiInput ansiInput = new ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        var processor = new AnsiInputProcessor (queue);
        processor.InputImpl = ansiInput;

        List<Key> receivedKeys = [];
        processor.KeyDown += (_, k) => receivedKeys.Add (k);

        Key key = Key.A;

        // Act
        processor.InjectKeyDownEvent (key);

        // Simulate the input thread moving items from _testInput to InputBuffer
        SimulateInputThread (ansiInput, queue);

        processor.ProcessQueue ();

        // Assert - Verify the key made it through
        Assert.Single (receivedKeys);
        Assert.Equal (key, receivedKeys [0]);
    }

    [Fact]
    public void AnsiInput_InjectKeyDownEvent_SupportsMultipleKeys ()
    {
        // Arrange
        AnsiInput ansiInput = new ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        var processor = new AnsiInputProcessor (queue);
        processor.InputImpl = ansiInput;

        Key [] keys = [Key.A, Key.B, Key.C, Key.Enter];
        List<Key> receivedKeys = [];
        processor.KeyDown += (_, k) => receivedKeys.Add (k);

        // Act
        foreach (Key key in keys)
        {
            processor.InjectKeyDownEvent (key);
        }

        SimulateInputThread (ansiInput, queue);
        processor.ProcessQueue ();

        // Assert
        Assert.Equal (keys.Length, receivedKeys.Count);
        Assert.Equal (keys, receivedKeys);
    }

    [Theory]
    [InlineData (KeyCode.A, false, false, false)]
    [InlineData (KeyCode.A, true, false, false)] // Shift+A
    [InlineData (KeyCode.A, false, true, false)] // Ctrl+A
    [InlineData (KeyCode.A, false, false, true)] // Alt+A
    // Note: Ctrl+Shift+Alt+A is not tested because ANSI doesn't have a standard way to represent
    // Shift with Ctrl combinations (Ctrl+A is 0x01 regardless of Shift state)
    public void AnsiInput_InjectKeyDownEvent_PreservesModifiers (KeyCode keyCode, bool shift, bool ctrl, bool alt)
    {
        // Arrange
        AnsiInput ansiInput = new ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        var processor = new AnsiInputProcessor (queue);
        processor.InputImpl = ansiInput;

        var key = new Key (keyCode);

        if (shift)
        {
            key = key.WithShift;
        }

        if (ctrl)
        {
            key = key.WithCtrl;
        }

        if (alt)
        {
            key = key.WithAlt;
        }

        Key? receivedKey = null;
        processor.KeyDown += (_, k) => receivedKey = k;

        // Act
        processor.InjectKeyDownEvent (key);
        SimulateInputThread (ansiInput, queue);

        // Alt combinations start with ESC, so they need escape handling
        if (alt)
        {
            ProcessQueueWithEscapeHandling (processor);
        }
        else
        {
            processor.ProcessQueue ();
        }

        // Assert
        Assert.NotNull (receivedKey);
        Assert.Equal (key.IsShift, receivedKey.IsShift);
        Assert.Equal (key.IsCtrl, receivedKey.IsCtrl);
        Assert.Equal (key.IsAlt, receivedKey.IsAlt);
        Assert.Equal (key.KeyCode, receivedKey.KeyCode);
    }

    [Theory]
    [InlineData (KeyCode.Enter)]
    [InlineData (KeyCode.Tab)]
    [InlineData (KeyCode.Esc)]
    [InlineData (KeyCode.Backspace)]
    [InlineData (KeyCode.Delete)]
    [InlineData (KeyCode.CursorUp)]
    [InlineData (KeyCode.CursorDown)]
    [InlineData (KeyCode.CursorLeft)]
    [InlineData (KeyCode.CursorRight)]
    [InlineData (KeyCode.F1)]
    [InlineData (KeyCode.F12)]
    public void AnsiInput_InjectKeyDownEvent_SupportsSpecialKeys (KeyCode keyCode)
    {
        // Arrange
        AnsiInput ansiInput = new ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        var processor = new AnsiInputProcessor (queue);
        processor.InputImpl = ansiInput;

        var key = new Key (keyCode);
        Key? receivedKey = null;
        processor.KeyDown += (_, k) => receivedKey = k;

        // Act
        processor.InjectKeyDownEvent (key);
        SimulateInputThread (ansiInput, queue);

        // Esc is special - the ANSI parser holds it waiting for potential escape sequences
        // We need to process with delay to let the parser release it after timeout
        if (keyCode == KeyCode.Esc)
        {
            ProcessQueueWithEscapeHandling (processor);
        }
        else
        {
            processor.ProcessQueue ();
        }

        // Assert
        Assert.NotNull (receivedKey);
        Assert.Equal (key.KeyCode, receivedKey.KeyCode);
    }

    [Fact]
    public void AnsiInput_InjectKeyDownEvent_RaisesKeyDownEvent ()
    {
        // Arrange
        AnsiInput ansiInput = new ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        var processor = new AnsiInputProcessor (queue);
        processor.InputImpl = ansiInput;

        var keyDownCount = 0;
        processor.KeyDown += (_, _) => keyDownCount++;

        // Act
        processor.InjectKeyDownEvent (Key.A);
        SimulateInputThread (ansiInput, queue);
        processor.ProcessQueue ();

        Assert.Equal (1, keyDownCount);
    }

    [Fact]
    public void AnsiInput_ProcessQueue_ParsesKittySequence_FromInjectedCharacters ()
    {
        AnsiInput ansiInput = new ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        AnsiInputProcessor processor = new (queue);
        processor.InputImpl = ansiInput;

        Key? receivedKey = null;
        processor.KeyDown += (_, k) => receivedKey = k;

        foreach (char ch in "\u001b[65;2u")
        {
            ((ITestableInput<char>)ansiInput).InjectInput (ch);
        }

        SimulateInputThread (ansiInput, queue);
        processor.ProcessQueue ();

        Assert.NotNull (receivedKey);
        Assert.Equal (Key.A.WithShift, receivedKey);
    }

    [Fact]
    public void AnsiInput_ProcessQueue_ParsesKittyNavigationSequence_FromInjectedCharacters ()
    {
        AnsiInput ansiInput = new ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        AnsiInputProcessor processor = new (queue);
        processor.InputImpl = ansiInput;

        Key? receivedKey = null;
        processor.KeyDown += (_, k) => receivedKey = k;

        foreach (char ch in "\u001b[57349;6u")
        {
            ((ITestableInput<char>)ansiInput).InjectInput (ch);
        }

        SimulateInputThread (ansiInput, queue);
        processor.ProcessQueue ();

        Assert.NotNull (receivedKey);
        Assert.Equal (Key.PageDown.WithCtrl.WithShift, receivedKey);
    }

    // Claude - Opus 5.5
    [Fact]
    public void AnsiInput_ProcessQueue_CombinesSurrogatePair_IntoSingleNonBmpKey ()
    {
        AnsiInput ansiInput = new ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        AnsiInputProcessor processor = new (queue);
        processor.InputImpl = ansiInput;

        List<Key> receivedKeys = [];
        processor.KeyDown += (_, k) => receivedKeys.Add (k);

        foreach (char ch in "a\U0001F642b")
        {
            ((ITestableInput<char>)ansiInput).InjectInput (ch);
        }

        SimulateInputThread (ansiInput, queue);
        processor.ProcessQueue ();

        Assert.Equal (3, receivedKeys.Count);
        Assert.Equal (Key.A, receivedKeys [0]);
        Assert.Equal ((KeyCode)0x1F642, receivedKeys [1].KeyCode);
        Assert.Equal (Key.B, receivedKeys [2]);
        Assert.DoesNotContain (receivedKeys, k => k.KeyCode == (KeyCode)0xFFFD);
    }

    // Claude - Opus 5.5
    [Fact]
    public void AnsiInput_ProcessQueue_CombinesSurrogatePair_SplitAcrossReads ()
    {
        AnsiInput ansiInput = new ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        AnsiInputProcessor processor = new (queue);
        processor.InputImpl = ansiInput;

        List<Key> receivedKeys = [];
        processor.KeyDown += (_, k) => receivedKeys.Add (k);

        // A UTF-16 console read (ReadConsoleW) may end between the high and low surrogate.
        ((ITestableInput<char>)ansiInput).InjectInput ('\uD83D');
        SimulateInputThread (ansiInput, queue);
        processor.ProcessQueue ();

        Assert.Empty (receivedKeys);

        ((ITestableInput<char>)ansiInput).InjectInput ('\uDE42');
        SimulateInputThread (ansiInput, queue);
        processor.ProcessQueue ();

        Assert.Single (receivedKeys);
        Assert.Equal ((KeyCode)0x1F642, receivedKeys [0].KeyCode);
    }

    #endregion

    #region Mouse Event Sequencing Tests

    [Fact]
    public void AnsiInput_InjectMouseEvent_HandlesCompleteClickSequence ()
    {
        // Arrange
        AnsiInput ansiInput = new ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        AnsiInputProcessor processor = new (queue);
        processor.InputImpl = ansiInput;

        List<Mouse> receivedEvents = [];
        processor.SyntheticMouseEvent += (_, e) => receivedEvents.Add (e);

        // Act - Simulate a complete click: press → release
        processor.InjectMouseEvent (null, new Mouse { Position = new Point (10, 5), Flags = MouseFlags.LeftButtonPressed });

        processor.InjectMouseEvent (null, new Mouse { Position = new Point (10, 5), Flags = MouseFlags.LeftButtonReleased });

        SimulateInputThread (ansiInput, queue);
        processor.ProcessQueue ();

        // Assert - Process() emits Pressed and Released immediately (clicks are deferred)
        Assert.Contains (receivedEvents, e => e.Flags.HasFlag (MouseFlags.LeftButtonPressed));
        Assert.Contains (receivedEvents, e => e.Flags.HasFlag (MouseFlags.LeftButtonReleased));

        // We should also see the synthetic Clicked event
        Assert.Contains (receivedEvents, e => e.Flags.HasFlag (MouseFlags.LeftButtonClicked));
        Assert.Equal (3, receivedEvents.Count);
    }

    [Theory]
    [InlineData (MouseFlags.WheeledUp)]
    [InlineData (MouseFlags.WheeledDown)]
    [InlineData (MouseFlags.WheeledLeft)]
    [InlineData (MouseFlags.WheeledRight)]
    public void AnsiInput_InjectMouseEvent_Wheel_Events (MouseFlags wheelEvent)
    {
        // Arrange
        AnsiInput ansiInput = new ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        AnsiInputProcessor processor = new (queue);
        processor.InputImpl = ansiInput;

        List<Mouse> receivedEvents = [];
        processor.SyntheticMouseEvent += (_, e) => receivedEvents.Add (e);

        // Act - Simulate a wheel event
        processor.InjectMouseEvent (null, new Mouse { Position = new Point (10, 5), Flags = wheelEvent });

        SimulateInputThread (ansiInput, queue);
        processor.ProcessQueue ();

        // Assert
        Assert.Contains (receivedEvents, e => e.Flags.HasFlag (wheelEvent));
        Assert.Single (receivedEvents);

        // Note: ANSI codes 68 and 69 (horizontal wheel) always include Shift flag per ANSI spec
        if (wheelEvent is MouseFlags.WheeledLeft or MouseFlags.WheeledRight)
        {
            Mouse wheelEventReceived = receivedEvents.First (e => e.Flags.HasFlag (wheelEvent));

            Assert.True (wheelEventReceived.Flags.HasFlag (MouseFlags.Shift),
                         $"Horizontal wheel events should include Shift flag, got: {wheelEventReceived.Flags}");
        }
    }

    #endregion

    #region Encoding Tests (Issue #4582)

    // Claude - Opus 5.5
    [Fact]
    public void AnsiInput_ProcessQueue_AccentedChars_ProduceMatchingKeys ()
    {
        // Issue #4582: accented characters became U+FFFD when Windows input bytes were decoded with the
        // wrong code page. Windows now reads UTF-16 code units directly (ReadConsoleW), so the characters
        // must reach KeyDown unchanged.
        const string ACCENTED = "áéíóúñüàèìòùâêîôû";

        AnsiInput ansiInput = new ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        AnsiInputProcessor processor = new (queue);
        processor.InputImpl = ansiInput;

        List<Key> receivedKeys = [];
        processor.KeyDown += (_, k) => receivedKeys.Add (k);

        foreach (char ch in ACCENTED)
        {
            ((ITestableInput<char>)ansiInput).InjectInput (ch);
        }

        SimulateInputThread (ansiInput, queue);
        processor.ProcessQueue ();

        Assert.Equal (ACCENTED.Length, receivedKeys.Count);

        for (var i = 0; i < ACCENTED.Length; i++)
        {
            Assert.Equal (new Key (ACCENTED [i]), receivedKeys [i]);
        }
    }

    #endregion
}
