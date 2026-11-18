#nullable enable

using global::Spectre.Console;
using global::Spectre.Console.Rendering;
using Terminal.Gui.Interop.Spectre;
using SpectreColor = global::Spectre.Console.Color;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Spectre", "Demonstrates Spectre.Console integration.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Text and Formatting")]
public sealed class SpectreViewScenario : Scenario
{
    public override void Main ()
    {

        using IApplication app = Application.Create ();
        app.Init ();

        using Window appWindow = new ();
        appWindow.Title = GetQuitKeyAndName ();
        appWindow.BorderStyle = LineStyle.None;

        string [] labels = ["_Table", "_Panel", "_Rule", "_Tree", "_BarChart", "_Calendar", "_Figlet", "_Markup"];
        OptionSelector selector = new ()
        {
            X = 0,
            Y = 0,
            Width = 36,
            Height = Dim.Auto (),
            BorderStyle = LineStyle.Rounded,
            Labels = labels,
            Value = 0,
            Title = "_Spectre Widget"
        };

        TextField userInput = new ()
        {
            X = Pos.Right (selector) + 1,
            Y = 0,
            Width = 22,
            Height = 1,
            Text = "Alice"
        };

        Label userLabel = new ()
        {
            X = Pos.Left (userInput),
            Y = Pos.Bottom (userInput),
            Text = "_Name for sample data:"
        };

        CheckBox autoSizeCheckBox = new ()
        {
            X = Pos.Left (userInput),
            Y = Pos.Bottom (userLabel),
            Text = "_AutoSize content",
            Value = CheckState.Checked
        };

        Button refreshButton = new ()
        {
            X = Pos.Left (userInput),
            Y = Pos.Bottom (autoSizeCheckBox) + 1,
            Text = "_Refresh"
        };

        FrameView previewFrame = new ()
        {
            X = 0,
            Y = Pos.Bottom (selector) + 1,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Title = "Preview",
            BorderStyle = LineStyle.Rounded
        };

        SpectreView spectreView = new ()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            AutoSize = true,
            Renderable = CreateRenderable (labels [0], userInput.Text),
            ViewportSettings = ViewportSettingsFlags.HasVerticalScrollBar | ViewportSettingsFlags.HasHorizontalScrollBar
        };

        previewFrame.Add (spectreView);
        appWindow.Add (selector, userInput, userLabel, autoSizeCheckBox, refreshButton, previewFrame);

        selector.ValueChanged += (_, args) =>
                                {
                                    if (args.NewValue is null)
                                    {
                                        return;
                                    }

                                    string selectedLabel = labels [(int)args.NewValue];
                                    spectreView.Renderable = CreateRenderable (selectedLabel, userInput.Text);
                                };

        refreshButton.Accepted += (_, _) =>
                                  {
                                      string selectedLabel = labels [(int)(selector.Value ?? 0)];
                                      spectreView.Renderable = CreateRenderable (selectedLabel, userInput.Text);
                                  };

        autoSizeCheckBox.ValueChanged += (_, args) =>
                                        {
                                            spectreView.AutoSize = args.NewValue == CheckState.Checked;
                                        };

        app.Run (appWindow);
    }

    private static IRenderable CreateRenderable (string selectedLabel, string userName)
    {
        string safeName = string.IsNullOrWhiteSpace (userName) ? "User" : userName;

        switch (selectedLabel)
        {
            case "_Panel":
            {
                return new Panel ($"Welcome, {safeName}!\nThis is Spectre rendered in a Terminal.Gui view.")
                {
                    Header = new PanelHeader ("Spectre Panel")
                };
            }

            case "_Rule":
            {
                return new Rule ($"Spectre Rule for {safeName}");
            }

            case "_Tree":
            {
                Tree tree = new ("Project");
                global::Spectre.Console.TreeNode src = tree.AddNode ("src");
                src.AddNode ("App.cs");
                src.AddNode ("SpectreView.cs");
                tree.AddNode ("README.md");

                return tree;
            }

            case "_BarChart":
            {
                BarChart chart = new ();
                chart.Width (60);
                chart.AddItem (safeName, 81, new SpectreColor (100, 149, 237));
                chart.AddItem ("Average", 73, new SpectreColor (0, 250, 154));
                chart.AddItem ("Target", 90, new SpectreColor (255, 165, 0));

                return chart;
            }

            case "_Calendar":
            {
                DateTime now = DateTime.Now;
                Calendar calendar = new (now.Year, now.Month);
                calendar.HighlightStyle (new Style (SpectreColor.Black, SpectreColor.Yellow));

                return calendar;
            }

            case "_Figlet":
            {
                return new FigletText (safeName).Centered ();
            }

            case "_Markup":
            {
                return new Markup ($"[bold aqua]{safeName}[/] uses [yellow]SpectreView[/] inside [green]Terminal.Gui[/].");
            }

            default:
            {
                Table table = new ();
                table.Border = TableBorder.Rounded;
                table.AddColumn ("Name");
                table.AddColumn ("Score");
                table.AddRow (safeName, "81");
                table.AddRow ("Average", "73");

                return table;
            }
        }
    }
}
