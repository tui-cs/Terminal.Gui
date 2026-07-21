namespace Terminal.Gui.KeySequences;

internal sealed record CandidateMatch
{
    public CandidateMatch (CandidateMatchKind kind, int count, IReadOnlyDictionary<string, object?> values)
    {
        Kind = kind;
        Count = count;
        Values = values;
    }

    public CandidateMatchKind Kind { get; init; }

    public int Count { get; init; }

    public IReadOnlyDictionary<string, object?> Values { get; init; }

    public KeySequenceBinding? Binding { get; init; }

    public static CandidateMatch NoMatch => new (CandidateMatchKind.NoMatch, 1, new Dictionary<string, object?> ());

    public static CandidateMatch Prefix (int count, IReadOnlyDictionary<string, object?> values) => new (CandidateMatchKind.Prefix, count, values);

    public static CandidateMatch Complete (int count, IReadOnlyDictionary<string, object?> values) => new (CandidateMatchKind.Complete, count, values);
}
