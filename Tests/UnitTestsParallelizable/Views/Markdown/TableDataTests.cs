using JetBrains.Annotations;

namespace ViewsTests.Markdown;

[TestSubject (typeof (TableData))]
public class TableDataTests
{
    // Copilot

    [Fact]
    public void TryParse_Valid_Two_Column_Table ()
    {
        List<string> lines = ["| Name | Value |", "|------|-------|", "| A    | 1     |", "| B    | 2     |"];

        TableData? data = TableData.TryParse (lines);

        Assert.NotNull (data);
        Assert.Equal (2, data.ColumnCount);
        Assert.Equal ("Name", data.Headers [0]);
        Assert.Equal ("Value", data.Headers [1]);
        Assert.Equal (2, data.Rows.Length);
        Assert.Equal ("A", data.Rows [0] [0]);
        Assert.Equal ("2", data.Rows [1] [1]);
    }

    [Fact]
    public void TryParse_Returns_Null_For_Single_Line ()
    {
        List<string> lines = ["| only header |"];

        TableData? data = TableData.TryParse (lines);

        Assert.Null (data);
    }

    [Fact]
    public void TryParse_Returns_Null_When_Separator_Invalid ()
    {
        List<string> lines = ["| H1 | H2 |", "| not dashes |", "| A  | B  |"];

        TableData? data = TableData.TryParse (lines);

        Assert.Null (data);
    }

    [Fact]
    public void TryParse_Parses_Alignment_Markers ()
    {
        List<string> lines = ["| Left | Center | Right |", "|:-----|:------:|------:|", "| a    | b      | c     |"];

        TableData? data = TableData.TryParse (lines);

        Assert.NotNull (data);
        Assert.Equal (3, data.ColumnCount);
        Assert.Equal (Alignment.Start, data.ColumnAlignments [0]);
        Assert.Equal (Alignment.Center, data.ColumnAlignments [1]);
        Assert.Equal (Alignment.End, data.ColumnAlignments [2]);
    }

    [Fact]
    public void TryParse_Normalizes_Mismatched_Column_Count ()
    {
        List<string> lines = ["| H1 | H2 | H3 |", "|-----|-----|-----|", "| only one |"];

        TableData? data = TableData.TryParse (lines);

        Assert.NotNull (data);
        Assert.Equal (3, data.ColumnCount);
        Assert.Single (data.Rows);
        Assert.Equal ("only one", data.Rows [0] [0]);
        Assert.Equal (string.Empty, data.Rows [0] [1]);
        Assert.Equal (string.Empty, data.Rows [0] [2]);
    }

    [Fact]
    public void TryParse_Header_Only_Table ()
    {
        List<string> lines = ["| H1 | H2 |", "|-----|-----|"];

        TableData? data = TableData.TryParse (lines);

        Assert.NotNull (data);
        Assert.Equal (2, data.ColumnCount);
        Assert.Empty (data.Rows);
    }
}
