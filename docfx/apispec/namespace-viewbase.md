---
uid: Terminal.Gui.ViewBase
summary: Core view system, base classes, layout primitives, and adornments.
---

The `ViewBase` namespace contains the foundational view architecture for all Terminal.Gui UI elements.

## Key Types

- **View** - Base class for all UI elements
- **Adornment** / **Margin** / **Border** / **Padding** - Visual decorations around content
- **Pos** / **Dim** - Flexible positioning and sizing system
- **ViewArrangement** - User interaction flags (Movable, Resizable, Overlapped)
- **IValue&lt;T&gt;** - Interface for views with strongly-typed values

## View Architecture

Views are composed of nested layers:

1. **Frame** - Outer rectangle in SuperView coordinates
2. **Margin** - Transparent spacing outside Border
3. **Border** - Visual frame with title and line style
4. **Padding** - Spacing inside Border
5. **Viewport** - Visible window into content area
6. **Content Area** - Where content is drawn (can be larger than Viewport for scrolling)

## Example

```csharp
View view = new ()
{
    X = Pos.Center (),
    Y = Pos.Center (),
    Width = Dim.Percent (50),
    Height = Dim.Auto (),
    Title = "My View",
    BorderStyle = LineStyle.Rounded,
    Arrangement = ViewArrangement.Movable | ViewArrangement.Resizable
};
```

## See Also

- [View Deep Dive](~/docs/View.md)
- [Layout Deep Dive](~/docs/layout.md)
- [Views Overview](~/docs/views.md)
- [Arrangement Deep Dive](~/docs/arrangement.md)
- [Navigation Deep Dive](~/docs/navigation.md)
- [Scrolling Deep Dive](~/docs/scrolling.md)
- [Events Deep Dive](~/docs/events.md) - Event handling patterns 