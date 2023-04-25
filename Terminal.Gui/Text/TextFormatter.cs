using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using NStack;
using Rune = System.Rune;

namespace Terminal.Gui {
	/// <summary>
	/// Text alignment enumeration, controls how text is displayed.
	/// </summary>
	public enum TextAlignment {
		/// <summary>
		/// Aligns the text to the left of the frame.
		/// </summary>
		Left,
		/// <summary>
		/// Aligns the text to the right side of the frame.
		/// </summary>
		Right,
		/// <summary>
		/// Centers the text in the frame.
		/// </summary>
		Centered,
		/// <summary>
		/// Shows the text as justified text in the frame.
		/// </summary>
		Justified
	}

	/// <summary>
	/// Vertical text alignment enumeration, controls how text is displayed.
	/// </summary>
	public enum VerticalTextAlignment {
		/// <summary>
		/// Aligns the text to the top of the frame.
		/// </summary>
		Top,
		/// <summary>
		/// Aligns the text to the bottom of the frame.
		/// </summary>
		Bottom,
		/// <summary>
		/// Centers the text verticaly in the frame.
		/// </summary>
		Middle,
		/// <summary>
		/// Shows the text as justified text in the frame.
		/// </summary>
		Justified
	}

	/// TextDirection  [H] = Horizontal  [V] = Vertical
	/// =============
	/// LeftRight_TopBottom [H] Normal
	/// TopBottom_LeftRight [V] Normal
	/// 
	/// RightLeft_TopBottom [H] Invert Text
	/// TopBottom_RightLeft [V] Invert Lines
	/// 
	/// LeftRight_BottomTop [H] Invert Lines
	/// BottomTop_LeftRight [V] Invert Text
	/// 
	/// RightLeft_BottomTop [H] Invert Text + Invert Lines
	/// BottomTop_RightLeft [V] Invert Text + Invert Lines
	///
	/// <summary>
	/// Text direction enumeration, controls how text is displayed.
	/// </summary>
	public enum TextDirection {
		/// <summary>
		/// Normal horizontal direction.
		/// <code>HELLO<br/>WORLD</code>
		/// </summary>
		LeftRight_TopBottom,
		/// <summary>
		/// Normal vertical direction.
		/// <code>H W<br/>E O<br/>L R<br/>L L<br/>O D</code>
		/// </summary>
		TopBottom_LeftRight,
		/// <summary>
		/// This is a horizontal direction. <br/> RTL
		/// <code>OLLEH<br/>DLROW</code>
		/// </summary>
		RightLeft_TopBottom,
		/// <summary>
		/// This is a vertical direction.
		/// <code>W H<br/>O E<br/>R L<br/>L L<br/>D O</code>
		/// </summary>
		TopBottom_RightLeft,
		/// <summary>
		/// This is a horizontal direction.
		/// <code>WORLD<br/>HELLO</code>
		/// </summary>
		LeftRight_BottomTop,
		/// <summary>
		/// This is a vertical direction.
		/// <code>O D<br/>L L<br/>L R<br/>E O<br/>H W</code>
		/// </summary>
		BottomTop_LeftRight,
		/// <summary>
		/// This is a horizontal direction.
		/// <code>DLROW<br/>OLLEH</code>
		/// </summary>
		RightLeft_BottomTop,
		/// <summary>
		/// This is a vertical direction.
		/// <code>D O<br/>L L<br/>R L<br/>O E<br/>W H</code>
		/// </summary>
		BottomTop_RightLeft
	}

	/// <summary>
	/// Provides text formatting capabilities for console apps. Supports, hotkeys, horizontal alignment, multiple lines, and word-based line wrap.
	/// </summary>
	public class TextFormatter {

		#region Static Members

		static ustring StripCRLF (ustring str, bool keepNewLine = false)
		{
			var runes = str.ToRuneList ();
			for (int i = 0; i < runes.Count; i++) {
				switch (runes [i]) {
				case '\n':
					if (!keepNewLine) {
						runes.RemoveAt (i);
					}
					break;

				case '\r':
					if ((i + 1) < runes.Count && runes [i + 1] == '\n') {
						runes.RemoveAt (i);
						if (!keepNewLine) {
							runes.RemoveAt (i);
						}
						i++;
					} else {
						if (!keepNewLine) {
							runes.RemoveAt (i);
						}
					}
					break;
				}
			}
			return ustring.Make (runes);
		}
		static ustring ReplaceCRLFWithSpace (ustring str)
		{
			var runes = str.ToRuneList ();
			for (int i = 0; i < runes.Count; i++) {
				switch (runes [i]) {
				case '\n':
					runes [i] = (Rune)' ';
					break;

				case '\r':
					if ((i + 1) < runes.Count && runes [i + 1] == '\n') {
						runes [i] = (Rune)' ';
						runes.RemoveAt (i + 1);
						i++;
					} else {
						runes [i] = (Rune)' ';
					}
					break;
				}
			}
			return ustring.Make (runes);
		}

		/// <summary>
		/// Splits all newlines in the <paramref name="text"/> into a list
		/// and supports both CRLF and LF, preserving the ending newline.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <returns>A list of text without the newline characters.</returns>
		public static List<ustring> SplitNewLine (ustring text)
		{
			var runes = text.ToRuneList ();
			var lines = new List<ustring> ();
			var start = 0;
			var end = 0;

			for (int i = 0; i < runes.Count; i++) {
				end = i;
				switch (runes [i]) {
				case '\n':
					lines.Add (ustring.Make (runes.GetRange (start, end - start)));
					i++;
					start = i;
					break;

				case '\r':
					if ((i + 1) < runes.Count && runes [i + 1] == '\n') {
						lines.Add (ustring.Make (runes.GetRange (start, end - start)));
						i += 2;
						start = i;
					} else {
						lines.Add (ustring.Make (runes.GetRange (start, end - start)));
						i++;
						start = i;
					}
					break;
				}
			}
			if (runes.Count > 0 && lines.Count == 0) {
				lines.Add (ustring.Make (runes));
			} else if (runes.Count > 0 && start < runes.Count) {
				lines.Add (ustring.Make (runes.GetRange (start, runes.Count - start)));
			} else {
				lines.Add (ustring.Make (""));
			}
			return lines;
		}

		/// <summary>
		/// Adds trailing whitespace or truncates <paramref name="text"/>
		/// so that it fits exactly <paramref name="width"/> console units.
		/// Note that some unicode characters take 2+ columns
		/// </summary>
		/// <param name="text"></param>
		/// <param name="width"></param>
		/// <returns></returns>
		public static string ClipOrPad (string text, int width)
		{
			if (string.IsNullOrEmpty (text))
				return text;

			// if value is not wide enough
			if (text.Sum (c => Rune.ColumnWidth (c)) < width) {

				// pad it out with spaces to the given alignment
				int toPad = width - (text.Sum (c => Rune.ColumnWidth (c)));

				return text + new string (' ', toPad);
			}

			// value is too wide
			return new string (text.TakeWhile (c => (width -= Rune.ColumnWidth (c)) >= 0).ToArray ());
		}

		/// <summary>
		/// Formats the provided text to fit within the width provided using word wrapping.
		/// </summary>
		/// <param name="text">The text to word wrap</param>
		/// <param name="width">The number of columns to constrain the text to</param>
		/// <param name="preserveTrailingSpaces">If <see langword="true"/> trailing spaces at the end of wrapped lines will be preserved.
		///  If <see langword="false"/>, trailing spaces at the end of wrapped lines will be trimmed.</param>
		/// <param name="tabWidth">The number of columns used for a tab.</param>
		/// <param name="textDirection">The text direction.</param>
		/// <returns>A list of word wrapped lines.</returns>
		/// <remarks>
		/// <para>
		/// This method does not do any justification.
		/// </para>
		/// <para>
		/// This method strips Newline ('\n' and '\r\n') sequences before processing.
		/// </para>
		/// <para>
		/// If <paramref name="preserveTrailingSpaces"/> is <see langword="false"/> at most one space will be preserved at the end of the last line.
		/// </para>
		/// </remarks>
		public static List<ustring> WordWrapText (ustring text, int width, bool preserveTrailingSpaces = false, int tabWidth = 0,
			TextDirection textDirection = TextDirection.LeftRight_TopBottom)
		{
			if (width < 0) {
				throw new ArgumentOutOfRangeException ("Width cannot be negative.");
			}

			int start = 0, end;
			var lines = new List<ustring> ();

			if (ustring.IsNullOrEmpty (text)) {
				return lines;
			}

			var runes = StripCRLF (text).ToRuneList ();
			if (preserveTrailingSpaces) {
				while ((end = start) < runes.Count) {
					end = GetNextWhiteSpace (start, width, out bool incomplete);
					if (end == 0 && incomplete) {
						start = text.RuneCount;
						break;
					}
					lines.Add (ustring.Make (runes.GetRange (start, end - start)));
					start = end;
					if (incomplete) {
						start = text.RuneCount;
						break;
					}
				}
			} else {
				if (IsHorizontalDirection (textDirection)) {
					//if (GetLengthThatFits (runes.GetRange (start, runes.Count - start), width) > 0) {
					//	// while there's still runes left and end is not past end...
					//	while (start < runes.Count &&
					//		(end = start + Math.Max (GetLengthThatFits (runes.GetRange (start, runes.Count - start), width) - 1, 0)) < runes.Count) {
					//		// end now points to start + LengthThatFits
					//		// Walk back over trailing spaces
					//		while (runes [end] == ' ' && end > start) {
					//			end--;
					//		}
					//		// end now points to start + LengthThatFits - any trailing spaces; start saving new line
					//		var line = runes.GetRange (start, end - start + 1);

					//		if (end == start && width > 1) {
					//			// it was all trailing spaces; now walk forward to next non-space
					//			do {
					//				start++;
					//			} while (start < runes.Count && runes [start] == ' ');

					//			// start now points to first non-space we haven't seen yet or we're done
					//			if (start < runes.Count) {
					//				// we're not done. we have remaining = width - line.Count columns left; 
					//				var remaining = width - line.Count;
					//				if (remaining > 1) {
					//					// add a space for all the spaces we walked over 
					//					line.Add (' ');
					//				}
					//				var count = GetLengthThatFits (runes.GetRange (start, runes.Count - start), width - line.Count);

					//				// [start..count] now has rest of line
					//				line.AddRange (runes.GetRange (start, count));
					//				start += count;
					//			}
					//		} else {
					//			start += line.Count;
					//		}

					//		//// if the previous line was just a ' ' and the new line is just a ' '
					//		//// don't add new line
					//		//if (line [0] == ' ' && (lines.Count > 0 && lines [lines.Count - 1] [0] == ' ')) {
					//		//} else {
					//		//}
					//		lines.Add (ustring.Make (line));

					//		// move forward to next non-space
					//		while (width > 1 && start < runes.Count && runes [start] == ' ') {
					//			start++;
					//		}
					//	}
					//}

					while ((end = start + Math.Max (GetLengthThatFits (runes.GetRange (start, runes.Count - start), width), 1)) < runes.Count) {
						while (runes [end] != ' ' && end > start)
							end--;
						if (end == start)
							end = start + GetLengthThatFits (runes.GetRange (end, runes.Count - end), width);
						lines.Add (ustring.Make (runes.GetRange (start, end - start)));
						start = end;
						if (runes [end] == ' ') {
							start++;
						}
					}
					
				} else {
					while ((end = start + width) < runes.Count) {
						while (runes [end] != ' ' && end > start) {
							end--;
						}
						if (end == start) {
							end = start + width;
						}
						lines.Add (ustring.Make (runes.GetRange (start, end - start)));
						start = end;
						if (runes [end] == ' ') {
							start++;
						}
					}
				}
			}

			int GetNextWhiteSpace (int from, int cWidth, out bool incomplete, int cLength = 0)
			{
				var lastFrom = from;
				var to = from;
				var length = cLength;
				incomplete = false;

				while (length < cWidth && to < runes.Count) {
					var rune = runes [to];
					if (IsHorizontalDirection (textDirection)) {
						length += Rune.ColumnWidth (rune);
					} else {
						length++;
					}
					if (length > cWidth) {
						if (to >= runes.Count || (length > 1 && cWidth <= 1)) {
							incomplete = true;
						}
						return to;
					}
					if (rune == ' ') {
						if (length == cWidth) {
							return to + 1;
						} else if (length > cWidth) {
							return to;
						} else {
							return GetNextWhiteSpace (to + 1, cWidth, out incomplete, length);
						}
					} else if (rune == '\t') {
						length += tabWidth + 1;
						if (length == tabWidth && tabWidth > cWidth) {
							return to + 1;
						} else if (length > cWidth && tabWidth > cWidth) {
							return to;
						} else {
							return GetNextWhiteSpace (to + 1, cWidth, out incomplete, length);
						}
					}
					to++;
				}
				if (cLength > 0 && to < runes.Count && runes [to] != ' ' && runes [to] != '\t') {
					return from;
				} else if (cLength > 0 && to < runes.Count && (runes [to] == ' ' || runes [to] == '\t')) {
					return lastFrom;
				} else {
					return to;
				}
			}

			if (start < text.RuneCount) {
				lines.Add (ustring.Make (runes.GetRange (start, runes.Count - start)));
			}

			return lines;
		}

		/// <summary>
		/// Justifies text within a specified width. 
		/// </summary>
		/// <param name="text">The text to justify.</param>
		/// <param name="width">The number of columns to clip the text to. Text longer than <paramref name="width"/> will be clipped.</param>
		/// <param name="talign">Alignment.</param>
		/// <param name="textDirection">The text direction.</param>
		/// <returns>Justified and clipped text.</returns>
		public static ustring ClipAndJustify (ustring text, int width, TextAlignment talign, TextDirection textDirection = TextDirection.LeftRight_TopBottom)
		{
			return ClipAndJustify (text, width, talign == TextAlignment.Justified, textDirection);
		}

		/// <summary>
		/// Justifies text within a specified width. 
		/// </summary>
		/// <param name="text">The text to justify.</param>
		/// <param name="width">The number of columns to clip the text to. Text longer than <paramref name="width"/> will be clipped.</param>
		/// <param name="justify">Justify.</param>
		/// <param name="textDirection">The text direction.</param>
		/// <returns>Justified and clipped text.</returns>
		public static ustring ClipAndJustify (ustring text, int width, bool justify, TextDirection textDirection = TextDirection.LeftRight_TopBottom)
		{
			if (width < 0) {
				throw new ArgumentOutOfRangeException ("Width cannot be negative.");
			}
			if (ustring.IsNullOrEmpty (text)) {
				return text;
			}

			var runes = text.ToRuneList ();
			int slen = runes.Count;
			if (slen > width) {
				if (IsHorizontalDirection (textDirection)) {
					return ustring.Make (runes.GetRange (0, GetLengthThatFits (text, width)));
				} else {
					return ustring.Make (runes.GetRange (0, width));
				}
			} else {
				if (justify) {
					return Justify (text, width, ' ', textDirection);
				} else if (IsHorizontalDirection (textDirection) && GetTextWidth (text) > width) {
					return ustring.Make (runes.GetRange (0, GetLengthThatFits (text, width)));
				}
				return text;
			}
		}

		/// <summary>
		/// Justifies the text to fill the width provided. Space will be added between words (demarked by spaces and tabs) to
		/// make the text just fit <c>width</c>. Spaces will not be added to the ends.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="width"></param>
		/// <param name="spaceChar">Character to replace whitespace and pad with. For debugging purposes.</param>
		/// <param name="textDirection">The text direction.</param>
		/// <returns>The justified text.</returns>
		public static ustring Justify (ustring text, int width, char spaceChar = ' ', TextDirection textDirection = TextDirection.LeftRight_TopBottom)
		{
			if (width < 0) {
				throw new ArgumentOutOfRangeException ("Width cannot be negative.");
			}
			if (ustring.IsNullOrEmpty (text)) {
				return text;
			}

			var words = text.Split (ustring.Make (' '));
			int textCount;
			if (IsHorizontalDirection (textDirection)) {
				textCount = words.Sum (arg => GetTextWidth (arg));
			} else {
				textCount = words.Sum (arg => arg.RuneCount);
			}
			var spaces = words.Length > 1 ? (width - textCount) / (words.Length - 1) : 0;
			var extras = words.Length > 1 ? (width - textCount) % (words.Length - 1) : 0;

			var s = new System.Text.StringBuilder ();
			for (int w = 0; w < words.Length; w++) {
				var x = words [w];
				s.Append (x);
				if (w + 1 < words.Length)
					for (int i = 0; i < spaces; i++)
						s.Append (spaceChar);
				if (extras > 0) {
					for (int i = 0; i < 1; i++)
						s.Append (spaceChar);
					extras--;
				}
				if (w + 1 == words.Length - 1) {
					for (int i = 0; i < extras; i++)
						s.Append (spaceChar);
				}
			}
			return ustring.Make (s.ToString ());
		}

		static char [] whitespace = new char [] { ' ', '\t' };

		/// <summary>
		/// Reformats text into lines, applying text alignment and optionally wrapping text to new lines on word boundaries.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="width">The number of columns to constrain the text to for word wrapping and clipping.</param>
		/// <param name="talign">Specifies how the text will be aligned horizontally.</param>
		/// <param name="wordWrap">If <see langword="true"/>, the text will be wrapped to new lines no longer than <paramref name="width"/>.	
		/// If <see langword="false"/>, forces text to fit a single line. Line breaks are converted to spaces. The text will be clipped to <paramref name="width"/>.</param>
		/// <param name="preserveTrailingSpaces">If <see langword="true"/> trailing spaces at the end of wrapped lines will be preserved.
		///  If <see langword="false"/>, trailing spaces at the end of wrapped lines will be trimmed.</param>
		/// <param name="tabWidth">The number of columns used for a tab.</param>
		/// <param name="textDirection">The text direction.</param>
		/// <returns>A list of word wrapped lines.</returns>
		/// <remarks>
		/// <para>
		/// An empty <paramref name="text"/> string will result in one empty line.
		/// </para>
		/// <para>
		/// If <paramref name="width"/> is 0, a single, empty line will be returned.
		/// </para>
		/// <para>
		/// If <paramref name="width"/> is int.MaxValue, the text will be formatted to the maximum width possible. 
		/// </para>
		/// </remarks>
		public static List<ustring> Format (ustring text, int width, TextAlignment talign, bool wordWrap, bool preserveTrailingSpaces = false, int tabWidth = 0, TextDirection textDirection = TextDirection.LeftRight_TopBottom)
		{
			return Format (text, width, talign == TextAlignment.Justified, wordWrap, preserveTrailingSpaces, tabWidth, textDirection);
		}

		/// <summary>
		/// Reformats text into lines, applying text alignment and optionally wrapping text to new lines on word boundaries.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="width">The number of columns to constrain the text to for word wrapping and clipping.</param>
		/// <param name="justify">Specifies whether the text should be justified.</param>
		/// <param name="wordWrap">If <see langword="true"/>, the text will be wrapped to new lines no longer than <paramref name="width"/>.	
		/// If <see langword="false"/>, forces text to fit a single line. Line breaks are converted to spaces. The text will be clipped to <paramref name="width"/>.</param>
		/// <param name="preserveTrailingSpaces">If <see langword="true"/> trailing spaces at the end of wrapped lines will be preserved.
		///  If <see langword="false"/>, trailing spaces at the end of wrapped lines will be trimmed.</param>
		/// <param name="tabWidth">The number of columns used for a tab.</param>
		/// <param name="textDirection">The text direction.</param>
		/// <returns>A list of word wrapped lines.</returns>
		/// <remarks>
		/// <para>
		/// An empty <paramref name="text"/> string will result in one empty line.
		/// </para>
		/// <para>
		/// If <paramref name="width"/> is 0, a single, empty line will be returned.
		/// </para>
		/// <para>
		/// If <paramref name="width"/> is int.MaxValue, the text will be formatted to the maximum width possible. 
		/// </para>
		/// </remarks>
		public static List<ustring> Format (ustring text, int width, bool justify, bool wordWrap,
			bool preserveTrailingSpaces = false, int tabWidth = 0, TextDirection textDirection = TextDirection.LeftRight_TopBottom)
		{
			if (width < 0) {
				throw new ArgumentOutOfRangeException ("width cannot be negative");
			}
			List<ustring> lineResult = new List<ustring> ();

			if (ustring.IsNullOrEmpty (text) || width == 0) {
				lineResult.Add (ustring.Empty);
				return lineResult;
			}

			if (wordWrap == false) {
				text = ReplaceCRLFWithSpace (text);
				lineResult.Add (ClipAndJustify (text, width, justify, textDirection));
				return lineResult;
			}

			var runes = StripCRLF (text, true).ToRuneList ();
			int runeCount = runes.Count;
			int lp = 0;
			for (int i = 0; i < runeCount; i++) {
				Rune c = runes [i];
				if (c == '\n') {
					var wrappedLines = WordWrapText (ustring.Make (runes.GetRange (lp, i - lp)), width, preserveTrailingSpaces, tabWidth, textDirection);
					foreach (var line in wrappedLines) {
						lineResult.Add (ClipAndJustify (line, width, justify, textDirection));
					}
					if (wrappedLines.Count == 0) {
						lineResult.Add (ustring.Empty);
					}
					lp = i + 1;
				}
			}
			foreach (var line in WordWrapText (ustring.Make (runes.GetRange (lp, runeCount - lp)), width, preserveTrailingSpaces, tabWidth, textDirection)) {
				lineResult.Add (ClipAndJustify (line, width, justify, textDirection));
			}

			return lineResult;
		}

		/// <summary>
		/// Computes the number of lines needed to render the specified text given the width.
		/// </summary>
		/// <returns>Number of lines.</returns>
		/// <param name="text">Text, may contain newlines.</param>
		/// <param name="width">The minimum width for the text.</param>
		public static int MaxLines (ustring text, int width)
		{
			var result = TextFormatter.Format (text, width, false, true);
			return result.Count;
		}

		/// <summary>
		/// Computes the maximum width needed to render the text (single line or multiple lines, word wrapped) given 
		/// a number of columns to constrain the text to.
		/// </summary>
		/// <returns>Width of the longest line after formatting the text constrained by <paramref name="maxColumns"/>.</returns>
		/// <param name="text">Text, may contain newlines.</param>
		/// <param name="maxColumns">The number of columns to constrain the text to for formatting.</param>
		public static int MaxWidth (ustring text, int maxColumns)
		{
			var result = TextFormatter.Format (text: text, width: maxColumns, justify: false, wordWrap: true);
			var max = 0;
			result.ForEach (s => {
				var m = 0;
				s.ToRuneList ().ForEach (r => m += Math.Max (Rune.ColumnWidth (r), 1));
				if (m > max) {
					max = m;
				}
			});
			return max;
		}

		/// <summary>
		/// Returns the width of the widest line in the text, accounting for wide-glyphs (uses <see cref="ustring.ConsoleWidth"/>).
		/// <paramref name="text"/> if it contains newlines.
		/// </summary>
		/// <param name="text">Text, may contain newlines.</param>
		/// <returns>The length of the longest line.</returns>
		public static int MaxWidthLine (ustring text)
		{
			var result = TextFormatter.SplitNewLine (text);
			return result.Max (x => x.ConsoleWidth);
		}

		/// <summary>
		/// Gets the number of columns the passed text will use, ignoring newlines and accounting for wide-glyphs (uses <see cref="ustring.ConsoleWidth"/>).
		/// </summary>
		/// <param name="text"></param>
		/// <returns>The text width.</returns>
		public static int GetTextWidth (ustring text)
		{
			return text.ToRuneList ().Sum (r => Math.Max (Rune.ColumnWidth (r), 1));
		}

		/// <summary>
		/// Gets the maximum characters width from the list based on the <paramref name="startIndex"/>
		/// and the <paramref name="length"/>.
		/// </summary>
		/// <param name="lines">The lines.</param>
		/// <param name="startIndex">The start index.</param>
		/// <param name="length">The length.</param>
		/// <returns>The maximum characters width.</returns>
		public static int GetSumMaxCharWidth (List<ustring> lines, int startIndex = -1, int length = -1)
		{
			var max = 0;
			for (int i = (startIndex == -1 ? 0 : startIndex); i < (length == -1 ? lines.Count : startIndex + length); i++) {
				var runes = lines [i];
				if (runes.Length > 0)
					max += runes.Max (r => Math.Max (Rune.ColumnWidth (r), 1));
			}
			return max;
		}

		/// <summary>
		/// Gets the maximum characters width from the text based on the <paramref name="startIndex"/>
		/// and the <paramref name="length"/>.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <param name="startIndex">The start index.</param>
		/// <param name="length">The length.</param>
		/// <returns>The maximum characters width.</returns>
		public static int GetSumMaxCharWidth (ustring text, int startIndex = -1, int length = -1)
		{
			var max = 0;
			var runes = text.ToRunes ();
			for (int i = (startIndex == -1 ? 0 : startIndex); i < (length == -1 ? runes.Length : startIndex + length); i++) {
				max += Math.Max (Rune.ColumnWidth (runes [i]), 1);
			}
			return max;
		}

		/// <summary>
		/// Gets the number of the Runes in a <see cref="ustring"/> that will fit in <paramref name="columns"/>.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <param name="columns">The width.</param>
		/// <returns>The index of the text that fit the width.</returns>
		public static int GetLengthThatFits (ustring text, int columns) => GetLengthThatFits (text?.ToRuneList (), columns);

		/// <summary>
		/// Gets the number of the Runes in a list of Runes that will fit in <paramref name="columns"/>.
		/// </summary>
		/// <param name="runes">The list of runes.</param>
		/// <param name="columns">The width.</param>
		/// <returns>The index of the last Rune in <paramref name="runes"/> that fit in <paramref name="columns"/>.</returns>
		public static int GetLengthThatFits (List<Rune> runes, int columns)
		{
			if (runes == null || runes.Count == 0) {
				return 0;
			}

			var runesLength = 0;
			var runeIdx = 0;
			for (; runeIdx < runes.Count; runeIdx++) {
				var runeWidth = Math.Max (Rune.ColumnWidth (runes [runeIdx]), 1);
				if (runesLength + runeWidth > columns) {
					break;
				}
				runesLength += runeWidth;
			}
			return runeIdx;
		}

		/// <summary>
		/// Gets the index position from the list based on the <paramref name="width"/>.
		/// </summary>
		/// <param name="lines">The lines.</param>
		/// <param name="width">The width.</param>
		/// <returns>The index of the list that fit the width.</returns>
		public static int GetMaxColsForWidth (List<ustring> lines, int width)
		{
			var runesLength = 0;
			var lineIdx = 0;
			for (; lineIdx < lines.Count; lineIdx++) {
				var runes = lines [lineIdx].ToRuneList ();
				var maxRruneWidth = runes.Count > 0
					? runes.Max (r => Math.Max (Rune.ColumnWidth (r), 1)) : 1;
				if (runesLength + maxRruneWidth > width) {
					break;
				}
				runesLength += maxRruneWidth;
			}
			return lineIdx;
		}

		/// <summary>
		///  Calculates the rectangle required to hold text, assuming no word wrapping or justification.
		/// </summary>
		/// <param name="x">The x location of the rectangle</param>
		/// <param name="y">The y location of the rectangle</param>
		/// <param name="text">The text to measure</param>
		/// <param name="direction">The text direction.</param>
		/// <returns></returns>
		public static Rect CalcRect (int x, int y, ustring text, TextDirection direction = TextDirection.LeftRight_TopBottom)
		{
			if (ustring.IsNullOrEmpty (text)) {
				return new Rect (new Point (x, y), Size.Empty);
			}

			int w, h;

			if (IsHorizontalDirection (direction)) {
				int mw = 0;
				int ml = 1;

				int cols = 0;
				foreach (var rune in text) {
					if (rune == '\n') {
						ml++;
						if (cols > mw) {
							mw = cols;
						}
						cols = 0;
					} else if (rune != '\r') {
						cols++;
						var rw = Rune.ColumnWidth (rune);
						if (rw > 0) {
							rw--;
						}
						cols += rw;
					}
				}
				if (cols > mw) {
					mw = cols;
				}
				w = mw;
				h = ml;
			} else {
				int vw = 1, cw = 1;
				int vh = 0;

				int rows = 0;
				foreach (var rune in text) {
					if (rune == '\n') {
						vw++;
						if (rows > vh) {
							vh = rows;
						}
						rows = 0;
						cw = 1;
					} else if (rune != '\r') {
						rows++;
						var rw = Rune.ColumnWidth (rune);
						if (cw < rw) {
							cw = rw;
							vw++;
						}
					}
				}
				if (rows > vh) {
					vh = rows;
				}
				w = vw;
				h = vh;
			}

			return new Rect (x, y, w, h);
		}

		/// <summary>
		/// Finds the hotkey and its location in text. 
		/// </summary>
		/// <param name="text">The text to look in.</param>
		/// <param name="hotKeySpecifier">The hotkey specifier (e.g. '_') to look for.</param>
		/// <param name="firstUpperCase">If <c>true</c> the legacy behavior of identifying the first upper case character as the hotkey will be enabled.
		/// Regardless of the value of this parameter, <c>hotKeySpecifier</c> takes precedence.</param>
		/// <param name="hotPos">Outputs the Rune index into <c>text</c>.</param>
		/// <param name="hotKey">Outputs the hotKey.</param>
		/// <returns><c>true</c> if a hotkey was found; <c>false</c> otherwise.</returns>
		public static bool FindHotKey (ustring text, Rune hotKeySpecifier, bool firstUpperCase, out int hotPos, out Key hotKey)
		{
			if (ustring.IsNullOrEmpty (text) || hotKeySpecifier == (Rune)0xFFFF) {
				hotPos = -1;
				hotKey = Key.Unknown;
				return false;
			}

			Rune hot_key = (Rune)0;
			int hot_pos = -1;

			// Use first hot_key char passed into 'hotKey'.
			// TODO: Ignore hot_key of two are provided
			// TODO: Do not support non-alphanumeric chars that can't be typed
			int i = 0;
			foreach (Rune c in text) {
				if ((char)c != 0xFFFD) {
					if (c == hotKeySpecifier) {
						hot_pos = i;
					} else if (hot_pos > -1) {
						hot_key = c;
						break;
					}
				}
				i++;
			}

			// Legacy support - use first upper case char if the specifier was not found
			if (hot_pos == -1 && firstUpperCase) {
				i = 0;
				foreach (Rune c in text) {
					if ((char)c != 0xFFFD) {
						if (Rune.IsUpper (c)) {
							hot_key = c;
							hot_pos = i;
							break;
						}
					}
					i++;
				}
			}

			if (hot_key != (Rune)0 && hot_pos != -1) {
				hotPos = hot_pos;

				if (hot_key.IsValid && char.IsLetterOrDigit ((char)hot_key)) {
					hotKey = (Key)char.ToUpperInvariant ((char)hot_key);
					return true;
				}
			}

			hotPos = -1;
			hotKey = Key.Unknown;
			return false;
		}

		/// <summary>
		/// Replaces the Rune at the index specified by the <c>hotPos</c> parameter with a tag identifying 
		/// it as the hotkey.
		/// </summary>
		/// <param name="text">The text to tag the hotkey in.</param>
		/// <param name="hotPos">The Rune index of the hotkey in <c>text</c>.</param>
		/// <returns>The text with the hotkey tagged.</returns>
		/// <remarks>
		/// The returned string will not render correctly without first un-doing the tag. To undo the tag, search for 
		/// </remarks>
		public ustring ReplaceHotKeyWithTag (ustring text, int hotPos)
		{
			// Set the high bit
			var runes = text.ToRuneList ();
			if (Rune.IsLetterOrNumber (runes [hotPos])) {
				runes [hotPos] = new Rune ((uint)runes [hotPos]);
			}
			return ustring.Make (runes);
		}

		/// <summary>
		/// Removes the hotkey specifier from text.
		/// </summary>
		/// <param name="text">The text to manipulate.</param>
		/// <param name="hotKeySpecifier">The hot-key specifier (e.g. '_') to look for.</param>
		/// <param name="hotPos">Returns the position of the hot-key in the text. -1 if not found.</param>
		/// <returns>The input text with the hotkey specifier ('_') removed.</returns>
		public static ustring RemoveHotKeySpecifier (ustring text, int hotPos, Rune hotKeySpecifier)
		{
			if (ustring.IsNullOrEmpty (text)) {
				return text;
			}

			// Scan 
			ustring start = ustring.Empty;
			int i = 0;
			foreach (Rune c in text) {
				if (c == hotKeySpecifier && i == hotPos) {
					i++;
					continue;
				}
				start += ustring.Make (c);
				i++;
			}
			return start;
		}
		#endregion // Static Members

		List<ustring> _lines = new List<ustring> ();
		ustring _text;
		TextAlignment _textAlignment;
		VerticalTextAlignment _textVerticalAlignment;
		TextDirection _textDirection;
		Attribute _textColor = -1;
		Key _hotKey;
		int _hotKeyPos = -1;
		Size _size;

		/// <summary>
		/// Event invoked when the <see cref="HotKey"/> is changed.
		/// </summary>
		public event EventHandler<KeyChangedEventArgs> HotKeyChanged;

		/// <summary>
		///   The text to be displayed. This text is never modified.
		/// </summary>
		public virtual ustring Text {
			get => _text;
			set {
				_text = value;

				if (_text != null && _text.RuneCount > 0 && (Size.Width == 0 || Size.Height == 0 || Size.Width != _text.ConsoleWidth)) {
					// Provide a default size (width = length of longest line, height = 1)
					// TODO: It might makes more sense for the default to be width = length of first line?
					Size = new Size (TextFormatter.MaxWidth (Text, int.MaxValue), 1);
				}

				NeedsFormat = true;
			}
		}

		/// <summary>
		/// Used by <see cref="Text"/> to resize the view's <see cref="View.Bounds"/> with the <see cref="Size"/>.
		/// Setting <see cref="AutoSize"/> to true only work if the <see cref="View.Width"/> and <see cref="View.Height"/> are null or
		///   <see cref="LayoutStyle.Absolute"/> values and doesn't work with <see cref="LayoutStyle.Computed"/> layout,
		///   to avoid breaking the <see cref="Pos"/> and <see cref="Dim"/> settings.
		/// </summary>
		public bool AutoSize { get; set; }

		/// <summary>
		/// Gets or sets whether trailing spaces at the end of word-wrapped lines are preserved
		/// or not when <see cref="TextFormatter.WordWrap"/> is enabled. 
		/// If <see langword="true"/> trailing spaces at the end of wrapped lines will be removed when 
		/// <see cref="Text"/> is formatted for display. The default is <see langword="false"/>.
		/// </summary>
		public bool PreserveTrailingSpaces { get; set; }

		/// <summary>
		/// Controls the horizontal text-alignment property.
		/// </summary>
		/// <value>The text alignment.</value>
		public TextAlignment Alignment {
			get => _textAlignment;
			set {
				_textAlignment = value;
				NeedsFormat = true;
			}
		}

		/// <summary>
		/// Controls the vertical text-alignment property. 
		/// </summary>
		/// <value>The text vertical alignment.</value>
		public VerticalTextAlignment VerticalAlignment {
			get => _textVerticalAlignment;
			set {
				_textVerticalAlignment = value;
				NeedsFormat = true;
			}
		}

		/// <summary>
		/// Controls the text-direction property. 
		/// </summary>
		/// <value>The text vertical alignment.</value>
		public TextDirection Direction {
			get => _textDirection;
			set {
				_textDirection = value;
				NeedsFormat = true;
			}
		}

		/// <summary>
		/// Check if it is a horizontal direction
		/// </summary>
		public static bool IsHorizontalDirection (TextDirection textDirection)
		{
			switch (textDirection) {
			case TextDirection.LeftRight_TopBottom:
			case TextDirection.LeftRight_BottomTop:
			case TextDirection.RightLeft_TopBottom:
			case TextDirection.RightLeft_BottomTop:
				return true;
			default:
				return false;
			}
		}

		/// <summary>
		/// Check if it is a vertical direction
		/// </summary>
		public static bool IsVerticalDirection (TextDirection textDirection)
		{
			switch (textDirection) {
			case TextDirection.TopBottom_LeftRight:
			case TextDirection.TopBottom_RightLeft:
			case TextDirection.BottomTop_LeftRight:
			case TextDirection.BottomTop_RightLeft:
				return true;
			default:
				return false;
			}
		}

		/// <summary>
		/// Check if it is Left to Right direction
		/// </summary>
		public static bool IsLeftToRight (TextDirection textDirection)
		{
			switch (textDirection) {
			case TextDirection.LeftRight_TopBottom:
			case TextDirection.LeftRight_BottomTop:
				return true;
			default:
				return false;
			}
		}

		/// <summary>
		/// Check if it is Top to Bottom direction
		/// </summary>
		public static bool IsTopToBottom (TextDirection textDirection)
		{
			switch (textDirection) {
			case TextDirection.TopBottom_LeftRight:
			case TextDirection.TopBottom_RightLeft:
				return true;
			default:
				return false;
			}
		}

		// TODO: This is not implemented!
		/// <summary>
		/// 
		/// </summary>
		public bool WordWrap { get; set; } = false;

		/// <summary>
		/// Gets or sets the size of the area the text will be constrained to when formatted.
		/// </summary>
		/// <remarks>
		/// Does not return the size the formatted text; just the value that was set.
		/// </remarks>
		public Size Size {
			get {
				return _size;
			}
			set {
				_size = value;
				NeedsFormat = true;
			}
		}

		/// <summary>
		/// The specifier character for the hotkey (e.g. '_'). Set to '\xffff' to disable hotkey support for this View instance. The default is '\xffff'.
		/// </summary>
		public Rune HotKeySpecifier { get; set; } = (Rune)0xFFFF;

		/// <summary>
		/// The position in the text of the hotkey. The hotkey will be rendered using the hot color.
		/// </summary>
		public int HotKeyPos { get => _hotKeyPos; set => _hotKeyPos = value; }

		/// <summary>
		/// Gets the hotkey. Will be an upper case letter or digit.
		/// </summary>
		public Key HotKey {
			get => _hotKey;
			internal set {
				if (_hotKey != value) {
					var oldKey = _hotKey;
					_hotKey = value;
					HotKeyChanged?.Invoke (this, new KeyChangedEventArgs (oldKey, value));
				}
			}
		}

		/// <summary>
		/// Gets the cursor position from <see cref="HotKey"/>. If the <see cref="HotKey"/> is defined, the cursor will be positioned over it.
		/// </summary>
		public int CursorPosition { get; set; }

		/// <summary>
		/// Gets the size required to hold the formatted text, given the constraints placed by <see cref="Size"/>.
		/// </summary>
		/// <remarks>
		/// Causes a format, resetting <see cref="NeedsFormat"/>.
		/// </remarks>
		/// <returns></returns>
		public Size GetFormattedSize ()
		{
			var lines = Lines;
			var width = Lines.Max (line => TextFormatter.GetTextWidth (line));
			var height = Lines.Count;
			return new Size (width, height);
		}

		/// <summary>
		/// Gets the formatted lines.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Upon a 'get' of this property, if the text needs to be formatted (if <see cref="NeedsFormat"/> is <c>true</c>)
		/// <see cref="Format(ustring, int, bool, bool, bool, int, TextDirection)"/> will be called internally. 
		/// </para>
		/// </remarks>
		public List<ustring> Lines {
			get {
				// With this check, we protect against subclasses with overrides of Text
				if (ustring.IsNullOrEmpty (Text) || Size.IsEmpty) {
					_lines = new List<ustring> {
						ustring.Empty
					};
					NeedsFormat = false;
					return _lines;
				}

				if (NeedsFormat) {
					var shown_text = _text;
					if (FindHotKey (_text, HotKeySpecifier, true, out _hotKeyPos, out Key newHotKey)) {
						HotKey = newHotKey;
						shown_text = RemoveHotKeySpecifier (Text, _hotKeyPos, HotKeySpecifier);
						shown_text = ReplaceHotKeyWithTag (shown_text, _hotKeyPos);
					}

					if (IsVerticalDirection (_textDirection)) {
						var colsWidth = GetSumMaxCharWidth (shown_text, 0, 1);
						_lines = Format (shown_text, Size.Height, _textVerticalAlignment == VerticalTextAlignment.Justified, Size.Width > colsWidth,
							PreserveTrailingSpaces, 0, _textDirection);
						if (!AutoSize) {
							colsWidth = GetMaxColsForWidth (_lines, Size.Width);
							if (_lines.Count > colsWidth) {
								_lines.RemoveRange (colsWidth, _lines.Count - colsWidth);
							}
						}
					} else {
						_lines = Format (shown_text, Size.Width, _textAlignment == TextAlignment.Justified, Size.Height > 1,
							PreserveTrailingSpaces, 0, _textDirection);
						if (!AutoSize && _lines.Count > Size.Height) {
							_lines.RemoveRange (Size.Height, _lines.Count - Size.Height);
						}
					}

					NeedsFormat = false;
				}
				return _lines;
			}
		}

		/// <summary>
		/// Gets or sets whether the <see cref="TextFormatter"/> needs to format the text when <see cref="Draw(Rect, Attribute, Attribute, Rect, bool)"/> is called.
		/// If it is <c>false</c> when Draw is called, the Draw call will be faster.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This is set to true when the properties of <see cref="TextFormatter"/> are set.
		/// </para>
		/// </remarks>
		public bool NeedsFormat { get; set; }

		/// <summary>
		/// Causes the <see cref="TextFormatter"/> to reformat the text. 
		/// </summary>
		/// <returns>The formatted text.</returns>
		public string Format ()
		{
			var sb = new StringBuilder ();
			// Lines_get causes a Format
			foreach (var line in Lines) {
				sb.AppendLine (line.ToString ());
			}
			return sb.ToString ();
		}

		/// <summary>
		/// Draws the text held by <see cref="TextFormatter"/> to <see cref="Application.Driver"/> using the colors specified.
		/// </summary>
		/// <param name="bounds">Specifies the screen-relative location and maximum size for drawing the text.</param>
		/// <param name="normalColor">The color to use for all text except the hotkey</param>
		/// <param name="hotColor">The color to use to draw the hotkey</param>
		/// <param name="containerBounds">Specifies the screen-relative location and maximum container size.</param>
		/// <param name="fillRemaining">Determines if the bounds width will be used (default) or only the text width will be used.</param>
		public void Draw (Rect bounds, Attribute normalColor, Attribute hotColor, Rect containerBounds = default, bool fillRemaining = true)
		{
			// With this check, we protect against subclasses with overrides of Text (like Button)
			if (ustring.IsNullOrEmpty (_text)) {
				return;
			}

			Application.Driver?.SetAttribute (normalColor);

			// Use "Lines" to ensure a Format (don't use "lines"))

			var linesFormated = Lines;
			switch (_textDirection) {
			case TextDirection.TopBottom_RightLeft:
			case TextDirection.LeftRight_BottomTop:
			case TextDirection.RightLeft_BottomTop:
			case TextDirection.BottomTop_RightLeft:
				linesFormated.Reverse ();
				break;
			}

			var isVertical = IsVerticalDirection (_textDirection);
			var maxBounds = bounds;
			if (Application.Driver != null) {
				maxBounds = containerBounds == default
					? bounds
					: new Rect (Math.Max (containerBounds.X, bounds.X),
					Math.Max (containerBounds.Y, bounds.Y),
					Math.Max (Math.Min (containerBounds.Width, containerBounds.Right - bounds.Left), 0),
					Math.Max (Math.Min (containerBounds.Height, containerBounds.Bottom - bounds.Top), 0));
			}
			if (maxBounds.Width == 0 || maxBounds.Height == 0) {
				return;
			}

			// BUGBUG: v2 - TextFormatter should not change the clip region. If a caller wants to break out of the clip region it should do
			// so explicitly.
			//var savedClip = Application.Driver?.Clip;
			//if (Application.Driver != null) {
			//	Application.Driver.Clip = maxBounds;
			//}
			var lineOffset = !isVertical && bounds.Y < 0 ? Math.Abs (bounds.Y) : 0;

			for (int line = lineOffset; line < linesFormated.Count; line++) {
				if ((isVertical && line > bounds.Width) || (!isVertical && line > bounds.Height))
					continue;
				if ((isVertical && line >= maxBounds.Left + maxBounds.Width)
					|| (!isVertical && line >= maxBounds.Top + maxBounds.Height + lineOffset))

					break;

				var runes = _lines [line].ToRunes ();

				switch (_textDirection) {
				case TextDirection.RightLeft_BottomTop:
				case TextDirection.RightLeft_TopBottom:
				case TextDirection.BottomTop_LeftRight:
				case TextDirection.BottomTop_RightLeft:
					runes = runes.Reverse ().ToArray ();
					break;
				}

				// When text is justified, we lost left or right, so we use the direction to align. 

				int x, y;
				// Horizontal Alignment
				if (_textAlignment == TextAlignment.Right || (_textAlignment == TextAlignment.Justified && !IsLeftToRight (_textDirection))) {
					if (isVertical) {
						var runesWidth = GetSumMaxCharWidth (Lines, line);
						x = bounds.Right - runesWidth;
						CursorPosition = bounds.Width - runesWidth + (_hotKeyPos > -1 ? _hotKeyPos : 0);
					} else {
						var runesWidth = GetTextWidth (ustring.Make (runes));
						x = bounds.Right - runesWidth;
						CursorPosition = bounds.Width - runesWidth + (_hotKeyPos > -1 ? _hotKeyPos : 0);
					}
				} else if (_textAlignment == TextAlignment.Left || _textAlignment == TextAlignment.Justified) {
					if (isVertical) {
						var runesWidth = line > 0 ? GetSumMaxCharWidth (Lines, 0, line) : 0;
						x = bounds.Left + runesWidth;
					} else {
						x = bounds.Left;
					}
					CursorPosition = _hotKeyPos > -1 ? _hotKeyPos : 0;
				} else if (_textAlignment == TextAlignment.Centered) {
					if (isVertical) {
						var runesWidth = GetSumMaxCharWidth (Lines, line);
						x = bounds.Left + line + ((bounds.Width - runesWidth) / 2);
						CursorPosition = (bounds.Width - runesWidth) / 2 + (_hotKeyPos > -1 ? _hotKeyPos : 0);
					} else {
						var runesWidth = GetTextWidth (ustring.Make (runes));
						x = bounds.Left + (bounds.Width - runesWidth) / 2;
						CursorPosition = (bounds.Width - runesWidth) / 2 + (_hotKeyPos > -1 ? _hotKeyPos : 0);
					}
				} else {
					throw new ArgumentOutOfRangeException ();
				}

				// Vertical Alignment
				if (_textVerticalAlignment == VerticalTextAlignment.Bottom || (_textVerticalAlignment == VerticalTextAlignment.Justified && !IsTopToBottom (_textDirection))) {
					if (isVertical) {
						y = bounds.Bottom - runes.Length;
					} else {
						y = bounds.Bottom - Lines.Count + line;
					}
				} else if (_textVerticalAlignment == VerticalTextAlignment.Top || _textVerticalAlignment == VerticalTextAlignment.Justified) {
					if (isVertical) {
						y = bounds.Top;
					} else {
						y = bounds.Top + line;
					}
				} else if (_textVerticalAlignment == VerticalTextAlignment.Middle) {
					if (isVertical) {
						var s = (bounds.Height - runes.Length) / 2;
						y = bounds.Top + s;
					} else {
						var s = (bounds.Height - Lines.Count) / 2;
						y = bounds.Top + line + s;
					}
				} else {
					throw new ArgumentOutOfRangeException ();
				}

				var colOffset = bounds.X < 0 ? Math.Abs (bounds.X) : 0;
				var start = isVertical ? bounds.Top : bounds.Left;
				var size = isVertical ? bounds.Height : bounds.Width;
				var current = start + colOffset;

				for (var idx = (isVertical ? start - y : start - x) + colOffset; current < start + size; idx++) {
					if (idx < 0 || x + current + colOffset < 0) {
						current++;
						continue;
					} else if (!fillRemaining && idx > runes.Length - 1) {
						break;
					}
					if ((!isVertical && idx > maxBounds.Left + maxBounds.Width - bounds.X + colOffset)
						|| (isVertical && idx > maxBounds.Top + maxBounds.Height - bounds.Y))

						break;

					var rune = (Rune)' ';
					if (isVertical) {
						Application.Driver?.Move (x, current);
						if (idx >= 0 && idx < runes.Length) {
							rune = runes [idx];
						}
					} else {
						Application.Driver?.Move (current, y);
						if (idx >= 0 && idx < runes.Length) {
							rune = runes [idx];
						}
					}
					if (HotKeyPos > -1 && idx == HotKeyPos) {
						if ((isVertical && _textVerticalAlignment == VerticalTextAlignment.Justified) ||
						(!isVertical && _textAlignment == TextAlignment.Justified)) {
							CursorPosition = idx - start;
						}
						Application.Driver?.SetAttribute (hotColor);
						Application.Driver?.AddRune (rune);
						Application.Driver?.SetAttribute (normalColor);
					} else {
						Application.Driver?.AddRune (rune);
					}
					var runeWidth = Math.Max (Rune.ColumnWidth (rune), 1);
					if (isVertical) {
						current++;
					} else {
						current += runeWidth;
					}
					var nextRuneWidth = idx + 1 > -1 && idx + 1 < runes.Length ? Rune.ColumnWidth (runes [idx + 1]) : 0;
					if (!isVertical && idx + 1 < runes.Length && current + nextRuneWidth > start + size) {
						break;
					}
				}
			}
			//if (Application.Driver != null) {
			//	Application.Driver.Clip = (Rect)savedClip;
			//}
		}
	}
}
