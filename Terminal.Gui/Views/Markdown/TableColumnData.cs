namespace Terminal.Gui.Views;

/// <summary>Describes a table column's alignment and optional preferred content width.</summary>
public sealed class TableColumnData
{
    /// <summary>Initializes a column description.</summary>
    /// <param name="alignment">Horizontal alignment within the cell.</param>
    /// <param name="preferredWidthPercentage">Preferred percentage of content space; zero or null means automatic.</param>
    /// <exception cref="ArgumentOutOfRangeException">The preference is negative or nonfinite.</exception>
    public TableColumnData (Alignment alignment = Alignment.Start, double? preferredWidthPercentage = null)
    {
        if (preferredWidthPercentage is { } width && (!double.IsFinite (width) || width < 0))
        {
            throw new ArgumentOutOfRangeException (nameof (preferredWidthPercentage));
        }

        Alignment = alignment;
        PreferredWidthPercentage = preferredWidthPercentage;
    }

    /// <summary>Gets the horizontal alignment.</summary>
    public Alignment Alignment { get; }

    /// <summary>Gets the preferred percentage, excluding cell padding and borders.</summary>
    public double? PreferredWidthPercentage { get; }
}
