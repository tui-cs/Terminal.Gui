---
uid: Terminal.Gui.Editor.Document.Folding
summary: Code-folding infrastructure for collapsing and expanding regions of a document.
---

Contains `FoldingManager` (manages fold state), `FoldingSection` (a single collapsible region), `IFoldingStrategy` (pluggable strategy interface), and built-in strategies (`BraceFoldingStrategy`, `XmlFoldingStrategy`).

Foldings auto-expand when the caret moves inside them. Consumers can implement custom `IFoldingStrategy` instances for language-specific folding rules.
