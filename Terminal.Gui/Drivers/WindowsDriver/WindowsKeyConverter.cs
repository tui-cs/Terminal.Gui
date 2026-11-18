namespace Terminal.Gui.Drivers;

/// <summary>
///     <see cref="IKeyConverter{T}"/> capable of converting the
///     windows native <see cref="WindowsConsole.InputRecord"/> class
///     into Terminal.Gui shared <see cref="Key"/> representation
///     (used by <see cref="View"/> etc).
/// </summary>
internal class WindowsKeyConverter : IKeyConverter<WindowsConsole.InputRecord>
{
    /// <inheritdoc/>
    public Key ToKey (WindowsConsole.InputRecord inputEvent)
    {
        if (inputEvent.KeyEvent.wVirtualKeyCode == (VK)ConsoleKey.Packet)
        {
            // Used to pass Unicode characters as if they were keystrokes.
            // The VK_PACKET key is the low word of a 32-bit
            // Virtual Key value used for non-keyboard input methods.
            inputEvent.KeyEvent = WindowsKeyHelper.FromVKPacketToKeyEventRecord (inputEvent.KeyEvent);
        }

        var keyInfo = WindowsConsole.ToConsoleKeyInfoEx (inputEvent.KeyEvent);

        //Debug.WriteLine ($"event: KBD: {GetKeyboardLayoutName()} {inputEvent.ToString ()} {keyInfo.ToString (keyInfo)}");

        KeyCode map = WindowsKeyHelper.MapKey (keyInfo);

        if (map == KeyCode.Null)
        {
            return 0;
        }

        return new (map);
    }

    /// <inheritdoc/>
    public WindowsConsole.InputRecord ToKeyInfo (Key key)
    {
        // Convert Key to ConsoleKeyInfo using the cross-platform mapping utility
        ConsoleKeyInfo consoleKeyInfo = ConsoleKeyMapping.GetConsoleKeyInfoFromKeyCode (key.KeyCode);

        // Build the ControlKeyState from the ConsoleKeyInfo modifiers
        var controlKeyState = WindowsConsole.ControlKeyState.NoControlKeyPressed;

        if (consoleKeyInfo.Modifiers.HasFlag (ConsoleModifiers.Shift))
        {
            controlKeyState |= WindowsConsole.ControlKeyState.ShiftPressed;
        }

        if (consoleKeyInfo.Modifiers.HasFlag (ConsoleModifiers.Alt))
        {
            controlKeyState |= WindowsConsole.ControlKeyState.LeftAltPressed;
        }

        if (consoleKeyInfo.Modifiers.HasFlag (ConsoleModifiers.Control))
        {
            controlKeyState |= WindowsConsole.ControlKeyState.LeftControlPressed;
        }

        // Get the scan code using Windows API if available, otherwise use a simple heuristic
        ushort scanCode = GetScanCodeForKey (consoleKeyInfo.Key);

        // Create a KeyEventRecord with the converted values
        var keyEvent = new WindowsConsole.KeyEventRecord
        {
            bKeyDown = true, // Assume key down for conversion
            wRepeatCount = 1,
            wVirtualKeyCode = (VK)consoleKeyInfo.Key,
            wVirtualScanCode = scanCode,
            UnicodeChar = consoleKeyInfo.KeyChar,
            dwControlKeyState = controlKeyState
        };

        // Create and return an InputRecord with the keyboard event
        return new ()
        {
            EventType = WindowsConsole.EventType.Key,
            KeyEvent = keyEvent
        };
    }

    /// <summary>
    ///     Gets the hardware scan code for a given ConsoleKey.
    /// </summary>
    /// <param name="key">The ConsoleKey to get the scan code for.</param>
    /// <returns>The scan code, or 0 if not available.</returns>
    /// <remarks>
    ///     On Windows, uses MapVirtualKey to get the actual scan code from the OS.
    ///     This respects the current keyboard layout and is more accurate than a static lookup table.
    ///     For test simulation purposes, returning 0 is acceptable as Windows doesn't strictly require it.
    /// </remarks>
    private static ushort GetScanCodeForKey (ConsoleKey key)
    {
        //// For test simulation, scan codes aren't critical. However, we can use the Windows API
        //// to get the correct scan code if we're running on Windows.
        //if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        //{
        //    try
        //    {
        //        // MapVirtualKey with MAPVK_VK_TO_VSC (0) converts VK to scan code
        //        // This uses the current keyboard layout, so it's more accurate than a static table
        //        uint scanCodeExtended = WindowsKeyboardLayout.MapVirtualKey ((VK)key, 0);

        //        // The scan code is in the low byte
        //        return (ushort)(scanCodeExtended & 0xFF);
        //    }
        //    catch
        //    {
        //        // If MapVirtualKey fails, fall back to simple heuristic
        //    }
        //}

        // Fallback: Use a simple heuristic for common keys
        // For most test scenarios, these values work fine
        return key switch
        {
            ConsoleKey.Escape => 1,
            ConsoleKey.D1 => 2,
            ConsoleKey.D2 => 3,
            ConsoleKey.D3 => 4,
            ConsoleKey.D4 => 5,
            ConsoleKey.D5 => 6,
            ConsoleKey.D6 => 7,
            ConsoleKey.D7 => 8,
            ConsoleKey.D8 => 9,
            ConsoleKey.D9 => 10,
            ConsoleKey.D0 => 11,
            ConsoleKey.Tab => 15,
            ConsoleKey.Q => 16,
            ConsoleKey.W => 17,
            ConsoleKey.E => 18,
            ConsoleKey.R => 19,
            ConsoleKey.T => 20,
            ConsoleKey.Y => 21,
            ConsoleKey.U => 22,
            ConsoleKey.I => 23,
            ConsoleKey.O => 24,
            ConsoleKey.P => 25,
            ConsoleKey.Enter => 28,
            ConsoleKey.A => 30,
            ConsoleKey.S => 31,
            ConsoleKey.D => 32,
            ConsoleKey.F => 33,
            ConsoleKey.G => 34,
            ConsoleKey.H => 35,
            ConsoleKey.J => 36,
            ConsoleKey.K => 37,
            ConsoleKey.L => 38,
            ConsoleKey.Z => 44,
            ConsoleKey.X => 45,
            ConsoleKey.C => 46,
            ConsoleKey.V => 47,
            ConsoleKey.B => 48,
            ConsoleKey.N => 49,
            ConsoleKey.M => 50,
            ConsoleKey.Spacebar => 57,
            ConsoleKey.F1 => 59,
            ConsoleKey.F2 => 60,
            ConsoleKey.F3 => 61,
            ConsoleKey.F4 => 62,
            ConsoleKey.F5 => 63,
            ConsoleKey.F6 => 64,
            ConsoleKey.F7 => 65,
            ConsoleKey.F8 => 66,
            ConsoleKey.F9 => 67,
            ConsoleKey.F10 => 68,
            ConsoleKey.Home => 71,
            ConsoleKey.UpArrow => 72,
            ConsoleKey.PageUp => 73,
            ConsoleKey.LeftArrow => 75,
            ConsoleKey.RightArrow => 77,
            ConsoleKey.End => 79,
            ConsoleKey.DownArrow => 80,
            ConsoleKey.PageDown => 81,
            ConsoleKey.Insert => 82,
            ConsoleKey.Delete => 83,
            ConsoleKey.F11 => 87,
            ConsoleKey.F12 => 88,
            _ => 0 // Unknown or not needed for test simulation
        };
    }
}
