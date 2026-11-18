---
uid: Terminal.Gui.Editor.Rendering
summary: Cell-grid rendering pipeline that transforms document lines into visual output.
---

The pipeline flows: `VisualLineBuilder` → `CellVisualLine` (composed of `CellVisualLineElement`s such as `TextRunElement`, `TabElement`, `NewlineGlyphElement`, and `FoldingMarkerElement`).

## Extension Points

- **IVisualLineTransformer** — mutates element attributes (syntax highlighting, fold markers)
- **IBackgroundRenderer** — paints cell rectangles (selection, current line, search hits)
- **IOverlayRenderer** — draws overlays above the text (multi-caret indicators)

Visual lines are cached with LRU eviction and selectively invalidated from document change events.
