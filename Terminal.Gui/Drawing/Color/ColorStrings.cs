using System.Globalization;

namespace Terminal.Gui.Drawing;

/// <summary>
///     Provides a mapping between <see cref="Color"/> and the W3C standard color name strings.
/// </summary>
public static class ColorStrings
{
    private static readonly StandardColorsNameResolver _standard = new ();

    /// <summary>
    ///     Gets the color name for <paramref name="color"/>.
    /// </summary>
    /// <param name="color">The color.</param>
    /// <returns>Standard color name for the specified color; otherwise <see langword="null"/>.</returns>
    public static string? GetColorName (Color color)
    {
        if (color == Color.None)
        {
            return "None";
        }

        if (_standard.TryNameColor (color, out string? name))
        {
            return name;
        }
        return null;
    }

    /// <summary>
    ///     Returns the list of W3C+ standard color names.
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<string> GetStandardColorNames ()
    {
        return _standard.GetColorNames ();
    }

    /// <summary>
    ///     Parses <paramref name="name"/> and returns <paramref name="color"/> if name is a W3C+ standard named color.
    /// </summary>
    /// <param name="name">The name to parse.</param>
    /// <param name="color">If successful, the color.</param>
    /// <returns><see langword="true"/> if <paramref name="name"/> was parsed successfully.</returns>
    public static bool TryParseStandardColorName (ReadOnlySpan<char> name, out Color color)
    {
        if (name.Equals ("None", StringComparison.OrdinalIgnoreCase))
        {
            color = Color.None;
            return true;
        }

        if (_standard.TryParseColor (name, out color))
        {
            return true;
        }
        // Backwards compatibility: Also parse #RRGGBB.
        return TryParseHexColor (name, out color);
    }

    /// <summary>
    ///     Parses <paramref name="name"/> and returns <paramref name="color"/> if name is a W3C+ standard named color.
    /// </summary>
    /// <param name="name">The name to parse.</param>
    /// <param name="color">If successful, the color.</param>
    /// <returns><see langword="true"/> if <paramref name="name"/> was parsed successfully.</returns>
    public static bool TryParseNamedColor (ReadOnlySpan<char> name, out Color color)
    {
        if (name.Equals ("None", StringComparison.OrdinalIgnoreCase))
        {
            color = Color.None;
            return true;
        }

        if (_standard.TryParseColor (name, out color))
        {
            return true;
        }
        // Backwards compatibility: Also parse #RRGGBB.
        if (TryParseHexColor (name, out color))
        {
            return true;
        }

        color = default (Color);
        return false;
    }

    private static bool TryParseHexColor (ReadOnlySpan<char> name, out Color color)
    {
        if (name.Length == 7 && name [0] == '#')
        {
            if (int.TryParse (name.Slice (1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int r) &&
                int.TryParse (name.Slice (3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int g) &&
                int.TryParse (name.Slice (5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int b))
            {
                color = new Color (r, g, b);
                return true;
            }
        }

        color = default (Color);
        return false;
    }
}
