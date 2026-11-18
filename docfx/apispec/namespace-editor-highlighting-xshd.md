---
uid: Terminal.Gui.Editor.Highlighting.Xshd
summary: xshd file format support — loaders, savers, and AST types for XML Syntax Highlighting Definitions.
---

Contains `HighlightingLoader` (reads xshd XML into an `XshdSyntaxDefinition` and compiles it into an `IHighlightingDefinition`), `SaveXshdVisitor` (serializes back to XML), and the AST node types (`XshdRuleSet`, `XshdSpan`, `XshdRule`, `XshdKeywords`, `XshdColor`, etc.).

Supports both xshd v1 and v2 formats via `V1Loader` and `V2Loader`.
