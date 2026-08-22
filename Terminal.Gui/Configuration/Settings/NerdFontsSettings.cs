namespace Terminal.Gui.Configuration;

/// <summary>
///     Immutable settings record for <see cref="Text.NerdFonts"/> defaults (ThemeScope).
/// </summary>
public sealed record NerdFontsSettings
{
    /// <summary>Gets whether Nerd Fonts glyphs are enabled.</summary>
    public bool Enable { get; init; } = false;

    /// <summary>The compile-time-known defaults.</summary>
    public static NerdFontsSettings Default { get; } = new ();

    /// <summary>The currently effective values, updated atomically by <see cref="MecThemeManager"/>.</summary>
    public static NerdFontsSettings Current
    {
        get => Volatile.Read (ref _current);
        set => Volatile.Write (ref _current, value);
    }

    private static NerdFontsSettings _current = Default;
}
