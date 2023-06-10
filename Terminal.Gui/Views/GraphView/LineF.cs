namespace Terminal.Gui; 

/// <summary>
/// Describes two points in graph space and a line between them
/// </summary>
public class LineF {
	/// <summary>
	/// The start of the line
	/// </summary>
	public PointF Start { get; }

	/// <summary>
	/// The end point of the line
	/// </summary>
	public PointF End { get; }

	/// <summary>
	/// Creates a new line between the points
	/// </summary>
	public LineF (PointF start, PointF end)
	{
		this.Start = start;
		this.End = end;
	}
}
