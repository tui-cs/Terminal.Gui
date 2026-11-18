namespace Terminal.Gui.ViewBase;

/// <summary>
///     Represents a position that is a percentage of the width or height of the SuperView.
/// </summary>
/// <remarks>
///     <para>
///         This is a low-level API that is typically used internally by the layout system. Use the various static
///         methods on the <see cref="Pos"/> class to create <see cref="Pos"/> objects instead.
///     </para>
/// </remarks>
/// <param name="Percent"></param>
public record PosPercent (int Percent) : Pos
{
    /// <summary>
    ///     Gets the percentage of the width or height of the SuperView.
    /// </summary>
    public new int Percent { get; } = Percent;

    /// <inheritdoc/>
    public override string ToString () => $"Percent({Percent})";

    internal override int GetAnchor (int size) => (int)(size * (Percent / 100f));
}
