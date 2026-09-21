namespace Terminal.Gui.KeySequences;

/// <summary>Describes how completed patterns are matched when they are also prefixes of longer patterns.</summary>
public enum KeySequenceMatchMode
{
    /// <summary>Waits for additional input when a completed pattern is also a prefix of a longer pattern.</summary>
    Longest,

    /// <summary>Matches as soon as the pattern is complete.</summary>
    Immediate
}
