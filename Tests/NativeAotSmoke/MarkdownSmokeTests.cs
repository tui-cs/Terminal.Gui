using System.Drawing;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.Tables;
using Markdown = Terminal.Gui.Views.Markdown;

namespace NativeAotSmoke;

// CoPilot - GPT-6
internal static class MarkdownSmokeTests
{
    public static void Run ()
    {
        using Markdown view = new ()
        {
            Text = "first  \nsecond\n---",
            Width = 60,
            Height = 20,
            ShowCopyButtons = false
        };
        view.Layout ();

        if (view.LineCount != 2 || !view.ScrollToAnchor ("first-second"))
        {
            throw new InvalidOperationException ("AOT Markdown heading layout or anchor generation failed.");
        }

        AssertRendered (view, "Term **bold**\n: definition", "Term bold\n  definition");
        AssertRendered (view, "paragraph\n\n-", "paragraph\n\n•");
        AssertRendered (view, "- first  \n  second", "• first\n  second");
        AssertRendered (view, "- first  \n  second\n  ---", "• ## first\n  second");
        AssertRendered (view, "*[HTML]: HyperText Markup Language\n\nHTML supports **bold**", "HTML supports bold");
        AssertRendered (view, "[reference][target]\n\n[target]: https://example.com", "reference");
        AssertRendered (view, "```csharp\nint answer = 42;\n```", "int answer = 42;");
        AssertRendered (view, "> [!WARNING]\n> Critical action", "> WARNING\n> Critical action");

        string codeTable = Normalize (view.RenderToAnsi ("| Code |\n| --- |\n| ``a`b`` |"));

        if (!codeTable.Contains (" a`b ", StringComparison.Ordinal))
        {
            throw new InvalidOperationException ("AOT Markdown table code span was corrupted.");
        }

        ExerciseTableLinks ();
        ExerciseRichTables ();

        view.MarkdownPipeline = new MarkdownPipelineBuilder ().UseAlertBlocks (allowNestedAlerts: true).Build ();
        AssertRendered (view, "> outer\n>\n> > [!WARNING]\n> > nested", "> outer\n> > WARNING\n> > nested");

        view.MarkdownPipeline = new MarkdownPipelineBuilder ().UseAdvancedExtensions ().UseEmojiAndSmiley ().UseCjkFriendlyEmphasis ().Build ();
        string table = view.RenderToAnsi ("| Name | Value |\n| --- | --- |\n| cell | :x: |");

        if (!table.Contains ("Name", StringComparison.Ordinal) || !table.Contains ("❌", StringComparison.Ordinal))
        {
            throw new InvalidOperationException ("AOT Markdown table rendering failed.");
        }
    }

    private static void ExerciseRichTables ()
    {
        using Markdown view = new ()
        {
            MarkdownPipeline = new MarkdownPipelineBuilder ().UsePipeTables (new PipeTableOptions { InferColumnWidthsFromSeparator = true })
                                                           .UseAdvancedExtensions ().Build ()
        };
        string proportional = Normalize (view.RenderToAnsi ("| A | B |\n| ----- | -------------------- |\n| x | y |", 47));

        if (proportional.Split ('\n') [1] [11] != '│')
        {
            throw new InvalidOperationException ("AOT proportional table geometry failed.");
        }

        const string grid = "+-------+-------+\n| A     | B     |\n+-------+-------+\n| C spanning    |\n+-------+-------+";
        view.Text = grid;
        view.Width = 24;
        view.Height = 12;
        view.Layout ();
        MarkdownTable parsed = view.SubViews.OfType<MarkdownTable> ().Single ();

        if (parsed.TableData.Headers.Length != 0 || parsed.TableData.TableRows [1].Cells [0].ColumnSpan != 2)
        {
            throw new InvalidOperationException ("AOT headerless or spanning grid metadata was lost.");
        }

        string rendered = Normalize (view.RenderToAnsi (width: 24));

        if (!rendered.Contains ("C spanning", StringComparison.Ordinal))
        {
            throw new InvalidOperationException ("AOT spanning grid content was lost.");
        }

        using MarkdownTable standalone = new ()
        {
            Width = 24,
            TableData = TableData.CreateGrid ([new (), new ()],
                                             [new (false, [new (0, "[A link that wraps inside a spanning cell](https://example.com/grid)", columnSpan: 2)])])
        };
        standalone.Layout ();

        if (standalone.GetContentHeight () <= 3)
        {
            throw new InvalidOperationException ("AOT spanning-cell link did not wrap.");
        }

        bool activated = false;
        standalone.LinkClicked += (_, e) =>
                                  {
                                      activated = e.Url == "https://example.com/grid";
                                      e.Handled = true;
                                  };
        standalone.NewMouseEvent (new Mouse { Position = new Point (2, 1), Flags = MouseFlags.LeftButtonClicked });

        if (!activated)
        {
            throw new InvalidOperationException ("AOT spanning-cell link activation failed.");
        }
    }

    private static void AssertRendered (Markdown view, string markdown, string expected)
    {
        string normalized = Normalize (view.RenderToAnsi (markdown));

        if (normalized != expected)
        {
            throw new InvalidOperationException ($"AOT Markdown rendering failed: expected '{expected}', got '{normalized}'.");
        }
    }

    private static string Normalize (string ansi)
    {
        string plain = Regex.Replace (ansi, @"\x1b\[[0-?]*[ -/]*[@-~]", string.Empty);
        plain = Regex.Replace (plain, @"\x1b\][^\x07\x1b]*(?:\x07|\x1b\\)", string.Empty);

        return string.Join ("\n", plain.Replace ("\r\n", "\n").TrimEnd ().Split ('\n').Select (line => line.TrimEnd ()));
    }

    private static void ExerciseTableLinks ()
    {
        const string url = "https://example.com/path_(part)";
        using IApplication app = Application.Create ().Init (Terminal.Gui.Drivers.DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (60, 12);
        using Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };
        Markdown markdown = new () { Text = $"| Link |\n| --- |\n| [**Testing [Header]**]({url}) |", Width = Dim.Fill (), Height = Dim.Fill () };
        window.Add (markdown);
        List<string> activated = [];
        markdown.LinkClicked += (_, e) =>
                                {
                                    activated.Add (e.Url);
                                    e.Handled = true;
                                };
        app.Begin (window);
        app.LayoutAndDraw ();
        MarkdownTable table = markdown.SubViews.OfType<MarkdownTable> ().Single ();
        Point position = table.ViewportToScreen (new Point (2, 3));
        TextStyle style = app.Driver.Contents! [position.Y, position.X].Attribute!.Value.Style;

        if ((style & (TextStyle.Bold | TextStyle.Underline)) != (TextStyle.Bold | TextStyle.Underline))
        {
            throw new InvalidOperationException ("AOT Markdown table link styling was lost.");
        }

        table.NewMouseEvent (new Mouse { Position = new Point (2, 3), Flags = MouseFlags.LeftButtonClicked });
        table.SetFocus ();
        table.InvokeCommand (Command.Accept);

        if (activated.Count != 2 || activated.Any (target => target != url))
        {
            throw new InvalidOperationException ("AOT Markdown table link activation changed its target.");
        }
    }
}
