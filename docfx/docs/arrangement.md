# View Arrangement Deep Dive

Terminal.Gui provides a powerful **Arrangement** system that enables users to interactively move and resize views using the keyboard and mouse. This system supports both **Tiled** and **Overlapped** layout modes, allowing for flexible UI organization.

See the [Layout Deep Dive](layout.md) for the broader layout system context.

## Table of Contents

- [Overview](#overview)
- [Arrangement Modes](#arrangement-modes)
- [Arrange Mode (Interactive)](#arrange-mode-interactive)
- [Tiled vs Overlapped Layouts](#tiled-vs-overlapped-layouts)
- [Movable Views](#movable-views)
- [Resizable Views](#resizable-views)
- [Creating Resizable Splitters](#creating-resizable-splitters)
- [Modal Views](#modal-views)
- [Runnable Views](#runnable-views)
- [Examples](#examples)

---

## Overview

The `Arrangement` property controls how users can arrange views within their `SuperView`. The <xref:Terminal.Gui.ViewBase.ViewArrangement> enum provides flags that can be combined to specify arrangement behavior.

### Arrangement Lexicon

[!INCLUDE [Arrangement Lexicon](~/includes/arrangement-lexicon.md)]

### ViewArrangement Flags

The [ViewArrangement](~/api/Terminal.Gui.ViewBase.ViewArrangement.yml) enum supports these flags (can be combined):

- **Fixed** (0) - View cannot be moved or resized (default)
- **Movable** (1) - View can be moved by the user
- **LeftResizable** (2) - Left edge can be resized
- **RightResizable** (4) - Right edge can be resized
- **TopResizable** (8) - Top edge can be resized
- **BottomResizable** (16) - Bottom edge can be resized
- **Resizable** (30) - All edges can be resized (combines all resize flags)
- **Overlapped** (32) - View overlaps other views (enables Z-order)

---

## Arrangement Modes

### Fixed (Default)

Views with [ViewArrangement.Fixed](~/api/Terminal.Gui.ViewBase.ViewArrangement.yml) cannot be moved or resized by the user:

```csharp
var view = new View
{
    Arrangement = ViewArrangement.Fixed // Default
};
```

### Movable

Views with [ViewArrangement.Movable](~/api/Terminal.Gui.ViewBase.ViewArrangement.yml) can be dragged with the mouse or moved with keyboard:

```csharp
var window = new Window
{
    Title = "Movable Window",
    Arrangement = ViewArrangement.Movable
};
```

**User Interaction:**
- **Mouse**: Drag the top [Border](~/api/Terminal.Gui.ViewBase.Border.yml)
- **Keyboard**: Press `Ctrl+F5` to enter Arrange Mode, use arrow keys to move

### Resizable

Views with [ViewArrangement.Resizable](~/api/Terminal.Gui.ViewBase.ViewArrangement.yml) can be resized by the user:

```csharp
var window = new Window
{
    Title = "Resizable Window",
    Arrangement = ViewArrangement.Resizable
};
```

**User Interaction:**
- **Mouse**: Drag any border edge
- **Keyboard**: Press `Ctrl+F5` to enter Arrange Mode, press `Tab` to cycle resize handles

### Movable and Resizable

Combine flags for full desktop-like experience:

```csharp
var window = new Window
{
    Title = "Movable and Resizable",
    Arrangement = ViewArrangement.Movable | ViewArrangement.Resizable
};
```

**Note:** When both <xref:Terminal.Gui.ViewBase.ViewArrangement.Movable> and <xref:Terminal.Gui.ViewBase.ViewArrangement.Resizable> are set, the top edge cannot be resized (Movable takes precedence).

### Individual Edge Resizing

For fine-grained control, use individual edge flags:

```csharp
// Only bottom edge resizable
var view = new View
{
    Arrangement = ViewArrangement.BottomResizable
};

// Left and right edges resizable
var view2 = new View
{
    Arrangement = ViewArrangement.LeftResizable | ViewArrangement.RightResizable
};
```

---

## Arrange Mode (Interactive)

**Arrange Mode** is an interactive mode for arranging views using the keyboard. It is activated by pressing the **Arrange Key** (default: `Ctrl+F5`, configurable via `Application.DefaultKeyBindings` for `Command.Arrange`).

### Entering Arrange Mode

When the user presses `Ctrl+F5`:

1. Visual indicators appear on arrangeable views
2. If [ViewArrangement.Movable](~/api/Terminal.Gui.ViewBase.ViewArrangement.yml), a move indicator (`◊`) appears in top-left corner
3. If [ViewArrangement.Resizable](~/api/Terminal.Gui.ViewBase.ViewArrangement.yml), pressing `Tab` cycles to resize indicators
4. Arrow keys move or resize the view
5. Press `Esc`, `Ctrl+F5`, or click outside to exit

### Arrange Mode Indicators

The [Border](~/api/Terminal.Gui.ViewBase.Border.yml) shows visual indicators based on arrangement options:

| Arrangement Flag | Indicator | Location |
|------------------|-----------|----------|
| Movable | `◊` (Glyphs.Move) | Top-left corner |
| Resizable | `⇲` (Glyphs.SizeBottomRight) | Bottom-right corner |
| LeftResizable | `↔` (Glyphs.SizeHorizontal) | Left edge, centered |
| RightResizable | `↔` (Glyphs.SizeHorizontal) | Right edge, centered |
| TopResizable | `↕` (Glyphs.SizeVertical) | Top edge, centered |
| BottomResizable | `↕` (Glyphs.SizeVertical) | Bottom edge, centered |

### Keyboard Controls in Arrange Mode

- **Arrow Keys** - Move or resize based on active mode
- **Tab** - Cycle between move and resize modes (if both available)
- **Shift+Tab** - Cycle backwards
- **Esc** - Exit Arrange Mode
- **Ctrl+F5** - Exit Arrange Mode

### Requirements for Arrangement

For a View to be arrangeable:

1. Must be part of a `SuperView`
2. Position and dimensions must be independent of other SubViews
3. Must have `Arrangement` flags set
4. Typically needs a <xref:Terminal.Gui.ViewBase.Border> for mouse interaction

---

## Tiled vs Overlapped Layouts

### Tiled Layout

In **Tiled** layouts, SubViews typically do not overlap. There is no Z-order; all views are at the same layer.

```csharp
var container = new View { Arrangement = ViewArrangement.Fixed };

var view1 = new View { X = 0, Y = 0, Width = 20, Height = 10 };
var view2 = new View { X = 21, Y = 0, Width = 20, Height = 10 };

container.Add(view1, view2);
// Views are side-by-side, non-overlapping
```

**Characteristics:**
- Default mode for most TUI applications
- Views use [Pos](~/api/Terminal.Gui.ViewBase.Pos.yml) and [Dim](~/api/Terminal.Gui.ViewBase.Dim.yml) for relative positioning
- No Z-order management needed
- More predictable layout behavior

### Overlapped Layout

In **Overlapped** layouts, SubViews can overlap with Z-order determining visual stacking.

Enable overlapped mode with [ViewArrangement.Overlapped](~/api/Terminal.Gui.ViewBase.ViewArrangement.yml):

```csharp
var container = new View 
{ 
    Arrangement = ViewArrangement.Overlapped 
};

var window1 = new Window 
{ 
    X = 5, Y = 3, Width = 40, Height = 15,
    Arrangement = ViewArrangement.Movable | ViewArrangement.Overlapped
};

var window2 = new Window 
{ 
    X = 15, Y = 8, Width = 40, Height = 15,
    Arrangement = ViewArrangement.Movable | ViewArrangement.Overlapped
};

container.Add(window1, window2);
// window2 will overlap window1
```

**Characteristics:**
- Z-order determined by SubViews collection order
- Later views appear above earlier views
- Tab navigation constrained to current overlapped view
- Use `Ctrl+Tab` / `Ctrl+Shift+Tab` to switch between overlapped views

---

## Movable Views

Views with [ViewArrangement.Movable](~/api/Terminal.Gui.ViewBase.ViewArrangement.yml) can be repositioned by the user.

### Enabling Movable

```csharp
var window = new Window
{
    Title = "Drag Me!",
    X = 10,
    Y = 5,
    Width = 40,
    Height = 15,
    Arrangement = ViewArrangement.Movable,
    BorderStyle = LineStyle.Single
};
```

### Moving with Mouse

- **Click and drag** the top [Border](~/api/Terminal.Gui.ViewBase.Border.yml) to move the view
- The view's `Frame` updates as it moves
- Release the mouse to complete the move

### Moving with Keyboard

1. Press `Ctrl+F5` to enter **Arrange Mode**
2. A move indicator (`◊`) appears in the top-left corner
3. Use **arrow keys** to move the view
4. Press `Esc` or `Ctrl+F5` to exit Arrange Mode

---

## Resizable Views

Views with resizable flags can be resized by the user on specific edges.

### All Edges Resizable

```csharp
var window = new Window
{
    Title = "Resize Me!",
    Arrangement = ViewArrangement.Resizable,
    BorderStyle = LineStyle.Single
};
```

### Specific Edge Resizable

```csharp
// Only right and bottom edges resizable
var view = new View
{
    Arrangement = ViewArrangement.RightResizable | ViewArrangement.BottomResizable,
    BorderStyle = LineStyle.Single
};
```

### Resizing with Mouse

- **Click and drag** any enabled border edge
- Resize indicators appear on hover
- The view's `Width` and `Height` update

### Resizing with Keyboard

1. Press `Ctrl+F5` to enter **Arrange Mode**
2. Press `Tab` to cycle to resize mode
3. Resize indicator (`⇲`) appears
4. Use **arrow keys** to resize
5. Press `Esc` or `Ctrl+F5` to exit

---

## Creating Resizable Splitters

A common pattern in tiled layouts is creating a resizable splitter between two panes.

### Horizontal Splitter (Left/Right Panes)

```csharp
View leftPane = new ()
{
    X = 0,
    Y = 0,
    Width = Dim.Fill(Dim.Func(_ => rightPane.Frame.Width)),
    Height = Dim.Fill(),
    BorderStyle = LineStyle.Single
};

View rightPane = new ()
{
    X = Pos.Right(leftPane) - 1,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(),
    Arrangement = ViewArrangement.LeftResizable,
    BorderStyle = LineStyle.Single,
    SuperViewRendersLineCanvas = true
};
rightPane.Border.Thickness = new Thickness(1, 0, 0, 0); // Only left border

container.Add(leftPane, rightPane);
```

**How it works:**
- `rightPane` has [ViewArrangement.LeftResizable](~/api/Terminal.Gui.ViewBase.ViewArrangement.yml) - its left border is draggable
- `leftPane` uses [Dim.Fill](~/api/Terminal.Gui.ViewBase.Dim.yml) with a function to fill remaining space
- `SuperViewRendersLineCanvas = true` ensures proper line rendering
- Only the left border is visible, acting as the splitter

### Vertical Splitter (Top/Bottom Panes)

```csharp
View topPane = new ()
{
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(Dim.Func(_ => bottomPane.Frame.Height)),
    BorderStyle = LineStyle.Single
};

View bottomPane = new ()
{
    X = 0,
    Y = Pos.Bottom(topPane) - 1,
    Width = Dim.Fill(),
    Height = Dim.Fill(),
    Arrangement = ViewArrangement.TopResizable,
    BorderStyle = LineStyle.Single,
    SuperViewRendersLineCanvas = true
};
bottomPane.Border.Thickness = new Thickness(1, 0, 0, 0); // Only top border

container.Add(topPane, bottomPane);
```

---

## Modal Views

**Modal** views run as exclusive applications that capture all user input until closed.

See the [Multitasking Deep Dive](multitasking.md) for complete details on modal execution.

### What Makes a View Modal

A view is modal when:
- Run via <xref:Terminal.Gui.App.IApplication>`.Run`
- Runnable.Modal == `true`

### Modal Characteristics

- **Exclusive Input** - All keyboard and mouse input goes to the modal view
- **Constrained Z-Order** - Modal view has Z-order of 1, everything else at 0
- **Blocks Execution** - <xref:Terminal.Gui.App.IApplication>`.Run` blocks until `Application.RequestStop()` is called
- **Own SessionToken** - Each modal view has its own [SessionToken](~/api/Terminal.Gui.App.SessionToken.yml)

### Modal View Types

- [Dialog](~/api/Terminal.Gui.Views.Dialog.yml) - Centered modal window with button support
- [MessageBox](~/api/Terminal.Gui.Views.MessageBox.yml) - Simple message dialogs
- [Wizard](~/api/Terminal.Gui.Views.Wizard.yml) - Multi-step modal dialogs

### Modal Example

```csharp
var dialog = new Dialog
{
    Title = "Confirm",
    Width = 40,
    Height = 10
};

var label = new Label 
{ 
    Text = "Are you sure?", 
    X = Pos.Center(), 
    Y = 2 
};
dialog.Add(label);

var ok = new Button { Text = "OK" };
ok.Accepting += (s, e) => Application.RequestStop();
dialog.AddButton(ok);

// Run modally - blocks until closed
Application.Run(dialog);

// Dialog has been closed
```

---

## Runnable Views

**Runnable** views are those run via [Application.Run](~/api/Terminal.Gui.App.Application.yml). Each non-modal Runnable view operates as a self-contained "application" with its own [SessionToken](~/api/Terminal.Gui.App.SessionToken.yml).

See the [Multitasking Deep Dive](multitasking.md) for complete details.

### Non-Modal Runnable Views

```csharp
var runnable = new Runnable
{
    Modal = false // Non-modal
};

// Runs as independent application
Application.Run(runnable);
```

**Characteristics:**
- Has its own `SessionToken`
- Events dispatched independently
- Can run on separate threads
- See `BackgroundWorkerCollection` for multi-threaded examples

### Modal vs Non-Modal Runnable

| Aspect | Modal | Non-Modal |
|--------|-------|-----------|
| Input | Exclusive | Shared |
| Z-Order | Constrained (1 vs 0) | Full Z-order support |
| Blocks Execution | Yes | No |
| Use Case | Dialogs, confirmations | Multi-window apps |

---

## Tiled vs Overlapped Layouts

### Tiled Layout (Default)

SubViews do not overlap, positioned side-by-side or top-to-bottom:

```csharp
var container = new View();

var left = new View 
{ 
    X = 0, 
    Y = 0, 
    Width = Dim.Percent(50), 
    Height = Dim.Fill() 
};

var right = new View 
{ 
    X = Pos.Right(left), 
    Y = 0, 
    Width = Dim.Fill(), 
    Height = Dim.Fill() 
};

container.Add(left, right);
```

**Benefits:**
- Simpler layout logic
- No Z-order management
- More predictable behavior
- Standard for most TUI applications

### Overlapped Layout

SubViews can overlap with Z-order determining which is on top:

```csharp
var container = new View 
{ 
    Arrangement = ViewArrangement.Overlapped 
};

var window1 = new Window 
{ 
    X = 5, 
    Y = 3, 
    Width = 40, 
    Height = 15,
    Arrangement = ViewArrangement.Movable | ViewArrangement.Overlapped
};

var window2 = new Window 
{ 
    X = 15, 
    Y = 8, 
    Width = 40, 
    Height = 15,
    Arrangement = ViewArrangement.Movable | ViewArrangement.Overlapped
};

container.Add(window1, window2);
// window2 appears on top of window1
```

**Z-Order:**
- Order in `SubViews` determines Z-order
- Later views appear above earlier views
- Use [View.MoveSubViewToEnd](~/api/Terminal.Gui.ViewBase.View.yml) / [View.MoveSubViewToStart](~/api/Terminal.Gui.ViewBase.View.yml) to change Z-order

**Navigation:**
- `Tab` / `Shift+Tab` - Navigate within current overlapped view
- `Ctrl+Tab` (`Ctrl+PageDown`) - Switch to next overlapped view
- `Ctrl+Shift+Tab` (`Ctrl+PageUp`) - Switch to previous overlapped view

---

## Examples

### Example 1: Movable and Resizable Window

```csharp
using Terminal.Gui;

using IApplication app = Application.Create();
app.Init();

Window window = new ()
{
    Title = "Drag and Resize Me! (Ctrl+F5 for keyboard mode)",
    X = Pos.Center(),
    Y = Pos.Center(),
    Width = 50,
    Height = 15,
    Arrangement = ViewArrangement.Movable | ViewArrangement.Resizable,
    BorderStyle = LineStyle.Double
};

Label label = new ()
{
    Text = "Try dragging the border with mouse\nor press Ctrl+F5!",
    X = Pos.Center(),
    Y = Pos.Center()
};
window.Add(label);

app.Run(window);
window.Dispose();
```

### Example 2: Horizontal Resizable Splitter

```csharp
using IApplication app = Application.Create();
app.Init();

Runnable top = new ();

FrameView leftPane = new ()
{
    Title = "Left Pane",
    X = 0,
    Y = 0,
    Width = Dim.Fill(Dim.Func(_ => rightPane.Frame.Width)),
    Height = Dim.Fill()
};

FrameView rightPane = new ()
{
    Title = "Right Pane (drag left edge)",
    X = Pos.Right(leftPane) - 1,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(),
    Arrangement = ViewArrangement.LeftResizable,
    SuperViewRendersLineCanvas = true
};
rightPane.Border.Thickness = new Thickness(1, 0, 0, 0);

top.Add(leftPane, rightPane);

app.Run(top);
top.Dispose();
```

### Example 3: Overlapped Windows

```csharp
using IApplication app = Application.Create();
app.Init();

Runnable desktop = new ()
{
    Arrangement = ViewArrangement.Overlapped
};

Window window1 = new ()
{
    Title = "Window 1",
    X = 5,
    Y = 3,
    Width = 40,
    Height = 12,
    Arrangement = ViewArrangement.Movable | ViewArrangement.Resizable | ViewArrangement.Overlapped
};

Window window2 = new ()
{
    Title = "Window 2 (overlaps Window 1)",
    X = 15,
    Y = 8,
    Width = 40,
    Height = 12,
    Arrangement = ViewArrangement.Movable | ViewArrangement.Resizable | ViewArrangement.Overlapped
};

desktop.Add(window1, window2);

app.Run(desktop);
desktop.Dispose();
```

### Example 4: Custom Arrange Key

```csharp
using Terminal.Gui;
using Terminal.Gui.Configuration;

using IApplication app = Application.Create();

// Change the arrange key
Application.DefaultKeyBindings[Command.Arrange] = Bind.All (Key.F2);

app.Init();

Window window = new ()
{
    Title = "Press F2 to enter arrange mode",
    Arrangement = ViewArrangement.Movable | ViewArrangement.Resizable
};

app.Run(window);
window.Dispose();
```

---

## Advanced Topics

### Constraints and Limitations

Arrangement only works when:

1. **View has a SuperView** - Root views cannot be arranged
2. **Independent Position/Size** - Views with [Pos.Align](~/api/Terminal.Gui.ViewBase.Pos.yml) or complex [Dim](~/api/Terminal.Gui.ViewBase.Dim.yml) constraints may not resize properly
3. **Border Required** - Mouse-based arrangement requires a visible [Border](~/api/Terminal.Gui.ViewBase.Border.yml)

### SuperViewRendersLineCanvas

When creating splitters, set `SuperViewRendersLineCanvas` = `true`:

```csharp
rightPane.SuperViewRendersLineCanvas = true;
```

This ensures [LineCanvas](~/api/Terminal.Gui.Drawing.LineCanvas.yml) properly handles line intersections at borders.

### Z-Order Management

For overlapped views, manage Z-order with:

```csharp
// Bring a view to the front (end of SubViews = highest Z-order)
container.MoveSubViewToEnd (window1);

// Send a view to the back (start of SubViews = lowest Z-order)
container.MoveSubViewToStart (window2);

// Move one position towards front/back
container.MoveSubViewTowardsEnd (window1);
container.MoveSubViewTowardsStart (window2);
```

### Arrangement Events

Monitor arrangement changes by handling layout events:

```csharp
view.FrameChanged += (_, e) =>
{
    // Fires when Frame changes (move or resize)
    Console.WriteLine ($"View moved/resized to {e.CurrentValue}");
};
```

---

## See Also

- **[Layout Deep Dive](layout.md)** - Overall layout system
- **[View Deep Dive](View.md)** - View base class
- **[Multitasking Deep Dive](multitasking.md)** - Modal and runnable views
- **[Drawing Deep Dive](drawing.md)** - LineCanvas and borders
- **[Configuration Deep Dive](config.md)** - Configuring the Arrange key via `Application.DefaultKeyBindings`

### API Reference

- `Arrangement`
- <xref:Terminal.Gui.ViewBase.ViewArrangement>
- <xref:Terminal.Gui.ViewBase.Border>
- `Application.GetDefaultKey (Command.Arrange)`
- Runnable.Modal

### UICatalog Examples

The UICatalog application demonstrates arrangement:

- **Arrangement Editor** - Interactive arrangement demonstration
- **Overlapped** scenario - Shows overlapped window management
- **Splitter** examples - Various splitter configurations
