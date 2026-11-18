#nullable enable

namespace UICatalog.Scenarios;

internal sealed class MandelbrotImageView : ImageView
{
    internal const int DefaultColumns = 30;
    internal const int DefaultRows = 20;
    internal const double MinimumSpan = double.Epsilon;
    internal const int MinimumIterations = 8;

    private const double DEFAULT_CENTER_X = -0.5;
    private const double DEFAULT_CENTER_Y = 0;
    private const double DEFAULT_SPAN = 3;
    private const int DEFAULT_ITERATIONS = 80;
    private const double PAN_FACTOR = 0.1;
    private const double ZOOM_IN_FACTOR = 0.8;
    private const double ZOOM_OUT_FACTOR = 1.25;

    private Point? _lastDragPosition;

    private double _centerX = DEFAULT_CENTER_X;
    public double CenterX => _centerX;

    private double _centerY = DEFAULT_CENTER_Y;
    public double CenterY => _centerY;

    public bool IsCellRenderActive => !IsKittyRasterActive && !IsSixelRasterActive;

    public bool IsKittyRasterActive => IsUsingRasterGraphics && ShouldUseKittyGraphics ();

    public bool IsSixelRasterActive => IsUsingRasterGraphics
                                       && !ShouldUseKittyGraphics ()
                                       && _sixelSupportResult is { IsSupported: true };

    public int LastImageColumns { get; private set; }

    public int LastImageRows { get; private set; }

    public int LastPixelHeight { get; private set; }

    public int LastPixelWidth { get; private set; }

    private int _maxIterations = DEFAULT_ITERATIONS;
    public int MaxIterations => _maxIterations;

    private double _span = DEFAULT_SPAN;
    public double Span => _span;

    public string ActiveRenderMode
    {
        get
        {
            if (!IsUsingRasterGraphics)
            {
                return "Cell fallback";
            }

            return ShouldUseKittyGraphics () ? "Kitty raster" : "Sixel raster";
        }
    }

    private SixelSupportResult _sixelSupportResult = new ();

    private KittyGraphicsSupportResult? _kittyGraphicsSupportResult;

    public event EventHandler? ImageRendered;

    public event EventHandler? MandelbrotSettingsChanged;

    public MandelbrotImageView ()
    {
        Width = DefaultColumns;
        Height = DefaultRows;
        BorderStyle = LineStyle.Double;
        CanFocus = true;
        TabStop = TabBehavior.TabStop;
        Arrangement = ViewArrangement.Resizable;
        UseRasterGraphics = true;

        ReplacePanAndZoomCommands ();
        ViewportChanged += (_, _) => RenderMandelbrot ();
    }

    public void RefreshImage () => RenderMandelbrot ();

    public void ResetMandelbrot ()
    {
        SetMandelbrot (DEFAULT_CENTER_X, DEFAULT_CENTER_Y, DEFAULT_SPAN, DEFAULT_ITERATIONS, true);
    }

    public void SetMandelbrot (double centerX, double centerY, double span, int maxIterations)
    {
        SetMandelbrot (centerX, centerY, span, maxIterations, false);
    }

    public void UpdateRasterSupport (SixelSupportResult? sixelResult, KittyGraphicsSupportResult? kittyResult)
    {
        _sixelSupportResult = sixelResult ?? new SixelSupportResult ();
        _kittyGraphicsSupportResult = kittyResult;
        RenderMandelbrot ();
    }

    protected override bool CenterOnViewportPoint (Point position)
    {
        Rectangle viewport = Viewport;

        if (viewport.Width <= 0 || viewport.Height <= 0)
        {
            return true;
        }

        if (position.X < 0 || position.Y < 0 || position.X >= viewport.Width || position.Y >= viewport.Height)
        {
            return true;
        }

        double spanY = _span * viewport.Height / viewport.Width;
        double centerX = _centerX + ((position.X + 0.5d) / viewport.Width - 0.5d) * _span;
        double centerY = _centerY + ((position.Y + 0.5d) / viewport.Height - 0.5d) * spanY;
        SetMandelbrot (centerX, centerY, _span, _maxIterations, true);

        return true;
    }

    protected override bool OnMouseEvent (Mouse mouse)
    {
        if (HandleFractalDrag (mouse))
        {
            return true;
        }

        return base.OnMouseEvent (mouse);
    }

    private static int CountIterations (double cx, double cy, int maxIterations)
    {
        double zx = 0;
        double zy = 0;
        var iterations = 0;

        while (zx * zx + zy * zy <= 4 && iterations < maxIterations)
        {
            double nextX = zx * zx - zy * zy + cx;
            zy = 2 * zx * zy + cy;
            zx = nextX;
            iterations++;
        }

        return iterations;
    }

    private static Color [,] CreateMandelbrotPixels (int width, int height, double centerX, double centerY, double span, int maxIterations)
    {
        Color [,] pixels = new Color [width, height];
        double spanY = span * height / width;
        double xMin = centerX - span / 2;
        double yMin = centerY - spanY / 2;

        for (var y = 0; y < height; y++)
        {
            double cy = yMin + spanY * y / Math.Max (1, height - 1);

            for (var x = 0; x < width; x++)
            {
                double cx = xMin + span * x / Math.Max (1, width - 1);
                int iterations = CountIterations (cx, cy, maxIterations);
                pixels [x, y] = GetMandelbrotColor (iterations, maxIterations);
            }
        }

        return pixels;
    }

    private static Color GetMandelbrotColor (int iterations, int maxIterations)
    {
        if (iterations >= maxIterations)
        {
            return Color.Black;
        }

        double t = (double)iterations / maxIterations;
        var red = (byte)(9 * (1 - t) * t * t * t * 255);
        var green = (byte)(15 * (1 - t) * (1 - t) * t * t * 255);
        var blue = (byte)(8.5 * (1 - t) * (1 - t) * (1 - t) * t * 255);

        return new Color (red, green, blue);
    }

    private Size GetPreferredRasterResolution ()
    {
        if (ShouldUseKittyGraphics () && _kittyGraphicsSupportResult is { IsSupported: true } kitty)
        {
            return kitty.Resolution;
        }

        if (_sixelSupportResult is { IsSupported: true } sixel)
        {
            return sixel.Resolution;
        }

        if (_kittyGraphicsSupportResult is { } detectedKitty)
        {
            return detectedKitty.Resolution;
        }

        return _sixelSupportResult.Resolution;
    }

    private bool HandleFractalDrag (Mouse mouse)
    {
        if (mouse.Position is not { } position)
        {
            return false;
        }

        if (mouse.Flags.FastHasFlags (MouseFlags.LeftButtonPressed) && !mouse.Flags.FastHasFlags (MouseFlags.PositionReport))
        {
            _lastDragPosition = position;
            App?.Mouse.GrabMouse (this);

            return true;
        }

        if (mouse.Flags == (MouseFlags.LeftButtonPressed | MouseFlags.PositionReport) && _lastDragPosition is { } lastDragPosition)
        {
            bool panned = PanMandelbrot (lastDragPosition.X - position.X, lastDragPosition.Y - position.Y);
            _lastDragPosition = position;

            return panned;
        }

        if (!mouse.Flags.FastHasFlags (MouseFlags.LeftButtonReleased))
        {
            return false;
        }

        _lastDragPosition = null;

        if (App is { } && App.Mouse.IsGrabbed (this))
        {
            App.Mouse.UngrabMouse ();
        }

        return true;
    }

    private bool PanMandelbrot (int deltaX, int deltaY)
    {
        Rectangle viewport = Viewport;

        if (viewport.Width <= 0 || viewport.Height <= 0)
        {
            return false;
        }

        double spanY = _span * viewport.Height / viewport.Width;
        double centerX = _centerX + deltaX * _span * PAN_FACTOR;
        double centerY = _centerY + deltaY * spanY * PAN_FACTOR;
        SetMandelbrot (centerX, centerY, _span, _maxIterations, true);

        return true;
    }

    private void ReplacePanAndZoomCommands ()
    {
        AddCommand (Command.ScrollLeft, () => PanMandelbrot (-1, 0));
        AddCommand (Command.ScrollRight, () => PanMandelbrot (1, 0));
        AddCommand (Command.ScrollUp, () => PanMandelbrot (0, -1));
        AddCommand (Command.ScrollDown, () => PanMandelbrot (0, 1));
        AddCommand (Command.Home,
                    () =>
                    {
                        ResetMandelbrot ();

                        return true;
                    });
        AddCommand (Command.ZoomIn, context => ZoomMandelbrot (context, ZOOM_IN_FACTOR));
        AddCommand (Command.ZoomOut, context => ZoomMandelbrot (context, ZOOM_OUT_FACTOR));
        AddCommand (Command.PageUp, context => ZoomMandelbrot (context, ZOOM_IN_FACTOR));
        AddCommand (Command.PageDown, context => ZoomMandelbrot (context, ZOOM_OUT_FACTOR));
    }

    private void RenderMandelbrot ()
    {
        Rectangle viewport = Viewport;
        int imageColumns = Math.Max (0, viewport.Width);
        int imageRows = Math.Max (0, viewport.Height);

        if (imageColumns == 0 || imageRows == 0)
        {
            return;
        }

        Size resolution = GetPreferredRasterResolution ();
        LastImageColumns = imageColumns;
        LastImageRows = imageRows;
        LastPixelWidth = imageColumns * Math.Max (1, resolution.Width);
        LastPixelHeight = imageRows * Math.Max (1, resolution.Height);
        Image = CreateMandelbrotPixels (LastPixelWidth, LastPixelHeight, _centerX, _centerY, _span, _maxIterations);
        ImageRendered?.Invoke (this, EventArgs.Empty);
    }

    private void SetMandelbrot (double centerX, double centerY, double span, int maxIterations, bool notifySettingsChanged)
    {
        if (span < MinimumSpan)
        {
            throw new ArgumentOutOfRangeException (nameof (span), @"Mandelbrot span must be greater than or equal to the minimum span.");
        }

        if (maxIterations < MinimumIterations)
        {
            throw new ArgumentOutOfRangeException (nameof (maxIterations), @"Mandelbrot iterations must be greater than or equal to the minimum iterations.");
        }

        if (_centerX == centerX && _centerY == centerY && _span == span && _maxIterations == maxIterations)
        {
            return;
        }

        _centerX = centerX;
        _centerY = centerY;
        _span = span;
        _maxIterations = maxIterations;
        RenderMandelbrot ();

        if (notifySettingsChanged)
        {
            MandelbrotSettingsChanged?.Invoke (this, EventArgs.Empty);
        }
    }

    private bool ShouldUseKittyGraphics () =>
        _kittyGraphicsSupportResult is { IsSupported: true }
        && App?.Driver?.GetOutput ().UseKittyGraphics == true;

    private bool ZoomMandelbrot (ICommandContext? context, double spanMultiplier)
    {
        Point? anchor = context?.Binding is MouseBinding { MouseEvent.Position: { } mousePosition } ? mousePosition : null;
        double nextSpan = Math.Max (MinimumSpan, _span * spanMultiplier);

        if (Math.Abs (nextSpan - _span) < double.Epsilon)
        {
            return false;
        }

        if (anchor is { } anchorPosition && TryGetMandelbrotPoint (anchorPosition, _span, out double anchorX, out double anchorY))
        {
            double centerX = anchorX - (anchorX - _centerX) * spanMultiplier;
            double centerY = anchorY - (anchorY - _centerY) * spanMultiplier;
            SetMandelbrot (centerX, centerY, nextSpan, _maxIterations, true);

            return true;
        }

        SetMandelbrot (_centerX, _centerY, nextSpan, _maxIterations, true);

        return true;
    }

    private bool TryGetMandelbrotPoint (Point position, double span, out double x, out double y)
    {
        x = _centerX;
        y = _centerY;
        Rectangle viewport = Viewport;

        if (viewport.Width <= 0 || viewport.Height <= 0)
        {
            return false;
        }

        if (position.X < 0 || position.Y < 0 || position.X >= viewport.Width || position.Y >= viewport.Height)
        {
            return false;
        }

        double spanY = span * viewport.Height / viewport.Width;
        x = _centerX + ((position.X + 0.5d) / viewport.Width - 0.5d) * span;
        y = _centerY + ((position.Y + 0.5d) / viewport.Height - 0.5d) * spanY;

        return true;
    }
}
