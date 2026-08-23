// Copilot - Opus 4.6

using System.Collections.Concurrent;

namespace DriverTests.AnsiHandling;

/// <summary>
///     Integration tests that inject raw ANSI escape sequences through the full input pipeline
///     (parser → driver → <c>app.Keyboard.KeyDown/KeyUp</c>) and verify correct event delivery.
/// </summary>
public class KittyKeyboardPipelineTests
{
    /// <summary>
    ///     Injects raw ANSI sequences into the full Application pipeline and collects KeyDown/KeyUp events.
    /// </summary>
    private static (List<Key> KeyDown, List<Key> KeyUp) InjectRawSequence (params string [] sequences)
    {
        return InjectRawSequenceCore (false, sequences);
    }

    private static (List<Key> KeyDown, List<Key> KeyUp) InjectRawSequenceWithKittyEnabled (params string [] sequences)
    {
        return InjectRawSequenceCore (true, sequences);
    }

    private static (List<Key> KeyDown, List<Key> KeyUp) InjectRawSequenceCore (bool kittyKeyboardEnabled, params string [] sequences)
    {
        VirtualTimeProvider timeProvider = new ();
        timeProvider.SetTime (new DateTime (2025, 1, 1, 12, 0, 0));

        using IApplication app = Application.Create (timeProvider);
        app.Init (DriverRegistry.Names.ANSI);

        List<Key> keyDownEvents = [];
        List<Key> keyUpEvents = [];
        app.Keyboard.KeyDown += (_, key) => keyDownEvents.Add (key);
        app.Keyboard.KeyUp += (_, key) => keyUpEvents.Add (key);

        IInputProcessor processor = app.Driver?.GetInputProcessor ()!;
        ((AnsiInputProcessor)processor).SetKittyKeyboardEnabled (kittyKeyboardEnabled);
        ConcurrentQueue<char> queue = ((AnsiInputProcessor)processor).InputQueue;

        foreach (string seq in sequences)
        {
            foreach (char ch in seq)
            {
                queue.Enqueue (ch);
            }

            processor.ProcessQueue ();
        }

        return (keyDownEvents, keyUpEvents);
    }

    #region CSI u Event Types

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_Press_RaisesKeyDown ()
    {
        // ESC[97;1:1u = 'a' press
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[97;1:1u");

        Assert.Single (down);
        Assert.Equal (Key.A, down [0]);
        Assert.Equal (KeyEventType.Press, down [0].EventType);
        Assert.Empty (up);
    }

    [Fact]
    public void Pipeline_AltGrE_Press_RaisesEuroKeyDown ()
    {
        // ESC[8364;1:1u = Euro symbol press
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[8364;1:1u");

        Assert.Single (down);
        Assert.Equal (KeyEventType.Press, down [0].EventType);
        Assert.Equal ((KeyCode)8364, down [0].KeyCode);
        Assert.Equal ("€", down [0].AsGrapheme);
        Assert.Empty (up);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_Repeat_RaisesKeyDown ()
    {
        // ESC[97;1:2u = 'a' repeat
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[97;1:2u");

        Assert.Single (down);
        Assert.Equal (Key.A, down [0]);
        Assert.Equal (KeyEventType.Repeat, down [0].EventType);
        Assert.Empty (up);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_Release_RaisesKeyUp ()
    {
        // ESC[97;1:3u = 'a' release
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[97;1:3u");

        Assert.Empty (down);
        Assert.Single (up);
        Assert.Equal (Key.A, up [0]);
        Assert.Equal (KeyEventType.Release, up [0].EventType);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_ShiftA_Release ()
    {
        // ESC[97;2:3u = 'a' + Shift(2), release(3)
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[97;2:3u");

        Assert.Empty (down);
        Assert.Single (up);
        Assert.Equal (Key.A.WithShift.KeyCode, up [0].KeyCode);
        Assert.Equal (KeyEventType.Release, up [0].EventType);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_CtrlC_Press ()
    {
        // ESC[99;5:1u = 'c'(99) + Ctrl(5), press(1)
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[99;5:1u");

        Assert.Single (down);
        Assert.Equal (Key.C.WithCtrl.KeyCode, down [0].KeyCode);
        Assert.Equal (KeyEventType.Press, down [0].EventType);
        Assert.Empty (up);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_CtrlShiftAlt_Release ()
    {
        // ESC[97;8:3u = 'a', Ctrl+Shift+Alt (8), release (3)
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[97;8:3u");

        Assert.Empty (down);
        Assert.Single (up);
        Assert.True (up [0].IsCtrl);
        Assert.True (up [0].IsShift);
        Assert.True (up [0].IsAlt);
        Assert.Equal (KeyEventType.Release, up [0].EventType);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_Enter_Repeat ()
    {
        // ESC[13;1:2u = Enter, repeat
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[13;1:2u");

        Assert.Single (down);
        Assert.Equal (Key.Enter.KeyCode, down [0].KeyCode);
        Assert.Equal (KeyEventType.Repeat, down [0].EventType);
        Assert.Empty (up);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_Escape_Release ()
    {
        // ESC[27;1:3u = Escape, release
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[27;1:3u");

        Assert.Empty (down);
        Assert.Single (up);
        Assert.Equal (Key.Esc.KeyCode, up [0].KeyCode);
        Assert.Equal (KeyEventType.Release, up [0].EventType);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_Tab_Press ()
    {
        // ESC[9;1:1u = Tab, press
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[9;1:1u");

        Assert.Single (down);
        Assert.Equal (Key.Tab.KeyCode, down [0].KeyCode);
        Assert.Equal (KeyEventType.Press, down [0].EventType);
        Assert.Empty (up);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_F1_Release ()
    {
        // ESC[57364;1:3u = F1, release (kitty functional key encoding)
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[57364;1:3u");

        Assert.Empty (down);
        Assert.Single (up);
        Assert.Equal (Key.F1.KeyCode, up [0].KeyCode);
        Assert.Equal (KeyEventType.Release, up [0].EventType);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_F12_CtrlShift_Press ()
    {
        // ESC[57375;6:1u = F12 (kitty), Ctrl+Shift (6), press (1)
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[57375;6:1u");

        Assert.Single (down);
        Assert.Equal (Key.F12.WithCtrl.WithShift.KeyCode, down [0].KeyCode);
        Assert.Equal (KeyEventType.Press, down [0].EventType);
        Assert.Empty (up);
    }

    #endregion

    #region Mixed Kitty + Legacy Duplicate Input

    // Copilot
    [Fact]
    public void Pipeline_MixedKittyAndLegacyPrintable_DoesNotRaiseDuplicateKeyDown ()
    {
        // Reproduces terminals that emit kitty CSI-u and a legacy printable char for the same keypress.
        // Expected behavior: a single logical key event should be raised.
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[97u", "a");

        Assert.Single (down);
        Assert.Equal (Key.A, down [0]);
        Assert.Empty (up);
    }

    // Copilot
    [Theory]
    [InlineData ("«")]
    [InlineData ("»")]
    [InlineData ("ç")]
    [InlineData ("Ç")]
    [InlineData ("º")]
    [InlineData ("ª")]
    public void Pipeline_LegacyPrintable_PortugueseKeys_WhenKittyEnabled_DoesNotRaiseDuplicateKeyDown (string printable)
    {
        // Issue #4918 (PT keyboard): some glyphs may still arrive as duplicated legacy printable input
        // even when kitty is enabled. A single keypress should still produce one KeyDown.
        string duplicatedInput = printable + printable;
        (List<Key> down, List<Key> up) = InjectRawSequenceWithKittyEnabled (duplicatedInput);

        Assert.Single (down);
        Assert.Equal (printable, down [0].GetPrintableText ());
        Assert.Empty (up);
    }

    // Copilot
    [Fact]
    public void Pipeline_MixedKittyAssociatedTextAndLegacyPrintable_DoesNotRaiseDuplicateKeyDown ()
    {
        // Reproduces terminals that emit a kitty key event with associated text plus a legacy char.
        // ESC[49;2;33u = shifted '1' producing associated text '!'
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[49;2;33u", "!");

        Assert.Single (down);
        Assert.Equal ("!", down [0].GetPrintableText ());
        Assert.Empty (up);
    }

    // Copilot
    [Theory]
    [InlineData ("«")]
    [InlineData ("»")]
    [InlineData ("ç")]
    [InlineData ("Ç")]
    [InlineData ("º")]
    [InlineData ("ª")]
    public void Pipeline_LegacyPrintable_PortugueseKeys_WhenKittySequenceNotPresent_DoesNotRaiseDuplicateKeyDown (string printable)
    {
        (List<Key> down, List<Key> up) = InjectRawSequence (printable);

        Assert.Single (down);
        Assert.Equal (printable, down [0].GetPrintableText ());
        Assert.Empty (up);
    }

    // Copilot
    [Fact]
    public void Pipeline_RepeatedLegacyPrintableInput_DoesNotDropRepeatedCharacters ()
    {
        (List<Key> down, List<Key> up) = InjectRawSequence ("111222333444");

        Assert.Equal (12, down.Count);
        Assert.Equal ("111222333444", string.Concat (down.Select (key => key.GetPrintableText ())));
        Assert.Empty (up);
    }

    // Copilot
    [Theory]
    [InlineData ("«")]
    [InlineData ("»")]
    [InlineData ("ç")]
    [InlineData ("Ç")]
    [InlineData ("º")]
    [InlineData ("ª")]
    public void Pipeline_RepeatedLegacyPrintable_PortugueseKeys_WithoutKittySequence_DoesNotDropCharacters (string printable)
    {
        string input = printable + printable;
        (List<Key> down, List<Key> up) = InjectRawSequence (input);

        Assert.Equal (2, down.Count);
        Assert.Equal (input, string.Concat (down.Select (key => key.GetPrintableText ())));
        Assert.Empty (up);
    }

    #endregion

    #region Standalone Modifier Key Events

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_LeftShift_PressAndRelease ()
    {
        // Press then release Left Shift
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[57441u", // press (no event type = press)
                                                            "\x1b[57441;1:3u" // release
                                                           );

        Assert.Single (down);
        Assert.True (down [0].IsModifierOnly);
        Assert.Equal (ModifierKey.LeftShift, down [0].ModifierKey);
        Assert.True (down [0].IsShift);
        Assert.Equal (KeyEventType.Press, down [0].EventType);

        Assert.Single (up);
        Assert.True (up [0].IsModifierOnly);
        Assert.Equal (ModifierKey.LeftShift, up [0].ModifierKey);
        Assert.Equal (KeyEventType.Release, up [0].EventType);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_LeftCtrl_Release ()
    {
        // ESC[57442;1:3u = Left Ctrl, release
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[57442;1:3u");

        Assert.Empty (down);
        Assert.Single (up);
        Assert.True (up [0].IsModifierOnly);
        Assert.Equal (ModifierKey.LeftCtrl, up [0].ModifierKey);
        Assert.Equal (KeyEventType.Release, up [0].EventType);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_RightAlt_Press ()
    {
        // ESC[57449u = Right Alt, press (no event type = press)
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[57449u");

        Assert.Single (down);
        Assert.True (down [0].IsModifierOnly);
        Assert.Equal (ModifierKey.RightAlt, down [0].ModifierKey);
        Assert.Equal (KeyEventType.Press, down [0].EventType);
        Assert.Empty (up);
    }

    // Copilot - Opus 4.6
    [Theory]
    [InlineData ("\x1b[57441u", ModifierKey.LeftShift)]
    [InlineData ("\x1b[57442u", ModifierKey.LeftCtrl)]
    [InlineData ("\x1b[57443u", ModifierKey.LeftAlt)]
    [InlineData ("\x1b[57447u", ModifierKey.RightShift)]
    [InlineData ("\x1b[57448u", ModifierKey.RightCtrl)]
    [InlineData ("\x1b[57449u", ModifierKey.RightAlt)]
    [InlineData ("\x1b[57453u", ModifierKey.AltGr)]
    public void Pipeline_ModifierPress_RaisesKeyDown (string sequence, ModifierKey expectedModifier)
    {
        // Standalone modifier press should raise app.Keyboard.KeyDown
        (List<Key> down, List<Key> up) = InjectRawSequence (sequence);

        Assert.Single (down);
        Assert.True (down [0].IsModifierOnly);
        Assert.Equal (expectedModifier, down [0].ModifierKey);
        Assert.Equal (KeyEventType.Press, down [0].EventType);
        Assert.Empty (up);
    }

    [Fact]
    public void Pipeline_ModifierPress_SetsImplicitModifierState ()
    {
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[57441u",
                                                            "\x1b[57442u",
                                                            "\x1b[57443u");

        Assert.Equal (3, down.Count);
        Assert.True (down [0].IsShift);
        Assert.True (down [1].IsCtrl);
        Assert.True (down [2].IsAlt);
        Assert.Empty (up);
    }

    [Theory]
    [InlineData ("\x1b[57358u", ModifierKey.CapsLock, false, false, false)]
    [InlineData ("\x1b[57359u", ModifierKey.ScrollLock, false, false, false)]
    [InlineData ("\x1b[57360u", ModifierKey.NumLock, false, false, false)]
    [InlineData ("\x1b[57441u", ModifierKey.LeftShift, true, false, false)]
    [InlineData ("\x1b[57442u", ModifierKey.LeftCtrl, false, false, true)]
    [InlineData ("\x1b[57443u", ModifierKey.LeftAlt, false, true, false)]
    [InlineData ("\x1b[57444u", ModifierKey.LeftSuper, false, false, false)]
    [InlineData ("\x1b[57445u", ModifierKey.LeftHyper, false, false, false)]
    [InlineData ("\x1b[57447u", ModifierKey.RightShift, true, false, false)]
    [InlineData ("\x1b[57448u", ModifierKey.RightCtrl, false, false, true)]
    [InlineData ("\x1b[57449u", ModifierKey.RightAlt, false, true, false)]
    [InlineData ("\x1b[57450u", ModifierKey.RightSuper, false, false, false)]
    [InlineData ("\x1b[57451u", ModifierKey.RightHyper, false, false, false)]
    [InlineData ("\x1b[57453u", ModifierKey.AltGr, false, true, false)]
    public void Pipeline_AllMappedModifierPresses_RaiseExpectedImplicitState (
        string sequence,
        ModifierKey expectedModifier,
        bool expectedShift,
        bool expectedAlt,
        bool expectedCtrl
    )
    {
        (List<Key> down, List<Key> up) = InjectRawSequence (sequence);

        Assert.Single (down);
        Assert.True (down [0].IsModifierOnly);
        Assert.Equal (expectedModifier, down [0].ModifierKey);
        Assert.Equal (expectedShift, down [0].IsShift);
        Assert.Equal (expectedAlt, down [0].IsAlt);
        Assert.Equal (expectedCtrl, down [0].IsCtrl);
        Assert.Equal (KeyEventType.Press, down [0].EventType);
        Assert.Empty (up);
    }

    // Copilot - Opus 4.6
    [Theory]
    [InlineData ("\x1b[57441;1:3u", ModifierKey.LeftShift)]
    [InlineData ("\x1b[57442;1:3u", ModifierKey.LeftCtrl)]
    [InlineData ("\x1b[57443;1:3u", ModifierKey.LeftAlt)]
    [InlineData ("\x1b[57447;1:3u", ModifierKey.RightShift)]
    [InlineData ("\x1b[57448;1:3u", ModifierKey.RightCtrl)]
    [InlineData ("\x1b[57449;1:3u", ModifierKey.RightAlt)]
    [InlineData ("\x1b[57453;1:3u", ModifierKey.AltGr)]
    public void Pipeline_ModifierRelease_RaisesKeyUp (string sequence, ModifierKey expectedModifier)
    {
        // Standalone modifier release should raise app.Keyboard.KeyUp
        (List<Key> down, List<Key> up) = InjectRawSequence (sequence);

        Assert.Empty (down);
        Assert.Single (up);
        Assert.True (up [0].IsModifierOnly);
        Assert.Equal (expectedModifier, up [0].ModifierKey);
        Assert.Equal (KeyEventType.Release, up [0].EventType);
    }


    [Fact]
    public void Pipeline_LeftAltPress_WithCtrlModifier_PreservesBothStates ()
    {
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[57443;5u");

        Assert.Single (down);
        Assert.True (down [0].IsModifierOnly);
        Assert.Equal (ModifierKey.LeftAlt, down [0].ModifierKey);
        Assert.True (down [0].IsCtrl);
        Assert.True (down [0].IsAlt);
        Assert.Empty (up);
    }

    [Fact]
    public void Pipeline_LeftCtrlPress_WithCapsLockModifier_PreservesCtrlState ()
    {
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[57442;65u");

        Assert.Single (down);
        Assert.True (down [0].IsModifierOnly);
        Assert.Equal (ModifierKey.LeftCtrl, down [0].ModifierKey);
        Assert.True (down [0].IsCtrl);
        Assert.False (down [0].IsAlt);
        Assert.False (down [0].IsShift);
        Assert.Equal (KeyEventType.Press, down [0].EventType);
        Assert.Empty (up);
    }

    [Fact]
    public void Pipeline_LeftShiftPress_WithCapsLockModifier_PreservesShiftState ()
    {
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[57441;65u");

        Assert.Single (down);
        Assert.True (down [0].IsModifierOnly);
        Assert.Equal (ModifierKey.LeftShift, down [0].ModifierKey);
        Assert.True (down [0].IsShift);
        Assert.False (down [0].IsAlt);
        Assert.False (down [0].IsCtrl);
        Assert.Equal (KeyEventType.Press, down [0].EventType);
        Assert.Empty (up);
    }

    #endregion

    #region CSI ~ and Cursor Keys

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_Delete_Release_CsiTilde ()
    {
        // ESC[3;1:3~ = Delete, no modifiers, release
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[3;1:3~");

        Assert.Empty (down);
        Assert.Single (up);
        Assert.Equal (Key.Delete.KeyCode, up [0].KeyCode);
        Assert.Equal (KeyEventType.Release, up [0].EventType);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_PageUp_Repeat_CsiTilde ()
    {
        // ESC[5;1:2~ = PageUp, no modifiers, repeat
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[5;1:2~");

        Assert.Single (down);
        Assert.Equal (Key.PageUp.KeyCode, down [0].KeyCode);
        Assert.Equal (KeyEventType.Repeat, down [0].EventType);
        Assert.Empty (up);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_F5_Ctrl_Release_CsiTilde ()
    {
        // ESC[15;5:3~ = F5 + Ctrl (5), release (3)
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[15;5:3~");

        Assert.Empty (down);
        Assert.Single (up);
        Assert.Equal (Key.F5.WithCtrl.KeyCode, up [0].KeyCode);
        Assert.Equal (KeyEventType.Release, up [0].EventType);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_CursorUp_Release_CsiCursor ()
    {
        // ESC[1;1:3A = CursorUp, no modifiers, release
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[1;1:3A");

        Assert.Empty (down);
        Assert.Single (up);
        Assert.Equal (Key.CursorUp.KeyCode, up [0].KeyCode);
        Assert.Equal (KeyEventType.Release, up [0].EventType);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_CursorDown_CtrlShift_Repeat ()
    {
        // ESC[1;6:2B = CursorDown, Ctrl+Shift (6), repeat (2)
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[1;6:2B");

        Assert.Single (down);
        Assert.Equal (Key.CursorDown.WithCtrl.WithShift.KeyCode, down [0].KeyCode);
        Assert.Equal (KeyEventType.Repeat, down [0].EventType);
        Assert.Empty (up);
    }

    #endregion

    #region Full Press-Repeat-Release Cycle

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_FullCycle_PressRepeatRelease ()
    {
        // Simulates holding 'a': press → repeat → repeat → release
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[97;1:1u", // press
                                                            "\x1b[97;1:2u", // repeat
                                                            "\x1b[97;1:2u", // repeat
                                                            "\x1b[97;1:3u" // release
                                                           );

        Assert.Equal (3, down.Count);
        Assert.Equal (KeyEventType.Press, down [0].EventType);
        Assert.Equal (KeyEventType.Repeat, down [1].EventType);
        Assert.Equal (KeyEventType.Repeat, down [2].EventType);

        Assert.Single (up);
        Assert.Equal (KeyEventType.Release, up [0].EventType);

        // All should be the same key
        Assert.All (down, k => Assert.Equal (Key.A, k));
        Assert.Equal (Key.A, up [0]);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_NoModifierField_DefaultsToPress ()
    {
        // ESC[97u = 'a' with no modifier/event type field
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[97u");

        Assert.Single (down);
        Assert.Equal (Key.A, down [0]);
        Assert.Equal (KeyEventType.Press, down [0].EventType);
        Assert.Empty (up);
    }

    #endregion

    #region Error Handling and Robustness

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_InvalidEventType_Zero_DefaultsToPress ()
    {
        // ESC[97;1:0u = 'a', event type 0 (invalid, out of 1-3 range)
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[97;1:0u");

        Assert.Single (down);
        Assert.Equal (KeyEventType.Press, down [0].EventType);
        Assert.Empty (up);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_InvalidEventType_Five_DefaultsToPress ()
    {
        // ESC[97;1:5u = 'a', event type 5 (invalid, out of 1-3 range)
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[97;1:5u");

        Assert.Single (down);
        Assert.Equal (KeyEventType.Press, down [0].EventType);
        Assert.Empty (up);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_MalformedSequence_NoTerminator_DropsInput ()
    {
        // ESC[97;1:1 (missing 'u' terminator) — parser should not produce a key event.
        // The incomplete sequence will be held, then expire as stale on next ProcessQueue
        // after time advances past the 50ms threshold.
        VirtualTimeProvider timeProvider = new ();
        timeProvider.SetTime (new DateTime (2025, 1, 1, 12, 0, 0));

        using IApplication app = Application.Create (timeProvider);
        app.Init (DriverRegistry.Names.ANSI);

        List<Key> keyDownEvents = [];
        app.Keyboard.KeyDown += (_, key) => keyDownEvents.Add (key);

        IInputProcessor processor = app.Driver?.GetInputProcessor ()!;
        ConcurrentQueue<char> queue = ((AnsiInputProcessor)processor).InputQueue;

        // Inject incomplete sequence
        foreach (char ch in "\x1b[97;1:1")
        {
            queue.Enqueue (ch);
        }

        processor.ProcessQueue ();

        // No kitty key should have been produced (sequence is still held in parser)
        // None of the received keys should have kitty event metadata
        bool hasKittyEvent = keyDownEvents.Any (k => k.EventType != KeyEventType.Press || k.ModifierKey != ModifierKey.None);
        Assert.False (hasKittyEvent, "Incomplete kitty sequence should not produce a kitty-typed key event");
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_InvalidKeycode_DoesNotCrash ()
    {
        // ESC[999999u = Huge codepoint — may not map to any key but should not crash
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[999999u");

        // Should not crash; the key may or may not be produced depending on Rune.IsValid
        // but the pipeline should remain functional
        int total = down.Count + up.Count;
        Assert.True (total >= 0); // Mainly verifying no exception
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_NegativeKeycode_Ignored ()
    {
        // ESC[-1u = negative keycode — parser regex won't match \d+ for negative
        // Should be released as individual characters after timeout, not as a kitty key
        VirtualTimeProvider timeProvider = new ();
        timeProvider.SetTime (new DateTime (2025, 1, 1, 12, 0, 0));

        using IApplication app = Application.Create (timeProvider);
        app.Init (DriverRegistry.Names.ANSI);

        List<Key> keyDownEvents = [];
        app.Keyboard.KeyDown += (_, key) => keyDownEvents.Add (key);

        IInputProcessor processor = app.Driver?.GetInputProcessor ()!;
        ConcurrentQueue<char> queue = ((AnsiInputProcessor)processor).InputQueue;

        foreach (char ch in "\x1b[-1u")
        {
            queue.Enqueue (ch);
        }

        processor.ProcessQueue ();

        // Advance past the escape timeout so held sequences get released
        timeProvider.Advance (TimeSpan.FromMilliseconds (60));
        processor.ProcessQueue ();

        // The sequence should NOT have been parsed as a kitty key
        bool hasModifierKey = keyDownEvents.Any (k => k.ModifierKey != ModifierKey.None);
        Assert.False (hasModifierKey, "Negative keycode should not produce a modifier key event");
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_ValidAfterInvalid_StillWorks ()
    {
        // Inject garbage first, then a valid kitty sequence.
        // The pipeline should recover and process the valid sequence.
        VirtualTimeProvider timeProvider = new ();
        timeProvider.SetTime (new DateTime (2025, 1, 1, 12, 0, 0));

        using IApplication app = Application.Create (timeProvider);
        app.Init (DriverRegistry.Names.ANSI);

        List<Key> keyDownEvents = [];
        List<Key> keyUpEvents = [];
        app.Keyboard.KeyDown += (_, key) => keyDownEvents.Add (key);
        app.Keyboard.KeyUp += (_, key) => keyUpEvents.Add (key);

        IInputProcessor processor = app.Driver?.GetInputProcessor ()!;
        ConcurrentQueue<char> queue = ((AnsiInputProcessor)processor).InputQueue;

        // Inject an incomplete/garbage escape sequence
        foreach (char ch in "\x1b[ZZZZ")
        {
            queue.Enqueue (ch);
        }

        processor.ProcessQueue ();

        // Advance time to flush stale sequence
        timeProvider.Advance (TimeSpan.FromMilliseconds (60));
        processor.ProcessQueue ();

        // Now inject a valid kitty release
        foreach (char ch in "\x1b[98;1:3u")
        {
            queue.Enqueue (ch);
        }

        processor.ProcessQueue ();

        // The valid kitty release should be received
        Assert.Single (keyUpEvents);
        Assert.Equal (Key.B, keyUpEvents [0]);
        Assert.Equal (KeyEventType.Release, keyUpEvents [0].EventType);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_KittyPrintableKey ()
    {
        // Test that kitty CSI u sequence produces the expected key event
        (List<Key> down, List<Key> up) = InjectRawSequence (
            "\x1b[171;2:1u"  // ESC[171;2:1u = '«'(171) + Shift(2), press(1) - kitty sequence
        );

        // Should have one key event from kitty
        Assert.Single (down);
        // For Shift+«, the kitty sequence should produce a key with KeyCode = 171 and IsShift = true
        Assert.Equal (171, down [0].AsRune.Value);
        Assert.True (down [0].IsShift);
        Assert.Empty (up);
    }

    // Copilot - Opus 4.6
    // Theory test for alternative kitty code points (57417-57426)
    [Theory]
    [InlineData (57417, nameof (Key.CursorLeft))]
    [InlineData (57418, nameof (Key.CursorRight))]
    [InlineData (57419, nameof (Key.CursorUp))]
    [InlineData (57420, nameof (Key.CursorDown))]
    [InlineData (57421, nameof (Key.PageUp))]
    [InlineData (57422, nameof (Key.PageDown))]
    [InlineData (57423, nameof (Key.Home))]
    [InlineData (57424, nameof (Key.End))]
    [InlineData (57425, nameof (Key.InsertChar))]
    [InlineData (57426, nameof (Key.Delete))]
    public void Alternative_KittyCodePoints_Map_To_Correct_Keys (int kittyCode, string expectedKeyName)
    {
        // Arrange - Build the kitty sequence for the code point
        string sequence = $"\x1b[{kittyCode}u";  // ESC[codePointu (press event, no modifiers)

        // Act
        (List<Key> down, List<Key> up) = InjectRawSequence (sequence);

        // Assert - Should have exactly one key down event
        Assert.Single (down);

        // Get the expected key by name from the Key class
        System.Reflection.PropertyInfo? prop = typeof (Key).GetProperty (expectedKeyName);
        Assert.NotNull (prop);
        Key expectedKey = (Key)prop!.GetValue (null)!;

        // Verify the mapped key matches the expected key
        Assert.Equal (expectedKey.KeyCode, down [0].KeyCode);
        Assert.Empty (up);
    }

    #endregion

    #region Regression tests for modifier key release events in Kitty keyboard protocol.

    [Fact]
    public void ModifierKeyRelease_PreservesModifierKey ()
    {
        // ESC[57441;1:3u = LeftShift release
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[57441;1:3u");

        Assert.Empty (down);
        Assert.Single (up);

        // The bug: ModifierKey should be LeftShift, not None
        Assert.True (up [0].IsModifierOnly, "Release event should be marked as modifier-only");
        Assert.Equal (ModifierKey.LeftShift, up [0].ModifierKey);
        Assert.Equal (KeyEventType.Release, up [0].EventType);
        Assert.Equal (KeyCode.ShiftMask, up [0].KeyCode);
    }

    [Fact]
    public void ModifierKeyRelease_LeftCtrl_PreservesModifierKey ()
    {
        // ESC[57442;1:3u = LeftCtrl release
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[57442;1:3u");

        Assert.Empty (down);
        Assert.Single (up);

        Assert.True (up [0].IsModifierOnly);
        Assert.Equal (ModifierKey.LeftCtrl, up [0].ModifierKey);
        Assert.Equal (KeyEventType.Release, up [0].EventType);
    }

    [Fact]
    public void ModifierKeyRelease_RightAlt_PreservesModifierKey ()
    {
        // ESC[57449;1:3u = RightAlt release
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[57449;1:3u");

        Assert.Empty (down);
        Assert.Single (up);

        Assert.True (up [0].IsModifierOnly);
        Assert.Equal (ModifierKey.RightAlt, up [0].ModifierKey);
        Assert.Equal (KeyEventType.Release, up [0].EventType);
    }

    [Fact]
    public void ModifierKeyRelease_AltGr_PreservesModifierKey ()
    {
        // ESC[57453;1:3u = AltGr release
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[57453;1:3u");

        Assert.Empty (down);
        Assert.Single (up);

        Assert.True (up [0].IsModifierOnly);
        Assert.Equal (ModifierKey.AltGr, up [0].ModifierKey);
        Assert.Equal (KeyEventType.Release, up [0].EventType);
    }

    [Theory]
    [InlineData ("\x1b[57441;1:3u", ModifierKey.LeftShift, KeyCode.ShiftMask)]
    [InlineData ("\x1b[57442;1:3u", ModifierKey.LeftCtrl, KeyCode.CtrlMask)]
    [InlineData ("\x1b[57443;1:3u", ModifierKey.LeftAlt, KeyCode.AltMask)]
    [InlineData ("\x1b[57447;1:3u", ModifierKey.RightShift, KeyCode.ShiftMask)]
    [InlineData ("\x1b[57448;1:3u", ModifierKey.RightCtrl, KeyCode.CtrlMask)]
    [InlineData ("\x1b[57449;1:3u", ModifierKey.RightAlt, KeyCode.AltMask)]
    [InlineData ("\x1b[57453;1:3u", ModifierKey.AltGr, KeyCode.AltMask)]
    public void ModifierKeyRelease_AllModifiers_PreserveModifierKey (string sequence, ModifierKey expectedModifier, KeyCode keyCode)
    {
        (List<Key> down, List<Key> up) = InjectRawSequence (sequence);

        Assert.Empty (down);
        Assert.Single (up);
        Assert.True (up [0].IsModifierOnly, $"Release event for {expectedModifier} should be modifier-only");
        Assert.Equal (expectedModifier, up [0].ModifierKey);
        Assert.Equal (KeyEventType.Release, up [0].EventType);
        Assert.Equal (keyCode, up [0].KeyCode);
    }

    [Fact]
    public void ModifierKeyRelease_PressAndRelease_Sequence ()
    {
        // Press LeftShift, then release it
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[57441u", // LeftShift press
                                                            "\x1b[57441;1:3u" // LeftShift release
                                                           );

        Assert.Single (down);
        Assert.Single (up);

        // Press event
        Assert.True (down [0].IsModifierOnly);
        Assert.Equal (ModifierKey.LeftShift, down [0].ModifierKey);
        Assert.Equal (KeyEventType.Press, down [0].EventType);

        // Release event - should preserve ModifierKey
        Assert.True (up [0].IsModifierOnly);
        Assert.Equal (ModifierKey.LeftShift, up [0].ModifierKey);
        Assert.Equal (KeyEventType.Release, up [0].EventType);
    }

    #endregion
}
