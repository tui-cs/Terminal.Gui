namespace Terminal.Gui.Views;

public partial class ListView
{
    /// <summary>Gets or sets where a column footer band is drawn.</summary>
    public enum ColumnFooterPlacement
    {
        /// <summary>Pins the footer to the bottom of the <see cref="ListView"/>.</summary>
        Bottom = 0,

        /// <summary>Draws the footer on the rows after the last item.</summary>
        AfterLastItem
    }
}
