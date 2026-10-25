using UnitTests;
using TgMarkdown = Terminal.Gui.Views.Markdown;

namespace ViewsTests.Markdown;

// CoPilot - GPT-6
public class MarkdownGridInteractionTests : TestDriverBase
{
    [Fact]
    public void Headerless_Grid_Draws_Exact_Intersections_Without_A_Synthetic_Header ()
    {
        TableData data = TableData.CreateGrid ([new (), new ()],
                                             [new (false, [new (0, "A"), new (1, "B")]),
                                              new (false, [new (0, "C"), new (1, "D")])]);
        Assert.Equal ("┌───┬───┐\n│ A │ B │\n├───┼───┤\n│ C │ D │\n└───┴───┘", Draw (data, 9));
    }

    [Fact]
    public void Column_Span_Draws_A_T_Junction_At_The_Header_Boundary ()
    {
        TableData data = TableData.CreateGrid ([new (), new ()],
                                             [new (true, [new (0, "A"), new (1, "B")]),
                                              new (false, [new (0, "M", columnSpan: 2)])]);
        Assert.Equal ("┌───┬───┐\n│ A │ B │\n├───┴───┤\n│ M     │\n└───────┘", Draw (data, 9));
    }

    [Fact]
    public void Row_Span_Draws_A_Partial_Horizontal_Separator ()
    {
        TableData data = TableData.CreateGrid ([new (), new ()],
                                             [new (false, [new (0, "A", rowSpan: 2), new (1, "B")]),
                                              new (false, [new (1, "C")])]);
        Assert.Equal ("┌───┬───┐\n│ A │ B │\n│   ├───┤\n│   │ C │\n└───┴───┘", Draw (data, 9));
    }

    [Theory]
    [InlineData ("\n")]
    [InlineData ("\r\n")]
    public void Complete_Grid_Copy_Preserves_Exact_Source_Inside_A_Document (string newline)
    {
        string source = MarkdownGridFixturesTests.ColumnSpan.Replace ("\n", newline, StringComparison.Ordinal);
        using TgMarkdown view = new () { Text = "before" + newline + newline + source + newline + newline + "after", Width = 40, Height = 30 };
        view.Layout ();
        MarkdownTable table = Assert.Single (view.SubViews.OfType<MarkdownTable> ());
        Select (view, new (0, table.Frame.Y), new (39, table.Frame.Y + table.RenderedHeight - 1));

        Assert.Equal (source, view.SelectedText);
        view.SelectAll ();
        Assert.Equal (view.Text, view.SelectedText);
    }

    [Fact]
    public void Partial_Spanning_Cell_Copy_Uses_Visible_Text_Once ()
    {
        using TgMarkdown view = new () { Text = "before\n\n" + MarkdownGridFixturesTests.ColumnSpan + "\n\nafter", Width = 40, Height = 30 };
        view.Layout ();
        MarkdownTable table = Assert.Single (view.SubViews.OfType<MarkdownTable> ());
        MarkdownTableCellLayout spanning = Assert.Single (table.TableLayout.Cells, cell => cell.Content.Definition.ColumnSpan == 2);
        int row = table.Frame.Y + spanning.ContentBounds.Y;
        Select (view, new (2, row), new (6, row));

        Assert.Equal ("Cell", view.SelectedText);
        Select (view, new (2, row), new (39, row));
        Assert.Equal ("Cell spanning", view.SelectedText);
    }

    [Fact]
    public void Source_Free_Grid_Text_Quotes_Multiline_Fields_Without_Duplicating_Spans ()
    {
        using MarkdownTable table = new ()
        {
            TableData = TableData.CreateGrid ([new (), new ()],
                                             [new (false, [new (0, "one\n\ntwo", columnSpan: 2)]),
                                              new (false, [new (0, "three"), new (1, "four")])])
        };

        Assert.Equal ("\"one\n\ntwo\"\t\nthree\tfour", table.Text);
    }

    [Fact]
    public void Standalone_Hit_Test_Accounts_For_Horizontal_Scrolling ()
    {
        using MarkdownTable table = new ()
        {
            Width = 5,
            TableData = TableData.CreateGrid ([new (), new ()], [new (false, [new (0, "界"), new (1, "[link](u)")])])
        };
        table.Layout ();
        MarkdownTableGlyph link = table.TableLayout.Glyphs.First (glyph => glyph.LinkIndex == 0);
        table.Viewport = table.Viewport with { X = link.Position.X - 1 };

        Assert.Equal (0, table.HitTestLinkIndex (link.Position.X - table.Viewport.X, link.Position.Y));
        Assert.Equal (-1, table.HitTestLinkIndex (0, 0));
    }

    [Fact]
    public void Keyboard_Activation_Preserves_The_Focused_Link_Across_Document_Resize ()
    {
        using IApplication app = Application.Create ().Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (50, 20);
        using Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill () };
        TgMarkdown view = new () { Text = "| Link |\n| --- |\n| [one](first) [two](second) |", Width = Dim.Fill (), Height = Dim.Fill () };
        window.Add (view);
        List<string> activated = [];
        view.LinkClicked += (_, e) =>
                            {
                                activated.Add (e.Url);
                                e.Handled = true;
                            };
        app.Begin (window);
        app.LayoutAndDraw ();
        MarkdownTable table = Assert.Single (view.SubViews.OfType<MarkdownTable> ());
        table.SetFocus ();
        table.ActiveLinkIndex = 1;
        app.Keyboard.RaiseKeyDownEvent (Key.Enter);
        app.Driver.SetScreenSize (24, 20);
        app.LayoutAndDraw ();
        MarkdownTable resized = Assert.Single (view.SubViews.OfType<MarkdownTable> ());

        Assert.True (resized.HasFocus);
        Assert.Equal (1, resized.ActiveLinkIndex);
        app.Keyboard.RaiseKeyDownEvent (Key.Enter);
        Assert.Equal (["second", "second"], activated);
    }

    [Fact]
    public void Application_Mouse_Drag_Through_A_Table_Selects_Text ()
    {
        using IApplication app = Application.Create ().Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (40, 20);
        using Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill () };
        TgMarkdown view = new () { Text = "before\n\n" + MarkdownGridFixturesTests.ColumnSpan + "\n\nafter", Width = Dim.Fill (), Height = Dim.Fill () };
        window.Add (view);
        app.Begin (window);
        app.LayoutAndDraw ();
        MarkdownTable table = Assert.Single (view.SubViews.OfType<MarkdownTable> ());
        MarkdownTableCellLayout cell = Assert.Single (table.TableLayout.Cells, cell => cell.Content.Definition.ColumnSpan == 2);
        Point first = table.ContentToScreen (cell.ContentBounds.Location);
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = first, Flags = MouseFlags.LeftButtonPressed });
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = first with { X = first.X + 4 }, Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport });
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = first with { X = first.X + 4 }, Flags = MouseFlags.LeftButtonReleased });

        Assert.Equal ("Cell", view.SelectedText);
    }

    [Theory]
    [InlineData (24)]
    [InlineData (47)]
    public void Ansi_Output_Uses_The_Same_Grapheme_Positions_As_The_Screen (int width)
    {
        using IDriver driver = CreateTestDriver (width, 30);
        using TgMarkdown view = new ()
        {
            Driver = driver,
            Width = width,
            Height = 30,
            Text = "| H |\n| --- |\n| e\u0301 👩‍💻 界 |\n\n" + MarkdownGridFixturesTests.ColumnSpan
        };
        view.Layout ();
        view.Draw ();
        string screen = string.Join ('\n', Enumerable.Range (0, view.GetContentHeight ()).Select (ReadRow));
        string ansi = view.RenderToAnsi (width: width);
        string plain = System.Text.RegularExpressions.Regex.Replace (ansi, @"\x1b\[[0-?]*[ -/]*[@-~]", string.Empty);
        plain = System.Text.RegularExpressions.Regex.Replace (plain, @"\x1b\][^\x07\x1b]*(?:\x07|\x1b\\)", string.Empty);
        string normalized = string.Join ('\n', plain.Replace ("\r\n", "\n", StringComparison.Ordinal).TrimEnd ().Split ('\n').Select (line => line.TrimEnd ()));

        Assert.Equal (screen.TrimEnd (), normalized);

        return;

        string ReadRow (int row)
        {
            System.Text.StringBuilder text = new ();

            for (int column = 0; column < width; column++)
            {
                string grapheme = driver.Contents! [row, column].Grapheme;
                text.Append (grapheme);
                column += Math.Max (1, grapheme.GetColumns ()) - 1;
            }

            return text.ToString ().TrimEnd ();
        }

    }

    private string Draw (TableData data, int width)
    {
        using IDriver driver = CreateTestDriver (width, 20);
        using MarkdownTable table = new () { TableData = data, Driver = driver, Width = width };
        table.Layout ();
        table.Draw ();

        return string.Join ('\n', Enumerable.Range (0, table.RenderedHeight)
                                           .Select (row => string.Concat (Enumerable.Range (0, width).Select (column => driver.Contents! [row, column].Grapheme))));
    }

    private static void Select (TgMarkdown view, Point first, Point last)
    {
        view.NewMouseEvent (new Mouse { Position = first, Flags = MouseFlags.LeftButtonPressed });
        view.NewMouseEvent (new Mouse { Position = last, Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport });
    }
}
