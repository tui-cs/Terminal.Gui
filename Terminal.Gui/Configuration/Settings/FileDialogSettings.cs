namespace Terminal.Gui.Configuration;

/// <summary>
///     Settings POCO for <see cref="Views.FileDialog"/> defaults (SettingsScope).
/// </summary>
public class FileDialogSettings
{
    /// <summary>Gets or sets the maximum number of search results in file dialogs.</summary>
    public int MaxSearchResults { get; set; } = 10000;

    /// <summary>
    ///     The static facade instance. Always contains the current effective values.
    ///     Updated by the MEC binding at <see cref="IApplication"/> initialization.
    /// </summary>
    public static FileDialogSettings Defaults { get; set; } = new ();
}
