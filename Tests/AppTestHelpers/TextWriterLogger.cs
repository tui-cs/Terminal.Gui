using Microsoft.Extensions.Logging;

namespace AppTestHelpers;

internal class TextWriterLogger (TextWriter writer) : ILogger
{
    public IDisposable? BeginScope<TState> (TState state) where TState : notnull { return null; }

    public bool IsEnabled (LogLevel logLevel) { return true; }

    public void Log<TState> (
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? ex,
        Func<TState, Exception?, string> formatter
    )
    {
        writer.WriteLine (formatter (state, ex));
    }
}
