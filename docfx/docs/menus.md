# Menus Deep Dive

Terminal.Gui provides a comprehensive, hierarchical menu system built on top of the <xref:Terminal.Gui.Views.Shortcut> and <xref:Terminal.Gui.Views.Bar> classes. This deep dive covers the architecture, class relationships, command routing, and interactions between the menu components.

## Table of Contents

- [Overview](#overview)
- [Class Hierarchy](#class-hierarchy)
- [Component Descriptions](#component-descriptions)
- [Architecture](#architecture)
- [Command Routing](#command-routing)
- [Interactions](#interactions)
- [Usage Examples](#usage-examples)
- [Keyboard Navigation](#keyboard-navigation)
- [Event Flow](#event-flow)

---

## Overview

The menu system in Terminal.Gui consists of the following key components:

| Component | Description |
|-----------|-------------|
| <xref:Terminal.Gui.Views.Shortcut> | Base class for displaying a command, help text, and key binding |
| <xref:Terminal.Gui.Views.Bar> | Container for <xref:Terminal.Gui.Views.Shortcut> items, supports horizontal/vertical orientation |
| <xref:Terminal.Gui.Views.MenuItem> | A <xref:Terminal.Gui.Views.Shortcut>-derived item for use in menus, supports submenus |
| <xref:Terminal.Gui.Views.Menu> | A vertically-oriented <xref:Terminal.Gui.Views.Bar> that contains <xref:Terminal.Gui.Views.MenuItem> items |
| <xref:Terminal.Gui.Views.MenuBarItem> | A <xref:Terminal.Gui.Views.MenuItem> that holds a <xref:Terminal.Gui.Views.PopoverMenu> instead of a `SubMenu` |
| <xref:Terminal.Gui.Views.MenuBar> | A horizontal <xref:Terminal.Gui.Views.Menu> that contains <xref:Terminal.Gui.Views.MenuBarItem> items |
| <xref:Terminal.Gui.Views.PopoverMenu> | A `Popover<Menu, MenuItem>`-derived view that hosts cascading menus |

---

## Class Hierarchy

The menu system builds upon a layered class hierarchy:

```
View
├── Shortcut                    // Command + HelpText + Key display
│   └── MenuItem                // Menu-specific Shortcut with SubMenu support
│       └── MenuBarItem         // MenuItem that uses PopoverMenu instead of SubMenu
│
├── Bar                         // Container for Shortcuts (horizontal/vertical)
│   └── Menu                    // Vertical Bar for MenuItems
│       └── MenuBar             // Horizontal Menu for MenuBarItems
│
└── PopoverImpl             // Base for popover views
    └── Popover<TView, TResult> // Generic popover with content view + result extraction
        └── PopoverMenu         // Cascading menu popover (Popover<Menu, MenuItem>)
```

### Inheritance Details

**Shortcut → MenuItem → MenuBarItem:**
- <xref:Terminal.Gui.Views.Shortcut> displays command text, help text, and a key binding
- <xref:Terminal.Gui.Views.MenuItem> extends <xref:Terminal.Gui.Views.Shortcut> to add `SubMenu` support for nested menus
- <xref:Terminal.Gui.Views.MenuBarItem> extends <xref:Terminal.Gui.Views.MenuItem> but replaces `SubMenu` with <xref:Terminal.Gui.Views.PopoverMenu>

**Bar → Menu → MenuBar:**
- <xref:Terminal.Gui.Views.Bar> is a generic container for <xref:Terminal.Gui.Views.Shortcut> views with orientation support
- <xref:Terminal.Gui.Views.Menu> is a vertical <xref:Terminal.Gui.Views.Bar> specialized for <xref:Terminal.Gui.Views.MenuItem> items
- <xref:Terminal.Gui.Views.MenuBar> is a horizontal <xref:Terminal.Gui.Views.Menu> specialized for <xref:Terminal.Gui.Views.MenuBarItem> items

For completeness, here's how <xref:Terminal.Gui.Views.StatusBar> fits in:

**Bar → StatusBar:**
- <xref:Terminal.Gui.Views.StatusBar> is a horizontal <xref:Terminal.Gui.Views.Bar> specialized for <xref:Terminal.Gui.Views.Shortcut> items

---

## Component Descriptions

### Shortcut

<xref:Terminal.Gui.Views.Shortcut> is the foundational building block. It displays three elements:

1. **CommandView** - The command text (left side by default)
2. **HelpView** - Help text (middle)
3. **KeyView** - Key binding display (right side)

```csharp
Shortcut shortcut = new ()
{
    Title = "_Save",           // CommandView text with hotkey
    HelpText = "Save the file",
    Key = Key.S.WithCtrl,
    Action = () => SaveFile ()
};
```

**IMPORTANT:** The `CommandView`, `HelpView`, and `KeyView` are subviews of the shortcut. But how they are managed is an implementation detail and `shortcut.SubViews` should not be used to try to access them.

Key features:
- Supports `Action` for direct invocation
- `BindKeyToApplication` enables application-wide key bindings
- `AlignmentModes` controls element ordering (start-to-end or end-to-start)
- `CommandView` can be replaced with custom views (e.g., <xref:Terminal.Gui.Views.CheckBox>)
- Uses relay dispatch (`ConsumeDispatch=false`): commands dispatched to `CommandView` complete normally, then <xref:Terminal.Gui.Views.Shortcut> is notified via a deferred callback
- Sets <xref:Terminal.Gui.ViewBase.View.CommandsToBubbleUp> = [<xref:Terminal.Gui.Input.Command.Activate>, <xref:Terminal.Gui.Input.Command.Accept>]

### Bar

<xref:Terminal.Gui.Views.Bar> is a container that arranges <xref:Terminal.Gui.Views.Shortcut> items either horizontally or vertically:

```csharp
Bar statusBar = new ()
{
    Orientation = Orientation.Horizontal,
    Y = Pos.AnchorEnd ()
};

statusBar.Add (new Shortcut { Title = "_Help", Key = Key.F1 });
statusBar.Add (new Shortcut { Title = "_Quit", Key = Key.Q.WithCtrl });
```

Key features:
- `Orientation` property controls layout direction
- `AlignmentModes` property controls item alignment
- Supports mouse wheel navigation
- Auto-sizes based on content (<xref:Terminal.Gui.ViewBase.DimAuto>)

### MenuItem

<xref:Terminal.Gui.Views.MenuItem> extends <xref:Terminal.Gui.Views.Shortcut> for use in menus:

```csharp
MenuItem menuItem = new ()
{
    Title = "_Open...",
    HelpText = "Open a file",
    Key = Key.O.WithCtrl,
    Action = () => OpenFile ()
};

// Or bind to a command on a target view
MenuItem boundItem = new (myView, Command.Save);
```

Key features:
- `SubMenu` property holds nested <xref:Terminal.Gui.Views.Menu> for cascading menus
- `TargetView` and `Command` enable command binding to other views
- Automatically gets focus on mouse enter
- Displays right-arrow glyph when it has a submenu
- When `SubMenu` is set, a `CommandBridge` connects the SubMenu back to this MenuItem (bridging <xref:Terminal.Gui.Input.Command.Activate> and <xref:Terminal.Gui.Input.Command.Accept> commands across the non-containment boundary)

### Menu

<xref:Terminal.Gui.Views.Menu> is a vertical <xref:Terminal.Gui.Views.Bar> specialized for menu items:

```csharp
Menu fileMenu = new ([
    new MenuItem ("_New", Key.N.WithCtrl, () => NewFile ()),
    new MenuItem ("_Open...", Key.O.WithCtrl, () => OpenFile ()),
    new Line (),  // Separator
    new MenuItem ("_Save", Key.S.WithCtrl, () => SaveFile ()),
    new MenuItem ("Save _As...", () => SaveAs ())
]);
```

Key features:
- Vertical orientation by default
- `SuperMenuItem` property links back to parent <xref:Terminal.Gui.Views.MenuItem>
- `SelectedMenuItem` tracks current selection
- Supports `Line` separators between items
- Uses `Schemes.Menu` color scheme by default
- Sets <xref:Terminal.Gui.ViewBase.View.CommandsToBubbleUp> = [<xref:Terminal.Gui.Input.Command.Accept>, <xref:Terminal.Gui.Input.Command.Activate>] — enables command propagation up through the PopoverMenu hierarchy
- Overrides `OnActivating` to dispatch <xref:Terminal.Gui.Input.Command.Activate> to the focused <xref:Terminal.Gui.Views.MenuItem> (manual dispatch, not the `GetDispatchTarget` pattern)
- `ShowMenu()` / `HideMenu()` control visibility and handle initialization; `HideMenu` cascades to visible SubMenus
- `GetAllSubMenus()` performs depth-first traversal of the SubMenu hierarchy
- `GetMenuItemsOfAllSubMenus()` collects all <xref:Terminal.Gui.Views.MenuItem>s across the hierarchy, with optional predicate
- `OnSelectedMenuItemChanged()` handles SubMenu display: hides peer SubMenus, shows the selected item's SubMenu, and performs basic positioning

### MenuBarItem

<xref:Terminal.Gui.Views.MenuBarItem> extends <xref:Terminal.Gui.Views.MenuItem> for use in <xref:Terminal.Gui.Views.MenuBar>:

```csharp
MenuBarItem fileMenuBarItem = new ("_File", [
    new MenuItem ("_New", Key.N.WithCtrl, () => NewFile ()),
    new MenuItem ("_Open...", Key.O.WithCtrl, () => OpenFile ()),
    new Line (),
    new MenuItem ("_Quit", Application.GetDefaultKey (Command.Quit), () => Application.RequestStop ())
]);
```

**Important:** <xref:Terminal.Gui.Views.MenuBarItem> uses <xref:Terminal.Gui.Views.PopoverMenu> instead of `SubMenu`. Attempting to set `SubMenu` will throw `InvalidOperationException`.

Key features:
- `PopoverMenu` property holds the dropdown menu and sets `Target`/`Anchor` on the popover for command bridging and positioning
- `PopoverMenuOpen` delegates to `PopoverMenu.IsOpen` and raises `PopoverMenuOpenChanged` (relayed from `PopoverMenu.IsOpenChanged`)
- When `PopoverMenu` is set, the base `Popover<TView, TResult>.Target` property creates a `CommandBridge` connecting the PopoverMenu back to this MenuBarItem, bridging <xref:Terminal.Gui.Input.Command.Activate> commands across the non-containment boundary
- Overrides `OnActivating` to toggle `PopoverMenuOpen`, with a guard that ignores `Bridged` commands (which are notifications from PopoverMenu internals, not toggle requests)
- Has a custom <xref:Terminal.Gui.ViewBase.View.HotKey> handler that skips [SetFocus()](xref:Terminal.Gui.ViewBase.View.SetFocus*) before invoking <xref:Terminal.Gui.Input.Command.Activate>, preventing premature popover opening during MenuBarItem switching

### MenuBar

<xref:Terminal.Gui.Views.MenuBar> is a horizontal menu bar typically placed at the top of a window:

```csharp
MenuBar menuBar = new ([
    new MenuBarItem ("_File", [
        new MenuItem ("_New", Key.N.WithCtrl, () => NewFile ()),
        new MenuItem ("_Open...", Key.O.WithCtrl, () => OpenFile ()),
        new Line (),
        new MenuItem ("E_xit", Application.GetDefaultKey (Command.Quit), () => Application.RequestStop ())
    ]),
    new MenuBarItem ("_Edit", [
        new MenuItem ("_Cut", Key.X.WithCtrl, () => Cut ()),
        new MenuItem ("_Copy", Key.C.WithCtrl, () => Copy ()),
        new MenuItem ("_Paste", Key.V.WithCtrl, () => Paste ())
    ]),
    new MenuBarItem ("_Help", [
        new MenuItem ("_About...", () => ShowAbout ())
    ])
]);

// Add to window
window.Add (menuBar);
```

Key features:
- `Key` property defines the activation key (default: `F10`)
- `Active` property controls whether the MenuBar is active — when `Active` changes, it drives <xref:Terminal.Gui.ViewBase.View.CanFocus> and hides any open PopoverMenus on deactivation
- `IsOpen()` returns whether any popover menu is visible
- `DefaultBorderStyle` configurable via themes
- Automatically positions at top with `Width = Dim.Fill ()`
- Uses consume dispatch (`ConsumeDispatch=true`, `GetDispatchTarget => Focused`) — the MenuBar owns activation state for its MenuBarItems
- Blocks activation when `!Visible || !Enabled`
- Registers custom command handlers for <xref:Terminal.Gui.Input.Command.HotKey>, `Command.Quit`, `Command.Right`, and `Command.Left`

### PopoverMenu

<xref:Terminal.Gui.Views.PopoverMenu> extends `Popover<Menu, MenuItem>` and hosts cascading menus:

```csharp
// Create a context menu
PopoverMenu contextMenu = new ([
    new MenuItem (targetView, Command.Cut),
    new MenuItem (targetView, Command.Copy),
    new MenuItem (targetView, Command.Paste),
    new Line (),
    new MenuItem (targetView, Command.SelectAll)
]);

// Register with application (required!)
Application.Popover?.Register (contextMenu);

// Show at mouse position
contextMenu.MakeVisible ();

// Or show at specific position
contextMenu.MakeVisible (new Point (10, 5));
```

Key features:
- Inherits from `Popover<Menu, MenuItem>`, which provides `ContentView`, `MakeVisible`, `SetPosition`, `Target`, `Anchor`, `Result`, and `ResultExtractor`
- `Root` property aliases `ContentView` and holds the top-level <xref:Terminal.Gui.Views.Menu>
- `Key` property for activation (default: `Shift+F10`)
- <xref:Terminal.Gui.Input.MouseFlags> property defines mouse button to show menu (default: right-click)
- **Must be registered** with `Application.Popover` before calling `MakeVisible`
- `Target` (inherited from `PopoverImpl`) establishes a `CommandBridge` so that commands from the menu hierarchy bridge to the target view
- Auto-positions to ensure visibility on screen via overridden `SetPosition` and `GetAdjustedPosition`
- SubMenu show/hide is handled by `Menu.OnSelectedMenuItemChanged()`; PopoverMenu's subscriber only adjusts positioning for screen boundaries via `GetMostVisibleLocationForSubMenu()`
- Registers custom command handlers for `Command.Right` (enter submenu), `Command.Left` (leave submenu), and `Command.Quit` (close menu)

**Important:** See the [Popovers Deep Dive](Popovers.md) for complete details on popover lifecycle and requirements.

---

## Architecture

### Relationship Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                              Window                                 │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │                           MenuBar                             │  │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐            │  │
│  │  │ MenuBarItem │  │ MenuBarItem │  │ MenuBarItem │   ...      │  │
│  │  │   "File"    │  │   "Edit"    │  │   "Help"    │            │  │
│  │  └──────┬──────┘  └─────────────┘  └─────────────┘            │  │
│  └─────────│─────────────────────────────────────────────────────┘  │
│            │                                                        │
│            │ owns (+ CommandBridge)                                 │
│            ▼                                                        │
│  ┌──────────────────┐                                               │
│  │   PopoverMenu    │ ◄─── Registered with Application.Popover      │
│  │  ┌────────────┐  │                                               │
│  │  │    Menu    │  │ ◄─── Root Menu                                │
│  │  │ (Root)     │  │                                               │
│  │  │ ┌────────┐ │  │                                               │
│  │  │ │MenuItem│─┼──┼──► SubMenu ──► Menu ──► MenuItem ──► SubMenu  │
│  │  │ │MenuItem│ │  │         ▲  (CommandBridge)                    │
│  │  │ │  Line  │ │  │         │                                     │
│  │  │ │MenuItem│ │  │     Cascading                                 │
│  │  │ └────────┘ │  │     Hierarchy                                 │
│  │  └────────────┘  │                                               │
│  └──────────────────┘                                               │
└─────────────────────────────────────────────────────────────────────┘
```

### Key Relationships

1. **MenuBar contains MenuBarItems:**
   - <xref:Terminal.Gui.Views.MenuBar> is a horizontal <xref:Terminal.Gui.Views.Menu> containing <xref:Terminal.Gui.Views.MenuBarItem> subviews
   - Each <xref:Terminal.Gui.Views.MenuBarItem> owns a <xref:Terminal.Gui.Views.PopoverMenu>

2. **MenuBarItem owns PopoverMenu (cross-boundary):**
   - `MenuBarItem.PopoverMenu` property holds the dropdown
   - A `CommandBridge` connects PopoverMenu back to MenuBarItem, bridging <xref:Terminal.Gui.Input.Command.Activate> commands
   - `PopoverMenuOpenChanged` event fires when visibility changes

3. **PopoverMenu contains Root Menu (via ContentView):**
   - `PopoverMenu.Root` aliases `Popover<Menu, MenuItem>.ContentView`, which is the top-level <xref:Terminal.Gui.Views.Menu>
   - A `CommandBridge` (created by the `ContentView` setter in `Popover<TView, TResult>`) bridges <xref:Terminal.Gui.Input.Command.Activate> from the Root Menu to the PopoverMenu
   - <xref:Terminal.Gui.Views.Menu> self-manages SubMenu show/hide and basic positioning via `OnSelectedMenuItemChanged()`
   - <xref:Terminal.Gui.Views.PopoverMenu> adjusts positioning for screen boundaries and manages its own visibility lifecycle

4. **Menu contains MenuItems:**
   - `Menu.SubViews` contains <xref:Terminal.Gui.Views.MenuItem> instances
   - `Menu.SelectedMenuItem` tracks the focused item
   - `Menu.CommandsToBubbleUp = [Command.Accept, Command.Activate]` enables propagation

5. **MenuItem may contain SubMenu (cross-boundary):**
   - `MenuItem.SubMenu` holds a nested <xref:Terminal.Gui.Views.Menu> for cascading
   - `Menu.SuperMenuItem` links back to the parent <xref:Terminal.Gui.Views.MenuItem>
   - A `CommandBridge` connects SubMenu back to its MenuItem, bridging <xref:Terminal.Gui.Input.Command.Activate> and <xref:Terminal.Gui.Input.Command.Accept>

---

## Command Routing

The menu system uses the [Command Deep Dive](command.md) infrastructure extensively. Understanding command routing is essential for working with menus.

### Dispatch Patterns

Each menu component uses a specific command dispatch pattern:

| Component | `GetDispatchTarget` | `ConsumeDispatch` | Pattern |
|-----------|--------------------|--------------------|---------|
| **MenuBar** | `Focused` (the focused MenuBarItem) | `true` | Consume: MenuBar owns activation state |
| **Menu** | Not overridden (manual dispatch in `OnActivating`) | Not overridden | Dispatches `Activate` to focused MenuItem directly |
| **MenuItem** | Inherited from Shortcut (`CommandView`) | `false` | Relay: CommandView completes normally, then MenuItem is notified |
| **MenuBarItem** | Inherited from MenuItem/Shortcut | `false` | Relay (inherited), plus custom `OnActivating` for PopoverMenu toggle |

### CommandBridge (Cross-Boundary Routing)

The menu system has two non-containment boundaries that require `CommandBridge`:

1. **MenuBarItem ↔ PopoverMenu:** <xref:Terminal.Gui.Views.PopoverMenu> is not a SubView of <xref:Terminal.Gui.Views.MenuBarItem> — it is registered with `Application.Popover` and lives outside the view hierarchy. The bridge brings <xref:Terminal.Gui.Input.Command.Activate> events from the PopoverMenu back to the MenuBarItem so they can bubble up through the MenuBar.

2. **MenuItem ↔ SubMenu:** `SubMenu` is not a SubView of <xref:Terminal.Gui.Views.MenuItem> — it is managed by the PopoverMenu's cascading infrastructure. The bridge brings <xref:Terminal.Gui.Input.Command.Activate> and <xref:Terminal.Gui.Input.Command.Accept> events from the SubMenu back to the MenuItem so they can bubble up through the Menu.

### Routing Modes in Menu Context

| Mode | When It Occurs | Effect |
|------|---------------|--------|
| **Direct** | User presses `F10`, or programmatic `InvokeCommand` | MenuBar toggles `Active` on/off |
| **BubblingUp** | MenuBarItem activation bubbles to MenuBar | MenuBar identifies the source MenuBarItem and shows/hides its PopoverMenu |
| **Bridged** | MenuItem activation inside PopoverMenu bridges to MenuBarItem | MenuBarItem ignores the command (notification only — no PopoverMenu toggle) |

### Command Bubbling

<xref:Terminal.Gui.Views.Menu> sets <xref:Terminal.Gui.ViewBase.View.CommandsToBubbleUp> = [<xref:Terminal.Gui.Input.Command.Accept>, <xref:Terminal.Gui.Input.Command.Activate>]. This means:

1. When a <xref:Terminal.Gui.Views.MenuItem> fires <xref:Terminal.Gui.Input.Command.Accept> or <xref:Terminal.Gui.Input.Command.Activate>, the command bubbles up through the Menu to its SuperView
2. For Root Menus inside a <xref:Terminal.Gui.Views.PopoverMenu>, the command reaches the PopoverMenu
3. The `CommandBridge` on MenuBarItem detects the <xref:Terminal.Gui.Input.Command.Activate> event on PopoverMenu and relays it to MenuBarItem
4. The MenuBarItem's activation then bubbles to the MenuBar via normal SuperView bubbling

---

## Interactions

### MenuBar Activation Flow

1. User presses `F10` (default) or clicks on <xref:Terminal.Gui.Views.MenuBar>
2. MenuBar's HotKey handler fires — for direct activation, this calls [InvokeCommand()](xref:Terminal.Gui.ViewBase.View.InvokeCommand*) (<xref:Terminal.Gui.Input.Command.Activate>)
3. `MenuBar.OnActivating` runs:
   - If `!Visible || !Enabled`: activation is blocked
   - If already `Active`: toggles off (`Active = false`)
   - Otherwise: sets `Active = true` and calls `ShowItem` on the first MenuBarItem with a PopoverMenu
4. `Active = true` sets <xref:Terminal.Gui.ViewBase.View.CanFocus> = `true`
5. `ShowItem` focuses the MenuBarItem and sets `PopoverMenuOpen = true`
6. `PopoverMenuOpen` setter calls `PopoverMenu.MakeVisible` with the calculated screen position

### MenuBarItem HotKey Activation

When a MenuBarItem's HotKey (e.g., `Alt+F` for "\_File") is pressed:

1. The HotKey is processed on the MenuBarItem, which has a custom HotKey handler
2. The handler skips [SetFocus()](xref:Terminal.Gui.ViewBase.View.SetFocus*) (to prevent premature popover opening) and directly invokes <xref:Terminal.Gui.Input.Command.Activate>
3. `MenuBarItem.OnActivating` toggles `PopoverMenuOpen`
4. The activation bubbles up to `MenuBar.OnActivating` with `BubblingUp` routing
5. MenuBar identifies the source MenuBarItem and either:
   - Activates the MenuBar and shows the source item's PopoverMenu (if opening)
   - Deactivates the MenuBar (if the PopoverMenu is closing)

### Switching Between MenuBarItems

When the MenuBar is active and a PopoverMenu is open:

- **Arrow keys:** `Command.Right`/`Command.Left` advance focus to the next/previous MenuBarItem. The `OnSelectedMenuItemChanged` callback detects the focus change and, while in _popover browsing mode_, calls `ShowItem` on the newly focused item.
- **Mouse hover:** Moving the mouse over a different MenuBarItem triggers `OnMouseEnter`, which sets focus. If in browsing mode, the new item's PopoverMenu opens automatically.
- **HotKey:** Pressing another MenuBarItem's <xref:Terminal.Gui.ViewBase.View.HotKey> directly invokes <xref:Terminal.Gui.Input.Command.Activate> on that item, causing a switch.

The `_isSwitchingItem` guard prevents premature deactivation during the brief interval when the old popover closes before the new one opens. The `_popoverBrowsingMode` flag tracks whether any popover is open, enabling auto-open behavior during navigation.

### PopoverMenu Display Flow

1. `MakeVisible()` is called (optionally with a position) — inherited from `Popover<TView, TResult>`
2. `Popover<TView, TResult>.SetPosition()` calculates a visible location on screen (PopoverMenu overrides with `new SetPosition` for menu-specific positioning)
3. `Application.Popover.Show()` is invoked, setting `Visible = true`
4. `PopoverMenu.OnVisibleChanged()` runs — calls `Root.ShowMenu()` (setting `Visible = true` and `Enabled = true` on the root Menu) **before** `base.OnVisibleChanged()` to ensure the Menu is enabled for focus
5. `Popover<TView, TResult>.OnVisibleChanged()` syncs `ContentView.Visible`
6. First <xref:Terminal.Gui.Views.MenuItem> receives focus

**Prerequisite:** The <xref:Terminal.Gui.Views.PopoverMenu> must be registered with `Application.Popover` before `MakeVisible` is called. For <xref:Terminal.Gui.Views.MenuBarItem>, registration happens automatically in `EndInit`. For standalone context menus, call `Application.Popover?.Register (contextMenu)` explicitly.

### Menu Selection Flow

1. User navigates with arrow keys or mouse
2. `Menu.Focused` changes to new <xref:Terminal.Gui.Views.MenuItem>
3. `Menu.OnSelectedMenuItemChanged()` runs: hides peer SubMenus, shows selected item's SubMenu with basic positioning
4. `Menu.SelectedMenuItemChanged` event fires
5. When inside a <xref:Terminal.Gui.Views.PopoverMenu>, the subscriber adjusts SubMenu positioning for screen boundaries

### MenuItem Acceptance Flow

When a user presses Enter or clicks a <xref:Terminal.Gui.Views.MenuItem>:

1. <xref:Terminal.Gui.Input.Command.Accept> is invoked on the focused <xref:Terminal.Gui.Views.MenuItem>
2. `MenuItem.RaiseAccepting` fires the cancellable <xref:Terminal.Gui.ViewBase.View.Accepting> event
3. If not cancelled, `Shortcut.OnAccepted` runs:
   - If `TargetView` and `Command` are set: invokes the command on the target view
   - If `Action` is set: invokes the action
4. <xref:Terminal.Gui.ViewBase.View.Accepted> fires on the MenuItem
5. Because `Menu.CommandsToBubbleUp` includes <xref:Terminal.Gui.Input.Command.Accept>, the command bubbles up:
   - MenuItem → Menu → PopoverMenu
6. PopoverMenu hides (closes) in response to the accepted command
7. The `CommandBridge` on MenuBarItem brings the event into the containment hierarchy
8. MenuBar deactivates

### Keyboard Navigation

| Key | Action |
|-----|--------|
| `F10` | Toggle MenuBar activation |
| `Shift+F10` | Show context PopoverMenu |
| `↑` / `↓` | Navigate within Menu |
| `←` / `→` | Navigate MenuBar items / Expand-collapse submenus |
| `Enter` | Accept selected MenuItem |
| `Space` | Activate selected MenuItem (e.g., toggle a CheckBox CommandView) |
| `Escape` / `Application.GetDefaultKey (Command.Quit)` | Close menu / Deactivate MenuBar |
| Hotkey (e.g., `Alt+F`) | Activate/toggle specific MenuBarItem |
| Hotkey in Menu | Jump to MenuItem with matching hotkey |

---

## Usage Examples

### Basic MenuBar

```csharp
using Terminal.Gui;

Application.Init ();

Window mainWindow = new () { Title = "Menu Demo" };

MenuBar menuBar = new ([
    new MenuBarItem ("_File", [
        new MenuItem ("_New", "", () => MessageBox.Query ("New", "Create new file?", "OK", "Cancel")),
        new MenuItem ("_Open...", "", () => MessageBox.Query ("Open", "Open file dialog", "OK")),
        new Line (),
        new MenuItem ("E_xit", Application.GetDefaultKey (Command.Quit), () => Application.RequestStop ())
    ]),
    new MenuBarItem ("_Edit", [
        new MenuItem ("_Undo", Key.Z.WithCtrl, () => { }),
        new Line (),
        new MenuItem ("Cu_t", Key.X.WithCtrl, () => { }),
        new MenuItem ("_Copy", Key.C.WithCtrl, () => { }),
        new MenuItem ("_Paste", Key.V.WithCtrl, () => { })
    ])
]);

mainWindow.Add (menuBar);

Application.Run (mainWindow);
Application.Shutdown ();
```

### Nested Submenus

```csharp
MenuBarItem optionsMenu = new ("_Options", [
    new MenuItem
    {
        Title = "_Preferences",
        SubMenu = new Menu ([
            new MenuItem { Title = "_General", Action = () => ShowGeneralPrefs () },
            new MenuItem { Title = "_Editor", Action = () => ShowEditorPrefs () },
            new MenuItem
            {
                Title = "_Advanced",
                SubMenu = new Menu ([
                    new MenuItem { Title = "_Debug Mode", Action = () => ToggleDebug () },
                    new MenuItem { Title = "_Experimental", Action = () => ToggleExperimental () }
                ])
            }
        ])
    },
    new Line (),
    new MenuItem { Title = "_Reset to Defaults", Action = () => ResetDefaults () }
]);
```

### Command Binding

```csharp
// Bind menu items to commands on a target view
Editor editor = new () { X = 0, Y = 1, Width = Dim.Fill (), Height = Dim.Fill () };

MenuBar menuBar = new ([
    new MenuBarItem ("_Edit", [
        new MenuItem (editor, Command.Cut),      // Uses editor's Cut command
        new MenuItem (editor, Command.Copy),     // Uses editor's Copy command
        new MenuItem (editor, Command.Paste),    // Uses editor's Paste command
        new Line (),
        new MenuItem (editor, Command.SelectAll)
    ])
]);
```

### CheckBox in Menu

```csharp
// Set CommandView.CanFocus = false to ensure the MenuItem receives activation for the CheckBox
CheckBox wordWrapCheckBox = new () { Title = "_Word Wrap", CanFocus = false };
wordWrapCheckBox.CheckedStateChanging += (_, e) =>
{
    editor.WordWrap = e.NewValue == CheckState.Checked;
};

MenuBarItem viewMenu = new ("_View", [
    new MenuItem { CommandView = wordWrapCheckBox },
    new MenuItem
    {
        CommandView = new CheckBox { Title = "_Line Numbers" },
        Key = Key.L.WithCtrl
    }
]);
```

### Context Menu (PopoverMenu)

```csharp
PopoverMenu contextMenu = new ([
    new MenuItem (editor, Command.Cut),
    new MenuItem (editor, Command.Copy),
    new MenuItem (editor, Command.Paste),
    new Line (),
    new MenuItem { Title = "_Properties...", Action = () => ShowProperties () }
]);

Application.Popover?.Register (contextMenu);

// Show on right-click
editor.MouseClick += (_, e) =>
{
    if (e.Flags.HasFlag (MouseFlags.RightButtonClicked))
    {
        contextMenu.MakeVisible (e.ScreenPosition);
        e.Handled = true;
    }
};
```

---

## Event Flow

### Command Propagation Through the Menu Hierarchy

When a <xref:Terminal.Gui.Views.MenuItem> is activated or accepted, commands propagate through the hierarchy using two mechanisms: **command bubbling** (within the containment hierarchy) and **CommandBridge** (across non-containment boundaries).

#### Accept Flow (e.g., user presses Enter on a MenuItem)

```
MenuItem
  ├─ Accepting event (cancellable)
  ├─ Accepted event
  ↓ (bubbles via CommandsToBubbleUp)
Menu (Root)
  ├─ Accepting event
  ├─ Accepted event
  ↓ (PopoverMenu receives via containment)
PopoverMenu
  ├─ Closes (Visible = false)
  ↓ (CommandBridge bridges to MenuBarItem)
MenuBarItem
  ├─ PopoverMenuOpen = false (via VisibleChanged)
  ↓ (bubbles to SuperView)
MenuBar
  ├─ Active = false
  └─ Deactivates
```

#### Activate Flow (e.g., user presses Space to toggle a CheckBox in a MenuItem)

```
MenuItem's CommandView (e.g., CheckBox)
  ├─ Activating / Activated events
  ↓ (relay dispatch from Shortcut)
MenuItem
  ├─ Activating / Activated events
  ↓ (bubbles via CommandsToBubbleUp)
Menu (Root / ContentView)
  ├─ Activated event
  ↓ (ContentView CommandBridge bridges to PopoverMenu)
PopoverMenu
  ├─ OnActivating hides popover for non-HotKey bridged commands
  ├─ Activated event
  ↓ (Target CommandBridge bridges to MenuBarItem)
MenuBarItem
  ├─ OnActivating sees Bridged routing → ignores (no toggle)
  ↓ (bubbles to SuperView)
MenuBar
  ├─ OnActivating sees BubblingUp routing → notification only
```

### Selection Change Events

```
User navigates → Menu.Focused changes
                        ↓
              Menu.OnFocusedChanged ()
                        ↓
              SelectedMenuItem updated
                        ↓
              Menu.OnSelectedMenuItemChanged ()
                ├─ Hides peer SubMenus
                ├─ Shows selected SubMenu (with basic positioning)
                        ↓
              SelectedMenuItemChanged event
                        ↓
              PopoverMenu adjusts SubMenu positioning (screen boundaries)
```

### Key Event Processing

Key events are processed depth-first through the view hierarchy:

```
NewKeyDownEvent (key)
  ├─ If has Focused SubView → recurse into Focused
  ├─ RaiseKeyDown (key) — OnKeyDown + KeyDown event
  ├─ InvokeCommandsBoundToKey (key) — KeyBindings lookup
  ├─ InvokeCommandsBoundToHotKey (key) — HotKeyBindings (this + SubViews)
  └─ RaiseKeyDownNotHandled (key) — OnKeyDownNotHandled + event
```

For menus specifically:
- <xref:Terminal.Gui.Views.MenuBar> binds `F10` to <xref:Terminal.Gui.Input.Command.HotKey> (via `HotKeyBindings`)
- <xref:Terminal.Gui.Views.MenuBar> binds `F10` and `Application.GetDefaultKey (Command.Quit)` to `Command.Quit` (via `KeyBindings`)
- <xref:Terminal.Gui.Views.MenuBar> binds arrow keys to `Command.Right`/`Command.Left`
- <xref:Terminal.Gui.Views.PopoverMenu> binds arrow keys to `Command.Right`/`Command.Left` for submenu navigation
- <xref:Terminal.Gui.Views.PopoverMenu> binds `Escape`/quit key to `Command.Quit`

---

## Configuration

Menu appearance can be customized via themes:

```csharp
// Menu appearance is configured via Settings POCOs (or config.json).
MenuSettings.Current = MenuSettings.Current with { DefaultBorderStyle = LineStyle.Single };
MenuBarSettings.Current = MenuBarSettings.Current with { DefaultBorderStyle = LineStyle.None };

// Activation keys remain settable on the view facades:
MenuBar.DefaultKey = Key.F10;
PopoverMenu.DefaultKey = Key.F10.WithShift;
```

These can also be configured in `config.json`:

```json
{
  "MenuBar": {
    "DefaultKey": "F10"
  },
  "PopoverMenu": {
    "DefaultKey": "Shift+F10"
  },
  "Themes": {
    "Default": {
      "Menu": { "DefaultBorderStyle": "Single" },
      "MenuBar": { "DefaultBorderStyle": "None" }
    }
  }
}
```

---

## See Also

- [Popovers Deep Dive](Popovers.md) - Complete details on popover lifecycle
- [Command Deep Dive](command.md) - Command binding, dispatch, and routing
- [Keyboard Deep Dive](keyboard.md) - Key binding system
- [Events Deep Dive](events.md) - Event handling patterns
- <xref:Terminal.Gui.Views.MenuBar> API Reference
- <xref:Terminal.Gui.Views.PopoverMenu> API Reference
- <xref:Terminal.Gui.Views.MenuItem> API Reference
