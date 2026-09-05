namespace Terminal.Gui.KeySequences;

/// <summary>Describes how <see cref="KeySequenceBindings"/> starts matching sequences.</summary>
public enum KeySequenceMode
{
    /// <summary>Matches sequences only after a configured leader key starts capture.</summary>
    Leader,

    /// <summary>Matches sequences while command mode is active.</summary>
    Persistent
}
