namespace Terminal.Gui.Testing;

/// <summary>
///     High-level input injection API - single entry point for all injection.
/// </summary>
public interface IInputInjector
{
    /// <summary>
    ///     Injects a keyboard event. Handles encoding, queueing, and processing automatically.
    /// </summary>
    /// <param name="key">The key to inject.</param>
    /// <param name="options">Optional injection options.</param>
    void InjectKey (Key key, InputInjectionOptions? options = null);

    /// <summary>
    ///     Injects a mouse event. Handles encoding, queueing, and processing automatically.
    /// </summary>
    /// <param name="mouseEvent">The mouse event to inject.</param>
    /// <param name="options">Optional injection options.</param>
    void InjectMouse (Mouse mouseEvent, InputInjectionOptions? options = null);

    /// <summary>
    ///     Injects a sequence of input events with delays between them.
    /// </summary>
    /// <param name="events">The sequence of events to inject.</param>
    /// <param name="options">Optional injection options.</param>
    void InjectSequence (IEnumerable<InputInjectionEvent> events, InputInjectionOptions? options = null);

    /// <summary>
    ///     Forces processing of the input queue (usually automatic).
    /// </summary>
    void ProcessQueue ();
}
