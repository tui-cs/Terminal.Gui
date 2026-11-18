namespace Terminal.Gui.Views;

public partial class ImageView
{
    private Point? _lastDragPosition;

    private bool PanByCells (int deltaX, int deltaY)
    {
        if (_image is null || _zoomLevel <= FIT_ZOOM_LEVEL || Viewport.Width <= 0 || Viewport.Height <= 0)
        {
            return false;
        }

        RectangleF visibleSource = GetVisibleSourceRectangle ();
        double centerX = _centerX + deltaX * (double)visibleSource.Width / Viewport.Width / _image.GetLength (0);
        double centerY = _centerY + deltaY * (double)visibleSource.Height / Viewport.Height / _image.GetLength (1);

        return SetCenter (centerX, centerY);
    }

    private bool ScrollFromCommand (int deltaX, int deltaY)
    {
        PanByCells (deltaX, deltaY);

        return true;
    }

    private bool ResetView ()
    {
        if (Math.Abs (_zoomLevel - FIT_ZOOM_LEVEL) < double.Epsilon
            && Math.Abs (_centerX - 0.5d) < double.Epsilon
            && Math.Abs (_centerY - 0.5d) < double.Epsilon)
        {
            return false;
        }

        _zoomLevel = FIT_ZOOM_LEVEL;
        _centerX = 0.5d;
        _centerY = 0.5d;
        InvalidateScaledImage ();
        ZoomLevelChanged?.Invoke (this, EventArgs.Empty);

        return true;
    }

    private bool SetCenter (double centerX, double centerY)
    {
        double previousCenterX = _centerX;
        double previousCenterY = _centerY;
        _centerX = centerX;
        _centerY = centerY;
        ClampCenter ();

        if (Math.Abs (previousCenterX - _centerX) < double.Epsilon && Math.Abs (previousCenterY - _centerY) < double.Epsilon)
        {
            return false;
        }

        InvalidateScaledImage ();

        return true;
    }

    private bool SetZoomLevel (double zoomLevel, Point? anchor)
    {
        if (double.IsNaN (zoomLevel) || double.IsInfinity (zoomLevel))
        {
            throw new ArgumentOutOfRangeException (nameof (zoomLevel), @"Zoom level must be a finite number.");
        }

        double previousZoomLevel = _zoomLevel;
        double clampedZoomLevel = Math.Clamp (zoomLevel, GetMinimumZoomLevel (), MAX_ZOOM_LEVEL);

        if (Math.Abs (previousZoomLevel - clampedZoomLevel) < double.Epsilon)
        {
            return false;
        }

        if (anchor is { } position && TryMapViewportPointToSourceCenter (position, out double sourceX, out double sourceY))
        {
            _zoomLevel = clampedZoomLevel;
            SetCenterForAnchor (position, sourceX, sourceY);
        }
        else
        {
            _zoomLevel = clampedZoomLevel;
            ClampCenter ();
        }

        InvalidateScaledImage ();
        ZoomLevelChanged?.Invoke (this, EventArgs.Empty);

        return true;
    }

    private void SetCenterForAnchor (Point position, double sourceX, double sourceY)
    {
        if (_image is null || Viewport.Width <= 0 || Viewport.Height <= 0)
        {
            return;
        }

        double effectiveZoom = Math.Max (FIT_ZOOM_LEVEL, _zoomLevel);
        double visibleWidth = 1d / effectiveZoom;
        double visibleHeight = 1d / effectiveZoom;
        _centerX = sourceX - ((position.X + 0.5d) / Viewport.Width - 0.5d) * visibleWidth;
        _centerY = sourceY - ((position.Y + 0.5d) / Viewport.Height - 0.5d) * visibleHeight;
        ClampCenter ();
    }

    private bool TryMapViewportPointToSourceCenter (Point position, out double centerX, out double centerY)
    {
        centerX = 0.5d;
        centerY = 0.5d;

        if (_image is null || Viewport.Width <= 0 || Viewport.Height <= 0)
        {
            return false;
        }

        if (position.X < 0 || position.Y < 0 || position.X >= Viewport.Width || position.Y >= Viewport.Height)
        {
            return false;
        }

        RectangleF visibleSource = GetVisibleSourceRectangle ();
        centerX = (visibleSource.X + (position.X + 0.5d) * visibleSource.Width / Viewport.Width) / _image.GetLength (0);
        centerY = (visibleSource.Y + (position.Y + 0.5d) * visibleSource.Height / Viewport.Height) / _image.GetLength (1);

        return true;
    }

    private bool ZoomFromCommand (ICommandContext? context, double zoomLevel)
    {
        Point? anchor = context?.Binding is MouseBinding { MouseEvent.Position: { } position } ? position : null;

        return SetZoomLevel (zoomLevel, anchor);
    }

    /// <inheritdoc/>
    protected override bool OnMouseEvent (Mouse mouse)
    {
        return HandleDrag (mouse) || base.OnMouseEvent (mouse);
    }

    private bool HandleDrag (Mouse mouse)
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
            bool panned = PanByCells (lastDragPosition.X - position.X, lastDragPosition.Y - position.Y);
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
}
