namespace Terminal.Gui.ViewBase;

/// <summary>
///     Represents a function <see cref="Dim"/> object that computes the dimension based on the passed view and by
///     executing the provided function.
/// </summary>
/// <remarks>
///     This is a low-level API that is typically used internally by the layout system. Use the various static
///     methods on the <see cref="Dim"/> class to create <see cref="Dim"/> objects instead.
/// </remarks>
/// <param name="Fn">The function that computes the dimension. If this function throws <see cref="LayoutException"/>... </param>
/// <param name="View">The <see cref="Dim"/> returned from the function based on the passed view.</param>
public record DimFunc (Func<View?, int> Fn, View? View = null) : Dim
{
    /// <summary>
    ///     Gets the function that computes the dimension.
    /// </summary>
    public Func<View?, int> Fn { get; } = Fn;

    /// <summary>
    ///     Gets the passed view that the dimension is based on.
    /// </summary>
    public View? View { get; } = View;

    /// <inheritdoc/>
    public override string ToString () => $"DimFunc({Fn (View)})";

    internal override int GetAnchor (int size) => Fn (View);

    /// <inheritdoc/>
    internal override IEnumerable<View> GetReferencedViews ()
    {
        if (View is { })
        {
            yield return View;
        }
    }

    /// <inheritdoc/>
    internal override bool IsFixed => true;
}
