namespace Terminal.Gui.Configuration;

/// <summary>
///     Immutable settings record for <see cref="MessageBox"/> visual defaults (ThemeScope).
/// </summary>
public sealed record MessageBoxSettings
{
    /// <summary>Gets the default border style for message boxes.</summary>
    public LineStyle DefaultBorderStyle { get; init; } = LineStyle.Heavy;

    /// <summary>Gets the default button alignment for message boxes.</summary>
    public Alignment DefaultButtonAlignment { get; init; } = Alignment.Center;

    /// <summary>The compile-time-known defaults.</summary>
    public static MessageBoxSettings Default { get; } = new ();

    /// <summary>The currently effective values, updated atomically by <see cref="MecThemeManager"/>.</summary>
    public static MessageBoxSettings Current
    {
        get => Volatile.Read (ref _current);
        set => Volatile.Write (ref _current, value);
    }

    private static MessageBoxSettings _current = Default;
}
