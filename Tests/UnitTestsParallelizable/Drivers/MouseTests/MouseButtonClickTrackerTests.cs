// ReSharper disable AccessToModifiedClosure
// ReSharper disable InconsistentNaming

namespace DriverTests.MouseTests;

/// <summary>
///     Unit tests for <see cref="MouseButtonClickTracker"/> state machine and click detection logic.
/// </summary>
[Trait ("Category", "Input")]
[Collection ("Driver Tests")]
public class MouseButtonClickTrackerTests (ITestOutputHelper output)
{
    #region Setup

    private readonly ITestOutputHelper _output = output;

    #endregion

    #region State Transition Tests

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void UpdateState_SinglePress_ReturnsNull ()
    {
        // Arrange
        DateTime now = DateTime.Now;
        MouseButtonClickTracker tracker = new (new FuncTimeProvider (() => now), TimeSpan.FromMilliseconds (500), 0);
        Mouse mouse = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed };

        // Act
        tracker.UpdateState (mouse, out int? numClicks);

        // Assert
        Assert.Null (numClicks);
        Assert.True (tracker.Pressed);
    }

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void UpdateState_Press_Release_Returns1 ()
    {
        // Arrange
        DateTime now = DateTime.Now;
        MouseButtonClickTracker tracker = new (new FuncTimeProvider (() => now), TimeSpan.FromMilliseconds (500), 0);

        Mouse pressEvent = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed };
        Mouse releaseEvent = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased };

        // Act
        tracker.UpdateState (pressEvent, out int? pressClicks);
        tracker.UpdateState (releaseEvent, out int? releaseClicks);

        // Assert
        Assert.Null (pressClicks);
        Assert.Equal (1, releaseClicks);
    }

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void UpdateState_Press_Release_Press_Release_WithinThreshold_Returns2 ()
    {
        // Arrange
        DateTime currentTime = DateTime.Now;
        MouseButtonClickTracker tracker = new (new FuncTimeProvider (() => currentTime), TimeSpan.FromMilliseconds (500), 0);

        Mouse pressEvent1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed };
        Mouse releaseEvent1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased };
        Mouse pressEvent2 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed };
        Mouse releaseEvent2 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased };

        // Act
        tracker.UpdateState (pressEvent1, out int? clicks1);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (releaseEvent1, out int? clicks2);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (pressEvent2, out int? clicks3);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (releaseEvent2, out int? clicks4);

        // Assert
        Assert.Null (clicks1); // First press
        Assert.Equal (1, clicks2); // First release
        Assert.Null (clicks3); // Second press
        Assert.Equal (2, clicks4); // Second release - double click!
    }

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void UpdateState_TripleClick_WithinThreshold_Returns3 ()
    {
        // Arrange
        DateTime currentTime = DateTime.Now;
        MouseButtonClickTracker tracker = new (new FuncTimeProvider (() => currentTime), TimeSpan.FromMilliseconds (500), 0);

        Mouse press = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed };
        Mouse release = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased };

        // Act - Three complete click sequences
        tracker.UpdateState (press, out _);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (release, out int? clicks1);

        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (press, out _);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (release, out int? clicks2);

        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (press, out _);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (release, out int? clicks3);

        // Assert
        Assert.Equal (1, clicks1);
        Assert.Equal (2, clicks2);
        Assert.Equal (3, clicks3); // Triple click!
    }

    #endregion

    #region ScreenPosition Change Tests

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void UpdateState_ScreenPositionChangeDuringSequence_ResetsCount ()
    {
        // Arrange
        DateTime currentTime = DateTime.Now;
        MouseButtonClickTracker tracker = new (new FuncTimeProvider (() => currentTime), TimeSpan.FromMilliseconds (500), 0);

        Mouse press1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed };
        Mouse release1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased };
        Mouse press2 = new () { ScreenPosition = new (20, 20), Flags = MouseFlags.LeftButtonPressed }; // Different position!
        Mouse release2 = new () { ScreenPosition = new (20, 20), Flags = MouseFlags.LeftButtonReleased };

        // Act
        tracker.UpdateState (press1, out _);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (release1, out int? clicks1);

        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (press2, out _);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (release2, out int? clicks2);

        // Assert
        Assert.Equal (1, clicks1);
        Assert.Equal (1, clicks2); // Reset to 1 because position changed
    }

    #endregion

    #region Timeout Tests

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void UpdateState_TimeoutExceeded_ResetsCount ()
    {
        // Arrange
        DateTime currentTime = DateTime.Now;
        MouseButtonClickTracker tracker = new (new FuncTimeProvider (() => currentTime), TimeSpan.FromMilliseconds (500), 0);

        Mouse press1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed };
        Mouse release1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased };
        Mouse press2 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed };
        Mouse release2 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased };

        // Act
        tracker.UpdateState (press1, out _);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (release1, out int? clicks1);

        currentTime = currentTime.AddMilliseconds (600); // Exceed 500ms threshold!
        tracker.UpdateState (press2, out _);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (release2, out int? clicks2);

        // Assert
        Assert.Equal (1, clicks1);
        Assert.Equal (1, clicks2); // Reset to 1 because timeout exceeded
    }

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void UpdateState_ExactlyAtThreshold_ResetsCount ()
    {
        // Arrange
        DateTime currentTime = DateTime.Now;
        TimeSpan threshold = TimeSpan.FromMilliseconds (500);
        MouseButtonClickTracker tracker = new (new FuncTimeProvider (() => currentTime), threshold, 0);

        Mouse press1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed };
        Mouse release1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased };
        Mouse press2 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed };
        Mouse release2 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased };

        // Act
        tracker.UpdateState (press1, out _);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (release1, out int? clicks1);

        currentTime = currentTime.Add (threshold).AddMilliseconds (1); // Just over threshold
        tracker.UpdateState (press2, out int? clicks2);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (release2, out int? clicks3);

        // Assert
        Assert.Equal (1, clicks1);
        Assert.Null (clicks2); // Reset because we exceeded threshold, press doesn't return click count
        Assert.Equal (1, clicks3); // After reset, starts counting from 1 again
    }

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void UpdateState_OneMsBelowThreshold_ContinuesCount ()
    {
        // Arrange
        DateTime currentTime = DateTime.Now;
        TimeSpan threshold = TimeSpan.FromMilliseconds (500);
        MouseButtonClickTracker tracker = new (new FuncTimeProvider (() => currentTime), threshold, 0);

        Mouse press1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed };
        Mouse release1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased };
        Mouse press2 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed };
        Mouse release2 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased };

        // Act
        tracker.UpdateState (press1, out _);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (release1, out int? clicks1);

        currentTime = currentTime.AddMilliseconds (449); // 499ms total - just below 500ms threshold
        tracker.UpdateState (press2, out _);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (release2, out int? clicks2);

        // Assert
        Assert.Equal (1, clicks1);
        Assert.Equal (2, clicks2); // Continues counting because within threshold
    }

    #endregion

    #region Edge Case Tests

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void UpdateState_ReleaseWithoutPress_ReturnsNull ()
    {
        // Arrange
        DateTime now = DateTime.Now;
        MouseButtonClickTracker tracker = new (new FuncTimeProvider (() => now), TimeSpan.FromMilliseconds (500), 0);
        Mouse releaseEvent = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased };

        // Act
        tracker.UpdateState (releaseEvent, out int? numClicks);

        // Assert
        Assert.Null (numClicks); // No press before release, so no click
        Assert.False (tracker.Pressed);
    }

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void UpdateState_MultipleConsecutivePresses_OnlyLastCounts ()
    {
        // Arrange
        DateTime currentTime = DateTime.Now;
        MouseButtonClickTracker tracker = new (new FuncTimeProvider (() => currentTime), TimeSpan.FromMilliseconds (500), 0);

        Mouse press1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed };
        Mouse press2 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed };
        Mouse press3 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed };
        Mouse release = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased };

        // Act
        tracker.UpdateState (press1, out int? clicks1);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (press2, out int? clicks2);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (press3, out int? clicks3);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (release, out int? clicks4);

        // Assert
        Assert.Null (clicks1);
        Assert.Null (clicks2); // No state change (already pressed)
        Assert.Null (clicks3); // No state change (already pressed)
        Assert.Equal (1, clicks4); // Single click when finally released
    }

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void UpdateState_MultipleConsecutiveReleases_OnlyFirstCounts ()
    {
        // Arrange
        DateTime currentTime = DateTime.Now;
        MouseButtonClickTracker tracker = new (new FuncTimeProvider (() => currentTime), TimeSpan.FromMilliseconds (500), 0);

        Mouse press = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed };
        Mouse release1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased };
        Mouse release2 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased };
        Mouse release3 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased };

        // Act
        tracker.UpdateState (press, out int? clicks1);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (release1, out int? clicks2);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (release2, out int? clicks3);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (release3, out int? clicks4);

        // Assert
        Assert.Null (clicks1);
        Assert.Equal (1, clicks2); // Click registered on first release
        Assert.Null (clicks3); // No state change (already released)
        Assert.Null (clicks4); // No state change (already released)
    }

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void UpdateState_DoublePress_WithoutIntermediateRelease_DoesNotCountAsDoubleClick ()
    {
        // Arrange
        DateTime currentTime = DateTime.Now;
        MouseButtonClickTracker tracker = new (new FuncTimeProvider (() => currentTime), TimeSpan.FromMilliseconds (500), 0);

        Mouse press1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed };
        Mouse press2 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed };
        Mouse release = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased };

        // Act
        tracker.UpdateState (press1, out int? clicks1);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (press2, out int? clicks2); // Second press without release
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (release, out int? clicks3);

        // Assert
        Assert.Null (clicks1);
        Assert.Null (clicks2); // No state change (already pressed)
        Assert.Equal (1, clicks3); // Only single click because no complete press-release cycle
    }

    #endregion

    #region Timestamp Tests

    // CoPilot - AI Generated
    [Fact]
    public void UpdateState_UsesMouseTimestamp_WhenProvided ()
    {
        // Arrange
        DateTime baseTime = new (2025, 1, 1, 12, 0, 0);
        MouseButtonClickTracker tracker = new (new FuncTimeProvider (() => DateTime.Now), TimeSpan.FromMilliseconds (500), 0);

        Mouse press = new ()
        {
            ScreenPosition = new (10, 10),
            Flags = MouseFlags.LeftButtonPressed,
            Timestamp = baseTime
        };

        Mouse release = new ()
        {
            ScreenPosition = new (10, 10),
            Flags = MouseFlags.LeftButtonReleased,
            Timestamp = baseTime.AddMilliseconds (100)
        };

        // Act
        tracker.UpdateState (press, out int? clicks1);
        tracker.UpdateState (release, out int? clicks2);

        // Assert
        Assert.Null (clicks1);
        Assert.Equal (1, clicks2);

        // Verify timestamp was recorded
        Assert.Equal (baseTime.AddMilliseconds (100), tracker.At);
    }

    // CoPilot - AI Generated
    [Fact]
    public void UpdateState_TimestampBased_DoubleClick_WithinThreshold ()
    {
        // Arrange
        DateTime baseTime = new (2025, 1, 1, 12, 0, 0);
        MouseButtonClickTracker tracker = new (new FuncTimeProvider (() => DateTime.Now.AddYears (10)), TimeSpan.FromMilliseconds (500), 0);

        // Act - First click at T+0
        Mouse press1 = new ()
        {
            ScreenPosition = new (10, 10),
            Flags = MouseFlags.LeftButtonPressed,
            Timestamp = baseTime
        };
        tracker.UpdateState (press1, out _);

        Mouse release1 = new ()
        {
            ScreenPosition = new (10, 10),
            Flags = MouseFlags.LeftButtonReleased,
            Timestamp = baseTime.AddMilliseconds (50)
        };
        tracker.UpdateState (release1, out int? clicks1);

        // Second click at T+200 (well within 500ms threshold)
        Mouse press2 = new ()
        {
            ScreenPosition = new (10, 10),
            Flags = MouseFlags.LeftButtonPressed,
            Timestamp = baseTime.AddMilliseconds (200)
        };
        tracker.UpdateState (press2, out _);

        Mouse release2 = new ()
        {
            ScreenPosition = new (10, 10),
            Flags = MouseFlags.LeftButtonReleased,
            Timestamp = baseTime.AddMilliseconds (250)
        };
        tracker.UpdateState (release2, out int? clicks2);

        // Assert
        Assert.Equal (1, clicks1);
        Assert.Equal (2, clicks2); // Double-click because timestamps are within 500ms
    }

    // CoPilot - AI Generated
    [Fact]
    public void UpdateState_TimestampBased_SingleClicks_OutsideThreshold ()
    {
        // Arrange
        DateTime baseTime = new (2025, 1, 1, 12, 0, 0);
        MouseButtonClickTracker tracker = new (new FuncTimeProvider (() => DateTime.Now.AddYears (10)), TimeSpan.FromMilliseconds (500), 0);

        // Act - First click at T+0
        Mouse press1 = new ()
        {
            ScreenPosition = new (10, 10),
            Flags = MouseFlags.LeftButtonPressed,
            Timestamp = baseTime
        };
        tracker.UpdateState (press1, out _);

        Mouse release1 = new ()
        {
            ScreenPosition = new (10, 10),
            Flags = MouseFlags.LeftButtonReleased,
            Timestamp = baseTime.AddMilliseconds (50)
        };
        tracker.UpdateState (release1, out int? clicks1);

        // Second click at T+600 (beyond 500ms threshold)
        Mouse press2 = new ()
        {
            ScreenPosition = new (10, 10),
            Flags = MouseFlags.LeftButtonPressed,
            Timestamp = baseTime.AddMilliseconds (600)
        };
        tracker.UpdateState (press2, out _);

        Mouse release2 = new ()
        {
            ScreenPosition = new (10, 10),
            Flags = MouseFlags.LeftButtonReleased,
            Timestamp = baseTime.AddMilliseconds (650)
        };
        tracker.UpdateState (release2, out int? clicks2);

        // Assert
        Assert.Equal (1, clicks1);
        Assert.Equal (1, clicks2); // Reset to single click because >500ms elapsed
    }

    // CoPilot - AI Generated
    [Fact]
    public void UpdateState_TimestampBased_TripleClick_WithinThreshold ()
    {
        // Arrange
        DateTime baseTime = new (2025, 1, 1, 12, 0, 0);
        MouseButtonClickTracker tracker = new (new FuncTimeProvider (() => DateTime.Now.AddYears (10)), TimeSpan.FromMilliseconds (500), 0);

        // Act - Three clicks, each 150ms apart (total 450ms < 500ms threshold)
        Mouse press1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime };
        tracker.UpdateState (press1, out _);
        Mouse release1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (50) };
        tracker.UpdateState (release1, out int? clicks1);

        Mouse press2 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime.AddMilliseconds (150) };
        tracker.UpdateState (press2, out _);
        Mouse release2 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (200) };
        tracker.UpdateState (release2, out int? clicks2);

        Mouse press3 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime.AddMilliseconds (300) };
        tracker.UpdateState (press3, out _);
        Mouse release3 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (350) };
        tracker.UpdateState (release3, out int? clicks3);

        // Assert
        Assert.Equal (1, clicks1);
        Assert.Equal (2, clicks2);
        Assert.Equal (3, clicks3); // Triple-click because all within 500ms
    }

    // CoPilot - AI Generated
    [Fact]
    public void UpdateState_QuadrupleClick_WithinThreshold_Returns4 ()
    {
        // Arrange
        DateTime baseTime = new (2025, 1, 1, 12, 0, 0);
        MouseButtonClickTracker tracker = new (new FuncTimeProvider (() => DateTime.Now.AddYears (10)), TimeSpan.FromMilliseconds (500), 0);

        // Act - Four complete click sequences within threshold
        tracker.UpdateState (new() { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime }, out _);

        tracker.UpdateState (
                             new() { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (50) },
                             out int? clicks1);

        tracker.UpdateState (new() { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime.AddMilliseconds (100) }, out _);

        tracker.UpdateState (
                             new() { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (150) },
                             out int? clicks2);

        tracker.UpdateState (new() { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime.AddMilliseconds (200) }, out _);

        tracker.UpdateState (
                             new() { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (250) },
                             out int? clicks3);

        tracker.UpdateState (new() { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime.AddMilliseconds (300) }, out _);

        tracker.UpdateState (
                             new() { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (350) },
                             out int? clicks4);

        // Assert
        Assert.Equal (1, clicks1);
        Assert.Equal (2, clicks2);
        Assert.Equal (3, clicks3);
        Assert.Equal (4, clicks4); // Quadruple click!
    }

    // CoPilot - AI Generated
    [Fact]
    public void UpdateState_MultipleClicksBeyond4_WithinThreshold_CountsContinuously ()
    {
        // Arrange
        DateTime baseTime = new (2025, 1, 1, 12, 0, 0);
        MouseButtonClickTracker tracker = new (new FuncTimeProvider (() => DateTime.Now.AddYears (10)), TimeSpan.FromMilliseconds (500), 0);

        // Act - Six complete click sequences
        var currentMs = 0;
        List<int?> clickCounts = new ();

        for (var i = 0; i < 6; i++)
        {
            tracker.UpdateState (
                                 new()
                                 {
                                     ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime.AddMilliseconds (currentMs)
                                 },
                                 out _);
            currentMs += 50;

            tracker.UpdateState (
                                 new()
                                 {
                                     ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (currentMs)
                                 },
                                 out int? clicks);
            clickCounts.Add (clicks);
            currentMs += 50;
        }

        // Assert
        Assert.Equal (1, clickCounts [0]);
        Assert.Equal (2, clickCounts [1]);
        Assert.Equal (3, clickCounts [2]);
        Assert.Equal (4, clickCounts [3]);
        Assert.Equal (5, clickCounts [4]);
        Assert.Equal (6, clickCounts [5]);
    }

    #endregion

    #region Position Change Tests with Timestamps

    // CoPilot - AI Generated
    [Fact]
    public void UpdateState_TimestampBased_ScreenPositionChange_ResetsCount ()
    {
        // Arrange
        DateTime baseTime = new (2025, 1, 1, 12, 0, 0);
        MouseButtonClickTracker tracker = new (new FuncTimeProvider (() => DateTime.Now.AddYears (10)), TimeSpan.FromMilliseconds (500), 0);

        // Act - First click at (10, 10)
        tracker.UpdateState (new() { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime }, out _);

        tracker.UpdateState (
                             new() { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (50) },
                             out int? clicks1);

        // Second click at (20, 20) within threshold
        tracker.UpdateState (new() { ScreenPosition = new (20, 20), Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime.AddMilliseconds (100) }, out _);

        tracker.UpdateState (
                             new() { ScreenPosition = new (20, 20), Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (150) },
                             out int? clicks2);

        // Assert
        Assert.Equal (1, clicks1);
        Assert.Equal (1, clicks2); // Reset to 1 because position changed
    }

    // CoPilot - AI Generated
    [Fact]
    public void UpdateState_TimestampBased_ScreenPositionChangeOnPress_ResetsCount ()
    {
        // Arrange
        DateTime baseTime = new (2025, 1, 1, 12, 0, 0);
        MouseButtonClickTracker tracker = new (new FuncTimeProvider (() => DateTime.Now.AddYears (10)), TimeSpan.FromMilliseconds (500), 0);

        // Act - First click at (10, 10)
        tracker.UpdateState (new() { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime }, out _);

        tracker.UpdateState (
                             new() { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (50) },
                             out int? clicks1);

        // Second press at different position (15, 15) - should reset before press happens
        tracker.UpdateState (
                             new() { ScreenPosition = new (15, 15), Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime.AddMilliseconds (100) },
                             out int? clicks2);

        tracker.UpdateState (
                             new() { ScreenPosition = new (15, 15), Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (150) },
                             out int? clicks3);

        // Assert
        Assert.Equal (1, clicks1);
        Assert.Null (clicks2); // Press doesn't emit click
        Assert.Equal (1, clicks3); // Reset to 1 because position changed on press
    }

    // CoPilot - AI Generated
    [Fact]
    public void UpdateState_TimestampBased_OnePixelPositionChange_ResetsCount ()
    {
        // Arrange
        DateTime baseTime = new (2025, 1, 1, 12, 0, 0);
        MouseButtonClickTracker tracker = new (new FuncTimeProvider (() => DateTime.Now.AddYears (10)), TimeSpan.FromMilliseconds (500), 0);

        // Act - First click at (10, 10)
        tracker.UpdateState (new() { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime }, out _);

        tracker.UpdateState (
                             new() { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (50) },
                             out int? clicks1);

        // Second click at (10, 11) - one pixel away
        tracker.UpdateState (new() { ScreenPosition = new (10, 11), Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime.AddMilliseconds (100) }, out _);

        tracker.UpdateState (
                             new() { ScreenPosition = new (10, 11), Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (150) },
                             out int? clicks2);

        // Assert
        Assert.Equal (1, clicks1);
        Assert.Equal (1, clicks2); // Reset even with 1-pixel movement
    }

    #endregion

    #region Threshold Boundary with Position Changes

    // CoPilot - AI Generated
    [Fact]
    public void UpdateState_PositionChangeAtThresholdBoundary_ResetsEvenWithinTime ()
    {
        // Arrange
        DateTime baseTime = new (2025, 1, 1, 12, 0, 0);
        MouseButtonClickTracker tracker = new (new FuncTimeProvider (() => DateTime.Now.AddYears (10)), TimeSpan.FromMilliseconds (500), 0);

        // Act - First click at (10, 10)
        tracker.UpdateState (new() { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime }, out _);

        tracker.UpdateState (
                             new() { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (50) },
                             out int? clicks1);

        // Second click at different position but exactly at 499ms (within threshold)
        tracker.UpdateState (new() { ScreenPosition = new (20, 20), Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime.AddMilliseconds (549) }, out _);

        tracker.UpdateState (
                             new() { ScreenPosition = new (20, 20), Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (600) },
                             out int? clicks2);

        // Assert
        Assert.Equal (1, clicks1);
        Assert.Equal (1, clicks2); // Reset because position changed (even though time is within threshold)
    }

    // CoPilot - AI Generated
    [Fact]
    public void UpdateState_TimeThresholdExceededWithSamePosition_ResetsCount ()
    {
        // Arrange
        DateTime baseTime = new (2025, 1, 1, 12, 0, 0);
        MouseButtonClickTracker tracker = new (new FuncTimeProvider (() => DateTime.Now.AddYears (10)), TimeSpan.FromMilliseconds (500), 0);

        // Act - First click at (10, 10)
        tracker.UpdateState (new() { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime }, out _);

        tracker.UpdateState (
                             new() { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (50) },
                             out int? clicks1);

        // Second click at same position but >500ms later
        tracker.UpdateState (new() { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime.AddMilliseconds (551) }, out _);

        tracker.UpdateState (
                             new() { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (600) },
                             out int? clicks2);

        // Assert
        Assert.Equal (1, clicks1);
        Assert.Equal (1, clicks2); // Reset because time threshold exceeded (even though position is same)
    }

    #endregion

    #region CheckForExpiredClicks Tests

    // CoPilot - AI Generated
    [Fact]
    public void CheckForExpiredClicks_AlwaysReturnsNull ()
    {
        // Arrange
        DateTime now = DateTime.Now;
        MouseButtonClickTracker tracker = new (new FuncTimeProvider (() => now), TimeSpan.FromMilliseconds (500), 0);

        // Act - Check with no clicks
        bool result1 = tracker.CheckForExpiredClicks (out int? numClicks1, out Point position1);

        // Do some clicks
        tracker.UpdateState (new() { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed, Timestamp = now }, out _);
        tracker.UpdateState (new() { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased, Timestamp = now.AddMilliseconds (50) }, out _);

        // Check again
        bool result2 = tracker.CheckForExpiredClicks (out int? numClicks2, out Point position2);

        // Assert - Method is deprecated and always returns false/null
        Assert.False (result1);
        Assert.Null (numClicks1);
        Assert.Equal (Point.Empty, position1);

        Assert.False (result2);
        Assert.Null (numClicks2);
        Assert.Equal (Point.Empty, position2);
    }

    #endregion

    #region State Property Tests

    // CoPilot - AI Generated
    [Fact]
    public void At_ReflectsLastStateChangeTimestamp ()
    {
        // Arrange
        DateTime baseTime = new (2025, 1, 1, 12, 0, 0);
        MouseButtonClickTracker tracker = new (new FuncTimeProvider (() => DateTime.Now.AddYears (10)), TimeSpan.FromMilliseconds (500), 0);

        // Act & Assert - Check At property after each state change
        tracker.UpdateState (new() { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime }, out _);
        Assert.Equal (baseTime, tracker.At);

        tracker.UpdateState (new() { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (100) }, out _);
        Assert.Equal (baseTime.AddMilliseconds (100), tracker.At);

        tracker.UpdateState (new() { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime.AddMilliseconds (200) }, out _);
        Assert.Equal (baseTime.AddMilliseconds (200), tracker.At);
    }

    // CoPilot - AI Generated
    [Fact]
    public void Pressed_ReflectsCurrentButtonState ()
    {
        // Arrange
        DateTime baseTime = new (2025, 1, 1, 12, 0, 0);
        MouseButtonClickTracker tracker = new (new FuncTimeProvider (() => DateTime.Now.AddYears (10)), TimeSpan.FromMilliseconds (500), 0);

        // Assert - Initial state
        Assert.False (tracker.Pressed);

        // Act & Assert - Press
        tracker.UpdateState (new() { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime }, out _);
        Assert.True (tracker.Pressed);

        // Release
        tracker.UpdateState (new() { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (50) }, out _);
        Assert.False (tracker.Pressed);

        // Press again
        tracker.UpdateState (new() { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime.AddMilliseconds (100) }, out _);
        Assert.True (tracker.Pressed);
    }

    #endregion

    #region Button Index Tests

    // CoPilot: claude-3-7-sonnet-20250219
    [Theory]
    [InlineData (0, MouseFlags.LeftButtonPressed, MouseFlags.LeftButtonReleased)]
    [InlineData (1, MouseFlags.MiddleButtonPressed, MouseFlags.MiddleButtonReleased)]
    [InlineData (2, MouseFlags.RightButtonPressed, MouseFlags.RightButtonReleased)]
    [InlineData (3, MouseFlags.Button4Pressed, MouseFlags.Button4Released)]
    public void UpdateState_CorrectButtonIndex_TracksCorrectButton (int buttonIdx, MouseFlags pressedFlag, MouseFlags releasedFlag)
    {
        // Arrange
        DateTime currentTime = DateTime.Now;
        MouseButtonClickTracker tracker = new (new FuncTimeProvider (() => currentTime), TimeSpan.FromMilliseconds (500), buttonIdx);

        Mouse press = new () { ScreenPosition = new (10, 10), Flags = pressedFlag };
        Mouse release = new () { ScreenPosition = new (10, 10), Flags = releasedFlag };

        // Act
        tracker.UpdateState (press, out int? clicks1);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (release, out int? clicks2);

        // Assert
        Assert.Null (clicks1);
        Assert.Equal (1, clicks2);
    }

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void UpdateState_MultipleButtonsSimultaneous_EachTrackerIndependent ()
    {
        // Arrange
        DateTime currentTime = DateTime.Now;
        MouseButtonClickTracker tracker1 = new (new FuncTimeProvider (() => currentTime), TimeSpan.FromMilliseconds (500), 0); // Tracking Button1
        MouseButtonClickTracker tracker2 = new (new FuncTimeProvider (() => currentTime), TimeSpan.FromMilliseconds (500), 1); // Tracking Button2

        Mouse press1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonPressed };
        Mouse press2 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.MiddleButtonPressed };
        Mouse release1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.LeftButtonReleased };
        Mouse release2 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.MiddleButtonReleased };

        // Act - Press Button1, press Button2, release Button1, release Button2
        tracker1.UpdateState (press1, out int? t1_press1);
        tracker2.UpdateState (press1, out int? t2_press1);

        currentTime = currentTime.AddMilliseconds (50);
        tracker1.UpdateState (press2, out int? t1_press2);
        tracker2.UpdateState (press2, out int? t2_press2);

        currentTime = currentTime.AddMilliseconds (50);
        tracker1.UpdateState (release1, out int? t1_release1);
        tracker2.UpdateState (release1, out int? t2_release1);

        currentTime = currentTime.AddMilliseconds (50);
        tracker1.UpdateState (release2, out int? t1_release2);
        tracker2.UpdateState (release2, out int? t2_release2);

        // Assert - This demonstrates the quirk: trackers detect state changes based on whether
        // their button is pressed in the flags, not whether the event is FOR their button
        Assert.Null (t1_press1); // Tracker1: Button1 pressed → state changes to Pressed
        Assert.Null (t2_press1); // Tracker2: sees Button1 press (Button2 not in flags) → Released (initial) to Released (no change)

        // When Button2 is pressed (Button1 not in flags), Tracker1 sees: Pressed (current) → Released (new)
        Assert.Equal (1, t1_press2); // Tracker1: detects Pressed→Released transition! Generates click
        Assert.Null (t2_press2); // Tracker2: Button2 pressed → state changes to Pressed

        // When Button1 is released, Tracker1 already thinks it's released, so no change
        Assert.Null (t1_release1); // Tracker1: Released→Released (no change)

        // Tracker2 sees Button1 release (Button2 not in flags), interprets as Pressed→Released
        Assert.Equal (1, t2_release1); // Tracker2: detects Pressed→Released transition! Generates click

        Assert.Null (t1_release2); // Tracker1: Released→Released (no change)
        Assert.Null (t2_release2); // Tracker2: already released from previous event, Released→Released (no change)

        // This demonstrates why MouseInterpreter uses separate trackers and only feeds each tracker
        // events that have its button pressed or released flags set
    }

    #endregion
}
