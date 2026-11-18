using ITimer = Terminal.Gui.Time.ITimer;

namespace TimeTests;

public class VirtualTimeProviderTests
{
    [Fact]
    public void VirtualTimeProvider_StartsAtInitialTime ()
    {
        // Copilot - Test that virtual time provider starts at the expected initial time
        VirtualTimeProvider timeProvider = new ();
        DateTime expectedStart = new (2025, 1, 1, 0, 0, 0);

        Assert.Equal (expectedStart, timeProvider.Now);
    }

    [Fact]
    public void VirtualTimeProvider_AdvanceTime_UpdatesNow ()
    {
        // Copilot - Test that advancing time updates the Now property
        VirtualTimeProvider timeProvider = new ();
        DateTime startTime = timeProvider.Now;

        timeProvider.Advance (TimeSpan.FromSeconds (10));

        Assert.Equal (startTime.AddSeconds (10), timeProvider.Now);
    }

    [Fact]
    public void VirtualTimeProvider_SetTime_UpdatesNow ()
    {
        // Copilot - Test that setting time explicitly works
        VirtualTimeProvider timeProvider = new ();
        DateTime newTime = new (2025, 6, 15, 12, 30, 45);

        timeProvider.SetTime (newTime);

        Assert.Equal (newTime, timeProvider.Now);
    }

    [Fact]
    public void VirtualTimeProvider_MultipleAdvances_Accumulate ()
    {
        // Copilot - Test that multiple time advances accumulate correctly
        VirtualTimeProvider timeProvider = new ();
        DateTime startTime = timeProvider.Now;

        timeProvider.Advance (TimeSpan.FromSeconds (5));
        timeProvider.Advance (TimeSpan.FromSeconds (10));
        timeProvider.Advance (TimeSpan.FromSeconds (15));

        Assert.Equal (startTime.AddSeconds (30), timeProvider.Now);
    }

    [Fact]
    public async Task VirtualTimeProvider_Delay_CompletesAfterAdvance ()
    {
        // Copilot - Test that delays complete when time is advanced past their completion time
        VirtualTimeProvider timeProvider = new ();

        Task delayTask = timeProvider.Delay (TimeSpan.FromSeconds (5), TestContext.Current.CancellationToken);

        // Delay should not be completed yet
        Assert.False (delayTask.IsCompleted);

        // Advance time past the delay duration
        timeProvider.Advance (TimeSpan.FromSeconds (6));

        // Delay should now be completed
        await delayTask;
        Assert.True (delayTask.IsCompleted);
    }

    [Fact]
    public void VirtualTimer_FiresWhenTimeAdvances ()
    {
        // Copilot - Test that timers fire when virtual time advances past trigger point
        VirtualTimeProvider timeProvider = new ();
        var callbackCount = 0;

        ITimer timer = timeProvider.CreateTimer (TimeSpan.FromSeconds (10), () => callbackCount++);
        timer.Start ();

        // Timer should not have fired yet
        Assert.Equal (0, callbackCount);

        // Advance time past first trigger
        timeProvider.Advance (TimeSpan.FromSeconds (11));

        // Timer should have fired once
        Assert.Equal (1, callbackCount);

        // Advance time past second trigger
        timeProvider.Advance (TimeSpan.FromSeconds (10));

        // Timer should have fired twice
        Assert.Equal (2, callbackCount);

        timer.Dispose ();
    }

    [Fact]
    public void VirtualTimer_StopPreventsCallbacks ()
    {
        // Copilot - Test that stopping a timer prevents further callbacks
        VirtualTimeProvider timeProvider = new ();
        var callbackCount = 0;

        ITimer timer = timeProvider.CreateTimer (TimeSpan.FromSeconds (10), () => callbackCount++);
        timer.Start ();

        // Advance time to trigger once
        timeProvider.Advance (TimeSpan.FromSeconds (11));
        Assert.Equal (1, callbackCount);

        // Stop the timer
        timer.Stop ();

        // Advance time again
        timeProvider.Advance (TimeSpan.FromSeconds (20));

        // Callback count should not have increased
        Assert.Equal (1, callbackCount);

        timer.Dispose ();
    }
}
