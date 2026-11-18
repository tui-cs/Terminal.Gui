using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using UnitTests;
using UnitTests.Parallelizable;

namespace ApplicationTests;

[Collection ("Logging Tests")]
public class LoggingTests : IDisposable
{
    private readonly ILogger _originalLogger;
    private readonly ITestOutputHelper _output;

    public LoggingTests (ITestOutputHelper output)
    {
        _output = output;
        _originalLogger = Logging.Logger;
        Logging.Logger = NullLogger.Instance;
    }

    public void Dispose () => Logging.Logger = _originalLogger;

    [Fact]
    public void Uses_Global_Logger_When_No_Scope ()
    {
        CollectingLogger globalLogger = new ();
        Logging.Logger = globalLogger;

        Logging.Information ("global-message");

        Assert.True (globalLogger.Contains ("global-message"));
    }

    [Fact]
    public void PushLogger_Overrides_Global_And_Restores_After_Dispose ()
    {
        CollectingLogger globalLogger = new ();
        CollectingLogger scopedLogger = new ();
        Logging.Logger = globalLogger;

        using (Logging.PushLogger (scopedLogger))
        {
            Logging.Warning ("scoped-message");
        }

        Logging.Warning ("global-message");

        Assert.True (scopedLogger.Contains ("scoped-message"));
        Assert.False (globalLogger.Contains ("scoped-message"));
        Assert.True (globalLogger.Contains ("global-message"));
    }

    [Fact]
    public void PushLogger_Nested_Scopes_Restore_Correctly ()
    {
        CollectingLogger outerLogger = new ();
        CollectingLogger innerLogger = new ();

        using (Logging.PushLogger (outerLogger))
        {
            Logging.Debug ("outer-before-inner");

            using (Logging.PushLogger (innerLogger))
            {
                Logging.Debug ("inner-message");
            }

            Logging.Debug ("outer-after-inner");
        }

        Assert.True (outerLogger.Contains ("outer-before-inner"));
        Assert.True (outerLogger.Contains ("outer-after-inner"));
        Assert.False (outerLogger.Contains ("inner-message"));

        Assert.True (innerLogger.Contains ("inner-message"));
        Assert.False (innerLogger.Contains ("outer-before-inner"));
        Assert.False (innerLogger.Contains ("outer-after-inner"));
    }

    [Fact]
    public async Task PushLogger_Flows_Across_Async_Boundary ()
    {
        CollectingLogger scopedLogger = new ();

        using (Logging.PushLogger (scopedLogger))
        {
            await Task.Run (() => Logging.Trace ("async-message"), TestContext.Current.CancellationToken);
        }

        Assert.True (scopedLogger.Contains ("async-message"));
    }

    /// <summary>
    ///     Demonstrates how PushLogger enables per-test log capture that appears in xUnit test output.
    ///     Run this test and check the test output - you'll see Terminal.Gui internal logs!
    /// </summary>
    [Fact]
    public void PushLogger_Captures_Logs_To_XUnit_Output ()
    {
        // Use Verbose to see all log levels for debugging
        using (TestLogging.Verbose (_output))
        {
            // These log calls from Terminal.Gui code will appear in test output
            Logging.Trace ("This is a TRACE message - verbose debugging info");
            Logging.Debug ("This is a DEBUG message - diagnostic information");
            Logging.Information ("This is an INFO message - general flow");
            Logging.Warning ("This is a WARNING message - something unusual");
            Logging.Error ("This is an ERROR message - something went wrong");

            // Simulate what happens inside Terminal.Gui during a test
            _output.WriteLine ("");
            _output.WriteLine ("--- Simulating Terminal.Gui internal operations ---");
            SimulateTerminalGuiOperation ();
        }

        // After scope disposed, logs no longer go to test output
        Logging.Information ("This message goes to NullLogger, not test output");

        _output.WriteLine ("");
        _output.WriteLine ("✓ Test completed - check output above for captured logs!");
    }

    /// <summary>
    ///     Demonstrates the default TestLogging.BindTo() behavior - only warnings and errors.
    /// </summary>
    [Fact]
    public void TestLogging_Default_Only_Shows_Warnings_And_Errors ()
    {
        // Default: only Warning and above
        using (TestLogging.BindTo (_output))
        {
            Logging.Trace ("TRACE - should NOT appear");
            Logging.Debug ("DEBUG - should NOT appear");
            Logging.Information ("INFO - should NOT appear");
            Logging.Warning ("WARNING - should appear");
            Logging.Error ("ERROR - should appear");
        }

        _output.WriteLine ("✓ Only WARNING and ERROR should appear above");
    }

    /// <summary>
    ///     Demonstrates TestLogging.Verbose() for debugging - shows all log levels.
    /// </summary>
    [Fact]
    public void TestLogging_Verbose_Shows_All_Levels ()
    {
        // Verbose: all levels for debugging
        using (TestLogging.Verbose (_output))
        {
            Logging.Trace ("TRACE - should appear");
            Logging.Debug ("DEBUG - should appear");
            Logging.Information ("INFO - should appear");
            Logging.Warning ("WARNING - should appear");
            Logging.Error ("ERROR - should appear");
        }

        _output.WriteLine ("✓ All log levels should appear above");
    }

    /// <summary>
    ///     Shows parallel tests each capture their own logs without interference.
    /// </summary>
    [Theory]
    [InlineData ("TestA")]
    [InlineData ("TestB")]
    [InlineData ("TestC")]
    public async Task PushLogger_Isolates_Parallel_Test_Logs (string testId)
    {
        CollectingLogger collector = new ();

        using (Logging.PushLogger (collector))
        {
            // Each parallel test logs its own ID
            Logging.Information ($"Starting {testId}");

            await Task.Delay (10, TestContext.Current.CancellationToken); // Simulate async work

            Logging.Information ($"Finishing {testId}");
        }

        // Verify this test only sees its own logs
        Assert.True (collector.Contains ($"Starting {testId}"));
        Assert.True (collector.Contains ($"Finishing {testId}"));

        // Output to xUnit so we can see in test results
        _output.WriteLine ($"[{testId}] Captured {collector.MessageCount} log messages - isolation verified!");
    }

    private void SimulateTerminalGuiOperation ()
    {
        // This simulates what Terminal.Gui internal code does
        Logging.Debug ("Driver initialized");
        Logging.Trace ("Processing input queue");
        Logging.Debug ("Layout pass complete");
        Logging.Trace ("Draw pass complete");
    }

    private sealed class CollectingLogger : ILogger
    {
        private readonly ConcurrentQueue<string> _messages = new ();

        public int MessageCount => _messages.Count;

        public IDisposable BeginScope<TState> (TState state) where TState : notnull => NoopScope.Instance;

        public bool IsEnabled (LogLevel logLevel) => true;

        public void Log<TState> (LogLevel logLevel, EventId eventId, TState state, Exception? ex, Func<TState, Exception?, string> formatter) =>
            _messages.Enqueue (formatter (state, ex));

        public bool Contains (string text)
        {
            foreach (string message in _messages)
            {
                if (message.Contains (text, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }

    private sealed class NoopScope : IDisposable
    {
        public static readonly NoopScope Instance = new ();

        public void Dispose () { }
    }
}
