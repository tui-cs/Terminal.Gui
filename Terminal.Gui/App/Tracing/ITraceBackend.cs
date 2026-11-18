namespace Terminal.Gui.Tracing;

/// <summary>
///     Interface for trace backends. Implement this to capture trace events
///     for logging, testing, or debugging.
/// </summary>
public interface ITraceBackend
{
    /// <summary>
    ///     Logs a trace entry.
    /// </summary>
    /// <param name="entry">The trace entry to log.</param>
    void Log (TraceEntry entry);

    /// <summary>
    ///     Clears any captured entries. For backends that don't capture, this is a no-op.
    /// </summary>
    void Clear ();
}
