namespace Terminal.Gui.Views;

/// <summary>Single-line text editor.</summary>
/// <remarks>
/// <img src="../images/views/TextField.gif" alt="TextField demo"/>
///     <para>The <see cref="TextField"/> <see cref="View"/> provides editing functionality and mouse support.</para>
///     <para>Default key bindings:</para>
///     <list type="table">
///         <listheader>
///             <term>Key</term> <description>Action</description>
///         </listheader>
///         <item>
///             <term>Left, Ctrl+B</term> <description>Moves the cursor left.</description>
///         </item>
///         <item>
///             <term>Right, Ctrl+F</term> <description>Moves the cursor right.</description>
///         </item>
///         <item>
///             <term>Home, Ctrl+Home</term> <description>Moves the cursor to the start of the text.</description>
///         </item>
///         <item>
///             <term>End, Ctrl+End, Ctrl+E</term> <description>Moves the cursor to the end of the text.</description>
///         </item>
///         <item>
///             <term>Ctrl+Left, Ctrl+Up</term> <description>Moves one word left.</description>
///         </item>
///         <item>
///             <term>Ctrl+Right, Ctrl+Down</term> <description>Moves one word right.</description>
///         </item>
///         <item>
///             <term>Shift+&lt;movement&gt;</term> <description>Extends the selection in the given direction.</description>
///         </item>
///         <item>
///             <term>Delete, Ctrl+D</term> <description>Deletes the character in front of the cursor.</description>
///         </item>
///         <item>
///             <term>Backspace</term> <description>Deletes the character behind the cursor.</description>
///         </item>
///         <item>
///             <term>Ctrl+K</term> <description>Cuts text from the cursor to the end of the line.</description>
///         </item>
///         <item>
///             <term>Ctrl+Shift+K</term> <description>Cuts text from the cursor to the start of the line.</description>
///         </item>
///         <item>
///             <term>Ctrl+Delete</term> <description>Deletes the word to the right of the cursor.</description>
///         </item>
///         <item>
///             <term>Ctrl+Backspace</term> <description>Deletes the word to the left of the cursor.</description>
///         </item>
///         <item>
///             <term>Ctrl+C</term> <description>Copies the selected text to the clipboard.</description>
///         </item>
///         <item>
///             <term>Ctrl+X</term> <description>Cuts the selected text to the clipboard.</description>
///         </item>
///         <item>
///             <term>Ctrl+V</term> <description>Pastes text from the clipboard.</description>
///         </item>
///         <item>
///             <term>Ctrl+A</term> <description>Selects all text.</description>
///         </item>
///         <item>
///             <term>Ctrl+Shift+Delete</term> <description>Deletes all text.</description>
///         </item>
///         <item>
///             <term>Ctrl+Z</term> <description>Undoes the last change.</description>
///         </item>
///         <item>
///             <term>Ctrl+Y</term> <description>Redoes the last undone change.</description>
///         </item>
///         <item>
///             <term>Insert</term> <description>Toggles overwrite mode.</description>
///         </item>
///     </list>
/// </remarks>
public partial class TextField : View, IDesignable, IValue<string>
{
    /// <summary>
    ///     Gets or sets the default cursor style.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static CursorStyle DefaultCursorStyle
    {
        get => TextFieldSettings.Defaults.DefaultCursorStyle;
        set => TextFieldSettings.Defaults.DefaultCursorStyle = value;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="TextField"/> class.
    /// </summary>
    public TextField ()
    {
        _historyText = new HistoryText ();
        _isButtonReleased = true;
        _selectionAnchor = -1;
        _text = [];

        ReadOnly = false;
        Autocomplete = new TextFieldAutocomplete ();
        Height = Dim.Auto (DimAutoStyle.Text, minimumContentDim: 1, maximumContentDim: 1);

        CanFocus = true;
        Used = true;
        MousePositionTracking = true;

        _historyText.ChangeText += HistoryText_ChangeText;

        Initialized += TextField_Initialized;

        CreateCommandsAndBindings ();

        _currentCulture = Thread.CurrentThread.CurrentUICulture;

        Cursor = new Cursor { Style = DefaultCursorStyle };
    }

    private void TextField_Initialized (object? sender, EventArgs e)
    {
        if (!ReadOnly)
        {
            _insertionPoint = GraphemeHelper.GetGraphemeCount (Text);
        }

        if (!ReadOnly && Viewport.Width > 0)
        {
            int colsWidth = Text.GetColumns ();
            ScrollOffset = colsWidth > Viewport.Width + 1 ? colsWidth - Viewport.Width + 1 : 0;
        }

        if (Autocomplete?.HostControl is { })
        {
            return;
        }
        Autocomplete?.HostControl = this;
        Autocomplete?.PopupInsideContainer = false;
    }

    /// <summary>Gets or sets whether the text field is read-only.</summary>
    public bool ReadOnly { get; set; }

    /// <summary>
    ///     Gets or sets whether the text entered is treated as secret (e.g., for passwords).
    ///     <remarks>The displayed text is masked (e.g., with asterisks) when this is set to true.</remarks>
    /// </summary>
    public bool Secret { get; set; }

    /// <inheritdoc/>
    protected override void OnHasFocusChanged (bool newHasFocus, View? previousFocusedView, View? view)
    {
        if (App is { } && App.Mouse.IsGrabbed (this))
        {
            App.Mouse.UngrabMouse ();
        }

        // If gaining focus via keyboard (not mouse), select all text if not ReadOnly
        if (newHasFocus && !ReadOnly && !_focusSetByMouse && _text.Count > 0)
        {
            SelectAll ();
        }

        if (ReadOnly && InsertionPoint > 0)
        {
            _insertionPoint = 0;
        }

        if (ReadOnly && ScrollOffset > 0)
        {
            ScrollOffset = 0;
        }

        if (ReadOnly && SelectedLength > 0)
        {
            ClearAllSelection ();
        }

        // Reset the flag after handling focus change
        _focusSetByMouse = false;

        UpdateCursor ();

        if (newHasFocus)
        {
            CreateContextMenu ();

            if (ContextMenu?.Key is { })
            {
                KeyBindings.Add (ContextMenu.Key, Command.Context);
            }
        }
        else
        {
            if (ContextMenu?.Key is { })
            {
                KeyBindings.Remove (ContextMenu.Key);
            }
            DisposeContextMenu ();
        }
    }

    /// <inheritdoc />
    protected override void OnViewportChanged (DrawEventArgs e)
    {
        base.OnViewportChanged (e);
        Adjust ();
    }

    /// <summary>Get the Context Menu for this view.</summary>
    public PopoverMenu? ContextMenu { get; private set; }

    private void ContextMenu_KeyChanged (object? sender, KeyChangedEventArgs e) => KeyBindings.Replace (e.OldKey.KeyCode, e.NewKey.KeyCode);

    private void CreateContextMenu ()
    {
        DisposeContextMenu ();

        PopoverMenu menu = new (new List<MenuItem>
        {
            new (this, Command.SelectAll),
            new (this, Command.DeleteAll),
            new (this, Command.Copy),
            new (this, Command.Cut),
            new (this, Command.Paste),
            new (this, Command.Undo),
            new (this, Command.Redo)
        })
        {
#if DEBUG
            Id = "textFieldContextMenu"
#endif
        };

        HotKeyBindings.Remove (menu.Key);
        HotKeyBindings.Add (menu.Key, Command.Context);
        menu.KeyChanged += ContextMenu_KeyChanged;

        ContextMenu = menu;
        App?.Popovers?.Register (ContextMenu);
    }

    private void DisposeContextMenu ()
    {
        if (ContextMenu is null)
        {
            return;
        }
        ContextMenu.Visible = false;
        App?.Popovers?.DeRegister (ContextMenu);
        ContextMenu.KeyChanged -= ContextMenu_KeyChanged;
        ContextMenu.Dispose ();
        ContextMenu = null;
    }

    /// <inheritdoc/>
    public virtual bool EnableForDesign ()
    {
        Text = "This is a test.";
        Title = "Caption";

        return true;
    }

    /// <inheritdoc/>
    public virtual string? GetDemoKeyStrokes () => "wait:300,Home,wait:300,`Hello `,wait:800,End,wait:300,` World`,wait:1000";

    #region IValue<string> Implementation

    /// <summary>
    ///     Gets or sets the value of the <see cref="TextField"/>. This is an alias for <see cref="Text"/>.
    /// </summary>
    /// <remarks>
    ///     This property enables <see cref="TextField"/> to be used with the <see cref="IValue{TValue}"/> pattern
    ///     for generic value access and command propagation.
    /// </remarks>
    public string? Value { get => Text; set => Text = value ?? string.Empty; }

    /// <inheritdoc/>
    public event EventHandler<ValueChangingEventArgs<string?>>? ValueChanging;

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<string?>>? ValueChanged;

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<object?>>? ValueChangedUntyped;

    /// <summary>
    ///     Raises the <see cref="ValueChanging"/> event.
    /// </summary>
    private bool RaiseValueChanging (string? currentValue, string? newValue)
    {
        ValueChangingEventArgs<string?> args = new (currentValue, newValue);
        ValueChanging?.Invoke (this, args);

        return args.Handled;
    }

    /// <summary>
    ///     Raises the <see cref="ValueChanged"/> event.
    /// </summary>
    private void RaiseValueChanged (string? oldValue, string? newValue)
    {
        ValueChangedEventArgs<string?> args = new (oldValue, newValue);
        ValueChanged?.Invoke (this, args);
        ValueChangedUntyped?.Invoke (this, new ValueChangedEventArgs<object?> (oldValue, newValue));
    }

    #endregion

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            DisposeContextMenu ();
        }

        base.Dispose (disposing);
    }
}
