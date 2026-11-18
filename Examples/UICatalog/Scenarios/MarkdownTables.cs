using Markdig;
using Markdig.Extensions.Tables;
using Markdown = Terminal.Gui.Views.Markdown;

namespace UICatalog.Scenarios;

/// <summary>Demonstrates proportional columns, grid spans, rich cells, and standalone table data.</summary>
[ScenarioMetadata ("Markdown Tables", "Resize Markdown tables, follow cell links, and compare inferred widths.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Text and Formatting")]
public class MarkdownTables : Scenario
{
    private const string _sample = """
                                  ## Preferred columns
                                  | Short | Description |
                                  | ----- | -------------------- |
                                  | A | A longer description wraps while the source requests a 1:4 content ratio. |
                                  | 界 | Combining é and emoji 👩‍💻 keep their display widths. |

                                  ## Spanning cell and explicit header
                                  +-------+-----------------------+
                                  | Name  | Notes                 |
                                  +=======+=======================+
                                  | [Follow this spanning link](https://example.com/tables) |
                                  +-------+-----------------------+

                                  ## Headerless grid
                                  +-------+-----------------------+
                                  | A     | Paragraph one.        |
                                  |       |                       |
                                  |       | Paragraph two.        |
                                  +-------+-----------------------+
                                  | B     | - List item           |
                                  |       | - Another item        |
                                  +-------+-----------------------+
                                  | C     |     int value = 42;   |
                                  +-------+-----------------------+
                                  """;

    /// <inheritdoc/>
    public override void Main ()
    {
        using IApplication app = Application.Create ().Init ();
        using Runnable window = new () { Title = "Markdown Tables (Esc to quit)", Width = Dim.Fill (), Height = Dim.Fill () };
        NumericUpDown<int> width = new () { Title = "Content width", Value = 38, Increment = 1, X = 1, Y = 0, Width = 9 };
        CheckBox infer = new () { Text = "_Infer widths", Value = CheckState.Checked, X = Pos.Right (width) + 2 };
        Label activated = new () { Text = "Tab to links; Enter activates. F2/F3 resize.", X = 1, Y = 2, Width = Dim.Fill (1) };
        Markdown preview = new ()
        {
            Text = _sample,
            SyntaxHighlighter = new TextMateSyntaxHighlighter (),
            UseThemeBackground = true,
            X = 1,
            Y = 3,
            Width = Dim.Fill (1),
            Height = Dim.Fill (5)
        };
        MarkdownTable standalone = new ()
        {
            X = 1,
            Y = Pos.AnchorEnd (4),
            Width = Dim.Fill (1),
            TableData = TableData.CreateGrid ([new (preferredWidthPercentage: 20), new (preferredWidthPercentage: 80)],
                                             [new (false, [new (0, "Standalone"), new (1, "[Link](https://example.com/standalone) in structured data")])])
        };
        preview.LinkClicked += (_, e) =>
                               {
                                   activated.Text = $"Activated: {e.Url}";
                                   e.Handled = true;
                               };
        standalone.LinkClicked += (_, e) =>
                                  {
                                      activated.Text = $"Activated: {e.Url}";
                                      e.Handled = true;
                                  };
        width.ValueChanged += (_, _) => ApplyWidth ();
        infer.ValueChanged += (_, _) => ApplyPipeline ();
        StatusBar status = new ();
        status.Add (new Shortcut { Key = Key.F2, Title = "Narrow (24)", Action = () => width.Value = 24 },
                    new Shortcut { Key = Key.F3, Title = "Wide (80)", Action = () => width.Value = 80 },
                    new Shortcut { Key = Key.Esc, Title = "Quit", Action = app.RequestStop });
        window.Add (width, infer, activated, preview, standalone, status);
        ApplyPipeline ();
        ApplyWidth ();
        app.Run (window);

        return;

        void ApplyWidth () => preview.SetContentSize (new Size (Math.Clamp (width.Value, 4, 200), 0));

        void ApplyPipeline ()
        {
            preview.MarkdownPipeline = new MarkdownPipelineBuilder ()
                                       .UsePipeTables (new PipeTableOptions { InferColumnWidthsFromSeparator = infer.Value == CheckState.Checked })
                                       .UseAdvancedExtensions ().Build ();
        }
    }
}
