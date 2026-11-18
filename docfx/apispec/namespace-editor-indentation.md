---
uid: Terminal.Gui.Editor.Indentation
summary: Pluggable indentation strategies that control how the editor auto-indents new lines.
---

Contains the `IIndentationStrategy` interface and the built-in `DefaultIndentationStrategy` (copies the previous line's leading whitespace on Enter). Consumers can implement custom strategies for language-aware indentation (e.g., increasing indent after an opening brace).
