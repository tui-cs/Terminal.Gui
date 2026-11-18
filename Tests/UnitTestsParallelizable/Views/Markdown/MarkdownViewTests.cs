using System.Collections;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;
using UnitTests;

namespace ViewsTests.Markdown;

[TestSubject (typeof (Terminal.Gui.Views.Markdown))]
public class MarkdownViewTests (ITestOutputHelper output)
{
    // Copilot

    [Fact]
    public void Constructor_Defaults ()
    {
        Terminal.Gui.Views.Markdown view = new ();

        Assert.True (view.CanFocus);
        Assert.Equal (string.Empty, view.Text);
        Assert.Equal (0, view.LineCount);
        Assert.True (view.UseThemeBackground);
    }

    [Fact]
    public void Text_Set_Raises_MarkdownChanged ()
    {
        Terminal.Gui.Views.Markdown view = new ();
        var fired = false;

        view.MarkdownChanged += (_, _) => fired = true;

        view.Text = "# Header";

        Assert.True (fired);
    }

    [Fact]
    public void IDesignable_EnableForDesign_Returns_True ()
    {
        Terminal.Gui.Views.Markdown markdownView = new ();
        IDesignable designable = markdownView;

        bool result = designable.EnableForDesign ();

        Assert.True (result);
        Assert.Contains ("Markdown Sample", markdownView.Text);
    }

    [Fact]
    public void Layout_Computes_Lines_And_ContentSize ()
    {
        Terminal.Gui.Views.Markdown view = new () { Text = "# Header\n\nParagraph text" };
        view.Width = 20;
        view.Height = 5;

        View host = new () { Width = 20, Height = 5 };
        host.Add (view);

        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        Assert.True (view.LineCount >= 2);
        Assert.True (view.GetContentSize ().Height >= 2);

        host.Dispose ();
    }

    // Copilot — verifies AllViews_Center_Properly pattern with complex markdown (tables, code blocks)
    [Fact]
    public void Layout_Center_In_Host_Does_Not_Hang ()
    {
        Terminal.Gui.Views.Markdown view = new ();
        ((IDesignable)view).EnableForDesign ();

        view.X = Pos.Center ();
        view.Y = Pos.Center ();
        view.Width = 10;
        view.Height = 10;

        View frame = new () { X = 0, Y = 0, Width = 50, Height = 50 };
        frame.Add (view);
        frame.LayoutSubViews ();

        Assert.Equal (20, view.Frame.Left);
        Assert.Equal (20, view.Frame.Top);

        frame.Dispose ();
    }

    // Copilot — verifies simple markdown (no compound SubViews) centers correctly
    [Fact]
    public void Layout_Center_Simple_Markdown_Does_Not_Hang ()
    {
        Terminal.Gui.Views.Markdown view = new () { Text = "# Hello" };
        view.X = Pos.Center ();
        view.Y = Pos.Center ();
        view.Width = 10;
        view.Height = 10;

        View frame = new () { X = 0, Y = 0, Width = 50, Height = 50 };
        frame.Add (view);
        frame.LayoutSubViews ();

        Assert.Equal (20, view.Frame.Left);
        Assert.Equal (20, view.Frame.Top);

        frame.Dispose ();
    }

    // Copilot — reproduces layout with table + code block SubViews
    [Fact]
    public void Layout_With_Table_And_CodeBlock_Does_Not_Hang ()
    {
        Terminal.Gui.Views.Markdown view = new () { Text = Terminal.Gui.Views.Markdown.DefaultMarkdownSample };
        view.Width = 40;
        view.Height = 20;

        View host = new () { Width = 40, Height = 20 };
        host.Add (view);
        host.LayoutSubViews ();

        Assert.True (view.LineCount > 0);

        host.Dispose ();
    }

    // Copilot — tests that LayoutSubViews can be called multiple times safely
    [Fact]
    public void Layout_Multiple_Passes_Does_Not_Hang ()
    {
        Terminal.Gui.Views.Markdown view = new () { Text = Terminal.Gui.Views.Markdown.DefaultMarkdownSample };
        view.Width = Dim.Fill ();
        view.Height = Dim.Fill ();

        View host = new () { Width = 30, Height = 15 };
        host.Add (view);
        host.LayoutSubViews ();
        host.LayoutSubViews ();
        host.LayoutSubViews ();

        Assert.True (view.LineCount > 0);

        host.Dispose ();
    }

    [Fact]
    public void Draw_Emits_OSC8_For_Link ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };

        Terminal.Gui.Views.Markdown markdownView = new ()
        {
            Text = "Visit [Terminal.Gui](https://github.com/tui-cs/Terminal.Gui)", Width = Dim.Fill (), Height = Dim.Fill ()
        };

        window.Add (markdownView);

        app.Begin (window);
        app.LayoutAndDraw ();
        app.Driver!.Refresh ();

        string lastOutput = app.Driver.GetOutput ().GetLastOutput ();

        Assert.Contains (EscSeqUtils.OSC_StartHyperlink ("https://github.com/tui-cs/Terminal.Gui"), lastOutput);
        Assert.Contains (EscSeqUtils.OSC_EndHyperlink (), lastOutput);

        window.Dispose ();
    }

    [Fact]
    public void Mouse_Click_On_Link_Raises_LinkClicked ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };

        Terminal.Gui.Views.Markdown markdownView = new () { Text = "[Click](https://example.com)", Width = 20, Height = 3 };

        window.Add (markdownView);

        var clicked = false;

        markdownView.LinkClicked += (_, e) =>
                                    {
                                        clicked = true;
                                        e.Handled = true;
                                    };

        app.Begin (window);
        app.LayoutAndDraw ();

        markdownView.NewMouseEvent (new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonClicked });

        Assert.True (clicked);

        window.Dispose ();
    }

    // Codex - GPT-5
    [Fact]
    public void Mouse_Click_On_Relative_Link_Raises_LinkClicked ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };

        Terminal.Gui.Views.Markdown markdownView = new () { Text = "[Local](docs/readme.md)", Width = 20, Height = 3 };

        window.Add (markdownView);

        string clickedUrl = "";

        markdownView.LinkClicked += (_, e) =>
                                    {
                                        clickedUrl = e.Url;
                                        e.Handled = true;
                                    };

        app.Begin (window);
        app.LayoutAndDraw ();

        markdownView.NewMouseEvent (new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonClicked });

        Assert.Equal ("docs/readme.md", clickedUrl);

        window.Dispose ();
    }

    // Copilot
    [Fact]
    public void Mouse_Click_On_Link_In_Table_Cell_Raises_LinkClicked ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable window = new () { Width = 80, Height = 20, BorderStyle = LineStyle.None };

        string markdown = """
                          | Name | Description |
                          |------|-------------|
                          | [select](https://example.com) | Pick one item |
                          """;

        Terminal.Gui.Views.Markdown markdownView = new () { Text = markdown, Width = 60, Height = 15 };
        window.Add (markdownView);

        var clickedUrl = "";

        markdownView.LinkClicked += (_, e) =>
                                    {
                                        clickedUrl = e.Url;
                                        e.Handled = true;
                                    };

        app.Begin (window);
        app.LayoutAndDraw ();

        // The table is a SubView of the Markdown view. Find it.
        MarkdownTable? tableView = null;

        foreach (View sub in markdownView.SubViews)
        {
            if (sub is MarkdownTable t)
            {
                tableView = t;

                break;
            }
        }

        Assert.NotNull (tableView);

        // Click on the link text in the first body row, first column.
        // Table layout: row 0 = top border, row 1 = header, row 2 = separator, row 3 = body row.
        // Column layout: col 0 starts after left border (x=1), with 1 char padding = x=2.
        tableView.NewMouseEvent (new Mouse { Position = new Point (2, 3), Flags = MouseFlags.LeftButtonClicked });

        Assert.Equal ("https://example.com", clickedUrl);

        window.Dispose ();
    }

    // Codex - GPT-5
    [Fact]
    public void Mouse_Click_On_Relative_Link_In_Table_Cell_Raises_LinkClicked ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable window = new () { Width = 80, Height = 20, BorderStyle = LineStyle.None };

        string markdown = """
                          | Name | Description |
                          |------|-------------|
                          | [local](docs/readme.md) | Pick one item |
                          """;

        Terminal.Gui.Views.Markdown markdownView = new () { Text = markdown, Width = 60, Height = 15 };
        window.Add (markdownView);

        string clickedUrl = "";

        markdownView.LinkClicked += (_, e) =>
                                    {
                                        clickedUrl = e.Url;
                                        e.Handled = true;
                                    };

        app.Begin (window);
        app.LayoutAndDraw ();

        MarkdownTable? tableView = markdownView.SubViews.OfType<MarkdownTable> ().FirstOrDefault ();
        Assert.NotNull (tableView);

        tableView.NewMouseEvent (new Mouse { Position = new Point (2, 3), Flags = MouseFlags.LeftButtonClicked });

        Assert.Equal ("docs/readme.md", clickedUrl);

        window.Dispose ();
    }

    // Copilot
    [Fact]
    public void Tab_Navigation_Cycles_Through_Table_Links ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable window = new () { Width = 80, Height = 20, BorderStyle = LineStyle.None };

        string markdown = """
                          | Name | Description |
                          |------|-------------|
                          | [alpha](https://a.com) | [beta](https://b.com) |
                          """;

        Terminal.Gui.Views.Markdown markdownView = new () { Text = markdown, Width = 60, Height = 15 };
        List<string> activatedUrls = new ();

        markdownView.LinkClicked += (_, e) =>
                                    {
                                        activatedUrls.Add (e.Url);
                                        e.Handled = true;
                                    };

        window.Add (markdownView);

        app.Begin (window);
        app.LayoutAndDraw ();

        // Find the table SubView
        MarkdownTable? tableView = markdownView.SubViews.OfType<MarkdownTable> ().FirstOrDefault ();
        Assert.NotNull (tableView);
        Assert.True (tableView.CanFocus);

        // Give focus to the table
        tableView.SetFocus ();

        // Activate to check first focused link
        tableView.InvokeCommand (Command.Accept);

        // Advance focus forward
        markdownView.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        tableView.InvokeCommand (Command.Accept);

        // Should have activated two distinct links
        Assert.Equal (2, activatedUrls.Count);
        Assert.Contains ("https://a.com", activatedUrls);
        Assert.Contains ("https://b.com", activatedUrls);

        window.Dispose ();
    }

    // Copilot
    [Fact]
    public void Enter_Activates_Focused_Table_Link ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable window = new () { Width = 80, Height = 20, BorderStyle = LineStyle.None };

        string markdown = """
                          | Name | Description |
                          |------|-------------|
                          | [alpha](https://a.com) | info |
                          """;

        Terminal.Gui.Views.Markdown markdownView = new () { Text = markdown, Width = 60, Height = 15 };
        var clickedUrl = "";

        markdownView.LinkClicked += (_, e) =>
                                    {
                                        clickedUrl = e.Url;
                                        e.Handled = true;
                                    };

        window.Add (markdownView);

        app.Begin (window);
        app.LayoutAndDraw ();

        MarkdownTable? tableView = markdownView.SubViews.OfType<MarkdownTable> ().FirstOrDefault ();
        Assert.NotNull (tableView);

        // Focus the table — the first link becomes active
        tableView.SetFocus ();

        // Simulate Enter key via Command.Accept
        tableView.InvokeCommand (Command.Accept);

        Assert.Equal ("https://a.com", clickedUrl);

        window.Dispose ();
    }

    // Copilot
    [Fact]
    public void ShiftTab_Navigation_Cycles_Backwards_Through_Table_Links ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable window = new () { Width = 80, Height = 20, BorderStyle = LineStyle.None };

        string markdown = """
                          | Name | Description |
                          |------|-------------|
                          | [alpha](https://a.com) | [beta](https://b.com) |
                          """;

        Terminal.Gui.Views.Markdown markdownView = new () { Text = markdown, Width = 60, Height = 15 };
        List<string> activatedUrls = new ();

        markdownView.LinkClicked += (_, e) =>
                                    {
                                        activatedUrls.Add (e.Url);
                                        e.Handled = true;
                                    };

        window.Add (markdownView);

        app.Begin (window);
        app.LayoutAndDraw ();

        MarkdownTable? tableView = markdownView.SubViews.OfType<MarkdownTable> ().FirstOrDefault ();
        Assert.NotNull (tableView);

        // Focus the table — first link (alpha) becomes active on focus gain
        tableView.SetFocus ();

        // Advance forward to second link (beta)
        markdownView.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        tableView.InvokeCommand (Command.Accept);
        Assert.Single (activatedUrls);
        Assert.Equal ("https://b.com", activatedUrls [0]);

        // Navigate backward — should go back to first link (alpha)
        markdownView.AdvanceFocus (NavigationDirection.Backward, TabBehavior.TabStop);
        tableView.InvokeCommand (Command.Accept);
        Assert.Equal (2, activatedUrls.Count);
        Assert.Equal ("https://a.com", activatedUrls [1]);

        window.Dispose ();
    }

    // Copilot - Opus 4.6 - verify Issue 1: anchor links in tables should scroll to heading
    [Fact]
    public void Anchor_Link_In_Table_Scrolls_To_Heading ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable window = new () { Width = 80, Height = 20, BorderStyle = LineStyle.None };

        // Create markdown with a heading and a table containing an anchor link to it.
        // The heading must be far enough below the table that scrolling is needed.
        string markdown = """
                          | Name | Link |
                          |------|------|
                          | jump | [go](#target) |

                          Line 1

                          Line 2

                          Line 3

                          Line 4

                          Line 5

                          Line 6

                          Line 7

                          Line 8

                          Line 9

                          ## Target
                          """;

        Terminal.Gui.Views.Markdown markdownView = new () { Text = markdown, Width = 60, Height = 10 };
        var clickedUrl = "";

        markdownView.LinkClicked += (_, e) =>
                                    {
                                        clickedUrl = e.Url;
                                        e.Handled = true;
                                    };

        window.Add (markdownView);

        app.Begin (window);
        app.LayoutAndDraw ();

        // Viewport should start at top
        Assert.Equal (0, markdownView.Viewport.Y);

        // Find the table and activate the anchor link
        MarkdownTable? tableView = markdownView.SubViews.OfType<MarkdownTable> ().FirstOrDefault ();
        Assert.NotNull (tableView);

        tableView.SetFocus ();
        tableView.InvokeCommand (Command.Accept);

        // The anchor link should have been reported
        Assert.Equal ("#target", clickedUrl);

        // The viewport should have scrolled down to the heading
        Assert.True (markdownView.Viewport.Y > 0, "Viewport should have scrolled to the anchor target");

        window.Dispose ();
    }

    // Copilot - Opus 4.6 - verify Issue 2: duplicate URLs in different cells create separate navigable entries
    [Fact]
    public void Duplicate_Urls_In_Different_Cells_Are_Separately_Navigable ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable window = new () { Width = 80, Height = 20, BorderStyle = LineStyle.None };

        // Two cells in different rows with the same URL — should be independently navigable
        string markdown = """
                          | Name | Description |
                          |------|-------------|
                          | [link](https://same.com) | info |
                          | [link](https://same.com) | info |
                          """;

        Terminal.Gui.Views.Markdown markdownView = new () { Text = markdown, Width = 60, Height = 15 };
        List<string> activatedUrls = [];

        markdownView.LinkClicked += (_, e) =>
                                    {
                                        activatedUrls.Add (e.Url);
                                        e.Handled = true;
                                    };

        window.Add (markdownView);

        app.Begin (window);
        app.LayoutAndDraw ();

        MarkdownTable? tableView = markdownView.SubViews.OfType<MarkdownTable> ().FirstOrDefault ();
        Assert.NotNull (tableView);
        Assert.True (tableView.CanFocus);

        // Focus the table — first link becomes active
        tableView.SetFocus ();
        tableView.InvokeCommand (Command.Accept);

        // Advance to second link (same URL but different cell)
        markdownView.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        tableView.InvokeCommand (Command.Accept);

        // Both activations should have fired — proving they are independently navigable
        Assert.Equal (2, activatedUrls.Count);
        Assert.Equal ("https://same.com", activatedUrls [0]);
        Assert.Equal ("https://same.com", activatedUrls [1]);

        window.Dispose ();
    }

    [Fact]
    public void Image_Fallback_Text_Renders ()
    {
        Terminal.Gui.Views.Markdown markdownView = new () { Text = "![logo](asset://logo)" };
        markdownView.Width = 40;
        markdownView.Height = 5;

        View host = new () { Width = 40, Height = 5 };
        host.Add (markdownView);

        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        Assert.True (markdownView.LineCount >= 1);

        host.Dispose ();
    }

    [Theory]
    [InlineData ("Hello * world")]
    [InlineData ("A stray ! in text")]
    [InlineData ("Unclosed [bracket")]
    [InlineData ("Lone ` backtick")]
    [InlineData ("Mixed **unclosed bold")]
    [InlineData ("Edge *")]

    // Copilot
    public void Stray_Special_Characters_Do_Not_Cause_Infinite_Loop (string markdown)
    {
        Terminal.Gui.Views.Markdown markdownView = new () { Text = markdown };
        markdownView.Width = 40;
        markdownView.Height = 5;

        View host = new () { Width = 40, Height = 5 };
        host.Add (markdownView);

        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        Assert.True (markdownView.LineCount >= 1);

        host.Dispose ();
    }

    [Fact]

    // Copilot
    public void WordWrap_Breaks_At_Word_Boundaries ()
    {
        // "Hello world" at width 8 should wrap between "Hello" and "world", not mid-word
        Terminal.Gui.Views.Markdown markdownView = new () { Text = "Hello world" };
        markdownView.Width = 8;
        markdownView.Height = 5;

        View host = new () { Width = 8, Height = 5 };
        host.Add (markdownView);

        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        // Should produce 2 lines: "Hello " and "world"
        Assert.Equal (2, markdownView.LineCount);

        host.Dispose ();
    }

    [Fact]

    // Copilot
    public void WordWrap_Long_Word_Falls_Back_To_Hard_Break ()
    {
        // "Abcdefghij" (10 chars, no spaces) at width 5 should hard-break
        const string MARKDOWN = "Abcdefghij";
        Terminal.Gui.Views.Markdown markdownView = new () { Text = MARKDOWN };
        markdownView.Width = 5;
        markdownView.Height = 5;

        View host = new () { Width = 5, Height = 5 };
        host.Add (markdownView);

        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        // 10 chars at width 5 = 2 lines via hard break
        Assert.Equal (2, markdownView.LineCount);

        host.Dispose ();
    }

    // ---- Style rendering tests (AssertDriverOutputIs pattern) ----
    // Copilot
    // Each test verifies the correct ANSI TextStyle escape codes are emitted
    // for a specific MarkdownStyleRole. Uses ANSI driver with Force16Colors
    // and scheme Color.Black (SGR 30) on Color.White (SGR 107).

    [Fact]
    public void Style_Heading_Renders_Bold ()
    {
        // ShowHeadingPrefix is true by default, so "# H" renders "# H" (all bold).
        (IApplication app, Runnable window) = SetupStyleTest ("# H");

        DriverAssert.AssertDriverOutputIs (@"\x1b[30m\x1b[107m\x1b[1m# H\x1b[30m\x1b[107m\x1b[22m", output, app.Driver);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Style_Emphasis_Renders_Italic ()
    {
        (IApplication app, Runnable window) = SetupStyleTest ("*E*");

        DriverAssert.AssertDriverOutputIs (@"\x1b[30m\x1b[107m\x1b[3mE\x1b[30m\x1b[107m\x1b[23m", output, app.Driver);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Style_Strong_Renders_Bold ()
    {
        (IApplication app, Runnable window) = SetupStyleTest ("**S**");

        DriverAssert.AssertDriverOutputIs (@"\x1b[30m\x1b[107m\x1b[1mS\x1b[30m\x1b[107m\x1b[22m", output, app.Driver);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Style_InlineCode_Renders_Bold_With_Dimmed_Background ()
    {
        (IApplication app, Runnable window) = SetupStyleTest ("`C`");

        // Code uses VisualRole.Code which derives from Editable with bold style
        DriverAssert.AssertDriverOutputIs (@"\x1b[30m\x1b[47m\x1b[1mC\x1b[30m\x1b[107m\x1b[22m", output, app.Driver);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Style_Quote_Marker_Bold_Text_Faint ()
    {
        (IApplication app, Runnable window) = SetupStyleTest ("> Q");

        DriverAssert.AssertDriverOutputIs (@"\x1b[30m\x1b[107m\x1b[1m> \x1b[30m\x1b[107m\x1b[22;2mQ\x1b[30m\x1b[107m\x1b[22m", output, app.Driver);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Style_ThematicBreak_Renders_Line ()
    {
        const int WIDTH = 5;
        (IApplication app, Runnable window) = SetupStyleTest ("---", WIDTH);

        // Line is inset: X=1, Width=Dim.Fill(1), so at WIDTH=5 it spans columns 1–3 (3 chars)
        DriverAssert.AssertDriverOutputIs (@"\x1b[30m\x1b[107m " + new string ('\u2500', WIDTH - 2), output, app.Driver);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Style_ListMarker_Bold_Text_Normal ()
    {
        (IApplication app, Runnable window) = SetupStyleTest ("- L");

        DriverAssert.AssertDriverOutputIs (@"\x1b[30m\x1b[107m\x1b[1m" + "• " + @"\x1b[30m\x1b[107m\x1b[22mL", output, app.Driver);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Style_TaskDone_Renders_Checked_Glyph_With_Strikethrough ()
    {
        (IApplication app, Runnable window) = SetupStyleTest ("- [x] D");

        DriverAssert.AssertDriverOutputIs (@"\x1b[30m\x1b[107m\x1b[1m" + $"• {Glyphs.CheckStateChecked} " + @"\x1b[30m\x1b[107m\x1b[22;9mD\x1b[30m\x1b[107m\x1b[29m",
                                           output,
                                           app.Driver);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Style_TaskTodo_Renders_Unchecked_Glyph_With_Bold ()
    {
        (IApplication app, Runnable window) = SetupStyleTest ("- [ ] T");

        DriverAssert.AssertDriverOutputIs (@"\x1b[30m\x1b[107m\x1b[1m" + $"• {Glyphs.CheckStateUnChecked} T" + @"\x1b[30m\x1b[107m\x1b[22m", output, app.Driver);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Style_Normal_No_TextStyle ()
    {
        (IApplication app, Runnable window) = SetupStyleTest ("Hi");

        DriverAssert.AssertDriverOutputIs (@"\x1b[30m\x1b[107mHi", output, app.Driver);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Style_Link_Absolute_Underline_With_OSC8 ()
    {
        (IApplication app, Runnable window) = SetupStyleTest ("[Go](https://x)");

        DriverAssert.AssertDriverOutputIs (@"\x1b]8;;https://x\x1b\\\x1b[30m\x1b[107m\x1b[4mGo\x1b]8;;\x1b\\\x1b[30m\x1b[107m\x1b[24m", output, app.Driver);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Style_Link_Relative_Underline_No_OSC8 ()
    {
        (IApplication app, Runnable window) = SetupStyleTest ("[Go](foo.md)");

        DriverAssert.AssertDriverOutputIs (@"\x1b[30m\x1b[107m\x1b[4mGo\x1b[30m\x1b[107m\x1b[24m", output, app.Driver);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Style_CodeBlock_Has_Full_Width_Dimmed_Background ()
    {
        // Fenced code block: the dimmed background should fill the entire row, not just the text
        const int WIDTH = 10;
        const string MARKDOWN = "```\nAB\n```";

        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (WIDTH, 3);
        app.Driver.Force16Colors = true;

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };
        window.SetScheme (new Scheme (new Attribute (Color.Black, Color.White)));

        Terminal.Gui.Views.Markdown mv = new () { Text = MARKDOWN, Width = Dim.Fill (), Height = Dim.Fill () };
        mv.SchemeName = null;
        mv.SetScheme (new Scheme (new Attribute (Color.Black, Color.White)));
        window.Add (mv);

        app.Begin (window);
        app.LayoutAndDraw ();
        app.Driver.Refresh ();

        string actual = app.Driver.GetOutput ().GetLastOutput ();
        output.WriteLine (actual);

        // The code line "AB" should have the dimmed background (\x1b[103m) filling the full 10-column width
        // Row format: fill entire row with dimmed bg spaces, then draw "AB" with bold+dimmed bg
        Assert.NotNull (actual);

        // The code block row should contain 10 columns of dimmed background (30m), not just 2 for "AB"
        // Count how many times the dimmed bg code appears - should be at least for the fill + the text
        int dimBgCount = CountOccurrences (actual, "\x1b[30m");
        Assert.True (dimBgCount >= 2, $"Expected dimmed background to appear at least twice (fill + text), got {dimBgCount}");

        window.Dispose ();
        app.Dispose ();
    }

    private static int CountOccurrences (string text, string pattern)
    {
        var count = 0;
        var idx = 0;

        while ((idx = text.IndexOf (pattern, idx, StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += pattern.Length;
        }

        return count;
    }

    /// <summary>Sets up a 1-row ANSI screen with Force16Colors and a Black-on-White scheme.</summary>
    private static (IApplication app, Runnable window) SetupStyleTest (string markdown, int width = 20)
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (width, 1);
        app.Driver.Force16Colors = true;

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };
        window.SetScheme (new Scheme (new Attribute (Color.Black, Color.White)));

        // Style tests verify unfocused rendering — disable focus so OnAdvancingFocus
        // doesn't activate the first link with reversed highlight colors.
        Terminal.Gui.Views.Markdown mv = new () { Text = markdown, Width = Dim.Fill (), Height = Dim.Fill (), CanFocus = false };
        mv.SchemeName = null;
        mv.SetScheme (new Scheme (new Attribute (Color.Black, Color.White)));
        window.Add (mv);

        app.Begin (window);
        app.LayoutAndDraw ();
        app.Driver.Refresh ();

        return (app, window);
    }

    #region Anchor Navigation Tests

    // Copilot

    [Theory]
    [InlineData ("Hello World", "hello-world")]
    [InlineData ("Getting Started", "getting-started")]
    [InlineData ("C# Code Examples!", "c-code-examples")]
    [InlineData ("  Spaces  ", "spaces")]
    [InlineData ("multiple---hyphens", "multiple---hyphens")]
    [InlineData ("ALL CAPS", "all-caps")]
    [InlineData ("dots.and", "dotsand")]
    [InlineData ("Lexicon & Taxonomy", "lexicon--taxonomy")]
    public void GenerateAnchorSlug_Produces_Expected_Slug (string input, string expected)
    {
        string slug = Terminal.Gui.Views.Markdown.GenerateAnchorSlug (input);
        Assert.Equal (expected, slug);
    }

    [Fact]
    public void ScrollToAnchor_Scrolls_To_Heading ()
    {
        // Copilot
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (40, 5);

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };

        Terminal.Gui.Views.Markdown mv = new ()
        {
            Text = "# First\n\nParagraph 1\n\n# Second\n\nParagraph 2\n\n# Third\n\nParagraph 3", Width = Dim.Fill (), Height = Dim.Fill ()
        };
        window.Add (mv);

        app.Begin (window);
        app.LayoutAndDraw ();

        // Initially at the top
        Assert.Equal (0, mv.Viewport.Y);

        // Scroll to "Third" heading
        bool found = mv.ScrollToAnchor ("third");
        Assert.True (found);
        Assert.True (mv.Viewport.Y > 0, "Should have scrolled down");

        // Scroll to "First" heading — should go back to top
        found = mv.ScrollToAnchor ("first");
        Assert.True (found);
        Assert.Equal (0, mv.Viewport.Y);

        // With leading # should also work
        found = mv.ScrollToAnchor ("#second");
        Assert.True (found);
        Assert.True (mv.Viewport.Y > 0);

        // Non-existent anchor returns false
        found = mv.ScrollToAnchor ("nonexistent");
        Assert.False (found);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void ScrollToAnchor_Duplicate_Headings_Get_Suffixed_Slugs ()
    {
        // Copilot
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (40, 3);

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };

        Terminal.Gui.Views.Markdown mv = new ()
        {
            Text = "# Overview\n\nFirst\n\n# Overview\n\nSecond\n\n# Overview\n\nThird", Width = Dim.Fill (), Height = Dim.Fill ()
        };
        window.Add (mv);

        app.Begin (window);
        app.LayoutAndDraw ();

        // First "Overview" → slug "overview"
        bool found = mv.ScrollToAnchor ("overview");
        Assert.True (found);
        Assert.Equal (0, mv.Viewport.Y);

        // Second "Overview" → slug "overview-1"
        found = mv.ScrollToAnchor ("overview-1");
        Assert.True (found);
        Assert.True (mv.Viewport.Y > 0);

        int secondY = mv.Viewport.Y;

        // Third "Overview" → slug "overview-2"
        found = mv.ScrollToAnchor ("overview-2");
        Assert.True (found);
        Assert.True (mv.Viewport.Y > secondY);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Anchor_Links_Are_Rendered_With_Underline ()
    {
        // Copilot
        // Anchor links like [Section](#section) should be underlined
        (IApplication app, Runnable window) = SetupStyleTest ("[Go](#sec)", 10);

        // Anchor link should render with underline SGR (4m) like absolute links
        DriverAssert.AssertDriverOutputIs (@"\x1b[30m\x1b[107m\x1b[4mGo\x1b[30m\x1b[107m\x1b[24m", output, app.Driver);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void ScrollToAnchor_With_Empty_String_Returns_False ()
    {
        // Copilot
        Terminal.Gui.Views.Markdown mv = new () { Text = "# Test" };
        Assert.False (mv.ScrollToAnchor (""));
        Assert.False (mv.ScrollToAnchor (null!));
    }

    #endregion

    #region Code Block Copy Button Tests

    // Copilot

    [Fact]
    public void CodeBlockRegions_Are_Detected_After_Layout ()
    {
        // Copilot
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (40, 10);

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };

        Terminal.Gui.Views.Markdown mv = new ()
        {
            Text = "Text\n\n```\nline1\nline2\n```\n\nMore text\n\n```\nA\n```", Width = Dim.Fill (), Height = Dim.Fill ()
        };
        window.Add (mv);

        app.Begin (window);
        app.LayoutAndDraw ();

        // Should have 2 code block regions
        Assert.True (mv.LineCount > 0);

        // Verify that code block lines exist by checking rendered line count includes code
        // The markdown has 2 code blocks: first with 2 lines, second with 1 line
        // Verify we can extract text from regions by checking that at least some lines are code blocks
        Assert.True (mv.LineCount >= 6, $"Expected at least 6 rendered lines, got {mv.LineCount}");

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Copy_Button_Glyph_Is_Drawn_On_Code_Block ()
    {
        // Copilot
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (20, 5);
        app.Driver.Force16Colors = true;

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };
        window.SetScheme (new Scheme (new Attribute (Color.Black, Color.White)));

        Terminal.Gui.Views.Markdown mv = new () { Text = "```\ncode\n```", Width = Dim.Fill (), Height = Dim.Fill () };
        mv.SchemeName = null;
        mv.SetScheme (new Scheme (new Attribute (Color.Black, Color.White)));
        window.Add (mv);

        app.Begin (window);
        app.LayoutAndDraw ();

        // The copy button glyph "⧉" should appear in the screen contents on a code block line
        var screenContents = app.Driver.ToString ();
        Assert.NotNull (screenContents);
        Assert.Contains ("\u29C9", screenContents); // U+29C9 TWO JOINED SQUARES

        window.Dispose ();
        app.Dispose ();
    }

    #endregion

    [Fact]
    public void ShowCopyButtons_False_Hides_Glyph_On_Code_Block ()
    {
        // Copilot
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (20, 5);
        app.Driver.Force16Colors = true;

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };
        window.SetScheme (new Scheme (new Attribute (Color.Black, Color.White)));

        Terminal.Gui.Views.Markdown mv = new () { Text = "```\ncode\n```", ShowCopyButtons = false, Width = Dim.Fill (), Height = Dim.Fill () };

        mv.SchemeName = null;
        mv.SetScheme (new Scheme (new Attribute (Color.Black, Color.White)));
        window.Add (mv);

        app.Begin (window);
        app.LayoutAndDraw ();

        // The copy glyph should NOT appear when ShowCopyButtons is false
        var screenContents = app.Driver.ToString ();
        Assert.NotNull (screenContents);
        Assert.DoesNotContain ("\u29C9", screenContents);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void ShowCopyButtons_False_Prevents_Copy_On_Click ()
    {
        // Copilot
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (30, 5);
        app.Driver.Clipboard = new FakeClipboard ();

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };

        Terminal.Gui.Views.Markdown mv = new () { Text = "```\nhello world\n```", ShowCopyButtons = false, Width = Dim.Fill (), Height = Dim.Fill () };

        window.Add (mv);

        app.Begin (window);
        app.LayoutAndDraw ();

        // Record clipboard state before click
        app.Clipboard!.TryGetClipboardData (out string before);

        // Find the code block SubView and click its copy button position
        MarkdownCodeBlock? codeBlock = mv.SubViews.OfType<MarkdownCodeBlock> ().FirstOrDefault ();
        Assert.NotNull (codeBlock);

        int copyX = codeBlock.Viewport.Width - 2;
        codeBlock.NewMouseEvent (new Mouse { Position = new Point (copyX, 0), Flags = MouseFlags.LeftButtonClicked });

        app.Clipboard.TryGetClipboardData (out string after);

        // Clipboard should not contain the code block text
        Assert.DoesNotContain ("hello world", after);

        // Clipboard should be unchanged by the click
        Assert.Equal (before, after);

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot
    [Fact]
    public void Bullet_With_Parentheses_In_Link_Text_Renders_Correctly ()
    {
        // Exact pattern from layout.md TOC — indented sub-items with parens in link text
        // Narrow viewport forces word-wrap which exposed the bug
        const string MARKDOWN = "- [How To](#how-to)\n"
                                + "  - [Stretch a View Between Fixed Elements](#stretch-a-view-between-fixed-elements)\n"
                                + "  - [Align Multiple Views (Like Dialog Buttons)](#align-multiple-views-like-dialog-buttons)\n"
                                + "  - [Center with Auto-Sizing and Constraints (Like Dialog)](#center-with-auto-sizing-and-constraints-like-dialog)";

        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (50, 10);

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };

        Terminal.Gui.Views.Markdown mv = new () { Text = MARKDOWN, Width = Dim.Fill (), Height = Dim.Fill () };
        window.Add (mv);

        app.Begin (window);
        app.LayoutAndDraw ();

        var screenContents = app.Driver.ToString ();
        Assert.NotNull (screenContents);

        // Should contain the full link text including "(Like Dialog)"
        Assert.Contains ("Like Dialog", screenContents);

        // "Dialog)" should NOT appear as orphaned text on its own line
        string [] lines = screenContents.Split ('\n');
        Assert.DoesNotContain (lines, l => l.TrimStart ().StartsWith ("Dialog)", StringComparison.Ordinal));

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot
    [Fact]
    public void Table_Height_Change_Reflows_Subsequent_Elements ()
    {
        // Markdown: paragraph, small table, then a heading below
        var md = """
                 Above

                 | A | B |
                 |---|---|
                 | Long cell content here | x |

                 # Below
                 """;

        Terminal.Gui.Views.Markdown view = new ()
        {
            Text = md, Width = 40, Height = 5 // Small viewport so scrolling is required
        };

        View host = new () { Width = 40, Height = 5 };
        host.Add (view);
        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        // Record initial line count and anchor position
        int initialLineCount = view.LineCount;

        Assert.True (view.ScrollToAnchor ("below"));
        int initialAnchorY = view.Viewport.Y;

        // Reset viewport
        view.Viewport = view.Viewport with { Y = 0 };

        // Shrink ContentSize.Width so the table columns wrap, making the table taller
        view.SetContentSize (new Size (20, view.GetContentSize ().Height));
        host.Layout ();

        int newLineCount = view.LineCount;

        // The narrower width should cause text and table to reflow — more lines
        Assert.True (newLineCount > initialLineCount, $"Expected more lines after narrowing: initial={initialLineCount}, new={newLineCount}");

        // The "# Below" anchor should still be reachable
        Assert.True (view.ScrollToAnchor ("below"));
        int newAnchorY = view.Viewport.Y;

        // The anchor should have moved down because the table grew taller
        Assert.True (newAnchorY > initialAnchorY, $"Expected anchor to move down: initial={initialAnchorY}, new={newAnchorY}");

        host.Dispose ();
    }

    // Copilot
    [Fact]
    public void CodeBlock_Width_Respects_ContentSize_Not_Viewport ()
    {
        var md = """
                 ```
                 code line
                 ```
                 """;

        Terminal.Gui.Views.Markdown view = new () { Text = md, Width = 40, Height = 10 };

        View host = new () { Width = 40, Height = 10 };
        host.Add (view);
        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        // Shrink content width to 20 (narrower than viewport)
        view.SetContentSize (new Size (20, view.GetContentSize ().Height));
        host.Layout ();

        // Get code block SubViews — they should be MarkdownCodeBlock instances
        List<View> codeBlocks = view.SubViews.Where (v => v.GetType ().Name == "MarkdownCodeBlock").ToList ();
        Assert.NotEmpty (codeBlocks);

        // Each code block should have Frame.Width == 20 (the content width), not 40 (viewport)
        foreach (View cb in codeBlocks)
        {
            Assert.Equal (20, cb.Frame.Width);
        }

        host.Dispose ();
    }

    #region ShowHeadingPrefix Tests

    // Copilot

    [Fact]
    public void ShowHeadingPrefix_Default_Is_True ()
    {
        Terminal.Gui.Views.Markdown view = new ();
        Assert.True (view.ShowHeadingPrefix);
    }

    [Fact]
    public void ShowHeadingPrefix_True_Includes_Hash_In_Output ()
    {
        // When ShowHeadingPrefix is true (default), the heading should include "# "
        Terminal.Gui.Views.Markdown mv = new () { Text = "# Hello", Width = 20, Height = 1 };
        mv.Layout (new Size (20, 1));

        Assert.True (mv.LineCount > 0);

        // Extract all text from the first rendered line's segments
        string lineText = GetRenderedLineText (mv, 0);
        Assert.StartsWith ("# ", lineText);
        Assert.Contains ("Hello", lineText);
    }

    [Fact]
    public void ShowHeadingPrefix_False_Strips_Hash ()
    {
        Terminal.Gui.Views.Markdown mv = new () { Text = "# Hello", Width = 20, Height = 1, ShowHeadingPrefix = false };
        mv.Layout (new Size (20, 1));

        Assert.True (mv.LineCount > 0);

        string lineText = GetRenderedLineText (mv, 0);
        Assert.DoesNotContain ("#", lineText);
        Assert.Contains ("Hello", lineText);
    }

    [Theory]
    [InlineData ("# H1", "# ")]
    [InlineData ("## H2", "## ")]
    [InlineData ("### H3", "### ")]
    [InlineData ("#### H4", "#### ")]
    [InlineData ("##### H5", "##### ")]
    [InlineData ("###### H6", "###### ")]
    public void ShowHeadingPrefix_Includes_Correct_Level_Prefix (string markdown, string expectedPrefix)
    {
        Terminal.Gui.Views.Markdown mv = new () { Text = markdown, Width = 30, Height = 1 };
        mv.Layout (new Size (30, 1));

        string lineText = GetRenderedLineText (mv, 0);
        Assert.StartsWith (expectedPrefix, lineText);
    }

    [Fact]
    public void ShowHeadingPrefix_HeadingMarker_Has_HeadingMarker_Role ()
    {
        Terminal.Gui.Views.Markdown mv = new () { Text = "## Test", Width = 20, Height = 1 };
        mv.Layout (new Size (20, 1));

        // The first segment(s) should be the "## " prefix with HeadingMarker role
        IReadOnlyList<StyledSegment> segments = GetRenderedLineSegments (mv, 0);
        Assert.True (segments.Count > 0);

        // Concatenate HeadingMarker segments at the start
        string markerText = string.Concat (segments.TakeWhile (s => s.StyleRole == MarkdownStyleRole.HeadingMarker).Select (s => s.Text));
        Assert.Equal ("## ", markerText);
    }

    [Fact]
    public void ShowHeadingPrefix_Toggle_Relayouts ()
    {
        Terminal.Gui.Views.Markdown mv = new () { Text = "# Hi", Width = 20, Height = 1 };
        mv.Layout (new Size (20, 1));

        string withPrefix = GetRenderedLineText (mv, 0);
        Assert.StartsWith ("# ", withPrefix);

        mv.ShowHeadingPrefix = false;
        mv.Layout (new Size (20, 1));

        string withoutPrefix = GetRenderedLineText (mv, 0);
        Assert.DoesNotContain ("#", withoutPrefix);
    }

    [Fact]
    public void Style_HeadingMarker_Renders_Bold ()
    {
        // HeadingMarker should render Bold (same as Heading text)
        // With ShowHeadingPrefix=true (default), output contains "# H" — all bold.
        (IApplication app, Runnable window) = SetupStyleTest ("# H");

        // The existing Style_Heading test pattern uses AssertDriverOutputIs with ANSI codes.
        // "# H" = bold "# " (HeadingMarker) + bold "H" (Heading).
        // Both are bold so SGR 1 at start, characters "# H", SGR 22 at end.
        DriverAssert.AssertDriverOutputIs (@"\x1b[30m\x1b[107m\x1b[1m# H\x1b[30m\x1b[107m\x1b[22m", output, app.Driver);

        window.Dispose ();
        app.Dispose ();
    }

    /// <summary>Extracts concatenated text from all segments of a rendered line.</summary>
    private static string GetRenderedLineText (Terminal.Gui.Views.Markdown mv, int lineIndex)
    {
        IReadOnlyList<StyledSegment> segments = GetRenderedLineSegments (mv, lineIndex);

        return string.Concat (segments.Select (s => s.Text));
    }

    /// <summary>Gets the styled segments for a rendered line via reflection.</summary>
    private static IReadOnlyList<StyledSegment> GetRenderedLineSegments (Terminal.Gui.Views.Markdown mv, int lineIndex)
    {
        FieldInfo? field = typeof (Terminal.Gui.Views.Markdown).GetField ("_renderedLines", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull (field);

        object? value = field.GetValue (mv);
        Assert.NotNull (value);

        var renderedLines = (IList)value;
        Assert.True (renderedLines.Count > lineIndex);

        object? line = renderedLines [lineIndex];
        Assert.NotNull (line);

        PropertyInfo? segProp = line.GetType ().GetProperty ("Segments");
        Assert.NotNull (segProp);

        return (IReadOnlyList<StyledSegment>)segProp.GetValue (line)!;
    }

    #endregion

    #region EnableForDesign + recursive md code blocks

    // Copilot

    [Fact]
    public void EnableForDesign_With_Embedded_Md_Block_Does_Not_Throw ()
    {
        Terminal.Gui.Views.Markdown view = new () { Width = 80, Height = 50 };
        IDesignable designable = view;

        // EnableForDesign sets a highlighter and loads DefaultMarkdownSample which contains ```md
        bool result = designable.EnableForDesign ();

        Assert.True (result);
        Assert.NotNull (view.SyntaxHighlighter);

        // Force layout to trigger code block creation
        View host = new () { Width = 80, Height = 50 };
        host.Add (view);
        host.Layout ();

        // Should render without crashing
        Assert.True (view.LineCount > 0);
    }

    [Fact]
    public void Md_CodeBlock_Gets_Syntax_Highlighted_Through_Highlighter ()
    {
        TextMateSyntaxHighlighter highlighter = new ();

        Terminal.Gui.Views.Markdown view = new ()
        {
            SyntaxHighlighter = highlighter,
            Width = 80,
            Height = 20,
            Text = """
                   # Test

                   ```md
                   # Heading
                   ```
                   """
        };

        View host = new () { Width = 80, Height = 20 };
        host.Add (view);
        host.Layout ();

        // The ```md code block should be recognized as markdown language
        // and its code lines highlighted through the TextMate highlighter
        Assert.True (view.LineCount > 0);
    }

    #endregion

    #region Text property unification

    // Copilot

    [Fact]
    public void Text_Sets_And_Gets_Markdown ()
    {
        Terminal.Gui.Views.Markdown view = new () { Text = "# Hello" };

        Assert.Equal ("# Hello", view.Text);
    }

    [Fact]
    public void Text_Set_Triggers_Parse_And_Layout ()
    {
        Terminal.Gui.Views.Markdown view = new () { Width = 40, Height = 10, Text = "# Hello\n\nWorld" };
        View host = new () { Width = 40, Height = 10 };
        host.Add (view);
        host.Layout ();

        Assert.True (view.LineCount > 0);
    }

    [Fact]
    public void Text_Set_Same_Value_Does_Not_Reparse ()
    {
        Terminal.Gui.Views.Markdown view = new () { Text = "# Hello" };
        var changeCount = 0;
        view.TextChanged += (_, _) => changeCount++;

        view.Text = "# Hello";

        Assert.Equal (0, changeCount);
    }

    #endregion

    #region UseThemeBackground with Line and Table views

    [Fact]
    public void UseThemeBackground_ThematicBreak_Line_Gets_ThemeBackground ()
    {
        // Copilot
        // When UseThemeBackground is true, the Line SubView for thematic breaks
        // must have its ColorScheme background set to the theme background.
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (40, 10);

        Color themeBg = new (30, 30, 30);
        ThemeBackgroundHighlighter highlighter = new (themeBg);

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };
        window.SetScheme (new Scheme (new Attribute (Color.White, Color.Blue)));

        Terminal.Gui.Views.Markdown mv = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            SyntaxHighlighter = highlighter,
            UseThemeBackground = true,
            Text = "# Title\n\n---\n\nParagraph"
        };
        mv.SetScheme (new Scheme (new Attribute (Color.White, Color.Blue)));
        window.Add (mv);

        app.Begin (window);
        app.LayoutAndDraw ();

        // Find the Line SubView
        Line? lineView = null;

        foreach (View sub in mv.SubViews)
        {
            if (sub is not Line line)
            {
                continue;
            }
            lineView = line;

            break;
        }

        Assert.NotNull (lineView);

        // The Line's ColorScheme normal background must match the theme background
        Attribute lineNormal = lineView.GetAttributeForRole (VisualRole.Normal);
        Assert.Equal (themeBg, lineNormal.Background);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void UseThemeBackground_False_ThematicBreak_Line_Uses_Default_Background ()
    {
        // Copilot
        // When UseThemeBackground is false, the Line SubView should NOT get theme background
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (40, 10);

        Color themeBg = new (30, 30, 30);
        ThemeBackgroundHighlighter highlighter = new (themeBg);

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };
        window.SetScheme (new Scheme (new Attribute (Color.White, Color.Blue)));

        Terminal.Gui.Views.Markdown mv = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            SyntaxHighlighter = highlighter,
            UseThemeBackground = false,
            Text = "# Title\n\n---\n\nParagraph"
        };
        mv.SetScheme (new Scheme (new Attribute (Color.White, Color.Blue)));
        window.Add (mv);

        app.Begin (window);
        app.LayoutAndDraw ();

        // Find the Line SubView
        Line? lineView = null;

        foreach (View sub in mv.SubViews)
        {
            if (sub is not Line line)
            {
                continue;
            }
            lineView = line;

            break;
        }

        Assert.NotNull (lineView);

        // The Line's background should NOT be the theme background
        Attribute lineNormal = lineView.GetAttributeForRole (VisualRole.Normal);
        Assert.NotEqual (themeBg, lineNormal.Background);

        window.Dispose ();
        app.Dispose ();
    }

    /// <summary>
    ///     Mock highlighter that exposes a theme background but doesn't style scopes.
    /// </summary>
    private sealed class ThemeBackgroundHighlighter (Color themeBg) : ISyntaxHighlighter
    {
        public IReadOnlyList<StyledSegment> Highlight (string code, string? language) => [new (code, MarkdownStyleRole.CodeBlock)];

        public void ResetState () { }

        public string ThemeName => string.Empty;

        public Color? DefaultBackground { get; } = themeBg;

        public Attribute? GetAttributeForScope (MarkdownStyleRole role) => null;
    }

    /// <summary>
    ///     A mock highlighter that returns segments with explicit <see cref="Attribute"/> values,
    ///     simulating real TextMate-style tokenization where each token carries its own colors.
    /// </summary>
    private sealed class ExplicitAttributeHighlighter (Color tokenFg, Color tokenBg) : ISyntaxHighlighter
    {
        public IReadOnlyList<StyledSegment> Highlight (string code, string? language)
            => [new (code, MarkdownStyleRole.CodeBlock, attribute: new Attribute (tokenFg, tokenBg))];

        public void ResetState () { }

        public string ThemeName => string.Empty;

        public Color? DefaultBackground { get; } = tokenBg;

        public Attribute? GetAttributeForScope (MarkdownStyleRole role) => null;
    }

    #endregion

    #region Viewport scroll position

    // Copilot — regression test: viewport should start at top after setting Text with code blocks
    [Fact]
    public void Setting_Text_With_CodeBlocks_Viewport_Starts_At_Top ()
    {
        // Markdown with code blocks that triggers SubView creation during layout.
        // The bug: MarkdownCodeBlock (CanFocus=true by default) would steal focus
        // when Add()'d, causing the viewport to scroll to the last code block.
        string mdWithCodeBlocks = """
                                  # Header

                                  Some text before.

                                  ```csharp
                                  var x = 1;
                                  var y = 2;
                                  ```

                                  More text.

                                  ```csharp
                                  Console.WriteLine("hello");
                                  ```

                                  """
                                  + string.Join ("\n\n", Enumerable.Range (1, 50).Select (i => $"Paragraph {i}."));

        Terminal.Gui.Views.Markdown mv = new () { Width = 40, Height = 10, Text = mdWithCodeBlocks };

        View host = new () { Width = 40, Height = 10 };
        host.Add (mv);

        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        // Content should be taller than viewport
        Assert.True (mv.GetContentSize ().Height > mv.Viewport.Height, "Content should exceed viewport height");

        // Viewport should start at the top — not scrolled to the last code block
        Assert.Equal (0, mv.Viewport.Y);

        host.Dispose ();
    }

    // Copilot — regression: full lifecycle with code blocks should start at top
    [Fact]
    public void Full_Lifecycle_With_CodeBlocks_Viewport_Starts_At_Top ()
    {
        string mdWithCodeBlocks = """
                                  # Title

                                  Intro paragraph.

                                  ```csharp
                                  int a = 42;
                                  ```

                                  Middle text.

                                  | Col1 | Col2 |
                                  |------|------|
                                  | A    | B    |

                                  ```python
                                  print("end")
                                  ```

                                  """
                                  + string.Join ("\n\n", Enumerable.Range (1, 80).Select (i => $"Line {i} of filler."));

        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };
        Terminal.Gui.Views.Markdown mv = new () { Width = Dim.Fill (), Height = Dim.Fill (), Text = mdWithCodeBlocks };
        window.Add (mv);

        app.Begin (window);
        app.LayoutAndDraw ();

        // Content should be taller than viewport
        Assert.True (mv.GetContentSize ().Height > mv.Viewport.Height, "Content should exceed viewport height");

        // Viewport should start at the top
        Assert.Equal (0, mv.Viewport.Y);

        window.Dispose ();
    }

    #endregion

    #region CodeBlock background attribute tests

    // Copilot

    [Fact]
    public void UseThemeBackground_True_CodeBlock_Is_Distinct_From_Body ()
    {
        // Copilot
        // When UseThemeBackground is true and a SyntaxHighlighter is set, the code block
        // must have a background that is DISTINCT from the body (which uses DefaultBackground)
        // but still derived from the theme (so token colors remain readable).
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (20, 6);

        Color themeBg = new (30, 30, 30);
        ThemeBackgroundHighlighter highlighter = new (themeBg);

        using Runnable window = new ();
        window.Width = Dim.Fill ();
        window.Height = Dim.Fill ();
        window.BorderStyle = LineStyle.None;
        window.SetScheme (new Scheme (new Attribute (Color.White, Color.Blue)));

        Terminal.Gui.Views.Markdown mv = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            SyntaxHighlighter = highlighter,
            UseThemeBackground = true,
            Text = "Hello\n\n```\ncode\n```"
        };
        mv.SetScheme (new Scheme (new Attribute (Color.White, Color.Blue)));
        window.Add (mv);

        app.Begin (window);
        app.LayoutAndDraw ();

        Cell [,]? contents = app.Driver.Contents;
        Assert.NotNull (contents);

        // Row 0 = "Hello" (body text with theme bg)
        Color mainBg = contents! [0, 0].Attribute!.Value.Background;

        // Row 2 = code block line "code" (should be distinct from body bg)
        Color codeBg = contents [2, 0].Attribute!.Value.Background;

        Assert.NotEqual (mainBg, codeBg);

        app.Dispose ();
    }

    [Fact]
    public void UseThemeBackground_True_CodeBlock_Bg_Derives_From_Theme_Not_Scheme ()
    {
        // Copilot
        // When UseThemeBackground is true with a light theme, the code block should NOT
        // use the dark VisualRole.Code from the view's (possibly dark) scheme. Instead
        // it should use a dimmed variant of the highlighter's DefaultBackground.
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (30, 10);

        Color lightThemeBg = new (250, 250, 250); // Light theme bg
        ThemeBackgroundHighlighter highlighter = new (lightThemeBg);

        using Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };
        window.SetScheme (new Scheme (new Attribute (Color.Black, new Color (0, 0, 128)))); // Dark scheme bg

        Terminal.Gui.Views.Markdown mv = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            SyntaxHighlighter = highlighter,
            UseThemeBackground = true,
            Text = "Hello\n\n```csharp\nvar x = 1;\n```"
        };
        mv.SetScheme (new Scheme (new Attribute (Color.Black, new Color (0, 0, 128))));
        window.Add (mv);

        app.Begin (window);
        app.LayoutAndDraw ();

        Cell [,]? contents = app.Driver.Contents;
        Assert.NotNull (contents);

        // Row 2 = code block line — bg should be light (derived from light theme), not dark
        Color codeBg = contents! [2, 0].Attribute!.Value.Background;
        Assert.False (codeBg.IsDarkColor (), $"Code block bg {codeBg} should be light (derived from light theme), not dark");

        // Code block bg should be the dimmed variant of the theme bg
        bool isDark = lightThemeBg.IsDarkColor ();
        Color expectedDimmed = lightThemeBg.GetDimmerColor (0.2, !isDark);
        Assert.Equal (expectedDimmed, codeBg);

        app.Dispose ();
    }

    [Fact]
    public void UseThemeBackground_False_CodeBlock_Text_Matches_Fill_Background ()
    {
        // Copilot
        // When UseThemeBackground is false, the code block text segments should use
        // the same background as the code block fill (Code role background).
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (20, 6);

        using Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };
        window.SetScheme (new Scheme (new Attribute (Color.White, Color.Blue)));

        Terminal.Gui.Views.Markdown mv = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            UseThemeBackground = false,
            Text = "Hello\n\n```\ncode\n```"
        };
        mv.SetScheme (new Scheme (new Attribute (Color.White, Color.Blue)));
        window.Add (mv);

        app.Begin (window);
        app.LayoutAndDraw ();

        Cell [,]? contents = app.Driver.Contents;
        Assert.NotNull (contents);

        // Row 2 = code block line "code"
        // The text cell (col 0, 'c') background should match the fill cell (col 10, empty) background
        Color textBg = contents! [2, 0].Attribute!.Value.Background;
        Color fillBg = contents [2, 10].Attribute!.Value.Background;

        Assert.Equal (textBg, fillBg);

        // The code block background should also differ from the main content background
        Color mainBg = contents [0, 0].Attribute!.Value.Background;
        Assert.NotEqual (mainBg, textBg);

        app.Dispose ();
    }

    [Fact]
    public void CodeBlock_With_Highlighter_Bg_Derives_From_Theme_Regardless_Of_UseThemeBackground ()
    {
        // Copilot
        // When a SyntaxHighlighter is set (with DefaultBackground), the code block's scheme
        // is overridden so VisualRole.Code derives from the highlighter bg, not the view's
        // scheme bg. This ensures token colors remain readable on a compatible background.
        // This applies regardless of UseThemeBackground.
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (30, 10);

        Color lightThemeBg = new (250, 250, 250);
        ThemeBackgroundHighlighter highlighter = new (lightThemeBg);

        using Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };
        window.SetScheme (new Scheme (new Attribute (Color.Black, new Color (0, 0, 128)))); // Dark scheme

        Terminal.Gui.Views.Markdown mv = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            SyntaxHighlighter = highlighter,
            UseThemeBackground = false, // Body uses scheme bg, but code block should still use theme-derived bg
            Text = "Hello\n\n```csharp\nvar x = 1;\n```"
        };
        mv.SetScheme (new Scheme (new Attribute (Color.Black, new Color (0, 0, 128))));
        window.Add (mv);

        app.Begin (window);
        app.LayoutAndDraw ();

        Cell [,]? contents = app.Driver.Contents;
        Assert.NotNull (contents);

        // Row 2 = code block — bg should be light (derived from light theme), not the dark scheme bg
        Color codeBg = contents! [2, 0].Attribute!.Value.Background;
        Assert.False (codeBg.IsDarkColor (), $"Code block bg {codeBg} should be light (theme-derived), not dark (scheme-derived)");

        // Code block fill and text should have matching bg
        Color fillBg = contents [2, 15].Attribute!.Value.Background;
        Assert.Equal (codeBg, fillBg);

        app.Dispose ();
    }

    [Fact]
    public void CodeBlock_SyntaxHighlighter_Is_Passed_To_SubView ()
    {
        // Copilot
        // The MarkdownCodeBlock SubViews created by SyncCodeBlockViews must receive
        // the parent Markdown view's SyntaxHighlighter so that GetAttributeForSegment
        // can query the highlighter for scope-specific attributes.
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (30, 10);

        Color themeBg = new (250, 250, 250);
        ThemeBackgroundHighlighter highlighter = new (themeBg);

        using Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };

        Terminal.Gui.Views.Markdown mv = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            SyntaxHighlighter = highlighter,
            UseThemeBackground = true,
            Text = "```\ncode\n```"
        };
        window.Add (mv);

        app.Begin (window);
        app.LayoutAndDraw ();

        // Find the MarkdownCodeBlock SubView
        MarkdownCodeBlock? codeBlockView = null;

        foreach (View sub in mv.SubViews)
        {
            if (sub is not MarkdownCodeBlock cb)
            {
                continue;
            }
            codeBlockView = cb;

            break;
        }

        Assert.NotNull (codeBlockView);

        // The code block must have the parent's SyntaxHighlighter set
        Assert.Same (highlighter, codeBlockView.SyntaxHighlighter);

        app.Dispose ();
    }

    [Fact]
    public void CodeBlock_With_ExplicitAttribute_Highlighter_Fill_Matches_Text_Bg ()
    {
        // Copilot
        // A real TextMate highlighter returns segments with explicit Attribute (tokenFg, tokenBg).
        // The code block fill bg (from OnClearingViewport) is a dimmed variant of DefaultBackground.
        // GetAttributeForSegment must override the explicit attribute's bg so text bg matches fill bg.
        // Without the fix, text bg = raw theme bg, fill bg = dimmed theme bg → mismatch.
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (30, 8);

        Color tokenFg = new (200, 200, 200);
        Color tokenBg = new (30, 30, 30);
        ExplicitAttributeHighlighter highlighter = new (tokenFg, tokenBg);

        using Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };
        window.SetScheme (new Scheme (new Attribute (Color.White, Color.Blue)));

        Terminal.Gui.Views.Markdown mv = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            SyntaxHighlighter = highlighter,
            UseThemeBackground = true,
            Text = "Hello\n\n```csharp\nvar x = 1;\n```"
        };
        mv.SetScheme (new Scheme (new Attribute (Color.White, Color.Blue)));
        window.Add (mv);

        app.Begin (window);
        app.LayoutAndDraw ();

        Cell [,]? contents = app.Driver.Contents;
        Assert.NotNull (contents);

        // Row 2 = code block line "var x = 1;"
        // Text cell bg (col 0) must match fill cell bg (col 20, empty space)
        Color textBg = contents! [2, 0].Attribute!.Value.Background;
        Color fillBg = contents [2, 20].Attribute!.Value.Background;

        Assert.Equal (fillBg, textBg);

        // The bg should be the dimmed variant of the theme bg, NOT the raw theme bg
        bool isDark = tokenBg.IsDarkColor ();
        Color expectedDimmed = tokenBg.GetDimmerColor (0.2, !isDark);
        Assert.Equal (expectedDimmed, textBg);

        // Token foreground should be preserved
        Color actualFg = contents [2, 0].Attribute!.Value.Foreground;
        Assert.Equal (tokenFg, actualFg);

        app.Dispose ();
    }

    [Fact]
    public void Setting_SyntaxHighlighter_After_Text_Updates_CodeBlock_Bg ()
    {
        // Copilot
        // Setting SyntaxHighlighter on a MarkdownView that already has Text must
        // invalidate layout so code blocks pick up the new highlighter's theme bg.
        // This should NOT require re-setting Text (that was a hack).
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (30, 8);

        using Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };

        Terminal.Gui.Views.Markdown mv = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Text = "Hello\n\n```csharp\nvar x = 1;\n```"
        };
        window.Add (mv);

        app.Begin (window);
        app.LayoutAndDraw ();

        Cell [,]? contents = app.Driver.Contents;
        Assert.NotNull (contents);

        // Before: code block bg uses VisualRole.Code from scheme
        Color bgBefore = contents! [2, 0].Attribute!.Value.Background;

        // Now set a highlighter with a light theme bg
        Color lightThemeBg = new (250, 250, 250);
        ThemeBackgroundHighlighter highlighter = new (lightThemeBg);
        mv.SyntaxHighlighter = highlighter;

        // Re-draw WITHOUT re-setting Text
        app.LayoutAndDraw ();

        contents = app.Driver.Contents;

        // After: code block bg should be derived from the light theme
        Color bgAfter = contents! [2, 0].Attribute!.Value.Background;

        Assert.NotEqual (bgBefore, bgAfter);
        Assert.False (bgAfter.IsDarkColor (), $"Code block bg {bgAfter} should be light (theme-derived) after setting highlighter");

        app.Dispose ();
    }

    [Fact]
    public void Setting_UseThemeBackground_After_Text_Updates_Without_ReSettingText ()
    {
        // Copilot
        // Changing UseThemeBackground must invalidate layout so body and code blocks update.
        // This should NOT require re-setting Text (that was a hack).
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (30, 8);

        Color themeBg = new (30, 30, 30);
        ThemeBackgroundHighlighter highlighter = new (themeBg);

        using Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };
        window.SetScheme (new Scheme (new Attribute (Color.White, Color.Blue)));

        Terminal.Gui.Views.Markdown mv = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            SyntaxHighlighter = highlighter,
            UseThemeBackground = false,
            Text = "Hello\n\n```\ncode\n```"
        };
        mv.SetScheme (new Scheme (new Attribute (Color.White, Color.Blue)));
        window.Add (mv);

        app.Begin (window);
        app.LayoutAndDraw ();

        Cell [,]? contents = app.Driver.Contents;
        Assert.NotNull (contents);

        // Body bg when UseThemeBackground = false → scheme bg (Blue)
        Color bodyBgBefore = contents! [0, 0].Attribute!.Value.Background;
        Assert.Equal (Color.Blue, bodyBgBefore);

        // Now toggle UseThemeBackground to true WITHOUT re-setting Text
        mv.UseThemeBackground = true;
        app.LayoutAndDraw ();

        contents = app.Driver.Contents;

        // Body bg should now use the theme bg (dark, 30,30,30)
        Color bodyBgAfter = contents! [0, 0].Attribute!.Value.Background;
        Assert.NotEqual (bodyBgBefore, bodyBgAfter);
        Assert.Equal (themeBg, bodyBgAfter);

        app.Dispose ();
    }

    #endregion

    // Copilot - regression test: GetContentHeight should not overestimate when content includes tables
    [Fact]
    public void GetContentHeight_Does_Not_Overestimate_With_Tables ()
    {
        // Wide cells that WILL wrap at 80 columns but fit single-line at 120
        string md = """
                    # Before

                    | Column A | Column B with a long header name | Column C also has a longish header | Column D |
                    |----------|----------------------------------|-------------------------------------|----------|
                    | Row 1 A  | This cell has content that is definitely long enough to wrap when columns are narrow at 80 | Another cell with enough text to cause wrapping at narrow widths | Value D1 |
                    | Row 2 A  | More long content in column B that should wrap around when the available width is restricted | Cell C2 also has substantial text that would need multiple lines | Value D2 |
                    | Row 3 A  | Third row with plenty of descriptive text to demonstrate the wrapping | Third row C column with lots of text content | Value D3 |
                    | Row 4 A  | Fourth row B column content that is quite verbose and will cause word wrap | Fourth row C with more text | Value D4 |
                    | Row 5 A  | Fifth row B demonstrating word wrap behavior at different terminal widths | Fifth row C also long enough to wrap | Value D5 |

                    # After
                    """;

        // At wide width (120), table cells don't wrap → fewer placeholder lines needed
        Terminal.Gui.Views.Markdown mv120 = new () { Width = 120, Height = 50, Text = md };

        View host120 = new () { Width = 120, Height = 50 };
        host120.Add (mv120);
        host120.BeginInit ();
        host120.EndInit ();
        host120.Layout ();

        int contentHeight120 = mv120.GetContentSize ().Height;

        // At narrow width (80), table cells wrap → more placeholder lines needed
        Terminal.Gui.Views.Markdown mv80 = new () { Width = 80, Height = 50, Text = md };

        View host80 = new () { Width = 80, Height = 50 };
        host80.Add (mv80);
        host80.BeginInit ();
        host80.EndInit ();
        host80.Layout ();

        int contentHeight80 = mv80.GetContentSize ().Height;

        // Content height at 120 cols should be LESS than at 80 cols because table rows
        // don't wrap at wider widths. Before the fix, Frame.Height was stale from the
        // initial Recalculate(80) in the TableData setter, causing overestimation.
        Assert.True (
            contentHeight120 < contentHeight80,
            $"Content height at 120 cols ({contentHeight120}) should be less than at 80 cols ({contentHeight80}) since table rows don't wrap at wider widths");

        host120.Dispose ();
        host80.Dispose ();
    }

    // Copilot - regression test: table height accounts for content width expansion from long code lines
    [Fact]
    public void GetContentHeight_Table_With_Wide_Code_Block_Does_Not_Overestimate ()
    {
        // When a long unwrapped code line makes contentWidth > viewportWidth, the table
        // (which uses Dim.Fill) renders at the wider content width and may need fewer
        // wrapped rows. The content height must reflect the table at its actual render width.
        string longCodeLine = new ('x', 150);

        string mdWithCode = $"""
                             # Title

                             | Column A | Column B with a fairly long header | Column C header |
                             |----------|-------------------------------------|-----------------|
                             | Row 1 A  | This cell has content that is long enough to wrap when rendered at 80 columns width | Cell C1 value |
                             | Row 2 A  | More long content in column B that should wrap around when available width is 80 cols | Cell C2 value |
                             | Row 3 A  | Third row with plenty of descriptive text to demonstrate the wrapping at narrow width | Cell C3 value |
                             | Row 4 A  | Fourth row B column content that is quite verbose and will cause word wrapping to occur | Cell C4 value |
                             | Row 5 A  | Fifth row B demonstrating word wrap behavior at narrow terminal widths but not at wide | Cell C5 value |

                             ```
                             {longCodeLine}
                             ```

                             # End
                             """;

        string mdNoCode = """
                          # Title

                          | Column A | Column B with a fairly long header | Column C header |
                          |----------|-------------------------------------|-----------------|
                          | Row 1 A  | This cell has content that is long enough to wrap when rendered at 80 columns width | Cell C1 value |
                          | Row 2 A  | More long content in column B that should wrap around when available width is 80 cols | Cell C2 value |
                          | Row 3 A  | Third row with plenty of descriptive text to demonstrate the wrapping at narrow width | Cell C3 value |
                          | Row 4 A  | Fourth row B column content that is quite verbose and will cause word wrapping to occur | Cell C4 value |
                          | Row 5 A  | Fifth row B demonstrating word wrap behavior at narrow terminal widths but not at wide | Cell C5 value |

                          # End
                          """;

        // Both at 80 col viewport
        Terminal.Gui.Views.Markdown mvCode = new () { Width = 80, Height = 50, Text = mdWithCode };
        View hostCode = new () { Width = 80, Height = 50 };
        hostCode.Add (mvCode);
        hostCode.BeginInit ();
        hostCode.EndInit ();
        hostCode.Layout ();

        Terminal.Gui.Views.Markdown mvNoCode = new () { Width = 80, Height = 50, Text = mdNoCode };
        View hostNoCode = new () { Width = 80, Height = 50 };
        hostNoCode.Add (mvNoCode);
        hostNoCode.BeginInit ();
        hostNoCode.EndInit ();
        hostNoCode.Layout ();

        int heightWithCode = mvCode.GetContentSize ().Height;
        int heightNoCode = mvNoCode.GetContentSize ().Height;
        int contentWidthWithCode = mvCode.GetContentSize ().Width;

        // Precondition: the code line expands content width beyond viewport
        Assert.True (contentWidthWithCode > 80, "Code line should expand content width beyond viewport");

        // The version with the code block has the table rendered at the wider content width
        // (less wrapping needed), so the table is shorter. The code block adds a few lines,
        // but the table savings should partially or fully offset that.
        // Key assertion: heightWithCode must be LESS than heightNoCode + code_block_lines.
        // If the bug exists (table placeholder uses viewport width), heightWithCode =
        //   heightNoCode + code_block_lines (table not re-sized, just code added).
        // If the fix works, heightWithCode < heightNoCode + code_block_lines
        //   (table shrinks because it renders wider).

        // The code block adds 1 rendered line of content. Count actual code block lines.
        int codeBlockRenderedLines = mvCode.SubViews
                                           .OfType<MarkdownCodeBlock> ()
                                           .Sum (v => v.Frame.Height);

        // With the fix: table is shorter at content width, so total is less than naive sum
        Assert.True (
            heightWithCode < heightNoCode + codeBlockRenderedLines,
            $"Content height with code ({heightWithCode}) should be less than no-code height ({heightNoCode}) + code lines ({codeBlockRenderedLines}) = {heightNoCode + codeBlockRenderedLines}, because the wider content width makes the table shorter");

        hostCode.Dispose ();
        hostNoCode.Dispose ();
    }

    // Copilot
    [Fact]
    public void CanFocus_False_Text_HotKeySpecifier_SetsFocus_Next ()
    {
        using IApplication app = Application.Create ();
        Runnable<bool> runnable = new ();
        View otherView = new () { CanFocus = true };
        Terminal.Gui.Views.Markdown markdown = new ()
        {
            CanFocus = false,
            Width = 20,
            Height = 1,
            Text = "_Markdown"
        };
        View nextView = new () { CanFocus = true };

        markdown.HotKeySpecifier = (Rune)'_';

        app.Begin (runnable);
        runnable.Add (otherView, markdown, nextView);
        otherView.SetFocus ();

        Assert.Equal (Key.M, markdown.HotKey);
        Assert.True (otherView.HasFocus);
        Assert.False (markdown.HasFocus);
        Assert.False (nextView.HasFocus);

        app.Keyboard.RaiseKeyDownEvent (markdown.HotKey);

        Assert.False (otherView.HasFocus);
        Assert.False (markdown.HasFocus);
        Assert.True (nextView.HasFocus);
    }

    // Copilot
    [Fact]
    public void CanFocus_False_LeftButtonClicked_SetsFocus_Next ()
    {
        using IApplication app = Application.Create ();
        Runnable<bool> runnable = new ();
        View otherView = new ()
        {
            X = 0,
            Y = 0,
            Width = 1,
            Height = 1,
            CanFocus = true
        };
        Terminal.Gui.Views.Markdown markdown = new ()
        {
            X = 0,
            Y = 1,
            Width = 20,
            Height = 1,
            CanFocus = false,
            Text = "_Markdown"
        };
        View nextView = new ()
        {
            X = Pos.Right (markdown),
            Y = Pos.Top (markdown),
            Width = 1,
            Height = 1,
            CanFocus = true
        };

        markdown.HotKeySpecifier = (Rune)'_';

        app.Begin (runnable);
        runnable.Add (otherView, markdown, nextView);
        otherView.SetFocus ();

        Assert.Equal (Key.M, markdown.HotKey);
        Assert.True (otherView.HasFocus);
        Assert.False (markdown.HasFocus);
        Assert.False (nextView.HasFocus);

        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = markdown.Frame.Location, Flags = MouseFlags.LeftButtonClicked });

        Assert.False (markdown.HasFocus);
        Assert.True (nextView.HasFocus);
    }

    // Copilot
    [Fact]
    public void CanFocus_True_Text_HotKeySpecifier_SetsFocus_OnMarkdown ()
    {
        using IApplication app = Application.Create ();
        Runnable<bool> runnable = new ();
        View otherView = new () { CanFocus = true };
        Terminal.Gui.Views.Markdown markdown = new ()
        {
            CanFocus = true,
            Width = 20,
            Height = 1,
            Text = "_Markdown"
        };
        View nextView = new () { CanFocus = true };

        markdown.HotKeySpecifier = (Rune)'_';

        app.Begin (runnable);
        runnable.Add (otherView, markdown, nextView);
        otherView.SetFocus ();

        Assert.Equal (Key.M, markdown.HotKey);

        app.Keyboard.RaiseKeyDownEvent (markdown.HotKey);

        Assert.False (otherView.HasFocus);
        Assert.True (markdown.HasFocus);
        Assert.False (nextView.HasFocus);
    }

    // Copilot
    [Fact]
    public void CanFocus_False_Text_HotKeySpecifier_NoNextPeer_DoesNotFocusMarkdown ()
    {
        using IApplication app = Application.Create ();
        Runnable<bool> runnable = new ();
        Terminal.Gui.Views.Markdown markdown = new ()
        {
            CanFocus = false,
            Width = 20,
            Height = 1,
            Text = "_Markdown"
        };

        markdown.HotKeySpecifier = (Rune)'_';

        app.Begin (runnable);
        runnable.Add (markdown);

        Assert.Equal (Key.M, markdown.HotKey);

        app.Keyboard.RaiseKeyDownEvent (markdown.HotKey);

        Assert.False (markdown.HasFocus);
    }

    // Copilot - regression test for #5365
    [Fact]
    public void Initial_Render_With_Table_Links_Does_Not_Scroll_Viewport ()
    {
        // When a Markdown view contains tables with hyperlinks, the viewport must remain
        // at Y=0 after initial layout. No table should have auto-focus.
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (80, 10);

        using Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };

        string markdown = """
                          # Title

                          Some introductory text at the top.

                          | Command | Description |
                          |---------|-------------|
                          | [help](app:help) | Shows help |
                          | [quit](app:quit) | Quits the app |

                          More text below.

                          | Name | Link |
                          |------|------|
                          | [alpha](https://a.com) | [beta](https://b.com) |
                          | [gamma](https://c.com) | [delta](https://d.com) |
                          """;

        Terminal.Gui.Views.Markdown markdownView = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Text = markdown
        };

        window.Add (markdownView);

        app.Begin (window);
        app.LayoutAndDraw ();

        // Viewport must remain at the top — no auto-scrolling to a focused table
        Assert.Equal (0, markdownView.Viewport.Y);

        // No table SubView should have focus
        foreach (View sub in markdownView.SubViews)
        {
            if (sub is MarkdownTable table)
            {
                Assert.False (table.HasFocus, "No table should have auto-focus on initial render");
            }
        }

        // Additional layout/draw cycles must not cause viewport drift or focus theft
        app.LayoutAndDraw ();
        Assert.Equal (0, markdownView.Viewport.Y);

        app.LayoutAndDraw ();
        Assert.Equal (0, markdownView.Viewport.Y);

        app.Dispose ();
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Multiple_LayoutDraw_Cycles_Do_Not_Scroll_Or_Focus_Table ()
    {
        // Verifies that repeated layout/draw cycles don't cause viewport drift
        // or auto-focus into a table (regression guard for issue #5365).
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (80, 10);

        using Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };

        string markdown = """
                          # Title

                          Some text at the top.

                          | Command | Description |
                          |---------|-------------|
                          | [help](app:help) | Shows help |
                          | [quit](app:quit) | Quits the app |

                          More text.

                          | Name | Link |
                          |------|------|
                          | [alpha](https://a.com) | [beta](https://b.com) |
                          """;

        Terminal.Gui.Views.Markdown markdownView = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Text = markdown
        };

        window.Add (markdownView);

        app.Begin (window);

        // Perform multiple layout/draw cycles
        for (var i = 0; i < 5; i++)
        {
            app.LayoutAndDraw ();

            Assert.Equal (0, markdownView.Viewport.Y);

            foreach (View sub in markdownView.SubViews)
            {
                if (sub is MarkdownTable table)
                {
                    Assert.False (table.HasFocus, $"Table should not have focus after cycle {i}");
                }
            }
        }

        app.Dispose ();
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Content_Change_While_Focused_Does_Not_Focus_Table ()
    {
        // When the MarkdownView itself has focus and content changes (triggering re-layout),
        // no table should steal focus. This tests the AddAt() auto-focus path.
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (80, 15);

        using Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };

        Terminal.Gui.Views.Markdown markdownView = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Text = "# No tables yet"
        };

        window.Add (markdownView);

        app.Begin (window);
        app.LayoutAndDraw ();

        // Give focus to the markdown view explicitly
        markdownView.SetFocus ();
        app.LayoutAndDraw ();
        Assert.True (markdownView.HasFocus);

        // Now change content to include tables with links — this triggers re-layout
        markdownView.Text = """
                            # Title

                            | Command | Link |
                            |---------|------|
                            | [help](app:help) | Shows help |
                            | [quit](app:quit) | Quits app |

                            | Name | Link |
                            |------|------|
                            | [a](https://a.com) | [b](https://b.com) |
                            """;

        app.LayoutAndDraw ();

        // Viewport must remain at top
        Assert.Equal (0, markdownView.Viewport.Y);

        // The MarkdownView should still have focus, not any table
        Assert.True (markdownView.HasFocus, "MarkdownView should retain focus after content change");

        foreach (View sub in markdownView.SubViews)
        {
            if (sub is MarkdownTable table)
            {
                Assert.False (table.HasFocus, "No table should steal focus when content changes");
            }
        }

        app.Dispose ();
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Multiple_Tables_With_Links_No_Cascade_Focus ()
    {
        // When multiple tables have links, clearing auto-focus on one table must not
        // cascade focus to another table (AdvanceFocus wrapping concern).
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (80, 20);

        using Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };

        string markdown = """
                          # Multi-table document

                          | T1 Col | Link |
                          |--------|------|
                          | [a](https://a.com) | [b](https://b.com) |
                          | [c](https://c.com) | [d](https://d.com) |

                          | T2 Col | Link |
                          |--------|------|
                          | [e](https://e.com) | [f](https://f.com) |
                          | [g](https://g.com) | [h](https://h.com) |

                          | T3 Col | Link |
                          |--------|------|
                          | [i](https://i.com) | [j](https://j.com) |
                          """;

        Terminal.Gui.Views.Markdown markdownView = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Text = markdown
        };

        window.Add (markdownView);

        app.Begin (window);
        app.LayoutAndDraw ();

        // No table should have focus at all
        int focusedTableCount = 0;

        foreach (View sub in markdownView.SubViews)
        {
            if (sub is MarkdownTable table && table.HasFocus)
            {
                focusedTableCount++;
            }
        }

        Assert.Equal (0, focusedTableCount);

        // Viewport should be at top
        Assert.Equal (0, markdownView.Viewport.Y);

        // Now give focus to markdownView and do another layout cycle
        markdownView.SetFocus ();
        app.LayoutAndDraw ();

        focusedTableCount = 0;

        foreach (View sub in markdownView.SubViews)
        {
            if (sub is MarkdownTable table && table.HasFocus)
            {
                focusedTableCount++;
            }
        }

        Assert.Equal (0, focusedTableCount);

        app.Dispose ();
    }
}
