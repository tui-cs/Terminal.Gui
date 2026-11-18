namespace Terminal.Gui.Views;

/// <summary>Displays text at a given position (in screen space or graph space)</summary>
public class TextAnnotation : IAnnotation
{
    /// <summary>
    ///     The location in graph space to draw the <see cref="Text"/>.  This annotation will only show if the point is in
    ///     the current viewable area of the graph presented in the <see cref="GraphView"/>
    /// </summary>
    public PointF GraphPosition { get; set; }

    /// <summary>
    ///     The location on screen to draw the <see cref="Text"/> regardless of scroll/zoom settings.  This overrides
    ///     <see cref="GraphPosition"/> if specified.
    /// </summary>
    public Point? ScreenPosition { get; set; }

    /// <summary>Text to display on the graph</summary>
    public required string Text { get; set; }

    /// <summary>True to add text before plotting series.  Defaults to false</summary>
    public bool BeforeSeries { get; set; }

    /// <summary>Draws the annotation</summary>
    /// <param name="graph"></param>
    /// <param name="drawContext"></param>
    public void Render (GraphView graph, DrawContext? drawContext)
    {
        if (ScreenPosition.HasValue)
        {
            DrawText (graph, ScreenPosition.Value.X, ScreenPosition.Value.Y);

            return;
        }

        Point viewportPos = graph.GraphSpaceToViewport (GraphPosition);
        DrawText (graph, viewportPos.X, viewportPos.Y);
    }

    /// <summary>
    ///     Draws the <see cref="Text"/> at the given coordinates with truncation to avoid spilling over
    ///     <see name="View.Viewport"/> of the <paramref name="graph"/>
    /// </summary>
    /// <param name="graph"></param>
    /// <param name="x">Screen x position to start drawing string</param>
    /// <param name="y">Screen y position to start drawing string</param>
    protected void DrawText (GraphView graph, int x, int y)
    {
        // the draw point is out of control bounds
        if (!graph.Viewport.Contains (new Point (x, y)))
        {
            return;
        }

        // There is no text to draw
        if (string.IsNullOrWhiteSpace (Text))
        {
            return;
        }

        graph.Move (x, y);

        int availableWidth = graph.Viewport.Width - x;

        if (availableWidth <= 0)
        {
            return;
        }

        graph.AddStr (Text.Length < availableWidth ? Text : Text [..availableWidth]);
    }
}
