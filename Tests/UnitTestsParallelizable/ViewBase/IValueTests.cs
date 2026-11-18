// Claude - Opus 4.5

namespace ViewBaseTests;

/// <summary>
///     Unit tests for the <see cref="IValue{TValue}"/> interface using test View subclasses.
/// </summary>
public class IValueTests
{
    /// <summary>
    ///     Test view implementing IValue&lt;int?&gt; using CWPPropertyHelper.
    /// </summary>
    private sealed class TestIntValueView : View, IValue<int?>
    {
        private int? _value;

        public int? Value
        {
            get => _value;
            set =>
                CWPPropertyHelper.ChangeProperty (this,
                                                  ref _value,
                                                  value,
                                                  OnValueChanging,
                                                  ValueChanging,
                                                  _ => { }, // No additional work needed for this test view
                                                  OnValueChanged,
                                                  ValueChanged,
                                                  out _);
        }

        public event EventHandler<ValueChangingEventArgs<int?>>? ValueChanging;
        public event EventHandler<ValueChangedEventArgs<int?>>? ValueChanged;
        public event EventHandler<ValueChangedEventArgs<object?>>? ValueChangedUntyped;

        private bool OnValueChanging (ValueChangingEventArgs<int?> args) => false;
        private void OnValueChanged (ValueChangedEventArgs<int?> args) => ValueChangedUntyped?.Invoke (this, new ValueChangedEventArgs<object?> (args.OldValue, args.NewValue));
    }

    /// <summary>
    ///     Test view implementing IValue&lt;string?&gt; using CWPPropertyHelper.
    /// </summary>
    private class TestStringValueView : View, IValue<string?>
    {
        private string? _value;

        public string? Value
        {
            get => _value;
            set =>
                CWPPropertyHelper.ChangeProperty (this,
                                                  ref _value,
                                                  value,
                                                  OnValueChanging,
                                                  ValueChanging,
                                                  _ => { },
                                                  OnValueChanged,
                                                  ValueChanged,
                                                  out _);
        }

        public event EventHandler<ValueChangingEventArgs<string?>>? ValueChanging;
        public event EventHandler<ValueChangedEventArgs<string?>>? ValueChanged;
        public event EventHandler<ValueChangedEventArgs<object?>>? ValueChangedUntyped;

        protected virtual bool OnValueChanging (ValueChangingEventArgs<string?> args) => false;

        protected virtual void OnValueChanged (ValueChangedEventArgs<string?> args) =>
            ValueChangedUntyped?.Invoke (this, new ValueChangedEventArgs<object?> (args.OldValue, args.NewValue));
    }

    [Fact]
    public void Value_InitiallyNull ()
    {
        TestIntValueView view = new ();
        Assert.Null (view.Value);
    }

    [Fact]
    public void Value_CanBeSet ()
    {
        TestIntValueView view = new ();
        view.Value = 42;
        Assert.Equal (42, view.Value);
    }

    [Fact]
    public void ValueChanged_Fires_WhenValueChanges ()
    {
        TestIntValueView view = new ();
        int? newValue = null;
        var count = 0;

        view.ValueChanged += (_, e) =>
                             {
                                 count++;
                                 newValue = e.NewValue;
                             };

        view.Value = 42;

        Assert.Equal (1, count);
        Assert.Equal (42, newValue);
    }

    [Fact]
    public void ValueChanged_DoesNotFire_WhenValueSame ()
    {
        TestIntValueView view = new ();
        view.Value = 42;
        var count = 0;

        view.ValueChanged += (_, _) => count++;

        view.Value = 42; // Same value

        Assert.Equal (0, count);
    }

    [Fact]
    public void ValueChanging_CanCancel ()
    {
        TestIntValueView view = new ();
        view.Value = 10;

        view.ValueChanging += (_, e) =>
                              {
                                  e.Handled = true; // Cancel the change
                              };

        view.Value = 42; // Should be cancelled

        Assert.Equal (10, view.Value); // Value unchanged
    }

    [Fact]
    public void ValueChanging_Fires_BeforeValueChanged ()
    {
        TestIntValueView view = new ();
        List<string> events = [];

        view.ValueChanging += (_, _) => events.Add ("changing");
        view.ValueChanged += (_, _) => events.Add ("changed");

        view.Value = 42;

        Assert.Equal (["changing", "changed"], events);
    }

    [Fact]
    public void ValueChanging_ReceivesCurrentAndNewValue ()
    {
        TestIntValueView view = new ();
        view.Value = 10;

        int? receivedCurrent = null;
        int? receivedNew = null;

        view.ValueChanging += (_, e) =>
                              {
                                  receivedCurrent = e.CurrentValue;
                                  receivedNew = e.NewValue;
                              };

        view.Value = 42;

        Assert.Equal (10, receivedCurrent);
        Assert.Equal (42, receivedNew);
    }

    [Fact]
    public void ValueChanged_ReceivesOldAndNewValue ()
    {
        TestIntValueView view = new ();
        view.Value = 10;

        int? receivedOld = null;
        int? receivedNew = null;

        view.ValueChanged += (_, e) =>
                             {
                                 receivedOld = e.OldValue;
                                 receivedNew = e.NewValue;
                             };

        view.Value = 42;

        Assert.Equal (10, receivedOld);
        Assert.Equal (42, receivedNew);
    }

    [Fact]
    public void StringValue_Works ()
    {
        TestStringValueView view = new ();

        view.Value = "Hello";
        Assert.Equal ("Hello", view.Value);

        view.Value = "World";
        Assert.Equal ("World", view.Value);
    }

    [Fact]
    public void Value_CanBeSetToNull ()
    {
        TestIntValueView view = new ();
        view.Value = 42;

        view.Value = null;

        Assert.Null (view.Value);
    }

    // Tests for non-generic IValue interface

    [Fact]
    public void GetValue_ReturnsBoxedValue_ForInt ()
    {
        TestIntValueView view = new ();
        view.Value = 42;

        IValue nonGeneric = view;
        object? result = nonGeneric.GetValue ();

        Assert.NotNull (result);
        Assert.IsType<int> (result);
        Assert.Equal (42, result);
    }

    [Fact]
    public void GetValue_ReturnsNull_WhenValueIsNull ()
    {
        TestIntValueView view = new ();
        view.Value = null;

        IValue nonGeneric = view;
        object? result = nonGeneric.GetValue ();

        Assert.Null (result);
    }

    [Fact]
    public void GetValue_ReturnsBoxedValue_ForString ()
    {
        TestStringValueView view = new ();
        view.Value = "Hello World";

        IValue nonGeneric = view;
        object? result = nonGeneric.GetValue ();

        Assert.NotNull (result);
        Assert.IsType<string> (result);
        Assert.Equal ("Hello World", result);
    }

    [Fact]
    public void GetValue_ReturnsUpdatedValue_AfterValueChange ()
    {
        TestIntValueView view = new ();
        view.Value = 10;

        IValue nonGeneric = view;
        Assert.Equal (10, nonGeneric.GetValue ());

        view.Value = 99;
        Assert.Equal (99, nonGeneric.GetValue ());
    }

    [Fact]
    public void IValue_CanBeUsedPolymorphically ()
    {
        // Demonstrates that different IValue<T> implementations can be used through IValue
        TestIntValueView intView = new () { Value = 42 };
        TestStringValueView stringView = new () { Value = "test" };

        List<IValue> values = [intView, stringView];

        Assert.Equal (42, values [0].GetValue ());
        Assert.Equal ("test", values [1].GetValue ());
    }

    // Tests for concrete views implementing IValue

    [Fact]
    public void CheckBox_GetValue_ReturnsCheckedState ()
    {
        CheckBox checkBox = new () { Value = CheckState.Checked };

        IValue valueProvider = checkBox;
        object? result = valueProvider.GetValue ();

        Assert.NotNull (result);
        Assert.IsType<CheckState> (result);
        Assert.Equal (CheckState.Checked, result);
    }

    [Fact]
    public void CheckBox_GetValue_ReturnsUnCheckedState ()
    {
        CheckBox checkBox = new () { Value = CheckState.UnChecked };

        IValue valueProvider = checkBox;
        object? result = valueProvider.GetValue ();

        Assert.Equal (CheckState.UnChecked, result);
    }

    [Fact]
    public void TextField_GetValue_ReturnsText ()
    {
        TextField textField = new () { Text = "Hello World" };

        IValue valueProvider = textField;
        object? result = valueProvider.GetValue ();

        Assert.NotNull (result);
        Assert.IsType<string> (result);
        Assert.Equal ("Hello World", result);
    }

    [Fact]
    public void TextField_GetValue_ReturnsEmptyString_WhenEmpty ()
    {
        TextField textField = new ();

        IValue valueProvider = textField;
        object? result = valueProvider.GetValue ();

        Assert.NotNull (result);
        Assert.Equal ("", result);
    }

    [Fact]
    public void OptionSelector_GetValue_ReturnsSelectedValue ()
    {
        OptionSelector optionSelector = new () { Labels = ["Option 1", "Option 2", "Option 3"], Value = 1 };

        IValue valueProvider = optionSelector;
        object? result = valueProvider.GetValue ();

        Assert.NotNull (result);
        Assert.IsType<int> (result);
        Assert.Equal (1, result);
    }

    [Fact]
    public void OptionSelectorT_GetValue_ReturnsTypedValue ()
    {
        OptionSelector<Alignment> optionSelector = new () { Value = Alignment.Center };

        IValue valueProvider = optionSelector;
        object? result = valueProvider.GetValue ();

        Assert.NotNull (result);
        Assert.IsType<Alignment> (result);
        Assert.Equal (Alignment.Center, result);
    }

    [Fact]
    public void FlagSelectorT_GetValue_ReturnsTypedValue ()
    {
        FlagSelector<AlignmentModes> flagSelector = new () { Value = AlignmentModes.StartToEnd | AlignmentModes.AddSpaceBetweenItems };

        IValue valueProvider = flagSelector;
        object? result = valueProvider.GetValue ();

        Assert.NotNull (result);
        Assert.IsType<AlignmentModes> (result);
        Assert.Equal (AlignmentModes.StartToEnd | AlignmentModes.AddSpaceBetweenItems, result);
    }

    [Fact]
    public void NumericUpDown_GetValue_ReturnsValue ()
    {
        NumericUpDown<int> numericUpDown = new () { Value = 42 };

        IValue valueProvider = numericUpDown;
        object? result = valueProvider.GetValue ();

        Assert.NotNull (result);
        Assert.IsType<int> (result);
        Assert.Equal (42, result);
    }

    [Fact]
    public void ColorPicker_GetValue_ReturnsColor ()
    {
        // ColorPicker implements IValue<Color?> with Value property delegating to SelectedColor
        ColorPicker colorPicker = new () { Value = Color.Red };

        IValue valueProvider = colorPicker;
        object? result = valueProvider.GetValue ();

        // The getter returns the SelectedColor which may differ from the set value
        // Just verify we get a Color back
        Assert.NotNull (result);
        Assert.IsType<Color> (result);
    }

    [Fact]
    public void SenderOfEvents_IsCorrect ()
    {
        // Verify that the sender parameter of ValueChanging and ValueChanged events is the view raising the event
        TestIntValueView view = new ();
        object? changingSender = null;
        object? changedSender = null;
        view.ValueChanging += (sender, _) => changingSender = sender;
        view.ValueChanged += (sender, _) => changedSender = sender;
        view.Value = 42;

        // Both events should have the view as sender
        Assert.NotNull (changingSender);
        Assert.NotNull (changedSender);
        Assert.Equal (view, changingSender);
        Assert.Equal (view, changedSender);
    }

    #region TrySetValueFromString

    // Copilot
    [Fact]
    public void TrySetValueFromString_Int_ParsesValid ()
    {
        TestIntValueView view = new ();

        bool result = ((IValue)view).TrySetValueFromString ("42");

        Assert.True (result);
        Assert.Equal (42, view.Value);
    }

    // Copilot
    [Fact]
    public void TrySetValueFromString_Int_ReturnsFalse_ForInvalid ()
    {
        TestIntValueView view = new () { Value = 7 };

        bool result = ((IValue)view).TrySetValueFromString ("not a number");

        Assert.False (result);
        Assert.Equal (7, view.Value);
    }

    // Copilot
    [Fact]
    public void TrySetValueFromString_String_AssignsDirectly ()
    {
        TestStringValueView view = new ();

        bool result = ((IValue)view).TrySetValueFromString ("hello");

        Assert.True (result);
        Assert.Equal ("hello", view.Value);
    }

    // Copilot - Test view implementing IValue<DateTime> to verify IParsable parsing.
    private sealed class TestDateValueView : View, IValue<DateTime>
    {
        private DateTime _value;

        public DateTime Value
        {
            get => _value;
            set =>
                CWPPropertyHelper.ChangeProperty (this,
                                                  ref _value,
                                                  value,
                                                  args => false,
                                                  ValueChanging,
                                                  _ => { },
                                                  args => ValueChangedUntyped?.Invoke (this, new ValueChangedEventArgs<object?> (args.OldValue, args.NewValue)),
                                                  ValueChanged,
                                                  out _);
        }

        public event EventHandler<ValueChangingEventArgs<DateTime>>? ValueChanging;
        public event EventHandler<ValueChangedEventArgs<DateTime>>? ValueChanged;
        public event EventHandler<ValueChangedEventArgs<object?>>? ValueChangedUntyped;
    }

    // Copilot
    [Fact]
    public void TrySetValueFromString_DateTime_ParsesViaIParsable ()
    {
        TestDateValueView view = new ();

        bool result = ((IValue)view).TrySetValueFromString ("2024-06-15");

        Assert.True (result);
        Assert.Equal (new DateTime (2024, 6, 15), view.Value);
    }

    // Copilot - Test view implementing IValue<DayOfWeek?> to verify nullable enum parsing.
    private sealed class TestEnumValueView : View, IValue<DayOfWeek?>
    {
        private DayOfWeek? _value;

        public DayOfWeek? Value
        {
            get => _value;
            set =>
                CWPPropertyHelper.ChangeProperty (this,
                                                  ref _value,
                                                  value,
                                                  args => false,
                                                  ValueChanging,
                                                  _ => { },
                                                  args => ValueChangedUntyped?.Invoke (this, new ValueChangedEventArgs<object?> (args.OldValue, args.NewValue)),
                                                  ValueChanged,
                                                  out _);
        }

        public event EventHandler<ValueChangingEventArgs<DayOfWeek?>>? ValueChanging;
        public event EventHandler<ValueChangedEventArgs<DayOfWeek?>>? ValueChanged;
        public event EventHandler<ValueChangedEventArgs<object?>>? ValueChangedUntyped;
    }

    // Copilot
    [Theory]
    [InlineData ("Monday", DayOfWeek.Monday)]
    [InlineData ("friday", DayOfWeek.Friday)] // case-insensitive
    public void TrySetValueFromString_NullableEnum_Parses (string input, DayOfWeek expected)
    {
        TestEnumValueView view = new ();

        bool result = ((IValue)view).TrySetValueFromString (input);

        Assert.True (result);
        Assert.Equal (expected, view.Value);
    }

    // Copilot
    [Fact]
    public void TrySetValueFromString_Enum_ReturnsFalse_ForInvalid ()
    {
        TestEnumValueView view = new () { Value = DayOfWeek.Sunday };

        bool result = ((IValue)view).TrySetValueFromString ("NotADay");

        Assert.False (result);
        Assert.Equal (DayOfWeek.Sunday, view.Value);
    }

    // Copilot - Test view implementing IValue<object?> to verify unsupported type handling.
    private sealed class TestObjectValueView : View, IValue<object?>
    {
        private object? _value;

        public object? Value
        {
            get => _value;
            set =>
                CWPPropertyHelper.ChangeProperty (this,
                                                  ref _value,
                                                  value,
                                                  args => false,
                                                  ValueChanging,
                                                  _ => { },
                                                  args => ValueChangedUntyped?.Invoke (this, new ValueChangedEventArgs<object?> (args.OldValue, args.NewValue)),
                                                  ValueChanged,
                                                  out _);
        }

        public event EventHandler<ValueChangingEventArgs<object?>>? ValueChanging;
        public event EventHandler<ValueChangedEventArgs<object?>>? ValueChanged;
        public event EventHandler<ValueChangedEventArgs<object?>>? ValueChangedUntyped;
    }

    // Copilot
    [Fact]
    public void TrySetValueFromString_UnsupportedType_ReturnsFalse ()
    {
        TestObjectValueView view = new ();

        bool result = ((IValue)view).TrySetValueFromString ("anything");

        Assert.False (result);
        Assert.Null (view.Value);
    }

    // Copilot
    [Fact]
    public void TrySetValueFromString_RaisesValueChanged ()
    {
        TestIntValueView view = new ();
        var raised = 0;
        view.ValueChanged += (_, _) => raised++;

        bool result = ((IValue)view).TrySetValueFromString ("99");

        Assert.True (result);
        Assert.Equal (1, raised);
        Assert.Equal (99, view.Value);
    }

    // Copilot - Verifies TrySetValueFromString integration with real Views.
    [Fact]
    public void TrySetValueFromString_NumericUpDown_Int_Parses ()
    {
        NumericUpDown<int> upDown = new ();

        bool result = ((IValue)upDown).TrySetValueFromString ("123");

        Assert.True (result);
        Assert.Equal (123, upDown.Value);
    }

    // Copilot
    [Fact]
    public void TrySetValueFromString_ColorPicker_ParsesHexColor ()
    {
        ColorPicker picker = new ();

        bool result = ((IValue)picker).TrySetValueFromString ("#FF0000");

        Assert.True (result);
        Assert.Equal (Color.Red, picker.SelectedColor);
    }

    // Copilot
    [Fact]
    public void TrySetValueFromString_ColorPicker_ReturnsFalse_ForInvalid ()
    {
        ColorPicker picker = new ();

        bool result = ((IValue)picker).TrySetValueFromString ("not-a-color");

        Assert.False (result);
    }

    // Copilot
    [Fact]
    public void TrySetValueFromString_DatePicker_ParsesIsoDate ()
    {
        DatePicker picker = new ();

        bool result = ((IValue)picker).TrySetValueFromString ("2024-12-25");

        Assert.True (result);
        Assert.Equal (new DateTime (2024, 12, 25), picker.Value);
    }

    // Copilot
    [Fact]
    public void TrySetValueFromString_MenuItem_SetsTitle ()
    {
        MenuItem item = new () { Title = "Old" };

        bool result = ((IValue)item).TrySetValueFromString ("New");

        Assert.True (result);
        Assert.Equal ("New", item.Title);
        Assert.Equal ("New", item.GetValue ());
    }

    #endregion TrySetValueFromString
}
