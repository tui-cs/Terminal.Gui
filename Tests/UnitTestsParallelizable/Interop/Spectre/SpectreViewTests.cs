// Copilot

using System.Text;
using JetBrains.Annotations;
using global::Spectre.Console;
using global::Spectre.Console.Rendering;
using Terminal.Gui.Interop.Spectre;
using Terminal.Gui.Drawing;
using Terminal.Gui.Views;
using SpectreColor = Spectre.Console.Color;
using TgAttribute = Terminal.Gui.Drawing.Attribute;

namespace InteropTests.Spectre;

[TestSubject (typeof (SpectreView))]
public class SpectreViewTests
{
    [Fact]
    public void SpectreMarkupBridge_ToAttribute_Converts_Color_And_Decoration ()
    {
        Style style = new (SpectreColor.Red, SpectreColor.Blue, Decoration.Bold | Decoration.Underline | Decoration.Strikethrough);

        TgAttribute attribute = style.ToAttribute ();

        Assert.Equal (255, attribute.Foreground.R);
        Assert.Equal (0, attribute.Foreground.G);
        Assert.Equal (0, attribute.Foreground.B);
        Assert.Equal (0, attribute.Background.R);
        Assert.Equal (0, attribute.Background.G);
        Assert.Equal (255, attribute.Background.B);
        Assert.True ((attribute.Style & TextStyle.Bold) != 0);
        Assert.True ((attribute.Style & TextStyle.Underline) != 0);
        Assert.True ((attribute.Style & TextStyle.Strikethrough) != 0);
    }

    [Fact]
    public void SpectreMarkupBridge_ToSpectreStyle_Converts_Color_And_Decoration ()
    {
        TgAttribute attribute = new (
            new Terminal.Gui.Drawing.Color (0, 255, 0),
            new Terminal.Gui.Drawing.Color (128, 0, 128),
            TextStyle.Italic | TextStyle.Underline);

        Style spectreStyle = attribute.ToSpectreStyle ();

        Assert.Equal (0, spectreStyle.Foreground.R);
        Assert.Equal (255, spectreStyle.Foreground.G);
        Assert.Equal (0, spectreStyle.Foreground.B);
        Assert.Equal (128, spectreStyle.Background.R);
        Assert.Equal (0, spectreStyle.Background.G);
        Assert.Equal (128, spectreStyle.Background.B);
        Assert.True ((spectreStyle.Decoration & Decoration.Italic) != 0);
        Assert.True ((spectreStyle.Decoration & Decoration.Underline) != 0);
    }

    [Fact]
    public void SpectreMarkupBridge_RoundTrip_Preserves_Style ()
    {
        TgAttribute original = new (
            new Terminal.Gui.Drawing.Color (100, 150, 200),
            new Terminal.Gui.Drawing.Color (50, 60, 70),
            TextStyle.Bold | TextStyle.Faint | TextStyle.Strikethrough);

        Style spectreStyle = original.ToSpectreStyle ();
        TgAttribute roundTripped = spectreStyle.ToAttribute ();

        Assert.Equal (original.Foreground.R, roundTripped.Foreground.R);
        Assert.Equal (original.Foreground.G, roundTripped.Foreground.G);
        Assert.Equal (original.Foreground.B, roundTripped.Foreground.B);
        Assert.Equal (original.Background.R, roundTripped.Background.R);
        Assert.Equal (original.Background.G, roundTripped.Background.G);
        Assert.Equal (original.Background.B, roundTripped.Background.B);
        Assert.Equal (original.Style, roundTripped.Style);
    }

    [Fact]
    public void SpectreMarkupBridge_Default_Colors_Map_To_None ()
    {
        Style style = new (SpectreColor.Default, SpectreColor.Default);

        TgAttribute attribute = style.ToAttribute ();

        Assert.Equal (Terminal.Gui.Drawing.Color.None, attribute.Foreground);
        Assert.Equal (Terminal.Gui.Drawing.Color.None, attribute.Background);
    }

    [Fact]
    public void SpectreMarkupBridge_None_Colors_Map_To_Default ()
    {
        TgAttribute attribute = new (Terminal.Gui.Drawing.Color.None, Terminal.Gui.Drawing.Color.None);

        Style spectreStyle = attribute.ToSpectreStyle ();

        Assert.Equal (SpectreColor.Default, spectreStyle.Foreground);
        Assert.Equal (SpectreColor.Default, spectreStyle.Background);
    }

    [Fact]
    public void SpectreView_Renders_Markup_IRenderable ()
    {
        Markup markup = new ("[bold red]Hello[/] [green]World[/]");

        string output = RenderToText (markup, 40, 3);

        Assert.Contains ("Hello", output);
        Assert.Contains ("World", output);
    }

    [Fact]
    public void SpectreView_Renders_Table_Panel_Rule_Tree_BarChart_And_FigletText ()
    {
        Table table = new ();
        table.AddColumn ("Name");
        table.AddColumn ("Score");
        table.AddRow ("Alice", "100");

        string tableOutput = RenderToText (table, 80, 8);
        Assert.Contains ("Alice", tableOutput);

        Panel panel = new ("Panel Body");
        string panelOutput = RenderToText (panel, 80, 6);
        Assert.Contains ("Panel Body", panelOutput);

        Rule rule = new ("Rule Title");
        string ruleOutput = RenderToText (rule, 80, 3);
        Assert.Contains ("Rule Title", ruleOutput);

        Tree tree = new ("Root");
        tree.AddNode ("Branch");
        string treeOutput = RenderToText (tree, 80, 6);
        Assert.Contains ("Root", treeOutput);
        Assert.Contains ("Branch", treeOutput);

        BarChart barChart = new ();
        barChart.AddItem ("A", 10, SpectreColor.Green);
        string chartOutput = RenderToText (barChart, 80, 6);
        Assert.Contains ("A", chartOutput);

        FigletText figlet = new ("Hi");
        string figletOutput = RenderToText (figlet, 80, 8);
        Assert.Contains ("_", figletOutput);
    }

    [Fact]
    public void SpectreView_Renderable_Change_Triggers_Rerender ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (60, 6);

        SpectreView view = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Renderable = new Rule ("Before")
        };

        using Runnable root = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.None
        };
        root.Add (view);
        app.Begin (root);
        app.LayoutAndDraw ();

        string before = GetDriverText (app.Driver.Contents!);
        Assert.Contains ("Before", before);

        view.Renderable = new Rule ("After");
        app.LayoutAndDraw ();

        string after = GetDriverText (app.Driver.Contents!);
        Assert.Contains ("After", after);
    }

    [Fact]
    public void SpectreView_AutoSize_Reports_ContentSize_For_DimAuto ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (80, 25);

        Table table = new ();
        table.AddColumn ("Name");
        table.AddColumn ("Value");
        table.AddRow ("Alice", "100");
        table.AddRow ("Bob", "95");

        SpectreView view = new ()
        {
            Width = 30,
            Height = Dim.Auto (DimAutoStyle.Content),
            Renderable = table
        };

        using Runnable root = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.None
        };
        root.Add (view);

        app.Begin (root);
        app.LayoutAndDraw ();

        Assert.True (view.GetContentSize ().Height > 0);
        Assert.True (view.Frame.Height > 0);
    }

    [Fact]
    public void SpectreView_Integrates_With_Window_And_Interactive_Views ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (80, 10);

        Button button = new ()
        {
            Text = "Go",
            X = 0,
            Y = 0
        };
        SpectreView spectre = new ()
        {
            Renderable = new Rule ("Decor"),
            X = 0,
            Y = 1,
            Width = Dim.Fill (),
            Height = 3
        };

        Window window = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.None
        };
        window.Add (button, spectre);

        app.Begin (window);
        app.LayoutAndDraw ();

        string output = GetDriverText (app.Driver.Contents!);
        Assert.Contains ("Go", output);
        Assert.Contains ("Decor", output);
    }

    [Fact]
    public void SpectreView_Skips_ControlCode_Segments ()
    {
        // A custom IRenderable that emits a control-code segment between text segments.
        // If control codes are not skipped, the escape text will appear between Hello and World.
        ControlCodeRenderable renderable = new ();

        string output = RenderToText (renderable, 40, 3);

        // The control code text "[?25l" should NOT appear as visible text between Hello and World
        Assert.DoesNotContain ("[?25", output);
        // The two text segments should be adjacent (no garbage between them)
        Assert.Contains ("HelloWorld", output);
    }

    [Fact]
    public void SpectreView_ZeroWidth_Characters_Do_Not_Misalign ()
    {
        // A renderable that emits a combining character (zero-width) followed by normal text.
        // The combining mark merges with the preceding character into a single grapheme.
        // "After" should follow immediately without spurious gaps.
        ZeroWidthRenderable renderable = new ();

        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (40, 3);

        SpectreView view = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Renderable = renderable
        };

        using Runnable root = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.None
        };
        root.Add (view);
        app.Begin (root);
        app.LayoutAndDraw ();

        Cell [,] contents = app.Driver.Contents!;
        StringBuilder row0 = new ();

        for (int col = 0; col < contents.GetLength (1); col++)
        {
            row0.Append (contents [0, col].Grapheme);
        }

        string line = row0.ToString ().TrimEnd ();

        // "Base\u0301" renders as "Bas" + "é" (4 display columns), then "After" immediately follows
        Assert.Contains ("After", line);

        // "After" should start at column 4 (width of "Base\u0301" = 4 graphemes, each 1 col)
        // No spurious gap between the combined character and "After"
        Assert.DoesNotContain ("  After", line);
    }

    /// <summary>A test renderable that emits control-code segments between text.</summary>
    private sealed class ControlCodeRenderable : IRenderable
    {
        public Measurement Measure (RenderOptions options, int maxWidth)
        {
            return new Measurement (10, 10);
        }

        public IEnumerable<Segment> Render (RenderOptions options, int maxWidth)
        {
            yield return new Segment ("Hello", Style.Plain);
            yield return Segment.Control ("\x1b[?25l");
            yield return new Segment ("World", Style.Plain);
        }
    }

    /// <summary>A test renderable that emits a combining mark (zero-width) character.</summary>
    private sealed class ZeroWidthRenderable : IRenderable
    {
        public Measurement Measure (RenderOptions options, int maxWidth)
        {
            return new Measurement (10, 10);
        }

        public IEnumerable<Segment> Render (RenderOptions options, int maxWidth)
        {
            // "Base" + combining diacritical acute (U+0301, zero-width) + "After"
            yield return new Segment ("Base\u0301", Style.Plain);
            yield return new Segment ("After", Style.Plain);
        }
    }

    private static string RenderToText (IRenderable renderable, int width, int height)
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (width, height);

        SpectreView view = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Renderable = renderable
        };

        using Runnable root = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.None
        };
        root.Add (view);

        app.Begin (root);
        app.LayoutAndDraw ();

        return GetDriverText (app.Driver.Contents!);
    }

    private static string GetDriverText (Cell [,] contents)
    {
        int rowCount = contents.GetLength (0);
        int colCount = contents.GetLength (1);
        List<string> lines = [];

        for (int row = 0; row < rowCount; row++)
        {
            StringBuilder lineBuilder = new ();

            for (int col = 0; col < colCount; col++)
            {
                lineBuilder.Append (contents [row, col].Grapheme);
            }

            lines.Add (lineBuilder.ToString ());
        }

        return string.Join ("\n", lines);
    }
}
