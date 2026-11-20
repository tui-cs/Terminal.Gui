using System.Diagnostics;
using System.Globalization;
using System.Text;

// ReSharper disable UnusedMember.Global

// ReSharper disable CommentTypo
// ReSharper disable GrammarMistakeInComment

// ReSharper disable InconsistentNaming

namespace Terminal.Gui.Drivers;

/// <summary>
///     Provides a platform-independent API for managing ANSI escape sequences.
/// </summary>
/// <remarks>
///     Useful resources:
///     * https://learn.microsoft.com/en-us/windows/console/console-virtual-terminal-sequences
///     * https://invisible-island.net/xterm/ctlseqs/ctlseqs.html
///     * https://vt100.net/
/// </remarks>
public static class EscSeqUtils
{
    /// <summary>
    ///     Escape key code (ASCII 27/0x1B).
    /// </summary>
    public const char KeyEsc = (char)KeyCode.Esc;

    /// <summary>
    ///     ESC [ - The CSI (Control Sequence Introducer).
    /// </summary>
    public const string CSI = "\u001B[";

    #region Screen Window Buffer

    /// <summary>
    ///     Options for ANSI ESC "[xJ" - Clears part of the screen.
    /// </summary>
    public enum ClearScreenOptions
    {
        /// <summary>
        ///     If n is 0 (or missing), clear from cursor to end of screen.
        /// </summary>
        CursorToEndOfScreen = 0,

        /// <summary>
        ///     If n is 1, clear from cursor to beginning of the screen.
        /// </summary>
        CursorToBeginningOfScreen = 1,

        /// <summary>
        ///     If n is 2, clear entire screen (and moves cursor to upper left on DOS ANSI.SYS).
        /// </summary>
        EntireScreen = 2,

        /// <summary>
        ///     If n is 3, clear entire screen and delete all lines saved in the scrollback buffer
        /// </summary>
        EntireScreenAndScrollbackBuffer = 3
    }

    /// <summary>
    ///     ESC [ ? 1047 h - Activate xterm alternative buffer (no backscroll)
    /// </summary>
    /// <remarks>
    ///     From
    ///     https://invisible-island.net/xterm/ctlseqs/ctlseqs.html#h3-Functions-using-CSI-_-ordered-by-the-final-character_s_
    ///     Use Alternate Screen Buffer, xterm.
    /// </remarks>
    public static readonly string CSI_ActivateAltBufferNoBackscroll = CSI + "?1047h";

    /// <summary>
    ///     ESC [ ? 1047 l - Restore xterm working buffer (with backscroll)
    /// </summary>
    /// <remarks>
    ///     From
    ///     https://invisible-island.net/xterm/ctlseqs/ctlseqs.html#h3-Functions-using-CSI-_-ordered-by-the-final-character_s_
    ///     Use Normal Screen Buffer, xterm.  Clear the screen first if in the Alternate Screen Buffer.
    /// </remarks>
    public static readonly string CSI_RestoreAltBufferWithBackscroll = CSI + "?1047l";

    /// <summary>
    ///     ESC [ ? 1049 l - Restore cursor position and restore xterm working buffer (with backscroll)
    /// </summary>
    /// <remarks>
    ///     From
    ///     https://invisible-island.net/xterm/ctlseqs/ctlseqs.html#h3-Functions-using-CSI-_-ordered-by-the-final-character_s_
    ///     Use Normal Screen Buffer and restore cursor as in DECRC, xterm.
    ///     resource.This combines the effects of the 1047 and 1048  modes.
    /// </remarks>
    public static readonly string CSI_RestoreCursorAndRestoreAltBufferWithBackscroll = CSI + "?1049l";

    /// <summary>
    ///     ESC [ ? 1049 h - Save cursor position and activate xterm alternative buffer (no backscroll)
    /// </summary>
    /// <remarks>
    ///     From
    ///     https://invisible-island.net/xterm/ctlseqs/ctlseqs.html#h3-Functions-using-CSI-_-ordered-by-the-final-character_s_
    ///     Save cursor as in DECSC, xterm. After saving the cursor, switch to the Alternate Screen Buffer,
    ///     clearing it first.
    ///     This control combines the effects of the 1047 and 1048 modes.
    ///     Use this with terminfo-based applications rather than the 47 mode.
    /// </remarks>
    public static readonly string CSI_SaveCursorAndActivateAltBufferNoBackscroll = CSI + "?1049h";

    /// <summary>
    ///     ESC [ x J - Clears part of the screen. See <see cref="ClearScreenOptions"/>.
    /// </summary>
    /// <param name="option"></param>
    /// <returns></returns>
    public static string CSI_ClearScreen (ClearScreenOptions option) => $"{CSI}{(int)option}J";

    /// <summary>
    ///     ESC [ 8 ; height ; width t - Set Terminal Window Size
    ///     https://terminalguide.namepad.de/seq/csi_st-8/
    /// </summary>
    public static string CSI_SetTerminalWindowSize (int height, int width) => $"{CSI}8;{height};{width}t";

    #endregion Screen Window Buffer

    #region Mouse

    /// <summary>
    ///     ESC [ ? 1003 l - Disable any-event mouse tracking (mode 1003).
    /// </summary>
    /// <remarks>
    ///     Mode 1003 enables reporting of all mouse events including motion with or without buttons pressed.
    ///     This is the most comprehensive tracking mode and includes button press/release and motion events.
    /// </remarks>
    public static readonly string CSI_DisableAnyEventMouse = CSI + "?1003l";

    /// <summary>
    ///     ESC [ ? 1006 l - Disable SGR extended mouse mode (mode 1006).
    /// </summary>
    /// <remarks>
    ///     SGR mode uses decimal text format (ESC[&lt;b;x;yM/m) instead of binary encoding, providing
    ///     unlimited coordinate range and unambiguous press (M) vs release (m) distinction.
    ///     This is the preferred modern mouse reporting format.
    /// </remarks>
    public static readonly string CSI_DisableSgrExtModeMouse = CSI + "?1006l";

    /// <summary>
    ///     ESC [ ? 1015 l - Disable URXVT extended mouse mode (mode 1015).
    /// </summary>
    /// <remarks>
    ///     URXVT mode uses UTF-8 encoding for mouse coordinates, extending the coordinate range
    ///     from 223×223 (traditional mode) to 2015×2015. Largely superseded by SGR mode but
    ///     useful for terminals that support URXVT but not SGR.
    /// </remarks>
    public static readonly string CSI_DisableUrxvtExtModeMouse = CSI + "?1015l";

    /// <summary>
    ///     ESC [ ? 1003 h - Enable any-event mouse tracking (mode 1003).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Mode 1003 enables reporting of all mouse events:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Button press and release events</description>
    ///         </item>
    ///         <item>
    ///             <description>Motion events with buttons pressed (drag)</description>
    ///         </item>
    ///         <item>
    ///             <description>Motion events without buttons pressed</description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         Note: This mode controls WHICH events are reported. The format of event data is controlled
    ///         by modes 1006 (SGR) or 1015 (URXVT).
    ///     </para>
    /// </remarks>
    public static readonly string CSI_EnableAnyEventMouse = CSI + "?1003h";

    /// <summary>
    ///     ESC [ ? 1006 h - Enable SGR extended mouse mode (mode 1006).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         SGR mode provides the modern mouse reporting format with several advantages:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Decimal text format: ESC[&lt;button;x;yM (press) or ESC[&lt;button;x;ym (release)</description>
    ///         </item>
    ///         <item>
    ///             <description>Unlimited coordinate range (not constrained by byte encoding)</description>
    ///         </item>
    ///         <item>
    ///             <description>Unambiguous press/release distinction via M/m terminator</description>
    ///         </item>
    ///         <item>
    ///             <description>Human-readable and easier to parse</description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         Supported by Windows Terminal, iTerm2, xterm, and most modern terminal emulators.
    ///     </para>
    /// </remarks>
    public static readonly string CSI_EnableSgrExtModeMouse = CSI + "?1006h";

    /// <summary>
    ///     ESC [ ? 1015 h - Enable URXVT extended mouse mode (mode 1015).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         URXVT mode extends traditional mouse reporting by using UTF-8 encoding for coordinates:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Coordinate range: up to 2015×2015 (vs 223×223 in traditional mode)</description>
    ///         </item>
    ///         <item>
    ///             <description>Uses multi-byte UTF-8 sequences for position encoding</description>
    ///         </item>
    ///         <item>
    ///             <description>Maintains ESC[M format with UTF-8 encoded coordinate bytes</description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         Originally developed for rxvt-unicode terminal emulator. Largely superseded by SGR mode (1006)
    ///         but useful for backward compatibility with older terminals.
    ///     </para>
    /// </remarks>
    public static readonly string CSI_EnableUrxvtExtModeMouse = CSI + "?1015h";

    /// <summary>
    ///     Control sequence for disabling all mouse event tracking.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Disables all mouse tracking modes in this order:
    ///     </para>
    ///     <list type="number">
    ///         <item>
    ///             <description>Mode 1003 (any-event tracking) - stops reporting mouse events</description>
    ///         </item>
    ///         <item>
    ///             <description>Mode 1015 (URXVT format) - disables UTF-8 coordinate encoding</description>
    ///         </item>
    ///         <item>
    ///             <description>Mode 1006 (SGR format) - disables decimal text format</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public static readonly string CSI_DisableMouseEvents = CSI_DisableAnyEventMouse + CSI_DisableUrxvtExtModeMouse + CSI_DisableSgrExtModeMouse;

    /// <summary>
    ///     Control sequence for enabling comprehensive mouse event tracking.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Enables three mouse tracking modes simultaneously:
    ///     </para>
    ///     <list type="number">
    ///         <item>
    ///             <description>Mode 1003 (any-event) - Reports all mouse events including motion with/without buttons</description>
    ///         </item>
    ///         <item>
    ///             <description>Mode 1015 (URXVT) - UTF-8 coordinate encoding (fallback for older terminals)</description>
    ///         </item>
    ///         <item>
    ///             <description>Mode 1006 (SGR) - Modern decimal format with unlimited coordinates (preferred)</description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         When multiple format modes are enabled, modern terminals typically use the most capable format (SGR),
    ///         while older terminals fall back to URXVT or traditional encoding. This ensures broad terminal compatibility.
    ///     </para>
    ///     <para>
    ///         Note: The ANSI specification does NOT provide auto-repeat of button press events while held stationary.
    ///         You receive one press event, optional motion events (if mode 1003 is enabled), and one release event.
    ///     </para>
    /// </remarks>
    public static readonly string CSI_EnableMouseEvents = CSI_EnableAnyEventMouse + CSI_EnableUrxvtExtModeMouse + CSI_EnableSgrExtModeMouse;

    #endregion Mouse

    #region Bracketed Paste

    /// <summary>
    ///     ESC [ ? 2004 h - Enable bracketed paste mode.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When bracketed paste mode is enabled, the terminal wraps pasted content with the start
    ///         marker <see cref="CSI_BracketedPasteStart"/> (<c>ESC[200~</c>) and the end marker
    ///         <see cref="CSI_BracketedPasteEnd"/> (<c>ESC[201~</c>). This lets applications distinguish
    ///         pasted text from typed input and handle large pastes efficiently.
    ///     </para>
    ///     <para>
    ///         Supported by xterm, Windows Terminal, iTerm2, and most modern terminal emulators.
    ///     </para>
    /// </remarks>
    public static readonly string CSI_EnableBracketedPaste = CSI + "?2004h";

    /// <summary>
    ///     ESC [ ? 2004 l - Disable bracketed paste mode.
    /// </summary>
    public static readonly string CSI_DisableBracketedPaste = CSI + "?2004l";

    /// <summary>
    ///     ESC [ 200 ~ - Sequence emitted by the terminal at the start of pasted content when
    ///     bracketed paste mode is enabled via <see cref="CSI_EnableBracketedPaste"/>.
    /// </summary>
    public static readonly string CSI_BracketedPasteStart = CSI + "200~";

    /// <summary>
    ///     ESC [ 201 ~ - Sequence emitted by the terminal at the end of pasted content when
    ///     bracketed paste mode is enabled via <see cref="CSI_EnableBracketedPaste"/>.
    /// </summary>
    public static readonly string CSI_BracketedPasteEnd = CSI + "201~";

    #endregion Bracketed Paste

    #region Keyboard

    /// <summary>
    ///     Helper to set the Control key states based on the char.
    /// </summary>
    /// <param name="ch">The char value.</param>
    /// <returns></returns>
    public static ConsoleKeyInfo MapChar (char ch) => MapConsoleKeyInfo (new ConsoleKeyInfo (ch, ConsoleKey.None, false, false, false));

    /// <summary>
    ///     Ensures a console key is mapped to one that works correctly with ANSI escape sequences.
    /// </summary>
    /// <param name="consoleKeyInfo">The <see cref="ConsoleKeyInfo"/>.</param>
    /// <returns>The <see cref="ConsoleKeyInfo"/> modified.</returns>
    public static ConsoleKeyInfo MapConsoleKeyInfo (ConsoleKeyInfo consoleKeyInfo)
    {
        ConsoleKeyInfo newConsoleKeyInfo = consoleKeyInfo;
        var key = ConsoleKey.None;
        char keyChar = consoleKeyInfo.KeyChar;

        switch ((uint)keyChar)
        {
            case 0:
                if (consoleKeyInfo.Key == (ConsoleKey)64)
                { // Ctrl+Space in Windows.
                    newConsoleKeyInfo = new ConsoleKeyInfo (consoleKeyInfo.KeyChar,
                                                            ConsoleKey.Spacebar,
                                                            (consoleKeyInfo.Modifiers & ConsoleModifiers.Shift) != 0,
                                                            (consoleKeyInfo.Modifiers & ConsoleModifiers.Alt) != 0,
                                                            (consoleKeyInfo.Modifiers & ConsoleModifiers.Control) != 0);
                }
                else if (consoleKeyInfo.Key == ConsoleKey.None)
                {
                    newConsoleKeyInfo = new ConsoleKeyInfo (consoleKeyInfo.KeyChar,
                                                            ConsoleKey.Spacebar,
                                                            (consoleKeyInfo.Modifiers & ConsoleModifiers.Shift) != 0,
                                                            (consoleKeyInfo.Modifiers & ConsoleModifiers.Alt) != 0,
                                                            true);
                }

                break;

            case uint n when n is > 0 and <= KeyEsc:
                if (consoleKeyInfo is { Key: 0, KeyChar: '\u001B' })
                {
                    key = ConsoleKey.Escape;

                    newConsoleKeyInfo = new ConsoleKeyInfo (consoleKeyInfo.KeyChar,
                                                            key,
                                                            (consoleKeyInfo.Modifiers & ConsoleModifiers.Shift) != 0,
                                                            (consoleKeyInfo.Modifiers & ConsoleModifiers.Alt) != 0,
                                                            (consoleKeyInfo.Modifiers & ConsoleModifiers.Control) != 0);
                }
                else if (consoleKeyInfo is { Key: 0, KeyChar: '\t' })
                {
                    key = ConsoleKey.Tab;

                    newConsoleKeyInfo = new ConsoleKeyInfo (consoleKeyInfo.KeyChar,
                                                            key,
                                                            (consoleKeyInfo.Modifiers & ConsoleModifiers.Shift) != 0,
                                                            (consoleKeyInfo.Modifiers & ConsoleModifiers.Alt) != 0,
                                                            (consoleKeyInfo.Modifiers & ConsoleModifiers.Control) != 0);
                }
                else if (consoleKeyInfo is { Key: 0, KeyChar: '\r' })
                {
                    key = ConsoleKey.Enter;

                    newConsoleKeyInfo = new ConsoleKeyInfo (consoleKeyInfo.KeyChar,
                                                            key,
                                                            (consoleKeyInfo.Modifiers & ConsoleModifiers.Shift) != 0,
                                                            (consoleKeyInfo.Modifiers & ConsoleModifiers.Alt) != 0,
                                                            (consoleKeyInfo.Modifiers & ConsoleModifiers.Control) != 0);
                }
                else if (consoleKeyInfo is { Key: 0, KeyChar: '\n' })
                {
                    key = ConsoleKey.Enter;

                    newConsoleKeyInfo = new ConsoleKeyInfo (consoleKeyInfo.KeyChar,
                                                            key,
                                                            (consoleKeyInfo.Modifiers & ConsoleModifiers.Shift) != 0,
                                                            (consoleKeyInfo.Modifiers & ConsoleModifiers.Alt) != 0,
                                                            true);
                }
                else if (consoleKeyInfo is { Key: 0, KeyChar: '\b' })
                {
                    key = ConsoleKey.Backspace;

                    newConsoleKeyInfo = new ConsoleKeyInfo (consoleKeyInfo.KeyChar,
                                                            key,
                                                            (consoleKeyInfo.Modifiers & ConsoleModifiers.Shift) != 0,
                                                            (consoleKeyInfo.Modifiers & ConsoleModifiers.Alt) != 0,
                                                            true);
                }
                else if (consoleKeyInfo.Key == 0)
                {
                    key = (ConsoleKey)(char)(consoleKeyInfo.KeyChar + (uint)ConsoleKey.A - 1);

                    newConsoleKeyInfo = new ConsoleKeyInfo (consoleKeyInfo.KeyChar,
                                                            key,
                                                            (consoleKeyInfo.Modifiers & ConsoleModifiers.Shift) != 0,
                                                            (consoleKeyInfo.Modifiers & ConsoleModifiers.Alt) != 0,
                                                            true);
                }

                break;

            case uint n when n is >= '\u001c' and <= '\u001f':
                key = (ConsoleKey)(char)(consoleKeyInfo.KeyChar + 24);

                newConsoleKeyInfo = new ConsoleKeyInfo ((char)key,
                                                        key,
                                                        (consoleKeyInfo.Modifiers & ConsoleModifiers.Shift) != 0,
                                                        (consoleKeyInfo.Modifiers & ConsoleModifiers.Alt) != 0,
                                                        true);

                break;

            case 127: // DEL
                key = ConsoleKey.Backspace;

                newConsoleKeyInfo = new ConsoleKeyInfo (consoleKeyInfo.KeyChar,
                                                        key,
                                                        (consoleKeyInfo.Modifiers & ConsoleModifiers.Shift) != 0,
                                                        (consoleKeyInfo.Modifiers & ConsoleModifiers.Alt) != 0,
                                                        (consoleKeyInfo.Modifiers & ConsoleModifiers.Control) != 0);

                break;

            default:
                //uint ck = ConsoleKeyMapping.MapKeyCodeToConsoleKey ((KeyCode)consoleKeyInfo.KeyChar, out bool isConsoleKey);

                //if (isConsoleKey)
                {
                    key = consoleKeyInfo.Key; // (ConsoleKey)ck;
                }

                newConsoleKeyInfo = new ConsoleKeyInfo (keyChar,
                                                        key,
                                                        GetShiftMod (consoleKeyInfo.Modifiers),
                                                        (consoleKeyInfo.Modifiers & ConsoleModifiers.Alt) != 0,
                                                        (consoleKeyInfo.Modifiers & ConsoleModifiers.Control) != 0);

                break;
        }

        return newConsoleKeyInfo;

        bool GetShiftMod (ConsoleModifiers modifiers)
        {
            if (consoleKeyInfo.KeyChar is >= (char)ConsoleKey.A and <= (char)ConsoleKey.Z && modifiers == ConsoleModifiers.None)
            {
                return true;
            }

            return (modifiers & ConsoleModifiers.Shift) != 0;
        }
    }

    internal static KeyCode MapKey (ConsoleKeyInfo keyInfo)
    {
        switch (keyInfo.Key)
        {
            case ConsoleKey.Multiply:
            case ConsoleKey.Add:
            case ConsoleKey.Separator:
            case ConsoleKey.Subtract:
            case ConsoleKey.Decimal:
            case ConsoleKey.Divide:
            case ConsoleKey.OemPeriod:
            case ConsoleKey.OemComma:
            case ConsoleKey.OemPlus:
            case ConsoleKey.OemMinus:
            case ConsoleKey.Packet:
            case ConsoleKey.Oem1:
            case ConsoleKey.Oem2:
            case ConsoleKey.Oem3:
            case ConsoleKey.Oem4:
            case ConsoleKey.Oem5:
            case ConsoleKey.Oem6:
            case ConsoleKey.Oem7:
            case ConsoleKey.Oem8:
            case ConsoleKey.Oem102:
                if (keyInfo.KeyChar == 0)
                {
                    // All Oem* produce a valid KeyChar and is not guaranteed to be printable ASCII, but it’s never just '\0' (null).
                    // If that happens it's because Console.ReadKey is misreporting for AltGr + non-character keys
                    // or if it's a combine key waiting for the next input which will determine the respective KeyChar.
                    // This behavior only happens on Windows and not on Unix-like systems.
                    if (keyInfo.Key != ConsoleKey.Multiply
                        && keyInfo.Key != ConsoleKey.Add
                        && keyInfo.Key != ConsoleKey.Decimal
                        && keyInfo.Key != ConsoleKey.Subtract
                        && keyInfo.Key != ConsoleKey.Divide
                        && keyInfo.Key != ConsoleKey.OemPeriod
                        && keyInfo.Key != ConsoleKey.OemComma
                        && keyInfo.Key != ConsoleKey.OemPlus
                        && keyInfo.Key != ConsoleKey.OemMinus
                        && keyInfo.Key != ConsoleKey.Oem1
                        && keyInfo.Key != ConsoleKey.Oem2
                        && keyInfo.Key != ConsoleKey.Oem3
                        && keyInfo.Key != ConsoleKey.Oem4
                        && keyInfo.Key != ConsoleKey.Oem5
                        && keyInfo.Key != ConsoleKey.Oem6
                        && keyInfo.Key != ConsoleKey.Oem7
                        && keyInfo.Key != ConsoleKey.Oem102)
                    {
                        // If the keyChar is 0, keyInfo.Key value is not a printable character.
                        Debug.Assert (keyInfo.Key == 0);
                    }

                    return KeyCode.Null; // MapToKeyCodeModifiers (keyInfo.Modifiers, KeyCode)keyInfo.Key);
                }

                if (keyInfo.Modifiers != ConsoleModifiers.Shift)
                {
                    // If Shift wasn't down we don't need to do anything but return the keyInfo.KeyChar
                    return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers, (KeyCode)keyInfo.KeyChar);
                }

                // Strip off Shift - We got here because they KeyChar from Windows is the shifted char (e.g. "Ç")
                // and passing on Shift would be redundant.
                return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers & ~ConsoleModifiers.Shift, (KeyCode)keyInfo.KeyChar);
        }

        // Handle control keys whose VK codes match the related ASCII value (those below ASCII 33) like ESC
        if (keyInfo.Key != ConsoleKey.None && Enum.IsDefined (typeof (KeyCode), (uint)keyInfo.Key))
        {
            if (keyInfo is { Modifiers: ConsoleModifiers.Control, Key: ConsoleKey.I })
            {
                return KeyCode.Tab;
            }

            return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers, (KeyCode)(uint)keyInfo.Key);
        }

        // Handle control keys (e.g. CursorUp)
        if (keyInfo.Key != ConsoleKey.None && Enum.IsDefined (typeof (KeyCode), (uint)keyInfo.Key + (uint)KeyCode.MaxCodePoint))
        {
            return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers, (KeyCode)((uint)keyInfo.Key + (uint)KeyCode.MaxCodePoint));
        }

        if ((ConsoleKey)keyInfo.KeyChar is >= ConsoleKey.A and <= ConsoleKey.Z)
        {
            // Shifted
            keyInfo = new ConsoleKeyInfo (keyInfo.KeyChar,
                                          (ConsoleKey)keyInfo.KeyChar,
                                          true,
                                          keyInfo.Modifiers.HasFlag (ConsoleModifiers.Alt),
                                          keyInfo.Modifiers.HasFlag (ConsoleModifiers.Control));
        }

        if ((ConsoleKey)keyInfo.KeyChar - 32 is >= ConsoleKey.A and <= ConsoleKey.Z)
        {
            // Unshifted
            keyInfo = new ConsoleKeyInfo (keyInfo.KeyChar,
                                          (ConsoleKey)(keyInfo.KeyChar - 32),
                                          false,
                                          keyInfo.Modifiers.HasFlag (ConsoleModifiers.Alt),
                                          keyInfo.Modifiers.HasFlag (ConsoleModifiers.Control));
        }

        if (keyInfo.Key is < ConsoleKey.A or > ConsoleKey.Z)
        {
            return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers, (KeyCode)keyInfo.KeyChar);
        }

        if (keyInfo.Modifiers.HasFlag (ConsoleModifiers.Alt) || keyInfo.Modifiers.HasFlag (ConsoleModifiers.Control))
        {
            // DotNetDriver doesn't support Shift-Ctrl/Shift-Alt combos
            return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers & ~ConsoleModifiers.Shift, (KeyCode)keyInfo.Key);
        }

        if (keyInfo.Modifiers != ConsoleModifiers.Shift)
        {
            return (KeyCode)keyInfo.Key;
        }

        // If ShiftMask is on  add the ShiftMask
        if (char.IsUpper (keyInfo.KeyChar))
        {
            return (KeyCode)keyInfo.Key | KeyCode.ShiftMask;
        }

        return (KeyCode)keyInfo.Key;
    }

    #endregion Keyboard

    #region Cursor

    //ESC [ M - RI Reverse Index – Performs the reverse operation of \n, moves cursor up one line, maintains horizontal position, scrolls buffer if necessary*

    /// <summary>
    ///     ESC [ 7 - Save Cursor Position in Memory**
    /// </summary>
    public static readonly string CSI_SaveCursorPosition = CSI + "7";

    /// <summary>
    ///     ESC [ 8 - DECSR Restore Cursor Position from Memory**
    /// </summary>
    public static readonly string CSI_RestoreCursorPosition = CSI + "8";

    //ESC [ < n > A - CUU - Cursor Up       Cursor up by < n >
    //ESC [ < n > B - CUD - Cursor Down     Cursor down by < n >
    //ESC [ < n > C - CUF - Cursor Forward  Cursor forward (Right) by < n >
    //ESC [ < n > D - CUB - Cursor Backward Cursor backward (Left) by < n >
    //ESC [ < n > E - CNL - Cursor Next Line - Cursor down < n > lines from current position
    //ESC [ < n > F - CPL - Cursor Previous Line    Cursor up < n > lines from current position
    //ESC [ < n > G - CHA - Cursor Horizontal Absolute      Cursor moves to < n > th position horizontally in the current line
    //ESC [ < n > d - VPA - Vertical Line Position Absolute Cursor moves to the < n > th position vertically in the current column

    /// <summary>
    ///     ESC [ y ; x H - CUP Cursor Position - Cursor moves to x ; y coordinate within the viewport, where x is the column
    ///     of the y line
    /// </summary>
    /// <param name="row">Origin is (1,1).</param>
    /// <param name="col">Origin is (1,1).</param>
    /// <returns></returns>
    public static string CSI_SetCursorPosition (int row, int col) => $"{CSI}{row};{col}H";

    /// <summary>
    ///     ESC [ y ; x H - CUP Cursor Position - Cursor moves to x ; y coordinate within the viewport, where x is the column
    ///     of the y line
    /// </summary>
    /// <param name="builder">StringBuilder where to append the cursor position sequence.</param>
    /// <param name="row">Origin is (1,1).</param>
    /// <param name="col">Origin is (1,1).</param>
    public static void CSI_AppendCursorPosition (StringBuilder builder, int row, int col) =>

        // InterpolatedStringHandler is composed in stack, skipping the string allocation.
        builder.Append ($"{CSI}{row};{col}H");

    /// <summary>
    ///     ESC [ y ; x H - CUP Cursor Position - Cursor moves to x ; y coordinate within the viewport, where x is the column
    ///     of the y line
    /// </summary>
    /// <param name="writer">TextWriter where to write the cursor position sequence.</param>
    /// <param name="row">Origin is (1,1).</param>
    /// <param name="col">Origin is (1,1).</param>
    public static void CSI_WriteCursorPosition (TextWriter writer, int row, int col)
    {
        const int maxInputBufferSize =

            // CSI (2) + ';' + 'H'
            4
            +

            // row + col (2x int sign + int max value)
            2
            + 20;
        Span<char> buffer = stackalloc char [maxInputBufferSize];

        if (!buffer.TryWrite (CultureInfo.InvariantCulture, $"{CSI}{row};{col}H", out int charsWritten))
        {
            var tooLongCursorPositionSequence = $"{CSI}{row};{col}H";

            throw new InvalidOperationException ($"{nameof (CSI_WriteCursorPosition)} buffer (len: {buffer.Length}) is too short for cursor position sequence '{tooLongCursorPositionSequence}' (len: {tooLongCursorPositionSequence.Length}).");
        }

        ReadOnlySpan<char> cursorPositionSequence = buffer [..charsWritten];
        writer.Write (cursorPositionSequence);
    }

    //ESC [ <y> ; <x> f - HVP     Horizontal Vertical Position* Cursor moves to<x>; <y> coordinate within the viewport, where <x> is the column of the<y> line
    //ESC [ s - ANSISYSSC       Save Cursor – Ansi.sys emulation **With no parameters, performs a save cursor operation like DECSC
    //ESC [ u - ANSISYSRC       Restore Cursor – Ansi.sys emulation **With no parameters, performs a restore cursor operation like DECRC
    //ESC [ ? 12 h - ATT160  Text Cursor Enable Blinking     Start the cursor blinking
    //ESC [ ? 12 l - ATT160  Text Cursor Disable Blinking    Stop blinking the cursor
    /// <summary>
    ///     ESC [ ? 25 h - DECTCEM Text Cursor Enable Mode Show    Show the cursor
    /// </summary>
    public static readonly string CSI_ShowCursor = CSI + "?25h";

    /// <summary>
    ///     ESC [ ? 25 l - DECTCEM Text Cursor Enable Mode Hide    Hide the cursor
    /// </summary>
    public static readonly string CSI_HideCursor = CSI + "?25l";

    //ESC [ ? 12 h - ATT160  Text Cursor Enable Blinking     Start the cursor blinking
    //ESC [ ? 12 l - ATT160  Text Cursor Disable Blinking    Stop blinking the cursor
    //ESC [ ? 25 h - DECTCEM Text Cursor Enable Mode Show    Show the cursor
    //ESC [ ? 25 l - DECTCEM Text Cursor Enable Mode Hide    Hide the cursor

    /// <summary>
    ///     ESC [ n SP q - Select Cursor Style (DECSCUSR)
    ///     https://terminalguide.namepad.de/seq/csi_sq_t_space/
    /// </summary>
    /// <param name="style"></param>
    /// <returns></returns>
    public static string CSI_SetCursorStyle (CursorStyle style) => $"{CSI}{(int)style} q";

    #endregion Cursor

    #region Colors

    /// <summary>
    ///     ESC [ (n) m - SGR - Set Graphics Rendition - Set the format of the screen and text as specified by (n)
    ///     This command is special in that the (n) position can accept between 0 and 16 parameters separated by semicolons.
    ///     When no parameters are specified, it is treated the same as a single 0 parameter.
    ///     https://terminalguide.namepad.de/seq/csi_sm/
    /// </summary>
    public static string CSI_SetGraphicsRendition (params int [] parameters) => $"{CSI}{string.Join (";", parameters)}m";

    /// <summary>
    ///     ESC [ (n) m - Uses <see cref="CSI_SetGraphicsRendition(int[])"/> to set the foreground color.
    /// </summary>
    /// <param name="code">One of the 16 color codes.</param>
    /// <returns></returns>
    public static string CSI_SetForegroundColor (AnsiColorCode code) => CSI_SetGraphicsRendition ((int)code);

    /// <summary>
    ///     ESC [ (n) m - Uses <see cref="CSI_SetGraphicsRendition(int[])"/> to set the background color.
    /// </summary>
    /// <param name="code">One of the 16 color codes.</param>
    /// <returns></returns>
    public static string CSI_SetBackgroundColor (AnsiColorCode code) => CSI_SetGraphicsRendition ((int)code + 10);

    /// <summary>
    ///     ESC[38;5;{id}m - Set foreground color (256 colors)
    /// </summary>
    public static string CSI_SetForegroundColor256 (int color) => $"{CSI}38;5;{color}m";

    /// <summary>
    ///     ESC[48;5;{id}m - Set background color (256 colors)
    /// </summary>
    public static string CSI_SetBackgroundColor256 (int color) => $"{CSI}48;5;{color}m";

    /// <summary>
    ///     ESC[38;2;{r};{g};{b}m - Set foreground color as RGB.
    /// </summary>
    public static string CSI_SetForegroundColorRGB (int r, int g, int b) => $"{CSI}38;2;{r};{g};{b}m";

    /// <summary>
    ///     ESC[38;2;{r};{g};{b}m - Append foreground color as RGB to StringBuilder.
    /// </summary>
    public static void CSI_AppendForegroundColorRGB (StringBuilder builder, int r, int g, int b) =>

        // InterpolatedStringHandler is composed in stack, skipping the string allocation.
        builder.Append ($"{CSI}38;2;{r};{g};{b}m");

    /// <summary>
    ///     ESC[48;2;{r};{g};{b}m - Set background color as RGB.
    /// </summary>
    public static string CSI_SetBackgroundColorRGB (int r, int g, int b) => $"{CSI}48;2;{r};{g};{b}m";

    /// <summary>
    ///     ESC[48;2;{r};{g};{b}m - Append background color as RGB to StringBuilder.
    /// </summary>
    public static void CSI_AppendBackgroundColorRGB (StringBuilder builder, int r, int g, int b) =>

        // InterpolatedStringHandler is composed in stack, skipping the string allocation.
        builder.Append ($"{CSI}48;2;{r};{g};{b}m");

    /// <summary>
    ///     ESC[39m - Resets the foreground color to the terminal's default.
    /// </summary>
    public static void CSI_AppendResetForegroundColor (StringBuilder builder) => builder.Append ($"{CSI}39m");

    /// <summary>
    ///     ESC[49m - Resets the background color to the terminal's default. This allows the terminal's
    ///     native background (including any transparency or acrylic effects) to show through.
    /// </summary>
    public static void CSI_AppendResetBackgroundColor (StringBuilder builder) => builder.Append ($"{CSI}49m");

    /// <summary>
    ///     ESC[0m - Resets all graphic attributes (foreground, background, bold, underline, etc.)
    ///     to the terminal's defaults (SGR 0).
    /// </summary>
    public static readonly string CSI_ResetAttributes = $"{CSI}0m";

    #endregion Colors

    #region Text Styles

    /// <summary>
    ///     Appends an ANSI SGR (Select Graphic Rendition) escape sequence to switch printed text from one
    ///     <see cref="TextStyle"/> to another.
    /// </summary>
    /// <param name="output"><see cref="StringBuilder"/> to add escape sequence to.</param>
    /// <param name="prev">Previous <see cref="TextStyle"/> to change away from.</param>
    /// <param name="next">Next <see cref="TextStyle"/> to change to.</param>
    /// <remarks>
    ///     <para>
    ///         Unlike colors, most text styling options are not mutually exclusive with each other, and can be applied
    ///         independently. This creates a problem when
    ///         switching from one style to another: For instance, if your previous style is just bold, and your next style is
    ///         just italic, then simply adding the
    ///         sequence to enable italic text would cause the text to remain bold. This method automatically handles this
    ///         problem, enabling and disabling styles as
    ///         necessary to apply exactly the next style.
    ///     </para>
    /// </remarks>
    internal static void CSI_AppendTextStyleChange (StringBuilder output, TextStyle prev, TextStyle next)
    {
        // Do nothing if styles are the same, as no changes are necessary.
        if (prev == next)
        {
            return;
        }

        List<int> sgr = ComputeTextStyleSgrCodes (prev, next);

        output.Append ("\x1b[");
        output.Append (string.Join (';', sgr));
        output.Append ('m');
    }

    /// <summary>
    ///     Builds the SGR escape sequence for a text style change as a string (ASCII).
    ///     Used by Utf8Buffer-based output path.
    /// </summary>
    internal static string CSI_BuildTextStyleChange (TextStyle prev, TextStyle next)
    {
        if (prev == next)
        {
            return string.Empty;
        }

        List<int> sgr = ComputeTextStyleSgrCodes (prev, next);

        return $"\x1b[{string.Join (';', sgr)}m";
    }

    /// <summary>
    ///     Computes the SGR parameter codes for a text style change.
    ///     Shared by <see cref="CSI_AppendTextStyleChange"/> and <see cref="CSI_BuildTextStyleChange"/>.
    /// </summary>
    private static List<int> ComputeTextStyleSgrCodes (TextStyle prev, TextStyle next)
    {
        // Bitwise operations to determine flag changes. A ^ B are the flags different between two flag sets. These different flags that exist in the next flag
        // set (diff & next) are the ones that were enabled in the switch, those that exist in the previous flag set (diff & prev) are the ones that were
        // disabled.
        TextStyle diff = prev ^ next;
        TextStyle enabled = diff & next;
        TextStyle disabled = diff & prev;

        // List of escape codes to apply.
        List<int> sgr = new ();

        if (disabled != TextStyle.None)
        {
            // Special case: Both bold and faint have the same disabling code. While unusual, it can be valid to have both enabled at the same time, so when
            // one and only one of them is being disabled, we need to re-enable the other afterward. We can check what flags remain enabled by taking
            // prev & next, as this is the set of flags both have.
            if (disabled.HasFlag (TextStyle.Bold))
            {
                sgr.Add (22);

                if ((prev & next).HasFlag (TextStyle.Faint))
                {
                    sgr.Add (2);
                }
            }

            if (disabled.HasFlag (TextStyle.Faint))
            {
                sgr.Add (22);

                if ((prev & next).HasFlag (TextStyle.Bold))
                {
                    sgr.Add (1);
                }
            }

            if (disabled.HasFlag (TextStyle.Italic))
            {
                sgr.Add (23);
            }

            if (disabled.HasFlag (TextStyle.Underline))
            {
                sgr.Add (24);
            }

            if (disabled.HasFlag (TextStyle.Blink))
            {
                sgr.Add (25);
            }

            if (disabled.HasFlag (TextStyle.Reverse))
            {
                sgr.Add (27);
            }

            if (disabled.HasFlag (TextStyle.Strikethrough))
            {
                sgr.Add (29);
            }
        }

        if (enabled != TextStyle.None)
        {
            if (enabled.HasFlag (TextStyle.Bold))
            {
                sgr.Add (1);
            }

            if (enabled.HasFlag (TextStyle.Faint))
            {
                sgr.Add (2);
            }

            if (enabled.HasFlag (TextStyle.Italic))
            {
                sgr.Add (3);
            }

            if (enabled.HasFlag (TextStyle.Underline))
            {
                sgr.Add (4);
            }

            if (enabled.HasFlag (TextStyle.Blink))
            {
                sgr.Add (5);
            }

            if (enabled.HasFlag (TextStyle.Reverse))
            {
                sgr.Add (7);
            }

            if (enabled.HasFlag (TextStyle.Strikethrough))
            {
                sgr.Add (9);
            }
        }

        return sgr;
    }

    #endregion Text Styles

    #region Requests

    /// <summary>
    ///     ESC [ 6 n - Request Cursor Position Report (CPR)
    ///     The terminal reply to <see cref="CSI_RequestCursorPositionReport"/>. ESC [ (y) ; (x) R
    /// </summary>
    public static readonly AnsiEscapeSequence CSI_RequestCursorPositionReport = new () { Request = CSI + "6n", Terminator = "R" };

    /// <summary>
    ///     ESC [ ? u - Query kitty keyboard progressive enhancement flags.
    ///     The terminal reply to <see cref="CSI_QueryKittyKeyboardFlags"/> is ESC [ ? flags u.
    /// </summary>
    public static readonly AnsiEscapeSequence CSI_QueryKittyKeyboardFlags = new () { Request = CSI + "?u", Terminator = "u", Value = "?" };

    /// <summary>
    ///     The kitty keyboard flags that Terminal.Gui requests when kitty keyboard protocol is supported.
    ///     Currently: disambiguate escape codes + report event types + report alternate keys
    ///     + report all keys as escape codes + report associated text.
    /// </summary>
    public const KittyKeyboardFlags KittyKeyboardRequestedFlags =
        KittyKeyboardFlags.DisambiguateEscapeCodes
        | KittyKeyboardFlags.ReportEventTypes
        | KittyKeyboardFlags.ReportAlternateKeys
        | KittyKeyboardFlags.ReportAllKeysAsEscapeCodes
        | KittyKeyboardFlags.ReportAssociatedText;

    /// <summary>
    ///     ESC [ &gt; flags u - Push current kitty keyboard flags and enable the specified flags.
    /// </summary>
    /// <param name="flags">The kitty keyboard progressive enhancement flags to enable.</param>
    /// <returns>The ANSI request string.</returns>
    public static string CSI_EnableKittyKeyboardFlags (KittyKeyboardFlags flags) => $"{CSI}>{(int)flags}u";

    /// <summary>
    ///     ESC [ &lt; u - Restore the previously pushed kitty keyboard flag state.
    /// </summary>
    public static readonly string CSI_DisableKittyKeyboardFlags = CSI + "<u";

    /// <summary>
    ///     The terminal reply to <see cref="CSI_RequestCursorPositionReport"/>. ESC [ (y) ; (x) R
    /// </summary>
    public const string CSI_RequestCursorPositionReport_Terminator = "R";

    /// <summary>
    ///     ESC [ 0 c - Send Device Attributes (Primary DA)
    ///     https://invisible-island.net/xterm/ctlseqs/ctlseqs.html#h3-Application-Program-Command-functions
    ///     https://www.xfree86.org/current/ctlseqs.html
    ///     Windows Terminal v1.17 and below emits “\x1b[?1;0c”, indicating "VT101 with No Options".
    ///     Windows Terminal v1.18+ emits: \x1b[?61;6;7;22;23;24;28;32;42c"
    ///     See https://github.com/microsoft/terminal/pull/14906
    ///     61 - The device conforms to level 1 of the character cell display architecture
    ///     (See https://github.com/microsoft/terminal/issues/15693#issuecomment-1633304497)
    ///     6 = Selective erase
    ///     7 = Soft fonts
    ///     22 = Color text
    ///     23 = Greek character sets
    ///     24 = Turkish character sets
    ///     28 = Rectangular area operations
    ///     32 = Text macros
    ///     42 = ISO Latin-2 character set
    ///     The terminator indicating a reply to <see cref="CSI_SendDeviceAttributes"/> or
    ///     <see cref="CSI_SendDeviceAttributes2"/>
    /// </summary>
    public static readonly AnsiEscapeSequence CSI_SendDeviceAttributes = new () { Request = CSI + "0c", Terminator = "c" };

    /// <summary>
    ///     ESC [ > 0 c - Send Device Attributes (Secondary DA)
    ///     Windows Terminal v1.18+ emits: "\x1b[>0;10;1c" (vt100, firmware version 1.0, vt220)
    /// </summary>
    public static readonly AnsiEscapeSequence CSI_SendDeviceAttributes2 = new () { Request = CSI + ">0c", Terminator = "c" };

    /// <summary>
    ///     CSI 16 t - Request sixel resolution (width and height in pixels)
    ///     The terminator indicating a reply to <see cref="CSI_RequestSixelResolution"/> : ESC [ 6 ; heightPx ; widthPx t
    /// </summary>
    public static readonly AnsiEscapeSequence CSI_RequestSixelResolution = new () { Request = CSI + "16t", Terminator = "t", Value = "6" };

    /// <summary>
    ///     CSI 14 t - Request window size in pixels (width x height)
    ///     The terminator indicating a reply to <see cref="CSI_RequestWindowSizeInPixels"/> : ESC [ 4 ; heightPx ; widthPx t
    /// </summary>
    public static readonly AnsiEscapeSequence CSI_RequestWindowSizeInPixels = new () { Request = CSI + "14t", Terminator = "t", Value = "4" };

    /// <summary>
    ///     CSI 18 t  | yes | yes |  yes  | report window size in chars
    ///     https://terminalguide.namepad.de/seq/csi_st-18/
    ///     The terminator indicating a reply to <see cref="CSI_ReportWindowSizeInChars"/> : ESC [ 8 ; height ; width t
    /// </summary>
    public static readonly AnsiEscapeSequence CSI_ReportWindowSizeInChars = new () { Request = CSI + "18t", Terminator = "t", Value = "8" };

    /// <summary>
    ///     Valid ANSI response terminators per CSI specification. See
    ///     https://invisible-island.net/xterm/ctlseqs/ctlseqs.html#h3-Functions-using-CSI-_-ordered-by-the-final-character_s
    ///     Note: N and O are intentionally excluded as they have special handling
    /// </summary>
    public static readonly char [] KnownTerminators =
    [
        '@', // ICH - Insert Character
        'A', // CUU - Cursor Up
        'B', // CUD - Cursor Down
        'C', // CUF - Cursor Forward (Right)
        'D', // CUB - Cursor Backward (Left)
        'E', // CNL - Cursor Next Line
        'F', // CPL - Cursor Previous Line
        'G', // CHA - Cursor Horizontal Absolute
        'H', // CUP - Cursor Position
        'I', // CHT - Cursor Horizontal Tabulation
        'J', // ED - Erase in Display
        'K', // EL - Erase in Line
        'L', // IL - Insert Line
        'M', // DL - Delete Line
        //'N', // Single Shift Select of G2 Character Set (SS2  is 0x8e), VT220. This affects next character only.
        //'O', // Single Shift Select of G3 Character Set (SS3 is 0x8f), VT220. This affects next character only.
        'P', // DCH - Delete Character
        'Q', // (Various sequences)
        'R', // CPR - Cursor Position Report
        'S', // SU - Scroll Up
        'T', // SD - Scroll Down / Mouse tracking
        'W', // CTC - Cursor Tabulation Control
        'X', // ECH - Erase Character
        'Z', // CBT - Cursor Backward Tabulation
        '^', // (Rarely used)
        '`', // HPA - Horizontal Position Absolute
        '~', // Function keys and other extended sequences
        'a', // HPR - Horizontal Position Relative
        'b', // REP - Repeat
        'c', // DA - Device Attributes
        'd', // VPA - Vertical Position Absolute
        'e', // VPR - Vertical Position Relative
        'f', // HVP - Horizontal and Vertical Position
        'g', // TBC - Tab Clear
        'h', // SM - Set Mode
        'i', // MC - Media Copy
        'l', // RM - Reset Mode
        'm', // SGR - Select Graphic Rendition
        'n', // DSR - Device Status Report
        'p', // (Various, including soft terminal reset)
        'q', // DECSCUSR - Set Cursor Style
        'r', // DECSTBM - Set Top and Bottom Margins
        's', // SCOSC - Save Cursor / DECSLRM
        't', // Window manipulation / DECSLPP
        'u', // SCORC - Restore Cursor Position
        'v', // (Rarely used)
        'w', // (Rarely used)
        'x', // DECREQTPARM - Request Terminal Parameters
        'y', // DECTST - Invoke Confidence Test
        'z' // (Rarely used)
    ];

    #endregion Requests

    #region OSC

    /// <summary>
    ///     OSC (Operating System Command) escape sequence prefix.
    /// </summary>
    /// <remarks>
    ///     OSC sequences are used for operating system-specific commands like setting window title,
    ///     hyperlinks (OSC 8), and other terminal emulator features.
    /// </remarks>
    public const string OSC = "\u001B]";

    /// <summary>
    ///     String Terminator (ST) - terminates OSC sequences.
    /// </summary>
    /// <remarks>
    ///     Can also be represented as BEL (0x07) in some terminals, but ST is more modern.
    /// </remarks>
    public const string ST = "\u001B\\";

    /// <summary>
    ///     Sets the terminal window title using OSC 0..2.
    /// </summary>
    /// <param name="title">The title text to write.</param>
    /// <param name="mode">
    ///     The OSC title selector:
    ///     0 = icon and window title, 1 = icon title, 2 = window title.
    ///     Values outside 0..2 are clamped.
    /// </param>
    /// <returns>The OSC title sequence.</returns>
    public static string OSC_SetWindowTitle (string title, int mode = 0)
    {
        string safeTitle = SanitizeOscText (title);

        return $"{OSC}{Math.Clamp (mode, 0, 2)};{safeTitle}{ST}";
    }

    /// <summary>
    ///     Starts a hyperlink using OSC 8 escape sequence.
    /// </summary>
    /// <param name="url">The URL to link to (e.g., "https://github.com").</param>
    /// <param name="id">Optional hyperlink ID for matching start/end pairs. Use null for automatic matching.</param>
    /// <returns>The OSC 8 start sequence.</returns>
    /// <remarks>
    ///     OSC 8 format: ESC ] 8 ; params ; URL ST
    ///     Supported in Windows Terminal, iTerm2, and other modern terminals.
    ///     Must be followed by visible text, then terminated with <see cref="OSC_EndHyperlink"/>.
    /// </remarks>
    public static string OSC_StartHyperlink (string url, string? id = null)
    {
        // Format: ESC ] 8 ; params ; URL ST
        // params can include "id=value" for matching start/end
        string parameters = string.IsNullOrEmpty (id) ? "" : $"id={id}";

        return $"{OSC}8;{parameters};{url}{ST}";
    }

    /// <summary>
    ///     Removes control characters from OSC text payloads to prevent escape-sequence injection.
    /// </summary>
    private static string SanitizeOscText (string text)
    {
        if (string.IsNullOrEmpty (text))
        {
            return string.Empty;
        }

        StringBuilder sanitized = new (text.Length);

        foreach (char ch in text)
        {
            if (!char.IsControl (ch))
            {
                sanitized.Append (ch);
            }
        }

        return sanitized.ToString ();
    }

    /// <summary>
    ///     Ends a hyperlink using OSC 8 escape sequence.
    /// </summary>
    /// <returns>The OSC 8 end sequence.</returns>
    /// <remarks>
    ///     This terminates the hyperlink started by <see cref="OSC_StartHyperlink"/>.
    ///     Format: ESC ] 8 ; ; ST (empty URL ends the hyperlink).
    /// </remarks>
    public static string OSC_EndHyperlink () =>

        // Format: ESC ] 8 ; ; ST (empty URL ends hyperlink)
        $"{OSC}8;;{ST}";

    /// <summary>
    ///     Clears any terminal progress indicator using OSC 9;4 state 0.
    /// </summary>
    /// <returns>The OSC 9;4 clear-progress sequence.</returns>
    public static string OSC_ClearProgress () => $"{OSC}9;4;0;0{ST}";

    /// <summary>
    ///     Sets terminal progress using OSC 9;4 state 1.
    /// </summary>
    /// <param name="progress">Progress percentage in the range 0-100.</param>
    /// <returns>The OSC 9;4 determinate-progress sequence.</returns>
    public static string OSC_SetProgressValue (int progress) => $"{OSC}9;4;1;{Math.Clamp (progress, 0, 100)}{ST}";

    /// <summary>
    ///     Sets terminal progress to the error state using OSC 9;4 state 2.
    /// </summary>
    /// <param name="progress">Progress percentage in the range 0-100.</param>
    /// <returns>The OSC 9;4 error-progress sequence.</returns>
    public static string OSC_SetProgressError (int progress = 0) => $"{OSC}9;4;2;{Math.Clamp (progress, 0, 100)}{ST}";

    /// <summary>
    ///     Sets terminal progress to the indeterminate state using OSC 9;4 state 3.
    /// </summary>
    /// <returns>The OSC 9;4 indeterminate-progress sequence.</returns>
    public static string OSC_SetProgressIndeterminate () => $"{OSC}9;4;3;0{ST}";

    /// <summary>
    ///     Sets terminal progress to the paused state using OSC 9;4 state 4.
    /// </summary>
    /// <param name="progress">Progress percentage in the range 0-100.</param>
    /// <returns>The OSC 9;4 paused-progress sequence.</returns>
    public static string OSC_SetProgressPaused (int progress = 0) => $"{OSC}9;4;4;{Math.Clamp (progress, 0, 100)}{ST}";

    #region OSC Color Queries

    /// <summary>
    ///     OSC 10 - Queries the terminal's default foreground color.
    ///     Response format: ESC ] 10 ; rgb:RRRR/GGGG/BBBB ST (or BEL terminated).
    /// </summary>
    public static readonly AnsiEscapeSequence OSC_QueryForegroundColor = new () { Request = $"{OSC}10;?{ST}", Terminator = ST, Value = "10" };

    /// <summary>
    ///     OSC 11 - Queries the terminal's default background color.
    ///     Response format: ESC ] 11 ; rgb:RRRR/GGGG/BBBB ST (or BEL terminated).
    /// </summary>
    public static readonly AnsiEscapeSequence OSC_QueryBackgroundColor = new () { Request = $"{OSC}11;?{ST}", Terminator = ST, Value = "11" };

    /// <summary>
    ///     Attempts to parse an OSC 10/11 color response into a <see cref="Color"/>.
    /// </summary>
    /// <param name="response">
    ///     The raw response string, e.g. <c>ESC]10;rgb:RRRR/GGGG/BBBB ESC\</c> or <c>ESC]11;rgb:RR/GG/BB BEL</c>.
    /// </param>
    /// <param name="color">The parsed color, or <see langword="null"/> if parsing fails.</param>
    /// <returns><see langword="true"/> if the response was successfully parsed.</returns>
    /// <remarks>
    ///     Handles both 4-digit (16-bit) and 2-digit (8-bit) per-channel hex values.
    ///     For 16-bit values, the high byte is used (e.g. <c>RRRR</c> → <c>RR</c>).
    /// </remarks>
    public static bool TryParseOscColorResponse (string? response, out Color? color)
    {
        color = null;

        if (string.IsNullOrEmpty (response))
        {
            return false;
        }

        // Find the rgb: prefix
        int rgbIndex = response.IndexOf ("rgb:", StringComparison.OrdinalIgnoreCase);

        if (rgbIndex < 0)
        {
            return false;
        }

        // Extract the color portion between "rgb:" and the terminator (ST or BEL)
        string colorPart = response [(rgbIndex + 4)..];

        // Strip trailing terminators: ST (ESC\) or BEL (\a)
        if (colorPart.EndsWith (ST, StringComparison.Ordinal))
        {
            colorPart = colorPart [..^ST.Length];
        }
        else if (colorPart.Length > 0 && colorPart [^1] == '\a')
        {
            colorPart = colorPart [..^1];
        }

        // Parse R/G/B components separated by '/'
        string [] parts = colorPart.Split ('/');

        if (parts.Length != 3)
        {
            return false;
        }

        if (!TryParseHexComponent (parts [0], out int r) || !TryParseHexComponent (parts [1], out int g) || !TryParseHexComponent (parts [2], out int b))
        {
            return false;
        }

        color = new Color (r, g, b);

        return true;

        static bool TryParseHexComponent (string hex, out int value)
        {
            value = 0;

            if (hex.Length is not (2 or 4) || !int.TryParse (hex, NumberStyles.HexNumber, null, out int parsed))
            {
                return false;
            }

            // For 16-bit values (4 hex digits), take the high byte
            value = hex.Length == 4 ? parsed >> 8 : parsed;

            return true;
        }
    }

    #endregion OSC Color Queries

    #endregion OSC

    /// <summary>
    ///     Convert a <see cref="ConsoleKeyInfo"/> array to string.
    /// </summary>
    /// <param name="consoleKeyInfos"></param>
    /// <returns>The string representing the array.</returns>
    public static string ToString (ConsoleKeyInfo [] consoleKeyInfos)
    {
        StringBuilder sb = new ();

        foreach (ConsoleKeyInfo keyChar in consoleKeyInfos)
        {
            sb.Append (keyChar.KeyChar);
        }

        return sb.ToString ();
    }
}
