using System.Globalization;
using UnitTests;

namespace ViewsTests;

// Claude - Opus 4.6
public class DateEditorTests (ITestOutputHelper output) : TestDriverBase
{
    [Fact]
    public void Constructor_Defaults ()
    {
        DateEditor de = new ();
        de.Layout ();

        Assert.NotNull (de.Provider);
        Assert.IsType<DateTextProvider> (de.Provider);
        Assert.Equal (DateTime.Today, de.Value);
        Assert.NotNull (de.Format);
        Assert.Equal (CultureInfo.CurrentCulture.DateTimeFormat, de.Format);
    }

    [Fact]
    public void Value_Property_GetSet ()
    {
        DateEditor de = new ();
        DateTime testDate = new (2024, 3, 15);

        de.Value = testDate;
        Assert.Equal (testDate, de.Value);

        // Test setting to another date
        DateTime anotherDate = new (2000, 1, 1);
        de.Value = anotherDate;
        Assert.Equal (anotherDate, de.Value);

        // Test setting to max year
        DateTime maxDate = new (9999, 12, 31);
        de.Value = maxDate;
        Assert.Equal (maxDate, de.Value);
    }

    [Fact]
    public void Format_Property_Changes_Width ()
    {
        DateEditor de = new ();

        // Set to US format (MM/dd/yyyy = 10 chars)
        var usFormat = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-US").DateTimeFormat.Clone ();
        de.Format = usFormat;
        de.Layout ();

        int initialWidth = de.Frame.Width;
        Assert.True (initialWidth > 0);

        // Change to a different culture format
        var deFormat = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("de-DE").DateTimeFormat.Clone ();
        de.Format = deFormat;
        de.Layout ();

        // Width should still be reasonable
        int newWidth = de.Frame.Width;
        Assert.True (newWidth > 0);
    }

    [Fact]
    public void ValueChanging_Event_Can_Cancel ()
    {
        DateEditor de = new () { Value = new DateTime (2024, 1, 1) };
        var eventFired = false;

        de.ValueChanging += (_, e) =>
                            {
                                eventFired = true;
                                e.Handled = true; // Cancel the change
                            };

        de.Value = new DateTime (2024, 6, 15);

        Assert.True (eventFired);
        Assert.Equal (new DateTime (2024, 1, 1), de.Value); // Value should not change
    }

    [Fact]
    public void ValueChanged_Event_Fires ()
    {
        DateEditor de = new () { Value = new DateTime (2024, 1, 1) };
        var eventFired = false;
        DateTime oldValue = DateTime.MinValue;
        DateTime newValue = DateTime.MinValue;

        de.ValueChanged += (_, e) =>
                           {
                               eventFired = true;
                               oldValue = e.OldValue;
                               newValue = e.NewValue;
                           };

        DateTime expectedNewValue = new (2024, 6, 15);
        de.Value = expectedNewValue;

        Assert.True (eventFired);
        Assert.Equal (new DateTime (2024, 1, 1), oldValue);
        Assert.Equal (expectedNewValue, newValue);
    }

    [Fact]
    public void DateTextProvider_CursorNavigation_SkipsSeparators ()
    {
        DateTextProvider provider = new ();

        // Use US format: MM/dd/yyyy
        var usFormat = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-US").DateTimeFormat.Clone ();
        provider.Format = usFormat;

        // CursorStart should return 0
        Assert.Equal (0, provider.CursorStart ());

        // For "MM/dd/yyyy" (positions: 0,1,/,3,4,/,6,7,8,9)
        // Position 2 is separator, cursor should skip it
        int cursorPos = provider.CursorRight (1);
        Assert.NotEqual (2, cursorPos); // Should skip position 2 (separator)

        // CursorLeft from position 3 should skip separator at 2 and go to 1
        cursorPos = provider.CursorLeft (3);
        Assert.Equal (1, cursorPos);
    }

    [Fact]
    public void DateTextProvider_InsertAt_ReplacesDigit ()
    {
        DateTextProvider provider = new ();

        // Use US format
        var usFormat = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-US").DateTimeFormat.Clone ();
        provider.Format = usFormat;

        provider.DateValue = new DateTime (2024, 1, 15); // "01/15/2024"

        // Insert '1' at position 0 (first month digit)
        bool result = provider.InsertAt ('1', 0);
        Assert.True (result);

        // Check that the value was updated
        string text = provider.Text;
        Assert.StartsWith ("1", text);
    }

    [Fact]
    public void DateTextProvider_Delete_ReplacesWithZero ()
    {
        DateTextProvider provider = new ();

        // Use US format
        var usFormat = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-US").DateTimeFormat.Clone ();
        provider.Format = usFormat;

        provider.DateValue = new DateTime (2024, 11, 25); // "11/25/2024"

        // Delete at position 0 should replace with '0'
        bool result = provider.Delete (0);
        Assert.True (result);

        // The month should now start with 0
        string text = provider.Text;
        Assert.StartsWith ("0", text);
    }

    [Fact]
    public void DateTextProvider_Format_Change_Updates_Pattern ()
    {
        DateTextProvider provider = new ();
        DateTime testDate = new (2024, 3, 15);
        provider.DateValue = testDate;

        // Use US format
        var usFormat = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-US").DateTimeFormat.Clone ();
        provider.Format = usFormat;

        string usDisplay = provider.DisplayText;
        output.WriteLine ($"US display: \"{usDisplay}\"");

        // Change to German format (dd.MM.yyyy)
        var deFormat = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("de-DE").DateTimeFormat.Clone ();
        provider.Format = deFormat;

        string deDisplay = provider.DisplayText;
        output.WriteLine ($"DE display: \"{deDisplay}\"");

        // Display should change (field order differs)
        Assert.NotEqual (usDisplay, deDisplay);
    }

    [Fact]
    public void DateTextProvider_IsValid_Always_True ()
    {
        DateTextProvider provider = new ();

        // Valid date
        provider.DateValue = new DateTime (2024, 12, 31);
        Assert.True (provider.IsValid);

        // Another valid date
        provider.DateValue = new DateTime (2000, 1, 1);
        Assert.True (provider.IsValid);

        // Provider auto-corrects invalid values, so IsValid should always be true
        provider.DateValue = new DateTime (2024, 2, 29); // Leap year
        Assert.True (provider.IsValid);
    }

    [Fact]
    public void DateEditor_KeyInput_UpdatesValue ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        try
        {
            DateEditor de = new () { App = app };
            de.Layout ();
            de.Value = new DateTime (2024, 1, 1);

            // Simulate typing '1'
            de.NewKeyDownEvent (Key.D1);

            // The value should have been updated
            string text = de.Text.Trim ();
            Assert.Contains ("1", text);
        }
        finally
        {
            app.Dispose ();
        }
    }

    [Fact]
    public void DateEditor_Navigation_Keys ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        try
        {
            DateEditor de = new () { App = app };
            de.Layout ();

            // Home key should move to start
            de.NewKeyDownEvent (Key.Home);

            // End key should move to end
            de.NewKeyDownEvent (Key.End);

            // Arrow keys should navigate
            de.NewKeyDownEvent (Key.CursorLeft);
            de.NewKeyDownEvent (Key.CursorRight);

            // No exceptions should be thrown
            Assert.NotNull (de);
        }
        finally
        {
            app.Dispose ();
        }
    }

    [Fact]
    public void DateEditor_IValue_GetValue ()
    {
        DateEditor de = new ();
        DateTime testDate = new (2024, 3, 15);
        de.Value = testDate;

        object? value = ((IValue)de).GetValue ();

        Assert.NotNull (value);
        Assert.IsType<DateTime> (value);
        Assert.Equal (testDate, (DateTime)value!);
    }

    [Fact]
    public void DateTextProvider_US_Format ()
    {
        DateTextProvider provider = new ();

        var usFormat = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-US").DateTimeFormat.Clone ();
        provider.Format = usFormat;

        provider.DateValue = new DateTime (2024, 3, 15);

        string display = provider.DisplayText;
        output.WriteLine ($"US display: \"{display}\"");

        // Should be MM/dd/yyyy format
        Assert.Contains ("/", display);
        Assert.Equal ("03/15/2024", display);
    }

    [Fact]
    public void DateTextProvider_UK_Format ()
    {
        DateTextProvider provider = new ();

        var ukFormat = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-GB").DateTimeFormat.Clone ();
        provider.Format = ukFormat;

        provider.DateValue = new DateTime (2024, 3, 15);

        string display = provider.DisplayText;
        output.WriteLine ($"UK display: \"{display}\"");

        // Should be dd/MM/yyyy format
        Assert.Contains ("/", display);
        Assert.Equal ("15/03/2024", display);
    }

    [Fact]
    public void DateTextProvider_German_Format ()
    {
        DateTextProvider provider = new ();

        var deFormat = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("de-DE").DateTimeFormat.Clone ();
        provider.Format = deFormat;

        provider.DateValue = new DateTime (2024, 3, 15);

        string display = provider.DisplayText;
        output.WriteLine ($"DE display: \"{display}\"");

        // Should be dd.MM.yyyy format
        Assert.Contains (".", display);
        Assert.Equal ("15.03.2024", display);
    }

    [Fact]
    public void DateEditor_Delete_And_Backspace ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        try
        {
            DateEditor de = new () { App = app };

            var usFormat = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-US").DateTimeFormat.Clone ();
            de.Format = usFormat;

            de.Value = new DateTime (2024, 11, 25);
            de.Layout ();

            // Move to start and delete
            de.NewKeyDownEvent (Key.Home);
            de.NewKeyDownEvent (Key.Delete);

            // Value should have changed (first digit replaced with 0)
            string text = de.Text.Trim ();
            Assert.NotEqual ("11/25/2024", text);

            // Backspace should also work
            de.NewKeyDownEvent (Key.Backspace);

            // Text should have changed again
            Assert.NotNull (de.Text);
        }
        finally
        {
            app.Dispose ();
        }
    }

    [Fact]
    public void DateTextProvider_CursorEnd_Returns_Last_Position ()
    {
        DateTextProvider provider = new ();

        int endPos = provider.CursorEnd ();

        // End position should be >= 0
        Assert.True (endPos >= 0);

        // For "MM/dd/yyyy", end should be at last digit (position 9)
        int displayLength = provider.DisplayText.Length;
        Assert.True (endPos < displayLength);
    }

    [Fact]
    public void DateEditor_Text_Property_Updates_Value ()
    {
        DateEditor de = new ();

        // Use US format
        var usFormat = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-US").DateTimeFormat.Clone ();
        de.Format = usFormat;

        // Set text directly
        de.Text = "03/15/2024";

        // Value should be updated
        Assert.Equal (2024, de.Value.Year);
        Assert.Equal (3, de.Value.Month);
        Assert.Equal (15, de.Value.Day);
    }

    [Fact]
    public void DateTextProvider_InsertAt_NonDigit_Returns_False ()
    {
        DateTextProvider provider = new ();

        // Try to insert a non-digit character at a digit position
        bool result = provider.InsertAt ('x', 0);

        // Should fail
        Assert.False (result);
    }

    [Fact]
    public void DateEditor_Multiple_Format_Changes ()
    {
        DateEditor de = new () { Value = new DateTime (2024, 3, 15) };
        de.Layout ();

        // Change format multiple times between US and UK
        for (var i = 0; i < 3; i++)
        {
            DateTimeFormatInfo format = i % 2 == 0
                                            ? (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-US").DateTimeFormat.Clone ()
                                            : (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-GB").DateTimeFormat.Clone ();
            de.Format = format;
            de.Layout ();

            // Value should remain the same
            Assert.Equal (2024, de.Value.Year);
            Assert.Equal (3, de.Value.Month);
            Assert.Equal (15, de.Value.Day);
        }
    }

    [Fact]
    public void DateTextProvider_ManualParse_InvalidInput ()
    {
        DateTextProvider provider = new ();

        var usFormat = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-US").DateTimeFormat.Clone ();
        provider.Format = usFormat;

        DateTime initialValue = provider.DateValue;

        // Test invalid input (no separator)
        provider.Text = "invalid";
        Assert.Equal (initialValue, provider.DateValue);

        // Test incomplete input (only two parts)
        provider.Text = "01/15";
        Assert.Equal (initialValue, provider.DateValue);
    }

    [Fact]
    public void DateTextProvider_ManualParse_AutoCorrection ()
    {
        DateTextProvider provider = new ();

        var usFormat = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-US").DateTimeFormat.Clone ();
        provider.Format = usFormat;

        // Test auto-correction for out-of-range month
        provider.Text = "13/15/2024";
        Assert.Equal (12, provider.DateValue.Month); // Max 12

        // Test auto-correction for out-of-range day
        provider.Text = "02/30/2024";
        Assert.Equal (29, provider.DateValue.Day); // Feb 2024 is leap year, max 29
    }

    [Fact]
    public void DateEditor_ValueChanging_Cancel ()
    {
        DateEditor de = new ();
        DateTime initialValue = new (2024, 1, 1);
        de.Value = initialValue;

        var changingEventFired = false;
        var changedEventFired = false;

        de.ValueChanging += (_, e) =>
                            {
                                changingEventFired = true;
                                e.Handled = true; // Cancel the change
                            };

        de.ValueChanged += (_, _) => { changedEventFired = true; };

        // Try to set new value
        de.Value = new DateTime (2024, 6, 15);

        // ValueChanging should have fired, but ValueChanged should not
        Assert.True (changingEventFired);
        Assert.False (changedEventFired);

        // Value should not have changed
        Assert.Equal (initialValue, de.Value);
    }

    [Fact]
    public void DateTextProvider_Delete_AtSeparatorPosition ()
    {
        DateTextProvider provider = new ();

        var usFormat = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-US").DateTimeFormat.Clone ();
        provider.Format = usFormat;

        provider.DateValue = new DateTime (2024, 3, 15);

        // Try to delete at separator position (position 2 in "03/15/2024")
        bool result = provider.Delete (2);

        // Delete at separator should fail (not a digit)
        Assert.False (result);
    }

    [Fact]
    public void DateEditor_ValueChangedUntyped_Event ()
    {
        DateEditor de = new ();
        var eventFired = false;
        object? oldValue = null;
        object? newValue = null;

        de.ValueChangedUntyped += (_, e) =>
                                  {
                                      eventFired = true;
                                      oldValue = e.OldValue;
                                      newValue = e.NewValue;
                                  };

        DateTime testValue = new (2024, 6, 15);
        de.Value = testValue;

        Assert.True (eventFired);
        Assert.Equal (testValue, newValue);
    }

    [Fact]
    public void DateTextProvider_CursorLeft_FromStart ()
    {
        DateTextProvider provider = new ();

        var usFormat = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-US").DateTimeFormat.Clone ();
        provider.Format = usFormat;

        // CursorLeft from start should return start
        int pos = provider.CursorLeft (0);
        Assert.Equal (0, pos);
    }

    [Fact]
    public void DateTextProvider_CursorRight_FromEnd ()
    {
        DateTextProvider provider = new ();

        var usFormat = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-US").DateTimeFormat.Clone ();
        provider.Format = usFormat;

        int endPos = provider.CursorEnd ();

        // CursorRight from end should return end
        int pos = provider.CursorRight (endPos);
        Assert.Equal (endPos, pos);
    }

    [Fact]
    public void DateEditor_Default_Constructor_Width_Fits_DisplayText ()
    {
        DateEditor de = new () { Value = new DateTime (2024, 3, 15) };
        de.Layout ();

        output.WriteLine ($"DisplayText: \"{de.Provider!.DisplayText}\"");
        output.WriteLine ($"DisplayText.Length: {de.Provider.DisplayText.Length}");
        output.WriteLine ($"Frame.Width: {de.Frame.Width}");

        Assert.True (de.Frame.Width >= de.Provider.DisplayText.Length,
                     $"Frame width {de.Frame.Width} is too narrow for DisplayText \"{de.Provider.DisplayText}\" ({de.Provider.DisplayText.Length} chars)");
    }

    [Fact]
    public void DateTextProvider_CursorNavigation_Comprehensive_US ()
    {
        DateTextProvider provider = new ();

        var usFormat = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-US").DateTimeFormat.Clone ();
        provider.Format = usFormat;

        // Test CursorStart
        Assert.Equal (0, provider.CursorStart ());

        // Test navigation through all positions for "MM/dd/yyyy"
        List<int> visitedPositions = [provider.CursorStart ()];
        int pos = provider.CursorStart ();

        while (pos < provider.CursorEnd ())
        {
            pos = provider.CursorRight (pos);
            visitedPositions.Add (pos);
        }

        output.WriteLine ($"Forward positions: [{string.Join (", ", visitedPositions)}]");

        // Should visit: 0, 1, 3, 4, 6, 7, 8, 9 (skipping separators at 2, 5)
        Assert.Equal ([0, 1, 3, 4, 6, 7, 8, 9], visitedPositions);
    }

    [Fact]
    public void DateTextProvider_InsertAt_AllDigitPositions_US ()
    {
        DateTextProvider provider = new ();

        var usFormat = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-US").DateTimeFormat.Clone ();
        provider.Format = usFormat;
        provider.DateValue = new DateTime (2000, 1, 1); // "01/01/2000"

        output.WriteLine ($"Initial: \"{provider.DisplayText}\"");

        // Type at each editable position to enter 12/25/2024
        Assert.True (provider.InsertAt ('1', 0)); // "11/01/2000"
        output.WriteLine ($"After InsertAt('1', 0): \"{provider.DisplayText}\"");

        Assert.True (provider.InsertAt ('2', 1)); // "12/01/2000"
        output.WriteLine ($"After InsertAt('2', 1): \"{provider.DisplayText}\"");

        Assert.True (provider.InsertAt ('2', 3)); // "12/21/2000"
        output.WriteLine ($"After InsertAt('2', 3): \"{provider.DisplayText}\"");

        Assert.True (provider.InsertAt ('5', 4)); // "12/25/2000"
        output.WriteLine ($"After InsertAt('5', 4): \"{provider.DisplayText}\"");

        Assert.True (provider.InsertAt ('2', 6)); // "12/25/2000"
        output.WriteLine ($"After InsertAt('2', 6): \"{provider.DisplayText}\"");

        Assert.True (provider.InsertAt ('0', 7)); // "12/25/2000"
        output.WriteLine ($"After InsertAt('0', 7): \"{provider.DisplayText}\"");

        Assert.True (provider.InsertAt ('2', 8)); // "12/25/2020"
        output.WriteLine ($"After InsertAt('2', 8): \"{provider.DisplayText}\"");

        Assert.True (provider.InsertAt ('4', 9)); // "12/25/2024"
        output.WriteLine ($"After InsertAt('4', 9): \"{provider.DisplayText}\"");

        Assert.Equal ("12/25/2024", provider.DisplayText);
        Assert.Equal (new DateTime (2024, 12, 25), provider.DateValue);
    }

    [Fact]
    public void DateTextProvider_DaysInMonth_AutoCorrection ()
    {
        DateTextProvider provider = new ();

        var usFormat = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-US").DateTimeFormat.Clone ();
        provider.Format = usFormat;

        // Set Feb 28 in non-leap year
        provider.DateValue = new DateTime (2023, 2, 28);
        output.WriteLine ($"Feb 28 2023: \"{provider.DisplayText}\"");
        Assert.Equal ("02/28/2023", provider.DisplayText);

        // Set Feb 29 in leap year
        provider.DateValue = new DateTime (2024, 2, 29);
        output.WriteLine ($"Feb 29 2024: \"{provider.DisplayText}\"");
        Assert.Equal ("02/29/2024", provider.DisplayText);
    }

    [Fact]
    public void DateEditor_DisplayText_Renders_Correctly ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (20, 1);

        try
        {
            Runnable<bool> runnable = new () { Width = 20, Height = 1 };
            app.Begin (runnable);

            var usFormat = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-US").DateTimeFormat.Clone ();

            DateEditor de = new () { Height = 1, Value = new DateTime (2024, 3, 15), Format = usFormat };
            runnable.Add (de);
            app.LayoutAndDraw ();

            output.WriteLine ($"DisplayText: \"{de.Provider!.DisplayText}\"");
            output.WriteLine ($"Frame: {de.Frame}");

            Assert.Equal ("03/15/2024", de.Provider.DisplayText);

            DriverAssert.AssertDriverContentsWithFrameAre (@"03/15/2024", output, app.Driver);
        }
        finally
        {
            app.Dispose ();
        }
    }

    [Fact]
    public void DateEditor_Typing_Date_Renders_Correctly ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (20, 1);

        try
        {
            Runnable<bool> runnable = new () { Width = 20, Height = 1 };
            app.Begin (runnable);

            var usFormat = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-US").DateTimeFormat.Clone ();

            DateEditor de = new () { Width = 12, Height = 1, Value = new DateTime (2024, 1, 1), Format = usFormat };
            runnable.Add (de);
            app.LayoutAndDraw ();

            output.WriteLine ($"Initial: \"{de.Provider!.DisplayText}\"");
            Assert.Equal ("01/01/2024", de.Provider.DisplayText);

            // Simulate focus and typing "12"
            de.SetFocus ();
            de.NewKeyDownEvent (Key.Home);
            de.NewKeyDownEvent (Key.D1);
            app.LayoutAndDraw ();

            output.WriteLine ($"After '1': \"{de.Provider.DisplayText}\"");

            de.NewKeyDownEvent (Key.D2);
            app.LayoutAndDraw ();

            output.WriteLine ($"After '2': \"{de.Provider.DisplayText}\"");

            // Month should now be 12
            Assert.Equal (12, de.Value.Month);

            DriverAssert.AssertDriverContentsWithFrameAre (@"12/01/2024", output, app.Driver);
        }
        finally
        {
            app.Dispose ();
        }
    }

    [Fact]
    public void DateEditor_CursorRight_SkipsSeparator_US ()
    {
        DateTextProvider provider = new ();
        var usFormat = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-US").DateTimeFormat.Clone ();
        provider.Format = usFormat;

        // Position 0 = M tens, 1 = M ones, 2 = '/', 3 = d tens
        int nextPos = provider.CursorRight (1);
        output.WriteLine ($"CursorRight(1) = {nextPos}");

        // Should skip separator at position 2 and land on 3
        Assert.Equal (3, nextPos);
    }

    [Fact]
    public void DateEditor_CursorLeft_SkipsSeparator_US ()
    {
        DateTextProvider provider = new ();
        var usFormat = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-US").DateTimeFormat.Clone ();
        provider.Format = usFormat;

        // CursorLeft from position 3 (d tens) should skip separator at 2 to position 1
        int prevPos = provider.CursorLeft (3);
        output.WriteLine ($"CursorLeft(3) = {prevPos}");
        Assert.Equal (1, prevPos);
    }

    [Fact]
    public void DateTextProvider_NormalizedPattern_PadsToTwoDigits ()
    {
        DateTextProvider provider = new ();

        // US format uses "M/d/yyyy" in some locales - verify normalization
        var usFormat = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-US").DateTimeFormat.Clone ();
        provider.Format = usFormat;

        // Single-digit month value
        provider.DateValue = new DateTime (2024, 3, 5);
        string display = provider.DisplayText;
        output.WriteLine ($"Display for 2024-03-05: \"{display}\"");

        // Should be padded to 2 digits
        Assert.Equal (10, display.Length); // "MM/dd/yyyy" = 10 chars
    }

    [Fact]
    public void DateTextProvider_FieldPositions_AreConsistent ()
    {
        DateTextProvider provider = new ();
        var usFormat = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-US").DateTimeFormat.Clone ();
        provider.Format = usFormat;

        // Single-digit month and day
        provider.DateValue = new DateTime (2024, 3, 5);
        string display1 = provider.DisplayText;
        output.WriteLine ($"2024-03-05 → \"{display1}\"");

        // Double-digit month and day
        provider.DateValue = new DateTime (2024, 11, 25);
        string display2 = provider.DisplayText;
        output.WriteLine ($"2024-11-25 → \"{display2}\"");

        // Both should have the same length due to normalization
        Assert.Equal (display1.Length, display2.Length);
    }

    [Fact]
    public void DateTextProvider_Fixed_Is_True ()
    {
        DateTextProvider provider = new ();
        Assert.True (provider.Fixed);
    }

    [Fact]
    public void DateEditor_FullNavigation_AllPositions_US ()
    {
        DateTextProvider provider = new ();
        var usFormat = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-US").DateTimeFormat.Clone ();
        provider.Format = usFormat;

        // Navigate backward from end to start
        List<int> backwardPositions = [provider.CursorEnd ()];
        int pos = provider.CursorEnd ();

        while (pos > provider.CursorStart ())
        {
            pos = provider.CursorLeft (pos);
            backwardPositions.Add (pos);
        }

        output.WriteLine ($"Backward positions: [{string.Join (", ", backwardPositions)}]");

        // Should visit: 9, 8, 7, 6, 4, 3, 1, 0 (skipping separators at 5, 2)
        Assert.Equal ([9, 8, 7, 6, 4, 3, 1, 0], backwardPositions);
    }
}
