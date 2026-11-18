namespace Terminal.Gui.Views;

internal class HistoryText
{
    private readonly List<HistoryTextItemEventArgs> _historyTextItems = [];
    private int _idxHistoryText = -1;
    private readonly List<List<Cell>> _originalCellsList = [];
    public bool HasHistoryChanges => _idxHistoryText > -1;
    public bool IsFromHistory { get; private set; }

    public void Add (List<List<Cell>> lines, Point curPos, TextEditingLineStatus lineStatus = TextEditingLineStatus.Original)
    {
        if (lineStatus == TextEditingLineStatus.Original
            && _historyTextItems.Count > 0
            && _historyTextItems.Last ().LineStatus == TextEditingLineStatus.Original)
        {
            return;
        }

        if (lineStatus == TextEditingLineStatus.Replaced
            && _historyTextItems.Count > 0
            && _historyTextItems.Last ().LineStatus == TextEditingLineStatus.Replaced)
        {
            return;
        }

        if (_historyTextItems.Count == 0 && lineStatus != TextEditingLineStatus.Original)
        {
            throw new ArgumentException ("The first item must be the original.");
        }

        if (_idxHistoryText >= 0 && _idxHistoryText + 1 < _historyTextItems.Count)
        {
            _historyTextItems.RemoveRange (_idxHistoryText + 1, _historyTextItems.Count - _idxHistoryText - 1);
        }

        _historyTextItems.Add (new HistoryTextItemEventArgs (lines, curPos, lineStatus));
        _idxHistoryText++;
    }

    public event EventHandler<HistoryTextItemEventArgs?>? ChangeText;

    public void Clear (List<List<Cell>> cellsList)
    {
        _historyTextItems.Clear ();
        _idxHistoryText = -1;
        _originalCellsList.Clear ();

        // Save a copy of the original, not the reference
        foreach (List<Cell> cells in cellsList)
        {
            _originalCellsList.Add ([.. cells]);
        }

        OnChangeText (null);
    }

    public bool IsDirty (List<List<Cell>> cellsList)
    {
        if (cellsList.Count != _originalCellsList.Count)
        {
            return true;
        }

        for (var r = 0; r < cellsList.Count; r++)
        {
            List<Cell> cells = cellsList [r];
            List<Cell> originalCells = _originalCellsList [r];

            if (cells.Count != originalCells.Count)
            {
                return true;
            }

            for (var c = 0; c < cells.Count; c++)
            {
                Cell cell = cells [c];
                Cell originalCell = originalCells [c];

                if (!cell.Equals (originalCell))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void Redo ()
    {
        if (!(_historyTextItems.Count > 0) || _idxHistoryText >= _historyTextItems.Count - 1)
        {
            return;
        }

        IsFromHistory = true;

        _idxHistoryText++;

        var historyTextItem = new HistoryTextItemEventArgs (_historyTextItems [_idxHistoryText]) { IsUndoing = false };

        ProcessChanges (ref historyTextItem);

        IsFromHistory = false;
    }

    public void ReplaceLast (List<List<Cell>> lines, Point curPos, TextEditingLineStatus lineStatus)
    {
        HistoryTextItemEventArgs? found = _historyTextItems.FindLast (x => x.LineStatus == lineStatus);

        if (found is null)
        {
            return;
        }
        found.Lines = lines;
        found.InsertionPoint = curPos;
    }

    public void Undo ()
    {
        if (!(_historyTextItems.Count > 0) || _idxHistoryText <= 0)
        {
            return;
        }

        IsFromHistory = true;

        _idxHistoryText--;

        var historyTextItem = new HistoryTextItemEventArgs (_historyTextItems [_idxHistoryText]) { IsUndoing = true };

        ProcessChanges (ref historyTextItem);

        IsFromHistory = false;
    }

    private void OnChangeText (HistoryTextItemEventArgs? lines) => ChangeText?.Invoke (this, lines);

    private void ProcessChanges (ref HistoryTextItemEventArgs historyTextItem)
    {
        if (historyTextItem.IsUndoing)
        {
            if (_idxHistoryText - 1 > -1
                && (_historyTextItems [_idxHistoryText - 1].LineStatus == TextEditingLineStatus.Added
                    || _historyTextItems [_idxHistoryText - 1].LineStatus == TextEditingLineStatus.Removed
                    || (historyTextItem.LineStatus == TextEditingLineStatus.Replaced
                        && _historyTextItems [_idxHistoryText - 1].LineStatus == TextEditingLineStatus.Original)
                    || (historyTextItem.LineStatus == TextEditingLineStatus.Attribute
                        && _historyTextItems [_idxHistoryText - 1].LineStatus == TextEditingLineStatus.Original)))
            {
                _idxHistoryText--;

                while (_historyTextItems [_idxHistoryText].LineStatus == TextEditingLineStatus.Added
                       && _historyTextItems [_idxHistoryText - 1].LineStatus == TextEditingLineStatus.Removed)
                {
                    _idxHistoryText--;
                }

                historyTextItem = new HistoryTextItemEventArgs (_historyTextItems [_idxHistoryText]) { IsUndoing = true };
                historyTextItem.FinalInsertionPoint = historyTextItem.InsertionPoint;
            }

            if (historyTextItem.LineStatus == TextEditingLineStatus.Removed
                && _historyTextItems [_idxHistoryText + 1].LineStatus == TextEditingLineStatus.Added)
            {
                historyTextItem.RemovedOnAdded = new HistoryTextItemEventArgs (_historyTextItems [_idxHistoryText + 1]);
            }

            if ((historyTextItem.LineStatus == TextEditingLineStatus.Added
                 && _historyTextItems [_idxHistoryText - 1].LineStatus == TextEditingLineStatus.Original)
                || (historyTextItem.LineStatus == TextEditingLineStatus.Removed
                    && _historyTextItems [_idxHistoryText - 1].LineStatus == TextEditingLineStatus.Original)
                || (historyTextItem.LineStatus == TextEditingLineStatus.Added
                    && _historyTextItems [_idxHistoryText - 1].LineStatus == TextEditingLineStatus.Removed))
            {
                if (!historyTextItem.Lines [0].SequenceEqual (_historyTextItems [_idxHistoryText - 1].Lines [0])
                    && historyTextItem.InsertionPoint == _historyTextItems [_idxHistoryText - 1].InsertionPoint)
                {
                    historyTextItem.Lines [0] = new List<Cell> (_historyTextItems [_idxHistoryText - 1].Lines [0]);
                }

                if (historyTextItem.LineStatus == TextEditingLineStatus.Added
                    && _historyTextItems [_idxHistoryText - 1].LineStatus == TextEditingLineStatus.Removed)
                {
                    historyTextItem.FinalInsertionPoint = _historyTextItems [_idxHistoryText - 2].InsertionPoint;
                }
                else
                {
                    historyTextItem.FinalInsertionPoint = _historyTextItems [_idxHistoryText - 1].InsertionPoint;
                }
            }
            else
            {
                historyTextItem.FinalInsertionPoint = historyTextItem.InsertionPoint;
            }

            OnChangeText (historyTextItem);

            while (_historyTextItems [_idxHistoryText].LineStatus == TextEditingLineStatus.Removed
                   || _historyTextItems [_idxHistoryText].LineStatus == TextEditingLineStatus.Added)
            {
                _idxHistoryText--;
            }
        }
        else if (!historyTextItem.IsUndoing)
        {
            if (_idxHistoryText + 1 < _historyTextItems.Count
                && (historyTextItem.LineStatus == TextEditingLineStatus.Original
                    || _historyTextItems [_idxHistoryText + 1].LineStatus == TextEditingLineStatus.Added
                    || _historyTextItems [_idxHistoryText + 1].LineStatus == TextEditingLineStatus.Removed))
            {
                _idxHistoryText++;
                historyTextItem = new HistoryTextItemEventArgs (_historyTextItems [_idxHistoryText]) { IsUndoing = false };
                historyTextItem.FinalInsertionPoint = historyTextItem.InsertionPoint;
            }

            if (historyTextItem.LineStatus == TextEditingLineStatus.Added
                && _historyTextItems [_idxHistoryText - 1].LineStatus == TextEditingLineStatus.Removed)
            {
                historyTextItem.RemovedOnAdded = new HistoryTextItemEventArgs (_historyTextItems [_idxHistoryText - 1]);
            }

            if ((historyTextItem.LineStatus == TextEditingLineStatus.Removed
                 && _historyTextItems [_idxHistoryText + 1].LineStatus == TextEditingLineStatus.Replaced)
                || (historyTextItem.LineStatus == TextEditingLineStatus.Removed
                    && _historyTextItems [_idxHistoryText + 1].LineStatus == TextEditingLineStatus.Original)
                || (historyTextItem.LineStatus == TextEditingLineStatus.Added
                    && _historyTextItems [_idxHistoryText + 1].LineStatus == TextEditingLineStatus.Replaced))
            {
                if (historyTextItem.LineStatus == TextEditingLineStatus.Removed
                    && !historyTextItem.Lines [0].SequenceEqual (_historyTextItems [_idxHistoryText + 1].Lines [0]))
                {
                    historyTextItem.Lines [0] = new List<Cell> (_historyTextItems [_idxHistoryText + 1].Lines [0]);
                }

                historyTextItem.FinalInsertionPoint = _historyTextItems [_idxHistoryText + 1].InsertionPoint;
            }
            else
            {
                historyTextItem.FinalInsertionPoint = historyTextItem.InsertionPoint;
            }

            OnChangeText (historyTextItem);

            while (_historyTextItems [_idxHistoryText].LineStatus == TextEditingLineStatus.Removed
                   || _historyTextItems [_idxHistoryText].LineStatus == TextEditingLineStatus.Added)
            {
                _idxHistoryText++;
            }
        }
    }
}
