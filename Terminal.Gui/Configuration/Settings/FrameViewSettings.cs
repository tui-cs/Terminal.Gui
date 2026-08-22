namespace Terminal.Gui.Configuration;

/// <summary>
///     Immutable settings record for <see cref="Views.FrameView"/> defaults (ThemeScope).
/// </summary>
public sealed record FrameViewSettings
{
    /// <summary>Gets the default border style for frame views.</summary>
    public LineStyle DefaultBorderStyle { get; init; } = LineStyle.Rounded;

    /// <summary>The compile-time-known defaults.</summary>
    public static FrameViewSettings Default { get; } = new ();

    /// <summary>The currently effective values, updated atomically by <see cref="MecThemeManager"/>.</summary>
    public static FrameViewSettings Current
    {
        get => Volatile.Read (ref _current);
        set => Volatile.Write (ref _current, value);
    }

    private static FrameViewSettings _current = Default;
}
