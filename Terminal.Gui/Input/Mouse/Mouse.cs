namespace Terminal.Gui.Input;

// NOTE: It may be tempting to think this should be a record struct.
// NOTE: If this were a struct, it would be boxed when used in events, and the ability to
// NOTE: modify properties like Handled would be lost on the boxed copy.
// NOTE: Key is a class for the same reason.

/// <summary>
///     Provides an abstraction for common mouse operations and state.
///     Represents a mouse event, including position, button state, and other flags.
/// </summary>
/// <remarks>
///     <para>
///         The <see cref="View.MouseEvent"/> event uses this class.
///     </para>
/// </remarks>
public class Mouse : EventArgs
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Mouse"/> class.
    /// </summary>
    public Mouse () { }

    /// <summary>
    ///     Gets or sets a value indicating whether the mouse event was handled.
    /// </summary>
    /// <remarks>
    ///     Set this to <see langword="true"/> to prevent the event from being processed by other views.
    /// </remarks>
    public bool Handled { get; set; }

    /// <summary>
    ///     The timestamp when this mouse event was created. Used for multi-click detection timing.
    /// </summary>
    public DateTime? Timestamp { get; set; }

    /// <summary>
    ///     Flags indicating the state of the mouse buttons and the type of event that occurred.
    /// </summary>
    public MouseFlags Flags { get; set; }

    /// <summary>
    ///     The screen-relative mouse position, in columns and rows.
    /// </summary>
    public Point ScreenPosition { get; set; }

    /// <summary>
    ///     The view that is the target of the mouse event.
    /// </summary>
    /// <remarks>
    ///     This is the deepest view in the view hierarchy that contains the mouse position.
    /// </remarks>
    public View? View { get; set; }

    /// <summary>
    ///     The position of the mouse in <see cref="View"/>'s viewport-relative coordinates.
    /// </summary>
    /// <remarks>
    ///     This property is only valid if <see cref="View"/> is not <see langword="null"/>.
    /// </remarks>
    public Point? Position { get; set; }

    /// <summary>
    ///     Gets a value indicating whether a mouse button was pressed.
    /// </summary>
    public bool IsPressed =>
        Flags.FastHasFlags (MouseFlags.LeftButtonPressed)
        || Flags.FastHasFlags (MouseFlags.MiddleButtonPressed)
        || Flags.FastHasFlags (MouseFlags.RightButtonPressed)
        || Flags.FastHasFlags (MouseFlags.Button4Pressed);

    /// <summary>
    ///     Gets a value indicating whether a mouse button was released.
    /// </summary>
    public bool IsReleased =>
        Flags.FastHasFlags (MouseFlags.LeftButtonReleased)
        || Flags.FastHasFlags (MouseFlags.MiddleButtonReleased)
        || Flags.FastHasFlags (MouseFlags.RightButtonReleased)
        || Flags.FastHasFlags (MouseFlags.Button4Released);

    /// <summary>
    ///     Gets a value indicating whether a single-click mouse event occurred.
    /// </summary>
    public bool IsSingleClicked =>
        Flags.FastHasFlags (MouseFlags.LeftButtonClicked)
        || Flags.FastHasFlags (MouseFlags.MiddleButtonClicked)
        || Flags.FastHasFlags (MouseFlags.RightButtonClicked)
        || Flags.FastHasFlags (MouseFlags.Button4Clicked);

    /// <summary>
    ///     Gets a value indicating whether a double-click mouse event occurred.
    /// </summary>
    public bool IsDoubleClicked =>
        Flags.FastHasFlags (MouseFlags.LeftButtonDoubleClicked)
        || Flags.FastHasFlags (MouseFlags.MiddleButtonDoubleClicked)
        || Flags.FastHasFlags (MouseFlags.RightButtonDoubleClicked)
        || Flags.FastHasFlags (MouseFlags.Button4DoubleClicked);

    /// <summary>
    ///     Gets a value indicating whether a triple-click mouse event occurred.
    /// </summary>
    public bool IsTripleClicked =>
        Flags.FastHasFlags (MouseFlags.LeftButtonTripleClicked)
        || Flags.FastHasFlags (MouseFlags.MiddleButtonTripleClicked)
        || Flags.FastHasFlags (MouseFlags.RightButtonTripleClicked)
        || Flags.FastHasFlags (MouseFlags.Button4TripleClicked);

    /// <summary>
    ///     Gets a value indicating whether a single, double, or triple-click mouse event occurred.
    /// </summary>
    public bool IsSingleDoubleOrTripleClicked =>
        Flags.FastHasFlags (MouseFlags.LeftButtonClicked)
        || Flags.FastHasFlags (MouseFlags.MiddleButtonClicked)
        || Flags.FastHasFlags (MouseFlags.RightButtonClicked)
        || Flags.FastHasFlags (MouseFlags.Button4Clicked)
        || Flags.FastHasFlags (MouseFlags.LeftButtonDoubleClicked)
        || Flags.FastHasFlags (MouseFlags.MiddleButtonDoubleClicked)
        || Flags.FastHasFlags (MouseFlags.RightButtonDoubleClicked)
        || Flags.FastHasFlags (MouseFlags.Button4DoubleClicked)
        || Flags.FastHasFlags (MouseFlags.LeftButtonTripleClicked)
        || Flags.FastHasFlags (MouseFlags.MiddleButtonTripleClicked)
        || Flags.FastHasFlags (MouseFlags.RightButtonTripleClicked)
        || Flags.FastHasFlags (MouseFlags.Button4TripleClicked);

    /// <summary>
    ///     Gets a value indicating whether a mouse wheel event occurred.
    /// </summary>
    public bool IsWheel =>
        Flags.FastHasFlags (MouseFlags.WheeledDown)
        || Flags.FastHasFlags (MouseFlags.WheeledUp)
        || Flags.FastHasFlags (MouseFlags.WheeledLeft)
        || Flags.FastHasFlags (MouseFlags.WheeledRight);

    /// <summary>Returns a string that represents the current mouse event.</summary>
    /// <returns>A string that represents the current mouse event.</returns>
    public override string ToString () => $"{Flags}:{ScreenPosition}:{(Position is { } ? Position.ToString () : "")}";
}
