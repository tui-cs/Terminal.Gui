namespace Terminal.Gui.Configuration;

/// <summary>
///     Immutable settings record for <see cref="Views.Menu"/> defaults (ThemeScope).
/// </summary>
public sealed record MenuSettings
{
    /// <summary>Gets the default border style for menus.</summary>
    public LineStyle DefaultBorderStyle { get; init; } = LineStyle.None;

    /// <summary>The compile-time-known defaults.</summary>
    public static MenuSettings Default { get; } = new ();

    /// <summary>The currently effective values, updated atomically by <see cref="MecThemeManager"/>.</summary>
    public static MenuSettings Current
    {
        get => Volatile.Read (ref _current);
        set => Volatile.Write (ref _current, value);
    }

    private static MenuSettings _current = Default;
}
