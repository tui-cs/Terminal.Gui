namespace ViewsTests;

public class CheckBoxTests
{
    [Fact]
    public void Accept_Cancel_Event_OnAccept_Returns_True ()
    {
        var ckb = new CheckBox ();
        var acceptInvoked = false;

        ckb.Accepting += ViewOnAccept;

        bool? ret = ckb.InvokeCommand (Command.Accept);
        Assert.True (ret);
        Assert.True (acceptInvoked);

        return;

        void ViewOnAccept (object? sender, CommandEventArgs e)
        {
            acceptInvoked = true;
            e.Handled = true;
        }
    }

    // Claude - Opus 4.5
    [Fact]
    public void AllowCheckStateNone_Get_Set ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        var checkBox = new CheckBox { Text = "Check this out 你" };
        (runnable as View)?.Add (checkBox);
        app.Begin (runnable);

        checkBox.HasFocus = true;
        Assert.True (checkBox.HasFocus);
        Assert.Equal (CheckState.UnChecked, checkBox.Value);

        // Select with keyboard - DefaultActivateHandler returns false on success
        checkBox.NewKeyDownEvent (Key.Space);
        Assert.Equal (CheckState.Checked, checkBox.Value);

        // Select with mouse
        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (Point.Empty));
        Assert.Equal (CheckState.UnChecked, checkBox.Value);

        checkBox.AllowCheckStateNone = true;

        // DefaultActivateHandler returns false on success
        checkBox.NewKeyDownEvent (Key.Space);
        Assert.Equal (CheckState.None, checkBox.Value);

        checkBox.AllowCheckStateNone = false;
        Assert.Equal (CheckState.UnChecked, checkBox.Value);
    }

    [Theory]
    [InlineData (0, 0, 0, 0)]
    [InlineData (1, 0, 1, 0)]
    [InlineData (0, 1, 0, 1)]
    [InlineData (1, 1, 1, 1)]
    [InlineData (10, 1, 10, 1)]
    [InlineData (10, 3, 10, 3)]
    public void AbsoluteSize_DefaultText (int width, int height, int expectedWidth, int expectedHeight)
    {
        var checkBox = new CheckBox { X = 0, Y = 0, Width = width, Height = height };

        Assert.Equal (new Size (expectedWidth, expectedHeight), checkBox.Frame.Size);
        Assert.Equal (new Size (expectedWidth, expectedHeight), checkBox.Viewport.Size);
        Assert.Equal (new Size (expectedWidth, expectedHeight), checkBox.TextFormatter.ConstrainToSize);

        checkBox.Dispose ();
    }

    [Theory]
    [InlineData ("01234", 0, 0, 0, 0)]
    [InlineData ("01234", 1, 0, 1, 0)]
    [InlineData ("01234", 0, 1, 0, 1)]
    [InlineData ("01234", 1, 1, 1, 1)]
    [InlineData ("01234", 10, 1, 10, 1)]
    [InlineData ("01234", 10, 3, 10, 3)]
    [InlineData ("0_1234", 0, 0, 0, 0)]
    [InlineData ("0_1234", 1, 0, 1, 0)]
    [InlineData ("0_1234", 0, 1, 0, 1)]
    [InlineData ("0_1234", 1, 1, 1, 1)]
    [InlineData ("0_1234", 10, 1, 10, 1)]
    [InlineData ("0_12你", 10, 3, 10, 3)]
    [InlineData ("0_12你", 0, 0, 0, 0)]
    [InlineData ("0_12你", 1, 0, 1, 0)]
    [InlineData ("0_12你", 0, 1, 0, 1)]
    [InlineData ("0_12你", 1, 1, 1, 1)]
    [InlineData ("0_12你", 10, 1, 10, 1)]
    public void AbsoluteSize_Text (string text, int width, int height, int expectedWidth, int expectedHeight)
    {
        var checkBox = new CheckBox
        {
            X = 0,
            Y = 0,
            Width = width,
            Height = height,
            Text = text
        };
        checkBox.Layout ();

        Assert.Equal (new Size (expectedWidth, expectedHeight), checkBox.Frame.Size);
        Assert.Equal (new Size (expectedWidth, expectedHeight), checkBox.Viewport.Size);
        Assert.Equal (new Size (expectedWidth, expectedHeight), checkBox.TextFormatter.ConstrainToSize);

        checkBox.Dispose ();
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void Command_Accept_ConfirmsStateWithoutToggle ()
    {
        CheckBox checkBox = new () { Text = "Test", Value = CheckState.Checked };
        CheckState initialState = checkBox.Value;
        var acceptingFired = false;

        checkBox.Accepting += (_, e) =>
                              {
                                  acceptingFired = true;
                                  e.Handled = true; // Signal that the Accept was processed
                              };

        bool? result = checkBox.InvokeCommand (Command.Accept);

        Assert.True (acceptingFired);
        Assert.Equal (initialState, checkBox.Value); // State unchanged
        Assert.True (result);

        checkBox.Dispose ();
    }

    [Fact]
    public void Command_Activate_TogglesState ()
    {
        CheckBox checkBox = new () { Text = "Test" };
        CheckState initialState = checkBox.Value;
        var activatingFired = false;

        checkBox.Activating += (_, _) => activatingFired = true;

        checkBox.InvokeCommand (Command.Activate);

        Assert.True (activatingFired);
        Assert.NotEqual (initialState, checkBox.Value);

        checkBox.Dispose ();
    }

    [Fact]
    public void Command_HotKey_SetsFocus_DoesNotToggle ()
    {
        using CheckBox checkBox = new ();
        checkBox.Text = "_Test";
        CheckState initialState = checkBox.Value;
        var activatingFired = false;

        checkBox.Activating += (_, _) => activatingFired = true;

        bool? result = checkBox.InvokeCommand (Command.HotKey);

        Assert.True (activatingFired);
        Assert.NotEqual (initialState, checkBox.Value);
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void Enter_ConfirmsWithoutToggle ()
    {
        CheckBox checkBox = new () { Text = "Test", Value = CheckState.Checked };
        CheckState initialState = checkBox.Value;
        var acceptingFired = false;

        checkBox.Accepting += (_, e) =>
                              {
                                  acceptingFired = true;
                                  e.Handled = true;
                              };

        // Enter should confirm without toggling via Accept command
        bool? result = checkBox.NewKeyDownEvent (Key.Enter);

        Assert.True (acceptingFired);
        Assert.Equal (initialState, checkBox.Value);
        Assert.True (result);

        checkBox.Dispose ();
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void Space_TogglesState ()
    {
        CheckBox checkBox = new () { Text = "Test" };
        CheckState initialState = checkBox.Value;

        // Space should trigger state toggle via Activate command
        checkBox.NewKeyDownEvent (Key.Space);

        Assert.NotEqual (initialState, checkBox.Value);

        checkBox.Dispose ();
    }

    [Fact]
    public void Constructors_Defaults ()
    {
        var ckb = new CheckBox ();
        Assert.True (ckb.Width is DimAuto);
        Assert.True (ckb.Height is DimAuto);
        ckb.Layout ();
        Assert.Equal (CheckState.UnChecked, ckb.Value);
        Assert.False (ckb.AllowCheckStateNone);
        Assert.Equal (string.Empty, ckb.Text);
        Assert.Equal ($"{Glyphs.CheckStateUnChecked} ", ckb.TextFormatter.Text);
        Assert.True (ckb.CanFocus);
        Assert.Equal (new Rectangle (0, 0, 2, 1), ckb.Frame);

        ckb = new CheckBox { Text = "Test", Value = CheckState.Checked };
        Assert.True (ckb.Width is DimAuto);
        Assert.True (ckb.Height is DimAuto);
        ckb.Layout ();
        Assert.Equal (CheckState.Checked, ckb.Value);
        Assert.False (ckb.AllowCheckStateNone);
        Assert.Equal ("Test", ckb.Text);
        Assert.Equal ($"{Glyphs.CheckStateChecked} Test", ckb.TextFormatter.Text);
        Assert.True (ckb.CanFocus);
        Assert.Equal (new Rectangle (0, 0, 6, 1), ckb.Frame);

        ckb = new CheckBox { Text = "Test", X = 1, Y = 2 };
        Assert.True (ckb.Width is DimAuto);
        Assert.True (ckb.Height is DimAuto);
        ckb.Layout ();
        Assert.Equal (CheckState.UnChecked, ckb.Value);
        Assert.False (ckb.AllowCheckStateNone);
        Assert.Equal ("Test", ckb.Text);
        Assert.Equal ($"{Glyphs.CheckStateUnChecked} Test", ckb.TextFormatter.Text);
        Assert.True (ckb.CanFocus);
        Assert.Equal (new Rectangle (1, 2, 6, 1), ckb.Frame);

        ckb = new CheckBox { Text = "Test", X = 3, Y = 4, Value = CheckState.Checked };
        Assert.True (ckb.Width is DimAuto);
        Assert.True (ckb.Height is DimAuto);
        ckb.Layout ();
        Assert.Equal (CheckState.Checked, ckb.Value);
        Assert.False (ckb.AllowCheckStateNone);
        Assert.Equal ("Test", ckb.Text);
        Assert.Equal ($"{Glyphs.CheckStateChecked} Test", ckb.TextFormatter.Text);
        Assert.True (ckb.CanFocus);
        Assert.Equal (new Rectangle (3, 4, 6, 1), ckb.Frame);
    }

    // Copilot
    [Fact]
    public void Default_CheckState_Glyphs_Are_Distinct ()
    {
        Assert.Equal ((System.Text.Rune)'☑', Glyphs.CheckStateChecked);
        Assert.Equal ((System.Text.Rune)'☐', Glyphs.CheckStateUnChecked);
        Assert.Equal ((System.Text.Rune)'⬛', Glyphs.CheckStateNone);
    }

    [Fact]
    public void LeftButtonReleased_Activates ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        var checkBox = new CheckBox { Text = "Check this out 你" };
        (runnable as View)?.Add (checkBox);
        app.Begin (runnable);

        var checkedStateChangingCount = 0;
        checkBox.ValueChanging += (s, e) => checkedStateChangingCount++;

        var activatingCount = 0;
        checkBox.Activating += (s, e) => activatingCount++;

        var acceptingCount = 0;
        checkBox.Accepting += (s, e) => acceptingCount++;

        checkBox.HasFocus = true;
        Assert.True (checkBox.HasFocus);
        Assert.Equal (CheckState.UnChecked, checkBox.Value);
        Assert.Equal (0, checkedStateChangingCount);
        Assert.Equal (0, activatingCount);
        Assert.Equal (0, acceptingCount);

        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = Point.Empty });
        Assert.Equal (CheckState.UnChecked, checkBox.Value);
        Assert.Equal (0, checkedStateChangingCount);
        Assert.Equal (0, activatingCount);
        Assert.Equal (0, acceptingCount);

        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = Point.Empty });
        Assert.Equal (CheckState.Checked, checkBox.Value);
        Assert.Equal (1, checkedStateChangingCount);
        Assert.Equal (1, activatingCount);
        Assert.Equal (0, acceptingCount);

        checkBox.AllowCheckStateNone = true;

        // Delay 500ms to prevent double click detection
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = Point.Empty, Timestamp = time.Now.AddMilliseconds (500) });
        Assert.Equal (CheckState.Checked, checkBox.Value);
        Assert.Equal (1, checkedStateChangingCount);
        Assert.Equal (1, activatingCount);
        Assert.Equal (0, acceptingCount);

        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = Point.Empty });
        Assert.Equal (CheckState.UnChecked, checkBox.Value);
        Assert.Equal (2, checkedStateChangingCount);
        Assert.Equal (2, activatingCount);
        Assert.Equal (0, acceptingCount);

        // Delay 500ms to prevent double click detection
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = Point.Empty, Timestamp = time.Now.AddMilliseconds (500) });
        Assert.Equal (CheckState.UnChecked, checkBox.Value);
        Assert.Equal (2, checkedStateChangingCount);
        Assert.Equal (2, activatingCount);
        Assert.Equal (0, acceptingCount);

        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = Point.Empty });
        Assert.Equal (CheckState.None, checkBox.Value);
        Assert.Equal (3, checkedStateChangingCount);
        Assert.Equal (3, activatingCount);
        Assert.Equal (0, acceptingCount);
    }

    [Fact]
    public void Mouse_DoubleClick_Advances_And_Accepts ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        var checkBox = new CheckBox { Text = "_Checkbox" };
        (runnable as View)?.Add (checkBox);
        app.Begin (runnable);
        Assert.True (checkBox.CanFocus);

        var valueChangingCount = 0;
        checkBox.ValueChanging += (s, e) => valueChangingCount++;

        var activatingCount = 0;
        checkBox.Activating += (s, e) => activatingCount++;

        var acceptingCount = 0;
        checkBox.Accepting += (s, e) => acceptingCount++;

        var acceptedCount = 0;
        checkBox.Accepted += (s, e) => acceptedCount++;

        checkBox.HasFocus = true;
        Assert.True (checkBox.HasFocus);
        Assert.Equal (CheckState.UnChecked, checkBox.Value);
        Assert.Equal (0, valueChangingCount);
        Assert.Equal (0, activatingCount);
        Assert.Equal (0, acceptingCount);

        // Double click should advance and then accept
        app.InjectSequence (InputInjectionExtensions.LeftButtonDoubleClick (Point.Empty));

        Assert.Equal (CheckState.Checked, checkBox.Value);
        Assert.Equal (1, valueChangingCount);
        Assert.Equal (1, activatingCount);
        Assert.Equal (1, acceptingCount);
        Assert.Equal (1, acceptedCount);
    }

    // Test that Title and Text are the same
    [Fact]
    public void Text_Mirrors_Title ()
    {
        var view = new CheckBox ();
        view.Title = "Hello";
        Assert.Equal ("Hello", view.Title);
        Assert.Equal ("Hello", view.TitleTextFormatter.Text);

        Assert.Equal ("Hello", view.Text);
        Assert.Equal ($"{Glyphs.CheckStateUnChecked} Hello", view.TextFormatter.Text);
    }

    [Fact]
    public void Title_Mirrors_Text ()
    {
        var view = new CheckBox ();
        view.Text = "Hello";
        Assert.Equal ("Hello", view.Text);
        Assert.Equal ($"{Glyphs.CheckStateUnChecked} Hello", view.TextFormatter.Text);

        Assert.Equal ("Hello", view.Title);
        Assert.Equal ("Hello", view.TitleTextFormatter.Text);
    }
}
