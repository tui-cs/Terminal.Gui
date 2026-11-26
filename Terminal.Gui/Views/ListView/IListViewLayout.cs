namespace Terminal.Gui.Views;

/// <summary>
///     Optional layout pass <see cref="ListView"/> invokes before drawing multi-line items and column headers.
/// </summary>
internal interface IListViewLayout
{
    /// <summary>
    ///     Measures content such as column widths before the list is drawn.
    /// </summary>
    /// <param name="listView">The <see cref="ListView"/> that is about to draw.</param>
    /// <param name="width">The width available for item content, excluding mark columns.</param>
    void PrepareRender (ListView listView, int width);

    /// <summary>
    ///     Gets how many rows <paramref name="item"/> needs when <see cref="ListView.ItemAutoHeight"/> is
    ///     <see langword="true"/>. The result is at least one.
    /// </summary>
    /// <param name="listView">The <see cref="ListView"/> that is measuring the item.</param>
    /// <param name="item">The item index.</param>
    /// <returns>The row count for that item.</returns>
    int MeasureItemHeight (ListView listView, int item);

    /// <summary>Draws the column footer band.</summary>
    /// <param name="listView">The <see cref="ListView"/> that is drawing.</param>
    /// <param name="col">The first content column, after any mark columns.</param>
    /// <param name="line">The first row of the footer band.</param>
    /// <param name="width">The width available for footer content.</param>
    /// <param name="viewportX">The horizontal scroll offset.</param>
    void RenderColumnFooter (ListView listView, int col, int line, int width, int viewportX);
}
