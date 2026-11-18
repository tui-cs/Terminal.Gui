---
uid: Terminal.Gui.Editor.Document
summary: UI-framework-independent document model — rope-backed TextDocument, DocumentLine, TextAnchor, UndoStack, ITextSource, TextSegment, and supporting types.
---

The document layer has no dependency on Terminal.Gui and can be used independently for text manipulation, analysis, or testing. It is adapted from [AvaloniaEdit](https://github.com/AvaloniaUI/AvaloniaEdit)'s pure-data layers.

## Key Types

- **TextDocument** - The rope-backed document (efficient insert/delete at any position)
- **DocumentLine** - Represents a single line in the document
- **TextAnchor** - A position that tracks across edits
- **UndoStack** - Undo/redo with compound grouping
- **ITextSource** - Read-only text source abstraction
- **TextSegment** - Offset+length region in the document
