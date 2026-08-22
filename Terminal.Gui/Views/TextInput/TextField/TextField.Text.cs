using System.Globalization;
using Terminal.Gui.Drawing;

namespace Terminal.Gui.Views;

public partial class TextField
{
    private CultureInfo _currentCulture;
    private string? _lastPastedText;

    /// <summary>Raised before <see cref="Text"/> changes. The change can be canceled the text adjusted.</summary>
    public new event EventHandler<ResultEventArgs<string>>? TextChanging;

    /// <summary>
    ///     Tracks whether the text field should be considered "used", that is, that the user has moved in the entry, so
    ///     new input should be appended at the cursor position, rather than clearing the entry
    /// </summary>
    public bool Used { get; set; }

    private TextModel GetModel ()
    {
        TextModel model = new ();
        model.LoadString (Text);

        return model;
    }

    /// <summary>
    ///     Inserts the given <paramref name="toAdd"/> text at the current cursor position exactly as if the user had just
    ///     typed it
    /// </summary>
    /// <param name="toAdd">Text to add</param>
    /// <param name="useOldCursorPos">Use the previous cursor position.</param>
    public void InsertText (string toAdd, bool useOldCursorPos = true)
    {
        foreach (string grapheme in TextModel.GetInsertableGraphemes (toAdd))
        {
            Key key = TextModel.CreateKeyFromGrapheme (grapheme);
            InsertText (key, useOldCursorPos);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    ///     TextField is single-line — keep only the first line of the paste and drop C0/C1 control
    ///     characters, including tab. This matches what <see cref="Text"/> can actually accept and
    ///     keeps <see cref="View.Pasting"/> aligned with the text that will be inserted.
    /// </remarks>
    protected override string OnSanitizingPaste (string raw)
    {
        int newline = raw.IndexOfAny (['\r', '\n']);
        string firstLine = newline >= 0 ? raw [..newline] : raw;

        StringBuilder sb = new (firstLine.Length);

        foreach (char c in firstLine)
        {
            if ((c >= 0x20 && c < 0x7F) || c >= 0xA0)
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
        _lastPastedText = null;

        if (ReadOnly)
        {
            return true;
        }

        SetSelectedStartSelectedLength ();
        int selStart = _selectionStart == -1 ? InsertionPoint : _selectionStart;
        int selectedLength = SelectedLength;
        List<string> oldText = [.. _text];
        string oldTextString = StringExtensions.ToString (oldText);
        List<string> pastedText = text.ToStringList ();
        List<string> proposedText = [.. oldText.GetRange (0, selStart),
                                     .. pastedText,
                                     .. oldText.GetRange (selStart + selectedLength, oldText.Count - (selStart + selectedLength))];

        Text = StringExtensions.ToString (proposedText);
        bool proposedEqualsFinal = AreEqual (proposedText, _text);

        if (oldTextString == Text && !proposedEqualsFinal)
        {
            return true;
        }

        GetActualPastedText (text, proposedText, selStart, pastedText.Count, _text, out string actualPastedText, out int insertionPoint);

        if (string.IsNullOrEmpty (actualPastedText))
        {
            _insertionPoint = insertionPoint;
            ClearAllSelection ();
            SetNeedsDraw ();
            Adjust ();

            return true;
        }

        _lastPastedText = actualPastedText;
        _insertionPoint = insertionPoint;
        ClearAllSelection ();
        SetNeedsDraw ();
        Adjust ();

        return true;
    }

    /// <inheritdoc/>
    protected override bool ShouldRaisePastedEvent (string text) => !ReadOnly && !string.IsNullOrEmpty (_lastPastedText);

    /// <inheritdoc/>
    protected override string? GetPastedEventText (string text) => _lastPastedText;

    private static void GetActualPastedText (string text, List<string> proposedText, int pasteStart, int pastedLength, List<string> finalText, out string actualPastedText, out int insertionPoint)
    {
        if (AreEqual (proposedText, finalText))
        {
            actualPastedText = text;
            insertionPoint = Math.Min (pasteStart + pastedLength, finalText.Count);

            return;
        }

        if (proposedText.Count == finalText.Count)
        {
            int actualPastedLength = Math.Min (pastedLength, finalText.Count - pasteStart);
            actualPastedText = actualPastedLength > 0
                                   ? StringExtensions.ToString (finalText.GetRange (pasteStart, actualPastedLength))
                                   : string.Empty;
            insertionPoint = Math.Min (pasteStart + actualPastedLength, finalText.Count);

            return;
        }

        actualPastedText = GetActualPastedTextFromEditOperations (proposedText, pasteStart, pastedLength, finalText, out insertionPoint);
    }

    private static string GetActualPastedTextFromEditOperations (List<string> proposedText, int pasteStart, int pastedLength, List<string> finalText, out int insertionPoint)
    {
        List<PasteEditOperation> operations = BuildPasteEditOperations (proposedText, finalText);
        int pasteEnd = pasteStart + pastedLength;
        int proposedIndex = 0;
        int finalIndex = 0;
        int pastedStart = -1;
        int pastedEnd = -1;
        int boundaryAfterPaste = -1;

        foreach (PasteEditOperation operation in operations)
        {
            if (ConsumesFinal (operation)
                && IsWithinPasteRange (operation, proposedIndex, pasteStart, pasteEnd))
            {
                if (pastedStart == -1)
                {
                    pastedStart = finalIndex;
                }

                pastedEnd = finalIndex + 1;
            }

            if (ConsumesProposed (operation))
            {
                proposedIndex++;

                if (proposedIndex == pasteEnd)
                {
                    boundaryAfterPaste = finalIndex + (ConsumesFinal (operation) ? 1 : 0);
                }
            }
            if (ConsumesFinal (operation))
            {
                finalIndex++;
            }
        }

        insertionPoint = boundaryAfterPaste == -1 ? finalIndex : boundaryAfterPaste;

        if (pastedStart == -1 || pastedEnd == -1 || pastedEnd <= pastedStart)
        {
            return string.Empty;
        }

        return StringExtensions.ToString (finalText.GetRange (pastedStart, pastedEnd - pastedStart));
    }

    private static List<PasteEditOperation> BuildPasteEditOperations (List<string> proposedText, List<string> finalText)
    {
        int [,] costs = new int [proposedText.Count + 1, finalText.Count + 1];

        for (int i = 0; i <= proposedText.Count; i++)
        {
            costs [i, 0] = i;
        }

        for (int j = 0; j <= finalText.Count; j++)
        {
            costs [0, j] = j;
        }

        for (int i = 1; i <= proposedText.Count; i++)
        {
            for (int j = 1; j <= finalText.Count; j++)
            {
                int substitutionCost = costs [i - 1, j - 1] + (proposedText [i - 1] == finalText [j - 1] ? 0 : 1);
                int insertionCost = costs [i, j - 1] + 1;
                int deletionCost = costs [i - 1, j] + 1;

                costs [i, j] = Math.Min (substitutionCost, Math.Min (insertionCost, deletionCost));
            }
        }

        List<PasteEditOperation> operations = [];
        int proposedIndex = proposedText.Count;
        int finalIndex = finalText.Count;

        while (proposedIndex > 0 || finalIndex > 0)
        {
            if (proposedIndex > 0
                && finalIndex > 0
                && costs [proposedIndex, finalIndex]
                == costs [proposedIndex - 1, finalIndex - 1]
                + (proposedText [proposedIndex - 1] == finalText [finalIndex - 1] ? 0 : 1))
            {
                operations.Add (PasteEditOperation.Substitute);
                proposedIndex--;
                finalIndex--;

                continue;
            }

            if (finalIndex > 0 && costs [proposedIndex, finalIndex] == costs [proposedIndex, finalIndex - 1] + 1)
            {
                operations.Add (PasteEditOperation.Insert);
                finalIndex--;

                continue;
            }

            operations.Add (PasteEditOperation.Delete);
            proposedIndex--;
        }

        operations.Reverse ();

        return operations;
    }

    private enum PasteEditOperation
    {
        Insert,
        Delete,
        Substitute
    }

    private static bool AreEqual (List<string> first, List<string> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        for (int i = 0; i < first.Count; i++)
        {
            if (first [i] != second [i])
            {
                return false;
            }
        }

        return true;
    }

    private static bool ConsumesFinal (PasteEditOperation operation) => operation is PasteEditOperation.Insert or PasteEditOperation.Substitute;

    private static bool ConsumesProposed (PasteEditOperation operation) => operation is PasteEditOperation.Delete or PasteEditOperation.Substitute;

    private static bool IsWithinPasteRange (PasteEditOperation operation, int proposedIndex, int pasteStart, int pasteEnd)
    {
        if (operation == PasteEditOperation.Insert)
        {
            return proposedIndex > pasteStart && proposedIndex < pasteEnd;
        }

        return proposedIndex >= pasteStart && proposedIndex < pasteEnd;
    }

    private List<string> DeleteSelectedText ()
    {
        SetSelectedStartSelectedLength ();
        int selStart = SelectedStart > -1 ? _selectionStart : InsertionPoint;

        string newText = StringExtensions.ToString (_text.GetRange (0, selStart))
                         + StringExtensions.ToString (_text.GetRange (selStart + SelectedLength, _text.Count - (selStart + SelectedLength)));

        ClearAllSelection ();
        InsertionPoint = selStart >= GraphemeHelper.GetGraphemeCount (newText) ? GraphemeHelper.GetGraphemeCount (newText) : selStart;

        return newText.ToStringList ();
    }

    private void InsertText (Key a, bool usePreTextChangedCursorPos)
    {
        _historyText.Add ([Cell.ToCells (_text)], new Point (InsertionPoint, 0));

        List<string> newText = _text;

        if (SelectedLength > 0)
        {
            newText = DeleteSelectedText ();
            _preChangeInsertionPoint = InsertionPoint;
        }

        if (!usePreTextChangedCursorPos)
        {
            _preChangeInsertionPoint = InsertionPoint;
        }

        string grapheme = a.AsGrapheme;

        if (string.IsNullOrEmpty (grapheme))
        {
            return;
        }

        if (Used)
        {
            _insertionPoint++;

            if (InsertionPoint == newText.Count + 1)
            {
                SetText (newText.Concat ([grapheme]).ToList ());
            }
            else
            {
                if (_preChangeInsertionPoint > newText.Count)
                {
                    _preChangeInsertionPoint = newText.Count;
                }

                SetText (newText.GetRange (0, _preChangeInsertionPoint)
                                .Concat ([grapheme])
                                .Concat (newText.GetRange (_preChangeInsertionPoint, Math.Min (newText.Count - _preChangeInsertionPoint, newText.Count))));
            }
        }
        else
        {
            SetText (newText.GetRange (0, _preChangeInsertionPoint)
                            .Concat ([grapheme])
                            .Concat (newText.GetRange (Math.Min (_preChangeInsertionPoint + 1, newText.Count),
                                                       Math.Max (newText.Count - _preChangeInsertionPoint - 1, 0))));
            InsertionPoint++;
        }

        Adjust ();
    }

    /// <summary>
    ///     Caches the cursor position before a text change operation. Used to properly handle text insertion
    ///     and deletion operations, particularly for undo/redo and proper cursor placement after edits.
    /// </summary>
    private int _preChangeInsertionPoint;

    /// <summary>
    ///     The text content stored as a list of strings, where each string represents a single text element
    ///     (grapheme cluster). This allows proper handling of Unicode combining characters and emoji.
    /// </summary>
    private List<string> _text;

    /// <summary>
    ///     Stashes the final text value determined during <see cref="OnTextChanging"/> (after sanitization
    ///     and possible subscriber modification) for use in <see cref="OnTextChanged"/>.
    /// </summary>
    private string? _pendingText;

    private void SetText (List<string> newText) => Text = StringExtensions.ToString (newText);
    private void SetText (IEnumerable<string> newText) => SetText (newText.ToList ());

    /// <inheritdoc/>
    /// <remarks>
    ///     <para>
    ///         TextField overrides this to sanitize text (strip tabs and newlines), fire
    ///         <see cref="IValue{TValue}.ValueChanging"/>, and raise the TextField-specific
    ///         <see cref="TextChanging"/> event (which allows subscribers to modify the text).
    ///     </para>
    /// </remarks>
    protected override bool OnTextChanging (string newText)
    {
        // Guard against base constructor calling before _text is initialized
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (_text is null)
        {
            return true;
        }

        // Sanitize: single-line, no tabs
        string sanitized = newText.Replace ("\t", "").Split ("\n") [0];

        var oldText = StringExtensions.ToString (_text);

        // After sanitization, check if there is an effective change
        if (oldText == sanitized)
        {
            return true;
        }

        // Raise IValue<string>.ValueChanging
        if (RaiseValueChanging (oldText, sanitized))
        {
            return true;
        }

        ResultEventArgs<string> args = new (sanitized);
        TextChanging?.Invoke (this, args);

        if (args.Handled)
        {
            if (InsertionPoint > _text.Count)
            {
                InsertionPoint = _text.Count;
            }

            return true;
        }

        // Stash the (possibly subscriber-modified) result for OnTextChanged
        _pendingText = args.Result;

        if (base.OnTextChanging (newText))
        {
            _pendingText = null;

            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    /// <remarks>
    ///     <para>
    ///         Syncs the internal grapheme-list editing model from the base <see cref="View.Text"/> value,
    ///         applying any sanitization or subscriber modifications stashed during <see cref="OnTextChanging"/>.
    ///     </para>
    /// </remarks>
    protected override void OnTextChanged ()
    {
        // Use stashed text from OnTextChanging if available; otherwise sanitize base.Text
        string finalText = _pendingText ?? Text.Replace ("\t", "").Split ("\n") [0];
        _pendingText = null;

        var oldText = StringExtensions.ToString (_text);

        ClearAllSelection ();
        _text = finalText.ToStringList ();

        // Ensure base View._text holds the sanitized/modified value
        if (Text != finalText)
        {
            SetTextDirect (finalText);
        }

        if (!Secret && !_historyText.IsFromHistory)
        {
            _historyText.Add ([Cell.ToCellList (oldText)], new Point (InsertionPoint, 0));

            _historyText.Add ([Cell.ToCells (_text)], new Point (InsertionPoint, 0), TextEditingLineStatus.Replaced);
        }

        base.OnTextChanged ();

        // Raise IValue<string>.ValueChanged
        RaiseValueChanged (oldText, StringExtensions.ToString (_text));

        ProcessAutocomplete ();

        if (InsertionPoint > _text.Count)
        {
            InsertionPoint = Math.Max (TextModel.DisplaySize (_text, 0).size - 1, 0);
        }

        Adjust ();
        SetNeedsDraw ();
    }

    /// <summary>
    ///     Returns <see langword="true"/> if the current cursor position is at the end of the <see cref="Text"/>. This
    ///     includes when it is empty.
    /// </summary>
    /// <returns></returns>
    internal bool InsertionPointIsAtEnd () => InsertionPoint == Text.Length;

    /// <summary>Returns <see langword="true"/> if the current cursor position is at the start of the <see cref="TextField"/>.</summary>
    /// <returns></returns>
    internal bool InsertionPointIsAtStart () => InsertionPoint <= 0;

    /// <summary>
    ///     Adjusts the <see cref="ScrollOffset"/> to ensure the cursor remains visible within the viewport,
    ///     and triggers a redraw if necessary.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method maintains the invariant that the cursor is always visible by adjusting <see cref="ScrollOffset"/>:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>If <see cref="InsertionPoint"/> is to the left of the visible area, scrolls left</description>
    ///             </item>
    ///             <item>
    ///                 <description>If <see cref="InsertionPoint"/> is to the right of the visible area, scrolls right</description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Called automatically after cursor movement or text changes to keep the cursor in view.
    ///         If scrolling occurred or a redraw is needed, calls <see cref="View.SetNeedsDraw()"/>;
    ///         otherwise, calls <see cref="UpdateCursor"/> to update the terminal cursor position.
    ///     </para>
    /// </remarks>
    private void Adjust ()
    {
        bool need = false;
        _ = TextModel.CursorColumn (_text, InsertionPoint, 0, out List<int> glyphWidths, out _);
        _ = TextModel.GetColumnWidthsBeforeStart (glyphWidths, ScrollOffset, out _, out int startIndex);
        int tSize = TextModel.DisplaySize (_text, 0, _text.Count).size;
        int pSize = TextModel.DisplaySize (_text, ScrollOffset, InsertionPoint).size;

        // If text is shorter than the viewport, reset scroll to 0
        if (ScrollOffset > 0 && tSize + 1 < Viewport.Width)
        {
            ScrollOffset = 0;
            need = true;
        }

        // If cursor is before the visible area, scroll left to show it
        else if ((InsertionPoint == 0 && ScrollOffset > 0) || InsertionPoint < startIndex)
        {
            ScrollOffset = InsertionPoint;
            need = true;
        }

        // If cursor is exactly at the left edge of the visible area, adjust to ensure it remains visible (handles wide chars)
        else if (ScrollOffset > 0 && InsertionPoint == startIndex)
        {
            ScrollOffset = TextModel.CalculateLeftColumn (_text, ScrollOffset, InsertionPoint, Viewport.Width);
            need = true;
        }

        // If cursor is beyond the visible area, scroll right to show it
        else if (Viewport.Width > 0 && (InsertionPoint - ScrollOffset >= Viewport.Width || pSize >= Viewport.Width))
        {
            ScrollOffset = Math.Max (TextModel.CalculateLeftColumn (_text, ScrollOffset, InsertionPoint, Viewport.Width), 0);
            need = true;
        }

        // If cursor is exactly at the right edge of the visible area, adjust to ensure it remains visible (handles wide chars)
        else if (ScrollOffset > 0 && ((InsertionPoint == _text.Count && pSize < Viewport.Width) || InsertionPoint - startIndex >= Viewport.Width - 1))
        {
            ScrollOffset = Math.Max (TextModel.CalculateLeftColumn (_text, ScrollOffset, InsertionPoint, Viewport.Width), 0);
            need = true;
        }

        if (need)
        {
            SetNeedsDraw ();
        }
        UpdateCursor ();
    }

    private int _insertionPoint;

    /// <summary>
    ///     Gets or sets the insertion point within the text, measured as a 0-based index into text elements.
    /// </summary>
    /// <value>
    ///     The insertion point position, clamped to the range [0, Text.Length]. Position 0 is before the first character;
    ///     position equal to the text length is after the last character.
    /// </value>
    /// <remarks>
    ///     <para>
    ///         This property provides access to the logical insertion point within the text. The value is automatically
    ///         clamped to valid bounds: values less than 0 become 0, and values greater than the text length become
    ///         the text length.
    ///     </para>
    ///     <para>
    ///         <b>Relationship to <see cref="View.Cursor"/>:</b>
    ///         <list type="bullet">
    ///             <item>
    ///                 <description><see cref="InsertionPoint"/>: Logical position in text elements (0-based index)</description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <see cref="UpdateCursor"/>: Converts logical position to screen coordinates, accounting for
    ///                     <see cref="ScrollOffset"/> and wide characters
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         <b>Example:</b> For text "Hello世界" (Hello + 2 CJK characters):
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>InsertionPoint = 0: Before 'H'</description>
    ///             </item>
    ///             <item>
    ///                 <description>InsertionPoint = 5: Before '世'</description>
    ///             </item>
    ///             <item>
    ///                 <description>InsertionPoint = 7: After '界' (end of text)</description>
    ///             </item>
    ///         </list>
    ///         Note that screen columns would differ because '世' and '界' each occupy 2 columns.
    ///     </para>
    ///     <para>
    ///         Setting this property also updates the text selection via <see cref="PrepareSelection"/>.
    ///     </para>
    /// </remarks>
    /// <seealso cref="ScrollOffset"/>
    public virtual int InsertionPoint
    {
        get => _insertionPoint;
        set
        {
            int previousInsertionPoint = _insertionPoint;

            if (value < 0)
            {
                _insertionPoint = 0;
            }
            else if (value > _text.Count)
            {
                _insertionPoint = _text.Count;
            }
            else
            {
                _insertionPoint = value;
            }

            PrepareSelection (_selectionAnchor, _insertionPoint - _selectionAnchor);

            if (_insertionPoint != previousInsertionPoint)
            {
                UpdateCursor ();
            }
        }
    }

    /// <summary>Updates the cursor position.</summary>
    /// <remarks>
    ///     This method calculates the cursor position and calls <see cref="View.SetCursor"/>.
    /// </remarks>
    private void UpdateCursor ()
    {
        ProcessAutocomplete ();

        if (!HasFocus || ReadOnly)
        {
            Cursor = Cursor with { Position = null };

            return;
        }

        // Calculate absolute cursor position and store each glyph width
        int cursorColumn = TextModel.CursorColumn (_text, InsertionPoint, 0, out List<int> glyphWidths, out _);
        _ = TextModel.GetColumnWidthsBeforeStart (glyphWidths, ScrollOffset, out int colOffset, out int viewportX);
        var colsWidth = 0;

        if (glyphWidths.Count > 0)
        {
            for (int i = 0; i < Viewport.X; i++)
            {
                if (i == glyphWidths.Count)
                {
                    break;
                }
                colsWidth += glyphWidths [i];
            }
        }

        for (int idx = viewportX; idx < _text.Count; idx++)
        {
            if (idx == InsertionPoint)
            {
                break;
            }

            int cols = glyphWidths [idx];

            // Viewport.Width is 1 based size, not 0 based index, so it must be used directly here without -1
            if (!TextModel.SetCol (ref colsWidth, Viewport.Width, cols))
            {
                break;
            }
        }

        int pos = colsWidth + Math.Min (Viewport.X, 0);

        Cursor = Cursor with { Position = ViewportToScreen (new Point (pos, 0)) };
    }

    /// <summary>
    ///     Gets the horizontal scroll offset, representing the index of the first visible text element.
    /// </summary>
    /// <value>
    ///     A 0-based index into the text elements indicating which element appears at the left edge of the viewport.
    /// </value>
    /// <remarks>
    ///     <para>
    ///         When the text is longer than the viewport width, this property tracks how far the view has scrolled.
    ///         The <see cref="Adjust"/> method automatically updates this value to keep the cursor visible.
    ///     </para>
    ///     <para>
    ///         <b>Relationship to cursor positioning:</b>
    ///         <list type="bullet">
    ///             <item>
    ///                 <description><see cref="InsertionPoint"/>: Absolute position in the text (0 to text length)</description>
    ///             </item>
    ///             <item>
    ///                 <description><see cref="ScrollOffset"/>: Index of first visible character</description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     Screen column = <see cref="InsertionPoint"/> - <see cref="ScrollOffset"/> (approximately,
    ///                     adjusted for wide chars)
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         <b>Example:</b> For text "Hello World" with viewport width 5:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>ScrollOffset = 0: Shows "Hello"</description>
    ///             </item>
    ///             <item>
    ///                 <description>ScrollOffset = 6: Shows "World"</description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <seealso cref="InsertionPoint"/>
    public int ScrollOffset { get; private set; }
}
