namespace Terminal.Gui.Views;

public partial class TextView
{
    private bool _clickWithSelecting;
    private bool _copyWithoutSelection;
    private int _selectionStartColumn;
    private int _selectionStartRow;
    private bool _shiftSelecting;

    /// <summary>Length of the selected text.</summary>
    public int SelectedLength => GetSelectedLength ();

    /// <summary>
    ///     Gets the selected text as
    ///     <see>
    ///         <cref>List{List{Cell}}</cref>
    ///     </see>
    /// </summary>
    public List<List<Cell>> SelectedCellsList
    {
        get
        {
            GetRegion (out List<List<Cell>> selectedCellsList);

            return selectedCellsList;
        }
    }

    /// <summary>The selected text.</summary>
    public string SelectedText
    {
        get
        {
            if (!IsSelecting || (_model.Count == 1 && _model.GetLine (0).Count == 0))
            {
                return string.Empty;
            }

            return GetSelectedRegion ();
        }
    }

    /// <summary>Get or sets whether the user is currently selecting text.</summary>
    public bool IsSelecting { get; set; }

    /// <summary>Start column position of the selected text.</summary>
    public int SelectionStartColumn
    {
        get => _selectionStartColumn;
        set
        {
            List<Cell> line = _model.GetLine (_selectionStartRow);

            _selectionStartColumn = value < 0 ? 0 : value > line.Count ? line.Count : value;
            IsSelecting = true;
            SetNeedsDraw ();
            AdjustViewport ();
        }
    }

    /// <summary>Start row position of the selected text.</summary>
    public int SelectionStartRow
    {
        get => _selectionStartRow;
        set
        {
            _selectionStartRow = value < 0 ? 0 : value > _model.Count - 1 ? Math.Max (_model.Count - 1, 0) : value;
            IsSelecting = true;
            SetNeedsDraw ();
            AdjustViewport ();
        }
    }

    private void ClearSelectedRegion ()
    {
        SetWrapModel ();

        if (!_isReadOnly)
        {
            ClearRegion ();
        }

        UpdateWrapModel ();
        IsSelecting = false;
        DoNeededAction ();
    }

    private int GetSelectedLength () => SelectedText.Length;

    private string GetSelectedRegion ()
    {
        int cRow = CurrentRow;
        int cCol = CurrentColumn;
        int startRow = _selectionStartRow;
        int startCol = _selectionStartColumn;
        TextModel model = _model;

        if (_wordWrap)
        {
            cRow = _wrapManager!.GetModelLineFromWrappedLines (CurrentRow);
            cCol = _wrapManager.GetModelColFromWrappedLines (CurrentRow, CurrentColumn);
            startRow = _wrapManager.GetModelLineFromWrappedLines (_selectionStartRow);
            startCol = _wrapManager.GetModelColFromWrappedLines (_selectionStartRow, _selectionStartColumn);
            model = _wrapManager.Model;
        }

        RaiseUnwrappedCursorPositionChanged (cRow, cCol);

        return GetRegion (out _, startRow, startCol, cRow, cCol, model);
    }

    private bool PointInSelection (int col, int row)
    {
        GetEncodedRegionBounds (out long start, out long end);
        long q = ((long)(uint)row << 32) | (uint)col;

        return q >= start && q <= end - 1;
    }

    private void StartSelecting ()
    {
        if (_shiftSelecting && IsSelecting)
        {
            return;
        }

        _shiftSelecting = true;
        IsSelecting = true;
        _selectionStartColumn = CurrentColumn;
        _selectionStartRow = CurrentRow;
        SetNeedsDraw ();
    }

    private void StopSelecting ()
    {
        if (IsSelecting)
        {
            SetNeedsDraw ();
        }

        _shiftSelecting = false;
        IsSelecting = false;
        _isButtonShift = false;
    }
}
