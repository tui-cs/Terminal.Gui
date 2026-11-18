namespace Terminal.Gui.Drawing;

/// <summary>
///     Encodes a pixel array into the Kitty terminal graphics protocol APC escape sequence stream.
/// </summary>
/// <remarks>
///     <para>
///         The Kitty graphics protocol transmits images via APC sequences of the form:
///         <c>ESC_G &lt;key=value,...&gt;;&lt;base64-data&gt;ESC\</c>.
///     </para>
///     <para>
///         Images are encoded as raw 32-bit RGBA pixel data (no palette, no quantization), split
///         into chunks of at most <see cref="MaxChunkSize"/> base64 characters. Multi-chunk
///         transmissions set <c>m=1</c> on all chunks except the last (<c>m=0</c>).
///     </para>
///     <para>
///         See <see href="https://sw.kovidgoyal.net/kitty/graphics-protocol/"/> for the full spec.
///     </para>
/// </remarks>
public class KittyGraphicsEncoder
{
    /// <summary>
    ///     Maximum number of base64-encoded characters per APC chunk.
    ///     The Kitty protocol recommends ≤4096 bytes of payload per chunk.
    /// </summary>
    public const int MaxChunkSize = 4096;

    // Each image id carries exactly one placement, so a single stable placement id is enough for an
    // a=T to replace it in place across repaints (see BuildApcSequence).
    private const int PlacementId = 1;

    // APC = ESC _ ... ESC \
    private const string APC_START = "\x1b_G";
    private const string APC_END = "\x1b\\";

    /// <summary>
    ///     Encodes the provided pixel array into a Kitty graphics protocol APC sequence string
    ///     that, when written to the terminal, renders the image at the current cursor position
    ///     occupying <paramref name="destCols"/> columns and <paramref name="destRows"/> rows.
    /// </summary>
    /// <param name="pixels">
    ///     The pixel array to encode, indexed as <c>[x, y]</c> where the first dimension is width
    ///     and the second is height.
    /// </param>
    /// <param name="destCols">The number of terminal columns the image should occupy.</param>
    /// <param name="destRows">The number of terminal rows the image should occupy.</param>
    /// <param name="imageId">
    ///     An optional stable image identifier (the Kitty <c>i</c> key). When supplied, placements can be
    ///     deleted and replaced by id via <see cref="EncodeDeletePlacements"/>, which is required to erase
    ///     a previous placement when the image is resized or moved. Use <see cref="GetImageId"/> to derive
    ///     a stable id from a string. When <see langword="null"/> the terminal assigns its own id and the
    ///     placement cannot be targeted for deletion.
    /// </param>
    /// <returns>The complete Kitty APC escape sequence string.</returns>
    public string EncodeKitty (Color [,] pixels, int destCols, int destRows, int? imageId = null)
    {
        int width = pixels.GetLength (0);
        int height = pixels.GetLength (1);

        byte [] rgba = PixelsToRgba (pixels, width, height);
        string base64 = Convert.ToBase64String (rgba);

        return BuildApcSequence (base64, width, height, destCols, destRows, imageId);
    }

    /// <summary>
    ///     Builds a Kitty graphics protocol APC sequence that deletes all placements of the image with the
    ///     specified <paramref name="imageId"/>, leaving the transmitted image data intact so it can be
    ///     re-displayed.
    /// </summary>
    /// <remarks>
    ///     Kitty image placements persist on screen until explicitly deleted — unlike Sixel, drawing text
    ///     over their cells does not erase them. Emit this before re-placing a resized or moved image, and
    ///     when an image is removed, so a stale placement is not left behind.
    /// </remarks>
    /// <param name="imageId">The image id (the Kitty <c>i</c> key) whose placements should be deleted.</param>
    /// <returns>The complete Kitty APC delete escape sequence string.</returns>
    public static string EncodeDeletePlacements (int imageId) => $"{APC_START}a=d,d=i,i={imageId}{APC_END}";

    /// <summary>
    ///     Encodes a transmit-only (<c>a=t</c>) Kitty sequence: it sends the full image data under the given
    ///     <paramref name="imageId"/> without creating a placement. The image stays resident in the terminal so
    ///     it can be displayed — and re-displayed with a different crop — via <see cref="EncodePut"/> without
    ///     re-sending the pixels. Used to pan/zoom a static image with tiny per-frame placement updates instead
    ///     of re-transmitting the whole image every frame (which reads as a flash for large images).
    /// </summary>
    /// <param name="pixels">The full source image to transmit, indexed as <c>[x, y]</c>.</param>
    /// <param name="imageId">The stable image id (the Kitty <c>i</c> key).</param>
    /// <returns>The complete Kitty APC transmit-only escape sequence (chunked).</returns>
    public string EncodeTransmit (Color [,] pixels, int imageId)
    {
        int width = pixels.GetLength (0);
        int height = pixels.GetLength (1);
        byte [] rgba = PixelsToRgba (pixels, width, height);
        string base64 = Convert.ToBase64String (rgba);

        var sb = new StringBuilder ();
        int total = base64.Length;
        var offset = 0;

        string firstChunk = offset + MaxChunkSize < total ? base64.Substring (offset, MaxChunkSize) : base64.Substring (offset);
        bool isLastChunk = offset + firstChunk.Length >= total;

        sb.Append (APC_START);
        sb.Append ($"a=t,f=32,i={imageId},s={width},v={height},q=2,m={( isLastChunk ? 0 : 1 )}");
        sb.Append (';');
        sb.Append (firstChunk);
        sb.Append (APC_END);
        offset += firstChunk.Length;

        while (offset < total)
        {
            string chunk = offset + MaxChunkSize < total ? base64.Substring (offset, MaxChunkSize) : base64.Substring (offset);
            bool last = offset + chunk.Length >= total;

            sb.Append (APC_START);
            sb.Append ($"m={( last ? 0 : 1 )}");
            sb.Append (';');
            sb.Append (chunk);
            sb.Append (APC_END);
            offset += chunk.Length;
        }

        return sb.ToString ();
    }

    /// <summary>
    ///     Encodes a placement (<c>a=p</c>) that displays a crop of an already-transmitted image (see
    ///     <see cref="EncodeTransmit"/>) at the current cursor position. The source rectangle
    ///     (<paramref name="srcX"/>,<paramref name="srcY"/>,<paramref name="srcW"/>,<paramref name="srcH"/>) in
    ///     image pixels is scaled to fill <paramref name="destCols"/>×<paramref name="destRows"/> cells. A later
    ///     placement with the same (image id, placement id) replaces this one in place — so panning/zooming a
    ///     static image is a tiny, flash-free update rather than a full re-transmit.
    /// </summary>
    /// <param name="imageId">The id of the already-transmitted image.</param>
    /// <param name="placementId">The placement id; reusing it replaces the placement in place.</param>
    /// <param name="srcX">Left edge of the source crop, in image pixels.</param>
    /// <param name="srcY">Top edge of the source crop, in image pixels.</param>
    /// <param name="srcW">Width of the source crop, in image pixels.</param>
    /// <param name="srcH">Height of the source crop, in image pixels.</param>
    /// <param name="destCols">Number of columns to display the crop in.</param>
    /// <param name="destRows">Number of rows to display the crop in.</param>
    /// <returns>The complete Kitty APC placement escape sequence.</returns>
    public static string EncodePut (int imageId, int placementId, int srcX, int srcY, int srcW, int srcH, int destCols, int destRows) =>
        $"{APC_START}a=p,i={imageId},p={placementId},x={srcX},y={srcY},w={srcW},h={srcH},c={destCols},r={destRows},z=-1,C=1,q=2{APC_END}";

    /// <summary>
    ///     Encodes a Kitty sequence that deletes a single placement (by image id + placement id), leaving the
    ///     transmitted image data and other placements intact.
    /// </summary>
    /// <param name="imageId">The image id (the Kitty <c>i</c> key).</param>
    /// <param name="placementId">The placement id (the Kitty <c>p</c> key) to delete.</param>
    /// <returns>The complete Kitty APC delete-placement escape sequence.</returns>
    public static string EncodeDeletePlacement (int imageId, int placementId) =>
        $"{APC_START}a=d,d=i,i={imageId},p={placementId}{APC_END}";

    /// <summary>
    ///     Derives a stable, positive, non-zero Kitty image id from the given string identifier.
    /// </summary>
    /// <remarks>
    ///     The same <paramref name="id"/> always maps to the same image id, so a view's placement can be
    ///     consistently replaced or deleted across renders.
    /// </remarks>
    /// <param name="id">The string identifier (e.g. a <see cref="Terminal.Gui.Drivers.RasterImageCommand.Id"/>).</param>
    /// <returns>A positive, non-zero image id suitable for the Kitty <c>i</c> key.</returns>
    public static int GetImageId (string id)
    {
        ArgumentException.ThrowIfNullOrEmpty (id);

        // FNV-1a 32-bit hash, clamped to a positive, non-zero value.
        var hash = 2166136261u;

        foreach (char c in id)
        {
            hash = (hash ^ c) * 16777619u;
        }

        var result = (int)(hash & 0x7FFFFFFF);

        return result == 0 ? 1 : result;
    }

    private static byte [] PixelsToRgba (Color [,] pixels, int width, int height)
    {
        byte [] rgba = new byte [width * height * 4];
        int i = 0;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                Color c = pixels [x, y];
                rgba [i++] = c.R;
                rgba [i++] = c.G;
                rgba [i++] = c.B;
                rgba [i++] = c.A;
            }
        }

        return rgba;
    }

    private static string BuildApcSequence (string base64, int pixelWidth, int pixelHeight, int destCols, int destRows, int? imageId)
    {
        var sb = new StringBuilder ();
        int totalLength = base64.Length;
        int offset = 0;

        // First chunk carries the image metadata.
        string firstChunk = offset + MaxChunkSize < totalLength
                                ? base64.Substring (offset, MaxChunkSize)
                                : base64.Substring (offset);
        bool isLastChunk = offset + firstChunk.Length >= totalLength;

        // i=<id> tags the placement with a stable image id so a prior placement can be deleted by
        // id (see EncodeDeletePlacements) when the image is resized or moved. p=<PlacementId> gives the
        // placement a stable id too: a later a=T with the same (image id, placement id) REPLACES the
        // placement in place — the previous pixels stay on screen until the new ones arrive — so an
        // unchanged-geometry repaint (pan, zoom-in) never blanks. Without it, re-placing would have to
        // delete first (image vanishes, then the full image is slowly re-sent = a visible flash).
        string idField = imageId.HasValue ? $"i={imageId.Value},p={PlacementId}," : string.Empty;

        // C=1 suppresses cursor movement after the image is displayed. Without it the terminal
        // advances the cursor past the bottom-right of the image, and an image near the bottom of
        // the screen scrolls the whole display up one row on every repaint (animated images march
        // up the screen instead of repainting in place).
        sb.Append (APC_START);
        sb.Append ($"a=T,f=32,{idField}s={pixelWidth},v={pixelHeight},c={destCols},r={destRows},z=-1,C=1,q=2,m={( isLastChunk ? 0 : 1 )}");
        sb.Append (';');
        sb.Append (firstChunk);
        sb.Append (APC_END);
        offset += firstChunk.Length;

        // Continuation chunks carry only data.
        while (offset < totalLength)
        {
            string chunk = offset + MaxChunkSize < totalLength
                               ? base64.Substring (offset, MaxChunkSize)
                               : base64.Substring (offset);
            bool last = offset + chunk.Length >= totalLength;

            sb.Append (APC_START);
            sb.Append ($"m={( last ? 0 : 1 )}");
            sb.Append (';');
            sb.Append (chunk);
            sb.Append (APC_END);
            offset += chunk.Length;
        }

        return sb.ToString ();
    }
}
