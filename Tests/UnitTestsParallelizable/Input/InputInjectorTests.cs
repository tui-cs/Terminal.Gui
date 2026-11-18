using System.Collections.Concurrent;
using DriverTests.Input;

namespace InputTests;

/// <summary>
///     Comprehensive unit tests for <see cref="InputInjector"/>.
///     Tests the high-level input injection API that simplifies testing.
/// </summary>
[Trait ("Category", "Input")]
[Trait ("Category", "InputInjection")]
public class InputInjectorTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_Succeeds ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        VirtualTimeProvider timeProvider = new ();

        // Act
        InputInjector injector = new (processor, timeProvider);

        // Assert
        Assert.NotNull (injector);
    }

    [Fact]
    public void Constructor_WithTestSource_Succeeds ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        VirtualTimeProvider timeProvider = new ();
        TestInputSource testSource = new (timeProvider);

        // Act
        InputInjector injector = new (processor, timeProvider, testSource);

        // Assert
        Assert.NotNull (injector);
    }

    #endregion

    #region InjectKey Tests - Direct Mode

    [Fact]
    public void InjectKey_DirectMode_RaisesKeyDownEvent ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        VirtualTimeProvider timeProvider = new ();
        InputInjector injector = new (processor, timeProvider);

        List<Key> receivedKeys = [];
        processor.KeyDown += (_, key) => receivedKeys.Add (key);

        InputInjectionOptions options = new () { Mode = InputInjectionMode.Direct };

        // Act
        injector.InjectKey (Key.A, options);

        // Assert - Should raise exactly 1 KeyDown event in Direct mode
        Assert.Single (receivedKeys);
        Assert.Equal (Key.A, receivedKeys [0]);
    }

    [Fact]
    public void InjectKey_DirectMode_MultipleKeys_RaisesAllEvents ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        VirtualTimeProvider timeProvider = new ();
        InputInjector injector = new (processor, timeProvider);

        List<Key> receivedKeys = [];
        processor.KeyDown += (_, key) => receivedKeys.Add (key);

        InputInjectionOptions options = new () { Mode = InputInjectionMode.Direct };

        // Act
        injector.InjectKey (Key.A, options);
        injector.InjectKey (Key.B, options);
        injector.InjectKey (Key.C, options);

        // Assert - Should raise exactly 3 KeyDown events
        Assert.Equal (3, receivedKeys.Count);
        Assert.Equal (Key.A, receivedKeys [0]);
        Assert.Equal (Key.B, receivedKeys [1]);
        Assert.Equal (Key.C, receivedKeys [2]);
    }

    [Fact]
    public void InjectKey_DefaultOptions_UsesDirectMode ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        VirtualTimeProvider timeProvider = new ();
        InputInjector injector = new (processor, timeProvider);

        List<Key> receivedKeys = [];
        processor.KeyDown += (_, key) => receivedKeys.Add (key);

        // Act - Call without options (should default to Direct mode)
        injector.InjectKey (Key.A);

        // Assert - Should raise event (Direct mode behavior)
        Assert.Single (receivedKeys);
        Assert.Equal (Key.A, receivedKeys [0]);
    }

    #endregion

    #region InjectKey Tests - Pipeline Mode

    [Fact]
    public void InjectKey_Pipeline_AutoProcess_False_AccentedKeys_RaisesAllEvents ()
    {
        // Arrange
        VirtualTimeProvider timeProvider = new ();
        timeProvider.SetTime (new DateTime (2025, 1, 1, 12, 0, 0));

        using IApplication app = Application.Create (timeProvider);
        app.Init (DriverRegistry.Names.ANSI);

        List<Key> receivedKeys = [];
        app.Keyboard.KeyDown += (_, key) => receivedKeys.Add (key);

        // Explicit Pipeline mode
        TestInputSource testSource = new (timeProvider);
        InputInjector injector = new (app.Driver?.GetInputProcessor ()!, timeProvider, testSource);

        InputInjectionOptions options = new () { Mode = InputInjectionMode.Pipeline, AutoProcess = false, TimeProvider = timeProvider };

        // Act
        injector.InjectKey (new Key ('á'), options);
        injector.InjectKey (new Key ('é'), options);
        injector.InjectKey (new Key ('í'), options);
        injector.InjectKey (new Key ('ó'), options);
        injector.InjectKey (new Key ('ú'), options);
        injector.InjectKey (new Key ('à'), options);
        injector.InjectKey (new Key ('è'), options);
        injector.InjectKey (new Key ('ì'), options);
        injector.InjectKey (new Key ('ò'), options);
        injector.InjectKey (new Key ('ù'), options);
        injector.InjectKey (new Key ('â'), options);
        injector.InjectKey (new Key ('ê'), options);
        injector.InjectKey (new Key ('î'), options);
        injector.InjectKey (new Key ('ô'), options);
        injector.InjectKey (new Key ('û'), options);
        injector.InjectKey (new Key ('ã'), options);
        injector.InjectKey (new Key ('õ'), options);
        injector.InjectKey (new Key ('Á'), options);
        injector.InjectKey (new Key ('É'), options);
        injector.InjectKey (new Key ('Í'), options);
        injector.InjectKey (new Key ('Ó'), options);
        injector.InjectKey (new Key ('Ú'), options);
        injector.InjectKey (new Key ('À'), options);
        injector.InjectKey (new Key ('È'), options);
        injector.InjectKey (new Key ('Ì'), options);
        injector.InjectKey (new Key ('Ò'), options);
        injector.InjectKey (new Key ('Ù'), options);
        injector.InjectKey (new Key ('Â'), options);
        injector.InjectKey (new Key ('Ê'), options);
        injector.InjectKey (new Key ('Î'), options);
        injector.InjectKey (new Key ('Ô'), options);
        injector.InjectKey (new Key ('Û'), options);
        injector.InjectKey (new Key ('Ã'), options);
        injector.InjectKey (new Key ('Õ'), options);

        injector.ProcessQueue ();

        // Assert - Should raise exactly 3 KeyDown events
        Assert.Equal (34, receivedKeys.Count);
        Assert.Equal (new Key ('á'), receivedKeys [0]);
        Assert.Equal (new Key ('é'), receivedKeys [1]);
        Assert.Equal (new Key ('í'), receivedKeys [2]);
        Assert.Equal (new Key ('ó'), receivedKeys [3]);
        Assert.Equal (new Key ('ú'), receivedKeys [4]);
        Assert.Equal (new Key ('à'), receivedKeys [5]);
        Assert.Equal (new Key ('è'), receivedKeys [6]);
        Assert.Equal (new Key ('ì'), receivedKeys [7]);
        Assert.Equal (new Key ('ò'), receivedKeys [8]);
        Assert.Equal (new Key ('ù'), receivedKeys [9]);
        Assert.Equal (new Key ('â'), receivedKeys [10]);
        Assert.Equal (new Key ('ê'), receivedKeys [11]);
        Assert.Equal (new Key ('î'), receivedKeys [12]);
        Assert.Equal (new Key ('ô'), receivedKeys [13]);
        Assert.Equal (new Key ('û'), receivedKeys [14]);
        Assert.Equal (new Key ('ã'), receivedKeys [15]);
        Assert.Equal (new Key ('õ'), receivedKeys [16]);
        Assert.Equal (new Key ('Á'), receivedKeys [17]);
        Assert.Equal (new Key ('É'), receivedKeys [18]);
        Assert.Equal (new Key ('Í'), receivedKeys [19]);
        Assert.Equal (new Key ('Ó'), receivedKeys [20]);
        Assert.Equal (new Key ('Ú'), receivedKeys [21]);
        Assert.Equal (new Key ('À'), receivedKeys [22]);
        Assert.Equal (new Key ('È'), receivedKeys [23]);
        Assert.Equal (new Key ('Ì'), receivedKeys [24]);
        Assert.Equal (new Key ('Ò'), receivedKeys [25]);
        Assert.Equal (new Key ('Ù'), receivedKeys [26]);
        Assert.Equal (new Key ('Â'), receivedKeys [27]);
        Assert.Equal (new Key ('Ê'), receivedKeys [28]);
        Assert.Equal (new Key ('Î'), receivedKeys [29]);
        Assert.Equal (new Key ('Ô'), receivedKeys [30]);
        Assert.Equal (new Key ('Û'), receivedKeys [31]);
        Assert.Equal (new Key ('Ã'), receivedKeys [32]);
        Assert.Equal (new Key ('Õ'), receivedKeys [33]);
    }

    [Fact]
    public void InjectKey_PipelineMode_AutoProcess_True_MultipleKeys_RaisesAllEvents ()
    {
        // Arrange
        VirtualTimeProvider timeProvider = new ();
        timeProvider.SetTime (new DateTime (2025, 1, 1, 12, 0, 0));

        using IApplication app = Application.Create (timeProvider);
        app.Init (DriverRegistry.Names.ANSI);

        List<Key> receivedKeys = [];
        app.Keyboard.KeyDown += (_, key) => receivedKeys.Add (key);

        // Explicit Pipeline mode
        TestInputSource testSource = new (timeProvider);
        InputInjector injector = new (app.Driver?.GetInputProcessor ()!, timeProvider, testSource);

        InputInjectionOptions options = new () { Mode = InputInjectionMode.Pipeline, AutoProcess = true, TimeProvider = timeProvider };

        // Act
        injector.InjectKey (Key.A, options);
        injector.InjectKey (Key.B, options);
        injector.InjectKey (Key.C, options);

        // Assert - Should raise exactly 3 KeyDown events
        Assert.Equal (3, receivedKeys.Count);
        Assert.Equal (Key.A, receivedKeys [0]);
        Assert.Equal (Key.B, receivedKeys [1]);
        Assert.Equal (Key.C, receivedKeys [2]);
    }

    #endregion

    #region InjectMouse Tests - Direct Mode

    [Fact]
    public void InjectMouse_DirectMode_RaisesMouseEventParsed ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        VirtualTimeProvider timeProvider = new ();
        InputInjector injector = new (processor, timeProvider);

        List<Mouse> receivedEvents = [];
        processor.MouseEventParsed += (_, mouse) => receivedEvents.Add (mouse);

        Mouse testMouse = new () { ScreenPosition = new Point (10, 10), Flags = MouseFlags.LeftButtonPressed };

        InputInjectionOptions options = new () { Mode = InputInjectionMode.Direct };

        // Act
        injector.InjectMouse (testMouse, options);

        // Assert - Should raise exactly 1 MouseEventParsed event
        Assert.Single (receivedEvents);
        Assert.Equal (testMouse.ScreenPosition, receivedEvents [0].ScreenPosition);
        Assert.Equal (testMouse.Flags, receivedEvents [0].Flags);
    }

    [Fact]
    public void InjectMouse_DirectMode_RaisesSyntheticMouseEvent ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        VirtualTimeProvider timeProvider = new ();
        InputInjector injector = new (processor, timeProvider);

        List<Mouse> receivedEvents = [];
        processor.SyntheticMouseEvent += (_, mouse) => receivedEvents.Add (mouse);

        Mouse testMouse = new () { ScreenPosition = new Point (10, 10), Flags = MouseFlags.LeftButtonPressed };

        InputInjectionOptions options = new () { Mode = InputInjectionMode.Direct };

        // Act
        injector.InjectMouse (testMouse, options);

        // Assert - RaiseMouseEventParsed internally calls RaiseSyntheticMouseEvent
        // MouseInterpreter yields original, so we get at least 1 event
        Assert.True (receivedEvents.Count >= 1);
        Assert.Equal (testMouse.ScreenPosition, receivedEvents [0].ScreenPosition);
    }

    [Fact]
    public void InjectMouse_DirectMode_PressAndRelease_GeneratesClickEvent ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        VirtualTimeProvider timeProvider = new ();
        InputInjector injector = new (processor, timeProvider);

        List<Mouse> syntheticEvents = [];
        processor.SyntheticMouseEvent += (_, mouse) => syntheticEvents.Add (mouse);

        Mouse pressEvent = new () { ScreenPosition = new Point (10, 10), Flags = MouseFlags.LeftButtonPressed };

        Mouse releaseEvent = new () { ScreenPosition = new Point (10, 10), Flags = MouseFlags.LeftButtonReleased };

        InputInjectionOptions options = new () { Mode = InputInjectionMode.Direct };

        // Act
        injector.InjectMouse (pressEvent, options);
        injector.InjectMouse (releaseEvent, options);

        // Assert - Should get exactly 3 events: Press, Release, Click
        Assert.Equal (3, syntheticEvents.Count);
        Assert.Equal (MouseFlags.LeftButtonPressed, syntheticEvents [0].Flags);
        Assert.Equal (MouseFlags.LeftButtonReleased, syntheticEvents [1].Flags);
        Assert.True (syntheticEvents [2].Flags.HasFlag (MouseFlags.LeftButtonClicked));
    }

    [Fact]
    public void InjectMouse_SetsTimestampIfNull ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        VirtualTimeProvider timeProvider = new ();
        timeProvider.SetTime (new DateTime (2025, 1, 1, 12, 0, 0));
        InputInjector injector = new (processor, timeProvider);

        Mouse mouseWithoutTimestamp = new () { ScreenPosition = new Point (10, 10), Flags = MouseFlags.LeftButtonPressed };

        Assert.Null (mouseWithoutTimestamp.Timestamp);

        InputInjectionOptions options = new () { Mode = InputInjectionMode.Direct };

        // Act
        injector.InjectMouse (mouseWithoutTimestamp, options);

        // Assert - Timestamp should be set to virtual time
        Assert.NotNull (mouseWithoutTimestamp.Timestamp);
        Assert.Equal (new DateTime (2025, 1, 1, 12, 0, 0), mouseWithoutTimestamp.Timestamp);
    }

    [Fact]
    public void InjectMouse_PreservesExistingTimestamp ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        VirtualTimeProvider timeProvider = new ();
        InputInjector injector = new (processor, timeProvider);

        DateTime specificTime = new (2025, 1, 1, 12, 30, 0);

        Mouse mouseWithTimestamp = new () { ScreenPosition = new Point (10, 10), Flags = MouseFlags.LeftButtonPressed, Timestamp = specificTime };

        InputInjectionOptions options = new () { Mode = InputInjectionMode.Direct };

        // Act
        injector.InjectMouse (mouseWithTimestamp, options);

        // Assert - Original timestamp should be preserved
        Assert.Equal (specificTime, mouseWithTimestamp.Timestamp);
    }

    #endregion

    #region AutoProcess Tests

    [Fact]
    public void InjectKey_AutoProcessTrue_ProcessesImmediately ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        VirtualTimeProvider timeProvider = new ();
        InputInjector injector = new (processor, timeProvider);

        List<Key> receivedKeys = [];
        processor.KeyDown += (_, key) => receivedKeys.Add (key);

        InputInjectionOptions options = new () { Mode = InputInjectionMode.Direct, AutoProcess = true };

        // Act
        injector.InjectKey (Key.A, options);

        // Assert - Event should be raised immediately
        Assert.Single (receivedKeys);
    }

    [Fact]
    public void InjectKey_AutoProcessFalse_RequiresManualProcessQueue ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        VirtualTimeProvider timeProvider = new ();
        IInputInjector injector = new InputInjector (processor, timeProvider);

        List<Key> receivedKeys = [];
        processor.KeyDown += (_, key) => receivedKeys.Add (key);

        InputInjectionOptions options = new () { Mode = InputInjectionMode.Direct, AutoProcess = false };

        // Act
        injector.InjectKey (Key.A, options);

        // Assert - Event should be raised even with AutoProcess=false in Direct mode
        // (Direct mode raises events immediately, doesn't use queue)
        Assert.Single (receivedKeys);
    }

    [Fact]
    public void ProcessQueue_CanBeCalledManually ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        VirtualTimeProvider timeProvider = new ();
        IInputInjector injector = new InputInjector (processor, timeProvider);

        // Act & Assert - Should not throw
        injector.ProcessQueue ();
    }

    #endregion

    #region InjectSequence Tests

    [Fact]
    public void InjectSequence_EmptySequence_DoesNotRaiseEvents ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        VirtualTimeProvider timeProvider = new ();
        IInputInjector injector = new InputInjector (processor, timeProvider);

        List<Key> receivedKeys = [];
        processor.KeyDown += (_, key) => receivedKeys.Add (key);

        InputInjectionEvent [] emptySequence = [];

        // Act
        injector.InjectSequence (emptySequence);

        // Assert - No events should be raised
        Assert.Empty (receivedKeys);
    }

    [Fact]
    public void InjectSequence_MultipleKeyEvents_RaisesAllEvents ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        VirtualTimeProvider timeProvider = new ();
        IInputInjector injector = new InputInjector (processor, timeProvider);

        List<Key> receivedKeys = [];
        processor.KeyDown += (_, key) => receivedKeys.Add (key);

        InputInjectionEvent [] sequence = [new KeyInjectionEvent (Key.A), new KeyInjectionEvent (Key.B), new KeyInjectionEvent (Key.C)];

        // Act
        injector.InjectSequence (sequence);

        // Assert - Should raise exactly 3 KeyDown events
        Assert.Equal (3, receivedKeys.Count);
        Assert.Equal (Key.A, receivedKeys [0]);
        Assert.Equal (Key.B, receivedKeys [1]);
        Assert.Equal (Key.C, receivedKeys [2]);
    }

    [Fact]
    public void InjectSequence_WithDelays_AdvancesVirtualTime ()
    {
        // Arrange
        VirtualTimeProvider timeProvider = new ();
        timeProvider.SetTime (new DateTime (2025, 1, 1, 12, 0, 0));

        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        IInputInjector injector = new InputInjector (processor, timeProvider);

        List<Key> receivedKeys = [];
        List<DateTime> timestamps = [];

        processor.KeyDown += (_, key) =>
                             {
                                 receivedKeys.Add (key);
                                 timestamps.Add (timeProvider.Now);
                             };

        InputInjectionEvent [] sequence =
        [
            new KeyInjectionEvent (Key.A),
            new KeyInjectionEvent (Key.B) { Delay = TimeSpan.FromMilliseconds (100) },
            new KeyInjectionEvent (Key.C) { Delay = TimeSpan.FromMilliseconds (200) }
        ];

        // Act
        injector.InjectSequence (sequence);

        // Assert - Time should advance according to delays
        Assert.Equal (3, receivedKeys.Count);
        Assert.Equal (new DateTime (2025, 1, 1, 12, 0, 0, 0), timestamps [0]);
        Assert.Equal (new DateTime (2025, 1, 1, 12, 0, 0, 100), timestamps [1]);
        Assert.Equal (new DateTime (2025, 1, 1, 12, 0, 0, 300), timestamps [2]);
    }

    [Fact]
    public void InjectSequence_MixedKeyAndMouseEvents_ProcessesAll ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        VirtualTimeProvider timeProvider = new ();
        IInputInjector injector = new InputInjector (processor, timeProvider);

        List<Key> receivedKeys = [];
        List<Mouse> receivedMouse = [];
        processor.KeyDown += (_, key) => receivedKeys.Add (key);
        processor.MouseEventParsed += (_, mouse) => receivedMouse.Add (mouse);

        InputInjectionEvent [] sequence =
        [
            new KeyInjectionEvent (Key.A),
            new MouseInjectionEvent (new Mouse { ScreenPosition = new Point (5, 5), Flags = MouseFlags.LeftButtonPressed }),
            new KeyInjectionEvent (Key.B)
        ];

        // Act
        injector.InjectSequence (sequence);

        // Assert - Should process both key and mouse events
        Assert.Equal (2, receivedKeys.Count);
        Assert.Equal (Key.A, receivedKeys [0]);
        Assert.Equal (Key.B, receivedKeys [1]);
        Assert.Single (receivedMouse);
        Assert.Equal (new Point (5, 5), receivedMouse [0].ScreenPosition);
    }

    #endregion

    #region Virtual Time Tests

    [Fact]
    public void InjectKey_UsesVirtualTimeProvider ()
    {
        // Arrange
        VirtualTimeProvider timeProvider = new ();
        timeProvider.SetTime (new DateTime (2025, 1, 1, 12, 0, 0));

        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        IInputInjector injector = new InputInjector (processor, timeProvider);

        // Act - Advance time
        timeProvider.Advance (TimeSpan.FromMinutes (5));

        // Assert - Virtual time should be advanced
        Assert.Equal (new DateTime (2025, 1, 1, 12, 5, 0), timeProvider.Now);
    }

    [Fact]
    public void InjectMouse_WithVirtualTime_UsesVirtualTimestamp ()
    {
        // Arrange
        VirtualTimeProvider timeProvider = new ();
        timeProvider.SetTime (new DateTime (2025, 1, 1, 12, 0, 0));

        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        IInputInjector injector = new InputInjector (processor, timeProvider);

        Mouse mouseEvent = new () { ScreenPosition = new Point (10, 10), Flags = MouseFlags.LeftButtonPressed };

        InputInjectionOptions options = new () { Mode = InputInjectionMode.Direct };

        // Act
        injector.InjectMouse (mouseEvent, options);

        // Assert - Timestamp should match virtual time
        Assert.NotNull (mouseEvent.Timestamp);
        Assert.Equal (new DateTime (2025, 1, 1, 12, 0, 0), mouseEvent.Timestamp);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void InjectKey_NullOptions_UsesDefaults ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        VirtualTimeProvider timeProvider = new ();
        IInputInjector injector = new InputInjector (processor, timeProvider);

        List<Key> receivedKeys = [];
        processor.KeyDown += (_, key) => receivedKeys.Add (key);

        // Act - Pass null options
        injector.InjectKey (Key.A);

        // Assert - Should use default options (Direct mode, AutoProcess=true)
        Assert.Single (receivedKeys);
    }

    [Fact]
    public void InjectMouse_NullOptions_UsesDefaults ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        VirtualTimeProvider timeProvider = new ();
        IInputInjector injector = new InputInjector (processor, timeProvider);

        List<Mouse> receivedEvents = [];
        processor.MouseEventParsed += (_, mouse) => receivedEvents.Add (mouse);

        Mouse testMouse = new () { ScreenPosition = new Point (10, 10), Flags = MouseFlags.LeftButtonPressed };

        // Act - Pass null options
        injector.InjectMouse (testMouse);

        // Assert - Should use default options
        Assert.Single (receivedEvents);
    }

    [Fact]
    public void InjectSequence_NullOptions_UsesDefaults ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        TestInputProcessor processor = new (queue);
        VirtualTimeProvider timeProvider = new ();
        IInputInjector injector = new InputInjector (processor, timeProvider);

        List<Key> receivedKeys = [];
        processor.KeyDown += (_, key) => receivedKeys.Add (key);

        InputInjectionEvent [] sequence = [new KeyInjectionEvent (Key.A)];

        // Act - Pass null options
        injector.InjectSequence (sequence);

        // Assert - Should use default options
        Assert.Single (receivedKeys);
    }

    #endregion

    #region Pipeline Mode - Escape Sequence Tests

    // Claude - Opus 4.6
    [Fact]
    public void InjectKey_PipelineMode_EscapeSequences_RoundTrip ()
    {
        // Arrange - Tests that multi-char escape sequences (F1, cursor keys) round-trip
        // through Pipeline mode with VirtualTimeProvider. This proves the stale-escape fix works.
        VirtualTimeProvider timeProvider = new ();
        timeProvider.SetTime (new DateTime (2025, 1, 1, 12, 0, 0));

        using IApplication app = Application.Create (timeProvider);
        app.Init (DriverRegistry.Names.ANSI);

        List<Key> receivedKeys = [];
        app.Keyboard.KeyDown += (_, key) => receivedKeys.Add (key);

        TestInputSource testSource = new (timeProvider);
        InputInjector injector = new (app.Driver?.GetInputProcessor ()!, timeProvider, testSource);

        InputInjectionOptions options = new () { Mode = InputInjectionMode.Pipeline, AutoProcess = true, TimeProvider = timeProvider };

        // Act - Inject keys that encode as multi-char ANSI escape sequences
        injector.InjectKey (Key.F1, options);
        injector.InjectKey (Key.CursorUp, options);
        injector.InjectKey (Key.CursorDown, options);
        injector.InjectKey (Key.Home, options);
        injector.InjectKey (Key.End, options);
        injector.InjectKey (Key.Delete, options);
        injector.InjectKey (Key.PageUp, options);

        // Assert - All keys should round-trip through encode → parse pipeline
        Assert.Equal (7, receivedKeys.Count);
        Assert.Equal (Key.F1, receivedKeys [0]);
        Assert.Equal (Key.CursorUp, receivedKeys [1]);
        Assert.Equal (Key.CursorDown, receivedKeys [2]);
        Assert.Equal (Key.Home, receivedKeys [3]);
        Assert.Equal (Key.End, receivedKeys [4]);
        Assert.Equal (Key.Delete, receivedKeys [5]);
        Assert.Equal (Key.PageUp, receivedKeys [6]);
    }

    #endregion
}
