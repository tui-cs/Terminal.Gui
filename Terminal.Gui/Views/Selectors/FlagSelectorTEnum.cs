namespace Terminal.Gui.Views;

/// <summary>
///     Provides a user interface for displaying and selecting non-mutually-exclusive flags in a type-safe way.
///     <see cref="FlagSelector"/> provides a non-type-safe version. <c>TFlagsEnum</c> must be a valid enum type with
///     the '[Flags]' attribute.
/// </summary>
public sealed class FlagSelector<TFlagsEnum> : FlagSelector, IValue where TFlagsEnum : struct, Enum
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="FlagSelector{TFlagsEnum}"/> class.
    /// </summary>
    public FlagSelector () => SetValuesAndLabels<TFlagsEnum> ();

    /// <summary>
    ///     Gets or sets the value of the selected flags.
    /// </summary>
    public new TFlagsEnum? Value
    {
        get => base.Value.HasValue ? (TFlagsEnum)Enum.ToObject (typeof (TFlagsEnum), base.Value.Value) : null;
        set => base.Value = value.HasValue ? Convert.ToInt32 (value.Value) : null;
    }

    /// <summary>
    ///     Raised when <see cref="Value"/> has changed. Provides the new value as <typeparamref name="TFlagsEnum"/>?.
    /// </summary>
    public new event EventHandler<EventArgs<TFlagsEnum?>>? ValueChanged;

    /// <summary>
    ///     Called when <see cref="Value"/> has changed. Raises the generic <see cref="ValueChanged"/> event.
    /// </summary>
    protected override void OnValueChanged (int? value, int? previousValue)
    {
        base.OnValueChanged (value, previousValue);

        TFlagsEnum? newValue = value.HasValue ? (TFlagsEnum)Enum.ToObject (typeof (TFlagsEnum), value.Value) : null;

        ValueChanged?.Invoke (this, new EventArgs<TFlagsEnum?> (newValue));
    }

    /// <inheritdoc/>
    object? IValue.GetValue () => Value;
}
