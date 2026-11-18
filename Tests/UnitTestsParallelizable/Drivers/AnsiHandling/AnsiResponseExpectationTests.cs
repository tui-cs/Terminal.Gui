namespace DriverTests.AnsiHandling;

[Collection ("Driver Tests")]
public class AnsiResponseExpectationTests
{
    [Fact]
    public void Matches_ReturnsFalse_When_CurOrTerminator_NullOrEmpty ()
    {
        var e1 = new AnsiResponseExpectation (null, null, _ => { }, null);
        Assert.False (e1.Matches ("\u001b[6;20;10t"));

        var e2 = new AnsiResponseExpectation ("c", null, _ => { }, null);
        Assert.False (e2.Matches (null));
        Assert.False (e2.Matches (string.Empty));
    }

    [Fact]
    public void Matches_ReturnsFalse_When_DoesNotEndWithTerminator ()
    {
        var e = new AnsiResponseExpectation ("c", null, _ => { }, null);
        Assert.False (e.Matches ("\u001b[6;20;10x"));
    }

    [Fact]
    public void Matches_ReturnsTrue_When_NoValue_And_EndsWithTerminator ()
    {
        var e = new AnsiResponseExpectation ("c", null, _ => { }, null);
        Assert.True (e.Matches ("\u001b[0c"));
        Assert.True (e.Matches ("[0c"));
    }

    [Fact]
    public void Matches_UsesRegexToMatch_FirstNumericToken_AfterBracket ()
    {
        var exp6 = new AnsiResponseExpectation ("t", "6", _ => { }, null);
        var exp8 = new AnsiResponseExpectation ("t", "8", _ => { }, null);

        Assert.True (exp6.Matches ("\u001b[6;20;10t"));
        Assert.False (exp8.Matches ("\u001b[6;20;10t"));

        // Also works without ESC prefix
        Assert.True (exp6.Matches ("[6;20;10t"));
    }

    [Fact]
    public void Matches_Fallback_Contains_Check_When_NotStartingWithBracket ()
    {
        var e = new AnsiResponseExpectation ("t", "6", _ => { }, null);

        // Does not start with '[' but contains the token sequence
        string cur = "randomprefix[6;20;10t";
        Assert.True (e.Matches (cur));

        // If value differs should be false
        var e8 = new AnsiResponseExpectation ("t", "8", _ => { }, null);
        Assert.False (e8.Matches (cur));
    }
}