# Keyboard Deep Dive

## See Also

* [Cancellable Work Pattern](cancellable-work-pattern.md)
* [Command Deep Dive](command.md)
* [Mouse Deep Dive](mouse.md)
* [Key Sequences Deep Dive](keysequences.md)
* [Lexicon & Taxonomy](lexicon.md)

## Tenets for Terminal.Gui Keyboard Handling (Unless you know better ones...)

Tenets higher in the list have precedence over tenets lower in the list.

* **Users Have Control** - *Terminal.Gui* provides default key bindings consistent with these tenets, but those defaults are configurable by the user. For example, <xref:Terminal.Gui.Configuration.ConfigurationManager> allows users to redefine key bindings for the system, a user, or an application.

* **More Editor than Command Line** - Once a *Terminal.Gui* app starts, the user is no longer using the command line. Users expect keyboard idioms in TUI apps to be consistent with GUI apps (such as VS Code, Vim, and Emacs). For example, in almost all GUI apps, `Ctrl+V` is `Paste`. But the Linux shells often use `Shift+Insert`. *Terminal.Gui* binds `Ctrl+V` by default.

* **Be Consistent With the User's Platform** - Users get to choose the platform they run *Terminal.Gui* apps on and those apps should respond to keyboard input in a way that is consistent with the platform. For example, on Windows to erase a word to the left, users press `Ctrl+Backspace`. But on Linux, `Ctrl+W` is used.

* **The Source of Truth is Wikipedia** - We use this [Wikipedia article](https://en.wikipedia.org/wiki/Table_of_keyboard_shortcuts) as our guide for default key bindings.

* **If It's Hot, It Works** - If a <xref:Terminal.Gui.ViewBase.View> with a <xref:Terminal.Gui.ViewBase.View.HotKey> is visible, and the HotKey is visible, the user should be able to press that HotKey and whatever behavior is defined for it should work. For example, in v1, when a Modal view was active, the HotKeys on <xref:Terminal.Gui.Views.MenuBar> continued to show "hot". In v2 we strive to ensure this doesn't happen.

## Keyboard APIs

*Terminal.Gui* provides the following APIs for handling keyboard input:

* **Key** - <xref:Terminal.Gui.Input.Key> provides a platform-independent abstraction for common keyboard operations. It is used for processing keyboard input and raising keyboard events. This class provides a high-level abstraction with helper methods and properties for common keyboard operations. Use this class instead of the low-level `KeyCode` enum when possible. `Key` also carries rich metadata:
  - `EventType` — `KeyEventType.Press` (default), `KeyEventType.Repeat`, or `KeyEventType.Release`. Defaults to `Press` so existing code is unaffected. Does not participate in equality.
  - `ModifierKey` — identifies standalone modifier key events (e.g. `ModifierKey.LeftShift`, `ModifierKey.RightCtrl`). Defaults to `ModifierKey.None`.
  - `IsModifierOnly` — `true` when `ModifierKey != ModifierKey.None`, indicating a standalone modifier key press/release with no accompanying character key.
  - `ShiftedKeyCode` — the key code that would be produced with the current modifier state (e.g. Shift+2 on US layout → `(KeyCode)'@'`). Reported by the kitty keyboard protocol when alternate key reporting is enabled (flag 4). Defaults to `KeyCode.Null`. Does not participate in equality.
  - `BaseLayoutKeyCode` — the key code corresponding to the physical key in the standard (US) keyboard layout, regardless of the active input language or modifier state. Reported by the kitty keyboard protocol when alternate key reporting is enabled (flag 4). Defaults to `KeyCode.Null`. Does not participate in equality.
* **Key Bindings** - Key Bindings provide a declarative method for handling keyboard input in <xref:Terminal.Gui.ViewBase.View> implementations. The View calls `AddCommand()`(Terminal.Gui.Command,System.Func{System.Nullable{System.Boolean}}) to declare it supports a particular command and then uses <xref:Terminal.Gui.Input.KeyBindings> to indicate which key presses will invoke the command.
* **Key Events** - The Key Bindings API is rich enough to support the vast majority of use-cases. However, in some cases subscribing directly to key events is needed (e.g. when capturing arbitrary typing by a user). Use `KeyDown` and `KeyUp` events in these cases.

Each of these APIs are described more fully below.

### **[Key Bindings](~/api/Terminal.Gui.Input.KeyBindings.yml)**

Key Bindings is the preferred way of handling keyboard input in <xref:Terminal.Gui.ViewBase.View> implementations. The View calls `AddCommand()`(Terminal.Gui.Command,System.Func{System.Nullable{System.Boolean}}) to declare it supports a particular command and then uses <xref:Terminal.Gui.Input.KeyBindings> to indicate which key presses will invoke the command. For example, if a <xref:Terminal.Gui.ViewBase.View> wants to respond to the user pressing the up arrow key to scroll up it would do this

```cs
public MyView : View
{
  AddCommand (Command.ScrollUp, () => ScrollVertical (-1));
  KeyBindings.Add (Key.CursorUp, Command.ScrollUp);
}
```

The `Character Map` Scenario includes a View called `CharMap` that is a good example of the Key Bindings API. 

The [Command](~/api/Terminal.Gui.Input.Command.yml) enum lists generic operations that are implemented by views. For example <xref:Terminal.Gui.Input.Command.Accept> in a <xref:Terminal.Gui.Views.Button> results in the <xref:Terminal.Gui.ViewBase.View.Accepting> event
firing while in `TableView` it is bound to `CellActivated`. Not all commands
are implemented by all views (e.g. you cannot scroll in a <xref:Terminal.Gui.Views.Button>). Use the `GetSupportedCommands()` method to determine which commands are implemented by a <xref:Terminal.Gui.ViewBase.View>.

The default key for activating a button is `Space`. You can change this using 
`KeyBindings.ReplaceKey()`:

```csharp
var btn = new Button () { Title = "Press me" };
btn.KeyBindings.ReplaceKey (btn.KeyBindings.GetKeyFromCommands (Command.Accept));
```

Key Bindings can be added at the <xref:Terminal.Gui.App.Application> or <xref:Terminal.Gui.ViewBase.View> level.

For **Application-scoped Key Bindings** there are two categories of Application-scoped Key Bindings:

1) **Application Command Key Bindings** - Bindings for <xref:Terminal.Gui.Input.Command>s supported by <xref:Terminal.Gui.App.Application>. For example, `Application.GetDefaultKey (Command.Quit)`, which is bound to `Command.Quit` in `Application.DefaultKeyBindings` and results in <xref:Terminal.Gui.App.Application.RequestStop(Terminal.Gui.App.IRunnable)> being called.
2) **Application Key Bindings** - Bindings for <xref:Terminal.Gui.Input.Command>s supported on arbitrary <xref:Terminal.Gui.ViewBase.View>s that are meant to be invoked regardless of which part of the application is visible/active.

Use `Application.Keyboard.KeyBindings` to add or modify Application-scoped Key Bindings. For backward compatibility, `Application.KeyBindings` also provides access to the same key bindings.

**View-scoped Key Bindings** also have two categories:

1) **HotKey Bindings** - These bind to <xref:Terminal.Gui.Input.Command>s that will be invoked regardless of whether the <xref:Terminal.Gui.ViewBase.View> has focus or not. The most common use-case for `HotKey` bindings is <xref:Terminal.Gui.ViewBase.View.HotKey>. For example, a <xref:Terminal.Gui.Views.Button> with a `Title` of `_OK`, the user can press `Alt-O` and the button will be accepted regardless of whether it has focus or not. Add and modify HotKey bindings with `HotKeyBindings`.
2) **Focused Bindings** - These bind to <xref:Terminal.Gui.Input.Command>s that will be invoked only when the <xref:Terminal.Gui.ViewBase.View> has focus. Focused Key Bindings are the easiest way to enable a <xref:Terminal.Gui.ViewBase.View> to support responding to key events. Add and modify Focused bindings with <xref:Terminal.Gui.Input.KeyBindings>.

**Application-Scoped** Key Bindings 

### HotKey

A **HotKey** is a key press that selects a visible UI item. For selecting items across <xref:Terminal.Gui.ViewBase.View>s (e.g. a <xref:Terminal.Gui.Views.Button> in a <xref:Terminal.Gui.Views.Dialog>) the key press must have the `Alt` modifier. For selecting items within a <xref:Terminal.Gui.ViewBase.View> that are not <xref:Terminal.Gui.ViewBase.View>s themselves, the key press can be key without the `Alt` modifier.  For example, in a <xref:Terminal.Gui.Views.Dialog>, a <xref:Terminal.Gui.Views.Button> with the text of "_Text" can be selected with `Alt+T`. Or, in a `Menu` with "_File _Edit", `Alt+F` will select (show) the Strings.menuFile menu. If the Strings.menuFile menu has a sub-menu of Strings.cmdNew `Alt+N` or `N` will ONLY select the Strings.cmdNew sub-menu if the Strings.menuFile menu is already opened.

By default, the `Text` of a <xref:Terminal.Gui.ViewBase.View> is used to determine the <xref:Terminal.Gui.ViewBase.View.HotKey> by looking for the first occurrence of the `HotKeySpecifier` (which is underscore (`_`) by default). The character following the underscore is the <xref:Terminal.Gui.ViewBase.View.HotKey>. If the `HotKeySpecifier` is not found in `Text`, the first character of `Text` is used as the <xref:Terminal.Gui.ViewBase.View.HotKey>. The `Text` of a <xref:Terminal.Gui.ViewBase.View> can be changed at runtime, and the <xref:Terminal.Gui.ViewBase.View.HotKey> will be updated accordingly. @"Terminal.Gui.View.HotKey" is `virtual` enabling this behavior to be customized.

### **Shortcut**

A **Shortcut** is an opinionated (visually & API) <xref:Terminal.Gui.ViewBase.View> for displaying a command, help text, key key press that invokes a [Command](~/api/Terminal.Gui.Input.Command.yml).

The <xref:Terminal.Gui.Input.Command> can be invoked even if the <xref:Terminal.Gui.ViewBase.View> that defines them is not focused or visible (but the <xref:Terminal.Gui.ViewBase.View> must be enabled). <xref:Terminal.Gui.Views.Shortcut>s can be any key press; `Key.A`, `Key.A.WithCtrl`, `Key.A.WithCtrl.WithAlt`, `Key.Del`, and `Key.F1`, are all valid.

<xref:Terminal.Gui.Views.Shortcut>s are used to define application-wide actions or actions that are not visible (e.g. `Copy`).

[MenuBar](~/api/Terminal.Gui.Views.MenuBar.yml), [PopoverMenu](~/api/Terminal.Gui.Views.PopoverMenu.yml), and [StatusBar](~/api/Terminal.Gui.Views.StatusBar.yml) support <xref:Terminal.Gui.Views.Shortcut>s.

### **Key Events**

Keyboard events are retrieved from [Drivers](drivers.md) each iteration of the [Application](~/api/Terminal.Gui.App.Application.yml) [Main Loop](multitasking.md). The driver raises `IDriver.KeyDown` for press/repeat events and `IDriver.KeyUp` for release events.

<xref:Terminal.Gui.App.Application.RaiseKeyDownEvent(Terminal.Gui.Input.Key)> raises `Application.KeyDown` and then calls `NewKeyDownEvent()` on all runnable <xref:Terminal.Gui.ViewBase.View>s. If no <xref:Terminal.Gui.ViewBase.View> handles the key event, any Application-scoped key bindings will be invoked. Application-scoped key bindings are managed through `Application.Keyboard.KeyBindings`.

`Application.Keyboard.KeyUp` fires for key release events. It routes through the focused view hierarchy via `View.NewKeyUpEvent()` → `View.OnKeyUp()` → `View.KeyUp`. Key bindings are not invoked for key-up events.

> [!NOTE]
> `KeyUp` events are only raised when the driver provides release information. The ANSI driver reports key releases when the terminal supports the [kitty keyboard protocol](https://sw.kovidgoyal.net/kitty/keyboard-protocol/) with event type reporting (flag 2). Terminals that do not support kitty, or drivers that do not implement key-up (e.g. Windows, DotNet), simply never raise `KeyUp`.

If a view is enabled, the `NewKeyDownEvent()` method will do the following:

1) If the view has a subview that has focus, 'NewKeyDown' on the focused view will be called. This is recursive. If the most-focused view handles the key press, processing stops.
2) If there is no most-focused sub-view, or a most-focused sub-view does not handle the key press, `OnKeyDown()` will be called. If the view handles the key press, processing stops.
3) If `OnKeyDown()` does not handle the event. `KeyDown` will be raised.
4) If the view does not handle the key down event, any bindings for the key will be invoked (see the <xref:Terminal.Gui.Input.KeyBindings> property). If the key is bound and any of it's command handlers return true, processing stops.
5) If the key is not bound, or the bound command handlers do not return true, `OnKeyDownNotHandled()` is called.

## **Application Key Handling**

To define application key handling logic for an entire application in cases where the methods listed above are not suitable, use the `Application.KeyDown` event. 

## **Key Down/Up Events**

*Terminal.Gui* supports both key down and key up events:

- `KeyDown` — raised for press and repeat events. This is the primary keyboard event used by most code.
- `KeyUp` — raised for release events. Only available when the driver supports it (currently the ANSI driver with kitty keyboard protocol).

Both events carry a <xref:Terminal.Gui.Input.Key> whose `EventType` property indicates `Press`, `Repeat`, or `Release`. The `EventType` defaults to `Press` and does not affect equality, so existing code that compares keys is unaffected.

## **Kitty Keyboard Protocol**

Terminal.Gui uses the [kitty keyboard protocol](https://sw.kovidgoyal.net/kitty/keyboard-protocol/) to enable enhanced keyboard capabilities when running under a supporting terminal (e.g. Windows Terminal, kitty, WezTerm, foot, Ghostty). The protocol is opt-in: the ANSI driver negotiates it at startup and falls back to legacy parsing when unsupported.

### Flags and Capabilities

The protocol defines progressive enhancement flags, represented by the <xref:Terminal.Gui.Drivers.KittyKeyboardFlags> enum:

| Flag | Value | Description |
|------|-------|-------------|
| `DisambiguateEscapeCodes` | 1 | Encodes keys unambiguously as CSI u sequences instead of legacy escape sequences. |
| `ReportEventTypes` | 2 | Reports press, repeat, and release events. Enables `KeyUp` and repeat `KeyDown` events. |
| `ReportAlternateKeys` | 4 | Reports shifted and base-layout key codes alongside the primary key code. |
| `ReportAllKeysAsEscapeCodes` | 8 | Reports standalone modifier key events (e.g. pressing Shift alone). |
| `ReportAssociatedText` | 16 | Reports the text generated by a key event (not yet implemented). |

Terminal.Gui currently requests flags 1 through 16 (value `31`) from the terminal. The terminal may grant a subset of these based on its capabilities.

For ANSI driver startup, Terminal.Gui performs this negotiation flow:

1. Query kitty support (`CSI ? u`).
2. If supported, send enable flags (`CSI > 1 u` form via `EscSeqUtils.CSI_EnableKittyKeyboardFlags`).
3. Query again (`CSI ? u`) to confirm which flags the terminal actually enabled.
4. Store the confirmed flags in `IDriver.KittyKeyboardCapabilities.Flags`.

On application shutdown, kitty mode is disabled by the main loop coordinator before output disposal.

### Alternate Key Reporting (Flag 4)

When the terminal supports flag 4 (`ReportAlternateKeys`), key events include additional information in two `Key` properties:

- **`ShiftedKeyCode`** — The key code produced by applying the current modifier state. For example, pressing Shift+`2` on a US keyboard reports `ShiftedKeyCode = (KeyCode)'@'`. This is useful for responding to the actual character a user sees rather than the unshifted base key.

- **`BaseLayoutKeyCode`** — The key code for the physical key in the standard US layout, regardless of the active keyboard language. For example, on a French AZERTY keyboard, pressing the physical "A" key (which types "Q" on AZERTY) would report `BaseLayoutKeyCode = (KeyCode)'a'`. This enables keyboard shortcuts that work by physical position rather than by label.

Both default to `KeyCode.Null` when the terminal does not report alternate keys (or doesn't support flag 4). Neither property participates in equality comparisons — two `Key` instances are equal if their `KeyCode` matches, regardless of alternate key data.

#### Kitty CSI u Format

The kitty protocol encodes key events as:

```
CSI code:shifted:base ; modifiers:eventtype u
```

For example, pressing Shift+`a` might produce `\x1b[97:65:97;2u` meaning:
- `97` — primary key code (lowercase `a`)
- `65` — shifted key code (uppercase `A`)
- `97` — base layout key code (lowercase `a` in US layout)
- `2` — modifier (Shift)

#### Example Usage

**NOTE:** Developers are encouraged to use `KeyBinding` for most keyboard input handling. These examples show direct use of `KeyDown` for scenarios where `KeyBinding` is not suitable (e.g. arbitrary text input) and demonstrate how to access alternate key data when available.

```csharp
// Respond to physical key position regardless of keyboard layout
view.KeyDown += (s, key) =>
{
    if (key.BaseLayoutKeyCode != KeyCode.Null)
    {
        // Use the US-layout key for positional shortcuts (e.g. WASD)
        switch (key.BaseLayoutKeyCode)
        {
            case (KeyCode)'w': MoveUp (); break;
            case (KeyCode)'a': MoveLeft (); break;
            case (KeyCode)'s': MoveDown (); break;
            case (KeyCode)'d': MoveRight (); break;
        }
    }
};

// Respond to the shifted character
view.KeyDown += (s, key) =>
{
    if (key.ShiftedKeyCode != KeyCode.Null)
    {
        // ShiftedKeyCode tells you what character the shift state actually produces
        Debug.WriteLine ($"Shifted key: {key.ShiftedKeyCode}");
    }
};
```

# General input model

- The driver generates `KeyDown` events (for press and repeat) and `KeyUp` events (for release, when supported).
- <xref:Terminal.Gui.App.IApplication> implementations subscribe to driver `KeyDown`/`KeyUp` events and forward them to the most-focused `Runnable` view using `View.NewKeyDownEvent` or `View.NewKeyUpEvent` respectively.
- The base (<xref:Terminal.Gui.ViewBase.View>) implementation of `NewKeyDownEvent` follows a pattern of "Before", "During", and "After" processing:
  - **Before**
    - If `Enabled == false` that view should *never* see keyboard (or mouse input).
    - `NewKeyDownEvent` is called on the most-focused SubView (if any) that has focus. If that call returns true, the method returns.
    - Calls `OnKeyDown`.
  - **During**
    - Assuming `OnKeyDown` call returns false (indicating the key wasn't handled) any commands bound to the key will be invoked.
  - **After**
    - Assuming no keybinding was found or all invoked commands were not handled:
       - `OnKeyDownNotHandled` is called to process the key.
       - `KeyDownNotHandled` is raised.

- Subclasses of <xref:Terminal.Gui.ViewBase.View> can (rarely) override `OnKeyDown` (or subscribe to `KeyDown`) to see keys before they are processed
- Subclasses of <xref:Terminal.Gui.ViewBase.View> can (often) override `OnKeyDownNotHandled` to do key processing for keys that were not previously handled. <xref:Terminal.Gui.Views.Editor> and <xref:Terminal.Gui.Views.TextView> are examples.

## Application

* Implements support for `KeyBindingScope.Application`.
* Keyboard functionality is now encapsulated in the <xref:Terminal.Gui.App.IKeyboard> interface, accessed via `Application.Keyboard`.
* `Application.Keyboard` provides access to <xref:Terminal.Gui.Input.KeyBindings>, key binding configuration (via `Application.DefaultKeyBindings` for commands like `Command.Quit`, `Command.Arrange`, and navigation commands), and keyboard event handling.
* For backward compatibility, <xref:Terminal.Gui.App.Application> still exposes static properties/methods that delegate to `Application.Keyboard` (e.g., `IApplication.KeyBindings`, `IApplication.RaiseKeyDownEvent`).
* Exposes cancelable `KeyDown` events (via `Handled = true`). The `RaiseKeyDownEvent` method is public and can be used to simulate keyboard input, although it is preferred to use `InputInjector` for testing.
* The <xref:Terminal.Gui.App.IKeyboard> interface enables testability with isolated keyboard instances that don't depend on static Application state.

## View

* Implements support for <xref:Terminal.Gui.Input.KeyBindings> and `HotKeyBindings`.
* Exposes cancelable non-virtual method for a new key event: `NewKeyDownEvent`.
* Exposes cancelable virtual methods for a new key event: `OnKeyDown`. This method is called by `NewKeyDownEvent` and can be overridden to handle keyboard input.

## IKeyboard Architecture

The <xref:Terminal.Gui.App.IKeyboard> interface provides a decoupled, testable architecture for keyboard handling in Terminal.Gui. This design allows for:

### Key Features

1. **Decoupled State** - All keyboard-related state (key bindings, navigation keys, events) is encapsulated in <xref:Terminal.Gui.App.IKeyboard>, separate from the static <xref:Terminal.Gui.App.Application> class.

2. **Dependency Injection** - The `Keyboard` implementation receives an <xref:Terminal.Gui.App.IApplication> reference, enabling it to interact with application state without static dependencies.

3. **Testability** - Unit tests can create isolated <xref:Terminal.Gui.App.IKeyboard> instances with mock <xref:Terminal.Gui.App.IApplication> references, enabling parallel test execution without interference.

4. **Backward Compatibility** - All existing <xref:Terminal.Gui.App.Application> keyboard APIs (e.g., `Application.KeyBindings`, `Application.RaiseKeyDownEvent`) remain available and delegate to `Application.Keyboard`. Application-level command keys are configured via `Application.DefaultKeyBindings`.

### Usage Examples

**Accessing keyboard functionality:**

```csharp
App.Keyboard.KeyBindings.Add(Key.F1, Command.HotKey);
App.Keyboard.RaiseKeyDownEvent(Key.Enter);
Application.DefaultKeyBindings[Command.Quit] = Bind.All (Key.Q.WithCtrl);
```

**Testing with isolated keyboard instances:**

```csharp
// Create independent keyboard instances for parallel tests
var keyboard1 = new ApplicationKeyboard ();
keyboard1.KeyBindings.Add (Key.Q.WithCtrl, Command.Quit);

var keyboard2 = new ApplicationKeyboard ();
keyboard2.KeyBindings.Add (Key.X.WithCtrl, Command.Quit);

// keyboard1 and keyboard2 maintain completely separate KeyBindings
Assert.True (keyboard1.KeyBindings.TryGet (Key.Q.WithCtrl, out _));
Assert.True (keyboard2.KeyBindings.TryGet (Key.X.WithCtrl, out _));
```

**Accessing application context from views:**

```csharp
public class MyView : View
{
    protected override bool OnKeyDown(Key key)
    {
        // Use View.App instead of static Application
        if (key == Key.F1)
        {
            App?.Keyboard?.KeyBindings.Add(Key.F2, Command.Accept);
            return true;
        }
        return base.OnKeyDown(key);
    }
}
```

### Architecture Benefits

- **Parallel Testing**: Multiple test methods can create and use separate <xref:Terminal.Gui.App.IKeyboard> instances simultaneously without state interference.
- **Dependency Inversion**: `Keyboard` depends on <xref:Terminal.Gui.App.IApplication> interface rather than static <xref:Terminal.Gui.App.Application> class.
- **Cleaner Code**: Keyboard functionality is organized in a dedicated interface rather than scattered across <xref:Terminal.Gui.App.Application> partial classes.
- **Mockability**: Tests can provide mock <xref:Terminal.Gui.App.IApplication> implementations to test keyboard behavior in isolation.

### Implementation Details

The `Keyboard` class implements <xref:Terminal.Gui.App.IKeyboard> and maintains:

- **KeyBindings**: Application-scoped key binding dictionary
- **Navigation Keys**: Configured via Application.DefaultKeyBindings dictionary (Quit, Arrange, NextTabStop, PreviousTabStop, NextTabGroup, PreviousTabGroup, Refresh, Suspend)
- **Events**: `KeyDown` and `KeyUp` events for application-level keyboard monitoring
- **Command Implementations**: Handlers for Application-scoped commands (Quit, Suspend, Navigation, Refresh, Arrange)

The <xref:Terminal.Gui.App.IApplication> implementations create and manage the <xref:Terminal.Gui.App.IKeyboard> instance, setting its `IApplication` property to `this` to provide the necessary <xref:Terminal.Gui.App.IApplication> reference.

## Testing Keyboard Input

> **For comprehensive documentation on testing,** see **[Input Injection](input-injection.md)**.

Terminal.Gui provides a sophisticated input injection system for testing keyboard behavior without requiring actual keyboard hardware. Here's a quick overview:

### Quick Test Example

```csharp
// Create application with virtual time for deterministic testing
VirtualTimeProvider time = new();
using IApplication app = Application.Create(time);
app.Init(DriverRegistry.Names.ANSI);

// Subscribe to key events
app.Keyboard.KeyDown += (s, e) => Console.WriteLine($"Key: {e}");

// Inject keys
app.InjectKey(Key.A);
app.InjectKey(Key.Enter);
app.InjectKey(Key.Esc);
```

### Testing Key Commands

```csharp
VirtualTimeProvider time = new();
using IApplication app = Application.Create(time);
app.Init(DriverRegistry.Names.ANSI);

Button button = new() { Text = "_Click Me" };
bool acceptingCalled = false;
button.Accepting += (s, e) => acceptingCalled = true;

IRunnable runnable = new Runnable();
(runnable as View)?.Add(button);
app.Begin(runnable);

// Inject hotkey (Alt+C)
app.InjectKey(Key.C.WithAlt);

Assert.True(acceptingCalled);
```

### Testing Escape Sequences with Pipeline Mode

```csharp
// Pipeline mode tests full ANSI encoding/decoding
VirtualTimeProvider time = new();
using IApplication app = Application.Create(time);
app.Init(DriverRegistry.Names.ANSI);

IInputInjector injector = app.GetInputInjector();
InputInjectionOptions options = new() { Mode = InputInjectionMode.Pipeline };

// This encodes Key.F1 ? "\x1b[OP", injects chars, parses back to Key.F1
injector.InjectKey(Key.F1, options);
```

### Key Testing Features

- **Virtual Time Control** - Deterministic timing for escape sequence handling
- **Single-Call Injection** - `app.InjectKey(key)` handles everything
- **No Real Delays** - Tests run instantly using virtual time
- **Two Modes** - Direct (default, fast) and Pipeline (full ANSI encoding)
- **Escape Sequence Handling** - Automatic release of stale escapes

**Learn More:** See **[Input Injection](input-injection.md)** for complete documentation including:
- Architecture and design
- Testing patterns and best practices
- Advanced scenarios (modifier keys, function keys, special keys)
- Troubleshooting guide

## Configurable Key Bindings

Terminal.Gui uses a layered, platform-aware key binding architecture. All default key bindings are defined declaratively using `PlatformKeyBinding` records and can be overridden via [ConfigurationManager](config.md).

### Three Layers

Key bindings are organized in three layers, applied from lowest to highest priority:

1. **`Application.DefaultKeyBindings`** - Application-wide bindings for commands like Quit, Suspend, Arrange, and tab navigation. This is a `[ConfigurationProperty]` and can be overridden via configuration.

2. **`View.DefaultKeyBindings`** - Shared base bindings for all views, covering navigation (cursor keys, Home, End), clipboard (Copy, Cut, Paste), and editing (Undo, Redo, Delete). This is also a `[ConfigurationProperty]`.

3. **Per-view `DefaultKeyBindings`** - View-specific bindings that layer on top of the base. For example, `TextField.DefaultKeyBindings` adds Emacs-style navigation (`Ctrl+B`, `Ctrl+F`), word movement (`Ctrl+CursorLeft`), and kill commands (`Ctrl+K`). These are plain static properties, not configurable via ConfigurationManager.

Each view's constructor calls `ApplyKeyBindings (View.DefaultKeyBindings, <ViewType>.DefaultKeyBindings)` to combine the layers. Only commands that the view actually supports (via `GetSupportedCommands ()`) are bound. Keys already bound by a lower layer are not overwritten by a higher layer.

### Platform-Aware Bindings

Key bindings can vary by operating system using the `PlatformKeyBinding` record:

```csharp
public record PlatformKeyBinding
{
    public string []? All { get; init; }      // All platforms
    public string []? Windows { get; init; }  // Windows only
    public string []? Linux { get; init; }    // Linux only
    public string []? Macos { get; init; }    // macOS only
}
```

`All` keys apply on every platform. Platform-specific arrays add additional bindings on that platform. For example, `Undo` is bound to `Ctrl+Z` everywhere, but also to `Ctrl+/` on Linux and macOS:

```csharp
["Undo"] = Bind.AllPlus ("Ctrl+Z", nonWindows: ["Ctrl+/"]),
```

The `Bind` helper class provides factory methods:

| Method | Description |
|--------|-------------|
| `Bind.All (...)` | Same keys on all platforms |
| `Bind.AllPlus (key, nonWindows, windows, linux, macos)` | A base key on all platforms, plus platform-specific extras |
| `Bind.NonWindows (...)` | Keys that apply only on Linux and macOS |
| `Bind.Platform (windows, linux, macos)` | Fully platform-specific, no shared keys |

### User Overrides via Configuration

Users can override key bindings for any view type using `View.ViewKeyBindings` in a configuration file. The outer key is the view type name; the inner dictionary maps command names to `PlatformKeyBinding` objects:

```json
{
  "View.ViewKeyBindings": {
    "TextField": {
      "Undo": { "All": ["Ctrl+Z"] },
      "CutToEndOfLine": { "All": ["Ctrl+K"] }
    },
    "TextView": {
      "Redo": { "All": ["Ctrl+Shift+Z"], "Windows": ["Ctrl+Y"] }
    }
  }
}
```

`ViewKeyBindings` overrides are applied last (highest priority), after both `View.DefaultKeyBindings` and per-view `DefaultKeyBindings`.

Application-level defaults can also be overridden:

```json
{
  "Application.DefaultKeyBindings": {
    "Quit": { "All": ["Ctrl+Q"] },
    "Suspend": { "Linux": ["Ctrl+Z"], "Macos": ["Ctrl+Z"] }
  }
}
```

### Resolution Order

When a view is created, key bindings are resolved in this order:

1. `View.DefaultKeyBindings` (base layer - navigation, clipboard, editing)
2. Per-view `DefaultKeyBindings` (e.g., `TextField.DefaultKeyBindings`)
3. `View.ViewKeyBindings` user overrides (from configuration)

At each layer, only commands supported by the view are bound, and keys already bound by a previous layer are skipped. This means user overrides take effect because they are applied last, after the default layers have established their bindings.

## Keyboard Tracing

For debugging keyboard event flow, use the `Trace` class from the `Terminal.Gui.Tracing` namespace:

```csharp
using Terminal.Gui.Tracing;

Trace.KeyboardEnabled = true;
```

When enabled, keyboard events are logged via `Logging.Trace` showing the flow from Driver → Application → View. Enable via:

- **Code**: `Trace.KeyboardEnabled = true;`
- **Config**: `"Trace.KeyboardEnabled": true`
- **UICatalog**: Logging menu → Keyboard Trace

See [Logging - View Event Tracing](logging.md#view-event-tracing) for more details.
