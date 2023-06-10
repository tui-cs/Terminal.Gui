//
// TextField.cs: single-line text editor with Emacs keybindings
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//

using System;
using System.Text;

namespace Terminal.Gui {
	/// <summary>
	/// Event args for the <see cref="TextField.TextChanged"/> event
	/// </summary>
	public class TextChangedEventArgs : EventArgs {

		/// <summary>
		/// Creates a new instance of the <see cref="TextChangedEventArgs"/> class
		/// </summary>
		/// <param name="oldValue"></param>
		public TextChangedEventArgs (string oldValue)
		{
			OldValue = oldValue;
		}

		/// <summary>
		/// The old value before the text changed
		/// </summary>
		public string OldValue { get; }
	}
}
