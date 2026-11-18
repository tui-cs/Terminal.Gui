namespace Terminal.Gui.Drivers;

/// <summary>
///     Encodes <see cref="Key"/> objects into ANSI escape sequences.
/// </summary>
/// <remarks>
///     This is the inverse operation of <see cref="AnsiKeyboardParser"/>. It converts Terminal.Gui
///     <see cref="Key"/> objects back into the ANSI escape sequences that would produce them.
///     Used primarily for test input injection in drivers that consume character streams (e.g., UnixDriver).
/// </remarks>
public static class AnsiKeyboardEncoder
{
    /// <summary>
    ///     Converts a <see cref="Key"/> to its ANSI escape sequence representation or character.
    /// </summary>
    /// <param name="key">The key to encode.</param>
    /// <returns>
    ///     An ANSI escape sequence string for special keys (arrows, function keys, etc.),
    ///     or a character string for regular characters.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         For special keys (arrows, function keys, etc.), this returns the appropriate
    ///         ANSI escape sequence. For regular characters, it returns the character itself.
    ///     </para>
    ///     <para>
    ///         Alt combinations are represented by prefixing the character/sequence with ESC.
    ///         Ctrl combinations for letters A-Z are represented as ASCII control codes (0x01-0x1A).
    ///         Shift affects letter case but not control key behavior.
    ///     </para>
    ///     <para>
    ///         Note: Certain modifier combinations cannot be represented in ANSI (e.g., Ctrl+Shift
    ///         combinations produce the same control code as Ctrl alone).
    ///     </para>
    /// </remarks>
    public static string Encode (Key key)
    {
        // Strip modifiers to get base key
        KeyCode baseKeyCode = key.KeyCode & ~(KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask);

        // Check if it's a special key that needs an ANSI sequence
        string? ansiSeq = GetSpecialKeySequence (baseKeyCode);

        if (ansiSeq != null)
        {
            // For Alt-only on special keys, use traditional ESC prefix for better compatibility
            // (e.g., ESC ESC[A instead of ESC[1;3A)
            if (key.IsAlt && !key.IsShift && !key.IsCtrl)
            {
                return $"{EscSeqUtils.KeyEsc}{ansiSeq}";
            }

            // Calculate ANSI modifier parameter (1-based, used when Shift/Ctrl are present)
            // Modifier values: 2=Shift, 3=Alt, 4=Shift+Alt, 5=Ctrl, 6=Ctrl+Shift, 7=Ctrl+Alt, 8=Ctrl+Shift+Alt
            int modifier = GetAnsiModifierParameter (key);

            // If modifiers are present (Shift, Ctrl, or combinations)
            if (modifier <= 1)
            {
                return ansiSeq;
            }

            // Check if the sequence uses CSI format (starts with ESC[)
            if (ansiSeq.StartsWith (EscSeqUtils.CSI, StringComparison.Ordinal))
            {
                // Insert modifier parameter into CSI sequence
                // Format: ESC [ <num> ; <modifier> ~ or ESC [ 1 ; <modifier> <letter>
                ansiSeq = InsertModifierIntoSequence (ansiSeq, modifier);
            }
            else
            {
                // For SS3 format (ESC O) or other non-CSI sequences with modifiers,
                // convert to CSI format with modifier
                // F1-F4: ESC O P/Q/R/S ? ESC [ 1 ; <mod> P/Q/R/S
                ansiSeq = ConvertSS3ToCSIWithModifier (ansiSeq, modifier);
            }

            return ansiSeq;

        }

        // Handle Ctrl combinations for letters (Ctrl takes precedence over Alt)
        if (key.IsCtrl && baseKeyCode is >= KeyCode.A and <= KeyCode.Z)
        {
            // Ctrl+A = 0x01, Ctrl+B = 0x02, etc.
            var ctrlChar = (char)(baseKeyCode - KeyCode.A + 1);

            // If Alt is also pressed, prefix with ESC
            if (key.IsAlt)
            {
                return $"{EscSeqUtils.KeyEsc}{ctrlChar}";
            }

            return ctrlChar.ToString ();
        }

        // For regular characters, use the character value
        if (baseKeyCode < (KeyCode)128)
        {
            var ch = (char)baseKeyCode;

            // KeyCode.A through KeyCode.Z are uppercase by definition
            // If shift is NOT pressed, convert to lowercase
            if (ch is >= 'A' and <= 'Z' && !key.IsShift)
            {
                ch = char.ToLower (ch);
            }

            // Handle Alt combinations by prefixing with ESC
            if (key.IsAlt)
            {
                return $"{EscSeqUtils.KeyEsc}{ch}";
            }

            return ch.ToString ();
        }

        // Fallback: use the ConsoleKeyMapping for complex cases
        ConsoleKeyInfo consoleKeyInfo = ConsoleKeyMapping.GetConsoleKeyInfoFromKeyCode (key.KeyCode);

        return consoleKeyInfo.KeyChar.ToString ();
    }

    /// <summary>
    ///     Gets the ANSI escape sequence for special keys (non-character keys).
    /// </summary>
    /// <param name="keyCode">The base key code (without modifiers).</param>
    /// <returns>The ANSI sequence string, or null if the key is not a special key.</returns>
    private static string? GetSpecialKeySequence (KeyCode keyCode) =>
        keyCode switch
        {
            // Cursor movement keys - CSI sequences
            KeyCode.CursorUp => $"{EscSeqUtils.CSI}A",
            KeyCode.CursorDown => $"{EscSeqUtils.CSI}B",
            KeyCode.CursorRight => $"{EscSeqUtils.CSI}C",
            KeyCode.CursorLeft => $"{EscSeqUtils.CSI}D",
            KeyCode.Home => $"{EscSeqUtils.CSI}H",
            KeyCode.End => $"{EscSeqUtils.CSI}F",

            // Function keys F1-F4 use SS3 format (ESC O)
            KeyCode.F1 => $"{EscSeqUtils.KeyEsc}OP",
            KeyCode.F2 => $"{EscSeqUtils.KeyEsc}OQ",
            KeyCode.F3 => $"{EscSeqUtils.KeyEsc}OR",
            KeyCode.F4 => $"{EscSeqUtils.KeyEsc}OS",

            // Function keys F5-F12 use CSI format with tilde terminator
            KeyCode.F5 => $"{EscSeqUtils.CSI}15~",
            KeyCode.F6 => $"{EscSeqUtils.CSI}17~",
            KeyCode.F7 => $"{EscSeqUtils.CSI}18~",
            KeyCode.F8 => $"{EscSeqUtils.CSI}19~",
            KeyCode.F9 => $"{EscSeqUtils.CSI}20~",
            KeyCode.F10 => $"{EscSeqUtils.CSI}21~",
            KeyCode.F11 => $"{EscSeqUtils.CSI}23~",
            KeyCode.F12 => $"{EscSeqUtils.CSI}24~",

            // Editing keys - CSI format with tilde terminator
            KeyCode.Insert => $"{EscSeqUtils.CSI}2~",
            KeyCode.Delete => $"{EscSeqUtils.CSI}3~",
            KeyCode.PageUp => $"{EscSeqUtils.CSI}5~",
            KeyCode.PageDown => $"{EscSeqUtils.CSI}6~",

            // Special characters
            KeyCode.Tab => "\t",
            KeyCode.Enter => "\r",
            KeyCode.Backspace => "\x7F", // DEL (127)
            KeyCode.Esc => $"{EscSeqUtils.KeyEsc}",

            _ => null
        };

    /// <summary>
    ///     Calculates the ANSI modifier parameter for a key.
    /// </summary>
    /// <param name="key">The key with modifiers.</param>
    /// <returns>
    ///     The modifier parameter value:
    ///     1 = no modifiers, 2 = Shift, 3 = Alt, 4 = Shift+Alt,
    ///     5 = Ctrl, 6 = Ctrl+Shift, 7 = Ctrl+Alt, 8 = Ctrl+Shift+Alt
    /// </returns>
    private static int GetAnsiModifierParameter (Key key)
    {
        var modifier = 1; // 1 = no modifiers

        if (key.IsShift)
        {
            modifier += 1;
        }

        if (key.IsAlt)
        {
            modifier += 2;
        }

        if (key.IsCtrl)
        {
            modifier += 4;
        }

        return modifier;
    }

    /// <summary>
    ///     Inserts the modifier parameter into a CSI sequence.
    /// </summary>
    /// <param name="sequence">The base CSI sequence (e.g., "ESC[17~" for F6).</param>
    /// <param name="modifier">The modifier parameter (2-8).</param>
    /// <returns>The sequence with modifier inserted (e.g., "ESC[17;2~" for Shift+F6).</returns>
    private static string InsertModifierIntoSequence (string sequence, int modifier)
    {
        // CSI sequences have format: ESC [ <number> <terminator>
        // We need to insert ;<modifier> before the terminator
        // Examples:
        //   ESC[17~ ? ESC[17;2~ (F6 ? Shift+F6)
        //   ESC[A ? ESC[1;2A (Up ? Shift+Up)

        int csiLength = EscSeqUtils.CSI.Length; // Length of "ESC["

        // Find the terminator (last character)
        char terminator = sequence [^1];

        // Extract the numeric part (if any)
        string numberPart = sequence.Substring (csiLength, sequence.Length - csiLength - 1);

        // If there's no number, default to "1"
        if (string.IsNullOrEmpty (numberPart) || !char.IsDigit (numberPart [0]))
        {
            numberPart = "1";
        }

        return $"{EscSeqUtils.CSI}{numberPart};{modifier}{terminator}";
    }

    /// <summary>
    ///     Converts an SS3 sequence (ESC O) to CSI format (ESC [) with modifier.
    /// </summary>
    /// <param name="sequence">The SS3 sequence (e.g., "ESC OP" for F1).</param>
    /// <param name="modifier">The modifier parameter (2-8).</param>
    /// <returns>The CSI sequence with modifier (e.g., "ESC[1;2P" for Shift+F1).</returns>
    private static string ConvertSS3ToCSIWithModifier (string sequence, int modifier)
    {
        // SS3 format: ESC O <letter>
        // Convert to: ESC [ 1 ; <modifier> <letter>
        // Example: ESC O P (F1) ? ESC [ 1 ; 2 P (Shift+F1)

        if (sequence is not [EscSeqUtils.KeyEsc, 'O', _, ..])
        {
            return sequence;
        }
        char letter = sequence [2];

        return $"{EscSeqUtils.CSI}1;{modifier}{letter}";

        // Fallback: return as-is
    }
}
