namespace Terminal.Gui.Testing;

/// <summary>
///     Mode for input injection, controlling how input flows through the system.
/// </summary>
public enum InputInjectionMode
{
    /// <summary>
    ///     Direct event injection - bypasses encoding/parsing, preserves all properties.
    ///     Use for testing View/Application logic with precise control.
    /// </summary>
    Direct,

    /// <summary>
    ///     Pipeline injection - goes through full encoding/parsing pipeline.
    ///     Use for testing ANSI encoding/decoding and parser behavior.
    /// </summary>
    Pipeline,

    /// <summary>
    ///     Automatic mode - uses Direct for simple test scenarios, Pipeline for full driver testing.
    ///     Recommended default for most tests.
    /// </summary>
    Auto
}
