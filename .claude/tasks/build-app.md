# Building a Terminal.Gui Application

> **For AI agents helping users build apps with Terminal.Gui.**
> This guide focuses on app development, not library contribution.

## Quick Assessment

When a user says "I want to build a terminal.gui app that does XYZ":

1. **Is this a new project?** Use the templates (see Setup below)
2. **Adding to existing project?** Add NuGet package
3. **What UI patterns are needed?** See Common Patterns below

## Project Setup

### New Project (Recommended)
```bash
dotnet new install Terminal.Gui.Templates@2.*
dotnet new tui-simple -n ProjectName
cd ProjectName
dotnet run
```

### Existing Project
```bash
dotnet add package Terminal.Gui
```

### Required Namespaces
```csharp
using Terminal.Gui.App;          // Application, IApplication
using Terminal.Gui.Views;        // All controls (Button, Label, etc.)
using Terminal.Gui.ViewBase;     // View, Pos, Dim
using Terminal.Gui.Drawing;      // Colors, Attribute, LineStyle
using Terminal.Gui.Input;        // Key, KeyCode, MouseFlags
using Terminal.Gui.Configuration; // ConfigurationManager (optional)
```

## Application Structure

### Modern Pattern (Recommended)
```csharp
using Terminal.Gui.App;
using Terminal.Gui.Views;

// Create and initialize application
IApplication app = Application.Create ().Init ();

// Run the main window
app.Run<MainWindow> ();

// Clean up
app.Dispose ();

// Main window class
public sealed class MainWindow : Runnable
{
    public MainWindow ()
    {
        Title = "My App (Esc to quit)";

        // Add controls here
        Button button = new () { Text = "Click Me", X = Pos.Center (), Y = Pos.Center () };
        button.Accepted += (_, _) =>
        {
            MessageBox.Query (App!, "Hello", "Button clicked!", "OK");
        };

        Add (button);
    }
}
```

### With Return Value
```csharp
// Usage:
IApplication app = Application.Create ().Init ();
string? username = app.Run<LoginWindow> ().GetResult<string> ();
app.Dispose ();

public sealed class LoginWindow : Runnable<string?>
{
    public LoginWindow ()
    {
        TextField usernameField = new () { X = 1, Y = 1, Width = 20 };
        Button loginButton = new () { Text = "Login", X = 1, Y = 3 };

        loginButton.Accepted += (_, _) =>
        {
            Result = usernameField.Text;  // Set return value
            App!.RequestStop ();          // Close window
        };

        Add (usernameField, loginButton);
    }
}
```

## Layout System

### Position (Pos)
```csharp
X = 5;                           // Absolute: 5 from left
X = Pos.Center ();               // Centered horizontally
X = Pos.Right (otherView) + 1;   // 1 right of another view
X = Pos.Left (otherView);        // Aligned with left of another view
X = Pos.Percent (25);            // 25% from left
X = Pos.AnchorEnd (10);          // 10 from right edge
```

### Size (Dim)
```csharp
Width = 20;                      // Absolute: 20 characters
Width = Dim.Fill ();             // Fill remaining width
Width = Dim.Fill (1);            // Fill minus 1 character margin
Width = Dim.Auto ();             // Size to content
Width = Dim.Percent (50);        // 50% of container
Width = Dim.Width (otherView);   // Same width as another view
```

## API Reference

Consult these compressed API files for available types:

| File | Contents |
|------|----------|
| `docfx/apispec/namespace-app.md` | Application, IApplication, Clipboard |
| `docfx/apispec/namespace-views.md` | All UI controls (Button, Label, etc.) |
| `docfx/apispec/namespace-viewbase.md` | View, Pos, Dim, Adornments |
| `docfx/apispec/namespace-drawing.md` | Colors, LineStyle, Attribute |
| `docfx/apispec/namespace-input.md` | Key, KeyCode, Mouse handling |
| `docfx/apispec/namespace-text.md` | Text manipulation, autocomplete |

## Common Patterns

See `.claude/cookbook/common-patterns.md` for recipes including:
- Form with validation
- List with selection
- Menu bar and dialogs
- Split views and tabs
- File dialogs
- Progress indicators

## Examples to Study

| Example | Location | Description |
|---------|----------|-------------|
| All Controls | `Examples/UICatalog/` | Comprehensive demo app |
| Scenario automation | `Examples/ScenarioRunner/` | Run UICatalog scenarios from the CLI |
| Additional samples | `tui-cs/Examples` | Standalone example applications |

## Event Handling Patterns

### Button Click
```csharp
// Simple side-effect handler — use Accepted (post-event)
button.Accepted += (_, _) =>
{
    // Handle the click
};

// Use Accepting (pre-event) ONLY to inspect or cancel the in-flight action
button.Accepting += (_, e) =>
{
    if (usernameField.Text.Length == 0)
    {
        e.Handled = true;  // Cancel — prevents Accepted from firing
    }
};
```

### Text Changed
```csharp
textField.TextChanged += (_, _) =>
{
    // React to text changes
};
```

### Selection Changed
```csharp
// Typed views expose their data via IValue<T> — ListView is IValue<int?> (the selected index)
listView.ValueChanged += (_, e) =>
{
    int? selectedIndex = e.NewValue;
};
```

### Keyboard Shortcuts
```csharp
// In your View subclass: add a command handler, then bind a key to it.
// (AddCommand is protected — call it from inside the view, not on an instance.)
AddCommand (Command.Refresh, () =>
{
    // Reload data here

    return true;
});
KeyBindings.Add (Key.F5, Command.Refresh);
```

## Dialogs

### Message Box
```csharp
// MessageBox makes the last button the default (Enter-activated), so put affirmative last.
int? result = MessageBox.Query (App!, "Title", "Message", "No", "Yes");
// result: 0 = No, 1 = Yes, null = dismissed without choosing
```

### Error Dialog
```csharp
MessageBox.ErrorQuery (App!, "Error", "Something went wrong", "OK");
```

### Custom Dialog

The last `Dialog` button added is the default (Enter-activated), just like `MessageBox`. Add `Cancel` before `OK` and avoid setting `IsDefault` manually unless you intentionally override that default.

```csharp
Dialog dialog = new ()
{
    Title = "Custom Dialog",
    Width = 40,
    Height = 10,
    // Dialog makes the last button the default; order cancel/destructive choices first.
    Buttons = [new Button { Text = "Cancel" }, new Button { Text = "OK" }]
};
// Add controls to dialog...
app.Run (dialog);
```

### File Dialogs
```csharp
OpenDialog openDialog = new () { Title = "Open File" };
app.Run (openDialog);
if (!openDialog.Canceled)
{
    string path = openDialog.FilePaths.First ();
}
```

## Styling

### Border Styles
```csharp
view.BorderStyle = LineStyle.Rounded;    // Rounded corners
view.BorderStyle = LineStyle.Single;     // Single line
view.BorderStyle = LineStyle.Double;     // Double line
view.BorderStyle = LineStyle.None;       // No border
```

### Colors (via Themes)
```csharp
// Set theme at startup
TuiConfigurationBuilder config = new ();
config.RuntimeConfig = """{ "Theme": "Dark" }""";
config.ApplyToStaticFacades ();
```

Available themes: `Default`, `Dark`, `Light`, `Amber Phosphor`, `Green Phosphor`, `Blue Phosphor`

## Verify Your App Actually Works (Give Yourself Eyes)

You cannot see a TUI from a build log. Before declaring an app done, **run it and observe it** with [`tuirec`](https://github.com/tui-cs/tuirec) — it spawns the app in a PTY, injects keystrokes, and records the terminal output:

```powershell
dotnet build -c Release
$ks = 'wait:1000,Tab,Enter,wait:800,Escape'

tuirec record `
    --binary dotnet `
    --args "./bin/Release/net10.0/MyApp.dll" `
    --name MyApp `
    --keystrokes $ks `
    --startup-delay 2000 --drain 1500 `
    --cols 120 --rows 30
```

Then **verify the output yourself**:

1. **Read `artifacts/MyApp.cast`** — it is asciinema v2 JSON (plain text). Inspect the frames to confirm the UI rendered what you expect (controls visible, focus moved, dialog appeared).
2. Grep the cast for failures: `Select-String -Path artifacts/MyApp.cast -Pattern "error|exception|usage:"`.
3. Check `artifacts/MyApp.gif` exists and is > 100KB (a blank recording is typically < 50KB).

See [Scripts/tuirec/README.md](../../Scripts/tuirec/README.md) for the full keystroke syntax, validation checklist, and troubleshooting table. For in-process assertions (no PTY), use `InputInjector` and `VirtualTimeProvider` — see `docfx/docs/input-injection.md`.

## Checklist for Building Apps

- [ ] Project setup with correct packages
- [ ] Main window class inheriting from `Runnable` or `Runnable<T>`
- [ ] Application lifecycle: Create -> Init -> Run -> Dispose
- [ ] Layout using Pos/Dim (not hardcoded positions)
- [ ] `-ed` events (`Accepted`) for side effects; `-ing` events (`Accepting`) only to cancel
- [ ] Proper cleanup with `Dispose` pattern
- [ ] Behavior verified by running the app (tuirec recording or input injection), not just by compiling

## What NOT to Do

- Don't use `Application.Init()` / `Application.Shutdown()` (legacy static API)
- Don't hardcode sizes - use `Dim.Fill()`, `Dim.Auto()`, `Dim.Percent()`
- Don't use `Accepting` for fire-and-forget side effects - use `Accepted`; reserve `Accepting` (with `e.Handled = true`) for canceling
- Don't block the main thread - use `Application.AddTimeout` for async work
