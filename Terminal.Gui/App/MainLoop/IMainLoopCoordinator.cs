namespace Terminal.Gui.App;

/// <summary>
///     Interface for the main loop coordinator that manages UI loop initialization and threading.
/// </summary>
/// <remarks>
///     The coordinator is responsible for:
///     <list type="bullet">
///         <item>Starting the asynchronous input reading thread</item>
///         <item>Initializing the main UI loop on the application thread</item>
///         <item>Coordinating clean shutdown of both threads</item>
///     </list>
/// </remarks>
public interface IMainLoopCoordinator
{
    /// <summary>
    ///     Initializes all required subcomponents and starts the input thread.
    /// </summary>
    /// <param name="app"></param>
    /// <remarks>
    ///     This method:
    ///     <list type="number">
    ///         <item>Starts the input thread that reads console input asynchronously</item>
    ///         <item>Initializes the main UI loop on the calling thread</item>
    ///         <item>Waits for both to be ready before returning</item>
    ///     </list>
    /// </remarks>
    /// <returns>A task that completes when initialization is done</returns>
    public Task StartInputTaskAsync (IApplication? app);

    /// <summary>
    ///     Stops the input thread and performs cleanup.
    /// </summary>
    /// <remarks>
    ///     This method blocks until the input thread has exited.
    ///     It must be called only from the main UI thread.
    /// </remarks>
    public void Stop ();

    /// <summary>
    ///     Executes a single iteration of the main UI loop.
    /// </summary>
    /// <remarks>
    ///     Each iteration processes input, runs timeouts, checks for UI changes,
    ///     and renders any necessary updates.
    /// </remarks>
    void RunIteration ();
}
