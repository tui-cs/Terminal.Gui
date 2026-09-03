namespace Terminal.Gui.Configuration;

/// <summary>
///     Per-builder <see cref="IThemeManager"/>. Process-wide callers use the static
///     <see cref="ThemeManager"/> facade (<c>ThemeManager.Theme =</c>).
/// </summary>
public class MecThemeManager : IThemeManager
{
    private readonly TuiConfigurationBuilder _builder;
    private readonly object _themeChangedLock = new ();

    /// <summary>Initializes a new instance of <see cref="MecThemeManager"/>.</summary>
    public MecThemeManager (TuiConfigurationBuilder builder) { _builder = builder; }

    private void OnStaticThemeChanged (object? sender, App.EventArgs<string> e) => _themeChanged?.Invoke (this, e);

    /// <inheritdoc/>
    public string CurrentThemeName => ThemeSettings.Defaults.Theme;

    /// <inheritdoc/>
    public IReadOnlyList<string> ThemeNames => ThemeCatalog.GetNames (_builder.Configuration);

    private EventHandler<App.EventArgs<string>>? _themeChanged;

    /// <inheritdoc/>
    /// <remarks>
    ///     Forwards <see cref="ThemeManager.ThemeChanged"/> only while this instance has a subscriber,
    ///     so unused instances are not kept alive by the static event.
    /// </remarks>
    public event EventHandler<App.EventArgs<string>>? ThemeChanged
    {
        add
        {
            lock (_themeChangedLock)
            {
                if (_themeChanged is null)
                {
                    ThemeManager.ThemeChanged += OnStaticThemeChanged;
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
                    ThemeManager.ThemeChanged -= OnStaticThemeChanged;
                }
            }
        }
    }

    /// <inheritdoc/>
    public bool SwitchTheme (string themeName) => _builder.TrySwitchTheme (themeName);
}
