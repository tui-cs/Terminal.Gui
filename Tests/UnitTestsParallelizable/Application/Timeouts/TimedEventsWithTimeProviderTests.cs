// ReSharper disable AccessToModifiedClosure
namespace ApplicationTests.TimedEventTests;

/// <summary>
///     Tests for TimedEvents with ITimeProvider (VirtualTimeProvider) support.
///     These tests validate that TimedEvents correctly uses virtual time for deterministic, instant testing.
/// </summary>
[Trait ("Category", "Timeout")]
[Collection ("Application Timer Tests")]
public class TimedEventsWithTimeProviderTests
{
    [Fact]
    public void WithVirtualTimeProvider_UsesVirtualTime ()
    {
        // Arrange
        VirtualTimeProvider timeProvider = new ();
        timeProvider.SetTime (new (2025, 1, 1, 12, 0, 0));

        TimedEvents timedEvents = new (timeProvider);
        var executed = false;
        var executionTime = DateTime.MinValue;

        // Act - Schedule timeout for 1 second from now
        timedEvents.Add (
                         TimeSpan.FromSeconds (1),
                         () =>
                         {
                             executed = true;
                             executionTime = timeProvider.Now;

                             return false; // One-shot
                         });

        // Assert - Not executed yet
        Assert.False (executed);
        Assert.Single (timedEvents.Timeouts);

        // Advance time by 500ms - still not ready
        timeProvider.Advance (TimeSpan.FromMilliseconds (500));
        timedEvents.RunTimers ();
        Assert.False (executed);

        // Advance time by another 500ms - now ready
        timeProvider.Advance (TimeSpan.FromMilliseconds (500));
        timedEvents.RunTimers ();

        // Assert - Should have executed
        Assert.True (executed);
        Assert.Empty (timedEvents.Timeouts);
        Assert.Equal (new (2025, 1, 1, 12, 0, 1), executionTime);
    }

    [Fact]
    public void WithVirtualTimeProvider_MultipleTimeouts_ExecuteInOrder ()
    {
        // Arrange
        VirtualTimeProvider timeProvider = new ();
        timeProvider.SetTime (new (2025, 1, 1, 12, 0, 0));

        TimedEvents timedEvents = new (timeProvider);
        List<int> executionOrder = [];

        // Add timeouts in different order than execution
        timedEvents.Add (
                         TimeSpan.FromMilliseconds (300),
                         () =>
                         {
                             executionOrder.Add (3);

                             return false;
                         });

        timedEvents.Add (
                         TimeSpan.FromMilliseconds (100),
                         () =>
                         {
                             executionOrder.Add (1);

                             return false;
                         });

        timedEvents.Add (
                         TimeSpan.FromMilliseconds (200),
                         () =>
                         {
                             executionOrder.Add (2);

                             return false;
                         });

        // Act & Assert - Advance time and check execution order
        timeProvider.Advance (TimeSpan.FromMilliseconds (100));
        timedEvents.RunTimers ();
        Assert.Equal ([1], executionOrder);

        timeProvider.Advance (TimeSpan.FromMilliseconds (100));
        timedEvents.RunTimers ();
        Assert.Equal ([1, 2], executionOrder);

        timeProvider.Advance (TimeSpan.FromMilliseconds (100));
        timedEvents.RunTimers ();
        Assert.Equal ([1, 2, 3], executionOrder);
    }

    [Fact]
    public void WithVirtualTimeProvider_RepeatingTimeout_Rescheduled ()
    {
        // Arrange
        VirtualTimeProvider timeProvider = new ();
        timeProvider.SetTime (new (2025, 1, 1, 12, 0, 0));

        TimedEvents timedEvents = new (timeProvider);
        var executionCount = 0;

        // Add repeating timeout
        timedEvents.Add (
                         TimeSpan.FromMilliseconds (100),
                         () =>
                         {
                             executionCount++;

                             return executionCount < 3; // Repeat 3 times
                         });

        // Act & Assert - Execute multiple times
        for (var i = 1; i <= 3; i++)
        {
            timeProvider.Advance (TimeSpan.FromMilliseconds (100));
            timedEvents.RunTimers ();
            Assert.Equal (i, executionCount);

            if (i < 3)
            {
                Assert.Single (timedEvents.Timeouts); // Still scheduled
            }
            else
            {
                Assert.Empty (timedEvents.Timeouts); // Done
            }
        }
    }

    [Fact]
    public void WithVirtualTimeProvider_TimeSpanZero_ExecutesImmediately ()
    {
        // Arrange
        VirtualTimeProvider timeProvider = new ();
        timeProvider.SetTime (new (2025, 1, 1, 12, 0, 0));

        TimedEvents timedEvents = new (timeProvider);
        var executed = false;

        // Act - Add TimeSpan.Zero timeout
        timedEvents.Add (
                         TimeSpan.Zero,
                         () =>
                         {
                             executed = true;

                             return false;
                         });

        // Assert - Not executed yet (needs RunTimers call)
        Assert.False (executed);

        // Run timers - should execute immediately
        timedEvents.RunTimers ();
        Assert.True (executed);
        Assert.Empty (timedEvents.Timeouts);
    }

    [Fact]
    public void WithVirtualTimeProvider_Remove_CancelsTimeout ()
    {
        // Arrange
        VirtualTimeProvider timeProvider = new ();
        timeProvider.SetTime (new (2025, 1, 1, 12, 0, 0));

        TimedEvents timedEvents = new (timeProvider);
        var executed = false;

        // Act - Add timeout
        object token = timedEvents.Add (
                                        TimeSpan.FromMilliseconds (100),
                                        () =>
                                        {
                                            executed = true;

                                            return false;
                                        });

        Assert.Single (timedEvents.Timeouts);

        // Remove it
        bool removed = timedEvents.Remove (token);
        Assert.True (removed);
        Assert.Empty (timedEvents.Timeouts);

        // Advance time and run timers
        timeProvider.Advance (TimeSpan.FromMilliseconds (100));
        timedEvents.RunTimers ();

        // Assert - Should not have executed
        Assert.False (executed);
    }

    [Fact]
    public void WithVirtualTimeProvider_CheckTimers_ReturnsCorrectWaitTimeout ()
    {
        // Arrange
        VirtualTimeProvider timeProvider = new ();
        timeProvider.SetTime (new (2025, 1, 1, 12, 0, 0));

        TimedEvents timedEvents = new (timeProvider);

        // Add timeout for 500ms from now
        timedEvents.Add (TimeSpan.FromMilliseconds (500), () => false);

        // Act & Assert - Check wait timeout
        bool hasTimeouts = timedEvents.CheckTimers (out int waitTimeout);
        Assert.True (hasTimeouts);
        Assert.True (waitTimeout >= 499 && waitTimeout <= 500, $"Expected ~500ms, got {waitTimeout}ms");

        // Advance time by 300ms
        timeProvider.Advance (TimeSpan.FromMilliseconds (300));

        hasTimeouts = timedEvents.CheckTimers (out waitTimeout);
        Assert.True (hasTimeouts);
        Assert.True (waitTimeout >= 199 && waitTimeout <= 200, $"Expected ~200ms, got {waitTimeout}ms");
    }

    [Fact]
    public void WithNullTimeProvider_UsesStopwatch ()
    {
        // Arrange - Null time provider means use Stopwatch
        TimedEvents timedEvents = new (null);
        var executed = false;
        DateTime startTime = DateTime.Now;

        // Act - Add timeout for 50ms
        timedEvents.Add (
                         TimeSpan.FromMilliseconds (50),
                         () =>
                         {
                             executed = true;

                             return false;
                         });

        // Wait and run timers
        Thread.Sleep (60);
        timedEvents.RunTimers ();

        // Assert - Should have executed using real time
        Assert.True (executed);
        TimeSpan elapsed = DateTime.Now - startTime;
        Assert.True (elapsed.TotalMilliseconds >= 50, $"Should have waited at least 50ms, waited {elapsed.TotalMilliseconds}ms");
    }

    [Fact]
    public void WithVirtualTimeProvider_SmoothAcceleratingTimeout_AdvancesStages ()
    {
        // Arrange
        VirtualTimeProvider timeProvider = new ();
        timeProvider.SetTime (new (2025, 1, 1, 12, 0, 0));

        TimedEvents timedEvents = new (timeProvider);
        var executionCount = 0;
        List<TimeSpan> executionDelays = [];

        // Create smooth accelerating timeout - declare before use in lambda
        SmoothAcceleratingTimeout? smoothTimeout = null;

        smoothTimeout = new (
                             TimeSpan.FromMilliseconds (500), // Initial delay
                             TimeSpan.FromMilliseconds (50), // Min delay
                             0.7, // Decay factor
                             () =>
                             {
                                 executionCount++;
                                 executionDelays.Add (smoothTimeout!.Span);

                                 if (executionCount < 5)
                                 {
                                     smoothTimeout.AdvanceStage ();

                                     return true; // Continue
                                 }

                                 return false; // Stop
                             });

        timedEvents.Add (smoothTimeout);

        // Act - Execute through multiple stages
        for (var i = 0; i < 5; i++)
        {
            TimeSpan currentSpan = smoothTimeout.Span;
            timeProvider.Advance (currentSpan);
            timedEvents.RunTimers ();
        }

        // Assert
        Assert.Equal (5, executionCount);
        Assert.Equal (5, executionDelays.Count);

        // Verify delays are decreasing (accelerating)
        Assert.True (executionDelays [0] > executionDelays [1], "Stage 0 should be slower than stage 1");
        Assert.True (executionDelays [1] > executionDelays [2], "Stage 1 should be slower than stage 2");
        Assert.True (executionDelays [2] > executionDelays [3], "Stage 2 should be slower than stage 3");

        // Verify final delay is at or approaching minimum (allow some margin due to exponential decay)
        Assert.True (
                     executionDelays [^1].TotalMilliseconds <= 200,
                     $"Final delay should be approaching minimum 50ms, got {executionDelays [^1].TotalMilliseconds}ms");
    }
}
