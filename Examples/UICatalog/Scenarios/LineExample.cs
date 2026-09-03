namespace UICatalog.Scenarios;

[ScenarioMetadata ("Line", "Demonstrates the Line view with LineCanvas integration.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Drawing")]
[ScenarioCategory ("Adornments")]
public class LineExample : Scenario
{
    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();

        using Window window = new ()
        {
            Title = GetQuitKeyAndName ()
        };

        // Section 1: Basic Lines
        var basicLabel = new Label
        {
            X = 0,
            Y = 0,
            Text = "Basic Lines:"
        };
        window.Add (basicLabel);

        // Horizontal line
        var hLine = new Line
        {
            X = 0,
            Y = 1,
            Width = 30
        };
        window.Add (hLine);

        // Vertical line
        var vLine = new Line
        {
            X = 32,
            Y = 0,
            Height = 10,
            Orientation = Orientation.Vertical
        };
        window.Add (vLine);

        // Section 2: Different Line Styles
        var stylesLabel = new Label
        {
            X = 0,
            Y = 3,
            Text = "Line Styles:"
        };
        window.Add (stylesLabel);

        (LineStyle, string) [] styles = new []
        {
            (LineStyle.Single, "Single"),
            (LineStyle.Double, "Double"),
            (LineStyle.Heavy, "Heavy"),
            (LineStyle.Rounded, "Rounded"),
            (LineStyle.Dashed, "Dashed"),
            (LineStyle.Dotted, "Dotted")
        };

        var yPos = 4;

        foreach ((LineStyle style, string name) in styles)
        {
            window.Add (new Label { X = 0, Y = yPos, Width = 15, Text = name + ":" });
            window.Add (new Line { X = 16, Y = yPos, Width = 14, Style = style });
            yPos++;
        }

        // Section 3: Line Intersections
        var intersectionLabel = new Label
        {
            X = 35,
            Y = 3,
            Text = "Line Intersections:"
        };
        window.Add (intersectionLabel);

        // Create a grid of intersecting lines
        var gridX = 35;
        var gridY = 5;

        // Horizontal lines in the grid
        for (var i = 0; i < 5; i++)
        {
            window.Add (
                     new Line
                     {
                         X = gridX,
                         Y = gridY + i * 2,
                         Width = 21,
                         Style = LineStyle.Single
                     });
        }

        // Vertical lines in the grid
        for (var i = 0; i < 5; i++)
        {
            window.Add (
                     new Line
                     {
                         X = gridX + i * 5,
                         Y = gridY,
                         Height = 9,
                         Orientation = Orientation.Vertical,
                         Style = LineStyle.Single
                     });
        }

        // Section 4: Mixed Styles (shows how LineCanvas handles different line styles)
        var mixedLabel = new Label
        {
            X = 60,
            Y = 3,
            Text = "Mixed Style Intersections:"
        };
        window.Add (mixedLabel);

        // Double horizontal
        window.Add (
                 new Line
                 {
                     X = 60,
                     Y = 5,
                     Width = 20,
                     Style = LineStyle.Double
                 });

        // Single vertical through double horizontal
        window.Add (
                 new Line
                 {
                     X = 70,
                     Y = 4,
                     Height = 3,
                     Orientation = Orientation.Vertical,
                     Style = LineStyle.Single
                 });

        // Heavy horizontal
        window.Add (
                 new Line
                 {
                     X = 60,
                     Y = 8,
                     Width = 20,
                     Style = LineStyle.Heavy
                 });

        // Single vertical through heavy horizontal
        window.Add (
                 new Line
                 {
                     X = 70,
                     Y = 7,
                     Height = 3,
                     Orientation = Orientation.Vertical,
                     Style = LineStyle.Single
                 });

        // Section 5: Box Example (showing borders and lines working together)
        var boxLabel = new Label
        {
            X = 0,
            Y = 12,
            Text = "Lines with Borders:"
        };
        window.Add (boxLabel);

        var framedView = new FrameView
        {
            Title = "Frame",
            X = 0,
            Y = 13,
            Width = 30,
            Height = 8,
            BorderStyle = LineStyle.Single
        };

        // Add a cross inside the frame
        framedView.Add (
                        new Line
                        {
                            X = 0,
                            Y = 3,
                            Width = Dim.Fill (),
                            Style = LineStyle.Single
                        });

        framedView.Add (
                        new Line
                        {
                            X = 14,
                            Y = 0,
                            Height = Dim.Fill (),
                            Orientation = Orientation.Vertical,
                            Style = LineStyle.Single
                        });

        window.Add (framedView);

        // Add help text
        var helpLabel = new Label
        {
            X = Pos.Center (),
            Y = Pos.AnchorEnd (1),
            Text = "Line integrates with LineCanvas for automatic intersection handling"
        };
        window.Add (helpLabel);

        app.Run (window);
    }
}
