using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Detects ansi escape sequences in strings that have been read from
///     the terminal (see <see cref="IAnsiResponseParser"/>).
///     Handles CSI key parsing such as <c>\x1b[3;5~</c> (Delete with Ctrl)
/// </summary>
public class CsiKeyPattern : AnsiKeyboardParserPattern
{
    private readonly Regex _pattern = new (@"^\u001b\[(\d+)(?:;(\d+(?::\d+)*))?~$");

    private readonly Dictionary<int, Key> _keyCodeMap = new ()
    {
        { 1, Key.Home }, // Home (modern variant)
        { 4, Key.End }, // End (modern variant)
        { 5, Key.PageUp },
        { 6, Key.PageDown },
        { 2, Key.InsertChar },
        { 3, Key.Delete },
        { 11, Key.F1 },
        { 12, Key.F2 },
        { 13, Key.F3 },
        { 14, Key.F4 },
        { 15, Key.F5 },
        { 17, Key.F6 },
        { 18, Key.F7 },
        { 19, Key.F8 },
        { 20, Key.F9 },
        { 21, Key.F10 },
        { 23, Key.F11 },
        { 24, Key.F12 }
    };

    /// <inheritdoc/>
    public override bool IsMatch (string? input) => _pattern.IsMatch (input!);

    /// <inheritdoc/>
    protected override Key? GetKeyImpl (string? input)
    {
        Match match = _pattern.Match (input!);

        if (!match.Success)
        {
            return null;
        }

        // Group 1: Key code (e.g. 3, 17, etc.)
        // Group 2: Optional modifier code (e.g. 2 = Shift, 5 = Ctrl)

        if (!int.TryParse (match.Groups [1].Value, out int keyCode))
        {
            return null;
        }

        if (!_keyCodeMap.TryGetValue (keyCode, out Key? key))
        {
            return null;
        }

        // See https://github.com/tui-cs/Terminal.Gui/issues/5067
        Debug.Assert (!key.Handled);

        // If there's no modifier, just return the key.
        string modifierField = match.Groups [2].Value;

        if (string.IsNullOrEmpty (modifierField))
        {
            return new Key (key);
        }

        return ApplyModifiersAndEventType (modifierField, new Key (key));
    }
}
