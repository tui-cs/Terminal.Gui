namespace Terminal.Gui.Drivers;

/// <summary>
///     Defines the style of the terminal cursor, based on ANSI/VT terminal standards.
/// </summary>
/// <remarks>
///     <para>
///         This enum follows the ANSI/VT DECSCUSR (CSI Ps SP q) sequence standard where Ps indicates:
///         0 = implementation defined (usually blinking block)
///         1 = blinking block
///         2 = steady block
///         3 = blinking underline
///         4 = steady underline
///         5 = blinking bar (vertical I-beam)
///         6 = steady bar (vertical I-beam)
///     </para>
///     <para>
///         Drivers map these values to platform-specific APIs:
///         - ANSI terminals: Use DECSCUSR escape sequences directly
///         - Windows Console: Map to CONSOLE_CURSOR_INFO (bVisible and dwSize)
///     </para>
///     <para>
///         To hide the cursor, use null for the cursor position.
///     </para>
/// </remarks>
public enum CursorStyle
{
    /// <summary>
    ///     The default cursor shape, typically a blinking block.
    /// </summary>
    Default = 0,

    /// <summary>Blinking block cursor (default for most terminals).</summary>
    /// <remarks>ANSI DECSCUSR Ps=1 or Ps=0.</remarks>
    BlinkingBlock = 1,

    /// <summary>Steady (non-blinking) block cursor.</summary>
    /// <remarks>ANSI DECSCUSR Ps=2.</remarks>
    SteadyBlock = 2,

    /// <summary>Blinking underline cursor.</summary>
    /// <remarks>ANSI DECSCUSR Ps=3.</remarks>
    BlinkingUnderline = 3,

    /// <summary>Steady (non-blinking) underline cursor.</summary>
    /// <remarks>ANSI DECSCUSR Ps=4.</remarks>
    SteadyUnderline = 4,

    /// <summary>Blinking vertical bar cursor (I-beam, commonly used in text editors).</summary>
    /// <remarks>ANSI DECSCUSR Ps=5.</remarks>
    BlinkingBar = 5,

    /// <summary>Steady (non-blinking) vertical bar cursor (I-beam).</summary>
    /// <remarks>ANSI DECSCUSR Ps=6.</remarks>
    SteadyBar = 6,

    /// <summary>
    ///     Represents a hidden cursor (no visible cursor).
    /// </summary>
    Hidden = -1
}
