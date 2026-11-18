namespace Terminal.Gui.Input;

/// <summary>Mouse flags reported in <see cref="Mouse"/>.</summary>
/// <remarks>
///     <para>
///         This enum provides both numbered button flags (LeftButton, MiddleButton, RightButton, Button4) and semantic aliases
///         (LeftButton, MiddleButton, RightButton) for improved code readability. The numbered flags follow the
///         ncurses convention, while the semantic aliases map to the standard mouse button layout:
///         LeftButton = Left, MiddleButton = Middle, RightButton = Right, Button4 = Extra/XLeftButton.
///     </para>
///     <para>
///         Each button supports multiple event types: Pressed, Released, Clicked, DoubleClicked, and TripleClicked.
///         Additionally, modifier flags (ButtonShift, ButtonCtrl, ButtonAlt) can be combined with button events using
///         bitwise OR operations.
///     </para>
/// </remarks>
[Flags]
public enum MouseFlags
{
    /// <summary>
    ///     No mouse event. This is the default value for <see cref="Mouse.Flags"/> when no mouse event is being
    ///     reported.
    /// </summary>
    None = 0,

    /// <summary>The first mouse button was pressed.</summary>
    LeftButtonPressed = 0x2,

    /// <summary>The first mouse button was released.</summary>
    LeftButtonReleased = 0x1,

    /// <summary>The first mouse button was clicked (press+release).</summary>
    LeftButtonClicked = 0x4,

    /// <summary>The first mouse button was double-clicked.</summary>
    LeftButtonDoubleClicked = 0x8,

    /// <summary>The first mouse button was triple-clicked.</summary>
    LeftButtonTripleClicked = 0x10,

    /// <summary>The second mouse button was pressed.</summary>
    MiddleButtonPressed = 0x80,

    /// <summary>The second mouse button was released.</summary>
    MiddleButtonReleased = 0x40,

    /// <summary>The second mouse button was clicked (press+release).</summary>
    MiddleButtonClicked = 0x100,

    /// <summary>The second mouse button was double-clicked.</summary>
    MiddleButtonDoubleClicked = 0x200,

    /// <summary>The second mouse button was triple-clicked.</summary>
    MiddleButtonTripleClicked = 0x400,

    /// <summary>The third mouse button was pressed.</summary>
    RightButtonPressed = 0x2000,

    /// <summary>The third mouse button was released.</summary>
    RightButtonReleased = 0x1000,

    /// <summary>The third mouse button was clicked (press+release).</summary>
    RightButtonClicked = 0x4000,

    /// <summary>The third mouse button was double-clicked.</summary>
    RightButtonDoubleClicked = 0x8000,

    /// <summary>The third mouse button was triple-clicked.</summary>
    RightButtonTripleClicked = 0x10000,

    /// <summary>The fourth mouse button was pressed.</summary>
    Button4Pressed = 0x80000,

    /// <summary>The fourth mouse button was released.</summary>
    Button4Released = 0x40000,

    /// <summary>The fourth mouse button was clicked.</summary>
    Button4Clicked = 0x100000,

    /// <summary>The fourth mouse button was double-clicked.</summary>
    Button4DoubleClicked = 0x200000,

    /// <summary>The fourth mouse button was triple-clicked.</summary>
    Button4TripleClicked = 0x400000,

    /// <summary>Flag: the shift key was pressed when the mouse button took place.</summary>
    Shift = 0x2000000,

    /// <summary>Flag: the ctrl key was pressed when the mouse button took place.</summary>
    Ctrl = 0x1000000,

    /// <summary>Flag: the alt key was pressed when the mouse button took place.</summary>
    Alt = 0x4000000,

    /// <summary>The mouse position is being reported in this event.</summary>
    PositionReport = 0x8000000,

    /// <summary>Vertical button wheeled up.</summary>
    WheeledUp = 0x10000000,

    /// <summary>Vertical button wheeled down.</summary>
    WheeledDown = 0x20000000,

    /// <summary>Vertical button wheeled up while pressing Ctrl.</summary>
    WheeledLeft = Ctrl | WheeledUp,

    /// <summary>Vertical button wheeled down while pressing Ctrl.</summary>
    WheeledRight = Ctrl | WheeledDown,

    /// <summary>Mask that captures all the events.</summary>
    AllEvents = 0x7ffffff
}
