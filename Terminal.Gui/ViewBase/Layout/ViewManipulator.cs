namespace Terminal.Gui.ViewBase;

/// <summary>
///     Helper class that encapsulates view manipulation operations for arrangement (move/resize).
///     Provides methods to move and resize a view while respecting minimum size constraints.
/// </summary>
public sealed class ViewManipulator
{
    private readonly View _view;
    private readonly Point _grabPoint;
    private readonly int _minWidth;
    private readonly int _minHeight;

    /// <summary>
    ///     Creates a new ViewManipulator for the specified view.
    /// </summary>
    /// <param name="view">The view to manipulate.</param>
    /// <param name="grabPoint">The point where the drag started (in frame coordinates).</param>
    /// <param name="minWidth">Minimum allowed width.</param>
    /// <param name="minHeight">Minimum allowed height.</param>
    public ViewManipulator (View view, Point grabPoint, int minWidth, int minHeight)
    {
        _view = view;
        _grabPoint = grabPoint;
        _minWidth = minWidth;
        _minHeight = minHeight;
    }

    /// <summary>
    ///     Creates a ViewManipulator for keyboard-based arrangement (no grab point needed).
    /// </summary>
    /// <param name="view">The view to manipulate.</param>
    /// <param name="minWidth">Minimum allowed width.</param>
    /// <param name="minHeight">Minimum allowed height.</param>
    public ViewManipulator (View view, int minWidth, int minHeight) : this (view, Point.Empty, minWidth, minHeight) { }

    #region Mouse-Based Manipulation (Absolute Positioning)

    /// <summary>
    ///     Moves the view to follow the mouse position.
    /// </summary>
    /// <param name="location">Mouse position in view's coordinate space.</param>
    public void Move (Point location)
    {
        _view.X = location.X - _grabPoint.X;
        _view.Y = location.Y - _grabPoint.Y;
    }

    /// <summary>
    ///     Resizes view from the top edge, adjusting Y position and height.
    /// </summary>
    /// <param name="location">Mouse position in view's coordinate space.</param>
    public void ResizeTop (Point location)
    {
        int deltaY = location.Y - _view.Frame.Y;
        int newHeight = Math.Max (_minHeight, _view.Frame.Height - deltaY);

        if (newHeight == _view.Frame.Height)
        {
            return;
        }
        _view.Frame = _view.Frame with { Height = newHeight, Y = location.Y - _grabPoint.Y };
    }

    /// <summary>
    ///     Resizes view from the bottom edge, adjusting height only.
    /// </summary>
    /// <param name="location">Mouse position in view's coordinate space.</param>
    public void ResizeBottom (Point location) => _view.Height = Math.Max (_minHeight, location.Y - _view.Frame.Y + _view.Margin.Thickness.Bottom + 1);

    /// <summary>
    ///     Resizes view from the left edge, adjusting X position and width.
    /// </summary>
    /// <param name="location">Mouse position in view's coordinate space.</param>
    public void ResizeLeft (Point location)
    {
        int deltaX = location.X - _view.Frame.X;
        int newWidth = Math.Max (_minWidth, _view.Frame.Width - deltaX);

        if (newWidth == _view.Frame.Width)
        {
            return;
        }
        _view.Frame = _view.Frame with { Width = newWidth, X = location.X - _grabPoint.X };
    }

    /// <summary>
    ///     Resizes view from the right edge, adjusting width only.
    /// </summary>
    /// <param name="location">Mouse position in view's coordinate space.</param>
    public void ResizeRight (Point location) => _view.Frame = _view.Frame with { Width = Math.Max (_minWidth, location.X - _view.Frame.X + _view.Margin.Thickness.Right + 1) };

    #endregion

    #region Keyboard-Based Manipulation (Delta/Increment)

    /// <summary>
    ///     Adjusts the view's X position by the specified delta.
    /// </summary>
    /// <param name="delta">Amount to adjust X by (positive = right, negative = left).</param>
    public void AdjustX (int delta) => _view.X += delta;

    /// <summary>
    ///     Adjusts the view's Y position by the specified delta.
    /// </summary>
    /// <param name="delta">Amount to adjust Y by (positive = down, negative = up).</param>
    public void AdjustY (int delta) => _view.Y += delta;

    /// <summary>
    ///     Adjusts the view's width by the specified delta, respecting minimum width.
    /// </summary>
    /// <param name="delta">Amount to adjust width by.</param>
    /// <returns>True if the width was actually changed.</returns>
    public bool AdjustWidth (int delta)
    {
        int currentWidth = _view.Viewport.Width;

        // Don't shrink if already at or below zero viewport width
        if (delta < 0 && currentWidth <= 0)
        {
            return false;
        }

        // Calculate new width using current frame width and ensure it respects minimum
        int currentFrameWidth = _view.Frame.Width;
        int newWidth = Math.Max (_minWidth, currentFrameWidth + delta);

        // Only apply if it actually changed
        if (newWidth == currentFrameWidth)
        {
            return false;
        }

        _view.Frame = _view.Frame with { Width = newWidth };

        return true;
    }

    /// <summary>
    ///     Adjusts the view's height by the specified delta, respecting minimum height.
    /// </summary>
    /// <param name="delta">Amount to adjust height by.</param>
    /// <returns>True if the height was actually changed.</returns>
    public bool AdjustHeight (int delta)
    {
        int currentHeight = _view.Viewport.Height;

        // Don't shrink if already at or below zero viewport height
        if (delta < 0 && currentHeight <= 0)
        {
            return false;
        }

        // Calculate new height using current frame height and ensure it respects minimum
        int currentFrameHeight = _view.Frame.Height;
        int newHeight = Math.Max (_minHeight, currentFrameHeight + delta);

        // Only apply if it actually changed
        if (newHeight == currentFrameHeight)
        {
            return false;
        }

        _view.Frame = _view.Frame with { Height = newHeight };

        return true;
    }

    /// <summary>
    ///     Resizes from the top edge by adjusting both Y position and height (for keyboard).
    ///     Moves the view up/down while expanding/contracting from the top.
    /// </summary>
    /// <param name="delta">Amount to adjust (negative = up/expand, positive = down/contract).</param>
    /// <returns>True if the resize was performed.</returns>
    public bool ResizeFromTop (int delta)
    {
        int currentHeight = _view.Viewport.Height;

        // Don't shrink if already at or below zero viewport height
        if (delta > 0 && currentHeight <= 0)
        {
            return false;
        }

        // Calculate new height using current frame height and ensure it respects minimum
        int currentFrameHeight = _view.Frame.Height;
        int newHeight = Math.Max (_minHeight, currentFrameHeight - delta);

        // Only apply if height would actually change
        if (newHeight == currentFrameHeight)
        {
            return false;
        }
        int actualDelta = currentFrameHeight - newHeight;

        _view.Frame = _view.Frame with { Y = _view.Frame.Y + actualDelta, Height = newHeight };

        return true;
    }

    /// <summary>
    ///     Resizes from the left edge by adjusting both X position and width (for keyboard).
    ///     Moves the view left/right while expanding/contracting from the left.
    /// </summary>
    /// <param name="delta">Amount to adjust (negative = left/expand, positive = right/contract).</param>
    /// <returns>True if the resize was performed.</returns>
    public bool ResizeFromLeft (int delta)
    {
        int currentWidth = _view.Viewport.Width;

        // Don't shrink if already at or below zero viewport width
        if (delta > 0 && currentWidth <= 0)
        {
            return false;
        }

        // Calculate new width using current frame width and ensure it respects minimum
        int currentFrameWidth = _view.Frame.Width;
        int newWidth = Math.Max (_minWidth, currentFrameWidth - delta);

        // Only apply if width would actually change
        if (newWidth == currentFrameWidth)
        {
            return false;
        }
        int actualDelta = currentFrameWidth - newWidth;

        _view.Frame = _view.Frame with { X = _view.Frame.X + actualDelta, Width = newWidth };

        return true;
    }

    #endregion
}
