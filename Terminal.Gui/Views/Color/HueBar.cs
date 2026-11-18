using ColorHelper;
using ColorConverter = ColorHelper.ColorConverter;

namespace Terminal.Gui.Views;

/// <summary>
///     A bar representing the Hue component of a <see cref="Color"/> in HSL color space.
/// </summary>
internal class HueBar : ColorBar
{
    /// <inheritdoc/>
    protected override Color GetColor (double fraction)
    {
        var hsl = new HSL ((int)(MaxValue * fraction), 100, 50);
        RGB rgb = ColorConverter.HslToRgb (hsl);

        return new (rgb.R, rgb.G, rgb.B);
    }

    /// <inheritdoc/>
    protected override int MaxValue => 360;
}
