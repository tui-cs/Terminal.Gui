using Terminal.Gui.Tracing;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Default implementation of <see cref="IAnsiStartupGate"/>.
/// </summary>
public sealed class AnsiStartupGate : IAnsiStartupGate
{
    private readonly Lock _lock = new ();
    private readonly Dictionary<AnsiStartupQuery, QueryState> _queries = [];

    private sealed class QueryState
    {
        public DateTime DeadlineUtc { get; init; }
        public bool Completed { get; set; }
        public bool TimedOut { get; set; }
    }

    /// <summary>
    ///     Creates a new startup gate.
    /// </summary>
    /// <param name="utcNow">Optional time provider for testability.</param>
    public AnsiStartupGate (Func<DateTime>? utcNow = null)
    {
        UtcNow = utcNow ?? (() => DateTime.UtcNow);
    }

    internal Func<DateTime> UtcNow { get; }

    /// <inheritdoc/>
    public IDisposable RegisterQuery (AnsiStartupQuery query, TimeSpan timeout)
    {
        DateTime nowUtc = UtcNow ();
        DateTime deadlineUtc = nowUtc + timeout;

        lock (_lock)
        {
            _queries [query] = new QueryState
            {
                DeadlineUtc = deadlineUtc
            };
        }

        Trace.Lifecycle (nameof (AnsiStartupGate), "RegisterQuery", $"'{query}' timeout={timeout.TotalMilliseconds}ms");

        return new CompletionHandle (this, query);
    }

    /// <inheritdoc/>
    public void MarkComplete (AnsiStartupQuery query)
    {
        lock (_lock)
        {
            if (!_queries.TryGetValue (query, out QueryState? state) || state.Completed || state.TimedOut)
            {
                return;
            }

            state.Completed = true;
        }

        Trace.Lifecycle (nameof (AnsiStartupGate), "MarkComplete", $"'{query}' completed");
    }

    /// <inheritdoc/>
    public bool IsReady
    {
        get
        {
            SweepTimedOutQueries ();

            lock (_lock)
            {
                return _queries.Values.All (q => q.Completed || q.TimedOut);
            }
        }
    }

    /// <inheritdoc/>
    public IReadOnlyCollection<AnsiStartupQuery> PendingQueries
    {
        get
        {
            SweepTimedOutQueries ();

            lock (_lock)
            {
                return _queries.Where (kvp => !kvp.Value.Completed && !kvp.Value.TimedOut).Select (kvp => kvp.Key).ToArray ();
            }
        }
    }

    private void SweepTimedOutQueries ()
    {
        AnsiStartupQuery [] timedOutQueries;
        DateTime nowUtc = UtcNow ();

        lock (_lock)
        {
            timedOutQueries = _queries.Where (kvp => !kvp.Value.Completed && !kvp.Value.TimedOut && nowUtc >= kvp.Value.DeadlineUtc)
                                      .Select (kvp => kvp.Key)
                                      .ToArray ();

            foreach (AnsiStartupQuery query in timedOutQueries)
            {
                _queries [query].TimedOut = true;
            }
        }

        foreach (AnsiStartupQuery query in timedOutQueries)
        {
            Trace.Lifecycle (nameof (AnsiStartupGate), "Timeout", $"'{query}' timed out");
        }
    }

    private sealed class CompletionHandle : IDisposable
    {
        private readonly AnsiStartupGate _gate;
        private readonly AnsiStartupQuery _query;

        public CompletionHandle (AnsiStartupGate gate, AnsiStartupQuery query)
        {
            _gate = gate;
            _query = query;
        }

        public void Dispose () => _gate.MarkComplete (_query);
    }
}
