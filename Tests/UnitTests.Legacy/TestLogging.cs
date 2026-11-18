#nullable enable
using Microsoft.Extensions.Logging;
using Terminal.Gui.Tracing;

namespace UnitTests;

/// <summary>
///     Helper for binding Terminal.Gui logging to xUnit test output.
///     By default, only Warning and above are logged. Use <see cref="Verbose"/> for full logging.
/// </summary>
public static class TestLogging
{
    /// <summary>
    ///     Binds Terminal.Gui logging to xUnit output with default filtering (Warning and above).
    ///     Dispose the returned scope to restore the previous logger.
    /// </summary>
    /// <param name="output">The xUnit test output helper.</param>
    /// <returns>An <see cref="IDisposable"/> scope.</returns>
    /// <example>
    ///     <code>
    ///     using (TestLogging.BindTo(_output))
    ///     {
    ///         // Only warnings and errors appear in test output
    ///         Application.Init();
    ///         // ... test code
    ///         Application.Shutdown();
    ///     }
    ///     </code>
    /// </example>
    public static IDisposable BindTo (ITestOutputHelper output) => BindTo (output, LogLevel.Warning);

    /// <summary>
    ///     Binds Terminal.Gui logging to xUnit output with the specified minimum log level.
    /// </summary>
    /// <param name="output">The xUnit test output helper.</param>
    /// <param name="minLevel">The minimum log level to output.</param>
    /// <returns>An <see cref="IDisposable"/> scope.</returns>
    public static IDisposable BindTo (ITestOutputHelper output, LogLevel minLevel) => Logging.PushLogger (new TestOutputLogger (output, minLevel));

    /// <summary>
    ///     Binds Terminal.Gui logging to xUnit output with verbose logging (Trace and above).
    ///     Use this when debugging a specific test.
    /// </summary>
    /// <param name="output">The xUnit test output helper.</param>
    /// <returns>An <see cref="IDisposable"/> scope.</returns>
    /// <example>
    ///     <code>
    ///     using (TestLogging.Verbose(_output))
    ///     {
    ///         // All log levels appear in test output
    ///         Application.Init();
    ///         // ... test code
    ///         Application.Shutdown();
    ///     }
    ///     </code>
    /// </example>
    public static IDisposable Verbose (ITestOutputHelper output) => BindTo (output, LogLevel.Trace);

    /// <summary>
    ///     Binds Terminal.Gui logging to xUnit output with verbose logging (Trace and above)
    ///     and enables the specified trace categories.
    ///     Use this when debugging a specific test and you want to see trace output.
    /// </summary>
    /// <param name="output">The xUnit test output helper.</param>
    /// <param name="traceCategories">The trace categories to enable.</param>
    /// <returns>An <see cref="IDisposable"/> scope that restores previous state.</returns>
    /// <example>
    ///     <code>
    ///     using (TestLogging.Verbose(_output, TraceCategory.Command | TraceCategory.Mouse))
    ///     {
    ///         // All log levels appear in test output, Command and Mouse tracing enabled
    ///         CheckBox checkbox = new () { Id = "test" };
    ///         checkbox.InvokeCommand (Command.Activate);
    ///     }
    ///     </code>
    /// </example>
    public static IDisposable Verbose (ITestOutputHelper output, TraceCategory traceCategories) =>
        new CompositeDisposable (BindTo (output, LogLevel.Trace), Trace.PushScope (traceCategories));

    private sealed class CompositeDisposable (IDisposable first, IDisposable second) : IDisposable
    {
        public void Dispose ()
        {
            second.Dispose ();
            first.Dispose ();
        }
    }

    private sealed class TestOutputLogger (ITestOutputHelper output, LogLevel minLevel) : ILogger
    {
        public IDisposable? BeginScope<TState> (TState state) where TState : notnull => null;

        public bool IsEnabled (LogLevel logLevel) => logLevel >= minLevel;

        public void Log<TState> (LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled (logLevel))
            {
                return;
            }

            string levelTag = logLevel switch
                              {
                                  LogLevel.Trace => "TRC",
                                  LogLevel.Debug => "DBG",
                                  LogLevel.Information => "INF",
                                  LogLevel.Warning => "WRN",
                                  LogLevel.Error => "ERR",
                                  LogLevel.Critical => "CRT",
                                  _ => "???"
                              };

            try
            {
                output.WriteLine ($"[{levelTag}] {formatter (state, exception)}");
            }
            catch (InvalidOperationException)
            {
                // Test already completed - ignore late writes
            }
        }
    }
}
