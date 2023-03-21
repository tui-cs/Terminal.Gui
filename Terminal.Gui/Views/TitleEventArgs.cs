using NStack;
using System;

namespace Terminal.Gui {

	public partial class TileView {

		public partial class Tile {
			/// <summary>
			/// An <see cref="EventArgs"/> which allows passing a cancelable new <see cref="Title"/> value event.
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
				/// Flag which allows cancelling the Title change.
				/// </summary>
				public bool Cancel { get; set; }

				/// <summary>
				/// Initializes a new instance of <see cref="TitleEventArgs"/>
				/// </summary>
				/// <param name="oldTitle">The <see cref="Title"/> that is/has been replaced.</param>
				/// <param name="newTitle">The new <see cref="Title"/> to be replaced.</param>
				public TitleEventArgs (ustring oldTitle, ustring newTitle)
				{
					OldTitle = oldTitle;
					NewTitle = newTitle;
				}
			}
		}

	}
}
