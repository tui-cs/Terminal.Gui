using Terminal.Gui.Input;

namespace Terminal.Gui.KeySequences;

/// <summary>Describes a token in a <see cref="KeySequencePattern"/>.</summary>
public readonly record struct KeySequenceToken
{
    private KeySequenceToken (KeySequenceTokenKind kind, Key? key, string? name)
    {
        Kind = kind;
        Key = key;
        Name = name;
    }

    /// <summary>Gets the token kind.</summary>
    public KeySequenceTokenKind Kind { get; }

    /// <summary>Gets the literal key for <see cref="KeySequenceTokenKind.Literal"/> tokens.</summary>
    public Key? Key { get; }

    /// <summary>Gets the token name.</summary>
    public string? Name { get; }

    /// <summary>Creates a literal key token.</summary>
    public static KeySequenceToken Literal (Key key) => new (KeySequenceTokenKind.Literal, key, null);

    /// <summary>Creates a count token.</summary>
    public static KeySequenceToken Count (string name = "count") => new (KeySequenceTokenKind.Count, null, name);

    /// <summary>Creates a printable character token.</summary>
    public static KeySequenceToken Char (string name = "char") => new (KeySequenceTokenKind.Char, null, name);

    /// <summary>Creates an arbitrary key token.</summary>
    public static KeySequenceToken AnyKey (string name = "key") => new (KeySequenceTokenKind.AnyKey, null, name);

    /// <inheritdoc/>
    public override string ToString ()
    {
        return Kind switch
        {
            KeySequenceTokenKind.Literal => Key?.ToString () ?? "",
            KeySequenceTokenKind.Count => $"<{Name ?? "count"}>",
            KeySequenceTokenKind.Char => $"<{Name ?? "char"}>",
            KeySequenceTokenKind.AnyKey => $"<{Name ?? "key"}>",
            _ => Kind.ToString ()
        };
    }
}
