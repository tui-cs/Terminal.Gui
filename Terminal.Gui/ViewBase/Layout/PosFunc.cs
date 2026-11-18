namespace Terminal.Gui.ViewBase;

/// <summary>
///     Represents a position that is computed by executing a function that returns an integer position.
/// </summary>
/// <remarks>
///     This is a low-level API that is typically used internally by the layout system. Use the various static
///     methods on the <see cref="Pos"/> class to create <see cref="Pos"/> objects instead.
/// </remarks>
/// <param name="Fn">The function that computes the position. If this function throws <see cref="LayoutException"/>... </param>
/// <param name="View">The <see cref="Pos"/> returned from the function based on the passed view.</param>
public record PosFunc (Func<View?, int> Fn, View? View = null) : Pos
{
    /// <summary>
    ///     Gets the function that computes the position.
    /// </summary>
    public Func<View?, int> Fn { get; } = Fn;

    /// <summary>
    ///     Gets the passed view that the position is based on.
    /// </summary>
    public View? View { get; } = View;

    /// <inheritdoc/>
    public override string ToString () => $"PosFunc({Fn (View)})";

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
