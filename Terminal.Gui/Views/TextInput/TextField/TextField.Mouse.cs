namespace Terminal.Gui.Views;

public partial class TextField
{
    /// <summary>
    ///     Sets the insertion point based on a mouse event by converting the mouse's screen X coordinate
    ///     to a logical text position.
    /// </summary>
    /// <param name="mouse">The mouse event containing the screen position.</param>
    /// <returns>The resulting <see cref="InsertionPoint"/> after positioning.</returns>
    private int SetInsertionPointFromMouse (Mouse mouse)
    {
        // Clamp mouse inside viewport -> Viewport.Width - 1 is used because it's a 0 based index
        int pX = Math.Clamp (mouse.Position!.Value.X, 0, Math.Max (Viewport.Width - 1, 0));

        int colsWidth = TextModel.CursorColumn (_text, _text.Count, 0, out List<int> glyphWidths, out _);
        _ = TextModel.GetColumnWidthsBeforeStart (glyphWidths, ScrollOffset, out _, out int startIndex);
        int middleWidth = Viewport.Width / 2;
        int desiredInsertion = Math.Clamp (ScrollOffset + pX, 0, _text.Count);
        int movement = desiredInsertion - _lastMouseInsertionPoint;

        // Convert relative position to absolute and clamp to valid range
        if (ScrollOffset + pX > _text.Count || (SelectedLength > 0 && pX >= Math.Max (Viewport.Width - 1, 0)))
        {
            ScrollOffset = Math.Max (colsWidth - Viewport.Width + 1, 0);
            InsertionPoint = _text.Count;
        }
        else if (ScrollOffset + pX == 0 || (SelectedLength > 0 && pX == 0))
        {
            InsertionPoint = ScrollOffset = 0;
        }
        else if (SelectedLength > 0 && pX > middleWidth && movement > 0)
        {
            // Scroll right
            int delta = pX - middleWidth;
            ScrollOffset = Math.Max (Math.Min (ScrollOffset + delta, Math.Max (0, colsWidth - Viewport.Width)), ScrollOffset);
            _lastMouseInsertionPoint = InsertionPoint = Math.Clamp (ScrollOffset + pX, 0, _text.Count);
        }
        else if (SelectedLength > 0 && pX < middleWidth && movement < 0)
        {
            // Scroll left
            int delta = middleWidth - pX;
            ScrollOffset = Math.Min (Math.Max (0, ScrollOffset - delta), ScrollOffset);
            _lastMouseInsertionPoint = InsertionPoint = Math.Clamp (ScrollOffset + pX, 0, _text.Count);
        }
        else if (SelectedLength > 0)
        {
            _lastMouseInsertionPoint = InsertionPoint = ScrollOffset + pX;
        }
        else if (mouse.Flags == MouseFlags.LeftButtonClicked && (pX == 0 || (pX == 1 && glyphWidths [startIndex] > 1)) && ScrollOffset > 0)
        {
            InsertionPoint = startIndex;
            ScrollOffset = TextModel.CalculateLeftColumn (_text, ScrollOffset, InsertionPoint, Viewport.Width);
            SetNeedsDraw ();
            UpdateCursor ();
        }
        else if (mouse.Flags == MouseFlags.LeftButtonClicked
                 && (pX == Math.Max (Viewport.Width - 1, 0) || (pX == Math.Max (Viewport.Width - 2, 0) && startIndex + Math.Max (Viewport.Width - 2, 0) < glyphWidths.Count && glyphWidths [startIndex + Math.Max (Viewport.Width - 2, 0)] > 1))
                 && startIndex + pX <= _text.Count)
        {
            InsertionPoint = Math.Min (startIndex + pX, _text.Count);
            ScrollOffset = TextModel.CalculateLeftColumn (_text, ScrollOffset, InsertionPoint + 1, Viewport.Width);
            SetNeedsDraw ();
            UpdateCursor ();
        }
        else
        {
            int cols = TextModel.GetColumnWidthsBeforeStart (glyphWidths, ScrollOffset + pX, out int clipOffset, out int newInsertionIndex);

            InsertionPoint = newInsertionIndex;
        }

        return InsertionPoint;
    }

    private bool _isButtonPressed;
    private bool _isButtonReleased;
    private bool _focusSetByMouse;
    private bool _selectingByMouse;
    private int _lastMouseInsertionPoint;

    /// <inheritdoc/>
    protected override bool OnMouseEvent (Mouse ev)
    {
        if (ev is { IsPressed: false, IsReleased: false }
            && !ev.Flags.HasFlag (MouseFlags.PositionReport)
            && !ev.Flags.HasFlag (MouseFlags.LeftButtonClicked)
            && !ev.Flags.HasFlag (MouseFlags.LeftButtonDoubleClicked)
            && !ev.Flags.HasFlag (MouseFlags.LeftButtonTripleClicked)
            && ContextMenu is { }
            && !ev.Flags.HasFlag (ContextMenu.MouseFlags))
        {
            return false;
        }

        if (!CanFocus)
        {
            return false;
        }

        if (!HasFocus && ev.Flags != MouseFlags.PositionReport)
        {
            _focusSetByMouse = true;
            SetFocus ();
        }

        // Give autocomplete first opportunity to respond to mouse clicks
        if (SelectedLength == 0 && Autocomplete is { } && Autocomplete.OnMouseEvent (ev, true))
        {
            return true;
        }

        if (ev.Flags == MouseFlags.LeftButtonClicked)
        {
            if (_isButtonReleased)
            {
                _isButtonReleased = false;

                if (_selectionAnchor > -1)
                {
                    return true;
                }
            }

            _selectingByMouse = true;
            SetInsertionPointFromMouse (ev);
        }
        else if (ev.Flags == MouseFlags.LeftButtonPressed)
        {
            if (_selectionAnchor > -1)
            {
                ClearAllSelection ();
            }

            EnsureHasFocus ();
            _selectingByMouse = true;
            SetInsertionPointFromMouse (ev);

            _isButtonPressed = true;
        }
        else if (ev.Flags == (MouseFlags.LeftButtonPressed | MouseFlags.PositionReport) && _isButtonPressed)
        {
            _selectingByMouse = true;
            int x = SetInsertionPointFromMouse (ev);
            PrepareSelection (x);

            if (App is null || !App.Mouse.IsGrabbed (this))
            {
                App?.Mouse.GrabMouse (this);
            }
        }
        else if (ev.Flags == MouseFlags.LeftButtonReleased)
        {
            _selectingByMouse = false;
            _isButtonReleased = true;
            _isButtonPressed = false;
            App?.Mouse.UngrabMouse ();
        }
        else if (ev.Flags == MouseFlags.LeftButtonDoubleClicked)
        {
            EnsureHasFocus ();
            int x = SetInsertionPointFromMouse (ev);
            (int startCol, int col, int row)? newPos = GetModel ().ProcessDoubleClickSelection (x, x, 0, UseSameRuneTypeForWords, SelectWordOnlyOnDoubleClick);

            if (newPos is null)
            {
                return true;
            }

            SelectedStart = newPos.Value.startCol;
            InsertionPoint = newPos.Value.col;
        }
        else if (ev.Flags == MouseFlags.LeftButtonTripleClicked)
        {
            EnsureHasFocus ();
            SetInsertionPointFromMouse (ev);
            ClearAllSelection ();
            PrepareSelection (0, _text.Count);
        }
        else if (ev.Flags == ContextMenu?.MouseFlags)
        {
            ShowContextMenu (false);
        }

        return true;

        void EnsureHasFocus ()
        {
            if (HasFocus)
            {
                return;
            }
            _focusSetByMouse = true;
            SetFocus ();
        }
    }
}
