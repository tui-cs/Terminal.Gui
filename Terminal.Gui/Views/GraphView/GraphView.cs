namespace Terminal.Gui.Views;

/// <summary>Displays graphs (bar, scatter, etc...) with flexible labels, scaling, and scrolling.</summary>
/// <remarks>
/// <img src="../images/views/GraphView.gif" alt="GraphView demo"/>
///     <para>Default key bindings:</para>
///     <list type="table">
///         <listheader>
///             <term>Key</term> <description>Action</description>
///         </listheader>
///         <item>
///             <term>Left / Right</term> <description>Scrolls the graph left or right.</description>
///         </item>
///         <item>
///             <term>Up / Down</term> <description>Scrolls the graph up or down.</description>
///         </item>
///     </list>
/// </remarks>
public class GraphView : View, IDesignable
{
    /// <summary>Creates a new graph with a 1 to 1 graph space with absolute layout.</summary>
    public GraphView ()
    {
        CanFocus = true;

        AxisX = new ();
        AxisY = new ();

        // Things this view knows how to do
        AddCommand (
                    Command.ScrollUp,
                    () =>
                    {
                        Scroll (0, CellSize.Y);

                        return true;
                    }
                   );

        AddCommand (
                    Command.ScrollDown,
                    () =>
                    {
                        Scroll (0, -CellSize.Y);

                        return true;
                    }
                   );

        AddCommand (
                    Command.ScrollRight,
                    () =>
                    {
                        Scroll (CellSize.X, 0);

                        return true;
                    }
                   );

        AddCommand (
                    Command.ScrollLeft,
                    () =>
                    {
                        Scroll (-CellSize.X, 0);

                        return true;
                    }
                   );

        AddCommand (
                    Command.PageUp,
                    () =>
                    {
                        PageUp ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.PageDown,
                    () =>
                    {
                        PageDown ();

                        return true;
                    }
                   );

        KeyBindings.Add (Key.CursorRight, Command.ScrollRight);
        KeyBindings.Add (Key.CursorLeft, Command.ScrollLeft);
        KeyBindings.Add (Key.CursorUp, Command.ScrollUp);
        KeyBindings.Add (Key.CursorDown, Command.ScrollDown);

        // Not bound by default (preserves backwards compatibility)
        //KeyBindings.Add (Key.PageUp, Command.PageUp);
        //KeyBindings.Add (Key.PageDown, Command.PageDown);
    }

    /// <summary>Elements drawn into graph after series have been drawn e.g. Legends etc.</summary>
    public List<IAnnotation> Annotations { get; } = new ();

    /// <summary>Horizontal axis.</summary>
    /// <value></value>
    public HorizontalAxis AxisX { get; set; }

    /// <summary>Vertical axis.</summary>
    /// <value></value>
    public VerticalAxis AxisY { get; set; }

    /// <summary>
    ///     Translates console width/height into graph space. Defaults to 1 row/col of console space being 1 unit of graph
    ///     space.
    /// </summary>
    /// <returns></returns>
    public PointF CellSize { get; set; } = new (1, 1);

    /// <summary>The color of the background of the graph and axis/labels.</summary>
    public Attribute? GraphColor { get; set; }

    /// <summary>
    ///     Amount of space (in viewport columns) to reserve on the bottom of the graph for axis labels and legends.
    ///     Graph content (<see cref="Series"/>) will not be rendered in this internal margin area, but axis labels may be.
    ///     This is separate from the View's <see cref="View.Margin"/>, <see cref="View.Border"/>, and
    ///     <see cref="View.Padding"/> adornments,
    ///     which create space outside the viewport.
    /// </summary>
    public uint MarginBottom { get; set; }

    /// <summary>
    ///     Amount of space (in viewport columns) to reserve on the left of the graph for axis labels.
    ///     Graph content (<see cref="Series"/>) will not be rendered in this internal margin area, but axis labels may be.
    ///     This is separate from the View's <see cref="View.Margin"/>, <see cref="View.Border"/>, and
    ///     <see cref="View.Padding"/> adornments,
    ///     which create space outside the viewport.
    /// </summary>
    public uint MarginLeft { get; set; }

    /// <summary>
    ///     The graph space position of the bottom left of the graph. Changing this scrolls the viewport around in the
    ///     graph.
    /// </summary>
    /// <value></value>
    public PointF ScrollOffset { get; set; } = new (0, 0);

    /// <summary>Collection of data series that are rendered in the graph.</summary>
    public List<ISeries> Series { get; } = [];

    #region Bresenham's line algorithm

    // https://rosettacode.org/wiki/Bitmap/Bresenham%27s_line_algorithm#C.23

    /// <summary>Draws a line between two points in screen space. Can be diagonals.</summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="symbol">The symbol to use for the line</param>
    public void DrawLine (Point start, Point end, Rune symbol)
    {
        if (Equals (start, end))
        {
            return;
        }

        int x0 = start.X;
        int y0 = start.Y;
        int x1 = end.X;
        int y1 = end.Y;

        int dx = Math.Abs (x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = Math.Abs (y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = (dx > dy ? dx : -dy) / 2, e2;

        while (true)
        {
            AddRune (x0, y0, symbol);

            if (x0 == x1 && y0 == y1)
            {
                break;
            }

            e2 = err;

            if (e2 > -dx)
            {
                err -= dy;
                x0 += sx;
            }

            if (e2 < dy)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    #endregion

    /// <summary>Calculates the viewport-relative location for a given point in graph space.</summary>
    /// <param name="location">
    ///     Point in graph space that may or may not be represented in the visible area of graph currently
    ///     presented.  E.g. 0,0 for origin.
    /// </param>
    /// <returns>
    ///     Viewport-relative position (Column/Row) which would be used to render the graph <paramref name="location"/>. Note
    ///     that
    ///     this can be outside the current content area of the view. Viewport-relative coordinates have (0,0) at the top-left
    ///     of the View's content area (after accounting for Border, Margin, and Padding).
    /// </returns>
    public Point GraphSpaceToViewport (PointF location)
    {
        return new (
                    (int)((location.X - ScrollOffset.X) / CellSize.X) + (int)MarginLeft,

                    // screen coordinates are top down while graph coordinates are bottom up
                    Viewport.Height - 1 - (int)MarginBottom - (int)((location.Y - ScrollOffset.Y) / CellSize.Y)
                   );
    }

    ///<inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        if (CellSize.X == 0 || CellSize.Y == 0)
        {
            throw new ($"{nameof (CellSize)} cannot be 0");
        }

        SetDriverColorToGraphColor ();

        Move (0, 0);

        // clear all old content
        for (var i = 0; i < Viewport.Height; i++)
        {
            Move (0, i);
            AddStr (new (' ', Viewport.Width));
        }

        // If there is no data do not display a graph
        if (!Series.Any () && !Annotations.Any ())
        {
            return true;
        }

        // The drawable area of the graph (anything that isn't in the margins)
        int graphScreenWidth = Viewport.Width - (int)MarginLeft;
        int graphScreenHeight = Viewport.Height - (int)MarginBottom;

        // if the margins take up the full draw bounds don't render
        if (graphScreenWidth < 0 || graphScreenHeight < 0)
        {
            return true;
        }

        // Draw 'before' annotations
        foreach (IAnnotation a in Annotations.ToArray ().Where (a => a.BeforeSeries))
        {
            a.Render (this, context);
        }

        SetDriverColorToGraphColor ();

        AxisY.DrawAxisLine (this);
        AxisX.DrawAxisLine (this);

        AxisY.DrawAxisLabels (this);
        AxisX.DrawAxisLabels (this);

        // Draw a cross where the two axis cross
        var axisIntersection = new Point (AxisY.GetAxisXPosition (this), AxisX.GetAxisYPosition (this));

        if (AxisX.Visible && AxisY.Visible)
        {
            Move (axisIntersection.X, axisIntersection.Y);
            AddRune (axisIntersection.X, axisIntersection.Y, (Rune)'\u253C');
        }

        SetDriverColorToGraphColor ();

        var drawBounds = new Rectangle ((int)MarginLeft, 0, graphScreenWidth, graphScreenHeight);

        RectangleF graphSpace = ViewportToGraphSpace (drawBounds);

        foreach (ISeries s in Series.ToArray ())
        {
            s.DrawSeries (this, drawBounds, graphSpace);

            // If a series changes the graph color reset it
            SetDriverColorToGraphColor ();
        }

        SetDriverColorToGraphColor ();

        // Draw 'after' annotations
        foreach (IAnnotation a in Annotations.ToArray ().Where (a => !a.BeforeSeries))
        {
            a.Render (this, context);
        }

        return true;
    }

    /// <summary>Scrolls the graph down 1 page.</summary>
    public void PageDown () { Scroll (0, -1 * CellSize.Y * Viewport.Height); }

    /// <summary>Scrolls the graph up 1 page.</summary>
    public void PageUp () { Scroll (0, CellSize.Y * Viewport.Height); }

    /// <summary>
    ///     Clears all settings configured on the graph and resets all properties to default values (
    ///     <see cref="CellSize"/>, <see cref="ScrollOffset"/> etc) .
    /// </summary>
    public void Reset ()
    {
        ScrollOffset = new (0, 0);
        CellSize = new (1, 1);
        AxisX.Reset ();
        AxisY.Reset ();
        Series.Clear ();
        Annotations.Clear ();
        GraphColor = null;
        SetNeedsDraw ();
    }

    /// <summary>Returns the section of the graph that is represented by the given viewport-relative position.</summary>
    /// <param name="col">Column in viewport-relative coordinates.</param>
    /// <param name="row">Row in viewport-relative coordinates.</param>
    /// <returns>The rectangle in graph space that corresponds to the viewport cell at the given position.</returns>
    public RectangleF ViewportToGraphSpace (int col, int row)
    {
        return new (
                    ScrollOffset.X + (col - MarginLeft) * CellSize.X,
                    ScrollOffset.Y + (Viewport.Height - (row + MarginBottom + 1)) * CellSize.Y,
                    CellSize.X,
                    CellSize.Y
                   );
    }

    /// <summary>Returns the section of the graph that is represented by the viewport-relative area.</summary>
    /// <param name="viewportArea">Rectangle in viewport-relative coordinates.</param>
    /// <returns>The rectangle in graph space that corresponds to the viewport area.</returns>
    public RectangleF ViewportToGraphSpace (Rectangle viewportArea)
    {
        // get position of the bottom left
        RectangleF pos = ViewportToGraphSpace (viewportArea.Left, viewportArea.Bottom - 1);

        return pos with { Width = viewportArea.Width * CellSize.X, Height = viewportArea.Height * CellSize.Y };
    }

    /// <summary>
    ///     Scrolls the view by a given number of units in graph space. See <see cref="CellSize"/> to translate this into
    ///     rows/cols.
    /// </summary>
    /// <param name="offsetX"></param>
    /// <param name="offsetY"></param>
    public void Scroll (float offsetX, float offsetY)
    {
        ScrollOffset = new (
                            ScrollOffset.X + offsetX,
                            ScrollOffset.Y + offsetY
                           );

        SetNeedsDraw ();
    }

    /// <summary>
    ///     Sets the color attribute of <see cref="IApplication.Driver"/> to the <see cref="GraphColor"/> (if defined) or
    ///     <see cref="Scheme"/> otherwise.
    /// </summary>
    public void SetDriverColorToGraphColor () { SetAttribute (GraphColor ?? GetAttributeForRole (VisualRole.Normal)); }

    bool IDesignable.EnableForDesign ()
    {
        Title = "Sine Wave";

        var points = new ScatterSeries ();
        var line = new PathAnnotation ();

        // Draw line first so it does not draw over top of points or axis labels
        line.BeforeSeries = true;

        // Generate line graph with 2,000 points
        for (float x = -500; x < 500; x += 0.5f)
        {
            points.Points.Add (new (x, (float)Math.Sin (x)));
            line.Points.Add (new (x, (float)Math.Sin (x)));
        }

        Series.Add (points);
        Annotations.Add (line);

        // How much graph space each cell of the console depicts
        CellSize = new (0.1f, 0.1f);

        // leave space for axis labels
        MarginBottom = 2;
        MarginLeft = 3;

        // One axis tick/label per
        AxisX.Increment = 0.5f;
        AxisX.ShowLabelsEvery = 2;
        AxisX.Text = "X →";
        AxisX.LabelGetter = v => v.Value.ToString ("N2");

        AxisY.Increment = 0.2f;
        AxisY.ShowLabelsEvery = 2;
        AxisY.Text = "↑Y";
        AxisY.LabelGetter = v => v.Value.ToString ("N2");

        ScrollOffset = new (-2.5f, -1);

        return true;
    }

    /// <inheritdoc/>
    string? IDesignable.GetDemoKeyStrokes () => "wait:500,CursorRight,wait:400,CursorRight,wait:400,CursorRight,wait:400,CursorDown,wait:400,CursorDown,wait:800";
}
