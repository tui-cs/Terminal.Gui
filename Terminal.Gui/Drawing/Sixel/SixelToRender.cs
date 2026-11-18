namespace Terminal.Gui.Drawing;

/// <summary>
///     Describes a request to render a given <see cref="SixelData"/> at a given <see cref="ScreenPosition"/>.
///     Requires that the terminal and <see cref="IDriver"/> both support sixel.
/// </summary>
public class SixelToRender
{
    /// <summary>
    ///     gets or sets the encoded sixel data. Use <see cref="SixelEncoder"/> to convert bitmaps
    ///     into encoded sixel data.
    /// </summary>
    public string? SixelData { get; set; }

    /// <summary>
    ///     gets or sets where to move the cursor to before outputting the <see cref="SixelData"/>.
    /// </summary>
    public Point ScreenPosition { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier for this sixel render operation.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    ///     Gets or sets whether this sixel needs to be re-rendered to the terminal.
    ///     When <see langword="false"/>, the output pipeline skips writing this sixel's data.
    ///     Set to <see langword="true"/> when the owning view's content is invalidated (e.g. via
    ///     <see cref="ViewBase.View.SetNeedsDraw()"/>).
    /// </summary>
    public bool IsDirty { get; set; } = true;

    /// <summary>
    ///     Gets or sets whether this sixel should always be rendered to the terminal.
    ///     When <see langword="true"/>, the output pipeline always writes this sixel's data.
    ///     Set to <see langword="false"/> to only render when the owning view's content is
    ///     invalidated (e.g. via <see cref="ViewBase.View.SetNeedsDraw()"/>).
    /// </summary>
    public bool AlwaysRender { get; set; } = false;
}
