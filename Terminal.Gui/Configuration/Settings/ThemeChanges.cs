#pragma warning disable CS0618 // Obsolete - bridges legacy ConfigurationManager.Applied and ThemeManager.ThemeChanged into the MEC-facing facade

namespace Terminal.Gui.Configuration;

/// <summary>
///     Static facade that raises when the active theme — or any setting that affects views
///     reading from <c>*Settings.Defaults</c> — has changed. Provided for consumers that cannot
///     take a <see cref="IThemeManager"/> dependency (typically <see cref="View"/> subclasses
///     constructed before an <see cref="IApplication"/> is available).
/// </summary>
/// <remarks>
///     <para>
///         This class is the MEC-friendly replacement for the obsolete
///         <c>ConfigurationManager.Applied</c> event subscriptions in <see cref="View"/> subclasses.
///         It raises after any settings re-application (theme switch, runtime config reload,
///         <c>ConfigurationManager.Reset()</c>, etc.) so that handlers re-read their static defaults.
///     </para>
///     <para>
///         The bridge to the legacy events is established the first time this class is referenced;
///         the subscription persists for the lifetime of the process and never needs explicit teardown.
///     </para>
/// </remarks>
public static class ThemeChanges
{
    static ThemeChanges ()
    {
        // ConfigurationManager.Apply() writes ConfigProperty values directly (bypassing the
        // ThemeManager.Theme setter), so the broad Applied event is required to catch the
        // most common change path (Init, Reset, RuntimeConfig).
        ConfigurationManager.Applied += (_, _) => ThemeChanged?.Invoke (null, new App.EventArgs<string> (ThemeManager.GetCurrentThemeName ()));

        // The narrow event also forwards, so explicit ThemeManager.Theme = X calls still surface here.
        ThemeManager.ThemeChanged += (_, e) => ThemeChanged?.Invoke (null, e);
    }

    /// <summary>
    ///     Raised after the active theme — or any setting affecting <c>*Settings.Defaults</c> — has changed.
    ///     The <see cref="App.EventArgs{T}.Value"/> is the name of the currently-active theme.
    /// </summary>
    public static event EventHandler<App.EventArgs<string>>? ThemeChanged;

    /// <summary>Raises <see cref="ThemeChanged"/> after overlays have been published.</summary>
    internal static void Raise (string themeName) => ThemeChanged?.Invoke (null, new App.EventArgs<string> (themeName));
}
