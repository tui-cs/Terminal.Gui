//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
// NOTE: Window is functionally identical to FrameView with the following exceptions. 
//  - Window is a Toplevel
//  - FrameView Does not support padding (but should)
//  - FrameView Does not support mouse dragging
//  - FrameView Does not support IEnumerable
// Any updates done here should probably be done in FrameView as well; TODO: Merge these classes

using System;
using NStack;

namespace Terminal.Gui {
	/// <summary>
	/// Event arguments for Title change events.
	/// </summary>
	public class TitleEventArgs : EventArgs {
		/// <summary>
		/// The new Window Title.
		/// </summary>
		public ustring NewTitle { get; set; }

		/// <summary>
		/// The old Window Title.
		/// </summary>
		public ustring OldTitle { get; set; }

		/// <summary>
		/// Flag which allows canceling the Title change.
		/// </summary>
		public bool Cancel { get; set; }

		/// <summary>
		/// Initializes a new instance of <see cref="TitleEventArgs"/>
		/// </summary>
		/// <param name="oldTitle">The <see cref="Window.Title"/> that is/has been replaced.</param>
		/// <param name="newTitle">The new <see cref="Window.Title"/> to be replaced.</param>
		public TitleEventArgs (ustring oldTitle, ustring newTitle)
		{
			OldTitle = oldTitle;
			NewTitle = newTitle;
		}
	}
}
