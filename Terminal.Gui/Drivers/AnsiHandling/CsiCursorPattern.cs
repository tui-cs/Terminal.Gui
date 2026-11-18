using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Detects ansi escape sequences in strings that have been read from
///     the terminal (see <see cref="IAnsiResponseParser"/>).
///     Handles navigation CSI key parsing such as <c>\x1b[A</c> (Cursor up)
///     and <c>\x1b[1;5A</c> (Cursor/Function with modifier(s))
/// </summary>
public class CsiCursorPattern : AnsiKeyboardParserPattern
{
    private readonly Regex _pattern = new (@"^\u001b\[(?:1;(\d+(?::\d+)*))?([A-DFHPQRSZ])$");

    private readonly Dictionary<char, Key> _cursorMap = new ()
    {
        { 'A', Key.CursorUp },
        { 'B', Key.CursorDown },
        { 'C', Key.CursorRight },
        { 'D', Key.CursorLeft },
        { 'H', Key.Home },
        { 'F', Key.End },

        // F1–F4 as per xterm VT100-style CSI sequences
        { 'P', Key.F1 },
        { 'Q', Key.F2 },
        { 'R', Key.F3 },
        { 'S', Key.F4 },

        // Shift+Tab (backtab)
        { 'Z', Key.Tab.WithShift }
    };

    /// <inheritdoc/>
    public override bool IsMatch (string? input) { return _pattern.IsMatch (input!); }

    /// <summary>
    ///     Called by the base class to determine the key that matches the input.
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

        string modifierGroup = match.Groups [1].Value;
        char terminator = match.Groups [2].Value [0];

        if (!_cursorMap.TryGetValue (terminator, out Key? key))
        {
            return null;
        }

        // See https://github.com/tui-cs/Terminal.Gui/issues/5067
        Debug.Assert (!key.Handled);

        if (string.IsNullOrEmpty (modifierGroup))
        {
            return new Key (key);
        }

        return ApplyModifiersAndEventType (modifierGroup, new Key (key));
    }
}
