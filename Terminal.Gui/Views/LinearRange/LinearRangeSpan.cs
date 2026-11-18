namespace Terminal.Gui.Views;

/// <summary>
///     Represents the value of a <see cref="LinearRange{T}"/>.
/// </summary>
/// <typeparam name="T">The data type of the underlying option.</typeparam>
/// <remarks>
///     <para>
///         A span is one of four kinds:
///         <see cref="LinearRangeSpanKind.None"/>,
///         <see cref="LinearRangeSpanKind.LeftBounded"/>,
///         <see cref="LinearRangeSpanKind.RightBounded"/>,
///         or <see cref="LinearRangeSpanKind.Closed"/>.
///     </para>
///     <para>
///         To create an empty span, use <see cref="Empty"/>.
///         To create a closed span between two bounds, use the corresponding constructor and pass the
///         option indices and data values for both ends.
///     </para>
///     <para>
///         <c>StartIndex</c> and <c>EndIndex</c> are option indices into <see cref="LinearRangeViewBase{TOption,TValue}.Options"/>;
///         they are <c>-1</c> when not relevant for the current <see cref="Kind"/>.
///     </para>
/// </remarks>
public readonly record struct LinearRangeSpan<T>
{
    /// <summary>Initializes a new instance of <see cref="LinearRangeSpan{T}"/>.</summary>
    /// <param name="kind">The kind of span.</param>
    /// <param name="start">The start data value (meaningful when <paramref name="kind"/> is <see cref="LinearRangeSpanKind.RightBounded"/> or <see cref="LinearRangeSpanKind.Closed"/>).</param>
    /// <param name="end">The end data value (meaningful when <paramref name="kind"/> is <see cref="LinearRangeSpanKind.LeftBounded"/> or <see cref="LinearRangeSpanKind.Closed"/>).</param>
    /// <param name="startIndex">The index of <paramref name="start"/> in the options list, or <c>-1</c>.</param>
    /// <param name="endIndex">The index of <paramref name="end"/> in the options list, or <c>-1</c>.</param>
    public LinearRangeSpan (LinearRangeSpanKind kind, T? start, T? end, int startIndex, int endIndex)
    {
        Kind = kind;
        Start = start;
        End = end;
        StartIndex = startIndex;
        EndIndex = endIndex;
    }

    /// <summary>Gets an empty span (<see cref="Kind"/> = <see cref="LinearRangeSpanKind.None"/>).</summary>
    public static LinearRangeSpan<T> Empty { get; } = new (LinearRangeSpanKind.None, default, default, -1, -1);

    /// <summary>Gets the kind of span.</summary>
    public LinearRangeSpanKind Kind { get; }

    /// <summary>Gets the start data value (meaningful when <see cref="Kind"/> is <see cref="LinearRangeSpanKind.RightBounded"/> or <see cref="LinearRangeSpanKind.Closed"/>).</summary>
    public T? Start { get; }

    /// <summary>Gets the end data value (meaningful when <see cref="Kind"/> is <see cref="LinearRangeSpanKind.LeftBounded"/> or <see cref="LinearRangeSpanKind.Closed"/>).</summary>
    public T? End { get; }

    /// <summary>Gets the index of <see cref="Start"/> in the options list, or <c>-1</c>.</summary>
    public int StartIndex { get; }

    /// <summary>Gets the index of <see cref="End"/> in the options list, or <c>-1</c>.</summary>
    public int EndIndex { get; }
}
