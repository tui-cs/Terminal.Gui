using ColorHelper;

namespace Terminal.Gui.Views;

internal class RBar : ColorBar
{
    public BBar? BBar { get; set; }
    public GBar? GBar { get; set; }

    /// <inheritdoc/>
    protected override Color GetColor (double fraction)
    {
        if (GBar == null || BBar == null)
        {
            throw new ($"{nameof (RBar)} has not been set up correctly before drawing");
        }

        var rgb = new RGB ((byte)(MaxValue * fraction), (byte)GBar.Value, (byte)BBar.Value);

        return new (rgb.R, rgb.G, rgb.B);
    }

    /// <inheritdoc/>
    protected override int MaxValue => 255;
}
