namespace Terminal.Gui.Views;

/// <summary>
///     Provides a user interface for displaying and selecting a single item from a list of options in a type-safe way.
///     Each option is represented by a checkbox, but only one can be selected at a time.
///     <see cref="OptionSelector"/> provides a non-type-safe version.
/// </summary>
public sealed class OptionSelector<TEnum> : OptionSelector, IValue where TEnum : struct, Enum
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="OptionSelector{TEnum}"/> class.
    /// </summary>
    public OptionSelector ()
    {
        // Reset Value so that the Values setter can auto-set it to the first actual enum value.
        // The base OptionSelector constructor sets Value = 0, which may not be a valid enum member.
        base.Value = null;

        // Set actual enum int values (bypass the throwing override via base).
        base.Values = Enum.GetValues<TEnum> ().Select (f => Convert.ToInt32 (f)).ToList ().AsReadOnly ();

        // Set labels, which triggers CreateSubViews + UpdateChecked.
        Labels = Enum.GetNames<TEnum> ();
    }

    /// <summary>
    ///     Gets or sets the value of the selected option.
    /// </summary>
    public new TEnum? Value
    {
        get => base.Value.HasValue ? (TEnum)Enum.ToObject (typeof (TEnum), base.Value.Value) : null;
        set => base.Value = value.HasValue ? Convert.ToInt32 (value.Value) : null;
    }

    /// <summary>
    ///     Prevents calling the base Values property setter with arbitrary values.
    /// </summary>
    public override IReadOnlyList<int>? Values { get => base.Values; set => throw new InvalidOperationException ("Setting Values directly is not allowed."); }

    /// <summary>
    ///     Raised when <see cref="Value"/> has changed. Provides the new value as <typeparamref name="TEnum"/>?.
    /// </summary>
    public new event EventHandler<EventArgs<TEnum?>>? ValueChanged;

    /// <summary>
    ///     Called when <see cref="Value"/> has changed. Raises the generic <see cref="ValueChanged"/> event.
    /// </summary>
    protected override void OnValueChanged (int? value, int? previousValue)
    {
        base.OnValueChanged (value, previousValue);

        TEnum? newValue = value.HasValue ? (TEnum)Enum.ToObject (typeof (TEnum), value.Value) : null;

        ValueChanged?.Invoke (this, new EventArgs<TEnum?> (newValue));
    }

    /// <inheritdoc/>
    object? IValue.GetValue () => Value;
}
