namespace Terminal.Gui.Drivers;

/// <summary>Windows Specific Terminal Cursor Visibility settings.</summary>
/// <remarks>
///     Hex value are set as 0xAABBCCDD where : AA stand for the TERMINFO DECSUSR parameter value to be used under
///     Linux and MacOS BB stand for the NCurses curs_set parameter value to be used under Linux and MacOS CC stand for the
///     CONSOLE_CURSOR_INFO.bVisible parameter value to be used under Windows DD stand for the CONSOLE_CURSOR_INFO.dwSize
///     parameter value to be used under Windows
/// </remarks>
public enum CursorVisibility
{
    /// <summary>Cursor caret has default</summary>
    /// <remarks>
    ///     Works under Xterm-like terminal otherwise this is equivalent to <see ref="Underscore"/>. This default directly
    ///     depends on the XTerm user configuration settings, so it could be Block, I-Beam, Underline with possible blinking.
    /// </remarks>
    Default = 0x00010119,

    /// <summary>Cursor caret is hidden</summary>
    Invisible = 0x03000019,

    /// <summary>Cursor caret is normally shown as a blinking underline bar _</summary>
    Underline = 0x03010119,

    /// <summary>Cursor caret is normally shown as a underline bar _</summary>
    /// <remarks>Under Windows, this is equivalent to <see ref="UnderscoreBlinking"/></remarks>
    UnderlineFix = 0x04010119,

    /// <summary>Cursor caret is displayed a blinking vertical bar |</summary>
    /// <remarks>Works under Xterm-like terminal otherwise this is equivalent to <see ref="Underscore"/></remarks>
    Vertical = 0x05010119,

    /// <summary>Cursor caret is displayed a blinking vertical bar |</summary>
    /// <remarks>Works under Xterm-like terminal otherwise this is equivalent to <see ref="Underscore"/></remarks>
    VerticalFix = 0x06010119,

    /// <summary>Cursor caret is displayed as a blinking block ▉</summary>
    Box = 0x01020164,

    /// <summary>Cursor caret is displayed a block ▉</summary>
    /// <remarks>Works under Xterm-like terminal otherwise this is equivalent to <see ref="Block"/></remarks>
    BoxFix = 0x02020164
}
