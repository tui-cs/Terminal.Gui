namespace Terminal.Gui.Drivers;

/// <summary>
///     Detects terminal color capabilities from environment variables such as
///     <c>TERM</c>, <c>COLORTERM</c>, <c>TERM_PROGRAM</c>, and <c>WT_SESSION</c>.
/// </summary>
public static class TerminalEnvironmentDetector
{
    /// <summary>
    ///     Reads environment variables and returns a <see cref="TerminalColorCapabilities"/>
    ///     describing the terminal's color support level.
    /// </summary>
    /// <returns>A <see cref="TerminalColorCapabilities"/> record describing the terminal.</returns>
    public static TerminalColorCapabilities DetectColorCapabilities ()
    {
        string? term = Environment.GetEnvironmentVariable ("TERM");
        string? colorTerm = Environment.GetEnvironmentVariable ("COLORTERM");
        string? termProgram = Environment.GetEnvironmentVariable ("TERM_PROGRAM");
        string? wtSession = Environment.GetEnvironmentVariable ("WT_SESSION");
        string? noColor = Environment.GetEnvironmentVariable ("NO_COLOR");

        bool isWindowsTerminal = !string.IsNullOrEmpty (wtSession);

        ColorCapabilityLevel capability = DetermineCapability (term, colorTerm, isWindowsTerminal, noColor);

        return new TerminalColorCapabilities
        {
            Term = term,
            ColorTerm = colorTerm,
            TermProgram = termProgram,
            IsWindowsTerminal = isWindowsTerminal,
            Capability = capability
        };
    }

    private static ColorCapabilityLevel DetermineCapability (string? term, string? colorTerm, bool isWindowsTerminal, string? noColor)
    {
        // NO_COLOR convention: https://no-color.org/
        if (noColor is { })
        {
            return ColorCapabilityLevel.NoColor;
        }

        // TERM=dumb means no color support
        if (string.Equals (term, "dumb", StringComparison.OrdinalIgnoreCase))
        {
            return ColorCapabilityLevel.NoColor;
        }

        // COLORTERM=truecolor or 24bit explicitly indicates TrueColor
        if (string.Equals (colorTerm, "truecolor", StringComparison.OrdinalIgnoreCase)
            || string.Equals (colorTerm, "24bit", StringComparison.OrdinalIgnoreCase))
        {
            return ColorCapabilityLevel.TrueColor;
        }

        // Windows Terminal always supports TrueColor
        if (isWindowsTerminal)
        {
            return ColorCapabilityLevel.TrueColor;
        }

        // Linux console supports 16 colors
        if (string.Equals (term, "linux", StringComparison.OrdinalIgnoreCase))
        {
            return ColorCapabilityLevel.Colors16;
        }

        // *-256color TERM values
        if (term is { } && term.EndsWith ("-256color", StringComparison.OrdinalIgnoreCase))
        {
            return ColorCapabilityLevel.Colors256;
        }

        // Default: assume TrueColor for modern terminals
        return ColorCapabilityLevel.TrueColor;
    }
}
