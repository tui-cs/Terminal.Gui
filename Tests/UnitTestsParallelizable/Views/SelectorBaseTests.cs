namespace ViewsTests;

/// <summary>
///     Tests for <see cref="SelectorBase"/> functionality that applies to all selector implementations.
///     These tests use <see cref="OptionSelector"/> as a concrete implementation to test the base class.
/// </summary>
public class SelectorBaseTests
{
    #region Initialization Tests

    [Fact]
    public void Constructor_SetsDefaults ()
    {
        OptionSelector selector = new ();

        Assert.True (selector.CanFocus);
        Assert.Equal (Dim.Auto (DimAutoStyle.Content), selector.Width);
        Assert.Equal (Dim.Auto (DimAutoStyle.Content), selector.Height);
        Assert.Equal (Orientation.Vertical, selector.Orientation);
        Assert.Null (selector.Labels);
        Assert.Null (selector.Values);
        Assert.Null (selector.AssignHotKeys);
        Assert.Empty (selector.UsedHotKeys);
        Assert.Equal (SelectorStyles.None, selector.Styles);
        Assert.True (selector.DoubleClickAccepts);
        Assert.Equal (2, selector.HorizontalSpace);
    }

    #endregion

    #region Value Property Tests

    [Fact]
    public void Value_Set_ValidValue_UpdatesValue ()
    {
        OptionSelector selector = new ();
        selector.Labels = ["Option1", "Option2"];

        selector.Value = 1;

        Assert.Equal (1, selector.Value);
    }

    [Fact]
    public void Value_Set_InvalidValue_ThrowsArgumentOutOfRangeException ()
    {
        OptionSelector selector = new ();
        selector.Labels = ["Option1", "Option2"];

        Assert.Throws<ArgumentOutOfRangeException> (() => selector.Value = 5);
        Assert.Throws<ArgumentOutOfRangeException> (() => selector.Value = -1);
    }

    [Fact]
    public void Value_Set_Null_Succeeds ()
    {
        OptionSelector selector = new ();
        selector.Labels = ["Option1", "Option2"];

        selector.Value = null;

        Assert.Null (selector.Value);
    }

    [Fact]
    public void Value_Set_SameValue_DoesNotRaiseEvent ()
    {
        OptionSelector selector = new ();
        selector.Labels = ["Option1", "Option2"];
        selector.Value = 1;

        var eventRaisedCount = 0;
        selector.ValueChanged += (_, _) => eventRaisedCount++;

        selector.Value = 1; // Set to same value

        Assert.Equal (0, eventRaisedCount);
    }

    [Fact]
    public void Value_Changed_RaisesValueChangedEvent ()
    {
        OptionSelector selector = new ();
        selector.Labels = ["Option1", "Option2"];

        int? capturedValue = null;
        selector.ValueChanged += (_, e) => capturedValue = e.NewValue;

        selector.Value = 1;

        Assert.Equal (1, capturedValue);
    }

    #endregion

    #region Values Property Tests

    [Fact]
    public void Values_Get_WhenNull_ReturnsSequentialValues ()
    {
        OptionSelector selector = new ();
        selector.Labels = ["Option1", "Option2", "Option3"];

        IReadOnlyList<int>? values = selector.Values;

        Assert.NotNull (values);
        Assert.Equal (3, values.Count);
        Assert.Equal ([0, 1, 2], values);
    }

    [Fact]
    public void Values_Set_UpdatesValues ()
    {
        OptionSelector selector = new ();
        selector.Labels = ["Option1", "Option2"];

        selector.Values = [10, 20];

        Assert.Equal ([10, 20], selector.Values);
    }

    [Fact]
    public void Values_Set_SetsDefaultValue ()
    {
        OptionSelector selector = new ();
        selector.Value = null;
        selector.Labels = ["Option1", "Option2"];

        selector.Values = [10, 20];

        Assert.Equal (10, selector.Value); // Should default to first value
    }

    #endregion

    #region Labels Property Tests

    [Fact]
    public void Labels_Set_CreatesSubViews ()
    {
        OptionSelector selector = new ();

        selector.Labels = ["Option1", "Option2"];

        Assert.Equal (2, selector.SubViews.OfType<CheckBox> ().Count ());
    }

    [Fact]
    public void Labels_Set_Null_RemovesSubViews ()
    {
        OptionSelector selector = new ();
        selector.Labels = ["Option1", "Option2"];

        selector.Labels = null;

        Assert.Empty (selector.SubViews.OfType<CheckBox> ());
    }

    [Fact]
    public void Labels_Values_CountMismatch_DoesNotCreateSubViews ()
    {
        OptionSelector selector = new ();

        selector.Values = [0, 1, 2];
        selector.Labels = ["Option1", "Option2"]; // Mismatch

        Assert.Empty (selector.SubViews.OfType<CheckBox> ());
    }

    #endregion

    #region SetValuesAndLabels Tests

    [Fact]
    public void SetValuesAndLabels_FromEnum_SetsValuesAndLabels ()
    {
        OptionSelector selector = new ();

        selector.SetValuesAndLabels<SelectorStyles> ();

        Assert.NotNull (selector.Values);
        Assert.NotNull (selector.Labels);
        Assert.Equal (Enum.GetValues<SelectorStyles> ().Length, selector.Values.Count);
        Assert.Equal (Enum.GetNames<SelectorStyles> (), selector.Labels);
    }

    [Fact]
    public void SetValuesAndLabels_SetsCorrectIntegerValues ()
    {
        OptionSelector selector = new ();

        selector.SetValuesAndLabels<SelectorStyles> ();

        // Verify values match enum integer values
        List<int> expectedValues = Enum.GetValues<SelectorStyles> ().Select (e => (int)e).ToList ();
        Assert.Equal (expectedValues, selector.Values);
    }

    #endregion

    #region Styles Property Tests

    [Fact]
    public void Styles_Set_None_NoExtraSubViews ()
    {
        OptionSelector selector = new ();
        selector.Labels = ["Option1", "Option2"];

        selector.Styles = SelectorStyles.None;

        Assert.Equal (2, selector.SubViews.Count);
        Assert.Null (selector.SubViews.FirstOrDefault (v => v.Id == "valueField"));
    }

    [Fact]
    public void Styles_Set_ShowValue_AddsValueField ()
    {
        OptionSelector selector = new ();
        selector.Labels = ["Option1", "Option2"];

        selector.Styles = SelectorStyles.ShowValue;

        View? valueField = selector.SubViews.FirstOrDefault (v => v.Id == "valueField");
        Assert.NotNull (valueField);
        Assert.IsType<TextField> (valueField);
    }

    [Fact]
    public void Styles_Set_ShowValue_ValueFieldDisplaysCurrentValue ()
    {
        OptionSelector selector = new ();
        selector.Labels = ["Option1", "Option2"];
        selector.Value = 1;

        selector.Styles = SelectorStyles.ShowValue;

        var valueField = (TextField?)selector.SubViews.FirstOrDefault (v => v.Id == "valueField");
        Assert.NotNull (valueField);
        Assert.Equal ("1", valueField.Text);
    }

    [Fact]
    public void Styles_Set_ShowValue_ValueFieldUpdatesOnValueChange ()
    {
        OptionSelector selector = new ();
        selector.Labels = ["Option1", "Option2"];
        selector.Styles = SelectorStyles.ShowValue;

        selector.Value = 1;

        var valueField = (TextField?)selector.SubViews.FirstOrDefault (v => v.Id == "valueField");
        Assert.NotNull (valueField);
        Assert.Equal ("1", valueField.Text);
    }

    [Fact]
    public void Styles_Set_SameValue_DoesNotRecreateSubViews ()
    {
        OptionSelector selector = new ();
        selector.Labels = ["Option1", "Option2"];
        selector.Styles = SelectorStyles.ShowValue;

        CheckBox firstCheckBox = selector.SubViews.OfType<CheckBox> ().First ();

        selector.Styles = SelectorStyles.ShowValue; // Set to same value

        // Should be the same instance
        Assert.Same (firstCheckBox, selector.SubViews.OfType<CheckBox> ().First ());
    }

    #endregion

    #region AssignHotKeys Tests

    [Fact]
    public void AssignHotKeys_True_AssignsHotKeysToCheckBoxes ()
    {
        var selector = new OptionSelector { AssignHotKeys = true };

        selector.Labels = ["Option1", "Option2"];

        CheckBox [] checkBoxes = selector.SubViews.OfType<CheckBox> ().ToArray ();
        Assert.NotEqual (Key.Empty, checkBoxes [0].HotKey);
        Assert.NotEqual (Key.Empty, checkBoxes [1].HotKey);
    }

    [Fact]
    public void AssignHotKeys_True_AddsHotKeySpecifierToTitles ()
    {
        var selector = new OptionSelector { AssignHotKeys = true };

        selector.Labels = ["Option1", "Option2"];

        CheckBox [] checkBoxes = selector.SubViews.OfType<CheckBox> ().ToArray ();
        Assert.Contains ('_', checkBoxes [0].Title);
        Assert.Contains ('_', checkBoxes [1].Title);
    }

    [Fact]
    public void AssignHotKeys_True_AssignsUniqueHotKeys ()
    {
        var selector = new OptionSelector { AssignHotKeys = true };

        selector.Labels = ["Option1", "Option2", "Option3"];

        CheckBox [] checkBoxes = selector.SubViews.OfType<CheckBox> ().ToArray ();
        List<Key> hotKeys = checkBoxes.Select (cb => cb.HotKey).ToList ();
        Assert.Equal (3, hotKeys.Distinct ().Count ()); // All unique
    }

    [Fact]
    public void AssignHotKeys_False_DoesNotAssignHotKeys ()
    {
        var selector = new OptionSelector { AssignHotKeys = false };

        selector.Labels = ["Option1", "Option2"];

        CheckBox [] checkBoxes = selector.SubViews.OfType<CheckBox> ().ToArray ();
        Assert.Equal (Key.Empty, checkBoxes [0].HotKey);
        Assert.Equal (Key.Empty, checkBoxes [1].HotKey);
    }

    [Fact]
    public void AssignHotKeys_PreservesExistingHotKeys ()
    {
        var selector = new OptionSelector { AssignHotKeys = true };

        selector.Labels = ["_Alt Option", "Option2"];

        CheckBox [] checkBoxes = selector.SubViews.OfType<CheckBox> ().ToArray ();

        // Should use 'A' from "_Alt"
        Assert.Equal (Key.A, checkBoxes [0].HotKey);
    }

    #endregion

    #region UsedHotKeys Tests

    [Fact]
    public void UsedHotKeys_SkipsMarkedKeys ()
    {
        var selector = new OptionSelector { AssignHotKeys = true };

        selector.UsedHotKeys.Add (Key.O); // Mark 'O' as used
        selector.Labels = ["Option1", "Option2"];

        CheckBox [] checkBoxes = selector.SubViews.OfType<CheckBox> ().ToArray ();

        // Should skip 'O' and use next available character
        Assert.NotEqual (Key.O, checkBoxes [0].HotKey);
    }

    [Fact]
    public void UsedHotKeys_PopulatedWhenHotKeysAssigned ()
    {
        var selector = new OptionSelector { AssignHotKeys = true };

        selector.Labels = ["Option1", "Option2"];

        // UsedHotKeys should contain the assigned hotkeys
        Assert.NotEmpty (selector.UsedHotKeys);
        CheckBox [] checkBoxes = selector.SubViews.OfType<CheckBox> ().ToArray ();

        foreach (CheckBox cb in checkBoxes)
        {
            Assert.Contains (cb.HotKey, selector.UsedHotKeys);
        }
    }

    #endregion

    #region Orientation Tests

    [Fact]
    public void Orientation_Vertical_CheckBoxesStackedVertically ()
    {
        OptionSelector selector = new ();
        selector.Labels = ["Option1", "Option2"];
        selector.Orientation = Orientation.Vertical;
        selector.Layout ();

        CheckBox [] checkBoxes = selector.SubViews.OfType<CheckBox> ().ToArray ();
        Assert.Equal (0, checkBoxes [0].Frame.Y);
        Assert.True (checkBoxes [1].Frame.Y > checkBoxes [0].Frame.Y);
    }

    [Fact]
    public void Orientation_Horizontal_CheckBoxesArrangedHorizontally ()
    {
        OptionSelector selector = new ();
        selector.Labels = ["Option1", "Option2"];
        selector.Orientation = Orientation.Horizontal;
        selector.Layout ();

        CheckBox [] checkBoxes = selector.SubViews.OfType<CheckBox> ().ToArray ();
        Assert.Equal (0, checkBoxes [0].Frame.Y);
        Assert.Equal (0, checkBoxes [1].Frame.Y);
        Assert.True (checkBoxes [1].Frame.X > checkBoxes [0].Frame.X);
    }

    [Fact]
    public void Orientation_Change_TriggersLayout ()
    {
        OptionSelector selector = new ();
        selector.Labels = ["Option1", "Option2"];
        selector.Layout ();

        CheckBox [] checkBoxes = selector.SubViews.OfType<CheckBox> ().ToArray ();
        int originalYDiff = checkBoxes [1].Frame.Y - checkBoxes [0].Frame.Y;

        selector.Orientation = Orientation.Horizontal;
        selector.Layout ();

        int newYDiff = checkBoxes [1].Frame.Y - checkBoxes [0].Frame.Y;
        Assert.NotEqual (originalYDiff, newYDiff);
        Assert.Equal (0, newYDiff); // Both should be at Y=0 now
    }

    // Copilot
    [Fact]
    public void Horizontal_Labels_SetAfterConstruction_SelectorWidthAccommodatesAllCheckBoxes ()
    {
        // Arrange: create selector with horizontal orientation, no labels yet
        OptionSelector selector = new () { Orientation = Orientation.Horizontal };

        // Act: set labels/values after construction (simulates reactive-binding scenario)
        selector.Labels = ["IMDb (Recommended)", "TMDb"];
        selector.Values = [0, 1];
        selector.Layout ();

        // Assert: selector width must be wide enough to fully contain every checkbox
        CheckBox [] checkBoxes = selector.SubViews.OfType<CheckBox> ().ToArray ();
        Assert.Equal (2, checkBoxes.Length);

        foreach (CheckBox cb in checkBoxes)
        {
            Assert.True (
                          cb.Frame.X + cb.Frame.Width <= selector.Frame.Width,
                          $"CheckBox '{cb.Title}' right edge ({cb.Frame.X + cb.Frame.Width}) exceeds selector width ({selector.Frame.Width})"
                         );
        }
    }

    #endregion

    #region HorizontalSpace Tests

    [Fact]
    public void HorizontalSpace_Default_Is2 ()
    {
        OptionSelector selector = new ();

        Assert.Equal (2, selector.HorizontalSpace);
    }

    [Fact]
    public void HorizontalSpace_Set_UpdatesSpacing ()
    {
        var selector = new OptionSelector { Orientation = Orientation.Horizontal };
        selector.Labels = ["Option1", "Option2"];
        selector.HorizontalSpace = 2;
        selector.Layout ();

        CheckBox [] checkBoxes = selector.SubViews.OfType<CheckBox> ().ToArray ();

        // HorizontalSpace is applied via Margin.Thickness.Right
        int spacing2 = checkBoxes [0].Margin.Thickness.Right;

        selector.HorizontalSpace = 5;
        selector.Layout ();

        int spacing5 = checkBoxes [0].Margin.Thickness.Right;
        Assert.True (spacing5 > spacing2);
        Assert.Equal (2, spacing2);
        Assert.Equal (5, spacing5);
    }

    [Fact]
    public void HorizontalSpace_OnlyAppliesToHorizontalOrientation ()
    {
        var selector = new OptionSelector { Orientation = Orientation.Vertical };
        selector.Labels = ["Option1", "Option2"];
        selector.HorizontalSpace = 10;
        selector.Layout ();

        CheckBox [] checkBoxes = selector.SubViews.OfType<CheckBox> ().ToArray ();

        // In vertical mode, checkboxes should be at same X
        Assert.Equal (checkBoxes [0].Frame.X, checkBoxes [1].Frame.X);
    }

    #endregion

    #region DoubleClickAccepts Tests

    [Fact]
    public void DoubleClickAccepts_Default_IsTrue ()
    {
        OptionSelector selector = new ();

        Assert.True (selector.DoubleClickAccepts);
    }

    [Fact]
    public void DoubleClickAccepts_True_AcceptOnDoubleClick ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        var selector = new OptionSelector { DoubleClickAccepts = true };
        selector.Labels = ["Option1", "Option2"];

        (runnable as View)?.Add (selector);
        app.Begin (runnable);

        var acceptCount = 0;
        selector.Accepting += (_, _) => acceptCount++;

        CheckBox checkBox = selector.SubViews.OfType<CheckBox> ().First ();
        app.InjectSequence (InputInjectionExtensions.LeftButtonDoubleClick (checkBox.Frame.Location));

        Assert.Equal (1, acceptCount);
        Assert.Equal (0, selector.Value); // Should select the first option on double-click

        checkBox = selector.SubViews.OfType<CheckBox> ().Last ();
        app.InjectSequence (InputInjectionExtensions.LeftButtonDoubleClick (checkBox.Frame.Location));

        Assert.Equal (2, acceptCount);
        Assert.Equal (1, selector.Value); // Should select the 2nd option on double-click
    }

    [Fact]
    public void DoubleClickAccepts_False_DoesNotAcceptOnDoubleClick ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        var selector = new OptionSelector { DoubleClickAccepts = false };
        selector.Labels = ["Option1", "Option2"];

        (runnable as View)?.Add (selector);
        app.Begin (runnable);

        var acceptCount = 0;
        selector.Accepting += (_, _) => acceptCount++;

        CheckBox checkBox = selector.SubViews.OfType<CheckBox> ().First ();
        app.InjectSequence (InputInjectionExtensions.LeftButtonDoubleClick (checkBox.Frame.Location));

        Assert.Equal (0, acceptCount);
        Assert.Equal (0, selector.Value);

        checkBox = selector.SubViews.OfType<CheckBox> ().Last ();
        app.InjectSequence (InputInjectionExtensions.LeftButtonDoubleClick (checkBox.Frame.Location));

        Assert.Equal (0, acceptCount);
        Assert.Equal (1, selector.Value); // Should select the second option on double-click
    }

    #endregion

    #region CreateSubViews Tests

    [Fact]
    public void CreateSubViews_RemovesOldSubViewsAndCreatesNew ()
    {
        OptionSelector selector = new ();
        selector.Labels = ["Option1", "Option2"];

        int oldCount = selector.SubViews.Count;

        selector.Labels = ["New1", "New2", "New3"];

        Assert.NotEqual (oldCount, selector.SubViews.Count);
        Assert.Equal (3, selector.SubViews.OfType<CheckBox> ().Count ());
        Assert.Contains (selector.SubViews.OfType<CheckBox> (), cb => cb.Title == "New1");
    }

    [Fact]
    public void CreateSubViews_SetsCheckBoxProperties ()
    {
        OptionSelector selector = new ();

        selector.Labels = ["Test Option"];
        selector.Values = [42];

        CheckBox checkBox = selector.SubViews.OfType<CheckBox> ().First ();
        Assert.Equal ("Test Option", checkBox.Title);
        Assert.Equal ("Test Option", checkBox.Id);
        Assert.Equal (42, selector.GetCheckBoxValue (checkBox));
        Assert.True (checkBox.CanFocus);
    }

    [Fact]
    public void CreateSubViews_ResetsValue_WhenCurrentValueNotInNewValues ()
    {
        OptionSelector selector = new ();

        selector.Labels = ["Option1", "Option2"];
        selector.Values = [10, 20];

        // Verify initial value is set to first value (10)
        Assert.Equal (10, selector.Value);

        // Change to new labels/values where current value (10) is not present
        selector.Labels = ["New1", "New2"];
        selector.Values = [30, 40];

        // Value should reset to first value (30)
        Assert.Equal (30, selector.Value);
    }

    [Fact]
    public void Setting_TabBehavior_AfterSubViewsAreCreated_DoesNotLoseCheckedState ()
    {
        OptionSelector selector = new ()
        {
            Labels = ["Option1", "Option2"], Values = [10, 20], Orientation = Orientation.Vertical, TabBehavior = TabBehavior.NoStop
        };

        List<CheckBox> checkBoxes = [.. selector.SubViews.OfType<CheckBox> ()];

        Assert.Equal (2, checkBoxes.Count);
        Assert.Equal (CheckState.Checked, checkBoxes [0].Value);
        Assert.Equal (CheckState.UnChecked, checkBoxes [1].Value);
        Assert.Equal (10, selector.Value);
        Assert.Equal (TabBehavior.NoStop, selector.TabBehavior);
    }

    [Fact]
    public void Setting_TabBehavior_AfterSubViewsAreCreated_DoesNotLoseCheckedState_With_Enum ()
    {
        OptionSelector<Side> selector = new ()
        {
            Orientation = Orientation.Vertical, TabBehavior = TabBehavior.NoStop
        };

        List<CheckBox> checkBoxes = [.. selector.SubViews.OfType<CheckBox> ()];

        Assert.Equal (4, checkBoxes.Count);
        Assert.Equal (CheckState.Checked, checkBoxes [0].Value);
        Assert.Equal (CheckState.UnChecked, checkBoxes [1].Value);
        Assert.Equal (CheckState.UnChecked, checkBoxes [2].Value);
        Assert.Equal (CheckState.UnChecked, checkBoxes [3].Value);
        Assert.Equal (Side.Left, selector.Value);
        Assert.Equal (TabBehavior.NoStop, selector.TabBehavior);
    }

    #endregion

    #region HotKey Command Tests

    [Fact]
    public void HotKey_Command_DoesNotFireAccept ()
    {
        OptionSelector selector = new ();
        selector.Labels = ["Option1", "Option2"];

        var acceptCount = 0;
        selector.Accepting += (_, _) => acceptCount++;

        selector.InvokeCommand (Command.HotKey);

        Assert.Equal (0, acceptCount);
    }

    [Fact]
    public void Accept_Command_FiresAccept ()
    {
        OptionSelector selector = new ();
        selector.Labels = ["Option1", "Option2"];

        var acceptCount = 0;
        selector.Accepting += (_, _) => acceptCount++;

        selector.InvokeCommand (Command.Accept);

        Assert.Equal (1, acceptCount);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void EmptyLabels_CreatesNoSubViews ()
    {
        OptionSelector selector = new ();

        selector.Labels = [];

        Assert.Empty (selector.SubViews);
    }

    [Fact]
    public void Value_WithNoLabels_CanBeSet ()
    {
        OptionSelector selector = new ();

        // This should work even without labels
        Exception? exception = Record.Exception (() => selector.Value = null);

        Assert.Null (exception);
        Assert.Null (selector.Value);
    }

    #endregion

    #region Navigation Keys

    [Theory]
    [InlineData (SelectorStyles.None)]
    [InlineData (SelectorStyles.ShowNoneFlag)]
    [InlineData (SelectorStyles.ShowAllFlag)]
    [InlineData (SelectorStyles.ShowValue)]
    [InlineData (SelectorStyles.All)]
    public void Navigation_Keys_Move_Out_And_Into_Not_Within (SelectorStyles selectorStyles)
    {
        using IApplication app = Application.Create ().Init (DriverRegistry.Names.ANSI);
        using Runnable runnable = new ();
        var view1 = new View { CanFocus = true };

        OptionSelector selector = new ()
        {
            Styles = selectorStyles,
            TabBehavior = TabBehavior.NoStop
        };
        List<string> options = ["Option1", "Option2", "Option3"];
        selector.Labels = options;
        var view2 = new View { CanFocus = true };
        runnable.Add (view1, selector, view2);

        app.Begin (runnable);

        // Set focus to view1
        view1.SetFocus ();

        // Invoke Tab command to move focus to selector
        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.Tab));
        Assert.True (selector.HasFocus);

        // Invoke Tab command again to move focus to view2
        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.Tab));
        Assert.True (view2.HasFocus);

        // Now test Shift+Tab to move focus back to selector
        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.Tab.WithShift));
        Assert.True (selector.HasFocus);

        // Finally, Shift+Tab again to move focus back to view1
        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.Tab.WithShift));
        Assert.True (view1.HasFocus);
    }

    #endregion
}
