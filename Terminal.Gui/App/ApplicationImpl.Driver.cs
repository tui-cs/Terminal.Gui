using System.Collections.Concurrent;
using Terminal.Gui.Tracing;

namespace Terminal.Gui.App;

internal partial class ApplicationImpl
{
    /// <inheritdoc/>
    public IDriver? Driver { get; set; }

    /// <inheritdoc/>
    public AnsiStartupGate? AnsiStartupGate { get; set; }

    /// <inheritdoc/>
    public string ForceDriver { get; set; } = string.Empty;

    /// <inheritdoc/>
    public AppModel AppModel { get; set; } = AppModel.FullScreen;

    /// <inheritdoc/>
    public Point? ForceInlinePosition { get; set; }

    /// <summary>
    ///     Creates the appropriate <see cref="IDriver"/> based on platform and driverName.
    /// </summary>
    /// <param name="driverName"></param>
    private void CreateDriver (string? driverName)
    {
        // If component factory already provided, use it
        if (_componentFactory != null)
        {
            _driverName = _componentFactory.GetDriverName ();
            Trace.Lifecycle (MainThreadId?.ToString (), "Init", $"Using provided component factory: {_driverName}");

            // Determine the input type and create subcomponents
            if (_componentFactory is IComponentFactory<WindowsConsole.InputRecord> windowsFactory)
            {
                Coordinator = CreateSubcomponents (() => windowsFactory);
            }
            else if (_componentFactory is IComponentFactory<ConsoleKeyInfo> netFactory)
            {
                Coordinator = CreateSubcomponents (() => netFactory);
            }
            else if (_componentFactory is IComponentFactory<char> unixFactory)
            {
                Coordinator = CreateSubcomponents (() => unixFactory);
            }
            else
            {
                throw new InvalidOperationException ($"Unknown component factory type: {_componentFactory.GetType ().Name}");
            }
        }
        else
        {
            if (!string.IsNullOrEmpty (driverName) && !DriverRegistry.TryGetDriver (driverName, out _))
            {
                throw new ArgumentException ($"Driver '{driverName}' is not registered in DriverRegistry.");
            }

            // Determine which driver to use
            if (!string.IsNullOrEmpty (driverName) && DriverRegistry.TryGetDriver (driverName, out DriverRegistry.DriverDescriptor? descriptor))
            {
                // Use explicitly specified driver name
                _driverName = descriptor!.Name;
                Trace.Lifecycle (MainThreadId?.ToString (), "Init", $"Using driver specified by parameter: {descriptor.Name} ({descriptor.DisplayName})");
            }
            else if (!string.IsNullOrEmpty (ForceDriver) && DriverRegistry.TryGetDriver (ForceDriver, out descriptor))
            {
                // Use ForceDriver configuration property
                _driverName = descriptor!.Name;

                Trace.Lifecycle (MainThreadId?.ToString (),
                                 "Init",
                                 $"Using driver from ForceDriver configuration: {descriptor.Name} ({descriptor.DisplayName})");
            }
            else
            {
                // Use platform default
                descriptor = DriverRegistry.GetDefaultDriver ();
                _driverName = descriptor.Name;
                Trace.Lifecycle (MainThreadId?.ToString (), "Init", $"Using platform default driver: {descriptor.Name} ({descriptor.DisplayName})");
            }

            // Create coordinator based on driver name
            switch (_driverName)
            {
                case DriverRegistry.Names.WINDOWS:
                    Coordinator = CreateSubcomponents (() => new WindowsComponentFactory ());

                    break;

                case DriverRegistry.Names.DOTNET:
                    Coordinator = CreateSubcomponents (() => new NetComponentFactory ());

                    break;

                case DriverRegistry.Names.ANSI:
                    Coordinator = CreateSubcomponents (() => new AnsiComponentFactory ());

                    break;

                default:
                    throw new InvalidOperationException ($"Unknown driver name: {_driverName}");
            }
        }

        Coordinator.StartInputTaskAsync (this).Wait ();

        if (Driver == null)
        {
            throw new InvalidOperationException ("Driver was null even after booting MainLoopCoordinator");
        }

        Driver.Force16Colors = Drivers.Driver.Force16Colors;
    }

    private readonly IComponentFactory? _componentFactory;

    /// <summary>
    ///     INTERNAL: Gets or sets the main loop coordinator that orchestrates the application's event processing,
    ///     input handling, and rendering pipeline.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The <see cref="IMainLoopCoordinator"/> is the central component responsible for:
    ///         <list type="bullet">
    ///             <item>Managing the platform-specific input thread that reads from the console</item>
    ///             <item>Coordinating the main application loop via <see cref="IMainLoopCoordinator.RunIteration"/></item>
    ///             <item>Processing queued input events and translating them to Terminal.Gui events</item>
    ///             <item>Managing the <see cref="ApplicationMainLoop{TInputRecord}"/> that handles rendering</item>
    ///             <item>Executing scheduled timeouts and callbacks via <see cref="ITimedEvents"/></item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         The coordinator is created in <see cref="CreateDriver"/> based on the selected driver.
    ///         <see cref="IMainLoopCoordinator.StartInputTaskAsync"/>.
    ///     </para>
    /// </remarks>
    internal IMainLoopCoordinator? Coordinator { get; private set; }

    /// <summary>
    ///     INTERNAL: Creates a <see cref="MainLoopCoordinator{TInputRecord}"/> with the appropriate component factory
    ///     for the specified input record type.
    /// </summary>
    /// <typeparam name="TInputRecord">
    ///     Platform-specific input type: <see cref="ConsoleKeyInfo"/> (.NET),
    ///     <see cref="WindowsConsole.InputRecord"/> (Windows), or <see cref="char"/> (Unix).
    /// </typeparam>
    /// <param name="fallbackFactory">
    ///     Factory function to create the component factory if <see cref="_componentFactory"/>
    ///     is not of type <see cref="IComponentFactory{TInputRecord}"/>.
    /// </param>
    /// <returns>
    ///     A <see cref="MainLoopCoordinator{TInputRecord}"/> configured with the input queue,
    ///     main loop, timed events, and selected component factory.
    /// </returns>
    private IMainLoopCoordinator CreateSubcomponents<TInputRecord> (Func<IComponentFactory<TInputRecord>> fallbackFactory) where TInputRecord : struct
    {
        ConcurrentQueue<TInputRecord> inputQueue = new ();
        ApplicationMainLoop<TInputRecord> loop = new ();

        IComponentFactory<TInputRecord> cf;

        if (_componentFactory is IComponentFactory<TInputRecord> typedFactory)
        {
            cf = typedFactory;
        }
        else
        {
            cf = fallbackFactory ();
        }

        // Propagate the instance-based AppModel to the factory so CreateOutput()
        // knows whether to use alternate screen buffer (Inline vs FullScreen).
        cf.AppModel = AppModel;

        if (cf is AnsiComponentFactory ansiFactory)
        {
            ansiFactory.IsMouseDisabled = Mouse.IsMouseDisabled;
        }

        // In inline mode, create the startup gate so the driver and size monitor can
        // register queries (size, cursor position) that must complete before the first render.
        if (AppModel == AppModel.Inline)
        {
            AnsiStartupGate ??= new AnsiStartupGate ();
        }

        return new MainLoopCoordinator<TInputRecord> (TimedEvents, inputQueue, loop, cf, _timeProvider);
    }

    internal void SubscribeDriverEvents ()
    {
        if (Driver is null)
        {
            Logging.Error ("Driver is null");

            return;
        }

        Driver.SizeChanged += Driver_SizeChanged;
        Driver.KeyDown += Driver_KeyDown;
        Driver.KeyUp += Driver_KeyUp;
        Driver.MouseEvent += Driver_MouseEvent;
        Driver.Paste += Driver_Paste;
    }

    private void Driver_KeyDown (object? sender, Key e) => Keyboard.RaiseKeyDownEvent (e);

    private void Driver_KeyUp (object? sender, Key e) => Keyboard.RaiseKeyUpEvent (e);

    private void Driver_MouseEvent (object? sender, Mouse e) => Mouse.RaiseMouseEvent (e);

    private void Driver_Paste (object? sender, string e) => RaisePasteEvent (e);
}
