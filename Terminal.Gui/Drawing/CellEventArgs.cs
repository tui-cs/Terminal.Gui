namespace Terminal.Gui.Drawing;

/// <summary>Args for events that relate to a specific <see cref="Cell"/>.</summary>
public record struct CellEventArgs
{
    /// <summary>Creates a new instance of the <see cref="CellEventArgs"/> class.</summary>
    /// <param name="line">The line.</param>
    /// <param name="col">The col index.</param>
    /// <param name="unwrappedPosition">The unwrapped row and col index.</param>
    public CellEventArgs (List<Cell> line, int col, (int Row, int Col) unwrappedPosition)
    {
        Line = line;
        Col = col;
        UnwrappedPosition = unwrappedPosition;
    }

    /// <summary>The index of the Cell in the line.</summary>
    public int Col { get; }

    /// <summary>The list of runes the Cell is part of.</summary>
    public List<Cell> Line { get; }

    /// <summary>
    ///     The unwrapped row and column index into the text containing the Cell. Unwrapped means the text without
    ///     word wrapping or other visual formatting having been applied.
    /// </summary>
    public (int Row, int Col) UnwrappedPosition { get; }
}
