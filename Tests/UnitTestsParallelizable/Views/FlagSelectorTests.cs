namespace ViewsTests;

public class FlagSelectorTests
{
    [Fact]
    public void Accept_Command_Fires_Accept ()
    {
        var flagSelector = new FlagSelector ();
        flagSelector.Values = [0, 1];
        flagSelector.Labels = ["_Left", "_Right"];
        var accepted = false;

        flagSelector.Accepting += OnAccept;
        flagSelector.InvokeCommand (Command.Accept);

        Assert.True (accepted);

        return;

        void OnAccept (object? sender, CommandEventArgs e) => accepted = true;
    }

    [Fact]
    public void Constructors_Defaults ()
    {
        var flagSelector = new FlagSelector ();
        Assert.True (flagSelector.CanFocus);
        Assert.Null (flagSelector.Values);
        Assert.Equal (Rectangle.Empty, flagSelector.Frame);
        Assert.Null (flagSelector.Value);

        flagSelector = new ();
        flagSelector.Values = [1];
        flagSelector.Labels = ["Flag1"];

        Assert.True (flagSelector.CanFocus);
        Assert.Single (flagSelector.Values!);
        Assert.Equal (1, flagSelector.Value);

        flagSelector = new () { X = 1, Y = 2, Width = 20, Height = 5 };
        flagSelector.Values = [1];
        flagSelector.Labels = ["Flag1"];

        Assert.True (flagSelector.CanFocus);
        Assert.Single (flagSelector.Values!);
        Assert.Equal (new (1, 2, 20, 5), flagSelector.Frame);
        Assert.Equal (1, flagSelector.Value);

        flagSelector = new () { X = 1, Y = 2 };
        flagSelector.Values = [1];
        flagSelector.Labels = ["Flag1"];

        var view = new View { Width = 30, Height = 40 };
        view.Add (flagSelector);
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        Assert.True (flagSelector.CanFocus);
        Assert.Single (flagSelector.Values!);
        Assert.Equal (new (1, 2, 7, 1), flagSelector.Frame);
        Assert.Equal (1, flagSelector.Value);
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void FlagSelector_Command_Accept_RaisesAccepting ()
    {
        FlagSelector<SelectorStyles> flagSelector = new ();
        var acceptingFired = false;

        flagSelector.Accepting += (_, e) =>
                                  {
                                      acceptingFired = true;
                                      e.Handled = true;
                                  };

        bool? result = flagSelector.InvokeCommand (Command.Accept);

        Assert.True (acceptingFired);
        Assert.True (result);

        flagSelector.Dispose ();
    }

    [Fact]
    public void FlagSelector_Command_Activate_Changes_Value_And_Activates ()
    {
        using FlagSelector<SelectorStyles> flagSelector = new ();

        CheckBox firstCheckBox = flagSelector.SubViews.OfType<CheckBox> ().ElementAt (0);
        flagSelector.SetFocus ();
        Assert.True (flagSelector.HasFocus);
        Assert.Equal (SelectorStyles.None, flagSelector.Value);

        int cbActivatingRaised = 0;
        firstCheckBox.Activating += (_, _) => cbActivatingRaised++;

        int selectorActivatingRaised = 0;
        flagSelector.Activating += (_, _) => selectorActivatingRaised++;

        int selectorValueChanged = 0;
        flagSelector.ValueChanged += (_, _) => selectorValueChanged++;

        // Activate should forward to the focused CheckBox's Activate
        flagSelector.InvokeCommand (Command.Activate);

        Assert.Equal (1, cbActivatingRaised);
        Assert.Equal (1, selectorActivatingRaised);
        Assert.Equal (1, selectorValueChanged);
        Assert.Equal (SelectorStyles.ShowNoneFlag, flagSelector.Value);
    }

    // Claude - Opus 4.6
    // Per FlagSelector spec: HotKey when focused is a no-op (does NOT change Active)
    [Fact]
    public void FlagSelector_Command_HotKey_WhenFocused_Does_Not_Change_Value ()
    {
        using FlagSelector<SelectorStyles> flagSelector = new ();

        CheckBox firstCheckBox = flagSelector.SubViews.OfType<CheckBox> ().ElementAt (0);
        flagSelector.SetFocus ();
        Assert.True (flagSelector.HasFocus);

        int cbActivatingRaised = 0;
        firstCheckBox.Activating += (_, _) => cbActivatingRaised++;

        int selectorActivatingRaised = 0;
        flagSelector.Activating += (_, _) => selectorActivatingRaised++;

        int selectorHotKeyRaised = 0;
        flagSelector.HandlingHotKey += (_, _) => selectorHotKeyRaised++;

        int selectorValueChanged = 0;
        flagSelector.ValueChanged += (_, _) => selectorValueChanged++;

        SelectorStyles? valueBefore = flagSelector.Value;

        // Per spec: HotKey when focused is a no-op
        flagSelector.InvokeCommand (Command.HotKey);

        // no value change
        Assert.Equal (0, selectorActivatingRaised);
        Assert.Equal (0, selectorHotKeyRaised);
        Assert.Equal (0, selectorValueChanged);
        Assert.Equal (0, cbActivatingRaised);

        // Value should not change
        Assert.Equal (valueBefore, flagSelector.Value);
    }

    // Copilot - Sonnet 4
    // Per issue: Enter should just accept, not toggle, in FlagSelector
    [Fact]
    public void FlagSelector_Enter_Does_Not_Toggle_Value ()
    {
        using FlagSelector<SelectorStyles> flagSelector = new ();

        CheckBox firstCheckBox = flagSelector.SubViews.OfType<CheckBox> ().ElementAt (0);
        flagSelector.SetFocus ();
        Assert.True (flagSelector.HasFocus);

        SelectorStyles? valueBefore = flagSelector.Value;

        int selectorValueChanged = 0;
        flagSelector.ValueChanged += (_, _) => selectorValueChanged++;

        int acceptingFired = 0;
        flagSelector.Accepting += (_, _) => acceptingFired++;

        // Press Enter on the focused checkbox
        firstCheckBox.NewKeyDownEvent (Key.Enter);

        // Value should NOT change (Enter does not toggle in FlagSelector)
        Assert.Equal (0, selectorValueChanged);
        Assert.Equal (valueBefore, flagSelector.Value);

        // But Accepting should fire
        Assert.Equal (1, acceptingFired);
    }

    // Tests for FlagSelector<TEnum>
    [Fact]
    public void GenericInitialization_ShouldSetDefaults ()
    {
        FlagSelector<SelectorStyles> flagSelector = new ();

        Assert.True (flagSelector.CanFocus);
        Assert.Equal (Dim.Auto (DimAutoStyle.Content), flagSelector.Width);
        Assert.Equal (Dim.Auto (DimAutoStyle.Content), flagSelector.Height);
        Assert.Equal (Orientation.Vertical, flagSelector.Orientation);
    }

    [Fact]
    public void GenericSetFlagNames_ShouldSetFlagNames ()
    {
        FlagSelector<SelectorStyles> flagSelector = new ();

        flagSelector.Labels = Enum.GetValues<SelectorStyles> ()
                                  .Select (l => l switch
                                                {
                                                    SelectorStyles.None => "_No Style",
                                                    SelectorStyles.ShowNoneFlag => "_Show None Value Style",
                                                    SelectorStyles.ShowValue => "Show _Value Editor Style",
                                                    SelectorStyles.All => "_All Styles",
                                                    _ => l.ToString ()
                                                })
                                  .ToList ();

        Dictionary<int, string> expectedFlags = Enum.GetValues<SelectorStyles> ()
                                                    .ToDictionary (f => Convert.ToInt32 (f),
                                                                   f => f switch
                                                                        {
                                                                            SelectorStyles.None => "_No Style",
                                                                            SelectorStyles.ShowNoneFlag => "_Show None Value Style",
                                                                            SelectorStyles.ShowValue => "Show _Value Editor Style",
                                                                            SelectorStyles.All => "_All Styles",
                                                                            _ => f.ToString ()
                                                                        });

        Assert.Equal (expectedFlags.Keys, flagSelector.Values);
    }

    [Fact]
    public void GenericValue_Set_ShouldUpdateCheckedState ()
    {
        FlagSelector<SelectorStyles> flagSelector = new ();

        flagSelector.Value = SelectorStyles.ShowNoneFlag;

        CheckBox checkBox = flagSelector.SubViews.OfType<CheckBox> ().First (cb => flagSelector.GetCheckBoxValue (cb) == Convert.ToInt32 (SelectorStyles.ShowNoneFlag));
        Assert.Equal (CheckState.Checked, checkBox.Value);

        checkBox = flagSelector.SubViews.OfType<CheckBox> ().First (cb => flagSelector.GetCheckBoxValue (cb) == Convert.ToInt32 (SelectorStyles.ShowValue));
        Assert.Equal (CheckState.UnChecked, checkBox.Value);
    }

    [Fact]
    public void GenericValueChanged_Event_ShouldBeRaised ()
    {
        FlagSelector<SelectorStyles> flagSelector = new ();

        var eventRaised = false;
        flagSelector.ValueChanged += (sender, args) => eventRaised = true;

        flagSelector.Value = SelectorStyles.ShowNoneFlag;

        Assert.True (eventRaised);
    }

    [Fact]
    public void HotKey_Command_Does_Not_Accept ()
    {
        var flagSelector = new FlagSelector ();
        flagSelector.Values = [0, 1];
        flagSelector.Labels = ["_Left", "_Right"];
        var accepted = false;

        flagSelector.Accepting += OnAccept;
        flagSelector.InvokeCommand (Command.HotKey);

        Assert.False (accepted);

        return;

        void OnAccept (object? sender, CommandEventArgs e) => accepted = true;
    }

    [Fact]
    public void Null_Value_Command_HotKey_Does_Not_Change_Value ()
    {
        var superView = new View { CanFocus = true };
        superView.Add (new View { CanFocus = true });

        var flagSelector = new FlagSelector { Title = "_FlagSelector" };
        flagSelector.Values = [0, 1];
        flagSelector.Labels = ["_Left", "_Right"];
        flagSelector.Value = null;

        superView.Add (flagSelector);

        Assert.False (flagSelector.HasFocus);
        Assert.Null (flagSelector.Value);

        flagSelector.InvokeCommand (Command.HotKey);

        Assert.True (flagSelector.HasFocus);
        Assert.Null (flagSelector.Value);
    }

    [Fact]
    public void HotKey_SetsFocus ()
    {
        var superView = new View { CanFocus = true };
        superView.Add (new View { CanFocus = true });

        var flagSelector = new FlagSelector { Title = "_FlagSelector" };
        flagSelector.Values = [0, 1];
        flagSelector.Labels = ["_Left", "_Right"];

        superView.Add (flagSelector);

        Assert.False (flagSelector.HasFocus);
        Assert.Equal (0, flagSelector.Value);

        flagSelector.NewKeyDownEvent (Key.F.WithAlt);

        Assert.True (flagSelector.HasFocus);
        Assert.Equal (0, flagSelector.Value);
    }

    [Fact]
    public void Initialization_ShouldSetDefaults ()
    {
        var flagSelector = new FlagSelector ();

        Assert.True (flagSelector.CanFocus);
        Assert.Equal (Dim.Auto (DimAutoStyle.Content), flagSelector.Width);
        Assert.Equal (Dim.Auto (DimAutoStyle.Content), flagSelector.Height);
        Assert.Equal (Orientation.Vertical, flagSelector.Orientation);
    }

    [Fact]
    public void Null_Value_Item_KeyDown_HotKey_Changes_Value_And_SetsFocus ()
    {
        var superView = new View { CanFocus = true };
        superView.Add (new View { CanFocus = true });
        var flagSelector = new FlagSelector ();
        flagSelector.Labels = ["_Left", "_Right"];
        superView.Add (flagSelector);

        Assert.False (flagSelector.HasFocus);
        Assert.Null (flagSelector.Value);

        flagSelector.NewKeyDownEvent (Key.R);

        Assert.True (flagSelector.HasFocus);
        Assert.Equal (1, flagSelector.Value);
    }

    [Fact]
    public void Key_Space_On_Activated_NoneFlag_Does_Nothing ()
    {
        FlagSelector selector = new ();
        selector.Styles = SelectorStyles.ShowNoneFlag;
        List<string> options = ["Flag1", "Flag2"];

        selector.Labels = options;
        selector.Layout ();

        CheckBox checkBox = selector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Flag1");
        Assert.Null (selector.Value);
        Assert.Equal (CheckState.UnChecked, checkBox.Value);
        selector.Value = 0;
        Assert.Equal (0, selector.Value);
        Assert.Equal (CheckState.Checked, checkBox.Value);

        checkBox.NewKeyDownEvent (Key.Space);

        Assert.Equal (0, selector.Value);
        Assert.Equal (CheckState.Checked, checkBox.Value);
        Assert.Equal (CheckState.UnChecked, selector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Flag2").Value);
    }

    [Fact]
    public void Key_Space_On_NotActivated_NoneFlag_Activates ()
    {
        FlagSelector selector = new ();
        selector.Styles = SelectorStyles.ShowNoneFlag;

        List<string> options = ["Flag1", "Flag2"];

        selector.Labels = options;
        selector.Layout ();

        CheckBox checkBox = selector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Flag1");
        Assert.Null (selector.Value);
        Assert.Equal (CheckState.UnChecked, checkBox.Value);

        checkBox.NewKeyDownEvent (Key.Space);

        // Assert.Equal (0, selector.Value);
        Assert.Equal (CheckState.Checked, checkBox.Value);
        Assert.Equal (CheckState.UnChecked, selector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Flag2").Value);
    }

    [Fact]
    public void Set_Value_Sets ()
    {
        var superView = new View { CanFocus = true };
        superView.Add (new View { CanFocus = true });
        var flagSelector = new FlagSelector ();
        flagSelector.Labels = ["_Left", "_Right"];
        superView.Add (flagSelector);

        Assert.False (flagSelector.HasFocus);
        Assert.Null (flagSelector.Value);

        flagSelector.Value = 1;

        Assert.False (flagSelector.HasFocus);
        Assert.Equal (1, flagSelector.Value);
    }

    [Fact]
    public void Styles_Set_ShouldCreateSubViews ()
    {
        var flagSelector = new FlagSelector ();
        Dictionary<int, string> flags = new () { { 1, "Flag1" }, { 2, "Flag2" } };

        flagSelector.Values = flags.Keys.ToList ();
        flagSelector.Labels = flags.Values.ToList ();
        flagSelector.Styles = SelectorStyles.ShowNoneFlag;

        Assert.Contains (flagSelector.SubViews, sv => sv is CheckBox cb && cb.Title == "None");
    }

    [Fact]
    public void Value_Set_ShouldUpdateCheckedState ()
    {
        var flagSelector = new FlagSelector ();
        Dictionary<int, string> flags = new () { { 1, "Flag1" }, { 2, "Flag2" } };

        flagSelector.Values = flags.Keys.ToList ();
        flagSelector.Labels = flags.Values.ToList ();
        flagSelector.Value = 1;

        CheckBox checkBox = flagSelector.SubViews.OfType<CheckBox> ().First (cb => flagSelector.GetCheckBoxValue (cb) == 1);
        Assert.Equal (CheckState.Checked, checkBox.Value);

        checkBox = flagSelector.SubViews.OfType<CheckBox> ().First (cb => flagSelector.GetCheckBoxValue (cb) == 2);
        Assert.Equal (CheckState.UnChecked, checkBox.Value);
    }

    [Fact]
    public void ValueChanged_Event ()
    {
        int? newValue = null;
        var flagSelector = new FlagSelector ();
        flagSelector.Values = [0, 1];
        flagSelector.Labels = ["_Left", "_Right"];

        flagSelector.ValueChanged += (s, e) => { newValue = e.NewValue; };

        flagSelector.Value = 1;
        Assert.Equal (newValue, flagSelector.Value);
    }

    [Fact]
    public void ValueChanged_Event_ShouldBeRaised ()
    {
        var flagSelector = new FlagSelector ();
        Dictionary<int, string> flags = new () { { 1, "Flag1" }, { 2, "Flag2" } };

        flagSelector.Values = flags.Keys.ToList ();
        flagSelector.Labels = flags.Values.ToList ();
        var eventRaised = false;
        flagSelector.ValueChanged += (sender, args) => eventRaised = true;

        flagSelector.Value = 2;

        Assert.True (eventRaised);
    }

    #region Mouse Tests

    [Fact]
    public void Mouse_DoubleClick_Accepts () { }

    [Fact]
    public void Mouse_Click_On_Activated_NoneFlag_Does_Nothing ()
    {
        FlagSelector selector = new ();
        selector.Styles = SelectorStyles.ShowNoneFlag;
        List<string> options = ["Flag1", "Flag2"];

        selector.Labels = options;
        selector.Layout ();

        CheckBox checkBox = selector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Flag1");
        Assert.Null (selector.Value);
        Assert.Equal (CheckState.UnChecked, checkBox.Value);
        selector.Value = 0;

        var mouse = new Mouse { Position = checkBox.Frame.Location, Flags = MouseFlags.LeftButtonClicked };

        checkBox.NewMouseEvent (mouse);

        Assert.Equal (0, selector.Value);
        Assert.Equal (CheckState.Checked, checkBox.Value);
        Assert.Equal (CheckState.UnChecked, selector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Flag2").Value);
    }

    [Fact]
    public void Mouse_Click_On_NotActivated_NoneFlag_Toggles ()
    {
        FlagSelector selector = new ();
        selector.Styles = SelectorStyles.ShowNoneFlag;
        List<string> options = ["Flag1", "Flag2"];

        selector.Labels = options;
        selector.Layout ();

        CheckBox checkBox = selector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Flag1");
        Assert.Null (selector.Value);
        Assert.Equal (CheckState.UnChecked, checkBox.Value);
        selector.Value = 0;
        Assert.Equal (CheckState.Checked, checkBox.Value);

        var mouse = new Mouse { Position = checkBox.Frame.Location, Flags = MouseFlags.LeftButtonClicked };

        checkBox.NewMouseEvent (mouse);

        Assert.Equal (0, selector.Value);
        Assert.Equal (CheckState.Checked, checkBox.Value);
        Assert.Equal (CheckState.UnChecked, selector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Flag2").Value);
    }

    #endregion Mouse Tests

    #region FlagSelector-Specific Tests (Non-Base Class Functionality)

    [Fact]
    public void Value_MultipleFlags_CombinesCorrectly ()
    {
        var selector = new FlagSelector ();
        selector.Values = [1, 2, 4];
        selector.Labels = ["Flag1", "Flag2", "Flag3"];

        selector.Value = 1 | 4; // Flags 1 and 3

        CheckBox [] checkBoxes = selector.SubViews.OfType<CheckBox> ().ToArray ();
        Assert.Equal (CheckState.Checked, checkBoxes [0].Value); // Flag 1
        Assert.Equal (CheckState.UnChecked, checkBoxes [1].Value); // Flag 2
        Assert.Equal (CheckState.Checked, checkBoxes [2].Value); // Flag 3
    }

    [Fact]
    public void Value_AllFlags_UpdatesCorrectly ()
    {
        var selector = new FlagSelector ();
        selector.Values = [1, 2, 4];
        selector.Labels = ["Flag1", "Flag2", "Flag3"];

        selector.Value = 1 | 2 | 4; // All flags

        CheckBox [] checkBoxes = selector.SubViews.OfType<CheckBox> ().ToArray ();
        Assert.True (checkBoxes.All (cb => cb.Value == CheckState.Checked));
    }

    [Fact]
    public void Value_ToggleFlag_AddsAndRemovesCorrectly ()
    {
        var selector = new FlagSelector ();
        selector.Values = [1, 2, 4];
        selector.Labels = ["Flag1", "Flag2", "Flag3"];

        selector.Value = 1; // Start with Flag 1
        Assert.Equal (1, selector.Value);

        // Toggle checkbox to add Flag 2
        CheckBox flag2Checkbox = selector.SubViews.OfType<CheckBox> ().First (cb => selector.GetCheckBoxValue (cb) == 2);
        flag2Checkbox.Value = CheckState.Checked;

        Assert.Equal (1 | 2, selector.Value);

        // Toggle checkbox to remove Flag 1
        CheckBox flag1Checkbox = selector.SubViews.OfType<CheckBox> ().First (cb => selector.GetCheckBoxValue (cb) == 1);
        flag1Checkbox.Value = CheckState.UnChecked;

        Assert.Equal (2, selector.Value);
    }

    [Fact]
    public void Value_SetToZero_ChecksNoneFlag ()
    {
        var selector = new FlagSelector ();
        selector.Styles = SelectorStyles.ShowNoneFlag;
        selector.Values = [1, 2];
        selector.Labels = ["Flag1", "Flag2"];

        selector.Value = 0;

        CheckBox? noneCheckBox = selector.SubViews.OfType<CheckBox> ().FirstOrDefault (cb => cb.Title == "None");
        Assert.NotNull (noneCheckBox);
        Assert.Equal (CheckState.Checked, noneCheckBox.Value);

        // All other flags should be unchecked
        Assert.True (selector.SubViews.OfType<CheckBox> ().Where (cb => selector.GetCheckBoxValue (cb) != 0).All (cb => cb.Value == CheckState.UnChecked));
    }

    [Fact]
    public void Value_SetToNull_UnchecksAllIncludingNone ()
    {
        var selector = new FlagSelector ();
        selector.Styles = SelectorStyles.ShowNoneFlag;
        selector.Values = [1, 2];
        selector.Labels = ["Flag1", "Flag2"];
        selector.Value = 1;

        selector.Value = null;

        Assert.True (selector.SubViews.OfType<CheckBox> ().All (cb => cb.Value == CheckState.UnChecked));
    }

    [Fact]
    public void ToggleNoneFlag_UnchecksAllOtherFlags ()
    {
        var selector = new FlagSelector ();
        selector.Styles = SelectorStyles.ShowNoneFlag;
        selector.Values = [1, 2];
        selector.Labels = ["Flag1", "Flag2"];
        selector.Value = 1 | 2; // Start with all flags set

        CheckBox? noneCheckBox = selector.SubViews.OfType<CheckBox> ().FirstOrDefault (cb => cb.Title == "None");
        Assert.NotNull (noneCheckBox);

        noneCheckBox.Value = CheckState.Checked;

        Assert.Equal (0, selector.Value);
        Assert.True (selector.SubViews.OfType<CheckBox> ().Where (cb => selector.GetCheckBoxValue (cb) != 0).All (cb => cb.Value == CheckState.UnChecked));
    }

    [Fact]
    public void ToggleAnyFlag_UnchecksNoneFlag ()
    {
        var selector = new FlagSelector ();
        selector.Styles = SelectorStyles.ShowNoneFlag;
        selector.Values = [1, 2];
        selector.Labels = ["Flag1", "Flag2"];
        selector.Value = 0; // Start with None

        CheckBox? noneCheckBox = selector.SubViews.OfType<CheckBox> ().FirstOrDefault (cb => cb.Title == "None");
        Assert.NotNull (noneCheckBox);
        Assert.Equal (CheckState.Checked, noneCheckBox.Value);

        // Toggle a flag
        CheckBox flag1CheckBox = selector.SubViews.OfType<CheckBox> ().First (cb => selector.GetCheckBoxValue (cb) == 1);
        flag1CheckBox.Value = CheckState.Checked;

        Assert.Equal (1, selector.Value);
        Assert.Equal (CheckState.UnChecked, noneCheckBox.Value);
    }

    [Fact]
    public void NoneFlag_WithoutShowNoneFlag_IsNotCreated ()
    {
        var selector = new FlagSelector ();
        selector.Styles = SelectorStyles.None; // No ShowNoneFlag
        selector.Values = [1, 2];
        selector.Labels = ["Flag1", "Flag2"];

        CheckBox? noneCheckBox = selector.SubViews.OfType<CheckBox> ().FirstOrDefault (cb => cb.Title == "None");
        Assert.Null (noneCheckBox);
        Assert.Equal (2, selector.SubViews.OfType<CheckBox> ().Count ());
    }

    [Fact]
    public void NoneFlag_AlreadyInValues_IsNotDuplicated ()
    {
        var selector = new FlagSelector ();
        selector.Styles = SelectorStyles.ShowNoneFlag;
        selector.Values = [0, 1, 2]; // 0 already included
        selector.Labels = ["None", "Flag1", "Flag2"];

        // Should only have one "None" checkbox
        Assert.Equal (1, selector.SubViews.OfType<CheckBox> ().Count (cb => selector.GetCheckBoxValue (cb) == 0));
    }

    [Fact]
    public void Mouse_DoubleClick_TogglesAndAccepts ()
    {      
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        var selector = new FlagSelector { DoubleClickAccepts = true };
        selector.Values = [1, 2];
        selector.Labels = ["Flag1", "Flag2"];

        (runnable as View)?.Add (selector);
        app.Begin (runnable);

        var acceptCount = 0;
        selector.Accepting += (s, e) => acceptCount++;

        var valueChangedCount = 0;
        selector.ValueChanged += (s, e) => valueChangedCount++;

        CheckBox checkBox = selector.SubViews.OfType<CheckBox> ().First ();

        // When Values is set, SelectorBase.Value defaults to the first value (1 in this case)
        // So the first checkbox (Data == 1) will be checked
        Assert.Equal (CheckState.Checked, checkBox.Value); // FIXED: Was UnChecked
        Assert.Equal (1, selector.Value); // Verify Value is set to first value

        checkBox = selector.SubViews.OfType<CheckBox> ().Last ();
        app.InjectSequence (InputInjectionExtensions.LeftButtonDoubleClick (checkBox.Frame.Location));

        Assert.Equal (1, acceptCount);

        // DoubleClick generates Click (Activate/toggle) + DoubleClick (Accept).
        // The click toggles Data=2 checkbox from UnChecked to Checked, changing Value from 1 to 1|2=3.
        Assert.Equal (1, valueChangedCount);
        Assert.Equal (CheckState.Checked, checkBox.Value);
        Assert.Equal (3, selector.Value);
    }

    [Fact]
    public void UpdateChecked_PreventsConcurrentModification ()
    {
        var selector = new FlagSelector ();
        selector.Values = [1, 2, 4];
        selector.Labels = ["Flag1", "Flag2", "Flag3"];
        selector.Value = 1;

        // This should not cause recursion or errors
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     CheckBox checkBox = selector.SubViews.OfType<CheckBox> ().First (cb => selector.GetCheckBoxValue (cb) == 2);
                                                     checkBox.Value = CheckState.Checked; // This triggers UpdateChecked internally
                                                 });

        Assert.Null (exception);
        Assert.Equal (1 | 2, selector.Value);
    }

    #endregion

    #region FlagSelector<T> Specific Tests

    [Fact]
    public void Generic_Value_SetWithEnum_UpdatesCorrectly ()
    {
        FlagSelector<SelectorStyles> selector = new ();

        selector.Value = SelectorStyles.ShowNoneFlag | SelectorStyles.ShowValue;

        Assert.True ((selector.Value & SelectorStyles.ShowNoneFlag) == SelectorStyles.ShowNoneFlag);
        Assert.True ((selector.Value & SelectorStyles.ShowValue) == SelectorStyles.ShowValue);

        // SelectorStyles.None is 0, so checking (value & 0) == 0 will always be true
        // What we actually want to check is that the value is NOT zero (i.e., not None)
        Assert.True (selector.Value != SelectorStyles.None);
    }

    [Fact]
    public void Generic_AutomaticallyPopulatesFromEnum ()
    {
        FlagSelector<SelectorStyles> selector = new ();

        // Should auto-populate from enum
        Assert.NotNull (selector.Values);
        Assert.NotNull (selector.Labels);
        Assert.Equal (Enum.GetValues<SelectorStyles> ().Length, selector.Values.Count);
    }

    [Fact]
    public void Null_Value_Generic_UnchecksAll ()
    {
        FlagSelector<SelectorStyles> selector = new ();
        selector.Value = SelectorStyles.ShowNoneFlag;

        selector.Value = null;

        Assert.Null (selector.Value);
        Assert.True (selector.SubViews.OfType<CheckBox> ().All (cb => cb.Value == CheckState.UnChecked));
    }

    [Fact]
    public void Generic_AllFlagsValue_ChecksAll ()
    {
        FlagSelector<SelectorStyles> selector = new ();

        selector.Value = SelectorStyles.All;

        // All non-None flags should be checked
        Assert.True (selector.SubViews.OfType<CheckBox> ().Where (cb => selector.GetCheckBoxValue (cb) != 0).All (cb => cb.Value == CheckState.Checked));
    }

    #endregion

    #region Value Propagation via Activated Event (CWP Bug Tests)

    // Claude - Sonnet 4.6
    /// <summary>
    ///     Proves the CWP ordering bug: <see cref="FlagSelector.OnActivated"/> calls
    ///     <c>base.OnActivated</c> (which fires <see cref="View.Activated"/>) BEFORE the bitmask
    ///     in <see cref="SelectorBase.Value"/> is updated.
    ///     The <see cref="ICommandContext.Value"/> must carry the <see cref="FlagSelector"/>'s
    ///     semantic bitmask <see cref="int"/>?, not the dispatched <see cref="CheckBox"/>'s
    ///     <see cref="CheckState"/>.
    /// </summary>
    [Fact]
    public void Activated_Event_Value_Is_FlagSelector_Bitmask_Not_CheckState ()
    {
        // Arrange: FlagSelector with three bit flags; initial Value = 1 (only Bit1 set).
        FlagSelector flagSelector = new ();
        flagSelector.Values = [1, 2, 4];
        flagSelector.Labels = ["Bit1", "Bit2", "Bit4"];
        flagSelector.SetFocus ();
        flagSelector.Layout ();

        Assert.Equal (1, flagSelector.Value); // Only Bit1 set

        // Focus Bit2 (value=2, currently UnChecked) so Activate dispatches to it.
        CheckBox bit2 = flagSelector.SubViews.OfType<CheckBox> ().First (cb => flagSelector.GetCheckBoxValue (cb) == 2);
        bit2.SetFocus ();

        object? capturedValue = null;
        int activatedCount = 0;

        flagSelector.Activated += (_, args) =>
        {
            activatedCount++;
            capturedValue = args.Value?.Value;
        };

        // Act: Activate on the selector dispatches down to focused Bit2, toggling it Checked.
        flagSelector.InvokeCommand (Command.Activate);

        // Assert: FlagSelector.Value is now 1|2 = 3 (Bit1 and Bit2 both set).
        Assert.Equal (3, flagSelector.Value);
        Assert.Equal (1, activatedCount);

        // The value arriving at Activated must be the FlagSelector's bitmask int? (3),
        // NOT the dispatched CheckBox's CheckState (CheckState.Checked).
        Assert.Null (capturedValue as CheckState?);    // Must NOT be a CheckState
        Assert.Equal ((int?)3, capturedValue as int?); // Must be the FlagSelector's bitmask

        flagSelector.Dispose ();
    }

    // Claude - Sonnet 4.6
    /// <summary>
    ///     Proves that an ancestor subscribing to <see cref="View.Activated"/> via
    ///     <see cref="View.CommandsToBubbleUp"/> receives the <see cref="FlagSelector"/>'s
    ///     bitmask <see cref="int"/>? — not the inner <see cref="CheckBox"/>'s
    ///     <see cref="CheckState"/> — through the <c>BubbleActivatedUp</c> path.
    /// </summary>
    [Fact]
    public void Activated_Event_Ancestor_Receives_FlagSelector_Value_Via_BubbleActivatedUp ()
    {
        // Arrange: Application context ensures SetFocus works through nested hierarchy.
        VirtualTimeProvider time = new ();

        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        // ancestor opts in to Activate bubbling and contains the FlagSelector.
        // CanFocus = true is required so the focus chain can propagate through ancestor.
        View ancestor = new () { Id = "ancestor", CanFocus = true };
        ancestor.CommandsToBubbleUp = [Command.Activate];

        FlagSelector flagSelector = new ();
        flagSelector.Values = [1, 2, 4];
        flagSelector.Labels = ["Bit1", "Bit2", "Bit4"];
        ancestor.Add (flagSelector);

        (runnable as View)!.Add (ancestor);
        app.Begin (runnable);

        Assert.Equal (1, flagSelector.Value); // Only Bit1 set

        // Focus Bit2 (value=2, currently UnChecked) so Activate dispatches to it.
        CheckBox bit2 = flagSelector.SubViews.OfType<CheckBox> ().First (cb => flagSelector.GetCheckBoxValue (cb) == 2);
        bit2.HasFocus = true;
        Assert.True (bit2.HasFocus);

        object? capturedValue = null;
        int ancestorActivatedCount = 0;

        ancestor.Activated += (_, args) =>
        {
            ancestorActivatedCount++;
            capturedValue = args.Value?.Value;
        };

        // Act: Activate dispatches to Bit2, then BubbleActivatedUp reaches ancestor.
        flagSelector.InvokeCommand (Command.Activate);

        // Assert: FlagSelector.Value updated; ancestor received Activated exactly once.
        Assert.Equal (3, flagSelector.Value);
        Assert.Equal (1, ancestorActivatedCount);

        // The ancestor must receive the FlagSelector's bitmask int? (3), NOT a CheckState.
        Assert.Null (capturedValue as CheckState?);    // Must NOT be a CheckState
        Assert.Equal ((int?)3, capturedValue as int?); // Must be the FlagSelector's bitmask

        ancestor.Dispose ();
    }

    #endregion
}
