namespace Terminal.Gui.Views;

public partial class TextField
{
    /// <inheritdoc/>
    protected override bool OnDrawingText (DrawContext? context) => true;

    /// <inheritdoc/>
    protected override bool OnGettingAttributeForRole (in VisualRole role, ref Attribute currentAttribute)
    {
        if (role == VisualRole.Normal)
        {
            currentAttribute = GetAttributeForRole (VisualRole.Focus);

            return true;
        }

        return base.OnGettingAttributeForRole (role, ref currentAttribute);
    }

    private bool _isDrawing;

    /// <inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        _isDrawing = true;

        // Cache attributes as GetAttributeForRole might raise events
        var selectedAttribute = new Attribute (GetAttributeForRole (VisualRole.Active));
        Attribute readonlyAttribute = GetAttributeForRole (VisualRole.ReadOnly);
        Attribute normalAttribute = GetAttributeForRole (VisualRole.Editable);

        SetSelectedStartSelectedLength ();

        SetAttribute (GetAttributeForRole (VisualRole.Normal));
        Move (0, 0);

        _ = TextModel.CursorColumn (_text, ScrollOffset, 0, out List<int> glyphWidths, out _);
        _ = TextModel.GetColumnWidthsBeforeStart (glyphWidths, ScrollOffset, out int col, out int viewportX);

        int width = Viewport.Width;
        int tCount = _text.Count;

        for (int idx = viewportX; idx < tCount; idx++)
        {
            string text = _text [idx];
            int cols = text.GetColumns ();

            if (!Enabled)
            {
                // Disabled
                SetAttributeForRole (VisualRole.Disabled);
            }
            else if (idx == _insertionPoint && HasFocus && !Used && SelectedLength == 0 && !ReadOnly)
            {
                // Selected text
                SetAttribute (selectedAttribute);
            }
            else if (ReadOnly)
            {
                SetAttribute (idx >= _selectionStart && SelectedLength > 0 && idx < _selectionStart + SelectedLength ? selectedAttribute : readonlyAttribute);
            }
            else if (!HasFocus && Enabled)
            {
                // Normal text
                SetAttribute (normalAttribute);
            }
            else
            {
                SetAttribute (idx >= _selectionStart && SelectedLength > 0 && idx < _selectionStart + SelectedLength ? selectedAttribute : normalAttribute);
            }

            if (col + cols <= width)
            {
                AddStr (Secret ? Glyphs.Dot.ToString () : text);
            }

            if (!TextModel.SetCol (ref col, width, cols))
            {
                break;
            }

            if (idx + 1 < tCount && col + _text [idx + 1].GetColumns () > width)
            {
                break;
            }
        }

        SetAttribute (normalAttribute);

        // Fill rest of line with spaces
        for (int i = col; i < width; i++)
        {
            AddRune ((Rune)' ');
        }

        RenderCaption ();
        DrawAutocomplete ();

        _isDrawing = false;

        return true;
    }

    private void DrawAutocomplete ()
    {
        if (SelectedLength > 0)
        {
            return;
        }

        if (Autocomplete?.Context is null)
        {
            return;
        }

        var renderAt = new Point (Autocomplete.Context.CursorPosition, 0);

        Autocomplete.RenderOverlay (renderAt);
    }

    private void RenderCaption ()
    {
        if (HasFocus || string.IsNullOrEmpty (Title) || Text.Length > 0)
        {
            return;
        }

        // Ensure TitleTextFormatter has the current Title text
        // (should already be set by the Title property setter, but being defensive)
        if (TitleTextFormatter.Text != Title)
        {
            TitleTextFormatter.Text = Title;
        }

        Attribute captionAttribute = new (GetAttributeForRole (VisualRole.Editable).Foreground.GetDimmerColor (),
                                           GetAttributeForRole (VisualRole.Editable).Background);

        Attribute hotKeyAttribute = new (GetAttributeForRole (VisualRole.Editable).Foreground.GetDimmerColor (),
                                                   GetAttributeForRole (VisualRole.Editable).Background,
                                                   GetAttributeForRole (VisualRole.Editable).Style | TextStyle.Underline);

        // Use TitleTextFormatter to render the caption with hotkey support
        TitleTextFormatter.Draw (Driver, ViewportToScreen (new Rectangle (0, 0, Viewport.Width, 1)), captionAttribute, hotKeyAttribute);
    }
}
