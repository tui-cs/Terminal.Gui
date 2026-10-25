using UnitTests;
using TgMarkdown = Terminal.Gui.Views.Markdown;

namespace ViewsTests.Markdown;

// CoPilot - GPT-6
public class MarkdownTableRoundTripTests : TestDriverBase
{
    [Theory]
    [InlineData ("a\\|b", "a|b", MarkdownStyleRole.Normal)]
    [InlineData ("\\*literal\\*", "*literal*", MarkdownStyleRole.Normal)]
    [InlineData ("``a`b``", "a`b", MarkdownStyleRole.InlineCode)]
    [InlineData ("``a\\|b`c``", "a|b`c", MarkdownStyleRole.InlineCode)]
    public void Table_Copy_Round_Trips_Literal_Text_And_Code (string cell, string expected, MarkdownStyleRole role)
    {
        using TgMarkdown view = new ()
        {
            Text = $"before\n\n| H |\n| --- |\n| {cell} |\n\nafter",
            Width = 60,
            Height = 20
        };
        view.Layout ();
        MarkdownTable table = Assert.Single (view.SubViews.OfType<MarkdownTable> ());
        string copy = SelectRows (view, table.Frame.Y, table.Frame.Y + table.RenderedHeight - 1);
        TableData data = Assert.IsType<TableData> (TableData.TryParse (copy.Split ('\n')));
        List<InlineRun> runs = MarkdownInlineParser.ParseInlines (data.Rows [0] [0], MarkdownStyleRole.Normal);

        Assert.Equal (1, data.ColumnCount);
        Assert.Equal (expected, string.Concat (runs.Select (run => run.Text)));
        Assert.All (runs, run => Assert.Equal (role, run.StyleRole));

        using TgMarkdown roundTrip = new ()
        {
            Text = copy,
            Width = 60,
            Height = 20
        };
        roundTrip.Layout ();
        MarkdownTable reparsed = Assert.Single (roundTrip.SubViews.OfType<MarkdownTable> ());
        Assert.Equal (table.TableData.Rows [0], reparsed.TableData.Rows [0]);

        using IDriver driver = CreateTestDriver (60, 10);
        using MarkdownTable standalone = new ()
        {
            Driver = driver,
            TableData = data,
            Width = 60
        };
        standalone.Layout ();
        Assert.Equal (MarkdownTable.ComputeColumnWidths (table.Content, 60), MarkdownTable.ComputeColumnWidths (standalone.Content, 60));
        standalone.Draw ();

        Assert.Equal (expected, ReadCellText ());

        standalone.TableData = table.TableData;
        standalone.Layout ();
        standalone.SetClipToScreen ();
        standalone.SetNeedsDraw ();
        standalone.Draw ();

        Assert.Equal (expected, ReadCellText ());

        return;

        string ReadCellText () => string.Concat (Enumerable.Range (2, expected.Length).Select (column => driver.Contents! [3, column].Grapheme));
    }

    private static string SelectRows (TgMarkdown view, int first, int last)
    {
        view.NewMouseEvent (new Mouse
        {
            Position = new Point (0, first),
            Flags = MouseFlags.LeftButtonPressed
        });
        view.NewMouseEvent (new Mouse
        {
            Position = new Point (59, last),
            Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport
        });

        return Assert.IsType<string> (view.SelectedText);
    }
}
