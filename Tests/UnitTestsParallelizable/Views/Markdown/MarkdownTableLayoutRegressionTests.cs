using UnitTests;

namespace ViewsTests.Markdown;

// CoPilot - GPT-6
public class MarkdownTableLayoutRegressionTests : TestDriverBase
{
    [Theory]
    [InlineData (MarkdownStyleRole.Strong)]
    [InlineData (MarkdownStyleRole.Link)]
    public void Wrapping_Keeps_A_Word_Together_Across_Styles (MarkdownStyleRole role)
    {
        StyledSegment styled = new ("bo", role, "https://example.com")
        {
            AdditionalStyle = TextStyle.Italic
        };
        List<StyledSegment> segments = [new ("see ", MarkdownStyleRole.Normal), styled, new ("ld", MarkdownStyleRole.Normal)];
        List<List<StyledSegment>> lines = MarkdownTable.WrapSegments (segments, 6);

        Assert.Equal (["see", "bold"], lines.Select (line => string.Concat (line.Select (segment => segment.Text)).TrimEnd ()));
        StyledSegment wrapped = lines [1] [0];

        Assert.Equal (role, wrapped.StyleRole);
        Assert.Equal (styled.Url, wrapped.Url);
        Assert.Equal (styled.AdditionalStyle, wrapped.AdditionalStyle);
    }

    [Fact]
    public void Wrapping_Keeps_A_Styled_Combining_Mark_With_Its_Base ()
    {
        List<List<StyledSegment>> lines = MarkdownTable.WrapSegments ([new ("a", MarkdownStyleRole.Normal), new ("\u0301", MarkdownStyleRole.Strong)], 1);
        List<StyledSegment> line = Assert.Single (lines);

        Assert.Equal ("a\u0301", string.Concat (line.Select (segment => segment.Text)));
        Assert.Equal (MarkdownStyleRole.Strong, line [1].StyleRole);
    }

    [Theory]
    [InlineData ("界\nx", "界|x")]
    [InlineData ("界\n\nx", "界||x")]
    [InlineData ("abcdef\nx", "ab|cd|ef|x")]
    public void Hard_Wrapping_Does_Not_Add_A_Line_Before_A_Hard_Break (string text, string expected)
    {
        List<List<StyledSegment>> lines = MarkdownTable.WrapSegments ([new (text, MarkdownStyleRole.Normal)], text.StartsWith ("界", StringComparison.Ordinal) ? 1 : 2);

        Assert.Equal (expected, string.Join ('|', lines.Select (line => string.Concat (line.Select (segment => segment.Text)))));
    }

    [Theory]
    [InlineData ("see **bo**ld")]
    [InlineData ("see [bo](u)ld")]
    [InlineData ("see **b*o***ld")]
    public void Drawing_Keeps_A_Styled_Word_On_One_Row (string source)
    {
        using IDriver driver = CreateTestDriver (10, 10);
        using MarkdownTable table = new ()
        {
            Driver = driver,
            TableData = new (["H"], [Alignment.Start], [[source]]),
            Width = 10
        };
        table.Layout ();
        table.Draw ();

        Assert.Equal ("see", ReadText (3));
        Assert.Equal ("bold", ReadText (4));

        return;

        string ReadText (int row) => string.Concat (Enumerable.Range (2, 6).Select (column => driver.Contents! [row, column].Grapheme)).TrimEnd ();
    }

    [Theory]
    [InlineData ("\t")]
    [InlineData ("\n")]
    public void Wrapping_Uses_The_Same_Word_Boundaries_As_Measurement (string separator)
    {
        List<StyledSegment> segments = [new ($"a{separator}bbbbbbbb", MarkdownStyleRole.Normal)];
        int wordWidth = MarkdownTable.MeasureLongestWord (segments);
        List<List<StyledSegment>> lines = MarkdownTable.WrapSegments (segments, wordWidth);

        Assert.Equal (8, wordWidth);
        Assert.Equal (["a", "bbbbbbbb"], lines.Select (line => string.Concat (line.Select (segment => segment.Text)).TrimEnd ()));
    }

    [Fact]
    public void Resizing_Preserves_Cached_Width_Bounds_Across_Style_Boundaries ()
    {
        MarkdownTableContent content = new ([[new ("H", MarkdownStyleRole.Heading)]],
                                           [[[new ("long word", MarkdownStyleRole.Normal)]],
                                            [[new ("abc", MarkdownStyleRole.Normal), new ("def", MarkdownStyleRole.Strong)]]]);

        Assert.Equal ([11], MarkdownTable.ComputeColumnWidths (content, 80));
        Assert.Equal ([8], MarkdownTable.ComputeColumnWidths (content, 10));
        Assert.Equal ([11], MarkdownTable.ComputeColumnWidths (content, 80));
        Assert.Equal ([9], content.MaximumWidths);
        Assert.Equal ([6], content.MinimumWidths);
    }

    [Fact]
    public void Tab_Separated_Cell_Draws_The_Long_Word_Without_Splitting_It ()
    {
        using IDriver driver = CreateTestDriver (12, 10);
        using MarkdownTable table = new ()
        {
            Driver = driver,
            TableData = new (["H"], [Alignment.Start], [["a\tbbbbbbbb"]]),
            Width = 12
        };
        table.Layout ();
        table.Draw ();

        Assert.Equal ("a", ReadText (3).TrimEnd ());
        Assert.Equal ("bbbbbbbb", ReadText (4));

        return;

        string ReadText (int row) => string.Concat (Enumerable.Range (2, 8).Select (column => driver.Contents! [row, column].Grapheme));
    }
}
