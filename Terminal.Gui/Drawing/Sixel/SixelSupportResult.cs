namespace Terminal.Gui.Drawing;

/// <summary>
///     Describes the discovered state of sixel support and ancillary information
///     e.g. <see cref="Resolution"/>. You can use any <see cref="SixelSupportDetector"/>
///     to discover this information.
/// </summary>
public class SixelSupportResult
{
    /// <summary>
    ///     Whether the terminal supports sixel graphic format.
    ///     Defaults to false.
    /// </summary>
    public bool IsSupported { get; set; }

    /// <summary>
    ///     The number of pixels of sixel that corresponds to each Col (<see cref="Size.Width"/>)
    ///     and each Row (<see cref="Size.Height"/>.  Defaults to 10x20.
    /// </summary>
    public Size Resolution { get; set; } = new (10, 20);

    /// <summary>
    ///     The maximum number of colors that can be included in a sixel image. Defaults
    ///     to 256.
    /// </summary>
    public int MaxPaletteColors { get; set; } = 256;

    /// <summary>
    ///     Whether the terminal supports transparent background sixels.
    ///     Defaults to false
    /// </summary>
    public bool SupportsTransparency { get; set; }
}
