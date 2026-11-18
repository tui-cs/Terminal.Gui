namespace Terminal.Gui.Drivers;

/// <summary>
///     Describes the color capability level of a terminal.
/// </summary>
public enum ColorCapabilityLevel
{
    /// <summary>
    ///     Terminal does not support color output (<c>TERM=dumb</c> or <c>NO_COLOR</c> set).
    /// </summary>
    NoColor,

    /// <summary>
    ///     Terminal supports 16 standard ANSI colors.
    /// </summary>
    Colors16,

    /// <summary>
    ///     Terminal supports 256 indexed colors.
    /// </summary>
    Colors256,

    /// <summary>
    ///     Terminal supports 24-bit TrueColor (16 million colors).
    /// </summary>
    TrueColor
}
