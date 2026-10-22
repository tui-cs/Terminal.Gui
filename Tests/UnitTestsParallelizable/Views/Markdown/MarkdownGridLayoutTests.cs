using Markdig;
using Markdig.Extensions.Tables;
using UnitTests;
using TgMarkdown = Terminal.Gui.Views.Markdown;

namespace ViewsTests.Markdown;

// CoPilot - GPT-6
public class MarkdownGridLayoutTests : TestDriverBase
{
    [Fact]
    public void Header_Only_Legacy_Table_Retains_Its_Separator_And_Height ()
    {
        using MarkdownTable table = new () { Width = 20, TableData = new (["H"], [Alignment.Start], []) };
        table.Layout ();

        Assert.Equal (4, table.RenderedHeight);
        Assert.Equal (4, MarkdownTable.CalculateTableHeight (table.TableData));
        Assert.Contains (table.TableLayout.Borders, border => border.Orientation == Orientation.Horizontal && border.Start.Y == 2);
    }

    [Theory]
    [InlineData (47, 10, 34)]
    [InlineData (46, 10, 33)]
    public void Preferences_Allocate_Content_Space_With_Deterministic_Rounding (int width, int first, int second)
    {
        using MarkdownTable table = new ()
        {
            Width = width,
            TableData = TableData.CreateGrid ([new (preferredWidthPercentage: 20), new (preferredWidthPercentage: 80)],
                                             [new (false, [new (0, "A"), new (1, "B")])])
        };
        table.Layout ();

        Assert.Equal ([first, second], table.TableLayout.ColumnWidths);
        Assert.Equal (width, table.TableLayout.Width);
    }

    [Fact]
    public void Parsed_Inference_Reaches_The_Layout_Engine ()
    {
        using TgMarkdown view = new ()
        {
            Width = 47,
            Height = 12,
            Text = "| A | B |\n| ----- | -------------------- |\n| x | y |",
            MarkdownPipeline = new MarkdownPipelineBuilder ().UsePipeTables (new PipeTableOptions { InferColumnWidthsFromSeparator = true }).Build ()
        };
        view.Layout ();
        MarkdownTable table = Assert.Single (view.SubViews.OfType<MarkdownTable> ());

        Assert.Equal ([10, 34], table.TableLayout.ColumnWidths);
        Assert.Equal ([20d, 80d], table.TableData.Columns.Select (column => column.PreferredWidthPercentage));
    }

    [Theory]
    [InlineData (0, 0)]
    [InlineData (20, 0)]
    [InlineData (100, 0)]
    [InlineData (200, 400)]
    public void Partial_And_Unspecified_Preferences_Are_Bounded_At_Every_Width (double first, double second)
    {
        TableData data = TableData.CreateGrid ([new (preferredWidthPercentage: first), new (preferredWidthPercentage: second)],
                                             [new (false, [new (0, "longword"), new (1, "界")])]);
        using MarkdownTable table = new () { TableData = data };

        for (int width = 0; width < 80; width++)
        {
            table.Recalculate (width);
            Assert.All (table.TableLayout.ColumnWidths, value => Assert.True (value >= 3));
            Assert.True (table.TableLayout.ColumnWidths [1] >= 4);
            int occupied = table.TableLayout.ColumnWidths.Sum () + 3;
            Assert.True (occupied <= width || occupied == 10);
            Assert.All (table.TableLayout.Glyphs, glyph => Assert.True (glyph.Position.X + glyph.Width < table.TableLayout.Width));
        }
    }

    [Theory]
    [InlineData (-1)]
    [InlineData (double.NaN)]
    [InlineData (double.PositiveInfinity)]
    public void Invalid_Public_Preferences_Are_Rejected (double preference) =>
        Assert.Throws<ArgumentOutOfRangeException> (() => new TableColumnData (preferredWidthPercentage: preference));

    [Fact]
    public void Invalid_Spans_And_Overlaps_Are_Rejected_Before_Layout ()
    {
        Assert.Throws<ArgumentOutOfRangeException> (() => new TableCellData (0, "A", columnSpan: 0));
        Assert.Throws<ArgumentException> (() => TableData.CreateGrid ([new ()], [new (false, [new (0, "A", rowSpan: 2)])]));
        Assert.Throws<ArgumentException> (() => TableData.CreateGrid ([new (), new ()], [new (false, [new (0, "A", columnSpan: 2), new (1, "B")])]));
        Assert.Throws<ArgumentException> (() => TableData.CreateGrid ([new ()], [new (false, [new (int.MaxValue, "A", columnSpan: int.MaxValue)])]));
    }

    [Fact]
    public void Grid_Factory_Snapshots_Collections_And_Projects_Cell_Origins ()
    {
        List<TableCellData> cells = [new (0, "A", columnSpan: 2)];
        List<TableRowData> rows = [new (false, cells)];
        List<TableColumnData> columns = [new (), new ()];
        TableData data = TableData.CreateGrid (columns, rows);
        cells.Clear ();
        rows.Clear ();
        columns.Clear ();

        Assert.Equal (2, data.ColumnCount);
        Assert.Empty (data.Headers);
        Assert.Equal (["A", ""], Assert.Single (data.Rows));
        Assert.Single (Assert.Single (data.TableRows).Cells);
    }

    [Theory]
    [InlineData (MarkdownGridFixturesTests.Headerless, 4, 0)]
    [InlineData (MarkdownGridFixturesTests.MultipleHeaders, 6, 4)]
    public void Header_Status_Comes_From_The_Ast (string source, int cells, int headerGlyphs)
    {
        using TgMarkdown view = CreateView (source, 25);
        MarkdownTable table = Assert.Single (view.SubViews.OfType<MarkdownTable> ());

        Assert.Equal (cells, table.TableLayout.Cells.Count);
        Assert.Equal (headerGlyphs, table.TableLayout.Glyphs.Count (glyph => glyph.IsHeader && !string.IsNullOrWhiteSpace (glyph.Text)));
        Assert.Equal (cells, table.TableLayout.Glyphs.Count (glyph => !string.IsNullOrWhiteSpace (glyph.Text)));
    }

    [Fact]
    public void Column_Span_Removes_The_Internal_Vertical_Border ()
    {
        using TgMarkdown view = CreateView (MarkdownGridFixturesTests.ColumnSpan, 25);
        MarkdownTable table = Assert.Single (view.SubViews.OfType<MarkdownTable> ());
        MarkdownTableCellLayout merged = Assert.Single (table.TableLayout.Cells, cell => cell.Content.Definition.ColumnSpan == 2);
        int separatorX = table.TableLayout.ColumnWidths [0] + 1;

        Assert.DoesNotContain (table.TableLayout.Borders, border => border.Orientation == Orientation.Vertical
                                                                 && border.Start.X == separatorX
                                                                 && border.Start.Y + border.Length - 1 >= merged.Bounds.Top);
        Assert.Contains ("Cell spanning", string.Join ('\n', Enumerable.Range (merged.Bounds.Top, merged.Bounds.Height)
                                                                                      .Select (row => table.TableLayout.SelectLine (row, 0, 25))));
    }

    [Fact]
    public void Row_Span_Uses_The_Suppressed_Border_Row_As_Content ()
    {
        TableData data = TableData.CreateGrid ([new (), new ()],
                                             [new (false, [new (0, "one  \ntwo  \nthree", rowSpan: 2), new (1, "B")]),
                                              new (false, [new (1, "C")])]);
        using MarkdownTable table = new () { Width = 24, TableData = data };
        table.Layout ();
        MarkdownTableCellLayout spanning = Assert.Single (table.TableLayout.Cells, cell => cell.Content.Definition.RowSpan == 2);

        Assert.Equal (3, spanning.ContentBounds.Height);
        Assert.Equal ([1, 1], table.TableLayout.RowHeights);
        Assert.Equal (5, table.TableLayout.Height);
        Assert.Contains ("two", table.TableLayout.SelectLine (2, 0, 24));
        Assert.DoesNotContain (table.TableLayout.Borders, border => border.Orientation == Orientation.Horizontal && border.Start.Y == 2 && border.Start.X == 0);
    }

    [Fact]
    public void Block_Cells_Preserve_Paragraphs_Lists_Code_And_Empty_Lines ()
    {
        const string source = "first\n\nsecond\n\n- item\n  - nested\n\n```text\n  code\n\nlast\n```";
        using MarkdownTable table = new () { Width = 40, TableData = TableData.CreateGrid ([new ()], [new (false, [new (0, source)])]) };
        table.Layout ();
        string [] lines = [.. Enumerable.Range (1, table.RenderedHeight - 2).Select (row => table.TableLayout.SelectLine (row, 2, 39))];

        Assert.Equal (["first", "", "second", "", "• item", "  • nested", "", "  code", "", "last"], lines);
    }

    [Theory]
    [InlineData ("界")]
    [InlineData ("e\u0301")]
    [InlineData ("👩‍💻")]
    [InlineData ("a**\u0301**")]
    public void Unicode_Graphemes_Are_Indivisible_In_Layout_And_Selection (string source)
    {
        using MarkdownTable table = new () { Width = 1, TableData = new (["H"], [Alignment.Start], [ [source]]) };
        table.Layout ();
        MarkdownTableGlyph glyph = Assert.Single (table.TableLayout.Glyphs, glyph => !glyph.IsHeader);
        string expected = source.Replace ("**", string.Empty, StringComparison.Ordinal);

        Assert.Equal (expected, glyph.Text);
        Assert.Equal (expected.GetColumns (), glyph.Width);
        Assert.Equal (expected, table.TableLayout.SelectLine (glyph.Position.Y, glyph.Position.X + glyph.Width - 1, glyph.Position.X + glyph.Width));
    }

    [Fact]
    public void Adjacent_Same_Url_Links_Remain_Separate_Keyboard_And_Mouse_Targets ()
    {
        using MarkdownTable table = new ()
        {
            Width = 30,
            TableData = TableData.CreateGrid ([new (), new ()], [new (false, [new (0, "[one](https://example.com)[two](https://example.com)", columnSpan: 2)])])
        };
        table.Layout ();

        Assert.Equal (2, table.TableLayout.Links.Count);
        Assert.Equal (0, table.HitTestLinkIndex (2, 1));
        Assert.Equal (1, table.HitTestLinkIndex (5, 1));
        Assert.Equal (-1, table.HitTestLinkIndex (1, 1));
        List<string> activated = [];
        table.LinkClicked += (_, e) =>
                             {
                                 activated.Add (e.Url);
                                 e.Handled = true;
                             };
        table.NewMouseEvent (new Mouse { Position = new Point (5, 1), Flags = MouseFlags.LeftButtonClicked });
        Assert.Equal (["https://example.com"], activated);
    }

    [Fact]
    public void Resizing_Recomputes_An_Identical_Snapshot_And_Following_Block_Position ()
    {
        using TgMarkdown view = CreateView (MarkdownGridFixturesTests.ColumnSpan + "\n\nafter", 24);
        MarkdownTable first = Assert.Single (view.SubViews.OfType<MarkdownTable> ());
        int [] widths = [.. first.TableLayout.ColumnWidths];
        int height = view.GetContentHeight ();
        MarkdownTableGlyph [] glyphs = [.. first.TableLayout.Glyphs];
        view.Width = 80;
        view.Layout ();
        view.Width = 24;
        view.Layout ();
        MarkdownTable last = Assert.Single (view.SubViews.OfType<MarkdownTable> ());

        Assert.Equal (widths, last.TableLayout.ColumnWidths);
        Assert.Equal (height, view.GetContentHeight ());
        Assert.Equal (glyphs.Select (glyph => (glyph.Text, glyph.Position)), last.TableLayout.Glyphs.Select (glyph => (glyph.Text, glyph.Position)));
    }

    [Theory]
    [InlineData (10)]
    [InlineData (25)]
    [InlineData (80)]
    public void Mixed_Spans_Never_Draw_Borders_Through_Cell_Rectangles (int width)
    {
        TableData data = TableData.CreateGrid ([new (), new (), new ()],
                                             [new (true, [new (0, "A  \nwrapped", columnSpan: 2, rowSpan: 2), new (2, "B")]),
                                              new (false, [new (2, "C")]),
                                              new (false, [new (0, "D"), new (1, "E", columnSpan: 2)])]);
        using MarkdownTable table = new () { Width = width, TableData = data };
        table.Layout ();

        foreach (MarkdownTableCellLayout cell in table.TableLayout.Cells)
        {
            foreach ((Point start, int length, Orientation orientation) in table.TableLayout.Borders)
            {
                for (int offset = 0; offset < length; offset++)
                {
                    Point point = orientation == Orientation.Horizontal ? start with { X = start.X + offset } : start with { Y = start.Y + offset };
                    Assert.False (cell.Bounds.Contains (point));
                }
            }
        }

        Assert.Equal ("ABCDE", string.Concat (table.TableLayout.Glyphs.Where (glyph => "ABCDE".Contains (glyph.Text, StringComparison.Ordinal)).Select (glyph => glyph.Text)));
    }

    [Theory]
    [InlineData (0)]
    [InlineData (1)]
    [InlineData (2)]
    [InlineData (3)]
    public void Document_Recovers_From_A_Viewport_Below_The_Normal_Minimum (int width)
    {
        using TgMarkdown view = CreateView (MarkdownGridFixturesTests.ColumnSpan, width);
        view.Width = 40;
        view.Layout ();
        MarkdownTable table = Assert.Single (view.SubViews.OfType<MarkdownTable> ());

        Assert.Contains ("Cell spanning", table.TableLayout.SelectLine (3, 0, 40));
        Assert.Equal (40, table.TableLayout.Width);
    }

    [Fact]
    public void Replacing_A_Highlighter_Recomputes_Transformed_Code_Bounds ()
    {
        using MarkdownTable table = new ()
        {
            Width = 30,
            TableData = TableData.CreateGrid ([new ()], [new (false, [new (0, "```text\nx\n```")])])
        };
        table.Layout ();
        Assert.Equal ([3], table.TableLayout.ColumnWidths);
        table.SyntaxHighlighter = new ExpandingHighlighter ();
        table.Layout ();
        Assert.Equal ([8], table.TableLayout.ColumnWidths);
        Assert.Equal ("longer", table.TableLayout.SelectLine (1, 2, 29));
        table.SyntaxHighlighter = null;
        table.Layout ();
        Assert.Equal ([3], table.TableLayout.ColumnWidths);
        Assert.Equal ("x", table.TableLayout.SelectLine (1, 2, 29));
    }

    private static TgMarkdown CreateView (string source, int width)
    {
        TgMarkdown view = new () { Text = source, Width = width, Height = 40 };
        view.Layout ();

        return view;
    }
    private sealed class ExpandingHighlighter : ISyntaxHighlighter
    {
        public IReadOnlyList<StyledSegment> Highlight (string code, string? language) => [new ("longer", MarkdownStyleRole.CodeBlock)];
        public void ResetState ()
        {
        }
        public string ThemeName => "Test";
        public Color? DefaultBackground => null;
        public Terminal.Gui.Drawing.Attribute? GetAttributeForScope (MarkdownStyleRole role) => null;
    }
}
