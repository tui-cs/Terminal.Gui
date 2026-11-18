using System.Collections.Concurrent;
using InputRecord = Terminal.Gui.Drivers.WindowsConsole.InputRecord;
using ButtonState = Terminal.Gui.Drivers.WindowsConsole.ButtonState;
using EventFlags = Terminal.Gui.Drivers.WindowsConsole.EventFlags;
using ControlKeyState = Terminal.Gui.Drivers.WindowsConsole.ControlKeyState;
using MouseEventRecord = Terminal.Gui.Drivers.WindowsConsole.MouseEventRecord;

namespace DriverTests.Windows;

[Collection ("Driver Tests")]
public class WindowsInputProcessorTests
{
    [Fact]
    public void Test_ProcessQueue_CapitalHLowerE ()
    {
        ConcurrentQueue<InputRecord> queue = new ();

        queue.Enqueue (
                       new ()
                       {
                           EventType = WindowsConsole.EventType.Key,
                           KeyEvent = new ()
                           {
                               bKeyDown = true,
                               UnicodeChar = 'H',
                               dwControlKeyState = ControlKeyState.CapslockOn,
                               wVirtualKeyCode = (VK)72,
                               wVirtualScanCode = 35
                           }
                       });

        queue.Enqueue (
                       new ()
                       {
                           EventType = WindowsConsole.EventType.Key,
                           KeyEvent = new ()
                           {
                               bKeyDown = false,
                               UnicodeChar = 'H',
                               dwControlKeyState = ControlKeyState.CapslockOn,
                               wVirtualKeyCode = (VK)72,
                               wVirtualScanCode = 35
                           }
                       });

        queue.Enqueue (
                       new ()
                       {
                           EventType = WindowsConsole.EventType.Key,
                           KeyEvent = new ()
                           {
                               bKeyDown = true,
                               UnicodeChar = 'i',
                               dwControlKeyState = ControlKeyState.NoControlKeyPressed,
                               wVirtualKeyCode = (VK)73,
                               wVirtualScanCode = 23
                           }
                       });

        queue.Enqueue (
                       new ()
                       {
                           EventType = WindowsConsole.EventType.Key,
                           KeyEvent = new ()
                           {
                               bKeyDown = false,
                               UnicodeChar = 'i',
                               dwControlKeyState = ControlKeyState.NoControlKeyPressed,
                               wVirtualKeyCode = (VK)73,
                               wVirtualScanCode = 23
                           }
                       });

        var processor = new WindowsInputProcessor (queue, null);

        List<Key> downs = [];

        processor.KeyDown += (s, e) => { downs.Add (e); };

        Assert.Empty (downs);

        processor.ProcessQueue ();

        Assert.Equal (Key.H.WithShift, downs [0]);
        Assert.Equal (Key.I, downs [1]);
    }

    [Fact]
    public void Test_ProcessQueue_Mouse_Move ()
    {
        ConcurrentQueue<InputRecord> queue = new ();

        queue.Enqueue (
                       new ()
                       {
                           EventType = WindowsConsole.EventType.Mouse,
                           MouseEvent = new ()
                           {
                               MousePosition = new (32, 31),
                               ButtonState = ButtonState.NoButtonPressed,
                               ControlKeyState = ControlKeyState.NoControlKeyPressed,
                               EventFlags = EventFlags.MouseMoved
                           }
                       });

        var processor = new WindowsInputProcessor (queue, null);

        List<Mouse> mouseEvents = [];

        processor.SyntheticMouseEvent += (s, e) => { mouseEvents.Add (e); };

        Assert.Empty (mouseEvents);

        processor.ProcessQueue ();

        Mouse s = Assert.Single (mouseEvents);
        Assert.Equal (MouseFlags.PositionReport, s.Flags);
        Assert.Equal (s.ScreenPosition, new (32, 31));
    }

    [Theory]
    [InlineData (ButtonState.Button1Pressed, MouseFlags.LeftButtonPressed)]
    [InlineData (ButtonState.Button2Pressed, MouseFlags.MiddleButtonPressed)]
    [InlineData (ButtonState.Button3Pressed, MouseFlags.RightButtonPressed)]
    [InlineData (ButtonState.Button4Pressed, MouseFlags.Button4Pressed)]
    internal void Test_ProcessQueue_Mouse_Pressed (ButtonState state, MouseFlags expectedFlag)
    {
        ConcurrentQueue<InputRecord> queue = new ();

        queue.Enqueue (
                       new ()
                       {
                           EventType = WindowsConsole.EventType.Mouse,
                           MouseEvent = new ()
                           {
                               MousePosition = new (32, 31),
                               ButtonState = state,
                               ControlKeyState = ControlKeyState.NoControlKeyPressed,
                               EventFlags = EventFlags.MouseMoved
                           }
                       });

        var processor = new WindowsInputProcessor (queue, null);

        List<Mouse> mouseEvents = [];

        processor.SyntheticMouseEvent += (s, e) => { mouseEvents.Add (e); };

        Assert.Empty (mouseEvents);

        processor.ProcessQueue ();

        Mouse s = Assert.Single (mouseEvents);
        Assert.Equal (s.Flags, MouseFlags.PositionReport | expectedFlag);
        Assert.Equal (s.ScreenPosition, new (32, 31));
    }

    [Theory]
    [InlineData (100, MouseFlags.WheeledUp)]
    [InlineData (-100, MouseFlags.WheeledDown)]
    internal void Test_ProcessQueue_Mouse_Wheel (int wheelValue, MouseFlags expectedFlag)
    {
        ConcurrentQueue<InputRecord> queue = new ();

        queue.Enqueue (
                       new ()
                       {
                           EventType = WindowsConsole.EventType.Mouse,
                           MouseEvent = new ()
                           {
                               MousePosition = new (32, 31),
                               ButtonState = (ButtonState)wheelValue,
                               ControlKeyState = ControlKeyState.NoControlKeyPressed,
                               EventFlags = EventFlags.MouseWheeled
                           }
                       });

        var processor = new WindowsInputProcessor (queue, null);

        List<Mouse> mouseEvents = [];

        processor.SyntheticMouseEvent += (s, e) => { mouseEvents.Add (e); };

        Assert.Empty (mouseEvents);

        processor.ProcessQueue ();

        Mouse s = Assert.Single (mouseEvents);
        Assert.Equal (s.Flags, expectedFlag);
        Assert.Equal (s.ScreenPosition, new (32, 31));
    }

    public static IEnumerable<object []> MouseFlagTestData ()
    {
        yield return
        [
            new []
            {
                Tuple.Create (ButtonState.Button1Pressed, EventFlags.NoEvent, ControlKeyState.NoControlKeyPressed, MouseFlags.LeftButtonPressed),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.NoControlKeyPressed, MouseFlags.LeftButtonReleased),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.NoControlKeyPressed, MouseFlags.None)
            }
        ];

        yield return
        [
            new []
            {
                Tuple.Create (
                              ButtonState.Button2Pressed,
                              EventFlags.MouseMoved,
                              ControlKeyState.NoControlKeyPressed,
                              MouseFlags.MiddleButtonPressed | MouseFlags.PositionReport),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.NoControlKeyPressed, MouseFlags.MiddleButtonReleased),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.NoControlKeyPressed, MouseFlags.None)
            }
        ];

        yield return
        [
            new []
            {
                Tuple.Create (
                              ButtonState.Button3Pressed,
                              EventFlags.MouseMoved,
                              ControlKeyState.NoControlKeyPressed,
                              MouseFlags.RightButtonPressed | MouseFlags.PositionReport),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.NoControlKeyPressed, MouseFlags.RightButtonReleased),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.NoControlKeyPressed, MouseFlags.None)
            }
        ];

        yield return
        [
            new []
            {
                Tuple.Create (
                              ButtonState.Button4Pressed,
                              EventFlags.MouseMoved,
                              ControlKeyState.NoControlKeyPressed,
                              MouseFlags.Button4Pressed | MouseFlags.PositionReport),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.NoControlKeyPressed, MouseFlags.Button4Released),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.MouseMoved, ControlKeyState.NoControlKeyPressed, MouseFlags.PositionReport)
            }
        ];

        yield return
        [
            new []
            {
                Tuple.Create (
                              ButtonState.RightmostButtonPressed,
                              EventFlags.MouseMoved,
                              ControlKeyState.NoControlKeyPressed,
                              MouseFlags.RightButtonPressed | MouseFlags.PositionReport),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.NoControlKeyPressed, MouseFlags.RightButtonReleased),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.NoControlKeyPressed, MouseFlags.None)
            }
        ];

        // Tests for holding down 2 buttons at once and releasing them one after the other
        yield return
        [
            new []
            {
                Tuple.Create (
                              ButtonState.Button1Pressed | ButtonState.Button2Pressed,
                              EventFlags.MouseMoved,
                              ControlKeyState.NoControlKeyPressed,
                              MouseFlags.LeftButtonPressed | MouseFlags.MiddleButtonPressed | MouseFlags.PositionReport),
                Tuple.Create (
                              ButtonState.Button1Pressed,
                              EventFlags.NoEvent,
                              ControlKeyState.NoControlKeyPressed,
                              MouseFlags.LeftButtonPressed | MouseFlags.MiddleButtonReleased),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.NoControlKeyPressed, MouseFlags.LeftButtonReleased),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.NoControlKeyPressed, MouseFlags.None)
            }
        ];

        yield return
        [
            new []
            {
                Tuple.Create (
                              ButtonState.Button3Pressed | ButtonState.Button4Pressed,
                              EventFlags.MouseMoved,
                              ControlKeyState.NoControlKeyPressed,
                              MouseFlags.RightButtonPressed | MouseFlags.Button4Pressed | MouseFlags.PositionReport),
                Tuple.Create (
                              ButtonState.Button3Pressed,
                              EventFlags.NoEvent,
                              ControlKeyState.NoControlKeyPressed,
                              MouseFlags.RightButtonPressed | MouseFlags.Button4Released),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.NoControlKeyPressed, MouseFlags.RightButtonReleased),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.NoControlKeyPressed, MouseFlags.None)
            }
        ];

        // Test for holding down 2 buttons at once and releasing them simultaneously
        yield return
        [
            new []
            {
                Tuple.Create (
                              ButtonState.Button1Pressed | ButtonState.Button2Pressed,
                              EventFlags.MouseMoved,
                              ControlKeyState.NoControlKeyPressed,
                              MouseFlags.LeftButtonPressed | MouseFlags.MiddleButtonPressed | MouseFlags.PositionReport),
                Tuple.Create (
                              ButtonState.NoButtonPressed,
                              EventFlags.NoEvent,
                              ControlKeyState.NoControlKeyPressed,
                              MouseFlags.LeftButtonReleased | MouseFlags.MiddleButtonReleased),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.NoControlKeyPressed, MouseFlags.None)
            }
        ];

        // Test that rightmost and button 3 are the same button so 2 states is still only 1 flag
        yield return
        [
            new []
            {
                Tuple.Create (
                              ButtonState.Button3Pressed | ButtonState.RightmostButtonPressed,
                              EventFlags.MouseMoved,
                              ControlKeyState.NoControlKeyPressed,
                              MouseFlags.RightButtonPressed | MouseFlags.PositionReport),

                // Can swap between without raising the released
                Tuple.Create (
                              ButtonState.Button3Pressed,
                              EventFlags.MouseMoved,
                              ControlKeyState.NoControlKeyPressed,
                              MouseFlags.RightButtonPressed | MouseFlags.PositionReport),
                Tuple.Create (
                              ButtonState.RightmostButtonPressed,
                              EventFlags.MouseMoved,
                              ControlKeyState.NoControlKeyPressed,
                              MouseFlags.RightButtonPressed | MouseFlags.PositionReport),

                // Now with neither we get released
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.NoControlKeyPressed, MouseFlags.RightButtonReleased),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.NoControlKeyPressed, MouseFlags.None)
            }
        ];

        // Test for ControlKeyState buttons pressed and handled
        yield return
        [
            new []
            {
                Tuple.Create (ButtonState.Button1Pressed, EventFlags.NoEvent, ControlKeyState.LeftAltPressed, MouseFlags.LeftButtonPressed | MouseFlags.Alt),
                Tuple.Create (
                              ButtonState.NoButtonPressed,
                              EventFlags.NoEvent,
                              ControlKeyState.LeftAltPressed,
                              MouseFlags.LeftButtonReleased | MouseFlags.Alt),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.LeftAltPressed, MouseFlags.None | MouseFlags.Alt)
            }
        ];

        yield return
        [
            new []
            {
                Tuple.Create (
                              ButtonState.Button1Pressed,
                              EventFlags.NoEvent,
                              ControlKeyState.RightAltPressed,
                              MouseFlags.LeftButtonPressed | MouseFlags.Alt),
                Tuple.Create (
                              ButtonState.NoButtonPressed,
                              EventFlags.NoEvent,
                              ControlKeyState.RightAltPressed,
                              MouseFlags.LeftButtonReleased | MouseFlags.Alt),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.RightAltPressed, MouseFlags.None | MouseFlags.Alt)
            }
        ];

        yield return
        [
            new []
            {
                Tuple.Create (
                              ButtonState.Button1Pressed,
                              EventFlags.NoEvent,
                              ControlKeyState.LeftControlPressed,
                              MouseFlags.LeftButtonPressed | MouseFlags.Ctrl),
                Tuple.Create (
                              ButtonState.NoButtonPressed,
                              EventFlags.NoEvent,
                              ControlKeyState.LeftControlPressed,
                              MouseFlags.LeftButtonReleased | MouseFlags.Ctrl),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.LeftControlPressed, MouseFlags.None | MouseFlags.Ctrl)
            }
        ];

        yield return
        [
            new []
            {
                Tuple.Create (
                              ButtonState.Button1Pressed,
                              EventFlags.NoEvent,
                              ControlKeyState.RightControlPressed,
                              MouseFlags.LeftButtonPressed | MouseFlags.Ctrl),
                Tuple.Create (
                              ButtonState.NoButtonPressed,
                              EventFlags.NoEvent,
                              ControlKeyState.RightControlPressed,
                              MouseFlags.LeftButtonReleased | MouseFlags.Ctrl),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.RightControlPressed, MouseFlags.None | MouseFlags.Ctrl)
            }
        ];

        yield return
        [
            new []
            {
                Tuple.Create (ButtonState.Button1Pressed, EventFlags.NoEvent, ControlKeyState.ShiftPressed, MouseFlags.LeftButtonPressed | MouseFlags.Shift),
                Tuple.Create (
                              ButtonState.NoButtonPressed,
                              EventFlags.NoEvent,
                              ControlKeyState.ShiftPressed,
                              MouseFlags.LeftButtonReleased | MouseFlags.Shift),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.ShiftPressed, MouseFlags.None | MouseFlags.Shift)
            }
        ];

        // Copilot - GPT-5.5
        yield return
        [
            new []
            {
                Tuple.Create (
                              ButtonState.Button1Pressed,
                              EventFlags.NoEvent,
                              ControlKeyState.ShiftPressed | ControlKeyState.LeftControlPressed,
                              MouseFlags.LeftButtonPressed | MouseFlags.Shift | MouseFlags.Ctrl),
                Tuple.Create (
                              ButtonState.NoButtonPressed,
                              EventFlags.NoEvent,
                              ControlKeyState.ShiftPressed | ControlKeyState.CapslockOn,
                              MouseFlags.LeftButtonReleased | MouseFlags.Shift),
                Tuple.Create (
                              ButtonState.NoButtonPressed,
                              EventFlags.NoEvent,
                              ControlKeyState.ShiftPressed | ControlKeyState.NumlockOn | ControlKeyState.ScrolllockOn,
                              MouseFlags.None | MouseFlags.Shift)
            }
        ];

        // Test for ControlKeyState buttons pressed and not handled
        yield return
        [
            new []
            {
                Tuple.Create (ButtonState.Button1Pressed, EventFlags.NoEvent, ControlKeyState.CapslockOn, MouseFlags.LeftButtonPressed),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.CapslockOn, MouseFlags.LeftButtonReleased),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.CapslockOn, MouseFlags.None)
            }
        ];

        yield return
        [
            new []
            {
                Tuple.Create (ButtonState.Button1Pressed, EventFlags.NoEvent, ControlKeyState.EnhancedKey, MouseFlags.LeftButtonPressed),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.EnhancedKey, MouseFlags.LeftButtonReleased),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.EnhancedKey, MouseFlags.None)
            }
        ];

        yield return
        [
            new []
            {
                Tuple.Create (ButtonState.Button1Pressed, EventFlags.NoEvent, ControlKeyState.NumlockOn, MouseFlags.LeftButtonPressed),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.NumlockOn, MouseFlags.LeftButtonReleased),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.NumlockOn, MouseFlags.None)
            }
        ];

        yield return
        [
            new []
            {
                Tuple.Create (ButtonState.Button1Pressed, EventFlags.NoEvent, ControlKeyState.ScrolllockOn, MouseFlags.LeftButtonPressed),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.ScrolllockOn, MouseFlags.LeftButtonReleased),
                Tuple.Create (ButtonState.NoButtonPressed, EventFlags.NoEvent, ControlKeyState.ScrolllockOn, MouseFlags.None)
            }
        ];
    }

    [Theory]
    [MemberData (nameof (MouseFlagTestData))]
    internal void MouseFlags_Should_Map_Correctly (Tuple<ButtonState, EventFlags, ControlKeyState, MouseFlags> [] inputOutputPairs)
    {
        var processor = new WindowsInputProcessor (new (), null);

        foreach (Tuple<ButtonState, EventFlags, ControlKeyState, MouseFlags> pair in inputOutputPairs)
        {
            var mockEvent = new MouseEventRecord { ButtonState = pair.Item1, EventFlags = pair.Item2, ControlKeyState = pair.Item3 };
            Mouse result = processor.ToMouseEvent (mockEvent);

            Assert.Equal (pair.Item4, result.Flags);
        }
    }

    }
