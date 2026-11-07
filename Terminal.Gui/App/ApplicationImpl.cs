namespace Terminal.Gui.App;

/// <summary>
///     Implementation of core <see cref="Application"/> methods using the modern
///     main loop architecture with component factories for different platforms.
/// </summary>
internal partial class ApplicationImpl : IApplication, IApplicationAsyncDispatcher, IApplicationInlineOutput, IApplicationThreadAccess
{
    private readonly ITimeProvider _timeProvider;
    private IInputInjector? _inputInjector;

    /// <summary>
    ///     INTERNAL: Creates a new instance of the Application backend and subscribes to Application configuration property
    ///     events.
    /// </summary>
    /// <param name="timeProvider">Time provider for timestamps and timing control.</param>
    internal ApplicationImpl (ITimeProvider timeProvider)
    {
        _timeProvider = timeProvider;

        // Initialize TimedEvents with the time provider for testable timing
        TimedEvents = new TimedEvents (timeProvider);

        ForceDriver = Application.ForceDriver;
        AppModel = Application.AppModel;
        ForceInlinePosition = Application.ForceInlinePosition;

        // Subscribe to Application static property change events
        Application.ForceDriverChanged += OnForceDriverChanged;
        Application.AppModelChanged += OnAppModelChanged;
        Application.ForceInlinePositionChanged += OnForceInlinePositionChanged;
    }

    /// <summary>
    ///     INTERNAL: Creates a new instance of the Application backend for legacy static model.
    ///     Uses SystemTimeProvider and production mode by default.
    /// </summary>
    internal ApplicationImpl () : this (new SystemTimeProvider ()) { }

    /// <summary>
    ///     INTERNAL: Creates a new instance of the Application backend.
    /// </summary>
    /// <param name="componentFactory"></param>
    internal ApplicationImpl (IComponentFactory componentFactory) : this () => _componentFactory = componentFactory;

    /// <summary>
    ///     INTERNAL: Creates a new instance of the Application backend for testing.
    /// </summary>
    /// <param name="componentFactory">The component factory.</param>
    /// <param name="timeProvider">Time provider for timestamps and timing control.</param>
    internal ApplicationImpl (IComponentFactory componentFactory, ITimeProvider timeProvider) : this (timeProvider) => _componentFactory = componentFactory;

    private string? _driverName;

    /// <inheritdoc/>
    public new string ToString () => Driver?.ToString () ?? string.Empty;

    #region Singleton - Legacy Static Support

    /// <summary>
    ///     Lock object for synchronizing access to ModelUsage and _instance.
    /// </summary>
    private static readonly Lock _modelUsageLock = new ();

    /// <summary>
    ///     Tracks which application model has been used in this process.
    /// </summary>
    public static ApplicationModelUsage ModelUsage { get; private set; } = ApplicationModelUsage.None;

    /// <summary>
    ///     Error message for when trying to use modern model after legacy static model.
    /// </summary>
    internal const string ERROR_MODERN_AFTER_LEGACY =
        "Cannot use modern instance-based model (Application.Create) after using legacy static Application model (Application.Init/ApplicationImpl.Instance). "
        + "Use only one model per process.";

    /// <summary>
    ///     Error message for when trying to use legacy static model after modern model.
    /// </summary>
    internal const string ERROR_LEGACY_AFTER_MODERN =
        "Cannot use legacy static Application model (Application.Init/ApplicationImpl.Instance) after using modern instance-based model (Application.Create). "
        + "Use only one model per process.";

    /// <summary>
    ///     Configures the singleton instance of <see cref="Application"/> to use the specified backend implementation.
    /// </summary>
    /// <param name="app"></param>
    public static void SetInstance (IApplication? app)
    {
        lock (_modelUsageLock)
        {
            ModelUsage = ApplicationModelUsage.LegacyStatic;
            _instance = app;
        }
    }

    // Private static readonly Lazy instance of Application
    private static IApplication? _instance;

    /// <summary>
    ///     Gets the currently configured backend implementation of <see cref="Application"/> gateway methods.
    /// </summary>
    internal static IApplication Instance
    {
        get
        {
            //Debug.Fail ("ApplicationImpl.Instance accessed - parallelizable tests should not use legacy static Application model");

            // Thread-safe: Use lock to make check-and-create atomic
            lock (_modelUsageLock)
            {
                // If an instance already exists, return it without fence checking
                // This allows for cleanup/reset operations
                if (_instance is { })
                {
                    return _instance;
                }

                // Check if the instance-based model has already been used
                if (ModelUsage == ApplicationModelUsage.InstanceBased)
                {
                    throw new InvalidOperationException (ERROR_LEGACY_AFTER_MODERN);
                }

                // Mark the usage and create the instance
                ModelUsage = ApplicationModelUsage.LegacyStatic;

                return _instance = new ApplicationImpl ();
            }
        }
    }

    /// <summary>
    ///     INTERNAL: Marks that the instance-based model has been used. Called by Application.Create().
    /// </summary>
    internal static void MarkInstanceBasedModelUsed ()
    {
        lock (_modelUsageLock)
        {
            // Check if the legacy static model has already been initialized
            if (ModelUsage == ApplicationModelUsage.LegacyStatic && _instance?.Initialized == true)
            {
                throw new InvalidOperationException (ERROR_MODERN_AFTER_LEGACY);
            }

            ModelUsage = ApplicationModelUsage.InstanceBased;
        }
    }

    /// <summary>
    ///     INTERNAL: Resets the model usage tracking. Only for testing purposes.
    /// </summary>
    internal static void ResetModelUsageTracking ()
    {
        lock (_modelUsageLock)
        {
            ModelUsage = ApplicationModelUsage.None;
            _instance = null;
        }
    }

    /// <summary>
    ///     INTERNAL: Resets state without going through the fence-checked Instance property.
    ///     Used by Application.ResetState() to allow cleanup regardless of which model was used.
    /// </summary>
    internal static void ResetStateStatic (bool ignoreDisposed = false)
    {
        // If an instance exists, reset it
        _instance?.ResetState (ignoreDisposed);

        // Always reset the model tracking to allow tests to use either model after reset
        ResetModelUsageTracking ();
    }

    #endregion Singleton - Legacy Static Support

    #region Screen and Driver

    /// <inheritdoc/>
    public IClipboard? Clipboard { get => Driver?.Clipboard; set => Driver?.Clipboard = value; }

    #endregion Screen and Driver

    #region Input (Mouse/Keyboard)

    /// <inheritdoc/>
    public IInputInjector GetInputInjector ()
    {
        if (_inputInjector is { })
        {
            return _inputInjector;
        }

        if (Driver is null)
        {
            throw new InvalidOperationException ("Driver not initialized. Call Init() first.");
        }

        IInputProcessor processor = Driver.GetInputProcessor ();
        _inputInjector = new InputInjector (processor, _timeProvider);

        return _inputInjector;
    }

    private IKeyboard? _keyboard;

    /// <inheritdoc/>
    public IKeyboard Keyboard
    {
        get
        {
            _keyboard ??= new ApplicationKeyboard { App = this };

            return _keyboard;
        }
        set => _keyboard = value ?? throw new ArgumentNullException (nameof (value));
    }

    private IMouse? _mouse;

    /// <inheritdoc/>
    public IMouse Mouse
    {
        get
        {
            _mouse ??= new ApplicationMouse { App = this };

            return _mouse;
        }
        set => _mouse = value ?? throw new ArgumentNullException (nameof (value));
    }

    /// <inheritdoc/>
    public event EventHandler<PasteEventArgs>? Paste;

    /// <inheritdoc/>
    public bool RaisePasteEvent (string text)
    {
        PasteEventArgs args = new (text);

        Paste?.Invoke (this, args);

        if (args.Handled)
        {
            return true;
        }

        // Route only to the focused view — paste data is text destined for a text-input control,
        // and silently dispatching to a non-text container would either drop the paste or surprise
        // the user. Apps that want to handle pastes without a focused view should subscribe to
        // Application.Paste and set Handled.
        View? focused = Navigation?.GetFocused ();

        if (focused is null)
        {
            return false;
        }

        // Dispatch through Command.Paste so bracketed paste shares the same paste handler
        // (sanitization, Pasting/Pasted events, insertion) as keyboard-driven paste. Use a
        // dedicated payload object so the handler does not mistake unrelated string-valued command
        // context entries for pasted text.
        CommandContext ctx = new (Command.Paste, new WeakReference<View> (focused), binding: null);

        return focused.InvokeCommand (Command.Paste, ctx.WithValue (new PastePayload (args.Text))) is true;
    }

    #endregion Input (Mouse/Keyboard)

    #region Navigation and Popover

    /// <inheritdoc/>
    public ApplicationNavigation? Navigation
    {
        get
        {
            field ??= new ApplicationNavigation { App = this };

            return field;
        }
        set => field = value ?? throw new ArgumentNullException (nameof (value));
    }

    /// <inheritdoc/>
    public ApplicationPopover? Popovers
    {
        get
        {
            field ??= new ApplicationPopover { App = this };

            return field;
        }
        set;
    }

    public ApplicationToolTip? ToolTips
    {
        get
        {
            field ??= new ApplicationToolTip { App = this };

            return field;
        }
        set;
    }

    #endregion Navigation and Popover
}
