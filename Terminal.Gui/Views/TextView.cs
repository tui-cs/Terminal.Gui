// TextView.cs: multi-line text editing
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using NStack;
using Terminal.Gui.Resources;
using static Terminal.Gui.Graphs.PathAnnotation;
using Rune = System.Rune;

namespace Terminal.Gui {
	class TextModel {
		List<List<Rune>> lines = new List<List<Rune>> ();

		public event EventHandler LinesLoaded;

		public bool LoadFile (string file)
		{
			FilePath = file ?? throw new ArgumentNullException (nameof (file));

			var stream = File.OpenRead (file);
			LoadStream (stream);
			return true;
		}

		public bool CloseFile ()
		{
			if (FilePath == null)
				throw new ArgumentNullException (nameof (FilePath));

			FilePath = null;
			lines = new List<List<Rune>> ();
			return true;
		}

		// Turns the ustring into runes, this does not split the 
		// contents on a newline if it is present.
		internal static List<Rune> ToRunes (ustring str)
		{
			List<Rune> runes = new List<Rune> ();
			foreach (var x in str.ToRunes ()) {
				runes.Add (x);
			}
			return runes;
		}

		// Splits a string into a List that contains a List<Rune> for each line
		public static List<List<Rune>> StringToRunes (ustring content)
		{
			var lines = new List<List<Rune>> ();
			int start = 0, i = 0;
			var hasCR = false;
			// ASCII code 13 = Carriage Return.
			// ASCII code 10 = Line Feed.
			for (; i < content.Length; i++) {
				if (content [i] == 13) {
					hasCR = true;
					continue;
				}
				if (content [i] == 10) {
					if (i - start > 0)
						lines.Add (ToRunes (content [start, hasCR ? i - 1 : i]));
					else
						lines.Add (ToRunes (ustring.Empty));
					start = i + 1;
					hasCR = false;
				}
			}
			if (i - start >= 0)
				lines.Add (ToRunes (content [start, null]));
			return lines;
		}

		void Append (List<byte> line)
		{
			var str = ustring.Make (line.ToArray ());
			lines.Add (ToRunes (str));
		}

		public void LoadStream (Stream input)
		{
			if (input == null)
				throw new ArgumentNullException (nameof (input));

			lines = new List<List<Rune>> ();
			var buff = new BufferedStream (input);
			int v;
			var line = new List<byte> ();
			var wasNewLine = false;
			while ((v = buff.ReadByte ()) != -1) {
				if (v == 13) {
					continue;
				}
				if (v == 10) {
					Append (line);
					line.Clear ();
					wasNewLine = true;
					continue;
				}
				line.Add ((byte)v);
				wasNewLine = false;
			}
			if (line.Count > 0 || wasNewLine)
				Append (line);
			buff.Dispose ();

			OnLinesLoaded ();
		}

		public void LoadString (ustring content)
		{
			lines = StringToRunes (content);

			OnLinesLoaded ();
		}

		void OnLinesLoaded ()
		{
			LinesLoaded?.Invoke (this, EventArgs.Empty);
		}

		public override string ToString ()
		{
			var sb = new StringBuilder ();
			for (int i = 0; i < lines.Count; i++) {
				sb.Append (ustring.Make (lines [i]));
				if ((i + 1) < lines.Count) {
					sb.AppendLine ();
				}
			}
			return sb.ToString ();
		}

		public string FilePath { get; set; }

		/// <summary>
		/// The number of text lines in the model
		/// </summary>
		public int Count => lines.Count;

		/// <summary>
		/// Returns the specified line as a List of Rune
		/// </summary>
		/// <returns>The line.</returns>
		/// <param name="line">Line number to retrieve.</param>
		public List<Rune> GetLine (int line)
		{
			if (lines.Count > 0) {
				if (line < Count) {
					return lines [line];
				} else {
					return lines [Count - 1];
				}
			} else {
				lines.Add (new List<Rune> ());
				return lines [0];
			}
		}

		/// <summary>
		/// Adds a line to the model at the specified position.
		/// </summary>
		/// <param name="pos">Line number where the line will be inserted.</param>
		/// <param name="runes">The line of text, as a List of Rune.</param>
		public void AddLine (int pos, List<Rune> runes)
		{
			lines.Insert (pos, runes);
		}

		/// <summary>
		/// Removes the line at the specified position
		/// </summary>
		/// <param name="pos">Position.</param>
		public void RemoveLine (int pos)
		{
			if (lines.Count > 0) {
				if (lines.Count == 1 && lines [0].Count == 0) {
					return;
				}
				lines.RemoveAt (pos);
			}
		}

		public void ReplaceLine (int pos, List<Rune> runes)
		{
			if (lines.Count > 0 && pos < lines.Count) {
				lines [pos] = new List<Rune> (runes);
			} else if (lines.Count == 0 || (lines.Count > 0 && pos >= lines.Count)) {
				lines.Add (runes);
			}
		}

		/// <summary>
		/// Returns the maximum line length of the visible lines.
		/// </summary>
		/// <param name="first">The first line.</param>
		/// <param name="last">The last line.</param>
		/// <param name="tabWidth">The tab width.</param>
		public int GetMaxVisibleLine (int first, int last, int tabWidth)
		{
			int maxLength = 0;
			last = last < lines.Count ? last : lines.Count;
			for (int i = first; i < last; i++) {
				var line = GetLine (i);
				var tabSum = line.Sum (r => r == '\t' ? Math.Max (tabWidth - 1, 0) : 0);
				var l = line.Count + tabSum;
				if (l > maxLength) {
					maxLength = l;
				}
			}

			return maxLength;
		}

		internal static bool SetCol (ref int col, int width, int cols)
		{
			if (col + cols <= width) {
				col += cols;
				return true;
			}

			return false;
		}

		internal static int GetColFromX (List<Rune> t, int start, int x, int tabWidth = 0)
		{
			if (x < 0) {
				return x;
			}
			int size = start;
			var pX = x + start;
			for (int i = start; i < t.Count; i++) {
				var r = t [i];
				size += Rune.ColumnWidth (r);
				if (r == '\t') {
					size += tabWidth + 1;
				}
				if (i == pX || (size > pX)) {
					return i - start;
				}
			}
			return t.Count - start;
		}

		// Returns the size and length in a range of the string.
		internal static (int size, int length) DisplaySize (List<Rune> t, int start = -1, int end = -1,
			bool checkNextRune = true, int tabWidth = 0)
		{
			if (t == null || t.Count == 0) {
				return (0, 0);
			}
			int size = 0;
			int len = 0;
			int tcount = end == -1 ? t.Count : end > t.Count ? t.Count : end;
			int i = start == -1 ? 0 : start;
			for (; i < tcount; i++) {
				var rune = t [i];
				size += Rune.ColumnWidth (rune);
				len += Rune.RuneLen (rune);
				if (rune == '\t') {
					size += tabWidth + 1;
					len += tabWidth - 1;
				}
				if (checkNextRune && i == tcount - 1 && t.Count > tcount
					&& IsWideRune (t [i + 1], tabWidth, out int s, out int l)) {
					size += s;
					len += l;
				}
			}

			bool IsWideRune (Rune r, int tWidth, out int s, out int l)
			{
				s = Rune.ColumnWidth (r);
				l = Rune.RuneLen (r);
				if (r == '\t') {
					s += tWidth + 1;
					l += tWidth - 1;
				}

				return s > 1;
			}

			return (size, len);
		}

		// Returns the left column in a range of the string.
		internal static int CalculateLeftColumn (List<Rune> t, int start, int end, int width, int tabWidth = 0)
		{
			if (t == null || t.Count == 0) {
				return 0;
			}
			int size = 0;
			int tcount = end > t.Count - 1 ? t.Count - 1 : end;
			int col = 0;

			for (int i = tcount; i >= 0; i--) {
				var rune = t [i];
				size += Rune.ColumnWidth (rune);
				if (rune == '\t') {
					size += tabWidth + 1;
				}
				if (size > width) {
					if (col + width == end) {
						col++;
					}
					break;
				} else if ((end < t.Count && col > 0 && start < end && col == start) || (end - col == width - 1)) {
					break;
				}
				col = i;
			}

			return col;
		}

		(Point startPointToFind, Point currentPointToFind, bool found) toFind;

		internal (Point current, bool found) FindNextText (ustring text, out bool gaveFullTurn, bool matchCase = false, bool matchWholeWord = false)
		{
			if (text == null || lines.Count == 0) {
				gaveFullTurn = false;
				return (Point.Empty, false);
			}

			if (toFind.found) {
				toFind.currentPointToFind.X++;
			}
			var foundPos = GetFoundNextTextPoint (text, lines.Count, matchCase, matchWholeWord, toFind.currentPointToFind);
			if (!foundPos.found && toFind.currentPointToFind != toFind.startPointToFind) {
				foundPos = GetFoundNextTextPoint (text, toFind.startPointToFind.Y + 1, matchCase, matchWholeWord, Point.Empty);
			}
			gaveFullTurn = ApplyToFind (foundPos);

			return foundPos;
		}

		internal (Point current, bool found) FindPreviousText (ustring text, out bool gaveFullTurn, bool matchCase = false, bool matchWholeWord = false)
		{
			if (text == null || lines.Count == 0) {
				gaveFullTurn = false;
				return (Point.Empty, false);
			}

			if (toFind.found) {
				toFind.currentPointToFind.X++;
			}
			var linesCount = toFind.currentPointToFind.IsEmpty ? lines.Count - 1 : toFind.currentPointToFind.Y;
			var foundPos = GetFoundPreviousTextPoint (text, linesCount, matchCase, matchWholeWord, toFind.currentPointToFind);
			if (!foundPos.found && toFind.currentPointToFind != toFind.startPointToFind) {
				foundPos = GetFoundPreviousTextPoint (text, lines.Count - 1, matchCase, matchWholeWord,
					new Point (lines [lines.Count - 1].Count, lines.Count));
			}
			gaveFullTurn = ApplyToFind (foundPos);

			return foundPos;
		}

		internal (Point current, bool found) ReplaceAllText (ustring text, bool matchCase = false, bool matchWholeWord = false, ustring textToReplace = null)
		{
			bool found = false;
			Point pos = Point.Empty;

			for (int i = 0; i < lines.Count; i++) {
				var x = lines [i];
				var txt = GetText (x);
				var matchText = !matchCase ? text.ToUpper ().ToString () : text.ToString ();
				var col = txt.IndexOf (matchText);
				while (col > -1) {
					if (matchWholeWord && !MatchWholeWord (txt, matchText, col)) {
						if (col + 1 > txt.Length) {
							break;
						}
						col = txt.IndexOf (matchText, col + 1);
						continue;
					}
					if (col > -1) {
						if (!found) {
							found = true;
						}
						lines [i] = ReplaceText (x, textToReplace, matchText, col).ToRuneList ();
						x = lines [i];
						txt = GetText (x);
						pos = new Point (col, i);
						col += (textToReplace.Length - matchText.Length);
					}
					if (col + 1 > txt.Length) {
						break;
					}
					col = txt.IndexOf (matchText, col + 1);
				}
			}

			string GetText (List<Rune> x)
			{
				var txt = ustring.Make (x).ToString ();
				if (!matchCase) {
					txt = txt.ToUpper ();
				}
				return txt;
			}

			return (pos, found);
		}

		ustring ReplaceText (List<Rune> source, ustring textToReplace, string matchText, int col)
		{
			var origTxt = ustring.Make (source);
			(int _, int len) = TextModel.DisplaySize (source, 0, col, false);
			(var _, var len2) = TextModel.DisplaySize (source, col, col + matchText.Length, false);
			(var _, var len3) = TextModel.DisplaySize (source, col + matchText.Length, origTxt.RuneCount, false);

			return origTxt [0, len] +
				textToReplace.ToString () +
				origTxt [len + len2, len + len2 + len3];
		}

		bool ApplyToFind ((Point current, bool found) foundPos)
		{
			bool gaveFullTurn = false;
			if (foundPos.found) {
				toFind.currentPointToFind = foundPos.current;
				if (toFind.found && toFind.currentPointToFind == toFind.startPointToFind) {
					gaveFullTurn = true;
				}
				if (!toFind.found) {
					toFind.startPointToFind = toFind.currentPointToFind = foundPos.current;
					toFind.found = foundPos.found;
				}
			}

			return gaveFullTurn;
		}

		(Point current, bool found) GetFoundNextTextPoint (ustring text, int linesCount, bool matchCase, bool matchWholeWord, Point start)
		{
			for (int i = start.Y; i < linesCount; i++) {
				var x = lines [i];
				var txt = ustring.Make (x).ToString ();
				if (!matchCase) {
					txt = txt.ToUpper ();
				}
				var matchText = !matchCase ? text.ToUpper ().ToString () : text.ToString ();
				var col = txt.IndexOf (matchText, Math.Min (start.X, txt.Length));
				if (col > -1 && matchWholeWord && !MatchWholeWord (txt, matchText, col)) {
					continue;
				}
				if (col > -1 && ((i == start.Y && col >= start.X)
					|| i > start.Y)
					&& txt.Contains (matchText)) {
					return (new Point (col, i), true);
				} else if (col == -1 && start.X > 0) {
					start.X = 0;
				}
			}

			return (Point.Empty, false);
		}

		(Point current, bool found) GetFoundPreviousTextPoint (ustring text, int linesCount, bool matchCase, bool matchWholeWord, Point start)
		{
			for (int i = linesCount; i >= 0; i--) {
				var x = lines [i];
				var txt = ustring.Make (x).ToString ();
				if (!matchCase) {
					txt = txt.ToUpper ();
				}
				if (start.Y != i) {
					start.X = Math.Max (x.Count - 1, 0);
				}
				var matchText = !matchCase ? text.ToUpper ().ToString () : text.ToString ();
				var col = txt.LastIndexOf (matchText, toFind.found ? start.X - 1 : start.X);
				if (col > -1 && matchWholeWord && !MatchWholeWord (txt, matchText, col)) {
					continue;
				}
				if (col > -1 && ((i <= linesCount && col <= start.X)
					|| i < start.Y)
					&& txt.Contains (matchText)) {
					return (new Point (col, i), true);
				}
			}

			return (Point.Empty, false);
		}

		bool MatchWholeWord (string source, string matchText, int index = 0)
		{
			if (string.IsNullOrEmpty (source) || string.IsNullOrEmpty (matchText)) {
				return false;
			}

			var txt = matchText.Trim ();
			var start = index > 0 ? index - 1 : 0;
			var end = index + txt.Length;

			if ((start == 0 || Rune.IsWhiteSpace (source [start]))
				&& (end == source.Length || Rune.IsWhiteSpace (source [end]))) {
				return true;
			}

			return false;
		}

		/// <summary>
		/// Redefine column and line tracking.
		/// </summary>
		/// <param name="point">Contains the column and line.</param>
		internal void ResetContinuousFind (Point point)
		{
			toFind.startPointToFind = toFind.currentPointToFind = point;
			toFind.found = false;
		}
	}

	partial class HistoryText {
		public enum LineStatus {
			Original,
			Replaced,
			Removed,
			Added
		}

		List<HistoryTextItem> historyTextItems = new List<HistoryTextItem> ();
		int idxHistoryText = -1;
		ustring originalText;

		public bool IsFromHistory { get; private set; }

		public bool HasHistoryChanges => idxHistoryText > -1;

		public event EventHandler<HistoryTextItem> ChangeText;

		public void Add (List<List<Rune>> lines, Point curPos, LineStatus lineStatus = LineStatus.Original)
		{
			if (lineStatus == LineStatus.Original && historyTextItems.Count > 0
				&& historyTextItems.Last ().LineStatus == LineStatus.Original) {
				return;
			}
			if (lineStatus == LineStatus.Replaced && historyTextItems.Count > 0
				&& historyTextItems.Last ().LineStatus == LineStatus.Replaced) {
				return;
			}

			if (historyTextItems.Count == 0 && lineStatus != LineStatus.Original)
				throw new ArgumentException ("The first item must be the original.");

			if (idxHistoryText >= 0 && idxHistoryText + 1 < historyTextItems.Count)
				historyTextItems.RemoveRange (idxHistoryText + 1, historyTextItems.Count - idxHistoryText - 1);

			historyTextItems.Add (new HistoryTextItem (lines, curPos, lineStatus));
			idxHistoryText++;
		}

		public void ReplaceLast (List<List<Rune>> lines, Point curPos, LineStatus lineStatus)
		{
			var found = historyTextItems.FindLast (x => x.LineStatus == lineStatus);
			if (found != null) {
				found.Lines = lines;
				found.CursorPosition = curPos;
			}
		}

		public void Undo ()
		{
			if (historyTextItems?.Count > 0 && idxHistoryText > 0) {
				IsFromHistory = true;

				idxHistoryText--;

				var historyTextItem = new HistoryTextItem (historyTextItems [idxHistoryText]) {
					IsUndoing = true
				};

				ProcessChanges (ref historyTextItem);

				IsFromHistory = false;
			}
		}

		public void Redo ()
		{
			if (historyTextItems?.Count > 0 && idxHistoryText < historyTextItems.Count - 1) {
				IsFromHistory = true;

				idxHistoryText++;

				var historyTextItem = new HistoryTextItem (historyTextItems [idxHistoryText]) {
					IsUndoing = false
				};

				ProcessChanges (ref historyTextItem);

				IsFromHistory = false;
			}
		}

		void ProcessChanges (ref HistoryTextItem historyTextItem)
		{
			if (historyTextItem.IsUndoing) {
				if (idxHistoryText - 1 > -1 && ((historyTextItems [idxHistoryText - 1].LineStatus == LineStatus.Added)
					|| historyTextItems [idxHistoryText - 1].LineStatus == LineStatus.Removed
					|| (historyTextItem.LineStatus == LineStatus.Replaced &&
					historyTextItems [idxHistoryText - 1].LineStatus == LineStatus.Original))) {

					idxHistoryText--;

					while (historyTextItems [idxHistoryText].LineStatus == LineStatus.Added
						&& historyTextItems [idxHistoryText - 1].LineStatus == LineStatus.Removed) {

						idxHistoryText--;
					}
					historyTextItem = new HistoryTextItem (historyTextItems [idxHistoryText]);
					historyTextItem.IsUndoing = true;
					historyTextItem.FinalCursorPosition = historyTextItem.CursorPosition;
				}

				if (historyTextItem.LineStatus == LineStatus.Removed && historyTextItems [idxHistoryText + 1].LineStatus == LineStatus.Added) {
					historyTextItem.RemovedOnAdded = new HistoryTextItem (historyTextItems [idxHistoryText + 1]);
				}

				if ((historyTextItem.LineStatus == LineStatus.Added && historyTextItems [idxHistoryText - 1].LineStatus == LineStatus.Original)
					|| (historyTextItem.LineStatus == LineStatus.Removed && historyTextItems [idxHistoryText - 1].LineStatus == LineStatus.Original)
					|| (historyTextItem.LineStatus == LineStatus.Added && historyTextItems [idxHistoryText - 1].LineStatus == LineStatus.Removed)) {

					if (!historyTextItem.Lines [0].SequenceEqual (historyTextItems [idxHistoryText - 1].Lines [0])
						&& historyTextItem.CursorPosition == historyTextItems [idxHistoryText - 1].CursorPosition) {
						historyTextItem.Lines [0] = new List<Rune> (historyTextItems [idxHistoryText - 1].Lines [0]);
					}
					if (historyTextItem.LineStatus == LineStatus.Added && historyTextItems [idxHistoryText - 1].LineStatus == LineStatus.Removed) {
						historyTextItem.FinalCursorPosition = historyTextItems [idxHistoryText - 2].CursorPosition;
					} else {
						historyTextItem.FinalCursorPosition = historyTextItems [idxHistoryText - 1].CursorPosition;
					}
				} else {
					historyTextItem.FinalCursorPosition = historyTextItem.CursorPosition;
				}

				OnChangeText (historyTextItem);
				while (historyTextItems [idxHistoryText].LineStatus == LineStatus.Removed
					|| historyTextItems [idxHistoryText].LineStatus == LineStatus.Added) {

					idxHistoryText--;
				}
			} else if (!historyTextItem.IsUndoing) {
				if (idxHistoryText + 1 < historyTextItems.Count && (historyTextItem.LineStatus == LineStatus.Original
					|| historyTextItems [idxHistoryText + 1].LineStatus == LineStatus.Added
					|| historyTextItems [idxHistoryText + 1].LineStatus == LineStatus.Removed)) {

					idxHistoryText++;
					historyTextItem = new HistoryTextItem (historyTextItems [idxHistoryText]);
					historyTextItem.IsUndoing = false;
					historyTextItem.FinalCursorPosition = historyTextItem.CursorPosition;
				}

				if (historyTextItem.LineStatus == LineStatus.Added && historyTextItems [idxHistoryText - 1].LineStatus == LineStatus.Removed) {
					historyTextItem.RemovedOnAdded = new HistoryTextItem (historyTextItems [idxHistoryText - 1]);
				}

				if ((historyTextItem.LineStatus == LineStatus.Removed && historyTextItems [idxHistoryText + 1].LineStatus == LineStatus.Replaced)
					|| (historyTextItem.LineStatus == LineStatus.Removed && historyTextItems [idxHistoryText + 1].LineStatus == LineStatus.Original)
					|| (historyTextItem.LineStatus == LineStatus.Added && historyTextItems [idxHistoryText + 1].LineStatus == LineStatus.Replaced)) {

					if (historyTextItem.LineStatus == LineStatus.Removed
						&& !historyTextItem.Lines [0].SequenceEqual (historyTextItems [idxHistoryText + 1].Lines [0])) {
						historyTextItem.Lines [0] = new List<Rune> (historyTextItems [idxHistoryText + 1].Lines [0]);
					}
					historyTextItem.FinalCursorPosition = historyTextItems [idxHistoryText + 1].CursorPosition;
				} else {
					historyTextItem.FinalCursorPosition = historyTextItem.CursorPosition;
				}

				OnChangeText (historyTextItem);
				while (historyTextItems [idxHistoryText].LineStatus == LineStatus.Removed
					|| historyTextItems [idxHistoryText].LineStatus == LineStatus.Added) {

					idxHistoryText++;
				}
			}
		}

		void OnChangeText (HistoryTextItem lines)
		{
			ChangeText?.Invoke (this, lines);
		}

		public void Clear (ustring text)
		{
			historyTextItems.Clear ();
			idxHistoryText = -1;
			originalText = text;
			OnChangeText (null);
		}

		public bool IsDirty (ustring text)
		{
			return originalText != text;
		}
	}

	class WordWrapManager {
		class WrappedLine {
			public int ModelLine;
			public int Row;
			public int RowIndex;
			public int ColWidth;
		}

		List<WrappedLine> wrappedModelLines = new List<WrappedLine> ();
		int frameWidth;
		bool isWrapModelRefreshing;

		public TextModel Model { get; private set; }

		public WordWrapManager (TextModel model)
		{
			Model = model;
		}

		public TextModel WrapModel (int width, out int nRow, out int nCol, out int nStartRow, out int nStartCol,
			int row = 0, int col = 0, int startRow = 0, int startCol = 0, int tabWidth = 0, bool preserveTrailingSpaces = true)
		{
			frameWidth = width;

			var modelRow = isWrapModelRefreshing ? row : GetModelLineFromWrappedLines (row);
			var modelCol = isWrapModelRefreshing ? col : GetModelColFromWrappedLines (row, col);
			var modelStartRow = isWrapModelRefreshing ? startRow : GetModelLineFromWrappedLines (startRow);
			var modelStartCol = isWrapModelRefreshing ? startCol : GetModelColFromWrappedLines (startRow, startCol);
			var wrappedModel = new TextModel ();
			int lines = 0;
			nRow = 0;
			nCol = 0;
			nStartRow = 0;
			nStartCol = 0;
			bool isRowAndColSetted = row == 0 && col == 0;
			bool isStartRowAndColSetted = startRow == 0 && startCol == 0;
			List<WrappedLine> wModelLines = new List<WrappedLine> ();

			for (int i = 0; i < Model.Count; i++) {
				var line = Model.GetLine (i);
				var wrappedLines = ToListRune (
					TextFormatter.Format (ustring.Make (line), width, TextAlignment.Left, true, preserveTrailingSpaces, tabWidth));
				int sumColWidth = 0;
				for (int j = 0; j < wrappedLines.Count; j++) {
					var wrapLine = wrappedLines [j];
					if (!isRowAndColSetted && modelRow == i) {
						if (nCol + wrapLine.Count <= modelCol) {
							nCol += wrapLine.Count;
							nRow = lines;
							if (nCol == modelCol) {
								nCol = wrapLine.Count;
								isRowAndColSetted = true;
							} else if (j == wrappedLines.Count - 1) {
								nCol = wrapLine.Count - j + modelCol - nCol;
								isRowAndColSetted = true;
							}
						} else {
							var offset = nCol + wrapLine.Count - modelCol;
							nCol = wrapLine.Count - offset;
							nRow = lines;
							isRowAndColSetted = true;
						}
					}
					if (!isStartRowAndColSetted && modelStartRow == i) {
						if (nStartCol + wrapLine.Count <= modelStartCol) {
							nStartCol += wrapLine.Count;
							nStartRow = lines;
							if (nStartCol == modelStartCol) {
								nStartCol = wrapLine.Count;
								isStartRowAndColSetted = true;
							} else if (j == wrappedLines.Count - 1) {
								nStartCol = wrapLine.Count - j + modelStartCol - nStartCol;
								isStartRowAndColSetted = true;
							}
						} else {
							var offset = nStartCol + wrapLine.Count - modelStartCol;
							nStartCol = wrapLine.Count - offset;
							nStartRow = lines;
							isStartRowAndColSetted = true;
						}
					}
					wrappedModel.AddLine (lines, wrapLine);
					sumColWidth += wrapLine.Count;
					var wrappedLine = new WrappedLine () {
						ModelLine = i,
						Row = lines,
						RowIndex = j,
						ColWidth = wrapLine.Count,
					};
					wModelLines.Add (wrappedLine);
					lines++;
				}
			}
			wrappedModelLines = wModelLines;

			return wrappedModel;
		}

		public List<List<Rune>> ToListRune (List<ustring> textList)
		{
			var runesList = new List<List<Rune>> ();

			foreach (var text in textList) {
				runesList.Add (text.ToRuneList ());
			}

			return runesList;
		}

		public int GetModelLineFromWrappedLines (int line) => wrappedModelLines.Count > 0
			? wrappedModelLines [Math.Min (line, wrappedModelLines.Count - 1)].ModelLine
			: 0;

		public int GetModelColFromWrappedLines (int line, int col)
		{
			if (wrappedModelLines?.Count == 0) {
				return 0;
			}

			var modelLine = GetModelLineFromWrappedLines (line);
			var firstLine = wrappedModelLines.IndexOf (r => r.ModelLine == modelLine);
			int modelCol = 0;

			for (int i = firstLine; i <= Math.Min (line, wrappedModelLines.Count - 1); i++) {
				var wLine = wrappedModelLines [i];

				if (i < line) {
					modelCol += wLine.ColWidth;
				} else {
					modelCol += col;
				}
			}

			return modelCol;
		}

		List<Rune> GetCurrentLine (int row) => Model.GetLine (row);

		public void AddLine (int row, int col)
		{
			var modelRow = GetModelLineFromWrappedLines (row);
			var modelCol = GetModelColFromWrappedLines (row, col);
			var line = GetCurrentLine (modelRow);
			var restCount = line.Count - modelCol;
			var rest = line.GetRange (modelCol, restCount);
			line.RemoveRange (modelCol, restCount);
			Model.AddLine (modelRow + 1, rest);
			isWrapModelRefreshing = true;
			WrapModel (frameWidth, out _, out _, out _, out _, modelRow + 1, 0);
			isWrapModelRefreshing = false;
		}

		public bool Insert (int row, int col, Rune rune)
		{
			var line = GetCurrentLine (GetModelLineFromWrappedLines (row));
			line.Insert (GetModelColFromWrappedLines (row, col), rune);
			if (line.Count > frameWidth) {
				return true;
			} else {
				return false;
			}
		}

		public bool RemoveAt (int row, int col)
		{
			var modelRow = GetModelLineFromWrappedLines (row);
			var line = GetCurrentLine (modelRow);
			var modelCol = GetModelColFromWrappedLines (row, col);

			if (modelCol > line.Count) {
				Model.RemoveLine (modelRow);
				RemoveAt (row, 0);
				return false;
			}
			if (modelCol < line.Count)
				line.RemoveAt (modelCol);
			if (line.Count > frameWidth || (row + 1 < wrappedModelLines.Count
				&& wrappedModelLines [row + 1].ModelLine == modelRow)) {
				return true;
			}

			return false;
		}

		public bool RemoveLine (int row, int col, out bool lineRemoved, bool forward = true)
		{
			lineRemoved = false;
			var modelRow = GetModelLineFromWrappedLines (row);
			var line = GetCurrentLine (modelRow);
			var modelCol = GetModelColFromWrappedLines (row, col);

			if (modelCol == 0 && line.Count == 0) {
				Model.RemoveLine (modelRow);
				return false;
			} else if (modelCol < line.Count) {
				if (forward) {
					line.RemoveAt (modelCol);
					return true;
				} else if (modelCol - 1 > -1) {
					line.RemoveAt (modelCol - 1);
					return true;
				}
			}
			lineRemoved = true;
			if (forward) {
				if (modelRow + 1 == Model.Count) {
					return false;
				}

				var nextLine = Model.GetLine (modelRow + 1);
				line.AddRange (nextLine);
				Model.RemoveLine (modelRow + 1);
				if (line.Count > frameWidth) {
					return true;
				}
			} else {
				if (modelRow == 0) {
					return false;
				}

				var prevLine = Model.GetLine (modelRow - 1);
				prevLine.AddRange (line);
				Model.RemoveLine (modelRow);
				if (prevLine.Count > frameWidth) {
					return true;
				}
			}

			return false;
		}

		public bool RemoveRange (int row, int index, int count)
		{
			var modelRow = GetModelLineFromWrappedLines (row);
			var line = GetCurrentLine (modelRow);
			var modelCol = GetModelColFromWrappedLines (row, index);

			try {
				line.RemoveRange (modelCol, count);
			} catch (Exception) {
				return false;
			}

			return true;
		}

		public void UpdateModel (TextModel model, out int nRow, out int nCol, out int nStartRow, out int nStartCol,
			int row, int col, int startRow, int startCol, bool preserveTrailingSpaces)
		{
			isWrapModelRefreshing = true;
			Model = model;
			WrapModel (frameWidth, out nRow, out nCol, out nStartRow, out nStartCol, row, col, startRow, startCol, tabWidth: 0, preserveTrailingSpaces);
			isWrapModelRefreshing = false;
		}

		public int GetWrappedLineColWidth (int line, int col, WordWrapManager wrapManager)
		{
			if (wrappedModelLines?.Count == 0)
				return 0;

			var wModelLines = wrapManager.wrappedModelLines;
			var modelLine = GetModelLineFromWrappedLines (line);
			var firstLine = wrappedModelLines.IndexOf (r => r.ModelLine == modelLine);
			int modelCol = 0;
			int colWidthOffset = 0;
			int i = firstLine;

			while (modelCol < col) {
				var wLine = wrappedModelLines [i];
				var wLineToCompare = wModelLines [i];

				if (wLine.ModelLine != modelLine || wLineToCompare.ModelLine != modelLine)
					break;

				modelCol += Math.Max (wLine.ColWidth, wLineToCompare.ColWidth);
				colWidthOffset += wLine.ColWidth - wLineToCompare.ColWidth;
				if (modelCol > col) {
					modelCol += col - modelCol;
				}
				i++;
			}

			return modelCol - colWidthOffset;
		}
	}

	/// <summary>
	///  Multi-line text editing <see cref="View"/>.
	/// </summary>
	/// <remarks>
	///  <para>
	///   <see cref="TextView"/> provides a multi-line text editor. Users interact
	///   with it with the standard Windows, Mac, and Linux (Emacs) commands. 
	///  </para> 
	///  <list type="table"> 
	///   <listheader>
	///    <term>Shortcut</term>
	///    <description>Action performed</description>
	///   </listheader>
	///   <item>
	///    <term>Left cursor, Control-b</term>
	///    <description>
	///     Moves the editing point left.
	///    </description>
	///   </item>
	///   <item>
	///    <term>Right cursor, Control-f</term>
	///    <description>
	///     Moves the editing point right.
	///    </description>
	///   </item>
	///   <item>
	///    <term>Alt-b</term>
	///    <description>
	///     Moves one word back.
	///    </description>
	///   </item>
	///   <item>
	///    <term>Alt-f</term>
	///    <description>
	///     Moves one word forward.
	///    </description>
	///   </item>
	///   <item>
	///    <term>Up cursor, Control-p</term>
	///    <description>
	///     Moves the editing point one line up.
	///    </description>
	///   </item>
	///   <item>
	///    <term>Down cursor, Control-n</term>
	///    <description>
	///     Moves the editing point one line down
	///    </description>
	///   </item>
	///   <item>
	///    <term>Home key, Control-a</term>
	///    <description>
	///     Moves the cursor to the beginning of the line.
	///    </description>
	///   </item>
	///   <item>
	///    <term>End key, Control-e</term>
	///    <description>
	///     Moves the cursor to the end of the line.
	///    </description>
	///   </item>
	///   <item>
	///    <term>Control-Home</term>
	///    <description>
	///     Scrolls to the first line and moves the cursor there.
	///    </description>
	///   </item>
	///   <item>
	///    <term>Control-End</term>
	///    <description>
	///     Scrolls to the last line and moves the cursor there.
	///    </description>
	///   </item>
	///   <item>
	///    <term>Delete, Control-d</term>
	///    <description>
	///     Deletes the character in front of the cursor.
	///    </description>
	///   </item>
	///   <item>
	///    <term>Backspace</term>
	///    <description>
	///     Deletes the character behind the cursor.
	///    </description>
	///   </item>
	///   <item>
	///    <term>Control-k</term>
	///    <description>
	///     Deletes the text until the end of the line and replaces the kill buffer
	///     with the deleted text. You can paste this text in a different place by
	///     using Control-y.
	///    </description>
	///   </item>
	///   <item>
	///    <term>Control-y</term>
	///    <description>
	///      Pastes the content of the kill ring into the current position.
	///    </description>
	///   </item>
	///   <item>
	///    <term>Alt-d</term>
	///    <description>
	///      Deletes the word above the cursor and adds it to the kill ring. You 
	///      can paste the contents of the kill ring with Control-y.
	///    </description>
	///   </item>
	///   <item>
	///    <term>Control-q</term>
	///    <description>
	///     Quotes the next input character, to prevent the normal processing of
	///     key handling to take place.
	///    </description>
	///   </item>
	///  </list>
	/// </remarks>
	public partial class TextView : View {
		TextModel model = new TextModel ();
		int topRow;
		int leftColumn;
		int currentRow;
		int currentColumn;
		int selectionStartColumn, selectionStartRow;
		bool selecting;
		bool wordWrap;
		WordWrapManager wrapManager;
		bool continuousFind;
		int bottomOffset, rightOffset;
		int tabWidth = 4;
		bool allowsTab = true;
		bool allowsReturn = true;
		bool multiline = true;
		HistoryText historyText = new HistoryText ();
		CultureInfo currentCulture;

		/// <summary>
		/// Raised when the <see cref="Text"/> property of the <see cref="TextView"/> changes.
		/// </summary>
		/// <remarks>
		/// The <see cref="Text"/> property of <see cref="TextView"/> only changes when it is explicitly
		/// set, not as the user types. To be notified as the user changes the contents of the TextView
		/// see <see cref="IsDirty"/>.
		/// </remarks>
		public event EventHandler TextChanged;

		/// <summary>
		///  Raised when the contents of the <see cref="TextView"/> are changed. 
		/// </summary>
		/// <remarks>
		/// Unlike the <see cref="TextChanged"/> event, this event is raised whenever the user types or
		/// otherwise changes the contents of the <see cref="TextView"/>.
		/// </remarks>
		public event EventHandler<ContentsChangedEventArgs> ContentsChanged;

		/// <summary>
		/// Invoked with the unwrapped <see cref="CursorPosition"/>.
		/// </summary>
		public event EventHandler<PointEventArgs> UnwrappedCursorPosition;

		/// <summary>
		/// Provides autocomplete context menu based on suggestions at the current cursor
		/// position. Populate <see cref="Autocomplete.AllSuggestions"/> to enable this feature
		/// </summary>
		public IAutocomplete Autocomplete { get; protected set; } = new TextViewAutocomplete ();

		/// <summary>
		///  Initializes a <see cref="TextView"/> on the specified area, with absolute position and size.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public TextView (Rect frame) : base (frame)
		{
			Initialize ();
		}

		/// <summary>
		///  Initializes a <see cref="TextView"/> on the specified area, 
		///  with dimensions controlled with the X, Y, Width and Height properties.
		/// </summary>
		public TextView () : base ()
		{
			Initialize ();
		}

		void Initialize ()
		{
			CanFocus = true;
			Used = true;

			model.LinesLoaded += Model_LinesLoaded;
			historyText.ChangeText += HistoryText_ChangeText;

			Initialized += TextView_Initialized;

			// Things this view knows how to do
			AddCommand (Command.PageDown, () => { ProcessPageDown (); return true; });
			AddCommand (Command.PageDownExtend, () => { ProcessPageDownExtend (); return true; });
			AddCommand (Command.PageUp, () => { ProcessPageUp (); return true; });
			AddCommand (Command.PageUpExtend, () => { ProcessPageUpExtend (); return true; });
			AddCommand (Command.LineDown, () => { ProcessMoveDown (); return true; });
			AddCommand (Command.LineDownExtend, () => { ProcessMoveDownExtend (); return true; });
			AddCommand (Command.LineUp, () => { ProcessMoveUp (); return true; });
			AddCommand (Command.LineUpExtend, () => { ProcessMoveUpExtend (); return true; });
			AddCommand (Command.Right, () => ProcessMoveRight ());
			AddCommand (Command.RightExtend, () => { ProcessMoveRightExtend (); return true; });
			AddCommand (Command.Left, () => ProcessMoveLeft ());
			AddCommand (Command.LeftExtend, () => { ProcessMoveLeftExtend (); return true; });
			AddCommand (Command.DeleteCharLeft, () => { ProcessDeleteCharLeft (); return true; });
			AddCommand (Command.StartOfLine, () => { ProcessMoveStartOfLine (); return true; });
			AddCommand (Command.StartOfLineExtend, () => { ProcessMoveStartOfLineExtend (); return true; });
			AddCommand (Command.DeleteCharRight, () => { ProcessDeleteCharRight (); return true; });
			AddCommand (Command.EndOfLine, () => { ProcessMoveEndOfLine (); return true; });
			AddCommand (Command.EndOfLineExtend, () => { ProcessMoveEndOfLineExtend (); return true; });
			AddCommand (Command.CutToEndLine, () => { KillToEndOfLine (); return true; });
			AddCommand (Command.CutToStartLine, () => { KillToStartOfLine (); return true; });
			AddCommand (Command.Paste, () => { ProcessPaste (); return true; });
			AddCommand (Command.ToggleExtend, () => { ToggleSelecting (); return true; });
			AddCommand (Command.Copy, () => { ProcessCopy (); return true; });
			AddCommand (Command.Cut, () => { ProcessCut (); return true; });
			AddCommand (Command.WordLeft, () => { ProcessMoveWordBackward (); return true; });
			AddCommand (Command.WordLeftExtend, () => { ProcessMoveWordBackwardExtend (); return true; });
			AddCommand (Command.WordRight, () => { ProcessMoveWordForward (); return true; });
			AddCommand (Command.WordRightExtend, () => { ProcessMoveWordForwardExtend (); return true; });
			AddCommand (Command.KillWordForwards, () => { ProcessKillWordForward (); return true; });
			AddCommand (Command.KillWordBackwards, () => { ProcessKillWordBackward (); return true; });
			AddCommand (Command.NewLine, () => ProcessReturn ());
			AddCommand (Command.BottomEnd, () => { MoveBottomEnd (); return true; });
			AddCommand (Command.BottomEndExtend, () => { MoveBottomEndExtend (); return true; });
			AddCommand (Command.TopHome, () => { MoveTopHome (); return true; });
			AddCommand (Command.TopHomeExtend, () => { MoveTopHomeExtend (); return true; });
			AddCommand (Command.SelectAll, () => { ProcessSelectAll (); return true; });
			AddCommand (Command.ToggleOverwrite, () => { ProcessSetOverwrite (); return true; });
			AddCommand (Command.EnableOverwrite, () => { SetOverwrite (true); return true; });
			AddCommand (Command.DisableOverwrite, () => { SetOverwrite (false); return true; });
			AddCommand (Command.Tab, () => ProcessTab ());
			AddCommand (Command.BackTab, () => ProcessBackTab ());
			AddCommand (Command.NextView, () => ProcessMoveNextView ());
			AddCommand (Command.PreviousView, () => ProcessMovePreviousView ());
			AddCommand (Command.Undo, () => { UndoChanges (); return true; });
			AddCommand (Command.Redo, () => { RedoChanges (); return true; });
			AddCommand (Command.DeleteAll, () => { DeleteAll (); return true; });
			AddCommand (Command.Accept, () => {
				ContextMenu.Position = new Point (CursorPosition.X - leftColumn + 2, CursorPosition.Y - topRow + 2);
				ShowContextMenu ();
				return true;
			});

			// Default keybindings for this view
			AddKeyBinding (Key.PageDown, Command.PageDown);
			AddKeyBinding (Key.V | Key.CtrlMask, Command.PageDown);

			AddKeyBinding (Key.PageDown | Key.ShiftMask, Command.PageDownExtend);

			AddKeyBinding (Key.PageUp, Command.PageUp);
			AddKeyBinding (((int)'V' + Key.AltMask), Command.PageUp);

			AddKeyBinding (Key.PageUp | Key.ShiftMask, Command.PageUpExtend);

			AddKeyBinding (Key.N | Key.CtrlMask, Command.LineDown);
			AddKeyBinding (Key.CursorDown, Command.LineDown);

			AddKeyBinding (Key.CursorDown | Key.ShiftMask, Command.LineDownExtend);

			AddKeyBinding (Key.P | Key.CtrlMask, Command.LineUp);
			AddKeyBinding (Key.CursorUp, Command.LineUp);

			AddKeyBinding (Key.CursorUp | Key.ShiftMask, Command.LineUpExtend);

			AddKeyBinding (Key.F | Key.CtrlMask, Command.Right);
			AddKeyBinding (Key.CursorRight, Command.Right);

			AddKeyBinding (Key.CursorRight | Key.ShiftMask, Command.RightExtend);

			AddKeyBinding (Key.B | Key.CtrlMask, Command.Left);
			AddKeyBinding (Key.CursorLeft, Command.Left);

			AddKeyBinding (Key.CursorLeft | Key.ShiftMask, Command.LeftExtend);

			AddKeyBinding (Key.Delete, Command.DeleteCharLeft);
			AddKeyBinding (Key.Backspace, Command.DeleteCharLeft);

			AddKeyBinding (Key.Home, Command.StartOfLine);
			AddKeyBinding (Key.A | Key.CtrlMask, Command.StartOfLine);

			AddKeyBinding (Key.Home | Key.ShiftMask, Command.StartOfLineExtend);

			AddKeyBinding (Key.DeleteChar, Command.DeleteCharRight);
			AddKeyBinding (Key.D | Key.CtrlMask, Command.DeleteCharRight);

			AddKeyBinding (Key.End, Command.EndOfLine);
			AddKeyBinding (Key.E | Key.CtrlMask, Command.EndOfLine);

			AddKeyBinding (Key.End | Key.ShiftMask, Command.EndOfLineExtend);

			AddKeyBinding (Key.K | Key.CtrlMask, Command.CutToEndLine); // kill-to-end
			AddKeyBinding (Key.DeleteChar | Key.CtrlMask | Key.ShiftMask, Command.CutToEndLine); // kill-to-end

			AddKeyBinding (Key.K | Key.AltMask, Command.CutToStartLine); // kill-to-start
			AddKeyBinding (Key.Backspace | Key.CtrlMask | Key.ShiftMask, Command.CutToStartLine); // kill-to-start

			AddKeyBinding (Key.Y | Key.CtrlMask, Command.Paste); // Control-y, yank
			AddKeyBinding (Key.Space | Key.CtrlMask, Command.ToggleExtend);

			AddKeyBinding (((int)'C' + Key.AltMask), Command.Copy);
			AddKeyBinding (Key.C | Key.CtrlMask, Command.Copy);

			AddKeyBinding (((int)'W' + Key.AltMask), Command.Cut);
			AddKeyBinding (Key.W | Key.CtrlMask, Command.Cut);
			AddKeyBinding (Key.X | Key.CtrlMask, Command.Cut);

			AddKeyBinding (Key.CursorLeft | Key.CtrlMask, Command.WordLeft);
			AddKeyBinding ((Key)((int)'B' + Key.AltMask), Command.WordLeft);

			AddKeyBinding (Key.CursorLeft | Key.CtrlMask | Key.ShiftMask, Command.WordLeftExtend);

			AddKeyBinding (Key.CursorRight | Key.CtrlMask, Command.WordRight);
			AddKeyBinding ((Key)((int)'F' + Key.AltMask), Command.WordRight);

			AddKeyBinding (Key.CursorRight | Key.CtrlMask | Key.ShiftMask, Command.WordRightExtend);
			AddKeyBinding (Key.DeleteChar | Key.CtrlMask, Command.KillWordForwards); // kill-word-forwards
			AddKeyBinding (Key.Backspace | Key.CtrlMask, Command.KillWordBackwards); // kill-word-backwards

			AddKeyBinding (Key.Enter, Command.NewLine);
			AddKeyBinding (Key.End | Key.CtrlMask, Command.BottomEnd);
			AddKeyBinding (Key.End | Key.CtrlMask | Key.ShiftMask, Command.BottomEndExtend);
			AddKeyBinding (Key.Home | Key.CtrlMask, Command.TopHome);
			AddKeyBinding (Key.Home | Key.CtrlMask | Key.ShiftMask, Command.TopHomeExtend);
			AddKeyBinding (Key.T | Key.CtrlMask, Command.SelectAll);
			AddKeyBinding (Key.InsertChar, Command.ToggleOverwrite);
			AddKeyBinding (Key.Tab, Command.Tab);
			AddKeyBinding (Key.BackTab | Key.ShiftMask, Command.BackTab);

			AddKeyBinding (Key.Tab | Key.CtrlMask, Command.NextView);
			AddKeyBinding (Application.AlternateForwardKey, Command.NextView);

			AddKeyBinding (Key.Tab | Key.CtrlMask | Key.ShiftMask, Command.PreviousView);
			AddKeyBinding (Application.AlternateBackwardKey, Command.PreviousView);

			AddKeyBinding (Key.Z | Key.CtrlMask, Command.Undo);
			AddKeyBinding (Key.R | Key.CtrlMask, Command.Redo);

			AddKeyBinding (Key.G | Key.CtrlMask, Command.DeleteAll);
			AddKeyBinding (Key.D | Key.CtrlMask | Key.ShiftMask, Command.DeleteAll);

			currentCulture = Thread.CurrentThread.CurrentUICulture;

			ContextMenu = new ContextMenu () { MenuItems = BuildContextMenuBarItem () };
			ContextMenu.KeyChanged += ContextMenu_KeyChanged;

			AddKeyBinding (ContextMenu.Key, Command.Accept);
		}

		private MenuBarItem BuildContextMenuBarItem ()
		{
			return new MenuBarItem (new MenuItem [] {
					new MenuItem (Strings.ctxSelectAll, "", () => SelectAll (), null, null, GetKeyFromCommand (Command.SelectAll)),
					new MenuItem (Strings.ctxDeleteAll, "", () => DeleteAll (), null, null, GetKeyFromCommand (Command.DeleteAll)),
					new MenuItem (Strings.ctxCopy, "", () => Copy (), null, null, GetKeyFromCommand (Command.Copy)),
					new MenuItem (Strings.ctxCut, "", () => Cut (), null, null, GetKeyFromCommand (Command.Cut)),
					new MenuItem (Strings.ctxPaste, "", () => Paste (), null, null, GetKeyFromCommand (Command.Paste)),
					new MenuItem (Strings.ctxUndo, "", () => UndoChanges (), null, null, GetKeyFromCommand (Command.Undo)),
					new MenuItem (Strings.ctxRedo, "", () => RedoChanges (), null, null, GetKeyFromCommand (Command.Redo)),
				});
		}

		private void ContextMenu_KeyChanged (object sender, KeyChangedEventArgs e)
		{
			ReplaceKeyBinding (e.OldKey, e.NewKey);
		}

		private void Model_LinesLoaded (object sender, EventArgs e)
		{
			// This call is not needed. Model_LinesLoaded gets invoked when
			// model.LoadString (value) is called. LoadString is called from one place
			// (Text.set) and historyText.Clear() is called immediately after.
			// If this call happens, HistoryText_ChangeText will get called multiple times
			// when Text is set, which is wrong.
			//historyText.Clear (Text);
		}

		private void HistoryText_ChangeText (object sender, HistoryText.HistoryTextItem obj)
		{
			SetWrapModel ();

			if (obj != null) {
				var startLine = obj.CursorPosition.Y;

				if (obj.RemovedOnAdded != null) {
					int offset;
					if (obj.IsUndoing) {
						offset = Math.Max (obj.RemovedOnAdded.Lines.Count - obj.Lines.Count, 1);
					} else {
						offset = obj.RemovedOnAdded.Lines.Count - 1;
					}
					for (int i = 0; i < offset; i++) {
						if (Lines > obj.RemovedOnAdded.CursorPosition.Y) {
							model.RemoveLine (obj.RemovedOnAdded.CursorPosition.Y);
						} else {
							break;
						}
					}
				}

				for (int i = 0; i < obj.Lines.Count; i++) {
					if (i == 0) {
						model.ReplaceLine (startLine, obj.Lines [i]);
					} else if ((obj.IsUndoing && obj.LineStatus == HistoryText.LineStatus.Removed)
							|| !obj.IsUndoing && obj.LineStatus == HistoryText.LineStatus.Added) {
						model.AddLine (startLine, obj.Lines [i]);
					} else if (Lines > obj.CursorPosition.Y + 1) {
						model.RemoveLine (obj.CursorPosition.Y + 1);
					}
					startLine++;
				}

				CursorPosition = obj.FinalCursorPosition;
			}

			UpdateWrapModel ();

			Adjust ();
			OnContentsChanged ();
		}

		void TextView_Initialized (object sender, EventArgs e)
		{
			Autocomplete.HostControl = this;

			Application.Top.AlternateForwardKeyChanged += Top_AlternateForwardKeyChanged;
			Application.Top.AlternateBackwardKeyChanged += Top_AlternateBackwardKeyChanged;
			OnContentsChanged ();
		}

		void Top_AlternateBackwardKeyChanged (object sender, KeyChangedEventArgs e)
		{
			ReplaceKeyBinding (e.OldKey, e.NewKey);
		}

		void Top_AlternateForwardKeyChanged (object sender, KeyChangedEventArgs e)
		{
			ReplaceKeyBinding (e.OldKey, e.NewKey);
		}

		/// <summary>
		/// Tracks whether the text view should be considered "used", that is, that the user has moved in the entry,
		/// so new input should be appended at the cursor position, rather than clearing the entry
		/// </summary>
		public bool Used { get; set; }

		void ResetPosition ()
		{
			topRow = leftColumn = currentRow = currentColumn = 0;
			StopSelecting ();
			ResetCursorVisibility ();
		}

		/// <summary>
		///  Sets or gets the text in the <see cref="TextView"/>.
		/// </summary>
		/// <remarks>
		/// The <see cref="TextChanged"/> event is fired whenever this property is set. Note, however,
		/// that Text is not set by <see cref="TextView"/> as the user types.
		/// </remarks>
		public override ustring Text {
			get {
				if (wordWrap) {
					return wrapManager.Model.ToString ();
				} else {
					return model.ToString ();
				}
			}

			set {
				ResetPosition ();
				model.LoadString (value);
				if (wordWrap) {
					wrapManager = new WordWrapManager (model);
					model = wrapManager.WrapModel (frameWidth, out _, out _, out _, out _);
				}
				TextChanged?.Invoke (this, EventArgs.Empty);
				SetNeedsDisplay ();

				historyText.Clear (Text);
			}
		}

		///<inheritdoc/>
		public override Rect Frame {
			get => base.Frame;
			set {
				base.Frame = value;
				WrapTextModel ();
				Adjust ();
			}
		}

		void WrapTextModel ()
		{
			if (wordWrap && wrapManager != null) {
				model = wrapManager.WrapModel (frameWidth,
					out int nRow, out int nCol,
					out int nStartRow, out int nStartCol,
					currentRow, currentColumn,
					selectionStartRow, selectionStartColumn,
					tabWidth, preserveTrailingSpaces: true);
				currentRow = nRow;
				currentColumn = nCol;
				selectionStartRow = nStartRow;
				selectionStartColumn = nStartCol;
				SetNeedsDisplay ();
			}
		}

		int frameWidth => Math.Max (Frame.Width - (RightOffset != 0 ? 2 : 1), 0);

		/// <summary>
		/// Gets or sets the top row.
		/// </summary>
		public int TopRow { get => topRow; set => topRow = Math.Max (Math.Min (value, Lines - 1), 0); }

		/// <summary>
		/// Gets or sets the left column.
		/// </summary>
		public int LeftColumn {
			get => leftColumn;
			set {
				if (value > 0 && wordWrap)
					return;
				leftColumn = Math.Max (Math.Min (value, Maxlength - 1), 0);
			}
		}

		/// <summary>
		/// Gets the maximum visible length line.
		/// </summary>
		public int Maxlength => model.GetMaxVisibleLine (topRow, topRow + Frame.Height, TabWidth);

		/// <summary>
		/// Gets the number of lines.
		/// </summary>
		public int Lines => model.Count;

		/// <summary>
		///  Sets or gets the current cursor position.
		/// </summary>
		public Point CursorPosition {
			get => new Point (currentColumn, currentRow);
			set {
				var line = model.GetLine (Math.Max (Math.Min (value.Y, model.Count - 1), 0));
				currentColumn = value.X < 0 ? 0 : value.X > line.Count ? line.Count : value.X;
				currentRow = value.Y < 0 ? 0 : value.Y > model.Count - 1
					? Math.Max (model.Count - 1, 0) : value.Y;
				SetNeedsDisplay ();
				Adjust ();
			}
		}

		/// <summary>
		/// Start column position of the selected text.
		/// </summary>
		public int SelectionStartColumn {
			get => selectionStartColumn;
			set {
				var line = model.GetLine (currentRow);
				selectionStartColumn = value < 0 ? 0 : value > line.Count ? line.Count : value;
				selecting = true;
				SetNeedsDisplay ();
				Adjust ();
			}
		}

		/// <summary>
		/// Start row position of the selected text.
		/// </summary>
		public int SelectionStartRow {
			get => selectionStartRow;
			set {
				selectionStartRow = value < 0 ? 0 : value > model.Count - 1
					? Math.Max (model.Count - 1, 0) : value;
				selecting = true;
				SetNeedsDisplay ();
				Adjust ();
			}
		}

		/// <summary>
		/// The selected text.
		/// </summary>
		public ustring SelectedText {
			get {
				if (!selecting || (model.Count == 1 && model.GetLine (0).Count == 0)) {
					return ustring.Empty;
				}

				return GetSelectedRegion ();
			}
		}

		/// <summary>
		/// Length of the selected text.
		/// </summary>
		public int SelectedLength => GetSelectedLength ();

		/// <summary>
		/// Get or sets the selecting.
		/// </summary>
		public bool Selecting {
			get => selecting;
			set => selecting = value;
		}
		/// <summary>
		/// Allows word wrap the to fit the available container width.
		/// </summary>
		public bool WordWrap {
			get => wordWrap;
			set {
				if (value == wordWrap) {
					return;
				}
				if (value && !multiline) {
					return;
				}
				wordWrap = value;
				ResetPosition ();
				if (wordWrap) {
					wrapManager = new WordWrapManager (model);
					model = wrapManager.WrapModel (frameWidth, out _, out _, out _, out _);
				} else if (!wordWrap && wrapManager != null) {
					model = wrapManager.Model;
				}
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// The bottom offset needed to use a horizontal scrollbar or for another reason.
		/// This is only needed with the keyboard navigation.
		/// </summary>
		public int BottomOffset {
			get => bottomOffset;
			set {
				if (currentRow == Lines - 1 && bottomOffset > 0 && value == 0) {
					topRow = Math.Max (topRow - bottomOffset, 0);
				}
				bottomOffset = value;
				Adjust ();
			}
		}

		/// <summary>
		/// The right offset needed to use a vertical scrollbar or for another reason.
		/// This is only needed with the keyboard navigation.
		/// </summary>
		public int RightOffset {
			get => rightOffset;
			set {
				if (!wordWrap && currentColumn == GetCurrentLine ().Count && rightOffset > 0 && value == 0) {
					leftColumn = Math.Max (leftColumn - rightOffset, 0);
				}
				rightOffset = value;
				Adjust ();
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether pressing ENTER in a <see cref="TextView"/>
		/// creates a new line of text in the view or activates the default button for the toplevel.
		/// </summary>
		public bool AllowsReturn {
			get => allowsReturn;
			set {
				allowsReturn = value;
				if (allowsReturn && !multiline) {
					Multiline = true;
				}
				if (!allowsReturn && multiline) {
					Multiline = false;
					AllowsTab = false;
				}
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Gets or sets whether the <see cref="TextView"/> inserts a tab character into the text or ignores 
		/// tab input. If set to `false` and the user presses the tab key (or shift-tab) the focus will move to the
		/// next view (or previous with shift-tab). The default is `true`; if the user presses the tab key, a tab 
		/// character will be inserted into the text.
		/// </summary>
		public bool AllowsTab {
			get => allowsTab;
			set {
				allowsTab = value;
				if (allowsTab && tabWidth == 0) {
					tabWidth = 4;
				}
				if (allowsTab && !multiline) {
					Multiline = true;
				}
				if (!allowsTab && tabWidth > 0) {
					tabWidth = 0;
				}
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Gets or sets a value indicating the number of whitespace when pressing the TAB key.
		/// </summary>
		public int TabWidth {
			get => tabWidth;
			set {
				tabWidth = Math.Max (value, 0);
				if (tabWidth > 0 && !AllowsTab) {
					AllowsTab = true;
				}
				SetNeedsDisplay ();
			}
		}

		Dim savedHeight = null;

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="TextView"/> is a multiline text view.
		/// </summary>
		public bool Multiline {
			get => multiline;
			set {
				multiline = value;
				if (multiline && !allowsTab) {
					AllowsTab = true;
				}
				if (multiline && !allowsReturn) {
					AllowsReturn = true;
				}

				if (!multiline) {
					AllowsReturn = false;
					AllowsTab = false;
					WordWrap = false;
					currentColumn = 0;
					currentRow = 0;
					savedHeight = Height;
					var lyout = LayoutStyle;
					if (LayoutStyle == LayoutStyle.Computed) {
						LayoutStyle = LayoutStyle.Absolute;
					}
					Height = 1;
					LayoutStyle = lyout;
					Autocomplete.PopupInsideContainer = false;
					SetNeedsDisplay ();
				} else if (multiline && savedHeight != null) {
					var lyout = LayoutStyle;
					if (LayoutStyle == LayoutStyle.Computed) {
						LayoutStyle = LayoutStyle.Absolute;
					}
					Height = savedHeight;
					LayoutStyle = lyout;
					Autocomplete.PopupInsideContainer = true;
					SetNeedsDisplay ();
				}
			}
		}

		/// <summary>
		/// Indicates whatever the text was changed or not.
		/// <see langword="true"/> if the text was changed <see langword="false"/> otherwise.
		/// </summary>
		public bool IsDirty => historyText.IsDirty (Text);

		/// <summary>
		/// Indicates whatever the text has history changes or not.
		/// <see langword="true"/> if the text has history changes <see langword="false"/> otherwise.
		/// </summary>
		public bool HasHistoryChanges => historyText.HasHistoryChanges;

		/// <summary>
		/// Get the <see cref="ContextMenu"/> for this view.
		/// </summary>
		public ContextMenu ContextMenu { get; private set; }

		int GetSelectedLength ()
		{
			return SelectedText.Length;
		}

		CursorVisibility savedCursorVisibility;

		void SaveCursorVisibility ()
		{
			if (desiredCursorVisibility != CursorVisibility.Invisible) {
				if (savedCursorVisibility == 0) {
					savedCursorVisibility = desiredCursorVisibility;
				}
				DesiredCursorVisibility = CursorVisibility.Invisible;
			}
		}

		void ResetCursorVisibility ()
		{
			if (savedCursorVisibility != 0) {
				DesiredCursorVisibility = savedCursorVisibility;
				savedCursorVisibility = 0;
			}
		}

		/// <summary>
		/// Loads the contents of the file into the <see cref="TextView"/>.
		/// </summary>
		/// <returns><c>true</c>, if file was loaded, <c>false</c> otherwise.</returns>
		/// <param name="path">Path to the file to load.</param>
		public bool LoadFile (string path)
		{
			bool res;
			try {
				SetWrapModel ();
				res = model.LoadFile (path);
				historyText.Clear (Text);
				ResetPosition ();
			} catch (Exception) {
				throw;
			} finally {
				UpdateWrapModel ();
				SetNeedsDisplay ();
				Adjust ();
			}
			return res;
		}

		/// <summary>
		/// Loads the contents of the stream into the <see cref="TextView"/>.
		/// </summary>
		/// <returns><c>true</c>, if stream was loaded, <c>false</c> otherwise.</returns>
		/// <param name="stream">Stream to load the contents from.</param>
		public void LoadStream (Stream stream)
		{
			model.LoadStream (stream);
			historyText.Clear (Text);
			ResetPosition ();
			SetNeedsDisplay ();
		}

		/// <summary>
		/// Closes the contents of the stream into the <see cref="TextView"/>.
		/// </summary>
		/// <returns><c>true</c>, if stream was closed, <c>false</c> otherwise.</returns>
		public bool CloseFile ()
		{
			var res = model.CloseFile ();
			ResetPosition ();
			SetNeedsDisplay ();
			return res;
		}

		/// <summary>
		///  Gets the current cursor row.
		/// </summary>
		public int CurrentRow => currentRow;

		/// <summary>
		/// Gets the cursor column.
		/// </summary>
		/// <value>The cursor column.</value>
		public int CurrentColumn => currentColumn;

		/// <summary>
		///  Positions the cursor on the current row and column
		/// </summary>
		public override void PositionCursor ()
		{
			if (!CanFocus || !Enabled) {
				return;
			}

			if (selecting) {
				var minRow = Math.Min (Math.Max (Math.Min (selectionStartRow, currentRow) - topRow, 0), Frame.Height);
				var maxRow = Math.Min (Math.Max (Math.Max (selectionStartRow, currentRow) - topRow, 0), Frame.Height);

				SetNeedsDisplay (new Rect (0, minRow, Frame.Width, maxRow));
			}
			var line = model.GetLine (currentRow);
			var col = 0;
			if (line.Count > 0) {
				for (int idx = leftColumn; idx < line.Count; idx++) {
					if (idx >= currentColumn)
						break;
					var cols = Rune.ColumnWidth (line [idx]);
					if (line [idx] == '\t') {
						cols += TabWidth + 1;
					}
					if (!TextModel.SetCol (ref col, Frame.Width, cols)) {
						col = currentColumn;
						break;
					}
				}
			}
			var posX = currentColumn - leftColumn;
			var posY = currentRow - topRow;
			if (posX > -1 && col >= posX && posX < Frame.Width - RightOffset
				&& topRow <= currentRow && posY < Frame.Height - BottomOffset) {
				ResetCursorVisibility ();
				Move (col, currentRow - topRow);
			} else {
				SaveCursorVisibility ();
			}
		}

		void ClearRegion (int left, int top, int right, int bottom)
		{
			for (int row = top; row < bottom; row++) {
				Move (left, row);
				for (int col = left; col < right; col++)
					AddRune (col, row, ' ');
			}
		}

		/// <summary>
		/// Sets the driver to the default color for the control where no text is being rendered. Defaults to <see cref="ColorScheme.Normal"/>.
		/// </summary>
		protected virtual void SetNormalColor ()
		{
			Driver.SetAttribute (GetNormalColor ());
		}

		/// <summary>
		/// Sets the <see cref="View.Driver"/> to an appropriate color for rendering the given <paramref name="idx"/> of the
		/// current <paramref name="line"/>. Override to provide custom coloring by calling <see cref="ConsoleDriver.SetAttribute(Attribute)"/>
		/// Defaults to <see cref="ColorScheme.Normal"/>.
		/// </summary>
		/// <param name="line"></param>
		/// <param name="idx"></param>
		protected virtual void SetNormalColor (List<Rune> line, int idx)
		{
			Driver.SetAttribute (GetNormalColor ());
		}

		/// <summary>
		/// Sets the <see cref="View.Driver"/> to an appropriate color for rendering the given <paramref name="idx"/> of the
		/// current <paramref name="line"/>. Override to provide custom coloring by calling <see cref="ConsoleDriver.SetAttribute(Attribute)"/>
		/// Defaults to <see cref="ColorScheme.Focus"/>.
		/// </summary>
		/// <param name="line"></param>
		/// <param name="idx"></param>
		protected virtual void SetSelectionColor (List<Rune> line, int idx)
		{
			Driver.SetAttribute (new Attribute (ColorScheme.Focus.Background, ColorScheme.Focus.Foreground));
		}

		/// <summary>
		/// Sets the <see cref="View.Driver"/> to an appropriate color for rendering the given <paramref name="idx"/> of the
		/// current <paramref name="line"/>. Override to provide custom coloring by calling <see cref="ConsoleDriver.SetAttribute(Attribute)"/>
		/// Defaults to <see cref="ColorScheme.Focus"/>.
		/// </summary>
		/// <param name="line"></param>
		/// <param name="idx"></param>
		protected virtual void SetReadOnlyColor (List<Rune> line, int idx)
		{
			Attribute attribute;
			if (ColorScheme.Disabled.Foreground == ColorScheme.Focus.Background) {
				attribute = new Attribute (ColorScheme.Focus.Foreground, ColorScheme.Focus.Background);
			} else {
				attribute = new Attribute (ColorScheme.Disabled.Foreground, ColorScheme.Focus.Background);
			}
			Driver.SetAttribute (attribute);
		}

		/// <summary>
		/// Sets the <see cref="View.Driver"/> to an appropriate color for rendering the given <paramref name="idx"/> of the
		/// current <paramref name="line"/>. Override to provide custom coloring by calling <see cref="ConsoleDriver.SetAttribute(Attribute)"/>
		/// Defaults to <see cref="ColorScheme.HotFocus"/>.
		/// </summary>
		/// <param name="line"></param>
		/// <param name="idx"></param>
		protected virtual void SetUsedColor (List<Rune> line, int idx)
		{
			Driver.SetAttribute (ColorScheme.HotFocus);
		}

		bool isReadOnly = false;

		/// <summary>
		/// Gets or sets whether the <see cref="TextView"/> is in read-only mode or not
		/// </summary>
		/// <value>Boolean value(Default false)</value>
		public bool ReadOnly {
			get => isReadOnly;
			set {
				if (value != isReadOnly) {
					isReadOnly = value;

					SetNeedsDisplay ();
					Adjust ();
				}
			}
		}

		CursorVisibility desiredCursorVisibility = CursorVisibility.Default;

		/// <summary>
		/// Get / Set the wished cursor when the field is focused
		/// </summary>
		public CursorVisibility DesiredCursorVisibility {
			get => desiredCursorVisibility;
			set {
				if (HasFocus) {
					Application.Driver.SetCursorVisibility (value);
				}

				desiredCursorVisibility = value;
				SetNeedsDisplay ();
			}
		}

		///<inheritdoc/>
		public override bool OnEnter (View view)
		{
			//TODO: Improve it by handling read only mode of the text field
			Application.Driver.SetCursorVisibility (DesiredCursorVisibility);

			return base.OnEnter (view);
		}

		///<inheritdoc/>
		public override bool OnLeave (View view)
		{
			if (Application.MouseGrabView != null && Application.MouseGrabView == this) {
				Application.UngrabMouse ();
			}

			return base.OnLeave (view);
		}

		// Returns an encoded region start..end (top 32 bits are the row, low32 the column)
		void GetEncodedRegionBounds (out long start, out long end,
			int? startRow = null, int? startCol = null, int? cRow = null, int? cCol = null)
		{
			long selection;
			long point;
			if (startRow == null || startCol == null || cRow == null || cCol == null) {
				selection = ((long)(uint)selectionStartRow << 32) | (uint)selectionStartColumn;
				point = ((long)(uint)currentRow << 32) | (uint)currentColumn;
			} else {
				selection = ((long)(uint)startRow << 32) | (uint)startCol;
				point = ((long)(uint)cRow << 32) | (uint)cCol;
			}
			if (selection > point) {
				start = point;
				end = selection;
			} else {
				start = selection;
				end = point;
			}
		}

		bool PointInSelection (int col, int row)
		{
			long start, end;
			GetEncodedRegionBounds (out start, out end);
			var q = ((long)(uint)row << 32) | (uint)col;
			return q >= start && q <= end - 1;
		}

		//
		// Returns a ustring with the text in the selected 
		// region.
		//
		ustring GetRegion (int? sRow = null, int? sCol = null, int? cRow = null, int? cCol = null, TextModel model = null)
		{
			long start, end;
			GetEncodedRegionBounds (out start, out end, sRow, sCol, cRow, cCol);
			if (start == end) {
				return ustring.Empty;
			}
			int startRow = (int)(start >> 32);
			var maxrow = ((int)(end >> 32));
			int startCol = (int)(start & 0xffffffff);
			var endCol = (int)(end & 0xffffffff);
			var line = model == null ? this.model.GetLine (startRow) : model.GetLine (startRow);

			if (startRow == maxrow)
				return StringFromRunes (line.GetRange (startCol, endCol - startCol));

			ustring res = StringFromRunes (line.GetRange (startCol, line.Count - startCol));

			for (int row = startRow + 1; row < maxrow; row++) {
				res = res + ustring.Make (Environment.NewLine) + StringFromRunes (model == null
					? this.model.GetLine (row) : model.GetLine (row));
			}
			line = model == null ? this.model.GetLine (maxrow) : model.GetLine (maxrow);
			res = res + ustring.Make (Environment.NewLine) + StringFromRunes (line.GetRange (0, endCol));
			return res;
		}

		//
		// Clears the contents of the selected region
		//
		void ClearRegion ()
		{
			SetWrapModel ();

			long start, end;
			long currentEncoded = ((long)(uint)currentRow << 32) | (uint)currentColumn;
			GetEncodedRegionBounds (out start, out end);
			int startRow = (int)(start >> 32);
			var maxrow = ((int)(end >> 32));
			int startCol = (int)(start & 0xffffffff);
			var endCol = (int)(end & 0xffffffff);
			var line = model.GetLine (startRow);

			historyText.Add (new List<List<Rune>> () { new List<Rune> (line) }, new Point (startCol, startRow));

			List<List<Rune>> removedLines = new List<List<Rune>> ();

			if (startRow == maxrow) {
				removedLines.Add (new List<Rune> (line));

				line.RemoveRange (startCol, endCol - startCol);
				currentColumn = startCol;
				if (wordWrap) {
					SetNeedsDisplay ();
				} else {
					SetNeedsDisplay (new Rect (0, startRow - topRow, Frame.Width, startRow - topRow + 1));
				}

				historyText.Add (new List<List<Rune>> (removedLines), CursorPosition, HistoryText.LineStatus.Removed);

				UpdateWrapModel ();

				return;
			}

			removedLines.Add (new List<Rune> (line));

			line.RemoveRange (startCol, line.Count - startCol);
			var line2 = model.GetLine (maxrow);
			line.AddRange (line2.Skip (endCol));
			for (int row = startRow + 1; row <= maxrow; row++) {

				removedLines.Add (new List<Rune> (model.GetLine (startRow + 1)));

				model.RemoveLine (startRow + 1);
			}
			if (currentEncoded == end) {
				currentRow -= maxrow - (startRow);
			}
			currentColumn = startCol;

			historyText.Add (new List<List<Rune>> (removedLines), CursorPosition,
				HistoryText.LineStatus.Removed);

			UpdateWrapModel ();

			SetNeedsDisplay ();
		}

		/// <summary>
		/// Select all text.
		/// </summary>
		public void SelectAll ()
		{
			if (model.Count == 0) {
				return;
			}

			StartSelecting ();
			selectionStartColumn = 0;
			selectionStartRow = 0;
			currentColumn = model.GetLine (model.Count - 1).Count;
			currentRow = model.Count - 1;
			SetNeedsDisplay ();
		}

		/// <summary>
		/// Find the next text based on the match case with the option to replace it.
		/// </summary>
		/// <param name="textToFind">The text to find.</param>
		/// <param name="gaveFullTurn"><c>true</c>If all the text was forward searched.<c>false</c>otherwise.</param>
		/// <param name="matchCase">The match case setting.</param>
		/// <param name="matchWholeWord">The match whole word setting.</param>
		/// <param name="textToReplace">The text to replace.</param>
		/// <param name="replace"><c>true</c>If is replacing.<c>false</c>otherwise.</param>
		/// <returns><c>true</c>If the text was found.<c>false</c>otherwise.</returns>
		public bool FindNextText (ustring textToFind, out bool gaveFullTurn, bool matchCase = false,
			bool matchWholeWord = false, ustring textToReplace = null, bool replace = false)
		{
			if (model.Count == 0) {
				gaveFullTurn = false;
				return false;
			}

			SetWrapModel ();
			ResetContinuousFind ();
			var foundPos = model.FindNextText (textToFind, out gaveFullTurn, matchCase, matchWholeWord);

			return SetFoundText (textToFind, foundPos, textToReplace, replace);
		}

		/// <summary>
		/// Find the previous text based on the match case with the option to replace it.
		/// </summary>
		/// <param name="textToFind">The text to find.</param>
		/// <param name="gaveFullTurn"><c>true</c>If all the text was backward searched.<c>false</c>otherwise.</param>
		/// <param name="matchCase">The match case setting.</param>
		/// <param name="matchWholeWord">The match whole word setting.</param>
		/// <param name="textToReplace">The text to replace.</param>
		/// <param name="replace"><c>true</c>If the text was found.<c>false</c>otherwise.</param>
		/// <returns><c>true</c>If the text was found.<c>false</c>otherwise.</returns>
		public bool FindPreviousText (ustring textToFind, out bool gaveFullTurn, bool matchCase = false,
			bool matchWholeWord = false, ustring textToReplace = null, bool replace = false)
		{
			if (model.Count == 0) {
				gaveFullTurn = false;
				return false;
			}

			SetWrapModel ();
			ResetContinuousFind ();
			var foundPos = model.FindPreviousText (textToFind, out gaveFullTurn, matchCase, matchWholeWord);

			return SetFoundText (textToFind, foundPos, textToReplace, replace);
		}

		/// <summary>
		/// Reset the flag to stop continuous find.
		/// </summary>
		public void FindTextChanged ()
		{
			continuousFind = false;
		}

		/// <summary>
		/// Replaces all the text based on the match case.
		/// </summary>
		/// <param name="textToFind">The text to find.</param>
		/// <param name="matchCase">The match case setting.</param>
		/// <param name="matchWholeWord">The match whole word setting.</param>
		/// <param name="textToReplace">The text to replace.</param>
		/// <returns><c>true</c>If the text was found.<c>false</c>otherwise.</returns>
		public bool ReplaceAllText (ustring textToFind, bool matchCase = false, bool matchWholeWord = false,
			ustring textToReplace = null)
		{
			if (isReadOnly || model.Count == 0) {
				return false;
			}

			SetWrapModel ();
			ResetContinuousFind ();
			var foundPos = model.ReplaceAllText (textToFind, matchCase, matchWholeWord, textToReplace);

			return SetFoundText (textToFind, foundPos, textToReplace, false, true);
		}

		bool SetFoundText (ustring text, (Point current, bool found) foundPos,
			ustring textToReplace = null, bool replace = false, bool replaceAll = false)
		{
			if (foundPos.found) {
				StartSelecting ();
				selectionStartColumn = foundPos.current.X;
				selectionStartRow = foundPos.current.Y;
				if (!replaceAll) {
					currentColumn = selectionStartColumn + text.RuneCount;
				} else {
					currentColumn = selectionStartColumn + textToReplace.RuneCount;
				}
				currentRow = foundPos.current.Y;
				if (!isReadOnly && replace) {
					Adjust ();
					ClearSelectedRegion ();
					InsertText (textToReplace);
					StartSelecting ();
					selectionStartColumn = currentColumn - textToReplace.RuneCount;
				} else {
					UpdateWrapModel ();
					SetNeedsDisplay ();
					Adjust ();
				}
				continuousFind = true;
				return foundPos.found;
			}
			UpdateWrapModel ();
			continuousFind = false;

			return foundPos.found;
		}

		void ResetContinuousFind ()
		{
			if (!continuousFind) {
				var col = selecting ? selectionStartColumn : currentColumn;
				var row = selecting ? selectionStartRow : currentRow;
				model.ResetContinuousFind (new Point (col, row));
			}
		}

		string currentCaller;

		/// <summary>
		/// Restore from original model.
		/// </summary>
		void SetWrapModel ([CallerMemberName] string caller = null)
		{
			if (currentCaller != null)
				return;

			if (wordWrap) {
				currentCaller = caller;

				currentColumn = wrapManager.GetModelColFromWrappedLines (currentRow, currentColumn);
				currentRow = wrapManager.GetModelLineFromWrappedLines (currentRow);
				selectionStartColumn = wrapManager.GetModelColFromWrappedLines (selectionStartRow, selectionStartColumn);
				selectionStartRow = wrapManager.GetModelLineFromWrappedLines (selectionStartRow);
				model = wrapManager.Model;
			}
		}

		/// <summary>
		/// Update the original model.
		/// </summary>
		void UpdateWrapModel ([CallerMemberName] string caller = null)
		{
			if (currentCaller != null && currentCaller != caller)
				return;

			if (wordWrap) {
				currentCaller = null;

				wrapManager.UpdateModel (model, out int nRow, out int nCol,
					out int nStartRow, out int nStartCol,
					currentRow, currentColumn,
					selectionStartRow, selectionStartColumn, preserveTrailingSpaces: true);
				currentRow = nRow;
				currentColumn = nCol;
				selectionStartRow = nStartRow;
				selectionStartColumn = nStartCol;
				wrapNeeded = true;
			}
			if (currentCaller != null)
				throw new InvalidOperationException ($"WordWrap settings was changed after the {currentCaller} call.");
		}

		/// <summary>
		/// Invoke the <see cref="UnwrappedCursorPosition"/> event with the unwrapped <see cref="CursorPosition"/>.
		/// </summary>
		public virtual void OnUnwrappedCursorPosition (int? cRow = null, int? cCol = null)
		{
			var row = cRow == null ? currentRow : cRow;
			var col = cCol == null ? currentColumn : cCol;
			if (cRow == null && cCol == null && wordWrap) {
				row = wrapManager.GetModelLineFromWrappedLines (currentRow);
				col = wrapManager.GetModelColFromWrappedLines (currentRow, currentColumn);
			}
			UnwrappedCursorPosition?.Invoke (this, new PointEventArgs(new Point ((int)col, (int)row)));
		}

		ustring GetSelectedRegion ()
		{
			var cRow = currentRow;
			var cCol = currentColumn;
			var startRow = selectionStartRow;
			var startCol = selectionStartColumn;
			var model = this.model;
			if (wordWrap) {
				cRow = wrapManager.GetModelLineFromWrappedLines (currentRow);
				cCol = wrapManager.GetModelColFromWrappedLines (currentRow, currentColumn);
				startRow = wrapManager.GetModelLineFromWrappedLines (selectionStartRow);
				startCol = wrapManager.GetModelColFromWrappedLines (selectionStartRow, selectionStartColumn);
				model = wrapManager.Model;
			}
			OnUnwrappedCursorPosition (cRow, cCol);
			return GetRegion (startRow, startCol, cRow, cCol, model);
		}

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			SetNormalColor ();

			var offB = OffSetBackground ();
			int right = Frame.Width + offB.width + RightOffset;
			int bottom = Frame.Height + offB.height + BottomOffset;
			var row = 0;
			for (int idxRow = topRow; idxRow < model.Count; idxRow++) {
				var line = model.GetLine (idxRow);
				int lineRuneCount = line.Count;
				var col = 0;

				Move (0, row);
				for (int idxCol = leftColumn; idxCol < lineRuneCount; idxCol++) {
					var rune = idxCol >= lineRuneCount ? ' ' : line [idxCol];
					var cols = Rune.ColumnWidth (rune);
					if (idxCol < line.Count && selecting && PointInSelection (idxCol, idxRow)) {
						SetSelectionColor (line, idxCol);
					} else if (idxCol == currentColumn && idxRow == currentRow && !selecting && !Used
						&& HasFocus && idxCol < lineRuneCount) {
						SetSelectionColor (line, idxCol);
					} else if (ReadOnly) {
						SetReadOnlyColor (line, idxCol);
					} else {
						SetNormalColor (line, idxCol);
					}

					if (rune == '\t') {
						cols += TabWidth + 1;
						if (col + cols > right) {
							cols = right - col;
						}
						for (int i = 0; i < cols; i++) {
							if (col + i < right) {
								AddRune (col + i, row, ' ');
							}
						}
					} else {
						AddRune (col, row, rune);
					}
					if (!TextModel.SetCol (ref col, bounds.Right, cols)) {
						break;
					}
					if (idxCol + 1 < lineRuneCount && col + Rune.ColumnWidth (line [idxCol + 1]) > right) {
						break;
					}
				}
				if (col < right) {
					SetNormalColor ();
					ClearRegion (col, row, right, row + 1);
				}
				row++;
			}
			if (row < bottom) {
				SetNormalColor ();
				ClearRegion (bounds.Left, row, right, bottom);
			}

			PositionCursor ();

			if (clickWithSelecting) {
				clickWithSelecting = false;
				return;
			}
			if (SelectedLength > 0)
				return;

			// draw autocomplete
			Autocomplete.GenerateSuggestions ();

			var renderAt = new Point (
				CursorPosition.X - LeftColumn,
				Autocomplete.PopupInsideContainer
					? (CursorPosition.Y + 1) - TopRow
					: 0);

			Autocomplete.RenderOverlay (renderAt);
		}

		/// <inheritdoc/>
		public override Attribute GetNormalColor ()
		{
			return Enabled ? ColorScheme.Focus : ColorScheme.Disabled;
		}

		///<inheritdoc/>
		public override bool CanFocus {
			get => base.CanFocus;
			set { base.CanFocus = value; }
		}

		void SetClipboard (ustring text)
		{
			if (text != null) {
				Clipboard.Contents = text;
			}
		}

		void AppendClipboard (ustring text)
		{
			Clipboard.Contents += text;
		}


		/// <summary>
		/// Inserts the given <paramref name="toAdd"/> text at the current cursor position
		/// exactly as if the user had just typed it
		/// </summary>
		/// <param name="toAdd">Text to add</param>
		public void InsertText (string toAdd)
		{
			foreach (var ch in toAdd) {

				Key key;

				try {
					key = (Key)ch;
				} catch (Exception) {

					throw new ArgumentException ($"Cannot insert character '{ch}' because it does not map to a Key");
				}


				InsertText (new KeyEvent () { Key = key });
			}

			if (NeedDisplay.IsEmpty) {
				PositionCursor ();
			} else {
				Adjust ();
			}
		}

		void Insert (Rune rune)
		{
			var line = GetCurrentLine ();
			if (Used) {
				line.Insert (Math.Min (currentColumn, line.Count), rune);
			} else {
				if (currentColumn < line.Count) {
					line.RemoveAt (currentColumn);
				}
				line.Insert (Math.Min (currentColumn, line.Count), rune);
			}
			var prow = currentRow - topRow;
			if (!wrapNeeded) {
				SetNeedsDisplay (new Rect (0, prow, Math.Max (Frame.Width, 0), Math.Max (prow + 1, 0)));
			}

		}

		ustring StringFromRunes (List<Rune> runes)
		{
			if (runes == null)
				throw new ArgumentNullException (nameof (runes));
			int size = 0;
			foreach (var rune in runes) {
				size += Utf8.RuneLen (rune);
			}
			var encoded = new byte [size];
			int offset = 0;
			foreach (var rune in runes) {
				offset += Utf8.EncodeRune (rune, encoded, offset);
			}
			return ustring.Make (encoded);
		}

		/// <summary>
		/// Returns the characters on the current line (where the cursor is positioned).
		/// Use <see cref="CurrentColumn"/> to determine the position of the cursor within
		/// that line
		/// </summary>
		/// <returns></returns>
		public List<Rune> GetCurrentLine () => model.GetLine (currentRow);

		void InsertText (ustring text)
		{
			if (ustring.IsNullOrEmpty (text)) {
				return;
			}

			var lines = TextModel.StringToRunes (text);

			if (lines.Count == 0) {
				return;
			}

			SetWrapModel ();

			var line = GetCurrentLine ();

			historyText.Add (new List<List<Rune>> () { new List<Rune> (line) }, CursorPosition);

			// Optimize single line
			if (lines.Count == 1) {
				line.InsertRange (currentColumn, lines [0]);
				currentColumn += lines [0].Count;

				historyText.Add (new List<List<Rune>> () { new List<Rune> (line) }, CursorPosition,
					HistoryText.LineStatus.Replaced);

				if (!wordWrap && currentColumn - leftColumn > Frame.Width) {
					leftColumn = Math.Max (currentColumn - Frame.Width + 1, 0);
				}
				if (wordWrap) {
					SetNeedsDisplay ();
				} else {
					SetNeedsDisplay (new Rect (0, currentRow - topRow, Frame.Width, Math.Max (currentRow - topRow + 1, 0)));
				}

				UpdateWrapModel ();

				OnContentsChanged ();

				return;
			}

			List<Rune> rest = null;
			int lastp = 0;

			if (model.Count > 0 && line.Count > 0 && !copyWithoutSelection) {
				// Keep a copy of the rest of the line
				var restCount = line.Count - currentColumn;
				rest = line.GetRange (currentColumn, restCount);
				line.RemoveRange (currentColumn, restCount);
			}

			// First line is inserted at the current location, the rest is appended
			line.InsertRange (currentColumn, lines [0]);
			//model.AddLine (currentRow, lines [0]);

			var addedLines = new List<List<Rune>> () { new List<Rune> (line) };

			for (int i = 1; i < lines.Count; i++) {
				model.AddLine (currentRow + i, lines [i]);

				addedLines.Add (new List<Rune> (lines [i]));
			}

			if (rest != null) {
				var last = model.GetLine (currentRow + lines.Count - 1);
				lastp = last.Count;
				last.InsertRange (last.Count, rest);

				addedLines.Last ().InsertRange (addedLines.Last ().Count, rest);
			}

			historyText.Add (addedLines, CursorPosition, HistoryText.LineStatus.Added);

			// Now adjust column and row positions
			currentRow += lines.Count - 1;
			currentColumn = rest != null ? lastp : lines [lines.Count - 1].Count;
			Adjust ();

			historyText.Add (new List<List<Rune>> () { new List<Rune> (line) }, CursorPosition,
				HistoryText.LineStatus.Replaced);

			UpdateWrapModel ();
		}

		// The column we are tracking, or -1 if we are not tracking any column
		int columnTrack = -1;

		// Tries to snap the cursor to the tracking column
		void TrackColumn ()
		{
			// Now track the column
			var line = GetCurrentLine ();
			if (line.Count < columnTrack)
				currentColumn = line.Count;
			else if (columnTrack != -1)
				currentColumn = columnTrack;
			else if (currentColumn > line.Count)
				currentColumn = line.Count;
			Adjust ();
		}

		void Adjust ()
		{
			var offB = OffSetBackground ();
			var line = GetCurrentLine ();
			bool need = !NeedDisplay.IsEmpty || wrapNeeded;
			var tSize = TextModel.DisplaySize (line, -1, -1, false, TabWidth);
			var dSize = TextModel.DisplaySize (line, leftColumn, currentColumn, true, TabWidth);
			if (!wordWrap && currentColumn < leftColumn) {
				leftColumn = currentColumn;
				need = true;
			} else if (!wordWrap && (currentColumn - leftColumn + RightOffset > Frame.Width + offB.width
				|| dSize.size + RightOffset >= Frame.Width + offB.width)) {
				leftColumn = TextModel.CalculateLeftColumn (line, leftColumn, currentColumn,
					Frame.Width + offB.width - RightOffset, TabWidth);
				need = true;
			} else if ((wordWrap && leftColumn > 0) || (dSize.size + RightOffset < Frame.Width + offB.width
				&& tSize.size + RightOffset < Frame.Width + offB.width)) {
				if (leftColumn > 0) {
					leftColumn = 0;
					need = true;
				}
			}

			if (currentRow < topRow) {
				topRow = currentRow;
				need = true;
			} else if (currentRow - topRow + BottomOffset >= Frame.Height + offB.height) {
				topRow = Math.Min (Math.Max (currentRow - Frame.Height + 1 + BottomOffset, 0), currentRow);
				need = true;
			} else if (topRow > 0 && currentRow == topRow) {
				topRow = Math.Max (topRow - 1, 0);
			}
			if (need) {
				if (wrapNeeded) {
					WrapTextModel ();
					wrapNeeded = false;
				}
				SetNeedsDisplay ();
			} else {
				PositionCursor ();
			}

			OnUnwrappedCursorPosition ();
		}

		/// <summary>
		/// Called when the contents of the TextView change. E.g. when the user types text or deletes text. Raises
		/// the <see cref="ContentsChanged"/> event.
		/// </summary>
		public virtual void OnContentsChanged ()
		{
			ContentsChanged?.Invoke (this, new ContentsChangedEventArgs (CurrentRow, CurrentColumn));
		}

		(int width, int height) OffSetBackground ()
		{
			int w = 0;
			int h = 0;
			if (SuperView?.Frame.Right - Frame.Right < 0) {
				w = SuperView.Frame.Right - Frame.Right - 1;
			}
			if (SuperView?.Frame.Bottom - Frame.Bottom < 0) {
				h = SuperView.Frame.Bottom - Frame.Bottom - 1;
			}
			return (w, h);
		}

		/// <summary>
		/// Will scroll the <see cref="TextView"/> to display the specified row at the top if <paramref name="isRow"/> is true or
		/// will scroll the <see cref="TextView"/> to display the specified column at the left if <paramref name="isRow"/> is false.
		/// </summary>
		/// <param name="idx">Row that should be displayed at the top or Column that should be displayed at the left,
		/// if the value is negative it will be reset to zero</param>
		/// <param name="isRow">If true (default) the <paramref name="idx"/> is a row, column otherwise.</param>
		public void ScrollTo (int idx, bool isRow = true)
		{
			if (idx < 0) {
				idx = 0;
			}
			if (isRow) {
				topRow = Math.Max (idx > model.Count - 1 ? model.Count - 1 : idx, 0);
			} else if (!wordWrap) {
				var maxlength = model.GetMaxVisibleLine (topRow, topRow + Frame.Height + RightOffset, TabWidth);
				leftColumn = Math.Max (!wordWrap && idx > maxlength - 1 ? maxlength - 1 : idx, 0);
			}
			SetNeedsDisplay ();
		}

		bool lastWasKill;
		bool wrapNeeded;
		bool shiftSelecting;

		///<inheritdoc/>
		public override bool ProcessKey (KeyEvent kb)
		{
			if (!CanFocus) {
				return true;
			}

			// Give autocomplete first opportunity to respond to key presses
			if (SelectedLength == 0 && Autocomplete.ProcessKey (kb)) {
				return true;
			}

			var result = InvokeKeybindings (new KeyEvent (ShortcutHelper.GetModifiersKey (kb),
				new KeyModifiers () { Alt = kb.IsAlt, Ctrl = kb.IsCtrl, Shift = kb.IsShift }));
			if (result != null)
				return (bool)result;

			ResetColumnTrack ();
			// Ignore control characters and other special keys
			if (kb.Key < Key.Space || kb.Key > Key.CharMask)
				return false;

			InsertText (kb);
			DoNeededAction ();

			return true;
		}

		void RedoChanges ()
		{
			if (ReadOnly)
				return;

			historyText.Redo ();
		}

		void UndoChanges ()
		{
			if (ReadOnly)
				return;

			historyText.Undo ();
		}

		bool ProcessMovePreviousView ()
		{
			ResetColumnTrack ();
			return MovePreviousView ();
		}

		bool ProcessMoveNextView ()
		{
			ResetColumnTrack ();
			return MoveNextView ();
		}

		void ProcessSetOverwrite ()
		{
			ResetColumnTrack ();
			SetOverwrite (!Used);
		}

		void ProcessSelectAll ()
		{
			ResetColumnTrack ();
			SelectAll ();
		}

		void MoveTopHomeExtend ()
		{
			ResetColumnTrack ();
			StartSelecting ();
			MoveHome ();
		}

		void MoveTopHome ()
		{
			ResetAllTrack ();
			if (shiftSelecting && selecting) {
				StopSelecting ();
			}
			MoveHome ();
		}

		void MoveBottomEndExtend ()
		{
			ResetAllTrack ();
			StartSelecting ();
			MoveEnd ();
		}

		void MoveBottomEnd ()
		{
			ResetAllTrack ();
			if (shiftSelecting && selecting) {
				StopSelecting ();
			}
			MoveEnd ();
		}

		void ProcessKillWordBackward ()
		{
			ResetColumnTrack ();
			KillWordBackward ();
		}

		void ProcessKillWordForward ()
		{
			ResetColumnTrack ();
			KillWordForward ();
		}

		void ProcessMoveWordForwardExtend ()
		{
			ResetAllTrack ();
			StartSelecting ();
			MoveWordForward ();
		}

		void ProcessMoveWordForward ()
		{
			ResetAllTrack ();
			if (shiftSelecting && selecting) {
				StopSelecting ();
			}
			MoveWordForward ();
		}

		void ProcessMoveWordBackwardExtend ()
		{
			ResetAllTrack ();
			StartSelecting ();
			MoveWordBackward ();
		}

		void ProcessMoveWordBackward ()
		{
			ResetAllTrack ();
			if (shiftSelecting && selecting) {
				StopSelecting ();
			}
			MoveWordBackward ();
		}

		void ProcessCut ()
		{
			ResetColumnTrack ();
			Cut ();
		}

		void ProcessCopy ()
		{
			ResetColumnTrack ();
			Copy ();
		}

		void ToggleSelecting ()
		{
			ResetColumnTrack ();
			selecting = !selecting;
			selectionStartColumn = currentColumn;
			selectionStartRow = currentRow;
		}

		void ProcessPaste ()
		{
			ResetColumnTrack ();
			if (isReadOnly)
				return;
			Paste ();
		}

		void ProcessMoveEndOfLineExtend ()
		{
			ResetAllTrack ();
			StartSelecting ();
			MoveEndOfLine ();
		}

		void ProcessMoveEndOfLine ()
		{
			ResetAllTrack ();
			if (shiftSelecting && selecting) {
				StopSelecting ();
			}
			MoveEndOfLine ();
		}

		void ProcessDeleteCharRight ()
		{
			ResetColumnTrack ();
			DeleteCharRight ();
		}

		void ProcessMoveStartOfLineExtend ()
		{
			ResetAllTrack ();
			StartSelecting ();
			MoveStartOfLine ();
		}

		void ProcessMoveStartOfLine ()
		{
			ResetAllTrack ();
			if (shiftSelecting && selecting) {
				StopSelecting ();
			}
			MoveStartOfLine ();
		}

		void ProcessDeleteCharLeft ()
		{
			ResetColumnTrack ();
			DeleteCharLeft ();
		}

		void ProcessMoveLeftExtend ()
		{
			ResetAllTrack ();
			StartSelecting ();
			MoveLeft ();
		}

		bool ProcessMoveLeft ()
		{
			// if the user presses Left (without any control keys) and they are at the start of the text
			if (currentColumn == 0 && currentRow == 0) {
				// do not respond (this lets the key press fall through to navigation system - which usually changes focus backward)
				return false;
			}

			ResetAllTrack ();
			if (shiftSelecting && selecting) {
				StopSelecting ();
			}
			MoveLeft ();
			return true;
		}

		void ProcessMoveRightExtend ()
		{
			ResetAllTrack ();
			StartSelecting ();
			MoveRight ();
		}

		bool ProcessMoveRight ()
		{
			// if the user presses Right (without any control keys)
			// determine where the last cursor position in the text is
			var lastRow = model.Count - 1;
			var lastCol = model.GetLine (lastRow).Count;

			// if they are at the very end of all the text do not respond (this lets the key press fall through to navigation system - which usually changes focus forward)
			if (currentColumn == lastCol && currentRow == lastRow) {
				return false;
			}

			ResetAllTrack ();
			if (shiftSelecting && selecting) {
				StopSelecting ();
			}
			MoveRight ();
			return true;
		}

		void ProcessMoveUpExtend ()
		{
			ResetColumnTrack ();
			StartSelecting ();
			MoveUp ();
		}

		void ProcessMoveUp ()
		{
			ResetContinuousFindTrack ();
			if (shiftSelecting && selecting) {
				StopSelecting ();
			}
			MoveUp ();
		}

		void ProcessMoveDownExtend ()
		{
			ResetColumnTrack ();
			StartSelecting ();
			MoveDown ();
		}

		void ProcessMoveDown ()
		{
			ResetContinuousFindTrack ();
			if (shiftSelecting && selecting) {
				StopSelecting ();
			}
			MoveDown ();
		}

		void ProcessPageUpExtend ()
		{
			ResetColumnTrack ();
			StartSelecting ();
			MovePageUp ();
		}

		void ProcessPageUp ()
		{
			ResetColumnTrack ();
			if (shiftSelecting && selecting) {
				StopSelecting ();
			}
			MovePageUp ();
		}

		void ProcessPageDownExtend ()
		{
			ResetColumnTrack ();
			StartSelecting ();
			MovePageDown ();
		}

		void ProcessPageDown ()
		{
			ResetColumnTrack ();
			if (shiftSelecting && selecting) {
				StopSelecting ();
			}
			MovePageDown ();
		}

		bool MovePreviousView ()
		{
			if (Application.MdiTop != null) {
				return SuperView?.FocusPrev () == true;
			}

			return false;
		}

		bool MoveNextView ()
		{
			if (Application.MdiTop != null) {
				return SuperView?.FocusNext () == true;
			}

			return false;
		}

		bool ProcessBackTab ()
		{
			ResetColumnTrack ();

			if (!AllowsTab || isReadOnly) {
				return ProcessMovePreviousView ();
			}
			if (currentColumn > 0) {
				SetWrapModel ();

				var currentLine = GetCurrentLine ();
				if (currentLine.Count > 0 && currentLine [currentColumn - 1] == '\t') {

					historyText.Add (new List<List<Rune>> () { new List<Rune> (currentLine) }, CursorPosition);

					currentLine.RemoveAt (currentColumn - 1);
					currentColumn--;

					historyText.Add (new List<List<Rune>> () { new List<Rune> (GetCurrentLine ()) }, CursorPosition,
						HistoryText.LineStatus.Replaced);
				}

				UpdateWrapModel ();
			}
			DoNeededAction ();
			return true;
		}

		bool ProcessTab ()
		{
			ResetColumnTrack ();

			if (!AllowsTab || isReadOnly) {
				return ProcessMoveNextView ();
			}
			InsertText (new KeyEvent ((Key)'\t', null));
			DoNeededAction ();
			return true;
		}

		void SetOverwrite (bool overwrite)
		{
			Used = overwrite;
			SetNeedsDisplay ();
			DoNeededAction ();
		}

		bool ProcessReturn ()
		{
			ResetColumnTrack ();

			if (!AllowsReturn || isReadOnly) {
				return false;
			}

			SetWrapModel ();

			var currentLine = GetCurrentLine ();

			historyText.Add (new List<List<Rune>> () { new List<Rune> (currentLine) }, CursorPosition);

			if (selecting) {
				ClearSelectedRegion ();
				currentLine = GetCurrentLine ();
			}
			var restCount = currentLine.Count - currentColumn;
			var rest = currentLine.GetRange (currentColumn, restCount);
			currentLine.RemoveRange (currentColumn, restCount);

			var addedLines = new List<List<Rune>> () { new List<Rune> (currentLine) };

			model.AddLine (currentRow + 1, rest);

			addedLines.Add (new List<Rune> (model.GetLine (currentRow + 1)));

			historyText.Add (addedLines, CursorPosition, HistoryText.LineStatus.Added);

			currentRow++;

			bool fullNeedsDisplay = false;
			if (currentRow >= topRow + Frame.Height) {
				topRow++;
				fullNeedsDisplay = true;
			}
			currentColumn = 0;

			historyText.Add (new List<List<Rune>> () { new List<Rune> (GetCurrentLine ()) }, CursorPosition,
				HistoryText.LineStatus.Replaced);

			if (!wordWrap && currentColumn < leftColumn) {
				fullNeedsDisplay = true;
				leftColumn = 0;
			}

			if (fullNeedsDisplay)
				SetNeedsDisplay ();
			else
				SetNeedsDisplay (new Rect (0, currentRow - topRow, 2, Frame.Height));

			UpdateWrapModel ();

			DoNeededAction ();
			OnContentsChanged ();
			return true;
		}

		void KillWordBackward ()
		{
			if (isReadOnly)
				return;

			SetWrapModel ();

			var currentLine = GetCurrentLine ();

			historyText.Add (new List<List<Rune>> () { new List<Rune> (GetCurrentLine ()) }, CursorPosition);

			if (currentColumn == 0) {
				DeleteTextBackwards ();

				historyText.ReplaceLast (new List<List<Rune>> () { new List<Rune> (GetCurrentLine ()) }, CursorPosition,
					HistoryText.LineStatus.Replaced);

				UpdateWrapModel ();

				return;
			}
			var newPos = WordBackward (currentColumn, currentRow);
			if (newPos.HasValue && currentRow == newPos.Value.row) {
				var restCount = currentColumn - newPos.Value.col;
				currentLine.RemoveRange (newPos.Value.col, restCount);
				if (wordWrap) {
					wrapNeeded = true;
				}
				currentColumn = newPos.Value.col;
			} else if (newPos.HasValue) {
				var restCount = currentLine.Count - currentColumn;
				currentLine.RemoveRange (currentColumn, restCount);
				if (wordWrap) {
					wrapNeeded = true;
				}
				currentColumn = newPos.Value.col;
				currentRow = newPos.Value.row;
			}

			historyText.Add (new List<List<Rune>> () { new List<Rune> (GetCurrentLine ()) }, CursorPosition,
				HistoryText.LineStatus.Replaced);

			UpdateWrapModel ();

			if (wrapNeeded) {
				SetNeedsDisplay ();
			} else {
				SetNeedsDisplay (new Rect (0, currentRow - topRow, Frame.Width, Frame.Height));
			}
			DoNeededAction ();
		}

		void KillWordForward ()
		{
			if (isReadOnly)
				return;

			SetWrapModel ();

			var currentLine = GetCurrentLine ();

			historyText.Add (new List<List<Rune>> () { new List<Rune> (GetCurrentLine ()) }, CursorPosition);

			if (currentLine.Count == 0 || currentColumn == currentLine.Count) {
				DeleteTextForwards ();

				historyText.ReplaceLast (new List<List<Rune>> () { new List<Rune> (GetCurrentLine ()) }, CursorPosition,
					HistoryText.LineStatus.Replaced);

				UpdateWrapModel ();

				return;
			}
			var newPos = WordForward (currentColumn, currentRow);
			var restCount = 0;
			if (newPos.HasValue && currentRow == newPos.Value.row) {
				restCount = newPos.Value.col - currentColumn;
				currentLine.RemoveRange (currentColumn, restCount);
			} else if (newPos.HasValue) {
				restCount = currentLine.Count - currentColumn;
				currentLine.RemoveRange (currentColumn, restCount);
			}
			if (wordWrap) {
				wrapNeeded = true;
			}

			historyText.Add (new List<List<Rune>> () { new List<Rune> (GetCurrentLine ()) }, CursorPosition,
				HistoryText.LineStatus.Replaced);

			UpdateWrapModel ();

			if (wrapNeeded) {
				SetNeedsDisplay ();
			} else {
				SetNeedsDisplay (new Rect (0, currentRow - topRow, Frame.Width, Frame.Height));
			}
			DoNeededAction ();
		}

		void MoveWordForward ()
		{
			var newPos = WordForward (currentColumn, currentRow);
			if (newPos.HasValue) {
				currentColumn = newPos.Value.col;
				currentRow = newPos.Value.row;
			}
			Adjust ();
			DoNeededAction ();
		}

		void MoveWordBackward ()
		{
			var newPos = WordBackward (currentColumn, currentRow);
			if (newPos.HasValue) {
				currentColumn = newPos.Value.col;
				currentRow = newPos.Value.row;
			}
			Adjust ();
			DoNeededAction ();
		}

		void KillToStartOfLine ()
		{
			if (isReadOnly)
				return;
			if (model.Count == 1 && GetCurrentLine ().Count == 0) {
				// Prevents from adding line feeds if there is no more lines.
				return;
			}

			SetWrapModel ();

			var currentLine = GetCurrentLine ();
			var setLastWasKill = true;
			if (currentLine.Count > 0 && currentColumn == 0) {
				UpdateWrapModel ();

				DeleteTextBackwards ();
				return;
			}

			historyText.Add (new List<List<Rune>> () { new List<Rune> (currentLine) }, CursorPosition);

			if (currentLine.Count == 0) {
				if (currentRow > 0) {
					model.RemoveLine (currentRow);

					if (model.Count > 0 || lastWasKill) {
						var val = ustring.Make (Environment.NewLine);
						if (lastWasKill) {
							AppendClipboard (val);
						} else {
							SetClipboard (val);
						}
					}
					if (model.Count == 0) {
						// Prevents from adding line feeds if there is no more lines.
						setLastWasKill = false;
					}

					currentRow--;
					currentLine = model.GetLine (currentRow);

					var removedLine = new List<List<Rune>> () { new List<Rune> (currentLine) };

					removedLine.Add (new List<Rune> ());

					historyText.Add (new List<List<Rune>> (removedLine), CursorPosition, HistoryText.LineStatus.Removed);

					currentColumn = currentLine.Count;
				}
			} else {
				var restCount = currentColumn;
				var rest = currentLine.GetRange (0, restCount);
				var val = ustring.Empty;
				val += StringFromRunes (rest);
				if (lastWasKill) {
					AppendClipboard (val);
				} else {
					SetClipboard (val);
				}
				currentLine.RemoveRange (0, restCount);
				currentColumn = 0;
			}

			historyText.Add (new List<List<Rune>> () { new List<Rune> (GetCurrentLine ()) }, CursorPosition,
				HistoryText.LineStatus.Replaced);

			UpdateWrapModel ();

			if (wrapNeeded)
				SetNeedsDisplay ();
			else
				SetNeedsDisplay (new Rect (0, currentRow - topRow, Frame.Width, Frame.Height));
			lastWasKill = setLastWasKill;
			DoNeededAction ();
		}

		void KillToEndOfLine ()
		{
			if (isReadOnly)
				return;
			if (model.Count == 1 && GetCurrentLine ().Count == 0) {
				// Prevents from adding line feeds if there is no more lines.
				return;
			}

			SetWrapModel ();

			var currentLine = GetCurrentLine ();
			var setLastWasKill = true;
			if (currentLine.Count > 0 && currentColumn == currentLine.Count) {
				UpdateWrapModel ();

				DeleteTextForwards ();
				return;
			}

			historyText.Add (new List<List<Rune>> () { new List<Rune> (currentLine) }, CursorPosition);

			if (currentLine.Count == 0) {
				if (currentRow < model.Count - 1) {
					var removedLines = new List<List<Rune>> () { new List<Rune> (currentLine) };

					model.RemoveLine (currentRow);

					removedLines.Add (new List<Rune> (GetCurrentLine ()));

					historyText.Add (new List<List<Rune>> (removedLines), CursorPosition,
						HistoryText.LineStatus.Removed);
				}
				if (model.Count > 0 || lastWasKill) {
					var val = ustring.Make (Environment.NewLine);
					if (lastWasKill) {
						AppendClipboard (val);
					} else {
						SetClipboard (val);
					}
				}
				if (model.Count == 0) {
					// Prevents from adding line feeds if there is no more lines.
					setLastWasKill = false;
				}
			} else {
				var restCount = currentLine.Count - currentColumn;
				var rest = currentLine.GetRange (currentColumn, restCount);
				var val = ustring.Empty;
				val += StringFromRunes (rest);
				if (lastWasKill) {
					AppendClipboard (val);
				} else {
					SetClipboard (val);
				}
				currentLine.RemoveRange (currentColumn, restCount);
			}

			historyText.Add (new List<List<Rune>> () { new List<Rune> (GetCurrentLine ()) }, CursorPosition,
				HistoryText.LineStatus.Replaced);

			UpdateWrapModel ();

			if (wrapNeeded)
				SetNeedsDisplay ();
			else
				SetNeedsDisplay (new Rect (0, currentRow - topRow, Frame.Width, Frame.Height));
			lastWasKill = setLastWasKill;
			DoNeededAction ();
		}

		void MoveEndOfLine ()
		{
			var currentLine = GetCurrentLine ();
			currentColumn = currentLine.Count;
			Adjust ();
			DoNeededAction ();
		}

		void MoveStartOfLine ()
		{
			currentColumn = 0;
			leftColumn = 0;
			Adjust ();
			DoNeededAction ();
		}

		/// <summary>
		/// Deletes all the selected or a single character at right from the position of the cursor.
		/// </summary>
		public void DeleteCharRight ()
		{
			if (isReadOnly)
				return;

			SetWrapModel ();

			if (selecting) {
				historyText.Add (new List<List<Rune>> () { new List<Rune> (GetCurrentLine ()) }, CursorPosition,
					HistoryText.LineStatus.Original);

				ClearSelectedRegion ();

				var currentLine = GetCurrentLine ();

				historyText.Add (new List<List<Rune>> () { new List<Rune> (currentLine) }, CursorPosition,
					HistoryText.LineStatus.Replaced);

				UpdateWrapModel ();

				return;
			}
			if (DeleteTextForwards ()) {
				UpdateWrapModel ();

				return;
			}

			UpdateWrapModel ();

			DoNeededAction ();
		}

		/// <summary>
		/// Deletes all the selected or a single character at left from the position of the cursor.
		/// </summary>
		public void DeleteCharLeft ()
		{
			if (isReadOnly)
				return;

			SetWrapModel ();

			if (selecting) {
				historyText.Add (new List<List<Rune>> () { new List<Rune> (GetCurrentLine ()) }, CursorPosition,
					HistoryText.LineStatus.Original);

				ClearSelectedRegion ();

				var currentLine = GetCurrentLine ();

				historyText.Add (new List<List<Rune>> () { new List<Rune> (currentLine) }, CursorPosition,
					HistoryText.LineStatus.Replaced);

				UpdateWrapModel ();

				return;
			}
			if (DeleteTextBackwards ()) {
				UpdateWrapModel ();

				return;
			}

			UpdateWrapModel ();

			DoNeededAction ();
		}

		void MoveLeft ()
		{
			if (currentColumn > 0) {
				currentColumn--;
			} else {
				if (currentRow > 0) {
					currentRow--;
					if (currentRow < topRow) {
						topRow--;
						SetNeedsDisplay ();
					}
					var currentLine = GetCurrentLine ();
					currentColumn = currentLine.Count;
				}
			}
			Adjust ();
			DoNeededAction ();
		}

		void MoveRight ()
		{
			var currentLine = GetCurrentLine ();
			if (currentColumn < currentLine.Count) {
				currentColumn++;
			} else {
				if (currentRow + 1 < model.Count) {
					currentRow++;
					currentColumn = 0;
					if (currentRow >= topRow + Frame.Height) {
						topRow++;
						SetNeedsDisplay ();
					}
				}
			}
			Adjust ();
			DoNeededAction ();
		}

		void MovePageUp ()
		{
			int nPageUpShift = Frame.Height - 1;
			if (currentRow > 0) {
				if (columnTrack == -1)
					columnTrack = currentColumn;
				currentRow = currentRow - nPageUpShift < 0 ? 0 : currentRow - nPageUpShift;
				if (currentRow < topRow) {
					topRow = topRow - nPageUpShift < 0 ? 0 : topRow - nPageUpShift;
					SetNeedsDisplay ();
				}
				TrackColumn ();
				PositionCursor ();
			}
			DoNeededAction ();
		}

		void MovePageDown ()
		{
			int nPageDnShift = Frame.Height - 1;
			if (currentRow >= 0 && currentRow < model.Count) {
				if (columnTrack == -1)
					columnTrack = currentColumn;
				currentRow = (currentRow + nPageDnShift) > model.Count
					? model.Count > 0 ? model.Count - 1 : 0
					: currentRow + nPageDnShift;
				if (topRow < currentRow - nPageDnShift) {
					topRow = currentRow >= model.Count ? currentRow - nPageDnShift : topRow + nPageDnShift;
					SetNeedsDisplay ();
				}
				TrackColumn ();
				PositionCursor ();
			}
			DoNeededAction ();
		}

		void ResetContinuousFindTrack ()
		{
			// Handle some state here - whether the last command was a kill
			// operation and the column tracking (up/down)
			lastWasKill = false;
			continuousFind = false;
		}

		void ResetColumnTrack ()
		{
			// Handle some state here - whether the last command was a kill
			// operation and the column tracking (up/down)
			lastWasKill = false;
			columnTrack = -1;
		}

		void ResetAllTrack ()
		{
			// Handle some state here - whether the last command was a kill
			// operation and the column tracking (up/down)
			lastWasKill = false;
			columnTrack = -1;
			continuousFind = false;
		}

		bool InsertText (KeyEvent kb)
		{
			//So that special keys like tab can be processed
			if (isReadOnly)
				return true;

			SetWrapModel ();

			historyText.Add (new List<List<Rune>> () { new List<Rune> (GetCurrentLine ()) }, CursorPosition);

			if (selecting) {
				ClearSelectedRegion ();
			}
			if (kb.Key == Key.Enter) {
				model.AddLine (currentRow + 1, new List<Rune> ());
				currentRow++;
				currentColumn = 0;
			} else if ((uint)kb.Key == 13) {
				currentColumn = 0;
			} else {
				if (Used) {
					Insert ((uint)kb.Key);
					currentColumn++;
					if (currentColumn >= leftColumn + Frame.Width) {
						leftColumn++;
						SetNeedsDisplay ();
					}
				} else {
					Insert ((uint)kb.Key);
					currentColumn++;
				}
			}

			historyText.Add (new List<List<Rune>> () { new List<Rune> (GetCurrentLine ()) }, CursorPosition,
				HistoryText.LineStatus.Replaced);

			UpdateWrapModel ();
			OnContentsChanged ();

			return true;
		}

		void ShowContextMenu ()
		{
			if (currentCulture != Thread.CurrentThread.CurrentUICulture) {

				currentCulture = Thread.CurrentThread.CurrentUICulture;

				ContextMenu.MenuItems = BuildContextMenuBarItem ();
			}
			ContextMenu.Show ();
		}

		/// <summary>
		/// Deletes all text.
		/// </summary>
		public void DeleteAll ()
		{
			if (Lines == 0) {
				return;
			}

			selectionStartColumn = 0;
			selectionStartRow = 0;
			MoveBottomEndExtend ();
			DeleteCharLeft ();
			SetNeedsDisplay ();
		}

		///<inheritdoc/>
		public override bool OnKeyUp (KeyEvent kb)
		{
			switch (kb.Key) {
			case Key.Space | Key.CtrlMask:
				return true;
			}

			return false;
		}

		void DoNeededAction ()
		{
			if (NeedDisplay.IsEmpty) {
				PositionCursor ();
			} else {
				Adjust ();
			}
		}

		bool DeleteTextForwards ()
		{
			SetWrapModel ();

			var currentLine = GetCurrentLine ();
			if (currentColumn == currentLine.Count) {
				if (currentRow + 1 == model.Count) {
					UpdateWrapModel ();

					return true;
				}

				historyText.Add (new List<List<Rune>> () { new List<Rune> (currentLine) }, CursorPosition);

				var removedLines = new List<List<Rune>> () { new List<Rune> (currentLine) };

				var nextLine = model.GetLine (currentRow + 1);

				removedLines.Add (new List<Rune> (nextLine));

				historyText.Add (removedLines, CursorPosition, HistoryText.LineStatus.Removed);

				currentLine.AddRange (nextLine);
				model.RemoveLine (currentRow + 1);

				historyText.Add (new List<List<Rune>> () { new List<Rune> (currentLine) }, CursorPosition,
					HistoryText.LineStatus.Replaced);

				if (wordWrap) {
					wrapNeeded = true;
				}
				if (wrapNeeded) {
					SetNeedsDisplay ();
				} else {
					var sr = currentRow - topRow;
					SetNeedsDisplay (new Rect (0, sr, Frame.Width, sr + 1));
				}
			} else {
				historyText.Add (new List<List<Rune>> () { new List<Rune> (currentLine) }, CursorPosition);

				currentLine.RemoveAt (currentColumn);

				historyText.Add (new List<List<Rune>> () { new List<Rune> (currentLine) }, CursorPosition,
					HistoryText.LineStatus.Replaced);

				if (wordWrap) {
					wrapNeeded = true;
				}

				if (wrapNeeded) {
					SetNeedsDisplay ();
				} else {
					var r = currentRow - topRow;
					SetNeedsDisplay (new Rect (currentColumn - leftColumn, r, Frame.Width, r + 1));
				}
			}

			UpdateWrapModel ();

			return false;
		}

		bool DeleteTextBackwards ()
		{
			SetWrapModel ();

			if (currentColumn > 0) {
				// Delete backwards 
				var currentLine = GetCurrentLine ();

				historyText.Add (new List<List<Rune>> () { new List<Rune> (currentLine) }, CursorPosition);

				currentLine.RemoveAt (currentColumn - 1);
				if (wordWrap) {
					wrapNeeded = true;
				}
				currentColumn--;

				historyText.Add (new List<List<Rune>> () { new List<Rune> (currentLine) }, CursorPosition,
					HistoryText.LineStatus.Replaced);

				if (currentColumn < leftColumn) {
					leftColumn--;
					SetNeedsDisplay ();
				} else
					SetNeedsDisplay (new Rect (0, currentRow - topRow, 1, Frame.Width));
			} else {
				// Merges the current line with the previous one.
				if (currentRow == 0)
					return true;
				var prowIdx = currentRow - 1;
				var prevRow = model.GetLine (prowIdx);

				historyText.Add (new List<List<Rune>> () { new List<Rune> (prevRow) }, CursorPosition);

				List<List<Rune>> removedLines = new List<List<Rune>> () { new List<Rune> (prevRow) };

				removedLines.Add (new List<Rune> (GetCurrentLine ()));

				historyText.Add (removedLines, new Point (currentColumn, prowIdx),
					HistoryText.LineStatus.Removed);

				var prevCount = prevRow.Count;
				model.GetLine (prowIdx).AddRange (GetCurrentLine ());
				model.RemoveLine (currentRow);
				if (wordWrap) {
					wrapNeeded = true;
				}
				currentRow--;

				historyText.Add (new List<List<Rune>> () { GetCurrentLine () }, new Point (currentColumn, prowIdx),
					HistoryText.LineStatus.Replaced);

				currentColumn = prevCount;
				SetNeedsDisplay ();
			}

			UpdateWrapModel ();

			return false;
		}

		bool copyWithoutSelection;

		/// <summary>
		/// Copy the selected text to the clipboard contents.
		/// </summary>
		public void Copy ()
		{
			SetWrapModel ();
			if (selecting) {
				SetClipboard (GetRegion ());
				copyWithoutSelection = false;
			} else {
				var currentLine = GetCurrentLine ();
				SetClipboard (ustring.Make (currentLine));
				copyWithoutSelection = true;
			}
			UpdateWrapModel ();
			DoNeededAction ();
		}

		/// <summary>
		/// Cut the selected text to the clipboard contents.
		/// </summary>
		public void Cut ()
		{
			SetWrapModel ();
			SetClipboard (GetRegion ());
			if (!isReadOnly) {
				ClearRegion ();

				historyText.Add (new List<List<Rune>> () { new List<Rune> (GetCurrentLine ()) }, CursorPosition,
					HistoryText.LineStatus.Replaced);
			}
			UpdateWrapModel ();
			selecting = false;
			DoNeededAction ();
			OnContentsChanged ();
		}

		/// <summary>
		/// Paste the clipboard contents into the current selected position.
		/// </summary>
		public void Paste ()
		{
			if (isReadOnly) {
				return;
			}

			SetWrapModel ();
			var contents = Clipboard.Contents;
			if (copyWithoutSelection && contents.FirstOrDefault (x => x == '\n' || x == '\r') == 0) {
				var runeList = contents == null ? new List<Rune> () : contents.ToRuneList ();
				var currentLine = GetCurrentLine ();

				historyText.Add (new List<List<Rune>> () { new List<Rune> (currentLine) }, CursorPosition);

				var addedLine = new List<List<Rune>> () { new List<Rune> (currentLine) };

				addedLine.Add (runeList);

				historyText.Add (new List<List<Rune>> (addedLine), CursorPosition, HistoryText.LineStatus.Added);

				model.AddLine (currentRow, runeList);
				currentRow++;

				historyText.Add (new List<List<Rune>> () { new List<Rune> (GetCurrentLine ()) }, CursorPosition,
					HistoryText.LineStatus.Replaced);
				OnContentsChanged ();
			} else {
				if (selecting) {
					ClearRegion ();
				}
				copyWithoutSelection = false;
				InsertText (contents);

				if (selecting) {
					historyText.ReplaceLast (new List<List<Rune>> () { new List<Rune> (GetCurrentLine ()) }, CursorPosition,
						HistoryText.LineStatus.Original);
				}
			}
			UpdateWrapModel ();
			selecting = false;
			DoNeededAction ();
		}

		void StartSelecting ()
		{
			if (shiftSelecting && selecting) {
				return;
			}
			shiftSelecting = true;
			selecting = true;
			selectionStartColumn = currentColumn;
			selectionStartRow = currentRow;
		}

		void StopSelecting ()
		{
			shiftSelecting = false;
			selecting = false;
			isButtonShift = false;
		}

		void ClearSelectedRegion ()
		{
			SetWrapModel ();
			if (!isReadOnly) {
				ClearRegion ();
			}
			UpdateWrapModel ();
			selecting = false;
			DoNeededAction ();
		}

		void MoveUp ()
		{
			if (currentRow > 0) {
				if (columnTrack == -1) {
					columnTrack = currentColumn;
				}
				currentRow--;
				if (currentRow < topRow) {
					topRow--;
					SetNeedsDisplay ();
				}
				TrackColumn ();
				PositionCursor ();
			}
			DoNeededAction ();
		}

		void MoveDown ()
		{
			if (currentRow + 1 < model.Count) {
				if (columnTrack == -1) {
					columnTrack = currentColumn;
				}
				currentRow++;
				if (currentRow + BottomOffset >= topRow + Frame.Height) {
					topRow++;
					SetNeedsDisplay ();
				}
				TrackColumn ();
				PositionCursor ();
			} else if (currentRow > Frame.Height) {
				Adjust ();
			}
			DoNeededAction ();
		}

		IEnumerable<(int col, int row, Rune rune)> ForwardIterator (int col, int row)
		{
			if (col < 0 || row < 0)
				yield break;
			if (row >= model.Count)
				yield break;
			var line = GetCurrentLine ();
			if (col >= line.Count)
				yield break;

			while (row < model.Count) {
				for (int c = col; c < line.Count; c++) {
					yield return (c, row, line [c]);
				}
				col = 0;
				row++;
				line = GetCurrentLine ();
			}
		}

		Rune RuneAt (int col, int row)
		{
			var line = model.GetLine (row);
			if (line.Count > 0) {
				return line [col > line.Count - 1 ? line.Count - 1 : col];
			} else {
				return 0;
			}
		}

		/// <summary>
		/// Will scroll the <see cref="TextView"/> to the last line and position the cursor there.
		/// </summary>
		public void MoveEnd ()
		{
			currentRow = model.Count - 1;
			var line = GetCurrentLine ();
			currentColumn = line.Count;
			TrackColumn ();
			PositionCursor ();
		}

		/// <summary>
		/// Will scroll the <see cref="TextView"/> to the first line and position the cursor there.
		/// </summary>
		public void MoveHome ()
		{
			currentRow = 0;
			topRow = 0;
			currentColumn = 0;
			leftColumn = 0;
			TrackColumn ();
			PositionCursor ();
		}

		bool MoveNext (ref int col, ref int row, out Rune rune)
		{
			var line = model.GetLine (row);
			if (col + 1 < line.Count) {
				col++;
				rune = line [col];
				if (col + 1 == line.Count && !Rune.IsLetterOrDigit (rune)
					&& !Rune.IsWhiteSpace (line [col - 1])) {
					col++;
				}
				return true;
			} else if (col + 1 == line.Count) {
				col++;
			}
			while (row + 1 < model.Count) {
				col = 0;
				row++;
				line = model.GetLine (row);
				if (line.Count > 0) {
					rune = line [0];
					return true;
				}
			}
			rune = 0;
			return false;
		}

		bool MovePrev (ref int col, ref int row, out Rune rune)
		{
			var line = model.GetLine (row);

			if (col > 0) {
				col--;
				rune = line [col];
				return true;
			}
			if (row == 0) {
				rune = 0;
				return false;
			}
			while (row > 0) {
				row--;
				line = model.GetLine (row);
				col = line.Count - 1;
				if (col >= 0) {
					rune = line [col];
					return true;
				}
			}
			rune = 0;
			return false;
		}

		(int col, int row)? WordForward (int fromCol, int fromRow)
		{
			var col = fromCol;
			var row = fromRow;
			try {
				var rune = RuneAt (col, row);

				void ProcMoveNext (ref int nCol, ref int nRow, Rune nRune)
				{
					if (Rune.IsSymbol (nRune) || Rune.IsWhiteSpace (nRune)) {
						while (MoveNext (ref nCol, ref nRow, out nRune)) {
							if (Rune.IsLetterOrDigit (nRune) || Rune.IsPunctuation (nRune))
								return;
						}
						if (nRow != fromRow && (Rune.IsLetterOrDigit (nRune) || Rune.IsPunctuation (nRune))) {
							return;
						}
						while (MoveNext (ref nCol, ref nRow, out nRune)) {
							if (!Rune.IsLetterOrDigit (nRune) && !Rune.IsPunctuation (nRune))
								break;
						}
					} else {
						if (!MoveNext (ref nCol, ref nRow, out nRune)) {
							return;
						}

						var line = model.GetLine (fromRow);
						if ((nRow != fromRow && fromCol < line.Count)
							|| (nRow == fromRow && nCol == line.Count - 1)) {
							nCol = line.Count;
							nRow = fromRow;
							return;
						} else if (nRow != fromRow && fromCol == line.Count) {
							line = model.GetLine (nRow);
							if (Rune.IsLetterOrDigit (line [nCol]) || Rune.IsPunctuation (line [nCol])) {
								return;
							}
						}
						ProcMoveNext (ref nCol, ref nRow, nRune);
					}
				}

				ProcMoveNext (ref col, ref row, rune);

				if (fromCol != col || fromRow != row)
					return (col, row);
				return null;
			} catch (Exception) {
				return null;
			}
		}

		(int col, int row)? WordBackward (int fromCol, int fromRow)
		{
			if (fromRow == 0 && fromCol == 0)
				return null;

			var col = Math.Max (fromCol - 1, 0);
			var row = fromRow;
			try {
				var rune = RuneAt (col, row);
				int lastValidCol = Rune.IsLetterOrDigit (rune) || Rune.IsPunctuation (rune) ? col : -1;

				void ProcMovePrev (ref int nCol, ref int nRow, Rune nRune)
				{
					if (Rune.IsSymbol (nRune) || Rune.IsWhiteSpace (nRune)) {
						while (MovePrev (ref nCol, ref nRow, out nRune)) {
							if (Rune.IsLetterOrDigit (nRune) || Rune.IsPunctuation (nRune)) {
								lastValidCol = nCol;
								break;
							}
						}
						if (nRow != fromRow && (Rune.IsLetterOrDigit (nRune) || Rune.IsPunctuation (nRune))) {
							if (lastValidCol > -1) {
								nCol = lastValidCol;
							}
							return;
						}
						while (MovePrev (ref nCol, ref nRow, out nRune)) {
							if (!Rune.IsLetterOrDigit (nRune) && !Rune.IsPunctuation (nRune))
								break;
							if (nRow != fromRow) {
								break;
							}
							lastValidCol = nCol;
						}
						if (lastValidCol > -1) {
							nCol = lastValidCol;
							nRow = fromRow;
						}
					} else {
						if (!MovePrev (ref nCol, ref nRow, out nRune)) {
							return;
						}

						var line = model.GetLine (nRow);
						if (nCol == 0 && nRow == fromRow && (Rune.IsLetterOrDigit (line [0]) || Rune.IsPunctuation (line [0]))) {
							return;
						}
						lastValidCol = Rune.IsLetterOrDigit (nRune) || Rune.IsPunctuation (nRune) ? nCol : lastValidCol;
						if (lastValidCol > -1 && (Rune.IsSymbol (nRune) || Rune.IsWhiteSpace (nRune))) {
							nCol = lastValidCol;
							return;
						}
						if (fromRow != nRow) {
							nCol = line.Count;
							return;
						}
						ProcMovePrev (ref nCol, ref nRow, nRune);
					}
				}

				ProcMovePrev (ref col, ref row, rune);

				if (fromCol != col || fromRow != row)
					return (col, row);
				return null;
			} catch (Exception) {
				return null;
			}
		}

		bool isButtonShift;
		bool clickWithSelecting;

		///<inheritdoc/>
		public override bool MouseEvent (MouseEvent ev)
		{
			if (!ev.Flags.HasFlag (MouseFlags.Button1Clicked) && !ev.Flags.HasFlag (MouseFlags.Button1Pressed)
				&& !ev.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition)
				&& !ev.Flags.HasFlag (MouseFlags.Button1Released)
				&& !ev.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ButtonShift)
				&& !ev.Flags.HasFlag (MouseFlags.WheeledDown) && !ev.Flags.HasFlag (MouseFlags.WheeledUp)
				&& !ev.Flags.HasFlag (MouseFlags.Button1DoubleClicked)
				&& !ev.Flags.HasFlag (MouseFlags.Button1DoubleClicked | MouseFlags.ButtonShift)
				&& !ev.Flags.HasFlag (MouseFlags.Button1TripleClicked)
				&& !ev.Flags.HasFlag (ContextMenu.MouseFlags)) {
				return false;
			}

			if (!CanFocus) {
				return true;
			}

			if (!HasFocus) {
				SetFocus ();
			}

			continuousFind = false;

			// Give autocomplete first opportunity to respond to mouse clicks
			if (SelectedLength == 0 && Autocomplete.MouseEvent (ev, true)) {
				return true;
			}

			if (ev.Flags == MouseFlags.Button1Clicked) {
				if (shiftSelecting && !isButtonShift) {
					StopSelecting ();
				}
				ProcessMouseClick (ev, out _);
				PositionCursor ();
				lastWasKill = false;
				columnTrack = currentColumn;
			} else if (ev.Flags == MouseFlags.WheeledDown) {
				lastWasKill = false;
				columnTrack = currentColumn;
				ScrollTo (topRow + 1);
			} else if (ev.Flags == MouseFlags.WheeledUp) {
				lastWasKill = false;
				columnTrack = currentColumn;
				ScrollTo (topRow - 1);
			} else if (ev.Flags == MouseFlags.WheeledRight) {
				lastWasKill = false;
				columnTrack = currentColumn;
				ScrollTo (leftColumn + 1, false);
			} else if (ev.Flags == MouseFlags.WheeledLeft) {
				lastWasKill = false;
				columnTrack = currentColumn;
				ScrollTo (leftColumn - 1, false);
			} else if (ev.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition)) {
				ProcessMouseClick (ev, out List<Rune> line);
				PositionCursor ();
				if (model.Count > 0 && shiftSelecting && selecting) {
					if (currentRow - topRow + BottomOffset >= Frame.Height - 1
						&& model.Count + BottomOffset > topRow + currentRow) {
						ScrollTo (topRow + Frame.Height);
					} else if (topRow > 0 && currentRow <= topRow) {
						ScrollTo (topRow - Frame.Height);
					} else if (ev.Y >= Frame.Height) {
						ScrollTo (model.Count + BottomOffset);
					} else if (ev.Y < 0 && topRow > 0) {
						ScrollTo (0);
					}
					if (currentColumn - leftColumn + RightOffset >= Frame.Width - 1
						&& line.Count + RightOffset > leftColumn + currentColumn) {
						ScrollTo (leftColumn + Frame.Width, false);
					} else if (leftColumn > 0 && currentColumn <= leftColumn) {
						ScrollTo (leftColumn - Frame.Width, false);
					} else if (ev.X >= Frame.Width) {
						ScrollTo (line.Count + RightOffset, false);
					} else if (ev.X < 0 && leftColumn > 0) {
						ScrollTo (0, false);
					}
				}
				lastWasKill = false;
				columnTrack = currentColumn;
			} else if (ev.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ButtonShift)) {
				if (!shiftSelecting) {
					isButtonShift = true;
					StartSelecting ();
				}
				ProcessMouseClick (ev, out _);
				PositionCursor ();
				lastWasKill = false;
				columnTrack = currentColumn;
			} else if (ev.Flags.HasFlag (MouseFlags.Button1Pressed)) {
				if (shiftSelecting) {
					clickWithSelecting = true;
					StopSelecting ();
				}
				ProcessMouseClick (ev, out _);
				PositionCursor ();
				if (!selecting) {
					StartSelecting ();
				}
				lastWasKill = false;
				columnTrack = currentColumn;
				if (Application.MouseGrabView == null) {
					Application.GrabMouse (this);
				}
			} else if (ev.Flags.HasFlag (MouseFlags.Button1Released)) {
				Application.UngrabMouse ();
			} else if (ev.Flags.HasFlag (MouseFlags.Button1DoubleClicked)) {
				if (ev.Flags.HasFlag (MouseFlags.ButtonShift)) {
					if (!selecting) {
						StartSelecting ();
					}
				} else if (selecting) {
					StopSelecting ();
				}
				ProcessMouseClick (ev, out List<Rune> line);
				(int col, int row)? newPos;
				if (currentColumn == line.Count || (currentColumn > 0 && (line [currentColumn - 1] != ' '
					|| line [currentColumn] == ' '))) {

					newPos = WordBackward (currentColumn, currentRow);
					if (newPos.HasValue) {
						currentColumn = currentRow == newPos.Value.row ? newPos.Value.col : 0;
					}
				}
				if (!selecting) {
					StartSelecting ();
				}
				newPos = WordForward (currentColumn, currentRow);
				if (newPos != null && newPos.HasValue) {
					currentColumn = currentRow == newPos.Value.row ? newPos.Value.col : line.Count;
				}
				PositionCursor ();
				lastWasKill = false;
				columnTrack = currentColumn;
			} else if (ev.Flags.HasFlag (MouseFlags.Button1TripleClicked)) {
				if (selecting) {
					StopSelecting ();
				}
				ProcessMouseClick (ev, out List<Rune> line);
				currentColumn = 0;
				if (!selecting) {
					StartSelecting ();
				}
				currentColumn = line.Count;
				PositionCursor ();
				lastWasKill = false;
				columnTrack = currentColumn;
			} else if (ev.Flags == ContextMenu.MouseFlags) {
				ContextMenu.Position = new Point (ev.X + 2, ev.Y + 2);
				ShowContextMenu ();
			}

			return true;
		}

		void ProcessMouseClick (MouseEvent ev, out List<Rune> line)
		{
			List<Rune> r = null;
			if (model.Count > 0) {
				var maxCursorPositionableLine = Math.Max ((model.Count - 1) - topRow, 0);
				if (Math.Max (ev.Y, 0) > maxCursorPositionableLine) {
					currentRow = maxCursorPositionableLine + topRow;
				} else {
					currentRow = Math.Max (ev.Y + topRow, 0);
				}
				r = GetCurrentLine ();
				var idx = TextModel.GetColFromX (r, leftColumn, Math.Max (ev.X, 0), TabWidth);
				if (idx - leftColumn >= r.Count + RightOffset) {
					currentColumn = Math.Max (r.Count - leftColumn + RightOffset, 0);
				} else {
					currentColumn = idx + leftColumn;
				}
			}

			line = r;
		}

		/// <summary>
		/// Allows clearing the <see cref="HistoryText.HistoryTextItem"/> items updating the original text.
		/// </summary>
		public void ClearHistoryChanges ()
		{
			historyText?.Clear (Text);
		}
	}


	/// <summary>
	/// Renders an overlay on another view at a given point that allows selecting
	/// from a range of 'autocomplete' options.
	/// An implementation on a TextView.
	/// </summary>
	public class TextViewAutocomplete : Autocomplete {

		///<inheritdoc/>
		protected override string GetCurrentWord (int columnOffset = 0)
		{
			var host = (TextView)HostControl;
			var currentLine = host.GetCurrentLine ();
			var cursorPosition = Math.Min (host.CurrentColumn + columnOffset, currentLine.Count);
			return IdxToWord (currentLine, cursorPosition, columnOffset);
		}

		/// <inheritdoc/>
		protected override void DeleteTextBackwards ()
		{
			((TextView)HostControl).DeleteCharLeft ();
		}

		/// <inheritdoc/>
		protected override void InsertText (string accepted)
		{
			((TextView)HostControl).InsertText (accepted);
		}
	}
}
