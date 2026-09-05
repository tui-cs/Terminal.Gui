namespace Terminal.Gui.KeySequences;

/// <summary>Describes the result of processing a key through <see cref="KeySequenceBindings"/>.</summary>
public enum KeySequenceResult
{
    /// <summary>The key did not start or continue a sequence.</summary>
    NotLeader,

    /// <summary>The key started sequence capture.</summary>
    Started,

    /// <summary>The key continued an incomplete sequence.</summary>
    Pending,

    /// <summary>The key completed a sequence and its handler consumed it.</summary>
    Matched,

    /// <summary>The key canceled active sequence capture.</summary>
    Canceled,

    /// <summary>The key was invalid for the active sequence.</summary>
    Rejected,

    /// <summary>The active sequence timed out.</summary>
    TimedOut,

    /// <summary>Persistent command mode was entered.</summary>
    ModeEntered,

    /// <summary>Persistent command mode was exited.</summary>
    ModeExited
}
