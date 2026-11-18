namespace Terminal.Gui.ViewBase;

/// <summary>
///     Provides data for events that allow cancellation of adornment drawing in the Cancellable Work Pattern (CWP).
/// </summary>
/// <remarks>
///     <para>
///         Used in events raised by <see cref="View.DoDrawAdornments"/> to allow handlers to cancel the drawing
///         of <see cref="View.Margin"/>, <see cref="View.Border"/>, and <see cref="View.Padding"/> adornments.
///     </para>
/// </remarks>
/// <seealso cref="View.DrawAdornments"/>
/// <seealso cref="CWPEventHelper"/>
public class DrawAdornmentsEventArgs
{
    /// <summary>
    ///     Gets the draw context for tracking drawn regions, or null if not tracking.
    /// </summary>
    public DrawContext? Context { get; }

    /// <summary>
    ///     Gets or sets a value indicating whether the adornment drawing is handled. If true, drawing is cancelled.
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DrawAdornmentsEventArgs"/> class.
    /// </summary>
    /// <param name="context">The draw context, or null if not tracking.</param>
    public DrawAdornmentsEventArgs (DrawContext? context)
    {
        Context = context;
    }
}
