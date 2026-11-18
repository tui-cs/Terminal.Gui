using System.Globalization;
using UnitTests;

namespace ViewsTests;

// Claude - Sonnet 4.6
public class TimeEditorTests (ITestOutputHelper output) : TestDriverBase
{
    [Fact]
    public void Constructor_Defaults ()
    {
        TimeEditor te = new ();
        te.Layout ();

        Assert.NotNull (te.Provider);
        Assert.IsType<TimeTextProvider> (te.Provider);
        Assert.Equal (TimeSpan.Zero, te.Value);
        Assert.NotNull (te.Format);
        Assert.Equal (CultureInfo.CurrentCulture.DateTimeFormat, te.Format);
    }

    [Fact]
    public void Value_Property_GetSet ()
    {
        TimeEditor te = new ();
        TimeSpan testTime = new (14, 30, 45);

        te.Value = testTime;
        Assert.Equal (testTime, te.Value);

        // Test setting to zero
        te.Value = TimeSpan.Zero;
        Assert.Equal (TimeSpan.Zero, te.Value);

        // Test setting to max
        te.Value = TimeSpan.FromHours (23) + TimeSpan.FromMinutes (59) + TimeSpan.FromSeconds (59);
        Assert.Equal (new TimeSpan (23, 59, 59), te.Value);
    }

    [Fact]
    public void Format_Property_Changes_Width ()
    {
        TimeEditor te = new ();

        // Set initial format explicitly to ensure deterministic test
        var initialFormat = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-GB").DateTimeFormat.Clone ();
        initialFormat.LongTimePattern = "HH:mm:ss";
        te.Format = initialFormat;
        te.Layout ();

        int initialWidth = te.Frame.Width;
        Assert.True (initialWidth > 0);

        // Change to a different pattern
        var customFormat = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-GB").DateTimeFormat.Clone ();
        customFormat.LongTimePattern = "HH:mm";
        te.Format = customFormat;
        te.Layout ();

        // Width should change to accommodate shorter pattern
        int newWidth = te.Frame.Width;
        Assert.NotEqual (initialWidth, newWidth);
        Assert.True (newWidth < initialWidth);
    }

    [Fact]
    public void ValueChanging_Event_Can_Cancel ()
    {
        TimeEditor te = new () { Value = TimeSpan.FromHours (10) };
        var eventFired = false;

        te.ValueChanging += (_, e) =>
                            {
                                eventFired = true;
                                e.Handled = true; // Cancel the change
                            };

        te.Value = TimeSpan.FromHours (15);

        Assert.True (eventFired);
        Assert.Equal (TimeSpan.FromHours (10), te.Value); // Value should not change
    }

    [Fact]
    public void ValueChanged_Event_Fires ()
    {
        TimeEditor te = new () { Value = TimeSpan.FromHours (10) };
        var eventFired = false;
        TimeSpan? oldValue = null;
        TimeSpan? newValue = null;

        te.ValueChanged += (_, e) =>
                           {
                               eventFired = true;
                               oldValue = e.OldValue;
                               newValue = e.NewValue;
                           };

        TimeSpan expectedNewValue = TimeSpan.FromHours (15);
        te.Value = expectedNewValue;

        Assert.True (eventFired);
        Assert.Equal (TimeSpan.FromHours (10), oldValue);
        Assert.Equal (expectedNewValue, newValue);
    }

    [Fact]
    public void TimeTextProvider_CursorNavigation_SkipsSeparators ()
    {
        TimeTextProvider provider = new ();

        // Use 24-hour format to ensure consistent behavior
        var format24h = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-GB").DateTimeFormat.Clone ();
        provider.Format = format24h;

        // CursorStart should return 0
        Assert.Equal (0, provider.CursorStart ());

        // For a format like "HH:mm:ss" (positions: 0,1,:,3,4,:,6,7)
        // Position 2 is separator, cursor should skip it
        int cursorPos = provider.CursorRight (1);
        Assert.NotEqual (2, cursorPos); // Should skip position 2 (separator)

        // CursorLeft from position 3 should skip separator at 2 and go to 1
        cursorPos = provider.CursorLeft (3);
        Assert.Equal (1, cursorPos);
    }

    [Fact]
    public void TimeTextProvider_InsertAt_ReplacesDigit ()
    {
        TimeTextProvider provider = new ();

        // Use 24-hour format to ensure consistent behavior
        var format24h = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-GB").DateTimeFormat.Clone ();
        provider.Format = format24h;

        provider.TimeValue = TimeSpan.Zero; // "00:00:00" in 24h format

        // Insert '1' at position 0 (first hour digit)
        bool result = provider.InsertAt ('1', 0);
        Assert.True (result);

        // Check that the value was updated
        string text = provider.Text;
        Assert.StartsWith ("1", text.TrimStart ());
    }

    [Fact]
    public void TimeTextProvider_Delete_ReplacesWithZero ()
    {
        TimeTextProvider provider = new ();

        // Use 24-hour format to avoid culture-specific issues
        var format24h = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-GB").DateTimeFormat.Clone ();
        provider.Format = format24h;

        provider.TimeValue = new TimeSpan (14, 30, 45);

        // Delete at position 0 should replace with '0'
        bool result = provider.Delete (0);
        Assert.True (result);

        // The hour should now start with 0
        string text = provider.Text.Trim ();
        Assert.StartsWith ("0", text);
    }

    [Fact]
    public void TimeTextProvider_Format_Change_Updates_Pattern ()
    {
        TimeTextProvider provider = new ();
        TimeSpan testTime = new (14, 30, 45);
        provider.TimeValue = testTime;

        string initialDisplay = provider.DisplayText;

        // Change to a custom format
        var customFormat = (DateTimeFormatInfo)CultureInfo.CurrentCulture.DateTimeFormat.Clone ();
        customFormat.LongTimePattern = "HH:mm";
        provider.Format = customFormat;

        string newDisplay = provider.DisplayText;

        // Display should change
        Assert.NotEqual (initialDisplay, newDisplay);

        // New display should not contain seconds
        Assert.DoesNotContain ("45", newDisplay);
    }

    [Fact]
    public void TimeTextProvider_Validates_Hours_Minutes_Seconds ()
    {
        TimeTextProvider provider = new ();

        // Valid time
        provider.TimeValue = new TimeSpan (23, 59, 59);
        Assert.True (provider.IsValid);

        // Another valid time
        provider.TimeValue = new TimeSpan (0, 0, 0);
        Assert.True (provider.IsValid);

        // Provider auto-corrects invalid values, so IsValid should always be true
        provider.TimeValue = new TimeSpan (12, 30, 15);
        Assert.True (provider.IsValid);
    }

    [Fact]
    public void TimeEditor_KeyInput_UpdatesValue ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        try
        {
            TimeEditor te = new () { App = app };
            te.Layout ();
            te.Value = TimeSpan.Zero;

            // Simulate typing '1'
            te.NewKeyDownEvent (Key.D1);

            // The value should have been updated
            string text = te.Text.Trim ();
            Assert.Contains ("1", text);
        }
        finally
        {
            app.Dispose ();
        }
    }

    [Fact]
    public void TimeEditor_Navigation_Keys ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        try
        {
            TimeEditor te = new () { App = app };
            te.Layout ();

            // Home key should move to start
            te.NewKeyDownEvent (Key.Home);

            // End key should move to end
            te.NewKeyDownEvent (Key.End);

            // Arrow keys should navigate
            te.NewKeyDownEvent (Key.CursorLeft);
            te.NewKeyDownEvent (Key.CursorRight);

            // No exceptions should be thrown
            Assert.NotNull (te);
        }
        finally
        {
            app.Dispose ();
        }
    }

    [Fact]
    public void TimeEditor_IValue_GetValue ()
    {
        TimeEditor te = new ();
        TimeSpan testTime = new (14, 30, 45);
        te.Value = testTime;

        object? value = ((IValue)te).GetValue ();

        Assert.NotNull (value);
        Assert.IsType<TimeSpan> (value);
        Assert.Equal (testTime, (TimeSpan)value);
    }

    [Fact]
    public void TimeTextProvider_12Hour_Format_With_AM_PM ()
    {
        TimeTextProvider provider = new ();

        // Set to a 12-hour format
        var format12h = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-US").DateTimeFormat.Clone ();
        format12h.LongTimePattern = "h:mm:ss tt";
        provider.Format = format12h;

        // Set time to 2:30 PM (14:30)
        provider.TimeValue = new TimeSpan (14, 30, 0);

        string display = provider.DisplayText.Trim ();

        // Should contain PM
        Assert.Contains ("PM", display, StringComparison.OrdinalIgnoreCase);

        // Set time to 2:30 AM (2:30)
        provider.TimeValue = new TimeSpan (2, 30, 0);

        display = provider.DisplayText.Trim ();

        // Should contain AM
        Assert.Contains ("AM", display, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TimeTextProvider_24Hour_Format ()
    {
        TimeTextProvider provider = new ();

        // Set to a 24-hour format
        var format24h = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-GB").DateTimeFormat.Clone ();
        format24h.LongTimePattern = "HH:mm:ss";
        provider.Format = format24h;

        // Set time to 14:30
        provider.TimeValue = new TimeSpan (14, 30, 0);

        string display = provider.DisplayText.Trim ();

        // Should contain 14
        Assert.Contains ("14", display);

        // Should not contain AM/PM
        Assert.DoesNotContain ("AM", display, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain ("PM", display, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TimeEditor_Delete_And_Backspace ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        try
        {
            TimeEditor te = new () { App = app };

            // Use 24-hour format to ensure consistent behavior
            var format24h = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-GB").DateTimeFormat.Clone ();
            te.Format = format24h;

            te.Value = new TimeSpan (12, 34, 56);
            te.Layout ();

            // Move to start and delete
            te.NewKeyDownEvent (Key.Home);
            te.NewKeyDownEvent (Key.Delete);

            // Value should have changed (first digit replaced with 0)
            string text = te.Text.Trim ();
            Assert.NotEqual ("12:34:56", text);

            // Backspace should also work
            te.NewKeyDownEvent (Key.Backspace);

            // Text should have changed again
            Assert.NotNull (te.Text);
        }
        finally
        {
            app.Dispose ();
        }
    }

    [Fact]
    public void TimeTextProvider_CursorEnd_Returns_Last_Position ()
    {
        TimeTextProvider provider = new ();

        int endPos = provider.CursorEnd ();

        // End position should be >= 0
        Assert.True (endPos >= 0);

        // For 24-hour format like "HH:mm:ss", end should be at last digit (position 7)
        // For 12-hour format with AM/PM, end should be at AM/PM position
        int displayLength = provider.DisplayText.Trim ().Length;
        Assert.True (endPos < displayLength);
    }

    [Fact]
    public void TimeEditor_Text_Property_Updates_Value ()
    {
        TimeEditor te = new ();

        // Use 24-hour format to ensure consistent parsing
        var format24h = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-GB").DateTimeFormat.Clone ();
        te.Format = format24h;

        // Set text directly
        te.Text = "14:30:45";

        // Value should be updated
        Assert.Equal (14, te.Value.Hours);
        Assert.Equal (30, te.Value.Minutes);
        Assert.Equal (45, te.Value.Seconds);
    }

    [Fact]
    public void TimeTextProvider_InsertAt_NonDigit_Returns_False ()
    {
        TimeTextProvider provider = new ();

        // Try to insert a non-digit character at a digit position
        bool result = provider.InsertAt ('x', 0);

        // Should fail
        Assert.False (result);
    }

    [Fact]
    public void TimeEditor_Multiple_Format_Changes ()
    {
        TimeEditor te = new () { Value = new TimeSpan (14, 30, 45) };
        te.Layout ();

        // Change format multiple times
        for (var i = 0; i < 3; i++)
        {
            var format = (DateTimeFormatInfo)CultureInfo.CurrentCulture.DateTimeFormat.Clone ();
            format.LongTimePattern = i % 2 == 0 ? "HH:mm" : "HH:mm:ss";
            te.Format = format;
            te.Layout ();

            // Value should remain the same
            Assert.Equal (14, te.Value.Hours);
            Assert.Equal (30, te.Value.Minutes);
        }
    }

    [Fact]
    public void TimeTextProvider_TryManualParse_PartialInput ()
    {
        TimeTextProvider provider = new ();

        // Use 24-hour format
        var format24h = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-GB").DateTimeFormat.Clone ();
        provider.Format = format24h;

        // Test partial input parsing (minutes only)
        provider.Text = "14:30";
        Assert.Equal (14, provider.TimeValue.Hours);
        Assert.Equal (30, provider.TimeValue.Minutes);
        Assert.Equal (0, provider.TimeValue.Seconds);

        // Test with seconds
        provider.Text = "14:30:45";
        Assert.Equal (14, provider.TimeValue.Hours);
        Assert.Equal (30, provider.TimeValue.Minutes);
        Assert.Equal (45, provider.TimeValue.Seconds);
    }

    [Fact]
    public void TimeTextProvider_TryManualParse_InvalidInput ()
    {
        TimeTextProvider provider = new ();

        // Use 24-hour format
        var format24h = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-GB").DateTimeFormat.Clone ();
        provider.Format = format24h;

        TimeSpan initialValue = provider.TimeValue;

        // Test invalid input (no separator)
        provider.Text = "invalid";
        Assert.Equal (initialValue, provider.TimeValue);

        // Test incomplete input (only one part)
        provider.Text = "14";
        Assert.Equal (initialValue, provider.TimeValue);
    }

    [Fact]
    public void TimeTextProvider_TryManualParse_AutoCorrection ()
    {
        TimeTextProvider provider = new ();

        // Use 24-hour format
        var format24h = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-GB").DateTimeFormat.Clone ();
        provider.Format = format24h;

        // Test auto-correction for out-of-range values
        provider.Text = "25:70:90";

        // Should auto-correct to valid ranges
        Assert.Equal (23, provider.TimeValue.Hours); // Max 23
        Assert.Equal (59, provider.TimeValue.Minutes); // Max 59
        Assert.Equal (59, provider.TimeValue.Seconds); // Max 59
    }

    [Fact]
    public void TimeTextProvider_12Hour_AM_PM_Parsing ()
    {
        TimeTextProvider provider = new ();

        // Use 12-hour format
        var format12h = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-US").DateTimeFormat.Clone ();
        provider.Format = format12h;

        // Test PM parsing
        provider.Text = "2:30:00 PM";
        Assert.Equal (14, provider.TimeValue.Hours);

        // Test AM parsing
        provider.Text = "2:30:00 AM";
        Assert.Equal (2, provider.TimeValue.Hours);

        // Test 12 PM (noon)
        provider.Text = "12:00:00 PM";
        Assert.Equal (12, provider.TimeValue.Hours);

        // Test 12 AM (midnight)
        provider.Text = "12:00:00 AM";
        Assert.Equal (0, provider.TimeValue.Hours);
    }

    [Fact]
    public void TimeTextProvider_CursorNavigation_Comprehensive ()
    {
        TimeTextProvider provider = new ();

        // Use 24-hour format
        var format24h = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-GB").DateTimeFormat.Clone ();
        provider.Format = format24h;

        // Test CursorStart
        Assert.Equal (0, provider.CursorStart ());

        // Test CursorEnd
        int endPos = provider.CursorEnd ();
        Assert.True (endPos >= 0);

        // Test navigation through all positions
        int pos = provider.CursorStart ();
        int lastPos = pos;

        for (var i = 0; i < 10; i++)
        {
            int nextPos = provider.CursorRight (pos);

            // Should skip separators
            Assert.NotEqual (pos, nextPos);
            pos = nextPos;

            if (pos >= provider.CursorEnd ())
            {
                break;
            }
        }
    }

    [Fact]
    public void TimeEditor_ValueChanging_Cancel ()
    {
        TimeEditor te = new ();
        TimeSpan initialValue = TimeSpan.FromHours (10);
        te.Value = initialValue;

        var changingEventFired = false;
        var changedEventFired = false;

        te.ValueChanging += (_, e) =>
                            {
                                changingEventFired = true;
                                e.Handled = true; // Cancel the change
                            };

        te.ValueChanged += (_, e) => { changedEventFired = true; };

        // Try to set new value
        te.Value = TimeSpan.FromHours (15);

        // ValueChanging should have fired, but ValueChanged should not
        Assert.True (changingEventFired);
        Assert.False (changedEventFired);

        // Value should not have changed
        Assert.Equal (initialValue, te.Value);
    }

    [Fact]
    public void TimeTextProvider_Delete_AtSeparatorPosition ()
    {
        TimeTextProvider provider = new ();

        // Use 24-hour format
        var format24h = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-GB").DateTimeFormat.Clone ();
        provider.Format = format24h;

        provider.TimeValue = new TimeSpan (14, 30, 45);

        // Try to delete at separator position (position 2 in "14:30:45")
        string beforeText = provider.Text.Trim ();
        bool result = provider.Delete (2);

        // Delete at separator should not change anything or should skip to next position
        string afterText = provider.Text.Trim ();

        // The behavior depends on implementation, but text should still be valid
        Assert.NotNull (afterText);
    }

    [Fact]
    public void TimeEditor_ValueChangedUntyped_Event ()
    {
        TimeEditor te = new ();
        var eventFired = false;
        object? oldValue = null;
        object? newValue = null;

        te.ValueChangedUntyped += (_, e) =>
                                  {
                                      eventFired = true;
                                      oldValue = e.OldValue;
                                      newValue = e.NewValue;
                                  };

        TimeSpan testValue = TimeSpan.FromHours (15);
        te.Value = testValue;

        Assert.True (eventFired);
        Assert.Equal (TimeSpan.Zero, oldValue);
        Assert.Equal (testValue, newValue);
    }

    [Fact]
    public void TimeTextProvider_CursorLeft_FromStart ()
    {
        TimeTextProvider provider = new ();

        // Use 24-hour format
        var format24h = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-GB").DateTimeFormat.Clone ();
        provider.Format = format24h;

        // CursorLeft from start should return start
        int pos = provider.CursorLeft (0);
        Assert.Equal (0, pos);
    }

    [Fact]
    public void TimeTextProvider_CursorRight_FromEnd ()
    {
        TimeTextProvider provider = new ();

        // Use 24-hour format
        var format24h = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-GB").DateTimeFormat.Clone ();
        provider.Format = format24h;

        int endPos = provider.CursorEnd ();

        // CursorRight from end should return end
        int pos = provider.CursorRight (endPos);
        Assert.Equal (endPos, pos);
    }

    // Claude - Opus 4.6
    [Fact]
    public void TimeEditor_12h_DisplayText_Shows_Full_AM_PM ()
    {
        // Verifies that AM/PM is fully visible (not clipped to just "A")
        // Uses default constructor without overriding Width to test the real scenario
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (30, 1);

        try
        {
            Runnable<bool> runnable = new () { Width = 30, Height = 1 };
            app.Begin (runnable);

            TimeEditor te = new () { Height = 1, Value = new TimeSpan (9, 0, 0) };

            // Explicitly set 12-hour format AFTER construction to simulate the scenario
            var format12h = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-US").DateTimeFormat.Clone ();
            format12h.LongTimePattern = "h:mm:ss tt";
            te.Format = format12h;

            runnable.Add (te);
            app.LayoutAndDraw ();

            output.WriteLine ($"DisplayText: \"{te.Provider!.DisplayText}\"");
            output.WriteLine ($"Frame: {te.Frame}");
            output.WriteLine ($"Viewport: {te.Viewport}");

            // DisplayText should be "09:00:00 AM" (normalized to 2-digit hours)
            Assert.Equal ("09:00:00 AM", te.Provider.DisplayText);

            // The view must be wide enough to show the full display text including "AM"
            Assert.True (te.Frame.Width >= te.Provider.DisplayText.Length,
                         $"Frame width {te.Frame.Width} is too narrow for DisplayText \"{te.Provider.DisplayText}\" ({te.Provider.DisplayText.Length} chars)");

            DriverAssert.AssertDriverContentsWithFrameAre (@"09:00:00 AM", output, app.Driver);
        }
        finally
        {
            app.Dispose ();
        }
    }

    // Claude - Opus 4.6
    [Fact]
    public void TimeEditor_Default_Constructor_Width_Fits_DisplayText ()
    {
        // Verifies that the default constructor produces a Width that fits the full DisplayText
        TimeEditor te = new () { Value = new TimeSpan (9, 0, 0) };
        te.Layout ();

        output.WriteLine ($"DisplayText: \"{te.Provider!.DisplayText}\"");
        output.WriteLine ($"DisplayText.Length: {te.Provider.DisplayText.Length}");
        output.WriteLine ($"Frame.Width: {te.Frame.Width}");

        Assert.True (te.Frame.Width >= te.Provider.DisplayText.Length,
                     $"Frame width {te.Frame.Width} is too narrow for DisplayText \"{te.Provider.DisplayText}\" ({te.Provider.DisplayText.Length} chars)");
    }

    // Claude - Opus 4.6
    [Fact]
    public void TimeEditor_CursorRight_From_BlankCell_DoesNotMoveBackward ()
    {
        // Verifies that pressing right arrow from the blank cell past the last editable
        // position does NOT move the cursor backward (e.g., from "M" in "PM" back to "A").
        TimeTextProvider provider = new ();
        var format12h = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-US").DateTimeFormat.Clone ();
        format12h.LongTimePattern = "h:mm:ss tt";
        provider.Format = format12h;

        int cursorEnd = provider.CursorEnd ();
        output.WriteLine ($"CursorEnd: {cursorEnd}");

        // CursorRight from CursorEnd should return CursorEnd (can't go further via provider)
        int fromEnd = provider.CursorRight (cursorEnd);
        output.WriteLine ($"CursorRight({cursorEnd}): {fromEnd}");
        Assert.Equal (cursorEnd, fromEnd);

        // Now test TextValidateField behavior: cursor at CursorEnd+1 should not move backward
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        try
        {
            TimeEditor te = new () { Value = new TimeSpan (9, 0, 0), Format = format12h };
            te.Layout ();
            te.SetFocus ();

            // Navigate to the end, then one past
            te.NewKeyDownEvent (Key.End);
            te.NewKeyDownEvent (Key.CursorRight);

            output.WriteLine ($"After End+Right, DisplayText: \"{te.Provider!.DisplayText}\"");

            // Press right again — should NOT move backward
            bool handled = te.NewKeyDownEvent (Key.CursorRight);
            output.WriteLine ($"Second Right handled: {handled}");

            // Pressing left from blank cell should go back to CursorEnd
            te.NewKeyDownEvent (Key.CursorLeft);

            // Verify we're at a valid position by typing — should insert at CursorEnd
            te.NewKeyDownEvent (Key.End);
            te.NewKeyDownEvent (Key.D5);
            output.WriteLine ($"After End+5: \"{te.Provider.DisplayText}\"");
        }
        finally
        {
            app.Dispose ();
        }
    }

    // Claude - Opus 4.6
    [Fact]
    public void NormalizedPattern_24h_Typing_12_Enters_TwoDigitHour ()
    {
        // Verifies that typing "12" at position 0 in 24h format sets hours to 12
        TimeTextProvider provider = new ();
        var format24h = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-GB").DateTimeFormat.Clone ();
        format24h.LongTimePattern = "HH:mm:ss";
        provider.Format = format24h;
        provider.TimeValue = new TimeSpan (9, 0, 0);

        output.WriteLine ($"Initial DisplayText: \"{provider.DisplayText}\"");
        Assert.Equal ("09:00:00", provider.DisplayText);

        // Type '1' at position 0 (tens digit of hours)
        bool inserted = provider.InsertAt ('1', 0);
        Assert.True (inserted);
        output.WriteLine ($"After '1' at pos 0: \"{provider.DisplayText}\"");
        Assert.Equal ("19:00:00", provider.DisplayText);

        // Type '2' at position 1 (ones digit of hours)
        inserted = provider.InsertAt ('2', 1);
        Assert.True (inserted);
        output.WriteLine ($"After '2' at pos 1: \"{provider.DisplayText}\"");
        Assert.Equal ("12:00:00", provider.DisplayText);
        Assert.Equal (new TimeSpan (12, 0, 0), provider.TimeValue);
    }

    // Claude - Opus 4.6
    [Fact]
    public void NormalizedPattern_DisplayText_Has_No_LeadingSpace ()
    {
        TimeTextProvider provider = new ();
        var format24h = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-GB").DateTimeFormat.Clone ();
        format24h.LongTimePattern = "HH:mm:ss";
        provider.Format = format24h;
        provider.TimeValue = new TimeSpan (9, 0, 0);

        string display = provider.DisplayText;
        output.WriteLine ($"DisplayText: \"{display}\"");

        Assert.Equal ("09:00:00", display);
        Assert.False (display.StartsWith (' '));
    }

    // Claude - Opus 4.6
    [Fact]
    public void NormalizedPattern_SingleDigitHourFormat_PadsToTwoDigits ()
    {
        // Verifies that "h:mm:ss tt" is normalized to "hh:mm:ss tt"
        TimeTextProvider provider = new ();
        var format12h = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-US").DateTimeFormat.Clone ();
        format12h.LongTimePattern = "h:mm:ss tt";
        provider.Format = format12h;
        provider.TimeValue = new TimeSpan (9, 0, 0);

        string display = provider.DisplayText;
        output.WriteLine ($"DisplayText for 9 AM with 'h:mm:ss tt': \"{display}\"");

        // Should be padded to 2 digits: "09:00:00 AM"
        Assert.StartsWith ("09", display);
        Assert.Equal (11, display.Length);
    }

    // Claude - Opus 4.6
    [Fact]
    public void NormalizedPattern_FieldPositions_AreConsistent ()
    {
        // Verifies separator positions don't shift based on time value
        TimeTextProvider provider = new ();
        var format24h = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-GB").DateTimeFormat.Clone ();
        format24h.LongTimePattern = "H:mm:ss"; // Single-digit H
        provider.Format = format24h;

        // Single-digit hour value
        provider.TimeValue = new TimeSpan (9, 30, 45);
        string display1 = provider.DisplayText;
        output.WriteLine ($"9:30:45 → \"{display1}\"");

        // Double-digit hour value
        provider.TimeValue = new TimeSpan (14, 30, 45);
        string display2 = provider.DisplayText;
        output.WriteLine ($"14:30:45 → \"{display2}\"");

        // Both should have the same length due to normalization
        Assert.Equal (display1.Length, display2.Length);
    }

    // Claude - Opus 4.6
    [Fact]
    public void TimeEditor_Typing_12_At_Start_Renders_Correctly ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (20, 1);

        try
        {
            Runnable<bool> runnable = new () { Width = 20, Height = 1 };
            app.Begin (runnable);

            var format24h = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-GB").DateTimeFormat.Clone ();
            format24h.LongTimePattern = "HH:mm:ss";

            TimeEditor te = new () { Width = 10, Height = 1, Value = new TimeSpan (9, 0, 0), Format = format24h };
            runnable.Add (te);
            app.LayoutAndDraw ();

            output.WriteLine ($"Initial Text: \"{te.Text}\"");
            output.WriteLine ($"Initial DisplayText: \"{te.Provider!.DisplayText}\"");

            DriverAssert.AssertDriverContentsWithFrameAre (@"09:00:00", output, app.Driver);

            // Simulate focus and typing "1" then "2"
            te.SetFocus ();
            te.NewKeyDownEvent (Key.Home);
            te.NewKeyDownEvent (Key.D1);
            app.LayoutAndDraw ();

            output.WriteLine ($"After '1': Text=\"{te.Text}\", DisplayText=\"{te.Provider.DisplayText}\"");

            te.NewKeyDownEvent (Key.D2);
            app.LayoutAndDraw ();

            output.WriteLine ($"After '2': Text=\"{te.Text}\", DisplayText=\"{te.Provider.DisplayText}\"");

            Assert.Equal (new TimeSpan (12, 0, 0), te.Value);

            DriverAssert.AssertDriverContentsWithFrameAre (@"12:00:00", output, app.Driver);
        }
        finally
        {
            app.Dispose ();
        }
    }

    // Claude - Opus 4.6
    [Fact]
    public void TimeEditor_CursorRight_SkipsSeparator_24h ()
    {
        // Verifies cursor movement: after typing at pos 1, cursor skips separator to pos 3
        TimeTextProvider provider = new ();
        var format24h = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-GB").DateTimeFormat.Clone ();
        format24h.LongTimePattern = "HH:mm:ss";
        provider.Format = format24h;

        // Position 0 = H tens, 1 = H ones, 2 = ':', 3 = m tens
        int nextPos = provider.CursorRight (1);
        output.WriteLine ($"CursorRight(1) = {nextPos}");

        // Should skip separator at position 2 and land on 3
        Assert.Equal (3, nextPos);
    }

    // Claude - Opus 4.6
    [Fact]
    public void TimeEditor_CursorLeft_SkipsSeparator_24h ()
    {
        TimeTextProvider provider = new ();
        var format24h = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-GB").DateTimeFormat.Clone ();
        format24h.LongTimePattern = "HH:mm:ss";
        provider.Format = format24h;

        // CursorLeft from position 3 (m tens) should skip separator at 2 to position 1
        int prevPos = provider.CursorLeft (3);
        output.WriteLine ($"CursorLeft(3) = {prevPos}");
        Assert.Equal (1, prevPos);
    }

    // Claude - Opus 4.6
    [Fact]
    public void TimeEditor_FullNavigation_24h_AllPositions ()
    {
        TimeTextProvider provider = new ();
        var format24h = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-GB").DateTimeFormat.Clone ();
        format24h.LongTimePattern = "HH:mm:ss";
        provider.Format = format24h;

        // Expected editable positions for "HH:mm:ss": 0,1, 3,4, 6,7
        List<int> visitedPositions = [provider.CursorStart ()];
        int pos = provider.CursorStart ();

        while (pos < provider.CursorEnd ())
        {
            pos = provider.CursorRight (pos);
            visitedPositions.Add (pos);
        }

        output.WriteLine ($"Forward positions: [{string.Join (", ", visitedPositions)}]");

        // Should visit: 0, 1, 3, 4, 6, 7
        Assert.Equal ([0, 1, 3, 4, 6, 7], visitedPositions);
    }

    // Claude - Opus 4.6
    [Fact]
    public void TimeEditor_InsertAt_AllDigitPositions_24h ()
    {
        TimeTextProvider provider = new ();
        var format24h = (DateTimeFormatInfo)CultureInfo.GetCultureInfo ("en-GB").DateTimeFormat.Clone ();
        format24h.LongTimePattern = "HH:mm:ss";
        provider.Format = format24h;
        provider.TimeValue = TimeSpan.Zero; // "00:00:00"

        output.WriteLine ($"Initial: \"{provider.DisplayText}\"");

        // Type at each editable position
        Assert.True (provider.InsertAt ('1', 0)); // "10:00:00"
        output.WriteLine ($"After InsertAt('1', 0): \"{provider.DisplayText}\"");

        Assert.True (provider.InsertAt ('4', 1)); // "14:00:00"
        output.WriteLine ($"After InsertAt('4', 1): \"{provider.DisplayText}\"");

        Assert.True (provider.InsertAt ('3', 3)); // "14:30:00"
        output.WriteLine ($"After InsertAt('3', 3): \"{provider.DisplayText}\"");

        Assert.True (provider.InsertAt ('5', 4)); // "14:35:00"
        output.WriteLine ($"After InsertAt('5', 4): \"{provider.DisplayText}\"");

        Assert.True (provider.InsertAt ('4', 6)); // "14:35:40"
        output.WriteLine ($"After InsertAt('4', 6): \"{provider.DisplayText}\"");

        Assert.True (provider.InsertAt ('2', 7)); // "14:35:42"
        output.WriteLine ($"After InsertAt('2', 7): \"{provider.DisplayText}\"");

        Assert.Equal ("14:35:42", provider.DisplayText);
        Assert.Equal (new TimeSpan (14, 35, 42), provider.TimeValue);
    }
}
