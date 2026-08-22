using Microsoft.Extensions.Configuration;

namespace Terminal.Gui.Configuration;

/// <summary>
///     Nested <c>Themes</c> dictionary helpers. Legacy array-of-single-key (pre-MEC) sources are
///     skipped at add time (see <see cref="TuiConfigurationExtensions"/>); convert those files with
///     <c>Tools/MigrateConfig</c>.
/// </summary>
internal static class ThemeCatalog
{
    /// <summary>
    ///     Gets theme names from a nested <c>Themes</c> object, with <see cref="ThemeManager.DEFAULT_THEME_NAME"/> first.
    /// </summary>
    public static IReadOnlyList<string> GetNames (IConfiguration config)
    {
        List<string> names = [];

        foreach (IConfigurationSection child in config.GetSection ("Themes").GetChildren ())
        {
            if (!names.Contains (child.Key, StringComparer.OrdinalIgnoreCase))
            {
                names.Add (child.Key);
            }
        }

        names.RemoveAll (n => string.Equals (n, ThemeManager.DEFAULT_THEME_NAME, StringComparison.OrdinalIgnoreCase));
        names.Insert (0, ThemeManager.DEFAULT_THEME_NAME);

        return names;
    }

    /// <summary>
    ///     Resolves <paramref name="themeName"/> to the canonical key in <c>Themes</c>, or
    ///     <see langword="null"/> if it is not a nested theme name.
    /// </summary>
    public static string? CanonicalName (IConfiguration config, string themeName)
    {
        if (string.IsNullOrEmpty (themeName))
        {
            return null;
        }

        return GetNames (config)
            .FirstOrDefault (n => string.Equals (n, themeName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     Gets the nested <c>Themes:{name}</c> section, or <see langword="null"/> if missing.
    /// </summary>
    public static IConfigurationSection? Find (IConfiguration config, string themeName)
    {
        string? canonical = CanonicalName (config, themeName);

        if (canonical is null)
        {
            return null;
        }

        IConfigurationSection named = config.GetSection ("Themes").GetSection (canonical);

        return named.Exists () ? named : null;
    }
}
