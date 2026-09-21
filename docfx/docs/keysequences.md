# Key Sequences Deep Dive

## See Also

* [Keyboard Deep Dive](keyboard.md)
* [Command Deep Dive](command.md)
* [Events Deep Dive](events.md)
* [Input Injection](input-injection.md)

`Terminal.Gui.KeySequences` is an add-on package for Vim-style keyboard sequences in Terminal.Gui apps. It is separate from the core `Terminal.Gui` assembly and uses public keyboard events instead of adding sequence APIs to core views.

Use key sequences to build command grammars that need more than a single shortcut, such as leader-key commands, operator-plus-motion commands, repeat counts, and persistent command modes.

## Concepts

A key sequence has three parts:

* **Capture trigger** - a leader key such as `;`, or persistent command mode entered by a key such as `Esc`.
* **Pattern** - a compact sequence string such as `"; m <count> k"` or `"d <count> d"`.
* **Handler** - a `KeySequenceHandler` that receives a `KeySequenceContext` after the pattern matches.

Leader mode captures a sequence after a configured leader key. Persistent mode captures sequence keys while command mode is active.

## Leader Mode

To define leader-key commands, attach `KeySequenceBindings` to the view that should receive the commands.

```csharp
using Terminal.Gui.Input;
using Terminal.Gui.KeySequences;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

TextView editor = new ();

IDisposable registration = editor.UseKeySequences (bindings =>
{
    bindings.Add ("; m <count> k", context =>
    {
        for (int i = 0; i < context.Count; i++)
        {
            context.Target.InvokeCommand (Command.Up);
        }

        return true;
    });
});
```

The pattern `"; m <count> k"` reads as:

* `;` starts sequence capture.
* `m` is a literal command key.
* `<count>` accepts one or more digits and defaults to `1` when omitted.
* `k` is a literal motion key.

The sequence `; m 4 k` moves up four times. The sequence `; m k` moves up once.

## Operator And Motion Commands

To model Vim-style operator commands, use literal keys for the operator and motion and a count token for the repeat count.

```csharp
editor.UseKeySequences (bindings =>
{
    bindings.Add ("; d <count> d", context =>
    {
        DeleteLines (context.Count);
        return true;
    });
});
```

The sequence `; d 2 d` deletes two lines. Handlers can inspect `context.OperatorKey`, `context.MotionKey`, `context.Count`, `context.Keys`, and `context.Values` to share implementation across related commands.

## Persistent Command Mode

Persistent command mode works like a small Vim command mode. The user presses one key to enter command mode, and subsequent keys are interpreted as sequence commands until the exit key is pressed.

```csharp
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.KeySequences;
using Terminal.Gui.Views;

IApplication app = Application.Create ().Init ();
TextView editor = new ();

app.Keyboard.KeyBindings.Remove (Key.Esc);

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

        bindings.AddMode ("d <count> d", context =>
        {
            DeleteLines (context.Count);
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

To use `Esc` as the command-mode enter key, remove or replace other `Esc` bindings that should no longer run first. The UICatalog Key Sequences scenario demonstrates `Esc` to enter command mode, `i` to exit command mode, and `: q` to quit.

Use `AddMode` for persistent-mode patterns. `AddMode` parses the pattern without a leader key, so `"d <count> d"` matches after command mode is already active.

## Pattern Syntax

Pattern strings are split on spaces.

| Token | Meaning |
| --- | --- |
| `;` | A literal key. In leader patterns, the first token is the leader key. |
| `k` | A printable literal key. |
| `<Esc>` | A named key parsed by `Key.TryParse`. |
| `<count>` | A numeric repeat count. At most one count token is allowed. |
| `<char>` | Any printable, non-control character. |
| `<key>` | Any valid key. |

To build a pattern in code, use `KeySequencePattern`.

```csharp
KeySequencePattern pattern = KeySequencePattern
                             .Leader (';')
                             .Then ('m')
                             .Count ()
                             .Then ('k');

bindings.Add (pattern, context =>
{
    MoveUp (context.Count);
    return true;
});
```

To create a persistent-mode pattern in code, use `KeySequencePattern.CommandMode ()`.

```csharp
KeySequencePattern pattern = KeySequencePattern
                             .CommandMode ()
                             .Then ('d')
                             .Count ()
                             .Then ('d');
```

## Counts

`<count>` accepts digit keys and exposes the parsed value through `KeySequenceContext.Count`.

If the user omits the count, `Count` is `1`. If a pattern matches a count of `0`, the match is rejected unless `KeySequencePattern.AllowZeroCount` is `true`.

```csharp
KeySequencePattern pattern = KeySequenceParser.Parse ("; g <count> g");
pattern.AllowZeroCount = true;

bindings.Add (pattern, context =>
{
    GoToLine (context.Count);
    return true;
});
```

## Capturing And Routing

View registrations can run in two interception modes:

* `AfterUnhandled` starts capture only after normal view handling leaves a key unhandled.
* `Preemptive` starts capture from the view's `KeyDown` event and continues consuming keys while capture is active.

```csharp
IDisposable registration = editor.UseKeySequences (
    ConfigureBindings,
    KeySequenceInterceptionMode.Preemptive);
```

Application registrations attach to `IApplication.Keyboard.KeyDown` and target the current top runnable view.

```csharp
IDisposable registration = app.UseKeySequences (bindings =>
{
    bindings.Add ("; q", _ =>
    {
        app.RequestStop ();
        return true;
    });
});
```

Some views handle character input before public `KeyDown` subscribers can consume it. To make a sequence key available in that case, attach the sequence handling at a level that receives the key first, use `Preemptive` where appropriate, or remove the view or application binding that conflicts with the sequence.

## State And Feedback

Subscribe to `KeySequenceBindings.StateChanged` to update status bars, command palettes, or mode indicators.

```csharp
bindings.StateChanged += (_, args) =>
{
    modeLabel.Text = args.IsCommandMode ? "COMMAND" : string.Empty;
    countLabel.Text = args.CountText;
};
```

`KeySequenceStateChangedEventArgs` includes:

* `State` - idle, capturing, or command mode.
* `LeaderKey` - the key that started the current leader sequence.
* `Keys` - captured keys after the leader.
* `CountText` - digits captured so far.
* `CandidateCount` - matching patterns that remain possible.
* `Result` - the result that caused the state change.
* `IsCommandMode` - whether persistent command mode is active.

## Timeouts And Cancellation

`KeySequenceBindings.Timeout` controls how long capture may pause between keys. The default is one second. Set it to `TimeSpan.Zero` or a negative value to disable timeout behavior.

```csharp
bindings.Timeout = TimeSpan.FromSeconds (2);
bindings.CancelKey = Key.Esc;
```

Leader mode resets after a match, rejection, cancellation, or timeout. Persistent mode resets the current sequence after a match, rejection, cancellation, or timeout, but stays in command mode until the exit key is pressed.

## Disposal

`UseKeySequences` returns an `IDisposable` registration. Dispose it when the binding lifetime is shorter than the view or application lifetime.

```csharp
IDisposable registration = editor.UseKeySequences (ConfigureBindings);

// Later:
registration.Dispose ();
```

The registration removes event handlers. The view can then use normal keyboard handling again.
