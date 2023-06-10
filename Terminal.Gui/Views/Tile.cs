using System;

namespace Terminal.Gui; 

/// <summary>
/// A single <see cref="ContentView"/> presented in a <see cref="TileView"/>. To create
/// new instances use <see cref="TileView.RebuildForTileCount(int)"/> 
/// or <see cref="TileView.InsertTile(int)"/>.
/// </summary>
public class Tile {
	/// <summary>
	/// The <see cref="ContentView"/> that is contained in this <see cref="TileView"/>.
	/// Add new child views to this member for multiple 
	/// <see cref="ContentView"/>s within the <see cref="Tile"/>.
	/// </summary>
	public View ContentView { get; internal set; }

	/// <summary>
	/// Gets or Sets the minimum size you to allow when splitter resizing along
	/// parent <see cref="TileView.Orientation"/> direction.
	/// </summary>
	public int MinSize { get; set; }

	/// <summary>
	/// The text that should be displayed above the <see cref="ContentView"/>. This 
	/// will appear over the splitter line or border (above the view client area).
	/// </summary>
	/// <remarks>
	/// Title are not rendered for root level tiles 
	/// <see cref="Gui.LineStyle"/> is <see cref="LineStyle.None"/>.
	///</remarks>
	public string Title {
		get => _title;
		set {
			if (!OnTitleChanging (_title, value)) {
				var old = _title;
				_title = value;
				OnTitleChanged (old, _title);
				return;
			}
			_title = value;
		}
	}

	private string _title = string.Empty;

	/// <summary>
	/// Called before the <see cref="Title"/> changes. Invokes the <see cref="TitleChanging"/> event, which can be cancelled.
	/// </summary>
	/// <param name="oldTitle">The <see cref="Title"/> that is/has been replaced.</param>
	/// <param name="newTitle">The new <see cref="Title"/> to be replaced.</param>
	/// <returns><c>true</c> if an event handler cancelled the Title change.</returns>
	public virtual bool OnTitleChanging (string oldTitle, string newTitle)
	{
		var args = new TitleEventArgs (oldTitle, newTitle);
		TitleChanging?.Invoke (this, args);
		return args.Cancel;
	}

	/// <summary>
	/// Event fired when the <see cref="Title"/> is changing. Set <see cref="TitleEventArgs.Cancel"/> to 
	/// <c>true</c> to cancel the Title change.
	/// </summary>
	public event EventHandler<TitleEventArgs> TitleChanging;

	/// <summary>
	/// Called when the <see cref="Title"/> has been changed. Invokes the <see cref="TitleChanged"/> event.
	/// </summary>
	/// <param name="oldTitle">The <see cref="Title"/> that is/has been replaced.</param>
	/// <param name="newTitle">The new <see cref="Title"/> to be replaced.</param>
	public virtual void OnTitleChanged (string oldTitle, string newTitle)
	{
		var args = new TitleEventArgs (oldTitle, newTitle);
		TitleChanged?.Invoke (this, args);
	}

	/// <summary>
	/// Event fired after the <see cref="Title"/> has been changed. 
	/// </summary>
	public event EventHandler<TitleEventArgs> TitleChanged;

	/// <summary>
	/// Creates a new instance of the <see cref="Tile"/> class.
	/// </summary>
	public Tile ()
	{
		ContentView = new View () { Width = Dim.Fill (), Height = Dim.Fill () };
#if DEBUG_IDISPOSABLE
		ContentView.Data = "Tile.ContentView";
#endif
		Title = string.Empty;
		MinSize = 0;
	}
}
