#nullable disable
namespace ViewsTests;

public class OptionSelectorTests
{
    [Fact]
    public void Initialization_ShouldSetDefaults ()
    {
        OptionSelector optionSelector = new ();

        Assert.True (optionSelector.CanFocus);
        Assert.Equal (Dim.Auto (DimAutoStyle.Content), optionSelector.Width);
        Assert.Equal (Dim.Auto (DimAutoStyle.Content), optionSelector.Height);
        Assert.Equal (Orientation.Vertical, optionSelector.Orientation);
        Assert.Equal (0, optionSelector.Value);
        Assert.Null (optionSelector.Labels);
    }

    [Fact]
    public void Initialization_With_Options_Value_Is_First ()
    {
        OptionSelector optionSelector = new ();
        List<string> options = ["Option1", "Option2"];

        optionSelector.Labels = options;
        Assert.Equal (0, optionSelector.Value);

        CheckBox checkBox = optionSelector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Option1");
        Assert.Equal (CheckState.Checked, checkBox.Value);
    }

    [Fact]
    public void SetOptions_ShouldCreateCheckBoxes ()
    {
        OptionSelector optionSelector = new ();
        List<string> options = ["Option1", "Option2", "Option3"];

        optionSelector.Labels = options;

        Assert.Equal (options, optionSelector.Labels);
        Assert.Equal (options.Count, optionSelector.SubViews.OfType<CheckBox> ().Count ());
        Assert.Contains (optionSelector.SubViews, sv => sv is CheckBox { Title: "Option1" });
        Assert.Contains (optionSelector.SubViews, sv => sv is CheckBox { Title: "Option2" });
        Assert.Contains (optionSelector.SubViews, sv => sv is CheckBox { Title: "Option3" });
    }

    [Fact]
    public void Value_Set_ShouldUpdateCheckedState ()
    {
        OptionSelector optionSelector = new ();
        List<string> options = ["Option1", "Option2"];

        optionSelector.Labels = options;
        optionSelector.Value = 1;

        CheckBox selectedCheckBox = optionSelector.SubViews.OfType<CheckBox> ().First (cb => optionSelector.GetCheckBoxValue (cb) == 1);
        Assert.Equal (CheckState.Checked, selectedCheckBox.Value);

        CheckBox unselectedCheckBox = optionSelector.SubViews.OfType<CheckBox> ().First (cb => optionSelector.GetCheckBoxValue (cb) == 0);
        Assert.Equal (CheckState.UnChecked, unselectedCheckBox.Value);
    }

    [Fact]
    public void Value_Set_OutOfRange_ShouldThrow ()
    {
        OptionSelector optionSelector = new ();
        List<string> options = ["Option1", "Option2"];

        optionSelector.Labels = options;

        Assert.Throws<ArgumentOutOfRangeException> (() => optionSelector.Value = -1);
        Assert.Throws<ArgumentOutOfRangeException> (() => optionSelector.Value = 2);
    }

    [Fact]
    public void ValueChanged_Event_ShouldBeRaised ()
    {
        OptionSelector optionSelector = new ();
        List<string> options = ["Option1", "Option2"];

        optionSelector.Labels = options;
        var eventRaised = false;
        optionSelector.ValueChanged += (_, _) => eventRaised = true;

        optionSelector.Value = 1;

        Assert.True (eventRaised);
    }

    [Fact]
    public void AssignHotKeys_ShouldAssignUniqueHotKeys ()
    {
        var optionSelector = new OptionSelector { AssignHotKeys = true };
        List<string> options = ["Option1", "Option2"];

        optionSelector.Labels = options;

        List<CheckBox> checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToList ();
        Assert.Contains ('_', checkBoxes [0].Title);
        Assert.Contains ('_', checkBoxes [1].Title);
    }

    [Fact]
    public void Orientation_Set_ShouldUpdateLayout ()
    {
        OptionSelector optionSelector = new ();
        List<string> options = ["Option1", "Option2"];

        optionSelector.Labels = options;
        optionSelector.Orientation = Orientation.Horizontal;

        foreach (CheckBox checkBox in optionSelector.SubViews.OfType<CheckBox> ())
        {
            Assert.Equal (0, checkBox.Y);
        }
    }

    [Fact]
    public void HotKey_No_Value_Selects_First ()
    {
        var superView = new View { CanFocus = true };
        superView.Add (new View { CanFocus = true });

        var selector = new OptionSelector { HotKey = Key.G.WithAlt, Labels = ["_Left", "_Right", "Cen_tered", "_Justified"] };
        selector.Value = null;

        superView.Add (selector);

        Assert.False (selector.HasFocus);
        Assert.Null (selector.Value);

        selector.NewKeyDownEvent (Key.G.WithAlt);

        Assert.Equal (0, selector.Value);
        Assert.Equal (selector.SubViews.OfType<CheckBox> ().First (), superView.MostFocused);
    }

    [Fact]
    public void Accept_Command_Fires_Accept ()
    {
        OptionSelector optionSelector = new ();
        optionSelector.Labels = new List<string> { "Option1", "Option2" };
        var accepted = false;

        optionSelector.Accepting += OnAccept;
        optionSelector.InvokeCommand (Command.Accept);

        Assert.True (accepted);

        return;

        void OnAccept (object sender, CommandEventArgs e) => accepted = true;
    }

    [Fact]
    public void LeftButtonClicked_On_Activated_Does_Nothing ()
    {
        OptionSelector optionSelector = new ();
        List<string> options = ["Option1", "Option2"];

        optionSelector.Labels = options;
        optionSelector.Layout ();

        CheckBox checkBox = optionSelector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Option1");
        Assert.Equal (0, optionSelector.Value);
        Assert.Equal (CheckState.Checked, checkBox.Value);

        var mouse = new Mouse { Position = checkBox.Frame.Location, Flags = MouseFlags.LeftButtonClicked };

        checkBox.NewMouseEvent (mouse);

        Assert.Equal (0, optionSelector.Value);
        Assert.Equal (CheckState.Checked, checkBox.Value);
        Assert.Equal (CheckState.UnChecked, optionSelector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Option2").Value);
    }

    [Fact]
    public void LeftButtonPressed_On_NotActivated_Activates ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        var optionSelector = new OptionSelector ();
        List<string> options = ["Option1", "Option2"];
        optionSelector.Labels = options;

        ((View)runnable).Add (optionSelector);
        app.Begin (runnable);

        CheckBox checkBox = optionSelector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Option2");
        Assert.Equal (0, optionSelector.Value);
        Assert.Equal (CheckState.Checked, optionSelector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Option1").Value);
        Assert.Equal (CheckState.UnChecked, checkBox.Value);

        app.InjectMouse (new Mouse { ScreenPosition = checkBox.Frame.Location, Flags = MouseFlags.LeftButtonPressed });
        Assert.Equal (0, optionSelector.Value);
        Assert.Equal (CheckState.Checked, optionSelector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Option1").Value);
        Assert.Equal (CheckState.UnChecked, checkBox.Value);

        app.InjectMouse (new Mouse { ScreenPosition = checkBox.Frame.Location, Flags = MouseFlags.LeftButtonReleased });
        Assert.Equal (1, optionSelector.Value);
        Assert.Equal (CheckState.Checked, checkBox.Value);
        Assert.Equal (CheckState.UnChecked, optionSelector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Option1").Value);
    }

    // Copilot
    [Fact]
    public void Key_Space_On_Activated_Does_Nothing ()
    {
        OptionSelector optionSelector = new ();
        List<string> options = ["Option1", "Option2"];

        optionSelector.Labels = options;
        optionSelector.Layout ();

        CheckBox checkBox = optionSelector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Option1");
        Assert.Equal (0, optionSelector.Value);
        Assert.Equal (CheckState.Checked, checkBox.Value);

        // Space on an already-selected option is a no-op (no cycling)
        checkBox.NewKeyDownEvent (Key.Space);

        Assert.Equal (0, optionSelector.Value);
        Assert.Equal (CheckState.Checked, checkBox.Value);
        Assert.Equal (CheckState.UnChecked, optionSelector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Option2").Value);
    }

    [Fact]
    public void Key_Space_On_NotActivated_Activates ()
    {
        OptionSelector optionSelector = new ();
        List<string> options = ["Option1", "Option2"];

        optionSelector.Labels = options;
        optionSelector.Layout ();

        CheckBox checkBox = optionSelector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Option2");
        Assert.Equal (0, optionSelector.Value);
        Assert.Equal (CheckState.Checked, optionSelector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Option1").Value);
        Assert.Equal (CheckState.UnChecked, checkBox.Value);

        checkBox.NewKeyDownEvent (Key.Space);

        Assert.Equal (1, optionSelector.Value);
        Assert.Equal (CheckState.Checked, checkBox.Value);
        Assert.Equal (CheckState.UnChecked, optionSelector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Option1").Value);
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     MINIMAL REPRO: OptionSelector inside a Shortcut with Application context.
    ///     LeftButtonReleased on "C" checkbox propagates to Shortcut (CheckBox doesn't handle it),
    ///     Shortcut dispatches down to OptionSelector. Value should change to 2 ("C"), not Cycle to 1.
    /// </summary>
    [Fact]
    public void OptionSelector_In_Shortcut_MouseClick_Selects_Correct_Item ()
    {
        // Need Application context for focus propagation (LeftButtonPressed sets focus)
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        OptionSelector optionSelector = new () { Labels = ["A", "B", "C"] };
        Shortcut shortcut = new () { Key = Key.F5, CommandView = optionSelector };

        ((View)runnable).Add (shortcut);
        app.Begin (runnable);

        Assert.Equal (0, optionSelector.Value);

        // Find "C" checkbox (index 2)
        CheckBox checkBoxC = optionSelector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "C");

        // Simulate: LeftButtonPressed (sets focus) then LeftButtonReleased (triggers Activate on Shortcut)
        Point pos = checkBoxC.FrameToScreen ().Location;
        app.InjectMouse (new Mouse { ScreenPosition = pos, Position = pos, Flags = MouseFlags.LeftButtonPressed });
        app.InjectMouse (new Mouse { ScreenPosition = pos, Position = pos, Flags = MouseFlags.LeftButtonReleased });

        // Assert: Value should be 2 ("C"), not 1 (Cycle from 0)
        Assert.Equal (2, optionSelector.Value);
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     OptionSelector inside a Shortcut. LeftButtonClicked (which CheckBox handles) on a
    ///     non-selected checkbox. This is the event that the real driver synthesizes after
    ///     Pressed+Released. If this fails, the Shortcut dispatch is intercepting it.
    /// </summary>
    [Fact]
    public void OptionSelector_In_Shortcut_LeftButtonClicked_Selects_Correct_Item ()
    {
        OptionSelector optionSelector = new () { Labels = ["A", "B", "C"] };
        Shortcut shortcut = new () { Key = Key.F5, CommandView = optionSelector };
        shortcut.Layout ();

        Assert.Equal (0, optionSelector.Value);

        CheckBox checkBoxC = optionSelector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "C");

        // LeftButtonClicked — this is what CheckBox binds to Command.Activate
        Mouse mouse = new () { Position = checkBoxC.Frame.Location, Flags = MouseFlags.LeftButtonClicked };
        checkBoxC.NewMouseEvent (mouse);

        Assert.Equal (2, optionSelector.Value);
    }

    [Fact]
    public void Values_ShouldUseOptions_WhenValuesIsNull ()
    {
        OptionSelector optionSelector = new ();
        Assert.Null (optionSelector.Values); // Initially null

        List<string> options = ["Option1", "Option2", "Option3"];

        optionSelector.Labels = options;

        IReadOnlyList<int> values = optionSelector.Values;

        Assert.NotNull (values);
        Assert.Equal (Enumerable.Range (0, options.Count).ToList (), values);
    }

    [Fact]
    public void Values_NonSequential_ShouldWorkCorrectly ()
    {
        // Arrange
        OptionSelector optionSelector = new ();
        List<string> options = ["Option _1", "Option _2", "Option _3"];
        List<int> values = [0, 1, 5];

        optionSelector.Labels = options;
        optionSelector.Values = values;

        // Act & Assert
        Assert.Equal (values, optionSelector.Values);
        Assert.Equal (options, optionSelector.Labels);

        // Verify that the Value property updates correctly
        optionSelector.Value = 5;
        Assert.Equal (5, optionSelector.Value);

        // Verify that the CheckBox states align with the non-sequential Values
        CheckBox selectedCheckBox = optionSelector.SubViews.OfType<CheckBox> ().First (cb => optionSelector.GetCheckBoxValue (cb) == 5);
        Assert.Equal (CheckState.Checked, selectedCheckBox.Value);

        CheckBox unselectedCheckBox = optionSelector.SubViews.OfType<CheckBox> ().First (cb => optionSelector.GetCheckBoxValue (cb) == 0); // Index 0 corresponds to value 0
        Assert.Equal (CheckState.UnChecked, unselectedCheckBox.Value);
    }

    [Fact]
    public void Item_HotKey_Null_Value_Changes_Value_And_SetsFocus ()
    {
        var superView = new View { CanFocus = true };
        superView.Add (new View { Id = "otherView", CanFocus = true });
        var selector = new OptionSelector ();
        selector.Labels = ["_One", "_Two"];
        superView.Add (selector);
        superView.SetFocus ();

        Assert.False (selector.HasFocus);
        Assert.Equal (0, selector.Value);
        selector.Value = null;
        Assert.False (selector.HasFocus);

        selector.NewKeyDownEvent (Key.T);

        Assert.True (selector.HasFocus);
        Assert.Equal (1, selector.Value);
    }

    [Fact]
    public void FocusedItem_Get_ReturnsCorrectIndex ()
    {
        OptionSelector optionSelector = new ();
        List<string> options = ["Option1", "Option2", "Option3"];

        optionSelector.Labels = options;
        optionSelector.Layout ();

        // Set focus to second checkbox
        CheckBox secondCheckBox = optionSelector.SubViews.OfType<CheckBox> ().ToArray () [1];
        secondCheckBox.SetFocus ();

        Assert.Equal (1, optionSelector.FocusedItem);

        // Set focus to third checkbox
        CheckBox thirdCheckBox = optionSelector.SubViews.OfType<CheckBox> ().ToArray () [2];
        thirdCheckBox.SetFocus ();

        Assert.Equal (2, optionSelector.FocusedItem);
    }

    [Fact]
    public void FocusedItem_Get_WhenNotFocusable_ReturnsZero ()
    {
        var optionSelector = new OptionSelector { CanFocus = false };
        List<string> options = ["Option1", "Option2", "Option3"];

        optionSelector.Labels = options;
        optionSelector.Layout ();

        Assert.Equal (0, optionSelector.FocusedItem);
    }

    [Fact]
    public void FocusedItem_Set_ShouldMoveFocusToCorrectCheckBox ()
    {
        OptionSelector optionSelector = new ();
        List<string> options = ["Option1", "Option2", "Option3"];

        optionSelector.Labels = options;
        optionSelector.SetFocus (); // Set focus to optionSelector
        optionSelector.Layout ();

        // Set cursor to second checkbox
        optionSelector.FocusedItem = 1;

        CheckBox [] checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToArray ();
        Assert.True (checkBoxes [1].HasFocus);
        Assert.Equal (1, optionSelector.FocusedItem);

        // Set cursor to third checkbox
        optionSelector.FocusedItem = 2;

        Assert.True (checkBoxes [2].HasFocus);
        Assert.Equal (2, optionSelector.FocusedItem);
    }

    [Fact]
    public void FocusedItem_Set_OutOfRange_ShouldThrow ()
    {
        OptionSelector optionSelector = new ();
        List<string> options = ["Option1", "Option2", "Option3"];

        optionSelector.Labels = options;
        optionSelector.Layout ();

        Assert.Throws<ArgumentOutOfRangeException> (() => optionSelector.FocusedItem = -1);
        Assert.Throws<ArgumentOutOfRangeException> (() => optionSelector.FocusedItem = 3);
    }

    [Fact]
    public void FocusedItem_Set_WhenNotFocusable_DoesNothing ()
    {
        var optionSelector = new OptionSelector { CanFocus = false };
        List<string> options = ["Option1", "Option2", "Option3"];

        optionSelector.Labels = options;
        optionSelector.Layout ();

        // Should not throw
        optionSelector.FocusedItem = 1;

        // Verify nothing changed
        Assert.Equal (0, optionSelector.FocusedItem);
        Assert.False (optionSelector is { } && optionSelector.SubViews.OfType<CheckBox> ().Any (cb => cb.HasFocus));
    }

    [Fact]
    public void FocusedItem_DoesNotChangeValue ()
    {
        OptionSelector optionSelector = new ();
        List<string> options = ["Option1", "Option2", "Option3"];

        optionSelector.Labels = options;
        optionSelector.Value = 0; // First option is selected
        optionSelector.SetFocus (); // Set focus to optionSelector
        optionSelector.Layout ();

        // Move cursor to second checkbox
        optionSelector.FocusedItem = 1;

        // Value should not change, only focus moves
        Assert.Equal (0, optionSelector.Value);
        Assert.Equal (1, optionSelector.FocusedItem);

        CheckBox [] checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToArray ();
        Assert.Equal (CheckState.Checked, checkBoxes [0].Value);
        Assert.Equal (CheckState.UnChecked, checkBoxes [1].Value);
        Assert.True (checkBoxes [1].HasFocus);
    }

    // Copilot
    // Programmatic Activate on OptionSelector (no CheckBox source) cycles to next option
    [Fact]
    public void OptionSelector_Command_Activate_ForwardsToFocusedCheckBox ()
    {
        OptionSelector optionSelector = new ();
        optionSelector.Labels = ["Option1", "Option2"];
        optionSelector.BeginInit ();
        optionSelector.EndInit ();
        optionSelector.SetFocus ();

        Assert.Equal (0, optionSelector.Value);

        // Activate without a CheckBox source cycles (same as HotKey)
        optionSelector.InvokeCommand (Command.Activate);

        Assert.Equal (1, optionSelector.Value);

        optionSelector.Dispose ();
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void OptionSelector_Command_Accept_RaisesAccepting ()
    {
        OptionSelector optionSelector = new ();
        optionSelector.Labels = ["Option1", "Option2"];
        var acceptingFired = false;

        optionSelector.Accepting += (_, e) =>
                                    {
                                        acceptingFired = true;
                                        e.Handled = true;
                                    };

        bool? result = optionSelector.InvokeCommand (Command.Accept);

        Assert.True (acceptingFired);
        Assert.True (result);

        optionSelector.Dispose ();
    }

    // Claude - Opus 4.6
    // Per OptionSelector spec: HotKey restores focus and advances Active
    [Fact]
    public void OptionSelector_Command_HotKey_ForwardsToFocusedItem ()
    {
        OptionSelector optionSelector = new ();
        optionSelector.Labels = ["Option1", "Option2"];
        optionSelector.BeginInit ();
        optionSelector.EndInit ();

        Assert.Equal (0, optionSelector.Value);

        // HotKey should restore focus and advance Active (Cycle)
        optionSelector.InvokeCommand (Command.HotKey);

        Assert.True (optionSelector.HasFocus);
        Assert.Equal (1, optionSelector.Value);

        optionSelector.Dispose ();
    }

    #region Navigation Command Tests (Down, Up, Right, Left)

    // Vertical Orientation - Down Command Tests

    [Fact]
    public void Command_Down_Vertical_MovesFocusToNextCheckBox ()
    {
        OptionSelector optionSelector = new () { Orientation = Orientation.Vertical, TabBehavior = TabBehavior.NoStop };
        optionSelector.Labels = ["Option1", "Option2", "Option3"];
        optionSelector.SetFocus ();
        optionSelector.Layout ();

        CheckBox [] checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToArray ();
        checkBoxes [0].SetFocus ();
        Assert.True (checkBoxes [0].HasFocus);

        optionSelector.InvokeCommand (Command.Down);

        Assert.True (checkBoxes [1].HasFocus);
        Assert.False (checkBoxes [0].HasFocus);

        optionSelector.Dispose ();
    }

    [Fact]
    public void Command_Down_Vertical_WrapsAroundToFirst ()
    {
        OptionSelector optionSelector = new () { Orientation = Orientation.Vertical, TabBehavior = TabBehavior.NoStop };
        optionSelector.Labels = ["Option1", "Option2", "Option3"];
        optionSelector.SetFocus ();
        optionSelector.Layout ();

        CheckBox [] checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToArray ();
        checkBoxes [2].SetFocus (); // Focus last checkbox
        Assert.True (checkBoxes [2].HasFocus);

        optionSelector.InvokeCommand (Command.Down);

        Assert.True (checkBoxes [0].HasFocus); // Should wrap to first
        Assert.False (checkBoxes [2].HasFocus);

        optionSelector.Dispose ();
    }

    [Fact]
    public void Command_Down_Horizontal_ReturnsFalse ()
    {
        OptionSelector optionSelector = new () { Orientation = Orientation.Horizontal };
        optionSelector.Labels = ["Option1", "Option2", "Option3"];
        optionSelector.SetFocus ();
        optionSelector.Layout ();

        CheckBox [] checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToArray ();
        checkBoxes [0].SetFocus ();

        bool? result = optionSelector.InvokeCommand (Command.Down);

        Assert.False (result);
        Assert.True (checkBoxes [0].HasFocus); // Focus should not change

        optionSelector.Dispose ();
    }

    // Vertical Orientation - Up Command Tests

    [Fact]
    public void Command_Up_Vertical_MovesFocusToPreviousCheckBox ()
    {
        OptionSelector optionSelector = new () { Orientation = Orientation.Vertical, TabBehavior = TabBehavior.NoStop };
        optionSelector.Labels = ["Option1", "Option2", "Option3"];
        optionSelector.SetFocus ();
        optionSelector.Layout ();

        CheckBox [] checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToArray ();
        checkBoxes [1].SetFocus ();
        Assert.True (checkBoxes [1].HasFocus);

        optionSelector.InvokeCommand (Command.Up);

        Assert.True (checkBoxes [0].HasFocus);
        Assert.False (checkBoxes [1].HasFocus);

        optionSelector.Dispose ();
    }

    [Fact]
    public void Command_Up_Vertical_WrapsAroundToLast ()
    {
        OptionSelector optionSelector = new () { Orientation = Orientation.Vertical, TabBehavior = TabBehavior.NoStop };
        optionSelector.Labels = ["Option1", "Option2", "Option3"];
        optionSelector.SetFocus ();
        optionSelector.Layout ();

        CheckBox [] checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToArray ();
        checkBoxes [0].SetFocus (); // Focus first checkbox
        Assert.True (checkBoxes [0].HasFocus);

        optionSelector.InvokeCommand (Command.Up);

        Assert.True (checkBoxes [2].HasFocus); // Should wrap to last
        Assert.False (checkBoxes [0].HasFocus);

        optionSelector.Dispose ();
    }

    [Fact]
    public void Command_Up_Horizontal_ReturnsFalse ()
    {
        OptionSelector optionSelector = new () { Orientation = Orientation.Horizontal, TabBehavior = TabBehavior.NoStop };
        optionSelector.Labels = ["Option1", "Option2", "Option3"];
        optionSelector.SetFocus ();
        optionSelector.Layout ();

        CheckBox [] checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToArray ();
        checkBoxes [1].SetFocus ();

        bool? result = optionSelector.InvokeCommand (Command.Up);

        Assert.False (result);
        Assert.True (checkBoxes [1].HasFocus); // Focus should not change

        optionSelector.Dispose ();
    }

    // Horizontal Orientation - Right Command Tests

    [Fact]
    public void Command_Right_Horizontal_MovesFocusToNextCheckBox ()
    {
        OptionSelector optionSelector = new () { Orientation = Orientation.Horizontal, TabBehavior = TabBehavior.NoStop };
        optionSelector.Labels = ["Option1", "Option2", "Option3"];
        optionSelector.SetFocus ();
        optionSelector.Layout ();

        CheckBox [] checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToArray ();
        checkBoxes [0].SetFocus ();
        Assert.True (checkBoxes [0].HasFocus);

        optionSelector.InvokeCommand (Command.Right);

        Assert.True (checkBoxes [1].HasFocus);
        Assert.False (checkBoxes [0].HasFocus);

        optionSelector.Dispose ();
    }

    [Fact]
    public void Command_Right_Horizontal_WrapsAroundToFirst ()
    {
        OptionSelector optionSelector = new () { Orientation = Orientation.Horizontal, TabBehavior = TabBehavior.NoStop };
        optionSelector.Labels = ["Option1", "Option2", "Option3"];
        optionSelector.SetFocus ();
        optionSelector.Layout ();

        CheckBox [] checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToArray ();
        checkBoxes [2].SetFocus (); // Focus last checkbox
        Assert.True (checkBoxes [2].HasFocus);

        optionSelector.InvokeCommand (Command.Right);

        Assert.True (checkBoxes [0].HasFocus); // Should wrap to first
        Assert.False (checkBoxes [2].HasFocus);

        optionSelector.Dispose ();
    }

    [Fact]
    public void Command_Right_Vertical_ReturnsFalse ()
    {
        OptionSelector optionSelector = new () { Orientation = Orientation.Vertical };
        optionSelector.Labels = ["Option1", "Option2", "Option3"];
        optionSelector.SetFocus ();
        optionSelector.Layout ();

        CheckBox [] checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToArray ();
        checkBoxes [0].SetFocus ();

        bool? result = optionSelector.InvokeCommand (Command.Right);

        Assert.False (result);
        Assert.True (checkBoxes [0].HasFocus); // Focus should not change

        optionSelector.Dispose ();
    }

    // Horizontal Orientation - Left Command Tests

    [Fact]
    public void Command_Left_Horizontal_MovesFocusToPreviousCheckBox ()
    {
        OptionSelector optionSelector = new () { Orientation = Orientation.Horizontal, TabBehavior = TabBehavior.NoStop };
        optionSelector.Labels = ["Option1", "Option2", "Option3"];
        optionSelector.SetFocus ();
        optionSelector.Layout ();

        CheckBox [] checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToArray ();
        checkBoxes [1].SetFocus ();
        Assert.True (checkBoxes [1].HasFocus);

        optionSelector.InvokeCommand (Command.Left);

        Assert.True (checkBoxes [0].HasFocus);
        Assert.False (checkBoxes [1].HasFocus);

        optionSelector.Dispose ();
    }

    [Fact]
    public void Command_Left_Horizontal_WrapsAroundToLast ()
    {
        OptionSelector optionSelector = new () { Orientation = Orientation.Horizontal, TabBehavior = TabBehavior.NoStop };
        optionSelector.Labels = ["Option1", "Option2", "Option3"];
        optionSelector.SetFocus ();
        optionSelector.Layout ();

        CheckBox [] checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToArray ();
        checkBoxes [0].SetFocus (); // Focus first checkbox
        Assert.True (checkBoxes [0].HasFocus);

        optionSelector.InvokeCommand (Command.Left);

        Assert.True (checkBoxes [2].HasFocus); // Should wrap to last
        Assert.False (checkBoxes [0].HasFocus);

        optionSelector.Dispose ();
    }

    [Fact]
    public void Command_Left_Vertical_ReturnsFalse ()
    {
        OptionSelector optionSelector = new () { Orientation = Orientation.Vertical };
        optionSelector.Labels = ["Option1", "Option2", "Option3"];
        optionSelector.SetFocus ();
        optionSelector.Layout ();

        CheckBox [] checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToArray ();
        checkBoxes [1].SetFocus ();

        bool? result = optionSelector.InvokeCommand (Command.Left);

        Assert.False (result);
        Assert.True (checkBoxes [1].HasFocus); // Focus should not change

        optionSelector.Dispose ();
    }

    // ShowValue Style Tests

    [Fact]
    public void Command_Down_Vertical_WithShowValue_FocusesValueFieldAtEnd ()
    {
        OptionSelector optionSelector = new () { Orientation = Orientation.Vertical, Styles = SelectorStyles.ShowValue, TabBehavior = TabBehavior.NoStop };
        optionSelector.Labels = ["Option1", "Option2"];
        optionSelector.SetFocus ();
        optionSelector.Layout ();

        CheckBox [] checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToArray ();
        checkBoxes [1].SetFocus (); // Focus last checkbox

        optionSelector.InvokeCommand (Command.Down);

        // Should focus the value field instead of wrapping
        View valueField = optionSelector.SubViews.FirstOrDefault (v => v.Id == "valueField");
        Assert.NotNull (valueField);
        Assert.False (valueField.HasFocus);

        optionSelector.Dispose ();
    }

    [Fact]
    public void Command_Up_Vertical_WithShowValue_FocusesValueFieldAtStart ()
    {
        OptionSelector optionSelector = new () { Orientation = Orientation.Vertical, Styles = SelectorStyles.ShowValue, TabBehavior = TabBehavior.NoStop };
        optionSelector.Labels = ["Option1", "Option2"];
        optionSelector.SetFocus ();
        optionSelector.Layout ();

        CheckBox [] checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToArray ();
        checkBoxes [0].SetFocus (); // Focus first checkbox

        optionSelector.InvokeCommand (Command.Up);

        // Should focus the value field instead of wrapping
        View valueField = optionSelector.SubViews.FirstOrDefault (v => v.Id == "valueField");
        Assert.NotNull (valueField);
        Assert.False (valueField.HasFocus);

        optionSelector.Dispose ();
    }

    // Navigation Tests

    [Theory]
    [CombinatorialData]
    public void Command_Down_DoesNotChangeValue (TabBehavior tabBehavior)
    {
        OptionSelector optionSelector = new () { Orientation = Orientation.Vertical, TabBehavior = tabBehavior };
        optionSelector.Labels = ["Option1", "Option2", "Option3"];
        optionSelector.Value = 0;
        optionSelector.SetFocus ();
        optionSelector.Layout ();

        CheckBox [] checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToArray ();
        checkBoxes [0].SetFocus ();

        optionSelector.InvokeCommand (Command.Down);

        Assert.Equal (0, optionSelector.Value); // Value should remain unchanged

        Assert.True (tabBehavior == TabBehavior.NoStop ? checkBoxes [1].HasFocus : checkBoxes [0].HasFocus);

        optionSelector.Dispose ();
    }

    [Theory]
    [CombinatorialData]
    public void Command_Up_DoesNotChangeValue (TabBehavior tabBehavior)
    {
        OptionSelector optionSelector = new () { Orientation = Orientation.Vertical, TabBehavior = tabBehavior };
        optionSelector.Labels = ["Option1", "Option2", "Option3"];
        optionSelector.Value = 2;
        optionSelector.SetFocus ();
        optionSelector.Layout ();

        CheckBox [] checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToArray ();
        checkBoxes [2].SetFocus ();

        optionSelector.InvokeCommand (Command.Up);

        Assert.Equal (2, optionSelector.Value); // Value should remain unchanged

        Assert.True (tabBehavior == TabBehavior.NoStop ? checkBoxes [1].HasFocus : checkBoxes [2].HasFocus);

        optionSelector.Dispose ();
    }

    #endregion

    [Fact]
    public void Key_Enter_Activates_And_Accepts_Focused_Item ()
    {
        // Arrange
        OptionSelector optionSelector = new ();
        optionSelector.Labels = ["Option1", "Option2", "Option3"];
        optionSelector.Value = 0;
        optionSelector.SetFocus ();
        optionSelector.Layout ();

        // Focus the second item (which is NOT the active/selected item)
        CheckBox [] checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToArray ();
        checkBoxes [1].SetFocus ();
        Assert.True (checkBoxes [1].HasFocus);
        Assert.Equal (0, optionSelector.Value); // Option1 is still selected

        var activatingCount = 0;
        optionSelector.Activating += (_, _) => activatingCount++;

        var acceptingCount = 0;
        optionSelector.Accepting += (_, _) => acceptingCount++;

        var valueChangedCount = 0;
        optionSelector.ValueChanged += (_, _) => valueChangedCount++;

        // Act - Press Enter on the focused (but not selected) checkbox
        checkBoxes [1].NewKeyDownEvent (Key.Enter);

        // Assert - Per spec: Enter should Activate AND Accept
        Assert.Equal (1, activatingCount);
        Assert.Equal (1, acceptingCount);
        Assert.Equal (1, valueChangedCount);
        Assert.Equal (1, optionSelector.Value); // Should now be Option2
    }

    [Fact]
    public void Key_Enter_On_Active_Item_Accepts ()
    {
        // Arrange
        OptionSelector optionSelector = new ();
        optionSelector.Labels = ["Option1", "Option2", "Option3"];
        optionSelector.Value = 0;
        optionSelector.SetFocus ();
        optionSelector.Layout ();

        // Focus the first item (which IS the active/selected item)
        CheckBox [] checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToArray ();
        checkBoxes [0].SetFocus ();
        Assert.True (checkBoxes [0].HasFocus);
        Assert.Equal (0, optionSelector.Value); // Option1 is still selected

        var activatingCount = 0;
        optionSelector.Activating += (_, _) => activatingCount++;

        var acceptingCount = 0;
        optionSelector.Accepting += (_, _) => acceptingCount++;

        var valueChangedCount = 0;
        optionSelector.ValueChanged += (_, _) => valueChangedCount++;

        // Act - Press Enter on the focused (but not selected) checkbox
        checkBoxes [0].NewKeyDownEvent (Key.Enter);

        // Assert - Per spec: Enter should Activate AND Accept
        Assert.Equal (1, activatingCount);
        Assert.Equal (1, acceptingCount);
        Assert.Equal (0, valueChangedCount);
        Assert.Equal (0, optionSelector.Value); // Should be Option1
        Assert.True (optionSelector.SubViews.OfType<CheckBox> ().Count (cb => cb.Value == CheckState.Checked) == 1);
    }

    #region Value Propagation via Activated Event (CWP Bug Tests)

    // Claude - Sonnet 4.6
    /// <summary>
    ///     Proves the CWP ordering bug: <see cref="OptionSelector.OnActivated"/> calls
    ///     <c>base.OnActivated</c> (which fires <see cref="View.Activated"/>) BEFORE
    ///     <c>ApplyActivation</c> updates <see cref="SelectorBase.Value"/>.
    ///     The <see cref="ICommandContext.Value"/> carried by <see cref="View.Activated"/> must be
    ///     the <see cref="OptionSelector"/>'s semantic <see cref="int"/>? index,
    ///     not the dispatched <see cref="CheckBox"/>'s <see cref="CheckState"/>.
    /// </summary>
    [Fact]
    public void Activated_Event_Value_Is_OptionSelector_Int_Not_CheckState ()
    {
        // Arrange: OptionSelector with two options; second option focused for dispatch.
        OptionSelector optionSelector = new ();
        optionSelector.Labels = ["Option1", "Option2"];
        optionSelector.SetFocus ();
        optionSelector.Layout ();

        Assert.Equal (0, optionSelector.Value); // Option1 selected

        // Focus Option2 so InvokeCommand dispatches Activate to it.
        CheckBox option2 = optionSelector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Option2");
        option2.SetFocus ();

        object capturedValue = null;
        int activatedCount = 0;

        optionSelector.Activated += (_, args) =>
        {
            activatedCount++;
            capturedValue = args.Value?.Value;
        };

        // Act: Activate on the selector dispatches down to the focused CheckBox (Option2).
        optionSelector.InvokeCommand (Command.Activate);

        // Assert: OptionSelector.Value changed to 1 (Option2 selected).
        Assert.Equal (1, optionSelector.Value);
        Assert.Equal (1, activatedCount);

        // The value arriving at Activated must be the OptionSelector's int? (1),
        // NOT the dispatched CheckBox's CheckState (CheckState.Checked).
        Assert.Null (capturedValue as CheckState?);    // Must NOT be a CheckState
        Assert.Equal ((int?)1, capturedValue as int?); // Must be the OptionSelector's index

        optionSelector.Dispose ();
    }

    // Claude - Sonnet 4.6
    /// <summary>
    ///     Proves that an ancestor subscribing to <see cref="View.Activated"/> via
    ///     <see cref="View.CommandsToBubbleUp"/> receives the <see cref="OptionSelector"/>'s
    ///     semantic <see cref="int"/>? value — not the inner <see cref="CheckBox"/>'s
    ///     <see cref="CheckState"/> — through the <c>BubbleActivatedUp</c> path.
    ///     Uses <see cref="Application"/> context so focus propagates correctly through
    ///     the hierarchy and the dispatch goes to the focused CheckBox, not the Cycle path.
    /// </summary>
    [Fact]
    public void Activated_Event_Ancestor_Receives_OptionSelector_Value_Via_BubbleActivatedUp ()
    {
        // Arrange: Application context ensures SetFocus works through nested hierarchy.
        VirtualTimeProvider time = new ();

        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        // ancestor opts in to Activate bubbling and contains the OptionSelector.
        // CanFocus = true is required so the focus chain can propagate through ancestor.
        View ancestor = new () { Id = "ancestor", CanFocus = true };
        ancestor.CommandsToBubbleUp = [Command.Activate];

        OptionSelector optionSelector = new ();
        optionSelector.Labels = ["Option1", "Option2"];
        ancestor.Add (optionSelector);

        (runnable as View)!.Add (ancestor);
        app.Begin (runnable);

        Assert.Equal (0, optionSelector.Value); // Option1 selected

        // Focus Option2 so Activate dispatches to it.
        CheckBox option2 = optionSelector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Option2");
        option2.HasFocus = true;
        Assert.True (option2.HasFocus);

        object capturedValue = null;
        int ancestorActivatedCount = 0;

        ancestor.Activated += (_, args) =>
        {
            ancestorActivatedCount++;
            capturedValue = args.Value?.Value;
        };

        // Act: Activate dispatches down to Option2, then BubbleActivatedUp reaches ancestor.
        optionSelector.InvokeCommand (Command.Activate);

        // Assert: OptionSelector.Value changed; ancestor received Activated exactly once.
        Assert.Equal (1, optionSelector.Value);
        Assert.Equal (1, ancestorActivatedCount);

        // The ancestor must receive the OptionSelector's int? (1), NOT a CheckState.
        Assert.Null (capturedValue as CheckState?);    // Must NOT be a CheckState
        Assert.Equal ((int?)1, capturedValue as int?); // Must be the OptionSelector's index

        ancestor.Dispose ();
    }

    #endregion

    #region OptionSelector<TEnum> Tests

    private enum TestEnumZero
    {
        Alpha,
        Beta,
        Gamma
    }

    private enum TestEnumNonZero
    {
        Alpha = 1,
        Beta = 2,
        Gamma = 3
    }

    private enum TestEnumSparse
    {
        Low = 5,
        Mid = 10,
        High = 20
    }

    [Fact]
    // Copilot
    public void Generic_Initialization_ZeroBased_FirstCheckBoxIsChecked ()
    {
        OptionSelector<TestEnumZero> selector = new ();

        Assert.Equal (TestEnumZero.Alpha, selector.Value);
        Assert.Equal (0, ((OptionSelector)selector).Value);

        List<int> expectedValues = [0, 1, 2];
        Assert.Equal (expectedValues, selector.Values);

        CheckBox first = selector.SubViews.OfType<CheckBox> ().First (cb => selector.GetCheckBoxValue (cb) == 0);
        Assert.Equal (CheckState.Checked, first.Value);
    }

    [Fact]
    // Copilot
    public void Generic_Initialization_NonZeroBased_FirstCheckBoxIsChecked ()
    {
        OptionSelector<TestEnumNonZero> selector = new ();

        Assert.Equal (TestEnumNonZero.Alpha, selector.Value);
        Assert.Equal (1, ((OptionSelector)selector).Value);

        List<int> expectedValues = [1, 2, 3];
        Assert.Equal (expectedValues, selector.Values);

        CheckBox first = selector.SubViews.OfType<CheckBox> ().First (cb => selector.GetCheckBoxValue (cb) == 1);
        Assert.Equal (CheckState.Checked, first.Value);

        // Other checkboxes should be unchecked
        foreach (CheckBox cb in selector.SubViews.OfType<CheckBox> ().Where (cb => selector.GetCheckBoxValue (cb) != 1))
        {
            Assert.Equal (CheckState.UnChecked, cb.Value);
        }
    }

    [Fact]
    // Copilot
    public void Generic_Initialization_SparseEnum_FirstCheckBoxIsChecked ()
    {
        OptionSelector<TestEnumSparse> selector = new ();

        Assert.Equal (TestEnumSparse.Low, selector.Value);
        Assert.Equal (5, ((OptionSelector)selector).Value);

        List<int> expectedValues = [5, 10, 20];
        Assert.Equal (expectedValues, selector.Values);

        CheckBox first = selector.SubViews.OfType<CheckBox> ().First (cb => selector.GetCheckBoxValue (cb) == 5);
        Assert.Equal (CheckState.Checked, first.Value);
    }

    [Fact]
    // Copilot
    public void Generic_Initialization_HasCorrectLabels ()
    {
        OptionSelector<TestEnumZero> selector = new ();

        List<string> expectedLabels = ["Alpha", "Beta", "Gamma"];
        Assert.Equal (expectedLabels, selector.Labels);
        Assert.Equal (3, selector.SubViews.OfType<CheckBox> ().Count ());
    }

    [Fact]
    // Copilot
    public void Generic_Values_Set_Throws ()
    {
        OptionSelector<TestEnumZero> selector = new ();

        Assert.Throws<InvalidOperationException> (() => selector.Values = [1, 2]);
    }

    #endregion
}
