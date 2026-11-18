namespace Terminal.Gui.Drivers;

/// <summary>
///     Interface for classes responsible for reporting the current
///     size of the terminal window.
/// </summary>
public interface ISizeMonitor
{
    /// <summary>
    ///     Gets whether the initial terminal size has been determined.
    ///     Returns <see langword="true"/> for size monitors that know the size synchronously
    ///     (e.g. via native APIs). For ANSI-based monitors that query size asynchronously
    ///     via CSI escape sequences, returns <see langword="false"/> until the first size
    ///     response is received from the terminal.
    /// </summary>
    bool InitialSizeReceived => true;

    /// <summary>
    ///     Gets the terminal row (0-indexed) where the cursor was when the application started.
    ///     Used by inline mode to position the inline region at the cursor's starting row.
    ///     Returns <c>0</c> by default (top of screen).
    /// </summary>
    int InitialCursorRow => 0;

    /// <summary>
    ///     Called after the driver is fully initialized to allow the size monitor to perform
    ///     any setup that requires access to the driver (e.g., queuing ANSI requests, setting up
    ///     signal handlers, registering for console events).
    /// </summary>
    /// <param name="driver">The fully initialized driver instance</param>
    /// <remarks>
    ///     <para>
    ///         This method is called by the framework after all driver components are created and wired up.
    ///         Implementations can use this to:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>Queue ANSI size queries (ANSI drivers)</item>
    ///         <item>Set up platform-specific signal handlers (UnixDriver with SIGWINCH)</item>
    ///         <item>Register for console buffer events (WindowsDriver)</item>
    ///         <item>Query initial size asynchronously</item>
    ///     </list>
    ///     <para>
    ///         The default implementation does nothing, making this method optional for size monitors
    ///         that don't need post-initialization setup (like those that can query size synchronously
    ///         via Console.WindowWidth/Height).
    ///     </para>
    /// </remarks>
    void Initialize (IDriver? driver) { }

    /// <summary>Invoked when the terminal's size changed. The new size of the terminal is provided.</summary>
    event EventHandler<SizeChangedEventArgs>? SizeChanged;

    /// <summary>
    ///     Examines the current size of the terminal and raises <see cref="SizeChanged"/> if it is different
    ///     from last inspection.
    /// </summary>
    /// <returns><see langword="true"/> if the size has changed; otherwise, <see langword="false"/>.</returns>
    bool Poll ();
}
