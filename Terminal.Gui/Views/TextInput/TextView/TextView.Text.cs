using Terminal.Gui.Drawing;

namespace Terminal.Gui.Views;

public partial class TextView
{
    private TextModel _model = new ();
    private int _currentColumn;
    private int _currentRow;

    /// <summary>Gets the cursor column.</summary>
    /// <value>The cursor column.</value>
    public int CurrentColumn
    {
        get => _currentColumn;
        private set
        {
            _currentColumn = value;
            _insertionPoint = new Point (_currentColumn, _currentRow);
            PositionCursor ();
        }
    }

    /// <summary>Gets the current cursor row.</summary>
    public int CurrentRow
    {
        get => _currentRow;
        private set
        {
            _currentRow = value;
            _insertionPoint = new Point (_currentColumn, _currentRow);
            PositionCursor ();
        }
    }

    private Point _insertionPoint;

    /// <summary>Sets or gets the current cursor position.</summary>
    public Point InsertionPoint
    {
        get => _insertionPoint;
        set
        {
            Point oldPosition = _insertionPoint;

            List<Cell> line = _model.GetLine (Math.Max (Math.Min (value.Y, _model.Count - 1), 0));

            CurrentColumn = value.X < 0 ? 0 : value.X > line.Count ? line.Count : value.X;

            CurrentRow = value.Y < 0 ? 0 : value.Y > _model.Count - 1 ? Math.Max (_model.Count - 1, 0) : value.Y;

            AdjustViewport ();

            // Signal cursor position changed without requiring additional redraw
            Point newPosition = new (CurrentColumn, CurrentRow);

            if (newPosition != oldPosition)
            {
                PositionCursor ();
            }
        }
    }

    /// <summary>
    ///     Gets whatever the text was changed or not. <see langword="true"/> if the text was changed
    ///     <see langword="false"/> otherwise. Calling <see cref="ClearHistoryChanges"/> will reset this property to
    ///     <see langword="false"/>.
    /// </summary>
    public bool IsDirty => _historyText.IsDirty (_model.GetAllLines ());

    /// <summary>Gets the number of lines.</summary>
    public int Lines => _model.Count;

    private bool _multiline = true;

    /// <summary>Gets or sets a value indicating whether this <see cref="TextView"/> is a multiline text view.</summary>
    public bool Multiline
    {
        get => _multiline;
        set
        {
            _multiline = value;

            if (_multiline && !_tabKeyAddsTab)
            {
                TabKeyAddsTab = true;
            }

            if (_multiline && !EnterKeyAddsLine)
            {
                EnterKeyAddsLine = true;
            }

            if (!_multiline)
            {
                EnterKeyAddsLine = false;
                TabKeyAddsTab = false;
                WordWrap = false;

                // Don't reset cursor position - this causes unwanted scrolling (issue #3988)

                if (!IsInitialized)
                {
                    _model.LoadString (Text);
                }

                SetNeedsDraw ();
            }
            else if (_multiline)
            {
                SetNeedsDraw ();
            }

            KeyBindings.Remove (Key.Enter);
            KeyBindings.Add (Key.Enter, Multiline ? Command.NewLine : Command.Accept);
        }
    }

    private bool _isReadOnly;

    /// <summary>Gets or sets whether the <see cref="TextView"/> is in read-only mode or not</summary>
    /// <value>Boolean value(Default false)</value>
    public bool ReadOnly
    {
        get => _isReadOnly;
        set
        {
            if (value == _isReadOnly)
            {
                return;
            }
            _isReadOnly = value;

            SetNeedsDraw ();

            WrapTextModel ();
            AdjustViewport ();
        }
    }

    private int _tabWidth = 4;

    /// <summary>
    ///     Gets or sets the number of columns that will be used for tab characters in the text.
    ///     The default is 4.
    /// </summary>
    public int TabWidth
    {
        get => _tabWidth;
        set
        {
            _tabWidth = Math.Max (value, 0);

            if (_tabWidth > 0 && !TabKeyAddsTab)
            {
                TabKeyAddsTab = true;
            }

            SetNeedsDraw ();
        }
    }

    /// <summary>Sets or gets the text in the <see cref="TextView"/>.</summary>
    /// <remarks>
    ///     The <see cref="View.TextChanged"/> event is fired whenever this property is set. Note, however, that Text is not
    ///     set by <see cref="TextView"/> as the user types.
    /// </remarks>
    public new string Text
    {
        get
        {
            if (_wordWrap)
            {
                return _wrapManager!.Model.ToString ();
            }

            return _model.ToString ();
        }
        set
        {
            // Raise View.TextChanging so subscribers holding a View reference can cancel.
            if (OnTextChanging (value))
            {
                return;
            }

            ResetPosition ();
            _model.LoadString (value);

            if (_wordWrap)
            {
                _wrapManager = new WordWrapManager (_model);
                _model = _wrapManager.WrapModel (Viewport.Width, out _, out _, out _, out _);
                _lastWrapWidth = Viewport.Width;
            }

            // Keep base View._text in sync
            SetTextDirect (value);

            _ownSetterActive = true;
            RaiseTextChanged ();
            _ownSetterActive = false;
            SetNeedsDraw ();

            _historyText.Clear (_model.GetAllLines ());
        }
    }

    /// <summary>Tracks whether the <c>new Text</c> setter is active to avoid redundant sync in <see cref="OnTextChanged"/>.</summary>
    private bool _ownSetterActive;

    /// <inheritdoc/>
    /// <remarks>
    ///     Syncs the internal <see cref="TextModel"/> when <see cref="View.Text"/> is set through a
    ///     polymorphic (<see cref="View"/>) reference, ensuring the TextView's editing model stays consistent.
    /// </remarks>
    protected override void OnTextChanged ()
    {
        // Skip sync when called from our own `new Text` setter — it already updated the model.
        if (_ownSetterActive)
        {
            base.OnTextChanged ();

            return;
        }

        string baseText = base.Text;

        // Sync when the internal model diverges (polymorphic setter case).
        ResetPosition ();
        _model.LoadString (baseText);

        if (_wordWrap)
        {
            _wrapManager = new WordWrapManager (_model);
            _model = _wrapManager.WrapModel (Viewport.Width, out _, out _, out _, out _);
            _lastWrapWidth = Viewport.Width;
        }

        _historyText.Clear (_model.GetAllLines ());
        SetNeedsDraw ();

        base.OnTextChanged ();
    }

    /// <summary>
    ///     Tracks whether the text view should be considered "used", that is, that the user has moved in the entry, so
    ///     new input should be appended at the cursor position, rather than clearing the entry
    /// </summary>
    public bool Used { get; set; }

    /// <summary>
    ///     Gets or sets whether the word forward and word backward navigation should use the same or equivalent rune type.
    ///     Default is <c>false</c> meaning using equivalent rune type.
    /// </summary>
    public bool UseSameRuneTypeForWords { get; set; }

    /// <summary>Gets all lines of characters.</summary>
    /// <returns></returns>
    public List<List<Cell>> GetAllLines () => _model.GetAllLines ();

    /// <summary>
    ///     Returns the characters on the current line (where the cursor is positioned). Use <see cref="CurrentColumn"/>
    ///     to determine the position of the cursor within that line
    /// </summary>
    /// <returns></returns>
    public List<Cell> GetCurrentLine () => _model.GetLine (CurrentRow);

    /// <summary>Returns the characters on the <paramref name="line"/>.</summary>
    /// <param name="line">The intended line.</param>
    /// <returns></returns>
    public List<Cell> GetLine (int line) => _model.GetLine (line);

    /// <summary>
    ///     Gets the width in columns of the given <paramref name="line"/>.
    /// </summary>
    /// <param name="line">The intended line.</param>
    /// <returns></returns>
    public int GetColumnsWidth (List<Cell> line) => TextModel.CursorColumn (TextModel.CellsToStringList (line), line.Count, TabWidth, out _, out _);

    /// <summary>
    ///     Inserts the given <paramref name="toAdd"/> text at the current cursor position exactly as if the user had just
    ///     typed it
    /// </summary>
    /// <param name="toAdd">Text to add</param>
    public void InsertText (string toAdd)
    {
        foreach (string grapheme in TextModel.GetInsertableGraphemes (toAdd))
        {
            Key key = TextModel.CreateKeyFromGrapheme (grapheme);

            InsertText (key);

            if (NeedsDraw)
            {
                AdjustViewport ();
            }

            PositionCursor ();
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    ///     Normalizes line endings (CR and CRLF → LF) so the text model sees a single newline form,
    ///     and strips C0/C1 control chars except tab and newline. Mirrors Windows Terminal's
    ///     <c>FilterStringForPaste</c>.
    /// </remarks>
    protected override string OnSanitizingPaste (string raw)
    {
        StringBuilder sb = new (raw.Length);

        for (var i = 0; i < raw.Length; i++)
        {
            char c = raw [i];

            if (c == '\r')
            {
                sb.Append ('\n');

                if (i + 1 < raw.Length && raw [i + 1] == '\n')
                {
                    i++;
                }

                continue;
            }

            if (c == '\n' || c == '\t' || (c >= 0x20 && c < 0x7F) || c >= 0xA0)
            {
                sb.Append (c);
            }
        }

        return sb.ToString ();
    }

    /// <inheritdoc/>
    /// <remarks>
    ///     Returns <see langword="true"/> even when <see cref="ReadOnly"/> so that <c>Ctrl+V</c>
    ///     does not bubble to a parent view that might also bind paste.
    /// </remarks>
    protected override bool OnPaste (string text)
    {
        if (_isReadOnly)
        {
            return true;
        }

        ResetColumnTrack ();
        SetWrapModel ();

        // Preserves the legacy clipboard "Copy without selection → Paste inserts as new line above"
        // behavior. _copyWithoutSelection is set only by this view's own Copy() command, so
        // bracketed paste (which has no preceding Copy) naturally skips this branch.
        if (!CurrentPasteUsesPayload && _copyWithoutSelection && text.FirstOrDefault (x => x is '\n' or '\r') == 0)
        {
            List<Cell> runeList = Cell.ToCellList (text);
            List<Cell> currentLine = GetCurrentLine ();
            _historyText.Add ([ [.. currentLine]], InsertionPoint);
            List<List<Cell>> addedLine = [ [.. currentLine], runeList];
            _historyText.Add ([.. addedLine], InsertionPoint, TextEditingLineStatus.Added);
            _model.AddLine (CurrentRow, runeList);
            SetNeedsDraw ();
            CurrentRow++;
            _historyText.Add ([ [.. GetCurrentLine ()]], InsertionPoint, TextEditingLineStatus.Replaced);

            OnContentsChanged ();
        }
        else
        {
            if (IsSelecting)
            {
                ClearRegion ();
            }

            _copyWithoutSelection = false;
            InsertAllText (text, true);

            if (IsSelecting)
            {
                _historyText.ReplaceLast ([ [.. GetCurrentLine ()]], InsertionPoint, TextEditingLineStatus.Original);
            }
        }

        UpdateWrapModel ();
        IsSelecting = false;
        DoNeededAction ();

        return true;
    }

    /// <inheritdoc/>
    protected override bool ShouldRaisePastedEvent (string text) => !_isReadOnly;

    /// <summary>Replaces all the text based on the match case.</summary>
    /// <param name="textToFind">The text to find.</param>
    /// <param name="matchCase">The match case setting.</param>
    /// <param name="matchWholeWord">The match whole word setting.</param>
    /// <param name="textToReplace">The text to replace.</param>
    /// <returns><c>true</c>If the text was found.<c>false</c>otherwise.</returns>
    public bool ReplaceAllText (string textToFind, bool matchCase = false, bool matchWholeWord = false, string? textToReplace = null)
    {
        if (_isReadOnly || _model.Count == 0)
        {
            return false;
        }

        SetWrapModel ();
        ResetContinuousFind ();

        (Point current, bool found) foundPos = _model.ReplaceAllText (textToFind, matchCase, matchWholeWord, textToReplace);

        // Calls UpdateWrapModel internally
        return SetFoundText (textToFind, foundPos, textToReplace, false, true);
    }

    private void ClearRegion ()
    {
        SetWrapModel ();

        long currentEncoded = ((long)(uint)CurrentRow << 32) | (uint)CurrentColumn;
        GetEncodedRegionBounds (out long start, out long end);
        var startRow = (int)(start >> 32);
        var maxRow = (int)(end >> 32);
        var startCol = (int)(start & 0xffffffff);
        var endCol = (int)(end & 0xffffffff);
        List<Cell> line = _model.GetLine (startRow);

        _historyText.Add ([ [.. line]], new Point (startCol, startRow));

        List<List<Cell>> removedLines = [];

        if (startRow == maxRow)
        {
            removedLines.Add ([.. line]);

            line.RemoveRange (startCol, endCol - startCol);
            SetNeedsDraw ();
            CurrentColumn = startCol;

            _historyText.Add ([.. removedLines], InsertionPoint, TextEditingLineStatus.Removed);

            UpdateWrapModel ();

            return;
        }

        removedLines.Add ([.. line]);

        line.RemoveRange (startCol, line.Count - startCol);
        List<Cell> line2 = _model.GetLine (maxRow);
        line.AddRange (line2.Skip (endCol));

        for (int row = startRow + 1; row <= maxRow; row++)
        {
            removedLines.Add ([.. _model.GetLine (startRow + 1)]);

            _model.RemoveLine (startRow + 1);
            SetNeedsDraw ();
        }

        if (currentEncoded == end)
        {
            CurrentRow -= maxRow - startRow;
        }

        CurrentColumn = startCol;

        _historyText.Add ([.. removedLines], InsertionPoint, TextEditingLineStatus.Removed);

        UpdateWrapModel ();
    }

    private void GetEncodedRegionBounds (out long start, out long end, int? startRow = null, int? startCol = null, int? cRow = null, int? cCol = null)
    {
        long selection;
        long point;

        if (startRow is null || startCol is null || cRow is null || cCol is null)
        {
            selection = ((long)(uint)_selectionStartRow << 32) | (uint)_selectionStartColumn;
            point = ((long)(uint)CurrentRow << 32) | (uint)CurrentColumn;
        }
        else
        {
            selection = ((long)(uint)startRow << 32) | (uint)startCol;
            point = ((long)(uint)cRow << 32) | (uint)cCol;
        }

        if (selection > point)
        {
            start = point;
            end = selection;
        }
        else
        {
            start = selection;
            end = point;
        }
    }

    internal string GetRegion (out List<List<Cell>> cellsList, int? sRow = null, int? sCol = null, int? cRow = null, int? cCol = null, TextModel? model = null)
    {
        GetEncodedRegionBounds (out long start, out long end, sRow, sCol, cRow, cCol);

        cellsList = [];

        if (start == end)
        {
            return string.Empty;
        }

        var startRow = (int)(start >> 32);
        var maxRow = (int)(end >> 32);
        var startCol = (int)(start & 0xffffffff);
        var endCol = (int)(end & 0xffffffff);
        List<Cell> line = model is null ? _model.GetLine (startRow) : model.GetLine (startRow);
        List<Cell> cells;

        if (startRow == maxRow)
        {
            cells = line.GetRange (startCol, endCol - startCol);
            cellsList.Add (cells);

            return StringFromCells (cells);
        }

        cells = line.GetRange (startCol, line.Count - startCol);
        cellsList.Add (cells);
        string res = StringFromCells (cells);

        for (int row = startRow + 1; row < maxRow; row++)
        {
            cellsList.AddRange ([]);
            cells = model == null ? _model.GetLine (row) : model.GetLine (row);
            cellsList.Add (cells);

            res = res + Environment.NewLine + StringFromCells (cells);
        }

        line = model is null ? _model.GetLine (maxRow) : model.GetLine (maxRow);
        cellsList.AddRange ([]);
        cells = line.GetRange (0, endCol);
        cellsList.Add (cells);
        res = res + Environment.NewLine + StringFromCells (cells);

        return res;
    }

    private void Insert (Cell cell)
    {
        List<Cell> line = GetCurrentLine ();

        if (!Used)
        {
            if (CurrentColumn < line.Count)
            {
                line.RemoveAt (CurrentColumn);
            }
        }

        line.Insert (Math.Min (CurrentColumn, line.Count), cell);

        if (!_wrapNeeded)
        {
            SetNeedsDraw ();
        }
    }

    private void InsertAllText (string text, bool fromClipboard = false)
    {
        if (string.IsNullOrEmpty (text))
        {
            return;
        }

        List<List<Cell>> lines;

        if (fromClipboard && text == _copiedText)
        {
            lines = _copiedCellsList;
        }
        else
        {
            // Get selected attribute
            Attribute? attribute = GetSelectedAttribute (CurrentRow, CurrentColumn);
            lines = Cell.StringToLinesOfCells (text, attribute);
        }

        if (lines.Count == 0)
        {
            return;
        }

        SetWrapModel ();

        List<Cell> line = GetCurrentLine ();

        _historyText.Add ([ [.. line]], InsertionPoint);

        // Optimize single line
        if (lines.Count == 1)
        {
            line.InsertRange (CurrentColumn, lines [0]);
            CurrentColumn += lines [0].Count;

            _historyText.Add ([ [.. line]], InsertionPoint, TextEditingLineStatus.Replaced);

            if (!_wordWrap && CurrentColumn - Viewport.X > Viewport.Width)
            {
                Viewport = Viewport with { X = Math.Max (CurrentColumn - Viewport.Width + 1, 0) };
            }

            UpdateWrapModel ();
            OnContentsChanged ();

            return;
        }

        List<Cell>? rest = null;
        var lastPosition = 0;

        if (_model.Count > 0 && line.Count > 0 && !_copyWithoutSelection)
        {
            // Keep a copy of the rest of the line
            int restCount = line.Count - CurrentColumn;
            rest = line.GetRange (CurrentColumn, restCount);
            line.RemoveRange (CurrentColumn, restCount);
        }

        // First line is inserted at the current location, the rest is appended
        line.InsertRange (CurrentColumn, lines [0]);

        List<List<Cell>> addedLines = [ [.. line]];

        for (var i = 1; i < lines.Count; i++)
        {
            _model.AddLine (CurrentRow + i, lines [i]);

            addedLines.Add ([.. lines [i]]);
        }

        if (rest is { })
        {
            List<Cell> last = _model.GetLine (CurrentRow + lines.Count - 1);
            lastPosition = last.Count;
            last.InsertRange (last.Count, rest);

            addedLines.Last ().InsertRange (addedLines.Last ().Count, rest);
        }

        _historyText.Add (addedLines, InsertionPoint, TextEditingLineStatus.Added);

        // Now adjust column and row positions
        CurrentRow += lines.Count - 1;
        CurrentColumn = rest is { } ? lastPosition : lines [^1].Count;
        AdjustViewport ();

        _historyText.Add ([ [.. line]], InsertionPoint, TextEditingLineStatus.Replaced);

        UpdateWrapModel ();
        OnContentsChanged ();
    }

    private void InsertText (Key a, Attribute? attribute = null)
    {
        //So that special keys like tab can be processed
        if (_isReadOnly)
        {
            return;
        }

        SetWrapModel ();

        _historyText.Add ([ [.. GetCurrentLine ()]], InsertionPoint);

        if (IsSelecting)
        {
            ClearSelectedRegion ();
        }

        if ((uint)a.KeyCode == '\n')
        {
            _model.AddLine (CurrentRow + 1, []);
            CurrentRow++;
            CurrentColumn = 0;
        }
        else if ((uint)a.KeyCode == '\r')
        {
            CurrentColumn = 0;
        }
        else
        {
            string grapheme = a.AsGrapheme;

            if (string.IsNullOrEmpty (grapheme))
            {
                return;
            }

            Insert (new Cell { Grapheme = grapheme, Attribute = attribute });
            CurrentColumn++;

            // Text was inserted, so it's always needed to redraw and update content size if needed
            SetNeedsDraw ();

            List<Cell> line = GetCurrentLine ();
            (int size, int length) dSize = TextModel.DisplaySize (line, 0, line.Count, true, TabWidth);

            if (_model.ShouldInvalidateMaxWidthCache (CurrentRow, true, dSize.size))
            {
                _model.InvalidateMaxWidthCache ();
                UpdateContentSize ();
            }
        }

        _historyText.Add ([ [.. GetCurrentLine ()]], InsertionPoint, TextEditingLineStatus.Replaced);

        UpdateWrapModel ();
        OnContentsChanged ();
    }

    private void Model_LinesLoaded (object sender, EventArgs e)
    {
        // This call is not needed. Model_LinesLoaded gets invoked when
        // model.LoadString (value) is called. LoadString is called from one place
        // (Text.set) and historyText.Clear() is called immediately after.
        // If this call happens, HistoryText_ChangeText will get called multiple times
        // when Text is set, which is wrong.
        //historyText.Clear (Text);

        if (!_multiline && !IsInitialized)
        {
            CurrentColumn = GraphemeHelper.GetGraphemeCount (Text);
            Viewport = Viewport with { X = CurrentColumn > Viewport.Width + 1 ? CurrentColumn - Viewport.Width + 1 : 0 };
        }

        UpdateContentSize ();
    }

    private int _columnTrack = -1;
    private bool _lastWasKill;

    private void ResetAllTrack ()
    {
        // Handle some state here - whether the last command was a kill
        // operation and the column tracking (up/down)
        _lastWasKill = false;
        _columnTrack = -1;
        _continuousFind = false;
    }

    private void ResetColumnTrack ()
    {
        // Handle some state here - whether the last command was a kill
        // operation and the column tracking (up/down)
        _lastWasKill = false;
        _columnTrack = -1;
    }

    private bool _continuousFind;

    private void ResetContinuousFind ()
    {
        if (_continuousFind)
        {
            return;
        }
        int col = IsSelecting ? _selectionStartColumn : CurrentColumn;
        int row = IsSelecting ? _selectionStartRow : CurrentRow;
        _model.ResetContinuousFind (new Point (col, row));
    }

    private void ResetContinuousFindTrack ()
    {
        // Handle some state here - whether the last command was a kill
        // operation and the column tracking (up/down)
        _lastWasKill = false;
        _continuousFind = false;
    }

    /// <summary>
    ///     Resets the Viewport and InsertionPoint to the top-left of the text content.
    /// </summary>
    private void ResetPosition ()
    {
        Viewport = Viewport with { Y = 0, X = 0 };
        CurrentRow = CurrentColumn = 0;
        StopSelecting ();
    }

    private bool SetFoundText (string text, (Point current, bool found) foundPos, string? textToReplace = null, bool replace = false, bool replaceAll = false)
    {
        if (foundPos.found)
        {
            StartSelecting ();
            _selectionStartColumn = foundPos.current.X;
            _selectionStartRow = foundPos.current.Y;

            if (!replaceAll)
            {
                CurrentColumn = _selectionStartColumn + GraphemeHelper.GetGraphemeCount (text);
            }
            else
            {
                CurrentColumn = _selectionStartColumn + GraphemeHelper.GetGraphemeCount (textToReplace!);
            }

            CurrentRow = foundPos.current.Y;

            if (!_isReadOnly && replace)
            {
                AdjustViewport ();
                ClearSelectedRegion ();
                InsertAllText (textToReplace!);
                StartSelecting ();
                _selectionStartColumn = CurrentColumn - GraphemeHelper.GetGraphemeCount (textToReplace!);
            }
            else
            {
                UpdateWrapModel ();
                AdjustViewport ();
            }

            _continuousFind = true;

            return foundPos.found;
        }

        UpdateWrapModel ();
        _continuousFind = false;

        return foundPos.found;
    }

    private string StringFromCells (List<Cell> cells)
    {
        ArgumentNullException.ThrowIfNull (cells);

        var size = 0;

        foreach (Cell cell in cells)
        {
            string t = cell.Grapheme;
            size += Encoding.Unicode.GetByteCount (t);
        }

        var encoded = new byte [size];
        var offset = 0;

        foreach (Cell cell in cells)
        {
            string t = cell.Grapheme;
            int bytesWritten = Encoding.Unicode.GetBytes (t, 0, t.Length, encoded, offset);
            offset += bytesWritten;
        }

        // decode using the same encoding and the bytes actually written
        return Encoding.Unicode.GetString (encoded, 0, offset);
    }

    private void TrackColumn ()
    {
        // Now track the column
        List<Cell> line = GetCurrentLine ();

        if (line.Count < _columnTrack)
        {
            CurrentColumn = line.Count;
        }
        else if (_columnTrack != -1)
        {
            System.Diagnostics.Debug.Assert (!(_columnTrack > line.Count));
            CurrentColumn = _columnTrack > line.Count ? line.Count : _columnTrack;
        }
        else if (CurrentColumn > line.Count)
        {
            CurrentColumn = line.Count;
        }

        if (CurrentColumn < Viewport.X || CurrentColumn > Viewport.Width + Viewport.X)
        {
            SetNeedsDraw ();
        }
    }
}
