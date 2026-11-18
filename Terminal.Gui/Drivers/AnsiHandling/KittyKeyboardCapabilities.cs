namespace Terminal.Gui.Drivers;

/// <summary>
///     Describes the kitty keyboard protocol capabilities discovered from the terminal.
/// </summary>
public class KittyKeyboardCapabilities
{
    /// <summary>
    ///     Gets or sets whether the terminal responded to the kitty keyboard protocol query. If <see langword="true"/>
    ///     the terminal supports the kitty keyboard protocol. This does not necessarily indicate that any particular flags are supported,
    ///     or that the support has been enabled, only that the terminal responded to the query and is therefore capable of supporting the protocol.
    /// </summary>
    public bool IsSupported { get; set; }

    /// <summary>
    ///     Gets or sets the kitty keyboard flags reported by the terminal as enabled. <see cref="KittyKeyboardFlags.None"/>
    ///     is a valid value indicating that no kitty keyboard flags were reported as enabled.
    /// </summary>
    public KittyKeyboardFlags Flags { get; set; }
}
