using System.Numerics;
using System.Text.Json.Serialization;

namespace Terminal.Gui.Drawing;

/// <summary>
///     Represents the visual styling for a UI element, including foreground and background color and text style.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="Attribute"/> is a lightweight, immutable struct used to define how visual elements are rendered
///         in a terminal UI. It wraps color and style information in a platform-independent way and is used
///         extensively in <see cref="Scheme"/>, <see cref="VisualRole"/>, and theming infrastructure.
///     </para>
/// </remarks>
/// <seealso cref="Color"/>
/// <seealso cref="TextStyle"/>
/// <seealso cref="VisualRole"/>
/// <seealso cref="Scheme"/>
[JsonConverter (typeof (AttributeJsonConverter))]
public readonly record struct Attribute : IEqualityOperators<Attribute, Attribute, bool>
{
    /// <summary>Default empty attribute.</summary>
    [JsonIgnore]
    public static Attribute Default => new (Color.White, Color.Black);

    /// <summary>
    ///     Gets the foreground <see cref="Color"/> used to render text.
    /// </summary>
    [JsonConverter (typeof (ColorJsonConverter))]
    public Color Foreground { get; init; }

    /// <summary>
    ///     Gets the background <see cref="Color"/> used behind text.
    /// </summary>
    [JsonConverter (typeof (ColorJsonConverter))]
    public Color Background { get; init; }

    // TODO: Add constructors which permit including a Style.
    /// <summary>
    ///     Gets the <see cref="TextStyle"/> (e.g., bold, underline, italic) applied to text.
    /// </summary>
    public TextStyle Style { get; init; } = TextStyle.None;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Attribute"/> struct with default values.
    /// </summary>
    public Attribute () => this = Default;

    /// <summary>
    ///     Initializes a new <see cref="Attribute"/> from an existing instance, preserving explicit state.
    /// </summary>
    public Attribute (in Attribute attr) => this = attr;

    /// <summary>
    ///     Initializes an instance using two named colors.
    /// </summary>
    public Attribute (in Color foreground, in Color background)
    {
        Foreground = foreground;
        Background = background;

        Style = TextStyle.None;
    }

    /// <summary>
    ///     Initializes a new instance with foreground, background, and <see cref="TextStyle"/>.
    /// </summary>
    public Attribute (in Color foreground, in Color background, in TextStyle style)
    {
        Foreground = foreground;
        Background = background;
        Style = style;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Attribute"/> struct from string representations of colors and style.
    /// </summary>
    /// <param name="foreground">Foreground color as a string (name, hex, or rgb).</param>
    /// <param name="background">Background color as a string (name, hex, or rgb).</param>
    /// <param name="style">Optional style as a string (e.g., "Bold,Underline").</param>
    /// <exception cref="ArgumentException">Thrown if color parsing fails.</exception>
    public Attribute (in string foreground, in string background, in string? style = null)
    {
        Foreground = Color.Parse (foreground);
        Background = Color.Parse (background);

        Style = style is { } && Enum.TryParse (style, true, out TextStyle parsedStyle) ? parsedStyle : TextStyle.None;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Attribute"/> struct from string representations of colors and style.
    /// </summary>
    /// <param name="foreground">Foreground color as a string (name, hex, or rgb).</param>
    /// <param name="background">Background color as a string (name, hex, or rgb).</param>
    /// <param name="style">Optional style as a string (e.g., "Bold,Underline").</param>
    /// <exception cref="ArgumentException">Thrown if color parsing fails.</exception>
    public Attribute (in string foreground, in string background, in TextStyle style)
    {
        Foreground = Color.Parse (foreground);
        Background = Color.Parse (background);

        Style = style;
    }

    /// <summary>
    ///     INTERNAL: Initializes a new instance with a <see cref="ColorName16"/> value. Both <see cref="Foreground"/> and
    ///     <see cref="Background"/> will be set to the specified color.
    /// </summary>
    /// <param name="color16Name">Value.</param>
    internal Attribute (in ColorName16 color16Name) : this (in color16Name, in color16Name) { }

    /// <summary>
    ///     Initializes a new instance with foreground and background colors.
    /// </summary>
    public Attribute (in ColorName16 foreground16Name, in ColorName16 background16Name) : this (new Color (in foreground16Name),
                                                                                                new Color (in background16Name))
    { }

    /// <summary>
    ///     Initializes a new instance with foreground and background colors.
    /// </summary>
    public Attribute (in ColorName16 foreground16Name, in Color background) : this (new Color (in foreground16Name), in background) { }

    /// <summary>
    ///     Initializes a new instance with foreground and background colors.
    /// </summary>
    public Attribute (in Color foreground, in ColorName16 background16Name) : this (in foreground, new Color (in background16Name)) { }

    /// <summary>
    ///     INTERNAL: Initializes a new instance with a <see cref="StandardColors"/> value. Both <see cref="Foreground"/> and
    ///     <see cref="Background"/> will be set to the specified color.
    /// </summary>
    /// <param name="standardColor">Value.</param>
    internal Attribute (in StandardColor standardColor) : this (in standardColor, in standardColor) { }

    /// <summary>
    ///     Initializes a new instance with foreground and background colors.
    /// </summary>
    public Attribute (in StandardColor foreground, in StandardColor background) : this (new Color (in foreground), new Color (in background)) { }

    /// <summary>
    ///     Initializes a new instance with foreground and background colors.
    /// </summary>
    public Attribute (in StandardColor foreground, in Color background) : this (new Color (in foreground), in background) { }

    /// <summary>
    ///     Initializes a new instance with foreground and background colors.
    /// </summary>
    public Attribute (in Color foreground, in StandardColor background) : this (in foreground, new Color (in background)) { }

    /// <summary>
    ///     Initializes an instance using a single color for both foreground and background.
    /// </summary>
    public Attribute (in Color color) : this (color, color) { }

    /// <summary>
    ///     Initializes a new instance with foreground and background colors and a <see cref="TextStyle"/>.
    /// </summary>
    public Attribute (in StandardColor foreground, in StandardColor background, in TextStyle style) : this (new Color (in foreground),
                                                                                                            new Color (in background),
                                                                                                            style)
    { }

    /// <inheritdoc/>
    public bool Equals (Attribute other) => Foreground.Equals (other.Foreground) && Background.Equals (other.Background) && Style == other.Style;

    /// <inheritdoc/>
    public override int GetHashCode () => HashCode.Combine (Foreground, Background, Style);

    /// <inheritdoc/>
    public override string ToString () =>

        // Note: Unit tests are dependent on this format
        $"[{Foreground},{Background},{Style}]";
}
