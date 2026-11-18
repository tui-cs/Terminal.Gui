namespace Terminal.Gui.Configuration;

/// <summary>
///     Settings POCO for theme selection (SettingsScope).
///     Controls which theme is active and what themes are available.
/// </summary>
public class ThemeSettings
{
    /// <summary>Gets or sets the name of the active theme.</summary>
    public string Theme { get; set; } = ThemeManager.DEFAULT_THEME_NAME;

    /// <summary>
    ///     The static facade instance. Always contains the current effective values.
    ///     Updated by the MEC binding at <see cref="IApplication"/> initialization.
    /// </summary>
    public static ThemeSettings Defaults { get; set; } = new ();
}
