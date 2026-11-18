---
uid: Terminal.Gui.Editor
summary: A reusable text-editing View for Terminal.Gui with caret movement, selection, clipboard, undo/redo, search & replace, folding, syntax highlighting, word wrap, and multi-caret editing.
---

![Terminal.Gui.Editor (ted demo app)](https://raw.githubusercontent.com/tui-cs/Editor/develop/docs/images/hero.gif)

The `Editor` class is a `View` subclass that consumes the `TextDocument` through a cell-grid rendering pipeline (`VisualLineBuilder` → `CellVisualLine`, with pluggable `IVisualLineTransformer`s and `IBackgroundRenderer`s).

Ships as a single NuGet package: **[Terminal.Gui.Editor](https://www.nuget.org/packages/Terminal.Gui.Editor)**.

## Key Types

- **Editor** - The main editing View (keyboard, mouse, multi-caret, clipboard, undo/redo)
- **Gutter** - Line numbers and fold indicator gutter (a real View subview of Padding)
- **FindReplaceDialog** - Built-in find & replace dialog
- **EditorMenuBar** / **EditorStatusBar** - Reusable menu bar and status bar for editor apps

## See Also

- [Editor GitHub Repository](https://github.com/tui-cs/Editor)
