# Terminal.Gui v2 - What's New

This document provides an in-depth overview of the new features, improvements, and architectural changes in Terminal.Gui v2 compared to v1.

**For migration guidance**, see the [v1 To v2 Migration Guide](migratingfromv1.md).

For the 2.5.0 API breaks (`View.Text`, `IAcceptTarget`, `ConfigurationManager`, nested `config.json`), see [2.5.0 Breaking Changes](breaking-changes-2.5.0.md).

## Table of Contents

- [Overview](#overview)
- [Architectural Overhaul](#architectural-overhaul)
- [Instance-Based Application Model](#instance-based-application-model)
- [IRunnable Architecture](#irunnable-architecture)
- [Modern Look & Feel](#modern-look--feel)
- [Simplified API](#simplified-api)
- [View Improvements](#view-improvements)
- [New and Improved Views](#new-and-improved-views)
- [Enhanced Input Handling](#enhanced-input-handling)
- [Configuration and Persistence](#configuration-and-persistence)
- [Debugging and Performance](#debugging-and-performance)
- [Additional Features](#additional-features)

---

## Overview

Terminal.Gui v2 represents a fundamental redesign of the library's architecture, API, and capabilities. Key improvements include:

- **Instance-Based Application Model** - Move from static singletons to <xref:Terminal.Gui.App.IApplication> instances
- **IRunnable Architecture** - Interface-based pattern for type-safe, runnable views
- **Proper Resource Management** - Full IDisposable pattern with automatic cleanup
- **Built-in Scrolling** - Every view supports scrolling inherently
- **24-bit TrueColor** - Full color spectrum by default
- **Enhanced Input** - Modern keyboard and mouse APIs
- **Improved Layout** - Simplified with adornments (Margin, Border, Padding)
- **Better Navigation** - Decoupled focus and tab navigation
- **Configuration System** - Persistent themes and settings
- **Logging and Metrics** - Built-in debugging and performance tracking

---

## Architectural Overhaul

### Design Philosophy

Terminal.Gui v2 was designed with these core principles:

1. **Separation of Concerns** - Layout, focus, input, and drawing are cleanly decoupled
2. **Performance** - Reduced overhead in rendering and event handling
3. **Modern .NET Practices** - Standard patterns like `EventHandler<T>` and `IDisposable`
4. **Testability** - Views can be tested in isolation without global state
5. **Accessibility** - Improved keyboard navigation and visual feedback

### Result

- Thousands of lines of redundant or complex code removed
- More modular and maintainable codebase
- Better performance and predictability
- Easier to extend and customize

---

## Instance-Based Application Model

See the [Application Deep Dive](application.md) for complete details.

v2 introduces an instance-based architecture that eliminates global state and enables multiple application contexts.

### Key Features

**IApplication Interface:**
- [Application.Create()](xref:Terminal.Gui.App.Application.Create*) returns an <xref:Terminal.Gui.App.IApplication> instance
- Multiple applications can coexist (useful for testing)
- Each instance manages its own driver, session stack, and resources

**View.App Property:**
- Every view has an <xref:Terminal.Gui.ViewBase.View.App> property referencing its <xref:Terminal.Gui.App.IApplication> context
- Views access application services through <xref:Terminal.Gui.ViewBase.View.App> (driver, session management, etc.)
- Eliminates static dependencies, improving testability

**Session Management:**
- <xref:Terminal.Gui.App.IApplication.SessionStack> tracks all running sessions as a stack
- <xref:Terminal.Gui.App.IApplication.TopRunnable> property references the currently active session
- [Begin()](xref:Terminal.Gui.App.IApplication.Begin*) and [End()](xref:Terminal.Gui.App.IApplication.End*) methods manage session lifecycle

### Example

```csharp
// Instance-based pattern (recommended)
IApplication app = Application.Create ().Init ();
Window window = new () { Title = "My App" };
app.Run (window);
window.Dispose ();
app.Dispose ();

// With using statement for automatic disposal
using (IApplication app = Application.Create ().Init ())
{
    Window window = new () { Title = "My App" };
    app.Run (window);
    window.Dispose ();
} // app.Dispose() called automatically

// Access from within a view
public class MyView : View
{
    public void DoWork ()
    {
        App?.Driver.Move (0, 0);
        App?.TopRunnableView?.SetNeedsDraw ();
    }
}
```

### Benefits

- **Testability** - Mock <xref:Terminal.Gui.App.IApplication> for unit tests
- **No Global State** - Multiple contexts can coexist
- **Clear Ownership** - Views explicitly know their context
- **Proper Cleanup** - IDisposable ensures resources are released

### Resource Management

v2 implements full `IDisposable` pattern:

```csharp
// Recommended: using statement
using (IApplication app = Application.Create ().Init ())
{
    app.Run<MyDialog> ();
    MyResult? result = app.GetResult<MyResult> ();
}

// Ensures:
// - Input thread stopped cleanly
// - Driver resources released
// - No thread leaks in tests
```

**Important Changes:**
- [Shutdown()](xref:Terminal.Gui.App.Application.Shutdown*) method is obsolete - use `Dispose()` instead
- Always dispose applications (especially in tests)
- Input thread runs at ~50 polls/second (20ms throttle) until disposed

---

## IRunnable Architecture

See the [Application Deep Dive](application.md) for complete details.

v2 introduces <xref:Terminal.Gui.App.IRunnable> - an interface-based pattern for runnable views with type-safe results.

### Key Features

**Interface-Based:**
- Implement <xref:Terminal.Gui.App.IRunnable> without inheriting from `Runnable`
- Any view can be runnable
- Decouples runnability from view hierarchy

**Type-Safe Results:**
- Generic `TResult` parameter provides compile-time type safety
- `null` indicates cancellation/non-acceptance
- Results extracted before disposal in lifecycle events

**Lifecycle Events (CWP-Compliant):**
- `IsRunningChanging` - Cancellable, before stack change
- `IsRunningChanged` - Non-cancellable, after stack change
- `IsModalChanging` - Cancellable, before modal state change
- `IsModalChanged` - Non-cancellable, after modal state change

### Example

```csharp
public class FileDialog : Runnable<string?>
{
    private TextField _pathField;
    
    public FileDialog ()
    {
        Title = "Select File";
        _pathField = new () { Width = Dim.Fill () };
        Add (_pathField);
        
        Button okButton = new () { Text = "OK", IsDefault = true };
        okButton.Accepting += (s, e) =>
        {
            Result = _pathField.Text;
            Application.RequestStop ();
        };
        AddButton (okButton);
    }
    
    protected override bool OnIsRunningChanging (bool oldValue, bool newValue)
    {
        if (!newValue)  // Stopping - extract result before disposal
        {
            Result = _pathField?.Text;
            
            // Optionally cancel stop
            if (HasUnsavedChanges ())
            {
                return true; // Cancel
            }
        }
        return base.OnIsRunningChanging (oldValue, newValue);
    }
}

// Use with fluent API
using (IApplication app = Application.Create ().Init ())
{
    app.Run<FileDialog> ();
    string? path = app.GetResult<string> ();
    
    if (path is { })
    {
        OpenFile (path);
    }
}
```

### Fluent API

v2 enables elegant method chaining:

```csharp
// Concise and readable
using (IApplication app = Application.Create ().Init ())
{
    app.Run<ColorPickerDialog> ();
    Color? result = app.GetResult<Color> ();
}
```

**Key Methods:**
- [Init()](xref:Terminal.Gui.App.IApplication.Init*) - Returns <xref:Terminal.Gui.App.IApplication> for chaining
- [Run<TRunnable>()](xref:Terminal.Gui.App.IApplication.Run*) - Creates and runs runnable, returns <xref:Terminal.Gui.App.IApplication>
- [GetResult<T>()](xref:Terminal.Gui.App.IApplication.GetResult*) - Extract typed result after run
- `Dispose()` - Release all resources

### Disposal Semantics

**"Whoever creates it, owns it":**

| Method | Creator | Owner | Disposal |
|--------|---------|-------|----------|
| [Run<TRunnable>()](xref:Terminal.Gui.App.IApplication.Run*) | Framework | Framework | Automatic when returns |
| `Run(IRunnable)` | Caller | Caller | Manual by caller |

```csharp
// Framework ownership - automatic disposal
app.Run<MyDialog> (); // Dialog disposed automatically

// Caller ownership - manual disposal
MyDialog dialog = new ();
app.Run (dialog);
dialog.Dispose (); // Caller must dispose
```

### Benefits

- **Type Safety** - No casting, compile-time checking
- **Clean Lifecycle** - CWP-compliant events
- **Automatic Disposal** - Framework manages created runnables
- **Flexible** - Works with any View, not just Toplevel

---

## Modern Look & Feel

### 24-bit TrueColor Support

See the [Drawing Deep Dive](drawing.md) for complete details.

v2 provides full 24-bit color support by default:

- **Implementation**: [Attribute](~/api/Terminal.Gui.Drawing.Attribute.yml) class handles RGB values
- **Fallback**: Automatic 16-color mode for older terminals
- **Driver Support**: `IDriver.SupportsTrueColor` detection
- **Usage**: Direct RGB input via [Color](~/api/Terminal.Gui.Drawing.Color.yml) struct

```csharp
// 24-bit RGB color
Color customColor = new (0xFF, 0x99, 0x00);

// Or use named colors (ANSI-compliant)
Color color = Color.Yellow; // Was "Brown" in v1
```

### Enhanced Borders and Adornments

See the [Layout Deep Dive](layout.md) for complete details.

v2 introduces a comprehensive [Adornment](<xref:Terminal.Gui.ViewBase.IAdornment>) system with lightweight adornments that lazily create Views when needed:

- **[Margin](<xref:Terminal.Gui.ViewBase.Margin>)** - Transparent spacing outside the border
- **[Border](<xref:Terminal.Gui.ViewBase.Border>)** - Visual frame with title, multiple styles
- **[Padding](<xref:Terminal.Gui.ViewBase.Padding>)** - Spacing inside the border
**Border Features:**
- Multiple [LineStyle](<xref:Terminal.Gui.Drawing.LineStyle>) options: Single, Double, Heavy, Rounded, Dashed, Dotted
- Automatic line intersection handling via [LineCanvas](<xref:Terminal.Gui.Drawing.LineCanvas>)
- Configurable thickness per side via [Thickness](<xref:Terminal.Gui.Drawing.Thickness>)
- Title display with alignment options

```csharp
view.BorderStyle = LineStyle.Double;
view.Border.Thickness = new (1);
view.Title = "My View";

view.Margin.Thickness = new (2);
view.Padding.Thickness = new (1);
```

### User Configurable Themes

See the [Configuration Deep Dive](config.md) and [Scheme Deep Dive](scheme.md) for details.

v2 adds comprehensive theme support:

- **TuiConfigurationBuilder**: Loads themes and settings from nested JSON files
- **Schemes**: Applied per-view or globally via [Scheme](~/api/Terminal.Gui.Drawing.Scheme.yml)
- **Text Styles**: [TextStyle](~/api/Terminal.Gui.Drawing.TextStyle.yml) supports Bold, Italic, Underline, Strikethrough, Blink, Reverse, Faint
- **User Customization**: End-users can personalize without code changes

```csharp
// Apply a theme
TuiConfigurationBuilder builder = new ();
builder.ThemeManager.SwitchTheme ("Dark");

// Customize text style
view.Scheme.Normal = new (
    Color.White, 
    Color.Black, 
    TextStyle.Bold | TextStyle.Underline
);
```

### LineCanvas

See the [Drawing Deep Dive](drawing.md) for complete details.

[LineCanvas](~/api/Terminal.Gui.Drawing.LineCanvas.yml) provides sophisticated line drawing:

- Auto-joining lines at intersections
- Multiple line styles (Single, Double, Heavy, etc.)
- Automatic glyph selection for corners and T-junctions
- Used by [Border](~/api/Terminal.Gui.ViewBase.Border.yml), [Line](~/api/Terminal.Gui.Views.Line.yml), and custom views

```csharp
// Line view uses LineCanvas
Line line = new () { Orientation = Orientation.Horizontal };
line.LineStyle = LineStyle.Double;
```

### Gradients

See the [Drawing Deep Dive](drawing.md) for details.

v2 adds gradient support:

- [Gradient](~/api/Terminal.Gui.Drawing.Gradient.yml) - Color transitions
- [GradientFill](~/api/Terminal.Gui.Drawing.GradientFill.yml) - Fill patterns
- Uses TrueColor for smooth effects
- Apply to borders, backgrounds, or custom elements

```csharp
Gradient gradient = new (Color.Blue, Color.Cyan);
view.BackgroundGradient = new (gradient, Orientation.Vertical);
```

---

## Simplified API

### Consistency and Reduction

v2 consolidates redundant APIs:

- **Centralized Navigation**: [ApplicationNavigation](~/api/Terminal.Gui.App.ApplicationNavigation.yml) replaces scattered focus methods
- **Standard Events**: All events use `EventHandler<T>` pattern
- **Consistent Naming**: Methods follow .NET conventions (e.g., `OnHasFocusChanged`)
- **Reduced Surface**: Fewer but more powerful APIs

**Example:**
```csharp
// v1 - Multiple scattered methods
View.MostFocused
View.EnsureFocus ()
View.FocusNext ()

// v2 - Centralized
Application.Navigation.GetFocused ()
view.SetFocus ()
view.AdvanceFocus ()
```

### Modern .NET Standards

- Events: `EventHandler<EventArgs>` instead of custom delegates
- Properties: Consistent get/set patterns
- Disposal: IDisposable throughout
- Nullability: Enabled in core library files

### Performance Optimizations

v2 reduces overhead through:

- Smarter <xref:Terminal.Gui.ViewBase.View.NeedsDraw> system (only draw what changed)
- Reduced allocations in hot paths (event handling, rendering)
- Optimized layout calculations
- Efficient input processing

**Result**: Snappier UIs, especially with many views or frequent updates

---

## View Improvements

### Deterministic Lifetime Management

v2 clarifies view ownership:

- Explicit disposal rules enforced by unit tests
- `Application.Run` manages `Runnable` lifecycle
- SubViews disposed automatically with SuperView
- Clear documentation of ownership semantics

### Built-in Scrolling

See the [Scrolling Deep Dive](scrolling.md) for complete details.

Every [View](~/api/Terminal.Gui.ViewBase.yml) supports scrolling inherently:

- **[Viewport](~/api/Terminal.Gui.ViewBase.yml)** - Visible rectangle (can have non-zero location)
- **[GetContentSize](~/api/Terminal.Gui.ViewBase.yml)** - Returns total content size
- **[SetContentSize](~/api/Terminal.Gui.ViewBase.yml)** - Sets scrollable content size
- **ScrollVertical/ScrollHorizontal** - Helper methods

**No need for ScrollView wrapper!**

```csharp
// Enable scrolling
view.SetContentSize (new (100, 100));

// Scroll by changing Viewport location
view.ScrollVertical (5);
view.ScrollHorizontal (3);

// Built-in scrollbars with automatic visibility
view.ViewportSettings |= ViewportSettingsFlags.HasVerticalScrollBar;
view.ViewportSettings |= ViewportSettingsFlags.HasHorizontalScrollBar;
```

### Enhanced ScrollBar

v2 replaces `ScrollBarView` with [ScrollBar](~/api/Terminal.Gui.Views.ScrollBar.yml):

- Cleaner implementation
- Automatic show/hide via [ScrollBarVisibilityMode](~/api/Terminal.Gui.Views.ScrollBarVisibilityMode.yml)
- Proportional sizing with `ScrollSlider`
- Integrated with View's scrolling system
- Simple to enable via [ViewportSettingsFlags](~/api/Terminal.Gui.ViewBase.ViewportSettingsFlags.yml)
- Accessible via [View.VerticalScrollBar](~/api/Terminal.Gui.ViewBase.yml) / [View.HorizontalScrollBar](~/api/Terminal.Gui.ViewBase.yml)

### Advanced Layout Features

See the [Layout Deep Dive](layout.md) and [DimAuto Deep Dive](dimauto.md) for details.

**<xref:Terminal.Gui.ViewBase.Dim.Auto*>:**
- Automatically sizes views based on content or subviews
- Reduces manual layout calculations
- Supports multiple styles (Text, Content, Position)

**<xref:Terminal.Gui.ViewBase.Pos.AnchorEnd*>:**
- Anchor to right or bottom of SuperView
- Enables flexible, responsive layouts

**[Pos.Align](~/api/Terminal.Gui.ViewBase.Pos.yml):**
- Align multiple views (Left, Center, Right)
- Simplifies creating aligned layouts

```csharp
// Auto-size based on text
label.Width = Dim.Auto ();
label.Height = Dim.Auto ();

// Anchor to bottom-right
button.X = Pos.AnchorEnd (10);
button.Y = Pos.AnchorEnd (2);

// Center align
label1.X = Pos.Center ();
label2.X = Pos.Center ();
```

### View Arrangement

See the [Arrangement Deep Dive](arrangement.md) for complete details.

[View.Arrangement](~/api/Terminal.Gui.ViewBase.yml) enables interactive UI:

- **[ViewArrangement.Movable](~/api/Terminal.Gui.ViewBase.ViewArrangement.yml)** - Drag with mouse or move with keyboard
- **[ViewArrangement.Resizable](~/api/Terminal.Gui.ViewBase.ViewArrangement.yml)** - Resize edges with mouse or keyboard
- **[ViewArrangement.Overlapped](~/api/Terminal.Gui.ViewBase.ViewArrangement.yml)** - Z-order management for overlapping views

**Arrangement Key**: Press `Ctrl+F5` (configurable via `Application.DefaultKeyBindings` for `Command.Arrange`) to enter arrange mode

```csharp
// Movable and resizable window
window.Arrangement = ViewArrangement.Movable | ViewArrangement.Resizable;

// Overlapped windows
container.Arrangement = ViewArrangement.Overlapped;
```

### Enhanced Navigation

See the [Navigation Deep Dive](navigation.md) for complete details.

v2 decouples navigation concepts:

- **[CanFocus](~/api/Terminal.Gui.ViewBase.yml)** - Whether view can receive focus (defaults to `false` in v2)
- **[TabStop](~/api/Terminal.Gui.ViewBase.yml)** - [TabBehavior](~/api/Terminal.Gui.ViewBase.TabBehavior.yml) enum (TabStop, TabGroup, NoStop)
- **[ApplicationNavigation](~/api/Terminal.Gui.App.ApplicationNavigation.yml)** - Centralized navigation logic

**Navigation Keys (Configurable):**
- `Tab` / `Shift+Tab` - Next/previous TabStop
- `F6` / `Shift+F6` - Next/previous TabGroup
- Arrow keys - Same as Tab navigation

```csharp
// Configure navigation keys via Application.DefaultKeyBindings
Application.DefaultKeyBindings[Command.NextTabStop] = Bind.All (Key.Tab);
Application.DefaultKeyBindings[Command.PreviousTabStop] = Bind.All (Key.Tab.WithShift);
Application.DefaultKeyBindings[Command.NextTabGroup] = Bind.All (Key.F6);
Application.DefaultKeyBindings[Command.PreviousTabGroup] = Bind.All (Key.F6.WithShift);

// Set tab behavior
view.CanFocus = true;
view.TabStop = TabBehavior.TabStop; // Normal tab navigation
```

---

## New and Improved Views

See the [Views Overview](views.md) for a complete catalog.

### New Views in v2

- **[Bar](~/api/Terminal.Gui.Views.Bar.yml)** - Foundation for StatusBar, MenuBar, PopoverMenu
- **[CharMap](~/api/Terminal.Gui.Views.CharMap.yml)** - Scrollable Unicode character map with UCD support
- **[ColorPicker](~/api/Terminal.Gui.Views.ColorPicker.yml)** - TrueColor selection with multiple color models
- **[DatePicker](~/api/Terminal.Gui.Views.DatePicker.yml)** - Calendar-based date selection
- **[FlagSelector](~/api/Terminal.Gui.Views.FlagSelector.yml)** - Non-mutually-exclusive flag selection
- **[GraphView](~/api/Terminal.Gui.Views.GraphView.yml)** - Data visualization (bar, scatter, line graphs)
- **[Line](~/api/Terminal.Gui.Views.Line.yml)** - Single lines with LineCanvas integration
- **[NumericUpDown<T>](~/api/Terminal.Gui.Views.NumericUpDown-1.yml)** - Type-safe numeric input
- **[OptionSelector](~/api/Terminal.Gui.Views.OptionSelector.yml)** - Mutually-exclusive option selection
- **[Shortcut](~/api/Terminal.Gui.Views.Shortcut.yml)** - Command display with key bindings
- **[LinearRange](~/api/Terminal.Gui.Views.LinearRange.yml)** - Sophisticated range selection control
- **[SpinnerView](~/api/Terminal.Gui.Views.SpinnerView.yml)** - Animated progress indicators

### Significantly Improved Views

- **[FileDialog](~/api/Terminal.Gui.Views.FileDialog.yml)** - TreeView navigation, Unicode icons, search, history
- **[ScrollBar](~/api/Terminal.Gui.Views.ScrollBar.yml)** - Clean implementation with auto-show
- **[StatusBar](~/api/Terminal.Gui.Views.StatusBar.yml)** - Rebuilt on Bar infrastructure
- **[TableView](~/api/Terminal.Gui.Views.TableView.yml)** - Generic collections, checkboxes, tree structures, custom rendering
- **[MenuBar](~/api/Terminal.Gui.Views.MenuBar.yml)** / **[PopoverMenu](~/api/Terminal.Gui.Views.PopoverMenu.yml)** - Redesigned menu system

---

## Enhanced Input Handling

### Keyboard API

See the [Keyboard Deep Dive](keyboard.md) and [Command Deep Dive](command.md) for details.

**[Key](~/api/Terminal.Gui.Input.Key.yml) Class:**
- Replaces v1's `KeyEvent` struct
- High-level abstraction over raw key codes
- Properties for modifiers and key type
- Platform-independent

```csharp
// Check keys
if (key == Key.Enter) { }
if (key == Key.C.WithCtrl) { }

// Modifiers
if (key.Shift) { }
if (key.Ctrl) { }
```

**Key Bindings:**
- Map keys to [Command](~/api/Terminal.Gui.Input.Command.yml) enums
- Scopes: Application, Focused, HotKey
- Views declare supported commands via [View.AddCommand](~/api/Terminal.Gui.ViewBase.yml)

```csharp
// Add command handler
view.AddCommand (Command.Accept, HandleAccept);

// Bind key to command
view.KeyBindings.Add (Key.Enter, Command.Accept);

private bool HandleAccept ()
{
    // Handle command
    return true; // Handled
}
```

**Configurable Keys** (via `Application.DefaultKeyBindings`):
- `Command.Quit` - Close app (default: Esc)
- `Command.Arrange` - Arrange mode (default: Ctrl+F5)
- Navigation commands (`Command.NextTabStop`, `Command.NextTabGroup`, etc.)

### Mouse API

See the [Mouse Deep Dive](mouse.md) for complete details.

**[MouseEventArgs](~/api/Terminal.Gui.Input.Mouse.yml):**
- Replaces v1's `MouseEventEventArgs`
- Cleaner structure for mouse data
- [MouseFlags](~/api/Terminal.Gui.Input.MouseFlags.yml) for button states

**Granular Events:**
- [View.MouseClick](~/api/Terminal.Gui.ViewBase.yml) - High-level click events
- Double-click support
- Mouse movement tracking
- Viewport-relative coordinates (not screen-relative)

**Highlight and Repeat on Hold:**
- [View.MouseHighlightStates](~/api/Terminal.Gui.ViewBase.yml) - Allows views to provide visual feedback on hover/click.
- [View.MouseState](~/api/Terminal.Gui.ViewBase.yml) - Indicates whether the mouse is pressed, hovered, or outside.
- [View.MouseHoldRepeat](~/api/Terminal.Gui.ViewBase.yml) - Enables or disables whether mouse click events will be repeated when the user holds the mouse down

## Configuration and Persistence

See the [Configuration Deep Dive](config.md) for complete details.

### TuiConfigurationBuilder

<xref:Terminal.Gui.Configuration.TuiConfigurationBuilder> provides:

- JSON-based persistence via Microsoft.Extensions.Configuration
- Theme management (`IThemeManager.SwitchTheme`)
- Key binding and view-default overlays
- App-specific Settings POCOs (`BindAppSettings<T>`)

```csharp
TuiConfigurationBuilder builder = new ("MyApp");
builder.RuntimeConfig = """{ "Theme": "Dark" }""";
builder.ApplyToStaticFacades ();
builder.ThemeManager.SwitchTheme ("Dark");
```

See [Migrating ConfigurationManager to TuiConfigurationBuilder](migrate-cm-to-mec.md) if you still have 2.4.x `ConfigurationManager` calls or a flat-key `config.json`. See [2.5.0 Breaking Changes](breaking-changes-2.5.0.md) for the other 2.5.0 API breaks.

**User Customization:**
- End-users can personalize themes, colors, text styles
- Key bindings can be remapped
- No code changes required
- JSON files easily editable

---

## Debugging and Performance

See the [Logging Deep Dive](logging.md) for complete details.

### Logging System

[Logging](~/api/Terminal.Gui.App.Logging.yml) integrates with Microsoft.Extensions.Logging:

- Multi-level logging (Trace, Debug, Info, Warning, Error)
- Internal operation tracking (rendering, input, layout)
- Works with standard .NET logging frameworks (Serilog, NLog, etc.)

```csharp
// Configure logging
Logging.ConfigureLogging ("myapp.log", LogLevel.Debug);

// Use in code
Logging.Debug ("Rendering view {ViewId}", view.Id);
```

### Metrics

`Logging.Meter` provides performance metrics:

- Frame rate tracking
- Redraw times
- Iteration timing
- Input processing overhead

**Tools**: Use `dotnet-counters` or other metrics tools to monitor

```bash
dotnet counters monitor --name MyApp Terminal.Gui
```

---

## Additional Features

### Sixel Image Support

v2 supports the Sixel protocol for rendering images:

- [SixelEncoder](~/api/Terminal.Gui.Drawing.SixelEncoder.yml) - Encode images as Sixel data
- [SixelSupportDetector](~/api/Terminal.Gui.Drawing.SixelSupportDetector.yml) - Detect terminal support
- [SixelToRender](~/api/Terminal.Gui.Drawing.SixelToRender.yml) - Render Sixel images
- Compatible terminals: Windows Terminal, xterm, others

**Use Cases**: Image previews, graphics in terminal apps

### AOT Support

v2 ensures compatibility with Ahead-of-Time compilation:

- Avoid reflection patterns problematic for AOT
- Source generators for JSON serialization via SourceGenerationContext
- Single-file deployment support
- Faster startup, reduced runtime overhead

**Example**: See `Tests/NativeAotSmoke` for the in-repo AOT smoke validation app.

### Enhanced Unicode Support

- Correctly manages wide characters (CJK scripts)
- [TextFormatter](~/api/Terminal.Gui.Text.TextFormatter.yml) accounts for Unicode width
- Fixes v1 layout issues with wide characters
- International application support

---

## Conclusion

Terminal.Gui v2 represents a comprehensive modernization:

**Architecture:**
- Instance-based application model
- IRunnable architecture with type-safe results
- Proper resource management (IDisposable)
- Decoupled concerns (layout, focus, input)

**Features:**
- 24-bit TrueColor
- Built-in scrolling
- Enhanced adornments (Margin, Border, Padding)
- Modern keyboard and mouse APIs
- Configuration and themes
- Logging and metrics

**API:**
- Simplified and consistent
- Modern .NET patterns
- Better performance
- Improved testability

**Views:**
- Many new views (CharMap, ColorPicker, GraphView, etc.)
- Significantly improved existing views
- Easier to create custom views

v2 provides a robust foundation for building sophisticated, maintainable, and user-friendly terminal applications. The architectural improvements, combined with new features and enhanced APIs, enable developers to create modern terminal UIs that feel responsive and polished.

For detailed migration guidance, see the [v1 To v2 Migration Guide](migratingfromv1.md).