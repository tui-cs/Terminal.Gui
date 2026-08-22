namespace Terminal.Gui.Views;

/// <summary>Masked text editor that validates input through a <see cref="ITextValidateProvider"/>.</summary>
/// <remarks>
/// <img src="../images/views/TextValidateField.gif" alt="TextValidateField demo"/>
///     <para>Default key bindings:</para>
///     <list type="table">
///         <listheader>
///             <term>Key</term> <description>Action</description>
///         </listheader>
///         <item>
///             <term>Left</term> <description>Moves the cursor left.</description>
///         </item>
///         <item>
///             <term>Right</term> <description>Moves the cursor right.</description>
///         </item>
///         <item>
///             <term>Home</term> <description>Moves the cursor to the start.</description>
///         </item>
///         <item>
///             <term>End</term> <description>Moves the cursor to the end.</description>
///         </item>
///         <item>
///             <term>Delete</term> <description>Deletes the character at the cursor.</description>
///         </item>
///         <item>
///             <term>Backspace</term> <description>Deletes the character before the cursor.</description>
///         </item>
///     </list>
/// </remarks>
public class TextValidateField : View, IDesignable, IValue<string>
{
    private const int DEFAULT_LENGTH = 10;

    private ITextValidateProvider? _provider;
    private string _lastKnownText = string.Empty;

    /// <summary>
    ///     Gets or sets the default key bindings for <see cref="TextValidateField"/>. All standard bindings are
    ///     inherited from <see cref="View.DefaultKeyBindings"/>, so this dictionary is empty by default.
    ///     <para>
    ///         <b>IMPORTANT:</b> This is a process-wide static property. Change with care.
    ///         Do not set in parallelizable unit tests.
    ///     </para>
    /// </summary>
    public new static Dictionary<Command, PlatformKeyBinding>? DefaultKeyBindings { get; set; } = new ();

    /// <summary>
    ///     Gets or sets whether value change events are suppressed.
    ///     Subclasses set this to <see langword="true"/> when programmatically updating the provider
    ///     to prevent re-entrant event firing.
    /// </summary>
    protected bool SuppressValueEvents { get; set; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="TextValidateField"/> class.
    /// </summary>
    public TextValidateField ()
    {
        Height = Dim.Auto (minimumContentDim: 1);
        CanFocus = true;

        // Things this view knows how to do
        AddCommand (Command.LeftStart, () => HomeKeyHandler ());
        AddCommand (Command.RightEnd, () => EndKeyHandler ());
        AddCommand (Command.DeleteCharRight, () => DeleteKeyHandler ());
        AddCommand (Command.DeleteCharLeft, () => BackspaceKeyHandler ());
        AddCommand (Command.Left, () => CursorLeft ());
        AddCommand (Command.Right, () => CursorRight ());

        // Apply layered key bindings (base View layer + TextValidateField-specific layer)
        ApplyKeyBindings (View.DefaultKeyBindings, DefaultKeyBindings);
    }

    /// <inheritdoc/>
    protected override void OnHasFocusChanged (bool newHasFocus, View? previousFocusedView, View? view)
    {
        if (!newHasFocus)
        {
            Cursor = Cursor with { Position = null };

            return;
        }

        if (_provider is null)
        {
            return;
        }

        // When we gain focus, put the insertion point at the start if it's before the start.
        InsertionPoint = Math.Max (InsertionPoint, _provider.CursorStart ());

        // Match the cursor position to the insertion point.
        // Don't call UpdateCursor so we can set the style too.
        Cursor = Cursor with { Style = CursorStyle.BlinkingBar };
        UpdateCursor ();
    }

    /// <summary>Updates the cursor position.</summary>
    /// <remarks>
    ///     This method calculates the cursor position and calls <see cref="View.SetCursor"/>.
    /// </remarks>
    private void UpdateCursor ()
    {
        (int left, int right) = GetMargins (Viewport.Width);

        // Fixed = true, is for inputs that have fixed width, like masked ones.
        // Fixed = false, is for normal input.
        // When it's right-aligned and it's a normal input, the cursor behaves differently.
        int curPos;

        if (_provider?.Fixed == false && TextAlignment == Alignment.End)
        {
            curPos = _insertionPoint + left - 1;
        }
        else
        {
            curPos = _insertionPoint + left;
        }

        Cursor = Cursor with { Position = ViewportToScreen (new Point (curPos, 0)) };
        SetNeedsDraw ();
    }

    /// <summary>This property returns true if the input is valid.</summary>
    public virtual bool IsValid
    {
        get
        {
            if (_provider is null)
            {
                return false;
            }

            return _provider.IsValid;
        }
    }

    /// <summary>Provider</summary>
    public ITextValidateProvider? Provider
    {
        get => _provider;
        set
        {
            if (_provider is { })
            {
                _provider.TextChanged -= ProviderOnTextChanged;
            }

            _provider = value;

            if (_provider is { })
            {
                _provider.TextChanged += ProviderOnTextChanged;
                _lastKnownText = _provider.Text;
                SetTextDirect (_provider.Text);
            }

            if (_provider!.Fixed)
            {
                // Add one so there is always a blank cell after the last editable character for the cursor.
                Width = (_provider.DisplayText == string.Empty ? DEFAULT_LENGTH : _provider.DisplayText.Length) + 1;
            }

            // HomeKeyHandler already call SetNeedsDisplay
            HomeKeyHandler ();
        }
    }

    #region IValue<string> Implementation

    /// <summary>
    ///     Gets or sets the value. This is an alias for <see cref="Text"/>.
    /// </summary>
    /// <remarks>
    ///     This property enables <see cref="TextValidateField"/> to be used with the <see cref="IValue{TValue}"/> pattern
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
    ///     Called when <see cref="Value"/> is about to change.
    ///     Allows derived classes to cancel the change.
    /// </summary>
    /// <param name="args">The event arguments.</param>
    /// <returns><see langword="true"/> to cancel the change; otherwise <see langword="false"/>.</returns>
    protected virtual bool OnValueChanging (ValueChangingEventArgs<string?> args) => false;

    /// <summary>
    ///     Called when <see cref="Value"/> has changed.
    ///     Allows derived classes to react to value changes.
    /// </summary>
    /// <param name="args">The event arguments.</param>
    protected virtual void OnValueChanged (ValueChangedEventArgs<string?> args) { }

    #endregion

    /// <summary>
    ///     Stashes the final text value determined during <see cref="OnTextChanging"/> (after possible
    ///     subscriber modification) for use in <see cref="OnTextChanged"/>.
    /// </summary>
    private string? _pendingValidatedText;

    /// <inheritdoc/>
    protected override bool OnTextChanging (string newText)
    {
        if (_provider is null || SuppressValueEvents)
        {
            return base.OnTextChanging (newText);
        }

        string oldValue = _provider.Text;

        if (oldValue == newText)
        {
            return true;
        }

        ValueChangingEventArgs<string?> args = new (oldValue, newText);

        if (OnValueChanging (args) || args.Handled)
        {
            return true;
        }

        ValueChanging?.Invoke (this, args);

        if (args.Handled)
        {
            return true;
        }

        // Allow subscribers to modify the new value
        _pendingValidatedText = args.NewValue ?? string.Empty;

        return base.OnTextChanging (newText);
    }

    /// <summary>Text</summary>
    protected override void OnTextChanged ()
    {
        string value = _pendingValidatedText ?? Text;
        _pendingValidatedText = null;

        if (_provider is null)
        {
            base.OnTextChanged ();

            return;
        }

        string oldValue = _provider.Text;

        if (oldValue == value)
        {
            base.OnTextChanged ();

            return;
        }

        // Apply subscriber-modified value if different from what the base stored
        if (value != Text)
        {
            SetTextDirect (value);
        }

        _lastKnownText = value;
        _provider.Text = value;

        // The provider may transform the text (e.g., mask formatting).
        // Keep base _text in sync with the provider's actual text.
        SetTextDirect (_provider.Text);

        if (!SuppressValueEvents)
        {
            ValueChangedEventArgs<string?> changedArgs = new (oldValue, _provider.Text);
            OnValueChanged (changedArgs);
            ValueChanged?.Invoke (this, changedArgs);
            ValueChangedUntyped?.Invoke (this, new ValueChangedEventArgs<object?> (oldValue, _provider.Text));
        }

        SetNeedsDraw ();

        base.OnTextChanged ();
    }

    private int _insertionPoint;

    private int InsertionPoint
    {
        get => _insertionPoint;
        set
        {
            if (_insertionPoint == value)
            {
                return;
            }

            _insertionPoint = value;

            UpdateCursor ();
        }
    }

    /// <summary>
    ///     Called when the provider's text changes through user input (InsertAt/Delete).
    ///     The base implementation raises <see cref="ValueChanging"/> and <see cref="ValueChanged"/> events
    ///     following the Cancellable Work Pattern.
    /// </summary>
    /// <param name="oldText">The text before the change.</param>
    /// <param name="newText">The text after the change.</param>
    protected virtual void HandleProviderTextChanged (string oldText, string newText)
    {
        ValueChangingEventArgs<string?> args = new (oldText, newText);

        if (OnValueChanging (args) || args.Handled)
        {
            RevertProviderText (oldText);

            return;
        }

        ValueChanging?.Invoke (this, args);

        if (args.Handled)
        {
            RevertProviderText (oldText);

            return;
        }

        ValueChangedEventArgs<string?> changedArgs = new (oldText, newText);
        OnValueChanged (changedArgs);
        ValueChanged?.Invoke (this, changedArgs);
        ValueChangedUntyped?.Invoke (this, new ValueChangedEventArgs<object?> (oldText, newText));
    }

    private void ProviderOnTextChanged (object? sender, EventArgs<string> e)
    {
        if (SuppressValueEvents)
        {
            return;
        }

        string currentText = _provider!.Text;

        if (_lastKnownText == currentText)
        {
            return;
        }

        HandleProviderTextChanged (_lastKnownText, currentText);

        // Sync _lastKnownText with actual provider state (may have been reverted by handler)
        _lastKnownText = _provider.Text;

        // Keep base Text in sync with provider
        SetTextDirect (_provider.Text);
    }

    private void RevertProviderText (string oldText)
    {
        SuppressValueEvents = true;
        _provider!.Text = oldText;
        SuppressValueEvents = false;
        SetNeedsDraw ();
    }

    /// <inheritdoc/>
    protected override bool OnMouseEvent (Mouse mouse)
    {
        if (!mouse.Flags.FastHasFlags (MouseFlags.LeftButtonPressed))
        {
            return false;
        }

        int c = _provider!.Cursor (mouse.Position!.Value.X - GetMargins (Viewport.Width).left);

        if (!_provider.Fixed && TextAlignment == Alignment.End && Text.Length > 0)
        {
            c++;
        }

        InsertionPoint = c;
        SetFocus ();
        SetNeedsDraw ();

        return true;
    }

    /// <inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        if (_provider is null)
        {
            Move (0, 0);
            AddStr ("Error: ITextValidateProvider not set!");

            return true;
        }

        VisualRole role = VisualRole.Editable;
        Attribute textColor = IsValid ? GetAttributeForRole (role) : SchemeManager.GetScheme (Schemes.Error).GetAttributeForRole (role);

        (int marginLeft, int marginRight) = GetMargins (Viewport.Width);

        Move (0, 0);

        // Left Margin
        SetAttribute (textColor);

        for (var i = 0; i < marginLeft; i++)
        {
            AddRune ((Rune)' ');
        }

        // Content
        SetAttribute (textColor);

        // Content
        foreach (char t in _provider.DisplayText)
        {
            AddRune ((Rune)t);
        }

        // Right Margin
        SetAttribute (textColor);

        for (var i = 0; i < marginRight; i++)
        {
            AddRune ((Rune)' ');
        }

        if (!HasFocus || _provider.DisplayText.Length <= 0 || InsertionPoint >= _provider.DisplayText.Length)
        {
            return true;
        }

        SetAttribute (textColor with { Style = textColor.Style | TextStyle.Underline });
        Move (InsertionPoint + marginLeft, 0);
        AddRune ((Rune)_provider.DisplayText [InsertionPoint]);

        return true;
    }

    /// <inheritdoc/>
    protected override bool OnKeyDownNotHandled (Key key)
    {
        if (_provider is null)
        {
            return false;
        }

        if (key.IsAlt || key.IsCtrl)
        {
            // Never insert modified keys
            return false;
        }

        if (key.AsRune == default (Rune) || key == Application.GetDefaultKey (Command.Quit))
        {
            return false;
        }

        Rune rune = key.AsRune;

        bool inserted = _provider.InsertAt ((char)rune.Value, InsertionPoint);

        if (!inserted)
        {
            return false;
        }

        CursorRight ();

        return true;
    }

    /// <summary>Delete char at cursor position - 1, moving the cursor.</summary>
    /// <returns></returns>
    private bool BackspaceKeyHandler ()
    {
        if (!_provider!.Fixed && TextAlignment == Alignment.End && InsertionPoint <= 1)
        {
            return false;
        }

        _insertionPoint = _provider.CursorLeft (InsertionPoint);
        _provider.Delete (InsertionPoint);

        SetNeedsDraw ();
        UpdateCursor ();

        return true;
    }

    /// <summary>Try to move the cursor to the left.</summary>
    /// <returns>True if moved.</returns>
    private bool CursorLeft ()
    {
        if (_provider is null)
        {
            return false;
        }

        int current = _insertionPoint;
        InsertionPoint = _provider.CursorLeft (InsertionPoint);
        SetNeedsDraw ();

        return current != InsertionPoint;
    }

    /// <summary>Try to move the cursor to the right.</summary>
    /// <returns>True if moved.</returns>
    private bool CursorRight ()
    {
        if (_provider is null)
        {
            return false;
        }

        int current = InsertionPoint;

        if (_provider.Fixed && current > _provider.CursorEnd ())
        {
            // Already in the blank cell past the last editable position. Don't move.
            return false;
        }

        InsertionPoint = _provider.CursorRight (InsertionPoint);

        if (current == InsertionPoint && _provider.Fixed && current == _provider.CursorEnd ())
        {
            // Allow cursor to move one past the last editable position (blank cell for cursor).
            InsertionPoint = current + 1;
        }

        SetNeedsDraw ();

        return current != InsertionPoint;
    }

    /// <summary>Deletes char at current position.</summary>
    /// <returns></returns>
    private bool DeleteKeyHandler ()
    {
        if (!_provider!.Fixed && TextAlignment == Alignment.End)
        {
            InsertionPoint = _provider.CursorLeft (InsertionPoint);
        }

        _provider.Delete (InsertionPoint);
        SetNeedsDraw ();

        return true;
    }

    /// <summary>Moves the cursor to the last char.</summary>
    /// <returns></returns>
    private bool EndKeyHandler ()
    {
        InsertionPoint = _provider!.CursorEnd ();
        SetNeedsDraw ();

        return true;
    }

    /// <summary>Margins for text alignment.</summary>
    /// <param name="width">Total width</param>
    /// <returns>Left and right margins</returns>
    private (int left, int right) GetMargins (int width)
    {
        int count = Text.Length;
        int total = width - count;

        return TextAlignment switch
        {
            Alignment.Start => (0, total),
            Alignment.Center => (total / 2, total / 2 + total % 2),
            Alignment.End => (total, 0),
            _ => (0, total)
        };
    }

    /// <summary>Moves the cursor to first char.</summary>
    /// <returns></returns>
    private bool HomeKeyHandler ()
    {
        InsertionPoint = _provider!.CursorStart ();
        SetNeedsDraw ();

        return true;
    }

    /// <inheritdoc/>
    public bool EnableForDesign ()
    {
        TextRegexProvider provider = new ("^([0-9]?[0-9]?[0-9]|1000)$") { ValidateOnInput = false };

        BorderStyle = LineStyle.Single;
        Title = provider.Pattern;
        Provider = provider;

        Text = "999";

        return true;
    }
}
