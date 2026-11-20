using Markdig;
using Markdig.Extensions.Tables;
using UnitTests;
using TgAttribute = Terminal.Gui.Drawing.Attribute;
using TgMarkdown = Terminal.Gui.Views.Markdown;

namespace ViewsTests.Markdown;

// CoPilot - GPT-6
public class MarkdownTableReviewTests : TestDriverBase
{
    [Theory]
    [InlineData ("- supercalifragilistic", "• supercalifragilistic", 22)]
    [InlineData ("1. supercalifragilistic", "1. supercalifragilistic", 23)]
    [InlineData ("> supercalifragilistic", "> supercalifragilistic", 22)]
    public void Preferred_Minimum_Includes_Block_Prefixes (string source, string expected, int contentWidth)
    {
        using MarkdownTable table = new ()
        {
            Width = contentWidth + 8,
            TableData = TableData.CreateGrid ([new (), new ()],
                                             [new (false, [new (0, source + " extra"), new (1, "x")])])
        };
        table.Layout ();

        Assert.Equal ([contentWidth + 2, 3], table.TableLayout.ColumnWidths);
        Assert.Equal (expected, table.TableLayout.SelectLine (1, 2, contentWidth + 2));
        Assert.Equal (2, table.TableLayout.RowHeights [0]);
    }

    [Fact]
    public void Nested_List_Uses_The_Outer_Marker_Only_On_Its_First_Item ()
    {
        using MarkdownTable table = CreateCell ("1. - a\n   - b");

        Assert.Equal (["1. • a", "   • b"], ContentLines (table));
    }

    [Fact]
    public void Loose_List_Separates_Items_With_A_Blank_Line ()
    {
        using MarkdownTable table = CreateCell ("- a\n\n- b");

        Assert.Equal (["• a", "", "• b"], ContentLines (table));
    }

    [Theory]
    [InlineData ("> ", "> ")]
    [InlineData ("- ", "  ")]
    [InlineData ("> - ", ">   ")]
    [InlineData ("-\t", "\t")]
    [InlineData ("> ", ">")]
    public void Complete_Nested_Grid_Copy_Removes_Enclosing_Syntax (string firstPrefix, string continuation)
    {
        string source = MarkdownGridFixturesTests.ColumnSpan;
        using TgMarkdown view = CreateNestedTable (source, firstPrefix, continuation);
        MarkdownTable table = Assert.Single (view.SubViews.OfType<MarkdownTable> ());
        SelectTable (view, table);

        Assert.Equal (source, view.SelectedText);
        using TgMarkdown copied = new () { Text = Assert.IsType<string> (view.SelectedText), Width = 40, Height = 30 };
        copied.Layout ();
        MarkdownTable roundTrip = Assert.Single (copied.SubViews.OfType<MarkdownTable> ());
        Assert.Equal (2, roundTrip.TableData.TableRows [1].Cells [0].ColumnSpan);
    }

    [Theory]
    [InlineData ("> ", "> ")]
    [InlineData ("- ", "  ")]
    [InlineData ("> - ", ">   ")]
    [InlineData ("-\t", "\t")]
    [InlineData ("> ", ">")]
    public void Complete_Nested_Pipe_Copy_Preserves_Standalone_Separator_Proportions (string firstPrefix, string continuation)
    {
        const string source = "| A | B |\n| ----- | -------------------- |\n| x | y |";
        using TgMarkdown view = CreateNestedTable (source, firstPrefix, continuation);
        MarkdownTable table = Assert.Single (view.SubViews.OfType<MarkdownTable> ());
        SelectTable (view, table);

        Assert.Equal (source, view.SelectedText);
        using TgMarkdown copied = new () { Text = Assert.IsType<string> (view.SelectedText), Width = 47, Height = 20, MarkdownPipeline = PipePipeline () };
        copied.Layout ();
        Assert.Equal ([10, 34], Assert.Single (copied.SubViews.OfType<MarkdownTable> ()).TableLayout.ColumnWidths);
    }

    [Fact]
    public void Width_Changes_Reuse_Highlighted_Cell_Content_And_Intrinsic_Bounds ()
    {
        CountingHighlighter highlighter = new ();
        const string source = "+-------------+\n| ```text     |\n| code        |\n| ```         |\n+-------------+";
        using TgMarkdown view = new () { Text = source, Width = 24, Height = 20, SyntaxHighlighter = highlighter };
        view.Layout ();
        MarkdownTableContent content = Assert.Single (view.SubViews.OfType<MarkdownTable> ()).Content;
        Assert.Equal (1, highlighter.HighlightCalls);

        int [] widths = [80, 24];

        foreach (int width in widths)
        {
            view.Width = width;
            view.Layout ();
            Assert.Same (content, Assert.Single (view.SubViews.OfType<MarkdownTable> ()).Content);
            Assert.Equal (1, highlighter.HighlightCalls);
        }

        CountingHighlighter replacement = new ();
        view.SyntaxHighlighter = replacement;
        view.Layout ();
        Assert.Equal (1, replacement.HighlightCalls);
        Assert.NotSame (content, Assert.Single (view.SubViews.OfType<MarkdownTable> ()).Content);
        view.UseThemeBackground = !view.UseThemeBackground;
        view.Layout ();
        Assert.Equal (2, replacement.HighlightCalls);
        view.Text = source.Replace ("code", "text", StringComparison.Ordinal);
        view.Layout ();
        Assert.Equal (3, replacement.HighlightCalls);
    }

    [Fact]
    public void Drawing_Resolves_Attributes_Per_Style_And_Refreshes_Them_On_Redraw ()
    {
        CountingHighlighter highlighter = new ();
        using IDriver driver = CreateTestDriver (40, 10);
        using MarkdownTable table = new ()
        {
            Driver = driver,
            Width = 40,
            SyntaxHighlighter = highlighter,
            TableData = TableData.CreateGrid ([new ()], [new (false, [new (0, "[many letters](https://example.com)")])])
        };
        table.Layout ();
        table.Draw ();
        Assert.Equal (1, highlighter.ScopeCalls);
        Assert.Equal (Color.Red, driver.Contents! [1, 2].Attribute!.Value.Foreground);
        highlighter.Foreground = Color.Blue;
        table.SetNeedsDraw ();
        table.SetClipToScreen ();
        table.Draw ();
        Assert.Equal (2, highlighter.ScopeCalls);
        Assert.Equal (Color.Blue, driver.Contents! [1, 2].Attribute!.Value.Foreground);
    }

    [Theory]
    [InlineData ("https://example.com/path", true)]
    [InlineData ("mailto:someone@example.com", true)]
    [InlineData ("relative/path", false)]
    [InlineData ("#heading", false)]
    [InlineData ("javascript:alert", false)]
    public void Table_Drawing_Emits_Osc8_Only_For_Safe_Absolute_Targets (string url, bool absolute)
    {
        using IDriver driver = CreateTestDriver (40, 10);
        using MarkdownTable table = new ()
        {
            Driver = driver,
            Width = 40,
            TableData = TableData.CreateGrid ([new ()], [new (false, [new (0, $"[linked text]({url})")])])
        };
        table.Layout ();
        table.Draw ();
        driver.Refresh ();
        string output = driver.GetOutput ().GetLastOutput ();

        Assert.Equal (absolute, output.Contains (EscSeqUtils.OSC_StartHyperlink (url), StringComparison.Ordinal));
        Assert.Null (driver.CurrentUrl);
    }

    [Fact]
    public void Selecting_Only_The_First_Column_Of_The_Last_Table_Row_Is_Not_SelectAll ()
    {
        using TgMarkdown view = new () { Text = "| H |\n| --- |\n| body |", Width = 30, Height = 20 };
        view.Layout ();
        MarkdownTable table = Assert.Single (view.SubViews.OfType<MarkdownTable> ());
        view.NewMouseEvent (new Mouse { Position = Point.Empty, Flags = MouseFlags.LeftButtonPressed });
        view.NewMouseEvent (new Mouse { Position = new (0, table.RenderedHeight - 1), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport });

        Assert.DoesNotContain ("|", view.SelectedText);
        Assert.Contains ("body", view.SelectedText);
        view.SelectAll ();
        Assert.Equal (view.Text, view.SelectedText);
    }

    [Theory]
    [InlineData ("| A | B |\n| --- | --- |\n| x | y |", "A\n\nB\n\nx\n\ny")]
    [InlineData ("<div>html</div>", "<div>html</div>")]
    [InlineData ("---", "")]
    [InlineData ("", "")]
    public void Unsupported_And_Empty_Blocks_Have_A_Deterministic_Text_Fallback (string source, string expected)
    {
        using MarkdownTable table = CreateCell (source);

        Assert.Equal (expected, string.Join ('\n', ContentLines (table)));
    }

    private static MarkdownTable CreateCell (string source)
    {
        MarkdownTable table = new () { Width = 40, TableData = TableData.CreateGrid ([new ()], [new (false, [new (0, source)])]) };
        table.Layout ();

        return table;
    }

    private static string [] ContentLines (MarkdownTable table) =>
        [.. Enumerable.Range (1, table.RenderedHeight - 2).Select (row => table.TableLayout.SelectLine (row, 2, 39))];

    private static MarkdownPipeline PipePipeline () =>
        new MarkdownPipelineBuilder ().UsePipeTables (new PipeTableOptions { InferColumnWidthsFromSeparator = true }).UseGridTables ().Build ();

    private static TgMarkdown CreateNestedTable (string source, string firstPrefix, string continuation)
    {
        TgMarkdown view = new ()
        {
            Text = "before\n\n" + firstPrefix + source.Replace ("\n", "\n" + continuation, StringComparison.Ordinal) + "\n\nafter",
            Width = 47,
            Height = 30,
            MarkdownPipeline = PipePipeline ()
        };
        view.Layout ();

        return view;
    }

    private static void SelectTable (TgMarkdown view, MarkdownTable table)
    {
        view.NewMouseEvent (new Mouse { Position = new (0, table.Frame.Y), Flags = MouseFlags.LeftButtonPressed });
        view.NewMouseEvent (new Mouse { Position = new (46, table.Frame.Bottom - 1), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport });
    }

    private sealed class CountingHighlighter : ISyntaxHighlighter
    {
        public int HighlightCalls { get; private set; }
        public int ScopeCalls { get; private set; }
        public Color Foreground { get; set; } = Color.Red;
        public string ThemeName => "Test";
        public Color? DefaultBackground => null;

        public IReadOnlyList<StyledSegment> Highlight (string code, string? language)
        {
            HighlightCalls++;

            return [new (code, MarkdownStyleRole.CodeBlock)];
        }

        public void ResetState ()
        {
        }

        public TgAttribute? GetAttributeForScope (MarkdownStyleRole role)
        {
            ScopeCalls++;

            return new (Foreground, Color.Black);
        }
    }
}
