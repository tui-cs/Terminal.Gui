namespace Terminal.Gui.Configuration;

/// <summary>
///     Static facade that raises when the active theme has changed. Provided for consumers that cannot
///     take an <see cref="IThemeManager"/> dependency (typically <see cref="View"/> subclasses).
/// </summary>
public static class ThemeChanges
{
    static ThemeChanges ()
    {
        ThemeManager.ThemeChanged += (_, e) =>
                                     {
                                         Generation++;
                                         ThemeChanged?.Invoke (null, e);
                                     };
    }

    /// <summary>Identifies the glyph/configuration generation used by resolved line caches.</summary>
    internal static int Generation { get; private set; }

    /// <summary>
    ///     Raised after the active theme has changed.
    ///     The <see cref="App.EventArgs{T}.Value"/> is the name of the currently-active theme.
    /// </summary>
    public static event EventHandler<App.EventArgs<string>>? ThemeChanged;
}
