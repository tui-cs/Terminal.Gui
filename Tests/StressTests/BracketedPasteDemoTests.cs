using UICatalog.Scenarios;

namespace StressTests;

public class BracketedPasteDemoTests
{
    // Copilot
    [Fact]
    public void BracketedPasteDemo_Categories_IncludeTextAndFormatting_NotInput ()
    {
        BracketedPasteDemo scenario = new ();

        List<string> categories = scenario.GetCategories ();

        Assert.Contains ("Text and Formatting", categories);
        Assert.DoesNotContain ("Input", categories);
    }

    // Copilot
    [Fact]
    public void FormatPasteLogEntry_IndicatesBracketedPasteEvent ()
    {
        string message = BracketedPasteDemo.FormatPasteLogEntry (1, "abc");

        Assert.Equal ("[1] Bracketed paste event: 3 chars: abc", message);
    }

    // Copilot
    [Fact]
    public void CreateHintLabel_InNarrowWindow_WrapsToMultipleLines ()
    {
        Window window = new ()
        {
            Width = 20,
            Height = 10
        };
        Label hint = BracketedPasteDemo.CreateHintLabel ();

        window.Add (hint);
        window.Layout ();

        Assert.True (hint.Frame.Height > 1);
    }
}
