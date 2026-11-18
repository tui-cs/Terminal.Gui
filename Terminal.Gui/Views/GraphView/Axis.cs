namespace Terminal.Gui.Views;

/// <summary>Renders a continuous line with grid line ticks and labels</summary>
public abstract class Axis
{
    /// <summary>Default value for <see cref="ShowLabelsEvery"/></summary>
    private const uint DEFAULT_SHOW_LABELS_EVERY = 5;

    /// <summary>
    ///     Allows you to control what label text is rendered for a given <see cref="Increment"/> when
    ///     <see cref="ShowLabelsEvery"/> is above 0
    /// </summary>
    public LabelGetterDelegate LabelGetter { get; set; }

    /// <summary>Populates base properties and sets the read only <see cref="Orientation"/></summary>
    /// <param name="orientation"></param>
    protected Axis (Orientation orientation)
    {
        Orientation = orientation;
        LabelGetter = DefaultLabelGetter;
    }

    /// <summary>Number of units of graph space between ticks on axis. 0 for no ticks</summary>
    /// <value></value>
    public float Increment { get; set; } = 1;

    /// <summary>The minimum axis point to show.  Defaults to null (no minimum)</summary>
    public float? Minimum { get; set; }

    /// <summary>Direction of the axis</summary>
    /// <value></value>
    public Orientation Orientation { get; }

    /// <summary>The number of <see cref="Increment"/> before a label is added. 0 = never show labels</summary>
    public uint ShowLabelsEvery { get; set; } = DEFAULT_SHOW_LABELS_EVERY;

    /// <summary>
    ///     Displayed below/to left of labels (see <see cref="Orientation"/>). If text is not visible, check
    ///     <see cref="GraphView.MarginBottom"/> / <see cref="GraphView.MarginLeft"/>
    /// </summary>
    public string? Text { get; set; }

    /// <summary>True to render axis.  Defaults to true</summary>
    public bool Visible { get; set; } = true;

    /// <summary>
    ///     Draws a custom label <paramref name="text"/> at <paramref name="screenPosition"/> units along the axis (X or Y
    ///     depending on <see cref="Orientation"/>)
    /// </summary>
    /// <param name="graph"></param>
    /// <param name="screenPosition"></param>
    /// <param name="text"></param>
    public abstract void DrawAxisLabel (GraphView graph, int screenPosition, string text);

    /// <summary>Draws labels and axis <see cref="Increment"/> ticks</summary>
    /// <param name="graph"></param>
    public abstract void DrawAxisLabels (GraphView graph);

    /// <summary>Draws the solid line of the axis</summary>
    /// <param name="graph"></param>
    public abstract void DrawAxisLine (GraphView graph);

    /// <summary>Resets all configurable properties of the axis to default values</summary>
    public virtual void Reset ()
    {
        Increment = 1;
        ShowLabelsEvery = DEFAULT_SHOW_LABELS_EVERY;
        Visible = true;
        Text = "";
        LabelGetter = DefaultLabelGetter;
        Minimum = null;
    }

    /// <summary>Draws a single cell of the solid line of the axis</summary>
    /// <param name="graph"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    protected abstract void DrawAxisLine (GraphView graph, int x, int y);

    private static string DefaultLabelGetter (AxisIncrementToRender toRender) { return toRender.Value.ToString ("N0"); }
}

/// <summary>Delegate for custom formatting of axis labels.  Determines what should be displayed at a given label</summary>
/// <param name="toRender">The axis increment to which the label is attached</param>
/// <returns></returns>
public delegate string LabelGetterDelegate (AxisIncrementToRender toRender);
