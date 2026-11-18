namespace Terminal.Gui.Drivers;

/// <summary>
///     Identifies ANSI startup queries tracked by <see cref="IAnsiStartupGate"/>.
/// </summary>
public enum AnsiStartupQuery
{
    /// <summary>
    ///     Terminal size query (<c>CSI 18t</c>).
    /// </summary>
    TerminalSize,

    /// <summary>
    ///     Cursor position query (<c>CSI 6n</c>).
    /// </summary>
    CursorPosition,

    /// <summary>
    ///     Kitty keyboard capability query (<c>CSI ?u</c>).
    /// </summary>
    KittyKeyboard,

    /// <summary>
    ///     Terminal color detection queries (OSC 10/11).
    /// </summary>
    TerminalColors,

    /// <summary>
    ///     Primary device attributes query (<c>CSI 0c</c>).
    /// </summary>
    DeviceAttributesPrimary
}
