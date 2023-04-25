using NStack;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.InteropServices;

namespace Terminal.Gui {
	internal class AutocompleteFilepathContext : AutocompleteContext {
		public FileDialogState State { get; set; }

		public AutocompleteFilepathContext (ustring currentLine, int cursorPosition, FileDialogState state)
			: base (currentLine.ToRuneList (), cursorPosition)
		{
			this.State = state;
		}
	}

	internal class FilepathSuggestionGenerator : ISuggestionGenerator {

		FileDialogState state;
		public IEnumerable<Suggestion> GenerateSuggestions (AutocompleteContext context)
		{
			if (context is AutocompleteFilepathContext fileState) {
				this.state = fileState.State;
			}

			if (state == null) {
				return Enumerable.Empty<Suggestion> ();
			}

			var path = ustring.Make (context.CurrentLine).ToString ();
			var last = path.LastIndexOfAny (FileDialog.Separators);
			
			if(string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path)) {
				return Enumerable.Empty<Suggestion> ();
			}

			var term = path.Substring (last + 1);
			
			// If path is /tmp/ then don't just list everything in it
			if(string.IsNullOrWhiteSpace(term))
			{
				return Enumerable.Empty<Suggestion> ();
			}

			if (term.Equals (state?.Directory?.Name)) {
				// Clear suggestions
				return Enumerable.Empty<Suggestion> ();
			}

			bool isWindows = RuntimeInformation.IsOSPlatform (OSPlatform.Windows);

			var suggestions = state.Children.Where(d=> !d.IsParent).Select (
				e => e.FileSystemInfo is IDirectoryInfo d
					? d.Name + System.IO.Path.DirectorySeparatorChar
					: e.FileSystemInfo.Name)
				.ToArray ();

			var validSuggestions = suggestions
				.Where (s => s.StartsWith (term, isWindows ?
					StringComparison.InvariantCultureIgnoreCase :
					StringComparison.InvariantCulture))
				.OrderBy (m => m.Length)
				.ToArray ();

			// nothing to suggest
			if (validSuggestions.Length == 0 || validSuggestions [0].Length == term.Length) {
				return Enumerable.Empty<Suggestion> ();
			}

			return validSuggestions.Select (
				f => new Suggestion (term.Length, f, f)).ToList ();
		}

		public bool IsWordChar (Rune rune)
		{
			if (rune == '\n') {
				return false;
			}

			return true;
		}

	}
}