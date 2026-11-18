namespace Terminal.Gui.Views;

/// <summary>
///     A linear range view that allows selection of a single option from a typed list of options.
/// </summary>
/// <typeparam name="T">The data type of the options.</typeparam>
/// <remarks>
///     <para>
///         Exposes the current selection through <see cref="Value"/>. When <typeparamref name="T"/> is a
///         reference type, <see langword="null"/> unambiguously represents "no selection". When
///         <typeparamref name="T"/> is a value type, <see cref="Value"/> is <c>default(T)</c> when no
///         option is selected — which can be indistinguishable from a legitimately selected default value
///         (e.g. <c>0</c> for <see cref="int"/>). To test for empty selection unambiguously for both
///         reference and value types, use <see cref="SelectedIndex"/>, which is <see langword="null"/>
///         only when nothing is selected.
///     </para>
///     <para>
///         To switch the current selection programmatically, set <see cref="Value"/> or
///         <see cref="SelectedIndex"/>. To observe selection changes, subscribe to
///         <see cref="LinearRangeViewBase{TOption,TValue}.ValueChanged"/>.
///     </para>
/// </remarks>
public class LinearSelector<T> : LinearRangeViewBase<T, T>, IDesignable
{
    private T? _value;

    /// <summary>Initializes a new instance of <see cref="LinearSelector{T}"/>.</summary>
    public LinearSelector () : base (LinearRangeRenderMode.Single) { }

    /// <summary>Initializes a new instance of <see cref="LinearSelector{T}"/>.</summary>
    /// <param name="options">Initial options.</param>
    /// <param name="orientation">Initial orientation.</param>
    public LinearSelector (List<T>? options, Orientation orientation = Orientation.Horizontal)
        : base (options, orientation, LinearRangeRenderMode.Single) { }

    /// <inheritdoc/>
    public override T? Value
    {
        get => _value;
        set
        {
            T? current = _value;

            if (EqualityComparer<T?>.Default.Equals (current, value))
            {
                return;
            }

            if (RaiseValueChanging (current, value))
            {
                return;
            }

            _value = value;

            // Sync indices to match value.
            if (value is null)
            {
                ApplySelectedIndices ([]);
            }
            else
            {
                int idx = IndexOfData (value);

                if (idx >= 0)
                {
                    ApplySelectedIndices ([idx]);
                }
                else
                {
                    // Value not present among options: clear selection but keep field.
                    ApplySelectedIndices ([]);
                }
            }

            RaiseValueChanged (current, _value);
        }
    }

    /// <summary>
    ///     Gets or sets the index of the currently selected option, or <see langword="null"/> if no option is
    ///     selected. This is the unambiguous "no selection" surface for both reference and value types
    ///     (compare with <see cref="Value"/>, where <c>default(T)</c> for value types may collide with a
    ///     legitimately selected option).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         To clear the selection, set to <see langword="null"/>. Requires
    ///         <see cref="LinearRangeViewBase{TOption,TValue}.AllowEmpty"/> to be <see langword="true"/>;
    ///         otherwise the clear is silently ignored (mirrors how <see cref="Value"/> behaves).
    ///     </para>
    ///     <para>
    ///         To select an option, set to its index in <see cref="LinearRangeViewBase{TOption,TValue}.Options"/>.
    ///         Out-of-range values throw <see cref="ArgumentOutOfRangeException"/>.
    ///     </para>
    /// </remarks>
    public int? SelectedIndex
    {
        get => SelectedIndices.Count > 0 ? SelectedIndices [0] : null;
        set
        {
            if (value is null)
            {
                if (!AllowEmpty)
                {
                    return;
                }

                _value = default;
                ApplySelectedIndices ([]);

                return;
            }

            if (Options is null || value < 0 || value >= Options.Count)
            {
                throw new ArgumentOutOfRangeException (nameof (value));
            }

            // Sync indices first so SelectedIndex can select an option whose Data equals the current
            // _value (e.g. selecting option 0 in a value-type selector where _value is already
            // default(int)=0 — Value setter would short-circuit on equality, leaving SelectedIndex null).
            T? newValue = Options [value.Value].Data;
            T? current = _value;
            bool valueChanged = !EqualityComparer<T?>.Default.Equals (current, newValue);

            if (valueChanged && RaiseValueChanging (current, newValue))
            {
                return;
            }

            _value = newValue;
            ApplySelectedIndices ([value.Value]);

            if (valueChanged)
            {
                RaiseValueChanged (current, _value);
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnSelectionChanged ()
    {
        T? previous = _value;
        T? newValue = SelectedIndices.Count > 0 ? Options [SelectedIndices [0]].Data : default;

        if (EqualityComparer<T?>.Default.Equals (previous, newValue))
        {
            return;
        }

        _value = newValue;
        RaiseValueChanged (previous, newValue);
    }

    /// <summary>
    ///     Loads demo data suitable for a designer preview: a single-select <see cref="LinearSelector{T}"/>
    ///     of T-shirt sizes (XS through XXL) with "M" preselected. Only populated when
    ///     <typeparamref name="T"/> is <see cref="string"/>; for any other type, the view is left untouched
    ///     and <see langword="false"/> is returned.
    /// </summary>
    /// <returns><see langword="true"/> if demo data was loaded.</returns>
    public virtual bool EnableForDesign ()
    {
        if (typeof (T) != typeof (string))
        {
            return false;
        }

        Title = "T-Shirt Size";
        AssignHotKeys = true;
        ShowLegends = true;

        string [] sizes = ["XS", "S", "M", "L", "XL", "XXL"];

        Options = sizes.Select (
                                s => new LinearRangeOption<T> (s, (Rune)s [0], (T)(object)s))
                       .ToList ();

        Value = (T)(object)"M";

        return true;
    }
}
