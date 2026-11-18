namespace Terminal.Gui.Testing;

/// <summary>
///     Provides an API for injecting mouse and keyboard input into Terminal.Gui applications.
/// </summary>
/// <remarks>
///     <para>
///         <c>InputInjector</c> offers two injection modes to support different testing scenarios:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 <b>Direct Mode</b> (default): Bypasses ANSI encoding/decoding and directly raises events.
///                 Ideal for fast, deterministic tests that don't need to verify encoding/parsing behavior.
///                 Use this mode when you want precise control over timestamps and event timing.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <b>Pipeline Mode</b>: Routes input through the complete processing pipeline including
///                 ANSI encoding/decoding. Use this mode when testing driver-level behavior or verifying
///                 that escape sequences are correctly encoded and parsed.
///             </description>
///         </item>
///     </list>
///     <para>
///         For convenience, use the extension methods in <see cref="InputInjectionExtensions"/>:
///         <see cref="InputInjectionExtensions.InjectKey"/>, <see cref="InputInjectionExtensions.InjectMouse"/>,
///         and <see cref="InputInjectionExtensions.InjectSequence"/> to inject input without directly
///         accessing the injector.
///     </para>
///     <para>
///         When you need to specify injection options (such as Direct mode with timestamps), use
///         <see cref="IApplication.GetInputInjector"/> to access the injector directly.
///     </para>
/// </remarks>
/// <example>
///     <b>Example 1: Basic key injection (using extension method)</b>
///     <code>
///     using IApplication app = Application.Create();
///     app.Init(DriverRegistry.Names.ANSI);
///     
///     app.Keyboard.KeyDown += (s, e) =&gt; {
///         Console.WriteLine($"Key pressed: {e}");
///     };
///     
///     // Simple injection using extension method
///     app.InjectKey(Key.A);
///     app.InjectKey(Key.Enter);
///     </code>
///     <b>Example 2: Mouse injection with Direct mode (preserving timestamps)</b>
///     <code>
///     using IApplication app = Application.Create();
///     app.Init(DriverRegistry.Names.ANSI);
///     
///     IInputInjector injector = app.GetInputInjector();
///     InputInjectionOptions options = new() { Mode = InputInjectionMode.Direct };
///     
///     DateTime baseTime = new(2025, 1, 1, 12, 0, 0);
///     
///     // First click
///     injector.InjectMouse(new() {
///         ScreenPosition = new(10, 10),
///         Flags = MouseFlags.LeftButtonPressed,
///         Timestamp = baseTime
///     }, options);
///     
///     injector.InjectMouse(new() {
///         ScreenPosition = new(10, 10),
///         Flags = MouseFlags.LeftButtonReleased,
///         Timestamp = baseTime.AddMilliseconds(50)
///     }, options);
///     
///     // Second click 600ms later (prevents double-click detection)
///     injector.InjectMouse(new() {
///         ScreenPosition = new(10, 10),
///         Flags = MouseFlags.LeftButtonPressed,
///         Timestamp = baseTime.AddMilliseconds(600)
///     }, options);
///     
///     injector.InjectMouse(new() {
///         ScreenPosition = new(10, 10),
///         Flags = MouseFlags.LeftButtonReleased,
///         Timestamp = baseTime.AddMilliseconds(650)
///     }, options);
///     </code>
///     <b>Example 3: Event sequence with virtual time</b>
///     <code>
///     VirtualTimeProvider timeProvider = new();
///     using IApplication app = Application.Create(timeProvider);
///     app.Init(DriverRegistry.Names.ANSI);
///     
///     // Inject sequence with delays (virtual time advances instantly)
///     InputEvent[] sequence = [
///         new KeyInjectionEvent(Key.H),
///         new KeyInjectionEvent(Key.E) { Delay = TimeSpan.FromMilliseconds(100) },
///         new KeyInjectionEvent(Key.L) { Delay = TimeSpan.FromMilliseconds(100) },
///         new KeyInjectionEvent(Key.L) { Delay = TimeSpan.FromMilliseconds(100) },
///         new KeyInjectionEvent(Key.O)
///     ];
///     
///     app.InjectSequence(sequence);
///     // Virtual time has advanced by 300ms, but test executes instantly
///     </code>
///     <b>Example 4: Testing ANSI encoding with Pipeline mode</b>
///     <code>
///     using IApplication app = Application.Create();
///     app.Init(DriverRegistry.Names.ANSI);
///     
///     IInputInjector injector = app.GetInputInjector();
///     
///     // Use Pipeline mode to test encoding/decoding
///     InputInjectionOptions options = new() { Mode = InputInjectionMode.Pipeline };
///     
///     // Key will go through: Key → ANSI sequence → Parser → Key event
///     injector.InjectKey(Key.F1, options);
///     injector.InjectKey(Key.CursorUp.WithCtrl, options);
///     </code>
/// </example>
/// <seealso cref="InputInjectionExtensions"/>
public class InputInjector : IInputInjector
{
    private readonly IInputProcessor _processor;
    private readonly ITimeProvider _timeProvider;
    private readonly TestInputSource? _testSource;

    /// <summary>
    ///     Initializes a new instance of the <see cref="InputInjector"/> class.
    /// </summary>
    /// <param name="processor">The input processor to inject events into.</param>
    /// <param name="timeProvider">The time provider for timestamps.</param>
    /// <param name="testSource">Optional test input source for queue-based injection.</param>
    public InputInjector (IInputProcessor processor, ITimeProvider timeProvider, TestInputSource? testSource = null)
    {
        _processor = processor;
        _timeProvider = timeProvider;
        _testSource = testSource;
    }

    /// <inheritdoc/>
    public void InjectKey (Key key, InputInjectionOptions? options = null)
    {
        options ??= new InputInjectionOptions { TimeProvider = _timeProvider };
        InputInjectionMode mode = ResolveMode (options.Mode);

        if (mode == InputInjectionMode.Direct)
        {
            // Direct injection - bypass encoding, raise event directly
            _processor.RaiseKeyDownEvent (key);
        }
        else // Pipeline
        {
            // Pipeline injection - use inject to go through processing pipeline
            _processor.InjectKeyDownEvent (key);

            if (_testSource != null)
            {
                // If we have a test source, enqueue the record for full pipeline processing
                KeyboardEventRecord record = new (key) { Timestamp = _timeProvider.Now };
                _testSource.Enqueue (record);
            }
        }

        if (options.AutoProcess)
        {
            ProcessQueue ();
        }
    }

    /// <inheritdoc/>
    public void InjectMouse (Mouse mouseEvent, InputInjectionOptions? options = null)
    {
        options ??= new InputInjectionOptions { TimeProvider = _timeProvider };
        InputInjectionMode mode = ResolveMode (options.Mode);

        // Set timestamp if not provided
        mouseEvent.Timestamp ??= _timeProvider.Now;

        if (mode == InputInjectionMode.Direct)
        {
            // Direct injection - bypass encoding, raise event directly
            // Note: RaiseMouseEventParsed internally calls RaiseSyntheticMouseEvent,
            // so we don't need to call it separately
            _processor.RaiseMouseEventParsed (mouseEvent);
        }
        else // Pipeline
        {
            // Pipeline injection - use inject to go through processing pipeline
            _processor.InjectMouseEvent (null, mouseEvent);

            if (_testSource != null)
            {
                // If we have a test source, enqueue the record for full pipeline processing
                MouseEventRecord record = new (mouseEvent) { Timestamp = _timeProvider.Now };
                _testSource.Enqueue (record);
            }
        }

        if (options.AutoProcess)
        {
            ProcessQueue ();
        }
    }

    /// <inheritdoc/>
    public void InjectSequence (IEnumerable<InputInjectionEvent> events, InputInjectionOptions? options = null)
    {
        options ??= new InputInjectionOptions { TimeProvider = _timeProvider };

        foreach (InputInjectionEvent evt in events)
        {
            // Advance time if delay specified
            if (evt.Delay.HasValue && _timeProvider is VirtualTimeProvider vtp)
            {
                vtp.Advance (evt.Delay.Value);
            }

            InputInjectionOptions eventOptions = new () { Mode = options.Mode, AutoProcess = false, TimeProvider = options.TimeProvider };

            switch (evt)
            {
                case KeyInjectionEvent ke:
                    InjectKey (ke.Key, eventOptions);

                    break;

                case MouseInjectionEvent me:
                    InjectMouse (me.Mouse, eventOptions);

                    break;
            }
        }

        if (options.AutoProcess)
        {
            ProcessQueue ();
        }
    }

    /// <inheritdoc/>
    public void ProcessQueue () => _processor.ProcessQueue ();

    /// <summary>
    ///     Resolves the injection mode, converting <see cref="InputInjectionMode.Auto"/> to a concrete mode.
    /// </summary>
    /// <param name="mode">The requested injection mode.</param>
    /// <returns>
    ///     The resolved mode. <see cref="InputInjectionMode.Auto"/> is resolved to
    ///     <see cref="InputInjectionMode.Direct"/> for faster, simpler tests.
    /// </returns>
    /// <remarks>
    ///     Auto mode defaults to Direct for optimal test performance. Use Pipeline mode explicitly
    ///     when you need to test ANSI encoding/decoding behavior.
    /// </remarks>
    private InputInjectionMode ResolveMode (InputInjectionMode mode)
    {
        if (mode != InputInjectionMode.Auto)
        {
            return mode;
        }

        // Auto mode: Default to Direct for faster, simpler tests.
        // Use Pipeline explicitly when you need to test encoding/parsing.
        return InputInjectionMode.Direct;
    }
}
