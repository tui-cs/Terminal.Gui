using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Xunit;

namespace ViewsTests.Markdown;

public class TableHeightInvestigation
{
    // Copilot

    [Theory]
    [InlineData (80)]
    [InlineData (100)]
    [InlineData (120)]
    [InlineData (150)]
    public void Table_RenderedHeight_Matches_Frame_Height_After_Layout (int screenWidth)
    {
        // Copilot
        // Regression test: RenderedHeight must match Frame.Height after layout.
        // Previously, Add(tableView) triggered EndInit → Layout → Recalculate at stale width,
        // corrupting RenderedHeight and causing extra blank lines after the table.
        string markdown = @"## Available Clets

| Alias | Description | Options |
|-------|-------------|---------|
| select | Presents a list of options and returns the text of the selected item. | --options, args ... |
| text | Prompts for free-form text input and returns the entered string. | |
| multiline-text, mt | Prompts for multi-line text input and returns the entered string. | |
| int | Prompts for an integer value using a numeric spinner. | --step |
| decimal | Prompts for a decimal value using a numeric spinner. | --step |
| confirm | Prompts for a yes/no confirmation and returns a boolean | --prompt |
| date | Prompts for a date and returns an ISO-8601 date string (YYYY-MM-DD). | |
| time | Prompts for a time and returns an ISO-8601 time string (HH:MM:SS). | |
| duration | Prompts for a duration and returns an ISO-8601 duration string (e.g. PT1H30M). | |
| color | Prompts for a color and returns a hex string (@rrggbb). | |
| multi-select | Presents a list of options with checkboxes and returns the selected texts. | --options, args ... |
| attribute-picker, attribute | Prompts for text attributes (Foreground, Background, style) and returns a JSON object. | |
| pick-file, file | Opens a file picker dialog and returns the selected file path(s). | --multi, --root, --filter |
| pick-directory, dir | Opens a directory picker dialog and returns the selected directory path. | --root |
| linear-range, range | Presents a LinearRange (single, multi, or bounded range) over a list of labelled options and returns the selection. | --mode, --options, --orientation, --range-kind, --allow-empty, --hide-legends, args ... |
| md, markdown | Browse and render Markdown files with link navigation and syntax highlighting. | --theme, --cat, --no-browse, args |
| help | Shows help for clet commands. | args ... |

Click for details: select, text";

        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (screenWidth, 50);

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };

        Terminal.Gui.Views.Markdown mv = new () { Text = markdown, Width = Dim.Fill (), Height = Dim.Fill () };
        window.Add (mv);

        app.Begin (window);
        app.LayoutAndDraw ();

        List<MarkdownTable> tableViews = mv.SubViews.OfType<MarkdownTable> ().ToList ();
        Assert.Single (tableViews);

        MarkdownTable table = tableViews [0];

        // The table's RenderedHeight must equal its Frame.Height after layout
        Assert.Equal (table.RenderedHeight, table.Frame.Height);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Table_LineCount_Consistent_After_Resize ()
    {
        // Copilot
        // Regression test: resizing should not produce extra blank lines after a table.
        string markdown = @"## Test

| Alias | Description | Options |
|-------|-------------|---------|
| select | Presents a list of options and returns the text of the selected item. | --options, args ... |
| multiline-text, mt | Prompts for multi-line text input and returns the entered string. | |
| linear-range, range | Presents a LinearRange (single, multi, or bounded range) over a list of labelled options and returns the selection. | --mode, --options, --orientation, --range-kind, --allow-empty, --hide-legends, args ... |

End";

        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (100, 30);

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };

        Terminal.Gui.Views.Markdown mv = new () { Text = markdown, Width = Dim.Fill (), Height = Dim.Fill () };
        window.Add (mv);

        app.Begin (window);
        app.LayoutAndDraw ();

        int initialLineCount = mv.LineCount;

        // Resize by 1 column - should not dramatically change line count
        app.Driver.SetScreenSize (101, 30);
        window.SetNeedsLayout ();
        app.LayoutAndDraw ();

        int resizedLineCount = mv.LineCount;

        // The line counts should be equal or differ by at most 1 (due to wrapping boundary)
        // Previously the initial count could be many lines too large.
        Assert.InRange (Math.Abs (initialLineCount - resizedLineCount), 0, 1);

        window.Dispose ();
        app.Dispose ();
    }
}
