namespace Terminal.Gui.Configuration;

/// <summary>
///     Immutable settings record for <see cref="Views.TextView"/> defaults (ThemeScope).
/// </summary>
public sealed record TextViewSettings
{
    /// <summary>Gets the default cursor style for text views.</summary>
    public CursorStyle DefaultCursorStyle { get; init; } = CursorStyle.BlinkingBar;

    /// <summary>The compile-time-known defaults.</summary>
    public static TextViewSettings Default { get; } = new ();

    /// <summary>The currently effective values, updated atomically by <see cref="MecThemeManager"/>.</summary>
    public static TextViewSettings Current
    {
        get => Volatile.Read (ref _current);
        set => Volatile.Write (ref _current, value);
    }

    private static TextViewSettings _current = Default;
}
