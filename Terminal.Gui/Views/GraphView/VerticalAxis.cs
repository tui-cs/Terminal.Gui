#nullable disable
namespace Terminal.Gui.Views;

/// <summary>The vertical (i.e. Y axis) of a <see cref="GraphView"/></summary>
public class VerticalAxis : Axis
{
    /// <summary>Creates a new <see cref="Orientation.Vertical"/> axis</summary>
    public VerticalAxis () : base (Orientation.Vertical) { }

    /// <summary>
    ///     Draws the given <paramref name="text"/> on the axis at y <paramref name="screenPosition"/>. For the screen x
    ///     position use <see cref="GetAxisXPosition(GraphView)"/>
    /// </summary>
    /// <param name="graph">Graph being drawn onto</param>
    /// <param name="screenPosition">Number of rows from the top of the screen (i.e. down the axis) before rendering</param>
    /// <param name="text">
    ///     Text to render to the left of the axis tick.  Ensure to set <see cref="GraphView.MarginLeft"/> or
    ///     <see cref="GraphView.ScrollOffset"/> sufficient that it is visible
    /// </param>
    public override void DrawAxisLabel (GraphView graph, int screenPosition, string text)
    {
        int x = GetAxisXPosition (graph);
        int labelThickness = text.Length;

        graph.Move (x, screenPosition);

        // draw the tick on the axis
        graph.AddRune (Glyphs.RightTee);

        // and the label text
        if (!string.IsNullOrWhiteSpace (text))
        {
            graph.Move (Math.Max (0, x - labelThickness), screenPosition);
            graph.AddStr (text);
        }
    }

    /// <summary>Draws axis <see cref="Axis.Increment"/> markers and labels</summary>
    /// <param name="graph"></param>
    public override void DrawAxisLabels (GraphView graph)
    {
        if (!Visible || Increment == 0)
        {
            return;
        }

        Rectangle bounds = graph.Viewport;
        IEnumerable<AxisIncrementToRender> labels = GetLabels (graph, bounds);

        foreach (AxisIncrementToRender label in labels)
        {
            DrawAxisLabel (graph, label.ScreenLocation, label.Text);
        }

        // if there is a title
        if (!string.IsNullOrWhiteSpace (Text))
        {
            string toRender = Text;

            // if label is too long
            if (toRender.Length > graph.Viewport.Height)
            {
                toRender = toRender.Substring (0, graph.Viewport.Height);
            }

            // Draw it 1 letter at a time vertically down row 0 of the control
            int startDrawingAtY = graph.Viewport.Height / 2 - toRender.Length / 2;

            for (var i = 0; i < toRender.Length; i++)
            {
                graph.Move (0, startDrawingAtY + i);
                graph.AddRune ((Rune)toRender [i]);
            }
        }
    }

    /// <summary>Draws the vertical axis line</summary>
    /// <param name="graph"></param>
    public override void DrawAxisLine (GraphView graph)
    {
        if (!Visible)
        {
            return;
        }

        Rectangle bounds = graph.Viewport;

        int x = GetAxisXPosition (graph);

        int yEnd = GetAxisYEnd (graph);

        // don't draw down further than the control bounds
        yEnd = Math.Min (yEnd, bounds.Height - (int)graph.MarginBottom);

        // Draw solid line
        for (var i = 0; i < yEnd; i++)
        {
            DrawAxisLine (graph, x, i);
        }
    }

    /// <summary>
    ///     Returns the X screen position of the origin (typically 0,0) of graph space. Return value is bounded by the
    ///     screen i.e. the axis is always rendered even if the origin is offscreen.
    /// </summary>
    /// <param name="graph"></param>
    public int GetAxisXPosition (GraphView graph)
    {
        // find the origin of the graph in screen space (this allows for 'crosshair' style
        // graphs where positive and negative numbers visible
        Point origin = graph.GraphSpaceToViewport (new (0, 0));

        // float the Y axis so that it accurately represents the origin of the graph
        // but anchor it to left/right if the origin is offscreen
        return Math.Min (Math.Max ((int)graph.MarginLeft, origin.X), graph.Viewport.Width - 1);
    }

    /// <summary>Draws a vertical axis line at the given <paramref name="x"/>, <paramref name="y"/> screen coordinates</summary>
    /// <param name="graph"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    protected override void DrawAxisLine (GraphView graph, int x, int y)
    {
        graph.Move (x, y);
        graph.AddRune (Glyphs.VLine);
    }

    private int GetAxisYEnd (GraphView graph)
    {
        // draw down the screen (0 is top of screen)
        // end at the bottom of the screen

        //unless there is a minimum
        if (Minimum.HasValue)
        {
            return graph.GraphSpaceToViewport (new (0, Minimum.Value)).Y;
        }

        return graph.Viewport.Height;
    }

    private IEnumerable<AxisIncrementToRender> GetLabels (GraphView graph, Rectangle bounds)
    {
        // if no labels
        if (Increment == 0)
        {
            yield break;
        }

        var labels = 0;
        int x = GetAxisXPosition (graph);

        // remember viewport space is top down so the lowest graph
        // space value is at the bottom of the viewport
        RectangleF start = graph.ViewportToGraphSpace (x, bounds.Height - (1 + (int)graph.MarginBottom));
        RectangleF end = graph.ViewportToGraphSpace (x, 0);

        // don't draw labels below the minimum
        if (Minimum.HasValue)
        {
            start.Y = Math.Max (start.Y, Minimum.Value);
        }

        RectangleF current = start;

        while (current.Y < end.Y)
        {
            int viewportY = graph.GraphSpaceToViewport (new (current.X, current.Y)).Y;

            // Create the axis symbol
            var toRender = new AxisIncrementToRender (Orientation, viewportY, current.Y);

            // and the label (if we are due one)
            if (ShowLabelsEvery != 0)
            {
                // if this increment also needs a label
                if (labels++ % ShowLabelsEvery == 0)
                {
                    toRender.Text = LabelGetter (toRender);
                }
            }

            // draw the axis symbol (and label if it has one)
            yield return toRender;

            current.Y += Increment;
        }
    }
}
