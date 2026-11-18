namespace Terminal.Gui.Views;

/// <summary>
///     Represents the complete selection state of a <see cref="TableView"/>: the cursor position and all
///     extended selection regions. Used as the <c>T</c> in <see cref="IValue{T}"/>.
/// </summary>
/// <remarks>
///     <para>A <see langword="null"/> <see cref="TableSelection"/> (as the value of <see cref="TableView.Value"/>) means
///     "no selection" — either no <see cref="TableView.Table"/> is assigned or the selection was explicitly cleared.</para>
///     <para>A non-null <see cref="TableSelection"/> always has a non-null <see cref="SelectedCell"/>.</para>
/// </remarks>
public class TableSelection : IEquatable<TableSelection>
{
    /// <summary>Creates a new <see cref="TableSelection"/> with the specified cursor and regions.</summary>
    /// <param name="cursor">The cursor cell position (navigation anchor). Must not be <see langword="null"/>.</param>
    /// <param name="regions">All extended selection regions (may be empty for cursor-only selection).</param>
    public TableSelection (Point cursor, IReadOnlyList<TableSelectionRegion>? regions)
    {
        SelectedCell = cursor;
        Regions = regions ?? [];
    }

    /// <summary>Creates a cursor-only <see cref="TableSelection"/> with no extended regions.</summary>
    /// <param name="cursor">The cursor cell position.</param>
    public TableSelection (Point cursor) : this (cursor, []) { }

    /// <summary>The selected cell used for navigation. Always non-null on a non-null <see cref="TableSelection"/>.</summary>
    public Point SelectedCell { get; }

    /// <summary>All extended selection regions. May be empty if only the cursor cell is selected.</summary>
    public IReadOnlyList<TableSelectionRegion> Regions { get; }

    /// <summary>Returns <see langword="true"/> if the given cell is within any of the <see cref="Regions"/>.</summary>
    public bool Contains (int col, int row) => Regions.Any (t => t.Rectangle.Contains (col, row));

    /// <inheritdoc/>
    public bool Equals (TableSelection? other)
    {
        if (other is null)
        {
            return false;
        }

        if (SelectedCell != other.SelectedCell)
        {
            return false;
        }

        if (Regions.Count != other.Regions.Count)
        {
            return false;
        }

        for (var i = 0; i < Regions.Count; i++)
        {
            if (!Regions [i].Equals (other.Regions [i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public override bool Equals (object? obj) => Equals (obj as TableSelection);

    /// <inheritdoc/>
    public override int GetHashCode ()
    {
        HashCode hash = new ();
        hash.Add (SelectedCell);

        foreach (TableSelectionRegion region in Regions)
        {
            hash.Add (region);
        }

        return hash.ToHashCode ();
    }
}
