---
uid: Terminal.Gui.App
summary: Application lifecycle, initialization, and core runtime services.
---

The `App` namespace provides the application entry point and core runtime infrastructure for Terminal.Gui applications.

## Key Types

- **Application** - Static gateway class providing the main entry point
- **IApplication** - Instance-based application interface for testability and multiple instances
- **ApplicationNavigation** - Focus management and cursor positioning
- **Clipboard** - Cross-platform clipboard access

## Instance-Based Architecture

Terminal.Gui v2 uses an instance-based architecture where `Application` is a static gateway to `IApplication` instances:

```csharp
// Recommended: Instance-based with automatic cleanup
using IApplication app = Application.Create ();
app.Init ();

using Window window = new () { Title = "Hello (Esc to quit)" };
app.Run (window);
```

```csharp
// Legacy: Static API (supported, but not recommended)
Application.Init ();
Window window = new () { Title = "Hello (Esc to quit)" };
Application.Run (window);
window.Dispose ();
Application.Shutdown ();
```

## See Also

- [Application Deep Dive](~/docs/application.md)
- [Navigation Deep Dive](~/docs/navigation.md)
