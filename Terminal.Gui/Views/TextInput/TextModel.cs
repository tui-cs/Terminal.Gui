namespace Terminal.Gui.Views;

/// <summary>
///     Represents the underlying data model for managing and manipulating multi-line text in a <see cref="TextView"/>.
/// </summary>
/// <remarks>
///     The <see cref="TextModel"/> class provides functionality for storing, retrieving, and modifying lines of text,
///     as well as supporting operations like word navigation, text search, and file loading. It is used internally
///     by text input controls such as <see cref="TextView"/> to manage text content.
/// </remarks>
internal class TextModel
{
    private List<List<Cell>> _lines = [];
    private (Point startPointToFind, Point currentPointToFind, bool found) _toFind;

    // Cached max visible line width to avoid O(N×L) rescans on every layout/scroll.
    private int _cachedMaxWidth = -1;
    private int _cachedMaxWidthTabWidth = -1;
    private Dictionary<int, int> _cachedMaxWidthPerLine = [];

    /// <summary>
    ///     Gets the number of times <see cref="GetMaxVisibleLine"/> performed a full line scan.
    ///     Used for performance testing. Reset via <see cref="ResetMaxVisibleLineCallCount"/>.
    /// </summary>
    internal int MaxVisibleLineScanCount { get; private set; }

    /// <summary>The number of text lines in the model</summary>
    public int Count => _lines.Count;

    public string? FilePath { get; set; }

    /// <summary>Resets the <see cref="MaxVisibleLineScanCount"/> counter to zero.</summary>
    internal void ResetMaxVisibleLineCallCount () => MaxVisibleLineScanCount = 0;

    /// <summary>Invalidates the cached max line width so the next call to <see cref="GetMaxVisibleLine"/> will rescan.</summary>
    internal void InvalidateMaxWidthCache () => _cachedMaxWidth = -1;

    /// <summary>
    ///   Determines whether the max width cache should be invalidated based on the line being modified, the column width of the modification, and whether it's an insert or delete operation.
    /// </summary>
    /// <param name="line">The line number being modified.</param>
    /// <param name="isInsert">Indicates whether the operation is an insert. Defaults to true.</param>
    /// <param name="columnWidth">The width of the column being modified. Defaults to -1 on delete.</param>
    /// <returns><see langword="true"/> if the cache should be invalidated; otherwise, <see langword="false"/>.</returns>
    internal bool ShouldInvalidateMaxWidthCache (int line, bool isInsert = true, int columnWidth = -1)
    {
        if (_cachedMaxWidth < 0)
        {
            return true;
        }

        if (isInsert)
        {
            if (_cachedMaxWidthPerLine.TryGetValue (line, out int cachedLineWidth) && columnWidth > cachedLineWidth)
            {
                return true;
            }
        }
        else
        {
            if (_cachedMaxWidthPerLine.Count == 1 && _cachedMaxWidthPerLine.ContainsKey (line))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Adds a line to the model at the specified position.</summary>
    /// <param name="pos">Line number where the line will be inserted.</param>
    /// <param name="cells">The line of text and color, as a List of Cell.</param>
    public void AddLine (int pos, List<Cell> cells)
    {
        _lines.Insert (pos, cells);
        InvalidateMaxWidthCache ();
    }

    public bool CloseFile ()
    {
        if (FilePath is null)
        {
            throw new ArgumentNullException (nameof (FilePath));
        }

        FilePath = null;
        _lines = [];
        InvalidateMaxWidthCache ();

        return true;
    }

    public List<List<Cell>> GetAllLines () => _lines;

    /// <summary>Returns the specified line as a List of Rune</summary>
    /// <returns>The line.</returns>
    /// <param name="line">Line number to retrieve.</param>
    public List<Cell> GetLine (int line)
    {
        if (_lines.Count > 0)
        {
            if (line < Count)
            {
                return _lines [line];
            }

            return _lines [Count - 1];
        }

        _lines.Add ([]);

        return _lines [0];
    }

    /// <summary>Returns the maximum line length of the visible lines.</summary>
    /// <param name="first">The first line.</param>
    /// <param name="last">The last line.</param>
    /// <param name="tabWidth">The tab width.</param>
    public int GetMaxVisibleLine (int first, int last, int tabWidth)
    {
        // Return cached value if scanning the full range and cache is valid
        if (first == 0 && last >= _lines.Count && _cachedMaxWidth >= 0 && _cachedMaxWidthTabWidth == tabWidth)
        {
            return _cachedMaxWidth;
        }

        MaxVisibleLineScanCount++;

        var maxLength = 0;
        last = last < _lines.Count ? last : _lines.Count;

        // When scanning the full range, reset cached max width per line
        if (first == 0 && last >= _lines.Count)
        {
            _cachedMaxWidthPerLine = [];
        }

        for (int i = first; i < last; i++)
        {
            List<Cell> line = GetLine (i);
            int colsWidth = CursorColumn (CellsToStringList (line), line.Count, tabWidth, out _, out _);

            if (colsWidth > maxLength)
            {
                maxLength = colsWidth;

                // Cache max width per line when scanning the full range, and remove cached widths for lines that are no longer the max
                if (first != 0 || last < _lines.Count)
                {
                    continue;
                }
                _cachedMaxWidthPerLine = new Dictionary<int, int> { { i, maxLength } };
            }
            else if (maxLength == colsWidth)
            {
                _cachedMaxWidthPerLine [i] = maxLength;
            }
        }

        // Cache the result when scanning the full range
        if (first == 0 && last >= _lines.Count)
        {
            _cachedMaxWidth = maxLength;
            _cachedMaxWidthTabWidth = tabWidth;
        }

        return maxLength;
    }

    public event EventHandler? LinesLoaded;

    public bool LoadFile (string file)
    {
        FilePath = file ?? throw new ArgumentNullException (nameof (file));

        using FileStream stream = File.OpenRead (file);

        LoadStream (stream);

        return true;
    }

    public void LoadListCells (List<List<Cell>> cellsList, Attribute? attribute)
    {
        _lines = cellsList;
        SetAttributes (attribute);
        InvalidateMaxWidthCache ();
        OnLinesLoaded ();
    }

    public void LoadCells (List<Cell> cells, Attribute? attribute)
    {
        _lines = Cell.ToCells (cells);
        SetAttributes (attribute);
        InvalidateMaxWidthCache ();
        OnLinesLoaded ();
    }

    public void LoadStream (Stream input)
    {
        ArgumentNullException.ThrowIfNull (input);

        _lines = [];
        var buff = new BufferedStream (input);
        int v;
        List<byte> line = [];
        var wasNewLine = false;

        while ((v = buff.ReadByte ()) != -1)
        {
            if (v == 13)
            {
                continue;
            }

            if (v == 10)
            {
                Append (line);
                line.Clear ();
                wasNewLine = true;

                continue;
            }

            line.Add ((byte)v);
            wasNewLine = false;
        }

        if (line.Count > 0 || wasNewLine)
        {
            Append (line);
        }

        buff.Dispose ();

        InvalidateMaxWidthCache ();
        OnLinesLoaded ();
    }

    public void LoadString (string content)
    {
        _lines = Cell.StringToLinesOfCells (content);
        InvalidateMaxWidthCache ();

        OnLinesLoaded ();
    }

    internal static Key CreateKeyFromGrapheme (string grapheme)
    {
        StringRuneEnumerator enumerator = grapheme.EnumerateRunes ();

        if (enumerator.MoveNext ())
        {
            Rune rune = enumerator.Current;

            if (!enumerator.MoveNext ())
            {
                return new Key (rune.Value) { AssociatedText = grapheme };
            }
        }

        return new Key { AssociatedText = grapheme };
    }

    internal static IEnumerable<string> GetInsertableGraphemes (string text)
    {
        foreach (string grapheme in GraphemeHelper.GetGraphemes (text))
        {
            yield return ContainsInvalidSurrogate (grapheme) ? Rune.ReplacementChar.ToString () : grapheme;
        }
    }

    /// <summary>Removes the line at the specified position</summary>
    /// <param name="pos">Position.</param>
    public void RemoveLine (int pos)
    {
        if (_lines.Count > 0)
        {
            if (_lines is [{ Count: 0 }])
            {
                return;
            }

            _lines.RemoveAt (pos);
            InvalidateMaxWidthCache ();
        }
    }

    public void ReplaceLine (int pos, List<Cell> runes)
    {
        if (_lines.Count > 0 && pos < _lines.Count)
        {
            _lines [pos] = [.. runes];
        }
        else if (_lines.Count == 0 || (_lines.Count > 0 && pos >= _lines.Count))
        {
            _lines.Add (runes);
        }

        InvalidateMaxWidthCache ();
    }

    public override string ToString ()
    {
        var sb = new StringBuilder ();

        for (var i = 0; i < _lines.Count; i++)
        {
            sb.Append (Cell.ToString (_lines [i]));

            if (i + 1 < _lines.Count)
            {
                sb.AppendLine ();
            }
        }

        return sb.ToString ();
    }

    private static bool ContainsInvalidSurrogate (string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            char current = text [i];

            if (char.IsHighSurrogate (current))
            {
                if (i + 1 < text.Length && char.IsLowSurrogate (text [i + 1]))
                {
                    i++;

                    continue;
                }

                return true;
            }

            if (char.IsLowSurrogate (current))
            {
                return true;
            }
        }

        return false;
    }

    public (int col, int row)? WordBackward (int fromCol, int fromRow, bool useSameRuneType)
    {
        if (fromRow == 0 && fromCol == 0)
        {
            return null;
        }

        int col = Math.Max (fromCol - 1, 0);
        int row = fromRow;

        try
        {
            Cell? cell = RuneAt (col, row);
            Rune rune;

            if (cell is { })
            {
                rune = Rune.GetRuneAt (cell.Value.Grapheme, 0);
            }
            else
            {
                if (col > 0)
                {
                    return (col, row);
                }

                if (row <= 0)
                {
                    return null;
                }

                row--;
                List<Cell> line = GetLine (row);

                return (line.Count, row);
            }

            RuneType runeType = GetRuneType (rune);

            int lastValidCol = IsSameRuneType (rune, runeType, useSameRuneType)
                               && (Rune.IsLetterOrDigit (rune) || Rune.IsPunctuation (rune) || Rune.IsSymbol (rune))
                                   ? col
                                   : -1;

            ProcMovePrev (ref col, ref row, rune);

            if (fromCol != col || fromRow != row)
            {
                return (col, row);
            }

            if (fromCol == col && fromRow == row && row > 0)
            {
                row--;
                List<Cell> line = GetLine (row);
                col = line.Count;

                return (col, row);
            }

            return null;

            void ProcMovePrev (ref int nCol, ref int nRow, Rune nRune)
            {
                if (Rune.IsWhiteSpace (nRune))
                {
                    while (MovePrev (ref nCol, ref nRow, out nRune, useSameRuneType))
                    {
                        lastValidCol = nCol;

                        if (Rune.IsLetterOrDigit (nRune) || Rune.IsPunctuation (nRune) || Rune.IsSymbol (nRune))
                        {
                            rune = nRune;
                            runeType = GetRuneType (nRune);
                        }
                    }

                    if (lastValidCol > -1)
                    {
                        nCol = lastValidCol;
                        nRow = fromRow;
                    }

                    if ((!Rune.IsWhiteSpace (nRune) && Rune.IsWhiteSpace (rune)) || (Rune.IsWhiteSpace (nRune) && !Rune.IsWhiteSpace (rune)))
                    {
                        return;
                    }

                    if (nRow == fromRow || (!Rune.IsLetterOrDigit (nRune) && !Rune.IsPunctuation (nRune) && !Rune.IsSymbol (nRune)))
                    {
                        return;
                    }

                    List<Cell> line = GetLine (nRow);

                    if (lastValidCol > -1)
                    {
                        nCol = lastValidCol + Math.Max (lastValidCol, line.Count);
                    }
                }
                else
                {
                    if (!MovePrev (ref nCol, ref nRow, out nRune, useSameRuneType))
                    {
                        if (lastValidCol > -1)
                        {
                            nCol = lastValidCol;
                            nRow = fromRow;
                        }

                        return;
                    }

                    List<Cell> line = GetLine (nRow);
                    var firstRune = Rune.GetRuneAt (line [0].Grapheme, 0);

                    if (nCol == 0 && nRow == fromRow && (Rune.IsLetterOrDigit (firstRune) || Rune.IsPunctuation (firstRune) || Rune.IsSymbol (firstRune)))
                    {
                        return;
                    }

                    lastValidCol = (IsSameRuneType (nRune, runeType, useSameRuneType) && Rune.IsLetterOrDigit (nRune))
                                   || Rune.IsPunctuation (nRune)
                                   || Rune.IsSymbol (nRune)
                                       ? nCol
                                       : lastValidCol;

                    if (lastValidCol > -1 && Rune.IsWhiteSpace (nRune))
                    {
                        nCol = lastValidCol;

                        return;
                    }

                    if (fromRow != nRow)
                    {
                        nCol = line.Count;

                        return;
                    }

                    ProcMovePrev (ref nCol, ref nRow, nRune);
                }
            }
        }
        catch (Exception)
        {
            return null;
        }
    }

    public (int col, int row)? WordForward (int fromCol, int fromRow, bool useSameRuneType)
    {
        if (fromRow == _lines.Count - 1 && fromCol == GetLine (_lines.Count - 1).Count)
        {
            return null;
        }

        int col = fromCol;
        int row = fromRow;

        try
        {
            Rune rune = _lines [row].Count > 0 ? Rune.GetRuneAt (RuneAt (col, row)!.Value.Grapheme, 0) : default (Rune);
            RuneType runeType = GetRuneType (rune);

            int lastValidCol;

            ProcMoveNext (ref col, ref row, rune);

            if (fromCol != col || fromRow != row)
            {
                return (col, row);
            }

            return null;

            void ProcMoveNext (ref int nCol, ref int nRow, Rune nRune)
            {
                if (Rune.IsWhiteSpace (nRune))
                {
                    while (MoveNext (ref nCol, ref nRow, out nRune, useSameRuneType))
                    {
                        lastValidCol = nCol;

                        if (Rune.IsLetterOrDigit (nRune) || Rune.IsPunctuation (nRune) || Rune.IsSymbol (nRune))
                        {
                            return;
                        }
                    }

                    lastValidCol = nCol;

                    if (!Rune.IsWhiteSpace (nRune) && Rune.IsWhiteSpace (rune))
                    {
                        return;
                    }

                    if (nRow != fromRow && (Rune.IsLetterOrDigit (nRune) || Rune.IsPunctuation (nRune) || Rune.IsSymbol (nRune)))
                    {
                        if (lastValidCol > -1)
                        {
                            nCol = lastValidCol;
                        }

                        return;
                    }

                    if (lastValidCol > -1)
                    {
                        nCol = lastValidCol;
                        nRow = fromRow;
                    }
                }
                else
                {
                    if (!MoveNext (ref nCol, ref nRow, out nRune, useSameRuneType))
                    {
                        return;
                    }

                    lastValidCol = nCol;

                    if (!IsSameRuneType (nRune, runeType, useSameRuneType) && !Rune.IsWhiteSpace (nRune))
                    {
                        return;
                    }

                    List<Cell> line = GetLine (nRow);
                    var firstRune = Rune.GetRuneAt (line [0].Grapheme, 0);

                    if (nCol == line.Count
                        && nRow == fromRow
                        && (Rune.IsLetterOrDigit (firstRune) || Rune.IsPunctuation (firstRune) || Rune.IsSymbol (firstRune)))
                    {
                        return;
                    }

                    if (fromRow != nRow)
                    {
                        nCol = 0;

                        return;
                    }

                    ProcMoveNext (ref nCol, ref nRow, nRune);
                }
            }
        }
        catch (Exception)
        {
            return null;
        }
    }

    public (int startCol, int col, int row)? ProcessDoubleClickSelection (int fromStartCol, int fromCol, int fromRow, bool useSameRuneType, bool selectWordOnly)
    {
        List<Cell> line = GetLine (fromRow);

        int startCol = fromStartCol;
        int col = fromCol;
        int row = fromRow;

        (int col, int row)? newPos = WordForward (col, row, useSameRuneType);

        if (newPos.HasValue)
        {
            col = row == newPos.Value.row ? newPos.Value.col : 0;
        }

        if (startCol > 0
            && StringExtensions.ToString (line.GetRange (startCol, Math.Max (col - startCol, 0)).Select (c => c.Grapheme).ToList ()).Trim () == ""
            && (col - startCol > 1 || (col - startCol > 0 && line [startCol - 1].Grapheme == " ")))
        {
            while (startCol > 0 && line [startCol - 1].Grapheme == " ")
            {
                startCol--;
            }
        }
        else
        {
            newPos = WordBackward (col, row, useSameRuneType);

            if (newPos is { })
            {
                startCol = row == newPos.Value.row ? newPos.Value.col : line.Count;
            }
        }

        if (selectWordOnly)
        {
            List<string> selText = line.GetRange (startCol, col - startCol).Select (c => c.Grapheme).ToList ();

            if (StringExtensions.ToString (selText).Trim () != "")
            {
                for (int i = selText.Count - 1; i > -1; i--)
                {
                    if (selText [i] == " ")
                    {
                        col--;
                    }
                }
            }
        }

        if (fromStartCol != startCol || fromCol != col || fromRow != row)
        {
            return (startCol, col, row);
        }

        return null;
    }

    internal static int CalculateLeftColumn (List<Cell> t, int start, int end, int width, int tabWidth = 0)
    {
        List<string> strings = CellsToStringList (t);

        return CalculateLeftColumn (strings, start, end, width, tabWidth);
    }

    internal static List<string> CellsToStringList (List<Cell> t)
    {
        List<string> strings = [];

        foreach (Cell cell in t)
        {
            strings.Add (cell.Grapheme);
        }

        return strings;
    }

    // Returns the left column in a range of the string.
    internal static int CalculateLeftColumn (List<string> t, int start, int end, int width, int tabWidth = 0)
    {
        if (t.Count == 0)
        {
            return 0;
        }

        // 1. Calculate absolute cursor position and store each glyph width
        int cursorColumn = CursorColumn (t, end, tabWidth, out List<int> glyphWidths, out List<int> excludedGlyphWidths);
        _ = GetColumnWidthsBeforeStart (glyphWidths, Math.Max (start, 0), out _, out int startIndex);

        // 2. LEFT NAVIGATION (Location 0 jump)
        if (Math.Min (end, t.Count) > 0 && (Math.Min (end, t.Count) <= startIndex || Math.Max (width, 0) > cursorColumn))
        {
            int maxGlyphWith = MaxGlyphWith (Math.Max (startIndex - 1, 0), -1);

            if (cursorColumn - 1 > Math.Max (start, 0) - maxGlyphWith)
            {
                return Math.Max (Math.Max (start, 0) - maxGlyphWith, 0);
            }

            return Math.Max (cursorColumn - 1, 0);
        }

        // 3. RIGHT NAVIGATION / END KEY
        if (cursorColumn >= Math.Max (start, 0) + Math.Max (width, 0)
            || cursorColumn - Math.Max (width, 0) >= 0
            || cursorColumn < Math.Max (start, 0) + Math.Max (width, 0))
        {
            // Standard right-line navigation
            int startCol = cursorColumn - Math.Max (width, 0);

            if (startCol < 0)
            {
                return 0;
            }

            _ = GetColumnWidthsBeforeStart (glyphWidths, startCol, out int clipOffset, out startIndex);
            int maxGlyphWith = MaxGlyphWith (startIndex - clipOffset, 1);

            if (cursorColumn + maxGlyphWith < startCol + Math.Max (width, 0))
            {
                return startCol;
            }

            if (cursorColumn + 1 <= Math.Min (end, t.Count) + maxGlyphWith)
            {
                return Math.Max (0, Math.Max (startCol, 0) + maxGlyphWith + clipOffset);
            }

            return startCol + 1;
        }

        return Math.Max (start, 0);

        int MaxGlyphWith (int startIdx, int direction)
        {
            int glyphIdx = startIdx;
            var startGlyphWith = 1;
            var lastGlyphWith = 1;

            if (direction == -1)
            {
                startGlyphWith = glyphIdx < glyphWidths.Count ? glyphWidths [glyphIdx] : 1;
            }
            else if (startIdx > 0)
            {
                startGlyphWith = glyphIdx < glyphWidths.Count ? glyphWidths [glyphIdx] : 1;
            }

            glyphIdx = startIndex + direction + Math.Max (width, 0);

            if (glyphIdx < glyphWidths.Count)
            {
                lastGlyphWith = glyphWidths [glyphIdx];
            }
            else if (excludedGlyphWidths.Count > 0)
            {
                if (direction == -1)
                {
                    glyphIdx -= glyphWidths.Count - direction;
                }
                else
                {
                    glyphIdx -= glyphWidths.Count + direction;
                }
                lastGlyphWith = glyphIdx > -1 && glyphIdx < excludedGlyphWidths.Count ? excludedGlyphWidths [glyphIdx] : 1;
            }

            int maxGlyphWith = Math.Max (startGlyphWith, lastGlyphWith);

            return maxGlyphWith;
        }
    }

    internal static int CursorColumn (List<string> t, int end, int tabWidth, out List<int> glyphWidths, out List<int> excludedGlyphWidths)
    {
        var cursorColumn = 0;
        glyphWidths = [];
        excludedGlyphWidths = [];

        // Calculate absolute cursor position and store each glyph width
        for (var i = 0; i < t.Count; i++)
        {
            int gWidth = t [i] == "\t"

                             // Calculate columns to next tab stop
                             // Tab stops are at multiples of TabWidth (0, 4, 8, 12, ...)
                             // If we're at visual column col, advance to next tab stop
                             ? tabWidth > 0
                                   ? Math.Max (tabWidth, 0) - cursorColumn % Math.Max (tabWidth, 0)

                                   // When TabWidth is 0, tabs are invisible (0 columns)
                                   : 0

                             // Ensures that cols less than 0 to be 1 because it will be converted to a printable rune
                             : Math.Max (t [i].GetColumns (), 1);

            if (i >= Math.Min (end, t.Count))
            {
                excludedGlyphWidths.Add (gWidth);

                continue;
            }

            glyphWidths.Add (gWidth);
            cursorColumn += gWidth;
        }

        return cursorColumn;
    }

    internal static int GetColumnWidthsBeforeStart (List<int>? glyphsWidths, int start, out int clipOffset, out int startIndex)
    {
        clipOffset = 0;
        startIndex = 0;

        if (glyphsWidths == null || glyphsWidths.Count == 0 || start <= 0)
        {
            return 0;
        }

        var sum = 0;
        var prev = 0;

        for (var i = 0; i < glyphsWidths.Count; i++)
        {
            sum += glyphsWidths [i];

            if (sum > start)
            {
                clipOffset = prev - start;
                startIndex = i;

                return prev;
            }

            prev = sum;
        }

        // If start is beyond the line
        clipOffset = sum - start;
        startIndex = glyphsWidths.Count;

        return sum;
    }

    internal static (int size, int length) DisplaySize (List<Cell> t, int start = -1, int end = -1, bool checkNextText = true, int tabWidth = 0)
    {
        List<string> strings = new ();

        foreach (Cell cell in t)
        {
            strings.Add (cell.Grapheme);
        }

        return DisplaySize (strings, start, end, checkNextText, tabWidth);
    }

    // Returns the size and length in a range of the string.
    internal static (int size, int length) DisplaySize (List<string> t, int start = -1, int end = -1, bool checkNextRune = true, int tabWidth = 0)
    {
        if (t.Count == 0)
        {
            return (0, 0);
        }

        var size = 0;
        var len = 0;

        int tCount = end <= -1 ? t.Count : end > t.Count ? t.Count : end;
        int i = start <= -1 ? 0 : start;

        for (; i < tCount; i++)
        {
            string text = t [i];
            int colWidth = text.GetColumns ();
            size += colWidth;
            len += text.Length;

            if (text == "\t" && tabWidth > 0)
            {
                size += tabWidth - size % tabWidth;
                len += tabWidth - 1;
            }
            else if (colWidth < 1)
            {
                size += 1;
            }

            if (checkNextRune && i == tCount - 1 && t.Count > tCount && IsWideText (t [i + 1], tabWidth, out int s, out int l))
            {
                size += s;
                len += l;
            }
        }

        return (size, len);

        bool IsWideText (string s1, int tWidth, out int s, out int l)
        {
            s = s1.GetColumns ();
            l = Encoding.Unicode.GetByteCount (s1);

            if (s1 == "\t")
            {
                s += tabWidth - s % tabWidth;
                l += tWidth - 1;
            }

            return s > 1;
        }
    }

    internal (Point current, bool found) FindNextText (string text, out bool gaveFullTurn, bool matchCase = false, bool matchWholeWord = false)
    {
        if (string.IsNullOrEmpty (text) || _lines.Count == 0)
        {
            gaveFullTurn = false;

            return (Point.Empty, false);
        }

        if (_toFind.found)
        {
            _toFind.currentPointToFind.X++;
        }

        (Point current, bool found) foundPos = GetFoundNextTextPoint (text, _lines.Count, matchCase, matchWholeWord, _toFind.currentPointToFind);

        if (!foundPos.found && _toFind.currentPointToFind != _toFind.startPointToFind)
        {
            foundPos = GetFoundNextTextPoint (text, _toFind.startPointToFind.Y + 1, matchCase, matchWholeWord, Point.Empty);
        }

        gaveFullTurn = ApplyToFind (foundPos);

        return foundPos;
    }

    internal (Point current, bool found) FindPreviousText (string text, out bool gaveFullTurn, bool matchCase = false, bool matchWholeWord = false)
    {
        if (string.IsNullOrEmpty (text) || _lines.Count == 0)
        {
            gaveFullTurn = false;

            return (Point.Empty, false);
        }

        if (_toFind.found)
        {
            _toFind.currentPointToFind.X++;
        }

        int linesCount = _toFind.currentPointToFind.IsEmpty ? _lines.Count - 1 : _toFind.currentPointToFind.Y;

        (Point current, bool found) foundPos = GetFoundPreviousTextPoint (text, linesCount, matchCase, matchWholeWord, _toFind.currentPointToFind);

        if (!foundPos.found && _toFind.currentPointToFind != _toFind.startPointToFind)
        {
            foundPos = GetFoundPreviousTextPoint (text, _lines.Count - 1, matchCase, matchWholeWord, new Point (_lines [^1].Count, _lines.Count));
        }

        gaveFullTurn = ApplyToFind (foundPos);

        return foundPos;
    }

    internal static int GetColFromX (List<Cell> t, int start, int x, int tabWidth = 0)
    {
        List<string> strings = new ();

        foreach (Cell cell in t)
        {
            strings.Add (cell.Grapheme);
        }

        return GetColFromX (strings, start, x, tabWidth);
    }

    internal static int GetColFromX (List<string> t, int start, int x, int tabWidth = 0)
    {
        if (x < 0)
        {
            return x;
        }

        int size = start;
        int pX = x + start;
        _ = CursorColumn (t, t.Count, tabWidth, out List<int> glyphWidths, out _);
        _ = GetColumnWidthsBeforeStart (glyphWidths, start, out _, out int startIndex);

        for (int i = startIndex; i < t.Count; i++)
        {
            string s = t [i];
            size += s.GetColumns ();

            if (s == "\t")
            {
                size += tabWidth - size % tabWidth;
            }

            if (i == pX)
            {
                return i - startIndex;
            }
        }

        return t.Count - startIndex;
    }

    internal (Point current, bool found) ReplaceAllText (string text, bool matchCase = false, bool matchWholeWord = false, string? textToReplace = null)
    {
        var found = false;
        var pos = Point.Empty;

        for (var i = 0; i < _lines.Count; i++)
        {
            List<Cell> x = _lines [i];
            string txt = GetText (x);
            string matchText = !matchCase ? text.ToUpper () : text;
            int col = txt.IndexOf (matchText, StringComparison.Ordinal);

            while (col > -1)
            {
                if (matchWholeWord && !MatchWholeWord (txt, matchText, col))
                {
                    if (col + 1 > txt.Length)
                    {
                        break;
                    }

                    col = txt.IndexOf (matchText, col + 1, StringComparison.Ordinal);

                    continue;
                }

                if (!found)
                {
                    found = true;
                }

                _lines [i] = Cell.ToCellList (ReplaceText (x, textToReplace!, matchText, col));
                x = _lines [i];
                txt = GetText (x);
                pos = new Point (col, i);
                col += textToReplace!.Length - matchText.Length;

                if (col < 0 || col + 1 > txt.Length)
                {
                    break;
                }

                col = txt.IndexOf (matchText, col + 1, StringComparison.Ordinal);
            }
        }

        return (pos, found);

        string GetText (List<Cell> x)
        {
            var txt = Cell.ToString (x);

            if (!matchCase)
            {
                txt = txt.ToUpper ();
            }

            return txt;
        }
    }

    /// <summary>Redefine column and line tracking.</summary>
    /// <param name="point">Contains the column and line.</param>
    internal void ResetContinuousFind (Point point)
    {
        _toFind.startPointToFind = _toFind.currentPointToFind = point;
        _toFind.found = false;
    }

    internal static bool SetCol (ref int col, int width, int cols, int viewportX = 0)
    {
        if (col + cols - viewportX > width)
        {
            return false;
        }

        col += cols;

        return true;
    }

    internal void Append (List<byte> line)
    {
        var str = StringExtensions.ToString (line.ToArray ());
        _lines.Add (Cell.StringToCells (str));
    }

    internal bool ApplyToFind ((Point current, bool found) foundPos)
    {
        var gaveFullTurn = false;

        if (!foundPos.found)
        {
            return gaveFullTurn;
        }

        _toFind.currentPointToFind = foundPos.current;

        switch (_toFind.found)
        {
            case true when _toFind.currentPointToFind == _toFind.startPointToFind:
                gaveFullTurn = true;

                break;

            case false:
                _toFind.startPointToFind = _toFind.currentPointToFind = foundPos.current;
                _toFind.found = foundPos.found;

                break;
        }

        return gaveFullTurn;
    }

    internal (Point current, bool found) GetFoundNextTextPoint (string text, int linesCount, bool matchCase, bool matchWholeWord, Point start)
    {
        for (int i = start.Y; i < linesCount; i++)
        {
            List<Cell> x = _lines [i];
            var txt = Cell.ToString (x);

            if (!matchCase)
            {
                txt = txt.ToUpper ();
            }

            string matchText = !matchCase ? text.ToUpper () : text;
            int col = txt.IndexOf (matchText, Math.Min (start.X, txt.Length), StringComparison.Ordinal);

            if (col > -1 && matchWholeWord && !MatchWholeWord (txt, matchText, col))
            {
                continue;
            }

            if (col > -1 && ((i == start.Y && col >= start.X) || i > start.Y) && txt.Contains (matchText))
            {
                return (new Point (col, i), true);
            }

            if (col == -1 && start.X > 0)
            {
                start.X = 0;
            }
        }

        return (Point.Empty, false);
    }

    internal (Point current, bool found) GetFoundPreviousTextPoint (string text, int linesCount, bool matchCase, bool matchWholeWord, Point start)
    {
        for (int i = linesCount; i >= 0; i--)
        {
            List<Cell> x = _lines [i];
            var txt = Cell.ToString (x);

            if (!matchCase)
            {
                txt = txt.ToUpper ();
            }

            if (start.Y != i)
            {
                start.X = Math.Max (x.Count - 1, 0);
            }

            string matchText = !matchCase ? text.ToUpper () : text;
            int col = txt.LastIndexOf (matchText, _toFind.found ? start.X - 1 : start.X, StringComparison.Ordinal);

            switch (col)
            {
                case > -1 when matchWholeWord && !MatchWholeWord (txt, matchText, col):
                    continue;

                case > -1 when ((i <= linesCount && col <= start.X) || i < start.Y) && txt.Contains (matchText):
                    return (new Point (col, i), true);
            }
        }

        return (Point.Empty, false);
    }

    internal static RuneType GetRuneType (Rune rune)
    {
        if (Rune.IsSymbol (rune))
        {
            return RuneType.IsSymbol;
        }

        if (Rune.IsWhiteSpace (rune))
        {
            return RuneType.IsWhiteSpace;
        }

        if (Rune.IsLetterOrDigit (rune))
        {
            return RuneType.IsLetterOrDigit;
        }

        if (Rune.IsPunctuation (rune))
        {
            return RuneType.IsPunctuation;
        }

        return RuneType.IsUnknown;
    }

    internal static bool IsSameRuneType (Rune newRune, RuneType runeType, bool useSameRuneType)
    {
        RuneType rt = GetRuneType (newRune);

        if (useSameRuneType)
        {
            return rt == runeType;
        }

        return runeType switch
        {
            RuneType.IsSymbol or RuneType.IsPunctuation => rt is RuneType.IsSymbol or RuneType.IsPunctuation,
            RuneType.IsWhiteSpace or RuneType.IsLetterOrDigit or RuneType.IsUnknown => rt == runeType,
            _ => throw new ArgumentOutOfRangeException (nameof (runeType), runeType, null)
        };
    }

    internal static bool MatchWholeWord (string source, string matchText, int index = 0)
    {
        if (string.IsNullOrEmpty (source) || string.IsNullOrEmpty (matchText))
        {
            return false;
        }

        string txt = matchText.Trim ();
        int start = index > 0 ? index - 1 : 0;
        int end = index + txt.Length;

        return (start == 0 || Rune.IsWhiteSpace ((Rune)source [start])) && (end == source.Length || Rune.IsWhiteSpace ((Rune)source [end]));
    }

    internal bool MoveNext (ref int col, ref int row, out Rune rune, bool useSameRuneType)
    {
        List<Cell> line = GetLine (row);

        if (col + 1 < line.Count)
        {
            col++;
            rune = Rune.GetRuneAt (line [col].Grapheme, 0);
            var prevRune = Rune.GetRuneAt (line [col - 1].Grapheme, 0);

            if (col + 1 == line.Count
                && !Rune.IsLetterOrDigit (rune)
                && !Rune.IsWhiteSpace (prevRune)
                && IsSameRuneType (prevRune, GetRuneType (rune), useSameRuneType))
            {
                col++;
            }

            prevRune = Rune.GetRuneAt (line [col - 1].Grapheme, 0);

            if (!Rune.IsWhiteSpace (rune) && (Rune.IsWhiteSpace (prevRune) || !IsSameRuneType (prevRune, GetRuneType (rune), useSameRuneType)))
            {
                return false;
            }

            return true;
        }

        if (col + 1 == line.Count)
        {
            col++;
            rune = default (Rune);

            return false;
        }

        // End of line
        col = 0;
        row++;
        rune = default (Rune);

        return false;
    }

    internal bool MovePrev (ref int col, ref int row, out Rune rune, bool useSameRuneType)
    {
        List<Cell> line = GetLine (row);

        if (col > 0)
        {
            col--;
            rune = Rune.GetRuneAt (line [col].Grapheme, 0);
            var nextRune = Rune.GetRuneAt (line [col + 1].Grapheme, 0);

            return (Rune.IsWhiteSpace (rune) || Rune.IsWhiteSpace (nextRune) || IsSameRuneType (nextRune, GetRuneType (rune), useSameRuneType))
                   && (!Rune.IsWhiteSpace (rune) || Rune.IsWhiteSpace (nextRune));
        }

        rune = default (Rune);

        return false;
    }

    internal void OnLinesLoaded () => LinesLoaded?.Invoke (this, EventArgs.Empty);

    internal static string ReplaceText (List<Cell> source, string textToReplace, string matchText, int col)
    {
        var origTxt = Cell.ToString (source);
        (_, int len) = DisplaySize (source, 0, col, false);
        (_, int len2) = DisplaySize (source, col, col + matchText.Length, false);
        (_, int len3) = DisplaySize (source, col + matchText.Length, GraphemeHelper.GetGraphemeCount (origTxt), false);

        return string.Concat (origTxt.AsSpan () [..len], textToReplace, origTxt.AsSpan (len + len2, len3));
    }

    internal Cell? RuneAt (int col, int row)
    {
        List<Cell> line = GetLine (row);

        if (line.Count > 0)
        {
            return line [col > line.Count - 1 ? line.Count - 1 : col];
        }

        return null;
    }

    internal void SetAttributes (Attribute? attribute)
    {
        foreach (List<Cell> line in _lines)
        {
            for (var i = 0; i < line.Count; i++)
            {
                Cell cell = line [i];
                cell.Attribute ??= attribute;
                line [i] = cell;
            }
        }
    }

    internal enum RuneType
    {
        IsSymbol,
        IsWhiteSpace,
        IsLetterOrDigit,
        IsPunctuation,
        IsUnknown
    }
}
