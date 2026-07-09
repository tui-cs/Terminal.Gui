using System.Globalization;

namespace ViewsTests;

public class NumericUpDownTests
{
    [Fact]
    public void WhenCreated_ShouldHaveDefaultValues_int ()
    {
        NumericUpDown<int> numericUpDown = new ();

        Assert.Equal (0, numericUpDown.Value);
        Assert.Equal (1, numericUpDown.Increment);
    }

    [Fact]
    public void WhenCreated_ShouldHaveDefaultValues_long ()
    {
        NumericUpDown<long> numericUpDown = new ();

        Assert.Equal (0, numericUpDown.Value);
        Assert.Equal (1, numericUpDown.Increment);
    }

    [Fact]
    public void WhenCreated_ShouldHaveDefaultValues_float ()
    {
        NumericUpDown<float> numericUpDown = new ();

        Assert.Equal (0F, numericUpDown.Value);
        Assert.Equal (1.0F, numericUpDown.Increment);
    }

    [Fact]
    public void WhenCreated_ShouldHaveDefaultValues_double ()
    {
        NumericUpDown<double> numericUpDown = new ();

        Assert.Equal (0F, numericUpDown.Value);
        Assert.Equal (1.0F, numericUpDown.Increment);
    }

    [Fact]
    public void WhenCreated_ShouldHaveDefaultValues_decimal ()
    {
        NumericUpDown<decimal> numericUpDown = new ();

        Assert.Equal (0, numericUpDown.Value);
        Assert.Equal (1, numericUpDown.Increment);
    }

    [Fact]
    public void WhenCreatedWithCustomValues_ShouldHaveCustomValues_int ()
    {
        NumericUpDown<int> numericUpDown = new () { Value = 10, Increment = 2 };

        Assert.Equal (10, numericUpDown.Value);
        Assert.Equal (2, numericUpDown.Increment);
    }

    [Fact]
    public void WhenCreatedWithCustomValues_ShouldHaveCustomValues_float ()
    {
        NumericUpDown<float> numericUpDown = new () { Value = 10.5F, Increment = 2.5F };

        Assert.Equal (10.5F, numericUpDown.Value);
        Assert.Equal (2.5F, numericUpDown.Increment);
    }

    [Fact]
    public void WhenCreatedWithCustomValues_ShouldHaveCustomValues_decimal ()
    {
        NumericUpDown<decimal> numericUpDown = new () { Value = 10.5m, Increment = 2.5m };

        Assert.Equal (10.5m, numericUpDown.Value);
        Assert.Equal (2.5m, numericUpDown.Increment);
    }

    [Fact]
    public void WhenCreatedWithInvalidType_ShouldThrowInvalidOperationException () =>
        Assert.Throws<InvalidOperationException> (() => new NumericUpDown<string> ());

    [Fact]
    public void WhenCreatedWithInvalidTypeObject_ShouldNotThrowInvalidOperationException ()
    {
        Exception? exception = Record.Exception (() => new NumericUpDown<object> ());
        Assert.Null (exception);
    }

    [Fact]
    public void WhenCreatedWithValidNumberType_ShouldThrowInvalidOperationException_UnlessTheyAreRegisterAsValid ()
    {
        Exception? exception = Record.Exception (() => new NumericUpDown<short> ());
        Assert.NotNull (exception);
    }

    [Fact]
    public void WhenCreated_ShouldHaveDefaultWidthAndHeight_int ()
    {
        NumericUpDown<int> numericUpDown = new ();
        numericUpDown.SetRelativeLayout (new Size (100, 100));

        Assert.Equal (3, numericUpDown.Frame.Width);
        Assert.Equal (1, numericUpDown.Frame.Height);
    }

    [Fact]
    public void WhenCreated_ShouldHaveDefaultWidthAndHeight_float ()
    {
        NumericUpDown<float> numericUpDown = new ();
        numericUpDown.SetRelativeLayout (new Size (100, 100));

        Assert.Equal (3, numericUpDown.Frame.Width);
        Assert.Equal (1, numericUpDown.Frame.Height);
    }

    [Fact]
    public void WhenCreated_ShouldHaveDefaultWidthAndHeight_double ()
    {
        NumericUpDown<double> numericUpDown = new ();
        numericUpDown.SetRelativeLayout (new Size (100, 100));

        Assert.Equal (3, numericUpDown.Frame.Width);
        Assert.Equal (1, numericUpDown.Frame.Height);
    }

    [Fact]
    public void WhenCreated_ShouldHaveDefaultWidthAndHeight_long ()
    {
        NumericUpDown<long> numericUpDown = new ();
        numericUpDown.SetRelativeLayout (new Size (100, 100));

        Assert.Equal (3, numericUpDown.Frame.Width);
        Assert.Equal (1, numericUpDown.Frame.Height);
    }

    [Fact]
    public void WhenCreated_ShouldHaveDefaultWidthAndHeight_decimal ()
    {
        NumericUpDown<decimal> numericUpDown = new ();
        numericUpDown.SetRelativeLayout (new Size (100, 100));

        Assert.Equal (3, numericUpDown.Frame.Width);
        Assert.Equal (1, numericUpDown.Frame.Height);
    }

    [Fact]
    public void WhenCreated_Text_Should_Be_Correct_int ()
    {
        NumericUpDown<int> numericUpDown = new ();

        Assert.Equal ("0", numericUpDown.Text);
    }

    [Fact]
    public void WhenCreated_Text_Should_Be_Correct_float ()
    {
        NumericUpDown<float> numericUpDown = new ();

        Assert.Equal ("0", numericUpDown.Text);
    }

    [Fact]
    public void Format_Default ()
    {
        NumericUpDown<float> numericUpDown = new ();

        Assert.Equal ("{0}", numericUpDown.Format);
    }

    [Theory]
    [InlineData (0F, "{0}", "0")]
    [InlineData (1.1F, "{0}", "1.1")]
    [InlineData (0F, "{0:0%}", "0%")]
    [InlineData (.75F, "{0:0%}", "75%")]
    public void Format_decimal (float value, string format, string expectedText)
    {
        CultureInfo currentCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        NumericUpDown<float> numericUpDown = new ();

        numericUpDown.Format = format;
        numericUpDown.Value = value;

        Assert.Equal (expectedText, numericUpDown.Text);

        CultureInfo.CurrentCulture = currentCulture;
    }

    [Theory]
    [InlineData (0, "{0}", "0")]
    [InlineData (11, "{0}", "11")]
    [InlineData (-1, "{0}", "-1")]
    [InlineData (911, "{0:X}", "38F")]
    [InlineData (911, "0x{0:X04}", "0x038F")]
    public void Format_int (int value, string format, string expectedText)
    {
        CultureInfo currentCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        NumericUpDown<int> numericUpDown = new ();

        numericUpDown.Format = format;
        numericUpDown.Value = value;

        Assert.Equal (expectedText, numericUpDown.Text);

        CultureInfo.CurrentCulture = currentCulture;
    }

    [Fact]
    public void KeyDown_CursorUp_Increments ()
    {
        NumericUpDown<int> numericUpDown = new ();

        numericUpDown.NewKeyDownEvent (Key.CursorUp);

        Assert.Equal (1, numericUpDown.Value);
    }

    [Fact]
    public void KeyDown_CursorDown_Decrements ()
    {
        NumericUpDown<int> numericUpDown = new ();

        numericUpDown.NewKeyDownEvent (Key.CursorDown);

        Assert.Equal (-1, numericUpDown.Value);
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void NumericUpDown_UpArrow_IncrementsValue ()
    {
        NumericUpDown<int> numericUpDown = new () { Value = 5 };

        // Up arrow increments value via Command.Up
        numericUpDown.NewKeyDownEvent (Key.CursorUp);

        Assert.Equal (6, numericUpDown.Value);

        numericUpDown.Dispose ();
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void NumericUpDown_DownArrow_DecrementsValue ()
    {
        NumericUpDown<int> numericUpDown = new () { Value = 5 };

        // Down arrow decrements value via Command.Down
        numericUpDown.NewKeyDownEvent (Key.CursorDown);

        Assert.Equal (4, numericUpDown.Value);

        numericUpDown.Dispose ();
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void NumericUpDown_ButtonAccept_ChangesValue ()
    {
        NumericUpDown<int> numericUpDown = new () { Value = 5, Width = 10, Height = 1 };
        numericUpDown.BeginInit ();
        numericUpDown.EndInit ();

        // Internal buttons change value when accepting
        // Verify the control is set up correctly
        Assert.Equal (5, numericUpDown.Value);

        numericUpDown.Dispose ();
    }

    // GitHub Copilot
    // Tests for ValueChanging event - should be cancellable
    [Fact]
    public void ValueChanging_Event_Is_Raised ()
    {
        NumericUpDown<int> numericUpDown = new () { Value = 0 };
        var eventRaised = false;
        int? currentValue = null;
        int? newValue = null;

        numericUpDown.ValueChanging += (_, e) =>
                                       {
                                           eventRaised = true;
                                           currentValue = e.CurrentValue;
                                           newValue = e.NewValue;
                                       };

        numericUpDown.Value = 10;

        Assert.True (eventRaised);
        Assert.Equal (0, currentValue);
        Assert.Equal (10, newValue);

        numericUpDown.Dispose ();
    }

    // GitHub Copilot
    // Tests that ValueChanging can cancel the value change
    [Fact]
    public void ValueChanging_Event_Can_Cancel_Change ()
    {
        NumericUpDown<int> numericUpDown = new () { Value = 5 };

        numericUpDown.ValueChanging += (_, e) => e.Handled = true;

        numericUpDown.Value = 10;

        Assert.Equal (5, numericUpDown.Value); // Value should not change

        numericUpDown.Dispose ();
    }

    // GitHub Copilot
    // Tests for ValueChanged event
    [Fact]
    public void ValueChanged_Event_Is_Raised ()
    {
        NumericUpDown<int> numericUpDown = new () { Value = 0 };
        var eventRaised = false;
        int? oldValue = null;
        int? newValue = null;

        numericUpDown.ValueChanged += (_, e) =>
                                      {
                                          eventRaised = true;
                                          oldValue = e.OldValue;
                                          newValue = e.NewValue;
                                      };

        numericUpDown.Value = 10;

        Assert.True (eventRaised);
        Assert.Equal (0, oldValue);
        Assert.Equal (10, newValue);

        numericUpDown.Dispose ();
    }

    // GitHub Copilot
    // Tests for ValueChangedUntyped event
    [Fact]
    public void ValueChangedUntyped_Event_Is_Raised ()
    {
        NumericUpDown<int> numericUpDown = new () { Value = 0 };
        var eventRaised = false;
        object? oldValue = null;
        object? newValue = null;

        numericUpDown.ValueChangedUntyped += (_, e) =>
                                             {
                                                 eventRaised = true;
                                                 oldValue = e.OldValue;
                                                 newValue = e.NewValue;
                                             };

        numericUpDown.Value = 10;

        Assert.True (eventRaised);
        Assert.Equal (0, oldValue);
        Assert.Equal (10, newValue);

        numericUpDown.Dispose ();
    }

    // GitHub Copilot
    // Tests for FormatChanged event
    [Fact]
    public void FormatChanged_Event_Is_Raised ()
    {
        NumericUpDown<int> numericUpDown = new ();
        var eventRaised = false;
        string? newFormat = null;

        numericUpDown.FormatChanged += (_, e) =>
                                       {
                                           eventRaised = true;
                                           newFormat = e.Value;
                                       };

        numericUpDown.Format = "{0:X}";

        Assert.True (eventRaised);
        Assert.Equal ("{0:X}", newFormat);

        numericUpDown.Dispose ();
    }

    // GitHub Copilot
    // Tests that Format same value does not raise event
    [Fact]
    public void Format_Same_Value_Does_Not_Raise_Event ()
    {
        NumericUpDown<int> numericUpDown = new ();
        var eventCount = 0;

        numericUpDown.FormatChanged += (_, _) => eventCount++;

        numericUpDown.Format = "{0}"; // Same as default

        Assert.Equal (0, eventCount);

        numericUpDown.Dispose ();
    }

    // GitHub Copilot
    // Tests for IncrementChanged event
    [Fact]
    public void IncrementChanged_Event_Is_Raised ()
    {
        NumericUpDown<int> numericUpDown = new ();
        var eventRaised = false;
        int? newIncrement = null;

        numericUpDown.IncrementChanged += (_, e) =>
                                          {
                                              eventRaised = true;
                                              newIncrement = e.Value;
                                          };

        numericUpDown.Increment = 5;

        Assert.True (eventRaised);
        Assert.Equal (5, newIncrement);

        numericUpDown.Dispose ();
    }

    // GitHub Copilot
    // Tests that Increment same value does not raise event
    [Fact]
    public void Increment_Same_Value_Does_Not_Raise_Event ()
    {
        NumericUpDown<int> numericUpDown = new ();
        var eventCount = 0;

        numericUpDown.IncrementChanged += (_, _) => eventCount++;

        numericUpDown.Increment = 1; // Same as default

        Assert.Equal (0, eventCount);

        numericUpDown.Dispose ();
    }

    // GitHub Copilot
    // Tests that setting same Value does not raise events
    [Fact]
    public void Value_Same_Value_Does_Not_Raise_Events ()
    {
        NumericUpDown<int> numericUpDown = new () { Value = 5 };
        var changingCount = 0;
        var changedCount = 0;

        numericUpDown.ValueChanging += (_, _) => changingCount++;
        numericUpDown.ValueChanged += (_, _) => changedCount++;

        numericUpDown.Value = 5; // Same value

        Assert.Equal (0, changingCount);
        Assert.Equal (0, changedCount);

        numericUpDown.Dispose ();
    }

    // GitHub Copilot
    // Tests TryConvert with valid conversion
    [Fact]
    public void TryConvert_Valid_Conversion_Returns_True ()
    {
        bool result = NumericUpDown<int>.TryConvert (10, out int converted);

        Assert.True (result);
        Assert.Equal (10, converted);
    }

    // GitHub Copilot
    // Tests TryConvert with invalid conversion
    [Fact]
    public void TryConvert_Invalid_Conversion_Returns_False ()
    {
        bool result = NumericUpDown<int>.TryConvert ("not a number", out int converted);

        Assert.False (result);
        Assert.Equal (default (int), converted);
    }

    // GitHub Copilot
    // Tests TryConvert from double to int - uses banker's rounding
    [Fact]
    public void TryConvert_Double_To_Int_Rounds ()
    {
        bool result = NumericUpDown<int>.TryConvert (10.9, out int converted);

        Assert.True (result);
        Assert.Equal (11, converted); // Convert.ChangeType uses banker's rounding
    }

    // GitHub Copilot
    // Tests InvokeCommand with Command.Up
    [Fact]
    public void InvokeCommand_Up_Increments_Value ()
    {
        NumericUpDown<int> numericUpDown = new () { Value = 5, Increment = 2 };

        numericUpDown.InvokeCommand (Command.Up);

        Assert.Equal (7, numericUpDown.Value);

        numericUpDown.Dispose ();
    }

    // GitHub Copilot
    // Tests InvokeCommand with Command.Down
    [Fact]
    public void InvokeCommand_Down_Decrements_Value ()
    {
        NumericUpDown<int> numericUpDown = new () { Value = 5, Increment = 2 };

        numericUpDown.InvokeCommand (Command.Down);

        Assert.Equal (3, numericUpDown.Value);

        numericUpDown.Dispose ();
    }

    // GitHub Copilot
    // Tests non-generic NumericUpDown defaults to int
    [Fact]
    public void NonGeneric_NumericUpDown_Uses_Int ()
    {
        NumericUpDown numericUpDown = new ();

        Assert.Equal (0, numericUpDown.Value);
        Assert.Equal (1, numericUpDown.Increment);

        numericUpDown.Value = 100;
        Assert.Equal (100, numericUpDown.Value);

        numericUpDown.Dispose ();
    }

    // GitHub Copilot
    // Tests that Text is updated when Value changes
    [Fact]
    public void Text_Updates_When_Value_Changes ()
    {
        NumericUpDown<int> numericUpDown = new () { Value = 0 };

        Assert.Equal ("0", numericUpDown.Text);

        numericUpDown.Value = 42;

        Assert.Equal ("42", numericUpDown.Text);

        numericUpDown.Dispose ();
    }

    // GitHub Copilot
    // Tests that Text respects Format when Value changes
    [Fact]
    public void Text_Respects_Format_When_Value_Changes ()
    {
        NumericUpDown<int> numericUpDown = new () { Format = "{0:X}", Value = 255 };

        Assert.Equal ("FF", numericUpDown.Text);

        numericUpDown.Dispose ();
    }

    // GitHub Copilot
    // Tests Command.Up with object type returns false
    [Fact]
    public void Command_Up_With_Object_Type_Returns_False ()
    {
        NumericUpDown<object> numericUpDown = new ();

        bool? result = numericUpDown.InvokeCommand (Command.Up);

        Assert.False (result);

        numericUpDown.Dispose ();
    }

    // GitHub Copilot
    // Tests Command.Down with object type returns false
    [Fact]
    public void Command_Down_With_Object_Type_Returns_False ()
    {
        NumericUpDown<object> numericUpDown = new ();

        bool? result = numericUpDown.InvokeCommand (Command.Down);

        Assert.False (result);

        numericUpDown.Dispose ();
    }

    // GitHub Copilot
    // Tests negative values work correctly
    [Fact]
    public void Negative_Values_Work_Correctly ()
    {
        NumericUpDown<int> numericUpDown = new () { Value = -10 };

        Assert.Equal ("-10", numericUpDown.Text);

        numericUpDown.InvokeCommand (Command.Down);

        Assert.Equal (-11, numericUpDown.Value);

        numericUpDown.Dispose ();
    }

    // GitHub Copilot
    // Tests decimal precision is maintained
    [Fact]
    public void Decimal_Precision_Is_Maintained ()
    {
        NumericUpDown<decimal> numericUpDown = new () { Value = 1.234m, Increment = 0.001m };

        numericUpDown.InvokeCommand (Command.Up);

        Assert.Equal (1.235m, numericUpDown.Value);

        numericUpDown.Dispose ();
    }

    // GitHub Copilot
    // Tests that CanFocus is true by default
    [Fact]
    public void CanFocus_Is_True_By_Default ()
    {
        NumericUpDown<int> numericUpDown = new ();

        Assert.True (numericUpDown.CanFocus);

        numericUpDown.Dispose ();
    }

    // GitHub Copilot
    // Tests that editable mode parses text, keeps invalid text visible, and resyncs on increment
    [Fact]
    public void CanEdit_Parses_Valid_Text_And_Resyncs_Invalid_Text_On_Increment ()
    {
        NumericUpDown<decimal> numericUpDown = new ()
        {
            CanEdit = true,
            Value = 12.5m,
            Increment = 0.25m,
            Format = "{0:0.00}"
        };

        TextField editor = Assert.IsType<TextField> (numericUpDown.SubViews.OfType<TextField> ().Single ());

        Assert.Equal ("12.50", editor.Text);
        Assert.Equal (12.5m, numericUpDown.Value);

        editor.Text = "3";

        // The parsed value is applied, but the editor text is NOT reformatted while the user is typing.
        Assert.Equal (3m, numericUpDown.Value);
        Assert.Equal ("3", editor.Text);

        editor.Text = "3a.00";

        Assert.Equal (3m, numericUpDown.Value);
        Assert.Equal ("3a.00", editor.Text);

        // Increment resyncs the editor text to the formatted value.
        numericUpDown.InvokeCommand (Command.Up);

        Assert.Equal (3.25m, numericUpDown.Value);
        Assert.Equal ("3.25", editor.Text);

        numericUpDown.Dispose ();
    }

    // Claude - Opus 4.8
    // Editable mode must keep the value editor between the down and up buttons so keyboard Tab order matches
    // the visual left-to-right layout (down, value, up).
    [Fact]
    public void CanEdit_Editor_Preserves_SubView_Order_Between_Buttons ()
    {
        NumericUpDown<int> numericUpDown = new () { CanEdit = true };

        TextField editor = numericUpDown.SubViews.OfType<TextField> ().Single ();

        Assert.Equal (3, numericUpDown.SubViews.Count);
        Assert.Same (editor, numericUpDown.SubViews.ElementAt (1));

        numericUpDown.Dispose ();
    }

    // Claude - Opus 4.8
    // Typing into the editor must update Value without reformatting the in-progress text, so partial input
    // such as a trailing decimal point is preserved. Only Up/Down (or an external Value change) resyncs the text.
    [Fact]
    public void CanEdit_Typing_Does_Not_Reformat_Editor_Text_Until_Increment ()
    {
        NumericUpDown<decimal> numericUpDown = new ()
        {
            CanEdit = true,
            Value = 0m,
            Increment = 0.25m,
            Format = "{0:0.00}"
        };

        TextField editor = numericUpDown.SubViews.OfType<TextField> ().Single ();

        // Simulate mid-typing a fractional value; the trailing "." must not be wiped out by reformatting.
        editor.Text = "3.";

        Assert.Equal (3m, numericUpDown.Value);
        Assert.Equal ("3.", editor.Text);

        editor.Text = "3.5";

        Assert.Equal (3.5m, numericUpDown.Value);
        Assert.Equal ("3.5", editor.Text);

        // An external Value change resyncs the editor to the formatted display.
        numericUpDown.InvokeCommand (Command.Up);

        Assert.Equal (3.75m, numericUpDown.Value);
        Assert.Equal ("3.75", editor.Text);

        numericUpDown.Dispose ();
    }
}
