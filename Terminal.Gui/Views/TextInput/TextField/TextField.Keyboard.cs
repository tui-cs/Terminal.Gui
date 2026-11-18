namespace Terminal.Gui.Views;

public partial class TextField
{
    /// <summary>
    ///     Provides autocomplete context menu based on suggestions at the current cursor position. Configure
    ///     <see cref="ISuggestionGenerator"/> to enable this feature.
    /// </summary>
    public IAutocomplete? Autocomplete { get; set; }

    private void ProcessAutocomplete ()
    {
        if (_isDrawing)
        {
            return;
        }

        if (SelectedLength > 0)
        {
            return;
        }

        GenerateSuggestions ();

        //DrawAutocomplete ();
    }

    private void GenerateSuggestions ()
    {
        List<Cell> currentLine = Cell.ToCellList (Text);
        int cursorPosition = Math.Min (InsertionPoint, currentLine.Count);

        Autocomplete?.Context = new AutocompleteContext (currentLine, cursorPosition, Autocomplete.Context?.Canceled ?? false);

        Autocomplete?.GenerateSuggestions (Autocomplete.Context);
    }

    /// <inheritdoc/>
    protected override bool OnHandlingHotKey (CommandEventArgs args)
    {
        if (base.OnHandlingHotKey (args))
        {
            return true;
        }

        // If we already have focus, cancel the hotkey so the key passes through
        // to OnKeyDownNotHandled for text input (e.g. the 'E' in "_Enter Path").
        return HasFocus;
    }

    /// <inheritdoc/>
    protected override bool OnKeyDown (Key key)
    {
        // Give autocomplete first opportunity to respond to key presses
        if (SelectedLength == 0 && Autocomplete?.Suggestions.Count > 0 && Autocomplete.ProcessKey (key))
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    protected override bool OnKeyDownNotHandled (Key a)
    {
        // Remember the cursor position because the new calculated cursor position is needed
        // to be set BEFORE the TextChanged event is triggered.
        // Needed for the Elmish Wrapper issue https://github.com/DieselMeister/Terminal.Gui.Elmish/issues/2
        _preChangeInsertionPoint = _insertionPoint;

        if (a.AsRune is { } rune && rune != default (Rune) && Rune.IsControl (rune))
        {
            return false;
        }

        // Never insert modified keys, except for AltGr combinations with associated text of the unmodified key.
        // This allows users to input characters that require AltGr (e.g. '@' on a Portuguese keyboard which is AltGr+2 with associated text "@"),
        // while still preventing most modified keys from being inserted into the TextField.
        if ((a.IsAlt && string.IsNullOrEmpty (a.AsGrapheme)) || a.IsCtrl)
        {
            // Never insert modified keys
            return false;
        }

        // Ignore other control characters.
        if (string.IsNullOrEmpty (a.AsGrapheme) && a is { IsKeyCodeAtoZ: false, KeyCode: < KeyCode.Space or > KeyCode.CharMask })
        {
            return false;
        }

        if (ReadOnly)
        {
            return true;
        }

        InsertText (a, true);

        return true;
    }

    /// <inheritdoc/>
    protected override void OnSuperViewChanged (ValueChangedEventArgs<View?> args)
    {
        base.OnSuperViewChanged (args);

        if (SuperView is { })
        {
            if (Autocomplete?.HostControl is { })
            {
                return;
            }
            Autocomplete?.HostControl = this;
            Autocomplete?.PopupInsideContainer = false;
        }
        else
        {
            Autocomplete?.HostControl = null;
        }
    }
}
