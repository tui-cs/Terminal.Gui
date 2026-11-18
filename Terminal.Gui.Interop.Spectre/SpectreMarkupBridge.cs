using global::Spectre.Console;
using Terminal.Gui.Drawing;
using TgAttribute = Terminal.Gui.Drawing.Attribute;
using TgColor = Terminal.Gui.Drawing.Color;
using SpectreColor = global::Spectre.Console.Color;

namespace Terminal.Gui.Interop.Spectre;

/// <summary>
///     Converts between Spectre.Console styling and Terminal.Gui drawing attributes.
/// </summary>
public static class SpectreMarkupBridge
{
    /// <summary>
    ///     Converts a Spectre <see cref="Style"/> to a Terminal.Gui <see cref="TgAttribute"/>.
    /// </summary>
    /// <param name="style">The Spectre style to convert.</param>
    /// <returns>The converted Terminal.Gui attribute.</returns>
    public static TgAttribute ToAttribute (this Style style)
    {
        TgColor foreground = SpectreColorToTg (style.Foreground);
        TgColor background = SpectreColorToTg (style.Background);
        TextStyle textStyle = DecorationToTextStyle (style.Decoration);

        return new (foreground, background, textStyle);
    }

    /// <summary>
    ///     Converts a Terminal.Gui <see cref="TgAttribute"/> to a Spectre <see cref="Style"/>.
    /// </summary>
    /// <param name="attribute">The Terminal.Gui attribute to convert.</param>
    /// <returns>The converted Spectre style.</returns>
    public static Style ToSpectreStyle (this TgAttribute attribute)
    {
        SpectreColor foreground = TgColorToSpectre (attribute.Foreground);
        SpectreColor background = TgColorToSpectre (attribute.Background);
        Decoration decoration = TextStyleToDecoration (attribute.Style);

        return new (foreground, background, decoration);
    }

    private static TgColor SpectreColorToTg (SpectreColor? color)
    {
        if (color is null)
        {
            return TgColor.None;
        }

        SpectreColor value = color.Value;

        if (value == SpectreColor.Default)
        {
            return TgColor.None;
        }

        return new TgColor (value.R, value.G, value.B);
    }

    private static SpectreColor TgColorToSpectre (TgColor color)
    {
        if (color == TgColor.None)
        {
            return SpectreColor.Default;
        }

        return new SpectreColor ((byte)color.R, (byte)color.G, (byte)color.B);
    }

    private static TextStyle DecorationToTextStyle (Decoration? decoration)
    {
        if (decoration is null)
        {
            return TextStyle.None;
        }

        Decoration value = decoration.Value;
        TextStyle style = TextStyle.None;

        if ((value & Decoration.Bold) != 0)
        {
            style |= TextStyle.Bold;
        }

        if ((value & Decoration.Dim) != 0)
        {
            style |= TextStyle.Faint;
        }

        if ((value & Decoration.Italic) != 0)
        {
            style |= TextStyle.Italic;
        }

        if ((value & Decoration.Underline) != 0)
        {
            style |= TextStyle.Underline;
        }

        if ((value & Decoration.Invert) != 0)
        {
            style |= TextStyle.Reverse;
        }

        if ((value & (Decoration.SlowBlink | Decoration.RapidBlink)) != 0)
        {
            style |= TextStyle.Blink;
        }

        if ((value & Decoration.Strikethrough) != 0)
        {
            style |= TextStyle.Strikethrough;
        }

        return style;
    }

    private static Decoration TextStyleToDecoration (TextStyle style)
    {
        Decoration decoration = Decoration.None;

        if ((style & TextStyle.Bold) != 0)
        {
            decoration |= Decoration.Bold;
        }

        if ((style & TextStyle.Faint) != 0)
        {
            decoration |= Decoration.Dim;
        }

        if ((style & TextStyle.Italic) != 0)
        {
            decoration |= Decoration.Italic;
        }

        if ((style & TextStyle.Underline) != 0)
        {
            decoration |= Decoration.Underline;
        }

        if ((style & TextStyle.Reverse) != 0)
        {
            decoration |= Decoration.Invert;
        }

        if ((style & TextStyle.Blink) != 0)
        {
            decoration |= Decoration.SlowBlink;
        }

        if ((style & TextStyle.Strikethrough) != 0)
        {
            decoration |= Decoration.Strikethrough;
        }

        return decoration;
    }
}
