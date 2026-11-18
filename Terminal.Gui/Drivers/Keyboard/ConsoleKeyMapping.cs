namespace Terminal.Gui.Drivers;

/// <summary>Helper class to handle mapping between <see cref="KeyCode"/> and <see cref="ConsoleKeyInfo"/>.</summary>
public static class ConsoleKeyMapping
{
    /// <summary>
    ///     Gets a <see cref="ConsoleKeyInfo"/> from a <see cref="KeyCode"/>.
    /// </summary>
    /// <param name="key">The key code to convert.</param>
    /// <returns>A ConsoleKeyInfo representing the key.</returns>
    /// <remarks>
    ///     This method is primarily used for test simulation via <see cref="IKeyConverter{T}.ToKeyInfo"/>.
    ///     It produces a keyboard-layout-agnostic "best effort" ConsoleKeyInfo suitable for testing.
    ///     For shifted characters (e.g., Shift+2), the character returned is US keyboard layout (Shift+2 = '@').
    ///     This is acceptable for test simulation but may not match the user's actual keyboard layout.
    /// </remarks>
    public static ConsoleKeyInfo GetConsoleKeyInfoFromKeyCode (KeyCode key)
    {
        ConsoleModifiers modifiers = MapToConsoleModifiers (key);
        KeyCode keyWithoutModifiers = key & ~KeyCode.CtrlMask & ~KeyCode.ShiftMask & ~KeyCode.AltMask;

        // Map to ConsoleKey enum
        (ConsoleKey consoleKey, char keyChar) = MapToConsoleKeyAndChar (keyWithoutModifiers, modifiers);

        return new (
                    keyChar,
                    consoleKey,
                    modifiers.HasFlag (ConsoleModifiers.Shift),
                    modifiers.HasFlag (ConsoleModifiers.Alt),
                    modifiers.HasFlag (ConsoleModifiers.Control)
                   );
    }

    /// <summary>Gets <see cref="ConsoleModifiers"/> from <see cref="bool"/> modifiers.</summary>
    /// <param name="shift">The shift key.</param>
    /// <param name="alt">The alt key.</param>
    /// <param name="control">The control key.</param>
    /// <returns>The console modifiers.</returns>
    public static ConsoleModifiers GetModifiers (bool shift, bool alt, bool control)
    {
        var modifiers = new ConsoleModifiers ();

        if (shift)
        {
            modifiers |= ConsoleModifiers.Shift;
        }

        if (alt)
        {
            modifiers |= ConsoleModifiers.Alt;
        }

        if (control)
        {
            modifiers |= ConsoleModifiers.Control;
        }

        return modifiers;
    }

    /// <summary>Maps a <see cref="ConsoleKeyInfo"/> to a <see cref="KeyCode"/>.</summary>
    /// <param name="consoleKeyInfo">The console key.</param>
    /// <returns>The <see cref="KeyCode"/> or the <paramref name="consoleKeyInfo"/>.</returns>
    public static KeyCode MapConsoleKeyInfoToKeyCode (ConsoleKeyInfo consoleKeyInfo)
    {
        KeyCode keyCode;

        switch (consoleKeyInfo.Key)
        {
            case ConsoleKey.Enter:
                keyCode = KeyCode.Enter;

                break;
            case ConsoleKey.Delete:
                keyCode = KeyCode.Delete;

                break;
            case ConsoleKey.UpArrow:
                keyCode = KeyCode.CursorUp;

                break;
            case ConsoleKey.DownArrow:
                keyCode = KeyCode.CursorDown;

                break;
            case ConsoleKey.LeftArrow:
                keyCode = KeyCode.CursorLeft;

                break;
            case ConsoleKey.RightArrow:
                keyCode = KeyCode.CursorRight;

                break;
            case ConsoleKey.PageUp:
                keyCode = KeyCode.PageUp;

                break;
            case ConsoleKey.PageDown:
                keyCode = KeyCode.PageDown;

                break;
            case ConsoleKey.Home:
                keyCode = KeyCode.Home;

                break;
            case ConsoleKey.End:
                keyCode = KeyCode.End;

                break;
            case ConsoleKey.Insert:
                keyCode = KeyCode.Insert;

                break;
            case ConsoleKey.F1:
                keyCode = KeyCode.F1;

                break;
            case ConsoleKey.F2:
                keyCode = KeyCode.F2;

                break;
            case ConsoleKey.F3:
                keyCode = KeyCode.F3;

                break;
            case ConsoleKey.F4:
                keyCode = KeyCode.F4;

                break;
            case ConsoleKey.F5:
                keyCode = KeyCode.F5;

                break;
            case ConsoleKey.F6:
                keyCode = KeyCode.F6;

                break;
            case ConsoleKey.F7:
                keyCode = KeyCode.F7;

                break;
            case ConsoleKey.F8:
                keyCode = KeyCode.F8;

                break;
            case ConsoleKey.F9:
                keyCode = KeyCode.F9;

                break;
            case ConsoleKey.F10:
                keyCode = KeyCode.F10;

                break;
            case ConsoleKey.F11:
                keyCode = KeyCode.F11;

                break;
            case ConsoleKey.F12:
                keyCode = KeyCode.F12;

                break;
            case ConsoleKey.F13:
                keyCode = KeyCode.F13;

                break;
            case ConsoleKey.F14:
                keyCode = KeyCode.F14;

                break;
            case ConsoleKey.F15:
                keyCode = KeyCode.F15;

                break;
            case ConsoleKey.F16:
                keyCode = KeyCode.F16;

                break;
            case ConsoleKey.F17:
                keyCode = KeyCode.F17;

                break;
            case ConsoleKey.F18:
                keyCode = KeyCode.F18;

                break;
            case ConsoleKey.F19:
                keyCode = KeyCode.F19;

                break;
            case ConsoleKey.F20:
                keyCode = KeyCode.F20;

                break;
            case ConsoleKey.F21:
                keyCode = KeyCode.F21;

                break;
            case ConsoleKey.F22:
                keyCode = KeyCode.F22;

                break;
            case ConsoleKey.F23:
                keyCode = KeyCode.F23;

                break;
            case ConsoleKey.F24:
                keyCode = KeyCode.F24;

                break;
            case ConsoleKey.Clear:
                keyCode = KeyCode.Clear;

                break;
            case ConsoleKey.Tab:
                keyCode = KeyCode.Tab;

                break;
            case ConsoleKey.Spacebar:
                keyCode = KeyCode.Space;

                break;
            case ConsoleKey.Backspace:
                keyCode = KeyCode.Backspace;

                break;
            default:
                if ((int)consoleKeyInfo.KeyChar is >= 1 and <= 26)
                {
                    keyCode = (KeyCode)(consoleKeyInfo.KeyChar + 64);
                }
                else
                {
                    keyCode = (KeyCode)consoleKeyInfo.KeyChar;
                }

                break;
        }

        keyCode |= MapToKeyCodeModifiers (consoleKeyInfo.Modifiers, keyCode);

        return keyCode;
    }

    /// <summary>Map existing <see cref="KeyCode"/> modifiers to <see cref="ConsoleModifiers"/>.</summary>
    /// <param name="key">The key code.</param>
    /// <returns>The console modifiers.</returns>
    public static ConsoleModifiers MapToConsoleModifiers (KeyCode key)
    {
        var modifiers = new ConsoleModifiers ();

        // BUGFIX: Only set Shift if ShiftMask is explicitly set.
        // KeyCode.A-Z (65-90) represent UNSHIFTED keys, even though their numeric values
        // match uppercase ASCII characters. Do NOT check char.IsUpper!
        if (key.FastHasFlags (KeyCode.ShiftMask))
        {
            modifiers |= ConsoleModifiers.Shift;
        }

        if (key.FastHasFlags (KeyCode.AltMask))
        {
            modifiers |= ConsoleModifiers.Alt;
        }

        if (key.FastHasFlags (KeyCode.CtrlMask))
        {
            modifiers |= ConsoleModifiers.Control;
        }

        return modifiers;
    }

    /// <summary>Maps a <see cref="ConsoleKeyInfo"/> to a <see cref="KeyCode"/>.</summary>
    /// <param name="modifiers">The console modifiers.</param>
    /// <param name="key">The key code.</param>
    /// <returns>The <see cref="KeyCode"/> with <see cref="ConsoleModifiers"/> or the <paramref name="key"/></returns>
    public static KeyCode MapToKeyCodeModifiers (ConsoleModifiers modifiers, KeyCode key)
    {
        var keyMod = new KeyCode ();

        if ((modifiers & ConsoleModifiers.Shift) != 0)
        {
            keyMod = KeyCode.ShiftMask;
        }

        if ((modifiers & ConsoleModifiers.Control) != 0)
        {
            keyMod |= KeyCode.CtrlMask;
        }

        if ((modifiers & ConsoleModifiers.Alt) != 0)
        {
            keyMod |= KeyCode.AltMask;
        }

        return keyMod != KeyCode.Null ? keyMod | key : key;
    }

    /// <summary>
    ///     Maps a KeyCode to its corresponding ConsoleKey and character representation.
    /// </summary>
    private static (ConsoleKey consoleKey, char keyChar) MapToConsoleKeyAndChar (KeyCode key, ConsoleModifiers modifiers)
    {
        var keyValue = (uint)key;

        // Check if this is a special key (value > MaxCodePoint means it's offset by MaxCodePoint)
        if (keyValue > (uint)KeyCode.MaxCodePoint)
        {
            var specialKey = (ConsoleKey)(keyValue - (uint)KeyCode.MaxCodePoint);

            // Special keys don't have printable characters
            char specialChar = specialKey switch
            {
                ConsoleKey.Enter => '\r',
                ConsoleKey.Tab => '\t',
                ConsoleKey.Escape => '\u001B',
                ConsoleKey.Backspace => '\b',
                ConsoleKey.Spacebar => ' ',
                _ => '\0' // Function keys, arrows, etc. have no character
            };

            return (specialKey, specialChar);
        }

        // Handle letter keys (A-Z)
        if (keyValue >= (uint)KeyCode.A && keyValue <= (uint)KeyCode.Z)
        {
            var letterKey = (ConsoleKey)keyValue;
            var letterChar = (char)('a' + (keyValue - (uint)KeyCode.A));

            if (modifiers.HasFlag (ConsoleModifiers.Shift))
            {
                letterChar = char.ToUpper (letterChar);
            }

            return (letterKey, letterChar);
        }

        // Handle number keys (D0-D9) with US keyboard layout
        if (keyValue >= (uint)KeyCode.D0 && keyValue <= (uint)KeyCode.D9)
        {
            var numberKey = (ConsoleKey)keyValue;
            char numberChar;

            if (modifiers.HasFlag (ConsoleModifiers.Shift))
            {
                // US keyboard layout: Shift+0-9 produces )!@#$%^&*(
                numberChar = ")!@#$%^&*(" [(int)(keyValue - (uint)KeyCode.D0)];
            }
            else
            {
                numberChar = (char)('0' + (keyValue - (uint)KeyCode.D0));
            }

            return (numberKey, numberChar);
        }

        // Handle other standard keys
        var standardKey = (ConsoleKey)keyValue;

        if (Enum.IsDefined (typeof (ConsoleKey), (int)keyValue))
        {
            char standardChar = standardKey switch
            {
                ConsoleKey.Enter => '\r',
                ConsoleKey.Tab => '\t',
                ConsoleKey.Escape => '\u001B',
                ConsoleKey.Backspace => '\b',
                ConsoleKey.Spacebar => ' ',
                ConsoleKey.Clear => '\0',
                _ when keyValue <= 0x1F => '\0', // Control characters
                _ => (char)keyValue
            };

            return (standardKey, standardChar);
        }

        // For printable Unicode characters, return character with ConsoleKey.None
        if (keyValue <= 0x10FFFF && !char.IsControl ((char)keyValue))
        {
            return (ConsoleKey.None, (char)keyValue);
        }

        // Fallback
        return (ConsoleKey.None, (char)keyValue);
    }
}
