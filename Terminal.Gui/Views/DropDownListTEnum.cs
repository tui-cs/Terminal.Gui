using System.Collections.ObjectModel;

namespace Terminal.Gui.Views;

/// <summary>
///     A type-safe dropdown control for selecting a single value from an enum.
///     Provides the same interface as <see cref="OptionSelector{TEnum}"/> but rendered as a compact dropdown list.
/// </summary>
/// <typeparam name="TEnum">The enum type to display and select from.</typeparam>
/// <remarks>
///     <para>
///         <see cref="DropDownList{TEnum}"/> and <see cref="OptionSelector{TEnum}"/> are interchangeable when the data
///         source is an enum. Both expose a typed <see cref="Value"/> property and a typed
///         <see cref="ValueChanged"/> event. They differ only in appearance: <see cref="DropDownList{TEnum}"/> is a
///         compact single-line dropdown, while <see cref="OptionSelector{TEnum}"/> shows all options as checkboxes.
///     </para>
///     <para>
///         The dropdown source is automatically populated from <typeparamref name="TEnum"/> values in the constructor.
///     </para>
///     <para>
///         <b>Usage Example:</b>
///     </para>
///     <code>
///         var dropdown = new DropDownList&lt;DayOfWeek&gt; ();
///         dropdown.Value = DayOfWeek.Monday;
///         dropdown.ValueChanged += (s, e) => Console.WriteLine ($"Selected: {e.Value}");
///     </code>
/// </remarks>
public sealed class DropDownList<TEnum> : DropDownList, IValue where TEnum : struct, Enum
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DropDownList{TEnum}"/> class.
    ///     Automatically populates the dropdown source from <typeparamref name="TEnum"/> values.
    /// </summary>
    public DropDownList ()
    {
        Source = new ListWrapper<string> (new ObservableCollection<string> (Enum.GetValues<TEnum> ().Select (enumValue => enumValue.ToString ())));

        base.ValueChanged += OnBaseValueChanged;
    }

    private void OnBaseValueChanged (object? sender, ValueChangedEventArgs<string?> e)
    {
        TEnum? newValue = null;

        if (!string.IsNullOrEmpty (e.NewValue) && Enum.TryParse (e.NewValue, out TEnum parsed))
        {
            newValue = parsed;
        }

        ValueChanged?.Invoke (this, new EventArgs<TEnum?> (newValue));
    }

    /// <summary>
    ///     Gets or sets the currently selected enum value.
    /// </summary>
    public new TEnum? Value
    {
        get => !string.IsNullOrEmpty (Text) && Enum.TryParse (Text, out TEnum result) ? result : null;
        set => base.Value = value?.ToString ();
    }

    /// <summary>
    ///     Raised when the selected value changes. Provides the new value as <typeparamref name="TEnum"/>?.
    /// </summary>
    public new event EventHandler<EventArgs<TEnum?>>? ValueChanged;

    /// <inheritdoc/>
    object? IValue.GetValue () => Value;
}
