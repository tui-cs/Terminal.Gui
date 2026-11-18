using System.Text.Json.Serialization;

namespace Terminal.Gui.ViewBase;

/// <summary>
///     Defines the style of shadow to be drawn on the right and bottom sides of the <see cref="View"/>.
/// </summary>
[JsonConverter (typeof (JsonStringEnumConverter<ShadowStyles>))]
public enum ShadowStyles
{
    /// <summary>
    ///     No shadow.
    /// </summary>
    None,

    /// <summary>
    ///     A shadow that is drawn using block elements. Ideal for smaller views such as buttons.
    /// </summary>
    Opaque,

    /// <summary>
    ///     A shadow that is drawn using the underlying text with a darker background. Ideal for larger views such as windows.
    /// </summary>
    Transparent
}
