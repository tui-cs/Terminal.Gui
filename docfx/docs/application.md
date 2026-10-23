# Application Architecture

Terminal.Gui v2 uses an instance-based application architecture that decouples views from the global application state, enabling multiple application contexts, providing type-safe result handling, enabling testability.

## Key Features

- **Instance-Based**: Use [Application.Create()](xref:Terminal.Gui.App.Application.Create*) to get an <xref:Terminal.Gui.App.IApplication> instance instead of static methods
- **IRunnable Interface**: Views implement <xref:Terminal.Gui.App.IRunnable> to participate in session management without inheriting from `Runnable`
- **Fluent API**: Chain [Init()](xref:Terminal.Gui.App.IApplication.Init*) and [Run<TRunnable>()](xref:Terminal.Gui.App.IApplication.Run*) for elegant, concise code
- **IDisposable Pattern**: Proper resource cleanup with `Dispose()` or `using` statements
- **Automatic Disposal**: Framework-created runnables are automatically disposed
- **Type-Safe Results**: Generic `TResult` parameter provides compile-time type safety
- **CWP Compliance**: All lifecycle events follow the Cancellable Work Pattern

## View Hierarchy and Run Stack

```mermaid
graph TB
    subgraph ViewTree["View Hierarchy (SuperView/SubView)"]
        direction TB
        Top[app.TopRunnable<br/>Window]
        Menu[MenuBar]
        Status[StatusBar]
        Content[Content View]
        LeftButton[Button]
        MiddleButton[Button]
        
        Top --> Menu
        Top --> Status
        Top --> Content
        Content --> LeftButton
        Content --> MiddleButton
    end
    
    subgraph Stack["app.SessionStack"]
        direction TB
        S1[Window<br/>Currently Active]
        S2[Previous Runnable<br/>Waiting]
        S3[Base Runnable<br/>Waiting]
        
        S1 -.-> S2 -.-> S3
    end
    
    Top -.->|"same instance"| S1
    
    style Top fill:#ccffcc,stroke:#339933,stroke-width:3px
    style S1 fill:#ccffcc,stroke:#339933,stroke-width:3px
```

## Usage Example Flow

```mermaid
sequenceDiagram
    participant App as IApplication
    participant Main as Main Window
    participant Dialog as Dialog
    
    Note over App: Initially empty SessionStack
    
    App->>Main: Run(mainWindow)
    activate Main
    Note over App: SessionStack: [Main]<br/>TopRunnable: Main
    
    Main->>Dialog: Run(dialog)
    activate Dialog
    Note over App: SessionStack: [Dialog, Main]<br/>TopRunnable: Dialog
    
    Dialog->>App: RequestStop()
    deactivate Dialog
    Note over App: SessionStack: [Main]<br/>TopRunnable: Main
    
    Main->>App: RequestStop()
    deactivate Main
    Note over App: SessionStack: []<br/>TopRunnable: null
```

## Key Concepts

### Instance-Based vs Static

**Terminal.Gui v2** supports both static and instance-based patterns. The static <xref:Terminal.Gui.App.Application> class is marked obsolete but still functional for backward compatibility. The recommended pattern is to use [Application.Create()](xref:Terminal.Gui.App.Application.Create*) to get an <xref:Terminal.Gui.App.IApplication> instance:

```csharp
// RECOMMENDED (v2 - instance-based with using statement):
using (IApplication app = Application.Create ().Init ())
{
    Window top = new ();
    top.Add (myView);
    app.Run (top);
    top.Dispose ();
} // app.Dispose() called automatically

// WITH IRunnable (fluent API with automatic disposal):
using (IApplication app = Application.Create ().Init ())
{
    app.Run<ColorPickerDialog> ();
    Color? result = app.GetResult<Color> ();
}

// ALTERNATIVE (manual disposal):
IApplication app = Application.Create ().Init ();
app.Run<ColorPickerDialog> ();
Color? result = app.GetResult<Color> ();
app.Dispose ();

// OLD (v1 / early v2 - obsolete, avoid in new code):
Application.Init ();
Window top = new ();
top.Add (myView);
Application.Run (top);
top.Dispose ();
Application.Shutdown (); // Obsolete - use Dispose() instead
```

**Note:** The static <xref:Terminal.Gui.App.Application> class delegates to an internal singleton backend instance. [Application.Create()](xref:Terminal.Gui.App.Application.Create*) creates a **new** application instance, enabling multiple application contexts and better testability.

### View.App Property

Every view now has an <xref:Terminal.Gui.ViewBase.View.App> property that references its application context:

```csharp
public class View
{
    /// <summary>
    /// Gets the application context for this view.
    /// </summary>
    public IApplication? App { get; internal set; }
    
    /// <summary>
    /// Gets the application context, checking parent hierarchy if needed.
    /// Override to customize application resolution.
    /// </summary>
    public virtual IApplication? GetApp () => App ?? SuperView?.GetApp ();
}
```

**Benefits:**
- Views can be tested without [Application.Init()](xref:Terminal.Gui.App.IApplication.Init*)
- Multiple applications can coexist
- Clear ownership: views know their context
- Reduced global state dependencies

### Accessing Application from Views

```csharp
public class MyView : View
{
    public override void OnEnter (View view)
    {
        // Use View.App instead of obsolete static Application
        IApplication? app = App;
        app?.TopRunnable?.SetNeedsDraw ();
        
        // Access SessionStack
        if (app?.SessionStack?.Count > 0)
        {
            // Work with sessions
        }
    }
}
```

## IRunnable Architecture

Terminal.Gui v2 introduces the **IRunnable** interface pattern that decouples runnable behavior from the `Runnable` class hierarchy. Views can implement <xref:Terminal.Gui.App.IRunnable> to participate in session management without inheritance constraints.

### Key Benefits

- **Interface-Based**: No forced inheritance from `Runnable`
- **Type-Safe Results**: Generic `TResult` parameter provides compile-time type safety
- **Fluent API**: Method chaining for elegant, concise code
- **Automatic Disposal**: Framework manages lifecycle of created runnables
- **CWP Compliance**: All lifecycle events follow the Cancellable Work Pattern

### Fluent API Pattern

The fluent API enables elegant method chaining with automatic resource management:

```csharp
// Recommended: using statement with GetResult
using (IApplication app = Application.Create ().Init ())
{
    app.Run<ColorPickerDialog> ();
    Color? result = app.GetResult<Color> ();
    
    if (result is { })
    {
        ApplyColor (result);
    }
}

// Alternative: Manual disposal
IApplication app = Application.Create ().Init ();
app.Run<ColorPickerDialog> ();
Color? result = app.GetResult<Color> ();
app.Dispose ();

if (result is { })
{
    ApplyColor (result);
}
```

**Key Methods:**

- [Init()](xref:Terminal.Gui.App.IApplication.Init*) - Returns <xref:Terminal.Gui.App.IApplication> for chaining
- [Run<TRunnable>()](xref:Terminal.Gui.App.IApplication.Run*) - Creates and runs runnable, returns <xref:Terminal.Gui.App.IApplication>
- [GetResult()](xref:Terminal.Gui.App.IApplication.GetResult*) / `GetResult<T>()` - Extract typed result after run
- `Dispose()` - Release all resources (called automatically with `using`)

### Disposal Semantics

**"Whoever creates it, owns it":**

During disposal, `MainLoopCoordinator.Stop()` performs terminal-side cleanup (including kitty keyboard disable when it was enabled) before disposing output resources.

| Method | Creator | Owner | Disposal |
|--------|---------|-------|----------|
| [Run<TRunnable>()](xref:Terminal.Gui.App.IApplication.Run*) | Framework | Framework | Automatic when [Run<TRunnable>()](xref:Terminal.Gui.App.IApplication.Run*) returns |
| `Run(IRunnable)` | Caller | Caller | Manual by caller |

```csharp
// Framework ownership - automatic disposal
using (IApplication app = Application.Create ().Init ())
{
    app.Run<MyDialog> (); // Dialog disposed automatically when Run returns
    MyResultType? result = app.GetResult<MyResultType> ();
}

// Caller ownership - manual disposal
using (IApplication app = Application.Create ().Init ())
{
    MyDialog dialog = new ();
    app.Run (dialog);
    MyResultType? result = dialog.Result;
    dialog.Dispose ();  // Caller must dispose
}
```

**NOTE**: The semantics of `view.Add (subView)` is such that ownership of the `subView` is controlled by `view` unless `view.Remove (subView)` is explicitly called.

### Creating Runnable Views

Derive from `Runnable<TResult>` or implement <xref:Terminal.Gui.App.IRunnable>:

```csharp
public class FileDialog : Runnable<string?>
{
    private TextField _pathField;
    
    public FileDialog ()
    {
        Title = "Select File";
        
        _pathField = new () { X = 1, Y = 1, Width = Dim.Fill (1) };
        
        Button okButton = new () { Text = "OK", IsDefault = true };
        okButton.Accepting += (s, e) =>
        {
            Result = _pathField.Text;
            App?.RequestStop ();
        };
        
        Add (_pathField, okButton);
    }
    
    protected override bool OnIsRunningChanging (bool oldValue, bool newValue)
    {
        if (!newValue)  // Stopping - extract result before disposal
        {
            Result = _pathField?.Text;
        }
        return base.OnIsRunningChanging (oldValue, newValue);
    }
}
```

### Lifecycle Properties

- **<xref:Terminal.Gui.App.IRunnable.IsRunning>** - True from [Begin()](xref:Terminal.Gui.App.IApplication.Begin*) until [End()](xref:Terminal.Gui.App.IApplication.End*) stops the session
- **<xref:Terminal.Gui.App.IRunnable.IsModal>** - True when runnable is at top of stack (capturing all input)
- **`Result`** - Typed result value set before stopping

### Lifecycle Events (CWP-Compliant)

All events follow Terminal.Gui's Cancellable Work Pattern:

| Event | Cancellable | When | Use Case |
|-------|-------------|------|----------|
| <xref:Terminal.Gui.App.IRunnable.IsRunningChanging> | ✓ | Before add/remove from stack | Extract result, prevent close |
| <xref:Terminal.Gui.App.IRunnable.IsRunningChanged> | ✗ | After stack change | Post-start/stop cleanup |
| `IsModalChanged` | ✗ | After modal state change | Update UI after focus change |

**Example - Result Extraction:**

```csharp
protected override bool OnIsRunningChanging (bool oldValue, bool newValue)
{
    if (!newValue)  // Stopping
    {
        // Extract result before views are disposed
        Result = _colorPicker.SelectedColor;
        
        // Optionally cancel stop (e.g., unsaved changes)
        if (HasUnsavedChanges ())
        {
            int? response = MessageBox.Query ("Save?", "Save changes?", Strings.btnNo, Strings.btnYes);
            
            if (response is null or 0)
            {
                return true;  // Cancel stop
            }

            if (response == 1)
            {
                Save ();
            }
        }
    }
    
    return base.OnIsRunningChanging (oldValue, newValue);
}
```

### SessionStack

The <xref:Terminal.Gui.App.IApplication.SessionStack> manages all running <xref:Terminal.Gui.App.IRunnable> sessions:

```csharp
public interface IApplication
{
    /// <summary>
    /// Stack of running IRunnable sessions.
    /// Each entry is a SessionToken wrapping an IRunnable.
    /// </summary>
    ConcurrentStack<SessionToken>? SessionStack { get; }
    
    /// <summary>
    /// The IRunnable at the top of SessionStack (currently modal).
    /// </summary>
    IRunnable? TopRunnable { get; }
}
```

**Stack Behavior:**

- Push: [Begin()](xref:Terminal.Gui.App.IApplication.Begin*) adds to top of stack
- Pop: [End()](xref:Terminal.Gui.App.IApplication.End*) removes from stack; a session ended beneath the top stays until the sessions above it end
- Peek: <xref:Terminal.Gui.App.IApplication.TopRunnable> returns current modal runnable
- All: <xref:Terminal.Gui.App.IApplication.SessionStack> enumerates session tokens; running ones have `token.Runnable is { IsRunning: true }`

## IApplication Interface

The <xref:Terminal.Gui.App.IApplication> interface defines the application contract with support for both legacy `Runnable` and modern <xref:Terminal.Gui.App.IRunnable> patterns:

```csharp
public interface IApplication
{
    // IRunnable support (primary)
    IRunnable? TopRunnable { get; }
    View? TopRunnableView { get; }
    ConcurrentStack<SessionToken>? SessionStack { get; }
    
    // Driver and lifecycle
    IDriver? Driver { get; }
    IMainLoopCoordinator? Coordinator { get; }
    
    // Fluent API methods  
    IApplication Init (string? driverName = null);
    void Dispose (); // IDisposable
    
    // Runnable methods
    SessionToken? Begin (IRunnable runnable);
    object? Run (IRunnable runnable, Func<Exception, bool>? errorHandler = null);
    IApplication Run<TRunnable> (Func<Exception, bool>? errorHandler = null) where TRunnable : IRunnable, new();
    void RequestStop (IRunnable? runnable);
    void End (SessionToken sessionToken);
    
    // Result extraction
    object? GetResult ();
    T? GetResult<T> () where T : class;
    
    // ... other members
}
```

## Terminology Changes

Terminal.Gui v2 modernized its terminology for clarity:

### Application.TopRunnable (formerly "Current", and before that "Top")

The <xref:Terminal.Gui.App.IApplication.TopRunnable> property represents the <xref:Terminal.Gui.App.IRunnable> on the top of the session stack (the active runnable session):

```csharp
// Access the top runnable session
IRunnable? topRunnable = app.TopRunnable;

// From within a view  
IRunnable? topRunnable = App?.TopRunnable;

// Cast to View if needed
View? topView = app.TopRunnableView;
```

**Why "TopRunnable"?**
- Clearly indicates it's the top of the runnable session stack
- Aligns with the IRunnable architecture
- Distinguishes from other concepts like "Current" which could be ambiguous
- Works with any view that implements <xref:Terminal.Gui.App.IRunnable>, not just `Runnable`

### Application.SessionStack (formerly "Runnables")

The <xref:Terminal.Gui.App.IApplication.SessionStack> property is the stack of running sessions:

```csharp
// Access all running sessions
foreach (SessionToken runnable in app.SessionStack)
{
    // Process each session
}

// From within a view
var sessionCount = App?.SessionStack.Count ?? 0;
```

**Why "SessionStack" instead of "Runnables"?**
- Describes both content (sessions) and structure (stack)
- Aligns with <xref:Terminal.Gui.App.SessionToken> terminology
- Follows .NET naming patterns (descriptive + collection type)

## Migration from Static Application

The static <xref:Terminal.Gui.App.Application> class delegates to a singleton instance and is marked obsolete. All static methods and properties are marked with `[Obsolete]` but remain functional for backward compatibility:

```csharp
public static partial class Application
{
    [Obsolete ("The legacy static Application object is going away.")]
    public static View? TopRunnableView => Instance.TopRunnableView;
    
    [Obsolete ("The legacy static Application object is going away.")]
    public static IRunnable? TopRunnable => Instance.TopRunnable;
    
    [Obsolete ("The legacy static Application object is going away.")]
    public static ConcurrentStack<SessionToken>? SessionStack => Instance.SessionStack;
    
    // ... other obsolete static members
}
```

**Important:** The static <xref:Terminal.Gui.App.Application> class uses a singleton (`Application.Instance`), while [Application.Create()](xref:Terminal.Gui.App.Application.Create*) creates new instances. For new code, prefer the instance-based pattern using [Application.Create()](xref:Terminal.Gui.App.Application.Create*).

### Migration Strategies

**Strategy 1: Use View.App**

```csharp
// OLD:
void MyMethod ()
{
    Application.TopRunnable?.SetNeedsDraw ();
}

// NEW:
void MyMethod (View view)
{
    view.App?.TopRunnableView?.SetNeedsDraw ();
}
```

**Strategy 2: Pass IApplication**

```csharp
// OLD:
void ProcessSessions ()
{
    foreach (SessionToken runnable in Application.SessionStack)
    {
        // Process
    }
}

// NEW:
void ProcessSessions (IApplication app)
{
    foreach (SessionToken runnable in app.SessionStack)
    {
        // Process
    }
}
```

**Strategy 3: Store IApplication Reference**

```csharp
public class MyService
{
    private readonly IApplication _app;
    
    public MyService (IApplication app)
    {
        _app = app;
    }
    
    public void DoWork ()
    {
        _app.TopRunnable?.Title = "Processing...";
    }
}
```

## Resource Management and Disposal

Terminal.Gui v2 implements the `IDisposable` pattern for proper resource cleanup. Applications must be disposed after use to:
- Stop the input thread cleanly
- Release driver resources
- Prevent thread leaks in tests
- Free unmanaged resources

### Using the `using` Statement (Recommended)

```csharp
// Automatic disposal with using statement
using (IApplication app = Application.Create ().Init ())
{
    app.Run<MyDialog> ();
    // app.Dispose() automatically called when scope exits
}
```

### Manual Disposal

```csharp
// Manual disposal
IApplication app = Application.Create ();
try
{
    app.Init ();
    app.Run<MyDialog> ();
}
finally
{
    app.Dispose (); // Ensure cleanup even if exception occurs
}
```

### Dispose() and Result Retrieval

- **`Dispose()`** - Standard IDisposable pattern for resource cleanup (required)
- **[GetResult()](xref:Terminal.Gui.App.IApplication.GetResult*)** / **`GetResult<T>()`** - Retrieve results after run completes
- **[Shutdown()](xref:Terminal.Gui.App.Application.Shutdown*)** - Obsolete (use `Dispose()` instead)

```csharp
// RECOMMENDED (using statement):
using (IApplication app = Application.Create ().Init ())
{
    app.Run<MyDialog> ();
    MyResult? result = app.GetResult<MyResult> ();
    // app.Dispose() called automatically here
}

// ALTERNATIVE (manual disposal):
IApplication app = Application.Create ().Init ();
app.Run<MyDialog> ();
MyResult? result = app.GetResult<MyResult> ();
app.Dispose (); // Must call explicitly

// OLD (obsolete - do not use):
object? result = app.Run<MyDialog> ().Shutdown ();
```

### Input Thread Lifecycle

When calling [Init()](xref:Terminal.Gui.App.IApplication.Init*), Terminal.Gui starts a dedicated input thread that continuously polls for console input. This thread must be stopped properly:

```csharp
using Terminal.Gui.Drivers;

IApplication app = Application.Create ();
app.Init (DriverRegistry.Names.ANSI); // Input thread starts here

// Input thread runs in background at ~50 polls/second (20ms throttle)

app.Dispose (); // Cancels input thread and waits for it to exit
```

**Important for Tests**: Always dispose applications in tests to prevent thread leaks:

```csharp
using Terminal.Gui.Drivers;

[Fact]
public void My_Test ()
{
    using IApplication app = Application.Create ();
    app.Init (DriverRegistry.Names.ANSI);
    
    // Test code here
    
    // app.Dispose() called automatically
}
```

### Singleton Re-initialization

The legacy static <xref:Terminal.Gui.App.Application> singleton can be re-initialized after disposal (for backward compatibility with old tests):

```csharp
// Test 1
Application.Init ();
Application.Shutdown (); // Obsolete but still works for legacy singleton

// Test 2 - singleton resets and can be re-initialized
Application.Init (); // ✅ Works!
Application.Shutdown (); // Obsolete but still works for legacy singleton
```

However, instance-based applications follow standard `IDisposable` semantics and cannot be reused after disposal:

```csharp
IApplication app = Application.Create ();
app.Init ();
app.Dispose ();

app.Init (); // ❌ Throws ObjectDisposedException
```

## Session Management

### Begin and End

Applications manage sessions through [Begin()](xref:Terminal.Gui.App.IApplication.Begin*) and [End()](xref:Terminal.Gui.App.IApplication.End*):

```csharp
using IApplication app = Application.Create ();
app.Init ();

Window window = new ();

// Begin a new session - pushes to SessionStack
SessionToken? token = app.Begin (window);

// TopRunnable now points to this window
Debug.Assert (app.TopRunnable == window);

// End the session - pops from SessionStack
if (token != null)
{
    app.End (token);
}

// TopRunnable restored to previous runnable (if any)
```

### Nested Sessions

Multiple sessions can run nested:

```csharp
using IApplication app = Application.Create ();
app.Init ();

// Session 1
Window main = new () { Title = "Main" };
SessionToken? token1 = app.Begin (main);
// app.TopRunnable == main, SessionStack.Count == 1

// Session 2 (nested)
Dialog dialog = new () { Title = "Dialog" };
SessionToken? token2 = app.Begin (dialog);
// app.TopRunnable == dialog, SessionStack.Count == 2

// End dialog
app.End (token2);
// app.TopRunnable == main, SessionStack.Count == 1

// End main
app.End (token1);
// app.TopRunnable == null, SessionStack.Count == 0
```

## Driver Management

### Discovering Available Drivers

Terminal.Gui provides AOT-friendly methods to discover available drivers through the **Driver Registry**:

```csharp
// Get all registered driver names (no reflection)
IEnumerable<string> driverNames = Application.GetRegisteredDriverNames();

foreach (string name in driverNames)
{
    Debug.WriteLine($"Available driver: {name}");
}
// Output:
// Available driver: dotnet
// Available driver: windows
// Available driver: unix
// Available driver: ansi

// Get detailed driver information with metadata
foreach (var descriptor in Application.GetRegisteredDrivers())
{
    Debug.WriteLine($"{descriptor.Name}: {descriptor.DisplayName}");
    Debug.WriteLine($"  Description: {descriptor.Description}");
    Debug.WriteLine($"  Platforms: {string.Join(", ", descriptor.SupportedPlatforms)}");
}

// Validate a driver name (useful for CLI argument validation)
if (Application.IsDriverNameValid(userInput))
{
    app.Init(driverName: userInput);
}
else
{
    Console.WriteLine($"Invalid driver: {userInput}");
    Console.WriteLine($"Valid options: {string.Join(", ", Application.GetRegisteredDriverNames())}");
}
```

**Type-Safe Constants:**

Use `DriverRegistry.Names` for compile-time type safety:

```csharp
using Terminal.Gui.Drivers;

// Type-safe driver names
string ansi = DriverRegistry.Names.ANSI;      // "ansi"
string windows = DriverRegistry.Names.WINDOWS; // "windows"
string unix = DriverRegistry.Names.UNIX;       // "unix"
string dotnet = DriverRegistry.Names.DOTNET;   // "dotnet"

app.Init(driverName: DriverRegistry.Names.ANSI);
```

**Note**: The legacy `GetDriverTypes()` method is now obsolete. Use `GetRegisteredDriverNames()` or `GetRegisteredDrivers()` instead for AOT-friendly, reflection-free driver discovery. See [Drivers](drivers.md) for complete Driver Registry documentation.

### ForceDriver Configuration Property

The <xref:Terminal.Gui.App.IApplication.ForceDriver> property is a configuration property that allows you to specify which driver to use. It can be set via code or through the configuration system (e.g., `config.json`):

```csharp
using Terminal.Gui.Drivers;

// RECOMMENDED: Set on instance with type-safe constant
using (IApplication app = Application.Create ())
{
    app.ForceDriver = DriverRegistry.Names.ANSI;
    app.Init ();
}

// ALTERNATIVE: Set with string
using (IApplication app = Application.Create ())
{
    app.ForceDriver = "ansi";
    app.Init ();
}

// LEGACY: Set on static Application (obsolete)
Application.ForceDriver = DriverRegistry.Names.DOTNET;
Application.Init ();
```

**Valid driver names**: `"dotnet"`, `"windows"`, `"unix"`, `"ansi"`

For complete driver documentation including the Driver Registry pattern, see [Drivers](drivers.md).

### ForceDriverChanged Event

The static `Application.ForceDriverChanged` event is raised when the <xref:Terminal.Gui.App.IApplication.ForceDriver> property changes:

```csharp
using Terminal.Gui.Drivers;

// ForceDriverChanged event (on legacy static Application)
Application.ForceDriverChanged += (sender, e) =>
{
    Debug.WriteLine ($"Driver changed from '{e.OldValue}' to '{e.NewValue}'");
};

Application.ForceDriver = DriverRegistry.Names.ANSI;
```

## View.Driver Property

Similar to <xref:Terminal.Gui.ViewBase.View.App>, views now have a `Driver` property for accessing driver functionality.

```csharp
public override void OnDrawContent (Rectangle viewport)
{
    // Use view's driver instead of obsolete Application.Driver
    Driver?.Move (0, 0);
    Driver?.AddStr ("Hello");
}
```

**Note**: See [Drivers Deep Dive](drivers.md) for complete driver architecture details, including the organized interface structure with lifecycle, components, display, rendering, cursor, and input regions.

## Testing with the New Architecture

The instance-based architecture dramatically improves testability:

### Testing Views in Isolation

```csharp
[Fact]
public void MyView_DisplaysCorrectly ()
{
    // Create mock application
    Mock<IApplication> mockApp = new ();
    Runnable runnable = new ();
    mockApp.Setup (a => a.TopRunnable).Returns (runnable);
    
    // Create view with mock app
    MyView view = new () { App = mockApp.Object };
    
    // Test without Application.Init()!
    view.SetNeedsDraw ();
    Assert.True (view.NeedsDraw);
    
    // No disposal needed for mock!
}
```

### Testing with Real Application

```csharp
using Terminal.Gui.Drivers;

[Fact]
public void MyView_WorksWithRealApplication ()
{
    using (IApplication app = Application.Create ())
    {
        app.Init (DriverRegistry.Names.ANSI);
        
        MyView view = new ();
        Window top = new ();
        top.Add (view);
        
        SessionToken? token = app.Begin (top);
        
        // View.App automatically set
        Assert.NotNull (view.App);
        Assert.Same (app, view.App);
        
        // Test view behavior
        view.DoSomething ();
        
        if (token is { })
        {
            app.End (token);
        }
    }
}
```

## Best Practices

### DO: Use View.App

```csharp
// ✅ GOOD - Use View.App (modern instance-based pattern):
public void Refresh ()
{
    App?.TopRunnableView?.SetNeedsDraw ();
}
```

### DON'T: Use Static Application

```csharp
// ❌ AVOID - Obsolete static Application:
public void Refresh ()
{
    Application.TopRunnableView?.SetNeedsDraw (); // Obsolete!
}
```

### DO: Pass IApplication as Dependency

```csharp
// ✅ GOOD - Dependency injection:
public class Service
{
    private readonly IApplication _app;
    
    public Service (IApplication app)
    {
        _app = app;
    }
}
```

### DON'T: Use Static Application in New Code

```csharp
// ❌ AVOID - Obsolete static Application in new code:
public void Refresh ()
{
    Application.TopRunnableView?.SetNeedsDraw (); // Obsolete!
}

// ✅ PREFERRED - Use View.App property:
public void Refresh ()
{
    App?.TopRunnableView?.SetNeedsDraw ();
}
```

## Advanced Scenarios

### Multiple Applications

The instance-based architecture enables multiple applications:

```csharp
using Terminal.Gui.Drivers;

// Application 1
using (IApplication app1 = Application.Create ())
{
    app1.Init (DriverRegistry.Names.ANSI);
    Window top1 = new () { Title = "App 1" };
    // ... configure and run top1
}

// Application 2 (different driver!)
using (IApplication app2 = Application.Create ())
{
    app2.Init (DriverRegistry.Names.ANSI);
    Window top2 = new () { Title = "App 2" };
    // ... configure and run top2
}

// Views in top1 use app1
// Views in top2 use app2
```

## Inline Mode (`AppModel.Inline`)

Terminal.Gui supports two rendering modes controlled by <xref:Terminal.Gui.App.AppModel>:

| Mode | Buffer | Screen cleared? | Use case |
|------|--------|-----------------|----------|
| `FullScreen` (default) | Alternate screen (`CSI ?1049h`) | Yes | Traditional full-screen TUI |
| `Inline` | Primary scrollback buffer | No | CLI tools (like Claude Code, GitHub Copilot CLI) |

In inline mode the app renders below the current shell prompt, sizes itself by content, grows dynamically, and on exit leaves its output in scrollback history.

### Quick Start

```csharp
using IApplication app = Application.Create ();
app.AppModel = AppModel.Inline;
app.Init ();

// RunnableWrapper provides the inline container with Dim.Auto sizing
RunnableWrapper<ColorPicker> wrapper = new () { Width = Dim.Fill () };
app.Run (wrapper);
```

### Coordinate System: `App.Screen`

`IApplication.Screen` is the single source of truth for the rendering region:

| Property | FullScreen | Inline |
|----------|-----------|--------|
| `Driver.Screen` | Full terminal (e.g. `0,0,120,40`) | Full terminal — **never changes** |
| `App.Screen` | Same as `Driver.Screen` | Sub-rectangle (e.g. `0,25,120,8`) |
| `App.Screen.Y` | Always `0` | Terminal row where the inline region starts |
| `App.Screen.Height` | Terminal height | Rows the app currently occupies |

All coordinate offsetting derives from `App.Screen`:

- **Output**: `AnsiOutput.SetCursorPositionImpl` adds `App.Screen.Y` to row coordinates.
- **Mouse**: `ApplicationMouse.RaiseMouseEvent` subtracts `App.Screen.Y` from `ScreenPosition.Y`.
- **Views**: `Frame` is relative to `App.Screen`, so `Frame.Y == 0` is the top of the inline region, not the terminal.

### Startup Sequence

```
Init()
  ├─ AnsiOutput: skip CSI ?1049h (stay in primary buffer)
  ├─ AnsiSizeMonitor: send ESC[6n (CPR) to discover cursor row
  ├─ AnsiStartupGate: defer first render for CPR and pending capability queries
  ├─ DA1 reply: release optional kitty and color probe waits
  └─ OutputBufferImpl: cells start clean (IsDirty = false)

Begin(view)  /  first LayoutAndDraw()
  ├─ Layout pass with full terminal size → measure view's desired height
  ├─ If view doesn't fit below cursor → emit newlines to scroll terminal up
  ├─ Set App.Screen = (0, cursorRow, termWidth, viewHeight)
  └─ Reserve vertical space with newlines + CSI {n}A
```

The `AnsiStartupGate` waits for cursor position and any pending terminal size query before the first draw. Kitty keyboard and default-color probes also join the gate, but a DA1 reply releases their first-draw wait if they have not answered. The probes continue to accept late replies; a late color reply requests a redraw. If cursor position never responds, its 500 ms timeout lets rendering proceed from row 0. If DA1 and the optional probes never respond, their one-second timeouts let rendering proceed.

### Dynamic Growth

When content is added at runtime (e.g. items appended to a `ListView`), `LayoutAndDraw` detects when the view's desired height exceeds `App.Screen.Height`:

1. **Grow down** — use empty terminal rows below the current inline region.
2. **Grow up** — if no room below, emit newlines to scroll the terminal and adjust `App.Screen.Y`.
3. **Cap** — height never exceeds `Driver.Screen.Height`.

### Resize Handling

On terminal resize, the ANSI driver coalesces dimension changes until they have been quiet for
500 ms, scrolls the previous viewport into history, and reserves a fresh live region at row zero.
It does not clear the display/scrollback or use a cursor report to infer which old rows it may
erase. Old live frames and blank rows can remain in history; completed messages are not replayed.
Drawing, printing and caret updates pause during recovery; input and dispatch continue.
See [Terminal resize](inline-output.md#terminal-resize) for recovery and failure details.

### Output Buffer

In inline mode, `ClearContents()` initializes cells with `IsDirty = false`. Only cells explicitly drawn by the app are flushed — the rest of the visible terminal stays untouched. This is what prevents the app from overwriting the user's shell output.

### Clean Exit

On shutdown the cursor moves to `App.Screen.Y + App.Screen.Height` (the row below the rendered region) so the shell prompt appears naturally. `CSI ?1049l` (restore alternate buffer) is **not** emitted.

### Testing Inline Mode

`IApplication.ForceInlinePosition` bypasses the CPR query for unit tests:

```csharp
using IApplication app = Application.Create ();
app.AppModel = AppModel.Inline;
app.ForceInlinePosition = new Point (0, 10);  // simulate cursor at row 10
app.Init (DriverRegistry.Names.ANSI);
```

The `Y` component sets the terminal row. `X` is reserved for future use.

### `RunnableWrapper<TView>`

A convenience wrapper that runs any `View` as an `IRunnable` with `Dim.Auto` sizing — no dialog buttons added. Ideal for inline mode where the view IS the entire UI:

```csharp
RunnableWrapper<ColorPicker> wrapper = new () { Width = Dim.Fill () };
Color? result = app.Run (wrapper).Result;
```

### Examples

- See [tui-cs/Examples](https://github.com/tui-cs/Examples) for inline mode samples and additional application examples.

## See Also

- [Navigation](navigation.md) - Navigation with the instance-based architecture
- [Keyboard](keyboard.md) - Keyboard handling through View.App
- [Mouse](mouse.md) - Mouse handling through View.App  
- [Drivers](drivers.md) - Driver access through View.Driver
- [Multitasking](multitasking.md) - Session management with SessionStack

## Output above a live inline region

To print completed styled text above an inline UI, await `app.PrintAboveAsync(...)`.
See [Printing above an inline application](inline-output.md) for ordering, cancellation,
supported output, and partial-write behavior.

## Clearing a ListView

| Intent | API | Result |
|--------|-----|--------|
| Detach any data source | `listView.Source = null` | `Source` is `null` |
| Clear items via `SetSource` | `listView.SetSource<T>(null)` | Empty default collection data source **or** `Source == null` if the current source was not that default (see API remarks) |
| Clear items via `SetSourceAsync` | `listView.SetSourceAsync<T>(null)` | Empty default collection data source **or** `Source == null` if the current source was not that default (see API remarks) |
| Custom rendering / data | `listView.Source = myDataSource` | `Source` is your `IListDataSource` |

Do not treat these as synonyms.