# Terminal.Gui Deep Dives 

![Terminal.Gui — cross-platform TUI toolkit for .NET. Build full-featured terminal UIs with menus, forms, tables, charts, wizards and file dialogs. 30+ views, Windows / macOS / Linux, MIT-licensed.](../images/hero.gif)

> [!IMPORTANT]
> Terminal.Gui v2 has been released

Welcome to the Terminal.Gui documentation! This comprehensive guide covers everything you need to know about building modern terminal user interfaces with Terminal.Gui.

## Getting Started

- [Getting Started](~/docs/getting-started.md) - Quick start guide to create your first Terminal.Gui application
- [Migrating from v1 to v2](~/docs/migratingfromv1.md) - Complete guide for upgrading existing applications
- [What's New in v2](~/docs/newinv2.md) - Overview of new features and improvements
- [Showcase](~/docs/showcase.md) - Showcase of TUI apps built with Terminal.Gui
- [Lexicon & Taxonomy](~/docs/lexicon.md) - Terminology and concepts used throughout Terminal.Gui
- [Views](~/docs/views.md) - Comprehensive list of built-in views and controls

## Deep Dives

- [ANSI Handling](~/docs/ansihandling.md) - Terminal escape sequence parsing, encoding, and state management
- [Agent Design Loop (Proposal)](~/docs/agent-design-loop.md) - Design for a gui-cs plugin-powered human+agent TUI co-design workflow
- [Application](~/docs/application.md) - Application lifecycle, initialization, and main loop
- [Arrangement](~/docs/arrangement.md) - View arrangement and positioning strategies
- [Borders](~/docs/borders.md) - Border rendering, tab headers, LineCanvas auto-join, and focus-aware styling
- [Cancellable Work Pattern](~/docs/cancellable-work-pattern.md) - Core design pattern for extensible workflows
- [Command](~/docs/command.md) - Command execution, key bindings, and the Activating/Accepting concepts
- [Command Diagrams](~/docs/command-diagrams.md) - Visual diagrams of command flow and processing
- [Configuration](~/docs/config.md) - Persistent settings, themes, and configuration scopes
- [Cursor](~/docs/cursor.md) - Terminal cursor management, positioning, and styles
- [Dim.Auto](~/docs/dimauto.md) - Automatic view sizing based on content
- [Drawing](~/docs/drawing.md) - Drawing primitives, rendering, and graphics operations
- [Drivers](~/docs/drivers.md) - Platform abstraction and console driver architecture
- [Events](~/docs/events.md) - Event patterns and handling throughout the framework
- [Input Injection](~/docs/input-injection.md) - Testing support for simulating keyboard and mouse input
- [Keyboard](~/docs/keyboard.md) - Key handling, bindings, commands, and shortcuts
- [Layout](~/docs/layout.md) - View positioning, sizing, and arrangement
- [Logging](~/docs/logging.md) - Comprehensive logging and performance monitoring
- [Menus](~/docs/menus.md) - Menu system architecture including MenuBar, MenuItem, and PopoverMenu
- [Mouse](~/docs/mouse.md) - Mouse event handling and interaction patterns
- [Multitasking](~/docs/multitasking.md) - Background operations and task management
- [Navigation](~/docs/navigation.md) - Focus management, keyboard navigation, and accessibility
- [Popovers](~/docs/Popovers.md) - Drawing outside viewport boundaries for menus and popups
- [Prompt](~/docs/prompt.md) - Interactive prompts and user input dialogs
- [Scheme](~/docs/scheme.md) - Color schemes, styling, and visual theming
- [Scrolling](~/docs/scrolling.md) - Built-in scrolling, virtual content areas, and scroll bars
- [TableView](~/docs/tableview.md) - Table view component, data binding, and column management
- [TreeView](~/docs/treeview.md) - Tree view component, hierarchical data, and node management
- [View](~/docs/View.md) - Base view class, view hierarchy, and core view functionality

## API Reference

For detailed API documentation, see the [API Reference](~/api/Terminal.Gui.ViewBase.yml).
