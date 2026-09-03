namespace Terminal.Gui.Views;

/// <summary>Shows a checkbox that can be cycled between two or three states.</summary>
/// <remarks>
/// <img src="../images/views/CheckBox.gif" alt="CheckBox demo"/>
///     <para>
///         <see cref="RadioStyle"/> is used to display radio button style glyphs (●) instead of checkbox style glyphs (☑).
///     </para>
///     <para>Default mouse bindings:</para>
///     <list type="table">
///         <listheader>
///             <term>Mouse Event</term> <description>Action</description>
///         </listheader>
///         <item>
///             <term>Click</term> <description>Toggles the checked state (<see cref="Command.Activate"/>).</description>
///         </item>
///         <item>
///             <term>Double-Click</term> <description>Accepts the checkbox (<see cref="Command.Accept"/>).</description>
///         </item>
///     </list>
/// </remarks>
public class CheckBox : View, IValue<CheckState>
{
    /// <summary>
    ///     Gets or sets the default Highlight Style.
    /// </summary>
    public static MouseState DefaultMouseHighlightStates => CheckBoxSettings.Current.DefaultMouseHighlightStates;

    /// <summary>
    ///     Initializes a new instance of <see cref="CheckBox"/>.
    /// </summary>
    public CheckBox ()
    {
        Width = Dim.Auto (DimAutoStyle.Text);
        Height = Dim.Auto (DimAutoStyle.Text, 1);

        CanFocus = true;

        // Activate (Space key and single-click) - Raise Activate event and Advance
        // - DO NOT raise Accept
        // - DO SetFocus (if focus is not desired, set CanFocus to false)

        // Accept (Enter key and double-click) - Raise Accept event
        // - DO NOT advance state

        // Use LeftButtonClicked instead of LeftButtonReleased to prevent double activation on double-click.
        // LeftButtonClicked fires once per click; LeftButtonReleased fires on each release (twice for double-click).
        MouseBindings.Remove (MouseFlags.LeftButtonReleased);
        MouseBindings.Add (MouseFlags.LeftButtonClicked, Command.Activate);
        MouseBindings.Add (MouseFlags.LeftButtonDoubleClicked, Command.Accept);

        TitleChanged += Checkbox_TitleChanged;

        MouseHighlightStates = DefaultMouseHighlightStates;
    }

    /// <inheritdoc/>
    protected override void OnActivated (ICommandContext? commandContext)
    {
        base.OnActivated (commandContext);
        AdvanceCheckState ();
    }

    private void Checkbox_TitleChanged (object? sender, EventArgs<string> e)
    {
        SetTextDirect (e.Value);
        TextFormatter.HotKeySpecifier = HotKeySpecifier;
    }

    /// <inheritdoc/>
    protected override void OnTextChanged ()
    {
        Title = Text;

        base.OnTextChanged ();
    }

    /// <inheritdoc/>
    public override Rune HotKeySpecifier { get => base.HotKeySpecifier; set => TextFormatter.HotKeySpecifier = base.HotKeySpecifier = value; }

    /// <summary>
    ///     If <see langword="true"/> allows <see cref="Value"/> to be <see cref="CheckState.None"/>. The default is
    ///     <see langword="false"/>.
    /// </summary>
    public bool AllowCheckStateNone
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;

            if (Value == CheckState.None)
            {
                Value = CheckState.UnChecked;
            }
        }
    }

    #region IValue<CheckState> Implementation

    private CheckState _value = CheckState.UnChecked;

    /// <summary>
    ///     Gets or sets the state of the <see cref="CheckBox"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If <see cref="AllowCheckStateNone"/> is <see langword="true"/> and <see cref="CheckState.None"/>, the
    ///         <see cref="CheckBox"/>
    ///         will display the <c>Glyphs.CheckStateNone</c> character (⬛).
    ///     </para>
    ///     <para>
    ///         If <see cref="CheckState.UnChecked"/>, the <see cref="CheckBox"/>
    ///         will display the <c>Glyphs.CheckStateUnChecked</c> character (☐).
    ///     </para>
    ///     <para>
    ///         If <see cref="CheckState.Checked"/>, the <see cref="CheckBox"/>
    ///         will display the <c>Glyphs.CheckStateChecked</c> character (☑).
    ///     </para>
    /// </remarks>
    public CheckState Value { get => _value; set => ChangeValue (value); }

    /// <inheritdoc/>
    public event EventHandler<ValueChangingEventArgs<CheckState>>? ValueChanging;

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<CheckState>>? ValueChanged;

    /// <summary>
    ///     Called when the <see cref="CheckBox"/> <see cref="Value"/> is changing.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The value change can be cancelled by returning <see langword="true"/> or setting
    ///         <see cref="ValueChangingEventArgs{T}.Handled"/> to <see langword="true"/>.
    ///     </para>
    /// </remarks>
    /// <param name="args">The event arguments containing old and new values.</param>
    /// <returns><see langword="true"/> to cancel the change; otherwise <see langword="false"/>.</returns>
    protected virtual bool OnValueChanging (ValueChangingEventArgs<CheckState> args) => false;

    /// <summary>
    ///     Called when the <see cref="CheckBox"/> <see cref="Value"/> has changed.
    /// </summary>
    /// <param name="args">The event arguments containing old and new values.</param>
    protected virtual void OnValueChanged (ValueChangedEventArgs<CheckState> args) { }

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<object?>>? ValueChangedUntyped;

    /// <summary>
    ///     INTERNAL Sets Value.
    /// </summary>
    /// <param name="newValue">The new value.</param>
    /// <returns>
    ///     <see langword="true"/> if state change was canceled, <see langword="false"/> if the state changed, and
    ///     <see langword="null"/> if the state was not changed for some other reason.
    /// </returns>
    private bool? ChangeValue (CheckState newValue)
    {
        if (_value == newValue || (newValue is CheckState.None && !AllowCheckStateNone))
        {
            return null;
        }

        CheckState oldValue = _value;

        ValueChangingEventArgs<CheckState> changingArgs = new (oldValue, newValue);

        if (OnValueChanging (changingArgs) || changingArgs.Handled)
        {
            return true;
        }

        ValueChanging?.Invoke (this, changingArgs);

        if (changingArgs.Handled)
        {
            return true;
        }

        _value = newValue;
        UpdateTextFormatterText ();
        SetNeedsLayout ();

        ValueChangedEventArgs<CheckState> changedArgs = new (oldValue, _value);
        OnValueChanged (changedArgs);
        ValueChanged?.Invoke (this, changedArgs);

        ValueChangedUntyped?.Invoke (this, new ValueChangedEventArgs<object?> (oldValue, _value));

        return false;
    }

    #endregion

    /// <summary>
    ///     Advances <see cref="Value"/> to the next value. Invokes the cancelable <see cref="ValueChanging"/>
    ///     event.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Cycles through the states <see cref="CheckState.None"/>, <see cref="CheckState.Checked"/>, and
    ///         <see cref="CheckState.UnChecked"/>.
    ///     </para>
    ///     <para>
    ///         If the <see cref="ValueChanging"/> event is not canceled, the <see cref="Value"/> will be updated
    ///         and the <see cref="Command.Accept"/> event will be raised.
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     <see langword="true"/> if state change was canceled, <see langword="false"/> if the state changed, and
    ///     <see langword="null"/> if the state was not changed for some other reason.
    /// </returns>
    public bool? AdvanceCheckState ()
    {
        CheckState nextValue = Value switch
        {
            CheckState.None => CheckState.Checked,
            CheckState.Checked => CheckState.UnChecked,
            CheckState.UnChecked => AllowCheckStateNone ? CheckState.None : CheckState.Checked,
            _ => CheckState.UnChecked
        };

        return ChangeValue (nextValue);
    }

    /// <inheritdoc/>
    protected override bool OnClearingViewport ()
    {
        SetAttributeForRole (HasFocus ? VisualRole.Focus : VisualRole.Normal);

        return base.OnClearingViewport ();
    }

    /// <inheritdoc/>
    protected override void UpdateTextFormatterText ()
    {
        base.UpdateTextFormatterText ();

        Rune glyph = RadioStyle ? GetRadioGlyph () : GetCheckGlyph ();

        switch (TextAlignment)
        {
            case Alignment.Start:
            case Alignment.Center:
            case Alignment.Fill:
                TextFormatter.Text = $"{glyph} {Text}";

                break;

            case Alignment.End:
                TextFormatter.Text = $"{Text} {glyph}";

                break;
        }
    }

    private Rune GetCheckGlyph () =>
        Value switch
        {
            CheckState.Checked => Glyphs.CheckStateChecked,
            CheckState.UnChecked => Glyphs.CheckStateUnChecked,
            CheckState.None => Glyphs.CheckStateNone,
            _ => throw new ArgumentOutOfRangeException ()
        };

    /// <summary>
    ///     If <see langword="true"/>, the <see cref="CheckBox"/> will display radio button style glyphs (●) instead of
    ///     checkbox style glyphs (☑).
    /// </summary>
    public bool RadioStyle { get; set; }

    private Rune GetRadioGlyph () =>
        Value switch
        {
            CheckState.Checked => Glyphs.Selected,
            CheckState.UnChecked => Glyphs.UnSelected,
            CheckState.None => Glyphs.Dot,
            _ => throw new ArgumentOutOfRangeException ()
        };
}
