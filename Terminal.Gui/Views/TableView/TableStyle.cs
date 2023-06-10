using System.Collections.Generic;

namespace Terminal.Gui; 

/// <summary>
/// Defines rendering options that affect how the table is displayed.
/// 
/// <a href="https://gui-cs.github.io/Terminal.Gui/articles/tableview.html">See TableView Deep Dive for more information</a>.
/// </summary>
public class TableStyle {

	/// <summary>
	/// Gets or sets a flag indicating whether to render headers of a <see cref="TableView"/>.
	/// Defaults to <see langword="true"/>.
	/// </summary>
	/// <remarks><see cref="ShowHorizontalHeaderOverline"/>, <see cref="ShowHorizontalHeaderUnderline"/> etc
	/// may still be used even if <see cref="ShowHeaders"/> is <see langword="false"/>.</remarks>
	public bool ShowHeaders { get; set; } = true;

	/// <summary>
	/// When scrolling down always lock the column headers in place as the first row of the table
	/// </summary>
	public bool AlwaysShowHeaders { get; set; } = false;

	/// <summary>
	/// True to render a solid line above the headers
	/// </summary>
	public bool ShowHorizontalHeaderOverline { get; set; } = true;

	/// <summary>
	/// True to render a solid line under the headers
	/// </summary>
	public bool ShowHorizontalHeaderUnderline { get; set; } = true;

	/// <summary>
	/// True to render a solid line vertical line between cells
	/// </summary>
	public bool ShowVerticalCellLines { get; set; } = true;

	/// <summary>
	/// True to render a solid line vertical line between headers
	/// </summary>
	public bool ShowVerticalHeaderLines { get; set; } = true;

	/// <summary>
	/// True to render a arrows on the right/left of the table when 
	/// there are more column(s) that can be scrolled to.  Requires
	/// <see cref="ShowHorizontalHeaderUnderline"/> to be true.
	/// Defaults to true
	/// </summary>
	public bool ShowHorizontalScrollIndicators { get; set; } = true;


	/// <summary>
	/// Gets or sets a flag indicating whether there should be a horizontal line after all the data
	/// in the table. Defaults to <see langword="false"/>.
	/// </summary>
	public bool ShowHorizontalBottomline { get; set; } = false;

	/// <summary>
	/// True to invert the colors of the first symbol of the selected cell in the <see cref="TableView"/>.
	/// This gives the appearance of a cursor for when the <see cref="ConsoleDriver"/> doesn't otherwise show
	/// this
	/// </summary>
	public bool InvertSelectedCellFirstCharacter { get; set; } = false;

	/// <summary>
	/// Gets or sets a flag indicating whether to force <see cref="ColorScheme.Normal"/> use when rendering
	/// vertical cell lines (even when <see cref="TableView.FullRowSelect"/> is on).
	/// </summary>
	public bool AlwaysUseNormalColorForVerticalCellLines { get; set; } = false;

	/// <summary>
	/// Collection of columns for which you want special rendering (e.g. custom column lengths, text alignment etc)
	/// </summary>
	public Dictionary<int, ColumnStyle> ColumnStyles { get; set; } = new Dictionary<int, ColumnStyle> ();

	/// <summary>
	/// Delegate for coloring specific rows in a different color.  For cell color <see cref="ColumnStyle.ColorGetter"/>
	/// </summary>
	/// <value></value>
	public RowColorGetterDelegate RowColorGetter { get; set; }

	/// <summary>
	/// Determines rendering when the last column in the table is visible but it's
	/// content or <see cref="ColumnStyle.MaxWidth"/> is less than the remaining 
	/// space in the control.  True (the default) will expand the column to fill
	/// the remaining bounds of the control.  False will draw a column ending line
	/// and leave a blank column that cannot be selected in the remaining space.  
	/// </summary>
	/// <value></value>
	public bool ExpandLastColumn { get; set; } = true;

	/// <summary>
	/// <para>
	/// Determines how <see cref="TableView.ColumnOffset"/> is updated when scrolling
	/// right off the end of the currently visible area.
	/// </para>
	/// <para>
	/// If true then when scrolling right the scroll offset is increased the minimum required to show
	/// the new column.  This may be slow if you have an incredibly large number of columns in
	/// your table and/or slow <see cref="ColumnStyle.RepresentationGetter"/> implementations
	/// </para>
	/// <para>
	/// If false then scroll offset is set to the currently selected column (i.e. PageRight).
	/// </para>
	/// </summary>
	public bool SmoothHorizontalScrolling { get; set; } = true;

	/// <summary>
	/// Returns the entry from <see cref="ColumnStyles"/> for the given <paramref name="col"/> or null if no custom styling is defined for it
	/// </summary>
	/// <param name="col"></param>
	/// <returns></returns>
	public ColumnStyle GetColumnStyleIfAny (int col)
	{
		return ColumnStyles.TryGetValue (col, out ColumnStyle result) ? result : null;
	}

	/// <summary>
	/// Returns an existing <see cref="ColumnStyle"/> for the given <paramref name="col"/> or creates a new one with default options
	/// </summary>
	/// <param name="col"></param>
	/// <returns></returns>
	public ColumnStyle GetOrCreateColumnStyle (int col)
	{
		if (!ColumnStyles.ContainsKey (col))
			ColumnStyles.Add (col, new ColumnStyle ());

		return ColumnStyles [col];
	}
}