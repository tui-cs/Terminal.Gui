# Terminal.Gui

Cross-platform UI toolkit for building sophisticated terminal UI (TUI) applications on Windows, macOS, and Linux/Unix.

![Terminal.Gui — cross-platform TUI toolkit for .NET. Build full-featured terminal UIs with menus, forms, tables, charts, wizards and file dialogs. 30+ stars, Windows / macOS / Linux, MIT-licensed.](~/images/hero.gif)

# Version 2.0 Has Been Released

Terminal.Gui enables building sophisticated console applications with modern UIs:

- **Responsive TUI** - Easy to use, innovative, layout system enables console apps as responsive as any responsive web page.
- **Performant and Scalable** - Built for modern TUIs - fast, double-buffering-based rendering; Tables and Tree Views scale to infinite elements with sorting and filtering.
- **Keyboard First; Mouse First Too** - Optimized for TUI experiences where the user's hands never need to leave the keyboard; full mouse support too.
- **Rich Built-in Widgets (Views)** - Text editors, buttons, checkboxes, trees, tables, markdown, linear ranges, menus, selectors, and more.
- **Visualizations** - Charts, graphs, progress indicators, and color pickers with TrueColor support.
- **Text Editors** - Full-featured text editing with clipboard, undo/redo, and Unicode support
- **Fully Configurable** - Themes, colors, key bindings, and settings are all customizable and persistable.
- **File Management** - File and directory browsers with search and filtering, supporting Nerdfonts and coloring.
- **Wizards and Multi-Step Processes** - Guided workflows with navigation and validation.
- **Cross-Platform** - Consistent experience on Windows, macOS, and Linux.
- **Apps Work In-line or Full Screen** - Build CLI tools like Claude Code/Copilot/Codex CLI that scroll with the terminal (in-line) or full screen.

See the [Views Overview](https://tui-cs.github.io/Terminal.Gui/docs/views) for available controls and [What's New in v2](https://tui-cs.github.io/Terminal.Gui/docs/newinv2) for architectural improvements.

## Quick Start

Install the [Terminal.Gui.Templates](https://github.com/tui-cs/Terminal.Gui.templates), create a new TUI app, and run it:

```powershell
dotnet new install Terminal.Gui.Templates
dotnet new tui-simple -n myproj
cd myproj
dotnet run
```

## Simple Example

```csharp
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

using IApplication app = Application.Create ();
app.Init ();

using Window window = new () { Title = "Hello World (Esc to quit)" };
Label label = new ()
{
    Text = "Hello, Terminal.Gui v2!",
    X = Pos.Center (),
    Y = Pos.Center ()
};
window.Add (label);

app.Run (window);
```

See the [Examples](https://github.com/tui-cs/Terminal.Gui/tree/develop/Examples) directory for more.

# Documentation 

Comprehensive documentation is at [tui-cs.github.io/Terminal.Gui](https://tui-cs.github.io/Terminal.Gui).

## Getting Started

- **[Getting Started Guide](https://tui-cs.github.io/Terminal.Gui/docs/getting-started)** - First Terminal.Gui application
- **[API Reference](https://tui-cs.github.io/Terminal.Gui/api/Terminal.Gui.App.html)** - Complete API documentation
- **[What's New in v2](https://tui-cs.github.io/Terminal.Gui/docs/newinv2)** - New features and improvements

## Migration & Deep Dives

- **[Migrating from v1 to v2](https://tui-cs.github.io/Terminal.Gui/docs/migratingfromv1)** - Complete migration guide
- **[Application Architecture](https://tui-cs.github.io/Terminal.Gui/docs/application)** - Instance-based model and IRunnable pattern
- **[Layout System](https://tui-cs.github.io/Terminal.Gui/docs/layout)** - Positioning, sizing, and adornments
- **[Keyboard Handling](https://tui-cs.github.io/Terminal.Gui/docs/keyboard)** - Key bindings and commands
- **[View Documentation](https://tui-cs.github.io/Terminal.Gui/docs/View)** - View hierarchy and lifecycle
- **[Configuration](https://tui-cs.github.io/Terminal.Gui/docs/config)** - Themes and persistent settings

See the [documentation index](https://tui-cs.github.io/Terminal.Gui/docs/index) for all topics.
