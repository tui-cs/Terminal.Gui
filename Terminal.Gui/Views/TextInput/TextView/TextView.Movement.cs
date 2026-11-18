namespace Terminal.Gui.Views;

public partial class TextView
{
    /// <summary>Will scroll the <see cref="TextView"/> to the last line and position the cursor there.</summary>
    public bool MoveEnd ()
    {
        CurrentRow = _model.Count - 1;
        List<Cell> line = GetCurrentLine ();
        CurrentColumn = line.Count;

        if (CurrentRow >= Viewport.Y + Viewport.Height || CurrentColumn >= Viewport.X + Viewport.Width)
        {
            SetNeedsDraw ();
        }
        TrackColumn ();
        DoNeededAction ();

        return true;
    }

    /// <summary>Will scroll the <see cref="TextView"/> to the first line and position the cursor there.</summary>
    public bool MoveHome ()
    {
        CurrentRow = 0;
        CurrentColumn = 0;

        if (Viewport.Y > 0 || Viewport.X > 0)
        {
            SetNeedsDraw ();
        }
        TrackColumn ();
        DoNeededAction ();

        return true;
    }

    /// <summary>
    ///     Will scroll the <see cref="TextView"/> to display the specified row at the top by the <paramref name="position.Y"/>
    ///     and will scroll the <see cref="TextView"/> to display the specified column at the left by the
    ///     <paramref name="position.X"/>.
    /// </summary>
    /// <param name="position">
    ///     The row that should be displayed at the top and the column that should be displayed at the left, if the value
    ///     is negative it will be reset to zero
    /// </param>
    /// <remarks>
    ///     The <see cref="CurrentRow"/> and <see cref="CurrentColumn"/> will not be changed by this method.
    /// </remarks>
    public void ScrollTo (Point position)
    {
        if (position.X < 0)
        {
            position.X = 0;
        }

        if (position.Y < 0)
        {
            position.Y = 0;
        }

        int newPositionX = position.X;

        if (!_wordWrap)
        {
            int maxlength = _model.GetMaxVisibleLine (Viewport.Y, Viewport.Y + Viewport.Height, TabWidth);
            newPositionX = Math.Max (position.X > maxlength - Viewport.Width ? maxlength - Viewport.Width + 1 : position.X, 0);
        }

        Viewport = Viewport with
        {
            X = newPositionX,
            Y = Math.Max (position.Y > _model.Count - 1 - Viewport.Height ? _model.Count - Viewport.Height : position.Y, 0)
        };
        PositionCursor ();
        SetNeedsDraw ();
    }

    private bool MoveBottomEnd ()
    {
        ResetAllTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        return MoveEnd ();
    }

    private bool MoveBottomEndExtend ()
    {
        ResetAllTrack ();
        StartSelecting ();

        return MoveEnd ();
    }

    private bool MoveDown ()
    {
        List<Cell> line = GetCurrentLine ();

        if (CurrentRow + 1 < _model.Count)
        {
            if (_columnTrack == -1)
            {
                _columnTrack = CurrentColumn;
            }

            CurrentRow++;

            if (CurrentRow >= Viewport.Y + Viewport.Height || CurrentRow < Viewport.Y)
            {
                SetNeedsDraw ();
            }

            TrackColumn ();
        }
        else if (CurrentRow > Viewport.Height)
        {
            SetNeedsDraw ();
        }
        else if (CurrentRow == _model.Count - 1 && CurrentColumn < line.Count)
        {
            // Move to the last column of the last row
            CurrentColumn = line.Count;

            if (CurrentColumn > Viewport.X + Viewport.Width)
            {
                SetNeedsDraw ();
            }
        }
        else
        {
            // Let bubbled to the up hierarchy
            return false;
        }

        DoNeededAction ();

        return true;
    }

    private bool MoveEndOfLine ()
    {
        List<Cell> currentLine = GetCurrentLine ();
        CurrentColumn = currentLine.Count;

        if (CurrentColumn >= Viewport.X + Viewport.Width
            || TextModel.CursorColumn (TextModel.CellsToStringList (currentLine), CurrentColumn, TabWidth, out _, out _) - Viewport.X >= Viewport.Width)
        {
            SetNeedsDraw ();
        }
        DoNeededAction ();

        return true;
    }

    private bool MoveLeft ()
    {
        if (CurrentColumn > 0)
        {
            CurrentColumn--;

            List<Cell> currentLine = GetCurrentLine ();
            int cursorColumn = TextModel.CursorColumn (TextModel.CellsToStringList (currentLine), CurrentColumn, TabWidth, out _, out _);

            if ((Viewport.X > 0 && cursorColumn <= Viewport.X) || cursorColumn - Viewport.X >= Viewport.Width)
            {
                SetNeedsDraw ();
            }
        }
        else
        {
            if (CurrentRow > 0)
            {
                CurrentRow--;

                List<Cell> currentLine = GetCurrentLine ();
                CurrentColumn = Math.Max (currentLine.Count - (ReadOnly ? 1 : 0), 0);

                if (CurrentRow < Viewport.Y || CurrentColumn > Viewport.Width)
                {
                    SetNeedsDraw ();
                }
            }
            else
            {
                return true;
            }
        }

        DoNeededAction ();

        return true;
    }

    private bool MoveLeftStart ()
    {
        if (Viewport.X > 0)
        {
            SetNeedsDraw ();
        }

        CurrentColumn = 0;
        DoNeededAction ();

        return true;
    }

    // MovePageDown and MovePageUp are a bit more complex than the other movement methods because they need to move the viewport as well as the cursor. The logic is as follows:
    // 1. Determine the number of lines to move (nPageDnShift or nPageUpShift) based on the height of the viewport.
    // 2. If the current row is within the bounds of the model, check if the column track is set. If not, set it to the current column.
    // 3. For MovePageDown, check if the viewport can be moved down by nPageDnShift lines without exceeding the model count. If so, move the viewport down and set needs draw.
    // 4. For MovePageUp, check if the viewport can be moved up by nPageUpShift lines without going below zero. If so, move the viewport up and set needs draw.
    // 5. For MovePageDown, check if the current row can be moved down by nPageDnShift lines without exceeding the model count. If so, move the current row down. If not, move it to the last line of the model.
    // 6. For MovePageUp, check if the current row can be moved up by nPageUpShift lines without going below zero. If so, move the current row up. If not, move it to the first line of the model.
    // 7. Track the column and position the cursor.
    // 8. Return true to indicate that the movement was handled.
    // The logic for moving the viewport and the cursor is intertwined because when moving a page up or down, we want to ensure that the cursor remains visible within the viewport. Therefore, we need to adjust the viewport position accordingly when the cursor moves beyond the current viewport bounds.
    // The use of the _columnTrack variable is to remember the column position when moving up or down, so that when we move back up or down, we can try to maintain the same column position if possible. This is a common behavior in text editors where moving up or down tries to keep the cursor in the same column if it can.
    // The checks for whether to set needs draw are based on whether the viewport has actually changed. If the viewport position changes, we need to redraw the view to reflect the new visible lines. If the cursor moves outside the current viewport, we also need to redraw to ensure the cursor is visible.
    // MovePageDown and MovePageUp are the only movement methods that does shouldn't call the DoNeededAction method because the AdjustViewport method may cause the viewport location to change with wrongly calculated values.

    private bool MovePageDown ()
    {
        int nPageDnShift = Viewport.Height;

        if (CurrentRow >= 0 && CurrentRow < _model.Count)
        {
            if (_columnTrack == -1)
            {
                _columnTrack = CurrentColumn;
            }

            if (Viewport.Y < Viewport.Y + nPageDnShift && Viewport.Y < _model.Count - nPageDnShift)
            {
                Viewport = Viewport with
                {
                    Y = Viewport.Y + nPageDnShift >= _model.Count
                            ? _model.Count - nPageDnShift
                            : Math.Min (Viewport.Y + nPageDnShift - (nPageDnShift > 1 ? 1 : 0), _model.Count - nPageDnShift)
                };
                SetNeedsDraw ();
            }

            if (CurrentRow + nPageDnShift < _model.Count)
            {
                CurrentRow = CurrentRow + nPageDnShift - (nPageDnShift > 1 ? 1 : 0);
                SetNeedsDraw ();
            }
            else if (CurrentRow < _model.Count)
            {
                CurrentRow = _model.Count - 1;
            }
            TrackColumn ();
            PositionCursor ();
        }

        return true;
    }

    private bool MovePageUp ()
    {
        int nPageUpShift = Viewport.Height;

        if (CurrentRow > 0)
        {
            if (_columnTrack == -1)
            {
                _columnTrack = CurrentColumn;
            }

            if (Viewport.Y > Viewport.Y - nPageUpShift)
            {
                Viewport = Viewport with { Y = Viewport.Y - nPageUpShift < 0 ? 0 : Viewport.Y - nPageUpShift + (nPageUpShift > 1 ? 1 : 0) };
                SetNeedsDraw ();
            }

            if (CurrentRow - nPageUpShift >= 0)
            {
                CurrentRow = CurrentRow - nPageUpShift + (nPageUpShift > 1 ? 1 : 0);
                SetNeedsDraw ();
            }
            else if (CurrentRow > 0)
            {
                CurrentRow = 0;
            }
            TrackColumn ();
            PositionCursor ();
        }

        return true;
    }

    private bool MoveRight ()
    {
        List<Cell> currentLine = GetCurrentLine ();

        if (CurrentColumn < currentLine.Count)
        {
            CurrentColumn++;

            int cursorColumn = TextModel.CursorColumn (TextModel.CellsToStringList (currentLine), CurrentColumn, TabWidth, out _, out _);

            if (cursorColumn >= currentLine.Count || (Viewport.X > 0 && cursorColumn < Viewport.X) || cursorColumn >= Viewport.X + Viewport.Width)
            {
                SetNeedsDraw ();
            }
        }
        else
        {
            if (CurrentRow + 1 < _model.Count)
            {
                CurrentRow++;
                CurrentColumn = 0;

                if (CurrentRow >= Viewport.Y + Viewport.Height || CurrentColumn < Viewport.X)
                {
                    SetNeedsDraw ();
                }
            }
            else
            {
                return true;
            }
        }

        DoNeededAction ();

        return true;
    }

    private bool MoveTopHome ()
    {
        ResetAllTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        return MoveHome ();
    }

    private bool MoveTopHomeExtend ()
    {
        ResetColumnTrack ();
        StartSelecting ();

        return MoveHome ();
    }

    private bool MoveUp ()
    {
        if (CurrentRow > 0 || (CurrentRow == 0 && CurrentRow < Viewport.Y))
        {
            if (_columnTrack == -1)
            {
                _columnTrack = CurrentColumn;
            }

            if (CurrentRow > 0)
            {
                CurrentRow--;
            }

            if (CurrentRow < Viewport.Y || CurrentRow >= Viewport.Y + Viewport.Height)
            {
                SetNeedsDraw ();
            }

            TrackColumn ();
        }
        else if (CurrentRow == 0 && CurrentColumn > 0)
        {
            // Move to the first column of the first row
            CurrentColumn = 0;

            if (Viewport.X > 0)
            {
                SetNeedsDraw ();
            }
        }
        else
        {
            // Let bubbled to the up hierarchy
            return false;
        }

        DoNeededAction ();

        return true;
    }

    private bool MoveWordLeft ()
    {
        (int col, int row)? newPos = _model.WordBackward (CurrentColumn, CurrentRow, UseSameRuneTypeForWords);

        if (newPos.HasValue)
        {
            CurrentColumn = newPos.Value.col;
            CurrentRow = newPos.Value.row;
        }

        List<Cell> currentLine = GetCurrentLine ();
        int cursorColumn = TextModel.CursorColumn (TextModel.CellsToStringList (currentLine), CurrentColumn, TabWidth, out _, out _);

        if (CurrentRow < Viewport.Y || cursorColumn < Viewport.X || cursorColumn >= Viewport.X + Viewport.Width)
        {
            SetNeedsDraw ();
        }

        DoNeededAction ();

        return true;
    }

    private bool MoveWordRight ()
    {
        (int col, int row)? newPos = _model.WordForward (CurrentColumn, CurrentRow, UseSameRuneTypeForWords);

        if (newPos.HasValue)
        {
            CurrentColumn = newPos.Value.col;
            CurrentRow = newPos.Value.row;
        }

        List<Cell> currentLine = GetCurrentLine ();
        int cursorColumn = TextModel.CursorColumn (TextModel.CellsToStringList (currentLine), CurrentColumn, TabWidth, out _, out _);

        if (CurrentRow >= Viewport.Y + Viewport.Height || cursorColumn >= Viewport.X + Viewport.Width || cursorColumn < Viewport.X)
        {
            SetNeedsDraw ();
        }

        DoNeededAction ();

        return true;
    }

    private bool ProcessMoveDown ()
    {
        ResetContinuousFindTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        return MoveDown ();
    }

    private bool ProcessMoveDownExtend ()
    {
        ResetColumnTrack ();
        StartSelecting ();

        return MoveDown ();
    }

    private bool ProcessMoveEndOfLine ()
    {
        ResetAllTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        return MoveEndOfLine ();
    }

    private bool ProcessMoveLeft ()
    {
        // if the user presses Left (without any control keys) and they are at the start of the text
        if (CurrentColumn == 0 && CurrentRow == 0)
        {
            if (!IsSelecting)
            {
                return false;
            }
            StopSelecting ();

            return true;

            // do not respond (this lets the key press fall through to navigation system - which usually changes focus backward)
        }

        ResetAllTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        MoveLeft ();

        return true;
    }

    private bool ProcessMoveLeftExtend ()
    {
        ResetAllTrack ();
        StartSelecting ();

        return MoveLeft ();
    }

    private bool ProcessMoveLeftStart ()
    {
        ResetAllTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        return MoveLeftStart ();
    }

    private bool ProcessMoveLeftStartExtend ()
    {
        ResetAllTrack ();
        StartSelecting ();

        return MoveLeftStart ();
    }

    private bool ProcessMoveRight ()
    {
        // if the user presses Right (without any control keys)
        // determine where the last cursor position in the text is
        int lastRow = _model.Count - 1;
        int lastCol = _model.GetLine (lastRow).Count;

        // if they are at the very end of all the text do not respond (this lets the key press fall through to navigation system - which usually changes focus forward)
        if (CurrentColumn == lastCol && CurrentRow == lastRow)
        {
            // Unless they have text selected
            if (!IsSelecting)
            {
                return false;
            }

            // In which case clear
            StopSelecting ();

            return true;

        }

        ResetAllTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        return MoveRight ();
    }

    private bool ProcessMoveRightEndExtend ()
    {
        ResetAllTrack ();
        StartSelecting ();

        return MoveEndOfLine ();
    }

    private bool ProcessMoveRightExtend ()
    {
        ResetAllTrack ();
        StartSelecting ();

        return MoveRight ();
    }

    private bool ProcessMoveUp ()
    {
        ResetContinuousFindTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        return MoveUp ();
    }

    private bool ProcessMoveUpExtend ()
    {
        ResetColumnTrack ();
        StartSelecting ();

        return MoveUp ();
    }

    private bool ProcessMoveWordLeft ()
    {
        ResetAllTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        return MoveWordLeft ();
    }

    private bool ProcessMoveWordLeftExtend ()
    {
        ResetAllTrack ();
        StartSelecting ();

        return MoveWordLeft ();
    }

    private bool ProcessMoveWordRight ()
    {
        ResetAllTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        return MoveWordRight ();
    }

    private bool ProcessMoveWordRightExtend ()
    {
        ResetAllTrack ();
        StartSelecting ();

        return MoveWordRight ();
    }

    private bool ProcessPageDown ()
    {

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        return MovePageDown ();
    }

    private bool ProcessPageDownExtend ()
    {
        StartSelecting ();

        return MovePageDown ();
    }

    private bool ProcessPageUp ()
    {

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        return MovePageUp ();
    }

    private bool ProcessPageUpExtend ()
    {
        StartSelecting ();

        return MovePageUp ();
    }
}
