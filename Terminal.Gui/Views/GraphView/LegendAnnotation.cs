namespace Terminal.Gui.Views;

/// <summary>
///     Used by <see cref="GraphView"/> to render symbol definitions in a graph, e.g. colors and their meanings
/// </summary>
public class LegendAnnotation : View, IAnnotation
{
    /// <summary>Ordered collection of entries that are rendered in the legend.</summary>
    private readonly List<Tuple<GraphCellToRender, string>> _entries = [];

    /// <summary>Creates a new empty legend at the empty screen coordinates.</summary>
    public LegendAnnotation () : this (Rectangle.Empty) { }

    /// <summary>Creates a new empty legend at the given screen coordinates.</summary>
    /// <param name="legendBounds">
    ///     Defines the area available for the legend to render in (within the graph).  This is in
    ///     screen units (i.e. not graph space)
    /// </param>
    public LegendAnnotation (Rectangle legendBounds)
    {
        X = legendBounds.X;
        Y = legendBounds.Y;
        Width = legendBounds.Width;
        Height = legendBounds.Height;
        BorderStyle = LineStyle.Single;
    }

    /// <summary>Returns false i.e. Legends render after series</summary>
    public bool BeforeSeries => false;

    // BUGBUG: Legend annotations are subviews. But for some reason they are rendered directly in OnDrawContent
    // BUGBUG: instead of just being normal subviews. They get rendered as blank rects, and thus we disable subview drawing.
    /// <inheritdoc/>
    protected override bool OnDrawingText () { return true; }

    // BUGBUG: Legend annotations are subviews. But for some reason they are rendered directly in OnDrawContent
    // BUGBUG: instead of just being normal subviews. They get rendered as blank rects, and thus we disable subview drawing.
    /// <inheritdoc/>
    protected override bool OnClearingViewport () { return true; }

    /// <summary>Draws the Legend and all entries into the area within <see cref="View.Viewport"/></summary>
    /// <param name="graph"></param>
    /// <param name="drawContext1"></param>
    public void Render (GraphView graph, DrawContext? drawContext1)
    {
        if (!IsInitialized)
        {
            // BUGBUG: We should be getting a visual role here?
            SetScheme (new () { Normal = GetCurrentAttribute () });
            graph.Add (this);
        }

        if (BorderStyle != LineStyle.None)
        {
            // BUGBUG: View code should never call Draw directly. This
            // BUGBUG: needs to be refactored to decouple.
            DrawAdornments ();
            if (Border.View is { } borderView)
            {
                LineCanvas.Merge (borderView.LineCanvas);
                borderView.LineCanvas.Clear ();
            }
            RenderLineCanvas (drawContext1);
        }

        var linesDrawn = 0;

        foreach (Tuple<GraphCellToRender, string> entry in _entries)
        {
            if (entry.Item1.Color.HasValue)
            {
                SetAttribute (entry.Item1.Color.Value);
            }
            else
            {
                graph.SetDriverColorToGraphColor ();
            }

            // add the symbol
            AddRune (0, linesDrawn, entry.Item1.Rune);

            // switch to normal coloring (for the text)
            graph.SetDriverColorToGraphColor ();

            // add the text
            Move (1, linesDrawn);

            string str = TextFormatter.ClipOrPad (entry.Item2, Viewport.Width - 1);
            AddStr (str);

            linesDrawn++;

            // Legend has run out of space
            if (linesDrawn >= Viewport.Height)
            {
                break;
            }
        }
    }

    /// <summary>Adds an entry into the legend.  Duplicate entries are permissible</summary>
    /// <param name="graphCellToRender">The symbol appearing on the graph that should appear in the legend</param>
    /// <param name="text">
    ///     Text to render on this line of the legend.  Will be truncated if outside of Legend
    ///     <see cref="View.Viewport"/>
    /// </param>
    public void AddEntry (GraphCellToRender graphCellToRender, string text) { _entries.Add (Tuple.Create (graphCellToRender, text)); }
}
