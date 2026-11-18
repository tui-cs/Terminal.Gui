namespace Terminal.Gui.Configuration;

/// <summary>
///     Immutable settings record for <see cref="Views.CharMap"/> defaults (ThemeScope).
/// </summary>
public sealed record CharMapSettings
{
    /// <summary>Gets the default cursor style for character map views.</summary>
    public CursorStyle DefaultCursorStyle { get; init; } = CursorStyle.BlinkingBlock;

    /// <summary>The compile-time-known defaults.</summary>
    public static CharMapSettings Default { get; } = new ();

    /// <summary>The currently effective values, updated atomically by <see cref="MecThemeManager"/>.</summary>
    public static CharMapSettings Current
    {
        get => Volatile.Read (ref _current);
        set => Volatile.Write (ref _current, value);
    }

    private static CharMapSettings _current = Default;
}
