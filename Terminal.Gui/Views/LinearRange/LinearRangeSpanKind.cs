namespace Terminal.Gui.Views;

/// <summary>
///     Identifies the shape of a <see cref="LinearRangeSpan{T}"/>.
/// </summary>
/// <remarks>
///     <para>
///         To represent the kind of range a <see cref="LinearRange{T}"/> currently holds, set the value via
///         <see cref="LinearRange{T}.RangeKind"/>.
///     </para>
/// </remarks>
public enum LinearRangeSpanKind
{
    /// <summary>The span is empty; no option is selected.</summary>
    None,

    /// <summary>The span is bounded only on the right; conceptually "everything ≤ End".</summary>
    LeftBounded,

    /// <summary>The span is bounded only on the left; conceptually "everything ≥ Start".</summary>
    RightBounded,

    /// <summary>The span is closed; both <c>Start</c> and <c>End</c> are bounded.</summary>
    Closed
}
