namespace Terminal.Gui.Views;

/// <summary>
///     A linear range view that allows selection of zero or more options from a typed list.
/// </summary>
/// <typeparam name="T">The data type of the options.</typeparam>
/// <remarks>
///     <para>
///         Exposes the current selection through <see cref="Value"/> as
///         <see cref="IReadOnlyList{T}"/>; the list is empty when no options are selected.
///         A defensive copy is taken when <see cref="Value"/> is set, so the caller may mutate
///         the list passed in without affecting subsequent reads.
///     </para>
///     <para>
///         Equality between the current value and a new value uses
///         <see cref="System.Linq.Enumerable.SequenceEqual{T}(IEnumerable{T},IEnumerable{T})"/>,
///         so two distinct list instances with the same elements in the same order are considered equal.
///     </para>
/// </remarks>
public class LinearMultiSelector<T> : LinearRangeViewBase<T, IReadOnlyList<T>>, IDesignable
{
    private static readonly IReadOnlyList<T> _emptyList = new List<T> (0).AsReadOnly ();

    private IReadOnlyList<T> _value = _emptyList;

    /// <summary>Initializes a new instance of <see cref="LinearMultiSelector{T}"/>.</summary>
    public LinearMultiSelector () : base (LinearRangeRenderMode.Multiple) { }

    /// <summary>Initializes a new instance of <see cref="LinearMultiSelector{T}"/>.</summary>
    /// <param name="options">Initial options.</param>
    /// <param name="orientation">Initial orientation.</param>
    public LinearMultiSelector (List<T>? options, Orientation orientation = Orientation.Horizontal)
        : base (options, orientation, LinearRangeRenderMode.Multiple) { }

    /// <inheritdoc/>
    /// <remarks>
    ///     The setter accepts <see langword="null"/> as a synonym for an empty list. The getter never
    ///     returns <see langword="null"/>.
    /// </remarks>
    public override IReadOnlyList<T>? Value
    {
        get => _value;
        set
        {
            IReadOnlyList<T> incoming = value is null ? _emptyList : new List<T> (value).AsReadOnly ();
            IReadOnlyList<T> current = _value;

            if (SequenceEqualByDefault (current, incoming))
            {
                return;
            }

            if (RaiseValueChanging (current, incoming))
            {
                return;
            }

            _value = incoming;

            // Sync indices: find the option index for each element of incoming.
            // Use a HashSet to dedupe in O(1) per item rather than O(n) List.Contains scans.
            List<int> indices = new (incoming.Count);
            HashSet<int> seen = new (incoming.Count);

            foreach (T item in incoming)
            {
                int idx = IndexOfData (item);

                if (idx >= 0 && seen.Add (idx))
                {
                    indices.Add (idx);
                }
            }

            ApplySelectedIndices (indices);

            RaiseValueChanged (current, _value);
        }
    }

    /// <inheritdoc/>
    protected override void OnSelectionChanged ()
    {
        IReadOnlyList<T> previous = _value;

        // Build the new value from current indices in the order they appear in Options
        // (rather than the order they were selected) for stable, predictable output.
        IReadOnlyList<int> indices = SelectedIndices;
        List<T> next = new (indices.Count);
        List<int> ordered = new (indices);
        ordered.Sort ();

        foreach (int i in ordered)
        {
            if (i >= 0 && i < Options.Count)
            {
                next.Add (Options [i].Data!);
            }
        }

        IReadOnlyList<T> newValue = next.AsReadOnly ();

        if (SequenceEqualByDefault (previous, newValue))
        {
            return;
        }

        _value = newValue;
        RaiseValueChanged (previous, newValue);
    }

    private static bool SequenceEqualByDefault (IReadOnlyList<T> a, IReadOnlyList<T> b)
    {
        if (ReferenceEquals (a, b))
        {
            return true;
        }

        if (a.Count != b.Count)
        {
            return false;
        }

        EqualityComparer<T> cmp = EqualityComparer<T>.Default;

        for (var i = 0; i < a.Count; i++)
        {
            if (!cmp.Equals (a [i], b [i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     Loads demo data suitable for a designer preview: a multi-select
    ///     <see cref="LinearMultiSelector{T}"/> of the seven days of the week, with the five weekdays
    ///     (Mon–Fri) preselected. Only populated when <typeparamref name="T"/> is <see cref="string"/>;
    ///     for any other type, the view is left untouched and <see langword="false"/> is returned.
    /// </summary>
    /// <returns><see langword="true"/> if demo data was loaded.</returns>
    public virtual bool EnableForDesign ()
    {
        if (typeof (T) != typeof (string))
        {
            return false;
        }

        Title = "Active Days";
        AssignHotKeys = true;
        ShowLegends = true;

        string [] days = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];

        Options = days.Select (
                               d => new LinearRangeOption<T> (d, (Rune)d [0], (T)(object)d))
                      .ToList ();

        Value = days.Take (5).Select (d => (T)(object)d).ToList ();

        return true;
    }
}
