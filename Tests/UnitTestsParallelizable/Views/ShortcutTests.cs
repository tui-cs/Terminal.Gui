using System.Text;
using JetBrains.Annotations;

namespace ViewsTests;

[TestSubject (typeof (Shortcut))]
public partial class ShortcutTests (ITestOutputHelper output)
{
    // CommandView Test view for Shortcut tests
    private sealed class TestCommandView : View, IValue<int?>
    {
        public TestCommandView ()
        {
            Width = 5;
            Height = 1;
        }

        public int? Value
        {
            get;
            set
            {
                if (field == value)
                {
                    return;
                }
                int? oldValue = field;
                field = value;
                OnValueChanging (oldValue, value);
                OnValueChanged (oldValue, value);
            }
        }

        public event EventHandler<ValueChangingEventArgs<int?>>? ValueChanging;
        public event EventHandler<ValueChangedEventArgs<int?>>? ValueChanged;
        public event EventHandler<ValueChangedEventArgs<object?>>? ValueChangedUntyped;

        private void OnValueChanging (int? oldValue, int? newValue) => ValueChanging?.Invoke (this, new ValueChangingEventArgs<int?> (oldValue, newValue));

        private void OnValueChanged (int? oldValue, int? newValue)
        {
            ValueChanged?.Invoke (this, new ValueChangedEventArgs<int?> (oldValue, newValue));
            ValueChangedUntyped?.Invoke (this, new ValueChangedEventArgs<object?> (oldValue, newValue));
        }
    }

    [Fact]
    public void Constructor_Defaults ()
    {
        // Test parameterless constructor
        Shortcut shortcut = new ();

        Assert.NotNull (shortcut);

        // CanFocus defaults
        Assert.True (shortcut.CanFocus);
        Assert.False (shortcut.CommandView?.CanFocus);

        // Dimension defaults
        Assert.IsType<DimAuto> (shortcut.Width);
        Assert.IsType<DimAuto> (shortcut.Height);

        // Orientation and alignment defaults
        Assert.Equal (Orientation.Horizontal, shortcut.Orientation);
        Assert.Equal (AlignmentModes.StartToEnd | AlignmentModes.IgnoreFirstOrLast, shortcut.AlignmentModes);

        // Key defaults
        Assert.Equal (Key.Empty, shortcut.Key);
        Assert.False (shortcut.BindKeyToApplication);
        Assert.Equal (0, shortcut.MinimumKeyTextSize);

        // Text/Title defaults
        Assert.Equal (string.Empty, shortcut.Title);
        Assert.Equal (string.Empty, shortcut.Text);
        Assert.Equal (string.Empty, shortcut.HelpText);

        // CommandView defaults
        Assert.NotNull (shortcut.CommandView);
#if DEBUG
        Assert.Equal ("CommandView", shortcut.CommandView?.Id);
#endif

        // HelpView defaults
        Assert.NotNull (shortcut.HelpView);
#if DEBUG
        Assert.Equal ("_helpView", shortcut.HelpView.Id);
#endif
        Assert.Equal (string.Empty, shortcut.HelpView.Text);
        Assert.True (shortcut.HelpView.Visible);

        // KeyView defaults
        Assert.NotNull (shortcut.KeyView);
#if DEBUG
        Assert.Equal ("_keyView", shortcut.KeyView.Id);
#endif
        Assert.Equal (string.Empty, shortcut.KeyView.Text);
        Assert.True (shortcut.KeyView.Visible);

        // Action defaults
        Assert.Null (shortcut.Action);

        // Mouse highlight defaults
        Assert.Equal (MouseState.In, shortcut.MouseHighlightStates);

        // SubViews - CommandView added, HelpView and KeyView not added (empty)
        Assert.Contains (shortcut.CommandView, shortcut.SubViews);
        Assert.DoesNotContain (shortcut.HelpView, shortcut.SubViews);
        Assert.DoesNotContain (shortcut.KeyView, shortcut.SubViews);

        // Test parameterized constructor
        Shortcut shortcut1 = new (Key.A, "_CommandText", () => { }, "Help text");
        Assert.Equal (Key.A, shortcut1.Key);
        Assert.Equal ("_CommandText", shortcut1.Title);
        Assert.Equal ("Help text", shortcut1.HelpText);
        Assert.Equal ("Help text", shortcut1.Text);
        Assert.NotNull (shortcut1.Action);

        // Other properties should have defaults
        Assert.True (shortcut1.CanFocus);
        Assert.IsType<DimAuto> (shortcut1.Width);
        Assert.IsType<DimAuto> (shortcut1.Height);
        Assert.False (shortcut1.BindKeyToApplication);
        Assert.Equal (Orientation.Horizontal, shortcut1.Orientation);
        Assert.Equal (AlignmentModes.StartToEnd | AlignmentModes.IgnoreFirstOrLast, shortcut1.AlignmentModes);
        Assert.Equal ("_CommandText", shortcut1.CommandView?.Text); // Title syncs to CommandView?.Text
        Assert.Equal (MouseState.In, shortcut1.MouseHighlightStates);

        // SubViews - All three should be present with values set
        Assert.Contains (shortcut1.CommandView, shortcut1.SubViews);
        Assert.Contains (shortcut1.HelpView, shortcut1.SubViews);
        Assert.Contains (shortcut1.KeyView, shortcut1.SubViews);

        // KeyView should display the key text
        Assert.Equal ("a", shortcut1.KeyView.Text);
    }

    [Fact]
    public void Size_Defaults ()
    {
        Shortcut shortcut = new ();
        shortcut.Layout ();

        Assert.Equal (2, shortcut.Frame.Width);
        Assert.Equal (1, shortcut.Frame.Height);
        Assert.Equal (2, shortcut.Viewport.Width);
        Assert.Equal (1, shortcut.Viewport.Height);

        Assert.Equal (0, shortcut.CommandView?.Viewport.Width);
        Assert.Equal (1, shortcut.CommandView?.Viewport.Height);

        Assert.Equal (2, shortcut.HelpView.Viewport.Width);
        Assert.Equal (1, shortcut.HelpView.Viewport.Height);

        Assert.Equal (2, shortcut.KeyView.Viewport.Width);
        Assert.Equal (1, shortcut.KeyView.Viewport.Height);

        //  0123456789
        // "   0  A "
        shortcut = new Shortcut { Key = Key.A, HelpText = "0" };
        shortcut.Layout ();
        Assert.Equal (8, shortcut.Frame.Width);
        Assert.Equal (1, shortcut.Frame.Height);
        Assert.Equal (8, shortcut.Viewport.Width);
        Assert.Equal (1, shortcut.Viewport.Height);

        Assert.Equal (0, shortcut.CommandView?.Viewport.Width);
        Assert.Equal (1, shortcut.CommandView?.Viewport.Height);

        Assert.Equal (1, shortcut.HelpView.Viewport.Width);
        Assert.Equal (1, shortcut.HelpView.Viewport.Height);

        Assert.Equal (1, shortcut.KeyView.Viewport.Width);
        Assert.Equal (1, shortcut.KeyView.Viewport.Height);

        //  0123456789
        // " C  0  A "
        shortcut = new Shortcut { Title = "C", Key = Key.A, HelpText = "0" };
        shortcut.Layout ();
        Assert.Equal (9, shortcut.Frame.Width);
        Assert.Equal (1, shortcut.Frame.Height);
        Assert.Equal (9, shortcut.Viewport.Width);
        Assert.Equal (1, shortcut.Viewport.Height);

        Assert.Equal (1, shortcut.CommandView?.Viewport.Width);
        Assert.Equal (1, shortcut.CommandView?.Viewport.Height);

        Assert.Equal (1, shortcut.HelpView.Viewport.Width);
        Assert.Equal (1, shortcut.HelpView.Viewport.Height);

        Assert.Equal (1, shortcut.KeyView.Viewport.Width);
        Assert.Equal (1, shortcut.KeyView.Viewport.Height);
    }

    [Theory]
    [InlineData ("", "", KeyCode.Null, 2)]
    [InlineData ("C", "", KeyCode.Null, 3)]
    [InlineData ("", "H", KeyCode.Null, 5)]
    [InlineData ("", "", KeyCode.K, 5)]
    [InlineData ("C", "", KeyCode.K, 6)]
    [InlineData ("C", "H", KeyCode.Null, 6)]
    [InlineData ("", "H", KeyCode.K, 8)]
    [InlineData ("C", "H", KeyCode.K, 9)]
    public void NaturalSize (string command, string help, KeyCode key, int expectedWidth)
    {
        var shortcut = new Shortcut { HelpText = help, Key = key, Title = command };

        shortcut.Layout ();

        // |0123456789
        // | C  H  K |
        Assert.Equal (expectedWidth, shortcut.Frame.Width);

        shortcut = new Shortcut { HelpText = help, Title = command, Key = key };

        shortcut.Layout ();
        Assert.Equal (expectedWidth, shortcut.Frame.Width);

        shortcut = new Shortcut { HelpText = help, Key = key, Title = command };

        shortcut.Layout ();
        Assert.Equal (expectedWidth, shortcut.Frame.Width);

        shortcut = new Shortcut { Key = key, HelpText = help, Title = command };

        shortcut.Layout ();
        Assert.Equal (expectedWidth, shortcut.Frame.Width);
    }

    [Theory]
    [InlineData (0, 0, 3, 3)]
    [InlineData (1, 0, 3, 3)]
    [InlineData (2, 0, 3, 3)]
    [InlineData (3, 0, 3, 3)]
    [InlineData (4, 0, 3, 3)]
    [InlineData (5, 0, 3, 3)]
    [InlineData (6, 0, 3, 3)]
    [InlineData (7, 0, 3, 4)]
    [InlineData (8, 0, 3, 5)]
    [InlineData (9, 0, 3, 6)]
    [InlineData (10, 0, 4, 7)]
    [InlineData (11, 0, 5, 8)]
    public void Set_Width_Layouts_Correctly (int width, int expectedCmdX, int expectedHelpX, int expectedKeyX)
    {
        var shortcut = new Shortcut { Width = width, Title = "C", Text = "H", Key = Key.K };
        shortcut.Layout ();

        // 01234
        // -C--K

        // 012345
        // -C--K-

        // 0123456
        // -C-H-K-

        // 01234567
        // -C--H-K-

        // 012345678
        // -C--H--K-

        // 0123456789
        // -C--H--K-
        Assert.Equal (expectedCmdX, shortcut.CommandView?.Frame.X);
        Assert.Equal (expectedHelpX, shortcut.HelpView.Frame.X);
        Assert.Equal (expectedKeyX, shortcut.KeyView.Frame.X);
    }

    [Fact]
    public void CommandView_Text_And_Title_Track ()
    {
        var shortcut = new Shortcut { Title = "T" };

        Assert.Equal (shortcut.Title, shortcut.CommandView?.Text);

        shortcut = new Shortcut ();

        shortcut.CommandView = new View { Text = "T" };
        Assert.Equal (shortcut.Title, shortcut.CommandView?.Text);
    }

    [Fact]
    public void HotKey_Title_Initializer_SetsCorrectly ()
    {
        Shortcut shortcut = new () { Title = "_C" };
        Assert.Equal (Key.Empty, shortcut.CommandView?.HotKey);
        Assert.Equal (Key.C, shortcut.HotKey);
    }

    [Fact]
    public void HotKey_CommandView_Set_Sets_Correctly ()
    {
        Shortcut shortcut = new () { Title = "_C" };

        shortcut.CommandView = new View { HotKeySpecifier = (Rune)'_', Text = "_D" };
        Assert.Equal (Key.Empty, shortcut.CommandView?.HotKey);
        Assert.Equal (Key.D, shortcut.HotKey);

        shortcut = new Shortcut { Title = "_C", CommandView = new View { HotKeySpecifier = (Rune)'_', Text = "_D" } };
        Assert.Equal (Key.Empty, shortcut.CommandView?.HotKey);
        Assert.Equal (Key.D, shortcut.HotKey);

        shortcut = new Shortcut { CommandView = new View { HotKeySpecifier = (Rune)'_', Text = "_D" }, Title = "_C" };
        Assert.Equal (Key.Empty, shortcut.CommandView?.HotKey);
        Assert.Equal (Key.C, shortcut.HotKey);
    }

    [Fact]
    public void HelpText_And_Text_Are_The_Same ()
    {
        var shortcut = new Shortcut { Text = "H" };

        Assert.Equal (shortcut.Text, shortcut.HelpText);

        shortcut = new Shortcut { HelpText = "H" };

        Assert.Equal (shortcut.Text, shortcut.HelpText);
    }

    [Theory]
    [InlineData (KeyCode.Null, "")]
    [InlineData (KeyCode.F1, "F1")]
    public void KeyView_Text_Tracks_Key (KeyCode key, string expected)
    {
        var shortcut = new Shortcut { Key = key };

        Assert.Equal (expected, shortcut.KeyView.Text);
    }

    // Test Key
    [Fact]
    public void Key_Defaults_To_Empty ()
    {
        Shortcut shortcut = new ();

        Assert.Equal (Key.Empty, shortcut.Key);
    }

    [Fact]
    public void Key_Can_Be_Set ()
    {
        Shortcut shortcut = new ();

        shortcut.Key = Key.F1;

        Assert.Equal (Key.F1, shortcut.Key);
    }

    [Fact]
    public void Key_Can_Be_Set_To_Empty ()
    {
        Shortcut shortcut = new ();

        shortcut.Key = Key.Empty;

        Assert.Equal (Key.Empty, shortcut.Key);
    }

    [Fact]
    public void Key_Set_Binds_Key_To_CommandView_Accept ()
    {
        Shortcut shortcut = new ();

        shortcut.Key = Key.F1;

        // TODO:
    }

    [Fact]
    public void Key_Changing_Removes_Previous_Binding ()
    {
        Shortcut shortcut = new ();

        shortcut.Key = Key.A;
        Assert.True (shortcut.HotKeyBindings.TryGet (Key.A, out _));

        shortcut.Key = Key.B;
        Assert.False (shortcut.HotKeyBindings.TryGet (Key.A, out _));
        Assert.True (shortcut.HotKeyBindings.TryGet (Key.B, out _));
    }

    // Test Key gets bound correctly
    [Fact]
    public void BindKeyToApplication_Defaults_To_HotKey ()
    {
        Shortcut shortcut = new ();

        Assert.False (shortcut.BindKeyToApplication);
    }

    [Fact]
    public void BindKeyToApplication_Can_Be_Set ()
    {
        IApplication app = Application.Create ();
        Shortcut shortcut = new () { App = app };

        shortcut.BindKeyToApplication = true;

        Assert.True (shortcut.BindKeyToApplication);
    }

    [Fact]
    public void BindKeyToApplication_Changing_Adjusts_KeyBindings ()
    {
        Shortcut shortcut = new ();

        shortcut.Key = Key.A;
        Assert.True (shortcut.HotKeyBindings.TryGet (Key.A, out _));

        shortcut.App = Application.Create ();
        shortcut.BindKeyToApplication = true;
        shortcut.BeginInit ();
        shortcut.EndInit ();
        Assert.False (shortcut.HotKeyBindings.TryGet (Key.A, out _));
        Assert.True (shortcut.App?.Keyboard.KeyBindings.TryGet (Key.A, out _));

        shortcut.BindKeyToApplication = false;
        Assert.True (shortcut.HotKeyBindings.TryGet (Key.A, out _));
        Assert.False (shortcut.App?.Keyboard.KeyBindings.TryGet (Key.A, out _));
    }

    [Theory]
    [InlineData (Orientation.Horizontal)]
    [InlineData (Orientation.Vertical)]
    public void Orientation_SetsCorrectly (Orientation orientation)
    {
        var shortcut = new Shortcut { Orientation = orientation };

        Assert.Equal (orientation, shortcut.Orientation);
    }

    [Theory]
    [InlineData (AlignmentModes.StartToEnd)]
    [InlineData (AlignmentModes.EndToStart)]
    public void AlignmentModes_SetsCorrectly (AlignmentModes alignmentModes)
    {
        var shortcut = new Shortcut { AlignmentModes = alignmentModes };

        Assert.Equal (alignmentModes, shortcut.AlignmentModes);
    }

    [Fact]
    public void Action_SetsAndGetsCorrectly ()
    {
        var actionInvoked = false;

        var shortcut = new Shortcut { Action = () => { actionInvoked = true; } };

        shortcut.Action.Invoke ();

        Assert.True (actionInvoked);
    }

    [Fact]
    public void SubView_Visibility_Controlled_By_Removal ()
    {
        Shortcut shortcut = new ();

        Assert.True (shortcut.CommandView?.Visible);
        Assert.Contains (shortcut.CommandView, shortcut.SubViews);
        Assert.True (shortcut.HelpView.Visible);
        Assert.DoesNotContain (shortcut.HelpView, shortcut.SubViews);
        Assert.True (shortcut.KeyView.Visible);
        Assert.DoesNotContain (shortcut.KeyView, shortcut.SubViews);

        shortcut.HelpText = "help";
        Assert.True (shortcut.HelpView.Visible);
        Assert.Contains (shortcut.HelpView, shortcut.SubViews);
        Assert.True (shortcut.KeyView.Visible);
        Assert.DoesNotContain (shortcut.KeyView, shortcut.SubViews);

        shortcut.Key = Key.A;
        Assert.True (shortcut.HelpView.Visible);
        Assert.Contains (shortcut.HelpView, shortcut.SubViews);
        Assert.True (shortcut.KeyView.Visible);
        Assert.Contains (shortcut.KeyView, shortcut.SubViews);

        shortcut.HelpView.Visible = false;
        shortcut.ShowHide ();
        Assert.False (shortcut.HelpView.Visible);
        Assert.DoesNotContain (shortcut.HelpView, shortcut.SubViews);
        Assert.True (shortcut.KeyView.Visible);
        Assert.Contains (shortcut.KeyView, shortcut.SubViews);

        shortcut.KeyView.Visible = false;
        shortcut.ShowHide ();
        Assert.False (shortcut.HelpView.Visible);
        Assert.DoesNotContain (shortcut.HelpView, shortcut.SubViews);
        Assert.False (shortcut.KeyView.Visible);
        Assert.DoesNotContain (shortcut.KeyView, shortcut.SubViews);
    }

    [Fact]
    public void Focus_CanFocus_Default_Is_True ()
    {
        Shortcut shortcut = new ();
        shortcut.Key = Key.A;
        shortcut.Text = "Help";
        shortcut.Title = "Command";
        Assert.True (shortcut.CanFocus);
        Assert.False (shortcut.CommandView?.CanFocus);
    }

    [Fact]
    public void Focus_CanFocus_CommandView_Add_Tracks ()
    {
        Shortcut shortcut = new ();
        Assert.True (shortcut.CanFocus);
        Assert.False (shortcut.CommandView?.CanFocus);

        shortcut.CommandView = new View { CanFocus = true };
        Assert.True (shortcut.CommandView?.CanFocus);

        shortcut.CommandView?.CanFocus = true;
        Assert.True (shortcut.CommandView?.CanFocus);

        shortcut.CanFocus = false;
        Assert.False (shortcut.CanFocus);
        Assert.True (shortcut.CommandView?.CanFocus);

        shortcut.CommandView?.CanFocus = false;
        Assert.False (shortcut.CanFocus);
        Assert.False (shortcut.CommandView?.CanFocus);

        shortcut.CommandView?.CanFocus = true;
        Assert.False (shortcut.CanFocus);
        Assert.True (shortcut.CommandView?.CanFocus);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Verifies that MouseHighlightStates defaults to MouseState.In for Shortcut.
    ///     This ensures Shortcuts highlight when the mouse hovers over them.
    /// </summary>
    [Fact]
    public void MouseHighlightStates_Defaults_To_In ()
    {
        // Arrange & Act
        Shortcut shortcut = new ();

        // Assert
        Assert.Equal (MouseState.In, shortcut.MouseHighlightStates);
    }
}
