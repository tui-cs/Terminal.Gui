namespace Terminal.Gui.Configuration;

/// <summary>
///     Immutable settings record for <see cref="Views.Dialog"/> visual defaults (ThemeScope).
/// </summary>
public sealed record DialogSettings
{
    /// <summary>Gets the default shadow style for dialogs.</summary>
    public ShadowStyles DefaultShadow { get; init; } = ShadowStyles.Transparent;

    /// <summary>Gets the default border style for dialogs.</summary>
    public LineStyle DefaultBorderStyle { get; init; } = LineStyle.Heavy;

    /// <summary>Gets the default button alignment for dialogs.</summary>
    public Alignment DefaultButtonAlignment { get; init; } = Alignment.End;

    /// <summary>Gets the default button alignment modes for dialogs.</summary>
    public AlignmentModes DefaultButtonAlignmentModes { get; init; } = AlignmentModes.StartToEnd | AlignmentModes.AddSpaceBetweenItems;

    /// <summary>The compile-time-known defaults.</summary>
    public static DialogSettings Default { get; } = new ();

    /// <summary>The currently effective values, updated atomically by <see cref="MecThemeManager"/>.</summary>
    public static DialogSettings Current
    {
        get => Volatile.Read (ref _current);
        set => Volatile.Write (ref _current, value);
    }

    private static DialogSettings _current = Default;
}
