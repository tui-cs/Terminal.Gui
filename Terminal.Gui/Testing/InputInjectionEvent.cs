namespace Terminal.Gui.Testing;

/// <summary>
///     Base class for input injection events in sequences.
/// </summary>
public abstract record InputInjectionEvent
{
    /// <summary>
    ///     Optional delay before processing this event.
    /// </summary>
    public TimeSpan? Delay { get; init; }
}

/// <summary>
///     Keyboard input injection event in a sequence.
/// </summary>
/// <param name="Key">The key to inject.</param>
public record KeyInjectionEvent (Key Key) : InputInjectionEvent;

/// <summary>
///     Mouse input injection in a sequence.
/// </summary>
/// <param name="Mouse">The mouse event to inject.</param>
public record MouseInjectionEvent (Mouse Mouse) : InputInjectionEvent;