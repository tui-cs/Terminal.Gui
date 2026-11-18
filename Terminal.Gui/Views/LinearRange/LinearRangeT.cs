namespace Terminal.Gui.Views;

/// <summary>
///     A linear range view representing a contiguous range of options. The current value is a
///     <see cref="LinearRangeSpan{T}"/> whose <see cref="LinearRangeSpan{T}.Kind"/> is one of
///     <see cref="LinearRangeSpanKind.None"/>, <see cref="LinearRangeSpanKind.LeftBounded"/>,
///     <see cref="LinearRangeSpanKind.RightBounded"/>, or <see cref="LinearRangeSpanKind.Closed"/>.
/// </summary>
/// <typeparam name="T">The data type of the options.</typeparam>
/// <remarks>
///     <img src="../images/views/LinearRange.gif" alt="LinearRange demo"/>
///     <para>
///         To switch between left-bounded, right-bounded, and closed range modes, set
///         <see cref="RangeKind"/>. Setting <see cref="RangeKind"/> migrates the current
///         <see cref="Value"/>, dropping fields that are no longer relevant.
///     </para>
///     <para>
///         To change the selection programmatically, set <see cref="Value"/>. Empty selections may be
///         represented either by <see cref="LinearRangeSpan{T}.Empty"/> or by a span of any
///         <see cref="LinearRangeSpanKind"/> with no matching options.
///     </para>
/// </remarks>
public class LinearRange<T> : LinearRangeViewBase<T, LinearRangeSpan<T>>, IDesignable
{
    private LinearRangeSpan<T> _value = LinearRangeSpan<T>.Empty;
    private LinearRangeSpanKind _rangeKind = LinearRangeSpanKind.Closed;

    /// <summary>Initializes a new instance of <see cref="LinearRange{T}"/>.</summary>
    public LinearRange () : base (LinearRangeRenderMode.Span) { }

    /// <summary>Initializes a new instance of <see cref="LinearRange{T}"/>.</summary>
    /// <param name="options">Initial options.</param>
    /// <param name="orientation">Initial orientation.</param>
    public LinearRange (List<T>? options, Orientation orientation = Orientation.Horizontal)
        : base (options, orientation, LinearRangeRenderMode.Span) { }

    /// <summary>
    ///     Gets or sets whether the range is allowed to collapse to a single option (only meaningful
    ///     when <see cref="RangeKind"/> is <see cref="LinearRangeSpanKind.Closed"/>).
    /// </summary>
    public bool RangeAllowSingle
    {
        get => RangeAllowSingleInternal;
        set => RangeAllowSingleInternal = value;
    }

    /// <summary>
    ///     Gets or sets the kind of range. The default is <see cref="LinearRangeSpanKind.Closed"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Setting this property re-renders the view in the new shape and migrates the current
    ///         <see cref="Value"/>: e.g. switching from <see cref="LinearRangeSpanKind.Closed"/> to
    ///         <see cref="LinearRangeSpanKind.LeftBounded"/> drops the <see cref="LinearRangeSpan{T}.Start"/>
    ///         and <see cref="LinearRangeSpan{T}.StartIndex"/>; switching to
    ///         <see cref="LinearRangeSpanKind.RightBounded"/> drops the <see cref="LinearRangeSpan{T}.End"/>
    ///         and <see cref="LinearRangeSpan{T}.EndIndex"/>; switching to
    ///         <see cref="LinearRangeSpanKind.None"/> clears the value.
    ///     </para>
    /// </remarks>
    public LinearRangeSpanKind RangeKind
    {
        get => _rangeKind;
        set
        {
            if (_rangeKind == value)
            {
                return;
            }

            _rangeKind = value;

            // Update internal render mode to match.
            RenderMode = value switch
            {
                LinearRangeSpanKind.LeftBounded => LinearRangeRenderMode.LeftSpan,
                LinearRangeSpanKind.RightBounded => LinearRangeRenderMode.RightSpan,
                LinearRangeSpanKind.Closed => LinearRangeRenderMode.Span,
                _ => LinearRangeRenderMode.Span
            };

            // Migrate current Value to the new kind.
            LinearRangeSpan<T> migrated = MigrateValueToKind (_value, value);

            if (!_value.Equals (migrated))
            {
                LinearRangeSpan<T> previous = _value;
                _value = migrated;

                // Sync indices to reflect the migrated value.
                ApplySelectedIndices (IndicesForValue (migrated));
                RaiseValueChanged (previous, migrated);
            }
        }
    }

    /// <inheritdoc/>
    public override LinearRangeSpan<T> Value
    {
        get => _value;
        set
        {
            LinearRangeSpan<T> current = _value;

            if (current.Equals (value))
            {
                return;
            }

            if (RaiseValueChanging (current, value))
            {
                return;
            }

            _value = value;

            ApplySelectedIndices (IndicesForValue (value));
            RaiseValueChanged (current, _value);
        }
    }

    /// <inheritdoc/>
    protected override void OnSelectionChanged ()
    {
        LinearRangeSpan<T> previous = _value;
        LinearRangeSpan<T> next = SpanFromIndices (SelectedIndices);

        if (previous.Equals (next))
        {
            return;
        }

        _value = next;
        RaiseValueChanged (previous, next);
    }

    private LinearRangeSpan<T> SpanFromIndices (IReadOnlyList<int> indices)
    {
        if (indices.Count == 0)
        {
            return new LinearRangeSpan<T> (_rangeKind == LinearRangeSpanKind.None ? LinearRangeSpanKind.None : _rangeKind,
                                           default,
                                           default,
                                           -1,
                                           -1);
        }

        // Sort to get logical [low, high]
        List<int> sorted = new (indices);
        sorted.Sort ();
        int lo = sorted [0];
        int hi = sorted [^1];

        switch (_rangeKind)
        {
            case LinearRangeSpanKind.LeftBounded:
                // Only the end is bounded
                return new LinearRangeSpan<T> (LinearRangeSpanKind.LeftBounded,
                                               default,
                                               Options [hi].Data,
                                               -1,
                                               hi);
            case LinearRangeSpanKind.RightBounded:
                // Only the start is bounded
                return new LinearRangeSpan<T> (LinearRangeSpanKind.RightBounded,
                                               Options [lo].Data,
                                               default,
                                               lo,
                                               -1);
            case LinearRangeSpanKind.Closed:
            default:
                // Closed (or fallback): both bounds
                if (sorted.Count == 1)
                {
                    return new LinearRangeSpan<T> (LinearRangeSpanKind.Closed,
                                                   Options [lo].Data,
                                                   Options [lo].Data,
                                                   lo,
                                                   lo);
                }

                return new LinearRangeSpan<T> (LinearRangeSpanKind.Closed,
                                               Options [lo].Data,
                                               Options [hi].Data,
                                               lo,
                                               hi);
        }
    }

    private List<int> IndicesForValue (LinearRangeSpan<T> span)
    {
        switch (span.Kind)
        {
            case LinearRangeSpanKind.None:
                return [];
            case LinearRangeSpanKind.LeftBounded:
                {
                    int end = span.EndIndex >= 0 ? span.EndIndex : IndexOfData (span.End);

                    return end >= 0 ? [end] : [];
                }
            case LinearRangeSpanKind.RightBounded:
                {
                    int start = span.StartIndex >= 0 ? span.StartIndex : IndexOfData (span.Start);

                    return start >= 0 ? [start] : [];
                }
            case LinearRangeSpanKind.Closed:
            default:
                {
                    int start = span.StartIndex >= 0 ? span.StartIndex : IndexOfData (span.Start);
                    int end = span.EndIndex >= 0 ? span.EndIndex : IndexOfData (span.End);

                    if (start < 0 && end < 0)
                    {
                        return [];
                    }

                    if (start < 0)
                    {
                        return [end];
                    }

                    if (end < 0 || start == end)
                    {
                        return [start];
                    }

                    return [start, end];
                }
        }
    }

    private static LinearRangeSpan<T> MigrateValueToKind (LinearRangeSpan<T> value, LinearRangeSpanKind newKind)
    {
        if (newKind == LinearRangeSpanKind.None)
        {
            return LinearRangeSpan<T>.Empty;
        }

        if (value.Kind == LinearRangeSpanKind.None)
        {
            return value;
        }

        return newKind switch
        {
            LinearRangeSpanKind.LeftBounded => new LinearRangeSpan<T> (
                                                                      LinearRangeSpanKind.LeftBounded,
                                                                      default,
                                                                      value.Kind == LinearRangeSpanKind.RightBounded ? value.Start : value.End,
                                                                      -1,
                                                                      value.Kind == LinearRangeSpanKind.RightBounded ? value.StartIndex : value.EndIndex),
            LinearRangeSpanKind.RightBounded => new LinearRangeSpan<T> (
                                                                       LinearRangeSpanKind.RightBounded,
                                                                       value.Kind == LinearRangeSpanKind.LeftBounded ? value.End : value.Start,
                                                                       default,
                                                                       value.Kind == LinearRangeSpanKind.LeftBounded ? value.EndIndex : value.StartIndex,
                                                                       -1),
            LinearRangeSpanKind.Closed => value.Kind == LinearRangeSpanKind.Closed
                                              ? value
                                              : value.Kind == LinearRangeSpanKind.LeftBounded
                                                  ? new LinearRangeSpan<T> (LinearRangeSpanKind.Closed, value.End, value.End, value.EndIndex, value.EndIndex)
                                                  : new LinearRangeSpan<T> (LinearRangeSpanKind.Closed, value.Start, value.Start, value.StartIndex, value.StartIndex),
            _ => value
        };
    }

    /// <summary>
    ///     Loads demo data suitable for a designer preview: a closed range of work hours
    ///     (8 AM through 6 PM in one-hour increments) with the range preset to "9 AM"–"5 PM".
    ///     Only populated when <typeparamref name="T"/> is <see cref="string"/>; for any other type,
    ///     the view is left untouched and <see langword="false"/> is returned.
    /// </summary>
    /// <returns><see langword="true"/> if demo data was loaded.</returns>
    public virtual bool EnableForDesign ()
    {
        if (typeof (T) != typeof (string))
        {
            return false;
        }

        Title = "Work Hours";
        AssignHotKeys = true;
        ShowLegends = true;
        RangeKind = LinearRangeSpanKind.Closed;
        RangeAllowSingle = true;

        string [] hours =
        [
            "8 AM", "9 AM", "10 AM", "11 AM", "12 PM",
            "1 PM", "2 PM", "3 PM", "4 PM", "5 PM", "6 PM"
        ];

        Options = hours.Select (h => new LinearRangeOption<T> (h, (Rune)h [0], (T)(object)h)).ToList ();

        const int startIdx = 1; // "9 AM"
        const int endIdx = 9;   // "5 PM"

        Value = new LinearRangeSpan<T> (
                                        LinearRangeSpanKind.Closed,
                                        (T)(object)hours [startIdx],
                                        (T)(object)hours [endIdx],
                                        startIdx,
                                        endIdx);

        return true;
    }
}
