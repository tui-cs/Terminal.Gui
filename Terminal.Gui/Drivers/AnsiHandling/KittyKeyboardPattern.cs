using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Parses kitty keyboard protocol CSI <c>u</c> sequences into the <see cref="Key"/> model.
///     Extracts event type (press/repeat/release) and maps it to <see cref="KeyEventType"/>.
/// </summary>
public class KittyKeyboardPattern : AnsiKeyboardParserPattern
{
    private readonly Regex _pattern = new (@"^\u001b\[(\d+)(?::(\d*))?(?::(\d*))?(?:;([^;u]*))?(?:;([^u]+))?u$");

    private readonly Dictionary<int, Key> _functionalKeyMap = new ()
    {
        { 27, Key.Esc },
        { 9, Key.Tab },
        { 13, Key.Enter },
        { 127, Key.Backspace },
        { 57344, Key.CursorUp },
        { 57345, Key.CursorDown },
        { 57346, Key.CursorLeft },
        { 57347, Key.CursorRight },
        { 57348, Key.PageUp },
        { 57349, Key.PageDown },
        { 57350, Key.Home },
        { 57351, Key.End },
        { 57352, Key.InsertChar },
        { 57353, Key.Delete },
        { 57354, Key.Clear },
        { 57361, Key.PrintScreen },
        { 57364, Key.F1 },
        { 57365, Key.F2 },
        { 57366, Key.F3 },
        { 57367, Key.F4 },
        { 57368, Key.F5 },
        { 57369, Key.F6 },
        { 57370, Key.F7 },
        { 57371, Key.F8 },
        { 57372, Key.F9 },
        { 57373, Key.F10 },
        { 57374, Key.F11 },
        { 57375, Key.F12 },
        { 57376, Key.F13 },
        { 57377, Key.F14 },
        { 57378, Key.F15 },
        { 57379, Key.F16 },
        { 57380, Key.F17 },
        { 57381, Key.F18 },
        { 57382, Key.F19 },
        { 57383, Key.F20 },
        { 57384, Key.F21 },
        { 57385, Key.F22 },
        { 57386, Key.F23 },
        { 57387, Key.F24 },
        { 57417, Key.CursorLeft },
        { 57418, Key.CursorRight },
        { 57419, Key.CursorUp },
        { 57420, Key.CursorDown },
        { 57421, Key.PageUp },
        { 57422, Key.PageDown },
        { 57423, Key.Home },
        { 57424, Key.End },
        { 57425, Key.InsertChar },
        { 57426, Key.Delete }
    };

    /// <inheritdoc/>
    public override bool IsMatch (string? input) => !string.IsNullOrEmpty (input) && _pattern.IsMatch (input);

    /// <inheritdoc/>
    protected override Key? GetKeyImpl (string? input)
    {
        Match match = _pattern.Match (input!);

        if (!match.Success)
        {
            return null;
        }

        if (!int.TryParse (match.Groups [1].Value, CultureInfo.InvariantCulture, out int kittyCode))
        {
            return null;
        }

        Key? key = MapKey (kittyCode);

        if (key is null)
        {
            return null;
        }

        string baseKeyCode = "";

        if (key.KeyCode < KeyCode.CharMask && (key.KeyCode & (KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask)) == 0)
        {
            baseKeyCode = new Rune ((uint)key.KeyCode).ToString ();
        }

        // Extract alternate key codes (kitty flag 4: report alternate keys)
        KeyCode shiftedKeyCode = KeyCode.Null;
        KeyCode baseLayoutKeyCode = KeyCode.Null;

        if (match.Groups [2].Success && int.TryParse (match.Groups [2].Value, CultureInfo.InvariantCulture, out int shiftedCode) && shiftedCode > 0)
        {
            shiftedKeyCode = (KeyCode)shiftedCode;
        }

        if (match.Groups [3].Success && int.TryParse (match.Groups [3].Value, CultureInfo.InvariantCulture, out int baseCode) && baseCode > 0)
        {
            baseLayoutKeyCode = (KeyCode)baseCode;
        }

        string associatedText = string.Empty;

        if (match.Groups [5].Success)
        {
            associatedText = ParseAssociatedText (match.Groups [5].Value);
        }

        if (shiftedKeyCode != KeyCode.Null || baseLayoutKeyCode != KeyCode.Null || !string.IsNullOrEmpty (associatedText))
        {
            key = new Key (key) { ShiftedKeyCode = shiftedKeyCode, BaseLayoutKeyCode = baseLayoutKeyCode, AssociatedText = associatedText };
        }

        string originalModifierField = match.Groups [4].Value;
        string modifierField = ApplyImplicitModifierState (key, originalModifierField);

        if (!string.IsNullOrEmpty (modifierField))
        {
            (key, modifierField) = NormalizeShiftedPrintableKey (key, modifierField);
        }

        if (!string.IsNullOrEmpty (modifierField))
        {
            key = ApplyModifiersAndEventType (MaxModifierFieldValue (originalModifierField, modifierField), key);
        }

        if ((key.IsAlt || key.IsCtrl) && (key.ShiftedKeyCode != KeyCode.Null || baseKeyCode.Equals (key.AssociatedText, StringComparison.OrdinalIgnoreCase)) && !string.IsNullOrEmpty (key.AssociatedText))
        {
            key = new Key (key) { AssociatedText = string.Empty };
        }

        return key;
    }

    private static string ParseAssociatedText (string textField)
    {
        if (string.IsNullOrEmpty (textField))
        {
            return string.Empty;
        }

        string [] codePoints = textField.Split (':', StringSplitOptions.RemoveEmptyEntries);

        if (codePoints.Length == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new ();

        foreach (string codePoint in codePoints)
        {
            if (!int.TryParse (codePoint, CultureInfo.InvariantCulture, out int value) || !Rune.IsValid (value))
            {
                return string.Empty;
            }

            builder.Append (new Rune (value).ToString ());
        }

        return builder.ToString ();
    }

    private static string ApplyImplicitModifierState (Key key, string modifierField)
    {
        if (!key.IsModifierOnly)
        {
            return modifierField;
        }

        int implicitEncodedModifiers = key.ModifierKey switch
        {
            ModifierKey.Shift or ModifierKey.LeftShift or ModifierKey.RightShift => 2,
            ModifierKey.Ctrl or ModifierKey.LeftCtrl or ModifierKey.RightCtrl => 5,
            ModifierKey.Alt or ModifierKey.LeftAlt or ModifierKey.RightAlt or ModifierKey.AltGr => 3,
            _ => 1
        };

        if (string.IsNullOrEmpty (modifierField))
        {
            return implicitEncodedModifiers.ToString (CultureInfo.InvariantCulture);
        }

        string [] parts = modifierField.Split (':');

        // Check for release event BEFORE parsing modifiers, to handle case where modifierField is just the event type
        bool isRelease = parts.Length > 1 && parts [1] == "3";

        if (!int.TryParse (parts [0], CultureInfo.InvariantCulture, out int encodedModifiers) || encodedModifiers < 1)
        {
            parts [0] = implicitEncodedModifiers.ToString (CultureInfo.InvariantCulture);

            return string.Join (':', parts);
        }

        // If it's a release event, preserve the event type and don't try to merge implicit modifiers
        if (isRelease)
        {
            // For release events of modifier-only keys, ensure explicit modifiers are correct
            int explicitModifiers = encodedModifiers - 1;
            int implicitModifiers = implicitEncodedModifiers - 1;

            // Only merge modifiers if the explicit modifiers don't already match the implicit ones
            if (explicitModifiers != implicitModifiers)
            {
                parts [0] = ((explicitModifiers | implicitModifiers) + 1).ToString (CultureInfo.InvariantCulture);
            }

            return string.Join (':', parts);
        }

        int explicitModifiersPress = encodedModifiers - 1;
        int implicitModifiersPress = implicitEncodedModifiers - 1;
        parts [0] = ((explicitModifiersPress | implicitModifiersPress) + 1).ToString (CultureInfo.InvariantCulture);

        return string.Join (':', parts);
    }

    private static string MaxModifierFieldValue (string modifierField1, string modifierField2)
    {
        if (string.IsNullOrEmpty (modifierField1))
        {
            return modifierField2;
        }

        if (string.IsNullOrEmpty (modifierField2))
        {
            return modifierField1;
        }

        string [] parts1 = modifierField1.Split (':');
        string [] parts2 = modifierField2.Split (':');

        if (parts1.Length == 0 || parts2.Length == 0)
        {
            return modifierField1;
        }

        if (!int.TryParse (parts1 [0], CultureInfo.InvariantCulture, out int encodedModifiers1)
            || !int.TryParse (parts2 [0], CultureInfo.InvariantCulture, out int encodedModifiers2))
        {
            return modifierField1;
        }

        int maxEncodedModifiers = Math.Max (encodedModifiers1, encodedModifiers2);
        parts1 [0] = maxEncodedModifiers.ToString (CultureInfo.InvariantCulture);

        return string.Join (':', parts1);
    }

    private static (Key Key, string ModifierField) NormalizeShiftedPrintableKey (Key key, string modifierField)
    {
        string [] parts = modifierField.Split (':');

        if (parts.Length == 0 || !int.TryParse (parts [0], CultureInfo.InvariantCulture, out int encodedModifiers) || encodedModifiers <= 1)
        {
            return (key, modifierField);
        }

        int modifiers = encodedModifiers - 1;

        if ((modifiers & 0b1) == 0)
        {
            return (key, modifierField);
        }

        var printableRune = default (Rune);

        if (!string.IsNullOrEmpty (key.AssociatedText))
        {
            StringRuneEnumerator enumerator = key.AssociatedText.EnumerateRunes ();

            if (enumerator.MoveNext ())
            {
                printableRune = enumerator.Current;

                if (enumerator.MoveNext () || Rune.IsControl (printableRune))
                {
                    printableRune = default (Rune);
                }
            }
        }

        if (printableRune == default (Rune) && key.ShiftedKeyCode != KeyCode.Null)
        {
            var shiftedRune = Key.ToRune (key.ShiftedKeyCode);

            if (!Rune.IsControl (shiftedRune))
            {
                printableRune = shiftedRune;
            }
        }

        if (printableRune == default (Rune))
        {
            return (key, modifierField);
        }

        Key printableKey = new (printableRune.Value)
        {
            ModifierKey = key.ModifierKey,
            ShiftedKeyCode = key.ShiftedKeyCode,
            BaseLayoutKeyCode = key.BaseLayoutKeyCode,
            AssociatedText = key.AssociatedText
        };

        int normalizedEncodedModifiers = encodedModifiers - 1;
        parts [0] = normalizedEncodedModifiers.ToString (CultureInfo.InvariantCulture);

        return (printableKey, string.Join (':', parts));
    }

    /// <summary>
    ///     Maps kitty modifier key codepoints to <see cref="ModifierKey"/> values.
    /// </summary>
    private static readonly Dictionary<int, ModifierKey> _modifierKeyMap = new ()
    {
        { 57358, ModifierKey.CapsLock },
        { 57359, ModifierKey.ScrollLock },
        { 57360, ModifierKey.NumLock },
        { 57441, ModifierKey.LeftShift },
        { 57442, ModifierKey.LeftCtrl },
        { 57443, ModifierKey.LeftAlt },
        { 57444, ModifierKey.LeftSuper },
        { 57445, ModifierKey.LeftHyper },

        // 57446 = Left Meta (mapped to Alt for Terminal.Gui)
        { 57447, ModifierKey.RightShift },
        { 57448, ModifierKey.RightCtrl },
        { 57449, ModifierKey.RightAlt },

        // 57453 = ISO_Level3_Shift (AltGr). Treat it as a dedicated modifier so
        // standalone AltGr does not fall through as a printable Private Use Area rune.
        { 57453, ModifierKey.AltGr },
        { 57450, ModifierKey.RightSuper },
        { 57451, ModifierKey.RightHyper }

        // 57452 = Right Meta (mapped to Alt for Terminal.Gui)
    };

    private Key? MapKey (int kittyCode)
    {
        if (_functionalKeyMap.TryGetValue (kittyCode, out Key? functionalKey))
        {
            // See https://github.com/tui-cs/Terminal.Gui/issues/5067
            Debug.Assert (!functionalKey.Handled);

            return new Key (functionalKey);
        }

        if (_modifierKeyMap.TryGetValue (kittyCode, out ModifierKey modifierKey))
        {
            return new Key { ModifierKey = modifierKey };
        }

        if (!Rune.IsValid (kittyCode))
        {
            return null;
        }

        return new Key (kittyCode);
    }
}
