namespace Terminal.Gui.Views;

public partial class TextView
{
    /// <summary>
    ///     Gets or sets whether the word navigation should select only the word itself without spaces around it or with the
    ///     spaces at right.
    ///     Default is <c>false</c> meaning that the spaces at right are included in the selection.
    /// </summary>
    public bool SelectWordOnlyOnDoubleClick { get; set; }

    private bool _isButtonShift;
    private bool _isButtonReleased;
    private int _lastMouseInsertionPointY;
    private int _lastMouseInsertionPointX;

    /// <inheritdoc/>
    protected override bool OnMouseEvent (Mouse mouse)
    {
        if (mouse is { IsSingleDoubleOrTripleClicked: false, IsPressed: false, IsReleased: false, IsWheel: false }
            && !mouse.Flags.HasFlag (MouseFlags.LeftButtonPressed | MouseFlags.PositionReport)
            && !IsMouseCommandBound (mouse, Command.StartSelection)
            && !IsMouseCommandBound (mouse, Command.StartRectangleSelection)
            && !mouse.Flags.HasFlag (MouseFlags.LeftButtonDoubleClicked | MouseFlags.Alt)
            && ContextMenu is { }
            && !mouse.Flags.HasFlag (ContextMenu.MouseFlags))
        {
            return false;
        }

        if (CanFocus && !HasFocus)
        {
            SetFocus ();
        }

        _continuousFind = false;

        // Give autocomplete first opportunity to respond to mouse clicks
        if (SelectedLength == 0 && Autocomplete.OnMouseEvent (mouse, true))
        {
            return true;
        }

        bool startedRectangleSelection = TryInvokeMouseCommand (mouse, Command.StartRectangleSelection);
        bool startedSelection = startedRectangleSelection || TryInvokeMouseCommand (mouse, Command.StartSelection);

        if (mouse.Flags == MouseFlags.LeftButtonClicked)
        {
            if (_isButtonReleased)
            {
                _isButtonReleased = false;

                if (IsSelecting)
                {
                    return true;
                }
            }

            if (_shiftSelecting && !_isButtonShift)
            {
                StopSelecting ();
            }

            ProcessMouseClick (mouse, out _);

            if (Used)
            {
                PositionCursor ();
            }
            else
            {
                SetNeedsDraw ();
            }

            _lastWasKill = false;
            _columnTrack = CurrentColumn;
        }
        else if (mouse.Flags == MouseFlags.WheeledDown)
        {
            _lastWasKill = false;
            _columnTrack = CurrentColumn;
            ScrollTo (new Point (Viewport.X, Viewport.Y + 1));
        }
        else if (mouse.Flags == MouseFlags.WheeledUp)
        {
            _lastWasKill = false;
            _columnTrack = CurrentColumn;
            ScrollTo (new Point (Viewport.X, Viewport.Y - 1));
        }
        else if (mouse.Flags == MouseFlags.WheeledRight)
        {
            _lastWasKill = false;
            _columnTrack = CurrentColumn;
            ScrollTo (new Point (Viewport.X + 1, Viewport.Y));
        }
        else if (mouse.Flags == MouseFlags.WheeledLeft)
        {
            _lastWasKill = false;
            _columnTrack = CurrentColumn;
            ScrollTo (new Point (Viewport.X - 1, Viewport.Y));
        }
        else if (mouse.Flags.FastHasFlags (MouseFlags.LeftButtonPressed | MouseFlags.PositionReport))
        {
            if (!IsSelecting)
            {
                StartSelecting ();
            }

            ProcessMouseClick (mouse, out List<Cell> _);
            PositionCursor ();

            _lastWasKill = false;
            _columnTrack = CurrentColumn;
        }
        else if (startedSelection)
        {
            ProcessMouseClick (mouse, out _);
            PositionCursor ();
            _lastWasKill = false;
            _columnTrack = CurrentColumn;
        }
        else if (mouse.Flags.FastHasFlags (MouseFlags.LeftButtonPressed))
        {
            if (_shiftSelecting)
            {
                _clickWithSelecting = true;
                StopSelecting ();
            }

            ProcessMouseClick (mouse, out _);
            PositionCursor ();

            _lastWasKill = false;
            _columnTrack = CurrentColumn;

            if (App is null || !App.Mouse.IsGrabbed (this))
            {
                App?.Mouse.GrabMouse (this);
            }
        }
        else if (mouse.Flags.FastHasFlags (MouseFlags.LeftButtonReleased))
        {
            _isButtonReleased = true;
            App?.Mouse.UngrabMouse ();
        }
        else if (mouse.Flags.FastHasFlags (MouseFlags.LeftButtonDoubleClicked))
        {
            if (mouse.Flags.HasFlag (MouseFlags.Alt))
            {
                if (!IsSelecting)
                {
                    StartSelecting ();
                }
            }
            else if (IsSelecting)
            {
                StopSelecting ();
            }

            ProcessMouseClick (mouse, out List<Cell> _);

            if (!IsSelecting)
            {
                StartSelecting ();
            }

            (int startCol, int col, int row)? newPos =
                _model.ProcessDoubleClickSelection (SelectionStartColumn, CurrentColumn, CurrentRow, UseSameRuneTypeForWords, SelectWordOnlyOnDoubleClick);

            if (newPos.HasValue)
            {
                SelectionStartColumn = newPos.Value.startCol;
                CurrentColumn = newPos.Value.col;
                CurrentRow = newPos.Value.row;
            }

            PositionCursor ();
            _lastWasKill = false;
            _columnTrack = CurrentColumn;
        }
        else if (mouse.Flags.FastHasFlags (MouseFlags.LeftButtonTripleClicked))
        {
            if (IsSelecting)
            {
                StopSelecting ();
            }

            ProcessMouseClick (mouse, out List<Cell> line);
            CurrentColumn = 0;

            if (!IsSelecting)
            {
                StartSelecting ();
            }

            CurrentColumn = line.Count;
            PositionCursor ();
            _lastWasKill = false;
            _columnTrack = CurrentColumn;
        }
        else if (ContextMenu is { } && mouse.Flags == ContextMenu.MouseFlags)
        {
            ShowContextMenu (mouse.ScreenPosition);
        }

        RaiseUnwrappedCursorPositionChanged ();
        SetNeedsDraw ();

        return true;
    }

    private void ProcessMouseClick (Mouse mouse, out List<Cell> line)
    {
        List<Cell>? r = null;

        if (_model.Count > 0)
        {
            // Clamp mouse inside viewport -> Viewport.Width - 1 and Viewport.Height - 1 is used because they are a 0 based index
            Point p = new (Math.Clamp (mouse.Position!.Value.X, 0, Math.Max (Viewport.Width - 1, 0)),
                           Math.Clamp (mouse.Position!.Value.Y, 0, Math.Max (Viewport.Height - 1, 0)));
            int middleHeight = Viewport.Height / 2;
            int desiredInsertionY = Math.Clamp (Viewport.Y + p.Y, 0, _model.Count);
            int movementY = desiredInsertionY - _lastMouseInsertionPointY;

            if (Viewport.Y + p.Y >= _model.Count || (IsSelecting && p.Y >= Math.Max (Viewport.Height - 1, 0)))
            {
                CurrentRow = _model.Count - 1;
            }
            else if (Viewport.Y + p.Y == 0 || (IsSelecting && p.Y == 0))
            {
                Viewport = Viewport with { Y = CurrentRow = 0 };
            }
            else if (IsSelecting && p.Y > middleHeight && movementY > 0)
            {
                // Scroll down
                int delta = p.Y - middleHeight;
                Viewport = Viewport with { Y = Math.Max (Math.Min (Viewport.Y + delta, Math.Max (0, _model.Count - Viewport.Height)), Viewport.Y) };
                _lastMouseInsertionPointY = CurrentRow = Math.Clamp (Viewport.Y + p.Y, 0, _model.Count);
            }
            else if (IsSelecting && p.Y < middleHeight && movementY < 0)
            {
                // Scroll up
                int delta = middleHeight - p.Y;
                Viewport = Viewport with { Y = Math.Min (Math.Max (0, Viewport.Y - delta), Viewport.Y) };
                _lastMouseInsertionPointY = CurrentRow = Math.Clamp (Viewport.Y + p.Y, 0, _model.Count);
            }
            else if (IsSelecting)
            {
                _lastMouseInsertionPointY = CurrentRow = Viewport.Y + p.Y;
            }
            else
            {
                CurrentRow = Viewport.Y + p.Y;
            }

            r = GetCurrentLine ();
            int colsWidth = TextModel.CursorColumn (TextModel.CellsToStringList (r), r.Count, TabWidth, out List<int> glyphWidths, out _);
            _ = TextModel.GetColumnWidthsBeforeStart (glyphWidths, Viewport.X, out _, out int startIndex);
            int middleWidth = Viewport.Width / 2;
            int desiredInsertionX = Math.Clamp (Viewport.X + p.X, 0, r.Count);
            int movementX = desiredInsertionX - _lastMouseInsertionPointX;

            _ = TextModel.GetColumnWidthsBeforeStart (glyphWidths, Viewport.X + p.X, out int clipOffset, out int newInsertionIndex);

            if (newInsertionIndex < glyphWidths.Count
                && glyphWidths [newInsertionIndex] > 2
                && glyphWidths [newInsertionIndex] + clipOffset <= glyphWidths [newInsertionIndex] / 2)
            {
                newInsertionIndex++;
            }

            if (newInsertionIndex >= r.Count || (IsSelecting && p.X >= Math.Max (Viewport.Width - 1, 0)))
            {
                Viewport = Viewport with { X = Math.Max (0, colsWidth - Viewport.Width + 1) };
                CurrentColumn = r.Count;
            }
            else if (Viewport.X + p.X == 0 || (IsSelecting && p.X == 0))
            {
                Viewport = Viewport with { X = CurrentColumn = 0 };
            }
            else if (IsSelecting && p.X > middleWidth && movementX > 0)
            {
                // Scroll right
                int delta = p.X - middleWidth;
                Viewport = Viewport with { X = Math.Max (Math.Min (Viewport.X + delta, Math.Max (0, colsWidth - Viewport.Width + 1)), Viewport.X) };
                _lastMouseInsertionPointX = CurrentColumn = Math.Clamp (Viewport.X + p.X, 0, r.Count);
            }
            else if (IsSelecting && p.X < middleWidth && movementX < 0)
            {
                // Scroll left
                int delta = middleWidth - p.X;
                Viewport = Viewport with { X = Math.Min (Math.Max (0, Viewport.X - delta), Viewport.X) };
                _lastMouseInsertionPointX = CurrentColumn = Math.Clamp (Viewport.X + p.X, 0, r.Count);
            }
            else if (IsSelecting)
            {
                _lastMouseInsertionPointX = CurrentColumn = Math.Min (Viewport.X + p.X, r.Count);
            }
            else if (mouse.Flags == MouseFlags.LeftButtonClicked && (p.X == 0 || (p.X == 1 && glyphWidths [startIndex] > 1)) && Viewport.X > 0)
            {
                CurrentColumn = startIndex;
                Viewport = Viewport with { X = TextModel.CalculateLeftColumn (r, Viewport.X, CurrentColumn, Viewport.Width, TabWidth) };
                SetNeedsDraw ();
            }
            else if (mouse.Flags == MouseFlags.LeftButtonClicked
                     && (p.X == Math.Max (Viewport.Width - 1, 0) || (p.X == Math.Max (Viewport.Width - 2, 0) && glyphWidths [startIndex + Math.Max (Viewport.Width - 2, 0)] > 1))
                     && startIndex + p.X <= r.Count)
            {
                CurrentColumn = Math.Min (startIndex + p.X, r.Count);
                Viewport = Viewport with { X = TextModel.CalculateLeftColumn (r, Viewport.X, CurrentColumn + 1, Viewport.Width, TabWidth) };
                SetNeedsDraw ();
            }
            else
            {
                CurrentColumn = newInsertionIndex;
            }
        }

        line = r!;
        ProcessAutocomplete ();
    }

    private bool IsMouseCommandBound (Mouse mouse, Command command)
    {
        if (!MouseBindings.TryGet (mouse.Flags, out MouseBinding binding))
        {
            return false;
        }

        return binding.Commands.Contains (command);
    }

    private bool TryInvokeMouseCommand (Mouse mouse, Command command)
    {
        if (!MouseBindings.TryGet (mouse.Flags, out MouseBinding binding))
        {
            return false;
        }

        if (!binding.Commands.Contains (command))
        {
            return false;
        }

        return InvokeCommand (command, binding with { MouseEvent = mouse, Source = new WeakReference<View> (this) }) == true;
    }
}
