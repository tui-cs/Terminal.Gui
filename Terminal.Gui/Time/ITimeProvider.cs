namespace Terminal.Gui.Time;

/// <summary>
///     Abstraction for time-related operations, allowing virtual time in tests.
/// </summary>
public interface ITimeProvider
{
    /// <summary>
    ///     Gets the current date/time. In tests, this can be controlled.
    /// </summary>
    DateTime Now { get; }

    /// <summary>
    ///     Creates a delay. In tests, this can be instant or controlled.
    /// </summary>
    /// <param name="duration">The time span to delay.</param>
    /// <param name="cancellationToken">Cancellation token for the delay.</param>
    /// <returns>A task that completes after the delay.</returns>
    Task Delay (TimeSpan duration, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Creates a timer that fires periodically.
    /// </summary>
    /// <param name="interval">The interval between timer callbacks.</param>
    /// <param name="callback">The action to invoke when the timer fires.</param>
    /// <returns>A timer instance that can be controlled.</returns>
    ITimer CreateTimer (TimeSpan interval, Action callback);
}
