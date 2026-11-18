# Popovers Deep Dive

Popovers are transient UI elements that appear above other content to display contextual information, such as menus, tooltips, autocomplete suggestions, and dialog boxes. Terminal.Gui's popover system provides a flexible, non-modal way to present temporary UI without blocking the rest of the application.

## Overview

Normally, Views cannot draw outside of their `Viewport`. To display content that appears to "pop over" other views, Terminal.Gui provides the popover system via `Application.Popover`. Popovers differ from alternatives like modifying <xref:Terminal.Gui.ViewBase.Border> or <xref:Terminal.Gui.ViewBase.Margin> behavior because they:

- Are managed centrally by the application
- Support focus and keyboard event routing
- Automatically hide in response to user actions
- Can receive global hotkeys even when not visible

## Architecture

The popover system follows a layered architecture with well-defined responsibilities at each level:

```
Application.Popover (static accessor)
    └── ApplicationPopover (manager)
            └── IPopoverView (interface contract)
                    └── PopoverImpl (abstract base)
                            └── Popover<TView, TResult> (generic content host)
                                    └── PopoverMenu : Popover<Menu, MenuItem> (concrete implementation)
```

### `IPopoverView` — The Contract

The `IPopoverView` interface defines the popover contract: visibility, enabled state, anchor positioning, target reference, and `MakeVisible` method. It extends `IPopover` which provides a `Current` property associating the popover with a specific <xref:Terminal.Gui.App.IRunnable>. This association controls keyboard event scoping — if `Current` is `null`, the popover receives all keyboard events globally; if set, events only flow when the associated runnable is the active `TopRunnableView`.

### `PopoverImpl` — The Foundation

`PopoverImpl` provides the standard popover behavior that all concrete popovers inherit. It configures:

- **Full-screen sizing** — `Width = Dim.Fill()`, `Height = Dim.Fill()`
- **Transparency** — `ViewportSettings.Transparent | ViewportSettings.TransparentMouse`
- **Dismiss key** — Binds `Application.GetDefaultKey (Command.Quit)` to `Command.Quit`
- **Focus management** — Restores focus to the previously-focused view when hidden
- **Layout on show** — Lays out the popover to fit the screen when becoming visible
- **Target** — Weak reference to a target view with automatic `CommandBridge` for command routing

### `Popover<TView, TResult>` — The Generic Content Host

`Popover<TView, TResult>` extends `PopoverImpl` to host a typed content view and optionally extract a result when the popover closes. This layer provides:

- **ContentView** — A typed `TView` that is added as a SubView, with a `CommandBridge` that routes `Command.Activate` from the content view to the popover
- **Result extraction** — Via `ResultExtractor` function or automatic `IValue<TResult>` extraction
- **Visible** — Uses standard `View.Visible` with `VisibleChanging`/`VisibleChanged` CWP events for lifecycle management
- **Anchor** — Function-based positioning anchor for `MakeVisible` / `SetPosition`
- **MakeVisible / SetPosition** — Methods for showing and positioning the popover

Custom popovers that host a specific view type should extend `Popover<TView, TResult>` rather than `PopoverImpl` directly.

### <xref:Terminal.Gui.App.ApplicationPopover> — The Manager

<xref:Terminal.Gui.App.ApplicationPopover> is a singleton held by `IApplication.Popover` that manages the lifecycle of all popovers. It maintains a list of registered popovers and tracks the single active (visible) popover. Only one popover can be active at a time — showing a new one automatically hides the previous one.

## The Transparency Model

Popovers use a "full-screen transparent overlay" technique. Instead of drawing a small popup widget directly, a popover fills the entire screen with `Dim.Fill()` but sets its viewport to be transparent. This means:

- The popover view itself is invisible — only its SubViews (like a <xref:Terminal.Gui.Views.Menu>) are drawn
- Mouse clicks that don't hit a SubView pass through to views beneath (via `TransparentMouse`)
- This naturally creates the "click outside to dismiss" behavior without complex hit-testing

This approach is elegant because the framework's existing transparency system handles the overlay logic. The popover doesn't need to know what's behind it or where it is positioned relative to other views.

## Creating a Popover

### Using PopoverMenu

The easiest way to create a popover is to use <xref:Terminal.Gui.Views.PopoverMenu>, which provides a cascading menu implementation:

```csharp
// Create a popover menu with menu items
PopoverMenu contextMenu = new ([
    new MenuItem ("Cut", Command.Cut),
    new MenuItem ("Copy", Command.Copy),
    new MenuItem ("Paste", Command.Paste),
    new MenuItem ("Select All", Command.SelectAll)
]);

// IMPORTANT: Register before showing
Application.Popover?.Register (contextMenu);

// Show at mouse position or specific location
contextMenu.MakeVisible (); // Uses current mouse position
// OR
contextMenu.MakeVisible (new Point (10, 5)); // Specific location
```

### Creating a Custom Popover

For popovers that host a specific view type, extend `Popover<TView, TResult>`:

```csharp
public class MyListPopover : Popover<ListView, string?>
{
    public MyListPopover ()
    {
        // ContentView is automatically created (new ListView())
        // and added as a SubView with command bridging

        // Configure the content view
        ContentView!.SetSource (["Option 1", "Option 2", "Option 3"]);

        // Set up result extraction
        ResultExtractor = lv => lv.Source?.ToList ()?.ElementAtOrDefault (lv.SelectedItem) as string;
    }
}

// Usage:
MyListPopover myPopover = new ();
Application.Popover?.Register (myPopover);
myPopover.MakeVisible (new Point (10, 5));
// After closing, check myPopover.Result for the selected item
```

For simpler popovers that don't need typed content hosting, inherit from `PopoverImpl`:

```csharp
public class MyCustomPopover : PopoverImpl
{
    public MyCustomPopover ()
    {
        // PopoverImpl already sets up required defaults:
        // - ViewportSettings with Transparent and TransparentMouse flags
        // - Command.Quit binding to hide the popover
        // - Width/Height set to Dim.Fill()

        // Add your custom content
        Label label = new () { Text = "Custom Popover Content" };
        Add (label);

        // Optionally override size
        Width = 40;
        Height = 10;
    }
}

// Usage:
MyCustomPopover myPopover = new ();
Application.Popover?.Register (myPopover);
Application.Popover?.Show (myPopover);
```

## Popover Requirements

A View qualifies as a popover if it:

1. **Implements `IPopoverView`** — Provides visibility, anchor, target, and `MakeVisible` operations
2. **Is Focusable** — `CanFocus = true` to receive keyboard input
3. **Is Transparent** — `ViewportSettings` includes both:
   - `ViewportSettings.Transparent` — Allows content beneath to show through
   - `ViewportSettings.TransparentMouse` — Mouse clicks outside SubViews pass through
4. **Handles Quit** — Binds `Application.GetDefaultKey (Command.Quit)` to `Command.Quit` and sets `Visible = false`

`PopoverImpl` provides all these requirements by default.

## Registration and Lifecycle

### The Registration-Before-Show Pattern

**All popovers must be registered before they can be shown.** Registration and showing are intentionally separate operations:

- **Registration** (`Register()`) is done once and enables the popover to participate in keyboard event routing, even when hidden. It also enrolls the popover for automatic lifecycle management.
- **Showing** (`Show()`) can be called many times and makes the popover visible.

Attempting to call `Show()` on an unregistered popover throws `InvalidOperationException`.

```csharp
PopoverMenu popover = new ([...]);

// REQUIRED: Register with the application
Application.Popover?.Register (popover);

// Now you can show it (and hide/show it repeatedly)
Application.Popover?.Show (popover);
// OR
popover.MakeVisible (); // For PopoverMenu
```

**Why Registration is Required:**

- Enables keyboard event routing to the popover (global hotkeys work even when hidden)
- Manages popover lifecycle (auto-disposal on `Application.Shutdown`)
- Sets the `Current` runnable association automatically to `TopRunnableView`

### Showing and Hiding

**Show a popover:**
```csharp
Application.Popover?.Show (popover);
```

The `Show()` method validates that the popover:
- Is registered
- Has `Transparent` and `TransparentMouse` viewport flags set
- Has a key binding for `Command.Quit`

It then initializes the popover if needed, hides any previously active popover, and makes the new one visible.

**Hide a popover:**
```csharp
// Method 1: Via ApplicationPopover
Application.Popover?.Hide (popover);

// Method 2: Set Visible property
popover.Visible = false;

// Automatic hiding occurs when:
// - User presses the quit key (typically Esc; see Application.GetDefaultKey (Command.Quit))
// - User clicks outside the popover (not on a SubView)
// - Another popover is shown
```

### Lifecycle Management

**Registered popovers:**
- Have their lifetime managed by the application
- Are automatically disposed when `Application.Shutdown ()` is called
- Receive keyboard events based on their associated runnable

**To manage lifetime manually:**
```csharp
// Deregister to take ownership of disposal
Application.Popover?.DeRegister (popover);

// Now you're responsible for disposal
popover.Dispose ();
```

## Visibility-Driven Lifecycle

The popover lifecycle is driven entirely by the `Visible` property — there is no separate "Open/Close" API. When visibility changes, a cascade of events occurs:

**Becoming visible (`Visible` changes to `true`):**
1. `PopoverImpl.OnVisibleChanging()` calls `Layout(App.Screen.Size)` to size the popover to the screen
2. `PopoverMenu.OnVisibleChanged()` calls `Root.ShowMenu()` (which sets `Menu.Visible = true` and `Menu.Enabled = true`)
3. `Popover<TView, TResult>.OnVisibleChanged()` sets `ContentView.Visible = true`
4. The popover receives focus

**Becoming hidden (`Visible` changes to `false`):**
1. `PopoverImpl.OnVisibleChanging()` restores focus to the previously-focused view in the `TopRunnableView`
2. `PopoverMenu.OnVisibleChanged()` calls `Root.HideMenu()` and `ApplicationPopover.Hide()`
3. `Popover<TView, TResult>.OnVisibleChanged()` sets `ContentView.Visible = false` and extracts `Result`
4. `ApplicationPopover.Hide()` clears the active popover reference and triggers a redraw

This pattern means setting `Visible = false` is equivalent to calling `Hide()` — both produce the same result.

## Keyboard Event Routing

### Dispatch Order

When a key is pressed, `ApplicationPopover.DispatchKeyDown()` routes it through popovers in a specific order:

1. **Active (visible) popover** receives ALL key events first. If it handles the key, processing stops.
2. **Inactive (hidden) popovers** each receive the key event, filtered by their `Current` runnable association. Popovers whose `Current` doesn't match the active `TopRunnableView` are skipped.

This design ensures:
- A visible popover can intercept any key (e.g., Escape to close, arrow keys to navigate)
- Hidden popovers can still respond to global hotkeys (e.g., Shift+F10 to open a menu)
- Popovers scoped to a specific runnable only activate when that runnable is in focus

### Global Hotkeys

Registered popovers receive keyboard events even when not visible, enabling global hotkey support:

```csharp
PopoverMenu menu = new ([...]);
menu.Key = Key.F10.WithShift; // Default hotkey

Application.Popover?.Register (menu);

// Now pressing Shift+F10 anywhere in the app will show the menu
```

### Runnable Association

The `IPopover.Current` property associates a popover with a specific <xref:Terminal.Gui.App.IRunnable>:

- If `null`: Popover receives all keyboard events from the application
- If set: Popover only receives events when the associated runnable is active
- Automatically set to `Application.TopRunnableView` during registration

```csharp
// Associate with a specific runnable
myPopover.Current = myWindow; // Only active when myWindow is the top runnable
```

## Focus and Input

**When visible:**
- Popovers receive focus automatically
- All keyboard input goes to the popover until hidden
- Mouse clicks on SubViews are captured
- Mouse clicks outside SubViews pass through (due to `TransparentMouse`)

**When hidden:**
- Only registered hotkeys are processed
- Other keyboard input is not captured

## Layout and Positioning

### Default Layout

`PopoverImpl` sets `Width = Dim.Fill ()` and `Height = Dim.Fill ()` (see <xref:Terminal.Gui.ViewBase.Dim>), making the popover fill the screen by default. The transparent viewport settings allow content beneath to remain visible.

### Custom Sizing

Override `Width` and `Height` to customize size:

```csharp
public class MyPopover : PopoverImpl
{
    public MyPopover ()
    {
        Width = 40;  // Fixed width
        Height = Dim.Auto (); // Auto height based on content
    }
}
```

### Positioning with PopoverMenu

<xref:Terminal.Gui.Views.PopoverMenu> provides positioning helpers:

```csharp
// Position at specific screen coordinates
menu.SetPosition (new Point (10, 5));

// Show and position in one call
menu.MakeVisible (new Point (10, 5));

// Uses mouse position if null
menu.MakeVisible (); // Uses Application.Mouse.LastMousePosition
```

The menu automatically adjusts position to ensure it remains fully visible on screen.

## Built-in Popover Types

### PopoverMenu

<xref:Terminal.Gui.Views.PopoverMenu> extends `Popover<Menu, MenuItem>` and is a sophisticated cascading menu implementation used for:
- Context menus
- <xref:Terminal.Gui.Views.MenuBar> drop-down menus
- Custom menu scenarios

**Key Features:**
- Extends `Popover<Menu, MenuItem>`, inheriting `ContentView`, `MakeVisible`, `SetPosition`, `Result`, and `Anchor`
- `Root` property aliases `ContentView` — the hosted <xref:Terminal.Gui.Views.Menu> instance
- Two `CommandBridge` instances route commands across containment boundaries: Content bridge (Menu → PopoverMenu) and Target bridge (PopoverMenu → host/MenuBarItem)
- Cascading submenus with automatic positioning
- Keyboard navigation (arrow keys, hotkeys)
- Automatic key binding discovery from Commands — menu items that specify a <xref:Terminal.Gui.Input.Command> automatically display the correct keyboard shortcut
- Mouse support
- Separator lines via `new Line ()`

**Example with submenus:**
```csharp
PopoverMenu fileMenu = new ([
    new MenuItem ("New", Command.New),
    new MenuItem ("Open", Command.Open),
    new MenuItem {
        Title = "Recent",
        SubMenu = new Menu ([
            new MenuItem ("File1.txt", Command.Open),
            new MenuItem ("File2.txt", Command.Open)
        ])
    },
    new Line (),
    new MenuItem ("Exit", Command.Quit)
]);

Application.Popover?.Register (fileMenu);
fileMenu.MakeVisible ();
```

## Mouse Event Handling

Popovers use `ViewportSettings.TransparentMouse`, which means:

- **Clicks on popover SubViews**: Captured and handled normally
- **Clicks outside SubViews**: Pass through to views beneath
- **Clicks on background**: Automatically hide the popover

This creates the expected behavior where clicking outside a menu or dialog closes it.

### Dismiss-and-Re-show Prevention

When a popover is dismissed by a mouse press outside its SubViews, `ApplicationMouse.RaiseMouseEvent` recurses so the click event can reach views beneath the popover. This enables the "click a different button while a context menu is open" scenario — the menu closes and the button activates in a single click.

However, if the view beneath the popover is the **same view that opened it** (e.g., a `DropDownList` toggle button or a `MenuBarItem`), the recursed event would re-activate the view and re-show the popover. To prevent this, `ApplicationMouse` records which popover was just dismissed (`DismissedByMousePress`) and `ApplicationPopover.Show` checks this guard — silently returning if the caller is trying to re-show the same popover. The guard spans the entire mouse interaction cycle (press → release → click) because `Button` and other views with `ShouldAutoGrab` start an auto-grab on the pressed event and invoke commands on the subsequent clicked event. The guard is cleared when:

- A new, unrelated press event arrives (enabling the popover to be toggled open again on the next click)
- The click cycle completes (`IsSingleDoubleOrTripleClicked`)
- `ApplicationMouse.ResetState` is called

This means views that open popovers on activation do **not** need their own re-show prevention logic — the framework handles it automatically.

## Best Practices

1. **Always Register First**
   ```csharp
   // WRONG - Will throw InvalidOperationException
   PopoverMenu menu = new ([...]);
   menu.MakeVisible ();

   // CORRECT
   PopoverMenu menu = new ([...]);
   Application.Popover?.Register (menu);
   menu.MakeVisible ();
   ```

2. **Use PopoverMenu for Menus**
   - Don't reinvent the wheel for standard menu scenarios
   - Leverage built-in keyboard navigation and positioning

3. **Manage Lifecycle Appropriately**
   - Let the application manage disposal for long-lived popovers
   - Deregister and manually dispose short-lived or conditional popovers

4. **Test Global Hotkeys**
   - Ensure hotkeys don't conflict with application-level keys
   - Consider providing configuration for custom hotkeys

5. **Handle Edge Cases**
   - Test positioning near screen edges
   - Verify behavior with multiple runnables
   - Test with keyboard-only navigation

## Common Scenarios

### Context Menu on Right-Click

```csharp
PopoverMenu contextMenu = new ([...]);
contextMenu.MouseFlags = MouseFlags.Button3Clicked; // Right-click
Application.Popover?.Register (contextMenu);

myView.MouseClick += (s, e) =>
{
    if (e.MouseEvent.Flags == MouseFlags.Button3Clicked)
    {
        contextMenu.MakeVisible (myView.ScreenToViewport (e.MouseEvent.Position));
        e.Handled = true;
    }
};
```

### Autocomplete Popup

```csharp
public class AutocompletePopover : PopoverImpl
{
    private ListView _listView;

    public AutocompletePopover ()
    {
        Width = 30;
        Height = 10;

        _listView = new ListView
        {
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };
        Add (_listView);
    }

    public void ShowSuggestions (IEnumerable<string> suggestions, Point position)
    {
        _listView.SetSource (suggestions.ToList ());
        // Position below the text entry field
        X = position.X;
        Y = position.Y + 1;
        Visible = true;
    }
}
```

### Global Command Palette

```csharp
PopoverMenu commandPalette = new (GetAllCommands ());
commandPalette.Key = Key.P.WithCtrl; // Ctrl+P to show

Application.Popover?.Register (commandPalette);

// Now Ctrl+P anywhere in the app shows the command palette
```

## API Reference

- `IPopoverView` - Interface for popover views (extends `IPopover`)
- `PopoverImpl` - Abstract base class providing standard popover behavior (implements `IPopoverView`)
- `Popover<TView, TResult>` - Generic base class for popovers hosting typed content views with result extraction
- <xref:Terminal.Gui.Views.PopoverMenu> - Cascading menu implementation (`Popover<Menu, MenuItem>`)
- <xref:Terminal.Gui.Views.DropDownList> - Dropdown/combo-box control (`TextField` with `Popover<ListView, string?>`)
- <xref:Terminal.Gui.App.ApplicationPopover> - Popover manager (accessed via `Application.Popover`)

## See Also

- [Keyboard Deep Dive](keyboard.md) - Understanding keyboard event routing
- [Mouse Deep Dive](mouse.md) - Mouse event handling
- [View List](views.md) - Full list of views including MenuBar
