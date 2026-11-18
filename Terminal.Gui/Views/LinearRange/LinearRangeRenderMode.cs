namespace Terminal.Gui.Views;

/// <summary>
///     Selection rendering mode used by <see cref="LinearRangeViewBase{TOption,TValue}"/> to
///     drive selection drawing and hit-testing. Each concrete subclass sets this once
///     in its constructor (or, for <see cref="LinearRange{T}"/>, whenever
///     <see cref="LinearRange{T}.RangeKind"/> changes).
/// </summary>
/// <remarks>
///     This enum is exposed publicly only because it appears in the protected constructor
///     signature of <see cref="LinearRangeViewBase{TOption,TValue}"/>; library consumers should
///     pick a concrete subclass rather than instantiate the base directly.
/// </remarks>
public enum LinearRangeRenderMode
{
    /// <summary>One option may be selected at a time.</summary>
    Single,

    /// <summary>Any number of options may be selected at the same time.</summary>
    Multiple,

    /// <summary>A range bounded only by an end point: "everything ≤ End".</summary>
    LeftSpan,

    /// <summary>A range bounded only by a start point: "everything ≥ Start".</summary>
    RightSpan,

    /// <summary>A range bounded by both a start and an end point.</summary>
    Span
}
