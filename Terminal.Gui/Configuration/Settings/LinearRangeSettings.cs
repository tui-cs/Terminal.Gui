namespace Terminal.Gui.Configuration;

/// <summary>
///     Immutable settings record for <see cref="Views.LinearRangeDefaults"/> defaults (ThemeScope).
/// </summary>
public sealed record LinearRangeSettings
{
    /// <summary>Gets the default cursor style for linear range views.</summary>
    public CursorStyle DefaultCursorStyle { get; init; } = CursorStyle.BlinkingBlock;

    /// <summary>The compile-time-known defaults.</summary>
    public static LinearRangeSettings Default { get; } = new ();

    /// <summary>The currently effective values, updated atomically by <see cref="MecThemeManager"/>.</summary>
    public static LinearRangeSettings Current
    {
        get => Volatile.Read (ref _current);
        set => Volatile.Write (ref _current, value);
    }

    private static LinearRangeSettings _current = Default;
}
