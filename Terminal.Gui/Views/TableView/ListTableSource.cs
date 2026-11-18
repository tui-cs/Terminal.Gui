using System.Collections;
using System.Data;

namespace Terminal.Gui.Views;

/// <summary>
///     <see cref="ITableSource"/> implementation that wraps a <see cref="System.Collections.IList"/>.  This class is
///     mutable: changes are permitted to the wrapped <see cref="IList"/>.
/// </summary>
public class ListTableSource : ITableSource
{
    /// <summary>The list this source wraps.</summary>
    public IList List;

    /// <summary>The style this source uses.</summary>
    public ListColumnStyle Style;

    private readonly TableView _tableView;
    private Rectangle _lastBounds;
    private IList? _lastList;
    private int _lastMaxCellWidth;
    private int _lastMinCellWidth;
    private ListColumnStyle? _lastStyle;

    /// <summary>
    ///     Creates a new columned list table instance based on the data in <paramref name="list"/> and dimensions from
    ///     <paramref name="tableView"/>.
    /// </summary>
    /// <param name="list"></param>
    /// <param name="tableView"></param>
    /// <param name="style"></param>
    public ListTableSource (IList list, TableView tableView, ListColumnStyle style)
    {
        List = list;
        _tableView = tableView;
        Style = style;

        DataTable = CreateTable (CalculateColumns ());

        // TODO: Determine the best event for this
        tableView.DrawingContent += TableView_DrawContent;
    }

    /// <summary>Creates a new instance with default <see cref="ListColumnStyle"/>.</summary>
    public ListTableSource (IList list, TableView tableView) : this (list, tableView, new ListColumnStyle ()) { }

    /// <summary>The number of items in the <see cref="IList"/> source.</summary>
    public int Count => List.Count;

    /// <summary>The <see cref="System.Data.DataTable"/> this source wraps.</summary>
    public DataTable DataTable { get; private set; }

    /// <inheritdoc/>
    public object this [int row, int col]
    {
        get
        {
            int idx;

            if (Style.Orientation == Orientation.Vertical)
            {
                idx = col * Rows + row;
            }
            else
            {
                idx = row * Columns + col;
            }

            if (idx < 0 || idx >= Count)
            {
                return null!;
            }

            return List [idx]!;
        }
    }

    /// <inheritdoc/>
    public int Rows => DataTable.Rows.Count;

    /// <inheritdoc/>
    public int Columns => DataTable.Columns.Count;

    /// <inheritdoc/>
    public string [] ColumnNames => Enumerable.Range (0, Columns).Select (n => n.ToString ()).ToArray ();

    private int CalculateColumns ()
    {
        int cols;

        int colWidth = CalculateMaxLength ();

        if (colWidth > _tableView.MaxCellWidth)
        {
            colWidth = _tableView.MaxCellWidth;
        }

        if (_tableView.MinCellWidth > 0 && colWidth < _tableView.MinCellWidth)
        {
            if (_tableView.MinCellWidth > _tableView.MaxCellWidth)
            {
                colWidth = _tableView.MaxCellWidth;
            }
            else
            {
                colWidth = _tableView.MinCellWidth;
            }
        }

        if (Style.Orientation == Orientation.Vertical != Style.ScrollParallel)
        {
            float f = (float)_tableView.Viewport.Height - _tableView.GetHeaderHeight ();
            cols = (int)Math.Ceiling (Count / f);
        }
        else
        {
            cols = (int)Math.Ceiling (((float)_tableView.Viewport.Width - 1) / colWidth) - 2;
        }

        return cols > 1 ? cols : 1;
    }

    /// <summary>Returns the size in characters of the longest value read from <see cref="ListTableSource.List"/></summary>
    /// <returns></returns>
    private int CalculateMaxLength ()
    {
        if (Count == 0)
        {
            return 0;
        }

        var maxLength = 0;

        foreach (object t in List)
        {
            int l;

            if (t is string s)
            {
                l = s.GetColumns ();
            }
            else
            {
                l = t.ToString ()?.Length ?? 0;
            }

            if (l > maxLength)
            {
                maxLength = l;
            }
        }

        return maxLength;
    }

    /// <summary>Creates a <see cref="DataTable"/> from an <see cref="IList"/> to display in a <see cref="TableView"/>.</summary>
    private DataTable CreateTable (int cols = 1)
    {
        var table = new DataTable ();

        for (var col = 0; col < cols; col++)
        {
            table.Columns.Add (new DataColumn (col.ToString ()));
        }

        for (var row = 0; row < Count / table.Columns.Count; row++)
        {
            table.Rows.Add ();
        }

        // return partial row
        if (Count % table.Columns.Count != 0)
        {
            table.Rows.Add ();
        }

        return table;
    }

    private void TableView_DrawContent (object? sender, DrawEventArgs e)
    {
        if (!_tableView.Viewport.Equals (_lastBounds)
            || _tableView.MaxCellWidth != _lastMaxCellWidth
            || _tableView.MinCellWidth != _lastMinCellWidth
            || Style != _lastStyle
            || List != _lastList)
        {
            DataTable = CreateTable (CalculateColumns ());
        }

        _lastBounds = _tableView.Viewport;
        _lastMinCellWidth = _tableView.MinCellWidth;
        _lastMaxCellWidth = _tableView.MaxCellWidth;
        _lastStyle = Style;
        _lastList = List;
    }
}
