namespace Terminal.Gui.Drivers;

/// <summary>
///     Source of input events. Production implementations read from console,
///     test implementations provide pre-programmed input.
/// </summary>
public interface IInputSource
{
    /// <summary>
    ///     Time provider for timestamps and timing.
    /// </summary>
    ITimeProvider TimeProvider { get; }

    /// <summary>
    ///     Check if input is available without consuming it.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    ///     Reads all available input synchronously.
    ///     Called by InputProcessor on the main loop thread.
    /// </summary>
    /// <returns>An enumerable of input records available for processing.</returns>
    IEnumerable<InputEventRecord> ReadAvailable ();

    /// <summary>
    ///     Starts background input reading (for production implementations).
    ///     Test implementations typically don't need this.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop reading.</param>
    void Start (CancellationToken cancellationToken);

    /// <summary>
    ///     Stops background input reading.
    /// </summary>
    void Stop ();
}
