using System.Text;
using Markdig;
using UnitTests;
using TgMarkdown = Terminal.Gui.Views.Markdown;

namespace ViewsTests.Markdown;

// CoPilot - GPT-6
public class MarkdownParsedContentTests : TestDriverBase
{
    [Theory]
    [InlineData ("$x^2$", "$x^2$")]
    [InlineData ("Wow\\![docs](https://x)", "Wow!docs")]
    [InlineData ("[`a\\|b`](u)", "a|b")]
    [InlineData ("``a`b``", "a`b")]
    [InlineData ("``a\\|b`c``", "a|b`c")]
    [InlineData ("`` `x` ``", "`x`")]
    [InlineData ("\\*literal\\*", "*literal*")]
    [InlineData ("[Testing [Header]](https://example.com)", "Testing [Header]")]
    [InlineData ("~~deleted~~", "deleted")]
    [InlineData ("**bold *inner***", "bold inner")]
    [InlineData ("a\\|b", "a|b")]
    public void Table_Cell_Renders_Parsed_Text (string source, string expected)
    {
        using IDriver driver = CreateTestDriver (60, 20);
        using TgMarkdown markdown = CreateTable (source, driver);
        MarkdownTable table = Assert.Single (markdown.SubViews.OfType<MarkdownTable> ());

        Assert.Equal ($"│ {expected}", ReadRow (driver, table, 3).TrimEnd (' ', '│'));
        Assert.Equal (5, table.RenderedHeight);
    }

    [Theory]
    [InlineData ("~~deleted~~", TextStyle.Strikethrough)]
    [InlineData ("**bold**", TextStyle.Bold)]
    [InlineData ("*italic*", TextStyle.Italic)]
    [InlineData ("***both***", TextStyle.Bold | TextStyle.Italic)]
    [InlineData ("[**bold**](https://example.com)", TextStyle.Bold | TextStyle.Underline)]
    public void Table_Cell_Preserves_Text_Style (string source, TextStyle expected)
    {
        using IDriver driver = CreateTestDriver (60, 20);
        using TgMarkdown markdown = CreateTable (source, driver);
        MarkdownTable table = Assert.Single (markdown.SubViews.OfType<MarkdownTable> ());
        Point position = table.ViewportToScreen (new Point (2, 3));
        TextStyle actual = driver.Contents! [position.Y, position.X].Attribute!.Value.Style;

        Assert.Equal (expected, actual & expected);
    }

    [Fact]
    public void Table_Nested_Emphasis_Combines_Styles_At_Inner_Text ()
    {
        using IDriver driver = CreateTestDriver (60, 20);
        using TgMarkdown markdown = CreateTable ("**bold *inner***", driver);
        MarkdownTable table = Assert.Single (markdown.SubViews.OfType<MarkdownTable> ());
        Point position = table.ViewportToScreen (new Point (7, 3));
        TextStyle actual = driver.Contents! [position.Y, position.X].Attribute!.Value.Style;

        Assert.Equal (TextStyle.Bold | TextStyle.Italic, actual & (TextStyle.Bold | TextStyle.Italic));
    }

    [Fact]
    public void Table_Text_Model_Uses_Safe_Code_Delimiters ()
    {
        using TgMarkdown markdown = CreateTable ("``a`b``");
        MarkdownTable table = Assert.Single (markdown.SubViews.OfType<MarkdownTable> ());

        Assert.Equal ("``a`b``", table.TableData.Rows [0] [0]);
    }

    [Fact]
    public void Table_Nested_Label_Keyboard_Activation_Uses_Original_Url ()
    {
        const string url = "https://example.com/path_(part)";

        // Exercise application keyboard focus here; rendering and mouse tests use plain drivers.
        using IApplication app = Application.Create ().Init (DriverRegistry.Names.ANSI);
        using Runnable window = BeginTable (app, $"[Testing [Header]]({url})", out TgMarkdown markdown);
        MarkdownTable table = Assert.Single (markdown.SubViews.OfType<MarkdownTable> ());
        List<string> activated = [];
        markdown.LinkClicked += (_, e) =>
                                {
                                    activated.Add (e.Url);
                                    e.Handled = true;
                                };

        table.SetFocus ();
        table.InvokeCommand (Command.Accept);

        Assert.Equal ([url], activated);
    }

    [Fact]
    public void Table_Nested_Label_Mouse_Activation_Uses_Original_Url ()
    {
        const string url = "https://example.com/path_(part)";
        using IDriver driver = CreateTestDriver (60, 20);
        using TgMarkdown markdown = CreateTable ($"[Testing [Header]]({url})", driver);
        MarkdownTable table = Assert.Single (markdown.SubViews.OfType<MarkdownTable> ());
        string? activated = null;
        markdown.LinkClicked += (_, e) =>
                                {
                                    activated = e.Url;
                                    e.Handled = true;
                                };
        table.NewMouseEvent (new Mouse
        {
            Position = new Point (2, 3),
            Flags = MouseFlags.LeftButtonClicked
        });

        Assert.Equal (url, activated);
    }

    [Fact]
    public void Table_Wrapping_Preserves_Styles_And_Original_Link ()
    {
        const string url = "https://example.com/path_(part)";
        using IDriver driver = CreateTestDriver (9, 20);
        using TgMarkdown markdown = CreateTable ($"[**abcdefghij**]({url})", driver, 9);
        MarkdownTable table = Assert.Single (markdown.SubViews.OfType<MarkdownTable> ());
        Assert.True (table.RenderedHeight > 5);
        Point position = table.ViewportToScreen (new Point (2, 4));
        TextStyle actual = driver.Contents! [position.Y, position.X].Attribute!.Value.Style;
        Assert.Equal (TextStyle.Bold | TextStyle.Underline, actual & (TextStyle.Bold | TextStyle.Underline));
        string? activated = null;
        markdown.LinkClicked += (_, e) =>
                                {
                                    activated = e.Url;
                                    e.Handled = true;
                                };

        table.NewMouseEvent (new Mouse { Position = new Point (2, 4), Flags = MouseFlags.LeftButtonClicked });

        Assert.Equal (url, activated);
    }

    [Fact]
    public void TableData_Reassignment_Replaces_Parsed_Content ()
    {
        using IDriver driver = CreateTestDriver (60, 20);
        using TgMarkdown markdown = CreateTable ("~~old~~", driver);
        MarkdownTable table = Assert.Single (markdown.SubViews.OfType<MarkdownTable> ());
        table.TableData = new (["Cell"], [Alignment.Start], [ ["**new**"]]);
        table.Layout ();
        table.SetNeedsDraw ();
        table.SetClipToScreen ();
        table.Draw ();

        Assert.Equal ("│ new", ReadRow (driver, table, 3).TrimEnd (' ', '│'));
    }

    [Theory]
    [InlineData ("NOTE")]
    [InlineData ("TIP")]
    [InlineData ("IMPORTANT")]
    [InlineData ("WARNING")]
    [InlineData ("CAUTION")]
    public void Alert_Kind_Is_Visible_And_Bold (string kind)
    {
        using IDriver driver = CreateTestDriver (50, 10);
        using TgMarkdown markdown = new ()
        {
            Driver = driver,
            Text = $"> [!{kind}]\n> Critical **action**",
            Width = 50,
            Height = 10
        };
        markdown.Layout ();
        markdown.Draw ();

        Assert.StartsWith ($"> {kind}", ReadRow (driver, markdown, 0));
        Assert.StartsWith ("> Critical action", ReadRow (driver, markdown, 1));
        Point position = markdown.ViewportToScreen (new Point (2, 0));
        Assert.True (driver.Contents! [position.Y, position.X].Attribute!.Value.Style.HasFlag (TextStyle.Bold));
    }

    [Fact]
    public void Custom_Pipeline_Nested_Alert_Preserves_Kind_And_Quote_Prefix ()
    {
        MarkdownPipeline pipeline = new MarkdownPipelineBuilder ().UseAlertBlocks (allowNestedAlerts: true).Build ();
        using TgMarkdown markdown = new () { MarkdownPipeline = pipeline };
        string ansi = markdown.RenderToAnsi ("> outer\n>\n> > [!WARNING]\n> > nested");
        string plain = System.Text.RegularExpressions.Regex.Replace (ansi, @"\x1b\[[0-?]*[ -/]*[@-~]", string.Empty);

        Assert.Contains ("> > WARNING", plain);
        Assert.Contains ("> > nested", plain);
    }

    private static TgMarkdown CreateTable (string source, IDriver? driver = null, int width = 60)
    {
        TgMarkdown markdown = new ()
        {
            Driver = driver,
            Text = $"| Cell |\n| --- |\n| {source} |",
            Width = width,
            Height = 20
        };
        markdown.Layout ();
        markdown.Draw ();

        return markdown;
    }

    private static Runnable BeginTable (IApplication app, string source, out TgMarkdown markdown, int width = 60)
    {
        app.Driver!.SetScreenSize (width, 20);
        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };
        markdown = new () { Text = $"| Cell |\n| --- |\n| {source} |", Width = Dim.Fill (), Height = Dim.Fill () };
        window.Add (markdown);
        app.Begin (window);
        app.LayoutAndDraw ();

        return window;
    }

    private static string ReadRow (IDriver driver, View view, int row)
    {
        Point position = view.ViewportToScreen (new Point (0, row));
        StringBuilder text = new ();

        for (int x = position.X; x < position.X + view.Viewport.Width; x++)
        {
            text.Append (driver.Contents! [position.Y, x].Grapheme);
        }

        return text.ToString ();
    }
}
