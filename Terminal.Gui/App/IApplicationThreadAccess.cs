namespace Terminal.Gui.App;

/// <summary>Optional support for automatic UI-thread affinity diagnostics.</summary>
/// <remarks>
///     Custom application implementations can implement this capability without changing <see cref="IApplication"/>.
///     This capability enables view checks; a custom application remains responsible for marshaling its own lifecycle
///     UI work. Deferred worker-End cleanup is provided by the built-in application implementation.
/// </remarks>
public interface IApplicationThreadAccess
{
    /// <summary>Gets or sets whether associated view mutations verify UI-thread access. Defaults to false.</summary>
    /// <remarks>This setting is available in both Debug and Release builds and affects only this application.</remarks>
    bool ThreadAccessChecksEnabled { get; set; }
}
