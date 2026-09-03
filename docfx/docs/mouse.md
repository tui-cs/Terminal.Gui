# Mouse Deep Dive

> **Quick Start:** Jump to [Quick Reference](#quick-reference) for a condensed overview of the mouse pipeline and common patterns.

## Table of Contents

- [Quick Reference](#quick-reference)
- [Tenets for Mouse Handling](#tenets-for-mouse-handling)
- [Mouse Behavior - End User's Perspective](#mouse-behavior---end-users-perspective)
- [Mouse APIs](#mouse-apis)
- [Mouse Bindings](#mouse-bindings)
- [Mouse Events](#mouse-events)
- [Mouse State and Mouse Grab](#mouse-state-and-mouse-grab)
- [Mouse Coordinate Systems](#mouse-coordinate-systems)
- [Complete Mouse Event Pipeline](#complete-mouse-event-pipeline)
- [Best Practices](#best-practices)
- [Testing Mouse Input](#testing-mouse-input)
- [Limitations and Considerations](#limitations-and-considerations)

## Quick Reference

### The Pipeline (TL;DR)

```
ANSI Input ? AnsiMouseParser ? MouseInterpreter ? ApplicationMouse ? View ? Commands
   (1-based)     (0-based screen)   (click synthesis)   (routing)  (viewport)  (Activate/Accept)
```

### Pipeline Stages

| Stage | Input | Output | Key Transformation |
|-------|-------|--------|-------------------|
| **ANSI** | User clicks | `ESC[<0;10;5M` | Hardware ? ANSI escape sequence |
| **Parser** | ANSI string | `Mouse{Pressed, Screen(9,4)}` | 1-based ? 0-based, Button code ? MouseFlags |
| **Interpreter** | Press/Release | `Mouse{Clicked, Screen(9,4)}` | Press+Release ? Clicked, Timing ? DoubleClicked |
| **ApplicationMouse** | Screen coords | `Mouse{Clicked, Viewport(2,1)}` | Screen ? Viewport, Find view, Handle grab |
| **View** | Viewport coords | Command invocation | Clicked ? Command.Activate, MouseState updates |
| **Commands** | Command | Event | Activate ? Activating, Accept ? Accepting |

### Coordinate Systems

| Level | Origin | Example |
|-------|--------|---------|
| **ANSI** | 1-based, (1,1) = top-left | `ESC[<0;10;5M` |
| **Screen** | 0-based, (0,0) = top-left of terminal | `ScreenPosition = (9,4)` |
| **Viewport** | 0-based, relative to view's content area | `Position = (2,1)` |

### Common Patterns

**Handle mouse clicks:**
```csharp
view.Activating += (s, e) =>
{
    if (e.Context?.Binding is MouseBinding { MouseEvent: { } mouse })
    {
        Point position = mouse.Position;  // Viewport-relative
        HandleClick(position);
        e.Handled = true;
    }
};
```

**Enable visual feedback and auto-grab:**
```csharp
view.MouseHighlightStates = MouseState.In | MouseState.Pressed;
```

**Continuous button press (scrollbar arrows, spin buttons):**
```csharp
view.MouseHoldRepeat = MouseFlags.LeftButtonReleased;
view.Activating += (s, e) => { DoRepeatAction(); e.Handled = true; };
```

## Tenets for Mouse Handling

Tenets higher in the list have precedence over tenets lower in the list.

* **Keyboard Required; Mouse Optional** - Terminal users expect full functionality without a mouse. We strive to ensure anything that can be done with the keyboard is also possible with the mouse, and avoid mouse-only features.

* **Be Consistent With the User's Platform** - Users choose their platform and Terminal.Gui apps should respond to mouse input consistent with platform conventions. For example, on Windows: right-click shows context menus, double-click activates items, mouse wheel scrolls content.

## Mouse Behavior - End User's Perspective

### Button Behavior

| Scenario | Visual State | `Command.Accept` Count | Notes |
|----------|-------------|----------------------|-------|
| **Single click** (press + release inside) | Pressed ? Released | **1** on release | Standard click behavior |
| **Hold** (MouseHoldRepeat = false) | Pressed ? stays ? Released | **1** on release | Normal push-button |
| **Hold** (MouseHoldRepeat = true) | Same visual | **~10+** (timer ~500ms initial, ~50ms intervals) + **1 final** on release | Scrollbar arrow behavior |
| **Drag outside ? release outside** | Pressed ? Released | **0** (canceled) | Standard click cancellation |
| **Double-click** (MouseHoldRepeat = false) | Press?Release?Press?Release | **2** (one per release) | Two separate accepts |
| **Double-click** (MouseHoldRepeat = true) | Same cycle | **2** (one per release) | Each press/release fires Accept |

**Key Point for MouseHoldRepeat:** When enabled, the view responds to **Press and Release events only**. Each press starts the timer (which fires Accept repeatedly), and each release fires one final Accept (if released inside).

### ListView Behavior  

| Scenario | Selection State | `Command.Activate` Count | `Command.Accept` Count | Notes |
|----------|----------------|------------------------|---------------------|-------|
| **Single click** | Item selected on click | **1** | **0** | Selection happens immediately |
| **Double-click** | Selected on first click | **1** (first click) | **1** (second click) | Standard file browser behavior |
| **Enter key** | No change (already selected) | **0** | **1** | Keyboard equivalent of double-click |

## Mouse APIs

Terminal.Gui provides these APIs for handling mouse input:

* **Mouse Bindings** - Declarative approach using <xref:Terminal.Gui.Input.MouseBindings> to map mouse events to commands. **Recommended for most scenarios.**

* **Mouse Events** - Direct event handling via `MouseEvent` for complex scenarios like drag-and-drop.

* **Mouse State** - <xref:Terminal.Gui.ViewBase.MouseState> property provides current interaction state for visual feedback.

* **Mouse** class - Platform-independent abstraction (<xref:Terminal.Gui.Input.Mouse>) for mouse events.

## Mouse Bindings

Mouse Bindings is the **recommended** way to handle mouse input. Views call `AddCommand` to declare command support, then use <xref:Terminal.Gui.Input.MouseBindings> to map mouse events to commands:

```csharp
public class MyView : View
{
    public MyView()
    {
        AddCommand (Command.ScrollUp, () => ScrollVertical (-1));
        MouseBindings.Add (MouseFlags.WheelUp, Command.ScrollUp);
        
        AddCommand (Command.ScrollDown, () => ScrollVertical (1));
        MouseBindings.Add (MouseFlags.WheelDown, Command.ScrollDown);
        
        // Mouse clicks invoke Command.Activate by default
        AddCommand (Command.Activate, () => {
            SelectItem();
            return true;
        });
    }
}
```

### Default Mouse Bindings

All views have these default bindings:

```csharp
MouseBindings.Add (MouseFlags.LeftButtonPressed, Command.Activate);
MouseBindings.Add (MouseFlags.LeftButtonPressed | MouseFlags.Ctrl, Command.Context);
```

When a mouse event occurs matching a binding, the bound command is invoked, which raises the corresponding event (e.g., <xref:Terminal.Gui.Input.Command.Activate> ? <xref:Terminal.Gui.ViewBase.View.Activating> event).

### Common Binding Patterns

* **Click Events**: `MouseFlags.LeftButtonPressed` for selection/interaction
* **Context Menu**: `MouseFlags.RightButtonPressed` or `LeftButtonPressed | Ctrl`  
* **Scroll Events**: `MouseFlags.WheelUp` / `WheelDown`
* **Drag Operations**: `MouseFlags.LeftButtonPressed` + mouse move tracking

## Mouse Events

### Mouse Event Processing Flow

Mouse events are processed using the [Cancellable Work Pattern](cancellable-work-pattern.md):

1. **Driver Level**: Captures platform-specific events ? converts to `Mouse`
2. **Application Level**: `IMouse.RaiseMouseEvent` determines target view and routes event
3. **View Level**: `View.NewMouseEvent()` processes:
   - Pre-condition validation (enabled, visible, wants event type)
   - Low-level `MouseEvent` (raises `OnMouseEvent()` and `MouseEvent` event)
   - Mouse grab handling (if `MouseHighlightStates` or `MouseHoldRepeat` set)
   - Command invocation via <xref:Terminal.Gui.Input.MouseBindings>

### Handling Mouse Events Directly

For scenarios requiring direct event handling (drag-and-drop, custom gestures):

```csharp
public class CustomView : View
{
    public CustomView()
    {
        MouseEvent += OnMouseEventHandler;
    }
    
    private void OnMouseEventHandler(object sender, Mouse e)
    {
        if (e.Flags.HasFlag(MouseFlags.LeftButtonPressed))
        {
            // Handle drag start
            e.Handled = true;
        }
    }
    
    // Alternative: Override the virtual method
    protected override bool OnMouseEvent(Mouse mouse)
    {
        if (mouse.Flags.HasFlag(MouseFlags.LeftButtonPressed))
        {
            return true; // Handled
        }
        return base.OnMouseEvent(mouse);
    }
}
```

### Handling Mouse Clicks

**Recommended pattern** - Use <xref:Terminal.Gui.ViewBase.View.Activating> event with command context:

```csharp
public class ClickableView : View
{
    public ClickableView()
    {
            Activating += OnActivating;
        }

        private void OnActivating(object sender, CommandEventArgs e)
        {
            if (e.Context?.Binding is MouseBinding { MouseEvent: { } mouse })
            {
                Point clickPosition = mouse.Position; // Viewport-relative

                if (mouse.Flags.HasFlag(MouseFlags.LeftButtonPressed))
                {
                    HandleLeftClick(clickPosition);
                }
                else if (mouse.Flags.HasFlag(MouseFlags.RightButtonPressed))
                {
                    ShowContextMenu(clickPosition);
                }

                e.Handled = true;
            }
        }
}
```

For custom button handling:

```csharp
// Clear defaults and add custom bindings
MouseBindings.Clear();
MouseBindings.Add(MouseFlags.LeftButtonPressed, Command.Activate);
MouseBindings.Add(MouseFlags.RightButtonPressed, Command.Context);

AddCommand(Command.Context, HandleContextMenu);
```

## Mouse State and Mouse Grab

### Mouse State

The <xref:Terminal.Gui.ViewBase.MouseState> property tracks the current mouse interaction state:

* **None** - No mouse interaction
* **In** - Mouse over the view's viewport
* **Pressed** - Mouse button pressed while over view
* **PressedOutside** - Button pressed inside but mouse moved outside

Configure which states trigger highlighting:

```csharp
view.MouseHighlightStates = MouseState.In | MouseState.Pressed;

view.MouseStateChanged += (sender, e) => 
{
    switch (e.Value)
    {
        case MouseState.In:
            // Hover appearance
            break;
        case MouseState.Pressed:
            // Pressed appearance
            break;
    }
};
```

### Mouse Grab

Views with `MouseHighlightStates` or `MouseHoldRepeat` enabled **automatically grab the mouse** when a button is pressed. For manual grab control, use the `IMouseGrabHandler` interface via <xref:Terminal.Gui.ViewBase.View.App>`.Mouse`.

**Grab Lifecycle:**
1. **Press inside** ? Call `GrabMouse(view)` (auto or manual), fires `GrabbingMouse` (cancellable) then `GrabbedMouse`
2. **During grab** ? ALL mouse events routed exclusively to grabbed view with viewport-relative coordinates
3. **Move outside** ? `MouseState |= PressedOutside` (unless `MouseHoldRepeat`)
4. **Release/Click** ? Call `UngrabMouse()`, fires `UnGrabbingMouse` (cancellable) then `UnGrabbedMouse`

**Grabbed View Receives:**
- ALL mouse events (even outside viewport)
- Coordinates converted to viewport-relative (`mouse.Position`)
- `mouse.View` set to grabbed view

**Auto-ungrab occurs when:**
- Button released (via clicked event)
- View disposed (uses `WeakReference<View>` internally)
- Application ends

**Manual Grab Example (for custom drag operations):**
```csharp
protected override bool OnMouseEvent(Mouse mouse)
{
    if (mouse.Flags.HasFlag(MouseFlags.Button1Pressed))
    {
        App?.Mouse.GrabMouse(this);
        _isDragging = true;
        return true;
    }

    if (_isDragging && mouse.Flags.HasFlag(MouseFlags.Button1Released))
    {
        App?.Mouse.UngrabMouse();
        _isDragging = false;
        return true;
    }

    if (_isDragging)
    {
        // mouse.Position is viewport-relative during grab
        UpdateDragPosition(mouse.Position);
        return true;
    }

    return false;
}
```

**Preventing Grab Theft (for complex drag operations):**
```csharp
// Subscribe to prevent other views from stealing the grab during drag
App.Mouse.GrabbingMouse += (sender, e) =>
{
    if (_isDragging && !ReferenceEquals(e.View, this))
    {
        e.Cancel = true; // Prevent other views from grabbing
    }
};
```

### Continuous Button Press

When `MouseHoldRepeat` is set, the <xref:Terminal.Gui.ViewBase.View> receives repeated events while the button is held:

```csharp
view.MouseHoldRepeat = MouseFlags.LeftButtonReleased;

view.Activating += (s, e) =>
{
    // Called repeatedly while held (~500ms initial, ~50ms intervals)
    DoRepeatAction();
    e.Handled = true;
};
```

## Mouse Coordinate Systems

Mouse coordinates in Terminal.Gui use multiple coordinate systems:

* **Screen Coordinates** - Relative to terminal (0,0 = top-left) - `Mouse.ScreenPosition`
* **Viewport Coordinates** - Relative to view's content area (0,0 = top-left of viewport) - `Mouse.Position`

When handling mouse events in views, use `Position` for viewport-relative coordinates:

```csharp
view.MouseEvent += (s, e) =>
{
    // e.Position is viewport-relative
    if (e.Position.X < 10 && e.Position.Y < 5)
    {
        // Click in top-left corner of viewport
    }
};
```

### Coordinate Conversion Methods

Views provide methods to convert between coordinate systems:

```csharp
// Screen ? Viewport
Point viewportPos = view.ScreenToViewport(screenPos);
Point screenPos = view.ViewportToScreen(viewportPos);

// Screen ? Content  
Point contentPos = view.ScreenToContent(screenPos);
Point screenPos = view.ContentToScreen(contentPos);

// Screen ? Frame
Point framePos = view.ScreenToFrame(screenPos);
Rectangle screenRect = view.FrameToScreen();
```

## Complete Mouse Event Pipeline

This section documents the complete flow from raw terminal input to View command execution.

### Stage 1: Terminal Input (ANSI Escape Sequences)

**Input Format:** SGR Extended Mouse Mode (`ESC[<button;x;yM/m`)

**Example - Single click at column 10, row 5:**
```
Press:   ESC[<0;10;5M    (button=0, x=10, y=5, 'M'=press)
Release: ESC[<0;10;5m    (button=0, x=10, y=5, 'm'=release)
```

**Key Points:**
- Coordinates are **1-based** in ANSI (top-left = 1,1)
- `M` terminator = press, `m` terminator = release
- Button codes: 0=left, 1=middle, 2=right, 64/65=wheel
- Modifiers in button code (8=Alt, 16=Ctrl, 4=Shift)

### Stage 2: ANSI Parsing (AnsiMouseParser)

**Location:** `Terminal.Gui/Drivers/AnsiHandling/AnsiMouseParser.cs`

**Responsibilities:**
1. Parse ANSI sequence: `\u001b\[<(\d+);(\d+);(\d+)(M|m)`
2. Extract button, x, y, terminator
3. Convert to **0-based** coordinates (subtract 1)
4. Map button code + terminator to <xref:Terminal.Gui.Input.MouseFlags>
5. Extract modifiers
6. Create `Mouse` instance

**Output:** `Mouse { Timestamp=now, ScreenPosition=(9,4), Flags=LeftButtonPressed }`

### Stage 3: Click Synthesis (MouseInterpreter)

**Location:** `Terminal.Gui/Drivers/MouseInterpreter.cs`

**Responsibilities:**
1. Track press/release pairs ? generate click events
2. Detect multi-clicks (double/triple) based on:
   - Time between clicks (500ms threshold)
   - Position proximity  
   - Same button
3. Emit synthetic events:
   - Press+Release ? `LeftButtonClicked`
   - Second click within threshold ? `LeftButtonDoubleClicked`
   - Third click ? `LeftButtonTripleClicked`

**Key Behavior:**
- Press and Release events pass through immediately
- Click events synthesized immediately after release
- Multi-click detection tracks timing/position/button
- Modifier keys (Shift, Ctrl, Alt) are preserved in synthetic click events

**Output:** Stream of `Mouse` events including synthesized clicks

### Stage 4: Application Routing (ApplicationMouse)

**Location:** `Terminal.Gui/App/Mouse/ApplicationMouse.cs`

**Entry:** `IMouse.RaiseMouseEvent(Mouse mouse)`

**Processing:**

#### 4.1: Find Target View
```csharp
List<View?> viewsUnderMouse = App.TopRunnableView.GetViewsUnderLocation(
    mouse.ScreenPosition, 
    ViewportSettingsFlags.TransparentMouse
);
View? deepestView = viewsUnderMouse?.LastOrDefault();
```

#### 4.2: Popover Dismissal
```csharp
if (mouse.IsPressed && 
    App.Popover?.GetActivePopover() is {} popover &&
    !View.IsInHierarchy(popover, deepestView, true))
{
    ApplicationPopover.HideWithQuitCommand(popover);
    RaiseMouseEvent(mouse); // Recurse to handle event below popover
}
```

**Re-show Guard:** When a popover is dismissed, `ApplicationMouse` records the dismissed popover in `DismissedByMousePress` and wraps the recursive `RaiseMouseEvent` call with an `_isDismissRecursing` flag. `ApplicationPopover.Show` checks this guard and silently returns if the caller is trying to re-show the same popover that was just dismissed. This prevents views beneath the popover (e.g., a `DropDownList` toggle button or a `MenuBarItem`) from re-opening the popover during the recursed press event or during the subsequent release/click events in the same mouse interaction cycle (press → release → click). The guard is cleared when the next fresh press event arrives or when the click cycle completes.

#### 4.3: Mouse Grab Handling
```csharp
// If a view has grabbed the mouse, route events exclusively to that view
// HandleMouseGrab converts coordinates to the grabbed view's viewport
// and delivers the event directly, returning true to stop further processing
if (HandleMouseGrab(deepestViewUnderMouse, mouse))
    return; // Grabbed view received the event
```

#### 4.4: Convert to View Coordinates
```csharp
Point viewportLocation = deepestView.ScreenToViewport(mouse.ScreenPosition);
Mouse viewMouseEvent = new() {
    Position = viewportLocation,      // Viewport-relative!
    Flags = mouse.Flags,
    ScreenPosition = mouse.ScreenPosition,
    View = deepestView
};
```

#### 4.5: Raise MouseEnter/Leave
```csharp
RaiseMouseEnterLeaveEvents(mouse.ScreenPosition, viewsUnderMouse);
```

#### 4.6: Send to View
```csharp
deepestView.NewMouseEvent(viewMouseEvent);
// If not handled, propagate to SuperView
```

### Stage 5: View Processing (View.NewMouseEvent)

**Location:** `Terminal.Gui/ViewBase/View.Mouse.cs`

**Entry:** `View.NewMouseEvent(Mouse mouse)`

#### 5.1: Pre-conditions
```csharp
if (!Enabled) return false;
if (!CanBeVisible(this)) return false;
if (!MousePositionTracking && mouse.Flags == MouseFlags.PositionReport) 
    return false;
```

#### 5.2: Low-Level MouseEvent
```csharp
if (RaiseMouseEvent(mouse) || mouse.Handled)
    return true;  // View handled via OnMouseEvent or subscriber
```

#### 5.3: Mouse Grab Handling

**Conditions:** `MouseHighlightStates != None` OR `MouseHoldRepeat.HasValue`

**On Pressed:**
```csharp
if (!App.Mouse.IsGrabbed(this))
{
    // GrabbingMouse event fires first (can be cancelled)
    // If not cancelled, GrabbedMouse event fires
    App.Mouse.GrabMouse(this);
}
if (!HasFocus && CanFocus) SetFocus();

if (mouse.Position in Viewport)
    MouseState |= MouseState.Pressed;
else if (!MouseHoldRepeat)
    MouseState |= MouseState.PressedOutside;
```

**On Released:**
```csharp
MouseState &= ~MouseState.Pressed;
MouseState &= ~MouseState.PressedOutside;
```

**On Clicked:**
```csharp
if (App.Mouse.IsGrabbed(this))
{
    // UnGrabbingMouse event fires first (can be cancelled)
    // If not cancelled, UnGrabbedMouse event fires
    // MouseEnter/Leave events update for views under current mouse position
    App.Mouse.UngrabMouse();
}
```

#### 5.4: Invoke Commands via MouseBindings
```csharp
if (MouseBindings.TryGet(mouse.Flags, out binding))
{
    binding.MouseEventArgs = mouse;
    InvokeCommands(binding.Commands, binding);
}
```

**Default Bindings:**
- `LeftButtonPressed` ? `Command.Activate`
- `LeftButtonPressed | Ctrl` ? `Command.Context`

#### 5.5: Command Execution

See [Command Deep Dive](command.md) for details.

**Example - LeftButtonPressed ? Command.Activate:**
```csharp
InvokeCommand(Command.Activate, context):
    OnActivating(args) || args.Cancel  // Subclass override
    Activating?.Invoke(this, args)     // Event subscribers
    if (!args.Cancel && CanFocus) SetFocus();
```

### Driver Architecture

**Platform-Specific Input:**
- **Windows**: `WindowsInputProcessor` - `ReadConsoleInput()` ? direct `Mouse` conversion
- **Unix/ANSI**: ANSI escape sequence parsing pipeline

**Input Processing:**
```
Platform API ? InputProcessorImpl ? AnsiResponseParser ? MouseInterpreter ? Application
```

This ensures consistent mouse behavior across platforms while maintaining platform-specific optimizations.

## Mouse Event Forwarding via Command Bubbling

A common need is forwarding mouse-wheel events from a child view to an ancestor (e.g., a gutter subview inside a scrollable editor's Padding that should scroll the editor). Terminal.Gui supports this idiomatically via the **CommandNotBound bubbling** mechanism.

### The Pattern

1. **Child view**: Add a `MouseBinding` for the wheel event **without** calling `AddCommand` for that command.
2. **Parent view**: Include the scroll commands in <xref:Terminal.Gui.ViewBase.View.CommandsToBubbleUp>.
3. **Result**: When the child receives the wheel event, the mouse binding fires the command. Because the child has no handler (`AddCommand` was not called), `DefaultCommandNotBoundHandler` runs → <xref:Terminal.Gui.ViewBase.View.TryBubbleUp*> finds the ancestor with the command in `CommandsToBubbleUp` → invokes it on the ancestor.

### Example: Gutter Forwards Wheel to Editor

```csharp
// Parent (Editor) handles scroll commands and opts into bubbling
public class Editor : View
{
    public Editor ()
    {
        AddCommand (Command.ScrollUp, () => ScrollVertical (-1));
        AddCommand (Command.ScrollDown, () => ScrollVertical (1));

        // Allow scroll commands from SubViews/adornment SubViews to bubble here
        CommandsToBubbleUp = [Command.ScrollUp, Command.ScrollDown];
    }
}

// Child (Gutter) binds wheel events but does NOT add command handlers
public class Gutter : View
{
    public Gutter ()
    {
        // Bind mouse wheel to scroll commands — no AddCommand needed.
        // The unhandled command will bubble up to the nearest ancestor
        // whose CommandsToBubbleUp includes it.
        MouseBindings.Add (MouseFlags.WheeledUp, Command.ScrollUp);
        MouseBindings.Add (MouseFlags.WheeledDown, Command.ScrollDown);
    }
}
```

### How It Works

```
Mouse wheel on Gutter
  → MouseBindings maps WheeledUp → Command.ScrollUp
  → Gutter.InvokeCommand(ScrollUp)
  → No handler found (AddCommand was never called)
  → DefaultCommandNotBoundHandler runs
  → RaiseCommandNotBound → TryBubbleUp
  → Finds ancestor (Editor) with Command.ScrollUp in CommandsToBubbleUp
  → Editor.InvokeCommand(ScrollUp) → scrolls
```

### AdornmentView Special Case

For views hosted inside an adornment (Padding, Border, or Margin), the bubble target is the **adornment's Parent** (the owning View), not the `SuperView`. This means a view added to `editor.Padding` will bubble commands directly to `editor`, skipping the `PaddingView` intermediary.

```csharp
// Gutter added to Editor's Padding — bubbles to Editor automatically
editor.Padding.Add (gutter);
```

This is handled internally by <xref:Terminal.Gui.ViewBase.View.TryBubbleUp*> and <xref:Terminal.Gui.ViewBase.View.CommandWillBubbleToAncestor*>.

### When to Use This Pattern

* A SubView should forward wheel/scroll events to its container without handling them itself.
* A view in Padding/Border needs to delegate commands to the owning View.
* You want clean separation: the child declares *what* user gesture maps to *which* command, and the ancestor decides *how* to handle it.

> [!TIP]
> Adding a `MouseBinding` without a corresponding `AddCommand` handler is **intentional and idiomatic**. It signals that the command should bubble to an ancestor rather than being handled locally. See <xref:Terminal.Gui.ViewBase.View.CommandNotBound>.

## Best Practices

* **Use Mouse Bindings and Commands** for simple interactions - integrates with keyboard bindings
* **Use <xref:Terminal.Gui.ViewBase.View.Activating> event** to handle clicks - provides mouse position via CommandContext
* **Access mouse details via CommandContext:**
  ```csharp
  if (e.Context?.Binding is MouseBinding { MouseEvent: { } mouse })
  {
      Point pos = mouse.Position;  // Viewport-relative
      MouseFlags flags = mouse.Flags;
  }
  ```
* **Handle MouseEvent directly** only for complex scenarios (drag-and-drop, custom gestures)
* **Use `MouseHighlightStates`** for automatic grab and visual feedback
* **Use `MouseHoldRepeat`** for repeating actions (scroll buttons, spinners)
* **Respect platform conventions** - right-click for menus, double-click for default actions
* **Provide keyboard alternatives** - essential for accessibility
* **Test with different terminals** - mouse support varies

## Testing Mouse Input

> **For comprehensive documentation,** see **[Input Injection](input-injection.md)**.

Terminal.Gui provides sophisticated input injection for testing without hardware:

### Quick Test Example (Using Helper Methods)

**Recommended approach** - Use helper methods for cleaner test code:

```csharp
using IApplication app = Application.Create();
app.Init(DriverRegistry.Names.ANSI);

// Inject a left click - simple and clear
app.InjectSequence(InputInjectionExtensions.LeftButtonClick(new Point(10, 5)));

// Inject a right click
app.InjectSequence(InputInjectionExtensions.RightButtonClick(new Point(10, 5)));

// Inject a double-click
app.InjectSequence(InputInjectionExtensions.LeftButtonDoubleClick(new Point(10, 5)));
```

**Alternative approach** - Manual event creation for advanced scenarios:

```csharp
VirtualTimeProvider time = new();
using IApplication app = Application.Create(time);
app.Init(DriverRegistry.Names.ANSI);

// Inject click manually
app.InjectMouse(new() { 
    ScreenPosition = new(10, 5), 
    Flags = MouseFlags.LeftButtonPressed 
});
app.InjectMouse(new() { 
    ScreenPosition = new(10, 5), 
    Flags = MouseFlags.LeftButtonReleased 
});
```

### Testing Double-Click with Virtual Time

**Using helper method** (recommended):

```csharp
using IApplication app = Application.Create();
app.Init(DriverRegistry.Names.ANSI);

// One line for a complete double-click
app.InjectSequence(InputInjectionExtensions.LeftButtonDoubleClick(new Point(10, 5)));
```

**Manual approach** (for custom timing control):

```csharp
VirtualTimeProvider time = new();
time.SetTime(new DateTime(2025, 1, 1, 12, 0, 0));
using IApplication app = Application.Create(time);
app.Init(DriverRegistry.Names.ANSI);

// First click
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

// Second click within threshold
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
// Double-click detected!
```

### Mouse Click Helper Methods

Terminal.Gui provides three helper methods in `InputInjectionExtensions` to simplify common mouse click patterns:

- **`LeftButtonClick(Point p)`** - Single left click (Press + Release)
- **`RightButtonClick(Point p)`** - Single right click (Press + Release)
- **`LeftButtonDoubleClick(Point p)`** - Double left click (two complete click sequences)

**Benefits:**
- Reduces boilerplate from 2-4 lines to 1 line
- Built-in appropriate delays for reliable click detection
- Clearer test intent
- Fewer errors from mismatched Press/Release pairs

### Key Testing Features

- **Virtual Time Control** - Deterministic multi-click timing
- **Helper Methods** - `InputInjectionExtensions.LeftButtonClick()`, etc. for simplified injection
- **Single-Call Injection** - `app.InjectMouse(mouse)` handles everything
- **No Real Delays** - Tests run instantly with virtual time
- **Two Modes** - Direct (fast) and Pipeline (full ANSI encoding)

**Learn More:** See **[Input Injection](input-injection.md)** for complete documentation.

## Limitations and Considerations

* **Terminal Support** - Not all terminals support mouse input; always provide keyboard alternatives
* **Mouse Wheel** - Support varies between platforms and terminals
* **Mouse Buttons** - Some terminals may not support all buttons or modifier keys
* **Coordinate Precision** - Limited to character cell boundaries; no sub-character precision
* **Performance** - Excessive mouse move tracking can impact performance; use Enter/Leave events when appropriate
* **Accessibility** - Mouse-only features exclude keyboard-only users

## Global Mouse Handling

Handle mouse events application-wide before views process them:

```csharp
App.Mouse.MouseEvent += (sender, e) => 
{
    // Application-wide handling
    if (e.Flags.HasFlag(MouseFlags.RightButtonClicked))
    {
        ShowGlobalContextMenu(e.Position);
        e.Handled = true;
    }
};
```

## Mouse Enter/Leave Events

Views can respond when the mouse enters or exits:

```csharp
view.MouseEnter += (sender, e) => 
{
    UpdateTooltip("Hovering");
};

view.MouseLeave += (sender, e) => 
{
    HideTooltip();
};
```

These events work with <xref:Terminal.Gui.ViewBase.MouseState> to enable hover effects and visual feedback.

## See Also

- [Command System](command.md) - Understanding how commands work with mouse events
- [Input Injection](input-injection.md) - Complete testing documentation
- [View Layout](layout.md) - Understanding coordinate systems and layout
- [Cancellable Work Pattern](cancellable-work-pattern.md) - Event processing pattern
- `IMouseGrabHandler` - API reference for mouse grab handling
- `IMouse` - API reference for the mouse interface

## Mouse Tracing

For debugging mouse event flow, use the `Trace` class from the `Terminal.Gui.Tracing` namespace:

```csharp
using Terminal.Gui.Tracing;

Trace.EnabledCategories |= TraceCategory.Mouse;
```

When enabled, mouse events are logged via `Logging.Trace` showing the flow from Driver → Application → View. Enable via:

- **Code**: `Trace.EnabledCategories |= TraceCategory.Mouse;`
- **Config**: `{ "Trace": { "EnabledCategories": "Mouse" } }`
- **UICatalog**: Logging menu → Mouse Trace

See [Logging - View Event Tracing](logging.md#view-event-tracing) for more details.
