namespace Terminal.Gui.Drivers;

/// <summary>
///     Describes the current state of an <see cref="IAnsiResponseParser"/>
/// </summary>
public enum AnsiResponseParserState
{
    /// <summary>
    ///     Parser is reading normal input e.g. keys typed by user.
    /// </summary>
    Normal,

    /// <summary>
    ///     Parser has encountered an Esc and is waiting to see if next
    ///     key(s) continue to form an Ansi escape sequence (typically '[' but
    ///     also other characters e.g. O for SS3).
    /// </summary>
    ExpectingEscapeSequence,

    /// <summary>
    ///     Parser has encountered Esc[ and considers that it is in the process
    ///     of reading an ANSI sequence.
    /// </summary>
    InResponse,

    /// <summary>
    ///     Parser has encountered the bracketed-paste start sequence (<c>ESC[200~</c>) and is
    ///     accumulating pasted content until the matching end sequence (<c>ESC[201~</c>).
    /// </summary>
    InBracketedPaste,

    /// <summary>
    ///     Parser has already delivered the truncated prefix of an oversized bracketed paste and is
    ///     discarding the remaining bytes until the matching end sequence (<c>ESC[201~</c>) arrives.
    /// </summary>
    DiscardingBracketedPasteRemainder
}
