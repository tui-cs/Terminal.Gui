namespace Terminal.Gui.Configuration;

/// <summary>
///     Settings POCO for driver-level configuration (SettingsScope).
/// </summary>
public class DriverSettings
{
    /// <summary>Gets or sets whether to force 16-color mode.</summary>
    public bool Force16Colors { get; set; }

    /// <summary>Gets or sets the size detection mode.</summary>
    public SizeDetectionMode SizeDetection { get; set; } = SizeDetectionMode.Polling;

    /// <summary>
    ///     The static facade instance. Always contains the current effective values.
    ///     Updated by the MEC binding at <see cref="IApplication"/> initialization.
    /// </summary>
    public static DriverSettings Defaults { get; set; } = new ();
}
