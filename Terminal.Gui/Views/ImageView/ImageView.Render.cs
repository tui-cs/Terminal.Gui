namespace Terminal.Gui.Views;

public partial class ImageView
{
    private string? _encodedSixel;
    private string? _encodedKitty;
    private Color [,]? _scaledImage;
    private Size? _scaledImageCellSize;
    private RenderKey? _scaledImageRenderKey;

    private readonly Lock _renderLock = new ();

    private RenderKey? _backgroundRenderKey;
    private bool _backgroundRenderRunning;
    private bool _disposed;
    private RenderRequest? _queuedRenderRequest;

    private void InvalidateScaledImage (bool clearCurrentRender = false)
    {
        if (clearCurrentRender || !UseBackgroundRendering || _scaledImage is null)
        {
            _scaledImage = null;
            _scaledImageCellSize = null;
            _encodedSixel = null;
            _encodedKitty = null;
            _attributeCache.Clear ();
        }

        _scaledImageRenderKey = null;
        SetRenderingOverlayVisible (UseBackgroundRendering && _image is { });
        SetNeedsDraw ();
    }

    private bool IsRenderCacheCurrent (RenderKey key) => _scaledImage is { } && _scaledImageCellSize is { } && _scaledImageRenderKey == key;

    private bool IsCurrentRenderReady ()
    {
        if (_image is null)
        {
            return true;
        }

        RenderRequest? request = CreateRenderRequest (IsUsingRasterGraphics);

        return request is null || IsRenderCacheCurrent (request.Key);
    }

    private void EnsureScaledImage (RenderRequest request)
    {
        if (IsRenderCacheCurrent (request.Key))
        {
            SetRenderingOverlayVisible (false);

            return;
        }

        if (UseBackgroundRendering && App?.Initialized == true)
        {
            ScheduleBackgroundRender (request);

            return;
        }

        ApplyRenderResult (RenderScaledImage (request));
        SetRenderingOverlayVisible (false);
    }

    private RenderRequest? CreateRenderRequest (bool useRasterGraphics)
    {
        if (_image is null)
        {
            return null;
        }

        RectangleF visibleSource = GetVisibleSourceRectangle ();

        if (visibleSource.Width <= 0 || visibleSource.Height <= 0)
        {
            return null;
        }

        Size? resolution = null;
        SixelEncoder? sixelEncoder = null;
        int? maxColors = null;
        Size targetSize;
        var allowUpscale = true;
        var preserveAspectRatio = false;
        var useKitty = false;

        if (useRasterGraphics)
        {
            if (!IsUsingRasterGraphics)
            {
                return null;
            }

            resolution = GetActiveResolution ();

            if (resolution is null)
            {
                return null;
            }

            useKitty = IsKittyGraphicsActive ();

            if (!useKitty)
            {
                if (App?.Driver?.SixelSupport is not { } support)
                {
                    return null;
                }

                sixelEncoder = PrepareSixelEncoder (support);
                maxColors = sixelEncoder.Quantizer.MaxColors;
            }

            Rectangle targetRect = ViewportToScreenInPixels ();
            targetSize = targetRect.Size;
            allowUpscale = AllowSixelUpscaling || _zoomLevel > FIT_ZOOM_LEVEL;
            preserveAspectRatio = true;
        }
        else
        {
            targetSize = new Size (Viewport.Width, Viewport.Height);
        }

        targetSize = ApplyZoomOutToTargetSize (targetSize);

        if (targetSize.Width <= 0 || targetSize.Height <= 0)
        {
            return null;
        }

        RenderKey key = new (_imageVersion,
                             useRasterGraphics,
                             useKitty,
                             targetSize,
                             resolution,
                             maxColors,
                             _centerX,
                             _centerY,
                             _zoomLevel,
                             allowUpscale,
                             preserveAspectRatio);

        return new RenderRequest (key,
                                  _image,
                                  visibleSource,
                                  targetSize,
                                  resolution,
                                  sixelEncoder is { } ? CreateBackgroundEncoder (sixelEncoder) : null,
                                  KittyGraphicsEncoder.GetImageId (RasterImageId));
    }

    private SixelEncoder PrepareSixelEncoder (SixelSupportResult support)
    {
        SixelEncoder? encoder = SixelEncoder;

        if (encoder is null)
        {
            encoder = new SixelEncoder ();
            _sixelEncoder = encoder;
            _usesDefaultSixelEncoder = true;
        }

        int maxColors = _usesDefaultSixelEncoder
                            ? Math.Min (MaxSixelPaletteColors, support.MaxPaletteColors)
                            : Math.Min (encoder.Quantizer.MaxColors, support.MaxPaletteColors);
        encoder.Quantizer.MaxColors = maxColors;

        return encoder;
    }

    private void ApplyRenderResult (RenderResult result)
    {
        _scaledImage = result.ScaledImage;
        _scaledImageCellSize = result.CellSize;
        _encodedSixel = result.EncodedSixel;
        _encodedKitty = result.EncodedKitty;
        _scaledImageRenderKey = result.Key;
        _attributeCache.Clear ();
    }

    private static RenderResult RenderScaledImage (RenderRequest request)
    {
        Color [,] scaledImage = ScaleVisibleImage (request.Source,
                                                   request.VisibleSource,
                                                   request.TargetSize,
                                                   request.Key.AllowUpscale,
                                                   request.Key.PreserveAspectRatio);

        Size cellSize = request.Key.UseRasterGraphics && request.Resolution is { } resolution
                            ? GetSixelCellSize (new Size (scaledImage.GetLength (0), scaledImage.GetLength (1)), resolution)
                            : new Size (scaledImage.GetLength (0), scaledImage.GetLength (1));

        string? encodedSixel = null;
        string? encodedKitty = null;

        if (!request.Key.UseRasterGraphics)
        {
            return new RenderResult (request.Key, scaledImage, cellSize, encodedSixel, encodedKitty);
        }

        if (request.Key.UseKitty)
        {
            encodedKitty = new KittyGraphicsEncoder ().EncodeKitty (scaledImage, cellSize.Width, cellSize.Height, request.KittyImageId);
        }
        else
        {
            encodedSixel = request.Encoder?.EncodeSixel (scaledImage);
        }

        return new RenderResult (request.Key, scaledImage, cellSize, encodedSixel, encodedKitty);
    }

    private static SixelEncoder CreateBackgroundEncoder (SixelEncoder encoder) =>
        new ()
        {
            AvoidBottomScroll = encoder.AvoidBottomScroll,
            Quantizer = new ColorQuantizer
            {
                MaxColors = encoder.Quantizer.MaxColors,
                DistanceAlgorithm = encoder.Quantizer.DistanceAlgorithm,
                PaletteBuildingAlgorithm = encoder.Quantizer.PaletteBuildingAlgorithm
            }
        };

    private void ScheduleBackgroundRender (RenderRequest request)
    {
        if (_disposed)
        {
            return;
        }

        SetRenderingOverlayVisible (true);

        RenderRequest? requestToStart;

        lock (_renderLock)
        {
            if (_backgroundRenderRunning)
            {
                if (_backgroundRenderKey == request.Key || _queuedRenderRequest?.Key == request.Key)
                {
                    return;
                }

                _queuedRenderRequest = request;

                return;
            }

            _backgroundRenderRunning = true;
            requestToStart = request;
        }

        StartBackgroundRender (requestToStart);
    }

    private void StartBackgroundRender (RenderRequest request)
    {
        lock (_renderLock)
        {
            _backgroundRenderKey = request.Key;
        }

        Task<RenderResult> task = Task.Run (() => RenderScaledImage (request));
        task.ContinueWith (CompleteBackgroundRender, TaskScheduler.Default);
    }

    private void CompleteBackgroundRender (Task<RenderResult> completed)
    {
        AggregateException? exception = completed.Exception;

        if (_disposed)
        {
            _ = exception;

            return;
        }

        IApplication? app = App;

        if (app is null || !app.Initialized)
        {
            _ = exception;

            return;
        }

        try
        {
            if (exception is { })
            {
                app.Invoke (() => FailBackgroundRender (exception.GetBaseException ()));

                return;
            }

            app.Invoke (() => CompleteBackgroundRenderOnMainThread (completed.Result));
        }
        catch (NotInitializedException)
        {
            _ = exception;
        }
        catch (ObjectDisposedException)
        {
            _ = exception;
        }
    }

    private void CompleteBackgroundRenderOnMainThread (RenderResult result)
    {
        if (_disposed)
        {
            return;
        }

        RenderRequest? currentRequest = CreateRenderRequest (result.Key.UseRasterGraphics);

        if (UseBackgroundRendering && currentRequest?.Key == result.Key)
        {
            ApplyRenderResult (result);
        }

        StartNextQueuedRenderOrFinish (result.Key);
        SetNeedsDraw ();
    }

    private void FailBackgroundRender (Exception exception)
    {
        StartNextQueuedRenderOrFinish (_backgroundRenderKey);

        throw new InvalidOperationException ("Background ImageView rendering failed.", exception);
    }

    private void StartNextQueuedRenderOrFinish (RenderKey? completedKey)
    {
        RenderRequest? nextRequest = null;

        lock (_renderLock)
        {
            if (_queuedRenderRequest is { } queuedRequest && queuedRequest.Key != completedKey)
            {
                nextRequest = queuedRequest;
                _queuedRenderRequest = null;
            }
            else
            {
                _queuedRenderRequest = null;
                _backgroundRenderKey = null;
                _backgroundRenderRunning = false;
            }
        }

        if (nextRequest is { })
        {
            StartBackgroundRender (nextRequest);

            return;
        }

        SetRenderingOverlayVisible (false);
    }

    private void SetRenderingOverlayVisible (bool visible)
    {
        if (_renderingOverlay.Visible == visible)
        {
            return;
        }

        _renderingOverlay.AutoSpin = visible;
        _renderingOverlay.Visible = visible;
    }

    private readonly record struct RenderKey (int ImageVersion,
                                              bool UseRasterGraphics,
                                              bool UseKitty,
                                              Size TargetSize,
                                              Size? Resolution,
                                              int? MaxColors,
                                              double CenterX,
                                              double CenterY,
                                              double ZoomLevel,
                                              bool AllowUpscale,
                                              bool PreserveAspectRatio);

    private sealed class RenderRequest (RenderKey key,
                                        Color [,] source,
                                        RectangleF visibleSource,
                                        Size targetSize,
                                        Size? resolution,
                                        SixelEncoder? encoder,
                                        int kittyImageId)
    {
        public RenderKey Key { get; } = key;
        public Color [,] Source { get; } = source;
        public RectangleF VisibleSource { get; } = visibleSource;
        public Size TargetSize { get; } = targetSize;
        public Size? Resolution { get; } = resolution;
        public SixelEncoder? Encoder { get; } = encoder;
        public int KittyImageId { get; } = kittyImageId;
    }

    private readonly record struct RenderResult (RenderKey Key, Color [,] ScaledImage, Size CellSize, string? EncodedSixel, string? EncodedKitty);
}
