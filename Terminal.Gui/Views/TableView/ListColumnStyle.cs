namespace Terminal.Gui; 
/// <summary>
/// Defines rendering options that affect how the view is displayed.
/// </summary>
public class ListColumnStyle {

	/// <summary>
	/// Gets or sets an Orientation enum indicating whether to populate data down each column
	/// rather than across each row.  Defaults to <see cref="Orientation.Horizontal"/>.
	/// </summary>
	public Orientation Orientation { get; set; } = Orientation.Horizontal;

	/// <summary>
	/// Gets or sets a flag indicating whether to scroll in the same direction as <see cref="ListColumnStyle.Orientation"/>.
	/// Defaults to <see langword="false"/>.
	/// </summary>
	public bool ScrollParallel { get; set; } = false;
}
