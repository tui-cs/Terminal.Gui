---
uid: Terminal.Gui.Editor.Highlighting
summary: Syntax highlighting engine driven by xshd (XML Syntax Highlighting Definition) files.
---

The core types are `IHighlightingDefinition` (describes a language's highlighting rules), `DocumentHighlighter` (applies rules to a TextDocument producing `HighlightedLine`s), and `HighlightingManager` (registry of built-in and user-loaded definitions, with lookup by name or file extension).

Built-in definitions include C#, C++, Java, JavaScript, Python, PowerShell, TSQL, VB, JSON, HTML, XML, CSS, and Markdown. Highlight colors compose with the active Terminal.Gui color scheme.
