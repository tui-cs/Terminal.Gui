namespace ApplicationTests.Timeout;

[Collection ("Application Timer Tests")]
public class LogarithmicTimeoutTests
{
    [Fact]
    public void Span_Should_Return_BaseDelay_When_Stage_Is_Zero ()
    {
        var baseDelay = TimeSpan.FromMilliseconds (1000);
        var timeout = new LogarithmicTimeout (baseDelay, () => true);

        Assert.Equal (TimeSpan.Zero, timeout.Span);
    }

    [Fact]
    public void Span_Should_Increase_Logarithmically ()
    {
        var baseDelay = TimeSpan.FromMilliseconds (1000);
        var timeout = new LogarithmicTimeout (baseDelay, () => true);

        var stage0 = timeout.Span;

        timeout.AdvanceStage (); // stage = 1
        var stage1 = timeout.Span;

        timeout.AdvanceStage (); // stage = 2
        var stage2 = timeout.Span;

        timeout.AdvanceStage (); // stage = 3
        var stage3 = timeout.Span;

        Assert.True (stage1 > stage0, "Stage 1 should be greater than stage 0");
        Assert.True (stage2 > stage1, "Stage 2 should be greater than stage 1");
        Assert.True (stage3 > stage2, "Stage 3 should be greater than stage 2");
    }

    [Theory]
    [MemberData (nameof (GetLogarithmicTestData))]
    public void Span_Should_Match_Expected_Logarithmic_Value (
        double baseDelayMs, int stage, double expectedMs)
    {
        var baseDelay = TimeSpan.FromMilliseconds (baseDelayMs);
        var timeout = new LogarithmicTimeout (baseDelay, () => true);

        for (int i = 0; i < stage; i++)
        {
            timeout.AdvanceStage ();
        }

        double actualMs = timeout.Span.TotalMilliseconds;
        double tolerance = 0.001; // Allow minor rounding error

        Assert.InRange (actualMs, expectedMs - tolerance, expectedMs + tolerance);
    }

    public static IEnumerable<object []> GetLogarithmicTestData ()
    {
        // baseDelayMs, stage, expectedSpanMs
        double baseMs = 1000;

        yield return new object [] { baseMs, 0, 0 };
        yield return new object [] { baseMs, 1, baseMs * Math.Log (2) };
        yield return new object [] { baseMs, 2, baseMs * Math.Log (3) };
        yield return new object [] { baseMs, 5, baseMs * Math.Log (6) };
        yield return new object [] { baseMs, 10, baseMs * Math.Log (11) };
    }


    [Fact]
    public void Reset_Should_Set_Stage_Back_To_Zero ()
    {
        var baseDelay = TimeSpan.FromMilliseconds (1000);
        var timeout = new LogarithmicTimeout (baseDelay, () => true);

        timeout.AdvanceStage ();
        timeout.AdvanceStage ();
        Assert.NotEqual (baseDelay, timeout.Span);

        timeout.Reset ();
        Assert.Equal (TimeSpan.Zero, timeout.Span);
    }
}