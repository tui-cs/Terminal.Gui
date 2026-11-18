namespace Terminal.Gui.ViewBase;

/// <summary>
///     Represents a dimension that is a percentage of the width or height of the SuperView.
/// </summary>
/// <remarks>
///     This is a low-level API that is typically used internally by the layout system. Use the various static
///     methods on the <see cref="Dim"/> class to create <see cref="Dim"/> objects instead.
/// </remarks>
/// <param name="Percentage">The percentage.</param>
/// <param name="Mode">
///     If <see cref="DimPercentMode.Position"/> the dimension is computed using the View's position (<see cref="View.X"/>
///     or
///     <see cref="View.Y"/>); otherwise, the dimension is computed using the View's <see cref="View.GetContentSize ()"/>.
/// </param>
public record DimPercent (int Percentage, DimPercentMode Mode = DimPercentMode.ContentSize) : Dim
{
    /// <summary>
    /// </summary>
    /// <returns></returns>
    public override string ToString () => $"Percent({Percentage},{Mode})";

    /// <summary>
    ///     Gets whether the dimension is computed using the View's position or GetContentSize ().
    /// </summary>
    public DimPercentMode Mode { get; } = Mode;

    internal override int GetAnchor (int size) => (int)(size * (Percentage / 100f));

    internal override int Calculate (int location, int superviewContentSize, View us, Dimension dimension) =>
        Mode == DimPercentMode.Position ? Math.Max (GetAnchor (superviewContentSize - location), 0) : GetAnchor (superviewContentSize);

    /// <inheritdoc/>
    internal override bool DependsOnSuperViewContentSize => true;

    /// <inheritdoc/>
    internal override bool CanContributeToAutoSizing => false;
}
