namespace Terminal.Gui.ViewBase;

/// <summary>
///     Indicates the mode for a <see cref="DimPercent"/> object.
/// </summary>
public enum DimPercentMode
{
    /// <summary>
    ///     The dimension is computed using the View's position (<see cref="View.X"/> or <see cref="View.Y"/>).
    /// </summary>
    Position = 0,

    /// <summary>
    ///     The dimension is computed using the View's <see cref="View.GetContentSize ()"/>.
    /// </summary>
    ContentSize = 1
}
