namespace Terminal.Gui.Views;

public partial class TextView
{
    /// <summary>
    ///     Gets or sets the view-specific default key bindings for <see cref="TextView"/>. Contains only bindings
    ///     unique to this view; shared bindings come from <see cref="View.DefaultKeyBindings"/>.
    ///     <para>
    ///         <b>IMPORTANT:</b> This is a process-wide static property. Change with care.
    ///         Do not set in parallelizable unit tests.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Only single-command bindings are included here. Dynamic bindings (e.g., Enter for NewLine/Accept
    ///         depending on <see cref="Multiline"/>, Tab depending on <see cref="TabKeyAddsTab"/>)
    ///         are added directly in the constructor.
    ///     </para>
    /// </remarks>
    public new static Dictionary<Command, PlatformKeyBinding>? DefaultKeyBindings { get; set; } = new ()
    {
        // Emacs navigation
        [Command.Down] = Bind.All (Key.N.WithCtrl),
        [Command.Up] = Bind.All (Key.P.WithCtrl),
        [Command.Right] = Bind.All (Key.F.WithCtrl),
        [Command.Left] = Bind.All (Key.B.WithCtrl),

        // Additional RightEnd binding
        [Command.RightEnd] = Bind.All (Key.E.WithCtrl),

        // Toggle selection mode
        [Command.ToggleExtend] = Bind.All (Key.Space.WithCtrl),

        // Kill / cut line commands
        [Command.CutToEndOfLine] = Bind.All (Key.K.WithCtrl),
        [Command.CutToStartOfLine] = Bind.All (Key.Backspace.WithCtrl.WithShift),
        [Command.DeleteAll] = Bind.All (Key.Delete.WithCtrl.WithShift),

        // Additional Cut binding (Emacs)
        [Command.Cut] = Bind.All (Key.W.WithCtrl),

        // Word navigation
        [Command.WordLeft] = Bind.All (Key.CursorLeft.WithCtrl),
        [Command.WordRight] = Bind.All (Key.CursorRight.WithCtrl),
        [Command.WordLeftExtend] = Bind.All (Key.CursorLeft.WithCtrl.WithShift),
        [Command.WordRightExtend] = Bind.All (Key.CursorRight.WithCtrl.WithShift),

        // Kill word
        [Command.KillWordRight] = Bind.All (Key.Delete.WithCtrl),
        [Command.KillWordLeft] = Bind.All (Key.Backspace.WithCtrl),

        // Overwrite mode
        [Command.ToggleOverwrite] = Bind.All (Key.InsertChar),

        // Open color picker
        [Command.Open] = Bind.All (Key.L.WithCtrl)
    };

    private void CreateCommandsAndBindings ()
    {
        // Things this view knows how to do

        // TextView does nothing on the Accept command (multiline input)
        //AddCommand (Command.Accept, () => false);

        // Note - NewLine is only bound to Enter if Multiline is true
        AddCommand (Command.NewLine, ctx => ProcessEnterKey (ctx));

        // Movement
        AddCommand (Command.PageDown, () => ProcessPageDown ());
        AddCommand (Command.PageDownExtend, () => ProcessPageDownExtend ());
        AddCommand (Command.PageUp, () => ProcessPageUp ());
        AddCommand (Command.PageUpExtend, () => ProcessPageUpExtend ());
        AddCommand (Command.Down, () => ProcessMoveDown ());
        AddCommand (Command.DownExtend, () => ProcessMoveDownExtend ());
        AddCommand (Command.Up, () => ProcessMoveUp ());
        AddCommand (Command.UpExtend, () => ProcessMoveUpExtend ());
        AddCommand (Command.Right, () => ProcessMoveRight ());
        AddCommand (Command.RightExtend, () => ProcessMoveRightExtend ());
        AddCommand (Command.Left, () => ProcessMoveLeft ());
        AddCommand (Command.LeftExtend, () => ProcessMoveLeftExtend ());
        AddCommand (Command.LeftStart, () => ProcessMoveLeftStart ());
        AddCommand (Command.LeftStartExtend, () => ProcessMoveLeftStartExtend ());
        AddCommand (Command.RightEnd, () => ProcessMoveEndOfLine ());
        AddCommand (Command.RightEndExtend, () => ProcessMoveRightEndExtend ());
        AddCommand (Command.WordLeft, () => ProcessMoveWordLeft ());
        AddCommand (Command.WordLeftExtend, () => ProcessMoveWordLeftExtend ());
        AddCommand (Command.WordRight, () => ProcessMoveWordRight ());
        AddCommand (Command.WordRightExtend, () => ProcessMoveWordRightExtend ());
        AddCommand (Command.End, () => MoveBottomEnd ());
        AddCommand (Command.EndExtend, () => MoveBottomEndExtend ());
        AddCommand (Command.Start, () => MoveTopHome ());
        AddCommand (Command.StartExtend, () => MoveTopHomeExtend ());
        AddCommand (Command.ToggleExtend, () => ToggleSelecting ());
        AddCommand (Command.StartSelection, () => StartMouseSelecting ());
        AddCommand (Command.StartRectangleSelection, () => StartMouseRectangleSelecting ());

        // Editing
        AddCommand (Command.SelectAll, () => ProcessSelectAll ());
        AddCommand (Command.CutToEndOfLine, () => CutToEndOfLine ());
        AddCommand (Command.CutToStartOfLine, () => CutToStartOfLine ());

        // Command.Paste uses the default View base handler — sanitization via OnSanitizingPaste,
        // insertion via OnPaste. ProcessPaste's column-track reset happens inside OnPaste.
        AddCommand (Command.Copy, () => ProcessCopy ());
        AddCommand (Command.Cut, () => ProcessCut ());
        AddCommand (Command.DeleteCharLeft, () => ProcessDeleteCharLeft ());
        AddCommand (Command.DeleteCharRight, () => ProcessDeleteCharRight ());
        AddCommand (Command.DeleteAll, () => DeleteAll ());
        AddCommand (Command.KillWordRight, () => ProcessKillWordRight ());
        AddCommand (Command.KillWordLeft, () => ProcessKillWordLeft ());
        AddCommand (Command.Undo, () => Undo ());
        AddCommand (Command.Redo, () => Redo ());

        AddCommand (Command.NextTabStop, () => ProcessTab (true));
        AddCommand (Command.PreviousTabStop, () => ProcessTab (false));
        AddCommand (Command.ToggleOverwrite, () => ProcessSetOverwrite ());
        AddCommand (Command.EnableOverwrite, () => SetOverwrite (true));
        AddCommand (Command.DisableOverwrite, () => SetOverwrite (false));
        AddCommand (Command.Context, () => ShowContextMenu (null));
        AddCommand (Command.Open, () => PromptForColors ());

        // Apply configurable key bindings (base layer + TextView-specific layer)
        ApplyKeyBindings (View.DefaultKeyBindings, DefaultKeyBindings);
        ApplyMouseBindings (View.DefaultMouseBindings);

        KeyBindings.Remove (Key.Space);

        // Dynamic binding: Enter maps to NewLine or Accept depending on Multiline
        KeyBindings.Remove (Key.Enter);
        KeyBindings.Add (Key.Enter, Multiline ? Command.NewLine : Command.Accept);

        // Dynamic bindings: Tab/Shift+Tab (depend on TabKeyAddsTab property)
        KeyBindings.Add (Key.Tab, Command.NextTabStop);
        KeyBindings.Add (Key.Tab.WithShift, Command.PreviousTabStop);
    }

    private void DoNeededAction ()
    {
        if (!NeedsDraw && (IsSelecting || _wrapNeeded || !Used))
        {
            SetNeedsDraw ();
        }

        if (NeedsDraw)
        {
            AdjustViewport ();
        }
        else
        {
            PositionCursor ();
            RaiseUnwrappedCursorPositionChanged ();
        }
        ProcessAutocomplete ();
    }

    /// <summary>
    ///     Open a dialog to set the foreground and background colors.
    /// </summary>
    public bool PromptForColors ()
    {
        Attribute? attribute = App?.TopRunnable?.Prompt<AttributePicker, Attribute?> (input: GetSelectedCellAttribute (),
                                                                                      beginInitHandler: prompt =>
                                                                                                        {
                                                                                                            // Customize the Prompt dialog
                                                                                                            prompt.Title = "Pick an Attribute";
                                                                                                        });

        if (attribute is null)
        {
            return true;
        }

        ApplyCellsAttribute (attribute.Value);

        return true;
    }

    private void SetClipboard (string text) => App?.Clipboard?.SetClipboardData (text);

    private string? _copiedText;
    private List<List<Cell>> _copiedCellsList = [];

    /// <summary>Copy the selected text to the clipboard contents.</summary>
    public bool Copy ()
    {
        SetWrapModel ();

        if (IsSelecting)
        {
            _copiedText = GetRegion (out _copiedCellsList);
            SetClipboard (_copiedText);
            _copyWithoutSelection = false;
        }
        else
        {
            List<Cell> currentLine = GetCurrentLine ();
            _copiedCellsList.Add (currentLine);
            _copiedText = Cell.ToString (currentLine);
            SetClipboard (_copiedText);
            _copyWithoutSelection = true;
        }

        UpdateWrapModel ();
        DoNeededAction ();

        return true;
    }

    /// <summary>Cut the selected text to the clipboard contents.</summary>
    public bool Cut ()
    {
        SetWrapModel ();
        _copiedText = GetRegion (out _copiedCellsList);
        SetClipboard (_copiedText);

        if (!_isReadOnly)
        {
            ClearRegion ();

            _historyText.Add ([ [.. GetCurrentLine ()]], InsertionPoint, TextEditingLineStatus.Replaced);
        }

        UpdateWrapModel ();
        IsSelecting = false;
        DoNeededAction ();
        OnContentsChanged ();

        return true;
    }

    /// <summary>Paste the clipboard contents into the current selected position.</summary>
    /// <remarks>
    ///     Routes through <see cref="Command.Paste"/> with a <see langword="null"/> payload, so the
    ///     default <see cref="View"/> handler reads from <see cref="IApplication.Clipboard"/>,
    ///     sanitizes via <see cref="OnSanitizingPaste"/>, and inserts via <see cref="OnPaste"/>.
    /// </remarks>
    public bool Paste () => InvokeCommand (Command.Paste) is true;

    private void AppendClipboard (string text) => App?.Clipboard?.SetClipboardData (App?.Clipboard?.GetClipboardData () + text);

    private bool ProcessCopy ()
    {
        ResetColumnTrack ();

        return Copy ();
    }

    private bool ProcessCut ()
    {
        ResetColumnTrack ();

        return Cut ();
    }

    /// <summary>Redoes the latest changes.</summary>
    public bool Redo ()
    {
        if (ReadOnly)
        {
            return true;
        }

        _historyText.Redo ();

        return true;
    }

    /// <summary>Undoes the latest changes.</summary>
    public bool Undo ()
    {
        if (ReadOnly)
        {
            return true;
        }

        _historyText.Undo ();
        SetNeedsDraw ();
        AdjustViewport ();

        return true;
    }

    /// <summary>Select all text.</summary>
    public bool SelectAll ()
    {
        if (_model.Count == 0)
        {
            return true;
        }

        StartSelecting ();
        _selectionStartColumn = 0;
        _selectionStartRow = 0;
        CurrentColumn = _model.GetLine (_model.Count - 1).Count;
        CurrentRow = _model.Count - 1;

        return true;
    }

    private bool ToggleSelecting ()
    {
        ResetColumnTrack ();
        IsSelecting = !IsSelecting;
        _selectionStartColumn = CurrentColumn;
        _selectionStartRow = CurrentRow;

        return true;
    }

    private bool StartMouseSelecting ()
    {
        if (!_shiftSelecting)
        {
            _isButtonShift = true;
            StartSelecting ();
        }

        return true;
    }

    private bool StartMouseRectangleSelecting () => StartMouseSelecting ();

    /// <summary>Deletes all text.</summary>
    public bool DeleteAll ()
    {
        if (Lines == 0)
        {
            return true;
        }

        SelectAll ();

        return DeleteCharLeft ();
    }

    /// <summary>Deletes all the selected or a single character at left from the position of the cursor.</summary>
    public bool DeleteCharLeft ()
    {
        if (_isReadOnly)
        {
            return true;
        }

        if (IsSelecting)
        {
            SetWrapModel ();
            _historyText.Add ([ [.. GetCurrentLine ()]], InsertionPoint);

            ClearSelectedRegion ();

            List<Cell> currentLine = GetCurrentLine ();

            _historyText.Add ([ [.. currentLine]], InsertionPoint, TextEditingLineStatus.Replaced);

            UpdateWrapModel ();
            OnContentsChanged ();

            return true;
        }

        bool retValue = DeleteTextLeft ();

        OnContentsChanged ();
        DoNeededAction ();

        return retValue;
    }

    /// <summary>Deletes all the selected or a single character at right from the position of the cursor.</summary>
    public bool DeleteCharRight ()
    {
        if (_isReadOnly)
        {
            return true;
        }

        if (IsSelecting)
        {
            SetWrapModel ();
            _historyText.Add ([ [.. GetCurrentLine ()]], InsertionPoint);

            ClearSelectedRegion ();

            List<Cell> currentLine = GetCurrentLine ();

            _historyText.Add ([ [.. currentLine]], InsertionPoint, TextEditingLineStatus.Replaced);

            UpdateWrapModel ();
            OnContentsChanged ();

            return true;
        }

        bool retValue = DeleteTextRight ();

        DoNeededAction ();
        OnContentsChanged ();

        return retValue;
    }

    private bool DeleteTextLeft ()
    {
        if (CurrentColumn > 0)
        {
            SetWrapModel ();

            // Delete backwards
            List<Cell> currentLine = GetCurrentLine ();

            _historyText.Add ([ [.. currentLine]], InsertionPoint);

            currentLine.RemoveAt (CurrentColumn - 1);

            if (_wordWrap)
            {
                _wrapNeeded = true;
            }

            CurrentColumn--;

            _historyText.Add ([ [.. currentLine]], InsertionPoint, TextEditingLineStatus.Replaced);
        }
        else
        {
            // Merges the current line with the previous one.
            if (CurrentRow == 0)
            {
                return true;
            }

            SetWrapModel ();

            if (CurrentRow - 1 > -1)
            {
                int prowIdx = CurrentRow - 1;
                List<Cell> prevRow = _model.GetLine (prowIdx);

                _historyText.Add ([ [.. prevRow]], InsertionPoint);

                List<List<Cell>> removedLines = [ [.. prevRow], [.. GetCurrentLine ()]];

                _historyText.Add (removedLines, new Point (CurrentColumn, prowIdx), TextEditingLineStatus.Removed);

                int prevCount = prevRow.Count;
                _model.GetLine (prowIdx).AddRange (GetCurrentLine ());
                _model.RemoveLine (CurrentRow);

                CurrentRow--;

                _historyText.Add ([GetCurrentLine ()], new Point (CurrentColumn, prowIdx), TextEditingLineStatus.Replaced);

                CurrentColumn = prevCount;
            }

            if (_wordWrap)
            {
                _wrapNeeded = true;
            }
        }

        // Text was deleted, so it's always needed to redraw and update content size if needed
        SetNeedsDraw ();

        if (_model.ShouldInvalidateMaxWidthCache (CurrentRow, false))
        {
            _model.InvalidateMaxWidthCache ();
            UpdateContentSize ();
        }

        UpdateWrapModel ();

        return true;
    }

    private bool DeleteTextRight ()
    {
        SetWrapModel ();

        List<Cell> currentLine = GetCurrentLine ();

        if (CurrentColumn == currentLine.Count && CurrentRow + 1 == _model.Count)
        {
            // Nothing to delete
            UpdateWrapModel ();

            return true;
        }

        if (CurrentColumn == currentLine.Count)
        {
            // We're at the end of the line; need to merge with the next line
            _historyText.Add ([ [.. currentLine]], InsertionPoint);

            List<List<Cell>> removedLines = [ [.. currentLine]];
            List<Cell> nextLine = _model.GetLine (CurrentRow + 1);
            removedLines.Add ([.. nextLine]);
            _historyText.Add (removedLines, InsertionPoint, TextEditingLineStatus.Removed);
            currentLine.AddRange (nextLine);
            _model.RemoveLine (CurrentRow + 1);

            // Text was deleted, so it's always needed to redraw and update content size if needed
            SetNeedsDraw ();

            // _model.RemoveLine already invalidates the max width cache for the removed line, but we also need to check if the merged line's width changed
            UpdateContentSize ();

            _historyText.Add ([ [.. currentLine]], InsertionPoint, TextEditingLineStatus.Replaced);

            if (_wordWrap)
            {
                _wrapNeeded = true;
            }

            UpdateWrapModel ();

            return true;
        }

        // We're not at the end of the line; delete to end of line
        _historyText.Add ([ [.. currentLine]], InsertionPoint);

        currentLine.RemoveAt (CurrentColumn);

        // Text was deleted, so it's always needed to redraw and update content size if needed
        SetNeedsDraw ();

        if (_model.ShouldInvalidateMaxWidthCache (CurrentRow, false))
        {
            _model.InvalidateMaxWidthCache ();
            UpdateContentSize ();
        }

        _historyText.Add ([ [.. currentLine]], InsertionPoint, TextEditingLineStatus.Replaced);

        if (_wordWrap)
        {
            _wrapNeeded = true;
        }
        UpdateWrapModel ();

        return true;
    }

    private bool CutToEndOfLine ()
    {
        if (_isReadOnly)
        {
            return true;
        }

        if (_model.Count == 1 && GetCurrentLine ().Count == 0)
        {
            // Prevents from adding line feeds if there is no more lines.
            return true;
        }

        SetWrapModel ();

        List<Cell> currentLine = GetCurrentLine ();
        var setLastWasKill = true;

        if (currentLine.Count > 0 && CurrentColumn == currentLine.Count)
        {
            UpdateWrapModel ();

            DeleteTextRight ();

            return true;
        }

        _historyText.Add ([ [.. currentLine]], InsertionPoint);

        if (currentLine.Count == 0)
        {
            if (CurrentRow < _model.Count - 1)
            {
                List<List<Cell>> removedLines = [ [.. currentLine]];

                _model.RemoveLine (CurrentRow);
                SetNeedsDraw ();

                removedLines.Add ([.. GetCurrentLine ()]);

                _historyText.Add ([.. removedLines], InsertionPoint, TextEditingLineStatus.Removed);
            }

            if (_model.Count > 0 || _lastWasKill)
            {
                string val = Environment.NewLine;

                if (_lastWasKill)
                {
                    AppendClipboard (val);
                }
                else
                {
                    SetClipboard (val);
                }
            }

            if (_model.Count == 0)
            {
                // Prevents from adding line feeds if there is no more lines.
                setLastWasKill = false;
            }
        }
        else
        {
            int restCount = currentLine.Count - CurrentColumn;
            List<Cell> rest = currentLine.GetRange (CurrentColumn, restCount);
            var val = string.Empty;
            val += StringFromCells (rest);

            if (_lastWasKill)
            {
                AppendClipboard (val);
            }
            else
            {
                SetClipboard (val);
            }

            currentLine.RemoveRange (CurrentColumn, restCount);
            SetNeedsDraw ();
        }

        _historyText.Add ([ [.. GetCurrentLine ()]], InsertionPoint, TextEditingLineStatus.Replaced);

        UpdateWrapModel ();

        _lastWasKill = setLastWasKill;
        DoNeededAction ();

        return true;
    }

    private bool CutToStartOfLine ()
    {
        if (_isReadOnly)
        {
            return true;
        }

        if (_model.Count == 1 && GetCurrentLine ().Count == 0)
        {
            // Prevents from adding line feeds if there is no more lines.
            return true;
        }

        SetWrapModel ();

        List<Cell> currentLine = GetCurrentLine ();
        var setLastWasKill = true;

        if (currentLine.Count > 0 && CurrentColumn == 0)
        {
            UpdateWrapModel ();

            DeleteTextLeft ();
            OnContentsChanged ();

            return true;
        }

        _historyText.Add ([ [.. currentLine]], InsertionPoint);

        if (currentLine.Count == 0)
        {
            if (CurrentRow > 0)
            {
                _model.RemoveLine (CurrentRow);

                if (_model.Count > 0 || _lastWasKill)
                {
                    string val = Environment.NewLine;

                    if (_lastWasKill)
                    {
                        AppendClipboard (val);
                    }
                    else
                    {
                        SetClipboard (val);
                    }
                }

                if (_model.Count == 0)
                {
                    // Prevents from adding line feeds if there is no more lines.
                    setLastWasKill = false;
                }

                CurrentRow--;
                currentLine = _model.GetLine (CurrentRow);

                List<List<Cell>> removedLine = [ [.. currentLine], []];

                _historyText.Add ([.. removedLine], InsertionPoint, TextEditingLineStatus.Removed);

                CurrentColumn = currentLine.Count;
            }
        }
        else
        {
            int restCount = CurrentColumn;
            List<Cell> rest = currentLine.GetRange (0, restCount);
            var val = string.Empty;
            val += StringFromCells (rest);

            if (_lastWasKill)
            {
                AppendClipboard (val);
            }
            else
            {
                SetClipboard (val);
            }

            currentLine.RemoveRange (0, restCount);
            CurrentColumn = 0;
        }

        _historyText.Add ([ [.. GetCurrentLine ()]], InsertionPoint, TextEditingLineStatus.Replaced);

        UpdateWrapModel ();

        SetNeedsDraw ();

        _lastWasKill = setLastWasKill;
        DoNeededAction ();

        return true;
    }

    private bool KillWordLeft ()
    {
        if (_isReadOnly)
        {
            return true;
        }

        SetWrapModel ();

        List<Cell> currentLine = GetCurrentLine ();

        _historyText.Add ([ [.. GetCurrentLine ()]], InsertionPoint);

        if (CurrentColumn == 0)
        {
            DeleteTextLeft ();
            OnContentsChanged ();

            _historyText.ReplaceLast ([ [.. GetCurrentLine ()]], InsertionPoint, TextEditingLineStatus.Replaced);

            UpdateWrapModel ();

            return true;
        }

        (int col, int row)? newPos = _model.WordBackward (CurrentColumn, CurrentRow, UseSameRuneTypeForWords);

        if (newPos.HasValue && CurrentRow == newPos.Value.row)
        {
            int restCount = CurrentColumn - newPos.Value.col;
            currentLine.RemoveRange (newPos.Value.col, restCount);
            SetNeedsDraw ();

            if (_wordWrap)
            {
                _wrapNeeded = true;
            }

            CurrentColumn = newPos.Value.col;
        }
        else if (newPos.HasValue)
        {
            int restCount;

            if (newPos.Value.row == CurrentRow)
            {
                restCount = currentLine.Count - CurrentColumn;
                currentLine.RemoveRange (CurrentColumn, restCount);
                SetNeedsDraw ();
            }
            else
            {
                while (CurrentRow != newPos.Value.row)
                {
                    restCount = currentLine.Count;
                    currentLine.RemoveRange (0, restCount);
                    SetNeedsDraw ();

                    CurrentRow--;
                    currentLine = GetCurrentLine ();
                }
            }

            if (_wordWrap)
            {
                _wrapNeeded = true;
            }

            CurrentColumn = newPos.Value.col;
            CurrentRow = newPos.Value.row;
        }

        _historyText.Add ([ [.. GetCurrentLine ()]], InsertionPoint, TextEditingLineStatus.Replaced);

        UpdateWrapModel ();

        DoNeededAction ();

        return true;
    }

    private bool KillWordRight ()
    {
        if (_isReadOnly)
        {
            return true;
        }

        SetWrapModel ();

        List<Cell> currentLine = GetCurrentLine ();

        _historyText.Add ([ [.. GetCurrentLine ()]], InsertionPoint);

        if (currentLine.Count == 0 || CurrentColumn == currentLine.Count)
        {
            DeleteTextRight ();

            _historyText.ReplaceLast ([ [.. GetCurrentLine ()]], InsertionPoint, TextEditingLineStatus.Replaced);

            UpdateWrapModel ();

            return true;
        }

        (int col, int row)? newPos = _model.WordForward (CurrentColumn, CurrentRow, UseSameRuneTypeForWords);
        int restCount;

        if (newPos.HasValue && CurrentRow == newPos.Value.row)
        {
            restCount = newPos.Value.col - CurrentColumn;
            currentLine.RemoveRange (CurrentColumn, restCount);
            SetNeedsDraw ();
        }
        else if (newPos.HasValue)
        {
            restCount = currentLine.Count - CurrentColumn;
            currentLine.RemoveRange (CurrentColumn, restCount);
            SetNeedsDraw ();
        }

        if (_wordWrap)
        {
            _wrapNeeded = true;
        }

        _historyText.Add ([ [.. GetCurrentLine ()]], InsertionPoint, TextEditingLineStatus.Replaced);

        UpdateWrapModel ();

        DoNeededAction ();

        return true;
    }

    private bool ProcessDeleteCharLeft ()
    {
        ResetColumnTrack ();

        return DeleteCharLeft ();
    }

    private bool ProcessDeleteCharRight ()
    {
        ResetColumnTrack ();

        return DeleteCharRight ();
    }

    private bool ProcessKillWordLeft ()
    {
        ResetColumnTrack ();

        return KillWordLeft ();
    }

    private bool ProcessKillWordRight ()
    {
        ResetColumnTrack ();
        StopSelecting ();

        return KillWordRight ();
    }

    private bool ProcessEnterKey (ICommandContext? commandContext)
    {
        ResetColumnTrack ();

        if (_isReadOnly)
        {
            return false;
        }

        if (!EnterKeyAddsLine)
        {
            // By Default pressing ENTER should be ignored (OnAccept will return false or null). Only cancel if the
            // event was fired and set Cancel = true.
            return RaiseAccepting (commandContext) is null or false;
        }

        SetWrapModel ();

        List<Cell> currentLine = GetCurrentLine ();
        _historyText.Add ([ [.. currentLine]], InsertionPoint);

        if (IsSelecting)
        {
            ClearSelectedRegion ();
            currentLine = GetCurrentLine ();
        }
        int restCount = currentLine.Count - CurrentColumn;
        List<Cell> rest = currentLine.GetRange (CurrentColumn, restCount);
        currentLine.RemoveRange (CurrentColumn, restCount);
        List<List<Cell>> addedLines = [ [.. currentLine]];
        _model.AddLine (CurrentRow + 1, rest);
        addedLines.Add ([.. _model.GetLine (CurrentRow + 1)]);
        _historyText.Add (addedLines, InsertionPoint, TextEditingLineStatus.Added);
        CurrentRow++;

        if (CurrentRow >= Viewport.Y + Viewport.Height)
        {
            SetNeedsDraw ();
        }

        CurrentColumn = 0;

        _historyText.Add ([ [.. GetCurrentLine ()]], InsertionPoint, TextEditingLineStatus.Replaced);

        if (!_wordWrap && CurrentColumn < Viewport.X)
        {
            SetNeedsDraw ();
        }

        SetNeedsDraw ();
        UpdateWrapModel ();

        DoNeededAction ();
        OnContentsChanged ();

        return true;
    }

    private bool ProcessSelectAll ()
    {
        ResetColumnTrack ();

        return SelectAll ();
    }

    private bool SetOverwrite (bool overwrite)
    {
        Used = overwrite;
        SetNeedsDraw ();
        DoNeededAction ();

        return true;
    }

    private bool ProcessSetOverwrite ()
    {
        ResetColumnTrack ();

        return SetOverwrite (!Used);
    }

    private bool ProcessTab (bool addTab)
    {
        ResetColumnTrack ();

        if (!TabKeyAddsTab || _isReadOnly)
        {
            return false;
        }

        if (addTab)
        {
            InsertText (new Key ((KeyCode)'\t'));
        }
        else
        {
            List<Cell> line = GetCurrentLine ();

            if (CurrentColumn - 1 > -1 && CurrentColumn - 1 < line.Count && line [CurrentColumn - 1].Grapheme == "\t")
            {
                DeleteTextLeft ();
                OnContentsChanged ();
            }
            else
            {
                return true;
            }
        }
        DoNeededAction ();

        return true;
    }

    private bool ShowContextMenu (Point? mousePosition)
    {
        if (!Equals (_currentCulture, Thread.CurrentThread.CurrentUICulture))
        {
            _currentCulture = Thread.CurrentThread.CurrentUICulture;
        }

        mousePosition ??= ViewportToScreen (new Point (InsertionPoint.X, InsertionPoint.Y));

        ContextMenu?.MakeVisible (mousePosition);

        return true;
    }
}
