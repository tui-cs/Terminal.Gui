using System.Collections.Frozen;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using ColorHelper;
using ColorConverter = ColorHelper.ColorConverter;

#pragma warning disable CS0618 // Obsolete - Color still uses ConfigurationPropertyAttribute during transition

namespace Terminal.Gui.Drawing;

/// <summary>
///     Represents a 24-bit color encoded in ARGB32 format.
///     <para>
///         The RGB components define the color identity (what color it is), while the alpha channel defines
///         rendering intent (how transparent it should be when drawn).
///     </para>
/// </summary>
/// <remarks>
///     <para>
///         When matching colors to standard color names (e.g., via <see cref="ColorStrings.GetColorName"/>),
///         the alpha channel is ignored. This means colors with the same RGB values but different alpha values
///         will resolve to the same color name. This design supports transparency features while maintaining
///         semantic color identity.
///     </para>
///     <para>
///         While Terminal.Gui does not currently support alpha blending during rendering, the alpha channel
///         is used to indicate rendering intent:
///         <list type="bullet">
///             <item>
///                 <description>Alpha = 0: Fully transparent (don't render)</description>
///             </item>
///             <item>
///                 <description>Alpha = 255: Fully opaque (normal rendering)</description>
///             </item>
///             <item>
///                 <description>Other values: Reserved for future alpha blending support</description>
///             </item>
///         </list>
///     </para>
/// </remarks>
/// <seealso cref="Attribute"/>
/// <seealso cref="ColorExtensions"/>
/// <seealso cref="ColorName16"/>
[JsonConverter (typeof (ColorJsonConverter))]
[StructLayout (LayoutKind.Explicit)]
public readonly partial record struct Color : ISpanParsable<Color>, IUtf8SpanParsable<Color>, ISpanFormattable, IUtf8SpanFormattable, IMinMaxValue<Color>
{
    /// <summary>
    ///     No color (alpha = 0). When used in an <see cref="Attribute"/>, the terminal's default
    ///     foreground or background color is used instead of an explicit color. This allows the terminal's native
    ///     background (including any transparency or acrylic effects) to show through.
    /// </summary>
    /// <remarks>
    ///     Uses ARGB 0x00FFFFFF (alpha=0, RGB=white) as the sentinel value. This ensures it is distinguishable from
    ///     <c>default(Color)</c> which has all bytes zeroed.
    /// </remarks>
    public static readonly Color None = new (255, 255, 255, 0);

    /// <summary>The value of the alpha channel component</summary>
    /// <remarks>
    ///     <para>
    ///         The alpha channel represents rendering intent (transparency) rather than color identity.
    ///         Terminal.Gui does not currently perform alpha blending, but uses this value to determine
    ///         whether to render the color at all (alpha = 0 means don't render).
    ///     </para>
    ///     <para>
    ///         When matching colors to standard color names, the alpha channel is ignored. For example,
    ///         <c>new Color(255, 0, 0, 255)</c> and <c>new Color(255, 0, 0, 128)</c> will both be
    ///         identified as "Red".
    ///     </para>
    /// </remarks>
    [JsonIgnore]
    [field: FieldOffset (3)]
    public readonly byte A;

    /// <summary>The value of this <see cref="Color"/> as a <see langword="uint"/> in ARGB32 format.</summary>
    /// <remarks>
    ///     The alpha channel in the ARGB value represents rendering intent (transparency), not color identity.
    ///     When matching to standard color names, only the RGB components are considered.
    /// </remarks>
    [JsonIgnore]
    [field: FieldOffset (0)]
    public readonly uint Argb;

    /// <summary>The value of the blue color component.</summary>
    [JsonIgnore]
    [field: FieldOffset (0)]
    public readonly byte B;

    /// <summary>The value of the green color component.</summary>
    [JsonIgnore]
    [field: FieldOffset (1)]
    public readonly byte G;

    /// <summary>The value of the red color component.</summary>
    [JsonIgnore]
    [field: FieldOffset (2)]
    public readonly byte R;

    /// <summary>The value of this <see cref="Color"/> encoded as a signed 32-bit integer in ARGB32 format.</summary>
    [JsonIgnore]
    [field: FieldOffset (0)]
    public readonly int Rgba;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Color"/> <see langword="struct"/> using the supplied component
    ///     values.
    /// </summary>
    /// <param name="red">The red 8-bits.</param>
    /// <param name="green">The green 8-bits.</param>
    /// <param name="blue">The blue 8-bits.</param>
    /// <param name="alpha">Optional; defaults to 0xFF. The Alpha channel is not supported by Terminal.Gui.</param>
    /// <remarks>Alpha channel is not currently supported by Terminal.Gui.</remarks>
    /// <exception cref="OverflowException">If the value of any parameter is greater than <see cref="byte.MaxValue"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If the value of any parameter is negative.</exception>
    public Color (int red = 0, int green = 0, int blue = 0, int alpha = byte.MaxValue)
    {
        ArgumentOutOfRangeException.ThrowIfNegative (red);
        ArgumentOutOfRangeException.ThrowIfNegative (green);
        ArgumentOutOfRangeException.ThrowIfNegative (blue);
        ArgumentOutOfRangeException.ThrowIfNegative (alpha);

        A = Convert.ToByte (alpha);
        R = Convert.ToByte (red);
        G = Convert.ToByte (green);
        B = Convert.ToByte (blue);
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Color"/> class with an encoded signed 32-bit color value in
    ///     ARGB32 format.
    /// </summary>
    /// <param name="rgba">The encoded 32-bit color value (see <see cref="Rgba"/>).</param>
    /// <remarks>
    ///     The alpha channel is not currently supported, so the value of the alpha channel bits will not affect
    ///     rendering.
    /// </remarks>
    public Color (int rgba) => Rgba = rgba;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Color"/> class with an encoded unsigned 32-bit color value in
    ///     ARGB32 format.
    /// </summary>
    /// <param name="argb">The encoded unsigned 32-bit color value (see <see cref="Argb"/>).</param>
    /// <remarks>
    ///     The alpha channel is not currently supported, so the value of the alpha channel bits will not affect
    ///     rendering.
    /// </remarks>
    public Color (uint argb) => Argb = argb;

    /// <summary>Initializes a new instance of the <see cref="Color"/> color from a legacy 16-color named value.</summary>
    /// <param name="colorName">The 16-color value.</param>
    public Color (in ColorName16 colorName) => this = ColorExtensions.ColorName16ToColorMap! [colorName];

    /// <summary>
    ///     Initializes a new instance of the <see cref="Color"/> color from a value in the <see cref="StandardColor"/>
    ///     enum.
    /// </summary>
    /// <param name="colorName">The 16-color value.</param>
    public Color (in StandardColor colorName) : this (StandardColors.GetArgb (colorName)) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Color"/> color from string. See
    ///     <see cref="TryParse(string, out Color?)"/> for details.
    /// </summary>
    /// <param name="colorString"></param>
    /// <exception cref="ArgumentNullException">If <paramref name="colorString"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    ///     If <paramref name="colorString"/> is an empty string or consists of only whitespace
    ///     characters.
    /// </exception>
    /// <exception cref="ColorParseException">If thrown by <see cref="Parse(string?,System.IFormatProvider?)"/></exception>
    public Color (string colorString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace (colorString);
        this = Parse (colorString, CultureInfo.InvariantCulture);
    }

    /// <summary>Initializes a new instance of the <see cref="Color"/> with all channels set to 0.</summary>
    public Color () => Argb = 0u;

    /// <summary>Gets or sets the 3-byte/6-character hexadecimal value for each of the legacy 16-color values.</summary>
    public static Dictionary<ColorName16, string> Colors16
    {
        get =>

            // Transform _colorToNameMap into a Dictionary<ColorNames,string>
            ColorExtensions.ColorToName16Map!.ToDictionary (static kvp => kvp.Value, static kvp => kvp.Key.ToString ("g"));
        set
        {
            // Transform Dictionary<ColorNames,string> into _colorToNameMap
            ColorExtensions.ColorToName16Map = value.ToFrozenDictionary (GetColorToNameMapKey, GetColorToNameMapValue);

            return;

            static Color GetColorToNameMapKey (KeyValuePair<ColorName16, string> kvp) => new (kvp.Value);

            static ColorName16 GetColorToNameMapValue (KeyValuePair<ColorName16, string> kvp) =>
                Enum.TryParse (kvp.Key.ToString (), true, out ColorName16 colorName)
                    ? colorName
                    : throw new ArgumentException ($"Invalid color name: {kvp.Key}");
        }
    }

    /// <summary>
    ///     Gets the <see cref="Color"/> using a legacy 16-color <see cref="ColorName16"/> value. <see langword="get"/> will
    ///     return the closest 16 color match to the true color when no exact value is found.
    /// </summary>
    /// <remarks>
    ///     Get returns the <see cref="GetClosestNamedColor16(Color)"/> of the closest 24-bit color value. Set sets the RGB
    ///     value using a hard-coded map.
    /// </remarks>
    public AnsiColorCode GetAnsiColorCode () => ColorExtensions.ColorName16ToAnsiColorMap [GetClosestNamedColor16 ()];

    /// <summary>
    ///     Gets the <see cref="Color"/> using a legacy 16-color <see cref="ColorName16"/> value. <see langword="get"/>
    ///     will return the closest 16 color match to the true color when no exact value is found.
    /// </summary>
    /// <remarks>
    ///     Get returns the <see cref="GetClosestNamedColor16(Color)"/> of the closest 24-bit color value. Set
    ///     sets the RGB
    ///     value using a hard-coded map.
    /// </remarks>
    public ColorName16 GetClosestNamedColor16 () => GetClosestNamedColor16 (this);

    /// <summary>
    ///     Determines if the closest named <see cref="Color"/> to <see langword="this"/> is the provided
    ///     <paramref name="namedColor"/>.
    /// </summary>
    /// <param name="namedColor">
    ///     The <see cref="GetClosestNamedColor16(Color)"/> to check if this <see cref="Color"/> is closer
    ///     to than any other configured named color.
    /// </param>
    /// <returns>
    ///     <see langword="true"/> if the closest named color is the provided value. <br/> <see langword="false"/> if any
    ///     other named color is closer to this <see cref="Color"/> than <paramref name="namedColor"/>.
    /// </returns>
    /// <remarks>
    ///     If <see langword="this"/> is equidistant from two named colors, the result of this method is not guaranteed to
    ///     be determinate.
    /// </remarks>
    [Pure]
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public bool IsClosestToNamedColor16 (in ColorName16 namedColor) => GetClosestNamedColor16 () == namedColor;

    /// <summary>Gets the "closest" named color to this <see cref="Color"/> value.</summary>
    /// <param name="inputColor"></param>
    /// <remarks>
    ///     Distance is defined here as the Euclidean distance between each color interpreted as a <see cref="Vector3"/>.
    /// </remarks>
    /// <returns></returns>
    [SkipLocalsInit]
    internal static ColorName16 GetClosestNamedColor16 (Color inputColor) =>
        ColorExtensions.ColorToName16Map!.MinBy (pair => CalculateColorDistance (inputColor, pair.Key)).Value;

    [SkipLocalsInit]
    private static float CalculateColorDistance (in Vector4 color1, in Vector4 color2) => Vector4.Distance (color1, color2);

    /// <summary>
    ///     Returns a "highlighted" version of this color — visually more prominent against
    ///     the given background context.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The algorithm works in HSL color space and adjusts the lightness channel. When
    ///         <paramref name="isDarkBackground"/> is provided, the direction is explicit. When <see langword="null"/>,
    ///         direction is auto-detected from this color's own lightness:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>Dark backgrounds (or dark colors when auto-detecting): lightness is increased.</description>
    ///             </item>
    ///             <item>
    ///                 <description>Light backgrounds (or light colors when auto-detecting): lightness is decreased.</description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     If the adjustment resulted in a color too close to the original, a larger adjustment is
    ///                     made.
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <param name="brightenAmount">The percent amount to adjust the lightness by. The default is <c>20%</c>.</param>
    /// <param name="isDarkBackground">
    ///     If <see langword="true"/>, brightens (increases lightness) for visibility on dark backgrounds.
    ///     If <see langword="false"/>, darkens (decreases lightness) for visibility on light backgrounds.
    ///     If <see langword="null"/>, auto-detects based on this color's own lightness (default/backward-compatible behavior).
    /// </param>
    public Color GetBrighterColor (double brightenAmount = 0.2, bool? isDarkBackground = null)
    {
        HSL hsl = ColorConverter.RgbToHsl (new RGB (R, G, B));

        double lNorm = hsl.L / 100.0;

        // Determine direction: on dark bg, brighten (increase L); on light bg, darken (decrease L)
        bool shouldIncrease = isDarkBackground ?? lNorm < 0.5;

        double newL = shouldIncrease ? Math.Min (1.0, lNorm + brightenAmount) : Math.Max (0.0, lNorm - brightenAmount);

        if (Math.Abs (newL - lNorm) < 0.1)
        {
            newL = shouldIncrease ? Math.Min (1.0, lNorm + 2 * brightenAmount) : Math.Max (0.0, lNorm - 2 * brightenAmount);
        }

        HSL newHsl = new (hsl.H, hsl.S, (byte)(newL * 100));
        RGB rgb = ColorConverter.HslToRgb (newHsl);

        return new Color (rgb.R, rgb.G, rgb.B);
    }

    /// <summary>
    ///     Returns a "dimmed" version of this color appropriate for the given background context.
    ///     On dark backgrounds, dims by reducing lightness (darker). On light backgrounds, dims by
    ///     increasing lightness (lighter/washed out), moving the color toward the background.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The algorithm works in HSL color space and adjusts the lightness channel:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     If the color is already at the extreme for the given direction, returns a context-appropriate
    ///                     gray (<see cref="ColorName16.DarkGray"/> for dark backgrounds,
    ///                     <see cref="ColorName16.Gray"/> for light backgrounds).
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>Otherwise, adjusts lightness by the specified amount in the appropriate direction.</description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     If the adjustment resulted in a color too close to the original, a larger adjustment is
    ///                     made.
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <param name="dimAmount">The percent amount to dim the color by. The default is <c>20%</c>.</param>
    /// <param name="isDarkBackground">
    ///     If <see langword="true"/>, dims by reducing lightness (darker, toward the dark background).
    ///     If <see langword="false"/>, dims by increasing lightness (washed out, toward the light background).
    ///     If <see langword="null"/>, always reduces lightness (default/backward-compatible behavior).
    /// </param>
    public Color GetDimmerColor (double dimAmount = 0.2, bool? isDarkBackground = null)
    {
        HSL hsl = ColorConverter.RgbToHsl (new RGB (R, G, B));

        double lNorm = hsl.L / 100.0;

        // Determine direction: on dark bg (or null/default), reduce L; on light bg, increase L
        bool shouldDecrease = isDarkBackground ?? true;

        // If the color is already at the extreme for the given direction, return a context-appropriate gray.
        // Note: ColorHelper's HSL uses L in range 0-100.
        if (shouldDecrease && hsl.L <= 10)
        {
            return new Color (ColorName16.DarkGray);
        }

        if (!shouldDecrease && hsl.L >= 90)
        {
            return new Color (ColorName16.Gray);
        }

        double newL = shouldDecrease ? Math.Max (0.0, lNorm - dimAmount) : Math.Min (1.0, lNorm + dimAmount);

        // If the new lightness is too close to the original, force a bigger change
        if (Math.Abs (newL - lNorm) < 0.1)
        {
            newL = shouldDecrease ? Math.Max (0.0, lNorm - 2 * dimAmount) : Math.Min (1.0, lNorm + 2 * dimAmount);
        }

        HSL newHsl = new (hsl.H, hsl.S, (byte)(newL * 100));
        RGB rgb = ColorConverter.HslToRgb (newHsl);

        return new Color (rgb.R, rgb.G, rgb.B);
    }

    /// <summary>
    ///     Returns <see langword="true"/> if this color is "dark" (HSL lightness &lt; 0.5).
    /// </summary>
    /// <returns>
    ///     <see langword="true"/> if the color's HSL lightness is below 0.5; otherwise <see langword="false"/>.
    /// </returns>
    public bool IsDarkColor ()
    {
        HSL hsl = ColorConverter.RgbToHsl (new RGB (R, G, B));

        // ColorHelper's HSL uses L in range 0-100
        return hsl.L < 50;
    }

    #region Legacy Color Names

    // ReSharper disable InconsistentNaming

    /// <summary>The black color.</summary>
    public const ColorName16 Black = ColorName16.Black;

    /// <summary>The blue color.</summary>
    public const ColorName16 Blue = ColorName16.Blue;

    /// <summary>The green color.</summary>
    public const ColorName16 Green = ColorName16.Green;

    /// <summary>The cyan color.</summary>
    public const ColorName16 Cyan = ColorName16.Cyan;

    /// <summary>The red color.</summary>
    public const ColorName16 Red = ColorName16.Red;

    /// <summary>The magenta color.</summary>
    public const ColorName16 Magenta = ColorName16.Magenta;

    /// <summary>The yellow color.</summary>
    public const ColorName16 Yellow = ColorName16.Yellow;

    /// <summary>The gray color.</summary>
    public const ColorName16 Gray = ColorName16.Gray;

    /// <summary>The dark gray color.</summary>
    public const ColorName16 DarkGray = ColorName16.DarkGray;

    /// <summary>The bright bBlue color.</summary>
    public const ColorName16 BrightBlue = ColorName16.BrightBlue;

    /// <summary>The bright green color.</summary>
    public const ColorName16 BrightGreen = ColorName16.BrightGreen;

    /// <summary>The bright cyan color.</summary>
    public const ColorName16 BrightCyan = ColorName16.BrightCyan;

    /// <summary>The bright red color.</summary>
    public const ColorName16 BrightRed = ColorName16.BrightRed;

    /// <summary>The bright magenta color.</summary>
    public const ColorName16 BrightMagenta = ColorName16.BrightMagenta;

    /// <summary>The bright yellow color.</summary>
    public const ColorName16 BrightYellow = ColorName16.BrightYellow;

    /// <summary>The White color.</summary>
    public const ColorName16 White = ColorName16.White;

    #endregion
}
