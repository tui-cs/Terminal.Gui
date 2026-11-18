namespace Terminal.Gui.Tracing;

/// <summary>
///     Represents a disposable scope for tracing that automatically restores previous state.
///     Used by <see cref="Trace.PushScope"/> to provide scoped tracing configuration.
/// </summary>
internal sealed class TraceScope : IDisposable
{
    private readonly TraceCategory _previousCategories;
    private readonly ITraceBackend? _previousBackend;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TraceScope"/> class.
    /// </summary>
    /// <param name="categories">The trace categories to enable in this scope.</param>
    /// <param name="backend">Optional backend to use. If null, uses default backend selection.</param>
    public TraceScope (TraceCategory categories, ITraceBackend? backend)
    {
        _previousCategories = Trace.EnabledCategories;
        _previousBackend = Trace.GetAsyncLocalBackend ();

        Trace.EnabledCategories = categories;

        if (backend is { })
        {
            Trace.Backend = backend;
        }
    }

    /// <summary>
    ///     Restores the previous tracing state.
    /// </summary>
    public void Dispose ()
    {
        Trace.EnabledCategories = _previousCategories;
        Trace.SetAsyncLocalBackend (_previousBackend);
    }
}
