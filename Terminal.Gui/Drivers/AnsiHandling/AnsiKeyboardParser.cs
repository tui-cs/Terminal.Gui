namespace Terminal.Gui.Drivers;

/// <summary>
///     Parses ANSI escape sequence strings that describe keyboard activity into <see cref="Key"/>.
/// </summary>
public class AnsiKeyboardParser
{
    private readonly List<AnsiKeyboardParserPattern> _patterns =
    [
        new Ss3Pattern (),
        new KittyKeyboardPattern (),
        new CsiKeyPattern (),
        new CsiCursorPattern (),
        new EscAsAltPattern { IsLastMinute = true }
    ];

    /// <summary>
    ///     Maximum input length for keyboard escape sequences. Real keyboard sequences are short
    ///     (typically under 20 characters). This guard prevents pattern evaluation against
    ///     pathologically large inputs accumulated by the parser.
    /// </summary>
    internal const int MaxKeyboardSequenceLength = 64;

    /// <summary>
    ///     Looks for any pattern that matches the <paramref name="input"/> and returns
    ///     the matching pattern or <see langword="null"/> if no matches.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="isLastMinute"></param>
    /// <returns></returns>
    public AnsiKeyboardParserPattern? IsKeyboard (string? input, bool isLastMinute = false)
    {
        if (input is null || input.Length > MaxKeyboardSequenceLength)
        {
            return null;
        }

        return _patterns.FirstOrDefault (pattern => pattern.IsLastMinute == isLastMinute && pattern.IsMatch (input));
    }
}
