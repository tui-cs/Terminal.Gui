using System.Collections.Generic;
using Rune = System.Rune;

namespace Terminal.Gui {
	/// <summary>
	/// Describes the current state of a <see cref="View"/> which
	/// is proposing autocomplete. Suggestions are based on this state.
	/// </summary>
	public class AutocompleteContext
	{
		/// <summary>
		/// The text on the current line.
		/// </summary>
		public List<Rune> CurrentLine { get; set; }

		/// <summary>
		/// The position of the input cursor within the <see cref="CurrentLine"/>.
		/// </summary>
		public int CursorPosition { get; set; }

		/// <summary>
		/// Creates anew instance of the <see cref="AutocompleteContext"/> class
		/// </summary>
		public AutocompleteContext (List<Rune> currentLine, int cursorPosition)
		{
			CurrentLine = currentLine;
			CursorPosition = cursorPosition;
		}
	}
}

