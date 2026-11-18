# CharMap Refactoring Plan: Move Headers to Padding

## Problem Statement

CharMap currently has a workaround where it sets `ViewportSettings |= ViewportSettingsFlags.AllowLocationPlusSizeGreaterThanContentSize` because it implements fixed row/column headers using custom drawing logic within `OnDrawingContent`. This fights against the default viewport clamping behavior.

The proper solution is to move the headers into the `Padding` adornment, which is the idiomatic Terminal.Gui pattern for fixed UI elements that surround scrollable content.

## Current Architecture

```
┌─────────────────────────────────────────────┐
│ Viewport (custom drawing)                   │
│ ┌─────────┬─────────────────────────────────┤
│ │         │  0  1  2  3  4  5 ... F  (header)
│ ├─────────┼─────────────────────────────────┤
│ │U+02500_ │  ─  ━  │  ┃  ┄  ...    (glyphs) │
│ │U+02510_ │  ┐  ┑  ┒  ┓  └  ...             │
│ │  ...    │  ...                            │
│ └─────────┴─────────────────────────────────┤
└─────────────────────────────────────────────┘
```

**Issues with current approach:**
1. Row labels (`U+XXXXX_`) are drawn in `OnDrawingContent` but don't scroll
2. Column headers (`0 1 2 ... F`) are drawn in `OnDrawingContent` but don't scroll  
3. Custom `RowLabelWidth` calculations throughout the code
4. `HorizontalScrollBar.X = RowLabelWidth` to offset scrollbar
5. `HorizontalScrollBar.ScrollableContentSize = GetContentSize().Width - RowLabelWidth`
6. Requires `AllowLocationPlusSizeGreaterThanContentSize` flag to work properly
7. Complex coordinate calculations in `GetCursor()`, `UpdateCursor()`, `ScrollToMakeCursorVisible()`

## Proposed Architecture

```
┌─────────────────────────────────────────────┐
│ Padding.Top (column headers)                │
│    0  1  2  3  4  5  6  7  8  9  A  B  C  D │
├─────────┬───────────────────────────────────┤
│ Padding │ Viewport (glyphs only)            │
│  .Left  │  ─  ━  │  ┃  ┄  ┅  ┆  ┇  ┈  ┉  ┊ │
│U+02500_ │  ┐  ┑  ┒  ┓  └  ┕  ┖  ┗  ┘  ┙  ┚ │
│U+02510_ │  ...                              │
│  ...    │                                   │
├─────────┴───────────────────────────────────┤
│ Padding.Bottom (horizontal scrollbar)       │
└─────────────────────────────────────────────┘
```

**Benefits:**
1. Headers are in Padding - they don't scroll with content
2. Viewport contains ONLY the glyph grid
3. Content size is simply `16 * COLUMN_WIDTH` × `visibleRows * rowHeight`
4. No need for `AllowLocationPlusSizeGreaterThanContentSize` flag
5. Scrollbars naturally position themselves correctly
6. Simpler coordinate calculations
7. Follows Terminal.Gui patterns (like how Border contains title)

## Implementation Steps

### Phase 1: Set Up Padding Structure

1. **Configure Padding thickness**
   ```csharp
   Padding.Thickness = new Thickness(RowLabelWidth, HEADER_HEIGHT, 0, 0);
   ```

2. **Create header drawing in Padding**
   - Subscribe to `Padding.DrawingContent` event
   - Or create a custom `CharMapPadding` class that overrides `OnDrawingContent`
   - Draw column headers in top area
   - Draw row labels in left area

### Phase 2: Simplify Content Area

1. **Update `SetContentSize()` calls**
   - Remove `RowLabelWidth` from width calculation
   - Remove `HEADER_HEIGHT` from height calculation
   - New: `SetContentSize(new Size(16 * COLUMN_WIDTH, visibleRows * rowHeight))`

2. **Simplify `OnDrawingContent()`**
   - Remove header drawing code (lines ~620-651)
   - Remove row label drawing code (lines ~670-687)
   - Only draw the glyph grid
   - Coordinates become simpler: `x = col * COLUMN_WIDTH`, `y = visibleRow * rowHeight`

### Phase 3: Update Scrollbar Configuration

1. **Remove custom scrollbar positioning**
   - Remove `HorizontalScrollBar.X = RowLabelWidth`
   - Remove custom `ScrollableContentSize` adjustments
   - Let scrollbars auto-position based on Padding

2. **Remove ViewportSettings workaround**
   - Remove `ViewportSettings |= ViewportSettingsFlags.AllowLocationPlusSizeGreaterThanContentSize`

### Phase 4: Simplify Coordinate Calculations

1. **Update `GetCursor()`**
   - Remove `RowLabelWidth` offset from X calculation
   - Remove `HEADER_HEIGHT` offset from Y calculation
   - Simply: `x = (codePoint % 16) * COLUMN_WIDTH - Viewport.X`
   - Simply: `y = visibleRowIndex * rowHeight - Viewport.Y`

2. **Update `UpdateCursor()`**
   - Simplify bounds checking (no RowLabelWidth offset)

3. **Update `ScrollToMakeCursorVisible()`**
   - Remove header offsets from scroll calculations

### Phase 5: Handle Synchronized Scrolling

The row labels need to scroll vertically (but not horizontally) with the content.
The column headers need to scroll horizontally (but not vertically) with the content.

**Option A: Redraw on ViewportChanged**
- Subscribe to `ViewportChanged`
- Call `Padding.SetNeedsDraw()` to redraw headers with current scroll position
- Pass `Viewport.X` to column header drawing
- Pass `Viewport.Y` to row label drawing

**Option B: Use SubViews in Padding**
- Add a custom View for column headers in `Padding`
- Add a custom View for row labels in `Padding`
- Bind their content offset to main view's Viewport

### Phase 6: Update Tests

1. Review and update any tests that depend on current coordinate system
2. Add tests for header visibility during scrolling
3. Verify cursor positioning works correctly

## Files to Modify

- `Terminal.Gui/Views/CharMap/CharMap.cs` - Main refactoring
- `Tests/UnitTestsParallelizable/Views/CharMapTests.cs` - If exists, update tests

## Risks and Considerations

1. **Breaking change for subclasses** - If anyone subclasses CharMap and overrides drawing
2. **Padding event timing** - Ensure Padding draws at correct time relative to content
3. **Mouse hit testing** - Ensure clicks on headers are handled correctly
4. **Focus/Active styling** - Headers currently use `VisualRole.Focus/Active` for highlighting

## Alternative Approaches Considered

1. **Keep current approach with flag** - Works but is a workaround, not idiomatic
2. **Use Border instead of Padding** - Border is for decorations, Padding is for functional UI
3. **Create separate header Views as siblings** - More complex layout management

## References

- `View.Padding` - The adornment where headers should live
- `ScrollBar` positioning in Padding - Example of content in Padding
- `Border.Title` drawing - Example of custom drawing in adornments
- `Adornment.OnDrawingContent` - How to customize adornment drawing
