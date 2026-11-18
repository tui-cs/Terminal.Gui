// Claude - Opus 4.6

namespace InputTests;

/// <summary>
///     Tests for Phase 2 rich keyboard event model: <see cref="KeyEventType"/>, <see cref="Key.EventType"/>,
///     <see cref="Key.ModifierKey"/>, and <see cref="Key.IsModifierOnly"/>.
/// </summary>
public class KeyEventTypeTests
{
    [Fact]
    public void Key_EventType_DefaultsToPress ()
    {
        Key key = new (KeyCode.A);

        Assert.Equal (KeyEventType.Press, key.EventType);
    }

    [Fact]
    public void Key_EventType_CanBeSetToRelease ()
    {
        Key key = new (KeyCode.A) { EventType = KeyEventType.Release };

        Assert.Equal (KeyEventType.Release, key.EventType);
    }

    [Fact]
    public void Key_EventType_CanBeSetToRepeat ()
    {
        Key key = new (KeyCode.A) { EventType = KeyEventType.Repeat };

        Assert.Equal (KeyEventType.Repeat, key.EventType);
    }

    [Fact]
    public void Key_EventType_PreservedByCopyConstructor ()
    {
        Key original = new (KeyCode.Enter) { EventType = KeyEventType.Release };
        Key copy = new (original);

        Assert.Equal (KeyEventType.Release, copy.EventType);
    }

    [Fact]
    public void Key_EventType_PreservedByWithShift ()
    {
        Key key = new (KeyCode.A) { EventType = KeyEventType.Repeat };
        Key shifted = key.WithShift;

        Assert.Equal (KeyEventType.Repeat, shifted.EventType);
    }

    [Fact]
    public void Key_EventType_PreservedByWithCtrl ()
    {
        Key key = new (KeyCode.A) { EventType = KeyEventType.Release };
        Key ctrl = key.WithCtrl;

        Assert.Equal (KeyEventType.Release, ctrl.EventType);
    }

    [Fact]
    public void Key_EventType_PreservedByWithAlt ()
    {
        Key key = new (KeyCode.A) { EventType = KeyEventType.Repeat };
        Key alt = key.WithAlt;

        Assert.Equal (KeyEventType.Repeat, alt.EventType);
    }

    [Fact]
    public void Key_EventType_PreservedByNoShift ()
    {
        Key key = new (KeyCode.A | KeyCode.ShiftMask) { EventType = KeyEventType.Release };
        Key noShift = key.NoShift;

        Assert.Equal (KeyEventType.Release, noShift.EventType);
    }

    [Fact]
    public void Key_EventType_DoesNotAffectEquality ()
    {
        Key press = new (KeyCode.A) { EventType = KeyEventType.Press };
        Key release = new (KeyCode.A) { EventType = KeyEventType.Release };

        // EventType is metadata-only and does not affect equality
        Assert.Equal (press, release);
    }

    [Fact]
    public void Key_ModifierKey_DefaultsToNone ()
    {
        Key key = new (KeyCode.A);

        Assert.Equal (ModifierKey.None, key.ModifierKey);
        Assert.False (key.IsModifierOnly);
    }

    [Fact]
    public void Key_ModifierKey_CanBeSet ()
    {
        Key key = new () { ModifierKey = ModifierKey.LeftShift };

        Assert.Equal (ModifierKey.LeftShift, key.ModifierKey);
        Assert.True (key.IsModifierOnly);
    }

    [Fact]
    public void Key_ModifierKey_PreservedByCopyConstructor ()
    {
        Key original = new () { ModifierKey = ModifierKey.RightCtrl };
        Key copy = new (original);

        Assert.Equal (ModifierKey.RightCtrl, copy.ModifierKey);
        Assert.True (copy.IsModifierOnly);
    }

    [Fact]
    public void Key_IsModifierOnly_FalseForNonModifierKeys ()
    {
        Key key = new (KeyCode.Enter);

        Assert.False (key.IsModifierOnly);
    }

    [Fact]
    public void Key_IsModifierOnly_TrueForModifierKeys ()
    {
        Key key = new () { ModifierKey = ModifierKey.Alt };

        Assert.True (key.IsModifierOnly);
    }

    [Fact]
    public void Key_ImplicitFromKeyCode_EventTypeDefaultsToPress ()
    {
        Key key = KeyCode.Enter;

        Assert.Equal (KeyEventType.Press, key.EventType);
    }

    [Fact]
    public void Key_ImplicitFromChar_EventTypeDefaultsToPress ()
    {
        Key key = 'a';

        Assert.Equal (KeyEventType.Press, key.EventType);
    }
}
