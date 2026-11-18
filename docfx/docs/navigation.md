# Navigation Deep Dive

This document covers Terminal.Gui's navigation system, which determines:

- What are the visual cues that help the user know which element of an application is receiving keyboard and mouse input (which one has focus)? 
- How does the user change which element of an application has focus?
- What are the visual cues that help the user know what keystrokes will change the focus?
- What are the visual cues that help the user know what keystrokes will cause action in elements of the application that don't currently have focus?
- What is the order in which UI elements are traversed when using keyboard navigation?
- What are the default actions for standard key/mouse input (e.g. <xref:Terminal.Gui.Input.Command.HotKey>, `Space`, `Enter`, or a mouse click)?

## See Also

* [Keyboard Deep Dive](keyboard.md)
* [Mouse Deep Dive](mouse.md)
* [Cursor Management](cursor.md)
* [Lexicon & Taxonomy](lexicon.md)

## Lexicon & Taxonomy

[!INCLUDE [Navigation Lexicon](~/includes/navigation-lexicon.md)]

## Tenets for Terminal.Gui UI Navigation (Unless you know better ones...)

See the [Keyboard Tenets](keyboard.md) as they apply as well.

Tenets higher in the list have precedence over tenets lower in the list.

* **One Focus Per App** - It should not be possible to have two views be the "most focused" view in an application. There is always exactly one view that is the target of keyboard input.

* **There's Always a Way With The Keyboard** - The framework strives to ensure users wanting to use the keyboard can't get into a situation where some element of the application is not accessible via the keyboard. For example, we have unit tests that ensure built-in Views will all have at least one navigation key that advances focus. Another example: As long as a View with a HotKey is visible and enabled, regardless of view-hierarchy, if the user presses that hotkey, the action defined by the hotkey will happen (and, by default the View that defines it will be focused). 

* **Flexible Overrides** - The framework makes it easy for navigation changes to be made from code and enables changing of behavior to be done in flexible ways. For example a view can be prevented from getting focus by setting <xref:Terminal.Gui.ViewBase.View.CanFocus> to `false` or overriding `OnHasFocusChanging` and returning `true` to cancel.

* **Decouple Concepts** - In v1 <xref:Terminal.Gui.ViewBase.View.CanFocus> is tightly coupled with <xref:Terminal.Gui.ViewBase.View.HasFocus>, `TabIndex`, `TabIndexes`, and <xref:Terminal.Gui.ViewBase.View.TabStop> and vice-versa. There was a bunch of "magic" logic that automatically attempted to keep these concepts aligned. This resulted in a poorly specified, hard-to-test, and fragile API. In v2 we strive to keep the related navigation concepts decoupled. For example, <xref:Terminal.Gui.ViewBase.View.CanFocus> and <xref:Terminal.Gui.ViewBase.View.TabStop> are decoupled. A view with `CanFocus == true` can have `TabStop == NoStop` and still be focusable with the mouse.

## Answering the Key Navigation Questions

### Visual Cues for Focus

**Current Focus Indicator:**
- Views with focus are rendered using their `ColorScheme.Focus` attribute
- The most focused view (deepest in focus chain) may display a terminal cursor via `View.Cursor`
- Views in the focus chain (SuperViews of the focused view) also use focused styling
- Only one terminal cursor is displayed at a time, managed by <xref:Terminal.Gui.App.ApplicationNavigation>

**Navigation Cues:**
- HotKeys are indicated by underlined characters in <xref:Terminal.Gui.Views.Label>s, <xref:Terminal.Gui.Views.Button>s, and <xref:Terminal.Gui.Views.MenuItem>s
- Tab order is generally left-to-right, top-to-bottom within containers
- Focus indicators (such as highlight rectangles) show which view will receive input

> [!TIP]
> See [Cursor Management](cursor.md) for details on how views control cursor position and style.

### Changing Focus

**Keyboard Methods:**
- `Tab` / `Shift+Tab` - Navigate between TabStop views
- `F6` / `Shift+F6` - Navigate between TabGroup containers  
- Arrow keys - Navigate within containers or between adjacent views
- HotKeys - Direct navigation to specific views (Alt+letter combinations)
- `Enter` / `Space` - Activate the focused view

**Mouse Methods:**
- Click on any focusable view to give it focus
- Focus behavior depends on whether the view was previously focused (RestoreFocus vs AdvanceFocus)

### Navigation Order

Views are traversed based on their <xref:Terminal.Gui.ViewBase.View.TabStop> behavior and position in the view hierarchy:

1. **TabStop Views** - Navigated with Tab/Shift+Tab in layout order
2. **TabGroup Views** - Containers navigated with F6/Shift+F6
3. **NoStop Views** - Skipped during keyboard navigation but can receive mouse focus

## Keyboard Navigation

The majority of the Terminal.Gui Navigation system is dedicated to enabling the keyboard to be used to navigate Views. 

Terminal.Gui defines these keys for keyboard navigation:

- `Application.GetDefaultKey (Command.NextTabStop)` (`Key.Tab`) - Navigates to the next subview that is a <xref:Terminal.Gui.ViewBase.TabBehavior.TabStop> (see below). If there is no next, the first subview that is a <xref:Terminal.Gui.ViewBase.TabBehavior.TabStop> will gain focus.
- `Application.GetDefaultKey (Command.PreviousTabStop)` (`Key.Tab.WithShift`) - Opposite of `NextTabStop`.
- `Key.CursorRight` - Operates identically to `NextTabStop`.
- `Key.CursorDown` - Operates identically to `NextTabStop`.
- `Key.CursorLeft` - Operates identically to `PreviousTabStop`.
- `Key.CursorUp` - Operates identically to `PreviousTabStop`.
- `Application.GetDefaultKey (Command.NextTabGroup)` (`Key.F6`) - Navigates to the next view in the view-hierarchy that is a <xref:Terminal.Gui.ViewBase.TabBehavior.TabGroup> (see below). If there is no next, the first view that is a <xref:Terminal.Gui.ViewBase.TabBehavior.TabGroup> will gain focus.
- `Application.GetDefaultKey (Command.PreviousTabGroup)` (`Key.F6.WithShift`) - Opposite of `NextTabGroup`.

`F6` was chosen to match [Windows](https://learn.microsoft.com/en-us/windows/apps/design/input/keyboard-accelerators#common-keyboard-accelerators) conventions.

These keys are all registered as `KeyBindingScope.Application` key bindings by <xref:Terminal.Gui.App.Application>. Because application-scoped key bindings have the lowest priority, Views can override the behaviors of these keys (e.g. <xref:Terminal.Gui.Views.Editor> overrides `Key.Tab` by default, enabling the user to enter `\t` into text). The `AllViews_AtLeastOneNavKey_Leaves` unit test ensures all built-in Views have at least one of the above keys that can advance focus.

### Navigation Examples

```csharp
// Basic focus management
var button = new Button() { Text = "Click Me", CanFocus = true, TabStop = TabBehavior.TabStop };
var textField = new TextField() { Text = "", CanFocus = true, TabStop = TabBehavior.TabStop };

// Container with group navigation
var frameView = new FrameView() 
{ 
    Title = "Options",
    CanFocus = true, 
    TabStop = TabBehavior.TabGroup 
};

// Programmatic focus control
button.SetFocus(); // Give focus to specific view
Application.Navigation.AdvanceFocus(NavigationDirection.Forward, TabBehavior.TabStop);
```

### HotKeys

See also [Keyboard](keyboard.md) where HotKey is covered more deeply...

<xref:Terminal.Gui.ViewBase.View.HotKey>s can be used to navigate across the entire application view-hierarchy. They work independently of `Focus`. This enables a user to navigate across a complex UI of nested subviews if needed (even in overlapped scenarios). An example use case is the `AllViewsTester` Scenario.

HotKeys are defined using the <xref:Terminal.Gui.ViewBase.View.HotKey> property and are activated using `Alt+` the specified key:

```csharp
var saveButton = new Button() { Text = Strings.cmdSave, HotKey = Key.S };
var exitButton = new Button() { Text = "E_xit", HotKey = Key.X };

// Alt+S will activate save, Alt+X will activate exit, regardless of current focus
```

Additionally, multiple Views in an application (even within the same SuperView) can have the same <xref:Terminal.Gui.ViewBase.View.HotKey>.

## Mouse Navigation

Mouse-based navigation is straightforward in comparison to keyboard: If a view is focusable and the user clicks on it, it gains focus. There are some nuances, though:

- If a <xref:Terminal.Gui.ViewBase.View> is focusable, and it has focusable sub-views, what happens when a user clicks on the `Border` of the <xref:Terminal.Gui.ViewBase.View>? Which sub-view (if any) will also get focus?

- If a <xref:Terminal.Gui.ViewBase.View> is focusable, and it has focusable sub-views, what happens when a user clicks on the `ContentArea` of the <xref:Terminal.Gui.ViewBase.View>? Which sub-view (if any) will also get focus?

The answer to both questions is:

If the <xref:Terminal.Gui.ViewBase.View> was previously focused, the system keeps a record of the SubView that was previously most-focused and restores focus to that SubView (`RestoreFocus()`).

If the <xref:Terminal.Gui.ViewBase.View> was not previously focused, `AdvanceFocus()` is called to find the next appropriate focus target.

For this to work properly, there must be logic that removes the focus-cache used by `RestoreFocus()` if something changes that makes the previously-focusable view not focusable (e.g. if Visible has changed).

### Mouse Focus Examples

```csharp
// Mouse click behavior
view.MouseEvent += (sender, e) =>
{
    if (e.Flags.HasFlag(MouseFlags.LeftButtonClicked) && view.CanFocus)
    {
        view.SetFocus();
        e.Handled = true;
    }
};

// Focus on mouse enter (optional behavior)
view.MouseEnter += (sender, e) =>
{
    if (view.CanFocus && focusOnHover)
    {
        view.SetFocus();
    }
};
```

## Application Level Navigation

At the application level, navigation is encapsulated within the <xref:Terminal.Gui.App.ApplicationNavigation> helper class which is publicly exposed via the `Application.Navigation` property.

`ApplicationNavigation.GetFocused()` gets the most-focused <xref:Terminal.Gui.ViewBase.View> in the application. Will return `null` if there is no view with focus (an extremely rare situation). This replaces `View.MostFocused` in v1.

The `ApplicationNavigation.FocusedChanged` and `ApplicationNavigation.FocusedChanging` events are raised when the most-focused <xref:Terminal.Gui.ViewBase.View> in the application is changing or has changed. `FocusedChanged` is useful for apps that want to do something with the most-focused view (e.g. see `AdornmentsEditor`). `FocusChanging` is useful for apps that want to override what view can be focused across an entire app.

The `ApplicationNavigation.AdvanceFocus()` method causes the focus to advance (forward or backwards) to the next <xref:Terminal.Gui.ViewBase.View> in the application view-hierarchy, using `behavior` as a filter.

The implementation is simple:

```cs
return app.Current?.AdvanceFocus (direction, behavior);
```

This method is called from the <xref:Terminal.Gui.Input.Command> handlers bound to the application-scoped keybindings created during `app.Init()`. It is `public` as a convenience.

**Note:** When accessing from within a View, use `App?.Current` instead of `Application.TopRunnable` (which is obsolete).

This method replaces about a dozen functions in v1 (scattered across <xref:Terminal.Gui.App.Application> and `Runnable`).

### Cursor Management

<xref:Terminal.Gui.App.ApplicationNavigation> also manages the terminal cursor. Each main loop iteration, `UpdateCursor()` is called to:

1. Check if cursor update is needed (optimization via `GetCursorNeedsUpdate()`)
2. Get the most focused view's `Cursor` property
3. Validate the cursor position is within all ancestor viewport bounds
4. Delegate to the driver via `SetCursor()`

The cursor is only displayed for the **most focused view** (deepest in the focus chain). When focus changes, the cursor automatically updates to reflect the new focused view's cursor state.

Views control their cursor through the `View.Cursor` property, not through <xref:Terminal.Gui.App.ApplicationNavigation> directly.

> [!NOTE]
> See [Cursor Management](cursor.md) for complete details on how views should set cursor position and style.

### Application Navigation Examples

```csharp
var app = Application.Create();
app.Init();

// Listen for global focus changes
app.Navigation.FocusedChanged += (sender, e) => 
{
    var focused = app.Navigation.GetFocused();
    StatusBar.Text = $"Focused: {focused?.GetType().Name ?? "None"}";
};

// Prevent certain views from getting focus
app.Navigation.FocusedChanging += (sender, e) => 
{
    if (e.NewView is SomeRestrictedView)
    {
        e.Cancel = true; // Prevent focus change
    }
};

// Programmatic navigation
Application.Navigation.AdvanceFocus(NavigationDirection.Forward, TabBehavior.TabStop);
Application.Navigation.AdvanceFocus(NavigationDirection.Backward, TabBehavior.TabGroup);
```

## View Level Navigation

`AdvanceFocus()` is the primary method for developers to cause a <xref:Terminal.Gui.ViewBase.View> to gain or lose focus.

Various events are raised when a <xref:Terminal.Gui.ViewBase.View>'s focus is changing. For example, <xref:Terminal.Gui.ViewBase.View.HasFocusChanging> and <xref:Terminal.Gui.ViewBase.View.HasFocusChanged>.

### View Focus Management

```csharp
// Basic focus control
public class CustomView : View
{
    protected override void OnHasFocusChanging(CancelEventArgs<bool> e)
    {
        if (SomeCondition)
        {
            e.Cancel = true; // Prevent focus change
            return;
        }
        base.OnHasFocusChanging(e);
    }

    protected override void OnHasFocusChanged(EventArgs<bool> e)
    {
        if (e.CurrentValue)
        {
            // View gained focus
            UpdateAppearance();
        }
        base.OnHasFocusChanged(e);
    }
}
```

## What makes a View focusable?

First, only Views that are visible and enabled can gain focus. Both `Visible` and `Enabled` must be `true` for a view to be focusable.

For visible and enabled Views, the <xref:Terminal.Gui.ViewBase.View.CanFocus> property is then used to determine whether the <xref:Terminal.Gui.ViewBase.View> is focusable. <xref:Terminal.Gui.ViewBase.View.CanFocus> must be `true` for a <xref:Terminal.Gui.ViewBase.View> to gain focus. However, even if <xref:Terminal.Gui.ViewBase.View.CanFocus> is `true`, other factors can prevent the view from gaining focus...

A visible, enabled, and `CanFocus == true` view can be focused if the user uses the mouse to clicks on it or if code explicitly calls [SetFocus()](xref:Terminal.Gui.ViewBase.View.SetFocus*). Of course, the view itself or some other code can cancel the focus (e.g. by overriding `OnHasFocusChanging`).

For keyboard navigation, the <xref:Terminal.Gui.ViewBase.View.TabStop> property is a filter for which views are focusable from the current most-focused. <xref:Terminal.Gui.ViewBase.View.TabStop> has no impact on mouse navigation. <xref:Terminal.Gui.ViewBase.View.TabStop> is of type <xref:Terminal.Gui.ViewBase.TabBehavior>.

### TabBehavior Values

* `null` - This <xref:Terminal.Gui.ViewBase.View> is still being initialized; acts as a signal to `set_CanFocus` to set <xref:Terminal.Gui.ViewBase.View.TabStop> to <xref:Terminal.Gui.ViewBase.TabBehavior.TabStop> as convenience for the most common use-case. Equivalent to <xref:Terminal.Gui.ViewBase.TabBehavior.NoStop> when determining if a view is focusable by the keyboard or not.

* <xref:Terminal.Gui.ViewBase.TabBehavior.NoStop> - Prevents the user from using keyboard navigation to cause view (and by definition its subviews) to gain focus. Note: The view can still be focused using code or the mouse.

* <xref:Terminal.Gui.ViewBase.TabBehavior.TabStop> - Indicates a <xref:Terminal.Gui.ViewBase.View> is a focusable view with no focusable subviews. `NextTabStop`/`PreviousTabStop` keys will advance ONLY through the peer-Views (`SuperView.SubViews`).

* <xref:Terminal.Gui.ViewBase.TabBehavior.TabGroup> - Indicates a <xref:Terminal.Gui.ViewBase.View> is a focusable container for other focusable views and enables keyboard navigation across these containers. This applies to both tiled and overlapped views. For example, `FrameView` is a simple view designed to be a visible container of other views in tiled scenarios. It has <xref:Terminal.Gui.ViewBase.View.TabStop> set to <xref:Terminal.Gui.ViewBase.TabBehavior.TabGroup> (and `Arrangement` set to `ViewArrangement.Fixed`). Likewise, <xref:Terminal.Gui.Views.Window> is a simple view designed to be a visible container of other views in overlapped scenarios. It has <xref:Terminal.Gui.ViewBase.View.TabStop> set to <xref:Terminal.Gui.ViewBase.TabBehavior.TabGroup> (and `Arrangement` set to `ViewArrangement.Movable | ViewArrangement.Resizable | ViewArrangement.Overlapped`). `NextTabGroup`/`PreviousTabGroup` keys will advance across all <xref:Terminal.Gui.ViewBase.TabBehavior.TabGroup> views in the application (unless blocked by a <xref:Terminal.Gui.ViewBase.TabBehavior.NoStop> SuperView).

### Focus Requirements Summary

For a view to be focusable:

1. **Visible** = `true`
2. **Enabled** = `true`
3. <xref:Terminal.Gui.ViewBase.View.CanFocus> = `true`
4. <xref:Terminal.Gui.ViewBase.View.TabStop> != <xref:Terminal.Gui.ViewBase.TabBehavior.NoStop> (for keyboard navigation only)

```csharp
// Example: Make a view focusable
var view = new Label() 
{
    Text = "Focusable Label",
    Visible = true,     // Must be visible
    Enabled = true,     // Must be enabled
    CanFocus = true,    // Must be able to focus
    TabStop = TabBehavior.TabStop  // Keyboard navigable
};
```

## How To Tell if a View has focus? And which view is the most-focused?

<xref:Terminal.Gui.ViewBase.View.HasFocus> indicates whether the <xref:Terminal.Gui.ViewBase.View> is focused or not. It is the definitive signal. If the view has no focusable SubViews then this property also indicates the view is the most-focused view in the application.

Setting this property to `true` has the same effect as calling [SetFocus()](xref:Terminal.Gui.ViewBase.View.SetFocus*), which also means the focus may not change as a result.

If `v.HasFocus == true` then:

- All views up `v`'s superview-hierarchy must be focusable.
- All views up `v`'s superview-hierarchy will also have <xref:Terminal.Gui.ViewBase.View.HasFocus> `== true`.
- The deepest-subview of `v` that is focusable will also have <xref:Terminal.Gui.ViewBase.View.HasFocus> `== true`

In other words, `v.HasFocus == true` does not necessarily mean `v` is the most-focused view, receiving input. If it has focusable sub-views, one of those (or a further subview) will be the most-focused (`Application.Navigation.GetFocused()`).

The `private bool _hasFocus` field backs <xref:Terminal.Gui.ViewBase.View.HasFocus> and is the ultimate source of truth whether a <xref:Terminal.Gui.ViewBase.View> has focus or not.

### Focus Chain Example

```csharp
// In a hierarchy: Window -> Dialog -> Button
// If Button has focus, then:
window.HasFocus == true    // Part of focus chain
dialog.HasFocus == true    // Part of focus chain  
button.HasFocus == true    // Actually focused

// Application.Navigation.GetFocused() returns button
var mostFocused = Application.Navigation.GetFocused(); // Returns button
```

### How does a user tell?

In short: `ColorScheme.Focus` - Views in the focus chain render with focused colors.

Views use their `ColorScheme.Focus` attribute when they are part of the focus chain. This provides visual feedback about which part of the application is active.

```csharp
// Custom focus styling
protected override void OnDrawContent(Rectangle viewport)
{
    var attribute = HasFocus ? GetFocusColor() : GetNormalColor();
    Driver.SetAttribute(attribute);
    // ... draw content
}
```

## How to make a View become focused?

The primary `public` method for developers to cause a view to get focus is [SetFocus()](xref:Terminal.Gui.ViewBase.View.SetFocus*).

Unlike v1, in v2, this method can return `false` if the focus change doesn't happen (e.g. because the view wasn't focusable, or the focus change was cancelled).

```csharp
// Programmatic focus control
if (myButton.SetFocus())
{
    Console.WriteLine("Button now has focus");
}
else
{
    Console.WriteLine("Could not focus button");
}

// Alternative: Set HasFocus property (same effect)
myButton.HasFocus = true;
```

## How to make a View become NOT focused?

The typical method to make a view lose focus is to have another View gain focus. 

```csharp
// Focus another view to remove focus from current
otherView.SetFocus();

// Or advance focus programmatically
Application.Navigation.AdvanceFocus(NavigationDirection.Forward, TabBehavior.TabStop);

// Focus can also be lost when views become non-focusable
myView.CanFocus = false;  // Will lose focus if it had it
myView.Visible = false;   // Will lose focus if it had it
myView.Enabled = false;   // Will lose focus if it had it
```

## Determining the Most Focused SubView

In v1 <xref:Terminal.Gui.ViewBase.View> had `MostFocused` property that traversed up the view-hierarchy returning the last view found with <xref:Terminal.Gui.ViewBase.View.HasFocus> `== true`. In v2, `Application.Navigation.GetFocused()` provides the same functionality with less overhead.

```csharp
// v2 way to get the most focused view
var focused = Application.Navigation.GetFocused();

// This replaces the v1 pattern:
// var focused = Application.TopRunnable.MostFocused;
```

## How Does `View.Add/Remove` Work?

In v1, calling `super.Add (view)` where `view.CanFocus == true` caused all views up the hierarchy (all SuperViews) to get <xref:Terminal.Gui.ViewBase.View.CanFocus> set to `true` as well.

Also, in v1, if `view.CanFocus == true`, `Add` would automatically set <xref:Terminal.Gui.ViewBase.View.TabStop>.

In v2, developers need to explicitly set <xref:Terminal.Gui.ViewBase.View.CanFocus> for any view in the view-hierarchy where focus is desired. This simplifies the implementation significantly and removes confusing behavior.

In v2, the automatic setting of <xref:Terminal.Gui.ViewBase.View.TabStop> in `Add` is retained because it is not overly complex to do so and is a nice convenience for developers to not have to set both <xref:Terminal.Gui.ViewBase.View.TabStop> and <xref:Terminal.Gui.ViewBase.View.CanFocus>. Note we do NOT automatically change <xref:Terminal.Gui.ViewBase.View.CanFocus> if <xref:Terminal.Gui.ViewBase.View.TabStop> is changed.

```csharp
// v2 explicit focus setup
var container = new FrameView() 
{ 
    Title = "Container",
    CanFocus = true,                    // Must be explicitly set
    TabStop = TabBehavior.TabGroup 
};

var button = new Button() 
{ 
    Text = "Click Me",
    CanFocus = true,                    // Must be explicitly set
    TabStop = TabBehavior.TabStop       // Set automatically by Add(), but can override
};

container.Add(button);  // Does not automatically set CanFocus on container
```

## Knowing When a View's Focus is Changing

<xref:Terminal.Gui.ViewBase.View.HasFocusChanging> and <xref:Terminal.Gui.ViewBase.View.HasFocusChanged> are raised when a View's focus is changing.

```csharp
// Monitor focus changes
view.HasFocusChanging += (sender, e) => 
{
    if (e.NewValue && !ValidateCanFocus())
    {
        e.Cancel = true; // Prevent gaining focus
    }
};

view.HasFocusChanged += (sender, e) => 
{
    if (e.CurrentValue)
    {
        OnViewGainedFocus();
    }
    else
    {
        OnViewLostFocus();
    }
};
```

## Built-In Views Interactivity

The following table summarizes how built-in views respond to various input methods:

| View | States | Static | Default | HotKeys | Activate Cmd | Accept Cmd | HotKey Cmd | Click Focus | DblClick | RightClick | GrabMouse |
|------|--------|--------|---------|---------|------------|------------|------------|-------------|----------|------------|-----------|
| **<xref:Terminal.Gui.ViewBase.View>** | 1 | Yes | No | 1 | OnSelect | OnAccept | Focus | Focus | - | - | No |
| **<xref:Terminal.Gui.Views.Label>** | 1 | Yes | No | 1 | OnSelect | OnAccept | FocusNext | Focus | - | FocusNext | No |
| **<xref:Terminal.Gui.Views.Button>** | 1 | No | Yes | 1 | OnSelect | Focus+OnAccept | Focus+OnAccept | HotKey | - | Select | No |
| **<xref:Terminal.Gui.Views.CheckBox>** | 3 | No | No | 1 | OnSelect+Advance | OnAccept | OnAccept | Select | - | Select | No |
| **OptionSelector** | >1 | No | No | 2+ | Advance | SetValue+OnAccept | Focus+SetValue | SetFocus+SetCursor | - | SetFocus+SetCursor | No |
| **LinearRange** | >1 | No | No | 1 | SetFocusedOption | SetFocusedOption+OnAccept | Focus | SetFocus+SetOption | - | SetFocus+SetOption | Yes |
| **<xref:Terminal.Gui.Views.ListView>** | >1 | No | No | 1 | MarkUnMarkRow | OpenSelected+OnAccept | OnAccept | SetMark+OnSelectedChanged | OpenSelected+OnAccept | - | No |
| **<xref:Terminal.Gui.Views.TextField>** | 1 | No | No | 1 | - | OnAccept | Focus | Focus | SelectAll | ContextMenu | No |
| **<xref:Terminal.Gui.Views.Editor>** | 1 | No | No | 1 | - | OnAccept | Focus | Focus | - | ContextMenu | Yes |

### Table Legend

- **States**: Number of visual/functional states the view can have
- **Static**: Whether the view is primarily for display (non-interactive)
- **Default**: Whether the view can be a default button (activated by Enter)
- **HotKeys**: Number of hotkeys the view typically supports
- **Activate Cmd**: What happens when <xref:Terminal.Gui.Input.Command.Activate> is invoked
- **Accept Cmd**: What happens when <xref:Terminal.Gui.Input.Command.Accept> is invoked
- **HotKey Cmd**: What happens when the view's hotkey is pressed
- **Click Focus**: Behavior when clicked (if <xref:Terminal.Gui.ViewBase.View.CanFocus>=true)
- **DblClick**: Behavior on double-click
- **RightClick**: Behavior on right-click
- **GrabMouse**: Whether the view captures mouse for drag operations

## Common Navigation Patterns

### Dialog Navigation

```csharp
var dialog = new Dialog()
{
    Title = "Settings",
    CanFocus = true,
    TabStop = TabBehavior.TabGroup
};

var okButton = new Button() { Text = "OK", IsDefault = true };
var cancelButton = new Button() { Text = "Cancel" };

// Tab navigates between buttons, Enter activates default
dialog.Add(okButton, cancelButton);
```

### Container Navigation

```csharp
var leftPanel = new FrameView() 
{ 
    Title = "Options",
    TabStop = TabBehavior.TabGroup,
    X = 0, 
    Width = Dim.Percent(50)
};

var rightPanel = new FrameView() 
{ 
    Title = "Preview",
    TabStop = TabBehavior.TabGroup,
    X = Pos.Right(leftPanel), 
    Width = Dim.Fill()
};

// F6 navigates between panels, Tab navigates within panels
```

### List Navigation

```csharp
var listView = new ListView()
{
    CanFocus = true,
    TabStop = TabBehavior.TabStop
};

// Arrow keys navigate items, Enter selects, Space toggles
listView.KeyBindings.Add(Key.CursorUp, Command.Up);
listView.KeyBindings.Add(Key.CursorDown, Command.Down);
listView.KeyBindings.Add(Key.Enter, Command.Accept);
```

## Accessibility Considerations

Terminal.Gui's navigation system is designed with accessibility in mind:

### Keyboard Accessibility
- All functionality must be accessible via keyboard
- Tab order should be logical and predictable
- HotKeys provide direct access to important functions
- Arrow keys provide fine-grained navigation within controls

### Visual Accessibility  
- Focus indicators must be clearly visible
- Color is not the only indicator of focus state
- Text and background contrast meets accessibility standards
- HotKeys are visually indicated (underlined characters)

### Screen Reader Support
- Focus changes are announced through system events
- View titles and labels provide context
- Status information is available programmatically

### Best Practices for Accessible Navigation

```csharp
// Provide meaningful labels
var button = new Button() { Text = "_Save Document", HotKey = Key.S };

// Set logical tab order
container.TabStop = TabBehavior.TabGroup;
foreach (var view in container.Subviews)
{
    view.TabStop = TabBehavior.TabStop;
}

// Provide keyboard alternatives to mouse actions
view.KeyBindings.Add(Key.F10, Command.Context); // Right-click equivalent
view.KeyBindings.Add(Key.Space, Command.Activate); // Click equivalent
```

For more information on accessibility standards, see:
- [Web Content Accessibility Guidelines (WCAG)](https://www.w3.org/WAI/WCAG21/quickref/)
- [Microsoft Accessibility Guidelines](https://learn.microsoft.com/en-us/windows/apps/design/accessibility/)
- [.NET Accessibility Documentation](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/advanced/walkthrough-creating-an-accessible-windows-based-application)

