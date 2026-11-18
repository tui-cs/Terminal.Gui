namespace Terminal.Gui.Views;

public partial class ImageView
{
    private double _centerX = 0.5d;
    private double _centerY = 0.5d;

    /// <summary>
    ///     Centers the image on the specified viewport-relative point.
    /// </summary>
    /// <param name="position">The viewport-relative point.</param>
    /// <returns><see langword="true"/> if the view was centered; otherwise, <see langword="false"/>.</returns>
    protected virtual bool CenterOnViewportPoint (Point position)
    {
        if (!TryMapViewportPointToSourceCenter (position, out double centerX, out double centerY))
        {
            return false;
        }

        return SetCenter (centerX, centerY);
    }

    private void ClampCenter ()
    {
        if (_image is null || _zoomLevel <= FIT_ZOOM_LEVEL)
        {
            _centerX = 0.5d;
            _centerY = 0.5d;

            return;
        }

        double halfVisible = 0.5d / _zoomLevel;
        _centerX = Math.Clamp (_centerX, halfVisible, 1d - halfVisible);
        _centerY = Math.Clamp (_centerY, halfVisible, 1d - halfVisible);
    }

    private RectangleF GetVisibleSourceRectangle ()
    {
        if (_image is null)
        {
            return RectangleF.Empty;
        }

        int srcWidth = _image.GetLength (0);
        int srcHeight = _image.GetLength (1);
        double effectiveZoom = Math.Max (FIT_ZOOM_LEVEL, _zoomLevel);
        double visibleWidth = srcWidth / effectiveZoom;
        double visibleHeight = srcHeight / effectiveZoom;
        double x = _centerX * srcWidth - visibleWidth / 2d;
        double y = _centerY * srcHeight - visibleHeight / 2d;
        x = Math.Clamp (x, 0d, Math.Max (0d, srcWidth - visibleWidth));
        y = Math.Clamp (y, 0d, Math.Max (0d, srcHeight - visibleHeight));

        return new RectangleF ((float)x, (float)y, (float)visibleWidth, (float)visibleHeight);
    }

    private Size ApplyZoomOutToTargetSize (Size targetSize)
    {
        if (_zoomLevel >= FIT_ZOOM_LEVEL)
        {
            return targetSize;
        }

        int width = Math.Max (1, (int)Math.Round (targetSize.Width * _zoomLevel));
        int height = Math.Max (1, (int)Math.Round (targetSize.Height * _zoomLevel));

        return new Size (width, height);
    }

    private static Point GetCenteredRenderOffset (Size renderSize, Size viewportSize)
    {
        int offsetX = Math.Max (0, (viewportSize.Width - renderSize.Width) / 2);
        int offsetY = Math.Max (0, (viewportSize.Height - renderSize.Height) / 2);

        return new Point (offsetX, offsetY);
    }

    private double GetMinimumZoomLevel ()
    {
        Size targetSize = GetBaseRenderTargetSize ();
        int maxDimension = Math.Max (targetSize.Width, targetSize.Height);

        if (maxDimension <= 0)
        {
            return FIT_ZOOM_LEVEL;
        }

        return 1d / maxDimension;
    }

    private Size GetBaseRenderTargetSize ()
    {
        if (IsUsingRasterGraphics && App?.Driver is { })
        {
            return ViewportToScreenInPixels ().Size;
        }

        return Viewport.Size;
    }

    private static Size GetSixelCellSize (Size pixelSize, Size resolution)
    {
        int cellWidth = Math.Max (1, (int)Math.Ceiling ((double)pixelSize.Width / Math.Max (1, resolution.Width)));
        int cellHeight = Math.Max (1, (int)Math.Ceiling ((double)pixelSize.Height / Math.Max (1, resolution.Height)));

        return new Size (cellWidth, cellHeight);
    }
}
