#pragma warning disable CS0618 // Obsolete - MecThemeManager forwards from legacy ThemeManager during transition

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
            // During transition, delegate to existing ThemeManager
            try
            {
                return ThemeManager.GetThemeNames ();
            }
            catch (InvalidOperationException)
            {
                return [ThemeManager.DEFAULT_THEME_NAME];
            }
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

        // Verify the theme exists before switching
        if (!ThemeNames.Contains (themeName))
        {
            return false;
        }

        // During transition, also update the existing ThemeManager. Its setter raises
        // ThemeManager.ThemeChanged, which is forwarded to subscribers via OnLegacyThemeChanged
        // (only while this instance has subscribers; see the ThemeChanged event above).
        try
        {
            ThemeManager.Theme = themeName;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (KeyNotFoundException)
        {
            return false;
        }

        // Re-apply all ThemeScope POCOs from configuration. This may rebind the scalar "Theme" key
        // from config, so the switched theme is recorded afterwards to ensure the selection persists.
        _builder.ApplyToStaticFacades ();

        // Record the switched theme on success, overriding any value the config re-apply may have set.
        ThemeSettings.Defaults.Theme = themeName;

        return true;
    }
}
