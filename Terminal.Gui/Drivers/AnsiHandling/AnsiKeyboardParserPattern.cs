using System.Diagnostics;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Base class for ANSI keyboard parsing patterns.
/// </summary>
public abstract class AnsiKeyboardParserPattern
{
    /// <summary>
    ///     Does this pattern dangerously overlap with other sequences
    ///     such that it should only be applied at the lsat second after
    ///     all other sequences have been tried.
    ///     <remarks>
    ///         When <see langword="true"/> this pattern will only be used
    ///         at <see cref="AnsiResponseParser.Release"/> time.
    ///     </remarks>
    /// </summary>
    public bool IsLastMinute { get; set; }

    /// <summary>
    ///     Returns <see langword="true"/> if <paramref name="input"/> is one
    ///     of the terminal sequences recognised by this class.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public abstract bool IsMatch (string? input);

    private readonly string _name;

    /// <summary>
    ///     Creates a new instance of the class.
    /// </summary>
    protected AnsiKeyboardParserPattern () => _name = GetType ().Name;

    /// <summary>
    ///     Returns the <see cref="Key"/> described by the escape sequence.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public Key? GetKey (string? input)
    {
        Key? key = GetKeyImpl (input);

        // See https://github.com/tui-cs/Terminal.Gui/issues/5067
        Debug.Assert (key is { Handled: false });

        // Create a copy just to be safe; the patterns are supposed to create new Key instances,
        // but we don't want to accidentally share references
        // See https://github.com/tui-cs/Terminal.Gui/issues/5067
        return new Key (key);
    }

    /// <summary>
    ///     When overriden in a derived class, returns the <see cref="Key"/>
    ///     that matches the input ansi escape sequence.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    protected abstract Key? GetKeyImpl (string? input);

    /// <summary>
    ///     Parses a kitty-extended modifier field that may contain an event type suffix.
    ///     The field format is <c>modifiers</c> or <c>modifiers:event_type</c>.
    /// </summary>
    /// <param name="modifierField">The raw modifier field string (e.g. "5" or "1:3").</param>
    /// <param name="key">The base key to apply modifiers to.</param>
    /// <returns>The key with modifiers and event type applied, or <see langword="null"/> if parsing fails.</returns>
    protected static Key ApplyModifiersAndEventType (string modifierField, Key key)
    {
        string [] parts = modifierField.Split (':');
        string modifierToken = parts [0];

        if (!int.TryParse (modifierToken, System.Globalization.CultureInfo.InvariantCulture, out int encodedModifiers))
        {
            return key;
        }

        int modifiers = Math.Max (0, encodedModifiers - 1);

        if ((modifiers & 0b1) != 0)
        {
            key = key.WithShift;
        }

        if ((modifiers & 0b10) != 0)
        {
            key = key.WithAlt;
        }

        if ((modifiers & 0b100) != 0)
        {
            key = key.WithCtrl;
        }

        // Extract event type from the modifier field: modifiers:event_type
        // Kitty values: 1=press, 2=repeat, 3=release (matching KeyEventType enum values)
        KeyEventType eventType = KeyEventType.Press;

        if (parts.Length > 1
            && int.TryParse (parts [1], System.Globalization.CultureInfo.InvariantCulture, out int kittyEventType)
            && kittyEventType is >= 1 and <= 3)
        {
            eventType = (KeyEventType)kittyEventType;
        }

        if (eventType != KeyEventType.Press)
        {
            key = new Key (key) { EventType = eventType };
        }

        return key;
    }
}
