// ReSharper disable AccessToModifiedClosure
#pragma warning disable CS9113 // Parameter is unread

namespace DriverTests.MouseTests;

/// <summary>
///     Extended unit tests for <see cref="MouseInterpreter"/> click detection and event generation.
///     Complements existing MouseInterpreterTests with additional coverage for HIGH and MEDIUM priority scenarios.
/// </summary>
[Collection ("Driver Tests")]
[Trait ("Category", "Input")]
public class MouseInterpreterExtendedTests (ITestOutputHelper output)
{
    #region Position Change Tests

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void Process_ClickAtDifferentPosition_ResetsClickCount ()
    {
        // Arrange
        DateTime currentTime = DateTime.Now;
        MouseInterpreter interpreter = new (new FuncTimeProvider (() => currentTime), TimeSpan.FromMilliseconds (500));

        Mouse press1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed };
        Mouse release1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased };
        Mouse press2 = new () { ScreenPosition = new (20, 20), Flags = MouseFlags.LeftButtonPressed }; // Different position
        Mouse release2 = new () { ScreenPosition = new (20, 20), Flags = MouseFlags.LeftButtonReleased };

        // Act
        _ = interpreter.Process (press1).ToList (); // Discard - just need to process the press
        currentTime = currentTime.AddMilliseconds (50);
        List<Mouse> events2 = interpreter.Process (release1).ToList ();
        currentTime = currentTime.AddMilliseconds (50);
        _ = interpreter.Process (press2).ToList (); // Discard - just need to process the press
        currentTime = currentTime.AddMilliseconds (50);
        List<Mouse> events4 = interpreter.Process (release2).ToList ();

        // Assert
        Assert.Equal (2, events2.Count); // Original release + Button1Clicked
        Assert.Contains (events2, e => e.Flags == MouseFlags.LeftButtonClicked);

        Assert.Equal (2, events4.Count); // Original release + Button1Clicked (not double-click due to position change)
        Assert.Contains (events4, e => e.Flags == MouseFlags.LeftButtonClicked);
        Assert.DoesNotContain (events4, e => e.Flags == MouseFlags.LeftButtonDoubleClicked);
    }

    #endregion

    #region Multiple Button Tests

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void Process_Button1And2PressedSimultaneously_TracksIndependently ()
    {
        // Arrange
        DateTime currentTime = DateTime.Now;
        MouseInterpreter interpreter = new (new FuncTimeProvider (() => currentTime), TimeSpan.FromMilliseconds (500));

        Mouse press1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed };
        Mouse press2 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.MiddleButtonPressed };
        Mouse release1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased };
        Mouse release2 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.MiddleButtonReleased };

        // Act
        List<Mouse> events1 = interpreter.Process (press1).ToList ();
        currentTime = currentTime.AddMilliseconds (50);
        List<Mouse> events2 = interpreter.Process (press2).ToList ();
        currentTime = currentTime.AddMilliseconds (50);
        List<Mouse> events3 = interpreter.Process (release1).ToList ();
        currentTime = currentTime.AddMilliseconds (50);
        List<Mouse> events4 = interpreter.Process (release2).ToList ();

        // Assert
        Assert.Single (events1); // Just Button1Pressed
        Assert.Equal (MouseFlags.LeftButtonPressed, events1 [0].Flags);

        // NOTE: This test demonstrates the quirk documented in MouseButtonClickTrackerTests:
        // When Button2 is pressed (Button1 not in flags), Button1's tracker sees: Pressed→Released
        // This generates a spurious Button1Clicked event
        Assert.Equal (2, events2.Count); // Button2Pressed + spurious Button1Clicked
        Assert.Contains (events2, e => e.Flags == MouseFlags.LeftButtonClicked); // Spurious click

        // When Button1 is actually released, Button1's tracker already thinks it's released (no change)
        // But Button2's tracker sees: Pressed→Released, generating Button2Clicked
        Assert.Equal (2, events3.Count); // Button1Released + spurious Button2Clicked
        Assert.Contains (events3, e => e.Flags == MouseFlags.MiddleButtonClicked);

        // Button2 release: both trackers already think their buttons are released
        Assert.Single (events4); // Just Button2Released
    }

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void Process_MultipleButtonsDoubleClick_EachIndependent ()
    {
        // Arrange
        DateTime currentTime = DateTime.Now;
        MouseInterpreter interpreter = new (new FuncTimeProvider (() => currentTime), TimeSpan.FromMilliseconds (500));

        // Act - Double-click Button1, then double-click Button2
        List<Mouse> allEvents = [];

        // Button1 first click
        allEvents.AddRange (interpreter.Process (new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed }));
        currentTime = currentTime.AddMilliseconds (50);
        allEvents.AddRange (interpreter.Process (new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased }));

        // Button1 second click
        currentTime = currentTime.AddMilliseconds (50);
        allEvents.AddRange (interpreter.Process (new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed }));
        currentTime = currentTime.AddMilliseconds (50);
        allEvents.AddRange (interpreter.Process (new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased }));

        // Button2 first click
        currentTime = currentTime.AddMilliseconds (50);
        allEvents.AddRange (interpreter.Process (new () { ScreenPosition = new (10, 10), Flags = MouseFlags.MiddleButtonPressed }));
        currentTime = currentTime.AddMilliseconds (50);
        allEvents.AddRange (interpreter.Process (new () { ScreenPosition = new (10, 10), Flags = MouseFlags.MiddleButtonReleased }));

        // Button2 second click
        currentTime = currentTime.AddMilliseconds (50);
        allEvents.AddRange (interpreter.Process (new () { ScreenPosition = new (10, 10), Flags = MouseFlags.MiddleButtonPressed }));
        currentTime = currentTime.AddMilliseconds (50);
        allEvents.AddRange (interpreter.Process (new () { ScreenPosition = new (10, 10), Flags = MouseFlags.MiddleButtonReleased }));

        // Assert
        Assert.Contains (allEvents, e => e.Flags == MouseFlags.LeftButtonDoubleClicked);
        Assert.Contains (allEvents, e => e.Flags == MouseFlags.MiddleButtonDoubleClicked);
    }

    #endregion

    #region Edge Case Tests

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void Process_ReleaseWithoutPress_DoesNotGenerateClick ()
    {
        // Arrange
        DateTime currentTime = DateTime.Now;
        MouseInterpreter interpreter = new (new FuncTimeProvider (() => currentTime), TimeSpan.FromMilliseconds (500));

        Mouse release = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased };

        // Act
        List<Mouse> events = interpreter.Process (release).ToList ();

        // Assert
        Assert.Single (events); // Only the original release event
        Assert.Equal (MouseFlags.LeftButtonReleased, events [0].Flags);
        Assert.DoesNotContain (events, e => e.Flags == MouseFlags.LeftButtonClicked);
    }

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void Process_DoublePress_WithoutIntermediateRelease_DoesNotCountAsDoubleClick ()
    {
        // Arrange
        DateTime currentTime = DateTime.Now;
        MouseInterpreter interpreter = new (new FuncTimeProvider (() => currentTime), TimeSpan.FromMilliseconds (500));

        Mouse press1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed };
        Mouse press2 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed };
        Mouse release = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased };

        // Act
        List<Mouse> events1 = interpreter.Process (press1).ToList ();
        currentTime = currentTime.AddMilliseconds (50);
        List<Mouse> events2 = interpreter.Process (press2).ToList ();
        currentTime = currentTime.AddMilliseconds (50);
        List<Mouse> events3 = interpreter.Process (release).ToList ();

        // Assert
        Assert.Single (events1); // Just Button1Pressed
        Assert.Single (events2); // Just Button1Pressed (no state change)
        Assert.Equal (2, events3.Count); // Button1Released + Button1Clicked (single, not double)
        Assert.Contains (events3, e => e.Flags == MouseFlags.LeftButtonClicked);
        Assert.DoesNotContain (events3, e => e.Flags == MouseFlags.LeftButtonDoubleClicked);
    }

    #endregion

    #region Modifier Key Tests

    // CoPilot: claude-3-7-sonnet-20250219
    // Claude - Opus 4.5 - Updated to verify modifiers ARE preserved
    [Theory]
    [InlineData (MouseFlags.Shift)]
    [InlineData (MouseFlags.Ctrl)]
    [InlineData (MouseFlags.Alt)]
    [InlineData (MouseFlags.Shift | MouseFlags.Ctrl)]
    public void Process_ClickWithModifier_PreservesModifier (MouseFlags modifier)
    {
        // Arrange
        DateTime currentTime = DateTime.Now;
        MouseInterpreter interpreter = new (new FuncTimeProvider (() => currentTime), TimeSpan.FromMilliseconds (500));

        Mouse press = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed | modifier };
        Mouse release = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased | modifier };

        // Act
        _ = interpreter.Process (press).ToList (); // Discard - just need to process the press
        currentTime = currentTime.AddMilliseconds (50);
        List<Mouse> events2 = interpreter.Process (release).ToList ();

        // Assert
        Assert.Equal (2, events2.Count); // Release + Clicked
        Mouse? clickEvent = events2.FirstOrDefault (e => e.Flags.HasFlag (MouseFlags.LeftButtonClicked));
        Assert.NotNull (clickEvent);

        // Verify that modifier flags are preserved in the synthetic click event
        Assert.True (clickEvent.Flags.HasFlag (modifier),
                     $"Synthetic click events should preserve {modifier} modifier. "
                     + "CreateClickEvent in MouseInterpreter now ORs modifier flags from the original event.");
    }

    // CoPilot: claude-3-7-sonnet-20250219
    // Claude - Opus 4.5 - Updated to verify modifiers ARE preserved
    [Fact]
    public void Process_DoubleClickWithShift_PreservesModifier ()
    {
        // Arrange
        DateTime currentTime = DateTime.Now;
        MouseInterpreter interpreter = new (new FuncTimeProvider (() => currentTime), TimeSpan.FromMilliseconds (500));

        MouseFlags modifiedPressed = MouseFlags.LeftButtonPressed | MouseFlags.Shift;
        MouseFlags modifiedReleased = MouseFlags.LeftButtonReleased | MouseFlags.Shift;

        // Act - Double-click with Shift held
        List<Mouse> allEvents = [];
        allEvents.AddRange (interpreter.Process (new () { ScreenPosition = new (10, 10), Flags = modifiedPressed }));
        currentTime = currentTime.AddMilliseconds (50);
        allEvents.AddRange (interpreter.Process (new () { ScreenPosition = new (10, 10), Flags = modifiedReleased }));
        currentTime = currentTime.AddMilliseconds (50);
        allEvents.AddRange (interpreter.Process (new () { ScreenPosition = new (10, 10), Flags = modifiedPressed }));
        currentTime = currentTime.AddMilliseconds (50);
        allEvents.AddRange (interpreter.Process (new () { ScreenPosition = new (10, 10), Flags = modifiedReleased }));

        // Assert
        Mouse? singleClick =
            allEvents.FirstOrDefault (e => e.Flags.HasFlag (MouseFlags.LeftButtonClicked) && !e.Flags.HasFlag (MouseFlags.LeftButtonDoubleClicked));
        Mouse? doubleClick = allEvents.FirstOrDefault (e => e.Flags.HasFlag (MouseFlags.LeftButtonDoubleClicked));

        Assert.NotNull (singleClick);
        Assert.NotNull (doubleClick);

        // Verify that modifier flags are preserved in both single and double click synthetic events
        Assert.True (singleClick.Flags.HasFlag (MouseFlags.Shift), "Single click synthetic event should preserve Shift modifier");

        Assert.True (doubleClick.Flags.HasFlag (MouseFlags.Shift), "Double click synthetic event should preserve Shift modifier");
    }

    #endregion

    #region Time Injection Tests

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void Process_WithInjectedTime_AllowsDeterministicTesting ()
    {
        // Arrange
        DateTime currentTime = new (2025, 1, 1, 12, 0, 0);
        MouseInterpreter interpreter = new (new FuncTimeProvider (() => currentTime), TimeSpan.FromMilliseconds (500));

        Mouse press = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed };
        Mouse release = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased };

        // Act - First click at T=0
        _ = interpreter.Process (press).ToList (); // Discard - just need to process the press
        currentTime = currentTime.AddMilliseconds (50);
        List<Mouse> events2 = interpreter.Process (release).ToList ();

        // Second click at T=600 (beyond 500ms threshold)
        currentTime = currentTime.AddMilliseconds (600);
        _ = interpreter.Process (press).ToList (); // Discard - just need to process the press
        currentTime = currentTime.AddMilliseconds (50);
        List<Mouse> events4 = interpreter.Process (release).ToList ();

        // Assert
        Assert.Contains (events2, e => e.Flags == MouseFlags.LeftButtonClicked);
        Assert.Contains (events4, e => e.Flags == MouseFlags.LeftButtonClicked); // Single click, not double
        Assert.DoesNotContain (events4, e => e.Flags == MouseFlags.LeftButtonDoubleClicked);
    }

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void Process_WithInjectedTime_ExactThresholdBoundary ()
    {
        // Arrange
        DateTime currentTime = new (2025, 1, 1, 12, 0, 0);
        TimeSpan threshold = TimeSpan.FromMilliseconds (500);
        MouseInterpreter interpreter = new (new FuncTimeProvider (() => currentTime), threshold);

        Mouse press = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed };
        Mouse release = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased };

        // Act - First click
        _ = interpreter.Process (press).ToList ();
        currentTime = currentTime.AddMilliseconds (50);
        _ = interpreter.Process (release).ToList ();

        // Second click exactly at threshold + 1ms
        currentTime = currentTime.Add (threshold).AddMilliseconds (1);
        _ = interpreter.Process (press).ToList ();
        currentTime = currentTime.AddMilliseconds (50);
        List<Mouse> events2 = interpreter.Process (release).ToList ();

        // Assert - Should be single click, not double (threshold exceeded)
        Assert.Contains (events2, e => e.Flags == MouseFlags.LeftButtonClicked);
        Assert.DoesNotContain (events2, e => e.Flags == MouseFlags.LeftButtonDoubleClicked);
    }

    #endregion

    #region Pass-Through Tests

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void Process_AlwaysYieldsOriginalEvent ()
    {
        // Arrange
        DateTime currentTime = DateTime.Now;
        MouseInterpreter interpreter = new (new FuncTimeProvider (() => currentTime), TimeSpan.FromMilliseconds (500));

        Mouse press = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed };
        Mouse release = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased };

        // Act
        List<Mouse> pressEvents = interpreter.Process (press).ToList ();
        currentTime = currentTime.AddMilliseconds (50);
        List<Mouse> releaseEvents = interpreter.Process (release).ToList ();

        // Assert - First event should always be the original
        Assert.True (pressEvents.Count >= 1);
        Assert.Equal (MouseFlags.LeftButtonPressed, pressEvents [0].Flags);

        Assert.True (releaseEvents.Count >= 1);
        Assert.Equal (MouseFlags.LeftButtonReleased, releaseEvents [0].Flags);
    }

    // CoPilot: claude-3-7-sonnet-20250219
    [Theory]
    [InlineData (MouseFlags.WheeledUp)]
    [InlineData (MouseFlags.WheeledDown)]
    [InlineData (MouseFlags.WheeledLeft)]
    [InlineData (MouseFlags.WheeledRight)]
    [InlineData (MouseFlags.PositionReport)]
    public void Process_NonClickEvents_PassThroughWithoutModification (MouseFlags flags)
    {
        // Arrange
        DateTime currentTime = DateTime.Now;
        MouseInterpreter interpreter = new (new FuncTimeProvider (() => currentTime), TimeSpan.FromMilliseconds (500));

        Mouse mouse = new () { ScreenPosition = new (10, 10), Flags = flags };

        // Act
        List<Mouse> events = interpreter.Process (mouse).ToList ();

        // Assert - Should only yield the original event, no synthetic events
        Assert.Single (events);
        Assert.Equal (flags, events [0].Flags);
    }

    #endregion
}
