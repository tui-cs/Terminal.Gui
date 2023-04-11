using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rune = System.Rune;

namespace Terminal.Gui {
	
	/// <summary>
	/// <see cref="ISuggestionGenerator"/> which suggests from a collection
	/// of words those that match the <see cref="AutocompleteContext"/>. You
	/// can update <see cref="AllSuggestions"/> at any time to change candidates
	/// considered for autocomplete.
	/// </summary>
	public class SingleWordSuggestionGenerator : ISuggestionGenerator {

		/// <summary>
		/// The full set of all strings that can be suggested.
		/// </summary>
		/// <returns></returns>
		public virtual List<string> AllSuggestions { get; set; } = new List<string> ();

		/// <inheritdoc/>
		public IEnumerable<Suggestion> GenerateSuggestions (AutocompleteContext context)
		{
			// if there is nothing to pick from
			if (AllSuggestions.Count == 0) {
				return Enumerable.Empty<Suggestion> ();
			}

			var currentWord = IdxToWord (context.CurrentLine, context.CursorPosition);

			if (string.IsNullOrWhiteSpace (currentWord)) {
				return Enumerable.Empty<Suggestion> ();
			} else {
				return AllSuggestions.Where (o =>
				o.StartsWith (currentWord, StringComparison.CurrentCultureIgnoreCase) &&
				!o.Equals (currentWord, StringComparison.CurrentCultureIgnoreCase)
				).Select (o => new Suggestion (currentWord.Length, o))
					.ToList ().AsReadOnly ();

			}
		}

		/// <summary>
		/// Return true if the given symbol should be considered part of a word
		/// and can be contained in matches. Base behavior is to use <see cref="char.IsLetterOrDigit(char)"/>
		/// </summary>
		/// <param name="rune"></param>
		/// <returns></returns>
		public virtual bool IsWordChar (Rune rune)
		{
			return Char.IsLetterOrDigit ((char)rune);
		}


		/// <summary>
		/// <para>
		/// Given a <paramref name="line"/> of characters, returns the word which ends at <paramref name="idx"/> 
		/// or null. Also returns null if the <paramref name="idx"/> is positioned in the middle of a word.
		/// </para>
		/// 
		/// <para>
		/// Use this method to determine whether autocomplete should be shown when the cursor is at
		/// a given point in a line and to get the word from which suggestions should be generated.
		/// Use the <paramref name="columnOffset"/> to indicate if search the word at left (negative),
		/// at right (positive) or at the current column (zero) which is the default.
		/// </para>
		/// </summary>
		/// <param name="line"></param>
		/// <param name="idx"></param>
		/// <param name="columnOffset"></param>
		/// <returns></returns>
		protected virtual string IdxToWord (List<Rune> line, int idx, int columnOffset = 0)
		{
			StringBuilder sb = new StringBuilder ();
			var endIdx = idx;

			// get the ending word index
			while (endIdx < line.Count) {
				if (IsWordChar (line [endIdx])) {
					endIdx++;
				} else {
					break;
				}
			}

			// It isn't a word char then there is no way to autocomplete that word
			if (endIdx == idx && columnOffset != 0) {
				return null;
			}

			// we are at the end of a word. Work out what has been typed so far
			while (endIdx-- > 0) {
				if (IsWordChar (line [endIdx])) {
					sb.Insert (0, (char)line [endIdx]);
				} else {
					break;
				}
			}
			return sb.ToString ();
		}
	}
}

