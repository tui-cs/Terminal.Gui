namespace Terminal.Gui; 

/// <summary>
/// A single tab in a <see cref="TabView"/>
/// </summary>
public class Tab {
	private string text;

	/// <summary>
	/// The text to display in a <see cref="TabView"/>
	/// </summary>
	/// <value></value>
	public string Text { get => text ?? "Unamed"; set => text = value; }

	/// <summary>
	/// The control to display when the tab is selected
	/// </summary>
	/// <value></value>
	public View View { get; set; }

	/// <summary>
	/// Creates a new unamed tab with no controls inside
	/// </summary>
	public Tab ()
	{

	}

	/// <summary>
	/// Creates a new tab with the given text hosting a view
	/// </summary>
	/// <param name="text"></param>
	/// <param name="view"></param>
	public Tab (string text, View view)
	{
		this.Text = text;
		this.View = view;
	}
}
