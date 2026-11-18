using System.Text.RegularExpressions;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Parses ANSI mouse escape sequences into <see cref="Mouse"/> including support for button
///     press/release, mouse wheel, and motion events.
/// </summary>
/// <remarks>
///     <para>
///         This parser handles SGR (1006) extended mouse mode format: <c>ESC[&lt;button;x;yM/m</c>
///         where 'M' indicates button press and 'm' indicates button release.
///     </para>
///     <para>
///         <b>Prerequisites:</b> The terminal must have mouse tracking enabled via
///         <see cref="EscSeqUtils.CSI_EnableMouseEvents"/>,
///         which enables modes 1003 (any-event tracking), 1015 (URXVT), and 1006 (SGR format).
///     </para>
///     <para>
///         <b>Common User Actions and ANSI Behavior:</b>
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 <b>Click:</b> Terminal sends one press event (M) at button down, one release event (m) at button up.
///                 No auto-repeat while held stationary.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <b>Drag:</b> Terminal sends one press event (M), multiple motion events with
///                 <see cref="MouseFlags.PositionReport"/>
///                 and the button flag set (e.g., button code 32-34 for drag), then one release event (m).
///             </description>
///         </item>
///         <item>
///             <description>
///                 <b>Mouse Move (no button):</b> Terminal sends motion events with button code 35-63 and
///                 <see cref="MouseFlags.PositionReport"/> flag (mode 1003 only).
///             </description>
///         </item>
///         <item>
///             <description>
///                 <b>Scroll Wheel:</b> Terminal sends single events with button codes 64 (up) or 65 (down).
///                 No press/release distinction - wheel events don't use M/m terminators.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <b>Horizontal Wheel:</b> Terminal sends button codes 68 (left) or 69 (right), typically with Shift
///                 modifier.
///             </description>
///         </item>
///     </list>
///     <para>
///         <b>Coordinate System:</b> ANSI uses 1-based coordinates where (1,1) is the top-left corner.
///         This parser converts to 0-based coordinates for Terminal.Gui's internal representation.
///     </para>
/// </remarks>
public class AnsiMouseParser
{
    // Regex patterns for button press/release, wheel scroll, and mouse position reporting
    private readonly Regex _mouseEventPattern = new (@"\u001b\[<(\d+);(\d+);(\d+)(M|m)", RegexOptions.Compiled);

    /// <summary>
    ///     Maximum input length for mouse escape sequences. Real mouse sequences are short
    ///     (typically under 20 characters). This guard prevents regex evaluation against
    ///     pathologically large inputs accumulated by the parser.
    /// </summary>
    internal const int MaxMouseSequenceLength = 64;

    /// <summary>
    ///     Returns true if it is a mouse event
    /// </summary>
    /// <param name="cur"></param>
    /// <returns></returns>
    public bool IsMouse (string? cur) =>

        // Typically in this format
        // ESC [ < {button_code};{x_pos};{y_pos}{final_byte}
        cur is { Length: <= MaxMouseSequenceLength } && (cur.EndsWith ('M') || cur.EndsWith ('m'));

    /// <summary>
    ///     Parses a mouse ansi escape sequence into a mouse event. Returns null if input
    ///     is not a mouse event or its syntax is not understood.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public Mouse? ProcessMouseInput (string? input)
    {
        if (input is null || input.Length > MaxMouseSequenceLength)
        {
            return null;
        }

        // Match mouse wheel events first
        Match match = _mouseEventPattern.Match (input);

        if (!match.Success)
        {
            return null;
        }

        int buttonCode = int.Parse (match.Groups [1].Value);

        // The top-left corner of the terminal corresponds to (1, 1) for both X (column) and Y (row) coordinates.
        // ANSI standards and terminal conventions historically treat screen positions as 1 - based.

        int x = int.Parse (match.Groups [2].Value) - 1;
        int y = int.Parse (match.Groups [3].Value) - 1;
        char terminator = match.Groups [4].Value.Single ();

        Mouse m = new () { Timestamp = DateTime.Now, ScreenPosition = new Point (x, y), Flags = GetFlags (buttonCode, terminator) };

        //Logging.Trace ($"{input} -> {m}");

        return m;
    }

    private const int ButtonMask = 0b_0000_0011;
    private const int ShiftMask = 0b_0000_0100;
    private const int AltMask = 0b_0000_1000;
    private const int CtrlMask = 0b_0001_0000;
    private const int MotionMask = 0b_0010_0000;
    private const int WheelMask = 0b_0100_0000;

    private static MouseFlags GetFlags (int buttonCode, char terminator)
    {
        MouseFlags flags = GetModifierFlags (buttonCode);

        if ((buttonCode & WheelMask) != 0)
        {
            return flags | GetWheelFlags (buttonCode);
        }

        if ((buttonCode & MotionMask) != 0)
        {
            flags |= MouseFlags.PositionReport;
        }

        return flags | GetButtonFlags (buttonCode & ButtonMask, terminator);
    }

    private static MouseFlags GetModifierFlags (int buttonCode)
    {
        MouseFlags flags = MouseFlags.None;

        if ((buttonCode & ShiftMask) != 0)
        {
            flags |= MouseFlags.Shift;
        }

        if ((buttonCode & AltMask) != 0)
        {
            flags |= MouseFlags.Alt;
        }

        if ((buttonCode & CtrlMask) != 0)
        {
            flags |= MouseFlags.Ctrl;
        }

        return flags;
    }

    private static MouseFlags GetButtonFlags (int button, char terminator)
    {
        return button switch
        {
            0 => terminator == 'M' ? MouseFlags.LeftButtonPressed : MouseFlags.LeftButtonReleased,
            1 => terminator == 'M' ? MouseFlags.MiddleButtonPressed : MouseFlags.MiddleButtonReleased,
            2 => terminator == 'M' ? MouseFlags.RightButtonPressed : MouseFlags.RightButtonReleased,
            _ => MouseFlags.None
        };
    }

    private static MouseFlags GetWheelFlags (int buttonCode)
    {
        return buttonCode switch
        {
            66 or 68 or 72 or 80 => MouseFlags.WheeledLeft,
            67 or 69 or 73 or 81 => MouseFlags.WheeledRight,
            _ => GetVerticalWheelFlags (buttonCode)
        };
    }

    private static MouseFlags GetVerticalWheelFlags (int buttonCode)
    {
        return (buttonCode & ButtonMask) switch
        {
            0 => MouseFlags.WheeledUp,
            1 => MouseFlags.WheeledDown,
            _ => MouseFlags.None
        };
    }
}
