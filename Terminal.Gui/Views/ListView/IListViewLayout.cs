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
}
