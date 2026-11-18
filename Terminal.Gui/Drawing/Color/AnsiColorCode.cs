// ReSharper disable InconsistentNaming
namespace Terminal.Gui.Drawing;

/// <summary>
///     The 16 foreground color codes used by ANSI Esc sequences for 256 color terminals. Add 10 to these values for
///     background color.
/// </summary>
public enum AnsiColorCode
{
    /// <summary>The ANSI color code for Black.</summary>
    BLACK = 30,

    /// <summary>The ANSI color code for Red.</summary>
    RED = 31,

    /// <summary>The ANSI color code for Green.</summary>
    GREEN = 32,

    /// <summary>The ANSI color code for Yellow.</summary>
    YELLOW = 33,

    /// <summary>The ANSI color code for Blue.</summary>
    BLUE = 34,

    /// <summary>The ANSI color code for Magenta.</summary>
    MAGENTA = 35,

    /// <summary>The ANSI color code for Cyan.</summary>
    CYAN = 36,

    /// <summary>The ANSI color code for White.</summary>
    WHITE = 37,

    /// <summary>The ANSI color code for Bright Black.</summary>
    BRIGHT_BLACK = 90,

    /// <summary>The ANSI color code for Bright Red.</summary>
    BRIGHT_RED = 91,

    /// <summary>The ANSI color code for Bright Green.</summary>
    BRIGHT_GREEN = 92,

    /// <summary>The ANSI color code for Bright Yellow.</summary>
    BRIGHT_YELLOW = 93,

    /// <summary>The ANSI color code for Bright Blue.</summary>
    BRIGHT_BLUE = 94,

    /// <summary>The ANSI color code for Bright Magenta.</summary>
    BRIGHT_MAGENTA = 95,

    /// <summary>The ANSI color code for Bright Cyan.</summary>
    BRIGHT_CYAN = 96,

    /// <summary>The ANSI color code for Bright White.</summary>
    BRIGHT_WHITE = 97
}
