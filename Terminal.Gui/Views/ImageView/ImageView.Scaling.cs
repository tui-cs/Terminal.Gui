namespace Terminal.Gui.Views;

public partial class ImageView
{
    private static Color [,] ScaleVisibleImage (Color [,] source, RectangleF visibleSource, Size targetSize, bool allowUpscale, bool preserveAspectRatio)
    {
        Size size = ComputeScaledSize (visibleSource, targetSize, allowUpscale, preserveAspectRatio);
        Color [,] scaledImage = new Color [size.Width, size.Height];
        ScaleVisibleNearestNeighbor (source, scaledImage, visibleSource);

        return scaledImage;
    }

    // The pixel size a visible source region scales to for the given target — the aspect-ratio-preserving fit
    // used by ScaleVisibleImage. Exposed so the Kitty source-crop path can size the destination without
    // actually scaling/encoding any pixels (it lets the terminal do the scaling from a once-transmitted image).
    private static Size ComputeScaledSize (RectangleF visibleSource, Size targetSize, bool allowUpscale, bool preserveAspectRatio)
    {
        if (!preserveAspectRatio)
        {
            return targetSize;
        }

        double widthScale = targetSize.Width / (double)visibleSource.Width;
        double heightScale = targetSize.Height / (double)visibleSource.Height;
        double scale = Math.Min (widthScale, heightScale);

        if (!allowUpscale)
        {
            scale = Math.Min (scale, 1d);
        }

        return new Size (Math.Max (1, (int)(visibleSource.Width * scale)), Math.Max (1, (int)(visibleSource.Height * scale)));
    }

    private static void ScaleVisibleNearestNeighbor (Color [,] source, Color [,] destination, RectangleF visibleSource)
    {
        int srcWidth = source.GetLength (0);
        int srcHeight = source.GetLength (1);
        int newWidth = destination.GetLength (0);
        int newHeight = destination.GetLength (1);

        for (var y = 0; y < newHeight; y++)
        {
            int srcY = Math.Clamp ((int)(visibleSource.Y + y * (double)visibleSource.Height / newHeight), 0, srcHeight - 1);

            for (var x = 0; x < newWidth; x++)
            {
                int srcX = Math.Clamp ((int)(visibleSource.X + x * (double)visibleSource.Width / newWidth), 0, srcWidth - 1);
                destination [x, y] = source [srcX, srcY];
            }
        }
    }

    /// <summary>
    ///     Scales a <c>Color[,]</c> pixel array into a destination array using nearest-neighbor interpolation.
    /// </summary>
    /// <param name="source">The source pixel array indexed as [x, y].</param>
    /// <param name="destination">The destination pixel array indexed as [x, y].</param>
    public static void ScaleNearestNeighbor (Color [,] source, Color [,] destination)
    {
        int srcWidth = source.GetLength (0);
        int srcHeight = source.GetLength (1);
        int newWidth = destination.GetLength (0);
        int newHeight = destination.GetLength (1);

        for (var y = 0; y < newHeight; y++)
        {
            int srcY = Math.Min (y * srcHeight / newHeight, srcHeight - 1);

            for (var x = 0; x < newWidth; x++)
            {
                int srcX = Math.Min (x * srcWidth / newWidth, srcWidth - 1);
                destination [x, y] = source [srcX, srcY];
            }
        }
    }
}
