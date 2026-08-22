namespace Terminal.Gui.Configuration;

/// <summary>
///     Immutable settings record for <see cref="Views.TextField"/> defaults (ThemeScope).
/// </summary>
public sealed record TextFieldSettings
{
    /// <summary>Gets the default cursor style for text fields.</summary>
    public CursorStyle DefaultCursorStyle { get; init; } = CursorStyle.BlinkingBar;

    /// <summary>The compile-time-known defaults.</summary>
    public static TextFieldSettings Default { get; } = new ();

    /// <summary>The currently effective values, updated atomically by <see cref="MecThemeManager"/>.</summary>
    public static TextFieldSettings Current
    {
        get => Volatile.Read (ref _current);
        set => Volatile.Write (ref _current, value);
    }

    private static TextFieldSettings _current = Default;
}
