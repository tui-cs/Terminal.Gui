namespace Terminal.Gui.Views;

/// <summary>The horizontal (x-axis) of a <see cref="GraphView"/></summary>
public class HorizontalAxis : Axis
{
    /// <summary>
    ///     Creates a new instance of axis with an <see cref="Orientation"/> of <see cref="Orientation.Horizontal"/>
    /// </summary>
    public HorizontalAxis () : base (Orientation.Horizontal) { }

    /// <summary>
    ///     Draws the given <paramref name="text"/> on the axis at x <paramref name="screenPosition"/>. For the screen y
    ///     position use <see cref="GetAxisYPosition(GraphView)"/>
    /// </summary>
    /// <param name="graph">Graph being drawn onto</param>
    /// <param name="screenPosition">Number of screen columns along the axis to take before rendering</param>
    /// <param name="text">Text to render under the axis tick</param>
    public override void DrawAxisLabel (GraphView graph, int screenPosition, string text)
    {
        int y = GetAxisYPosition (graph);

        graph.Move (screenPosition, y);

        // draw the tick on the axis
        graph.AddRune (Glyphs.TopTee);

        // and the label text
        if (string.IsNullOrWhiteSpace (text))
        {
            return;
        }

        // center the label but don't draw it outside bounds of the graph
        int drawAtX = Math.Max (0, screenPosition - text.Length / 2);
        string toRender = text;

        // this is how much space is left
        int xSpaceAvailable = graph.Viewport.Width - drawAtX;

        // There is no space for the label at all!
        if (xSpaceAvailable <= 0)
        {
            return;
        }

        // if we are close to right side of graph, don't overspill
        if (toRender.Length > xSpaceAvailable)
        {
            toRender = toRender [..xSpaceAvailable];
        }

        graph.Move (drawAtX, Math.Min (y + 1, graph.Viewport.Height - 1));
        graph.AddStr (toRender);
    }

    /// <summary>Draws the horizontal x-axis labels and <see cref="Axis.Increment"/> ticks</summary>
    public override void DrawAxisLabels (GraphView graph)
    {
        if (!Visible || Increment == 0)
        {
            return;
        }

        Rectangle viewport = graph.Viewport;

        IEnumerable<AxisIncrementToRender> labels = GetLabels (graph, viewport);

        foreach (AxisIncrementToRender label in labels)
        {
            DrawAxisLabel (graph, label.ScreenLocation, label.Text);
        }

        // if there is a title
        if (!string.IsNullOrWhiteSpace (Text))
        {
            string toRender = Text;

            // if label is too long
            if (toRender.Length > graph.Viewport.Width)
            {
                toRender = toRender.Substring (0, graph.Viewport.Width);
            }

            graph.Move (graph.Viewport.Width / 2 - toRender.Length / 2, graph.Viewport.Height - 1);
            graph.AddStr (toRender);
        }
    }

    /// <summary>Draws the horizontal axis line</summary>
    /// <param name="graph"></param>
    public override void DrawAxisLine (GraphView graph)
    {
        if (!Visible)
        {
            return;
        }

        Rectangle bounds = graph.Viewport;

        graph.Move (0, 0);

        int y = GetAxisYPosition (graph);

        // start the x-axis at left of screen (either 0 or margin)
        var xStart = (int)graph.MarginLeft;

        // but if the x-axis has a minimum (minimum is in graph space units)
        if (Minimum.HasValue)
        {
            // start at the viewport location of the minimum
            int minimumViewportX = graph.GraphSpaceToViewport (new (Minimum.Value, y)).X;

            // unless that is off the viewport to the left
            xStart = Math.Max (xStart, minimumViewportX);
        }

        for (int i = xStart; i < bounds.Width; i++)
        {
            DrawAxisLine (graph, i, y);
        }
    }

    /// <summary>
    ///     Returns the Y viewport position of the origin (typically 0,0) of graph space. Return value is bounded by the
    ///     viewport i.e. the axis is always rendered even if the origin is offscreen.
    /// </summary>
    /// <param name="graph"></param>
    public int GetAxisYPosition (GraphView graph)
    {
        // find the origin of the graph in viewport space (this allows for 'crosshair' style
        // graphs where positive and negative numbers visible
        Point origin = graph.GraphSpaceToViewport (new (0, 0));

        // float the X axis so that it accurately represents the origin of the graph
        // but anchor it to top/bottom if the origin is offscreen
        return Math.Min (Math.Max (0, origin.Y), graph.Viewport.Height - ((int)graph.MarginBottom + 1));
    }

    /// <summary>Draws a horizontal axis line at the given <paramref name="x"/>, <paramref name="y"/> screen coordinates</summary>
    /// <param name="graph"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    protected override void DrawAxisLine (GraphView graph, int x, int y)
    {
        graph.Move (x, y);
        graph.AddRune (Glyphs.HLine);
    }

    private IEnumerable<AxisIncrementToRender> GetLabels (GraphView graph, Rectangle viewport)
    {
        // if no labels
        if (Increment == 0)
        {
            yield break;
        }

        var labels = 0;
        int y = GetAxisYPosition (graph);

        RectangleF start = graph.ViewportToGraphSpace ((int)graph.MarginLeft, y);
        RectangleF end = graph.ViewportToGraphSpace (viewport.Width, y);

        // don't draw labels below the minimum
        if (Minimum.HasValue)
        {
            start.X = Math.Max (start.X, Minimum.Value);
        }

        RectangleF current = start;

        while (current.X < end.X)
        {
            int viewportX = graph.GraphSpaceToViewport (new (current.X, current.Y)).X;

            // The increment we will render (normally a top T Unicode symbol)
            var toRender = new AxisIncrementToRender (Orientation, viewportX, current.X);

            // Not every increment has to have a label
            if (ShowLabelsEvery != 0)
            {
                // if this increment also needs a label
                if (labels++ % ShowLabelsEvery == 0)
                {
                    toRender.Text = LabelGetter (toRender);
                }
            }

            // Label or no label definitely render it
            yield return toRender;

            current.X += Increment;
        }
    }
}
