# Scrolling

Terminal.Gui provides a rich system for how [View](View.md) users can scroll content with the keyboard and/or mouse.

## See Also

- [View Deep Dive](View.md)
- [Layout](layout.md)
- [Arrangement](arrangement.md)

## Lexicon & Taxonomy

[!INCLUDE [Scrolling Lexicon](./../includes/scrolling-lexicon.md)]

## Overview

The ability to scroll content is built into View. The `Viewport` represents the scrollable "viewport" into the View's Content Area (which is defined by the return value of `GetContentSize()` ). 

By default, [View](~/api/Terminal.Gui.ViewBase.yml), includes no bindings for the typical directional keyboard and mouse input and cause the Content Area.

Terminal.Gui also provides the ability show a visual scroll bar that responds to mouse input. This ability is not enabled by default given how precious TUI screen real estate is.

Scrolling with the mouse and keyboard are enabled by:

1) Making the `Viewport` size smaller than the size returned by `GetContentSize()`. 
2) Creating key bindings for the appropriate directional keys, and calling `ScrollHorizontal()`(System.Int32) / `ScrollVertical()`(System.Int32) as needed.
3) Subscribing to `MouseEvent` and calling calling `ScrollHorizontal()`(System.Int32) / `ScrollVertical()`(System.Int32) as needed.
4) Enabling the ScrollBars built into View by setting the <xref:Terminal.Gui.ViewBase.ViewportSettingsFlags>.HasVerticalScrollBar or <xref:Terminal.Gui.ViewBase.ViewportSettingsFlags>.HasHorizontalScrollBar flags on the `ViewportSettings` property. Alternatively, the `ScrollBar.VisibilityMode` property can be set to control scrollbar visibility manually.

While <xref:Terminal.Gui.Views.ScrollBar> can be used in a standalone manner to provide proportional scrolling, it is typically enabled automatically via the `HorizontalScrollBar` and `VerticalScrollBar` properties.

## ScrollBar Visibility

The `ScrollBar.VisibilityMode` property controls how a ScrollBar manages its `Visible` state. The <xref:Terminal.Gui.Views.ScrollBarVisibilityMode> enum provides these options:

* `Manual` (default) - The scrollbar does not manage its own visibility. The developer controls `Visible` directly to show or hide the scrollbar.
* `Auto` - The scrollbar is automatically shown when the scrollable content size exceeds the visible content size, and hidden otherwise.
* `Always` - The scrollbar is always visible regardless of content size.
* `None` - The scrollbar is always hidden regardless of content size or <xref:Terminal.Gui.ViewBase.ViewportSettingsFlags>.

### Enabling Built-in Scrollbars

The recommended way to enable the built-in scrollbars (`VerticalScrollBar` and `HorizontalScrollBar`) is to use the <xref:Terminal.Gui.ViewBase.ViewportSettingsFlags>.HasVerticalScrollBar and <xref:Terminal.Gui.ViewBase.ViewportSettingsFlags>.HasHorizontalScrollBar flags:

```csharp
// Enable vertical scrollbar with automatic visibility
view.ViewportSettings |= ViewportSettingsFlags.HasVerticalScrollBar;

// Enable both scrollbars
view.ViewportSettings |= ViewportSettingsFlags.HasScrollBars;

// Disable horizontal scrollbar
view.ViewportSettings &= ~ViewportSettingsFlags.HasHorizontalScrollBar;
```

Setting these flags automatically:
1. Creates the scrollbar (they are lazy-loaded)
2. Sets the scrollbar's `VisibilityMode` to `Auto`
3. Makes the scrollbar visible when content exceeds viewport size

Alternatively, you can manually control scrollbar visibility:

```csharp
// Manual control
view.VerticalScrollBar.Visible = true;
view.VerticalScrollBar.VisibilityMode = ScrollBarVisibilityMode.Always;
```

## Examples

These `UI Catalog` Scenarios illustrate Terminal.Gui scrolling:

* *Scrolling* - Demonstrates the <xref:Terminal.Gui.Views.ScrollBar> objects built into-View.
* *ScrollBar Demo* - Demonstrates using <xref:Terminal.Gui.Views.ScrollBar> view in a standalone manner.
* *ViewportSettings* - Demonstrates the various <xref:Terminal.Gui.ViewBase.ViewportSettingsFlags> (see below) in an interactive manner. Used by the development team to visually verify that convoluted View layout and arrangement scenarios scroll properly.
* *Character Map* - Demonstrates a sophisticated scrolling use-case. The entire set of Unicode code-points can be scrolled and searched. From a scrolling perspective, this Scenario illustrates how to manually configure Viewport, Content Area, and Viewport Settings to enable horizontal and vertical headers (as might appear in a spreadsheet), full keyboard and mouse support, and more. 
* *ListView* and *HexEdit* - The source code to these built-in Views are good references for how to support scrolling and ScrollBars in a re-usable View sub-class. 

## ViewportSettings

The `ViewportSettings` property (of type <xref:Terminal.Gui.ViewBase.ViewportSettingsFlags>) controls the behavior of scrolling. 

**Negative Location Flags** - Allow scrolling before the content origin (0,0):

* `AllowNegativeX` - If set, `Viewport.X` can be set to negative coordinates enabling scrolling beyond the left of the content area.
* `AllowNegativeY` - If set, `Viewport.Y` can be set to negative coordinates enabling scrolling beyond the top of the content area.
* `AllowNegativeLocation` - Combines both X and Y.

**Greater Than Content Flags** - Allow scrolling past the last row/column:

* `AllowXGreaterThanContentWidth` - If set, `Viewport` `.X` can be set to values greater than or equal to the content width, enabling scrolling beyond the right of the Content Area. When not set, `Viewport.X` is constrained so the last column remains visible.
* `AllowYGreaterThanContentHeight` - If set, `Viewport` `.Y` can be set to values greater than or equal to the content height, enabling scrolling beyond the bottom of the Content Area. When not set, `Viewport.Y` is constrained so the last row remains visible.
* `AllowLocationGreaterThanContentSize` - Combines both X and Y.

**Blank Space Flags** - Allow blank space to appear when scrolling:

* `AllowXPlusWidthGreaterThanContentWidth` - If set, `Viewport.X + Viewport.Width` can exceed `GetContentSize().Width`, allowing blank space on the right when scrolling.
* `AllowYPlusHeightGreaterThanContentHeight` - If set, `Viewport.Y + Viewport.Height` can exceed `GetContentSize().Height`, allowing blank space at the bottom when scrolling.
* `AllowLocationPlusSizeGreaterThanContentSize` - Combines both X and Y.

**Conditional Negative Flags** - Allow negative scrolling only when viewport is larger than content:

* `AllowNegativeXWhenWidthGreaterThanContentWidth` - Useful for centering content smaller than the view.
* `AllowNegativeYWhenHeightGreaterThanContentHeight` - Useful for centering content smaller than the view.
* `AllowNegativeLocationWhenSizeGreaterThanContentSize` - Combines both X and Y.

**Drawing Flags** - Control clipping and clearing behavior:

* `ClipContentOnly` - By default, clipping is applied to `Viewport`. Setting this flag will cause clipping to be applied to the visible content area.
* `ClearContentOnly` - If set, `ClearViewport()` will clear only the portion of the content area that is visible within the Viewport. This is useful for views that have a content area larger than the Viewport and want the area outside the content to be visually distinct. `ClipContentOnly` must be set for this to work.
* `Transparent` - The view does not clear its background when drawing.
* `TransparentMouse` - Mouse events pass through areas not occupied by SubViews.

**ScrollBar Flags** - Enable built-in scrollbars:

* `HasVerticalScrollBar` - If set, the built-in `VerticalScrollBar` is enabled with <xref:Terminal.Gui.Views.ScrollBarVisibilityMode>.Auto behavior. Clearing this flag disables the scrollbar.
* `HasHorizontalScrollBar` - If set, the built-in `HorizontalScrollBar` is enabled with <xref:Terminal.Gui.Views.ScrollBarVisibilityMode>.Auto behavior. Clearing this flag disables the scrollbar.
* `HasScrollBars` - Combines both vertical and horizontal scrollbar flags.

