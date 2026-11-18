namespace Terminal.Gui.ViewBase;

/// <summary>
///     Represents a dimension that tracks the Height or Width of the specified View.
/// </summary>
/// <remarks>
///     This is a low-level API that is typically used internally by the layout system. Use the various static
///     methods on the <see cref="Dim"/> class to create <see cref="Dim"/> objects instead.
/// </remarks>
public record DimView : Dim
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DimView"/> class.
    /// </summary>
    /// <param name="view">The view the dimension is anchored to.</param>
    /// <param name="dimension">Indicates which dimension is tracked.</param>
    public DimView (View? view, Dimension dimension)
    {
        Target = view;
        Dimension = dimension;
    }

    /// <summary>
    ///     Gets the indicated dimension of the View.
    /// </summary>
    public Dimension Dimension { get; }

    /// <summary>
    ///     Gets the View the dimension is anchored to.
    /// </summary>
    public View? Target { get; init; }

    /// <inheritdoc/>
    public override string ToString ()
    {
        if (Target == null)
        {
            throw new NullReferenceException ();
        }

        return $"View({Dimension},{Target})";
    }

    internal override int GetAnchor (int size) =>
        Dimension switch
        {
            Dimension.Height => Target?.Frame.Height ?? 0,
            Dimension.Width => Target?.Frame.Width ?? 0,
            _ => 0
        };

    internal override bool ReferencesOtherViews () => true;

    /// <inheritdoc/>
    internal override IEnumerable<View> GetReferencedViews ()
    {
        if (Target is { })
        {
            yield return Target;
        }
    }

    /// <inheritdoc/>
    internal override bool RequiresTargetLayout => true;
}
