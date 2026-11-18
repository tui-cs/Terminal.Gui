namespace Terminal.Gui.Drawing;

/// <summary>
///     Describes the discovered state of Kitty graphics protocol support and ancillary information
///     such as <see cref="Resolution"/>. Use <see cref="KittyGraphicsSupportDetector"/> to populate this.
/// </summary>
public class KittyGraphicsSupportResult
{
    /// <summary>
    ///     Whether the terminal supports the Kitty graphics protocol.
    ///     Defaults to <see langword="false"/>.
    /// </summary>
    public bool IsSupported { get; set; }

    /// <summary>
    ///     The number of pixels that corresponds to each column (<see cref="Size.Width"/>)
    ///     and each row (<see cref="Size.Height"/>). Defaults to 10×20.
    /// </summary>
    public Size Resolution { get; set; } = new (10, 20);
}
