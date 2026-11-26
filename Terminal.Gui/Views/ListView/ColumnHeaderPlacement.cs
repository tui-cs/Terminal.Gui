namespace Terminal.Gui.Views;

public partial class ListView
{
    /// <summary>
    ///     Gets or sets where a column header band is drawn.
    ///     <a href="../docs/listview.md">See the ListView Deep Dive for more</a>.
    /// </summary>
    public enum ColumnHeaderPlacement
    {
        /// <summary>Pins the header to the top of the <see cref="ListView"/>.</summary>
        Top = 0,

        /// <summary>Draws the header on the rows before the first item.</summary>
        BeforeFirstItem
    }
}
