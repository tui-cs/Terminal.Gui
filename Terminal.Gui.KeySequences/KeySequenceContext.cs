using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;

namespace Terminal.Gui.KeySequences;

/// <summary>Provides context for a completed key sequence.</summary>
public sealed record KeySequenceContext
{
    /// <summary>Gets the view that received the sequence.</summary>
    public required View Target { get; init; }

    /// <summary>Gets the leader key that started capture.</summary>
    public Key? LeaderKey { get; init; }

    /// <summary>Gets whether the sequence matched in persistent command mode.</summary>
    public bool IsCommandMode { get; init; }

    /// <summary>Gets the keys entered after the leader key.</summary>
    public required IReadOnlyList<Key> Keys { get; init; }

    /// <summary>Gets the pattern that matched the sequence.</summary>
    public required KeySequencePattern Pattern { get; init; }

    /// <summary>Gets the parsed count, or <c>1</c> when the sequence did not include a count.</summary>
    public int Count { get; init; } = 1;

    /// <summary>Gets the first literal key in the pattern, if any.</summary>
    public Key? OperatorKey { get; init; }

    /// <summary>Gets the last literal key in the pattern, if any.</summary>
    public Key? MotionKey { get; init; }

    /// <summary>Gets named values parsed from sequence tokens.</summary>
    public IReadOnlyDictionary<string, object?> Values { get; init; } = new Dictionary<string, object?> ();

    /// <summary>Gets the command context associated with the sequence, if one exists.</summary>
    public CommandContext? CommandContext { get; init; }
}
