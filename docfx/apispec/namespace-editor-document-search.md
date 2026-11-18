---
uid: Terminal.Gui.Editor.Document.Search
summary: Pluggable search strategies for find and replace operations.
---

Contains `ISearchStrategy` (the strategy interface), built-in implementations (`RegexSearchStrategy` and normal/whole-word string search), and `SearchStrategyFactory` for constructing strategies by mode and options.

Search strategies operate on `ITextSource` and return match results as offset/length pairs. The editor's find/replace UI and `SearchHitRenderer` consume these results.
