using System.Collections.Concurrent;

namespace Terminal.Gui.App;

/// <summary>
///     Interface for the main application loop that runs the core Terminal.Gui UI rendering and event processing.
/// </summary>
/// <remarks>
///     This interface defines the contract for the main loop that coordinates:
///     <list type="bullet">
///         <item>Processing input events from the console</item>
///         <item>Running user timeout callbacks</item>
///         <item>Detecting UI changes that need redrawing</item>
///         <item>Rendering UI updates to the console</item>
///     </list>
/// </remarks>
/// <typeparam name="TInputRecord">Type of raw input events processed by the loop, e.g. <see cref="ConsoleKeyInfo"/> for cross-platform .NET driver</typeparam>
public interface IApplicationMainLoop<TInputRecord> : IDisposable where TInputRecord : struct
{
    /// <summary>
    ///     The Application this loop is associated with.
    /// </summary>
    public IApplication? App { get; }

    /// <summary>
    ///     Gets the <see cref="ITimedEvents"/> implementation that manages user-defined timeouts and periodic events.
    /// </summary>
    public ITimedEvents TimedEvents { get; }

    /// <summary>
    ///     Gets the <see cref="IOutputBuffer"/> representing the desired screen state for console rendering.
    /// </summary>
    public IOutputBuffer OutputBuffer { get; }

    /// <summary>
    ///     Gets the <see cref="IOutput"/> implementation responsible for rendering the <see cref="OutputBuffer"/> to the console using platform specific methods.
    /// </summary>
    public IOutput Output { get; }

    /// <summary>
    ///     Gets <see cref="InputProcessor"/> implementation that processes the mouse and keyboard input populated by <see cref="IInput{TInputRecord}"/>
    ///     implementations on the input thread and translating to events on the UI thread.
    /// </summary>
    public IInputProcessor InputProcessor { get; }

    /// <summary>
    ///     Gets the class responsible for sending ANSI escape requests which expect a response
    ///     from the remote terminal e.g. Device Attribute Request
    /// </summary>
    public AnsiRequestScheduler AnsiRequestScheduler { get; }

    /// <summary>
    ///     Gets the <see cref="ISizeMonitor"/> implementation that tracks terminal size changes.
    /// </summary>
    public ISizeMonitor SizeMonitor { get; }

    /// <summary>
    ///     Initializes the main loop with its required dependencies.
    /// </summary>
    /// <param name="timedEvents">
    ///     The <see cref="ITimedEvents"/> implementation for managing user-defined timeouts and periodic callbacks
    ///     (e.g., <see cref="Application.AddTimeout"/>).
    /// </param>
    /// <param name="inputQueue">
    ///     The thread-safe queue containing raw input events populated by <see cref="IInput{TInputRecord}"/> on
    ///     the input thread. This queue is drained by <see cref="InputProcessor"/> during each <see cref="Iteration"/>.
    /// </param>
    /// <param name="inputProcessor">
    ///     The <see cref="IInputProcessor"/> that translates raw input records (e.g., <see cref="ConsoleKeyInfo"/>) 
    ///     into Terminal.Gui events (<see cref="Key"/>, <see cref="Mouse"/>) and raises them on the main UI thread.
    /// </param>
    /// <param name="output">
    ///     The <see cref="IOutput"/> implementation responsible for rendering the <see cref="OutputBuffer"/> to the
    ///     console using platform-specific methods (e.g., Win32 APIs, ANSI escape sequences).
    /// </param>
    /// <param name="componentFactory">
    ///     The factory for creating driver-specific components. Used here to create the <see cref="ISizeMonitor"/>
    ///     that tracks terminal size changes.
    /// </param>
    /// <param name="app"></param>
    /// <remarks>
    ///     <para>
    ///         This method is called by <see cref="MainLoopCoordinator{TInputRecord}"/> during application startup
    ///         to wire up all the components needed for the main loop to function. It must be called before
    ///         <see cref="Iteration"/> can be invoked.
    ///     </para>
    ///     <para>
    ///         <b>Initialization order:</b>
    ///     </para>
    ///     <list type="number">
    ///         <item>Store references to <paramref name="timedEvents"/>, <paramref name="inputQueue"/>, 
    ///               <paramref name="inputProcessor"/>, and <paramref name="output"/></item>
    ///         <item>Create <see cref="AnsiRequestScheduler"/> for managing ANSI requests/responses</item>
    ///         <item>Initialize <see cref="OutputBuffer"/> size to match current console dimensions</item>
    ///         <item>Create <see cref="ISizeMonitor"/> using the <paramref name="componentFactory"/></item>
    ///     </list>
    ///     <para>
    ///         After initialization, the main loop is ready to process events via <see cref="Iteration"/>.
    ///     </para>
    /// </remarks>
    void Initialize (
        ITimedEvents timedEvents,
        ConcurrentQueue<TInputRecord> inputQueue,
        IInputProcessor inputProcessor,
        IOutput output,
        IComponentFactory<TInputRecord> componentFactory,
        IApplication? app
    );

    /// <summary>
    ///     Perform a single iteration of the main loop then blocks for a fixed length
    ///     of time, this method is designed to be run in a loop.
    /// </summary>
    public void Iteration ();
}
