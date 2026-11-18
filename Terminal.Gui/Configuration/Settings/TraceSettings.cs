namespace Terminal.Gui.Configuration;

/// <summary>
///     Settings POCO for <see cref="Terminal.Gui.Tracing.Trace"/> defaults (SettingsScope).
/// </summary>
public class TraceSettings
{
    /// <summary>Gets or sets the enabled trace categories.</summary>
    public TraceCategory EnabledCategories { get; set; } = TraceCategory.None;

    /// <summary>
    ///     The static facade instance. Always contains the current effective values.
    ///     Updated by the MEC binding at <see cref="IApplication"/> initialization.
    /// </summary>
    public static TraceSettings Defaults { get; set; } = new ();
}
