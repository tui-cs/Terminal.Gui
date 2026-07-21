namespace Terminal.Gui.KeySequences;

/// <summary>Describes the current sequence capture state.</summary>
public enum KeySequenceState
{
    /// <summary>No sequence is active.</summary>
    Idle,

    /// <summary>A leader key has started sequence capture.</summary>
    Capturing,

    /// <summary>Persistent command mode is active.</summary>
    CommandMode
}
