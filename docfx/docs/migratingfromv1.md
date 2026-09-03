# Migrating From v1 To v2

This document provides a comprehensive guide for migrating applications from Terminal.Gui v1 to v2. 

For detailed breaking change documentation, check out this Discussion: https://github.com/tui-cs/Terminal.Gui/discussions/2448

## Table of Contents

- [Overview of Major Changes](#overview-of-major-changes)
- [Application Architecture](#application-architecture)
- [Configuration](#configuration)
- [View Construction and Initialization](#view-construction-and-initialization)
- [Layout System Changes](#layout-system-changes)
- [Color and Attribute Changes](#color-and-attribute-changes)
- [Type Changes](#type-changes)
- [Unicode and Text](#unicode-and-text)
- [Keyboard API](#keyboard-api)
- [Mouse API](#mouse-api)
- [Navigation Changes](#navigation-changes)
- [Scrolling Changes](#scrolling-changes)
- [Adornments](#adornments)
- [Event Pattern Changes](#event-pattern-changes)
- [Cursor Management](#cursor-management)
- [View-Specific Changes](#view-specific-changes)
- [Disposal and Resource Management](#disposal-and-resource-management)
- [API Terminology Changes](#api-terminology-changes)

---

## Overview of Major Changes

Terminal.Gui v2 represents a major architectural evolution with these key improvements:

1. **Instance-Based Application Model** - Move from static <xref:Terminal.Gui.App.Application> to <xref:Terminal.Gui.App.IApplication> instances
2. **IRunnable Architecture** - Interface-based runnable pattern with type-safe results
3. **Simplified Layout** - Removed Absolute/Computed distinction, improved adornments
4. **24-bit TrueColor** - Full color support by default
5. **Enhanced Input** - Better keyboard and mouse APIs
6. **Built-in Scrolling** - All views support scrolling inherently
7. **Fluent API** - Method chaining for elegant code
8. **Proper Disposal** - IDisposable pattern throughout

---

## Application Architecture

### Instance-Based Application Model

**v1 Pattern (Static):**
```csharp
// v1 - static Application
Application.Init();
var top = Application.Top;
top.Add(myView);
Application.Run();
Application.Shutdown();
```

**v2 Recommended Pattern (Instance-Based):**
```csharp
// v2 - instance-based with using statement
using (var app = Application.Create().Init())
{
    var top = new Window();
    top.Add(myView);
    app.Run(top);
    top.Dispose();
} // app.Dispose() called automatically
```

**v2 Legacy Pattern (Still Works):**
```csharp
// v2 - legacy static (marked obsolete but functional)
Application.Init();
var top = new Window();
top.Add(myView);
Application.Run(top);
top.Dispose();
Application.Shutdown(); // Obsolete - use Dispose() instead
```

### IRunnable Architecture

v2 introduces <xref:Terminal.Gui.App.IRunnable> for type-safe, runnable views:

```csharp
// Create a dialog that returns a typed result
public class FileDialog : Runnable<string>
{
    // Implementation
}

// Use it
using (var app = Application.Create().Init())
{
    app.Run<FileDialog>();
    string? result = app.GetResult<string>();
    
    if (result is { })
    {
        OpenFile(result);
    }
}
```

**Key Benefits:**
- Type-safe results (no casting)
- Automatic disposal of framework-created runnables
- CWP-compliant lifecycle events
- Works with any View (not just Toplevel)

### Disposal and Resource Management

v2 requires explicit disposal:

```csharp
// ❌ v1 - Application.Shutdown() disposed everything
Application.Init();
var top = new Window();
Application.Run(top);
Application.Shutdown(); // Disposed top automatically

// ✅ v2 - Dispose views explicitly
using (var app = Application.Create().Init())
{
    var top = new Window();
    app.Run(top);
    top.Dispose(); // Must dispose
}

// ✅ v2 - Framework-created runnables disposed automatically
using (var app = Application.Create().Init())
{
    app.Run<ColorPickerDialog>();
    var result = app.GetResult<Color>();
}
```

**Disposal Rules:**
- "Whoever creates it, owns it"
- `Run<TRunnable>()`: Framework creates → Framework disposes
- `Run(IRunnable)`: Caller creates → Caller disposes
- Always dispose <xref:Terminal.Gui.App.IApplication> (use `using` statement)

### View.App Property

Views now have an `App` property for accessing the application context:

```csharp
// ❌ v1 - Direct static reference
Application.Driver.Move(x, y);

// ✅ v2 - Use View.App
App?.Driver.Move(x, y);

// ✅ v2 - Dependency injection
public class MyView : View
{
    private readonly IApplication _app;
    
    public MyView(IApplication app)
    {
        _app = app;
    }
}
```

---

## Configuration

v1 exposed `ConfigurationManager` (typically `ConfigurationManager.Enable`). That API is gone in 2.5.0.

To load themes and settings in v2, use <xref:Terminal.Gui.Configuration.TuiConfigurationBuilder> and Settings POCOs. Do not call `ConfigurationManager.Enable`.

To convert a pre-2.5.0 `config.json`, see [Migrating ConfigurationManager to TuiConfigurationBuilder](migrate-cm-to-mec.md). For the other 2.5.0 API breaks, see [2.5.0 Breaking Changes](breaking-changes-2.5.0.md).

---

## View Construction and Initialization

### Constructors → Initializers

**v1:**
```csharp
var myView = new View(new Rect(10, 10, 40, 10));
```

**v2:**
```csharp
var myView = new View 
{ 
    X = 10, 
    Y = 10, 
    Width = 40, 
    Height = 10 
};
```

### Initialization Pattern

v2 uses `ISupportInitializeNotification`:

```csharp
// v1 - No explicit initialization
var view = new View();
Application.Run(view);

// v2 - Automatic initialization via BeginInit/EndInit
var view = new View();
// BeginInit() called automatically when added to SuperView
// EndInit() called automatically
// Initialized event raised after EndInit()
```

---

## Layout System Changes

### Removed LayoutStyle Distinction

v1 had `Absolute` and `Computed` layout styles. v2 removed this distinction.

**v1:**
```csharp
view.LayoutStyle = LayoutStyle.Computed;
```

**v2:**
```csharp
// No LayoutStyle - all layout is declarative via Pos/Dim
view.X = Pos.Center();
view.Y = Pos.Center();
view.Width = Dim.Percent(50);
view.Height = Dim.Fill();
```

### Frame vs Bounds

**v1:**
- `Frame` - Position/size in SuperView coordinates
- `Bounds` - Always `{0, 0, Width, Height}` (location always empty)

**v2:**
- `Frame` - Position/size in SuperView coordinates (same as v1)
- `Viewport` - Visible area in content coordinates (replaces Bounds)
  - **Important**: `Viewport.Location` can now be non-zero for scrolling

```csharp
// ❌ v1
var size = view.Bounds.Size;
Debug.Assert(view.Bounds.Location == Point.Empty); // Always true

// ✅ v2
var visibleArea = view.Viewport;
var contentSize = view.GetContentSize();

// Viewport.Location can be non-zero when scrolled
view.ScrollVertical(10);
Debug.Assert(view.Viewport.Location.Y == 10);
```

### Pos and Dim API Changes

| v1 | v2 |
|----|-----|
| `Pos.At(x)` | `Pos.Absolute(x)` |
| `Dim.Sized(width)` | `Dim.Absolute(width)` |
| `Pos.Anchor()` | `Pos.GetAnchor()` |
| `Dim.Anchor()` | `Dim.GetAnchor()` |

```csharp
// ❌ v1
view.X = Pos.At(10);
view.Width = Dim.Sized(20);

// ✅ v2
view.X = Pos.Absolute(10);
view.Width = Dim.Absolute(20);
```

### View.AutoSize Removed

**v1:**
```csharp
view.AutoSize = true;
```

**v2:**
```csharp
view.Width = Dim.Auto();
view.Height = Dim.Auto();
```

See [Dim.Auto Deep Dive](dimauto.md) for details.

---

## Adornments

v2 adds `Border`, `Margin`, and `Padding` as built-in adornments.

**v1:**
```csharp
// Custom border drawing
view.Border = new Border { /* ... */ };
```

**v2:**
```csharp
// Built-in Border adornment
view.BorderStyle = LineStyle.Single;
view.Border.Thickness = new Thickness(1);
view.Title = "My View";

// Built-in Margin and Padding
view.Margin.Thickness = new Thickness(2);
view.Padding.Thickness = new Thickness(1);
```

See [Layout Deep Dive](layout.md) for complete details.

---

## Color and Attribute Changes

### 24-bit TrueColor Default

v2 uses 24-bit color by default.

```csharp
// v1 - Limited color palette
var color = Color.Brown;

// v2 - ANSI-compliant names + TrueColor
var color = Color.Yellow; // Brown renamed
var customColor = new Color(0xFF, 0x99, 0x00); // 24-bit RGB
```

### Attribute.Make Removed

**v1:**
```csharp
var attr = Attribute.Make(Color.BrightMagenta, Color.Blue);
```

**v2:**
```csharp
var attr = new Attribute(Color.BrightMagenta, Color.Blue);
```

### Color Name Changes

| v1 | v2 |
|----|-----|
| `Color.Brown` | `Color.Yellow` |

---

## Type Changes

### Low-Level Types

| v1 | v2 |
|----|-----|
| `Rect` | `Rectangle` |
| `Point` | `Point` |
| `Size` | `Size` |

```csharp
// ❌ v1
Rect rect = new Rect(0, 0, 10, 10);

// ✅ v2
Rectangle rect = new Rectangle(0, 0, 10, 10);
```

---

## Unicode and Text

### NStack.ustring Removed

**v1:**
```csharp
using NStack;
ustring text = "Hello";
var width = text.Sum(c => Rune.ColumnWidth(c));
```

**v2:**
```csharp
using System.Text;
string text = "Hello";
var width = text.GetColumns(); // Extension method
```

### Rune Changes

**v1:**
```csharp
// Implicit cast
myView.AddRune(col, row, '▄');

// Width
var width = Rune.ColumnWidth(rune);
```

**v2:**
```csharp
// Explicit constructor
myView.AddRune(col, row, new Rune('▄'));

// Width
var width = rune.GetColumns();
```

See [Unicode](https://tui-cs.github.io/Terminal.Gui/docs/overview#unicode) for details.

---

## Keyboard API

v2 has a completely redesigned keyboard API.

### Key Class

**v1:**
```csharp
KeyEvent keyEvent;
if (keyEvent.KeyCode == KeyCode.Enter) { }
```

**v2:**
```csharp
Key key;
if (key == Key.Enter) { }

// Modifiers
if (key.Shift) { }
if (key.Ctrl) { }

// With modifiers
Key ctrlC = Key.C.WithCtrl;
Key shiftF1 = Key.F1.WithShift;
```

### Key Bindings

**v1:**
```csharp
// Override OnKeyPress
protected override bool OnKeyPress(KeyEvent keyEvent)
{
    if (keyEvent.KeyCode == KeyCode.Enter)
    {
        // Handle
        return true;
    }
    return base.OnKeyPress(keyEvent);
}
```

**v2:**
```csharp
// Use KeyBindings + Commands
AddCommand(Command.Accept, HandleAccept);
KeyBindings.Add(Key.Enter, Command.Accept);

private bool HandleAccept()
{
    // Handle
    return true;
}
```

### Application-Wide Keys

**v1:**
```csharp
// Hard-coded Ctrl+Q
if (keyEvent.Key == Key.CtrlMask | Key.Q)
{
    Application.RequestStop();
}
```

**v2:**
```csharp
// Configurable quit key
if (key == Application.GetDefaultKey (Command.Quit))
{
    Application.RequestStop ();
}

// Change the quit key via DefaultKeyBindings
Application.DefaultKeyBindings[Command.Quit] = Bind.All (Key.Esc);
```

### Navigation Keys

v2 has consistent, configurable navigation keys:

| Key | Purpose |
|-----|---------|
| `Tab` | Next TabStop |
| `Shift+Tab` | Previous TabStop |
| `F6` | Next TabGroup |
| `Shift+F6` | Previous TabGroup |

```csharp
// Configurable via Application.DefaultKeyBindings
Application.GetDefaultKey (Command.NextTabStop);    // Key.Tab
Application.GetDefaultKey (Command.PreviousTabStop); // Key.Tab.WithShift
Application.GetDefaultKey (Command.NextTabGroup);    // Key.F6
Application.GetDefaultKey (Command.PreviousTabGroup); // Key.F6.WithShift
```

See [Keyboard Deep Dive](keyboard.md) for complete details.

---

## Mouse API

### MouseEventEventArgs → MouseEventArgs

**v1:**
```csharp
void HandleMouse(MouseEventEventArgs args) { }
```

**v2:**
```csharp
void HandleMouse(object? sender, MouseEventArgs args) { }
```

### Mouse Coordinates

**v1:**
- Mouse coordinates were screen-relative

**v2:**
- Mouse coordinates are now **Viewport-relative**

```csharp
// v2 - Viewport-relative coordinates
view.MouseEvent += (s, e) =>
{
    // e.Position is relative to view's Viewport
    var x = e.Position.X; // 0 = left edge of viewport
    var y = e.Position.Y; // 0 = top edge of viewport
};
```

### Mouse Click Handling

**v1:**
```csharp
// v1 - MouseClick event
view.MouseClick += (mouseEvent) =>
{
    // Handle click
    DoSomething();
};
```

**v2:**
```csharp
// v2 - Use MouseBindings + Commands + Activating event
view.MouseBindings.Add(MouseFlags.Button1Clicked, Command.Activate);
view.Activating += (s, e) =>
{
    // Handle selection (called when Button1Clicked)
    DoSomething();
};

// Alternative: Use MouseEvent for low-level handling
view.MouseEvent += (s, e) =>
{
    if (e.Flags.HasFlag(MouseFlags.Button1Clicked))
    {
        DoSomething();
        e.Handled = true;
    }
};
```

**Key Changes:**
- `View.MouseClick` event has been **removed**
- Use `MouseBindings` to map mouse events to <xref:Terminal.Gui.Input.Command>s
- Default mouse bindings invoke <xref:Terminal.Gui.Input.Command.Activate> which raises the <xref:Terminal.Gui.ViewBase.View.Activating> event
- For custom behavior, override `OnActivating` or subscribe to the <xref:Terminal.Gui.ViewBase.View.Activating> event
- For low-level mouse handling, use `MouseEvent` directly

**Migration Pattern:**
```csharp
// ❌ v1 - OnMouseClick override
protected override bool OnMouseClick(MouseEventArgs mouseEvent)
{
    if (mouseEvent.Flags.HasFlag(MouseFlags.Button1Clicked))
    {
        PerformAction();
        return true;
    }
    return base.OnMouseClick(mouseEvent);
}

// ✅ v2 - OnActivating override
protected override bool OnActivating(CommandEventArgs args)
{
    if (args.Context is CommandContext { Binding.MouseEventArgs: { } mouseArgs })
    {
        // Access mouse position and flags via context
        if (mouseArgs.Flags.HasFlag(MouseFlags.Button1Clicked))
        {
            PerformAction();
            return true;
        }
    }
    return base.OnActivating(args);
}

// ✅ v2 - Activating event (simpler)
view.Activating += (s, e) =>
{
    PerformAction();
    e.Handled = true;
};
```

**Accessing Mouse Position in Activating Event:**
```csharp
view.Activating += (s, e) =>
{
    // Extract mouse event args from command context
    if (e.Context is CommandContext { Binding.MouseEventArgs: { } mouseArgs })
    {
        Point position = mouseArgs.Position;
        MouseFlags flags = mouseArgs.Flags;
        
        // Use position and flags for custom logic
        HandleClick(position, flags);
        e.Handled = true;
    }
};
```

### Mouse State and Highlighting

v2 adds enhanced mouse state tracking:

```csharp
// Configure which mouse states trigger highlighting
view.HighlightStates = MouseState.In | MouseState.Pressed;

// React to mouse state changes
view.MouseStateChanged += (s, e) =>
{
    switch (e.Value)
    {
        case MouseState.In:
            // Mouse entered view
            break;
        case MouseState.Pressed:
            // Mouse button pressed in view
            break;
    }
};
```

See [Mouse Deep Dive](mouse.md) for complete details.

---

## Navigation Changes

### Focus Properties

**v1:**
```csharp
view.CanFocus = true; // Default was true
```

**v2:**
```csharp
view.CanFocus = true; // Default is FALSE - must opt-in
```

**Important:** In v2, <xref:Terminal.Gui.ViewBase.View.CanFocus> defaults to `false`. Views that want focus must explicitly set it.

### Focus Changes

**v1:**
```csharp
// HasFocus was read-only
bool hasFocus = view.HasFocus;
```

**v2:**
```csharp
// HasFocus can be set
view.HasFocus = true; // Equivalent to SetFocus()
view.HasFocus = false; // Equivalent to SuperView.AdvanceFocus()
```

### TabStop Behavior

**v1:**
```csharp
view.TabStop = true; // Boolean
```

**v2:**
```csharp
view.TabStop = TabBehavior.TabStop; // Enum with more options

// Options:
// - NoStop: Focusable but not via Tab
// - TabStop: Normal tab navigation
// - TabGroup: Advance via F6
```

### Navigation Events

**v1:**
```csharp
view.Enter += (s, e) => { }; // Gained focus
view.Leave += (s, e) => { }; // Lost focus
```

**v2:**
```csharp
view.HasFocusChanging += (s, e) => 
{ 
    // Before focus changes (cancellable)
    if (preventFocusChange)
        e.Cancel = true;
};

view.HasFocusChanged += (s, e) => 
{ 
    // After focus changed
    if (e.Value)
        Console.WriteLine("Gained focus");
    else
        Console.WriteLine("Lost focus");
};
```

See [Navigation Deep Dive](navigation.md) for complete details.

---

## Scrolling Changes

### ScrollView Removed

**v1:**
```csharp
var scrollView = new ScrollView
{
    ContentSize = new Size(100, 100),
    ShowHorizontalScrollIndicator = true,
    ShowVerticalScrollIndicator = true
};
```

**v2:**
```csharp
// Built-in scrolling on every View
var view = new View();
view.SetContentSize(new Size(100, 100));

// Built-in scrollbars with automatic visibility
view.ViewportSettings |= ViewportSettingsFlags.HasVerticalScrollBar;
view.ViewportSettings |= ViewportSettingsFlags.HasHorizontalScrollBar;
```

### Scrolling API

**v2:**
```csharp
// Set content larger than viewport
view.SetContentSize(new Size(100, 100));

// Scroll by changing Viewport location
view.Viewport = view.Viewport with { Location = new Point(10, 10) };

// Or use helper methods
view.ScrollVertical(5);
view.ScrollHorizontal(3);
```

See [Scrolling Deep Dive](scrolling.md) for complete details.

---

## Event Pattern Changes

v2 standardizes all events to use `object sender, EventArgs args` pattern.

### Button.Clicked → Button.Accepting

**v1:**
```csharp
button.Clicked += () => { /* do something */ };
```

**v2:**
```csharp
button.Accepting += (s, e) => { /* do something */ };
```

### Event Signatures

**v1:**
```csharp
// Various patterns
event Action SomeEvent;
event Action<T> OtherEvent;
event Action<T1, T2> ThirdEvent;
```

**v2:**
```csharp
// Consistent pattern
event EventHandler<EventArgs>? SomeEvent;
event EventHandler<T>? OtherEvent;
```

---

## Cursor Management

Terminal.Gui v2 introduces a completely redesigned cursor system that separates the **Terminal Cursor** (visible indicator) from the **Draw Cursor** (internal rendering position).

### Key Changes

**v1 Pattern (PositionCursor Override):**
```csharp
// v1 - Override PositionCursor method
public override void PositionCursor ()
{
    if (!HasFocus) return;
    
    var col = _cursorPosition - _scrollOffset;
    if (col < 0 || col >= Frame.Width) return;
    
    Move (col, 0);  // This was confusing - affected both cursors
}
```

**v2 Pattern (Cursor Property):**
```csharp
// v2 - Set Cursor property in OnDrawContent
protected override void OnDrawContent (Rectangle viewport)
{
    // ... drawing code ...
    
    if (HasFocus)
    {
        int col = _cursorPosition - _scrollOffset;
        
        if (col >= 0 && col < viewport.Width)
        {
            // Convert to screen coordinates and set cursor
            Point screenPos = ViewportToScreen (new Point (col, 0));
            Cursor = new Cursor 
            { 
                Position = screenPos,
                Style = CursorStyle.BlinkingBar 
            };
        }
        else
        {
            // Hide cursor when outside viewport
            Cursor = new Cursor { Position = null };
        }
    }
}
```

### Cursor Class

v2 uses an immutable `Cursor` record class:

```csharp
// Immutable cursor with screen coordinates
Cursor = new Cursor
{
    Position = screenPos,        // Point? - null = hidden
    Style = CursorStyle.BlinkingBar  // ANSI-based styles
};

// Update position keeping same style
Cursor = Cursor with { Position = newScreenPos };

// Hide cursor
Cursor = new Cursor { Position = null };
```

### CursorStyle Enum

v2 uses ANSI/VT terminal standards instead of Windows-based styles:

```csharp
// v2 - ANSI DECSCUSR-based styles
public enum CursorStyle
{
    Default = 0,           // Usually BlinkingBlock
    BlinkingBlock = 1,     // █ (blinking)
    SteadyBlock = 2,       // █ (steady)
    BlinkingUnderline = 3, // _ (blinking)
    SteadyUnderline = 4,   // _ (steady)
    BlinkingBar = 5,       // | (blinking) - common for text editors
    SteadyBar = 6,         // | (steady)
    Hidden = -1            // No visible cursor
}
```

### Coordinate Systems

**CRITICAL**: `Cursor.Position` must ALWAYS be in screen-absolute coordinates.

```csharp
// v2 - Always convert to screen coordinates
Point contentPos = new Point (col, row);        // Your internal coordinates
Point screenPos = ContentToScreen (contentPos); // Convert to screen
Cursor = new Cursor { Position = screenPos, Style = CursorStyle.BlinkingBar };

// Or from viewport coordinates
Point viewportPos = new Point (col, row);
Point screenPos = ViewportToScreen (viewportPos);
Cursor = new Cursor { Position = screenPos, Style = CursorStyle.BlinkingBar };
```

### Efficient Cursor Updates

When cursor moves without content changes, use `SetCursorNeedsUpdate()`:

```csharp
// v2 - Signal cursor update without full redraw
private void MoveCursorRight ()
{
    _cursorPosition++;
    
    int viewportCol = _cursorPosition - _scrollOffset;
    if (viewportCol >= 0 && viewportCol < Viewport.Width)
    {
        Point screenPos = ViewportToScreen (new Point (viewportCol, 0));
        Cursor = Cursor with { Position = screenPos };
        SetCursorNeedsUpdate (); // Efficient - no redraw
    }
}
```

### Critical: Move() vs Cursor

**v1 Confusion:**
- `Move()` affected both Draw Cursor and positioning for Terminal Cursor

**v2 Clarity:**
- `Move()` ONLY affects **Draw Cursor** (where next character renders)
- `Cursor` property ONLY affects **Terminal Cursor** (visible indicator)

```csharp
// ❌ WRONG in v2 - Don't use Move() for cursor positioning
Move (cursorCol, cursorRow);  // This is for drawing, not Terminal Cursor

// ✅ CORRECT in v2 - Use Cursor property
Point screenPos = ViewportToScreen (new Point (cursorCol, cursorRow));
Cursor = new Cursor { Position = screenPos, Style = CursorStyle.BlinkingBar };
```

### Migration Checklist

When migrating cursor code from v1 to v2:

1. ✅ Remove `PositionCursor()` override
2. ✅ Move cursor logic to `OnDrawContent()` or event handlers
3. ✅ Convert coordinates to screen space using `ViewportToScreen()` or `ContentToScreen()`
4. ✅ Set `Cursor` property instead of calling `Move()`
5. ✅ Use `CursorStyle` enum for cursor appearance
6. ✅ Use `SetCursorNeedsUpdate()` for position-only changes
7. ✅ Set `Cursor.Position = null` to hide cursor

See [Cursor Management](cursor.md) for comprehensive documentation and examples.

---

## View-Specific Changes

### ListView

**v1:**
```csharp
var listView = new ListView(items);
listView.SelectedChanged += () => { };
```

**v2:**
```csharp
var listView = new ListView();
listView.SetSource(items);
listView.SelectedItemChanged += (s, e) => { };
```

### TextView

**v1:**
```csharp
var textView = new TextView
{
    Text = "Initial text"
};
```

**v2:**
```csharp
var editor = new Editor
{
    Text = "Initial text"
};
// Similar API, but better performance
```

### Button

**v1:**
```csharp
var button = new Button("Click Me");
button.Clicked += () => { };
```

**v2:**
```csharp
var button = new Button { Text = "Click Me" };
button.Accepting += (s, e) => { };
```

---

## Disposal and Resource Management

v2 implements `IDisposable` throughout the API.

### Disposal Rules

1. **Whoever creates it, owns it** - If you create a View, you must dispose it
2. **Framework-created instances** - The framework disposes what it creates
3. **Always use `using` statements** - For <xref:Terminal.Gui.App.IApplication> instances

```csharp
// ✅ Correct disposal pattern
using (var app = Application.Create().Init())
{
    var window = new Window();
    try
    {
        app.Run(window);
    }
    finally
    {
        window.Dispose();
    }
}

// ✅ Framework disposes what it creates
using (var app = Application.Create().Init())
{
    app.Run<MyDialog>(); // Framework creates and disposes MyDialog
}
```

---

## API Terminology Changes

| v1 | v2 | Notes |
|----|-----|-------|
| `Application.Top` | `app.TopRunnable` | Property on IApplication instance |
| `Application.MainLoop` | `app.MainLoop` | Property on IApplication instance |
| `Application.Driver` | `app.Driver` | Property on IApplication instance |
| `Bounds` | `Viewport` | Viewport can have non-zero location for scrolling |
| `Rect` | `Rectangle` | Standard .NET type |
| `MouseClick` event | <xref:Terminal.Gui.ViewBase.View.Activating> event | Via Command.Activate |
| `Enter`/`Leave` events | `HasFocusChanged` event | Unified focus event |
| `Button.Clicked` | `Button.Accepting` | Consistent with Command pattern |
| `AutoSize` | `Dim.Auto()` | Part of layout system |
| `LayoutStyle` | Removed | All layout is now declarative |

---

## Summary

v2 represents a significant evolution of Terminal.Gui with:

- **Better Architecture** - Instance-based, testable, maintainable
- **Modern APIs** - Standard .NET patterns throughout
- **Enhanced Capabilities** - TrueColor, built-in scrolling, better input
- **Improved Developer Experience** - Fluent API, better documentation

While migration requires some effort, the result is a more robust, performant, and maintainable codebase. Start by updating your application lifecycle to use `Application.Create()`, then address layout and input changes incrementally.

For more details, see:
- [Application Deep Dive](application.md)
- [Configuration Deep Dive](config.md)
- [Migrating ConfigurationManager to TuiConfigurationBuilder](migrate-cm-to-mec.md)
- [2.5.0 Breaking Changes](breaking-changes-2.5.0.md)
- [Keyboard Deep Dive](keyboard.md)
- [Mouse Deep Dive](mouse.md)
- [Layout Deep Dive](layout.md)
- [Navigation Deep Dive](navigation.md)
- [What's New in v2](newinv2.md)
