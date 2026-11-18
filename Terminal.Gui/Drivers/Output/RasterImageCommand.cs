namespace Terminal.Gui.Drivers;

/// <summary>
///     Describes a raster image to compose through the output buffer.
/// </summary>
public class RasterImageCommand
{
    /// <summary>
    ///     Gets or sets the stable identifier for this raster image.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    ///     Gets or sets the image pixels to encode.
    /// </summary>
    public Color [,]? Pixels { get; set; }

    /// <summary>
    ///     Gets or sets pre-encoded sixel data for <see cref="Pixels"/>.
    /// </summary>
    /// <remarks>
    ///     This is used only when the full <see cref="DestinationCells"/> rectangle is visible. Clipped output still
    ///     encodes the cropped pixels so the emitted sixel dimensions match the clipped cell region.
    /// </remarks>
    public string? EncodedSixel { get; set; }

    /// <summary>
    ///     Gets or sets pre-encoded Kitty graphics protocol data for <see cref="Pixels"/>.
    /// </summary>
    /// <remarks>
    ///     This is used only when the full <see cref="DestinationCells"/> rectangle is visible. Clipped output still
    ///     re-encodes the cropped pixels so the emitted dimensions match the clipped cell region.
    /// </remarks>
    public string? EncodedKitty { get; set; }

    /// <summary>
    ///     Gets or sets the screen cells occupied by <see cref="Pixels"/>.
    /// </summary>
    public Rectangle DestinationCells { get; set; }

    /// <summary>
    ///     When set (Kitty only), <see cref="Pixels"/> is the <em>full</em> source image and this is the crop of
    ///     it (in image pixels) to display in <see cref="DestinationCells"/>. The image is transmitted to the
    ///     terminal once; subsequent frames that only change the crop/destination (pan, zoom of a static image)
    ///     emit a tiny placement update instead of re-sending the pixels, avoiding the per-frame flash. When
    ///     <see langword="null"/> the legacy path is used: <see cref="Pixels"/> is the already-scaled region and
    ///     is (re)transmitted each dirty frame.
    /// </summary>
    public Rectangle? SourceRect { get; set; }

    /// <summary>
    ///     A monotonic version of the <see cref="Pixels"/> contents, used only by the Kitty source-crop path
    ///     to decide whether the resident image must be re-transmitted. The source-crop path keeps the full
    ///     image resident in the terminal and pans/zooms it with placement updates; it re-transmits when this
    ///     version changes. Hosts (e.g. <c>ImageView</c>) should bump it whenever the pixel contents change —
    ///     including when the <em>same</em> array instance is reused for a new render, which a reference
    ///     comparison alone would miss.
    /// </summary>
    public int SourceVersion { get; set; }

    /// <summary>
    ///     Gets or sets the clip region captured when the image is added to the output buffer.
    /// </summary>
    public Region? Clip { get; set; }

    /// <summary>
    ///     Gets or sets the sixel encoder to use. A default encoder is used when this is <see langword="null"/>.
    /// </summary>
    public SixelEncoder? Encoder { get; set; }

    /// <summary>
    ///     Gets or sets whether the image needs to be emitted during the next driver write.
    /// </summary>
    public bool IsDirty { get; set; } = true;

    /// <summary>
    ///     Gets or sets whether the image should be emitted on every driver write.
    /// </summary>
    public bool AlwaysRender { get; set; }

    /// <summary>
    ///     Gets or sets whether the image should be emitted after dirty text cells rather than before them.
    /// </summary>
    /// <remarks>
    ///     The default is <see langword="false"/> so text drawn later can appear above a raster image. Set this to
    ///     <see langword="true"/> for animated Sixel overlays that need to repaint after the normal cell pass to
    ///     avoid visible flicker.
    /// </remarks>
    public bool RenderAfterText { get; set; }

    /// <summary>
    ///     Gets or sets whether covered blank cells need to be emitted as transparent blanks before rendering.
    /// </summary>
    internal bool NeedsTransparentCellClear { get; set; }
}
