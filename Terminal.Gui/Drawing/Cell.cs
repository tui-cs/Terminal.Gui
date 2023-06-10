using System.Text;


namespace Terminal.Gui; 

/// <summary>
/// Represents a single row/column within the <see cref="LineCanvas"/>. Includes the glyph and the foreground/background colors.
/// </summary>
public class Cell {
	/// <summary>
	/// The glyph to draw.
	/// </summary>
	public Rune? Rune { get; set; }

	/// <summary>
	/// The foreground color to draw the glyph with.
	/// </summary>
	public Attribute? Attribute { get; set; }

}
