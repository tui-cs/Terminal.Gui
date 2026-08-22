// We use global using directives to simplify the code and avoid repetitive namespace declarations.
// Put them here so they are available throughout the application.
// Do not put them in AssemblyInfo.cs as it will break GitVersion's /updateassemblyinfo

global using Terminal.Gui.App;
global using Terminal.Gui.Configuration;
global using Terminal.Gui.Drawing;
global using Terminal.Gui.Drivers;
global using Terminal.Gui.FileServices;
global using Terminal.Gui.Input;
global using Terminal.Gui.Resources;
global using Terminal.Gui.Testing;
global using Terminal.Gui.Text;
global using Terminal.Gui.Time;
global using Terminal.Gui.ViewBase;
global using Terminal.Gui.Views;
global using Attribute = Terminal.Gui.Drawing.Attribute;
global using Color = Terminal.Gui.Drawing.Color;
using System.Globalization;
using System.Reflection;
using System.Resources;
using Terminal.Gui.Tracing;

namespace Terminal.Gui.App;

/// <summary>
///     <para>
///         The <c>Application</c> class provides static methods and properties for managing the application's lifecycle,
///         configuration, and global events. It serves as the primary entry point for creating and running a Terminal.Gui
///         application. The class includes methods for creating application instances, configuring global settings, and
///         raising events related to application creation, initialization, and disposal.
///     </para>
///     <para>
///         It also provides properties for managing supported cultures and key bindings for common actions. The
///         <c>Application</c> class is designed
///         to support both a modern instance-based model (where developers create and manage their own
///         <see cref="IApplication"/> instances) and a legacy static model (which is being phased out). The events in this
///         class are thread-local, allowing for parallel test execution where each thread can independently monitor
///         application instances created on that thread.
///     </para>
/// </summary>
/// <example>
///     <para>
///         Here's a simple example of how to create and run a Terminal.Gui application using the modern instance-based
///         model:
///     </para>
///     <code>
///       IApplication app = Application.Create ().Init ();
///       using Window top = new ();
///       top.Add(myView);
///       app.Run(top);
///    </code>
/// </example>
public static partial class Application
{
    /// <summary>
    ///     Creates a new <see cref="IApplication"/> instance.
    /// </summary>
    /// <param name="timeProvider">
    ///     Optional time provider for controlling time in tests. If <see langword="null"/>, defaults to
    ///     <see cref="SystemTimeProvider"/>.
    ///     For production use, omit this parameter or pass <see langword="null"/>. For testing, pass a
    ///     <see cref="VirtualTimeProvider"/>.
    /// </param>
    /// <remarks>
    ///     The recommended pattern is for developers to call <c>Application.Create()</c> and then use the returned
    ///     <see cref="IApplication"/> instance for all subsequent application operations.
    /// </remarks>
    /// <returns>A new <see cref="IApplication"/> instance.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if the legacy static Application model has already been used in this process.
    /// </exception>
    public static IApplication Create (ITimeProvider? timeProvider = null)
    {
        //Debug.Fail ("Application.Create() called");
        ApplicationImpl.MarkInstanceBasedModelUsed ();

        IApplication app = new ApplicationImpl (timeProvider ?? new SystemTimeProvider ());
        RaiseInstanceCreated (app);

        return app;
    }

    #region Modern Instance-Based Model Events (Thread-Local)

    // Thread-local backing fields for events - each thread has its own subscribers
    private static readonly ThreadLocal<EventHandler<EventArgs<IApplication>>?> _instanceCreated = new ();
    private static readonly ThreadLocal<EventHandler<EventArgs<IApplication>>?> _instanceInitialized = new ();
    private static readonly ThreadLocal<EventHandler<EventArgs<IApplication>>?> _instanceDisposed = new ();

    /// <summary>
    ///     Raised when an <see cref="IApplication"/> instance is created via <see cref="Create"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This event is for the modern instance-based model only. It fires immediately after
    ///         <see cref="Create"/> creates a new instance, before <see cref="IApplication.Init"/> is called.
    ///     </para>
    ///     <para>
    ///         This event is thread-local, meaning each thread has its own set of subscribers.
    ///         This enables parallel test execution where each test thread can independently
    ///         monitor application instances created on that thread.
    ///     </para>
    /// </remarks>
    public static event EventHandler<EventArgs<IApplication>>? InstanceCreated
    {
        add => _instanceCreated.Value += value;
        remove => _instanceCreated.Value -= value;
    }

    /// <summary>
    ///     Raised when an <see cref="IApplication"/> instance completes initialization.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This event is for the modern instance-based model only. It fires after
    ///         <see cref="IApplication.Init"/> completes successfully.
    ///     </para>
    ///     <para>
    ///         This event is thread-local, meaning each thread has its own set of subscribers.
    ///         This enables parallel test execution where each test thread can independently
    ///         monitor application instances initialized on that thread.
    ///     </para>
    /// </remarks>
    public static event EventHandler<EventArgs<IApplication>>? InstanceInitialized
    {
        add => _instanceInitialized.Value += value;
        remove => _instanceInitialized.Value -= value;
    }

    /// <summary>
    ///     Raised when an <see cref="IApplication"/> instance is disposed.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This event is for the modern instance-based model only. It fires when
    ///         <see cref="IDisposable.Dispose"/> is called on an instance.
    ///     </para>
    ///     <para>
    ///         This event is thread-local, meaning each thread has its own set of subscribers.
    ///         This enables parallel test execution where each test thread can independently
    ///         monitor application instances disposed on that thread.
    ///     </para>
    /// </remarks>
    public static event EventHandler<EventArgs<IApplication>>? InstanceDisposed
    {
        add => _instanceDisposed.Value += value;
        remove => _instanceDisposed.Value -= value;
    }

    /// <summary>
    ///     Raises the <see cref="InstanceCreated"/> event on the current thread.
    /// </summary>
    internal static void RaiseInstanceCreated (IApplication app) => _instanceCreated.Value?.Invoke (null, new EventArgs<IApplication> (app));

    /// <summary>
    ///     Raises the <see cref="InstanceInitialized"/> event on the current thread.
    /// </summary>
    internal static void RaiseInstanceInitialized (IApplication app) => _instanceInitialized.Value?.Invoke (null, new EventArgs<IApplication> (app));

    /// <summary>
    ///     Raises the <see cref="InstanceDisposed"/> event on the current thread.
    /// </summary>
    internal static void RaiseInstanceDisposed (IApplication app) => _instanceDisposed.Value?.Invoke (null, new EventArgs<IApplication> (app));

    #endregion Modern Instance-Based Model Events (Thread-Local)

    /// <summary>
    ///     Maximum number of iterations of the main loop (and hence draws)
    ///     to allow to occur per second. Defaults to 25ms.
    ///     <remarks>
    ///         Note that not every iteration draws (see <see cref="View.NeedsDraw"/>).
    ///     </remarks>
    /// </summary>

    public static ushort MaximumIterationsPerSecond { get; set; } = 25;

    /// <summary>
    ///     Gets the default maximum number of iterations per second for the main loop.
    /// </summary>
    /// <remarks>
    ///     This value determines the default upper limit on how many times the main loop can execute per
    ///     second. Adjusting this value can affect application responsiveness and CPU usage.
    /// </remarks>
    public static ushort DefaultMaximumIterationsPerSecond { get; } = 25;

    /// <summary>Gets all cultures supported by the application without the invariant language.</summary>
    public static List<CultureInfo>? SupportedCultures { get; private set; } = GetSupportedCultures ();

    internal static List<CultureInfo> GetAvailableCulturesFromEmbeddedResources ()
    {
        ResourceManager rm = new (typeof (Strings));

        CultureInfo [] cultures = CultureInfo.GetCultures (CultureTypes.AllCultures);

        return cultures.Where (cultureInfo => !cultureInfo.Equals (CultureInfo.InvariantCulture) && rm.GetResourceSet (cultureInfo, true, false) is { })
                       .ToList ();
    }

    // BUGBUG: This does not return en-US even though it's supported by default
    internal static List<CultureInfo> GetSupportedCultures ()
    {
        CultureInfo [] cultures = CultureInfo.GetCultures (CultureTypes.AllCultures);

        // Get the assembly
        var assembly = Assembly.GetExecutingAssembly ();

        //Find the location of the assembly
        string assemblyLocation = AppDomain.CurrentDomain.BaseDirectory;

        // Find the resource file name of the assembly
        var resourceFilename = $"{assembly.GetName ().Name}.resources.dll";

        if (cultures.Length > 1 && Directory.Exists (Path.Combine (assemblyLocation, "pt-PT")))
        {
            // Return all culture for which satellite folder found with culture code.
            return cultures.Where (cultureInfo => Directory.Exists (Path.Combine (assemblyLocation, cultureInfo.Name))
                                                  && File.Exists (Path.Combine (assemblyLocation, cultureInfo.Name, resourceFilename)))
                           .ToList ();
        }

        // It's called from a self-contained single-file and get available cultures from the embedded resources strings.
        return GetAvailableCulturesFromEmbeddedResources ();
    }

    /// <summary>
    ///     Gets or sets the rendering mode for the application. When set to <see cref="App.AppModel.Inline"/>,
    ///     the application renders inline within the primary (scrollback) buffer instead of switching to
    ///     the alternate screen buffer.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Set this property <b>before</b> calling <see cref="IApplication.Init"/> to control how the
    ///         application interacts with the terminal buffer.
    ///     </para>
    /// </remarks>
    public static AppModel AppModel
    {
        get => ApplicationSettings.Defaults.AppModel;
        set
        {
            AppModel oldValue = ApplicationSettings.Defaults.AppModel;
            ApplicationSettings.Defaults.AppModel = value;
            AppModelChanged?.Invoke (null, new ValueChangedEventArgs<AppModel> (oldValue, value));
        }
    }

    /// <summary>
    ///     Gets or sets an override for the initial cursor position used in <see cref="AppModel.Inline"/> mode.
    ///     When set (non-null) before <see cref="IApplication.Run{T}"/>, this value is used instead of
    ///     querying the terminal via ANSI CPR. Useful for testing inline mode at specific cursor positions.
    ///     The <c>Y</c> component specifies the terminal row; <c>X</c> is reserved for future use.
    /// </summary>
    public static Point? ForceInlinePosition
    {
        get;
        set
        {
            Point? oldValue = field;
            field = value;
            ForceInlinePositionChanged?.Invoke (null, new ValueChangedEventArgs<Point?> (oldValue, field));
        }
    }

    /// <inheritdoc cref="IApplication.ForceDriver"/>
    public static string ForceDriver
    {
        get => ApplicationSettings.Defaults.ForceDriver;
        set
        {
            string oldValue = ApplicationSettings.Defaults.ForceDriver;
            ApplicationSettings.Defaults.ForceDriver = value;
            ForceDriverChanged?.Invoke (null, new ValueChangedEventArgs<string> (oldValue, value));
        }
    }

    /// <summary>
    ///     Gets or sets the default key bindings for Application-level commands, optionally varying by platform.
    ///     Each entry maps a <see cref="Command"/> to a <see cref="PlatformKeyBinding"/>
    ///     that specifies the key strings for all platforms or specific ones.
    ///     <para>
    ///         To change a single binding and have the change take effect immediately, use
    ///         <see cref="SetDefaultKeyBinding"/> or <see cref="RemoveDefaultKeyBinding"/>
    ///         instead of mutating the dictionary directly. Direct dictionary mutation
    ///         (e.g. <c>DefaultKeyBindings [Command.Quit] = ...</c>) does <b>not</b> raise
    ///         <see cref="DefaultKeyBindingsChanged"/>.
    ///     </para>
    ///     <para>
    ///         <b>IMPORTANT:</b> This is a process-wide static property. Change with care.
    ///         Do not set in parallelizable unit tests.
    ///     </para>
    /// </summary>
    public static Dictionary<Command, PlatformKeyBinding>? DefaultKeyBindings
    {
        get;
        set
        {
            if (ReferenceEquals (field, value))
            {
                return;
            }

            field = value;
            Trace.Configuration ("DefaultKeyBindings", "App Set", $"{string.Join (", ", value?.Select (kvp => $"{kvp.Key}=[{kvp.Value}]") ?? [])}");
            DefaultKeyBindingsChanged?.Invoke (null, EventArgs.Empty);
        }
    } = new ()
    {
        [Command.Quit] = Bind.All (Key.Esc),
        [Command.Suspend] = Bind.NonWindows (Key.Z.WithCtrl),
        [Command.Arrange] = Bind.All (Key.F5.WithCtrl),
        [Command.NextTabStop] = Bind.All (Key.Tab),
        [Command.PreviousTabStop] = Bind.All (Key.Tab.WithShift),
        [Command.NextTabGroup] = Bind.All (Key.F6),
        [Command.PreviousTabGroup] = Bind.All (Key.F6.WithShift),
        [Command.Refresh] = Bind.All (Key.F5)
    };

    /// <summary>
    ///     Raised when the <see cref="DefaultKeyBindings"/> are changed — either by replacing the entire dictionary
    ///     (property setter) or by calling <see cref="SetDefaultKeyBinding"/> / <see cref="RemoveDefaultKeyBinding"/>.
    /// </summary>
    public static event EventHandler? DefaultKeyBindingsChanged;

    /// <summary>
    ///     Sets (or replaces) the key binding for a single <paramref name="command"/> in <see cref="DefaultKeyBindings"/>
    ///     and raises <see cref="DefaultKeyBindingsChanged"/> so that subscribers (e.g. <c>ApplicationKeyboard</c>)
    ///     pick up the change immediately.
    /// </summary>
    /// <param name="command">The command whose binding should be set.</param>
    /// <param name="binding">The platform key binding to associate with <paramref name="command"/>.</param>
    public static void SetDefaultKeyBinding (Command command, PlatformKeyBinding binding)
    {
        if (DefaultKeyBindings is null)
        {
            throw new InvalidOperationException ("DefaultKeyBindings dictionary is null. Initialize it before setting individual bindings.");
        }

        DefaultKeyBindings [command] = binding;
        Trace.Configuration ("DefaultKeyBindings", "SetDefaultKeyBinding", $"{command}=[{binding}]");
        DefaultKeyBindingsChanged?.Invoke (null, EventArgs.Empty);
    }

    /// <summary>
    ///     Removes the key binding for <paramref name="command"/> from <see cref="DefaultKeyBindings"/>
    ///     and raises <see cref="DefaultKeyBindingsChanged"/> so that subscribers pick up the change immediately.
    /// </summary>
    /// <param name="command">The command whose binding should be removed.</param>
    /// <returns><see langword="true"/> if the binding was found and removed; otherwise <see langword="false"/>.</returns>
    public static bool RemoveDefaultKeyBinding (Command command)
    {
        if (DefaultKeyBindings is null || !DefaultKeyBindings.Remove (command))
        {
            return false;
        }

        Trace.Configuration ("DefaultKeyBindings", "RemoveDefaultKeyBinding", $"{command}");
        DefaultKeyBindingsChanged?.Invoke (null, EventArgs.Empty);

        return true;
    }

    /// <summary>
    ///     Returns the first platform-resolved key for the specified <paramref name="command"/>
    ///     from <see cref="DefaultKeyBindings"/>, or <see cref="Key.Empty"/> if none is configured.
    /// </summary>
    public static Key GetDefaultKey (Command command)
    {
        if (DefaultKeyBindings is null || !DefaultKeyBindings.TryGetValue (command, out PlatformKeyBinding? binding))
        {
            return Key.Empty;
        }

        return binding.GetCurrentPlatformKeys ().FirstOrDefault () ?? Key.Empty;
    }

    /// <summary>
    ///     Returns all platform-resolved keys for the specified <paramref name="command"/>
    ///     from <see cref="DefaultKeyBindings"/>.
    /// </summary>
    public static IEnumerable<Key> GetDefaultKeys (Command command)
    {
        if (DefaultKeyBindings is null || !DefaultKeyBindings.TryGetValue (command, out PlatformKeyBinding? binding))
        {
            return [];
        }

        return binding.GetCurrentPlatformKeys ();
    }
}
