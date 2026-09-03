// Grok - grok-4.6
using System.Collections.Immutable;

namespace Terminal.Gui.Configuration;

/// <summary>
///     Static facade for the active theme. Backed by <see cref="ThemeSettings"/> and
///     <see cref="TuiConfigurationBuilder.Shared"/>.
/// </summary>
public static class ThemeManager
{
    /// <summary>The name of the default theme.</summary>
    public const string DEFAULT_THEME_NAME = "Default";

    /// <summary>
    ///     Gets or sets the currently selected theme name. Setting this applies ThemeScope overlays
    ///     and raises <see cref="ThemeChanged"/>.
    /// </summary>
    public static string Theme
    {
        get => ThemeSettings.Defaults.Theme ?? DEFAULT_THEME_NAME;
        set
        {
            if (string.IsNullOrEmpty (value))
            {
                return;
            }

            TuiConfigurationBuilder.Shared.TrySwitchTheme (value);
        }
    }

    /// <summary>Raised after <see cref="Theme"/> changes and overlays have been published.</summary>
    public static event EventHandler<App.EventArgs<string>>? ThemeChanged;

    /// <summary>Raises <see cref="ThemeChanged"/> after overlays were published by a non-Shared builder.</summary>
    internal static void RaiseThemeChanged (string themeName) =>
        ThemeChanged?.Invoke (null, new App.EventArgs<string> (themeName));

    /// <summary>Gets the current theme name.</summary>
    public static string GetCurrentThemeName () => Theme;

    /// <summary>Gets the available theme names from configuration, with <see cref="DEFAULT_THEME_NAME"/> first.</summary>
    public static ImmutableList<string> GetThemeNames () => [.. ThemeCatalog.GetNames (TuiConfigurationBuilder.Shared.Configuration)];
}
