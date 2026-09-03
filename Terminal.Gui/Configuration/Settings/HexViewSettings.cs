namespace Terminal.Gui.Configuration;

/// <summary>
///     Immutable settings record for <see cref="Views.HexView"/> defaults (ThemeScope).
/// </summary>
public sealed record HexViewSettings
{
    /// <summary>Gets the default cursor style for hex views.</summary>
    public CursorStyle DefaultCursorStyle { get; init; } = CursorStyle.BlinkingBlock;

    /// <summary>The compile-time-known defaults.</summary>
    public static HexViewSettings Default { get; } = new ();

    /// <summary>The currently effective values, updated atomically by <see cref="MecThemeManager"/>.</summary>
    public static HexViewSettings Current
    {
        get => Volatile.Read (ref _current);
        set => Volatile.Write (ref _current, value);
    }

    private static HexViewSettings _current = Default;
}
