namespace Terminal.Gui.Views;

/// <summary>Sequence of lines to connect points e.g. of a <see cref="ScatterSeries"/></summary>
public class PathAnnotation : IAnnotation
{
    /// <summary>Color for the line that connects points</summary>
    public Attribute? LineColor { get; set; }

    /// <summary>The symbol that gets drawn along the line, defaults to '.'</summary>
    public Rune LineRune { get; set; } = new ('.');

    /// <summary>Points that should be connected.  Lines will be drawn between points in the order they appear in the list</summary>
    public List<PointF> Points { get; set; } = [];

    /// <summary>True to add line before plotting series.  Defaults to false</summary>
    public bool BeforeSeries { get; set; }

    /// <summary>Draws lines connecting each of the <see cref="Points"/></summary>
    /// <param name="graph"></param>
    /// <param name="drawContext"></param>
    public void Render (GraphView graph, DrawContext? drawContext)
    {
        graph.SetAttribute (LineColor ?? graph.GetAttributeForRole (VisualRole.Normal));

        foreach (LineF line in PointsToLines ())
        {
            Point start = graph.GraphSpaceToViewport (line.Start);
            Point end = graph.GraphSpaceToViewport (line.End);
            graph.DrawLine (start, end, LineRune);
        }
    }

    /// <summary>Generates lines joining <see cref="Points"/></summary>
    /// <returns></returns>
    private IEnumerable<LineF> PointsToLines ()
    {
        for (var i = 0; i < Points.Count - 1; i++)
        {
            yield return new (Points [i], Points [i + 1]);
        }
    }
}
