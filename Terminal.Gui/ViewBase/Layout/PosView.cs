namespace Terminal.Gui.ViewBase;

/// <summary>
///     Represents a position that is anchored to the side of another view.
/// </summary>
/// <remarks>
///     <para>
///         This is a low-level API that is typically used internally by the layout system. Use the various static
///         methods on the <see cref="Pos"/> class to create <see cref="Pos"/> objects instead.
///     </para>
/// </remarks>
public record PosView : Pos
{
    /// <summary>
    ///     Represents a position that is anchored to the side of another view.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is a low-level API that is typically used internally by the layout system. Use the various static
    ///         methods on the <see cref="Pos"/> class to create <see cref="Pos"/> objects instead.
    ///     </para>
    /// </remarks>
    /// <param name="view">The View the position is anchored to.</param>
    /// <param name="side">The side of the View the position is anchored to.</param>
    public PosView (View view, Side side)
    {
        ArgumentNullException.ThrowIfNull (view);
        Target = view;
        Side = side;
    }

    /// <summary>
    ///     Gets the View the position is anchored to.
    /// </summary>
    public View Target { get; }

    /// <summary>
    ///     Gets the side of the View the position is anchored to.
    /// </summary>
    public Side Side { get; }

    /// <inheritdoc/>
    public override string ToString ()
    {
        var sideString = Side.ToString ();

        if (Target == null)
        {
            throw new NullReferenceException (nameof (Target));
        }

        return $"View(Side={sideString},Target={Target})";
    }

    internal override int GetAnchor (int size) =>
        Side switch
        {
            Side.Left => Target.Frame.X,
            Side.Top => Target.Frame.Y,
            Side.Right => Target.Frame.Right,
            Side.Bottom => Target.Frame.Bottom,
            _ => 0
        };

    internal override bool ReferencesOtherViews () => true;

    /// <inheritdoc/>
    internal override IEnumerable<View> GetReferencedViews ()
    {
        yield return Target;
    }

    /// <inheritdoc/>
    internal override bool RequiresTargetLayout => true;
}
