namespace Terminal.Gui.KeySequences.Tests;

public class KeySequenceParserTests
{
    [Fact]
    public void Parse_Creates_Leader_And_Tokens ()
    {
        KeySequencePattern pattern = KeySequenceParser.Parse ("; m <count> k");

        Assert.Equal (';', pattern.LeaderKey);
        Assert.Equal (3, pattern.Tokens.Count);
        Assert.Equal (KeySequenceTokenKind.Literal, pattern.Tokens [0].Kind);
        Assert.Equal (KeySequenceTokenKind.Count, pattern.Tokens [1].Kind);
        Assert.Equal (KeySequenceTokenKind.Literal, pattern.Tokens [2].Kind);
    }

    [Fact]
    public void Parse_Supports_Named_Keys ()
    {
        KeySequencePattern pattern = KeySequenceParser.Parse ("<Space> f f");

        Assert.Equal (Key.Space, pattern.LeaderKey);
        Assert.Equal (2, pattern.Tokens.Count);
    }

    [Fact]
    public void Parse_Rejects_Multiple_Count_Tokens ()
    {
        Assert.Throws<ArgumentException> (() => KeySequenceParser.Parse ("; <count> m <count> k"));
    }

    [Fact]
    public void ParseCommandMode_Creates_Tokens_Without_Leader ()
    {
        KeySequencePattern pattern = KeySequenceParser.ParseCommandMode ("<count> k");

        Assert.Null (pattern.LeaderKey);
        Assert.Equal (2, pattern.Tokens.Count);
        Assert.Equal (KeySequenceTokenKind.Count, pattern.Tokens [0].Kind);
        Assert.Equal (KeySequenceTokenKind.Literal, pattern.Tokens [1].Kind);
    }
}
