using System.Text.Json.Serialization;

namespace Terminal.Gui.ViewBase;

/// <summary>
///     Used to describe the state of the mouse in relation to a <see cref="View"/> (<see cref="View.MouseState"/>) and to
///     specify visual effects,
///     such as highlighting a button when the mouse is over it or changing the appearance of a view when the mouse is
///     pressed (<see cref="View.MouseHighlightStates"/>).
/// </summary>
/// <seealso cref="View.MouseState"/>
/// <seealso cref="View.MouseHighlightStates"/>
[JsonConverter (typeof (JsonStringEnumConverter<MouseState>))]
[Flags]
public enum MouseState
{
    /// <summary>
    ///     No mouse interaction with the view is occurring.
    /// </summary>
    None = 0,

    /// <summary>
    ///     The mouse is in the <see cref="View.Viewport"/> (but not pressed). Set between the <see cref="View.MouseEnter"/>
    ///     and <see cref="View.MouseLeave"/> events.
    /// </summary>
    In = 1,

    /// <summary>
    ///     The mouse is in the <see cref="View.Viewport"/> and is pressed.
    /// </summary>
    Pressed = 2,

    /// <summary>
    ///     The mouse is outside the <see cref="View.Viewport"/> and is pressed. If
    ///     <see cref="View.MouseHoldRepeat"/> is true,
    ///     this flag is ignored so that the view remains in the pressed state until the mouse is released.
    /// </summary>
    PressedOutside = 4
}
