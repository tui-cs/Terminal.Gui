using System.Collections.Concurrent;
using System.Diagnostics;
using Trace = Terminal.Gui.Tracing.Trace;

namespace Terminal.Gui.App;

/// <summary>
///     The main application loop that runs Terminal.Gui's UI rendering and event processing.
/// </summary>
/// <remarks>
///     This class coordinates the Terminal.Gui application lifecycle by:
///     <list type="bullet">
///         <item>Processing buffered input events and translating them to UI events</item>
///         <item>Executing user timeout callbacks at scheduled intervals</item>
///         <item>Detecting which views need redrawing or layout updates</item>
///         <item>Rendering UI changes to the console output buffer</item>
///         <item>Managing cursor position and visibility</item>
///         <item>Throttling iterations to respect <see cref="Application.MaximumIterationsPerSecond"/></item>
///     </list>
/// </remarks>
/// <typeparam name="TInputRecord">Type of raw input events, e.g. <see cref="ConsoleKeyInfo"/> for .NET driver</typeparam>
public class ApplicationMainLoop<TInputRecord> : IApplicationMainLoop<TInputRecord> where TInputRecord : struct
{
    /// <inheritdoc/>
    public IApplication? App { get; private set; }

    /// <inheritdoc/>
    public ITimedEvents TimedEvents { get => field ?? throw new NotInitializedException (nameof (TimedEvents)); private set; }

    // TODO: follow above pattern for others too

    /// <summary>
    ///     The input events thread-safe collection. This is populated on separate
    ///     thread by a <see cref="IInput{T}"/>. Is drained as part of each
    ///     <see cref="Iteration"/> on the main loop thread.
    /// </summary>
    public ConcurrentQueue<TInputRecord> InputQueue { get => field ?? throw new NotInitializedException (nameof (InputQueue)); private set; }

    /// <inheritdoc/>
    public IInputProcessor InputProcessor { get => field ?? throw new NotInitializedException (nameof (InputProcessor)); private set; }

    /// <inheritdoc/>
    public IOutputBuffer OutputBuffer { get; } = new OutputBufferImpl ();

    /// <inheritdoc/>
    public IOutput Output { get => field ?? throw new NotInitializedException (nameof (Output)); private set; }

    /// <inheritdoc/>
    public AnsiRequestScheduler AnsiRequestScheduler { get => field ?? throw new NotInitializedException (nameof (AnsiRequestScheduler)); private set; }

    /// <inheritdoc/>
    public ISizeMonitor SizeMonitor { get => field ?? throw new NotInitializedException (nameof (SizeMonitor)); private set; }

    /// <summary>
    ///     Initializes the class with the provided subcomponents
    /// </summary>
    /// <param name="timedEvents"></param>
    /// <param name="inputBuffer"></param>
    /// <param name="inputProcessor"></param>
    /// <param name="consoleOutput"></param>
    /// <param name="componentFactory"></param>
    /// <param name="app"></param>
    public void Initialize (ITimedEvents timedEvents,
                            ConcurrentQueue<TInputRecord> inputBuffer,
                            IInputProcessor inputProcessor,
                            IOutput consoleOutput,
                            IComponentFactory<TInputRecord> componentFactory,
                            IApplication? app)
    {
        App = app;
        InputQueue = inputBuffer;
        Output = consoleOutput;
        InputProcessor = inputProcessor;

        TimedEvents = timedEvents;
        AnsiRequestScheduler = new AnsiRequestScheduler (InputProcessor.GetParser ());

        // In inline mode, cells must start clean so the first render only flushes
        // cells that views explicitly draw, leaving the rest of the terminal untouched.
        if (App?.AppModel == AppModel.Inline && OutputBuffer is OutputBufferImpl outputBufferImpl)
        {
            outputBufferImpl.InlineMode = true;
        }

        OutputBuffer.SetSize (consoleOutput.GetSize ().Width, consoleOutput.GetSize ().Height);
        SizeMonitor = componentFactory.CreateSizeMonitor (Output, OutputBuffer);
    }

    /// <inheritdoc/>
    public void Iteration ()
    {
        App?.RaiseIteration ();

        DateTime dt = DateTime.Now;
        int timeAllowed = 1000 / Math.Max (1, (int)Application.MaximumIterationsPerSecond);

        IterationImpl ();

        TimeSpan took = DateTime.Now - dt;
        TimeSpan sleepFor = TimeSpan.FromMilliseconds (timeAllowed) - took;

        Logging.TotalIterationMetric.Record (took.Milliseconds);

        if (sleepFor.Milliseconds > 0)
        {
            Task.Delay (sleepFor).Wait ();
        }
    }

    private bool _startupWaitLogged;
    private bool _firstLayoutAndDrawComplete;

    internal void IterationImpl ()
    {
        // Pull any input events from the input queue and process them
        InputProcessor.ProcessQueue ();

        // Run any queued ANSI requests that previously could not be sent
        // (e.g. throttled duplicate request sent too soon after an earlier one).
        AnsiRequestScheduler.RunSchedule (App?.Driver);

        // Check for any size changes; this will cause SizeChanged events
        SizeMonitor.Poll ();

        IDriver? driver = App?.Driver;

        // First-render deferral: wait for the ANSI startup gate unless bypassed.
        if (!_firstLayoutAndDrawComplete
            && driver?.AnsiStartupGate is { IsReady: false } startupGate)
        {
            // ForceInlinePosition bypasses the gate for inline mode testing.
            if (App?.AppModel == AppModel.Inline && App?.ForceInlinePosition.HasValue == true)
            {
                driver.InlinePosition = new Point (App.ForceInlinePosition!.Value.X, App.ForceInlinePosition!.Value.Y);

                Trace.Lifecycle (nameof (ApplicationMainLoop<TInputRecord>), "InlineSizeForced", $"Position={App?.ForceInlinePosition}");
            }
            else
            {
                if (!_startupWaitLogged)
                {
                    string pending = string.Join (", ", startupGate.PendingQueries);

                    Trace.Lifecycle (nameof (ApplicationMainLoop<TInputRecord>),
                                     nameof (IterationImpl),
                                     $"Deferring first render until ANSI startup queries complete. Pending: {pending}");

                    _startupWaitLogged = true;
                }

                var swStartupCallbacks = Stopwatch.StartNew ();
                TimedEvents.RunTimers ();
                (App as ApplicationImpl)?.DrainDispatches ();
                Logging.IterationInvokesAndTimeouts.Record (swStartupCallbacks.Elapsed.Milliseconds);

                return;
            }
        }

        // Startup gate just became ready — set up inline state if needed.
        if (!_firstLayoutAndDrawComplete)
        {
            if (_startupWaitLogged)
            {
                Trace.Lifecycle (nameof (ApplicationMainLoop<TInputRecord>),
                                 nameof (IterationImpl),
                                 "ANSI startup gate ready; rendering can begin");
            }

            // For inline mode, transfer the cursor row from the CPR response to InlinePosition.
            if (App?.AppModel == AppModel.Inline && driver is { } && App?.ForceInlinePosition.HasValue != true)
            {
                driver.InlinePosition = new Point (0, SizeMonitor.InitialCursorRow);

                Trace.Lifecycle (nameof (ApplicationMainLoop<TInputRecord>), "InlineSizeConfirmed", $"CursorRow={SizeMonitor.InitialCursorRow}");
            }
        }

        Trace.Draw (nameof (ApplicationMainLoop<TInputRecord>), "IterationDraw", $"Screen={App?.Screen}");

        // Layout and draw any views that need it
        // This will raise IApplication.LayoutAndDrawComplete
        App?.LayoutAndDraw (false);
        _firstLayoutAndDrawComplete = true;

        // Update the cursor
        App?.Navigation?.UpdateCursor ();

        var swCallbacks = Stopwatch.StartNew ();

        // Run any timeout callbacks that are due
        TimedEvents.RunTimers ();
        (App as ApplicationImpl)?.DrainDispatches ();

        Logging.IterationInvokesAndTimeouts.Record (swCallbacks.Elapsed.Milliseconds);
    }

    /// <inheritdoc/>
    public void Dispose ()
    {
        // TODO release managed resources here
    }
}
