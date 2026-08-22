namespace Terminal.Gui.Configuration;

/// <summary>
///     Immutable settings record for <see cref="Views.SelectorBase"/> defaults (ThemeScope).
/// </summary>
public sealed record SelectorBaseSettings
{
    /// <summary>Gets the default mouse highlight states for selectors.</summary>
    public MouseState DefaultMouseHighlightStates { get; init; } = MouseState.In;

    /// <summary>The compile-time-known defaults.</summary>
    public static SelectorBaseSettings Default { get; } = new ();

    /// <summary>The currently effective values, updated atomically by <see cref="MecThemeManager"/>.</summary>
    public static SelectorBaseSettings Current
    {
        get => Volatile.Read (ref _current);
        set => Volatile.Write (ref _current, value);
    }

    private static SelectorBaseSettings _current = Default;
}
