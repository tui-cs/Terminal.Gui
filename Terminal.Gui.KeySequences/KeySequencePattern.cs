using Terminal.Gui.Input;

namespace Terminal.Gui.KeySequences;

/// <summary>Describes a leader-key sequence pattern.</summary>
public sealed class KeySequencePattern
{
    private readonly List<KeySequenceToken> _tokens = [];

    private KeySequencePattern (Key? leaderKey)
    {
        LeaderKey = leaderKey;
    }

    /// <summary>Gets the leader key that starts this sequence.</summary>
    public Key? LeaderKey { get; }

    /// <summary>Gets the tokens entered after the leader key.</summary>
    public IReadOnlyList<KeySequenceToken> Tokens => _tokens;

    /// <summary>Gets or sets the match mode for this pattern.</summary>
    public KeySequenceMatchMode MatchMode { get; set; }

    /// <summary>Gets or sets whether a parsed count of zero is allowed.</summary>
    public bool AllowZeroCount { get; set; }

    /// <summary>Creates a pattern with the specified leader key.</summary>
    public static KeySequencePattern Leader (Key leaderKey) => new (leaderKey);

    /// <summary>Creates a pattern that matches in persistent command mode.</summary>
    public static KeySequencePattern CommandMode () => new (null);

    /// <summary>Adds a literal key token.</summary>
    public KeySequencePattern Then (Key key)
    {
        _tokens.Add (KeySequenceToken.Literal (key));
        return this;
    }

    /// <summary>Adds a count token.</summary>
    public KeySequencePattern Count (string name = "count")
    {
        _tokens.Add (KeySequenceToken.Count (name));
        return this;
    }

    /// <summary>Adds a printable character token.</summary>
    public KeySequencePattern Char (string name = "char")
    {
        _tokens.Add (KeySequenceToken.Char (name));
        return this;
    }

    /// <summary>Adds an arbitrary key token.</summary>
    public KeySequencePattern AnyKey (string name = "key")
    {
        _tokens.Add (KeySequenceToken.AnyKey (name));
        return this;
    }

    /// <inheritdoc/>
    public override string ToString () => LeaderKey is { } leaderKey
                                              ? $"{leaderKey} {string.Join (" ", _tokens)}".TrimEnd ()
                                              : string.Join (" ", _tokens);
}
