namespace Terminal.Gui.Views;

public partial class TextView
{
    /// <summary>
    ///     Gets or sets whether pressing ENTER in a <see cref="TextView"/> adds a new line of text
    ///     invokes the <see cref="View.Accepting"/> event.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Setting this property alters <see cref="Multiline"/>.
    ///         If <see cref="EnterKeyAddsLine"/> is set to <see langword="true"/>, then <see cref="Multiline"/> is also set to
    ///         `true` and vice versa.
    ///     </para>
    ///     <para>
    ///         If <see cref="EnterKeyAddsLine"/> is set to <see langword="false"/>, then <see cref="TabKeyAddsTab"/> gets set
    ///         to
    ///         <see langword="false"/>.
    ///     </para>
    /// </remarks>
    public bool EnterKeyAddsLine
    {
        get;
        set
        {
            field = value;

            if (field && !_multiline)
            {
                // BUGBUG: Setting properties should not have side effects like this. Multiline and AllowsReturn should be independent.
                Multiline = true;
            }

            if (!field && _multiline)
            {
                Multiline = false;

                // BUGBUG: Setting properties should not have side effects like this. Multiline and AllowsTab should be independent.
                TabKeyAddsTab = false;
            }

            SetNeedsDraw ();
        }
    } = true;

    private bool _tabKeyAddsTab = true;

    /// <summary>
    ///     Gets or sets whether <see cref="Key.Tab"/> inserts a tab character (<c>\t</c>) into the <see cref="TextView"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Default: <see langword="true"/>
    ///     </para>
    ///     <para>
    ///         If <see cref="TabKeyAddsTab"/> is set to <see langword="true"/>, and the user presses <see cref="Key.Tab"/>,
    ///         a tab character is inserted at the current cursor position; if Shift+<see cref="Key.Tab"/> is pressed,
    ///         a tab character is removed from the current cursor position if one exists.
    ///     </para>
    ///     <para>
    ///         If <see cref="TabKeyAddsTab"/> is set to <see langword="false"/>, the <see cref="Key.Tab"/> event and the
    ///         Shift+<see cref="Key.Tab"/> is bubbled recursively to the up hierarchy. If left unhandled, the app will move
    ///         focus to the next or previous view.
    ///     </para>
    /// </remarks>
    public bool TabKeyAddsTab
    {
        get => _tabKeyAddsTab;
        set
        {
            _tabKeyAddsTab = value;

            if (_tabKeyAddsTab && _tabWidth == 0)
            {
                _tabWidth = 4;
            }

            if (_tabKeyAddsTab && !_multiline)
            {
                Multiline = true;
            }

            if (!_tabKeyAddsTab && _tabWidth > 0)
            {
                _tabWidth = 0;
            }

            SetNeedsDraw ();
        }
    }

    /// <summary>
    ///     Provides autocomplete context menu based on suggestions at the current cursor position. Configure
    ///     <see cref="IAutocomplete.SuggestionGenerator"/> to enable this feature
    /// </summary>
    public IAutocomplete Autocomplete { get; protected set; } = new TextViewAutocomplete ();

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
        if (!key.IsValid)
        {
            return false;
        }

        // Give autocomplete first opportunity to respond to key presses
        if (SelectedLength == 0 && Autocomplete.Suggestions.Count > 0 && Autocomplete.ProcessKey (key))
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    protected override bool OnKeyDownNotHandled (Key a)
    {
        if (!CanFocus)
        {
            return true;
        }

        ResetColumnTrack ();

        if (a == Autocomplete.Reopen)
        {
            GenerateSuggestions ();

            return Autocomplete.ProcessKey (a);
        }

        // Never insert modified keys, except for AltGr combinations with associated text of the unmodified key.
        // This allows users to input characters that require AltGr (e.g. '@' on a Portuguese keyboard which is AltGr+2 with associated text "@"),
        // while still preventing most modified keys from being inserted into the TextView.
        if ((a.IsAlt && string.IsNullOrEmpty (a.AsGrapheme)) || a.IsCtrl)
        {
            // Never insert modified keys
            return false;
        }

        if (a.AsRune is { } rune && rune != default (Rune) && Rune.IsControl (rune))
        {
            return false;
        }

        // Ignore control characters and other special keys
        if (string.IsNullOrEmpty (a.AsGrapheme) && a is { IsKeyCodeAtoZ: false, KeyCode: < KeyCode.Space or > KeyCode.CharMask })
        {
            return false;
        }

        InsertText (a);
        DoNeededAction ();

        return true;
    }

    /// <inheritdoc/>
    protected override void OnSuperViewChanged (ValueChangedEventArgs<View?> args)
    {
        base.OnSuperViewChanged (args);

        if (SuperView is { })
        {
            if (Autocomplete.HostControl is null)
            {
                Autocomplete.HostControl = this;
            }
        }
        else
        {
            Autocomplete.HostControl = null;
        }
    }

    private void ProcessAutocomplete ()
    {
        if (_isDrawing)
        {
            return;
        }

        if (_clickWithSelecting)
        {
            _clickWithSelecting = false;

            return;
        }

        if (SelectedLength > 0)
        {
            return;
        }

        // draw autocomplete
        GenerateSuggestions ();

        var renderAt = new Point (Autocomplete.Context.CursorPosition, Autocomplete.PopupInsideContainer ? InsertionPoint.Y + 1 - Viewport.Y : 0);

        Autocomplete.RenderOverlay (renderAt);
    }

    private void GenerateSuggestions ()
    {
        List<Cell> currentLine = GetCurrentLine ();
        int cursorPosition = Math.Min (CurrentColumn, currentLine.Count);

        Autocomplete.Context = new AutocompleteContext (currentLine, cursorPosition, Autocomplete.Context?.Canceled ?? false);

        Autocomplete.GenerateSuggestions (Autocomplete.Context);
    }
}
