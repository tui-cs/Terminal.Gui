namespace Terminal.Gui.Tracing;

/// <summary>
///     A no-op backend that discards all trace entries. This is the default.
/// </summary>
public sealed class NullBackend : ITraceBackend
{
    /// <inheritdoc/>
    public void Log (TraceEntry entry) { }

    /// <inheritdoc/>
    public void Clear () { }
}
