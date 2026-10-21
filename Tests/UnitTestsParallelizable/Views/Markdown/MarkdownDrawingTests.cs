namespace ViewsTests.Markdown;

public class MarkdownDrawingTests
{
    private const string CONTENT = """
                                   Intro

                                   ```markdown
                                   # Nested heading
                                   | Nested | Table |
                                   ```

                                   | Header | Value |
                                   | ------ | ----- |
                                   | Cell   | Data  |

                                   ---

                                   End
                                   """;

    // CoPilot - GPT-6.1
    [Theory]
    [InlineData (0, false)]
    [InlineData (0, true)]
    [InlineData (1, false)]
    [InlineData (1, true)]
    public void ScrollSlider_Hover_Preserves_Content (int scrollOffset, bool selectAll)
    {
        using IApplication app = Application.Create ().Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (40, 16);
        using Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };
        Terminal.Gui.Views.Markdown markdown = new ()
        {
            Text = CONTENT + string.Concat (Enumerable.Repeat ("\n\nMore content", 20)),
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };
        window.Add (markdown);
        app.Begin (window);
        app.LayoutAndDraw ();
        markdown.Viewport = markdown.Viewport with { Y = scrollOffset };

        if (selectAll)
        {
            markdown.SelectAll ();
        }

        app.LayoutAndDraw ();
        Assert.Contains ("Nested heading", app.Driver.ToString ());
        Assert.Contains ("Cell", app.Driver.ToString ());
        List<Cell> expected = SnapshotContent (markdown);
        ScrollSlider slider = markdown.VerticalScrollBar.Slider;
        Assert.True (slider.Visible);

        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = slider.ViewportToScreen (Point.Empty), Flags = MouseFlags.PositionReport });
        Assert.True (slider.MouseState.HasFlag (MouseState.In));
        app.LayoutAndDraw ();
        Assert.Equal (expected, SnapshotContent (markdown));

        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = markdown.ViewportToScreen (Point.Empty), Flags = MouseFlags.PositionReport });
        Assert.False (slider.MouseState.HasFlag (MouseState.In));
        app.LayoutAndDraw ();
        Assert.Equal (expected, SnapshotContent (markdown));
    }

    // CoPilot - GPT-6.1
    [Fact]
    public void Partial_Redraw_Preserves_SubViews_Outside_Dirty_Region ()
    {
        using IApplication app = Application.Create ().Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (40, 16);
        using Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };
        Terminal.Gui.Views.Markdown markdown = new () { Text = CONTENT, Width = Dim.Fill (), Height = Dim.Fill () };
        window.Add (markdown);
        app.Begin (window);
        app.LayoutAndDraw ();
        Assert.Contains ("Nested heading", app.Driver.ToString ());
        Assert.Contains ("Cell", app.Driver.ToString ());
        List<Cell> expected = SnapshotContent (markdown);

        markdown.SetNeedsDraw (new Rectangle (0, 0, 1, 1));
        app.LayoutAndDraw ();

        Assert.Equal (expected, SnapshotContent (markdown));
    }

    private static List<Cell> SnapshotContent (Terminal.Gui.Views.Markdown markdown)
    {
        Cell [,] contents = markdown.Driver!.Contents!;
        Rectangle viewport = markdown.ViewportToScreen (new Rectangle (Point.Empty, markdown.Viewport.Size));
        List<Cell> snapshot = [];

        for (int y = viewport.Top; y < viewport.Bottom; y++)
        {
            for (int x = viewport.Left; x < viewport.Right; x++)
            {
                snapshot.Add (contents [y, x] with { IsDirty = false });
            }
        }

        return snapshot;
    }
}
