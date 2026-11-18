# Input Injection

> **Quick Start:** Most developers only need to use `app.InjectKey()` and `app.InjectMouse()`. See [Simple Examples](#simple-examples) below.

## Overview

Input injection allows tests to simulate user input (keyboard and mouse) without requiring actual hardware. The system provides:

- **Single-call API** - `app.InjectKey(Key.A)` handles everything
- **Virtual time control** - Tests run instantly, no real delays
- **Two modes** - Direct (default, fast) and Pipeline (full ANSI encoding/parsing)
- **Deterministic behavior** - Same input → same result, every time

## Simple Examples

### Basic Keyboard Input

```csharp
using IApplication app = Application.Create();
app.Init(DriverRegistry.Names.ANSI);

// Subscribe to key events
app.Keyboard.KeyDown += (s, e) => Console.WriteLine($"Key: {e}");

// Inject keys
app.InjectKey(Key.A);
app.InjectKey(Key.Enter);
app.InjectKey(Key.Esc);
```

### Basic Mouse Input

**Recommended: Use helper methods for common click patterns**

```csharp
using IApplication app = Application.Create();
app.Init(DriverRegistry.Names.ANSI);

// Subscribe to mouse events
app.Mouse.MouseEvent += (s, e) => Console.WriteLine($"Mouse: {e.Flags} at {e.ScreenPosition}");

// Inject a left click using the helper method
app.InjectSequence(InputInjectionExtensions.LeftButtonClick(new Point(10, 5)));

// Inject a right click
app.InjectSequence(InputInjectionExtensions.RightButtonClick(new Point(10, 5)));

// Inject a double-click
app.InjectSequence(InputInjectionExtensions.LeftButtonDoubleClick(new Point(10, 5)));
```

**Alternative: Manual event creation (for advanced scenarios)**

```csharp
// Inject mouse click manually (two separate events)
app.InjectMouse(new() { 
    ScreenPosition = new(10, 5), 
    Flags = MouseFlags.LeftButtonPressed 
});

app.InjectMouse(new() { 
    ScreenPosition = new(10, 5), 
    Flags = MouseFlags.LeftButtonReleased 
});
```

### Testing with Virtual Time

```csharp
// Create virtual time provider for deterministic timing
VirtualTimeProvider time = new();
time.SetTime(new DateTime(2025, 1, 1, 12, 0, 0));

using IApplication app = Application.Create(time);
app.Init(DriverRegistry.Names.ANSI);

// First click at T+0
app.InjectMouse(new() { 
    ScreenPosition = new(10, 5), 
    Flags = MouseFlags.LeftButtonPressed,
    Timestamp = time.Now
});

time.Advance(TimeSpan.FromMilliseconds(50));

app.InjectMouse(new() { 
    ScreenPosition = new(10, 5), 
    Flags = MouseFlags.LeftButtonReleased,
    Timestamp = time.Now
});

// Second click at T+300 (within double-click threshold)
time.Advance(TimeSpan.FromMilliseconds(250));

app.InjectMouse(new() { 
    ScreenPosition = new(10, 5), 
    Flags = MouseFlags.LeftButtonPressed,
    Timestamp = time.Now
});

time.Advance(TimeSpan.FromMilliseconds(50));

app.InjectMouse(new() { 
    ScreenPosition = new(10, 5), 
    Flags = MouseFlags.LeftButtonReleased,
    Timestamp = time.Now
});

// Double-click was detected!
```

### Mouse Click Helper Methods

For simplified mouse click injection, use the <xref:Terminal.Gui.Testing.InputInjectionExtensions> helper methods. These encapsulate common click patterns and reduce boilerplate code.

#### Available Helpers

**`LeftButtonClick(Point p)`** - Single left mouse click
```csharp
app.InjectSequence(InputInjectionExtensions.LeftButtonClick(new Point(10, 5)));
```

**`RightButtonClick(Point p)`** - Single right mouse click
```csharp
app.InjectSequence(InputInjectionExtensions.RightButtonClick(new Point(10, 5)));
```

**`LeftButtonDoubleClick(Point p)`** - Double-click
```csharp
app.InjectSequence(InputInjectionExtensions.LeftButtonDoubleClick(new Point(10, 5)));
```

#### Benefits

- **Less code** - One line instead of multiple `InjectMouse` calls
- **Built-in timing** - Appropriate delays for reliable click detection
- **Fewer errors** - No need to manually match Press/Release pairs
- **Clearer intent** - Code explicitly shows what user action is being simulated

#### Before and After Comparison

**Before (manual):**
```csharp
// Double-click requires 4 separate events
app.InjectMouse(new() { ScreenPosition = new(10, 5), Flags = MouseFlags.LeftButtonPressed });
app.InjectMouse(new() { ScreenPosition = new(10, 5), Flags = MouseFlags.LeftButtonReleased });
app.InjectMouse(new() { ScreenPosition = new(10, 5), Flags = MouseFlags.LeftButtonPressed });
app.InjectMouse(new() { ScreenPosition = new(10, 5), Flags = MouseFlags.LeftButtonReleased });
```

**After (helper):**
```csharp
// Same behavior, one line
app.InjectSequence(InputInjectionExtensions.LeftButtonDoubleClick(new Point(10, 5)));
```

#### Example: Testing Button Click

```csharp
using IApplication app = Application.Create();
app.Init(DriverRegistry.Names.ANSI);

using Runnable runnable = new();
app.Begin(runnable);

Button button = new() { Text = "Click Me" };
runnable.Add(button);

var acceptingCalled = false;
button.Accepting += (s, e) => acceptingCalled = true;

// Use helper to inject click
app.InjectSequence(InputInjectionExtensions.LeftButtonClick(new Point(0, 0)));

Assert.True(acceptingCalled);
```

## When to Use Each Mode

### Direct Mode (Default)

**Use for:** 99% of tests - view behavior, command execution, event handling

```csharp
// Direct mode is default - just inject
app.InjectKey(Key.A);
app.InjectMouse(new() { ScreenPosition = new(10, 5), Flags = MouseFlags.LeftButtonClicked });
```

### Pipeline Mode (ANSI Testing)

**Use for:** Testing ANSI encoding/parsing, escape sequences, driver behavior

```csharp
// Explicit Pipeline mode
IInputInjector injector = app.GetInputInjector();
InputInjectionOptions options = new() { Mode = InputInjectionMode.Pipeline };

// This tests: Key.F1 → "\x1b[OP" → Parser → Key.F1
injector.InjectKey(Key.F1, options);
```

### Auto Mode

**Purpose:** Let the system choose the appropriate mode

**Behavior:** Currently defaults to Direct mode for performance

**When to Use:** When there are no specific requirements for mode selection

---

## Detailed Documentation

The sections below provide in-depth documentation for advanced scenarios.

- [Virtual Time Details](#virtual-time-details)
- [Injection Modes Deep Dive](#injection-modes-deep-dive)
- [Architecture Layers](#architecture-layers)
- [Testing Patterns](#testing-patterns)
- [Best Practices](#best-practices)
- [Advanced Topics](#advanced-topics)
- [Troubleshooting](#troubleshooting)

---

## Virtual Time Details

**Virtual time** is the foundation of deterministic testing. Instead of relying on `DateTime.Now` and real delays, tests explicitly control time advancement.

### How Virtual Time Works

```csharp
// Create virtual time provider
VirtualTimeProvider time = new();

// Set initial time
time.SetTime(new DateTime(2025, 1, 1, 12, 0, 0));

// Advance time by 100ms (instant - no real delay)
time.Advance(TimeSpan.FromMilliseconds(100));

// Current virtual time is now 12:00:00.100
DateTime now = time.Now;  // 2025-01-01 12:00:00.100
```

### Benefits of Virtual Time

- **Fast** - No real delays; `time.Advance(TimeSpan.FromSeconds(10))` is instant
- **Precise** - Control timing to the millisecond
- **Repeatable** - Same time sequence → same test results
- **Debuggable** - Pause time, inspect state, advance step-by-step

### Components Using Virtual Time

All timing-dependent components accept <xref:Terminal.Gui.Time.ITimeProvider>:

- **`MouseButtonClickTracker`** - Double/triple-click detection based on time thresholds
- **`MouseInterpreter`** - Click timing and multi-click synthesis
- **`AnsiResponseParser`** - Escape sequence timeout detection (50ms)
- **Application timers and delays** - All time-based operations

## Injection Modes Deep Dive

### Input Injection Modes

The system supports two injection modes to balance speed and coverage:

#### Direct Mode (Default)

**Purpose:** Fast, simple testing of view/application logic

**Flow:** 
```
InputInjector → InputProcessor → Events
```

**Characteristics:**
- Bypasses ANSI encoding/decoding
- Preserves all event properties (timestamps, flags, etc.)
- Fastest execution
- Default for most tests

**When to Use:**
- Testing view behavior (button clicks, text input)
- Testing mouse event handling
- Testing command execution
- Any test not specifically testing ANSI encoding

**Example:**
```csharp
// Direct mode (default)
VirtualTimeProvider time = new();
using IApplication app = Application.Create(time);
app.Init(DriverRegistry.Names.ANSI);

// Injection goes directly to events
app.InjectKey(Key.Enter);  // Fast, bypasses ANSI encoding
```

#### Pipeline Mode

**Purpose:** Full ANSI encoding/parsing pipeline testing

**Flow:**
```
InputInjector → ANSI Encoder → TestInputSource (chars) → AnsiResponseParser → InputProcessor → Events
```

**Characteristics:**
- Tests full ANSI escape sequence encoding
- Tests ANSI parser behavior
- Validates round-trip encoding/decoding
- Slightly slower (more processing steps)

**When to Use:**
- Testing ANSI keyboard encoding (e.g., `Key.F1` → `"\x1b[OP"`)
- Testing ANSI mouse encoding (e.g., SGR format `"\x1b[<0;10;5M"`)
- Testing escape sequence parsing
- Testing parser timeout behavior

**Example:**
```csharp
// Pipeline mode for ANSI testing
VirtualTimeProvider time = new();
using IApplication app = Application.Create(time);
app.Init(DriverRegistry.Names.ANSI);

InputInjectionOptions options = new() 
{ 
    Mode = InputInjectionMode.Pipeline 
};

// This encodes Key.F1 → "\x1b[OP", injects chars, parses back
app.GetInputInjector().InjectKey(Key.F1, options);

// Verify ANSI encoding worked correctly
// (parser should decode "\x1b[OP" back to Key.F1)
```

#### Auto Mode

**Purpose:** Let the system choose the appropriate mode

**Behavior:** Currently defaults to Direct mode for performance

**When to Use:** When there are no specific requirements for mode selection

## Architecture Layers

### Layer 1: Time Abstraction

**Location:** `Terminal.Gui/Time/ITimeProvider.cs`

**Purpose:** Provide pluggable time source for testing and production

#### ITimeProvider Interface

```csharp
/// <summary>
/// Abstraction for time-related operations, allowing virtual time in tests.
/// </summary>
public interface ITimeProvider
{
    /// <summary>
    /// Gets the current date/time. In tests, this can be controlled.
    /// </summary>
    DateTime Now { get; }
    
    /// <summary>
    /// Creates a delay. In tests, this can be instant or controlled.
    /// </summary>
    Task Delay(TimeSpan duration, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a timer. In tests, this can be controlled.
    /// </summary>
    ITimer CreateTimer(TimeSpan interval, Action callback);
}
```

#### SystemTimeProvider (Production)

```csharp
/// <summary>
/// Real time provider using DateTime.Now and Task.Delay.
/// </summary>
public class SystemTimeProvider : ITimeProvider
{
    public DateTime Now => DateTime.Now;
    
    public Task Delay(TimeSpan duration, CancellationToken ct) 
        => Task.Delay(duration, ct);
    
    public ITimer CreateTimer(TimeSpan interval, Action callback)
        => new SystemTimer(interval, callback);
}
```

**Usage:** Production applications use system time automatically

#### VirtualTimeProvider (Testing)

```csharp
/// <summary>
/// Virtual time provider for testing - all time is controlled.
/// </summary>
public class VirtualTimeProvider : ITimeProvider
{
    private DateTime _currentTime = new (2025, 1, 1, 0, 0, 0);
    
    public DateTime Now => _currentTime;
    
    /// <summary>
    /// Advance virtual time by the specified duration.
    /// </summary>
    public void Advance(TimeSpan duration)
    {
        _currentTime += duration;
        // Trigger timers and complete delays
    }
    
    /// <summary>
    /// Set virtual time to a specific value.
    /// </summary>
    public void SetTime(DateTime time)
    {
        _currentTime = time;
    }
}
```

**Usage:** Tests create and control virtual time explicitly

**Key Design Decision:** All timing-dependent code uses <xref:Terminal.Gui.Time.ITimeProvider> instead of `DateTime.Now` directly. This single change enables complete time control in tests.

### Layer 2: Input Source

**Location:** `Terminal.Gui/Drivers/Input/IInputSource.cs`

**Purpose:** Provide input records to the input processor

#### IInputSource Interface

```csharp
/// <summary>
/// Source of input events. Production implementations read from console,
/// test implementations provide pre-programmed input.
/// </summary>
public interface IInputSource
{
    /// <summary>
    /// Time provider for timestamps and timing.
    /// </summary>
    ITimeProvider TimeProvider { get; }
    
    /// <summary>
    /// Check if input is available without consuming it.
    /// </summary>
    bool IsAvailable { get; }
    
    /// <summary>
    /// Read all available input synchronously.
    /// </summary>
    IEnumerable<InputRecord> ReadAvailable();
    
    /// <summary>
    /// Start background input reading (for production).
    /// </summary>
    void Start(CancellationToken cancellationToken);
    
    /// <summary>
    /// Stop background input reading.
    /// </summary>
    void Stop();
}
```

#### Input Record Types

All input flows through platform-independent records:

```csharp
/// <summary>
/// Base class for input records.
/// </summary>
public abstract record InputRecord
{
    /// <summary>
    /// When this input occurred (set by ITimeProvider).
    /// </summary>
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Keyboard input record.
/// </summary>
public record KeyboardRecord(Key Key) : InputRecord;

/// <summary>
/// Mouse input record (raw, before click synthesis).
/// </summary>
public record MouseRecord(Mouse Mouse) : InputRecord;

/// <summary>
/// ANSI sequence record (for Pipeline mode testing).
/// </summary>
public record AnsiRecord(string Sequence) : InputRecord;
```

#### TestInputSource (Testing)

```csharp
/// <summary>
/// Test input source - provides pre-programmed input via queue.
/// </summary>
public class TestInputSource : IInputSource
{
    private readonly Queue<InputRecord> _inputQueue = new ();
    
    public ITimeProvider TimeProvider { get; }
    
    public bool IsAvailable => _inputQueue.Count > 0;
    
    public IEnumerable<InputRecord> ReadAvailable()
    {
        while (_inputQueue.Count > 0)
        {
            yield return _inputQueue.Dequeue();
        }
    }
    
    /// <summary>
    /// Add input to the queue (called by InputInjector).
    /// </summary>
    public void Enqueue(InputRecord record)
    {
        // Stamp with current virtual time if not set
        if (record.Timestamp == default)
        {
            record = record with { Timestamp = TimeProvider.Now };
        }
        
        _inputQueue.Enqueue(record);
    }
}
```

**Key Characteristics:**
- Thread-safe queue
- Automatic timestamping via <xref:Terminal.Gui.Time.ITimeProvider>
- No background thread (synchronous testing)
- Complete control over input sequence

#### ConsoleInputSource (Production)

```csharp
/// <summary>
/// Console input source - reads from actual console.
/// </summary>
public abstract class ConsoleInputSource : IInputSource
{
    protected readonly ConcurrentQueue<InputRecord> InputBuffer = new ();
    
    public void Start(CancellationToken cancellationToken)
    {
        // Start background thread that reads from console
        Task.Run(() => ReadLoop(cancellationToken), cancellationToken);
    }
    
    /// <summary>
    /// Platform-specific console reading (overridden by drivers).
    /// </summary>
    protected abstract Task ReadLoop(CancellationToken cancellationToken);
}
```

**Platform Implementations:**
- `WindowsInputSource` - Reads `InputRecord` from Windows Console API
- `NetInputSource` - Reads `ConsoleKeyInfo` from .NET Console
- `UnixInputSource` - Reads `char` from Unix terminal
- `AnsiInputSource` - Reads `char` and parses ANSI sequences

### Layer 3: Input Processor

**Location:** `Terminal.Gui/Drivers/InputProcessor/InputProcessor.cs`

**Purpose:** Convert input records to Terminal.Gui events

#### IInputProcessor Interface

```csharp
/// <summary>
/// Input processor - consumes input from IInputSource and raises events.
/// </summary>
public interface IInputProcessor
{
    /// <summary>
    /// Input source providing input records.
    /// </summary>
    IInputSource InputSource { get; }
    
    /// <summary>
    /// Time provider for timing-dependent operations.
    /// </summary>
    ITimeProvider TimeProvider { get; }
    
    /// <summary>
    /// Process all available input from the source.
    /// </summary>
    void ProcessInput();
    
    /// <summary>
    /// Keyboard events.
    /// </summary>
    event EventHandler<Key>? KeyDown;
    event EventHandler<Key>? KeyUp;
    
    /// <summary>
    /// Mouse event (includes synthesized clicks).
    /// </summary>
    event EventHandler<Mouse>? MouseEvent;
    
    /// <summary>
    /// ANSI sequence parser (for ANSI drivers only).
    /// </summary>
    IAnsiResponseParser? AnsiParser { get; }
}
```

#### InputProcessor Implementation

```csharp
/// <summary>
/// Implementation of IInputProcessor.
/// </summary>
public class InputProcessor : IInputProcessor
{
    private readonly MouseInterpreter _mouseInterpreter;
    private readonly IAnsiResponseParser? _ansiParser;
    
    public IInputSource InputSource { get; }
    public ITimeProvider TimeProvider { get; }
    
    public void ProcessInput()
    {
        // 1. Read all available input
        foreach (InputRecord record in InputSource.ReadAvailable())
        {
            ProcessRecord(record);
        }
        
        // 2. Check for stale escape sequences
        if (_ansiParser?.IsEscapeSequenceStale() == true)
        {
            // Release held Esc key via virtual time
            foreach (char released in _ansiParser.Release())
            {
                ProcessRecord(new AnsiRecord(released.ToString()) 
                { 
                    Timestamp = TimeProvider.Now 
                });
            }
        }
    }
    
    private void ProcessRecord(InputRecord record)
    {
        switch (record)
        {
            case KeyboardRecord kr:
                RaiseKeyboard(kr.Key);
                break;
                
            case MouseRecord mr:
                RaiseMouse(mr.Mouse);
                break;
                
            case AnsiRecord ar:
                // Feed to ANSI parser (Pipeline mode)
                _ansiParser?.ProcessInput(ar.Sequence);
                break;
        }
    }
    
    private void RaiseMouse(Mouse mouse)
    {
        // Process through MouseInterpreter for click synthesis
        foreach (Mouse processedMouse in _mouseInterpreter.Process(mouse))
        {
            MouseEvent?.Invoke(this, processedMouse);
        }
    }
}
```

**Key Features:**
- Single `ProcessInput()` call processes all queued input
- Automatic escape sequence timeout via <xref:Terminal.Gui.Time.ITimeProvider>
- Optional ANSI parsing for Pipeline mode
- Click synthesis via `MouseInterpreter`

### Layer 4: Input Injector

**Location:** `Terminal.Gui/Testing/InputInjector.cs`

**Purpose:** High-level API for test input injection

#### IInputInjector Interface

```csharp
/// <summary>
/// High-level input injection API - single entry point for all injection.
/// </summary>
public interface IInputInjector
{
    /// <summary>
    /// Inject a keyboard event.
    /// </summary>
    void InjectKey(Key key, InputInjectionOptions? options = null);
    
    /// <summary>
    /// Inject a mouse event.
    /// </summary>
    void InjectMouse(Mouse mouse, InputInjectionOptions? options = null);
    
    /// <summary>
    /// Inject a sequence of input events with delays.
    /// </summary>
    void InjectSequence(IEnumerable<InputEvent> events, InputInjectionOptions? options = null);
    
    /// <summary>
    /// Force processing of the input queue (usually automatic).
    /// </summary>
    void ProcessQueue();
}
```

#### Input Injection Options

```csharp
/// <summary>
/// Configuration for input injection behavior.
/// </summary>
public class InputInjectionOptions
{
    /// <summary>
    /// Injection mode (Direct, Pipeline, or Auto).
    /// </summary>
    public InputInjectionMode Mode { get; set; } = InputInjectionMode.Auto;
    
    /// <summary>
    /// Whether to automatically process the input queue after injection.
    /// </summary>
    public bool AutoProcess { get; set; } = true;
    
    /// <summary>
    /// Time provider to use for timestamps and timing.
    /// </summary>
    public ITimeProvider? TimeProvider { get; set; }
}
```

#### InputInjector Implementation

```csharp
/// <summary>
/// Implementation of IInputInjector for testing.
/// </summary>
public class InputInjector : IInputInjector
{
    private readonly IInputProcessor _processor;
    private readonly TestInputSource? _testSource;
    private readonly ITimeProvider _timeProvider;
    
    public void InjectKey(Key key, InputInjectionOptions? options = null)
    {
        options ??= new InputInjectionOptions();
        InputInjectionMode mode = ResolveMode(options.Mode);
        
        InputRecord record;
        if (mode == InputInjectionMode.Direct)
        {
            // Direct: Bypass encoding
            record = new KeyboardRecord(key) 
            { 
                Timestamp = _timeProvider.Now 
            };
        }
        else // Pipeline
        {
            // Encode to ANSI sequence
            string ansiSequence = AnsiKeyboardEncoder.Encode(key);
            record = new AnsiRecord(ansiSequence) 
            { 
                Timestamp = _timeProvider.Now 
            };
        }
        
        _testSource.Enqueue(record);
        
        if (options.AutoProcess)
        {
            ProcessQueue();
        }
    }
    
    public void ProcessQueue()
    {
        _processor.ProcessInput();
        
        // If escape sequences are stale, advance time and process again
        if (_timeProvider is VirtualTimeProvider vtp)
        {
            if (_processor.AnsiParser?.State == AnsiResponseParserState.ExpectingEscapeSequence)
            {
                vtp.Advance(TimeSpan.FromMilliseconds(60)); // Past 50ms timeout
                _processor.ProcessInput();
            }
        }
    }
}
```

**Key Features:**
- Mode selection (Direct vs Pipeline)
- Automatic processing by default
- Automatic escape sequence handling via virtual time
- Timestamp management

### Layer 5: Application Integration

**Location:** `Terminal.Gui/Testing/InputInjectionExtensions.cs`

**Purpose:** Convenient extension methods on <xref:Terminal.Gui.App.IApplication>

#### Extension Methods

```csharp
/// <summary>
/// Extension methods for input injection.
/// </summary>
public static class InputInjectionExtensions
{
    private static readonly ConditionalWeakTable<IApplication, IInputInjector> _injectorCache = new ();
    
    /// <summary>
    /// Get or create the input injector for this application.
    /// </summary>
    public static IInputInjector GetInputInjector(this IApplication app)
    {
        return _injectorCache.GetValue(app, _ => 
        {
            ITimeProvider timeProvider = app.GetTimeProvider();
            IInputProcessor processor = app.Driver.GetInputProcessor();
            return new InputInjector(processor, timeProvider);
        });
    }
    
    /// <summary>
    /// Inject a key event (convenience method).
    /// </summary>
    public static void InjectKey(this IApplication app, Key key)
    {
        app.GetInputInjector().InjectKey(key);
    }
    
    /// <summary>
    /// Inject a mouse event (convenience method).
    /// </summary>
    public static void InjectMouse(this IApplication app, Mouse mouse)
    {
        app.GetInputInjector().InjectMouse(mouse);
    }
    
    /// <summary>
    /// Inject a sequence of events (convenience method).
    /// </summary>
    public static void InjectSequence(this IApplication app, params InputEvent[] events)
    {
        app.GetInputInjector().InjectSequence(events);
    }
}
```

**Key Features:**
- Cached injector per application instance
- Clean API via extension methods
- Automatic setup and teardown

## Testing Patterns

### Simple Unit Tests

**Goal:** Test view behavior without timing concerns

```csharp
// Simple button click test
[Fact]
public void Button_ClickWithMouse_RaisesAccepting()
{
    VirtualTimeProvider time = new ();
    using IApplication app = Application.Create(time);
    app.Init(DriverRegistry.Names.ANSI);
    
    Button button = new () { Text = "Click Me" };
    bool acceptingCalled = false;
    button.Accepting += (s, e) => acceptingCalled = true;
    
    // Single-call injection
    app.InjectMouse(new () 
    {
        Flags = MouseFlags.LeftButtonPressed,
        Position = new (5, 5)
    });
    
    app.InjectMouse(new () 
    {
        Flags = MouseFlags.LeftButtonReleased,
        Position = new (5, 5)
    });
    
    Assert.True(acceptingCalled);
}
```

### Timing-Dependent Tests

**Goal:** Test double-click, timing thresholds, etc.

```csharp
// Double-click detection test
[Fact]
public void DoubleClick_WithinThreshold_DetectsDoubleClick()
{
    VirtualTimeProvider time = new ();
    time.SetTime(new DateTime(2025, 1, 1, 12, 0, 0));
    
    using IApplication app = Application.Create(time);
    app.Init(DriverRegistry.Names.ANSI);
    
    List<MouseFlags> receivedFlags = [];
    app.Mouse.MouseEvent += (s, e) => receivedFlags.Add(e.Flags);
    
    // First click at T+0
    app.InjectMouse(new () 
    { 
        Flags = MouseFlags.LeftButtonPressed,
        Position = new (5, 5)
    });
    
    app.InjectMouse(new () 
    { 
        Flags = MouseFlags.LeftButtonReleased,
        Position = new (5, 5)
    });
    
    // Advance virtual time by 300ms
    time.Advance(TimeSpan.FromMilliseconds(300));
    
    // Second click at T+300 (within 500ms threshold = double-click)
    app.InjectMouse(new () 
    { 
        Flags = MouseFlags.LeftButtonPressed,
        Position = new (5, 5)
    });
    
    app.InjectMouse(new () 
    { 
        Flags = MouseFlags.LeftButtonReleased,
        Position = new (5, 5)
    });
    
    // Verify double-click detected
    Assert.Contains(receivedFlags, f => f.HasFlag(MouseFlags.LeftButtonDoubleClicked));
}
```

**Key Points:**
- Explicit time control via `time.Advance()`
- Precise timing to millisecond
- No real delays - tests run instantly
- Repeatable results every time

### ANSI Pipeline Tests

**Goal:** Test ANSI encoding/parsing pipeline

```csharp
// Test ANSI keyboard encoding
[Fact]
public void AnsiEncoding_F1Key_EncodesAndDecodesCorrectly()
{
    VirtualTimeProvider time = new ();
    using IApplication app = Application.Create(time);
    app.Init(DriverRegistry.Names.ANSI);
    
    Key? receivedKey = null;
    app.Driver.KeyDown += (s, e) => receivedKey = e;
    
    // Force Pipeline mode to test ANSI encoding
    InputInjectionOptions options = new () 
    { 
        Mode = InputInjectionMode.Pipeline 
    };
    
    // This will:
    // 1. Encode Key.F1 → "\x1b[OP"
    // 2. Inject chars individually
    // 3. Parser detects escape sequence
    // 4. Decodes back to Key.F1
    app.InjectKey(Key.F1, options);
    
    Assert.Equal(Key.F1, receivedKey);
}
```

**Key Points:**
- Explicit Pipeline mode selection
- Tests full ANSI round-trip
- Validates encoding/decoding correctness

### Integration Tests

**Goal:** Test complete application lifecycle with fluent API

```csharp
// Integration test using fluent API
[Fact]
public void Application_FullLifecycle_WorksCorrectly()
{
    using GuiTestContext context = With.A<Window>(40, 10, TestDriver.ANSI)
        .Add(new Button { Text = "Click Me", X = 5, Y = 5 })
        .LeftClick(6, 6)  // Click button
        .InjectKey(Key.Tab)  // Navigate
        .InjectKey(Key.Enter)  // Activate
        .AssertTrue(app => app.IsShuttingDown == false);
    
    // Context automatically manages application lifecycle
}
```

## Best Practices

### 1. Always Use Virtual Time for Tests

```csharp
// ✅ CORRECT - Virtual time for determinism
VirtualTimeProvider time = new ();
using IApplication app = Application.Create(time);

// ❌ WRONG - System time causes flaky tests
using IApplication app = Application.Create();  // Uses SystemTimeProvider
```

### 2. Use ANSI Driver for Tests

```csharp
// ✅ CORRECT - ANSI driver is cross-platform
app.Init(DriverRegistry.Names.ANSI);

// ⚠️ AVOID - Platform-specific drivers in tests
app.Init(DriverRegistry.Names.WINDOWS);  // Won't run on Unix
```

### 3. Use Direct Mode by Default

```csharp
// ✅ CORRECT - Default Direct mode is fast
app.InjectKey(Key.Enter);

// ⚠️ ONLY when testing ANSI encoding
app.InjectKey(Key.F1, new () { Mode = InputInjectionMode.Pipeline });
```

### 4. Advance Time Explicitly

```csharp
// ✅ CORRECT - Explicit time advancement
app.InjectMouse(firstClick);
time.Advance(TimeSpan.FromMilliseconds(50));
app.InjectMouse(secondClick);

// ❌ WRONG - Real delays defeat virtual time
app.InjectMouse(firstClick);
Thread.Sleep(300);  // Uses real time!
app.InjectMouse(secondClick);
```

### 5. Use Input Sequences for Complex Scenarios

```csharp
// ✅ CORRECT - Declarative sequence with delays
app.InjectSequence(
    new KeyEvent(Key.H),
    new KeyEvent(Key.E) { Delay = TimeSpan.FromMilliseconds(100) },
    new KeyEvent(Key.L),
    new KeyEvent(Key.L),
    new KeyEvent(Key.O)
);

// ⚠️ VERBOSE - Manual injection
app.InjectKey(Key.H);
time.Advance(TimeSpan.FromMilliseconds(100));
app.InjectKey(Key.E);
// ... etc
```

## Advanced Topics

### Custom Input Sources

You can create custom input sources for specialized testing:

```csharp
// Custom input source for file replay
public class FileInputSource : IInputSource
{
    private readonly Queue<InputRecord> _recordedInput;
    
    public FileInputSource(string recordingFile, ITimeProvider timeProvider)
    {
        TimeProvider = timeProvider;
        _recordedInput = LoadRecording(recordingFile);
    }
    
    public IEnumerable<InputRecord> ReadAvailable()
    {
        while (_recordedInput.Count > 0)
        {
            yield return _recordedInput.Dequeue();
        }
    }
}
```

### Custom Time Providers

Advanced scenarios may need custom time behavior:

```csharp
// Time provider with variable speed
public class ScaledTimeProvider : ITimeProvider
{
    private readonly double _timeScale;  // 2.0 = 2x speed
    private DateTime _baseTime = DateTime.Now;
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    
    public DateTime Now => _baseTime + TimeSpan.FromTicks(
        (long)(_stopwatch.Elapsed.Ticks * _timeScale)
    );
}
```

### Input Sequences

Complex input patterns can be defined declaratively:

```csharp
// Define reusable input sequences
public static class InputSequences
{
    public static InputEvent[] TypeText(string text)
    {
        return text.Select(c => new KeyEvent(Key.Create(c))).ToArray();
    }
    
    public static InputEvent[] DoubleClick(Point position)
    {
        return 
        [
            new MouseEvent(new () { Flags = MouseFlags.LeftButtonPressed, Position = position }),
            new MouseEvent(new () { Flags = MouseFlags.LeftButtonReleased, Position = position }) 
                { Delay = TimeSpan.FromMilliseconds(50) },
            new MouseEvent(new () { Flags = MouseFlags.LeftButtonPressed, Position = position }) 
                { Delay = TimeSpan.FromMilliseconds(300) },
            new MouseEvent(new () { Flags = MouseFlags.LeftButtonReleased, Position = position }) 
                { Delay = TimeSpan.FromMilliseconds(50) }
        ];
    }
}

// Use in tests
app.InjectSequence(InputSequences.TypeText("Hello"));
app.InjectSequence(InputSequences.DoubleClick(new (5, 5)));
```

## Troubleshooting

### Events Not Firing

**Symptom:** Injected input doesn't trigger expected events

**Checklist:**
1. Did you call `app.Init()` before injection?
2. Is the view enabled and visible?
3. Is `AutoProcess` enabled (default: true)?
4. Are you using the correct coordinate system (viewport-relative)?

**Debug:**
```csharp
// Enable trace logging to see pipeline flow
Logging.Trace($"Injecting: {key}");
app.Driver.KeyDown += (s, e) => Logging.Trace($"KeyDown: {e}");
```

### Double-Clicks Not Detected

**Symptom:** Multi-click detection fails

**Causes:**
1. Time not advanced between clicks
2. Position changed between clicks
3. Timing outside threshold (default 500ms)

**Fix:**
```csharp
// Ensure timing is within threshold
app.InjectMouse(firstPress);
time.Advance(TimeSpan.FromMilliseconds(50));
app.InjectMouse(firstRelease);

time.Advance(TimeSpan.FromMilliseconds(300));  // < 500ms total

app.InjectMouse(secondPress);  // SAME position!
time.Advance(TimeSpan.FromMilliseconds(50));
app.InjectMouse(secondRelease);
```

### ANSI Encoding Fails

**Symptom:** Pipeline mode doesn't work correctly

**Causes:**
1. Not using ANSI driver
2. Parser not enabled in processor
3. Invalid ANSI sequence encoding

**Fix:**
```csharp
// Ensure ANSI driver and Pipeline mode
app.Init(DriverRegistry.Names.ANSI);

InputInjectionOptions options = new () 
{ 
    Mode = InputInjectionMode.Pipeline 
};

app.InjectKey(key, options);
```

### Escape Key Behavior

**Symptom:** Escape key not processed correctly

**Cause:** ANSI parser holds Esc for 50ms to detect escape sequences

**Solution:** Virtual time automatically handles this via `ProcessQueue()`:
```csharp
// This automatically advances time past escape timeout
app.InjectKey(Key.Esc);  // ProcessQueue handles timing internally
```

### Performance Issues

**Symptom:** Tests run slowly

**Causes:**
1. Using real time instead of virtual time
2. Using Pipeline mode when Direct mode sufficient
3. Too many small injections vs sequences

**Fix:**
```csharp
// Use virtual time and Direct mode
VirtualTimeProvider time = new ();
app.InjectKey(key);  // Fast Direct mode

// Use sequences for multiple inputs
app.InjectSequence(events);  // Better than loop of InjectKey()
