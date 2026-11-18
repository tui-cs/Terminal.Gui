using ColorHelper;
using ColorConverter = ColorHelper.ColorConverter;

namespace Terminal.Gui.Views;

internal class LightnessBar : ColorBar
{
    public HueBar? HBar { get; set; }
    public SaturationBar? SBar { get; set; }

    /// <inheritdoc/>
    protected override Color GetColor (double fraction)
    {
        if (HBar == null || SBar == null)
        {
            throw new ($"{nameof (LightnessBar)} has not been set up correctly before drawing");
        }

        var hsl = new HSL (HBar.Value, (byte)SBar.Value, (byte)(MaxValue * fraction));
        RGB rgb = ColorConverter.HslToRgb (hsl);

        return new (rgb.R, rgb.G, rgb.B);
    }

    /// <inheritdoc/>
    protected override int MaxValue => 100;
}
