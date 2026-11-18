namespace Terminal.Gui.Views;

/// <summary>Series composed of any number of discrete data points</summary>
public class ScatterSeries : ISeries
{
    /// <summary>
    ///     The color and character that will be rendered in the console when there are point(s) in the corresponding
    ///     graph space. Defaults to uncolored 'dot'
    /// </summary>
    public GraphCellToRender Fill { get; set; } = new (Glyphs.Dot);

    /// <summary>Collection of each discrete point in the series</summary>
    /// <returns></returns>
    public List<PointF> Points { get; set; } = [];

    /// <summary>Draws all points directly onto the graph</summary>
    public void DrawSeries (GraphView graph, Rectangle drawBounds, RectangleF graphBounds)
    {
        if (Fill.Color.HasValue)
        {
            graph.SetAttribute (Fill.Color.Value);
        }

        foreach (PointF p in Points.Where (graphBounds.Contains))
        {
            Point viewportPoint = graph.GraphSpaceToViewport (p);
            graph.AddRune (viewportPoint.X, viewportPoint.Y, Fill.Rune);
        }
    }
}
