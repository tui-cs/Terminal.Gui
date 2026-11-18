namespace Terminal.Gui.Views;

public partial class TextView
{
    // Tracks the last viewport width used for word wrapping to avoid redundant rewraps.
    private int _lastWrapWidth = -1;

    /// <summary>
    ///     Gets or sets a value indicating whether scroll bars are enabled.
    /// </summary>
    /// <remarks>
    ///     When set to true, scroll bars will be displayed if the content exceeds the viewable area.
    /// </remarks>
    public bool ScrollBars
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;

            UpdateScrollBars ();
        }
    }

    private void UpdateScrollBars ()
    {
        // Configure scrollbars to use modern Viewport system
        UpdateHorizontalScrollBarVisibility ();
        AdjustViewport ();
    }

    /// <inheritdoc/>
    protected override void OnViewportChanged (DrawEventArgs e)
    {
        base.OnViewportChanged (e);
        UpdateHorizontalScrollBarVisibility ();

        if (HasFocus)
        {
            PositionCursor ();
        }
    }

    /// <summary>
    ///     Updates the content size based on the current text model dimensions.
    /// </summary>
    private void UpdateContentSize ()
    {
        if (!IsInitialized)
        {
            return;
        }

        int contentHeight = _model.Count;
        int contentWidth = WordWrap ? Viewport.Width : _model.GetMaxVisibleLine (0, _model.Count, TabWidth);

        SetContentSize (new Size (contentWidth + 1, contentHeight));
        UpdateHorizontalScrollBarVisibility ();
    }

    /// <summary>
    ///     Updates the horizontal scrollbar visibility based on WordWrap state.
    /// </summary>
    private void UpdateHorizontalScrollBarVisibility ()
    {
        if (!IsInitialized)
        {
            return;
        }

        // Only directly manage Visible when VisibilityMode is Manual.
        // When Auto/Always/None, ScrollBar.ShowHide() is the sole authority;
        // setting Visible here would fight with it and cause Padding.Thickness
        // to drift (issue #4890).
        if (HorizontalScrollBar.VisibilityMode == ScrollBarVisibilityMode.Manual)
        {
            HorizontalScrollBar.Visible = ScrollBars && !WordWrap && Viewport.Width < GetContentSize ().Width;
        }

        if (VerticalScrollBar.VisibilityMode == ScrollBarVisibilityMode.Manual)
        {
            VerticalScrollBar.Visible = ScrollBars && Viewport.Height < GetContentSize ().Height;
        }
    }

    private void AdjustViewport ()
    {
        List<Cell> line = GetCurrentLine ();

        // Track if content may have changed (vs pure cursor movement)
        // Only update content size when content actually changes to avoid expensive recalculation
        bool contentMayHaveChanged = NeedsDraw || _wrapNeeded || !Used;
        bool need = contentMayHaveChanged;

        (int size, int length) tSize = TextModel.DisplaySize (line, -1, -1, false, TabWidth);
        (int size, int length) dSize = TextModel.DisplaySize (line, 0, CurrentColumn, true, TabWidth);
        _ = TextModel.CursorColumn (TextModel.CellsToStringList (line), CurrentColumn, TabWidth, out List<int> glyphWidths, out _);
        _ = TextModel.GetColumnWidthsBeforeStart (glyphWidths, Viewport.X, out _, out int startIndex);

        // Handle horizontal scrolling (only when CurrentColumn is 0 or WordWrap is off)
        if (Viewport.X > 0 && CurrentColumn <= startIndex)
        {
            if (CurrentColumn == 0)
            {
                Viewport = Viewport with { X = 0 };
            }
            else if (!_wordWrap)
            {
                Viewport = Viewport with { X = TextModel.CalculateLeftColumn (line, Viewport.X, CurrentColumn, Viewport.Width, TabWidth) };
            }
            need = true;
        }
        else if (!_wordWrap && (CurrentColumn - Viewport.X + 1 > Viewport.Width || dSize.size - Viewport.X + 1 >= Viewport.Width))
        {
            Viewport = Viewport with { X = TextModel.CalculateLeftColumn (line, Viewport.X, CurrentColumn, Viewport.Width, TabWidth) };
            need = true;
        }
        else if (tSize.size - Viewport.X + 1 <= Viewport.Width)
        {
            Viewport = Viewport with { X = Math.Max (0, tSize.size - Viewport.Width + 1) };
            need = true;
        }
        else if ((_wordWrap && Viewport.X > 0) || (dSize.size < Viewport.Width && tSize.size < Viewport.Width))
        {
            if (Viewport.X > 0)
            {
                Viewport = Viewport with { X = 0 };
                need = true;
            }
        }

        // Handle vertical scrolling
        if (CurrentRow < Viewport.Y)
        {
            Viewport = Viewport with { Y = CurrentRow };
            need = true;
        }
        else if (CurrentRow - Viewport.Y >= Viewport.Height)
        {
            Viewport = Viewport with { Y = Math.Min (Math.Max (CurrentRow - Viewport.Height + 1, 0), CurrentRow) };
            need = true;
        }

        if (need)
        {
            if (_wrapNeeded)
            {
                WrapTextModel ();
                _wrapNeeded = false;
            }

            SetNeedsDraw ();
        }
        else
        {
            PositionCursor ();
        }

        // Only update content size when content may have changed, not for pure cursor movement
        // This avoids expensive GetMaxVisibleLine() calls on every cursor move
        if (!contentMayHaveChanged)
        {
            return;
        }
        UpdateContentSize ();
        RaiseUnwrappedCursorPositionChanged ();
    }
}
