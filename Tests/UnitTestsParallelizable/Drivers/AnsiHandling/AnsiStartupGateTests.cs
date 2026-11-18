namespace DriverTests.AnsiHandling;

// Copilot

[Collection ("Driver Tests")]
public class AnsiStartupGateTests
{
    [Fact]
    public void RegisterQuery_And_MarkComplete_MakesGateReady ()
    {
        DateTime nowUtc = DateTime.UtcNow;
        AnsiStartupGate gate = new (() => nowUtc);

        gate.RegisterQuery (AnsiStartupQuery.TerminalSize, TimeSpan.FromMilliseconds (500));

        Assert.False (gate.IsReady);
        Assert.Equal ([AnsiStartupQuery.TerminalSize], gate.PendingQueries);

        gate.MarkComplete (AnsiStartupQuery.TerminalSize);

        Assert.True (gate.IsReady);
        Assert.Empty (gate.PendingQueries);
    }

    [Fact]
    public void RegisterQuery_TimesOut_And_MakesGateReady ()
    {
        DateTime nowUtc = DateTime.UtcNow;
        AnsiStartupGate gate = new (() => nowUtc);

        gate.RegisterQuery (AnsiStartupQuery.TerminalSize, TimeSpan.FromMilliseconds (100));

        Assert.False (gate.IsReady);
        Assert.Equal ([AnsiStartupQuery.TerminalSize], gate.PendingQueries);

        nowUtc = nowUtc.AddMilliseconds (101);

        Assert.True (gate.IsReady);
        Assert.Empty (gate.PendingQueries);
    }

    [Fact]
    public void RegisterMultipleQueries_TracksOnlyPendingNames ()
    {
        DateTime nowUtc = DateTime.UtcNow;
        AnsiStartupGate gate = new (() => nowUtc);

        gate.RegisterQuery (AnsiStartupQuery.TerminalSize, TimeSpan.FromSeconds (1));
        gate.RegisterQuery (AnsiStartupQuery.CursorPosition, TimeSpan.FromSeconds (1));

        gate.MarkComplete (AnsiStartupQuery.TerminalSize);

        Assert.False (gate.IsReady);
        Assert.Equal ([AnsiStartupQuery.CursorPosition], gate.PendingQueries);
    }
}
