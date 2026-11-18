namespace Terminal.Gui.Time;

/// <summary>
///     Represents a timer that can be controlled.
/// </summary>
public interface ITimer : IDisposable
{
    /// <summary>
    ///     Starts the timer.
    /// </summary>
    void Start ();

    /// <summary>
    ///     Stops the timer.
    /// </summary>
    void Stop ();

    /// <summary>
    ///     Gets a value indicating whether the timer is currently running.
    /// </summary>
    bool IsRunning { get; }
}
