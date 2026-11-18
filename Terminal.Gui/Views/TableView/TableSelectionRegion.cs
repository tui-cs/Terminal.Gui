namespace Terminal.Gui.Views;

/// <summary>Describes a single contiguous rectangular selection region within a <see cref="TableView"/>.</summary>
public class TableSelectionRegion : IEquatable<TableSelectionRegion>
{
    /// <summary>Creates a new selected area starting at the origin corner and covering the provided rectangular area.</summary>
    /// <param name="origin">The corner where the selection began.</param>
    /// <param name="rect">The rectangular area of the selection.</param>
    public TableSelectionRegion (Point origin, Rectangle rect)
    {
        Origin = origin;
        Rectangle = rect;
    }

    /// <summary>
    ///     <see langword="true"/> if the selection was made through <see cref="Command.ToggleExtend"/> (e.g. Ctrl+Click)
    ///     and therefore should persist even through keyboard navigation.
    /// </summary>
    public bool IsExtended { get; init; }

    /// <summary>Corner of the <see cref="Rectangle"/> where selection began.</summary>
    public Point Origin { get; init; }

    /// <summary>Area selected.</summary>
    public Rectangle Rectangle { get; init; }

    /// <inheritdoc/>
    public bool Equals (TableSelectionRegion? other)
    {
        if (other is null)
        {
            return false;
        }

        return Origin == other.Origin
               && Rectangle == other.Rectangle
               && IsExtended == other.IsExtended;
    }

    /// <inheritdoc/>
    public override bool Equals (object? obj) => Equals (obj as TableSelectionRegion);

    /// <inheritdoc/>
    public override int GetHashCode () => HashCode.Combine (Origin, Rectangle, IsExtended);
}
