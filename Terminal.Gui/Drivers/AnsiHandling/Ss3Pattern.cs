using System.Text.RegularExpressions;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Parser for SS3 terminal escape sequences. These describe specific keys e.g.
///     <c>EscOP</c> is F1.
/// </summary>
public class Ss3Pattern : AnsiKeyboardParserPattern
{
#pragma warning disable IDE1006 // Naming Styles
    private static readonly Regex _pattern = new(@"^\u001bO([PQRStDCABOHFwqysu])$");
#pragma warning restore IDE1006 // Naming Styles

    /// <inheritdoc/>
    public override bool IsMatch (string? input) => _pattern.IsMatch (input!);

    /// <summary>
    ///     Returns the ss3 key that corresponds to the provided input escape sequence
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    protected override Key? GetKeyImpl (string? input)
    {
        Match match = _pattern.Match (input!);

        if (!match.Success)
        {
            return null;
        }

        return match.Groups [1].Value.Single () switch
        {
            'P' => Key.F1,
            'Q' => Key.F2,
            'R' => Key.F3,
            'S' => Key.F4,
            't' => Key.F5,
            'D' => Key.CursorLeft,
            'C' => Key.CursorRight,
            'A' => Key.CursorUp,
            'B' => Key.CursorDown,
            'H' => Key.Home,
            'F' => Key.End,
            'w' => Key.Home,
            'q' => Key.End,
            'y' => Key.PageUp,
            's' => Key.PageDown,
            'u' => Key.Clear,
            _ => null
        };
    }
}
