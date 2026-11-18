using ColorHelper;

namespace Terminal.Gui.Views;

internal class BBar : ColorBar
{
    public GBar? GBar { get; set; }
    public RBar? RBar { get; set; }

    /// <inheritdoc/>
    protected override Color GetColor (double fraction)
    {
        if (RBar == null || GBar == null)
        {
            throw new ($"{nameof (BBar)} has not been set up correctly before drawing");
        }

        var rgb = new RGB ((byte)RBar.Value, (byte)GBar.Value, (byte)(MaxValue * fraction));

        return new (rgb.R, rgb.G, rgb.B);
    }

    /// <inheritdoc/>
    protected override int MaxValue => 255;
}
