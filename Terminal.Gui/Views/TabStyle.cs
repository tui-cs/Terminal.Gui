namespace Terminal.Gui; 

/// <summary>
/// Describes render stylistic selections of a <see cref="TabView"/>
/// </summary>
public class TabStyle {

	/// <summary>
	/// True to show the top lip of tabs.  False to directly begin with tab text during 
	/// rendering.  When true header line occupies 3 rows, when false only 2.
	/// Defaults to true.
	/// 
	/// <para>When <see cref="TabsOnBottom"/> is enabled this instead applies to the
	///  bottommost line of the control</para>
	/// </summary> 
	public bool ShowTopLine { get; set; } = true;

	/// <summary>
	/// True to show a solid box around the edge of the control.  Defaults to true.
	/// </summary>
	public bool ShowBorder { get; set; } = true;

	/// <summary>
	/// True to render tabs at the bottom of the view instead of the top
	/// </summary>
	public bool TabsOnBottom { get; set; } = false;

}
