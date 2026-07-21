using Terminal.Gui.Input;

namespace Terminal.Gui.KeySequences;

/// <summary>Provides data for <see cref="KeySequenceBindings.StateChanged"/>.</summary>
public sealed class KeySequenceStateChangedEventArgs : EventArgs
{
    /// <summary>Initializes a new instance.</summary>
    public KeySequenceStateChangedEventArgs (
        KeySequenceState state,
        Key? leaderKey,
        IReadOnlyList<Key> keys,
        string countText,
        int candidateCount,
        KeySequenceResult result,
        bool isCommandMode = false)
    {
        State = state;
        LeaderKey = leaderKey;
        Keys = keys;
        CountText = countText;
        CandidateCount = candidateCount;
        Result = result;
        IsCommandMode = isCommandMode;
    }

    /// <summary>Gets the current capture state.</summary>
    public KeySequenceState State { get; }

    /// <summary>Gets the leader key that started capture.</summary>
    public Key? LeaderKey { get; }

    /// <summary>Gets the keys entered after the leader key.</summary>
    public IReadOnlyList<Key> Keys { get; }

    /// <summary>Gets the current count text.</summary>
    public string CountText { get; }

    /// <summary>Gets the number of candidate patterns.</summary>
    public int CandidateCount { get; }

    /// <summary>Gets the processing result that caused the state change.</summary>
    public KeySequenceResult Result { get; }

    /// <summary>Gets whether persistent command mode is active.</summary>
    public bool IsCommandMode { get; }
}
