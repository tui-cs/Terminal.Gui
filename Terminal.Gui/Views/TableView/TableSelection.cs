namespace Terminal.Gui; 

/// <summary>
/// Describes a selected region of the table
/// </summary>
public class TableSelection {

	/// <summary>
	/// Corner of the <see cref="Rect"/> where selection began
	/// </summary>
	/// <value></value>
	public Point Origin { get; set; }

	/// <summary>
	/// Area selected
	/// </summary>
	/// <value></value>
	public Rect Rect { get; set; }

	/// <summary>
	/// True if the selection was made through <see cref="Command.ToggleChecked"/>
	/// and therefore should persist even through keyboard navigation.
	/// </summary>
	public bool IsToggled { get; set; }

	/// <summary>
	/// Creates a new selected area starting at the origin corner and covering the provided rectangular area
	/// </summary>
	/// <param name="origin"></param>
	/// <param name="rect"></param>
	public TableSelection (Point origin, Rect rect)
	{
		Origin = origin;
		Rect = rect;
	}
}