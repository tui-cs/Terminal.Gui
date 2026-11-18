namespace Terminal.Gui.Views;

/// <summary>
///     Describes how to render a given column in  a <see cref="TableView"/> including <see cref="Alignment"/> and
///     textual representation of cells (e.g. date formats)
///     <a href="../docs/tableview.md">See TableView Deep Dive for more information</a>.
/// </summary>
public class ColumnStyle
{
    /// <summary>
    ///     Defines a delegate for returning custom alignment per cell based on cell values. When specified this will
    ///     override <see cref="Alignment"/>
    /// </summary>
    public Func<object, Alignment>? AlignmentGetter { get; set; }

    /// <summary>
    ///     Defines a delegate for returning a custom scheme per cell based on cell values. Return null for the
    ///     default
    /// </summary>
    public CellColorGetterDelegate? ColorGetter { get; set; }

    /// <summary>
    ///     Defines a delegate for returning a custom scheme for this column's header. Return <see langword="null"/> to
    ///     fall back to <see cref="TableStyle.HeaderScheme"/> or the view's default scheme.
    /// </summary>
    public HeaderColorGetterDelegate? HeaderColorGetter { get; set; }

    /// <summary>
    ///     Defines a delegate for returning custom representations of cell values. If not set then
    ///     <see cref="object.ToString()"/> is used. Return values from your delegate may be truncated e.g. based on
    ///     <see cref="MaxWidth"/>
    /// </summary>
    public Func<object, string>? RepresentationGetter { get; set; }

    /// <summary>
    ///     Defines the default alignment for all values rendered in this column. For custom alignment based on cell
    ///     contents use <see cref="AlignmentGetter"/>.
    /// </summary>
    public Alignment Alignment { get; set; }

    /// <summary>Defines the format for values e.g. "yyyy-MM-dd" for dates</summary>
    public string? Format { get; set; }

    /// <summary>
    ///     Set the maximum width of the column in characters. This value will be ignored if more than the tables
    ///     <see cref="TableView.MaxCellWidth"/>. Defaults to <see cref="TableView.DEFAULT_MAX_CELL_WIDTH"/>
    /// </summary>
    public int MaxWidth { get; set; } = TableView.DEFAULT_MAX_CELL_WIDTH;

    /// <summary>Enables flexible sizing of this column based on available screen space to render into.</summary>
    public int MinAcceptableWidth { get; set; } = TableView.DEFAULT_MIN_ACCEPTABLE_WIDTH;

    /// <summary>
    ///     Set the minimum width of the column in characters. Setting this will ensure that even when a column has short
    ///     content/header it still fills a given width of the control.
    ///     <para>
    ///         This value will be ignored if more than the tables <see cref="TableView.MaxCellWidth"/> or the
    ///         <see cref="MaxWidth"/>
    ///     </para>
    ///     <remarks>For setting a flexible column width (down to a lower limit) use <see cref="MinAcceptableWidth"/> instead</remarks>
    /// </summary>
    public int MinWidth { get; set; }

    /// <summary>
    ///     Gets or Sets a value indicating whether the column should be visible to the user. This affects both whether it
    ///     is rendered and whether it can be selected. Defaults to true.
    /// </summary>
    /// <remarks>If <see cref="MaxWidth"/> is 0 then <see cref="Visible"/> will always return false.</remarks>
    public bool Visible { get => MaxWidth >= 0 && field; set; } = true;

    /// <summary>
    ///     Gets or sets the string appended to a cell value (or column header) when its rendered representation does
    ///     not fit in the available column width. Defaults to <see cref="Glyphs.HorizontalEllipsis"/> ("…").
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Set to <see langword="null"/> or an empty string to suppress the indicator and silently clip the value at
    ///         the available width.
    ///     </para>
    ///     <para>
    ///         If the indicator itself is wider than the available column width the value is silently clipped (no
    ///         indicator is drawn).
    ///     </para>
    /// </remarks>
    public string? TruncationIndicator { get; set; } = Glyphs.HorizontalEllipsis.ToString ();

    /// <summary>
    ///     Returns the alignment for the cell based on <paramref name="cellValue"/> and <see cref="AlignmentGetter"/>/
    ///     <see cref="Alignment"/>
    /// </summary>
    /// <param name="cellValue"></param>
    /// <returns></returns>
    public Alignment GetAlignment (object cellValue)
    {
        if (AlignmentGetter is { })
        {
            return AlignmentGetter (cellValue);
        }

        return Alignment;
    }

    /// <summary>
    ///     Returns the full string to render (which may be truncated if too long) that the current style says best
    ///     represents the given <paramref name="value"/>
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public string GetRepresentation (object value)
    {
        if (!string.IsNullOrWhiteSpace (Format))
        {
            if (value is IFormattable f)
            {
                return f.ToString (Format, null);
            }
        }

        if (RepresentationGetter is { })
        {
            return RepresentationGetter (value);
        }

        return value.ToString () ?? string.Empty;
    }
}
