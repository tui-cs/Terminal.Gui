namespace Terminal.Gui.Time;

/// <summary>
///     Test helper that wraps a Func&lt;DateTime&gt; into an ITimeProvider for backward compatibility.
///     Used in tests that need to control time via a lambda expression.
/// </summary>
internal class FuncTimeProvider : ITimeProvider
{
    private readonly Func<DateTime> _timeFunc;

    /// <summary>
    ///     Initializes a new instance of the <see cref="FuncTimeProvider"/> class.
    /// </summary>
    /// <param name="timeFunc">Function that returns the current time.</param>
    public FuncTimeProvider (Func<DateTime> timeFunc) { _timeFunc = timeFunc; }

    /// <inheritdoc/>
    public DateTime Now => _timeFunc ();

    /// <inheritdoc/>
    public Task Delay (TimeSpan duration, CancellationToken cancellationToken = default)
    {
        // For test scenarios, delays complete immediately
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public ITimer CreateTimer (TimeSpan interval, Action callback)
    {
        throw new NotSupportedException ("FuncTimeProvider does not support timers. Use VirtualTimeProvider for timer support.");
    }
}
