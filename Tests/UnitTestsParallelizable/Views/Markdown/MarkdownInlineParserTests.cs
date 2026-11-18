using JetBrains.Annotations;

namespace ViewsTests.Markdown;

[TestSubject (typeof (MarkdownInlineParser))]
public class MarkdownInlineParserTests
{
    // Copilot

    [Fact]
    public void ParseInlines_Plain_Text_Returns_Single_Run ()
    {
        List<InlineRun> runs = MarkdownInlineParser.ParseInlines ("hello world", MarkdownStyleRole.Normal);

        Assert.Single (runs);
        Assert.Equal ("hello world", runs [0].Text);
        Assert.Equal (MarkdownStyleRole.Normal, runs [0].StyleRole);
    }

    [Fact]
    public void ParseInlines_Bold_Returns_Strong_Run ()
    {
        List<InlineRun> runs = MarkdownInlineParser.ParseInlines ("before **bold** after", MarkdownStyleRole.Normal);

        Assert.Equal (3, runs.Count);
        Assert.Equal ("before ", runs [0].Text);
        Assert.Equal (MarkdownStyleRole.Normal, runs [0].StyleRole);
        Assert.Equal ("bold", runs [1].Text);
        Assert.Equal (MarkdownStyleRole.Strong, runs [1].StyleRole);
        Assert.Equal (" after", runs [2].Text);
        Assert.Equal (MarkdownStyleRole.Normal, runs [2].StyleRole);
    }

    [Fact]
    public void ParseInlines_Italic_Returns_Emphasis_Run ()
    {
        List<InlineRun> runs = MarkdownInlineParser.ParseInlines ("some *italic* text", MarkdownStyleRole.Normal);

        Assert.Equal (3, runs.Count);
        Assert.Equal ("italic", runs [1].Text);
        Assert.Equal (MarkdownStyleRole.Emphasis, runs [1].StyleRole);
    }

    [Fact]
    public void ParseInlines_InlineCode_Returns_Code_Run ()
    {
        List<InlineRun> runs = MarkdownInlineParser.ParseInlines ("use `code` here", MarkdownStyleRole.Normal);

        Assert.Equal (3, runs.Count);
        Assert.Equal ("code", runs [1].Text);
        Assert.Equal (MarkdownStyleRole.InlineCode, runs [1].StyleRole);
    }

    [Fact]
    public void ParseInlines_Link_Returns_Link_Run_With_Url ()
    {
        List<InlineRun> runs = MarkdownInlineParser.ParseInlines ("click [here](https://example.com) now", MarkdownStyleRole.Normal);

        Assert.Equal (3, runs.Count);
        Assert.Equal ("here", runs [1].Text);
        Assert.Equal (MarkdownStyleRole.Link, runs [1].StyleRole);
        Assert.Equal ("https://example.com", runs [1].Url);
    }

    [Fact]
    public void ParseInlines_Mixed_Formatting ()
    {
        List<InlineRun> runs = MarkdownInlineParser.ParseInlines ("**bold** and `code`", MarkdownStyleRole.Normal);

        Assert.Equal (3, runs.Count);
        Assert.Equal (MarkdownStyleRole.Strong, runs [0].StyleRole);
        Assert.Equal (" and ", runs [1].Text);
        Assert.Equal (MarkdownStyleRole.InlineCode, runs [2].StyleRole);
    }

    [Fact]
    public void ParseInlines_DefaultRole_Propagates ()
    {
        List<InlineRun> runs = MarkdownInlineParser.ParseInlines ("heading text", MarkdownStyleRole.Heading);

        Assert.Single (runs);
        Assert.Equal (MarkdownStyleRole.Heading, runs [0].StyleRole);
    }

    [Fact]
    public void ParseInlines_Unclosed_Delimiter_Treated_As_Plain_Text ()
    {
        List<InlineRun> runs = MarkdownInlineParser.ParseInlines ("a **unclosed bold", MarkdownStyleRole.Normal);

        // Should not hang and should produce runs covering the full text
        int totalLen = 0;

        foreach (InlineRun run in runs)
        {
            totalLen += run.Text.Length;
        }

        Assert.Equal ("a **unclosed bold".Length, totalLen);
    }

    [Fact]
    public void ParseInlines_Empty_String_Returns_Empty ()
    {
        List<InlineRun> runs = MarkdownInlineParser.ParseInlines ("", MarkdownStyleRole.Normal);

        Assert.Empty (runs);
    }

    // Copilot
    [Fact]
    public void ParseInlines_Link_With_Parentheses_In_Text ()
    {
        // This is the exact pattern from the layout.md TOC that was broken
        List<InlineRun> runs = MarkdownInlineParser.ParseInlines (
            "[Center with Auto-Sizing and Constraints (Like Dialog)](#center-with-auto-sizing-and-constraints-like-dialog)",
            MarkdownStyleRole.Normal);

        Assert.Single (runs);
        Assert.Equal ("Center with Auto-Sizing and Constraints (Like Dialog)", runs [0].Text);
        Assert.Equal (MarkdownStyleRole.Link, runs [0].StyleRole);
        Assert.Equal ("#center-with-auto-sizing-and-constraints-like-dialog", runs [0].Url);
    }
}
