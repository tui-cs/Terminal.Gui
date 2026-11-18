# Cursor Management in Terminal.Gui

## Overview

Terminal.Gui provides a unified cursor management system that separates the **Terminal Cursor** (the visible cursor indicator users see) from the **Draw Cursor** (internal rendering state).

**Key Concepts:**
- **Terminal Cursor**: The visible, blinking cursor indicator that shows where user input will go
- **Draw Cursor**: Internal position (`IOutputBuffer.Col/Row`) for rendering - NOT related to Terminal Cursor
- **Cursor Class**: Immutable record consolidating position (screen coordinates) and style
- **CursorStyle Enum**: ANSI-first cursor shape definitions

## The Cursor Class

Terminal.Gui uses a <xref:Terminal.Gui.Drivers.Cursor> record class to represent cursor state:

```csharp
public record Cursor
{
    /// <summary>Position in screen coordinates. Null = hidden.</summary>
    public Point? Position { get; init; }
    
    /// <summary>Cursor visual style (blinking bar, block, etc.)</summary>
    public CursorStyle Style { get; init; } = CursorStyle.Hidden;
    
    /// <summary>True if cursor should be visible</summary>
    public bool IsVisible => Position.HasValue && Style != CursorStyle.Hidden;
}
```

**Key characteristics:**
- Immutable value type (`record`)
- Position always in screen coordinates
- Null position means cursor is hidden
- Use `with` expression to modify: `cursor with { Position = newPos }`

## CursorStyle Enum

The <xref:Terminal.Gui.Drivers.CursorStyle> enum is based on ANSI/VT DECSCUSR terminal standards:

```csharp
public enum CursorStyle
{
    Default = 0,           // Implementation-defined (usually BlinkingBlock)
    BlinkingBlock = 1,     // █ (blinking)
    SteadyBlock = 2,       // █ (steady)
    BlinkingUnderline = 3, // _ (blinking)
    SteadyUnderline = 4,   // _ (steady)
    BlinkingBar = 5,       // | (blinking, common in text editors)
    SteadyBar = 6,         // | (steady)
    Hidden = -1            // No visible cursor
}
```

**Platform Mapping:**
- **ANSI Terminals**: Maps directly to DECSCUSR escape sequences (CSI Ps SP q)
- **Windows Console**: Drivers convert to CONSOLE_CURSOR_INFO (bVisible, dwSize)

## Setting the Cursor in Views

Views use the <xref:Terminal.Gui.ViewBase.View.Cursor> property to manage cursor state:

### Basic Usage

```csharp
// Set cursor at column 5, row 0 in viewport - convert to screen coords
Point screenPos = ViewportToScreen (new Point (5, 0));
Cursor = new Cursor { Position = screenPos, Style = CursorStyle.BlinkingBar };

// Hide cursor
Cursor = new Cursor { Position = null };
// or
Cursor = new Cursor { Style = CursorStyle.Hidden };

// Update position keeping same style
Point newScreenPos = ViewportToScreen (new Point (6, 0));
Cursor = Cursor with { Position = newScreenPos };
```

### <xref:Terminal.Gui.Views.TextField> Example

```csharp
protected override void OnDrawContent (Rectangle viewport)
{
    // Calculate cursor position in content coordinates
    int cursorCol = _cursorPosition - _scrollOffset;
    
    // Only set cursor if within viewport
    if (cursorCol >= 0 && cursorCol < Viewport.Width && HasFocus)
    {
        Point screenPos = ViewportToScreen (new Point (cursorCol, 0));
        Cursor = new Cursor 
        { 
            Position = screenPos, 
            Style = CursorStyle.BlinkingBar 
        };
    }
    else
    {
        // Cursor outside viewport or no focus - hide it
        Cursor = new Cursor { Position = null };
    }
    
    // ... drawing code ...
}
```

## Coordinate Systems

**CRITICAL**: `Cursor.Position` must ALWAYS be in screen coordinates.

Views have three coordinate systems:
1. **Content Area**: View's internal coordinates (e.g., document position in Editor)
2. **Viewport**: Visible portion of content (accounting for scrolling)
3. **Screen**: Absolute terminal screen coordinates

**Always convert before setting cursor:**

```csharp
// From content area
Point contentPos = new Point (column, row);
Point screenPos = ContentToScreen (contentPos);
Cursor = new Cursor { Position = screenPos, Style = CursorStyle.BlinkingBar };

// From viewport
Point viewportPos = new Point (column, row);
Point screenPos = ViewportToScreen (viewportPos);
Cursor = new Cursor { Position = screenPos, Style = CursorStyle.BlinkingBar };
```

## Cursor Visibility Rules

The framework automatically hides the cursor when:
1. `view.Enabled == false`
2. `view.Visible == false`
3. <xref:Terminal.Gui.ViewBase.View.CanFocus> `== false`
4. <xref:Terminal.Gui.ViewBase.View.HasFocus> `== false`
5. View is not the most focused view (not deepest in focus chain)
6. Cursor position is outside any ancestor viewport bounds

Views only need to:
- Set `Cursor.Position` when they want the cursor visible
- Use `null` or `CursorStyle.Hidden` to hide
- Convert coordinates to screen space

## Efficient Cursor Updates

### [SetCursorNeedsUpdate()](xref:Terminal.Gui.ViewBase.View.SetCursorNeedsUpdate*)

When cursor position changes without requiring a full redraw, use:

```csharp
public void SetCursorNeedsUpdate ()
{
    App?.Driver?.SetCursorNeedsUpdate (true);
}
```

This signals the driver that cursor position needs updating on next iteration without triggering view redraw.

**When to use:**
- Cursor moves but view content unchanged
- Blinking/animation of cursor
- Focus changes

**When NOT to use:**
- View content changed (use <xref:Terminal.Gui.ViewBase.View.NeedsDraw> instead)
- Layout changed

### Example: Cursor Movement

```csharp
private void MoveCursorRight ()
{
    _cursorPosition++;
    
    // Calculate new screen position
    int viewportCol = _cursorPosition - _scrollOffset;
    
    if (viewportCol >= 0 && viewportCol < Viewport.Width)
    {
        Point screenPos = ViewportToScreen (new Point (viewportCol, 0));
        Cursor = Cursor with { Position = screenPos };
        SetCursorNeedsUpdate (); // Efficient - no redraw needed
    }
}
```

## Implementation Details

### ApplicationNavigation

The `ApplicationNavigation` class manages cursor positioning at the application level. Called once per main loop iteration, it:

1. **Checks update flag** - Exits early if `GetCursorNeedsUpdate()` returns false (optimization)
2. **Gets most focused view** - Retrieves `TopRunnableView.MostFocused` 
3. **Validates visibility** - Hides cursor if no focused view or cursor not visible
4. **Validates position** - Walks ancestor chain to ensure cursor position is within all viewport bounds
5. **Delegates to driver** - Calls `Driver.SetCursor()` with the final cursor state
6. **Clears the update flag** - Only when the output actually wrote the cursor. An output can skip the write (inline placement unknown, resize pending) or swallow an I/O error; the flag then stays set and the next iteration retries. Focus changes, `Cursor` changes, `Refresh` and screen resizes set it again.

This ensures only the deepest focused view's cursor is displayed, and only when visible within the view hierarchy.

### Driver Architecture

Drivers implement cursor control through `IDriver.SetCursor(Cursor)` which delegates to `IOutput.SetCursor(Cursor)`. The driver also tracks an update flag via `GetCursorNeedsUpdate()` / `SetCursorNeedsUpdate()` to avoid redundant cursor updates.

### Platform Implementations

Each driver implements cursor control based on platform capabilities:

#### WindowsOutput

Windows supports two modes based on terminal capabilities:

**Legacy Console Mode** (pre-Windows 10 or conhost compatibility mode):
- Uses Win32 `CONSOLE_CURSOR_INFO` structure with P/Invoke
- Maps <xref:Terminal.Gui.Drivers.CursorStyle> to cursor size percentage (Block → 100%, Underline/Bar → 15%)
- Cannot distinguish between blinking and steady styles

**Modern VT Mode** (Windows Terminal, modern ConHost):
- Uses ANSI escape sequences identical to Unix drivers
- Full <xref:Terminal.Gui.Drivers.CursorStyle> support via DECSCUSR

#### UnixOutput / AnsiOutput / NetOutput

All use pure ANSI escape sequences:
- **DECTCEM** (`CSI ? 25 h/l`) for show/hide
- **DECSCUSR** (`CSI Ps SP q`) for cursor style
- **CUP** (`CSI row;col H`) for positioning

Only emits style change sequences when the style actually changes (optimization).

### ANSI Escape Sequences Reference

| Sequence | Description |
|----------|-------------|
| `CSI ? 25 h` | DECTCEM - Show cursor |
| `CSI ? 25 l` | DECTCEM - Hide cursor |
| `CSI Ps SP q` | DECSCUSR - Set cursor style (Ps = 0-6) |
| `CSI row ; col H` | CUP - Set cursor position |

**CursorStyle to DECSCUSR Mapping:**

| CursorStyle | DECSCUSR Ps | Appearance |
|-------------|-------------|------------|
| `Default` | 0 | Implementation-defined (usually blinking block) |
| `BlinkingBlock` | 1 | █ blinking |
| `SteadyBlock` | 2 | █ steady |
| `BlinkingUnderline` | 3 | _ blinking |
| `SteadyUnderline` | 4 | _ steady |
| `BlinkingBar` | 5 | \| blinking |
| `SteadyBar` | 6 | \| steady |
| `Hidden` | N/A | Uses DECTCEM hide instead |

### Cursor Update Flow

The cursor is updated once per main loop iteration:

```
┌──────────────────────────────────────────────────────────┐
│                  Main Loop Iteration                      │
├──────────────────────────────────────────────────────────┤
│ 1. Process input events                                  │
│ 2. Execute callbacks                                     │
│ 3. Layout pass (if needed)                               │
│ 4. Draw pass (if needed)                                 │
│    └─ Cursor hidden during draw to prevent flicker       │
│ 5. ApplicationNavigation.UpdateCursor()                  │
│    └─ Checks GetCursorNeedsUpdate()                      │
│    └─ Gets MostFocused view's Cursor                     │
│    └─ Validates position within ancestor viewports       │
│    └─ Calls Driver.SetCursor(cursor)                     │
└──────────────────────────────────────────────────────────┘
```

## Common Patterns

### Text Editor Pattern

Views that display text with a visible insertion point should:
1. Track cursor position in content coordinates
2. Check if cursor is within visible viewport bounds
3. Convert to screen coordinates using [ViewportToScreen()](xref:Terminal.Gui.ViewBase.View.ViewportToScreen*)
4. Set <xref:Terminal.Gui.ViewBase.View.Cursor> property with appropriate style (typically `BlinkingBar`)
5. Hide cursor when out of viewport bounds or unfocused

### List Selection Pattern

Views using highlight-based selection (like <xref:Terminal.Gui.Views.ListView>) should hide the cursor:

```csharp
Cursor = new Cursor { Position = null };
```

The selection is indicated visually through attribute changes, not the terminal cursor.

## Migration from Old API

**Before** (v1 / early v2): `PositionCursor()` override returned viewport-relative coordinates.

**After** (current v2): Set <xref:Terminal.Gui.ViewBase.View.Cursor> property with screen-absolute coordinates during `OnDrawContent()`.

Key changes:
- No more `PositionCursor()` override
- Cursor set during drawing, not in separate method
- Must convert to screen coordinates explicitly
- Use [ViewportToScreen()](xref:Terminal.Gui.ViewBase.View.ViewportToScreen*) for conversion

## Key Differences from Draw Cursor

**Terminal Cursor** vs **Draw Cursor** are completely separate:

| Aspect | Terminal Cursor | Draw Cursor |
|--------|----------------|-------------|
| Purpose | Show user where input goes | Track where next character renders |
| API | <xref:Terminal.Gui.ViewBase.View.Cursor> property | `IOutputBuffer.Col/Row` |
| Affected by | `Cursor = new Cursor { ... }` | `Move()`, `AddRune()`, `AddStr()` |
| Visibility | User sees blinking cursor | Internal only |
| Coordinates | Screen-absolute | Buffer-relative |
| Management | View sets explicitly | Rendering system updates |

**❌ NEVER do this:**
```csharp
// WRONG - Don't use Move() for cursor positioning
Move (cursorCol, cursorRow);  // This affects Draw Cursor, not Terminal Cursor
```

**✅ DO this:**
```csharp
// CORRECT - Use Cursor property
Point screenPos = ViewportToScreen (new Point (cursorCol, cursorRow));
Cursor = new Cursor { Position = screenPos, Style = CursorStyle.BlinkingBar };
```

## Best Practices

1. **Always use screen coordinates** for `Cursor.Position`
2. **Always convert** from content/viewport to screen before setting
3. **Use [SetCursorNeedsUpdate()](xref:Terminal.Gui.ViewBase.View.SetCursorNeedsUpdate*)** for position-only changes (no redraw needed)
4. **Set to null** to hide cursor, don't use visibility tricks
5. **Never call `Move()`** for cursor positioning (it affects Draw Cursor)
6. **Don't access Driver directly** from views - use <xref:Terminal.Gui.ViewBase.View.Cursor> property
7. **Test with viewport scrolling** to ensure coordinates are correct
8. **Prefer immutable updates** using `with` expression

## Troubleshooting

### Cursor not visible

**Check:**
1. Is `Cursor.Position` set to a valid screen coordinate (not null)?
2. Is `Cursor.Style` something other than `Hidden`?
3. Does view have <xref:Terminal.Gui.ViewBase.View.HasFocus> `== true`?
4. Is view the most focused (deepest in focus chain)?
5. Is cursor position within all ancestor viewports?
6. Is view `Enabled` and `Visible`?

### Cursor in wrong position

**Check:**
1. Are you using screen coordinates (not content/viewport)?
2. Did you call [ViewportToScreen()](xref:Terminal.Gui.ViewBase.View.ViewportToScreen*) or [ContentToScreen()](xref:Terminal.Gui.ViewBase.View.ContentToScreen*)?
3. Is viewport scrolling accounted for?
4. Are ancestor viewports considered?

### Cursor flickers

**Check:**
1. Are you calling `SetNeedsDraw()` when only cursor moved? (Use [SetCursorNeedsUpdate()](xref:Terminal.Gui.ViewBase.View.SetCursorNeedsUpdate*) instead)
2. Is cursor being set/unset multiple times per frame?
3. Is cursor style changing unnecessarily?

## See Also

- [Navigation and Focus](navigation.md) - Focus management and view hierarchy
- [Drivers](drivers.md) - Driver architecture and platform abstraction
- [View Deep Dive](View.md) - View class and hierarchy
- [Events](events.md) - Keyboard and mouse event handling
