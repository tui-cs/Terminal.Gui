namespace Terminal.Gui.Views;

/// <summary>
///     Displays an image represented as a 2D array of <see cref="Color"/> pixels.
///     Supports two rendering modes: cell-based (one colored space per pixel, works everywhere)
///     and raster-based (for terminals that support either the Sixel or Kitty graphics protocols).
/// </summary>
/// <remarks>
///     <para>
///         The image data is provided via the <see cref="Image"/> property as a <c>Color[,]</c> array
///         where the first dimension is width (x) and the second is height (y). Image loading and
///         decoding from file formats (PNG, JPEG, etc.) is the caller's responsibility — this view
///         has no dependency on any image library.
///     </para>
///     <para>
///         When <see cref="UseRasterGraphics"/> is <see langword="true"/>, the view renders through
///         the best raster protocol available to the driver: Kitty graphics first, then Sixel.
///         Raster data is only re-sent to the terminal when <see cref="View.NeedsDraw"/> is true,
///         avoiding redundant rendering of unchanged images.
///     </para>
///     <para>
///         When no raster protocol is available, the view falls back to cell-based rendering where
///         each terminal cell is colored with the background color of the corresponding pixel.
///     </para>
/// </remarks>
public partial class ImageView : View, IDesignable
{
    private const double MAX_ZOOM_LEVEL = 64d;
    private const double FIT_ZOOM_LEVEL = 1d;
    private const double ZOOM_FACTOR = 1.25d;
    private const int DEFAULT_MAX_SIXEL_PALETTE_COLORS = 64;

    private readonly SpinnerView _renderingOverlay = new ()
    {
        X = Pos.Center (),
        Y = Pos.Center (),
        CanFocus = false,
        Style = new SpinnerStyle.Aesthetic2 (),
        Visible = false
    };

    /// <summary>
    ///     Gets or sets the default key bindings for <see cref="ImageView"/>.
    /// </summary>
    public new static Dictionary<Command, PlatformKeyBinding>? DefaultKeyBindings { get; set; } = new ()
    {
        [Command.ScrollLeft] = Bind.All (Key.CursorLeft),
        [Command.ScrollRight] = Bind.All (Key.CursorRight),
        [Command.ScrollUp] = Bind.All (Key.CursorUp),
        [Command.ScrollDown] = Bind.All (Key.CursorDown),
        [Command.Home] = Bind.All (Key.Home, Key.D0),
        [Command.ZoomIn] = Bind.All (new Key ('+'),
                                      new Key ('+').WithShift,
                                      new Key ('='),
                                      new Key ('=').WithShift),
        [Command.ZoomOut] = Bind.All (new Key ('-'))
    };

    /// <summary>
    ///     Gets or sets the default mouse bindings for <see cref="ImageView"/>.
    /// </summary>
    public new static Dictionary<Command, PlatformMouseBinding>? DefaultMouseBindings { get; set; } = new ()
    {
        [Command.ZoomIn] = BindMouse.All (MouseFlags.WheeledUp),
        [Command.ZoomOut] = BindMouse.All (MouseFlags.WheeledDown),
        [Command.Center] = BindMouse.All (MouseFlags.LeftButtonDoubleClicked)
    };

    /// <summary>Initializes a new instance of the <see cref="ImageView"/> class.</summary>
    public ImageView ()
    {
        CanFocus = true;
        MousePositionTracking = true;

        AddCommand (Command.ScrollLeft, () => ScrollFromCommand (-1, 0));
        AddCommand (Command.ScrollRight, () => ScrollFromCommand (1, 0));
        AddCommand (Command.ScrollUp, () => ScrollFromCommand (0, -1));
        AddCommand (Command.ScrollDown, () => ScrollFromCommand (0, 1));
        AddCommand (Command.Home, () => ResetView ());
        AddCommand (Command.ZoomIn, context => ZoomFromCommand (context, _zoomLevel * ZOOM_FACTOR));
        AddCommand (Command.ZoomOut, context => ZoomFromCommand (context, _zoomLevel / ZOOM_FACTOR));
        AddCommand (Command.PageUp, context => ZoomFromCommand (context, _zoomLevel * ZOOM_FACTOR));
        AddCommand (Command.PageDown, context => ZoomFromCommand (context, _zoomLevel / ZOOM_FACTOR));
        AddCommand (Command.Center, CenterFromCommand);

        ApplyKeyBindings (DefaultKeyBindings, View.DefaultKeyBindings);
        ApplyMouseBindings (DefaultMouseBindings, View.DefaultMouseBindings);
        ReplacePanAndZoomBindings ();
        Add (_renderingOverlay);
    }

    private void ReplacePanAndZoomBindings ()
    {
        KeyBindings.ReplaceCommands (Key.CursorLeft, Command.ScrollLeft);
        KeyBindings.ReplaceCommands (Key.CursorRight, Command.ScrollRight);
        KeyBindings.ReplaceCommands (Key.CursorUp, Command.ScrollUp);
        KeyBindings.ReplaceCommands (Key.CursorDown, Command.ScrollDown);
        KeyBindings.ReplaceCommands (Key.Home, Command.Home);

        MouseBindings.ReplaceCommands (MouseFlags.WheeledUp, Command.ZoomIn);
        MouseBindings.ReplaceCommands (MouseFlags.WheeledDown, Command.ZoomOut);
        MouseBindings.Remove (MouseFlags.LeftButtonReleased);
        MouseBindings.ReplaceCommands (MouseFlags.LeftButtonClicked, Command.Activate);
        MouseBindings.ReplaceCommands (MouseFlags.LeftButtonDoubleClicked, Command.Center);
    }

    private Color [,]? _image;

    private int _imageVersion;

    private string RasterImageId => $"ImageView_{GetHashCode ()}";

    /// <summary>
    ///     Gets or sets the pixel data to display. The array is indexed as [x, y] where
    ///     the first dimension is width and the second is height.
    /// </summary>
    /// <remarks>
    ///     Setting this property marks the view as needing redraw. The image will be
    ///     scaled to fit the current <see cref="View.Viewport"/> while maintaining
    ///     aspect ratio using nearest-neighbor interpolation.
    /// </remarks>
    public Color [,]? Image
    {
        get => _image;
        set
        {
            _image = value;
            _imageVersion++;
            ClampCenter ();

            if (_image is null)
            {
                InvalidateScaledImage (true);
                App?.Driver?.GetOutputBuffer ().RemoveRasterImage (RasterImageId);

                return;
            }

            InvalidateScaledImage ();
        }
    }

    /// <summary>
    ///     Gets or sets whether to prefer raster-graphics rendering (Kitty or Sixel protocol)
    ///     when the terminal supports it. Default is <see langword="true"/>.
    /// </summary>
    /// <remarks>
    ///     When <see langword="true"/> the view selects the best available protocol:
    ///     Kitty graphics (if the driver reports <see cref="IDriver.KittyGraphicsSupport"/>),
    ///     then Sixel (if the driver reports <see cref="IDriver.SixelSupport"/>),
    ///     then cell-based rendering as a fallback.
    ///     Set to <see langword="false"/> to always use cell-based rendering.
    /// </remarks>
    public bool UseRasterGraphics { get; set; } = true;

    /// <summary>
    ///     Gets whether to prefer raster rendering when the terminal supports it.
    ///     Default is <see langword="true"/>.
    /// </summary>
    /// <remarks>
    ///     This obsolete compatibility property delegates to <see cref="UseRasterGraphics"/>.
    ///     When <see langword="true"/>, Kitty graphics is preferred and Sixel is used as the
    ///     fallback. When <see langword="false"/>, cell-based rendering is always used.
    /// </remarks>
    [Obsolete ("Use UseRasterGraphics instead. UseSixel will be removed in a future version.")]
    public bool UseSixel
    {
        get => UseRasterGraphics;
        set => UseRasterGraphics = value;
    }

    /// <summary>
    ///     Gets or sets whether ImageView scales image renders on a background thread.
    /// </summary>
    /// <remarks>
    ///     The default is <see langword="false"/>. When enabled, ImageView keeps the last completed render visible while
    ///     a newer render is being prepared and shows a centered spinner until the background render is
    ///     ready.
    /// </remarks>
    public bool UseBackgroundRendering
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;

            if (field && _image is { } && !IsCurrentRenderReady ())
            {
                SetRenderingOverlayVisible (true);
            }
            else if (!field)
            {
                SetRenderingOverlayVisible (false);
            }

            InvalidateScaledImage ();
        }
    }

    /// <summary>
    ///     Gets or sets the maximum number of colors to use when encoding ImageView sixel output.
    /// </summary>
    /// <remarks>
    ///     The default is 64 colors to keep interactive ImageView redraws responsive. The effective
    ///     encoder palette is also limited by <see cref="SixelSupportResult.MaxPaletteColors"/> and by
    ///     the configured <see cref="SixelEncoder.Quantizer"/> <c>MaxColors</c>.
    /// </remarks>
    public int MaxSixelPaletteColors
    {
        get;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException (nameof (value), @"Maximum sixel palette colors must be greater than zero.");
            }

            if (field == value)
            {
                return;
            }

            field = value;
            InvalidateScaledImage ();
        }
    } = DEFAULT_MAX_SIXEL_PALETTE_COLORS;

    /// <summary>
    ///     Gets or sets whether sixel rendering may upscale the visible image region above its source pixel size.
    /// </summary>
    /// <remarks>
    ///     The default is <see langword="true"/> so the image fills the Viewport at <see cref="ZoomLevel"/> 1.
    ///     Set to <see langword="false"/> to avoid encoding more pixels than the source image provides during fit-to-view
    ///     rendering.
    /// </remarks>
    public bool AllowSixelUpscaling
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            InvalidateScaledImage ();
        }
    } = true;

    private double _zoomLevel = FIT_ZOOM_LEVEL;

    /// <summary>
    ///     Gets or sets the zoom level. A value of <c>1</c> fits the image in the viewport.
    /// </summary>
    public double ZoomLevel { get => _zoomLevel; set => SetZoomLevel (value, null); }

    /// <summary>
    ///     Raised when <see cref="ZoomLevel"/> changes (including a reset to fit). A host that renders its
    ///     own source image (e.g. rasterizes a document) can subscribe to re-render it at the new zoom for
    ///     crispness — the view itself only ever scales the pixels it was given.
    /// </summary>
    public event EventHandler? ZoomLevelChanged;

    private SixelEncoder? _sixelEncoder;

    private bool _usesDefaultSixelEncoder;

    /// <summary>
    ///     Gets or sets the <see cref="Drawing.SixelEncoder"/> used to encode images as sixel data.
    ///     When <see langword="null"/>, a default encoder is created lazily on first use.
    /// </summary>
    /// <remarks>
    ///     Set this to provide a custom encoder with specific quantizer settings, palette building
    ///     algorithms, or color distance algorithms. The encoder's
    ///     <see cref="Drawing.SixelEncoder.Quantizer"/> <c>MaxColors</c> will be clamped during
    ///     rendering. The default encoder is capped by <see cref="MaxSixelPaletteColors"/> and the
    ///     terminal's <see cref="SixelSupportResult.MaxPaletteColors"/>. Custom encoders are capped
    ///     only by the terminal's <see cref="SixelSupportResult.MaxPaletteColors"/>.
    /// </remarks>
    public SixelEncoder? SixelEncoder
    {
        get => _sixelEncoder;
        set
        {
            if (ReferenceEquals (_sixelEncoder, value))
            {
                return;
            }

            _sixelEncoder = value;
            _usesDefaultSixelEncoder = false;
            InvalidateScaledImage ();
        }
    }

    /// <summary>
    ///     Gets whether the current rendering mode is using a raster graphics protocol (Kitty or Sixel).
    /// </summary>
    public bool IsUsingRasterGraphics => UseRasterGraphics
                                         && (IsKittyGraphicsActive ()
                                             || App?.Driver?.SixelSupport is { IsSupported: true });

    /// <summary>
    ///     Gets whether the current rendering mode is using sixel.
    /// </summary>
    [Obsolete ("Use IsUsingRasterGraphics instead. IsUsingSixel will be removed in a future version.")]
    public bool IsUsingSixel => UseRasterGraphics && !IsKittyGraphicsActive () && App?.Driver?.SixelSupport is { IsSupported: true };

    /// <summary>
    ///     Converts the Viewport to screen coordinates in pixels.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method accounts for the terminal's cell resolution and the viewport's
    ///         size, returning the exact pixel dimensions and position required for
    ///         fully cover the viewport.
    ///     </para>
    /// </remarks>
    /// <returns>The screen coordinates of the Viewport in pixels.</returns>
    public Rectangle ViewportToScreenInPixels ()
    {
        Size resolution = GetActiveResolution () ?? throw new InvalidOperationException (@"No raster graphics support available.");

        int pixelsPerCellX = resolution.Width;
        int pixelsPerCellY = resolution.Height;
        Rectangle boundsRect = ViewportToScreen ();

        int targetWidthInPixels = boundsRect.Width * pixelsPerCellX;
        int targetHeightInPixels = IsKittyGraphicsActive ()
                                       ? boundsRect.Height * pixelsPerCellY
                                       : SixelEncoder?.GetHeightInPixels (boundsRect.Height, pixelsPerCellY) ?? boundsRect.Height * pixelsPerCellY;

        return new Rectangle (boundsRect.X * pixelsPerCellX, boundsRect.Y * pixelsPerCellY, targetWidthInPixels, targetHeightInPixels);
    }

    /// <summary>
    ///     Returns the resolution (pixels per cell) for the active raster graphics protocol,
    ///     preferring Kitty over Sixel.  Returns <see langword="null"/> when neither is available.
    /// </summary>
    private Size? GetActiveResolution ()
    {
        if (IsKittyGraphicsActive () && App?.Driver?.KittyGraphicsSupport is { IsSupported: true } kitty)
        {
            return kitty.Resolution;
        }

        if (App?.Driver?.SixelSupport is { IsSupported: true } sixel)
        {
            return sixel.Resolution;
        }

        return null;
    }

    private bool IsKittyGraphicsActive () =>
        App?.Driver is { KittyGraphicsSupport.IsSupported: true } driver && driver.GetOutput ().UseKittyGraphics;

    /// <summary>
    ///     Returns the size in cell terms of the given image resized to fit in the viewport.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method accounts for the terminal's cell resolution and the viewport's
    ///         size, returning the exact pixel dimensions and position required for
    ///         fully cover the viewport without changing the images aspect ratio.
    ///     </para>
    /// </remarks>
    /// <param name="imageSizeInPixels">The size of the image in pixels.</param>
    /// <returns>The largest possible size of the image in cell terms that fits within the viewport.</returns>
    public Size FitImageInViewportCells (Size imageSizeInPixels)
    {
        if (imageSizeInPixels.Width == 0 || imageSizeInPixels.Height == 0)
        {
            return Size.Empty;
        }

        // Account for the terminal cell aspect ratio
        Size? activeResolution = GetActiveResolution ();
        double cellAspectRatio = activeResolution is { } res ? (double)res.Height / res.Width : 2.0;
        Size imageSize = imageSizeInPixels with { Height = (int)(imageSizeInPixels.Height / cellAspectRatio) };

        // Calculate aspect-ratio-preserving size
        double widthScale = (double)Viewport.Width / imageSize.Width;
        double heightScale = (double)Viewport.Height / imageSize.Height;
        double scale = Math.Min (widthScale, heightScale);

        int newWidth = Math.Max (1, (int)(imageSize.Width * scale));
        int newHeight = Math.Max (1, (int)(imageSize.Height * scale));

        return new Size (newWidth, newHeight);
    }

    /// <summary>
    ///     Scales an image to fit within the current Viewport while maintaining aspect ratio.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method calculates the largest possible size for the given image that will fit
    ///         within the current <see cref="View.Viewport"/> while maintaining its aspect ratio.
    ///     </para>
    ///     <para>
    ///         The calculation is based on the terminal's cell resolution and the <see cref="View.Viewport"/>
    ///         size, returning the exact pixel dimensions and position required for the scaled image.
    ///     </para>
    /// </remarks>
    /// <param name="imageSize">The original size of the image to scale.</param>
    /// <returns>The scaled size of the image that fits within the <see cref="View.Viewport"/>.</returns>
    public Size FitImageInViewportInPixels (Size imageSize)
    {
        Rectangle viewportInPixels = ViewportToScreenInPixels ();

        if (imageSize.Width == 0 || imageSize.Height == 0)
        {
            return Size.Empty;
        }

        // Calculate aspect-ratio-preserving size
        double widthScale = (double)viewportInPixels.Width / imageSize.Width;
        double heightScale = (double)viewportInPixels.Height / imageSize.Height;
        double scale = Math.Min (widthScale, heightScale);

        int newWidth = Math.Max (1, (int)(imageSize.Width * scale));
        int newHeight = Math.Max (1, (int)(imageSize.Height * scale));

        return new Size (newWidth, newHeight);
    }
}
