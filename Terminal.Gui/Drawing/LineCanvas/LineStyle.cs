using System.Text.Json.Serialization;

namespace Terminal.Gui.Drawing;

/// <summary>Defines the style of lines for a <see cref="LineCanvas"/>.</summary>
[JsonConverter (typeof (JsonStringEnumConverter<LineStyle>))]
public enum LineStyle
{
    /// <summary>
    ///     No border is drawn. When used with <see cref="LineCanvas.AddLine(Point, int, Orientation, LineStyle, Attribute?)"/>,
    ///     <see cref="LineCanvas"/> does not treat this value specially — the line is stored and participates in
    ///     intersection resolution, rendering with default (single-line) glyphs. Callers that want eraser
    ///     semantics should use
    ///     <see cref="StraightLineExtensions.Exclude(IEnumerable{StraightLine}, Point, int, Orientation)"/>
    ///     to remove overlapping geometry from the line collection.
    /// </summary>
    None,

    /// <summary>The border is drawn using thin line Glyphs.</summary>
    Single,

    /// <summary>The border is drawn using thin line glyphs with dashed (double and triple) straight lines.</summary>
    Dashed,

    /// <summary>The border is drawn using thin line glyphs with short dashed (triple and quadruple) straight lines.</summary>
    Dotted,

    /// <summary>The border is drawn using thin double line Glyphs.</summary>
    Double,

    /// <summary>The border is drawn using heavy line Glyphs.</summary>
    Heavy,

    /// <summary>The border is drawn using heavy line glyphs with dashed (double and triple) straight lines.</summary>
    HeavyDashed,

    /// <summary>The border is drawn using heavy line glyphs with short dashed (triple and quadruple) straight lines.</summary>
    HeavyDotted,

    /// <summary>The border is drawn using thin line glyphs with rounded corners.</summary>
    Rounded,

    /// <summary>The border is drawn using thin line glyphs with rounded corners and dashed (double and triple) straight lines.</summary>
    RoundedDashed,

    /// <summary>
    ///     The border is drawn using thin line glyphs with rounded corners and short dashed (triple and quadruple)
    ///     straight lines.
    /// </summary>
    RoundedDotted

    // TODO: Support Ruler
    ///// <summary>
    ///// The border is drawn as a diagnostic ruler ("|123456789...").
    ///// </summary>
    //Ruler
}
