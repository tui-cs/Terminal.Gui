namespace Terminal.Gui.ViewBase;

/// <summary>
///     Represents a dimension that is a combination of two other dimensions.
/// </summary>
/// <param name="Add">
///     Indicates whether the two dimensions are added or subtracted.
/// </param>
/// <remarks>
///     This is a low-level API that is typically used internally by the layout system. Use the various static
///     methods on the <see cref="Dim"/> class to create <see cref="Dim"/> objects instead.
/// </remarks>
/// <param name="Left">The left dimension.</param>
/// <param name="Right">The right dimension.</param>
public record DimCombine (AddOrSubtract Add, Dim Left, Dim Right) : Dim
{
    /// <summary>
    ///     Gets whether the two dimensions are added or subtracted.
    /// </summary>
    public AddOrSubtract Add { get; } = Add;

    /// <summary>
    ///     Gets the left dimension.
    /// </summary>
    public Dim Left { get; } = Left;

    /// <summary>
    ///     Gets the right dimension.
    /// </summary>
    public Dim Right { get; } = Right;

    /// <inheritdoc/>
    public override string ToString () => $"Combine({Left}{(Add == AddOrSubtract.Add ? '+' : '-')}{Right})";

    internal override int GetAnchor (int size)
    {
        if (Add == AddOrSubtract.Add)
        {
            return Left.GetAnchor (size) + Right.GetAnchor (size);
        }

        return Left.GetAnchor (size) - Right.GetAnchor (size);
    }

    internal override int Calculate (int location, int superviewContentSize, View us, Dimension dimension)
    {
        int newDimension;

        if (Add == AddOrSubtract.Add)
        {
            newDimension = Left.Calculate (location, superviewContentSize, us, dimension) + Right.Calculate (location, superviewContentSize, us, dimension);
        }
        else
        {
            newDimension = Math.Max (0,
                                     Left.Calculate (location, superviewContentSize, us, dimension)
                                     - Right.Calculate (location, superviewContentSize, us, dimension));
        }

        return newDimension;
    }

    internal override bool ReferencesOtherViews ()
    {
        if (Left.ReferencesOtherViews ())
        {
            return true;
        }

        if (Right.ReferencesOtherViews ())
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    internal override IEnumerable<View> GetReferencedViews ()
    {
        foreach (View view in Left.GetReferencedViews ())
        {
            yield return view;
        }

        foreach (View view in Right.GetReferencedViews ())
        {
            yield return view;
        }
    }

    /// <inheritdoc/>
    internal override bool DependsOnSuperViewContentSize => Left.DependsOnSuperViewContentSize || Right.DependsOnSuperViewContentSize;

    /// <inheritdoc/>
    internal override bool CanContributeToAutoSizing => Left.CanContributeToAutoSizing || Right.CanContributeToAutoSizing;

    /// <inheritdoc/>
    internal override int GetMinimumContribution (int location, int superviewContentSize, View us, Dimension dimension)
    {
        int newDimension;

        if (Add == AddOrSubtract.Add)
        {
            newDimension = Left.GetMinimumContribution (location, superviewContentSize, us, dimension)
                           + Right.GetMinimumContribution (location, superviewContentSize, us, dimension);
        }
        else
        {
            newDimension = Math.Max (
                                     0,
                                     Left.GetMinimumContribution (location, superviewContentSize, us, dimension)
                                     - Right.GetMinimumContribution (location, superviewContentSize, us, dimension)
                                    );
        }

        return newDimension;
    }

    /// <inheritdoc/>
    internal override bool IsFixed => Left.IsFixed && Right.IsFixed;

    /// <inheritdoc/>
    internal override bool RequiresTargetLayout => Left.RequiresTargetLayout || Right.RequiresTargetLayout;

    /// <inheritdoc/>
    protected override bool HasInner<TDim> (out TDim dim) => Left.Has (out dim) || Right.Has (out dim);
}
