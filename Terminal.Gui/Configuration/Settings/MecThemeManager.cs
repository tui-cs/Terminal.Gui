#pragma warning disable CS0618 // Obsolete - MecThemeManager forwards from legacy ThemeManager during transition

using Microsoft.Extensions.Configuration;

namespace Terminal.Gui.Configuration;

/// <summary>
///     MEC-backed implementation of <see cref="IThemeManager"/>.
///     During the transition period (PR #5411), this delegates writes to the legacy static <see cref="ThemeManager"/>
///     because the runtime theme/scheme dictionary is still owned by <see cref="ConfigurationManager.Settings"/>.
///     The Phase A2 work in #5416 will let this type own the theme/scheme data directly.
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

                    if (first is not null && !names.Contains (first.Key))
                    {
                        names.Add (first.Key);
                    }

                    continue;
                }

                if (!names.Contains (child.Key))
                {
                    names.Add (child.Key);
                }
            }

            if (!names.Contains (ThemeManager.DEFAULT_THEME_NAME))
            {
                names.Insert (0, ThemeManager.DEFAULT_THEME_NAME);
            }

            return names;
        }
    }

    private EventHandler<App.EventArgs<string>>? _themeChanged;

    /// <inheritdoc/>
    /// <remarks>
    ///     Forwarding from the legacy static <see cref="ThemeManager.ThemeChanged"/> event is wired up only while
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

        // Verify the theme exists in the MEC configuration (legacy array or nested dictionary).
        if (!ThemeNames.Contains (themeName))
        {
            return false;
        }

        ThemeSettings.Defaults = new () { Theme = themeName };

        // During transition, also update the existing ThemeManager. Its setter raises
        // ThemeManager.ThemeChanged, which is forwarded to subscribers via OnLegacyThemeChanged
        // (only while this instance has subscribers; see the ThemeChanged event above).
        try
        {
            ThemeManager.Theme = themeName;
        }
        catch (InvalidOperationException)
        {
        }
        catch (KeyNotFoundException)
        {
        }

        // Re-apply ThemeScope overlays for the already-selected theme. Do not rebind the
        // scalar Theme key from configuration — that would reset activeTheme to the JSON default.
        _builder.ApplyToStaticFacades (rebindSelectedTheme: false);

        return true;
    }
}
