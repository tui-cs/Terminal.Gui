namespace ApplicationTests.Timeout;

[Collection ("Application Timer Tests")]
public class SmoothAcceleratingTimeoutTests
{
    [Fact]
    public void Span_Should_Return_InitialDelay_On_StageZero ()
    {
        var initialDelay = TimeSpan.FromMilliseconds (500);
        var minDelay = TimeSpan.FromMilliseconds (50);
        double decayFactor = 0.7;

        var timeout = new SmoothAcceleratingTimeout (initialDelay, minDelay, decayFactor, () => true);

        Assert.Equal (initialDelay, timeout.Span);
    }

    [Fact]
    public void Span_Should_Decrease_As_Stage_Increases ()
    {
        var initialDelay = TimeSpan.FromMilliseconds (500);
        var minDelay = TimeSpan.FromMilliseconds (50);
        double decayFactor = 0.7;

        var timeout = new SmoothAcceleratingTimeout (initialDelay, minDelay, decayFactor, () => true);

        var previousSpan = timeout.Span;
        for (int i = 0; i < 10; i++)
        {
            timeout.AdvanceStage ();
            var currentSpan = timeout.Span;
            Assert.True (currentSpan <= previousSpan, $"Stage {i + 1}: {currentSpan} should be <= {previousSpan}");
            previousSpan = currentSpan;
        }
    }

    [Fact]
    public void Span_Should_Not_Go_Below_MinDelay ()
    {
        var initialDelay = TimeSpan.FromMilliseconds (500);
        var minDelay = TimeSpan.FromMilliseconds (50);
        double decayFactor = 0.5;

        var timeout = new SmoothAcceleratingTimeout (initialDelay, minDelay, decayFactor, () => true);

        for (int i = 0; i < 100; i++)
        {
            timeout.AdvanceStage ();
        }

        Assert.Equal (minDelay, timeout.Span);
    }

    [Fact]
    public void Reset_Should_Set_Stage_Back_To_Zero ()
    {
        var initialDelay = TimeSpan.FromMilliseconds (500);
        var minDelay = TimeSpan.FromMilliseconds (50);
        double decayFactor = 0.7;

        var timeout = new SmoothAcceleratingTimeout (initialDelay, minDelay, decayFactor, () => true);

        timeout.AdvanceStage ();
        timeout.AdvanceStage ();
        Assert.NotEqual (initialDelay, timeout.Span);

        timeout.Reset ();
        Assert.Equal (initialDelay, timeout.Span);
    }
}
