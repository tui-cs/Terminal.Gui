# Drawing (Text, Lines, and Color)

Terminal.Gui provides a set of APIs for formatting text, line drawing, and character-based graphing. 

## Drawing Taxonomy & Lexicon

[!INCLUDE [Drawing Lexicon](~/includes/drawing-lexicon.md)]

# View Drawing API

Terminal.Gui apps draw using the `Move()` and `AddRune()` APIs. Move selects the column and row of the cell and AddRune places the specified glyph in that cell using the <xref:Terminal.Gui.Drawing.Attribute> that was most recently set via `SetAttribute()`. The driver caches all changed Cells and efficiently outputs them to the terminal each iteration of the Application. In other words, Terminal.Gui uses deferred rendering. 

## Drawing Lifecycle

**Drawing occurs during Application MainLoop iterations**, not immediately when draw-related methods are called. This deferred rendering approach provides better performance and ensures visual consistency.

### MainLoop Iteration Process

Each iteration of the <xref:Terminal.Gui.App.Application> MainLoop (throttled to a maximum rate) performs these steps in order:

1. **Layout** - Views that need layout are measured and positioned (`LayoutSubviews()` is called)
2. **Draw** - Views that need drawing update the driver's back buffer (`Draw()` is called)
3. **Write** - The driver writes changed portions of the back buffer to the actual terminal
4. **Cursor** - The driver ensures the cursor is positioned correctly with appropriate visibility

### When Drawing Actually Occurs

- **Normal Operation**: Drawing happens automatically during MainLoop iterations when <xref:Terminal.Gui.ViewBase.View.NeedsDraw> or `SubViewNeedsDraw` is set
- **Forced Update**: <xref:Terminal.Gui.App.Application.LayoutAndDraw(System.Boolean)> can be called to immediately trigger layout and drawing outside of the normal iteration cycle
- **Testing**: Tests can call `Draw()` directly to update the back buffer, then call `IDriver.Refresh()` to output to the terminal

**Important**: Calling `View.Draw()` does not immediately update the terminal screen. It only updates the driver's back buffer. The actual terminal output occurs when the driver's `Refresh()` method is called, which happens automatically during MainLoop iterations.

## Coordinate System for Drawing

The <xref:Terminal.Gui.ViewBase.View> draw APIs all take coordinates specified in *Viewport-Relative* coordinates. That is, `0, 0` is the top-left cell visible to the user.

See [Layout](layout.md) for more details of the Terminal.Gui coordinate system.

## Outputting unformatted text

1) Moving the draw cursor using `Move()`.
2) Setting the attributes using `SetAttribute()`.
3) Outputting glyphs by calling `AddRune()` or `AddStr()` .

## Outputting formatted text

1) Adding the text to a <xref:Terminal.Gui.Text.TextFormatter> object.
2) Setting formatting options, such as `TextFormatter.Alignment`.
3) Calling `TextFormatter.Draw()`(Terminal.Gui.IDriver, System.Drawing.Rectangle,Terminal.Gui.Attribute,Terminal.Gui.Attribute,System.Drawing.Rectangle).

## Line Drawing

See [LineCanvas Deep Dive](#linecanvas-deep-dive) below.

## View.Draw — Per-View Drawing Flow

When `View.Draw()` is called on a view that has <xref:Terminal.Gui.ViewBase.View.NeedsDraw> or `SubViewNeedsDraw` set, it executes these steps in order:

1. **Draw Adornments** — Draws the <xref:Terminal.Gui.ViewBase.Border> and <xref:Terminal.Gui.ViewBase.Padding> frames (fills and line art). Non-transparent <xref:Terminal.Gui.ViewBase.Margin> is also drawn here. Transparent margins (those with shadows) are deferred to a second pass.
2. **Clip to Viewport** — Sets the clip region to the view's Viewport, preventing content from drawing outside it.
3. **Clear Viewport** — Fills the viewport with the background color.
4. **Draw SubViews** — Draws SubViews in reverse Z-order (earliest added = highest Z = drawn last, on top). For SubViews with `SuperViewRendersLineCanvas = true`, their <xref:Terminal.Gui.Drawing.LineCanvas> is merged into the parent's canvas for unified intersection resolution. Overlapped SubViews' canvases are collected for painters'-algorithm compositing.
5. **Draw Text** — Renders `View.Text` via <xref:Terminal.Gui.Text.TextFormatter>.
6. **Draw Content** — Raises `DrawingContent` (override `OnDrawingContent` for custom drawing).
7. **Draw Adornment SubViews** — Draws SubViews of <xref:Terminal.Gui.ViewBase.Border> and <xref:Terminal.Gui.ViewBase.Padding> (e.g., tab headers, diagnostic indicators). Their <xref:Terminal.Gui.Drawing.LineCanvas> lines are merged into the parent's canvas.
8. **Render LineCanvas** — Resolves all lines (including merged lines from adornments and SubViews) into glyphs via `GetCellMap`, then composites overlapped canvases using the painters' algorithm.
9. **Cache Clip for Margin** — If the <xref:Terminal.Gui.ViewBase.Margin> has a shadow, the current clip is cached for the second-pass shadow render.
10. **DrawComplete & Clip Exclusion** — Raises `DrawComplete`. For opaque views, the entire frame is excluded from the clip. For transparent views, only the actually-drawn cells are excluded. This ensures later-drawn (lower-Z) views don't overwrite this view's content.

### Peer-View Draw Loop

The static `View.Draw(views, force)` method orchestrates drawing a set of peer views (views sharing the same SuperView):

1. Each peer view's `Draw()` is called in order.
2. After all peers complete, `MarginView.DrawMargins()` performs a **second pass** that draws transparent margins (shadows). This ensures shadows render on top of all other content. The cached clip from step 9 above is restored for each margin so the shadow draws into the correct region.
3. `NeedsDraw` flags are cleared on all peers.

### Declaring that Drawing is Needed

Call `SetNeedsDraw()` when something changes within a view's content area. Call `SetNeedsLayout()` when the viewport size needs recalculation. Both propagate up the view hierarchy via `SubViewNeedsDraw`.

**Note**: These methods do not cause immediate drawing. They mark the view for redraw in the next MainLoop iteration. To force immediate drawing (typically only in tests), call <xref:Terminal.Gui.App.Application.LayoutAndDraw(System.Boolean)>.

### Overriding Draw Behavior

Most draw steps can be overridden using the [Cancellable Work Pattern](cancellable-work-pattern.md). For example, to prevent the viewport from being cleared, override `OnClearingViewport()` to return `true`, or subscribe to the `ClearingViewport` event and set `Cancel = true`.

## Clipping

Clipping enables better performance and features like shadows by ensuring regions of the terminal that need to be drawn actually get drawn by the driver. Terminal.Gui supports non-rectangular clip regions with <xref:Terminal.Gui.Drawing.Region>. The driver.Clip is the application managed clip region and is managed by <xref:Terminal.Gui.App.Application>. Developers cannot change this directly, but can use `SetClipToScreen()`, `SetClip()`(Terminal.Gui.Region), `SetClipToFrame()`, etc...


## Raster Images (Kitty and Sixel)

Terminal.Gui can render raster images through the same deferred drawing pipeline as text and line art. <xref:Terminal.Gui.Views.ImageView> accepts a `Color[,]` pixel buffer. When `ImageView.UseRasterGraphics` is `true`, the driver uses the best available terminal raster protocol:

1. Kitty graphics, when <xref:Terminal.Gui.Drivers.IDriver.KittyGraphicsSupport> reports support and the output path has Kitty enabled.
2. Sixel, when Kitty is not active and <xref:Terminal.Gui.Drivers.IDriver.SixelSupport> reports support.
3. Cell rendering, when no raster protocol is available.

Kitty is preferred because it supports full RGBA pixels and works well in modern macOS terminals such as Kitty, Ghostty, and WezTerm. Sixel remains the fallback for terminals such as Windows Terminal and xterm. The obsolete `ImageView.UseSixel` and `ImageView.IsUsingSixel` members still work as compatibility shims; use `UseRasterGraphics` and `IsUsingRasterGraphics` in new code.

Raster image commands participate in normal composition:

1. The current clip region is captured when the image is drawn.
2. The output layer crops the pixel buffer to the visible clipped cell rectangles.
3. The raster command is emitted before dirty text cells, so later-drawn SubViews, dialogs, borders, and overlays can paint over the image.
4. Replacing or removing the image invalidates the cells the previous raster occupied, so stale terminal graphics are cleared by the next refresh.

To render an image, assign the pixel buffer and mark the view for drawing:

```cs
Color [,] pixels = new Color [1, 1];

ImageView imageView = new ()
{
    Width = 30,
    Height = 20,
    UseRasterGraphics = true
};

imageView.Image = pixels;
```

To customize Sixel encoding, assign `ImageView.SixelEncoder` before setting `Image`. Kitty output is encoded by the driver/output layer and does not use `SixelEncoder`.

The UICatalog Images scenario demonstrates runtime protocol selection, and the Mandelbrot scenario demonstrates a custom `ImageView` that re-renders fractal pixels while zooming and panning.

![Mandelbrot Kitty graphics raster demo](../images/Mandelbrot.gif)

## Cell

The <xref:Terminal.Gui.Drawing.Cell> class represents a single cell on the screen. It contains a character and an attribute. The character is of type `Rune` and the attribute is of type <xref:Terminal.Gui.Drawing.Attribute>.

`Cell` is not exposed directly to the developer. Instead, the driver classes manage the `Cell` array that represents the screen.

To draw a `Cell` to the screen, use `Move()` to specify the row and column coordinates and then use the `AddRune()` method to draw a single glyph.  

## Attribute 

The <xref:Terminal.Gui.Drawing.Attribute> class represents the formatting attributes of a `Cell`. It exposes properties for the foreground and background colors as well as the text style. The foreground and background colors are of type <xref:Terminal.Gui.Drawing.Color>. Bold, underline, and other formatting attributes are supported via the `Attribute.Style` property.

Use `SetAttribute()` to indicate which Attribute subsequent `AddRune()` and `AddStr()` calls will use:

```cs
// This is for illustration only. Developers typically use SetAttributeForRole instead.
SetAttribute (new Attribute (Color.Red, Color.Black, Style.Underline));
AddStr ("Red on Black Underlined.");
```

In the above example a hard-coded Attribute is set. Normally, developers will use `SetAttributeForRole()` to have the system use the Attributes associated with a `VisualRole` (see below).

```cs
// Modify the View's Scheme such that Focus is Red on Black Underlined
SetScheme (new Scheme (Scheme)
    {
        Focus = new Attribute (Color.Red, Color.Black, Style.Underline)
    });
    
SetAttributeForRole (VisualRole.Focus);
AddStr ("Red on Black Underlined.");
```

## Color

Terminal.Gui supports 24-bit true color (16.7 million colors) via the <xref:Terminal.Gui.Drawing.Color> struct. The <xref:Terminal.Gui.Drawing.Color> struct represents colors in ARGB32 format, with separate bytes for Alpha (transparency), Red, Green, and Blue components.

### Standard Colors (W3C+)

Terminal.Gui provides comprehensive support for W3C standard color names plus additional common terminal colors via the <xref:Terminal.Gui.Drawing.StandardColor> enum. This includes all standard W3C colors (like `AliceBlue`, `Red`, `Tomato`, etc.) as well as classic terminal colors (like `AmberPhosphor`, `GreenPhosphor`).

Colors can be created from standard color names:

```cs
var color1 = new Color(StandardColor.CornflowerBlue);
var color2 = new Color(StandardColor.Tomato);
var color3 = new Color("Red");  // Case-insensitive color name parsing
```

Standard colors can also be parsed from strings:

```cs
if (Color.TryParse("CornflowerBlue", out Color color))
{
    // Use the color
}
```

### Alpha Channel and Transparency

While <xref:Terminal.Gui.Drawing.Color> supports an alpha channel for transparency (values 0-255), **terminal rendering does not currently support alpha blending**. The alpha channel is primarily used to:

- Indicate whether a color should be rendered at all (alpha = 0 means fully transparent/don't render)
- Support future transparency features
- Enable terminal background pass-through (see [#2381](https://github.com/tui-cs/Terminal.Gui/issues/2381) and [#4229](https://github.com/tui-cs/Terminal.Gui/issues/4229))

**Important**: When matching colors to standard color names, the alpha channel is **ignored**. This means `Color(255, 0, 0, 255)` (opaque red) and `Color(255, 0, 0, 128)` (semi-transparent red) will both be recognized as "Red". This design decision supports the vision of enabling transparent backgrounds while still being able to identify colors semantically.

```cs
var opaqueRed = new Color(255, 0, 0, 255);
var transparentRed = new Color(255, 0, 0, 0);

// Both will resolve to "Red"
ColorStrings.GetColorName(opaqueRed);      // Returns "Red"
ColorStrings.GetColorName(transparentRed); // Returns "Red"
```

### Color.None (Terminal Default Colors)

`Color.None` is a special sentinel value (alpha=0) that tells the driver to emit ANSI reset codes (`CSI 39m` / `CSI 49m`) instead of explicit RGB values. This allows the terminal's native foreground/background colors — including any transparency or acrylic effects — to show through.

When `Color.None` is used in a <xref:Terminal.Gui.Drawing.Scheme>, the derivation algorithm resolves it to the terminal's actual default colors (detected via OSC 10/11 queries at startup) before performing color math. See [Scheme Deep Dive](scheme.md) for details.

### Dark/Light Background Awareness

The `Color.IsDarkColor()` method returns `true` if a color's HSL lightness is below 50%. This is used by the <xref:Terminal.Gui.Drawing.Scheme> derivation algorithm to determine the direction for `GetBrighterColor` and `GetDimmerColor`:

- `Color.GetBrighterColor(double, bool?)` — Makes a color more visually prominent. On dark backgrounds (or when auto-detecting), increases lightness. On light backgrounds, decreases lightness.
- `Color.GetDimmerColor(double, bool?)` — Makes a color less visually prominent. On dark backgrounds, decreases lightness. On light backgrounds, increases lightness (washes out toward white).

Both methods accept an optional `isDarkBackground` parameter. When `null` (the default), they auto-detect from the color's own lightness for backward compatibility. The <xref:Terminal.Gui.Drawing.Scheme> derivation algorithm passes explicit values based on the resolved background color.

### Legacy 16-Color Support

For backwards compatibility and terminals with limited color support, Terminal.Gui maintains the legacy 16-color system via <xref:Terminal.Gui.Drawing.ColorName16>. When true color is not available or when `Application.Force16Colors` is set, Terminal.Gui will map true colors to the nearest 16-color equivalent.

## VisualRole

Represents the semantic visual role of a visual element rendered by a View (e.g., Normal text, Focused item, Active selection).

<xref:Terminal.Gui.Drawing.VisualRole> provides a set of predefined VisualRoles:

[!code-csharp[VisualRole.cs](../../Terminal.Gui/Drawing/VisualRole.cs)]

## Schemes

[!INCLUDE [Scheme Overview](~/includes/scheme-overview.md)]

See [Scheme Deep Dive](scheme.md) for more details.

## Text Formatting

Terminal.Gui supports text formatting using <xref:Terminal.Gui.Text.TextFormatter>. <xref:Terminal.Gui.Text.TextFormatter> provides methods for formatting text using the following formatting options:

* Horizontal Alignment - Left, Center, Right
* Vertical Alignment - Top, Middle, Bottom
* Word Wrap - Enabled or Disabled
* Formatting Hot Keys

## Glyphs

The <xref:Terminal.Gui.Drawing.Glyphs> class defines the common set of glyphs used to draw checkboxes, lines, borders, etc... The default glyphs can be changed per-theme via <xref:Terminal.Gui.Configuration.GlyphSettings> and <xref:Terminal.Gui.Configuration.TuiConfigurationBuilder>. 

## LineCanvas Deep Dive

### What LineCanvas Does

Terminal UI borders are built from Unicode box-drawing characters: `─`, `│`, `┌`, `┐`, `┼`, `├`, and dozens more. When two borders meet, the correct junction glyph must be selected. Doing this by hand is tedious and error-prone.

<xref:Terminal.Gui.Drawing.LineCanvas> solves this. You describe *lines* — start point, length, orientation, style — and the canvas automatically resolves every intersection into the correct Unicode glyph. Where a horizontal and vertical line meet, it produces a `┼`. Where three lines meet, a `├`. Corners, T-junctions, and crosses are all handled automatically.

### Basic Usage

```csharp
LineCanvas lc = new ();

// Draw a 10-cell horizontal line
lc.AddLine (new Point (0, 0), 10, Orientation.Horizontal, LineStyle.Single);

// Draw a 5-cell vertical line crossing at (4, 0)
lc.AddLine (new Point (4, 0), 5, Orientation.Vertical, LineStyle.Single);

// Resolve all intersections and get the glyphs
Dictionary<Point, Cell?> cells = lc.GetCellMap ();
// At (4, 0), this returns a ┬ (top-tee), not a ─ or │
```

Each <xref:Terminal.Gui.Drawing.StraightLine> is always horizontal or vertical. It has a `Start` point, a `Length` (positive = right/down, negative = left/up, zero = a single junction point), an `Orientation`, a <xref:Terminal.Gui.Drawing.LineStyle>, and an optional color <xref:Terminal.Gui.Drawing.Attribute>.

### How Intersection Resolution Works

When you call `GetCellMap()`, the canvas walks every point within its bounds:

1. **Collect intersections.** For each point, every line that passes through it produces an `IntersectionDefinition` describing *how* the line relates to that point — does it pass over horizontally? Start here going right? End here from below?

2. **Determine glyph type.** The set of intersection types at a point is analyzed to decide the glyph category: corner, T-junction, cross, straight line, etc. For example, `{StartRight, StartDown}` = upper-left corner (`┌`).

3. **Select style variant.** The <xref:Terminal.Gui.Drawing.LineStyle> of the intersecting lines determines which Unicode variant to render: single (`─`), double (`═`), heavy (`━`), dashed, dotted, or rounded.

4. **Filter exclusions.** Points in the exclusion region (see [Exclude](#linecanvasexclude--output-filter) below) are removed from the output.

### Line Styles

<xref:Terminal.Gui.Drawing.LineStyle> determines the glyph variant used for each line segment and intersection:

| Style | Horizontal | Vertical | Corner |
|-------|-----------|----------|--------|
| `Single` | `─` | `│` | `┌` |
| `Double` | `═` | `║` | `╔` |
| `Heavy` | `━` | `┃` | `┏` |
| `Rounded` | `─` | `│` | `╭` |
| `Dashed` | `╌` | `╎` | `┌` |
| `Dotted` | `┄` | `┆` | `┌` |

When lines of different styles intersect, the canvas selects the appropriate mixed-style glyph (e.g., a single horizontal meeting a double vertical produces `╥`).

### Merging Canvases Across Views

Every <xref:Terminal.Gui.ViewBase.View> has its own `LineCanvas`. During drawing, the framework merges canvases so that lines from different views auto-join seamlessly.

When `SuperViewRendersLineCanvas` is `true` on a SubView, its lines are merged into the SuperView's canvas via `LineCanvas.Merge()`. All lines then participate in a single intersection-resolution pass. This is how adjacent tab headers, nested frames, and other multi-view border compositions achieve connected line art.

SubViews that render their own `LineCanvas` independently (the default) are composited using a painters' algorithm during `View.RenderLineCanvas()`. Higher-Z views take priority; lower-Z cells only render if they provide richer junctions.

### `LineStyle.None` — A Convention, Not an Eraser

<xref:Terminal.Gui.Drawing.LineStyle.None> has **no special handling** inside <xref:Terminal.Gui.Drawing.LineCanvas>. When passed to `AddLine`, the line is stored and participates in intersection resolution like any other. Because `None` doesn't match any styled-glyph check, it falls through to the default glyphs and **renders identically to `LineStyle.Single`**.

The "eraser" behavior in the LineDrawing scenario is implemented by the *consumer*, not by LineCanvas:

```csharp
// LineDrawing scenario — eraser logic on mouse-up
if (_currentLine.Style == LineStyle.None)
{
    // Physically remove overlapping segments from the line collection
    area.CurrentLayer = new LineCanvas (
        area.CurrentLayer.Lines.Exclude (
            _currentLine.Start,
            _currentLine.Length,
            _currentLine.Orientation));
}
```

This calls <xref:Terminal.Gui.Drawing.StraightLineExtensions.Exclude*> to split and remove overlapping lines. The `None`-styled line itself is never kept.

> **Key point:** If you add a `LineStyle.None` line without eraser handling, it renders as a visible single-style line. To suppress lines, use the mechanisms described below.

### Suppressing and Removing Lines

LineCanvas provides three distinct mechanisms for controlling what gets drawn. Each operates at a different stage and has different semantics. Using the wrong one produces subtle bugs.

#### `LineCanvas.Exclude` — Output Filter

<xref:Terminal.Gui.Drawing.LineCanvas.Exclude*> suppresses resolved cells from `GetCellMap` output **without affecting the underlying geometry**. Lines still exist and still auto-join through excluded positions.

```csharp
LineCanvas lc = new ();
lc.AddLine (new (0, 0), 10, Orientation.Horizontal, LineStyle.Single);

// Exclude positions 3–5 (a title label occupies those cells)
lc.Exclude (new Region (new Rectangle (3, 0, 3, 1)));

// GetCellMap returns 7 cells (positions 0–2 and 6–9).
// The line auto-joins correctly on either side because
// the full line still participates in intersection resolution.
```

**Use this when something else is drawn at a position** — for example, a title label on a border. The border joins correctly on either side because the line is continuous behind the label.

**Do not use this as an eraser.** Because lines auto-join through excluded regions, phantom geometry leaks into junction decisions. A vertical line crossing an excluded horizontal line resolves as `┼` instead of `│`.

#### `Reserve` — Compositing Metadata

<xref:Terminal.Gui.Drawing.LineCanvas.Reserve*> marks positions as intentionally empty for multi-canvas compositing. It has **no effect** on the canvas that calls it — `GetCellMap` does not check reserved cells.

Reserved cells are consumed during `View.RenderLineCanvas`, which layers multiple independently-resolved canvases. Reserved cells claim positions so that cells from lower-Z canvases do not show through:

```csharp
// In a focused tab's border rendering:
// Reserve the gap where the header connects to the content area.
// This prevents the content area's top border from showing through.
lc.Reserve (new Rectangle (gapStart, borderY, gapWidth, 1));
```

#### `StraightLineExtensions.Exclude` — Geometry Surgery

<xref:Terminal.Gui.Drawing.StraightLineExtensions.Exclude*> physically splits or removes lines from a collection. This is the correct tool for erasing geometry:

```csharp
LineCanvas lc = new ();
lc.AddLine (new (0, 0), 10, Orientation.Horizontal, LineStyle.Single);

// Erase positions 4–5 by rebuilding the line collection
IEnumerable<StraightLine> remaining = lc.Lines.Exclude (
    new Point (4, 0), 2, Orientation.Horizontal);

LineCanvas erased = new (remaining);
// erased contains two separate segments: 0–3 and 6–9.
// A vertical line through x=4 correctly renders as │, not ┼.
```

> **Warning:** There are two unrelated methods named `Exclude`. `LineCanvas.Exclude (Region)` filters output while preserving auto-join. `StraightLineExtensions.Exclude (...)` physically removes geometry. They have opposite semantics.

#### Choosing the Right Mechanism

| Scenario | Mechanism | Why |
|----------|-----------|-----|
| Hide cells behind a title label | `LineCanvas.Exclude (Region)` | Lines auto-join through the label — the border looks continuous |
| Erase drawn lines | `StraightLineExtensions.Exclude (...)` | Geometry is physically removed — no phantom junctions |
| Gap where a focused tab meets content | `Reserve (Rectangle)` | Claims positions during compositing so lower-Z borders don't bleed through |
| "No border" on a view | Don't call `AddLine` | Check `if (style != LineStyle.None)` before adding |

## Thickness

Describes the thickness of a frame around a rectangle. The thickness is specified for each side of the rectangle using a <xref:Terminal.Gui.Drawing.Thickness> object. The Thickness class contains properties for the left, top, right, and bottom thickness. The <xref:Terminal.Gui.ViewBase.AdornmentImpl> class uses <xref:Terminal.Gui.Drawing.Thickness> to support drawing the frame around a view. 

See [View Deep Dive](View.md) for details.

## Diagnostics

The `ViewDiagnosticFlags.DrawIndicator` flag can be set on `View.Diagnostics` to cause an animated glyph to appear in the <xref:Terminal.Gui.ViewBase.Border> of each View. The glyph will animate each time that View's `Draw` method is called where either <xref:Terminal.Gui.ViewBase.View.NeedsDraw> or `SubViewNeedsDraw` is set.

## Accessing Application Drawing Context

Views can access application-level drawing functionality through `View.App`:

```csharp
public class CustomView : View
{
    protected override bool OnDrawingContent()
    {
        // Access driver capabilities through View.App
        if (App?.Driver?.SupportsTrueColor == true)
        {
            // Use true color features
            SetAttribute(new Attribute(Color.FromRgb(255, 0, 0), Color.FromRgb(0, 0, 255)));
        }
        else
        {
            // Fallback to 16-color mode
            SetAttributeForRole(VisualRole.Normal);
        }
        
        AddStr("Custom drawing with application context");
        return true;
    }
}