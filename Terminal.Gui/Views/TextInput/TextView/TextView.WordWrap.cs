using System.Runtime.CompilerServices;

namespace Terminal.Gui.Views;

/// <summary>
///     Word wrapping functionality for <see cref="TextView"/>.
/// </summary>
/// <remarks>
///     <para>
///         This partial class handles word wrapping by maintaining two <see cref="TextModel"/> instances:
///         the original unwrapped model (stored in <see cref="WordWrapManager.Model"/>) and a wrapped
///         model (stored in <see cref="_model"/>) that contains lines broken to fit the viewport width.
///     </para>
///     <para>
///         The <see cref="WordWrapManager"/> manages the bidirectional mapping between wrapped and unwrapped
///         positions, ensuring cursor positions, selections, and text operations work correctly regardless
///         of whether word wrap is enabled.
///     </para>
///     <para>
///         <b>State Management Pattern:</b>
///         To modify text when word wrap is enabled, code must:
///         <list type="number">
///             <item>Call <see cref="SetWrapModel"/> to restore the original unwrapped model</item>
///             <item>Perform text modifications on the unwrapped model</item>
///             <item>Call <see cref="UpdateWrapModel"/> to regenerate the wrapped model and update positions</item>
///         </list>
///         This pattern is critical because text operations (insertions, deletions) must occur on the
///         original unwrapped model to maintain consistency. The <c>CallerMemberName</c> tracking ensures
///         SetWrapModel and UpdateWrapModel are paired correctly within the same method.
///     </para>
///     <para>
///         <b>?? ARCHITECTURAL ISSUES WITH CURRENT IMPLEMENTATION:</b>
///         <list type="bullet">
///             <item>
///                 <b>Fragile Pairing:</b> Relies on developers remembering to call both SetWrapModel and UpdateWrapModel.
///                 Easy to forget UpdateWrapModel or call in wrong order (currently 28 call sites across 8 files).
///             </item>
///             <item>
///                 <b>Hidden State:</b> <c>_currentCaller</c> is module-level state for what should be a scoped operation.
///                 Makes debugging difficult and reasoning about state complex.
///             </item>
///             <item>
///                 <b>Redundant Position Updates:</b> Setting CurrentRow/CurrentColumn in both SetWrapModel (to unwrapped)
///                 and UpdateWrapModel (to wrapped) triggers property setters twice, calling PositionCursor() and
///                 potentially causing intermediate rendering states or flicker.
///             </item>
///             <item>
///                 <b>Error Detection After-the-Fact:</b> InvalidOperationException thrown in UpdateWrapModel happens
///                 after the transaction, not when the error occurs, making debugging harder.
///             </item>
///             <item>
///                 <b>No Exception Safety:</b> If an exception occurs during text modification, the model can be left
///                 in an inconsistent state (unwrapped with no way to re-wrap).
///             </item>
///         </list>
///     </para>
///     <para>
///         <b>?? RECOMMENDED REFACTORING - Action-Based Transaction Pattern:</b>
///         Replace SetWrapModel/UpdateWrapModel with a single method that takes an Action:
///         <code>
///         private void ExecuteWithUnwrappedModel(Action modifyAction)
///         {
///             if (!_wordWrap) { modifyAction(); return; }
///             
///             // Save wrapped positions without triggering setters
///             int wrappedRow = _currentRow, wrappedCol = _currentColumn;
///             int wrappedSelRow = _selectionStartRow, wrappedSelCol = _selectionStartColumn;
///             
///             // Convert to unwrapped (without triggering setters)
///             _currentRow = _wrapManager!.GetModelLineFromWrappedLines(wrappedRow);
///             _currentColumn = _wrapManager.GetModelColFromWrappedLines(wrappedRow, wrappedCol);
///             // ... same for selection ...
///             
///             TextModel originalModel = _model;
///             _model = _wrapManager.Model;
///             
///             try {
///                 modifyAction(); // Execute modification
///                 
///                 // Rewrap
///                 _model = _wrapManager.WrapModel(..., out int nRow, out int nCol, ...);
///                 
///                 // Update positions ONCE with final wrapped coordinates
///                 CurrentRow = nRow; CurrentColumn = nCol;
///                 _wrapNeeded = true; SetNeedsDraw();
///             }
///             catch {
///                 _model = originalModel; // Restore on error
///                 _currentRow = wrappedRow; _currentColumn = wrappedCol;
///                 throw;
///             }
///         }
///         
///         // Usage becomes:
///         ExecuteWithUnwrappedModel(() => {
///             GetCurrentLine().InsertRange(CurrentColumn, cells);
///             CurrentColumn += cells.Count;
///         });
///         </code>
///         <b>Benefits:</b>
///         <list type="bullet">
///             <item>? Impossible to forget the second half of the transaction</item>
///             <item>? No hidden module-level state</item>
///             <item>? Position setters triggered only ONCE with final wrapped coordinates</item>
///             <item>? Exception-safe with automatic rollback</item>
///             <item>? Clear transaction boundary - self-documenting</item>
///             <item>? Easier to debug - all logic in one place</item>
///         </list>
///         <b>Scope:</b> Would require updating 28 call sites across 8 files:
///         TextView.Commands.cs (12), TextView.Files.cs (7), TextView.Find.cs (2),
///         TextView.History.cs (1), TextView.Selection.cs (1), TextView.Text.cs (4), TextView.WordWrap.cs (2)
///     </para>
/// </remarks>
public partial class TextView
{
    private bool _wordWrap;
    private WordWrapManager? _wrapManager;
    private bool _wrapNeeded;

    /// <summary>
    ///     Tracks the calling method name to ensure <see cref="SetWrapModel"/> and <see cref="UpdateWrapModel"/>
    ///     are properly paired. This prevents incorrect nesting of wrap/unwrap operations.
    /// </summary>
    /// <remarks>
    ///     <b>ARCHITECTURAL ISSUE:</b> This field represents hidden module-level state for what should be
    ///     a scoped operation. Consider refactoring to an action-based transaction pattern.
    /// </remarks>
    private string? _currentCaller;

    /// <summary>
    ///     Gets or sets a value indicating whether word wrapping is enabled.
    /// </summary>
    /// <value>
    ///     <see langword="true"/> if text should be wrapped to fit the available container width;
    ///     otherwise, <see langword="false"/>.
    /// </value>
    /// <remarks>
    ///     <para>
    ///         When enabled, long lines are automatically broken at word boundaries to fit within
    ///         <see cref="View.Viewport"/>.Width. The horizontal scrollbar is hidden when word wrap is active.
    ///     </para>
    ///     <para>
    ///         Word wrap can only be enabled if <see cref="Multiline"/> is <see langword="true"/>.
    ///         Attempting to enable word wrap on a single-line TextView has no effect.
    ///     </para>
    ///     <para>
    ///         When toggled, the cursor position is reset to (0,0) and the view is redrawn.
    ///     </para>
    /// </remarks>
    public bool WordWrap
    {
        get => _wordWrap;
        set
        {
            if (value == _wordWrap)
            {
                return;
            }

            if (value && !_multiline)
            {
                return;
            }

            _wordWrap = value;
            ResetPosition ();
            _lastWrapWidth = -1; // Force rewrap on next layout

            if (_wordWrap)
            {
                _wrapManager = new WordWrapManager (_model);
                WrapTextModel ();
            }
            else if (!_wordWrap && _wrapManager is { })
            {
                _model = _wrapManager.Model;
            }

            UpdateContentSize ();
            UpdateHorizontalScrollBarVisibility ();
            SetNeedsDraw ();
        }
    }

    /// <summary>
    ///     INTERNAL: Raises the <see cref="UnwrappedCursorPositionChanged"/> event with the cursor position in the
    ///     original unwrapped model coordinates.
    /// </summary>
    /// <param name="cRow">
    ///     The row in wrapped coordinates, or <see langword="null"/> to use <see cref="CurrentRow"/>.
    /// </param>
    /// <param name="cCol">
    ///     The column in wrapped coordinates, or <see langword="null"/> to use <see cref="CurrentColumn"/>.
    /// </param>
    /// <remarks>
    ///     When word wrap is enabled, the cursor position reported by <see cref="InsertionPoint"/>
    ///     refers to the wrapped model. This method translates that position back to the original
    ///     unwrapped model coordinates, which is useful for reporting the "true" position in the source text.
    /// </remarks>
    private void RaiseUnwrappedCursorPositionChanged (int? cRow = null, int? cCol = null)
    {
        int? row = cRow ?? CurrentRow;
        int? col = cCol ?? CurrentColumn;

        if (cRow is null && cCol is null && _wordWrap)
        {
            row = _wrapManager!.GetModelLineFromWrappedLines (CurrentRow);
            col = _wrapManager.GetModelColFromWrappedLines (CurrentRow, CurrentColumn);
        }

        OnUnwrappedCursorPositionChanged (new Point (col.Value, row.Value));
        UnwrappedCursorPositionChanged?.Invoke (this, new Point (col.Value, row.Value));
    }

    /// <summary>
    ///     Called when the cursor position changes, providing the position in unwrapped model coordinates.
    /// </summary>
    /// <remarks>
    ///     This event is useful for knowing the cursor position in the original text,
    ///     independent of how the text is displayed (wrapped or unwrapped). The <see cref="Point"/>
    ///     provided contains (column, row) in the unwrapped model.
    /// </remarks>
    /// <param name="newUnwrappedCursorPosition"></param>
    protected virtual void OnUnwrappedCursorPositionChanged (Point newUnwrappedCursorPosition) { }

    /// <summary>
    ///     Raised when the cursor position changes, providing the position in unwrapped model coordinates.
    /// </summary>
    /// <remarks>
    ///     This event is useful for knowing the cursor position in the original text,
    ///     independent of how the text is displayed (wrapped or unwrapped). The <see cref="Point"/>
    ///     provided contains (column, row) in the unwrapped model.
    /// </remarks>
    public event EventHandler<Point>? UnwrappedCursorPositionChanged;

    /// <summary>
    ///     INTERNAL: Converts a position in wrapped coordinates to unwrapped (original model) coordinates.
    /// </summary>
    /// <param name="line">The row in wrapped coordinates.</param>
    /// <param name="col">The column in wrapped coordinates.</param>
    /// <returns>A tuple containing (row, column) in unwrapped coordinates.</returns>
    /// <remarks>
    ///     If word wrap is disabled, this method returns the input coordinates unchanged.
    ///     When word wrap is enabled, it uses <see cref="WordWrapManager"/> to map from the
    ///     display position to the position in the original text.
    /// </remarks>
    private (int Row, int Col) GetUnwrappedPosition (int line, int col)
    {
        if (WordWrap)
        {
            return new ValueTuple<int, int> (_wrapManager!.GetModelLineFromWrappedLines (line), _wrapManager.GetModelColFromWrappedLines (line, col));
        }

        return new ValueTuple<int, int> (line, col);
    }

    /// <summary>
    ///     INTERNAL: Prepares for text modification by restoring the original unwrapped model.
    /// </summary>
    /// <param name="caller">
    ///     Automatically populated with the calling method name via <see cref="CallerMemberNameAttribute"/>.
    ///     Used to ensure proper pairing with <see cref="UpdateWrapModel"/>.
    /// </param>
    /// <remarks>
    ///     <para>
    ///         This method is the first step in the word wrap state management pattern. When word wrap
    ///         is enabled, the <see cref="_model"/> field contains the wrapped version of the text.
    ///         Before making any text modifications, this method:
    ///         <list type="number">
    ///             <item>Converts cursor and selection positions from wrapped to unwrapped coordinates</item>
    ///             <item>Restores <see cref="_model"/> to reference the original unwrapped model</item>
    ///             <item>Records the caller's name to ensure <see cref="UpdateWrapModel"/> is called from the same method</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         <b>Important:</b> Every call to <c>SetWrapModel</c> must be followed by a corresponding
    ///         call to <see cref="UpdateWrapModel"/> from the same method. Failure to do so will result
    ///         in an <see cref="InvalidOperationException"/>.
    ///     </para>
    ///     <para>
    ///         If word wrap is disabled, this method does nothing.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     Typical usage pattern:
    ///     <code>
    ///     void InsertText(string text)
    ///     {
    ///         SetWrapModel();
    ///         
    ///         // Perform modifications on the unwrapped model
    ///         GetCurrentLine().Insert(CurrentColumn, text);
    ///         CurrentColumn += text.Length;
    ///         
    ///         UpdateWrapModel();
    ///     }
    ///     </code>
    /// </example>
    private void SetWrapModel ([CallerMemberName] string? caller = null)
    {
        if (_currentCaller is { })
        {
            return;
        }

        if (!_wordWrap)
        {
            return;
        }
        _currentCaller = caller;

        CurrentColumn = _wrapManager!.GetModelColFromWrappedLines (CurrentRow, CurrentColumn);
        CurrentRow = _wrapManager.GetModelLineFromWrappedLines (CurrentRow);

        _selectionStartColumn = _wrapManager.GetModelColFromWrappedLines (_selectionStartRow, _selectionStartColumn);
        _selectionStartRow = _wrapManager.GetModelLineFromWrappedLines (_selectionStartRow);
        _model = _wrapManager.Model;
    }

    /// <summary>
    ///     INTERNAL: Completes text modification by regenerating the wrapped model from the modified unwrapped model.
    /// </summary>
    /// <param name="caller">
    ///     Automatically populated with the calling method name via <see cref="CallerMemberNameAttribute"/>.
    ///     Must match the caller of the corresponding <see cref="SetWrapModel"/> call.
    /// </param>
    /// <remarks>
    ///     <para>
    ///         This method is the second step in the word wrap state management pattern. After text has
    ///         been modified on the unwrapped model (between <see cref="SetWrapModel"/> and this call),
    ///         this method:
    ///         <list type="number">
    ///             <item>Regenerates the wrapped model by rewrapping all lines to fit the viewport width</item>
    ///             <item>Converts cursor and selection positions from unwrapped to wrapped coordinates</item>
    ///             <item>Sets <see cref="_wrapNeeded"/> to trigger wrapping on the next layout</item>
    ///             <item>Clears the <see cref="_currentCaller"/> tracking variable</item>
    ///             <item>Marks the view for redraw</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         <b>Important:</b> This method must be called from the same method that called
    ///         <see cref="SetWrapModel"/>. If called from a different method, or if <c>SetWrapModel</c>
    ///         was not called first, an <see cref="InvalidOperationException"/> is thrown.
    ///     </para>
    ///     <para>
    ///         If word wrap is disabled, this method does nothing.
    ///     </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if <see cref="_currentCaller"/> is not cleared, indicating that <see cref="SetWrapModel"/>
    ///     was called but <c>UpdateWrapModel</c> was not called from the same method. This indicates a
    ///     programming error where the word wrap state was changed mid-operation.
    /// </exception>
    private void UpdateWrapModel ([CallerMemberName] string? caller = null)
    {
        if (_currentCaller is { } && _currentCaller != caller)
        {
            return;
        }

        if (_wordWrap)
        {
            _currentCaller = null;

            _wrapManager!.UpdateModel (_model,
                                       out int nRow,
                                       out int nCol,
                                       out int nStartRow,
                                       out int nStartCol,
                                       CurrentRow,
                                       CurrentColumn,
                                       _selectionStartRow,
                                       _selectionStartColumn,
                                       _tabWidth,
                                       true);
            CurrentRow = nRow;
            CurrentColumn = nCol;
            _selectionStartRow = nStartRow;
            _selectionStartColumn = nStartCol;
            _wrapNeeded = true;

            SetNeedsDraw ();
        }

        if (_currentCaller is { })
        {
            throw new InvalidOperationException ($"WordWrap settings was changed after the {_currentCaller} call.");
        }
    }

    /// <summary>
    ///     INTERNAL: Wraps or rewraps the text model to fit the current Viewport width.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method regenerates the wrapped model whenever the viewport width changes or when
    ///         word wrap is first enabled. It is typically called during layout operations.
    ///     </para>
    ///     <para>
    ///         The wrapping process:
    ///         <list type="bullet">
    ///             <item>Takes each line from the original model</item>
    ///             <item>Breaks it into multiple lines to fit within the viewport width</item>
    ///             <item>Preserves tab expansion based on <see cref="TabWidth"/></item>
    ///             <item>Maintains word boundaries (doesn't break words mid-character)</item>
    ///             <item>Updates cursor and selection positions to match the new wrapped layout</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         The viewport width used for wrapping accounts for the cursor position in non-readonly
    ///         mode by reserving one column for the cursor at the end of lines.
    ///     </para>
    /// </remarks>
    private void WrapTextModel ()
    {
        if (!_wordWrap || _wrapManager is null)
        {
            return;
        }

        _model = _wrapManager.WrapModel (Math.Max (Viewport.Width - (ReadOnly ? 0 : 1), 0), // For the cursor on the last column of a line
                                         out int nRow,
                                         out int nCol,
                                         out int nStartRow,
                                         out int nStartCol,
                                         CurrentRow,
                                         CurrentColumn,
                                         _selectionStartRow,
                                         _selectionStartColumn,
                                         _tabWidth);
        CurrentRow = nRow;
        CurrentColumn = nCol;
        _selectionStartRow = nStartRow;
        _selectionStartColumn = nStartCol;
        SetNeedsDraw ();
    }
}
