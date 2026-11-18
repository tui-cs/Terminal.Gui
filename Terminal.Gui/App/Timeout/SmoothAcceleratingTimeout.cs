#nullable disable
namespace Terminal.Gui.App;

/// <summary>
///     Timeout which accelerates slowly at first then fast up to a maximum speed.
///     Use <see cref="AdvanceStage"/> to increment the stage of the timer (e.g. in
///     your timer callback code).
/// </summary>
public class SmoothAcceleratingTimeout : Timeout
{
    /// <summary>
    ///     Creates a new instance of the smooth acceleration timeout.
    /// </summary>
    /// <param name="initialDelay">Delay before first tick, the longest it will ever take</param>
    /// <param name="minDelay">The fastest the timer can get no matter how long it runs</param>
    /// <param name="decayFactor">Controls how fast the timer accelerates</param>
    /// <param name="callback">Method to call when timer ticks</param>
    public SmoothAcceleratingTimeout (TimeSpan initialDelay, TimeSpan minDelay, double decayFactor, Func<bool> callback)
    {
        _initialDelay = initialDelay;
        _minDelay = minDelay;
        _decayFactor = decayFactor;
        Callback = callback;
    }

    private readonly TimeSpan _initialDelay;
    private readonly TimeSpan _minDelay;
    private readonly double _decayFactor;
    private int _stage;

    /// <summary>
    ///     Advances the timer stage, this should be called from your timer callback or whenever
    ///     you want to advance the speed.
    /// </summary>
    public void AdvanceStage () { _stage++; }

    /// <summary>
    ///     Resets the timer to original speed.
    /// </summary>
    public void Reset () { _stage = 0; }

    /// <inheritdoc/>
    public override TimeSpan Span
    {
        get
        {
            double initialMs = _initialDelay.TotalMilliseconds;
            double minMs = _minDelay.TotalMilliseconds;
            double delayMs = minMs + (initialMs - minMs) * Math.Pow (_decayFactor, _stage);

            return TimeSpan.FromMilliseconds (delayMs);
        }
    }
}
