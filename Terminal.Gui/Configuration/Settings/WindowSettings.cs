namespace Terminal.Gui.Configuration;

/// <summary>
///     Immutable settings record for <see cref="Views.Window"/> visual defaults (ThemeScope).
/// </summary>
public sealed record WindowSettings
{
    /// <summary>Gets the default shadow style for windows.</summary>
    public ShadowStyles DefaultShadow { get; init; } = ShadowStyles.None;

    /// <summary>Gets the default border style for windows.</summary>
    public LineStyle DefaultBorderStyle { get; init; } = LineStyle.Single;

    /// <summary>The compile-time-known defaults.</summary>
    public static WindowSettings Default { get; } = new ();

    /// <summary>The currently effective values, updated atomically by <see cref="MecThemeManager"/>.</summary>
    public static WindowSettings Current
    {
        get => Volatile.Read (ref _current);
        set => Volatile.Write (ref _current, value);
    }

    private static WindowSettings _current = Default;
}
