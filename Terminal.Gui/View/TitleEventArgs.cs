using System;
using System.Text;

namespace Terminal.Gui {
	/// <summary>
	/// Event arguments for Title change events.
	/// </summary>
	public class TitleEventArgs : EventArgs {
		/// <summary>
		/// The new Window Title.
		/// </summary>
		public string NewTitle { get; set; }

		/// <summary>
		/// The old Window Title.
		/// </summary>
		public string OldTitle { get; set; }

		/// <summary>
		/// Flag which allows canceling the Title change.
		/// </summary>
		public bool Cancel { get; set; }

		/// <summary>
		/// Initializes a new instance of <see cref="TitleEventArgs"/>
		/// </summary>
		/// <param name="oldTitle">The <see cref="View.Title"/> that is/has been replaced.</param>
		/// <param name="newTitle">The new <see cref="View.Title"/> to be replaced.</param>
		public TitleEventArgs (string oldTitle, string newTitle)
		{
			OldTitle = oldTitle;
			NewTitle = newTitle;
		}
	}
}
