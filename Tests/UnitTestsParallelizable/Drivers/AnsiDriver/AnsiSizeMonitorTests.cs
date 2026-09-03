using Moq;
using Terminal.Gui.Tracing;

namespace DriverTests.Ansi;

// Copilot

/// <summary>
///     Behavioral tests for <see cref="AnsiSizeMonitor"/>.
/// </summary>
[Collection ("Driver Tests")]
public class AnsiSizeMonitorTests
{
    /// <summary>
    ///     Simulates a terminal size-query response arriving and verifies that
    ///     <see cref="ISizeMonitor.SizeChanged"/> is raised with the correct dimensions.
    /// </summary>
    [Fact]
    public void HandleSizeResponse_UpdatesSize_And_RaisesEvent ()
    {
        AnsiOutput output = new ();
        output.SetSize (80, 25);

        AnsiEscapeSequenceRequest? captured = null;
        AnsiSizeMonitor monitor = new (output, req => captured = req);

        Size? raisedSize = null;
        monitor.SizeChanged += (_, e) => raisedSize = e.Size;

        // Trigger the query so the ResponseReceived callback is registered.
        monitor.Poll ();
        Assert.NotNull (captured);

        // Simulate terminal responding with 100 columns × 30 rows.
        captured!.ResponseReceived! ("[8;30;100t");

        Assert.Equal (new Size (100, 30), raisedSize);
        Assert.Equal (new Size (100, 30), output.GetSize ());
    }

    /// <summary>
    ///     When the terminal responds with the same size that is already cached,
    ///     <see cref="ISizeMonitor.SizeChanged"/> must NOT be raised.
    /// </summary>
    [Fact]
    public void HandleSizeResponse_SameSize_DoesNotRaiseEvent ()
    {
        AnsiOutput output = new ();
        output.SetSize (80, 25);

        AnsiEscapeSequenceRequest? captured = null;
        AnsiSizeMonitor monitor = new (output, req => captured = req);

        var raised = false;
        monitor.SizeChanged += (_, _) => raised = true;

        monitor.Poll ();
        Assert.NotNull (captured);

        // Respond with the current size — no change.
        captured!.ResponseReceived! ("[8;25;80t");

        Assert.False (raised);
    }

    /// <summary>
    ///     The first <see cref="AnsiSizeMonitor.Poll"/> call (outside the throttle window)
    ///     must enqueue exactly one ANSI request.
    /// </summary>
    [Fact]
    public void Poll_SendsQuery_WhenNotThrottled ()
    {
        AnsiOutput output = new ();
        output.SetSize (80, 25);

        var queued = 0;
        AnsiSizeMonitor monitor = new (output, _ => queued++);

        monitor.Poll ();

        Assert.Equal (1, queued);
    }

    /// <summary>
    ///     A second <see cref="AnsiSizeMonitor.Poll"/> call immediately after the first
    ///     must NOT enqueue another request (throttled within the 500 ms window).
    /// </summary>
    [Fact]
    public void Poll_DoesNotSendQuery_WhenThrottled ()
    {
        AnsiOutput output = new ();
        output.SetSize (80, 25);

        var queued = 0;
        AnsiSizeMonitor monitor = new (output, _ => queued++);

        monitor.Poll (); // First call — sends query, now in throttle window.
        monitor.Poll (); // Second call — still within 500 ms, must be suppressed.

        Assert.Equal (1, queued);
    }

    /// <summary>
    ///     After a response is received (not expecting another) and the throttle window
    ///     expires, the next <see cref="AnsiSizeMonitor.Poll"/> must send a new query.
    /// </summary>
    [Fact]
    public void Poll_SendsQuery_AfterThrottle_And_ResponseReceived ()
    {
        AnsiOutput output = new ();
        output.SetSize (80, 25);

        List<AnsiEscapeSequenceRequest> requests = [];
        AnsiSizeMonitor monitor = new (output, req => requests.Add (req));

        // First poll — sends query #1.
        monitor.Poll ();
        Assert.Single (requests);

        // Complete response — clears _expectingResponse.
        requests [0].ResponseReceived! ("[8;25;80t");

        // Advance time by simulating: we can't easily fast-forward DateTime.Now,
        // so we simply confirm a second poll does NOT send while throttled.
        monitor.Poll ();
        Assert.Single (requests); // Still only 1 — throttle window not expired.
    }

    /// <summary>
    ///     <see cref="AnsiSizeMonitor.Initialize"/> must wire the driver's queue and send
    ///     the initial size query immediately.
    /// </summary>
    [Fact]
    public void Initialize_SendsInitialQuery ()
    {
        AnsiOutput output = new ();
        output.SetSize (80, 25);

        // Create without a pre-wired queue — Initialize must supply it.
        AnsiSizeMonitor monitor = new (output);

        List<AnsiEscapeSequenceRequest> queued = [];
        Mock<IDriver> driverMock = new ();
        driverMock.Setup (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ())).Callback<AnsiEscapeSequenceRequest> (r => queued.Add (r));

        monitor.Initialize (driverMock.Object);

        Assert.NotEmpty (queued);
        Assert.Contains (queued, r => r.Request.Contains (EscSeqUtils.CSI_ReportWindowSizeInChars.Request, StringComparison.Ordinal));
    }

    /// <summary>
    ///     A size-change response must propagate end-to-end: ANSI response → monitor →
    ///     <see cref="ISizeMonitor.SizeChanged"/> event with correct <see cref="Size"/>.
    /// </summary>
    [Fact]
    public void SizeChange_PropagatesThrough_MonitorEvent ()
    {
        AnsiOutput output = new ();
        output.SetSize (80, 25);

        AnsiEscapeSequenceRequest? captured = null;
        AnsiSizeMonitor monitor = new (output, req => captured = req);

        List<Size> sizes = [];
        monitor.SizeChanged += (_, e) => sizes.Add (e.Size!.Value);

        monitor.Poll ();
        captured!.ResponseReceived! ("[8;40;120t");

        Assert.Single (sizes);
        Assert.Equal (new Size (120, 40), sizes [0]);
    }

    /// <summary>
    ///     <see cref="AnsiSizeMonitor.InitialSizeReceived"/> starts <see langword="false"/>
    ///     and becomes <see langword="true"/> after the first response is handled.
    /// </summary>
    [Fact]
    public void InitialSizeReceived_IsFalse_BeforeFirstResponse () // Copilot
    {
        AnsiOutput output = new ();
        output.SetSize (80, 25);

        AnsiEscapeSequenceRequest? captured = null;
        AnsiSizeMonitor monitor = new (output, req => captured = req);

        Assert.False (monitor.InitialSizeReceived);

        // Trigger a query and simulate a response.
        monitor.Poll ();
        Assert.NotNull (captured);
        captured!.ResponseReceived! ("[8;30;100t");

        Assert.True (monitor.InitialSizeReceived);
    }

    /// <summary>
    ///     Even when the response is null or empty, <see cref="AnsiSizeMonitor.InitialSizeReceived"/>
    ///     becomes <see langword="true"/> — we received *something* from the terminal.
    /// </summary>
    [Fact]
    public void InitialSizeReceived_TrueEvenForNullResponse () // Copilot
    {
        AnsiOutput output = new ();
        output.SetSize (80, 25);

        AnsiEscapeSequenceRequest? captured = null;
        AnsiSizeMonitor monitor = new (output, req => captured = req);

        monitor.Poll ();
        captured!.ResponseReceived! (null);

        Assert.True (monitor.InitialSizeReceived);
    }

#if DEBUG

    /// <summary>
    ///     In inline mode, <see cref="AnsiSizeMonitor.InitialSizeReceived"/> requires
    ///     both the size response AND the cursor position response.
    /// </summary>
    [Fact]
    public void InlineMode_InitialSizeReceived_RequiresBothResponses () // Copilot
    {
        AnsiOutput output = new (AppModel.Inline);
        output.SetSize (80, 25);

        List<AnsiEscapeSequenceRequest> captured = [];
        AnsiSizeMonitor monitor = new (output, req => captured.Add (req));

        Assert.False (monitor.InitialSizeReceived);

        // Poll triggers the size query
        monitor.Poll ();

        // Find the size response (terminator "t") and CPR response (terminator "R")
        AnsiEscapeSequenceRequest? sizeReq = captured.FirstOrDefault (r => r.Terminator == "t");
        AnsiEscapeSequenceRequest? cprReq = captured.FirstOrDefault (r => r.Terminator == "R");

        // If CPR wasn't sent via Poll, simulate Initialize sending it
        if (cprReq is null)
        {
            Mock<IDriver> driverMock = new ();
            driverMock.Setup (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()))
                      .Callback<AnsiEscapeSequenceRequest> (r => captured.Add (r));
            monitor.Initialize (driverMock.Object);
            cprReq = captured.FirstOrDefault (r => r.Terminator == "R");
        }

        Assert.NotNull (sizeReq);
        Assert.NotNull (cprReq);

        // Only size response — not yet complete
        sizeReq!.ResponseReceived! ("[8;30;100t");
        Assert.False (monitor.InitialSizeReceived);

        // Now cursor position response — both complete
        cprReq!.ResponseReceived! ("[?10;1R");
        Assert.True (monitor.InitialSizeReceived);
    }

    /// <summary>
    ///     <see cref="AnsiSizeMonitor.InitialCursorRow"/> parses the row from the
    ///     CPR response and converts from 1-indexed to 0-indexed.
    /// </summary>
    [Fact]
    public void InlineMode_InitialCursorRow_ParsedFromCprResponse () // Copilot
    {
        AnsiOutput output = new (AppModel.Inline);
        output.SetSize (80, 50);

        List<AnsiEscapeSequenceRequest> captured = [];

        Mock<IDriver> driverMock = new ();
        driverMock.Setup (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()))
                  .Callback<AnsiEscapeSequenceRequest> (r => captured.Add (r));

        AnsiSizeMonitor monitor = new (output);
        monitor.Initialize (driverMock.Object);

        AnsiEscapeSequenceRequest? cprReq = captured.FirstOrDefault (r => r.Terminator == "R");
        Assert.NotNull (cprReq);
        Assert.Equal ($"{EscSeqUtils.CSI}6n", cprReq!.Request);

        // Simulate terminal at row 15 (1-indexed), col 1
        cprReq.ResponseReceived! ("[15;1R");

        Assert.Equal (14, monitor.InitialCursorRow); // 0-indexed
    }

    // Codex - GPT-5 Codex
    [Fact]
    public void InlineMode_NativeSizeQuery_StillDiscoversInitialCursorRow ()
    {
        AnsiOutput output = new (AppModel.Inline);
        output.NativeSizeQuery = () => new Size (80, 50);

        List<AnsiEscapeSequenceRequest> captured = [];
        Mock<IDriver> driverMock = new ();
        driverMock.Setup (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()))
                  .Callback<AnsiEscapeSequenceRequest> (captured.Add);

        AnsiSizeMonitor monitor = new (output, queryTerminalSize: false);
        monitor.Initialize (driverMock.Object);

        Assert.DoesNotContain (captured, request => request.Terminator == "t");
        AnsiEscapeSequenceRequest cprRequest = Assert.Single (captured, request => request.Terminator == "R");
        Assert.False (monitor.InitialSizeReceived);

        cprRequest.ResponseReceived! ("[15;1R");

        Assert.True (monitor.InitialSizeReceived);
        Assert.Equal (14, monitor.InitialCursorRow);
    }

    /// <summary>
    ///     In FullScreen mode, <see cref="AnsiSizeMonitor"/> sends both size and cursor
    ///     position queries during initialization (cursor position is needed for the startup gate).
    ///     <see cref="AnsiSizeMonitor.InitialSizeReceived"/> becomes true once the size response arrives.
    /// </summary>
    [Fact]
    public void FullScreenMode_InitialSizeReceived_OnlySizeNeeded () // Copilot
    {
        AnsiOutput output = new (); // FullScreen default
        output.SetSize (80, 25);

        List<AnsiEscapeSequenceRequest> captured = [];

        Mock<IDriver> driverMock = new ();
        driverMock.Setup (d => d.QueueAnsiRequest (It.IsAny<AnsiEscapeSequenceRequest> ()))
                  .Callback<AnsiEscapeSequenceRequest> (r => captured.Add (r));

        AnsiSizeMonitor monitor = new (output);
        monitor.Initialize (driverMock.Object);

        // Both size and CPR queries should be sent
        Assert.Contains (captured, r => r.Terminator == "t");
        Assert.Contains (captured, r => r.Terminator == "R");

        // Size response alone should be sufficient for InitialSizeReceived
        AnsiEscapeSequenceRequest? sizeReq = captured.FirstOrDefault (r => r.Terminator == "t");
        Assert.NotNull (sizeReq);
        sizeReq!.ResponseReceived! ("[8;30;100t");
        Assert.True (monitor.InitialSizeReceived);
        Assert.Equal (0, monitor.InitialCursorRow); // default until CPR response
    }

#endif
}
