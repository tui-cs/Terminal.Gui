using ColorHelper;

namespace Terminal.Gui.Views;

internal class GBar : ColorBar
{
    public BBar? BBar { get; set; }
    public RBar? RBar { get; set; }

    /// <inheritdoc/>
    protected override Color GetColor (double fraction)
    {
        if (RBar == null || BBar == null)
        {
            throw new ($"{nameof (GBar)} has not been set up correctly before drawing");
        }

        var rgb = new RGB ((byte)RBar.Value, (byte)(MaxValue * fraction), (byte)BBar.Value);

        return new (rgb.R, rgb.G, rgb.B);
    }

    /// <inheritdoc/>
    protected override int MaxValue => 255;
}
