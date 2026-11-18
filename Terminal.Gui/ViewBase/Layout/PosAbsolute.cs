namespace Terminal.Gui.ViewBase;

/// <summary>
///     Represents an absolute position in the layout. This is used to specify a fixed position in the layout.
/// </summary>
/// <remarks>
///     <para>
///         This is a low-level API that is typically used internally by the layout system. Use the various static
///         methods on the <see cref="Pos"/> class to create <see cref="Pos"/> objects instead.
///     </para>
/// </remarks>
/// <param name="Position"></param>
public record PosAbsolute (int Position) : Pos
{
    /// <summary>
    ///     The position of the <see cref="View"/> in the layout.
    /// </summary>
    public int Position { get; } = Position;

    /// <inheritdoc/>
    public override string ToString () => $"Absolute({Position})";

    internal override int GetAnchor (int size) => Position;

    /// <inheritdoc/>
    internal override bool IsFixed => true;
}
