using UnitTests;
using TgMarkdown = Terminal.Gui.Views.Markdown;

namespace ViewsTests.Markdown;

// CoPilot - GPT-6
public class MarkdownInlineRoundTripTests : TestDriverBase
{
    [Theory]
    [InlineData ("**a\\|b**", "a|b", MarkdownStyleRole.Strong)]
    [InlineData ("*x\\*y*", "x*y", MarkdownStyleRole.Emphasis)]
    [InlineData ("[Testing \\[Header\\]](https://example.com/path_(part))", "Testing [Header]", MarkdownStyleRole.Link)]
    [InlineData ("![alt\\]text](image_(part).png)", "[alt]text]", MarkdownStyleRole.ImageAlt)]
    public void String_Inlines_Respect_Escaped_Delimiters (string source, string expected, MarkdownStyleRole role)
    {
        List<InlineRun> runs = MarkdownInlineParser.ParseInlines (source, MarkdownStyleRole.Normal);

        Assert.Equal (expected, string.Concat (runs.Select (run => run.Text)));
        Assert.All (runs, run => Assert.Equal (role, run.StyleRole));

        if (role == MarkdownStyleRole.Link)
        {
            Assert.All (runs, run => Assert.Equal ("https://example.com/path_(part)", run.Url));
        }

        if (role == MarkdownStyleRole.ImageAlt)
        {
            Assert.Equal ("image_(part).png", Assert.Single (runs).ImageSource);
        }
    }

    [Theory]
    [InlineData ("| a \\|", "a \\|")]
    [InlineData ("| a \\||", "a \\|")]
    [InlineData ("| a \\\\|", "a \\\\")]
    public void Row_Boundaries_Preserve_Escaped_Pipes (string row, string expected)
    {
        TableData data = Assert.IsType<TableData> (TableData.TryParse (["| H |", "| --- |", row]));

        Assert.Equal (expected, data.Rows [0] [0]);
    }

    [Theory]
    [InlineData ("|| H |", 2)]
    [InlineData ("| H ||", 2)]
    [InlineData ("||", 1)]
    public void Row_Boundaries_Remove_Only_One_Outer_Pipe (string header, int columns)
    {
        TableData data = Assert.IsType<TableData> (TableData.TryParse ([header, "| --- | --- |"]));

        Assert.Equal (columns, data.ColumnCount);
    }

    [Theory]
    [InlineData ("snake_case")]
    [InlineData ("[1]")]
    public void Harmless_Literals_Do_Not_Gain_Unnecessary_Escapes (string source)
    {
        using TgMarkdown view = CreateTable (source);
        MarkdownTable table = Assert.Single (view.SubViews.OfType<MarkdownTable> ());

        Assert.Equal (source, table.TableData.Rows [0] [0]);
    }

    [Theory]
    [InlineData ("$x^2$")]
    [InlineData ("Wow\\![docs](https://x)")]
    [InlineData ("Wow&#33;[docs](https://x)")]
    [InlineData ("[`a\\|b`](u)")]
    [InlineData ("a\\|b")]
    [InlineData ("**a\\|b**")]
    [InlineData ("\\*literal\\*")]
    [InlineData ("*x\\*y*")]
    [InlineData ("[Testing \\[Header\\]](https://example.com/path_(part))")]
    [InlineData ("[Testing [Header]](https://example.com/path_(part))")]
    [InlineData ("[label](https://example.com/a\\|b)")]
    [InlineData ("[label](https://example.com/a\\))")]
    [InlineData ("**bold *inner***")]
    [InlineData ("``a\\|b`c``")]
    [InlineData ("![alt\\]text](image_(part).png)")]
    [InlineData ("\\~literal\\~")]
    [InlineData ("\\^literal\\^")]
    [InlineData ("\\=\\=literal\\=\\=")]
    public void Table_String_And_Ast_Paths_Produce_Identical_Styled_Content (string source)
    {
        using TgMarkdown view = CreateTable (source);
        MarkdownTable parsed = Assert.Single (view.SubViews.OfType<MarkdownTable> ());
        using MarkdownTable standalone = new ()
        {
            TableData = parsed.TableData
        };
        TableData copied = Assert.IsType<TableData> (TableData.TryParse (standalone.Text.Split ('\n')));
        using MarkdownTable reparsed = new ()
        {
            TableData = copied
        };

        Assert.Equal (Expand (GetBody (parsed)), Expand (GetBody (standalone)));
        Assert.Equal (Expand (GetBody (parsed)), Expand (GetBody (reparsed)));
    }

    [Theory]
    [InlineData ("``a`", false)]
    [InlineData ("```x`", false)]
    [InlineData ("``a``", true)]
    [InlineData ("`", false)]
    public void Backtick_Runs_Are_Matched_Whole_Or_Kept_Literal (string source, bool code)
    {
        List<InlineRun> runs = MarkdownInlineParser.ParseInlines (source, MarkdownStyleRole.Normal);

        Assert.Equal (code ? "a" : source, string.Concat (runs.Select (run => run.Text)));
        Assert.All (runs, run => Assert.Equal (code ? MarkdownStyleRole.InlineCode : MarkdownStyleRole.Normal, run.StyleRole));
    }

    [Theory]
    [InlineData ("$x^2$", "$x^2$")]
    [InlineData ("before $x^2$ after", "before $x^2$ after")]
    [InlineData ("\\{note\\}", "{note}")]
    public void Table_Cell_Text_Survives_Advanced_Inline_Parsing (string source, string expected)
    {
        using TgMarkdown view = CreateTable (source);
        MarkdownTable parsed = Assert.Single (view.SubViews.OfType<MarkdownTable> ());
        using MarkdownTable standalone = new ()
        {
            TableData = new (["H"], [Alignment.Start], [[source]])
        };

        Assert.Equal (expected, string.Concat (GetBody (parsed).Select (segment => segment.Text)));
        Assert.Equal (expected, string.Concat (GetBody (standalone).Select (segment => segment.Text)));
    }

    [Fact]
    public void Unescaped_Table_Attributes_Are_Metadata_In_Both_Paths ()
    {
        using TgMarkdown view = CreateTable ("**bold**{note}");
        MarkdownTable parsed = Assert.Single (view.SubViews.OfType<MarkdownTable> ());
        List<InlineRun> standalone = MarkdownInlineParser.ParseInlines ("**bold**{note}", MarkdownStyleRole.Normal);

        Assert.Equal ("bold", string.Concat (GetBody (parsed).Select (segment => segment.Text)));
        Assert.Equal ("bold", string.Concat (standalone.Select (run => run.Text)));
    }

    [Fact]
    public void Linked_Table_Code_Unescapes_Pipe_Before_Applying_Link_Role ()
    {
        using TgMarkdown view = CreateTable ("[`a\\|b`](u)");
        MarkdownTable table = Assert.Single (view.SubViews.OfType<MarkdownTable> ());
        StyledSegment segment = Assert.Single (GetBody (table));

        Assert.Equal ("a|b", segment.Text);
        Assert.Equal (MarkdownStyleRole.Link, segment.StyleRole);
        Assert.Equal ("u", segment.Url);
    }

    [Fact]
    public void Linked_Code_Outside_Tables_Preserves_Literal_Backslash ()
    {
        using IDriver driver = CreateTestDriver (60, 10);
        using TgMarkdown view = new ()
        {
            Driver = driver,
            Text = "[`a\\|b`](u)",
            Width = 60,
            Height = 10
        };
        view.Layout ();
        view.Draw ();

        Assert.Equal ("a\\|b", string.Concat (Enumerable.Range (0, 4).Select (column => driver.Contents! [0, column].Grapheme)));
    }

    private static TgMarkdown CreateTable (string cell)
    {
        TgMarkdown view = new ()
        {
            Text = $"| H |\n| --- |\n| {cell} |",
            Width = 60,
            Height = 20
        };
        view.Layout ();

        return view;
    }

    private static List<StyledSegment> GetBody (MarkdownTable table) => table.Content.Rows [0] [0];

    private static IEnumerable<(string, MarkdownStyleRole, string?, string?, TextStyle, VisualRole?)> Expand (IEnumerable<StyledSegment> segments) =>
        segments.SelectMany (segment => GraphemeHelper.GetGraphemes (segment.Text)
                                                     .Select (grapheme => (grapheme, segment.StyleRole, segment.Url, segment.ImageSource, segment.AdditionalStyle, segment.Role)));
}
