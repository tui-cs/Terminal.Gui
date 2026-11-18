---
uid: Terminal.Gui.Editor.Document.Utils
summary: Shared utility types used by the document layer — Rope, Deque, CompressingTreeList, FileReader, and text helpers.
---

Includes the `Rope<T>` data structure (a balanced B-tree for efficient insert/delete in large sequences), `Deque<T>` (double-ended queue), `CompressingTreeList<T>` (run-length compressed list), `FileReader` (encoding-detecting file reader), `IFreezable` (immutability pattern), and various string/text helpers.

These types are adapted from [AvaloniaEdit](https://github.com/AvaloniaUI/AvaloniaEdit)'s utility layer and have no dependency on Terminal.Gui.
