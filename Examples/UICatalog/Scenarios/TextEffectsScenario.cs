namespace UICatalog.Scenarios;

[ScenarioMetadata ("Text Effects", "Text Effects.")]
[ScenarioCategory ("Colors")]
[ScenarioCategory ("Text and Formatting")]
public class TextEffectsScenario : Scenario
{
    /// <summary>
    ///     Enable or disable looping of the gradient colors.
    /// </summary>
    public static bool _loopingGradient;

    public override void Main ()
    {

        using IApplication app = Application.Create ();
        app.Init ();

        using Window w = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Title = "Text Effects Scenario"
        };

        w.IsModalChanged += (s, e) => { SetupGradientLineCanvas (w, w.Frame.Size); };

        w.ViewportChanged += (s, e) => { SetupGradientLineCanvas (w, e.NewViewport.Size); };

        w.SetScheme (
                     new ()
                     {
                         Normal = new (ColorName16.White, ColorName16.Black),
                         Focus = new (ColorName16.Black, ColorName16.White),
                         HotNormal = new (ColorName16.White, ColorName16.Black),
                         HotFocus = new (ColorName16.White, ColorName16.Black),
                         Disabled = new (ColorName16.Gray, ColorName16.Black)
                     });

        var gradientsView = new GradientsView
        {
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        var cbLooping = new CheckBox
        {
            Text = "Looping",
            Y = Pos.AnchorEnd (1)
        };

        cbLooping.ValueChanging += (_, e) =>
                                          {
                                              _loopingGradient = e.NewValue == CheckState.Checked;
                                              SetupGradientLineCanvas (w, w.Frame.Size);
                                          };

        gradientsView.Add (cbLooping);

        w.Add (gradientsView);

        app.Run (w);
    }

    private static void SetupGradientLineCanvas (View w, Size size)
    {
        GetAppealingGradientColors (out List<Color> stops, out List<int> steps);

        var g = new Gradient (stops, steps, _loopingGradient);

        var fore = new GradientFill (
                                     new (0, 0, size.Width, size.Height),
                                     g,
                                     GradientDirection.Diagonal);
        var back = new SolidFill (new (ColorName16.Black));

        w.LineCanvas.Fill = new (
                                 fore,
                                 back);
    }

    public static void GetAppealingGradientColors (out List<Color> stops, out List<int> steps)
    {
        // Define the colors of the gradient stops with more appealing colors
        stops =
        [
            new (0, 128, 255), // Bright Blue
            new (0, 255, 128), // Bright Green
            new (255, 255), // Bright Yellow
            new (255, 128), // Bright Orange
            new (255, 0, 128)
        ];

        // Define the number of steps between each color for smoother transitions
        // If we pass only a single value then it will assume equal steps between all pairs
        steps = [15];
    }
}

internal class GradientsView : View
{
    private const int GRADIENT_WIDTH = 30;
    private const int GRADIENT_HEIGHT = 15;
    private const int LABEL_HEIGHT = 1;
    private const int GRADIENT_WITH_LABEL_HEIGHT = GRADIENT_HEIGHT + LABEL_HEIGHT + 1; // +1 for spacing

    protected override bool OnDrawingContent (DrawContext context)
    {
        DrawTopLineGradient (Viewport);

        var x = 2;
        var y = 3;

        List<(string Label, GradientDirection Direction)> gradients = new ()
        {
            ("Horizontal", GradientDirection.Horizontal),
            ("Vertical", GradientDirection.Vertical),
            ("Radial", GradientDirection.Radial),
            ("Diagonal", GradientDirection.Diagonal)
        };

        foreach ((string label, GradientDirection direction) in gradients)
        {
            if (x + GRADIENT_WIDTH > Viewport.Width)
            {
                x = 2; // Reset to left margin
                y += GRADIENT_WITH_LABEL_HEIGHT; // Move down to next row
            }

            DrawLabeledGradientArea (label, direction, x, y);
            x += GRADIENT_WIDTH + 2; // Move right for next gradient, +2 for spacing
        }

        return true;
    }

    private void DrawLabeledGradientArea (string label, GradientDirection direction, int xOffset, int yOffset)
    {
        DrawGradientArea (direction, xOffset, yOffset);
        CenterText (label, xOffset, yOffset + GRADIENT_HEIGHT); // Adjusted for text below the gradient
    }

    private void CenterText (string text, int xOffset, int yOffset)
    {
        if (yOffset + 1 >= Viewport.Height)
        {
            // Not enough space for label
            return;
        }

        int width = text.Length;
        int x = xOffset + (GRADIENT_WIDTH - width) / 2; // Center the text within the gradient area width
        SetAttribute (GetAttributeForRole (VisualRole.Normal));
        Move (x, yOffset + 1);
        AddStr (text);
    }

    private void DrawGradientArea (GradientDirection direction, int xOffset, int yOffset)
    {
        // Define the colors of the gradient stops
        List<Color> stops =
        [
            new (255, 0), // Red
            new (0, 255), // Green
            new (238, 130, 238)
        ];

        // Define the number of steps between each color
        List<int> steps = [10, 10]; // 10 steps between Red -> Green, and Green -> Blue

        // Create the gradient
        var radialGradient = new Gradient (stops, steps, TextEffectsScenario._loopingGradient);

        // Define the size of the rectangle
        int maxRow = GRADIENT_HEIGHT; // Adjusted to keep aspect ratio
        int maxColumn = GRADIENT_WIDTH;

        // Build the coordinate-color mapping for a radial gradient
        Dictionary<Point, Color> gradientMapping = radialGradient.BuildCoordinateColorMapping (maxRow, maxColumn, direction);

        // Print the gradient
        for (var row = 0; row <= maxRow; row++)
        {
            for (var col = 0; col <= maxColumn; col++)
            {
                var coord = new Point (col, row);
                Color color = gradientMapping [coord];

                SetColor (color);

                AddRune (col + xOffset, row + yOffset, new ('█'));
            }
        }
    }

    private void DrawTopLineGradient (Rectangle viewport)
    {
        // Define the colors of the rainbow
        List<Color> stops =
        [
            new (255, 0), // Red
            new (255, 165), // Orange
            new (255, 255), // Yellow
            new (0, 128), // Green
            new (0, 0, 255), // Blue
            new (75, 0, 130), // Indigo
            new (238, 130, 238)
        ];

        // Define the number of steps between each color
        List<int> steps =
        [
            20, // between Red and Orange
            20, // between Orange and Yellow
            20, // between Yellow and Green
            20, // between Green and Blue
            20, // between Blue and Indigo
            20
        ];

        // Create the gradient
        var rainbowGradient = new Gradient (stops, steps, TextEffectsScenario._loopingGradient);

        for (var x = 0; x < viewport.Width; x++)
        {
            double fraction = (double)x / (viewport.Width - 1);
            Color color = rainbowGradient.GetColorAtFraction (fraction);

            SetColor (color);

            AddRune (x, 0, new ('█'));
        }
    }

    private void SetColor (Color color) { SetAttribute (new (color, color)); }
}
