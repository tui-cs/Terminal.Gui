#nullable enable
using System.Text;
using Microsoft.Extensions.Logging;

namespace UICatalog;

/// <summary>
///     An <see cref="ILoggerProvider"/> that captures log output in-memory for scenario debugging.
///     Supports marking a position when a scenario starts and retrieving logs since that point.
/// </summary>
public class ScenarioLogCapture : ILoggerProvider
{
    private const int MaxBufferChars = 256_000;
    private const int TrimTargetChars = 192_000;

    private readonly object _lock = new ();
    private readonly StringBuilder _buffer = new ();

    private int _scenarioStartPosition;
    public int ScenarioStartPosition
    {
        get
        {
            lock (_lock)
            {
                return _scenarioStartPosition;
            }
        }
        private set => _scenarioStartPosition = value;
    }

    private bool _hasErrors;
    /// <summary>
    ///     Gets whether any Error-level or higher logs have been recorded since the last <see cref="MarkScenarioStart"/> call.
    /// </summary>
    public bool HasErrors
    {
        get
        {
            lock (_lock)
            {
                return _hasErrors;
            }
        }
        set => _hasErrors = value;
    }

    /// <summary>
    ///     Marks the current buffer position as the start of a scenario.
    ///     Call this before running a scenario to capture only that scenario's logs.
    /// </summary>
    public void MarkScenarioStart ()
    {
        lock (_lock)
        {
            _scenarioStartPosition = _buffer.Length;
            _hasErrors = false;
        }
    }

    /// <summary>
    ///     Gets all log entries since the last <see cref="MarkScenarioStart"/> call.
    /// </summary>
    /// <returns>The log content for the current scenario.</returns>
    public string GetScenarioLogs ()
    {
        lock (_lock)
        {
            if (_scenarioStartPosition >= _buffer.Length)
            {
                return string.Empty;
            }

            return _buffer.ToString (_scenarioStartPosition, _buffer.Length - _scenarioStartPosition);
        }
    }

    /// <summary>
    ///     Gets all log entries in the buffer.
    /// </summary>
    /// <returns>All captured log content.</returns>
    public string GetAllLogs ()
    {
        lock (_lock)
        {
            return _buffer.ToString ();
        }
    }

    /// <summary>
    ///     Clears all captured logs and resets state.
    /// </summary>
    public void Clear ()
    {
        lock (_lock)
        {
            _buffer.Clear ();
            _scenarioStartPosition = 0;
            _hasErrors = false;
        }
    }

    /// <inheritdoc />
    public ILogger CreateLogger (string categoryName)
    {
        return new ScenarioLogger (this);
    }

    /// <summary>
    ///     Called by <see cref="ScenarioLogger"/> to append a log entry.
    /// </summary>
    /// <param name="logLevel">The log level of the entry.</param>
    /// <param name="message">The formatted log message.</param>
    internal void Log (LogLevel logLevel, string message)
    {
        lock (_lock)
        {
            _buffer.AppendLine ($"[{logLevel}] {message}");
            TrimIfNeeded ();

            if (logLevel >= LogLevel.Error)
            {
                _hasErrors = true;
            }
        }
    }

    private void TrimIfNeeded ()
    {
        if (_buffer.Length <= MaxBufferChars)
        {
            return;
        }

        var removeCount = _buffer.Length - TrimTargetChars;

        if (removeCount <= 0)
        {
            return;
        }

        int nextLineBreak = -1;

        for (int i = removeCount; i < _buffer.Length; i++)
        {
            if (_buffer [i] == '\n')
            {
                nextLineBreak = i;
                break;
            }
        }

        if (nextLineBreak >= 0)
        {
            removeCount = nextLineBreak + 1;
        }

        _buffer.Remove (0, removeCount);
        _scenarioStartPosition = Math.Max (0, _scenarioStartPosition - removeCount);
    }

    /// <inheritdoc />
    public void Dispose ()
    {
        // Nothing to dispose - buffer is managed memory
    }

    /// <summary>
    ///     Internal logger implementation that delegates to <see cref="ScenarioLogCapture"/>.
    /// </summary>
    private sealed class ScenarioLogger : ILogger
    {
        private readonly ScenarioLogCapture _capture;

        public ScenarioLogger (ScenarioLogCapture capture)
        {
            _capture = capture;
        }

        /// <inheritdoc />
        public IDisposable? BeginScope<TState> (TState state) where TState : notnull
        {
            return null;
        }

        /// <inheritdoc />
        public bool IsEnabled (LogLevel logLevel)
        {
            return true;
        }

        /// <inheritdoc />
        public void Log<TState> (
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            string message = formatter (state, exception);
            _capture.Log (logLevel, message);
        }
    }
}
