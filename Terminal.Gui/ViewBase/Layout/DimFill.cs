namespace Terminal.Gui.ViewBase;

/// <summary>
///     Represents a dimension that fills the dimension, leaving the specified margin.
/// </summary>
/// <remarks>
///     <para>
///         This is a low-level API that is typically used internally by the layout system. Use the various static
///         methods on the <see cref="Dim"/> class to create <see cref="Dim"/> objects instead.
///     </para>
///     <para>
///         When the SuperView uses <see cref="DimAuto"/>, a <see cref="DimFill"/> SubView does <b>not</b> contribute
///         to the auto-sizing calculation by default. Because <see cref="DimFill"/> derives its size from the
///         SuperView's ContentSize, and <see cref="DimAuto"/> computes the ContentSize from its SubViews, a circular
///         dependency arises: the <see cref="DimFill"/> SubView will be sized to 0 unless
///         <see cref="MinimumContentDim"/> is specified.
///     </para>
///     <para>
///         Set <see cref="MinimumContentDim"/> to ensure the SubView contributes a minimum size to the auto-sizing
///         calculation. See the <a href="../docs/dimauto.md">Dim.Auto Deep Dive</a> for details.
///     </para>
/// </remarks>
/// <param name="Margin">The margin to not fill.</param>
/// <param name="MinimumContentDim">
///     The minimum dimension the filled view will be constrained to. When the SuperView uses <see cref="DimAuto"/>,
///     this minimum will contribute to the auto-sizing calculation, ensuring the SuperView is at least large enough
///     to accommodate the minimum.
/// </param>
/// <param name="To">
///     The view to fill up to. When specified, the fill will stop at the position of this view (either its X or Y
///     coordinate depending on the dimension being calculated).
/// </param>
public record DimFill (Dim Margin, Dim? MinimumContentDim = null, View? To = null) : Dim
{
    /// <inheritdoc/>
    public override string ToString ()
    {
        StringBuilder result = new ();
        result.Append ("Fill(");
        result.Append (Margin);

        if (MinimumContentDim is { })
        {
            result.Append (",min:");
            result.Append (MinimumContentDim);
        }

        if (To is { })
        {
            result.Append (",to:");
            result.Append (To);
        }

        result.Append (')');

        return result.ToString ();
    }

    internal override int GetAnchor (int size) => size - Margin.GetAnchor (0);

    internal override int Calculate (int location, int superviewContentSize, View us, Dimension dimension)
    {
        int endPos = superviewContentSize;

        if (To is { })
        {
            endPos = dimension == Dimension.Width ? To.Frame.X : To.Frame.Y;
        }

        int fillSize = endPos - location - Margin.GetAnchor (0);

        if (MinimumContentDim is null)
        {
            return int.Max (fillSize, 0);
        }
        int minSize = MinimumContentDim.Calculate (location, superviewContentSize, us, dimension);
        fillSize = int.Max (fillSize, minSize);

        return int.Max (fillSize, 0);
    }

    /// <inheritdoc/>
    internal override IEnumerable<View> GetReferencedViews ()
    {
        if (To is { })
        {
            yield return To;
        }
    }

    /// <inheritdoc/>
    internal override bool DependsOnSuperViewContentSize => true;

    /// <inheritdoc/>
    internal override bool CanContributeToAutoSizing => MinimumContentDim is { } || To is { };

    /// <inheritdoc/>
    internal override int GetMinimumContribution (int location, int superviewContentSize, View us, Dimension dimension)
    {
        // DimFill's minimum contribution depends on MinimumContentDim and To
        if (MinimumContentDim is { })
        {
            // Return the minimum content dimension
            return MinimumContentDim.Calculate (0, superviewContentSize, us, dimension);
        }

        // If only To is set (without MinimumContentDim), we need special handling in DimAuto
        // because the contribution depends on the To view's position
        return 0;
    }

    /// <inheritdoc/>
    protected override bool HasInner<TDim> (out TDim dim)
    {
        if (Margin.Has (out dim))
        {
            return true;
        }

        if (MinimumContentDim is { } && MinimumContentDim.Has (out dim))
        {
            return true;
        }

        dim = null!;

        return false;
    }
}
