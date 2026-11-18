namespace Terminal.Gui.Input;

/// <summary>
///     Describes the type of keyboard event: press, release, or repeat.
/// </summary>
/// <remarks>
///     <para>
///         Not all drivers report all event types. When a driver does not distinguish press from repeat,
///         or does not report key releases, the event type defaults to <see cref="Press"/>.
///     </para>
///     <para>
///         This enum is used by <see cref="Key.EventType"/> to carry richer keyboard semantics
///         through the input pipeline.
///     </para>
/// </remarks>
public enum KeyEventType
{
    /// <summary>
    ///     A key was pressed. This is the default and matches legacy <see cref="Key"/> behavior.
    /// </summary>
    Press = 1,

    /// <summary>
    ///     A key that was previously pressed is being held down and the terminal is sending repeat events.
    /// </summary>
    Repeat = 2,

    /// <summary>
    ///     A key was released.
    /// </summary>
    Release = 3
}
