namespace Terminal.Gui.Configuration;

/// <summary>
///     Immutable settings record for <see cref="Views.CheckBox"/> visual defaults (ThemeScope).
/// </summary>
public sealed record CheckBoxSettings
{
    /// <summary>Gets the default mouse highlight states for checkboxes.</summary>
    public MouseState DefaultMouseHighlightStates { get; init; } = MouseState.PressedOutside | MouseState.Pressed | MouseState.In;

    /// <summary>The compile-time-known defaults.</summary>
    public static CheckBoxSettings Default { get; } = new ();

    /// <summary>The currently effective values, updated atomically by <see cref="MecThemeManager"/>.</summary>
    public static CheckBoxSettings Current
    {
        get => Volatile.Read (ref _current);
        set => Volatile.Write (ref _current, value);
    }

    private static CheckBoxSettings _current = Default;
}
