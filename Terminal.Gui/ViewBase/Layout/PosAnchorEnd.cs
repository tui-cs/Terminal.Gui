namespace Terminal.Gui.ViewBase;

/// <summary>
///     Represents a position anchored to the end (right side or bottom) of the SuperView's content area.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="PosAnchorEnd"/> positions a view relative to the end (right edge for X, bottom edge for Y)
///         of the SuperView's content area, accounting for the view's own dimension.
///     </para>
///     <para>
///         When used within a SuperView that has <see cref="Dim.Auto"/>, <see cref="PosAnchorEnd"/> actively
///         contributes to determining the SuperView's content size. The <see cref="Dim.Auto"/> algorithm calculates
///         the minimum content size needed to accommodate views positioned with <see cref="PosAnchorEnd"/>.
///     </para>
///     <para>
///         <b>How PosAnchorEnd affects Dim.Auto:</b>
///     </para>
///     <code>
///     View superView = new () { Width = Dim.Auto () };
///     
///     // This label contributes ~10 to the SuperView's width
///     Label label = new () { Text = "Name:" };
///     superView.Add (label);
///     
///     // This TextField does NOT force the SuperView wider - Dim.Fill doesn't contribute to Dim.Auto
///     TextField field = new () { Y = 1, Width = Dim.Fill () };
///     superView.Add (field);
///     
///     // This button WILL force the SuperView to be wide enough to accommodate it at the right edge
///     Button button = new ()
///     {
///         Text = "OK",
///         X = Pos.AnchorEnd (),  // ← SuperView width must accommodate this position!
///         Y = 2
///     };
///     superView.Add (button);
///     
///     // Result: SuperView width = Max (label width, button position + button width)
///     //         The TextField fills whatever width the SuperView ends up being
///     </code>
///     <para>
///         The calculation works by determining the content size needed such that when the view is positioned
///         at the end, it still fits within the content area. See <see cref="DimAuto"/> for details on how
///         the auto-sizing algorithm processes anchored views.
///     </para>
///     <para>
///         This is a low-level API that is typically used internally by the layout system. Use the various static
///         methods on the <see cref="Pos"/> class to create <see cref="Pos"/> objects instead.
///     </para>
/// </remarks>
/// <example>
///     <code>
///     // Position a button 2 columns from the right edge
///     button.X = Pos.AnchorEnd (2);
///     
///     // Position a view exactly at the bottom (accounting for its height)
///     view.Y = Pos.AnchorEnd ();
///     </code>
/// </example>
public record PosAnchorEnd : Pos
{
    /// <summary>
    ///     Gets the offset of the position from the right/bottom edge of the SuperView's content area.
    /// </summary>
    /// <remarks>
    ///     When <see cref="UseDimForOffset"/> is <see langword="true"/>, this value is ignored and the
    ///     view's dimension is used as the offset instead.
    /// </remarks>
    public int Offset { get; }

    /// <summary>
    ///     Constructs a new position anchored to the end (right side or bottom) of the SuperView's content area,
    ///     offset by the view's respective dimension. This positions the view so its right/bottom edge aligns
    ///     with the SuperView's right/bottom edge.
    /// </summary>
    /// <remarks>
    ///     This is equivalent to using <c>Pos.AnchorEnd (0)</c>, but uses the view's calculated dimension
    ///     as the offset, ensuring the view fits exactly at the end.
    /// </remarks>
    public PosAnchorEnd () => UseDimForOffset = true;

    /// <summary>
    ///     Constructs a new position anchored to the end (right side or bottom) of the SuperView's content area,
    ///     with a specified offset from that edge.
    /// </summary>
    /// <param name="offset">
    ///     The number of columns (for X) or rows (for Y) from the right/bottom edge. Positive values move the
    ///     view away from the edge (towards the start), creating space between the view and the edge.
    /// </param>
    /// <example>
    ///     <code>
    ///     // Position button 2 columns from the right edge
    ///     button.X = Pos.AnchorEnd (2);
    ///     // If SuperView width is 80, button will be at X = 80 - 2 = 78
    ///     </code>
    /// </example>
    public PosAnchorEnd (int offset) => Offset = offset;

    /// <summary>
    ///     If <see langword="true"/>, the offset is the width/height of the view.
    ///     If <see langword="false"/>, the offset is the <see cref="Offset"/> value.
    /// </summary>
    /// <remarks>
    ///     When <see langword="true"/> (set by the parameterless constructor), the view is positioned such that
    ///     its right/bottom edge aligns with the SuperView's right/bottom edge. This is the most common use case.
    /// </remarks>
    public bool UseDimForOffset { get; }

    /// <inheritdoc/>
    public override string ToString () => UseDimForOffset ? "AnchorEnd" : $"AnchorEnd({Offset})";

    internal override int GetAnchor (int size)
    {
        if (UseDimForOffset)
        {
            return size;
        }

        return size - Offset;
    }

    internal override int Calculate (int superviewDimension, Dim dim, View us, Dimension dimension)
    {
        int newLocation = GetAnchor (superviewDimension);

        if (UseDimForOffset)
        {
            newLocation -= dim.GetAnchor (superviewDimension);
        }

        return newLocation;
    }

    /// <inheritdoc/>
    internal override bool DependsOnSuperViewContentSize => true;
}
