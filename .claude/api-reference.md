# Terminal.Gui API Reference (Compressed)

> Condensed reference for AI agents. For full details, see `docfx/docs/*.md`.

---

## Application Architecture

### Instance-Based Pattern (v2)

```csharp
// RECOMMENDED: Instance-based with using statement
using (IApplication app = Application.Create ().Init ())
{
    app.Run<MyDialog> ();
    MyResult? result = app.GetResult<MyResult> ();
}

// LEGACY (obsolete): Static Application
Application.Init ();
Application.Run (top);
Application.Shutdown ();
```

### Key Properties

- **`IApplication.TopRunnable`** - Current modal runnable (top of SessionStack)
- **`IApplication.SessionStack`** - Stack of running IRunnable sessions
- **`View.App`** - Application context for a view (use instead of static `Application`)
- **`View.Driver`** - Driver access for rendering

### IRunnable Pattern

Views can implement `IRunnable<TResult>` for typed results:

```csharp
public class MyDialog : Runnable<string?>
{
    protected override bool OnIsRunningChanging (bool oldValue, bool newValue)
    {
        if (!newValue) // Stopping
        {
            Result = _textField.Text; // Extract result before disposal
        }
        return base.OnIsRunningChanging (oldValue, newValue);
    }
}
```

**Disposal**: "Whoever creates it, owns it" - `Run<T>()` auto-disposes, `Run(instance)` requires manual disposal.

### RunnableWrapper<TView, TResult>

`RunnableWrapper<TView, TResult>` wraps a `View` as a runnable without adding prompt buttons.

- Clears wrapper `KeyBindings` and `MouseBindings` so the wrapped view handles input.
- Sets `CommandsToBubbleUp = [Command.Accept]`.
- On accept, extracts result via `ResultExtractor` if set, otherwise from `IValue<TResult>.Value` when implemented.
- Unlike `Prompt`, it does not add OK/Cancel buttons.

---

## View Hierarchy

### Terminology (CRITICAL)

| Term | Meaning |
|------|---------|
| **SuperView** | Container view (via `Add()`) |
| **SubView** | Contained view (added via `Add()`) |
| **Parent/Child** | Non-containment references ONLY (rare) |

### View Composition (Layers)

```
Frame (outermost - SuperView-relative coords)
└── Margin (spacing/shadows)
    └── Border (visual frame, title)
        └── Padding (spacing, scrollbars)
            └── Viewport (visible content window)
                └── Content Area (drawable area)
```

### Key Properties

- **`Frame`** - Location/size in SuperView coordinates (includes adornments)
- **`Viewport`** - Visible portion of content (content-relative coords)
- **`GetContentSize()`** - Total content area size
- **`SetContentSize()`** - Enable scrolling by setting larger than Viewport

---

## Layout System

### Pos (Position)

```csharp
view.X = 10;                          // Absolute
view.X = Pos.Percent (50);            // Percentage of SuperView
view.X = Pos.Center ();               // Centered
view.X = Pos.Right (otherView) + 1;   // Relative to another view
view.X = Pos.AnchorEnd (5);           // From right edge
view.X = Pos.Align (Alignment.End);   // Aligned with peers
```

### Dim (Dimension)

```csharp
view.Width = 20;                      // Absolute
view.Width = Dim.Fill ();             // Fill available space
view.Width = Dim.Percent (50);        // Percentage
view.Width = Dim.Auto ();             // Size to content
view.Width = Dim.Width (otherView);   // Match another view
```

### Layout Lifecycle

1. `SetNeedsLayout()` marks view for layout
2. `Layout()` calculates Frame/Viewport
3. `LayoutStarted` event raised
4. SubViews laid out recursively
5. `LayoutComplete` event raised

---

## Command System

### Core Commands

| Command | Trigger | Purpose |
|---------|---------|---------|
| `Command.Activate` | Space, Click | State change (toggle, select) |
| `Command.Accept` | Enter, DblClick | Confirm action (submit, execute) |
| `Command.HotKey` | Alt+Key | Direct access regardless of focus |

### Command Pattern

```csharp
// Register command
AddCommand (Command.Accept, ctx =>
{
    // Handle command
    return true; // Handled
});

// Bind key to command
KeyBindings.Add (Key.Enter, Command.Accept);

// Bind mouse to command
MouseBindings.Add (MouseFlags.LeftButtonClicked, Command.Activate);
```

### Activating vs Accepting

- **`Activating`** - State change/preparation (local, no propagation)
  - CheckBox: toggles state
  - ListView: selects item
  - MenuItem: focuses item

- **`Accepting`** - Confirms/finalizes action (propagates to SuperView)
  - Button: executes action
  - Dialog: submits
  - MenuItem: executes command

---

## Cancellable Work Pattern (CWP)

### Structure

```csharp
// Internal raise method
internal void RaiseXxx (EventArgs args)
{
    // 1. Do work BEFORE notifications
    DoActualWork ();

    // 2. Call virtual method (empty in base class)
    OnXxx (args);

    // 3. Raise event
    Xxx?.Invoke (this, args);
}

// Virtual method - empty in base, subclasses override
protected virtual void OnXxx (EventArgs args) { }
```

### Event Handling

```csharp
// Subscribe to event
view.Accepting += (sender, e) =>
{
    e.Handled = true; // Mark as handled
    // e.Cancel = true; // Cancel the operation (if cancellable)
};

// Override virtual method in subclass
protected override bool OnAccepting (CommandEventArgs args)
{
    if (ShouldCancel)
        return true; // Cancel
    return base.OnAccepting (args);
}
```

### Common Events

| Event | Virtual Method | Cancellable | Purpose |
|-------|---------------|-------------|---------|
| `Activating` | `OnActivating` | Yes | Before state change |
| `Accepting` | `OnAccepting` | Yes | Before action confirm |
| `HasFocusChanging` | `OnHasFocusChanging` | Yes | Before focus change |
| `HasFocusChanged` | `OnHasFocusChanged` | No | After focus change |
| `DrawingContent` | `OnDrawingContent` | Yes | Before content draw |

---

## Navigation & Focus

### Focus Requirements

1. `Visible = true`
2. `Enabled = true`
3. `CanFocus = true`
4. `TabStop != TabBehavior.NoStop` (for keyboard nav)

### TabBehavior

| Value | Behavior |
|-------|----------|
| `NoStop` | Skip in keyboard nav (mouse can still focus) |
| `TabStop` | Standard Tab navigation |
| `TabGroup` | F6 navigation between containers |

### Navigation Keys

- `Tab` / `Shift+Tab` - Navigate TabStop views
- `F6` / `Shift+F6` - Navigate TabGroup containers
- Arrow keys - Navigate within/between views
- HotKeys (`Alt+X`) - Direct access

### Focus Methods

```csharp
view.SetFocus ();                    // Request focus
view.HasFocus                        // Check if focused
Application.Navigation.GetFocused () // Get most-focused view
Application.Navigation.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
```

---

## Keyboard Handling

### Key Bindings

```csharp
// View-scoped (focused)
view.KeyBindings.Add (Key.Enter, Command.Accept);

// View-scoped (hotkey - works without focus)
view.HotKeyBindings.Add (Key.S.WithAlt, Command.HotKey);

// Application-scoped
app.Keyboard.KeyBindings.Add (Key.Q.WithCtrl, Command.Quit);
```

### Key Processing Order

1. Most-focused SubView processes first
2. `OnKeyDown()` virtual method
3. `KeyDown` event
4. KeyBindings lookup → Command execution
5. `OnKeyDownNotHandled()` for unhandled keys

### IKeyboard Interface

```csharp
// Modern (instance-based)
App.Keyboard.KeyBindings.Add (Key.F1, Command.HotKey);
App.Keyboard.QuitKey = Key.Q.WithCtrl;

// Legacy (delegates to Application.Keyboard)
Application.KeyBindings.Add (Key.F1, Command.HotKey);
```

---

## Mouse Handling

### Mouse Bindings

```csharp
view.MouseBindings.Add (MouseFlags.LeftButtonClicked, Command.Activate);
view.MouseBindings.Add (MouseFlags.LeftButtonDoubleClicked, Command.Accept);
```

### Mouse Events

- `MouseEnter` / `MouseLeave` - Hover tracking
- `MouseEvent` - Low-level mouse handling
- `Highlight` - Visual feedback on hover/click

---

## Drawing

### Drawing Methods

```csharp
protected override bool OnDrawingContent ()
{
    Move (0, 0);                              // Position draw cursor
    SetAttributeForRole (VisualRole.Normal);  // Set colors
    AddStr ("Hello");                         // Draw text
    AddRune ('X');                            // Draw single char
    return true; // Handled
}
```

### Key Concepts

- **Draw Cursor** - Internal rendering position (`Move()`)
- **Terminal Cursor** - Visible cursor (`View.Cursor` property)
- **Attribute** - Color and text style combination
- **LineCanvas** - Auto-joining line drawing

### Drawing Lifecycle

1. `SetNeedsDraw()` marks view for redraw
2. `Draw()` called by framework
3. `DrawingContent` event raised
4. `OnDrawingContent()` called
5. Adornments drawn
6. SubViews drawn recursively

---

## Color & Theming

### Scheme

```csharp
view.Scheme = new Scheme
{
    Normal = new Attribute (Color.White, Color.Black),
    Focus = new Attribute (Color.Black, Color.White),
    HotNormal = new Attribute (Color.Yellow, Color.Black),
    HotFocus = new Attribute (Color.Yellow, Color.White),
    Disabled = new Attribute (Color.Gray, Color.Black)
};
```

### VisualRole

```csharp
SetAttributeForRole (VisualRole.Normal);    // Standard
SetAttributeForRole (VisualRole.Focus);     // Focused
SetAttributeForRole (VisualRole.HotNormal); // HotKey
SetAttributeForRole (VisualRole.Disabled);  // Disabled
```

---

## Testing

### Input Injection

```csharp
VirtualTimeProvider time = new ();
using IApplication app = Application.Create (time);
app.Init (DriverRegistry.Names.ANSI);

// Inject keyboard input
app.InjectKey (Key.A);
app.InjectKey (Key.Enter);

// No real delays - uses virtual time
```

### Test Patterns

```csharp
[Fact]
public void MyView_WorksCorrectly ()
{
    // Arrange
    View view = new () { Width = 10, Height = 5 };

    // Act
    view.SetNeedsDraw ();

    // Assert
    Assert.True (view.NeedsDraw);
}
```

**Avoid**: `Application.Init()` in tests unless testing Application-specific functionality.

---

## Common Patterns

### Creating Custom Views

```csharp
public class MyView : View
{
    public MyView ()
    {
        CanFocus = true;
        Width = Dim.Auto ();
        Height = Dim.Auto ();

        AddCommand (Command.Accept, HandleAccept);
        KeyBindings.Add (Key.Enter, Command.Accept);
    }

    protected override bool OnDrawingContent ()
    {
        Move (0, 0);
        AddStr ("Custom content");
        return true;
    }

    private bool HandleAccept () => true;
}
```

### Adding SubViews

```csharp
View container = new () { Width = Dim.Fill (), Height = Dim.Fill () };
Button btn1 = new () { Text = "OK", X = 2, Y = 2 };
Button btn2 = new () { Text = "Cancel", X = Pos.Right (btn1) + 2, Y = 2 };
container.Add (btn1, btn2);
```

### Dialogs with Results

```csharp
using (IApplication app = Application.Create ().Init ())
{
    app.Run<ConfirmDialog> ();
    bool? confirmed = app.GetResult<bool> ();
}
```

---

## Quick Reference

### IValue<T> Pattern

All typed views expose values through `IValue<T>.Value`. Avoid guessing member names like `.Date`, `.Time`, or `.Color`.

| View | IValue<T> |
|------|-----------|
| TextField | `IValue<string>` |
| NumericUpDown<T> | `IValue<T>` |
| DatePicker | `IValue<DateTime>` |
| TimeEditor | `IValue<TimeSpan>` |
| ColorPicker | `IValue<Color?>` |
| AttributePicker | `IValue<Attribute?>` |
| CheckBox | `IValue<CheckState>` |
| OptionSelector | `IValue<int?>` |
| FlagSelector | `IValue<int?>` |

Implementing `IValue<T>` requires `ValueChanging`, `ValueChanged`, and `ValueChangedUntyped`.

### View Properties

| Property | Purpose |
|----------|---------|
| `X`, `Y` | Position (Pos) |
| `Width`, `Height` | Size (Dim) |
| `Frame` | SuperView-relative bounds |
| `Viewport` | Content-relative visible area |
| `Text` | View text content |
| `Title` | Title (shown in Border) |
| `Visible` | Show/hide |
| `Enabled` | Enable/disable |
| `CanFocus` | Focusable |
| `TabStop` | Keyboard navigation behavior |

### Important Methods

| Method | Purpose |
|--------|---------|
| `Add()` | Add SubView |
| `Remove()` | Remove SubView |
| `SetFocus()` | Request focus |
| `SetNeedsDraw()` | Mark for redraw |
| `SetNeedsLayout()` | Mark for layout |
| `InvokeCommand()` | Execute command |
| `Dispose()` | Clean up resources |

### Driver Names

```csharp
DriverRegistry.Names.ANSI    // "ansi"
DriverRegistry.Names.WINDOWS // "windows"
DriverRegistry.Names.UNIX    // "unix"
DriverRegistry.Names.DOTNET  // "dotnet"
```

### Gotchas

- `Terminal.Gui.Drawing.Attribute` may conflict with `System.Attribute` with implicit usings. Use `using TgAttribute = Terminal.Gui.Drawing.Attribute;` or fully qualify.
- `Color.TryParse (string, out Color?)` uses nullable out; `Color.TryParse (string?, IFormatProvider?, out Color)` uses non-nullable out.
- `OpenDialog` results are exposed through `FilePaths`; related state includes `AllowsMultipleSelection`, `Canceled`, and `OpenMode`.
