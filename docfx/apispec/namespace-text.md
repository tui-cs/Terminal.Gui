---
uid: Terminal.Gui.Text
summary: Text processing, formatting, Unicode handling, and autocomplete.
---

The `Text` namespace provides text processing, formatting, and Unicode support for Terminal.Gui.

## Key Types

- **TextFormatter** - Text formatting with alignment, wrapping, and hotkey processing
- **Rune** - Unicode character representation
- **TextDirection** - Left-to-right and right-to-left text support
- **Autocomplete** - Text completion suggestions
- **CollectionNavigator** - Keyboard navigation through collections by typing

## Text Features

- **Unicode Support** - Full Unicode including combining characters and wide glyphs
- **Text Alignment** - Horizontal (Left, Center, Right, Fill) and vertical alignment
- **Word Wrapping** - Automatic text wrapping with configurable behavior
- **Hotkey Processing** - Underlined hotkey characters (`_` prefix)
- **Measurement** - Accurate text width calculation for layout

## Example

```csharp
TextFormatter formatter = new ()
{
    Text = "_Save File",
    Alignment = Alignment.Center,
    VerticalAlignment = Alignment.Center,
    WordWrap = true,
    HotKeySpecifier = (Rune)'_'
};

// Get formatted lines for a given width
List<string> lines = formatter.GetLines (20);
```

## See Also

- [Drawing Deep Dive](~/docs/drawing.md)
