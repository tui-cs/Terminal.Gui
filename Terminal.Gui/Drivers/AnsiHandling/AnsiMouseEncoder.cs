namespace Terminal.Gui.Drivers;

/// <summary>
///     Encodes <see cref="Mouse"/> events into ANSI SGR (1006) extended mouse format escape sequences.
/// </summary>
/// <remarks>
///     <para>
///         This is the inverse operation of <see cref="AnsiMouseParser"/>. It converts Terminal.Gui
///         <see cref="Mouse"/> events back into the ANSI escape sequences that would produce them.
///         Used primarily for test input injection in drivers that consume character streams (e.g., UnixDriver).
///     </para>
///     <para>
///         The SGR format uses decimal text encoding: <c>ESC[&lt;button;x;yM</c> (press) or <c>ESC[&lt;button;x;ym</c>
///         (release).
///     </para>
/// </remarks>
public static class AnsiMouseEncoder
{
    /// <summary>
    ///     Converts a <see cref="Mouse"/> event to an ANSI SGR (1006) extended mouse format escape sequence.
    /// </summary>
    /// <param name="mouse">The mouse event to convert.</param>
    /// <returns>ANSI escape sequence string in format: <c>ESC[&lt;button;x;yM</c> or <c>ESC[&lt;button;x;ym</c></returns>
    /// <remarks>
    ///     <para>
    ///         SGR format: <c>ESC[&lt;button;x;y{M|m}</c>
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>M = press, m = release</description>
    ///         </item>
    ///         <item>
    ///             <description>Coordinates are 1-based in ANSI (Terminal.Gui uses 0-based)</description>
    ///         </item>
    ///         <item>
    ///             <description>Button codes encode both the button and modifier keys</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public static string Encode (Mouse mouse)
    {
        // SGR format: ESC[<button;x;y{M|m}
        // M = press, m = release
        // Coordinates are 1-based in ANSI

        int buttonCode = GetButtonCode (mouse.Flags);
        int x = mouse.ScreenPosition.X + 1; // Convert to 1-based
        int y = mouse.ScreenPosition.Y + 1; // Convert to 1-based
        char terminator = GetTerminator (mouse.Flags);

        return $"{EscSeqUtils.CSI}<{buttonCode};{x};{y}{terminator}";
    }

    /// <summary>
    ///     Gets the ANSI button code from <see cref="MouseFlags"/>.
    /// </summary>
    /// <param name="flags">The mouse flags to encode.</param>
    /// <returns>The ANSI SGR button code.</returns>
    /// <remarks>
    ///     <para>
    ///         This is the inverse of <see cref="AnsiMouseParser.GetFlags"/> - it converts Terminal.Gui
    ///         <see cref="MouseFlags"/> back to the ANSI SGR button code that would produce those flags.
    ///     </para>
    ///     <para>
    ///         The ANSI button code encoding is:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Base button: 0=left, 1=middle, 2=right</description>
    ///         </item>
    ///         <item>
    ///             <description>Add 32 for drag (PositionReport with button)</description>
    ///         </item>
    ///         <item>
    ///             <description>Add 64 for wheel (64=up, 65=down, 68=left, 69=right)</description>
    ///         </item>
    ///         <item>
    ///             <description>Modifiers: +4 for Shift, +8 for Alt, +16 for Ctrl</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    private static int GetButtonCode (MouseFlags flags)
    {
        // Special cases: wheel events
        // Note: WheeledLeft = Ctrl | WheeledUp, WheeledRight = Ctrl | WheeledDown
        // So we need to check for these combinations first before checking individual flags

        if (flags.FastHasFlags (MouseFlags.WheeledLeft))
        {
            // WheeledLeft is defined as Ctrl | WheeledUp, which maps to button code 68
            // The ANSI parser also adds Shift flag for code 68, but we'll let that happen naturally
            return 68;
        }

        if (flags.FastHasFlags (MouseFlags.WheeledRight))
        {
            // WheeledRight is defined as Ctrl | WheeledDown, which maps to button code 69
            // The ANSI parser also adds Shift flag for code 69, but we'll let that happen naturally
            return 69;
        }

        if (flags.FastHasFlags (MouseFlags.WheeledUp))
        {
            return 64;
        }

        if (flags.FastHasFlags (MouseFlags.WheeledDown))
        {
            return 65;
        }

        int buttonCode;

        if (flags.FastHasFlags (MouseFlags.LeftButtonPressed) || flags.FastHasFlags (MouseFlags.LeftButtonReleased))
        {
            buttonCode = 0;
        }
        else if (flags.FastHasFlags (MouseFlags.MiddleButtonPressed) || flags.FastHasFlags (MouseFlags.MiddleButtonReleased))
        {
            buttonCode = 1;
        }
        else if (flags.FastHasFlags (MouseFlags.RightButtonPressed) || flags.FastHasFlags (MouseFlags.RightButtonReleased))
        {
            buttonCode = 2;
        }
        else if (flags.FastHasFlags (MouseFlags.PositionReport))
        {
            // Motion without button
            buttonCode = 3;
        }
        else
        {
            buttonCode = 0; // Default to left
        }

        // Check if it's a drag event (position report with button pressed)
        bool isDrag = flags.FastHasFlags (MouseFlags.PositionReport)
                      && (flags.FastHasFlags (MouseFlags.LeftButtonPressed)
                          || flags.FastHasFlags (MouseFlags.MiddleButtonPressed)
                          || flags.FastHasFlags (MouseFlags.RightButtonPressed));

        if (isDrag)
        {
            // Drag events use button codes with the motion bit set.
            buttonCode += 32;
        }
        else if (flags.FastHasFlags (MouseFlags.PositionReport))
        {
            // Motion without a button uses code 35 before modifiers.
            buttonCode = 35;
        }

        // Add modifiers
        bool hasAlt = flags.FastHasFlags (MouseFlags.Alt);
        bool hasCtrl = flags.FastHasFlags (MouseFlags.Ctrl);
        bool hasShift = flags.FastHasFlags (MouseFlags.Shift);

        // Standard modifier encoding: Shift=+4, Alt=+8, Ctrl=+16
        if (hasShift)
        {
            buttonCode += 4;
        }

        if (hasAlt)
        {
            buttonCode += 8;
        }

        if (hasCtrl)
        {
            buttonCode += 16;
        }

        return buttonCode;
    }

    /// <summary>
    ///     Gets the terminator character for the ANSI mouse sequence.
    /// </summary>
    /// <param name="flags">The mouse flags.</param>
    /// <returns>M for press/wheel/motion events, m for release events.</returns>
    private static char GetTerminator (MouseFlags flags)
    {
        // Release events use 'm', press/wheel/motion use 'M'
        if (flags.FastHasFlags (MouseFlags.LeftButtonReleased)
            || flags.FastHasFlags (MouseFlags.MiddleButtonReleased)
            || flags.FastHasFlags (MouseFlags.RightButtonReleased)
            || flags.FastHasFlags (MouseFlags.Button4Released))
        {
            return 'm';
        }

        return 'M';
    }
}
