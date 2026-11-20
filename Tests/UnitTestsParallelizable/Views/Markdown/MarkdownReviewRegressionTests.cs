using System.Text.RegularExpressions;
using Markdig;
using TgMarkdown = Terminal.Gui.Views.Markdown;

namespace ViewsTests.Markdown;

// CoPilot - GPT-6
public class MarkdownReviewRegressionTests
{
    [Theory]
    [InlineData ("- Term\n  : definition", new string [] { "• Term", "    definition" })]
    [InlineData ("- First\n  : one\n  : two\n\n  Second\n  : three", new string [] { "• First", "    one", "    two", "  Second", "    three" })]
    public void Definition_List_In_List_Uses_One_Bullet (string source, string [] expected)
    {
        Assert.Equal (expected, RenderLines (source).Where (line => line.Length > 0));
    }

    [Fact]
    public void Alert_In_List_Uses_One_Bullet ()
    {
        Assert.Equal (["• > NOTE", "  > first", "  > second"], RenderLines ("- > [!NOTE]\n  > first\n  >\n  > second", new MarkdownPipelineBuilder ().UseAlertBlocks (allowNestedAlerts: true).Build ()).Where (line => line.Length > 0));
    }

    [Theory]
    [InlineData ("\n\ntext", "", 3)]
    [InlineData ("\ntext", "", 2)]
    [InlineData ("text\n", "• ", 1)]
    public void Hard_Breaks_Keep_Nonempty_Segments_Without_Trailing_Phantom_Line (string text, string prefix, int count)
    {
        IntermediateBlock block = new ([new InlineRun (text, MarkdownStyleRole.Emphasis, "https://example.com")], true, prefix, "  ");
        List<RenderedLine> lines = TgMarkdown.WrapBlock (block, 80);

        Assert.Equal (count, lines.Count);
        Assert.All (lines, line => Assert.NotEmpty (line.Segments));
    }

    [Theory]
    [InlineData ("*\\\ntext*")]
    [InlineData ("*\\\n\\\ntext*")]
    [InlineData ("first  \n\nlast")]
    [InlineData ("[first  \nsecond](https://example.com)")]
    public void Hard_Break_Lines_Allow_Selection_And_Hit_Testing (string source)
    {
        using TgMarkdown view = new ()
        {
            Text = source,
            Width = 40,
            Height = 10
        };
        view.Layout ();
        view.NewMouseEvent (new Mouse
        {
            Position = new Point (0, 0),
            Flags = MouseFlags.LeftButtonPressed
        });
        view.NewMouseEvent (new Mouse
        {
            Position = new Point (1, 1),
            Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport
        });

        Assert.NotNull (view.SelectedText);
    }

    private static string [] RenderLines (string source, MarkdownPipeline? pipeline = null)
    {
        using TgMarkdown view = new ()
        {
            MarkdownPipeline = pipeline
        };
        string ansi = view.RenderToAnsi (source);
        string plain = Regex.Replace (ansi, @"\x1b\[[0-?]*[ -/]*[@-~]", string.Empty);

        return [.. plain.Replace ("\r\n", "\n").TrimEnd ().Split ('\n').Select (line => line.TrimEnd ())];
    }
}
