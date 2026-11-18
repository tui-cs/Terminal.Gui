#nullable enable
namespace UICatalog;

/// <summary>
///     Renders the Terminal.Gui logo in box-drawing characters with a diagonal gradient.
/// </summary>
public sealed class Logo : View
{
    // @formatter:off
    private const string ART = """
                               ╺┳╸┏━╸┏━┓┏┳┓╻┏┓╻┏━┓╻   ┏━╸╻ ╻╻
                                ┃ ┣╸ ┣┳┛┃┃┃┃┃┗┫┣━┫┃   ┃╺┓┃ ┃┃
                                ╹ ┗━╸╹┗╸╹ ╹╹╹ ╹╹ ╹┗━╸╹┗━┛┗━┛╹
                               """;

    // @formatter:on

    private static readonly string [] _artLines = ART.ReplaceLineEndings ("\n").Split ('\n');

    public Logo ()
    {
        int artWidth = _artLines.Select (line => line.Length).Prepend (0).Max ();

        Width = artWidth;
        Height = _artLines.Length;
    }

    /// <inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        List<Color> stops =
        [
            new (0, 128, 255), // Bright Blue
            new (0, 255, 128), // Bright Green
            new (255, 255), // Bright Yellow
            new (255, 128) // Bright Orange
        ];

        List<int> steps = [10];

        Gradient gradient = new (stops, steps);

        var artHeight = 3; // Only the box-drawing lines get the gradient
        int artWidth = _artLines [0].Length;

        Dictionary<Point, Color> colorMap = gradient.BuildCoordinateColorMapping (artHeight, artWidth, GradientDirection.Diagonal);

        Attribute normalAttr = GetAttributeForRole (VisualRole.Normal);

        for (var row = 0; row < _artLines.Length; row++)
        {
            string line = _artLines [row];

            for (var col = 0; col < line.Length; col++)
            {
                char ch = line [col];

                if (ch == ' ')
                {
                    continue;
                }

                // Gradient only on the 3 art lines; version text uses normal color
                if (row < 3)
                {
                    Point coord = new (col, row);

                    if (colorMap.TryGetValue (coord, out Color color))
                    {
                        SetAttribute (new Attribute (color, normalAttr.Background));
                    }
                    else
                    {
                        SetAttribute (normalAttr);
                    }
                }
                else
                {
                    SetAttribute (normalAttr);
                }

                Move (col, row);
                AddStr (ch.ToString ());
            }
        }

        return true;
    }
}
