namespace Terminal.Gui.Configuration;

/// <summary>
///     Settings POCO for application-level configuration (SettingsScope).
///     These correspond to the static properties on <see cref="Application"/> and related classes.
/// </summary>
/// <remarks>
///     Key bindings are not properties on this POCO. Nested JSON <c>Application.DefaultKeyBindings</c>
///     overlays <see cref="Application.DefaultKeyBindings"/> when configuration is applied.
/// </remarks>
public class ApplicationSettings
{
    /// <summary>Gets or sets the application model (FullScreen or Inline).</summary>
    public AppModel AppModel { get; set; } = AppModel.FullScreen;

    /// <summary>Gets or sets the driver to force (e.g., "windows", "dotnet", "ansi"). Empty means auto-detect.</summary>
    public string ForceDriver { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the mouse is disabled.</summary>
    public bool IsMouseDisabled { get; set; }

    /// <summary>
    ///     The static facade instance. Always contains the current effective values.
    ///     Updated by the MEC binding at <see cref="IApplication"/> initialization.
    /// </summary>
    public static ApplicationSettings Defaults { get; set; } = new ();
}
