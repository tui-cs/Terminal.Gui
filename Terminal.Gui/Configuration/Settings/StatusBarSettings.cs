namespace Terminal.Gui.Configuration;

/// <summary>
///     Immutable settings record for <see cref="Views.StatusBar"/> defaults (ThemeScope).
/// </summary>
public sealed record StatusBarSettings
{
    /// <summary>Gets the default separator line style for status bars.</summary>
    public LineStyle DefaultSeparatorLineStyle { get; init; } = LineStyle.Single;

    /// <summary>The compile-time-known defaults.</summary>
    public static StatusBarSettings Default { get; } = new ();

    /// <summary>The currently effective values, updated atomically by <see cref="MecThemeManager"/>.</summary>
    public static StatusBarSettings Current
    {
        get => Volatile.Read (ref _current);
        set => Volatile.Write (ref _current, value);
    }

    private static StatusBarSettings _current = Default;
}
