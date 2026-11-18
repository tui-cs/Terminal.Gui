using System.Text.Json.Serialization;

namespace Terminal.Gui.Drawing;

/// <summary>
///     Defines non-color style flags for an <see cref="Attribute"/>.
/// </summary>
/// <remarks>
///     <para>
///         Only a subset of ANSI SGR (Select Graphic Rendition) styles are represented.
///         Styles that are poorly supported, non-visual, or redundant with other APIs are omitted.
///     </para>
///     <para>
///         Multiple styles can be combined using bitwise operations. Use <see cref="Attribute.Style"/>
///         to get or set these styles on an <see cref="Attribute"/>.
///     </para>
///     <para>
///         Note that <see cref="TextStyle.Bold"/> and <see cref="TextStyle.Faint"/> may be mutually exclusive depending on
///         the user's terminal and its settings. For instance, if a terminal displays faint text as a darker color, and
///         bold text as a lighter color, then both cannot
///         be shown at the same time, and it will be up to the terminal to decide which to display.
///     </para>
/// </remarks>
[Flags]
[JsonConverter (typeof (JsonStringEnumConverter<TextStyle>))]
public enum TextStyle : byte
{
    /// <summary>
    ///     No text style.
    /// </summary>
    /// <remarks>Corresponds to no active SGR styles.</remarks>
    None = 0b_0000_0000,

    /// <summary>
    ///     Bold text.
    /// </summary>
    /// <remarks>
    ///     SGR code: 1 (Bold). May be mutually exclusive with <see cref="TextStyle.Faint"/>, see <see cref="TextStyle"/>
    ///     remarks.
    /// </remarks>
    Bold = 0b_0000_0001,

    /// <summary>
    ///     Faint (dim) text.
    /// </summary>
    /// <remarks>
    ///     SGR code: 2 (Faint). Not widely supported on all terminals. May be mutually exclusive with
    ///     <see cref="TextStyle.Bold"/>, see
    ///     <see cref="TextStyle"/> remarks.
    /// </remarks>
    Faint = 0b_0000_0010,

    /// <summary>
    ///     Italic text.
    /// </summary>
    /// <remarks>SGR code: 3 (Italic). Some terminals may not support italic rendering.</remarks>
    Italic = 0b_0000_0100,

    /// <summary>
    ///     Underlined text.
    /// </summary>
    /// <remarks>SGR code: 4 (Underline).</remarks>
    Underline = 0b_0000_1000,

    /// <summary>
    ///     Slow blinking text.
    /// </summary>
    /// <remarks>SGR code: 5 (Slow Blink). Support varies; blinking is often disabled in modern terminals.</remarks>
    Blink = 0b_0001_0000,

    /// <summary>
    ///     Reverse video (swaps foreground and background colors).
    /// </summary>
    /// <remarks>SGR code: 7 (Reverse Video).</remarks>
    Reverse = 0b_0010_0000,

    /// <summary>
    ///     Strikethrough (crossed-out) text.
    /// </summary>
    /// <remarks>SGR code: 9 (Crossed-out / Strikethrough).</remarks>
    Strikethrough = 0b_0100_0000
}
