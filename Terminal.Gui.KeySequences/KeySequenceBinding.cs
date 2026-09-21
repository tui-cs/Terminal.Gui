namespace Terminal.Gui.KeySequences;

/// <summary>Maps a <see cref="KeySequencePattern"/> to a handler.</summary>
public sealed class KeySequenceBinding
{
    /// <summary>Initializes a new instance.</summary>
    public KeySequenceBinding (KeySequencePattern pattern, KeySequenceHandler handler)
    {
        Pattern = pattern;
        Handler = handler;
    }

    /// <summary>Gets the sequence pattern.</summary>
    public KeySequencePattern Pattern { get; }

    /// <summary>Gets the sequence handler.</summary>
    public KeySequenceHandler Handler { get; }
}
