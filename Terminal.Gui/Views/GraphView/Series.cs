namespace Terminal.Gui.Views;

/// <summary>Series of bars positioned at regular intervals</summary>
public class BarSeries : ISeries
{
    /// <summary>
    ///     Determines the spacing of bars along the axis. Defaults to 1 i.e. every 1 unit of graph space a bar is
    ///     rendered. Note that you should also consider <see cref="GraphView.CellSize"/> when changing this.
    /// </summary>
    public float BarEvery { get; set; } = 1;

    /// <summary>Ordered collection of graph bars to position along axis</summary>
    public List<BarSeriesBar> Bars { get; set; } = [];

    /// <summary>True to draw <see cref="BarSeriesBar.Text"/> along the axis under the bar.  Defaults to true.</summary>
    public bool DrawLabels { get; set; } = true;

    /// <summary>
    ///     The number of units of graph space along the axis before rendering the first bar (and subsequent bars - see
    ///     <see cref="BarEvery"/>).  Defaults to 0
    /// </summary>
    public float Offset { get; set; }

    /// <summary>Direction bars protrude from the corresponding axis. Defaults to vertical</summary>
    public Orientation Orientation { get; set; } = Orientation.Vertical;

    /// <summary>Overrides the <see cref="BarSeriesBar.Fill"/> with a fixed color</summary>
    public Attribute? OverrideBarColor { get; set; }

    /// <summary>Draws bars that are currently in the <paramref name="drawBounds"/></summary>
    /// <param name="graph"></param>
    /// <param name="drawBounds">Screen area of the graph excluding margins</param>
    /// <param name="graphBounds">Graph space area that should be drawn into <paramref name="drawBounds"/></param>
    public virtual void DrawSeries (GraphView graph, Rectangle drawBounds, RectangleF graphBounds)
    {
        for (var i = 0; i < Bars.Count; i++)
        {
            float xStart = Orientation == Orientation.Horizontal ? 0 : Offset + (i + 1) * BarEvery;
            float yStart = Orientation == Orientation.Horizontal ? Offset + (i + 1) * BarEvery : 0;

            float endX = Orientation == Orientation.Horizontal ? Bars [i].Value : xStart;
            float endY = Orientation == Orientation.Horizontal ? yStart : Bars [i].Value;

            // translate to viewport positions
            Point viewportStart = graph.GraphSpaceToViewport (new (xStart, yStart));
            Point viewportEnd = graph.GraphSpaceToViewport (new (endX, endY));

            // Start the bar from wherever the axis is
            if (Orientation == Orientation.Horizontal)
            {
                viewportStart.X = graph.AxisY.GetAxisXPosition (graph);

                // don't draw bar off the right of the control
                viewportEnd.X = Math.Min (graph.Viewport.Width - 1, viewportEnd.X);

                // if bar is off the screen
                if (viewportStart.Y < 0 || viewportStart.Y > drawBounds.Height - graph.MarginBottom)
                {
                    continue;
                }
            }
            else
            {
                // Start the axis
                viewportStart.Y = graph.AxisX.GetAxisYPosition (graph);

                // don't draw bar up above top of control
                viewportEnd.Y = Math.Max (0, viewportEnd.Y);

                // if bar is off the screen
                if (viewportStart.X < graph.MarginLeft || viewportStart.X > graph.MarginLeft + drawBounds.Width - 1)
                {
                    continue;
                }
            }

            // draw the bar unless it has no height
            if (Bars [i].Value != 0)
            {
                DrawBarLine (graph, viewportStart, viewportEnd, Bars [i]);
            }

            // If we are drawing labels and the bar has one
            if (DrawLabels && !string.IsNullOrWhiteSpace (Bars [i].Text))
            {
                // Add the label to the relevant axis
                if (Orientation == Orientation.Horizontal)
                {
                    graph.AxisY.DrawAxisLabel (graph, viewportStart.Y, Bars [i].Text);
                }
                else if (Orientation == Orientation.Vertical)
                {
                    graph.AxisX.DrawAxisLabel (graph, viewportStart.X, Bars [i].Text);
                }
            }
        }
    }

    /// <summary>Applies any color overriding</summary>
    /// <param name="graphCellToRender"></param>
    /// <returns></returns>
    protected virtual GraphCellToRender AdjustColor (GraphCellToRender graphCellToRender)
    {
        if (OverrideBarColor.HasValue)
        {
            graphCellToRender.Color = OverrideBarColor;
        }

        return graphCellToRender;
    }

    /// <summary>Override to do custom drawing of the bar e.g. to apply varying color or changing the fill symbol mid-bar.</summary>
    /// <param name="graph"></param>
    /// <param name="start">Screen position of the start of the bar</param>
    /// <param name="end">Screen position of the end of the bar</param>
    /// <param name="beingDrawn">The Bar that occupies this space and is being drawn</param>
    protected virtual void DrawBarLine (GraphView graph, Point start, Point end, BarSeriesBar beingDrawn)
    {
        GraphCellToRender adjusted = AdjustColor (beingDrawn.Fill);

        if (adjusted.Color.HasValue)
        {
            graph.SetAttribute (adjusted.Color.Value);
        }

        graph.DrawLine (start, end, adjusted.Rune);

        graph.SetDriverColorToGraphColor ();
    }
}
