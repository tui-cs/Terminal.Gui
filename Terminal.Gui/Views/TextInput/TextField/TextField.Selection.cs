namespace Terminal.Gui.Views;

public partial class TextField
{
    private string? _selectedText;

    /// <summary>Gets The selected text.</summary>
    public string? SelectedText => Secret ? null : _selectedText;

    /// <summary>
    ///     Gets or sets whether the word navigation should select only the word itself without spaces around it or with the
    ///     spaces at right.
    ///     Default is <c>false</c> meaning that the spaces at right are included in the selection.
    /// </summary>
    public bool SelectWordOnlyOnDoubleClick { get; set; }

    /// <summary>
    ///     The normalized start position of the selection for drawing purposes. Unlike <see cref="_selectionAnchor"/>
    ///     (the anchor), this is always the leftmost position of the selection range.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This value is computed by <see cref="SetSelectedStartSelectedLength"/> to ensure:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>When selecting left-to-right: <c>_selectionStart == _selectionAnchor</c></description>
    ///             </item>
    ///             <item>
    ///                 <description>When selecting right-to-left: <c>_selectionStart == _insertionPoint</c></description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         This normalization simplifies rendering logic in <see cref="OnDrawingContent"/> and
    ///         text operations in <see cref="DeleteSelectedText"/>.
    ///     </para>
    /// </remarks>
    private int _selectionStart;

    private void SetSelectedStartSelectedLength ()
    {
        if (SelectedStart > -1 && _insertionPoint < SelectedStart)
        {
            _selectionStart = _insertionPoint;
        }
        else
        {
            _selectionStart = SelectedStart;
        }
    }

    /// <summary>
    ///     Gets the length of the selected text in text elements.
    /// </summary>
    /// <value>
    ///     The number of text elements (graphemes) currently selected. Returns 0 when no text is selected.
    /// </value>
    /// <remarks>
    ///     <para>
    ///         This value represents the absolute length of the selection, regardless of selection direction.
    ///         Use in combination with <see cref="SelectedStart"/> and <see cref="SelectedText"/> to work with selections.
    ///     </para>
    /// </remarks>
    /// <seealso cref="SelectedStart"/>
    /// <seealso cref="SelectedText"/>
    public int SelectedLength { get; private set; }

    private int _selectionAnchor;

    /// <summary>
    ///     Gets or sets the anchor position where text selection began, measured as a 0-based index into text elements.
    /// </summary>
    /// <value>
    ///     The starting position of the selection, or -1 if no selection is active.
    ///     The value is clamped to the range [-1, Text.Length].
    /// </value>
    /// <remarks>
    ///     <para>
    ///         <b>Selection model:</b> TextField uses an anchor-based selection model:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <see cref="SelectedStart"/>: The anchor point where selection began (can be before or
    ///                     after insertion point)
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description><see cref="InsertionPoint"/>: The current end of the selection</description>
    ///             </item>
    ///             <item>
    ///                 <description><see cref="SelectedLength"/>: The absolute length of the selection</description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         <b>Example:</b> In text "Hello World", selecting "World" by shift+clicking:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     If insertion point was at position 6 and user shift-clicks at position 11: SelectedStart=6,
    ///                     InsertionPoint=11
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     If insertion point was at position 11 and user shift-clicks at position 6: SelectedStart=11,
    ///                     InsertionPoint=6
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>In both cases, SelectedLength=5 and SelectedText="World"</description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Setting this property triggers <see cref="PrepareSelection"/> to update the selection state.
    ///     </para>
    /// </remarks>
    /// <seealso cref="SelectedLength"/>
    /// <seealso cref="SelectedText"/>
    /// <seealso cref="InsertionPoint"/>
    public int SelectedStart
    {
        get => _selectionAnchor;
        set
        {
            if (value < -1)
            {
                _selectionAnchor = -1;
            }
            else if (value > _text.Count)
            {
                _selectionAnchor = _text.Count;
            }
            else
            {
                _selectionAnchor = value;
            }

            PrepareSelection (_selectionAnchor, _insertionPoint - _selectionAnchor);
        }
    }

    /// <summary>
    ///     Updates the text selection state based on the anchor position and selection direction.
    /// </summary>
    /// <param name="x">The anchor position (where selection started) as a text element index.</param>
    /// <param name="direction">
    ///     The direction and distance of selection change. Positive values extend right,
    ///     negative values extend left. A value of 0 indicates position-based selection (e.g., from mouse click).
    /// </param>
    /// <remarks>
    ///     <para>
    ///         This method manages the selection state:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>Sets <see cref="_selectionAnchor"/> if not already set and position is valid</description>
    ///             </item>
    ///             <item>
    ///                 <description>Calculates <see cref="SelectedLength"/> based on anchor and direction</description>
    ///             </item>
    ///             <item>
    ///                 <description>Extracts <see cref="SelectedText"/> from the text</description>
    ///             </item>
    ///             <item>
    ///                 <description>Adjusts <see cref="ScrollOffset"/> if selection extends beyond visible area</description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Called by cursor movement methods (with direction) and mouse handling (with direction=0).
    ///     </para>
    /// </remarks>
    private void PrepareSelection (int x, int direction = 0)
    {
        x = x + ScrollOffset < -1 ? 0 : x;

        _selectionAnchor = _selectionAnchor == -1 && _text.Count > 0 && x >= 0 && x <= _text.Count ? x : _selectionAnchor;

        if (_selectionAnchor > -1)
        {
            SelectedLength = Math.Abs (x + direction <= _text.Count ? x + direction - _selectionAnchor : _text.Count - _selectionAnchor);
            SetSelectedStartSelectedLength ();

            if (_selectionStart > -1 && SelectedLength > 0)
            {
                _selectedText = SelectedLength > 0
                                    ? StringExtensions.ToString (_text.GetRange (_selectionStart < 0 ? 0 : _selectionStart,
                                                                                 SelectedLength > _text.Count ? _text.Count : SelectedLength))
                                    : "";

            }
            else if (_selectionStart > -1 && SelectedLength == 0)
            {
                _selectedText = null;
            }

            SetNeedsDraw ();
        }
        else if (SelectedLength > 0 || _selectedText is { })
        {
            ClearAllSelection ();
        }

        if (!_selectingByMouse)
        {
            Adjust ();
        }
        else
        {
            UpdateCursor ();
        }
    }

    /// <summary>Clear the selected text.</summary>
    public void ClearAllSelection ()
    {
        if (_selectionAnchor == -1 && SelectedLength == 0 && string.IsNullOrEmpty (_selectedText))
        {
            return;
        }

        _selectionAnchor = -1;
        SelectedLength = 0;
        _selectedText = null;
        _selectionStart = 0;
        SelectedLength = 0;
        SetNeedsDraw ();
    }
}
