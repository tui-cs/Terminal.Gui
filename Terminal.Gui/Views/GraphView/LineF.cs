namespace Terminal.Gui.Views;

/// <summary>Describes two points in graph space and a line between them</summary>
public class LineF
{
    /// <summary>Creates a new line between the points</summary>
    public LineF (PointF start, PointF end)
    {
        Start = start;
        End = end;
    }

    /// <summary>The end point of the line</summary>
    public PointF End { get; }

    /// <summary>The start of the line</summary>
    public PointF Start { get; }
}
