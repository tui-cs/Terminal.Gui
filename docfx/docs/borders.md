# Borders Deep Dive

[Border](~/api/Terminal.Gui.ViewBase.Border.yml) is the adornment that draws the visual frame, title, and tab header for a [View](~/api/Terminal.Gui.ViewBase.View.yml). It is one of the three adornment layers (Margin → Border → Padding) that surround a View's content area.

This deep dive covers Border's rendering modes, the tab header system, and how `LineCanvas` auto-join produces flowing connected tab styles.

## Table of Contents

- [Border Basics](#border-basics)
- [Title Rendering by Thickness](#title-rendering-by-thickness)
- [Tab Style Borders](#tab-style-borders)
- [Tab Header Geometry](#tab-header-geometry)
- [Focus and Attributes](#focus-and-attributes)
- [Tab Offset and Clipping](#tab-offset-and-clipping)
- [Auto-Join with SuperViewRendersLineCanvas](#auto-join-with-superviewrenderslinecanvas)
- [Border Line Positioning](#border-line-positioning)
- [Arrangement (Move and Resize)](#arrangement-move-and-resize)

---

## Border Basics

Every [View](~/api/Terminal.Gui.ViewBase.View.yml) has a `Border` adornment accessible via `View.Border`. The border's appearance is controlled by:

- **`View.BorderStyle`** ([BorderStyle](~/api/Terminal.Gui.ViewBase.View.BorderStyle.yml)) — Helper property that sets `Border.LineStyle`, `Border.Settings`, and `Border.Thickness` to common presets for different line styles. 
- **`View.Border.Settings`** ([BorderSettings](~/api/Terminal.Gui.ViewBase.BorderSettings.yml)) — Flags controlling title and tab rendering.
- **`View.Border.LineStyle`** ([LineStyle](~/api/Terminal.Gui.ViewBase.Border.LineStyle.yml)) — Which line-drawing characters to use for the border.
- **`View.Border.Thickness`** ([Thickness](~/api/Terminal.Gui.ViewBase.IAdornment.Thickness.yml)) — How many rows/columns each side occupies.

When `BorderStyle` is set to a non-`None` value, it implicitly sets `Border.Settings` to include `BorderSettings.Title`, enabling title rendering based on the thickness of the top border.

The border is rendered by [BorderView](~/api/Terminal.Gui.ViewBase.BorderView.yml), the internal `AdornmentView` created when `Border.GetOrCreateView()` is called (or implicitly when `BorderStyle` is set).

---

## Title Rendering by Thickness

When `BorderSettings.Default | BorderSettings.Title` is set, the `Thickness` on the title side determines how many rows (or columns) the border occupies and how the title is rendered within that space.

### `Thickness.Top == 1` — Title Inline on Border Line

The title sits directly on the single top border line with `┤` and `├` connectors:

```
┌┤Title├──┐
│         │
└─────────┘
```

### `Thickness.Top == 2` — Title with Cap Line (No Closing Edge)

Two rows: a cap line above the title, then the main border line with the title. Corner connectors (`┘`/`└`) terminate — there is no closing line:

```
 ╭─────╮
╭┘Title└──╮
│         │
╰─────────╯
```

### `Thickness.Top == 3` — Title in Enclosed Rectangle

Three rows: top cap, title row, and a closing line. T-junction connectors (`┤`/`├`) continue through:

```
 ╭─────╮
╭┤Title├──╮
│╰─────╯  │
│         │
╰─────────╯
```

### `Thickness.Top ≥ 4` — Same as 3 with Extra Space

Identical rendering to thickness 3, with additional empty rows above. The title rectangle is the same shape, just positioned higher.

---

## Tab Style Borders

When `Border.Settings` includes `BorderSettings.Tab`, the border renders a **tab header** — a small rectangle containing the View's `Title` that protrudes from one side of the content border. This is the foundation for building tabbed interfaces.

### Enabling Tab Style

```csharp
view.BorderStyle = LineStyle.Rounded;
view.Border.Settings = BorderSettings.Tab | BorderSettings.Title;
view.Border.TabSide = Side.Top;
view.Border.TabOffset = 0;
view.Border.Thickness = new Thickness (1, 3, 1, 1); // 3 on the tab side
```

### Key Properties

| Property | Type | Description |
|----------|------|-------------|
| `Border.Settings` | `BorderSettings` | Must include `BorderSettings.Tab` to enable tab rendering |
| `BorderView.TabSide` | `Side` | Which side the tab header appears on (`Top`, `Bottom`, `Left`, `Right`) |
| `BorderView.TabOffset` | `int` | Offset along the tab side where the header starts (can be negative) |
| `BorderView.TabLength` | `int?` | Total length of the tab including borders. `null` = auto-compute from `Title` |
| `BorderView.TitleView` | `View?` | The `View` rendering the tab title (for custom mouse handling) |
| `Tabs.TabSpacing` | `int` | Gap between adjacent tab headers. `-1` = shared edge (default), `0` = edge-to-edge, `1+` = gap |

When both `Tab` and `Title` are set, `TabLength` auto-computes as `Title.GetColumns() + 2` (title text width + two border columns). When only `Tab` is set without `Title`, `TabLength` defaults to `2` (just the border columns, no text).

---

## Tab Header Geometry

The tab-side thickness determines the **depth** of the header (`depth = sideThickness`). The `TitleView`'s border thickness caps its visual structure at depth ≥ 3 (cap line + title + optional closing edge), but the header is positioned `depth - 1` cells outward from the content border, so thickness > 3 adds empty space between the header and the content border.

| Title-Side Thickness | Depth | Header Structure |
|----------------------|-------|------------------|
| 1 | 1 | No header (content border line only) |
| 2 | 2 | Cap line + title row |
| 3 | 3 | Cap line + title row + closing edge (focus-toggled) |
| 4+ | N | Same structure as 3, with extra space between header and content |

### Visual Examples by Side (Thickness = 3, Depth = 2)

All examples use `BorderStyle = Rounded`, `TabOffset = 0`.

#### `Side.Top`

**Unfocused** (closed — header closing line drawn):
```
╭───╮
│Tab│
├───┴───╮
│content│
╰───────╯
```

**Focused** (open — header closing line suppressed):
```
╭───╮
│Tab│
│   ╰───╮
│content│
╰───────╯
```

#### `Side.Bottom`

**Unfocused:**
```
╭───────╮
│content│
├───┬───╯
│Tab│
╰───╯
```

**Focused:**
```
╭───────╮
│content│
│   ╭───╯
│Tab│
╰───╯
```

#### `Side.Left`

Tab text is rendered vertically using `TextDirection.TopBottom_LeftRight`.

**Unfocused:**
```
╭─┬───────╮
│T├content│
│a│       │
│b│       │
╰─┴───────╯
```

**Focused:**
```
╭─────────╮
│T content│
│a        │
│b        │
╰─────────╯
```

#### `Side.Right`

**Unfocused:**
```
╭───────┬─╮
│content│T│
│       │a│
│       │b│
╰───────┴─╯
```

**Focused:**
```
╭─────────╮
│content T│
│        a│
│        b│
╰─────────╯
```

### Summary: Which Line Gets Suppressed

| `TabSide` | Thickness = 3 on | Focused suppresses | TabOffset axis |
|-----------|------------------|--------------------|----------------|
| Top | `Thickness.Top` | Bottom line of header | Horizontal |
| Bottom | `Thickness.Bottom` | Top line of header | Horizontal |
| Left | `Thickness.Left` | Right line of header | Vertical |
| Right | `Thickness.Right` | Left line of header | Vertical |

---

## Focus and Attributes

Focus state affects tab rendering in two ways:

### Border Geometry (Depth ≥ 3)

At depth ≥ 3, the TitleView has a content-side edge (the line adjacent to the content area):
- **Focused**: Content-side edge is **suppressed** (open gap), visually connecting the header to the content
- **Unfocused**: Content-side edge is **drawn** (closed separator), visually separating the header from content

**At depth < 3**, focused and unfocused tabs render with **identical border geometry** — only the title text attributes differentiate them.

### Title Text Attributes

The tab title text always uses the owning View's focus-appropriate attributes:

| View State | Title Text | Hotkey Character |
|------------|------------|------------------|
| Focused | `VisualRole.Focus` | `VisualRole.HotFocus` |
| Unfocused | `VisualRole.Normal` | `VisualRole.HotNormal` |

The `TitleView` uses `SuperViewRendersLineCanvas = true` and inherits color attributes from the owning View's scheme via the adornment hierarchy.

---

## Tab Offset and Clipping

`TabOffset` positions the tab header along the tab side. It can be positive (shifted right/down), zero (at the start), or negative (shifted left/up, partially off-screen).

### Positive Offset (`TabOffset = 2`)

```
  ╭───╮
  │Tab│
╭─┴───┴──╮
│content │
╰────────╯
```

### Negative Offset (`TabOffset = -1`)

```
───╮
Tab│
╭──┴────╮
│content│
╰───────╯
```

### Fully Clipped (`TabOffset = -5`, tab length = 5)

```
╭───────╮
│content│
╰───────╯
```

### Clipping Mechanism

The `TitleView` is positioned at the **unclipped** header rectangle coordinates. The View system's natural viewport clipping handles partial visibility — both border lines and text are clipped automatically. No manual substring calculations or cap-line extensions are needed.

---

## Auto-Join with SuperViewRendersLineCanvas

The tab header is rendered by a `TitleView` SubView that has `SuperViewRendersLineCanvas = true`. This means its border lines merge into the parent View's `LineCanvas` instead of rendering independently.

### How LineCanvas Auto-Join Works

When two border lines overlap at the same `(x, y)` on the same `LineCanvas`, the system resolves them into the correct junction glyph:

| Overlap | Result |
|---------|--------|
| `╮` + `╭` | `┬` (top T-junction) |
| `╯` + `╰` | `┴` (bottom T-junction) |
| horizontal end + vertical | `├` or `┤` |
| two verticals | continuous `│` |

### Multi-Tab Auto-Join

When multiple tab Views share a `LineCanvas` (via a common SuperView with `SuperViewRendersLineCanvas = true`), adjacent tab headers overlap by one column. LineCanvas automatically produces the flowing connected style:

```
Tab1's header:    Tab2's header:    Combined result:
╭────╮                 ╭────╮       ╭────┬────╮
│Tab1│                 │Tab2│       │Tab1│Tab2│
╰────╯                 ╰────╯       ╰────┴────╯
       ↑              ↑
       Tab1 right overlaps Tab2 left
       ╮ + ╭ → ┬ (top), ╯ + ╰ → ┴ (bottom)
```

With the selected tab open and unselected tabs closed:
```
╭────┬────╮
│Tab1│Tab2│
│    ╰────┴───────╮
│content for Tab1 │
╰─────────────────╯
```

---

## Border Line Positioning

When `BorderSettings.Tab` is set, border line positioning differs from the standard model.

**Non-tab sides** (the 3 sides without the tab): The content border line is drawn at the **outer edge** of the thickness.

**Tab side**: The content border line is drawn at `thickness - 1` from the outer edge. The rows/columns between the outer edge and the content border line form the **tab header region**.

| Title-Side Thickness | Content Border Position (Side.Top) | Tab Header Region | Depth |
|----------------------|------------------------------------|-------------------|-------|
| 1 | y = 0 | None (border only) | 1 |
| 2 | y = 1 | y = 0 (1 row) | 2 |
| 3 | y = 2 | y = 0–1 (2 rows) | 3 |
| 4 | y = 3 | y = 0–2 (3 rows) | 4 |
| N | y = N − 1 | y = 0 to N − 2 | N |

General rule: content border at `y = thickness − 1`. Depth = thickness. The `TitleView` border structure is the same for depth ≥ 3 (cap + title + optional closing edge), but the header rectangle grows outward with increasing depth.

---

## Implementation: TitleView

The tab header is rendered by [TitleView](~/api/Terminal.Gui.ViewBase.TitleView.yml), a public sealed `View` subclass that implements [ITitleView](~/api/Terminal.Gui.ViewBase.ITitleView.yml). It handles both the header border frame and the title text.

### Configuration

```csharp
TitleView:
  CanFocus = true
  TabStop = TabBehavior.TabStop
  SuperViewRendersLineCanvas = true      // border lines merge into parent LineCanvas
  BorderStyle = parentLineStyle          // matching parent's line style
  Border.Settings = BorderSettings.None  // no title rendering on the view's own border
  Border.Thickness = ComputeTitleViewThickness (side, depth, hasFocus)
  Orientation = Horizontal or Vertical   // based on TabSide
```

### TitleView Border Thickness (Side.Top)

Computed by `TitleView.ComputeTitleViewThickness(side, depth, hasFocus)`:

| Depth | Focus | Border.Thickness (left, top, right, bottom) | Notes |
|-------|-------|---------------------------------------------|-------|
| ≥ 3 | focused | `(1, 1, 1, 0)` | No bottom = open gap connecting header to content |
| ≥ 3 | unfocused | `(1, 1, 1, 1)` | Bottom = closed separator |
| 2 | any | `(1, 1, 1, 0)` | Cap line, no closing edge |
| 1 | any | `(1, 0, 1, 0)` | Side edges only, no cap |

Other sides rotate accordingly (cap is always the outward edge, content-side is the inward edge).

### Key Methods

| Method | Purpose |
|--------|---------|
| `DrawTabBorder()` | Main entry. Computes geometry, positions TitleView, draws content border segments. |
| `EnsureTitleView()` | Lazy-creates the TitleView with correct configuration. |
| `TitleView.ComputeHeaderRect()` | Computes the unclipped header rectangle in content coordinates (static). |
| `TitleView.ComputeTitleViewThickness()` | Maps depth + side + focus → `Thickness` for the TitleView's border (static). |
| `TitleView.UpdateLayout()` | Sets frame, border thickness, text, orientation, and visibility from `TabLayoutContext`. |
| `AddTabSideContentBorder()` | Draws content border with gap/separator segments on the tab side (static). |

### Draw Pipeline

The tab rendering relies on the View draw pipeline ordering that enables adornment SubView border lines to auto-join with the parent View's border. The pipeline order is:

```
DoDrawAdornments → DoClearViewport → DoDrawSubViews → DoDrawText → DoDrawContent
→ DoDrawAdornmentsSubViews → DoRenderLineCanvas → DoDrawComplete
```

The key change: `DoDrawAdornmentsSubViews` now runs **before** `DoRenderLineCanvas`, so SubView border lines are merged into the parent's `LineCanvas` before it is rendered to screen.

---

## Arrangement (Move and Resize)

The [BorderView](~/api/Terminal.Gui.ViewBase.BorderView.yml) provides the interactive surface for keyboard and mouse-driven move and resize operations.

For a comprehensive guide to the arrangement system (including keyboard-based arrangement, overlapped layouts, and splitter patterns), see the [View Arrangement Deep Dive](arrangement.md).

### How It Works

1. Set [View.Arrangement](~/api/Terminal.Gui.ViewBase.View.yml) to enable move/resize flags
2. The Border must be visible (non-zero `Thickness`) for mouse interaction
3. `BorderView.Arranger` handles mouse events on the border edges

### Quick Reference

| Flag | Mouse Behavior |
|------|----------------|
| `ViewArrangement.Movable` | Drag the top border to move the view |
| `ViewArrangement.Resizable` | Drag any border edge to resize |
| `ViewArrangement.LeftResizable` | Drag the left border edge to resize width |
| `ViewArrangement.BottomResizable` | Drag the bottom border edge to resize height |

When both `Movable` and `Resizable` are set, `Movable` takes precedence on the top edge (it cannot be resized).

### Keyboard Arrangement

Press `Ctrl+F5` (default, configurable via `Application.DefaultKeyBindings`) to enter **Arrange Mode**. Visual indicators appear on the border:

- `◊` (move indicator) in the top-left corner
- `⇲` (resize indicator) in the bottom-right corner
- `↔` / `↕` (edge indicators) on resizable edges

Use arrow keys to move or resize, `Tab` to cycle between modes, and `Esc` to exit.

### Example

```csharp
Window window = new ()
{
    Title = "Drag Me!",
    X = 10, Y = 5,
    Width = 40, Height = 15,
    Arrangement = ViewArrangement.Movable | ViewArrangement.Resizable,
    BorderStyle = LineStyle.Double
};
```
