namespace Terminal.Gui.Configuration;

/// <summary>
///     Settings POCO for <see cref="Input.Key"/> defaults (SettingsScope).
/// </summary>
public class KeySettings
{
    /// <summary>Gets or sets the separator character used when parsing and printing Keys.</summary>
    public Rune Separator { get; set; } = new Rune ('+');

    /// <summary>
    ///     The static facade instance. Always contains the current effective values.
    ///     Updated by the MEC binding at <see cref="IApplication"/> initialization.
    /// </summary>
    public static KeySettings Defaults { get; set; } = new ();
}
