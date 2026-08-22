using Microsoft.Extensions.Configuration;

namespace Terminal.Gui.Configuration;

/// <summary>
///     MEC-backed implementation of <see cref="IThemeManager"/>. Applies theme overlays through
///     <see cref="TuiConfigurationBuilder"/> and raises <see cref="IThemeManager.ThemeChanged"/>.
///     Distinct from the static <see cref="ThemeManager"/> facade (<c>ThemeManager.Theme =</c>).
/// </summary>
public class MecThemeManager : IThemeManager
{
    private readonly TuiConfigurationBuilder _builder;
    private readonly object _themeChangedLock = new ();

    /// <summary>Initializes a new instance of <see cref="MecThemeManager"/>.</summary>
    public MecThemeManager (TuiConfigurationBuilder builder) { _builder = builder; }

    private void OnLegacyThemeChanged (object? sender, App.EventArgs<string> e) => _themeChanged?.Invoke (this, e);

    /// <inheritdoc/>
    public string CurrentThemeName => ThemeSettings.Defaults.Theme;

    /// <inheritdoc/>
    public IReadOnlyList<string> ThemeNames
    {
        get
        {
            List<string> names = [];
            IConfigurationSection themes = _builder.Configuration.GetSection ("Themes");

            foreach (IConfigurationSection child in themes.GetChildren ())
            {
                if (int.TryParse (child.Key, out _))
                {
                    IConfigurationSection? first = child.GetChildren ().FirstOrDefault ();

                    if (first is not null && !names.Contains (first.Key, StringComparer.OrdinalIgnoreCase))
                    {
                        names.Add (first.Key);
                    }

                    continue;
                }

                if (!names.Contains (child.Key, StringComparer.OrdinalIgnoreCase))
                {
                    names.Add (child.Key);
                }
            }

            if (!names.Contains (ThemeManager.DEFAULT_THEME_NAME, StringComparer.OrdinalIgnoreCase))
            {
                names.Insert (0, ThemeManager.DEFAULT_THEME_NAME);
            }

            return names;
        }
    }

    private EventHandler<App.EventArgs<string>>? _themeChanged;

    /// <inheritdoc/>
    /// <remarks>
    ///     Forwarding from the static <see cref="ThemeManager.ThemeChanged"/> event is wired up only while
    ///     this instance has at least one subscriber. This avoids leaking the instance through the static event
    ///     (which would otherwise keep every <see cref="MecThemeManager"/> alive for the lifetime of the process).
    /// </remarks>
    public event EventHandler<App.EventArgs<string>>? ThemeChanged
    {
        add
        {
            lock (_themeChangedLock)
            {
                if (_themeChanged is null)
                {
                    ThemeManager.ThemeChanged += OnLegacyThemeChanged;
                }

                _themeChanged += value;
            }
        }
        remove
        {
            lock (_themeChangedLock)
            {
                _themeChanged -= value;

                if (_themeChanged is null)
                {
                    ThemeManager.ThemeChanged -= OnLegacyThemeChanged;
                }
            }
        }
    }

    /// <inheritdoc/>
    public bool SwitchTheme (string themeName)
    {
        if (string.IsNullOrEmpty (themeName))
        {
            return false;
        }

        IReadOnlyList<string> names = ThemeNames;
        string? canonical = names.FirstOrDefault (n => string.Equals (n, themeName, StringComparison.OrdinalIgnoreCase));

        if (canonical is null)
        {
            return false;
        }

        ThemeSettings.Defaults = new () { Theme = canonical };

        // Publish overlays before raising ThemeChanged so handlers that re-read
        // Button.DefaultShadow / Glyphs.* observe the new theme.
        _builder.ApplyToStaticFacades (rebindSelectedTheme: false);
        _themeChanged?.Invoke (this, new App.EventArgs<string> (canonical));
        ThemeManager.RaiseThemeChanged (canonical);

        return true;
    }
}
