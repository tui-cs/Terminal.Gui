using Markdig;
using Markdig.Extensions.Tables;
using UnitTests;

namespace ViewsTests.Markdown;

// CoPilot - GPT-6
public class MarkdownGridFixturesTests : TestDriverBase
{
    internal const string Headerless = "+-------+-------+\n| A     | B     |\n+-------+-------+\n| C     | D     |\n+-------+-------+";
    internal const string ColumnSpan = "+-------+-------+\n| A     | B     |\n+=======+=======+\n| Cell spanning |\n+-------+-------+";
    internal const string RowSpan = "+-------+-------+\n| A     | B     |\n+       +-------+\n|       | C     |\n+-------+-------+";
    internal const string MultipleHeaders = "+-------+-------+\n| A     | B     |\n+-------+-------+\n| C     | D     |\n+=======+=======+\n| E     | F     |\n+-------+-------+";

    [Fact]
    public void Inference_Is_Opt_In_And_Produces_Percentages ()
    {
        const string source = "| A | B |\n| ----- | -------------------- |\n| x | y |";
        Table ordinary = Parse (source);
        MarkdownPipeline pipeline = new MarkdownPipelineBuilder ().UsePipeTables (new PipeTableOptions { InferColumnWidthsFromSeparator = true }).Build ();
        Table inferred = Assert.IsType<Table> (Assert.Single (Markdig.Markdown.Parse (source, pipeline)));

        Assert.All (ordinary.ColumnDefinitions, column => Assert.Equal (0, column.Width));
        Assert.Equal ([20f, 80f], inferred.ColumnDefinitions.Select (column => column.Width));
    }

    [Fact]
    public void Grid_Fixtures_Expose_Real_Header_And_Span_Metadata ()
    {
        Assert.All (Parse (Headerless).Cast<TableRow> (), row => Assert.False (row.IsHeader));
        Table spanning = Parse (ColumnSpan);
        Assert.True (((TableRow)spanning [0]).IsHeader);
        Assert.Equal (2, ((TableCell)((TableRow)spanning [1]) [0]).ColumnSpan);
        Assert.Equal (2, ((TableCell)((TableRow)Parse (RowSpan) [0]) [0]).RowSpan);
        Assert.Equal ([true, true, false], Parse (MultipleHeaders).Cast<TableRow> ().Select (row => row.IsHeader));
        Assert.Equal ([50f, 50f], spanning.ColumnDefinitions.Select (column => column.Width));
    }

    [Fact]
    public void Legacy_Inline_Sources_And_Reassignment_Keep_Their_Contract ()
    {
        using IDriver driver = CreateTestDriver (20, 12);
        TableData data = new (["H"], [Alignment.Start], [["- literal"]]);
        using MarkdownTable table = new () { Driver = driver, Width = 20, TableData = data };
        table.Layout ();
        table.Draw ();
        Assert.Equal ("- literal", ReadBody ());
        Assert.Equal (5, MarkdownTable.CalculateTableHeight (data));

        data.Rows [0] [0] = "**changed**";
        table.TableData = data;
        table.Layout ();
        table.SetClipToScreen ();
        table.SetNeedsDraw ();
        table.Draw ();
        Assert.Equal ("changed", ReadBody ());

        return;

        string ReadBody () => string.Concat (Enumerable.Range (2, 15).Select (x => driver.Contents! [3, x].Grapheme)).TrimEnd (' ', '│');
    }

    private static Table Parse (string source) => Assert.IsType<Table> (Assert.Single (Markdig.Markdown.Parse (source, new MarkdownPipelineBuilder ().UseAdvancedExtensions ().Build ())));
}
