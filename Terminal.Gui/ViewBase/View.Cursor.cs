namespace Terminal.Gui.ViewBase;

public partial class View
{
    private Cursor _cursor = new () { Position = null };

    /// <summary>
    ///     Gets or sets the cursor for this view. <see cref="Cursor.Position"/> must be in screen coordinates.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Use <c>ViewportToScreen()</c> to convert from view-relative coordinates.
    ///     </para>
    ///     <para>
    ///         To hide the cursor, set <see cref="Cursor.Position"/> to null or set the Style property to
    ///         <see cref="CursorStyle.Hidden"/>.
    ///     </para>
    ///     <para>
    ///         Common patterns:
    ///         <code>
    /// // Text cursor at column 5 in viewport - convert to screen coords
    /// Point screenPos = ViewportToScreen(new Point(5, 0));
    /// SetCursor(new Cursor { Position = screenPos, Shape = CursorStyle.BlinkingBar });
    /// 
    /// // Hide cursor
    /// SetCursor(new Cursor { Position = null });
    /// SetCursor(new Cursor { Style = CursorStyle.Hidden });
    /// 
    /// // Update position keeping same shape
    /// Point newScreenPos = ViewportToScreen(new Point(6, 0));
    /// SetCursor(_cursor with { Position = newScreenPos });
    /// </code>
    ///     </para>
    /// </remarks>
    public Cursor Cursor { get => _cursor; set => SetCursor (value); }

    /// <summary>
    ///     INTERNAL: Sets the cursor for this view.
    /// </summary>
    private void SetCursor (Cursor cursor)
    {
        if (_cursor == cursor)
        {
            return;
        }

        _cursor = cursor;

        if (HasFocus)
        {
            SetCursorNeedsUpdate ();
        }
    }

    /// <summary>
    ///     Signals that the cursor position needs to be updated without requiring a full redraw.
    /// </summary>
    public void SetCursorNeedsUpdate () => App?.Driver?.SetCursorNeedsUpdate (true);
}
