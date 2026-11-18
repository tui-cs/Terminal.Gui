namespace Terminal.Gui.Configuration;

/// <summary>
///     Abstracts theme management. Allows switching themes and querying available themes.
/// </summary>
public interface IThemeManager
{
    /// <summary>Gets the name of the currently active theme.</summary>
    string CurrentThemeName { get; }

    /// <summary>Gets the names of all available themes.</summary>
    IReadOnlyList<string> ThemeNames { get; }

    /// <summary>
    ///     Switches the active theme. This updates all ThemeScope Settings POCOs
    ///     and triggers redraw on affected views.
    /// </summary>
    /// <param name="themeName">The name of the theme to activate.</param>
    /// <returns><see langword="true"/> if the theme was found and activated; otherwise <see langword="false"/>.</returns>
    bool SwitchTheme (string themeName);

    /// <summary>
    ///     Raised after the active theme has changed. The <see cref="EventArgs{T}.Value"/>
    ///     is the name of the newly-active theme.
    /// </summary>
    /// <remarks>
    ///     For static (non-DI) consumers such as <see cref="View"/> subclasses, see
    ///     <see cref="ThemeChanges.ThemeChanged"/>, which raises in lockstep with this event.
    /// </remarks>
    event EventHandler<App.EventArgs<string>>? ThemeChanged;
}
