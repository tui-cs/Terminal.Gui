namespace Terminal.Gui.Views;

/// <summary>Describes a series of data that can be rendered into a <see cref="GraphView"/>></summary>
public interface ISeries
{
    /// <summary>
    ///     Draws the <paramref name="graphBounds"/> section of a series into the <paramref name="graph"/> view
    ///     <paramref name="drawBounds"/>
    /// </summary>
    /// <param name="graph">Graph series is to be drawn onto</param>
    /// <param name="drawBounds">Visible area of the graph in Console Screen units (excluding margins)</param>
    /// <param name="graphBounds">Visible area of the graph in Graph space units</param>
    void DrawSeries (GraphView graph, Rectangle drawBounds, RectangleF graphBounds);
}
