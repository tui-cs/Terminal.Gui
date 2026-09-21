namespace Terminal.Gui.KeySequences;

/// <summary>Describes the kind of a key sequence token.</summary>
public enum KeySequenceTokenKind
{
    /// <summary>A literal key.</summary>
    Literal,

    /// <summary>Zero or more digit keys parsed as a count.</summary>
    Count,

    /// <summary>Any printable non-control key.</summary>
    Char,

    /// <summary>Any valid key.</summary>
    AnyKey
}
