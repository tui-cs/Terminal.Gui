using System.Diagnostics;
// ReSharper disable InconsistentNaming

namespace Terminal.Gui.Drivers;

/// <summary>
///     Helper class for Windows key conversion utilities.
///     Contains static methods extracted from the legacy WindowsDriver for key processing.
/// </summary>
internal static class WindowsKeyHelper
{
    /// <summary>
    ///     Converts a key event record with a virtual key code of Packet to a corresponding key event record with updated
    ///     key information.
    /// </summary>
    /// <remarks>
    ///     This method is typically used to interpret Packet key events, which may represent input from
    ///     IMEs or other sources that generate Unicode characters not directly mapped to standard virtual key codes. The
    ///     returned record will have its key and scan code fields updated to reflect the decoded character and
    ///     modifiers.
    /// </remarks>
    /// <param name="keyEvent">
    ///     The key event record to convert. If the virtual key code is not Packet, the original record is returned
    ///     unchanged.
    /// </param>
    /// <returns>
    ///     A new key event record with updated key, scan code, and character information if the input represents a Packet
    ///     key; otherwise, the original key event record.
    /// </returns>
    public static WindowsConsole.KeyEventRecord FromVKPacketToKeyEventRecord (WindowsConsole.KeyEventRecord keyEvent)
    {
        if (keyEvent.wVirtualKeyCode != (VK)ConsoleKey.Packet)
        {
            return keyEvent;
        }

        // VK_PACKET means Windows is giving us a Unicode character without a virtual key.
        // The character is already in UnicodeChar - we don't need to decode anything.
        // We set VK to None and scan code to 0 since they're meaningless for VK_PACKET.
        return new ()
        {
            UnicodeChar = keyEvent.UnicodeChar,        // Keep the character - this is the key info!
            bKeyDown = keyEvent.bKeyDown,
            dwControlKeyState = keyEvent.dwControlKeyState,  // Keep modifiers
            wRepeatCount = keyEvent.wRepeatCount,
            wVirtualKeyCode = (VK)ConsoleKey.None,     // No virtual key for VK_PACKET
            wVirtualScanCode = 0                        // No scan code for VK_PACKET
        };
    }

    public static KeyCode MapKey (WindowsConsole.ConsoleKeyInfoEx keyInfoEx)
    {
        ConsoleKeyInfo keyInfo = keyInfoEx.ConsoleKeyInfo;

        // Handle VK_PACKET / None - character-only input (IME, emoji, etc.)
        if (keyInfo.Key == ConsoleKey.None && keyInfo.KeyChar != 0)
        {
            // This is a character from VK_PACKET (IME, emoji picker, etc.)
            // Just return the character as-is with modifiers
            return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers, (KeyCode)keyInfo.KeyChar);
        }

        switch (keyInfo.Key)
        {
            case ConsoleKey.D0:
            case ConsoleKey.D1:
            case ConsoleKey.D2:
            case ConsoleKey.D3:
            case ConsoleKey.D4:
            case ConsoleKey.D5:
            case ConsoleKey.D6:
            case ConsoleKey.D7:
            case ConsoleKey.D8:
            case ConsoleKey.D9:
            case ConsoleKey.NumPad0:
            case ConsoleKey.NumPad1:
            case ConsoleKey.NumPad2:
            case ConsoleKey.NumPad3:
            case ConsoleKey.NumPad4:
            case ConsoleKey.NumPad5:
            case ConsoleKey.NumPad6:
            case ConsoleKey.NumPad7:
            case ConsoleKey.NumPad8:
            case ConsoleKey.NumPad9:
            case ConsoleKey.Oem1:
            case ConsoleKey.Oem2:
            case ConsoleKey.Oem3:
            case ConsoleKey.Oem4:
            case ConsoleKey.Oem5:
            case ConsoleKey.Oem6:
            case ConsoleKey.Oem7:
            case ConsoleKey.Oem8:
            case ConsoleKey.Oem102:
            case ConsoleKey.Multiply:
            case ConsoleKey.Add:
            case ConsoleKey.Separator:
            case ConsoleKey.Subtract:
            case ConsoleKey.Decimal:
            case ConsoleKey.Divide:
            case ConsoleKey.OemPeriod:
            case ConsoleKey.OemComma:
            case ConsoleKey.OemPlus:
            case ConsoleKey.OemMinus:
                // These virtual key codes are mapped differently depending on the keyboard layout in use.
                // We use the Win32 API to map them to the correct character.
                uint mapResult = WindowsKeyboardLayout.MapVKtoChar ((VK)keyInfo.Key);

                if (mapResult == 0)
                {
                    // There is no mapping - this should not happen
                    Debug.Assert (true, $@"Unable to map the virtual key code {keyInfo.Key}.");

                    return KeyCode.Null;
                }

                // An un-shifted character value is in the low order word of the return value.
                var mappedChar = (char)(mapResult & 0x0000FFFF);

                if (keyInfo.KeyChar == 0)
                {
                    // If the keyChar is 0, keyInfo.Key value is not a printable character. 

                    // Dead keys (diacritics) are indicated by setting the top bit of the return value. 
                    if ((mapResult & 0x80000000) != 0)
                    {
                        // Dead key (e.g. Oem2 '~'/'^' on POR keyboard)
                        // Option 1: Throw it out. 
                        //    - Apps will never see the dead keys
                        //    - If user presses a key that can be combined with the dead key ('a'), the right thing happens (app will see '�').
                        //      - NOTE: With Dead Keys, KeyDown != KeyUp. The KeyUp event will have just the base char ('a').
                        //    - If user presses dead key again, the right thing happens (app will see `~~`)
                        //    - This is what Notepad etc... appear to do
                        // Option 2: Expand the API to indicate the KeyCode is a dead key
                        //    - Enables apps to do their own dead key processing
                        //    - Adds complexity; no dev has asked for this (yet).
                        // We choose Option 1 for now.
                        return KeyCode.Null;

                        // Note: Ctrl-Deadkey (like Oem3 '`'/'~` on ENG) can't be supported.
                        // Sadly, the charVal is just the deadkey and subsequent key events do not contain
                        // any info that the previous event was a deadkey.
                        // Note WT does not support Ctrl-Deadkey either.
                    }

                    if (keyInfo.Modifiers != 0)
                    {
                        // These Oem keys have well-defined chars. We ensure the representative char is used.
                        // If we don't do this, then on some keyboard layouts the wrong char is 
                        // returned (e.g. on ENG OemPlus un-shifted is =, not +). This is important
                        // for key persistence ("Ctrl++" vs. "Ctrl+=").
                        mappedChar = keyInfo.Key switch
                        {
                            ConsoleKey.OemPeriod => '.',
                            ConsoleKey.OemComma => ',',
                            ConsoleKey.OemPlus => '+',
                            ConsoleKey.OemMinus => '-',
                            _ => mappedChar
                        };
                    }

                    // Return the mappedChar with modifiers. Because mappedChar is un-shifted, if Shift was down
                    // we should keep it
                    return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers, (KeyCode)mappedChar);
                }

                // KeyChar is printable
                if (keyInfo.Modifiers.HasFlag (ConsoleModifiers.Alt) && keyInfo.Modifiers.HasFlag (ConsoleModifiers.Control))
                {
                    // AltGr support - AltGr is equivalent to Ctrl+Alt - the correct char is in KeyChar
                    return (KeyCode)keyInfo.KeyChar;
                }

                if (keyInfo.Modifiers != ConsoleModifiers.Shift)
                {
                    // If Shift wasn't down we don't need to do anything but return the mappedChar
                    return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers, (KeyCode)mappedChar);
                }

                // Strip off Shift - We got here because they KeyChar from Windows is the shifted char (e.g. "�")
                // and passing on Shift would be redundant.
                return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers & ~ConsoleModifiers.Shift, (KeyCode)keyInfo.KeyChar);
        }

        // A..Z are special cased:
        // - Alone, they represent lowercase a...z
        // - With ShiftMask they are A..Z
        // - If CapsLock is on the above is reversed.
        // - If Alt and/or Ctrl are present, treat as upper case
        if (keyInfo.Key is >= ConsoleKey.A and <= ConsoleKey.Z)
        {
            if (keyInfo.KeyChar == 0)
            {
                // KeyChar is not printable - possibly an AltGr key?
                // AltGr support - AltGr is equivalent to Ctrl+Alt
                if (keyInfo.Modifiers.HasFlag (ConsoleModifiers.Alt) && keyInfo.Modifiers.HasFlag (ConsoleModifiers.Control))
                {
                    return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers, (KeyCode)(uint)keyInfo.Key);
                }
            }

            if (keyInfo.Modifiers.HasFlag (ConsoleModifiers.Alt) || keyInfo.Modifiers.HasFlag (ConsoleModifiers.Control))
            {
                return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers, (KeyCode)(uint)keyInfo.Key);
            }

            if ((keyInfo.Modifiers == ConsoleModifiers.Shift) ^ keyInfoEx.CapsLock)
            {
                // If (ShiftMask is on and CapsLock is off) or (ShiftMask is off and CapsLock is on) add the ShiftMask
                if (char.IsUpper (keyInfo.KeyChar))
                {
                    if (keyInfo.KeyChar <= 'Z')
                    {
                        return (KeyCode)keyInfo.Key | KeyCode.ShiftMask;
                    }

                    // Always return the KeyChar because it may be an Á, À with Oem1, etc
                    return (KeyCode)keyInfo.KeyChar;
                }
            }

            if (keyInfo.KeyChar <= 'z')
            {
                return (KeyCode)keyInfo.Key;
            }

            // Always return the KeyChar because it may be an á, à with Oem1, etc
            return (KeyCode)keyInfo.KeyChar;
        }

        // Handle control keys whose VK codes match the related ASCII value (those below ASCII 33) like ESC
        // Also handle the key ASCII value 127 (BACK)
        if (Enum.IsDefined (typeof (KeyCode), (uint)keyInfo.Key))
        {
            // If the key is JUST a modifier, return it as just that key
            if (keyInfo.Key == (ConsoleKey)VK.SHIFT)
            { // Shift 16
                return KeyCode.ShiftMask;
            }

            if (keyInfo.Key == (ConsoleKey)VK.CONTROL)
            { // Ctrl 17
                return KeyCode.CtrlMask;
            }

            if (keyInfo.Key == (ConsoleKey)VK.MENU)
            { // Alt 18
                return KeyCode.AltMask;
            }

            if (keyInfo.KeyChar == 0)
            {
                return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers, (KeyCode)keyInfo.KeyChar);
            }

            // Backspace (ASCII 127)
            if (keyInfo.KeyChar == '\u007f')
            {
                return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers, (KeyCode)keyInfo.Key);
            }

            if (keyInfo.Key != ConsoleKey.None)
            {
                return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers, (KeyCode)keyInfo.KeyChar);
            }

            return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers & ~ConsoleModifiers.Shift, (KeyCode)keyInfo.KeyChar);
        }

        // Handle control keys (e.g. CursorUp)
        if (Enum.IsDefined (typeof (KeyCode), (uint)keyInfo.Key + (uint)KeyCode.MaxCodePoint))
        {
            return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers, (KeyCode)((uint)keyInfo.Key + (uint)KeyCode.MaxCodePoint));
        }

        return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers, (KeyCode)keyInfo.KeyChar);
    }
}
