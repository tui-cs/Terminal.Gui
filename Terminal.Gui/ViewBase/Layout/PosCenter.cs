namespace Terminal.Gui.ViewBase;

/// <summary>
///     Represents a position that is centered.
/// </summary>
public record PosCenter : Pos
{
    /// <inheritdoc/>
    public override string ToString () => "Center";

    internal override int GetAnchor (int size) => size / 2;

    internal override int Calculate (int superviewDimension, Dim dim, View us, Dimension dimension)
    {
        // Protect against negative dimensions
        int newDimension = Math.Max (dim.Calculate (0, superviewDimension, us, dimension), 0);

        return (superviewDimension - newDimension) / 2;
    }
}
