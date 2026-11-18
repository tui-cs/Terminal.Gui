// Copilot - Opus 4.6

namespace DriverTests.AnsiHandling;

/// <summary>
///     Unit tests for kitty keyboard protocol ANSI escape sequence parsing.
///     Tests the <see cref="KittyKeyboardPattern"/>, <see cref="CsiKeyPattern"/>,
///     and <see cref="CsiCursorPattern"/> parsers directly via <c>GetKey()</c>.
/// </summary>
public class KittyKeyboardParsingTests
{
    private readonly KittyKeyboardPattern _pattern = new ();

    #region Event Type Parsing

    [Fact]
    public void KittyPattern_NoEventType_DefaultsToPress ()
    {
        // ESC[97u = 'a' with no modifier field
        Key? key = _pattern.GetKey ("\u001b[97u");

        Assert.NotNull (key);
        Assert.Equal (KeyEventType.Press, key.EventType);
    }

    [Fact]
    public void KittyPattern_EventType_Press ()
    {
        // ESC[97;1:1u = 'a', no modifiers, event type 1 (press)
        Key? key = _pattern.GetKey ("\u001b[97;1:1u");

        Assert.NotNull (key);
        Assert.Equal (KeyEventType.Press, key.EventType);
        Assert.Equal (Key.A, key.NoShift.NoCtrl.NoAlt);
    }

    [Fact]
    public void KittyPattern_EventType_Repeat ()
    {
        // ESC[97;1:2u = 'a', no modifiers, event type 2 (repeat)
        Key? key = _pattern.GetKey ("\u001b[97;1:2u");

        Assert.NotNull (key);
        Assert.Equal (KeyEventType.Repeat, key.EventType);
    }

    [Fact]
    public void KittyPattern_EventType_Release ()
    {
        // ESC[97;1:3u = 'a', no modifiers, event type 3 (release)
        Key? key = _pattern.GetKey ("\u001b[97;1:3u");

        Assert.NotNull (key);
        Assert.Equal (KeyEventType.Release, key.EventType);
    }

    [Fact]
    public void KittyPattern_EventType_WithModifiers ()
    {
        // ESC[97;4:3u = 'a', Shift+Alt (modifiers=4 → 3=Shift|Alt), event type 3 (release)
        Key? key = _pattern.GetKey ("\u001b[97;4:3u");

        Assert.NotNull (key);
        Assert.Equal (KeyEventType.Release, key.EventType);
        Assert.True (key.IsShift);
        Assert.True (key.IsAlt);
    }

    [Fact]
    public void KittyPattern_EventType_CtrlWithRelease ()
    {
        // ESC[97;5:3u = 'a', Ctrl (modifiers=5 → 4=Ctrl), event type 3 (release)
        Key? key = _pattern.GetKey ("\u001b[97;5:3u");

        Assert.NotNull (key);
        Assert.Equal (KeyEventType.Release, key.EventType);
        Assert.True (key.IsCtrl);
    }

    [Fact]
    public void KittyPattern_FunctionKey_EventType_Release ()
    {
        // ESC[57364;1:3u = F1, no modifiers, event type 3 (release)
        Key? key = _pattern.GetKey ("\u001b[57364;1:3u");

        Assert.NotNull (key);
        Assert.Equal (Key.F1.KeyCode, key.KeyCode);
        Assert.Equal (KeyEventType.Release, key.EventType);
    }

    [Fact]
    public void KittyPattern_Enter_EventType_Repeat ()
    {
        // ESC[13;1:2u = Enter, no modifiers, event type 2 (repeat)
        Key? key = _pattern.GetKey ("\u001b[13;1:2u");

        Assert.NotNull (key);
        Assert.Equal (Key.Enter.KeyCode, key.KeyCode);
        Assert.Equal (KeyEventType.Repeat, key.EventType);
    }

    [Fact]
    public void KittyPattern_ModifiersOnly_NoEventType_DefaultsToPress ()
    {
        // ESC[97;2u = 'a' with Shift modifier, no event type → defaults to press
        Key? key = _pattern.GetKey ("\u001b[97;2u");

        Assert.NotNull (key);
        Assert.Equal (KeyEventType.Press, key.EventType);
        Assert.True (key.IsShift);
    }

    [Fact]
    public void KittyPattern_InvalidEventType_DefaultsToPress ()
    {
        // ESC[97;1:5u = 'a', event type 5 (invalid, out of 1-3 range)
        Key? key = _pattern.GetKey ("\u001b[97;1:5u");

        Assert.NotNull (key);
        Assert.Equal (KeyEventType.Press, key.EventType);
    }

    #endregion

    #region Standalone Modifier Key Events

    [Fact]
    public void KittyPattern_LeftShift_Standalone ()
    {
        // ESC[57441u = Left Shift
        Key? key = _pattern.GetKey ("\u001b[57441u");

        Assert.NotNull (key);
        Assert.True (key.IsModifierOnly);
        Assert.Equal (ModifierKey.LeftShift, key.ModifierKey);
        Assert.True (key.IsShift);
    }

    [Fact]
    public void KittyPattern_LeftCtrl_Standalone ()
    {
        // ESC[57442u = Left Ctrl
        Key? key = _pattern.GetKey ("\u001b[57442u");

        Assert.NotNull (key);
        Assert.True (key.IsModifierOnly);
        Assert.Equal (ModifierKey.LeftCtrl, key.ModifierKey);
        Assert.True (key.IsCtrl);
    }

    [Fact]
    public void KittyPattern_LeftAlt_Standalone ()
    {
        // ESC[57443u = Left Alt
        Key? key = _pattern.GetKey ("\u001b[57443u");

        Assert.NotNull (key);
        Assert.True (key.IsModifierOnly);
        Assert.Equal (ModifierKey.LeftAlt, key.ModifierKey);
        Assert.True (key.IsAlt);
    }

    [Fact]
    public void KittyPattern_RightShift_Standalone ()
    {
        // ESC[57447u = Right Shift
        Key? key = _pattern.GetKey ("\u001b[57447u");

        Assert.NotNull (key);
        Assert.True (key.IsModifierOnly);
        Assert.Equal (ModifierKey.RightShift, key.ModifierKey);
    }

    [Fact]
    public void KittyPattern_RightCtrl_Standalone ()
    {
        // ESC[57448u = Right Ctrl
        Key? key = _pattern.GetKey ("\u001b[57448u");

        Assert.NotNull (key);
        Assert.True (key.IsModifierOnly);
        Assert.Equal (ModifierKey.RightCtrl, key.ModifierKey);
    }

    [Fact]
    public void KittyPattern_RightAlt_Standalone ()
    {
        // ESC[57449u = Right Alt
        Key? key = _pattern.GetKey ("\u001b[57449u");

        Assert.NotNull (key);
        Assert.True (key.IsModifierOnly);
        Assert.Equal (ModifierKey.RightAlt, key.ModifierKey);
    }

    [Fact]
    public void KittyPattern_AltGr_Standalone ()
    {
        // ESC[57453u = AltGr / ISO_Level3_Shift
        Key? key = _pattern.GetKey ("\u001b[57453u");

        Assert.NotNull (key);
        Assert.True (key.IsModifierOnly);
        Assert.Equal (ModifierKey.AltGr, key.ModifierKey);
    }

    [Fact]
    public void KittyPattern_CapsLock_Standalone ()
    {
        // ESC[57358u = Caps Lock
        Key? key = _pattern.GetKey ("\u001b[57358u");

        Assert.NotNull (key);
        Assert.True (key.IsModifierOnly);
        Assert.Equal (ModifierKey.CapsLock, key.ModifierKey);
    }

    [Fact]
    public void KittyPattern_NumLock_Standalone ()
    {
        // ESC[57360u = Num Lock
        Key? key = _pattern.GetKey ("\u001b[57360u");

        Assert.NotNull (key);
        Assert.True (key.IsModifierOnly);
        Assert.Equal (ModifierKey.NumLock, key.ModifierKey);
    }

    [Fact]
    public void KittyPattern_ModifierKey_WithEventType_Release ()
    {
        // ESC[57441;1:3u = Left Shift, event type 3 (release)
        Key? key = _pattern.GetKey ("\u001b[57441;1:3u");

        Assert.NotNull (key);
        Assert.True (key.IsModifierOnly);
        Assert.Equal (ModifierKey.LeftShift, key.ModifierKey);
        Assert.Equal (KeyEventType.Release, key.EventType);
    }

    [Fact]
    public void KittyPattern_AltGr_WithEventType_Release ()
    {
        // ESC[57453;1:3u = AltGr / ISO_Level3_Shift, event type 3 (release)
        Key? key = _pattern.GetKey ("\u001b[57453;1:3u");

        Assert.NotNull (key);
        Assert.True (key.IsModifierOnly);
        Assert.Equal (ModifierKey.AltGr, key.ModifierKey);
        Assert.Equal (KeyEventType.Release, key.EventType);
    }

    [Fact]
    public void KittyPattern_LeftAlt_WithCtrlModifier_PreservesBothStates ()
    {
        // ESC[57443;5u = LeftAlt (implicit Alt) with Ctrl held (explicit Ctrl=5)
        // After the fix, implicit Alt is combined with explicit Ctrl
        Key? key = _pattern.GetKey ("\u001b[57443;5u");

        Assert.NotNull (key);
        Assert.True (key.IsModifierOnly);
        Assert.Equal (ModifierKey.LeftAlt, key.ModifierKey);
        Assert.True (key.IsCtrl);
        Assert.True (key.IsAlt);
        Assert.Equal (KeyEventType.Press, key.EventType);
    }

    [Fact]
    public void KittyPattern_LeftAlt_Release_WithCtrlAndAltModifiers_PreservesBothStates ()
    {
        Key? key = _pattern.GetKey ("\u001b[57443;7:3u");

        Assert.NotNull (key);
        Assert.True (key.IsModifierOnly);
        Assert.Equal (ModifierKey.LeftAlt, key.ModifierKey);
        Assert.True (key.IsCtrl);
        Assert.True (key.IsAlt);
        Assert.Equal (KeyEventType.Release, key.EventType);
    }

    [Fact]
    public void KittyPattern_LeftCtrl_Release_WithCtrlModifier_PreservesState ()
    {
        Key? key = _pattern.GetKey ("\u001b[57442;5:3u");

        Assert.NotNull (key);
        Assert.True (key.IsModifierOnly);
        Assert.Equal (ModifierKey.LeftCtrl, key.ModifierKey);
        Assert.True (key.IsCtrl);
        Assert.False (key.IsAlt);
        Assert.Equal (KeyEventType.Release, key.EventType);
    }

    [Fact]
    public void KittyPattern_LeftCtrl_WithCapsLockModifier_PreservesCtrlState ()
    {
        Key? key = _pattern.GetKey ("\u001b[57442;65u");

        Assert.NotNull (key);
        Assert.True (key.IsModifierOnly);
        Assert.Equal (ModifierKey.LeftCtrl, key.ModifierKey);
        Assert.True (key.IsCtrl);
        Assert.False (key.IsAlt);
        Assert.False (key.IsShift);
        Assert.Equal (KeyEventType.Press, key.EventType);
    }

    [Fact]
    public void KittyPattern_LeftShift_WithCapsLockModifier_PreservesShiftState ()
    {
        Key? key = _pattern.GetKey ("\u001b[57441;65u");

        Assert.NotNull (key);
        Assert.True (key.IsModifierOnly);
        Assert.Equal (ModifierKey.LeftShift, key.ModifierKey);
        Assert.True (key.IsShift);
        Assert.False (key.IsAlt);
        Assert.False (key.IsCtrl);
        Assert.Equal (KeyEventType.Press, key.EventType);
    }

    // Regression test for issue where modifier combinations weren't being combined correctly
    [Fact]
    public void KittyPattern_LeftCtrl_WithShiftModifier_CombinesImplicitAndExplicit ()
    {
        // ESC[57442;2u = LeftCtrl (implicit Ctrl) with Shift held (explicit Shift=2)
        // Should combine to Ctrl+Shift, not just Shift
        Key? key = _pattern.GetKey ("\u001b[57442;2u");

        Assert.NotNull (key);
        Assert.True (key.IsModifierOnly);
        Assert.Equal (ModifierKey.LeftCtrl, key.ModifierKey);
        Assert.True (key.IsCtrl);
        Assert.True (key.IsShift);
        Assert.False (key.IsAlt);
        Assert.Equal (KeyEventType.Press, key.EventType);
    }

    // Regression test for issue where modifier combinations weren't being combined correctly
    [Fact]
    public void KittyPattern_LeftAlt_WithShiftAndCtrlModifiers_CombinesAllModifiers ()
    {
        // ESC[57443;6u = LeftAlt (implicit Alt) with Shift+Ctrl held (explicit Shift+Ctrl=6)
        // Should combine to Alt+Shift+Ctrl, not just Shift+Ctrl
        Key? key = _pattern.GetKey ("\u001b[57443;6u");

        Assert.NotNull (key);
        Assert.True (key.IsModifierOnly);
        Assert.Equal (ModifierKey.LeftAlt, key.ModifierKey);
        Assert.True (key.IsAlt);
        Assert.True (key.IsShift);
        Assert.True (key.IsCtrl);
        Assert.Equal (KeyEventType.Press, key.EventType);
    }

    [Fact]
    public void KittyPattern_NonModifierKey_IsNotModifierOnly ()
    {
        // ESC[97u = 'a'
        Key? key = _pattern.GetKey ("\u001b[97u");

        Assert.NotNull (key);
        Assert.False (key.IsModifierOnly);
        Assert.Equal (ModifierKey.None, key.ModifierKey);
    }

    [Fact]
    public void KittyPattern_LeftSuper_Standalone ()
    {
        // ESC[57444u = Left Super
        Key? key = _pattern.GetKey ("\u001b[57444u");

        Assert.NotNull (key);
        Assert.True (key.IsModifierOnly);
        Assert.Equal (ModifierKey.LeftSuper, key.ModifierKey);
    }

    [Theory]
    [InlineData ("\u001b[57358u", ModifierKey.CapsLock, false, false, false)]
    [InlineData ("\u001b[57359u", ModifierKey.ScrollLock, false, false, false)]
    [InlineData ("\u001b[57360u", ModifierKey.NumLock, false, false, false)]
    [InlineData ("\u001b[57441u", ModifierKey.LeftShift, true, false, false)]
    [InlineData ("\u001b[57442u", ModifierKey.LeftCtrl, false, false, true)]
    [InlineData ("\u001b[57443u", ModifierKey.LeftAlt, false, true, false)]
    [InlineData ("\u001b[57444u", ModifierKey.LeftSuper, false, false, false)]
    [InlineData ("\u001b[57445u", ModifierKey.LeftHyper, false, false, false)]
    [InlineData ("\u001b[57447u", ModifierKey.RightShift, true, false, false)]
    [InlineData ("\u001b[57448u", ModifierKey.RightCtrl, false, false, true)]
    [InlineData ("\u001b[57449u", ModifierKey.RightAlt, false, true, false)]
    [InlineData ("\u001b[57450u", ModifierKey.RightSuper, false, false, false)]
    [InlineData ("\u001b[57451u", ModifierKey.RightHyper, false, false, false)]
    [InlineData ("\u001b[57453u", ModifierKey.AltGr, false, true, false)]
    public void KittyPattern_AllMappedModifierPresses_ParseWithExpectedImplicitState (
        string sequence,
        ModifierKey expectedModifier,
        bool expectedShift,
        bool expectedAlt,
        bool expectedCtrl
    )
    {
        Key? key = _pattern.GetKey (sequence);

        Assert.NotNull (key);
        Assert.True (key.IsModifierOnly);
        Assert.Equal (expectedModifier, key.ModifierKey);
        Assert.Equal (expectedShift, key.IsShift);
        Assert.Equal (expectedAlt, key.IsAlt);
        Assert.Equal (expectedCtrl, key.IsCtrl);
        Assert.Equal (KeyEventType.Press, key.EventType);
    }

    #endregion

    #region CSI ~ and Cursor Key Event Types

    [Fact]
    public void CsiKeyPattern_Delete_WithRelease ()
    {
        // ESC[3;1:3~ = Delete, no modifiers, event type 3 (release)
        CsiKeyPattern pattern = new ();
        Key? key = pattern.GetKey ("\u001b[3;1:3~");

        Assert.NotNull (key);
        Assert.Equal (Key.Delete.KeyCode, key.KeyCode);
        Assert.Equal (KeyEventType.Release, key.EventType);
    }

    [Fact]
    public void CsiKeyPattern_Delete_WithRepeat ()
    {
        // ESC[3;1:2~ = Delete, no modifiers, event type 2 (repeat)
        CsiKeyPattern pattern = new ();
        Key? key = pattern.GetKey ("\u001b[3;1:2~");

        Assert.NotNull (key);
        Assert.Equal (Key.Delete.KeyCode, key.KeyCode);
        Assert.Equal (KeyEventType.Repeat, key.EventType);
    }

    [Fact]
    public void CsiKeyPattern_F5_WithCtrl_Release ()
    {
        // ESC[15;5:3~ = F5, Ctrl (5), event type 3 (release)
        CsiKeyPattern pattern = new ();
        Key? key = pattern.GetKey ("\u001b[15;5:3~");

        Assert.NotNull (key);
        Assert.Equal (Key.F5.WithCtrl.KeyCode, key.KeyCode);
        Assert.Equal (KeyEventType.Release, key.EventType);
    }

    [Fact]
    public void CsiKeyPattern_Delete_NoEventType_DefaultsToPress ()
    {
        // ESC[3~ = Delete, no modifiers, no event type
        CsiKeyPattern pattern = new ();
        Key? key = pattern.GetKey ("\u001b[3~");

        Assert.NotNull (key);
        Assert.Equal (Key.Delete.KeyCode, key.KeyCode);
        Assert.Equal (KeyEventType.Press, key.EventType);
    }

    [Fact]
    public void CsiCursorPattern_CursorUp_WithRelease ()
    {
        // ESC[1;1:3A = CursorUp, no modifiers, event type 3 (release)
        CsiCursorPattern pattern = new ();
        Key? key = pattern.GetKey ("\u001b[1;1:3A");

        Assert.NotNull (key);
        Assert.Equal (Key.CursorUp.KeyCode, key.KeyCode);
        Assert.Equal (KeyEventType.Release, key.EventType);
    }

    [Fact]
    public void CsiCursorPattern_CursorDown_WithCtrl_Repeat ()
    {
        // ESC[1;5:2B = CursorDown, Ctrl (5), event type 2 (repeat)
        CsiCursorPattern pattern = new ();
        Key? key = pattern.GetKey ("\u001b[1;5:2B");

        Assert.NotNull (key);
        Assert.Equal (Key.CursorDown.WithCtrl.KeyCode, key.KeyCode);
        Assert.Equal (KeyEventType.Repeat, key.EventType);
    }

    [Fact]
    public void CsiCursorPattern_CursorRight_NoEventType_DefaultsToPress ()
    {
        // ESC[C = CursorRight, no modifier, no event type
        CsiCursorPattern pattern = new ();
        Key? key = pattern.GetKey ("\u001b[C");

        Assert.NotNull (key);
        Assert.Equal (Key.CursorRight.KeyCode, key.KeyCode);
        Assert.Equal (KeyEventType.Press, key.EventType);
    }

    #endregion

    #region Alternate Key Parsing

    // Copilot - Opus 4.6
    [Fact]
    public void KittyPattern_AlternateKeys_ShiftedOnly ()
    {
        // Previous fixture ESC[97:64u was misleading because 'a' -> '@' is not a sensible US-keyboard example.
        // ESC[50:64u = '2' with shifted key '@' (codepoint 64)
        Key? key = _pattern.GetKey ("\u001b[50:64u");

        Assert.NotNull (key);
        Assert.Equal (Key.D2, key);
        Assert.Equal ((KeyCode)64, key.ShiftedKeyCode);
        Assert.Equal (KeyCode.Null, key.BaseLayoutKeyCode);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void KittyPattern_AlternateKeys_ShiftedAndBaseLayout ()
    {
        // Previous fixture ESC[97:64:50u was internally inconsistent for a US-keyboard example.
        // ESC[50:64:50u = '2' with shifted key '@' (64) and base layout key '2' (50)
        Key? key = _pattern.GetKey ("\u001b[50:64:50u");

        Assert.NotNull (key);
        Assert.Equal (Key.D2, key);
        Assert.Equal ((KeyCode)64, key.ShiftedKeyCode);
        Assert.Equal ((KeyCode)50, key.BaseLayoutKeyCode);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void KittyPattern_AlternateKeys_BaseLayoutOnly ()
    {
        // ESC[50:0:50u = '2' with no shifted key (0) and base layout key '2' (50)
        Key? key = _pattern.GetKey ("\u001b[50:0:50u");

        Assert.NotNull (key);
        Assert.Equal (Key.D2, key);
        Assert.Equal (KeyCode.Null, key.ShiftedKeyCode);
        Assert.Equal ((KeyCode)50, key.BaseLayoutKeyCode);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void KittyPattern_NoAlternateKeys_DefaultsToNull ()
    {
        // ESC[97u = 'a' with no alternate key fields
        Key? key = _pattern.GetKey ("\u001b[97u");

        Assert.NotNull (key);
        Assert.Equal (Key.A, key);
        Assert.Equal (KeyCode.Null, key.ShiftedKeyCode);
        Assert.Equal (KeyCode.Null, key.BaseLayoutKeyCode);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void KittyPattern_AlternateKeys_WithModifiers ()
    {
        // Previous fixture ESC[97:64:50;2u was misleading because 'a', '@', and US-base '2' do not describe
        // a coherent US-keyboard case.
        // ESC[50:64;2;64u = '2' with shifted key '@' (64), Shift modifier (2),
        // and associated text '@' (64). The redundant base-layout field is omitted because
        // it would duplicate the primary key code on a US keyboard.
        Key? key = _pattern.GetKey ("\u001b[50:64;2;64u");

        Assert.NotNull (key);
        Assert.Equal ("@", key.AsGrapheme);
        Assert.Equal (new Key ('@').WithShift, key);
        Assert.Equal ((KeyCode)64, key.ShiftedKeyCode);
        Assert.Equal (KeyCode.Null, key.BaseLayoutKeyCode);
        Assert.Equal ("@", key.AssociatedText);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void KittyPattern_AlternateKeys_WithModifiersAndEventType ()
    {
        // ESC[50:64;2:3;64u = same coherent US-keyboard example, release event
        Key? key = _pattern.GetKey ("\u001b[50:64;2:3;64u");

        Assert.NotNull (key);
        Assert.Equal ("@", key.AsGrapheme);
        Assert.Equal (new Key ('@').WithShift, key);
        Assert.Equal ((KeyCode)64, key.ShiftedKeyCode);
        Assert.Equal (KeyCode.Null, key.BaseLayoutKeyCode);
        Assert.Equal (KeyEventType.Release, key.EventType);
        Assert.Equal ("@", key.AssociatedText);
    }

    [Fact]
    public void KittyPattern_AssociatedText_ShiftedPrintableKey ()
    {
        // ESC[49;2;33u = physical '1' key with Shift modifier and associated text '!'
        Key? key = _pattern.GetKey ("\u001b[49;2;33u");

        Assert.NotNull (key);
        Assert.Equal (new Key ('!').WithShift, key);
        Assert.Equal ("!", key.AssociatedText);
        Assert.Equal ("!", key.GetPrintableText ());
    }

    [Fact]
    public void KittyPattern_AssociatedText_AltModifiedPrintableKey_IsSuppressed ()
    {
        // ESC[116;3;116u = Alt+t with associated text 't'
        Key? key = _pattern.GetKey ("\u001b[116;3;116u");

        Assert.NotNull (key);
        Assert.True (key.IsAlt);
        Assert.Equal (Key.T.WithAlt, key);
        Assert.Equal (Key.T.WithAlt.KeyCode, key.KeyCode);
        Assert.Equal (string.Empty, key.AssociatedText);
        Assert.Equal (string.Empty, key.GetPrintableText ());
        Assert.Equal (string.Empty, key.AsGrapheme);
        Assert.Equal (0, key.AsRune.Value);
    }

    [Fact]
    public void KittyPattern_AssociatedText_ShiftAltModifiedPrintableKey_IsSuppressed ()
    {
        // ESC[116:84;4;84u = Shift+Alt+T with shifted key 'T' and associated text 'T'
        Key? key = _pattern.GetKey ("\u001b[116:84;4;84u");

        Assert.NotNull (key);
        Assert.True (key.IsShift);
        Assert.True (key.IsAlt);
        Assert.Equal (Key.T.WithShift.WithAlt, key);
        Assert.Equal (Key.T.WithShift.WithAlt.KeyCode, key.KeyCode);
        Assert.Equal ((KeyCode)'T', key.ShiftedKeyCode);
        Assert.Equal (string.Empty, key.AssociatedText);
        Assert.Equal (string.Empty, key.GetPrintableText ());
        Assert.Equal (string.Empty, key.AsGrapheme);
        Assert.Equal (0, key.AsRune.Value);
    }

    [Fact]
    public void KittyPattern_AssociatedText_MultipleCodePoints ()
    {
        // ESC[97;1;769:97u = associated text composed of combining acute accent + 'a'
        Key? key = _pattern.GetKey ("\u001b[97;1;769:97u");

        Assert.NotNull (key);
        Assert.Equal ("\u0301a", key.AssociatedText);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void KittyPattern_AlternateKeys_FunctionalKey ()
    {
        // ESC[13:0:13u = Enter with no shifted key, base layout Enter
        Key? key = _pattern.GetKey ("\u001b[13:0:13u");

        Assert.NotNull (key);
        Assert.Equal (Key.Enter, key);
        Assert.Equal (KeyCode.Null, key.ShiftedKeyCode);
        Assert.Equal ((KeyCode)13, key.BaseLayoutKeyCode);
    }

    [Fact]
    public void KittyPattern_EmptyFields_WithAssociatedText ()
    {
        // ESC[64::50;;64u = '@' with empty shifted field, base layout '2', empty modifiers, associated text '@'
        Key? key = _pattern.GetKey ("\u001b[64::50;;64u");

        Assert.NotNull (key);
        Assert.Equal (new Key ('@'), key);
        Assert.Equal (KeyCode.Null, key.ShiftedKeyCode);
        Assert.Equal ((KeyCode)50, key.BaseLayoutKeyCode);
        Assert.Equal ("@", key.AssociatedText);
    }

    [Fact]
    public void KittyPattern_AltGr5_Press_ReturnsEuroGrapheme ()
    {
        // ESC[8364;1:1u = Euro symbol, no modifiers, press
        Key? key = _pattern.GetKey ("\u001b[8364;1:1u");

        Assert.NotNull (key);
        Assert.Equal (KeyEventType.Press, key.EventType);
        Assert.Equal ((KeyCode)8364, key.KeyCode);
        Assert.Equal ("€", key.AsGrapheme);
        Assert.Equal ("€", key.GetPrintableText ());
    }

    [Fact]
    public void KittyPattern_AltGrE_EuroKey_Press_ReturnsEuroGrapheme ()
    {
        // ESC[8364;1:1u = AltGr+E on many layouts, which sends the euro codepoint 8364.
        Key? key = _pattern.GetKey ("\u001b[8364;1:1u");

        Assert.NotNull (key);
        Assert.Equal (KeyEventType.Press, key.EventType);
        Assert.Equal ((KeyCode)8364, key.KeyCode);
        Assert.Equal ("€", key.AsGrapheme);
        Assert.Equal ("€", key.GetPrintableText ());
    }

    [Fact]
    public void KittyPattern_AltGrE_EuroKey_Release_ReturnsEuroGrapheme ()
    {
        // ESC[8364;1:3u = Euro symbol, no modifiers, release
        Key? key = _pattern.GetKey ("\u001b[8364;1:3u");

        Assert.NotNull (key);
        Assert.Equal (KeyEventType.Release, key.EventType);
        Assert.Equal ((KeyCode)8364, key.KeyCode);
        Assert.Equal ("€", key.AsGrapheme);
        Assert.Equal ("€", key.GetPrintableText ());
    }

    #endregion

    #region Kitty Flags Validation

    // Copilot - Opus 4.6
    [Fact]
    public void KittyRequestedFlags_IncludesReportAllKeys () =>

        // The kitty spec requires flag 8 (report all keys as escape codes) for the terminal
        // to send standalone modifier key events (e.g., pressing Shift alone).
        // Without this flag, terminals like Windows Terminal won't report modifier-only presses.
        Assert.True (EscSeqUtils.KittyKeyboardRequestedFlags.HasFlag (KittyKeyboardFlags.ReportAllKeysAsEscapeCodes),
                     $"KittyKeyboardRequestedFlags ({EscSeqUtils.KittyKeyboardRequestedFlags}) must include ReportAllKeysAsEscapeCodes "
                     + "to receive standalone modifier key events from the terminal.");

    // Copilot - Opus 4.6
    [Fact]
    public void KittyRequestedFlags_IncludesReportAlternateKeys () =>
        Assert.True (EscSeqUtils.KittyKeyboardRequestedFlags.HasFlag (KittyKeyboardFlags.ReportAlternateKeys),
                     $"KittyKeyboardRequestedFlags ({EscSeqUtils.KittyKeyboardRequestedFlags}) must include ReportAlternateKeys "
                     + "for international keyboard layout support.");

    [Fact]
    public void KittyRequestedFlags_IncludesReportAssociatedText () =>
        Assert.True (EscSeqUtils.KittyKeyboardRequestedFlags.HasFlag (KittyKeyboardFlags.ReportAssociatedText),
                     $"KittyKeyboardRequestedFlags ({EscSeqUtils.KittyKeyboardRequestedFlags}) must include ReportAssociatedText "
                     + "for printable text fidelity.");

    #endregion

    #region Regression Tests - Modifiers Preservation

    // Copilot - ChatGPT v4
    /// <summary>
    /// Regression test for issue where Ctrl+Shift+Alt+A was being parsed as Ctrl+Alt+A.
    /// The bug was in NormalizeShiftedPrintableKey modifying the modifierField,
    /// causing double-decoding in ApplyModifiersAndEventType.
    /// Input: ESC[97:65;8u = 'a' with shifted key 'A' (65), modifiers 8 (Shift+Ctrl+Alt)
    /// Expected: Ctrl+Shift+Alt+A
    /// </summary>
    [Fact]
    public void KittyPattern_ShiftCtrlAlt_A_PreservesAllModifiers ()
    {
        // ESC[97:65;8u = 'a' with shifted key 'A' (65), modifiers 8 (0b111 = Shift+Ctrl+Alt)
        // mask = 8 - 1 = 7 which means 7 = 1 (Shift) + 2 (Alt) + 4 (Ctrl)
        Key? key = _pattern.GetKey ("\u001b[97:65;8u");

        Assert.NotNull (key);
        Assert.Equal (Key.A.WithShift.WithCtrl.WithAlt, key);
        Assert.True (key.IsShift, "Shift should be preserved");
        Assert.True (key.IsCtrl, "Ctrl should be preserved");
        Assert.True (key.IsAlt, "Alt should be preserved");
        Assert.Equal (new Key (KeyCode.A | KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask), key.WithShift.WithCtrl.WithAlt);
        Assert.Equal (KeyCode.A | KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask, key.KeyCode);
    }

    // Copilot - ChatGPT v4
    /// <summary>
    /// Regression test for Ctrl+A (without Shift).
    /// This should remain as Ctrl+A, not regress.
    /// Input: ESC[97;5u = 'a', modifiers 5 (0b100 = Ctrl, no Shift)
    /// </summary>
    [Fact]
    public void KittyPattern_Ctrl_A_WithoutShift ()
    {
        // ESC[97;5u = 'a', modifiers 5 (0b100 = Ctrl, no Shift)
        // mask = 5 - 1 = 4 which means 4 = 4 (Ctrl)
        Key? key = _pattern.GetKey ("\u001b[97;5u");

        Assert.NotNull (key);
        Assert.Equal (Key.A.WithCtrl, key);
        Assert.False (key.IsShift, "Shift should not be present");
        Assert.True (key.IsCtrl, "Ctrl should be present");
        Assert.False (key.IsAlt, "Alt should not be present");
        Assert.Equal (new Key ('a'), key.NoCtrl);
        Assert.Equal (KeyCode.A | KeyCode.CtrlMask, key.KeyCode);
    }

    // Copilot - ChatGPT v4
    /// <summary>
    /// Regression test for Ctrl+Alt+A (without Shift).
    /// This should remain as Ctrl+Alt+A, not regress.
    /// Input: ESC[97;7u = 'a', modifiers 7 (0b110 = Ctrl+Alt, no Shift)
    /// </summary>
    [Fact]
    public void KittyPattern_CtrlAlt_A_WithoutShift ()
    {
        // ESC[97;7u = 'a', modifiers 7 (0b110 = Ctrl+Alt, no Shift)
        // mask = 7 - 1 = 6 which means 6 = 2 (Alt) + 4 (Ctrl)
        Key? key = _pattern.GetKey ("\u001b[97;7u");

        Assert.NotNull (key);
        Assert.Equal (Key.A.WithCtrl.WithAlt, key);
        Assert.False (key.IsShift, "Shift should not be present");
        Assert.True (key.IsCtrl, "Ctrl should be present");
        Assert.True (key.IsAlt, "Alt should be present");
        Assert.Equal (new Key ('a'), key.NoCtrl.NoAlt);
        Assert.Equal (KeyCode.A | KeyCode.CtrlMask | KeyCode.AltMask, key.KeyCode);
    }

    // Copilot - ChatGPT v4
    /// <summary>
    /// Regression test for Shift+A (with shifted character 65 and only shift modifier).
    /// Input: ESC[97:65;2u = 'a' with shifted key 'A' (65), modifiers 2 (0b001 = Shift only)
    /// Expected: Shift+A
    /// </summary>
    [Fact]
    public void KittyPattern_Shift_A_WithShiftedCharacter ()
    {
        // ESC[97:65;2u = 'a' with shifted key 'A' (65), modifiers 2 (0b001 = Shift)
        // mask = 2 - 1 = 1 which means 1 = Shift only
        Key? key = _pattern.GetKey ("\u001b[97:65;2u");

        Assert.NotNull (key);
        Assert.Equal (Key.A.WithShift, key);
        Assert.True (key.IsShift, "Shift should be preserved");
        Assert.False (key.IsCtrl, "Ctrl should not be present");
        Assert.False (key.IsAlt, "Alt should not be present");
        Assert.Equal ("A", key.AsGrapheme);
        Assert.Equal (new Key ('A'), key);
        Assert.Equal (new Key ('a'), key.NoShift);
        Assert.Equal (KeyCode.A | KeyCode.ShiftMask, key.KeyCode);
    }

    // Copilot - ChatGPT v4
    /// <summary>
    /// Regression test for Shift+Ctrl+A (without Alt).
    /// Input: ESC[97:65;6u = 'a' with shifted key 'A' (65), modifiers 6 (0b0101 = Shift+Ctrl)
    /// Expected: Shift+Ctrl+A
    /// </summary>
    [Fact]
    public void KittyPattern_ShiftCtrl_A_PreservesAllModifiers ()
    {
        // ESC[97:65;6u = 'a' with shifted key 'A' (65), modifiers 6 (0b0101 = Shift+Ctrl)
        // mask = 6 - 1 = 5 which means 5 = 1 (Shift) + 4 (Ctrl)
        Key? key = _pattern.GetKey ("\u001b[97:65;6u");

        Assert.NotNull (key);
        Assert.Equal (Key.A.WithShift.WithCtrl, key);
        Assert.True (key.IsShift, "Shift should be preserved");
        Assert.True (key.IsCtrl, "Ctrl should be preserved");
        Assert.False (key.IsAlt, "Alt should not be present");
        Assert.Equal (new Key ('a'), key.NoShift.NoCtrl);
        Assert.Equal (KeyCode.A | KeyCode.ShiftMask | KeyCode.CtrlMask, key.KeyCode);
    }

    // Copilot - ChatGPT v4
    /// <summary>
    /// Regression test for Shift+Alt+A (without Ctrl).
    /// Input: ESC[97:65;4u = 'a' with shifted key 'A' (65), modifiers 4 (0b011 = Shift+Alt)
    /// Expected: Shift+Alt+A
    /// </summary>
    [Fact]
    public void KittyPattern_ShiftAlt_A_PreservesAllModifiers ()
    {
        // ESC[97:65;4u = 'a' with shifted key 'A' (65), modifiers 4 (0b011 = Shift+Alt)
        // mask = 4 - 1 = 3 which means 3 = 1 (Shift) + 2 (Alt)
        Key? key = _pattern.GetKey ("\u001b[97:65;4u");

        Assert.NotNull (key);
        Assert.Equal (Key.A.WithShift.WithAlt, key);
        Assert.True (key.IsShift, "Shift should be preserved");
        Assert.False (key.IsCtrl, "Ctrl should not be present");
        Assert.True (key.IsAlt, "Alt should be preserved");
        Assert.Equal (new Key ('a'), key.NoShift.NoAlt);
        Assert.Equal (KeyCode.A | KeyCode.ShiftMask | KeyCode.AltMask, key.KeyCode);
    }

    // Copilot - ChatGPT v4
    /// <summary>
    /// Regression test for Alt+A (without Shift and Ctrl).
    /// This should remain as Alt+A, not regress.
    /// Input: ESC[97;3;97u = 'a', modifiers 3 (0b010 = Alt, no Shift)
    /// </summary>
    [Fact]
    public void KittyPattern_Alt_A_WithoutShift ()
    {
        // ESC[97;3;97u = 'a', modifiers 3 (0b010 = Alt, no Shift)
        // mask = 3 - 1 = 2 which means 2 = 2 (Alt)
        Key? key = _pattern.GetKey ("\u001b[97;3;97u");

        Assert.NotNull (key);
        Assert.Equal (Key.A.WithAlt, key);
        Assert.False (key.IsShift, "Shift should not be present");
        Assert.False (key.IsCtrl, "Ctrl should not be present");
        Assert.True (key.IsAlt, "Alt should be present");
        Assert.Equal (new Key ('a'), key.NoAlt);
        Assert.Equal (KeyCode.A | KeyCode.AltMask, key.KeyCode);
    }

    #endregion

    #region Regression Tests - Printable AltGr keys

    /// <summary>
    /// Regression test for AltGr+@.
    /// This should remain as @, not regress.
    /// Input: ESC[50;3;64u = '@', modifiers 3 (0b010 = Alt)
    /// </summary>
    [Fact]
    public void KittyPattern_Alt_Arroba_WithAltGr ()
    {
        // ESC[50;3;64u = '@', modifiers 3 (0b010 = Alt)
        // mask = 3 - 1 = 2 which means 2 = 2 (Alt)
        Key? key = _pattern.GetKey ("\u001b[50;3;64u");

        Assert.NotNull (key);
        Assert.Equal (Key.D2.WithAlt, key);
        Assert.Equal ("@", key.AsGrapheme);
        Assert.False (key.IsShift, "Shift should not be present");
        Assert.False (key.IsCtrl, "Ctrl should not be present");
        Assert.True (key.IsAlt, "Alt should be present");
        Assert.Equal (Key.D2, key.NoAlt);
        Assert.Equal (KeyCode.D2 | KeyCode.AltMask, key.KeyCode);
    }

    /// <summary>
    /// Regression test for AltGr+€.
    /// This should remain as €, not regress.
    /// Input: ESC[101;3;8364u = '€', modifiers 3 (0b010 = Alt)
    /// </summary>
    [Fact]
    public void KittyPattern_Alt_Euro_WithAltGr ()
    {
        // ESC[101;3;8364u = '€', modifiers 3 (0b010 = Alt)
        // mask = 3 - 1 = 2 which means 2 = 2 (Alt)
        Key? key = _pattern.GetKey ("\u001b[101;3;8364u");

        Assert.NotNull (key);
        Assert.Equal (Key.E.WithAlt, key);
        Assert.Equal ("€", key.AsGrapheme);
        Assert.False (key.IsShift, "Shift should not be present");
        Assert.False (key.IsCtrl, "Ctrl should not be present");
        Assert.True (key.IsAlt, "Alt should be present");
        Assert.Equal (Key.E, key.NoAlt);
        Assert.Equal (KeyCode.E | KeyCode.AltMask, key.KeyCode);
    }

    #endregion
}
