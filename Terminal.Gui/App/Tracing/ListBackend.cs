namespace Terminal.Gui.Tracing;

/// <summary>
///     A backend that captures trace entries to a list for testing and inspection.
/// </summary>
public sealed class ListBackend : ITraceBackend
{
    private readonly List<TraceEntry> _entries = [];

    /// <summary>
    ///     Gets the captured trace entries.
    /// </summary>
    public IReadOnlyList<TraceEntry> Entries => _entries;

    /// <inheritdoc/>
    public void Log (TraceEntry entry) => _entries.Add (entry);

    /// <inheritdoc/>
    public void Clear () => _entries.Clear ();
}
