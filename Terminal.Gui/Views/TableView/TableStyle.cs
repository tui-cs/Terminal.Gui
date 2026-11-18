namespace Terminal.Gui.Views;

/// <summary>
///     Defines rendering options that affect how the table is displayed.
///     <a href="../docs/tableview.md">See TableView Deep Dive for more information</a>.
/// </summary>
public class TableStyle
{
    /// <summary>When scrolling down always lock the column headers in place as the first row of the table</summary>
    public bool AlwaysShowHeaders { get; set; }

    /// <summary>
    ///     Gets or sets a flag indicating whether to force <see cref="Scheme.Normal"/> use when rendering vertical
    ///     cell lines (even when <see cref="TableView.FullRowSelect"/> is on).
    /// </summary>
    public bool AlwaysUseNormalColorForVerticalCellLines { get; set; }

    /// <summary>
    ///     Collection of columns for which you want special rendering (e.g. custom column lengths, text justification,
    ///     etc.)
    /// </summary>
    public Dictionary<int, ColumnStyle> ColumnStyles { get; set; } = new ();

    /// <summary>
    ///     Determines rendering when the last column in the table is visible, but its content or
    ///     <see cref="ColumnStyle.MaxWidth"/> is less than the remaining space in the control.  True (the default) will expand
    ///     the column to fill the remaining bounds of the control.  False will draw a column ending line and leave a blank
    ///     column that cannot be selected in the remaining space.
    /// </summary>
    public bool ExpandLastColumn { get; set; } = true;

    /// <summary>
    ///     True to invert the colors of the first symbol of the selected cell in the <see cref="TableView"/>. This gives
    ///     the appearance of a cursor for when the <see cref="IDriver"/> doesn't otherwise show this.
    /// </summary>
    public bool InvertSelectedCellFirstCharacter { get; set; }

    /// <summary>
    ///     Delegate for coloring specific rows in a different color.  For cell color see
    ///     <see cref="ColumnStyle.ColorGetter"/>.
    /// </summary>
    public RowColorGetterDelegate? RowColorGetter { get; set; }

    /// <summary>
    ///     Gets or sets a base <see cref="Scheme"/> applied to all column headers. Falls back to the view's scheme if
    ///     <see langword="null"/>. Per-column overrides can be specified via <see cref="ColumnStyle.HeaderColorGetter"/>.
    /// </summary>
    public Scheme? HeaderScheme { get; set; }

    /// <summary>
    ///     Gets or sets a flag indicating whether to render headers of a <see cref="TableView"/>. Defaults to
    ///     <see langword="true"/>.
    /// </summary>
    /// <remarks>
    ///     <see cref="ShowHorizontalHeaderOverline"/>, <see cref="ShowHorizontalHeaderUnderline"/> etc may still be used
    ///     even if <see cref="ShowHeaders"/> is <see langword="false"/>.
    /// </remarks>
    public bool ShowHeaders { get; set; } = true;

    /// <summary>
    ///     Gets or sets a flag indicating whether there should be a horizontal line after all the data in the table.
    ///     Defaults to <see langword="false"/>.
    /// </summary>
    public bool ShowHorizontalBottomLine { get; set; }

    /// <summary>True to render a solid line above the headers</summary>
    public bool ShowHorizontalHeaderOverline { get; set; } = true;

    /// <summary>True to render a solid line under the headers</summary>
    public bool ShowHorizontalHeaderUnderline { get; set; } = true;

    /// <summary>True to render a solid line vertical line between cells</summary>
    public bool ShowVerticalCellLines { get; set; } = true;

    /// <summary>
    ///     Gets or sets whether the left-most visible column renders a vertical line on its left side when vertical
    ///     lines are enabled.
    /// </summary>
    public bool ShowVerticalCellLineForFirstColumn { get; set; } = true;

    /// <summary>
    ///     Gets or sets whether the right-most visible column renders a vertical line on its right side when vertical
    ///     lines are enabled.
    /// </summary>
    public bool ShowVerticalCellLineForLastColumn { get; set; } = true;

    /// <summary>True to render a solid line vertical line between headers</summary>
    public bool ShowVerticalHeaderLines { get; set; } = true;

    /// <summary>
    ///     <para>
    ///         Determines how <see cref="TableView.ColumnOffset"/> is updated when scrolling right off the end of the
    ///         currently visible area.
    ///     </para>
    ///     <para>
    ///         If true then when scrolling right the scroll offset is increased the minimum required to show the new column.
    ///         This may be slow if you have an incredibly large number of columns in your table and/or slow
    ///         <see cref="ColumnStyle.RepresentationGetter"/> implementations
    ///     </para>
    ///     <para>If false then scroll offset is set to the currently selected column (i.e. PageRight).</para>
    /// </summary>
    public bool SmoothHorizontalScrolling { get; set; } = true;

    /// <summary>
    ///     Returns the entry from <see cref="ColumnStyles"/> for the given <paramref name="col"/> or null if no custom
    ///     styling is defined for it
    /// </summary>
    /// <param name="col"></param>
    /// <returns></returns>
    public ColumnStyle? GetColumnStyleIfAny (int col) => ColumnStyles.GetValueOrDefault (col);

    /// <summary>
    ///     Returns an existing <see cref="ColumnStyle"/> for the given <paramref name="col"/> or creates a new one with
    ///     default options
    /// </summary>
    /// <param name="col"></param>
    /// <returns></returns>
    public ColumnStyle GetOrCreateColumnStyle (int col)
    {
        if (!ColumnStyles.ContainsKey (col))
        {
            ColumnStyles.Add (col, new ColumnStyle ());
        }

        return ColumnStyles [col];
    }
}
