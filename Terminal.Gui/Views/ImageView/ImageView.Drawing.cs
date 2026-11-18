namespace Terminal.Gui.Views;

public partial class ImageView
{
    private readonly Dictionary<Color, Attribute> _attributeCache = new ();

    /// <inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        base.OnDrawingContent (context);

        if (_image is null)
        {
            return true;
        }

        if (IsUsingRasterGraphics)
        {
            DrawRasterImage ();

            if (_scaledImageCellSize is not { } cellSize)
            {
                return true;
            }

            Rectangle viewport = ViewportToScreen ();
            Rectangle dirtyRect = viewport with { Width = Math.Min (viewport.Width, cellSize.Width), Height = Math.Min (viewport.Height, cellSize.Height) };
            context?.AddDrawnRectangle (dirtyRect);
        }
        else
        {
            DrawCellBased ();

            if (_scaledImageCellSize is not { } cellSize)
            {
                return true;
            }

            Rectangle viewport = ViewportToScreen ();
            Rectangle dirtyRect = viewport with { Width = Math.Min (viewport.Width, cellSize.Width), Height = Math.Min (viewport.Height, cellSize.Height) };
            context?.AddDrawnRectangle (dirtyRect);
        }

        return true;
    }

    /// <inheritdoc/>
    protected override void OnFrameChanged (in Rectangle frame)
    {
        base.OnFrameChanged (frame);
        InvalidateScaledImage ();
    }

    /// <inheritdoc/>
    internal override void CollectActiveRasterImageIds (HashSet<string> ids)
    {
        if (!Visible)
        {
            return;
        }

        base.CollectActiveRasterImageIds (ids);

        if (IsUsingRasterGraphics && _image is { })
        {
            ids.Add (RasterImageId);
        }
    }

    /// <summary>
    ///     Renders the image using cell-based rendering where each terminal cell
    ///     gets the background color of the corresponding pixel.
    /// </summary>
    private void DrawCellBased ()
    {
        RenderRequest? request = CreateRenderRequest (false);

        if (request is null)
        {
            return;
        }

        EnsureScaledImage (request);

        if (_scaledImage is null || _scaledImageCellSize is null)
        {
            return;
        }

        int drawWidth = Math.Min (Viewport.Width, _scaledImage.GetLength (0));
        int drawHeight = Math.Min (Viewport.Height, _scaledImage.GetLength (1));
        Point offset = _zoomLevel < FIT_ZOOM_LEVEL ? GetCenteredRenderOffset (new Size (drawWidth, drawHeight), Viewport.Size) : Point.Empty;

        for (var y = 0; y < drawHeight; y++)
        {
            for (var x = 0; x < drawWidth; x++)
            {
                Color pixel = _scaledImage [x, y];

                if (!_attributeCache.TryGetValue (pixel, out Attribute attr))
                {
                    attr = new Attribute (new Color (), pixel);
                    _attributeCache.Add (pixel, attr);
                }

                SetAttribute (attr);
                AddRune (x + offset.X, y + offset.Y, (Rune)' ');
            }
        }
    }

    /// <summary>
    ///     Renders the image using a raster graphics protocol (Kitty or Sixel).
    /// </summary>
    private void DrawRasterImage ()
    {
        RenderRequest? request = CreateRenderRequest (true);

        if (request is null || App?.Driver is not { } driver)
        {
            return;
        }

        // Kitty source-crop path: transmit the full source image once and pan/zoom it with tiny placement
        // updates that crop a different region — no per-frame pixel re-transmit (and so no flash) and no
        // CPU scale/encode. The terminal does the scaling from the resident image.
        if (request.Key.UseKitty && _image is { } image)
        {
            Size scaledPixels = ComputeScaledSize (request.VisibleSource, request.TargetSize, request.Key.AllowUpscale, request.Key.PreserveAspectRatio);
            Size cellSize = request.Resolution is { } resolution ? GetSixelCellSize (scaledPixels, resolution) : scaledPixels;

            Rectangle viewport = ViewportToScreen ();
            Size destinationSize = new (Math.Min (viewport.Width, cellSize.Width), Math.Min (viewport.Height, cellSize.Height));
            Point offset = _zoomLevel < FIT_ZOOM_LEVEL ? GetCenteredRenderOffset (destinationSize, viewport.Size) : Point.Empty;
            Rectangle destinationCells = new (viewport.X + offset.X, viewport.Y + offset.Y, destinationSize.Width, destinationSize.Height);

            if (destinationCells.Width <= 0 || destinationCells.Height <= 0)
            {
                return;
            }

            // Keep the cell size current so OnDrawingContent's drawn-region reporting still works without
            // the scaled-image cache (which this path skips).
            _scaledImageCellSize = cellSize;
            SetRenderingOverlayVisible (false);

            // Mark the covered cells transparent so opaque overlays drawn later (e.g. a View's shadow) still
            // paint over the terminal-composited image, exactly as the legacy path below. See issue #5502.
            MarkRasterCellsTransparent (destinationSize, offset);

            RectangleF visible = request.VisibleSource;
            int srcX = Math.Clamp ((int)Math.Floor (visible.X), 0, Math.Max (0, image.GetLength (0) - 1));
            int srcY = Math.Clamp ((int)Math.Floor (visible.Y), 0, Math.Max (0, image.GetLength (1) - 1));
            int srcW = Math.Clamp ((int)Math.Round (visible.Width), 1, image.GetLength (0) - srcX);
            int srcH = Math.Clamp ((int)Math.Round (visible.Height), 1, image.GetLength (1) - srcY);

            RasterImageCommand kittyCommand = new ()
            {
                Id = RasterImageId,
                Pixels = image,
                SourceRect = new Rectangle (srcX, srcY, srcW, srcH),

                // Bumped on every Image set (even to the same instance), so a reused buffer still re-transmits.
                SourceVersion = _imageVersion,
                DestinationCells = destinationCells,
                IsDirty = true
            };

            driver.GetOutputBuffer ().AddRasterImage (kittyCommand);

            return;
        }

        EnsureScaledImage (request);

        if (_scaledImage is null || _scaledImageCellSize is null)
        {
            return;
        }

        Rectangle vp = ViewportToScreen ();
        Size scaledCellSize = _scaledImageCellSize.Value;
        Size sixelDestinationSize = new (Math.Min (vp.Width, scaledCellSize.Width), Math.Min (vp.Height, scaledCellSize.Height));
        Point sixelOffset = _zoomLevel < FIT_ZOOM_LEVEL ? GetCenteredRenderOffset (sixelDestinationSize, vp.Size) : Point.Empty;
        Rectangle sixelDestinationCells = new (vp.X + sixelOffset.X, vp.Y + sixelOffset.Y, sixelDestinationSize.Width, sixelDestinationSize.Height);

        if (sixelDestinationCells.Width <= 0 || sixelDestinationCells.Height <= 0)
        {
            return;
        }

        // Mark the image's covered cells transparent so the output layer treats them as raster-owned
        // and lets the terminal-composited image show through. Opaque overlay cells drawn later (e.g.
        // a View's shadow) keep their background and so still paint over the image. See issue #5502.
        MarkRasterCellsTransparent (sixelDestinationSize, sixelOffset);

        RasterImageCommand command = new ()
        {
            Id = RasterImageId,
            Pixels = _scaledImage,
            EncodedSixel = _encodedSixel,
            EncodedKitty = _encodedKitty,
            DestinationCells = sixelDestinationCells,
            Encoder = SixelEncoder,
            IsDirty = true
        };

        driver.GetOutputBuffer ().AddRasterImage (command);
    }

    // Marks the cells the raster image covers transparent (Color.None) so the terminal-composited image shows
    // through, while opaque overlay cells drawn later (e.g. a View's shadow) keep their background and still
    // paint over the image. See issue #5502.
    private void MarkRasterCellsTransparent (Size destinationSize, Point offset)
    {
        SetAttribute (new Attribute (Color.None, Color.None));

        for (var row = 0; row < destinationSize.Height; row++)
        {
            for (var col = 0; col < destinationSize.Width; col++)
            {
                AddRune (offset.X + col, offset.Y + row, (Rune)' ');
            }
        }
    }

    private bool? CenterFromCommand (ICommandContext? context) =>
        context?.Binding is MouseBinding { MouseEvent.Position: { } position } && CenterOnViewportPoint (position);
}
