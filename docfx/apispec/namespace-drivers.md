---
uid: Terminal.Gui.Drivers
summary: Cross-platform terminal abstraction, console drivers, and ANSI handling.
---

The `Drivers` namespace provides the platform abstraction layer enabling Terminal.Gui to run consistently across Windows, macOS, and Linux/Unix.

## Key Types

- **IDriver** - Main driver interface for terminal operations
- **DriverRegistry** - Registry of available drivers with metadata
- **AnsiResponseParser** - ANSI escape sequence parsing for input
- **AnsiKeyboardParser** / **AnsiMouseParser** - Keyboard and mouse input parsing
- **EscSeqUtils** - ANSI escape sequence constants and utilities
- **Cursor** / **CursorStyle** - Terminal cursor management
- **IOutput** / **IOutputBuffer** - Output surfaces, including raster image dispatch

## Available Drivers

| Driver | Platform | Description |
|--------|----------|-------------|
| `windows` | Windows | Native Win32 Console API with highest performance |
| `unix` | Unix/Linux/macOS | Direct syscalls with ANSI sequences |
| `dotnet` | All | Cross-platform using `System.Console` |
| `ansi` | All | Pure ANSI implementation, ideal for testing |

## Driver Selection

```csharp
// Automatic (recommended)
app.Init ();  // Selects best driver for platform

// Explicit selection
app.Init (DriverRegistry.Names.ANSI);

// Via configuration
app.ForceDriver = DriverRegistry.Names.UNIX;
app.Init ();
```

## Raster Graphics Capabilities

Drivers expose raster protocol detection through `IDriver.KittyGraphicsSupport` and `IDriver.SixelSupport`, plus matching change events. Kitty graphics is the preferred raster protocol when supported. Sixel remains the fallback for terminals that do not support Kitty graphics. `IOutput.UseKittyGraphics` selects the Kitty output path; when it is false and Sixel is supported, raster commands are emitted as Sixel.

## See Also

- [Drivers Deep Dive](~/docs/drivers.md)
- [ANSI Handling Deep Dive](~/docs/ansihandling.md)
- [Cursor Management](~/docs/cursor.md)

## Threading Model

The driver architecture uses a multi-threaded design:

- **Input Thread**: Asynchronously reads console input without blocking the UI
- **Main UI Thread**: Processes events, performs layout, and renders output

This separation ensures responsive input handling even during intensive UI operations.

## Deep Dive

See the [Cross-Platform Driver Model](~/docs/drivers.md) for comprehensive details. 