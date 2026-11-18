using ColorHelper;
using ColorConverter = ColorHelper.ColorConverter;

namespace Terminal.Gui.Views;

internal class SaturationBar : ColorBar
{
    public HueBar? HBar { get; set; }

    // Should only have either LBar or VBar not both
    public LightnessBar? LBar { get; set; }
    public ValueBar? VBar { get; set; }

    /// <inheritdoc/>
    protected override Color GetColor (double fraction)
    {
        if (LBar != null && HBar != null)
        {
            var hsl = new HSL (HBar.Value, (byte)(MaxValue * fraction), (byte)LBar.Value);
            RGB rgb = ColorConverter.HslToRgb (hsl);

            return new (rgb.R, rgb.G, rgb.B);
        }

        if (VBar != null && HBar != null)
        {
            var hsv = new HSV (HBar.Value, (byte)(MaxValue * fraction), (byte)VBar.Value);
            RGB rgb = ColorConverter.HsvToRgb (hsv);

            return new (rgb.R, rgb.G, rgb.B);
        }

        throw new ($"{nameof (SaturationBar)} requires either Lightness or Value to render");
    }

    /// <inheritdoc/>
    protected override int MaxValue => 100;
}
