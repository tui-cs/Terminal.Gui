using TgMarkdown = Terminal.Gui.Views.Markdown;

namespace ViewsTests.Markdown;

// CoPilot - GPT-6
public class MarkdownAlertCopyTests
{
    [Theory]
    [InlineData ("WARNING")]
    [InlineData ("CUSTOM")]
    [InlineData ("Note")]
    [InlineData ("custom")]
    public void Partial_Selection_Preserves_Alert_Marker (string kind)
    {
        using TgMarkdown view = new ()
        {
            Text = $"before\n\n> [!{kind}]\n> content\n\nafter",
            Width = 60,
            Height = 20
        };
        view.Layout ();

        Assert.Equal ($"> [!{kind}]\n> content", SelectRows (view, 2, 3));
    }

    [Fact]
    public void Wrapped_Alert_Header_Copies_Marker_Once ()
    {
        using TgMarkdown view = new ()
        {
            Text = "before\n\n> [!IMPORTANT]\n> x",
            Width = 8,
            Height = 20
        };
        view.Layout ();

        Assert.Equal ("> [!IMPORTANT]\n> x", SelectRows (view, 2, view.LineCount - 1));
    }

    [Theory]
    [InlineData (0, 2, "> IMPORT")]
    [InlineData (1, 3, " IMPORT\n> ANT")]
    public void Partially_Selected_Wrapped_Header_Keeps_Display_Text (int firstColumn, int lastRow, string expected)
    {
        using TgMarkdown view = new ()
        {
            Text = "before\n\n> [!IMPORTANT]\n> x",
            Width = 8,
            Height = 20
        };
        view.Layout ();

        Assert.Equal (expected, SelectRows (view, 2, lastRow, firstColumn));
    }

    private static string SelectRows (TgMarkdown view, int first, int last, int firstColumn = 0)
    {
        view.NewMouseEvent (new Mouse
        {
            Position = new Point (firstColumn, first),
            Flags = MouseFlags.LeftButtonPressed
        });
        view.NewMouseEvent (new Mouse
        {
            Position = new Point (59, last),
            Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport
        });

        return Assert.IsType<string> (view.SelectedText);
    }
}
