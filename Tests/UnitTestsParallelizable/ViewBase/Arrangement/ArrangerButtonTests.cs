// Claude - Opus 4.5

namespace ViewBaseTests.Arrangement;

/// <summary>
///     Tests for <see cref="ArrangerButton"/> orientation, direction, and key binding behavior.
/// </summary>
[Trait ("Category", "Adornment")]
[Trait ("Category", "Arrangement")]
public class ArrangerButtonTests
{
    #region Orientation and Direction

    [Fact]
    public void LeftSize_Sets_Horizontal_Backward ()
    {
        ArrangerButton button = new () { ButtonType = ArrangeButtons.LeftSize };

        Assert.Equal (Orientation.Horizontal, button.Orientation);
        Assert.Equal (NavigationDirection.Backward, button.Direction);

        button.Dispose ();
    }

    [Fact]
    public void RightSize_Sets_Horizontal_Forward ()
    {
        ArrangerButton button = new () { ButtonType = ArrangeButtons.RightSize };

        Assert.Equal (Orientation.Horizontal, button.Orientation);
        Assert.Equal (NavigationDirection.Forward, button.Direction);

        button.Dispose ();
    }

    [Fact]
    public void TopSize_Sets_Vertical_Backward ()
    {
        ArrangerButton button = new () { ButtonType = ArrangeButtons.TopSize };

        Assert.Equal (Orientation.Vertical, button.Orientation);
        Assert.Equal (NavigationDirection.Backward, button.Direction);

        button.Dispose ();
    }

    [Fact]
    public void BottomSize_Sets_Vertical_Forward ()
    {
        ArrangerButton button = new () { ButtonType = ArrangeButtons.BottomSize };

        Assert.Equal (Orientation.Vertical, button.Orientation);
        Assert.Equal (NavigationDirection.Forward, button.Direction);

        button.Dispose ();
    }

    [Fact]
    public void Move_Sets_Vertical_Forward ()
    {
        ArrangerButton button = new () { ButtonType = ArrangeButtons.Move };

        Assert.Equal (Orientation.Vertical, button.Orientation);
        Assert.Equal (NavigationDirection.Forward, button.Direction);

        button.Dispose ();
    }

    [Fact]
    public void AllSize_Sets_Vertical_Forward ()
    {
        ArrangerButton button = new () { ButtonType = ArrangeButtons.AllSize };

        Assert.Equal (Orientation.Vertical, button.Orientation);
        Assert.Equal (NavigationDirection.Forward, button.Direction);

        button.Dispose ();
    }

    #endregion

    #region KeyBindings — Arrow Keys Bound to Directional Commands

    [Fact]
    public void Move_BindsAllFourArrowKeys_ToDirectionalCommands ()
    {
        ArrangerButton button = new () { ButtonType = ArrangeButtons.Move };

        AssertKeyBoundToCommand (button, Key.CursorUp, Command.Up);
        AssertKeyBoundToCommand (button, Key.CursorDown, Command.Down);
        AssertKeyBoundToCommand (button, Key.CursorLeft, Command.Left);
        AssertKeyBoundToCommand (button, Key.CursorRight, Command.Right);

        button.Dispose ();
    }

    [Fact]
    public void AllSize_BindsAllFourArrowKeys_ToDirectionalCommands ()
    {
        ArrangerButton button = new () { ButtonType = ArrangeButtons.AllSize };

        AssertKeyBoundToCommand (button, Key.CursorUp, Command.Up);
        AssertKeyBoundToCommand (button, Key.CursorDown, Command.Down);
        AssertKeyBoundToCommand (button, Key.CursorLeft, Command.Left);
        AssertKeyBoundToCommand (button, Key.CursorRight, Command.Right);

        button.Dispose ();
    }

    [Fact]
    public void LeftSize_BindsLeftRight_ToDirectionalCommands ()
    {
        ArrangerButton button = new () { ButtonType = ArrangeButtons.LeftSize };

        AssertKeyBoundToCommand (button, Key.CursorLeft, Command.Left);
        AssertKeyBoundToCommand (button, Key.CursorRight, Command.Right);

        button.Dispose ();
    }

    [Fact]
    public void LeftSize_DoesNotBindUpDown ()
    {
        ArrangerButton button = new () { ButtonType = ArrangeButtons.LeftSize };

        Assert.False (button.KeyBindings.TryGet (Key.CursorUp, out _));
        Assert.False (button.KeyBindings.TryGet (Key.CursorDown, out _));

        button.Dispose ();
    }

    [Fact]
    public void RightSize_BindsLeftRight_ToDirectionalCommands ()
    {
        ArrangerButton button = new () { ButtonType = ArrangeButtons.RightSize };

        AssertKeyBoundToCommand (button, Key.CursorLeft, Command.Left);
        AssertKeyBoundToCommand (button, Key.CursorRight, Command.Right);

        button.Dispose ();
    }

    [Fact]
    public void RightSize_DoesNotBindUpDown ()
    {
        ArrangerButton button = new () { ButtonType = ArrangeButtons.RightSize };

        Assert.False (button.KeyBindings.TryGet (Key.CursorUp, out _));
        Assert.False (button.KeyBindings.TryGet (Key.CursorDown, out _));

        button.Dispose ();
    }

    [Fact]
    public void TopSize_BindsUpDown_ToDirectionalCommands ()
    {
        ArrangerButton button = new () { ButtonType = ArrangeButtons.TopSize };

        AssertKeyBoundToCommand (button, Key.CursorUp, Command.Up);
        AssertKeyBoundToCommand (button, Key.CursorDown, Command.Down);

        button.Dispose ();
    }

    [Fact]
    public void TopSize_DoesNotBindLeftRight ()
    {
        ArrangerButton button = new () { ButtonType = ArrangeButtons.TopSize };

        Assert.False (button.KeyBindings.TryGet (Key.CursorLeft, out _));
        Assert.False (button.KeyBindings.TryGet (Key.CursorRight, out _));

        button.Dispose ();
    }

    [Fact]
    public void BottomSize_BindsUpDown_ToDirectionalCommands ()
    {
        ArrangerButton button = new () { ButtonType = ArrangeButtons.BottomSize };

        AssertKeyBoundToCommand (button, Key.CursorUp, Command.Up);
        AssertKeyBoundToCommand (button, Key.CursorDown, Command.Down);

        button.Dispose ();
    }

    [Fact]
    public void BottomSize_DoesNotBindLeftRight ()
    {
        ArrangerButton button = new () { ButtonType = ArrangeButtons.BottomSize };

        Assert.False (button.KeyBindings.TryGet (Key.CursorLeft, out _));
        Assert.False (button.KeyBindings.TryGet (Key.CursorRight, out _));

        button.Dispose ();
    }

    #endregion

    #region KeyBindings — Space Retained, Enter Removed

    [Fact]
    public void ButtonType_RemoveSpace_RemovesEnter ()
    {
        ArrangerButton button = new () { ButtonType = ArrangeButtons.Move };

        // Space should remain bound (inherited from Button)
        IEnumerable<KeyValuePair<Key, KeyBinding>> bindings = button.KeyBindings.GetBindings ();
        bool hasSpace = bindings.Any (kvp => kvp.Key.KeyCode == KeyCode.Space);
        bool hasEnter = bindings.Any (kvp => kvp.Key.KeyCode == KeyCode.Enter);

        Assert.False (hasSpace, "ArrangerButton should not have Space bound");
        Assert.False (hasEnter, "ArrangerButton should not have Enter bound");

        button.Dispose ();
    }

    #endregion

    #region ButtonType Change Rebinds Keys

    [Fact]
    public void ChangingButtonType_RebindsKeys ()
    {
        ArrangerButton button = new () { ButtonType = ArrangeButtons.LeftSize };

        // Initially: only Left/Right bound
        AssertKeyBoundToCommand (button, Key.CursorLeft, Command.Left);
        AssertKeyBoundToCommand (button, Key.CursorRight, Command.Right);
        Assert.False (button.KeyBindings.TryGet (Key.CursorUp, out _));
        Assert.False (button.KeyBindings.TryGet (Key.CursorDown, out _));

        // Change to TopSize: only Up/Down bound
        button.ButtonType = ArrangeButtons.TopSize;

        AssertKeyBoundToCommand (button, Key.CursorUp, Command.Up);
        AssertKeyBoundToCommand (button, Key.CursorDown, Command.Down);
        Assert.False (button.KeyBindings.TryGet (Key.CursorLeft, out _));
        Assert.False (button.KeyBindings.TryGet (Key.CursorRight, out _));

        // Orientation and Direction should also have changed
        Assert.Equal (Orientation.Vertical, button.Orientation);
        Assert.Equal (NavigationDirection.Backward, button.Direction);

        button.Dispose ();
    }

    [Fact]
    public void ChangingButtonType_ToMove_BindsAllArrowKeys ()
    {
        ArrangerButton button = new () { ButtonType = ArrangeButtons.TopSize };

        // Initially: only Up/Down
        Assert.False (button.KeyBindings.TryGet (Key.CursorLeft, out _));
        Assert.False (button.KeyBindings.TryGet (Key.CursorRight, out _));

        // Change to Move: all four
        button.ButtonType = ArrangeButtons.Move;

        AssertKeyBoundToCommand (button, Key.CursorUp, Command.Up);
        AssertKeyBoundToCommand (button, Key.CursorDown, Command.Down);
        AssertKeyBoundToCommand (button, Key.CursorLeft, Command.Left);
        AssertKeyBoundToCommand (button, Key.CursorRight, Command.Right);

        button.Dispose ();
    }

    [Fact]
    public void SettingSameButtonType_DoesNotRebind ()
    {
        ArrangerButton button = new () { ButtonType = ArrangeButtons.LeftSize };

        // Setting same value is idempotent — no exception
        button.ButtonType = ArrangeButtons.LeftSize;

        AssertKeyBoundToCommand (button, Key.CursorLeft, Command.Left);
        AssertKeyBoundToCommand (button, Key.CursorRight, Command.Right);
        Assert.False (button.KeyBindings.TryGet (Key.CursorUp, out _));

        button.Dispose ();
    }

    #endregion

    #region Glyph / Text

    [Fact]
    public void Text_ReturnsCorrectGlyph_ForEachButtonType ()
    {
        ArrangerButton button = new ();

        button.ButtonType = ArrangeButtons.Move;
        Assert.Equal ($"{Glyphs.Move}", button.Text);

        button.ButtonType = ArrangeButtons.AllSize;
        Assert.Equal ($"{Glyphs.SizeBottomRight}", button.Text);

        button.ButtonType = ArrangeButtons.LeftSize;
        Assert.Equal ($"{Glyphs.SizeHorizontal}", button.Text);

        button.ButtonType = ArrangeButtons.RightSize;
        Assert.Equal ($"{Glyphs.SizeHorizontal}", button.Text);

        button.ButtonType = ArrangeButtons.TopSize;
        Assert.Equal ($"{Glyphs.SizeVertical}", button.Text);

        button.ButtonType = ArrangeButtons.BottomSize;
        Assert.Equal ($"{Glyphs.SizeVertical}", button.Text);

        button.Dispose ();
    }

    #endregion

    #region Constructor Defaults

    [Fact]
    public void Constructor_SetsExpectedDefaults ()
    {
        ArrangerButton button = new ();

        Assert.True (button.CanFocus);
        Assert.Equal (1, button.Width!.GetAnchor (0));
        Assert.Equal (1, button.Height!.GetAnchor (0));
        Assert.True (button.NoDecorations);
        Assert.True (button.NoPadding);
        Assert.Null (button.ShadowStyle);
        Assert.False (button.Visible);

        button.Dispose ();
    }

    #endregion

    #region Helpers

    private static void AssertKeyBoundToCommand (ArrangerButton button, Key key, Command expectedCommand)
    {
        Assert.True (button.KeyBindings.TryGet (key, out KeyBinding binding), $"Expected key {key} to be bound on {button.ButtonType}");
        Assert.Contains (expectedCommand, binding.Commands);
    }

    #endregion
}
