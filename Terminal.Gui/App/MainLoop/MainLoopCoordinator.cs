using System.Collections.Concurrent;
using Terminal.Gui.Tracing;

namespace Terminal.Gui.App;

/// <summary>
///     <para>
///         Coordinates the creation and startup of the main UI loop and input thread.
///     </para>
///     <para>
///         This class bootstraps the <see cref="ApplicationMainLoop{T}"/> that handles
///         UI layout, drawing, and event processing while also managing a separate thread
///         for reading console input asynchronously.
///     </para>
///     <para>This class is designed to be managed by <see cref="ApplicationImpl"/></para>
/// </summary>
/// <typeparam name="TInputRecord">Type of raw input events, e.g. <see cref="ConsoleKeyInfo"/> for .NET driver</typeparam>
internal class MainLoopCoordinator<TInputRecord> : IMainLoopCoordinator where TInputRecord : struct
{
    /// <summary>
    ///     Creates a new coordinator that will manage the main UI loop and input thread.
    /// </summary>
    /// <param name="timedEvents">Handles scheduling and execution of user timeout callbacks</param>
    /// <param name="inputQueue">Thread-safe queue for buffering raw console input</param>
    /// <param name="loop">The main application loop instance</param>
    /// <param name="componentFactory">Factory for creating driver-specific components (input, output, etc.)</param>
    /// <param name="timeProvider">Time provider for timestamps and timing control.</param>
    public MainLoopCoordinator (ITimedEvents timedEvents,
                                ConcurrentQueue<TInputRecord> inputQueue,
                                IApplicationMainLoop<TInputRecord> loop,
                                IComponentFactory<TInputRecord> componentFactory,
                                ITimeProvider? timeProvider = null)
    {
        _timedEvents = timedEvents;
        _inputQueue = inputQueue;
        _inputProcessor = componentFactory.CreateInputProcessor (_inputQueue, timeProvider);
        _loop = loop;
        _componentFactory = componentFactory;
    }

    private readonly IApplicationMainLoop<TInputRecord> _loop;
    private readonly IComponentFactory<TInputRecord> _componentFactory;
    private readonly CancellationTokenSource _runCancellationTokenSource = new ();
    private readonly ConcurrentQueue<TInputRecord> _inputQueue;
    private readonly IInputProcessor _inputProcessor;
    private readonly Lock _oLockInitialization = new ();
    private readonly ITimedEvents _timedEvents;

    private readonly SemaphoreSlim _startupSemaphore = new (0, 1);
    private IInput<TInputRecord>? _input;
    private Task? _inputTask;
    private IOutput? _output;
    private DriverImpl? _driver;

    private bool _stopCalled;

    /// <summary>
    ///     Starts the input loop thread in separate task (returning immediately).
    /// </summary>
    /// <param name="app">The <see cref="IApplication"/> instance that is running the input loop.</param>
    public async Task StartInputTaskAsync (IApplication? app)
    {
        Trace.Lifecycle (app?.MainThreadId.ToString (), "Init", "Booting...");

        _inputTask = Task.Run (() => RunInput (app));

        // Main loop is now booted on same thread as rest of users application
        BootMainLoop (app);

        // Wait asynchronously for the semaphore or task failure.
        Task waitForSemaphore = _startupSemaphore.WaitAsync ();

        // Wait for either the semaphore to be released or the input task to crash.
        Task completedTask = await Task.WhenAny (waitForSemaphore, _inputTask).ConfigureAwait (false);

        // Check if the task was the input task and if it has failed.
        if (completedTask == _inputTask)
        {
            if (_inputTask.IsFaulted)
            {
                throw _inputTask.Exception;
            }

            Logging.Critical ($"app: {app?.MainThreadId} Input loop exited during startup instead of entering read loop properly (i.e. and blocking)");
        }

        Trace.Lifecycle (app?.MainThreadId.ToString (), "Init", "Booting complete");
    }

    /// <inheritdoc/>
    public void RunIteration ()
    {
        lock (_oLockInitialization)
        {
            _loop.Iteration ();
        }
    }

    /// <inheritdoc/>
    public void Stop ()
    {
        // Ignore repeated calls to Stop - happens if user spams Application.Shutdown().
        if (_stopCalled)
        {
            return;
        }

        _stopCalled = true;

        // Restore terminal kitty keyboard mode before shutting down output resources.

        try
        {
            if (_driver is { KittyKeyboardCapabilities.IsSupported: true })
            {
                KittyKeyboardProtocolDetector kittyKeyboardDetector = new (_driver);
                kittyKeyboardDetector.Disable ();
            }
        }
        catch (Exception ex)
        {
            Logging.Warning ($"Kitty keyboard protocol disable failed: {ex.Message}");
        }

        _runCancellationTokenSource.Cancel ();
        _output?.Dispose ();

        // Wait for input infinite loop to exit
        _inputTask?.Wait ();
    }

    private void BootMainLoop (IApplication? app)
    {
        lock (_oLockInitialization)
        {
            // Instance must be constructed on the thread in which it is used.
            _output = _componentFactory.CreateOutput ();
            _loop.Initialize (_timedEvents, _inputQueue, _inputProcessor, _output, _componentFactory, app);

            BuildDriverIfPossible (app);
        }
    }

    private void BuildDriverIfPossible (IApplication? app)
    {
        if (_input == null || _output == null)
        {
            return;
        }
        AnsiStartupGate? startupGate = app?.AnsiStartupGate;

        _driver = new DriverImpl (_componentFactory, _inputProcessor, _loop.OutputBuffer, _output, _loop.AnsiRequestScheduler, _loop.SizeMonitor, startupGate);

        // Set the driver's AppModel from the application instance so it doesn't rely on the static.
        if (app is { })
        {
            _driver.AppModel = app.AppModel;
        }

        // Wire up the inline screen callback so AnsiOutput can read the row offset
        // and dimensions from App.Screen for cursor positioning and exit cleanup.
        if (_output is AnsiOutput ansiOutput && app is { })
        {
            IApplication appRef = app;
            ansiOutput.AppScreenGetter = () => appRef.Screen;
        }

        // Initialize the size monitor now that the driver is fully constructed
        // This allows size monitors to set up platform-specific mechanisms:
        // - ANSI queries (ANSIDriver)
        // - Signal handlers (UnixDriver)
        // - Console events (WindowsDriver)
        _loop.SizeMonitor.Initialize (_driver);

        app?.Driver = _driver;

        // Detect terminal color capabilities from environment variables
        TerminalColorCapabilities caps = TerminalEnvironmentDetector.DetectColorCapabilities ();

        _driver.SetColorCapabilities (caps);
        _driver.InitializeProgressIndicator ();

        if (caps.Capability is ColorCapabilityLevel.NoColor or ColorCapabilityLevel.Colors16)
        {
            Driver.Force16Colors = true;
        }

        // Detect the terminal's actual default colors via OSC 10/11 queries.
        // Skip if color capabilities indicate a terminal that won't support OSC.
        if (_driver.ColorCapabilities is { Capability: ColorCapabilityLevel.Colors256 or ColorCapabilityLevel.TrueColor })
        {
            try
            {
                TerminalColorDetector colorDetector = new (_driver, startupGate);

                colorDetector.Detect ((fg, bg) =>
                                      {
                                          if (fg is null && bg is null)
                                          {
                                              return;
                                          }

                                          Attribute attribute = new (fg ?? new Color (255, 255, 255), bg ?? new Color (0, 0));
                                          Logging.Trace ($"app: SetDefaultAttribute ({attribute})");

                                          _driver.SetDefaultAttribute (attribute);
                                      });
            }
            catch (Exception ex)
            {
                Logging.Warning ($"Terminal color detection failed: {ex.Message}");
            }
        }

        try
        {
            // Detect Kitty support. The async response we get back only indicates whether
            // kitty is supported or not.
            KittyKeyboardProtocolDetector kittyKeyboardDetector = new (_driver, startupGate);

            kittyKeyboardDetector.Detect (result =>
                                          {
                                              if (!result.IsSupported)
                                              {
                                                  if (_inputProcessor is AnsiInputProcessor unsupportedAnsiInputProcessor)
                                                  {
                                                      unsupportedAnsiInputProcessor.SetKittyKeyboardEnabled (false);
                                                  }

                                                  Trace.Lifecycle (app?.MainThreadId?.ToString (), "KittyKeyboard", "Kitty keyboard mode not enabled");

                                                  return;
                                              }

                                              // Kitty is supported. Store the capabilities and set the flags we care about.
                                              _driver?.SetKittyKeyboardCapabilities (result);

                                              if (_inputProcessor is AnsiInputProcessor supportedAnsiInputProcessor)
                                              {
                                                  supportedAnsiInputProcessor.SetKittyKeyboardEnabled (true);
                                              }

                                              kittyKeyboardDetector.Enable (EscSeqUtils.KittyKeyboardRequestedFlags);

                                              Trace.Lifecycle (app?.MainThreadId?.ToString (),
                                                               "KittyKeyboard",
                                                               $"Requested kitty keyboard flags {EscSeqUtils.KittyKeyboardRequestedFlags}; awaiting confirmation");
                                          });
        }
        catch (Exception ex)
        {
            Logging.Warning ($"Kitty keyboard protocol detection failed: {ex.Message}");
        }

        if (startupGate is { })
        {
            QueueDeviceAttributesProbe (startupGate);
        }

        // Detect sixel support via DAR query.
        // Follows the same async callback pattern as TerminalColorDetector above.
        if (!_driver.IsLegacyConsole)
        {
            try
            {
                SixelSupportDetector sixelDetector = new (_driver);

                sixelDetector.Detect (result =>
                                      {
                                          _driver.SetSixelSupport (result);
                                          Logging.Trace ($"app: Sixel support: {result.IsSupported}, Resolution: {result.Resolution}");
                                      });
            }
            catch (Exception ex)
            {
                Logging.Warning ($"Sixel support detection failed: {ex.Message}");
            }

            try
            {
                KittyGraphicsSupportDetector kittyDetector = new (_driver);

                kittyDetector.Detect (result =>
                                      {
                                          _driver.SetKittyGraphicsSupport (result);
                                          Logging.Trace ($"app: Kitty graphics support: {result.IsSupported}, Resolution: {result.Resolution}");
                                      });
            }
            catch (Exception ex)
            {
                Logging.Warning ($"Kitty graphics support detection failed: {ex.Message}");
            }
        }


        _startupSemaphore.Release ();
        Trace.Lifecycle (app?.MainThreadId.ToString (), "Driver", $"_input: {_input}, _output: {_output}");
    }

    private void QueueDeviceAttributesProbe (IAnsiStartupGate startupGate)
    {
        IDisposable deviceAttributesQueryCompletionHandle = startupGate.RegisterQuery (AnsiStartupQuery.DeviceAttributesPrimary,
                                                                                       TimeSpan.FromSeconds (1));

        AnsiEscapeSequenceRequest request = new ()
        {
            Request = EscSeqUtils.CSI_SendDeviceAttributes.Request,
            Value = EscSeqUtils.CSI_SendDeviceAttributes.Value,
            Terminator = EscSeqUtils.CSI_SendDeviceAttributes.Terminator,
            ResponseReceived = _ => deviceAttributesQueryCompletionHandle.Dispose (),
            Abandoned = deviceAttributesQueryCompletionHandle.Dispose
        };

        _driver?.QueueAnsiRequest (request);
    }

    /// <summary>
    ///     INTERNAL: Runs the IInput read loop on a new thread called the "Input Thread".
    /// </summary>
    /// <param name="app"></param>
    private void RunInput (IApplication? app)
    {
        try
        {
            lock (_oLockInitialization)
            {
                // Instance must be constructed on the thread in which it is used.
                _input = _componentFactory.CreateInput ();
                _input.Initialize (_inputQueue);

                // Wire up InputImpl reference for ITestableInput support
                if (_inputProcessor is InputProcessorImpl<TInputRecord> impl)
                {
                    impl.InputImpl = _input;
                }

                BuildDriverIfPossible (app);
            }

            try
            {
                _input.Run (_runCancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                Trace.Lifecycle (app?.MainThreadId.ToString (), "Init", $"{app?.MainThreadId}Input loop canceled");
            }

            _input.Dispose ();
        }
        catch (Exception e)
        {
            Logging.Critical ($"app: {app?.MainThreadId} Input loop crashed: {e}");

            throw;
        }

        if (_stopCalled)
        {
            Trace.Lifecycle (app?.MainThreadId.ToString (), "Init", "Input loop exited cleanly");
        }
        else
        {
            Logging.Critical ($"app: {app?.MainThreadId}Input loop exited early (stop not called)");
        }
    }
}
