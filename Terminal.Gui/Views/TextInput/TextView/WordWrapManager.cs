namespace Terminal.Gui.Views;

/// <summary>
///     Manages word wrapping for a <see cref="TextModel"/> in a <see cref="TextView"/>.
/// </summary>
/// <remarks>
///     The <see cref="WordWrapManager"/> class provides functionality to handle word wrapping for multi-line text.
///     It works with a <see cref="TextModel"/> to manage wrapped lines, calculate positions, and update the model
///     when text is added, removed, or modified. This is used internally by text input controls like
///     <see cref="TextView"/>
///     to ensure proper word wrapping behavior.
/// </remarks>
internal class WordWrapManager (TextModel model)
{
    private int _frameWidth;
    private bool _isWrapModelRefreshing;
    private List<WrappedLine> _wrappedModelLines = [];
    public TextModel Model { get; private set; } = model;

    public int GetModelColFromWrappedLines (int line, int col)
    {
        if (_wrappedModelLines.Count == 0)
        {
            return 0;
        }

        int modelLine = GetModelLineFromWrappedLines (line);
        int firstLine = _wrappedModelLines.IndexOf (r => r._modelLine == modelLine);
        var modelCol = 0;

        for (int i = firstLine; i <= Math.Min (line, _wrappedModelLines.Count - 1); i++)
        {
            WrappedLine wLine = _wrappedModelLines [i];

            if (i < line)
            {
                modelCol += wLine._colWidth;
            }
            else
            {
                modelCol += col;
            }
        }

        return modelCol;
    }

    public int GetModelLineFromWrappedLines (int line) =>
        _wrappedModelLines.Count > 0 ? _wrappedModelLines [Math.Min (line, _wrappedModelLines.Count - 1)]._modelLine : 0;

    public bool Insert (int row, int col, Cell cell)
    {
        List<Cell> line = GetCurrentLine (GetModelLineFromWrappedLines (row));
        line.Insert (GetModelColFromWrappedLines (row, col), cell);

        if (line.Count > _frameWidth)
        {
            return true;
        }

        return false;
    }

    public bool RemoveAt (int row, int col)
    {
        int modelRow = GetModelLineFromWrappedLines (row);
        List<Cell> line = GetCurrentLine (modelRow);
        int modelCol = GetModelColFromWrappedLines (row, col);

        if (modelCol > line.Count)
        {
            Model.RemoveLine (modelRow);
            RemoveAt (row, 0);

            return false;
        }

        if (modelCol < line.Count)
        {
            line.RemoveAt (modelCol);
        }

        if (line.Count > _frameWidth || (row + 1 < _wrappedModelLines.Count && _wrappedModelLines [row + 1]._modelLine == modelRow))
        {
            return true;
        }

        return false;
    }

    public bool RemoveRange (int row, int index, int count)
    {
        int modelRow = GetModelLineFromWrappedLines (row);
        List<Cell> line = GetCurrentLine (modelRow);
        int modelCol = GetModelColFromWrappedLines (row, index);

        try
        {
            line.RemoveRange (modelCol, count);
        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }

    public List<List<Cell>> ToListRune (List<string> textList)
    {
        List<List<Cell>> runesList = new ();

        foreach (string text in textList)
        {
            runesList.Add (Cell.ToCellList (text));
        }

        return runesList;
    }

    public void UpdateModel (TextModel model,
                             out int nRow,
                             out int nCol,
                             out int nStartRow,
                             out int nStartCol,
                             int row,
                             int col,
                             int startRow,
                             int startCol,
                             int tabWidth,
                             bool preserveTrailingSpaces)
    {
        _isWrapModelRefreshing = true;
        Model = model;

        WrapModel (_frameWidth,
                   out nRow,
                   out nCol,
                   out nStartRow,
                   out nStartCol,
                   row,
                   col,
                   startRow,
                   startCol,
                   tabWidth,
                   preserveTrailingSpaces);
        _isWrapModelRefreshing = false;
    }

    public TextModel WrapModel (int width,
                                out int nRow,
                                out int nCol,
                                out int nStartRow,
                                out int nStartCol,
                                int row = 0,
                                int col = 0,
                                int startRow = 0,
                                int startCol = 0,
                                int tabWidth = 0,
                                bool preserveTrailingSpaces = true)
    {
        _frameWidth = width;

        int modelRow = _isWrapModelRefreshing ? row : GetModelLineFromWrappedLines (row);
        int modelCol = _isWrapModelRefreshing ? col : GetModelColFromWrappedLines (row, col);
        int modelStartRow = _isWrapModelRefreshing ? startRow : GetModelLineFromWrappedLines (startRow);
        int modelStartCol = _isWrapModelRefreshing ? startCol : GetModelColFromWrappedLines (startRow, startCol);
        var wrappedModel = new TextModel ();
        var lines = 0;
        nRow = 0;
        nCol = 0;
        nStartRow = 0;
        nStartCol = 0;
        bool isRowAndColSet = row == 0 && col == 0;
        bool isStartRowAndColSet = startRow == 0 && startCol == 0;
        List<WrappedLine> wModelLines = new ();

        for (var i = 0; i < Model.Count; i++)
        {
            List<Cell> line = Model.GetLine (i);

            List<List<Cell>> wrappedLines =
                ToListRune (TextFormatter.Format (Cell.ToString (line), width, Alignment.Start, true, preserveTrailingSpaces, tabWidth, preserveTabs: true));

            for (var j = 0; j < wrappedLines.Count; j++)
            {
                List<Cell> wrapLine = wrappedLines [j];

                if (!isRowAndColSet && modelRow == i)
                {
                    if (nCol + wrapLine.Count <= modelCol)
                    {
                        nCol += wrapLine.Count;
                        nRow = lines;

                        if (nCol == modelCol)
                        {
                            nCol = wrapLine.Count;
                            isRowAndColSet = true;
                        }
                        else if (j == wrappedLines.Count - 1)
                        {
                            nCol = wrapLine.Count - j + modelCol - nCol;
                            isRowAndColSet = true;
                        }
                    }
                    else
                    {
                        int offset = nCol + wrapLine.Count - modelCol;
                        nCol = wrapLine.Count - offset;
                        nRow = lines;
                        isRowAndColSet = true;
                    }
                }

                if (!isStartRowAndColSet && modelStartRow == i)
                {
                    if (nStartCol + wrapLine.Count <= modelStartCol)
                    {
                        nStartCol += wrapLine.Count;
                        nStartRow = lines;

                        if (nStartCol == modelStartCol)
                        {
                            nStartCol = wrapLine.Count;
                            isStartRowAndColSet = true;
                        }
                        else if (j == wrappedLines.Count - 1)
                        {
                            nStartCol = wrapLine.Count - j + modelStartCol - nStartCol;
                            isStartRowAndColSet = true;
                        }
                    }
                    else
                    {
                        int offset = nStartCol + wrapLine.Count - modelStartCol;
                        nStartCol = wrapLine.Count - offset;
                        nStartRow = lines;
                        isStartRowAndColSet = true;
                    }
                }

                for (int k = j; k < wrapLine.Count; k++)
                {
                    Cell cell = wrapLine [k];
                    cell.Attribute = line [k].Attribute;
                    wrapLine [k] = cell;
                }

                wrappedModel.AddLine (lines, wrapLine);

                var wrappedLine = new WrappedLine { _modelLine = i, _row = lines, _rowIndex = j, _colWidth = wrapLine.Count };
                wModelLines.Add (wrappedLine);
                lines++;
            }
        }

        _wrappedModelLines = wModelLines;

        return wrappedModel;
    }

    private List<Cell> GetCurrentLine (int row) => Model.GetLine (row);

    private class WrappedLine
    {
        public int _colWidth;
        public int _modelLine;
        public int _row;
        public int _rowIndex;
    }
}
