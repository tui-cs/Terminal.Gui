namespace Terminal.Gui.Configuration;

/// <summary>
///     Settings POCO for <see cref="Views.FileDialogStyle"/> defaults (SettingsScope).
/// </summary>
public class FileDialogStyleSettings
{
    /// <summary>Gets or sets whether file dialogs should use colors by default.</summary>
    public bool DefaultUseColors { get; set; } = true;

    /// <summary>Gets or sets whether file dialogs should use Unicode characters by default.</summary>
    public bool DefaultUseUnicodeCharacters { get; set; } = false;

    /// <summary>
    ///     The static facade instance. Always contains the current effective values.
    ///     Updated by the MEC binding at <see cref="IApplication"/> initialization.
    /// </summary>
    public static FileDialogStyleSettings Defaults { get; set; } = new ();
}
