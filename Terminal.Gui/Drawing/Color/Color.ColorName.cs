namespace Terminal.Gui.Drawing;

/// <summary>
///     Defines the 16 legacy color names and values that can be used to set the foreground and background colors in
///     Terminal.Gui apps. Used with <see cref="Color"/>.
/// </summary>
/// <remarks>
///     <para>These colors match the 16 colors defined for ANSI escape sequences for 4-bit (16) colors.</para>
///     <para>
///         For terminals that support 24-bit color (TrueColor), the RGB values for each of these colors can be
///         configured using the <see cref="Color.Colors16"/> property.
///     </para>
/// </remarks>
public enum ColorName16
{
    /// <summary>The black color. ANSI escape sequence: <c>\u001b[30m</c>.</summary>
    Black,

    /// <summary>The blue color. ANSI escape sequence: <c>\u001b[34m</c>.</summary>
    Blue,

    /// <summary>The green color. ANSI escape sequence: <c>\u001b[32m</c>.</summary>
    Green,

    /// <summary>The cyan color. ANSI escape sequence: <c>\u001b[36m</c>.</summary>
    Cyan,

    /// <summary>The red color. ANSI escape sequence: <c>\u001b[31m</c>.</summary>
    Red,

    /// <summary>The magenta color. ANSI escape sequence: <c>\u001b[35m</c>.</summary>
    Magenta,

    /// <summary>The yellow color (also known as Brown). ANSI escape sequence: <c>\u001b[33m</c>.</summary>
    Yellow,

    /// <summary>The gray color (also known as White). ANSI escape sequence: <c>\u001b[37m</c>.</summary>
    Gray,

    /// <summary>The dark gray color (also known as Bright Black). ANSI escape sequence: <c>\u001b[30;1m</c>.</summary>
    DarkGray,

    /// <summary>The bright blue color. ANSI escape sequence: <c>\u001b[34;1m</c>.</summary>
    BrightBlue,

    /// <summary>The bright green color. ANSI escape sequence: <c>\u001b[32;1m</c>.</summary>
    BrightGreen,

    /// <summary>The bright cyan color. ANSI escape sequence: <c>\u001b[36;1m</c>.</summary>
    BrightCyan,

    /// <summary>The bright red color. ANSI escape sequence: <c>\u001b[31;1m</c>.</summary>
    BrightRed,

    /// <summary>The bright magenta color. ANSI escape sequence: <c>\u001b[35;1m</c>.</summary>
    BrightMagenta,

    /// <summary>The bright yellow color. ANSI escape sequence: <c>\u001b[33;1m</c>.</summary>
    BrightYellow,

    /// <summary>The White color (also known as Bright White). ANSI escape sequence: <c>\u001b[37;1m</c>.</summary>
    White
}
