using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

using InputRecord = WindowsConsole.InputRecord;

/// <summary>
///     Input processor for <see cref="WindowsInput"/>, deals in <see cref="WindowsConsole.InputRecord"/> stream.
/// </summary>
internal class WindowsInputProcessor : InputProcessorImpl<InputRecord>
{
    private readonly bool [] _lastWasPressed = new bool [4];

    /// <inheritdoc/>
    /// <param name="inputBuffer">The input buffer to process.</param>
    /// <param name="timeProvider">Time provider for timestamps and timing control.</param>
    public WindowsInputProcessor (ConcurrentQueue<InputRecord> inputBuffer, ITimeProvider? timeProvider = null)
        : base (inputBuffer, new WindowsKeyConverter (), timeProvider)
    {
    }

    /// <inheritdoc />
    public override void InjectMouseEvent (IApplication? app, Mouse mouse)
    {
        InputQueue.Enqueue (new ()
        {
            EventType = WindowsConsole.EventType.Mouse,
            MouseEvent = ToMouseEventRecord (mouse)
        });
    }

    /// <inheritdoc/>
    protected override void Process (InputRecord inputEvent)
    {
        switch (inputEvent.EventType)
        {
            case WindowsConsole.EventType.Key:

                // Ignore keyup because TG v2 does not support keyup events
                if (!inputEvent.KeyEvent.bKeyDown)
                {
                    return;
                }

                foreach (Tuple<char, InputRecord> released in Parser.ProcessInput (Tuple.Create (inputEvent.KeyEvent.UnicodeChar, inputEvent)))
                {
                    ProcessAfterParsing (released.Item2);
                }

                /*
                if (inputEvent.KeyEvent.wVirtualKeyCode == (VK)ConsoleKey.Packet)
                {
                    // Used to pass Unicode characters as if they were keystrokes.
                    // The VK_PACKET key is the low word of a 32-bit
                    // Virtual Key value used for non-keyboard input methods.
                    inputEvent.KeyEvent = FromVKPacketToKeyEventRecord (inputEvent.KeyEvent);
                }

                WindowsConsole.ConsoleKeyInfoEx keyInfo = ToConsoleKeyInfoEx (inputEvent.KeyEvent);

                //Debug.WriteLine ($"event: KBD: {GetKeyboardLayoutName()} {inputEvent.ToString ()} {keyInfo.ToString (keyInfo)}");

                KeyCode map = MapKey (keyInfo);

                if (map == KeyCode.Null)
                {
                    break;
                }
                */
                // This follows convention in DotNetDriver

                break;

            case WindowsConsole.EventType.Mouse:
                Mouse me = ToMouseEvent (inputEvent.MouseEvent);

                RaiseSyntheticMouseEvent (me);

                break;
        }
    }

    /// <summary>
    ///     Converts a Windows-specific mouse event to a <see cref="Mouse"/>.
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    public Mouse ToMouseEvent (WindowsConsole.MouseEventRecord e)
    {
        var mouseFlags = MouseFlags.None;

        mouseFlags = UpdateMouseFlags (
                                       mouseFlags,
                                       e.ButtonState,
                                       WindowsConsole.ButtonState.Button1Pressed,
                                       MouseFlags.LeftButtonPressed,
                                       MouseFlags.LeftButtonReleased,
                                       0);

        mouseFlags = UpdateMouseFlags (
                                       mouseFlags,
                                       e.ButtonState,
                                       WindowsConsole.ButtonState.Button2Pressed,
                                       MouseFlags.MiddleButtonPressed,
                                       MouseFlags.MiddleButtonReleased,
                                       1);

        mouseFlags = UpdateMouseFlags (
                                       mouseFlags,
                                       e.ButtonState,
                                       WindowsConsole.ButtonState.Button4Pressed,
                                       MouseFlags.Button4Pressed,
                                       MouseFlags.Button4Released,
                                       3);

        // Deal with button 3 separately because it is considered same as 'rightmost button'
        if (e.ButtonState.HasFlag (WindowsConsole.ButtonState.Button3Pressed) || e.ButtonState.HasFlag (WindowsConsole.ButtonState.RightmostButtonPressed))
        {
            mouseFlags |= MouseFlags.RightButtonPressed;
            _lastWasPressed [2] = true;
        }
        else
        {
            if (_lastWasPressed [2])
            {
                mouseFlags |= MouseFlags.RightButtonReleased;

                _lastWasPressed [2] = false;
            }
        }

        if (e.EventFlags == WindowsConsole.EventFlags.MouseWheeled)
        {
            switch ((int)e.ButtonState)
            {
                case > 0:
                    mouseFlags = MouseFlags.WheeledUp;

                    break;

                case < 0:
                    mouseFlags = MouseFlags.WheeledDown;

                    break;
            }
        }

        if (e.EventFlags != WindowsConsole.EventFlags.NoEvent)
        {
            switch (e.EventFlags)
            {
                case WindowsConsole.EventFlags.MouseMoved:
                    mouseFlags |= MouseFlags.PositionReport;

                    break;
            }
        }

        if ((e.ControlKeyState & (WindowsConsole.ControlKeyState.RightAltPressed | WindowsConsole.ControlKeyState.LeftAltPressed)) != 0)
        {
            mouseFlags |= MouseFlags.Alt;
        }

        if ((e.ControlKeyState & (WindowsConsole.ControlKeyState.RightControlPressed | WindowsConsole.ControlKeyState.LeftControlPressed)) != 0)
        {
            mouseFlags |= MouseFlags.Ctrl;
        }

        if ((e.ControlKeyState & WindowsConsole.ControlKeyState.ShiftPressed) != 0)
        {
            mouseFlags |= MouseFlags.Shift;
        }

        var result = new Mouse
        {
            Timestamp = DateTime.Now,
            Position = new (e.MousePosition.X, e.MousePosition.Y),
            ScreenPosition = new (e.MousePosition.X, e.MousePosition.Y),
            Flags = mouseFlags
        };

        return result;
    }

    private MouseFlags UpdateMouseFlags (
        MouseFlags current,
        WindowsConsole.ButtonState newState,
        WindowsConsole.ButtonState pressedState,
        MouseFlags pressedFlag,
        MouseFlags releasedFlag,
        int buttonIndex
    )
    {
        if (newState.HasFlag (pressedState))
        {
            current |= pressedFlag;
            _lastWasPressed [buttonIndex] = true;
        }
        else
        {
            if (_lastWasPressed [buttonIndex])
            {
                current |= releasedFlag;

                _lastWasPressed [buttonIndex] = false;
            }
        }

        return current;
    }

    /// <summary>
    ///     Converts a <see cref="Mouse"/> to a Windows-specific <see cref="WindowsConsole.MouseEventRecord"/>.
    /// </summary>
    /// <param name="mouse"></param>
    /// <returns></returns>
    public WindowsConsole.MouseEventRecord ToMouseEventRecord (Mouse mouse)
    {
        var buttonState = WindowsConsole.ButtonState.NoButtonPressed;
        var eventFlags = WindowsConsole.EventFlags.NoEvent;
        var controlKeyState = WindowsConsole.ControlKeyState.NoControlKeyPressed;

        // Convert button states
        if (mouse.Flags.FastHasFlags (MouseFlags.LeftButtonPressed))
        {
            buttonState |= WindowsConsole.ButtonState.Button1Pressed;
        }

        if (mouse.Flags.FastHasFlags (MouseFlags.MiddleButtonPressed))
        {
            buttonState |= WindowsConsole.ButtonState.Button2Pressed;
        }

        if (mouse.Flags.FastHasFlags (MouseFlags.RightButtonPressed))
        {
            buttonState |= WindowsConsole.ButtonState.Button3Pressed;
        }

        if (mouse.Flags.FastHasFlags (MouseFlags.Button4Pressed))
        {
            buttonState |= WindowsConsole.ButtonState.Button4Pressed;
        }

        // Convert mouse wheel events
        if (mouse.Flags.FastHasFlags (MouseFlags.WheeledUp))
        {
            eventFlags = WindowsConsole.EventFlags.MouseWheeled;
            buttonState = (WindowsConsole.ButtonState)0x00780000; // Positive value for wheel up
        }
        else if (mouse.Flags.FastHasFlags (MouseFlags.WheeledDown))
        {
            eventFlags = WindowsConsole.EventFlags.MouseWheeled;
            buttonState = (WindowsConsole.ButtonState)unchecked((int)0xFF880000); // Negative value for wheel down
        }

        // Convert movement flag
        if (mouse.Flags.FastHasFlags (MouseFlags.PositionReport))
        {
            eventFlags |= WindowsConsole.EventFlags.MouseMoved;
        }

        // Convert modifier keys
        if (mouse.Flags.FastHasFlags (MouseFlags.Alt))
        {
            controlKeyState |= WindowsConsole.ControlKeyState.LeftAltPressed;
        }

        if (mouse.Flags.FastHasFlags (MouseFlags.Ctrl))
        {
            controlKeyState |= WindowsConsole.ControlKeyState.LeftControlPressed;
        }

        if (mouse.Flags.FastHasFlags (MouseFlags.Shift))
        {
            controlKeyState |= WindowsConsole.ControlKeyState.ShiftPressed;
        }

        return new ()
        {
            MousePosition = new ((short)mouse.ScreenPosition.X, (short)mouse.ScreenPosition.Y),
            ButtonState = buttonState,
            ControlKeyState = controlKeyState,
            EventFlags = eventFlags
        };
    }
}
