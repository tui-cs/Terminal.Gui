namespace Terminal.Gui.Configuration;

/// <summary>
///     Immutable settings record for <see cref="Views.MenuBar"/> defaults.
/// </summary>
public sealed record MenuBarSettings
{
    /// <summary>Gets the default border style for menu bars.</summary>
    public LineStyle DefaultBorderStyle { get; init; } = LineStyle.None;

    /// <summary>Gets the default activation key for menu bars.</summary>
    public Key DefaultKey { get; init; } = Key.F10;

    /// <summary>The compile-time-known defaults.</summary>
    public static MenuBarSettings Default { get; } = new ();

    /// <summary>The currently effective values, updated atomically by <see cref="MecThemeManager"/>.</summary>
    public static MenuBarSettings Current
    {
        get => Volatile.Read (ref _current);
        set => Volatile.Write (ref _current, value);
    }

    private static MenuBarSettings _current = Default;
}
