namespace Terminal.Gui.Drivers;

/// <summary>
///     Represents a cursor with position in screen coordinates and shape.
/// </summary>
/// <remarks>
///     <para>
///         The position is always in screen coordinates.
///         Views are responsible for converting from their content-area or viewport coordinates to
///         screen coordinates before setting the cursor.
///     </para>
///     <para>
///         Immutable value type - use with 'with' expression to modify:
///         <code>
/// Cursor newCursor = currentCursor with { Position = new Point(5, 0) };
/// </code>
///     </para>
///     <para>
///         To hide the cursor, set Position to null or set the Style property to <see cref="CursorStyle.Hidden"/>.
///     </para>
/// </remarks>
public record Cursor
{
    /// <summary>
    ///     Gets the cursor position in screen coordinates.
    /// </summary>
    /// <remarks>
    ///     Null position indicates the cursor is hidden.
    ///     When setting, ensure coordinates are in screen space (not content-area or viewport relative).
    ///     Use <c>View.ContentToScreen()</c> or <c>View.ViewportToScreen()</c> to convert if needed.
    /// </remarks>
    public Point? Position { get; init; }

    /// <summary>
    ///     Gets the cursor style.
    /// </summary>
    /// <remarks>
    ///     Defines the visual appearance when <see cref="Position"/> is not null.
    ///     Default is <see cref="CursorStyle.Hidden"/>.
    /// </remarks>
    public CursorStyle Style { get; init; } = CursorStyle.Hidden;

    /// <summary>
    ///     Gets whether the cursor is visible (has valid position).
    /// </summary>
    public bool IsVisible => Position.HasValue && Style != CursorStyle.Hidden;

    /// <summary>
    ///     Returns string representation for debugging.
    /// </summary>
    public override string ToString ()
    {
        if (!IsVisible)
        {
            return "Cursor { Hidden }";
        }

        return $"Cursor {{ Position = {Position}, Style = {Style} }}";
    }
}
