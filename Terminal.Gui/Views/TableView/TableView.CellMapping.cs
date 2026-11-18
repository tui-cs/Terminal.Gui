namespace Terminal.Gui.Views;

/// <summary>
///     Displays and enables infinite scrolling through tabular data based on a <see cref="ITableSource"/>.
///     <a href="../docs/tableview.md">See the TableView Deep Dive for more</a>.
/// </summary>
public partial class TableView
{
    /// <summary>
    ///     Returns the column and row of <see cref="Table"/> that corresponds to a given point on the screen (relative
    ///     to the control client area).  Returns null if the point is in the header, no table is loaded or outside the control
    ///     bounds.
    /// </summary>
    /// <param name="clientX">X offset from the top left of the control.</param>
    /// <param name="clientY">Y offset from the top left of the control.</param>
    /// <returns>Cell clicked or null.</returns>
    public Point? ScreenToCell (int clientX, int clientY) => ScreenToCell (clientX, clientY, out _, out _);

    /// <summary>
    ///     Returns the column and row of <see cref="Table"/> that corresponds to a given point on the screen (relative
    ///     to the control client area).  Returns null if the point is in the header, no table is loaded or outside the control
    ///     bounds.
    /// </summary>
    /// <param name="clientX">X offset from the top left of the control.</param>
    /// <param name="clientY">Y offset from the top left of the control.</param>
    /// <param name="headerIfAny">If the click is in a header this is the column clicked.</param>
    public Point? ScreenToCell (int clientX, int clientY, out int? headerIfAny) => ScreenToCell (clientX, clientY, out headerIfAny, out _);

    /// <summary>
    ///     Returns the column and row of <see cref="Table"/> that corresponds to a given point on the screen (relative
    ///     to the control client area).  Returns null if the point is in the header, no table is loaded or outside the control
    ///     bounds.
    /// </summary>
    /// <param name="client">offset from the top left of the control.</param>
    /// <param name="headerIfAny">If the click is in a header this is the column clicked.</param>
    public Point? ScreenToCell (Point client, out int? headerIfAny) => ScreenToCell (client, out headerIfAny, out _);

    /// <summary>
    ///     Returns the column and row of <see cref="Table"/> that corresponds to a given point on the screen (relative
    ///     to the control client area).  Returns null if the point is in the header, no table is loaded or outside the control
    ///     bounds.
    /// </summary>
    /// <param name="clientX">X offset from the top left of the control.</param>
    /// <param name="clientY">Y offset from the top left of the control.</param>
    /// <param name="headerIfAny">If the click is in a header this is the column clicked.</param>
    /// <param name="offsetX">The horizontal offset of the click within the returned cell.</param>
    public Point? ScreenToCell (int clientX, int clientY, out int? headerIfAny, out int? offsetX)
    {
        headerIfAny = null;
        offsetX = null;

        if (TableIsNullOrInvisible ())
        {
            return null;
        }

        ColumnToRender [] cellInfos = NonHiddenCellInfos ();
        int rowIdx;

        int currentHeaderHeightVisible = CurrentHeaderHeightVisible ();
        ColumnToRender? col = cellInfos.LastOrDefault (c => c.X <= clientX + Viewport.X);
        offsetX = clientX + Viewport.X - col?.X;

        if (clientY < currentHeaderHeightVisible)
        {
            // header clicked
            headerIfAny = col?.Column;
        }

        if (Style.AlwaysShowHeaders)
        {
            rowIdx = clientY - currentHeaderHeightVisible + Viewport.Y;
        }
        else
        {
            rowIdx = clientY + Viewport.Y - GetHeaderHeightIfAny ();
        }

        // if click is off bottom of the rows don't give an
        // invalid index back to user!
        if (rowIdx >= Table!.Rows)
        {
            return null;
        }

        if (col is not { } || rowIdx < 0)
        {
            return null;
        }

        offsetX = clientX - col.X;

        return new Point (col.Column, rowIdx);
    }

    /// <summary>
    ///     Returns the column and row of <see cref="Table"/> that corresponds to a given point on the screen (relative
    ///     to the control client area).  Returns null if the point is in the header, no table is loaded or outside the control
    ///     bounds.
    /// </summary>
    /// <param name="client">offset from the top left of the control.</param>
    /// <param name="headerIfAny">If the click is in a header this is the column clicked.</param>
    /// <param name="offsetX">The horizontal offset of the click within the returned cell.</param>
    public Point? ScreenToCell (Point client, out int? headerIfAny, out int? offsetX) => ScreenToCell (client.X, client.Y, out headerIfAny, out offsetX);
}
