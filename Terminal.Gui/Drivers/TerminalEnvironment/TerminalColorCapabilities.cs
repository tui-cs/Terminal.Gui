namespace Terminal.Gui.Drivers;

/// <summary>
///     Describes the color capabilities of the terminal as detected from environment variables.
/// </summary>
public record TerminalColorCapabilities
{
    /// <summary>
    ///     Gets the value of the <c>TERM</c> environment variable, if set.
    /// </summary>
    public string? Term { get; init; }

    /// <summary>
    ///     Gets the value of the <c>COLORTERM</c> environment variable, if set.
    /// </summary>
    public string? ColorTerm { get; init; }

    /// <summary>
    ///     Gets the value of the <c>TERM_PROGRAM</c> environment variable, if set.
    /// </summary>
    public string? TermProgram { get; init; }

    /// <summary>
    ///     Gets whether the terminal is Windows Terminal (detected via the <c>WT_SESSION</c> environment variable).
    /// </summary>
    public bool IsWindowsTerminal { get; init; }

    /// <summary>
    ///     Gets the detected color capability level.
    /// </summary>
    public ColorCapabilityLevel Capability { get; internal set; }
}