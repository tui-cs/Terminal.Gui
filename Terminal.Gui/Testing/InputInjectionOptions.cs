namespace Terminal.Gui.Testing;

/// <summary>
///     Configuration for input injection behavior.
/// </summary>
public class InputInjectionOptions
{
    /// <summary>
    ///     Injection mode (Direct, Pipeline, or Auto).
    /// </summary>
    public InputInjectionMode Mode { get; set; } = InputInjectionMode.Auto;

    /// <summary>
    ///     Whether to automatically process the input queue after injection.
    /// </summary>
    public bool AutoProcess { get; set; } = true;

    /// <summary>
    ///     Time provider to use for timestamps and timing.
    /// </summary>
    public ITimeProvider? TimeProvider { get; set; }
}
