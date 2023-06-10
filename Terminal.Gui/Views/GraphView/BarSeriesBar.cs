namespace Terminal.Gui; 

/// <summary>
/// A single bar in a <see cref="BarSeries"/>
/// </summary>
public class BarSeriesBar {

	/// <summary>
	/// Optional text that describes the bar.  This will be rendered on the corresponding
	/// <see cref="Axis"/> unless <see cref="BarSeries.DrawLabels"/> is false
	/// </summary>
	public string Text { get; set; }

	/// <summary>
	/// The color and character that will be rendered in the console
	/// when the bar extends over it
	/// </summary>
	public GraphCellToRender Fill { get; set; }

	/// <summary>
	/// The value in graph space X/Y (depending on <see cref="Orientation"/>) to which the bar extends.
	/// </summary>
	public float Value { get; }

	/// <summary>
	/// Creates a new instance of a single bar rendered in the given <paramref name="fill"/> that extends
	/// out <paramref name="value"/> graph space units in the default <see cref="Orientation"/>
	/// </summary>
	/// <param name="text"></param>
	/// <param name="fill"></param>
	/// <param name="value"></param>
	public BarSeriesBar (string text, GraphCellToRender fill, float value)
	{
		Text = text;
		Fill = fill;
		Value = value;
	}
}
