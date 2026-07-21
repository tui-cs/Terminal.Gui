# Terminal.Gui.KeySequences Specification

## Problem Statement

Terminal.Gui has a command and key binding model for single-key gestures: a `Key` maps to one or more `Command` values, and a focused `View` invokes supported commands through the existing routing and Cancellable Work Pattern.

Some applications need modal, Vim-style key sequences instead of single-key gestures. These sequences commonly start with a leader key, continue through one or more symbolic keys, and may include numeric counts before an operator or motion.

Examples:

```text
; m 4 k  => move cursor 4 lines up
; d 2 d  => delete current line and 1 line below
```

`Terminal.Gui.KeySequences` provides this capability as a separate add-on project. It references Terminal.Gui, uses public keyboard events, owns its own tests and docs, and does not add public APIs to the core `Terminal.Gui` assembly.

## Goals

- Let developers define leader keys, such as `;`, `<Space>`, or `<Ctrl+X>`.
- Let developers bind chains of keys to handlers.
- Let chains accept printable letters, punctuation keys, named keys, wildcard keys, printable characters, and numeric counts.
- Let handlers receive parsed sequence context, including leader key, matched keys, numeric count, operator key, motion key, target view, command context, and named token values.
- Support both transient leader sequences and persistent command mode.
- Integrate with existing `View.KeyDown`, `View.KeyDownNotHandled`, `IApplication.Keyboard.KeyDown`, `Command`, and `CommandContext`.
- Preserve existing single-key behavior unless a sequence is actively captured or an opt-in preemptive registration is used.
- Support application-scoped sequences and focused-view sequences.
- Make partial input, cancellation, timeouts, and invalid sequences predictable.
- Provide testable APIs that do not depend on static application state.

## Non-Goals

- Do not implement a full Vim editor.
- Do not add Vim-specific commands to the core `Command` enum.
- Do not require existing views to understand Vim semantics.
- Do not replace `KeyBindings`; compose with the current model.
- Do not require key-up events or kitty-only keyboard metadata.
- Do not add `View.KeySequences`, `IKeyboard.KeySequences`, or any other public API to the Terminal.Gui core assembly.

## Package Model

The add-on project is:

```text
Terminal.Gui.KeySequences
```

The public namespace is:

```csharp
namespace Terminal.Gui.KeySequences;
```

The package name is intentionally generic. Vim-style motions are the primary motivating scenario, but the same API supports leader shortcuts, command palettes, and application-specific multi-key chords.

## Terminology

- **Leader key**: The first key that enters leader sequence capture, such as `;`.
- **Command mode**: A persistent mode entered by `EnterModeKey`; sequence patterns match without a leader until `ExitModeKey` is pressed.
- **Sequence**: The ordered keys entered after the leader key, or after command mode is active.
- **Token**: A matched element in a sequence pattern. Tokens can be literal keys, a count, a printable character, or a wildcard key.
- **Count**: A decimal number parsed from one or more digit keys. Counts default to `1`.
- **Operator**: The first literal key in the matched pattern, such as `d` for delete.
- **Motion**: The last literal key in the matched pattern, such as `k` for up.
- **Binding**: A declarative mapping from a sequence pattern to a handler.
- **Sequence capture**: The transient state after a leader key, or the current partially entered command-mode sequence.

## Public API

### KeySequenceBindings

`KeySequenceBindings` owns sequence patterns, capture state, timeout behavior, and handler invocation.

```csharp
public sealed class KeySequenceBindings
{
    public event EventHandler<KeySequenceStateChangedEventArgs>? StateChanged;

    public TimeSpan Timeout { get; set; }
    public Key CancelKey { get; set; }
    public KeySequenceMode Mode { get; set; }
    public Key EnterModeKey { get; set; }
    public Key ExitModeKey { get; set; }
    public TimeProvider TimeProvider { get; set; }

    public bool IsCapturing { get; }
    public bool IsCommandMode { get; }

    public void AddLeader (Key leaderKey);
    public void RemoveLeader (Key leaderKey);
    public bool IsLeader (Key key);

    public void Add (KeySequencePattern pattern, KeySequenceHandler handler);
    public void Add (string pattern, KeySequenceHandler handler);
    public void AddMode (string pattern, KeySequenceHandler handler);
    public bool Remove (KeySequencePattern pattern);
    public void Clear ();

    public KeySequenceResult ProcessKey (View target, Key key, CommandContext? commandContext = null);
    public void Reset ();
    public void EnterCommandMode ();
    public void ExitCommandMode ();
}
```

Defaults:

- `Timeout = TimeSpan.FromSeconds (1)`
- `CancelKey = Key.Esc`
- `Mode = KeySequenceMode.Leader`
- `EnterModeKey = Key.Esc`
- `ExitModeKey = 'i'`
- `TimeProvider = TimeProvider.System`

### Handlers And Context

Handlers return `true` when they consumed the sequence.

```csharp
public delegate bool KeySequenceHandler (KeySequenceContext context);
```

`KeySequenceContext` describes the completed match.

```csharp
public sealed record KeySequenceContext
{
    public required View Target { get; init; }
    public Key? LeaderKey { get; init; }
    public bool IsCommandMode { get; init; }
    public required IReadOnlyList<Key> Keys { get; init; }
    public required KeySequencePattern Pattern { get; init; }
    public int Count { get; init; } = 1;
    public Key? OperatorKey { get; init; }
    public Key? MotionKey { get; init; }
    public IReadOnlyDictionary<string, object?> Values { get; init; }
    public CommandContext? CommandContext { get; init; }
}
```

In leader mode, `LeaderKey` is the key that started capture. In persistent command mode, `LeaderKey` is `null` and `IsCommandMode` is `true`.

### Results

```csharp
public enum KeySequenceResult
{
    NotLeader,
    Started,
    Pending,
    Matched,
    Canceled,
    Rejected,
    TimedOut,
    ModeEntered,
    ModeExited
}
```

`Matched` means a pattern completed and its handler returned `true`. If a handler returns `false`, processing returns `Rejected`. The add-on does not replay original keys after a rejected handler.

### Modes

```csharp
public enum KeySequenceMode
{
    Leader,
    Persistent
}
```

Leader mode starts capture with any registered leader key. Persistent mode starts command mode with `EnterModeKey`, exits with `ExitModeKey`, and matches only patterns added through `AddMode`.

### Patterns

`KeySequencePattern` supports both leader patterns and command-mode patterns.

```csharp
KeySequencePattern leaderPattern = KeySequencePattern
                                   .Leader (';')
                                   .Then ('m')
                                   .Count ()
                                   .Then ('k');

KeySequencePattern modePattern = KeySequencePattern
                                 .CommandMode ()
                                 .Then ('d')
                                 .Count ()
                                 .Then ('d');
```

`KeySequencePattern.MatchMode` controls ambiguous matches:

```csharp
public enum KeySequenceMatchMode
{
    Longest,
    Immediate
}
```

`Longest` waits when a complete pattern is also a prefix of a longer pattern. `Immediate` executes as soon as the pattern is complete.

`KeySequencePattern.AllowZeroCount` allows a parsed count of `0`. Without it, a completed match with count `0` is rejected.

## Extension Methods

View extension methods attach sequence handling without changing Terminal.Gui core types.

```csharp
public static class KeySequenceViewExtensions
{
    public static KeySequenceBindings GetKeySequences (this View view);

    public static IDisposable UseKeySequences (
        this View view,
        KeySequenceBindings bindings,
        KeySequenceInterceptionMode mode = KeySequenceInterceptionMode.AfterUnhandled);

    public static IDisposable UseKeySequences (
        this View view,
        Action<KeySequenceBindings> configure,
        KeySequenceInterceptionMode mode = KeySequenceInterceptionMode.AfterUnhandled);
}
```

Application extension methods attach sequence handling to `IApplication.Keyboard.KeyDown`.

```csharp
public static class KeySequenceApplicationExtensions
{
    public static IDisposable UseKeySequences (this IApplication application, KeySequenceBindings bindings);
    public static IDisposable UseKeySequences (this IApplication application, Action<KeySequenceBindings> configure);
}
```

Both registration APIs return `IDisposable`. Disposing removes event handlers.

## Pattern Syntax

Compact string patterns are whitespace-separated.

```csharp
bindings.Add ("; m <count> k", MoveUp);
bindings.Add ("; d <count> d", DeleteLines);
bindings.AddMode ("d <count> d", DeleteLines);
```

String pattern rules:

- The first token in `Add` is the leader key.
- `AddMode` parses command-mode patterns without a leader key.
- A literal printable key is written as the character itself: `m`, `k`, `;`, `.`, `:`.
- Named keys use angle brackets and are parsed by `Key.TryParse`: `<Esc>`, `<Enter>`, `<Space>`, `<Backspace>`.
- `<count>` matches zero or more digits at that position and stores the parsed count. Omitted count means `1`.
- `<char>` matches any printable non-control character.
- `<key>` matches any valid key.
- A pattern may include at most one `<count>` token.

## Numeric Counts

Counts are parsed as base-10 integers.

Rules:

- Missing count means `1`.
- Leading zeroes are accepted by integer parsing, so `04` means `4`.
- A parsed count of `0` is rejected unless `AllowZeroCount` is `true`.
- Count parsing stops when the next non-digit token is entered.
- A pattern may include at most one `<count>` token.
- Overflow resolves to `int.MaxValue`.

For `; d 2 d`, the handler receives `Count = 2`. The view-specific handler decides whether that means "delete two lines" or "delete current line and one line below." The add-on does not encode editor-specific range semantics.

## Matching Model

`KeySequenceBindings` keeps a small state machine.

```text
Idle
  leader key -> Capturing
  non-leader key -> NotLeader

Capturing
  cancel key -> Canceled -> Idle
  timeout -> TimedOut -> Idle
  key extends at least one possible pattern -> Pending
  key completes a pattern -> Matched or Rejected -> Idle
  key completes one pattern and is prefix of a longer pattern -> Pending when MatchMode is Longest
  key matches no pattern -> Rejected -> Idle

Persistent idle
  enter mode key -> ModeEntered -> CommandMode
  other key -> NotLeader

CommandMode
  exit mode key -> ModeExited -> Persistent idle
  cancel key -> Canceled, stay in CommandMode
  timeout -> TimedOut, stay in CommandMode
  key extends at least one possible pattern -> Pending
  key completes a pattern -> Matched or Rejected, stay in CommandMode
  key matches no pattern -> Rejected, stay in CommandMode
```

The current implementation evaluates candidate bindings directly. It does not require a trie and can evolve to one later if profiling shows a need.

## Integration With Key Routing

The add-on integrates through public Terminal.Gui APIs. It does not edit `View.NewKeyDownEvent`, `IKeyboard`, or core routing internals.

Focused-view routing:

1. `UseKeySequences (this View view, ...)` subscribes to `view.KeyDown` and `view.KeyDownNotHandled`.
2. In `AfterUnhandled` mode, the add-on starts capture from `KeyDownNotHandled`.
3. During active capture, the add-on continues processing from `KeyDown` so subsequent keys can be consumed before normal bindings use them.
4. In `Preemptive` mode, the add-on starts capture from `KeyDown`.
5. When processing returns anything other than `NotLeader` or `TimedOut`, the key is marked handled.

Application-scoped routing:

1. `UseKeySequences (this IApplication application, ...)` subscribes to `application.Keyboard.KeyDown`.
2. The target view is `application.TopRunnableView`.
3. When processing returns anything other than `NotLeader` or `TimedOut`, the key is marked handled.

Some views consume character keys before public `KeyDown` subscribers can handle them. To use a sequence key in that situation, attach sequence handling at a level that receives the key first, use `Preemptive` where appropriate, or remove the conflicting view or application binding.

## Persistent Command Mode

Persistent command mode supports Vim-style mode switching.

```csharp
IDisposable registration = editor.UseKeySequences (
    bindings =>
    {
        bindings.Mode = KeySequenceMode.Persistent;
        bindings.EnterModeKey = Key.Esc;
        bindings.ExitModeKey = 'i';

        bindings.AddMode ("<count> k", context =>
        {
            MoveUp (context.Count);
            return true;
        });

        bindings.AddMode (": q", _ =>
        {
            app.RequestStop ();
            return true;
        });
    },
    KeySequenceInterceptionMode.Preemptive);
```

To use `Esc` as the command-mode enter key in an app that also binds `Esc` to quit, remove or replace the `Esc` quit binding.

```csharp
app.Keyboard.KeyBindings.Remove (Key.Esc);
```

## User Feedback

`StateChanged` exposes capture state without prescribing UI.

```csharp
bindings.StateChanged += (_, args) =>
{
    modeLabel.Text = args.IsCommandMode ? "COMMAND" : string.Empty;
    countLabel.Text = args.CountText;
};
```

`KeySequenceStateChangedEventArgs` includes:

- `State`
- `LeaderKey`
- `Keys`
- `CountText`
- `CandidateCount`
- `Result`
- `IsCommandMode`

Applications can use this to show command palettes, status bar hints, command-mode indicators, or transient prompts.

## Error Handling

- Adding a pattern with an invalid leader throws `ArgumentException`.
- Adding a pattern with no tokens after the leader throws `ArgumentException`.
- Adding a duplicate pattern throws `InvalidOperationException`.
- Adding a pattern with multiple `<count>` tokens throws `ArgumentException`.
- Parsing an invalid key token throws `FormatException`.
- Processing an invalid key or modifier-only key returns `Rejected` during capture.
- Processing a key while idle returns `NotLeader` unless it starts leader capture or enters persistent command mode.

## Add-On File Layout

Implementation files live in `Terminal.Gui.KeySequences/`.

Key files:

- `Terminal.Gui.KeySequences.csproj`
- `KeySequenceBindings.cs`
- `KeySequenceBinding.cs`
- `KeySequencePattern.cs`
- `KeySequenceContext.cs`
- `KeySequenceParser.cs`
- `KeySequenceMatcher.cs`
- `KeySequenceResult.cs`
- `KeySequenceStateChangedEventArgs.cs`
- `KeySequenceViewExtensions.cs`
- `KeySequenceApplicationExtensions.cs`
- `KeySequenceInterceptionMode.cs`
- `KeySequenceMatchMode.cs`
- `KeySequenceMode.cs`
- `specification.md`

Tests live in `Terminal.Gui.KeySequences/Tests/`.

Current test files:

- `KeySequenceBindingsTests.cs`
- `KeySequenceParserTests.cs`
- `ViewKeySequenceExtensionTests.cs`
- `Terminal.Gui.KeySequences.Tests.csproj`

Documentation lives in `docfx/docs/keysequences.md`.

## Verification

Run focused tests with:

```bash
dotnet test --project Terminal.Gui.KeySequences/Tests/Terminal.Gui.KeySequences.Tests.csproj --filter-class "*KeySequence*"
```

Build the add-on with:

```bash
dotnet build Terminal.Gui.KeySequences/Terminal.Gui.KeySequences.csproj --no-restore
```
