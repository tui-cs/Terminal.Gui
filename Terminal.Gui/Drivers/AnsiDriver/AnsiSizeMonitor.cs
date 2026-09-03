using System.Text.RegularExpressions;
using Terminal.Gui.Tracing;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Size monitor for <see cref="AnsiOutput"/> that uses either ANSI escape sequences or a native query
///     to detect terminal resize events.
/// </summary>
/// <remarks>
///     <para>
///         In ANSI-query mode, <see cref="AnsiSizeMonitor"/> sends <c>CSI 18t</c>. In native-query mode,
///         <see cref="AnsiOutput.GetSize"/> obtains the size from the operating system. Both modes retain
///         ANSI cursor-position discovery for inline applications.
///     </para>
///     <para>
///         <b>How it works:</b>
///     </para>
///     <list type="number">
///         <item><see cref="Poll"/> obtains the current size through the configured mode</item>
///         <item>ANSI-query mode parses the terminal's ESC [ 8 ; height ; width t response</item>
///         <item>Native-query mode reads the size through <see cref="AnsiOutput.GetSize"/></item>
///         <item>If size changed, <see cref="ISizeMonitor.SizeChanged"/> event is raised</item>
///     </list>
/// </remarks>
internal class AnsiSizeMonitor : ISizeMonitor
{
    private static readonly TimeSpan StartupSizeQueryTimeout = TimeSpan.FromMilliseconds (500);
    private static readonly TimeSpan StartupCursorPositionQueryTimeout = TimeSpan.FromMilliseconds (500);

    private readonly AnsiOutput _output;
    private readonly bool _queryTerminalSize;
    private Action<AnsiEscapeSequenceRequest>? _queueAnsiRequest;
    private IAnsiStartupGate? _startupGate;
    private IDisposable? _startupSizeQueryCompletionHandle;
    private IDisposable? _startupCursorPositionQueryCompletionHandle;
    private Size _lastSize;
    private DateTime _lastQuery = DateTime.MinValue;
    private readonly TimeSpan _queryThrottle = TimeSpan.FromMilliseconds (500); // Don't spam queries
    private bool _expectingResponse;
    private bool _sizeResponseReceived;
    private bool _cursorPositionReceived;

    /// <inheritdoc/>
    public bool InitialSizeReceived =>
        (!_queryTerminalSize || _sizeResponseReceived)
        && (_output.AppModel != AppModel.Inline || _cursorPositionReceived);

    /// <inheritdoc/>
    public int InitialCursorRow { get; private set; }

    /// <summary>
    ///     Creates a new ANSISizeMonitor.
    /// </summary>
    /// <param name="output">The ANSIOutput instance to query for size</param>
    /// <param name="queueAnsiRequest">Callback to queue ANSI requests (provided by driver/scheduler)</param>
    /// <param name="queryTerminalSize">
    ///     <see langword="true"/> to query terminal size with <c>CSI 18t</c>; <see langword="false"/> to use
    ///     the native query configured on <paramref name="output"/>.
    /// </param>
    public AnsiSizeMonitor (AnsiOutput output, Action<AnsiEscapeSequenceRequest>? queueAnsiRequest = null, bool queryTerminalSize = true)
    {
        _output = output;
        _queueAnsiRequest = queueAnsiRequest;
        _queryTerminalSize = queryTerminalSize;

        // Get initial size from console or fallback
        _lastSize = _output.GetSize ();
    }

    /// <inheritdoc/>
    public void Initialize (IDriver? driver)
    {
        if (driver is null)
        {
            Logging.Warning ("ANSISizeMonitor: Initialize called with null driver");

            return;
        }

        _queueAnsiRequest = driver.QueueAnsiRequest;
        _startupGate = driver.AnsiStartupGate;

        _startupCursorPositionQueryCompletionHandle = _startupGate?.RegisterQuery (AnsiStartupQuery.CursorPosition, StartupCursorPositionQueryTimeout);

        if (_queryTerminalSize)
        {
            Trace.Lifecycle (nameof (AnsiSizeMonitor), "Initialize", "Driver wired up; sending initial size query");
            _startupSizeQueryCompletionHandle = _startupGate?.RegisterQuery (AnsiStartupQuery.TerminalSize, StartupSizeQueryTimeout);
            SendSizeQuery ();
        }

        SendCursorPositionQuery ();
    }

    private void SendSizeQuery ()
    {
        if (_queueAnsiRequest is null)
        {
            Logging.Warning ("ANSISizeMonitor: Cannot send size query - _queueAnsiRequest is null");

            return;
        }

        _expectingResponse = true;
        _lastQuery = DateTime.Now;

        //Trace.Lifecycle (nameof (AnsiSizeMonitor), "SendSizeQuery", "Queuing CSI 18t size query");

        AnsiEscapeSequenceRequest request = new ()
        {
            Request = EscSeqUtils.CSI_ReportWindowSizeInChars.Request,
            Value = EscSeqUtils.CSI_ReportWindowSizeInChars.Value,
            Terminator = EscSeqUtils.CSI_ReportWindowSizeInChars.Terminator,
            ResponseReceived = HandleSizeResponse,
            Abandoned = () =>
            {
                _expectingResponse = false;
                CompleteStartupSizeQuery ();
            }
        };

        //Logging.Trace($"{request.Request}");
        _queueAnsiRequest! (request);
    }

    /// <inheritdoc/>
    public event EventHandler<SizeChangedEventArgs>? SizeChanged;

    /// <inheritdoc/>
    public bool Poll ()
    {
        if (!_queryTerminalSize)
        {
            return CheckSizeChanged ();
        }

        // Throttle queries to avoid spamming the terminal
        if (DateTime.Now - _lastQuery < _queryThrottle)
        {
            // Still check if size changed (in case response came in)
            return CheckSizeChanged ();
        }

        // Send ANSI query if we have a way to queue requests
        if (_queueAnsiRequest != null && !_expectingResponse)
        {
            SendSizeQuery ();
        }

        // Check if size changed
        return CheckSizeChanged ();
    }

    private bool CheckSizeChanged ()
    {
        Size currentSize = _output.GetSize ();

        if (currentSize == _lastSize)
        {
            return false;
        }

        //Trace.Lifecycle (nameof (AnsiSizeMonitor), "SizeChanged", $"{_lastSize} → {currentSize}");
        _lastSize = currentSize;
        SizeChanged?.Invoke (this, new SizeChangedEventArgs (currentSize));

        return true;
    }

    private void HandleSizeResponse (string? response)
    {
        //Trace.Lifecycle (nameof (AnsiSizeMonitor), "HandleSizeResponse", $"Response: '{response ?? "<null>"}'");
        _expectingResponse = false;
        _sizeResponseReceived = true;

        if (string.IsNullOrEmpty (response))
        {
            CompleteStartupSizeQuery ();

            return;
        }

        // The response is handled by ANSIOutput.HandleSizeQueryResponse
        // which updates the cached size. We just need to check if it changed.
        _output.HandleSizeQueryResponse (response);
        CompleteStartupSizeQuery ();

        // Check for size change after the response is processed
        CheckSizeChanged ();
    }

    private void SendCursorPositionQuery ()
    {
        if (_queueAnsiRequest is null)
        {
            Logging.Warning ("ANSISizeMonitor: Cannot send cursor position query - _queueAnsiRequest is null");
            _cursorPositionReceived = true;
            CompleteStartupCursorPositionQuery ();

            return;
        }

        Trace.Lifecycle (nameof (AnsiSizeMonitor), "SendCursorPositionQuery", "Queuing CSI 6n cursor position query");

        AnsiEscapeSequenceRequest request = new ()
        {
            Request = EscSeqUtils.CSI_RequestCursorPositionReport.Request,
            Terminator = EscSeqUtils.CSI_RequestCursorPositionReport.Terminator,
            ResponseReceived = HandleCursorPositionResponse,
            Abandoned = () =>
                        {
                            Trace.Lifecycle (nameof (AnsiSizeMonitor), "CursorPositionAbandoned", "Defaulting to row 0");
                            _cursorPositionReceived = true;
                            CompleteStartupCursorPositionQuery ();
                        }
        };

        _queueAnsiRequest! (request);
    }

    private void HandleCursorPositionResponse (string? response)
    {
        Trace.Lifecycle (nameof (AnsiSizeMonitor), "HandleCursorPositionResponse", $"Response: '{response ?? "<null>"}'");
        _cursorPositionReceived = true;

        if (string.IsNullOrEmpty (response))
        {
            CompleteStartupCursorPositionQuery ();

            return;
        }

        // Response format for standard CPR: ESC [ row ; col R
        Match match = Regex.Match (response, @"\[?\??(\d+);(\d+)");

        if (match.Success && int.TryParse (match.Groups [1].Value, out int row))
        {
            // Convert from 1-indexed terminal row to 0-indexed
            InitialCursorRow = Math.Max (0, row - 1);
            Trace.Lifecycle (nameof (AnsiSizeMonitor), "CursorPositionParsed", $"Row={InitialCursorRow} (1-indexed: {row})");
        }

        // Complete AFTER parsing so the gate doesn't release with InitialCursorRow still 0
        CompleteStartupCursorPositionQuery ();
    }

    private void CompleteStartupSizeQuery ()
    {
        _startupSizeQueryCompletionHandle?.Dispose ();
        _startupSizeQueryCompletionHandle = null;
    }

    private void CompleteStartupCursorPositionQuery ()
    {
        _startupCursorPositionQueryCompletionHandle?.Dispose ();
        _startupCursorPositionQueryCompletionHandle = null;
    }
}
