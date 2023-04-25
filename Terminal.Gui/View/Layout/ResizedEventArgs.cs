//
// Core.cs: The core engine for gui.cs
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
// Pending:
//   - Check for NeedDisplay on the hierarchy and repaint
//   - Layout support
//   - "Colors" type or "Attributes" type?
//   - What to surface as "BackgroundCOlor" when clearing a window, an attribute or colors?
//
// Optimizations
//   - Add rendering limitation to the exposed area
using System;

namespace Terminal.Gui {

	/// <summary>
	/// Event arguments for the <see cref="Application.TerminalResized"/> event.
	/// </summary>
	public class ResizedEventArgs : EventArgs {
		/// <summary>
		/// The number of rows in the resized terminal.
		/// </summary>
		public int Rows { get; set; }
		/// <summary>
		/// The number of columns in the resized terminal.
		/// </summary>
		public int Cols { get; set; }
	}
}
