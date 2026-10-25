using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.Tables;

namespace ViewsTests.Markdown;

// CoPilot - GPT-6
public class MarkdownDependencyRegressionTests
{
    [Theory]
    [InlineData (": ")]
    [InlineData (":  ")]
    [InlineData (":   ")]
    [InlineData ("~ ")]
    public void DefinitionList_Preserves_Term_And_Indents_Definition (string marker)
    {
        Assert.Equal (["Term bold", "  definition"], RenderLines ($"Term **bold**\n{marker}definition"));
    }

    [Fact]
    public void DefinitionList_Preserves_Multiple_Terms_And_Definitions ()
    {
        string [] lines = RenderLines ("First\n: one\n~ two\n\nSecond\n: three");

        Assert.Equal (["First", "  one", "  two", "Second", "  three"], lines);
    }

    [Theory]
    [InlineData ("\n", "")]
    [InlineData ("\r\n", "")]
    [InlineData ("\n", "\n")]
    public void DefinitionList_Spacing_Before_Heading_Matches_Source (string newline, string blankLine)
    {
        string source = $"Term{newline}: def{newline}{blankLine}# Heading";
        string [] expected = blankLine.Length == 0 ? ["Term", "  def", "  # Heading"] : ["Term", "  def", "", "# Heading"];

        Assert.Equal (expected, RenderLines (source));
    }

    [Fact]
    public void DefinitionList_End_Line_Includes_The_Definition ()
    {
        Markdig.Syntax.MarkdownDocument document = Markdig.Markdown.Parse ("Term\n: def", new MarkdownPipelineBuilder ().UseAdvancedExtensions ().Build ());
        Assert.Equal (1, Terminal.Gui.Views.Markdown.GetBlockEndLine (Assert.Single (document)));
    }

    [Fact]
    public void DefinitionList_Continuation_Preserves_Leading_Colon ()
    {
        string [] lines = RenderLines ("Term\n: definition\n:not-a-marker");

        Assert.Contains (":not-a-marker", string.Join ("\n", lines));
    }

    [Theory]
    [InlineData ("-", "•")]
    [InlineData ("- \t", "•")]
    [InlineData ("2.", "2.")]
    public void EmptyListItem_After_Paragraph_Preserves_Marker (string marker, string expected)
    {
        string [] lines = RenderLines ($"paragraph\n\n{marker}");

        Assert.Equal ("paragraph", lines [0]);
        Assert.Equal (expected, lines [^1]);
    }

    [Fact]
    public void EmptyListItems_Preserve_Each_Marker ()
    {
        Assert.Equal (["•", "•"], RenderLines ("-\n-"));
    }

    [Theory]
    [InlineData ("first  \nsecond", "first", "second")]
    [InlineData ("first\\\nsecond", "first", "second")]
    [InlineData ("**first  \nsecond**", "first", "second")]
    [InlineData ("[first  \nsecond](https://example.com)", "first", "second")]
    [InlineData ("first  \nsecond\n---", "## first", "second")]
    [InlineData ("> first  \n> second", "> first", "> second")]
    [InlineData ("> first  \n> second\n> ---", "> ## first", "> second")]
    [InlineData ("- first  \n  second", "• first", "  second")]
    [InlineData ("- first  \n  second\n  ---", "• ## first", "  second")]
    public void HardBreak_Starts_New_Rendered_Line (string markdown, string first, string second)
    {
        Assert.Equal ([first, second], RenderLines (markdown));
    }

    [Fact]
    public void SoftBreak_Remains_A_Space ()
    {
        Assert.Equal (["first second"], RenderLines ("first\nsecond"));
    }

    [Fact]
    public void HardBreak_After_Wrapping_Preserves_Continuation_Indent ()
    {
        Assert.Equal (["• abcdef", "  ghij", "  next"], RenderLines ("- abcdefghij  \n  next", 8));
    }

    [Fact]
    public void Setext_HardBreak_Generates_Usable_Anchor ()
    {
        using Terminal.Gui.Views.Markdown view = new () { Text = "first  \nsecond\n---", Width = 40, Height = 10 };
        view.Layout ();

        Assert.True (view.ScrollToAnchor ("first-second"));
        Assert.Equal (2, view.LineCount);
    }

    [Theory]
    [InlineData (">  bar\n> -", "> ## bar")]
    [InlineData ("-\n   bar\n  -", "• ## bar")]
    public void SetextHeading_In_Indented_Structure_Renders_As_Heading (string markdown, string expected)
    {
        Assert.Equal ([expected], RenderLines (markdown));
    }

    [Fact]
    public void HtmlBlock_Ends_At_WhitespaceOnly_Line ()
    {
        string [] lines = RenderLines ("<div>html</div>\n \t\n# Heading");

        Assert.Equal (["html", "# Heading"], lines.Where (line => line.Length > 0));
    }

    [Fact]
    public void GridTable_Open_Last_Cell_Does_Not_Consume_Next_Line ()
    {
        using Terminal.Gui.Views.Markdown view = new ()
        {
            Text = "+-----+----------+\n| 6   |  1\n|     |\n+-----+----------+", Width = 60, Height = 20
        };
        view.Layout ();
        MarkdownTable table = Assert.Single (view.SubViews.OfType<MarkdownTable> ());

        Assert.Empty (table.TableData.Headers);
        Assert.Equal (["6", "1"], Assert.Single (table.TableData.Rows));
    }

    [Fact]
    public void PipeTable_After_Paragraph_Preserves_Code_And_Unmatched_Subscript ()
    {
        using Terminal.Gui.Views.Markdown view = new ()
        {
            Text = "paragraph\n| Code | Cost |\n| --- | --- |\n| `value` | **~$1.50** |", Width = 60, Height = 20
        };
        view.Layout ();
        MarkdownTable table = Assert.Single (view.SubViews.OfType<MarkdownTable> ());

        Assert.Equal (["Code", "Cost"], table.TableData.Headers);
        Assert.Equal (["`value`", "**~$1.50**"], Assert.Single (table.TableData.Rows));
    }

    [Fact]
    public void Custom_Pipeline_Strict_Gfm_Tables_Normalize_Body_Columns ()
    {
        MarkdownPipeline pipeline = new MarkdownPipelineBuilder ().UsePipeTables (new PipeTableOptions { UseGfmRules = true }).Build ();
        using Terminal.Gui.Views.Markdown view = new ()
        {
            Text = "| A | B |\n| --- | --- |\n| one | two | extra |\n| short |", MarkdownPipeline = pipeline, Width = 60, Height = 20
        };
        view.Layout ();
        MarkdownTable table = Assert.Single (view.SubViews.OfType<MarkdownTable> ());

        Assert.Equal (["one", "two"], table.TableData.Rows [0]);
        Assert.Equal (["short", ""], table.TableData.Rows [1]);
    }

    [Fact]
    public void Custom_Pipeline_Emoji_Preserves_Table_Alignment_And_Content ()
    {
        MarkdownPipeline pipeline = new MarkdownPipelineBuilder ().UseEmojiAndSmiley ().UsePipeTables ().Build ();
        using Terminal.Gui.Views.Markdown view = new ()
        {
            Text = "| Left | Center | Right |\n| --- | :---: | ---: |\n| :x: | b | c |", MarkdownPipeline = pipeline, Width = 60, Height = 20
        };
        view.Layout ();
        MarkdownTable table = Assert.Single (view.SubViews.OfType<MarkdownTable> ());

        Assert.Equal ([Alignment.Start, Alignment.Center, Alignment.End], table.TableData.ColumnAlignments);
        Assert.Equal (["❌", "b", "c"], Assert.Single (table.TableData.Rows));
    }

    [Fact]
    public void Custom_Pipeline_Cjk_Emphasis_Preserves_Supplementary_Characters ()
    {
        MarkdownPipeline pipeline = new MarkdownPipelineBuilder ().UseCjkFriendlyEmphasis ().Build ();

        Assert.Equal (["𠮷SurrogatePair"], RenderLines ("𠮷**SurrogatePair**", pipeline: pipeline));
    }

    [Fact]
    public void Custom_Pipeline_Nesting_Limit_Is_Respected ()
    {
        MarkdownPipeline pipeline = new MarkdownPipelineBuilder { MaximumNestingDepth = 16 }.Build ();
        using Terminal.Gui.Views.Markdown view = new () { Width = 60, Height = 20, MarkdownPipeline = pipeline, Text = new string ('>', 20) };

        Assert.Throws<ArgumentException> (() => Markdig.Markdown.Parse (view.Text, pipeline));
        view.Layout ();

        Assert.Equal ([new string ('>', 20)], RenderLines (view.Text, pipeline: pipeline));
    }

    [Fact]
    public void Default_Pipeline_Deep_Nesting_Renders_Literal_Source ()
    {
        string source = new string ('>', 140) + " **literal**";
        using Terminal.Gui.Views.Markdown view = new ()
        {
            Text = source,
            Width = 200,
            Height = 10
        };
        view.Layout ();

        Assert.Equal ([source], RenderLines (source, 200));
        Assert.False (view.ScrollToAnchor ("literal"));
    }

    [Fact]
    public void Heading_AutoIdentifier_Does_Not_Consume_Nested_Link_Label ()
    {
        string [] lines = RenderLines ("### Header\n\n[Testing [Header]](https://example.com)");

        Assert.Equal ("Testing [Header]", lines [^1]);
    }

    [Fact]
    public void Unmatched_Emphasis_Closer_Preserves_Trailing_Text ()
    {
        Assert.Equal (["=a=== tail"], RenderLines ("===a===== tail"));
    }

    [Theory]
    [InlineData ("HTML supports **bold** and *italic*", "HTML supports bold and italic")]
    [InlineData ("**HTML is great**", "HTML is great")]
    [InlineData ("HTML supports **bold** and **HTML too**", "HTML supports bold and HTML too")]
    public void Abbreviations_Preserve_Label_And_Adjacent_Emphasis (string markdown, string expected)
    {
        Assert.Equal ([expected], RenderLines ($"*[HTML]: HyperText Markup Language\n\n{markdown}"));
    }

    [Fact]
    public void Abbreviations_In_Table_Cells_Preserve_Label ()
    {
        using Terminal.Gui.Views.Markdown view = new ()
        {
            Text = "*[HTML]: HyperText Markup Language\n\n| Name |\n| --- |\n| **HTML** |", Width = 60, Height = 20
        };
        view.Layout ();
        MarkdownTable table = Assert.Single (view.SubViews.OfType<MarkdownTable> ());

        Assert.Equal (["**HTML**"], Assert.Single (table.TableData.Rows));
    }

    private static string [] RenderLines (string markdown, int width = 80, MarkdownPipeline? pipeline = null)
    {
        using Terminal.Gui.Views.Markdown view = new () { MarkdownPipeline = pipeline };
        string ansi = view.RenderToAnsi (markdown, width);
        string plain = Regex.Replace (ansi, @"\x1b\[[0-?]*[ -/]*[@-~]", string.Empty);
        plain = Regex.Replace (plain, @"\x1b\][^\x07\x1b]*(?:\x07|\x1b\\)", string.Empty);

        return [.. plain.Replace ("\r\n", "\n").TrimEnd ().Split ('\n').Select (line => line.TrimEnd ())];
    }
}
